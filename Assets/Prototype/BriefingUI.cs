using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [BriefingUI.cs] v1 (신규, v9.9 2026-09-16) - 브리핑 카드: "읽는 동안 세계가 멈추는" 설명 창
///
/// 튜토리얼 계획 v2 §5. 견습 운행(TutorialDirector)의 단계 시작마다, 그리고 정식 런에서 처음 만나는 것
/// (지역 / 새 손님 / 방해 이벤트 / 도박꾼 베팅 / 보스 - 2차 팩에서 훅 연결)을 같은 카드로 설명한다.
///
/// 화면: 어둡게(0.55) + 가운데 위 파이프 창 860x300 (center 앵커, (0,+180) = 화면 y 210~510, 기차 위쪽)
///   - 왼쪽 위 황동 명판 = 화자("스피노" / "새 손님" / "지역" ...)
///   - 제목 22pt 금색 + 본문 17pt 크림 최대 4줄
///   - 오른쪽 초상 판 220x220 (링 카드): 손님 스프라이트(e_*.png 2배) 또는 키 카드(큰 키 글자 + 한 줄)
///   - 왼쪽 아래 "시간 정지 중" / 오른쪽 아래 "[Enter] 알겠다"
/// 닫기 = Enter / KeypadEnter / 마우스 클릭 (열린 뒤 0.15초 지나야 - 열리게 한 키가 바로 닫지 않게).
///   닫힌 프레임은 KeyConsumedFrame 에 남긴다 - 같은 프레임에 [Enter] = 단계 건너뛰기 등이 이중 소비되지 않게.
/// 시간: GameBalance.BriefingPausesTime 이면 timeScale 0 (AugmentListUI 의 pausedByMe 패턴 - 이미 멈춘 화면 위면 손대지 않는다).
///   안 멈추면 BriefingAutoCloseSec 뒤 자동으로 닫힌다.
/// 큐: 여러 장이 겹치면 순서대로 한 장씩. 씬이 다시 로드되면 큐를 비우고 닫는다(콜백 없이).
/// 입력 차단: AugmentListUI.ReadingOpen 이 BriefingUI.IsOpen 을 포함한다 - 이미 그 플래그를 보는 시스템
///   (일시정지·이벤트·슬롯 마커·기관실·조리대·주방창·정비소)이 카드가 떠 있는 동안 키를 무시한다.
///   클릭은 어둡게 판(raycastTarget)이 아래 캔버스로 가는 클릭을 막는다.
/// 1회성: ShowOnce(id, def) - PlayerPrefs "WDT_Brief_<id>" 가 1이면 안 띄우고 false. F4 치트가 지운다.
///
/// 사용법: 파일만 넣으면 자동 생성. 호출은 정적 API
///   BriefingUI.Show(def)                       - 큐에 넣고 차례가 오면 띄운다
///   BriefingUI.ShowOnce("enemy_ptera", def)    - 처음 한 번만
///   def = new BriefingUI.BriefDef { speaker, title, lines, keyGlyph/keyLabel 또는 portrait, ring, onClose }
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class BriefingUI : MonoBehaviour
{
    /// <summary>브리핑 한 장</summary>
    public class BriefDef
    {
        public string id = "";              // 1회성 키 (ShowOnce) / 로그용
        public string speaker = "스피노";    // 명판 글자
        public string title = "";           // 제목 (금색 22pt)
        public string[] lines;              // 본문 (최대 4줄 권장)
        public string keyGlyph = "";        // 키 카드 큰 글자 (예: "[E] 꾹") - portrait 가 없을 때
        public string keyLabel = "";        // 키 카드 아래 한 줄 (예: "멈춘 포탑 곁에서")
        public string portrait = "";        // 초상 스프라이트 이름 (SpriteBank, 예: "e_ptera") - 있으면 키 카드 대신
        public Color ring = new Color(0.84f, 0.667f, 0.282f, 1f);   // 초상 판 테 색 (기본 황동)
        public System.Action onClose;       // 닫힌 뒤 호출 (null 가능)
    }

    public static BriefingUI Instance { get; private set; }

    /// <summary>카드가 떠 있는지 (AugmentListUI.ReadingOpen 이 이 값을 포함 - 다른 시스템 입력 차단)</summary>
    public static bool IsOpen { get; private set; }

    /// <summary>Enter/클릭으로 카드가 닫힌 프레임 - 같은 프레임의 [Enter] 이중 소비 방지 (TutorialDirector 건너뛰기 등)</summary>
    public static int KeyConsumedFrame = -1;

    private const string PREF_PREFIX = "WDT_Brief_";
    private const float MIN_OPEN_SEC = 0.15f;    // 열린 직후 이 시간은 닫기 입력을 무시 (열리게 한 키/클릭이 바로 닫지 않게)

    private static readonly List<BriefDef> queue = new List<BriefDef>();

    private Canvas canvas;
    private GameObject root;
    private RectTransform panel;
    private RectTransform speakerPlate;
    private Text speakerFallback;            // 스킨이 없을 때 명판 대신 글자
    private Text titleText;
    private Text bodyText;
    private RectTransform portraitCard;
    private Image portraitImg;
    private Image portraitRing;              // 스킨 링 (색 갱신용, 없으면 null)
    private Image portraitBorderImg;         // 단색 폴백 테
    private Text keyGlyphText;
    private Text keyLabelText;
    private Text pauseNote;
    private Text closeHint;

    private BriefDef current;
    private bool pausedByMe;
    private float openedAt;                  // unscaled

    // ─────────────────────────────────────────────
    // 부트스트랩 (씬 오브젝트 불필요)
    // ─────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("BriefingUI");
        DontDestroyOnLoad(go);
        go.AddComponent<BriefingUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        IsOpen = false;
        BuildUI();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        // 씬 리로드 안전장치: 이 카드가 멈춘 시간은 반드시 돌려놓는다
        if (pausedByMe) { pausedByMe = false; Time.timeScale = 1f; }
        if (Instance == this) { Instance = null; IsOpen = false; }
    }

    /// <summary>씬이 다시 로드되면(런 포기/재시작) 남은 카드는 전부 버린다 - 콜백 없이 닫는다</summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        queue.Clear();
        if (IsOpen) CloseInternal(false);
    }

    // ─────────────────────────────────────────────
    // 정적 API
    // ─────────────────────────────────────────────
    /// <summary>카드를 큐에 넣는다. 아무것도 안 떠 있으면 다음 프레임에 뜬다</summary>
    public static void Show(BriefDef def)
    {
        if (def == null) return;
        if (!GameBalance.BriefingEnabled)
        {
            // 카드를 끈 상태: 안 보여주고 콜백만 바로 (진행이 막히지 않게)
            if (def.onClose != null) def.onClose();
            return;
        }
        queue.Add(def);
    }

    /// <summary>처음 한 번만 띄운다. 이미 본 카드면 false (콜백도 안 부른다)</summary>
    public static bool ShowOnce(string id, BriefDef def)
    {
        if (def == null || string.IsNullOrEmpty(id)) return false;
        if (PlayerPrefs.GetInt(PREF_PREFIX + id, 0) == 1) return false;
        PlayerPrefs.SetInt(PREF_PREFIX + id, 1);
        PlayerPrefs.Save();
        def.id = id;
        Show(def);
        return true;
    }

    /// <summary>이 카드를 본 적이 있는가</summary>
    public static bool HasSeen(string id)
    {
        return PlayerPrefs.GetInt(PREF_PREFIX + id, 0) == 1;
    }

    /// <summary>대기 중인 카드 전부 버리기 (튜토리얼 그만두기 등)</summary>
    public static void ClearQueue()
    {
        queue.Clear();
    }

    /// <summary>떠 있는 카드 + 큐 전부 닫기 (콜백 없이)</summary>
    public static void CloseAll()
    {
        queue.Clear();
        if (Instance != null && IsOpen) Instance.CloseInternal(false);
    }

    /// <summary>1회성 기록 전체 삭제 (치트 F4). ids = 지울 키 목록이 없으면 알려진 접두어로는 지울 수 없으므로 호출부가 목록을 준다</summary>
    public static void ResetSeen(string[] ids)
    {
        if (ids == null) return;
        for (int i = 0; i < ids.Length; i++) PlayerPrefs.DeleteKey(PREF_PREFIX + ids[i]);
        PlayerPrefs.Save();
    }

    /// <summary>큐에 카드가 남아 있거나 떠 있는가 (디렉터가 "다 읽었나" 확인용)</summary>
    public static bool Busy
    {
        get { return IsOpen || queue.Count > 0; }
    }

    // ─────────────────────────────────────────────
    // 매 프레임
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (!IsOpen)
        {
            if (queue.Count > 0 && CanOpenNow()) OpenNext();
            return;
        }

        // 자동 닫힘 (시간을 안 멈추는 설정)
        if (!GameBalance.BriefingPausesTime && Time.unscaledTime - openedAt >= GameBalance.BriefingAutoCloseSec)
        {
            CloseInternal(true);
            return;
        }

        if (Time.unscaledTime - openedAt < MIN_OPEN_SEC) return;

        bool enter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        bool click = Input.GetMouseButtonDown(0);
        if (enter || click)
        {
            KeyConsumedFrame = Time.frameCount;
            CloseInternal(true);
        }
    }

    /// <summary>다른 전체 화면 연출/메뉴가 떠 있으면 그 뒤에 띄운다</summary>
    private static bool CanOpenNow()
    {
        // 다른 정지 창(정비소/열람/증강 선택) 위에 열리면 시간 소유권이 꼬인다 - 그쪽이 닫힐 때까지 기다린다
        return !StoryTexts.IsBlocking && !PauseMenu.IsOpen && !CookingMinigame.IsActive
            && !WorkshopUI.IsOpen && !AugmentListUI.ReadingOpen && !AugmentPickUI.IsOpen;
    }

    private void OpenNext()
    {
        current = queue[0];
        queue.RemoveAt(0);
        Fill(current);

        root.SetActive(true);
        IsOpen = true;
        openedAt = Time.unscaledTime;

        if (GameBalance.BriefingPausesTime)
        {
            pausedByMe = Time.timeScale > 0f;    // 이미 멈춘 화면 위면 손대지 않는다
            if (pausedByMe) Time.timeScale = 0f;
        }
        else pausedByMe = false;

        SoundManager.Play("sfx_ui_click");   // 클립 없으면 무시
        Debug.Log("[BriefingUI] 카드: " + (string.IsNullOrEmpty(current.id) ? current.title : current.id));
    }

    private void CloseInternal(bool invokeCallback)
    {
        root.SetActive(false);
        IsOpen = false;
        BriefDef closed = current;
        current = null;

        if (pausedByMe)
        {
            pausedByMe = false;
            // 다른 정지 UI 가 떠 있으면 그쪽이 닫힐 때 시간을 돌려준다
            if (!PauseMenu.IsOpen && !AugmentPickUI.IsOpen && !WorkshopUI.IsOpen
                && !FinalOrderUI.QteOpen && !BranchRouteUI.IsOpen && !AugmentListUI.IsOpen && !JournalViewerUI.IsOpen)
                Time.timeScale = 1f;
        }

        if (invokeCallback && closed != null && closed.onClose != null) closed.onClose();
    }

    // ─────────────────────────────────────────────
    // 내용 채우기
    // ─────────────────────────────────────────────
    private void Fill(BriefDef def)
    {
        // 화자 명판
        if (speakerPlate != null) UISkin.Relabel(speakerPlate, def.speaker, 16);
        if (speakerFallback != null) speakerFallback.text = def.speaker;

        titleText.text = def.title;

        string body = "";
        if (def.lines != null)
            for (int i = 0; i < def.lines.Length; i++)
                body += (i > 0 ? "\n" : "") + def.lines[i];
        bodyText.text = body;

        // 초상 / 키 카드
        Sprite p = string.IsNullOrEmpty(def.portrait) ? null : SpriteBank.Get(def.portrait);
        bool hasPortrait = p != null;
        portraitImg.gameObject.SetActive(hasPortrait);
        keyGlyphText.gameObject.SetActive(!hasPortrait);
        keyLabelText.gameObject.SetActive(!hasPortrait);
        if (hasPortrait)
        {
            portraitImg.sprite = p;
            // 월드 스프라이트(32ppu)는 픽셀 크기 그대로 2배 (200 을 넘으면 1배) - 도트가 뭉개지지 않게 정수 배
            float w = p.rect.width, h = p.rect.height;
            float k = (Mathf.Max(w, h) * 2f <= 200f) ? 2f : 1f;
            portraitImg.rectTransform.sizeDelta = new Vector2(w * k, h * k);
        }
        else
        {
            keyGlyphText.text = def.keyGlyph;
            keyLabelText.text = def.keyLabel;
        }
        Color ring = def.ring; ring.a = 1f;
        if (portraitRing != null) portraitRing.color = ring;
        if (portraitBorderImg != null) portraitBorderImg.color = ring;

        pauseNote.text = GameBalance.BriefingPausesTime ? "시간 정지 중" : "";
        closeHint.text = "[Enter] 알겠다";
    }

    // ─────────────────────────────────────────────
    // UI 생성 (목업 v2 (D) 좌표)
    // ─────────────────────────────────────────────
    private const float PANEL_W = 860f;
    private const float PANEL_H = 300f;
    private const float PANEL_CY = 180f;     // center 앵커 기준 y (+ = 위)

    private void BuildUI()
    {
        canvas = UIFactory.CreateCanvas("Briefing_Canvas", 720);   // 일시정지(700) 위
        canvas.transform.SetParent(transform, false);

        // 어둡게 (클릭 차단 - raycastTarget)
        root = new GameObject("Dim");
        root.transform.SetParent(canvas.transform, false);
        RectTransform dimRt = root.AddComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero; dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero; dimRt.offsetMax = Vector2.zero;
        Image dim = root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        dim.raycastTarget = true;

        // 파이프 창 (860x300 >= 600x150 + 테 3 = 파이프 프레임. 스킨 없으면 단색 박스 + 구리 테)
        panel = UIFactory.CreatePanel(root.transform, "BriefingPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-PANEL_W * 0.5f, PANEL_CY - PANEL_H * 0.5f),
            new Vector2(PANEL_W * 0.5f, PANEL_CY + PANEL_H * 0.5f),
            UIFactory.PANEL, UIFactory.COPPER, 3f);

        bool skin = UISkin.Available;

        // 화자 명판 (파이프 위에 4px 걸림) - 스킨 없으면 금색 글자
        if (skin)
            speakerPlate = UISkin.Nameplate(panel, "Speaker", "스피노", 16, new Vector2(0f, 1f), new Vector2(26f, 4f));
        else
        {
            speakerFallback = UIFactory.CreateText(panel, "Speaker", "스피노", 16, UIFactory.GOLD, TextAnchor.MiddleLeft);
            PlaceTopLeft(speakerFallback.rectTransform, 40f, -12f, 300f, 26f);
        }

        // 제목 / 본문
        titleText = UIFactory.CreateText(panel, "Title", "", 22, UIFactory.GOLD, TextAnchor.MiddleLeft);
        PlaceTopLeft(titleText.rectTransform, 40f, -44f, 560f, 32f);

        bodyText = UIFactory.CreateText(panel, "Body", "", 17, UIFactory.CREAM, TextAnchor.UpperLeft);
        PlaceTopLeft(bodyText.rectTransform, 40f, -90f, 560f, 130f);
        bodyText.lineSpacing = 1.3f;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;   // 긴 줄은 접는다 (560 폭)

        // 초상 판 220x200 (오른쪽 위 (-30,-40), 아래 끝 y 60 = "[Enter] 알겠다" 줄 위) - 링 카드
        portraitCard = UIFactory.CreatePanel(panel, "Portrait",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-30f - 220f, -40f - 200f), new Vector2(-30f, -40f),
            UIFactory.PANEL, UIFactory.GOLD, 2f);
        portraitBorderImg = portraitCard.GetComponent<Image>();
        if (skin)
        {
            // CreatePanel 이 바깥 이미지를 링으로 입혔다 - 색 갱신은 그 이미지에
            portraitRing = portraitBorderImg;
            portraitBorderImg = null;
        }

        GameObject imgGo = new GameObject("Sprite");
        imgGo.transform.SetParent(portraitCard, false);
        RectTransform imgRt = imgGo.AddComponent<RectTransform>();
        imgRt.anchorMin = new Vector2(0.5f, 0.5f); imgRt.anchorMax = new Vector2(0.5f, 0.5f);
        imgRt.anchoredPosition = Vector2.zero; imgRt.sizeDelta = new Vector2(200f, 200f);
        portraitImg = imgGo.AddComponent<Image>();
        portraitImg.preserveAspect = true;
        portraitImg.raycastTarget = false;

        keyGlyphText = UIFactory.CreateText(portraitCard, "KeyGlyph", "", 54, UIFactory.GOLD, TextAnchor.MiddleCenter);
        keyGlyphText.rectTransform.anchorMin = new Vector2(0f, 0.5f); keyGlyphText.rectTransform.anchorMax = new Vector2(1f, 1f);
        keyGlyphText.rectTransform.offsetMin = new Vector2(6f, -40f); keyGlyphText.rectTransform.offsetMax = new Vector2(-6f, -40f);   // 판 위쪽 절반, 가운데 (목업 y 96)
        keyGlyphText.resizeTextForBestFit = true; keyGlyphText.resizeTextMinSize = 24; keyGlyphText.resizeTextMaxSize = 54;
        keyGlyphText.horizontalOverflow = HorizontalWrapMode.Wrap;

        keyLabelText = UIFactory.CreateText(portraitCard, "KeyLabel", "", 16, UIFactory.CREAM, TextAnchor.MiddleCenter);
        keyLabelText.rectTransform.anchorMin = new Vector2(0f, 0f); keyLabelText.rectTransform.anchorMax = new Vector2(1f, 0f);
        keyLabelText.rectTransform.pivot = new Vector2(0.5f, 0f);
        keyLabelText.rectTransform.anchoredPosition = new Vector2(0f, 24f); keyLabelText.rectTransform.sizeDelta = new Vector2(-12f, 30f);

        // 아래 줄
        pauseNote = UIFactory.CreateText(panel, "PauseNote", "시간 정지 중", 13, UIFactory.DIM, TextAnchor.LowerLeft);
        pauseNote.rectTransform.anchorMin = new Vector2(0f, 0f); pauseNote.rectTransform.anchorMax = new Vector2(0f, 0f);
        pauseNote.rectTransform.pivot = new Vector2(0f, 0f);
        pauseNote.rectTransform.anchoredPosition = new Vector2(40f, 34f); pauseNote.rectTransform.sizeDelta = new Vector2(300f, 20f);

        closeHint = UIFactory.CreateText(panel, "CloseHint", "[Enter] 알겠다", 15, UIFactory.DIM, TextAnchor.LowerRight);
        closeHint.rectTransform.anchorMin = new Vector2(1f, 0f); closeHint.rectTransform.anchorMax = new Vector2(1f, 0f);
        closeHint.rectTransform.pivot = new Vector2(1f, 0f);
        closeHint.rectTransform.anchoredPosition = new Vector2(-40f, 34f); closeHint.rectTransform.sizeDelta = new Vector2(300f, 22f);

        root.SetActive(false);
    }

    private static void PlaceTopLeft(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }
}
