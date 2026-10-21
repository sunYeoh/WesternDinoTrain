using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// [ItemSystem.cs] v1.6 (v9.29 2026-10-21 손맛 3차 B8: 획득(Acquire = 효과·소유)과 드러내기(Reveal = 알림 줄·카드·소리)를 나눴다 - 갑판 상자는 GrantRandom(label, deferReveal, out item) 으로 효과만 먼저 걸고 상자가 날아와 닿을 때 Reveal. GrantRandom 이 "유물을 줬나" 를 돌려준다 (못 주면 골드). RelicToast.FlightTarget = 날아갈 캔버스·카드 자리, 씬이 바뀌면 날아가던 상자도 지운다) / v1.5 (v9.25 2026-10-16 B5 - 5차 피드백 4번 "유물 개구림 의미없음": 조리 판정·사고 쪽 8 (보험·부채·매트·숫돌·온도계·뒤집개·밸브·고글)을 싸움 유물 8 로 - 녹슨 탄창·굶주린 화덕·짝꿍 숟가락·탐욕의 국자·무거운 냄비·선대의 식탁보·독한 소스·빈 접시. 09 월 "증강 = 싸움 / 유물 = 주방 일" 나누기를 바꾸는 것이라 GameBalance.RelicCombatSet = false 면 예전 15종. 새 값은 ItemManager 의 v1.5 칸, 피해 배율은 RelicDamageMul) / v1.4 (v9.18 2026-10-06: 유물 획득 알림 카드 RelicToast - 이름·효과·"[V] 로 다시 본다" 를 4.5초 띄운다. 상자로 주운 유물은 알림 줄에 이름만 나와 무슨 효과인지 알 수 없었다) / v1.3 (v9.16 2026-09-29 손맛 2차 - 소리: 유물 획득 sfx_relic) / v1.2 (v9.14 2026-09-28: 유물 15종 설명을 일상어로 - 테스터 "황금 조리 기구가 뭔지 모르겠음": "판정 구간" -> "맞춰야 하는 구간이 넓어진다") / v1.1 (2026-09-14: IsItemUsable 스위치 연동) / v1 (신규 파일) - Phase 2-3: 아이템(유물) 시스템
///
/// 설계 (사용자 결정 - 증강/아이템 이원화):
///  - 증강 = 포탑 강화 + 기차 유틸 (전투 출력에 관여)
///  - 아이템 = 주방 내부의 일 (조리 미니게임 / 방해 이벤트 / 도구) 담당
///    기존 증강 5종(잘 드는 식칼/황금 조리 기구/보험 계약/부채질 장인/미끄럼 방지 매트)이
///    이쪽으로 이관됐고, 신규 10종이 추가됐다. (슬레이 더 스파이어 유물 스타일)
///
/// 규칙:
///  - 아이템은 전부 "유일 보유" (증강과 달리 중복 획득 없음)
///  - 획득 즉시 발동하는 패시브. 런 단위로 리셋 (ResetRun)
///  - 획득처 4곳: 행상인 안킬로(MerchantUI) / 폐역 선로 / 적 저확률 드랍 / 침입자 격퇴
///
/// 다른 시스템은 ItemManager의 static 값만 읽으면 된다.
/// 훅 위치: CookingMinigame(조리 배율/기름) / CookingBridge(환급/향신료) /
///          ChefController(마모/고글) / KitchenEventManager(이벤트 배율/소화기) /
///          KitchenEvents(침입자) / Enemy(드랍/전갈) / WaveManager(폐역 보상)
/// VS 2017 (C# 7.3) 호환
/// </summary>

public enum ItemRarity
{
    Common,   // 일반 - 행상인 단골 매물
    Rare,     // 희귀 - 비싸지만 강한 효과
    Special   // 유일 - 상점에 없음, 특정 획득처 전용 (예: 장물 주머니)
}

/// <summary>아이템 1종의 정의</summary>
public class ItemData
{
    public string id;              // 내부 식별자
    public string name;            // 표시 이름
    public string desc;            // 표시 설명
    public ItemRarity rarity;      // 희귀도
    public int price;              // 행상인 판매가 (Special은 0 = 비매품)
    public System.Action apply;    // 획득 시 실행되는 효과

    public ItemData(string id, string name, string desc, ItemRarity rarity, int price, System.Action apply)
    {
        this.id = id;
        this.name = name;
        this.desc = desc;
        this.rarity = rarity;
        this.price = price;
        this.apply = apply;
    }

