using UnityEngine;

/// <summary>
/// [CookingBridge.cs]
/// 재료 -> 조리 -> 요리 획득 흐름의 연결부 (정적 클래스)
/// 지금은 "즉시 완성" 모드. 나중에 기존 미니게임(CookingSystem)과 연결하면
/// StartCook 후 미니게임 결과에서 FinishCook(품질)을 호출하는 구조가 된다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public static class CookingBridge
{
    // 조리 중인 레시피 (StartCook에서 설정)
    public static string pendingRecipeId = "";

    /// <summary>재료 2개로 조리 시작. 재료 차감 + 레시피 결정. 성공하면 true</summary>
    public static bool StartCook(MaterialType a, MaterialType b)
    {
        if (MaterialInventory.Instance == null) return false;
        if (!MaterialInventory.Instance.TryConsume(a, b))
        {
            Debug.Log("[CookingBridge] 재료 부족: " + a + " + " + b);
            return false;
        }
        pendingRecipeId = RecipeDatabase.MakeKey(a, b);
        Debug.Log("[CookingBridge] 조리 시작: " + RecipeDatabase.Get(pendingRecipeId).displayName);
        return true;
    }

    /// <summary>
    /// 조리 완료. quality: "perfect" / "good" / "bad"
    /// perfect = 요리 2개, good = 1개, bad = 획득 없음 (추후 폭탄 지급 예정)
    /// </summary>
    public static void FinishCook(string quality)
    {
        if (string.IsNullOrEmpty(pendingRecipeId)) return;

        if (quality == "bad")
        {
            // Phase 2-3 아이템 '선대의 앞치마': 실패해도 재료를 돌려받는다
            // (pendingRecipeId는 T1 조리 키 "재료+재료" 형식 - 여기서 재료 2개를 복원)
            if (ItemManager.FailRefund && MaterialInventory.Instance != null)
            {
                string[] parts = pendingRecipeId.Split('+');
                MaterialType ra, rb;
                if (parts.Length == 2
                    && TryParseMaterial(parts[0], out ra) && TryParseMaterial(parts[1], out rb))
                {
                    MaterialInventory.Instance.Add(ra, 1);
                    MaterialInventory.Instance.Add(rb, 1);
                    UIManager.Instance?.ShowStatChange("[선대의 앞치마] 실패한 재료를 되살렸다");
                }
            }
            Debug.Log("[CookingBridge] 조리 실패! (폭탄 시스템은 추후 연결)");
        }
        else
        {
            int n = (quality == "perfect") ? 2 : 1;

            // P1+: 마스터 요리(숙련 100회) - PERFECT 조리 수량 +1 (2 -> 3)
            if (quality == "perfect"
                && MetaProgress.GetMasteryTier(pendingRecipeId) >= GameBalance.MasteryPerfectTier)
                n += 1;

            // Phase 2-3 아이템 '비밀 향신료 주머니': PERFECT 시 확률로 요리 +1
            if (quality == "perfect" && ItemManager.PerfectExtraChance > 0f
                && Random.value < ItemManager.PerfectExtraChance)
            {
                n += 1;
                UIManager.Instance?.ShowStatChange("[향신료 주머니] 풍미 폭발! 요리 +1");
            }

            // Phase 2-1: 스피노 베팅 [완벽한 접시] PERFECT 카운트
            if (quality == "perfect")
                SpinoBet.CountPerfect();

            FoodStock.Instance.Add(pendingRecipeId, n);

            // P1+: 요리 숙련 카운트 (평생 누적 - 마일스톤 알림은 FoodStock이 처리)
            FoodStock.Instance.CountCook(pendingRecipeId);

            RecipeData r = RecipeDatabase.Get(pendingRecipeId);
            Debug.Log("[CookingBridge] " + r.displayName + " x" + n + " 획득! (" + quality + ")");
        }
        pendingRecipeId = "";
    }

    /// <summary>
    /// B-1: 조리 자발 중단 (CookingMinigame [ESC]).
    /// 시작할 때 차감한 재료 2개를 그대로 돌려준다 - 위기 대응을 위한 중단이
    /// 손해가 되지 않게. (pendingRecipeId는 T1 조리 키 "재료+재료" 형식)
    /// </summary>
    public static void AbortCook()
    {
        if (string.IsNullOrEmpty(pendingRecipeId)) return;

        if (MaterialInventory.Instance != null)
        {
            string[] parts = pendingRecipeId.Split('+');
            MaterialType a, b;
            if (parts.Length == 2
                && TryParseMaterial(parts[0], out a) && TryParseMaterial(parts[1], out b))
            {
                MaterialInventory.Instance.Add(a, 1);
                MaterialInventory.Instance.Add(b, 1);
            }
        }

        UIManager.Instance?.ShowStatChange("[조리 중단] 재료를 되찾았다 - 현장으로!");
        Debug.Log("[CookingBridge] 조리 자발 중단 - 재료 환급: " + pendingRecipeId);
        pendingRecipeId = "";
    }

    /// <summary>간편 조리: 발견한 레시피를 바로 조리 (재료 자동 차감)</summary>
    public static bool QuickCook(string recipeId)
    {
        RecipeData r = RecipeDatabase.Get(recipeId);
        if (r == null || r.tier != 1) return false;

        // recipeId 형식: "fire+meat" -> 재료 2개 복원
        string[] parts = recipeId.Split('+');
        MaterialType a, b;
        if (!TryParseMaterial(parts[0], out a)) return false;
        if (!TryParseMaterial(parts[1], out b)) return false;

        if (!StartCook(a, b)) return false;

        // 즉시 완성 모드 (미니게임 연결 전 임시)
        FinishCook("good");
        return true;
    }

    /// <summary>
    /// T2 합성: T1 요리 2개 -> 태그 조합으로 전설 요리 1개
    /// 성공하면 결과 recipeId 반환, 실패하면 null
    /// </summary>
    public static string FuseFoods(string recipeIdA, string recipeIdB)
    {
        RecipeData a = RecipeDatabase.Get(recipeIdA);
        RecipeData b = RecipeDatabase.Get(recipeIdB);

        // T1 요리만 합성 가능
        if (a == null || b == null || a.tier != 1 || b.tier != 1)
        {
            Debug.Log("[CookingBridge] 합성 불가: T1 요리만 가능");
            return null;
        }

        // 같은 요리 2개 합성이면 보유량 2개 필요
        if (recipeIdA == recipeIdB)
        {
            if (FoodStock.Instance.Get(recipeIdA) < 2)
            {
                Debug.Log("[CookingBridge] 합성 실패: " + a.displayName + " 2개 필요");
                return null;
            }
        }
        else
        {
            if (FoodStock.Instance.Get(recipeIdA) < 1 || FoodStock.Instance.Get(recipeIdB) < 1)
            {
                Debug.Log("[CookingBridge] 합성 실패: 재료 요리 부족");
                return null;
            }
        }

        // 태그 조합으로 T2 결과 조회
        RecipeData result = RecipeDatabase.GetFusion(a.tag, b.tag);
        if (result == null)
        {
            Debug.Log("[CookingBridge] 해당 태그 조합의 T2 없음: " + a.tag + " + " + b.tag);
            return null;
        }

        // 소모 + 지급
        FoodStock.Instance.TryConsume(recipeIdA, 1);
        FoodStock.Instance.TryConsume(recipeIdB, 1);
        FoodStock.Instance.Add(result.recipeId, 1);

        Debug.Log("[CookingBridge] 합성 성공! " + a.displayName + " + " + b.displayName +
                  " = " + result.displayName);
        return result.recipeId;
    }

    private static bool TryParseMaterial(string s, out MaterialType result)
    {
        // 소문자 문자열 -> enum ("meat" -> MaterialType.Meat)
        switch (s)
        {
            case "meat": result = MaterialType.Meat; return true;
            case "armor": result = MaterialType.Armor; return true;
            case "elec": result = MaterialType.Elec; return true;
            case "fire": result = MaterialType.Fire; return true;
            case "ice": result = MaterialType.Ice; return true;
            case "poison": result = MaterialType.Poison; return true;
        }
        result = MaterialType.Meat;
        return false;
    }
}