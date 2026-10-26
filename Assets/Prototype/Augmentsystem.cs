using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// [AugmentSystem.cs] v4.14 (v9.35.2 2026-10-26 유저 "증강설명 폭발반경이나 이런거 좀 더 친절하게 - 어떤 종류에 적용이 되는지 알아야 집든말든하지": 설명 66개를 '어디에 걸리나'가 보이게 새로 (밸런스 표 '증강' 시트) / 카드 맨 아래 줄 AugmentTargets.Note - 지금 내 포탑 중 몇 기에 걸리나 / '부식성 위산'은 깎기 겹 상한 +2 (ShredCapAdd - 예전엔 실버와 같은 효과), 깎기 증강 둘은 겹쳐 집기 없음, 부식성 위산 뒤엔 구리 도금 탄환이 후보에 안 나온다 / AugmentHooks 장부 정리는 1초에 한 번 / 공격속도 -> 공격 속도) / v4.13 (v9.35 2026-10-25 성장·구조: 증강 등급 확률은 옛 25웨이브 자 (GameBalance.EqWave) / '야전 정비반' 웨이브마다 최대 HP x 웨이브 한 판의 몫 (지역 5 = +50 -> +80)) / v4.12 (v9.34 2026-10-25: '선대의 기본기' 글의 "새로 놓으면 레벨 2 부터"는 GameBalance.StartLevelBonusOn 일 때만 - 새 포탑은 늘 Lv1. 피해 % 는 BasicsT1Bonus 에서 읽는다) / v4.11 (v9.25 2026-10-16 B1 증강 덧셈 - 5차 피드백 3·10·11번 "너무 쉬움 / 명예도 안 쓰고 깸 / 무기 하나로 깸": 피해·화상독·폭발 반경·사거리·재료·공격속도 증강은 같은 종류끼리 % 를 더해 배율을 만든다 (AddAtk 등 - 피해 +12% 다섯 장 = +60%, 예전엔 곱 + 점증으로 x3.9). 같은 증강을 또 집어도 같은 만큼만 (StackTimes = 1). 증강 카드·중첩 알림에 그 종류의 지금 합계 (StackSummary). GameBalance.AugmentLinear = false 면 예전) / v4.10 (v9.24.1 2026-10-16 5차 피드백 6번 "실버 공속증강 사기": 고속 회전 모터는 점증 없이 집을 때마다 x1.12 (GameBalance.AugmentNoEscalate - StackTimes), 증강으로 붙는 공격속도는 합쳐서 GameBalance.AugmentAspdCap 배까지 (오버차지 엔진 포함 - 상한 없이 곱이 쌓여 다섯 번이면 x3.9 였다)) / v4.9 (v9.21.2 2026-10-14 문구 팩: 용어 통일 - 접시->요리(개) / 데미지->피해 / 슬롯->칸 / 랜덤->무작위 / 레시피->요리 / 적·아군->손님 / 드랍->드롭 / 마법 저항->저항 / 힐러·러시·청구 / 진화 조리 판 이름) / v4.8 (v9.19.3 2026-10-07: 육수 한 국자·회복의 만찬 설명에 "(1초에 N번까지)" - 타격당 회복을 1초에 GameBalance.LifestealHitsPerSec 번까지만 센다. LifestealDesc) / v4.7 (v9.19.2 2026-10-07: 육수 한 국자·회복의 만찬 설명에서 v4.6 의 "최대 … (떼로 온 손님은 덜)"을 뺐다 - 때릴 때마다 적힌 만큼 회복한다) / v4.6 (v9.19.1: 그 문구를 넣었다) / v4.5 (v9.18 2026-10-06: 선대의 기본기 설명에 "기본 요리 최대 레벨" - 레벨 상한이 돌아오면서 이 증강은 기본 요리 상한을 올려 준다(GameBalance.BasicsT1MaxLevel)) / v4.4 (v9.13 2026-09-23: AugmentDatabase.ForceGrade - 선로 보상 "증강 1회 더" 의 등급 고정) / v4.3 (v9.11.1 2026-09-22 문구: 이번 운행·유물·진화) / v4.2 (v9.10 2026-09-17 테스터 피드백 "증강에 모르겠는 말": 첫 지역은 바로 이해되는 증강만 후보(EARLY_SIMPLE, GameBalance.AugmentSimpleEarly) / 설명 낱말 일상어 - 도트·스택·인접 버프·공명·DEF·감쇄) / v4.1 (2026-09-14: 중첩 배수 상한 AugmentStackMulCap) / v4
/// 로그라이크 증강 시스템 (기획 C) - 창의적 증강 재설계판
///
/// 설계 철학
///  - 실버  : 무난한 수치 강화 (안전한 선택지)
///  - 골드  : 조건부 시너지 - "이걸 먹었으니 이렇게 운영하자"가 생기는 효과
///  - 프리즘 : 공격 방식 자체를 비트는 효과 - 빌드의 축이 되는 유물급
///  - v3 추가: 하이리스크/하이리턴 증강 + [도박] 패밀리 시너지
///    ([도박] 태그 증강을 모을수록 도박 계열 효과가 강해진다)
///  - v4 (Phase 2-3, 사용자 결정 - 증강/아이템 이원화):
///    주방 관여 증강 5종(식칼/황금 조리 기구/보험/부채질/미끄럼 매트)을
///    아이템(ItemSystem.cs)으로 이관하고, 판도를 바꾸는 신규 증강 10종을 추가.
///    증강 = 포탑 강화 + 기차 유틸만 남긴다.
///
/// 다른 시스템은 AugmentManager의 static 값 / 플래그만 읽으면 된다.
/// 실제 전투 반영은 TurretAttackExecutor v4가 담당.
///
/// VS 2017 (C# 7.3) 호환
/// </summary>

public enum AugmentGrade
{
    Silver,     // 실버 - 수치 강화
    Gold,       // 골드 - 조건부 시너지
    Prismatic   // 프리즘 - 플레이 방식 변형
}

/// <summary>증강 1종의 정의</summary>
public class AugmentData
{
    public string id;                  // 내부 식별자 (중복 방지)
    public string name;                // 표시 이름
    public string desc;                // 표시 설명
    public AugmentGrade grade;         // 등급
    public bool stackable;             // 중복 획득 허용 여부
    public string conflictId;          // 같이 가질 수 없는 증강 id (없으면 null)
    public string family;              // 패밀리 태그 (예: "도박") - 시너지 카운트용
    public System.Action apply;        // 획득 시 실행되는 효과

    public AugmentData(string id, string name, string desc, AugmentGrade grade,
        bool stackable, System.Action apply, string conflictId = null, string family = null)
    {
        this.id = id;
        this.name = name;
        this.desc = desc;
        this.grade = grade;
        this.stackable = stackable;
        this.apply = apply;
        this.conflictId = conflictId;
        this.family = family;
    }

    /// <summary>등급별 카드 색상</summary>
    public Color GradeColor()
    {
        switch (grade)
        {
            case AugmentGrade.Silver: return new Color(0.72f, 0.76f, 0.80f);
            case AugmentGrade.Gold: return new Color(0.95f, 0.76f, 0.30f);
            default: return new Color(0.72f, 0.55f, 0.95f);
        }
    }

    /// <summary>등급 표시 문자열</summary>
    public string GradeName()
    {
        switch (grade)
        {
            case AugmentGrade.Silver: return "실버";
            case AugmentGrade.Gold: return "골드";
            default: return "프리즘";
        }
    }
}


/// <summary>
/// 획득한 증강의 결과(배율/플래그)를 전역으로 제공한다.
/// </summary>
public static class AugmentManager
{
    // ---------- 수치 배율 (실버/골드) ----------
    public static float AtkMul = 1f;             // 포탑 데미지 배율
    public static float CritChanceAdd = 0f;      // 치명타 확률 (0.08 = 8%)
    public static float CritDamageAdd = 0f;      // 치명타 추가 배율 (기본 1.5배에 가산)
    public static float ExplodeRadiusMul = 1f;   // 폭발 반경 배율
    public static int ChainCountAdd = 0;         // 연쇄 전이 횟수 가산
    public static float DotMul = 1f;             // 화상/중독 도트 배율
    public static int ShredAdd = 0;              // 방깎/마깎 수치 가산
    public static int ShredCapAdd = 0;           // v4.14: 부식성 위산 - 깎기 겹 상한 가산 (Enemy: GameBalance.ShredMaxStacks + 이것)
    public static float LifestealPerHit = 0f;    // 타격당 기차 회복량
    public static float HealPerWave = 0f;        // 웨이브 클리어 시 회복량

    // ---------- 조건부 시너지 (골드) ----------
    public static float DotTargetBonus = 0f;     // 도트 걸린 적에게 추가 데미지 (0.25 = +25%)
    public static float ControlTargetBonus = 0f; // 슬로우/스턴 적에게 추가 데미지
    public static int StaticNth = 0;             // N번째 타격마다 감전 (0 = 비활성)

    // ---------- 주방 이벤트 관련 ----------
    // Phase 2-3: 담당이 아이템(ItemManager)으로 이관됨. 어떤 증강도 더 이상 이 값을
    // 바꾸지 않지만, 훅(KitchenEventManager) 호환을 위해 필드는 유지한다 (항상 1)
    public static float EventPenaltyMul = 1f;    // 이벤트 실패 페널티 배율
    public static float EventRewardMul = 1f;     // 이벤트 성공 보상 배율
    public static float EventIntervalMul = 1f;   // 이벤트 발생 간격 배율 (0.5 = 2배 자주)

