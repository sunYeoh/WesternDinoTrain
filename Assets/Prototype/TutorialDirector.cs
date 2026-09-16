using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [TutorialDirector.cs] v1.2 (v9.9.2 2026-09-16: 단계 7~12 - 낙뢰 [E] / 작살 / 레버 / 실전 랩터 5 + 프테라 1 (기차 피해 켬, 멈추면 이 단계만 재시작) /
///   정산 = 진짜 증강 선택 + [G] 정비소 / 도박꾼·앞길 카드. 발밑 링은 오브젝트 "발" 자리에 오브젝트보다 넓게(tut_ring_l 72x26) - 포탑·작살·레버 밑에 깔린다 (유저 09-16).
///   단계별 걸린 시간 로그) / v1.1 (v9.9.1: 1단계 목표 = 통로 건너 포탑 칸 / 손님은 화면 오른쪽에서 / 4단계는 재료가 찬 뒤 설명 / 완료 1.2초 비트)
///   / v1 (신규, v9.9 2026-09-16) - "견습 운행": 로비 [T]로 들어가는 전용 튜토리얼 런
///
/// 계획: claude/튜토리얼_완성계획_2026-09-15.md v2. 교수 요구 "제대로 된 튜토리얼" (마감 9/28 주). 목업 = WDT_튜토리얼목업v3.png (컨펌 09-16).
/// 단계: 1 이동 / 2 보급 투입 / 3 첫 손님 / 4 재료 / 5 굽기 / 6 레벨업 / 7 낙뢰 [E] / 8 작살 / 9 레버 / 10 실전 / 11 정산 / 12 도박꾼·앞길.
/// 링 규칙 (RingStyle): 포탑 = 큰 링, 받침 정렬(-3) 뒤 z / 작살·레버 = 정렬 -4, z -0.01 (EngineCab 이 작살·레버를 z -0.02 로 앞에 둔다) /
///   조리대 = 작은 링, 정렬 0 (그림자 0 위, 조리대 1 아래) / 바닥 목표 = 작은 링, 정렬 -3.
/// 8단계부터 EngineCabUnlocked = true: EngineCab 이 TutorialGateActive 를 무시하고 바위·작살·레버를 켠다 (방해 이벤트는 계속 막힘 - BlockAmbient).
///
/// 구조
///  - 정식 씬 없음. 로비에서 Begin() -> GameManager.StartTutorial() 이 Battle 상태로 보내되, 웨이브/보급/메타 기록은
///    WaveManager 가 아니라 이 디렉터가 맡는다 (GameManager.HandleBattlePhase 는 Active 면 바로 돌아온다).
///  - 단계 = 브리핑 카드(BriefingUI, 시간 정지) -> 목표 카드(우상단 웨이브 판 아래) + 현장 마커(월드 화살표/링) -> 코드 상태로 판정.
///  - [Enter] = 이 단계 건너뛰기 (브리핑이 떠 있으면 브리핑이 먼저 먹는다 - BriefingUI.KeyConsumedFrame).
///  - [ESC] 일시정지 메뉴의 "견습 운행 그만두기" -> Quit() (완료 기록 없이 로비).
///  - 완료 -> 요약 카드 -> PlayerPrefs WDT_TutorialDone=1 (+ WDT_PrologueSeen=1: 정식 런 프롤로그 생략) -> 명성 +30 (최초 1회)
///    -> GameManager.EndTutorial() (GameManager 파괴 + 씬 리로드 = 로비).
///  - 무적: GameBalance.TutorialGodMode 면 실전 방어 단계(2차 팩) 전까지 기차 HP 를 매 프레임 가득 채운다.
///  - 방해 이벤트·바위·작살·레버는 BlockAmbient(기본 true) 동안 WaveManager.TutorialGateActive 를 통해 막힌다.
///  - 씬이 다시 로드되면(런 포기 등) 조용히 정리 (정적 Active 잔존 방지).
///
/// 목표 카드 (목업 v2 (A)): 우상단 (-8,-128) 330x156 링 카드 - 명판 "목표 n / 12" + 제목 18 금색 + 지시 15 크림 + 진행 핍 + [Enter] 건너뛰기.
/// 현장 마커 (목업 v2 (B)): tut_arrow(24x21, 끝점 피벗) 가 목표 위 +offset 에서 위아래 4px 까딱 + tut_ring(36x14) 발밑. PNG 없으면 코드 도트.
///
/// 사용법: 파일만 넣으면 자동 생성. LobbyUI 가 TutorialDirector.Begin() 을 부른다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class TutorialDirector : MonoBehaviour
{
    public static TutorialDirector Instance { get; private set; }

    /// <summary>견습 운행 진행 중인가 (다른 시스템 가드용: GameManager/WaveManager/치트/힌트)</summary>
    public static bool Active { get; private set; }

    /// <summary>견습 운행을 한 번이라도 끝까지 마쳤는가 (로비 [T] 강조 여부)</summary>
    public static bool Done
    {
        get { return PlayerPrefs.GetInt(DONE_KEY, 0) == 1; }
    }

    /// <summary>true 면 방해 이벤트가 막힌다 (WaveManager.TutorialGateActive 가 읽는다). 견습 내내 true - 사고는 정식 런 첫 등장 카드가 가르친다</summary>
    public static bool BlockAmbient = true;

    /// <summary>v1.2: 8단계부터 true - EngineCab 이 게이트를 무시하고 바위·작살·레버를 켠다 (이벤트는 여전히 BlockAmbient)</summary>
    public static bool EngineCabUnlocked = false;

    public const string DONE_KEY = "WDT_TutorialDone";
    public const int TOTAL_STEPS = 12;
    private const int IMPLEMENTED_STEPS = 12;    // v1.2: 전부

    private const int CARD_SORT = 690;           // 조리 미니게임(30)·증강(600)·배너(610) 위, 일시정지(700) 아래
    private const int ARROW_ORDER = 7;           // 셰프(6) 위
    private const string STARTER_RECIPE = "meat+meat";   // 더블 육포 (GameBalance.StarterFoods[0] 와 같은 것)

    /// <summary>발밑 링 스타일 - 오브젝트마다 "발" 자리·정렬이 다르다 (목업 v3 링 수정: 링은 오브젝트 밑에 깔린다)</summary>
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
    private float runStartTime;                  // unscaled
    private float stepStartTime;                 // scaled (조리/투입 시각 비교용)
    private float stepStartUnscaled;             // v1.2: 단계별 걸린 시간 로그
    private readonly float[] stepSeconds = new float[TOTAL_STEPS + 1];
    private int skips = 0;
    private int badsAtStart = 0;
    private int retries10 = 0;                   // v1.2: 실전 단계 재시작 횟수 (로그)
    private bool skipRequested = false;
    private bool godMode = false;
    private bool trainStoppedFlag = false;
    private Coroutine runRoutine;

    // ── 목표 카드 UI ──
    private Canvas cardCanvas;
    private GameObject cardRoot;
    private RectTransform stepPlate;             // 스킨 명판
    private Text stepFallback;                   // 스킨 없을 때
    private Text titleText;
    private Text linesText;
    private Text progressText;
    private readonly List<Image> pips = new List<Image>();
    private Sprite pipSprite;
    private static readonly Color PIP_ON = new Color(0.886f, 0.698f, 0.227f, 1f);
    private static readonly Color PIP_OFF = new Color(0.55f, 0.47f, 0.35f, 1f);

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
        BlockAmbient = true;
        EngineCabUnlocked = false;
        SceneManager.sceneLoaded += OnSceneLoaded;
        BuildCard();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) { Instance = null; Active = false; }
    }

    /// <summary>씬이 다시 로드되면 진행 중이던 견습 운행은 조용히 끝난다 (런 포기/재시작/EndTutorial 전부)</summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (runRoutine != null) { StopCoroutine(runRoutine); runRoutine = null; }
        Active = false;
        BlockAmbient = true;
        EngineCabUnlocked = false;
        godMode = false;
        markerRoot = null; ringTf = null; arrowTf = null; ringSr = null; markerOn = false;   // 씬 오브젝트였으므로 이미 사라졌다
        chefTf = null;
        if (cardRoot != null) cardRoot.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // 진입 / 종료 (정적 API)
    // ─────────────────────────────────────────────
    /// <summary>로비에서 [T]: 견습 운행 시작</summary>
    public static void Begin()
    {
        if (Instance == null) Bootstrap();
        if (Instance == null || Active) return;
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Lobby) return;
        if (!GameBalance.TutorialRunEnabled) return;

        Active = true;
        BlockAmbient = true;
        EngineCabUnlocked = false;
        BriefingUI.ClearQueue();
        GameManager.Instance.StartTutorial();          // Battle 상태로 (웨이브 시작 없음)
        Instance.runRoutine = Instance.StartCoroutine(Instance.Run());
        Debug.Log("[Tutorial] 견습 운행 시작");
    }

    /// <summary>일시정지 메뉴 "견습 운행 그만두기": 완료 기록 없이 로비로</summary>
    public static void Quit()
    {
        if (Instance == null || !Active) return;
        Debug.Log("[Tutorial] 그만두기 - 단계 " + Instance.step + " 에서 (기록 없음)");
        Instance.Teardown();
        if (GameManager.Instance != null) GameManager.Instance.EndTutorial();
    }

    /// <summary>GameManager.OnTrainDestroyed: 견습 운행 중 기차가 멈추면 게임오버 대신 이쪽 (2차 팩 실전 단계에서 재시작 처리)</summary>
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
        if (!Active) return;

        // [Enter] = 단계 건너뛰기. 브리핑 카드가 같은 프레임에 Enter 를 먹었으면 무시
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            && !BriefingUI.IsOpen && BriefingUI.KeyConsumedFrame != Time.frameCount
            && !PauseMenu.IsOpen && !CookingMinigame.IsActive && !AugmentListUI.ReadingOpen
            && !WorkshopUI.IsOpen && !AugmentPickUI.IsOpen && !KitchenPanel.IsOpenStatic)   // 다른 창이 시간을 잡고 있을 때는 안 넘어간다
            skipRequested = true;

        TickMarker();
    }

    private void LateUpdate()
    {
        if (!Active) return;

        // 무적: 실전 단계 전까지 기차는 다치지 않는다 (HP 를 채워 두는 방식 - TrainManager 무수정)
        if (godMode && GameBalance.TutorialGodMode && TrainManager.Instance != null)
            TrainManager.Instance.currentHP = TrainManager.Instance.currentMaxHP;

        // 우상단 판 글자: 웨이브 대신 "견습 운행", 상태 "견습 중" (UIManager.Update 가 매 프레임 덮어쓰므로 LateUpdate 에서 다시)
        UIManager um = UIManager.Instance;
        if (um != null)
        {
            if (um.waveText != null) um.waveText.text = "견습 운행";
            if (um.stateText != null) um.stateText.text = "견습 중  " + step + " / " + TOTAL_STEPS;
        }
    }

    // ─────────────────────────────────────────────
    // 런 본체 (코루틴 상태기계)
    // ─────────────────────────────────────────────
    private IEnumerator Run()
    {
        yield return null;                                   // Battle 전환이 끝나도록 한 프레임
        runStartTime = Time.unscaledTime;
        skips = 0;
        retries10 = 0;
        badsAtStart = CookingBridge.BadsThisRun;
        godMode = true;
        trainStoppedFlag = false;
        for (int i = 0; i < stepSeconds.Length; i++) stepSeconds[i] = 0f;

        SetupKit();

        yield return Step1_Move();
        yield return Step2_Insert();
        yield return Step3_FirstGuests();
        yield return Step4_Materials();
        yield return Step5_Grill();
        yield return Step6_LevelUp();
        yield return Step7_Lightning();
        yield return Step8_Harpoon();
        yield return Step9_Lever();
        yield return Step10_Defense();
        yield return Step11_Settlement();
        yield return Step12_Gambler();

        yield return Finish();
    }

    /// <summary>시작 상태: 슬롯 0 = 더블 육포 프리셋(선대가 걸어둔 포탑), 보급 요리 1접시 (2단계 투입용), 재료 0</summary>
    private void SetupKit()
    {
        if (TurretSlotManager.Instance != null)
        {
            TurretSlot first = TurretSlotManager.Instance.slots[0];
            if (first != null && first.IsEmpty && !first.isLocked) first.TryInsertFood(STARTER_RECIPE);
        }
        if (FoodStock.Instance != null && FoodStock.Instance.Get(STARTER_RECIPE) < 1)
            FoodStock.Instance.Add(STARTER_RECIPE, 1);
        UIManager.Instance?.ShowStatChange("[견습 운행] 보급 요리 도착 - 더블 육포 1접시");
    }

    // ── 1. 이동: 통로를 건너 포탑 칸 A 로 (셰프는 주방 칸 한가운데서 시작한다 - 2단계 투입 슬롯이 바로 그 칸에 있다) ──
    private IEnumerator Step1_Move()
    {
        BeginStep(1, "[WASD] 통로를 건너 포탑 칸으로 달려라", "[Shift] 대시\n칸 사이는 발판으로만 건넌다", null, 0, 0);
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
        BeginStep(2, "보급 요리를 포탑 이름표에 투입하라", "하단 바 요리 카드 클릭\n→ 화살표 아래 이름표 클릭", "투입", 0, 1);
        if (FoodStock.Instance != null && FoodStock.Instance.Get(STARTER_RECIPE) < 1) FoodStock.Instance.Add(STARTER_RECIPE, 1);
        if (slot != null) ShowMarkerFollow(slot.transform, SlotArrowTip(1), RING_TURRET);
        yield return Brief(BriefingTexts.Tutorial(2));

        int filledAtStart = FilledSlotCount();
        float insertMark2 = Time.time;                       // 브리핑이 닫힌 뒤의 투입만 인정 (프리셋 투입은 그 전)
        float hintAt = Time.time + 15f;
        while (!skipRequested)
        {
            // 빈 칸에 넣어 포탑이 늘었거나, 이미 있는 포탑에 넣어 레벨업했거나 - 어느 쪽이든 "투입"을 해냈다
            if (FilledSlotCount() > filledAtStart || TurretSlot.LastInsertTime > insertMark2) break;
            if (Time.time > hintAt) { hintAt = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] 요리 카드를 먼저 클릭하고, 포탑 위 [+] 이름표를 클릭 - 마우스"); }
            yield return null;
        }
        SetProgress(1, 1);
        yield return EndStep();
    }

    // ── 3. 첫 손님 (랩터 2, 약체, 기차 무적) - 기차 꼬리 오른쪽 화면 안에서 걸어온다 (보이는 자리에서 포탑이 잡게) ──
    private IEnumerator Step3_FirstGuests()
    {
        BeginStep(3, "첫 손님이다 - 포탑이 알아서 쏜다", "쓰러질 때까지 지켜봐라\n기차는 다치지 않는다", "손님", 0, 2);
        HideMarker();
        yield return Brief(BriefingTexts.Tutorial(3));

        // 기차 오른쪽 끝(꼬리) 바깥 3.5u, 화면 안 (카메라 반폭 15.1u) - 기차 피벗은 0 이라 거리 = 꼬리 x + 3.5
        float spawnDist = GameBalance.CarEdgesX[GameBalance.CarEdgesX.Length - 1] + 3.5f;
        if (TrainManager.Instance != null) spawnDist -= TrainManager.Instance.transform.position.x;
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

    // ── 4. 재료 (자동 흡수 확인): 조각이 도착해 고기 2개가 찬 뒤에 설명 카드 (카드 문구가 "찼다"로 시작한다) ──
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
        BeginStep(5, "[E] 그릴 조리대 - 더블 육포를 구워라", "그릴 곁에서 [E] → 더블 육포\n판정 칸 안에서 [Space]", "조리", 0, 1);
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
        BeginStep(6, "만든 요리를 첫 포탑에 투입하라", "같은 요리 = 레벨업\n다른 포탑에 넣어도 좋다", "투입", 0, 1);
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
        TurretSlot target = SlotAt(1);
        if (target == null || target.IsEmpty || target.isLocked)
        {
            target = null;
            for (int i = 0; i < 8 && target == null; i++)
            {
                TurretSlot s = SlotAt(i);
                if (s != null && !s.IsEmpty && !s.isLocked) target = s;
            }
        }
        BeginStep(7, "낙뢰! 멈춘 포탑에 달려가 [E]", "곁에 서서 [E] 한 번\n스파크가 꺼지면 재가동", "포탑", 0, 1);
        HideMarker();

        // 낙뢰 연출: 흰 번쩍 + 흔들림 + 감전 (브리핑은 그 뒤 - "포탑이 멈췄다" 는 과거형)
        WarningFX.Flash("낙뢰!", 0.6f, new Color(1f, 0.96f, 0.7f));
        GameFeel.Shake(0.2f);
        SoundManager.Play("sfx_boss_warning");   // 클립 없으면 무시
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

    // ── 8. 작살: 기관실 작살포 [E] - 디렉터가 바위를 띄운다 (자동 스폰은 8단계부터 EngineCabUnlocked 로 함께 켜진다) ──
    private IEnumerator Step8_Harpoon()
    {
        EngineCabUnlocked = true;
        BeginStep(8, "작살포로 바위를 낚아라", "기관실 작살포 곁에서 [E]\n놓치면 바위는 또 온다", "바위", 0, 1);
        ShowMarkerAt(new Vector3(GameBalance.HarpoonX, 1.7f, 0f), 1.0f, RING_HARPOON);
        yield return Brief(BriefingTexts.Tutorial(8));

        int mark = EngineCab.HarpoonRetrievals;
        float nextRockAt = 0f;
        float giveUpAt = Time.time + 40f;
        float hintAt = Time.time + 15f;
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
            if (Time.time > giveUpAt) { UIManager.Instance?.ShowStatChange("[견습] 작살은 정식 런에서 다시 - 넘어간다"); break; }
            yield return null;
        }
        SetProgress(1, 1);
        yield return EndStep();
    }

    // ── 9. 레버: [E] 잠깐 꾹 = 전속 주행 (다시 당기면 순항 - 유저 선택) ──
    private IEnumerator Step9_Lever()
    {
        BeginStep(9, "레버를 당겨라 - 전속 주행", "기관실 레버 곁에서 [E] 잠깐 꾹\n다시 당기면 원래대로", "레버", 0, 1);
        ShowMarkerAt(new Vector3(GameBalance.LeverX, 0.35f, 0f), 1.0f, RING_LEVER);
        yield return Brief(BriefingTexts.Tutorial(9));

        int mark = EngineCab.LeverPulls;
        float giveUpAt = Time.time + 30f;
        float hintAt = Time.time + 12f;
        while (!skipRequested)
        {
            if (EngineCab.LeverPulls > mark) break;
            if (Time.time > hintAt) { hintAt = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] 레버 곁에서 [E] 를 " + GameBalance.LeverHoldSec + "초 꾹 - 손을 떼면 취소"); }
            if (Time.time > giveUpAt) { UIManager.Instance?.ShowStatChange("[견습] 레버는 정식 런에서 다시 - 넘어간다"); break; }
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
        yield return Brief(BriefingTexts.TutorialPtera());   // 정식 런 첫 등장 카드와 같은 형식 (기록은 안 남긴다)

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
                if (retries10 >= 4) { UIManager.Instance?.ShowStatChange("[견습] 손님들이 물러갔다 - 정식 런에서 다시 해보자"); break; }
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

    /// <summary>실전 구성 스폰: 랩터는 기차 꼬리 오른쪽 화면 안에서, 프테라는 조금 더 멀리서. need = 실제로 나온 수</summary>
    private static List<Enemy> SpawnDefenseWave(int raptors, int pteras, float statMul, out int need)
    {
        List<Enemy> all = new List<Enemy>();
        need = 0;
        if (WaveManager.Instance == null) return all;
        float spawnDist = GameBalance.CarEdgesX[GameBalance.CarEdgesX.Length - 1] + 3.5f;
        if (TrainManager.Instance != null) spawnDist -= TrainManager.Instance.transform.position.x;
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
                if (linesText != null) { linesText.text = "[G] 정비소 - 지금 눌러봐라\n(정식 런에선 정차역마다)"; linesText.color = new Color(1f, 0.9f, 0.3f, 1f); }
                UIManager.Instance?.ShowStatChange("[견습] [G] 를 눌러 정비소를 열어봐라 - 수리·연마·재료 시장");
            }
            yield return null;
        }
        if (linesText != null) linesText.color = UIFactory.CREAM;
        SetProgress(2, 2);
        while (!skipRequested && WorkshopUI.IsOpen) yield return null;   // 구경 끝날 때까지 (닫으면 완료)
        yield return EndStep();
    }

    // ── 12. 도박꾼 / 앞길: 실습 없이 카드 2장 ──
    private IEnumerator Step12_Gambler()
    {
        BeginStep(12, "도박꾼과 앞길", "카드를 읽어라\n[Enter] 다음 장", null, 0, 0);
        HideMarker();
        yield return Brief(BriefingTexts.Tutorial(12));
        yield return Brief(BriefingTexts.Tutorial(13));
        yield return EndStep();
    }

    private IEnumerator WaitUnscaled(float sec)
    {
        float until = Time.unscaledTime + sec;
        while (Time.unscaledTime < until && Active) yield return null;
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

    // ── 완료 ──
    private IEnumerator Finish()
    {
        HideMarker();
        EngineCabUnlocked = false;
        bool first = !Done;
        float seconds = Time.unscaledTime - runStartTime;
        int cookFails = CookingBridge.BadsThisRun - badsAtStart;

        PlayerPrefs.SetInt(DONE_KEY, 1);
        if (GameBalance.TutorialSkipsPrologue) PlayerPrefs.SetInt("WDT_PrologueSeen", 1);
        PlayerPrefs.Save();
        if (first && GameBalance.TutorialReward > 0) MetaProgress.AddFame(GameBalance.TutorialReward);

        string perStep = "";
        for (int i = 1; i <= TOTAL_STEPS; i++) perStep += (i > 1 ? " " : "") + i + ":" + Mathf.RoundToInt(stepSeconds[i]);
        Debug.Log("[Tutorial] 완료 " + Mathf.FloorToInt(seconds / 60f) + "분" + Mathf.FloorToInt(seconds % 60f) + "초 | 스킵 " + skips
            + " | 조리 실패 " + cookFails + " | 실전 재시작 " + retries10 + " | 단계 " + IMPLEMENTED_STEPS + "/" + TOTAL_STEPS
            + (first ? " | 명성 +" + GameBalance.TutorialReward : " | 재플레이") + " | 단계별 초 " + perStep);

        if (cardRoot != null) cardRoot.SetActive(false);
        bool closed = false;
        BriefingUI.BriefDef done = BriefingTexts.TutorialDone(first, GameBalance.TutorialReward, seconds, skips, cookFails);
        done.onClose = delegate { closed = true; };
        BriefingUI.Show(done);
        while (!closed) yield return null;

        Teardown();
        if (GameManager.Instance != null) GameManager.Instance.EndTutorial();
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
        ShowCard(n, title, lines, progLabel, done, total);
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
            skips++;
            Debug.Log("[Tutorial] 단계 " + step + " 건너뜀");
            skipRequested = false;
            yield break;
        }
        SoundManager.Play("sfx_wave_clear");
        if (titleText != null) { titleText.text = "완료 - " + titleText.text; titleText.color = new Color(0.55f, 0.95f, 0.55f, 1f); }
        if (linesText != null) linesText.text = "잘했다. 다음 -";
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
        while (!closed && Active) yield return null;
        skipRequested = false;    // 카드를 닫은 Enter 가 건너뛰기로 새지 않게 (같은 프레임 가드에 더해 한 번 더)
    }

    private void Spino(string line)
    {
        UIManager.Instance?.ShowWaveNotice("", line);    // 예고 카드는 비우고 안내 카드(초록 줄)만
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
    }

    // ─────────────────────────────────────────────
    // 목표 카드 (우상단 웨이브 판 아래 - 목업 v2 (A) 좌표)
    // ─────────────────────────────────────────────
    private const float CARD_W = 330f;
    private const float CARD_H = 156f;

    private void BuildCard()
    {
        cardCanvas = UIFactory.CreateCanvas("TutorialCard_Canvas", CARD_SORT);
        cardCanvas.transform.SetParent(transform, false);      // 디렉터(DontDestroyOnLoad) 밑 - 씬 리로드에도 남는다

        RectTransform card = UIFactory.CreatePanel(cardCanvas.transform, "GoalCard",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-8f - CARD_W, -128f - CARD_H), new Vector2(-8f, -128f),
            UIFactory.PANEL, UIFactory.GOLD, 2f);
        cardRoot = card.gameObject;

        if (UISkin.Available)
            stepPlate = UISkin.Nameplate(card, "Step", "목표  1 / " + TOTAL_STEPS, 16, new Vector2(0f, 1f), new Vector2(26f, 4f));
        else
        {
            stepFallback = UIFactory.CreateText(card, "Step", "목표  1 / " + TOTAL_STEPS, 15, UIFactory.GOLD, TextAnchor.MiddleLeft);
            PlaceTopLeft(stepFallback.rectTransform, 26f, -6f, 200f, 22f);
        }

        titleText = UIFactory.CreateText(card, "Title", "", 18, UIFactory.GOLD, TextAnchor.MiddleLeft);
        PlaceTopLeft(titleText.rectTransform, 26f, -24f, CARD_W - 52f, 26f);

        linesText = UIFactory.CreateText(card, "Lines", "", 15, UIFactory.CREAM, TextAnchor.UpperLeft);
        PlaceTopLeft(linesText.rectTransform, 26f, -56f, CARD_W - 52f, 48f);
        linesText.lineSpacing = 1.15f;

        pipSprite = SpriteBank.Get("ui_mg_meat");
        for (int i = 0; i < 8; i++)
        {
            GameObject pg = new GameObject("Pip_" + i);
            pg.transform.SetParent(card, false);
            RectTransform prt = pg.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 1f); prt.anchorMax = new Vector2(0f, 1f); prt.pivot = new Vector2(0f, 1f);
            prt.anchoredPosition = new Vector2(26f + i * 22f, -102f);
            prt.sizeDelta = new Vector2(16f, 16f);
            Image pi = pg.AddComponent<Image>();
            if (pipSprite != null) pi.sprite = pipSprite;
            pi.color = PIP_OFF; pi.raycastTarget = false;
            pg.SetActive(false);
            pips.Add(pi);
        }

        progressText = UIFactory.CreateText(card, "Progress", "", 15, UIFactory.CREAM, TextAnchor.MiddleLeft);
        PlaceTopLeft(progressText.rectTransform, 26f, -100f, 240f, 20f);

        Text footer = UIFactory.CreateText(card, "Footer", "[Enter] 건너뛰기", 12, UIFactory.DIM, TextAnchor.LowerRight);
        footer.rectTransform.anchorMin = new Vector2(1f, 0f); footer.rectTransform.anchorMax = new Vector2(1f, 0f);
        footer.rectTransform.pivot = new Vector2(1f, 0f);
        footer.rectTransform.anchoredPosition = new Vector2(-16f, 8f); footer.rectTransform.sizeDelta = new Vector2(200f, 16f);

        cardRoot.SetActive(false);
    }

    private int progTotal = 0;
    private string progLabel = "";

    private void ShowCard(int n, string title, string lines, string label, int done, int total)
    {
        if (cardRoot == null) return;
        cardRoot.SetActive(true);
        if (stepPlate != null) UISkin.Relabel(stepPlate, "목표  " + n + " / " + TOTAL_STEPS, 16);
        if (stepFallback != null) stepFallback.text = "목표  " + n + " / " + TOTAL_STEPS;
        titleText.text = title;
        linesText.text = lines;
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
            progressText.rectTransform.anchoredPosition = new Vector2(26f + progTotal * 22f + 8f, -100f);
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
