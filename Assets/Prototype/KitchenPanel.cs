using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// [KitchenPanel.cs] v3.6 (v9.37 2026-10-29 유저 "텍스트가 너무 많다": 머리줄 "재료" · "솥에서 되는 요리" 만 (조작 괄호 뺌, 금색 17), 빈 상자 글 "재료 둘을 올려 봐", 위 오른쪽 줄 = 정차 조리 횟수(금색) + "[1~n] 인분  [E] 조리  [ESC] 닫기" ([WASD] 는 뺌) - GameBalance.KitchenHintDiet) / v3.5 (v9.36 2026-10-29 그림 팩: 되는 요리 목록 줄 왼쪽 아이콘 30 (미발견은 어둡게) / 되는 요리 상자 이름 옆 아이콘 56 / 도감 칸 아이콘 40 / 설명 상자 오른쪽 위 아이콘 72. 전부 GameBalance.DishIconOn + 그림이 있을 때만) / v3.4 (v9.35.3 2026-10-26 UI 목업 v1 의 1·6: 창 글 말투를 해체로 ("끌어다 아래 솥에 놓아라" -> "아래 솥으로 끌어다 놔", "올려라 - 여기에 뜬다" -> "올려 - 여기 떠", 모자란다 -> 모자라 ...) · 받침 맞춤 (Ro · Eul - 솥으로 / 그릴로, 을 / 를) / RefreshIfOpen - 견습이 재료를 채우면 열린 창도 새 수로 (예전엔 창을 닫아야 보였다) / 리뷰: RefreshIfOpen 이 그렸나를 돌려준다 (못 그리면 부른 쪽이 다음 프레임에 다시) · 진화 조리 중에도 안 그린다 · "조리법은 요리마다" -> "굽기·볶기·끓이기는 요리마다" ('조리법'이 재료 조합으로 읽힌다)) / v3.3 (v9.32 2026-10-22 노선: "3인분은 지역 3부터" - 지역마다 맵이 바뀌니 맵 이름을 뺐다) / v3.2 (v9.31 2026-10-21 급식 조리: 되는 요리 상자 아래 [1] 1인분 [2] 2인분 [3] 3인분 칩 (키 1~3, 최대 인분 CookingBridge.MaxPortions - 지역 2 부터 2, 지역 3 부터 3. 1 이면 칩 없음). 고른 인분만큼 재료를 쓰고 "2개 나온다 (PERFECT 4개)", 모자란 재료 수도 인분으로. 창을 열면 1인분부터) / v3.1 (v9.26 2026-10-16 마감 정리: Tab 키 · 합성 탭(BuildFuseTab) · 옛 요리 카드 격자 조리 탭(BuildCookTab · BuildRecipeGrid) 코드 삭제 - 스위치로 꺼 두었던 것. 탭 = 조리 · 도감, 번호 0 · 1) / v3.0 (v9.22 2026-10-14 조작 팩 2-1 ~ 2-3: 조리 탭을 재료 중심으로(BuildMaterialTab - 재료 6칸을 끌어다(또는 눌러) 두 칸에 올리면 되는 요리·역할·조리법이 뜨고 정해진 조리법 하나만 불이 들어온다, 아래 "이 조리대에서 되는 요리" 목록은 누르면 두 칸이 채워진다, GameBalance.KitchenMaterialFirst) /
///   [Tab] 전체 보기는 KitchenTabKey(false) 로 끔 - 조리 창은 조리대 [E] 로만 / 합성 탭은 KitchenFuseTab(false) 로 뺌 - 요리끼리는 하단 카드 끌어다 놓기 / 키 커서 [WASD]+[E] 는 목록에서) / v2.12 (v9.21.2 2026-10-14 문구 팩: 용어 통일 - 접시->요리(개) / 데미지->피해 / 슬롯->칸 / 랜덤->무작위 / 레시피->요리 / 적·아군->손님 / 드랍->드롭 / 마법 저항->저항 / 힐러·러시·청구 / 진화 조리 판 이름) / v2.11 (v9.21.1 2026-10-13 4차 피드백 2번: 합성 탭의 접시 합성이 진화 조리 미니게임을 거친다 - 시작하면 창을 닫는다(미니게임이 보이게), 진화 조리 중엔 Tab 토글 금지. 버튼 "합성!" -> "진화 조리") / v2.10 (v9.18 2026-10-06: 창(1240x900)이 기준 화면보다 크면 화면 안으로 줄인다 - UIFactory.FitScale. UI 배율 1.2 에서 창 높이가 화면 높이와 같아져 테가 잘렸다) / v2.9 (v9.16 2026-09-29 손맛 2차 - 소리: 닫힘 sfx_ui_close. 열림은 ModalFeel) / v2.8 (v9.15 2026-09-29 2차 피드백 "마우스와 WASD 혼용": 조리 탭 카드 격자에 키 커서 - [WASD]/방향키로 고르고 [E]/[Enter] 로 조리 시작. 마우스는 그대로 (GameBalance.KitchenKeyCursor)) / v2.7 (v9.14 2026-09-28: 설명에서 맛 문구 제외) / v2.6 (v9.11.1 2026-09-22 문구: 기본/전설 요리) / v2.5 (v9.11 2026-09-22: 등장 연출 ModalFeel) / v2.4 (v9.10.1 2026-09-21: 재료 이름 MaterialNames 한 곳 / 안내줄에 정차 조리 남은 횟수(CookingBridge.StopCookHint)) / v2.3 (v9.10 2026-09-17 테스터 피드백: [ESC] 로도 닫힘(단축키로 열고 ESC 로 닫기) / 행상인·베팅·선로 창 중 Tab 금지 / 도감 카드 클릭 = 오른쪽 상세(무엇을 하나·어떤 손님에·언제, RecipeText)) / v2.2 (v9.8 재료 아이콘) / v2.1
/// 주방 창 (uGUI 코드 생성) - 조리 / 도감 2탭. 조리대 앞 [E] 로 연다 (그 조리대 요리만 - OpenForStation)
/// GameSystems 오브젝트에 부착
///
/// - v2.2: 조리 탭 상단 재료 현황 바를 "아이콘 32px + 수량" 칸 6개로 (ui_mat_*.png 있을 때만, 없으면 v2.1 글자 줄). 로직 무변경
///
/// - v2.1 (2026-09-07, "쇳냄새" 픽셀 스킨 - HUD 목업 v3 컨펌): UISkin 이 있을 때만
///   제목 "주 방" 을 파이프 위에 걸린 황동 명판으로, 닫기 힌트를 파이프 아래로, 내용 영역을 파이프 안쪽(30px)으로,
///   오른쪽 위 파이프에 압력 게이지·밸브 장식. 스킨이 없으면 v2 배치 그대로. 로직 변경 없음
///
/// - v2 변경점 (조리법 정체성):
///   1) 모든 레시피에 고유 조리법 부여 (MethodOf: 굽기/볶기/끓이기 자동 판정)
///      - 스프/정식/장판/오라/버프 계열 -> 끓이기
///      - 고기 재료 포함 또는 물리 계열 -> 굽기
///      - 나머지 마법/속성 계열 -> 볶기
///   2) 레시피 카드에 [굽기] 같은 조리법 표기
///   3) 조리법 선택 버튼 3개 제거 -> 레시피 고유 조리법으로 자동 시작
///   4) OpenForStation(method): 조리대(E키)에서 호출 - 해당 조리법 요리만 표시
///
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class KitchenPanel : MonoBehaviour
{
    public static KitchenPanel Instance { get; private set; }

    /// <summary>패널이 열려 있는지 (다른 시스템 입력 차단용)</summary>
    public static bool IsOpenStatic
    {
        get { return Instance != null && Instance.isOpen; }
    }

    /// <summary>
    /// v3.4 (v9.35.3): 창이 열려 있으면 지금 재고로 다시 그린다 - 견습(TutorialDirector)이 재료를 채운 뒤 부른다.
    /// 재료 칸은 창을 열 때의 수를 그린 것이라, 예전엔 재료가 들어와도 창을 닫았다 열어야 보였다.
    /// 돌려주는 값: true = 다시 그렸다 · 창이 닫혀 있어 그릴 것이 없다 / false = 지금은 못 그린다 (재료 카드를 끄는 중 · 조리 중 · 진화 조리 중) - 부른 쪽이 다음 프레임에 다시 부른다
    /// </summary>
    public static bool RefreshIfOpen()
    {
        KitchenPanel k = Instance;
        if (k == null || !k.isOpen) return true;
        if (k.matDragging >= 0 || CookingMinigame.IsActive || InfusingMinigame.IsActive) return false;
        k.Refresh();
        return true;
    }

    /// <summary>v3.4: 받침에 맞는 "으로/로" - 솥으로 · 팬으로 · 그릴로 · 조리대로 (ㄹ 받침은 "로")</summary>
    private static string Ro(string word)
    {
        int jong = Jong(word);
        return jong <= 0 || jong == 8 ? "로" : "으로";
    }

    /// <summary>v3.4: 받침에 맞는 "을/를" (한글로 안 끝나면 "을(를)")</summary>
    private static string Eul(string word)
    {
        int jong = Jong(word);
        if (jong < 0) return "을(를)";
        return jong == 0 ? "를" : "을";
    }

    /// <summary>마지막 글자의 받침 번호 (0 = 받침 없음, 8 = ㄹ, -1 = 한글 아님)</summary>
    private static int Jong(string word)
    {
        if (string.IsNullOrEmpty(word)) return -1;
        char c = word.TrimEnd()[word.TrimEnd().Length - 1];
        if (c < '\uAC00' || c > '\uD7A3') return -1;
        return (c - 0xAC00) % 28;
    }

    private Canvas canvas;
    private RectTransform root;          // 패널 전체
    private RectTransform contentArea;   // 탭 내용 영역
    private bool isOpen = false;
    private int tabIndex = 0;            // 0=조리 1=도감
    private int stationFilter = -1;      // -1=전체, 0=굽기 1=볶기 2=끓이기 (조리대에서 열면 설정)

    // 조리 상태
    private string selectedRecipe = "";
    // v2.8: 키 커서 - 조리 탭 아래 목록에 보이는 순서대로 (BuildMaterialTab 이 채운다)
    private readonly List<string> gridIds = new List<string>();

    private readonly List<GameObject> spawned = new List<GameObject>();
    private Image detailIcon;            // v3.5 (v9.36): 설명 상자 아이콘
    private Text detailText;             // v2.3: 요리 설명 상자 (조리·도감 탭 아래) - 카드에 마우스를 올리거나 클릭하면 채워진다
    private Text hintText;               // v2.4: 오른쪽 위 안내줄 - 정차 중엔 남은 조리 횟수도
    private Button[] tabButtons = new Button[2];   // 0 조리 / 1 도감

    // ── v3.0 (v9.22): 재료 중심 조리 탭 ──
    private readonly int[] potMat = { -1, -1 };                  // 두 칸에 올린 재료 (MaterialType 번호, -1 = 비었다)
    private readonly RectTransform[] potSlotRects = new RectTransform[2];
    private RectTransform potBoxRect;                             // 조리대 상자 (두 칸이 아닌 곳에 놓아도 빈 칸으로 들어간다)
    private RectTransform matDragGhost;                           // 끌고 있는 재료 고스트
    private RectTransform panelRt;                                // 가운데 창 (fit 배율이 걸려 있다 - 고스트도 이 아래에 둬야 같은 크기)
    private int matDragging = -1;
    private int portions = 1;                                     // v3.2 (v9.31): 고른 인분 (창을 열면 1)
    private const int LIST_COLUMNS = 4;                           // 목록 칸 수 (키 커서도 이 칸 수로 움직인다)
    private static readonly string[] STATION_NAMES = { "그릴", "팬", "솥" };   // 조리법 0/1/2 의 조리대 이름 (CookingStation 과 같은 말)

    private static readonly string[] METHOD_NAMES = { "굽기", "볶기", "끓이기" };

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        canvas = UIFactory.CreateCanvas("KitchenPanel_Canvas", 20); // HUD보다 위
        BuildFrame();
        root.gameObject.SetActive(false);
    }

    void Update()
    {
        // v2.3: 열려 있으면 [ESC] 도 닫기 (같은 프레임에 일시정지 메뉴가 열리지 않게 공용 스탬프)
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CookingMinigame.EscConsumedFrame = Time.frameCount;
            Close();
            return;
        }

        if (isOpen && tabIndex == 0 && GameBalance.KitchenKeyCursor && !CookingMinigame.IsActive) TickKeyCursor();
        if (isOpen && tabIndex == 0 && !CookingMinigame.IsActive) TickPortionKeys();   // v3.2 (v9.31): [1]~[3] 인분
    }

    // ─────────────────────────────────────────
    // v3.2 (v9.31): 급식 조리 - 인분 고르기 (칩 또는 키 1~3)
    // ─────────────────────────────────────────
    private void TickPortionKeys()
    {
        if (CookingBridge.MaxPortions() <= 1) return;
        if (matDragging >= 0) return;   // 리뷰 3: 재료를 끄는 중엔 안 받는다 (Refresh 를 못 하니 칩·개수가 어긋난다)
        if (BriefingUI.IsOpen || BriefingUI.KeyConsumedFrame == Time.frameCount) return;   // 카드가 떠 있으면 키는 카드 것
        int top = Mathf.Max(3, TableMax());   // 표를 4 로 올리면 키도 따라간다
        for (int i = 1; i <= top; i++)
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i)) { SetPortions(i); return; }
    }

    /// <summary>BatchPortionsByRegion 의 최댓값 (키 상한)</summary>
    private static int TableMax()
    {
        int[] table = GameBalance.BatchPortionsByRegion;
        int m = 1;
        if (table != null) for (int r = 0; r < table.Length; r++) if (table[r] > m) m = table[r];
        return m;
    }

    /// <summary>인분을 고른다 - 최대 인분을 넘으면 어느 지역부터 되는지 알리고 그대로</summary>
    private void SetPortions(int n)
    {
        int max = CookingBridge.MaxPortions();
        if (n > max)
        {
            UIManager.Instance?.ShowStatChange(n + "인분은 " + PortionRegionWord(n) + "부터");
            return;
        }
        n = Mathf.Clamp(n, 1, max);
        if (n == portions) return;
        portions = n;
        SoundManager.Play("sfx_ui_click");
        if (matDragging < 0) Refresh();
    }

    /// <summary>n 인분이 처음 되는 자리 (BatchPortionsByRegion 에서) - "지역 3". v3.3: 노선이 섞여 자리마다 맵이 바뀌니 이름은 안 쓴다</summary>
    private static string PortionRegionWord(int n)
    {
        int[] table = GameBalance.BatchPortionsByRegion;
        if (table != null)
            for (int r = 0; r < table.Length; r++)
                if (table[r] >= n)
                    return r >= 3 ? "최종전" : "지역 " + (r + 1);
        return "다음 지역";
    }

    /// <summary>되는 요리 상자 아래 줄: "급식 조리" + 인분 칩 (최대 인분까지) + 한 줄 설명. 최대 인분이 1 이면 아무것도 안 그린다</summary>
    private void BuildPortionChips(RectTransform res)
    {
        int max = CookingBridge.MaxPortions();
        if (max <= 1) return;   // (portions 는 BuildMaterialTab 첫머리에서 이미 깎았다)
        Text head = UIFactory.CreateText(res, "PortionHead", "급식 조리", 14, UIFactory.GOLD, TextAnchor.MiddleLeft);
        head.rectTransform.anchorMin = new Vector2(0f, 1f); head.rectTransform.anchorMax = new Vector2(0f, 1f); head.rectTransform.pivot = new Vector2(0f, 1f);
        head.rectTransform.anchoredPosition = new Vector2(20f, -134f); head.rectTransform.sizeDelta = new Vector2(84f, 30f);
        head.raycastTarget = false;
        float cx = 106f;
        for (int n = 1; n <= max; n++)
        {
            bool on = n == portions;
            bool can = true;
            if (potMat[0] >= 0 && potMat[1] >= 0 && MaterialInventory.Instance != null)
                can = MaterialInventory.Instance.CanAfford((MaterialType)potMat[0], (MaterialType)potMat[1], n);
            Button b = UIFactory.CreateButton(res, "Portion_" + n, "[" + n + "] " + n + "인분", new Vector2(80f, 30f),
                on ? new Color(0.62f, 0.42f, 0.14f) : new Color(0.27f, 0.24f, 0.22f),
                on ? UIFactory.CREAM : (can ? UIFactory.DIM : new Color(0.5f, 0.42f, 0.36f)), 14);
            RectTransform brt = b.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(0f, 1f); brt.pivot = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(cx, -134f);
            int pn = n;
            b.onClick.AddListener(delegate { SetPortions(pn); });
            cx += 86f;
        }
        Text tip = UIFactory.CreateText(res, "PortionTip", portions > 1 ? "재료 " + portions + "개씩 · 불이 세져" : "재료도 요리도 인분만큼", 13, UIFactory.DIM, TextAnchor.MiddleLeft);
        tip.rectTransform.anchorMin = new Vector2(0f, 1f); tip.rectTransform.anchorMax = new Vector2(1f, 1f); tip.rectTransform.pivot = new Vector2(0f, 1f);
        tip.rectTransform.offsetMin = new Vector2(cx + 6f, -164f); tip.rectTransform.offsetMax = new Vector2(-12f, -134f);
        tip.raycastTarget = false;
    }

    /// <summary>
    /// v2.8: 조리 탭 키 조작 - [W/S] 한 줄 위·아래, [A/D] 한 칸 좌·우 (방향키도), [E]/[Enter] 조리 시작.
    /// 마우스 클릭으로 고른 카드에서 이어서 움직인다 (같은 selectedRecipe). 재료가 모자란 카드도 고를 수는 있다(설명을 읽게) - 시작만 막는다
    /// </summary>
    private void TickKeyCursor()
    {
        if (gridIds.Count == 0) return;
        if (matDragging >= 0) return;   // v3.0 (리뷰 4-1): 재료를 끄는 중엔 Refresh 를 부르지 않는다 (끌던 카드가 지워진다)
        int dx = 0, dy = 0;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dx = -1;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dx = 1;
        else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) dy = -1;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dy = 1;

        if (dx != 0 || dy != 0)
        {
            int cur = gridIds.IndexOf(selectedRecipe);
            int next;
            if (cur < 0)
            {
                // v3.0 (리뷰 5-1): 두 칸에 뭔가 올려 둔 채면 키로 덮어쓰지 않는다 - 목록에 없는 조합(다른 조리대 요리)이거나 한 칸만 찬 상태
                if (potMat[0] >= 0 || potMat[1] >= 0) return;
                next = 0;
            }
            else
            {
                // v3.0 (리뷰 5-2): 줄 끝에서 옆으로는 안 넘어가고, 아래위는 칸이 있을 때만
                int colI = cur % LIST_COLUMNS, rowI = cur / LIST_COLUMNS;
                int nc = colI + dx, nr = rowI + dy;
                if (nc < 0 || nc >= LIST_COLUMNS) nc = colI;
                next = nr * LIST_COLUMNS + nc;
                if (nr < 0 || next >= gridIds.Count) next = cur;
            }
            if (next != cur)
            {
                selectedRecipe = gridIds[next];
                FillPotFromKey(selectedRecipe);   // v3.0: 목록에서 고르면 두 칸도 채워진다
                Refresh();
            }
            return;
        }

        bool go = Input.GetKeyDown(KeyCode.E) || ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && BriefingUI.KeyConsumedFrame != Time.frameCount);
        if (!go || string.IsNullOrEmpty(selectedRecipe)) return;
        int idx = gridIds.IndexOf(selectedRecipe);
        if (idx < 0) return;
        string[] keyParts = selectedRecipe.Split('+');   // v3.0 (리뷰 5-5): 재고는 지금 값으로 다시 본다
        bool affordNow = keyParts.Length >= 2 && MaterialInventory.Instance != null && MaterialInventory.Instance.CanAfford(ParseMat(keyParts[0]), ParseMat(keyParts[1]), portions);   // v3.2: 인분만큼
        if (!affordNow) { UIManager.Instance?.ShowStatChange(portions > 1 ? "재료가 모자라 - " + portions + "인분은 재료가 " + portions + "개씩 들어" : "재료가 모자라 - 다른 요리를 골라"); return; }
        RecipeData sel = RecipeDatabase.Get(selectedRecipe);
        if (sel == null) return;
        int m = MethodOf(sel);
        if (stationFilter >= 0 && m != stationFilter) { UIManager.Instance?.ShowStatChange(sel.displayName + " 은(는) " + STATION_NAMES[m] + "에서 " + MethodName(m)); return; }   // v3.0
        StartMinigame(m);
    }

    // ─────────────────────────────────────────
    // v3.0 (v9.22): 재료 중심 조리 탭 - 두 칸·끌어다 놓기
    // ─────────────────────────────────────────
    /// <summary>두 칸에 재료를 올린다 (slotIdx 0/1). 둘 다 차면 되는 요리가 selectedRecipe 가 된다</summary>
    private void SetPot(int slotIdx, int mat)
    {
        if (slotIdx < 0 || slotIdx > 1) return;
        potMat[slotIdx] = mat;
        SyncSelectedFromPot();
        SoundManager.Play("sfx_ui_click");
        Refresh();
    }

    /// <summary>재료를 눌렀을 때 - 빈 칸부터, 둘 다 차 있으면 둘째 칸을 바꾼다</summary>
    private void OnMaterialClicked(int mat)
    {
        if (potMat[0] < 0) SetPot(0, mat);
        else if (potMat[1] < 0) SetPot(1, mat);
        else SetPot(1, mat);
    }

    /// <summary>두 칸의 재료 -> selectedRecipe ("fire+meat" 식 키). 한 칸이라도 비면 빈 문자열</summary>
    private void SyncSelectedFromPot()
    {
        selectedRecipe = (potMat[0] >= 0 && potMat[1] >= 0) ? RecipeDatabase.MakeKey((MaterialType)potMat[0], (MaterialType)potMat[1]) : "";
    }

    /// <summary>목록의 요리를 골랐을 때 - 그 요리의 재료 둘을 두 칸에 올린다</summary>
    private void FillPotFromKey(string key)
    {
        string[] parts = key.Split('+');
        if (parts.Length < 2) return;
        potMat[0] = (int)ParseMat(parts[0]);
        potMat[1] = (int)ParseMat(parts[1]);
        selectedRecipe = key;
    }

    private void ClearPot()
    {
        potMat[0] = -1; potMat[1] = -1;
        selectedRecipe = "";
    }

    /// <summary>MaterialCardDrag 에서: 끌기 시작 - 재료 고스트(아이콘 + 이름)가 커서를 따라간다</summary>
    public void BeginMatDrag(int mat, Vector2 screenPos)
    {
        if (!isOpen || mat < 0 || mat > 5) return;
        if (MaterialInventory.Instance == null || MaterialInventory.Instance.Get((MaterialType)mat) <= 0) return;   // 리뷰 4-3: 없는 재료는 못 끈다
        EndMatDrag(Vector2.zero, false);
        matDragging = mat;
        MaterialType t = (MaterialType)mat;
        GameObject go = new GameObject("MatDragGhost");
        matDragGhost = go.AddComponent<RectTransform>();
        matDragGhost.SetParent(panelRt != null ? (Transform)panelRt : canvas.transform, false);   // 리뷰 4-2: 창과 같은 배율
        matDragGhost.SetAsLastSibling();
        matDragGhost.pivot = new Vector2(0.5f, 0.5f);
        matDragGhost.sizeDelta = new Vector2(150f, 60f);
        Image border = go.AddComponent<Image>();
        Color mc = UIFactory.TagColor(MAT_TAG[mat]);
        border.color = new Color(mc.r, mc.g, mc.b, 0.85f);
        border.raycastTarget = false;
        if (UISkin.Available) UISkin.Ring(border, border.color);
        GameObject bg = new GameObject("BG");
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.SetParent(matDragGhost, false);
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        float pad = UISkin.Available ? 8f : 2f;
        bgRt.offsetMin = new Vector2(pad, pad); bgRt.offsetMax = new Vector2(-pad, -pad);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(mc.r * 0.28f, mc.g * 0.28f, mc.b * 0.28f, 0.85f);
        bgImg.raycastTarget = false;
        if (UISkin.Available) UISkin.Plate(bgImg, new Color(1f, 1f, 1f, 0.85f));
        Image icon = UISkin.AddMaterialIcon(bgRt, t, new Vector2(0f, 0.5f), new Vector2(8f, 0f), 32f);
        Text name = UIFactory.CreateText(bgRt, "Name", MaterialNames.Kor(t), 17, UIFactory.CREAM, TextAnchor.MiddleLeft);
        name.rectTransform.offsetMin = new Vector2(icon != null ? 48f : 10f, 0f);
        name.raycastTarget = false;
        MoveMatDrag(screenPos);
    }

    /// <summary>MaterialCardDrag 에서: 매 프레임 - 고스트가 커서를 따라가고, 칸 위면 커진다</summary>
    public void MoveMatDrag(Vector2 screenPos)
    {
        if (matDragGhost == null) return;
        matDragGhost.position = new Vector3(screenPos.x + 12f, screenPos.y - 10f, 0f);
        bool over = PotSlotUnder(screenPos) >= 0 || (potBoxRect != null && RectTransformUtility.RectangleContainsScreenPoint(potBoxRect, screenPos, null));
        matDragGhost.localScale = over ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
    }

    /// <summary>MaterialCardDrag 에서: 놓기 - 두 칸 중 하나 위면 그 칸, 조리대 상자 안이면 빈 칸, 아니면 취소</summary>
    public void EndMatDrag(Vector2 screenPos, bool drop)
    {
        int mat = matDragging;
        matDragging = -1;
        if (matDragGhost != null) { Destroy(matDragGhost.gameObject); matDragGhost = null; }
        if (!drop || mat < 0 || !isOpen) return;
        if (MaterialInventory.Instance == null || MaterialInventory.Instance.Get((MaterialType)mat) <= 0) return;
        int j = PotSlotUnder(screenPos);
        if (j >= 0) { SetPot(j, mat); return; }
        if (potBoxRect != null && RectTransformUtility.RectangleContainsScreenPoint(potBoxRect, screenPos, null)) { OnMaterialClicked(mat); return; }
    }

    private int PotSlotUnder(Vector2 screenPos)
    {
        for (int j = 0; j < 2; j++)
            if (potSlotRects[j] != null && RectTransformUtility.RectangleContainsScreenPoint(potSlotRects[j], screenPos, null)) return j;
        return -1;
    }

    private static readonly FoodTag[] MAT_TAG = { FoodTag.Phys, FoodTag.Def, FoodTag.Elec, FoodTag.Fire, FoodTag.Ice, FoodTag.Poison };   // 재료 -> 계열색 (GameHUD 와 같은 짝)

    /// <summary>v3.0: 재료 중심 조리 탭 - 재료 6칸 / 조리대 두 칸 + 되는 요리·조리법 / 이 조리대에서 되는 요리 목록</summary>
    private void BuildMaterialTab()
    {
        gridIds.Clear();
        portions = Mathf.Clamp(portions, 1, CookingBridge.MaxPortions());   // v3.2 (리뷰 참고): 개수·재료 계산보다 먼저 깎는다
        string station = stationFilter >= 0 ? STATION_NAMES[stationFilter] : "조리대";
        string stationTitle = stationFilter >= 0 ? STATION_NAMES[stationFilter] + " (" + MethodName(stationFilter) + ")" : "조리대 (전체)";

        // ── 재료 6칸 (끌어다 놓거나 누른다) ──
        Text matHead = GameBalance.KitchenHintDiet
            ? UIFactory.CreateText(contentArea, "MatHead", "재료", 17, UIFactory.GOLD, TextAnchor.UpperLeft)   // v3.6: 글 줄이기 - 끌거나 누르는 건 견습이 가르친다
            : UIFactory.CreateText(contentArea, "MatHead", "재료  -  아래 " + station + Ro(station) + " 끌어다 놔 (두 개). 눌러도 빈 칸에 들어가", 15, UIFactory.DIM, TextAnchor.UpperLeft);   // v3.4: 해체
        matHead.rectTransform.anchorMin = new Vector2(0f, 1f); matHead.rectTransform.anchorMax = new Vector2(1f, 1f);
        matHead.rectTransform.offsetMin = new Vector2(4f, -26f); matHead.rectTransform.offsetMax = new Vector2(0f, -4f);
        spawned.Add(matHead.gameObject);
        bool icons = UISkin.Available && UISkin.MaterialIcon(MaterialType.Meat) != null;
        for (int i = 0; i < 6; i++)
        {
            MaterialType t = (MaterialType)i;
            int have = MaterialInventory.Instance != null ? MaterialInventory.Instance.Get(t) : 0;
            float x = 4f + i * 188f;
            Color tagC = UIFactory.TagColor(MAT_TAG[i]);
            RectTransform card = UIFactory.CreatePanel(contentArea, "MatCard_" + i,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(x, -94f), new Vector2(x + 180f, -30f),
                new Color(tagC.r * 0.2f, tagC.g * 0.2f, tagC.b * 0.2f, 0.95f), have > 0 ? tagC : new Color(0.35f, 0.3f, 0.26f), 2f);
            spawned.Add(card.gameObject);
            float labelX = 12f;
            if (icons) { UISkin.AddMaterialIcon(card, t, new Vector2(0f, 0.5f), new Vector2(10f, 0f), 36f); labelX = 54f; }
            Text nm = UIFactory.CreateText(card, "Name", MaterialNames.Kor(t), 15, have > 0 ? UIFactory.CREAM : UIFactory.DIM, TextAnchor.UpperLeft);
            nm.rectTransform.offsetMin = new Vector2(labelX, 30f); nm.rectTransform.offsetMax = new Vector2(-6f, -8f);
            Text cnt = UIFactory.CreateText(card, "Count", "x " + have, 17, have > 0 ? UIFactory.CREAM : UIFactory.DIM, TextAnchor.LowerLeft);
            cnt.rectTransform.offsetMin = new Vector2(labelX, 6f); cnt.rectTransform.offsetMax = new Vector2(-6f, -30f);
            if (have <= 0) continue;   // 리뷰 4-3: 없는 재료는 끌지도 누르지도 못한다 (흐리게만)
            int mi = i;
            Button b = card.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(delegate { OnMaterialClicked(mi); });
            MaterialCardDrag d = card.gameObject.AddComponent<MaterialCardDrag>();
            d.owner = this; d.mat = i;
        }

        // ── 조리대 상자: 두 칸 + 조리법 그림 ──
        RectTransform box = UIFactory.CreatePanel(contentArea, "PotBox",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -278f), new Vector2(560f, -108f),
            new Color(0.15f, 0.095f, 0.06f, 0.92f), UIFactory.COPPER, 2f);
        spawned.Add(box.gameObject);
        potBoxRect = box;
        Text boxTitle = UIFactory.CreateText(box, "Title", stationTitle, 17, UIFactory.GOLD, TextAnchor.UpperLeft);
        TopStrip(boxTitle, 16f, 16f, 10f, 34f);
        for (int j = 0; j < 2; j++)
        {
            int mat = potMat[j];
            bool filled = mat >= 0;
            Color mc = filled ? UIFactory.TagColor(MAT_TAG[mat]) : new Color(0.35f, 0.3f, 0.26f);
            float sx = 40f + j * 230f;
            RectTransform slot = UIFactory.CreatePanel(box, "PotSlot_" + j,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(sx, -120f), new Vector2(sx + 150f, -50f),
                new Color(0.08f, 0.06f, 0.05f, 0.95f), mc, 2f);
            spawned.Add(slot.gameObject);
            potSlotRects[j] = slot;
            if (filled)
            {
                MaterialType t = (MaterialType)mat;
                float lx = 14f;
                if (icons) { UISkin.AddMaterialIcon(slot, t, new Vector2(0f, 0.5f), new Vector2(12f, 0f), 40f); lx = 62f; }
                Text nm = UIFactory.CreateText(slot, "Name", MaterialNames.Kor(t), 18, UIFactory.CREAM, TextAnchor.MiddleLeft);
                nm.rectTransform.offsetMin = new Vector2(lx, 0f); nm.rectTransform.offsetMax = new Vector2(-6f, 0f);
                int sj = j;
                Button clear = slot.gameObject.AddComponent<Button>();
                clear.transition = Selectable.Transition.None;
                clear.onClick.AddListener(delegate { SetPot(sj, -1); });   // 누르면 내린다
            }
            else
            {
                Text hint = UIFactory.CreateText(slot, "Hint", "여기에", 16, UIFactory.DIM, TextAnchor.MiddleCenter);
                hint.rectTransform.offsetMin = new Vector2(6f, 0f); hint.rectTransform.offsetMax = new Vector2(-6f, 0f);
            }
        }
        Text plus = UIFactory.CreateText(box, "Plus", "+", 26, UIFactory.CREAM, TextAnchor.MiddleCenter);
        plus.rectTransform.anchorMin = new Vector2(0f, 1f); plus.rectTransform.anchorMax = new Vector2(0f, 1f);
        plus.rectTransform.offsetMin = new Vector2(196f, -120f); plus.rectTransform.offsetMax = new Vector2(264f, -50f);
        // 조리법 그림 (그릴 = 석쇠, 팬·솥 = 엠블럼) - 그림이 있을 때만
        if (UISkin.Available)
        {
            string emblem = stationFilter == 1 ? "ui_mg_pan" : stationFilter == 2 ? "ui_mg_pot" : "ui_mg_grate";
            if (SpriteBank.Has(emblem))
            {
                GameObject eg = new GameObject("Emblem");
                RectTransform ert = eg.AddComponent<RectTransform>();
                ert.SetParent(box, false);
                ert.anchorMin = new Vector2(0f, 1f); ert.anchorMax = new Vector2(0f, 1f); ert.pivot = new Vector2(0f, 1f);
                if (stationFilter == 1) { ert.anchoredPosition = new Vector2(450f, -56f); ert.sizeDelta = new Vector2(80f, 44f); }
                else if (stationFilter == 2) { ert.anchoredPosition = new Vector2(456f, -52f); ert.sizeDelta = new Vector2(72f, 60f); }
                else { ert.anchoredPosition = new Vector2(40f, -130f); ert.sizeDelta = new Vector2(380f, 40f); }
                Image ei = eg.AddComponent<Image>();
                ei.sprite = SpriteBank.Get(emblem); ei.type = Image.Type.Simple; ei.raycastTarget = false; ei.preserveAspect = true;
            }
        }

        // ── 되는 요리 + 조리법 칩 ──
        RectTransform res = UIFactory.CreatePanel(contentArea, "ResultBox",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(580f, -278f), new Vector2(0f, -108f),
            new Color(0.22f, 0.14f, 0.08f, 0.95f), UIFactory.GOLD, 2f);
        spawned.Add(res.gameObject);
        RecipeData r = string.IsNullOrEmpty(selectedRecipe) ? null : RecipeDatabase.Get(selectedRecipe);
        if (r == null)
        {
            string emptyMsg = potMat[0] < 0 && potMat[1] < 0 ? (GameBalance.KitchenHintDiet ? "재료 둘을 올려 봐" : "재료 둘을 " + station + "에 올려 - 되는 요리가 여기 떠")
                : (potMat[0] >= 0 && potMat[1] >= 0) ? "이 조합은 요리가 없어" : "재료 하나 더";   // 리뷰 5-3. v3.4: 해체. v3.6: 짧게
            Text t0 = UIFactory.CreateText(res, "Empty", emptyMsg, 18, UIFactory.DIM, TextAnchor.MiddleLeft);
            t0.rectTransform.offsetMin = new Vector2(20f, 0f); t0.rectTransform.offsetMax = new Vector2(-16f, 0f);
        }
        else
        {
            bool disc = FoodStock.Instance != null && FoodStock.Instance.IsDiscovered(r.recipeId);
            bool afford = MaterialInventory.Instance != null && MaterialInventory.Instance.CanAfford((MaterialType)potMat[0], (MaterialType)potMat[1], portions);   // v3.2: 인분만큼
            int m = MethodOf(r);
            Text nm = UIFactory.CreateText(res, "Name", disc ? r.displayName : "???", 24, disc ? UIFactory.GOLD : UIFactory.DIM, TextAnchor.UpperLeft);
            Image resIcon = UIFactory.CreateIcon(res, "Icon", SpriteBank.DishIcon(r.recipeId), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -8f), 56f);   // v3.5
            if (resIcon != null && !disc) resIcon.color = UIFactory.SILHOUETTE;   // 아직 못 만든 요리 = 실루엣
            TopStrip(nm, resIcon != null ? 82f : 20f, 16f, 12f, 46f);
            float nameW = disc ? r.displayName.Length * 25f + 12f : 70f;
            Text role = UIFactory.CreateText(res, "Role", disc ? RecipeText.RoleWord(r) + " · " + RecipeText.ShapeWord(r) + (afford ? " · " + portions + "개 나와 (PERFECT " + (portions * 2) + "개)" : "")
                : "만들어 보면 알아", 15, UIFactory.CREAM, TextAnchor.UpperLeft);
            TopStrip(role, 20f + nameW, 16f, 18f, 44f);
            string line2 = afford ? "굽기·볶기·끓이기는 요리마다 정해져 있어 - 불 들어온 걸 눌러 시작"
                : "재료가 모자라 - " + MissingWord((MaterialType)potMat[0], (MaterialType)potMat[1], portions) + (portions > 1 ? " (" + portions + "인분)" : "");
            Text l2 = UIFactory.CreateText(res, "Line2", line2, 13, afford ? UIFactory.DIM : new Color(1f, 0.6f, 0.48f), TextAnchor.UpperLeft);
            TopStrip(l2, 20f, 16f, 50f, 70f);
            float bx = 20f;
            for (int k = 0; k < 3; k++)
            {
                bool lit = k == m;
                bool here = stationFilter < 0 || stationFilter == m;
                bool canGo = lit && here && afford;
                string label = lit ? (here ? MethodName(k) + " 시작" : MethodName(k) + " - " + STATION_NAMES[k] + "에서") : MethodName(k);
                float bw = lit ? (here ? 150f : 190f) : 100f;
                Button b = UIFactory.CreateButton(res, "Method_" + k, label, new Vector2(bw, 40f),
                    lit ? (canGo ? new Color(0.8f, 0.28f, 0.18f) : new Color(0.45f, 0.29f, 0.15f)) : new Color(0.27f, 0.24f, 0.22f),
                    lit ? UIFactory.CREAM : UIFactory.DIM, lit ? 17 : 14);
                RectTransform brt = b.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(0f, 1f); brt.pivot = new Vector2(0f, 1f);
                brt.anchoredPosition = new Vector2(bx, -86f);
                b.interactable = canGo;
                int km = k;
                b.onClick.AddListener(delegate { StartMinigame(km); });
                bx += bw + 10f;
            }
            AttachDetailHover(res, r);
        }
        BuildPortionChips(res);   // v3.2 (v9.31): 급식 조리 인분 칩 (지역 2 부터)

        // ── 이 조리대에서 되는 요리 목록 (누르면 두 칸이 채워진다) ──
        Text listHead = GameBalance.KitchenHintDiet
            ? UIFactory.CreateText(contentArea, "ListHead", station + "에서 되는 요리", 17, UIFactory.GOLD, TextAnchor.UpperLeft)   // v3.6: 글 줄이기 (??? 는 보면 안다)
            : UIFactory.CreateText(contentArea, "ListHead", station + "에서 되는 요리  (누르면 두 칸이 채워져  ·  ??? = 아직 못 만든 요리  ·  [WASD] 고르기 [E] 시작)", 15, UIFactory.DIM, TextAnchor.UpperLeft);
        listHead.rectTransform.anchorMin = new Vector2(0f, 1f); listHead.rectTransform.anchorMax = new Vector2(1f, 1f);
        listHead.rectTransform.offsetMin = new Vector2(4f, -314f); listHead.rectTransform.offsetMax = new Vector2(0f, -292f);
        spawned.Add(listHead.gameObject);
        int col = 0, row = 0;
        foreach (RecipeData rr in RecipeDatabase.All)
        {
            if (rr.tier != 1) continue;
            if (stationFilter >= 0 && MethodOf(rr) != stationFilter) continue;
            string[] parts = rr.recipeId.Split('+');
            if (parts.Length < 2) continue;
            bool disc = FoodStock.Instance != null && FoodStock.Instance.IsDiscovered(rr.recipeId);
            bool afford = MaterialInventory.Instance != null && MaterialInventory.Instance.CanAfford(ParseMat(parts[0]), ParseMat(parts[1]));
            bool sel = selectedRecipe == rr.recipeId;
            float cx = col * 290f, cy = -318f - row * 46f;
            Color ring = sel ? UIFactory.GOLD : disc ? UIFactory.TagColor(rr.tag) : new Color(0.35f, 0.3f, 0.26f);
            RectTransform card = UIFactory.CreatePanel(contentArea, "Row_" + rr.recipeId,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(cx, cy - 38f), new Vector2(cx + 280f, cy),
                sel ? new Color(0.22f, 0.14f, 0.08f, 0.95f) : new Color(0.12f, 0.08f, 0.06f, 0.9f), ring, 2f);
            spawned.Add(card.gameObject);
            Image rowIcon = UIFactory.CreateIcon(card, "Icon", SpriteBank.DishIcon(rr.recipeId), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), 30f);   // v3.5
            if (rowIcon != null && !disc) rowIcon.color = UIFactory.SILHOUETTE;
            float matsLeft = rowIcon != null ? 38f : 10f;
            Text mats = UIFactory.CreateText(card, "Mats", MaterialNames.PairKor(rr.recipeId), 13, afford ? UIFactory.DIM : new Color(0.5f, 0.42f, 0.36f), TextAnchor.MiddleLeft);
            mats.rectTransform.offsetMin = new Vector2(matsLeft, 0f); mats.rectTransform.offsetMax = new Vector2(-120f, 0f);
            Text nmT = UIFactory.CreateText(card, "Name", disc ? rr.displayName : "???", 15, afford ? UIFactory.CREAM : UIFactory.DIM, TextAnchor.MiddleRight);
            nmT.rectTransform.offsetMin = new Vector2(120f, 0f); nmT.rectTransform.offsetMax = new Vector2(-10f, 0f);
            string key = rr.recipeId;
            Button b = card.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(delegate { FillPotFromKey(key); SoundManager.Play("sfx_ui_click"); Refresh(); });
            AttachDetailHover(card, rr);
            gridIds.Add(rr.recipeId);
            col++;
            if (col >= LIST_COLUMNS) { col = 0; row++; }
        }
    }

    /// <summary>위에 붙는 글 띠: 부모의 위 테에서 top ~ bottom 만큼 내려온 자리, 좌우 여백 left/right</summary>
    private static void TopStrip(Text t, float left, float right, float top, float bottom)
    {
        RectTransform rt = t.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(left, -bottom); rt.offsetMax = new Vector2(-right, -top);
    }

    /// <summary>모자란 재료를 말로 - "고기 1개 더" / "고기 1개 · 화염꽃 1개 더". v3.2: sets 인분이면 sets 개씩 기준</summary>
    private string MissingWord(MaterialType a, MaterialType b, int sets)
    {
        if (MaterialInventory.Instance == null) return "";
        if (sets < 1) sets = 1;
        string s = "";
        if (a == b)
        {
            int have = MaterialInventory.Instance.Get(a);
            if (have < 2 * sets) s = MaterialNames.Kor(a) + " " + (2 * sets - have) + "개";
        }
        else
        {
            int ha = MaterialInventory.Instance.Get(a), hb = MaterialInventory.Instance.Get(b);
            if (ha < sets) s = MaterialNames.Kor(a) + " " + (sets - ha) + "개";
            if (hb < sets) s += (s.Length > 0 ? " · " : "") + MaterialNames.Kor(b) + " " + (sets - hb) + "개";
        }
        return s.Length > 0 ? s + " 더" : "";
    }

    // ─────────────────────────────────────────
    // 열기 / 닫기
    // ─────────────────────────────────────────

    /// <summary>패널 열기. filter: -1=전체, 0/1/2=해당 조리법 요리만</summary>
    public void Open(int filter)
    {
        if (CookingMinigame.IsActive) return;
        if (PauseMenu.IsOpen) return;
        isOpen = true;
        stationFilter = filter;
        tabIndex = 0;
        selectedRecipe = "";
        portions = 1;   // v3.2: 창을 열면 1인분부터 (급식은 매번 고른다 - 실수로 세 몫을 태우지 않게)
        ClearPot();   // v3.0
        root.gameObject.SetActive(true);
        ModalFeel.Play(root);   // v2.5: 등장 연출
        Refresh();
    }

    /// <summary>조리대(E키)에서 호출: 해당 조리법 전용으로 열기</summary>
    public void OpenForStation(int method)
    {
        Open(Mathf.Clamp(method, 0, 2));
    }

    /// <summary>v3.6: 정차 조리 횟수 배지 글 (리치 텍스트) - "정차 중 조리 1번 남음" 금색, 다 쓰면 주황 "정차 조리 끝 - 출발하면 다시 돼", 제한이 없으면 빈 글. 조리 독 명판도 같은 뜻을 쓴다</summary>
    public static string StopCookBadge()
    {
        if (!CookingBridge.StopCookLimited) return "";
        if (GameBalance.StopCookLimit == 0) return "<color=#F08A5D>정차 중엔 조리 못 해 - 출발하면 돼</color>";
        int left = CookingBridge.StopCooksLeft;
        if (left <= 0) return "<color=#F08A5D>정차 조리 끝 - 출발하면 다시 돼</color>";
        return "<color=#FFD24D>정차 중 조리 " + left + "번 남음</color>";
    }

    public void Close()
    {
        if (isOpen) SoundManager.Play("sfx_ui_close");   // v2.9
        isOpen = false;
        root.gameObject.SetActive(false);
        selectedRecipe = "";
        stationFilter = -1;
        ClearPot();   // v3.0
        EndMatDrag(Vector2.zero, false);
    }

    // ─────────────────────────────────────────
    // 레시피 고유 조리법 판정 (조리법 정체성의 핵심)
    // ─────────────────────────────────────────

    /// <summary>레시피의 고유 조리법. 0=굽기 1=볶기 2=끓이기</summary>
    public static int MethodOf(RecipeData r)
    {
        if (r == null) return 0;

        // 픽스 2차 (플레이테스트: "스튜인데 볶기를 한다"): 이름이 조리법을 말하면
        // 이름이 최우선이다 - 유저가 읽는 그대로 조리해야 어색하지 않다.
        // (예: '절대영도 수프' 볶기 -> 끓이기 / '마비독 꼬치' 끓이기 -> 굽기)
        string n = r.displayName;
        if (!string.IsNullOrEmpty(n))
        {
            if (n.Contains("수프") || n.Contains("스튜") || n.Contains("탕")
                || n.Contains("찜") || n.Contains("진액") || n.Contains("스프"))
                return 2;   // 끓이기
            if (n.Contains("볶음") || n.Contains("볶기"))
                return 1;   // 볶기
            if (n.Contains("육포") || n.Contains("스테이크") || n.Contains("구이")
                || n.Contains("꼬치") || n.Contains("바베큐") || n.Contains("립")
                || n.Contains("철판") || n.Contains("그릴"))
                return 0;   // 굽기
        }

        // 스프/정식/장판/오라/버프 계열 -> 끓이기 (냄비)
        bool isPot = !string.IsNullOrEmpty(r.passiveType) || !string.IsNullOrEmpty(r.buffType)
            || r.shape == AttackShape.Passive || r.shape == AttackShape.Aura || r.shape == AttackShape.Field;
        if (isPot) return 2;

        // 고기 재료(T1) 또는 물리 계열(T2) -> 굽기 (그릴)
        if (r.recipeId.Contains("meat")) return 0;
        if (r.tier == 2 && r.damageType == DamageType.Phys) return 0;

        // 독 진액 계열 -> 끓이기
        if (r.tag == FoodTag.Poison) return 2;

        // 나머지 속성/마법 계열 -> 볶기 (팬)
        return 1;
    }

    /// <summary>조리법 이름 (0=굽기 1=볶기 2=끓이기)</summary>
    public static string MethodName(int method)
    {
        if (method < 0 || method >= METHOD_NAMES.Length) return "?";
        return METHOD_NAMES[method];
    }

    // ─────────────────────────────────────────
    // 프레임 구성 (1회)
    // ─────────────────────────────────────────
    private void BuildFrame()
    {
        // 어두운 배경 (클릭 차단)
        root = UIFactory.CreatePanel(canvas.transform, "KitchenRoot",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0.6f), new Color(0f, 0f, 0f, 0f), 0f);

        // 중앙 패널
        RectTransform panel = UIFactory.CreatePanel(root, "Panel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-620f, -450f), new Vector2(620f, 450f),   // v2.3: 760 -> 900 (아래 설명 상자 자리)
            UIFactory.PANEL, UIFactory.COPPER, 4f);
        float fit = UIFactory.FitScale(1240f, 900f);   // v9.18: 기준 화면(UI 배율)보다 크면 화면 안으로
        panel.localScale = new Vector3(fit, fit, 1f);
        panelRt = panel;   // v3.0

        bool skin = UISkin.Available;   // v2.1: 파이프 프레임(테 28px)일 때만 배치를 조금 옮긴다

        // 타이틀 (스킨: 파이프 위에 걸린 황동 명판 / 아니면 금색 글자)
        if (skin)
        {
            UISkin.Nameplate(panel, "Title", "주 방", 24, new Vector2(0f, 1f), new Vector2(40f, 4f));
            UISkin.AddOrnament(panel, "gauge", new Vector2(1f, 1f), new Vector2(-230f, 14f), new Vector2(56f, 56f));
            UISkin.AddOrnament(panel, "valve", new Vector2(1f, 1f), new Vector2(-166f, 10f), new Vector2(48f, 48f));
        }
        else
        {
            Text title = UIFactory.CreateText(panel, "Title", "주방", 30, UIFactory.GOLD, TextAnchor.UpperLeft);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = new Vector2(28f, -56f);
            title.rectTransform.offsetMax = new Vector2(0f, -16f);
        }

        hintText = UIFactory.CreateText(panel, "Hint", "[ESC] 닫기", 16, UIFactory.DIM, TextAnchor.UpperRight);
        Text hint = hintText;
        hint.rectTransform.anchorMin = new Vector2(0f, 1f);
        hint.rectTransform.anchorMax = new Vector2(1f, 1f);
        hint.rectTransform.offsetMin = new Vector2(0f, skin ? -60f : -46f);
        hint.rectTransform.offsetMax = new Vector2(-28f, skin ? -34f : -20f);

        // 탭 버튼: 조리 / 도감 (요리끼리 합치기는 하단 카드 끌어다 놓기 - GameHUD)
        string[] tabNames = { "조리", "도감" };
        for (int i = 0; i < tabNames.Length; i++)
        {
            int idx = i;
            Button b = UIFactory.CreateButton(panel, "Tab_" + i, tabNames[i],
                new Vector2(120f, 40f), new Color(0.35f, 0.23f, 0.13f), UIFactory.CREAM, 20);
            RectTransform brt = b.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(130f + i * 128f, -14f);
            b.onClick.AddListener(delegate { tabIndex = idx; Refresh(); });
            tabButtons[i] = b;
        }

        // 내용 영역
        GameObject contentGo = new GameObject("Content");
        contentArea = contentGo.AddComponent<RectTransform>();
        contentArea.SetParent(panel, false);
        contentArea.anchorMin = Vector2.zero;
        contentArea.anchorMax = Vector2.one;
        contentArea.offsetMin = skin ? new Vector2(30f, 30f) : new Vector2(20f, 16f);      // 스킨: 파이프(28px) 안쪽
        contentArea.offsetMax = skin ? new Vector2(-30f, -70f) : new Vector2(-20f, -66f);
    }

    // ─────────────────────────────────────────
    // 탭 내용 갱신
    // ─────────────────────────────────────────
    private void Refresh()
    {
        if (hintText != null)
        {
            if (GameBalance.KitchenHintDiet)
            {
                // v3.6: 정차 조리 횟수는 금색으로 앞에 크게, 조작은 셋만 ([WASD] 는 뺐다 - 마우스로 다 된다)
                string stop = StopCookBadge();
                hintText.text = (stop.Length > 0 ? stop + "      " : "")
                    + (tabIndex == 0 && CookingBridge.MaxPortions() > 1 ? "[1~" + CookingBridge.MaxPortions() + "] 인분   " : "")
                    + (GameBalance.KitchenKeyCursor && tabIndex == 0 ? "[E] 조리   " : "") + "[ESC] 닫기";
                hintText.supportRichText = true;
            }
            else
                hintText.text = CookingBridge.StopCookHint() + (GameBalance.KitchenKeyCursor && tabIndex == 0 ? "[WASD] 고르기  [E] 조리  " : "") + (tabIndex == 0 && CookingBridge.MaxPortions() > 1 ? "[1~" + CookingBridge.MaxPortions() + "] 인분  " : "") + "[ESC] 닫기";   // v2.8 / v3.0 / v3.2 인분
        }
        // 탭 버튼 강조
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] == null) continue;
            Image img = tabButtons[i].GetComponent<Image>();
            img.color = (i == tabIndex) ? new Color(0.55f, 0.36f, 0.18f) : new Color(0.35f, 0.23f, 0.13f);
        }

        // 기존 내용 제거
        if (matDragging >= 0) EndMatDrag(Vector2.zero, false);   // v3.0 (리뷰 4-1): 끌던 재료 카드가 지워지니 드래그부터 거둔다
        for (int i = 0; i < spawned.Count; i++) Destroy(spawned[i]);
        spawned.Clear();
        potSlotRects[0] = null; potSlotRects[1] = null; potBoxRect = null;

        if (tabIndex == 0) BuildMaterialTab();
        else BuildDexTab();

        // v2.3: 아래쪽에 설명 상자 (선택한 요리가 있으면 그것부터)
        MakeDetailBox();
        RecipeData sel = string.IsNullOrEmpty(selectedRecipe) ? null : RecipeDatabase.Get(selectedRecipe);
        if (sel != null) ShowRecipeDetail(sel);
    }

    // ═════════════════ v2.3: 요리 설명 상자 ═════════════════
    private void MakeDetailBox()
    {
        RectTransform box = UIFactory.CreatePanel(contentArea, "DetailBox",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 0f), new Vector2(0f, 130f),
            new Color(0.10f, 0.065f, 0.045f, 0.96f), UIFactory.GOLD, 2f);
        spawned.Add(box.gameObject);
        detailText = UIFactory.CreateText(box, "Text",
            "요리 카드에 마우스를 올리거나 누르면 여기 설명이 떠 - 무엇을 하는지, 어떤 손님에 잘 박히는지, 언제 쓰는지", 16,
            UIFactory.DIM, TextAnchor.UpperLeft);
        detailText.rectTransform.offsetMin = new Vector2(16f, 10f);
        detailText.rectTransform.offsetMax = new Vector2(-16f, -10f);
        detailText.lineSpacing = 1.15f;
        detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailText.raycastTarget = false;
    }

    /// <summary>카드 위에 마우스 / 클릭 - 설명 상자 채우기 (미발견은 재료·조리법만)</summary>
    private void ShowRecipeDetail(RecipeData r)
    {
        if (detailText == null || r == null) return;
        // v3.5 (v9.36): 설명 상자 오른쪽 위에 그 요리 아이콘 72 (미발견은 실루엣). 글은 아이콘 자리만큼 비운다
        Sprite ds = SpriteBank.DishIcon(r.recipeId);
        if (detailIcon == null && ds != null && detailText.transform.parent != null)   // 리뷰: 처음 그림이 있는 요리를 볼 때 만든다 (특정 파일에 안 기댄다)
            detailIcon = UIFactory.CreateIcon(detailText.transform.parent, "Icon", ds, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -8f), 72f);
        if (detailIcon != null)
        {
            detailIcon.gameObject.SetActive(ds != null);
            if (ds != null) { detailIcon.sprite = ds; detailIcon.color = FoodStock.Instance != null && FoodStock.Instance.IsDiscovered(r.recipeId) ? Color.white : UIFactory.SILHOUETTE; }
            detailText.rectTransform.offsetMax = new Vector2(ds != null ? -96f : -16f, -10f);
        }
        bool disc = FoodStock.Instance != null && FoodStock.Instance.IsDiscovered(r.recipeId);
        string[] parts = r.recipeId.Replace("T2:", "").Split('+');
        string src = r.tier == 1 && parts.Length >= 2 ? MatKor(parts[0]) + " + " + MatKor(parts[1])
                   : (parts.Length >= 2 ? "합성: " + parts[0] + " + " + parts[1] : r.recipeId);
        if (!disc)
        {
            detailText.text = "???   " + src + Eul(src) + " " + MethodName(MethodOf(r)) + " 하면 알게 돼";   // v3.4: 받침 맞춤 · 해체
            detailText.color = UIFactory.DIM;
            return;
        }
        string t = r.displayName + (r.tier == 2 ? "  [전설]" : "") + "   " + RecipeText.RoleWord(r) + "   |   " + src + "   [" + MethodName(MethodOf(r)) + "]\n";
        t += RecipeText.Full(r, 1f);   // v2.7 (v9.14): 맛 문구(flavor)는 뺐다 - 테스터 "설명이 길고 현학적"
        detailText.text = t;
        detailText.color = UIFactory.CREAM;
    }

    /// <summary>카드에 호버 수신기 부착</summary>
    private void AttachDetailHover(RectTransform card, RecipeData r)
    {
        KitchenPanelCardHover h = card.gameObject.AddComponent<KitchenPanelCardHover>();
        h.owner = this; h.recipe = r;
    }

    /// <summary>KitchenPanelCardHover 가 부른다</summary>
    public void OnCardHover(RecipeData r) { ShowRecipeDetail(r); }

    // ═════════════════ 조리 시작 (조리 탭의 [조리 시작]·[E]) ═════════════════

    private void StartMinigame(int method)
    {
        string[] parts = selectedRecipe.Split('+');
        if (!CookingBridge.StartCook(ParseMat(parts[0]), ParseMat(parts[1]), portions)) return;   // v3.2: 인분

        // 새 미니게임 실행 (레시피 고유 조리법으로)
        if (CookingMinigame.Instance != null)
        {
            CookingMinigame.Instance.StartGame(method);
        }
        else
        {
            Debug.LogWarning("[주방] CookingMinigame 없음 - 즉시 완성 처리");
            CookingBridge.FinishCook("good");
        }

        // 미니게임 진행 동안 패널 닫기
        isOpen = false;
        root.gameObject.SetActive(false);
        selectedRecipe = "";
        stationFilter = -1;
        ClearPot();   // v3.0
    }

    // ═════════════════ 도감 탭 ═════════════════
    private void BuildDexTab()
    {
        int total = 0, found = 0;
        foreach (RecipeData r in RecipeDatabase.All)
        {
            total++;
            if (FoodStock.Instance.IsDiscovered(r.recipeId)) found++;
        }

        Text header = UIFactory.CreateText(contentArea, "DexHeader",
            "발견한 요리: " + found + " / " + total, 20, UIFactory.GOLD, TextAnchor.UpperLeft);
        header.rectTransform.anchorMin = new Vector2(0f, 1f);
        header.rectTransform.anchorMax = new Vector2(1f, 1f);
        header.rectTransform.offsetMin = new Vector2(4f, -30f);
        header.rectTransform.offsetMax = new Vector2(0f, 0f);
        spawned.Add(header.gameObject);

        // T1 그리드 (위) + T2 그리드 (아래)
        int col = 0, row = 0;
        int columns = 7;
        float cardW = 158f, cardH = 92f, gap = 8f;   // v2.3: 96 -> 92 (설명 상자 자리)
        float startY = -36f;

        // 전체 42종 순회 (T1 먼저, T2 나중)
        for (int tier = 1; tier <= 2; tier++)
        {
            foreach (RecipeData r in RecipeDatabase.All)
            {
                if (r.tier != tier) continue;
                bool disc = FoodStock.Instance.IsDiscovered(r.recipeId);

                Color borderC = !disc ? new Color(0.3f, 0.24f, 0.19f)
                    : (r.tier == 2 ? UIFactory.T2PINK : UIFactory.TagColor(r.tag));

                RectTransform card = UIFactory.CreatePanel(contentArea, "Dex_" + r.recipeId,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(col * (cardW + gap), startY - (row + 1) * (cardH + gap)),
                    new Vector2(col * (cardW + gap) + cardW, startY - row * (cardH + gap) - gap),
                    new Color(0.14f, 0.09f, 0.06f, 0.95f), borderC, 2f);
                spawned.Add(card.gameObject);

                string txt;
                if (disc)
                {
                    string[] parts = r.recipeId.Replace("T2:", "").Split('+');
                    string src = r.tier == 1
                        ? MatKor(parts[0]) + "+" + MatKor(parts[1])
                        : "합성: " + parts[0] + "+" + parts[1];
                    txt = r.displayName + (r.tier == 2 ? " [전설]" : "") +
                          " [" + MethodName(MethodOf(r)) + "]\n" +
                          src + "\n" + RecipeText.RoleWord(r) + " - 클릭하면 설명";
                }
                else
                {
                    txt = "???\n" + (r.tier == 2 ? "(전설 미발견)" : "(미발견)");
                }

                Image dexIcon = UIFactory.CreateIcon(card, "Icon", SpriteBank.DishIcon(r.recipeId), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -6f), 40f);   // v3.5
                if (dexIcon != null && !disc) dexIcon.color = UIFactory.SILHOUETTE;
                Text t = UIFactory.CreateText(card, "Txt", txt, 13,
                    disc ? UIFactory.CREAM : UIFactory.DIM, TextAnchor.UpperLeft);
                t.rectTransform.offsetMin = new Vector2(dexIcon != null ? 50f : 8f, 4f);
                t.rectTransform.offsetMax = new Vector2(-6f, -6f);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                AttachDetailHover(card, r);   // v2.3: 올리거나 클릭 = 아래 설명 상자

                col++;
                if (col >= columns) { col = 0; row++; }
            }
        }
    }

    // ─────────────────────────────────────────
    private MaterialType ParseMat(string s)
    {
        switch (s)
        {
            case "armor": return MaterialType.Armor;
            case "elec": return MaterialType.Elec;
            case "fire": return MaterialType.Fire;
            case "ice": return MaterialType.Ice;
            case "poison": return MaterialType.Poison;
            default: return MaterialType.Meat;
        }
    }

    /// <summary>레시피 키 낱말 -> 재료 이름 (v9.10.1: MaterialNames 한 곳에서)</summary>
    private string MatKor(string s) { return MaterialNames.Kor(s); }
}

