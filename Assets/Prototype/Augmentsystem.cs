using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// [AugmentSystem.cs] v3
/// 로그라이크 증강 시스템 (기획 C) - 창의적 증강 재설계판
///
/// 설계 철학
///  - 실버  : 무난한 수치 강화 (안전한 선택지)
///  - 골드  : 조건부 시너지 - "이걸 먹었으니 이렇게 운영하자"가 생기는 효과
///  - 프리즘 : 공격 방식 자체를 비트는 효과 - 빌드의 축이 되는 유물급
///  - v3 추가: 하이리스크/하이리턴 증강 + [도박] 패밀리 시너지
///    ([도박] 태그 증강을 모을수록 도박 계열 효과가 강해진다)
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
    public static float LifestealPerHit = 0f;    // 타격당 기차 회복량
    public static float HealPerWave = 0f;        // 웨이브 클리어 시 회복량

    // ---------- 조건부 시너지 (골드) ----------
    public static float DotTargetBonus = 0f;     // 도트 걸린 적에게 추가 데미지 (0.25 = +25%)
    public static float ControlTargetBonus = 0f; // 슬로우/스턴 적에게 추가 데미지
    public static int StaticNth = 0;             // N번째 타격마다 감전 (0 = 비활성)

    // ---------- 주방 이벤트 관련 ----------
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

    /// <summary>지금까지 획득한 증강 목록 (UI 표시용)</summary>
    public static List<AugmentData> Owned = new List<AugmentData>();

    /// <summary>런 시작 시 초기화 (static은 씬 재시작에도 남으므로 반드시 호출)</summary>
    public static void ResetRun()
    {
        AtkMul = 1f; CritChanceAdd = 0f; CritDamageAdd = 0f;
        ExplodeRadiusMul = 1f; ChainCountAdd = 0;
        DotMul = 1f; ShredAdd = 0;
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

        Owned.Clear();
        AugmentHooks.Clear();
        Debug.Log("[증강] 런 초기화 완료");
    }

    /// <summary>증강 획득</summary>
    public static void Acquire(AugmentData aug)
    {
        if (aug == null) return;
        Owned.Add(aug);

        // [도박] 패밀리 카운트 (시너지용) - apply보다 먼저 올려서 효과가 자기 자신을 포함
        if (aug.family == "도박") GamblerFamilyCount++;

        if (aug.apply != null) aug.apply();
        Debug.Log("[증강] 획득: " + aug.name + " (" + aug.GradeName() + ")"
            + (aug.family != null ? " [" + aug.family + "]" : ""));
    }

    /// <summary>해당 증강을 이미 갖고 있는지</summary>
    public static bool HasAugment(string id)
    {
        for (int i = 0; i < Owned.Count; i++)
            if (Owned[i].id == id) return true;
        return false;
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

    public static void Clear()
    {
        controlUntil.Clear();
        dotUntil.Clear();
    }

    /// <summary>슬로우/스턴을 걸었을 때 호출</summary>
    public static void RegisterControl(Enemy en, float duration)
    {
        if (en == null) return;
        float until = Time.time + duration;
        float old;
        if (controlUntil.TryGetValue(en, out old) && old > until) return;
        controlUntil[en] = until;
        CleanupIfBig(controlUntil);
    }

    /// <summary>화상/중독을 걸었을 때 호출</summary>
    public static void RegisterDot(Enemy en, float duration)
    {
        if (en == null) return;
        float until = Time.time + duration;
        float old;
        if (dotUntil.TryGetValue(en, out old) && old > until) return;
        dotUntil[en] = until;
        CleanupIfBig(dotUntil);
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

    /// <summary>장부가 커지면 만료된 항목 정리</summary>
    private static void CleanupIfBig(Dictionary<Enemy, float> dict)
    {
        if (dict.Count < 64) return;
        List<Enemy> removeList = new List<Enemy>();
        foreach (KeyValuePair<Enemy, float> pair in dict)
            if (pair.Key == null || pair.Value <= Time.time) removeList.Add(pair.Key);
        for (int i = 0; i < removeList.Count; i++)
            dict.Remove(removeList[i]);
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

    private static void Build()
    {
        all = new List<AugmentData>();

        // ==========================================================
        //  실버 - 무난한 수치 강화 (안전픽)
        // ==========================================================
        all.Add(new AugmentData("silver_atk", "기름칠한 포신",
            "모든 포탑 데미지 +12%", AugmentGrade.Silver, true,
            delegate { AugmentManager.AtkMul *= 1.12f; }));

        all.Add(new AugmentData("silver_hp", "강철 리벳 보강",
            "기차 최대 HP +250 (즉시 회복)", AugmentGrade.Silver, true,
            delegate { AugmentManager.AddTrainMaxHP(250f); AugmentManager.HealTrain(250f); }));

        all.Add(new AugmentData("silver_crit", "정밀 렌즈",
            "치명타 확률 +8%", AugmentGrade.Silver, true,
            delegate { AugmentManager.CritChanceAdd += 0.08f; }));

        all.Add(new AugmentData("silver_dot", "매운 양념 한 스푼",
            "화상 / 중독 도트 데미지 +25%", AugmentGrade.Silver, true,
            delegate { AugmentManager.DotMul *= 1.25f; }));

        all.Add(new AugmentData("silver_explode", "화약 추가 배합",
            "폭발 반경 +15%", AugmentGrade.Silver, true,
            delegate { AugmentManager.ExplodeRadiusMul *= 1.15f; }));

        all.Add(new AugmentData("silver_shred", "구리 도금 탄환",
            "방어력 / 마법저항 깎기 수치 +5", AugmentGrade.Silver, true,
            delegate { AugmentManager.ShredAdd += 5; }));

        all.Add(new AugmentData("silver_lifesteal", "육수 한 국자",
            "포탑이 적을 때릴 때마다 기차 HP 0.5 회복", AugmentGrade.Silver, true,
            delegate { AugmentManager.LifestealPerHit += 0.5f; }));

        all.Add(new AugmentData("silver_wavehal", "응급 정비",
            "웨이브 클리어마다 기차 HP 60 회복", AugmentGrade.Silver, true,
            delegate { AugmentManager.HealPerWave += 60f; }));

        all.Add(new AugmentData("silver_aspd", "고속 회전 모터",
            "모든 포탑 공격속도 +12%", AugmentGrade.Silver, true,
            delegate { AugmentManager.AspdMul *= 1.12f; }));

        all.Add(new AugmentData("silver_range", "망원 조준경",
            "모든 포탑 사거리 +15%", AugmentGrade.Silver, true,
            delegate { AugmentManager.RangeMul *= 1.15f; }));

        all.Add(new AugmentData("silver_knife", "잘 드는 식칼",
            "조리 미니게임 제한 시간 +20% (굽기 커서도 느려짐)", AugmentGrade.Silver, true,
            delegate { AugmentManager.CookSpeedMul *= 1.20f; }));

        all.Add(new AugmentData("silver_magnet", "자석 흡입기 개조",
            "적 처치 시 재료 드랍량 +25%", AugmentGrade.Silver, true,
            delegate { AugmentManager.MaterialDropMul *= 1.25f; }));

        // ==========================================================
        //  골드 - 조건부 시너지 ("이걸 먹었으니 이렇게 운영하자")
        // ==========================================================
        all.Add(new AugmentData("gold_atk", "볼케이노 압축기",
            "모든 포탑 데미지 +30%", AugmentGrade.Gold, true,
            delegate { AugmentManager.AtkMul *= 1.30f; }));

        all.Add(new AugmentData("gold_dot", "매운맛 중독",
            "화상 / 중독 도트 데미지 +60%", AugmentGrade.Gold, true,
            delegate { AugmentManager.DotMul *= 1.60f; }));

        all.Add(new AugmentData("gold_chain", "전격의 협곡",
            "연쇄 번개 전이 횟수 +2", AugmentGrade.Gold, true,
            delegate { AugmentManager.ChainCountAdd += 2; }));

        all.Add(new AugmentData("gold_explode", "화염의 포효",
            "폭발 반경 +35%", AugmentGrade.Gold, true,
            delegate { AugmentManager.ExplodeRadiusMul *= 1.35f; }));

        all.Add(new AugmentData("gold_shred", "부식성 위산",
            "방어력 / 마법저항 깎기 수치 +12", AugmentGrade.Gold, true,
            delegate { AugmentManager.ShredAdd += 12; }));

        all.Add(new AugmentData("gold_crit", "헤드샷 프로토콜",
            "치명타 확률 +15%, 치명타 데미지 +50%", AugmentGrade.Gold, true,
            delegate { AugmentManager.CritChanceAdd += 0.15f; AugmentManager.CritDamageAdd += 0.50f; }));

        all.Add(new AugmentData("gold_lifesteal", "회복의 만찬",
            "포탑이 적을 때릴 때마다 기차 HP 2 회복", AugmentGrade.Gold, true,
            delegate { AugmentManager.LifestealPerHit += 2f; }));

        all.Add(new AugmentData("gold_fortress", "강철의 요새",
            "기차 최대 HP +1000 (즉시 회복)", AugmentGrade.Gold, false,
            delegate { AugmentManager.AddTrainMaxHP(1000f); AugmentManager.HealTrain(1000f); }));

        all.Add(new AugmentData("gold_nanoarmor", "나노 수복 장갑",
            "기차가 받는 피해 15% 감소", AugmentGrade.Gold, true,
            delegate { AugmentManager.DamageReductionAdd += 0.15f; }));

        all.Add(new AugmentData("gold_goldentool", "황금 조리 기구",
            "조리 판정 존 +35%, 제한 시간 +10%", AugmentGrade.Gold, false,
            delegate { AugmentManager.CookJudgeMul *= 1.35f; AugmentManager.CookSpeedMul *= 1.10f; }));

        // --- 여기부터 시너지형 골드 ---
        all.Add(new AugmentData("gold_static", "정전기 축적",
            "모든 포탑의 4번째 타격이 적을 0.4초 감전시킨다", AugmentGrade.Gold, false,
            delegate { AugmentManager.StaticNth = 4; }));

        all.Add(new AugmentData("gold_weakpoint", "약점 파고들기",
            "화상 / 중독이 걸린 적에게 데미지 +25%", AugmentGrade.Gold, true,
            delegate { AugmentManager.DotTargetBonus += 0.25f; }));

        all.Add(new AugmentData("gold_hunter", "사냥꾼의 본능",
            "슬로우 / 스턴 상태의 적에게 데미지 +30%", AugmentGrade.Gold, true,
            delegate { AugmentManager.ControlTargetBonus += 0.30f; }));

        all.Add(new AugmentData("gold_insurance", "보험 계약",
            "주방 이벤트 실패 페널티 60% 감소", AugmentGrade.Gold, false,
            delegate { AugmentManager.EventPenaltyMul *= 0.40f; }));

        all.Add(new AugmentData("gold_fanflame", "부채질 장인",
            "주방 이벤트가 2배 자주 발생하지만, 성공 보상 3배", AugmentGrade.Gold, false,
            delegate { AugmentManager.EventIntervalMul *= 0.5f; AugmentManager.EventRewardMul *= 3f; },
            null, "도박"));

        all.Add(new AugmentData("gold_fullsplash", "폭발 전문가",
            "폭발 스플래시 데미지 감쇄가 사라진다 (80% -> 100%)", AugmentGrade.Gold, false,
            delegate { AugmentManager.FullSplash = true; }));

        all.Add(new AugmentData("gold_doubletap", "2연장 개조",
            "모든 단일 타격이 25% 확률로 즉시 한 발 더 나간다 (50% 데미지)", AugmentGrade.Gold, false,
            delegate { AugmentManager.DoubleTapChance = 0.25f; }));

        all.Add(new AugmentData("gold_fieldrepair", "야전 정비반",
            "웨이브 클리어마다 기차 최대 HP +50 (영구, 중복 가능)", AugmentGrade.Gold, true,
            delegate { AugmentManager.MaxHPPerWave += 50f; }));

        all.Add(new AugmentData("gold_luckycharm", "행운의 부적",
            "앞으로 증강 선택지가 1장 더 나온다 (최대 +2)", AugmentGrade.Gold, true,
            delegate { AugmentManager.ExtraCards = Mathf.Min(2, AugmentManager.ExtraCards + 1); },
            null, "도박"));

        // --- 하이리스크 / 도박 패밀리 골드 ---
        all.Add(new AugmentData("gold_usurer", "고리대금업자",
            "즉시 골드 +800. 대신 앞으로 웨이브 골드 보상 -50%", AugmentGrade.Gold, false,
            delegate
            {
                if (GameManager.Instance != null) GameManager.Instance.AddGold(800);
                AugmentManager.GoldRewardMul *= 0.5f;
            },
            null, "도박"));

        all.Add(new AugmentData("gold_bloodbet", "출혈 베팅",
            "웨이브가 시작될 때마다 기차 HP -80. 대신 모든 포탑 데미지 +35%", AugmentGrade.Gold, false,
            delegate { AugmentManager.BloodBet = true; AugmentManager.AtkMul *= 1.35f; },
            null, "도박"));

        all.Add(new AugmentData("gold_chalice", "도박사의 성배",
            "보유한 [도박] 증강 1개당 모든 포탑 데미지 +10% (이 증강 포함, 이후 획득분도 반영)",
            AugmentGrade.Gold, false,
            delegate { AugmentManager.HasChalice = true; },
            null, "도박"));

        // ==========================================================
        //  프리즘 - 공격 방식 자체를 비튼다 (빌드의 축)
        // ==========================================================
        all.Add(new AugmentData("prism_rail", "열차포 개조",
            "모든 투사체 공격이 직선 관통 레일건으로 변한다", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.PierceConversion = true; },
            "prism_shotgun"));

        all.Add(new AugmentData("prism_shotgun", "산탄 셰프",
            "모든 투사체 공격이 부채꼴 산탄으로 변한다 (데미지 -25%)", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.ConeConversion = true; },
            "prism_rail"));

        all.Add(new AugmentData("prism_redkitchen", "붉은 주방",
            "모든 타격이 화상 1스택을 남긴다. 도트 데미지 +50%", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.RedKitchen = true; AugmentManager.DotMul *= 1.50f; },
            "prism_primal"));

        all.Add(new AugmentData("prism_iceheart", "얼음 심장",
            "모든 슬로우 효과가 0.8초 빙결(스턴)로 변한다", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.IceHeart = true; }));

        all.Add(new AugmentData("prism_gambler", "도박사 스피노의 탄환",
            "모든 데미지가 50% 확률로 2배, 아니면 절반이 된다. [도박] 증강 1개당 2배 확률 +4%p (최대 70%)",
            AugmentGrade.Prismatic, false,
            delegate { AugmentManager.GamblerBullet = true; },
            null, "도박"));

        all.Add(new AugmentData("prism_chainproc", "번개 계승",
            "모든 타격이 20% 확률로 소형 연쇄 번개를 일으킨다", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.ChainProcChance = 0.20f; }));

        all.Add(new AugmentData("prism_echo", "메아리치는 폭발",
            "폭발이 한 번 더 터진다 (60% 데미지, 1.2배 반경)", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.DoubleExplosion = true; }));

        all.Add(new AugmentData("prism_chainamp", "증폭 전이",
            "연쇄 번개가 튕길수록 강해진다 (튕길 때마다 +20%)", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.ChainAmplify = true; }));

        all.Add(new AugmentData("prism_ramp", "과열 기관",
            "포탑이 사격을 이어갈수록 데미지가 오른다 (타격당 +5%, 최대 +75%, 2.5초 쉬면 초기화)",
            AugmentGrade.Prismatic, false,
            delegate { AugmentManager.RampAttack = true; }));

        all.Add(new AugmentData("prism_shatter", "동상 파편",
            "슬로우 / 스턴 상태의 적을 때리면 30% 확률로 서리 폭발이 일어난다 (50% 데미지)",
            AugmentGrade.Prismatic, false,
            delegate { AugmentManager.FrostShatter = true; }));

        all.Add(new AugmentData("prism_opening", "개전 포격",
            "웨이브 시작 후 8초 동안 모든 포탑 데미지 2배",
            AugmentGrade.Prismatic, false,
            delegate { AugmentManager.OpeningBarrage = true; }));

        // --- 하이리스크 프리즘 ---
        all.Add(new AugmentData("prism_primal", "원시 화력",
            "포탑의 모든 상태이상(도트/슬로우/스턴/방깎)이 사라진다. 대신 순수 데미지 +80%",
            AugmentGrade.Prismatic, false,
            delegate { AugmentManager.PrimalPower = true; },
            "prism_redkitchen"));

        all.Add(new AugmentData("prism_ninelives", "아홉 개의 목숨",
            "기차가 완파될 때 1회 부활한다 (HP 800 회복)", AugmentGrade.Prismatic, true,
            delegate { AugmentManager.ReviveCharges += 1; }));

        all.Add(new AugmentData("prism_glasscannon", "유리 대포",
            "모든 포탑 데미지 +70%. 대신 기차가 받는 피해 +25%", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.AtkMul *= 1.70f; AugmentManager.DamageReductionAdd -= 0.25f; }));

        all.Add(new AugmentData("prism_overdrive", "오버차지 엔진",
            "모든 포탑 공격속도 +45%. 대신 사거리 -15%", AugmentGrade.Prismatic, false,
            delegate { AugmentManager.AspdMul *= 1.45f; AugmentManager.RangeMul *= 0.85f; }));

        all.Add(new AugmentData("prism_extraslot", "증축된 주방 칸",
            "포탑 슬롯 1칸 추가 해금 (최대 8칸)", AugmentGrade.Prismatic, true,
            delegate { AugmentManager.ExtraSlotUnlock = Mathf.Min(2, AugmentManager.ExtraSlotUnlock + 1); }));

        all.Add(new AugmentData("gold_adjacent", "주방 동선 최적화",
            "인접 슬롯 버프 효과 +50%", AugmentGrade.Gold, false,
            delegate { AugmentManager.AdjacentBuffMul *= 1.5f; }));

        all.Add(new AugmentData("gold_resonance", "속성 공명 증폭기",
            "속성 공명 보너스 +15%p (같은 속성 3개 이상 배치 시)", AugmentGrade.Gold, true,
            delegate { AugmentManager.ResonanceBonusAdd += 0.15f; }));

        all.Add(new AugmentData("prism_allin", "올인",
            "지금 가진 골드를 전부 잃는다. 잃은 골드 100당 모든 포탑 데미지 +4% (최대 +100%)",
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
                AugmentManager.AtkMul *= 1f + bonus;
                Debug.Log("[증강] 올인: 골드 " + lostGold + " 소모, 데미지 +" + Mathf.RoundToInt(bonus * 100f) + "%");
            },
            null, "도박"));

        Debug.Log("[증강] 데이터베이스 로드 완료 - 총 " + all.Count + "종");
    }

    /// <summary>
    /// 웨이브 수에 맞춰 등급 확률을 정하고, 중복/충돌을 피해 count개를 뽑는다.
    /// </summary>
    public static List<AugmentData> Roll(int waveNumber, int count)
    {
        List<AugmentData> result = new List<AugmentData>();
        List<AugmentData> pool = new List<AugmentData>();

        for (int i = 0; i < All.Count; i++)
        {
            AugmentData a = All[i];
            // 논스택 증강은 이미 가졌으면 제외
            if (!a.stackable && AugmentManager.HasAugment(a.id)) continue;
            // 충돌 증강(예: 레일건 개조 vs 산탄 셰프)은 상대를 가졌으면 제외
            if (a.conflictId != null && AugmentManager.HasAugment(a.conflictId)) continue;
            pool.Add(a);
        }

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

    /// <summary>웨이브가 올라갈수록 상위 등급이 잘 나온다</summary>
    private static AugmentGrade RollGrade(int waveNumber)
    {
        float roll = Random.value;
        float prismChance = Mathf.Clamp(0.05f + waveNumber * 0.015f, 0.05f, 0.30f);
        float goldChance = Mathf.Clamp(0.25f + waveNumber * 0.020f, 0.25f, 0.50f);

        if (roll < prismChance) return AugmentGrade.Prismatic;
        if (roll < prismChance + goldChance) return AugmentGrade.Gold;
        return AugmentGrade.Silver;
    }
}
