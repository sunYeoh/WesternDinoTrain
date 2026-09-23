using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [TutorialDirector.cs] v2 (v9.12 2026-09-22: 견습 운행 구간화 - 7구간(S1 이동과 첫 포탑 1~6 / S2 감전된 포탑 복구 7 / S3 작살로 재료 얻기 8 / S4 전속 주행 켜고 끄기 9 /
///   S5 요리하며 기차 지키기 10 / S6 증강 선택과 정비 11 / S7 미끼로 첫 보스 상대하기 12(신규)) + 마지막 앞길 카드 13. Begin(segment) 로 한 구간만 (훈련장 TrainingGroundUI),
///   구간마다 PlayerPrefs WDT_Tut_S1..S7 = 1 완료 / 2 건너뜀. 정식 운행 인라인 연습 PlayInline(seg) - 새 기믹 첫 등장 순간(협곡 낙뢰 / 첫 바위 / 레버 웨이브) 손님·스폰·사고·포탑을 멈추고(InlineFreeze)
///   셰프만 움직여 그 행동을 해낸다. 30초 = 힌트 추가(자동 통과 없음), [Enter] = 건너뛰기(기록 2). 큰 카드 540x250 = 연습·예습, 목표 카드 330x156 은 그대로 (목업 v4.2).
///   새끼 발톱 = BossEnemy.practice (보상·카드·베팅 체인 없음), 완료 = 미끼 유인 2회. 처음 실행은 로비 [출발] 도 견습부터 (GameBalance.TutorialForceFirst))
///   / v1.3 (v9.11.1 문구) / v1.2 (v9.9.2: 단계 7~12) / v1.1 (v9.9.1) / v1 (v9.9 2026-09-16) - "견습 운행": 로비 [T]로 들어가는 전용 튜토리얼 런
///
/// 계획: claude/다음팩_설계메모_2026-09-21.md B·C (목업 WDT_튜토리얼목업v4.2_구간화.png 컨펌 09-22). 문구 규칙: claude/문구원칙_2026-09-22.md
/// 링 규칙 (RingStyle): 포탑 = 큰 링, 받침 정렬(-3) 뒤 z / 작살·레버 = 정렬 -4, z -0.01 / 조리대 = 작은 링, 정렬 0 / 바닥 목표 = 작은 링, 정렬 -3.
/// 8단계(구간 3)부터 EngineCabUnlocked = true: EngineCab 이 TutorialGateActive 를 무시하고 바위·작살·레버를 켠다.
///
/// 구조
///  - 정식 씬 없음. 로비에서 Begin(segment) -> GameManager.StartTutorial() 이 Battle 상태로 보내되, 웨이브/보급/메타 기록은 이 디렉터가 맡는다.
///  - 단계 = 브리핑 카드(BriefingUI, 시간 정지) -> 목표 카드(우상단 웨이브 판 아래) + 현장 마커(월드 화살표/링) -> 코드 상태로 판정.
///  - [Enter] = 이 단계 건너뛰기 (브리핑이 떠 있으면 브리핑이 먼저 먹는다 - BriefingUI.KeyConsumedFrame). 건너뛴 단계가 있는 구간은 "건너뜀"(2) 으로 적힌다.
///  - [ESC] 일시정지 메뉴의 "견습 운행 그만두기" -> Quit() (완료 기록 없이 로비).
///  - 전부 돌린 런 완료 -> 요약 카드 -> WDT_TutorialDone=1 (+ WDT_PrologueSeen=1) -> 명성 +30 (최초 1회) -> 로비. 한 구간 런 완료 -> 구간 카드 -> 로비.
///  - 무적: GameBalance.TutorialGodMode 면 실전(10)·예습(12) 전까지 기차 HP 를 매 프레임 가득 채운다.
///  - 방해 이벤트·바위·작살·레버는 BlockAmbient(기본 true) 동안 WaveManager.TutorialGateActive 를 통해 막힌다.
///  - 인라인 연습: Active 는 false 인 채 InlineActive/InlineFreeze 만 켠다. Enemy/BossEnemy/TurretSlot.TickFire/Bullet/KitchenEventManager/WaveManager 가 InlineFreeze 를 보고 쉰다.
///    (Time.timeScale 은 1 - 셰프·조리·[E] 는 그대로 움직인다)
///  - 씬이 다시 로드되면(런 포기 등) 조용히 정리 (정적 플래그 잔존 방지).
///
/// 사용법: 파일만 넣으면 자동 생성. LobbyUI/TrainingGroundUI 가 Begin(segment) 를, WaveManager/EngineCab 이 PlayInline(seg) 를 부른다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class TutorialDirector : MonoBehaviour
{
    public static TutorialDirector Instance { get; private set; }

    /// <summary>견습 운행(전용 런) 진행 중인가 (다른 시스템 가드용: GameManager/WaveManager/치트/힌트)</summary>
    public static bool Active { get; private set; }

    /// <summary>v2: 정식 운행 안에서 인라인 연습 진행 중 (카드·마커·Enter 건너뛰기가 산다)</summary>
    public static bool InlineActive { get; private set; }

    /// <summary>v2: 인라인 연습 동안 손님·보스·스폰·사고 타이머·포탑 사격·탄이 쉰다 (셰프·조리·[E] 는 그대로)</summary>
    public static bool InlineFreeze { get; private set; }

    /// <summary>견습 운행을 한 번이라도 끝까지(전부) 마쳤는가</summary>
    public static bool Done
    {
        get { return PlayerPrefs.GetInt(DONE_KEY, 0) == 1; }
    }

    /// <summary>true 면 방해 이벤트가 막힌다 (WaveManager.TutorialGateActive 가 읽는다). 견습 내내 true</summary>
    public static bool BlockAmbient = true;

    /// <summary>8단계(구간 3)부터 true - EngineCab 이 게이트를 무시하고 바위·작살·레버를 켠다</summary>
    public static bool EngineCabUnlocked = false;

    public const string DONE_KEY = "WDT_TutorialDone";
    public const string SEG_KEY_PREFIX = "WDT_Tut_S";
    public const int SEGMENTS = 7;
    public const int TOTAL_STEPS = 13;

    private const int CARD_SORT = 690;           // 조리 미니게임(30)·증강(600)·배너(610) 위, 일시정지(700) 아래
    private const int ARROW_ORDER = 7;           // 셰프(6) 위
    private const string STARTER_RECIPE = "meat+meat";   // 더블 육포 (GameBalance.StarterFoods[0] 와 같은 것)

    // ─────────────────────────────────────────────
    // 구간 표 (훈련장이 읽는다) - 제목 = 배울 행동 (검토 보고서 §5)
    // ─────────────────────────────────────────────
    public struct SegmentInfo
    {
        public int no;
        public string title;      // 배울 행동
        public string line;       // 한 줄
        public string stepLabel;  // "단계 1~6"
        public string estimate;   // "예상 3분" (실측 전 값)
        public int firstStep, lastStep;
        public SegmentInfo(int no, string title, string line, string stepLabel, string estimate, int firstStep, int lastStep)
        { this.no = no; this.title = title; this.line = line; this.stepLabel = stepLabel; this.estimate = estimate; this.firstStep = firstStep; this.lastStep = lastStep; }
    }

    public static readonly SegmentInfo[] SEGMENT_TABLE = new SegmentInfo[] {
        new SegmentInfo(1, "이동과 첫 포탑",        "달리고, 보급 요리를 포탑에 넣고, 굽는다",           "단계 1~6", "예상 3분",  1, 6),
        new SegmentInfo(2, "감전된 포탑 복구",      "낙뢰로 멈춘 포탑 곁에서 [E] 한 번",                 "단계 7",   "예상 30초", 7, 7),
        new SegmentInfo(3, "작살로 재료 얻기",      "기관실 작살포로 창밖 바위를 낚는다",                "단계 8",   "예상 40초", 8, 8),
        new SegmentInfo(4, "전속 주행 켜고 끄기",   "레버를 당기면 빨라지고 골드가 늘지만 손님도 는다",  "단계 9",   "예상 30초", 9, 9),
        new SegmentInfo(5, "요리하며 기차 지키기",  "손님 무리를 막는다 - 기차가 다친다",               "단계 10",  "예상 1분",  10, 10),
        new SegmentInfo(6, "증강 선택과 정비",      "증강 카드를 고르고 정차역에서 [G] 정비소",          "단계 11",  "예상 40초", 11, 11),
        new SegmentInfo(7, "미끼로 첫 보스 상대하기", "미끼 화덕으로 무리와 새끼 발톱을 유인한다",       "단계 12",  "예상 1분",  12, 12),
    };

    /// <summary>구간 기록: 0 = 아직 / 1 = 완료 / 2 = 건너뜀(단계를 건너뛰었거나 인라인 연습을 건너뜀)</summary>
    public static int SegmentState(int seg)
    {
        return PlayerPrefs.GetInt(SEG_KEY_PREFIX + seg, 0);
    }

    public static bool SegDone(int seg) { return SegmentState(seg) == 1; }

    /// <summary>완료(1)로 적힌 구간 수</summary>
    public static int CompletedCount()
    {
        int n = 0;
        for (int i = 1; i <= SEGMENTS; i++) if (SegmentState(i) == 1) n++;
        return n;
    }

    /// <summary>구간 기록이 하나라도 있는가 (처음 실행 판정)</summary>
    public static bool HasAnyRecord()
    {
        for (int i = 1; i <= SEGMENTS; i++) if (SegmentState(i) != 0) return true;
        return false;
    }

    /// <summary>처음 실행: 로비 [출발] 도 견습부터 (GameBalance.TutorialForceFirst)</summary>
    public static bool MustPlayFirst
    {
        get { return GameBalance.TutorialForceFirst && GameBalance.TutorialRunEnabled && !Done && !HasAnyRecord(); }
    }

    /// <summary>치트 F4: 구간 기록 전부 삭제 (TutorialHint 가 DONE_KEY 와 함께 부른다)</summary>
    public static void ResetSegments()
    {
        for (int i = 1; i <= SEGMENTS; i++) PlayerPrefs.DeleteKey(SEG_KEY_PREFIX + i);
        PlayerPrefs.Save();
    }

    /// <summary>구간 기록 쓰기 - 이미 완료(1)면 건너뜀(2)으로 내리지 않는다</summary>
    private static void MarkSegment(int seg, int state)
    {
        if (seg < 1 || seg > SEGMENTS) return;
        int cur = SegmentState(seg);
        if (cur == 1 && state == 2) return;
        PlayerPrefs.SetInt(SEG_KEY_PREFIX + seg, state);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] 구간 " + seg + " 기록 = " + (state == 1 ? "완료" : "건너뜀"));
    }

    /// <summary>v9.11 이전에 견습을 끝낸 저장: 구간 기록이 없으면 1~6 을 완료로 (7 은 새 구간이라 아직)</summary>
    private static void MigrateOldDone()
    {
        if (!Done || HasAnyRecord()) return;
        for (int i = 1; i <= 6; i++) PlayerPrefs.SetInt(SEG_KEY_PREFIX + i, 1);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] 옛 완료 기록 -> 구간 1~6 완료로 옮김");
    }

    // ─────────────────────────────────────────────
    // 링 스타일
    // ─────────────────────────────────────────────
    private struct RingStyle
    {
        public bool large;      // tut_ring_l(72x26) / tut_ring(36x14)
        public float dy;        // 목표점 기준 y 오프셋 (오브젝트 발 자리)
        public int order;       // sortingOrder
        public float z;         // 같은 정렬값 안에서 앞뒤 (z+ 가 먼저 그려진다 = 뒤)
        public float scale;
        public RingStyle(bool large, float dy, int order, float z, float scale)
        { this.large = large; this.dy = dy; this.order = order; this.z = z; this.scale = scale; }
    }
    private static readonly RingStyle RING_TURRET = new RingStyle(true, -0.6f, -3, 0.05f, 1.0f);    // 받침(-3) 뒤
    private static readonly RingStyle RING_HARPOON = new RingStyle(true, -0.45f, -4, -0.01f, 1.3f); // 작살(-4, z -0.02) 뒤, 두상(-4, z 0) 앞
    private static readonly RingStyle RING_LEVER = new RingStyle(false, -0.5f, -4, -0.01f, 1.2f);   // 레버 기둥(-4, z -0.02) 뒤
    private static readonly RingStyle RING_STATION = new RingStyle(false, 0f, 0, -0.01f, 1.0f);      // 조리대 그림자(0) 위, 조리대(1) 아래
    private static readonly RingStyle RING_FLOOR = new RingStyle(false, 0f, -3, 0.05f, 1.0f);       // 바닥 목표 (갑판 위)

    // ── 진행 상태 ──
    private int step = 0;
    private int runSegment = 0;                  // 0 = 전부, 1~7 = 그 구간만
    private readonly List<int> runSteps = new List<int>();   // 이번 런의 단계 번호 목록 (카드 "목표 n / N")
    private int curSeg = 0;
    private int segSkips = 0;                    // 현재 구간에서 건너뛴 단계 수
    private float runStartTime;                  // unscaled
    private float stepStartTime;                 // scaled (조리/투입 시각 비교용)
    private float stepStartUnscaled;             // 단계별 걸린 시간 로그
    private readonly float[] stepSeconds = new float[TOTAL_STEPS + 1];
    private int skips = 0;
    private int badsAtStart = 0;
    private int retries10 = 0;                   // 실전 단계 재시작 횟수 (로그)
    private int retries12 = 0;                   // 예습 재시작 횟수
    private bool skipRequested = false;
    private bool godMode = false;
    private bool trainStoppedFlag = false;
    private Coroutine runRoutine;
    private Coroutine inlineRoutine;
    private int inlineSeg = 0;

    // ── 목표 카드 UI ──
    private Canvas cardCanvas;
    private GameObject cardRoot;
    private RectTransform cardRect;
    private RectTransform stepPlate;             // 스킨 명판
    private Text stepFallback;                   // 스킨 없을 때
    private Text titleText;
    private Text linesText;
    private Text progressText;
    private Text footLeftText;
    private Text footRightText;
    private readonly List<Image> pips = new List<Image>();
    private Sprite pipSprite;
    private bool cardBig = false;
    private static readonly Color PIP_ON = new Color(0.886f, 0.698f, 0.227f, 1f);
    private static readonly Color PIP_OFF = new Color(0.55f, 0.47f, 0.35f, 1f);
    private static readonly Color HINT_GOLD = new Color(1f, 0.9f, 0.3f, 1f);

    // ── "멈춤" 명판 (인라인 연습 - 손님 머리 위) ──
    private readonly List<RectTransform> freezePlates = new List<RectTransform>();
    private RectTransform canvasRect;
    private float freezeScanAt = 0f;
    private Enemy[] frozenEnemies = new Enemy[0];

    // ── 현장 마커 (월드) ──
    private GameObject markerRoot;
    private Transform ringTf, arrowTf;
    private SpriteRenderer ringSr;
    private Transform followTarget;
    private Vector3 fixedTarget;
    private float arrowTipOffset = 1.0f;
    private RingStyle ringStyle = RING_FLOOR;
    private bool markerOn = false;
    private Sprite arrowSprite, ringSprite, ringLargeSprite;

    // ── 참조 ──
    private Transform chefTf;

    // ─────────────────────────────────────────────
    // 부트스트랩
    // ─────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("TutorialDirector");
        DontDestroyOnLoad(go);
        go.AddComponent<TutorialDirector>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Active = false;          // 씬 리로드 대비 - 정적 플래그는 여기서 항상 내린다
        InlineActive = false; InlineFreeze = false;
        BlockAmbient = true;
        EngineCabUnlocked = false;
        SceneManager.sceneLoaded += OnSceneLoaded;
        MigrateOldDone();
        BuildCard();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) { Instance = null; Active = false; InlineActive = false; InlineFreeze = false; }
    }

    /// <summary>씬이 다시 로드되면 진행 중이던 견습 운행·인라인 연습은 조용히 끝난다 (런 포기/재시작/EndTutorial 전부)</summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (runRoutine != null) { StopCoroutine(runRoutine); runRoutine = null; }
        if (inlineRoutine != null) { StopCoroutine(inlineRoutine); inlineRoutine = null; }
        Active = false;
        InlineActive = false; InlineFreeze = false;
        BlockAmbient = true;
        EngineCabUnlocked = false;
        godMode = false;
        markerRoot = null; ringTf = null; arrowTf = null; ringSr = null; markerOn = false;   // 씬 오브젝트였으므로 이미 사라졌다
        chefTf = null;
        frozenEnemies = new Enemy[0];
        HideFreezePlates();
        if (cardRoot != null) cardRoot.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // 진입 / 종료 (정적 API)
    // ─────────────────────────────────────────────
    /// <summary>로비에서: 견습 운행 전부 (1~7 + 앞길 카드)</summary>
    public static void Begin() { Begin(0); }

    /// <summary>로비에서: segment = 0 전부 / 1~7 그 구간만 (훈련장)</summary>
    public static void Begin(int segment)
    {
        if (Instance == null) Bootstrap();
        if (Instance == null || Active || InlineActive) return;
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Lobby) return;
        if (!GameBalance.TutorialRunEnabled) return;
        if (segment < 0 || segment > SEGMENTS) segment = 0;

        Active = true;
        BlockAmbient = true;
        EngineCabUnlocked = false;
        Instance.runSegment = segment;
        BriefingUI.ClearQueue();
        GameManager.Instance.StartTutorial();          // Battle 상태로 (웨이브 시작 없음)
        Instance.runRoutine = Instance.StartCoroutine(Instance.Run());
        Debug.Log("[Tutorial] 견습 운행 시작 (구간 " + (segment == 0 ? "전부" : segment.ToString()) + ")");
    }

    /// <summary>일시정지 메뉴 "견습 운행 그만두기": 완료 기록 없이 로비로</summary>
    public static void Quit()
    {
        if (Instance == null || !Active) return;
        Debug.Log("[Tutorial] 그만두기 - 단계 " + Instance.step + " 에서 (기록 없음)");
        Instance.Teardown();
        if (GameManager.Instance != null) GameManager.Instance.EndTutorial();
    }

    /// <summary>GameManager.OnTrainDestroyed: 견습 운행 중 기차가 멈추면 게임오버 대신 이쪽 (실전·예습 단계가 재시작 처리)</summary>
    public void OnTrainStopped()
    {
        trainStoppedFlag = true;
    }

    private void Teardown()
    {
        if (runRoutine != null) { StopCoroutine(runRoutine); runRoutine = null; }
        Active = false;
        BlockAmbient = true;
        EngineCabUnlocked = false;
        godMode = false;
        HideMarker();
        if (cardRoot != null) cardRoot.SetActive(false);
        BriefingUI.CloseAll();
        Time.timeScale = 1f;
    }

    // ─────────────────────────────────────────────
    // 매 프레임: 건너뛰기 입력 / 마커 까딱 / 무적 / HUD 글자
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (!Active && !InlineActive) return;

        // [Enter] = 단계(연습) 건너뛰기. 브리핑 카드가 같은 프레임에 Enter 를 먹었으면 무시
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            && !BriefingUI.IsOpen && BriefingUI.KeyConsumedFrame != Time.frameCount
            && !PauseMenu.IsOpen && !CookingMinigame.IsActive && !AugmentListUI.ReadingOpen
            && !WorkshopUI.IsOpen && !AugmentPickUI.IsOpen && !KitchenPanel.IsOpenStatic)   // 다른 창이 시간을 잡고 있을 때는 안 넘어간다
            skipRequested = true;

        TickMarker();
        if (InlineFreeze) TickFreezePlates();
    }

    private void LateUpdate()
    {
        if (!Active && !InlineActive) return;

        UIManager um = UIManager.Instance;
        if (Active)
        {
            // 무적: 실전·예습 단계 전까지 기차는 다치지 않는다 (HP 를 채워 두는 방식 - TrainManager 무수정)
            if (godMode && GameBalance.TutorialGodMode && TrainManager.Instance != null)
                TrainManager.Instance.currentHP = TrainManager.Instance.currentMaxHP;

            // 우상단 판 글자: 웨이브 대신 "견습 운행", 상태 "견습 중" (UIManager.Update 가 매 프레임 덮어쓰므로 LateUpdate 에서 다시)
            if (um != null)
            {
                if (um.waveText != null) um.waveText.text = runSegment == 0 ? "견습 운행" : "훈련장  구간 " + runSegment;
                if (um.stateText != null) um.stateText.text = "견습 중  " + StepPos(step) + " / " + runSteps.Count;
            }
        }
        else if (um != null && um.stateText != null)
            um.stateText.text = "전투 중  (연습 - 손님 멈춤)";
    }

    /// <summary>이번 런에서 단계 n 이 몇 번째인가 (카드 "목표 n / N")</summary>
    private int StepPos(int n)
    {
        int i = runSteps.IndexOf(n);
        return i < 0 ? Mathf.Max(1, n) : i + 1;
    }

    // ─────────────────────────────────────────────
    // 런 본체 (코루틴 상태기계) - 구간 단위
    // ─────────────────────────────────────────────
    private bool RunsSeg(int seg) { return runSegment == 0 || runSegment == seg; }

    private IEnumerator Run()
    {
        yield return null;                                   // Battle 전환이 끝나도록 한 프레임
        runStartTime = Time.unscaledTime;
        skips = 0;
        retries10 = 0; retries12 = 0;
        badsAtStart = CookingBridge.BadsThisRun;
        godMode = true;
        trainStoppedFlag = false;
        for (int i = 0; i < stepSeconds.Length; i++) stepSeconds[i] = 0f;

        // 이번 런의 단계 목록
        runSteps.Clear();
        for (int s = 1; s <= SEGMENTS; s++)
            if (RunsSeg(s))
                for (int n = SEGMENT_TABLE[s - 1].firstStep; n <= SEGMENT_TABLE[s - 1].lastStep; n++) runSteps.Add(n);
        if (runSegment == 0) runSteps.Add(13);

        SetupKit(runSegment);

        if (RunsSeg(1))
        {
            BeginSegment(1);
            yield return Step1_Move();
            yield return Step2_Insert();
            yield return Step3_FirstGuests();
            yield return Step4_Materials();
            yield return Step5_Grill();
            yield return Step6_LevelUp();
            EndSegment(1);
        }
        if (RunsSeg(2)) { BeginSegment(2); yield return Step7_Lightning(); EndSegment(2); }
        if (RunsSeg(3)) { BeginSegment(3); yield return Step8_Harpoon(); EndSegment(3); }
        if (RunsSeg(4)) { BeginSegment(4); yield return Step9_Lever(); EndSegment(4); }
        if (RunsSeg(5)) { BeginSegment(5); yield return Step10_Defense(); EndSegment(5); }
        if (RunsSeg(6)) { BeginSegment(6); yield return Step11_Settlement(); EndSegment(6); }
        if (RunsSeg(7)) { BeginSegment(7); yield return Step12_BossPractice(); EndSegment(7); }
        if (runSegment == 0) yield return Step13_Gambler();

        yield return Finish();
    }

    /// <summary>구간 시작: 예고 띠 한 줄 ("다음 구간" 카드 대신 - 클릭을 늘리지 않는다)</summary>
    private void BeginSegment(int seg)
    {
        curSeg = seg; segSkips = 0;
        SegmentInfo info = SEGMENT_TABLE[seg - 1];
        if (runSegment == 0 && seg > 1)
            UIManager.Instance?.ShowWaveNotice("[구간 " + seg + " / " + SEGMENTS + "]  " + info.title, info.line);
        Debug.Log("[Tutorial] 구간 " + seg + " 시작: " + info.title);
    }

    private void EndSegment(int seg)
    {
        MarkSegment(seg, segSkips > 0 ? 2 : 1);
        curSeg = 0;
    }

    /// <summary>
    /// 시작 상태 (구간별): 슬롯 0 = 더블 육포 프리셋(선대가 걸어둔 포탑) 은 항상. 구간 2·5·7 은 슬롯 1 도 (감전시킬 포탑 / 화력).
    /// 보급 요리 1접시는 구간 1(2단계 투입용)·5. 재료: 구간 5 고기 2, 구간 7 고기 BossPracticeMeat. 구간 3·4·7 은 기관실을 바로 연다.
    /// </summary>
    private void SetupKit(int seg)
    {
        TurretSlot first = SlotAt(0);
        if (first != null && first.IsEmpty && !first.isLocked) first.TryInsertFood(STARTER_RECIPE);
        if (seg == 5 || seg == 7)
        {
            if (first != null && !first.IsEmpty) first.TryInsertFood(STARTER_RECIPE);   // Lv2
            TurretSlot second = SlotAt(1);
            if (second != null && second.IsEmpty && !second.isLocked) second.TryInsertFood(STARTER_RECIPE);
        }
        else if (seg == 2)
        {
            TurretSlot second = SlotAt(1);
            if (second != null && second.IsEmpty && !second.isLocked) second.TryInsertFood(STARTER_RECIPE);
        }
        if ((seg == 0 || seg == 1 || seg == 5) && FoodStock.Instance != null && FoodStock.Instance.Get(STARTER_RECIPE) < 1)
            FoodStock.Instance.Add(STARTER_RECIPE, 1);
        if (seg == 5 && MaterialInventory.Instance != null && MeatCount() < 2) MaterialInventory.Instance.Add(MaterialType.Meat, 2 - MeatCount());
        if (seg == 7 && MaterialInventory.Instance != null && MeatCount() < GameBalance.BossPracticeMeat)
            MaterialInventory.Instance.Add(MaterialType.Meat, GameBalance.BossPracticeMeat - MeatCount());
        if (seg == 3 || seg == 4) EngineCabUnlocked = true;
        UIManager.Instance?.ShowStatChange(seg == 0 || seg == 1 ? "[견습 운행] 보급 요리 도착 - 더블 육포 1접시" : "[훈련장] 구간 " + seg + " - " + SEGMENT_TABLE[seg - 1].title);
    }

    // ── 1. 이동: 통로를 건너 포탑 칸 A 로 ──
    private IEnumerator Step1_Move()
    {
        BeginStep(1, "오른쪽 포탑 칸으로 달려라", "[WASD] 이동, [Shift] 대시\n칸 사이는 통로 발판으로만 건넌다", null, 0, 0);
        float carL = GameBalance.CarEdgesX[2] + 0.12f, carR = GameBalance.CarEdgesX[3] - 0.12f;
        Vector3 target = new Vector3((carL + carR) * 0.5f, 0.3f, 0f);   // 포탑 칸 A 바닥 가운데 (약 4.75, 0.3)
        ShowMarkerAt(target, 0.9f, RING_FLOOR);
        yield return Brief(BriefingTexts.Tutorial(1));

        float blinkAt = Time.time + 20f;
        while (!skipRequested)
        {
            Transform chef = Chef();
            if (chef != null && Vector2.Distance(chef.position, target) <= 1.0f) break;
            if (Time.time > blinkAt) { blinkAt = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] 포탑 칸은 주방 오른쪽 - 칸 사이 발판(통로)으로 건너라"); }
            yield return null;
        }
        yield return EndStep();
    }

    // ── 2. 보급 요리 투입 ──
    private IEnumerator Step2_Insert()
    {
        TurretSlot slot = SlotAt(1);
        BeginStep(2, "보급 요리를 포탑에 넣어라", "하단 바의 더블 육포 카드를 클릭\n그다음 화살표 아래 [+] 이름표를 클릭", "투입", 0, 1);
        if (FoodStock.Instance != null && FoodStock.Instance.Get(STARTER_RECIPE) < 1) FoodStock.Instance.Add(STARTER_RECIPE, 1);
        if (slot != null) ShowMarkerFollow(slot.transform, SlotArrowTip(1), RING_TURRET);
        yield return Brief(BriefingTexts.Tutorial(2));

        int filledAtStart = FilledSlotCount();
        float insertMark2 = Time.time;                       // 브리핑이 닫힌 뒤의 투입만 인정 (프리셋 투입은 그 전)
        float hintAt = Time.time + 15f;
        while (!skipRequested)
        {
            if (FilledSlotCount() > filledAtStart || TurretSlot.LastInsertTime > insertMark2) break;
            if (Time.time > hintAt) { hintAt = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] 요리 카드를 먼저 클릭하고, 포탑 위 [+] 이름표를 클릭 - 마우스"); }
            yield return null;
        }
        SetProgress(1, 1);
        yield return EndStep();
    }

    // ── 3. 첫 손님 (랩터 2, 약체, 기차 무적) ──
    private IEnumerator Step3_FirstGuests()
    {
        BeginStep(3, "첫 손님이다 - 포탑이 알아서 쏜다", "포탑이 손님을 쏘는 걸 지켜봐라\n쓰러진 손님의 재료가 기차로 모인다", "손님", 0, 2);
        HideMarker();
        yield return Brief(BriefingTexts.Tutorial(3));

        float spawnDist = TailSpawnDistance();
        List<Enemy> guests = WaveManager.Instance != null ? WaveManager.Instance.SpawnForTutorial("raptor", 2, 0.7f, 0f, spawnDist) : null;
        int need = guests == null ? 0 : guests.Count;         // 프리팹도 폴백도 없으면 0 - 바로 통과 (막히지 않게)
        Spino("[스피노] 애피타이저다 - 접시가 곧 탄환이지");
        float giveUpAt = Time.time + 90f;
        while (!skipRequested && need > 0)
        {
            int dead = CountDead(guests);
            SetProgress(dead, 2);
            if (dead >= need) break;
            if (Time.time > giveUpAt) break;                  // 포탑이 못 잡는 이상 상황 - 막히지 않게
            yield return null;
        }
        KillAll(guests);
        SetProgress(2, 2);
        yield return EndStep();
    }

    // ── 4. 재료 (자동 흡수 확인) ──
    private IEnumerator Step4_Materials()
    {
        BeginStep(4, "재료가 날아온다 - 하단 바를 봐라", "쓰러진 손님의 재료가\n기차로 빨려 온다", "고기", MeatCount(), 2);

        float fallbackAt = Time.time + 30f;
        while (!skipRequested)
        {
            SetProgress(MeatCount(), 2);
            if (MeatCount() >= 2) break;
            if (Time.time > fallbackAt)
            {
                if (MaterialInventory.Instance != null) MaterialInventory.Instance.Add(MaterialType.Meat, 2 - MeatCount());
                UIManager.Instance?.ShowStatChange("[견습] 찬장에서 고기를 꺼내 뒀다");
                break;
            }
            yield return null;
        }
        if (MeatCount() < 2 && MaterialInventory.Instance != null) MaterialInventory.Instance.Add(MaterialType.Meat, 2 - MeatCount());
        SetProgress(2, 2);
        if (!skipRequested) yield return Brief(BriefingTexts.Tutorial(4));   // 재료 칸 설명은 찬 뒤에
        yield return EndStep();
    }

    // ── 5. 굽기 (진짜 미니게임) ──
    private IEnumerator Step5_Grill()
    {
        CookingStation grill = FindStation(CookingStation.StationType.Grilling);
        BeginStep(5, "그릴에서 더블 육포를 구워라", "그릴 곁에서 [E] → 더블 육포 고르기\n눈금이 판정 칸 안에 오면 [Space]", "조리", 0, 1);
        if (grill != null) ShowMarkerFollow(grill.transform, 1.0f, RING_STATION);
        yield return Brief(BriefingTexts.Tutorial(5));

        float cookMark = Time.time;
        int badsSeen = CookingBridge.BadsThisRun;
        int resupplies = 0;
        bool passedByGift = false;
        while (!skipRequested)
        {
            if (CookingBridge.LastGoodCookTime > cookMark) break;

            // Bad 판정으로 재료를 잃었으면 다시 준다 (3회) - 3회 다 태우면 요리를 주고 넘긴다 (막히지 않게)
            if (CookingBridge.BadsThisRun > badsSeen)
            {
                badsSeen = CookingBridge.BadsThisRun;
                resupplies++;
                if (resupplies > 3)
                {
                    if (FoodStock.Instance != null) FoodStock.Instance.Add(STARTER_RECIPE, 1);
                    UIManager.Instance?.ShowStatChange("[견습] 접시를 하나 구워 뒀다 - 굽기는 나중에 더 연습하자");
                    passedByGift = true;
                    break;
                }
                if (MaterialInventory.Instance != null && MeatCount() < 2) MaterialInventory.Instance.Add(MaterialType.Meat, 2 - MeatCount());
                UIManager.Instance?.ShowStatChange("[견습] 태웠다. 고기를 다시 줬다 - 판정 칸 안에서 [Space]");
            }
            yield return null;
        }
        if (!passedByGift && !skipRequested) Spino("[스피노] 그거다. 접시가 곧 탄환이다");
        SetProgress(1, 1);
        yield return EndStep();
    }

    // ── 6. 투입 = 레벨업 ──
    private IEnumerator Step6_LevelUp()
    {
        TurretSlot first = SlotAt(0);
        BeginStep(6, "구운 육포를 아까 그 포탑에 넣어라", "같은 요리를 넣으면 레벨업\n빈 칸에 넣으면 새 포탑", "투입", 0, 1);
        if (FoodStock.Instance != null && TotalFood() < 1) FoodStock.Instance.Add(STARTER_RECIPE, 1);
        if (first != null) ShowMarkerFollow(first.transform, SlotArrowTip(0), RING_TURRET);
        yield return Brief(BriefingTexts.Tutorial(6));

        float insertMark = Time.time;
        float hintAt = Time.time + 20f;
        while (!skipRequested)
        {
            if (TurretSlot.LastInsertTime > insertMark) break;
            if (Time.time > hintAt) { hintAt = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] 하단 바의 요리 카드를 클릭 → 포탑 이름표 클릭"); }
            yield return null;
        }
        SetProgress(1, 1);
        yield return EndStep();
    }

    // ── 7. 낙뢰: 슬롯 1(없으면 아무 가동 포탑)을 감전시키고 [E] 한 번으로 털게 한다 (기차는 무적) ──
    private IEnumerator Step7_Lightning()
    {
        TurretSlot target = AnyActiveSlot(1);
        BeginStep(7, "낙뢰! 멈춘 포탑에 달려가 [E]", "곁에 서서 [E] 한 번\n스파크가 꺼지면 재가동", "포탑", 0, 1);
        HideMarker();

        // 낙뢰 연출: 흰 번쩍 + 흔들림 + 감전 (브리핑은 그 뒤 - "포탑이 멈췄다" 는 과거형)
        LightningFx();
        if (target != null) target.StunSlot(999f, "감전");
        yield return WaitUnscaled(0.7f);

        yield return Brief(BriefingTexts.Tutorial(7));
        if (target != null && target.IsStunned)
            ShowMarkerFollow(target.transform, SlotArrowTip(SlotIndexOf(target)), RING_TURRET);

        float hintAt = Time.time + 20f;
        while (!skipRequested && target != null)
        {
            if (!target.IsStunned) break;                  // SlotMarkerUI 가 [E] 한 번에 ClearStun
            if (Time.time > hintAt) { hintAt = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] 감전 포탑 바로 앞(벽 쪽)까지 가서 [E] 한 번"); }
            yield return null;
        }
        if (target != null && target.IsStunned) target.ClearStun();   // 건너뛰기 - 멈춘 채로 두지 않는다
        SetProgress(1, 1);
        yield return EndStep();
    }

    /// <summary>낙뢰 연출 (견습 7단계 / 인라인 연습 공용). 정식 운행 협곡 낙뢰는 WaveManager 가 같은 연출을 낸다</summary>
    public static void LightningFx()
    {
        WarningFX.Flash("낙뢰!", 0.6f, new Color(1f, 0.96f, 0.7f));
        GameFeel.Shake(0.2f);
        SoundManager.Play("sfx_boss_warning");   // 클립 없으면 무시
    }

    // ── 8. 작살: 기관실 작살포 [E] - 디렉터가 바위를 띄운다 (v2: 자동 통과 없음 - 30초에 힌트, [Enter] 만) ──
    private IEnumerator Step8_Harpoon()
    {
        EngineCabUnlocked = true;
        BeginStep(8, "작살포로 바위를 낚아라", "기관실 작살포 곁에서 [E]\n놓치면 바위는 또 온다", "바위", 0, 1);
        ShowMarkerAt(new Vector3(GameBalance.HarpoonX, 1.7f, 0f), 1.0f, RING_HARPOON);
        yield return Brief(BriefingTexts.Tutorial(8));

        int mark = EngineCab.HarpoonRetrievals;
        float nextRockAt = 0f;
        float hintAt = Time.time + 15f;
        float hint2At = Time.time + GameBalance.InlineHintSec;
        while (!skipRequested)
        {
            if (EngineCab.HarpoonRetrievals > mark) break;
            // 바위가 없으면 화면 왼쪽에서 하나 띄운다 (지나가 버리면 또)
            if (EngineCab.RockCount == 0 && Time.time >= nextRockAt)
            {
                EngineCab.SpawnRockNow(-13f);
                nextRockAt = Time.time + 1.5f;
            }
            if (Time.time > hintAt) { hintAt = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] 작살포 곁(기관실 왼쪽 위)에서 [E] - 바위가 사거리 안이면 바로 맞는다"); }
            if (Time.time > hint2At) { hint2At = float.MaxValue; AddHintLine("바위가 작살포 앞을 지날 때 [E] - 재장전 " + Mathf.RoundToInt(GameBalance.HarpoonCooldown) + "초. 안 되면 [Enter] 로 건너뛴다"); }
            yield return null;
        }
        SetProgress(1, 1);
        yield return EndStep();
    }

    // ── 9. 레버: [E] 잠깐 꾹 = 전속 주행 (v2: 자동 통과 없음) ──
    private IEnumerator Step9_Lever()
    {
        EngineCabUnlocked = true;
        BeginStep(9, "레버를 당겨라 - 전속 주행", "기관실 레버 곁에서 [E] 잠깐 꾹\n다시 당기면 원래대로", "레버", 0, 1);
        ShowMarkerAt(new Vector3(GameBalance.LeverX, 0.35f, 0f), 1.0f, RING_LEVER);
        yield return Brief(BriefingTexts.Tutorial(9));

        int mark = EngineCab.LeverPulls;
        float hintAt = Time.time + 12f;
        float hint2At = Time.time + GameBalance.InlineHintSec;
        while (!skipRequested)
        {
            if (EngineCab.LeverPulls > mark) break;
            if (Time.time > hintAt) { hintAt = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] 레버 곁에서 [E] 를 " + GameBalance.LeverHoldSec + "초 꾹 - 손을 떼면 취소"); }
            if (Time.time > hint2At) { hint2At = float.MaxValue; AddHintLine("레버는 기관실 오른쪽 아래 기둥 - 곁에 서서 [E] 를 놓지 말고 " + GameBalance.LeverHoldSec + "초. 안 되면 [Enter]"); }
            yield return null;
        }
        SetProgress(1, 1);
        yield return EndStep();
    }

    // ── 10. 실전: 랩터 5 + 프테라 1 (정식 스탯), 기차 피해 켬. 기차가 멈추면 이 단계만 다시 ──
    private IEnumerator Step10_Defense()
    {
        int RAPTORS = Mathf.Max(0, GameBalance.TutorialDefenseRaptors), PTERAS = Mathf.Max(0, GameBalance.TutorialDefensePteras);
        BeginStep(10, "손님이 몰려온다 - 배운 대로 막아라", "접시를 늘리고, 멈춘 포탑은 털어라\n기차가 멈추면 이 단계만 다시", "손님", 0, RAPTORS + PTERAS);
        HideMarker();
        if (MaterialInventory.Instance != null && MeatCount() < 2) MaterialInventory.Instance.Add(MaterialType.Meat, 2 - MeatCount());   // 싸우면서 구울 재료
        yield return Brief(BriefingTexts.Tutorial(10));
        yield return Brief(BriefingTexts.TutorialPtera());   // 정식 운행 첫 등장 카드와 같은 형식 (기록은 안 남긴다)

        godMode = false;                                     // 이 단계만 기차가 다친다
        float statMul = 1.0f;
        int need = 0;
        List<Enemy> guests = SpawnDefenseWave(RAPTORS, PTERAS, statMul, out need);
        Spino("[스피노] 진짜다 - 접시를 늘려라. 멈춘 포탑은 달려가 털어라");
        trainStoppedFlag = false;

        while (!skipRequested && need > 0)
        {
            int dead = CountDead(guests);
            SetProgress(dead, RAPTORS + PTERAS);
            if (dead >= need) break;

            if (trainStoppedFlag)
            {
                // GameManager.OnTrainDestroyed 가 기차를 고쳐 놓고 알렸다 - 손님 정리 -> 재시작 카드 -> 같은 구성 다시 (3회째부터 약체)
                trainStoppedFlag = false;
                retries10++;
                KillAll(guests);
                godMode = true;
                yield return Brief(BriefingTexts.TutorialRetry());
                if (retries10 >= 2) statMul = 0.6f;
                if (retries10 >= 4) { UIManager.Instance?.ShowStatChange("[견습] 손님들이 물러갔다 - 정식 운행에서 다시 해보자"); break; }
                if (MaterialInventory.Instance != null && MeatCount() < 2) MaterialInventory.Instance.Add(MaterialType.Meat, 2 - MeatCount());
                godMode = false;
                guests = SpawnDefenseWave(RAPTORS, PTERAS, statMul, out need);
                SetProgress(0, RAPTORS + PTERAS);
                Debug.Log("[Tutorial] 단계 10 재시작 " + retries10 + "회 (배율 " + statMul + ")");
            }
            yield return null;
        }
        KillAll(guests);
        godMode = true;
        if (TrainManager.Instance != null) TrainManager.Instance.currentHP = TrainManager.Instance.currentMaxHP;
        SetProgress(RAPTORS + PTERAS, RAPTORS + PTERAS);
        yield return EndStep();
    }

    /// <summary>기차 꼬리 오른쪽 화면 안 스폰 거리 (카메라 반폭 15.1u) - 기차 피벗은 0 이라 거리 = 꼬리 x + 3.5</summary>
    private static float TailSpawnDistance()
    {
        float d = GameBalance.CarEdgesX[GameBalance.CarEdgesX.Length - 1] + 3.5f;
        if (TrainManager.Instance != null) d -= TrainManager.Instance.transform.position.x;
        return d;
    }

    /// <summary>실전 구성 스폰: 랩터는 기차 꼬리 오른쪽 화면 안에서, 프테라는 조금 더 멀리서. need = 실제로 나온 수</summary>
    private static List<Enemy> SpawnDefenseWave(int raptors, int pteras, float statMul, out int need)
    {
        List<Enemy> all = new List<Enemy>();
        need = 0;
        if (WaveManager.Instance == null) return all;
        float spawnDist = TailSpawnDistance();
        List<Enemy> a = WaveManager.Instance.SpawnForTutorial("raptor", raptors, statMul, 0f, spawnDist);
        List<Enemy> b = WaveManager.Instance.SpawnForTutorial("ptera", pteras, statMul, 0f, spawnDist + 2.5f);
        if (a != null) all.AddRange(a);
        if (b != null) all.AddRange(b);
        need = all.Count;
        return all;
    }

    // ── 11. 정산: 진짜 증강 선택창 -> [G] 정비소 한 번 열기 ──
    private IEnumerator Step11_Settlement()
    {
        BeginStep(11, "정산 - 증강과 정비소", "[1~5] 증강 하나 고르기\n[G] 정비소를 열어봐라", "증강·정비소", 0, 2);
        HideMarker();
        yield return Brief(BriefingTexts.Tutorial(11));

        bool picked = false;
        if (AugmentPickUI.Instance != null) AugmentPickUI.Instance.Open(1, delegate { picked = true; });
        else picked = true;
        while (!picked && !skipRequested && AugmentPickUI.IsOpen) yield return null;
        if (!picked && !skipRequested) yield return null;   // 콜백이 닫힘 다음 프레임에 오는 경우
        SetProgress(1, 2);

        float hintAt = Time.unscaledTime + 20f;
        bool highlighted = false;
        while (!skipRequested)
        {
            if (WorkshopUI.IsOpen) break;
            if (WorkshopUI.Instance == null) { Debug.LogWarning("[Tutorial] WorkshopUI 없음 - 정비소 반쪽은 건너뛴다"); break; }
            if (!highlighted && Time.unscaledTime > hintAt)
            {
                highlighted = true;
                if (linesText != null) { linesText.text = "[G] 정비소 - 지금 눌러봐라\n(정식 운행에선 정차역마다)"; linesText.color = HINT_GOLD; }
                UIManager.Instance?.ShowStatChange("[견습] [G] 를 눌러 정비소를 열어봐라 - 수리·연마·재료 시장");
            }
            yield return null;
        }
        if (linesText != null) linesText.color = UIFactory.CREAM;
        SetProgress(2, 2);
        while (!skipRequested && WorkshopUI.IsOpen) yield return null;   // 구경 끝날 때까지 (닫으면 완료)
        yield return EndStep();
    }

    // ── 12. (v2 신규) 미니 보스 예습: 새끼 발톱 + 랩터 무리 + 미끼 화덕. 완료 = 미끼 유인 BossPracticeLures 회 ──
    private IEnumerator Step12_BossPractice()
    {
        int LURES = Mathf.Max(1, GameBalance.BossPracticeLures);
        SetCardBig(true);
        BeginStep(12, "미끼로 보스 무리를 유인하라",
            "1. 왼쪽 아래 [미끼 굽기] 를 눌러라. 고기 1개가 든다\n2. 눈금이 판정 구간에 오면 [Space]\n3. 던진 미끼로 무리가 가는 동안 포탑이 때린다\n4. 유인이 끝나면 다시 기차를 노린다 - 남은 시간을 봐라\n   무리가 미끼를 물면 왕(새끼 발톱)도 따라온다",
            "유인", 0, LURES);
        SetFooter("유인 " + LURES + "회 성공 = 완료. 기차 HP " + Mathf.RoundToInt(GameBalance.BossPracticeRetryHp * 100f) + "% 아래면 한 번 다시", "[Enter] 건너뛰기");
        HideMarker();
        if (MaterialInventory.Instance != null && MeatCount() < GameBalance.BossPracticeMeat)
            MaterialInventory.Instance.Add(MaterialType.Meat, GameBalance.BossPracticeMeat - MeatCount());
        yield return Brief(BriefingTexts.TutorialBossPractice());

        // 스폰: 새끼 발톱(연습 보스) + 랩터 무리 - 기차 꼬리 오른쪽에서
        godMode = false;
        trainStoppedFlag = false;
        int luresAtStart = BaitStationUI.LuresThisScene;
        List<Enemy> pack = new List<Enemy>();
        BossEnemy cub = SpawnPractice(pack);
        bool hadCub = cub != null;
        UIManager.Instance?.ShowWaveNotice("[예습]  첫 보스 연습 - 미끼로 무리를 유인하라", "왼쪽 아래 미끼 화덕에서 고기를 굽는다");
        Spino("[스피노] 왕의 새끼다 - 정면으로 붙지 말고 미끼부터 구워라");

        float hintAt = Time.time + 20f;
        float hint2At = Time.time + GameBalance.InlineHintSec;
        bool retried = false;
        while (!skipRequested)
        {
            int lures = BaitStationUI.LuresThisScene - luresAtStart;
            SetProgress(lures, LURES);
            if (lures >= LURES) break;
            if (hadCub && (cub == null || !cub.IsAlive))
            {
                // 새끼를 먼저 잡았다 - 완료 조건은 유인이지만 잡았으면 덤으로 통과 (막히지 않게)
                UIManager.Instance?.ShowStatChange("[예습] 새끼 발톱을 잡았다 - 유인 없이도 통과");
                break;
            }
            // 고기가 다 떨어지면 채워 준다 (미끼 = 고기 1)
            if (MeatCount() <= 0 && !BaitStationUI.TimingActive)
            {
                if (MaterialInventory.Instance != null) MaterialInventory.Instance.Add(MaterialType.Meat, 2);
                UIManager.Instance?.ShowStatChange("[예습] 고기를 2개 더 줬다 - 미끼는 고기 1개");
            }
            // 기차 HP 절반 아래 -> 다시 (1회). 두 번째부터는 기차가 안 다친다
            bool lowHp = TrainManager.Instance != null && TrainManager.Instance.HPRatio <= GameBalance.BossPracticeRetryHp;
            if ((trainStoppedFlag || lowHp) && !godMode)
            {
                trainStoppedFlag = false;
                retries12++;
                if (!retried)
                {
                    retried = true;
                    KillAll(pack); DespawnPractice(ref cub);
                    godMode = true;
                    if (TrainManager.Instance != null) TrainManager.Instance.currentHP = TrainManager.Instance.currentMaxHP;
                    if (MaterialInventory.Instance != null && MeatCount() < GameBalance.BossPracticeMeat)
                        MaterialInventory.Instance.Add(MaterialType.Meat, GameBalance.BossPracticeMeat - MeatCount());
                    yield return Brief(BriefingTexts.TutorialBossRetry());
                    godMode = false;
                    luresAtStart = BaitStationUI.LuresThisScene;
                    cub = SpawnPractice(pack);
                    hadCub = cub != null;
                    SetProgress(0, LURES);
                    AddHintLine("무리가 기차 곁에 붙기 전에 미끼를 던져라 - 굽는 동안은 포탑이 지킨다");
                    Debug.Log("[Tutorial] 단계 12 재시작 1회");
                }
                else
                {
                    godMode = true;
                    if (TrainManager.Instance != null) TrainManager.Instance.currentHP = TrainManager.Instance.currentMaxHP;
                    UIManager.Instance?.ShowStatChange("[예습] 이제 기차는 안 다친다 - 유인 " + LURES + "회를 채우거나 [Enter] 로 건너뛴다");
                }
            }
            if (Time.time > hintAt) { hintAt = float.MaxValue; UIManager.Instance?.ShowStatChange("[예습] 왼쪽 아래 [미끼 굽기] 클릭 -> 눈금이 판정 구간에 올 때 [Space]"); }
            if (Time.time > hint2At) { hint2At = float.MaxValue; AddHintLine("가운데(노란 구간)에서 누를수록 오래 유인한다 (8초). 안 되면 [Enter]"); }
            yield return null;
        }
        KillAll(pack); DespawnPractice(ref cub);
        godMode = true;
        if (TrainManager.Instance != null) TrainManager.Instance.currentHP = TrainManager.Instance.currentMaxHP;
        SetProgress(LURES, LURES);
        if (!skipRequested) Spino("[스피노] 그거다. 왕은 무리를 지키러 온다 - 진짜 왕도 똑같다");
        yield return EndStep();
        SetCardBig(false);
    }

    /// <summary>새끼 발톱 + 랩터 무리 스폰. 돌려주는 값 = 새끼 발톱 (프리팹이 없으면 null - 랩터만으로 진행)</summary>
    private BossEnemy SpawnPractice(List<Enemy> pack)
    {
        pack.Clear();
        if (WaveManager.Instance == null) return null;
        float spawnDist = TailSpawnDistance();
        List<Enemy> raptors = WaveManager.Instance.SpawnForTutorial("raptor", Mathf.Max(0, GameBalance.BossPracticeRaptors), GameBalance.BossPracticeRaptorMul, 0f, spawnDist);
        if (raptors != null) pack.AddRange(raptors);
        BossEnemy cub = WaveManager.Instance.SpawnBossForPractice(spawnDist + 2.5f);
        if (cub == null) UIManager.Instance?.ShowStatChange("[예습] 보스 프리팹이 없다 - 랩터 무리로만 연습한다");
        return cub;
    }

    private void DespawnPractice(ref BossEnemy cub)
    {
        if (cub != null)
        {
            if (cub.IsAlive && BossGimmickSystem.Instance != null) BossGimmickSystem.Instance.ClearBossUI();
            Destroy(cub.gameObject);
            cub = null;
        }
    }

    // ── 13. 도박꾼 / 앞길: 실습 없이 카드 2장 (전부 돌리는 런에만) ──
    private IEnumerator Step13_Gambler()
    {
        BeginStep(13, "도박꾼과 앞길", "카드를 읽어라\n[Enter] 다음 장", null, 0, 0);
        HideMarker();
        yield return Brief(BriefingTexts.Tutorial(12));
        yield return Brief(BriefingTexts.Tutorial(13));
        yield return EndStep();
    }

    private IEnumerator WaitUnscaled(float sec)
    {
        float until = Time.unscaledTime + sec;
        while (Time.unscaledTime < until && (Active || InlineActive)) yield return null;
    }

    /// <summary>슬롯 위 화살표 끝점 높이: 마커 칩(북 = 머리 위 / 남 = 발 아래) 과 안 겹치게 칩 위</summary>
    private static float SlotArrowTip(int slotIndex)
    {
        return GameBalance.IsSouthSlot(slotIndex) ? 0.95f : GameBalance.SlotMarkerYOffset + 0.55f;
    }

    private static int SlotIndexOf(TurretSlot slot)
    {
        if (TurretSlotManager.Instance == null || slot == null) return 0;
        for (int i = 0; i < TurretSlotManager.Instance.slots.Length; i++)
            if (TurretSlotManager.Instance.slots[i] == slot) return i;
        return 0;
    }

    /// <summary>가동 중인 포탑 하나 (preferred 번 슬롯 우선, 없으면 아무거나)</summary>
    private static TurretSlot AnyActiveSlot(int preferred)
    {
        TurretSlot s = SlotAt(preferred);
        if (s != null && !s.IsEmpty && !s.isLocked) return s;
        if (TurretSlotManager.Instance == null) return null;
        for (int i = 0; i < TurretSlotManager.Instance.slots.Length; i++)
        {
            s = TurretSlotManager.Instance.slots[i];
            if (s != null && !s.IsEmpty && !s.isLocked) return s;
        }
        return null;
    }

    // ── 완료 ──
    private IEnumerator Finish()
    {
        HideMarker();
        SetCardBig(false);
        EngineCabUnlocked = false;
        float seconds = Time.unscaledTime - runStartTime;
        int cookFails = CookingBridge.BadsThisRun - badsAtStart;
        if (cardRoot != null) cardRoot.SetActive(false);

        BriefingUI.BriefDef done;
        if (runSegment == 0)
        {
            bool first = !Done;
            PlayerPrefs.SetInt(DONE_KEY, 1);
            if (GameBalance.TutorialSkipsPrologue) PlayerPrefs.SetInt("WDT_PrologueSeen", 1);
            PlayerPrefs.Save();
            if (first && GameBalance.TutorialReward > 0) MetaProgress.AddFame(GameBalance.TutorialReward);

            string perStep = "";
            for (int i = 1; i <= TOTAL_STEPS; i++) perStep += (i > 1 ? " " : "") + i + ":" + Mathf.RoundToInt(stepSeconds[i]);
            Debug.Log("[Tutorial] 완료 " + Mathf.FloorToInt(seconds / 60f) + "분" + Mathf.FloorToInt(seconds % 60f) + "초 | 스킵 " + skips
                + " | 조리 실패 " + cookFails + " | 실전 재시작 " + retries10 + " | 예습 재시작 " + retries12
                + (first ? " | 명성 +" + GameBalance.TutorialReward : " | 재플레이") + " | 단계별 초 " + perStep);
            done = BriefingTexts.TutorialDone(first, GameBalance.TutorialReward, seconds, skips, cookFails);
        }
        else
        {
            Debug.Log("[Tutorial] 구간 " + runSegment + " 끝 " + Mathf.RoundToInt(seconds) + "초 | 스킵 " + skips);
            done = BriefingTexts.TutorialSegmentDone(SEGMENT_TABLE[runSegment - 1].title, seconds, skips > 0);
        }

        bool closed = false;
        done.onClose = delegate { closed = true; };
        BriefingUI.Show(done);
        while (!closed) yield return null;

        Teardown();
        if (GameManager.Instance != null) GameManager.Instance.EndTutorial();
    }

    // ─────────────────────────────────────────────
    // v2: 정식 운행 인라인 연습 (새 기믹 첫 등장 순간, 그 구간만)
    //   2 = 협곡 낙뢰 (WaveManager 가 감전 직후) / 3 = 첫 바위 (EngineCab 이 바위를 띄운 직후, 웨이브 InlineHarpoonMinWave 부터) / 4 = 레버 (WaveManager 가 InlineLeverWave 시작에)
    // ─────────────────────────────────────────────
    /// <summary>지금 이 구간의 인라인 연습을 띄울 수 있는가 (완료·건너뜀 기록이 없고, 전투 중이고, 보스·사고·다른 연습이 없을 때)</summary>
    public static bool WantsInline(int seg)
    {
        if (!GameBalance.InlinePracticeOn || !GameBalance.TutorialRunEnabled) return false;
        if (Instance == null || Active || InlineActive) return false;
        if (seg < 2 || seg > 4 || SegmentState(seg) != 0) return false;
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Battle) return false;
        if (WaveManager.TutorialGateActive) return false;
        if (BossGimmickSystem.Instance != null && BossGimmickSystem.Instance.HasActiveBoss) return false;
        if (KitchenEventManager.IsActive) return false;
        return true;
    }

    /// <summary>인라인 연습 시작. slot = 구간 2 에서 감전된 포탑 (null 이면 디렉터가 하나 고른다)</summary>
    public static void PlayInline(int seg, TurretSlot slot)
    {
        if (!WantsInline(seg)) return;
        InlineActive = true;
        InlineFreeze = true;
        Instance.inlineSeg = seg;
        Instance.inlineRoutine = Instance.StartCoroutine(Instance.InlineRoutine(seg, slot));
        Debug.Log("[Tutorial] 인라인 연습 시작 - 구간 " + seg);
    }

    private IEnumerator InlineRoutine(int seg, TurretSlot slot)
    {
        yield return null;
        while (BriefingUI.Busy && InlineActive) yield return null;   // 첫 등장 카드(사고 카드 등)가 먼저

        skipRequested = false;
        string title = "", lines = "", notice = "", noticeSub = "", hint = "", doneNotice = "";
        int harpoonMark = EngineCab.HarpoonRetrievals, leverMark = EngineCab.LeverPulls;
        float nextRockAt = 0f;

        if (seg == 2)
        {
            if (slot == null || slot.IsEmpty || slot.isLocked) slot = FindStunnedSlot();
            if (slot == null) slot = AnyActiveSlot(1);
            if (slot == null) { EndInline(seg, false, null); yield break; }   // 포탑이 하나도 없으면 연습이 성립하지 않는다 (기록 없음)
            slot.StunSlot(999f, "감전");                      // 연습 동안은 저절로 안 풀린다
            title = "낙뢰로 포탑이 멈췄다";
            lines = "화살표를 따라 \"감전\" 표시가 있는 포탑으로 가라\n포탑 가까이에서 [E] 를 한 번 눌러라\n복구하면 전투가 다시 진행된다\n앞으로 낙뢰가 칠 때마다 이렇게 한다";
            notice = "[연습]  첫 낙뢰 - 손님은 멈춰 있다"; noticeSub = "멈춘 포탑을 복구하라 - 곁에서 [E] 한 번";
            hint = "포탑 바로 앞(벽 쪽)까지 가서 [E] 한 번 - 멀면 안 먹는다";
            doneNotice = "복구했다 - 손님이 다시 온다";
            ShowMarkerFollow(slot.transform, SlotArrowTip(SlotIndexOf(slot)), RING_TURRET);
        }
        else if (seg == 3)
        {
            yield return Brief(BriefingTexts.Tutorial(8));    // 왜 낚나 (시간 정지 카드) - 그 다음 연습 카드
            if (!InlineActive) yield break;
            title = "작살포로 바위를 낚아라";
            lines = "화살표를 따라 기관실 작살포 곁으로 가라\n창밖 바위가 사거리에 들어오면 [E]\n놓치면 바위는 또 온다\n낚은 재료는 기차로 빨려 온다";
            notice = "[연습]  첫 바위 - 손님은 멈춰 있다"; noticeSub = "작살포로 바위를 낚아라 - 기관실 왼쪽 위에서 [E]";
            hint = "작살포 곁(기관실 왼쪽 위)에서 [E] - 바위가 사거리 안에 있을 때만 맞는다";
            doneNotice = "낚았다 - 손님이 다시 온다";
            ShowMarkerAt(new Vector3(GameBalance.HarpoonX, 1.7f, 0f), 1.0f, RING_HARPOON);
        }
        else
        {
            yield return Brief(BriefingTexts.Tutorial(9));
            if (!InlineActive) yield break;
            title = "레버를 당겨 전속 주행을 켜라";
            lines = "화살표를 따라 기관실 레버 곁으로 가라\n[E] 를 " + GameBalance.LeverHoldSec + "초 꾹 누르면 전속 주행\n빨라지면 골드가 늘지만 손님도 더 온다\n다시 당기면 원래대로 - 연습이 끝나면 되돌려 놓는다";
            notice = "[연습]  레버 - 손님은 멈춰 있다"; noticeSub = "레버를 한 번 당겨 봐라 - 곁에서 [E] " + GameBalance.LeverHoldSec + "초 꾹";
            hint = "레버 곁에서 [E] 를 " + GameBalance.LeverHoldSec + "초 꾹 - 손을 떼면 취소";
            doneNotice = "당겼다 - 레버는 원래대로 돌려놨다. 손님이 다시 온다";
            ShowMarkerAt(new Vector3(GameBalance.LeverX, 0.35f, 0f), 1.0f, RING_LEVER);
        }

        SetCardBig(true);
        ShowCard(0, title, lines, null, 0, 0, "연습");
        SetFooter(Mathf.RoundToInt(GameBalance.InlineHintSec) + "초가 지나면 힌트가 더 뜬다", "[Enter] 연습 건너뛰기");
        UIManager.Instance?.ShowWaveNotice(notice, noticeSub);
        freezeScanAt = 0f;

        float hintAt = Time.time + GameBalance.InlineHintSec;
        bool success = false;
        while (InlineActive && !skipRequested)
        {
            if (seg == 2) { if (!slot.IsStunned) { success = true; break; } }
            else if (seg == 3)
            {
                if (EngineCab.HarpoonRetrievals > harpoonMark) { success = true; break; }
                if (EngineCab.RockCount == 0 && Time.time >= nextRockAt) { EngineCab.SpawnRockNow(-13f); nextRockAt = Time.time + 1.5f; }
            }
            else { if (EngineCab.LeverPulls > leverMark) { success = true; break; } }

            if (Time.time > hintAt) { hintAt = float.MaxValue; AddHintLine(hint); UIManager.Instance?.ShowStatChange("[연습] " + hint); }
            yield return null;
        }
        if (!InlineActive) yield break;   // 씬 리로드 등으로 이미 정리됨

        if (success)
        {
            SoundManager.Play("sfx_wave_clear");
            if (titleText != null) { titleText.text = "완료 - " + titleText.text; titleText.color = new Color(0.55f, 0.95f, 0.55f, 1f); }
            if (linesText != null) { linesText.text = "잘했다. 손님이 다시 온다 -"; linesText.color = UIFactory.CREAM; }
            UIManager.Instance?.ShowWaveNotice("", doneNotice);
            HideMarker();
            yield return WaitUnscaled(DONE_BEAT_SEC);
            if (titleText != null) titleText.color = UIFactory.GOLD;
        }
        EndInline(seg, success, slot);
    }

    /// <summary>인라인 연습 정리: 기록(완료 1 / 건너뜀 2), 멈춤 해제, 카드·마커·명판 끄기. 레버 연습은 순항으로 되돌린다</summary>
    private void EndInline(int seg, bool success, TurretSlot slot)
    {
        if (seg == 2 && slot != null && slot.IsStunned) slot.ClearStun();
        if (seg == 4 && EngineCab.FullSteam) EngineCab.ForceCruise();
        if (success) MarkSegment(seg, 1);
        else if (skipRequested)
        {
            MarkSegment(seg, 2);
            UIManager.Instance?.ShowWaveNotice("", "연습을 건너뛰었다 - 로비 [T] 훈련장에서 다시 할 수 있다");
        }
        skipRequested = false;
        HideMarker();
        HideFreezePlates();
        frozenEnemies = new Enemy[0];
        if (cardRoot != null) cardRoot.SetActive(false);
        SetCardBig(false);
        InlineFreeze = false;
        InlineActive = false;
        inlineRoutine = null;
        Debug.Log("[Tutorial] 인라인 연습 끝 - 구간 " + seg + (success ? " 완료" : " 건너뜀/중단"));
    }

    private static TurretSlot FindStunnedSlot()
    {
        if (TurretSlotManager.Instance == null) return null;
        for (int i = 0; i < TurretSlotManager.Instance.slots.Length; i++)
        {
            TurretSlot s = TurretSlotManager.Instance.slots[i];
            if (s != null && !s.IsEmpty && !s.isLocked && s.IsStunned && s.StunKind == "감전") return s;
        }
        return null;
    }

    // ─────────────────────────────────────────────
    // 단계 공통
    // ─────────────────────────────────────────────
    private void BeginStep(int n, string title, string lines, string progLabel, int done, int total)
    {
        step = n;
        stepStartTime = Time.time;
        stepStartUnscaled = Time.unscaledTime;
        skipRequested = false;
        ShowCard(n, title, lines, progLabel, done, total, null);
        Debug.Log("[Tutorial] 단계 " + n + " 시작: " + title);
    }

    /// <summary>
    /// 단계 마무리: 건너뛰었으면 기록만, 해냈으면 카드를 "완료" 상태로 바꿔 DONE_BEAT_SEC 동안 보여준 뒤 다음으로.
    /// (해내자마자 다음 카드가 뜨면 "막무가내로 넘어가는" 느낌 - 유저 피드백 09-16)
    /// </summary>
    private IEnumerator EndStep()
    {
        HideMarker();
        if (step >= 1 && step <= TOTAL_STEPS) stepSeconds[step] = Time.unscaledTime - stepStartUnscaled;
        if (skipRequested)
        {
            skips++; segSkips++;
            Debug.Log("[Tutorial] 단계 " + step + " 건너뜀");
            skipRequested = false;
            yield break;
        }
        SoundManager.Play("sfx_wave_clear");
        if (titleText != null) { titleText.text = "완료 - " + titleText.text; titleText.color = new Color(0.55f, 0.95f, 0.55f, 1f); }
        if (linesText != null) { linesText.text = "잘했다. 다음 -"; linesText.color = UIFactory.CREAM; }
        float until = Time.unscaledTime + DONE_BEAT_SEC;
        while (Time.unscaledTime < until && Active) yield return null;
        if (titleText != null) titleText.color = UIFactory.GOLD;
    }

    private const float DONE_BEAT_SEC = 1.2f;

    /// <summary>브리핑 카드를 띄우고 닫힐 때까지 기다린다 (시간 정지 중에도 프레임은 돈다)</summary>
    private IEnumerator Brief(BriefingUI.BriefDef def)
    {
        if (def == null) yield break;
        bool closed = false;
        def.onClose = delegate { closed = true; };
        BriefingUI.Show(def);
        while (!closed && (Active || InlineActive)) yield return null;
        skipRequested = false;    // 카드를 닫은 Enter 가 건너뛰기로 새지 않게 (같은 프레임 가드에 더해 한 번 더)
    }

    private void Spino(string line)
    {
        UIManager.Instance?.ShowWaveNotice("", line);    // 예고 카드는 비우고 안내 카드(초록 줄)만
    }

    /// <summary>카드에 힌트 (30초 규칙: 자동 통과 대신 힌트). 큰 카드 = 본문 아래 한 줄 추가, 작은 카드 = 본문을 힌트로 바꾼다(두 줄 자리라 덧붙이면 핍과 겹친다)</summary>
    private void AddHintLine(string hint)
    {
        if (linesText == null) return;
        string body = linesText.text;
        if (body.Contains(hint)) return;
        linesText.text = cardBig ? body + "\n" + hint : hint;
        linesText.color = cardBig ? UIFactory.CREAM : HINT_GOLD;
        if (titleText != null) titleText.color = HINT_GOLD;
    }

    // ─────────────────────────────────────────────
    // 판정 헬퍼
    // ─────────────────────────────────────────────
    private Transform Chef()
    {
        if (chefTf != null) return chefTf;
        GameObject go = GameObject.Find("Chef");
        if (go != null) { chefTf = go.transform; return chefTf; }
        ChefController cc = FindFirstObjectByType<ChefController>();
        if (cc != null) chefTf = cc.transform;
        return chefTf;
    }

    private static TurretSlot SlotAt(int i)
    {
        if (TurretSlotManager.Instance == null || i < 0 || i >= TurretSlotManager.Instance.slots.Length) return null;
        return TurretSlotManager.Instance.slots[i];
    }

    private static int FilledSlotCount()
    {
        if (TurretSlotManager.Instance == null) return 0;
        int n = 0;
        for (int i = 0; i < TurretSlotManager.Instance.slots.Length; i++)
        {
            TurretSlot s = TurretSlotManager.Instance.slots[i];
            if (s != null && !s.IsEmpty) n++;
        }
        return n;
    }

    private static int MeatCount()
    {
        return MaterialInventory.Instance != null ? MaterialInventory.Instance.Get(MaterialType.Meat) : 0;
    }

    private static int TotalFood()
    {
        if (FoodStock.Instance == null) return 0;
        int n = 0;
        foreach (RecipeData r in RecipeDatabase.All)
            if (r != null) n += FoodStock.Instance.Get(r.recipeId);
        return n;
    }

    private static CookingStation FindStation(CookingStation.StationType type)
    {
        CookingStation[] all = FindObjectsByType<CookingStation>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].stationType == type) return all[i];
        return null;
    }

    private static int CountDead(List<Enemy> list)
    {
        int dead = 0;
        for (int i = 0; i < list.Count; i++)
            if (list[i] == null || !list[i].IsAlive) dead++;
        return dead;
    }

    private static void KillAll(List<Enemy> list)
    {
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null && list[i].IsAlive) Destroy(list[i].gameObject);
        list.Clear();
    }

    // ─────────────────────────────────────────────
    // 목표 카드 (우상단 웨이브 판 아래 - 목업 v2 (A) 좌표). v2: 큰 카드(540x250, 연습·예습) / 작은 카드(330x156, 목표) 한 위젯
    // ─────────────────────────────────────────────
    private const float CARD_W = 330f;
    private const float CARD_H = 156f;
    private const float BIG_W = 540f;
    private const float BIG_H = 250f;

    private void BuildCard()
    {
        cardCanvas = UIFactory.CreateCanvas("TutorialCard_Canvas", CARD_SORT);
        cardCanvas.transform.SetParent(transform, false);      // 디렉터(DontDestroyOnLoad) 밑 - 씬 리로드에도 남는다
        canvasRect = cardCanvas.GetComponent<RectTransform>();

        RectTransform card = UIFactory.CreatePanel(cardCanvas.transform, "GoalCard",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-8f - CARD_W, -128f - CARD_H), new Vector2(-8f, -128f),
            UIFactory.PANEL, UIFactory.GOLD, 2f);
        cardRoot = card.gameObject;
        cardRect = card;

        if (UISkin.Available)
            stepPlate = UISkin.Nameplate(card, "Step", "목표  1 / " + TOTAL_STEPS, 16, new Vector2(0f, 1f), new Vector2(26f, 4f));
        else
        {
            stepFallback = UIFactory.CreateText(card, "Step", "목표  1 / " + TOTAL_STEPS, 15, UIFactory.GOLD, TextAnchor.MiddleLeft);
            PlaceTopLeft(stepFallback.rectTransform, 26f, -6f, 200f, 22f);
        }

        titleText = UIFactory.CreateText(card, "Title", "", 18, UIFactory.GOLD, TextAnchor.MiddleLeft);
        linesText = UIFactory.CreateText(card, "Lines", "", 15, UIFactory.CREAM, TextAnchor.UpperLeft);
        linesText.lineSpacing = 1.15f;

        pipSprite = SpriteBank.Get("ui_mg_meat");
        for (int i = 0; i < 8; i++)
        {
            GameObject pg = new GameObject("Pip_" + i);
            pg.transform.SetParent(card, false);
            RectTransform prt = pg.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 1f); prt.anchorMax = new Vector2(0f, 1f); prt.pivot = new Vector2(0f, 1f);
            prt.sizeDelta = new Vector2(16f, 16f);
            Image pi = pg.AddComponent<Image>();
            if (pipSprite != null) pi.sprite = pipSprite;
            pi.color = PIP_OFF; pi.raycastTarget = false;
            pg.SetActive(false);
            pips.Add(pi);
        }

        progressText = UIFactory.CreateText(card, "Progress", "", 15, UIFactory.CREAM, TextAnchor.MiddleLeft);

        footLeftText = UIFactory.CreateText(card, "FootL", "", 12, UIFactory.DIM, TextAnchor.LowerLeft);
        footLeftText.rectTransform.anchorMin = new Vector2(0f, 0f); footLeftText.rectTransform.anchorMax = new Vector2(0f, 0f);
        footLeftText.rectTransform.pivot = new Vector2(0f, 0f);
        footLeftText.rectTransform.anchoredPosition = new Vector2(26f, 8f); footLeftText.rectTransform.sizeDelta = new Vector2(360f, 16f);

        footRightText = UIFactory.CreateText(card, "Footer", "[Enter] 건너뛰기", 12, UIFactory.DIM, TextAnchor.LowerRight);
        footRightText.rectTransform.anchorMin = new Vector2(1f, 0f); footRightText.rectTransform.anchorMax = new Vector2(1f, 0f);
        footRightText.rectTransform.pivot = new Vector2(1f, 0f);
        footRightText.rectTransform.anchoredPosition = new Vector2(-16f, 8f); footRightText.rectTransform.sizeDelta = new Vector2(200f, 16f);

        SetCardBig(false);
        cardRoot.SetActive(false);
    }

    /// <summary>카드 크기 전환 - 작은 목표 카드(330x156) / 큰 연습·예습 카드(540x250, 제목 22 / 본문 17 / 최대 5줄 + 힌트)</summary>
    private void SetCardBig(bool big)
    {
        cardBig = big;
        if (cardRect == null) return;
        float w = big ? BIG_W : CARD_W, h = big ? BIG_H : CARD_H;
        cardRect.offsetMin = new Vector2(-8f - w, -128f - h);
        cardRect.offsetMax = new Vector2(-8f, -128f);
        titleText.fontSize = big ? 22 : 18;
        PlaceTopLeft(titleText.rectTransform, 26f, big ? -30f : -24f, w - 52f, big ? 30f : 26f);
        linesText.fontSize = big ? 17 : 15;
        PlaceTopLeft(linesText.rectTransform, 26f, big ? -70f : -56f, w - 52f, big ? 130f : 48f);
        linesText.lineSpacing = big ? 1.2f : 1.15f;
        float pipY = big ? -(h - 44f) : -102f;
        for (int i = 0; i < pips.Count; i++)
            pips[i].rectTransform.anchoredPosition = new Vector2(26f + i * 22f, pipY);
        PlaceTopLeft(progressText.rectTransform, 26f, pipY + 2f, 300f, 20f);
        progressText.fontSize = big ? 16 : 15;
        footLeftText.fontSize = big ? 14 : 12;
        footRightText.fontSize = big ? 14 : 12;
        footLeftText.gameObject.SetActive(big);
        if (!big) { footLeftText.text = ""; footRightText.text = "[Enter] 건너뛰기"; }
    }

    private void SetFooter(string left, string right)
    {
        if (footLeftText != null) footLeftText.text = left ?? "";
        if (footRightText != null) footRightText.text = right ?? "[Enter] 건너뛰기";
    }

    private int progTotal = 0;
    private string progLabel = "";

    /// <summary>카드 표시. n = 단계 번호(0 이면 명판은 chip 글자만), chip = 명판 글자를 직접 줄 때 ("연습")</summary>
    private void ShowCard(int n, string title, string lines, string label, int done, int total, string chip)
    {
        if (cardRoot == null) return;
        cardRoot.SetActive(true);
        string plate = chip != null ? chip : "목표  " + StepPos(n) + " / " + runSteps.Count;
        if (stepPlate != null) UISkin.Relabel(stepPlate, plate, 16);
        if (stepFallback != null) stepFallback.text = plate;
        titleText.text = title;
        titleText.color = UIFactory.GOLD;
        linesText.text = lines;
        linesText.color = UIFactory.CREAM;
        if (!cardBig) SetFooter("", "[Enter] 건너뛰기");
        progLabel = label ?? "";
        progTotal = Mathf.Clamp(total, 0, pips.Count);
        SetProgress(done, total);
    }

    private void SetProgress(int done, int total)
    {
        progTotal = Mathf.Clamp(total, 0, pips.Count);
        for (int i = 0; i < pips.Count; i++)
        {
            bool show = i < progTotal;
            if (pips[i].gameObject.activeSelf != show) pips[i].gameObject.SetActive(show);
            if (show) pips[i].color = i < done ? PIP_ON : PIP_OFF;
        }
        if (progTotal > 0)
        {
            progressText.text = progLabel + "  " + Mathf.Clamp(done, 0, progTotal) + " / " + progTotal;
            float pipY = pips.Count > 0 ? pips[0].rectTransform.anchoredPosition.y : -100f;
            progressText.rectTransform.anchoredPosition = new Vector2(26f + progTotal * 22f + 8f, pipY + 2f);
        }
        else progressText.text = "";
    }

    private static void PlaceTopLeft(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    // ─────────────────────────────────────────────
    // v2: "멈춤" 명판 - 인라인 연습 동안 살아 있는 손님 머리 위 (화면 캔버스, 0.25초마다 손님 목록 갱신)
    // ─────────────────────────────────────────────
    private void TickFreezePlates()
    {
        if (cardCanvas == null || canvasRect == null) return;
        if (Time.unscaledTime >= freezeScanAt)
        {
            freezeScanAt = Time.unscaledTime + 0.25f;
            frozenEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        }
        Camera cam = Camera.main;
        int used = 0;
        if (cam != null)
        {
            for (int i = 0; i < frozenEnemies.Length && used < 16; i++)
            {
                Enemy e = frozenEnemies[i];
                if (e == null || !e.IsAlive) continue;
                Vector3 sp = cam.WorldToScreenPoint(e.transform.position + Vector3.up * 1.25f);
                if (sp.z < 0f) continue;
                Vector2 local;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out local)) continue;
                RectTransform plate = FreezePlate(used);
                plate.gameObject.SetActive(true);
                plate.anchoredPosition = local;
                used++;
            }
        }
        for (int i = used; i < freezePlates.Count; i++)
            if (freezePlates[i] != null && freezePlates[i].gameObject.activeSelf) freezePlates[i].gameObject.SetActive(false);
    }

    private RectTransform FreezePlate(int i)
    {
        while (freezePlates.Count <= i)
        {
            RectTransform rt;
            if (UISkin.Available)
                rt = UISkin.Nameplate(cardCanvas.transform, "Freeze_" + freezePlates.Count, "멈춤", 13, new Vector2(0.5f, 0.5f), Vector2.zero, 56f);
            else
            {
                rt = UIFactory.CreatePanel(cardCanvas.transform, "Freeze_" + freezePlates.Count,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 24f), UIFactory.PANEL, UIFactory.GOLD, 1f);
                UIFactory.CreateText(rt, "Label", "멈춤", 13, UIFactory.GOLD, TextAnchor.MiddleCenter);
            }
            rt.pivot = new Vector2(0.5f, 0f);
            rt.gameObject.SetActive(false);
            freezePlates.Add(rt);
        }
        return freezePlates[i];
    }

    private void HideFreezePlates()
    {
        for (int i = 0; i < freezePlates.Count; i++)
            if (freezePlates[i] != null) freezePlates[i].gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // 현장 마커 (월드 스프라이트 2개: 발밑 링 + 위 화살표) - 목업 v2 (B) + v3 링 규칙 (오브젝트 발 자리, 오브젝트보다 넓게, 밑에 깔림)
    // ─────────────────────────────────────────────
    private void EnsureMarker()
    {
        if (markerRoot != null) return;
        if (arrowSprite == null) arrowSprite = SpriteBank.Get("tut_arrow") ?? PaintFallbackArrow();
        if (ringSprite == null) ringSprite = SpriteBank.Get("tut_ring") ?? PaintFallbackRing(36, 14);
        if (ringLargeSprite == null) ringLargeSprite = SpriteBank.Get("tut_ring_l") ?? PaintFallbackRing(72, 26);

        markerRoot = new GameObject("TutorialMarker");
        ringSr = PixelPainter.Attach(markerRoot.transform, "Ring", ringSprite, Vector3.zero, RING_FLOOR.order);
        SpriteRenderer arrow = PixelPainter.Attach(markerRoot.transform, "Arrow", arrowSprite, Vector3.zero, ARROW_ORDER);
        ringTf = ringSr.transform; arrowTf = arrow.transform;
    }

    /// <summary>고정 위치 위에 마커</summary>
    private void ShowMarkerAt(Vector3 pos, float tipOffset, RingStyle ring)
    {
        EnsureMarker();
        followTarget = null; fixedTarget = pos; arrowTipOffset = tipOffset;
        ApplyRing(ring);
        markerOn = true;
        markerRoot.SetActive(true);
        TickMarker();
    }

    /// <summary>오브젝트(슬롯/조리대)를 따라다니는 마커</summary>
    private void ShowMarkerFollow(Transform t, float tipOffset, RingStyle ring)
    {
        EnsureMarker();
        followTarget = t; arrowTipOffset = tipOffset;
        ApplyRing(ring);
        markerOn = true;
        markerRoot.SetActive(true);
        TickMarker();
    }

    /// <summary>링 스타일 적용: 스프라이트(작은/큰), 정렬, 배율. 위치·z 는 TickMarker 가 매 프레임</summary>
    private void ApplyRing(RingStyle style)
    {
        ringStyle = style;
        if (ringSr == null) return;
        ringSr.sprite = style.large ? ringLargeSprite : ringSprite;
        ringSr.sortingOrder = style.order;
        ringTf.localScale = new Vector3(style.scale, style.scale, 1f);
        ringTf.gameObject.SetActive(true);
    }

    private void HideMarker()
    {
        markerOn = false;
        if (markerRoot != null) markerRoot.SetActive(false);
    }

    private void TickMarker()
    {
        if (!markerOn || markerRoot == null) return;
        Vector3 pos = followTarget != null ? followTarget.position : fixedTarget;
        pos.z = 0f;
        // 까딱: 0.6초 왕복, 4px(0.125u) - 시간 정지 중에도 움직인다 (unscaled)
        float t = Mathf.Repeat(Time.unscaledTime, GameBalance.TutorialMarkerBobSec) / GameBalance.TutorialMarkerBobSec;
        float bob = GameBalance.TutorialMarkerBob * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f));
        // 링 = 오브젝트 발 자리 (dy), 같은 정렬값 안의 앞뒤는 z 로 (z+ = 뒤 = 먼저 그려진다)
        if (ringTf != null) ringTf.position = new Vector3(pos.x, pos.y + ringStyle.dy, ringStyle.z);
        if (arrowTf != null) arrowTf.position = pos + Vector3.up * (arrowTipOffset + bob);
    }

    /// <summary>PNG 없을 때: 구리색 아래 화살표 24x21 (끝점 피벗) - 몸통 8x7 + 머리(폭 22 -> 1) + 검정 1px 외곽</summary>
    private static Sprite PaintFallbackArrow()
    {
        PixelPainter p = new PixelPainter(24, 21);
        Color32 cu = new Color32(184, 112, 52, 255), blk = new Color32(16, 14, 20, 255), hi = new Color32(232, 168, 96, 255);
        p.Rect(8, 1, 15, 7, cu);                            // 몸통 (1~7행, x 8~15)
        p.Rect(8, 1, 15, 1, hi);                            // 윗줄 하이라이트
        p.Rect(7, 0, 16, 0, blk);                           // 위 외곽
        for (int y = 1; y <= 7; y++) { p.Point(7, y, blk); p.Point(16, y, blk); }
        p.Rect(0, 7, 6, 7, blk); p.Rect(17, 7, 23, 7, blk); // 어깨 위 외곽
        for (int y = 8; y <= 18; y++)                       // 머리: 8행 폭 22 (x 1~22) -> 18행 폭 2
        {
            int half = 11 - (y - 8);
            p.Rect(12 - half, y, 11 + half, y, cu);
            p.Point(11 - half, y, blk); p.Point(12 + half, y, blk);
        }
        p.Point(11, 19, cu); p.Point(10, 19, blk); p.Point(12, 19, blk);   // 끝
        p.Point(11, 20, blk);                                             // 끝 아래 외곽 (피벗 줄)
        return p.Bake(32f, 11.5f, 21f);
    }

    /// <summary>PNG 없을 때: 황동 타원 링 (중앙 피벗) - 36x14 작은 링 / 72x26 큰 링</summary>
    private static Sprite PaintFallbackRing(int w, int h)
    {
        PixelPainter p = new PixelPainter(w, h);
        Color32 clear = new Color32(0, 0, 0, 0), brass = new Color32(214, 170, 72, 255), blk = new Color32(16, 14, 20, 200);
        int band = w >= 60 ? 3 : 2;
        p.Ellipse(0, 0, w - 1, h - 1, clear, blk);
        for (int i = 1; i <= band; i++) p.Ellipse(i, i, w - 1 - i, h - 1 - i, clear, brass);
        p.Ellipse(band + 1, band + 1, w - 2 - band, h - 2 - band, clear, blk);
        return p.Bake(32f, w * 0.5f, h * 0.5f);
    }
}
