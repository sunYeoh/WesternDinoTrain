using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// [PauseMenu.cs] v1.6 (v9.12 2026-09-22: 훈련장 창이 떠 있으면 ESC 양보) / v1.5 (v9.11.1 2026-09-22 문구) / v1.4 (v9.11 2026-09-22: 등장 연출 ModalFeel) / v1.3 (v9.10 2026-09-17: 주방 패널(Tab)·정비소(G)가 열려 있으면 ESC 는 그 창을 닫는 용도 - 일시정지 안 열림) / v1.2 (v9.9 2026-09-16: 견습 운행 중엔 "런 포기" 대신 "견습 운행 그만두기", 브리핑 카드 위에선 안 열림) / v1.1 (교수 피드백 A10 반영 2026-09-14) / v1
/// ESC 일시정지 메뉴: 계속하기 / 런 포기(재시작) / 게임 종료
/// - v1.2: TutorialDirector.Active 면 가운데 버튼이 "견습 운행 그만두기" -> TutorialDirector.Quit() (완료 기록 없이 로비)
/// - v1.1: 열람 패널(증강 목록 [V] / 일지 [J])이 열려 있으면 ESC는 그쪽 닫기에 양보
/// 로그라이크 필수 편의 - 망한 런을 빠르게 접고 새 런을 시작할 수 있다.
///
/// 사용법: "GameSystems" 오브젝트에 이 스크립트 추가 (UI는 코드 생성)
/// - 증강 선택/미니게임/합체 선택 중에는 열리지 않는다 (ESC 용도 충돌 방지)
/// - '런 포기'는 씬을 다시 불러온다. 이때 DontDestroyOnLoad로 살아남는
///   구 GameManager를 제거해서 웨이브/골드가 깨끗하게 초기화되도록 한다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance;

    /// <summary>일시정지 메뉴가 열려 있는지 (다른 시스템 입력 차단용)</summary>
    public static bool IsOpen
    {
        get { return Instance != null && Instance.isOpen; }
    }

    private bool isOpen;
    private Canvas canvas;
    private RectTransform root;

    void Awake()
    {
        Instance = this;
        BuildUI();
        root.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (isOpen)
        {
            Close();
            return;
        }

        // ESC 용도가 겹치는 상황에서는 열지 않는다
        if (AugmentPickUI.IsOpen) return;        // 증강 선택 중
        if (CookingMinigame.IsActive) return;    // 미니게임 중
        // B-1: 같은 프레임에 조리 중단(ESC)이 이미 소비된 경우 - 일시정지로 새지 않게
        if (CookingMinigame.EscConsumedFrame == Time.frameCount) return;
        if (WorkshopUI.IsOpen) return;           // v1.3: 정비소는 G 또는 ESC 로 닫음 (WorkshopUI 가 처리)
        if (KitchenPanel.IsOpenStatic) return;   // v1.3: 주방 패널은 Tab 또는 ESC 로 닫음 (KitchenPanel 이 처리)
        if (SlotMarkerUI.MergeSelecting) return; // 합체 선택 취소가 우선
        if (BranchRouteUI.IsOpen) return;        // 분기 선로 선택 중
        if (FinalOrderUI.QteOpen) return;        // C-2: 마지막 주문 QTE 중 (시간정지 충돌 방지)
        if (InfusingMinigame.IsActive) return;   // P1: 인퓨징 중 (ESC = 인퓨징 취소가 우선)
        if (SpinoBetUI.IsOpen) return;           // Phase 2-1: 스피노 베팅 중 (ESC = 거절이 우선)
        if (MerchantUI.IsOpen) return;           // Phase 2-3: 행상인 안킬로 응대 중 (ESC = 떠나기가 우선)
        if (AugmentListUI.ReadingOpen) return;   // A10: 증강 목록[V]/일지[J] 열람 중 (ESC = 열람 닫기가 우선). v9.9: 브리핑 카드도 포함
        if (TrainingGroundUI.IsOpen) return;     // v9.12: 로비 훈련장 목록 (ESC = 닫기가 우선)

        Open();
    }

    public void Open()
    {
        isOpen = true;
        root.gameObject.SetActive(true);
        ModalFeel.Play(root);   // v1.4: 등장 연출
        Time.timeScale = 0f;
        // v1.2: 견습 운행 중이면 가운데 버튼 글자를 바꾼다
        if (giveUpLabel != null)
            giveUpLabel.text = TutorialDirector.Active ? "견습 운행 그만두기" : "이번 운행 포기 (다시 시작)";
    }

    private Text giveUpLabel;   // v1.2: 가운데 버튼 글자 (런 포기 / 견습 운행 그만두기)

    public void Close()
    {
        isOpen = false;
        root.gameObject.SetActive(false);

        // 다른 일시정지 UI가 없을 때만 시간 재개 (A10: 열람 패널도 시간을 잡는다)
        if (!AugmentPickUI.IsOpen && !WorkshopUI.IsOpen && !AugmentListUI.ReadingOpen)
            Time.timeScale = 1f;
    }

    /// <summary>런 포기 - 씬 재시작 (새 런). v1.2: 견습 운행 중이면 디렉터의 그만두기 (기록 없이 로비)</summary>
    private void GiveUpRun()
    {
        if (TutorialDirector.Active)
        {
            isOpen = false;
            root.gameObject.SetActive(false);
            TutorialDirector.Quit();   // 안에서 timeScale 1 + GameManager.EndTutorial (씬 리로드)
            return;
        }

        Time.timeScale = 1f;

        // DontDestroyOnLoad로 살아남는 구 GameManager 제거
        // (남겨두면 웨이브/골드가 이전 런 값으로 이어지는 버그)
        if (GameManager.Instance != null)
            Destroy(GameManager.Instance.gameObject);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void QuitGame()
    {
        Debug.Log("[PauseMenu] 게임 종료");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ─────────────────────────────────────────────
    // UI 생성 (KitchenEventManager 헬퍼 재사용)
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("PauseCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 700;   // 모든 UI보다 위
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // 전체 암전 (뒤 클릭 차단)
        root = KitchenEventManager.MakeBox(canvasGo.transform, "PauseDim", new Color(0f, 0f, 0f, 0.8f));
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        // 본체 패널
        RectTransform body = KitchenEventManager.MakeBox(root, "Body", new Color(0.12f, 0.10f, 0.08f, 0.98f));
        body.anchorMin = new Vector2(0.5f, 0.5f);
        body.anchorMax = new Vector2(0.5f, 0.5f);
        body.anchoredPosition = Vector2.zero;
        body.sizeDelta = new Vector2(420f, 380f);

        Text title = KitchenEventManager.MakeText(body, "Title", "일시 정지", 30, new Color(1f, 0.85f, 0.4f));
        RectTransform tRt = title.rectTransform;
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -24f);
        tRt.sizeDelta = new Vector2(0f, 40f);

        // 버튼 3개
        Button resumeBtn = KitchenEventManager.MakeButton(body, "계속하기 (ESC)",
            new Color(0.25f, 0.42f, 0.25f, 1f), new Vector2(0f, 20f), new Vector2(320f, 60f));
        resumeBtn.onClick.AddListener(delegate { Close(); });

        Button giveUpBtn = KitchenEventManager.MakeButton(body, "이번 운행 포기 (다시 시작)",
            new Color(0.45f, 0.32f, 0.18f, 1f), new Vector2(0f, -60f), new Vector2(320f, 60f));
        giveUpBtn.onClick.AddListener(delegate { GiveUpRun(); });
        giveUpLabel = giveUpBtn.GetComponentInChildren<Text>();   // v1.2: 글자 교체용

        Button quitBtn = KitchenEventManager.MakeButton(body, "게임 종료",
            new Color(0.45f, 0.22f, 0.18f, 1f), new Vector2(0f, -140f), new Vector2(320f, 60f));
        quitBtn.onClick.AddListener(delegate { QuitGame(); });
    }
}
