using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 분기 선로 1개의 정의 (다음 웨이브에 적용될 규칙 + 보상)
/// v2 (v9.13): 보상이 골드·재료 -> "끝나면 증강 1회 더"(extraPickGrade) / 유물 확률(relicChance, 1 = 확정) 로 바뀌었다. sign = 화면 자리
/// </summary>
public class RouteData
{
    public string id;          // 내부 식별자 (straight / hunt / danger / fog / ghost)
    public string routeName;   // 표시 이름
    public string desc;        // 규칙 한 줄 (손님 +20% 등)
    public string rewardDesc;  // 보상 한 줄

    public float countMul = 1f;     // 손님 수 배율
    public float statMul = 1f;      // 손님 HP/ATK 배율
    public bool journal = false;    // 끝나면 선대의 일지 발견 (폐역)
    public bool earlyEvent = false; // 주방 사고가 이르게 옴 (안개)
    public int extraPickGrade = -1; // 끝나면 증강 1회 더 - 등급 (AugmentGrade 값: 0 은 / 1 금, -1 = 없음)
    public float relicChance = 0f;  // 끝나면 유물 확률 (0 = 없음, 1 = 확정)
    public int sign = 0;            // 화면 자리: +1 위 가지 / 0 곧은 길 / -1 아래 가지 (ShowRoutes 가 정한다)
}

