using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [RouteFX.cs] v1 (신규, v9.13 2026-09-23) - 선로 v2 보조 연출
///
/// 1) 세계 밀림 (ParallaxBackground v4.2 가 매 프레임 부른다): 기차가 가지로 들어갈 때 지면과 같이 밀려야 하는 것들을
///    세로로 옮긴다 - 손님(보스 제외: 등장 연출 자리 고정) / 길가 바위 / 재료 조각 / 미끼. 기차 위 것(셰프·포탑·상자·먼지)은 안 옮긴다
/// 2) 선로 톤 (RouteToneOn): 고른 길로 달리는 동안 화면 전체 색 + 가장자리 어둡기.
///    위험 = 붉게 / 안개 = 푸르게 흐릿 / 폐역 = 잿빛 / 사냥터 = 따뜻하게 / 곧은 길 = 없음. 웨이브가 끝나면 걷힌다 (WaveManager)
///    HUD(10) 아래 캔버스 5 - 글자·카드는 물들지 않는다. 클릭을 막지 않는다
///
/// 사용법: 없음! 파일만 넣으면 게임 시작 시 스스로 생성된다. SetTone(routeId) / ClearTone() 은 WaveManager 가 부른다
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class RouteFX : MonoBehaviour
{
    private const float TONE_IN_SEC = 1.2f;     // 톤이 배는 시간
    private const float TONE_OUT_SEC = 1.0f;    // 걷히는 시간

    private static RouteFX instance;

    private Canvas canvas;
    private Image tint;          // 전체 화면 단색
    private Image vignette;      // 가장자리 어둡기 (코드로 구운 128x72 그라데이션)
    private Color tintTarget = new Color(0f, 0f, 0f, 0f);
    private Color vigTarget = new Color(0f, 0f, 0f, 0f);
    private Color tintNow = new Color(0f, 0f, 0f, 0f);
    private Color vigNow = new Color(0f, 0f, 0f, 0f);
    private float fadeSec = TONE_IN_SEC;
    private static Sprite vignetteSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("RouteFX");
        DontDestroyOnLoad(go);
        go.AddComponent<RouteFX>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        Build();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // ─────────────────────────────────────────────
    // 1) 세계 밀림
    // ─────────────────────────────────────────────
    /// <summary>지면과 같이 밀려야 하는 월드 오브젝트를 dy 만큼 세로로 옮긴다 (ParallaxBackground 가 밀림 프레임마다 부른다)</summary>
    public static void ShiftWorldObjects(float dy)
    {
        if (Mathf.Abs(dy) < 1e-6f) return;
        Vector3 d = new Vector3(0f, dy, 0f);

        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null || enemies[i] is BossEnemy) continue;   // 보스는 등장 연출 자리를 스스로 잡는다
            enemies[i].transform.position += d;
        }
        ResourceRock[] rocks = FindObjectsByType<ResourceRock>(FindObjectsSortMode.None);
        for (int i = 0; i < rocks.Length; i++)
            if (rocks[i] != null) rocks[i].transform.position += d;
        PickupFX[] pickups = FindObjectsByType<PickupFX>(FindObjectsSortMode.None);
        for (int i = 0; i < pickups.Length; i++)
            if (pickups[i] != null) pickups[i].transform.position += d;
        GameObject bait = GameObject.Find("Bait");
        if (bait != null) bait.transform.position += d;
    }

    // ─────────────────────────────────────────────
    // 2) 선로 톤
    // ─────────────────────────────────────────────
    /// <summary>고른 길의 톤을 켠다 (routeId: danger / hunt / fog / ghost / 그 외 = 없음)</summary>
    public static void SetTone(string routeId)
    {
        if (instance == null) return;
        Color t, v;
        ToneOf(routeId, out t, out v);
        if (!GameBalance.RouteToneOn) { t.a = 0f; v.a = 0f; }
        instance.tintTarget = t;
        instance.vigTarget = v;
        instance.fadeSec = TONE_IN_SEC;
    }

    /// <summary>톤을 걷는다 (웨이브 끝, 런 포기)</summary>
    public static void ClearTone()
    {
        if (instance == null) return;
        Color t = instance.tintTarget; t.a = 0f;
        Color v = instance.vigTarget; v.a = 0f;
        instance.tintTarget = t;
        instance.vigTarget = v;
        instance.fadeSec = TONE_OUT_SEC;
    }

    /// <summary>선로별 색 (단색 틴트, 가장자리 어둡기)</summary>
    private static void ToneOf(string id, out Color tint, out Color vig)
    {
        switch (id)
        {
            case "danger": tint = new Color(0.86f, 0.31f, 0.16f, 0.10f); vig = new Color(0.35f, 0.08f, 0.04f, 0.50f); return;
            case "hunt": tint = new Color(0.94f, 0.67f, 0.24f, 0.06f); vig = new Color(0.31f, 0.20f, 0.04f, 0.25f); return;
            case "fog": tint = new Color(0.59f, 0.67f, 0.78f, 0.14f); vig = new Color(0.24f, 0.27f, 0.35f, 0.55f); return;
            case "ghost": tint = new Color(0.47f, 0.51f, 0.67f, 0.10f); vig = new Color(0.16f, 0.16f, 0.24f, 0.50f); return;
        }
        tint = new Color(0f, 0f, 0f, 0f); vig = new Color(0f, 0f, 0f, 0f);
    }

    private void Build()
    {
        canvas = UIFactory.CreateCanvas("RouteFX_Canvas", 5);   // HUD(10) 아래
        canvas.transform.SetParent(transform, false);
        GraphicRaycaster gr = canvas.GetComponent<GraphicRaycaster>();
        if (gr != null) gr.enabled = false;   // 클릭을 절대 막지 않는다

        tint = MakeFull("Tint");
        vignette = MakeFull("Vignette");
        vignette.sprite = VignetteSprite();
        vignette.type = Image.Type.Simple;
        vignette.preserveAspect = false;
        tint.color = tintNow;
        vignette.color = vigNow;
    }

    private Image MakeFull(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(canvas.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    /// <summary>가장자리로 갈수록 진해지는 흰 그라데이션 128x72 (색은 Image.color 로 입힌다)</summary>
    private static Sprite VignetteSprite()
    {
        if (vignetteSprite != null) return vignetteSprite;
        int w = 128, h = 72;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x / (float)(w - 1) - 0.5f) * 2f;
                float dy = (y / (float)(h - 1) - 0.5f) * 2f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / 1.25f;
                float a = Mathf.Clamp01((r - 0.5f) / 0.5f);
                a = a * a;
                px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        vignetteSprite = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
        return vignetteSprite;
    }

    private void Update()
    {
        if (tint == null) return;
        float k = Mathf.Clamp01(Time.unscaledDeltaTime / Mathf.Max(0.05f, fadeSec)) * 3f;   // 대략 fadeSec 안에 도달
        tintNow = Color.Lerp(tintNow, tintTarget, k);
        vigNow = Color.Lerp(vigNow, vigTarget, k);
        tint.color = tintNow;
        vignette.color = vigNow;
        bool on = tintNow.a > 0.003f || vigNow.a > 0.003f;
        if (tint.gameObject.activeSelf != on) { tint.gameObject.SetActive(on); vignette.gameObject.SetActive(on); }
    }
}
