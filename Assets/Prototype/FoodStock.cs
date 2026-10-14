using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [FoodStock.cs] v2.4 (v9.23 2026-10-14 점검 수정: 숙련 알림 말을 튜토리얼과 같게 - "조리 판정 칸" -> "초록·노란 구간", 이름 뒤 을/를을 받침으로 고른다 (예전엔 "을(를)") / 진화 조리 성공도 숙련으로 센다 - 부르는 곳은 CookingBridge·TurretSlotManager) / v2.3 (v9.21.2 2026-10-14 문구 팩: 용어 통일 - 접시->요리(개) / 데미지->피해 / 슬롯->칸 / 랜덤->무작위 / 레시피->요리 / 적·아군->손님 / 드랍->드롭 / 마법 저항->저항 / 힐러·러시·청구 / 진화 조리 판 이름) / v2.2 (v9.11 2026-09-22: 방금 늘어난 요리 LastAddedId/Frame - HUD 카드 날아가기·튀기 연출용) / v2.1 (v9.10 2026-09-17: 숙련 알림을 일상어로 - "이 요리를 N번 만들었다 - 손에 익어 ..." 테스터 "숙련도 어쩌구 설명 필요") / v2
/// 완성된 요리 보관소 + 도감(발견) 관리 (싱글톤)
/// - 조리 성공 -> Add / 슬롯 투입, T2 합성 -> TryConsume
/// - v2 변경점 (도감 영구화):
///   1) 요리를 처음 만들면 MetaProgress에 영구 기록 -> 다음 런에서도 도감에 남는다
///   2) IsDiscovered()가 "과거 런 발견"까지 포함해서 true 반환
///   3) 역대 최초 발견이면 화면에 [도감] 신규 등록 알림 표시
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class FoodStock : MonoBehaviour
{
    public static FoodStock Instance { get; private set; }

    // recipeId -> 보유 개수 (이번 런 한정)
    private Dictionary<string, int> stock = new Dictionary<string, int>();

    // 이번 런에서 한 번이라도 얻어본 요리 (신규 획득 연출용)
    private HashSet<string> discovered = new HashSet<string>();

    public event Action OnChanged;

    // 이번 런에서 새 요리를 처음 얻었을 때 알림 (연출/사운드용). 인자: recipeId
    public event Action<string> OnDiscovered;

    // ─────────────────────────────────────────────
    // P1+: 요리 숙련 (단골 메뉴의 영구화 - 사용자 결정 2026-08-24)
    // 성공 조리마다 MetaProgress에 "평생" 횟수를 쌓고, 마일스톤 통과 순간 알림을 띄운다.
    // 실제 저장/보너스 조회는 MetaProgress.GetCookCount/GetMasteryAtk 등이 담당.
    // ─────────────────────────────────────────────

    /// <summary>v2.4: 이름 뒤 목적격 조사 - 마지막 글자에 받침이 있으면 "을", 없으면 "를" (한글로 안 끝나면 "을(를)"). 보스 무방비 안내(BossGimmickSystem)도 쓴다</summary>
    public static string Eul(string word)
    {
        if (string.IsNullOrEmpty(word)) return "을(를)";
        char last = word[word.Length - 1];
        if (last < 0xAC00 || last > 0xD7A3) return "을(를)";
        return ((last - 0xAC00) % 28) != 0 ? "을" : "를";
    }

    /// <summary>성공 조리 1회 기록 (CookingBridge.FinishCook 일반 조리 / v2.4: 진화 조리 성공 - CompleteStockFusion·TurretSlotManager.CompleteFusion)</summary>
    public void CountCook(string recipeId)
    {
        if (string.IsNullOrEmpty(recipeId)) return;

        int c = MetaProgress.AddCookCount(recipeId);
        int tier = GameBalance.MasteryTier(c);
        int prevTier = GameBalance.MasteryTier(c - 1);
        if (tier <= prevTier) return;   // 마일스톤 통과 순간에만 알림

        RecipeData r = RecipeDatabase.Get(recipeId);
        string shownName = r != null ? r.displayName : recipeId;
        string title = GameBalance.MasteryTitles[tier];

        // v1.3: 일상어 - 무슨 일이 왜 생겼는지 (칭호는 괄호). v2.4: 두 줄로 - 한 줄이면 알림 줄(560)을 두 배 넘어 글자가 너무 작아졌다
        string head = "[단골 요리] " + shownName + Eul(shownName) + " " + c + "번 만들었다 (" + title + ")";
        string msg = "이 요리 포탑 공격력 +" + Mathf.RoundToInt(GameBalance.MasteryAtkBonus[tier] * 100f) + "%";
        if (GameBalance.MasteryJudgeBonus[tier] > 0f)
            msg += ", 초록·노란 구간 +" + Mathf.RoundToInt(GameBalance.MasteryJudgeBonus[tier] * 100f) + "%";
        if (tier == GameBalance.MasteryStartLevelTier)
            msg += ", 새로 놓으면 레벨 2부터";
        if (tier >= GameBalance.MasteryPerfectTier)
            msg += ", PERFECT면 하나 더";

        UIManager.Instance?.ShowStatChange(head);
        UIManager.Instance?.ShowStatChange(msg);
        SoundManager.Play("sfx_judge_perfect");
        Debug.Log("[FoodStock] 숙련 마일스톤: " + head + " / " + msg);

        // 100회 마스터: 최초 1회 명성 + 전용 문구 (팬 리워드)
        if (tier >= GameBalance.MasteryPerfectTier && MetaProgress.TryGrantMasterFame(recipeId))
        {
            UIManager.Instance?.ShowStatChange("[마스터] " + shownName + " - 죽음도 손맛은 앗아가지 못한다 (명성 +"
                + GameBalance.MasteryFame + ")");
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public int Get(string recipeId)
    {
        int n;
        return stock.TryGetValue(recipeId, out n) ? n : 0;
    }

    // v2.2: 방금 늘어난 요리 (GameHUD 가 같은 프레임에 카드 연출을 건다)
    public static string LastAddedId = "";
    public static int LastAddedFrame = -1;

    public void Add(string recipeId, int n)
    {
        stock[recipeId] = Get(recipeId) + n;
        if (n > 0) { LastAddedId = recipeId; LastAddedFrame = Time.frameCount; }

        // 이번 런 첫 획득이면 발견 처리 (개수 0이어도 발견됨 - 치트용)
        if (!discovered.Contains(recipeId))
        {
            discovered.Add(recipeId);
            if (OnDiscovered != null) OnDiscovered(recipeId);

            // v2: 영구 도감에 기록. 역대 "최초" 발견일 때만 true가 돌아온다.
            bool firstEver = MetaProgress.DiscoverRecipe(recipeId);
            if (firstEver)
            {
                // v2.1: 플레이버 텍스트가 있으면 스토리 연출로 크게 표시 (첫 발견의 순간)
                RecipeData recipe = RecipeDatabase.Get(recipeId);
                string shownName = recipe != null ? recipe.displayName : recipeId;
                string flavor = recipe != null ? recipe.flavor : "";

                if (!string.IsNullOrEmpty(flavor))
                    StoryTexts.ShowRecipeFlavor(shownName, flavor, MetaProgress.DiscoveredCount);
                else
                    UIManager.Instance?.ShowStatChange("[도감] 신규 요리 등록: " + shownName
                        + " (" + MetaProgress.DiscoveredCount + "종)");
            }
        }
        if (OnChanged != null) OnChanged();
    }

    public bool TryConsume(string recipeId, int n)
    {
        if (Get(recipeId) < n) return false;
        stock[recipeId] -= n;
        if (OnChanged != null) OnChanged();
        return true;
    }

    /// <summary>
    /// 발견 여부. v2: 과거 런에서 만들어본 요리도 true.
    /// (도감 UI에서 "아는 요리"로 표시되어 다회차 수집 욕구를 만든다)
    /// </summary>
    public bool IsDiscovered(string recipeId)
    {
        return discovered.Contains(recipeId) || MetaProgress.IsRecipeDiscovered(recipeId);
    }

    /// <summary>이번 런에서 발견한 요리 수.</summary>
    public int DiscoveredCount { get { return discovered.Count; } }

    /// <summary>역대 통산 발견한 요리 수 (영구 도감).</summary>
    public int TotalDiscoveredCount { get { return MetaProgress.DiscoveredCount; } }

    // 보유 중인 요리 목록 (UI 순회용)
    public IEnumerable<KeyValuePair<string, int>> AllStock
    {
        get { return stock; }
    }
}
