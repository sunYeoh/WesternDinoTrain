using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TutorialHint.cs] v1 - 컨텍스트 트리거 튜토리얼 (설계: 튜토리얼_온보딩_설계_2026-08-18)
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
            "[WASD] 달려라, [Shift] 대시! 보급 요리를 포탑 이름표에 클릭해 투입하라"),
        new HintDef("cook_ready", "재료가 모였다",
            "재료 2개 = 요리 1개. [E] 가까운 조리대 / [Tab] 주방 전체 메뉴"),
        new HintDef("cook_start", "조리 개시",
            "판정에 맞춰 손을 움직여라. [ESC] 중단하면 재료는 돌려받는다"),
        new HintDef("merge_ready", "같은 요리는 겹친다",
            "가동 중인 포탑에 같은 요리를 다시 투입 = 레벨업! 같은 포탑 2문은 좌클릭 합체"),
        new HintDef("resonance_near", "공명 임박",
            "같은 속성 2문째다. 3문을 모으면 속성 공명 데미지 +20%!"),
        new HintDef("first_stun", "포탑 마비!",
            "달려가서 [E]로 되살려라. 서 있는 포탑은 요리값을 못 한다"),
        new HintDef("first_overheat", "포탑 과열!",
            "[E]를 꾹 눌러 식혀라. 중간에 손을 놓으면 다시 달아오른다"),
        new HintDef("first_event", "주방 사고!",
            "화살표를 따라 달려가라 - 현장에 도착해야 수습이 시작된다"),
        new HintDef("first_crate", "갑판의 전리품",
            "떨어진 상자는 밟아서 회수한다"),
        new HintDef("first_item", "유물 획득",
            "[V] 소지품 목록을 봐라 - 유물은 하나씩만, 효과는 영구다"),
        new HintDef("first_rock", "광맥 바위 발견",
            "기관차 작살포 [E]로 낚아채라. 가끔 굶주린 것들이 딸려온다"),
        new HintDef("lever_hint", "기관차 레버",
            "기관차의 레버 [E] = 전속 주행. 위험한 만큼 벌이가 좋다"),
        new HintDef("first_town", "간이역 정차",
            "[G] 정비소 - 수리·연마·재료 시장. 도구가 상하면 조리가 어려워진다"),
        new HintDef("first_augment", "증강 선택",
            "[1~3] 선택 / [0] 건너뛰기 / [9] 리롤. 증강이 이번 런의 빌드를 만든다"),
        new HintDef("first_route", "분기 선로",
            "위험과 보상의 교환이다. 폐역에는 선대의 기록이 잠들어 있다"),
        new HintDef("boss_incoming", "보스 접근!",
            "그로기(가슴 해치 개방) 때 [F]로 디버프 요리를 던져라!"),
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
        if (Input.GetKeyDown(KeyCode.F4))
        {
            for (int i = 0; i < HINTS.Length; i++)
                PlayerPrefs.DeleteKey(PREF_PREFIX + HINTS[i].id);
            PlayerPrefs.DeleteKey("WDT_PrologueSeen");   // 프롤로그(웨이브 1 스피노 안내)도 초기화
            PlayerPrefs.Save();
            allSeen = false;
            UIManager.Instance?.ShowStatChange("[치트] 튜토리얼/프롤로그 기록 리셋 - 처음 온 셰프가 됐다");
            Debug.Log("[TutorialHint] 치트 F4 - 기록 전체 리셋");
        }

        if (!GameBalance.TutorialEnabled) { HideBannerIfExpired(true); return; }

        // 배너 수명 관리
        HideBannerIfExpired(false);

        // 표시 중이 아니고 대기 큐가 있으면 다음 힌트 표시
        // (오프닝 등 전체 화면 연출 중에는 기다렸다가 보여준다)
        if (Time.unscaledTime >= showUntil && pending.Count > 0
            && !PauseMenu.IsOpen && !StoryTexts.IsBlocking)
        {
            int idx = pending[0];
            pending.RemoveAt(0);
            ShowBanner(HINTS[idx]);
        }

        // 상태 폴링 (전부 봤으면 중단 - 비용 0)
        if (allSeen || Time.unscaledTime < nextPollTime) return;
        nextPollTime = Time.unscaledTime + 0.3f;
        PollTriggers();
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
        if (id == "first_route") return BranchRouteUI.IsOpen;
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

        if (id == "first_crate")
            return FindObjectsByType<DeckLoot>(FindObjectsSortMode.None).Length > 0;
        if (id == "first_rock")
            return FindObjectsByType<ResourceRock>(FindObjectsSortMode.None).Length > 0;

        return false;
    }

    // ─────────────────────────────────────────────
    // 배너 (상단 중앙 - 기차 스트립 아래)
    // ─────────────────────────────────────────────
    private void BuildBanner()
    {
        bannerCanvas = UIFactory.CreateCanvas("TutorialHint_Canvas", 610);   // 증강(600) 위, 경고(640) 아래

        bannerRoot = UIFactory.CreatePanel(bannerCanvas.transform, "Banner",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-330f, -160f), new Vector2(330f, -84f),
            UIFactory.PANEL, UIFactory.GOLD, 2f);

        bannerTitle = UIFactory.CreateText(bannerRoot, "Title", "", 18,
            UIFactory.GOLD, TextAnchor.UpperCenter);
        bannerTitle.rectTransform.offsetMin = new Vector2(10f, 30f);
        bannerTitle.rectTransform.offsetMax = new Vector2(-10f, -6f);

        bannerBody = UIFactory.CreateText(bannerRoot, "Body", "", 15,
            UIFactory.CREAM, TextAnchor.UpperCenter);
        bannerBody.rectTransform.offsetMin = new Vector2(10f, 18f);
        bannerBody.rectTransform.offsetMax = new Vector2(-10f, -30f);

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
        showUntil = Time.unscaledTime + 6.5f;
        SoundManager.Play("sfx_ui_click");   // 클립 없으면 무시
    }

    private void HideBannerIfExpired(bool force)
    {
        if (bannerRoot != null && bannerRoot.gameObject.activeSelf
            && (force || Time.unscaledTime >= showUntil))
            bannerRoot.gameObject.SetActive(false);
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
                     : "???  -  아직 만나지 않은 상황이다",
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
