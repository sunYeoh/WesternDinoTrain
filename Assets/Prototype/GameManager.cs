using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// [GameManager.cs] v4.7 (v9.17 2026-10-06 화면 손맛 2차 - D5 패배 순서: 슬로모션 + 붉은 가장자리 -> 어두워지며 세상이 멈춤 -> 결과 / D4 최종 승리 순서: 엔딩 글 뒤 흰 화면 -> 결과 / D6: 견습 종료 때 검정 페이드 / 승리 기적은 엔딩 쪽에서 울렸으면 생략) / v4.6 (v9.16 2026-09-29 손맛 2차 - 소리: 배경음을 로비부터 / 정차 골드 sfx_gold / 패배 = 기차 정지음 sfx_train_break -> 0.7초 뒤 sfx_game_over, 배경음 덕킹 / 승리 = 기적 -> 0.6초 뒤 sfx_victory) / v4.5 (v9.15 2026-09-29: 운행 시작에 파손 포탑 자리 전부 수리 - 파손은 이번 운행 한정) / v4.4 (v9.14 2026-09-28: 운행 시작에 증강·유물 강제 초기화 + 명성 상점 "출발 증강") / v4.3 (v9.9 2026-09-16: 견습 운행 StartTutorial/EndTutorial - 튜토리얼 런은 웨이브·보급·메타 기록 없이 Battle 상태만 빌린다) / v4.2 (2026-09-14: 포탑 과열 런 통계 초기화 - TurretSlot.ResetRunStats) / v4.1 (런 통계 초기화 / 프롤로그 찬장 고기 고정 / 전투 중 수리 기록) / v4
/// 게임 전체 상태를 관리하는 최상위 싱글톤 클래스.
/// Cooking 페이즈 제거 — 게임 시작하면 바로 Battle.
/// 조리는 전투 중 언제든 가능.
/// - v2 변경점 (로그라이크 연동):
///   1) '다음 웨이브' 버튼 제거 — 증강 선택 후 WaveManager가 자동 진행을 담당
///   2) 웨이브 골드 보상에 증강 배율 적용 (고리대금업자)
///   3) 기차 완파 시 '아홉 개의 목숨' 증강이 있으면 부활 처리
/// - v3 변경점 (밸런스):
///   4) 시작 골드를 GameBalance에서 적용 (Inspector 값 무시)
///   5) 웨이브 1 시작 시 시작 보급품 지급 (기본 요리 -> 첫 포탑 제작 가능)
/// - v4 변경점 (메타 진행 연동):
///   6) 런 시작/웨이브 클리어/게임오버/승리 시 MetaProgress에 기록
///      (명성 적립, 최고 웨이브 갱신 - 게임을 꺼도 유지되는 영구 저장)
/// VS 2017 (C# 7.3) 호환 버전입니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // 싱글톤
    // ─────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // 게임 상태 열거형 (Cooking 제거)
    // ─────────────────────────────────────────────
    public enum GameState
    {
        Lobby,    // 로비
        Battle,   // 전투 (조리는 전투 중 항상 가능)
        Town,     // 웨이브 종료 후 마을 정비
        GameOver, // 패배
        Victory   // 승리
    }

    // ─────────────────────────────────────────────
    // Inspector 설정
    // ─────────────────────────────────────────────
    [Header("─ 현재 게임 상태 ─")]
    public GameState currentState = GameState.Lobby;

    [Header("─ 외부 참조 매니저 ─")]
    public TrainManager trainManager;
    public WaveManager waveManager;
    public ChefController chefController;

    [Header("─ 게임 진행 데이터 ─")]
    public int currentWave = 0;
    public int playerGold = 500;
    public int playerXP = 0;
    public int playerLevel = 1;

    // 상태 변경 이벤트
    public UnityEvent<GameState> OnGameStateChanged = new UnityEvent<GameState>();

    // 시작 보급품 지급 여부 (런당 1회)
    // v4.1 (교수 피드백 C4): 전투 중 정비소 기차 수리 기록 - "돈으로 위기를 지우는가"를 재검증에서 잰다
    public int RepairsInBattle = 0;
    public int RepairGoldInBattle = 0;

    private bool starterKitGiven = false;

    // ── v4.7 (v9.17): 운행의 끝 ──
    /// <summary>기차 HP 0 뒤, 결과 화면이 뜨기 전 (패배 연출 중). WaveManager 가 이 동안 웨이브 클리어 판정을 멈춘다</summary>
    public bool DefeatPending { get; private set; }
    /// <summary>최종전을 깬 뒤, 승리 화면이 뜨기 전</summary>
    public bool VictoryPending { get; private set; }
    private bool runFrozen = false;   // 패배 연출을 거친 뒤: 세상이 멈춰 있다 (결과 화면 뒤에서 전투가 계속 돌지 않는다)

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 밸런스 설정이 Inspector 값을 덮어쓴다 (조정은 GameBalance.cs에서)
        // v4.1: 명성 상점 '두둑한 전대' 보너스 가산
        playerGold = GameBalance.StartGold + MetaProgress.StartGoldBonus;

        ChangeState(GameState.Lobby);
    }

    private void Update()
    {
        // v4.7 (D5): 패배 뒤에는 세상이 멈춰 있다. 일시정지·증강 목록 같은 창이 닫히며 시간을 1 로 돌려도 다시 멈춘다
        if (runFrozen && Time.timeScale != 0f) Time.timeScale = 0f;
    }

    /// <summary>v4.7: 멈춰 둔 세상을 푼다 - 재출발·포기로 씬을 다시 싣기 직전에 부른다 (안 부르면 같은 프레임의 Update 가 시간을 다시 0 으로 돌린다)</summary>
    public void ReleaseFreeze()
    {
        if (!runFrozen) return;
        runFrozen = false;
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        // v4.7: 어떤 길로 사라지든 멈춘 시간은 풀고 간다 (다음 씬이 timeScale 0 으로 시작하지 않게)
        if (runFrozen) { runFrozen = false; Time.timeScale = 1f; }
    }

    // ─────────────────────────────────────────────
    // 상태 전환
    // ─────────────────────────────────────────────
    public void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log("[GameManager] 상태 전환 → " + newState);
        OnGameStateChanged.Invoke(newState);

        if (newState == GameState.Lobby) HandleLobby();
        else if (newState == GameState.Battle) HandleBattlePhase();
        else if (newState == GameState.Town) HandleTownPhase();
        else if (newState == GameState.GameOver) HandleGameOver();
        else if (newState == GameState.Victory) HandleVictory();
    }

    // ─────────────────────────────────────────────
    // 로비
    // ─────────────────────────────────────────────
    private void HandleLobby()
    {
        SoundManager.PlayBGM("bgm_main");   // v4.6: 배경음은 로비부터 (같은 곡이 이미 돌고 있으면 그대로. 지난 운행의 덕킹은 여기서 풀린다)
        Debug.Log("[GameManager] 로비 진입 - 게임 시작 버튼 대기");
    }

    // ─────────────────────────────────────────────
    // 전투 페이즈 — Cooking 없이 바로 시작
    // 조리는 전투 중 ChefController가 항상 활성화
    // ─────────────────────────────────────────────
    private void HandleBattlePhase()
    {
        // v4.3: 견습 운행 - 웨이브/보급/메타 기록/오프닝은 TutorialDirector 가 맡는다. 여기서는 상태만 Battle 로 두고 조리를 켠다
        if (TutorialDirector.Active)
        {
            currentWave = 1;                         // 표시용 (디렉터가 "견습 운행" 으로 덮어쓴다)
            chefController?.EnableCooking(true);
            SoundManager.PlayBGM("bgm_main");
            Debug.Log("[GameManager] 견습 운행 - Battle 상태 (웨이브 없음)");
            return;
        }

        currentWave++;

        // 첫 웨이브 시작 시 보급품 지급 (포탑 없음 -> 파밍 불가 데드락 방지)
        if (!starterKitGiven)
        {
            starterKitGiven = true;
            CookingBridge.ResetRunStats();   // v4.1: 런 통계 초기화 (프롤로그 조리 게이트·관찰 시트)
            TurretSlot.ResetRunStats();      // v4.2: 과열 횟수·정지 시간 (static 이라 씬 리로드 뒤에도 남는다)
            RepairsInBattle = 0; RepairGoldInBattle = 0;
            if (TurretSlotManager.Instance != null) TurretSlotManager.Instance.RepairAllBroken();   // v4.5 (v9.15): 파손 슬롯은 이번 운행 한정

            // v4.4 (v9.14): 증강·유물은 운행 시작에 반드시 비운다 (테스터 "한 판 끝나고 다음 판에도 증강이 남는다" - AugmentPickUI.Awake 만 믿지 않는다)
            if (AugmentManager.Owned.Count > 0 || ItemManager.OwnedCount > 0)
                Debug.LogWarning("[GameManager] 운행 시작인데 증강 " + AugmentManager.Owned.Count + " / 유물 " + ItemManager.OwnedCount + " 이 남아 있었다 - 비운다");
            AugmentManager.ResetRun();
            ItemManager.ResetRun();

            GiveStarterKit();

            // v4: 새 런 시작을 메타 기록에 등록 (런 카운트 +1)
            MetaProgress.BeginRun();

            // v4.4 (v9.14): 명성 상점 "출발 증강" - 은 증강 1회 (오프닝 카드 뒤에 뜬다 - 시간 정지 창끼리 겹치지 않게 다음 프레임)
            if (MetaProgress.StartAugment && AugmentPickUI.Instance != null)
                StartCoroutine(OpenStartAugmentNextFrame());

            // v4.2: 오프닝 연출 (클릭/아무 키로 스킵)
            StoryTexts.ShowOpening();

            // v4.3: 배경음 시작 (클립 없으면 조용히 무시)
            SoundManager.PlayBGM("bgm_main");
        }

        // 셰프 조리 항상 활성화 (전투 중 언제든 가능)
        chefController?.EnableCooking(true);

        // 웨이브 시작
        waveManager?.StartWave(currentWave);

        Debug.Log("[GameManager] 웨이브 " + currentWave + " 전투 시작! (조리 동시 진행)");
    }

    /// <summary>시작 보급품: 기본 요리를 지급해 첫 포탑을 바로 만들 수 있게 한다</summary>
    private void GiveStarterKit()
    {
        if (FoodStock.Instance == null)
        {
            Debug.LogWarning("[GameManager] FoodStock 없음 - 시작 보급품 지급 실패");
            return;
        }

        string summary = "";
        for (int i = 0; i < GameBalance.StarterFoods.Length; i++)
        {
            GameBalance.StarterFood item = GameBalance.StarterFoods[i];
            RecipeData recipe = RecipeDatabase.Get(item.recipeId);
            if (recipe == null)
            {
                Debug.LogWarning("[GameManager] 시작 보급품 레시피 없음: " + item.recipeId);
                continue;
            }

            FoodStock.Instance.Add(item.recipeId, item.count);
            if (summary.Length > 0) summary += ", ";
            summary += recipe.displayName + " x" + item.count;
        }

        // v4.1: 명성 상점 '여분의 도시락' - 첫 번째 시작 요리를 추가 지급
        if (MetaProgress.StarterFoodBonus > 0 && GameBalance.StarterFoods.Length > 0)
        {
            string extraId = GameBalance.StarterFoods[0].recipeId;
            FoodStock.Instance.Add(extraId, MetaProgress.StarterFoodBonus);
            summary += " (+도시락 " + MetaProgress.StarterFoodBonus + ")";
        }

        // v5 (감사 3-D): 선대가 남긴 찬장 - 기본 랜덤 재료 2개 (첫 조리를 1분 안에)
        // v4.1 (교수 피드백 A11): 1회차(프롤로그 예정)면 고기 2개로 고정 - 조리 게이트에서 더블 육포를 반드시 구울 수 있게
        if (MaterialInventory.Instance != null)
        {
            bool prologuePending = GameBalance.PrologueEnabled && GameBalance.PrologueCookGate
                && PlayerPrefs.GetInt("WDT_PrologueSeen", 0) == 0;
            for (int m = 0; m < 2; m++)
                MaterialInventory.Instance.Add(prologuePending ? MaterialType.Meat : (MaterialType)Random.Range(0, 6), 1);
            summary += prologuePending ? " (+찬장 고기 2)" : " (+찬장 재료 2)";
        }

        // v4.1: 명성 상점 '재료 가방' - 시작 시 랜덤 재료 추가 지급
        if (MetaProgress.StartMaterialBonus > 0 && MaterialInventory.Instance != null)
        {
            for (int m = 0; m < MetaProgress.StartMaterialBonus; m++)
                MaterialInventory.Instance.Add((MaterialType)Random.Range(0, 6), 1);
            summary += " (+재료 " + MetaProgress.StartMaterialBonus + ")";
        }

        Debug.Log("[GameManager] 시작 보급품 지급: " + summary);

        // 픽스 2차 (플레이테스트: "시작하자마자 적응할 새 없이 바쁘다"):
        // 첫 포탑 1문은 선대가 미리 걸어두고 떠났다 - 유저는 달리는 기차부터 익힌다.
        // (재고 소모 없음 - 남은 보급 요리를 같은 포탑에 부으면 레벨업을 바로 배운다)
        bool preInstalled = false;
        if (TurretSlotManager.Instance != null && GameBalance.StarterFoods.Length > 0)
        {
            TurretSlot first = TurretSlotManager.Instance.slots[0];
            if (first != null && first.IsEmpty && !first.isLocked)
                preInstalled = first.TryInsertFood(GameBalance.StarterFoods[0].recipeId);
        }

        UIManager.Instance?.ShowStatChange(preInstalled
            ? "보급품 도착! " + summary + " - 포탑 1문은 선대가 걸어뒀다. 나머지는 셰프의 몫!"
            : "보급품 도착! " + summary + " - 슬롯에 투입해 포탑을 세워라!");
    }

    /// <summary>v4.4: 출발 증강 - 오프닝 연출·첫 카드가 닫힌 뒤 은 증강 선택창 (명성 상점 "출발 증강")</summary>
    private System.Collections.IEnumerator OpenStartAugmentNextFrame()
    {
        yield return null;
        while (StoryTexts.IsBlocking || BriefingUI.IsOpen || AugmentPickUI.IsOpen) yield return null;
        if (currentState != GameState.Battle) yield break;
        AugmentPickUI.Instance.OpenExtra(currentWave, AugmentGrade.Silver, "출발 증강", null);
    }

    // ─────────────────────────────────────────────
    // v4.3: 견습 운행 (전용 튜토리얼 런) 진입/종료 - TutorialDirector 가 부른다
    // ─────────────────────────────────────────────
    /// <summary>로비 -> 견습 운행. TutorialDirector.Active 가 먼저 true 여야 HandleBattlePhase 가 웨이브를 안 돌린다</summary>
    public void StartTutorial()
    {
        if (currentState != GameState.Lobby)
        {
            Debug.LogWarning("[GameManager] 견습 운행은 로비에서만 시작한다 (현재 " + currentState + ")");
            return;
        }
        if (UIManager.Instance != null) UIManager.Instance.OnClickStartGame();   // 로비 패널 정리 + Battle (LobbyUI.StartRun 과 같은 길)
        else ChangeState(GameState.Battle);
        SoundManager.Play("sfx_train_whistle");
        Debug.Log("[GameManager] 견습 운행 진입");
    }

    /// <summary>견습 운행 종료 -> 로비. PauseMenu.GiveUpRun 과 같은 방식(GameManager 파괴 후 씬 리로드)이 가장 깨끗하다</summary>
    public void EndTutorial()
    {
        Time.timeScale = 1f;
        Debug.Log("[GameManager] 견습 운행 종료 - 로비로 (씬 리로드)");
        // v4.7 (D6): 바로 검정으로 덮고 씬을 다시 실으면 로비가 밝아지며 나타난다.
        //   앞쪽 페이드는 넣지 않는다 - 그 0.3초 동안 견습이 이미 끝난 상태(TutorialDirector.Active = false)로 게임이 돌아, 정식 운행용 힌트가 "본 것"으로 기록된다
        ScreenFx.Reveal(Color.black, GameBalance.SceneFadeSec);
        Destroy(gameObject);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ─────────────────────────────────────────────
    // 마을 정비 (웨이브 사이 보상 지급)
    // ─────────────────────────────────────────────
    private void HandleTownPhase()
    {
        // 웨이브 사이 Town 상태에서도 조리는 계속 가능 (EnableCooking(false) 호출 없음)

        // v5 (감사 3-A): 골드 커브 완화 - 후반 인플레이션 억제. 증강 '고리대금업자' 배율 반영
        int goldReward = Mathf.RoundToInt(
            (GameBalance.TownGoldBase + currentWave * GameBalance.TownGoldPerWave)
            * AugmentManager.GoldRewardMul);
        AddGold(goldReward);
        SoundManager.Play("sfx_gold");   // v4.6: 정차 수입 - 동전 쏟아짐

        // 보스 웨이브 클리어 보너스 (별도 지급)
        // Phase 2-1: 도박 베팅 패배 시 스피노가 이 보너스를 몰수한다
        if (GameBalance.IsBossWave(currentWave))
        {
            if (SpinoBet.ConsumeForfeit())
            {
                UIManager.Instance?.ShowDanger("[스피노] 격파 보너스 " + GameBalance.BossClearGold
                    + "G는 내 몫이다 - 약속은 약속이지");
            }
            else
            {
                AddGold(GameBalance.BossClearGold);
                UIManager.Instance?.ShowStatChange("[보스 격파 보너스] 골드 +" + GameBalance.BossClearGold);
            }
        }

        Debug.Log("[GameManager] 마을 정비 - 골드 +" + goldReward);
    }

    // ─────────────────────────────────────────────
    // 게임오버 / 승리
    // ─────────────────────────────────────────────
    /// <summary>
    /// v4.7 (D5): 패배 순서 (전부 실시간). 마지막 피격 -> DefeatSlowSec 동안 슬로모션 + 붉은 가장자리 + 쇠 긁힘
    /// -> DefeatFadeSec 동안 화면이 어두워지며 세상이 멈춘다 -> 결과(명성 상점 머리글 "기차가 멈췄다") + 패배 스팅.
    /// 일시정지 창이 열려 있는 동안은 이 순서도 멈춘다
    /// </summary>
    private IEnumerator DefeatSequence()
    {
        DefeatPending = true;
        SoundManager.Play("sfx_train_break");
        SoundManager.BgmDuck("gameover", true);
        GameFeel.Shake(GameBalance.ShakeBoss * 0.8f);
        ScreenFx.Vignette(new Color(0.85f, 0.08f, 0.05f, 0.75f), 0.12f);

        float slowSec = Mathf.Max(0f, GameBalance.DefeatSlowSec);
        float fadeSec = Mathf.Max(0.05f, GameBalance.DefeatFadeSec);
        float slow = Mathf.Clamp(GameBalance.DefeatSlowScale, 0.05f, 1f);
        float t = 0f;
        bool curtainOn = false;
        while (t < slowSec + fadeSec)
        {
            if (PauseMenu.IsOpen) { yield return null; continue; }
            t += Time.unscaledDeltaTime;
            if (t < slowSec) Time.timeScale = slow;
            else
            {
                if (!curtainOn) { curtainOn = true; ScreenFx.Curtain(GameBalance.DefeatCurtainAlpha, fadeSec); }
                Time.timeScale = Mathf.Lerp(slow, 0f, Mathf.Clamp01((t - slowSec) / fadeSec));
            }
            yield return null;
        }

        ScreenFx.VignetteOff(0.3f);
        Time.timeScale = 0f;
        runFrozen = true;
        DefeatPending = false;
        ChangeState(GameState.GameOver);
    }

    private void HandleGameOver()
    {
        chefController?.EnableCooking(false);

        if (runFrozen)
        {
            // v4.7 (D5): 패배 연출을 거쳐 왔다 - 쇠 긁힘·배경음 낮추기는 이미 나갔다. 결과가 뜨는 순간 패배 스팅.
            // "기차가 멈췄다" 와 이번 운행 숫자는 명성 상점 머리글·줄이 보여 준다 (가운데 예고와 겹치던 것)
            SoundManager.Play("sfx_game_over");
            UIManager.Instance?.ClearWaveNotice();   // 시간이 멈춰 있어, 떠 있던 예고가 결과 화면에 그대로 남는다
        }
        else
        {
            // v4.6: 기차가 멈춘다 - 쇠 긁힘 -> 잠깐 뒤 패배 스팅. 배경음은 낮춘 채 (새 운행이 PlayBGM 으로 되돌린다)
            SoundManager.Play("sfx_train_break");
            SoundManager.PlayDelayed("sfx_game_over", 0.7f);
            SoundManager.BgmDuck("gameover", true);

            // v4: 런 종료 요약 표시 (명성은 웨이브 클리어마다 이미 저장돼 있음)
            UIManager.Instance?.ShowWaveNotice("기차가 멈췄다...", MetaProgress.RunSummary());
        }

        // v4.2: 스피노의 사망 대사 (첫 사망은 고정, 이후 랜덤). v4.7: 결과 화면이면 상점 패널 위 띠에
        StoryTexts.ShowDeathQuote(runFrozen);
        Debug.Log("[GameManager] 게임 오버! " + MetaProgress.RunSummary());
    }

    /// <summary>
    /// v4.7 (D4): 최종 승리 순서 (실시간). 식사 엔딩은 엔딩 글이 닫히면 바로, 격파 엔딩은 "철길이 열렸다" 를 읽을 VictoryHoldSec 뒤
    /// -> 흰 화면이 덮였다 걷히며(VictoryWhiteSec) 그 사이에 Victory 로 넘어간다
    /// </summary>
    private IEnumerator VictorySequence()
    {
        VictoryPending = true;
        while (StoryTexts.IsBlocking) yield return null;
        if (!StoryTexts.TrueEndingJustPlayed)
        {
            float w = 0f;
            while (w < GameBalance.VictoryHoldSec)
            {
                if (!PauseMenu.IsOpen) w += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        while (ScreenFx.Covering) yield return null;   // 다른 덮개(장면 전환)가 돌고 있으면 끝난 뒤
        float half = Mathf.Max(0.05f, GameBalance.VictoryWhiteSec * 0.5f);
        ScreenFx.Cover(new Color(1f, 0.97f, 0.9f, 0.95f), half, half, delegate
        {
            VictoryPending = false;
            ChangeState(GameState.Victory);
        });
    }

    private void HandleVictory()
    {
        chefController?.EnableCooking(false);

        // v4.3: 승리의 기적 소리 (일지 7 - "배가 불러서 우는 소리"). v4.6: 기적 뒤에 승리 팡파르
        // v4.7: 최종 보스 쪽에서 이미 기적을 울렸으면(격파 = 한 번, 식사 = 두 번) 여기서 또 울리지 않는다 - 두 엔딩의 기적 횟수가 달라야 한다
        if (!BossEnemy.EndingWhistled) SoundManager.Play("sfx_train_whistle");
        SoundManager.PlayDelayed("sfx_victory", BossEnemy.EndingWhistled ? 0.1f : 0.6f);
        BossEnemy.EndingWhistled = false;

        // v4: 승리 보너스 명성 + 런 종료 요약 표시
        MetaProgress.AddFame(300);
        // v4.7 (D4): 결과 막을 깔고, "종착역 도착!" 과 이번 운행 숫자는 명성 상점 머리글·줄이 보여 준다. 연출이 꺼져 있으면 예전처럼 가운데 예고
        bool staged = GameBalance.VictorySequenceOn && GameBalance.GameFeelMaster > 0f;
        if (staged) ScreenFx.Curtain(0.6f, 0.2f);
        else UIManager.Instance?.ShowWaveNotice("종착역 도착!", MetaProgress.RunSummary());

        // v5 (C-2): 엔딩 B 직후라면 스피노 침묵 문구 생략 (엔딩 연출이 이미 마무리 대사 포함)
        if (StoryTexts.TrueEndingJustPlayed)
            StoryTexts.TrueEndingJustPlayed = false;
        else
            StoryTexts.ShowVictoryQuote(staged);

        Debug.Log("[GameManager] 승리! " + MetaProgress.RunSummary());
    }

    // ─────────────────────────────────────────────
    // 자원 관리
    // ─────────────────────────────────────────────
    public void AddGold(int amount) { playerGold += amount; }

    public bool SpendGold(int amount)
    {
        if (playerGold < amount) { Debug.Log("[GameManager] 골드 부족!"); return false; }
        playerGold -= amount;
        return true;
    }

    /// <summary>
    /// [절단됨 - 감사 3-B] XP/레벨 시스템 제거.
    /// 성장은 증강/포탑 합체가 전담한다. 구 스크립트(DevCheat 등) 호환용 빈 함수.
    /// playerXP/playerLevel 필드는 Inspector 호환을 위해 남아 있지만 아무 데도 안 쓰인다.
    /// </summary>
    public void AddXP(int amount) { }

    // ─────────────────────────────────────────────
    // 외부 콜백
    // ─────────────────────────────────────────────
    public void OnWaveCleared()
    {
        if (DefeatPending || runFrozen) return;   // v4.7: 멈춘 기차는 웨이브를 깨지 못한다
        Debug.Log("[GameManager] 웨이브 " + currentWave + " 클리어!");

        // v4: 메타 기록 적립 (명성 +10+웨이브, 최고 기록 갱신, 즉시 저장)
        MetaProgress.OnWaveCleared(currentWave);

        // 조리 방식 해금 체크
        ChefController chef = FindFirstObjectByType<ChefController>();
        chef?.CheckUnlocks(currentWave);

        // v4.1: 최종전 클리어 -> 승리!
        if (currentWave >= GameBalance.FinalWave)
        {
            // v4.7 (D4): 승리 순서를 거쳐서 (연출 끔이면 바로)
            if (GameBalance.VictorySequenceOn && GameBalance.GameFeelMaster > 0f) { if (!VictoryPending) StartCoroutine(VictorySequence()); }
            else ChangeState(GameState.Victory);
            return;
        }

        // Town 상태로 전환 (보상 지급). 다음 웨이브는 WaveManager가 자동 진행한다.
        // v2: '다음 웨이브' 버튼 표시 제거 - 증강 선택 -> 정비 시간 -> 자동 시작 흐름으로 대체
        ChangeState(GameState.Town);
    }

    /// <summary>
    /// 다음 웨이브 시작 (WaveManager 자동 진행이 호출).
    /// 구 UI 버튼이 남아 있어도 이 함수에 연결되어 있으면 그대로 동작한다.
    /// </summary>
    public void OnClickNextWave()
    {
        // 이미 전투 중이면 중복 시작 방지
        if (currentState == GameState.Battle)
        {
            Debug.Log("[GameManager] 이미 전투 중 - 웨이브 시작 요청 무시");
            return;
        }

        UIManager.Instance?.HideNextWaveButton();
        ChangeState(GameState.Battle);
    }

    public void OnTrainDestroyed()
    {
        // v4.7: 이미 이겼으면(승리 순서 중 포함) 기차 정지로 뒤집지 않는다
        if (VictoryPending || currentState == GameState.Victory) return;

        // v4.3: 견습 운행 중에는 게임오버가 없다 - 기차를 고쳐 놓고 디렉터에게 알린다 (실전 단계는 그 단계만 재시작)
        if (TutorialDirector.Active)
        {
            if (trainManager == null) trainManager = FindFirstObjectByType<TrainManager>();
            if (trainManager != null) trainManager.Heal(trainManager.currentMaxHP);
            if (TutorialDirector.Instance != null) TutorialDirector.Instance.OnTrainStopped();
            Debug.Log("[GameManager] 견습 운행 - 기차 정지 -> 수리 후 디렉터에게 통보");
            return;
        }

        // 증강 '아홉 개의 목숨': 부활 충전이 있으면 게임오버 대신 부활
        if (AugmentManager.ReviveCharges > 0)
        {
            AugmentManager.ReviveCharges--;
            if (trainManager == null)
                trainManager = FindFirstObjectByType<TrainManager>();
            if (trainManager != null)
            {
                trainManager.Heal(800f);
                Debug.Log("[GameManager] 아홉 개의 목숨 발동! 기차 부활 (남은 충전 "
                    + AugmentManager.ReviveCharges + "회)");
                UIManager.Instance?.ShowWaveNotice("아홉 개의 목숨!", "기차가 부활했다 (HP 800)");

                // v4.2: 부활 연출 문구
                StoryTexts.ShowReviveQuote();
                return;
            }
        }

        // v4.7 (D5): 패배 순서를 거쳐서 (연출 끔이면 바로 결과)
        if (GameBalance.DefeatSequenceOn && GameBalance.GameFeelMaster > 0f) { if (!DefeatPending) StartCoroutine(DefeatSequence()); }
        else ChangeState(GameState.GameOver);
    }
}
