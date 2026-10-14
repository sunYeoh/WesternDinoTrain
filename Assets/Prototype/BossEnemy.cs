using System.Collections;
using UnityEngine;

/// <summary>
/// [BossEnemy.cs] v7.14 (v9.23 2026-10-14 보스 새 패턴: partial 로 나눴다 - 새 패턴은 BossPatterns.cs (같은 클래스의 나머지 반쪽). 여기서 바뀐 것: 패턴 고르기(PickPattern) / 기본 공격 고르기(StartBasicAttack - 동면자 꼬리·디 오리지널 측면 충돌·천둥 둥지 없음) / 등장 전조에 부관 둘(SpawnLieutenants) / 호령이 편성 조각으로(R3) / 디 오리지널 페이즈마다 앞뒤로(OnOriginalPhase) / 몸 배율 poseScale·고개 lookOffset·붉은 열 heatGlow / 갑주가 생길 때 해동포 창 깜빡임 / 해동포가 눈사태를 멈춘다 / 쓰러질 때 남은 손님이 달아난다(RoutMinions)·디 오리지널은 칸이 떨어져 나간 뒤에(CollapseThenDie) / 보스전 글 말투를 튜토리얼과 같게 (반말 해체, 사건은 ~했다)) / v7.13 (v9.21.2 2026-10-14 문구 팩: 용어 통일 - 접시->요리(개) / 데미지->피해 / 슬롯->칸 / 랜덤->무작위 / 레시피->요리 / 적·아군->손님 / 드랍->드롭 / 마법 저항->저항 / 힐러·러시·청구 / 진화 조리 판 이름) / v7.12 (v9.21 2026-10-12 보스 1단계 큰 몸 - 보스개편 스펙 1·4·5절: 그림을 목표 크기(GameBalance.BossBodyLen x Wide)에 맞춘다(SetupSkin, 불투명 부분을 잰다 OpaqueSize) / 정식 보스는 서는 자리(BossStandX, 북쪽 BossStandOff)에 등장 연출이 놓고 제자리에 선다 - 걸어오지 않는다, 미끼에 끌려도 제자리 / 천둥 둥지는 상공에서 기차를 내려다본다(270도) + 땅 그림자 / 등장 연출 2판 EntranceV2: 전조 -> 보스전 카메라(CameraZoom.BossFraming) -> 보스별 길(EnterRust 떨어져 내림 · EnterThunder 날개 그림자와 번쩍 · EnterHibernator 땅 금과 떠오름 · EnterOriginal 평행 선로와 추월) -> 도착(Arrive) -> 이름판 -> HP 바 -> 밝아짐. 그동안 공격·패턴 없음 / 범위 판정은 몸 가장자리까지(DistanceFrom) / 쓰러지면 카메라·평행 선로 복귀) / v7.11 (v9.20 2026-10-07 유저 "패턴이 눈에 안 띔" - 스펙 3-0: 패턴의 준비(몸이 부푼다 + 보스 울음 + 패턴별 표시) -> 발동(눌렸다 돌아온다 + 충격 고리) -> 결과가 화면에서 보인다. 호령·포효 = 입에서 퍼지는 소리 고리 + 무리가 보스 등 뒤에서 / 낙뢰 = 맞을 포탑을 미리 정해 예고 줄과 발밑 고리(PickBoltTargets·BossMark.Bolt) + 번개 줄기(BoltStrike) + 패링하면 병으로(BoltsToBottle) / 갑주 = 모이는 냉기·서리 원·껍질 고리 / 물기 = 웅크림(그사이 무방비가 되면 물지 않는다. 공격 간격은 그대로) + 문 자리의 불똥·칸 튐·이빨 자국(BiteFx). GameBalance.BossPatternFxOn) / v7.10 (v9.19.2 2026-10-07: 보스 HP 에 종류별 배율 GameBalance.BossHPKindMul (예습 보스 제외) - 뒤 지역 보스가 제 웨이브의 큰 손님 한 마리보다 약했다. 폭식 회복·해동포 피해도 같은 배율 hpScale 을 따라간다) / v7.9 (v9.19 2026-10-06 보스 페이즈 모습 - A1 상태 그림: _rage_groggy(발악 중 무방비)·_p2_groggy 추가, 디 오리지널은 페이즈 그림이 먼저(WantedSkin, GameBalance.BossStateSkins) / A2 전환 순간(PhaseShift): 히트스톱 -> 흰 번쩍 아래에서 그림 교체 -> 장갑 파편·링·폭음 -> 몸이 커졌다 제자리. 지역 보스 발악과 디 오리지널 P2·P3 / A3 상태 유지: 발악 = 김·불티, 무방비 = 불똥·연기 + 몸이 기울어 흔들린다(SkinPivot) / A4 알림 자리: 보스 쪽 알림은 HP 바 밑 띠로(BossNotice), 발악의 화면 가운데 큰 글자는 가장자리 맥동으로) /
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
public partial class BossEnemy : Enemy
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
    /// <summary>v7.10: 이 보스에 걸린 HP 배율 (GameBalance.BossHPKindMul, 예습 보스는 1). HP 에 고정값으로 맞춰 둔 것(폭식 회복·해동포 피해)에 같이 곱한다</summary>
    private float hpScale = 1f;
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

    // ── v7.11 (v9.20): 패턴이 화면에서 보이게 ──
    private float castAge = -1f;        // 준비(예고)가 시작된 뒤 흐른 시간 (음수 = 준비 중 아님)
    private float castMulNow = 1f;      // 지금 몸 배율: 준비 = 부푼다 / 발동 = 눌렸다 돌아온다 / 물기 전 = 웅크린다 (TickCastScale)
    private float crouchNow = 0f;       // 물기 전 웅크림 (0 ~ 1, AttackLunge 가 건다)
    private Vector3 lungeHome;          // 물기를 시작한 자리 (물다가 끊기면 여기로 돌아간다)
    private float castFxTimer = 0f;     // 다음 준비 표시(소리 고리·냉기 조각)까지
    private readonly System.Collections.Generic.List<TurretSlot> boltTargets = new System.Collections.Generic.List<TurretSlot>();   // 낙뢰: 이번에 맞을 포탑 (준비가 시작될 때 정한다)
    private readonly System.Collections.Generic.List<BossMark> boltMarks = new System.Collections.Generic.List<BossMark>();          // 그 포탑 위의 예고 줄
    private BossMark castDisc;          // 갑주 재전개: 바닥의 서리 (준비가 끊기면 걷는다)
    private Transform chefTf;           // 패링한 번개가 들어갈 자리 (셰프)

    // ── v7.12 (v9.21): 큰 몸·서는 자리·등장 연출 2판 ──
    private bool entering = false;          // 등장 연출 중 (이동·공격·패턴 없음. 맞기는 한다)
    private Vector3 standPos;               // 서는 자리 (GameBalance.BossStandX, 기차 y + BossStandOff[종류])
    private Vector2 bodyExtLocal;           // 그림의 불투명 부분 반 크기 (그림 좌표, 배율 전) - DistanceFrom
    private float landAge = -1f;            // 도착 순간의 몸 펀치가 시작된 뒤 흐른 시간 (음수 = 없음)
    private float enterAlpha = 1f;          // 등장 중 그림 알파 (동면자가 떠오를 때)
    private Vector2 enterScale = Vector2.one;   // 등장 중 SkinPivot 에 곱하는 배율 (떨어져 내림·떠오름·날갯짓)
    private float eyesAge = -1f;            // 디 오리지널: 머리가 나란해진 순간 몸이 붉게 (새 그림이 오면 눈만)
    private Vector2 bodyCenterLocal;        // 그림 불투명 부분의 가운데가 피벗에서 벗어난 양 (그림 좌표) - DistanceFrom
    private float enterClock = 0f;          // 등장 연출이 흐른 시간 (실시간, 메뉴가 떠 있는 동안은 멈춤 - 안전장치용: 너무 오래 끌면 Update 가 끝낸다)
    private float enterTotal = 3f;
    private SpriteRenderer shadow;          // 천둥 둥지: 땅 그림자 (이름이 Flash_ 라 HitFeelBody 가 몸으로 세지 않는다)
    private static readonly System.Collections.Generic.Dictionary<string, Vector4> opaqueCache = new System.Collections.Generic.Dictionary<string, Vector4>();
    private static readonly Color DUST = new Color(0.80f, 0.66f, 0.48f, 0.75f);
    private static readonly Color ICE_LINE = new Color(0.86f, 0.96f, 1f, 1f);
    private static readonly Color SHADOW = new Color(0.04f, 0.03f, 0.08f, 0.42f);
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
            intro = "무리의 왕이 나타났다. 무리를 부를 때 마비시키면 랩터가 절반만 와";
        }
        else if (kind == BossKind.ThunderNest)
        {
            data.enemyName = "천둥 둥지";
            bossMaxHP *= 0.95f; spd = 1.5f; bossAttackRange = 6.5f; // 멀리서 때림
            baseTint = new Color(0.72f, 0.72f, 1f);
            intro = "선대의 번개 병이 기차에 실려 있어. 낙뢰가 떨어지기 직전에 [Space]로 병을 내밀어";
        }
        else if (kind == BossKind.Hibernator)
        {
            data.enemyName = "동면자";
            bossMaxHP *= 1.15f; spd = 1.25f;                      // 느리고 단단함
            baseTint = new Color(0.6f, 0.85f, 1f);
            intro = "고대 모사야. 갑주는 화염으로만 녹아. 광산의 해동포에 화염을 넣고 쏴";
        }
        else
        {
            data.enemyName = "디 오리지널";
            bossMaxHP *= 1.2f; bossATK *= 1.1f; spd = 1.4f;
            baseTint = new Color(1f, 0.5f, 0.45f);
            intro = "낡은 기적이 울린다 - 급식 열차 1호였던 것이 식탁에 앉았다.";   // v7.5: 기차였다는 흔적 (일지 3 의 기적)
        }

        // v7.10: 종류별 HP 배율 (정식 보스만). 공식(BossHPBase + 웨이브 x BossHPPerWave)만으로는 뒤 지역 보스가 큰 손님 한 마리보다 약했다
        hpScale = 1f;
        if (!practice)
        {
            float[] kindMul = GameBalance.BossHPKindMul;
            int kindIdx = (int)kind;
            if (kindMul != null && kindIdx >= 0 && kindIdx < kindMul.Length && kindMul[kindIdx] > 0f) hpScale = kindMul[kindIdx];
            bossMaxHP *= hpScale;
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
            intro = "녹슨 발톱의 새끼야. 미끼 화덕으로 무리부터 꾀어내";
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
        SetupNewPatterns();   // v7.14 (v9.23): 부관이 오는 만큼 왕 HP / 측면 충돌 간격 / 서리 초기화 (BossPatterns.cs)

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
        // v7.12: 서는 자리. 정식 보스는 여기에 등장 연출이 놓는다 (WaveManager 는 이 자리 위 BossHideDY 에 숨겨 두고 Start 를 부른다)
        float trainY = trainTarget != null ? trainTarget.position.y : 0f;
        standPos = new Vector3(GameBalance.BossStandX, trainY + attackRange, 0f);
        if (StandMode)
        {
            faceAngle = StandAngle();
            transform.rotation = Quaternion.AngleAxis(faceAngle, Vector3.forward);
            SetVisible(false);
        }
        // v7.7 (D2): 등장 연출 - 예습 보스와 연출 끔은 예전처럼 바로 포효
        if (!practice && GameBalance.BossEntranceOn && GameBalance.GameFeelMaster > 0f)
        {
            BossGimmickSystem.Instance?.HoldBarForIntro();   // v7.8: HP 바는 포효 순간에 내려와 차오른다
            StartCoroutine(StandMode ? EntranceV2(intro) : EntranceRoutine(intro));
        }
        else
        {
            if (StandMode) PlaceAtStand();   // 연출 없이 바로 자리에
            AnnounceEntrance(intro);
            if (NewPatterns)   // v7.14: 연출을 끈 판에서도 부관·북쪽 막기는 그대로
            {
                if (kind == BossKind.RustClaw && GameBalance.RustLieutenantsOn) SpawnLieutenants();
                OnEntranceEnd();
            }
        }

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
            // v7.12: 2판에서는 띠 밑 "[이름] 대응법" 줄을 뺀다 (스펙 5절 - 첫 등장 카드와 이름판이면 충분하다). GameBalance.BossEntranceLine 으로 되살린다
            if (!StandMode || GameBalance.BossEntranceLine)
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
        // v7.12: 정식 보스는 그림을 목표 크기(길이 x 폭, u)에 맞춘다 - 길이·폭 중 먼저 닿는 쪽까지. 그림의 투명 여백은 빼고 잰다 (OpaqueSize).
        //   그 크기로 그린 새 그림(32px = 1u)은 저절로 1배가 되고, 지금 그림은 늘어난다 (천둥 둥지는 정사각이라 폭 5.2 에서 멈춘다)
        Vector2 opaque = OpaqueSize(s);
        bodyExtLocal = opaque * 0.5f;
        bodyCenterLocal = OpaqueCenter(s);
        if (GameBalance.BossFitBody && !practice && GameBalance.BossBodyLen != null && k < GameBalance.BossBodyLen.Length)
        {
            float tl = GameBalance.BossBodyLen[k];
            float tw = (GameBalance.BossBodyWide != null && k < GameBalance.BossBodyWide.Length) ? GameBalance.BossBodyWide[k] : tl * 0.4f;
            want = Mathf.Min(tl / Mathf.Max(0.05f, opaque.x), tw / Mathf.Max(0.05f, opaque.y));
        }
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
        // v7.12: 천둥 둥지는 떠 있다 - 땅에 그림자 (같은 그림을 검게, 기차 쪽으로 BossShadowDY 내려서. 보스를 따라 돌고 상태 그림도 따라간다 - TickSkin)
        if (!practice && GameBalance.BossEntranceV2 && kind == BossKind.ThunderNest && GameBalance.BossThunderHover)
        {
            GameObject sh = new GameObject("Flash_BossShadow");
            sh.transform.SetParent(transform, false);
            shadow = sh.AddComponent<SpriteRenderer>();
            shadow.sprite = s;
            shadow.color = SHADOW;
            shadow.sortingOrder = -9;   // 땅(-10) 위, 서리 원(-7)·데크(-6) 아래 - 기차 위로는 안 올라온다
            shadow.transform.localScale = new Vector3(local, local, 1f);
        }
        Debug.Log("[BossEnemy] 그림 " + skinBase + " 적용 (배율 " + want.ToString("0.00") + " = " + (opaque.x * want).ToString("0.0") + " x " + (opaque.y * want).ToString("0.0") + "u)");
        return true;
    }

    /// <summary>v7.12: 그림의 불투명 부분 크기 (가로, 세로 - 그림 좌표 u, 배율 전). 흰 실루엣(FlashSprites)의 알파를 읽는다 - 원본 텍스처는 읽기 금지일 수 있다. 못 읽으면 사각형 전체</summary>
    private static Vector2 OpaqueSize(Sprite s)
    {
        Vector4 r = OpaqueRect(s);
        return new Vector2(r.x, r.y);
    }

    /// <summary>v7.12: 불투명 부분의 가운데가 그림 피벗에서 벗어난 양 (그림 좌표 u). 유저 그림은 가운데에 그려져 있어 0 에 가깝다</summary>
    private static Vector2 OpaqueCenter(Sprite s)
    {
        Vector4 r = OpaqueRect(s);
        return new Vector2(r.z, r.w);
    }

    /// <summary>(가로, 세로, 가운데 x 오프셋, 가운데 y 오프셋) - 그림 이름으로 한 번만 잰다</summary>
    private static Vector4 OpaqueRect(Sprite s)
    {
        Vector4 full = new Vector4(s.bounds.size.x, s.bounds.size.y, 0f, 0f);
        string key = s.name;
        Vector4 got;
        if (opaqueCache.TryGetValue(key, out got)) return got;
        try
        {
            Sprite w = FlashSprites.Get(s);
            if (w == null || w.texture == null) { opaqueCache[key] = full; return full; }
            Color32[] px = w.texture.GetPixels32();
            int tw = w.texture.width;
            int x0 = (int)s.rect.x, y0 = (int)s.rect.y, rw = (int)s.rect.width, rh = (int)s.rect.height;
            int minX = rw, maxX = -1, minY = rh, maxY = -1;
            for (int y = 0; y < rh; y++)
                for (int x = 0; x < rw; x++)
                {
                    int i = (y0 + y) * tw + x0 + x;
                    if (i < 0 || i >= px.Length || px[i].a < 10) continue;
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (y < minY) minY = y; if (y > maxY) maxY = y;
                }
            if (maxX < 0) { opaqueCache[key] = full; return full; }
            float ppu = Mathf.Max(1f, s.pixelsPerUnit);
            got = new Vector4((maxX - minX + 1) / ppu, (maxY - minY + 1) / ppu,
                ((minX + maxX + 1) * 0.5f - s.pivot.x) / ppu, ((minY + maxY + 1) * 0.5f - s.pivot.y) / ppu);
        }
        catch (System.Exception) { got = full; }
        opaqueCache[key] = got;
        return got;
    }

    /// <summary>v7.12: 범위 판정이 쓰는 거리 - 몸(돌아간 사각형, 불투명 부분)의 가장자리까지. 그림이 없으면 중심까지</summary>
    public override float DistanceFrom(Vector3 p)
    {
        if (skin == null || skin.sprite == null || bodyExtLocal.x <= 0.01f) return base.DistanceFrom(p);
        Vector3 l = skin.transform.InverseTransformPoint(p);   // 그림 좌표 (배율·회전 전)
        float dx = Mathf.Max(0f, Mathf.Abs(l.x - bodyCenterLocal.x) - bodyExtLocal.x), dy = Mathf.Max(0f, Mathf.Abs(l.y - bodyCenterLocal.y) - bodyExtLocal.y);
        return Mathf.Sqrt(dx * dx + dy * dy) * Mathf.Abs(skin.transform.lossyScale.x);
    }

    // ─────────────────────────────────────────────
    // v7.12: 서는 자리 (정식 보스는 걸어오지 않는다)
    // ─────────────────────────────────────────────
    /// <summary>제자리 모드: 정식 보스 + 등장 연출 2판. 예습 보스와 구 모드는 걸어와서 돈다</summary>
    private bool StandMode { get { return !practice && GameBalance.BossEntranceV2; } }

    /// <summary>서 있을 때 몸이 보는 각도: 기차가 달리는 쪽(180). 천둥 둥지는 상공에서 기차를 내려다본다(270 - 날개가 좌우로)</summary>
    private float StandAngle()
    {
        return (StandMode && kind == BossKind.ThunderNest && GameBalance.BossThunderHover) ? 270f : 180f;
    }

    /// <summary>그림(과 그림자)을 켜고 끈다 - 등장 전에는 숨긴다. 그림이 없는(프리팹 사각형) 보스는 그대로 보인다</summary>
    private void SetVisible(bool on)
    {
        if (skin != null) skin.enabled = on;
        if (shadow != null) shadow.enabled = on;
        if (!on && glow != null) glow.enabled = false;
    }

    /// <summary>서는 자리에 바로 놓는다 (연출 없이)</summary>
    private void PlaceAtStand()
    {
        transform.position = standPos;
        faceAngle = StandAngle();
        transform.rotation = Quaternion.AngleAxis(faceAngle, Vector3.forward);
        SetVisible(true);
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
        TickCastScale();   // v7.11: 패턴 준비·발동·웅크림의 몸 배율 (그림이 없어도 시간은 센다)
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
        // v7.12: 도착 순간의 펀치 (BossArrivePunch 만큼 커졌다가 0.25초에 제자리 - 전환 순간과 같은 곡선, 흰 번쩍은 없다)
        float land = 0f;
        if (landAge >= 0f)
        {
            landAge += Time.deltaTime;   // 게임 시간 - 히트스톱이 풀린 뒤에 터지고, 일시정지면 선다
            float k = 1f - Mathf.Clamp01(landAge / SHIFT_PUNCH_SEC);
            land = k * k * GameBalance.BossArrivePunch;
            if (landAge >= SHIFT_PUNCH_SEC) landAge = -1f;
        }

        if (skinPivot != null)
        {
            float tiltMax = Mathf.Max(0f, GameBalance.BossGroggyTiltDeg);
            float tiltWant = (isGroggy && !isServing && GameBalance.BossStateFx) ? tiltMax : 0f;
            tiltNow = Mathf.MoveTowards(tiltNow, tiltWant, 40f * Time.deltaTime);
            float sway = tiltMax > 0.01f ? Mathf.Sin(Time.time * 1.8f) * 1.5f * (tiltNow / tiltMax) : 0f;
            float sc = (1f + GameBalance.BossPhaseShiftPunch * punch + land) * castMulNow;   // v7.11: x 패턴 준비·발동 배율 / v7.12: + 도착 펀치
            skinPivot.localScale = new Vector3(sc * enterScale.x * poseScale.x, sc * enterScale.y * poseScale.y, 1f);     // v7.12: x 등장 중 배율 (떨어져 내림·떠오름·날갯짓) / v7.14: x 패턴 자세 (날갯짓·날개 접기·뛰어오름)
            skinPivot.localRotation = Quaternion.Euler(0f, 0f, tiltNow + sway);
        }
        // v7.12: 등장 중 알파 (동면자가 떠오를 때) - 연출이 끝나면 1 로 돌려놓는다 (EndEntrance)
        if (enterAlpha < 1f) { Color sa = skin.color; sa.a = enterAlpha; skin.color = sa; }
        // v7.12: 천둥 둥지의 땅 그림자 - 보스를 따라 돌고, 상태 그림·몸 배율을 따라간다. 기차 쪽(아래)으로 BossShadowDY
        if (shadow != null)
        {
            if (shadow.sprite != skin.sprite) shadow.sprite = skin.sprite;
            shadow.transform.position = transform.position + Vector3.down * GameBalance.BossShadowDY;
            shadow.transform.rotation = skin.transform.rotation;
            shadow.transform.localScale = skin.transform.lossyScale;
        }

        if (glow == null) return;
        Color c = Color.clear;
        float t = Time.time;
        if (flash > 0f) c = new Color(1f, 1f, 1f, flash);
        else if (eyesAge >= 0f)
        {
            // v7.12: 디 오리지널 도착 - 머리가 나란해진 순간 몸이 붉게 달아올랐다 식는다 (0.4초. 새 그림이 오면 눈에만)
            eyesAge += Time.deltaTime;
            c = new Color(1f, 0.12f, 0.08f, 0.6f * (1f - Mathf.Clamp01(eyesAge / 0.4f)));
            if (eyesAge >= 0.4f) eyesAge = -1f;
        }
        else if (isServing) c = Color.clear;
        else if (isGroggy) c = new Color(1f, 0.85f, 0.35f, 0.22f + 0.1f * Mathf.Sin(t * 5f));
        else if (heatGlow > 0.01f) c = new Color(1f, 0.22f, 0.08f, 0.5f * heatGlow + 0.08f * Mathf.Sin(t * 20f) * heatGlow);   // v7.14 O4: 측면 충돌 예고 - 차체가 붉게
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

    /// <summary>
    /// v7.11: 패턴 표시 한 개. 스스로 사라진다 (보스가 먼저 쓰러져도 남지 않는다).
    ///   Bolt = 맞을 자리 위에 서는 세로 예고 줄 + 발밑에서 조여 드는 고리 - 준비 시간 동안 점점 굵고 진해지고, 마지막 blinkSec(패링 창) 동안 깜빡인다
    ///   Disc = 바닥에 깔려 커지는 원 (냉기가 뭉치는 자리)
    ///   Claw = 문 자리의 이빨 자국 세 줄 (금방 흐려진다)
    ///   v7.12: Streak = 짧은 줄이 한 방향으로 날아가며 흐려진다 (날갯짓 바람) / Crack = 땅의 금 - 한쪽 끝에서 반대쪽으로 달리며 끝에서 얼음 조각이 튄다 / DiscAt = 자리가 고정된 Disc (떨어져 내릴 자리의 그림자)
    /// </summary>
    private class BossMark : MonoBehaviour
    {
        private int mode;                     // 0 = Bolt / 1 = Disc / 2 = Claw / 3 = Streak / 4 = Crack
        private float age, life, blinkSec;
        private Color col;
        private Transform follow;             // 따라갈 자리 (Bolt = 포탑 / Disc = 보스)
        private Vector3 basePos;
        private float fromRadius, toRadius;   // Disc
        private float aspect = 1f;            // Disc: 세로/가로
        private LineRenderer[] lines;
        private SpriteRenderer disc;
        private Vector3 vel;                  // Streak: 속도 (u/초) / Crack: 끝점
        private float growSec, sparkT;        // Crack: 달리는 시간 / 다음 얼음 조각까지
        private Vector3[] path;               // Crack: 금의 꺾인 점들
        private const int CRACK_PTS = 28;
        private const int RING_SEG = 20;      // Bolt 의 발밑 고리를 이루는 선분 수
        private static Material lineMat;      // 줄 표시가 같이 쓰는 재질 (표시마다 새로 만들면 오브젝트가 사라져도 재질이 남는다)

        private static LineRenderer MakeLine(Transform parent, Color c, float width, int order)
        {
            GameObject go = new GameObject("Line");
            go.transform.SetParent(parent, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            if (lineMat == null) lineMat = new Material(Shader.Find("Sprites/Default"));
            lr.sharedMaterial = lineMat;
            lr.startColor = c; lr.endColor = c;
            lr.startWidth = width; lr.endWidth = width;
            lr.positionCount = 2;
            lr.sortingOrder = order;
            return lr;
        }

        public static BossMark Bolt(Transform target, Color c, float sec, float blink)
        {
            GameObject go = new GameObject("BossMark_Bolt");
            BossMark m = go.AddComponent<BossMark>();
            m.mode = 0; m.life = Mathf.Max(0.1f, sec); m.blinkSec = Mathf.Max(0f, blink); m.col = c;
            m.follow = target; m.basePos = target != null ? target.position : Vector3.zero;
            // [0] = 화면 위에서 내려오는 세로 줄 / [1] = 발밑 고리 (어느 포탑이 맞는지 - 줄만으로는 위아래 포탑이 헷갈린다)
            m.lines = new LineRenderer[] { MakeLine(go.transform, c, 0.04f, 56), MakeLine(go.transform, c, 0.05f, 56) };
            m.lines[1].positionCount = RING_SEG + 1;
            m.Draw();
            return m;
        }

        /// <summary>패링 성공: 깜빡임을 멈추고 병 색으로 바뀐다 (잡았다는 표시 - 떨어질 때 줄기가 병으로 간다)</summary>
        public void Caught(Color c) { col = c; blinkSec = 0f; }

        public static BossMark Disc(Transform center, Color c, float fromRadius, float toRadius, float sec, float aspect = 1f)
        {
            GameObject go = new GameObject("BossMark_Disc");
            Vector3 pos = center != null ? center.position : Vector3.zero;
            go.transform.position = pos;
            BossMark m = go.AddComponent<BossMark>();
            m.mode = 1; m.life = Mathf.Max(0.1f, sec); m.col = c; m.basePos = pos; m.follow = center;
            m.fromRadius = fromRadius; m.toRadius = toRadius; m.aspect = Mathf.Max(0.05f, aspect);   // v7.12: 세로/가로 (1 = 원, 작으면 긴 몸을 따라가는 타원 - 따라가는 것의 회전을 따른다)
            m.disc = go.AddComponent<SpriteRenderer>();
            m.disc.sprite = AttackVFX.GetCircleSprite();   // 지름 1u
            m.disc.sortingOrder = -7;                      // 땅(-10) 위, 기차 데크(-6) 아래 - 서리는 땅에만 깔린다 (지붕을 덮으면 기차가 어는 것처럼 읽힌다)
            m.Draw();
            return m;
        }

        /// <summary>v7.12: 자리가 고정된 Disc (보스를 따라가지 않는다) - 떨어져 내릴 자리에 커지는 그림자</summary>
        public static BossMark DiscAt(Vector3 pos, Color c, float fromRadius, float toRadius, float sec)
        {
            BossMark m = Disc(null, c, fromRadius, toRadius, sec);
            m.transform.position = pos; m.basePos = pos;
            m.Draw();
            return m;
        }

        /// <summary>v7.12: 바람 줄무늬 - pos 에서 dir 로 travel 만큼 sec 동안 날아가며 흐려진다</summary>
        public static BossMark Streak(Vector3 pos, Vector3 dir, float travel, float sec, Color c)
        {
            GameObject go = new GameObject("BossMark_Streak");
            go.transform.position = pos;
            BossMark m = go.AddComponent<BossMark>();
            m.mode = 3; m.life = Mathf.Max(0.1f, sec); m.col = c; m.basePos = pos;
            Vector3 d = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.down;
            m.vel = d * (travel / m.life);
            m.lines = new LineRenderer[] { MakeLine(go.transform, c, 0.06f, 40) };
            m.lines[0].SetPosition(0, pos); m.lines[0].SetPosition(1, pos + d * 1.1f);
            return m;
        }

        /// <summary>v7.12: 땅의 금 - from 에서 to 까지 growSec 에 달리고(끝에서 얼음 조각) sec 뒤에 사라진다 (마지막 0.5초에 흐려진다). 땅 위·데크 아래 층</summary>
        public static BossMark Crack(Vector3 from, Vector3 to, Color c, float growSec, float sec)
        {
            GameObject go = new GameObject("BossMark_Crack");
            go.transform.position = from;
            BossMark m = go.AddComponent<BossMark>();
            m.mode = 4; m.life = Mathf.Max(growSec + 0.2f, sec); m.col = c; m.basePos = from; m.vel = to;
            m.growSec = Mathf.Max(0.05f, growSec);
            Vector3 along = to - from;
            Vector3 side = new Vector3(-along.y, along.x, 0f).normalized;
            m.path = new Vector3[CRACK_PTS];
            for (int i = 0; i < CRACK_PTS; i++)
            {
                float k = i / (float)(CRACK_PTS - 1);
                float jit = (i == 0 || i == CRACK_PTS - 1) ? 0f : Random.Range(-0.35f, 0.35f);
                m.path[i] = from + along * k + side * jit;
            }
            m.lines = new LineRenderer[] { MakeLine(go.transform, c, 0.13f, -8), MakeLine(go.transform, new Color(0.1f, 0.2f, 0.3f, 0.8f), 0.22f, -9) };   // 밝은 금 + 그 밑의 어두운 틈
            m.lines[0].positionCount = 2; m.lines[1].positionCount = 2;
            m.lines[0].SetPosition(0, from); m.lines[0].SetPosition(1, from);
            m.lines[1].SetPosition(0, from); m.lines[1].SetPosition(1, from);
            return m;
        }

        public static BossMark Claw(Vector3 pos, Vector2 dir, float sec)
        {
            if (sec <= 0.01f) return null;
            GameObject go = new GameObject("BossMark_Claw");
            go.transform.position = pos;
            BossMark m = go.AddComponent<BossMark>();
            m.mode = 2; m.life = sec; m.col = new Color(1f, 0.96f, 0.85f); m.basePos = pos;
            Vector3 along = dir.sqrMagnitude > 0.0001f ? new Vector3(dir.x, dir.y, 0f).normalized : Vector3.down;
            Vector3 side = new Vector3(-along.y, along.x, 0f);
            Vector3 slant = (along + side * 0.45f).normalized;   // 비스듬히 긁힌 자국
            m.lines = new LineRenderer[3];
            for (int i = 0; i < 3; i++)
            {
                m.lines[i] = MakeLine(go.transform, m.col, 0.08f, 12);
                Vector3 c = pos + side * ((i - 1) * 0.28f);
                m.lines[i].SetPosition(0, c - slant * 0.45f);
                m.lines[i].SetPosition(1, c + slant * 0.45f);
            }
            return m;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= life) { Destroy(gameObject); return; }
            Draw();
        }

        private void Draw()
        {
            float k = Mathf.Clamp01(age / life);
            if (mode == 0)
            {
                Vector3 p = follow != null ? follow.position : basePos;
                lines[0].SetPosition(0, new Vector3(p.x, SkyY(p.y), 0f));
                lines[0].SetPosition(1, p);
                float w = Mathf.Lerp(0.04f, 0.14f, k);
                float a = Mathf.Lerp(0.3f, 0.9f, k);
                if (life - age <= blinkSec) a *= 0.55f + 0.45f * Mathf.Sin(age * 40f);   // 패링 창: 깜빡인다
                Color c = col; c.a = a;
                lines[0].startWidth = w; lines[0].endWidth = w;
                lines[0].startColor = c; lines[0].endColor = c;
                // 발밑 고리: 0.95u 에서 0.55u 로 조여 든다 (이웃 포탑과 2u 떨어져 있어 서로 겹치지 않는다)
                float ringR = Mathf.Lerp(0.95f, 0.55f, k);
                for (int i = 0; i <= RING_SEG; i++)
                {
                    float ang = i * (Mathf.PI * 2f / RING_SEG);
                    lines[1].SetPosition(i, new Vector3(p.x + Mathf.Cos(ang) * ringR, p.y + Mathf.Sin(ang) * ringR, 0f));
                }
                float ringW = Mathf.Max(0.05f, w * 0.75f);
                lines[1].startWidth = ringW; lines[1].endWidth = ringW;
                lines[1].startColor = c; lines[1].endColor = c;
            }
            else if (mode == 1)
            {
                if (follow != null) { transform.position = follow.position; if (aspect < 0.999f) transform.rotation = follow.rotation; }   // 보스가 움직이면 같이 (타원은 몸을 따라 돈다)
                float r = Mathf.Lerp(fromRadius, toRadius, 1f - (1f - k) * (1f - k));
                transform.localScale = new Vector3(r * 2f, r * 2f * aspect, 1f);
                Color c = col; c.a = col.a * (k < 0.85f ? 1f : (1f - k) / 0.15f);
                disc.color = c;
            }
            else if (mode == 3)
            {
                Vector3 p = basePos + vel * age;
                Vector3 d = vel.sqrMagnitude > 0.0001f ? vel.normalized : Vector3.down;
                lines[0].SetPosition(0, p); lines[0].SetPosition(1, p + d * 1.1f);
                Color c = col; c.a = col.a * (1f - k * k);
                lines[0].startColor = c; lines[0].endColor = c;
            }
            else if (mode == 4)
            {
                float g = Mathf.Clamp01(age / growSec);
                int n = Mathf.Clamp(2 + Mathf.FloorToInt(g * (CRACK_PTS - 2)), 2, CRACK_PTS);
                for (int l = 0; l < lines.Length; l++)
                {
                    if (lines[l].positionCount != n) lines[l].positionCount = n;
                    for (int i = 0; i < n; i++) lines[l].SetPosition(i, path[i]);
                }
                if (g < 1f)
                {
                    sparkT -= Time.deltaTime;
                    if (sparkT <= 0f) { sparkT = 0.04f; SparkPool.Emit(path[n - 1], col, 2, 0.08f, 3f); }
                }
                float fade = Mathf.Clamp01((life - age) / 0.5f);
                Color c = col; c.a = col.a * fade;
                lines[0].startColor = c; lines[0].endColor = c;
                Color c2 = lines[1].startColor; c2.a = 0.8f * fade;
                lines[1].startColor = c2; lines[1].endColor = c2;
            }
            else
            {
                Color c = col; c.a = 1f - k * k;
                for (int i = 0; i < lines.Length; i++) { lines[i].startColor = c; lines[i].endColor = c; }
            }
        }
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
    // v7.12 (v9.21): 등장 연출 2판 (스펙 4절) - 네 보스가 같은 박자, 들어오는 길만 다르다
    //   0.0 전조 (배경음 줄고 어두워짐 + 보스별) -> 0.5 카메라가 빠진다 -> 0.9 들어오는 길 -> 도착 (히트스톱 + 흔들림 + 먼지 고리 + 포효 + 몸 1.12배)
    //   -> +0.3 이름판 -> +0.6 HP 바 -> 밝아지고 패턴 시계 시작. 실시간으로 돈다 (게임은 멈추지 않는다). 그동안 보스는 공격하지 않는다 (맞기는 한다)
    // ─────────────────────────────────────────────
    /// <summary>등장 연출의 한 프레임 시간: 실시간이되 카드·일시정지·증강 창이 떠 있는 동안은 0 (연출이 메뉴 뒤에서 혼자 진행되지 않게)</summary>
    private static float EnterDt
    {
        get
        {
            if (BriefingUI.IsOpen || StoryTexts.IsBlocking || AugmentPickUI.IsOpen || PauseMenu.IsOpen) return 0f;
            return Time.unscaledDeltaTime;
        }
    }

    /// <summary>sec 초(실시간, 메뉴가 떠 있는 동안은 멈춤) 기다린다</summary>
    private IEnumerator WaitRT(float sec)
    {
        float t = 0f;
        while (t < sec) { t += EnterDt; yield return null; }
    }

    /// <summary>등장 중 배율·알파를 지금 바로 그림에 쓴다 - 코루틴은 Update(TickSkin) 뒤에 돌아서, 보이게 하는 프레임에 TickSkin 값(배율 1)으로 한 번 그려지는 것을 막는다</summary>
    private void ApplyEnterPose()
    {
        if (skinPivot != null) skinPivot.localScale = new Vector3(enterScale.x * castMulNow, enterScale.y * castMulNow, 1f);
        if (skin != null && enterAlpha < 1f) { Color sa = skin.color; sa.a = enterAlpha; skin.color = sa; }
    }

    private IEnumerator EntranceV2(string intro)
    {
        entering = true;
        enterClock = 0f;
        yield return null;   // WaveManager.SpawnBoss 가 같은 프레임에 여는 첫 등장 카드가 열릴 틈
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
        if (!IsAlive) yield break;
        BossGimmickSystem.Instance?.HoldBarForIntro();   // 카드를 오래 읽었어도 8초 안전장치가 지금부터 세게 (바가 카드 뒤에서 먼저 뜨지 않게)

        int k = Mathf.Clamp((int)kind, 0, 3);
        float total = (GameBalance.BossEntranceSec != null && k < GameBalance.BossEntranceSec.Length) ? GameBalance.BossEntranceSec[k] : 3f;
        enterTotal = total; enterClock = 0f;   // enterClock 은 Update 가 메뉴가 안 떠 있는 동안만 센다

        // 0.0 전조: 어두워진다 (밝아지는 건 total 에 맞춰) + 보스별
        float dimA = kind == BossKind.ThunderNest ? GameBalance.BossEntranceDim + 0.15f : GameBalance.BossEntranceDim;
        ScreenFx.WorldDim(dimA, 0.35f, Mathf.Max(0.1f, total - 0.35f - 0.5f), 0.5f);
        if (kind == BossKind.RustClaw)
        {
            SoundManager.Play("sfx_boss_far_howl", 0.9f, -1f);
            StartCoroutine(DustPillars());
            if (NewPatterns && GameBalance.RustLieutenantsOn) SpawnLieutenants();   // v7.14 R1: 부관 둘이 좌우에서 먼저 달려온다
        }
        else if (kind == BossKind.ThunderNest) SoundManager.Play("sfx_thunder_roll", 0.9f, -1f);
        else if (kind == BossKind.Hibernator)
        {
            WarningFX.FrostEdge(1.8f, new Color(0.78f, 0.92f, 1f));
            StartCoroutine(RattleTrain(0.8f));
        }
        else
        {
            SoundManager.BgmCut(0.4f);
            SoundManager.PlayDelayed("sfx_whistle_low", 0.4f);
        }
        yield return WaitRT(0.5f);
        if (!IsAlive) yield break;

        // 0.5 카메라가 빠진다
        if (GameBalance.BossCamOn && GameBalance.BossCamZoom != null && k < GameBalance.BossCamZoom.Length)
            CameraZoom.BossFraming(GameBalance.BossCamZoom[k], GameBalance.BossCamInSec);
        yield return WaitRT(0.4f);
        if (!IsAlive) yield break;

        // 0.9 들어오는 길 (끝나면 보스가 서는 자리에 있다)
        if (kind == BossKind.RustClaw) yield return StartCoroutine(EnterRust());
        else if (kind == BossKind.ThunderNest) yield return StartCoroutine(EnterThunder());
        else if (kind == BossKind.Hibernator) yield return StartCoroutine(EnterHibernator());
        else yield return StartCoroutine(EnterOriginal());
        if (!IsAlive) yield break;

        // 도착
        Arrive(kind == BossKind.Original ? GameBalance.BossEntranceHitstop + 0.02f : GameBalance.BossEntranceHitstop);
        yield return WaitRT(0.3f);
        if (!IsAlive) yield break;

        // 이름판
        float band = GameBalance.BossIntroBandSec > 0f ? GameBalance.BossNamePlateSec + 0.38f : 0f;
        if (band > 0f) WarningFX.BossIntro(data.enemyName, Epithet(), band);
        if (GameBalance.BossEntranceLine) StartCoroutine(NoticeAfter(Mathf.Max(0.5f, band - 0.2f), "[" + data.enemyName + "]", intro));
        yield return WaitRT(0.3f);
        if (!IsAlive) yield break;

        // HP 바가 내려와 차오른다 (래칫은 BossGimmickSystem)
        BossGimmickSystem.Instance?.PlayBarIntro();

        // 밝아질 때까지 남은 시간 - 그다음 패턴 시계 시작
        float left = total - enterClock;
        if (left > 0f) yield return WaitRT(left);
        if (!IsAlive) yield break;
        EndEntrance();
    }

    /// <summary>등장 연출 끝: 몸 배율·알파를 제자리로, 패턴·공격 시계 시작</summary>
    private void EndEntrance()
    {
        entering = false;
        enterScale = Vector2.one; enterAlpha = 1f;
        if (skin != null) { Color c = skin.color; c.a = 1f; skin.color = c; }
        SetVisible(true);
        transform.position = standPos;
        patternTimer = GameBalance.BossPatternFirstDelay;
        attackTimer = 0f;
        OnEntranceEnd();   // v7.14: 디 오리지널 북쪽 막기 / 천둥 둥지 자리 옮기기 시계
    }

    /// <summary>도착: 히트스톱 + 흔들림 + 먼지 고리 + 포효 + 몸 펀치 (landAge -> TickSkin). 디 오리지널은 포효 대신 쇠 비명 + 몸이 붉게</summary>
    private void Arrive(float hitstop)
    {
        transform.position = standPos;
        SetVisible(true);
        enterScale = Vector2.one;
        GameFeel.Hitstop(Mathf.Max(0f, hitstop));
        GameFeel.Shake(GameBalance.BossEntranceShake);
        landAge = 0f;
        float half = FxRadius();
        if (kind == BossKind.ThunderNest)
        {
            WorldFeel.Ring(transform.position, new Color(0.9f, 0.95f, 1f, 0.55f), half * 0.9f, 0.3f);   // 바람
        }
        else if (kind == BossKind.Hibernator)
        {
            WorldFeel.Ring(transform.position, new Color(0.8f, 0.95f, 1f, 0.8f), 9f, 0.6f);               // 냉기 고리가 기차를 훑는다
            SoundManager.Play("sfx_freeze", 0.8f, -1f);
        }
        else
        {
            WorldFeel.Ring(transform.position, DUST, half * 0.9f, 0.35f);
            for (int i = 0; i < 4; i++)
                DustFX.Puff(transform.position + new Vector3(Random.Range(-half, half), Random.Range(-0.6f, 0.6f) - 0.5f, 0f), Random.Range(1.0f, 1.5f));
        }
        if (kind == BossKind.Original)
        {
            eyesAge = 0f;
            SoundManager.Play("sfx_iron_scream", 1f, -1f);
            SoundManager.PlayDelayed(SoundKeys.BossRoar(kind.ToString()), 0.25f);
        }
        else SoundManager.Play(SoundKeys.BossRoar(kind.ToString()), 1f, -1f);
    }

    /// <summary>E1 전조: 화면 세 가장자리(꼬리 쪽·위·아래) 안쪽에 먼지 기둥이 솟는다 - 무리가 사방에서 몰려온다는 신호 (부관 둘은 v9.22 에서 여기에 더한다)</summary>
    private IEnumerator DustPillars()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;
        if (statePuffs == null) statePuffs = StatePuffs.Create(this);
        float hh = cam.orthographicSize, hw = hh * cam.aspect;
        Vector3 c = cam.transform.position;
        Vector3[] at =
        {
            new Vector3(c.x + hw - 2.5f, c.y + Random.Range(-2f, 2f), 0f),                 // 꼬리 쪽 (오른쪽)
            new Vector3(c.x + Random.Range(-6f, 6f), c.y + hh - 2.2f, 0f),                 // 위
            new Vector3(c.x + Random.Range(-6f, 6f), c.y - hh + hh * (190f / 450f) + 1.2f, 0f)   // 아래 (하단 HUD 위)
        };
        for (int step = 0; step < 4; step++)
        {
            for (int i = 0; i < at.Length; i++)
                statePuffs.Emit(at[i] + new Vector3(Random.Range(-0.4f, 0.4f), step * 0.35f, 0f), DUST, Random.Range(1.0f, 1.4f), 0.9f, new Vector3(0f, 2.2f, 0f));
            yield return WaitRT(0.12f);
        }
    }

    /// <summary>E3 전조: 기차가 두 배로 덜컹인다 - sec 동안 칸을 번갈아 걷어찬다 (TrainDeck.KickCar, 그림만)</summary>
    private IEnumerator RattleTrain(float sec)
    {
        float[] edges = GameBalance.CarEdgesX;
        float t = 0f; int n = 0;
        while (t < sec)
        {
            int car = n % (edges.Length - 1);
            float x = (edges[car] + edges[car + 1]) * 0.5f;
            TrainDeck.KickCar(x, (n % 2 == 0) ? Vector2.up : Vector2.down, 0.09f);
            n++;
            yield return WaitRT(0.1f);
            t += 0.1f;
        }
    }

    /// <summary>E1: 땅에 그림자 원이 커진다(뛰어오른 것) -> 왕이 떨어져 내린다 (크게 보이다 제 크기로, 위에서 내려온다) -> 착지: 먼지 고리 두 겹 + 쿵</summary>
    private IEnumerator EnterRust()
    {
        BossMark.DiscAt(standPos, new Color(0f, 0f, 0f, 0.45f), 0.4f, FxRadius() * 0.6f, 0.65f);
        yield return WaitRT(0.12f);
        if (!IsAlive) yield break;
        enterScale = new Vector2(1.9f, 1.9f); ApplyEnterPose();
        transform.position = standPos + Vector3.up * 2.5f;
        SetVisible(true);
        const float FALL = 0.5f;
        float t = 0f;
        while (t < FALL)
        {
            t += EnterDt;
            float k = Mathf.Clamp01(t / FALL);
            float e = k * k;   // 떨어지며 빨라진다
            float s = Mathf.Lerp(1.9f, 1f, e);
            enterScale = new Vector2(s, s); ApplyEnterPose();
            transform.position = standPos + Vector3.up * Mathf.Lerp(2.5f, 0f, e);
            yield return null;
            if (!IsAlive) yield break;
        }
        transform.position = standPos; enterScale = Vector2.one;
        SoundManager.Play("sfx_boss_land", 1f, -1f);
        WorldFeel.Ring(standPos, DUST, FxRadius() * 1.3f, 0.45f);   // 두 겹 중 바깥 (안쪽은 Arrive 의 고리)
    }

    /// <summary>E2: 거대한 날개 그림자가 기차를 꼬리에서 머리 쪽으로 훑는다 -> 흰 번쩍 -> 북쪽 상공에 날개를 편 여왕 -> 날갯짓 한 번에 바람 줄무늬</summary>
    private IEnumerator EnterThunder()
    {
        Camera cam = Camera.main;
        float hw = cam != null ? cam.orthographicSize * cam.aspect : 18f;
        float cx = cam != null ? cam.transform.position.x : standPos.x;
        // 날개 그림자: 보스 실루엣을 검게, 목표 날개폭의 1.3배로 (검은 실루엣이라 도트가 뭉개져도 안 보인다 - "거대한" 그림자)
        GameObject sh = null;
        Sprite sil = skin != null ? FlashSprites.Get(skin.sprite) : null;
        if (sil != null)
        {
            sh = new GameObject("BossWingShadow");
            SpriteRenderer sr = sh.AddComponent<SpriteRenderer>();
            sr.sprite = sil; sr.color = new Color(0f, 0f, 0f, 0.5f); sr.sortingOrder = 40;   // 셰프·포탑·손님 위, 표시(56)·HUD 아래
            float wing = GameBalance.BossBodyWide != null && GameBalance.BossBodyWide.Length > 1 ? GameBalance.BossBodyWide[1] : 12f;
            float sc = wing * 1.3f / Mathf.Max(0.5f, OpaqueSize(skin.sprite).y);
            sh.transform.localScale = new Vector3(sc, sc, 1f);
            sh.transform.rotation = Quaternion.AngleAxis(270f, Vector3.forward);
            Destroy(sh, 3f);   // 보험 - 보스가 그사이 사라져도 그림자가 남지 않는다
        }
        const float SWEEP = 0.6f;
        float t = 0f;
        float y = (trainTarget != null ? trainTarget.position.y : 0f) + 0.6f;
        while (t < SWEEP)
        {
            t += EnterDt;
            float k = Mathf.Clamp01(t / SWEEP);
            if (sh != null) sh.transform.position = new Vector3(Mathf.Lerp(cx + hw + 7f, cx - hw - 7f, k), y, 0f);
            yield return null;
        }
        if (sh != null) Destroy(sh);
        if (!IsAlive) yield break;
        // 흰 번쩍 한 장 - 걷히면 여왕이 떠 있다
        bool covered = ScreenFx.Cover(Color.white, 0.04f, 0.3f, delegate
        {
            if (this == null || !IsAlive) return;   // 덮이는 0.04초 사이에 사라졌으면 아무것도 안 한다
            transform.position = standPos; SetVisible(true);
        });
        if (!covered) { transform.position = standPos; SetVisible(true); }
        yield return WaitRT(0.3f);   // 흰 번쩍이 거의 걷힌 뒤에 날갯짓 (덮개 = 0.04 + 0.3초)
        if (!IsAlive) yield break;
        // 날갯짓 한 번 (날개 쪽 = 그림의 세로 = enterScale.y) + 바람 줄무늬가 기차를 친다
        SoundManager.Play("sfx_wing_gust", 1f, -1f);
        for (int i = 0; i < 7; i++)
            BossMark.Streak(standPos + new Vector3(Random.Range(-5.5f, 5.5f), Random.Range(-3.2f, -2.2f), 0f), Vector3.down, 9f, 0.38f, new Color(1f, 1f, 1f, 0.5f));
        const float BEAT = 0.4f;
        t = 0f;
        while (t < BEAT)
        {
            t += EnterDt;
            float k = Mathf.Clamp01(t / BEAT);
            enterScale = new Vector2(1f, 1f - 0.18f * Mathf.Sin(k * Mathf.PI)); ApplyEnterPose();
            yield return null;
        }
        enterScale = Vector2.one; ApplyEnterPose();
    }

    /// <summary>E3: 기차 북쪽 땅에 금이 머리 쪽에서 꼬리 쪽으로 달린다 (얼음 조각이 차례로 튄다) -> 금이 벌어지며 등줄기부터 떠오른다 -> 냉기 고리 (Arrive)</summary>
    private IEnumerator EnterHibernator()
    {
        SoundManager.Play("sfx_ground_crack", 0.9f, -1f);
        float half = GameBalance.BossBodyLen != null && GameBalance.BossBodyLen.Length > 2 ? GameBalance.BossBodyLen[2] * 0.5f : 7f;
        BossMark.Crack(standPos + Vector3.left * half, standPos + Vector3.right * half, ICE_LINE, 0.5f, 1.6f);
        yield return WaitRT(0.5f);
        if (!IsAlive) yield break;
        SoundManager.Play("sfx_iron_groan", 0.9f, -1f);
        transform.position = standPos;
        enterScale = new Vector2(1f, 0.15f); enterAlpha = 0.45f; ApplyEnterPose();
        SetVisible(true);
        const float RISE = 0.45f;
        float t = 0f;
        while (t < RISE)
        {
            t += EnterDt;
            float k = Mathf.Clamp01(t / RISE);
            float e = 1f - (1f - k) * (1f - k);
            enterScale = new Vector2(1f, Mathf.Lerp(0.15f, 1f, e));
            enterAlpha = Mathf.Lerp(0.45f, 1f, e);
            ApplyEnterPose();
            if (Random.value < 0.5f) SparkPool.Emit(RandomBodyPoint(), ICE_LINE, 2, 0.08f, 3f);
            yield return null;
            if (!IsAlive) yield break;
        }
        enterScale = Vector2.one; enterAlpha = 1f;
        if (skin != null) { Color c = skin.color; c.a = 1f; skin.color = c; }
    }

    /// <summary>E4: 두 번째 선로가 깔린다 -> 붉은 전조등 빛이 화면을 가른다 -> 붉은 열차가 뒤(꼬리 쪽)에서 추월해 들어온다 (두 열차 사이 불티) -> 머리가 나란해지면 쇠 비명 + 몸이 붉게 (Arrive)</summary>
    private IEnumerator EnterOriginal()
    {
        ParallaxBackground.SideRail(standPos.y, 0.5f);
        yield return WaitRT(0.3f);
        if (!IsAlive) yield break;
        Camera cam = Camera.main;
        float hw = cam != null ? cam.orthographicSize * cam.aspect : 20f;
        float cx = cam != null ? cam.transform.position.x : standPos.x;
        // 전조등: 길고 얇은 붉은 빛이 오른쪽에서 왼쪽으로 화면을 가른다
        GameObject beam = new GameObject("BossHeadlight");
        SpriteRenderer br = beam.AddComponent<SpriteRenderer>();
        br.sprite = TrainDeck.GetWhiteSprite(); br.color = new Color(1f, 0.15f, 0.1f, 0.35f); br.sortingOrder = 40;
        beam.transform.localScale = new Vector3(hw * 1.1f, 1.1f, 1f);
        Destroy(beam, 3f);   // 보험
        SoundManager.Play("sfx_rival_wheels", 1f, -1f);
        const float BEAM = 0.4f, RUN = 1.1f;
        float t = 0f;
        while (t < BEAM)
        {
            t += EnterDt;
            float k = Mathf.Clamp01(t / BEAM);
            beam.transform.position = new Vector3(Mathf.Lerp(cx + hw * 1.6f, cx - hw * 1.6f, k), standPos.y + 0.2f, 0f);
            yield return null;
        }
        Destroy(beam);
        if (!IsAlive) yield break;
        // 추월: 화면 오른쪽 밖에서 서는 자리로 (빠르게 와서 끝에서 선다). 두 열차 사이에 불티
        float fromX = cx + hw + BodyHalfLength() + 2f;
        transform.position = new Vector3(fromX, standPos.y, 0f);
        SetVisible(true);
        float sparkT = 0f;
        float gapY = ((trainTarget != null ? trainTarget.position.y : 0f) + 1.9f + standPos.y - bodyExtLocal.y * Mathf.Abs(skin != null ? skin.transform.lossyScale.x : 1f)) * 0.5f;
        t = 0f;
        while (t < RUN)
        {
            t += EnterDt;
            float k = Mathf.Clamp01(t / RUN);
            float e = 1f - (1f - k) * (1f - k) * (1f - k);
            transform.position = new Vector3(Mathf.Lerp(fromX, standPos.x, e), standPos.y, 0f);
            sparkT -= EnterDt;
            if (sparkT <= 0f)
            {
                sparkT = 0.06f;
                float sx = Mathf.Clamp(transform.position.x + Random.Range(-BodyHalfLength(), BodyHalfLength()), GameBalance.CarEdgesX[0], GameBalance.CarEdgesX[GameBalance.CarEdgesX.Length - 1]);
                SparkPool.Emit(new Vector3(sx, gapY + Random.Range(-0.3f, 0.3f), 0f), EMBER[3], 3, 0.08f, 3.5f);
            }
            yield return null;
            if (!IsAlive) yield break;
        }
        transform.position = standPos;
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
        if (entering)
        {
            // v7.12: 등장 연출 중 - 이동·공격·패턴·발악·무방비 판정 없음 (맞기는 한다).
            // 안전장치: 연출 코루틴이 예외로 죽어 보스가 숨은 채 굳으면(사거리 밖이라 웨이브가 안 끝난다) 길이 + 4초 뒤에 자리에 놓고 끝낸다
            enterClock += EnterDt;
            if (enterClock > enterTotal + 4f)
            {
                Debug.LogWarning("[BossEnemy] 등장 연출이 끝나지 않았다 - 자리에 놓고 시작한다");
                StopAllCoroutines();
                PlaceAtStand(); EndEntrance();
            }
            return;
        }
        TickNewPatterns();   // v7.14 (v9.23): 천둥 둥지 날갯짓·자리 옮기기 / 눈사태 시작 / 측면 충돌 열 (BossPatterns.cs)

        // 빙하 갑주 파괴 판정 (화상 스택 누적 감시)
        if (armorActive && TotalBurnApplied - burnBaseline >= GameBalance.GlacierBreakBurnStacks)
            BreakArmor();

        // v5: 발악 페이즈 진입 (HP 50% 이하, 1회). v7.4: 예습 보스는 발악·무방비·패턴 없음 (돌진만)
        if (!practice && !enraged && currentHP / bossMaxHP <= GameBalance.EnrageHPRatio)
        {
            enraged = true;
            UIManager.Instance?.ShowStatChange("[" + data.enemyName + "] 발악. 패턴이 빨라진다");
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
        if (isGroggy || isLunging || isCasting || gliding) return;   // v7.14: 자리를 옮기는 동안도 (천둥 둥지 활공 · 디 오리지널 앞뒤로)

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
        // v7.12: 제자리 모드 - 등장 연출이 놓은 자리에 서서 물기만 한다 (큰 몸이 걸어 다니면 기차 위를 쓸고 지나간다). 미끼에 끌리면 제자리에서 미끼 쪽으로 문다 (BossHoldWhenTaunted)
        if (StandMode && (!IsTaunted || GameBalance.BossHoldWhenTaunted))
        {
            HoldStance();
            if (attackTimer >= attackCooldown)
            {
                attackTimer = 0f;
                StartBasicAttack();   // v7.14: 보스마다 기본 공격 (녹슨 발톱 물기 / 동면자 꼬리 / 디 오리지널 측면 충돌 / 천둥 둥지 없음)
            }
            return;
        }
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

    /// <summary>나란히 서는 자세를 쓸 때인가 (미끼에 끌려갈 때는 미끼를 본다 - v7.12: 제자리 모드에서는 끌려도 자세를 지킨다)</summary>
    private bool StanceOn { get { return GameBalance.BossFaceAlongTrain && (!IsTaunted || (StandMode && GameBalance.BossHoldWhenTaunted)); } }

    /// <summary>몸을 want 각도로 돌린다 (BossTurnDegPerSec 로 부드럽게)</summary>
    private void TurnBody(float want)
    {
        if (float.IsNaN(faceAngle)) faceAngle = want;
        faceAngle = Mathf.MoveTowardsAngle(faceAngle, want, GameBalance.BossTurnDegPerSec * Time.deltaTime);
        transform.rotation = Quaternion.AngleAxis(faceAngle, Vector3.forward);
    }

    /// <summary>사거리 안에 서 있는 동안: 기차가 달리는 쪽(왼쪽)을 본다 (v7.12: 천둥 둥지는 기차를 내려다본다 - StandAngle)</summary>
    private void HoldStance()
    {
        if (StanceOn && !isLunging) TurnBody(StandAngle() + lookOffset);   // v7.14: + 고개 (부관이 쓰러지면 쳐든다)
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
            BossNotice("[디 오리지널] 폭식",
                "놈이 원료 조각을 닥치는 대로 삼켜. 조각이 놈 옆에 있으면 회복하고 세져. 식사가 아니라 연료야");   // v7.5: 원료 섭취와 식사의 구분
            PhaseShift();                                                                            // v7.9 (A2): 장갑이 벗겨지는 순간
            OnOriginalPhase(2);                                                                      // v7.14: 3u 앞으로 치고 나간다 + 약점 칸 (BossPatterns.cs)
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
            BossNotice("[디 오리지널] 해치가 열려 기관심장이 드러났다",
                "받는 피해 +" + Mathf.RoundToInt((GameBalance.HatchDamageTakenMul - 1f) * 100f)
                + "%. 무방비 때 [F]로 격파하거나 [R]로 마지막 식사");   // v7.5: 왜 지금 식사가 가능한지
            PhaseShift();                                           // v7.9 (A2): 기관심장이 드러나는 순간
            OnOriginalPhase(3);                                     // v7.14: 3u 뒤로 처진다 + 약점 칸 (BossPatterns.cs)
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
            float heal = Mathf.Min(GameBalance.FeedHealPerFragment * hpScale, cap - feedHealAccum);   // v7.10: 조각당 회복도 HP 배율만큼
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

        // v7.14 (v9.23): 보스마다 패턴 순서 (BossPatterns.PickPattern). 새 패턴을 끄면 예전처럼 보스마다 하나
        yield return StartCoroutine(PickPattern());

        isCasting = false;

        // v5: 발악 시 패턴 간격 단축
        float interval = GameBalance.BossPatternInterval + Random.Range(-2f, 2f);
        if (enraged) interval *= GameBalance.EnragePatternIntervalMul;
        patternTimer = interval;
    }

    // ─────────────────────────────────────────────
    // v7.11 (v9.20): 패턴이 화면에서 보이게 - 준비 -> 발동 -> 결과 -> 되돌아옴 (스펙 3-0. 유저 "패턴이 눈에 안 띔")
    //   예전엔 예고가 전부 HUD(띠·가장자리 맥동·공통 경보음)였고 보스 몸은 하얗게 깜빡이기만 했다. 발동 순간도, 결과가 보스에게서 나오는 모습도 없었다
    // ─────────────────────────────────────────────
    /// <summary>패턴 연출을 쓰는가 (GameBalance.BossPatternFxOn + 손맛 전체 스위치)</summary>
    private bool PatternFx { get { return GameBalance.BossPatternFxOn && GameBalance.GameFeelMaster > 0f; } }

    /// <summary>몸 길이의 절반 (월드 유닛). 고리·표시의 크기를 여기에 맞춘다 - 보스 그림이 커지면 같이 커진다</summary>
    private float BodyHalfLength()
    {
        if (skin != null && skin.sprite != null) return skin.sprite.bounds.extents.x * Mathf.Abs(skin.transform.lossyScale.x);
        return 1.5f * Mathf.Abs(transform.lossyScale.x);
    }

    /// <summary>v7.12: 몸 폭의 절반 (월드 유닛, 불투명 부분 기준). 그림이 없으면 0.9u</summary>
    private float BodyHalfWidth()
    {
        if (skin != null && skin.sprite != null && bodyExtLocal.y > 0.01f) return bodyExtLocal.y * Mathf.Abs(skin.transform.lossyScale.x);
        return 0.9f * Mathf.Abs(transform.lossyScale.x);
    }

    /// <summary>v7.12: 고리·서리·모이는 냉기의 기준 반지름 - 몸 길이의 절반이되 몸 폭의 1.6배를 넘지 않는다.
    /// v9.20 은 길이의 절반만 썼는데 14 ~ 22u 몸에서는 고리가 기차와 HUD 를 다 덮었다 (px/boss_pattern_preview.py). 녹슨 발톱 2.9 / 천둥 둥지 2.6 / 동면자 3.5 / 디 오리지널 3.1</summary>
    private float FxRadius() { return Mathf.Min(BodyHalfLength(), BodyHalfWidth() * 1.6f); }

    /// <summary>입 자리 (몸 앞쪽 끝에서 조금 안). 그림은 오른쪽을 보고 그려져 있으니 몸이 보는 쪽 = transform.right</summary>
    private Vector3 MouthPos() { return transform.position + transform.right * (BodyHalfLength() * 0.75f); }

    /// <summary>준비 시작: 몸이 부풀기 시작하고(TickCastScale) 보스가 제 울음을 낸다 (네 보스 공통 경보음 대신)</summary>
    private void CastBegin(bool frostDisc = true)
    {
        castAge = 0f;
        castFxTimer = 0f;
        if (!PatternFx) return;
        SoundManager.Play(SoundKeys.BossRoar(kind.ToString()), 0.55f, -1f);
        // 갑주 재전개: 냉기가 뭉치는 자리 - 바닥의 서리가 준비 시간 동안 몸 크기까지 커진다
        // v7.14: 동면자의 새 패턴(입김·눈사태)은 frostDisc = false 로 부른다 - 서리 원은 갑주 재전개만
        if (kind == BossKind.Hibernator && frostDisc)
            castDisc = BossMark.Disc(transform, new Color(0.7f, 0.95f, 1f, 0.3f), 0.5f, BodyHalfLength() * 1.15f, GameBalance.BossTelegraphSec,
                BodyHalfWidth() * 1.7f / Mathf.Max(0.1f, BodyHalfLength() * 1.15f));   // v7.12: 긴 몸을 따라가는 타원 (몸보다 조금 넓게)
    }

    /// <summary>준비하는 동안 매 프레임: 패턴별 표시 (호령·포효 = 입에서 퍼지는 소리 고리 / 갑주 = 사방에서 몸으로 모이는 냉기)</summary>
    private void TickCastFx()
    {
        if (!PatternFx) return;
        castFxTimer -= Time.deltaTime;
        if (castFxTimer > 0f) return;
        int k = Mathf.Clamp((int)kind, 0, EMBER.Length - 1);
        if (kind == BossKind.Hibernator)
        {
            castFxTimer = 0.07f;
            float ang = Random.Range(0f, Mathf.PI * 2f);
            Vector3 dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
            float r = FxRadius() * 1.4f + 0.5f;   // v7.12: 몸 폭 기준
            if (statePuffs == null) statePuffs = StatePuffs.Create(this);
            statePuffs.Emit(transform.position + dir * r, STEAM[k], 0.5f, 0.5f, -dir * (r / 0.5f));   // 0.5초에 몸에 닿는다
        }
        else
        {
            castFxTimer = 0.5f;
            WorldFeel.Ring(MouthPos(), Color.Lerp(EMBER[k], Color.white, 0.5f), FxRadius() * 0.8f + 0.8f, 0.45f);   // v7.12: 몸 폭 기준
        }
    }

    /// <summary>
    /// 발동: 몸이 눌렸다 돌아오고(TickCastScale) 충격 고리가 퍼진다 + 흔들림 + 김 한 번 (되돌아옴).
    /// bodyRing = false 면 몸 둘레의 충격 고리를 뺀다 (갑주 재전개 - 같은 자리에 껍질 고리가 따로 퍼진다. 고리 둘이 겹치면 같은 말을 두 번 한다)
    /// </summary>
    private void CastRelease(bool bodyRing = true)
    {
        castAge = -1f;
        if (!PatternFx) return;
        castMulNow = 1f - Mathf.Clamp(GameBalance.BossReleaseSquash, 0f, 0.5f);
        int k = Mathf.Clamp((int)kind, 0, EMBER.Length - 1);
        if (bodyRing) WorldFeel.Ring(transform.position, Color.Lerp(EMBER[k], Color.white, 0.4f), FxRadius() * 1.6f, 0.3f);   // v7.12: 몸 폭 기준
        GameFeel.Shake(GameBalance.BossEntranceShake * GameBalance.BossReleaseShake);
        SoundManager.Play("sfx_explosion", 0.3f, 0.05f);
        if (statePuffs == null) statePuffs = StatePuffs.Create(this);
        Vector3 drift = new Vector3(ParallaxBackground.CurrentSpeed * 0.35f, 0.55f, 0f);
        statePuffs.Emit(MouthPos(), STEAM[k], 0.9f, 0.7f, drift);
    }

    /// <summary>준비가 끊겼다 (무방비·사망·마지막 식사) - 발동 없이 표시를 걷고 몸을 제 크기로 돌린다</summary>
    private void CastCancel()
    {
        castAge = -1f;
        if (castDisc != null) Destroy(castDisc.gameObject);
        castDisc = null;
        ClearBoltMarks();
        boltTargets.Clear();
    }

    /// <summary>
    /// 매 프레임(TickSkin): 몸 배율 - 준비 = 부푼다(처음에 빨리, 끝에서 천천히) / 발동 = 눌렸다 0.25초에 제자리 / 물기 전 = 웅크린다.
    /// 그림을 감싼 SkinPivot 의 크기에 곱한다 (전환 순간의 몸 크기와 같은 자리)
    /// </summary>
    private void TickCastScale()
    {
        // 안전망: 예고(telegraphing)가 끝났는데 준비가 남아 있으면 발동 없이 끊긴 것 - 부푼 채로 남지 않게 걷는다
        if (castAge >= 0f && !telegraphing) castAge = -1f;
        float want = 1f;
        if (castAge >= 0f)
        {
            castAge += Time.deltaTime;
            float k = Mathf.Clamp01(castAge / Mathf.Max(0.1f, GameBalance.BossTelegraphSec));
            want = 1f + GameBalance.BossCastSwell * (1f - (1f - k) * (1f - k));
        }
        want *= 1f - 0.06f * crouchNow;
        if (!PatternFx) { castMulNow = 1f; return; }
        if (castAge >= 0f && crouchNow <= 0f) { castMulNow = want; return; }   // 부푸는 곡선은 준비 시간이 만든다
        float back = Mathf.Max(0.05f, GameBalance.BossReleaseSquash) / 0.25f;  // 눌린 만큼을 0.25초에
        castMulNow = Mathf.MoveTowards(castMulNow, want, back * Time.deltaTime);
    }

    // ── 낙뢰 폭격의 표시 (천둥 둥지) ──
    private static readonly Color BOLT_WARN = new Color(1f, 0.95f, 0.55f);
    private static readonly Color BOLT_CORE = new Color(1f, 1f, 0.88f);
    private static readonly Color BOLT_EDGE = new Color(0.6f, 0.9f, 1f);

    /// <summary>
    /// 이번 낙뢰가 맞을 포탑을 정하고 그 위에 예고 줄을 세운다 (준비가 시작될 때).
    /// 예전엔 떨어지는 순간에 정해서 어디가 맞을지 미리 알 수 없었다 - 패링할지 말지를 감으로 골랐다.
    /// 후보 = 가동 중인 포탑 (비어 있지 않고, 잠금 아니고, 이미 마비 아님). 수 = LightningSlotCount (+ 발악 EnrageExtraLightning)
    /// </summary>
    private void PickBoltTargets()
    {
        ClearBoltMarks();
        boltTargets.Clear();
        if (TurretSlotManager.Instance == null) return;

        TurretSlot[] slots = TurretSlotManager.Instance.slots;
        System.Collections.Generic.List<TurretSlot> candidates = new System.Collections.Generic.List<TurretSlot>();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null || slots[i].IsEmpty || slots[i].isLocked || slots[i].IsStunned) continue;
            candidates.Add(slots[i]);
        }

        int strikeCount = GameBalance.LightningSlotCount + (enraged ? GameBalance.EnrageExtraLightning : 0);
        for (int n = 0; n < strikeCount && candidates.Count > 0; n++)
        {
            int idx = Random.Range(0, candidates.Count);
            boltTargets.Add(candidates[idx]);
            candidates.RemoveAt(idx);
        }

        if (!PatternFx) return;
        for (int n = 0; n < boltTargets.Count; n++)
            boltMarks.Add(BossMark.Bolt(boltTargets[n].transform, BOLT_WARN, GameBalance.BossTelegraphSec, GameBalance.ParryWindowSec));
    }

    private void ClearBoltMarks()
    {
        for (int i = 0; i < boltMarks.Count; i++)
            if (boltMarks[i] != null) Destroy(boltMarks[i].gameObject);
        boltMarks.Clear();
    }

    /// <summary>화면 위 끝보다 조금 높은 곳의 y (번개가 내려오는 자리)</summary>
    private static float SkyY(float fallbackY)
    {
        Camera cam = Camera.main;
        return cam != null ? cam.transform.position.y + cam.orthographicSize + 1f : fallbackY + 12f;
    }

    /// <summary>번개 줄기 하나가 pos 에 꽂힌다 - 겹친 두 줄기 + 고리 + 불똥 + 소리</summary>
    private void BoltStrike(Vector3 pos)
    {
        if (!PatternFx) return;
        Vector3 from = new Vector3(pos.x + Random.Range(-0.8f, 0.8f), SkyY(pos.y), 0f);
        if (AttackVFX.Instance != null)
        {
            AttackVFX.Instance.Lightning(from, pos, BOLT_CORE);
            AttackVFX.Instance.Lightning(from, pos, BOLT_EDGE);
        }
        WorldFeel.Ring(pos, BOLT_CORE, 1.2f, 0.25f);
        SparkPool.Emit(pos, SPARK, 8, 0.08f, 5f);
        SoundManager.PlayAt("sfx_chain", pos);
    }

    /// <summary>패링 성공: 줄기가 포탑이 아니라 셰프의 병으로 꺾여 들어간다 (예고했던 수만큼)</summary>
    private void BoltsToBottle()
    {
        if (!PatternFx) return;
        if (chefTf == null) { GameObject chefObj = GameObject.Find("Chef"); if (chefObj != null) chefTf = chefObj.transform; }
        if (chefTf == null) return;
        Vector3 bottle = chefTf.position + Vector3.up * 0.4f;
        int bolts = Mathf.Max(1, boltTargets.Count);
        for (int n = 0; n < bolts; n++)
        {
            Vector3 from = new Vector3(bottle.x + Random.Range(-1.5f, 1.5f), SkyY(bottle.y), 0f);
            if (AttackVFX.Instance != null) AttackVFX.Instance.Lightning(from, bottle, BOLT_EDGE);
        }
        WorldFeel.Ring(bottle, BOLT_EDGE, 0.9f, 0.3f);
        SparkPool.Emit(bottle, BOLT_EDGE, 6, 0.07f, 3.5f);
    }

    /// <summary>문 자리: 불똥 + 그 칸이 튄다 + 이빨 자국 (언제, 어느 칸이 물렸는지 보인다)</summary>
    private void BiteFx(Vector3 hull, Vector3 dir)
    {
        if (!PatternFx) return;
        // 표적은 기차 가운데 줄이다 - 보이는 자리는 보스 쪽 옆면(지붕선·섀시)으로 1.6u 옮긴다
        Vector3 at = new Vector3(hull.x, hull.y - Mathf.Sign(dir.y) * 1.6f, 0f);
        SparkPool.Emit(at, SPARK, 8, 0.09f, 5f);
        TrainDeck.KickCar(hull.x, new Vector2(dir.x, dir.y), GameBalance.BossBiteCarKick);
        BossMark.Claw(at, new Vector2(dir.x, dir.y), GameBalance.BossBiteMarkSec);
    }

    /// <summary>예고 대기 공통 처리. 그로기/사망으로 끊기면 false</summary>
    private IEnumerator Telegraph(string text)
    {
        BossGimmickSystem.Instance?.ShowPatternTelegraph(text, GameBalance.BossTelegraphSec);
        ApplyTint(Color.Lerp(baseTint, Color.white, 0.6f));   // 예고 중 발광
        telegraphing = true;   // v7.8: 그림이 있으면 흰 실루엣이 깜빡인다 (TickSkin)
        CastBegin();           // v7.11: 준비 - 몸이 부풀고 보스가 운다

        float t = 0f;
        while (t < GameBalance.BossTelegraphSec)
        {
            t += Time.deltaTime;
            if (isGroggy || !IsAlive) break;
            TickCastFx();      // v7.11: 패턴별 준비 표시
            yield return null;
        }

        telegraphing = false;
        ApplyTint(RestTint());   // v7.14: 쉬는 색 (P3 해치 빛을 지우지 않게)
        if (isGroggy || !IsAlive) CastCancel();   // v7.11: 끊겼다 - 발동 없이 몸만 제 크기로
    }

    /// <summary>지역 1 - 사냥 호령: 랩터 소환. 예고 중 스턴 명중 시 절반으로 저지. v7.14 (R3): 새 패턴이면 편성 조각(포위·양동 번갈아, 제 힘 그대로, 화살표 없음)으로 온다</summary>
    private IEnumerator PatternHowl()
    {
        float castStart = Time.time;
        bool formation = NewPatterns && GameBalance.RustHowlFormation;
        yield return StartCoroutine(Telegraph("녹슨 발톱이 무리를 부른다. 지금 마비시키면 절반"));
        if (isGroggy || !IsAlive) yield break;

        int count = formation ? (enraged ? GameBalance.RustHowlCountEnraged : GameBalance.RustHowlCount)
                              : GameBalance.HowlSummonCount + (enraged ? GameBalance.EnrageExtraSummon : 0);
        bool disrupted = LastStunTime >= castStart;   // 예고 중 스턴 맞았는가
        if (disrupted)
        {
            count = Mathf.Max(1, count / 2);
            UIManager.Instance?.ShowStatChange("호령을 끊었다. 랩터가 절반만 와");
        }

        CastRelease();   // v7.11: 발동
        if (waveManagerRef != null)
        {
            if (formation)
            {
                // v7.14 (R3): 웨이브 편성과 같은 조각 - 포위(사방) / 양동(양쪽)을 번갈아. 어느 쪽인지 띠에 한 줄 (화살표가 없으니 말로)
                bool encircle = (howlCount % 2 == 0);
                howlCount++;
                formationCo = waveManagerRef.SpawnBossFormation(count, encircle, GameBalance.BossSummonShare);
                string body = "랩터 " + count + "마리가 " + (encircle ? "사방에서 와" : "양쪽에서 와");
                if (!BossLine("[녹슨 발톱] 사냥 호령", body, 1.5f)) UIManager.Instance?.ShowStatChange(body);
            }
            else
            {
                // v7.11: 부른 무리는 보스 등 뒤에서 뛰쳐나온다 (구: 아무 방향) / 한 마리의 힘 = BossSummonStatMul (구: 0.7 고정) / 보상 몫 = BossSummonShare
                waveManagerRef.SpawnReinforcements("raptor", count, GameBalance.BossSummonStatMul, transform.position, GameBalance.BossSummonBehind, GameBalance.BossSummonShare);
            }
        }
        Debug.Log("[BossEnemy] 사냥 호령 - 랩터 " + count + "마리" + (disrupted ? " (저지됨)" : "") + (formation ? " 편성" : ""));
    }

    /// <summary>
    /// 지역 2 - 낙뢰 폭격 + 번개 병 패링 (v5)
    /// 예고 마지막 ParryWindowSec 동안 Space -> 낙뢰를 병에 담는다 (낙뢰 무효 + 1충전)
    /// 너무 일찍 누르면 헛스윙 (이번 낙뢰의 패링 기회 소진)
    /// 3병 모으면 여왕에게 되쏘아 강제 그로기
    /// v7.11: 맞을 포탑을 준비가 시작될 때 정하고(PickBoltTargets) 그 위에 예고 줄을 세운다 - 줄이 깜빡이는 동안이 패링 창이다.
    ///        떨어질 때는 번개 줄기가 그 포탑에 꽂히고, 패링에 성공하면 줄기가 셰프의 병으로 꺾여 들어간다
    /// </summary>
    private IEnumerator PatternLightning()
    {
        float teleSec = GameBalance.BossTelegraphSec;
        BossGimmickSystem.Instance?.ShowPatternTelegraph(
            "낙뢰가 떨어진다. 게이지 끝에서 [Space]면 병에 담겨", teleSec);
        ApplyTint(Color.Lerp(baseTint, Color.white, 0.6f));
        telegraphing = true;   // v7.8
        CastBegin();           // v7.11
        PickBoltTargets();     // v7.11

        bool parried = false;
        bool attempted = false;
        float t = 0f;

        while (t < teleSec)
        {
            t += Time.deltaTime;
            if (isGroggy || !IsAlive)
            {
                telegraphing = false; ApplyTint(RestTint());
                if (parried) BoltsToBottle();   // v7.11: 이미 병에 담았다 - 줄기는 병으로 간다 (CastCancel 이 표적을 비우기 전에)
                CastCancel();
                yield break;
            }

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
                    for (int n = 0; n < boltMarks.Count; n++)   // v7.11: 예고 줄이 병 색으로 - 잡았다는 표시
                        if (boltMarks[n] != null) boltMarks[n].Caught(BOLT_EDGE);
                    UIManager.Instance?.ShowStatChange("번개를 병에 담았다 ("
                        + ParryCharges + "/" + GameBalance.ParryChargesForCounter + ")");
                    Debug.Log("[BossEnemy] 번개 병 패링 성공! 충전 " + ParryCharges);
                }
                else
                {
                    UIManager.Instance?.ShowStatChange("너무 빨랐다. 번개가 병을 비껴갔다");
                }
            }

            yield return null;
        }

        telegraphing = false;
        ApplyTint(RestTint());
        if (isGroggy || !IsAlive) { if (parried) BoltsToBottle(); CastCancel(); yield break; }
        ClearBoltMarks();   // v7.11: 예고 줄은 떨어지는 순간까지

        // 패링 성공 -> 낙뢰 무효. 3병이면 되쏘기(강제 그로기)
        if (parried)
        {
            BoltsToBottle();      // v7.11: 줄기가 병으로 들어간다
            boltTargets.Clear();
            castAge = -1f;        // 발동 없이 끝 - 몸은 제 크기로 돌아간다
            if (ParryCharges >= GameBalance.ParryChargesForCounter)
            {
                ParryCharges = 0;
                BossNoticeAtGroggy("[되쏘기]", "병에 담은 번개가 여왕을 꿰뚫었다. 무방비");   // v7.9: 띠는 무방비 안내가 쓴다
                Debug.Log("[BossEnemy] 번개 되쏘기 - 강제 그로기!");
                ForceGroggy(GameBalance.ParryCounterGroggySec);
            }
            yield break;
        }

        // v7.11: 예고한 포탑에 떨어진다. 그사이 비었거나 잠긴 칸, 과열로 넘어간 칸(낙뢰가 덮어쓰지 못한다)은 건너뛴다
        bool anyTarget = boltTargets.Count > 0;
        int hitCount = 0;
        for (int n = 0; n < boltTargets.Count; n++)
        {
            TurretSlot slot = boltTargets[n];
            if (slot == null || slot.IsEmpty || slot.isLocked) continue;
            if (slot.IsStunned && slot.StunKind == "과열") continue;
            BoltStrike(slot.transform.position);
            slot.StunSlot(GameBalance.LightningStunSec);
            hitCount++;
        }
        boltTargets.Clear();

        // v7.11: 발동(몸이 눌렸다 돌아온다 + 고리). 번개 소리 뒤에 부른다 - 발동의 낮은 폭음은 "큰 소리"라 먼저 나면 뒤따르는 번개 소리를 눌러 버린다(SoundManager 덕킹).
        //        맞을 포탑이 하나도 없었으면(예고 줄도 없었다) 조용히 끝낸다 - 결과 없는 발동은 소음이다
        if (anyTarget) CastRelease();
        else castAge = -1f;

        if (hitCount > 0)
            UIManager.Instance?.ShowStatChange("포탑 " + hitCount + "개가 감전됐다. 옆에서 [E] 한 번");
        Debug.Log("[BossEnemy] 낙뢰 폭격 - 슬롯 " + hitCount + "곳 마비");
    }

    /// <summary>지역 3 - 갑주 재전개: 50% 이하에서 1회, 빙하 갑주를 다시 두른다</summary>
    private IEnumerator PatternRearmor()
    {
        // 갑주가 이미 있거나 재전개를 썼으면 이번 사이클은 조용히 넘어간다
        if (armorActive || secondArmorUsed || currentHP / bossMaxHP > 0.5f)
            yield break;

        yield return StartCoroutine(Telegraph("동면자가 갑주를 다시 두른다. 화염으로 녹여"));
        if (isGroggy || !IsAlive) yield break;

        CastRelease(false);   // v7.11: 발동 (몸 고리는 아래의 껍질 고리가 대신한다)
        secondArmorUsed = true;
        ActivateArmor();
        // v7.11: 얼음 껍질이 덮이는 순간 - 몸 둘레의 고리 + 얼어붙는 소리 (갑주가 생긴 때가 보인다)
        if (PatternFx)
        {
            WorldFeel.Ring(transform.position, ArmorTint(), FxRadius() * 1.5f, 0.4f);   // v7.12: 몸 폭 기준
            SoundManager.Play("sfx_freeze", 0.8f, -1f);
        }
    }

    /// <summary>최종 - 포효: 정예 증원 소환</summary>
    private IEnumerator PatternRoar()
    {
        yield return StartCoroutine(Telegraph("디 오리지널이 포효한다"));
        if (isGroggy || !IsAlive) yield break;

        int roarCount = GameBalance.OriginalRoarCount + (enraged ? GameBalance.EnrageExtraSummon : 0);
        CastRelease();   // v7.11: 발동
        // v7.11: 보스 등 뒤에서 / 한 마리의 힘 = BossSummonStatMul (구: 0.8 고정)
        if (waveManagerRef != null)
            waveManagerRef.SpawnReinforcements("raptor", roarCount, GameBalance.BossSummonStatMul, transform.position, GameBalance.BossSummonBehind, GameBalance.BossSummonShare);
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
            + Mathf.RoundToInt(GameBalance.GlacierArmorDR * 100f) + "%. 화상이나 해동포로 녹여");
        // v7.14 (스펙 3-0 P3 뒷부분): 갑주가 생기는 순간 해동포 창이 깜빡인다 - 푸는 방법이 같이 읽힌다
        if (GameBalance.ThawAttentionOn) ThawCannonUI.Attention(GameBalance.ThawAttentionSec);
        Debug.Log("[BossEnemy] 빙하 갑주 전개 (화상 " + GameBalance.GlacierBreakBurnStacks + "스택으로 파괴)");
    }

    private void BreakArmor()
    {
        armorActive = false;
        ApplyTint(baseTint);
        UIManager.Instance?.ShowStatChange("빙하 갑주가 깨졌다. 동면자가 무방비");
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

        // v7.14 (H3): 눈사태가 내려오는 중이면 GOOD 이상으로 멈춘다 (벽이 깨지고 무방비 - BossPatterns.PatternAvalanche)
        if (quality >= 1) CancelAvalanche("cannon");   // 알림은 눈사태 쪽이 한 줄 ("해동포가 눈사태를 깨뜨렸다")

        if (quality >= 2)
        {
            // 정중앙: 갑주 즉시 전파괴(보너스 그로기 포함) + 대미지
            if (armorActive) BreakArmor();
            TakeDamage(GameBalance.ThawPerfectDamage * hpScale, DamageType.Magic);
            UIManager.Instance?.ShowStatChange("해동포 직격. 갑주가 산산조각 났다");
        }
        else if (quality == 1)
        {
            // 존 안: 갑주 감쇄율 절반 + 중간 대미지
            if (armorActive)
            {
                armorDR *= 0.5f;
                UIManager.Instance?.ShowStatChange("해동포 명중. 갑주가 절반쯤 녹았다");
            }
            TakeDamage(GameBalance.ThawGoodDamage * hpScale, DamageType.Magic);
        }
        else
        {
            // 빗맞음: 대미지만
            TakeDamage(GameBalance.ThawMissDamage * hpScale, DamageType.Magic);
            UIManager.Instance?.ShowStatChange("해동포가 빗나갔다");
        }

        Debug.Log("[BossEnemy] 해동포 피격 (품질 " + quality + ")");
    }

    // ─────────────────────────────────────────────
    // 돌진 공격 (v3 유지). v7.11: 물기 전에 웅크리고, 문 자리에 불똥·칸 튐·이빨 자국
    // ─────────────────────────────────────────────
    private IEnumerator AttackLunge()
    {
        isLunging = true;

        Vector3 startPos = transform.position;
        lungeHome = startPos;
        Vector3 hull = CurrentTargetPos;                          // v7.8: 중심이 아니라 눈앞의 몸통으로
        Vector3 dir = (hull - startPos).normalized;
        Vector3 peakPos = startPos + dir * 1.2f;
        float t;

        // v7.11 (P4): 준비 - 뒤로 물러나며 웅크린다. 언제 무는지 보인다 (예전엔 준비 동작 없이 0.16초에 튀어나왔다)
        Vector3 fromPos = startPos;
        // 웅크리는 시간은 스위치(BossPatternFxOn)만 따른다 - 손맛 세기(GameFeelMaster)를 0 으로 내려도 무는 박자는 같다 (몸 크기·불똥만 빠진다)
        float wind = GameBalance.BossPatternFxOn ? Mathf.Max(0f, GameBalance.BossLungeWindupSec) : 0f;
        if (wind > 0.01f)
        {
            fromPos = startPos - dir * Mathf.Max(0f, GameBalance.BossLungeBack);
            t = 0f;
            while (t < wind)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / wind);
                transform.position = Vector3.Lerp(startPos, fromPos, 1f - (1f - k) * (1f - k));
                crouchNow = k;                                    // 몸이 살짝 작아진다 (TickCastScale)
                yield return null;
                if (isGroggy || !IsAlive)
                {
                    // 웅크리다 무방비가 됐다(또는 쓰러졌다) - 물지 않고 제자리로. 준비 동작이 생긴 만큼 끊을 틈도 생긴다
                    crouchNow = 0f;
                    Vector3 backFrom = transform.position;
                    t = 0f;
                    while (t < 0.12f)
                    {
                        t += Time.deltaTime;
                        transform.position = Vector3.Lerp(backFrom, startPos, Mathf.Clamp01(t / 0.12f));
                        yield return null;
                    }
                    transform.position = startPos;
                    isLunging = false;
                    yield break;
                }
            }
        }

        crouchNow = 0f;   // 튀어나가는 동안 몸이 제 크기로 펴진다 (웅크림 -> 뻗음)
        t = 0f;
        while (t < 0.16f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(fromPos, peakPos, t / 0.16f);
            yield return null;
        }
        if (!IsAlive) { isLunging = false; yield break; }   // v7.11: 튀어나가는 사이에 쓰러졌다 - 쓰러진 보스가 기차를 물지 않는다 (예전엔 물었다)

        // P1 게임필: 런지 착지 임팩트 (기차 피격 셰이크와 별개의 육중함 - 절반 강도)
        GameFeel.Shake(GameBalance.ShakeBoss * 0.5f);

        // v6: 도발 중이면 미끼를 물어뜯는다 (기차 무피해)
        if (!IsTaunted)
        {
            AttackTrain();
            BiteFx(hull, dir);   // v7.11: 문 자리 - 불똥 + 그 칸이 튄다 + 이빨 자국
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
        // v7.11: 웅크린 시간만큼 다음 공격과 다음 패턴을 당긴다 - 무는 동안엔 두 타이머가 멈추니, 그대로 두면 물 때마다 0.25초씩 늦어진다
        attackTimer = wind;
        patternTimer -= wind;
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
            BossNoticeAtGroggy("[디 오리지널] 마지막 틈", "손님이 다시 식탁 앞에 무릎을 꿇었다. 마지막 기회야");   // v7.9
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
        CastCancel();          // v7.11: 준비하다 멈췄으면 표시를 걷고 몸을 제 크기로
        crouchNow = 0f;
        telegraphing = false;
        if (isLunging) transform.position = lungeHome;   // 물다가 멈췄으면 선 자리로 (기차 위에 걸친 채 식사하지 않게)
        isLunging = false;
        isCasting = false;

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

            BossNotice("[디 오리지널] 포탑이 멈췄다. 요리가 건너간다.", "");   // v7.9: 씹는 모습을 가리지 않게 띠로
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
        if (collapsing && !collapseDone) return;   // v7.14: 칸이 떨어져 나가는 중 - 끝나면 CollapseThenDie 가 부른다
        // v7.14 (O6): 디 오리지널은 칸이 떨어져 나간 뒤에 이 순서로 돌아온다 (CollapseThenDie 가 다시 부른다). 마지막 식사 엔딩은 그대로
        if (kind == BossKind.Original && !practice && !isServing && !collapseDone && !collapsing && NewPatterns
            && GameBalance.OrigCollapseOn && GameBalance.GameFeelMaster > 0f && skin != null)
        {
            StartCoroutine(CollapseThenDie());
            return;
        }
        CleanupNewPatterns();   // v7.14: 서리·눈사태 벽·빗금·북쪽 막기
        RoutMinions();          // v7.14: 남은 손님이 달아난다 (처치 골드는 준다)
        if (glow != null) glow.enabled = false;   // v7.8: 상태 발광은 여기까지 (죽는 과정은 HitFeelBody 가 그림을 흐린다)
        CastCancel();   // v7.11: 준비하다 쓰러지면 예고 줄을 걷는다
        LeaveStage(GameBalance.BossCamOutSec);   // v7.12: 보스전 카메라·평행 선로·그림자를 걷는다 (쓰러진 뒤 1.2초에 돌아온다)
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
            UIManager.Instance?.ShowStatChange("남은 번개 병 " + ParryCharges + "개를 전기알로 바꿨다");
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
        if (IsAlive) LeaveStage(0.3f);   // v7.12: Die 를 안 거치고 사라질 때만 (씬 전환은 CameraZoom.Start 가 다시 끈다)
        if (frostOwner == this) { frostMask = 0; frostUntil = -1f; frostOwner = null; }   // v7.14: 다른 스크립트가 보는 것만 (표시는 스스로 사라진다)
        if (northBlockedBy == this) northBlockedBy = null;
    }

    /// <summary>v7.12: 보스전 무대 걷기 - 카메라 복귀 + 평행 선로 걷기 + 그림자 끔. 두 번 불려도 된다</summary>
    private void LeaveStage(float sec)
    {
        entering = false;
        if (practice) return;
        CameraZoom.BossFramingOff(sec);
        if (kind == BossKind.Original) ParallaxBackground.SideRailOff(sec);
        if (shadow != null) shadow.enabled = false;
    }

    public bool IsGroggy => isGroggy;
}
