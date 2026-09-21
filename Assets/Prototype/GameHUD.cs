using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [GameHUD.cs] v3.5 (v9.11 2026-09-22 타격감: 조리 완료 접시 날아가기 + 카드 튀기 / 요리 카드 ButtonFeel) / v3.4 (v9.10.1 2026-09-21: 재료 이름 MaterialNames 한 곳(전기알·화염꽃·얼음꽃·독샘) - 칸 폭 102 에 세 글자 이름이 안 들어가 이름(11px, 위)·개수(20px, 아래) 두 줄) / v3.3 (v9.9 2026-09-16: 로비에서는 하단 바 숨김 - 로비 버튼이 바 위에 겹쳐 있던 것) / v3.2 (v9.8 재료 아이콘) / v3.1 (교수 피드백 A9 반영 2026-09-14) / v3 - 전투 중 핵심 HUD (전부 코드 생성 - Canvas 세팅 불필요)
/// - v3.2: 재료 칸의 16px 계열색 판을 ui_mat_*.png 아이콘(32px)으로. 칸 폭 96 -> 102, 간격 100 -> 106 (3열 318 <= 재료 구역 326).
///   PNG 가 없으면 v3.1 그대로(계열색 판 + 글자). 이벤트 "재료 흘림" 칩과 같은 그림이라 재료 = 한 그림으로 통일
/// - 하단 바: 재료 6종 카운트 + 보유 요리 카드 목록 (2줄 그리드, 휠 가로 스크롤)
/// - 요리 카드 클릭 -> 투입 모드 (슬롯 마커 클릭으로 투입)
/// - v3.1 (A9): 하단 바 오른쪽 위 파이프에 칼/팬 상태 칩 2개 (명판). 마모가 콘솔에만 찍혀
///   "판정이 왜 좁아졌는지" 알 수 없던 문제 - 0.25초마다 갱신, 60% 이하 호박색, 30% 이하 빨강 + "!".
///   GameBalance.ToolWearEnabled 가 false(마모 off 실험)면 칩을 숨긴다
/// - v3 변경점 (2026-09-07, "쇳냄새" 픽셀 스킨 - HUD 목업 v3 컨펌):
///   1) 하단 바 158 -> 184: 구리 파이프 프레임(테 28px) 안에 재료 2x3 + 요리 카드 2줄이 들어가도록
///   2) "재료"/"요리" 제목을 파이프 위에 걸린 황동 명판으로 (안쪽 높이 절약)
///   3) 요리 카드 = 무쇠 평판 + 계열색 테(리벳), 선택 중이면 황동 테. 재료 칸 = 계열색 판 + 글자
///   4) 오른쪽 끝 장식: 압력 게이지 + 밸브 휠 + 배기 그릴
///   5) 투입 모드 배너: 평판 + 황동 테 카드(880x72) + 위험 스트라이프, 글자 좌우 84·상하 10 여백 (테두리에 붙지 않게)
///   UISkin(ui_*.png)이 없으면 v2 단색 박스 배치로 자동 폴백 (수치만 다름)
/// GameSystems 오브젝트에 부착
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    // 투입 모드: 선택된 요리 recipeId ("" = 모드 아님)
    public string placingRecipeId = "";

    /// <summary>
    /// v3.1: 우클릭으로 투입 모드를 취소한 시각 (unscaled). 이 Update는 버튼을 누르는 순간 취소하고
    /// 슬롯 마커의 클릭 이벤트는 버튼을 뗄 때 오므로, 마커 쪽은 이 시각 직후의 우클릭을 "취소"로 취급한다
    /// (취소하려던 우클릭이 커서 밑 포탑의 폐기 예고로 새는 것을 막는다)
    /// </summary>
    public static float LastPlacingCancelTime = -10f;

    private Canvas canvas;
    private RectTransform bottomBarRt;     // v3.3: 로비에서 숨기기 위해 보관
    private Text[] matTexts = new Text[6];
    private RectTransform foodListRoot;    // 스크롤 내용물 (카드 부모)
    private Text placingBanner;
    private readonly List<GameObject> foodCards = new List<GameObject>();

    // v3.1 (A9): 도구 상태 칩
    private GameObject knifeChipGo, panChipGo;   // 칩 루트 (스킨 = 명판 / 단색 = 글자)
    private Text knifeText, panText;             // 칩 글자
    private ChefController chefRef;              // 마모 수치 출처 (씬에 1명)
    private float toolRefreshAt;                 // 다음 갱신 시각 (unscaled)
    private int lastKnifeShown = -1, lastPanShown = -1;
    private bool chipsVisible = true;
    private const float TOOL_REFRESH_SEC = 0.25f;
    private const float CHIP_W = 92f;            // 칩 명판 폭

    private const float BAR_H = 184f;          // v3: 하단 바 높이 (v2 158)
    private const float FRAME = 28f;           // 파이프 테 두께 (ui_pipe 테두리 = 28px @1080p)
    private const float MAT_W = 330f;          // 재료 구역 폭 (바 왼쪽)
    private const float CARD_W = 116f;
    private const float CARD_H = 56f;
    private const float CARD_GAP = 6f;

    // v3.4: 재료 이름은 MaterialNames.KOR (한 곳). 여기선 이름(작게)·개수(크게) 두 줄로 놓는다
    private Text[] matNameTexts = new Text[6];
    private static readonly FoodTag[] MAT_TAG = { FoodTag.Phys, FoodTag.Def, FoodTag.Elec, FoodTag.Fire, FoodTag.Ice, FoodTag.Poison };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        BuildUI();

        if (FoodStock.Instance != null)
            FoodStock.Instance.OnChanged += RebuildFoodList;
        if (MaterialInventory.Instance != null)
            MaterialInventory.Instance.OnChanged += RefreshMaterials;

        RefreshMaterials();
        RebuildFoodList();
    }

    void Update()
    {
        // v3.3: 로비(대기 화면)에서는 하단 바를 숨긴다 - 로비 UI(출발/[T]/상점 버튼)가 이 자리에 앉는다
        bool lobby = GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Lobby;
        if (bottomBarRt != null && bottomBarRt.gameObject.activeSelf == lobby)
            bottomBarRt.gameObject.SetActive(!lobby);
        if (lobby) return;

        // 우클릭 = 투입 모드 취소
        if (!string.IsNullOrEmpty(placingRecipeId) && Input.GetMouseButtonDown(1))
        {
            LastPlacingCancelTime = Time.unscaledTime;
            SetPlacing("");
        }

        UpdateToolChips();
    }

    // ──────────────────────────────────────
    // UI 생성
    // ──────────────────────────────────────
    private void BuildUI()
    {
        canvas = UIFactory.CreateCanvas("GameHUD_Canvas", 10);
        bool skin = UISkin.Available;
        float inset = skin ? FRAME : 12f;              // 바 안쪽 여백
        float topInset = skin ? FRAME - 4f : 34f;      // 위쪽 여백 (스킨: 명판이 파이프에 걸리므로 제목 줄이 필요 없다)

        // ── 하단 바 (전체 폭) ──
        RectTransform bottomBar = UIFactory.CreatePanel(canvas.transform, "BottomBar",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(6f, 6f), new Vector2(-6f, 6f + BAR_H),
            UIFactory.PANEL, UIFactory.COPPER, 3f);
        bottomBarRt = bottomBar;   // v3.3

        // ── 제목: 스킨이면 파이프 위에 걸린 황동 명판, 아니면 글자 ──
        if (skin)
        {
            UISkin.Nameplate(bottomBar, "Mat", "재료", 17, new Vector2(0f, 1f), new Vector2(34f, 4f), 76f);           // 파이프 위에 4px 걸림 (목업과 동일)
            UISkin.Nameplate(bottomBar, "Food", "요리  (클릭 = 투입,  휠 = 스크롤)", 16, new Vector2(0f, 1f), new Vector2(MAT_W + 56f, 4f));
        }
        else
        {
            Text matTitle = UIFactory.CreateText(bottomBar, "MatTitle", "재료", 18, UIFactory.GOLD, TextAnchor.UpperLeft);
            matTitle.rectTransform.anchorMin = new Vector2(0f, 1f); matTitle.rectTransform.anchorMax = new Vector2(0f, 1f);
            matTitle.rectTransform.pivot = new Vector2(0f, 1f);
            matTitle.rectTransform.anchoredPosition = new Vector2(16f, -8f); matTitle.rectTransform.sizeDelta = new Vector2(200f, 24f);
            Text foodTitle = UIFactory.CreateText(bottomBar, "FoodTitle", "요리 (클릭 = 투입 모드, 휠 = 스크롤)", 18, UIFactory.GOLD, TextAnchor.UpperLeft);
            foodTitle.rectTransform.anchorMin = new Vector2(0f, 1f); foodTitle.rectTransform.anchorMax = new Vector2(0f, 1f);
            foodTitle.rectTransform.pivot = new Vector2(0f, 1f);
            foodTitle.rectTransform.anchoredPosition = new Vector2(MAT_W + 20f, -8f); foodTitle.rectTransform.sizeDelta = new Vector2(600f, 24f);
        }

        // ── 재료 목록 (하단 바 왼쪽): 2행 3열 (계열색 판 + 이름 + 수) ──
        RectTransform matPanel = new GameObject("MatPanel").AddComponent<RectTransform>();
        matPanel.SetParent(bottomBar, false);
        matPanel.anchorMin = new Vector2(0f, 0f);
        matPanel.anchorMax = new Vector2(0f, 1f);
        matPanel.offsetMin = new Vector2(inset + 4f, inset);
        matPanel.offsetMax = new Vector2(inset + MAT_W, -topInset);

        // v3.2: 재료 아이콘(ui_mat_*)이 있으면 32px 그림 + 글자, 없으면 v3.1 계열색 판 + 글자
        bool matIcons = skin && UISkin.MaterialIcon(MaterialType.Meat) != null;
        float cellW = matIcons ? 102f : 96f;
        float cellGap = matIcons ? 106f : 100f;

        for (int i = 0; i < 6; i++)
        {
            int col = i % 3;
            int row = i / 3;

            GameObject cell = new GameObject("Mat_" + i);
            RectTransform crt = cell.AddComponent<RectTransform>();
            crt.SetParent(matPanel, false);
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(0f, 1f);
            crt.pivot = new Vector2(0f, 1f);
            crt.anchoredPosition = new Vector2(4f + col * cellGap, -12f - row * 50f);
            crt.sizeDelta = new Vector2(cellW, 34f);

            float labelX;
            if (matIcons)
            {
                // 재료 아이콘 32px (칸 왼쪽 가운데)
                UISkin.AddMaterialIcon(crt, (MaterialType)i, new Vector2(0f, 0.5f), new Vector2(2f, 0f), 32f);
                labelX = 40f;
            }
            else
            {
                // 계열색 판 (스킨: 틴트 평판, 아니면 색점)
                GameObject dot = new GameObject("Chip");
                RectTransform drt = dot.AddComponent<RectTransform>();
                drt.SetParent(crt, false);
                drt.anchorMin = new Vector2(0f, 0.5f);
                drt.anchorMax = new Vector2(0f, 0.5f);
                drt.anchoredPosition = new Vector2(9f, 0f);
                drt.sizeDelta = new Vector2(16f, 16f);
                Image dotImg = dot.AddComponent<Image>();
                dotImg.color = UIFactory.TagColor(MAT_TAG[i]);
                dotImg.raycastTarget = false;
                if (skin) UISkin.Plate(dotImg, UIFactory.TagColor(MAT_TAG[i]));
                labelX = 26f;
            }

            // v3.4: 위 = 이름 11px (흐리게), 아래 = 개수 20px. 칸 높이 34 안에 두 줄
            Text nameLabel = UIFactory.CreateText(crt, "Name", MaterialNames.KOR[i], 11, UIFactory.DIM, TextAnchor.UpperLeft);
            nameLabel.rectTransform.offsetMin = new Vector2(labelX, 16f);
            nameLabel.rectTransform.offsetMax = new Vector2(0f, 0f);
            matNameTexts[i] = nameLabel;

            Text label = UIFactory.CreateText(crt, "Label", "0", 20, UIFactory.CREAM, TextAnchor.LowerLeft);
            label.rectTransform.offsetMin = new Vector2(labelX, -2f);
            label.rectTransform.offsetMax = new Vector2(0f, -14f);
            matTexts[i] = label;
        }

        // ── 요리 스크롤 영역 (2줄 그리드 + 가로 스크롤) ──
        GameObject scrollGo = new GameObject("FoodScroll");
        RectTransform scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.SetParent(bottomBar, false);
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(inset + MAT_W + 24f, inset - 2f);
        scrollRt.offsetMax = new Vector2(-(inset + (skin ? 150f : 8f)), -topInset + 2f);   // 오른쪽은 장식 자리

        // 스크롤 판정용 투명 이미지 (휠 입력을 받으려면 레이캐스트 대상 필요)
        Image scrollBg = scrollGo.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0.01f);

        ScrollRect sr = scrollGo.AddComponent<ScrollRect>();

        // 뷰포트 (넘치는 카드 잘라냄)
        GameObject viewportGo = new GameObject("Viewport");
        RectTransform viewportRt = viewportGo.AddComponent<RectTransform>();
        viewportRt.SetParent(scrollRt, false);
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        viewportGo.AddComponent<RectMask2D>();

        // 내용물 (카드 부모) - 카드 2줄이 스크롤 영역 세로 가운데에 오도록 남는 높이를 위아래로 나눈다
        float scrollH = BAR_H - (inset - 2f) - (topInset - 2f);
        float gridPadY = Mathf.Max(0f, (scrollH - (CARD_H * 2f + CARD_GAP)) * 0.5f);
        GameObject listGo = new GameObject("FoodList");
        foodListRoot = listGo.AddComponent<RectTransform>();
        foodListRoot.SetParent(viewportRt, false);
        foodListRoot.anchorMin = new Vector2(0f, 0f);
        foodListRoot.anchorMax = new Vector2(0f, 1f);
        foodListRoot.pivot = new Vector2(0f, 0.5f);
        foodListRoot.offsetMin = new Vector2(0f, gridPadY);
        foodListRoot.offsetMax = new Vector2(0f, -gridPadY);

        sr.viewport = viewportRt;
        sr.content = foodListRoot;
        sr.horizontal = true;
        sr.vertical = false;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 35f;
        sr.inertia = true;

        // ── 재료/요리 사이 세로 파이프 구분선 + 오른쪽 끝 장식 (스킨): 압력 게이지 + 밸브 휠 + 배기 그릴 ──
        if (skin)
        {
            GameObject divGo = new GameObject("Divider");
            RectTransform divRt = divGo.AddComponent<RectTransform>();
            divRt.SetParent(bottomBar, false);
            divRt.anchorMin = new Vector2(0f, 0f);
            divRt.anchorMax = new Vector2(0f, 1f);
            divRt.pivot = new Vector2(0f, 1f);
            divRt.offsetMin = new Vector2(inset + MAT_W - 12f, 20f);       // 폭 28, 위아래 파이프에 8px 겹쳐 이어진 것처럼
            divRt.offsetMax = new Vector2(inset + MAT_W + 16f, -20f);
            Image divImg = divGo.AddComponent<Image>();
            divImg.raycastTarget = false;
            UISkin.PipeVertical(divImg);

            UISkin.AddOrnament(bottomBar, "gauge", new Vector2(1f, 1f), new Vector2(-136f, -40f), new Vector2(56f, 56f));
            UISkin.AddOrnament(bottomBar, "valve", new Vector2(1f, 1f), new Vector2(-72f, -48f), new Vector2(48f, 48f));
            UISkin.AddOrnament(bottomBar, "vent", new Vector2(1f, 1f), new Vector2(-124f, -118f), new Vector2(64f, 24f));
        }

        // ── v3.1 (A9): 칼/팬 상태 칩 - 바 오른쪽 위 파이프에 걸린 작은 명판 2개 (장식 위, 겹치지 않음) ──
        if (skin)
        {
            RectTransform panPlate = UISkin.Nameplate(bottomBar, "Pan", "팬 100%", 15,
                new Vector2(1f, 1f), new Vector2(-34f - CHIP_W, 4f), CHIP_W);
            RectTransform knifePlate = UISkin.Nameplate(bottomBar, "Knife", "칼 100%", 15,
                new Vector2(1f, 1f), new Vector2(-34f - CHIP_W * 2f - 6f, 4f), CHIP_W);
            panChipGo = panPlate.gameObject;
            knifeChipGo = knifePlate.gameObject;
            panText = FindLabel(panPlate);
            knifeText = FindLabel(knifePlate);
        }
        else
        {
            knifeText = UIFactory.CreateText(bottomBar, "KnifeChip", "칼 100%", 16, UIFactory.GOLD, TextAnchor.UpperRight);
            knifeText.rectTransform.anchorMin = new Vector2(1f, 1f); knifeText.rectTransform.anchorMax = new Vector2(1f, 1f);
            knifeText.rectTransform.pivot = new Vector2(1f, 1f);
            knifeText.rectTransform.anchoredPosition = new Vector2(-118f, -8f); knifeText.rectTransform.sizeDelta = new Vector2(96f, 24f);
            panText = UIFactory.CreateText(bottomBar, "PanChip", "팬 100%", 16, UIFactory.GOLD, TextAnchor.UpperRight);
            panText.rectTransform.anchorMin = new Vector2(1f, 1f); panText.rectTransform.anchorMax = new Vector2(1f, 1f);
            panText.rectTransform.pivot = new Vector2(1f, 1f);
            panText.rectTransform.anchoredPosition = new Vector2(-16f, -8f); panText.rectTransform.sizeDelta = new Vector2(96f, 24f);
            knifeChipGo = knifeText.gameObject;
            panChipGo = panText.gameObject;
        }

        // ── 투입 모드 안내 배너 (화면 상단 중앙, 웨이브 예고/안내 카드 아래) ──
        //    평판 + 황동 테 카드 (파이프 프레임은 테가 28px 라 72px 배너에선 글자가 파이프에 붙는다)
        //    글자 좌우 84px 안에 위험 스트라이프, 상하 10px 여백
        RectTransform bannerPanel = UIFactory.CreatePanel(canvas.transform, "PlacingBanner",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-440f, -318f), new Vector2(440f, -246f),
            UIFactory.PANEL, UIFactory.GOLD, 2f);
        placingBanner = UIFactory.CreateText(bannerPanel, "Text", "", 20, UIFactory.GOLD, TextAnchor.MiddleCenter);
        placingBanner.rectTransform.offsetMin = new Vector2(84f, 10f);
        placingBanner.rectTransform.offsetMax = new Vector2(-84f, -10f);
        if (skin)
        {
            // 피벗이 왼쪽 위라 y=+8 이 세로 가운데
            UISkin.AddOrnament(bannerPanel, "hazard", new Vector2(0f, 0.5f), new Vector2(16f, 8f), new Vector2(56f, 16f));
            UISkin.AddOrnament(bannerPanel, "hazard", new Vector2(1f, 0.5f), new Vector2(-72f, 8f), new Vector2(56f, 16f));
        }
        bannerPanel.gameObject.SetActive(false);
    }

    /// <summary>명판 안의 글자 컴포넌트 (UISkin.Nameplate 가 "Label" 이름으로 만든다)</summary>
    private static Text FindLabel(RectTransform plate)
    {
        if (plate == null) return null;
        Transform lt = plate.Find("Label");
        return lt != null ? lt.GetComponent<Text>() : null;
    }

    // ──────────────────────────────────────
    // v3.1 (A9): 칼/팬 상태 칩 갱신 (0.25초 간격, 값이 바뀔 때만 글자를 다시 쓴다)
    // ──────────────────────────────────────
    private void UpdateToolChips()
    {
        if (knifeText == null || panText == null) return;
        if (Time.unscaledTime < toolRefreshAt) return;
        toolRefreshAt = Time.unscaledTime + TOOL_REFRESH_SEC;

        // 마모 off 실험(B3) 중에는 의미 없는 100% 칩을 치운다
        bool show = GameBalance.ToolWearEnabled;
        if (show != chipsVisible)
        {
            chipsVisible = show;
            if (knifeChipGo != null) knifeChipGo.SetActive(show);
            if (panChipGo != null) panChipGo.SetActive(show);
        }
        if (!show) return;

        if (chefRef == null) chefRef = FindFirstObjectByType<ChefController>();
        if (chefRef == null) return;

        int knife = Mathf.RoundToInt(chefRef.knifeSharpness);
        int pan = Mathf.RoundToInt(chefRef.panCondition);
        if (knife != lastKnifeShown)
        {
            lastKnifeShown = knife;
            knifeText.text = "칼 " + knife + "%" + (knife <= 30 ? " !" : "");
            knifeText.color = ChipColor(knife);
        }
        if (pan != lastPanShown)
        {
            lastPanShown = pan;
            panText.text = "팬 " + pan + "%" + (pan <= 30 ? " !" : "");
            panText.color = ChipColor(pan);
        }
    }

    /// <summary>칩 글자색: 30% 이하 빨강(경고와 같은 문턱), 60% 이하 호박색, 그 외 기본(명판 = 먹색 / 단색 = 금색)</summary>
    private static Color ChipColor(int percent)
    {
        if (percent <= 30) return UISkin.HP_RED;
        if (percent <= 60) return new Color(0.72f, 0.36f, 0.05f);
        return UISkin.Available ? UISkin.INK : UIFactory.GOLD;
    }

    // ──────────────────────────────────────
    // 갱신
    // ──────────────────────────────────────
    private void RefreshMaterials()
    {
        if (MaterialInventory.Instance == null) return;
        int i = 0;
        foreach (MaterialType t in System.Enum.GetValues(typeof(MaterialType)))
        {
            int have = MaterialInventory.Instance.Get(t);
            matTexts[i].text = have.ToString();
            matTexts[i].color = have > 0 ? UIFactory.CREAM : UIFactory.DIM;   // 0개는 흐리게
            i++;
        }
    }

    private void RebuildFoodList()
    {
        // 기존 카드 제거
        for (int i = 0; i < foodCards.Count; i++)
            Destroy(foodCards[i]);
        foodCards.Clear();

        if (FoodStock.Instance == null) return;

        // 보유 요리 수집 후 정렬: 티어 -> 속성 -> 이름 (같은 계열이 모이게)
        List<KeyValuePair<string, int>> owned = new List<KeyValuePair<string, int>>();
        foreach (KeyValuePair<string, int> kv in FoodStock.Instance.AllStock)
        {
            if (kv.Value <= 0) continue;
            if (RecipeDatabase.Get(kv.Key) == null) continue;
            owned.Add(kv);
        }
        owned.Sort(delegate (KeyValuePair<string, int> a, KeyValuePair<string, int> b)
        {
            RecipeData ra = RecipeDatabase.Get(a.Key);
            RecipeData rb = RecipeDatabase.Get(b.Key);
            if (ra.tier != rb.tier) return ra.tier.CompareTo(rb.tier);
            if (ra.tag != rb.tag) return ((int)ra.tag).CompareTo((int)rb.tag);
            return string.CompareOrdinal(ra.displayName, rb.displayName);
        });

        // 2줄 그리드 배치 (세로 먼저 채우고 오른쪽으로)
        for (int i = 0; i < owned.Count; i++)
        {
            int col = i / 2;
            int row = i % 2;
            RecipeData r = RecipeDatabase.Get(owned[i].Key);
            GameObject card = CreateFoodCard(r, owned[i].Value, col, row);
            foodCards.Add(card);
        }

        // 스크롤 내용물 폭 갱신 (세로는 BuildUI 에서 정한 위아래 여백 유지)
        int cols = (owned.Count + 1) / 2;
        foodListRoot.sizeDelta = new Vector2(cols * (CARD_W + CARD_GAP) + 4f, foodListRoot.sizeDelta.y);

        // v3.5: 방금 조리가 끝난 요리면 접시가 셰프에서 카드로 날아가 카드가 튄다 (스펙 표 B4)
        if (GameBalance.CookFeelOn && FoodStock.LastAddedFrame == Time.frameCount && !string.IsNullOrEmpty(FoodStock.LastAddedId))
            PlayCookArrive(FoodStock.LastAddedId);
    }

    /// <summary>v3.5: 조리 완료 연출 - 셰프 자리(화면)에서 카드로 접시(속성색 원)가 0.35초 날아가고, 도착하면 카드 1.25배 -> 1</summary>
    private void PlayCookArrive(string recipeId)
    {
        RecipeData r = RecipeDatabase.Get(recipeId);
        if (r == null || canvas == null) return;
        Vector2 from = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        ChefController chef = FindFirstObjectByType<ChefController>();
        if (chef != null && Camera.main != null) from = Camera.main.WorldToScreenPoint(chef.transform.position);
        RectTransform card = FindFoodCard(recipeId);
        if (card == null) return;
        string id = recipeId;
        UIFeel.FlyTo(canvas, from, card, TrainDeck.GetCircleSprite(), UIFactory.TagColor(r.tag), 34f, 0.35f, delegate
        {
            RectTransform c = FindFoodCard(id);   // 그 사이 목록이 다시 만들어졌을 수 있다
            if (c != null) UIFeel.Bounce(c, 0.25f, 0.25f);
            SoundManager.Play("sfx_pickup");
        });
    }

    private RectTransform FindFoodCard(string recipeId)
    {
        for (int i = 0; i < foodCards.Count; i++)
            if (foodCards[i] != null && foodCards[i].name == "Food_" + recipeId) return foodCards[i].GetComponent<RectTransform>();
        return null;
    }

    private GameObject CreateFoodCard(RecipeData r, int count, int col, int row)
    {
        string id = r.recipeId;
        bool skin = UISkin.Available;
        Color tagC = UIFactory.TagColor(r.tag);

        // 카드 테 색: 선택 중 = 황동, T2 = 핑크, 그 외 = 계열색 (스킨) / 구리 (단색)
        Color border = (placingRecipeId == id) ? UIFactory.GOLD
            : r.tier == 2 ? UIFactory.T2PINK
            : skin ? tagC : UIFactory.COPPER;

        GameObject cardGo = new GameObject("Food_" + id);
        RectTransform rt = cardGo.AddComponent<RectTransform>();
        rt.SetParent(foodListRoot, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(col * (CARD_W + CARD_GAP), -(row * (CARD_H + CARD_GAP)));
        rt.sizeDelta = new Vector2(CARD_W, CARD_H);

        Image borderImg = cardGo.AddComponent<Image>();
        borderImg.color = border;
        if (skin) UISkin.Ring(borderImg, border);

        Button btn = cardGo.AddComponent<Button>();
        btn.onClick.AddListener(delegate { OnFoodCardClicked(id); });
        ButtonFeel.Attach(btn);   // v3.5: 호버·프레스 반응

        // 내부 배경 (스킨: 무쇠 평판 / 단색: 계열색 어둡게)
        GameObject bg = new GameObject("BG");
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.SetParent(rt, false);
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        float pad = skin ? 8f : 2f;
        bgRt.offsetMin = new Vector2(pad, pad);
        bgRt.offsetMax = new Vector2(-pad, -pad);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(tagC.r * 0.28f, tagC.g * 0.28f, tagC.b * 0.28f, 0.95f);
        bgImg.raycastTarget = false;
        if (skin) UISkin.Plate(bgImg, Color.white);

        // 이름
        Text nameText = UIFactory.CreateText(bgRt, "Name", r.displayName, 14, UIFactory.CREAM, TextAnchor.UpperCenter);
        nameText.rectTransform.offsetMin = new Vector2(2f, 18f);
        nameText.rectTransform.offsetMax = new Vector2(-2f, -3f);
        nameText.horizontalOverflow = HorizontalWrapMode.Wrap;

        // 하단: 티어 + 수량
        string bottomStr = (r.tier == 2 ? "T2  " : "") + "x" + count;
        Text cntText = UIFactory.CreateText(bgRt, "Count", bottomStr, 14,
            r.tier == 2 ? UIFactory.T2PINK : UIFactory.GOLD, TextAnchor.LowerCenter);
        cntText.rectTransform.offsetMin = new Vector2(2f, 2f);
        cntText.rectTransform.offsetMax = new Vector2(-2f, 18f);

        return cardGo;
    }

    // ──────────────────────────────────────
    // 투입 모드
    // ──────────────────────────────────────
    private void OnFoodCardClicked(string recipeId)
    {
        // 같은 카드 다시 클릭 = 취소
        SetPlacing(placingRecipeId == recipeId ? "" : recipeId);
    }

    public void SetPlacing(string recipeId)
    {
        placingRecipeId = recipeId;

        Transform banner = canvas.transform.Find("PlacingBanner");
        if (banner != null)
        {
            bool on = !string.IsNullOrEmpty(recipeId);
            banner.gameObject.SetActive(on);
            if (on)
            {
                RecipeData r = RecipeDatabase.Get(recipeId);
                placingBanner.text = r.displayName + " - 슬롯을 골라서 클릭 (우클릭 취소)";
            }
        }
        RebuildFoodList(); // 선택 테두리 갱신
    }

    /// <summary>슬롯 마커를 클릭했을 때 호출 (SlotMarkerUI에서)</summary>
    public void OnSlotClicked(TurretSlot slot)
    {
        if (string.IsNullOrEmpty(placingRecipeId)) return;

        if (slot.TryInsertFood(placingRecipeId))
        {
            FoodStock.Instance.TryConsume(placingRecipeId, 1);
            // 재고 전부 소진되면 모드 해제
            if (FoodStock.Instance.Get(placingRecipeId) <= 0)
                SetPlacing("");
        }
        else
        {
            Debug.Log("[GameHUD] 이 슬롯에 투입 불가 (잠금 또는 다른 요리 존재)");
        }
    }
}
