using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// [UIManager.cs] v3.4 (v9.18 2026-10-06: 견습 목표 카드가 떠 있으면 알림 줄을 카드 아래로(카드 뒤에 가려지던 것) / 씬 HUD 손질(FixSceneHud) - 씬 캔버스 [HUD Canvas] 도 UI 배율(GameBalance.UIScale)을 따른다(HP·골드·라운드·예고 글자가 코드로 만든 창보다 작던 것) + 씬에 "New Text" 로 남아 있는 글자를 비우고 포만감 잔재를 끈다 / 골드 글자가 다른 값으로 바뀌어 있으면 바로 다시 쓴다) / v3.3 (v9.17 2026-10-06 화면 손맛 2차 - B7: 골드가 0.4초 동안 세어 올라가고(줄 땐 0.2초) 한 번에 50 이상 벌면 금색 반짝 + 튐 / 알림 통합: 같은 문구가 2초 안에 또 오면 새 줄 대신 "x2" / 위험 알림은 2.5초 동안 일반 알림에 안 밀린다 / 가운데 예고는 같은 문구 2초 무시 + 앞 문구가 1초는 떠 있게) / v3.2 (v9.15 2026-09-29 HUD 재배치 GameBalance.HudRegroup: 우상단 정보 2줄(UISkin.InfoLine1/2 - 손님 남음·보스까지 / 지역·예고) 0.25초마다, 알림 로그 스택을 우상단 판 아래(앵커 (1,1))로) / v3.1 (v9.11 2026-09-22 타격감: 기차 HP 바 지연 잔량(빨간 띠가 0.5초 뒤 따라 내려온다, 회복은 즉시) / 웨이브 예고·클리어 문구 위에서 내려오며 팝, 새 문구가 오면 이전 문구 즉시 교체) / v3
/// 게임 HUD 전체를 담당하는 UI 관리 스크립트입니다.
/// - v3 변경점 (P1: 알림 채널 2분리 - 기술감사 처방):
///   1) ShowStatChange가 "우측 로그 스택"으로 개조 - 여러 알림이 겹쳐도 씹히지 않고
///      최근 5줄이 쌓였다가 차례로 사라진다 (호출부 30여 곳은 수정 불필요)
///   2) ShowDanger 신설 - 위험 알림(빙결/기름/독침 등)은 주황 굵은 줄로 구분
///   3) 대형 경고(보스 예고 등)는 기존 WarningFX(중앙+가장자리 맥동)가 담당 - 채널 2개 체제
///   4) 씬의 StatChangeText 오브젝트는 더 이상 사용하지 않음 (자동 비활성, 삭제해도 무방)
/// - v2 변경점 (구시스템 정리):
///   1) 포만감 게이지 / 허기 경고 연출 전부 제거 (허기 시스템 삭제)
///   2) '다음 웨이브' 버튼 UI 제거 (웨이브는 증강 선택 후 자동 진행)
///   3) HP 바 / 골드·웨이브 텍스트 / 상태 패널 / 웨이브 예고 표시는 유지
/// VS 2017 (C# 7.3) 호환 버전입니다.
/// </summary>
public class UIManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // 싱글톤
    // ─────────────────────────────────────────────
    public static UIManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // HP 바
    // ─────────────────────────────────────────────
    [Header("─ HP 바 ─")]
    public Slider hpSlider;
    private Image hpTrailImage;            // v3.1: 지연 잔량 (코드 생성, 채움 뒤)
    private float hpTrailRatio = 1f;       // v3.1: 지연 잔량 비율
    private float hpTrailHoldUntil = 0f;   // v3.1: 이 시각까지 멈췄다가 내려온다
    private float hpLastRatio = 1f;
    private Coroutine waveNoticeRoutine;   // v3.1: 진행 중인 예고 (새 문구가 오면 끊는다)
    private Vector2 waveNoticeBasePos, waveWarningBasePos;
    private bool waveNoticeBaseSaved = false;
    public Image hpFillImage;
    public TextMeshProUGUI hpText;

    // ─────────────────────────────────────────────
    // 상단 정보
    // ─────────────────────────────────────────────
    [Header("─ 상단 정보 텍스트 ─")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI stateText;

    // ─────────────────────────────────────────────
    // 게임 상태별 패널
    // ─────────────────────────────────────────────
    [Header("─ 게임 상태별 패널 ─")]
    public GameObject lobbyPanel;
    public GameObject battlePanel;
    public GameObject townPanel;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    [Header("─ HP 색상 ─")]
    public Color colorHPHigh = new Color(0.2f, 0.8f, 0.2f);
    public Color colorHPMid = new Color(1.0f, 0.8f, 0.0f);
    public Color colorHPLow = new Color(1.0f, 0.2f, 0.2f);

    [Header("─ 알림 텍스트 ─")]
    public TextMeshProUGUI statChangeText;    // 스탯 변화 / 재료 획득 알림
    public TextMeshProUGUI waveNoticeText;    // 웨이브 속성 예고 텍스트
    public TextMeshProUGUI waveWarningText;   // 드롭/대응 안내 텍스트

    // ─────────────────────────────────────────────
    // 내부 참조
    // ─────────────────────────────────────────────
    private TrainManager trainManager;
    private GameManager gameManager;

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        trainManager = FindFirstObjectByType<TrainManager>();
        gameManager = GameManager.Instance;

        if (gameManager != null)
            gameManager.OnGameStateChanged.AddListener(OnGameStateChanged);

        SetupSliders();
        FixSceneHud();   // v3.4
        ShowOnlyPanel(lobbyPanel);

        if (statChangeText != null) statChangeText.gameObject.SetActive(false);

        Debug.Log("[UIManager] HUD 초기화 완료 (v2 - 포만감/다음웨이브 버튼 제거)");
    }

    // ─────────────────────────────────────────────
    // v3.4: 씬 HUD 손질 (씬 파일은 그대로 두고 시작할 때 고친다)
    // ─────────────────────────────────────────────
    private void FixSceneHud()
    {
        // 씬 HUD 캔버스 = battlePanel 의 맨 위 부모 (없으면 이름으로)
        Transform root = null;
        if (battlePanel != null) { root = battlePanel.transform; while (root.parent != null) root = root.parent; }
        else { GameObject hud = GameObject.Find("[HUD Canvas]"); if (hud != null) root = hud.transform; }
        if (root == null) return;

        // 1) UI 배율: 코드로 만드는 캔버스는 v9.14 부터 1920x1080 / UIScale 을 기준으로 잡는데 씬 캔버스만 1920x1080 그대로였다.
        //    그래서 HP·골드·라운드·가운데 예고만 다른 창보다 작았다. 같은 기준으로 맞춘다 (판·글자가 같은 비율로 커진다)
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            scaler.referenceResolution = UIFactory.RefResolution;

        // 2) 씬에 기본 글자 "New Text" 로 남아 있는 TMP 를 비운다 (꺼진 것 포함). 코드가 값을 써 넣는 글자는 곧 덮어쓰이고,
        //    아무도 안 쓰는 잔재(포만감 글자 등)는 화면에 보여도 빈 글자가 된다
        TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        int cleared = 0;
        for (int i = 0; i < texts.Length; i++)
            if (texts[i] != null && texts[i].text == "New Text") { texts[i].text = ""; cleared++; }

        // 3) 포만감 시스템 잔재 (v2 에서 없앤 기능의 바·글자) - UISkin 이 없을 때도 꺼지게 여기서도 끈다
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && (all[i].name == "SatietyBar" || all[i].name == "SatietyText")) all[i].gameObject.SetActive(false);

        if (cleared > 0) Debug.Log("[UIManager] 씬 HUD 손질: 기본 글자 \"New Text\" " + cleared + "개 비움");
    }

    // ─────────────────────────────────────────────
    // 슬라이더 초기 설정
    // ─────────────────────────────────────────────
    private void SetupSliders()
    {
        if (hpSlider != null && trainManager != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = trainManager.currentMaxHP;
            hpSlider.value = trainManager.currentHP;
        }
    }

    // ─────────────────────────────────────────────
    // 매 프레임 갱신
    // ─────────────────────────────────────────────
    private void Update()
    {
        RefreshHPBar();
        RefreshInfoTexts();
        UpdateLogStack();   // P1: 우측 알림 로그 수명 관리
        TickPendingNotice();   // v3.3
    }

    // ─────────────────────────────────────────────
    // HP 바 갱신
    // ─────────────────────────────────────────────
    private void RefreshHPBar()
    {
        if (trainManager == null || hpSlider == null) return;

        hpSlider.maxValue = trainManager.currentMaxHP;
        hpSlider.value = trainManager.currentHP;

        if (hpFillImage != null)
        {
            float hpRatio = trainManager.currentHP / trainManager.currentMaxHP;
            if (hpRatio >= 0.7f) hpFillImage.color = colorHPHigh;
            else if (hpRatio >= 0.3f) hpFillImage.color = colorHPMid;
            else hpFillImage.color = colorHPLow;
        }

        if (hpText != null)
            hpText.text = (int)trainManager.currentHP + " / " + (int)trainManager.currentMaxHP;

        RefreshHPTrail();
    }

    /// <summary>
    /// v3.1: 지연 잔량 - 맞으면 빨간 띠가 잠깐(TrailingHpDelay) 남아 있다가 TrailingHpSpeed(초당 비율)로 따라 내려온다.
    /// 회복은 즉시 따라간다. 채움(hpFillImage)이 Filled 타입이면 fillAmount, 아니면 앵커로 같은 방식으로 그린다.
    /// </summary>
    private void RefreshHPTrail()
    {
        if (!GameBalance.TrailingHpBarOn || hpFillImage == null) { if (hpTrailImage != null) hpTrailImage.enabled = false; return; }
        float ratio = Mathf.Clamp01(trainManager.currentHP / Mathf.Max(1f, trainManager.currentMaxHP));

        if (hpTrailImage == null)
        {
            GameObject go = new GameObject("HpTrail");
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(hpFillImage.transform.parent, false);
            rt.SetSiblingIndex(hpFillImage.transform.GetSiblingIndex());   // 채움 뒤에 그려진다
            RectTransform fillRt = hpFillImage.rectTransform;
            rt.anchorMin = fillRt.anchorMin; rt.anchorMax = fillRt.anchorMax;
            rt.offsetMin = fillRt.offsetMin; rt.offsetMax = fillRt.offsetMax;
            rt.pivot = fillRt.pivot;
            hpTrailImage = go.AddComponent<Image>();
            hpTrailImage.sprite = hpFillImage.sprite;
            hpTrailImage.type = hpFillImage.type;
            hpTrailImage.fillMethod = hpFillImage.fillMethod;
            hpTrailImage.fillOrigin = hpFillImage.fillOrigin;
            hpTrailImage.color = new Color(0.85f, 0.18f, 0.12f, 0.95f);
            hpTrailImage.raycastTarget = false;
            hpTrailRatio = ratio; hpLastRatio = ratio;
        }
        hpTrailImage.enabled = true;

        if (ratio < hpLastRatio - 0.0005f) hpTrailHoldUntil = Time.unscaledTime + GameBalance.TrailingHpDelay;   // 맞았다 - 잠깐 멈춤
        hpLastRatio = ratio;

        if (ratio >= hpTrailRatio) hpTrailRatio = ratio;   // 회복·초기화는 즉시
        else if (Time.unscaledTime >= hpTrailHoldUntil)
            hpTrailRatio = Mathf.MoveTowards(hpTrailRatio, ratio, GameBalance.TrailingHpSpeed * Time.unscaledDeltaTime);

        RectTransform trt = hpTrailImage.rectTransform;
        if (hpFillImage.type == Image.Type.Filled)
        {
            hpTrailImage.fillAmount = hpTrailRatio;
        }
        else
        {
            // Slider 가 채움 앵커를 0..value 로 놓는 것과 같은 방식
            RectTransform fillRt = hpFillImage.rectTransform;
            Vector2 aMin = fillRt.anchorMin, aMax = fillRt.anchorMax;
            if (hpSlider != null && hpSlider.direction == Slider.Direction.LeftToRight) { aMin.x = 0f; aMax.x = hpTrailRatio; }
            else if (hpSlider != null && hpSlider.direction == Slider.Direction.RightToLeft) { aMin.x = 1f - hpTrailRatio; aMax.x = 1f; }
            trt.anchorMin = aMin; trt.anchorMax = aMax;
            trt.offsetMin = fillRt.offsetMin; trt.offsetMax = fillRt.offsetMax;
        }
    }

    // ─────────────────────────────────────────────
    // 골드 · 웨이브 텍스트 갱신
    // ─────────────────────────────────────────────
    private void RefreshInfoTexts()
    {
        if (gameManager == null) return;

        if (goldText != null) RefreshGold(gameManager.playerGold);

        if (waveText != null)
            waveText.text = "Wave  " + gameManager.currentWave;

        RefreshInfoLines();   // v3.2
    }

    // ── v3.3 (B7): 골드 세어 올리기 ──
    private int goldShown = -1, goldFrom = 0, goldTarget = 0, goldTextShown = -1;
    private string goldTextStr = null;     // v3.4: 마지막으로 써 넣은 골드 글자
    private float goldT = 0f, goldDur = 0.4f;
    private float goldFlashT = -1f;        // 0 이상 = 금색 반짝 진행 (실시간 초)
    private Color goldBaseColor = Color.white;
    private const float GOLD_FLASH_SEC = 0.35f;

    /// <summary>
    /// 골드 표시: 값이 바뀌면 지금 보이는 숫자에서 새 값으로 세어 간다 (늘면 GoldCountSec, 줄면 그 절반 - 실시간).
    /// 한 번에 GoldFlashMin 이상 늘면 글자가 금빛으로 밝아졌다 돌아오고 한 번 튄다. 글자는 숫자가 바뀔 때만 다시 만든다
    /// </summary>
    private void RefreshGold(int gold)
    {
        bool feel = GameBalance.HudCountFeelOn && GameBalance.GameFeelMaster > 0f;
        if (!feel || goldShown < 0)
        {
            goldShown = gold; goldFrom = gold; goldTarget = gold;
        }
        else if (gold != goldTarget)
        {
            int delta = gold - goldTarget;
            goldFrom = goldShown; goldTarget = gold; goldT = 0f;
            goldDur = Mathf.Max(0.05f, delta > 0 ? GameBalance.GoldCountSec : GameBalance.GoldCountSec * 0.5f);
            if (delta >= GameBalance.GoldFlashMin)
            {
                if (goldFlashT < 0f) goldBaseColor = goldText.color;
                goldFlashT = 0f;
                UIFeel.Bounce(goldText.rectTransform, 0.18f, 0.25f);
            }
        }

        if (goldShown != goldTarget)
        {
            goldT += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(goldT / goldDur);
            float e = 1f - (1f - k) * (1f - k);
            goldShown = k >= 1f ? goldTarget : Mathf.RoundToInt(Mathf.Lerp(goldFrom, goldTarget, e));
        }
        // v3.4: 숫자가 바뀌었거나, 글자가 다른 값으로 바뀌어 있으면(씬 기본 글자 등) 다시 쓴다
        if (goldShown != goldTextShown || goldTextStr == null || goldText.text != goldTextStr)
        {
            goldTextShown = goldShown;
            goldTextStr = "G  " + goldShown;
            goldText.text = goldTextStr;
        }

        if (goldFlashT >= 0f)
        {
            goldFlashT += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(goldFlashT / GOLD_FLASH_SEC);
            goldText.color = Color.Lerp(new Color(1f, 0.97f, 0.6f, goldBaseColor.a), goldBaseColor, k);
            if (k >= 1f) { goldFlashT = -1f; goldText.color = goldBaseColor; }
        }
    }

    // ── v3.2: 우상단 정보 2줄 (HUD 재배치) ──
    private float infoRefreshAt = 0f;
    private static readonly string[] REGION_NAMES = { "", "구리 사막", "테슬라 협곡", "코발트 광산", "황야의 끝" };

    /// <summary>손님 남음·보스까지 / 지역·예고. 0.25초마다 (손님 수는 FindObjectsByType). 견습·로비에선 비운다</summary>
    private void RefreshInfoLines()
    {
        if (UISkin.InfoLine1 == null || UISkin.InfoLine2 == null) return;
        if (Time.unscaledTime < infoRefreshAt) return;
        infoRefreshAt = Time.unscaledTime + 0.25f;

        bool battle = gameManager.currentState == GameManager.GameState.Battle || gameManager.currentState == GameManager.GameState.Town;
        if (!battle || TutorialDirector.Active)
        {
            UISkin.InfoLine1.text = TutorialDirector.Active ? "" : "";
            UISkin.InfoLine2.text = "";
            return;
        }

        int wave = Mathf.Max(1, gameManager.currentWave);
        int alive = 0; bool boss = false;
        Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].IsAlive) continue;
            alive++;
            if (all[i] is BossEnemy) boss = true;
        }
        int nextBoss = wave;
        while (!GameBalance.IsBossWave(nextBoss) && nextBoss < GameBalance.FinalWave) nextBoss++;
        string toBoss = GameBalance.IsBossWave(wave) ? (boss ? "보스와 싸우는 중" : "보스 라운드") : "보스까지 " + (nextBoss - wave) + " 라운드";
        UISkin.InfoLine1.text = (gameManager.currentState == GameManager.GameState.Town ? "정차 중" : "손님 남음 " + alive) + "  /  " + toBoss;

        int region = Mathf.Clamp(GameBalance.RegionOf(wave), 1, 4);
        string hazard = region == GameBalance.AmbientLightningRegion && !GameBalance.IsBossWave(wave) ? " - 낙뢰가 친다" : "";
        UISkin.InfoLine2.text = "지역 " + region + " " + REGION_NAMES[region] + hazard;
    }

    // ─────────────────────────────────────────────
    // P1: 알림 로그 스택 (우측) - 채널 1 (일반/위험 라인)
    // 채널 2(대형 경고)는 WarningFX.Flash가 담당.
    // ─────────────────────────────────────────────

    private const int LOG_LINES = 5;       // 동시 표시 줄 수
    private const float LOG_TOP = 172f;    // 첫 줄 위 끝 (화면 위에서) = 우상단 판(8 ~ 158) 아래
    private float logTopShown = LOG_TOP;   // v3.4: 지금 놓인 자리 (견습 목표 카드가 뜨면 카드 아래로 내린다)
    private const float LOG_LIFE = 3.5f;   // 줄 수명(초)
    private const float LOG_FADE = 0.6f;   // 수명 끝 페이드 구간

    private Text[] logTexts;               // 코드 생성 로그 줄 (0 = 최신, 맨 위)
    private string[] logMsgs = new string[LOG_LINES];
    private Color[] logColors = new Color[LOG_LINES];
    private float[] logAges = new float[LOG_LINES];   // 경과 시간 (수명 지나면 숨김)
    private bool[] logUsed = new bool[LOG_LINES];
    private bool[] logBold = new bool[LOG_LINES];          // v3.3: 위험 줄 (굵게 + 잠깐 고정)
    private string[] logBase = new string[LOG_LINES];      // v3.3: "x2" 를 붙이기 전 원래 문구 (중복 비교용)
    private int[] logCount = new int[LOG_LINES];           // v3.3: 같은 문구가 온 횟수

    private static readonly Color LOG_NORMAL = new Color(1f, 0.92f, 0.55f);   // 일반: 크림 노랑
    private static readonly Color LOG_DANGER = new Color(1f, 0.5f, 0.25f);    // 위험: 주황

    /// <summary>
    /// 일반 알림 (보상/획득/진행 등). 여러 개가 연달아 와도 스택에 쌓여 씹히지 않는다.
    /// 사용법: UIManager.Instance?.ShowStatChange("재료 +1");
    /// </summary>
    public void ShowStatChange(string message)
    {
        PushLog(message, LOG_NORMAL, false);
    }

    /// <summary>
    /// 위험 알림 (빙결/기름/독침 등 지금 플레이에 영향 주는 것) - 주황 굵은 줄.
    /// 보스급 대형 경고는 이걸 쓰지 말고 WarningFX.Flash를 쓸 것.
    /// </summary>
    public void ShowDanger(string message)
    {
        PushLog(message, LOG_DANGER, true);
    }

    private void PushLog(string message, Color col, bool bold)
    {
        if (logTexts == null) BuildLogStack();

        // v3.3: 같은 문구가 NoticeDedupeSec 안에 또 왔다 - 새 줄을 쌓지 않고 그 줄에 "x2" 를 올리고 수명을 되돌린다
        //       (포탑 여럿이 한꺼번에 식거나 재료가 연달아 들어올 때 다섯 줄이 같은 말로 차던 것)
        if (GameBalance.NoticeDedupeSec > 0f)
        {
            for (int i = 0; i < LOG_LINES; i++)
            {
                if (!logUsed[i] || logBase[i] != message || logBold[i] != bold || logAges[i] > GameBalance.NoticeDedupeSec) continue;
                logCount[i]++;
                logMsgs[i] = message + "  x" + logCount[i];
                logAges[i] = 0f;
                RenderLog();
                return;
            }
        }

        // v3.3: 위험 줄 고정 - 새 일반 알림은 아직 생생한(DangerPinSec 안) 위험 줄 아래에 끼운다. 위험 알림은 늘 맨 위
        int at = 0;
        if (!bold && GameBalance.DangerPinSec > 0f)
            while (at < LOG_LINES && logUsed[at] && logBold[at] && logAges[at] < GameBalance.DangerPinSec) at++;
        if (at >= LOG_LINES) return;   // 다섯 줄이 전부 방금 뜬 위험 알림이면 일반 알림은 버린다

        // at 부터 한 칸씩 아래로 밀기 (맨 아래는 버림)
        for (int i = LOG_LINES - 1; i > at; i--)
        {
            logMsgs[i] = logMsgs[i - 1];
            logColors[i] = logColors[i - 1];
            logAges[i] = logAges[i - 1];
            logUsed[i] = logUsed[i - 1];
            logBold[i] = logBold[i - 1];
            logBase[i] = logBase[i - 1];
            logCount[i] = logCount[i - 1];
        }

        logMsgs[at] = message;
        logColors[at] = col;
        logAges[at] = 0f;
        logUsed[at] = true;
        logBold[at] = bold;
        logBase[at] = message;
        logCount[at] = 1;

        RenderLog();
    }

    /// <summary>매 프레임: 로그 수명/페이드 갱신 (일시정지 중에도 흐르게 unscaled)</summary>
    private void UpdateLogStack()
    {
        if (logTexts == null) return;

        // v3.4: 견습 목표 카드(우상단 판 아래, 불투명)가 떠 있으면 알림 줄을 카드 아래로 옮긴다 - 같은 자리라 알림이 카드 뒤에 가려졌다
        if (GameBalance.HudRegroup)
        {
            float cardBottom = TutorialDirector.CardBottomY;
            float top = cardBottom > 0f ? Mathf.Max(LOG_TOP, cardBottom + 10f) : LOG_TOP;
            if (!Mathf.Approximately(top, logTopShown))
            {
                logTopShown = top;
                for (int i = 0; i < LOG_LINES; i++)
                    logTexts[i].rectTransform.anchoredPosition = new Vector2(-16f, -top - i * 28f);
            }
        }

        bool any = false, expired = false;
        for (int i = 0; i < LOG_LINES; i++)
        {
            if (!logUsed[i]) continue;
            logAges[i] += Time.unscaledDeltaTime;
            if (logAges[i] >= LOG_LIFE) { logUsed[i] = false; expired = true; }
            else any = true;
        }
        // v3.3: 위험 줄 고정·중복 합치기 때문에 줄이 나이순이 아닐 수 있다 - 위 줄이 먼저 끝나면 빈 줄이 남지 않게 당기고, 끝난 줄은 그 프레임에 숨긴다
        if (expired) CompactLog();
        if (any || expired || logTexts[0].gameObject.activeSelf) RenderLog();
    }

    /// <summary>v3.3: 쓰는 줄을 순서 그대로 위로 당긴다 (가운데 빈 줄 없애기)</summary>
    private void CompactLog()
    {
        int w = 0;
        for (int r = 0; r < LOG_LINES; r++)
        {
            if (!logUsed[r]) continue;
            if (w != r)
            {
                logMsgs[w] = logMsgs[r];
                logColors[w] = logColors[r];
                logAges[w] = logAges[r];
                logBold[w] = logBold[r];
                logBase[w] = logBase[r];
                logCount[w] = logCount[r];
                logUsed[w] = true;
                logUsed[r] = false;
            }
            w++;
        }
    }

    private void RenderLog()
    {
        for (int i = 0; i < LOG_LINES; i++)
        {
            if (!logUsed[i])
            {
                if (logTexts[i].gameObject.activeSelf) logTexts[i].gameObject.SetActive(false);
                continue;
            }

            float alpha = 1f;
            float remain = LOG_LIFE - logAges[i];
            if (remain < LOG_FADE) alpha = Mathf.Clamp01(remain / LOG_FADE);
            // 아래 줄(오래된 것)일수록 살짝 흐리게 - 시선은 최신 줄로
            alpha *= Mathf.Lerp(1f, 0.55f, i / (float)(LOG_LINES - 1));

            logTexts[i].text = logMsgs[i];
            logTexts[i].fontStyle = logBold[i] ? FontStyle.Bold : FontStyle.Normal;
            Color c = logColors[i];
            c.a = alpha;
            logTexts[i].color = c;
            if (!logTexts[i].gameObject.activeSelf) logTexts[i].gameObject.SetActive(true);
        }
    }

    /// <summary>로그 스택 UI 생성 (최초 1회, 코드 생성 - 씬 작업 불필요)</summary>
    private void BuildLogStack()
    {
        GameObject canvasGo = new GameObject("LogStackCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 455;   // 공명 HUD(470) 바로 아래
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIFactory.RefResolution;   // v9.14: UI 전체 배율 (GameBalance.UIScale)

        logTexts = new Text[LOG_LINES];
        for (int i = 0; i < LOG_LINES; i++)
        {
            Text t = KitchenEventManager.MakeText(canvasGo.transform, "Log" + i, "", 19, LOG_NORMAL);
            RectTransform rt = t.rectTransform;
            if (GameBalance.HudRegroup)
            {
                // v3.2: 우상단 정보 판(150) 바로 아래 - "정보는 오른쪽 위에" (테스터 09-29)
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-16f, -LOG_TOP - i * 28f);
            }
            else
            {
                rt.anchorMin = new Vector2(1f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-14f, 200f - i * 28f);   // 우측, 위에서 아래로
            }
            rt.sizeDelta = new Vector2(560f, 26f);
            t.alignment = TextAnchor.MiddleRight;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;

            // 가독성용 얇은 그림자
            UnityEngine.UI.Shadow sh = t.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            sh.effectDistance = new Vector2(1f, -1f);
            sh.effectColor = new Color(0f, 0f, 0f, 0.8f);

            t.gameObject.SetActive(false);
            logTexts[i] = t;
        }
    }

    // ─────────────────────────────────────────────
    // 게임 상태 변경 콜백
    // ─────────────────────────────────────────────
    private void OnGameStateChanged(GameManager.GameState newState)
    {
        if (stateText != null)
        {
            if (newState == GameManager.GameState.Lobby)
                stateText.text = "대기 중";
            else if (newState == GameManager.GameState.Battle)
                stateText.text = "전투 중";
            else if (newState == GameManager.GameState.Town)
                stateText.text = "마을 정비";
            else if (newState == GameManager.GameState.GameOver)
                stateText.text = "게임 오버";
            else if (newState == GameManager.GameState.Victory)
                stateText.text = "승리!";
            else
                stateText.text = "";
        }

        // 패널 전환
        if (newState == GameManager.GameState.Lobby)
            ShowOnlyPanel(lobbyPanel);
        else if (newState == GameManager.GameState.Battle)
            ShowOnlyPanel(battlePanel);
        else if (newState == GameManager.GameState.Town)
            ShowOnlyPanel(townPanel);
        else if (newState == GameManager.GameState.GameOver)
            ShowOnlyPanel(gameOverPanel);
        else if (newState == GameManager.GameState.Victory)
            ShowOnlyPanel(victoryPanel);
    }

    // ─────────────────────────────────────────────
    // 패널 전환
    // ─────────────────────────────────────────────
    private void ShowOnlyPanel(GameObject targetPanel)
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (battlePanel != null) battlePanel.SetActive(false);
        if (townPanel != null) townPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);

        if (targetPanel != null) targetPanel.SetActive(true);
    }

    // ─────────────────────────────────────────────
    // 버튼 OnClick 연결용
    // ─────────────────────────────────────────────
    public void OnClickStartGame()
    {
        GameManager.Instance?.ChangeState(GameManager.GameState.Battle);
    }

    public void OnClickStartBattle()
    {
        GameManager.Instance?.ChangeState(GameManager.GameState.Battle);
    }

    public void OnClickGoToTown()
    {
        GameManager.Instance?.ChangeState(GameManager.GameState.Town);
    }

    public void OnClickRestart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public void OnClickNextWave()
    {
        GameManager.Instance?.OnClickNextWave();
    }

    // ─────────────────────────────────────────────
    // 구시스템 호환 스텁 (다음 웨이브 버튼 제거됨)
    // ─────────────────────────────────────────────

    /// <summary>[구시스템 호환] 버튼 UI 제거됨 - 아무 것도 하지 않는다</summary>
    public void ShowNextWaveButton(int nextWave) { }

    /// <summary>[구시스템 호환] 버튼 UI 제거됨 - 아무 것도 하지 않는다</summary>
    public void HideNextWaveButton() { }

    // ─────────────────────────────────────────────
    // 웨이브 예고
    // ─────────────────────────────────────────────

    /// <summary>웨이브 시작 시 속성 예고 표시 (3초 후 사라짐)</summary>
    public void ShowWaveNotice(string notice, string warning)
    {
        float now = Time.unscaledTime;
        bool showing = waveNoticeRoutine != null;

        // v3.3: 같은 문구가 NoticeDedupeSec 안에 또 오면 무시 (등장 연출이 처음부터 다시 돌던 것)
        if (showing && GameBalance.NoticeDedupeSec > 0f && notice == lastNotice && warning == lastWarning
            && now - lastNoticeAt < GameBalance.NoticeDedupeSec)
            return;

        // v3.3: 앞 문구가 뜬 지 NOTICE_MIN_SHOW 가 안 됐으면 그만큼 채운 뒤 바꾼다 (읽기도 전에 덮어쓰던 것).
        //       기다리는 문구는 하나만 - 그사이 더 새 것이 오면 그것으로 바뀐다
        if (showing && GameBalance.NoticeDedupeSec > 0f && now - lastNoticeAt < NOTICE_MIN_SHOW)
        {
            pendingNotice = notice; pendingWarning = warning; hasPendingNotice = true;
            return;
        }
        PlayWaveNotice(notice, warning);
    }

    private const float NOTICE_MIN_SHOW = 1f;
    private string lastNotice = null, lastWarning = null;
    private float lastNoticeAt = -10f;
    private string pendingNotice = null, pendingWarning = null;
    private bool hasPendingNotice = false;

    private void PlayWaveNotice(string notice, string warning)
    {
        // v3.1: 이전 문구가 아직 떠 있으면 끊고 새 문구로 (두 코루틴이 서로 알파를 덮어쓰던 것)
        if (waveNoticeRoutine != null) StopCoroutine(waveNoticeRoutine);
        hasPendingNotice = false;
        lastNotice = notice; lastWarning = warning; lastNoticeAt = Time.unscaledTime;
        waveNoticeRoutine = StartCoroutine(WaveNoticeCoroutine(notice, warning));
    }

    /// <summary>v3.3: 가운데 예고를 바로 지운다 (기다리던 것도). 패배 결과 화면용 - 거기선 시간이 멈춰 있어, 떠 있던 예고가 저절로 사라지지 않는다</summary>
    public void ClearWaveNotice()
    {
        hasPendingNotice = false;
        if (waveNoticeRoutine != null) { StopCoroutine(waveNoticeRoutine); waveNoticeRoutine = null; }
        if (waveNoticeText != null) waveNoticeText.gameObject.SetActive(false);
        if (waveWarningText != null) waveWarningText.gameObject.SetActive(false);
    }

    /// <summary>v3.3: 기다리던 예고를 제때 띄운다 (Update 에서)</summary>
    private void TickPendingNotice()
    {
        if (!hasPendingNotice) return;
        if (waveNoticeRoutine != null && Time.unscaledTime - lastNoticeAt < NOTICE_MIN_SHOW) return;
        PlayWaveNotice(pendingNotice, pendingWarning);
    }

    private IEnumerator WaveNoticeCoroutine(string notice, string warning)
    {
        if (!waveNoticeBaseSaved)
        {
            if (waveNoticeText != null) waveNoticeBasePos = waveNoticeText.rectTransform.anchoredPosition;
            if (waveWarningText != null) waveWarningBasePos = waveWarningText.rectTransform.anchoredPosition;
            waveNoticeBaseSaved = true;
        }

        if (waveNoticeText != null)
        {
            waveNoticeText.gameObject.SetActive(true);
            waveNoticeText.text = notice;
            waveNoticeText.color = new Color(1f, 0.9f, 0.2f, 1f); // 노란색
            waveNoticeText.rectTransform.anchoredPosition = waveNoticeBasePos;
            waveNoticeText.rectTransform.localScale = Vector3.one;
        }

        bool hasWarning = waveWarningText != null && !string.IsNullOrEmpty(warning);
        if (hasWarning)
        {
            waveWarningText.gameObject.SetActive(true);
            waveWarningText.text = warning;
            waveWarningText.color = new Color(0.4f, 1f, 0.4f, 1f); // 초록색
            waveWarningText.rectTransform.anchoredPosition = waveWarningBasePos;
        }
        else if (waveWarningText != null) waveWarningText.gameObject.SetActive(false);

        // v3.1: 위에서 내려오며 1.15 -> 1.0 (0.2초, 실시간 - 카드로 시간이 멈춰도 움직인다)
        if (GameBalance.WaveBannerOn && GameBalance.GameFeelMaster > 0f)
        {
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.2f);
                float e = 1f - (1f - k) * (1f - k);
                if (waveNoticeText != null)
                {
                    waveNoticeText.rectTransform.anchoredPosition = waveNoticeBasePos + Vector2.up * 40f * (1f - e);
                    waveNoticeText.rectTransform.localScale = Vector3.one * (1.15f - 0.15f * e);
                    Color c = waveNoticeText.color; c.a = e; waveNoticeText.color = c;
                }
                if (hasWarning)
                {
                    waveWarningText.rectTransform.anchoredPosition = waveWarningBasePos + Vector2.up * 24f * (1f - e);
                    Color c = waveWarningText.color; c.a = e; waveWarningText.color = c;
                }
                yield return null;
            }
            if (waveNoticeText != null) { waveNoticeText.rectTransform.anchoredPosition = waveNoticeBasePos; waveNoticeText.rectTransform.localScale = Vector3.one; }
            if (hasWarning) waveWarningText.rectTransform.anchoredPosition = waveWarningBasePos;
        }

        // 2초 표시 후 1초 페이드 아웃
        yield return new WaitForSeconds(2f);

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - elapsed;

            if (waveNoticeText != null)
            {
                Color c = waveNoticeText.color; c.a = alpha;
                waveNoticeText.color = c;
            }
            if (waveWarningText != null)
            {
                Color c = waveWarningText.color; c.a = alpha;
                waveWarningText.color = c;
            }
            yield return null;
        }

        if (waveNoticeText != null) waveNoticeText.gameObject.SetActive(false);
        if (waveWarningText != null) waveWarningText.gameObject.SetActive(false);
        waveNoticeRoutine = null;
    }
}
