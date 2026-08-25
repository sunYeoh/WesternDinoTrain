using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [GameManager.cs] v4
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
    private bool starterKitGiven = false;

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
        Debug.Log("[GameManager] 로비 진입 - 게임 시작 버튼 대기");
    }

    // ─────────────────────────────────────────────
    // 전투 페이즈 — Cooking 없이 바로 시작
    // 조리는 전투 중 ChefController가 항상 활성화
    // ─────────────────────────────────────────────
    private void HandleBattlePhase()
    {
        currentWave++;

        // 첫 웨이브 시작 시 보급품 지급 (포탑 없음 -> 파밍 불가 데드락 방지)
        if (!starterKitGiven)
        {
            starterKitGiven = true;
            GiveStarterKit();

            // v4: 새 런 시작을 메타 기록에 등록 (런 카운트 +1)
            MetaProgress.BeginRun();

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
        if (MaterialInventory.Instance != null)
        {
            for (int m = 0; m < 2; m++)
                MaterialInventory.Instance.Add((MaterialType)Random.Range(0, 6), 1);
            summary += " (+찬장 재료 2)";
        }

        // v4.1: 명성 상점 '재료 가방' - 시작 시 랜덤 재료 추가 지급
        if (MetaProgress.StartMaterialBonus > 0 && MaterialInventory.Instance != null)
        {
            for (int m = 0; m < MetaProgress.StartMaterialBonus; m++)
                MaterialInventory.Instance.Add((MaterialType)Random.Range(0, 6), 1);
            summary += " (+재료 " + MetaProgress.StartMaterialBonus + ")";
        }

        Debug.Log("[GameManager] 시작 보급품 지급: " + summary);
        UIManager.Instance?.ShowStatChange("보급품 도착! " + summary + " - 슬롯에 투입해 포탑을 세워라!");
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
    private void HandleGameOver()
    {
        chefController?.EnableCooking(false);

        // v4: 런 종료 요약 표시 (명성은 웨이브 클리어마다 이미 저장돼 있음)
        UIManager.Instance?.ShowWaveNotice("기차가 멈췄다...", MetaProgress.RunSummary());

        // v4.2: 스피노의 사망 대사 (첫 사망은 고정, 이후 랜덤)
        StoryTexts.ShowDeathQuote();
        Debug.Log("[GameManager] 게임 오버! " + MetaProgress.RunSummary());
    }

    private void HandleVictory()
    {
        chefController?.EnableCooking(false);

        // v4.3: 승리의 기적 소리 (일지 7 - "배가 불러서 우는 소리")
        SoundManager.Play("sfx_train_whistle");

        // v4: 승리 보너스 명성 + 런 종료 요약 표시
        MetaProgress.AddFame(300);
        UIManager.Instance?.ShowWaveNotice("종착역 도착!", MetaProgress.RunSummary());

        // v5 (C-2): 엔딩 B 직후라면 스피노 침묵 문구 생략 (엔딩 연출이 이미 마무리 대사 포함)
        if (StoryTexts.TrueEndingJustPlayed)
            StoryTexts.TrueEndingJustPlayed = false;
        else
            StoryTexts.ShowVictoryQuote();

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
        Debug.Log("[GameManager] 웨이브 " + currentWave + " 클리어!");

        // v4: 메타 기록 적립 (명성 +10+웨이브, 최고 기록 갱신, 즉시 저장)
        MetaProgress.OnWaveCleared(currentWave);

        // 조리 방식 해금 체크
        ChefController chef = FindFirstObjectByType<ChefController>();
        chef?.CheckUnlocks(currentWave);

        // v4.1: 최종전 클리어 -> 승리!
        if (currentWave >= GameBalance.FinalWave)
        {
            ChangeState(GameState.Victory);
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

        ChangeState(GameState.GameOver);
    }
}
