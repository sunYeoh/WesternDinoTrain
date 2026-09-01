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
    //  B-2: 구 2열 4행(SlotOrigin/Spacing) 폐기 -> 포탑칸 가로 1열 배치는
    //  아래 "B-2" 섹션의 SlotRowAX/SlotRowBX/SlotGapX/SlotY가 담당한다
    // ==================================================================

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
    //  조리 난이도 (P1, 감사 1-A) - "협곡에서는 손도 떨린다"
    //  지역이 깊어질수록 커서가 빨라지고 판정이 좁아진다.
    //  플레이테스트에서 "짜증난다" 싶으면 수치를 절반으로 (감사 셀프피드백 1 참조).
    // ==================================================================

    /// <summary>지역별 조리 압박(커서 속도/시간 가속률): [지역1, 지역2, 지역3, 최종]</summary>
    public static float[] CookRegionSpeedUp = { 0f, 0.12f, 0.25f, 0.25f };

    /// <summary>지역별 판정 존 축소율: [지역1, 지역2, 지역3, 최종]</summary>
    public static float[] CookRegionJudgeShrink = { 0f, 0f, 0.10f, 0.10f };

    // 오일 캑터스 '기름 튐' (죽은 플레이버의 실기믹화, 감사 2-C)
    public static float OilSlipDuration = 6f;    // 명중 시 조리대 미끄러짐 지속(초)
    public static float OilSlipWobble = 0.45f;   // 굽기 커서 요동 강도 (0이면 기믹 꺼짐)

    // 인퓨징 (P1, 감사 1-A 처방 2): T2 진화 미니게임 - InfusingMinigame.cs가 사용
    // 실패해도 진화는 성공 (보너스만 없음). 지역 난이도는 적용하지 않음 (이미 고부담 순간)
    public static int InfuseBonusScoreNeed = 3;  // 판정 합계(라운드당 PERFECT 2/Good 1) 이 이상 = 보너스
    public static int InfuseBonusLevel = 1;      // 보너스 레벨 (+1로 탄생)
    public static float InfuseGrillSpeed = 70f;  // 1라운드(정수 추출) 커서 속도
    public static float InfuseBoilTime = 4f;     // 2라운드(융합 안정화) 유지 시간(초)

    // ==================================================================
    //  요리 숙련 (P1+, 사용자 결정 2026-08-24: 단골 메뉴의 영구화)
    //  레시피별 "평생" 조리 횟수 누적 - 죽어도 리셋 안 됨 (같은 셰프니까).
    //  배열은 전부 티어 순서 대응: [3회, 5회, 10회, 20회, 30회, 50회, 100회]
    // ==================================================================

    /// <summary>숙련 마일스톤 (누적 조리 횟수)</summary>
    public static int[] MasteryThresholds = { 3, 5, 10, 20, 30, 50, 100 };

    /// <summary>티어별 칭호 (알림/툴팁 표기)</summary>
    public static string[] MasteryTitles =
        { "단골 메뉴", "입소문", "익숙한 손길", "단골의 맛", "장인의 길", "장인의 감각", "마스터 요리" };

    /// <summary>티어별 그 레시피 포탑 공격력 보너스 (대체 방식 - 중첩 아님)</summary>
    public static float[] MasteryAtkBonus =
        { 0.04f, 0.06f, 0.08f, 0.10f, 0.12f, 0.15f, 0.20f };

    /// <summary>티어별 그 레시피 조리 판정 존 보너스 (10회부터)</summary>
    public static float[] MasteryJudgeBonus =
        { 0f, 0f, 0.05f, 0.08f, 0.08f, 0.10f, 0.12f };

    /// <summary>이 티어(50회)부터: 빈 슬롯에 배치 시 시작 레벨 +1</summary>
    public static int MasteryStartLevelTier = 5;

    /// <summary>이 티어(100회)부터: PERFECT 조리 획득 수량 +1 (2 -> 3)</summary>
    public static int MasteryPerfectTier = 6;

    /// <summary>100회 최초 달성 시 1회 지급 명성</summary>
    public static int MasteryFame = 100;

    /// <summary>누적 횟수 -> 현재 티어 (-1 = 아직 없음)</summary>
    public static int MasteryTier(int count)
    {
        int tier = -1;
        for (int i = 0; i < MasteryThresholds.Length; i++)
            if (count >= MasteryThresholds[i]) tier = i;
        return tier;
    }

    // 아이스 모사 슬롯 빙결 (P1, 감사 2-C): 죽은 플레이버("바퀴 결빙")의 실기믹화
    public static float FreezeChance = 0.5f;        // 모사 명중 시 빙결 발동 확률
    public static float FreezeSlotSec = 4f;         // 슬롯 빙결 지속(초) - 클릭으로 즉시 해빙 가능
    public static float FreezeGlobalCooldown = 7f;  // 전체 모사 공유 쿨타임 (다중 모사 스턴락 방지)

    // ==================================================================
    //  스피노 베팅 (Phase 2-1) - 보스 직전 정차의 도박사
    //  일반 베팅 = 실패해도 무손실 / 도박 베팅 = 화끈한 대가 (사용자 결정 2026-08-25)
    //  조건 추적/정산은 SpinoBet.cs, 등장 UI는 SpinoBetUI.cs
    // ==================================================================

    // [일반] 정시 배식: 제한 시간 내 보스 격파
    public static float BetOnTimeSec = 120f;
    public static int BetOnTimeGold = 150;

    // [일반] 완벽한 접시: 보스전 중 PERFECT 조리
    public static int BetPerfectNeed = 2;
    public static int BetPerfectMats = 4;      // 보상: 랜덤 재료 수

    // [일반] 철벽 주방: 기차 피격 제한
    public static int BetTankHitsMax = 8;
    public static float BetTankMaxHP = 80f;    // 보상: 최대 HP (런 한정)

    // [도박] 외상 장부: 판돈 선불, 그로기 투척 명중
    public static int BetLedgerStake = 150;
    public static int BetLedgerPayoutMul = 4;  // 성공 배수 (150 -> 600)
    public static int BetLedgerThrowNeed = 2;
    // 실패: 판돈 몰수 + 재료 전 종류 절반 압류

    // [도박] 속전속결: 제한 시간 내 격파
    public static float BetRushSec = 90f;
    public static int BetRushGold = 500;
    public static float BetRushHPPenalty = 50f;   // 실패: 최대 HP 감소 (+격파 보너스 몰수)

    // [도박] 굶주린 식탁: 적은 포탑으로 격파
    public static int BetFeastSlotsMax = 4;
    public static int BetFeastMats = 4;        // 성공: 전 재료 +4
    public static int BetFeastFame = 50;
    // 실패: 골드 절반 압류 + 격파 보너스 몰수

    // ==================================================================
    //  증강 확장 (Phase 2-2) - 리롤 / 최후의 만찬
    // ==================================================================

    /// <summary>증강 리롤 기본 비용 (골드). 사용할 때마다 Growth만큼 비싸진다 (런 단위 리셋)</summary>
    public static int RerollBaseCost = 80;
    public static int RerollCostGrowth = 40;

    /// <summary>증강 '최후의 만찬': 이 HP 비율 이하일 때 공속 배율 발동</summary>
    public static float LastSupperHPRatio = 0.4f;
    public static float LastSupperAspdMul = 1.5f;

    // ==================================================================
    //  아이템(유물) + 행상인 안킬로 (Phase 2-3) - ItemSystem/MerchantUI가 사용
    // ==================================================================

    /// <summary>정차 시 행상인 등장 확률 (보스 직전 정차 제외, 각 지역 첫 정차는 확정 등장)</summary>
    public static float MerchantChance = 0.35f;

    /// <summary>아이템 가격 전체 배율 (경제 조이기/풀기용 - 개별 가격은 ItemSystem.cs)</summary>
    public static float ItemPriceMul = 1f;

    /// <summary>적 처치 시 아이템 드랍 확률 (일반 / 보스 / 침입자 격퇴)</summary>
    public static float ItemDropChance = 0.008f;
    public static float ItemDropChanceBoss = 0.25f;
    public static float ItemDropChanceIntruder = 0.12f;

    /// <summary>폐역 선로 클리어 시 아이템 획득 확률</summary>
    public static float RouteRelicChance = 0.35f;

    // ==================================================================
    //  증강 확장 (Phase 2-3) - 신규 증강 10종 계수
    // ==================================================================

    /// <summary>마지막 서비스: 처치한 적 폭발 (처치 데미지 비율 / 반경)</summary>
    public static float CorpseServiceRatio = 0.25f;
    public static float CorpseServiceRadius = 2.6f;

    /// <summary>옆 테이블 계산서: 초과 데미지 이월 탐색 범위</summary>
    public static float OverkillCarryRange = 8f;

    /// <summary>가시철조망 도금: 반격 = 기차 DEF x 이 값 x 스택 (쿨타임 안에 1회)</summary>
    public static float ThornsDefRatio = 1.5f;
    public static float ThornsRadius = 6f;
    public static float ThornsCooldown = 0.5f;

    /// <summary>강철의 심장: 최대 HP 100당 데미지 증가율 (전체 상한 +100%)</summary>
    public static float SteelHeartPer100 = 0.02f;

    /// <summary>선대의 기본기: T1 포탑 데미지 증가율</summary>
    public static float BasicsT1Bonus = 0.65f;

    /// <summary>주방장은 하나다: 기본 보너스 / 처치당 누적 / 누적 상한 / 나머지 포탑 감소율</summary>
    public static float OneChefBonus = 0.5f;
    public static float OneChefPerKill = 0.02f;
    public static int OneChefMaxStacks = 100;
    public static float OneChefOthersPenalty = 0.2f;

    /// <summary>넘치는 솥: 증기 보호막 상한 (최대 HP 비율)</summary>
    public static float OverflowShieldCap = 0.25f;

    /// <summary>골동품 감정가: 보유 아이템 1개당 데미지 증가율</summary>
    public static float CollectorPerItem = 0.06f;

    // ==================================================================
    //  B-1: 셰프의 몸 (방향결정 2026-08-31) - 이동감 + 근접 위기 대응
    //  ProximityInteract = false 로 두면 위기 대응이 기존 클릭 방식으로 복귀
    // ==================================================================

    /// <summary>셰프 이동 속도 (기존 3 - 몸이 주인공이 되면서 상향)</summary>
    public static float ChefMoveSpeed = 4.2f;
    public static float ChefAccel = 30f;          // 가속 (유닛/초^2)
    public static float ChefDecel = 40f;          // 감속

    /// <summary>대시 (Shift): 순간 가속 + 흙먼지. 조리 중에는 이동 자체가 잠겨 발동 불가</summary>
    public static float ChefDashSpeed = 12f;
    public static float ChefDashTime = 0.16f;
    public static float ChefDashCooldown = 1.2f;

    /// <summary>셰프 활동 범위 (B-2: 트레일러 4칸으로 확장됨)</summary>
    public static float TrainWalkMinX = -6.3f;
    public static float TrainWalkMaxX = 11.3f;
    public static float TrainWalkMinY = -1.5f;
    public static float TrainWalkMaxY = 1.5f;

    /// <summary>위기 대응 근접 전환 스위치 (false = 빙결/감전 해제가 클릭으로 복귀)</summary>
    public static bool ProximityInteract = true;

    /// <summary>마비(빙결/감전/과열) 포탑 해제 근접 반경 (셰프-슬롯 거리)</summary>
    public static float SlotReach = 1.3f;

    /// <summary>위치형 주방 이벤트: 조작 가능 근접 반경 (X 거리)</summary>
    public static float EventReachX = 1.8f;

    /// <summary>이벤트 발생 지점 범위 (B-2: 기차 전체 칸에서 터진다)</summary>
    public static float EventAnchorMinX = -6.0f;
    public static float EventAnchorMaxX = 11.0f;

    /// <summary>위치형 이벤트 제한시간 보정 (+초, 달려가는 시간만큼 여유)</summary>
    // 밸런스 1차: 2.5 -> 4.0. 최악 대각(포탑B 끝 -> 기관차, 17유닛 = 걷기 4초)
    // + 조리 중단 반응 1초를 더하면 2.5초로는 도달 전 실패가 난다 (헌법 위반)
    public static float EventReachGrace = 4.0f;

    // ==================================================================
    //  B-2: 트레일러 4칸 + 과열 + 카메라 + 갑판 전리품 (방향결정 2026-08-31)
    // ==================================================================

    /// <summary>칸 경계 X (5개 값 = 4칸): 기관차 / 주방 / 포탑 A / 포탑 B</summary>
    public static float[] CarEdgesX = { -6.5f, -2.5f, 2.5f, 7f, 11.5f };
    public static string[] CarNames = { "기관차", "주방", "포탑 A", "포탑 B" };

    /// <summary>x 좌표가 속한 칸 인덱스 (0~3, 범위 밖은 가장 가까운 칸)</summary>
    public static int CarIndexOf(float x)
    {
        for (int i = 1; i < CarEdgesX.Length - 1; i++)
            if (x < CarEdgesX[i]) return i - 1;
        return CarEdgesX.Length - 2;
    }

    /// <summary>슬롯 배치 (B-2: 포탑칸 가로 1열 4+4. 0~3=포탑 A, 4~7=포탑 B)</summary>
    public static float SlotRowAX = 3.1f;      // 포탑 A 첫 슬롯 x
    public static float SlotRowBX = 7.6f;      // 포탑 B 첫 슬롯 x
    public static float SlotGapX = 1.1f;       // 슬롯 간격
    // B-2.2: 0.9(칸 몸통 속) -> 1.95(지붕 위). 포탑 받침이 지붕선(1.8)에 딱 앉는다 (원안 복원).
    // 근접 판정은 가로 거리만 보므로(FindStunnedSlotNear) 셰프는 여전히 발밑에서 정비 가능
    public static float SlotY = 1.95f;

    // ── B-2.2: 포탑 실물 비주얼 (TurretSlot이 코드 도형으로 그린다) ──
    public static bool TurretVisuals = true;       // false = 실물 끄기 (마커 칩만)
    public static float SlotMarkerYOffset = 1.05f; // 마커 칩을 포탑 머리 위로 (월드 유닛)
    public static float StationScale = 0.55f;      // 조리대 통일 스케일 (씬 0.4 -> 시인성 업)
    public static bool ClearStunsOnTown = true;    // 정비 시간 진입 시 마비/과열 전체 해제

    // 밸런스 1차: 인접 버프 보정. B-2 가로 1열 재배치로 버프 수혜 슬롯이
    // 평균 ~3개(구 2x4 격자) -> 최대 2개(양옆)로 줄었다 - 버프형 포탑 가치 복원
    // (예: 물리 +40% -> 실효 +60%. 수혜 폭 절반 x 1.5배 = 구 가치의 ~75%)
    public static float AdjBuffScale = 1.5f;

    /// <summary>
    /// 비주얼 정렬 (B-2.1): 구 기차 스프라이트(씬의 5x5 사각형)를 숨긴다.
    /// 4칸 데크가 기차 본체 역할을 이어받는다. 렌더러만 끄고 로직/태그는 유지.
    /// </summary>
    public static bool HideLegacyTrainVisual = true;

    /// <summary>비주얼 정렬 (B-2.1): 조리대 3대를 주방칸 안 정위치로 자동 정렬 (false=씬 배치 그대로)</summary>
    public static bool AlignStations = true;
    public static float StationY = -0.7f;                      // 조리대 높이 (갑판 위)
    public static float[] StationXs = { -1.6f, 0f, 1.6f };     // 그릴 / 볶음팬 / 냄비 x

    /// <summary>포탑 과열: 연속 사격이 쌓이면 정지, 근접 [E] 홀드로 냉각 (0=끔)</summary>
    public static bool OverheatEnabled = true;
    public static int OverheatShotsMin = 22;       // 과열까지 사격 수 (랜덤 하한)
    public static int OverheatShotsMax = 34;       // (랜덤 상한)
    public static int OverheatPerLevel = 2;        // 포탑 레벨당 임계 감소 (캐리일수록 손이 간다)
    public static float OverheatCoolHold = 0.8f;   // [E] 홀드 냉각 시간
    public static float OverheatImmuneTime = 14f;  // 냉각 후 그 포탑 재과열 면역
    // 밸런스 1차: 25 -> 30. 60초 웨이브 기준 왕복 2.4회 -> 2.0회,
    // 보스전(90~120초)은 4회 -> 3회 (낙뢰 마비 대응과 겹치는 피로 완화)
    public static float OverheatGlobalGap = 30f;   // 기차 전체 과열 최소 간격 (빈도 상한)

    /// <summary>카메라: 셰프 소프트 팔로우 (B-2)</summary>
    public static bool CamFollowChef = true;
    public static float CamDefaultZoom = 8.5f;     // 기본 줌 (7 -> 8.5, 긴 기차 프레이밍)
    public static float CamDeadzone = 1.5f;        // 이 거리까지는 카메라가 안 따라온다
    public static float CamFollowLerp = 4f;        // 따라오는 속도
    public static float CamFollowMinX = -2.5f;     // 카메라 이동 한계 (전장이 화면 밖으로 안 나가게)
    public static float CamFollowMaxX = 4.5f;

    /// <summary>갑판 전리품 상자 (아이템 획득이 상자로 떨어짐 - 밟아서 회수. false=즉시 지급)</summary>
    public static bool DeckLootEnabled = true;
    public static float DeckLootY = -1.25f;        // 상자가 놓이는 갑판 높이
    public static float DeckLootPickupRange = 0.9f;

    // ==================================================================
    //  B-3: 작살포 + 기관차 레버 (방향결정 2026-08-31 - 이중 페르소나 완성)
    // ==================================================================

    /// <summary>작살포 (기관차 앞): 지나가는 자원 바위를 [E]로 낚는다 (false=끔)</summary>
    public static bool HarpoonEnabled = true;
    public static float HarpoonX = -5.8f;          // 거치대 위치
    public static float HarpoonReach = 1.2f;       // 조작 근접 반경
    public static float HarpoonRange = 14f;        // 작살 사거리 (바위 탐색)
    public static float HarpoonCooldown = 12f;
    // 밸런스 1차: 3~5 -> 2~4. 희소 재료(전기/화염/얼음/독)를 골라 낚는 게 작살의 가치라
    // 평균 4개/12초는 디버프 요리 재료가 항상 남아도는 수준이었다 (기대값 하향)
    public static int HarpoonMatMin = 2;           // 명중 보상 재료 수
    public static int HarpoonMatMax = 4;
    public static float HarpoonAggroChance = 0.25f; // 원안의 리트리벌 리스크
    public static int HarpoonAggroMin = 1;
    public static int HarpoonAggroMax = 3;         // 밸런스 1차: 2 -> 3 (도박은 화끈하게)

    /// <summary>자원 바위: 전투 중 길가를 흘러가는 표적</summary>
    public static float RockSpawnIntervalMin = 9f;
    public static float RockSpawnIntervalMax = 16f;
    public static int RockMaxAlive = 2;
    public static float RockSpeed = 3.2f;          // 왼쪽으로 흐르는 속도
    public static float RockY = -2.55f;            // 길가 높이 (데크 아래)

    /// <summary>기관차 레버: 순항 <-> 전속 토글 (false=끔)</summary>
    public static bool LeverEnabled = true;
    public static float LeverX = -3.2f;            // 레버 위치 (기관차 뒤쪽 = 운전석)
    public static float LeverReach = 1.2f;
    public static float LeverSpawnMul = 0.65f;     // 전속: 적 스폰 간격 배율 (-35%)
    public static float LeverJudgePenalty = 0.10f; // 전속: 조리 판정 존 -10%
    public static float LeverParallaxMul = 1.8f;   // 전속: 주행 연출 가속
    // 밸런스 1차: 전속의 보상 신설. 기존엔 "웨이브가 빨리 끝난다"뿐이라 판정 페널티만
    // 체감되는 함정 레버였다 - 전속 중 처치 골드 +25%로 리턴을 눈에 보이게 (Enemy.Die 적용)
    public static float LeverGoldMul = 1.25f;

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