    // ---------- 플레이 변형 플래그 (프리즘) ----------
    public static bool GamblerBullet = false;    // 모든 데미지 50% 확률 2배/절반
    public static bool PierceConversion = false; // 투사체 -> 관통 레일 변환
    public static bool ConeConversion = false;   // 투사체 -> 부채꼴 산탄 변환
    public static bool RedKitchen = false;       // 모든 타격에 화상 1스택
    public static bool IceHeart = false;         // 모든 슬로우 -> 0.8초 빙결(스턴)
    public static bool DoubleExplosion = false;  // 폭발이 한 번 더 터짐
    public static bool ChainAmplify = false;     // 연쇄가 튕길수록 강해짐
    public static float ChainProcChance = 0f;    // 모든 타격 확률로 소형 연쇄 번개
    public static bool RampAttack = false;       // 과열 기관: 연속 사격 시 데미지 상승
    public static bool FrostShatter = false;     // 동상 파편: CC 걸린 적 타격 시 서리 폭발
    public static bool OpeningBarrage = false;   // 개전 포격: 웨이브 시작 8초간 데미지 2배
    public static float WaveStartTime = -999f;   // 웨이브 시작 시각 (WaveManager가 기록)

    // ---------- 추가 골드 효과 ----------
    public static bool FullSplash = false;       // 폭발 전문가: 스플래시 감쇄 제거
    public static float DoubleTapChance = 0f;    // 2연장 개조: 확률로 한 발 더 (50% 데미지)
    public static float MaxHPPerWave = 0f;       // 야전 정비반: 웨이브당 최대 HP 영구 증가
    public static int ExtraCards = 0;            // 행운의 부적: 증강 선택지 추가 (최대 +2)

    // ---------- 하이리스크 / 도박 패밀리 (v3) ----------
    public static bool PrimalPower = false;      // 원시 화력: 상태이상 전부 포기, 순수 데미지 +80%
    public static int ReviveCharges = 0;         // 아홉 개의 목숨: 기차 완파 시 부활 횟수
    public static bool BloodBet = false;         // 출혈 배팅: 웨이브 시작마다 기차 HP -80
    public static float GoldRewardMul = 1f;      // 웨이브 골드 보상 배율 (고리대금업자가 낮춤)
    public static bool HasChalice = false;       // 도박사의 성배: 도박 증강 1개당 데미지 +10%
    public static int GamblerFamilyCount = 0;    // 보유한 [도박] 패밀리 증강 수

    /// <summary>도박사의 탄환 2배 확률: 기본 50% + 도박 증강 1개당 +4%p (최대 70%)</summary>
    public static float GamblerWinChance
    {
        get { return Mathf.Min(0.7f, 0.5f + 0.04f * GamblerFamilyCount); }
    }

    /// <summary>도박사의 성배 배율 (동적 계산 - 이후에 도박 증강을 먹어도 반영)</summary>
    public static float ChaliceMul
    {
        get { return HasChalice ? 1f + 0.10f * GamblerFamilyCount : 1f; }
    }

    // ---------- 방어 (TrainManager v3에서 연동됨) ----------
    public static float DamageReductionAdd = 0f; // 받는 피해 감소 (0.15 = 15% 감소, 음수 = 더 받음)

    // ---------- 포탑 성능 (TurretSlot v2에서 연동됨) ----------
    public static float AspdMul = 1f;            // 공격속도 배율
    public static float RangeMul = 1f;           // 사거리 배율

    // ---------- 조리 (CookingMinigame v2에서 연동됨) ----------
    // Phase 2-3: 담당이 아이템(ItemManager)으로 이관됨 - 필드는 훅 호환용 유지 (항상 1)
    public static float CookSpeedMul = 1f;       // 제한 시간 배율 (클수록 여유, 굽기 커서 감속)
    public static float CookJudgeMul = 1f;       // 판정 존 배율 (클수록 관대)

    // ---------- 파밍 (Enemy v3에서 연동됨) ----------
    public static float MaterialDropMul = 1f;    // 처치 시 재료 드랍량 배율

    // ---------- 슬롯 (TurretSlotManager v2에서 연동됨) ----------
    public static int ExtraSlotUnlock = 0;       // 추가 해금 슬롯 수 (기본 6 + 이 값, 최대 8)

    // ---------- 인접 버프 (TurretSlotManager v2에서 연동됨) ----------
    public static float AdjacentBuffMul = 1f;    // 인접 슬롯 버프 배율

    // ---------- 속성 공명 (TurretSlotManager v3에서 연동됨) ----------
    public static float ResonanceBonusAdd = 0f;  // 공명 보너스 가산 (0.15 = +15%p)

    // ---------- Phase 2-2 신규 증강 연동 ----------
    public static int RerollsUsed = 0;           // 증강 리롤 사용 횟수 (비용 점증용)
    public static float MasteryAmp = 1f;         // 단골 장부: 요리 숙련 공격력 보너스 배율
    public static float SlotStunDurMul = 1f;     // 부동액 배관: 슬롯 감전/빙결 지속 배율
    public static int ClearMatBonus = 0;         // 비상 식량 창고: 웨이브 클리어 랜덤 재료
    public static float BetStakesMul = 1f;       // 판돈 두 배: 스피노 베팅 판돈/보상/대가 배율
    public static int ResonanceNeedOverride = 0; // 공명 폭주: 공명 필요 개수 덮어쓰기 (0=기본)
    public static bool LastSupperRush = false;   // 최후의 만찬: 저체력 시 포탑 공속 상승

    // ---------- Phase 2-3 신규 증강 연동 ----------
    public static int ApprenticeCooks = 0;       // 견습 셰프 고용: 웨이브 시작 시 자동 조리 수
    public static bool CorpseService = false;    // 마지막 서비스: 처치한 적이 터진다
    public static bool OverkillCarry = false;    // 옆 테이블 계산서: 초과 데미지 이월
    public static int ThornsStacks = 0;          // 가시철조망 도금: 반격 스택 (0 = 없음)
    public static float TrainDefAdd = 0f;        // 가시철조망 도금: 기차 DEF 가산
    public static bool HasCollector = false;     // 골동품 감정가: 아이템 1개당 데미지 증가
    public static bool SteelHeart = false;       // 강철의 심장: 최대 HP 비례 데미지
    public static bool BasicsDoctrine = false;   // 선대의 기본기: T2 봉인 + T1 강화
    public static bool OneChef = false;          // 주방장은 하나다: 최고 레벨 포탑 몰아주기
    public static int OneChefKillStacks = 0;     // 주방장 처치 누적 스택 (런 한정)
    public static bool OverflowShield = false;   // 넘치는 솥: 초과 회복 -> 증기 보호막
    public static bool CurationBoost = false;    // 엄선된 메뉴판: 카드 -1장, 상위 등급 확률 상승

    /// <summary>골동품 감정가 배율 (동적 계산 - 이후에 아이템을 얻어도 반영)</summary>
    public static float CollectorMul
    {
        get { return HasCollector ? 1f + GameBalance.CollectorPerItem * ItemManager.OwnedCount : 1f; }
    }

    /// <summary>지금까지 획득한 증강 목록 (UI 표시용)</summary>
    public static List<AugmentData> Owned = new List<AugmentData>();

    /// <summary>런 시작 시 초기화 (static은 씬 재시작에도 남으므로 반드시 호출)</summary>
    public static void ResetRun()
    {
        AtkMul = 1f; CritChanceAdd = 0f; CritDamageAdd = 0f;
        atkPct = 0f; dotPct = 0f; explodePct = 0f; rangePct = 0f; dropPct = 0f; aspdPct = 0f;   // v4.11: 덧셈 합
        ExplodeRadiusMul = 1f; ChainCountAdd = 0;
        DotMul = 1f; ShredAdd = 0; ShredCapAdd = 0;
        LifestealPerHit = 0f; HealPerWave = 0f;

        DotTargetBonus = 0f; ControlTargetBonus = 0f; StaticNth = 0;

        EventPenaltyMul = 1f; EventRewardMul = 1f; EventIntervalMul = 1f;

        GamblerBullet = false; PierceConversion = false; ConeConversion = false;
        RedKitchen = false; IceHeart = false; DoubleExplosion = false;
        ChainAmplify = false; ChainProcChance = 0f;
        RampAttack = false; FrostShatter = false; OpeningBarrage = false;
        WaveStartTime = -999f;
        FullSplash = false; DoubleTapChance = 0f; MaxHPPerWave = 0f; ExtraCards = 0;
        PrimalPower = false; ReviveCharges = 0; BloodBet = false;
        GoldRewardMul = 1f; HasChalice = false; GamblerFamilyCount = 0;

        AspdMul = 1f; RangeMul = 1f; CookSpeedMul = 1f; CookJudgeMul = 1f;
        MaterialDropMul = 1f; DamageReductionAdd = 0f; ExtraSlotUnlock = 0;
        AdjacentBuffMul = 1f; ResonanceBonusAdd = 0f;

        // Phase 2-2 신규
        RerollsUsed = 0; MasteryAmp = 1f; SlotStunDurMul = 1f;
        ClearMatBonus = 0; BetStakesMul = 1f; ResonanceNeedOverride = 0; LastSupperRush = false;

        // Phase 2-3 신규
        ApprenticeCooks = 0; CorpseService = false; OverkillCarry = false;
        ThornsStacks = 0; TrainDefAdd = 0f; HasCollector = false;
        SteelHeart = false; BasicsDoctrine = false;
        OneChef = false; OneChefKillStacks = 0;
        OverflowShield = false; CurationBoost = false;

        Owned.Clear();
        AugmentHooks.Clear();
        Debug.Log("[증강] 런 초기화 완료");
    }

