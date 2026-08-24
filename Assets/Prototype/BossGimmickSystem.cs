using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [BossGimmickSystem.cs] v4
/// 보스전 전용 기믹 + 보스 UI를 관리합니다.
///
/// - v4 변경점 (UI 재작성):
///   하이어라키 수동 패널 전부 제거 -> UI를 코드로 자동 생성 (겹침 문제 해결)
///   * 상단 중앙: 보스 이름 + HP 바 + 수치
///   * 그 아래: 그로기 배너 (안내 문구 + 남은 시간 게이지)
///   씬 세팅 필요 없음 - 기존 보스 HP/그로기 패널은 하이어라키에서 삭제할 것
///
/// 동작 흐름:
///   보스 HP 75/50/25% 도달 -> 그로기 발동 (10초 정지)
///   그로기 중 보유한 디버프 요리를 자동 탐색해 표시
///   F키 -> FoodStock에서 1개 소모 -> 보스 DEF/RES 무력화
///
/// 사용법: 씬에 빈 오브젝트 -> 이 스크립트 붙이기 (UI 자동 생성)
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class BossGimmickSystem : MonoBehaviour
{
    public static BossGimmickSystem Instance { get; private set; }

    [Header("─ 설정 ─")]
    public float groggyDuration = 10f;        // 그로기 지속 시간 (BossEnemy와 맞출 것)

    // ─────────────────────────────────────────────
    // 내부 상태
    // ─────────────────────────────────────────────
    private BossEnemy currentBoss = null;
    private bool isGroggyPhase = false;
    private float groggyTimer = 0f;
    private bool hasThrownThisGroggy = false;
    private float guideRefreshTimer = 0f;

    // ─────────────────────────────────────────────
    // 코드 생성 UI
    // ─────────────────────────────────────────────
    private Canvas canvas;
    private RectTransform bossRoot;       // 보스 이름 + HP 바
    private Text bossNameText;
    private Text bossHPText;
    private RectTransform hpFill;
    private RectTransform groggyRoot;     // 그로기 배너
    private Text groggyGuideText;
    private RectTransform groggyTimeFill;
    private Image groggyTimeFillImg;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    private void Start()
    {
        bossRoot.gameObject.SetActive(false);
        groggyRoot.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // 보스 등록 (BossEnemy.Start에서 호출)
    // ─────────────────────────────────────────────
    /// <summary>보스가 살아서 활동 중인가? (주방 이벤트 차단 등 외부 참조용)</summary>
    public bool HasActiveBoss
    {
        get { return currentBoss != null && currentBoss.IsAlive; }
    }

    public void RegisterBoss(BossEnemy boss)
    {
        currentBoss = boss;

        // v4.1: 그로기 시간을 보스 쪽 설정과 자동 동기화 (게이지 바 길이 불일치 방지)
        groggyDuration = boss.groggyDuration;

        bossRoot.gameObject.SetActive(true);
        // v5: 보스 4종 개성화 - 등록된 보스의 실제 이름 표시
        if (bossNameText != null) bossNameText.text = boss.data.enemyName;

        // v5.1: 동면자 보스전이면 해동포 UI 자동 생성 (씬 세팅 불필요)
        if (boss.kind == BossEnemy.BossKind.Hibernator)
        {
            GameObject cannonGo = new GameObject("ThawCannon");
            cannonGo.AddComponent<ThawCannonUI>().Setup(boss);
        }

        // v5.2 (C단계): 녹슨 발톱 보스전이면 미끼 화덕 자동 생성
        if (boss.kind == BossEnemy.BossKind.RustClaw)
        {
            GameObject baitGo = new GameObject("BaitStation");
            baitGo.AddComponent<BaitStationUI>().Setup(boss);
        }

        // v5.3 (C-2): 디 오리지널 보스전이면 '마지막 주문' UI 자동 생성 (엔딩 B 분기 담당)
        if (boss.kind == BossEnemy.BossKind.Original)
        {
            GameObject finalGo = new GameObject("FinalOrder");
            finalGo.AddComponent<FinalOrderUI>().Setup(boss);
        }

        Debug.Log("[BossGimmickSystem] 보스 등록 완료: " + boss.data.enemyName);
    }

    // ─────────────────────────────────────────────
    // v5: 패턴 예고(텔레그래프) 배너 - 그로기 배너 재사용
    // ─────────────────────────────────────────────
    private Coroutine telegraphCo = null;

    /// <summary>보스 패턴 예고 표시. seconds 후 자동으로 숨김 (그로기가 시작되면 그로기가 우선)</summary>
    public void ShowPatternTelegraph(string text, float seconds)
    {
        if (isGroggyPhase) return;   // 그로기 안내가 우선

        groggyRoot.gameObject.SetActive(true);
        if (groggyGuideText != null) groggyGuideText.text = text;
        if (groggyTimeFillImg != null) groggyTimeFillImg.color = new Color(0.8f, 0.3f, 0.9f); // 보라 = 패턴 예고
        SetFill(groggyTimeFill, 1f);
        SoundManager.Play("sfx_boss_warning");   // 예고 경보음

        // v5.2 (감사 2-D): 화면 가장자리 붉은 플래시 + 대형 경고 - 예고가 눈에 확 들어오게
        WarningFX.Flash(text, seconds);

        if (telegraphCo != null) StopCoroutine(telegraphCo);
        telegraphCo = StartCoroutine(TelegraphCountdown(seconds));
    }

    private IEnumerator TelegraphCountdown(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            if (isGroggyPhase) yield break;   // 그로기 시작되면 배너 넘겨줌
            SetFill(groggyTimeFill, 1f - Mathf.Clamp01(t / seconds));
            yield return null;
        }

        if (!isGroggyPhase)
            groggyRoot.gameObject.SetActive(false);
        telegraphCo = null;
    }

    // ─────────────────────────────────────────────
    // 매 프레임
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (currentBoss == null || !currentBoss.IsAlive)
        {
            if (bossRoot.gameObject.activeSelf) bossRoot.gameObject.SetActive(false);
            if (groggyRoot.gameObject.activeSelf) groggyRoot.gameObject.SetActive(false);
            return;
        }

        UpdateBossHPBar();

        if (!isGroggyPhase && currentBoss.IsGroggy)
            StartGroggyPhase();

        if (isGroggyPhase)
            UpdateGroggyPhase();
    }

    private void UpdateBossHPBar()
    {
        float ratio = Mathf.Clamp01(currentBoss.currentHP / Mathf.Max(1f, currentBoss.bossMaxHP));
        SetFill(hpFill, ratio);

        if (bossHPText != null)
            bossHPText.text = (int)currentBoss.currentHP + " / " + (int)currentBoss.bossMaxHP;

        // v5.1: 천둥 둥지 - 번개 병 충전 수 표시
        if (bossNameText != null && currentBoss.kind == BossEnemy.BossKind.ThunderNest)
            bossNameText.text = currentBoss.data.enemyName + "   [번개 병 "
                + currentBoss.ParryCharges + "/" + GameBalance.ParryChargesForCounter + "]";
    }

    // ─────────────────────────────────────────────
    // 그로기 페이즈
    // ─────────────────────────────────────────────
    private void StartGroggyPhase()
    {
        isGroggyPhase = true;
        groggyTimer = 0f;
        hasThrownThisGroggy = false;
        guideRefreshTimer = 0f;

        // v5: 패턴 예고가 떠 있었으면 중단하고 그로기가 배너를 가져간다
        if (telegraphCo != null) { StopCoroutine(telegraphCo); telegraphCo = null; }

        // v5: 보너스 그로기(빙하 갑주 파괴 등)는 보스가 지정한 짧은 시간 사용
        if (currentBoss != null && currentBoss.CurrentGroggyDuration > 0f)
            groggyDuration = currentBoss.CurrentGroggyDuration;

        groggyRoot.gameObject.SetActive(true);
        if (groggyTimeFillImg != null) groggyTimeFillImg.color = new Color(1f, 0.55f, 0.15f);
        SoundManager.Play("sfx_boss_groggy");

        // v5.2: 그로기는 기회의 순간 - 금색 플래시로 구분
        WarningFX.Flash("보스 그로기! [F] 디버프 요리 투척!", 1.6f, new Color(1f, 0.8f, 0.2f));
        RefreshGuideText();

        Debug.Log("[BossGimmickSystem] 보스 그로기 발동! 10초 안에 디버프 요리 투척!");
        UIManager.Instance?.ShowStatChange("보스 그로기!! F키로 투척!");
    }

    private void UpdateGroggyPhase()
    {
        groggyTimer += Time.deltaTime;
        SetFill(groggyTimeFill, 1f - Mathf.Clamp01(groggyTimer / groggyDuration));

        // 안내 문구 주기 갱신 (그로기 중에 요리를 새로 만들 수도 있으므로)
        guideRefreshTimer -= Time.deltaTime;
        if (guideRefreshTimer <= 0f && !hasThrownThisGroggy)
        {
            guideRefreshTimer = 1f;
            RefreshGuideText();
        }

        if (Input.GetKeyDown(KeyCode.F))
            TryThrowDebuffFood();

        if (!currentBoss.IsGroggy || groggyTimer >= groggyDuration)
            EndGroggyPhase(hasThrownThisGroggy);
    }

    // ─────────────────────────────────────────────
    // 디버프 요리 탐색 (FoodStock + RecipeDatabase 자동 판정)
    // ─────────────────────────────────────────────

    /// <summary>보유 요리 중 가장 좋은 디버프 요리. 없으면 null</summary>
    private RecipeData FindBestDebuffFood()
    {
        if (FoodStock.Instance == null) return null;

        RecipeData best = null;
        int bestScore = 0;

        foreach (KeyValuePair<string, int> pair in FoodStock.Instance.AllStock)
        {
            if (pair.Value <= 0) continue;

            RecipeData r = RecipeDatabase.Get(pair.Key);
            if (r == null) continue;

            int score = GetDebuffScore(r);
            if (score > bestScore)
            {
                bestScore = score;
                best = r;
            }
        }

        return best;
    }

    /// <summary>디버프 요리 점수. 0이면 디버프 요리가 아님</summary>
    private int GetDebuffScore(RecipeData r)
    {
        int score = 0;
        if (r.shredDef > 0) score += r.shredDef * 100;
        if (r.shredRes > 0) score += r.shredRes * 100;
        if (r.stunSec > 0f) score += 50;
        if (r.slowLevel >= 2) score += 30;

        if (score > 0) score += r.tier * 10;
        return score;
    }

    /// <summary>계열별 디버프 강도 (남는 방어력 비율 - 낮을수록 강력)</summary>
    private float GetDebuffPower(RecipeData r)
    {
        if (r.shredDef > 0 || r.shredRes > 0) return 0.25f;  // 부식 계열
        if (r.stunSec > 0f) return 0.40f;                    // 마비 계열
        return 0.50f;                                        // 빙결 계열
    }

    private void RefreshGuideText()
    {
        if (groggyGuideText == null || hasThrownThisGroggy) return;

        RecipeData found = FindBestDebuffFood();
        if (found != null)
            groggyGuideText.text = "그로기!  [F] " + found.displayName + " 투척!";
        else
            groggyGuideText.text = "디버프 요리 없음!  독침 육포(고기+독) 등을 조리하라!";
    }

    private void TryThrowDebuffFood()
    {
        if (hasThrownThisGroggy) return;

        RecipeData food = FindBestDebuffFood();
        if (food == null)
        {
            RefreshGuideText();
            return;
        }

        if (FoodStock.Instance == null || !FoodStock.Instance.TryConsume(food.recipeId, 1))
            return;

        hasThrownThisGroggy = true;

        float power = GetDebuffPower(food);
        currentBoss.ReceiveDebuffFood(power);

        int reducedPct = Mathf.RoundToInt((1f - power) * 100f);
        Debug.Log("[BossGimmickSystem] " + food.displayName + " 투척! 보스 방어력 " + reducedPct + "% 감소!");
        UIManager.Instance?.ShowStatChange(food.displayName + " 적중! 보스 방어력 -" + reducedPct + "%!");

        if (groggyGuideText != null)
            groggyGuideText.text = food.displayName + " 적중!  방어력 -" + reducedPct + "%";
        if (groggyTimeFillImg != null)
            groggyTimeFillImg.color = new Color(0.25f, 0.9f, 0.3f);
    }

    private void EndGroggyPhase(bool wasDebuffed)
    {
        isGroggyPhase = false;
        groggyRoot.gameObject.SetActive(false);

        if (!wasDebuffed)
        {
            Debug.Log("[BossGimmickSystem] 그로기 미투척 - 기회를 놓쳤다!");
            UIManager.Instance?.ShowStatChange("투척 실패! 그로기 기회를 놓쳤다!");
        }
    }

    // ─────────────────────────────────────────────
    // 보스 처치 시 정리
    // ─────────────────────────────────────────────
    public void OnBossDefeated()
    {
        currentBoss = null;
        isGroggyPhase = false;
        bossRoot.gameObject.SetActive(false);
        groggyRoot.gameObject.SetActive(false);

        Debug.Log("[BossGimmickSystem] 보스 처치!");
        UIManager.Instance?.ShowStatChange("보스 처치! 승리!");
    }

    // ─────────────────────────────────────────────
    // UI 생성 (코드 생성 - 씬 세팅 불필요)
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("BossUICanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 480;   // 주방 이벤트(500)/정비소(550)/증강(600)보다 아래
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // ---------- 보스 HP 바 (상단 중앙) ----------
        bossRoot = KitchenEventManager.MakeBox(canvasGo.transform, "BossBar", new Color(0.08f, 0.06f, 0.05f, 0.88f));
        bossRoot.anchorMin = new Vector2(0.5f, 1f);
        bossRoot.anchorMax = new Vector2(0.5f, 1f);
        bossRoot.pivot = new Vector2(0.5f, 1f);
        bossRoot.anchoredPosition = new Vector2(0f, -150f);
        bossRoot.sizeDelta = new Vector2(700f, 64f);
        bossRoot.GetComponent<Image>().raycastTarget = false;

        // 보스 이름 (좌측)
        bossNameText = KitchenEventManager.MakeText(bossRoot, "Name", "메카 티렉스 보스", 20, new Color(1f, 0.45f, 0.35f));
        RectTransform nRt = bossNameText.rectTransform;
        nRt.anchorMin = new Vector2(0f, 1f);
        nRt.anchorMax = new Vector2(0.5f, 1f);
        nRt.pivot = new Vector2(0f, 1f);
        nRt.anchoredPosition = new Vector2(14f, -4f);
        nRt.sizeDelta = new Vector2(0f, 24f);
        bossNameText.alignment = TextAnchor.MiddleLeft;

        // HP 바 배경
        RectTransform hpBg = KitchenEventManager.MakeBox(bossRoot, "HPBG", new Color(0f, 0f, 0f, 0.6f));
        hpBg.anchorMin = new Vector2(0f, 0f);
        hpBg.anchorMax = new Vector2(1f, 0f);
        hpBg.pivot = new Vector2(0.5f, 0f);
        hpBg.offsetMin = new Vector2(14f, 8f);
        hpBg.offsetMax = new Vector2(-14f, 8f);
        hpBg.sizeDelta = new Vector2(hpBg.sizeDelta.x, 26f);
        hpBg.GetComponent<Image>().raycastTarget = false;

        // HP 채움 (빨강)
        hpFill = KitchenEventManager.MakeBox(hpBg, "HPFill", new Color(0.85f, 0.2f, 0.15f));
        hpFill.anchorMin = new Vector2(0f, 0f);
        hpFill.anchorMax = new Vector2(1f, 1f);
        hpFill.offsetMin = Vector2.zero;
        hpFill.offsetMax = Vector2.zero;
        hpFill.GetComponent<Image>().raycastTarget = false;

        // HP 수치 (바 위 중앙)
        bossHPText = KitchenEventManager.MakeText(hpBg, "HPText", "", 17, Color.white);
        RectTransform hRt = bossHPText.rectTransform;
        hRt.anchorMin = Vector2.zero;
        hRt.anchorMax = Vector2.one;
        hRt.offsetMin = Vector2.zero;
        hRt.offsetMax = Vector2.zero;

        // ---------- 그로기 배너 (보스 바 아래) ----------
        groggyRoot = KitchenEventManager.MakeBox(canvasGo.transform, "GroggyBanner", new Color(0.25f, 0.08f, 0.05f, 0.92f));
        groggyRoot.anchorMin = new Vector2(0.5f, 1f);
        groggyRoot.anchorMax = new Vector2(0.5f, 1f);
        groggyRoot.pivot = new Vector2(0.5f, 1f);
        groggyRoot.anchoredPosition = new Vector2(0f, -222f);
        groggyRoot.sizeDelta = new Vector2(700f, 66f);
        groggyRoot.GetComponent<Image>().raycastTarget = false;

        // 안내 문구 (한 줄, 겹침 없음)
        groggyGuideText = KitchenEventManager.MakeText(groggyRoot, "Guide", "", 22, new Color(1f, 0.85f, 0.4f));
        RectTransform gRt = groggyGuideText.rectTransform;
        gRt.anchorMin = new Vector2(0f, 1f);
        gRt.anchorMax = new Vector2(1f, 1f);
        gRt.pivot = new Vector2(0.5f, 1f);
        gRt.anchoredPosition = new Vector2(0f, -6f);
        gRt.sizeDelta = new Vector2(-20f, 32f);

        // 남은 시간 게이지 (하단)
        RectTransform tBg = KitchenEventManager.MakeBox(groggyRoot, "TimeBG", new Color(0f, 0f, 0f, 0.55f));
        tBg.anchorMin = new Vector2(0f, 0f);
        tBg.anchorMax = new Vector2(1f, 0f);
        tBg.pivot = new Vector2(0.5f, 0f);
        tBg.offsetMin = new Vector2(14f, 8f);
        tBg.offsetMax = new Vector2(-14f, 8f);
        tBg.sizeDelta = new Vector2(tBg.sizeDelta.x, 12f);
        tBg.GetComponent<Image>().raycastTarget = false;

        groggyTimeFill = KitchenEventManager.MakeBox(tBg, "TimeFill", new Color(1f, 0.55f, 0.15f));
        groggyTimeFill.anchorMin = new Vector2(0f, 0f);
        groggyTimeFill.anchorMax = new Vector2(1f, 1f);
        groggyTimeFill.offsetMin = Vector2.zero;
        groggyTimeFill.offsetMax = Vector2.zero;
        groggyTimeFillImg = groggyTimeFill.GetComponent<Image>();
        groggyTimeFillImg.raycastTarget = false;
    }

    /// <summary>게이지 채움 비율 (0~1)</summary>
    private void SetFill(RectTransform fill, float ratio)
    {
        if (fill == null) return;
        Vector2 max = fill.anchorMax;
        max.x = Mathf.Clamp01(ratio);
        fill.anchorMax = max;
        fill.offsetMax = new Vector2(0f, fill.offsetMax.y);
    }
}
