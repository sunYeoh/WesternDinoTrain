using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// [PauseMenu.cs] v1
/// ESC 일시정지 메뉴: 계속하기 / 런 포기(재시작) / 게임 종료
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
        if (WorkshopUI.IsOpen) return;           // 정비소는 G로 닫음
        if (SlotMarkerUI.MergeSelecting) return; // 합체 선택 취소가 우선
        if (BranchRouteUI.IsOpen) return;        // 분기 선로 선택 중
        if (FinalOrderUI.QteOpen) return;        // C-2: 마지막 주문 QTE 중 (시간정지 충돌 방지)

        Open();
    }

    public void Open()
    {
        isOpen = true;
        root.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Close()
    {
        isOpen = false;
        root.gameObject.SetActive(false);

        // 다른 일시정지 UI가 없을 때만 시간 재개
        if (!AugmentPickUI.IsOpen && !WorkshopUI.IsOpen)
            Time.timeScale = 1f;
    }

    /// <summary>런 포기 - 씬 재시작 (새 런)</summary>
    private void GiveUpRun()
    {
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

        Button giveUpBtn = KitchenEventManager.MakeButton(body, "런 포기 (다시 시작)",
            new Color(0.45f, 0.32f, 0.18f, 1f), new Vector2(0f, -60f), new Vector2(320f, 60f));
        giveUpBtn.onClick.AddListener(delegate { GiveUpRun(); });

        Button quitBtn = KitchenEventManager.MakeButton(body, "게임 종료",
            new Color(0.45f, 0.22f, 0.18f, 1f), new Vector2(0f, -140f), new Vector2(320f, 60f));
        quitBtn.onClick.AddListener(delegate { QuitGame(); });
    }
}