    /// <summary>희귀도 표시 문자열</summary>
    public string RarityName()
    {
        switch (rarity)
        {
            case ItemRarity.Common: return "일반";
            case ItemRarity.Rare: return "희귀";
            default: return "유일";
        }
    }

    /// <summary>희귀도별 표시 색 (일반=무쇠 / 희귀=구리금 / 유일=보라)</summary>
    public Color RarityColor()
    {
        switch (rarity)
        {
            case ItemRarity.Common: return new Color(0.75f, 0.73f, 0.68f);
            case ItemRarity.Rare: return new Color(0.95f, 0.68f, 0.30f);
            default: return new Color(0.72f, 0.55f, 0.95f);
        }
    }
}


/// <summary>
/// 보유 아이템과 그 효과(배율/플래그)를 전역으로 제공한다. (AugmentManager와 같은 패턴)
/// </summary>
public static class ItemManager
{
    // ---------- 조리 미니게임 (CookingMinigame에서 읽음) ----------
    public static float CookTimeMul = 1f;        // 잘 드는 식칼: 제한 시간 배율
    public static float CookJudgeMul = 1f;       // 황금 조리 기구: 판정 존 배율
    public static float GrillJudgeMul = 1f;      // 구리 온도계: 굽기 판정 존
    public static float StirTimeMul = 1f;        // 균형 잡힌 뒤집개: 볶기 제한 시간
    public static float BoilJudgeMul = 1f;       // 압력 조절 밸브: 끓이기 안정존
    public static bool OilImmune = false;        // 미끄럼 방지 매트: 기름 튐 무효 (증강에서 이관)

    // ---------- 도구 / 셰프 (ChefController에서 읽음) ----------
    public static float ToolWearMul = 1f;        // 휴대용 숫돌: 도구 마모 배율 (전갈 부식 포함)
    public static bool SnipeImmune = false;      // 김서림 방지 고글: 프테라 저격 무효

    // ---------- 주방 이벤트 (KitchenEventManager/KitchenEvents에서 읽음) ----------
    public static float EventPenaltyMul = 1f;    // 보험 계약서: 실패 페널티 배율
    public static float EventRewardMul = 1f;     // 부채질 장인의 부채: 성공 보상 배율
    public static float EventIntervalMul = 1f;   // 부채질 장인의 부채: 발생 간격 배율
    public static bool HasExtinguisher = false;  // 구리 소화기: 화재 자동 진압 (웨이브당 1회)
    public static float IntruderGaugeMul = 1f;   // 랩터 덫: 침입자 격퇴 게이지 배율
    public static int SwagGoldPerIntruder = 0;   // 장물 주머니: 침입자 격퇴 시 추가 골드

    // ---------- 조리 결과 (CookingBridge에서 읽음) ----------
    public static bool FailRefund = false;       // 선대의 앞치마: 실패 시 재료 환급
    public static float PerfectExtraChance = 0f; // 비밀 향신료 주머니: PERFECT 시 요리 +1 확률

    // ---------- v1.5 (v9.25 B5): 싸움 유물 (TurretSlot·TurretAttackExecutor·TurretSlotManager·Enemy·TrainManager·AugmentPickUI 에서 읽음) ----------
    public static float AspdMul = 1f;            // 녹슨 탄창 x1.25 / 무거운 냄비 x0.9 - 포탑 공격속도 (증강 공격속도 상한과 따로)
    public static float RangeMul = 1f;           // 녹슨 탄창 x0.85 - 포탑 사거리
    public static float LowHpAtkBonus = 0f;      // 굶주린 화덕: 기차 HP 절반 아래면 피해 +30%
    public static float PairAtkBonus = 0f;       // 짝꿍 숟가락: 같은 요리 포탑이 둘 이상이면 그 포탑들 피해 +20%
    public static bool NoResonance = false;      // 짝꿍 숟가락의 대가: 공명이 꺼진다
    public static float KillGoldMul = 1f;        // 탐욕의 국자: 처치 골드 x1.4
    public static float DamageTakenAdd = 0f;     // 탐욕의 국자의 대가: 기차가 받는 피해 +15% (받는 피해 감소에서 뺀다)
    public static float ExplodeRadiusMul = 1f;   // 무거운 냄비: 폭발 반경 x1.3
    public static float WaveHeal = 0f;           // 선대의 식탁보: 웨이브를 막을 때마다 기차 HP 회복
    public static float DotMul = 1f;             // 독한 소스: 화상·독 x1.5
    public static float EmptySlotAtkBonus = 0f;  // 빈 접시: 비어 있는 포탑 칸 하나마다 다른 포탑 피해 +8%

