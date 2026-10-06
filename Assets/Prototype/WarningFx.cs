using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [WarningFX.cs] v1.2 (v9.19 2026-10-06 웨이브 편성: SpawnCue - 손님이 오는 쪽 화면 가장자리에 뜨는 예고 화살표(갈매기 3개가 기차 쪽으로 물결친다). 게임 시간으로 돌고, 연습으로 멈춘 동안은 같이 멈춘다 / FlashEdges - 글자 없이 가장자리 맥동만) / v1.1 (v9.18 2026-10-06: BossIntro - 보스 등장 띠. 화면을 가로지르는 어두운 띠 + "보스 등장" + 큰 이름 + 한 줄, 가장자리 붉은 맥동 2회. 실시간으로 돌고 게임은 멈추지 않는다) / v1 (신규 파일) - 감사 2-D 결정 사항
/// 전체 화면 경고 연출 - 화면 가장자리 붉은 플래시(비네트) + 중앙 대형 경고 텍스트.
/// 보스 패턴 예고가 눈에 안 들어와서 반응하기 어렵다는 피드백의 해결책.
///
/// 사용법: 어디서든 WarningFX.Flash("낙뢰 폭격!", 2f); 한 줄. 씬 세팅 불필요.
/// 나중에 주방 이벤트 몰입 연출(발톱/불길 오버레이)도 이 캔버스를 재사용한다 (백로그 13절).
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class WarningFX : MonoBehaviour
{
    private static WarningFX instance;

    private Image[] edgeBars = new Image[4];   // 상/하/좌/우 가장자리 띠
    private Text bigText;
    private Coroutine playCo;

    // v1.1: 보스 등장 띠
    private RectTransform bandRoot;
    private CanvasGroup bandGroup;
    private Text bandCaption, bandName, bandSub;
    private Coroutine bandCo;
    private static readonly Color BAND_RED = new Color(1f, 0.2f, 0.12f);

    // v1.2: 편성 예고 화살표 (풀)
    private const int CUE_POOL = 14;            // 포위 한 번에 10개까지 + 겹치는 조각
    private const float CUE_SIDE = 44f;         // 화면 좌우 끝에서 (기준 화면 단위)
    private const float CUE_TOP = 250f;         // 위에서 - 좌상단·우상단 판(아래 끝 184)과 가운데 안내 카드(아래 끝 222) 밑
    private const float CUE_BOTTOM = 232f;      // 아래에서 - 하단 HUD(위 끝 190) 위
    private static readonly Color CUE_COLOR = new Color(1f, 0.45f, 0.12f);
    private class Cue
    {
        public RectTransform root;
        public Image[] bars;                    // 갈매기 3개 x 막대 2개 (바깥 것부터)
        public Vector3 world;                   // 손님이 생기는 자리
        public float angle, age, life;
        public bool on;
    }
    private Cue[] cues;
    private Canvas cueCanvas;

    // ─────────────────────────────────────────────
    // 공개 API
    // ─────────────────────────────────────────────

    /// <summary>붉은 경고 플래시 + 중앙 대형 텍스트. duration 동안 2회 맥동 후 사라진다.</summary>
    public static void Flash(string message, float duration)
    {
        Flash(message, duration, new Color(1f, 0.15f, 0.1f));
    }

    /// <summary>v1.2: 가장자리 맥동만 (글자 없음). 같은 말이 다른 곳(보스 HP 바 밑 띠)에 이미 떠 있을 때 - 화면 가운데를 가리지 않는다</summary>
    public static void FlashEdges(float duration, Color color)
    {
        Flash("", duration, color);
    }

    /// <summary>색 지정 버전 (예: 그로기 = 금색)</summary>
    public static void Flash(string message, float duration, Color color)
    {
        WarningFX fx = Get();
        if (fx.playCo != null) fx.StopCoroutine(fx.playCo);
        fx.playCo = fx.StartCoroutine(fx.PlayFlash(message, duration, color));
    }

    /// <summary>
    /// v1.1: 보스 등장 띠. name = 보스 이름(크게), sub = 그 아래 한 줄, seconds = 떠 있는 시간(실시간, 등장 0.14 + 퇴장 0.25 포함).
    /// 보스 웨이브마다 한 번뿐인 사건이라 화면 가운데를 쓴다. 게임은 멈추지 않는다 (조리 중이어도 흐름이 끊기지 않게)
    /// </summary>
    public static void BossIntro(string name, string sub, float seconds)
    {
        WarningFX fx = Get();
        if (fx.bandRoot == null) return;
        if (fx.bandCo != null) fx.StopCoroutine(fx.bandCo);
        fx.bandCo = fx.StartCoroutine(fx.PlayBand(name, sub, Mathf.Max(0.8f, seconds)));
    }

    /// <summary>
    /// v1.2: 편성 예고 화살표. worldPos = 손님이 생기는 자리(화면 밖이면 그쪽 가장자리에 붙는다), worldDir = 손님이 가는 방향, seconds = 떠 있는 시간.
    /// 잦은 사건이라 작고 조용하게 - 가장자리에 갈매기 3개만. 화면 가운데·HUD 판 위로는 오지 않는다
    /// </summary>
    public static void SpawnCue(Vector3 worldPos, Vector3 worldDir, float seconds)
    {
        if (seconds <= 0.01f) return;
        WarningFX fx = Get();
        if (fx.cues == null) return;

        // 빈 것을 쓰고, 다 쓰는 중이면 가장 오래된 것을 다시 쓴다
        Cue pick = null;
        float bestLeft = float.MaxValue;
        for (int i = 0; i < fx.cues.Length; i++)
        {
            Cue c = fx.cues[i];
            if (!c.on) { pick = c; break; }
            float left = c.life - c.age;
            if (left < bestLeft) { bestLeft = left; pick = c; }
        }
        pick.world = worldPos;
        pick.angle = Mathf.Atan2(worldDir.y, worldDir.x) * Mathf.Rad2Deg;
        pick.age = 0f;
        pick.life = seconds;
        pick.on = true;
        fx.PlaceCue(pick);
        pick.root.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (cues == null) return;
        // 전투가 끝났으면(패배·승리·정차) 남은 예고를 걷는다 - 결과 화면 위에 화살표가 남지 않게
        bool battle = GameManager.Instance == null || GameManager.Instance.currentState == GameManager.GameState.Battle;
        bool frozen = TutorialDirector.InlineFreeze;           // 연습으로 멈춘 동안은 스폰도 멈춘다 - 예고 시간도 같이
        // 카드·스토리 글·증강 창·일시정지·연습 카드가 떠 있는 동안은 숨긴다 (시간 배율로 재지 않는다 - 히트스톱마다 깜빡인다)
        bool paused = frozen || BriefingUI.IsOpen || StoryTexts.IsBlocking || AugmentPickUI.IsOpen || PauseMenu.IsOpen;
        for (int i = 0; i < cues.Length; i++)
        {
            Cue c = cues[i];
            if (!c.on) continue;
            if (!frozen) c.age += Time.deltaTime;
            if (!battle || c.age >= c.life)
            {
                c.on = false;
                c.root.gameObject.SetActive(false);
                continue;
            }
            if (c.root.gameObject.activeSelf == paused) c.root.gameObject.SetActive(!paused);
            if (!paused) PlaceCue(c);
        }
    }

    /// <summary>
    /// 화살표 하나를 제자리에 놓고 물결을 그린다. 자리 = 손님이 가는 길(스폰 자리에서 가는 방향으로 그은 선)이 전장 사각형(HUD 판을 뺀 화면)에 들어오는 점.
    /// 스폰 자리가 이미 전장 안이면(화면을 넓게 본 경우) 그 자리, 길이 사각형을 안 지나면 스폰 자리를 사각형 안으로 끌어당긴 곳
    /// </summary>
    private void PlaceCue(Cue c)
    {
        Camera cam = Camera.main;
        if (cam == null || cueCanvas == null) return;
        // 캔버스 배율 = 화면 폭 / 기준 폭 (이 캔버스의 CanvasScaler 는 폭 맞춤). canvas.scaleFactor 는 만든 첫 프레임엔 아직 1 이라 직접 잰다
        float sf = Mathf.Max(0.01f, Screen.width / Mathf.Max(1f, UIFactory.RefResolution.x));
        float w = Screen.width / sf, h = Screen.height / sf;
        Vector3 sp = cam.WorldToScreenPoint(c.world);
        float xMin = CUE_SIDE, xMax = Mathf.Max(CUE_SIDE, w - CUE_SIDE);
        float yMin = CUE_BOTTOM, yMax = Mathf.Max(CUE_BOTTOM, h - CUE_TOP);
        float sx = sp.x / sf, sy = sp.y / sf;
        float x = Mathf.Clamp(sx, xMin, xMax), y = Mathf.Clamp(sy, yMin, yMax);   // 기본: 끌어당긴 자리
        if (sx < xMin || sx > xMax || sy < yMin || sy > yMax)
        {
            // 화면 밖에서 온다: 가는 방향(카메라가 돌지 않으니 월드 방향 = 화면 방향)으로 그은 선이 사각형에 처음 닿는 점
            float rad = c.angle * Mathf.Deg2Rad;
            float dx = Mathf.Cos(rad), dy = Mathf.Sin(rad);
            float tIn = 0f, tOut = float.MaxValue;
            bool hit = true;
            if (Mathf.Abs(dx) < 0.0001f) hit = sx >= xMin && sx <= xMax;
            else
            {
                float a = (xMin - sx) / dx, b = (xMax - sx) / dx;
                tIn = Mathf.Max(tIn, Mathf.Min(a, b)); tOut = Mathf.Min(tOut, Mathf.Max(a, b));
            }
            if (hit)
            {
                if (Mathf.Abs(dy) < 0.0001f) hit = sy >= yMin && sy <= yMax;
                else
                {
                    float a = (yMin - sy) / dy, b = (yMax - sy) / dy;
                    tIn = Mathf.Max(tIn, Mathf.Min(a, b)); tOut = Mathf.Min(tOut, Mathf.Max(a, b));
                }
            }
            if (hit && tIn <= tOut) { x = sx + dx * tIn; y = sy + dy * tIn; }
        }
        c.root.anchoredPosition = new Vector2(x, y);
        c.root.localRotation = Quaternion.Euler(0f, 0f, c.angle);

        // 등장: 1.5배에서 제자리로 0.12초 / 퇴장: 마지막 0.15초에 흐려진다
        float pop = c.age < 0.12f ? Mathf.Lerp(1.5f, 1f, c.age / 0.12f) : 1f;
        float fade = (c.life - c.age) < 0.15f ? Mathf.Max(0f, (c.life - c.age) / 0.15f) : 1f;
        c.root.localScale = new Vector3(pop, pop, 1f);
        for (int i = 0; i < 3; i++)
        {
            // 물결: 바깥 갈매기부터 안쪽으로 차례로 밝아진다 (초당 2.5번)
            float wave = Mathf.Repeat(c.age * 2.5f - i * 0.22f, 1f);
            float a = (0.35f + 0.65f * Mathf.Clamp01(1f - wave * 2f)) * fade;
            Color col = CUE_COLOR; col.a = a;
            c.bars[i * 2].color = col;
            c.bars[i * 2 + 1].color = col;
        }
    }

    private IEnumerator PlayBand(string name, string sub, float seconds)
    {
        bandName.text = name;
        bandSub.text = sub ?? "";
        bandRoot.gameObject.SetActive(true);
        bool useEdges = playCo == null;   // 패턴 예고(Flash)가 가장자리를 쓰는 중이면 건드리지 않는다
        if (useEdges) for (int i = 0; i < edgeBars.Length; i++) if (edgeBars[i] != null) edgeBars[i].gameObject.SetActive(true);

        const float IN = 0.14f, PUNCH = 0.2f, OUT = 0.25f;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;

            // 띠: 세로로 펼쳐지며 등장, 끝에서 흐려지며 살짝 접힌다
            float kin = Mathf.Clamp01(t / IN);
            float ein = 1f - (1f - kin) * (1f - kin);
            float kout = Mathf.Clamp01((t - (seconds - OUT)) / OUT);
            bandRoot.localScale = new Vector3(1f, ein * (1f - 0.3f * kout), 1f);
            bandGroup.alpha = ein * (1f - kout);

            // 이름: 1.5배에서 제자리로 찍힌다
            float kp = Mathf.Clamp01((t - 0.05f) / PUNCH);
            float ep = 1f - (1f - kp) * (1f - kp);
            float sc = Mathf.Lerp(1.5f, 1f, ep);
            bandName.rectTransform.localScale = new Vector3(sc, sc, 1f);
            Color nc = bandName.color; nc.a = kp; bandName.color = nc;

            // 가장자리 붉은 맥동 2회
            if (useEdges && playCo == null)
            {
                float pulse = (Mathf.Sin((t / seconds) * 4f * Mathf.PI - Mathf.PI * 0.5f) + 1f) * 0.5f;
                Color edge = BAND_RED; edge.a = pulse * 0.4f * (1f - kout);
                for (int i = 0; i < edgeBars.Length; i++) if (edgeBars[i] != null) edgeBars[i].color = edge;
            }
            yield return null;
        }

        bandRoot.gameObject.SetActive(false);
        if (useEdges && playCo == null) for (int i = 0; i < edgeBars.Length; i++) if (edgeBars[i] != null) edgeBars[i].gameObject.SetActive(false);
        bandCo = null;
    }

    // ─────────────────────────────────────────────
    // 내부
    // ─────────────────────────────────────────────
    private static WarningFX Get()
    {
        if (instance != null) return instance;

        GameObject go = new GameObject("WarningFX");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<WarningFX>();
        instance.BuildUI(go);
        return instance;
    }

    private IEnumerator PlayFlash(string message, float duration, Color color)
    {
        bigText.text = message;
        bigText.color = new Color(1f, 0.92f, 0.75f, 0f);

        SetActiveAll(true);

        // 맥동 2회 (unscaled - 일시정지 중에도 보임)
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float phase = (t / duration) * 2f * Mathf.PI * 2f;   // 2회 맥동
            float pulse = (Mathf.Sin(phase - Mathf.PI * 0.5f) + 1f) * 0.5f;   // 0~1 왕복

            // 가장자리 띠: 최대 알파 0.45
            Color edge = color;
            edge.a = pulse * 0.45f;
            for (int i = 0; i < edgeBars.Length; i++)
                if (edgeBars[i] != null) edgeBars[i].color = edge;

            // 중앙 텍스트: 초반 페이드 인, 마지막 0.4초 페이드 아웃
            float textAlpha = 1f;
            if (t < 0.25f) textAlpha = t / 0.25f;
            else if (duration - t < 0.4f) textAlpha = Mathf.Max(0f, (duration - t) / 0.4f);
            Color tc = bigText.color;
            tc.a = textAlpha;
            bigText.color = tc;

            yield return null;
        }

        SetActiveAll(false);
        playCo = null;
    }

    private void SetActiveAll(bool on)
    {
        for (int i = 0; i < edgeBars.Length; i++)
            if (edgeBars[i] != null) edgeBars[i].gameObject.SetActive(on);
        if (bigText != null) bigText.gameObject.SetActive(on);
    }

    private void BuildUI(GameObject host)
    {
        GameObject canvasGo = new GameObject("WarningCanvas");
        canvasGo.transform.SetParent(host.transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 640;   // 증강(600) 위, 스토리(650) 아래
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIFactory.RefResolution;   // v9.14: UI 전체 배율 (GameBalance.UIScale)

        // 가장자리 띠 4개 (상/하/좌/우) - 클릭 통과
        for (int i = 0; i < 4; i++)
        {
            GameObject bar = new GameObject("Edge" + i);
            bar.transform.SetParent(canvasGo.transform, false);
            RectTransform rt = bar.AddComponent<RectTransform>();
            Image img = bar.AddComponent<Image>();
            img.raycastTarget = false;
            edgeBars[i] = img;

            if (i == 0) { rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 1f); rt.sizeDelta = new Vector2(0f, 70f); }        // 상
            else if (i == 1) { rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(1f, 0f); rt.pivot = new Vector2(0.5f, 0f); rt.sizeDelta = new Vector2(0f, 70f); }        // 하
            else if (i == 2) { rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 0.5f); rt.sizeDelta = new Vector2(55f, 0f); }        // 좌
            else { rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = Vector2.one; rt.pivot = new Vector2(1f, 0.5f); rt.sizeDelta = new Vector2(55f, 0f); }        // 우
            rt.anchoredPosition = Vector2.zero;
        }

        // 중앙 대형 경고 텍스트
        GameObject txtGo = new GameObject("BigText");
        txtGo.transform.SetParent(canvasGo.transform, false);
        RectTransform tRt = txtGo.AddComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.5f, 0.5f);
        tRt.anchorMax = new Vector2(0.5f, 0.5f);
        tRt.pivot = new Vector2(0.5f, 0.5f);
        tRt.anchoredPosition = new Vector2(0f, 230f);   // 중앙보다 살짝 위 (전장 가림 최소화)
        tRt.sizeDelta = new Vector2(1500f, 120f);

        bigText = txtGo.AddComponent<Text>();
        bigText.font = KitchenEventManager.GetFont();
        bigText.fontSize = 46;
        bigText.fontStyle = FontStyle.Bold;
        bigText.alignment = TextAnchor.MiddleCenter;
        bigText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bigText.verticalOverflow = VerticalWrapMode.Overflow;
        bigText.raycastTarget = false;

        // 외곽선으로 가독성 확보 (배경 어떤 색이든 읽히게)
        Outline outline = txtGo.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        // ── v1.1: 보스 등장 띠 (화면 폭 전체, 가운데보다 조금 위) ──
        GameObject bandGo = new GameObject("BossBand");
        bandGo.transform.SetParent(canvasGo.transform, false);
        bandRoot = bandGo.AddComponent<RectTransform>();
        bandRoot.anchorMin = new Vector2(0f, 0.5f);
        bandRoot.anchorMax = new Vector2(1f, 0.5f);
        bandRoot.pivot = new Vector2(0.5f, 0.5f);
        bandRoot.anchoredPosition = new Vector2(0f, 150f);
        bandRoot.sizeDelta = new Vector2(0f, 176f);
        Image bandBg = bandGo.AddComponent<Image>();
        bandBg.color = new Color(0.03f, 0.02f, 0.02f, 0.78f);
        bandBg.raycastTarget = false;
        bandGroup = bandGo.AddComponent<CanvasGroup>();
        bandGroup.blocksRaycasts = false;
        bandGroup.interactable = false;

        // 위·아래 붉은 선
        for (int i = 0; i < 2; i++)
        {
            GameObject line = new GameObject(i == 0 ? "LineTop" : "LineBottom");
            line.transform.SetParent(bandGo.transform, false);
            RectTransform lrt = line.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, i == 0 ? 1f : 0f);
            lrt.anchorMax = new Vector2(1f, i == 0 ? 1f : 0f);
            lrt.pivot = new Vector2(0.5f, i == 0 ? 1f : 0f);
            lrt.anchoredPosition = Vector2.zero;
            lrt.sizeDelta = new Vector2(0f, 6f);
            Image lim = line.AddComponent<Image>();
            lim.color = new Color(0.95f, 0.28f, 0.14f, 1f);
            lim.raycastTarget = false;
        }

        bandCaption = MakeBandText(bandGo.transform, "Caption", "보 스   등 장", 24, new Color(1f, 0.45f, 0.3f, 1f), 54f, false);
        bandName = MakeBandText(bandGo.transform, "Name", "", 68, new Color(1f, 0.94f, 0.8f, 1f), -2f, true);
        bandSub = MakeBandText(bandGo.transform, "Sub", "", 24, new Color(0.85f, 0.78f, 0.65f, 1f), -58f, false);
        bandGo.SetActive(false);

        BuildCues(host.transform);

        SetActiveAll(false);
    }

    /// <summary>
    /// v1.2: 예고 화살표 풀. 화살표 하나 = 갈매기(>) 3개, 갈매기 하나 = 끝이 맞닿은 막대 2개. 그림 파일 없이 사각형만으로 그린다.
    /// 경고 캔버스(640)가 아니라 따로 둔 캔버스(460)에 그린다: 오른쪽 알림 줄(455) 위 - 알림이 쌓여도 화살표가 묻히지 않는다 /
    /// 공명 HUD(470)·보스 UI(480)·사고 배너(500)·창들 아래 - 읽어야 할 큰 글자는 화살표가 가리지 않는다
    /// </summary>
    private void BuildCues(Transform hostTf)
    {
        GameObject holder = new GameObject("CueCanvas");
        holder.transform.SetParent(hostTf, false);
        cueCanvas = holder.AddComponent<Canvas>();
        cueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cueCanvas.sortingOrder = 460;
        CanvasScaler cueScaler = holder.AddComponent<CanvasScaler>();
        cueScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cueScaler.referenceResolution = UIFactory.RefResolution;

        cues = new Cue[CUE_POOL];
        for (int n = 0; n < CUE_POOL; n++)
        {
            Cue c = new Cue();
            GameObject go = new GameObject("Cue" + n);
            go.transform.SetParent(holder.transform, false);
            c.root = go.AddComponent<RectTransform>();
            c.root.anchorMin = Vector2.zero; c.root.anchorMax = Vector2.zero;   // 왼쪽 아래 기준 = 화면 좌표를 그대로 쓴다
            c.root.pivot = new Vector2(0.5f, 0.5f);
            c.root.sizeDelta = new Vector2(64f, 40f);
            c.bars = new Image[6];
            for (int i = 0; i < 3; i++)
            {
                float tipX = -17f + i * 17f + 9f;       // 갈매기 끝(뾰족한 쪽)의 x - 화살표는 +x(가는 방향)를 본다
                for (int k = 0; k < 2; k++)
                {
                    GameObject bar = new GameObject("Bar" + i + (k == 0 ? "a" : "b"));
                    bar.transform.SetParent(go.transform, false);
                    RectTransform brt = bar.AddComponent<RectTransform>();
                    brt.anchorMin = new Vector2(0.5f, 0.5f); brt.anchorMax = new Vector2(0.5f, 0.5f);
                    brt.pivot = new Vector2(1f, 0.5f);  // 오른쪽 끝 = 갈매기의 뾰족한 끝
                    brt.sizeDelta = new Vector2(24f, 8f);
                    brt.anchoredPosition = new Vector2(tipX, 0f);
                    brt.localRotation = Quaternion.Euler(0f, 0f, k == 0 ? 40f : -40f);
                    Image img = bar.AddComponent<Image>();
                    img.raycastTarget = false;
                    img.color = CUE_COLOR;
                    // 밝은 모래 바닥에서도 읽히게 어두운 테두리
                    Outline ol = bar.AddComponent<Outline>();
                    ol.effectColor = new Color(0.1f, 0.04f, 0.02f, 0.9f);
                    ol.effectDistance = new Vector2(1.5f, -1.5f);
                    c.bars[i * 2 + k] = img;
                }
            }
            go.SetActive(false);
            cues[n] = c;
        }
    }

    /// <summary>띠 안의 글자 한 줄 (가운데 정렬, 띠 가운데에서 y 만큼)</summary>
    private static Text MakeBandText(Transform parent, string name, string content, int size, Color color, float y, bool bold)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(1400f, size + 16f);
        Text t = go.AddComponent<Text>();
        t.font = KitchenEventManager.GetFont();
        t.fontSize = size;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.color = color;
        t.text = content;
        t.raycastTarget = false;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);
        return t;
    }
}
