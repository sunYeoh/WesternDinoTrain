using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [ThawCannonUI.cs] v1 (신규 파일) - 보스 패턴 B단계
/// 광산 열차포 "해동포" - 동면자(지역 3 보스) 전용 시그니처 기믹.
///
/// 스토리: 광산 사람들이 대붕괴 때 만들다 버리고 간 열차포. 화실은 아직 살아 있다.
/// 기믹: 화염 재료/요리를 "장전"(태워서) -> 충전 100 -> 발사 -> 압력 게이지 타이밍 판정
///   PERFECT(정중앙) = 빙하 갑주 즉시 전파괴 + 대미지
///   GOOD(존 안)     = 갑주 감쇄율 절반 + 중간 대미지
///   MISS(존 밖)     = 소량 대미지
///
/// 사용법: 없음! BossGimmickSystem이 동면자 보스전 시작 시 자동 생성하고,
/// 보스가 죽거나 사라지면 스스로 정리된다. 파일만 프로젝트에 넣으면 끝.
/// 수치는 GameBalance의 '보스 패턴 (B단계)' 섹션에서 조정.
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class ThawCannonUI : MonoBehaviour
{
    private BossEnemy boss;

    // ── UI ──
    private GameObject canvasGo;
    private Text chargeText;
    private RectTransform chargeFill;
    private Button loadMatButton;
    private Button loadFoodButton;
    private Button fireButton;
    private Text loadMatLabel;
    private Text loadFoodLabel;

    // ── 압력 발사 미니게임 ──
    private GameObject pressureRoot;
    private RectTransform pressureCursor;
    private bool pressureActive = false;
    private float pressurePos = 0f;      // 0~100 왕복
    private float pressureDir = 1f;
    private const float PRESSURE_SPEED = 85f;
    private const float TRACK_W = 460f;

    // ── 상태 ──
    private float charge = 0f;

    public void Setup(BossEnemy targetBoss)
    {
        boss = targetBoss;
        BuildUI();
        Refresh();
        UIManager.Instance?.ShowStatChange("[해동포] 연결! 화염을 장전해 갑주를 녹여라!");
        Debug.Log("[ThawCannon] 해동포 연결 (동면자 보스전)");
    }

    private void OnDestroy()
    {
        if (canvasGo != null) Destroy(canvasGo);
    }

    private void Update()
    {
        // 보스가 죽거나 사라지면 자동 정리
        if (boss == null || !boss.IsAlive)
        {
            Destroy(gameObject);
            return;
        }

        if (pressureActive)
            UpdatePressure();
    }

    // ─────────────────────────────────────────────
    // 장전
    // ─────────────────────────────────────────────
    private void OnLoadMaterial()
    {
        if (pressureActive) return;
        if (MaterialInventory.Instance == null || MaterialInventory.Instance.Get(MaterialType.Fire) <= 0)
        {
            UIManager.Instance?.ShowStatChange("화염 재료가 없다!");
            return;
        }

        MaterialInventory.Instance.Add(MaterialType.Fire, -1);
        charge = Mathf.Min(GameBalance.ThawChargeMax, charge + GameBalance.ThawChargePerMaterial);
        Debug.Log("[ThawCannon] 화염 재료 장전 (+" + GameBalance.ThawChargePerMaterial + ")");
        Refresh();
    }

    private void OnLoadFood()
    {
        if (pressureActive) return;

        // 보유 중인 화염 태그 요리 탐색
        string fireFood = FindFireFood();
        if (fireFood == null)
        {
            UIManager.Instance?.ShowStatChange("화염 요리가 없다! (화염 방벽/용암 폭탄밥 등)");
            return;
        }

        FoodStock.Instance.TryConsume(fireFood, 1);
        charge = Mathf.Min(GameBalance.ThawChargeMax, charge + GameBalance.ThawChargePerFood);

        RecipeData r = RecipeDatabase.Get(fireFood);
        string name = r != null ? r.displayName : fireFood;
        UIManager.Instance?.ShowStatChange("[해동포] " + name + " 장전! (화력 +" + GameBalance.ThawChargePerFood + ")");
        Refresh();
    }

    /// <summary>보유 중인 화염(Fire 태그) 요리 중 하나의 id. 없으면 null</summary>
    private string FindFireFood()
    {
        if (FoodStock.Instance == null) return null;

        foreach (KeyValuePair<string, int> pair in FoodStock.Instance.AllStock)
        {
            if (pair.Value <= 0) continue;
            RecipeData r = RecipeDatabase.Get(pair.Key);
            if (r != null && r.tag == FoodTag.Fire) return pair.Key;
        }
        return null;
    }

    // ─────────────────────────────────────────────
    // 발사 (압력 게이지 타이밍)
    // ─────────────────────────────────────────────
    private void OnFire()
    {
        if (pressureActive) return;
        if (charge < GameBalance.ThawChargeMax) return;
        if (CookingMinigame.IsActive)
        {
            UIManager.Instance?.ShowStatChange("조리 중에는 발사할 수 없다!");
            return;
        }

        pressureActive = true;
        pressurePos = 0f;
        pressureDir = 1f;
        pressureRoot.SetActive(true);
        UIManager.Instance?.ShowStatChange("압력 게이지 정중앙에서 [Space] 발사!");
    }

    private void UpdatePressure()
    {
        // 커서 왕복
        pressurePos += pressureDir * PRESSURE_SPEED * Time.deltaTime;
        if (pressurePos >= 100f) { pressurePos = 100f; pressureDir = -1f; }
        if (pressurePos <= 0f) { pressurePos = 0f; pressureDir = 1f; }

        pressureCursor.anchoredPosition = new Vector2(-TRACK_W / 2f + TRACK_W * (pressurePos / 100f), 0f);

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            FireResolve();
    }

    private void FireResolve()
    {
        pressureActive = false;
        pressureRoot.SetActive(false);
        charge = 0f;
        SoundManager.Play("sfx_cannon_fire");

        // 판정: 중앙(50)에서의 거리
        float d = Mathf.Abs(pressurePos - 50f);
        int quality = d <= 7f ? 2 : (d <= 20f ? 1 : 0);

        Debug.Log("[ThawCannon] 발사! 압력 " + (int)pressurePos + " -> 판정 " + quality);
        if (boss != null && boss.IsAlive)
            boss.HitByThawCannon(quality);

        Refresh();
    }

    // ─────────────────────────────────────────────
    // UI 갱신
    // ─────────────────────────────────────────────
    private void Refresh()
    {
        float ratio = charge / GameBalance.ThawChargeMax;
        Vector2 max = chargeFill.anchorMax;
        max.x = Mathf.Clamp01(ratio);
        chargeFill.anchorMax = max;
        chargeFill.offsetMax = new Vector2(0f, chargeFill.offsetMax.y);

        bool full = charge >= GameBalance.ThawChargeMax;
        chargeText.text = full ? "충전 완료! 발사하라!" : "화력 " + (int)charge + " / " + (int)GameBalance.ThawChargeMax;
        fireButton.interactable = full;

        int fireMat = MaterialInventory.Instance != null ? MaterialInventory.Instance.Get(MaterialType.Fire) : 0;
        loadMatLabel.text = "재료 장전 (+" + (int)GameBalance.ThawChargePerMaterial + ") 보유 " + fireMat;
        loadFoodLabel.text = "요리 장전 (+" + (int)GameBalance.ThawChargePerFood + ")";
    }

    // ─────────────────────────────────────────────
    // UI 생성 (코드 생성 - 좌측 중단)
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        canvasGo = new GameObject("ThawCannonCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 485;   // 보스 UI(480) 바로 위, 주방이벤트(500) 아래
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // 본체 패널 (좌측 중단)
        RectTransform panel = KitchenEventManager.MakeBox(canvasGo.transform, "CannonPanel",
            new Color(0.1f, 0.08f, 0.06f, 0.92f));
        panel.anchorMin = new Vector2(0f, 0.5f);
        panel.anchorMax = new Vector2(0f, 0.5f);
        panel.pivot = new Vector2(0f, 0.5f);
        panel.anchoredPosition = new Vector2(14f, 60f);
        panel.sizeDelta = new Vector2(280f, 190f);

        Text title = KitchenEventManager.MakeText(panel, "Title", "해동포 (광산 열차포)", 21,
            new Color(1f, 0.7f, 0.35f));
        RectTransform tRt = title.rectTransform;
        tRt.anchorMin = new Vector2(0f, 1f); tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -8f);
        tRt.sizeDelta = new Vector2(0f, 26f);

        // 충전 게이지
        RectTransform gaugeBg = KitchenEventManager.MakeBox(panel, "GaugeBG", new Color(0f, 0f, 0f, 0.6f));
        gaugeBg.anchorMin = new Vector2(0f, 1f); gaugeBg.anchorMax = new Vector2(1f, 1f);
        gaugeBg.pivot = new Vector2(0.5f, 1f);
        gaugeBg.anchoredPosition = new Vector2(0f, -40f);
        gaugeBg.offsetMin = new Vector2(12f, gaugeBg.offsetMin.y);
        gaugeBg.offsetMax = new Vector2(-12f, gaugeBg.offsetMax.y);
        gaugeBg.sizeDelta = new Vector2(gaugeBg.sizeDelta.x, 18f);

        chargeFill = KitchenEventManager.MakeBox(gaugeBg, "Fill", new Color(1f, 0.5f, 0.15f));
        chargeFill.anchorMin = new Vector2(0f, 0f);
        chargeFill.anchorMax = new Vector2(0f, 1f);
        chargeFill.offsetMin = Vector2.zero;
        chargeFill.offsetMax = Vector2.zero;

        chargeText = KitchenEventManager.MakeText(panel, "ChargeText", "", 17,
            new Color(0.95f, 0.9f, 0.8f));
        RectTransform cRt = chargeText.rectTransform;
        cRt.anchorMin = new Vector2(0f, 1f); cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.anchoredPosition = new Vector2(0f, -62f);
        cRt.sizeDelta = new Vector2(0f, 22f);

        // 장전 버튼 2개
        loadMatButton = KitchenEventManager.MakeButton(panel, "재료 장전",
            new Color(0.45f, 0.28f, 0.1f), new Vector2(0f, -8f), new Vector2(250f, 34f));
        loadMatLabel = loadMatButton.GetComponentInChildren<Text>();
        loadMatButton.onClick.AddListener(OnLoadMaterial);

        loadFoodButton = KitchenEventManager.MakeButton(panel, "요리 장전",
            new Color(0.55f, 0.3f, 0.1f), new Vector2(0f, -46f), new Vector2(250f, 34f));
        loadFoodLabel = loadFoodButton.GetComponentInChildren<Text>();
        loadFoodButton.onClick.AddListener(OnLoadFood);

        // 발사 버튼
        fireButton = KitchenEventManager.MakeButton(panel, "발사!",
            new Color(0.75f, 0.2f, 0.1f), new Vector2(0f, -82f), new Vector2(250f, 36f));
        fireButton.onClick.AddListener(OnFire);

        // ── 압력 발사 오버레이 (화면 중앙, 발사 시에만 표시) ──
        RectTransform pRoot = KitchenEventManager.MakeBox(canvasGo.transform, "PressureRoot",
            new Color(0.08f, 0.05f, 0.04f, 0.95f));
        pRoot.anchorMin = new Vector2(0.5f, 0.5f);
        pRoot.anchorMax = new Vector2(0.5f, 0.5f);
        pRoot.pivot = new Vector2(0.5f, 0.5f);
        pRoot.anchoredPosition = new Vector2(0f, 140f);
        pRoot.sizeDelta = new Vector2(560f, 110f);
        pressureRoot = pRoot.gameObject;

        Text pTitle = KitchenEventManager.MakeText(pRoot, "PTitle",
            "압력 정중앙에서 [Space] 발사!", 22, new Color(1f, 0.8f, 0.4f));
        RectTransform ptRt = pTitle.rectTransform;
        ptRt.anchorMin = new Vector2(0f, 1f); ptRt.anchorMax = new Vector2(1f, 1f);
        ptRt.pivot = new Vector2(0.5f, 1f);
        ptRt.anchoredPosition = new Vector2(0f, -8f);
        ptRt.sizeDelta = new Vector2(0f, 28f);

        // 트랙 + 판정 존 (중앙 넓은 존 = GOOD, 좁은 존 = PERFECT)
        RectTransform track = KitchenEventManager.MakeBox(pRoot, "Track", new Color(0f, 0f, 0f, 0.6f));
        track.anchorMin = new Vector2(0.5f, 0f);
        track.anchorMax = new Vector2(0.5f, 0f);
        track.pivot = new Vector2(0.5f, 0f);
        track.anchoredPosition = new Vector2(0f, 22f);
        track.sizeDelta = new Vector2(TRACK_W, 26f);

        RectTransform goodZone = KitchenEventManager.MakeBox(track, "Good", new Color(0.35f, 0.6f, 0.3f, 0.8f));
        goodZone.anchorMin = new Vector2(0.5f, 0f); goodZone.anchorMax = new Vector2(0.5f, 1f);
        goodZone.pivot = new Vector2(0.5f, 0.5f);
        goodZone.anchoredPosition = Vector2.zero;
        goodZone.sizeDelta = new Vector2(TRACK_W * 0.4f, 0f);   // +-20%

        RectTransform perfectZone = KitchenEventManager.MakeBox(track, "Perfect", new Color(1f, 0.85f, 0.3f, 0.9f));
        perfectZone.anchorMin = new Vector2(0.5f, 0f); perfectZone.anchorMax = new Vector2(0.5f, 1f);
        perfectZone.pivot = new Vector2(0.5f, 0.5f);
        perfectZone.anchoredPosition = Vector2.zero;
        perfectZone.sizeDelta = new Vector2(TRACK_W * 0.14f, 0f);   // +-7%

        pressureCursor = KitchenEventManager.MakeBox(track, "Cursor", Color.white);
        pressureCursor.anchorMin = new Vector2(0.5f, 0f); pressureCursor.anchorMax = new Vector2(0.5f, 1f);
        pressureCursor.pivot = new Vector2(0.5f, 0.5f);
        pressureCursor.sizeDelta = new Vector2(6f, 8f);

        pressureRoot.SetActive(false);
    }
}