    /// <summary>v1.5 (v9.25 B5): 이 요리 포탑의 유물 피해 배율 - 굶주린 화덕 / 짝꿍 숟가락 / 빈 접시 (TurretAttackExecutor.DealDamage 가 곱한다)</summary>
    public static float RelicDamageMul(RecipeData r)
    {
        float mul = 1f;
        if (LowHpAtkBonus > 0f && TrainManager.Instance != null && TrainManager.Instance.HPRatio < 0.5f)
            mul *= 1f + LowHpAtkBonus;
        TurretSlotManager mgr = TurretSlotManager.Instance;
        if (mgr != null && r != null)
        {
            if (PairAtkBonus > 0f && mgr.CountRecipe(r.recipeId) >= 2) mul *= 1f + PairAtkBonus;
            if (EmptySlotAtkBonus > 0f) mul *= 1f + EmptySlotAtkBonus * mgr.EmptyUnlockedCount();
        }
        return mul;
    }

    // ---------- 내부 상태 ----------
    private static int lastExtinguishWave = -1;  // 소화기: 이번 웨이브에 이미 썼는가
    public static int MerchantGuaranteedRegion = 0; // 행상인: 지역 첫 정차 확정 등장 기록

    /// <summary>지금까지 획득한 아이템 목록 (UI 표시/감정가 증강 참조용)</summary>
    public static List<ItemData> Owned = new List<ItemData>();

    /// <summary>보유 아이템 수 (증강 '골동품 감정가'가 참조)</summary>
    public static int OwnedCount { get { return Owned.Count; } }

    /// <summary>런 시작 시 초기화 (static은 씬 재시작에도 남으므로 반드시 호출)</summary>
    public static void ResetRun()
    {
        CookTimeMul = 1f; CookJudgeMul = 1f;
        GrillJudgeMul = 1f; StirTimeMul = 1f; BoilJudgeMul = 1f;
        OilImmune = false; ToolWearMul = 1f; SnipeImmune = false;
        EventPenaltyMul = 1f; EventRewardMul = 1f; EventIntervalMul = 1f;
        HasExtinguisher = false; IntruderGaugeMul = 1f; SwagGoldPerIntruder = 0;
        FailRefund = false; PerfectExtraChance = 0f;
        AspdMul = 1f; RangeMul = 1f; LowHpAtkBonus = 0f; PairAtkBonus = 0f; NoResonance = false;   // v1.5: 싸움 유물
        KillGoldMul = 1f; DamageTakenAdd = 0f; ExplodeRadiusMul = 1f; WaveHeal = 0f; DotMul = 1f; EmptySlotAtkBonus = 0f;
        lastExtinguishWave = -1;
        MerchantGuaranteedRegion = 0;
        Owned.Clear();
        Debug.Log("[아이템] 런 초기화 완료");
    }

    /// <summary>해당 아이템을 이미 갖고 있는지</summary>
    public static bool HasItem(string id)
    {
        for (int i = 0; i < Owned.Count; i++)
            if (Owned[i].id == id) return true;
        return false;
    }

    /// <summary>
    /// 아이템 획득 (효과 즉시 발동 + 알림).
    /// sourceLabel이 있으면 "[전리품] {출처}: ..." 형태로 표시 (드랍 획득용)
    /// </summary>
    public static void Acquire(ItemData item, string sourceLabel = null) { Acquire(item, sourceLabel, false); }

    /// <summary>v1.6: deferReveal = 효과·소유만 지금, 알림 줄·카드·소리는 부르는 쪽이 Reveal 할 때 (갑판 상자가 날아와 닿을 때)</summary>
    public static void Acquire(ItemData item, string sourceLabel, bool deferReveal)
    {
        if (item == null || HasItem(item.id)) return;

        Owned.Add(item);
        if (item.apply != null) item.apply();
        Debug.Log("[아이템] 획득: " + item.name + " (" + item.RarityName() + ")"
            + (sourceLabel != null ? " / 출처: " + sourceLabel : "") + (deferReveal ? " / 드러내기는 상자가 닿을 때" : ""));
        if (!deferReveal) Reveal(item, sourceLabel);
    }