    /// <summary>
    /// 증강 획득.
    /// Phase 2-2 (사용자 결정 - 중첩 점증): 같은 증강을 n번째로 집으면 이번 획득 효과가
    /// 기본의 n배 (효과 함수를 n회 실행). 스택 가능(stackable) 증강만 중복 등장하므로
    /// 일회성 증강(골드 지급/부활 등)은 영향 없음. 2026-09-14: 배수 상한 AugmentStackMulCap 추가.
    /// </summary>
    public static void Acquire(AugmentData aug)
    {
        if (aug == null) return;

        int stackNumber = CountAugment(aug.id) + 1;   // 이번이 몇 번째 스택인가

        Owned.Add(aug);

        // [도박] 패밀리 카운트 (시너지용) - apply보다 먼저 올려서 효과가 자기 자신을 포함
        if (aug.family == "도박") GamblerFamilyCount++;

        // 2026-09-14 (교수 피드백 C2, 사용자 결정): 점증은 유지하되 배수 상한 GameBalance.AugmentStackMulCap(기본 3)
        // - 3스택 = 1+2+3 = 6회 복리는 과했고, 상한 3이면 4스택째부터는 3배로 고정 (터지는 딜의 도파민은 남긴다)
        // v4.10 (v9.24.1): 점증 없는 증강(AugmentNoEscalate)은 늘 1번 - StackTimes 가 정한다 (증강 카드도 같은 값을 보여 준다)
        int times = StackTimes(aug.id, stackNumber);
        if (aug.apply != null)
            for (int k = 0; k < times; k++) aug.apply();

        if (stackNumber > 1)
        {
            string sum = StackSummary(aug.id);   // v4.11: 덧셈이면 그 종류의 지금 합계 ("지금 피해 +36%")
            UIManager.Instance?.ShowStatChange("[중첩 " + stackNumber + "] " + aug.name
                + (times > 1 ? " - 이번 효과 " + times + "배!" + (times < stackNumber ? " (최대 " + times + "배)" : "")
                             : (sum.Length > 0 ? " - " + sum : " - 효과를 한 번 더 받았다")));
        }

        Debug.Log("[증강] 획득: " + aug.name + " (" + aug.GradeName() + ") x" + stackNumber
            + (aug.family != null ? " [" + aug.family + "]" : ""));
    }

    /// <summary>
    /// v4.10 (v9.24.1): stackNumber 번째로 집은 증강의 효과를 몇 번 실행하나 - 점증(n번째 = n배, 최대 AugmentStackMulCap).
    /// GameBalance.AugmentNoEscalate 에 든 증강은 늘 1번. AugmentPickUI 의 "[보유 n개 - 이번 획득 효과 n배!]" 도 이 값을 쓴다
    /// </summary>
    public static int StackTimes(string id, int stackNumber)
    {
        if (GameBalance.AugmentLinear) return 1;   // v4.11 (v9.25 B1): 덧셈 - 또 집으면 같은 만큼만
        string[] noEscalate = GameBalance.AugmentNoEscalate;
        if (noEscalate != null)
            for (int i = 0; i < noEscalate.Length; i++)
                if (noEscalate[i] == id) return 1;
        return Mathf.Max(1, Mathf.Min(stackNumber, Mathf.Max(1, GameBalance.AugmentStackMulCap)));
    }

    /// <summary>v4.10 (v9.24.1): 증강 공격속도 배율을 곱하되 GameBalance.AugmentAspdCap 을 넘지 않게 (고속 회전 모터·오버차지 엔진이 같이 쓴다)</summary>
    public static void MulAspdCapped(float mul)
    {
        float cap = GameBalance.AugmentAspdCap > 1f ? GameBalance.AugmentAspdCap : float.MaxValue;
        AspdMul = Mathf.Min(AspdMul * mul, cap);
    }

    /// <summary>v4.10: 공속 증강 두 장의 배율 (설명·후보 거르기·카드 글이 같이 쓴다)</summary>
    public const float AspdMotorMul = 1.12f;       // 고속 회전 모터 (실버)
    public const float AspdOverdriveMul = 1.45f;   // 오버차지 엔진 (프리즘)

    /// <summary>v4.10: 공속 증강이면 그 배율, 아니면 0</summary>
    public static float AspdNominal(string id)
    {
        if (id == "silver_aspd") return AspdMotorMul;
        if (id == "prism_overdrive") return AspdOverdriveMul;
        return 0f;
    }

    /// <summary>v4.10: 이 공속 증강을 지금 집으면 실제로 붙는 몫 (0.12 = +12%). 공속 증강이 아니면 -1</summary>
    public static float AspdGainNow(string id)
    {
        float nominal = AspdNominal(id);
        if (nominal <= 0f) return -1f;
        float cap = GameBalance.AugmentAspdCap > 1f ? GameBalance.AugmentAspdCap : float.MaxValue;
        float now = Mathf.Max(0.01f, AspdMul);
        if (GameBalance.AugmentLinear) return Mathf.Max(0f, Mathf.Min(now + (nominal - 1f), cap) - now);   // v4.11: 덧셈 - 붙는 % 포인트
        return Mathf.Min(now * nominal, cap) / now - 1f;
    }

    /// <summary>v4.10: 상한 때문에 원래 몫보다 덜 붙으면 증강 카드에 붙일 한 줄 (아니면 빈 글)</summary>
    public static string AspdGainNote(string id)
    {
        float gain = AspdGainNow(id);
        if (gain < 0f || gain >= AspdNominal(id) - 1f - 0.005f) return "";
        return "[지금 집으면 공격 속도 +" + Mathf.RoundToInt(gain * 100f) + "%]";
    }

    /// <summary>v4.10: 공속 증강인데 남은 몫이 원래 효과의 절반도 안 되나 - 증강 후보에서 뺀다 (집어도 거의 안 붙는다. 오버차지는 사거리 -15% 만 남는다)</summary>
    public static bool AspdNearCap(string id)
    {
        float gain = AspdGainNow(id);
        return gain >= 0f && gain < (AspdNominal(id) - 1f) * 0.5f;
    }

    // ── v4.11 (v9.25 B1): 증강 덧셈 - 같은 종류의 % 를 더해 배율을 만든다 (GameBalance.AugmentLinear). 끄면 예전처럼 곱한다 ──
    private static float atkPct, dotPct, explodePct, rangePct, dropPct, aspdPct;   // 종류별 % 합 (0.12 = +12%)

    /// <summary>v4.11: 피해 배율에 pct 를 얹는다 (0.12 = +12%)</summary>
    public static void AddAtk(float pct) { AtkMul = Stack(ref atkPct, AtkMul, pct, 0.1f); }
    /// <summary>v4.11: 화상·독 배율</summary>
    public static void AddDot(float pct) { DotMul = Stack(ref dotPct, DotMul, pct, 0.1f); }
    /// <summary>v4.11: 폭발 반경 배율</summary>
    public static void AddExplode(float pct) { ExplodeRadiusMul = Stack(ref explodePct, ExplodeRadiusMul, pct, 0.3f); }
    /// <summary>v4.11: 사거리 배율 (오버차지 엔진은 -0.15)</summary>
    public static void AddRange(float pct) { RangeMul = Stack(ref rangePct, RangeMul, pct, 0.3f); }
    /// <summary>v4.11: 재료 드롭 배율</summary>
    public static void AddDrop(float pct) { MaterialDropMul = Stack(ref dropPct, MaterialDropMul, pct, 0.1f); }

    /// <summary>v4.11: 공격속도 - 덧셈이어도 상한(AugmentAspdCap)까지</summary>
    public static void AddAspd(float pct)
    {
        if (!GameBalance.AugmentLinear) { MulAspdCapped(1f + pct); return; }
        float cap = GameBalance.AugmentAspdCap > 1f ? GameBalance.AugmentAspdCap : float.MaxValue;
        aspdPct += pct;
        AspdMul = Mathf.Clamp(1f + aspdPct, 0.1f, cap);
    }

    /// <summary>덧셈이면 pool 에 더해 1 + pool, 아니면 지금 배율에 곱한다 (floor 아래로는 안 내려간다)</summary>
    private static float Stack(ref float pool, float current, float pct, float floor)
    {
        if (!GameBalance.AugmentLinear) return Mathf.Max(floor, current * (1f + pct));
        pool += pct;
        return Mathf.Max(floor, 1f + pool);
    }

    /// <summary>v4.11 (v9.25): 증강 카드의 보유 줄·중첩 알림 - 그 종류의 지금 합계 ("지금 피해 +48%"). 종류가 없는 증강은 빈 글</summary>
    public static string StackSummary(string id)
    {
        switch (id)
        {
            case "silver_atk": case "gold_atk": return "지금 피해 " + Pct(AtkMul - 1f);
            case "silver_dot": case "gold_dot": return "지금 화상·독 " + Pct(DotMul - 1f);
            case "silver_explode": case "gold_explode": return "지금 폭발 반경 " + Pct(ExplodeRadiusMul - 1f);
            case "silver_range": return "지금 사거리 " + Pct(RangeMul - 1f);
            case "silver_magnet": return "지금 재료 " + Pct(MaterialDropMul - 1f);
            case "silver_aspd": return "지금 공격속도 " + Pct(AspdMul - 1f);
            case "silver_crit": case "gold_crit": return "지금 치명타 " + Pct(CritChanceAdd);
            default: return "";
        }
    }

    private static string Pct(float v)
    {
        int p = Mathf.RoundToInt(v * 100f);
        return (p >= 0 ? "+" : "") + p + "%";
    }

    /// <summary>v4.10: 증강 카드 설명 꼬리 - "증강 공격속도는 합쳐서 +80% 까지" (상한을 끄면 빈 글)</summary>
    public static string AspdCapNote()
    {
        if (GameBalance.AugmentAspdCap <= 1f) return "";
        return ". 증강 공격 속도는 합쳐서 +" + Mathf.RoundToInt((GameBalance.AugmentAspdCap - 1f) * 100f) + "% 까지";
    }

    /// <summary>해당 증강을 이미 갖고 있는지</summary>
    public static bool HasAugment(string id)
    {
        for (int i = 0; i < Owned.Count; i++)
            if (Owned[i].id == id) return true;
        return false;
    }

    /// <summary>해당 증강의 보유 개수 (중첩 점증/카드 표기용)</summary>
    public static int CountAugment(string id)
    {
        int c = 0;
        for (int i = 0; i < Owned.Count; i++)
            if (Owned[i].id == id) c++;
        return c;
    }

    // ---------- 외부 연동 헬퍼 ----------

