using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [WarningFX.cs] v1 (신규 파일) - 감사 2-D 결정 사항
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
        scaler.referenceResolution = new Vector2(1920f, 1080f);

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

        SetActiveAll(false);
    }
}
