using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [ChefVisual.cs] v2 - 셰프 스프라이트 애니메이션 (2026-09-07, 유저 제작 도트 8장 대응)
///
/// 씬의 "Chef" 오브젝트에 자동으로 붙어서 Resources/Sprites/WDT/ 의 셰프 PNG로 걷기/대시를 그린다.
///   파일 규약: hero_{s|n|e}_{idle|run0|run1|...}.png  (서향은 동향을 좌우 반전)
///   - 방향별 달리기 프레임 수를 자동 감지한다 (run0부터 번호가 이어지는 만큼). 예: s/n = run0~run1, e = run0
///     · 2장 이상: run0 -> run1 -> ... 순환
///     · 1장뿐:    run0 <-> idle 교대 (동향처럼 걷기 프레임이 한 장인 경우)
///     · 0장:      idle 고정
///   - 대시: 전용 프레임 없이 달리기 순환을 1.8배 속도로 + 잔상(고스트) 스프라이트를 흘린다
///   - 발밑 그림자 타원(코드 생성)으로 갑판 위에 서 있는 느낌을 준다
///   - ChefController는 건드리지 않는다: 매 프레임 위치 변화량으로 방향·달리기·대시를 스스로 판단
///   - hero_ 세트가 없으면 구 chef_ 세트(v7e)로 폴백, 그것도 없으면 아무것도 하지 않는다 (씬 placeholder 유지)
///
/// 조절값: SPRITE_Y_OFFSET(발 위치) / RUN_FPS / DASH_FPS_MUL / SHOW_SHADOW / SORT_ORDER
/// 사용법: 없음. PNG 8장 + 이 파일 + SpriteBank.cs + Editor/WDTSpriteImporter.cs 만 있으면 된다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class ChefVisual : MonoBehaviour
{
    private const float SPRITE_Y_OFFSET = -0.35f;  // 피벗이 발밑이라 오브젝트 중심보다 살짝 아래에 발을 둔다 (걷기 범위 y -1.5~1.5 -> 발 -1.85~1.15)
    private const float RUN_FPS = 7f;               // 달리기 프레임 속도 (2프레임 순환 기준)
    private const float DASH_FPS_MUL = 1.8f;        // 대시 중 프레임 속도 배율
    private const float MOVE_EPS = 0.6f;            // 이 속도(유닛/초) 이상이면 "이동 중"
    private const int SORT_ORDER = 6;               // 포탑 돔(-1)/적(5) 위, 전리품(58)/팝업(60) 아래
    private const bool SHOW_SHADOW = true;          // 발밑 그림자 타원
    private const float GHOST_INTERVAL = 0.06f;     // 대시 잔상 생성 간격(초)
    private const float GHOST_LIFE = 0.22f;         // 잔상이 사라지기까지(초)
    private const float GHOST_ALPHA = 0.45f;        // 잔상 시작 투명도
    private const int MAX_RUN_FRAMES = 8;           // run0~run7 까지 탐색

    private static string prefix = "hero_";         // "hero_"(유저 도트) 또는 "chef_"(구 v7e 세트)

    private SpriteRenderer sr;
    private Transform spriteTf;
    private Vector3 lastPos;
    private string dir = "s";                       // s / n / e / w
    private float animTime = 0f;
    private bool wasMoving = false;
    private float ghostTimer = 0f;
    private readonly Dictionary<string, string[]> cycles = new Dictionary<string, string[]>();   // 방향 -> 달리기 프레임 이름 순환
    private readonly List<Ghost> ghosts = new List<Ghost>();
    private static Sprite shadowSprite;

    private class Ghost
    {
        public SpriteRenderer sr;
        public float life;
    }

    // ─────────────────────────────────────────────
    // 부트스트랩: 씬 로드마다 "Chef"를 찾아 붙인다
    // ─────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryAttach();
    }

    private static void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        TryAttach();
    }

    private static void TryAttach()
    {
        GameObject chef = GameObject.Find("Chef");
        if (chef == null || chef.GetComponent<ChefVisual>() != null) return;
        if (SpriteBank.Has("hero_s_idle")) prefix = "hero_";
        else if (SpriteBank.Has("chef_s_idle")) prefix = "chef_";
        else
        {
            Debug.Log("[ChefVisual] 셰프 PNG 없음 (hero_s_idle / chef_s_idle) - 씬 placeholder 스프라이트 유지");
            return;
        }
        chef.AddComponent<ChefVisual>();
    }

    private void Awake()
    {
        // 기존 렌더러 끄기 (본체 + 자식). 로직/충돌은 그대로
        SpriteRenderer[] old = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < old.Length; i++) old[i].enabled = false;

        // 방향별 달리기 순환 구성
        string[] dirs = { "s", "n", "e" };
        for (int d = 0; d < dirs.Length; d++) cycles[dirs[d]] = BuildCycle(dirs[d]);

        // 그림자 (스프라이트보다 먼저 만들어 아래 정렬)
        if (SHOW_SHADOW)
        {
            GameObject sh = new GameObject("ChefShadow");
            sh.transform.SetParent(transform, false);
            sh.transform.localPosition = new Vector3(0f, SPRITE_Y_OFFSET + 0.02f, 0f);
            SpriteRenderer ssr = sh.AddComponent<SpriteRenderer>();
            ssr.sprite = GetShadowSprite();
            ssr.color = new Color(0f, 0f, 0f, 0.35f);
            ssr.sortingOrder = SORT_ORDER - 2;
        }

        GameObject go = new GameObject("ChefSprite");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, SPRITE_Y_OFFSET, 0f);
        spriteTf = go.transform;
        sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = SORT_ORDER;
        sr.sprite = SpriteBank.Get(prefix + "s_idle");
        lastPos = transform.position;
        Debug.Log("[ChefVisual] 셰프 스프라이트 적용 (" + prefix + "*, 달리기 프레임 s/n/e = "
            + cycles["s"].Length + "/" + cycles["n"].Length + "/" + cycles["e"].Length + ", 기존 렌더러 " + old.Length + "개 숨김)");
    }

    /// <summary>방향별 달리기 프레임 이름 순환을 만든다 (파일이 있는 만큼)</summary>
    private static string[] BuildCycle(string d)
    {
        List<string> frames = new List<string>();
        for (int i = 0; i < MAX_RUN_FRAMES; i++)
        {
            string name = prefix + d + "_run" + i;
            if (!SpriteBank.Has(name)) break;
            frames.Add(name);
        }
        string idle = prefix + d + "_idle";
        if (frames.Count == 0) return new string[] { idle };
        if (frames.Count == 1) return new string[] { frames[0], idle };
        return frames.ToArray();
    }

    // ─────────────────────────────────────────────
    // 매 프레임: 이동량 -> 방향/상태 -> 프레임 선택 -> 대시 잔상
    // ─────────────────────────────────────────────
    private void LateUpdate()
    {
        if (sr == null) return;
        float dt = Time.deltaTime;
        Vector3 delta = transform.position - lastPos;
        lastPos = transform.position;
        UpdateGhosts(dt);
        if (dt <= 0f) return;

        float speed = delta.magnitude / dt;
        bool moving = speed > MOVE_EPS;
        bool dashing = speed > GameBalance.ChefMoveSpeed * 1.6f;   // 대시(12) vs 달리기(4.2) 사이

        // 방향: 가로가 더 크면 동/서, 아니면 남/북 (멈추면 마지막 방향 유지)
        if (moving)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)) dir = delta.x >= 0f ? "e" : "w";
            else dir = delta.y >= 0f ? "n" : "s";
        }

        // 조리 중엔 조리대(남쪽 = 화면 아래)를 보고 선다
        if (CookingMinigame.IsActive) { moving = false; dashing = false; dir = "s"; }

        string spriteDir = dir == "w" ? "e" : dir;
        string name;
        if (moving)
        {
            if (!wasMoving) animTime = 0f;
            animTime += dt * (dashing ? DASH_FPS_MUL : 1f);
            string[] cyc = cycles[spriteDir];
            int frame = Mathf.FloorToInt(animTime * RUN_FPS) % cyc.Length;
            name = cyc[frame];
        }
        else name = prefix + spriteDir + "_idle";
        wasMoving = moving;

        Sprite s = SpriteBank.Get(name);
        if (s != null) sr.sprite = s;
        sr.flipX = dir == "w";

        // 대시 잔상
        if (dashing)
        {
            ghostTimer += dt;
            if (ghostTimer >= GHOST_INTERVAL) { ghostTimer = 0f; SpawnGhost(); }
        }
        else ghostTimer = GHOST_INTERVAL;   // 대시 시작 즉시 첫 잔상
    }

    /// <summary>현재 스프라이트를 제자리에 복사해 두고 서서히 지운다</summary>
    private void SpawnGhost()
    {
        GameObject go = new GameObject("ChefGhost");
        go.transform.position = spriteTf.position;
        SpriteRenderer g = go.AddComponent<SpriteRenderer>();
        g.sprite = sr.sprite;
        g.flipX = sr.flipX;
        g.sortingOrder = SORT_ORDER - 1;
        g.color = new Color(1f, 1f, 1f, GHOST_ALPHA);
        Ghost gh = new Ghost();
        gh.sr = g; gh.life = GHOST_LIFE;
        ghosts.Add(gh);
    }

    private void UpdateGhosts(float dt)
    {
        for (int i = ghosts.Count - 1; i >= 0; i--)
        {
            Ghost gh = ghosts[i];
            gh.life -= dt;
            if (gh.life <= 0f || gh.sr == null)
            {
                if (gh.sr != null) Destroy(gh.sr.gameObject);
                ghosts.RemoveAt(i);
                continue;
            }
            Color c = gh.sr.color;
            c.a = GHOST_ALPHA * (gh.life / GHOST_LIFE);
            gh.sr.color = c;
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < ghosts.Count; i++)
            if (ghosts[i].sr != null) Destroy(ghosts[i].sr.gameObject);
        ghosts.Clear();
    }

    /// <summary>발밑 그림자: 24x8 타원 텍스처를 코드로 만든다 (32px/유닛 = 0.75 x 0.25 유닛)</summary>
    private static Sprite GetShadowSprite()
    {
        if (shadowSprite != null) return shadowSprite;
        const int w = 24, h = 8;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color32[] px = new Color32[w * h];
        float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x - cx) / (w * 0.5f), dy = (y - cy) / (h * 0.5f);
                bool inside = dx * dx + dy * dy <= 1f;
                px[y * w + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        tex.SetPixels32(px);
        tex.Apply(false, false);
        shadowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        return shadowSprite;
    }
}
