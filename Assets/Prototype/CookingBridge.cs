using UnityEngine;

/// <summary>
/// [CookingBridge.cs] v1.7 (v9.26 2026-10-16 마감 정리: 미니게임 전 임시 함수 QuickCook 삭제) / v1.6 (v9.23 2026-10-14: 굽기 요리를 완성하면 동면자의 백야의 입김이 풀린다 (BossEnemy.OnGrillCooked) / 점검 수정: 요리 진화 조리 성공 = 그 전설 요리 숙련 1회 (CompleteStockFusion 이 FoodStock.CountCook)) / v1.5 (v9.21.2 2026-10-14 문구 팩: 용어 통일 - 접시->요리(개) / 데미지->피해 / 슬롯->칸 / 랜덤->무작위 / 레시피->요리 / 적·아군->손님 / 드랍->드롭 / 마법 저항->저항 / 힐러·러시·청구 / 진화 조리 판 이름) / v1.4 (v9.21.1 2026-10-13 4차 피드백 2번: 접시 합성(FuseFoods)도 진화 조리 미니게임을 거친다 - InfusingMinigame.BeginForStock 을 부르고 끝나면 CompleteStockFusion 이 소모·지급 (보너스 = 접시 +1). GameBalance.InfuseStockOn) / v1.3 (v9.18 2026-10-06: 견습 운행에서는 숙련 특전 "PERFECT 접시 +1" 을 끈다 - 안내는 2접시인데 3접시가 나왔다) / v1.2 (v9.10.1 2026-09-21: 정차 조리 횟수 제한 - GameBalance.StopCookLimit, 웨이브 시작에 WaveManager 가 StopCooksUsed 를 0 으로)
/// 재료 -> 조리 -> 요리 획득 흐름의 연결부 (정적 클래스)
/// v1.1 (교수 피드백 09-14): 런 통계(조리/실패 횟수, 마지막 성공 시각) + Bad 판정 부분 환급 스위치
/// 지금은 "즉시 완성" 모드. 나중에 기존 미니게임(CookingSystem)과 연결하면
/// StartCook 후 미니게임 결과에서 FinishCook(품질)을 호출하는 구조가 된다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public static class CookingBridge
{
    // 조리 중인 레시피 (StartCook에서 설정)
    public static string pendingRecipeId = "";

    // v1.1 (교수 피드백 09-14): 런 통계 - 프롤로그 조리 게이트(WaveManager)와 관찰 시트용
    /// <summary>이번 런에서 Good 이상으로 끝난 조리 횟수 / Bad 횟수</summary>
    public static int CooksThisRun = 0;
    public static int BadsThisRun = 0;
    /// <summary>마지막으로 Good 이상 조리가 끝난 시각 (Time.time, 0 = 없음)</summary>
    public static float LastGoodCookTime = 0f;

    /// <summary>런 시작 시 통계 초기화 (GameManager가 호출)</summary>
    public static void ResetRunStats()
    {
        Enemy.goldCarry = 0f;   // v1.4 (v9.21.1): 처치 골드 소수 찌꺼기도 새 운행에서 0 (GameManager 가 새 운행 시작에 여기를 부른다)
        CooksThisRun = 0; BadsThisRun = 0; LastGoodCookTime = 0f; StopCooksUsed = 0;
    }

    // ── v1.2: 정차 조리 제한 ──
    // 유저 소감: "정차(행상인 창) 동안 요리를 무한으로 뽑아 정비를 다 하고 넘어가니 쉬워진다" + 개정안 §4.
    // 정차(Town) 상태에서 시작한 조리 횟수를 세고, GameBalance.StopCookLimit 에 닿으면 출발할 때까지 조리 시작을 막는다.
    // 견습 운행·프롤로그 조리 게이트(둘 다 WaveManager.TutorialGateActive)는 제한하지 않는다. 웨이브가 시작되면(StartWave) 0 으로.
    /// <summary>이번 정차에서 시작한 조리 횟수</summary>
    public static int StopCooksUsed = 0;

    /// <summary>지금 정차 중이고 제한이 걸린 상태인가 (정차 아님 / 견습 / 제한 -1 이면 false)</summary>
    public static bool StopCookLimited
    {
        get
        {
            if (GameBalance.StopCookLimit < 0) return false;
            if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Town) return false;
            if (WaveManager.TutorialGateActive || TutorialDirector.Active) return false;
            return true;
        }
    }

    /// <summary>이번 정차에 남은 조리 횟수 (제한 없으면 -1)</summary>
    public static int StopCooksLeft
    {
        get { return StopCookLimited ? Mathf.Max(0, GameBalance.StopCookLimit - StopCooksUsed) : -1; }
    }

    /// <summary>주방 패널 안내줄용: "정차 조리 1/2 남음   " 또는 "" </summary>
    public static string StopCookHint()
    {
        if (!StopCookLimited) return "";
        if (GameBalance.StopCookLimit == 0) return "[정차] 조리는 달리면서 - 출발하면 화덕이 켜진다   ";
        return "[정차] 조리 " + StopCooksLeft + "/" + GameBalance.StopCookLimit + " 남음 - 출발하면 제한 없음   ";
    }

    /// <summary>재료 2개로 조리 시작. 재료 차감 + 레시피 결정. 성공하면 true</summary>
    public static bool StartCook(MaterialType a, MaterialType b)
    {
        if (MaterialInventory.Instance == null) return false;

        // v1.2: 정차 조리 제한
        if (StopCookLimited && StopCooksUsed >= GameBalance.StopCookLimit)
        {
            UIManager.Instance?.ShowDanger(GameBalance.StopCookLimit == 0
                ? "[정차] 조리는 달리면서 한다 - 출발([Enter]) 뒤 화덕이 켜진다"
                : "[정차] 이번 정차 조리 끝 (" + GameBalance.StopCookLimit + "/" + GameBalance.StopCookLimit + ") - 출발([Enter]) 뒤 제한 없음");
            SoundManager.Play("sfx_ui_click");
            return false;
        }
        if (!MaterialInventory.Instance.TryConsume(a, b))
        {
            Debug.Log("[CookingBridge] 재료 부족: " + a + " + " + b);
            return false;
        }
        pendingRecipeId = RecipeDatabase.MakeKey(a, b);
        if (StopCookLimited) StopCooksUsed++;   // v1.2: 정차 중 시작한 조리만 센다
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
            BadsThisRun++;

            // Phase 2-3 아이템 '선대의 앞치마': 실패해도 재료를 전부 돌려받는다
            // v1.1 (교수 피드백 B4): 앞치마가 없어도 GameBalance.BadRefundCount(기본 0, 실험안 1)개는
            // 돌려준다 - 초보의 재시도 보장. 요리는 계속 0개.
            // (pendingRecipeId는 T1 조리 키 "재료+재료" 형식 - 여기서 재료 2개를 복원)
            int refund = ItemManager.FailRefund ? 2 : Mathf.Clamp(GameBalance.BadRefundCount, 0, 2);
            if (refund > 0 && MaterialInventory.Instance != null)
            {
                string[] parts = pendingRecipeId.Split('+');
                MaterialType ra, rb;
                if (parts.Length == 2
                    && TryParseMaterial(parts[0], out ra) && TryParseMaterial(parts[1], out rb))
                {
                    MaterialInventory.Instance.Add(ra, 1);
                    if (refund >= 2) MaterialInventory.Instance.Add(rb, 1);
                    UIManager.Instance?.ShowStatChange(ItemManager.FailRefund
                        ? "[선대의 앞치마] 실패한 재료를 되살렸다"
                        : "[실패] 타다 만 재료 1개는 건졌다");
                }
            }
            Debug.Log("[CookingBridge] 조리 실패! 환급 " + refund + "개");
        }
        else
        {
            int n = (quality == "perfect") ? 2 : 1;

            // P1+: 마스터 요리(숙련 100회) - PERFECT 조리 수량 +1 (2 -> 3)
            // v1.3: 견습 운행(자유 연습 제외)에서는 끈다 - 배우는 자리는 누가 해도 같은 결과가 나와야 한다
            bool tutorialPlain = GameBalance.TutorialFixedRules && TutorialDirector.Active && !TutorialDirector.SandboxActive;
            if (quality == "perfect" && !tutorialPlain
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

            // v1.1: 런 통계
            CooksThisRun++;
            LastGoodCookTime = Time.time;

            RecipeData r = RecipeDatabase.Get(pendingRecipeId);
            Debug.Log("[CookingBridge] " + r.displayName + " x" + n + " 획득! (" + quality + ")");
            // v1.6: 굽기 요리를 완성했다 - 동면자의 백야의 입김(서리)이 녹는다 ("불을 피워 녹인다")
            if (r != null && KitchenPanel.MethodOf(r) == 0) BossEnemy.OnGrillCooked();
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

        // v1.4 (v9.21.1): 접시 합성도 진화 조리를 거친다 - 미니게임이 끝나면 CompleteStockFusion 이 소모·지급. 지금은 아무것도 소모하지 않는다
        if (GameBalance.InfuseStockOn)
        {
            if (InfusingMinigame.IsActive) { UIManager.Instance?.ShowStatChange("진화 조리가 이미 진행 중"); return null; }
            if (CookingMinigame.IsActive) { UIManager.Instance?.ShowStatChange("조리 중에는 진화 조리를 못 한다"); return null; }
            if (AugmentManager.BasicsDoctrine) { UIManager.Instance?.ShowStatChange("선대의 기본기 - 전설 요리 진화는 봉인됐다"); return null; }
            InfusingMinigame.BeginForStock(recipeIdA, recipeIdB, result);
            return result.recipeId;
        }

        // 소모 + 지급
        FoodStock.Instance.TryConsume(recipeIdA, 1);
        FoodStock.Instance.TryConsume(recipeIdB, 1);
        FoodStock.Instance.Add(result.recipeId, 1);

        Debug.Log("[CookingBridge] 합성 성공! " + a.displayName + " + " + b.displayName +
                  " = " + result.displayName);
        return result.recipeId;
    }

    /// <summary>v1.4 (v9.21.1): 접시 진화 조리가 끝났다 - 접시 둘을 소모하고 전설 접시 1 + 보너스(판정이 좋으면 InfuseStockBonusPlates) 지급. 그사이 접시를 써 버렸으면 아무것도 잃지 않는다</summary>
    public static void CompleteStockFusion(string recipeIdA, string recipeIdB, RecipeData result, int bonusPlates)
    {
        if (FoodStock.Instance == null || result == null) return;
        int need = recipeIdA == recipeIdB ? 2 : 1;
        if (FoodStock.Instance.Get(recipeIdA) < need || FoodStock.Instance.Get(recipeIdB) < 1)
        {
            UIManager.Instance?.ShowStatChange("진화 조리 무산 - 재료로 쓸 요리가 없어졌다 (아무것도 잃지 않음)");
            return;
        }
        FoodStock.Instance.TryConsume(recipeIdA, 1);
        FoodStock.Instance.TryConsume(recipeIdB, 1);
        int made = 1 + Mathf.Max(0, bonusPlates);
        FoodStock.Instance.Add(result.recipeId, made);
        FoodStock.Instance.CountCook(result.recipeId);   // v1.6: 진화 조리 성공 = 그 전설 요리를 한 번 만든 것 (숙련)
        UIManager.Instance?.ShowStatChange("[진화] " + result.displayName + " " + made + "개" + (bonusPlates > 0 ? " (잘 익혀 +" + bonusPlates + ")" : ""));
        Debug.Log("[CookingBridge] 접시 진화 조리 완료: " + result.displayName + " x" + made);
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