    /// <summary>기차 최대 HP를 영구 증가</summary>
    public static void AddTrainMaxHP(float amount)
    {
        TrainManager tm = Object.FindFirstObjectByType<TrainManager>();
        if (tm != null) tm.AddMaxHP(amount);
    }

    /// <summary>기차 즉시 회복</summary>
    public static void HealTrain(float amount)
    {
        TrainManager tm = Object.FindFirstObjectByType<TrainManager>();
        if (tm != null) tm.Heal(amount);
    }

    /// <summary>기차 스탯 재계산 요청 (가시철조망 DEF 가산 등을 즉시 반영)</summary>
    public static void RecalcTrain()
    {
        TrainManager tm = Object.FindFirstObjectByType<TrainManager>();
        if (tm != null) tm.RecalculateStats();
    }
}


/// <summary>
/// 증강 효과를 위해 적 상태(슬로우/스턴/도트)를 추적하는 보조 장부.
/// Enemy 스크립트를 수정하지 않고, 우리가 상태를 걸 때 직접 기록한다.
/// </summary>
public static class AugmentHooks
{
    // 적별 "언제까지 이 상태인지" 기록 (Time.time 기준)
    private static Dictionary<Enemy, float> controlUntil = new Dictionary<Enemy, float>();
    private static Dictionary<Enemy, float> dotUntil = new Dictionary<Enemy, float>();

    // v4.14 (v9.35.2): 정리는 장부마다 1초에 한 번, 지울 목록은 하나를 돌려 쓴다 (장판이 0.5초마다 손님 전부를 기록한다)
    private static readonly List<Enemy> removeBuf = new List<Enemy>();
    private static float nextControlCleanup, nextDotCleanup;

    public static void Clear()
    {
        controlUntil.Clear();
        dotUntil.Clear();
        nextControlCleanup = 0f;
        nextDotCleanup = 0f;
    }

    /// <summary>슬로우/스턴을 걸었을 때 호출</summary>
    public static void RegisterControl(Enemy en, float duration)
    {
        if (en == null) return;
        float until = Time.time + duration;
        float old;
        if (controlUntil.TryGetValue(en, out old) && old > until) return;
        controlUntil[en] = until;
        CleanupIfBig(controlUntil, ref nextControlCleanup);
    }

    /// <summary>화상/중독을 걸었을 때 호출</summary>
    public static void RegisterDot(Enemy en, float duration)
    {
        if (en == null) return;
        float until = Time.time + duration;
        float old;
        if (dotUntil.TryGetValue(en, out old) && old > until) return;
        dotUntil[en] = until;
        CleanupIfBig(dotUntil, ref nextDotCleanup);
    }

    /// <summary>지금 슬로우/스턴 상태인가</summary>
    public static bool IsControlled(Enemy en)
    {
        if (en == null) return false;
        float until;
        return controlUntil.TryGetValue(en, out until) && until > Time.time;
    }

    /// <summary>지금 도트(화상/중독)가 붙어 있는가</summary>
    public static bool HasDotTracked(Enemy en)
    {
        if (en == null) return false;
        float until;
        return dotUntil.TryGetValue(en, out until) && until > Time.time;
    }

    /// <summary>장부가 커지면 만료된 항목 정리 - v4.14: 장부마다 1초에 한 번 (nextTime)</summary>
    private static void CleanupIfBig(Dictionary<Enemy, float> dict, ref float nextTime)
    {
        if (dict.Count < 64 || Time.time < nextTime) return;
        nextTime = Time.time + 1f;
        removeBuf.Clear();
        foreach (KeyValuePair<Enemy, float> pair in dict)
            if (pair.Key == null || pair.Value <= Time.time) removeBuf.Add(pair.Key);
        for (int i = 0; i < removeBuf.Count; i++)
            dict.Remove(removeBuf[i]);
        removeBuf.Clear();
    }
}


/// <summary>
/// 전체 증강 목록. 웨이브 클리어 시 여기서 3개를 뽑아 제시한다.
/// </summary>
public static class AugmentDatabase
{
    private static List<AugmentData> all;

    public static List<AugmentData> All
    {
        get
        {
            if (all == null) Build();
            return all;
        }
    }

    /// <summary>
    /// v4.8: 타격당 회복 카드의 설명. v4.14 (v9.35.2): 1초에 실제로 차는 양으로 - "1초에 0.5 까지 (포탑이 몇 기든 합쳐서)".
    /// 제한(GameBalance.LifestealHitsPerSec)을 끄면(0) "맞힐 때마다 +n". 값을 바꿔도 카드가 실제 동작과 같은 말을 하게 여기서 만든다
    /// </summary>
    private static string LifestealDesc(float amount)
    {
        float perSec = GameBalance.LifestealHitsPerSec;
        if (perSec <= 0f) return "포탑이 손님을 맞힐 때마다 기차 HP +" + amount.ToString("0.#");
        return "포탑이 손님을 맞히면 기차 HP 회복 - 1초에 " + (amount * perSec).ToString("0.#") + " 까지 (포탑이 몇 기든 합쳐서)";
    }

    // ── v4.14 (v9.35.2): 설명 숫자 도우미 - 카드 글이 GameBalance 값과 같은 말을 하게 ──
    /// <summary>0.12 -> "12" (퍼센트 숫자)</summary>
    private static string P(float v) { return Mathf.RoundToInt(v * 100f).ToString(); }
    /// <summary>노리는 거리 x 배율 -> "23" (GameBalance.TurretRange 20 기준)</summary>
    private static string R(float mul) { return Mathf.RoundToInt(GameBalance.TurretRange * mul).ToString(); }
    /// <summary>초 -> "2.4"</summary>
    private static string S(float sec) { return sec.ToString("0.#"); }
    /// <summary>화상·독 증강이 붙는 곳 (화염 방벽 오라 불을 화상으로 치면 그것도)</summary>
    private static string DotWhere()
    {
        return GameBalance.AuraBurnIsDot ? "화상·독 요리와 화염 방벽에 붙는다" : "화상·독 요리에 붙는다 (화염 방벽 불은 그대로)";
    }
    /// <summary>깎기 증강이 붙는 곳 (부식의 정수 오라가 진짜 깎으면 그것도)</summary>
    private static string ShredWhere()
    {
        return GameBalance.AuraShredReal ? "깎기 요리와 부식의 정수에 붙는다" : "깎기 요리(독침 육포 · 마비독 꼬치 등)에 붙는다";
    }

