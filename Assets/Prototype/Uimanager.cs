using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// [UIManager.cs] v3
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
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        logTexts = new Text[LOG_LINES];
        for (int i = 0; i < LOG_LINES; i++)
        {
            Text t = KitchenEventManager.MakeText(canvasGo.transform, "Log" + i, "", 19, LOG_NORMAL);
            RectTransform rt = t.rectTransform;
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-14f, 200f - i * 28f);   // 우측, 위에서 아래로
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
        StartCoroutine(WaveNoticeCoroutine(notice, warning));
    }

    private IEnumerator WaveNoticeCoroutine(string notice, string warning)
    {
        if (waveNoticeText != null)
        {
            waveNoticeText.gameObject.SetActive(true);
            waveNoticeText.text = notice;
            waveNoticeText.color = new Color(1f, 0.9f, 0.2f, 1f); // 노란색
        }

        if (waveWarningText != null && !string.IsNullOrEmpty(warning))
        {
            waveWarningText.gameObject.SetActive(true);
            waveWarningText.text = warning;
            waveWarningText.color = new Color(0.4f, 1f, 0.4f, 1f); // 초록색
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
    }
}
