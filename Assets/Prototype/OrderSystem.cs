using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// [OrderSystem.cs] v1 (신규, v9.25 2026-10-16 5차 피드백 7·9번 "6렙 4개 찍으면 이제 뭐함 - 재료 개많이 남음" / "1라운드 중반부터 할 게 없음" -> 스펙 B4 단골 주문)
/// 웨이브마다 한 번 (첫 주문은 GameBalance.OrderFirstWave 부터, 보스 웨이브는 쉰다) 왼쪽에 주문 카드가 들어온다: "냉기 요리 2개 - 40초".
///   - 조리를 마칠 때마다 (FoodStock.OnCooked - 일반 조리·접시 진화 조리·포탑 진화 조리) 그 계열 요리면 +1. 다 채운 처리는 자기 Update 에서 (조리 콜스택 밖)
///   - 다 채우면 그 계열 포탑 피해 +40% 20초 (TurretAttackExecutor.DealDamage 가 DamageMulFor 를 곱한다) + 골드 30. 버프 중엔 그 포탑 위에 "+40%"
///   - 못 채우면 회색 "[주문 놓침]" 으로 흐려진다 - 벌칙 없음. 정차하면 조용히 접힌다
///   - 주문 계열 = 지금 가진 재료로 만들 수 있는 기본 요리가 있는 계열 중 무작위 (방어 계열 제외). 카드 넷째 줄에 만들 수 있는 요리 둘
/// 화면 = 목업 px/user_pack_1005/sheets/ui_mock_v925.png (왼쪽 HP 판·유물 카드 아래 300 x 140 - 목업에 보상 줄을 더했다). 연출은 짧게 (들어옴 0.25초 / 완료 0.3초 / 놓침 1.5초)
/// 글은 바뀔 때만 쓴다 (매 프레임 문자열을 만들지 않는다). 포탑 위 "+40%" 는 LateUpdate 에서 (카메라가 움직인 뒤 자리를 잡는다)
/// 스위치·값 = GameBalance 맨 아래 v9.25 섹션 (OrderOn = false 면 주문 없음). 씬 세팅 없음 (RuntimeInitializeOnLoadMethod)
/// VS 2017 (C# 7.3) 호환
/// </summary>
[DefaultExecutionOrder(1000)]   // 카메라 추적·흔들림보다 뒤 - 포탑 위 꼬리표가 한 프레임 묵은 카메라로 떨리지 않게
public class OrderSystem : MonoBehaviour
{
    private static OrderSystem instance;

    private enum State { Idle, Active, Done, Missed, Closing }
    private State state = State.Idle;

    // ---------- 주문 ----------
    private FoodTag family;          // 주문 계열
    private int need, made;          // 채울 수 / 채운 수
    private float timeLeft;          // 남은 주문 시간 (게임 시간 - 멈추면 같이 멈춘다)
    private float buffLeft;          // 완료 버프 남은 시간
    private int lastWave = -1;       // 마지막으로 본 웨이브 (웨이브가 바뀌면 주문 자격을 새로)
    private bool wasBattle;          // 지난 프레임이 전투였나 (전투로 막 들어온 프레임 = 대기 시작)
    private float waveSeenAt;        // 대기 시작 시각 (새 웨이브 또는 전투 진입)
    private bool orderedThisWave;    // 이번 웨이브에 주문을 냈나 (냈거나 못 낼 걸 확인했으면 true)
    private float hintRefreshAt;     // 넷째 줄(만들 수 있는 요리) 다시 고를 시각

    // ---------- 화면 ----------
    private Canvas canvas;
    private RectTransform card;
    private CanvasGroup group;
    private Image ring;              // 스킨 테 (없으면 null - 대신 왼쪽 색 띠)
    private Image sideBar;
    private Image icon;
    private Text titleText, timeText, mainText, countText, rewardText, hintText;
    private RectTransform gaugeFill;
    private Image gaugeImg;
    private readonly List<Text> turretTags = new List<Text>();
    private float slideT = 1f;       // 들어옴 미끄럼 진행 (0 -> 1, 실시간)
    private float punchT = 1f;       // 숫자 튕김 진행
    private float flashT = 1f;       // 완료 번쩍 진행
    private float fadeT;             // 놓침·접힘 흐려짐 진행
    private int shownSec = -1;       // timeText 에 쓴 초 (바뀔 때만 다시 쓴다)
    private int shownTurrets = -1;   // 완료 둘째 줄에 쓴 포탑 수

