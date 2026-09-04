using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// [EngineCab.cs] v3 - B-3: 기관차 칸 = 기관사 페르소나 (방향결정 2026-08-31)
///
/// - v3 (고퀄 PNG 적용 2026-09-03): 작살포/레버/바위가 Resources/Sprites/WDT/ 의 harpoon / leverpost / leverhandle /
///   rock_<재료> PNG를 SpriteBank로 우선 사용. 없으면 v2 코드 도트. 좌표 동일.
///
/// - v2 (탑뷰 재스킨 - 목업 v2 컨펌 2026-09-02): 작살포/레버/자원 바위를 사각형 조합 -> 도트 스프라이트로
///   (PixelPainter.cs 신규). 작살포 = 무쇠 삼각대 링 + 북동 조준 포신 + 미늘촉, 레버 = 무쇠 슬롯 판 + 구리 손잡이,
///   바위 = 3톤 바위 + 재료색 광맥 점. 조작 좌표(HarpoonX/LeverX/Reach)와 로직은 v1 그대로.
///   기차는 두상 쪽(왼쪽)으로 달리므로 바위는 왼쪽에서 나타나 오른쪽으로 흘러간다 (v1은 반대였음 -
///   두상이 뒤를 보고 달리는 모순 해소. ParallaxBackground v2와 방향 일치).
///
/// 기관차에 두 개의 손 조작이 생긴다. 이로써 기획서 v1.5의 이중 페르소나
/// (요리사 x 기관사)가 처음으로 실제 조작으로 존재하게 된다.
///
///  1) 작살포 (원안 부활): 길가를 지나가는 자원 바위를 [E]로 낚는다.
///     - 바위는 재료 1종의 색을 띠고 흘러간다 -> 명중 시 그 재료 3~5개 (PickupFX 흡수)
///     - 25% 확률로 어그로: "황야가 마주 낚아챈다" - 스팀 랩터 1~2 난입
///     - 쿨타임 12초. 미니게임 아님 - 바위가 지나갈 때 자리에 있는가의 타이밍 잡
///  2) 기관차 레버: [E]로 순항 <-> 전속 토글.
///     - 전속: 적 스폰 간격 -35% (웨이브를 당겨온다) + 조리 판정 -10% + 주행 연출 가속
///     - 자신 있으면 당기는 리스크 레버. 다른 시스템은 EngineCab.FullSteam /
///       EngineCab.SpawnIntervalMul만 읽으면 된다
///
/// E 우선순위: 주방 이벤트 진행 중에는 양보 (이벤트 E 연타와 충돌 방지).
/// 조리대/마비 슬롯과는 위치가 겹치지 않지만 InteractConsumedFrame도 존중한다.
/// 사용법: 없음! 파일만 넣으면 자동 생성. GameBalance의 Harpoon*/Lever*가 수치.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class EngineCab : MonoBehaviour
{
    private static EngineCab instance;

    // ── 다른 시스템이 읽는 상태 ──
    /// <summary>전속 주행 중인가 (레버 ON)</summary>
    public static bool FullSteam { get; private set; }

    /// <summary>적 스폰 간격 배율 (WaveManager가 읽음 - 전속이면 0.65)</summary>
    public static float SpawnIntervalMul
    {
        get { return FullSteam ? GameBalance.LeverSpawnMul : 1f; }
    }

    // ── 내부 상태 ──
    private Transform chefTransform;
    private float harpoonReadyTime = 0f;
    private readonly List<ResourceRock> rocks = new List<ResourceRock>();
    private float nextRockTime = 0f;

    // 런 1회 스토리 안내
    private static bool harpoonStoryShown = false;
    private static bool leverStoryShown = false;

    // 비주얼/힌트
    private Transform leverHandle;
    private Canvas hintCanvas;
    private Text harpoonHint;
    private Text leverHint;

    // ─────────────────────────────────────────────
    // 부트스트랩 (씬 로드마다 상태 리셋)
    // ─────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("EngineCab");
        DontDestroyOnLoad(go);
        go.AddComponent<EngineCab>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // 새 런: 레버는 순항으로, 안내는 다시 나오게
        FullSteam = false;
        harpoonStoryShown = false;
        leverStoryShown = false;
        ParallaxBackground.SetSpeedMultiplier(1f);
        if (instance != null) instance.ResetSceneState();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        BuildVisuals();
        BuildHints();
    }

    private void ResetSceneState()
    {
        rocks.Clear();   // 씬 오브젝트였던 바위들은 리로드로 이미 소멸
        harpoonReadyTime = 0f;
        nextRockTime = 0f;
        chefTransform = null;
        if (leverHandle != null)
            leverHandle.localEulerAngles = new Vector3(0f, 0f, 25f);
    }

    // ─────────────────────────────────────────────
    // 비주얼 (코드 도형 - 아트 단계에서 교체)
    // ─────────────────────────────────────────────
    private void BuildVisuals()
    {
        // 작살포: 기관차 지붕 북쪽 가장자리 거치 (두상 눈썹 장갑 위쪽) - 삼각대 링 + 북동 조준 포신 + 미늘촉
        SpriteBank.Attach(transform, "Harpoon", "harpoon", PaintHarpoon(),
            new Vector3(GameBalance.HarpoonX, 1.7f, 0f), -4);

        // 레버: 운전석 바닥 (B-2.1: 지붕 위에 떠 있던 것을 칸 안으로 내림) - 슬롯 판 + 손잡이(뿌리 피벗 회전)
        SpriteBank.Attach(transform, "LeverPost", "leverpost", PaintLeverPost(),
            new Vector3(GameBalance.LeverX, 0.35f, 0f), -4);
        SpriteRenderer handle = SpriteBank.Attach(transform, "LeverHandle", "leverhandle", PaintLeverHandle(),
            new Vector3(GameBalance.LeverX, 0.55f, 0f), -3);
        handle.transform.localEulerAngles = new Vector3(0f, 0f, 25f);
        leverHandle = handle.transform;
    }

    /// <summary>작살포 (캔버스 40x40, 피벗 = 삼각대 링 중심 (14,26)). 목업 v2 좌표 이식</summary>
    private static Sprite PaintHarpoon()
    {
        PixelPainter p = new PixelPainter(40, 40);
        Color32 steel = new Color32(214, 210, 198, 255);
        p.Ellipse(6, 21, 22, 32, PixelPainter.BLK, PixelPainter.BLK_O);                 // 삼각대 링 (검정)
        p.Ellipse(9, 23, 19, 28, PixelPainter.BLK_L, PixelPainter.CLEAR);
        p.Line(9, 30, 14, 23, PixelPainter.BLK_O, 1); p.Line(19, 30, 14, 23, PixelPainter.BLK_O, 1);
        p.Line(14, 26, 31, 4, PixelPainter.BLK_L, 3);                                    // 포신 (북동 조준)
        p.Line(14, 26, 30, 5, PixelPainter.GREY, 1);
        p.Line(20, 18, 23, 21, PixelPainter.GOLD, 2);                                    // 금 밴드
        p.Polygon(new int[] { 29, 1, 36, 6, 31, 9, 28, 4 }, steel, PixelPainter.CLEAR);  // 작살촉
        p.Polygon(new int[] { 27, 6, 25, 10, 30, 8 }, steel, PixelPainter.CLEAR);        // 미늘
        p.Rivet(11, 25, PixelPainter.GOLD_D, PixelPainter.GOLD_L); p.Rivet(18, 29, PixelPainter.GOLD_D, PixelPainter.GOLD_L);
        return p.Bake(TrainDeck.PPU, 14f, 26f);
    }

    /// <summary>레버 슬롯 판: 무쇠 받침 + 세로 슬롯 (캔버스 12x24)</summary>
    private static Sprite PaintLeverPost()
    {
        PixelPainter p = new PixelPainter(12, 24);
        p.Ellipse(0, 17, 11, 23, PixelPainter.GREY, PixelPainter.BLK_O);                 // 받침 (회색)
        p.Rect(4, 0, 7, 20, PixelPainter.BLK);                                           // 슬롯 판
        p.RectOutline(4, 0, 7, 20, PixelPainter.BLK_O);
        p.Line(5, 1, 5, 19, PixelPainter.BLK_L, 1);
        return p.Bake(TrainDeck.PPU);
    }

    /// <summary>레버 손잡이: 구리 막대 + 손잡이 구슬 (피벗 = 뿌리 (4,17) - 회전이 레버처럼 보인다)</summary>
    private static Sprite PaintLeverHandle()
    {
        PixelPainter p = new PixelPainter(9, 18);
        p.Line(4, 17, 4, 4, PixelPainter.BLK_O, 3);
        p.Line(4, 16, 4, 4, PixelPainter.GREY, 1);
        p.Ellipse(1, 0, 7, 6, PixelPainter.GOLD, PixelPainter.GOLD_D);                   // 손잡이 구슬 (금)
        p.Point(3, 2, PixelPainter.GOLD_L);
        return p.Bake(TrainDeck.PPU, 4f, 17f);
    }

    /// <summary>근접 힌트 라벨 2개 (SlotMarkerUI와 같은 화면 추종 방식)</summary>
    private void BuildHints()
    {
        hintCanvas = UIFactory.CreateCanvas("EngineCab_Canvas", 8);   // 슬롯 마커(9)보다 아래
        harpoonHint = UIFactory.CreateText(hintCanvas.transform, "HarpoonHint", "", 16,
            new Color(0.95f, 0.9f, 0.8f), TextAnchor.MiddleCenter);
        harpoonHint.rectTransform.sizeDelta = new Vector2(240f, 44f);
        leverHint = UIFactory.CreateText(hintCanvas.transform, "LeverHint", "", 16,
            new Color(0.95f, 0.9f, 0.8f), TextAnchor.MiddleCenter);
        leverHint.rectTransform.sizeDelta = new Vector2(240f, 44f);
    }

    // ─────────────────────────────────────────────
    // 매 프레임
    // ─────────────────────────────────────────────
    private void Update()
    {
        TickRocks();
        TickInteract();
        TickLeverHold();   // 픽스 2차: 레버 [E] 홀드 진행
        TickHints();
    }

    private bool InBattle()
    {
        return GameManager.Instance != null
            && GameManager.Instance.currentState == GameManager.GameState.Battle;
    }

    // ─────────────────────────────────────────────
    // 자원 바위 (길가를 흘러간다 - 작살의 표적)
    // ─────────────────────────────────────────────
    private void TickRocks()
    {
        // 전투가 아니면 (기차 정차) 바위도 흐르지 않는다 - 남은 바위 정리
        if (!InBattle() || !GameBalance.HarpoonEnabled)
        {
            for (int i = rocks.Count - 1; i >= 0; i--)
                if (rocks[i] != null) Destroy(rocks[i].gameObject);
            rocks.Clear();
            nextRockTime = Time.time + GameBalance.RockSpawnIntervalMin;
            return;
        }

        // 스폰
        rocks.RemoveAll(r => r == null);
        if (Time.time >= nextRockTime && rocks.Count < GameBalance.RockMaxAlive)
        {
            nextRockTime = Time.time
                + Random.Range(GameBalance.RockSpawnIntervalMin, GameBalance.RockSpawnIntervalMax);
            SpawnRock();
        }
    }

    private void SpawnRock()
    {
        MaterialType type = (MaterialType)Random.Range(0, 6);
        GameObject go = new GameObject("ResourceRock");
        go.transform.position = new Vector3(-20f, GameBalance.RockY, 0f);   // v2: 앞(왼쪽)에서 나타난다
        ResourceRock rock = go.AddComponent<ResourceRock>();
        rock.Init(type);
        rocks.Add(rock);
    }

    /// <summary>사거리 안에서 가장 가까운 바위 (없으면 null)</summary>
    private ResourceRock NearestRock()
    {
        ResourceRock best = null;
        float bestDist = GameBalance.HarpoonRange;
        for (int i = 0; i < rocks.Count; i++)
        {
            if (rocks[i] == null) continue;
            float d = Mathf.Abs(rocks[i].transform.position.x - GameBalance.HarpoonX);
            if (d < bestDist) { bestDist = d; best = rocks[i]; }
        }
        return best;
    }

    // ─────────────────────────────────────────────
    // [E] 조작 (작살 / 레버)
    // ─────────────────────────────────────────────
    private void TickInteract()
    {
        if (chefTransform == null)
        {
            GameObject chefObj = GameObject.Find("Chef");
            if (chefObj != null) chefTransform = chefObj.transform;
            if (chefTransform == null) return;
        }

        // UI/이벤트 진행 중엔 양보 (이벤트 E 연타가 레버를 당기는 사고 방지)
        if (CookingMinigame.IsActive || KitchenPanel.IsOpenStatic || PauseMenu.IsOpen
            || AugmentPickUI.IsOpen || WorkshopUI.IsOpen || KitchenEventManager.IsActive)
            return;

        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (ChefController.InteractConsumedFrame == Time.frameCount) return;

        float chefX = chefTransform.position.x;

        // 작살포
        if (GameBalance.HarpoonEnabled
            && Mathf.Abs(chefX - GameBalance.HarpoonX) <= GameBalance.HarpoonReach)
        {
            ChefController.InteractConsumedFrame = Time.frameCount;
            TryFireHarpoon();
            return;
        }

        // 레버 - 픽스 2차 (상호작용 변주): 꾹 눌러 당긴다 (묵직함 + 지나가다 오발 방지)
        // GetKeyDown은 홀드 시작만 기록 - 실제 당김은 TickLeverHold가 처리
        if (GameBalance.LeverEnabled
            && Mathf.Abs(chefX - GameBalance.LeverX) <= GameBalance.LeverReach)
        {
            ChefController.InteractConsumedFrame = Time.frameCount;
            leverHolding = true;
            leverHoldTime = 0f;
        }
    }

    // ── 픽스 2차: 레버 홀드 처리 (매 프레임) ──
    private bool leverHolding = false;
    private float leverHoldTime = 0f;

    private void TickLeverHold()
    {
        if (!leverHolding) return;

        // 손을 뗐거나 레버에서 멀어지면 취소
        bool near = chefTransform != null && GameBalance.LeverEnabled
            && Mathf.Abs(chefTransform.position.x - GameBalance.LeverX) <= GameBalance.LeverReach;
        if (!Input.GetKey(KeyCode.E) || !near)
        {
            leverHolding = false;
            leverHoldTime = 0f;
            return;
        }

        ChefController.InteractConsumedFrame = Time.frameCount;   // 홀드 중 조리대/작살 양보
        leverHoldTime += Time.deltaTime;
        if (leverHoldTime >= GameBalance.LeverHoldSec)
        {
            leverHolding = false;
            leverHoldTime = 0f;
            ToggleLever();
        }
    }

    private void TryFireHarpoon()
    {
        if (!InBattle())
        {
            UIManager.Instance?.ShowStatChange("[작살포] 기차가 서 있다 - 달릴 때 낚아라");
            return;
        }
        if (Time.time < harpoonReadyTime)
        {
            UIManager.Instance?.ShowStatChange("[작살포] 재장전 중... "
                + Mathf.CeilToInt(harpoonReadyTime - Time.time) + "초");
            return;
        }

        ResourceRock target = NearestRock();
        if (target == null)
        {
            UIManager.Instance?.ShowStatChange("[작살포] 지금은 낚을 바위가 없다 - 길가를 지켜봐라");
            return;
        }

        harpoonReadyTime = Time.time + GameBalance.HarpoonCooldown;

        // 발사 연출: 거치대 -> 바위로 빔 + 명중 팝
        Vector3 muzzle = new Vector3(GameBalance.HarpoonX + 0.85f, 2.8f, 0f);   // v2: 포신 끝 (북동)
        Color rockColor = PickupFX.ColorOf(target.materialType);
        if (AttackVFX.Instance != null)
            AttackVFX.Instance.Beam(muzzle, target.transform.position, rockColor, 0.12f);
        GameFeel.DeathPop(target.transform.position, rockColor, 0.7f);
        SoundManager.Play("sfx_harpoon");   // 클립 없으면 무시
        GameFeel.Shake(0.08f, "harpoon", 1f);

        // 보상: 바위 색 재료 3~5개 (흡수 연출)
        int amount = Random.Range(GameBalance.HarpoonMatMin, GameBalance.HarpoonMatMax + 1);
        PickupFX.Spawn(target.transform.position, target.materialType, amount);
        UIManager.Instance?.ShowStatChange("[작살포] 명중! " + amount + "개를 낚았다");

        if (!harpoonStoryShown)
        {
            harpoonStoryShown = true;
            UIManager.Instance?.ShowStatChange("선대도 이 작살로 황야를 낚았다...");
        }

        // 원안의 리트리벌 리스크: 가끔 황야가 마주 낚아챈다
        if (Random.value < GameBalance.HarpoonAggroChance && WaveManager.Instance != null)
        {
            int n = Random.Range(GameBalance.HarpoonAggroMin, GameBalance.HarpoonAggroMax + 1);
            WaveManager.Instance.SpawnAmbush(n);
            UIManager.Instance?.ShowDanger("황야가 마주 낚아챘다! 굶주린 것들 " + n + "마리 난입!");
        }

        rocks.Remove(target);
        Destroy(target.gameObject);
    }

    private void ToggleLever()
    {
        FullSteam = !FullSteam;
        ParallaxBackground.SetSpeedMultiplier(FullSteam ? GameBalance.LeverParallaxMul : 1f);
        if (leverHandle != null)
            leverHandle.localEulerAngles = new Vector3(0f, 0f, FullSteam ? -25f : 25f);

        SoundManager.Play("sfx_lever");   // 클립 없으면 무시
        GameFeel.Shake(0.12f, "lever", 0.5f);

        if (FullSteam)
        {
            UIManager.Instance?.ShowDanger("전속 주행! 손님들이 빨리 온다 - 도마가 흔들린다!");
            // 밸런스 1차: 전속의 리턴을 유저에게 명시 (수치는 GameBalance.LeverGoldMul)
            UIManager.Instance?.ShowStatChange("[전속 보너스] 회전율이 곧 매출 - 처치 골드 +"
                + Mathf.RoundToInt((GameBalance.LeverGoldMul - 1f) * 100f) + "%");
            if (!leverStoryShown)
            {
                leverStoryShown = true;
                UIManager.Instance?.ShowStatChange("속도는 공짜가 아니다. 황야에서는 더더욱.");
            }
        }
        else
            UIManager.Instance?.ShowStatChange("[기관차] 순항 복귀 - 도마가 잠잠해졌다");
    }

    // ─────────────────────────────────────────────
    // 근접 힌트 라벨 (조작 지점 위에 뜬다)
    // ─────────────────────────────────────────────
    private void TickHints()
    {
        if (harpoonHint == null || Camera.main == null) return;

        bool uiBlocked = CookingMinigame.IsActive || KitchenPanel.IsOpenStatic
            || PauseMenu.IsOpen || AugmentPickUI.IsOpen || WorkshopUI.IsOpen
            || KitchenEventManager.IsActive;   // 이벤트 중엔 E가 이벤트 몫 - 힌트도 숨김
        float chefX = chefTransform != null ? chefTransform.position.x : -999f;

        // 작살 힌트
        bool nearHarpoon = !uiBlocked && GameBalance.HarpoonEnabled
            && Mathf.Abs(chefX - GameBalance.HarpoonX) <= GameBalance.HarpoonReach;
        harpoonHint.gameObject.SetActive(nearHarpoon);
        if (nearHarpoon)
        {
            string label;
            if (Time.time < harpoonReadyTime)
                label = "재장전 " + Mathf.CeilToInt(harpoonReadyTime - Time.time) + "초";
            else if (NearestRock() != null)
                label = "[E] 작살 발사!";
            else
                label = "낚을 바위 대기 중...";
            harpoonHint.text = label;
            harpoonHint.rectTransform.position = Camera.main.WorldToScreenPoint(
                new Vector3(GameBalance.HarpoonX, 3.4f, 0f));
        }

        // 레버 힌트
        bool nearLever = !uiBlocked && GameBalance.LeverEnabled
            && Mathf.Abs(chefX - GameBalance.LeverX) <= GameBalance.LeverReach;
        leverHint.gameObject.SetActive(nearLever);
        if (nearLever)
        {
            // 픽스 2차: 홀드 진행 표시 (당기는 중이면 %)
            if (leverHolding && leverHoldTime > 0f)
                leverHint.text = "당기는 중... " + Mathf.RoundToInt(
                    Mathf.Clamp01(leverHoldTime / GameBalance.LeverHoldSec) * 100f) + "%";
            else
                leverHint.text = FullSteam ? "[E] 꾹 - 순항 복귀" : "[E] 꾹 - 전속 주행!";
            // B-2.1: 레버가 운전석 바닥으로 내려왔으므로 힌트도 함께 하강 (3.1 -> 2.0)
            leverHint.rectTransform.position = Camera.main.WorldToScreenPoint(
                new Vector3(GameBalance.LeverX, 2.0f, 0f));
        }
    }
}


