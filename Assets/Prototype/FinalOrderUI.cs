using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [FinalOrderUI.cs] v1 (신규 파일) - C-2: 마지막 주문 (진엔딩 B)
/// 디 오리지널 P3(해치 개방)에서, 도감을 완성한 요리사에게만 열리는 마지막 선택지.
///
/// 흐름:
///  - P3 진입 + 도감 미완성: "다른 결말이 있다" 힌트 1회 (다회차 동기)
///  - P3 진입 + 도감 완성 + 그로기 중: "[R] 마지막 식사를 대접한다" 표시
///  - R 입력 -> 세계가 멈추고(일시정지) 풀코스 QTE 3라운드 (굽기 문법, 라운드마다 빨라짐)
///  - 2라운드 이상 성공 -> 정찬 대접 -> 엔딩 B / 실패 -> 다음 그로기에 재도전
///
/// 사용법: 없음! BossGimmickSystem이 디 오리지널전에서 자동 생성.
/// 조건/라운드 수는 GameBalance 'C-2' 섹션에서 조정 (테스트 시 TrueEndingRecipesNeeded를 낮출 것).
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class FinalOrderUI : MonoBehaviour
{
    /// <summary>QTE 진행 중 여부 (PauseMenu 등 외부에서 ESC 충돌 방지용)</summary>
    public static bool QteOpen = false;

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

    private const float TRACK_W = 520f;
    private const float BASE_SPEED = 95f;

    public void Setup(BossEnemy targetBoss)
    {
        boss = targetBoss;
        BuildUI();
        Debug.Log("[FinalOrder] 마지막 주문 대기 (도감 "
            + MetaProgress.DiscoveredCount + "/" + GameBalance.TrueEndingRecipesNeeded + ")");
    }

    private void OnDestroy()
    {
        if (canvasGo != null) Destroy(canvasGo);
        if (qteActive) Time.timeScale = 1f;   // 안전장치
        QteOpen = false;
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
        bool haveRecipes = MetaProgress.DiscoveredCount >= GameBalance.TrueEndingRecipesNeeded;

        // 도감 미완성 힌트 (전투당 1회 - 다회차 수집 동기)
        if (phase3 && !haveRecipes && !lockedNoticeShown)
        {
            lockedNoticeShown = true;
            UIManager.Instance?.ShowStatChange("도감 " + GameBalance.TrueEndingRecipesNeeded
                + "종을 완성한 요리사에게는 다른 결말이 있다... (현재 "
                + MetaProgress.DiscoveredCount + "종)");
        }

        // [R] 힌트 표시 조건
        bool ready = phase3 && haveRecipes && boss.IsGroggy && !attemptedThisGroggy && !qteActive;
        if (hintGo.activeSelf != ready)
            hintGo.SetActive(ready);

        // 조리 미니게임 중에는 시작 불가 (Space 입력이 겹치는 사고 방지)
        if (ready && Input.GetKeyDown(KeyCode.R) && !CookingMinigame.IsActive)
            StartQTE();

        if (qteActive)
            UpdateQTE();
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
            UIManager.Instance?.ShowStatChange("손이 떨렸다... 요리가 식었다. (다음 그로기에 다시)");
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
