using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [EnemySkin.cs] v1 (신규 파일) - 프리팹 적에게 PNG 스프라이트 입히기 (2026-09-07)
///
/// 문제: WaveManager v6.4는 "프리팹이 없는 종"(코드 폴백)에만 e_*.png를 입혔다. 씬에 프리팹이 할당된 종은
///       프리팹의 placeholder 그림이 그대로 나와서 유저 눈에는 "몬스터 스프라이트가 안 만들어진" 것으로 보였다.
/// 해결: 0.2초마다 살아있는 Enemy를 훑어 아직 스킨이 없는 적에게 PNG 자식("Skin")을 붙이고 프리팹 렌더러는 끈다.
///       WaveManager/Enemy/프리팹은 건드리지 않는다 (스폰 경로가 몇 개든 전부 잡힌다). 보스(BossEnemy)는 제외.
///
/// 종 매핑 (이름 키워드 -> PNG): 6종은 전용 그림, 나머지 10종은 **가장 비슷한 그림 + 색 틴트 + 크기**로 임시 대체
///   (전용 그림이 나오면 e_<이름>.png 만 추가하고 표의 png 이름을 바꾸면 된다)
///   강철 -> e_steel / 전갈 -> e_scorpion / 거북 -> e_tortoise / 네크로·스피노 -> e_necro / 랩터 -> e_raptor
///   테라노돈(노랑)·프테라(보라)·플라이(하늘, 작게)·익룡·프테로(주황) -> e_ptera
///   아르마딜로·안킬로(녹회색, 작게)·파키(분홍)·맘모스(청백, 크게) -> e_tortoise
///   캑터스(초록, 작게) -> e_necro / 파라사우(자홍) -> e_raptor / 카르노(주홍, 크게) -> e_raptor / 모사(하늘, 크게) -> e_steel
/// 크기: WaveManager 폴백과 동일 규칙 (PNG_SCALE 0.6 x 체력 덩치 0.85~1.4 x 종 배율), 프리팹 루트 스케일은 보정
/// 정렬 5 (데크/포탑 위, 처치 팝 아래). 그림은 +x를 향해 그려져 있고 Enemy가 이동 방향으로 회전시킨다
///
/// 사용법: 없음! 파일만 넣으면 게임 시작 시 스스로 생성된다. (SpriteBank.cs + e_*.png 필요)
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class EnemySkin : MonoBehaviour
{
    private const float PNG_SCALE = 0.6f;       // WaveManager.EnemyPngScale 과 동일
    private const int SORT_ORDER = 5;

    private struct Rule
    {
        public string key, png; public Color tint; public float scale;
        public Rule(string key, string png, Color tint, float scale) { this.key = key; this.png = png; this.tint = tint; this.scale = scale; }
    }

    // 먼저 맞는 항목이 이긴다 (강철 랩터가 "랩터"보다 앞에 있어야 함)
    private static readonly Rule[] RULES =
    {
        new Rule("강철", "steel", Color.white, 1f),
        new Rule("전갈", "scorpion", Color.white, 1f),
        new Rule("거북", "tortoise", Color.white, 1f),
        new Rule("네크로", "necro", Color.white, 1f),
        new Rule("스피노", "necro", Color.white, 1f),
        new Rule("테라노돈", "ptera", new Color(1f, 0.95f, 0.6f), 1f),
        new Rule("프테라", "ptera", new Color(0.8f, 0.62f, 1f), 1f),
        new Rule("플라이", "ptera", new Color(0.75f, 0.9f, 1f), 0.7f),
        new Rule("익룡", "ptera", new Color(1f, 0.62f, 0.4f), 1f),
        new Rule("프테로", "ptera", new Color(1f, 0.62f, 0.4f), 1f),
        new Rule("아르마딜로", "tortoise", new Color(0.75f, 0.85f, 0.7f), 0.85f),
        new Rule("안킬로", "tortoise", new Color(0.75f, 0.85f, 0.7f), 0.85f),
        new Rule("캑터스", "necro", new Color(0.55f, 1f, 0.55f), 0.8f),
        new Rule("파라사우", "raptor", new Color(1f, 0.7f, 1f), 1.1f),
        new Rule("모사", "steel", new Color(0.7f, 0.9f, 1f), 1.2f),
        new Rule("파키", "tortoise", new Color(1f, 0.8f, 0.95f), 1.1f),
        new Rule("카르노", "raptor", new Color(1f, 0.55f, 0.35f), 1.3f),
        new Rule("맘모스", "tortoise", new Color(0.8f, 0.9f, 1f), 1.4f),
        new Rule("랩터", "raptor", Color.white, 1f),
    };

    /// <summary>이 적에게 입힌 스프라이트 (다른 코드가 틴트/깜빡임에 쓰고 싶을 때)</summary>
    public SpriteRenderer skin;

    // ─────────────────────────────────────────────
    // 스캐너 (싱글턴) - 새 적을 찾아 스킨을 입힌다
    // ─────────────────────────────────────────────
    private class Scanner : MonoBehaviour
    {
        private const float SCAN_INTERVAL = 0.2f;
        private float timer;
        private bool loggedMissing;

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < SCAN_INTERVAL) return;
            timer = 0f;
            Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                Enemy e = enemies[i];
                if (e == null || e.GetComponent<EnemySkin>() != null || e is BossEnemy) continue;
                if (!Apply(e) && !loggedMissing)
                {
                    loggedMissing = true;
                    Debug.LogWarning("[EnemySkin] e_*.png 를 찾지 못했다 - 프리팹 그림 유지 (Resources/Sprites/WDT/e_raptor.png 등 확인)");
                }
            }
        }
    }

    private static Scanner scanner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (scanner != null) return;
        if (!SpriteBank.Has("e_raptor")) { Debug.Log("[EnemySkin] e_raptor.png 없음 - 적 스킨 생략"); return; }
        GameObject go = new GameObject("EnemySkinScanner");
        DontDestroyOnLoad(go);
        scanner = go.AddComponent<Scanner>();
        Debug.Log("[EnemySkin] 적 PNG 스킨 스캐너 준비");
    }

    /// <summary>적 하나에 PNG 스킨 적용. 이름이 아직 없으면(스폰 직후) 다음 스캔에 다시 시도</summary>
    private static bool Apply(Enemy e)
    {
        string n = e.data.enemyName;
        if (string.IsNullOrEmpty(n)) return true;   // 아직 data 미설정 - 다음에

        // WaveManager 코드 폴백이 이미 PNG "Body"를 붙인 적이면 마커만 달고 끝
        Transform body = e.transform.Find("Body");
        if (body != null)
        {
            SpriteRenderer bsr = body.GetComponent<SpriteRenderer>();
            if (bsr != null && bsr.sprite != null && bsr.sprite.name.StartsWith("e_"))
            {
                EnemySkin done = e.gameObject.AddComponent<EnemySkin>();
                done.skin = bsr;
                return true;
            }
        }

        Rule rule = RULES[RULES.Length - 1];
        for (int i = 0; i < RULES.Length; i++)
            if (n.Contains(RULES[i].key)) { rule = RULES[i]; break; }
        Sprite sprite = SpriteBank.Get("e_" + rule.png);
        if (sprite == null) return false;

        // 프리팹 placeholder 렌더러 끄기 (로직/충돌/태그는 그대로)
        SpriteRenderer[] old = e.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < old.Length; i++) old[i].enabled = false;

        // 체력이 클수록 덩치도 조금 크게 (WaveManager 폴백과 동일) + 프리팹 루트 스케일 보정
        float bulk = Mathf.Clamp(0.85f + e.data.baseHP / 500f, 0.85f, 1.4f);
        float rootScale = Mathf.Abs(e.transform.localScale.x);
        if (rootScale < 0.01f) rootScale = 1f;
        float k = PNG_SCALE * bulk * rule.scale / rootScale;

        GameObject go = new GameObject("Skin");
        go.transform.SetParent(e.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = new Vector3(k, k, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = rule.tint;
        sr.sortingOrder = SORT_ORDER;

        EnemySkin marker = e.gameObject.AddComponent<EnemySkin>();
        marker.skin = sr;
        return true;
    }
}
