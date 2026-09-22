using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// [KitchenPanel.cs] v2.6 (v9.11.1 2026-09-22 문구: 기본/전설 요리) / v2.5 (v9.11 2026-09-22: 등장 연출 ModalFeel) / v2.4 (v9.10.1 2026-09-21: 재료 이름 MaterialNames 한 곳 / 안내줄에 정차 조리 남은 횟수(CookingBridge.StopCookHint)) / v2.3 (v9.10 2026-09-17 테스터 피드백: [ESC] 로도 닫힘(단축키로 열고 ESC 로 닫기) / 행상인·베팅·선로 창 중 Tab 금지 / 도감 카드 클릭 = 오른쪽 상세(무엇을 하나·어떤 손님에·언제, RecipeText)) / v2.2 (v9.8 재료 아이콘) / v2.1
/// Tab키 주방 패널 (uGUI 코드 생성) - 조리 / 합성 / 도감 3탭
/// GameSystems 오브젝트에 부착
///
/// - v2.2: 조리 탭 상단 재료 현황 바를 "아이콘 32px + 수량" 칸 6개로 (ui_mat_*.png 있을 때만, 없으면 v2.1 글자 줄). 로직 무변경
///
/// - v2.1 (2026-09-07, "쇳냄새" 픽셀 스킨 - HUD 목업 v3 컨펌): UISkin 이 있을 때만
///   제목 "주 방" 을 파이프 위에 걸린 황동 명판으로, 닫기 힌트를 파이프 아래로, 내용 영역을 파이프 안쪽(30px)으로,
///   오른쪽 위 파이프에 압력 게이지·밸브 장식. 스킨이 없으면 v2 배치 그대로. 로직 변경 없음
///
/// - v2 변경점 (조리법 정체성):
///   1) 모든 레시피에 고유 조리법 부여 (MethodOf: 굽기/볶기/끓이기 자동 판정)
///      - 스프/정식/장판/오라/버프 계열 -> 끓이기
///      - 고기 재료 포함 또는 물리 계열 -> 굽기
///      - 나머지 마법/속성 계열 -> 볶기
///   2) 레시피 카드에 [굽기] 같은 조리법 표기
///   3) 조리법 선택 버튼 3개 제거 -> 레시피 고유 조리법으로 자동 시작
///   4) OpenForStation(method): 조리대(E키)에서 호출 - 해당 조리법 요리만 표시
///
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class KitchenPanel : MonoBehaviour
{
    public static KitchenPanel Instance { get; private set; }

    /// <summary>패널이 열려 있는지 (다른 시스템 입력 차단용)</summary>
    public static bool IsOpenStatic
    {
        get { return Instance != null && Instance.isOpen; }
    }

    private Canvas canvas;
    private RectTransform root;          // 패널 전체
    private RectTransform contentArea;   // 탭 내용 영역
    private bool isOpen = false;
    private int tabIndex = 0;            // 0=조리 1=합성 2=도감
    private int stationFilter = -1;      // -1=전체, 0=굽기 1=볶기 2=끓이기 (조리대에서 열면 설정)

    // 조리 상태
    private string selectedRecipe = "";
    // 합성 상태
    private string fuseA = "";
    private string fuseB = "";

    private readonly List<GameObject> spawned = new List<GameObject>();
    private Text detailText;             // v2.3: 요리 설명 상자 (조리·도감 탭 아래) - 카드에 마우스를 올리거나 클릭하면 채워진다
    private Text hintText;               // v2.4: 오른쪽 위 안내줄 - 정차 중엔 남은 조리 횟수도
    private Button[] tabButtons = new Button[3];

    private static readonly string[] METHOD_NAMES = { "굽기", "볶기", "끓이기" };

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        canvas = UIFactory.CreateCanvas("KitchenPanel_Canvas", 20); // HUD보다 위
        BuildFrame();
        root.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (CookingMinigame.IsActive) return;   // 미니게임 중엔 토글 금지
            if (PauseMenu.IsOpen) return;           // 일시정지 중엔 토글 금지
            if (AugmentListUI.ReadingOpen) return;  // A10: 증강 목록[V]/일지[J] 열람 중엔 토글 금지
            if (MerchantUI.IsOpen || SpinoBetUI.IsOpen || BranchRouteUI.IsOpen) return;   // v2.3: 정차역 창이 입력을 독점

            if (isOpen) Close();
            else Open(-1);   // Tab은 항상 전체 보기
        }

        // v2.3: 열려 있으면 [ESC] 도 닫기 (같은 프레임에 일시정지 메뉴가 열리지 않게 공용 스탬프)
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CookingMinigame.EscConsumedFrame = Time.frameCount;
            Close();
        }
    }

    // ─────────────────────────────────────────
    // 열기 / 닫기
    // ─────────────────────────────────────────

    /// <summary>패널 열기. filter: -1=전체, 0/1/2=해당 조리법 요리만</summary>
    public void Open(int filter)
    {
        if (CookingMinigame.IsActive) return;
        if (PauseMenu.IsOpen) return;
        isOpen = true;
        stationFilter = filter;
        tabIndex = 0;
        selectedRecipe = "";
        root.gameObject.SetActive(true);
        ModalFeel.Play(root);   // v2.5: 등장 연출
        Refresh();
    }

    /// <summary>조리대(E키)에서 호출: 해당 조리법 전용으로 열기</summary>
    public void OpenForStation(int method)
    {
        Open(Mathf.Clamp(method, 0, 2));
    }

    public void Close()
    {
        isOpen = false;
        root.gameObject.SetActive(false);
        selectedRecipe = ""; fuseA = ""; fuseB = "";
        stationFilter = -1;
    }

    // ─────────────────────────────────────────
    // 레시피 고유 조리법 판정 (조리법 정체성의 핵심)
    // ─────────────────────────────────────────

    /// <summary>레시피의 고유 조리법. 0=굽기 1=볶기 2=끓이기</summary>
    public static int MethodOf(RecipeData r)
    {
        if (r == null) return 0;

        // 픽스 2차 (플레이테스트: "스튜인데 볶기를 한다"): 이름이 조리법을 말하면
        // 이름이 최우선이다 - 유저가 읽는 그대로 조리해야 어색하지 않다.
        // (예: '절대영도 수프' 볶기 -> 끓이기 / '마비독 꼬치' 끓이기 -> 굽기)
        string n = r.displayName;
        if (!string.IsNullOrEmpty(n))
        {
            if (n.Contains("수프") || n.Contains("스튜") || n.Contains("탕")
                || n.Contains("찜") || n.Contains("진액") || n.Contains("스프"))
                return 2;   // 끓이기
            if (n.Contains("볶음") || n.Contains("볶기"))
                return 1;   // 볶기
            if (n.Contains("육포") || n.Contains("스테이크") || n.Contains("구이")
                || n.Contains("꼬치") || n.Contains("바베큐") || n.Contains("립")
                || n.Contains("철판") || n.Contains("그릴"))
                return 0;   // 굽기
        }

        // 스프/정식/장판/오라/버프 계열 -> 끓이기 (냄비)
        bool isPot = !string.IsNullOrEmpty(r.passiveType) || !string.IsNullOrEmpty(r.buffType)
            || r.shape == AttackShape.Passive || r.shape == AttackShape.Aura || r.shape == AttackShape.Field;
        if (isPot) return 2;

        // 고기 재료(T1) 또는 물리 계열(T2) -> 굽기 (그릴)
        if (r.recipeId.Contains("meat")) return 0;
        if (r.tier == 2 && r.damageType == DamageType.Phys) return 0;

        // 독 진액 계열 -> 끓이기
        if (r.tag == FoodTag.Poison) return 2;

        // 나머지 속성/마법 계열 -> 볶기 (팬)
        return 1;
    }

    /// <summary>조리법 이름 (0=굽기 1=볶기 2=끓이기)</summary>
    public static string MethodName(int method)
    {
        if (method < 0 || method >= METHOD_NAMES.Length) return "?";
        return METHOD_NAMES[method];
    }

    // ─────────────────────────────────────────
    // 프레임 구성 (1회)
    // ─────────────────────────────────────────
    private void BuildFrame()
    {
        // 어두운 배경 (클릭 차단)
        root = UIFactory.CreatePanel(canvas.transform, "KitchenRoot",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0.6f), new Color(0f, 0f, 0f, 0f), 0f);

        // 중앙 패널
        RectTransform panel = UIFactory.CreatePanel(root, "Panel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-620f, -450f), new Vector2(620f, 450f),   // v2.3: 760 -> 900 (아래 설명 상자 자리)
            UIFactory.PANEL, UIFactory.COPPER, 4f);

        bool skin = UISkin.Available;   // v2.1: 파이프 프레임(테 28px)일 때만 배치를 조금 옮긴다

        // 타이틀 (스킨: 파이프 위에 걸린 황동 명판 / 아니면 금색 글자)
        if (skin)
        {
            UISkin.Nameplate(panel, "Title", "주 방", 24, new Vector2(0f, 1f), new Vector2(40f, 4f));
            UISkin.AddOrnament(panel, "gauge", new Vector2(1f, 1f), new Vector2(-230f, 14f), new Vector2(56f, 56f));
            UISkin.AddOrnament(panel, "valve", new Vector2(1f, 1f), new Vector2(-166f, 10f), new Vector2(48f, 48f));
        }
        else
        {
            Text title = UIFactory.CreateText(panel, "Title", "주방", 30, UIFactory.GOLD, TextAnchor.UpperLeft);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = new Vector2(28f, -56f);
            title.rectTransform.offsetMax = new Vector2(0f, -16f);
        }

        hintText = UIFactory.CreateText(panel, "Hint", "[Tab]/[ESC] 닫기", 16, UIFactory.DIM, TextAnchor.UpperRight);
        Text hint = hintText;
        hint.rectTransform.anchorMin = new Vector2(0f, 1f);
        hint.rectTransform.anchorMax = new Vector2(1f, 1f);
        hint.rectTransform.offsetMin = new Vector2(0f, skin ? -60f : -46f);
        hint.rectTransform.offsetMax = new Vector2(-28f, skin ? -34f : -20f);

        // 탭 버튼 3개
        string[] tabNames = { "조리", "합성", "도감" };
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            Button b = UIFactory.CreateButton(panel, "Tab_" + i, tabNames[i],
                new Vector2(120f, 40f), new Color(0.35f, 0.23f, 0.13f), UIFactory.CREAM, 20);
            RectTransform brt = b.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(130f + i * 128f, -14f);
            b.onClick.AddListener(delegate { tabIndex = idx; Refresh(); });
            tabButtons[i] = b;
        }

        // 내용 영역
        GameObject contentGo = new GameObject("Content");
        contentArea = contentGo.AddComponent<RectTransform>();
        contentArea.SetParent(panel, false);
        contentArea.anchorMin = Vector2.zero;
        contentArea.anchorMax = Vector2.one;
        contentArea.offsetMin = skin ? new Vector2(30f, 30f) : new Vector2(20f, 16f);      // 스킨: 파이프(28px) 안쪽
        contentArea.offsetMax = skin ? new Vector2(-30f, -70f) : new Vector2(-20f, -66f);
    }

    // ─────────────────────────────────────────
    // 탭 내용 갱신
    // ─────────────────────────────────────────
    private void Refresh()
    {
        if (hintText != null) hintText.text = CookingBridge.StopCookHint() + "[Tab]/[ESC] 닫기";
        // 탭 버튼 강조
        for (int i = 0; i < 3; i++)
        {
            Image img = tabButtons[i].GetComponent<Image>();
            img.color = (i == tabIndex) ? new Color(0.55f, 0.36f, 0.18f) : new Color(0.35f, 0.23f, 0.13f);
        }

        // 기존 내용 제거
        for (int i = 0; i < spawned.Count; i++) Destroy(spawned[i]);
        spawned.Clear();

        if (tabIndex == 0) BuildCookTab();
        else if (tabIndex == 1) BuildFuseTab();
        else BuildDexTab();

        // v2.3: 조리·도감 탭엔 아래쪽에 설명 상자 (선택한 요리가 있으면 그것부터)
        if (tabIndex != 1)
        {
            MakeDetailBox();
            RecipeData sel = string.IsNullOrEmpty(selectedRecipe) ? null : RecipeDatabase.Get(selectedRecipe);
            if (sel != null) ShowRecipeDetail(sel);
        }
    }

    // ═════════════════ v2.3: 요리 설명 상자 ═════════════════
    private void MakeDetailBox()
    {
        RectTransform box = UIFactory.CreatePanel(contentArea, "DetailBox",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 0f), new Vector2(0f, 130f),
            new Color(0.10f, 0.065f, 0.045f, 0.96f), UIFactory.GOLD, 2f);
        spawned.Add(box.gameObject);
        detailText = UIFactory.CreateText(box, "Text",
            "요리 카드에 마우스를 올리거나 클릭하면 여기에 설명이 뜬다 - 무엇을 하나 / 어떤 손님에 잘 박히나 / 언제 쓰나", 16,
            UIFactory.DIM, TextAnchor.UpperLeft);
        detailText.rectTransform.offsetMin = new Vector2(16f, 10f);
        detailText.rectTransform.offsetMax = new Vector2(-16f, -10f);
        detailText.lineSpacing = 1.15f;
        detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailText.raycastTarget = false;
    }

    /// <summary>카드 위에 마우스 / 클릭 - 설명 상자 채우기 (미발견은 재료·조리법만)</summary>
    private void ShowRecipeDetail(RecipeData r)
    {
        if (detailText == null || r == null) return;
        bool disc = FoodStock.Instance != null && FoodStock.Instance.IsDiscovered(r.recipeId);
        string[] parts = r.recipeId.Replace("T2:", "").Split('+');
        string src = r.tier == 1 && parts.Length >= 2 ? MatKor(parts[0]) + " + " + MatKor(parts[1])
                   : (parts.Length >= 2 ? "합성: " + parts[0] + " + " + parts[1] : r.recipeId);
        if (!disc)
        {
            detailText.text = "???   " + src + " 을(를) " + MethodName(MethodOf(r)) + " 하면 처음 알게 된다";
            detailText.color = UIFactory.DIM;
            return;
        }
        string t = r.displayName + (r.tier == 2 ? "  [전설]" : "") + "   " + RecipeText.RoleWord(r) + "   |   " + src + "   [" + MethodName(MethodOf(r)) + "]\n";
        t += RecipeText.Full(r, 1f);
        if (!string.IsNullOrEmpty(r.flavor)) t += "\n" + r.flavor;
        detailText.text = t;
        detailText.color = UIFactory.CREAM;
    }

    /// <summary>카드에 호버 수신기 부착</summary>
    private void AttachDetailHover(RectTransform card, RecipeData r)
    {
        KitchenPanelCardHover h = card.gameObject.AddComponent<KitchenPanelCardHover>();
        h.owner = this; h.recipe = r;
    }

    /// <summary>KitchenPanelCardHover 가 부른다</summary>
    public void OnCardHover(RecipeData r) { ShowRecipeDetail(r); }

    // ═════════════════ 조리 탭 ═════════════════
    private void BuildCookTab()
    {
        // 상단: 재료 현황 바 (+ 조리대 필터 안내)
        RectTransform matBar = UIFactory.CreatePanel(contentArea, "MatBar",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -46f), new Vector2(0f, 0f),
            new Color(0.15f, 0.095f, 0.06f, 0.9f), UIFactory.COPPER, 2f);
        spawned.Add(matBar.gameObject);

        // v2.2: 재료 아이콘이 있으면 "아이콘 + 이름 수량" 칸 6개, 없으면 한 줄 글자
        bool matIcons = UISkin.Available && UISkin.MaterialIcon(MaterialType.Meat) != null;
        string matStr = "";
        int mi = 0;
        foreach (MaterialType t in System.Enum.GetValues(typeof(MaterialType)))
        {
            int have = MaterialInventory.Instance.Get(t);
            if (matIcons)
            {
                float cellX = 14f + mi * 128f;
                UISkin.AddMaterialIcon(matBar, t, new Vector2(0f, 0.5f), new Vector2(cellX, 0f), 32f);
                Text cnt = UIFactory.CreateText(matBar, "MatCnt" + mi, MaterialNames.KOR[mi] + " " + have, 19,
                    have > 0 ? UIFactory.CREAM : UIFactory.DIM, TextAnchor.MiddleLeft);
                RectTransform cRt = cnt.rectTransform;
                cRt.anchorMin = new Vector2(0f, 0f);
                cRt.anchorMax = new Vector2(0f, 1f);
                cRt.pivot = new Vector2(0f, 0.5f);
                cRt.anchoredPosition = new Vector2(cellX + 38f, 0f);
                cRt.sizeDelta = new Vector2(86f, 0f);
            }
            else
                matStr += MaterialNames.KOR[mi] + " " + have + "   ";
            mi++;
        }
        // 조리대에서 열었으면 필터 안내 추가 (아이콘 모드에서는 오른쪽 끝에 따로)
        string filterStr = stationFilter >= 0 ? "<< [" + MethodName(stationFilter) + "] 전용 조리대 >>" : "";
        if (matIcons)
        {
            if (filterStr.Length > 0)
            {
                Text ft = UIFactory.CreateText(matBar, "FilterText", filterStr, 17, UIFactory.GOLD, TextAnchor.MiddleRight);
                ft.rectTransform.offsetMax = new Vector2(-16f, 0f);
            }
        }
        else
        {
            if (filterStr.Length > 0) matStr += "     " + filterStr;
            UIFactory.CreateText(matBar, "MatText", matStr, 19, UIFactory.CREAM, TextAnchor.MiddleLeft)
                .rectTransform.offsetMin = new Vector2(16f, 0f);
        }

        // 선택된 레시피 바 + 조리 시작 버튼 (레시피 고유 조리법 1개만)
        float topY = -56f;
        if (!string.IsNullOrEmpty(selectedRecipe))
        {
            RecipeData sel = RecipeDatabase.Get(selectedRecipe);
            bool disc = FoodStock.Instance.IsDiscovered(selectedRecipe);
            int method = MethodOf(sel);

            RectTransform selBar = UIFactory.CreatePanel(contentArea, "SelBar",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, topY - 52f), new Vector2(0f, topY),
                new Color(0.22f, 0.14f, 0.08f, 0.95f), UIFactory.GOLD, 2f);
            spawned.Add(selBar.gameObject);

            UIFactory.CreateText(selBar, "SelText",
                "선택: " + (disc ? sel.displayName : "??? (미지의 요리)") +
                "   조리법: [" + MethodName(method) + "]",
                19, UIFactory.GOLD, TextAnchor.MiddleLeft)
                .rectTransform.offsetMin = new Vector2(16f, 0f);

            Button cookBtn = UIFactory.CreateButton(selBar, "CookGo", MethodName(method) + " 시작!",
                new Vector2(150f, 38f), new Color(0.45f, 0.29f, 0.15f), UIFactory.CREAM, 19);
            RectTransform brt = cookBtn.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1f, 0.5f);
            brt.anchorMax = new Vector2(1f, 0.5f);
            brt.pivot = new Vector2(1f, 0.5f);
            brt.anchoredPosition = new Vector2(-16f, 0f);
            int capturedMethod = method;
            cookBtn.onClick.AddListener(delegate { StartMinigame(capturedMethod); });

            topY -= 62f;
        }

        // 레시피 카드 그리드 (5열, 조리대 필터 적용)
        BuildRecipeGrid(topY, 1, true);
    }

    /// <summary>레시피 카드 그리드 생성. tier 필터, 조리용/도감용 구분</summary>
    private void BuildRecipeGrid(float startY, int tierFilter, bool forCooking)
    {
        int col = 0, row = 0;
        int columns = 5;
        float cardW = 224f, cardH = 84f, gap = 10f;

        foreach (RecipeData r in RecipeDatabase.All)
        {
            if (tierFilter > 0 && r.tier != tierFilter) continue;

            // 조리대 필터: 해당 조리법 요리만 표시
            if (forCooking && stationFilter >= 0 && MethodOf(r) != stationFilter) continue;

            bool discovered = FoodStock.Instance.IsDiscovered(r.recipeId);
            string[] parts = r.recipeId.Replace("T2:", "").Split('+');

            bool canAfford = false;
            if (forCooking)
                canAfford = MaterialInventory.Instance.CanAfford(ParseMat(parts[0]), ParseMat(parts[1]));

            Color borderC = (selectedRecipe == r.recipeId) ? UIFactory.GOLD
                : discovered ? UIFactory.TagColor(r.tag) : new Color(0.35f, 0.28f, 0.22f);

            RectTransform card = UIFactory.CreatePanel(contentArea, "Card_" + r.recipeId,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(col * (cardW + gap), startY - (row + 1) * (cardH + gap)),
                new Vector2(col * (cardW + gap) + cardW, startY - row * (cardH + gap) - gap),
                new Color(0.16f, 0.10f, 0.065f, 0.95f), borderC, 2f);
            spawned.Add(card.gameObject);

            string nameStr = discovered ? r.displayName : "???";
            // 재료 조합 + 고유 조리법 표기
            string matStr = MatKor(parts[0]) + " + " + MatKor(parts[1]) + "   [" + MethodName(MethodOf(r)) + "]";

            Text nameText = UIFactory.CreateText(card, "Name", nameStr, 18,
                canAfford || !forCooking ? UIFactory.CREAM : UIFactory.DIM, TextAnchor.UpperLeft);
            nameText.rectTransform.offsetMin = new Vector2(12f, 34f);
            nameText.rectTransform.offsetMax = new Vector2(-8f, -8f);

            Text matText = UIFactory.CreateText(card, "Mats", matStr, 15, UIFactory.DIM, TextAnchor.LowerLeft);
            matText.rectTransform.offsetMin = new Vector2(12f, 8f);
            matText.rectTransform.offsetMax = new Vector2(-8f, 34f);

            if (forCooking)
            {
                Button btn = card.gameObject.AddComponent<Button>();
                string id = r.recipeId;
                btn.interactable = canAfford;
                btn.onClick.AddListener(delegate { selectedRecipe = id; Refresh(); });
            }
            AttachDetailHover(card, r);   // v2.3

            col++;
            if (col >= columns) { col = 0; row++; }
        }
    }

    private void StartMinigame(int method)
    {
        string[] parts = selectedRecipe.Split('+');
        if (!CookingBridge.StartCook(ParseMat(parts[0]), ParseMat(parts[1]))) return;

        // 새 미니게임 실행 (레시피 고유 조리법으로)
        if (CookingMinigame.Instance != null)
        {
            CookingMinigame.Instance.StartGame(method);
        }
        else
        {
            Debug.LogWarning("[주방] CookingMinigame 없음 - 즉시 완성 처리");
            CookingBridge.FinishCook("good");
        }

        // 미니게임 진행 동안 패널 닫기
        isOpen = false;
        root.gameObject.SetActive(false);
        selectedRecipe = "";
        stationFilter = -1;
    }

    // ═════════════════ 합성 탭 ═════════════════
    private void BuildFuseTab()
    {
        // 상단: 선택 상태 + 결과 미리보기 + 합성 버튼
        RectTransform selBar = UIFactory.CreatePanel(contentArea, "FuseSel",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -96f), new Vector2(0f, 0f),
            new Color(0.22f, 0.14f, 0.08f, 0.95f), UIFactory.T2PINK, 2f);
        spawned.Add(selBar.gameObject);

        string aName = string.IsNullOrEmpty(fuseA) ? "[?]" : RecipeDatabase.Get(fuseA).displayName;
        string bName = string.IsNullOrEmpty(fuseB) ? "[?]" : RecipeDatabase.Get(fuseB).displayName;
        string resultLine = "";

        if (!string.IsNullOrEmpty(fuseA) && !string.IsNullOrEmpty(fuseB))
        {
            RecipeData ra = RecipeDatabase.Get(fuseA);
            RecipeData rb = RecipeDatabase.Get(fuseB);
            RecipeData result = RecipeDatabase.GetFusion(ra.tag, rb.tag);
            if (result != null)
                resultLine = "결과: " + (FoodStock.Instance.IsDiscovered(result.recipeId)
                    ? result.displayName : "??? (미지의 전설 요리)");
        }

        UIFactory.CreateText(selBar, "SelText",
            "기본 요리 2개 선택:  " + aName + "  +  " + bName + "\n" + resultLine,
            19, UIFactory.CREAM, TextAnchor.MiddleLeft)
            .rectTransform.offsetMin = new Vector2(16f, 0f);

        Button fuseBtn = UIFactory.CreateButton(selBar, "FuseGo", "합성!",
            new Vector2(130f, 48f), new Color(0.55f, 0.2f, 0.42f), Color.white, 22);
        RectTransform frt = fuseBtn.GetComponent<RectTransform>();
        frt.anchorMin = new Vector2(1f, 0.5f);
        frt.anchorMax = new Vector2(1f, 0.5f);
        frt.pivot = new Vector2(1f, 0.5f);
        frt.anchoredPosition = new Vector2(-16f, 0f);
        fuseBtn.interactable = !string.IsNullOrEmpty(fuseA) && !string.IsNullOrEmpty(fuseB);
        fuseBtn.onClick.AddListener(delegate
        {
            string made = CookingBridge.FuseFoods(fuseA, fuseB);
            if (made != null) { fuseA = ""; fuseB = ""; }
            Refresh();
        });

        // 보유 T1 요리 카드 목록
        int col = 0, row = 0;
        int columns = 5;
        float cardW = 224f, cardH = 74f, gap = 10f;
        float startY = -106f;

        foreach (KeyValuePair<string, int> kv in FoodStock.Instance.AllStock)
        {
            RecipeData r = RecipeDatabase.Get(kv.Key);
            if (r == null || r.tier != 1 || kv.Value <= 0) continue;

            bool isSelected = (fuseA == kv.Key || fuseB == kv.Key);
            Color borderC = isSelected ? UIFactory.T2PINK : UIFactory.TagColor(r.tag);

            RectTransform card = UIFactory.CreatePanel(contentArea, "Fuse_" + kv.Key,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(col * (cardW + gap), startY - (row + 1) * (cardH + gap)),
                new Vector2(col * (cardW + gap) + cardW, startY - row * (cardH + gap) - gap),
                new Color(0.16f, 0.10f, 0.065f, 0.95f), borderC, isSelected ? 4f : 2f);
            spawned.Add(card.gameObject);

            UIFactory.CreateText(card, "Name",
                r.displayName + "  x" + kv.Value + "\n계열: " + r.tag,
                17, UIFactory.CREAM, TextAnchor.MiddleLeft)
                .rectTransform.offsetMin = new Vector2(12f, 0f);

            Button btn = card.gameObject.AddComponent<Button>();
            string id = kv.Key;
            int owned = kv.Value;
            btn.onClick.AddListener(delegate { SelectFuse(id, owned); Refresh(); });

            col++;
            if (col >= columns) { col = 0; row++; }
        }
    }

    private void SelectFuse(string id, int owned)
    {
        if (fuseA == id && fuseB != id) { fuseA = ""; return; }   // 재클릭 = 해제
        if (fuseB == id) { fuseB = ""; return; }

        if (string.IsNullOrEmpty(fuseA)) fuseA = id;
        else if (string.IsNullOrEmpty(fuseB))
        {
            if (id == fuseA && owned < 2)
            {
                Debug.Log("[주방] 같은 요리 합성엔 2개 필요");
                return;
            }
            fuseB = id;
        }
        else { fuseA = id; fuseB = ""; }
    }

    // ═════════════════ 도감 탭 ═════════════════
    private void BuildDexTab()
    {
        int total = 0, found = 0;
        foreach (RecipeData r in RecipeDatabase.All)
        {
            total++;
            if (FoodStock.Instance.IsDiscovered(r.recipeId)) found++;
        }

        Text header = UIFactory.CreateText(contentArea, "DexHeader",
            "발견한 레시피: " + found + " / " + total, 20, UIFactory.GOLD, TextAnchor.UpperLeft);
        header.rectTransform.anchorMin = new Vector2(0f, 1f);
        header.rectTransform.anchorMax = new Vector2(1f, 1f);
        header.rectTransform.offsetMin = new Vector2(4f, -30f);
        header.rectTransform.offsetMax = new Vector2(0f, 0f);
        spawned.Add(header.gameObject);

        // T1 그리드 (위) + T2 그리드 (아래)
        int col = 0, row = 0;
        int columns = 7;
        float cardW = 158f, cardH = 92f, gap = 8f;   // v2.3: 96 -> 92 (설명 상자 자리)
        float startY = -36f;

        // 전체 42종 순회 (T1 먼저, T2 나중)
        for (int tier = 1; tier <= 2; tier++)
        {
            foreach (RecipeData r in RecipeDatabase.All)
            {
                if (r.tier != tier) continue;
                bool disc = FoodStock.Instance.IsDiscovered(r.recipeId);

                Color borderC = !disc ? new Color(0.3f, 0.24f, 0.19f)
                    : (r.tier == 2 ? UIFactory.T2PINK : UIFactory.TagColor(r.tag));

                RectTransform card = UIFactory.CreatePanel(contentArea, "Dex_" + r.recipeId,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(col * (cardW + gap), startY - (row + 1) * (cardH + gap)),
                    new Vector2(col * (cardW + gap) + cardW, startY - row * (cardH + gap) - gap),
                    new Color(0.14f, 0.09f, 0.06f, 0.95f), borderC, 2f);
                spawned.Add(card.gameObject);

                string txt;
                if (disc)
                {
                    string[] parts = r.recipeId.Replace("T2:", "").Split('+');
                    string src = r.tier == 1
                        ? MatKor(parts[0]) + "+" + MatKor(parts[1])
                        : "합성: " + parts[0] + "+" + parts[1];
                    txt = r.displayName + (r.tier == 2 ? " [전설]" : "") +
                          " [" + MethodName(MethodOf(r)) + "]\n" +
                          src + "\n" + RecipeText.RoleWord(r) + " - 클릭하면 설명";
                }
                else
                {
                    txt = "???\n" + (r.tier == 2 ? "(전설 미발견)" : "(미발견)");
                }

                Text t = UIFactory.CreateText(card, "Txt", txt, 13,
                    disc ? UIFactory.CREAM : UIFactory.DIM, TextAnchor.UpperLeft);
                t.rectTransform.offsetMin = new Vector2(8f, 4f);
                t.rectTransform.offsetMax = new Vector2(-6f, -6f);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                AttachDetailHover(card, r);   // v2.3: 올리거나 클릭 = 아래 설명 상자

                col++;
                if (col >= columns) { col = 0; row++; }
            }
        }
    }

    // ─────────────────────────────────────────
    private MaterialType ParseMat(string s)
    {
        switch (s)
        {
            case "armor": return MaterialType.Armor;
            case "elec": return MaterialType.Elec;
            case "fire": return MaterialType.Fire;
            case "ice": return MaterialType.Ice;
            case "poison": return MaterialType.Poison;
            default: return MaterialType.Meat;
        }
    }

    /// <summary>레시피 키 낱말 -> 재료 이름 (v9.10.1: MaterialNames 한 곳에서)</summary>
    private string MatKor(string s) { return MaterialNames.Kor(s); }
}

/// <summary>v2.3: 요리 카드 호버/클릭 수신기 - KitchenPanel.OnCardHover 로 설명 상자를 채운다</summary>
public class KitchenPanelCardHover : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public KitchenPanel owner;
    public RecipeData recipe;
    public void OnPointerEnter(PointerEventData e) { if (owner != null) owner.OnCardHover(recipe); }
    public void OnPointerClick(PointerEventData e) { if (owner != null) owner.OnCardHover(recipe); }
}
