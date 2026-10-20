using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// [KitchenEventManager.cs] v4.15 (v9.27 2026-10-20 튜토리얼 개편 - 견습 구간 4 "주방 사고 막기": TutorialDirector.ForcedEventsOnly 동안 견습 중에도 디렉터가 ForceEvent 로 일으킨 사고를 돌린다 (무작위 사고는 없음), 끝난 사고 수·결과를 공개 (EventsEnded · LastEventSuccess - 디렉터가 성공·실패를 센다), 첫 등장 카드는 견습 전체에서 기록 없이 (자유 연습과 같게 - 정식 운행에서 다시 뜬다), 견습에선 제한 시간 x GameBalance.TutorialEventTimeMul) / v4.14 (v9.24.1 2026-10-16 5차 피드백 8번: 위치형 사고(침입·고장·화재)는 위치 먼저 (GameBalance.EventLocationFirst) - 현장 고리·화살표를 키우고(EventSiteScaleAway) 현장 위에 "화재 12초", 셰프 머리 위에 현장 쪽 세모. 배너·현장 카드·전체 화면 그림·처음 보는 사고의 설명 카드는 셰프가 현장에 처음 닿는 순간에 뜬다. 경보음·가장자리 붉은빛·배경음 낮춤은 처음부터. 현장을 벗어나면 안내 줄 "현장으로 돌아가". 사고 종류 키를 EventKey 하나로) / v4.13 (v9.21.2 2026-10-14 문구 팩: 용어 통일 - 접시->요리(개) / 데미지->피해 / 슬롯->칸 / 랜덤->무작위 / 레시피->요리 / 적·아군->손님 / 드랍->드롭 / 마법 저항->저항 / 힐러·러시·청구 / 진화 조리 판 이름) / v4.12 (v9.20 2026-10-07: 글 속 화살표를 게임 글꼴에 있는 세모로(게임 글꼴에 화살표 글리프가 없어 PC 의 다른 글꼴로 그려졌다 - 모양이 따로 놀고, 대신할 글꼴이 없는 빌드에선 빈칸)) / v4.11 (v9.18 2026-10-06: 경광등 2개를 기차 상황판 양옆으로(구 자리는 커진 좌우 판 위) / 실패 피해를 기차 최대 HP 에 맞춰 키우고 방어력으로 깎지 않는다(EventFailScaleOn - "HP 2800 에 19 닳던 것") / 웨이브가 끝나 사고 수습을 기다리는 동안(WaveManager.PostWaveHold) 새 사고를 시작하지 않는다 / MakeText 최소 글자 크기 / AnchorCanvasX 를 캔버스 실제 폭으로 - 1920 고정이라 현장 그림이 바깥으로 밀려 있었다) / v4.10 (v9.16 2026-09-29 손맛 2차 - 소리: 사고 시작 = 경보 + 종류별 소리(SoundKeys.Event: 침입 발소리 / 고장 쇠 삐걱 / 화재 불붙음 / 흘림 쏟아짐) + 배경음 덕킹 / 끝나면 덕킹 해제 / 결과 sfx_event_resolve·sfx_event_fail) / v4.9 (v9.15 2026-09-29 2차 피드백: 사고 해결 연출 - 현장 초록 링 2겹 + "해결!" + 화면 테두리 초록 한 번 + 배너가 초록 "해결 - 기차 HP +n"으로 바뀌고 EventResolveHoldSec 뒤 내려감 / 실패는 붉은 "실패" + "[사고 실패] ... 기차 HP -n" 한 줄 (훈련장에서 결과를 알게) / ForceEvent(kind) - 자유 연습 [2]~[5] / 자유 연습 중 첫 등장 카드는 세션당 1회) / v4.8 (v9.14 2026-09-28 테스터 반영: 사고 현장에 월드 마커(붉은 화살표 + 링) - "직접 가야 한다는 인식 부족" / 흘림 조각이 사고 배너 뒤에 가려지던 것 - 커스텀 층을 배너 위로) / v4.7 (v9.12 2026-09-22: 인라인 연습 중 사고 타이머 정지) / v4.6 (v9.11 2026-09-22: MakeButton 에 ButtonFeel) / v4.5 (v9.10 2026-09-17 테스터 피드백 "사고 중에 증강 선택이 뜨면 사고가 끝난다": 웨이브가 끝나 정차로 넘어가며 사고가 취소될 때 "정차 정비로 사고가 정리됐다 (벌점 없음)" 알림 - 조용히 사라지던 것) / v4.4 (v9.9.2 2026-09-16: 사고 종류별 첫 등장 카드 - StartEvent 에서 BriefingUI.ShowOnce("event_<종류>"), 카드가 뜨면 시간이 멈춰 제한 시간은 그 뒤 흐른다) / v4.3 (v9.9 2026-09-16: 견습 운행 중 F11 무시 - 이벤트 자체는 WaveManager.TutorialGateActive 로 쉰다) / v4.2 (v9.8.1: F11 강제 발생은 GameBalance.CheatsAllowed 일 때만) / v4.1 (2026-09-14: 마모 off 가중치 / 프롤로그 게이트 차단) / v4
/// 주방 돌발 이벤트 총괄 매니저 (기획 B-4)
/// - v4 (v9.6, 2026-09-09): "화면 전체 경보" - 기차 안 작은 아이콘은 조리하다 놓친다는 피드백
///   * 경보 글로우: 화면 가장자리 붉은(이벤트별 색) 비네트가 0.6초 주기로 맥동 (삐뽀삐뽀). SetAlarm(color, strength)
///   * 구리 경광등 2개가 화면 위에서 번갈아 깜빡 + 이벤트 시작 사이렌 sfx_alarm (클립 없으면 조용히 무시)
///   * OverlayRoot: 각 이벤트가 화면 전체 연출(발톱 자국 / 불의 벽 / 노이즈 줄)을 붙이는 전체 화면 영역 (CustomRoot 아래 층)
///   * 배너를 HUD(184) 위 288 로 올림 (불의 벽 96px 자리) + 스킨: 평판 + 빨간 테 + 위험 스트라이프 + 게이지 유리 채움
///   * 스킨 그림(ui_ev_*.png)이 없으면 v3 단색 UI 그대로 (SkinReady 로 분기)
///   이벤트 발생 주기 / 가중치 / 보상 / 페널티 / 앵커 로직은 v3.5 와 동일
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

    /// <summary>v4.15: 이 장면에서 끝난(성공·실패) 사고 수 - 견습 구간 4 가 "끝났나"를 이걸로 본다 (취소는 안 센다)</summary>
    public static int EventsEnded { get; private set; }
    /// <summary>v4.15: 마지막으로 끝난 사고의 결과</summary>
    public static bool LastEventSuccess { get; private set; }

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

    // ---------- v4 경보 레이어 ----------
    private RectTransform overlayRoot;        // 화면 전체 연출 영역 (발톱 / 불의 벽 / 노이즈) - CustomRoot 아래
    private Image alarmVignette;              // 가장자리 글로우 (전체 화면, 알파 맥동)
    private Image[] beacons = new Image[2];   // 경광등 2개
    private Image bannerRing;                 // 배너 테 (스킨)
    private Color alarmColor = new Color(0.84f, 0.16f, 0.16f, 1f);
    private float alarmStrength = 0f;         // 0 = 꺼짐. 이벤트가 SetAlarm 으로 켠다
    private float alarmTime = 0f;
    private const float ALARM_PERIOD = 0.6f;  // 삐뽀삐뽀 주기 (초)
    private const float BEACON_BLINK = 0.3f;  // 경광등 교대 주기 (초)
    private const float BANNER_H = 144f;      // 배너 높이
    /// <summary>
    /// 배너 아래 끝 (화면 아래에서, 캔버스 단위). 배너 위 끝이 화면 높이의 40% = 기본 줌에서 기차 바닥 선 바로 아래에 오게 잡는다.
    /// 구: 288 고정 (UI 배율 1.0 에서 맞춘 값 = HUD 184 + 불의 벽 96 + 8) - UI 배율을 올릴수록 배너가 기차 남쪽 절반(남쪽 포탑·조리대) 위로 올라왔다.
    /// UI 배율 1.0 = 288 (그대로) / 1.12 = 242 / 1.2 = 216. 하단 HUD(184) 아래로는 안 내려간다
    /// </summary>
    private static float BannerY { get { return Mathf.Max(184f + 8f, UIFactory.RefResolution.y * 0.4f - BANNER_H); } }

    private static bool skinChecked, skinReady;

    /// <summary>v4: 경보 그림(ui_ev_vignette 등)과 UI 스킨이 있는가 - 이벤트 클래스들이 그림/단색을 고를 때 본다</summary>
    public static bool SkinReady
    {
        get
        {
            if (!skinChecked)
            {
                skinChecked = true;
                skinReady = UISkin.Available && SpriteBank.Has("ui_ev_vignette") && SpriteBank.Has("ui_ev_claw")
                    && SpriteBank.Has("ui_ev_firewall_0") && SpriteBank.Has("ui_ev_fire_0") && SpriteBank.Has("ui_mat_meat");
                if (UISkin.Available && !skinReady)
                    Debug.LogWarning("[주방이벤트] ui_ev_*.png / ui_mat_*.png 가 빠져 이벤트는 단색 UI로 표시 (Resources/Sprites/WDT 확인)");
            }
            return skinReady;
        }
    }

    /// <summary>v4: 화면 전체 연출을 붙일 부모 (전체 화면 크기, CustomRoot 아래 층)</summary>
    public RectTransform OverlayRoot { get { return overlayRoot; } }

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

    // v4.8: 사고 현장 월드 마커 (TutorialDirector 마커와 같은 그림 - 붉은색)
    private Transform siteRoot;
    private Transform siteArrowTf, siteRingTf;
    private SpriteRenderer siteArrowSr, siteRingSr;
    private static readonly Color SITE_RED = new Color(1f, 0.38f, 0.32f, 1f);
    private static readonly Color RESOLVE_GREEN = new Color(0.38f, 0.8f, 0.43f, 1f);   // v4.9
    private static readonly Color FAIL_RED = new Color(0.9f, 0.25f, 0.2f, 1f);
    private Coroutine resultRoutine;                                     // v4.9: 결과 배너 내려가기
    private Color resultAlarmRestore = new Color(0.84f, 0.16f, 0.16f, 1f);   // v4.9: 결과 글로우 전의 경보 색 (되돌린다)
    private Color titleBaseColor = new Color(1f, 0.78f, 0.32f);
    private readonly HashSet<string> sandboxSeen = new HashSet<string>();   // v4.9: 자유 연습 중 첫 등장 카드 (세션당 1회)
    // v4.14 (v9.24.1): 위치 먼저 - 셰프가 현장에 처음 닿기 전까지 배너·현장 카드·전체 화면 그림·설명 카드를 미룬다
    private bool awaitingArrival;             // 참 = 아직 현장에 한 번도 안 닿았다 (위치 표시만 보인다)
    private string pendingBriefKey;           // 닿을 때 보여 줄 첫 등장 카드 (없으면 null)
    private Text siteLabel;                   // 현장 위 "화재 12초"
    private Text chefCue;                     // 셰프 머리 위 현장 쪽 세모
    private const float CHEF_HEAD_Y = 1.85f;  // 셰프 머리 위 (발밑 피벗 -0.35 + 그림 높이 약 1.9u)
    private const int SITE_ARROW_ORDER = 7;   // 셰프(0+)·포탑 위
    private const int SITE_RING_ORDER = -3;   // 갑판(-6~-4) 위, 조리대 아래

    /// <summary>앵커의 캔버스 X 좌표 (이벤트 아이콘 배치용. 앵커 없으면 0)</summary>
    public float AnchorCanvasX()
    {
        if (!HasAnchor || Camera.main == null) return 0f;
        float screenX = Camera.main.WorldToScreenPoint(new Vector3(AnchorX, 0f, 0f)).x;
        // v4.11: 캔버스의 실제 폭으로 바꾼다. 구: 1920 고정 - UI 배율(v9.14)을 넣은 뒤로 캔버스 폭은 1920 / UIScale 이라
        //   현장 그림(불길·발톱·고장 카드)이 화면 가운데에서 멀수록 실제 자리보다 바깥으로 밀려 있었다
        RectTransform crt = canvas != null ? canvas.transform as RectTransform : null;
        float canvasW = crt != null && crt.rect.width > 1f ? crt.rect.width : UIFactory.RefResolution.x;
        float canvasX = (screenX / Mathf.Max(1f, Screen.width) - 0.5f) * canvasW;
        float limit = Mathf.Max(100f, canvasW * 0.5f - 260f);
        return Mathf.Clamp(canvasX, -limit, limit);
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

    // ─────────────────────────────────────────────
    // v4.8: 사고 현장 마커 - 붉은 화살표(까딱) + 붉은 링(맥동). 셰프가 현장에 닿으면 링만 남고 화살표는 내려간다
    // ─────────────────────────────────────────────
    private void ShowSiteMarker()
    {
        if (siteRoot == null)
        {
            Sprite arrow = SpriteBank.Get("tut_arrow");
            Sprite ring = SpriteBank.Get("tut_ring_l");
            if (ring == null) ring = SpriteBank.Get("tut_ring");   // v4.14: UnityEngine.Object 에 ?? 를 쓰지 않는다
            if (arrow == null && ring == null) return;   // PNG 없으면 배너 화살표(→→)만
            siteRoot = new GameObject("EventSiteMarker").transform;
            if (ring != null)
            {
                siteRingSr = PixelPainter.Attach(siteRoot, "Ring", ring, Vector3.zero, SITE_RING_ORDER);
                siteRingTf = siteRingSr.transform;
                siteRingSr.color = SITE_RED;
            }
            if (arrow != null)
            {
                siteArrowSr = PixelPainter.Attach(siteRoot, "Arrow", arrow, Vector3.zero, SITE_ARROW_ORDER);
                siteArrowTf = siteArrowSr.transform;
                siteArrowSr.color = SITE_RED;
                siteArrowTf.localScale = new Vector3(1.5f, 1.5f, 1f);   // 견습 마커보다 크게 - 전투 중에 눈에 띄어야 한다
            }
        }
        siteRoot.gameObject.SetActive(true);
        TickSiteMarker();
    }

    private void HideSiteMarker()
    {
        if (siteRoot != null) siteRoot.gameObject.SetActive(false);
    }

    private void TickSiteMarker()
    {
        if (siteRoot == null || !siteRoot.gameObject.activeSelf || !HasAnchor) return;
        float t = Time.unscaledTime;
        // 링: 기차 중심선 (갑판 바닥), 0.8초 주기로 맥동
        float pulse = 1f + 0.12f * Mathf.Sin(t * Mathf.PI * 2f / 0.8f);
        float awayMul = awaitingArrival ? GameBalance.EventSiteScaleAway : 1f;   // v4.14: 아직 안 닿았으면 크게 (위치 먼저)
        if (siteRingTf != null)
        {
            siteRingTf.position = new Vector3(AnchorX, 0f, 0.05f);
            siteRingTf.localScale = new Vector3(pulse * awayMul, pulse * awayMul, 1f);
        }
        // 화살표: 현장 위 1.6u 에서 까딱 (0.5초 주기 0.25u). 셰프가 닿으면 작게
        if (siteArrowTf != null)
        {
            float bob = 0.25f * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / 0.5f));
            float k = ChefInReach ? 0.9f : 1.5f * awayMul;
            siteArrowTf.position = new Vector3(AnchorX, 1.6f + bob, 0f);
            siteArrowTf.localScale = new Vector3(k, k, 1f);
        }
    }

    // ─────────────────────────────────────────────
    // v4.14 (v9.24.1): 위치 먼저 - 현장 위 이름표 "화재 12초" + 셰프 머리 위 현장 쪽 세모 (화면 캔버스, 배너·현장 카드와 따로)
    // ─────────────────────────────────────────────

    /// <summary>사고 종류 키 (첫 등장 카드·현장음 이름). 흘림은 "spill"</summary>
    private static string EventKey(IKitchenEvent ev)
    {
        return ev is MonsterIntrusionEvent ? "intrusion" : ev is EquipmentBreakEvent ? "break" : ev is KitchenFireEvent ? "fire" : "spill";
    }

    /// <summary>현장 이름표에 쓰는 짧은 이름</summary>
    private static string ShortName(IKitchenEvent ev)
    {
        if (ev is MonsterIntrusionEvent) return "침입자";
        if (ev is EquipmentBreakEvent) return "기구 고장";
        if (ev is KitchenFireEvent) return "화재";
        return ev != null ? ev.Title : "";
    }

    /// <summary>첫 등장 카드를 지금 보여 준다 (자유 연습은 종류마다 세션당 한 번, 운행은 기록에 한 번). 띄웠으면 true</summary>
    private bool ShowBriefingNow(string key)
    {
        if (TutorialDirector.Active)
        {
            // v4.9: 자유 연습에서는 기록과 무관하게 종류마다 한 번 보여 준다 (실패하면 어떻게 되는지가 카드에 있다). v4.15: 견습 구간 4 도 같게 - 정식 운행에서 처음 만나면 또 뜬다
            if (!sandboxSeen.Add(key)) return false;
            BriefingUI.Show(BriefingTexts.Event(key));
            return true;
        }
        return BriefingUI.ShowOnce("event_" + key, BriefingTexts.Event(key));
    }

    /// <summary>월드 좌표 -> 이 캔버스의 가운데 기준 좌표 (AnchorCanvasX 와 같은 셈을 y 까지)</summary>
    private Vector2 WorldToCanvas(Vector3 world)
    {
        if (Camera.main == null) return Vector2.zero;
        Vector3 sp = Camera.main.WorldToScreenPoint(world);
        RectTransform crt = canvas != null ? canvas.transform as RectTransform : null;
        float w = crt != null && crt.rect.width > 1f ? crt.rect.width : UIFactory.RefResolution.x;
        float h = crt != null && crt.rect.height > 1f ? crt.rect.height : UIFactory.RefResolution.y;
        return new Vector2((sp.x / Mathf.Max(1f, Screen.width) - 0.5f) * w, (sp.y / Mathf.Max(1f, Screen.height) - 0.5f) * h);
    }

    /// <summary>현장 카드(CustomRoot)·전체 화면 그림(OverlayRoot)을 켜고 끈다 - 배너는 ShowPanel/HidePanel</summary>
    private void SetEventVisualsActive(bool on)
    {
        if (customRoot != null) customRoot.gameObject.SetActive(on);
        if (overlayRoot != null) overlayRoot.gameObject.SetActive(on);
    }

    private void ShowSiteCue()
    {
        if (siteLabel == null && canvas != null)
        {
            siteLabel = MakeText(canvas.transform, "SiteLabel", "", 34, SITE_RED);
            Outline o = siteLabel.gameObject.AddComponent<Outline>();   // 싸움 화면 위에서도 읽히게 검은 테
            o.effectColor = new Color(0f, 0f, 0f, 0.9f);
            o.effectDistance = new Vector2(2f, -2f);
            chefCue = MakeText(canvas.transform, "ChefCue", "", 40, SITE_RED);
            Outline o2 = chefCue.gameObject.AddComponent<Outline>();
            o2.effectColor = new Color(0f, 0f, 0f, 0.9f);
            o2.effectDistance = new Vector2(2f, -2f);
        }
        if (siteLabel != null) siteLabel.gameObject.SetActive(true);
        if (chefCue != null) chefCue.gameObject.SetActive(true);
        TickSiteCue();
    }

    private void HideSiteCue()
    {
        if (siteLabel != null) siteLabel.gameObject.SetActive(false);
        if (chefCue != null) chefCue.gameObject.SetActive(false);
    }

    private void TickSiteCue()
    {
        if (siteLabel == null) return;
        if (currentEvent == null) { HideSiteCue(); return; }   // 사고 없이 남은 이름표·세모는 거둔다 (방어)
        float t = Time.unscaledTime;
        RectTransform crt = canvas != null ? canvas.transform as RectTransform : null;
        float halfW = (crt != null && crt.rect.width > 1f ? crt.rect.width : UIFactory.RefResolution.x) * 0.5f;
        float halfH = (crt != null && crt.rect.height > 1f ? crt.rect.height : UIFactory.RefResolution.y) * 0.5f;

        // 현장 이름표: 화살표 머리 위 (화살표 = 끝이 발밑인 0.66u 그림 x 크기 1.5 x EventSiteScaleAway, 까딱 0.25u). 화면 밖이면 가장자리에 붙는다
        float arrowTop = 1.6f + 0.25f + 0.66f * 1.5f * GameBalance.EventSiteScaleAway;
        Vector2 p = WorldToCanvas(new Vector3(AnchorX, arrowTop + 0.45f, 0f));
        p.x = Mathf.Clamp(p.x, -halfW + 120f, halfW - 120f);
        p.y = Mathf.Clamp(p.y, -halfH + 40f, halfH - 60f);
        siteLabel.rectTransform.anchoredPosition = p;
        siteLabel.text = ShortName(currentEvent) + (eventTimeMax > 0f ? " " + Mathf.Max(0, Mathf.CeilToInt(eventTimeLeft)) + "초" : "");
        float s = 1f + 0.06f * Mathf.Sin(t * Mathf.PI * 2f / 0.8f);   // 고리와 같은 0.8초 맥동
        siteLabel.rectTransform.localScale = new Vector3(s, s, 1f);

        // 셰프 머리 위 세모: 현장 쪽을 가리키며 그쪽으로 까딱
        if (chefCue != null)
        {
            bool show = chefTransform != null;
            if (chefCue.gameObject.activeSelf != show) chefCue.gameObject.SetActive(show);
            if (show)
            {
                float dir = AnchorX > chefTransform.position.x ? 1f : -1f;
                chefCue.text = dir > 0f ? "▶" : "◀";
                Vector2 c = WorldToCanvas(chefTransform.position + new Vector3(0f, CHEF_HEAD_Y, 0f));
                c.x += dir * 10f * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / 0.5f));
                chefCue.rectTransform.anchoredPosition = c;
            }
        }
    }

    /// <summary>셰프가 현장에 처음 닿았다: 배너·현장 카드·전체 화면 그림·미뤘던 첫 등장 카드</summary>
    private void Arrive()
    {
        awaitingArrival = false;
        HideSiteCue();
        SetEventVisualsActive(true);
        ShowPanel();
        if (pendingBriefKey != null)
        {
            string key = pendingBriefKey;
            pendingBriefKey = null;
            // 처음 보는 사고: 걸어오는 동안 깎인 시간을 반까지 채워 준다 (카드를 읽고 2 ~ 3초 안에 처음 보는 사고를 풀어야 하는 일이 없게)
            if (ShowBriefingNow(key)) eventTimeLeft = Mathf.Max(eventTimeLeft, eventTimeMax * 0.5f);
        }
    }

    /// <summary>사고가 끝나거나 취소될 때 위치 먼저 상태를 되돌린다</summary>
    private void ResetLocationFirst()
    {
        awaitingArrival = false;
        pendingBriefKey = null;
        HideSiteCue();
        SetEventVisualsActive(true);
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

        // v4: 경보 글로우 맥동 + 경광등 교대 (이벤트 진행 중에만 켜져 있다)
        UpdateAlarm();

        if (!eventEnabled) return;

        // v4.7: 인라인 연습(TutorialDirector.InlineFreeze) 중 - 다음 사고 시각을 그만큼 뒤로 민다 (연습이 끝나자마자 사고가 터지지 않게)
        if (TutorialDirector.InlineFreeze)
        {
            if (currentEvent == null) nextEventTime += Time.deltaTime;
            return;
        }

        // v3.2: 증강 선택 / 일시정지 / 정비소가 떠 있는 동안 이벤트 완전 동결
        // (시간이 멈춰도 Update와 키 입력은 살아 있어서, 멈춘 시간에 공짜로
        //  이벤트를 해결하는 꼼수가 가능했음 -> 입력 처리 자체를 차단)
        if (AugmentPickUI.IsOpen || PauseMenu.IsOpen || WorkshopUI.IsOpen
            || AugmentListUI.ReadingOpen)   // A10: 열람 패널(V/J)도 시간을 멈추므로 같은 취급
            return;

        // v3.1: 전투 중이 아니면(로비/마을 정비/게임오버) 이벤트 금지
        // - 진행 중이던 이벤트는 페널티 없이 조용히 정리
        // - 타이머를 firstDelay로 계속 밀어서, 전투 시작 후에도 최소 firstDelay만큼 여유를 준다
        bool inBattle = GameManager.Instance != null
            && GameManager.Instance.currentState == GameManager.GameState.Battle
            && (!WaveManager.TutorialGateActive || TutorialDirector.SandboxActive || TutorialDirector.ForcedEventsOnly);   // v4.1: 프롤로그 조리 게이트 중에는 이벤트 없음. v4.9: 자유 연습은 키로 일으킨 사고를 돌린다. v4.15: 견습 구간 4 도
        if (!inBattle)
        {
            if (currentEvent != null)
            {
                // v4.5: 정차(웨이브 종료)로 사고가 정리되는 것은 버그가 아니라 규칙 - 말로 알린다. 일시정지 메뉴는 상태를 바꾸지 않으므로 여기 안 온다
                bool town = GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Town;
                string title = currentEvent.Title;
                CancelCurrentEvent();
                if (town) UIManager.Instance?.ShowStatChange("[정차] 정비로 사고가 정리됐다 - 벌점 없음 (" + title + ")");
            }
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
        if (debugKeyEnabled && GameBalance.CheatsAllowed && !TutorialDirector.Active && Input.GetKeyDown(KeyCode.F11) && currentEvent == null)
            StartRandomEvent();   // v9.9: 견습 운행 중엔 치트 무시

        // 진행 중인 이벤트가 있으면 그것만 돌린다
        if (currentEvent != null)
        {
            RunCurrentEvent();
            return;
        }

        // v4.9: 자유 연습은 무작위 사고 없음 - [2]~[5] 로만 (ForceEvent). v4.15: 견습 구간 4 도 디렉터가 일으킨 것만
        if (TutorialDirector.SandboxActive || TutorialDirector.ForcedEventsOnly) return;

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

        // v4.11: 웨이브가 끝나 진행 중인 사고의 수습을 기다리는 동안에는 새 사고 없음
        if (WaveManager.PostWaveHold)
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
    /// <summary>v4.9: 종류를 골라 즉시 발생 (자유 연습 [2] 화재 [3] 흘림 [4] 고장 [5] 침입). 진행 중이면 무시</summary>
    public void ForceEvent(int kind)
    {
        if (currentEvent != null) return;
        IKitchenEvent ev;
        switch (kind)
        {
            case 2: ev = new KitchenFireEvent(); break;
            case 3: ev = new MaterialSpillEvent(); break;
            case 4: ev = new EquipmentBreakEvent(); break;
            default: ev = new MonsterIntrusionEvent(); break;
        }
        StartEvent(ev);
    }

    /// <summary>v4.9: 사고가 진행 중인가 (자유 연습 안내 줄)</summary>
    public bool EventRunning { get { return currentEvent != null; } }

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
        // v4.1 (교수 피드백 B3): 마모 off 실험 중에는 마모 항이 0 - 고장 이벤트가 엉뚱하게 늘거나 줄지 않게
        if (!GameBalance.ToolWearEnabled) toolWear = 50f;

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
        { ev = new MonsterIntrusionEvent(); reason = "근처 손님 " + nearbyCount + "마리"; }
        else if (roll < wIntrude + wFire)
        { ev = new KitchenFireEvent(); reason = fireThreat ? "화염 손님 접근" : "무작위"; }
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

        // v4.4: 첫 등장 카드 (종류별 1회). 견습 운행 중엔 이벤트 자체가 막혀 있다
        //   v4.14: 키만 정해 두고, 앵커·셰프 거리를 정한 뒤에 바로 보여 주거나(현장 곁) 셰프가 닿을 때로 미룬다(위치 먼저)
        string briefKey = GameBalance.FirstEncounterBriefings ? EventKey(ev) : null;

        // v4.9: 결과 배너가 아직 내려가는 중이면 즉시 정리 (글로우 색도 원래대로 - 새 사고의 OnStart 가 SetAlarm 을 안 부를 수도 있다)
        if (resultRoutine != null) { StopCoroutine(resultRoutine); resultRoutine = null; RestoreBanner(); SetAlarm(resultAlarmRestore, 0f); }
        ResetLocationFirst();   // v4.14: 앞 사고의 위치 먼저 상태가 남았으면 정리 (방어 - 끝·취소가 늘 거두지만)

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
            ShowSiteMarker();   // v4.8: 현장에 붉은 화살표 + 링
        }
        UpdateChefReach();

        // v4.14: 위치 먼저 - 현장에서 멀면 현장 표시만 먼저 (배너·현장 카드·전체 화면 그림·설명 카드는 닿을 때)
        awaitingArrival = HasAnchor && GameBalance.EventLocationFirst && !ChefInReach;
        pendingBriefKey = null;
        if (briefKey != null)
        {
            if (awaitingArrival) pendingBriefKey = briefKey;
            else ShowBriefingNow(briefKey);
        }

        // 이벤트가 누적될수록 조금씩 어려워진다 (최대 +100%)
        float difficulty = Mathf.Min(1f, firedCount * 0.08f);

        ClearCustomRoot();
        ev.OnStart(this, difficulty);

        // 위치형 이벤트는 달려가는 시간만큼 제한시간에 여유를 준다. v4.15: 견습(자유 연습 제외)은 배율만큼 더
        eventTimeMax = ev.TimeLimit + (HasAnchor ? GameBalance.EventReachGrace : 0f);
        if (TutorialDirector.Active && !TutorialDirector.SandboxActive) eventTimeMax *= Mathf.Max(1f, GameBalance.TutorialEventTimeMul);
        eventTimeLeft = eventTimeMax;

        titleText.text = ev.Title;
        guideText.text = ev.Guide;
        if (awaitingArrival)
        {
            // v4.14: 현장 카드·전체 화면 그림은 OnStart 가 만들어 둔 채 숨긴다 (닿는 순간 Arrive 가 켠다)
            SetEventVisualsActive(false);
            HidePanel();
            ShowSiteCue();
        }
        else
        {
            SetEventVisualsActive(true);
            ShowPanel();
        }

        // v4: 사이렌 (클립 없으면 조용히 무시) - 이벤트가 SetAlarm 을 안 불렀으면 기본 빨강 경보
        // v4.10: 경보 + 종류별 현장음 + 배경음 덕킹 (사고 동안 -35%). v4.14: 위치 먼저여도 처음부터 (급하다는 건 바로 알아야 한다)
        SoundManager.Play("sfx_alarm");
        SoundManager.PlayDelayed(SoundKeys.Event(EventKey(ev)), 0.25f);
        SoundManager.BgmDuck("event", true);
        if (alarmStrength <= 0f) SetAlarm(new Color(0.84f, 0.16f, 0.16f, 1f), 0.45f);

        Debug.Log("[주방이벤트] 발생: " + ev.Title);
    }

    private void RunCurrentEvent()
    {
        float dt = Time.deltaTime;
        eventTimeLeft -= dt;

        // B-1: 셰프-앵커 근접 갱신 (각 이벤트가 ChefInReach로 입력을 게이트)
        UpdateChefReach();
        if (awaitingArrival)
        {
            if (ChefInReach)
            {
                Arrive();   // v4.14: 처음 닿았다 - 배너·현장 카드·설명 카드
                if (BriefingUI.IsOpen) return;   // 설명 카드가 떴다 - 이 프레임의 사고 입력은 넘긴다 (카드가 닫힌 뒤부터)
            }
            else TickSiteCue();
        }
        TickSiteMarker();   // v4.8

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

        // B-1: 현장 밖이면 방향 화살표 + 현장 안내가 가이드를 대신한다 (v4.14: 위치 먼저면 배너는 한 번 닿은 뒤에만 보이니 "돌아가")
        if (HasAnchor && !ChefInReach && chefTransform != null)
        {
            string arrow = AnchorX > chefTransform.position.x ? "▶▶" : "◀◀";
            guideText.text = arrow + (GameBalance.EventLocationFirst ? " 현장으로 돌아가 " : " 현장으로 달려가 ") + arrow + "   " + currentEvent.Guide;
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
        HideSiteMarker();
        ResetLocationFirst();   // v4.14

        ClearCustomRoot();
        ClearOverlay();
        SetAlarm(alarmColor, 0f);
        HidePanel();
        SoundManager.BgmDuck("event", false);   // v4.10
        Debug.Log("[주방이벤트] 전투 종료로 취소: " + ev.Title);
    }

    private void EndCurrentEvent(bool success)
    {
        IKitchenEvent ev = currentEvent;
        currentEvent = null;
        EventsEnded++; LastEventSuccess = success;   // v4.15: 맨 앞에서 - 뒤 연출이 어떻게 끝나든 디렉터가 센다
        bool hadAnchor = HasAnchor; float siteX = AnchorX;
        HasAnchor = false; ChefInReach = true;   // B-1: 앵커 정리
        HideSiteMarker();
        ResetLocationFirst();   // v4.14: 못 닿고 끝났으면 미룬 설명 카드는 버린다 (다음에 같은 사고가 나면 그때)

        if (cachedTrain == null) cachedTrain = Object.FindFirstObjectByType<TrainManager>();
        float hpBefore = cachedTrain != null ? cachedTrain.currentHP : 0f;
        ev.OnEnd(success);
        float hpDelta = cachedTrain != null ? cachedTrain.currentHP - hpBefore : 0f;
        ClearCustomRoot();
        ClearOverlay();
        SetAlarm(alarmColor, 0f);
        HidePanel();
        SoundManager.BgmDuck("event", false);   // v4.10

        // v4.9: 결과 연출 - 해결(초록) / 실패(붉음). 원인과 결과가 화면에서 이어진다
        if (GameBalance.EventResolveFx) PlayResult(ev, success, hadAnchor, siteX, hpDelta);
        else if (!success) UIManager.Instance?.ShowDanger("[사고 실패] " + ev.Title + (hpDelta < 0f ? " - 기차 HP " + Mathf.RoundToInt(hpDelta) : ""));

        // 발생 간격 배율 (Phase 2-3: '부채질 장인의 부채'는 아이템으로 이관 - 증강 값은 호환용)
        nextEventTime = Time.time + Random.Range(minInterval, maxInterval)
            * AugmentManager.EventIntervalMul * ItemManager.EventIntervalMul;
        Debug.Log("[주방이벤트] 종료: " + ev.Title + " / 결과 " + (success ? "성공" : "실패"));
    }

    // ==================================================================
    //  v4.9: 결과 연출 (해결 / 실패)
    // ==================================================================

    /// <summary>
    /// 해결: 현장에 초록 링 2겹 + 불티 + "해결!" 팝, 화면 테두리 초록 한 번(경보 글로우 재사용), 배너가 초록 "해결 - 기차 HP +n" 으로
    /// 바뀌어 EventResolveHoldSec 뒤 내려간다. 실패: 붉은 "실패" 팝 + 배너 "실패 - 기차 HP -n" + 경고 한 줄
    /// </summary>
    private void PlayResult(IKitchenEvent ev, bool success, bool hadAnchor, float siteX, float hpDelta)
    {
        Color col = success ? RESOLVE_GREEN : FAIL_RED;
        string hpStr = hpDelta > 0.5f ? "기차 HP +" + Mathf.RoundToInt(hpDelta) : hpDelta < -0.5f ? "기차 HP " + Mathf.RoundToInt(hpDelta) : "";

        // 현장 (앵커가 있는 사고: 침입·화재·고장. 흘림은 화면 전체라 배너만)
        if (hadAnchor && GameBalance.GameFeelMaster > 0f)
        {
            Vector3 pos = new Vector3(siteX, 0f, 0f);
            if (success)
            {
                WorldFeel.Ring(pos, col, 1.3f, 0.5f);
                WorldFeel.Ring(pos, col, 0.7f, 0.35f);
                SparkPool.Emit(pos, new Color(0.7f, 1f, 0.65f), 12, 0.1f, 3f);
                WorldFeel.TextPop(pos + Vector3.up * 0.9f, "해결!", col, 4.2f);
            }
            else
            {
                WorldFeel.Ring(pos, col, 0.9f, 0.4f);
                WorldFeel.TextPop(pos + Vector3.up * 0.9f, "실패", col, 4.2f);
            }
        }

        // 화면 테두리 한 번 (경보 글로우를 초록/붉음으로 켰다가 배너와 같이 끈다. 색은 끌 때 원래대로)
        resultAlarmRestore = alarmColor;
        if (alarmVignette != null) SetAlarm(col, success ? 0.4f : 0.3f);

        // 배너: 결과 상태로 바꿔 잠깐 보여 준다
        titleText.text = ev.Title + (success ? " - 해결!" : " - 실패");
        titleText.color = col;
        guideText.text = success ? (string.IsNullOrEmpty(hpStr) ? "잘 막았다" : hpStr) : (string.IsNullOrEmpty(hpStr) ? "못 막았다" : hpStr);
        SetFill(gaugeFill, success ? 1f : 0f);
        SetFill(timeFill, 0f);
        if (eventHpText != null && cachedTrain != null)
            eventHpText.text = "기차 HP  " + Mathf.RoundToInt(cachedTrain.currentHP) + " / " + Mathf.RoundToInt(cachedTrain.currentMaxHP);
        if (bannerRing != null) bannerRing.color = col;
        ShowPanel();
        if (resultRoutine != null) StopCoroutine(resultRoutine);
        resultRoutine = StartCoroutine(ResultBannerDown());

        SoundManager.Play(success ? "sfx_event_resolve" : "sfx_event_fail");   // v4.10: 전용 결과음
        if (success) UIManager.Instance?.ShowStatChange("[사고 해결] " + ev.Title + (string.IsNullOrEmpty(hpStr) ? "" : " - " + hpStr));
        else UIManager.Instance?.ShowDanger("[사고 실패] " + ev.Title + (string.IsNullOrEmpty(hpStr) ? "" : " - " + hpStr));
    }

    private IEnumerator ResultBannerDown()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, GameBalance.EventResolveHoldSec));
        // 그 사이 새 사고가 시작됐으면 그쪽 배너다 - 건드리지 않는다
        if (currentEvent == null) { HidePanel(); SetAlarm(resultAlarmRestore, 0f); }
        RestoreBanner();
        resultRoutine = null;
    }

    /// <summary>결과 상태로 바꿨던 배너 색을 원래대로</summary>
    private void RestoreBanner()
    {
        titleText.color = titleBaseColor;
        if (bannerRing != null) bannerRing.color = UISkin.HP_RED;
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

        // v4.11 (유저 10-06 "HP 2800 인데 실패해도 19 닳는다"): 사고 표의 값은 기차 HP 500 기준이다.
        //   구 동작은 그 고정값에서 방어력을 빼서(TakeDamage) 정비소로 HP·방어를 올릴수록 사고가 공짜가 됐다.
        //   지금은 최대 HP 비율로 키우고 방어력을 건너뛴다 (예: 침입 실패 60 = 최대 HP 의 12%). 받는 피해 감소(%)와 증기 보호막은 그대로 먹는다
        if (GameBalance.EventFailScaleOn)
        {
            float scale = Mathf.Max(1f, tm.currentMaxHP / Mathf.Max(1f, GameBalance.EventFailRefHP));
            tm.TakeDamageIgnoringDef(amount * scale);
        }
        else tm.TakeDamage(amount);
    }

    /// <summary>
    /// v4.11: 사고가 진행되는 동안 조금씩 들어오는 피해 (화재가 타는 동안의 틱). 표의 값 그대로 - 최대 HP 비례는 "실패 한 방"(DamageTrain)에만 건다.
    /// 방어력은 건너뛴다: 틱은 값이 작아(2.5 ~ 5) 방어력을 빼면 언제나 1 이 됐다
    /// </summary>
    public void DamageTrainTick(float amount)
    {
        amount *= AugmentManager.EventPenaltyMul * ItemManager.EventPenaltyMul;
        TrainManager tm = Object.FindFirstObjectByType<TrainManager>();
        if (tm == null) return;
        if (GameBalance.EventFailScaleOn) tm.TakeDamageIgnoringDef(amount);
        else tm.TakeDamage(amount);
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
        scaler.referenceResolution = UIFactory.RefResolution;   // v9.14: UI 전체 배율 (GameBalance.UIScale)
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // v4: 경보 글로우 (맨 아래 층, 전체 화면. 스킨 없으면 만들지 않는다)
        if (SkinReady)
        {
            RectTransform vrt = MakeBox(canvasGo.transform, "AlarmVignette", new Color(1f, 1f, 1f, 0f));
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
            alarmVignette = vrt.GetComponent<Image>();
            alarmVignette.sprite = SpriteBank.Get("ui_ev_vignette");
            alarmVignette.type = Image.Type.Simple;
            alarmVignette.raycastTarget = false;
            alarmVignette.enabled = false;
        }

        // v4: 화면 전체 연출 영역 (발톱 / 불의 벽 / 노이즈) - 커스텀 영역 아래
        overlayRoot = MakeBox(canvasGo.transform, "OverlayRoot", new Color(0f, 0f, 0f, 0f));
        overlayRoot.anchorMin = Vector2.zero; overlayRoot.anchorMax = Vector2.one;
        overlayRoot.offsetMin = Vector2.zero; overlayRoot.offsetMax = Vector2.zero;
        overlayRoot.GetComponent<Image>().raycastTarget = false;

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
        panelRoot.anchoredPosition = new Vector2(0f, BannerY);   // v4.11: 기차 바닥 선 바로 아래 (UI 배율을 따라 내려간다)
        panelRoot.sizeDelta = new Vector2(760f, BANNER_H);
        Image panelImg = panelRoot.GetComponent<Image>();
        panelImg.raycastTarget = false;
        customRoot.SetAsLastSibling();   // v4.8: 흘림 조각·이벤트 부품이 배너 뒤에 가려지지 않게 (테스터 "떨어진 재료가 창에 가려 안 보임")
        if (SkinReady)
        {
            // 스킨: 무쇠 평판 + 빨간 리벳 테 + 위험 스트라이프 양끝 (스캐너가 건드리지 않게 직접 지정)
            UISkin.Plate(panelImg, Color.white);
            bannerRing = UISkin.AddRing(panelRoot, UISkin.HP_RED);
            UISkin.AddOrnament(panelRoot, "hazard", new Vector2(0f, 1f), new Vector2(16f, -20f), new Vector2(56f, 16f));
            UISkin.AddOrnament(panelRoot, "hazard", new Vector2(1f, 1f), new Vector2(-72f, -20f), new Vector2(56f, 16f));
        }

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

        // v4: 구리 경광등 2개 (화면 위, 예고 카드 양옆) - 이벤트 중에만 켜져 번갈아 깜빡
        if (SkinReady)
        {
            for (int i = 0; i < 2; i++)
            {
                RectTransform brt = MakeBox(canvasGo.transform, "Beacon_" + i, Color.white);
                brt.anchorMin = new Vector2(0.5f, 1f); brt.anchorMax = new Vector2(0.5f, 1f);
                // v4.11: 기차 상황판(폭 460, 위에서 8 ~ 42) 양옆. 구 (±444, -44) 는 씬 HUD 가 UI 배율을 따르면서 커진 좌상단·우상단 판 위에 올라탔다
                brt.anchoredPosition = new Vector2(i == 0 ? -270f : 270f, -26f);
                brt.sizeDelta = new Vector2(32f, 32f);
                beacons[i] = brt.GetComponent<Image>();
                beacons[i].sprite = SpriteBank.Get("ui_ev_beacon_0");
                beacons[i].raycastTarget = false;
                beacons[i].enabled = false;
            }
        }
    }

    // ==================================================================
    //  v4 경보 레이어
    // ==================================================================

    /// <summary>경보 글로우 색과 세기(0~1). 0 이면 끈다. 이벤트 OnStart 에서 부른다 (안 부르면 기본 빨강 0.45)</summary>
    public void SetAlarm(Color color, float strength)
    {
        alarmColor = color;
        alarmStrength = Mathf.Clamp01(strength);
        if (alarmStrength <= 0f)
        {
            if (alarmVignette != null) alarmVignette.enabled = false;
            for (int i = 0; i < 2; i++) if (beacons[i] != null) beacons[i].enabled = false;
        }
        else
        {
            if (alarmVignette != null) alarmVignette.enabled = true;
            for (int i = 0; i < 2; i++) if (beacons[i] != null) beacons[i].enabled = true;
            alarmTime = 0f;
        }
    }

    private void UpdateAlarm()
    {
        if (alarmStrength <= 0f || alarmVignette == null) return;
        alarmTime += Time.unscaledDeltaTime;
        // 삐뽀삐뽀: 0.6초 주기로 진했다 옅었다 (최소 35%는 유지해서 "경보 중" 상태가 항상 보인다)
        float pulse = 0.5f + 0.5f * Mathf.Sin(alarmTime * (2f * Mathf.PI / ALARM_PERIOD));
        float a = alarmStrength * (0.35f + 0.65f * pulse);
        alarmVignette.color = new Color(alarmColor.r, alarmColor.g, alarmColor.b, a);

        int phase = Mathf.FloorToInt(alarmTime / BEACON_BLINK) % 2;
        for (int i = 0; i < 2; i++)
        {
            if (beacons[i] == null) continue;
            bool on = (i == phase);
            beacons[i].sprite = SpriteBank.Get(on ? "ui_ev_beacon_0" : "ui_ev_beacon_1");
        }
    }

    private void ClearOverlay()
    {
        if (overlayRoot == null) return;
        for (int i = overlayRoot.childCount - 1; i >= 0; i--)
            Destroy(overlayRoot.GetChild(i).gameObject);
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
        Image fillImg = fill.GetComponent<Image>();
        fillImg.raycastTarget = false;
        if (SkinReady)
        {
            // v4: 검정 홈 안에 유리 채움 (조리 미니게임 압력 게이지와 같은 조각)
            bg.GetComponent<Image>().color = new Color(0.055f, 0.047f, 0.07f, 1f);
            fillImg.sprite = SpriteBank.Get("ui_gauge_fill");
            fillImg.type = Image.Type.Sliced;
            fill.offsetMin = new Vector2(2f, 2f);
            fill.offsetMax = new Vector2(-2f, -2f);
        }
        return fill;
    }

    /// <summary>게이지 채움 비율 적용 (0~1)</summary>
    private void SetFill(RectTransform fill, float ratio)
    {
        if (fill == null) return;
        Vector2 max = fill.anchorMax;
        max.x = Mathf.Clamp01(ratio);
        fill.anchorMax = max;
        fill.offsetMax = new Vector2(SkinReady ? -2f : 0f, fill.offsetMax.y);
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
        txt.fontSize = GameBalance.UIFontFloor > 0 ? Mathf.Max(size, GameBalance.UIFontFloor) : size;   // v4.11: 최소 글자 크기
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
        ButtonFeel.Attach(btn);   // v4.6: 호버·프레스 반응

        Text txt = MakeText(rt, "Label", label, 18, Color.white);
        RectTransform trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        return btn;
    }

    // ---------- v4: 스킨 이벤트 클래스들이 같이 쓰는 그림 헬퍼 ----------

    /// <summary>스프라이트 이미지 하나 (앵커/피벗 = 화면 중앙 기준, 클릭 통과). sprite 가 null 이면 흰 사각형이 뜨므로 호출부가 확인할 것</summary>
    public static Image MakeSprite(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>무쇠 평판 + 색 테 카드 (화면 중앙 기준 pos). 반환 = 카드 RectTransform, ring 에 테 이미지</summary>
    public static RectTransform MakeCard(Transform parent, string name, Vector2 pos, Vector2 size, Color ringColor, out Image ring)
    {
        RectTransform rt = MakeBox(parent, name, Color.white);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image plate = rt.GetComponent<Image>();
        plate.raycastTarget = false;
        UISkin.Plate(plate, Color.white);
        ring = UISkin.AddRing(rt, ringColor);
        return rt;
    }

    /// <summary>조작 안내 칩 ("[E] 연타!" 등): 작은 카드 + 글자. 폭은 글자 수로 어림</summary>
    public static RectTransform MakeChip(Transform parent, string name, string label, Vector2 pos, Color ringColor)
    {
        float w = 0f;
        for (int i = 0; i < label.Length; i++) w += label[i] < 128 ? 9f : 15f;
        Image ring;
        RectTransform rt = MakeCard(parent, name, pos, new Vector2(w + 28f, 30f), ringColor, out ring);
        Text t = MakeText(rt, "Label", label, 15, new Color(0.969f, 0.910f, 0.776f));
        RectTransform trt = t.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        return rt;
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