    private static void Build()
    {
        all = new List<AugmentData>();

        // ==========================================================
        //  실버 - 무난한 수치 강화 (안전픽)
        // ==========================================================
        all.Add(new AugmentData("silver_atk", "기름칠한 포신",
            "쏘는 포탑 피해 +12% (쏘지 않는 요리와 화상·독엔 안 붙는다)", AugmentGrade.Silver, true,
            delegate { AugmentManager.AddAtk(0.12f); }));

        all.Add(new AugmentData("silver_hp", "강철 리벳 보강",
            "기차 최대 HP +250, 그만큼 바로 찬다", AugmentGrade.Silver, true,
            delegate { AugmentManager.AddTrainMaxHP(250f); AugmentManager.HealTrain(250f); }));

        all.Add(new AugmentData("silver_crit", "정밀 렌즈",
            "쏘는 포탑 한 발이 8% 확률로 치명타 - 피해 1.5배 (치명타는 증강으로만 생긴다)", AugmentGrade.Silver, true,
            delegate { AugmentManager.CritChanceAdd += 0.08f; }));

        all.Add(new AugmentData("silver_dot", "매운 양념 한 스푼",
            "화상·독으로 계속 들어가는 피해 +25% - " + DotWhere(), AugmentGrade.Silver, true,
            delegate { AugmentManager.AddDot(0.25f); }));

        all.Add(new AugmentData("silver_explode", "화약 추가 배합",
            "폭발 요리의 폭발 범위 +15% (용암 폭탄밥 · 증기 폭발처럼 터지는 요리만)", AugmentGrade.Silver, true,
            delegate { AugmentManager.AddExplode(0.15f); }));

        all.Add(new AugmentData("silver_shred", "구리 도금 탄환",
            "방어·저항 깎기가 첫 발에 바로 끝까지 (-" + (GameBalance.ShredMaxStacks * 15) + ") - " + ShredWhere(), AugmentGrade.Silver, false,
            delegate { AugmentManager.ShredAdd += 5; }));

        all.Add(new AugmentData("silver_lifesteal", "육수 한 국자",
            LifestealDesc(0.5f), AugmentGrade.Silver, true,
            delegate { AugmentManager.LifestealPerHit += 0.5f; }));

        all.Add(new AugmentData("silver_wavehal", "응급 정비",
            "웨이브를 넘길 때마다 기차 HP 60 회복", AugmentGrade.Silver, true,
            delegate { AugmentManager.HealPerWave += 60f; }));

        all.Add(new AugmentData("silver_aspd", "고속 회전 모터",
            "쏘는 포탑이 12% 더 자주 쏜다 (화상·독도 그만큼 자주 붙는다)" + AugmentManager.AspdCapNote(), AugmentGrade.Silver, true,
            delegate { AugmentManager.AddAspd(AugmentManager.AspdMotorMul - 1f); }));   // v4.11: 덧셈 + 상한 (v4.10: 점증 없음)

        all.Add(new AugmentData("silver_range", "망원 조준경",
            "포탑이 노리는 거리 +15% (" + R(1f) + " -> " + R(1.15f) + ")"
                + (GameBalance.PierceFollowsAim ? ", 관통 줄기도 같이 길어진다" : "")
                + (GameBalance.ConeAimWithinReach ? ". 부채꼴 요리엔 안 붙는다 (불길이 닿는 " + Mathf.RoundToInt(GameBalance.ConeReach) + " 안만 노린다)" : ". 부채꼴 불길은 그대로")
                + ". 오라 거리는 그대로", AugmentGrade.Silver, true,
            delegate { AugmentManager.AddRange(0.15f); }));

        // (Phase 2-3: '잘 드는 식칼'은 아이템으로 이관 - ItemSystem.cs 참고)

        all.Add(new AugmentData("silver_magnet", "자석 흡입기 개조",
            "손님을 잡으면 떨어지는 재료 +25%", AugmentGrade.Silver, true,
            delegate { AugmentManager.AddDrop(0.25f); }));

        // ==========================================================
        //  골드 - 조건부 시너지 ("이걸 먹었으니 이렇게 운영하자")
        // ==========================================================
        all.Add(new AugmentData("gold_atk", "볼케이노 압축기",
            "쏘는 포탑 피해 +30% (쏘지 않는 요리와 화상·독엔 안 붙는다)", AugmentGrade.Gold, true,
            delegate { AugmentManager.AddAtk(0.30f); }));

        all.Add(new AugmentData("gold_dot", "매운맛 중독",
            "화상·독으로 계속 들어가는 피해 +60% - " + DotWhere(), AugmentGrade.Gold, true,
            delegate { AugmentManager.AddDot(0.60f); }));

        all.Add(new AugmentData("gold_chain", "전격의 협곡",
            "연쇄 번개 요리(과부하 코일 · 테슬라 갓 핑거 · 신경독 코일)의 번개가 2마리 더 튄다", AugmentGrade.Gold, true,
            delegate { AugmentManager.ChainCountAdd += 2; }));

        all.Add(new AugmentData("gold_explode", "화염의 포효",
            "폭발 요리의 폭발 범위 +35% (터지는 요리만)", AugmentGrade.Gold, true,
            delegate { AugmentManager.AddExplode(0.35f); }));

        all.Add(new AugmentData("gold_shred", "부식성 위산",
            "방어·저항을 더 깊이 깎는다 - 끝 -" + (GameBalance.ShredMaxStacks * 15) + " -> -" + ((GameBalance.ShredMaxStacks + GameBalance.GoldShredCapAdd) * 15) + ", 첫 발에 바로 끝까지. " + ShredWhere(), AugmentGrade.Gold, false,
            delegate { AugmentManager.ShredAdd += 12; AugmentManager.ShredCapAdd = Mathf.Max(AugmentManager.ShredCapAdd, GameBalance.GoldShredCapAdd); }));

        all.Add(new AugmentData("gold_crit", "헤드샷 프로토콜",
            "쏘는 포탑 치명타 확률 +15%, 치명타 피해 1.5배 -> 2배", AugmentGrade.Gold, true,
            delegate { AugmentManager.CritChanceAdd += 0.15f; AugmentManager.CritDamageAdd += 0.50f; }));

        all.Add(new AugmentData("gold_lifesteal", "회복의 만찬",
            LifestealDesc(2f), AugmentGrade.Gold, true,
            delegate { AugmentManager.LifestealPerHit += 2f; }));

        all.Add(new AugmentData("gold_fortress", "강철의 요새",
            "기차 최대 HP +1000, 그만큼 바로 찬다", AugmentGrade.Gold, false,
            delegate { AugmentManager.AddTrainMaxHP(1000f); AugmentManager.HealTrain(1000f); }));

        all.Add(new AugmentData("gold_nanoarmor", "나노 수복 장갑",
            "기차가 받는 피해 -15%", AugmentGrade.Gold, true,
            delegate { AugmentManager.DamageReductionAdd += 0.15f; }));

        // (Phase 2-3: '황금 조리 기구'는 아이템으로 이관 - ItemSystem.cs 참고)

        // --- 여기부터 시너지형 골드 ---
        all.Add(new AugmentData("gold_static", "정전기 축적",
            "같은 요리 포탑이 손님을 4번 맞힐 때마다 0.4초 마비 (폭발 파편 · 번개 튐도 센다)", AugmentGrade.Gold, false,
            delegate { AugmentManager.StaticNth = 4; }));

        all.Add(new AugmentData("gold_weakpoint", "약점 파고들기",
            "화상·독이 붙은 손님에게 쏘는 포탑 피해 +25% - 화상·독을 거는 포탑이 있어야 한다", AugmentGrade.Gold, true,
            delegate { AugmentManager.DotTargetBonus += 0.25f; }));

        all.Add(new AugmentData("gold_hunter", "사냥꾼의 본능",
            "느려지거나 마비된 손님에게 쏘는 포탑 피해 +30% - 감속·마비를 거는 포탑이 있어야 한다", AugmentGrade.Gold, true,
            delegate { AugmentManager.ControlTargetBonus += 0.30f; }));

        // (Phase 2-3: '보험 계약'/'부채질 장인'은 아이템으로 이관 - ItemSystem.cs 참고)

        all.Add(new AugmentData("gold_fullsplash", "폭발 전문가",
            "폭발 요리의 폭발에 휘말린 손님도 100% 피해 (원래 80%)", AugmentGrade.Gold, false,
            delegate { AugmentManager.FullSplash = true; }));

        all.Add(new AugmentData("gold_doubletap", "2연장 개조",
            "한 손님을 맞히는 탄이 25% 확률로 한 발 더 (피해 50%) - 단일 · 폭발 · 장판 요리만 (관통 · 번개 · 부채꼴은 안 된다)", AugmentGrade.Gold, false,
            delegate { AugmentManager.DoubleTapChance = 0.25f; }));

        // v4.13 (v9.35): 웨이브가 줄어든 만큼(GameBalance.WaveWorth) 한 번에 더 - 같은 웨이브 자에서 옛 운행과 같은 최대 HP (지역 5 = +80). 응급 정비·식탁보의 회복은 그대로 (한 판의 피해가 옛 한 판과 같다)
        int fieldRepairHp = Mathf.RoundToInt(50f * GameBalance.WaveWorth);
        all.Add(new AugmentData("gold_fieldrepair", "야전 정비반",
            "웨이브를 넘길 때마다 기차 최대 HP +" + fieldRepairHp + " (이번 운행 동안 쌓인다)", AugmentGrade.Gold, true,
            delegate { AugmentManager.MaxHPPerWave += fieldRepairHp; }));

        all.Add(new AugmentData("gold_luckycharm", "행운의 부적",
            "앞으로 증강 후보가 1장 더 나온다 (최대 +2)", AugmentGrade.Gold, true,
            delegate { AugmentManager.ExtraCards = Mathf.Min(2, AugmentManager.ExtraCards + 1); },
            "prism_curation", "도박"));

        // --- 하이리스크 / 도박 패밀리 골드 ---
        all.Add(new AugmentData("gold_usurer", "고리대금업자",
            "바로 골드 +800. 대신 앞으로 웨이브 골드 -50%", AugmentGrade.Gold, false,
            delegate
            {
                if (GameManager.Instance != null) GameManager.Instance.AddGold(800);
                AugmentManager.GoldRewardMul *= 0.5f;
            },
            null, "도박"));

        all.Add(new AugmentData("gold_bloodbet", "출혈 베팅",
            "쏘는 포탑 피해 +35%. 대신 웨이브가 시작될 때마다 기차 HP -80", AugmentGrade.Gold, false,
            delegate { AugmentManager.BloodBet = true; AugmentManager.AddAtk(0.35f); },
            null, "도박"));

        all.Add(new AugmentData("gold_chalice", "도박사의 성배",
            "[도박] 증강 1개마다 쏘는 포탑 피해 +10% (이 증강도 세고, 나중에 집는 것도 센다)",
            AugmentGrade.Gold, false,
            delegate { AugmentManager.HasChalice = true; },
            null, "도박"));

        // ==========================================================
        //  프리즘 - 공격 방식 자체를 비튼다 (빌드의 축)
        // ==========================================================
        all.Add(new AugmentData("prism_rail", "열차포 개조",
            "단일 요리가 직선 관통 줄기로 바뀐다 - 한 줄의 손님이 전부 같은 피해 (폭발 · 장판 · 번개 · 부채꼴 요리는 그대로)", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.PierceConversion = true; },
            "prism_shotgun"));

        all.Add(new AugmentData("prism_shotgun", "산탄 셰프",
            "단일 요리가 앞쪽 부채꼴로 바뀐다 - 피해 -25%, 닿는 거리 " + Mathf.RoundToInt(GameBalance.ConeReach) + (GameBalance.ConeAimWithinReach ? " (가까이 온 손님만 노린다)" : " (먼 손님은 못 맞힌다)"), AugmentGrade.Prismatic, false,
            delegate { AugmentManager.ConeConversion = true; },
            "prism_rail"));

