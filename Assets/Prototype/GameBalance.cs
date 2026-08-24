using UnityEngine;

/// <summary>
/// [GameBalance.cs] v1
/// 게임 전체 밸런스 수치를 한 곳에 모은 설정 파일.
///
/// 여기 값을 바꾸면 Inspector 값과 상관없이 게임에 적용된다
/// (TrainManager / GameManager가 Start에서 이 값으로 덮어쓴다).
/// 밸런스 조정은 이 파일만 고치면 된다.
///
/// VS 2017 (C# 7.3) 호환
/// </summary>
public static class GameBalance
{
    // ==================================================================
    //  기차 (플레이어) - 증강/포탑 성장이 생겼으므로 기저 스탯 하향
    // ==================================================================

    /// <summary>기차 시작 최대 HP (기존 1000 -> 500. 강철 리벳/요새/야전 정비반으로 성장)</summary>
    public static float TrainStartHP = 500f;

    /// <summary>모든 포탑 데미지 전역 배율 (기존 1.0 -> 0.7. 증강으로 화력을 되찾는 구조)</summary>
    public static float TurretDamageMul = 0.7f;

    /// <summary>시작 골드 (기존 500 -> 200. 정비소/도박 증강의 무게를 살림)</summary>
    public static int StartGold = 200;

    // ── 감사 3-A: 골드 커브 (인플레이션 억제) ──
    /// <summary>웨이브 클리어 골드 = TownGoldBase + 웨이브 x TownGoldPerWave</summary>
    public static int TownGoldBase = 80;
    public static int TownGoldPerWave = 18;
    /// <summary>보스 웨이브 클리어 추가 보너스</summary>
    public static int BossClearGold = 300;

    /// <summary>증강 건너뛰기 보상 명성 (감사 2-A)</summary>
    public static int AugmentSkipFame = 15;

    /// <summary>기본 해금 포탑 슬롯 수 (총 8칸 중. 나머지는 증강 '증축된 주방 칸'으로 확장)</summary>
    public static int BaseSlotCount = 6;

    // ==================================================================
    //  속성 공명 (B-5) - 같은 속성 포탑을 모으면 세트 보너스
    // ==================================================================

    // ==================================================================
    //  슬롯 배치 (TurretSlotManager가 Start에서 적용 - Inspector 무시)
    //  기차 로컬 좌표 기준. 좌우 2열이 기차 양옆에 붙도록 컴팩트하게
    // ==================================================================

    public static float SlotOriginX = -1.7f;   // 왼쪽 열 x
    public static float SlotOriginY = 1.2f;    // 첫 행 y
    public static float SlotSpacingX = 3.4f;   // 왼쪽 열 -> 오른쪽 열 간격
    public static float SlotSpacingY = 0.8f;   // 행 간격 (4행: 1.2 / 0.4 / -0.4 / -1.2)

    /// <summary>공명 발동에 필요한 같은 속성 포탑 수</summary>
    public static int ResonanceCount = 3;

    /// <summary>공명 시 해당 속성 데미지 보너스 (0.20 = +20%). 방어 속성은 피해감소 +10%로 대체</summary>
    public static float ResonanceBonus = 0.20f;

    // ==================================================================
    //  런 구조 - 3지역 x N웨이브 + 최종전 (기획: 슬더스 3막 구조)
    //  지역 1: 구리 사막 / 지역 2: 테슬라 협곡 / 지역 3: 코발트 광산
    //  각 지역 마지막 웨이브에 보스, 최종 웨이브(FinalWave)에 최종 보스
    // ==================================================================

    /// <summary>
    /// 지역 하나의 웨이브 수.
    /// [테스트용 임시] 3 (보스 3/6/9, 최종전 10 - 빠른 확인용)
    /// 빌드 전 8로 복구할 것 -> 보스 8/16/24, 최종전 25 (정식 25웨이브 런)
    /// </summary>
    public static int RegionLength = 3;

    /// <summary>최종전 웨이브 번호 (지역 3개 + 1)</summary>
    public static int FinalWave { get { return RegionLength * 3 + 1; } }

    /// <summary>이 웨이브가 속한 지역 번호 (1~3, 최종전은 4)</summary>
    public static int RegionOf(int wave)
    {
        if (wave >= FinalWave) return 4;                 // 최종전
        int r = (wave - 1) / RegionLength + 1;
        return Mathf.Clamp(r, 1, 3);
    }

    /// <summary>지역 안에서의 진행도 0.0~1.0 (적 물량 계산용)</summary>
    public static float RegionProgress(int wave)
    {
        if (wave >= FinalWave) return 1f;
        int t = (wave - 1) % RegionLength + 1;           // 지역 내 1~RegionLength
        return (float)t / RegionLength;
    }

