using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// [FameShopUI.cs] v1.11 (v9.35 2026-10-25 유저 "하드모드에는 새로운 명성상점 같은 거 도입해서 조금씩 포탑레벨제한을 올려도": 하드가 열리면 맨 아래 잠긴 줄 자리에 '화덕 증축' -
///   하드 운행의 포탑 최대 레벨 +20 씩 4단계 (80 -> 160), 값 300 / 600 / 900 / 1200 명성. 붉은 줄. GameBalance.HardShopOn·HardCap*) / v1.10 (v9.34 2026-10-25: 로비에서 펼친 상점은 [ESC] 로도 접는다 - 예전엔 ESC 에 일시정지 창이 대신 열렸다 (PauseMenu 가 LobbyOpen 을 본다). 결과 화면(패배·승리)의 상점은 결과 화면이라 그대로) / v1.9 (v9.23 2026-10-14 요구사항 점검 1절 3번 (08-21 결정 4-A): 맨 아래 잠긴 줄 "심장이 아직 기억해내지 못한 것들" - 기차 실루엣 칸 + ??? 칸 (살 수 없다, 다음에 올 것의 자리). 줄 간격 82 -> 70 · 줄 높이 72 -> 64 로 줄여 패널 크기 그대로. GameBalance.FameShopSilhouette) / v1.8 (v9.21.2 2026-10-14 문구 팩: 용어 통일 - 접시->요리(개) / 데미지->피해 / 슬롯->칸 / 랜덤->무작위 / 레시피->요리 / 적·아군->손님 / 드랍->드롭 / 마법 저항->저항 / 힐러·러시·청구 / 진화 조리 판 이름) / v1.7 (v9.18 2026-10-06: UI 배율 1.2 에서 결과 화면의 대사 띠와 패널 윗부분이 겹치던 것 - 패널·재출발 버튼을 모자란 여백만큼 내린다) / v1.6 (v9.17 2026-10-06 화면 손맛 2차 - 결과 화면: 운행이 끝나 열리면 머리글이 결과("기차가 멈췄다" / "종착역 도착!")로 바뀌고, 보유 명성이 이번 운행 몫만큼 세어 올라가고(C5), 상품 줄이 위에서부터 하나씩 나타난다(D4). 열릴 때 판 팝. 재출발은 검정 페이드 뒤(D6)) / v1.5 (v9.15.1 2026-09-29 스토리 개정: 재출발 버튼 "다시 굽는다" -> "다시 출발한다 - 비상 복구 끝" (셰프 재생 설정 삭제)) / v1.4 (v9.14 2026-09-28: 명성 사용처 "출발 증강" 줄 추가, 패널 680) / v1.3 (2026-09-14: 런 종료 화면에 이번 런 통계 한 줄) / v1.2 (즉시 재출발 버튼) / v1
/// 명성 상점 - 런 사이(로비/게임오버)에 명성을 소모해 영구 업그레이드를 사는 UI.
///
/// - v1.3 변경점 (스위치 실험 관찰 시트): 게임오버/승리로 열렸을 때 보유 명성 줄 아래에
///   MetaProgress.RunStatsLine() (클리어 웨이브 / 조리·실패 / 전투 중 수리 / 과열 횟수·정지 시간) 을 띄운다.
///   게임오버 알림은 3초 만에 사라져서 옮겨 적을 수 없었던 것 - 이 창은 [다시 굽는다] 를 누를 때까지 남는다. 로비에서는 빈 줄.
/// 하데스의 '어둠의 거울' 포지션: 죽어도 명성은 남고, 그걸로 다음 런을 강하게 만든다.
///
/// 사용법:
///  1) 이 파일을 Assets/Prototype 폴더에 넣는다
///  2) 하이어라키의 아무 오브젝트(예: UIManager가 붙은 오브젝트)에 AddComponent
///  3) 씬 배치 필요 없음 - UI는 전부 코드로 생성된다
///
/// 동작:
///  - 로비 / 게임오버 상태에서 자동으로 표시, 전투 시작하면 자동으로 숨김
///  - M 키로 접기/펼치기 (로비, 게임오버에서만)
///  - 업그레이드 효과는 이미 GameManager/TrainManager/CookingMinigame에 연결되어 있어
///    구매 즉시(다음 런부터) 적용된다
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class FameShopUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    /// <summary>v1.10 (v9.34): 로비에서 펼쳐져 있나 (결과 화면 상점은 아니다) - PauseMenu 가 이때 ESC 로 일시정지를 열지 않는다</summary>
    public static bool LobbyOpen
    {
        get { return IsOpen && GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Lobby; }
    }

    // ─────────────────────────────────────────────
    // 상품 정의
    // 가격은 baseCost * (현재레벨 + 1) - 레벨이 오를수록 비싸진다
    // ─────────────────────────────────────────────
    private class ShopItem
    {
        public string id;        // MetaProgress 저장 키
        public string itemName;  // 표시 이름
        public string desc;      // 효과 설명
        public int baseCost;     // 1레벨 가격
        public int maxLevel;     // 최대 레벨
        public bool hardOnly;    // v1.11: 하드가 열린 뒤에만 보이는 줄 (화덕 증축 - 잠긴 줄 자리)

        public ShopItem(string id, string itemName, string desc, int baseCost, int maxLevel)
        {
            this.id = id; this.itemName = itemName; this.desc = desc;
            this.baseCost = baseCost; this.maxLevel = maxLevel;
        }

        public int CostAt(int level) { return baseCost * (level + 1); }
    }

    private ShopItem[] items;

    // ── UI 참조 ──
    private GameObject canvasGo;
    private GameObject root;
    private Text fameText;
    private Text titleText;               // v1.6: 머리글 (운행이 끝나 열리면 결과 문구)
    private RectTransform[] rows;         // v1.6: 상품 줄 (순차 등장용)
    // v1.6 (C5): 보유 명성 세어 올리기 - 운행이 끝나고 처음 열릴 때 한 번
    private bool fameCounted = false;
    private float fameCountT = -1f;       // 0 이상 = 세는 중 (실시간 초)
    private int fameFrom = 0, fameTo = 0, fameShown = -1;
    private bool runEndFeelPending = false;   // v1.6: 운행 끝 연출(줄 순차 등장·명성 세기)이 화면 덮개가 걷히기를 기다리는 중
    private bool fameCountWanted = false;
    private Text runText;      // v1.3: 이번 런 통계 (게임오버/승리에서만 내용 있음)
    private Text[] levelTexts;
    private Text[] descTexts;            // v1.11: 줄 설명 (화덕 증축은 단계마다 글이 바뀐다)
    private int hardIndex = -1;          // v1.11: 화덕 증축 줄 번호 (없으면 -1)
    private RectTransform lockedRow;     // v1.11: 잠긴 줄 - 화덕 증축이 보이면 숨는다 (같은 자리)
    private Text[] buyLabels;
    private Button[] buyButtons;
    private GameObject restartButtonGo;   // v1.2: [다시 굽는다] - 게임오버/승리 시에만 표시

    // 표시 상태 추적
    // 플레이테스트 픽스: 로비에서는 기본 접힘 - 시작 화면을 가리지 않는다.
    // 로비의 [명성 상점] 버튼이나 M 키로 열어 본다. (사망/승리 시에는 자동으로 펼쳐진다)
    private bool userCollapsed = true;
    private GameManager.GameState lastSeenState = GameManager.GameState.Lobby;

    /// <summary>로비 버튼(LobbyUI)이 접기/펼치기를 호출할 수 있게 공개</summary>
    public static FameShopUI Instance { get; private set; }

    public void ToggleShop() { userCollapsed = !userCollapsed; }

    private void Awake()
    {
        Instance = this;
    }

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────
    private void Start()
    {
        items = new ShopItem[]
        {
            new ShopItem("gold",  "두둑한 전대",   "시작 골드 +100",             80,  3),
            new ShopItem("hp",    "강화 보일러",   "기차 최대 HP +50",           100, 3),
            new ShopItem("food",  "여분의 도시락", "시작 요리 +1 (첫 포탑 가속)", 120, 2),
            new ShopItem("mat",   "재료 가방",     "시작 시 무작위 재료 +2",        100, 2),
            new ShopItem("judge", "셰프의 감각",   "조리에서 맞춰야 하는 구간 +4% (다음 운행에도)",     150, 3),
            new ShopItem("augment", "출발 증강",   "운행을 시작할 때 은 증강을 하나 고른다 (매 운행)", GameBalance.FameStartAugmentCost, 1),   // v1.4 (v9.14): 명성 사용처
        };
        // v1.11 (v9.35): 하드 명성 상점 '화덕 증축' - 맨 끝에 (BuildUI 가 잠긴 줄 자리에 놓고, Refresh 가 하드가 열렸을 때만 보인다)
        if (GameBalance.HardShopOn)
        {
            ShopItem hard = new ShopItem("hardcap", "화덕 증축", HardCapDesc(0), Mathf.Max(1, GameBalance.HardCapCost), Mathf.Max(1, GameBalance.HardCapMaxSteps));
            hard.hardOnly = true;
            System.Array.Resize(ref items, items.Length + 1);
            items[items.Length - 1] = hard;
        }

        BuildUI();
        root.SetActive(false);
        IsOpen = false;
    }

    private void OnDestroy()
    {
        if (canvasGo != null) Destroy(canvasGo);
        IsOpen = false;
    }

    // ─────────────────────────────────────────────
    // 표시 조건: 로비 또는 게임오버 상태
    // ─────────────────────────────────────────────
    private void Update()
    {
        bool allowed = false;
        if (GameManager.Instance != null)
        {
            GameManager.GameState s = GameManager.Instance.currentState;
            allowed = (s == GameManager.GameState.Lobby
                || s == GameManager.GameState.GameOver
                || s == GameManager.GameState.Victory);

            // 플레이테스트 픽스: 사망/승리 화면에 들어온 순간엔 자동으로 펼친다
            // (죽은 직후가 명성을 쓸 가장 뜨거운 순간 - 로비 기본 접힘과 별개)
            if (s != lastSeenState)
            {
                if (s == GameManager.GameState.GameOver || s == GameManager.GameState.Victory)
                    userCollapsed = false;
                else if (s == GameManager.GameState.Lobby)
                    userCollapsed = true;
                lastSeenState = s;
            }
        }

        // M 키로 접기/펼치기
        if (allowed && Input.GetKeyDown(KeyCode.M))
            userCollapsed = !userCollapsed;

        // v1.10 (v9.34): 로비에서는 [ESC] 로도 접는다. 같은 프레임에 다른 창이 ESC 를 먼저 썼거나(공용 스탬프) 위에 다른 창(일시정지·설정·일지·훈련장·도감)이 떠 있으면 양보 - 한 번 누르면 하나만 닫힌다
        if (allowed && !userCollapsed && GameManager.Instance.currentState == GameManager.GameState.Lobby
            && Input.GetKeyDown(KeyCode.Escape) && CookingMinigame.EscConsumedFrame != Time.frameCount
            && !LobbyUI.EscTakenAbove && !LobbyUI.CollectionOpen)
        {
            userCollapsed = true;
            CookingMinigame.EscConsumedFrame = Time.frameCount;
        }

        bool shouldShow = allowed && !userCollapsed;
        if (root.activeSelf != shouldShow)
        {
            root.SetActive(shouldShow);
            IsOpen = shouldShow;
            if (shouldShow)
            {
                Refresh();   // 열릴 때마다 명성/가격 갱신
                PlayOpenFeel();   // v1.6
            }
        }
        TickRunEndFeel();   // v1.6 (D4)
        TickFameCount();    // v1.6 (C5)

        // v1.2 (감사 3-E): [다시 굽는다] 버튼은 런이 끝났을 때만 (로비에서는 숨김)
        if (restartButtonGo != null && GameManager.Instance != null)
        {
            bool runEnded = GameManager.Instance.currentState == GameManager.GameState.GameOver
                || GameManager.Instance.currentState == GameManager.GameState.Victory;
            bool showRestart = runEnded && !userCollapsed;
            if (restartButtonGo.activeSelf != showRestart)
                restartButtonGo.SetActive(showRestart);
        }
    }

    /// <summary>
    /// v1.2 (감사 3-E): 즉시 재출발 - 죽음이 가장 뜨거운 재도전 욕구의 순간.
    /// PauseMenu의 런 포기와 같은 방식: GameManager 파괴 후 씬 리로드 (DontDestroyOnLoad 잔재 방지)
    /// </summary>
    private void RestartRun()
    {
        // v1.6 (D6): 검정 페이드로 덮은 뒤 다시 싣는다. 덮는 중에 또 눌러도 한 번만 (SceneFadeSec 0 = 바로)
        ScreenFx.Cover(Color.black, GameBalance.SceneFadeSec, GameBalance.SceneFadeSec, delegate
        {
            if (GameManager.Instance != null) GameManager.Instance.ReleaseFreeze();   // 패배 뒤 멈춰 둔 세상을 먼저 푼다
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
                Destroy(GameManager.Instance.gameObject);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
    }

    // ─────────────────────────────────────────────
    // v1.6: 열릴 때 연출
    // ─────────────────────────────────────────────
    private static bool RunEnded()
    {
        return GameManager.Instance != null
            && (GameManager.Instance.currentState == GameManager.GameState.GameOver
                || GameManager.Instance.currentState == GameManager.GameState.Victory);
    }

    /// <summary>
    /// 판 팝. 운행이 끝나 열린 것이면 (한 번만): 상품 줄을 숨기고 보유 명성을 세기 전 값으로 돌려 둔 뒤, TickRunEndFeel 이 시작한다.
    /// 바로 시작하지 않는 이유 - 최종 승리 땐 흰 화면이 아직 덮여 있어, 그 밑에서 줄 등장과 세기가 거의 끝나 버린다
    /// </summary>
    private void PlayOpenFeel()
    {
        ModalFeel.Play(root.transform);
        if (!RunEnded() || fameCounted) return;
        fameCounted = true;

        if (rows != null && CardFeel.Active(GameBalance.ShopRowStaggerSec))
            for (int i = 0; i < rows.Length; i++)
                if (rows[i] != null) CardFeel.Group(rows[i]).alpha = 0f;

        int gained = MetaProgress.RunFame;
        fameCountWanted = GameBalance.HudCountFeelOn && GameBalance.GameFeelMaster > 0f && gained > 0 && GameBalance.FameCountSec > 0f;
        if (fameCountWanted)
        {
            fameTo = MetaProgress.Fame;
            fameFrom = Mathf.Max(0, fameTo - gained);
            fameShown = fameFrom;
            SetFameLine(fameFrom);
        }
        runEndFeelPending = true;
    }

    /// <summary>운행 끝 연출 시작: 화면 덮개가 걷힌 뒤 상품 줄이 위에서부터 하나씩(재출발 버튼은 처음부터 보인다) + 보유 명성이 이번 운행 몫만큼 세어 올라간다</summary>
    private void TickRunEndFeel()
    {
        if (!runEndFeelPending || ScreenFx.Covering) return;
        runEndFeelPending = false;

        if (!root.activeSelf)
        {
            // 그사이 접었다 ([M]) - 연출 없이 다음에 열면 그냥 보이게
            if (rows != null)
                for (int i = 0; i < rows.Length; i++)
                    if (rows[i] != null) CardFeel.Group(rows[i]).alpha = 1f;
            return;
        }
        if (rows != null)
        {
            // v1.11: 꺼진 줄(하드 전의 화덕 증축 / 하드 뒤의 잠긴 줄)은 순차 등장에서 빼고 알파만 돌려 둔다 - 켜진 줄 사이에 빈 박자가 없게
            System.Collections.Generic.List<RectTransform> shown = new System.Collections.Generic.List<RectTransform>();
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null) continue;
                if (rows[i].gameObject.activeSelf) shown.Add(rows[i]);
                else CardFeel.Group(rows[i]).alpha = 1f;
            }
            CardFeel.StaggerIn(shown, GameBalance.ShopRowStaggerSec);
        }
        if (fameCountWanted && MetaProgress.Fame == fameTo) fameCountT = 0f;
    }

    /// <summary>보유 명성 세어 올리기 (실시간 - 패배 뒤엔 시간이 멈춰 있다). 그사이 명성을 쓰면(구매) 세기를 끝내고 실제 값으로</summary>
    private void TickFameCount()
    {
        if (fameCountT < 0f) return;
        if (!root.activeSelf || MetaProgress.Fame != fameTo) { fameCountT = -1f; if (root.activeSelf) SetFameLine(MetaProgress.Fame); return; }
        fameCountT += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(fameCountT / GameBalance.FameCountSec);
        float e = 1f - (1f - k) * (1f - k);
        int now = k >= 1f ? fameTo : Mathf.RoundToInt(Mathf.Lerp(fameFrom, fameTo, e));
        if (now != fameShown) { fameShown = now; SetFameLine(now); }
        if (k >= 1f)
        {
            fameCountT = -1f;
            UIFeel.Bounce(fameText.rectTransform, 0.12f, 0.2f);   // 다 세면 한 번 튄다
        }
    }

    /// <summary>보유 명성 줄. 운행이 끝난 화면이면 이번 운행에서 번 몫을 괄호로</summary>
    private void SetFameLine(int fame)
    {
        string gained = RunEnded() && MetaProgress.RunFame > 0 ? " (+" + MetaProgress.RunFame + ")" : "";
        fameText.text = "보유 명성: " + fame + gained
            + "   |   최고 기록: " + MetaProgress.BestWave + "웨이브"
            + "   |   도감: " + MetaProgress.DiscoveredCount + "종";
    }

    // ─────────────────────────────────────────────
    // UI 생성 (전부 코드)
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        // 전용 캔버스 (Workshop 550 과 Augment 600 사이)
        canvasGo = new GameObject("FameShopCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 560;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIFactory.RefResolution;   // v9.14: UI 전체 배율 (GameBalance.UIScale)
        canvasGo.AddComponent<GraphicRaycaster>();

        // 반투명 배경 패널 (중앙)
        RectTransform panel = KitchenEventManager.MakeBox(canvasGo.transform, "FameShopPanel",
            new Color(0.08f, 0.06f, 0.05f, 0.94f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        // v1.7: 패널 위에는 결과 화면의 대사 띠(화면 위 끝에서 42 ~ 112)가 들어갈 여백 122 가 있어야 한다.
        //   UI 배율이 커지면 화면 반높이가 줄어 여백이 모자란다 (1.12 = 122 / 1.2 = 90) - 모자란 만큼 패널과 재출발 버튼을 같이 내린다 (1.2 = 32).
        //   1.2 를 넘기면 재출발 버튼이 화면 아래로 나가기 시작한다 - 그땐 패널 높이를 줄여야 한다
        float topLift = Mathf.Max(0f, 122f - (UIFactory.RefResolution.y * 0.5f - 360f));
        panel.anchoredPosition = new Vector2(0f, 20f - topLift);
        panel.sizeDelta = new Vector2(860f, 680f);   // v1.4: 6번째 줄 (구 600)
        root = panel.gameObject;

        // 제목
        Text title = KitchenEventManager.MakeText(panel, "Title",
            "명성 상점 - 황야의 전설", 32, new Color(1f, 0.78f, 0.32f));
        SetTopStretch(title.rectTransform, -14f, 40f);
        titleText = title;   // v1.6

        // 보유 명성
        fameText = KitchenEventManager.MakeText(panel, "Fame", "", 24,
            new Color(0.95f, 0.9f, 0.6f));
        SetTopStretch(fameText.rectTransform, -58f, 30f);

        // v1.3: 이번 런 통계 한 줄 (보유 명성 줄과 첫 상품 행 사이의 빈 띠 - 첫 행 윗변은 패널 중심 +166)
        runText = KitchenEventManager.MakeText(panel, "RunStats", "", 17,
            new Color(0.7f, 0.9f, 0.75f));
        SetTopStretch(runText.rectTransform, -90f, 26f);

        // 상품 목록
        int count = items.Length;
        levelTexts = new Text[count];
        descTexts = new Text[count];   // v1.11
        buyLabels = new Text[count];
        buyButtons = new Button[count];
        rows = new RectTransform[count];   // v1.6

        // v1.9: 잠긴 줄이 들어갈 자리 - 줄 간격·높이를 줄여 패널 크기를 그대로 둔다 (첫 줄 윗변 168, 잠긴 줄 아랫변 -316 = 예전 마지막 줄 아랫변)
        bool silhouette = GameBalance.FameShopSilhouette;
        bool compact = silhouette || HasHardRow();   // v1.11: 화덕 증축 줄도 같은 일곱째 자리를 쓴다
        float rowY = compact ? 136f : 130f;
        float rowStep = compact ? 70f : 82f, rowH = compact ? 64f : 72f;
        for (int i = 0; i < count; i++)
        {
            // 클로저 캡처용 지역 변수 (for 변수 직접 캡처 금지)
            int index = i;

            RectTransform row = KitchenEventManager.MakeBox(panel, "Row_" + items[i].id,
                items[i].hardOnly ? new Color(0.24f, 0.09f, 0.06f, 0.92f) : new Color(0.16f, 0.13f, 0.1f, 0.9f));   // v1.11: 화덕 증축 = 붉은 줄 (하드)
            row.anchorMin = new Vector2(0.5f, 0.5f);
            row.anchorMax = new Vector2(0.5f, 0.5f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.anchoredPosition = new Vector2(0f, rowY);
            row.sizeDelta = new Vector2(800f, rowH);
            if (items[i].hardOnly) hardIndex = i;   // v1.11: 화덕 증축은 잠긴 줄 자리 (맨 끝이라 아래 잠긴 줄과 같은 y) - 다음 줄로 안 내려간다
            else rowY -= rowStep;
            rows[i] = row;

            // 이름 (좌측 상단)
            Text nameText = KitchenEventManager.MakeText(row, "Name", items[i].itemName, 23,
                new Color(1f, 0.92f, 0.8f));
            nameText.alignment = TextAnchor.MiddleLeft;
            RectTransform nRt = nameText.rectTransform;
            nRt.anchorMin = new Vector2(0f, 0.5f);
            nRt.anchorMax = new Vector2(0f, 0.5f);
            nRt.pivot = new Vector2(0f, 0.5f);
            nRt.anchoredPosition = new Vector2(18f, 14f);
            nRt.sizeDelta = new Vector2(300f, 30f);

            // 설명 (좌측 하단)
            Text descText = KitchenEventManager.MakeText(row, "Desc", items[i].desc, 18,
                new Color(0.75f, 0.72f, 0.65f));
            descText.alignment = TextAnchor.MiddleLeft;
            descTexts[i] = descText;   // v1.11
            RectTransform dRt = descText.rectTransform;
            dRt.anchorMin = new Vector2(0f, 0.5f);
            dRt.anchorMax = new Vector2(0f, 0.5f);
            dRt.pivot = new Vector2(0f, 0.5f);
            dRt.anchoredPosition = new Vector2(18f, -14f);
            dRt.sizeDelta = new Vector2(420f, 26f);

            // 레벨 표시 (중앙 우측)
            levelTexts[i] = KitchenEventManager.MakeText(row, "Level", "", 21,
                new Color(0.6f, 0.85f, 0.95f));
            RectTransform lRt = levelTexts[i].rectTransform;
            lRt.anchorMin = new Vector2(1f, 0.5f);
            lRt.anchorMax = new Vector2(1f, 0.5f);
            lRt.pivot = new Vector2(1f, 0.5f);
            lRt.anchoredPosition = new Vector2(-190f, 0f);
            lRt.sizeDelta = new Vector2(120f, 30f);

            // 구매 버튼 (우측)
            buyButtons[i] = KitchenEventManager.MakeButton(row, "구매",
                new Color(0.55f, 0.35f, 0.12f), new Vector2(310f, 0f), new Vector2(150f, 50f));
            buyLabels[i] = buyButtons[i].GetComponentInChildren<Text>();
            buyButtons[i].onClick.AddListener(delegate { OnBuy(index); });
        }

        if (silhouette)
        {
            // v1.9: 잠긴 줄도 운행 끝 연출(줄이 위에서부터 하나씩)에 같이 - 맨 끝에
            RectTransform locked = BuildLockedRow(panel, rowY, rowH);
            lockedRow = locked;   // v1.11: 화덕 증축이 보이면 숨긴다 (Refresh)
            System.Array.Resize(ref rows, rows.Length + 1);
            rows[rows.Length - 1] = locked;
        }

        // v1.2 (감사 3-E): [다시 굽는다] 버튼 - 캔버스 직속 (패널 아래)
        Button restartBtn = KitchenEventManager.MakeButton(canvasGo.transform,
            "다시 출발한다 (비상 복구 끝 - 즉시 재출발)",
            new Color(0.62f, 0.25f, 0.12f), Vector2.zero, new Vector2(340f, 58f));
        RectTransform rRt = restartBtn.GetComponent<RectTransform>();
        rRt.anchorMin = new Vector2(0.5f, 0.5f);
        rRt.anchorMax = new Vector2(0.5f, 0.5f);
        rRt.pivot = new Vector2(0.5f, 0.5f);
        rRt.anchoredPosition = new Vector2(0f, -372f - topLift);   // 상점 패널 바로 아래 (v1.4: 패널 680, v1.7: 패널과 같이 내려간다)
        restartBtn.onClick.AddListener(RestartRun);
        restartButtonGo = restartBtn.gameObject;
        restartButtonGo.SetActive(false);

        // 하단 안내
        Text hint = KitchenEventManager.MakeText(panel, "Hint",
            "명성은 웨이브를 클리어할 때마다 쌓이고, 죽어도 잃지 않는다.  [M] 접기/펼치기", 17,
            new Color(0.6f, 0.58f, 0.52f));
        RectTransform hRt = hint.rectTransform;
        hRt.anchorMin = new Vector2(0f, 0f);
        hRt.anchorMax = new Vector2(1f, 0f);
        hRt.pivot = new Vector2(0.5f, 0f);
        hRt.anchoredPosition = new Vector2(0f, 12f);
        hRt.sizeDelta = new Vector2(0f, 26f);
    }

    /// <summary>
    /// v1.9: 잠긴 줄 - 왼쪽 글 "심장이 아직 기억해내지 못한 것들" + 오른쪽 어두운 칸 둘 (기차 실루엣 = 맞수 열차 그림을 검게 / ???). 누를 수 없다.
    /// 다음에 열릴 것(기차 종류 등 - 로드맵 다회차 동기)의 자리를 미리 보여 준다
    /// </summary>
    private RectTransform BuildLockedRow(RectTransform panel, float y, float h)
    {
        RectTransform row = KitchenEventManager.MakeBox(panel, "Row_locked", new Color(0.1f, 0.08f, 0.07f, 0.9f));
        row.anchorMin = new Vector2(0.5f, 0.5f); row.anchorMax = new Vector2(0.5f, 0.5f); row.pivot = new Vector2(0.5f, 0.5f);
        row.anchoredPosition = new Vector2(0f, y);
        row.sizeDelta = new Vector2(800f, h);

        Text cap = KitchenEventManager.MakeText(row, "Caption", "심장이 아직 기억해내지 못한 것들", 19, new Color(0.62f, 0.58f, 0.52f));
        cap.alignment = TextAnchor.MiddleLeft;
        RectTransform cRt = cap.rectTransform;
        cRt.anchorMin = new Vector2(0f, 0.5f); cRt.anchorMax = new Vector2(0f, 0.5f); cRt.pivot = new Vector2(0f, 0.5f);
        cRt.anchoredPosition = new Vector2(18f, 0f);
        cRt.sizeDelta = new Vector2(330f, 30f);

        // 칸 둘 (오른쪽부터): ??? / 기차 실루엣
        for (int k = 0; k < 2; k++)
        {
            RectTransform tile = KitchenEventManager.MakeBox(row, "Locked" + k, new Color(0.04f, 0.035f, 0.03f, 0.95f));
            tile.anchorMin = new Vector2(1f, 0.5f); tile.anchorMax = new Vector2(1f, 0.5f); tile.pivot = new Vector2(1f, 0.5f);
            tile.anchoredPosition = new Vector2(-12f - k * 212f, 0f);
            tile.sizeDelta = new Vector2(200f, h - 12f);
            Outline o = tile.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.35f, 0.3f, 0.24f, 0.9f);
            if (k == 1)
            {
                Sprite train = SpriteBank.Get("boss_original");   // 기차 모양 그림 (검게 칠해 실루엣으로)
                if (train != null)
                {
                    GameObject g = new GameObject("Silhouette");
                    g.transform.SetParent(tile, false);
                    RectTransform gr = g.AddComponent<RectTransform>();
                    gr.anchorMin = Vector2.zero; gr.anchorMax = Vector2.one;
                    gr.offsetMin = new Vector2(10f, 8f); gr.offsetMax = new Vector2(-10f, -8f);
                    gr.localRotation = Quaternion.Euler(0f, 0f, 180f);   // 그림은 오른쪽을 보고 저장돼 있다 - 내 기차처럼 머리가 왼쪽
                    Image img = g.AddComponent<Image>();
                    img.sprite = train; img.preserveAspect = true; img.raycastTarget = false;
                    img.color = new Color(0f, 0f, 0f, 0.85f);
                    continue;
                }
            }
            Text q = KitchenEventManager.MakeText(tile, "Q", "? ? ?", 24, new Color(0.45f, 0.4f, 0.34f));
            RectTransform qRt = q.rectTransform;
            qRt.anchorMin = Vector2.zero; qRt.anchorMax = Vector2.one; qRt.offsetMin = Vector2.zero; qRt.offsetMax = Vector2.zero;
        }
        return row;
    }

    /// <summary>v1.11: 화덕 증축 줄이 있나 (Start 가 items 에 넣었나 - BuildUI 의 줄 간격이 이걸 본다)</summary>
    private bool HasHardRow()
    {
        for (int i = 0; i < items.Length; i++) if (items[i].hardOnly) return true;
        return false;
    }

    /// <summary>v1.11: 화덕 증축 설명 - 지금 단계의 하드 포탑 최대 레벨 -> 다음 단계 (다 사면 지금 값만). 기본·전설 상한이 다르면 "+N" 으로</summary>
    private static string HardCapDesc(int level)
    {
        int step = GameBalance.HardCapStep;
        bool maxed = level >= GameBalance.HardCapMaxSteps;
        if (GameBalance.T1MaxLevel == GameBalance.T2MaxLevel && GameBalance.T2MaxLevel > 0)
        {
            int now = GameBalance.T2MaxLevel + level * step;
            return "하드에서 포탑 최대 레벨 " + now + (maxed ? "" : " -> " + (now + step));
        }
        return "하드에서 포탑 최대 레벨 +" + (level * step) + (maxed ? "" : " -> +" + ((level + 1) * step));
    }

    /// <summary>상단에 가로로 붙는 텍스트 배치 헬퍼</summary>
    private void SetTopStretch(RectTransform rt, float y, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(0f, height);
    }

    // ─────────────────────────────────────────────
    // 구매 처리
    // ─────────────────────────────────────────────
    private void OnBuy(int index)
    {
        ShopItem item = items[index];
        int level = MetaProgress.UpgradeLevel(item.id);
        int cost = item.CostAt(level);

        if (MetaProgress.TryBuyUpgrade(item.id, cost, item.maxLevel))
        {
            UIManager.Instance?.ShowStatChange("[명성 상점] " + item.itemName + " Lv."
                + MetaProgress.UpgradeLevel(item.id) + " 구매!");
        }
        Refresh();
    }

    /// <summary>보유 명성 / 각 상품의 레벨, 가격, 버튼 상태 갱신</summary>
    private void Refresh()
    {
        fameCountT = -1f;   // v1.6: 갱신(구매 포함)하면 세던 것을 끝내고 실제 값
        SetFameLine(MetaProgress.Fame);

        // v1.3: 런이 끝난 상태로 열렸을 때만 이번 런 통계 (로비에서는 지난 런 값이 남아 있어도 안 보여 준다)
        bool runEnded = RunEnded();
        // v1.6: 머리글 = 결과. 가운데 예고("기차가 멈췄다..." + 요약 3줄)가 이 패널 위에 겹쳐 뜨던 것을 패널 안으로
        if (titleText != null)
            titleText.text = !runEnded ? "명성 상점 - 황야의 전설"
                : GameManager.Instance.currentState == GameManager.GameState.Victory ? "종착역 도착!  -  명성 상점"
                : "기차가 멈췄다  -  명성 상점";
        if (runText != null)
            runText.text = runEnded ? "이번 운행:  " + MetaProgress.RunStatsLine() : "";

        // v1.11 (v9.35): 화덕 증축 줄은 하드가 열린 뒤에만 - 같은 자리의 잠긴 줄과 번갈아 (둘 중 하나만 보인다)
        bool hardShown = hardIndex >= 0 && HardMode.Unlocked;
        if (hardIndex >= 0 && rows != null && rows[hardIndex] != null && rows[hardIndex].gameObject.activeSelf != hardShown)
            rows[hardIndex].gameObject.SetActive(hardShown);
        if (lockedRow != null && lockedRow.gameObject.activeSelf == hardShown)
            lockedRow.gameObject.SetActive(!hardShown);

        for (int i = 0; i < items.Length; i++)
        {
            int level = MetaProgress.UpgradeLevel(items[i].id);
            bool maxed = level >= items[i].maxLevel;
            levelTexts[i].text = "Lv." + level + " / " + items[i].maxLevel;
            if (items[i].hardOnly && descTexts != null && descTexts[i] != null) descTexts[i].text = HardCapDesc(level);   // v1.11: 단계마다 글

            if (maxed)
            {
                buyLabels[i].text = "완성";
                buyButtons[i].interactable = false;
            }
            else
            {
                int cost = items[i].CostAt(level);
                buyLabels[i].text = cost + " 명성";
                buyButtons[i].interactable = MetaProgress.Fame >= cost;
            }
        }
    }
}