    /// <summary>v1.6: 드러내기 - 알림 줄 + 카드 + 유물 소리 (획득 자체는 Acquire 가 먼저 한다)</summary>
    public static void Reveal(ItemData item, string sourceLabel)
    {
        if (item == null) return;
        if (string.IsNullOrEmpty(sourceLabel))
            UIManager.Instance?.ShowStatChange("[유물] " + item.name + " - " + item.desc);
        else
            UIManager.Instance?.ShowStatChange("[전리품] " + sourceLabel + " - " + item.name + "!  ([V] 로 효과 보기)");
        RelicToast.Show(item);   // v1.4: 무엇을 얻었고 뭐가 좋아지는지 - 왼쪽 위 HP 판 아래 카드
        SoundManager.Play("sfx_relic");   // v1.3: 유물 전용 획득음
    }

    /// <summary>미보유 아이템이 남아 있는가 (Special 제외 - 행상인/드랍 공용 체크)</summary>
    public static bool HasStock()
    {
        List<ItemData> all = ItemDatabase.All;
        for (int i = 0; i < all.Count; i++)
            if (all[i].rarity != ItemRarity.Special && !HasItem(all[i].id) && IsItemUsable(all[i].id)) return true;
        return false;
    }

    /// <summary>
    /// 무작위 미보유 아이템 1개 지급 (Special 제외).
    /// 전부 보유 중이면 골드로 대체 - "행낭이 가득 찼다"
    /// </summary>
    /// <summary>2026-09-14 (교수 피드백 B3): 실험 스위치에 따라 의미가 사라지는 아이템은 풀·상점에서 뺀다</summary>
    public static bool IsItemUsable(string id)
    {
        if (id == "item_whetstone" && !GameBalance.ToolWearEnabled) return false;
        return true;
    }

    public static bool GrantRandom(string sourceLabel) { ItemData dummy; return GrantRandom(sourceLabel, false, out dummy); }

    /// <summary>미보유 유물 하나를 무작위로 (없으면 골드 60). 돌려주는 값 = 유물을 줬나. v1.6: deferReveal 이면 효과만 걸고 알림·카드·소리는 Reveal 때 (갑판 상자)</summary>
    public static bool GrantRandom(string sourceLabel, bool deferReveal, out ItemData granted)
    {
        granted = null;
        List<ItemData> pool = new List<ItemData>();
        List<ItemData> all = ItemDatabase.All;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].rarity == ItemRarity.Special || HasItem(all[i].id)) continue;
            if (!IsItemUsable(all[i].id)) continue;   // 2026-09-14 (B3): 마모 off면 숫돌은 의미가 없다
            pool.Add(all[i]);
        }

        if (pool.Count == 0)
        {
            GameManager.Instance?.AddGold(60);
            UIManager.Instance?.ShowStatChange("[전리품] 행낭이 가득 찼다... 대신 골드 +60");
            return false;
        }
        granted = pool[Random.Range(0, pool.Count)];
        Acquire(granted, sourceLabel, deferReveal);
        return true;
    }

    /// <summary>구리 소화기: 화재 자동 진압 시도 (웨이브당 1회). 성공하면 true</summary>
    public static bool TryAutoExtinguish()
    {
        if (!HasExtinguisher) return false;
        int wave = GameManager.Instance != null ? GameManager.Instance.currentWave : 0;
        if (lastExtinguishWave == wave) return false;   // 이번 웨이브 분량은 이미 소진
        lastExtinguishWave = wave;
        return true;
    }

    /// <summary>
    /// 침입자 격퇴 드랍 판정 (KitchenEvents가 호출).
    /// 장물 주머니(유일)를 아직 못 얻었으면 그것부터 나온다 - "도둑이 훔친 물건"
    /// </summary>
    public static void TryIntruderDrop()
    {
        if (Random.value >= GameBalance.ItemDropChanceIntruder) return;

        if (!HasItem("item_swagbag"))
        {
            ItemData swag = ItemDatabase.Find("item_swagbag");
            if (swag != null) { Acquire(swag, "침입자가 떨어뜨렸다"); return; }
        }
        GrantRandom("침입자가 떨어뜨렸다");
    }

    /// <summary>
    /// 행상인 매대 2칸 구성: 일반 1 + 희귀 1 우선, 모자라면 남은 것 아무거나.
    /// 남은 물건이 1개면 b는 null.
    /// </summary>
    public static void GetShopOffer(out ItemData a, out ItemData b)
    {
        List<ItemData> commons = new List<ItemData>();
        List<ItemData> rares = new List<ItemData>();
        List<ItemData> all = ItemDatabase.All;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].rarity == ItemRarity.Special || HasItem(all[i].id)) continue;
            if (!IsItemUsable(all[i].id)) continue;
            if (all[i].rarity == ItemRarity.Common) commons.Add(all[i]);
            else rares.Add(all[i]);
        }

        a = null; b = null;
        if (commons.Count > 0) a = commons[Random.Range(0, commons.Count)];
        if (rares.Count > 0) b = rares[Random.Range(0, rares.Count)];

        // 한쪽 풀이 비었으면 남은 풀에서 채운다 (같은 것 중복만 방지)
        List<ItemData> rest = commons.Count > 0 ? commons : rares;
        if (a == null && rest.Count > 0) a = rest[Random.Range(0, rest.Count)];
        if (b == null)
        {
            for (int i = 0; i < rest.Count; i++)
                if (rest[i] != a) { b = rest[i]; break; }
        }
        if (a == null) { a = b; b = null; }   // 정리: a부터 채운다
    }

    /// <summary>판매가 (GameBalance 배율 반영)</summary>
    public static int PriceOf(ItemData item)
    {
        return item == null ? 0 : Mathf.RoundToInt(item.price * GameBalance.ItemPriceMul);
    }
}


