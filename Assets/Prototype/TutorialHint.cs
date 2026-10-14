using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TutorialHint.cs] v1.11 (v9.22.2 2026-10-14 유저 "튜토리얼 말투가 번역체·AI 같고 딱딱하다": 안내 글 말투를 반말 해체로 - 명령형 ~라·설명 ~다 끝맺음, 문장 속 기호, 어려운 말(투입·재가동·곁·유인)를 걷어냈다. 규칙 = 문구원칙 10-14 저녁 "말투" 절) / v1.10 (v9.22.1 2026-10-14 유저 "튜토리얼에 드래그가 반영 안 됐다": first_battle·merge_ready 칩을 끌어다 놓기 기준으로, first_stun 에 "방치하면 파손" / 새 칩 fuse_ready(기본 요리 카드 둘 = 진화 조리 - 합칠 수 있는 짝이 재고에 생기면) · swap_ready(빈 칸이 없는데 못 넣은 요리가 있으면 = 교체)) / v1.9 (v9.22 2026-10-14 조작 팩 2-2: Tab 키가 없어져 "재료가 모였다" 칩에서 [Tab] 주방 전체 메뉴를 뺐다 - 조리대 [E] 만) / v1.8 (v9.18 2026-10-06: 배너 폭 660 -> 620 - 씬 HUD 가 UI 배율을 따르면서 커진 좌상단 판과 8 겹치던 것) / v1.7 (v9.13 2026-09-23: first_route 트리거 = BranchRouteUI.ChoicePending - 선로 선택이 카드 창이 아니게 됨) / v1.6 (v9.12 2026-09-22: F4 가 구간 기록도 지운다 / 리롤 복원) / v1.5 (v9.11.1 2026-09-22 문구: 복구법 상태별·공명 예외·이번 운행·다시 뽑기·무방비) / v1.4 (v9.9.2 2026-09-16: 승격 5 - first_town / first_augment / first_route / first_item / first_overheat 는 배너 대신 브리핑 카드(BriefingTexts.Promoted), F4 가 첫 등장 카드 기록도 지운다) / v1.3 (v9.9 2026-09-16: 견습 운행·브리핑 카드 중엔 배너 쉼, F4 가 견습 완료 기록도 지움, 배너 캔버스 DontDestroyOnLoad) / v1.2 (v9.8.1: F4 리셋은 GameBalance.CheatsAllowed 일 때만) / v1.1 (교수 피드백 A13: 표시 5초 + 아무 키 닫기 + 조리 중 대기) - 컨텍스트 트리거 튜토리얼 (설계: 튜토리얼_온보딩_설계_2026-08-18)
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
            "재료 두 개면 요리 하나. 조리대 앞에서 [E], 재료를 올리면 뭐가 나올지 보여"),
        new HintDef("cook_start", "조리 시작",
            "눈금 보고 타이밍 맞춰 눌러. [ESC]로 그만둬도 재료는 돌려받아"),
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

        // 개발 치트 F4: 튜토리얼+프롤로그 기록 전체 리셋 (재테스트용 - 빌드 전 치트 정리 대상)
        if (Input.GetKeyDown(KeyCode.F4) && GameBalance.CheatsAllowed)
        {
            for (int i = 0; i < HINTS.Length; i++)
                PlayerPrefs.DeleteKey(PREF_PREFIX + HINTS[i].id);
            PlayerPrefs.DeleteKey("WDT_PrologueSeen");   // 프롤로그(웨이브 1 스피노 안내)도 초기화
            PlayerPrefs.DeleteKey(TutorialDirector.DONE_KEY);   // v1.3: 견습 운행 완료 기록도 (로비 [T] 강조가 다시 켜진다)
            TutorialDirector.ResetSegments();                   // v1.6: 구간 기록(WDT_Tut_S1..S7) 도 - 인라인 연습·훈련장 표시가 처음으로
            BriefingUI.ResetSeen(BriefingTexts.AllOnceIds());  // v1.4: 정식 런 첫 등장 카드 34장 + 승격 5 기록도
            PlayerPrefs.Save();
            allSeen = false;
            UIManager.Instance?.ShowStatChange("[치트] 튜토리얼/프롤로그/견습 운행/구간 기록 리셋 - 처음 온 셰프가 됐다");
            Debug.Log("[TutorialHint] 치트 F4 - 기록 전체 리셋");
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
            int idx = pending[0];
            pending.RemoveAt(0);
            // v1.4: 승격 5 는 5초 배너 대신 시간 정지 카드 (증강 선택창 위에서도 열린다 - BriefingUI v1.1)
            if (GameBalance.FirstEncounterBriefings && IsPromoted(HINTS[idx].id))
                BriefingUI.ShowOnce("promo_" + HINTS[idx].id, BriefingTexts.Promoted(HINTS[idx].id));
            else
                ShowBanner(HINTS[idx]);
        }

        // 상태 폴링 (전부 봤으면 중단 - 비용 0)
        if (allSeen || Time.unscaledTime < nextPollTime) return;
        nextPollTime = Time.unscaledTime + 0.3f;
        PollTriggers();
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

            MarkSeen(id);          // 트리거 즉시 기록 (큐 대기 중 중복 방지)
            pending.Add(i);
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

        bannerRoot.gameObject.SetActive(false);
    }

    private void ShowBanner(HintDef def)
    {
        bannerTitle.text = "일지 조각 - " + def.title;
        bannerBody.text = def.body;
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
        bool dismissed = Time.unscaledTime - bannerShownAt > 0.6f && Input.anyKeyDown;
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
