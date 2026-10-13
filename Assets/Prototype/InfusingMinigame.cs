using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [InfusingMinigame.cs] v1.1 (v9.21.1 2026-10-13 4차 피드백 1·2·4번: 라운드마다 "읽을 시간"(커서 정지, [Space] 로 바로 시작) + 2라운드에 바늘을 그린다(구간 안이면 금색 - 예전엔 채움 바 끝이 바늘이라 뭘 어디에 두라는지 몰랐다) + 안내를 한 줄씩 크게 + 판 560x260 /
///   접시 합성도 같은 미니게임(BeginForStock - 끝나면 CookingBridge.CompleteStockFusion, 보너스 = 접시 +1) / 창이 떠 있으면 보류(CookingMinigame.WindowOpen)) /
/// v1 (신규 파일) - P1: T2 인퓨징 미니게임 (감사 1-A 처방 2)
///
/// 배경: 런의 가장 큰 파워 스파이크(T2 전설 포탑 탄생)가 클릭 두 번이었다.
/// "가장 큰 순간에 손이 가장 한가한 것은 정체성 위반" - 이제 T2 진화는 직접 조리한다.
///
/// 흐름: 다른 T1 포탑 2개 합체 시도 -> 이 미니게임 자동 시작 (TurretSlotManager가 호출)
///  - 1라운드 [정수 추출] : 굽기 문법 - 타이밍 바 1회 (Space/클릭)
///  - 2라운드 [융합 안정화]: 끓이기 문법 - 움직이는 존에 게이지 유지 (Space 홀드)
///  - 판정 합계(라운드당 PERFECT 2 / Good 1)가 기준 이상이면 T2가 +1레벨로 탄생
///  - 실패해도 진화는 성공 (보너스만 없음 - 벌점 없음, 도파민 보존)
///  - ESC = 인퓨징 중단 (포탑 2개 그대로, 아무 일 없음)
///
/// 사용법: 없음! 파일만 넣으면 TurretSlotManager가 알아서 호출한다.
/// 수치는 GameBalance '인퓨징' 섹션. VS 2017 (C# 7.3) 호환.
/// </summary>
public class InfusingMinigame : MonoBehaviour
{
    /// <summary>인퓨징 진행 중 여부 (합체/조리/일시정지 충돌 방지용)</summary>
    public static bool IsActive = false;

    // ── 대상 ──
    private int idxA, idxB;
    private RecipeData fusion;
    private TurretSlotManager manager;
    // v1.1: 접시 합성 (주방 창) - 포탑 대신 인벤토리 접시 둘
    private bool stockMode = false;
    private string stockA = "", stockB = "";
    private float readTimer = 0f;        // v1.1: 라운드 앞 읽을 시간 (커서가 서 있다)
    private RectTransform needleRect;    // v1.1: 2라운드 바늘
    private Text subText;                // v1.1: 둘째 줄 (점수·보너스 규칙)
    private CanvasGroup panelGroup;      // v1.1: 보류 때 흐리기
    private bool heldByWindow = false;

    // ── 진행 상태 ──
    private int phase = 0;            // 0=굽기, 1=끓이기, 2=종료 연출
    private int score = 0;            // 판정 합계 (최대 4)
    private float endTimer = 0f;

    // ── 1라운드: 타이밍 바 ──
    private float bar = 0f;
    private float dir = 1f;

    // ── 2라운드: 압력 유지 ──
    private float gauge = 50f;
    private float zonePhase = 0f;
    private float zoneCenter = 50f;
    private float holdTimer = 0f;
    private float inZone = 0f;

    // ── UI ──
    private GameObject canvasGo;
    private Text titleText;
    private Text infoText;
    private Text judgeText;
    private float judgeTimer = 0f;
    private RectTransform track;
    private RectTransform zoneRect;
    private RectTransform cursorRect;
    private RectTransform fillRect;

    private const float TRACK_W = 480f;   // v1.1: 380 -> 480

    // ─────────────────────────────────────────────
    // 시작 (TurretSlotManager.TryMergeSlots의 T2 분기가 호출)
    // ─────────────────────────────────────────────
    public static void Begin(int slotA, int slotB, RecipeData fusionRecipe, TurretSlotManager mgr)
    {
        if (IsActive) return;
        GameObject go = new GameObject("InfusingMinigame");
        InfusingMinigame m = go.AddComponent<InfusingMinigame>();
        m.idxA = slotA;
        m.idxB = slotB;
        m.fusion = fusionRecipe;
        m.manager = mgr;
        m.Setup();
    }

    /// <summary>v1.1: 접시 합성 (주방 창의 합성 탭에서 - CookingBridge.FuseFoods). 끝나면 CookingBridge.CompleteStockFusion 이 접시를 소모하고 전설 접시를 준다</summary>
    public static void BeginForStock(string recipeIdA, string recipeIdB, RecipeData fusionRecipe)
    {
        if (IsActive || fusionRecipe == null) return;
        GameObject go = new GameObject("InfusingMinigame");
        InfusingMinigame m = go.AddComponent<InfusingMinigame>();
        m.stockMode = true; m.stockA = recipeIdA; m.stockB = recipeIdB;
        m.fusion = fusionRecipe;
        m.Setup();
    }

    private void Setup()
    {
        IsActive = true;
        phase = 0;
        score = 0;
        bar = 0f; dir = 1f;
        readTimer = Mathf.Max(0f, GameBalance.InfuseReadSec);   // v1.1: 첫 라운드 앞 읽을 시간

        BuildUI();
        titleText.text = "진화 조리  -  " + fusion.displayName + " [전설]";
        SetPhaseInfo();

        Debug.Log("[Infusing] 인퓨징 시작 -> " + fusion.displayName + (stockMode ? " (접시)" : ""));
    }

    private void OnDestroy()
    {
        IsActive = false;
        if (canvasGo != null) Destroy(canvasGo);
    }

    // ─────────────────────────────────────────────
    // 매 프레임
    // ─────────────────────────────────────────────
    private void Update()
    {
        // 게임이 끝났으면 조용히 중단
        GameManager gm = GameManager.Instance;
        if (gm != null && (gm.currentState == GameManager.GameState.GameOver
                        || gm.currentState == GameManager.GameState.Victory))
        {
            Destroy(gameObject);
            return;
        }

        // v1.1: 창(증강·행상인·베팅·정비소·카드·일지·일시정지·선로 고르기)이 떠 있으면 보류 - 시간도 입력도 안 받는다
        bool hold = GameBalance.CookHoldWhileWindow && CookingMinigame.WindowOpen;
        if (hold != heldByWindow)
        {
            heldByWindow = hold;
            if (panelGroup != null) panelGroup.alpha = hold ? 0.55f : 1f;
            if (hold) ShowJudge("창을 닫으면 이어진다", new Color(0.85f, 0.8f, 0.7f));
            else if (judgeText != null && judgeText.text == "창을 닫으면 이어진다") judgeText.text = "";
        }
        if (hold) return;

        // ESC = 중단 (포탑은 그대로 - 아무 일도 일어나지 않은 것으로)
        if (phase < 2 && Input.GetKeyDown(KeyCode.Escape))
        {
            UIManager.Instance?.ShowStatChange("진화 조리 중단 - 두 요리는 그대로 남았다");
            Destroy(gameObject);
            return;
        }

        // v1.1: 라운드 앞 읽을 시간 - 커서를 세워 두고 안내를 읽는다. [Space] 나 클릭이면 바로 시작 (판정은 아니다)
        if (phase < 2 && readTimer > 0f)
        {
            readTimer -= Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) readTimer = 0f;
            if (judgeTimer > 0f) { judgeTimer -= Time.deltaTime; if (judgeText != null) { Color jc = judgeText.color; jc.a = Mathf.Clamp01(judgeTimer / 0.35f); judgeText.color = jc; } }
            return;
        }

        // 판정 팝업 페이드
        if (judgeTimer > 0f)
        {
            judgeTimer -= Time.deltaTime;
            if (judgeText != null)
            {
                Color c = judgeText.color;
                c.a = Mathf.Clamp01(judgeTimer / 0.35f);
                judgeText.color = c;
            }
        }

        if (phase == 0) UpdateGrillRound();
        else if (phase == 1) UpdateBoilRound();
        else UpdateFinish();
    }

    // ─────────────────────────────────────────────
    // 1라운드: 정수 추출 (굽기 문법)
    // ─────────────────────────────────────────────
    private void UpdateGrillRound()
    {
        bar += dir * GameBalance.InfuseGrillSpeed * Time.deltaTime;
        if (bar >= 100f) { bar = 100f; dir = -1f; }
        if (bar <= 0f) { bar = 0f; dir = 1f; }

        cursorRect.anchoredPosition = new Vector2(-TRACK_W / 2f + TRACK_W * (bar / 100f), 0f);

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            float d = Mathf.Abs(bar - 50f);
            if (d <= 6f) { score += 2; ShowJudge("PERFECT!", UIFactory.GOLD); SoundManager.Play("sfx_judge_perfect"); }
            else if (d <= 20f) { score += 1; ShowJudge("Good", new Color(0.6f, 0.85f, 0.54f)); SoundManager.Play("sfx_judge_good"); }
            else { ShowJudge("정수가 흩어졌다...", new Color(1f, 0.6f, 0.48f)); SoundManager.Play("sfx_judge_bad"); }

            // 2라운드 준비
            phase = 1;
            gauge = 50f;
            zonePhase = Random.Range(0f, 6.28f);
            holdTimer = GameBalance.InfuseBoilTime;
            inZone = 0f;
            readTimer = Mathf.Max(0f, GameBalance.InfuseReadSec);   // v1.1: 2라운드 앞에도 읽을 시간
            cursorRect.gameObject.SetActive(false);
            fillRect.gameObject.SetActive(true);
            zoneRect.gameObject.SetActive(true);
            if (needleRect != null) needleRect.gameObject.SetActive(true);
            DrawBoil();
            SetPhaseInfo();
        }
    }

    // ─────────────────────────────────────────────
    // 2라운드: 융합 안정화 (끓이기 문법 - 가로 게이지)
    // ─────────────────────────────────────────────
    private void UpdateBoilRound()
    {
        bool hold = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);
        gauge += (hold ? 55f : -38f) * Time.deltaTime;
        gauge = Mathf.Clamp(gauge, 0f, 100f);

        zonePhase += Time.deltaTime * 1.1f;
        zoneCenter = 50f + Mathf.Sin(zonePhase) * 26f;

        holdTimer -= Time.deltaTime;
        if (Mathf.Abs(gauge - zoneCenter) <= 12f)
            inZone += Time.deltaTime;

        DrawBoil();

        if (holdTimer <= 0f)
        {
            float ratio = inZone / GameBalance.InfuseBoilTime;
            if (ratio >= 0.75f) { score += 2; ShowJudge("PERFECT!", UIFactory.GOLD); SoundManager.Play("sfx_judge_perfect"); }
            else if (ratio >= 0.45f) { score += 1; ShowJudge("Good", new Color(0.6f, 0.85f, 0.54f)); SoundManager.Play("sfx_judge_good"); }
            else { ShowJudge("융합이 요동친다...", new Color(1f, 0.6f, 0.48f)); SoundManager.Play("sfx_judge_bad"); }

            phase = 2;
            endTimer = 0.8f;
            SetPhaseInfo();
        }
    }

    /// <summary>v1.1: 2라운드 그리기 - 채움 바 + 바늘(채움 끝. 구간 안이면 금색, 밖이면 흰색) + 움직이는 구간</summary>
    private void DrawBoil()
    {
        fillRect.sizeDelta = new Vector2(TRACK_W * (gauge / 100f), 26f);
        zoneRect.anchoredPosition = new Vector2(-TRACK_W / 2f + TRACK_W * (zoneCenter / 100f), 0f);
        if (needleRect != null)
        {
            needleRect.anchoredPosition = new Vector2(-TRACK_W / 2f + TRACK_W * (gauge / 100f), 0f);
            Image ni = needleRect.GetComponent<Image>();
            if (ni != null) ni.color = Mathf.Abs(gauge - zoneCenter) <= 12f ? UIFactory.GOLD : Color.white;
        }
    }

    // ─────────────────────────────────────────────
    // 종료: 잠깐 결과 보여주고 실제 진화 수행
    // ─────────────────────────────────────────────
    private void UpdateFinish()
    {
        endTimer -= Time.deltaTime;
        if (endTimer > 0f) return;

        int bonus = score >= GameBalance.InfuseBonusScoreNeed ? GameBalance.InfuseBonusLevel : 0;

        if (stockMode)
            CookingBridge.CompleteStockFusion(stockA, stockB, fusion, score >= GameBalance.InfuseBonusScoreNeed ? GameBalance.InfuseStockBonusPlates : 0);   // v1.1: 접시 합성
        else if (manager != null)
            manager.CompleteFusion(idxA, idxB, fusion, bonus, score >= 4);

        Destroy(gameObject);
    }

    // ─────────────────────────────────────────────
    // UI
    // ─────────────────────────────────────────────
    /// <summary>v1.1: 안내 한 줄씩 크게 (무엇을 언제 누르나) + 둘째 줄에 점수와 보너스 규칙</summary>
    private void SetPhaseInfo()
    {
        string bonusWord = stockMode ? "접시 +" + GameBalance.InfuseStockBonusPlates : "Lv+" + GameBalance.InfuseBonusLevel;
        if (phase == 0)
            infoText.text = "1/2 정수 추출 - 꼬치가 가운데 올 때 [Space]";
        else if (phase == 1)
            infoText.text = "2/2 융합 안정화 - [Space] 꾹 = 바늘 오른쪽, 떼면 왼쪽. 흰 구간 안에 두어라";
        else
            infoText.text = "합계 " + score + "점" + (score >= GameBalance.InfuseBonusScoreNeed ? " - 잘 익었다! " + bonusWord : " - 진화는 된다 (보너스 없음)");
        if (subText != null)
            subText.text = phase < 2 ? "지금 " + score + "점 / " + GameBalance.InfuseBonusScoreNeed + "점 이상이면 " + bonusWord + "  ·  [ESC] 그만두기 (잃는 것 없음)" : "";
    }

    private void ShowJudge(string msg, Color col)
    {
        judgeText.text = msg;
        judgeText.color = col;
        judgeTimer = 0.9f;
    }

    private void BuildUI()
    {
        canvasGo = new GameObject("InfusingCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31;   // 조리 미니게임(30) 바로 위 (동시 활성은 코드로 차단됨)
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIFactory.RefResolution;   // v9.14: UI 전체 배율 (GameBalance.UIScale)

        // 좌하단 패널 (조리 미니게임과 같은 자리 - 주방 작업 공간). v1.1: 440x210 -> 560x260 (안내 글자 15 -> 20)
        RectTransform panel = KitchenEventManager.MakeBox(canvasGo.transform, "Panel",
            new Color(0.10f, 0.06f, 0.09f, 0.95f));
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(0f, 0f);
        panel.pivot = new Vector2(0f, 0f);
        panel.anchoredPosition = new Vector2(12f, 192f);
        panel.sizeDelta = new Vector2(560f, 260f);
        panelGroup = panel.gameObject.AddComponent<CanvasGroup>();

        // T2 핑크 테두리 (전설 융합의 색)
        RectTransform border = KitchenEventManager.MakeBox(panel, "Border", UIFactory.T2PINK);
        border.anchorMin = Vector2.zero; border.anchorMax = new Vector2(1f, 0f);
        border.pivot = new Vector2(0.5f, 0f);
        border.anchoredPosition = Vector2.zero;
        border.sizeDelta = new Vector2(0f, 3f);

        titleText = KitchenEventManager.MakeText(panel, "Title", "", 22, UIFactory.T2PINK);
        RectTransform tRt = titleText.rectTransform;
        tRt.anchorMin = new Vector2(0f, 1f); tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -10f);
        tRt.sizeDelta = new Vector2(-16f, 30f);

        // v1.1: 안내 = 큰 한 줄 (무엇을 언제 누르나) + 작은 둘째 줄 (점수·보너스·그만두기)
        infoText = KitchenEventManager.MakeText(panel, "Info", "", 20,
            new Color(0.95f, 0.92f, 0.85f));
        RectTransform iRt = infoText.rectTransform;
        iRt.anchorMin = new Vector2(0f, 0f); iRt.anchorMax = new Vector2(1f, 0f);
        iRt.pivot = new Vector2(0.5f, 0f);
        iRt.anchoredPosition = new Vector2(0f, 36f);
        iRt.sizeDelta = new Vector2(-16f, 54f);

        subText = KitchenEventManager.MakeText(panel, "Sub", "", 14, new Color(0.72f, 0.66f, 0.58f));
        RectTransform sRt = subText.rectTransform;
        sRt.anchorMin = new Vector2(0f, 0f); sRt.anchorMax = new Vector2(1f, 0f);
        sRt.pivot = new Vector2(0.5f, 0f);
        sRt.anchoredPosition = new Vector2(0f, 10f);
        sRt.sizeDelta = new Vector2(-16f, 24f);

        judgeText = KitchenEventManager.MakeText(panel, "Judge", "", 28, UIFactory.GOLD);
        RectTransform jRt = judgeText.rectTransform;
        jRt.anchorMin = new Vector2(0.5f, 0.5f); jRt.anchorMax = new Vector2(0.5f, 0.5f);
        jRt.pivot = new Vector2(0.5f, 0.5f);
        jRt.anchoredPosition = new Vector2(0f, 62f);
        jRt.sizeDelta = new Vector2(520f, 36f);

        // 판정 트랙 (v1.1: 폭 380 -> 480)
        track = KitchenEventManager.MakeBox(panel, "Track", new Color(0f, 0f, 0f, 0.6f));
        track.anchorMin = new Vector2(0.5f, 0.5f); track.anchorMax = new Vector2(0.5f, 0.5f);
        track.pivot = new Vector2(0.5f, 0.5f);
        track.anchoredPosition = new Vector2(0f, 12f);
        track.sizeDelta = new Vector2(TRACK_W, 34f);

        // 1라운드용: 중앙 판정 존 (고정) - Good 폭
        RectTransform goodZone = KitchenEventManager.MakeBox(track, "Good", new Color(0.35f, 0.6f, 0.3f, 0.8f));
        goodZone.anchorMin = new Vector2(0.5f, 0f); goodZone.anchorMax = new Vector2(0.5f, 1f);
        goodZone.pivot = new Vector2(0.5f, 0.5f);
        goodZone.anchoredPosition = Vector2.zero;
        goodZone.sizeDelta = new Vector2(TRACK_W * 0.4f, 0f);

        RectTransform perfectZone = KitchenEventManager.MakeBox(track, "Perfect", new Color(1f, 0.42f, 0.85f, 0.9f));
        perfectZone.anchorMin = new Vector2(0.5f, 0f); perfectZone.anchorMax = new Vector2(0.5f, 1f);
        perfectZone.pivot = new Vector2(0.5f, 0.5f);
        perfectZone.anchoredPosition = Vector2.zero;
        perfectZone.sizeDelta = new Vector2(TRACK_W * 0.12f, 0f);

        cursorRect = KitchenEventManager.MakeBox(track, "Cursor", Color.white);
        cursorRect.anchorMin = new Vector2(0.5f, 0f); cursorRect.anchorMax = new Vector2(0.5f, 1f);
        cursorRect.pivot = new Vector2(0.5f, 0.5f);
        cursorRect.sizeDelta = new Vector2(6f, 8f);

        // 2라운드용: 게이지 채움(왼쪽부터) + 움직이는 존 (시작 시 숨김)
        fillRect = KitchenEventManager.MakeBox(track, "Fill", new Color(1f, 0.42f, 0.85f, 0.45f));
        fillRect.anchorMin = new Vector2(0f, 0.5f); fillRect.anchorMax = new Vector2(0f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(0f, 26f);
        fillRect.gameObject.SetActive(false);

        zoneRect = KitchenEventManager.MakeBox(track, "Zone", new Color(1f, 1f, 1f, 0.85f));
        zoneRect.anchorMin = new Vector2(0.5f, 0f); zoneRect.anchorMax = new Vector2(0.5f, 1f);
        zoneRect.pivot = new Vector2(0.5f, 0.5f);
        zoneRect.sizeDelta = new Vector2(TRACK_W * 0.24f, 4f);
        zoneRect.gameObject.SetActive(false);

        // v1.1: 바늘 - 채움 바의 끝에 서는 굵은 막대 (이게 구간 안에 있어야 한다는 것이 보이게). 구간 안이면 금색
        needleRect = KitchenEventManager.MakeBox(track, "Needle", Color.white);
        needleRect.anchorMin = new Vector2(0.5f, 0f); needleRect.anchorMax = new Vector2(0.5f, 1f);
        needleRect.pivot = new Vector2(0.5f, 0.5f);
        needleRect.sizeDelta = new Vector2(8f, 14f);   // 트랙보다 위아래로 7 씩 튀어나온다
        needleRect.gameObject.SetActive(false);
    }
}