/// <summary>전체 아이템 목록 (15종)</summary>
public static class ItemDatabase
{
    private static List<ItemData> all;

    public static List<ItemData> All
    {
        get
        {
            if (all == null) Build();
            return all;
        }
    }

    public static ItemData Find(string id)
    {
        List<ItemData> list = All;
        for (int i = 0; i < list.Count; i++)
            if (list[i].id == id) return list[i];
        return null;
    }

    private static void Build()
    {
        all = new List<ItemData>();

        // ==========================================================
        //  증강에서 이관된 5종 (효과 유지)
        // ==========================================================
        all.Add(new ItemData("item_knife", "잘 드는 식칼",
            "조리 제한 시간이 20% 길어진다 (굽기 눈금도 그만큼 느리게 간다)",
            ItemRarity.Common, 150,
            delegate { ItemManager.CookTimeMul *= 1.20f; }));

        all.Add(new ItemData("item_goldentool", "황금 조리 기구",
            "조리할 때 맞춰야 하는 구간이 35% 넓어진다 - 판정이 쉬워진다. 제한 시간도 10% 길어진다",
            ItemRarity.Rare, 300,
            delegate { ItemManager.CookJudgeMul *= 1.35f; ItemManager.CookTimeMul *= 1.10f; }));

        // v1.5 (v9.25 B5): 아래 셋(보험·부채·매트)과 숫돌·온도계·뒤집개·밸브·고글은 RelicCombatSet 을 끈 때만 - 켜면 싸움 유물 8 이 그 자리를 맡는다
        if (!GameBalance.RelicCombatSet)
        {
            all.Add(new ItemData("item_insurance", "보험 계약서",
                "주방 사고에 실패해도 기차가 받는 피해가 60% 줄어든다",
                ItemRarity.Common, 160,
                delegate { ItemManager.EventPenaltyMul *= 0.40f; }));

            all.Add(new ItemData("item_fan", "부채질 장인의 부채",
                "주방 사고가 2배 자주 오지만, 막아내면 보상이 3배",
                ItemRarity.Rare, 280,
                delegate { ItemManager.EventIntervalMul *= 0.5f; ItemManager.EventRewardMul *= 3f; }));

            all.Add(new ItemData("item_oilmat", "미끄럼 방지 매트",
                "오일 캑터스가 튀기는 기름에 안 미끄러진다",
                ItemRarity.Common, 140,
                delegate { ItemManager.OilImmune = true; }));
        }

        // ==========================================================
        //  신규 10종 (v1.5: 숫돌·온도계·뒤집개·밸브·고글은 RelicCombatSet 을 끈 때만)
        // ==========================================================
        if (!GameBalance.RelicCombatSet)
        {
            all.Add(new ItemData("item_whetstone", "휴대용 숫돌",
                "조리 도구가 절반만 닳는다 (전갈의 부식도 절반)",
                ItemRarity.Common, 150,
                delegate { ItemManager.ToolWearMul *= 0.5f; }));

            all.Add(new ItemData("item_thermometer", "구리 온도계",
                "굽기(그릴)에서 맞춰야 하는 구간이 25% 넓어진다",
                ItemRarity.Common, 150,
                delegate { ItemManager.GrillJudgeMul *= 1.25f; }));

            all.Add(new ItemData("item_spatula", "균형 잡힌 뒤집개",
                "볶기(팬) 제한 시간이 25% 길어진다",
                ItemRarity.Common, 150,
                delegate { ItemManager.StirTimeMul *= 1.25f; }));

            all.Add(new ItemData("item_valve", "압력 조절 밸브",
                "끓이기(솥)에서 맞춰야 하는 구간이 25% 넓어진다",
                ItemRarity.Common, 150,
                delegate { ItemManager.BoilJudgeMul *= 1.25f; }));
        }

        all.Add(new ItemData("item_extinguisher", "구리 소화기",
            "주방 화재가 웨이브마다 한 번은 저절로 꺼진다",
            ItemRarity.Rare, 260,
            delegate { ItemManager.HasExtinguisher = true; }));

        all.Add(new ItemData("item_rattrap", "랩터 덫",
            "손님이 기차에 뛰어들면 덫부터 밟는다 - 쫓아내는 데 채워야 하는 게이지가 40% 줄어든다",
            ItemRarity.Common, 160,
            delegate { ItemManager.IntruderGaugeMul *= 0.6f; }));

        if (!GameBalance.RelicCombatSet)
            all.Add(new ItemData("item_goggles", "김서림 방지 고글",
                "독침 프테라가 쏴 맞혀도 조리 속도가 안 느려진다",
                ItemRarity.Rare, 260,
                delegate { ItemManager.SnipeImmune = true; }));

        all.Add(new ItemData("item_apron", "선대의 앞치마",
            "조리에 실패해도 재료를 돌려받는다",
            ItemRarity.Rare, 320,
            delegate { ItemManager.FailRefund = true; }));

        all.Add(new ItemData("item_spicebag", "비밀 향신료 주머니",
            "조리를 PERFECT 로 마치면 20% 확률로 요리가 하나 더 나온다",
            ItemRarity.Rare, 300,
            delegate { ItemManager.PerfectExtraChance += 0.20f; }));

        // ==========================================================
        //  v1.5 (v9.25 B5): 싸움 유물 8 - 대가가 붙은 것 위주 (RelicCombatSet)
        // ==========================================================
        if (GameBalance.RelicCombatSet)
        {
            all.Add(new ItemData("item_rustymag", "녹슨 탄창",
                "모든 포탑 공격속도 +25%. 대신 사거리 -15%",
                ItemRarity.Common, 160,
                delegate { ItemManager.AspdMul *= 1.25f; ItemManager.RangeMul *= 0.85f; }));

            all.Add(new ItemData("item_hungryoven", "굶주린 화덕",
                "기차 HP 가 절반 아래면 모든 포탑 피해 +30%",
                ItemRarity.Rare, 260,
                delegate { ItemManager.LowHpAtkBonus += 0.30f; }));

            all.Add(new ItemData("item_pairspoon", "짝꿍 숟가락",
                "같은 요리 포탑이 둘 이상이면 그 포탑들 피해 +20%. 대신 공명이 꺼져",
                ItemRarity.Rare, 280,
                delegate { ItemManager.PairAtkBonus += 0.20f; ItemManager.NoResonance = true; }));

            all.Add(new ItemData("item_greedladle", "탐욕의 국자",
                "손님을 잡으면 골드 +40%. 대신 기차가 받는 피해 +15%",
                ItemRarity.Common, 150,
                delegate { ItemManager.KillGoldMul *= 1.40f; ItemManager.DamageTakenAdd += 0.15f; }));

            all.Add(new ItemData("item_heavypot", "무거운 냄비",
                "폭발 반경 +30%. 대신 모든 포탑 공격속도 -10%",
                ItemRarity.Common, 150,
                delegate { ItemManager.ExplodeRadiusMul *= 1.30f; ItemManager.AspdMul *= 0.90f; }));

            all.Add(new ItemData("item_tablecloth", "선대의 식탁보",
                "웨이브를 막을 때마다 기차 HP 40 회복",
                ItemRarity.Common, 140,
                delegate { ItemManager.WaveHeal += 40f; }));

            all.Add(new ItemData("item_hotsauce", "독한 소스",
                "화상·독 피해 +50%",
                ItemRarity.Common, 160,
                delegate { ItemManager.DotMul *= 1.50f; }));

            all.Add(new ItemData("item_emptyplate", "빈 접시",
                "비어 있는 포탑 칸 하나마다 다른 포탑 피해 +8%",
                ItemRarity.Rare, 240,
                delegate { ItemManager.EmptySlotAtkBonus += 0.08f; }));
        }

        // 유일 - 침입자 격퇴 시에만 낮은 확률로 획득 (비매품)
        all.Add(new ItemData("item_swagbag", "장물 주머니",
            "줍는 즉시 골드 +150. 그 뒤 뛰어든 손님을 쫓아낼 때마다 골드 +40",
            ItemRarity.Special, 0,
            delegate
            {
                GameManager.Instance?.AddGold(150);
                ItemManager.SwagGoldPerIntruder += 40;
            }));

        Debug.Log("[아이템] 데이터베이스 로드 완료 - 총 " + all.Count + "종");
    }
}


