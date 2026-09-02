using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [LobbyUI.cs] v1 - 로비 개편 (튜토리얼_온보딩_설계 6절 + 화면 검수 "시작 버튼 묻힘")
///
/// 로비를 게임의 대문으로 만든다:
/// - 상단: 타이틀 + 부제
/// - 하단: 큰 [출발한다!] 버튼 (클릭 또는 [Enter])
/// - 안내줄: [M] 명성 상점 / [J] 선대의 일지 / [H] 차장의 안내 일지
/// - 좌하단: 소리 설정 (배경음/효과음 [-][+] - SoundManager의 PlayerPrefs 볼륨 연동)
/// - 우하단: 서체 라이선스 고지 (백로그 "로비 크레딧" 항목)
/// 명성 상점(중앙 패널)은 그대로 두고 이 화면이 위아래로 감싼다.
/// 씬의 구 lobbyPanel(어두운 배경에 묻힌 시작 버튼)은 자동 숨김 (HideLegacyLobbyPanel).
///
/// 사용법: 없음! 파일만 넣으면 자동 생성된다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class LobbyUI : MonoBehaviour
{
    private static LobbyUI instance;

    private Canvas canvas;
    private GameObject root;      // 로비에서만 켜는 묶음
    private Text bgmLabel;
    private Text sfxLabel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("LobbyUI");
        DontDestroyOnLoad(go);
        go.AddComponent<LobbyUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        BuildUI();
    }

    private void Update()
    {
        bool lobby = GameManager.Instance != null
            && GameManager.Instance.currentState == GameManager.GameState.Lobby;

        if (root != null && root.activeSelf != lobby)
            root.SetActive(lobby);
        if (!lobby) return;

        // 구 씬 로비 패널 숨김 (Uimanager.ShowOnlyPanel이 다시 켜도 매 프레임 꺼서 유지)
        if (GameBalance.HideLegacyLobbyPanel && UIManager.Instance != null
            && UIManager.Instance.lobbyPanel != null
            && UIManager.Instance.lobbyPanel.activeSelf)
            UIManager.Instance.lobbyPanel.SetActive(false);

        // [Enter] 출발 (일지/일시정지가 열려 있으면 양보)
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            && !JournalViewerUI.IsOpen && !PauseMenu.IsOpen)
            StartRun();
    }

    private void StartRun()
    {
        SoundManager.Play("sfx_train_whistle");   // 출발 기적 (클립 없으면 무시)
        if (UIManager.Instance != null) UIManager.Instance.OnClickStartGame();
        else GameManager.Instance?.ChangeState(GameManager.GameState.Battle);
        Debug.Log("[LobbyUI] 출발! 로비 -> 전투");
    }

    // ─────────────────────────────────────────────
    // UI 생성 (코드 생성 - 씬 작업 0)
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        canvas = UIFactory.CreateCanvas("Lobby_Canvas", 555);   // 명성 상점(560) 바로 아래

        root = new GameObject("Root");
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        // ── 상단: 타이틀 ──
        Text title = UIFactory.CreateText(root.transform, "Title",
            "WESTERN DINO TRAIN", 46, UIFactory.GOLD, TextAnchor.MiddleCenter);
        SetTopStrip(title.rectTransform, -104f, -30f);

        Text subtitle = UIFactory.CreateText(root.transform, "Subtitle",
            "황야의 마스터 셰프 - 무장 조리 열차의 기록", 18, UIFactory.CREAM, TextAnchor.MiddleCenter);
        SetTopStrip(subtitle.rectTransform, -138f, -104f);

        // ── 하단: 출발 버튼 ──
        Button startBtn = UIFactory.CreateButton(root.transform, "StartBtn",
            "출발한다!  [Enter]", new Vector2(340f, 62f),
            UIFactory.COPPER, UIFactory.CREAM, 26);
        RectTransform startRt = startBtn.GetComponent<RectTransform>();
        startRt.anchorMin = new Vector2(0.5f, 0f);
        startRt.anchorMax = new Vector2(0.5f, 0f);
        startRt.anchoredPosition = new Vector2(0f, 118f);
        startBtn.onClick.AddListener(StartRun);

        // ── 안내줄 ──
        Text guide = UIFactory.CreateText(root.transform, "Guide",
            "[M] 명성 상점 접기/펼치기   |   [J] 선대의 일지   |   [H] 차장의 안내 일지",
            14, UIFactory.DIM, TextAnchor.MiddleCenter);
        guide.rectTransform.anchorMin = new Vector2(0f, 0f);
        guide.rectTransform.anchorMax = new Vector2(1f, 0f);
        guide.rectTransform.offsetMin = new Vector2(0f, 58f);
        guide.rectTransform.offsetMax = new Vector2(0f, 84f);

        // ── 좌하단: 소리 설정 ──
        Text soundTitle = UIFactory.CreateText(root.transform, "SoundTitle",
            "- 소리 설정 -", 14, UIFactory.CREAM, TextAnchor.MiddleLeft);
        SetCorner(soundTitle.rectTransform, true, 16f, 96f, 200f, 22f);

        bgmLabel = MakeVolumeRow(true, 62f);
        sfxLabel = MakeVolumeRow(false, 28f);
        RefreshVolumeLabels();

        // ── 우하단: 서체 고지 (크레딧) ──
        Text credit = UIFactory.CreateText(root.transform, "Credit",
            "서체: Neo둥근모 (라이선스: FONT_LICENSE 파일 참조)", 11,
            UIFactory.DIM, TextAnchor.MiddleRight);
        credit.rectTransform.anchorMin = new Vector2(1f, 0f);
        credit.rectTransform.anchorMax = new Vector2(1f, 0f);
        credit.rectTransform.pivot = new Vector2(1f, 0f);
        credit.rectTransform.sizeDelta = new Vector2(420f, 20f);
        credit.rectTransform.anchoredPosition = new Vector2(-14f, 12f);

        root.SetActive(false);   // 상태 폴링이 로비에서 켠다
    }

    /// <summary>볼륨 조절 한 줄: 이름 [-] 수치 [+]</summary>
    private Text MakeVolumeRow(bool bgm, float y)
    {
        string rowName = bgm ? "BgmRow" : "SfxRow";

        Text label = UIFactory.CreateText(root.transform, rowName + "_Label", "", 14,
            UIFactory.CREAM, TextAnchor.MiddleLeft);
        SetCorner(label.rectTransform, true, 58f, y, 150f, 26f);

        Button minus = UIFactory.CreateButton(root.transform, rowName + "_Minus", "-",
            new Vector2(30f, 26f), UIFactory.PANEL, UIFactory.CREAM, 18);
        PlaceCornerButton(minus, 16f, y);
        minus.onClick.AddListener(delegate { AdjustVolume(bgm, -0.1f); });

        Button plus = UIFactory.CreateButton(root.transform, rowName + "_Plus", "+",
            new Vector2(30f, 26f), UIFactory.PANEL, UIFactory.CREAM, 18);
        PlaceCornerButton(plus, 214f, y);
        plus.onClick.AddListener(delegate { AdjustVolume(bgm, 0.1f); });

        return label;
    }

    private void AdjustVolume(bool bgm, float delta)
    {
        if (bgm) SoundManager.BgmVolume = SoundManager.BgmVolume + delta;
        else
        {
            SoundManager.SfxVolume = SoundManager.SfxVolume + delta;
            SoundManager.Play("sfx_ui_click");   // 새 크기 즉시 들려주기
        }
        RefreshVolumeLabels();
    }

    private void RefreshVolumeLabels()
    {
        if (bgmLabel != null)
            bgmLabel.text = "배경음  " + Mathf.RoundToInt(SoundManager.BgmVolume * 100f) + "%";
        if (sfxLabel != null)
            sfxLabel.text = "효과음  " + Mathf.RoundToInt(SoundManager.SfxVolume * 100f) + "%";
    }

    // ─────────────────────────────────────────────
    // 배치 헬퍼
    // ─────────────────────────────────────────────
    /// <summary>화면 상단 가로줄 (top 기준 offsetY0 ~ offsetY1)</summary>
    private static void SetTopStrip(RectTransform rt, float yMin, float yMax)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(0f, yMin);
        rt.offsetMax = new Vector2(0f, yMax);
    }

    /// <summary>좌/우 하단 코너 고정 (left=true면 좌하단 기준 x,y)</summary>
    private static void SetCorner(RectTransform rt, bool left, float x, float y, float w, float h)
    {
        Vector2 a = left ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
        rt.anchorMin = a;
        rt.anchorMax = a;
        rt.pivot = new Vector2(left ? 0f : 1f, 0f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(left ? x : -x, y);
    }

    private static void PlaceCornerButton(Button b, float x, float y)
    {
        RectTransform rt = b.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(x, y);
    }
}