        all.Add(new AugmentData("prism_redkitchen", "붉은 주방",
            "쏘는 포탑의 모든 타격이 화상을 붙인다. 화상·독 피해 +50% (빨리 쏘는 요리일수록 세다)", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.RedKitchen = true; AugmentManager.AddDot(0.50f); },
            "prism_primal"));

        all.Add(new AugmentData("prism_iceheart", "얼음 심장",
            "감속 요리의 감속이 0.8초 얼려 멈추기로 바뀐다 (강감속 요리는 오히려 짧아진다" + (GameBalance.FieldLingerSec > 0f ? " · 장판은 처음 밟을 때 한 번" : "") + " · 빙벽 스튜 오라는 그대로)", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.IceHeart = true; }));

        all.Add(new AugmentData("prism_gambler", "도박사 스피노의 탄환",
            "쏘는 포탑 한 발이 50% 확률로 2배, 아니면 절반. [도박] 증강 1개마다 2배 확률 +4% (최대 70%)",
            AugmentGrade.Prismatic, false,
            delegate { AugmentManager.GamblerBullet = true; },
            null, "도박"));

        all.Add(new AugmentData("prism_chainproc", "번개 계승",
            "쏘는 포탑의 타격이 20% 확률로 작은 번개를 튀긴다 - 곁 손님 둘에게 60%", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.ChainProcChance = 0.20f; }));

        all.Add(new AugmentData("prism_echo", "메아리치는 폭발",
            "폭발 요리의 폭발이 한 번 더 터진다 (피해 60%, 범위 1.2배)", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.DoubleExplosion = true; }));

        all.Add(new AugmentData("prism_chainamp", "증폭 전이",
            "연쇄 번개 요리의 번개가 튈 때마다 +20% 세진다 (원래는 튈 때마다 80%)", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.ChainAmplify = true; }));

        all.Add(new AugmentData("prism_ramp", "과열 기관",
            "쉬지 않고 쏘면 피해가 오른다 - 맞힐 때마다 +5%, 최대 +75%. 2.5초 쉬면 처음부터",
            AugmentGrade.Prismatic, false,
            delegate { AugmentManager.RampAttack = true; }));

        all.Add(new AugmentData("prism_shatter", "동상 파편",
            "느려지거나 마비된 손님을 맞히면 30% 확률로 서리가 터진다 - 곁 손님에게 50% (감속·마비를 거는 포탑이 있어야)",
            AugmentGrade.Prismatic, false,
            delegate { AugmentManager.FrostShatter = true; }));

        all.Add(new AugmentData("prism_opening", "개전 포격",
            "웨이브 시작 뒤 8초 동안 쏘는 포탑 피해 2배",
            AugmentGrade.Prismatic, false,
            delegate { AugmentManager.OpeningBarrage = true; }));

        // --- 하이리스크 프리즘 ---
        all.Add(new AugmentData("prism_primal", "원시 화력",
            "쏘는 포탑 피해 +80%. 대신 요리의 덤 효과(화상 · 독 · 감속 · 마비 · 깎기)가 전부 사라진다",
            AugmentGrade.Prismatic, false,
            delegate { AugmentManager.PrimalPower = true; },
            "prism_redkitchen"));

        all.Add(new AugmentData("prism_ninelives", "아홉 개의 목숨",
            "기차가 완파되면 한 번 되살아난다 (HP 800)", AugmentGrade.Prismatic, true,
            delegate { AugmentManager.ReviveCharges += 1; }));

        all.Add(new AugmentData("prism_glasscannon", "유리 대포",
            "쏘는 포탑 피해 +70%. 대신 기차가 받는 피해 +25%", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.AddAtk(0.70f); AugmentManager.DamageReductionAdd -= 0.25f; }));

        all.Add(new AugmentData("prism_overdrive", "오버차지 엔진",
            "쏘는 포탑이 45% 더 자주 쏜다. 대신 노리는 거리 -15% (" + R(1f) + " -> " + R(0.85f) + ")" + AugmentManager.AspdCapNote(), AugmentGrade.Prismatic, false,
            delegate { AugmentManager.AddAspd(AugmentManager.AspdOverdriveMul - 1f); AugmentManager.AddRange(-0.15f); }));   // v4.11: 덧셈 + 공격속도 상한

        all.Add(new AugmentData("prism_extraslot", "증축된 주방 칸",
            "포탑 칸 1개를 더 연다 (최대 8칸)", AugmentGrade.Prismatic, true,
            delegate { AugmentManager.ExtraSlotUnlock = Mathf.Min(2, AugmentManager.ExtraSlotUnlock + 1); }));

        all.Add(new AugmentData("gold_adjacent", "주방 동선 최적화",
            "이웃 강화 요리(지휘관의 만찬 · 축포의 연회 · 마법사의 만찬 - 셋 다 전설)의 강화 폭 1.5배", AugmentGrade.Gold, false,
            delegate { AugmentManager.AdjacentBuffMul *= 1.5f; }));

        all.Add(new AugmentData("gold_resonance", "속성 공명 증폭기",
            "같은 계열 포탑 " + GameBalance.ResonanceCount + "기 보너스(공명) +" + P(GameBalance.ResonanceBonus) + "% -> +" + P(GameBalance.ResonanceBonus + 0.15f) + "% (방어 계열 공명은 그대로)", AugmentGrade.Gold, true,
            delegate { AugmentManager.ResonanceBonusAdd += 0.15f; }));

        all.Add(new AugmentData("prism_allin", "올인",
            "지금 골드를 전부 잃는다. 잃은 골드 100마다 쏘는 포탑 피해 +4% (최대 +100%)",
            AugmentGrade.Prismatic, false,
            delegate
            {
                int lostGold = 0;
                if (GameManager.Instance != null)
                {
                    lostGold = GameManager.Instance.playerGold;
                    GameManager.Instance.SpendGold(lostGold);
                }
                float bonus = Mathf.Min(1.0f, (lostGold / 100) * 0.04f);
                AugmentManager.AddAtk(bonus);   // v4.11: 덧셈
                Debug.Log("[증강] 올인: 골드 " + lostGold + " 소모, 데미지 +" + Mathf.RoundToInt(bonus * 100f) + "%");
            },
            null, "도박"));

        // ==========================================================
        //  Phase 2-2 신규 8종 - 최근 시스템(숙련/기름/빙결/베팅/공명)과 시너지
        // ==========================================================

        // (Phase 2-3: '미끄럼 방지 매트'는 아이템으로 이관 - ItemSystem.cs 참고)

        all.Add(new AugmentData("silver_regular", "단골 장부",
            "단골 요리(많이 만든 요리)의 피해 보너스 1.5배 - 예: +8% -> +12%", AugmentGrade.Silver, true,
            delegate { AugmentManager.MasteryAmp += 0.5f; }));

        all.Add(new AugmentData("silver_armorpad", "질긴 장갑",
            "기차가 받는 피해 -5%", AugmentGrade.Silver, true,
            delegate { AugmentManager.DamageReductionAdd += 0.05f; }));

        all.Add(new AugmentData("gold_antifreeze", "부동액 배관",
            "포탑이 감전·빙결로 멈춰 있는 시간 -60% (감전 " + S(GameBalance.LightningStunSec) + " -> " + S(GameBalance.LightningStunSec * 0.4f) + "초, 빙결 " + S(GameBalance.FreezeSlotSec) + " -> " + S(GameBalance.FreezeSlotSec * 0.4f) + "초). 과열은 그대로", AugmentGrade.Gold, false,
            delegate { AugmentManager.SlotStunDurMul *= 0.4f; }));

        all.Add(new AugmentData("gold_pantry", "비상 식량 창고",
            "웨이브를 넘길 때마다 무작위 재료 +2", AugmentGrade.Gold, true,
            delegate { AugmentManager.ClearMatBonus += 2; }));

        all.Add(new AugmentData("gold_stakes", "판돈 두 배",
            "스피노 베팅의 판돈 · 성공 보상 · 실패 대가가 전부 2배", AugmentGrade.Gold, false,
            delegate { AugmentManager.BetStakesMul *= 2f; },
            null, "도박"));

        all.Add(new AugmentData("prism_resonance", "공명 폭주",
            "같은 계열 포탑 보너스(공명)가 3기가 아니라 2기부터 붙는다", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.ResonanceNeedOverride = 2; }));

        all.Add(new AugmentData("prism_lastsupper", "최후의 만찬",
            "기차 HP " + P(GameBalance.LastSupperHPRatio) + "% 이하일 때 쏘는 포탑 공격 속도 x" + GameBalance.LastSupperAspdMul.ToString("0.#") + " (증강 공격 속도 상한과 따로)", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.LastSupperRush = true; }));

        // ==========================================================
        //  Phase 2-3 신규 10종 - "판도를 바꾸는" 증강 대거 추가
        //  (아레나/롤토체스/StS 유물 레퍼런스를 우리 세계관으로 번안)
        // ==========================================================

        // --- 골드 5종 ---
        all.Add(new AugmentData("gold_apprentice", "견습 셰프 고용",
            "웨이브가 시작될 때 찾아낸 요리 중 하나를 1개 만들어 둔다", AugmentGrade.Gold, true,
            delegate { AugmentManager.ApprenticeCooks += 1; }));

        all.Add(new AugmentData("gold_lastservice", "마지막 서비스",
            "쓰러진 손님이 터진다 - 곁 손님에게 마지막 한 발 피해의 " + P(GameBalance.CorpseServiceRatio) + "%",
            AugmentGrade.Gold, false,
            delegate { AugmentManager.CorpseService = true; }));

        all.Add(new AugmentData("gold_overkill", "옆 테이블 계산서",
            "손님을 잡고 남은 피해가 가까운 손님에게 넘어간다 (한 방이 큰 요리일수록 좋다)", AugmentGrade.Gold, false,
            delegate { AugmentManager.OverkillCarry = true; }));

        all.Add(new AugmentData("gold_barbedwire", "가시철조망 도금",
            "기차 방어 +8. 기차를 문 손님이 방어만큼 되받는다",
            AugmentGrade.Gold, true,
            delegate
            {
                AugmentManager.TrainDefAdd += 8f;
                AugmentManager.ThornsStacks += 1;
                AugmentManager.RecalcTrain();
            }));

        all.Add(new AugmentData("gold_collector", "골동품 감정가",
            "가진 유물 1개마다 쏘는 포탑 피해 +" + P(GameBalance.CollectorPerItem) + "% (나중에 얻는 유물도 센다)",
            AugmentGrade.Gold, false,
            delegate { AugmentManager.HasCollector = true; }));

        // --- 프리즘 5종 ---
        all.Add(new AugmentData("prism_steelheart", "강철의 심장",
            "기차 최대 HP 100마다 쏘는 포탑 피해 +" + P(GameBalance.SteelHeartPer100) + "% (최대 +100%)",
            AugmentGrade.Prismatic, false,
            delegate { AugmentManager.SteelHeart = true; }));

        all.Add(new AugmentData("prism_basics", "선대의 기본기",
            "전설 요리 진화가 막힌다 (이미 있는 건 그대로). 대신 기본 요리 포탑 피해 +" + P(GameBalance.BasicsT1Bonus) + "%"
                + (GameBalance.StartLevelBonusOn ? ", 새로 놓으면 레벨 2 부터" : "")
                + (GameBalance.T1MaxLevel > 0 && GameBalance.BasicsT1MaxLevel > GameBalance.T1MaxLevel ? ", 최대 레벨 " + GameBalance.BasicsT1MaxLevel + " 까지 오른다" : ""),
            AugmentGrade.Prismatic, false,
            delegate { AugmentManager.BasicsDoctrine = true; }));

        all.Add(new AugmentData("prism_onechef", "주방장은 하나다",
            "가장 레벨 높은 포탑이 주방장 - 피해 +" + P(GameBalance.OneChefBonus) + "%, 그 포탑이 잡을 때마다 +" + P(GameBalance.OneChefPerKill)
                + "% (최대 +" + P(GameBalance.OneChefPerKill * GameBalance.OneChefMaxStacks) + "% 더). 다른 포탑은 피해 -" + P(GameBalance.OneChefOthersPenalty) + "%",
            AugmentGrade.Prismatic, false,
            delegate { AugmentManager.OneChef = true; }));

        all.Add(new AugmentData("prism_overflow", "넘치는 솥",
            "최대 HP를 넘는 회복은 증기 보호막으로 쌓인다 (최대 HP의 25%까지, 피해를 먼저 막는다)",
            AugmentGrade.Prismatic, false,
            delegate { AugmentManager.OverflowShield = true; }));

        all.Add(new AugmentData("prism_curation", "엄선된 메뉴판",
            "증강 후보가 2장으로 준다. 대신 골드 · 프리즘 증강이 훨씬 잘 나온다",
            AugmentGrade.Prismatic, false,
            delegate { AugmentManager.CurationBoost = true; },
            "gold_luckycharm"));

        Debug.Log("[증강] 데이터베이스 로드 완료 - 총 " + all.Count + "종");
    }

    /// <summary>
    /// 웨이브 수에 맞춰 등급 확률을 정하고, 중복/충돌을 피해 count개를 뽑는다.
    /// </summary>
    public static List<AugmentData> Roll(int waveNumber, int count)
    {
        List<AugmentData> result = new List<AugmentData>();
        List<AugmentData> pool = new List<AugmentData>();

        // v9.10: 첫 지역은 바로 이해되는 효과만 (개정안 §7 "초반 후보는 이미 쓰는 요리나 즉시 이해되는 효과로 제한")
        bool simpleOnly = GameBalance.AugmentSimpleEarly && waveNumber <= GameBalance.RegionLength;

        for (int i = 0; i < All.Count; i++)
        {
            AugmentData a = All[i];
            // 논스택 증강은 이미 가졌으면 제외
            if (!a.stackable && AugmentManager.HasAugment(a.id)) continue;
            // 충돌 증강(예: 레일건 개조 vs 산탄 셰프)은 상대를 가졌으면 제외
            if (a.conflictId != null && AugmentManager.HasAugment(a.conflictId)) continue;
            if (simpleOnly && System.Array.IndexOf(EARLY_SIMPLE, a.id) < 0) continue;
            if (AugmentManager.AspdNearCap(a.id)) continue;   // v4.10 (v9.24.1): 증강 공격속도가 상한 가까이면 공속 증강은 안 나온다
            if (a.id == "silver_shred" && AugmentManager.ShredCapAdd > 0) continue;   // v4.14 (v9.35.2): 부식성 위산 뒤엔 구리 도금 탄환이 더 하는 게 없다
            pool.Add(a);
        }
        if (pool.Count < count) { pool.Clear(); for (int i = 0; i < All.Count; i++) if ((All[i].stackable || !AugmentManager.HasAugment(All[i].id))) pool.Add(All[i]); }

        for (int n = 0; n < count && pool.Count > 0; n++)
        {
            AugmentGrade wanted = RollGrade(waveNumber);

            List<AugmentData> tier = new List<AugmentData>();
            for (int i = 0; i < pool.Count; i++)
                if (pool[i].grade == wanted) tier.Add(pool[i]);
            if (tier.Count == 0) tier = pool;

            AugmentData picked = tier[Random.Range(0, tier.Count)];
            result.Add(picked);
            pool.Remove(picked);

            // 같은 판에 충돌쌍이 동시에 나오는 것도 막는다
            if (picked.conflictId != null)
            {
                for (int i = pool.Count - 1; i >= 0; i--)
                    if (pool[i].id == picked.conflictId) pool.RemoveAt(i);
            }
        }

        return result;
    }

    /// <summary>v9.10: 첫 지역 후보 - 숫자 하나로 이해되는 것들 (베팅·숙련·공명·T2·도박 패밀리·조건부 계산은 지역 2 부터)</summary>
    private static readonly string[] EARLY_SIMPLE = {
        "silver_atk", "silver_hp", "silver_crit", "silver_explode", "silver_lifesteal", "silver_wavehal", "silver_aspd", "silver_range", "silver_magnet", "silver_armorpad",
        "gold_atk", "gold_explode", "gold_lifesteal", "gold_fortress", "gold_nanoarmor", "gold_fieldrepair", "gold_pantry", "gold_doubletap" };

    /// <summary>v9.13: 다음 Roll 의 등급을 고정 (-1 = 안 함). 선로 보상 "증강 1회 더 (은/금)" - AugmentPickUI.OpenExtra 가 걸고 푼다</summary>
    public static int ForceGrade = -1;

    /// <summary>웨이브가 올라갈수록 상위 등급이 잘 나온다. v9.13: ForceGrade 가 걸려 있으면 그 등급</summary>
    private static AugmentGrade RollGrade(int waveNumber)
    {
        if (ForceGrade >= 0) return (AugmentGrade)ForceGrade;

        float roll = Random.value;
        float eqWave = GameBalance.EqWave(waveNumber);   // v4.13 (v9.35): 옛 25웨이브 자 - 지역 5 의 웨이브 10 = 옛 16
        float prismChance = Mathf.Clamp(0.05f + eqWave * 0.015f, 0.05f, 0.30f);
        float goldChance = Mathf.Clamp(0.25f + eqWave * 0.020f, 0.25f, 0.50f);

        // Phase 2-3 '엄선된 메뉴판': 선택지가 줄어드는 대신 상위 등급 확률 급상승
        if (AugmentManager.CurationBoost)
        {
            prismChance = Mathf.Min(0.50f, prismChance * 2f);
            goldChance = Mathf.Min(0.60f, goldChance * 1.5f);
        }

        if (roll < prismChance) return AugmentGrade.Prismatic;
        if (roll < prismChance + goldChance) return AugmentGrade.Gold;
        return AugmentGrade.Silver;
    }
}