/// <summary>
/// v1.4: 유물 획득 알림 카드. 화면 왼쪽 위, HP·골드 판 바로 아래에 "유물 획득 / 이름 / 효과 / [V] 다시 보기" 를 잠깐 띄운다
/// (아래 가운데는 사고 배너·미끼·해동포 창 자리라 피했다. "내 상태" 는 왼쪽 위에 모은다).
/// 유물은 한 운행에 몇 번 안 나오는 사건이라 알림 줄 한 줄보다 크게 알린다. 게임은 멈추지 않고, 여러 개를 연달아 얻으면 차례로 보여 준다.
/// 다른 창(증강 선택·상점 등)이 떠 있으면 그 뒤에 가려진다 (정렬 458) - 창을 닫으면 남은 시간만큼 보인다.
/// 사용법: RelicToast.Show(item) - ItemManager.Acquire 가 부른다. 씬 세팅 없음
/// </summary>
public class RelicToast : MonoBehaviour
{
    private static RelicToast instance;

    private const float WIDTH = 470f;         // 좌상단 판과 같은 폭
    private const float SHOW_SEC = 4.5f;      // 떠 있는 시간 (실시간)
    private const float IN_SEC = 0.15f, OUT_SEC = 0.3f;

    private RectTransform root;
    private CanvasGroup group;
    private Image rarityBar;
    private Image ring;                       // 스킨이 있을 때의 테 (희귀도 색)
    private Text captionText, nameText, descText, footText;
    private readonly Queue<ItemData> queue = new Queue<ItemData>();
    private Coroutine showCo;
    private Canvas canvas;                    // v1.6: 날아오는 상자가 쓰는 캔버스

