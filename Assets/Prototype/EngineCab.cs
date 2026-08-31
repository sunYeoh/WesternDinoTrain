using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// [EngineCab.cs] v1 (신규 파일) - B-3: 기관차 칸 = 기관사 페르소나 (방향결정 2026-08-31)
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
        Color iron = new Color(0.16f, 0.11f, 0.08f);
        Color copper = new Color(0.62f, 0.40f, 0.22f);

        // 작살포: 받침 + 45도 포신
        MakeQuad("HarpoonBase", GameBalance.HarpoonX, 2.0f, 0.5f, 0.35f, 0f, iron, -4);
        MakeQuad("HarpoonBarrel", GameBalance.HarpoonX + 0.18f, 2.45f, 0.95f, 0.14f, 40f, copper, -4);

        // 레버: 기둥 + 손잡이 (전속이면 반대로 기운다)
        MakeQuad("LeverPost", GameBalance.LeverX, 1.95f, 0.16f, 0.5f, 0f, iron, -4);
        Transform handle = MakeQuad("LeverHandle", GameBalance.LeverX, 2.25f, 0.1f, 0.7f, 25f,
            new Color(0.85f, 0.55f, 0.25f), -4);
        leverHandle = handle;
    }

    private Transform MakeQuad(string name, float x, float y, float w, float h, float tiltZ,
        Color color, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(x, y, 0f);
        go.transform.localScale = new Vector3(w, h, 1f);
        go.transform.localEulerAngles = new Vector3(0f, 0f, tiltZ);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = TrainDeck.GetWhiteSprite();
        sr.color = color;
        sr.sortingOrder = order;
        return go.transform;
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
        go.transform.position = new Vector3(20f, GameBalance.RockY, 0f);
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

        // 레버
        if (GameBalance.LeverEnabled
            && Mathf.Abs(chefX - GameBalance.LeverX) <= GameBalance.LeverReach)
        {
            ChefController.InteractConsumedFrame = Time.frameCount;
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
        Vector3 muzzle = new Vector3(GameBalance.HarpoonX + 0.5f, 2.7f, 0f);
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
            leverHint.text = FullSteam ? "[E] 순항 복귀" : "[E] 전속 주행!";
            leverHint.rectTransform.position = Camera.main.WorldToScreenPoint(
                new Vector3(GameBalance.LeverX, 3.1f, 0f));
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

    public void Init(MaterialType type)
    {
        materialType = type;

        // 바위 본체 (회갈색)
        SpriteRenderer body = gameObject.AddComponent<SpriteRenderer>();
        body.sprite = TrainDeck.GetCircleSprite();
        body.color = new Color(0.42f, 0.38f, 0.33f);
        body.sortingOrder = -8;   // 패럴랙스(-10)와 데크(-6) 사이 = 길가
        transform.localScale = Vector3.one * 1.05f;

        // 광맥 코어 (재료 색 - 뭘 낚을지 보이게)
        GameObject core = new GameObject("Core");
        core.transform.SetParent(transform, false);
        core.transform.localScale = Vector3.one * 0.5f;
        SpriteRenderer coreSr = core.AddComponent<SpriteRenderer>();
        coreSr.sprite = TrainDeck.GetCircleSprite();
        coreSr.color = PickupFX.ColorOf(type);
        coreSr.sortingOrder = -7;
    }

    private void Update()
    {
        // 왼쪽으로 흘러간다 (기차가 오른쪽으로 달리는 연출과 합치)
        transform.position += Vector3.left * GameBalance.RockSpeed * Time.deltaTime;
        if (transform.position.x < -16f) Destroy(gameObject);
    }
}