/// <summary>
/// v4.14 (v9.35.2 2026-10-26): 증강 카드 맨 아래 한 줄 - 이 증강이 지금 내 기차의 어느 포탑에 걸리나.
/// 유저 10-26 "증강설명 폭발반경이나 이런거 좀 더 친절하게 - 어떤 종류에 적용이 되는지 알아야 집든말든하지".
/// 걸리는 종류는 밸런스 표(balance/WDT_밸런스표) '증강' 시트의 '걸리는 곳'과 같다. AugmentPickUI 가 설명 밑에 붙인다.
///   AugmentTargets.Note(aug)
///     -> 걸리는 수   "[지금 내 포탑 중 2기에 걸린다]" (금색) / "[지금은 걸리는 포탑이 없다]" (주황)
///     -> 조건형      "[조건: 화상·독을 거는 포탑 지금 2기]" (약점 파고들기 · 사냥꾼의 본능 · 동상 파편)
///     -> 지금 값형   "[지금 유물 3개 -> 피해 +18%]" (골동품 감정가 · 도박사의 성배 · 강철의 심장 · 올인 · 주방장 · 공명 · 단골)
///     -> 기차 · 골드 · 재료처럼 포탑과 상관없는 증강은 "" (줄 없음)
/// 스위치: GameBalance.AugmentTargetNoteOn
/// </summary>
public static class AugmentTargets
{
    private const string GOOD = "#FFD24D";   // 걸린다 - 공명 설명 상자와 같은 금색
    private const string WARN = "#F08A5D";   // 없다 - 집기 전에 한 번 더 보라는 주황

    /// <summary>걸리는 포탑 종류 (밸런스 표의 '걸리는 곳')</summary>
    private enum Kind { None, Shoot, Range, Explode, Chain, Dot, Slow, Control, Shred, Single, Direct, Buff, BasicShoot }

    /// <summary>카드 맨 아래 한 줄 (색 글자). 셀 포탑이 없는 곳(전투 밖)이나 포탑과 상관없는 증강이면 ""</summary>
    public static string Note(AugmentData a)
    {
        if (a == null || !GameBalance.AugmentTargetNoteOn) return "";
        TurretSlotManager m = TurretSlotManager.Instance;
        if (m == null || m.slots == null) return "";

        switch (a.id)
        {
            case "gold_weakpoint": return NeedLine(Kind.Dot, "화상·독을 거는");
            case "gold_hunter":
            case "prism_shatter": return NeedLine(Kind.Control, "감속·마비를 거는");
            case "prism_primal": return PrimalLine();
            case "silver_regular": return MasteryLine();
            case "gold_resonance": return ResonanceLine(false);
            case "prism_resonance": return ResonanceLine(true);
            case "prism_onechef": return ChefLine();
            case "gold_collector":
                {
                    int n = ItemManager.OwnedCount;
                    string line = "[지금 유물 " + n + "개 -> 피해 +" + Mathf.RoundToInt(n * GameBalance.CollectorPerItem * 100f) + "%]";
                    return n > 0 ? Good(line) : Warn(line);
                }
            case "gold_chalice":
                {
                    // 이 증강도 [도박] 이라 집으면 하나 더 센다 (AugmentManager.ChaliceMul = 1 + 0.10 x 도박 수)
                    int n = AugmentManager.GamblerFamilyCount + (AugmentManager.HasAugment(a.id) ? 0 : 1);
                    return Good("[이것까지 도박 증강 " + n + "개 -> 피해 +" + (n * 10) + "%]");
                }
            case "prism_steelheart":
                {
                    float hp = TrainManager.Instance != null ? TrainManager.Instance.currentMaxHP : 0f;
                    int pct = Mathf.RoundToInt(Mathf.Min(1f, hp / 100f * GameBalance.SteelHeartPer100) * 100f);
                    return Good("[지금 최대 HP " + Mathf.RoundToInt(hp) + " -> 피해 +" + pct + "%]");
                }
            case "prism_allin":
                {
                    int gold = GameManager.Instance != null ? GameManager.Instance.playerGold : 0;
                    int pct = Mathf.RoundToInt(Mathf.Min(1f, (gold / 100) * 0.04f) * 100f);   // 증강 효과와 같은 셈 (100 단위로 내림)
                    return gold >= 100 ? Good("[지금 골드 " + gold + " -> 피해 +" + pct + "%]")
                                       : Warn("[지금 골드 " + gold + " - 피해가 거의 안 오른다]");
                }
        }

        Kind k = KindOf(a.id);
        if (k == Kind.None) return "";
        int count = Count(k);
        return count > 0 ? Good("[지금 내 포탑 중 " + count + "기에 걸린다]") : Warn("[지금은 걸리는 포탑이 없다]");
    }