    /// <summary>v1.6 (B8): 갑판 상자가 날아갈 캔버스와 카드 자리 (카드가 꺼져 있어도 자리는 있다)</summary>
    public static void FlightTarget(out Canvas c, out RectTransform card)
    {
        Ensure();
        c = instance.canvas; card = instance.root;
    }

    private static void Ensure()
    {
        if (instance != null) return;
        GameObject go = new GameObject("RelicToast");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<RelicToast>();
        instance.Build();
        // 장면이 다시 불리면(운행 포기·재출발) 줄 서 있던 카드를 비운다 - 지난 운행의 유물 카드가 다음 판 위에 뜨지 않게
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += delegate { if (instance != null) instance.ClearAll(); };
    }

    /// <summary>획득 카드를 띄운다 (이미 떠 있으면 줄을 선다)</summary>
    public static void Show(ItemData item)
    {
        if (item == null) return;
        Ensure();
        instance.queue.Enqueue(item);
        if (instance.showCo == null) instance.showCo = instance.StartCoroutine(instance.Run());
    }

    private void Build()
    {
        canvas = UIFactory.CreateCanvas("RelicToastCanvas", 458);   // 알림 로그(455) 위, 속성 줄(470) 아래
        canvas.transform.SetParent(transform, false);
        GraphicRaycaster ray = canvas.GetComponent<GraphicRaycaster>();
        if (ray != null) ray.enabled = false;   // 클릭을 가로채지 않는다

        root = KitchenEventManager.MakeBox(canvas.transform, "Card", new Color(0.08f, 0.06f, 0.05f, 0.95f));
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(8f, -196f);   // 좌상단 판(위에서 8 ~ 184) 바로 아래
        root.sizeDelta = new Vector2(WIDTH, 120f);
        Image rootImg = root.GetComponent<Image>();
        rootImg.raycastTarget = false;
        if (UISkin.Available)
        {
            // 다른 창과 같은 무쇠 평판 + 테 (테 색 = 희귀도, Run 에서 바꾼다). 직접 지정해 두면 스킨 스캐너가 다시 건드리지 않는다
            UISkin.Plate(rootImg, Color.white);
            ring = UISkin.AddRing(root, UISkin.BRASS_DIM);
        }
        else
        {
            Outline edge = root.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(0.72f, 0.44f, 0.2f, 0.95f);
            edge.effectDistance = new Vector2(2f, -2f);
        }
        group = root.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        // 왼쪽 희귀도 색 띠
        RectTransform bar = KitchenEventManager.MakeBox(root, "Rarity", Color.white);
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(0f, 1f);
        bar.pivot = new Vector2(0f, 0.5f);
        bar.anchoredPosition = Vector2.zero;
        bar.sizeDelta = new Vector2(8f, 0f);
        rarityBar = bar.GetComponent<Image>();
        rarityBar.raycastTarget = false;

        captionText = MakeLine("Caption", 16, new Vector2(24f, -8f), 22f, TextAnchor.MiddleLeft);
        nameText = MakeLine("Name", 26, new Vector2(24f, -30f), 34f, TextAnchor.MiddleLeft);
        nameText.fontStyle = FontStyle.Bold;
        descText = MakeLine("Desc", 19, new Vector2(24f, -68f), 26f, TextAnchor.UpperLeft);
        descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        descText.lineSpacing = 1.1f;
        footText = MakeLine("Foot", 15, new Vector2(24f, 0f), 22f, TextAnchor.MiddleRight);
        footText.rectTransform.anchorMin = new Vector2(0f, 0f);
        footText.rectTransform.anchorMax = new Vector2(1f, 0f);
        footText.rectTransform.pivot = new Vector2(0.5f, 0f);
        footText.rectTransform.anchoredPosition = new Vector2(0f, 6f);
        footText.rectTransform.sizeDelta = new Vector2(-36f, 22f);
        footText.color = new Color(0.63f, 0.55f, 0.43f, 1f);
        footText.text = "[V] 가진 증강·유물 다시 보기";

        root.gameObject.SetActive(false);
    }

