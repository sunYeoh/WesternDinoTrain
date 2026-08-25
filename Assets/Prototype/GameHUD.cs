using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [GameHUD.cs] v2
/// 전투 중 핵심 HUD (전부 코드 생성 - Canvas 세팅 불필요)
/// - 하단 바: 재료 6종 카운트 + 보유 요리 카드 목록
/// - 요리 카드 클릭 -> 투입 모드 (슬롯 마커 클릭으로 투입)
/// - v2 변경점 (UI 개선):
///   1) 요리 목록 2줄 그리드 (기존 1줄 -> 화면 밖으로 넘치던 문제 해결)
///   2) 티어 -> 속성 -> 이름 순 정렬 (같은 계열이 모여서 찾기 쉬움)
///   3) 카드가 화면을 넘으면 마우스 휠로 가로 스크롤
///   4) 하단 바 높이 128 -> 176 (2줄 수용)
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

    private const float CARD_W = 112f;
    private const float CARD_H = 64f;
    private const float CARD_GAP = 4f;

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

        // ── 하단 바 (전체 폭, v2: 176px로 확장) ──
        RectTransform bottomBar = UIFactory.CreatePanel(canvas.transform, "BottomBar",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(8f, 8f), new Vector2(-8f, 176f),
            UIFactory.PANEL, UIFactory.COPPER, 3f);

        // ── 재료 목록 패널 (하단 바 왼쪽) ──
        RectTransform matPanel = new GameObject("MatPanel").AddComponent<RectTransform>();
        matPanel.SetParent(bottomBar, false);
        matPanel.anchorMin = new Vector2(0f, 0f);
        matPanel.anchorMax = new Vector2(0f, 1f);
        matPanel.offsetMin = new Vector2(12f, 8f);
        matPanel.offsetMax = new Vector2(320f, -8f);

        Text matTitle = UIFactory.CreateText(matPanel, "Title", "재료", 18, UIFactory.GOLD, TextAnchor.UpperLeft);
        matTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        matTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        matTitle.rectTransform.offsetMin = new Vector2(4f, -26f);
        matTitle.rectTransform.offsetMax = new Vector2(0f, -2f);

        // 재료 6종: 2행 3열 (점 + 이름 + 수)
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
            crt.anchoredPosition = new Vector2(4f + col * 102f, -34f - row * 40f);
            crt.sizeDelta = new Vector2(98f, 34f);

            // 색점
            GameObject dot = new GameObject("Dot");
            RectTransform drt = dot.AddComponent<RectTransform>();
            drt.SetParent(crt, false);
            drt.anchorMin = new Vector2(0f, 0.5f);
            drt.anchorMax = new Vector2(0f, 0.5f);
            drt.anchoredPosition = new Vector2(9f, 0f);
            drt.sizeDelta = new Vector2(14f, 14f);
            dot.AddComponent<Image>().color = UIFactory.TagColor(MAT_TAG[i]);

            Text label = UIFactory.CreateText(crt, "Label", MAT_SHORT[i] + " 0", 17, UIFactory.CREAM, TextAnchor.MiddleLeft);
            label.rectTransform.offsetMin = new Vector2(22f, 0f);
            matTexts[i] = label;
        }

        // ── 요리 리스트 제목 ──
        Text foodTitle = UIFactory.CreateText(bottomBar, "FoodTitle", "요리 (클릭 = 투입 모드, 휠 = 스크롤)", 18, UIFactory.GOLD, TextAnchor.UpperLeft);
        foodTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        foodTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        foodTitle.rectTransform.offsetMin = new Vector2(340f, -30f);
        foodTitle.rectTransform.offsetMax = new Vector2(0f, -6f);

        // ── 요리 스크롤 영역 (v2: 2줄 그리드 + 가로 스크롤) ──
        GameObject scrollGo = new GameObject("FoodScroll");
        RectTransform scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.SetParent(bottomBar, false);
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(340f, 8f);
        scrollRt.offsetMax = new Vector2(-12f, -34f);

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

        // 내용물 (카드 부모)
        GameObject listGo = new GameObject("FoodList");
        foodListRoot = listGo.AddComponent<RectTransform>();
        foodListRoot.SetParent(viewportRt, false);
        foodListRoot.anchorMin = new Vector2(0f, 0f);
        foodListRoot.anchorMax = new Vector2(0f, 1f);
        foodListRoot.pivot = new Vector2(0f, 0.5f);
        foodListRoot.offsetMin = Vector2.zero;
        foodListRoot.offsetMax = Vector2.zero;

        sr.viewport = viewportRt;
        sr.content = foodListRoot;
        sr.horizontal = true;
        sr.vertical = false;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 35f;
        sr.inertia = true;

        // ── 투입 모드 안내 배너 (화면 상단 중앙) ──
        RectTransform bannerPanel = UIFactory.CreatePanel(canvas.transform, "PlacingBanner",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-320f, -96f), new Vector2(320f, -52f),
            UIFactory.PANEL, UIFactory.GOLD, 2f);
        placingBanner = UIFactory.CreateText(bannerPanel, "Text", "", 20, UIFactory.GOLD, TextAnchor.MiddleCenter);
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

        // 스크롤 내용물 폭 갱신
        int cols = (owned.Count + 1) / 2;
        foodListRoot.sizeDelta = new Vector2(cols * (CARD_W + CARD_GAP) + 4f, 0f);
    }

    private GameObject CreateFoodCard(RecipeData r, int count, int col, int row)
    {
        string id = r.recipeId;

        // 카드 (T2는 핑크 테두리)
        Color border = r.tier == 2 ? UIFactory.T2PINK : UIFactory.COPPER;
        GameObject cardGo = new GameObject("Food_" + id);
        RectTransform rt = cardGo.AddComponent<RectTransform>();
        rt.SetParent(foodListRoot, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(col * (CARD_W + CARD_GAP), -(row * (CARD_H + CARD_GAP)));
        rt.sizeDelta = new Vector2(CARD_W, CARD_H);

        Image borderImg = cardGo.AddComponent<Image>();
        borderImg.color = (placingRecipeId == id) ? UIFactory.GOLD : border;

        Button btn = cardGo.AddComponent<Button>();
        btn.onClick.AddListener(delegate { OnFoodCardClicked(id); });

        // 내부 배경 (계열색 어둡게)
        GameObject bg = new GameObject("BG");
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.SetParent(rt, false);
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = new Vector2(2f, 2f);
        bgRt.offsetMax = new Vector2(-2f, -2f);
        Color tagC = UIFactory.TagColor(r.tag);
        bg.AddComponent<Image>().color = new Color(tagC.r * 0.28f, tagC.g * 0.28f, tagC.b * 0.28f, 0.95f);
        bg.GetComponent<Image>().raycastTarget = false;

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
