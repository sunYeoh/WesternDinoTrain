using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [MetaProgress.cs] v1 (신규 파일)
/// 런이 끝나도 사라지지 않는 "메타 진행" 저장소.
///
/// - PlayerPrefs 기반 static 클래스라서 씬 배치, 오브젝트 연결이 전혀 필요 없다.
///   파일을 Assets/Prototype 폴더에 넣기만 하면 어디서든 MetaProgress.Fame 처럼 호출 가능.
/// - 저장 위치는 유니티가 알아서 관리한다 (Windows는 레지스트리).
///
/// 저장 항목:
///  1) 명성(Fame)        : 런을 반복할수록 쌓이는 영구 점수. 웨이브 클리어마다 즉시 적립.
///                         나중에 명성으로 언락(새 기차, 시작 보너스 등)을 여는 화폐가 된다.
///  2) 총 런 횟수        : 몇 번 도전했는지 (스토리 회차 조건에도 사용 예정)
///  3) 최고 도달 웨이브   : 개인 기록
///  4) 누적 클리어 웨이브 : 통계용
///  5) 도감(발견 레시피)  : 한 번 만든 요리는 다음 런에서도 "발견됨" 상태 유지
///
/// 명성 적립 규칙 (v1):
///  - 웨이브 클리어: 10 + 웨이브 번호 (뒤로 갈수록 더 준다)
///  - 승리(엔딩): +300 보너스
///  - 즉시 저장 방식이라 중간에 게임을 꺼도 그때까지 번 명성은 남는다.
///
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public static class MetaProgress
{
    // PlayerPrefs 키 접두어 (다른 프로젝트/에셋과 키 충돌 방지)
    private const string PREFIX = "WDT_";

    // 이번 런에서 얻은 명성 (화면 표시용. 총합은 어차피 저장돼 있으므로 저장 안 함)
    public static int RunFame { get; private set; }

    // ─────────────────────────────────────────────
    // 읽기 프로퍼티 (어디서든 바로 사용 가능)
    // ─────────────────────────────────────────────
    public static int Fame { get { return PlayerPrefs.GetInt(PREFIX + "Fame", 0); } }
    public static int RunsPlayed { get { return PlayerPrefs.GetInt(PREFIX + "RunsPlayed", 0); } }
    public static int BestWave { get { return PlayerPrefs.GetInt(PREFIX + "BestWave", 0); } }
    public static int TotalWavesCleared { get { return PlayerPrefs.GetInt(PREFIX + "TotalWaves", 0); } }

    // ─────────────────────────────────────────────
    // 런 흐름 훅 (GameManager가 호출)
    // ─────────────────────────────────────────────

    // ─────────────────────────────────────────────
    // 세이브 버전 (기술감사 지적 - 지금 안 넣으면 나중에 구분 불가)
    // 키 구조를 바꾸는 업데이트를 하면 CurrentSaveVersion을 올리고
    // EnsureSaveVersion 안에 마이그레이션 코드를 추가한다
    // ─────────────────────────────────────────────
    private const int CurrentSaveVersion = 1;

    public static void EnsureSaveVersion()
    {
        int saved = PlayerPrefs.GetInt(PREFIX + "SaveVersion", 0);
        if (saved == 0)
        {
            // 신규 or 버전 도입 이전 세이브 -> 현재 버전으로 표기
            PlayerPrefs.SetInt(PREFIX + "SaveVersion", CurrentSaveVersion);
            PlayerPrefs.Save();
        }
        else if (saved < CurrentSaveVersion)
        {
            // (여기에 버전별 마이그레이션 추가)
            PlayerPrefs.SetInt(PREFIX + "SaveVersion", CurrentSaveVersion);
            PlayerPrefs.Save();
            Debug.Log("[MetaProgress] 세이브 마이그레이션: v" + saved + " -> v" + CurrentSaveVersion);
        }
    }

    /// <summary>새 런 시작 시 1회 호출. 런 카운트 증가 + 이번 런 명성 초기화.</summary>
    public static void BeginRun()
    {
        EnsureSaveVersion();
        RunFame = 0;
        PlayerPrefs.SetInt(PREFIX + "RunsPlayed", RunsPlayed + 1);
        PlayerPrefs.Save();
        Debug.Log("[MetaProgress] " + RunsPlayed + "번째 런 시작 | 누적 명성 " + Fame
            + " | 최고 기록 " + BestWave + "웨이브");
    }

    /// <summary>웨이브 클리어 시 호출. 명성 적립 + 기록 갱신 + 즉시 저장.</summary>
    public static void OnWaveCleared(int wave)
    {
        int gain = 10 + wave;
        RunFame += gain;
        PlayerPrefs.SetInt(PREFIX + "Fame", Fame + gain);
        PlayerPrefs.SetInt(PREFIX + "TotalWaves", TotalWavesCleared + 1);
        if (wave > BestWave)
            PlayerPrefs.SetInt(PREFIX + "BestWave", wave);
        PlayerPrefs.Save();
    }

    /// <summary>보너스 명성 (승리 엔딩, 특별 업적 등).</summary>
    public static void AddFame(int amount)
    {
        if (amount <= 0) return;
        RunFame += amount;
        PlayerPrefs.SetInt(PREFIX + "Fame", Fame + amount);
        PlayerPrefs.Save();
    }

    /// <summary>게임오버/승리 화면에 띄울 요약 문자열.</summary>
    public static string RunSummary()
    {
        return "이번 런 명성 +" + RunFame
            + "  |  보유 명성 " + Fame
            + "  |  최고 기록 " + BestWave + "웨이브";
    }

    // ─────────────────────────────────────────────
    // 명성 상점 (영구 업그레이드)
    // 명성은 "쌓이는 점수"이자 "쓰는 화폐"다 (하데스의 어둠 결정 방식).
    // 업그레이드 레벨은 PlayerPrefs에 저장되어 모든 런에 적용된다.
    // ─────────────────────────────────────────────

    /// <summary>업그레이드 현재 레벨 (0 = 미구매).</summary>
    public static int UpgradeLevel(string upgradeId)
    {
        return PlayerPrefs.GetInt(PREFIX + "Up_" + upgradeId, 0);
    }

    /// <summary>
    /// 명성을 소모해 업그레이드 1레벨 구매.
    /// 최대 레벨이거나 명성이 부족하면 false.
    /// </summary>
    public static bool TryBuyUpgrade(string upgradeId, int cost, int maxLevel)
    {
        int level = UpgradeLevel(upgradeId);
        if (level >= maxLevel) return false;
        if (Fame < cost) return false;

        PlayerPrefs.SetInt(PREFIX + "Fame", Fame - cost);
        PlayerPrefs.SetInt(PREFIX + "Up_" + upgradeId, level + 1);
        PlayerPrefs.Save();
        Debug.Log("[MetaProgress] 업그레이드 구매: " + upgradeId + " Lv." + (level + 1)
            + " (-" + cost + " 명성, 잔여 " + Fame + ")");
        return true;
    }

    // ── 보너스 읽기 헬퍼 (게임 코드는 이것만 읽으면 된다) ──

    /// <summary>시작 골드 보너스 (레벨당 +100)</summary>
    public static int StartGoldBonus { get { return UpgradeLevel("gold") * 100; } }

    /// <summary>기차 최대 HP 보너스 (레벨당 +50)</summary>
    public static int TrainHPBonus { get { return UpgradeLevel("hp") * 50; } }

    /// <summary>시작 보급품 요리 추가 개수 (레벨당 +1)</summary>
    public static int StarterFoodBonus { get { return UpgradeLevel("food"); } }

    /// <summary>시작 시 랜덤 재료 추가 개수 (레벨당 +2)</summary>
    public static int StartMaterialBonus { get { return UpgradeLevel("mat") * 2; } }

    /// <summary>조리 판정 존 확대 배율 가산 (레벨당 +4%)</summary>
    public static float CookJudgeBonus { get { return UpgradeLevel("judge") * 0.04f; } }

    // ─────────────────────────────────────────────
    // 도감 영구화 (발견한 레시피 목록)
    // FoodStock/RecipeDatabase 쪽에서 발견 시 DiscoverRecipe()를 호출해주면
    // 다음 런에서도 IsRecipeDiscovered()가 true를 반환한다.
    // ─────────────────────────────────────────────

    // 매번 문자열 파싱을 피하기 위한 메모리 캐시
    private static HashSet<string> discoveredCache;

    private static void LoadDiscoveredCache()
    {
        if (discoveredCache != null) return;
        discoveredCache = new HashSet<string>();

        string raw = PlayerPrefs.GetString(PREFIX + "Recipes", "");
        if (string.IsNullOrEmpty(raw)) return;

        // 구분자 '|' 로 저장된 레시피 id 목록 복원
        string[] parts = raw.Split('|');
        for (int i = 0; i < parts.Length; i++)
        {
            if (!string.IsNullOrEmpty(parts[i]))
                discoveredCache.Add(parts[i]);
        }
    }

    /// <summary>이 레시피를 과거 런에서 한 번이라도 발견(제작)했는가?</summary>
    public static bool IsRecipeDiscovered(string recipeId)
    {
        LoadDiscoveredCache();
        return discoveredCache.Contains(recipeId);
    }

    /// <summary>레시피 발견 기록. 처음 발견이면 저장하고 true 반환 (도감 신규 등록 연출용).</summary>
    public static bool DiscoverRecipe(string recipeId)
    {
        if (string.IsNullOrEmpty(recipeId)) return false;
        LoadDiscoveredCache();
        if (discoveredCache.Contains(recipeId)) return false;

        discoveredCache.Add(recipeId);

        // HashSet -> "id1|id2|id3" 형태로 직렬화하여 저장
        string raw = "";
        foreach (string id in discoveredCache)
        {
            if (raw.Length > 0) raw += "|";
            raw += id;
        }
        PlayerPrefs.SetString(PREFIX + "Recipes", raw);
        PlayerPrefs.Save();

        Debug.Log("[MetaProgress] 도감 신규 등록: " + recipeId + " (총 " + discoveredCache.Count + "종)");
        return true;
    }

    /// <summary>지금까지 발견한 레시피 총 수.</summary>
    public static int DiscoveredCount
    {
        get { LoadDiscoveredCache(); return discoveredCache.Count; }
    }

    // ─────────────────────────────────────────────
    // C-2: 엔딩 기록
    // ─────────────────────────────────────────────

    /// <summary>진엔딩(엔딩 B: 마지막 식사)을 달성했는가 (영구 기록)</summary>
    public static bool EndingBCleared
    {
        get { return PlayerPrefs.GetInt(PREFIX + "EndingB", 0) == 1; }
    }

    /// <summary>엔딩 B 달성 기록 + 보너스 명성 (최초 1회만 보너스)</summary>
    public static void RecordEndingB()
    {
        bool first = !EndingBCleared;
        PlayerPrefs.SetInt(PREFIX + "EndingB", 1);
        PlayerPrefs.Save();

        if (first)
        {
            AddFame(GameBalance.EndingBFame);
            Debug.Log("[MetaProgress] 진엔딩 달성! 보너스 명성 +" + GameBalance.EndingBFame);
        }
    }

    // ─────────────────────────────────────────────
    // 요리 숙련 (P1+: 단골 메뉴의 영구화 - 사용자 결정 2026-08-24)
    // 레시피별 "평생" 조리 횟수. 죽어도 리셋되지 않는다 - 같은 셰프가 계속 굽고 있으니까.
    // 마일스톤/보상 수치는 GameBalance Mastery* 참조.
    // 저장: 단일 키 "WDT_CookCounts" = "레시피id:횟수;..." (42종이라 가볍다)
    // ─────────────────────────────────────────────

    private static Dictionary<string, int> cookCountCache;
    private static HashSet<string> masterFamedCache;   // 100회 명성을 이미 받은 레시피

    private static void LoadCookCounts()
    {
        if (cookCountCache != null) return;
        cookCountCache = new Dictionary<string, int>();
        string raw = PlayerPrefs.GetString(PREFIX + "CookCounts", "");
        if (!string.IsNullOrEmpty(raw))
        {
            string[] pairs = raw.Split(';');
            for (int i = 0; i < pairs.Length; i++)
            {
                int sep = pairs[i].LastIndexOf(':');
                if (sep <= 0) continue;
                string id = pairs[i].Substring(0, sep);
                int n;
                if (int.TryParse(pairs[i].Substring(sep + 1), out n))
                    cookCountCache[id] = n;
            }
        }
    }

    private static void SaveCookCounts()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (KeyValuePair<string, int> kv in cookCountCache)
        {
            if (sb.Length > 0) sb.Append(';');
            sb.Append(kv.Key).Append(':').Append(kv.Value);
        }
        PlayerPrefs.SetString(PREFIX + "CookCounts", sb.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>이 레시피의 평생 조리 횟수</summary>
    public static int GetCookCount(string recipeId)
    {
        LoadCookCounts();
        int n;
        return cookCountCache.TryGetValue(recipeId, out n) ? n : 0;
    }

    /// <summary>조리 1회 기록 (FoodStock.CountCook이 호출). 갱신된 횟수 반환</summary>
    public static int AddCookCount(string recipeId)
    {
        LoadCookCounts();
        int n = GetCookCount(recipeId) + 1;
        cookCountCache[recipeId] = n;
        SaveCookCounts();
        return n;
    }

    /// <summary>이 레시피의 현재 숙련 티어 (-1 = 없음)</summary>
    public static int GetMasteryTier(string recipeId)
    {
        return GameBalance.MasteryTier(GetCookCount(recipeId));
    }

    /// <summary>숙련 공격력 보너스 (TurretAttackExecutor가 매 타격 참조)</summary>
    public static float GetMasteryAtk(string recipeId)
    {
        int t = GetMasteryTier(recipeId);
        return t >= 0 ? GameBalance.MasteryAtkBonus[t] : 0f;
    }

    /// <summary>숙련 판정 존 보너스 (CookingMinigame이 참조)</summary>
    public static float GetMasteryJudge(string recipeId)
    {
        int t = GetMasteryTier(recipeId);
        return t >= 0 ? GameBalance.MasteryJudgeBonus[t] : 0f;
    }

    // ─────────────────────────────────────────────
    // 스피노 베팅 기록 (Phase 2-1) - 만남 횟수/승패 영구 저장
    // 고회차 대사 조건("네 눈빛이 점점 나를 닮아간다" 등)에 재사용된다.
    // ─────────────────────────────────────────────

    /// <summary>스피노와 만난 총 횟수 (첫만남 대사 분기용)</summary>
    public static int SpinoMeetings { get { return PlayerPrefs.GetInt(PREFIX + "SpinoMet", 0); } }

    public static int BetWins { get { return PlayerPrefs.GetInt(PREFIX + "BetWins", 0); } }
    public static int BetLosses { get { return PlayerPrefs.GetInt(PREFIX + "BetLosses", 0); } }

    /// <summary>스피노 등장 1회 기록</summary>
    public static void AddSpinoMeeting()
    {
        PlayerPrefs.SetInt(PREFIX + "SpinoMet", SpinoMeetings + 1);
        PlayerPrefs.Save();
    }

    // ─────────────────────────────────────────────
    // 등짐장수 안킬로 기록 (Phase 2-3) - 첫만남 대사 분기용
    // ─────────────────────────────────────────────

    /// <summary>안킬로 행상인과 만난 총 횟수</summary>
    public static int AnkyMeetings { get { return PlayerPrefs.GetInt(PREFIX + "AnkyMet", 0); } }

    /// <summary>안킬로 등장 1회 기록</summary>
    public static void AddAnkyMeeting()
    {
        PlayerPrefs.SetInt(PREFIX + "AnkyMet", AnkyMeetings + 1);
        PlayerPrefs.Save();
    }

    /// <summary>베팅 결과 기록</summary>
    public static void RecordBetResult(bool win)
    {
        if (win) PlayerPrefs.SetInt(PREFIX + "BetWins", BetWins + 1);
        else PlayerPrefs.SetInt(PREFIX + "BetLosses", BetLosses + 1);
        PlayerPrefs.Save();
    }

    /// <summary>100회 마스터 명성을 최초 1회만 지급 (지급했으면 true)</summary>
    public static bool TryGrantMasterFame(string recipeId)
    {
        if (masterFamedCache == null)
        {
            masterFamedCache = new HashSet<string>();
            string raw = PlayerPrefs.GetString(PREFIX + "MasterFamed", "");
            if (!string.IsNullOrEmpty(raw))
                foreach (string id in raw.Split(';'))
                    if (!string.IsNullOrEmpty(id)) masterFamedCache.Add(id);
        }

        if (masterFamedCache.Contains(recipeId)) return false;

        masterFamedCache.Add(recipeId);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (string id in masterFamedCache)
        {
            if (sb.Length > 0) sb.Append(';');
            sb.Append(id);
        }
        PlayerPrefs.SetString(PREFIX + "MasterFamed", sb.ToString());
        AddFame(GameBalance.MasteryFame);
        return true;
    }

    // ─────────────────────────────────────────────
    // 선대의 일지 수집 (분기 선로 '폐역' 보상 - 12장, 영구 저장)
    // 순서 무관 수집이지만 12장(최종)은 나머지 11장을 다 모아야 나온다.
    // ─────────────────────────────────────────────

    private static HashSet<int> journalCache;

    private static void LoadJournalCache()
    {
        if (journalCache != null) return;
        journalCache = new HashSet<int>();

        string raw = PlayerPrefs.GetString(PREFIX + "Journals", "");
        if (string.IsNullOrEmpty(raw)) return;

        string[] parts = raw.Split('|');
        for (int i = 0; i < parts.Length; i++)
        {
            int n;
            if (int.TryParse(parts[i], out n)) journalCache.Add(n);
        }
    }

    public static bool IsJournalCollected(int number)
    {
        LoadJournalCache();
        return journalCache.Contains(number);
    }

    public static int CollectedJournalCount
    {
        get { LoadJournalCache(); return journalCache.Count; }
    }

    /// <summary>
    /// 아직 안 모은 일지 번호를 무작위로 하나 고른다.
    /// 1~11 중 무작위, 11장을 다 모았으면 12(최종), 전부 모았으면 -1.
    /// </summary>
    public static int PickUncollectedJournal()
    {
        LoadJournalCache();

        List<int> candidates = new List<int>();
        for (int n = 1; n <= 11; n++)
            if (!journalCache.Contains(n)) candidates.Add(n);

        if (candidates.Count > 0)
            return candidates[Random.Range(0, candidates.Count)];

        if (!journalCache.Contains(12)) return 12;
        return -1;
    }

    /// <summary>일지 수집 기록 (즉시 저장).</summary>
    public static void CollectJournal(int number)
    {
        LoadJournalCache();
        if (journalCache.Contains(number)) return;

        journalCache.Add(number);

        string raw = "";
        foreach (int n in journalCache)
        {
            if (raw.Length > 0) raw += "|";
            raw += n;
        }
        PlayerPrefs.SetString(PREFIX + "Journals", raw);
        PlayerPrefs.Save();
        Debug.Log("[MetaProgress] 선대의 일지 #" + number + " 수집 (" + journalCache.Count + "/12)");
    }

    // ─────────────────────────────────────────────
    // 전체 초기화 (테스트용)
    // 에디터에서 메타 저장을 지우고 싶을 때만 호출한다.
    // ─────────────────────────────────────────────
    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(PREFIX + "Fame");
        PlayerPrefs.DeleteKey(PREFIX + "RunsPlayed");
        PlayerPrefs.DeleteKey(PREFIX + "BestWave");
        PlayerPrefs.DeleteKey(PREFIX + "TotalWaves");
        PlayerPrefs.DeleteKey(PREFIX + "Recipes");
        PlayerPrefs.DeleteKey(PREFIX + "Journals");
        PlayerPrefs.DeleteKey(PREFIX + "EndingB");
        PlayerPrefs.DeleteKey(PREFIX + "CookCounts");
        PlayerPrefs.DeleteKey(PREFIX + "MasterFamed");
        PlayerPrefs.DeleteKey(PREFIX + "SpinoMet");
        PlayerPrefs.DeleteKey(PREFIX + "BetWins");
        PlayerPrefs.DeleteKey(PREFIX + "BetLosses");
        PlayerPrefs.DeleteKey(PREFIX + "AnkyMet");
        cookCountCache = null;
        masterFamedCache = null;

        // 명성 상점 업그레이드도 초기화
        string[] upgradeIds = { "gold", "hp", "food", "mat", "judge" };
        for (int i = 0; i < upgradeIds.Length; i++)
            PlayerPrefs.DeleteKey(PREFIX + "Up_" + upgradeIds[i]);

        PlayerPrefs.Save();
        discoveredCache = null;
        journalCache = null;
        RunFame = 0;
        Debug.Log("[MetaProgress] 메타 저장 전체 초기화 완료");
    }
}
