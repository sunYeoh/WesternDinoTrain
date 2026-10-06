using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [WarningFX.cs] v1.1 (v9.18 2026-10-06: BossIntro - 보스 등장 띠. 화면을 가로지르는 어두운 띠 + "보스 등장" + 큰 이름 + 한 줄, 가장자리 붉은 맥동 2회. 실시간으로 돌고 게임은 멈추지 않는다) / v1 (신규 파일) - 감사 2-D 결정 사항
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

    // ─────────────────────────────────────────────
    // 공개 API
    // ─────────────────────────────────────────────

    /// <summary>붉은 경고 플래시 + 중앙 대형 텍스트. duration 동안 2회 맥동 후 사라진다.</summary>
    public static void Flash(string message, float duration)
    {
        Flash(message, duration, new Color(1f, 0.15f, 0.1f));
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

        SetActiveAll(false);
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
