using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TutorialHint.cs] v1.13 (v9.38 2026-10-29 유저 "와바바박 - 띄엄띄엄": 급하지 않은 조각끼리 GameBalance.HintGapSec 간격 (급한 것 = 마비 · 과열 · 사고 · 보스 · 바위 · 정차 · 증강 · 갈림길 · 유물 - 줄 앞으로, 바로) / 띠 왼쪽 그림 (HintIconOn) / 새 조각 "속성 재료" (팬 · 솥) / 조리 조각은 조리 독 단추를 짚는다 (GameHUD.PointDock) / 조리 조각 글에 아래 단추) / v1.12 (v9.27 2026-10-20 튜토리얼 개편: "조리 시작" 칩을 조리법 셋(굽기 타이밍 / 볶기 키 순서 / 끓이기 [Space] 꾹)으로 / 새 빌드를 처음 켜면 견습·첫 등장 카드·프롤로그 기록을 비운다 CheckNewBuild (GameBalance.FreshRecordsOnNewBuild - PlayerPrefs 는 PC 레지스트리에 남아 시연 PC 가 지난 시연의 견습 완료를 들고 있었다, 유저 10-20) / 빌드에서도 F4 리셋 (GameBalance.ResetKeyInBuild) / 리셋 본문을 ResetRecords 로 - F4 와 새 빌드가 같이 쓴다) / v1.11 (v9.22.2 2026-10-14 유저 "튜토리얼 말투가 번역체·AI 같고 딱딱하다": 안내 글 말투를 반말 해체로 - 명령형 ~라·설명 ~다 끝맺음, 문장 속 기호, 어려운 말(투입·재가동·곁·유인)를 걷어냈다. 규칙 = 문구원칙 10-14 저녁 "말투" 절) / v1.10 (v9.22.1 2026-10-14 유저 "튜토리얼에 드래그가 반영 안 됐다": first_battle·merge_ready 칩을 끌어다 놓기 기준으로, first_stun 에 "방치하면 파손" / 새 칩 fuse_ready(기본 요리 카드 둘 = 진화 조리 - 합칠 수 있는 짝이 재고에 생기면) · swap_ready(빈 칸이 없는데 못 넣은 요리가 있으면 = 교체)) / v1.9 (v9.22 2026-10-14 조작 팩 2-2: Tab 키가 없어져 "재료가 모였다" 칩에서 [Tab] 주방 전체 메뉴를 뺐다 - 조리대 [E] 만) / v1.8 (v9.18 2026-10-06: 배너 폭 660 -> 620 - 씬 HUD 가 UI 배율을 따르면서 커진 좌상단 판과 8 겹치던 것) / v1.7 (v9.13 2026-09-23: first_route 트리거 = BranchRouteUI.ChoicePending - 선로 선택이 카드 창이 아니게 됨) / v1.6 (v9.12 2026-09-22: F4 가 구간 기록도 지운다 / 리롤 복원) / v1.5 (v9.11.1 2026-09-22 문구: 복구법 상태별·공명 예외·이번 운행·다시 뽑기·무방비) / v1.4 (v9.9.2 2026-09-16: 승격 5 - first_town / first_augment / first_route / first_item / first_overheat 는 배너 대신 브리핑 카드(BriefingTexts.Promoted), F4 가 첫 등장 카드 기록도 지운다) / v1.3 (v9.9 2026-09-16: 견습 운행·브리핑 카드 중엔 배너 쉼, F4 가 견습 완료 기록도 지움, 배너 캔버스 DontDestroyOnLoad) / v1.2 (v9.8.1: F4 리셋은 GameBalance.CheatsAllowed 일 때만) / v1.1 (교수 피드백 A13: 표시 5초 + 아무 키 닫기 + 조리 중 대기) - 컨텍스트 트리거 튜토리얼 (설계: 튜토리얼_온보딩_설계_2026-08-18)
///
/// 몰아서 가르치지 않는다. 각 기믹을 "처음 마주치는 순간" 1회만 배너로 안내한다.
/// - 영구 기록: PlayerPrefs "WDT_Tut_(id)" - 2회차부터 반복 없음 (다회차 마찰 방지)
/// - 다른 파일 수정 0: 전부 게임 상태 폴링(0.3초)으로 감지. 전부 보고 나면 폴링 중단
/// - [H] 도움말 아카이브: 지나간 안내 재열람 (아직 안 만난 상황은 잠김 표시)
/// - 신규 기믹 추가 시: 아래 HINTS 배열에 1줄 + CheckTrigger에 조건 1줄
///
/// 사용법: 없음! 파일만 넣으면 자동 생성된다. (GameBalance.TutorialEnabled로 끄기)
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class TutorialHint : MonoBehaviour
{
    private static TutorialHint instance;

    // ─────────────────────────────────────────────
    // 힌트 정의 (아카이브 표시 순서이기도 하다)
    // ─────────────────────────────────────────────
    private struct HintDef
    {
        public string id;
        public string title;
        public string body;
        public HintDef(string id, string title, string body)
        {
            this.id = id; this.title = title; this.body = body;
        }
    }

    private static readonly HintDef[] HINTS =
    {
        new HintDef("first_battle", "황야로 출발!",
            "[WASD]로 달리고 [Shift]로 대시. 요리 카드는 끌어서 포탑 이름표에 놔"),
        new HintDef("cook_ready", "재료가 모였다",
            "재료 두 개면 요리 하나. 아래 [그릴 · 굽기] 를 누르거나 조리대 앞에서 [E]"),
        new HintDef("attr_mats", "속성 재료",
            "속성 재료는 팬이나 솥에서. 아래 [팬 · 볶기] [솥 · 끓이기]"),
        new HintDef("cook_start", "조리 시작",
            "굽기는 초록 구간에서 [Space], 볶기는 뜨는 키 순서대로, 끓이기는 [Space] 꾹으로 바늘을 구간에. [ESC]로 그만둬도 재료는 돌려받아"),
        new HintDef("merge_ready", "같은 요리로 키우기",
            "카드를 같은 요리가 든 포탑에 또 놓으면 레벨이 올라. 같은 포탑 둘은 하나씩 좌클릭하면 합체"),
        new HintDef("fuse_ready", "전설로 가는 길",
            "기본 요리 카드를 끌면 합칠 짝에 분홍 테가 떠. 거기 놓으면 진화 조리로 전설 요리가 나와"),
        new HintDef("swap_ready", "빈 칸이 없을 땐",
            "다른 요리 칸에 놓으면 교체야. 원래 포탑은 폐기되고 넣었던 요리 절반만큼 재료가 돌아와"),
        new HintDef("resonance_near", "공명까지 하나 남았어",
            "같은 속성 포탑이 둘이야. 셋이면 공명, 그 속성 피해 +20% (방어 계열은 기차가 받는 피해 -10%)"),
        new HintDef("first_stun", "포탑 마비!",
            "포탑 옆으로 달려가. 감전은 [E] 한 번, 빙결은 [E] 여러 번. 오래 두면 망가져"),
        new HintDef("first_overheat", "포탑 과열!",
            "[E]를 누른 채 마우스를 흔들어서 식혀. 손을 떼면 열이 도로 올라"),
        new HintDef("first_event", "주방 사고!",
            "화살표를 따라 달려가. 현장에 가야 처리할 수 있어"),
        new HintDef("first_crate", "갑판의 전리품",
            "갑판에 떨어진 상자는 밟아서 주워"),
        new HintDef("first_item", "유물을 얻었다",
            "[V]로 소지품을 봐. 유물은 종류마다 하나씩, 효과는 이번 운행 동안만"),
        new HintDef("first_rock", "광맥 바위 발견",
            "기관실 작살포 옆에서 [E]로 낚아. 가끔 굶주린 놈들이 같이 딸려 와"),
        new HintDef("lever_hint", "기관차 레버",
            "기관실 레버 옆에서 [E]를 꾹 누르면 전속 주행. 위험한 만큼 벌이가 좋아"),
        new HintDef("first_town", "간이역 정차",
            "[G]로 정비소를 열어. 수리, 연마, 재료 시장이 있어. 도구가 상하면 조리가 어려워져"),
        new HintDef("first_augment", "증강 고르기",
            "[1~5] 숫자키로 골라. [0]은 건너뛰기(명성 조금), [9]는 리롤(골드). 효과는 이번 운행 동안만"),
        new HintDef("first_route", "갈림길",
            "두상 앞에서 선로가 갈라져. 왼쪽 카드로 길을 골라. 폐역엔 선대의 일지가 남아 있어"),
        new HintDef("boss_incoming", "보스가 온다!",
            "보스가 무방비가 되면 [F]로 독샘 요리를 던져!"),
    };

    private const string PREF_PREFIX = "WDT_Tut_";   // 메타(WDT_)와 같은 계열, 독립 키

    // ─────────────────────────────────────────────
    // 내부 상태
    // ─────────────────────────────────────────────
    private readonly List<int> pending = new List<int>();  // 표시 대기 큐 (HINTS 인덱스)
    private float showUntil = 0f;      // 현재 배너 표시 종료 시각
    private float nextPollTime = 0f;
    private bool allSeen = false;

    // 배너 UI
    private Canvas bannerCanvas;
    private RectTransform bannerRoot;
    private Text bannerTitle;
    private Text bannerBody;

    // 아카이브 UI
    private Canvas archiveCanvas;
    private RectTransform archiveRoot;
    private bool archiveOpen = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("TutorialHint");
        DontDestroyOnLoad(go);
        go.AddComponent<TutorialHint>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedHint;   // v1.13
        CheckNewBuild();   // v1.12: 새 빌드 첫 실행이면 기록 리셋 (allSeen 을 세기 전에)
        BuildBanner();
        RefreshAllSeen();
    }

    // ─────────────────────────────────────────────
    // 기록 (영구)
    // ─────────────────────────────────────────────
    private static bool Seen(string id)
    {
        return PlayerPrefs.GetInt(PREF_PREFIX + id, 0) == 1;
    }

    private static void MarkSeen(string id)
    {
        PlayerPrefs.SetInt(PREF_PREFIX + id, 1);
        PlayerPrefs.Save();
    }

    // ── v1.12: 기록 리셋 - F4 와 새 빌드 첫 실행이 같이 쓴다 ──
    private const string BUILD_KEY = "WDT_Build";   // 마지막으로 켠 빌드 (GameBalance.BuildTag)
    private static bool buildChecked = false;

    /// <summary>
    /// 새 빌드(BuildTag 가 저장된 것과 다름)의 첫 실행이면 견습·첫 등장 카드·프롤로그 기록을 비운다 (GameBalance.FreshRecordsOnNewBuild).
    /// PlayerPrefs 는 빌드 폴더가 아니라 PC(레지스트리)에 남아, 새 빌드를 가져가도 지난 시연의 "견습 완료" 를 읽어 로비 [출발] 이 견습을 안 거쳤다.
    /// 빌드에서만 (에디터는 기록을 두고 F4 로). 힌트와 디렉터의 Awake 가 둘 다 부른다 - 먼저 온 쪽이 하고 한 번만 (디렉터는 기록 옮기기보다 먼저)
    /// </summary>
    public static void CheckNewBuild()
    {
        if (buildChecked) return;
        buildChecked = true;
        string stored = PlayerPrefs.GetString(BUILD_KEY, "");
        if (stored == GameBalance.BuildTag) return;
        if (GameBalance.FreshRecordsOnNewBuild && !Application.isEditor)   // 에디터는 그대로 (팩마다 기록이 날아가면 테스트가 번거롭다 - 에디터는 F4)
        {
            ResetRecords();
            Debug.Log("[TutorialHint] 새 빌드 첫 실행 (" + (stored == "" ? "기록 없음" : stored) + " -> " + GameBalance.BuildTag + ") - 견습·첫 등장 카드·프롤로그 기록 리셋");
        }
        PlayerPrefs.SetString(BUILD_KEY, GameBalance.BuildTag);
        PlayerPrefs.Save();
    }

    /// <summary>튜토리얼 쪽 기록 전부 삭제: 힌트 칩 · 프롤로그 · 견습 완료·구간·인라인 연습 · 첫 등장 카드 34 + 승격 5 (명성·요리책 같은 메타 기록은 그대로)</summary>
    public static void ResetRecords()
    {
        for (int i = 0; i < HINTS.Length; i++) PlayerPrefs.DeleteKey(PREF_PREFIX + HINTS[i].id);
        PlayerPrefs.DeleteKey("WDT_PrologueSeen");          // 프롤로그(웨이브 1 스피노 안내)
        PlayerPrefs.DeleteKey(TutorialDirector.DONE_KEY);   // 견습 운행 완료 (로비 [출발] 이 다시 견습부터)
        TutorialDirector.ResetSegments();                   // 구간·인라인 연습 기록 - 훈련장 표시가 처음으로
        BriefingUI.ResetSeen(BriefingTexts.AllOnceIds());   // 정식 운행 첫 등장 카드 34장 + 승격 5
        PlayerPrefs.Save();
        if (instance != null) instance.allSeen = false;
    }

    private void RefreshAllSeen()
    {
        allSeen = true;
        for (int i = 0; i < HINTS.Length; i++)
            if (!Seen(HINTS[i].id)) { allSeen = false; return; }
    }

    // ─────────────────────────────────────────────
    // 매 프레임: 폴링 + 큐 표시 + [H] 아카이브
    // ─────────────────────────────────────────────
    private void Update()
    {
        // [H] 도움말 아카이브 (튜토리얼 스위치와 무관하게 열람은 항상 가능)
        if (Input.GetKeyDown(KeyCode.H) && !PauseMenu.IsOpen)
            ToggleArchive();

        // F4: 튜토리얼+프롤로그 기록 전체 리셋 (재테스트·시연 직전). v1.12: 빌드에서도 (GameBalance.ResetKeyInBuild) - 치트 전체가 아니라 이 키만
        if (Input.GetKeyDown(KeyCode.F4) && (GameBalance.CheatsAllowed || GameBalance.ResetKeyInBuild))
        {
            ResetRecords();
            UIManager.Instance?.ShowStatChange("[기록 리셋] 견습 운행 · 첫 등장 카드 · 프롤로그 기록을 지웠다 - 처음 온 셰프가 됐다");
            Debug.Log("[TutorialHint] F4 - 기록 전체 리셋");
        }

        if (!GameBalance.TutorialEnabled) { HideBannerIfExpired(true); return; }

        // v1.3: 견습 운행 중(디렉터가 다 설명한다)과 브리핑 카드 위에서는 배너를 내리고 폴링도 쉰다
        //  - "본 것"으로 기록하지 않으므로 정식 런에서 그 힌트를 제때 다시 만난다
        if (TutorialDirector.Active || BriefingUI.IsOpen) { HideBannerIfExpired(true); return; }

        // 배너 수명 관리
        HideBannerIfExpired(false);

        // 표시 중이 아니고 대기 큐가 있으면 다음 힌트 표시
        // (오프닝 등 전체 화면 연출 중에는 기다렸다가 보여준다)
        // v1.1 (교수 피드백 A13): 조리 미니게임이 떠 있는 동안(판정 팝업 포함)은 기다린다 -
        // 성공을 설명하는 창이 성공 자체를 가리지 않게
        if (Time.unscaledTime >= showUntil && pending.Count > 0
            && !PauseMenu.IsOpen && !StoryTexts.IsBlocking && !CookingMinigame.IsActive)
        {
            // v1.13: 급한 것부터, 급하지 않은 것은 앞 조각에서 HintGapSec 지난 뒤 (한 번에 몰아서 안 나오게)
            int at = 0;
            for (int i = 0; i < pending.Count; i++) if (Urgent(HINTS[pending[i]].id)) { at = i; break; }
            int idx = pending[at];
            if (!Urgent(HINTS[idx].id) && GameBalance.HintGapSec > 0f && lastHintAt > 0f && Time.unscaledTime - lastHintAt < GameBalance.HintGapSec)
                goto Poll;
            pending.RemoveAt(at);
            // v1.13 (리뷰 M1): 본 것으로 적는 건 띄우는 지금 - 기다리는 동안 상황이 지나간 조각(재료 · 진화 · 교체)은 적지 않고 버린다 (다음에 다시 맞으면 또 들어온다)
            if (Seen(HINTS[idx].id) || (StateHint(HINTS[idx].id) && !CheckTrigger(HINTS[idx].id))) goto Poll;
            MarkSeen(HINTS[idx].id);
            RefreshAllSeen();
            lastHintAt = Time.unscaledTime;
            // v1.4: 승격 5 는 5초 배너 대신 시간 정지 카드 (증강 선택창 위에서도 열린다 - BriefingUI v1.1)
            if (GameBalance.FirstEncounterBriefings && IsPromoted(HINTS[idx].id))
                BriefingUI.ShowOnce("promo_" + HINTS[idx].id, BriefingTexts.Promoted(HINTS[idx].id));
            else
                ShowBanner(HINTS[idx]);
            PointFor(HINTS[idx].id);   // v1.13: 조리 조각은 아래 단추를 짚는다
        }

        Poll:
        // 상태 폴링 (전부 봤으면 중단 - 비용 0)
        if (allSeen || Time.unscaledTime < nextPollTime) return;
        nextPollTime = Time.unscaledTime + 0.3f;
        PollTriggers();
    }

    private float lastHintAt = 0f;   // v1.13: 마지막 조각을 띄운 때 (실시간)

    /// <summary>v1.13 (리뷰 M1): 상황이 지나가면 뜻이 없는 조각 - 띄우기 직전에 조건을 다시 본다</summary>
    private static bool StateHint(string id)
    {
        return id == "cook_ready" || id == "attr_mats" || id == "fuse_ready" || id == "swap_ready" || id == "merge_ready" || id == "resonance_near";
    }

    /// <summary>v1.13 (리뷰 M1): 씬이 바뀌면 기다리던 조각을 버린다 (판이 끝나 로비에서 "아래 [팬 · 볶기]" 가 뜨지 않게). 본 것으로 안 적었으니 다음 판에 다시 들어온다</summary>
    private void OnSceneLoadedHint(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        pending.Clear();
        lastHintAt = Time.unscaledTime;   // 판 시작에 급하지 않은 조각이 바로 뜨지 않게 (리뷰 L3)
    }

    /// <summary>v1.13: 견습이 방금 가르친 조각은 본 것으로 (정식 운행에서 또 안 뜬다)</summary>
    public static void MarkLearned(params string[] ids)
    {
        for (int i = 0; i < ids.Length; i++) PlayerPrefs.SetInt(PREF_PREFIX + ids[i], 1);
        PlayerPrefs.Save();
        if (instance != null) instance.RefreshAllSeen();
    }

    /// <summary>v1.13: 기다리면 때를 놓치는 조각 (그 상황이 지금 화면에 있다) - 간격 없이 바로, 줄 앞으로</summary>
    private static bool Urgent(string id)
    {
        return id == "first_stun" || id == "first_overheat" || id == "first_event" || id == "boss_incoming" || id == "first_rock"
            || id == "first_town" || id == "first_augment" || id == "first_route" || id == "first_item" || id == "attr_mats";   // 리뷰 H2: 속성 재료는 바로 (cook_start 는 조리가 끝난 뒤라 급하지 않다)
    }

    /// <summary>v1.13: 조각마다 그림 (없으면 null - 그림 없는 띠)</summary>
    private static string IconOf(string id)
    {
        switch (id)
        {
            case "first_battle": return "ui_npc_spino";
            case "cook_ready": return "ui_dish_meat_meat";
            case "attr_mats": return "ui_mat_elec";
            case "cook_start": return "ui_mat_fire";
            case "merge_ready": return "fx_levelup";
            case "fuse_ready": return "ui_dish_t2_phys_phys";
            case "swap_ready": return "ui_dish_elec_elec";
            case "first_stun": return "fx_status_stun";
            case "first_overheat": return "fx_status_burn";
            case "first_event": return "ui_item_extinguisher";   // 리뷰 L7: 과열(불 표시)과 다른 그림
            case "first_item": return "ui_item_knife";
            case "first_town": return "ui_npc_ankylo";
            case "boss_incoming": return "ui_dish_meat_poison";
            default: return null;
        }
    }

    /// <summary>v1.13: 조각이 짚을 UI (조리 독 단추) - GameHUD 가 금색 테 + ▼</summary>
    private static void PointFor(string id)
    {
        if (GameHUD.Instance == null) return;
        if (id == "cook_ready") GameHUD.Instance.PointDock(0, GameBalance.HintShowSec + 3f);
        else if (id == "attr_mats") GameHUD.Instance.PointDock(1, GameBalance.HintShowSec + 3f);
    }

    /// <summary>v1.4: 카드로 승격된 힌트 (계획 v2 §4 "승격 5")</summary>
    private static bool IsPromoted(string id)
    {
        return id == "first_town" || id == "first_augment" || id == "first_route" || id == "first_item" || id == "first_overheat";
    }

    private void PollTriggers()
    {
        for (int i = 0; i < HINTS.Length; i++)
        {
            string id = HINTS[i].id;
            if (Seen(id) || pending.Contains(i)) continue;
            if (!CheckTrigger(id)) continue;

            pending.Add(i);        // v1.13 (리뷰 M1): 큐에 넣을 땐 안 적는다 - 띄울 때 적는다 (중복은 pending.Contains 가 막는다)
        }
        RefreshAllSeen();
    }

    /// <summary>id별 발동 조건 - 전부 공개 상태만 읽는다 (다른 파일 무수정)</summary>
    private bool CheckTrigger(string id)
    {
        GameManager gm = GameManager.Instance;
        bool inBattle = gm != null && gm.currentState == GameManager.GameState.Battle;

        if (id == "first_battle") return inBattle;
        if (id == "first_town") return gm != null && gm.currentState == GameManager.GameState.Town;
        if (id == "lever_hint") return inBattle && gm.currentWave >= 2;
        if (id == "boss_incoming") return inBattle && gm != null && GameBalance.IsBossWave(gm.currentWave);

        if (id == "cook_start") return CookingMinigame.IsActive;
        if (id == "attr_mats")   // v1.13: 속성 재료가 둘 모였다 (요리 하나 만들 만큼) - 팬 · 솥 차례
        {
            MaterialInventory inv = MaterialInventory.Instance;
            if (inv == null || !inBattle) return false;
            return inv.Get(MaterialType.Armor) >= 2 || inv.Get(MaterialType.Elec) >= 2 || inv.Get(MaterialType.Fire) >= 2
                || inv.Get(MaterialType.Ice) >= 2 || inv.Get(MaterialType.Poison) >= 2;
        }
        if (id == "first_event") return KitchenEventManager.IsActive;
        if (id == "first_augment") return AugmentPickUI.IsOpen;
        if (id == "first_route") return BranchRouteUI.ChoicePending;   // v1.7: 갈림길 + 왼쪽 카드 (창이 아니다)
        if (id == "first_item") return ItemManager.OwnedCount > 0;

        if (id == "cook_ready")
        {
            MaterialInventory inv = MaterialInventory.Instance;
            if (inv == null) return false;
            int total = 0;
            total += inv.Get(MaterialType.Meat) + inv.Get(MaterialType.Armor)
                   + inv.Get(MaterialType.Elec) + inv.Get(MaterialType.Fire)
                   + inv.Get(MaterialType.Ice) + inv.Get(MaterialType.Poison);
            return total >= 2;
        }

        if (id == "first_stun" || id == "first_overheat")
        {
            TurretSlotManager mgr = TurretSlotManager.Instance;
            if (mgr == null) return false;
            for (int i = 0; i < mgr.slots.Length; i++)
            {
                TurretSlot s = mgr.slots[i];
                if (s == null || !s.IsStunned) continue;
                bool overheated = s.StunKind == "과열";
                if (id == "first_overheat" && overheated) return true;
                if (id == "first_stun" && !overheated) return true;
            }
            return false;
        }

        if (id == "merge_ready" || id == "resonance_near")
        {
            TurretSlotManager mgr = TurretSlotManager.Instance;
            if (mgr == null) return false;
            for (int a = 0; a < mgr.slots.Length; a++)
            {
                TurretSlot sa = mgr.slots[a];
                if (sa == null || sa.IsEmpty || sa.isLocked) continue;

                // 픽스 2차: 합체 안내를 앞당긴다 - 가동 포탑과 같은 요리가 "재고"에만
                // 있어도 발동 (시작 포탑 + 보급 요리 조합이면 첫 전투 몇 초 안에 배운다)
                if (id == "merge_ready" && FoodStock.Instance != null
                    && FoodStock.Instance.Get(sa.recipeId) >= 1) return true;

                for (int b = a + 1; b < mgr.slots.Length; b++)
                {
                    TurretSlot sb = mgr.slots[b];
                    if (sb == null || sb.IsEmpty || sb.isLocked) continue;
                    if (id == "merge_ready" && sa.recipeId == sb.recipeId) return true;
                    if (id == "resonance_near" && sa.Recipe != null && sb.Recipe != null
                        && sa.Recipe.tag == sb.Recipe.tag) return true;
                }
            }
            return false;
        }

        // v1.10: 재고에 합칠 수 있는 기본 요리 짝이 생기면 (GameHUD.FuseCheck 와 같은 규칙 - 조합 있음·선대의 기본기 아님). 포탑을 하나는 세운 뒤에 (투입 -> 진화 순서로 배운다)
        if (id == "fuse_ready")
        {
            if (!GameBalance.DragInsertOn || !GameBalance.DragFuseOn || FoodStock.Instance == null) return false;
            if (TurretSlotManager.Instance == null || !AnyTurretRunning(TurretSlotManager.Instance)) return false;
            List<string> have = new List<string>();
            foreach (RecipeData r in RecipeDatabase.All)
                if (r != null && r.tier == 1 && FoodStock.Instance.Get(r.recipeId) >= 1) have.Add(r.recipeId);
            RecipeData fusion; string why;
            for (int a = 0; a < have.Count; a++)
                for (int b = a + 1; b < have.Count; b++)
                    if (GameHUD.FuseCheck(have[a], have[b], out fusion, out why)) return true;
            return false;
        }

        // v1.10: 놓을 빈 칸이 없는데(바꿀 수 있는 포탑은 있고) 어느 포탑에도 안 든 요리 카드가 있으면 - 교체를 알려 준다
        if (id == "swap_ready")
        {
            if (!GameBalance.DragInsertOn || !GameBalance.DragReplaceOn || FoodStock.Instance == null) return false;
            TurretSlotManager mgr = TurretSlotManager.Instance;
            if (mgr == null || !AnyTurretRunning(mgr)) return false;   // 리뷰 2: 바꿀 포탑이 없으면 교체도 없다
            for (int i = 0; i < mgr.slots.Length; i++)
            {
                TurretSlot s = mgr.slots[i];
                if (s != null && s.IsEmpty && !s.isLocked && !s.isBroken) return false;   // 빈 칸이 있다
            }
            foreach (RecipeData r in RecipeDatabase.All)
            {
                if (r == null || FoodStock.Instance.Get(r.recipeId) < 1) continue;
                bool inTurret = false;
                for (int i = 0; i < mgr.slots.Length; i++)
                {
                    TurretSlot s = mgr.slots[i];
                    if (s != null && !s.IsEmpty && s.recipeId == r.recipeId) { inTurret = true; break; }
                }
                if (!inTurret) return true;
            }
            return false;
        }

        if (id == "first_crate")
            return FindObjectsByType<DeckLoot>(FindObjectsSortMode.None).Length > 0;
        if (id == "first_rock")
            return FindObjectsByType<ResourceRock>(FindObjectsSortMode.None).Length > 0;

        return false;
    }

    /// <summary>v1.10: 요리가 들어 있고 잠기지도 파손되지도 않은 포탑이 하나라도 있나</summary>
    private static bool AnyTurretRunning(TurretSlotManager mgr)
    {
        for (int i = 0; i < mgr.slots.Length; i++)
        {
            TurretSlot s = mgr.slots[i];
            if (s != null && !s.IsEmpty && !s.isLocked && !s.isBroken) return true;
        }
        return false;
    }

    // ─────────────────────────────────────────────
    // 배너 (상단 중앙 - 기차 스트립 아래)
    // ─────────────────────────────────────────────
    private void BuildBanner()
    {
        bannerCanvas = UIFactory.CreateCanvas("TutorialHint_Canvas", 610);   // 증강(600) 위, 경고(640) 아래
        DontDestroyOnLoad(bannerCanvas.gameObject);   // v1.3: 씬 리로드 뒤에도 배너가 남는다 (이 오브젝트처럼)

        bannerRoot = UIFactory.CreatePanel(bannerCanvas.transform, "Banner",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-310f, -160f), new Vector2(310f, -84f),   // v1.8: 폭 660 -> 620 (좌우 판 사이 띠 - UISkin.TopBandWidth. 660 은 커진 좌상단 판에 걸쳤다)
            UIFactory.PANEL, UIFactory.GOLD, 2f);

        // v1.13: 왼쪽 그림 칸 (그림이 있을 때만 켜고 글을 오른쪽으로 민다)
        GameObject iconGo = new GameObject("Icon");
        bannerIconRt = iconGo.AddComponent<RectTransform>();
        bannerIconRt.SetParent(bannerRoot, false);
        bannerIconRt.anchorMin = new Vector2(0f, 0.5f); bannerIconRt.anchorMax = new Vector2(0f, 0.5f); bannerIconRt.pivot = new Vector2(0f, 0.5f);
        bannerIconRt.anchoredPosition = new Vector2(10f, 0f); bannerIconRt.sizeDelta = new Vector2(56f, 56f);
        bannerIcon = iconGo.AddComponent<Image>();
        bannerIcon.preserveAspect = true; bannerIcon.raycastTarget = false;
        iconGo.SetActive(false);

        bannerTitle = UIFactory.CreateText(bannerRoot, "Title", "", 18,
            UIFactory.GOLD, TextAnchor.UpperCenter);
        bannerTitle.rectTransform.offsetMin = new Vector2(10f, 30f);
        bannerTitle.rectTransform.offsetMax = new Vector2(-10f, -6f);

        bannerBody = UIFactory.CreateText(bannerRoot, "Body", "", 15,
            UIFactory.CREAM, TextAnchor.UpperCenter);
        bannerBody.rectTransform.offsetMin = new Vector2(10f, 18f);
        bannerBody.rectTransform.offsetMax = new Vector2(-10f, -30f);
        // v1.8: 긴 본문(15px 로 660 안팎)은 판 밖으로 삐져나가지 않게 글자를 줄여 한 줄에 맞춘다 (15 -> 최소 13)
        bannerBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        bannerBody.verticalOverflow = VerticalWrapMode.Truncate;
        bannerBody.resizeTextForBestFit = true;
        bannerBody.resizeTextMinSize = 13;
        bannerBody.resizeTextMaxSize = 15;

        Text footer = UIFactory.CreateText(bannerRoot, "Footer", "[H] 지나간 안내 다시 보기", 11,
            UIFactory.DIM, TextAnchor.LowerRight);
        footer.rectTransform.offsetMin = new Vector2(10f, 3f);
        footer.rectTransform.offsetMax = new Vector2(-8f, -56f);
        bannerFooter = footer;   // v1.13: 그림 띠에선 끈다

        bannerRoot.gameObject.SetActive(false);
    }

    private Image bannerIcon;
    private RectTransform bannerIconRt;
    private Text bannerFooter;   // v1.13: 바닥 "[H] 지나간 안내 다시 보기"

    private void ShowBanner(HintDef def)
    {
        bannerTitle.text = "일지 조각 - " + def.title;
        bannerBody.text = def.body;
        // v1.13: 그림이 있으면 왼쪽에, 글은 그만큼 오른쪽으로
        string ic = GameBalance.HintIconOn ? IconOf(def.id) : null;
        Sprite sp = string.IsNullOrEmpty(ic) ? null : SpriteBank.Get(ic);
        if (bannerIcon != null)
        {
            bannerIcon.gameObject.SetActive(sp != null);
            if (sp != null) bannerIcon.sprite = sp;
        }
        float left = sp != null ? 74f : 10f;
        if (GameBalance.HintIconOn)
        {
            // v1.13 (리뷰 M5): 제목 46 ~ 70 / 본문 6 ~ 46 (두 줄 - 그림 때문에 폭이 줄어 넘치던 둘째 줄이 잘렸다), 바닥 "[H] ..." 줄은 뺀다 (글 줄이기)
            bannerTitle.rectTransform.offsetMin = new Vector2(left, 46f);
            bannerBody.rectTransform.offsetMin = new Vector2(left, 6f);
            bannerBody.rectTransform.offsetMax = new Vector2(-10f, -30f);
            if (bannerFooter != null) bannerFooter.gameObject.SetActive(false);
        }
        else
        {
            bannerTitle.rectTransform.offsetMin = new Vector2(left, 30f);
            bannerBody.rectTransform.offsetMin = new Vector2(left, 18f);
        }
        bannerRoot.gameObject.SetActive(true);
        showUntil = Time.unscaledTime + GameBalance.HintShowSec;   // v1.1: 6.5 -> GameBalance(5초)
        bannerShownAt = Time.unscaledTime;
        SoundManager.Play("sfx_ui_click");   // 클립 없으면 무시
    }

    private float bannerShownAt = 0f;

    private void HideBannerIfExpired(bool force)
    {
        if (bannerRoot == null || !bannerRoot.gameObject.activeSelf) return;
        // v1.1 (A13): 0.6초 지난 뒤 아무 키나 누르면 먼저 닫힌다 (읽었으면 넘긴다)
        // v1.13 (리뷰 H1): 싸우는 중엔 키로 안 닫힌다 - 달리고 [E] 누르는 키에 뜨자마자 닫혀 못 읽던 것. 시간(HintShowSec)이 되면 닫힌다
        bool inBattle = GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Battle;
        bool dismissed = !inBattle && Time.unscaledTime - bannerShownAt > 1.2f && Input.anyKeyDown;
        if (force || dismissed || Time.unscaledTime >= showUntil)
        {
            bannerRoot.gameObject.SetActive(false);
            showUntil = Time.unscaledTime;
        }
    }

    // ─────────────────────────────────────────────
    // [H] 도움말 아카이브
    // ─────────────────────────────────────────────
    private void ToggleArchive()
    {
        archiveOpen = !archiveOpen;
        if (archiveOpen) BuildArchive();
        else if (archiveRoot != null) Destroy(archiveRoot.gameObject);
    }

    /// <summary>열 때마다 새로 그린다 (본 항목이 늘었을 수 있으니)</summary>
    private void BuildArchive()
    {
        if (archiveCanvas == null)
            archiveCanvas = UIFactory.CreateCanvas("TutorialArchive_Canvas", 575);
        if (archiveRoot != null) Destroy(archiveRoot.gameObject);

        float height = 96f + HINTS.Length * 30f;
        archiveRoot = UIFactory.CreatePanel(archiveCanvas.transform, "Archive",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-310f, -height * 0.5f), new Vector2(310f, height * 0.5f),
            UIFactory.PANEL, UIFactory.COPPER, 2f);

        Text title = UIFactory.CreateText(archiveRoot, "Title",
            "차장의 안내 일지 - 황야에서 배운 것들", 19, UIFactory.GOLD, TextAnchor.UpperCenter);
        title.rectTransform.offsetMin = new Vector2(10f, height - 40f);
        title.rectTransform.offsetMax = new Vector2(-10f, -10f);

        int seenCount = 0;
        for (int i = 0; i < HINTS.Length; i++)
        {
            bool seen = Seen(HINTS[i].id);
            if (seen) seenCount++;

            float top = height - 52f - i * 30f;
            Text row = UIFactory.CreateText(archiveRoot, "Row" + i,
                seen ? (HINTS[i].title + "  -  " + HINTS[i].body)
                     : "???  -  아직 못 본 상황",
                13, seen ? UIFactory.CREAM : UIFactory.DIM, TextAnchor.MiddleLeft);
            row.rectTransform.anchorMin = new Vector2(0f, 0f);
            row.rectTransform.anchorMax = new Vector2(1f, 0f);
            row.rectTransform.offsetMin = new Vector2(18f, top - 26f);
            row.rectTransform.offsetMax = new Vector2(-14f, top);
        }

        Text footer = UIFactory.CreateText(archiveRoot, "Footer",
            "기록 " + seenCount + " / " + HINTS.Length + "  -  [H] 닫기", 13,
            UIFactory.DIM, TextAnchor.LowerCenter);
        footer.rectTransform.offsetMin = new Vector2(10f, 8f);
        footer.rectTransform.offsetMax = new Vector2(-10f, -(height - 34f));
    }
}
