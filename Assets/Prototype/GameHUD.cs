using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [GameHUD.cs] v3 - 전투 중 핵심 HUD (전부 코드 생성 - Canvas 세팅 불필요)
/// - 하단 바: 재료 6종 카운트 + 보유 요리 카드 목록 (2줄 그리드, 휠 가로 스크롤)
/// - 요리 카드 클릭 -> 투입 모드 (슬롯 마커 클릭으로 투입)
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

    private Canvas canvas;
    private Text[] matTexts = new Text[6];
    private RectTransform foodListRoot;    // 스크롤 내용물 (카드 부모)
    private Text placingBanner;
    private readonly List<GameObject> foodCards = new List<GameObject>();

    private const float BAR_H = 184f;          // v3: 하단 바 높이 (v2 158)
    private const float FRAME = 28f;           // 파이프 테 두께 (ui_pipe 테두리 = 28px @1080p)
    private const float MAT_W = 330f;          // 재료 구역 폭 (바 왼쪽)
    private const float CARD_W = 116f;
    private const float CARD_H = 56f;
    private const float CARD_GAP = 6f;

    private static readonly string[] MAT_SHORT = { "고기", "등심", "전기", "화염", "얼음", "독" };
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
        // 우클릭 = 투입 모드 취소
        if (!string.IsNullOrEmpty(placingRecipeId) && Input.GetMouseButtonDown(1))
            SetPlacing("");
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
            crt.anchoredPosition = new Vector2(4f + col * 100f, -12f - row * 50f);
            crt.sizeDelta = new Vector2(96f, 34f);

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

            Text label = UIFactory.CreateText(crt, "Label", MAT_SHORT[i] + " 0", 18, UIFactory.CREAM, TextAnchor.MiddleLeft);
            label.rectTransform.offsetMin = new Vector2(26f, 0f);
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

    // ──────────────────────────────────────
    // 갱신
    // ──────────────────────────────────────
    private void RefreshMaterials()
    {
        if (MaterialInventory.Instance == null) return;
        int i = 0;
        foreach (MaterialType t in System.Enum.GetValues(typeof(MaterialType)))
        {
            matTexts[i].text = MAT_SHORT[i] + " " + MaterialInventory.Instance.Get(t);
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
