using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [JournalViewerUI.cs] v1 (신규 파일) - P1: 선대의 일지 열람 패널 (감사 4-B)
///
/// 배경: 일지 12장을 수집은 하는데 다시 볼 곳이 없었다 - 수집형 메타의 보상 화면.
/// [J] 키로 언제든 열고 닫는다. 수집한 장은 전문 재열람, 미수집 장은 ??? 로 표시되어
/// "몇 장이 남았는지"가 곧 수집 동기가 된다. (추후 로비 개편 때 로비 진입점 추가 예정)
///
/// 사용법: 없음! 파일만 넣으면 게임 시작 시 스스로 준비된다 (씬 작업 0).
/// 게임은 멈추지 않는다 (V 증강 목록과 동일한 가벼운 열람 패널).
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class JournalViewerUI : MonoBehaviour
{
    /// <summary>패널 열림 여부 (외부 참조용)</summary>
    public static bool IsOpen { get; private set; }

    private static JournalViewerUI instance;

    private GameObject canvasGo;
    private RectTransform panel;
    private Text titleText;
    private Text bodyText;
    private Text[] entryLabels;   // 12장 목록 라벨
    private bool built = false;

    // ─────────────────────────────────────────────
    // 자동 부트스트랩 (씬 리로드에도 유지)
    // ─────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("JournalViewer");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<JournalViewerUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (IsOpen) Close();
            else Open();
        }
    }

    // ─────────────────────────────────────────────
    // 열기/닫기
    // ─────────────────────────────────────────────
    private void Open()
    {
        if (!built) BuildUI();
        RefreshList();

        bodyText.text = "낡은 가죽 표지가 손에 익는다.\n\n왼쪽에서 장을 선택하라.";
        titleText.text = "선대의 일지  (" + MetaProgress.CollectedJournalCount + "/"
            + StoryTexts.JournalCount + ")   [J] 닫기";

        canvasGo.SetActive(true);
        IsOpen = true;
        SoundManager.Play("sfx_ui_click");
    }

    private void Close()
    {
        if (canvasGo != null) canvasGo.SetActive(false);
        IsOpen = false;
    }

    // ─────────────────────────────────────────────
    // 목록 갱신 (수집 상태 반영)
    // ─────────────────────────────────────────────
    private void RefreshList()
    {
        for (int n = 1; n <= StoryTexts.JournalCount; n++)
        {
            bool has = MetaProgress.IsJournalCollected(n);
            entryLabels[n - 1].text = has ? ("제 " + n + " 장") : (n + " ...???");
            entryLabels[n - 1].color = has
                ? new Color(0.95f, 0.88f, 0.7f)
                : new Color(0.45f, 0.42f, 0.38f);
        }
    }

    private void OnEntryClick(int number)
    {
        if (MetaProgress.IsJournalCollected(number))
        {
            bodyText.text = "선대의 일지  #" + number + "\n\n"
                + StoryTexts.GetJournalText(number)
                + "\n\n- 서명은 불탄 자국뿐이다";
        }
        else
        {
            bodyText.text = "아직 찾지 못한 장이다.\n\n분기 선로 [폐역]에 기록이 잠들어 있다.";
        }
        SoundManager.Play("sfx_ui_click");
    }

    // ─────────────────────────────────────────────
    // UI 생성 (최초 열람 시 1회)
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        built = true;

        canvasGo = new GameObject("JournalCanvas");
        canvasGo.transform.SetParent(transform, false);   // 호스트와 함께 씬 전환 생존
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 570;   // 명성상점(560) 위, 증강목록(585) 아래
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        panel = KitchenEventManager.MakeBox(canvasGo.transform, "Panel",
            new Color(0.09f, 0.07f, 0.05f, 0.96f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(780f, 540f);

        titleText = KitchenEventManager.MakeText(panel, "Title", "", 24,
            new Color(1f, 0.8f, 0.35f));
        RectTransform tRt = titleText.rectTransform;
        tRt.anchorMin = new Vector2(0f, 1f); tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -12f);
        tRt.sizeDelta = new Vector2(-24f, 32f);

        // 좌측: 12장 목록 (버튼)
        entryLabels = new Text[StoryTexts.JournalCount];
        for (int n = 1; n <= StoryTexts.JournalCount; n++)
        {
            int captured = n;   // 클로저 캡처용

            RectTransform row = KitchenEventManager.MakeBox(panel, "Entry" + n,
                new Color(0.16f, 0.12f, 0.08f, 0.9f));
            row.anchorMin = new Vector2(0f, 1f); row.anchorMax = new Vector2(0f, 1f);
            row.pivot = new Vector2(0f, 1f);
            row.anchoredPosition = new Vector2(16f, -54f - (n - 1) * 38f);
            row.sizeDelta = new Vector2(180f, 34f);

            Button btn = row.gameObject.AddComponent<Button>();
            btn.targetGraphic = row.GetComponent<Image>();
            btn.onClick.AddListener(delegate { OnEntryClick(captured); });

            Text label = KitchenEventManager.MakeText(row, "Label", "", 17, Color.white);
            RectTransform lRt = label.rectTransform;
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(10f, 0f);
            lRt.offsetMax = Vector2.zero;
            label.alignment = TextAnchor.MiddleLeft;
            entryLabels[n - 1] = label;
        }

        // 우측: 본문 (불탄 종이 느낌의 어두운 배경판)
        RectTransform bodyBox = KitchenEventManager.MakeBox(panel, "BodyBox",
            new Color(0.13f, 0.10f, 0.07f, 0.95f));
        bodyBox.anchorMin = new Vector2(0f, 0f); bodyBox.anchorMax = new Vector2(1f, 1f);
        bodyBox.pivot = new Vector2(0.5f, 0.5f);
        bodyBox.offsetMin = new Vector2(212f, 16f);
        bodyBox.offsetMax = new Vector2(-16f, -54f);

        bodyText = KitchenEventManager.MakeText(bodyBox, "Body", "", 19,
            new Color(0.92f, 0.87f, 0.78f));
        RectTransform bRt = bodyText.rectTransform;
        bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
        bRt.offsetMin = new Vector2(18f, 14f);
        bRt.offsetMax = new Vector2(-18f, -14f);
        bodyText.alignment = TextAnchor.UpperLeft;

        canvasGo.SetActive(false);
    }
}
