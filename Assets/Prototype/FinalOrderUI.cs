using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [FinalOrderUI.cs] v2 (교수 피드백 A7/C1/C3 반영 2026-09-14) - C-2: 마지막 주문 (진엔딩 B)
/// 디 오리지널 P3(해치 개방)에서, 자격을 갖춘 요리사에게만 열리는 마지막 선택지.
///
/// - v2 변경점:
///   자격 = (사용자 결정 C1) 선대의 일지 12장 + 전설 요리(T2) 1종 이상 보유(재고 또는 배치). 도감 42종은 명예 보상.
///   그로기 진입 순간 세계를 멈추고 "[R] 대접 / [F] 격파" 선택창 - 포탑이 자동으로 쏘는 7초 동안 R을 못 눌러
///   기회를 잃던 문제(A7) 해결. 조리 중이었으면 무손실 중단 후 선택창.
///   실패 문구는 사실대로: 디 오리지널의 추가 그로기(C3, 12%)가 남았으면 "한 번 더", 없으면 "이번 런엔 없다".
///
/// 흐름:
///  - P3 진입 + 자격 미달: 무엇이 부족한지 힌트 1회 (다회차 동기)
///  - P3 진입 + 자격 + 그로기 시작: 시간 정지 + 선택창 ([R] 대접 / [F] 이대로 격파)
///  - R -> 풀코스 QTE 3라운드 (굽기 문법, 라운드마다 빨라짐)
///  - 2라운드 이상 성공 -> 정찬 대접 -> 엔딩 B / 실패 -> 추가 그로기가 있으면 재도전
///
/// 사용법: 없음! BossGimmickSystem이 디 오리지널전에서 자동 생성.
/// 조건/라운드 수는 GameBalance 'C-2' 섹션에서 조정 (테스트 시 TrueEndingRecipesNeeded를 낮출 것).
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class FinalOrderUI : MonoBehaviour
{
    /// <summary>QTE 진행 중 여부 (PauseMenu 등 외부에서 ESC 충돌 방지용)</summary>
    public static bool QteOpen = false;

    /// <summary>v2: 선택창이 [R]/[F]/[ESC]를 소비한 프레임 - 같은 키를 보는 다른 시스템(보스 투척, 일시정지)이 양보한다</summary>
    public static int KeyConsumedFrame = -1;

    private BossEnemy boss;

    // ── UI ──
    private GameObject canvasGo;
    private GameObject hintGo;
    private Text hintText;
    private GameObject qteRoot;
    private Text roundText;
    private Text feedbackText;
    private RectTransform cursor;

    // ── QTE 상태 ──
    private bool qteActive = false;
    private int round = 0;
    private int successes = 0;
    private float pos = 0f;
    private float dir = 1f;

    // ── 진행 상태 ──
    private bool attemptedThisGroggy = false;
    private bool wasGroggy = false;
    private bool lockedNoticeShown = false;

    // v2: 선택창 (시간 정지)
    private bool choiceOpen = false;
    private GameObject choiceGo;
    private Text choiceText;

    private const float TRACK_W = 520f;
    private const float BASE_SPEED = 95f;

    public void Setup(BossEnemy targetBoss)
    {
        boss = targetBoss;
        BuildUI();
        Debug.Log("[FinalOrder] 마지막 주문 대기 (자격 " + (Qualified() ? "충족" : "미달: " + MissingText()) + ")");
    }

    private void OnDestroy()
    {
        if (canvasGo != null) Destroy(canvasGo);
        if (qteActive || choiceOpen) Time.timeScale = 1f;   // 안전장치
        QteOpen = false;
    }

    /// <summary>
    /// v2 (C1): 마지막 주문 자격. 이정표 방식이면 일지 12장 + T2 요리 1종 보유(재고 또는 배치 포탑).
    /// 세 지역 보스는 최종전에 온 것으로 이미 넘은 상태. 구 방식(도감 42)은 스위치로 남긴다.
    /// </summary>
    public static bool Qualified()
    {
        if (!GameBalance.TrueEndingMilestoneMode)
            return MetaProgress.DiscoveredCount >= GameBalance.TrueEndingRecipesNeeded;
        if (MetaProgress.CollectedJournalCount < GameBalance.TrueEndingJournalsNeeded) return false;
        return CountTier2Kinds() >= Mathf.Max(1, GameBalance.TrueEndingT2Needed);
    }

    /// <summary>지금 손에 있는 전설 요리(T2)의 종류 수 - 재고와 배치 포탑을 합쳐 같은 요리는 1종으로 센다</summary>
    private static int CountTier2Kinds()
    {
        List<string> kinds = new List<string>();
        if (FoodStock.Instance != null)
        {
            foreach (KeyValuePair<string, int> kv in FoodStock.Instance.AllStock)
            {
                if (kv.Value <= 0 || kinds.Contains(kv.Key)) continue;
                RecipeData r = RecipeDatabase.Get(kv.Key);
                if (r != null && r.tier == 2) kinds.Add(kv.Key);
            }
        }
        if (TurretSlotManager.Instance != null)
        {
            TurretSlot[] slots = TurretSlotManager.Instance.slots;
            for (int i = 0; i < slots.Length; i++)
            {
                TurretSlot s = slots[i];
                if (s == null || s.IsEmpty || kinds.Contains(s.recipeId)) continue;
                RecipeData r = s.Recipe;
                if (r != null && r.tier == 2) kinds.Add(s.recipeId);
            }
        }
        return kinds.Count;
    }

    /// <summary>자격 미달일 때 무엇이 부족한지 (힌트 문구)</summary>
    private static string MissingText()
    {
        if (!GameBalance.TrueEndingMilestoneMode)
            return "도감 " + GameBalance.TrueEndingRecipesNeeded + "종 (현재 " + MetaProgress.DiscoveredCount + "종)";
        string s = "";
        if (MetaProgress.CollectedJournalCount < GameBalance.TrueEndingJournalsNeeded)
            s += "선대의 일지 " + GameBalance.TrueEndingJournalsNeeded + "장 (현재 " + MetaProgress.CollectedJournalCount + "장, 폐역 선로에서)";
        int t2Need = Mathf.Max(1, GameBalance.TrueEndingT2Needed);
        if (CountTier2Kinds() < t2Need)
            s += (s.Length > 0 ? " + " : "") + "전설 요리(T2) " + (t2Need == 1 ? "한 접시" : t2Need + "종");
        return s;
    }

    private void Update()
    {
        if (boss == null || !boss.IsAlive)
        {
            if (!qteActive) Destroy(gameObject);
            return;
        }

        // 그로기가 새로 시작될 때마다 도전 기회 갱신
        if (boss.IsGroggy && !wasGroggy)
            attemptedThisGroggy = false;
        wasGroggy = boss.IsGroggy;

        bool phase3 = boss.OriginalPhaseNow >= 3;
        bool qualified = Qualified();

        // 자격 미달 힌트 (전투당 1회 - 다회차 동기)
        if (phase3 && !qualified && !lockedNoticeShown)
        {
            lockedNoticeShown = true;
            UIManager.Instance?.ShowStatChange("다른 결말이 있다... 필요한 것: " + MissingText());
        }

        // v2 (A7): 자격을 갖춘 채 그로기가 시작되면 세계를 멈추고 선택을 묻는다
        bool ready = phase3 && qualified && boss.IsGroggy && !attemptedThisGroggy && !qteActive && !choiceOpen;
        if (ready)
            OpenChoice();

        if (choiceOpen)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                KeyConsumedFrame = Time.frameCount;
                CloseChoice();
                StartQTE();
            }
            else if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Escape))
            {
                KeyConsumedFrame = Time.frameCount;
                CookingMinigame.EscConsumedFrame = Time.frameCount;   // 같은 프레임에 일시정지 메뉴가 열리지 않게
                CloseChoice();
                attemptedThisGroggy = true;
                UIManager.Instance?.ShowStatChange("격파를 택했다 - [F] 디버프 요리로 밀어붙여라");
            }
            return;
        }

        if (qteActive)
            UpdateQTE();
    }

    private void OpenChoice()
    {
        // 조리 중이면 무손실 중단 (재료 환급) - 선택창과 Space 입력이 겹치지 않게
        if (CookingMinigame.IsActive && CookingMinigame.Instance != null)
            CookingMinigame.Instance.AbortExternal();

        choiceOpen = true;
        QteOpen = true;   // PauseMenu 등 외부 ESC 충돌 방지 (같은 플래그 공유)
        Time.timeScale = 0f;
        string second = boss.HasExtraGroggyPending ? "실패해도 마지막 틈이 한 번 더 온다" : "이번 런의 마지막 기회";
        choiceText.text = "대륙에서 가장 오래 굶은 손님이 무릎을 꿇었다.\n\n"
            + "[R]  마지막 식사를 대접한다  (풀코스 3코스 - " + second + ")\n"
            + "[F]  이대로 격파한다  (그로기 중 디버프 요리 투척)";
        choiceGo.SetActive(true);
        hintGo.SetActive(false);
        Debug.Log("[FinalOrder] 선택창 열림 (시간 정지)");
    }

    private void CloseChoice()
    {
        choiceOpen = false;
        QteOpen = false;
        choiceGo.SetActive(false);
        Time.timeScale = 1f;
    }

    // ─────────────────────────────────────────────
    // 풀코스 QTE
    // ─────────────────────────────────────────────
    private void StartQTE()
    {
        qteActive = true;
        QteOpen = true;
        attemptedThisGroggy = true;
        round = 0;
        successes = 0;
        pos = 0f;
        dir = 1f;

        Time.timeScale = 0f;   // 세계가 멈추고 식탁만 남는다
        qteRoot.SetActive(true);
        feedbackText.text = "";
        UpdateRoundLabel();

        Debug.Log("[FinalOrder] 풀코스 QTE 시작");
    }

    private void UpdateQTE()
    {
        // 라운드가 오를수록 빨라진다 (코스가 이어질수록 긴장)
        float speed = BASE_SPEED * (1f + 0.18f * round);
        pos += dir * speed * Time.unscaledDeltaTime;
        if (pos >= 100f) { pos = 100f; dir = -1f; }
        if (pos <= 0f) { pos = 0f; dir = 1f; }

        cursor.anchoredPosition = new Vector2(-TRACK_W / 2f + TRACK_W * (pos / 100f), 0f);

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            JudgeRound();
    }

    private void JudgeRound()
    {
        float d = Mathf.Abs(pos - 50f);
        if (d <= 7f)
        {
            successes++;
            feedbackText.text = "PERFECT!";
            SoundManager.Play("sfx_judge_perfect");
        }
        else if (d <= 20f)
        {
            successes++;
            feedbackText.text = "Good";
            SoundManager.Play("sfx_judge_good");
        }
        else
        {
            feedbackText.text = "탔다...";
            SoundManager.Play("sfx_judge_bad");
        }

        round++;
        pos = 0f;
        dir = 1f;

        if (round >= GameBalance.FinalOrderRounds)
            Resolve();
        else
            UpdateRoundLabel();
    }

    private void UpdateRoundLabel()
    {
        string course = round == 0 ? "전채" : (round == 1 ? "본식" : "후식");
        roundText.text = "코스 " + (round + 1) + "/" + GameBalance.FinalOrderRounds
            + "  [" + course + "]   정중앙에서 [Space]";
    }

    private void Resolve()
    {
        qteActive = false;
        QteOpen = false;
        qteRoot.SetActive(false);
        Time.timeScale = 1f;

        bool win = successes >= GameBalance.FinalOrderNeeded;
        Debug.Log("[FinalOrder] 풀코스 결과: " + successes + "/" + GameBalance.FinalOrderRounds
            + (win ? " - 대접 성공" : " - 실패"));

        if (win)
        {
            hintGo.SetActive(false);
            boss.ServeLastSupper();
        }
        else
        {
            // v2 (A7/C3): 재도전 정책과 문구를 맞춘다
            if (boss.HasExtraGroggyPending)
                UIManager.Instance?.ShowStatChange("손이 떨렸다... 요리가 식었다. 손님이 한 번 더 무릎을 꿇을 때 다시 - HP "
                    + Mathf.RoundToInt(GameBalance.OriginalExtraGroggyRatio * 100f) + "%");
            else
                UIManager.Instance?.ShowStatChange("손이 떨렸다... 요리가 식었다. 이번 런에는 기회가 없다 - 격파로 간다");
        }
    }

    // ─────────────────────────────────────────────
    // UI 생성
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        canvasGo = new GameObject("FinalOrderCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 645;   // 경고(640) 위, 스토리(650) 아래
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── [R] 힌트 (우측 중단, 금색) ──
        RectTransform hint = KitchenEventManager.MakeBox(canvasGo.transform, "Hint",
            new Color(0.12f, 0.09f, 0.04f, 0.92f));
        hint.anchorMin = new Vector2(1f, 0.5f);
        hint.anchorMax = new Vector2(1f, 0.5f);
        hint.pivot = new Vector2(1f, 0.5f);
        hint.anchoredPosition = new Vector2(-14f, -60f);
        hint.sizeDelta = new Vector2(380f, 64f);
        hintGo = hint.gameObject;

        hintText = KitchenEventManager.MakeText(hint, "Text",
            "[R] 마지막 식사를 대접한다", 22, new Color(1f, 0.85f, 0.35f));
        RectTransform htRt = hintText.rectTransform;
        htRt.anchorMin = Vector2.zero;
        htRt.anchorMax = Vector2.one;
        htRt.offsetMin = Vector2.zero;
        htRt.offsetMax = Vector2.zero;

        hintGo.SetActive(false);

        // ── v2: 선택창 (시간 정지, 중앙) ──
        RectTransform choice = KitchenEventManager.MakeBox(canvasGo.transform, "Choice",
            new Color(0.10f, 0.08f, 0.05f, 0.96f));
        choice.anchorMin = new Vector2(0.5f, 0.5f);
        choice.anchorMax = new Vector2(0.5f, 0.5f);
        choice.pivot = new Vector2(0.5f, 0.5f);
        choice.anchoredPosition = new Vector2(0f, 60f);
        choice.sizeDelta = new Vector2(760f, 200f);
        choiceGo = choice.gameObject;

        Text choiceTitle = KitchenEventManager.MakeText(choice, "Title", "마지막 주문", 26,
            new Color(1f, 0.8f, 0.35f));
        RectTransform ctRt = choiceTitle.rectTransform;
        ctRt.anchorMin = new Vector2(0f, 1f);
        ctRt.anchorMax = new Vector2(1f, 1f);
        ctRt.pivot = new Vector2(0.5f, 1f);
        ctRt.anchoredPosition = new Vector2(0f, -12f);
        ctRt.sizeDelta = new Vector2(-20f, 34f);

        choiceText = KitchenEventManager.MakeText(choice, "Body", "", 20, new Color(0.92f, 0.9f, 0.82f));
        RectTransform cbRt = choiceText.rectTransform;
        cbRt.anchorMin = Vector2.zero;
        cbRt.anchorMax = Vector2.one;
        cbRt.offsetMin = new Vector2(24f, 16f);
        cbRt.offsetMax = new Vector2(-24f, -52f);
        choiceGo.SetActive(false);

        // ── 풀코스 QTE 오버레이 (거대한 주문서) ──
        RectTransform order = KitchenEventManager.MakeBox(canvasGo.transform, "OrderSheet",
            new Color(0.09f, 0.07f, 0.05f, 0.97f));
        order.anchorMin = new Vector2(0.5f, 0.5f);
        order.anchorMax = new Vector2(0.5f, 0.5f);
        order.pivot = new Vector2(0.5f, 0.5f);
        order.anchoredPosition = new Vector2(0f, 40f);
        order.sizeDelta = new Vector2(640f, 340f);
        qteRoot = order.gameObject;

        Text title = KitchenEventManager.MakeText(order, "Title",
            "마지막 주문 - 대륙에서 가장 오래 굶은 손님의, 첫 주문", 24,
            new Color(1f, 0.8f, 0.35f));
        RectTransform tRt = title.rectTransform;
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -14f);
        tRt.sizeDelta = new Vector2(-20f, 34f);

        roundText = KitchenEventManager.MakeText(order, "Round", "", 21,
            new Color(0.92f, 0.9f, 0.82f));
        RectTransform rRt = roundText.rectTransform;
        rRt.anchorMin = new Vector2(0f, 1f);
        rRt.anchorMax = new Vector2(1f, 1f);
        rRt.pivot = new Vector2(0.5f, 1f);
        rRt.anchoredPosition = new Vector2(0f, -58f);
        rRt.sizeDelta = new Vector2(0f, 28f);

        feedbackText = KitchenEventManager.MakeText(order, "Feedback", "", 30,
            new Color(1f, 0.85f, 0.4f));
        RectTransform fRt = feedbackText.rectTransform;
        fRt.anchorMin = new Vector2(0f, 1f);
        fRt.anchorMax = new Vector2(1f, 1f);
        fRt.pivot = new Vector2(0.5f, 1f);
        fRt.anchoredPosition = new Vector2(0f, -100f);
        fRt.sizeDelta = new Vector2(0f, 40f);

        // 판정 트랙 (굽기 문법)
        RectTransform track = KitchenEventManager.MakeBox(order, "Track", new Color(0f, 0f, 0f, 0.6f));
        track.anchorMin = new Vector2(0.5f, 0f);
        track.anchorMax = new Vector2(0.5f, 0f);
        track.pivot = new Vector2(0.5f, 0f);
        track.anchoredPosition = new Vector2(0f, 60f);
        track.sizeDelta = new Vector2(TRACK_W, 34f);

        RectTransform goodZone = KitchenEventManager.MakeBox(track, "Good", new Color(0.35f, 0.6f, 0.3f, 0.8f));
        goodZone.anchorMin = new Vector2(0.5f, 0f); goodZone.anchorMax = new Vector2(0.5f, 1f);
        goodZone.pivot = new Vector2(0.5f, 0.5f);
        goodZone.anchoredPosition = Vector2.zero;
        goodZone.sizeDelta = new Vector2(TRACK_W * 0.4f, 0f);

        RectTransform perfectZone = KitchenEventManager.MakeBox(track, "Perfect", new Color(1f, 0.85f, 0.3f, 0.9f));
        perfectZone.anchorMin = new Vector2(0.5f, 0f); perfectZone.anchorMax = new Vector2(0.5f, 1f);
        perfectZone.pivot = new Vector2(0.5f, 0.5f);
        perfectZone.anchoredPosition = Vector2.zero;
        perfectZone.sizeDelta = new Vector2(TRACK_W * 0.14f, 0f);

        cursor = KitchenEventManager.MakeBox(track, "Cursor", Color.white);
        cursor.anchorMin = new Vector2(0.5f, 0f); cursor.anchorMax = new Vector2(0.5f, 1f);
        cursor.pivot = new Vector2(0.5f, 0.5f);
        cursor.sizeDelta = new Vector2(7f, 10f);

        Text subText = KitchenEventManager.MakeText(order, "Sub",
            "세 코스 중 " + GameBalance.FinalOrderNeeded + "번 이상 성공하면 식사가 완성된다", 17,
            new Color(0.7f, 0.68f, 0.6f));
        RectTransform sRt = subText.rectTransform;
        sRt.anchorMin = new Vector2(0f, 0f);
        sRt.anchorMax = new Vector2(1f, 0f);
        sRt.pivot = new Vector2(0.5f, 0f);
        sRt.anchoredPosition = new Vector2(0f, 20f);
        sRt.sizeDelta = new Vector2(0f, 26f);

        qteRoot.SetActive(false);
    }
}
