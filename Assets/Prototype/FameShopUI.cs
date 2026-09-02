using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// [FameShopUI.cs] v1 (신규 파일)
/// 명성 상점 - 런 사이(로비/게임오버)에 명성을 소모해 영구 업그레이드를 사는 UI.
/// 하데스의 '어둠의 거울' 포지션: 죽어도 명성은 남고, 그걸로 다음 런을 강하게 만든다.
///
/// 사용법:
///  1) 이 파일을 Assets/Prototype 폴더에 넣는다
///  2) 하이어라키의 아무 오브젝트(예: UIManager가 붙은 오브젝트)에 AddComponent
///  3) 씬 배치 필요 없음 - UI는 전부 코드로 생성된다
///
/// 동작:
///  - 로비 / 게임오버 상태에서 자동으로 표시, 전투 시작하면 자동으로 숨김
///  - M 키로 접기/펼치기 (로비, 게임오버에서만)
///  - 업그레이드 효과는 이미 GameManager/TrainManager/CookingMinigame에 연결되어 있어
///    구매 즉시(다음 런부터) 적용된다
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class FameShopUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    // ─────────────────────────────────────────────
    // 상품 정의
    // 가격은 baseCost * (현재레벨 + 1) - 레벨이 오를수록 비싸진다
    // ─────────────────────────────────────────────
    private class ShopItem
    {
        public string id;        // MetaProgress 저장 키
        public string itemName;  // 표시 이름
        public string desc;      // 효과 설명
        public int baseCost;     // 1레벨 가격
        public int maxLevel;     // 최대 레벨

        public ShopItem(string id, string itemName, string desc, int baseCost, int maxLevel)
        {
            this.id = id; this.itemName = itemName; this.desc = desc;
            this.baseCost = baseCost; this.maxLevel = maxLevel;
        }

        public int CostAt(int level) { return baseCost * (level + 1); }
    }

    private ShopItem[] items;

    // ── UI 참조 ──
    private GameObject canvasGo;
    private GameObject root;
    private Text fameText;
    private Text[] levelTexts;
    private Text[] buyLabels;
    private Button[] buyButtons;
    private GameObject restartButtonGo;   // v1.2: [다시 굽는다] - 게임오버/승리 시에만 표시

    // 표시 상태 추적
    // 플레이테스트 픽스: 로비에서는 기본 접힘 - 시작 화면을 가리지 않는다.
    // 로비의 [명성 상점] 버튼이나 M 키로 열어 본다. (사망/승리 시에는 자동으로 펼쳐진다)
    private bool userCollapsed = true;
    private GameManager.GameState lastSeenState = GameManager.GameState.Lobby;

    /// <summary>로비 버튼(LobbyUI)이 접기/펼치기를 호출할 수 있게 공개</summary>
    public static FameShopUI Instance { get; private set; }

    public void ToggleShop() { userCollapsed = !userCollapsed; }

    private void Awake()
    {
        Instance = this;
    }

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────
    private void Start()
    {
        items = new ShopItem[]
        {
            new ShopItem("gold",  "두둑한 전대",   "시작 골드 +100",             80,  3),
            new ShopItem("hp",    "강화 보일러",   "기차 최대 HP +50",           100, 3),
            new ShopItem("food",  "여분의 도시락", "시작 요리 +1 (첫 포탑 가속)", 120, 2),
            new ShopItem("mat",   "재료 가방",     "시작 시 랜덤 재료 +2",        100, 2),
            new ShopItem("judge", "셰프의 감각",   "조리 판정 존 +4% (영구)",     150, 3),
        };

        BuildUI();
        root.SetActive(false);
        IsOpen = false;
    }

    private void OnDestroy()
    {
        if (canvasGo != null) Destroy(canvasGo);
        IsOpen = false;
    }

    // ─────────────────────────────────────────────
    // 표시 조건: 로비 또는 게임오버 상태
    // ─────────────────────────────────────────────
    private void Update()
    {
        bool allowed = false;
        if (GameManager.Instance != null)
        {
            GameManager.GameState s = GameManager.Instance.currentState;
            allowed = (s == GameManager.GameState.Lobby
                || s == GameManager.GameState.GameOver
                || s == GameManager.GameState.Victory);

            // 플레이테스트 픽스: 사망/승리 화면에 들어온 순간엔 자동으로 펼친다
            // (죽은 직후가 명성을 쓸 가장 뜨거운 순간 - 로비 기본 접힘과 별개)
            if (s != lastSeenState)
            {
                if (s == GameManager.GameState.GameOver || s == GameManager.GameState.Victory)
                    userCollapsed = false;
                else if (s == GameManager.GameState.Lobby)
                    userCollapsed = true;
                lastSeenState = s;
            }
        }

        // M 키로 접기/펼치기
        if (allowed && Input.GetKeyDown(KeyCode.M))
            userCollapsed = !userCollapsed;

        bool shouldShow = allowed && !userCollapsed;
        if (root.activeSelf != shouldShow)
        {
            root.SetActive(shouldShow);
            IsOpen = shouldShow;
            if (shouldShow) Refresh();   // 열릴 때마다 명성/가격 갱신
        }

        // v1.2 (감사 3-E): [다시 굽는다] 버튼은 런이 끝났을 때만 (로비에서는 숨김)
        if (restartButtonGo != null && GameManager.Instance != null)
        {
            bool runEnded = GameManager.Instance.currentState == GameManager.GameState.GameOver
                || GameManager.Instance.currentState == GameManager.GameState.Victory;
            bool showRestart = runEnded && !userCollapsed;
            if (restartButtonGo.activeSelf != showRestart)
                restartButtonGo.SetActive(showRestart);
        }
    }

    /// <summary>
    /// v1.2 (감사 3-E): 즉시 재출발 - 죽음이 가장 뜨거운 재도전 욕구의 순간.
    /// PauseMenu의 런 포기와 같은 방식: GameManager 파괴 후 씬 리로드 (DontDestroyOnLoad 잔재 방지)
    /// </summary>
    private void RestartRun()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
            Destroy(GameManager.Instance.gameObject);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ─────────────────────────────────────────────
    // UI 생성 (전부 코드)
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        // 전용 캔버스 (Workshop 550 과 Augment 600 사이)
        canvasGo = new GameObject("FameShopCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 560;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // 반투명 배경 패널 (중앙)
        RectTransform panel = KitchenEventManager.MakeBox(canvasGo.transform, "FameShopPanel",
            new Color(0.08f, 0.06f, 0.05f, 0.94f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = new Vector2(0f, 20f);
        panel.sizeDelta = new Vector2(860f, 600f);
        root = panel.gameObject;

        // 제목
        Text title = KitchenEventManager.MakeText(panel, "Title",
            "명성 상점 - 황야의 전설", 32, new Color(1f, 0.78f, 0.32f));
        SetTopStretch(title.rectTransform, -14f, 40f);

        // 보유 명성
        fameText = KitchenEventManager.MakeText(panel, "Fame", "", 24,
            new Color(0.95f, 0.9f, 0.6f));
        SetTopStretch(fameText.rectTransform, -58f, 30f);

        // 상품 목록
        int count = items.Length;
        levelTexts = new Text[count];
        buyLabels = new Text[count];
        buyButtons = new Button[count];

        float rowY = 130f;
        for (int i = 0; i < count; i++)
        {
            // 클로저 캡처용 지역 변수 (for 변수 직접 캡처 금지)
            int index = i;

            RectTransform row = KitchenEventManager.MakeBox(panel, "Row_" + items[i].id,
                new Color(0.16f, 0.13f, 0.1f, 0.9f));
            row.anchorMin = new Vector2(0.5f, 0.5f);
            row.anchorMax = new Vector2(0.5f, 0.5f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.anchoredPosition = new Vector2(0f, rowY);
            row.sizeDelta = new Vector2(800f, 72f);
            rowY -= 82f;

            // 이름 (좌측 상단)
            Text nameText = KitchenEventManager.MakeText(row, "Name", items[i].itemName, 23,
                new Color(1f, 0.92f, 0.8f));
            nameText.alignment = TextAnchor.MiddleLeft;
            RectTransform nRt = nameText.rectTransform;
            nRt.anchorMin = new Vector2(0f, 0.5f);
            nRt.anchorMax = new Vector2(0f, 0.5f);
            nRt.pivot = new Vector2(0f, 0.5f);
            nRt.anchoredPosition = new Vector2(18f, 14f);
            nRt.sizeDelta = new Vector2(300f, 30f);

            // 설명 (좌측 하단)
            Text descText = KitchenEventManager.MakeText(row, "Desc", items[i].desc, 18,
                new Color(0.75f, 0.72f, 0.65f));
            descText.alignment = TextAnchor.MiddleLeft;
            RectTransform dRt = descText.rectTransform;
            dRt.anchorMin = new Vector2(0f, 0.5f);
            dRt.anchorMax = new Vector2(0f, 0.5f);
            dRt.pivot = new Vector2(0f, 0.5f);
            dRt.anchoredPosition = new Vector2(18f, -14f);
            dRt.sizeDelta = new Vector2(420f, 26f);

            // 레벨 표시 (중앙 우측)
            levelTexts[i] = KitchenEventManager.MakeText(row, "Level", "", 21,
                new Color(0.6f, 0.85f, 0.95f));
            RectTransform lRt = levelTexts[i].rectTransform;
            lRt.anchorMin = new Vector2(1f, 0.5f);
            lRt.anchorMax = new Vector2(1f, 0.5f);
            lRt.pivot = new Vector2(1f, 0.5f);
            lRt.anchoredPosition = new Vector2(-190f, 0f);
            lRt.sizeDelta = new Vector2(120f, 30f);

            // 구매 버튼 (우측)
            buyButtons[i] = KitchenEventManager.MakeButton(row, "구매",
                new Color(0.55f, 0.35f, 0.12f), new Vector2(310f, 0f), new Vector2(150f, 50f));
            buyLabels[i] = buyButtons[i].GetComponentInChildren<Text>();
            buyButtons[i].onClick.AddListener(delegate { OnBuy(index); });
        }

        // v1.2 (감사 3-E): [다시 굽는다] 버튼 - 캔버스 직속 (패널 아래)
        Button restartBtn = KitchenEventManager.MakeButton(canvasGo.transform,
            "다시 굽는다 (즉시 재출발)",
            new Color(0.62f, 0.25f, 0.12f), Vector2.zero, new Vector2(340f, 58f));
        RectTransform rRt = restartBtn.GetComponent<RectTransform>();
        rRt.anchorMin = new Vector2(0.5f, 0.5f);
        rRt.anchorMax = new Vector2(0.5f, 0.5f);
        rRt.pivot = new Vector2(0.5f, 0.5f);
        rRt.anchoredPosition = new Vector2(0f, -330f);   // 상점 패널 바로 아래
        restartBtn.onClick.AddListener(RestartRun);
        restartButtonGo = restartBtn.gameObject;
        restartButtonGo.SetActive(false);

        // 하단 안내
        Text hint = KitchenEventManager.MakeText(panel, "Hint",
            "명성은 웨이브를 클리어할 때마다 쌓이고, 죽어도 잃지 않는다.  [M] 접기/펼치기", 17,
            new Color(0.6f, 0.58f, 0.52f));
        RectTransform hRt = hint.rectTransform;
        hRt.anchorMin = new Vector2(0f, 0f);
        hRt.anchorMax = new Vector2(1f, 0f);
        hRt.pivot = new Vector2(0.5f, 0f);
        hRt.anchoredPosition = new Vector2(0f, 12f);
        hRt.sizeDelta = new Vector2(0f, 26f);
    }

    /// <summary>상단에 가로로 붙는 텍스트 배치 헬퍼</summary>
    private void SetTopStretch(RectTransform rt, float y, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(0f, height);
    }

    // ─────────────────────────────────────────────
    // 구매 처리
    // ─────────────────────────────────────────────
    private void OnBuy(int index)
    {
        ShopItem item = items[index];
        int level = MetaProgress.UpgradeLevel(item.id);
        int cost = item.CostAt(level);

        if (MetaProgress.TryBuyUpgrade(item.id, cost, item.maxLevel))
        {
            UIManager.Instance?.ShowStatChange("[명성 상점] " + item.itemName + " Lv."
                + MetaProgress.UpgradeLevel(item.id) + " 구매!");
        }
        Refresh();
    }

    /// <summary>보유 명성 / 각 상품의 레벨, 가격, 버튼 상태 갱신</summary>
    private void Refresh()
    {
        fameText.text = "보유 명성: " + MetaProgress.Fame
            + "   |   최고 기록: " + MetaProgress.BestWave + "웨이브"
            + "   |   도감: " + MetaProgress.DiscoveredCount + "종";

        for (int i = 0; i < items.Length; i++)
        {
            int level = MetaProgress.UpgradeLevel(items[i].id);
            bool maxed = level >= items[i].maxLevel;
            levelTexts[i].text = "Lv." + level + " / " + items[i].maxLevel;

            if (maxed)
            {
                buyLabels[i].text = "완성";
                buyButtons[i].interactable = false;
            }
            else
            {
                int cost = items[i].CostAt(level);
                buyLabels[i].text = cost + " 명성";
                buyButtons[i].interactable = MetaProgress.Fame >= cost;
            }
        }
    }
}
