using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// [UIManager.cs] v2
/// 게임 HUD 전체를 담당하는 UI 관리 스크립트입니다.
/// - v2 변경점 (구시스템 정리):
///   1) 포만감 게이지 / 허기 경고 연출 전부 제거 (허기 시스템 삭제)
///   2) '다음 웨이브' 버튼 UI 제거 (웨이브는 증강 선택 후 자동 진행)
///      - Show/HideNextWaveButton은 다른 스크립트 호환용 빈 함수로만 남김
///   3) HP 바 / 골드·웨이브 텍스트 / 상태 패널 / 웨이브 예고 / 스탯 변화 표시는 유지
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

    /// <summary>
    /// 스탯 변화 / 재료 획득 알림 표시
    /// 사용법: UIManager.Instance?.ShowStatChange("ATK +15%!");
    /// </summary>
    public void ShowStatChange(string message)
    {
        if (statChangeText == null) return;
        StartCoroutine(StatChangeCoroutine(message));
    }

    private IEnumerator StatChangeCoroutine(string message)
    {
        statChangeText.gameObject.SetActive(true);
        statChangeText.text = message;
        statChangeText.color = new Color(1f, 0.9f, 0.2f, 1f); // 노란색

        // 1.5초 동안 위로 올라가며 사라짐
        float elapsed = 0f;
        Vector3 startPos = statChangeText.rectTransform.anchoredPosition;

        while (elapsed < 1.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 1.5f;

            statChangeText.rectTransform.anchoredPosition =
                startPos + new Vector3(0f, t * 50f, 0f);

            Color c = statChangeText.color;
            c.a = 1f - t;
            statChangeText.color = c;

            yield return null;
        }

        statChangeText.gameObject.SetActive(false);
        statChangeText.rectTransform.anchoredPosition = startPos;
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