/// <summary>v2.3: 요리 카드 호버/클릭 수신기 - KitchenPanel.OnCardHover 로 설명 상자를 채운다</summary>
public class KitchenPanelCardHover : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public KitchenPanel owner;
    public RecipeData recipe;
    public void OnPointerEnter(PointerEventData e) { if (owner != null) owner.OnCardHover(recipe); }
    public void OnPointerClick(PointerEventData e) { if (owner != null) owner.OnCardHover(recipe); }
}

/// <summary>v3.0 (v9.22): 재료 카드 드래그 수신기 - 끌기 시작/이동/놓기를 KitchenPanel 로 보낸다 (GameHUD.FoodCardDrag 와 같은 꼴). 끌기 시작하면 클릭 자격을 내려 "누르면 빈 칸에" 와 겹치지 않는다</summary>
public class MaterialCardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public KitchenPanel owner;
    public int mat = -1;

    public void OnBeginDrag(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        e.eligibleForClick = false;
        if (owner != null) owner.BeginMatDrag(mat, e.position);
    }

    public void OnDrag(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;   // 리뷰 4-4
        if (owner != null) owner.MoveMatDrag(e.position);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        if (owner != null) { owner.MoveMatDrag(e.position); owner.EndMatDrag(e.position, true); }
    }
}
