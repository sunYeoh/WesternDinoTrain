using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// [UIManager.cs] v3.2 (v9.15 2026-09-29 HUD 재배치 GameBalance.HudRegroup: 우상단 정보 2줄(UISkin.InfoLine1/2 - 손님 남음·보스까지 / 지역·예고) 0.25초마다, 알림 로그 스택을 우상단 판 아래(앵커 (1,1))로) / v3.1 (v9.11 2026-09-22 타격감: 기차 HP 바 지연 잔량(빨간 띠가 0.5초 뒤 따라 내려온다, 회복은 즉시) / 웨이브 예고·클리어 문구 위에서 내려오며 팝, 새 문구가 오면 이전 문구 즉시 교체) / v3
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
        ShowOnlyPanel(lobbyPanel);

        if (statChangeText != null) statChangeText.gameObject.SetActive(false);

        Debug.Log("[UIManager] HUD 초기화 완료 (v2 - 포만감/다음웨이브 버튼 제거)");
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

        if (goldText != null)
            goldText.text = "G  " + gameManager.playerGold;

        if (waveText != null)
            waveText.text = "Wave  " + gameManager.currentWave;

        RefreshInfoLines();   // v3.2
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
    private const float LOG_LIFE = 3.5f;   // 줄 수명(초)
    private const float LOG_FADE = 0.6f;   // 수명 끝 페이드 구간

    private Text[] logTexts;               // 코드 생성 로그 줄 (0 = 최신, 맨 위)
    private string[] logMsgs = new string[LOG_LINES];
    private Color[] logColors = new Color[LOG_LINES];
    private float[] logAges = new float[LOG_LINES];   // 경과 시간 (수명 지나면 숨김)
    private bool[] logUsed = new bool[LOG_LINES];

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

        // 한 칸씩 아래로 밀기 (맨 아래는 버림)
        for (int i = LOG_LINES - 1; i >= 1; i--)
        {
            logMsgs[i] = logMsgs[i - 1];
            logColors[i] = logColors[i - 1];
            logAges[i] = logAges[i - 1];
            logUsed[i] = logUsed[i - 1];
            logTexts[i].fontStyle = logTexts[i - 1].fontStyle;
        }

        logMsgs[0] = message;
        logColors[0] = col;
        logAges[0] = 0f;
        logUsed[0] = true;
        logTexts[0].fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;

        RenderLog();
    }

    /// <summary>매 프레임: 로그 수명/페이드 갱신 (일시정지 중에도 흐르게 unscaled)</summary>
    private void UpdateLogStack()
    {
        if (logTexts == null) return;

        bool any = false;
        for (int i = 0; i < LOG_LINES; i++)
        {
            if (!logUsed[i]) continue;
            logAges[i] += Time.unscaledDeltaTime;
            if (logAges[i] >= LOG_LIFE) logUsed[i] = false;
            else any = true;
        }
        if (any || logTexts[0].gameObject.activeSelf) RenderLog();
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
                rt.anchoredPosition = new Vector2(-16f, -172f - i * 28f);
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
        // v3.1: 이전 문구가 아직 떠 있으면 끊고 새 문구로 (두 코루틴이 서로 알파를 덮어쓰던 것)
        if (waveNoticeRoutine != null) StopCoroutine(waveNoticeRoutine);
        waveNoticeRoutine = StartCoroutine(WaveNoticeCoroutine(notice, warning));
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
