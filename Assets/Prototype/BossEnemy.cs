using System.Collections;
using UnityEngine;

/// <summary>
/// [BossEnemy.cs] v7.9 (v9.19 2026-10-06 보스 페이즈 모습 - A1 상태 그림: _rage_groggy(발악 중 무방비)·_p2_groggy 추가, 디 오리지널은 페이즈 그림이 먼저(WantedSkin, GameBalance.BossStateSkins) / A2 전환 순간(PhaseShift): 히트스톱 -> 흰 번쩍 아래에서 그림 교체 -> 장갑 파편·링·폭음 -> 몸이 커졌다 제자리. 지역 보스 발악과 디 오리지널 P2·P3 / A3 상태 유지: 발악 = 김·불티, 무방비 = 불똥·연기 + 몸이 기울어 흔들린다(SkinPivot) / A4 알림 자리: 보스 쪽 알림은 HP 바 밑 띠로(BossNotice), 발악의 화면 가운데 큰 글자는 가장자리 맥동으로) /
/// v7.8 (v9.18 2026-10-06 - 서는 자세: 기차 옆에 이르면 나란히(머리가 기차 진행 방향) 돌아서서 선다 - 몸 전체가 지붕과 HP 바 사이에 보이게(GameBalance.BossFaceAlongTrain·BossStandOff·BossTurnZone, MoveTowardsTrain 재정의 + HoldStance) / 그림: boss_<종류>.png 를 입힌다(없으면 프리팹의 색 사각형 그대로), 상태 그림 _groggy·_rage·_p2·_p3 는 있으면 자동, 상태 발광 = 그림 위 흰 실루엣(예고 흰빛 / 무방비 금빛 / 발악 붉은 맥동) / 등장: 포효와 함께 경고 띠(WarningFX.BossIntro) + HP 바가 차오른다, 대응법 안내는 띠가 걷힌 뒤 / 발악 신호: 붉은 경고 + 흔들림 + 포효 / 버그: 공격 거리를 기차 "중심"에서 재서 옆에서 온 보스는 기차 위에 올라앉아 물지도 않았다 -> 가장 가까운 몸통에서 잰다, 돌진 방향도. 무는 양은 BossMeleeMul) / v7.7 (v9.17 2026-10-06 화면 손맛 2차 - D2 등장: 첫 등장 카드가 닫힌 뒤 배경이 0.5초 어두워졌다가 흔들림 + 포효 + 이름 예고가 같이 나온다 / A11 처치: 히트스톱 뒤 0.25초 슬로모션 + 줌 당김 / 식사 엔딩의 Die 는 히트스톱·흔들림·킬 버스트 없이 조용히 / EndingWhistled - 엔딩 쪽에서 기적을 울렸으면 승리 화면이 또 울리지 않는다) / v7.6 (v9.16 2026-09-29 손맛 2차 - 소리: 등장 포효 = 종류별(SoundKeys.BossRoar - 녹슨 발톱 무리 울음 / 천둥 둥지 번개 / 동면자 얼음 / 디 오리지널 기본 포효 + 낮은 기적 sfx_whistle_low, 예습 보스는 작게) / 엔딩 B 두 번째 기적 = 낮은 기적 / 공격음은 Enemy.AttackTrain 이 종류별로) / v7.5 (v9.15.1 2026-09-29 스토리 개정: 디 오리지널 = 급식 열차 1호였던 것 - 등장에 낡은 기적 + 안내 문구, 폭식 = 원료 삼키기, 해치 = 기관심장이 드러남 / 마지막 식사 장면 LastSupperRoutine - 포탑 정지(LastSupperServing)·남은 손님 물러남·천천히 씹기(LastSupperChewSec)·두 대의 기적 -> 엔딩 B 글 / 격파 엔딩 = 기적 한 번 + "철길이 열렸다") / v7.4 (v9.12 2026-09-22: practice = 견습 구간 7 "새끼 발톱" - 녹슨 발톱 고정, HP·공격력 배율(GameBalance.BossPractice*), 0.7배 크기, 패턴·무방비·발악 없음(돌진만), 처치해도 재료·베팅·"승리" 없음(ClearBossUI) / TutorialDirector.InlineFreeze 동안 정지) / v7.3 (v9.11.1 2026-09-22 문구: 무방비, 실행 가능한 예고) / v7.2 (v9.10.1 2026-09-21: 재료 이름 전기알) / v7.1 (교수 피드백 C3: 디 오리지널 추가 그로기 / A8: 재가동 문구) / v6 - 보스 패턴 C단계 1차 (보스패턴설계 문서)
/// - v6 변경점:
///   1) 미끼 도발 대응: 도발 중엔 미끼를 쫓아가고 물어뜯는다 (기차 무피해)
///   2) 디 오리지널 3페이즈:
///      P1 사냥(100~70%): 포효 소환 (기존)
///      P2 폭식(70~35%): 재료 조각 쟁탈전 - 보스가 조각을 먹으면 회복+공격력 스택
///         (회복 상한 = 최대 HP 15%, 공격력 상한 +50%)
///      P3 해치 개방(35%~): 받는 피해 +30%, 폭식 종료 (마지막 주문/엔딩 분기는 C-2에서)
/// ---------------------------------------------------------------
/// (v5) 보스 패턴 B단계
/// - v5 변경점:
///   1) 번개 병 패링 (천둥 둥지): 낙뢰 예고 마지막 0.6초에 Space -> 낙뢰 무효 + 병 1충전
///      3병 모으면 여왕에게 되쏘아 강제 그로기. 미사용 병은 처치 시 전기 재료로 환급
///      조리 미니게임 중이면 미니게임이 잠시 대기하고 Space가 패링으로 쓰인다
///   2) 해동포 연동 (동면자): ThawCannonUI가 호출하는 HitByThawCannon (갑주 파괴/약화)
///   3) 발악 페이즈: HP 50% 이하 -> 패턴 가속 + 소환/낙뢰 규모 증가
/// ---------------------------------------------------------------
/// (v4) 보스 패턴 A단계
/// 프리팹 1개를 그대로 쓰면서, 등장 지역에 따라 다른 보스가 된다.
///
/// - v4 변경점:
///   1) 보스 4종 개성화 (지역 번호로 자동 결정 - 씬/프리팹 작업 0):
///      지역 1 "녹슨 발톱"   (알파 랩터, 녹슨 적갈색, 빠름)
///      지역 2 "천둥 둥지"   (프테라 여왕, 뇌운 보라, 원거리)
///      지역 3 "동면자"      (고대 모사, 한랭 청록, 단단함)
///      최종   "디 오리지널" (메카 티렉스, 핏빛)
///   2) 패턴 시스템: 예고(텔레그래프) -> 실행 -> 파훼 판정
///      - 사냥 호령(지역1): 랩터 소환. 예고 중 보스에게 스턴 명중 시 소환 절반
///      - 낙뢰 폭격(지역2): 포탑 슬롯 감전 마비. 슬롯 곁에서 [E]로 재가동 (ProximityInteract)
///      - 빙하 갑주(지역3): 받는 피해 90% 감소. 화상 스택 누적으로 파괴(+보너스 그로기)
///        (화염 도트는 갑주를 무시하고 태운다 - Enemy.ModifyIncomingDamage 주석 참조)
///      - 포효(최종): 정예 증원 소환
///   3) 수치는 전부 GameBalance의 '보스 패턴' 섹션에서 조정
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class BossEnemy : Enemy
{
    // ─────────────────────────────────────────────
    // 보스 종류 (지역 기반 자동 결정)
    // ─────────────────────────────────────────────
    public enum BossKind
    {
        RustClaw,     // 지역 1: 녹슨 발톱 (알파 랩터)
        ThunderNest,  // 지역 2: 천둥 둥지 (프테라 여왕)
        Hibernator,   // 지역 3: 동면자 (고대 모사)
        Original      // 최종: 디 오리지널
    }

    /// <summary>v7.4: 견습 구간 7 예습용 새끼 발톱 - WaveManager.SpawnBossForPractice 가 Start 전에 켠다 (보상·카드·베팅 체인 없음, 돌진만)</summary>
    [HideInInspector] public bool practice = false;

    [Header("─ 보스 전용 (런타임 계산 - GameBalance에서 조정) ─")]
    public float bossMaxHP = 1000f;
    public BossKind kind = BossKind.RustClaw;   // Start에서 지역 기반으로 덮어씀

    [Header("─ 보스 이동/공격 ─")]
    public float bossAttackRange = 5f;
    public float bossAttackCooldown = 2.5f;
    public float bossMoveSpeed = 1.6f;

    [Header("─ 보스 방어 스탯 ─")]
    public float bossDefense = 25f;
    public float bossResistance = 25f;

    [Header("─ 그로기 설정 ─")]
    public float groggyDuration = 7f;
    public float groggyCooldownGap = 6f;

    // ── 그로기 상태 ──
    private bool isGroggy = false;
    private float groggyLockUntil = 0f;
    private float[] groggyThresholds = { 0.75f, 0.50f, 0.25f };
    private bool[] groggyTriggered = { false, false, false };
    // v7.1 (교수 피드백 C3): 디 오리지널 전용 추가 그로기 (GameBalance.OriginalExtraGroggyRatio, 기본 12%)
    // - 마지막 주문을 실패한 요리사에게 한 번 더 기회. 다른 보스에는 없음
    private bool extraGroggyTriggered = false;

    /// <summary>v7.1: 디 오리지널의 추가 그로기가 아직 남아 있는가 (마지막 주문 실패 문구용)</summary>
    public bool HasExtraGroggyPending
    {
        get
        {
            // HP가 이미 기준 아래여도 아직 안 터졌으면 다음 피격에 발동하므로 "남아 있음"으로 본다
            return kind == BossKind.Original && GameBalance.OriginalExtraGroggyRatio > 0f
                && !extraGroggyTriggered && IsAlive;
        }
    }

    /// <summary>이번 그로기의 실제 지속 시간 (BossGimmickSystem이 게이지에 사용)</summary>
    public float CurrentGroggyDuration { get; private set; }

    // ── 런지/패턴 상태 ──
    private bool isLunging = false;
    private bool isCasting = false;        // 패턴 시전 중 (이동/공격 정지)
    private float patternTimer = 0f;       // 다음 패턴까지 남은 시간

    // ── 빙하 갑주 (동면자) ──
    private bool armorActive = false;
    private bool secondArmorUsed = false;  // 50% 재전개는 1회만
    private int burnBaseline = 0;          // 갑주 전개 시점의 화상 누적치
    private float armorDR = 0f;            // v5: 현재 갑주 감쇄율 (해동포 GOOD으로 절반 가능)

    // ── v5: 번개 병 패링 (천둥 둥지) ──
    public int ParryCharges { get; private set; }

    // ── v5: 발악 페이즈 ──
    private bool enraged = false;

    // ── v6: 디 오리지널 3페이즈 ──
    private int originalPhase = 1;
    private float feedHealAccum = 0f;   // 폭식으로 회복한 총량 (상한 관리)
    private float feedAtkBonus = 0f;    // 폭식 공격력 보너스 누적
    private bool hatchOpen = false;     // P3: 가슴 해치 개방 (받는 피해 증가)

    // ── v7 (C-2): 마지막 식사 (엔딩 B) ──
    private bool isServing = false;     // 정찬 대접 연출 중 (모든 행동 정지)

    /// <summary>디 오리지널 현재 페이즈 (FinalOrderUI 참조용. 다른 보스는 1)</summary>
    public int OriginalPhaseNow { get { return originalPhase; } }

    /// <summary>갑주 활성 여부 (ThawCannonUI 참조용)</summary>
    public bool ArmorActive { get { return armorActive; } }

    // ── 디버프 요리 복구용 ──
    private float baseDefenseValue;
    private float baseResistanceValue;

    // ── 연출 ──
    private SpriteRenderer[] sprites;
    private Color baseTint = Color.white;
    private WaveManager waveManagerRef;

    // ── v7.8: 그림 (boss_*.png) ──
    private static readonly string[] SKIN_KEYS = { "rust", "thunder", "hibernator", "original" };   // BossKind 순서
    private const int SKIN_SORT = 6;        // 일반 손님(5) 위
    private SpriteRenderer skin;            // 보스 그림 (PNG 가 없으면 null)
    private SpriteRenderer glow;            // 그림 위에 겹친 흰 실루엣 - 상태 발광
    private string skinBase = "";           // "boss_rust" 등
    private string skinShown = "";          // 지금 보이는 그림 이름
    private bool telegraphing = false;      // 패턴 예고 중 (흰빛)

    // ── v7.9: 페이즈 전환 순간·상태 유지 효과 ──
    private Transform skinPivot;            // 그림을 감싼 축 - 전환 순간의 몸 크기와 무방비의 기울어짐은 여기에 건다 (그림 자체는 HitFeelBody 가 쓴다)
    private float shiftAge = -1f;           // 전환 연출이 시작된 뒤 흐른 실시간 (음수 = 없음)
    private float tiltNow = 0f;             // 지금 기울어진 각도
    private float stateFxTimer = 0f;        // 다음 김·불티까지
    private StatePuffs statePuffs;          // 김·연기 조각 풀 (처음 쓸 때 만든다)
    private const float SHIFT_FLASH_FADE = 0.22f;
    private const float SHIFT_PUNCH_SEC = 0.25f;
    // 종류별 색 (녹슨 발톱 / 천둥 둥지 / 동면자 / 디 오리지널): 파편(장갑·속) / 김 / 불티
    private static readonly Color[] SHARD_A = { new Color(0.62f, 0.25f, 0.12f), new Color(0.50f, 0.25f, 0.85f), new Color(0.86f, 0.95f, 1f), new Color(0.80f, 0.15f, 0.12f) };
    private static readonly Color[] SHARD_B = { new Color(0.46f, 0.46f, 0.50f), new Color(0.95f, 0.75f, 0.20f), new Color(0.20f, 0.60f, 0.75f), new Color(0.30f, 0.30f, 0.33f) };
    private static readonly Color[] STEAM = { new Color(0.92f, 0.90f, 0.86f, 0.55f), new Color(0.75f, 0.92f, 1f, 0.5f), new Color(0.82f, 0.95f, 1f, 0.55f), new Color(0.95f, 0.80f, 0.60f, 0.55f) };
    private static readonly Color[] EMBER = { new Color(1f, 0.6f, 0.2f), new Color(0.6f, 0.95f, 1f), new Color(0.7f, 0.95f, 1f), new Color(1f, 0.7f, 0.25f) };
    private static readonly Color SMOKE = new Color(0.2f, 0.19f, 0.2f, 0.6f);
    private static readonly Color SPARK = new Color(1f, 0.95f, 0.7f);

    /// <summary>v7.8: 발악 중인가 (보스 HP 바의 색·딱지)</summary>
    public bool IsEnraged { get { return enraged; } }
    /// <summary>v7.8: 디 오리지널 해치 개방 중인가 (보스 HP 바 딱지)</summary>
    public bool HatchOpen { get { return hatchOpen; } }

    private void Awake()
    {
        // 보스 데이터 초기화 (이름/수치는 Start에서 지역 기반으로 채움)
        data = new EnemyData
        {
            enemyName = "메카 티렉스 보스",
            baseHP = 1000f,
            baseATK = 50f,
            baseSPD = 1.0f,
            dropMaterialName = "메카 티렉스의 심장",
            goldReward = 1000,
            xpReward = 500,
            targetPriority = "기차 전체",
            specialAbility = "HP 75/50/25% 무방비(그로기)"
        };
    }

    private void Start()
    {
        int wave = GameManager.Instance != null ? GameManager.Instance.currentWave : 3;
        if (practice) wave = GameBalance.RegionLength;   // v7.4: 새끼 발톱은 정식 첫 보스(지역 1 마지막 웨이브) 기준으로 재고 배율을 곱한다

        // ── 지역 기반 보스 종류 결정 ──
        int region = GameBalance.RegionOf(wave);
        if (region == 1) kind = BossKind.RustClaw;
        else if (region == 2) kind = BossKind.ThunderNest;
        else if (region == 3) kind = BossKind.Hibernator;
        else kind = BossKind.Original;

        // ── 공통 웨이브 비례 스탯 ──
        bossMaxHP = GameBalance.BossHPBase + wave * GameBalance.BossHPPerWave;
        float bossATK = GameBalance.BossATKBase + wave * GameBalance.BossATKPerWave;
        float spd = bossMoveSpeed;

        // ── 종류별 개성 (이름 / 스탯 방향 / 색) ──
        string intro;
        if (kind == BossKind.RustClaw)
        {
            data.enemyName = "녹슨 발톱";
            bossMaxHP *= 0.9f; bossATK *= 0.9f; spd = 2.0f;      // 빠르고 가벼움
            baseTint = new Color(0.9f, 0.55f, 0.38f);
            intro = "무리의 왕이 나타났다! 호령(예고) 중에 보스를 멈추면 소환이 절반!";
        }
        else if (kind == BossKind.ThunderNest)
        {
            data.enemyName = "천둥 둥지";
            bossMaxHP *= 0.95f; spd = 1.5f; bossAttackRange = 6.5f; // 멀리서 때림
            baseTint = new Color(0.72f, 0.72f, 1f);
            intro = "선대의 번개 병이 기차에 실려 있다 - 낙뢰의 마지막 순간, [Space]로 병을 내밀어라!";
        }
        else if (kind == BossKind.Hibernator)
        {
            data.enemyName = "동면자";
            bossMaxHP *= 1.15f; spd = 1.25f;                      // 느리고 단단함
            baseTint = new Color(0.6f, 0.85f, 1f);
            intro = "고대 모사! 갑주는 화염으로만 녹는다 - 광산의 해동포에 화염을 장전하라!";
        }
        else
        {
            data.enemyName = "디 오리지널";
            bossMaxHP *= 1.2f; bossATK *= 1.1f; spd = 1.4f;
            baseTint = new Color(1f, 0.5f, 0.45f);
            intro = "낡은 기적이 울린다 - 급식 열차 1호였던 것이 식탁에 앉았다.";   // v7.5: 기차였다는 흔적 (일지 3 의 기적)
        }

        // v7.4: 예습 보스 - 작고 약한 새끼. 이름·안내·보상 없음
        if (practice)
        {
            data.enemyName = "새끼 발톱";
            data.goldReward = 0; data.xpReward = 0;
            data.dropMaterialName = "고기";
            bossMaxHP *= GameBalance.BossPracticeHpMul;
            bossATK *= GameBalance.BossPracticeAtkMul;
            baseTint = new Color(0.75f, 0.42f, 0.30f);
            transform.localScale = transform.localScale * GameBalance.BossPracticeScale;
            intro = "녹슨 발톱의 새끼 - 왕의 손님부터 대접해라 (미끼 화덕)";
        }

        currentHP = bossMaxHP;
        scaledMaxHP = bossMaxHP;
        scaledATK = bossATK;
        scaledSPD = spd;

        // 방어 스탯 (부모 자동배정이 안 돌므로 직접)
        defense = bossDefense;
        resistance = bossResistance;
        baseDefenseValue = defense;
        baseResistanceValue = resistance;

        attackRange = bossAttackRange;
        // v7.8: 나란히 서는 자세(BossFaceAlongTrain)면 서는 거리는 GameBalance.BossStandOff (보스 종류별 - 몸 옆면이 지붕 바로 밖에 오는 값).
        //   프리팹 값(5 / 6.5)은 머리부터 들이받는 자세의 거리라, 나란히 설 때 쓰면 기차에서 너무 멀다
        float[] standOff = GameBalance.BossStandOff;
        int standIdx = (int)kind;
        if (GameBalance.BossFaceAlongTrain && standOff != null && standIdx >= 0 && standIdx < standOff.Length && standOff[standIdx] > 0.1f)
            attackRange = standOff[standIdx];
        attackCooldown = bossAttackCooldown;

        GameObject trainObj = GameObject.FindGameObjectWithTag("Train");
        if (trainObj != null) trainTarget = trainObj.transform;
        trainManager = FindFirstObjectByType<TrainManager>();
        waveManagerRef = FindFirstObjectByType<WaveManager>();

        // 색 입히기 (자식 스프라이트 전부). v7.8: 보스 그림이 있으면 그걸 입히고 프리팹의 색 사각형은 끈다 (그림엔 자기 색이 있으니 틴트는 흰색)
        if (SetupSkin()) { sprites = new SpriteRenderer[] { skin }; baseTint = Color.white; }
        else sprites = GetComponentsInChildren<SpriteRenderer>();
        ApplyTint(baseTint);

        // 동면자: 개전 시 빙하 갑주 전개
        if (kind == BossKind.Hibernator)
            ActivateArmor();

        // 첫 패턴 타이머
        patternTimer = GameBalance.BossPatternFirstDelay;

        BossGimmickSystem.Instance?.RegisterBoss(this);
        EndingWhistled = false;
        // v7.7 (D2): 등장 연출 - 예습 보스와 연출 끔은 예전처럼 바로 포효
        if (!practice && GameBalance.BossEntranceOn && GameBalance.GameFeelMaster > 0f)
        {
            BossGimmickSystem.Instance?.HoldBarForIntro();   // v7.8: HP 바는 포효 순간에 내려와 차오른다
            StartCoroutine(EntranceRoutine(intro));
        }
        else AnnounceEntrance(intro);

        Debug.Log("[BossEnemy] " + data.enemyName + " 등장! (웨이브 " + wave + ") HP:" + (int)bossMaxHP
            + " ATK:" + (int)scaledATK + " 종류:" + kind);
    }

    /// <summary>v7.7: 최종전에서 엔딩 쪽이 이미 기적을 울렸다 (격파 = 한 번, 식사 = 두 번). GameManager.HandleVictory 가 보고 또 울리지 않는다</summary>
    public static bool EndingWhistled = false;

    /// <summary>이름 예고 + 등장 포효 (종류별. 예습 보스는 작게). 디 오리지널은 포효 뒤 1호의 낮은 기적</summary>
    private void AnnounceEntrance(string intro)
    {
        // v7.8: 경고 띠(이름) -> 띠가 걷힌 뒤 대응법 안내. 같은 순간에 이름을 두 군데서 말하지 않는다. 예습 보스·연출 끔은 예전처럼 안내만
        float band = (!practice && GameBalance.GameFeelMaster > 0f) ? GameBalance.BossIntroBandSec : 0f;
        if (band > 0f)
        {
            WarningFX.BossIntro(data.enemyName, Epithet(), band);
            StartCoroutine(NoticeAfter(Mathf.Max(0f, band - 0.2f), "[" + data.enemyName + "]", intro));
        }
        else BossNotice("[" + data.enemyName + "]", intro);   // v7.9: HP 바 밑 띠로
        if (!practice) BossGimmickSystem.Instance?.PlayBarIntro();
        SoundManager.Play(SoundKeys.BossRoar(kind.ToString()), practice ? 0.6f : 1f, -1f);
        if (kind == BossKind.Original && !practice) SoundManager.PlayDelayed("sfx_whistle_low", 0.8f);   // v7.5: 낡은 기적 - 두 기차의 관계 단서
    }

    /// <summary>v7.8: 경고 띠에 이름 아래 한 줄 (보스가 무엇인지 - 기존 카드·안내에 쓰던 말)</summary>
    private string Epithet()
    {
        switch (kind)
        {
            case BossKind.RustClaw: return "무리의 왕";
            case BossKind.ThunderNest: return "프테라 여왕";
            case BossKind.Hibernator: return "고대 모사";
            default: return "급식 열차 1호였던 것";
        }
    }

    /// <summary>v7.8: sec 초(실시간) 뒤 가운데 예고. 그사이 쓰러졌으면 안 띄운다</summary>
    private IEnumerator NoticeAfter(float sec, string title, string body)
    {
        float t = 0f;
        while (t < sec) { t += Time.unscaledDeltaTime; yield return null; }
        if (IsAlive) BossNotice(title, body);   // v7.9: HP 바 밑 띠로 (가운데 카드는 보스 몸을 가렸다)
    }

    // ─────────────────────────────────────────────
    // v7.8: 그림
    // ─────────────────────────────────────────────
    /// <summary>boss_<종류>.png 를 자식 "Skin" 으로 붙인다. PNG 가 없거나 스위치가 꺼져 있으면 false (프리팹 그림 유지)</summary>
    private bool SetupSkin()
    {
        if (!GameBalance.BossSkinOn) return false;
        int k = Mathf.Clamp((int)kind, 0, SKIN_KEYS.Length - 1);
        skinBase = "boss_" + SKIN_KEYS[k];
        Sprite s = SpriteBank.Get(skinBase);
        if (s == null) return false;

        // 프리팹의 색 사각형 끄기 (로직·태그는 그대로)
        SpriteRenderer[] old = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < old.Length; i++) old[i].enabled = false;

        // 크기: 그림 배율 / 루트 스케일. 예습 보스는 루트가 이미 BossPracticeScale 만큼 줄어 있어 그림도 같이 줄어든다
        float rootScale = Mathf.Abs(transform.localScale.x);
        if (practice && GameBalance.BossPracticeScale > 0.01f) rootScale /= GameBalance.BossPracticeScale;
        if (rootScale < 0.01f) rootScale = 1f;
        float want = (GameBalance.BossSkinScale != null && k < GameBalance.BossSkinScale.Length) ? GameBalance.BossSkinScale[k] : 0.85f;
        float local = want / rootScale;

        // v7.9: 그림을 축(SkinPivot)으로 감싼다 - 전환 순간의 몸 크기·무방비의 기울어짐은 축에, 맞을 때의 찌그러짐은 그림에 (서로 덮어쓰지 않게)
        GameObject pivotGo = new GameObject("SkinPivot");
        pivotGo.transform.SetParent(transform, false);
        skinPivot = pivotGo.transform;

        GameObject go = new GameObject("Skin");
        go.transform.SetParent(skinPivot, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = new Vector3(local, local, 1f);
        skin = go.AddComponent<SpriteRenderer>();
        skin.sprite = s;
        skin.sortingOrder = SKIN_SORT;
        skinShown = skinBase;

        // 상태 발광용 흰 실루엣 (이름이 Flash_ 로 시작하면 HitFeelBody 가 몸으로 세지 않는다)
        Sprite white = FlashSprites.Get(s);
        if (white != null)
        {
            GameObject g = new GameObject("Flash_BossGlow");
            g.transform.SetParent(go.transform, false);
            glow = g.AddComponent<SpriteRenderer>();
            glow.sprite = white;
            glow.sortingOrder = SKIN_SORT + 1;
            glow.color = new Color(1f, 1f, 1f, 0f);
            glow.enabled = false;
        }
        Debug.Log("[BossEnemy] 그림 " + skinBase + " 적용 (배율 " + want + ")");
        return true;
    }

    private bool HasSkin(string suffix) { return SpriteBank.Has(skinBase + suffix); }

    /// <summary>
    /// 지금 상태에 맞는 그림 이름. 상태 그림은 파일이 있을 때만 쓴다 (없으면 그 앞 단계 그림).
    ///   지역 보스: 무방비 = _groggy (발악 중이면 _rage_groggy 먼저) / 발악 = _rage
    ///   디 오리지널: P3 = _p3 (없으면 해치가 열린 _groggy) / P2 = _p2, P2 무방비 = _p2_groggy / P1 무방비 = _groggy
    /// GameBalance.BossStateSkins 를 끄면 v9.18 에 있던 그림만 쓴다 (디 오리지널의 해치 개방 boss_original_groggy)
    /// </summary>
    private string WantedSkin()
    {
        if (!GameBalance.BossStateSkins)
        {
            if (kind == BossKind.Original && (isGroggy || originalPhase >= 3) && HasSkin("_groggy")) return skinBase + "_groggy";
            return skinBase;
        }
        if (kind == BossKind.Original)
        {
            if (originalPhase >= 3)
            {
                if (HasSkin("_p3")) return skinBase + "_p3";
                if (HasSkin("_groggy")) return skinBase + "_groggy";   // 해치가 열린 그림 = 기관심장이 드러난 P3
            }
            else if (originalPhase == 2)
            {
                if (isGroggy && HasSkin("_p2_groggy")) return skinBase + "_p2_groggy";
                if (!isGroggy && HasSkin("_p2")) return skinBase + "_p2";
            }
            if (isGroggy && HasSkin("_groggy")) return skinBase + "_groggy";
            return skinBase;
        }
        if (isGroggy)
        {
            if (enraged && HasSkin("_rage_groggy")) return skinBase + "_rage_groggy";
            if (HasSkin("_groggy")) return skinBase + "_groggy";
        }
        if (enraged && HasSkin("_rage")) return skinBase + "_rage";
        return skinBase;
    }

    /// <summary>
    /// 매 프레임: 상태 그림 바꾸기 + 상태 발광 (예고 = 흰빛 깜빡 / 무방비 = 금빛 / 발악 = 붉은 맥동)
    /// + v7.9: 전환 순간의 흰 번쩍·몸 크기 / 무방비의 기울어짐 / 상태 유지 효과(김·불티·불똥·연기). 그림이 없으면 상태 유지 효과만
    /// </summary>
    private void TickSkin()
    {
        TickStateFx();
        if (skin == null) return;

        string want = WantedSkin();
        if (want != skinShown)
        {
            Sprite s = SpriteBank.Get(want);
            if (s != null)
            {
                skin.sprite = s;
                if (glow != null) { Sprite w = FlashSprites.Get(s); if (w != null) glow.sprite = w; }
            }
            skinShown = want;
        }

        // v7.9 (A2): 전환 순간 - 흰 번쩍(히트스톱 동안 가득, 그 뒤 SHIFT_FLASH_FADE 에 걷힌다) + 몸이 커졌다가 SHIFT_PUNCH_SEC 에 제자리.
        // 그림은 위에서 이미 바뀌었다 - 흰 실루엣 한 장 아래에서 바뀐 셈이라 "번쩍하고 달라졌다"로 보인다
        float flash = 0f, punch = 0f;
        if (shiftAge >= 0f)
        {
            shiftAge += Time.unscaledDeltaTime;
            float after = shiftAge - Mathf.Max(0f, GameBalance.BossPhaseShiftHitstop);
            flash = after <= 0f ? 1f : Mathf.Clamp01(1f - after / SHIFT_FLASH_FADE);
            punch = after <= 0f ? 1f : Mathf.Clamp01(1f - after / SHIFT_PUNCH_SEC);
            punch *= punch;
            if (after >= Mathf.Max(SHIFT_FLASH_FADE, SHIFT_PUNCH_SEC)) shiftAge = -1f;
        }

        // v7.9 (A3): 무방비 = 몸이 기울어 느리게 흔들린다 (주저앉은 모습). 그림을 감싼 SkinPivot 을 돌린다 - 그림 자체는 HitFeelBody 가 찌그러뜨린다
        if (skinPivot != null)
        {
            float tiltMax = Mathf.Max(0f, GameBalance.BossGroggyTiltDeg);
            float tiltWant = (isGroggy && !isServing && GameBalance.BossStateFx) ? tiltMax : 0f;
            tiltNow = Mathf.MoveTowards(tiltNow, tiltWant, 40f * Time.deltaTime);
            float sway = tiltMax > 0.01f ? Mathf.Sin(Time.time * 1.8f) * 1.5f * (tiltNow / tiltMax) : 0f;
            float sc = 1f + GameBalance.BossPhaseShiftPunch * punch;
            skinPivot.localScale = new Vector3(sc, sc, 1f);
            skinPivot.localRotation = Quaternion.Euler(0f, 0f, tiltNow + sway);
        }

        if (glow == null) return;
        Color c = Color.clear;
        float t = Time.time;
        if (flash > 0f) c = new Color(1f, 1f, 1f, flash);
        else if (isServing) c = Color.clear;
        else if (isGroggy) c = new Color(1f, 0.85f, 0.35f, 0.22f + 0.1f * Mathf.Sin(t * 5f));
        else if (telegraphing) c = new Color(1f, 1f, 1f, 0.3f + 0.25f * Mathf.Sin(t * 14f));
        else if (enraged && GameBalance.BossEnrageSignal) c = new Color(1f, 0.15f, 0.08f, 0.16f + 0.12f * Mathf.Sin(t * 7f));
        c.a *= Mathf.Clamp01(GameBalance.GameFeelMaster);
        if (c.a <= 0.01f) { if (glow.enabled) glow.enabled = false; }
        else { glow.color = c; if (!glow.enabled) glow.enabled = true; }
    }

    // ─────────────────────────────────────────────
    // v7.9: 페이즈 전환 순간 (A2) / 상태 유지 효과 (A3) / 알림 자리 (A4)
    // ─────────────────────────────────────────────
    /// <summary>
    /// A2: 페이즈가 바뀌는 순간 - 히트스톱 -> 흰 번쩍 한 장 아래에서 그림이 바뀐다 -> 장갑 파편 + 링 + 폭음 -> 몸이 커졌다가 제자리.
    /// 지역 보스의 발악과 디 오리지널의 P2·P3 에만 쓴다 (무방비는 자기 연출이 따로 있다). 그림이 없는 보스는 파편·링·소리만
    /// </summary>
    private void PhaseShift()
    {
        if (practice || !GameBalance.BossPhaseShiftFx || GameBalance.GameFeelMaster <= 0f) return;
        shiftAge = 0f;                                       // 다음 TickSkin 부터 흰 번쩍·몸 크기가 돈다
        GameFeel.Hitstop(GameBalance.BossPhaseShiftHitstop);
        StartCoroutine(PhaseShiftBurst());
    }

    private IEnumerator PhaseShiftBurst()
    {
        // 파편은 히트스톱이 풀리는 순간에 터진다 (멈춘 화면에서 터지면 안 보인다)
        float wait = Mathf.Max(0f, GameBalance.BossPhaseShiftHitstop), t = 0f;
        while (t < wait) { t += Time.unscaledDeltaTime; yield return null; }
        if (!IsAlive) yield break;
        int k = Mathf.Clamp((int)kind, 0, SHARD_A.Length - 1);
        ArmorShard.Burst(transform.position, SHARD_A[k], SHARD_B[k], GameBalance.BossPhaseShiftShards);
        WorldFeel.Ring(transform.position, Color.Lerp(EMBER[k], Color.white, 0.3f), 3.4f, 0.35f);
        SoundManager.Play("sfx_explosion", 0.6f, 0.03f);
    }

    /// <summary>
    /// A3: 상태가 이어지는 동안 몸에서 새는 것 - 발악(디 오리지널은 P2 부터) = 김 + 불티 / 무방비 = 불똥 + 검은 연기.
    /// 정지 화면에서도 상태가 읽히게. 계속 나오는 효과라 작고 드물게 (0.15 ~ 0.3초에 조각 하나꼴)
    /// </summary>
    private void TickStateFx()
    {
        if (practice || isServing || !GameBalance.BossStateFx || GameBalance.GameFeelMaster <= 0f) return;
        bool hot = enraged || (kind == BossKind.Original && originalPhase >= 2);
        if (!isGroggy && !hot) return;

        stateFxTimer -= Time.deltaTime;
        if (stateFxTimer > 0f) return;
        int k = Mathf.Clamp((int)kind, 0, STEAM.Length - 1);
        Vector3 p = RandomBodyPoint();
        // 김·연기는 지면이 흐르는 쪽(오른쪽)으로 밀리며 화면 위로 뜬다
        Vector3 drift = new Vector3(ParallaxBackground.CurrentSpeed * 0.35f, 0.55f, 0f);
        if (statePuffs == null) statePuffs = StatePuffs.Create(this);
        if (isGroggy)
        {
            stateFxTimer = Random.Range(0.18f, 0.3f);
            if (Random.value < 0.45f) SparkPool.Emit(p, SPARK, 3, 0.07f, 3.5f);
            statePuffs.Emit(p, SMOKE, Random.Range(0.6f, 0.9f), 0.8f, drift);
        }
        else
        {
            stateFxTimer = Random.Range(0.15f, 0.26f);
            if (Random.value < 0.6f) SparkPool.Emit(p, EMBER[k], 1, 0.08f, 2.2f);
            if (Random.value < 0.7f) statePuffs.Emit(p, STEAM[k], Random.Range(0.5f, 0.8f), 0.6f, drift);
        }
    }

    /// <summary>몸 위의 아무 자리 (그림의 가운데 70% 안). 그림이 없으면 중심에서 1.5u 안</summary>
    private Vector3 RandomBodyPoint()
    {
        if (skin != null && skin.sprite != null)
        {
            Vector3 ext = skin.sprite.bounds.extents;
            Vector3 local = new Vector3(Random.Range(-ext.x, ext.x) * 0.7f, Random.Range(-ext.y, ext.y) * 0.7f, 0f);
            return skin.transform.TransformPoint(local);
        }
        return transform.position + new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-0.8f, 0.8f), 0f);
    }

    /// <summary>A4: 보스 쪽 알림을 HP 바 밑 띠에 (true). 띠에 못 띄우면 false - 부른 쪽이 예전 방식으로. maxWait = 띠가 비기를 기다리는 한도(초)</summary>
    private bool BossLine(string title, string body, float maxWait)
    {
        BossGimmickSystem sys = BossGimmickSystem.Instance;
        return sys != null && sys.ShowBossLine(title, body, GameBalance.BossLineSec, maxWait);
    }

    /// <summary>
    /// A4: 보스 쪽 알림(대응법이 들어 있는 것) - 띠에 띄우고, 안 되면 가운데 예고 카드 (가운데 카드는 보스가 서는 높이라 몸을 가린다).
    /// 무방비 중이면 띠가 7초까지 안 빈다 - 그동안 놓치지 않게 알림 줄(로그)에도 바로 한 줄 남긴다 (띠는 무방비가 끝난 뒤에 뜬다)
    /// </summary>
    private void BossNotice(string title, string body)
    {
        if (!BossLine(title, body, 12f)) { UIManager.Instance?.ShowWaveNotice(title, body); return; }
        if (isGroggy) UIManager.Instance?.ShowStatChange(string.IsNullOrEmpty(body) ? title : title + " - " + body);
    }

    /// <summary>A4: 무방비와 같은 순간에 나는 알림 - 띠는 무방비 안내가 쓰므로 알림 줄(로그)로. 스위치가 꺼져 있으면 예전처럼 가운데 카드</summary>
    private void BossNoticeAtGroggy(string title, string body)
    {
        if (GameBalance.BossNoticeInBar && GameBalance.BossBarBig) UIManager.Instance?.ShowStatChange(title + " " + body);
        else UIManager.Instance?.ShowWaveNotice(title, body);
    }

    /// <summary>A2 의 장갑 파편 한 조각: 튀어나와 금방 느려지며 돌다가 흐려진다. 스스로 사라진다 (보스가 먼저 사라져도 남지 않는다)</summary>
    private class ArmorShard : MonoBehaviour
    {
        private Vector3 vel;
        private float spin, age, life;
        private SpriteRenderer sr;

        public static void Burst(Vector3 pos, Color armor, Color inner, int count)
        {
            Sprite square = TrainDeck.GetWhiteSprite();
            if (square == null || count <= 0) return;
            for (int i = 0; i < count; i++)
            {
                float ang = (i + Random.value) / count * Mathf.PI * 2f;      // 사방으로 고르게
                Vector3 dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                GameObject go = new GameObject("ArmorShard");
                go.transform.position = pos + dir * Random.Range(0.4f, 1.1f);
                go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                go.transform.localScale = new Vector3(Random.Range(0.18f, 0.36f), Random.Range(0.12f, 0.24f), 1f);
                ArmorShard sh = go.AddComponent<ArmorShard>();
                sh.sr = go.AddComponent<SpriteRenderer>();
                sh.sr.sprite = square;
                sh.sr.sortingOrder = 58;                                     // 손님·보스 위 (불똥과 같은 층)
                sh.sr.color = (i % 3 == 0) ? inner : armor;
                sh.vel = dir * Random.Range(4.5f, 8f);
                sh.spin = Random.Range(-540f, 540f);
                sh.life = Random.Range(0.5f, 0.7f);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;                                       // 히트스톱·일시정지 동안은 같이 멈춘다
            age += dt;
            if (age >= life) { Destroy(gameObject); return; }
            vel *= Mathf.Pow(0.03f, dt);                                     // 0.5초 뒤 속도 17%
            transform.position += vel * dt;
            transform.Rotate(0f, 0f, spin * dt);
            float k = age / life;
            Color c = sr.color; c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f; sr.color = c;
        }
    }

    /// <summary>
    /// A3 의 김·연기 조각 풀 (보스마다 하나). 보스의 자식이 아니다 - 보스가 돌아도 연기는 제 갈 길로 흐른다.
    /// 보스가 사라지면 남은 조각이 다 꺼진 뒤 스스로 없어진다
    /// </summary>
    private class StatePuffs : MonoBehaviour
    {
        private const int MAX = 12;
        private readonly SpriteRenderer[] sr = new SpriteRenderer[MAX];
        private readonly Vector3[] vel = new Vector3[MAX];
        private readonly float[] age = new float[MAX], life = new float[MAX], size = new float[MAX];
        private readonly Color[] col = new Color[MAX];
        private readonly bool[] on = new bool[MAX];
        private Sprite[] frames;                                             // 먼지 4프레임 (작고 진함 -> 크고 옅음). 없으면 흰 사각형
        private int cursor;
        private BossEnemy owner;

        public static StatePuffs Create(BossEnemy owner)
        {
            GameObject go = new GameObject("BossStatePuffs");
            StatePuffs p = go.AddComponent<StatePuffs>();
            p.owner = owner;
            Sprite d0 = SpriteBank.Get("dust_0");
            if (d0 != null)
            {
                p.frames = new Sprite[4];
                for (int i = 0; i < 4; i++) p.frames[i] = SpriteBank.Get("dust_" + i) ?? d0;
            }
            else p.frames = new Sprite[] { TrainDeck.GetWhiteSprite() };
            for (int i = 0; i < MAX; i++)
            {
                GameObject c = new GameObject("Puff");
                c.transform.SetParent(go.transform, false);
                p.sr[i] = c.AddComponent<SpriteRenderer>();
                p.sr[i].sortingOrder = SKIN_SORT + 2;                        // 보스 그림(6)·발광(7) 위
                c.SetActive(false);
            }
            return p;
        }

        /// <summary>조각 하나: pos 에서 v 로 흐르며 sec 동안 커지며 흐려진다. sz = 배율 (1 = 먼지 그림 0.75u)</summary>
        public void Emit(Vector3 pos, Color c, float sz, float sec, Vector3 v)
        {
            int i = cursor; cursor = (cursor + 1) % MAX;
            on[i] = true; age[i] = 0f; life[i] = Mathf.Max(0.1f, sec); size[i] = sz; col[i] = c;
            vel[i] = v + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.15f, 0.25f), 0f);
            sr[i].transform.position = pos;
            sr[i].transform.localScale = new Vector3(sz, sz, 1f);
            sr[i].sprite = frames[0];
            sr[i].color = c;
            sr[i].gameObject.SetActive(true);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            bool any = false;
            for (int i = 0; i < MAX; i++)
            {
                if (!on[i]) continue;
                age[i] += dt;
                float k = age[i] / life[i];
                if (k >= 1f) { on[i] = false; sr[i].gameObject.SetActive(false); continue; }
                any = true;
                sr[i].transform.position += vel[i] * dt;
                float s = size[i] * (1f + 0.7f * k);
                sr[i].transform.localScale = new Vector3(s, s, 1f);
                sr[i].sprite = frames[Mathf.Min(frames.Length - 1, Mathf.FloorToInt(k * frames.Length))];
                Color c = col[i]; c.a = col[i].a * (1f - k * k); sr[i].color = c;
            }
            if (owner == null && !any) Destroy(gameObject);
        }
    }

    /// <summary>v7.8: 무는 양 배율 - 정식 보스 GameBalance.BossMeleeMul / 예습 보스 BossPracticeMeleeMul (공격력 자체의 BossPracticeAtkMul 과 별개)</summary>
    protected override void AttackTrain()
    {
        float keep = scaledATK;
        scaledATK = keep * (practice ? GameBalance.BossPracticeMeleeMul : GameBalance.BossMeleeMul);
        base.AttackTrain();
        scaledATK = keep;
    }

    /// <summary>
    /// v7.7 (D2): 보스 등장 순서 (실시간). 첫 등장 카드·스토리 글·일시정지가 떠 있으면 닫힐 때까지 기다린다 (읽는 동안 포효가 먼저 지나가던 것)
    /// -> 배경이 BossEntranceDimSec 동안 어두워진다 -> 흔들림 + 포효 + 이름 예고 -> 밝아진다
    /// </summary>
    private IEnumerator EntranceRoutine(string intro)
    {
        yield return null;   // WaveManager.SpawnBoss 가 같은 프레임에 여는 첫 등장 카드가 열릴 틈
        // 카드·스토리 글·증강 창·일시정지가 떠 있는 동안은 기다린다 (그동안 게임도 멈춰 있다).
        // 카드가 아직 못 열리고 줄만 서 있는 경우(조리 중 등 - 게임은 돌고 있다)는 3초까지만 기다린다 - 보스가 한참 싸운 뒤에야 포효하지 않게
        float queued = 0f;
        while (true)
        {
            bool shown = BriefingUI.IsOpen || StoryTexts.IsBlocking || AugmentPickUI.IsOpen || PauseMenu.IsOpen;
            bool waitingCard = !shown && BriefingUI.Busy;
            if (!shown && !waitingCard) break;
            if (waitingCard)
            {
                queued += Time.unscaledDeltaTime;
                if (queued >= 3f) break;
            }
            yield return null;
        }

        float dimSec = Mathf.Max(0.05f, GameBalance.BossEntranceDimSec);
        ScreenFx.WorldDim(GameBalance.BossEntranceDim, dimSec, 0.15f, 0.4f);
        float t = 0f;
        while (t < dimSec) { t += Time.unscaledDeltaTime; yield return null; }

        if (!IsAlive) yield break;
        GameFeel.Shake(GameBalance.BossEntranceShake);
        AnnounceEntrance(intro);
    }

    // ─────────────────────────────────────────────
    // 메인 루프
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (!IsAlive) return;
        if (TutorialDirector.InlineFreeze) return;   // v7.4: 인라인 연습 중 정지 (보스 웨이브엔 연습이 안 뜨지만 안전장치)

        // 도트/방깎 타이머 (v3에서 수정된 보스 도트 버그 유지)
        TickStatusEffects();
        TickSkin();   // v7.8: 상태 그림·발광

        // 빙하 갑주 파괴 판정 (화상 스택 누적 감시)
        if (armorActive && TotalBurnApplied - burnBaseline >= GameBalance.GlacierBreakBurnStacks)
            BreakArmor();

        // v5: 발악 페이즈 진입 (HP 50% 이하, 1회). v7.4: 예습 보스는 발악·무방비·패턴 없음 (돌진만)
        if (!practice && !enraged && currentHP / bossMaxHP <= GameBalance.EnrageHPRatio)
        {
            enraged = true;
            UIManager.Instance?.ShowStatChange("[" + data.enemyName + "] 발악! 패턴이 빨라진다!");
            // v7.8: 알림 한 줄로는 지나쳤다 - 붉은 경고 + 흔들림 + 포효 한 번. 그 뒤로 몸에 붉은 맥동(TickSkin), HP 바에 "발악" 딱지
            if (GameBalance.BossEnrageSignal && GameBalance.GameFeelMaster > 0f)
            {
                // v7.9 (A4): 화면 가운데 큰 글자는 바뀌는 보스 몸을 가렸다 - 가장자리 맥동 + HP 바 밑 한 줄로. 띠에 못 띄우면 예전처럼.
                // 발악(HP 50%)은 무방비 임계(50%)와 같은 프레임에 걸리곤 한다 - 그때는 띠를 무방비 안내가 쓰므로 2.5초만 기다리고 버린다
                // (위의 알림 줄 한 줄과 HP 바의 "발악" 딱지가 남는다. 7초 뒤에 뒤늦게 뜨지 않게)
                if (BossLine("[" + data.enemyName + "] 발악", "패턴이 빨라진다", 2.5f)) WarningFX.FlashEdges(1.4f, new Color(1f, 0.15f, 0.1f));
                else WarningFX.Flash("[" + data.enemyName + "] 발악!", 1.4f);
                GameFeel.Shake(GameBalance.ShakeBoss * 0.7f);
                SoundManager.Play(SoundKeys.BossRoar(kind.ToString()), 0.8f, -1f);
            }
            if (kind != BossKind.Original) PhaseShift();   // v7.9 (A2): 그림이 바뀌는 순간 (디 오리지널은 P2·P3 에서)
            Debug.Log("[BossEnemy] 발악 페이즈 진입!");
        }

        // v6: 디 오리지널 페이즈 전환
        if (kind == BossKind.Original)
            CheckOriginalPhases();

        // 그로기 진입 체크
        if (!practice && !isGroggy && Time.time >= groggyLockUntil)
            CheckGroggyThresholds();

        if (isServing) return;   // v7: 마지막 식사 연출 중 - 완전 정지
        if (isGroggy || isLunging || isCasting) return;

        if (trainTarget == null)
        {
            GameObject trainObj = GameObject.FindGameObjectWithTag("Train");
            if (trainObj != null) trainTarget = trainObj.transform;
            return;
        }

        // 패턴 타이머 (통상 상태에서만 감소). v7.4: 예습 보스는 패턴 없음
        if (!practice)
        {
            patternTimer -= Time.deltaTime;
            if (patternTimer <= 0f)
            {
                StartCoroutine(RunPattern());
                return;
            }
        }

        // 통상 이동/공격 (v6: 도발 중이면 미끼를 추적)
        attackTimer += Time.deltaTime;
        // v7.8: 가장 가까운 기차 몸통까지의 거리 (Enemy 와 같은 기준). 구: 기차 "중심"(0,0)까지 - 기차가 4칸으로 길어진 뒤로
        //   중심에서 5u 밖으로 다가온 보스(옆에서 온 경우 대부분)는 영영 사거리에 못 들고 기차 위에 올라앉아 물지도 않았다
        float distanceToTrain = Vector3.Distance(transform.position, CurrentTargetPos);

        if (distanceToTrain > attackRange)
            MoveTowardsTrain();
        else
        {
            HoldStance();   // v7.8: 기차와 나란히
            if (attackTimer >= attackCooldown)
            {
                attackTimer = 0f;
                StartCoroutine(AttackLunge());
            }
        }
    }

    // ─────────────────────────────────────────────
    // v7.8: 서는 자세 - 기차와 나란히 (GameBalance.BossFaceAlongTrain)
    //   그림은 오른쪽을 보고 그려져 있고, 손님은 가는 쪽으로 몸을 돌린다 (Enemy.MoveTowardsTrain).
    //   보스가 그대로 서면 머리가 기차를 향하고 긴 몸이 세로로 놓여, 뒤쪽이 화면 위 HP 바 뒤로 들어간다.
    //   그래서 기차 옆에 이르면 기차가 달리는 쪽(왼쪽, 180도)으로 돌아 나란히 선다 - 몸 전체가 지붕과 HP 바 사이에 보인다
    // ─────────────────────────────────────────────
    private float faceAngle = float.NaN;   // 지금 몸이 보는 각도 (도). NaN = 아직 안 정함

    /// <summary>나란히 서는 자세를 쓸 때인가 (미끼에 끌려갈 때는 미끼를 본다)</summary>
    private bool StanceOn { get { return GameBalance.BossFaceAlongTrain && !IsTaunted; } }

    /// <summary>몸을 want 각도로 돌린다 (BossTurnDegPerSec 로 부드럽게)</summary>
    private void TurnBody(float want)
    {
        if (float.IsNaN(faceAngle)) faceAngle = want;
        faceAngle = Mathf.MoveTowardsAngle(faceAngle, want, GameBalance.BossTurnDegPerSec * Time.deltaTime);
        transform.rotation = Quaternion.AngleAxis(faceAngle, Vector3.forward);
    }

    /// <summary>사거리 안에 서 있는 동안: 기차가 달리는 쪽(왼쪽)을 본다</summary>
    private void HoldStance()
    {
        if (StanceOn && !isLunging) TurnBody(180f);
    }

    /// <summary>다가오는 동안: 멀리서는 기차를 보고 오다가, 서는 거리 + BossTurnZone 안에 들어오면 미리 옆으로 돈다</summary>
    protected override void MoveTowardsTrain()
    {
        base.MoveTowardsTrain();   // 이동 + 표적 쪽으로 몸 돌리기
        Vector3 toTarget = CurrentTargetPos - transform.position;
        float faceTarget = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        if (!StanceOn) { faceAngle = faceTarget; return; }   // 구 동작 그대로 (각도만 기억해 둔다)
        TurnBody(toTarget.magnitude <= attackRange + GameBalance.BossTurnZone ? 180f : faceTarget);
    }

    // ─────────────────────────────────────────────
    // v6: 디 오리지널 3페이즈 관리
    // ─────────────────────────────────────────────
    private void CheckOriginalPhases()
    {
        float ratio = currentHP / bossMaxHP;

        // P2 폭식 (70% 이하): 재료 조각 쟁탈전 시작
        if (originalPhase == 1 && ratio <= GameBalance.FeedPhaseStartRatio)
        {
            originalPhase = 2;
            PickupFX.FeedingBoss = this;
            BossNotice("[디 오리지널] 폭식!",
                "원료 조각을 닥치는 대로 삼킨다 - 곁에 두면 회복하고 세진다. 식사가 아니라 연료다");   // v7.5: 원료 섭취와 식사의 구분
            PhaseShift();                                                                            // v7.9 (A2): 장갑이 벗겨지는 순간
            SoundManager.Play(SoundKeys.BossRoar(kind.ToString()), 0.8f, -1f);
            Debug.Log("[BossEnemy] P2 폭식 페이즈 - 조각 쟁탈전 시작");
        }

        // P3 해치 개방 (35% 이하): 폭식 종료 + 받는 피해 증가
        if (originalPhase == 2 && ratio <= GameBalance.HatchPhaseStartRatio)
        {
            originalPhase = 3;
            PickupFX.FeedingBoss = null;
            hatchOpen = true;
            ApplyTint(Color.Lerp(baseTint, Color.white, 0.35f));   // 해치의 빛
            BossNotice("[디 오리지널] 가슴 해치 개방 - 기관심장이 드러났다",
                "받는 피해 +" + Mathf.RoundToInt((GameBalance.HatchDamageTakenMul - 1f) * 100f)
                + "%. 무방비 때 [F] 격파, 또는 [R] 마지막 식사");   // v7.5: 왜 지금 식사가 가능한지
            PhaseShift();                                           // v7.9 (A2): 기관심장이 드러나는 순간
            SoundManager.Play(SoundKeys.BossRoar(kind.ToString()), 1f, -1f);
            Debug.Log("[BossEnemy] P3 해치 개방 - 받는 피해 증가");
        }
    }

    /// <summary>v6: 폭식 - 재료 조각을 먹어치움 (PickupFX가 호출)</summary>
    public void EatFragment()
    {
        if (!IsAlive) return;

        // 회복 (총량 상한)
        float cap = bossMaxHP * GameBalance.FeedHealCapRatio;
        if (feedHealAccum < cap)
        {
            float heal = Mathf.Min(GameBalance.FeedHealPerFragment, cap - feedHealAccum);
            feedHealAccum += heal;
            currentHP = Mathf.Min(currentHP + heal, bossMaxHP);
        }

        // 공격력 스택 (상한)
        if (feedAtkBonus < GameBalance.FeedAtkCap)
        {
            feedAtkBonus += GameBalance.FeedAtkPerFragment;
            scaledATK *= (1f + GameBalance.FeedAtkPerFragment);
        }

        Debug.Log("[BossEnemy] 폭식! 조각 흡수 (회복 누적 " + (int)feedHealAccum
            + " / ATK 보너스 " + Mathf.RoundToInt(feedAtkBonus * 100f) + "%)");
    }

    // ─────────────────────────────────────────────
    // 패턴 시스템 (A단계: 종류별 시그니처 1개)
    // ─────────────────────────────────────────────
    private IEnumerator RunPattern()
    {
        isCasting = true;

        if (kind == BossKind.RustClaw)
            yield return StartCoroutine(PatternHowl());
        else if (kind == BossKind.ThunderNest)
            yield return StartCoroutine(PatternLightning());
        else if (kind == BossKind.Hibernator)
            yield return StartCoroutine(PatternRearmor());
        else
            yield return StartCoroutine(PatternRoar());

        isCasting = false;

        // v5: 발악 시 패턴 간격 단축
        float interval = GameBalance.BossPatternInterval + Random.Range(-2f, 2f);
        if (enraged) interval *= GameBalance.EnragePatternIntervalMul;
        patternTimer = interval;
    }

    /// <summary>예고 대기 공통 처리. 그로기/사망으로 끊기면 false</summary>
    private IEnumerator Telegraph(string text)
    {
        BossGimmickSystem.Instance?.ShowPatternTelegraph(text, GameBalance.BossTelegraphSec);
        ApplyTint(Color.Lerp(baseTint, Color.white, 0.6f));   // 예고 중 발광
        telegraphing = true;   // v7.8: 그림이 있으면 흰 실루엣이 깜빡인다 (TickSkin)

        float t = 0f;
        while (t < GameBalance.BossTelegraphSec)
        {
            t += Time.deltaTime;
            if (isGroggy || !IsAlive) break;
            yield return null;
        }

        telegraphing = false;
        ApplyTint(armorActive ? ArmorTint() : baseTint);
    }

    /// <summary>지역 1 - 사냥 호령: 랩터 소환. 예고 중 스턴 명중 시 절반으로 저지</summary>
    private IEnumerator PatternHowl()
    {
        float castStart = Time.time;
        yield return StartCoroutine(Telegraph("사냥 호령! 울음소리가 황야를 가른다 (마비·멈춤으로 저지!)"));
        if (isGroggy || !IsAlive) yield break;

        int count = GameBalance.HowlSummonCount + (enraged ? GameBalance.EnrageExtraSummon : 0);
        bool disrupted = LastStunTime >= castStart;   // 예고 중 스턴 맞았는가
        if (disrupted)
        {
            count = Mathf.Max(1, count / 2);
            UIManager.Instance?.ShowStatChange("호령 저지 성공! 소환 절반!");
        }

        if (waveManagerRef != null)
            waveManagerRef.SpawnReinforcements("raptor", count, 0.7f);
        Debug.Log("[BossEnemy] 사냥 호령 - 랩터 " + count + "마리" + (disrupted ? " (저지됨)" : ""));
    }

    /// <summary>
    /// 지역 2 - 낙뢰 폭격 + 번개 병 패링 (v5)
    /// 예고 마지막 ParryWindowSec 동안 Space -> 낙뢰를 병에 담는다 (낙뢰 무효 + 1충전)
    /// 너무 일찍 누르면 헛스윙 (이번 낙뢰의 패링 기회 소진)
    /// 3병 모으면 여왕에게 되쏘아 강제 그로기
    /// </summary>
    private IEnumerator PatternLightning()
    {
        float teleSec = GameBalance.BossTelegraphSec;
        BossGimmickSystem.Instance?.ShowPatternTelegraph(
            "낙뢰 폭격! 게이지 끝자락에서 [Space] 패링 - 번개를 병에 담아라!", teleSec);
        ApplyTint(Color.Lerp(baseTint, Color.white, 0.6f));
        telegraphing = true;   // v7.8

        bool parried = false;
        bool attempted = false;
        float t = 0f;

        while (t < teleSec)
        {
            t += Time.deltaTime;
            if (isGroggy || !IsAlive) { telegraphing = false; ApplyTint(baseTint); yield break; }

            bool inWindow = (teleSec - t) <= GameBalance.ParryWindowSec;

            // 패링 창이 열리면 조리 미니게임을 잠시 대기시켜 Space를 빌려온다
            if (inWindow && CookingMinigame.Instance != null)
                CookingMinigame.Instance.HoldFor(0.2f);

            if (!attempted && Input.GetKeyDown(KeyCode.Space))
            {
                attempted = true;
                if (inWindow)
                {
                    parried = true;
                    ParryCharges++;
                    SoundManager.Play("sfx_parry");
                    UIManager.Instance?.ShowStatChange("패링! 번개를 병에 담았다 ("
                        + ParryCharges + "/" + GameBalance.ParryChargesForCounter + ")");
                    Debug.Log("[BossEnemy] 번개 병 패링 성공! 충전 " + ParryCharges);
                }
                else
                {
                    UIManager.Instance?.ShowStatChange("너무 빨랐다! 번개가 병을 비껴갔다...");
                }
            }

            yield return null;
        }

        telegraphing = false;
        ApplyTint(armorActive ? ArmorTint() : baseTint);
        if (isGroggy || !IsAlive) yield break;

        // 패링 성공 -> 낙뢰 무효. 3병이면 되쏘기(강제 그로기)
        if (parried)
        {
            if (ParryCharges >= GameBalance.ParryChargesForCounter)
            {
                ParryCharges = 0;
                BossNoticeAtGroggy("되쏘기!", "병에 담은 번개가 여왕을 꿰뚫는다 - 무방비!");   // v7.9: 띠는 무방비 안내가 쓴다
                Debug.Log("[BossEnemy] 번개 되쏘기 - 강제 그로기!");
                ForceGroggy(GameBalance.ParryCounterGroggySec);
            }
            yield break;
        }

        if (TurretSlotManager.Instance == null) yield break;

        // 마비 후보: 가동 중(비어있지 않고, 잠금 아니고, 이미 마비 아님)
        TurretSlot[] slots = TurretSlotManager.Instance.slots;
        System.Collections.Generic.List<TurretSlot> candidates =
            new System.Collections.Generic.List<TurretSlot>();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null || slots[i].IsEmpty || slots[i].isLocked || slots[i].IsStunned) continue;
            candidates.Add(slots[i]);
        }

        int hitCount = 0;
        int strikeCount = GameBalance.LightningSlotCount + (enraged ? GameBalance.EnrageExtraLightning : 0);
        for (int n = 0; n < strikeCount && candidates.Count > 0; n++)
        {
            int idx = Random.Range(0, candidates.Count);
            candidates[idx].StunSlot(GameBalance.LightningStunSec);
            candidates.RemoveAt(idx);
            hitCount++;
        }

        if (hitCount > 0)
            UIManager.Instance?.ShowStatChange("포탑 " + hitCount + "기 감전! 포탑 곁에서 [E] 한 번!");
        Debug.Log("[BossEnemy] 낙뢰 폭격 - 슬롯 " + hitCount + "곳 마비");
    }

    /// <summary>지역 3 - 갑주 재전개: 50% 이하에서 1회, 빙하 갑주를 다시 두른다</summary>
    private IEnumerator PatternRearmor()
    {
        // 갑주가 이미 있거나 재전개를 썼으면 이번 사이클은 조용히 넘어간다
        if (armorActive || secondArmorUsed || currentHP / bossMaxHP > 0.5f)
            yield break;

        yield return StartCoroutine(Telegraph("냉기가 다시 뭉친다 - 갑주 재전개!"));
        if (isGroggy || !IsAlive) yield break;

        secondArmorUsed = true;
        ActivateArmor();
    }

    /// <summary>최종 - 포효: 정예 증원 소환</summary>
    private IEnumerator PatternRoar()
    {
        yield return StartCoroutine(Telegraph("포효! 대륙이 울린다!"));
        if (isGroggy || !IsAlive) yield break;

        int roarCount = GameBalance.OriginalRoarCount + (enraged ? GameBalance.EnrageExtraSummon : 0);
        if (waveManagerRef != null)
            waveManagerRef.SpawnReinforcements("raptor", roarCount, 0.8f);
        Debug.Log("[BossEnemy] 포효 - 증원 " + roarCount + "마리");
    }

    // ─────────────────────────────────────────────
    // 빙하 갑주 (동면자)
    // ─────────────────────────────────────────────
    private void ActivateArmor()
    {
        armorActive = true;
        armorDR = GameBalance.GlacierArmorDR;   // v5: 현재 감쇄율 (해동포 GOOD으로 절반 가능)
        burnBaseline = TotalBurnApplied;
        ApplyTint(ArmorTint());
        UIManager.Instance?.ShowStatChange("[빙하 갑주] 받는 피해 -"
            + Mathf.RoundToInt(GameBalance.GlacierArmorDR * 100f) + "%! 화염으로 녹여라!");
        Debug.Log("[BossEnemy] 빙하 갑주 전개 (화상 " + GameBalance.GlacierBreakBurnStacks + "스택으로 파괴)");
    }

    private void BreakArmor()
    {
        armorActive = false;
        ApplyTint(baseTint);
        UIManager.Instance?.ShowStatChange("빙하 갑주 파괴! 보스 무방비!");
        Debug.Log("[BossEnemy] 빙하 갑주 파괴 - 보너스 그로기 " + GameBalance.GlacierBreakGroggySec + "초");

        // 파괴 보상: 짧은 보너스 그로기
        ForceGroggy(GameBalance.GlacierBreakGroggySec);
    }

    private Color ArmorTint()
    {
        return new Color(0.45f, 0.95f, 1f);   // 갑주 중엔 얼음빛 강조
    }

    /// <summary>강제 그로기 (갑주 파괴 보상 / 추후 패링 반격 등에서 사용)</summary>
    public void ForceGroggy(float seconds)
    {
        if (isGroggy || !IsAlive) return;
        StartCoroutine(EnterGroggyState(seconds));
    }

    // ─────────────────────────────────────────────
    // 갑주 데미지 감쇄 (Enemy 훅 오버라이드)
    // 도트(화상/독)는 이 훅을 거치지 않는다 - 화염 도트가 갑주 파훼 수단
    // ─────────────────────────────────────────────
    protected override float ModifyIncomingDamage(float damage, DamageType dtype)
    {
        if (armorActive)
            return damage * (1f - armorDR);

        // v6: 해치 개방 (디 오리지널 P3) - 받는 피해 증가
        if (hatchOpen)
            return damage * GameBalance.HatchDamageTakenMul;

        return damage;
    }

    // ─────────────────────────────────────────────
    // v5: 해동포 (ThawCannonUI가 호출)
    // quality: 2=PERFECT(정중앙) / 1=GOOD(존 안) / 0=MISS(존 밖)
    // ─────────────────────────────────────────────
    public void HitByThawCannon(int quality)
    {
        if (!IsAlive) return;

        if (quality >= 2)
        {
            // 정중앙: 갑주 즉시 전파괴(보너스 그로기 포함) + 대미지
            if (armorActive) BreakArmor();
            TakeDamage(GameBalance.ThawPerfectDamage, DamageType.Magic);
            UIManager.Instance?.ShowStatChange("해동포 직격! 갑주가 산산조각났다!");
        }
        else if (quality == 1)
        {
            // 존 안: 갑주 감쇄율 절반 + 중간 대미지
            if (armorActive)
            {
                armorDR *= 0.5f;
                UIManager.Instance?.ShowStatChange("해동포 명중! 갑주 감쇄율 절반!");
            }
            TakeDamage(GameBalance.ThawGoodDamage, DamageType.Magic);
        }
        else
        {
            // 빗맞음: 대미지만
            TakeDamage(GameBalance.ThawMissDamage, DamageType.Magic);
            UIManager.Instance?.ShowStatChange("해동포 빗맞음...");
        }

        Debug.Log("[BossEnemy] 해동포 피격 (품질 " + quality + ")");
    }

    // ─────────────────────────────────────────────
    // 돌진 공격 (v3 유지)
    // ─────────────────────────────────────────────
    private IEnumerator AttackLunge()
    {
        isLunging = true;

        Vector3 startPos = transform.position;
        Vector3 dir = (CurrentTargetPos - startPos).normalized;   // v7.8: 중심이 아니라 눈앞의 몸통으로
        Vector3 peakPos = startPos + dir * 1.2f;

        float t = 0f;
        while (t < 0.16f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, peakPos, t / 0.16f);
            yield return null;
        }

        // P1 게임필: 런지 착지 임팩트 (기차 피격 셰이크와 별개의 육중함 - 절반 강도)
        GameFeel.Shake(GameBalance.ShakeBoss * 0.5f);

        // v6: 도발 중이면 미끼를 물어뜯는다 (기차 무피해)
        if (!IsTaunted)
        {
            AttackTrain();
            Debug.Log("[BossEnemy] 기차 공격! -" + (int)scaledATK);
        }
        else
        {
            Debug.Log("[BossEnemy] 미끼를 물어뜯는 중!");
        }

        t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(peakPos, startPos, t / 0.3f);
            yield return null;
        }
        transform.position = startPos;

        isLunging = false;
    }

    // ─────────────────────────────────────────────
    // 그로기 (v3 유지 + 지속 시간 파라미터화)
    // ─────────────────────────────────────────────
    private void CheckGroggyThresholds()
    {
        float hpRatio = currentHP / bossMaxHP;
        for (int i = 0; i < groggyThresholds.Length; i++)
        {
            if (!groggyTriggered[i] && hpRatio <= groggyThresholds[i])
            {
                groggyTriggered[i] = true;
                StartCoroutine(EnterGroggyState(groggyDuration));
                return;
            }
        }

        // v7.1 (C3): 디 오리지널 - 마지막 그로기(25%) 뒤 한 번 더 (기본 12%)
        if (kind == BossKind.Original && !extraGroggyTriggered && !isGroggy
            && GameBalance.OriginalExtraGroggyRatio > 0f
            && hpRatio <= GameBalance.OriginalExtraGroggyRatio)
        {
            extraGroggyTriggered = true;
            BossNoticeAtGroggy("[디 오리지널] 마지막 틈", "손님이 다시 식탁 앞에 무릎을 꿇었다 - 마지막 기회");   // v7.9
            StartCoroutine(EnterGroggyState(groggyDuration));
        }
    }

    private IEnumerator EnterGroggyState(float duration)
    {
        isGroggy = true;
        CurrentGroggyDuration = duration;
        Debug.Log("[BossEnemy] !! 보스 그로기 !! F키로 디버프 요리 투척! (" + duration + "초)");

        // P1 게임필: 그로기 진입 = 히트스톱 + 강한 셰이크 (거체가 무너지는 순간)
        GameFeel.Hitstop(GameBalance.HitstopBossGroggy);
        GameFeel.Shake(GameBalance.ShakeBoss);

        yield return new WaitForSeconds(duration);

        isGroggy = false;
        CurrentGroggyDuration = 0f;
        groggyLockUntil = Time.time + groggyCooldownGap;

        defense = baseDefenseValue;
        resistance = baseResistanceValue;
        Debug.Log("[BossEnemy] 보스 그로기 종료 - 방어력 복구");
    }

    public void ReceiveDebuffFood(float reductionMultiplier = 0.5f)
    {
        if (!isGroggy)
        {
            Debug.Log("[BossEnemy] 그로기 상태가 아니어서 디버프 요리 무효!");
            return;
        }

        defense = baseDefenseValue * reductionMultiplier;
        resistance = baseResistanceValue * reductionMultiplier;
        Debug.Log("[BossEnemy] 디버프 요리 적중! DEF/RES " +
                  ((1f - reductionMultiplier) * 100f).ToString("F0") + "% 감소! (" +
                  (int)defense + "/" + (int)resistance + ")");
    }

    // ─────────────────────────────────────────────
    // v7 (C-2): 마지막 식사 - 풀코스 QTE 성공 시 (FinalOrderUI가 호출)
    // 격파가 아니라 "대접"으로 끝나는 진엔딩 경로
    // ─────────────────────────────────────────────
    /// <summary>v7.5: 마지막 식사 장면 중 (TurretSlot.TickFire 가 사격을 멈춘다 - "공격을 멈추고 음식을 건넨다")</summary>
    public static bool LastSupperServing = false;

    public void ServeLastSupper()
    {
        if (!IsAlive || isServing) return;

        isServing = true;
        isGroggy = false;   // 그로기 해제 (연출 우선)
        StopAllCoroutines();   // 패턴/그로기 코루틴 정리

        // 흡수 참조 정리
        if (PickupFX.FeedingBoss == this) PickupFX.FeedingBoss = null;

        Debug.Log("[BossEnemy] 마지막 식사 - 디 오리지널이 정찬을 받았다");
        StartCoroutine(LastSupperRoutine());
    }

    /// <summary>
    /// v7.5 (스토리 개정 5절 "식사 성공"): 격파와 다른 결과를 화면으로 - 포탑이 멈추고, 남은 손님이 물러나고, 접시가 건너가고,
    /// 보스가 천천히 씹고(따뜻한 틴트 + 느린 들썩임), 두 대가 기적을 울린 뒤 엔딩 B 글이 뜬다. LastSupperChewSec 0 = 장면 없이 바로 글
    /// </summary>
    private IEnumerator LastSupperRoutine()
    {
        LastSupperServing = true;
        float chew = GameBalance.LastSupperChewSec;
        if (chew > 0f)
        {
            // 남은 손님은 물러난다 (보상 없이 사라짐 - 식탁 앞에서 싸우지 않는다)
            Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i] != this && all[i].IsAlive && !(all[i] is BossEnemy)) Destroy(all[i].gameObject);

            BossNotice("[디 오리지널] 포탑이 멈췄다. 접시가 건너간다.", "");   // v7.9: 씹는 모습을 가리지 않게 띠로
            SoundManager.Play("sfx_pickup");
            WorldFeel.Ring(transform.position, new Color(1f, 0.85f, 0.5f), 1.6f, 0.6f);
            ApplyTint(Color.Lerp(baseTint, new Color(1f, 0.9f, 0.7f), 0.7f));   // 핏빛이 가라앉는다

            // 천천히 씹는다 - 느린 들썩임
            Vector3 baseScale = transform.localScale;
            float t = 0f;
            while (t < chew)
            {
                t += Time.deltaTime;
                float k = 1f + 0.025f * Mathf.Sin(t * Mathf.PI * 2f / 0.9f);
                transform.localScale = new Vector3(baseScale.x * k, baseScale.y * (2f - k), baseScale.z);
                yield return null;
            }
            transform.localScale = baseScale;

            // 두 대의 기적 - 네 기차, 그리고 1호 (v7.6: 1호는 낮은 기적)
            EndingWhistled = true;   // v7.7
            SoundManager.Play("sfx_train_whistle");
            yield return new WaitForSeconds(0.9f);
            SoundManager.Play("sfx_whistle_low", 0.9f, 0f);
            GameFeel.Shake(GameBalance.ShakeBoss * 0.4f);
            yield return new WaitForSeconds(0.7f);
        }
        else { SoundManager.Play("sfx_train_whistle"); EndingWhistled = true; }

        // 엔딩 B 연출 -> 닫히면 기록 + 처치 처리 (웨이브 클리어 -> Victory로 이어짐)
        StoryTexts.ShowEndingB(delegate
        {
            MetaProgress.RecordEndingB();
            LastSupperServing = false;
            Die();   // 보상 지급 + 웨이브 클리어 체인 (최종전 -> Victory)
        });
    }

    // ─────────────────────────────────────────────
    // 연출 헬퍼
    // ─────────────────────────────────────────────
    private void ApplyTint(Color c)
    {
        if (sprites == null) return;
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) sprites[i].color = c;
    }

    // ─────────────────────────────────────────────
    // 사망
    // ─────────────────────────────────────────────
    protected override void Die()
    {
        if (glow != null) glow.enabled = false;   // v7.8: 상태 발광은 여기까지 (죽는 과정은 HitFeelBody 가 그림을 흐린다)
        // v7.4: 예습 보스 - 연출만, 보상·베팅·"승리" 체인 없음 (디렉터가 완료를 판정한다)
        if (practice)
        {
            GameFeel.Shake(GameBalance.ShakeBoss * 0.5f);
            GameFeel.DeathPop(transform.position, new Color(1f, 0.85f, 0.4f), 2f);
            base.Die();
            BossGimmickSystem.Instance?.ClearBossUI();
            return;
        }

        if (isServing)
        {
            // v7.7: 식사 엔딩 - 대접받은 손님이 폭발하듯 죽지 않는다. 히트스톱·흔들림·처치음·킬 버스트 없이 흐려진다 (보상과 웨이브 클리어 체인은 그대로)
            quietDeath = true;
        }
        else
        {
            // P1 게임필: 보스 처치 = 가장 긴 히트스톱 + 강한 셰이크 + 금색 대형 팝
            GameFeel.Hitstop(GameBalance.HitstopBossKill);
            GameFeel.Shake(GameBalance.ShakeBoss);
            GameFeel.DeathPop(transform.position, new Color(1f, 0.85f, 0.4f), 3f);
            // v7.7 (A11): 히트스톱 뒤 짧은 슬로모션 + 줌 당김 - 마지막 일격의 여운 (보스만)
            if (GameBalance.BossKillSlowMoOn)
                GameFeel.SlowMo(GameBalance.BossKillSlowScale, GameBalance.BossKillSlowSec, GameBalance.BossKillRecoverSec, GameBalance.BossKillZoom);
        }

        // 보스는 전 재료 2개씩 지급 (base.Die()가 심장 매핑 1개도 추가로 줌)
        if (MaterialInventory.Instance != null)
        {
            foreach (MaterialType t in System.Enum.GetValues(typeof(MaterialType)))
                MaterialInventory.Instance.Add(t, 2);
            Debug.Log("[BossEnemy] 보스 처치 보상: 전 재료 2개씩 지급!");
        }

        // v5: 미사용 번개 병은 전기 재료로 환급 (패링 보상 = 식재료 수확)
        if (ParryCharges > 0 && MaterialInventory.Instance != null)
        {
            MaterialInventory.Instance.Add(MaterialType.Elec, ParryCharges);
            UIManager.Instance?.ShowStatChange("번개 병 " + ParryCharges + "개 -> 전기알로 환급!");
            ParryCharges = 0;
        }

        // v7.5: 격파 엔딩 - 디 오리지널이 멈추면 기적 한 번 + "철길이 열렸다" (식사 엔딩의 두 번과 구분). 식사 뒤의 Die 는 조용히
        if (kind == BossKind.Original && !isServing && GameBalance.OriginalDefeatWhistle)
        {
            SoundManager.Play("sfx_train_whistle");
            EndingWhistled = true;   // v7.7
            UIManager.Instance?.ShowWaveNotice("[디 오리지널] 멈췄다 - 철길이 열렸다", "종착역까지 남은 열차는 네 것뿐이다");
        }

        base.Die();
        BossGimmickSystem.Instance?.OnBossDefeated();
    }

    // v7.5: 어떤 이유로든 사라질 때 마지막 식사 플래그 정리 (씬 전환 등)
    private void OnDisable()
    {
        if (isServing) LastSupperServing = false;
    }

    // v6: 어떤 이유로든 사라질 때 폭식 참조 정리 (씬 전환/사망)
    private void OnDestroy()
    {
        if (PickupFX.FeedingBoss == this)
            PickupFX.FeedingBoss = null;
    }

    public bool IsGroggy => isGroggy;
}
