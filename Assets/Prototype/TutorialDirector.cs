using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [TutorialDirector.cs] v1 (신규, v9.9 2026-09-16) - "견습 운행": 로비 [T]로 들어가는 전용 튜토리얼 런
///
/// 계획: claude/튜토리얼_완성계획_2026-09-15.md v2. 교수 요구 "제대로 된 튜토리얼" (마감 9/28 주).
/// 1차 팩(v9.9) = 골격 + 단계 1~6 (이동 / 보급 투입 / 첫 손님 / 재료 / 굽기 / 레벨업). 7~12 는 2차 팩.
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

    /// <summary>true 면 방해 이벤트·자원 바위·작살·레버가 막힌다 (WaveManager.TutorialGateActive 가 읽는다). 작살/레버 단계에서 잠깐 false</summary>
    public static bool BlockAmbient = true;

    public const string DONE_KEY = "WDT_TutorialDone";
    public const int TOTAL_STEPS = 12;
    private const int IMPLEMENTED_STEPS = 6;     // 1차 팩: 1~6 (2차 팩에서 12 로)

    private const int CARD_SORT = 690;           // 조리 미니게임(30)·증강(600)·배너(610) 위, 일시정지(700) 아래
    private const int RING_ORDER = -3;           // 갑판(-6~-4) 위. 포탑 받침(-3)과 같은 값이라 z 를 0.05 뒤로 둬서 받침 밑에 깔린다
    private const int ARROW_ORDER = 7;           // 셰프(6) 위
    private const string STARTER_RECIPE = "meat+meat";   // 더블 육포 (GameBalance.StarterFoods[0] 와 같은 것)

    // ── 진행 상태 ──
    private int step = 0;
    private float runStartTime;                  // unscaled
    private float stepStartTime;                 // scaled (조리/투입 시각 비교용)
    private int skips = 0;
    private int badsAtStart = 0;
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
    private Transform followTarget;
    private Vector3 fixedTarget;
    private float arrowTipOffset = 1.0f;
    private bool markerOn = false;
    private Sprite arrowSprite, ringSprite;

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
        godMode = false;
        markerRoot = null; ringTf = null; arrowTf = null; markerOn = false;   // 씬 오브젝트였으므로 이미 사라졌다
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
        badsAtStart = CookingBridge.BadsThisRun;
        godMode = true;
        trainStoppedFlag = false;

        SetupKit();

        yield return Step1_Move();
        yield return Step2_Insert();
        yield return Step3_FirstGuests();
        yield return Step4_Materials();
        yield return Step5_Grill();
        yield return Step6_LevelUp();

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

    // ── 1. 이동 ──
    private IEnumerator Step1_Move()
    {
        BeginStep(1, "[WASD] 주방 칸으로 달려라", "[Shift] 대시\n화살표 자리까지", null, 0, 0);
        Vector3 target = new Vector3(0f, 0.6f, 0f);          // 주방 칸 가운데 (조리대 위쪽 바닥)
        ShowMarkerAt(target, 0.9f);
        yield return Brief(BriefingTexts.Tutorial(1));

        float blinkAt = Time.time + 20f;
        while (!skipRequested)
        {
            Transform chef = Chef();
            if (chef != null && Vector2.Distance(chef.position, target) <= 1.0f) break;
            if (Time.time > blinkAt) { blinkAt = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] 주방 칸은 기관실 오른쪽 - 통로 발판으로 건너라"); }
            yield return null;
        }
        EndStep();
    }

    // ── 2. 보급 요리 투입 ──
    private IEnumerator Step2_Insert()
    {
        TurretSlot slot = SlotAt(1);
        BeginStep(2, "보급 요리를 포탑 이름표에 투입하라", "하단 바 요리 카드 클릭\n→ 화살표 아래 이름표 클릭", "투입", 0, 1);
        if (FoodStock.Instance != null && FoodStock.Instance.Get(STARTER_RECIPE) < 1) FoodStock.Instance.Add(STARTER_RECIPE, 1);
        if (slot != null) ShowMarkerFollow(slot.transform, GameBalance.IsSouthSlot(1) ? 0.95f : GameBalance.SlotMarkerYOffset + 0.55f, true);
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
        EndStep();
    }

    // ── 3. 첫 손님 (랩터 2, 약체, 기차 무적) ──
    private IEnumerator Step3_FirstGuests()
    {
        BeginStep(3, "첫 손님이다 - 포탑이 알아서 쏜다", "쓰러질 때까지 지켜봐라\n기차는 다치지 않는다", "손님", 0, 2);
        HideMarker();
        yield return Brief(BriefingTexts.Tutorial(3));

        List<Enemy> guests = WaveManager.Instance != null ? WaveManager.Instance.SpawnForTutorial("raptor", 2, 0.7f) : null;
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
        EndStep();
    }

    // ── 4. 재료 (자동 흡수 확인) ──
    private IEnumerator Step4_Materials()
    {
        BeginStep(4, "재료가 날아온다 - 하단 바를 봐라", "쓰러진 손님의 재료가\n기차로 빨려 온다", "고기", MeatCount(), 2);
        yield return Brief(BriefingTexts.Tutorial(4));

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
        EndStep();
    }

    // ── 5. 굽기 (진짜 미니게임) ──
    private IEnumerator Step5_Grill()
    {
        CookingStation grill = FindStation(CookingStation.StationType.Grilling);
        BeginStep(5, "[E] 그릴 조리대 - 더블 육포를 구워라", "그릴 곁에서 [E] → 더블 육포\n판정 칸 안에서 [Space]", "조리", 0, 1);
        if (grill != null) ShowMarkerFollow(grill.transform, 1.0f, true);
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
        EndStep();
    }

    // ── 6. 투입 = 레벨업 ──
    private IEnumerator Step6_LevelUp()
    {
        TurretSlot first = SlotAt(0);
        BeginStep(6, "만든 요리를 첫 포탑에 투입하라", "같은 요리 = 레벨업\n다른 포탑에 넣어도 좋다", "투입", 0, 1);
        if (FoodStock.Instance != null && TotalFood() < 1) FoodStock.Instance.Add(STARTER_RECIPE, 1);
        if (first != null) ShowMarkerFollow(first.transform, GameBalance.IsSouthSlot(0) ? 0.95f : GameBalance.SlotMarkerYOffset + 0.55f, true);
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
        EndStep();
    }

    // ── 완료 ──
    private IEnumerator Finish()
    {
        HideMarker();
        bool first = !Done;
        float seconds = Time.unscaledTime - runStartTime;
        int cookFails = CookingBridge.BadsThisRun - badsAtStart;

        PlayerPrefs.SetInt(DONE_KEY, 1);
        if (GameBalance.TutorialSkipsPrologue) PlayerPrefs.SetInt("WDT_PrologueSeen", 1);
        PlayerPrefs.Save();
        if (first && GameBalance.TutorialReward > 0) MetaProgress.AddFame(GameBalance.TutorialReward);

        Debug.Log("[Tutorial] 완료 " + Mathf.FloorToInt(seconds / 60f) + "분" + Mathf.FloorToInt(seconds % 60f) + "초 | 스킵 " + skips
            + " | 조리 실패 " + cookFails + " | 단계 " + IMPLEMENTED_STEPS + "/" + TOTAL_STEPS + (first ? " | 명성 +" + GameBalance.TutorialReward : " | 재플레이"));

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
        skipRequested = false;
        ShowCard(n, title, lines, progLabel, done, total);
        Debug.Log("[Tutorial] 단계 " + n + " 시작: " + title);
    }

    private void EndStep()
    {
        if (skipRequested)
        {
            skips++;
            Debug.Log("[Tutorial] 단계 " + step + " 건너뜀");
            skipRequested = false;
        }
        HideMarker();
        SoundManager.Play("sfx_ui_click");
    }

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
    // 현장 마커 (월드 스프라이트 2개: 발밑 링 + 위 화살표) - 목업 v2 (B)
    // ─────────────────────────────────────────────
    private void EnsureMarker()
    {
        if (markerRoot != null) return;
        if (arrowSprite == null) arrowSprite = SpriteBank.Get("tut_arrow") ?? PaintFallbackArrow();
        if (ringSprite == null) ringSprite = SpriteBank.Get("tut_ring") ?? PaintFallbackRing();

        markerRoot = new GameObject("TutorialMarker");
        SpriteRenderer ring = PixelPainter.Attach(markerRoot.transform, "Ring", ringSprite, Vector3.zero, RING_ORDER);
        SpriteRenderer arrow = PixelPainter.Attach(markerRoot.transform, "Arrow", arrowSprite, Vector3.zero, ARROW_ORDER);
        ringTf = ring.transform; arrowTf = arrow.transform;
    }

    /// <summary>고정 위치 위에 마커</summary>
    private void ShowMarkerAt(Vector3 pos, float tipOffset)
    {
        EnsureMarker();
        followTarget = null; fixedTarget = pos; arrowTipOffset = tipOffset;
        markerOn = true;
        markerRoot.SetActive(true);
        if (ringTf != null) ringTf.gameObject.SetActive(true);
        TickMarker();
    }

    /// <summary>오브젝트(슬롯/조리대)를 따라다니는 마커. ring=false 면 링 없이 화살표만</summary>
    private void ShowMarkerFollow(Transform t, float tipOffset, bool ring)
    {
        EnsureMarker();
        followTarget = t; arrowTipOffset = tipOffset;
        markerOn = true;
        markerRoot.SetActive(true);
        if (ringTf != null) ringTf.gameObject.SetActive(ring);
        TickMarker();
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
        if (ringTf != null) ringTf.position = new Vector3(pos.x, pos.y, 0.05f);   // 같은 정렬값(포탑 받침 -3)에서는 뒤(z+)가 먼저 그려진다
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

    /// <summary>PNG 없을 때: 황동 타원 링 36x14 (중앙 피벗)</summary>
    private static Sprite PaintFallbackRing()
    {
        PixelPainter p = new PixelPainter(36, 14);
        Color32 clear = new Color32(0, 0, 0, 0), brass = new Color32(214, 170, 72, 255), blk = new Color32(16, 14, 20, 200);
        p.Ellipse(0, 0, 35, 13, clear, blk);
        p.Ellipse(1, 1, 34, 12, clear, brass);
        p.Ellipse(2, 2, 33, 11, clear, brass);
        p.Ellipse(3, 3, 32, 10, clear, blk);
        return p.Bake(32f, 18f, 7f);
    }
}
