using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// [ItemSystem.cs] v1 (신규 파일) - Phase 2-3: 아이템(유물) 시스템
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
    public static void Acquire(ItemData item, string sourceLabel = null)
    {
        if (item == null || HasItem(item.id)) return;

        Owned.Add(item);
        if (item.apply != null) item.apply();

        if (string.IsNullOrEmpty(sourceLabel))
            UIManager.Instance?.ShowStatChange("[아이템] " + item.name + " - " + item.desc);
        else
            UIManager.Instance?.ShowStatChange("[전리품] " + sourceLabel + " - " + item.name + "!");

        SoundManager.Play("sfx_pickup");
        Debug.Log("[아이템] 획득: " + item.name + " (" + item.RarityName() + ")"
            + (sourceLabel != null ? " / 출처: " + sourceLabel : ""));
    }

    /// <summary>미보유 아이템이 남아 있는가 (Special 제외 - 행상인/드랍 공용 체크)</summary>
    public static bool HasStock()
    {
        List<ItemData> all = ItemDatabase.All;
        for (int i = 0; i < all.Count; i++)
            if (all[i].rarity != ItemRarity.Special && !HasItem(all[i].id)) return true;
        return false;
    }

    /// <summary>
    /// 무작위 미보유 아이템 1개 지급 (Special 제외).
    /// 전부 보유 중이면 골드로 대체 - "행낭이 가득 찼다"
    /// </summary>
    public static void GrantRandom(string sourceLabel)
    {
        List<ItemData> pool = new List<ItemData>();
        List<ItemData> all = ItemDatabase.All;
        for (int i = 0; i < all.Count; i++)
            if (all[i].rarity != ItemRarity.Special && !HasItem(all[i].id)) pool.Add(all[i]);

        if (pool.Count == 0)
        {
            GameManager.Instance?.AddGold(60);
            UIManager.Instance?.ShowStatChange("[전리품] 행낭이 가득 찼다... 대신 골드 +60");
            return;
        }
        Acquire(pool[Random.Range(0, pool.Count)], sourceLabel);
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
            "조리 미니게임 제한 시간 +20% (굽기 커서도 느려진다)",
            ItemRarity.Common, 150,
            delegate { ItemManager.CookTimeMul *= 1.20f; }));

        all.Add(new ItemData("item_goldentool", "황금 조리 기구",
            "조리 판정 존 +35%, 제한 시간 +10%",
            ItemRarity.Rare, 300,
            delegate { ItemManager.CookJudgeMul *= 1.35f; ItemManager.CookTimeMul *= 1.10f; }));

        all.Add(new ItemData("item_insurance", "보험 계약서",
            "주방 이벤트 실패 페널티 60% 감소",
            ItemRarity.Common, 160,
            delegate { ItemManager.EventPenaltyMul *= 0.40f; }));

        all.Add(new ItemData("item_fan", "부채질 장인의 부채",
            "주방 이벤트가 2배 자주 발생하지만, 성공 보상 3배",
            ItemRarity.Rare, 280,
            delegate { ItemManager.EventIntervalMul *= 0.5f; ItemManager.EventRewardMul *= 3f; }));

        all.Add(new ItemData("item_oilmat", "미끄럼 방지 매트",
            "오일 캑터스의 기름 튐에 면역이 된다",
            ItemRarity.Common, 140,
            delegate { ItemManager.OilImmune = true; }));

        // ==========================================================
        //  신규 10종
        // ==========================================================
        all.Add(new ItemData("item_whetstone", "휴대용 숫돌",
            "조리 도구 마모가 절반이 된다 (전갈의 부식 포함)",
            ItemRarity.Common, 150,
            delegate { ItemManager.ToolWearMul *= 0.5f; }));

        all.Add(new ItemData("item_thermometer", "구리 온도계",
            "굽기 판정 존 +25%",
            ItemRarity.Common, 150,
            delegate { ItemManager.GrillJudgeMul *= 1.25f; }));

        all.Add(new ItemData("item_spatula", "균형 잡힌 뒤집개",
            "볶기 제한 시간 +25%",
            ItemRarity.Common, 150,
            delegate { ItemManager.StirTimeMul *= 1.25f; }));

        all.Add(new ItemData("item_valve", "압력 조절 밸브",
            "끓이기 안정존 +25%",
            ItemRarity.Common, 150,
            delegate { ItemManager.BoilJudgeMul *= 1.25f; }));

        all.Add(new ItemData("item_extinguisher", "구리 소화기",
            "주방 화재를 웨이브당 1회 자동 진압한다",
            ItemRarity.Rare, 260,
            delegate { ItemManager.HasExtinguisher = true; }));

        all.Add(new ItemData("item_rattrap", "랩터 덫",
            "침입자가 덫을 밟고 시작한다: 격퇴 게이지 -40%",
            ItemRarity.Common, 160,
            delegate { ItemManager.IntruderGaugeMul *= 0.6f; }));

        all.Add(new ItemData("item_goggles", "김서림 방지 고글",
            "독침 프테라의 저격(조리 속도 저하)을 무시한다",
            ItemRarity.Rare, 260,
            delegate { ItemManager.SnipeImmune = true; }));

        all.Add(new ItemData("item_apron", "선대의 앞치마",
            "조리에 실패해도 재료를 돌려받는다",
            ItemRarity.Rare, 320,
            delegate { ItemManager.FailRefund = true; }));

        all.Add(new ItemData("item_spicebag", "비밀 향신료 주머니",
            "PERFECT 조리 시 20% 확률로 요리 +1",
            ItemRarity.Rare, 300,
            delegate { ItemManager.PerfectExtraChance += 0.20f; }));

        // 유일 - 침입자 격퇴 시에만 낮은 확률로 획득 (비매품)
        all.Add(new ItemData("item_swagbag", "장물 주머니",
            "획득 즉시 골드 +150. 이후 침입자 격퇴마다 골드 +40",
            ItemRarity.Special, 0,
            delegate
            {
                GameManager.Instance?.AddGold(150);
                ItemManager.SwagGoldPerIntruder += 40;
            }));

        Debug.Log("[아이템] 데이터베이스 로드 완료 - 총 " + all.Count + "종");
    }
}
