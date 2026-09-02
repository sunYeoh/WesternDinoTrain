using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// [KitchenEventManager.cs] v3
/// 주방 돌발 이벤트 총괄 매니저 (기획 B-4)
/// - v2: 증강 연동 (보험 계약 = 페널티 감소 / 부채질 장인 = 발생 2배 + 보상 3배)
/// - v3: 이벤트 맥락 가중치 - "왜 지금 이 이벤트인가"가 전장 상황에서 나온다
///   * 몬스터 침입: 기차 근처 적이 많을수록 확률 증가
///   * 화재: 화염 계열 적(카르노/익룡)이 가까이 있을 때 확률 급증
///   * 재료 흘림: 최근 큰 피해를 받았을수록 확률 증가
///   * 기구 고장: 도구(칼/팬)가 낡을수록 확률 증가
///
/// 역할
/// 1) 일정 주기마다 랜덤 주방 이벤트를 발생시킨다
/// 2) 이벤트 공용 UI(제목/안내/진행 게이지/남은 시간)를 코드로 생성해 관리한다
/// 3) 성공/실패에 따른 보상과 페널티를 기차에 적용한다
///
/// 사용법
/// - "GameSystems" 오브젝트에 이 스크립트를 추가하면 끝 (UI는 자동 생성)
/// - 조리 미니게임(CookingMinigame) 진행 중에는 이벤트를 발생시키지 않는다 (키 충돌 방지)
///
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class KitchenEventManager : MonoBehaviour
{
    public static KitchenEventManager Instance;

    /// <summary>이벤트 진행 중인지 여부 (CookingStation 등에서 조작 충돌 방지용으로 확인)</summary>
    public static bool IsActive
    {
        get { return Instance != null && Instance.currentEvent != null; }
    }

    [Header("발생 주기 설정")]
    public bool eventEnabled = true;          // 전체 on/off
    public float firstDelay = 25f;            // 게임 시작 후 첫 이벤트까지 대기 시간
    public float minInterval = 20f;           // 다음 이벤트까지 최소 간격
    public float maxInterval = 35f;           // 다음 이벤트까지 최대 간격

    [Header("디버그")]
    public bool debugKeyEnabled = true;       // F11로 이벤트 강제 발생 (빌드 전 false)

    // 현재 진행 중인 이벤트
    private IKitchenEvent currentEvent;
    private float eventTimeLeft;              // 남은 제한 시간
    private float eventTimeMax;               // 제한 시간 원본 (게이지 비율 계산용)
    private float nextEventTime;              // 다음 이벤트 발생 시각
    private int firedCount;                   // 지금까지 발생한 이벤트 수 (난이도 계산용)

    // ---------- UI 참조 ----------
    private Canvas canvas;
    private RectTransform panelRoot;          // 이벤트 배너 전체
    private Text titleText;
    private Text guideText;
    private Text eventHpText;                 // 기차 HP 실시간 표시
    private RectTransform gaugeFill;          // 진행도 게이지 (초록)
    private RectTransform timeFill;           // 남은 시간 게이지 (주황)
    private RectTransform customRoot;         // 각 이벤트가 자유롭게 쓰는 영역
    private TrainManager cachedTrain;         // HP 표시용 캐시

    // 맥락 가중치용 (v3): 최근 받은 피해 추적
    private float prevTrainHP = -1f;
    private float recentDamage = 0f;          // 최근 피해 누적 (초당 8씩 감쇠)

    /// <summary>이벤트별 커스텀 오브젝트를 붙일 부모 (전체 화면 크기)</summary>
    public RectTransform CustomRoot { get { return customRoot; } }

    // ==================================================================
    //  B-1: 이벤트 위치 앵커 (방향결정 2026-08-31)
    //  이벤트가 "어딘가에서" 터지고, 셰프가 그 곁에 있어야 조작이 먹힌다.
    //  흘림(마우스 줍기)은 제외. GameBalance.ProximityInteract=false면 전부 위치 무관.
    // ==================================================================

    /// <summary>이번 이벤트의 월드 X 앵커 (HasAnchor일 때만 유효)</summary>
    public static float AnchorX { get; private set; }

    /// <summary>이번 이벤트가 위치형인가</summary>
    public static bool HasAnchor { get; private set; }

    /// <summary>셰프가 앵커 근처에 있는가 (위치형 이벤트의 입력 게이트 - 각 이벤트가 읽음)</summary>
    public static bool ChefInReach { get; private set; }

    private Transform chefTransform;   // 근접 판정용 캐시

    /// <summary>앵커의 캔버스 X 좌표 (이벤트 아이콘 배치용, 1920 기준. 앵커 없으면 0)</summary>
    public float AnchorCanvasX()
    {
        if (!HasAnchor || Camera.main == null) return 0f;
        float screenX = Camera.main.WorldToScreenPoint(new Vector3(AnchorX, 0f, 0f)).x;
        float canvasX = (screenX / Mathf.Max(1f, Screen.width) - 0.5f) * 1920f;
        return Mathf.Clamp(canvasX, -700f, 700f);
    }

    /// <summary>매 프레임 셰프-앵커 근접 갱신 (RunCurrentEvent에서 호출)</summary>
    private void UpdateChefReach()
    {
        if (!HasAnchor) { ChefInReach = true; return; }

        if (chefTransform == null)
        {
            GameObject chefObj = GameObject.Find("Chef");
            if (chefObj != null) chefTransform = chefObj.transform;
        }
        // 셰프를 못 찾으면 막지 않는다 (안전)
        ChefInReach = chefTransform == null
            || Mathf.Abs(chefTransform.position.x - AnchorX) <= GameBalance.EventReachX;
    }

    private static Font cachedFont;

    void Awake()
    {
        Instance = this;
        ChefInReach = true;   // B-1: 이벤트 없을 때 기본값 (게이트 잠김 방지)
        HasAnchor = false;
        BuildUI();
        nextEventTime = Time.time + firstDelay;
        HidePanel();
    }

    void Update()
    {
        // 최근 피해 추적 (재료 흘림 이벤트 가중치용)
        TrackRecentDamage();

        if (!eventEnabled) return;

        // v3.2: 증강 선택 / 일시정지 / 정비소가 떠 있는 동안 이벤트 완전 동결
        // (시간이 멈춰도 Update와 키 입력은 살아 있어서, 멈춘 시간에 공짜로
        //  이벤트를 해결하는 꼼수가 가능했음 -> 입력 처리 자체를 차단)
        if (AugmentPickUI.IsOpen || PauseMenu.IsOpen || WorkshopUI.IsOpen)
            return;

        // v3.1: 전투 중이 아니면(로비/마을 정비/게임오버) 이벤트 금지
        // - 진행 중이던 이벤트는 페널티 없이 조용히 정리
        // - 타이머를 firstDelay로 계속 밀어서, 전투 시작 후에도 최소 firstDelay만큼 여유를 준다
        bool inBattle = GameManager.Instance != null
            && GameManager.Instance.currentState == GameManager.GameState.Battle;
        if (!inBattle)
        {
            if (currentEvent != null) CancelCurrentEvent();
            nextEventTime = Time.time + firstDelay;
            return;
        }

        // v3.3: 분기 선로 '안개 선로' - 전투 시작 후 이벤트를 이르게 당긴다
        if (earlyEventPending)
        {
            earlyEventPending = false;
            nextEventTime = Time.time + 8f;
            Debug.Log("[주방이벤트] 안개 선로 - 이벤트 조기 발생 예약 (8초 후)");
        }

        // 디버그: F11로 즉시 발생 (전투 중에만 - 빌드 전 debugKeyEnabled를 꺼야 함)
        if (debugKeyEnabled && Input.GetKeyDown(KeyCode.F11) && currentEvent == null)
            StartRandomEvent();

        // 진행 중인 이벤트가 있으면 그것만 돌린다
        if (currentEvent != null)
        {
            RunCurrentEvent();
            return;
        }

        // 조리 미니게임 중에는 이벤트를 미룬다 (E / 방향키 입력이 겹치기 때문)
        if (CookingMinigame.IsActive)
        {
            nextEventTime = Mathf.Max(nextEventTime, Time.time + 2f);
            return;
        }

        // v3.4: 보스전 중에는 새 이벤트 시작 금지 (보스 패턴이 방해 역할을 대신 - 인지 과부하 방지)
        // 진행 중이던 이벤트는 위에서 정상 처리된다
        if (BossGimmickSystem.Instance != null && BossGimmickSystem.Instance.HasActiveBoss)
        {
            nextEventTime = Mathf.Max(nextEventTime, Time.time + 5f);
            return;
        }

        if (Time.time >= nextEventTime)
            StartRandomEvent();
    }

    // ==================================================================
    //  이벤트 진행 흐름
    // ==================================================================

    /// <summary>최근 피해 추적 (초당 8씩 감쇠) - 재료 흘림 가중치용</summary>
    private void TrackRecentDamage()
    {
        if (cachedTrain == null) cachedTrain = Object.FindFirstObjectByType<TrainManager>();
        if (cachedTrain == null) return;

        if (prevTrainHP < 0f) prevTrainHP = cachedTrain.currentHP;

        if (cachedTrain.currentHP < prevTrainHP)
            recentDamage += prevTrainHP - cachedTrain.currentHP;
        prevTrainHP = cachedTrain.currentHP;

        recentDamage = Mathf.Max(0f, recentDamage - 8f * Time.deltaTime);
    }

    /// <summary>전장 상황 기반 가중치로 이벤트 1개를 뽑아 시작 (v3)</summary>
    public void StartRandomEvent()
    {
        // ── 상황 수집 ──
        Vector3 trainPos = cachedTrain != null ? cachedTrain.transform.position : Vector3.zero;
        int nearbyCount = 0;
        bool fireThreat = false;

        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].IsAlive) continue;
            float d = Vector3.Distance(all[i].transform.position, trainPos);
            if (d <= 4f) nearbyCount++;
            if (d <= 9f && (all[i].data.enemyName.Contains("카르노") || all[i].data.enemyName.Contains("익룡")))
                fireThreat = true;
        }

        ChefController chef = Object.FindFirstObjectByType<ChefController>();
        float toolWear = chef != null ? (200f - chef.knifeSharpness - chef.panCondition) : 50f;

        // ── 가중치 계산: "왜 지금 이 이벤트인가"가 전장에서 나온다 ──
        float wIntrude = 1f + nearbyCount * 1.2f;              // 적이 붙어 있으면 침입
        float wFire = fireThreat ? 4f : 0.6f;                  // 화염 적이 있으면 화재
        float wSpill = recentDamage >= 40f ? 3f : 0.8f;        // 두들겨 맞았으면 흘림
        float wBreak = 0.6f + toolWear / 80f;                  // 도구가 낡았으면 고장

        float total = wIntrude + wFire + wSpill + wBreak;
        float roll = Random.Range(0f, total);

        IKitchenEvent ev;
        string reason;
        if (roll < wIntrude)
        { ev = new MonsterIntrusionEvent(); reason = "근처 적 " + nearbyCount + "마리"; }
        else if (roll < wIntrude + wFire)
        { ev = new KitchenFireEvent(); reason = fireThreat ? "화염 적 접근" : "무작위"; }
        else if (roll < wIntrude + wFire + wSpill)
        { ev = new MaterialSpillEvent(); reason = "최근 피해 " + Mathf.RoundToInt(recentDamage); }
        else
        { ev = new EquipmentBreakEvent(); reason = "도구 마모 " + Mathf.RoundToInt(toolWear); }

        Debug.Log("[주방이벤트] 선택 근거: " + reason);
        StartEvent(ev);
    }

    /// <summary>지정한 이벤트 시작</summary>
    public void StartEvent(IKitchenEvent ev)
    {
        if (ev == null || currentEvent != null) return;

        // Phase 2-3 아이템 '구리 소화기': 화재는 웨이브당 1회 자동 진압 (이벤트 자체가 안 뜬다)
        if (ev is KitchenFireEvent && ItemManager.TryAutoExtinguish())
        {
            UIManager.Instance?.ShowStatChange("[구리 소화기] 불길이 붙기도 전에 꺼졌다!");
            SoundManager.Play("sfx_ui_click");
            // 다음 이벤트 시각을 정상 주기로 재예약 (안 하면 같은 프레임에 다른 이벤트가 또 뜬다)
            nextEventTime = Time.time + Random.Range(minInterval, maxInterval)
                * AugmentManager.EventIntervalMul * ItemManager.EventIntervalMul;
            Debug.Log("[주방이벤트] 화재 자동 진압 (구리 소화기)");
            return;
        }

        currentEvent = ev;
        firedCount++;

        // ── B-1/B-2: 위치 앵커 결정 - 침입/화재/고장은 기차 어느 칸에서든 터진다 ──
        // (흘림은 마우스 줍기라 위치 무관.) 뒷칸 화재를 향해 달려가는 게 이 게임의 몸이다.
        HasAnchor = GameBalance.ProximityInteract && !(ev is MaterialSpillEvent);
        if (HasAnchor)
        {
            AnchorX = Random.Range(GameBalance.EventAnchorMinX, GameBalance.EventAnchorMaxX);
            Debug.Log("[주방이벤트] 발생 칸: " + GameBalance.CarNames[GameBalance.CarIndexOf(AnchorX)]
                + " (x " + AnchorX.ToString("F1") + ")");
        }
        UpdateChefReach();

        // 이벤트가 누적될수록 조금씩 어려워진다 (최대 +100%)
        float difficulty = Mathf.Min(1f, firedCount * 0.08f);

        ClearCustomRoot();
        ev.OnStart(this, difficulty);

        // 위치형 이벤트는 달려가는 시간만큼 제한시간에 여유를 준다
        eventTimeMax = ev.TimeLimit + (HasAnchor ? GameBalance.EventReachGrace : 0f);
        eventTimeLeft = eventTimeMax;

        titleText.text = ev.Title;
        guideText.text = ev.Guide;
        ShowPanel();

        Debug.Log("[주방이벤트] 발생: " + ev.Title);
    }

    private void RunCurrentEvent()
    {
        float dt = Time.deltaTime;
        eventTimeLeft -= dt;

        // B-1: 셰프-앵커 근접 갱신 (각 이벤트가 ChefInReach로 입력을 게이트)
        UpdateChefReach();

        bool success;
        bool finished = currentEvent.OnUpdate(dt, out success);

        // 시간 초과 = 실패
        if (!finished && eventTimeLeft <= 0f)
        {
            finished = true;
            success = false;
        }

        // 게이지 갱신
        SetFill(gaugeFill, Mathf.Clamp01(currentEvent.Progress));
        SetFill(timeFill, eventTimeMax > 0f ? Mathf.Clamp01(eventTimeLeft / eventTimeMax) : 0f);

        // B-1: 현장 밖이면 방향 화살표 + 달려가라 안내가 가이드를 대신한다
        if (HasAnchor && !ChefInReach && chefTransform != null)
        {
            string arrow = AnchorX > chefTransform.position.x ? "→→" : "←←";
            guideText.text = arrow + " 현장으로 달려가라! " + arrow + "   (" + currentEvent.Guide + ")";
        }
        else
            guideText.text = currentEvent.Guide;

        // 기차 HP 실시간 표시
        if (cachedTrain == null) cachedTrain = Object.FindFirstObjectByType<TrainManager>();
        if (cachedTrain != null && eventHpText != null)
            eventHpText.text = "기차 HP  " + Mathf.RoundToInt(cachedTrain.currentHP)
                + " / " + Mathf.RoundToInt(cachedTrain.currentMaxHP);

        if (finished)
            EndCurrentEvent(success);
    }

    // v3.3: 분기 선로 '안개 선로' 예약 플래그 (다음 전투 시작 시 1회 적용)
    private bool earlyEventPending = false;

    /// <summary>다음 전투가 시작되면 이벤트를 이르게 발생시킨다 (안개 선로).</summary>
    public void ScheduleEarlyEvent()
    {
        earlyEventPending = true;
    }

    /// <summary>
    /// v3.1: 이벤트를 성공/실패 판정 없이 조용히 중단 (페널티 없음).
    /// 전투가 아닌 상태(게임오버 등)로 넘어갈 때 사용.
    /// </summary>
    private void CancelCurrentEvent()
    {
        IKitchenEvent ev = currentEvent;
        currentEvent = null;
        HasAnchor = false; ChefInReach = true;   // B-1: 앵커 정리

        ClearCustomRoot();
        HidePanel();
        Debug.Log("[주방이벤트] 전투 종료로 취소: " + ev.Title);
    }

    private void EndCurrentEvent(bool success)
    {
        IKitchenEvent ev = currentEvent;
        currentEvent = null;
        HasAnchor = false; ChefInReach = true;   // B-1: 앵커 정리

        ev.OnEnd(success);
        ClearCustomRoot();
        HidePanel();

        // 발생 간격 배율 (Phase 2-3: '부채질 장인의 부채'는 아이템으로 이관 - 증강 값은 호환용)
        nextEventTime = Time.time + Random.Range(minInterval, maxInterval)
            * AugmentManager.EventIntervalMul * ItemManager.EventIntervalMul;
        Debug.Log("[주방이벤트] 종료: " + ev.Title + " / 결과 " + (success ? "성공" : "실패"));
    }

    // ==================================================================
    //  외부 연동 (프로젝트 API가 다르면 이 구역만 고치면 된다)
    // ==================================================================

    /// <summary>이벤트 실패 페널티 - 기차에 데미지 (아이템 '보험 계약서'가 배율을 줄인다)</summary>
    public void DamageTrain(float amount)
    {
        amount *= AugmentManager.EventPenaltyMul * ItemManager.EventPenaltyMul;
        TrainManager tm = Object.FindFirstObjectByType<TrainManager>();
        if (tm == null) return;
        tm.TakeDamage(amount);
        // 만약 TakeDamage가 공격자 인자를 필수로 요구해서 에러가 나면 위 줄을 아래로 교체
        // tm.TakeDamage(amount, null);
    }

    /// <summary>이벤트 성공 보상 - 기차 회복 (아이템 '부채질 장인의 부채'가 배율을 올린다)</summary>
    public void HealTrain(float amount)
    {
        amount *= AugmentManager.EventRewardMul * ItemManager.EventRewardMul;
        TrainManager tm = Object.FindFirstObjectByType<TrainManager>();
        if (tm != null) tm.Heal(amount);
    }

    // ==================================================================
    //  UI 생성 (코드 생성 uGUI)
    // ==================================================================

    private void BuildUI()
    {
        // 전용 캔버스 (다른 UI보다 위에 그린다)
        GameObject canvasGo = new GameObject("KitchenEventCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // 이벤트별 커스텀 오브젝트 영역 (전체 화면)
        customRoot = MakeBox(canvasGo.transform, "CustomRoot", new Color(0f, 0f, 0f, 0f));
        customRoot.anchorMin = Vector2.zero;
        customRoot.anchorMax = Vector2.one;
        customRoot.offsetMin = Vector2.zero;
        customRoot.offsetMax = Vector2.zero;
        Image customImg = customRoot.GetComponent<Image>();
        customImg.raycastTarget = false;

        // 이벤트 배너 패널
        // [수정] 상단 배치 시 기차 체력바를 가리는 문제 -> 하단 중앙(HUD 위)으로 이동
        panelRoot = MakeBox(canvasGo.transform, "EventBanner", new Color(0.09f, 0.07f, 0.06f, 0.9f));
        panelRoot.anchorMin = new Vector2(0.5f, 0f);
        panelRoot.anchorMax = new Vector2(0.5f, 0f);
        panelRoot.pivot = new Vector2(0.5f, 0f);
        panelRoot.anchoredPosition = new Vector2(0f, 172f);   // 하단 HUD(158px) 위 (HUD 정리 연동)
        panelRoot.sizeDelta = new Vector2(760f, 144f);
        panelRoot.GetComponent<Image>().raycastTarget = false;

        // 제목
        titleText = MakeText(panelRoot, "Title", "", 30, new Color(1f, 0.78f, 0.32f));
        RectTransform tRt = titleText.rectTransform;
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.offsetMin = new Vector2(14f, 0f);
        tRt.offsetMax = new Vector2(-14f, -8f);
        tRt.sizeDelta = new Vector2(tRt.sizeDelta.x, 38f);

        // 조작 안내
        guideText = MakeText(panelRoot, "Guide", "", 21, new Color(0.92f, 0.92f, 0.88f));
        RectTransform gRt = guideText.rectTransform;
        gRt.anchorMin = new Vector2(0f, 1f);
        gRt.anchorMax = new Vector2(1f, 1f);
        gRt.pivot = new Vector2(0.5f, 1f);
        gRt.anchoredPosition = new Vector2(0f, -48f);
        gRt.offsetMin = new Vector2(14f, gRt.offsetMin.y);
        gRt.offsetMax = new Vector2(-14f, gRt.offsetMax.y);
        gRt.sizeDelta = new Vector2(gRt.sizeDelta.x, 30f);

        // 진행도 게이지 (초록)
        gaugeFill = MakeGauge(panelRoot, "Gauge", -84f, new Color(0.35f, 0.85f, 0.4f));
        // 남은 시간 게이지 (주황)
        timeFill = MakeGauge(panelRoot, "TimeBar", -104f, new Color(0.95f, 0.55f, 0.2f));

        // 기차 HP 실시간 표시 (화재 등에서 닳는 속도가 바로 보이게)
        eventHpText = MakeText(panelRoot, "TrainHP", "", 19, new Color(1f, 0.5f, 0.45f));
        RectTransform hpRt = eventHpText.rectTransform;
        hpRt.anchorMin = new Vector2(0f, 1f);
        hpRt.anchorMax = new Vector2(1f, 1f);
        hpRt.pivot = new Vector2(0.5f, 1f);
        hpRt.anchoredPosition = new Vector2(0f, -118f);
        hpRt.sizeDelta = new Vector2(0f, 24f);
    }

    /// <summary>배경 + 채워지는 막대 한 쌍을 만들고 채움용 RectTransform을 반환</summary>
    private RectTransform MakeGauge(RectTransform parent, string name, float y, Color color)
    {
        RectTransform bg = MakeBox(parent, name + "BG", new Color(0f, 0f, 0f, 0.55f));
        bg.anchorMin = new Vector2(0f, 1f);
        bg.anchorMax = new Vector2(1f, 1f);
        bg.pivot = new Vector2(0.5f, 1f);
        bg.offsetMin = new Vector2(16f, 0f);
        bg.offsetMax = new Vector2(-16f, 0f);
        bg.anchoredPosition = new Vector2(0f, y);
        bg.sizeDelta = new Vector2(bg.sizeDelta.x, 14f);
        bg.GetComponent<Image>().raycastTarget = false;

        RectTransform fill = MakeBox(bg, name + "Fill", color);
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(1f, 1f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().raycastTarget = false;
        return fill;
    }

    /// <summary>게이지 채움 비율 적용 (0~1)</summary>
    private void SetFill(RectTransform fill, float ratio)
    {
        if (fill == null) return;
        Vector2 max = fill.anchorMax;
        max.x = Mathf.Clamp01(ratio);
        fill.anchorMax = max;
        fill.offsetMax = new Vector2(0f, fill.offsetMax.y);
    }

    private void ShowPanel() { panelRoot.gameObject.SetActive(true); }
    private void HidePanel() { panelRoot.gameObject.SetActive(false); }

    private void ClearCustomRoot()
    {
        if (customRoot == null) return;
        for (int i = customRoot.childCount - 1; i >= 0; i--)
            Destroy(customRoot.GetChild(i).gameObject);
    }

    // ---------- 이벤트 클래스들이 같이 쓰는 UI 헬퍼 ----------

    /// <summary>단색 사각형 패널 생성</summary>
    public static RectTransform MakeBox(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        Image img = go.AddComponent<Image>();
        img.color = color;
        return rt;
    }

    /// <summary>가운데 정렬 텍스트 생성</summary>
    public static Text MakeText(Transform parent, string name, string content, int size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400f, 40f);
        Text txt = go.AddComponent<Text>();
        txt.text = content;
        txt.font = GetFont();
        txt.fontSize = size;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;
        return txt;
    }

    /// <summary>클릭 가능한 버튼 생성 (재료 줍기용)</summary>
    public static Button MakeButton(Transform parent, string label, Color bgColor, Vector2 pos, Vector2 size)
    {
        RectTransform rt = MakeBox(parent, "Btn_" + label, bgColor);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Button btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = rt.GetComponent<Image>();

        Text txt = MakeText(rt, "Label", label, 18, Color.white);
        RectTransform trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        return btn;
    }

    /// <summary>
    /// 한글 표시 가능한 폰트 확보.
    /// v3.5: 번들 폰트 1순위 - Assets/Resources/Fonts/GameFont.ttf 가 있으면 그걸 쓴다
    /// (OS 폰트 의존 제거: 한글 미탑재 환경에서 UI가 깨지는 문제의 근본 해결)
    /// 없으면 기존처럼 OS 폰트(맑은 고딕 등) -> 내장 폰트 순서로 폴백
    /// </summary>
    public static Font GetFont()
    {
        if (cachedFont != null) return cachedFont;

        // 1순위: 프로젝트에 번들된 폰트 (권장: 둥근모꼴/Galmuri 등 무료 픽셀 한글 폰트)
        Font bundled = Resources.Load<Font>("Fonts/GameFont");
        if (bundled != null) { cachedFont = bundled; return cachedFont; }

        string[] candidates = { "Malgun Gothic", "맑은 고딕", "NanumGothic", "Gulim", "Arial" };
        for (int i = 0; i < candidates.Length; i++)
        {
            Font f = Font.CreateDynamicFontFromOSFont(candidates[i], 20);
            if (f != null) { cachedFont = f; return cachedFont; }
        }
        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return cachedFont;
    }
}