/// <summary>
/// [BranchRouteUI.cs] v2 (v9.13 2026-09-23: 선로 v2 - 목업 v2 그대로) / v1.1 숫자키 / v1 (Phase 2: 분기 선로)
///
/// v2: 어두운 카드 창(시간 정지) 삭제. 정차 때 두상 앞에 갈림길이 놓이고(ParallaxBackground.PlaceFork), 카메라가 앞을 비추며 줌아웃(CameraZoom.SetRouteFraming),
///     화면 왼쪽에 팻말 카드 226x84 가 가지 수만큼 세로로 - 각 가지가 화면 왼쪽 끝을 지나는 높이에, 짧은 이음선으로 그 선로를 가리킨다.
///     [1~3] 또는 카드 클릭으로 고른다 (번호 = 위에서부터). 고르면 그 가지가 금색으로 깜빡이고 나머지 카드는 회색 - 출발할 때까지 남는다.
///     고르기 전 [Enter] = "먼저 길을 골라라". 시간은 안 멈춘다 (요리·정비소 그대로). 다른 창(정비소·카드·일시정지)이 떠 있으면 키를 안 받는다.
///     출발(OnDepart - WaveManager) = 카드 치우고 ParallaxBackground.BeginLaneShift(가지) + 카메라 복귀 + RouteFX.SetTone(길)
///
/// 선로 종류 (v2 보상):
///  - 곧은 선로: 규칙 없음 / 보상 없음 (항상)
///  - 사냥터 선로: 손님 +20% / 끝나면 은 증강 1회 더
///  - 위험 선로: 손님 +40% · 강화 +15% / 끝나면 금 증강 1회 더 + 유물 50%
///  - 안개 선로: 손님 -25% · 주방 사고가 일찍 온다 / 끝나면 유물 50%
///  - 폐역 (25%, 마지막 칸 대체): 손님 -40% / 끝나면 선대의 일지 + 유물 확정
///  - 보스 직전에는 곧은 / 위험 2택만 (위 가지 전철기만 놓인다)
///
/// 사용법: 하이어라키 아무 오브젝트(AugmentPickUI 있는 곳)에 AddComponent (v1 과 같다). WaveManager 가 ShowRoutes / OnDepart / Cancel 을 부른다
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class BranchRouteUI : MonoBehaviour
{
    public static BranchRouteUI Instance;

    /// <summary>v2: 카드 창이 아니라 항상 false (다른 창의 입력 가드 호환용). 고르는 중인지는 ChoicePending</summary>
    public static bool IsOpen { get { return false; } }
    /// <summary>선로를 아직 안 골랐다 (카드가 떠 있고 출발 못 함)</summary>
    public static bool ChoicePending { get; private set; }
    /// <summary>고른 선로 (출발 전까지). 없으면 null</summary>
    public static RouteData Chosen { get; private set; }

    // 카드 배치 (캔버스 1920x1080 기준 px, y 는 아래에서)
    private const float CARD_W = 226f, CARD_H = 84f, CARD_X = 16f;
    private const float TOP_KEEP = 132f;              // 위쪽 비워 두는 높이 (HP 판 아래)
    private const float BOTTOM_KEEP = 196f;           // 아래쪽 비워 두는 높이 (하단 바 184 + 여유)
    private const float LEADER_LEN = 26f;             // 이음선 길이 (카드 오른쪽 끝에서)

    private Canvas canvas;
    private GameObject canvasGo;
    private System.Action<RouteData> onChosen;
    private List<RouteData> shownRoutes = new List<RouteData>();   // 화면 순서 (위 -> 아래)
    private readonly List<CardView> cards = new List<CardView>();
    private float enterNagAt = -10f;

    /// <summary>카드 1장의 위젯 묶음</summary>
    private class CardView
    {
        public RouteData route;
        public RectTransform root;
        public Image ring;
        public Text title, risk, reward;
        public RectTransform leader, dot;
        public Image leaderImg, dotImg;
        public Color ringColor, titleColor;
    }

    private static readonly Color GREY = new Color(0.47f, 0.44f, 0.39f, 1f);
    private static readonly Color RING_GOLD = new Color(1f, 0.84f, 0.38f, 1f);
    private static readonly Color RING_BRASS = new Color(0.72f, 0.56f, 0.26f, 1f);

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (canvasGo == null) return;

        // 카드 위치 갱신 (가지 높이는 카메라·줌·갈림길에 따라 변한다) - 고른 뒤에도 출발까지
        LayoutCards();
        if (!ChoicePending) return;

        // 다른 창이 떠 있으면 키를 안 받는다 (숫자키가 증강·행상인과 겹치지 않게)
        if (BriefingUI.IsOpen || BriefingUI.KeyConsumedFrame == Time.frameCount || PauseMenu.IsOpen
            || AugmentPickUI.IsOpen || WorkshopUI.IsOpen || AugmentListUI.ReadingOpen
            || CookingMinigame.IsActive || KitchenPanel.IsOpenStatic || SpinoBetUI.IsOpen || MerchantUI.IsOpen)
            return;

        for (int i = 0; i < shownRoutes.Count && i < 3; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                Choose(shownRoutes[i]);
                return;
            }
        }

        // 고르기 전 [Enter]: 출발 못 한다고 알린다 (2초에 한 번)
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && Time.unscaledTime - enterNagAt > 2f)
        {
            enterNagAt = Time.unscaledTime;
            UIManager.Instance?.ShowStatChange("[분기 선로] 먼저 길을 골라라 - [1~" + shownRoutes.Count + "] 또는 왼쪽 카드");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (canvasGo != null) Destroy(canvasGo);
        ChoicePending = false; Chosen = null;
    }

    // ─────────────────────────────────────────────
    // 선로 후보 생성
    // ─────────────────────────────────────────────
    private List<RouteData> BuildRoutes(int nextWave)
    {
        List<RouteData> routes = new List<RouteData>();

        // 곧은 선로 (항상)
        RouteData straight = new RouteData();
        straight.id = "straight";
        straight.routeName = "곧은 선로";
        straight.desc = "규칙 없음";
        straight.rewardDesc = "보상 없음";
        routes.Add(straight);

        // 보스 직전: 위험 선로와 2택만 (보스전 변수 최소화)
        if (GameBalance.IsBossWave(nextWave))
        {
            routes.Add(MakeDanger());
            return routes;
        }

        // 일반 웨이브: 후보 풀에서 2개
        List<RouteData> pool = new List<RouteData>();
        pool.Add(MakeDanger());

        RouteData hunt = new RouteData();
        hunt.id = "hunt";
        hunt.routeName = "사냥터 선로";
        hunt.desc = "손님 +20%";
        hunt.rewardDesc = "끝나면 증강 1회 더 (은)";
        hunt.countMul = 1.2f;
        hunt.extraPickGrade = (int)AugmentGrade.Silver;
        pool.Add(hunt);

        RouteData fog = new RouteData();
        fog.id = "fog";
        fog.routeName = "안개 선로";
        fog.desc = "손님 -25% · 사고가 일찍 온다";
        fog.rewardDesc = "끝나면 유물 " + Mathf.RoundToInt(GameBalance.RouteFogRelic * 100f) + "%";
        fog.countMul = 0.75f;
        fog.earlyEvent = true;
        fog.relicChance = GameBalance.RouteFogRelic;
        pool.Add(fog);

        while (routes.Count < 3 && pool.Count > 0)
        {
            int idx = Random.Range(0, pool.Count);
            routes.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        // 폐역: 25% 확률. 미수집 일지가 남았거나 주울 유물이 남아 있으면 마지막 칸을 대체
        bool journalLeft = MetaProgress.PickUncollectedJournal() > 0;
        if (Random.value < 0.25f && (journalLeft || ItemManager.HasStock()))
        {
            RouteData ghost = new RouteData();
            ghost.id = "ghost";
            ghost.routeName = "폐역";
            ghost.desc = "손님 -40% · 버려진 역";
            string relic = GameBalance.RouteGhostRelic >= 1f ? "유물 확정" : "유물 " + Mathf.RoundToInt(GameBalance.RouteGhostRelic * 100f) + "%";
            ghost.rewardDesc = journalLeft ? "끝나면 일지 + " + relic : "끝나면 " + relic;
            ghost.countMul = 0.6f;
            ghost.journal = journalLeft;
            ghost.relicChance = GameBalance.RouteGhostRelic;
            routes[routes.Count - 1] = ghost;
        }

        return routes;
    }

    private RouteData MakeDanger()
    {
        RouteData danger = new RouteData();
        danger.id = "danger";
        danger.routeName = "위험 선로";
        danger.desc = "손님 +40% · 강화 +15%";
        danger.rewardDesc = "금 증강 1회 + 유물 " + Mathf.RoundToInt(GameBalance.RouteDangerRelic * 100f) + "%";
        danger.countMul = 1.4f;
        danger.statMul = 1.15f;
        danger.extraPickGrade = (int)AugmentGrade.Gold;
        danger.relicChance = GameBalance.RouteDangerRelic;
        return danger;
    }

    // ─────────────────────────────────────────────
    // 표시 (WaveManager 가 정차 때 부른다)
    // ─────────────────────────────────────────────
    public void ShowRoutes(int nextWave, System.Action<RouteData> chosenCallback)
    {
        Cancel();
        onChosen = chosenCallback;
        List<RouteData> built = BuildRoutes(nextWave);

        // 화면 자리: 곧은 길 0, 첫 후보 = 위 가지(+1), 둘째 후보 = 아래 가지(-1). 위 -> 아래 순으로 번호를 붙인다
        RouteData up = null, straight = null, down = null;
        for (int i = 0; i < built.Count; i++)
        {
            if (built[i].id == "straight") { built[i].sign = 0; straight = built[i]; }
            else if (up == null) { built[i].sign = 1; up = built[i]; }
            else { built[i].sign = -1; down = built[i]; }
        }
        shownRoutes = new List<RouteData>();
        if (up != null) shownRoutes.Add(up);
        if (straight != null) shownRoutes.Add(straight);
        if (down != null) shownRoutes.Add(down);

        ChoicePending = true;
        Chosen = null;

        // 갈림길 + 정차 카메라
        bool fork = ParallaxBackground.PlaceFork(up != null, down != null);
        if (fork) CameraZoom.SetRouteFraming(true);

        BuildCanvas();
        LayoutCards();
        UIManager.Instance?.ShowWaveNotice("[분기 선로]  다음 길을 골라라",
            "[1~" + shownRoutes.Count + "] 또는 왼쪽 카드 클릭  -  고른 뒤 [Enter] 출발");
        Debug.Log("[분기선로] 후보 " + shownRoutes.Count + " (갈림길 " + (fork ? "놓음" : "없음") + ", 웨이브 " + nextWave + ")");
    }

    /// <summary>카드 캔버스 (HUD 위, 정비소·카드 창 아래)</summary>
    private void BuildCanvas()
    {
        canvas = UIFactory.CreateCanvas("BranchRouteCanvas", 400);
        canvasGo = canvas.gameObject;
        cards.Clear();

        for (int i = 0; i < shownRoutes.Count; i++)
        {
            RouteData route = shownRoutes[i];
            CardView v = new CardView();
            v.route = route;
            v.ringColor = RING_BRASS;
            v.titleColor = route.id == "ghost" ? new Color(0.7f, 0.85f, 1f)
                : route.id == "danger" ? new Color(1f, 0.55f, 0.43f)
                : route.id == "fog" ? new Color(0.78f, 0.85f, 0.95f)
                : route.id == "hunt" ? UIFactory.GOLD
                : UIFactory.CREAM;

            v.root = UIFactory.CreateCard(canvasGo.transform, "Card_" + route.id,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(CARD_X, 0f), new Vector2(CARD_X + CARD_W, CARD_H), v.ringColor);
            v.root.pivot = new Vector2(0f, 0f);
            v.root.anchoredPosition = new Vector2(CARD_X, BOTTOM_KEEP);
            v.root.sizeDelta = new Vector2(CARD_W, CARD_H);
            v.ring = v.root.GetComponent<Image>();

            v.title = MakeLine(v.root, "Title", "[" + (i + 1) + "] " + route.routeName, 18, v.titleColor, -22f);
            v.risk = MakeLine(v.root, "Risk", route.desc, 14, UIFactory.CREAM, -46f);
            v.reward = MakeLine(v.root, "Reward", route.rewardDesc, 14, UIFactory.GOLD, -66f);

            // 클릭 = 고르기 (테두리 이미지가 받는다)
            Button btn = v.root.gameObject.AddComponent<Button>();
            btn.targetGraphic = v.ring;
            btn.onClick.AddListener(delegate { Choose(route); });

            // 이음선 + 점 (카드 오른쪽 가운데 -> 그 선로)
            v.leader = MakeBar(canvasGo.transform, "Leader_" + route.id, 2f, out v.leaderImg);
            v.dot = MakeBar(canvasGo.transform, "Dot_" + route.id, 8f, out v.dotImg);
            v.dot.sizeDelta = new Vector2(8f, 8f);
            v.dot.pivot = new Vector2(0.5f, 0.5f);

            cards.Add(v);
        }
    }

    private static Text MakeLine(RectTransform parent, string name, string content, int size, Color color, float y)
    {
        Text t = UIFactory.CreateText(parent, name, content, size, color, TextAnchor.MiddleLeft);
        RectTransform rt = t.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(16f, y);
        rt.sizeDelta = new Vector2(CARD_W - 24f, 22f);
        return t;
    }

    /// <summary>얇은 막대 이미지 (이음선·점). 왼쪽 가운데 피벗 - 회전으로 방향을 잡는다</summary>
    private static RectTransform MakeBar(Transform parent, string name, float thick, out Image img)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(10f, thick);
        img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.color = RING_BRASS;
        return rt;
    }

    /// <summary>카드를 각 가지가 화면 왼쪽 끝을 지나는 높이에 (HP 판·하단 바 사이로 클램프) + 이음선</summary>
    private void LayoutCards()
    {
        Camera cam = Camera.main;
        if (cam == null || canvas == null) return;
        float sf = Mathf.Max(0.01f, canvas.scaleFactor);
        RectTransform canvasRt = canvas.GetComponent<RectTransform>();
        float canvasH = canvasRt != null && canvasRt.rect.height > 1f ? canvasRt.rect.height : 1080f;
        float minY = BOTTOM_KEEP;                        // 카드 아래쪽 끝의 범위 (하단 바 위 ~ HP 판 아래)
        float maxY = Mathf.Max(minY, canvasH - TOP_KEEP - CARD_H);

        // 카드 오른쪽 끝 + 이음선 자리의 월드 x (거기서 가지 높이를 읽는다)
        float dotCanvasX = CARD_X + CARD_W + LEADER_LEN;
        float dotWorldX = cam.ScreenToWorldPoint(new Vector3(dotCanvasX * sf, 0f, 0f)).x;

        for (int i = 0; i < cards.Count; i++)
        {
            CardView v = cards[i];
            float wy = ParallaxBackground.BranchY(v.route.sign, dotWorldX);
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(dotWorldX, wy, 0f));
            float dotY = sp.y / sf;
            float cardY = Mathf.Clamp(dotY - CARD_H * 0.5f, minY, maxY);
            v.root.anchoredPosition = new Vector2(CARD_X, cardY);

            // 이음선: 카드 오른쪽 가운데 -> 점
            Vector2 from = new Vector2(CARD_X + CARD_W, cardY + CARD_H * 0.5f);
            Vector2 to = new Vector2(dotCanvasX, dotY);
            Vector2 dir = to - from;
            float len = dir.magnitude;
            v.leader.anchoredPosition = from;
            v.leader.sizeDelta = new Vector2(Mathf.Max(1f, len), 2f);
            v.leader.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            v.dot.anchoredPosition = to;
        }
    }

    // ─────────────────────────────────────────────
    // 선택 처리
    // ─────────────────────────────────────────────
    private void Choose(RouteData route)
    {
        if (!ChoicePending) return;
        ChoicePending = false;
        Chosen = route;
        SoundManager.Play("sfx_ui_click");
        Debug.Log("[분기선로] 선택: " + route.routeName + " (" + (route.sign > 0 ? "위" : route.sign < 0 ? "아래" : "곧은") + ")");

        // 고른 카드 금색, 나머지 회색
        for (int i = 0; i < cards.Count; i++)
        {
            CardView v = cards[i];
            bool me = v.route == route;
            Color ring = me ? RING_GOLD : GREY;
            if (v.ring != null) v.ring.color = ring;
            v.title.color = me ? v.titleColor : GREY;
            v.risk.color = me ? UIFactory.CREAM : GREY;
            v.reward.color = me ? UIFactory.GOLD : GREY;
            if (v.leaderImg != null) v.leaderImg.color = ring;
            if (v.dotImg != null) v.dotImg.color = ring;
        }
        ParallaxBackground.SetHighlight(route.sign);

        System.Action<RouteData> cb = onChosen;
        onChosen = null;
        if (cb != null) cb(route);
    }

    /// <summary>출발 (WaveManager - 웨이브 시작 직전): 카드를 치우고 고른 가지로 들어간다</summary>
    public void OnDepart()
    {
        RouteData route = Chosen;
        int sign = route != null ? route.sign : 0;
        if (canvasGo != null) Destroy(canvasGo);
        canvasGo = null; canvas = null; cards.Clear();
        ChoicePending = false; Chosen = null; onChosen = null;

        CameraZoom.SetRouteFraming(false);
        ParallaxBackground.BeginLaneShift(sign);
        if (route != null) RouteFX.SetTone(route.id);
    }

    /// <summary>선택 취소 (치트 점프·런 포기·다시 표시): 카드·갈림길·카메라를 원래대로</summary>
    public void Cancel()
    {
        if (canvasGo != null) Destroy(canvasGo);
        canvasGo = null; canvas = null; cards.Clear();
        bool was = ChoicePending || Chosen != null;
        ChoicePending = false; Chosen = null; onChosen = null;
        if (was)
        {
            CameraZoom.SetRouteFraming(false);
            ParallaxBackground.CancelFork();
        }
    }
}