    private const float CARD_W = 300f, CARD_H = 140f;
    private const float CARD_X = 8f, CARD_Y = -330f;   // 좌상단 판(8 ~ 184)과 유물 카드(196 ~ 316) 아래
    private const float SLIDE_SEC = 0.25f, PUNCH_SEC = 0.2f, FLASH_SEC = 0.3f, MISS_FADE_SEC = 1.5f, CLOSE_FADE_SEC = 0.3f;
    private static readonly Color OK_GREEN = new Color(0.38f, 0.8f, 0.43f, 1f);
    private static readonly Color MISS_GREY = new Color(0.47f, 0.44f, 0.39f, 1f);
    private static readonly Color DIM = new Color(0.63f, 0.55f, 0.43f, 1f);
    private static readonly FoodTag[] FAMILIES = { FoodTag.Phys, FoodTag.Elec, FoodTag.Fire, FoodTag.Ice, FoodTag.Poison };
    private static readonly int MATERIAL_KINDS = System.Enum.GetValues(typeof(MaterialType)).Length;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("OrderSystem");
        DontDestroyOnLoad(go);
        go.AddComponent<OrderSystem>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        Build();
        FoodStock.OnCooked += OnCooked;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        FoodStock.OnCooked -= OnCooked;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    /// <summary>장면이 새로 불리면(운행 포기·재출발) 주문·버프를 비운다. 덧붙이는 로드는 건드리지 않는다</summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) ResetAll();
    }

    // ==================================================================
    //  다른 시스템이 읽는 값
    // ==================================================================

    /// <summary>이 계열 포탑의 주문 버프 피해 배율 (버프 없으면 1). TurretAttackExecutor.DealDamage 가 곱한다</summary>
    public static float DamageMulFor(FoodTag tag)
    {
        if (instance == null || instance.state != State.Done || tag != instance.family) return 1f;
        return 1f + GameBalance.OrderBuffPct;
    }

    // ==================================================================
    //  흐름
    // ==================================================================

    private void Update()
    {
        if (!GameBalance.OrderOn) { if (state != State.Idle) ResetAll(); return; }

        bool battle = GameManager.Instance != null
            && GameManager.Instance.currentState == GameManager.GameState.Battle
            && !TutorialDirector.Active;   // 견습 운행·자유 연습에는 주문이 없다

        if (!battle)
        {
            wasBattle = false;
            // 정차·게임오버: 진행 중이던 주문은 조용히 접고(벌칙 없음) 버프도 끝낸다 - 싸움이 없으니
            if (state == State.Active || state == State.Done) BeginClose();
            TickVisuals();
            return;
        }

        // 새 웨이브(GameManager 가 전투에 들어갈 때 웨이브 번호를 올린다)이거나 전투로 막 들어왔으면 대기를 새로 센다
        int wave = GameManager.Instance.currentWave;
        if (wave != lastWave) { lastWave = wave; orderedThisWave = false; wasBattle = false; }
        if (!wasBattle) { waveSeenAt = Time.time; wasBattle = true; }

        float dt = Time.deltaTime;
        switch (state)
        {
            case State.Idle:
                if (CanStartOrder(wave)) TryStartOrder();
                break;
            case State.Active:
                if (made >= need) { Complete(); break; }   // 다 채운 처리는 조리 콜스택 밖에서
                timeLeft -= dt;
                if (Time.time >= hintRefreshAt) RefreshHint();
                if (timeLeft <= 0f) Miss();
                break;
            case State.Done:
                buffLeft -= dt;
                if (buffLeft <= 0f) BeginClose();
                break;
        }
        TickVisuals();
    }

    /// <summary>포탑 위 "+40%" 는 카메라가 움직인 뒤에 자리를 잡는다</summary>
    private void LateUpdate()
    {
        if (state == State.Done && card != null) TickTags();
    }

    /// <summary>이번 웨이브에 주문을 낼 때인가</summary>
    private bool CanStartOrder(int wave)
    {
        if (orderedThisWave) return false;
        if (wave < GameBalance.OrderFirstWave || GameBalance.IsBossWave(wave)) return false;   // 보스 웨이브는 보스 패턴이 할 일을 준다
        if (Time.time - waveSeenAt < GameBalance.OrderDelaySec) return false;                   // 웨이브가 자리 잡은 뒤에
        if (WaveManager.PostWaveHold) return false;                                             // 웨이브가 끝나 사고 수습만 기다리는 중
        // 주방 사고가 나 있으면 기다린다 (한꺼번에 두 가지를 시키지 않는다)
        if (KitchenEventManager.Instance != null && KitchenEventManager.Instance.EventRunning) return false;
        return true;
    }

    private void TryStartOrder()
    {
        orderedThisWave = true;   // 못 내도 이번 웨이브는 끝 (재료가 없으면 다음 웨이브에)

        // 지금 만들 수 있는 계열만 후보
        List<FoodTag> pool = new List<FoodTag>();
        for (int i = 0; i < FAMILIES.Length; i++)
            if (CookableNow(FAMILIES[i], 1).Count > 0) pool.Add(FAMILIES[i]);
        if (pool.Count == 0)
        {
            Debug.Log("[주문] 만들 수 있는 요리가 없어 이번 웨이브는 쉰다");
            return;
        }

        family = pool[Random.Range(0, pool.Count)];
        need = Mathf.Max(1, GameBalance.OrderCount);
        made = 0;
        timeLeft = Mathf.Max(5f, GameBalance.OrderTimeSec);
        state = State.Active;

        ShowCard();
        SoundManager.Play("sfx_lever");   // 주문표를 거는 쇠 손잡이 소리 (밝은 종은 쓰지 않는다 - 소리 원칙)
        Debug.Log("[주문] " + TagWord(family) + " 요리 " + need + "개 - " + Mathf.RoundToInt(timeLeft) + "초");
    }

    /// <summary>조리를 하나 마쳤다 (FoodStock.CountCook 이 부른다). 세기만 한다 - 골드·버프·완료음은 Update 가</summary>
    private void OnCooked(string recipeId)
    {
        if (state != State.Active || string.IsNullOrEmpty(recipeId)) return;
        RecipeData r = RecipeDatabase.Get(recipeId);
        if (r == null || r.tag != family) return;

        made++;
        punchT = 0f;
        if (countText != null) countText.text = Mathf.Min(made, need) + "/" + need;
        if (made < need) SoundManager.Play("sfx_ui_click");
    }

    private void Complete()
    {
        state = State.Done;
        buffLeft = Mathf.Max(1f, GameBalance.OrderBuffSec);
        flashT = 0f;
        if (GameManager.Instance != null && GameBalance.OrderGold > 0) GameManager.Instance.AddGold(GameBalance.OrderGold);   // 카드 넷째 줄 "골드 30 받았어"
        SoundManager.Play("sfx_event_resolve");
        SoundManager.PlayDelayed("sfx_gold", 0.15f);

        // 완료 카드: 초록 + 버프 (글은 여기서 한 번, 남은 초·포탑 수만 바뀔 때)
        string tag = TagWord(family);
        titleText.text = "[주문 완료]";
        titleText.color = OK_GREEN;
        mainText.text = tag + " 포탑 피해 +" + Mathf.RoundToInt(GameBalance.OrderBuffPct * 100f) + "%";
        countText.text = "";
        hintText.text = GameBalance.OrderGold > 0 ? "골드 " + GameBalance.OrderGold + " 받았어" : "";
        shownSec = -1; shownTurrets = -1;
        if (ring != null) ring.color = OK_GREEN;
        if (sideBar != null) sideBar.color = OK_GREEN;
        Debug.Log("[주문] 완료 - " + tag + " 포탑 피해 +" + Mathf.RoundToInt(GameBalance.OrderBuffPct * 100f) + "% " + buffLeft + "초, 골드 +" + GameBalance.OrderGold);
    }

    private void Miss()
    {
        state = State.Missed;
        fadeT = 0f;
        titleText.text = "[주문 놓침]";
        titleText.color = MISS_GREY;
        timeText.text = "";
        rewardText.text = "벌칙 없음";
        hintText.text = "";
        SetGauge(0f, MISS_GREY);
        if (ring != null) ring.color = MISS_GREY;
        if (sideBar != null) sideBar.color = MISS_GREY;
        Debug.Log("[주문] 놓침 (" + made + "/" + need + ") - 벌칙 없음");
    }

    private void BeginClose()
    {
        if (state == State.Idle || state == State.Closing || state == State.Missed) return;
        state = State.Closing;
        fadeT = 0f;
    }

    private void ResetAll()
    {
        state = State.Idle;
        lastWave = -1;
        wasBattle = false;
        orderedThisWave = false;
        if (card != null) card.gameObject.SetActive(false);
        HideTags();
    }

    // ==================================================================
    //  만들 수 있는 요리 (재료 두 칸 = 각 1개, 같은 재료면 2개)
    // ==================================================================

    /// <summary>지금 재료로 만들 수 있는 이 계열 기본 요리 (최대 max 개)</summary>
    private static List<RecipeData> CookableNow(FoodTag tag, int max)
    {
        List<RecipeData> list = new List<RecipeData>();
        MaterialInventory inv = MaterialInventory.Instance;
        if (inv == null) return list;
        for (int i = 0; i < MATERIAL_KINDS && list.Count < max; i++)
        {
            for (int j = i; j < MATERIAL_KINDS && list.Count < max; j++)
            {
                MaterialType a = (MaterialType)i, b = (MaterialType)j;
                if (i == j) { if (inv.Get(a) < 2) continue; }
                else if (inv.Get(a) < 1 || inv.Get(b) < 1) continue;
                RecipeData r = RecipeDatabase.GetByMaterials(a, b);   // 키는 순서와 상관없다 (MakeKey 가 정렬)
                if (r != null && r.tier == 1 && r.tag == tag) list.Add(r);
            }
        }
        return list;
    }

    private void RefreshHint()
    {
        hintRefreshAt = Time.time + 1f;
        if (hintText == null) return;
        List<RecipeData> can = CookableNow(family, 2);
        string s;
        if (can.Count == 0) s = "재료가 모자라";
        else s = can.Count > 1 ? can[0].displayName + " · " + can[1].displayName : can[0].displayName;
        if (hintText.text != s) hintText.text = s;
    }

    private static string TagWord(FoodTag tag)
    {
        switch (tag)
        {
            case FoodTag.Phys: return "물리";
            case FoodTag.Elec: return "전기";
            case FoodTag.Fire: return "화염";
            case FoodTag.Ice: return "냉기";
            case FoodTag.Poison: return "독";
            default: return "방어";
        }
    }

    private static string MatIconKey(FoodTag tag)
    {
        switch (tag)
        {
            case FoodTag.Elec: return "ui_mat_elec";
            case FoodTag.Fire: return "ui_mat_fire";
            case FoodTag.Ice: return "ui_mat_ice";
            case FoodTag.Poison: return "ui_mat_poison";
            default: return "ui_mat_meat";
        }
    }

    /// <summary>이 계열 포탑 수 (전설 포함)</summary>
    private static int TurretCount(FoodTag tag)
    {
        TurretSlotManager mgr = TurretSlotManager.Instance;
        if (mgr == null) return 0;
        int n = 0;
        for (int i = 0; i < mgr.slots.Length; i++)
        {
            TurretSlot s = mgr.slots[i];
            if (s == null || s.IsEmpty) continue;
            RecipeData r = s.Recipe;
            if (r != null && r.tag == tag) n++;
        }
        return n;
    }

    // ==================================================================
    //  화면
    // ==================================================================

    private void Build()
    {
        canvas = UIFactory.CreateCanvas("OrderCanvas", 452);   // 알림 줄(455)·유물 카드(458)·창들 아래
        canvas.transform.SetParent(transform, false);           // 이 오브젝트(DontDestroyOnLoad) 밑 - 씬이 바뀌어도 남는다
        GraphicRaycaster ray = canvas.GetComponent<GraphicRaycaster>();
        if (ray != null) ray.enabled = false;   // 클릭을 가로채지 않는다

        card = KitchenEventManager.MakeBox(canvas.transform, "OrderCard", new Color(0.08f, 0.06f, 0.05f, 0.95f));
        card.anchorMin = new Vector2(0f, 1f);
        card.anchorMax = new Vector2(0f, 1f);
        card.pivot = new Vector2(0f, 1f);
        card.anchoredPosition = new Vector2(CARD_X, CARD_Y);
        card.sizeDelta = new Vector2(CARD_W, CARD_H);
        Image bg = card.GetComponent<Image>();
        bg.raycastTarget = false;
        if (UISkin.Available)
        {
            UISkin.Plate(bg, Color.white);
            ring = UISkin.AddRing(card, UISkin.BRASS_DIM);
        }
        else
        {
            RectTransform bar = KitchenEventManager.MakeBox(card, "Side", Color.white);
            bar.anchorMin = new Vector2(0f, 0f); bar.anchorMax = new Vector2(0f, 1f);
            bar.pivot = new Vector2(0f, 0.5f); bar.anchoredPosition = Vector2.zero; bar.sizeDelta = new Vector2(6f, 0f);
            sideBar = bar.GetComponent<Image>();
            sideBar.raycastTarget = false;
        }
        group = card.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        titleText = Line("Title", 17, 14f, -6f, 22f, TextAnchor.MiddleLeft, UIFactory.GOLD);
        timeText = Line("Time", 17, 14f, -6f, 22f, TextAnchor.MiddleRight, UIFactory.CREAM);
        mainText = Line("Main", 22, 54f, -32f, 30f, TextAnchor.MiddleLeft, UIFactory.CREAM);
        countText = Line("Count", 22, 14f, -32f, 30f, TextAnchor.MiddleRight, UIFactory.GOLD);
        rewardText = Line("Reward", 15, 14f, -64f, 22f, TextAnchor.MiddleLeft, UIFactory.GOLD);   // 받는 것 (완료면 무엇이 세졌나)
        hintText = Line("Hint", 15, 14f, -86f, 22f, TextAnchor.MiddleLeft, DIM);                   // 만들 수 있는 요리 둘 (완료면 골드)

        GameObject iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(card, false);
        RectTransform irt = iconGo.AddComponent<RectTransform>();
        irt.anchorMin = new Vector2(0f, 1f); irt.anchorMax = new Vector2(0f, 1f); irt.pivot = new Vector2(0f, 1f);
        irt.anchoredPosition = new Vector2(12f, -31f);
        irt.sizeDelta = new Vector2(32f, 32f);
        icon = iconGo.AddComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;

        // 아래 막대 = 남은 주문 시간 (완료면 버프 남은 시간)
        RectTransform gbg = KitchenEventManager.MakeBox(card, "GaugeBG", new Color(0.055f, 0.047f, 0.07f, 1f));
        gbg.anchorMin = new Vector2(0f, 0f); gbg.anchorMax = new Vector2(1f, 0f); gbg.pivot = new Vector2(0.5f, 0f);
        gbg.anchoredPosition = new Vector2(0f, 12f);
        gbg.sizeDelta = new Vector2(-28f, 10f);
        gbg.GetComponent<Image>().raycastTarget = false;
        gaugeFill = KitchenEventManager.MakeBox(gbg, "Fill", Color.white);
        gaugeFill.anchorMin = new Vector2(0f, 0f); gaugeFill.anchorMax = new Vector2(1f, 1f);
        gaugeFill.offsetMin = new Vector2(2f, 2f); gaugeFill.offsetMax = new Vector2(-2f, -2f);
        gaugeImg = gaugeFill.GetComponent<Image>();
        gaugeImg.raycastTarget = false;

        card.gameObject.SetActive(false);
    }

    /// <summary>카드 안 한 줄 (왼쪽 위 기준, 폭 = 카드 안쪽)</summary>
    private Text Line(string name, int size, float left, float top, float height, TextAnchor align, Color color)
    {
        Text t = KitchenEventManager.MakeText(card, name, "", size, color);
        RectTransform rt = t.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(left, top);
        rt.sizeDelta = new Vector2(-(left + 14f), height);
        t.alignment = align;
        return t;
    }

    /// <summary>주문이 들어왔다: 계열 색·아이콘·글을 한 번 쓰고 왼쪽에서 들인다</summary>
    private void ShowCard()
    {
        Color fam = UIFactory.TagColor(family);
        if (ring != null) ring.color = fam;
        if (sideBar != null) sideBar.color = fam;
        Sprite s = SpriteBank.Get(MatIconKey(family));
        icon.sprite = s;
        icon.enabled = s != null;

        string tag = TagWord(family);
        titleText.text = "[단골 주문]";
        titleText.color = UIFactory.GOLD;
        mainText.text = tag + " 요리 " + need + "개";
        countText.text = made + "/" + need;
        rewardText.text = "보상 " + tag + " 포탑 +" + Mathf.RoundToInt(GameBalance.OrderBuffPct * 100f) + "%"
            + (GameBalance.OrderGold > 0 ? " · 골드 +" + GameBalance.OrderGold : "");
        hintText.text = "";
        RefreshHint();
        shownSec = -1;

        group.alpha = 1f;
        slideT = 0f;
        punchT = 1f; flashT = 1f;
        card.localScale = Vector3.one;
        card.gameObject.SetActive(true);
    }

    /// <summary>남은 초·막대·연출만 매 프레임 (글은 바뀔 때만). 연출 시간은 실시간 - 히트스톱에 멈추지 않게</summary>
    private void TickVisuals()
    {
        if (card == null)
        {
            // 카드가 없어도 상태는 Idle 로 돌아가야 다음 웨이브 주문이 나온다
            if (state == State.Missed || state == State.Closing) state = State.Idle;
            return;
        }
        if (state == State.Idle) { if (card.gameObject.activeSelf) card.gameObject.SetActive(false); HideTags(); return; }
        float udt = Time.unscaledDeltaTime;

        // 들어옴: 왼쪽에서 미끄러져 들어온다
        if (slideT < 1f) slideT = Mathf.Min(1f, slideT + udt / SLIDE_SEC);
        float ease = 1f - (1f - slideT) * (1f - slideT);
        card.anchoredPosition = new Vector2(Mathf.Lerp(-CARD_W - 20f, CARD_X, ease), CARD_Y);

        switch (state)
        {
            case State.Active:
                SetSeconds(timeLeft);
                SetGauge(Mathf.Clamp01(timeLeft / Mathf.Max(1f, GameBalance.OrderTimeSec)), UIFactory.TagColor(family));
                break;
            case State.Done:
                SetSeconds(buffLeft);
                int n = TurretCount(family);
                if (n != shownTurrets)
                {
                    shownTurrets = n;
                    string tag = TagWord(family);
                    rewardText.text = n > 0 ? tag + " 포탑 " + n + "개가 세졌어" : tag + " 요리를 칸에 놓으면 세져";
                }
                SetGauge(Mathf.Clamp01(buffLeft / Mathf.Max(1f, GameBalance.OrderBuffSec)), OK_GREEN);
                break;
            case State.Missed:
                fadeT += udt / MISS_FADE_SEC;
                group.alpha = Mathf.Clamp01(1f - fadeT * fadeT);
                if (fadeT >= 1f) { state = State.Idle; card.gameObject.SetActive(false); }
                break;
            case State.Closing:
                fadeT += udt / CLOSE_FADE_SEC;
                group.alpha = Mathf.Clamp01(1f - fadeT);
                if (fadeT >= 1f) { state = State.Idle; card.gameObject.SetActive(false); }
                break;
        }

        // 숫자 튕김 (하나 채울 때) / 완료 번쩍 (카드가 한 번 커졌다 돌아온다 - 왼쪽 위 기준이라 오른쪽·아래로만)
        if (punchT < 1f) punchT = Mathf.Min(1f, punchT + udt / PUNCH_SEC);
        float punch = 1f + 0.35f * (1f - punchT);
        countText.rectTransform.localScale = new Vector3(punch, punch, 1f);
        if (flashT < 1f) flashT = Mathf.Min(1f, flashT + udt / FLASH_SEC);
        float flash = 1f + 0.06f * Mathf.Sin(flashT * Mathf.PI);
        card.localScale = new Vector3(flash, flash, 1f);

        if (state != State.Done) HideTags();   // 버프 중 꼬리표는 LateUpdate 가
    }

    /// <summary>오른쪽 위 남은 초 - 초가 바뀔 때만 글을 쓴다</summary>
    private void SetSeconds(float sec)
    {
        int s = Mathf.Max(0, Mathf.CeilToInt(sec));
        if (s == shownSec) return;
        shownSec = s;
        timeText.text = s + "초";
    }

    private void SetGauge(float ratio, Color color)
    {
        if (gaugeFill == null) return;
        gaugeImg.enabled = ratio > 0.02f;   // 아주 짧으면 여백 2 때문에 폭이 뒤집힌다 - 안 그린다
        gaugeFill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        gaugeFill.offsetMax = new Vector2(-2f, -2f);
        gaugeImg.color = color;
    }

    /// <summary>버프 중: 그 계열 포탑 위에 "+40%" (화면 캔버스 글 - 포탑 이름표와 안 겹치게 포탑 몸 위)</summary>
    private void TickTags()
    {
        TurretSlotManager mgr = TurretSlotManager.Instance;
        Camera cam = Camera.main;
        int used = 0;
        if (mgr != null && cam != null)
        {
            RectTransform crt = canvas.transform as RectTransform;
            float w = crt.rect.width > 1f ? crt.rect.width : UIFactory.RefResolution.x;
            float h = crt.rect.height > 1f ? crt.rect.height : UIFactory.RefResolution.y;
            for (int i = 0; i < mgr.slots.Length; i++)
            {
                TurretSlot s = mgr.slots[i];
                if (s == null || s.IsEmpty) continue;
                RecipeData r = s.Recipe;
                if (r == null || r.tag != family) continue;
                Vector3 world = TrainDeck.RidePos(s.transform.position) + new Vector3(0f, 0.55f, 0f);
                Vector3 sp = cam.WorldToScreenPoint(world);
                Text t = TagAt(used++);
                t.rectTransform.anchoredPosition = new Vector2((sp.x / Mathf.Max(1f, Screen.width) - 0.5f) * w, (sp.y / Mathf.Max(1f, Screen.height) - 0.5f) * h);
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            }
        }
        for (int i = used; i < turretTags.Count; i++)
            if (turretTags[i].gameObject.activeSelf) turretTags[i].gameObject.SetActive(false);
    }

    /// <summary>꼬리표 글 (캔버스 바로 밑, 가운데 기준 - 쓸 때마다 하나씩 늘린다)</summary>
    private Text TagAt(int index)
    {
        while (turretTags.Count <= index)
        {
            Text t = KitchenEventManager.MakeText(canvas.transform, "BuffTag", "+" + Mathf.RoundToInt(GameBalance.OrderBuffPct * 100f) + "%", 16, OK_GREEN);
            t.rectTransform.sizeDelta = new Vector2(80f, 24f);
            Outline o = t.gameObject.AddComponent<Outline>();   // 싸움 화면 위에서도 읽히게 검은 테
            o.effectColor = new Color(0f, 0f, 0f, 0.9f);
            o.effectDistance = new Vector2(2f, -2f);
            t.gameObject.SetActive(false);
            turretTags.Add(t);
        }
        return turretTags[index];
    }

    private void HideTags()
    {
        for (int i = 0; i < turretTags.Count; i++)
            if (turretTags[i] != null && turretTags[i].gameObject.activeSelf) turretTags[i].gameObject.SetActive(false);
    }
}
