using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [SettingsUI.cs] v1 (신규, v9.15 2026-09-29) - 설정창: 화면 / 소리 / 언어를 한 곳에 (테스터 09-29 "홈 화면 설정창에 언어·볼륨·화면 비율을 몰아넣기")
///
/// 창: 어둡게 + 가운데 카드 560x520 - 명판 "설정"
///   화면  : [<] 전체화면 / 창 1920x1080 / 창 1600x900 / 창 1280x720 [>]  (PlayerPrefs WDT_ScreenMode. Alt+Enter 도 된다)
///   소리  : 배경음 슬라이더 0~100% / 효과음 슬라이더 0~100% (SoundManager.BgmVolume/SfxVolume - PlayerPrefs)
///   언어  : 한국어 (지금은 이것뿐) / English - 준비 중 (회색, 눌러도 아무 일 없음. 게임 글 전부를 번역해야 열린다)
///   바닥  : [ESC]/[닫기]
/// 여는 곳: 로비 왼쪽 아래 [설정] 버튼(LobbyUI) / 일시정지 메뉴 [설정](PauseMenu). 일시정지 위에서 열리면 시간은 그쪽이 잡고 있다.
/// 캔버스 720 (일시정지 700 위). IsOpen 동안 PauseMenu 의 ESC 는 양보한다.
///
/// 사용법: 파일만 넣으면 자동 생성 (Bootstrap). LobbyUI/PauseMenu 가 SettingsUI.Toggle() 을 부른다.
///         화면 모드 저장값 적용은 LobbyUI.Awake 가 SettingsUI.ApplySavedScreenMode() 로.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class SettingsUI : MonoBehaviour
{
    public static SettingsUI Instance { get; private set; }

    /// <summary>창이 떠 있는가 (PauseMenu ESC / LobbyUI Enter 출발이 본다)</summary>
    public static bool IsOpen { get; private set; }

    private const int SORT = 720;
    private const float PW = 560f, PH = 520f;

    // ── 화면 모드 (LobbyUI v1.7 에서 옮겨 옴) ──
    public const string SCREEN_PREF = "WDT_ScreenMode";
    private static readonly string[] SCREEN_NAMES = { "전체화면", "창 1920x1080", "창 1600x900", "창 1280x720" };
    private static readonly int[] SCREEN_W = { 0, 1920, 1600, 1280 };
    private static readonly int[] SCREEN_H = { 0, 1080, 900, 720 };

    private Canvas canvas;
    private GameObject root;
    private Text screenLabel;
    private Slider bgmSlider, sfxSlider;
    private Text bgmValue, sfxValue;
    private float openedAt;
    private bool applying = false;   // 슬라이더 값을 코드로 맞추는 중 (콜백 무시)

    // ─────────────────────────────────────────────
    // 정적 API
    // ─────────────────────────────────────────────
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("SettingsUI");
        go.AddComponent<SettingsUI>();
    }

    /// <summary>열기/닫기 (로비 [설정] / 일시정지 [설정])</summary>
    public static void Toggle()
    {
        if (Instance == null) Bootstrap();
        if (Instance == null) return;
        if (IsOpen) Instance.Close(); else Instance.Open();
    }

    public static void CloseIfOpen()
    {
        if (Instance != null && IsOpen) Instance.Close();
    }

    /// <summary>게임 시작 때 저장된 화면 모드 적용 (0 = 전체화면 = 기본이라 손대지 않는다)</summary>
    public static void ApplySavedScreenMode()
    {
        ApplyScreenMode(PlayerPrefs.GetInt(SCREEN_PREF, 0), false);
    }

    public static string ScreenModeName
    {
        get { return SCREEN_NAMES[Mathf.Clamp(PlayerPrefs.GetInt(SCREEN_PREF, 0), 0, SCREEN_NAMES.Length - 1)]; }
    }

    /// <summary>화면 모드 적용. save = PlayerPrefs 에 기록</summary>
    private static void ApplyScreenMode(int mode, bool save)
    {
        mode = Mathf.Clamp(mode, 0, SCREEN_NAMES.Length - 1);
        if (mode == 0)
        {
            if (save) Screen.SetResolution(DisplayW(), DisplayH(), FullScreenMode.FullScreenWindow);
        }
        else
            Screen.SetResolution(SCREEN_W[mode], SCREEN_H[mode], FullScreenMode.Windowed);
        if (save) { PlayerPrefs.SetInt(SCREEN_PREF, mode); PlayerPrefs.Save(); }
        Debug.Log("[SettingsUI] 화면 모드: " + SCREEN_NAMES[mode]);
    }

    private static int DisplayW() { return Screen.currentResolution.width > 0 ? Screen.currentResolution.width : 1920; }
    private static int DisplayH() { return Screen.currentResolution.height > 0 ? Screen.currentResolution.height : 1080; }

    // ─────────────────────────────────────────────
    // 생명 주기
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        IsOpen = false;
        canvas = UIFactory.CreateCanvas("Settings_Canvas", SORT);
        canvas.transform.SetParent(transform, false);
        root = new GameObject("Root");
        BuildUI();
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; IsOpen = false; }
    }

    private void Open()
    {
        IsOpen = true;
        openedAt = Time.unscaledTime;
        RefreshAll();
        root.SetActive(true);
        ModalFeel.Play(root.transform);
        SoundManager.Play("sfx_ui_open");   // 클립 없으면 무시
    }

    private void Close()
    {
        IsOpen = false;
        if (root != null) root.SetActive(false);
        PlayerPrefs.Save();
    }

    private void Update()
    {
        if (!IsOpen) return;
        // 로비 단축키([J] 일지 / [V] 증강 목록 / [M] 명성 상점)가 뒤에서 창을 열면 이 창은 물러난다 (겹쳐 뜨지 않게)
        if (JournalViewerUI.IsOpen || AugmentListUI.ReadingOpen || FameShopUI.IsOpen || TrainingGroundUI.IsOpen) { Close(); return; }
        if (Time.unscaledTime - openedAt < 0.15f) return;   // 연 키가 바로 닫지 않게
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CookingMinigame.EscConsumedFrame = Time.frameCount;   // 같은 프레임에 일시정지 메뉴가 반응하지 않게 (공용 스탬프)
            Close();
        }
    }

    // ─────────────────────────────────────────────
    // 값 갱신
    // ─────────────────────────────────────────────
    private void RefreshAll()
    {
        if (screenLabel != null) screenLabel.text = ScreenModeName;
        applying = true;
        if (bgmSlider != null) bgmSlider.value = Mathf.Round(SoundManager.BgmVolume * 100f);
        if (sfxSlider != null) sfxSlider.value = Mathf.Round(SoundManager.SfxVolume * 100f);
        applying = false;
        if (bgmValue != null) bgmValue.text = Mathf.RoundToInt(SoundManager.BgmVolume * 100f) + "%";
        if (sfxValue != null) sfxValue.text = Mathf.RoundToInt(SoundManager.SfxVolume * 100f) + "%";
    }

    private void CycleScreen(int dir)
    {
        int mode = (PlayerPrefs.GetInt(SCREEN_PREF, 0) + dir + SCREEN_NAMES.Length) % SCREEN_NAMES.Length;
        ApplyScreenMode(mode, true);
        SoundManager.Play("sfx_ui_click");
        if (screenLabel != null) screenLabel.text = SCREEN_NAMES[mode];
    }

    private void OnBgmChanged(float v)
    {
        if (applying) return;
        SoundManager.BgmVolume = v / 100f;
        if (bgmValue != null) bgmValue.text = Mathf.RoundToInt(v) + "%";
    }

    private float lastSfxPreview = -1f;
    private void OnSfxChanged(float v)
    {
        if (applying) return;
        SoundManager.SfxVolume = v / 100f;
        if (sfxValue != null) sfxValue.text = Mathf.RoundToInt(v) + "%";
        // 끌면서 계속 울리지 않게 0.15초에 한 번만 들려준다
        if (Time.unscaledTime - lastSfxPreview > 0.15f) { lastSfxPreview = Time.unscaledTime; SoundManager.Play("sfx_ui_click"); }
    }

    // ─────────────────────────────────────────────
    // UI 생성 (코드 생성 - 씬 작업 0)
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero; rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero; rootRt.offsetMax = Vector2.zero;

        // 어둡게 (클릭 차단)
        GameObject dimGo = new GameObject("Dim");
        dimGo.transform.SetParent(root.transform, false);
        RectTransform dimRt = dimGo.AddComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero; dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero; dimRt.offsetMax = Vector2.zero;
        Image dim = dimGo.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        dim.raycastTarget = true;

        // 가운데 카드
        RectTransform panel = UIFactory.CreatePanel(root.transform, "Panel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-PW * 0.5f, -PH * 0.5f), new Vector2(PW * 0.5f, PH * 0.5f),
            UIFactory.PANEL, UIFactory.GOLD, 3f);

        if (UISkin.Available)
            UISkin.Nameplate(panel, "Title", "설정", 17, new Vector2(0f, 1f), new Vector2(26f, 4f), 90f);
        else
        {
            Text t = UIFactory.CreateText(panel, "Title", "설정", 20, UIFactory.GOLD, TextAnchor.MiddleLeft);
            PlaceTopLeft(t.rectTransform, 30f, -10f, 200f, 28f);
        }

        float y = -58f;

        // ── 화면 ──
        SectionTitle(panel, "화면", y); y -= 34f;
        Button sPrev = UIFactory.CreateButton(panel, "ScreenPrev", "<", new Vector2(36f, 30f), UIFactory.PANEL, UIFactory.CREAM, 18);
        PlaceTopLeft(sPrev.GetComponent<RectTransform>(), 40f, y, 36f, 30f);
        sPrev.onClick.AddListener(delegate { CycleScreen(-1); });
        screenLabel = UIFactory.CreateText(panel, "ScreenLabel", "", 17, UIFactory.CREAM, TextAnchor.MiddleCenter);
        PlaceTopLeft(screenLabel.rectTransform, 84f, y, 220f, 30f);
        Button sNext = UIFactory.CreateButton(panel, "ScreenNext", ">", new Vector2(36f, 30f), UIFactory.PANEL, UIFactory.CREAM, 18);
        PlaceTopLeft(sNext.GetComponent<RectTransform>(), 312f, y, 36f, 30f);
        sNext.onClick.AddListener(delegate { CycleScreen(1); });
        Text sHint = UIFactory.CreateText(panel, "ScreenHint", "Alt+Enter 로도 바뀐다", 13, UIFactory.DIM, TextAnchor.MiddleLeft);
        PlaceTopLeft(sHint.rectTransform, 362f, y, 180f, 30f);
        y -= 54f;

        // ── 소리 ──
        SectionTitle(panel, "소리", y); y -= 34f;
        bgmSlider = MakeVolumeRow(panel, "Bgm", "배경음", y, out bgmValue);
        bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        y -= 44f;
        sfxSlider = MakeVolumeRow(panel, "Sfx", "효과음", y, out sfxValue);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        y -= 54f;

        // ── 언어 ──
        SectionTitle(panel, "언어", y); y -= 34f;
        Button ko = UIFactory.CreateButton(panel, "LangKo", "한국어", new Vector2(150f, 34f), new Color(0.45f, 0.29f, 0.15f), UIFactory.GOLD, 16);
        PlaceTopLeft(ko.GetComponent<RectTransform>(), 40f, y, 150f, 34f);
        Button en = UIFactory.CreateButton(panel, "LangEn", "English  (준비 중)", new Vector2(190f, 34f), UIFactory.PANEL, UIFactory.DIM, 15);
        PlaceTopLeft(en.GetComponent<RectTransform>(), 202f, y, 190f, 34f);
        en.interactable = false;
        Text lHint = UIFactory.CreateText(panel, "LangHint", "지금은 한국어만. 영어는 게임 글 전부를 옮긴 뒤 열린다.", 13, UIFactory.DIM, TextAnchor.MiddleLeft);
        PlaceTopLeft(lHint.rectTransform, 40f, y - 40f, 480f, 22f);
        y -= 100f;

        // ── 바닥 ──
        Button close = UIFactory.CreateButton(panel, "Close", "닫기  (ESC)", new Vector2(180f, 40f), new Color(0.25f, 0.42f, 0.25f), UIFactory.CREAM, 17);
        RectTransform crt = close.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0f); crt.anchorMax = new Vector2(0.5f, 0f); crt.pivot = new Vector2(0.5f, 0f);
        crt.anchoredPosition = new Vector2(0f, 22f);
        close.onClick.AddListener(delegate { Close(); });

        root.SetActive(false);
    }

    private static void SectionTitle(RectTransform panel, string label, float y)
    {
        Text t = UIFactory.CreateText(panel, "Sec_" + label, "- " + label + " -", 15, UIFactory.GOLD, TextAnchor.MiddleLeft);
        PlaceTopLeft(t.rectTransform, 30f, y, 300f, 24f);
    }

    /// <summary>이름 + 슬라이더(0~100) + 값 글자 한 줄. 슬라이더는 코드 생성 (Background / Fill Area / Handle - UISkin.Gauge 가 입힌다)</summary>
    private static Slider MakeVolumeRow(RectTransform panel, string name, string label, float y, out Text valueText)
    {
        Text t = UIFactory.CreateText(panel, name + "_Label", label, 16, UIFactory.CREAM, TextAnchor.MiddleLeft);
        PlaceTopLeft(t.rectTransform, 40f, y, 90f, 30f);

        GameObject go = new GameObject(name + "_Slider");
        go.transform.SetParent(panel, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        PlaceTopLeft(rt, 136f, y + 2f, 300f, 26f);
        Slider s = go.AddComponent<Slider>();
        s.minValue = 0f; s.maxValue = 100f; s.wholeNumbers = true;

        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(go.transform, false);
        RectTransform bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.06f, 0.05f, 1f);

        GameObject faGo = new GameObject("Fill Area");
        faGo.transform.SetParent(go.transform, false);
        RectTransform faRt = faGo.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one; faRt.offsetMin = new Vector2(4f, 4f); faRt.offsetMax = new Vector2(-4f, -4f);
        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(faGo.transform, false);
        RectTransform fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one; fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
        Image fillImg = fillGo.AddComponent<Image>();
        fillImg.color = UISkin.GAUGE_GOLD;
        s.fillRect = fillRt;

        GameObject hsGo = new GameObject("Handle Slide Area");
        hsGo.transform.SetParent(go.transform, false);
        RectTransform hsRt = hsGo.AddComponent<RectTransform>();
        hsRt.anchorMin = Vector2.zero; hsRt.anchorMax = Vector2.one; hsRt.offsetMin = new Vector2(8f, 0f); hsRt.offsetMax = new Vector2(-8f, 0f);
        GameObject hGo = new GameObject("Handle");
        hGo.transform.SetParent(hsGo.transform, false);
        RectTransform hRt = hGo.AddComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(16f, 0f);
        Image hImg = hGo.AddComponent<Image>();
        hImg.color = UIFactory.CREAM;
        s.handleRect = hRt;
        s.targetGraphic = hImg;
        s.direction = Slider.Direction.LeftToRight;

        if (UISkin.Available)
        {
            UISkin.Gauge(s, UISkin.GAUGE_GOLD);   // HP 게이지용이라 손잡이를 끄고 잠근다 - 설정 슬라이더는 다시 켠다
            hsGo.SetActive(true);
            s.interactable = true;
            if (UISkin.Available) UISkin.Plate(hImg, UISkin.BRASS);
        }

        valueText = UIFactory.CreateText(panel, name + "_Value", "", 16, UIFactory.GOLD, TextAnchor.MiddleRight);
        PlaceTopLeft(valueText.rectTransform, 446f, y, 74f, 30f);
        return s;
    }

    private static void PlaceTopLeft(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }
}