    /// <summary>보스 웨이브인가? (각 지역 마지막 + 최종전)</summary>
    public static bool IsBossWave(int wave)
    {
        return (wave % RegionLength == 0 && wave <= RegionLength * 3) || wave == FinalWave;
    }

    // ==================================================================
    //  적 난이도
    // ==================================================================

    /// <summary>
    /// 적 스케일링 난이도 계수 L. 공식: Final = Base * (1 + Wave * 0.15 / L)
    /// L이 낮을수록 웨이브당 적이 빨리 강해진다. (기존 2.0 = Easy -> 1.5)
    /// </summary>
    public static float EnemyDifficultyL = 1.5f;

    /// <summary>일반 적 체력 전역 배율 (웨이브 스케일링 이후 곱해짐)</summary>
    public static float EnemyHPMul = 1.0f;

    /// <summary>일반 적 공격력 전역 배율</summary>
    public static float EnemyATKMul = 0.9f;

    // ==================================================================
    //  연속 피격 완충 - 무리 러시가 같은 순간에 우르르 때려도 즉사하지 않게
    //  같은 시간 창(BurstHitWindow) 안에서 BurstFreeHits번째까지는 정상 피해,
    //  그 이후 타격은 BurstExtraHitMul 배율로 감소
    // ==================================================================

    public static float BurstHitWindow = 0.8f;   // 판정 시간 창(초)
    public static int BurstFreeHits = 2;         // 정상 피해로 들어오는 타격 수
    public static float BurstExtraHitMul = 0.5f; // 초과 타격 데미지 배율

    // ==================================================================
    //  보스 (BossEnemy가 사용 - 고정 스탯 대신 웨이브 비례 공식)
    //  보스 HP = BossHPBase + 웨이브 x BossHPPerWave
    //  보스 ATK = BossATKBase + 웨이브 x BossATKPerWave
    //  예) 웨이브 3: HP 1550 / ATK 64   웨이브 10: HP 3300 / ATK 120
    // ==================================================================

    public static float BossHPBase = 800f;
    public static float BossHPPerWave = 250f;
    public static float BossATKBase = 40f;
    public static float BossATKPerWave = 8f;

    // ==================================================================
    //  보스 패턴 (A단계) - 보스패턴설계 문서 참조. 수치는 전부 가설, 여기서 조정
    // ==================================================================

    public static float BossPatternFirstDelay = 8f;    // 전투 시작 후 첫 패턴까지
    public static float BossPatternInterval = 13f;     // 패턴 간격 (+-2초 랜덤)
    public static float BossTelegraphSec = 2f;         // 패턴 예고 시간

    // 지역 1 '녹슨 발톱' - 사냥 호령 (소환. 예고 중 스턴 명중 시 절반)
    public static int HowlSummonCount = 5;

    // 지역 2 '천둥 둥지' - 낙뢰 폭격 (포탑 슬롯 마비. 마커 클릭으로 재가동)
    public static int LightningSlotCount = 2;
    public static float LightningStunSec = 6f;

    // 지역 3 '동면자' - 빙하 갑주 (피해 90% 감소. 화상 스택 누적으로 파괴)
    public static float GlacierArmorDR = 0.9f;         // 갑주 피해 감소율
    public static int GlacierBreakBurnStacks = 5;      // 파괴에 필요한 화상 스택 누적
    public static float GlacierBreakGroggySec = 3f;    // 파괴 시 보너스 그로기

    // 최종 '디 오리지널' - 포효 (정예 증원 소환)
    public static int OriginalRoarCount = 4;

    // ── C-2: 마지막 주문 (진엔딩 B) ──
    /// <summary>
    /// 엔딩 B 조건: 도감 발견 수. 정식 42 (전 요리).
    /// [테스트용] 낮춰서 확인 가능 (예: 3) - 빌드 전 42 복구
    /// </summary>
    public static int TrueEndingRecipesNeeded = 42;

    /// <summary>풀코스 QTE 라운드 수 / 성공 필요 수</summary>
    public static int FinalOrderRounds = 3;
    public static int FinalOrderNeeded = 2;

    /// <summary>엔딩 B 달성 보너스 명성</summary>
    public static int EndingBFame = 500;

    // ==================================================================
    //  보스 패턴 (B단계) - 패링 / 해동포 / 발악
    // ==================================================================

    // 번개 병 패링 (천둥 둥지): 낙뢰 예고 마지막 순간에 Space
    public static float ParryWindowSec = 0.6f;      // 예고 종료 직전 판정 창
    public static int ParryChargesForCounter = 3;   // 이 수만큼 모으면 되쏘기(강제 그로기)
    public static float ParryCounterGroggySec = 4f; // 되쏘기 그로기 시간

