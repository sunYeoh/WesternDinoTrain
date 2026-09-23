using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TrainingGroundUI.cs] v1 (신규, v9.12 2026-09-22) - 로비 [T] 훈련장: 견습 운행을 구간별로 다시 연습하는 목록 창 (목업 v4.2 (A))
///
/// 처음 실행(구간 기록이 하나도 없음)에는 [T] 가 이 창을 열지 않고 견습 운행 전부(1~7)를 바로 연다 (LobbyUI). 한 번이라도 돌린 뒤엔 이 목록.
/// 창: 어둡게 + 가운데 카드 680x600 - 명판 "훈련장  -  구간별 연습" / 안내 한 줄 + 오른쪽 "완료 n / 7" / 구간 7줄(번호·배울 행동·한 줄·단계·예상·기록) /
///     바닥 "[1~7] 고르기   [Enter] 시작   [0] 처음부터 전부   [ESC] 닫기". 기록: 완료 = 초록 / 건너뜀 = 주황 / 아직 = 회색. 첫 미완료 구간이 기본 선택.
/// 조작: [1~7] 고르기, [Enter] 고른 구간 시작, [0] 전부, [ESC]·[T] 닫기, 마우스 = 줄 클릭(고르기) - 고른 줄을 다시 클릭하면 시작.
/// 시작 = TutorialDirector.Begin(seg) (로비 상태에서만). 창은 로비를 떠나면 닫힌다.
/// 캔버스 575 (로비 555·명성 상점 560 위, 증강 600 아래). IsOpen 동안 LobbyUI 의 [Enter] 출발과 PauseMenu 의 ESC 는 양보한다.
///
/// 사용법: 파일만 넣으면 자동 생성. LobbyUI 가 TrainingGroundUI.Toggle() 을 부른다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class TrainingGroundUI : MonoBehaviour
{
    public static TrainingGroundUI Instance { get; private set; }

    /// <summary>창이 떠 있는가 (LobbyUI Enter 출발 / PauseMenu ESC 가 본다)</summary>
    public static bool IsOpen { get; private set; }

    private const int SORT = 575;
    private const float PW = 680f, PH = 600f;
    private const float ROW_H = 62f, ROW_GAP = 6f;

    private static readonly Color GREEN = new Color(0.45f, 0.85f, 0.45f, 1f);
    private static readonly Color GREEN_BG = new Color(0.09f, 0.18f, 0.11f, 1f);
    private static readonly Color ORANGE = new Color(0.95f, 0.62f, 0.25f, 1f);
    private static readonly Color ORANGE_BG = new Color(0.22f, 0.14f, 0.06f, 1f);
    private static readonly Color GREY = new Color(0.5f, 0.47f, 0.42f, 1f);
    private static readonly Color RING_DIM = new Color(0.35f, 0.31f, 0.26f, 1f);
    private static readonly Color CHIP_BG = new Color(0.118f, 0.086f, 0.063f, 1f);

    private Canvas canvas;
    private GameObject root;
    private Text doneCountText;
    private readonly RectTransform[] rows = new RectTransform[TutorialDirector.SEGMENTS];
    private readonly Image[] rowRings = new Image[TutorialDirector.SEGMENTS];
    private readonly Text[] rowNo = new Text[TutorialDirector.SEGMENTS];
    private readonly Text[] rowTitle = new Text[TutorialDirector.SEGMENTS];
    private readonly Text[] rowLine = new Text[TutorialDirector.SEGMENTS];
    private readonly Image[] rowStateBox = new Image[TutorialDirector.SEGMENTS];   // 바깥 테 (기록 색)
    private readonly Image[] rowStateBg = new Image[TutorialDirector.SEGMENTS];    // 안쪽 바탕 (완료 = 어두운 초록 등)
    private readonly Text[] rowState = new Text[TutorialDirector.SEGMENTS];
    private int selected = 1;
    private float openedAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("TrainingGroundUI");
        DontDestroyOnLoad(go);
        go.AddComponent<TrainingGroundUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        IsOpen = false;
        BuildUI();
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; IsOpen = false; }
    }

    // ─────────────────────────────────────────────
    // 정적 API
    // ─────────────────────────────────────────────
    public static void Toggle()
    {
        if (Instance == null) return;
        if (IsOpen) Instance.Close(); else Instance.Open();
    }

    public static void CloseStatic()
    {
        if (Instance != null && IsOpen) Instance.Close();
    }

    private void Open()
    {
        if (!GameBalance.TutorialRunEnabled) return;
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Lobby) return;
        IsOpen = true;
        openedAt = Time.unscaledTime;
        // 기본 선택 = 첫 미완료(0·2) 구간, 전부 완료면 1
        selected = 1;
        for (int i = 1; i <= TutorialDirector.SEGMENTS; i++)
            if (TutorialDirector.SegmentState(i) != 1) { selected = i; break; }
        Refresh();
        root.SetActive(true);
        ModalFeel.Play(root.transform);
        SoundManager.Play("sfx_ui_open");   // 클립 없으면 무시
    }

    private void Close()
    {
        IsOpen = false;
        if (root != null) root.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen) return;
        bool lobby = GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Lobby;
        if (!lobby) { Close(); return; }
        if (Time.unscaledTime - openedAt < 0.15f) return;   // 연 키가 바로 닫지 않게

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.T)) { Close(); return; }
        for (int i = 1; i <= TutorialDirector.SEGMENTS; i++)
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i)) { selected = i; Refresh(); }
        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)) { StartSegment(0); return; }
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            && !BriefingUI.IsOpen && BriefingUI.KeyConsumedFrame != Time.frameCount)
            StartSegment(selected);
    }

    /// <summary>구간 시작 (0 = 전부). 창을 닫고 디렉터에 넘긴다</summary>
    private void StartSegment(int seg)
    {
        Close();
        Debug.Log("[TrainingGround] 시작 - 구간 " + (seg == 0 ? "전부" : seg.ToString()));
        TutorialDirector.Begin(seg);
    }

    private void OnRowClick(int seg)
    {
        if (!IsOpen) return;
        if (selected == seg) { StartSegment(seg); return; }
        selected = seg;
        Refresh();
    }

    // ─────────────────────────────────────────────
    // 표시 갱신 (기록·선택)
    // ─────────────────────────────────────────────
    private void Refresh()
    {
        if (doneCountText != null) doneCountText.text = "완료 " + TutorialDirector.CompletedCount() + " / " + TutorialDirector.SEGMENTS;
        for (int i = 0; i < TutorialDirector.SEGMENTS; i++)
        {
            int seg = i + 1;
            bool sel = seg == selected;
            SetRing(rowRings[i], sel ? UIFactory.GOLD : RING_DIM);
            if (rowNo[i] != null) rowNo[i].color = sel ? UIFactory.GOLD : UIFactory.CREAM;
            if (rowTitle[i] != null) rowTitle[i].color = sel ? UIFactory.GOLD : UIFactory.CREAM;
            if (rowLine[i] != null) rowLine[i].color = sel ? UIFactory.CREAM : UIFactory.DIM;

            int state = TutorialDirector.SegmentState(seg);
            string label = state == 1 ? "완료" : state == 2 ? "건너뜀" : "아직";
            Color fg = state == 1 ? GREEN : state == 2 ? ORANGE : GREY;
            Color bg = state == 1 ? GREEN_BG : state == 2 ? ORANGE_BG : new Color(0f, 0f, 0f, 0f);
            if (rowState[i] != null) { rowState[i].text = label; rowState[i].color = fg; }
            SetRing(rowStateBox[i], fg);
            if (rowStateBg[i] != null) rowStateBg[i].color = bg;
        }
    }

    private static void SetRing(Image img, Color c)
    {
        if (img == null) return;
        if (UISkin.Available) UISkin.Ring(img, c); else img.color = c;
    }

    // ─────────────────────────────────────────────
    // UI 생성 (코드 생성 - 씬 작업 0)
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        canvas = UIFactory.CreateCanvas("TrainingGround_Canvas", SORT);
        canvas.transform.SetParent(transform, false);

        root = new GameObject("Root");
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero; rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero; rootRt.offsetMax = Vector2.zero;

        // 어둡게 (클릭 차단)
        GameObject dimGo = new GameObject("Dim");
        dimGo.transform.SetParent(root.transform, false);
        RectTransform dimRt = dimGo.AddComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero; dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero; dimRt.offsetMax = Vector2.zero;
        Image dim = dimGo.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        dim.raycastTarget = true;

        // 가운데 카드 680x600 (화면 가운데에서 20 위)
        RectTransform panel = UIFactory.CreatePanel(root.transform, "Panel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-PW * 0.5f, -PH * 0.5f + 20f), new Vector2(PW * 0.5f, PH * 0.5f + 20f),
            UIFactory.PANEL, UIFactory.GOLD, 2f);

        if (UISkin.Available)
            UISkin.Nameplate(panel, "Title", "훈련장  -  구간별 연습", 17, new Vector2(0f, 1f), new Vector2(26f, 4f));
        else
        {
            Text t = UIFactory.CreateText(panel, "Title", "훈련장  -  구간별 연습", 18, UIFactory.GOLD, TextAnchor.MiddleLeft);
            PlaceTopLeft(t.rectTransform, 26f, -8f, 400f, 26f);
        }

        Text guide = UIFactory.CreateText(panel, "Guide", "연습할 구간을 골라라. 끝나면 로비로 돌아온다.", 15, UIFactory.CREAM, TextAnchor.MiddleLeft);
        PlaceTopLeft(guide.rectTransform, 26f, -38f, 460f, 24f);
        doneCountText = UIFactory.CreateText(panel, "DoneCount", "", 15, UIFactory.GOLD, TextAnchor.MiddleRight);
        PlaceTopLeft(doneCountText.rectTransform, PW - 26f - 160f, -38f, 160f, 24f);

        float ry = -78f;
        for (int i = 0; i < TutorialDirector.SEGMENTS; i++)
        {
            TutorialDirector.SegmentInfo info = TutorialDirector.SEGMENT_TABLE[i];
            RectTransform row = UIFactory.CreatePanel(panel, "Row_" + info.no,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(22f, ry - ROW_H), new Vector2(PW - 22f, ry),
                UIFactory.PANEL, RING_DIM, 2f);
            rows[i] = row;
            rowRings[i] = row.GetComponent<Image>();

            // 줄 전체가 버튼 (투명) - 클릭 = 고르기 / 고른 줄 다시 클릭 = 시작
            Button btn = row.gameObject.AddComponent<Button>();
            Image rowImg = rowRings[i];
            rowImg.raycastTarget = true;
            btn.targetGraphic = rowImg;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            cb.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            btn.colors = cb;
            int segCaptured = info.no;
            btn.onClick.AddListener(delegate { OnRowClick(segCaptured); });

            // 번호 칩
            RectTransform chip = UIFactory.CreatePanel(row, "Chip", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(12f, -15f - 32f), new Vector2(12f + 40f, -15f), CHIP_BG, CHIP_BG, 0f);
            rowNo[i] = UIFactory.CreateText(chip, "No", info.no.ToString(), 15, UIFactory.CREAM, TextAnchor.MiddleCenter);

            rowTitle[i] = UIFactory.CreateText(row, "Title", info.title, 18, UIFactory.CREAM, TextAnchor.MiddleLeft);
            PlaceTopLeft(rowTitle[i].rectTransform, 68f, -10f, 360f, 24f);
            rowLine[i] = UIFactory.CreateText(row, "Line", info.line, 14, UIFactory.DIM, TextAnchor.MiddleLeft);
            PlaceTopLeft(rowLine[i].rectTransform, 68f, -34f, 400f, 20f);

            Text stepT = UIFactory.CreateText(row, "Step", info.stepLabel, 13, UIFactory.DIM, TextAnchor.MiddleRight);
            PlaceTopLeft(stepT.rectTransform, PW - 44f - 128f - 110f, -10f, 110f, 22f);
            Text estT = UIFactory.CreateText(row, "Est", info.estimate, 13, UIFactory.DIM, TextAnchor.MiddleRight);
            PlaceTopLeft(estT.rectTransform, PW - 44f - 128f - 110f, -33f, 110f, 22f);

            // 기록 상자 88x28 (오른쪽)
            RectTransform box = UIFactory.CreatePanel(row, "State", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-22f - 88f, -17f - 28f), new Vector2(-22f, -17f), new Color(0f, 0f, 0f, 0f), GREY, 1f);
            rowStateBox[i] = box.GetComponent<Image>();
            Transform bgT = box.Find("BG");
            if (bgT != null) { rowStateBg[i] = bgT.GetComponent<Image>(); if (rowStateBg[i] != null) rowStateBg[i].color = new Color(0f, 0f, 0f, 0f); }
            rowState[i] = UIFactory.CreateText(box, "Label", "아직", 14, GREY, TextAnchor.MiddleCenter);

            ry -= ROW_H + ROW_GAP;
        }

        Text keys = UIFactory.CreateText(panel, "Keys", "[1~7] 고르기   [Enter] 시작   [0] 처음부터 전부   [ESC] 닫기", 14, UIFactory.DIM, TextAnchor.MiddleLeft);
        keys.rectTransform.anchorMin = new Vector2(0f, 0f); keys.rectTransform.anchorMax = new Vector2(0f, 0f);
        keys.rectTransform.pivot = new Vector2(0f, 0f);
        keys.rectTransform.anchoredPosition = new Vector2(26f, 18f); keys.rectTransform.sizeDelta = new Vector2(600f, 22f);

        root.SetActive(false);
    }

    private static void PlaceTopLeft(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }
}
