using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// [WorkshopUI.cs] v2
/// 정비소 - 골드를 소모해 도구/기차를 정비하고 재료를 구매하는 상점
///
/// 조작
///  - 화면 우상단 [정비] 버튼 클릭 또는 G키로 열기/닫기
///  - 열려 있는 동안 게임 일시정지 (Time.timeScale = 0)
///
/// 메뉴
///  [정비]
///   1. 칼 연마    150G : 칼 예리함 100% 복구
///   2. 팬 정비    150G : 팬 상태 100% 복구
///   3. 기차 수리  200G : 기차 HP 500 회복
///   4. 장갑 보강  400G : 기차 최대 HP +150 영구
///  [재료 시장] (v2 신규)
///   - 조합 재료 6종을 개당 60G에 구매 (MaterialInventory 연동)
///
/// 사용법
///  - "GameSystems" 오브젝트에 이 스크립트만 추가 (UI는 코드로 자동 생성)
///  - KitchenEventManager의 UI 헬퍼를 재사용하므로 KitchenEventManager.cs 필요
///
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class WorkshopUI : MonoBehaviour
{
    public static WorkshopUI Instance;

    /// <summary>정비소가 열려 있는지 (다른 시스템에서 입력 차단용)</summary>
    public static bool IsOpen
    {
        get { return Instance != null && Instance.isOpen; }
    }

    [Header("정비 가격")]
    public int knifeCost = 150;      // 칼 연마
    public int panCost = 150;        // 팬 정비
    public int repairCost = 200;     // 기차 수리
    public float repairAmount = 500f;
    public int armorCost = 400;      // 장갑 보강
    public float armorAmount = 150f;

    [Header("재료 시장")]
    public int materialCost = 60;    // 재료 1개 가격

    private bool isOpen;

    // ---------- UI 참조 ----------
    private Canvas canvas;
    private RectTransform panelRoot;      // 정비소 패널 (암전 포함)
    private Text goldText;                // 보유 골드
    private Button gearButton;            // 화면 우상단 열기 버튼

    // 정비 메뉴 행별 참조
    private Text knifeStatus; private Button knifeBtn;
    private Text panStatus; private Button panBtn;
    private Text repairStatus; private Button repairBtn;
    private Text armorStatus; private Button armorBtn;

    // 재료 시장 행 (재료 종류별)
    private class MatRow
    {
        public MaterialType type;
        public Text status;
        public Button btn;
    }
    private List<MatRow> matRows = new List<MatRow>();

    // 외부 참조 캐시
    private ChefController chef;
    private TrainManager train;

    void Awake()
    {
        Instance = this;
        BuildUI();
        panelRoot.gameObject.SetActive(false);
    }

    void Update()
    {
        // G키 토글
        if (Input.GetKeyDown(KeyCode.G))
            Toggle();

        // 열려 있는 동안 실시간 갱신
        if (isOpen)
            RefreshAll();
    }

    // ==================================================================
    //  열기 / 닫기
    // ==================================================================

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        // 다른 전체화면 UI와 충돌 방지
        if (isOpen) return;
        if (PauseMenu.IsOpen) return;
        if (AugmentPickUI.IsOpen) return;
        if (CookingMinigame.IsActive) return;
        if (KitchenEventManager.IsActive) return;

        isOpen = true;
        FindRefs();
        RefreshAll();
        panelRoot.gameObject.SetActive(true);
        Time.timeScale = 0f;
        Debug.Log("[정비소] 열림");
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        panelRoot.gameObject.SetActive(false);

        // 증강 선택창이 떠 있지 않을 때만 시간 재개
        if (!AugmentPickUI.IsOpen)
            Time.timeScale = 1f;
        Debug.Log("[정비소] 닫힘");
    }

    private void FindRefs()
    {
        if (chef == null) chef = FindFirstObjectByType<ChefController>();
        if (train == null) train = FindFirstObjectByType<TrainManager>();
    }

    // ==================================================================
    //  구매 처리
    // ==================================================================

    private bool TrySpend(int cost)
    {
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.SpendGold(cost);
    }

    private void BuyKnife()
    {
        FindRefs();
        if (chef == null || chef.knifeSharpness >= 100f) return;
        if (!TrySpend(knifeCost)) return;
        chef.RepairKnife(100f);
        UIManager.Instance?.ShowStatChange("칼 연마 완료!");
    }

    private void BuyPan()
    {
        FindRefs();
        if (chef == null || chef.panCondition >= 100f) return;
        if (!TrySpend(panCost)) return;
        chef.RepairPan(100f);
        UIManager.Instance?.ShowStatChange("팬 정비 완료!");
    }

    private void BuyRepair()
    {
        FindRefs();
        if (train == null || train.currentHP >= train.currentMaxHP) return;
        if (!TrySpend(repairCost)) return;
        train.Heal(repairAmount);
        UIManager.Instance?.ShowStatChange("기차 수리 +" + Mathf.RoundToInt(repairAmount) + " HP!");
    }

    private void BuyArmor()
    {
        FindRefs();
        if (train == null) return;
        if (!TrySpend(armorCost)) return;
        train.AddMaxHP(armorAmount);
        UIManager.Instance?.ShowStatChange("장갑 보강! 최대 HP +" + Mathf.RoundToInt(armorAmount));
    }

    /// <summary>
    /// v2.1 (감사 3-A): 재료 시장 실가격 - 지역이 깊어질수록 비싸진다 (골드 인플레 흡수)
    /// 지역 1 = 기본가, 지역 2 = +20G, 지역 3+ = +40G
    /// </summary>
    private int GetMaterialCost()
    {
        int wave = GameManager.Instance != null ? GameManager.Instance.currentWave : 1;
        int region = Mathf.Clamp(GameBalance.RegionOf(wave), 1, 3);
        return materialCost + (region - 1) * 20;
    }

    private void BuyMaterial(MaterialType t)
    {
        if (MaterialInventory.Instance == null) return;
        if (!TrySpend(GetMaterialCost())) return;
        MaterialInventory.Instance.Add(t, 1);
        UIManager.Instance?.ShowStatChange(MaterialKoreanName(t) + " 구매! (-" + GetMaterialCost() + "G)");
    }

    /// <summary>재료 enum -> 한글 표시 이름</summary>
    private string MaterialKoreanName(MaterialType t)
    {
        string key = t.ToString().ToLower();
        if (key == "meat") return "고기";
        if (key == "armor") return "등심(장갑)";
        if (key == "fire") return "화염 재료";
        if (key == "ice") return "냉기 재료";
        if (key == "elec") return "전기 재료";
        if (key == "poison") return "독 재료";
        return t.ToString();
    }

    // ==================================================================
    //  상태 갱신
    // ==================================================================

    private void RefreshAll()
    {
        FindRefs();

        int gold = GameManager.Instance != null ? GameManager.Instance.playerGold : 0;
        goldText.text = "보유 골드:  " + gold + " G";

        // 칼
        float knife = chef != null ? chef.knifeSharpness : 0f;
        knifeStatus.text = "칼 연마  -  예리함 " + Mathf.RoundToInt(knife) + "%";
        SetButtonState(knifeBtn, gold >= knifeCost && knife < 100f);

        // 팬
        float pan = chef != null ? chef.panCondition : 0f;
        panStatus.text = "팬 정비  -  상태 " + Mathf.RoundToInt(pan) + "%";
        SetButtonState(panBtn, gold >= panCost && pan < 100f);

        // 기차 수리
        float hp = train != null ? train.currentHP : 0f;
        float maxHp = train != null ? train.currentMaxHP : 0f;
        repairStatus.text = "기차 수리 (+" + Mathf.RoundToInt(repairAmount) + " HP)  -  현재 "
            + Mathf.RoundToInt(hp) + "/" + Mathf.RoundToInt(maxHp);
        SetButtonState(repairBtn, gold >= repairCost && hp < maxHp);

        // 장갑 보강
        armorStatus.text = "장갑 보강  -  최대 HP +" + Mathf.RoundToInt(armorAmount) + " (영구)";
        SetButtonState(armorBtn, gold >= armorCost);

        // 재료 시장
        for (int i = 0; i < matRows.Count; i++)
        {
            MatRow row = matRows[i];
            int have = MaterialInventory.Instance != null ? MaterialInventory.Instance.Get(row.type) : 0;
            row.status.text = MaterialKoreanName(row.type) + "  -  보유 " + have + "개";
            SetButtonState(row.btn, gold >= GetMaterialCost() && MaterialInventory.Instance != null);
        }
    }

    /// <summary>구매 가능 여부에 따라 버튼 활성/회색 처리</summary>
    private void SetButtonState(Button btn, bool canBuy)
    {
        if (btn == null) return;
        btn.interactable = canBuy;
        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = canBuy ? 1f : 0.35f;
            img.color = c;
        }
    }

    // ==================================================================
    //  UI 생성 (KitchenEventManager 헬퍼 재사용)
    // ==================================================================

    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("WorkshopCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 550;   // 주방 이벤트(500)보다 위, 증강창(600)보다 아래
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // ---------- 우상단 정비소 열기 버튼 (항상 표시) ----------
        gearButton = KitchenEventManager.MakeButton(
            canvasGo.transform, "정비 (G)", new Color(0.35f, 0.30f, 0.22f, 0.92f),
            Vector2.zero, new Vector2(130f, 52f));
        RectTransform gearRt = gearButton.GetComponent<RectTransform>();
        gearRt.anchorMin = new Vector2(1f, 1f);
        gearRt.anchorMax = new Vector2(1f, 1f);
        gearRt.pivot = new Vector2(1f, 1f);
        // HUD 정리: 허공(-160)에 떠 있던 버튼을 우상단 구석에 정렬
        gearRt.anchoredPosition = new Vector2(-16f, -14f);
        gearButton.onClick.AddListener(delegate { Toggle(); });

        // ---------- 정비소 패널 ----------
        panelRoot = KitchenEventManager.MakeBox(canvasGo.transform, "WorkshopDim", new Color(0f, 0f, 0f, 0.75f));
        panelRoot.anchorMin = Vector2.zero;
        panelRoot.anchorMax = Vector2.one;
        panelRoot.offsetMin = Vector2.zero;
        panelRoot.offsetMax = Vector2.zero;

        // 본체 (v2: 재료 시장 추가로 세로 확장)
        RectTransform body = KitchenEventManager.MakeBox(panelRoot, "Body", new Color(0.12f, 0.10f, 0.08f, 0.98f));
        body.anchorMin = new Vector2(0.5f, 0.5f);
        body.anchorMax = new Vector2(0.5f, 0.5f);
        body.anchoredPosition = Vector2.zero;
        body.sizeDelta = new Vector2(720f, 880f);

        // 상단 띠 + 제목
        RectTransform band = KitchenEventManager.MakeBox(body, "Band", new Color(0.80f, 0.55f, 0.25f, 1f));
        band.anchorMin = new Vector2(0f, 1f);
        band.anchorMax = new Vector2(1f, 1f);
        band.pivot = new Vector2(0.5f, 1f);
        band.sizeDelta = new Vector2(0f, 54f);
        band.GetComponent<Image>().raycastTarget = false;

        Text title = KitchenEventManager.MakeText(band, "Title", "정비소", 28, new Color(0.10f, 0.08f, 0.05f));
        StretchFull(title.rectTransform);

        // 보유 골드
        goldText = KitchenEventManager.MakeText(body, "Gold", "", 23, new Color(1f, 0.85f, 0.35f));
        RectTransform goldRt = goldText.rectTransform;
        goldRt.anchorMin = new Vector2(0f, 1f);
        goldRt.anchorMax = new Vector2(1f, 1f);
        goldRt.pivot = new Vector2(0.5f, 1f);
        goldRt.anchoredPosition = new Vector2(0f, -62f);
        goldRt.sizeDelta = new Vector2(0f, 32f);

        // ---------- 정비 섹션 ----------
        MakeSectionLabel(body, "─ 정비 ─", -100f);
        knifeStatus = MakeRow(body, -128f, knifeCost, delegate { BuyKnife(); }, out knifeBtn);
        panStatus = MakeRow(body, -190f, panCost, delegate { BuyPan(); }, out panBtn);
        repairStatus = MakeRow(body, -252f, repairCost, delegate { BuyRepair(); }, out repairBtn);
        armorStatus = MakeRow(body, -314f, armorCost, delegate { BuyArmor(); }, out armorBtn);

        // ---------- 재료 시장 섹션 (v2) ----------
        MakeSectionLabel(body, "─ 재료 시장 (개당 " + materialCost + "G, 지역당 +20G 할증) ─", -388f);

        float matY = -416f;
        foreach (MaterialType t in System.Enum.GetValues(typeof(MaterialType)))
        {
            MaterialType captured = t;   // 클로저 캡처 (C# 7.3 필수)
            MatRow row = new MatRow();
            row.type = t;
            row.status = MakeRow(body, matY, materialCost, delegate { BuyMaterial(captured); }, out row.btn);
            matRows.Add(row);
            matY -= 56f;
        }

        // 닫기 버튼 (본체 하단)
        Button closeBtn = KitchenEventManager.MakeButton(
            body, "닫기 (G)", new Color(0.45f, 0.25f, 0.20f, 1f),
            new Vector2(0f, -390f), new Vector2(220f, 52f));
        RectTransform cRt = closeBtn.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.5f, 0f);
        cRt.anchorMax = new Vector2(0.5f, 0f);
        cRt.pivot = new Vector2(0.5f, 0f);
        cRt.anchoredPosition = new Vector2(0f, 16f);
        closeBtn.onClick.AddListener(delegate { Close(); });
    }

    /// <summary>섹션 구분 라벨</summary>
    private void MakeSectionLabel(RectTransform parent, string label, float y)
    {
        Text t = KitchenEventManager.MakeText(parent, "Section", label, 19, new Color(0.75f, 0.65f, 0.50f));
        RectTransform rt = t.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(0f, 26f);
    }

    /// <summary>메뉴 한 줄 생성: 왼쪽 상태 텍스트 + 오른쪽 구매 버튼. 상태 텍스트를 반환</summary>
    private Text MakeRow(RectTransform parent, float y, int cost,
        UnityEngine.Events.UnityAction onBuy, out Button buyBtn)
    {
        // 행 배경
        RectTransform row = KitchenEventManager.MakeBox(parent, "Row", new Color(1f, 1f, 1f, 0.06f));
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.anchoredPosition = new Vector2(0f, y);
        row.offsetMin = new Vector2(24f, row.offsetMin.y);
        row.offsetMax = new Vector2(-24f, row.offsetMax.y);
        row.sizeDelta = new Vector2(row.sizeDelta.x, 52f);
        row.GetComponent<Image>().raycastTarget = false;

        // 상태 텍스트 (왼쪽 정렬)
        Text status = KitchenEventManager.MakeText(row, "Status", "", 20, new Color(0.92f, 0.90f, 0.85f));
        RectTransform sRt = status.rectTransform;
        sRt.anchorMin = new Vector2(0f, 0f);
        sRt.anchorMax = new Vector2(0.72f, 1f);
        sRt.offsetMin = new Vector2(14f, 0f);
        sRt.offsetMax = Vector2.zero;
        status.alignment = TextAnchor.MiddleLeft;

        // 구매 버튼 (오른쪽)
        buyBtn = KitchenEventManager.MakeButton(
            row, cost + " G", new Color(0.25f, 0.42f, 0.25f, 1f),
            Vector2.zero, new Vector2(140f, 40f));
        RectTransform bRt = buyBtn.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(1f, 0.5f);
        bRt.anchorMax = new Vector2(1f, 0.5f);
        bRt.pivot = new Vector2(1f, 0.5f);
        bRt.anchoredPosition = new Vector2(-10f, 0f);
        buyBtn.onClick.AddListener(onBuy);

        return status;
    }

    /// <summary>부모 영역에 꽉 채우기</summary>
    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
