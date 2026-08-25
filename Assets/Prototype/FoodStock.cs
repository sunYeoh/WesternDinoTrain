using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [FoodStock.cs] v2
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

    /// <summary>성공 조리 1회 기록 (CookingBridge.FinishCook이 호출)</summary>
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

        string msg = "[숙련] " + shownName + " - \"" + title + "\" (누적 " + c + "회) 공격력 +"
            + Mathf.RoundToInt(GameBalance.MasteryAtkBonus[tier] * 100f) + "%";
        if (GameBalance.MasteryJudgeBonus[tier] > 0f)
            msg += " / 판정 +" + Mathf.RoundToInt(GameBalance.MasteryJudgeBonus[tier] * 100f) + "%";
        if (tier == GameBalance.MasteryStartLevelTier)
            msg += " / 배치 시 Lv+1";
        if (tier >= GameBalance.MasteryPerfectTier)
            msg += " / PERFECT 수량 +1";

        UIManager.Instance?.ShowStatChange(msg);
        SoundManager.Play("sfx_judge_perfect");
        Debug.Log("[FoodStock] 숙련 마일스톤: " + msg);

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

    public void Add(string recipeId, int n)
    {
        stock[recipeId] = Get(recipeId) + n;

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