    private void ClearAll()
    {
        queue.Clear();
        if (showCo != null) { StopCoroutine(showCo); showCo = null; }
        if (root != null) root.gameObject.SetActive(false);
        // v1.6: 날아가던 상자(UIFeel.FlyTo 의 Image)가 캔버스에 남지 않게
        if (canvas != null)
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
                if (canvas.transform.GetChild(i).name == "Fly") Destroy(canvas.transform.GetChild(i).gameObject);
    }

    /// <summary>카드 안 글자 한 줄 (왼쪽 위 기준 자리, 폭은 카드 안쪽 전체)</summary>
    private Text MakeLine(string name, int size, Vector2 pos, float height, TextAnchor align)
    {
        Text t = KitchenEventManager.MakeText(root, name, "", size, new Color(0.97f, 0.91f, 0.78f, 1f));
        RectTransform rt = t.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(-(pos.x + 18f), height);
        t.alignment = align;
        return t;
    }

    private IEnumerator Run()
    {
        while (queue.Count > 0)
        {
            ItemData item = queue.Dequeue();
            Color rc = item.RarityColor();
            rarityBar.color = rc;
            if (ring != null) ring.color = rc;
            captionText.text = "유물 획득  ·  " + item.RarityName();
            captionText.color = rc;
            nameText.text = item.name;
            descText.text = item.desc;

            // 설명 줄 수에 맞춰 카드 높이 (위 68 + 설명 + 아래 34)
            root.gameObject.SetActive(true);
            float descH = Mathf.Max(26f, descText.preferredHeight);
            descText.rectTransform.sizeDelta = new Vector2(descText.rectTransform.sizeDelta.x, descH);
            root.sizeDelta = new Vector2(WIDTH, 68f + descH + 34f);

            float t = 0f;
            while (t < SHOW_SEC)
            {
                t += Time.unscaledDeltaTime;
                float kin = Mathf.Clamp01(t / IN_SEC);
                float ein = 1f - (1f - kin) * (1f - kin);
                float kout = Mathf.Clamp01((t - (SHOW_SEC - OUT_SEC)) / OUT_SEC);
                group.alpha = ein * (1f - kout);
                float sc = Mathf.Lerp(0.92f, 1f, ein);
                root.localScale = new Vector3(sc, sc, 1f);
                yield return null;
            }
            root.gameObject.SetActive(false);
        }
        showCo = null;
    }
}
