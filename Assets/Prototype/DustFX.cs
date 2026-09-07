using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [DustFX.cs] v1 (신규 파일) - 탑다운 먼지 퍼프 연출 (2026-09-07, Apocalypse Express 문법)
///
/// Resources/Sprites/WDT/dust_0 ~ dust_3 (24x24, 작고 진함 -> 크고 옅음) 4프레임을 짧게 재생하는 퍼프를 풀에서 꺼내 쓴다.
///   1) 기차 바퀴 먼지: 지면이 흐르는 동안(ParallaxBackground.CurrentSpeed > 0.5) 기차 위아래 바퀴선(y = ±WHEEL_Y)에
///      무작위 x로 퍼프를 흘린다. 퍼프는 지면과 같은 속도로 오른쪽으로 흘러가 "기차가 달린다"는 느낌을 준다
///   2) 적 이동 먼지: 0.15초마다 살아있는 Enemy를 훑어 이동 중(속도 > 0.8)인 지상 적 뒤에 퍼프 (비행 적은 제외)
///   3) 외부 훅: DustFX.Puff(위치, 크기) - 대시/착지/폭발 등 어디서나 한 줄로 호출 가능
/// 정렬: ORDER_DUST(-5) = 선로(-10) 위, 기차 칸(0 이상) 아래 -> 기차 밑에서 새어 나오는 것처럼 보인다
///
/// 사용법: 없음! 파일만 넣으면 게임 시작 시 스스로 생성된다. (SpriteBank.cs 필요, dust_*.png 없으면 아무것도 안 함)
/// 조절값: WHEEL_INTERVAL / ENEMY_INTERVAL / PUFF_LIFE / MAX_PUFFS
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class DustFX : MonoBehaviour
{
    private const float WHEEL_Y = 1.85f;            // 기차 몸통 가장자리(±1.8) 바로 바깥
    private const float WHEEL_X_MIN = -6.0f;        // 기차 x 범위 (CarEdgesX -6.5 ~ 11.5 안쪽)
    private const float WHEEL_X_MAX = 11.0f;
    private const float WHEEL_INTERVAL = 0.11f;     // 바퀴 퍼프 간격(초) - 위/아래 번갈아
    private const float ENEMY_INTERVAL = 0.15f;     // 적 훑기 간격(초)
    private const float ENEMY_MIN_SPEED = 0.8f;     // 이 속도(유닛/초) 이상 움직이는 적만
    private const float PUFF_LIFE = 0.45f;          // 퍼프 수명(초) - 4프레임
    private const int MAX_PUFFS = 48;               // 풀 크기 (초과 시 가장 오래된 것 재사용)
    private const int ORDER_DUST = -5;
    private const float MIN_GROUND_SPEED = 0.5f;    // 지면이 이 속도 이상 흐를 때만 바퀴 먼지

    private static DustFX instance;
    private static readonly string[] FLYING_KEYS = { "테라노돈", "프테라", "플라이", "익룡", "프테로" };

    private class PuffItem
    {
        public SpriteRenderer sr;
        public float age;
        public float life;
        public Vector3 vel;
        public bool active;
    }

    private readonly List<PuffItem> pool = new List<PuffItem>();
    private Sprite[] frames;
    private float wheelTimer, enemyTimer;
    private bool wheelTop;
    private readonly Dictionary<Enemy, Vector3> lastEnemyPos = new Dictionary<Enemy, Vector3>();
    private readonly List<Enemy> deadKeys = new List<Enemy>();

    // ─────────────────────────────────────────────
    // 부트스트랩
    // ─────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        if (!SpriteBank.Has("dust_0")) { Debug.Log("[DustFX] dust_*.png 없음 - 먼지 연출 생략"); return; }
        GameObject go = new GameObject("DustFX");
        DontDestroyOnLoad(go);
        go.AddComponent<DustFX>();
    }

    /// <summary>외부 훅: 위치에 퍼프 하나 (scale 1 = 0.75유닛)</summary>
    public static void Puff(Vector3 pos, float scale)
    {
        if (instance != null) instance.Spawn(pos, scale, Vector3.zero);
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        frames = new Sprite[4];
        for (int i = 0; i < 4; i++) frames[i] = SpriteBank.Get("dust_" + i);
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("[DustFX] 먼지 퍼프 연출 준비 (풀 " + MAX_PUFFS + ")");
    }

    private void OnDestroy()
    {
        if (instance == this) { SceneManager.sceneLoaded -= OnSceneLoaded; instance = null; }
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // 씬 리로드([다시 굽는다]) 시 죽은 퍼프 참조 정리
        for (int i = 0; i < pool.Count; i++) if (pool[i].sr == null) pool[i].active = false;
        lastEnemyPos.Clear();
    }

    // ─────────────────────────────────────────────
    // 매 프레임: 퍼프 갱신 -> 바퀴 먼지 -> 적 먼지
    // ─────────────────────────────────────────────
    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        UpdatePuffs(dt);

        float ground = ParallaxBackground.CurrentSpeed;
        if (ground > MIN_GROUND_SPEED)
        {
            wheelTimer += dt;
            if (wheelTimer >= WHEEL_INTERVAL)
            {
                wheelTimer = 0f; wheelTop = !wheelTop;
                float x = Random.Range(WHEEL_X_MIN, WHEEL_X_MAX);
                float y = wheelTop ? WHEEL_Y : -WHEEL_Y;
                Spawn(new Vector3(x, y, 0f), Random.Range(0.8f, 1.1f), new Vector3(ground * 0.9f, wheelTop ? 0.4f : -0.4f, 0f));
            }
        }

        enemyTimer += dt;
        if (enemyTimer >= ENEMY_INTERVAL)
        {
            enemyTimer = 0f;
            ScanEnemies(ENEMY_INTERVAL, ground);
        }
    }

    /// <summary>적 위치 변화량으로 이동 중인 지상 적을 찾아 뒤쪽에 퍼프</summary>
    private void ScanEnemies(float interval, float ground)
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy e = enemies[i];
            if (e == null || IsFlying(e)) continue;
            Vector3 now = e.transform.position;
            Vector3 prev;
            if (lastEnemyPos.TryGetValue(e, out prev))
            {
                Vector3 d = now - prev;
                float speed = d.magnitude / interval;
                if (speed > ENEMY_MIN_SPEED)
                {
                    Vector3 back = -d.normalized * 0.55f;
                    Spawn(now + back, 0.75f, new Vector3(ground * 0.5f, 0f, 0f));
                }
            }
            lastEnemyPos[e] = now;
        }
        // 죽은 적 항목 정리 (파괴된 키 제거)
        if (lastEnemyPos.Count > enemies.Length + 8)
        {
            deadKeys.Clear();
            foreach (KeyValuePair<Enemy, Vector3> kv in lastEnemyPos) if (kv.Key == null) deadKeys.Add(kv.Key);
            for (int i = 0; i < deadKeys.Count; i++) lastEnemyPos.Remove(deadKeys[i]);
        }
    }

    private static bool IsFlying(Enemy e)
    {
        string n = e.data.enemyName;
        if (string.IsNullOrEmpty(n)) return false;
        for (int i = 0; i < FLYING_KEYS.Length; i++) if (n.Contains(FLYING_KEYS[i])) return true;
        return false;
    }

    // ─────────────────────────────────────────────
    // 퍼프 풀
    // ─────────────────────────────────────────────
    private void Spawn(Vector3 pos, float scale, Vector3 vel)
    {
        PuffItem p = null;
        for (int i = 0; i < pool.Count; i++) if (!pool[i].active) { p = pool[i]; break; }
        if (p == null)
        {
            if (pool.Count >= MAX_PUFFS)
            {
                // 가장 오래된 것 재사용
                p = pool[0];
                for (int i = 1; i < pool.Count; i++) if (pool[i].age > p.age) p = pool[i];
            }
            else
            {
                p = new PuffItem();
                GameObject go = new GameObject("DustPuff");
                go.transform.SetParent(transform, false);
                p.sr = go.AddComponent<SpriteRenderer>();
                p.sr.sortingOrder = ORDER_DUST;
                pool.Add(p);
            }
        }
        if (p.sr == null)
        {
            GameObject go = new GameObject("DustPuff");
            go.transform.SetParent(transform, false);
            p.sr = go.AddComponent<SpriteRenderer>();
            p.sr.sortingOrder = ORDER_DUST;
        }
        p.active = true; p.age = 0f; p.life = PUFF_LIFE; p.vel = vel;
        p.sr.transform.position = pos;
        p.sr.transform.localScale = new Vector3(scale, scale, 1f);
        p.sr.flipX = Random.value < 0.5f;
        p.sr.sprite = frames[0];
        p.sr.color = new Color(1f, 1f, 1f, 0.9f);
        p.sr.enabled = true;
    }

    private void UpdatePuffs(float dt)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            PuffItem p = pool[i];
            if (!p.active || p.sr == null) continue;
            p.age += dt;
            if (p.age >= p.life) { p.active = false; p.sr.enabled = false; continue; }
            float t = p.age / p.life;
            int f = Mathf.Min(3, Mathf.FloorToInt(t * 4f));
            p.sr.sprite = frames[f];
            p.sr.transform.position += p.vel * dt;
            Color c = p.sr.color; c.a = 0.9f * (1f - t * t); p.sr.color = c;
        }
    }
}