    /// <summary>증강 id -> 걸리는 포탑 종류 (조건형 · 지금 값형은 Note 가 따로 다룬다)</summary>
    private static Kind KindOf(string id)
    {
        switch (id)
        {
            case "silver_atk": case "silver_crit": case "silver_aspd":
            case "gold_atk": case "gold_crit": case "gold_static": case "gold_bloodbet":
            case "gold_lastservice": case "gold_overkill":
            case "prism_redkitchen": case "prism_gambler": case "prism_chainproc": case "prism_ramp":
            case "prism_opening": case "prism_glasscannon": case "prism_overdrive": case "prism_lastsupper":
                return Kind.Shoot;
            case "silver_range":
                return Kind.Range;
            case "silver_explode": case "gold_explode": case "gold_fullsplash": case "prism_echo":
                return Kind.Explode;
            case "gold_chain": case "prism_chainamp":
                return Kind.Chain;
            case "silver_dot": case "gold_dot":
                return Kind.Dot;
            case "silver_shred": case "gold_shred":
                return Kind.Shred;
            case "prism_iceheart":
                return Kind.Slow;
            case "prism_rail": case "prism_shotgun":
                return Kind.Single;
            case "gold_doubletap":
                return Kind.Direct;
            case "gold_adjacent":
                return Kind.Buff;
            case "prism_basics":
                return Kind.BasicShoot;
            default:
                return Kind.None;
        }
    }

    /// <summary>쏘는 포탑인가 (이웃 강화 · 상시 · 오라는 쏘지 않는다)</summary>
    private static bool IsShooter(RecipeData r)
    {
        return r != null && string.IsNullOrEmpty(r.buffType) && r.shape != AttackShape.Aura && r.shape != AttackShape.Passive;
    }

    /// <summary>요리 r 이 종류 k 에 드나 - 지금 가진 증강(원시 화력 · 붉은 주방 · 정전기 축적 · 열차포/산탄)까지 본다</summary>
    private static bool Matches(Kind k, RecipeData r)
    {
        if (r == null) return false;
        bool shoots = IsShooter(r);
        bool effects = shoots && !AugmentManager.PrimalPower;   // 원시 화력이면 쏘는 요리의 덤 효과가 없다
        switch (k)
        {
            case Kind.Shoot: return shoots;
            case Kind.Range: return shoots && !TurretAttackExecutor.FiresCone(r);   // 부채꼴은 불길이 닿는 거리만 노린다 - 노리는 거리 증강이 안 붙는다
            case Kind.Explode: return r.shape == AttackShape.Explode;
            case Kind.Chain: return r.shape == AttackShape.Chain;
            case Kind.Dot:
                return (effects && (r.burnStack > 0 || r.poisonStack > 0 || (GameBalance.FieldPoisonOn && r.fieldPoison)))
                    || (shoots && AugmentManager.RedKitchen)
                    || (GameBalance.AuraBurnIsDot && r.passiveType == "auraBurn");
            case Kind.Slow: return effects && r.slowLevel > 0;
            case Kind.Control:
                return (effects && (r.slowLevel > 0 || r.stunSec > 0f))
                    || (shoots && AugmentManager.StaticNth > 0)
                    || (GameBalance.TrainCcCounts && (r.passiveType == "auraSlow" || r.passiveType == "thorns"));
            case Kind.Shred:
                return (effects && (r.shredDef > 0 || r.shredRes > 0))
                    || (GameBalance.AuraShredReal && r.passiveType == "auraShred");
            case Kind.Single: return shoots && r.shape == AttackShape.Projectile;
            case Kind.Direct:
                return shoots && (r.shape == AttackShape.Explode || r.shape == AttackShape.Field
                    || (r.shape == AttackShape.Projectile && !AugmentManager.PierceConversion && !AugmentManager.ConeConversion));
            case Kind.Buff: return !string.IsNullOrEmpty(r.buffType);
            case Kind.BasicShoot: return shoots && r.tier == 1;
            default: return false;
        }
    }

    /// <summary>지금 기차에 놓인 포탑 중 종류 k 에 드는 수 (빈 칸 · 잠긴 칸 빼고)</summary>
    private static int Count(Kind k)
    {
        int n = 0;
        TurretSlot[] slots = TurretSlotManager.Instance.slots;
        for (int i = 0; i < slots.Length; i++)
        {
            TurretSlot s = slots[i];
            if (s == null || s.IsEmpty || s.isLocked) continue;
            if (Matches(k, s.Recipe)) n++;
        }
        return n;
    }

    /// <summary>조건형: 조건을 채워 주는 포탑 수</summary>
    private static string NeedLine(Kind k, string what)
    {
        int n = Count(k);
        return n > 0 ? Good("[조건: " + what + " 포탑 지금 " + n + "기]") : Warn("[지금은 " + what + " 포탑이 없다]");
    }

    /// <summary>원시 화력: 덤 효과를 잃는 포탑 수 (없으면 잃는 게 없다)</summary>
    private static string PrimalLine()
    {
        int n = 0;
        TurretSlot[] slots = TurretSlotManager.Instance.slots;
        for (int i = 0; i < slots.Length; i++)
        {
            TurretSlot s = slots[i];
            if (s == null || s.IsEmpty || s.isLocked) continue;
            RecipeData r = s.Recipe;
            if (!IsShooter(r)) continue;
            if (r.burnStack > 0 || r.poisonStack > 0 || (GameBalance.FieldPoisonOn && r.fieldPoison) || r.slowLevel > 0 || r.stunSec > 0f || r.shredDef > 0 || r.shredRes > 0) n++;
        }
        return n > 0 ? Warn("[덤 효과를 잃는 포탑: 지금 " + n + "기]") : Good("[지금 포탑엔 잃을 덤 효과가 없다]");
    }

    /// <summary>단골 장부: 숙련 보너스가 있는 요리 포탑 수</summary>
    private static string MasteryLine()
    {
        int n = 0;
        TurretSlot[] slots = TurretSlotManager.Instance.slots;
        for (int i = 0; i < slots.Length; i++)
        {
            TurretSlot s = slots[i];
            if (s == null || s.IsEmpty || s.isLocked) continue;
            if (MetaProgress.GetMasteryAtk(s.recipeId) > 0f) n++;
        }
        return n > 0 ? Good("[지금 단골 요리 포탑 " + n + "기]") : Warn("[지금은 단골 요리 포탑이 없다]");
    }

    /// <summary>공명 증강: 지금 공명 중인 계열 (fromTwo = 공명 폭주 - 2기 넘는 계열). 방어 공명은 피해 감소라 뺀다</summary>
    private static string ResonanceLine(bool fromTwo)
    {
        if (ItemManager.NoResonance) return Warn("[짝꿍 숟가락 때문에 공명이 꺼져 있다]");
        TurretSlotManager m = TurretSlotManager.Instance;
        int need = fromTwo ? 2 : (AugmentManager.ResonanceNeedOverride > 0 ? AugmentManager.ResonanceNeedOverride : GameBalance.ResonanceCount);
        string list = "";
        foreach (FoodTag tag in System.Enum.GetValues(typeof(FoodTag)))
        {
            if (tag == FoodTag.Def) continue;
            int c = m.GetTagCount(tag);
            if (c >= need) list += (list.Length > 0 ? " · " : "") + m.TagName(tag) + " " + c + "기";
        }
        if (list.Length > 0) return Good(fromTwo ? "[집으면 바로 공명: " + list + "]" : "[지금 공명 중: " + list + "]");
        return Warn(fromTwo ? "[지금은 2기 넘는 계열이 없다]" : "[지금은 공명 중인 계열이 없다]");
    }

    /// <summary>주방장은 하나다: 지금 주방장이 될 포탑 (가장 레벨 높은 포탑 - TurretSlotManager.GetChefRecipeId)</summary>
    private static string ChefLine()
    {
        TurretSlotManager m = TurretSlotManager.Instance;
        string id = m.GetChefRecipeId();
        if (string.IsNullOrEmpty(id)) return Warn("[지금은 주방장이 될 포탑이 없다]");
        int best = 0;
        for (int i = 0; i < m.slots.Length; i++)
        {
            TurretSlot s = m.slots[i];
            if (s != null && !s.IsEmpty && !s.isLocked && s.recipeId == id) best = Mathf.Max(best, s.level);
        }
        RecipeData r = RecipeDatabase.Get(id);
        return Good("[지금 주방장: " + (r != null ? r.displayName : id) + " Lv" + best + "]");
    }

    private static string Good(string s) { return "<color=" + GOOD + ">" + s + "</color>"; }
    private static string Warn(string s) { return "<color=" + WARN + ">" + s + "</color>"; }
}