    // 해동포 (동면자): 화염을 태워 쏘는 광산 열차포
    public static float ThawChargeMax = 100f;       // 발사에 필요한 충전량
    public static float ThawChargePerMaterial = 25f;// 화염 재료 1개 장전량
    public static float ThawChargePerFood = 50f;    // 화염 요리 1개 장전량
    public static float ThawPerfectDamage = 300f;   // 압력 정중앙 발사
    public static float ThawGoodDamage = 150f;      // 압력 존 안 발사
    public static float ThawMissDamage = 80f;       // 존 밖 발사

    // 발악 (HP 50% 이하): 패턴 가속 + 규모 증가
    public static float EnrageHPRatio = 0.5f;
    public static float EnragePatternIntervalMul = 0.7f;  // 패턴 간격 배율
    public static int EnrageExtraSummon = 2;              // 호령/포효 소환 추가
    public static int EnrageExtraLightning = 1;           // 낙뢰 마비 슬롯 추가

    // ==================================================================
    //  보스 패턴 (C단계) - 미끼 화덕 / 디 오리지널 3페이즈
    // ==================================================================

    // 미끼 화덕 (녹슨 발톱): 고기 1개를 구워 던져 무리+보스를 유인
    public static float BaitDurationPerfect = 8f;   // 굽기 판정별 유인 시간
    public static float BaitDurationGood = 6f;
    public static float BaitDurationMiss = 4f;
    public static float BaitCooldown = 6f;          // 미끼 재사용 대기
    public static float BaitDistance = 7f;          // 기차로부터 미끼 설치 거리

    // 디 오리지널 3페이즈
    public static float FeedPhaseStartRatio = 0.70f;  // P2 폭식 시작 HP 비율
    public static float HatchPhaseStartRatio = 0.35f; // P3 해치 개방 HP 비율
    public static float FeedHealPerFragment = 60f;    // 조각 1개 흡수 시 회복
    public static float FeedHealCapRatio = 0.15f;     // 총 회복 상한 (최대 HP 비율)
    public static float FeedAtkPerFragment = 0.04f;   // 조각당 공격력 +4%
    public static float FeedAtkCap = 0.5f;            // 공격력 증가 상한 (+50%)
    public static float FeedContestChance = 0.6f;     // 조각이 쟁탈 대상이 될 확률
    public static float HatchDamageTakenMul = 1.3f;   // 해치 개방 중 받는 피해 배율

    // ==================================================================
    //  게임필 (P1) - 셰이크 / 히트스톱 / 처치 팝 (GameFeel.cs가 사용)
    //  전부 0으로 만들면 해당 연출이 완전히 꺼진다.
    //  플레이테스트에서 "과하다/멀미난다" 싶으면 GameFeelMaster 하나만 낮출 것.
    // ==================================================================

    /// <summary>게임필 전체 강도 배율 (1=기본, 0.5=절반, 0=전부 끄기)</summary>
    public static float GameFeelMaster = 1.0f;

    public static float ShakeTrainHit = 0.22f;     // 기차 피격 셰이크 (자주 발생 - 약하게)
    public static float ShakeExplosion = 0.08f;    // 포탑 폭발 셰이크 (매우 잦음 - 미세한 럼블 수준)
    public static float ShakeBoss = 0.45f;         // 보스 임팩트 공용 (런지 착지/그로기 진입/처치, 쿨타임 없음)

    // 셰이크 쿨타임: 잦은 이벤트가 화면을 쉬지 않고 흔들면 피로해진다 (사용자 피드백 반영)
    // 쿨타임 동안의 같은 종류 충격은 조용히 무시. 보스 임팩트는 드물어서 쿨타임 미적용
    public static float ShakeTrainHitCooldown = 1.5f;  // 기차 피격 셰이크 최소 간격 (초)
    public static float ShakeExplosionCooldown = 2.5f; // 폭발 럼블 최소 간격 (초)

    public static float HitstopBossGroggy = 0.12f; // 그로기 진입 히트스톱 (실시간 초)
    public static float HitstopBossKill = 0.22f;   // 보스 처치 히트스톱

    public static float DeathPopScale = 1.0f;      // 적 처치 팝 크기 배율 (0=끄기)

    // ==================================================================
    //  시작 보급품 - "포탑 없음 -> 파밍 불가 -> 사망" 데드락 방지
    //  웨이브 1 시작 시 FoodStock에 완성 요리를 지급한다 (바로 슬롯에 투입 가능)
    // ==================================================================

    /// <summary>시작 지급 요리 목록: (레시피 id, 수량)</summary>
    public static readonly StarterFood[] StarterFoods = new StarterFood[]
    {
        new StarterFood("meat+meat", 2),   // 더블 육포 x2 (기본 물리 포탑)
        new StarterFood("armor+meat", 1),  // 하티 스테이크 x1 (명중 시 기차 회복)
    };

    public struct StarterFood
    {
        public string recipeId;
        public int count;
        public StarterFood(string id, int n) { recipeId = id; count = n; }
    }
}