/// <summary>
/// 길가를 흘러가는 자원 바위 1개. 재료 1종의 색을 띤다 (작살포의 표적).
/// EngineCab이 생성/정리한다.
/// </summary>
public class ResourceRock : MonoBehaviour
{
    public MaterialType materialType;

    private static readonly Dictionary<MaterialType, Sprite> rockSpriteCache = new Dictionary<MaterialType, Sprite>();

    public void Init(MaterialType type)
    {
        materialType = type;

        // v2: 3톤 바위 + 재료색 광맥 점 (뭘 낚을지 보이게) - 종류별 1회 굽고 캐시
        Sprite sprite = SpriteBank.Get("rock_" + type.ToString().ToLower());   // v3: PNG 우선 (rock_meat 등)
        if (sprite == null && !rockSpriteCache.TryGetValue(type, out sprite))
        {
            sprite = PaintRock(PickupFX.ColorOf(type));
            rockSpriteCache[type] = sprite;
        }
        SpriteRenderer body = gameObject.AddComponent<SpriteRenderer>();
        body.sprite = sprite;
        body.sortingOrder = -8;   // 패럴랙스(-10)와 데크(-6) 사이 = 길가
    }

    /// <summary>바위 도트 (캔버스 24x20): 그림자 + 3톤 바위 + 외곽선 + 광맥 점 4개</summary>
    private static Sprite PaintRock(Color ore)
    {
        PixelPainter p = new PixelPainter(24, 20);
        Color32 oreC = ore;
        Color32 oreL = PixelPainter.Mix(oreC, new Color32(255, 255, 255, 255), 0.45f);
        p.Shadow(2, 13, 23, 19);
        p.Ellipse(1, 2, 21, 16, new Color32(150, 104, 58, 255), new Color32(96, 62, 34, 255));
        p.Ellipse(3, 3, 18, 12, new Color32(176, 126, 72, 255), PixelPainter.CLEAR);
        p.Ellipse(4, 4, 11, 8, new Color32(198, 148, 90, 255), PixelPainter.CLEAR);
        p.Rect(8, 7, 10, 9, oreC); p.Point(8, 7, oreL);
        p.Rect(13, 9, 15, 11, oreC); p.Point(13, 9, oreL);
        p.Rect(6, 11, 7, 12, oreC);
        p.Rect(15, 5, 16, 6, oreC); p.Point(15, 5, oreL);
        return p.Bake(TrainDeck.PPU);
    }

    private void Update()
    {
        // v2: 오른쪽으로 흘러간다 (기차가 두상 쪽 = 왼쪽으로 달리는 연출과 합치)
        transform.position += Vector3.right * GameBalance.RockSpeed * Time.deltaTime;
        if (transform.position.x > 16f) Destroy(gameObject);
    }
}
