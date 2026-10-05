using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// [CameraZoom.cs] v5.1 (v9.17 2026-10-06 A11: GameFeel.ZoomMul(보스 처치 때 잠깐 당김)을 부드러운 줌 값에 마지막으로 곱한다 - 줌 상태(smoothZoom)와 화면 값을 분리) / v5 (v9.13 2026-09-23: 선로 v2 - 정차 프레이밍 SetRouteFraming: 선로를 고르는 동안 x RouteStopCamX(-3.6)·줌 x RouteStopZoomMul(1.18) 로 0.6초에 옮겨 두상 앞 갈림길이 다 보이게, 출발하면 되돌린다 /
///   가지로 들어가는 동안 ParallaxBackground.RouteRollDeg 만큼 화면 기울임(Dutch angle) - 기차 데크를 돌리면 셰프 활동 범위·포탑 자리가 어긋나서 카메라를 돌린다)
/// v4.1 (v9.10 2026-09-17: 최대 줌아웃을 GameBalance.CamMaxZoom(14)으로 - "화면 축소하면 셰프가 점") / v4 (B-2: 셰프 소프트 팔로우 - 방향결정 2026-08-31)
/// 마우스 휠로 카메라 줌인/줌아웃합니다.
/// Main Camera 오브젝트에 붙이세요.
/// 줌아웃: 전장 전체 파악 / 줌인: 주방 정밀 조작
///
/// - v4 변경점 (B-2 트레일러 확장):
///   1) 셰프 소프트 팔로우 - 데드존 밖으로 나가면 카메라 X가 따라간다
///      (기차가 4칸이 되면서 화면 한 장에 다 안 들어감 - 몸이 가는 곳이 화면의 중심)
///   2) 카메라 X 이동 한계(CamFollowMinX/MaxX) - 전장이 화면 밖으로 새지 않게
///   3) 기본 줌 7 -> GameBalance.CamDefaultZoom(8.5) - 긴 기차 프레이밍
///   4) 수치 전부 GameBalance (CamFollowChef=false면 기존 기차 고정 추적으로 복귀)
/// - v3: GameFeel.ShakeOffset 최종 적용(basePos 분리) / 줌 리셋 R -> Z
/// - v2: 줌 범위 2~20 / UI 위 휠 무시
///
/// VS 2017 (C# 7.3) 호환 버전입니다.
/// </summary>
public class CameraZoom : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector 설정 (참고용 - 실제 값은 Start에서 강제 적용)
    // ─────────────────────────────────────────────
    [Header("─ 줌 설정 (Start에서 아래 값으로 덮어씀) ─")]
    public float zoomSpeed = 3f;    // 줌 속도
    public float minZoom = 2f;    // 최대 줌인 (작을수록 가까이)
    public float maxZoom = 20f;   // 최대 줌아웃
    public float defaultZoom = 7f;    // 기본 카메라 크기
    public float smoothSpeed = 5f;    // 줌 부드러움

    [Header("─ 카메라 추적 대상 ─")]
    public Transform targetTransform;  // 기차 Transform (Inspector에서 연결)
    public Vector3 offset = new Vector3(0f, 0f, -10f); // 카메라 오프셋

    // ─────────────────────────────────────────────
    // 내부 상태
    // ─────────────────────────────────────────────
    private Camera cam;
    private float targetZoom;
    private float smoothZoom;   // v5.1: 부드럽게 따라가는 줌 값 (여기에 GameFeel.ZoomMul 을 곱해 화면에 넣는다)

    // v3: 셰이크를 제외한 '진짜' 카메라 위치 (셰이크가 추적 Lerp에 섞여 들어가는 것 방지)
    private Vector3 basePos;

    // v5: 정차 프레이밍 (선로 선택) + 화면 기울임
    private static bool routeFrameOn = false;   // BranchRouteUI 가 켜고 WaveManager 출발이 끈다
    private float routeFrameT = 0f;             // 0 = 평소, 1 = 정차 프레이밍 (RouteStopCamSec 에 걸쳐)
    private float rollNow = 0f;                 // 현재 기울임 (도)
    private bool rollApplied = false;

    /// <summary>선로를 고르는 동안 카메라를 앞으로·줌아웃 (true) / 되돌리기 (false). 씬이 바뀌면 저절로 꺼진다</summary>
    public static void SetRouteFraming(bool on)
    {
        routeFrameOn = on;
    }

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────
    // B-2: 셰프 팔로우 대상
    private Transform chefTransform;

    private void Start()
    {
        // 줌 범위 강제 적용 (Inspector에 저장된 구값 무시 - 조절은 여기 숫자로)
        zoomSpeed = 3f;
        minZoom = 2f;      // 주방 정밀 조작용 근접
        maxZoom = GameBalance.CamMaxZoom;     // v4.1: 14 (구 20) - 더 빼면 셰프가 점이 된다
        defaultZoom = GameBalance.CamDefaultZoom;   // B-2: 긴 기차 프레이밍 (8.5)

        cam = GetComponent<Camera>();
        targetZoom = defaultZoom;
        basePos = transform.position;   // v3: 셰이크 없는 기준 위치 초기화
        routeFrameOn = false; routeFrameT = 0f; rollNow = 0f;   // v5: 씬 전환 뒤 정차 프레이밍·기울임 잔존 방지

        smoothZoom = defaultZoom;
        if (cam != null)
            cam.orthographicSize = defaultZoom;

        // 타겟 자동 탐색 (Inspector 미연결 시)
        if (targetTransform == null)
        {
            GameObject trainObj = GameObject.FindGameObjectWithTag("Train");
            if (trainObj != null) targetTransform = trainObj.transform;
        }

        // B-2: 셰프 자동 탐색 (팔로우 대상)
        GameObject chefObj = GameObject.Find("Chef");
        if (chefObj != null) chefTransform = chefObj.transform;

        Debug.Log("[CameraZoom] 카메라 초기화 완료 (줌 " + minZoom + "~" + maxZoom
            + ", 기본 " + defaultZoom + ", 셰프 팔로우 " + (GameBalance.CamFollowChef ? "ON" : "OFF") + ")");
    }

    // ─────────────────────────────────────────────
    // 매 프레임: 줌 + 카메라 위치
    // ─────────────────────────────────────────────
    private void Update()
    {
        // v5: 정차 프레이밍 진행도 (스케일드 시간 - 카드 창이 시간을 멈추면 카메라도 선다)
        routeFrameT = Mathf.MoveTowards(routeFrameT, routeFrameOn ? 1f : 0f,
            Time.deltaTime / Mathf.Max(0.05f, GameBalance.RouteStopCamSec));

        HandleZoom();
        HandleCameraPosition();
        HandleRoll();
    }

    /// <summary>v5: 정차 프레이밍 가중치 0~1 (부드러운 시작·끝)</summary>
    private float RouteFrameEase()
    {
        float t = routeFrameT;
        return t * t * (3f - 2f * t);
    }

    /// <summary>v5: 가지로 들어가는 동안 화면 기울임 (ParallaxBackground.RouteRollDeg 를 부드럽게 따라간다)</summary>
    private void HandleRoll()
    {
        float want = ParallaxBackground.RouteRollDeg;
        rollNow = Mathf.Lerp(rollNow, want, Time.deltaTime * 6f);
        if (Mathf.Abs(rollNow) < 0.01f && Mathf.Abs(want) < 0.01f)
        {
            if (rollApplied) { transform.rotation = Quaternion.identity; rollApplied = false; rollNow = 0f; }
            return;
        }
        transform.rotation = Quaternion.Euler(0f, 0f, rollNow);
        rollApplied = true;
    }

    // ─────────────────────────────────────────────
    // 마우스 휠 줌
    // ─────────────────────────────────────────────
    private void HandleZoom()
    {
        // 마우스가 UI 위에 있으면 줌 무시 (요리 목록 스크롤/버튼과 충돌 방지)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            // 부드러운 줌 적용은 계속 (진행 중이던 줌이 뚝 끊기지 않게)
            ApplySmoothZoom();
            return;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            // 휠 위 = 줌인 (orthographicSize 감소)
            // 휠 아래 = 줌아웃 (orthographicSize 증가)
            targetZoom -= scroll * zoomSpeed * 10f;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        ApplySmoothZoom();
    }

    /// <summary>부드럽게 줌 적용. v5: 정차 프레이밍 중에는 목표 줌에 RouteStopZoomMul 을 곱한다 (휠 줌은 그대로 먹는다)</summary>
    private void ApplySmoothZoom()
    {
        if (cam == null) return;
        float mul = Mathf.Lerp(1f, GameBalance.RouteStopZoomMul, RouteFrameEase());
        smoothZoom = Mathf.Lerp(smoothZoom, targetZoom * mul, Time.deltaTime * smoothSpeed);
        cam.orthographicSize = smoothZoom * GameFeel.ZoomMul;   // v5.1: 보스 처치 줌 당김 (평소 1)
    }

    // ─────────────────────────────────────────────
    // 카메라 위치 (기차 추적 + 셰이크 최종 적용)
    // ─────────────────────────────────────────────
    private void HandleCameraPosition()
    {
        // 추적은 basePos에만 적용 (셰이크 오프셋이 Lerp에 오염되지 않게 분리)
        if (GameBalance.CamFollowChef && chefTransform != null)
        {
            // ── B-2: 셰프 소프트 팔로우 ──
            // 데드존 안에서는 카메라가 가만히, 벗어나면 가장자리를 잡고 따라간다.
            // X 이동 한계로 전장(적 스폰 방향)이 화면 밖으로 새는 것을 막는다.
            float targetX = basePos.x;
            float dx = chefTransform.position.x - basePos.x;
            if (Mathf.Abs(dx) > GameBalance.CamDeadzone)
                targetX = chefTransform.position.x - Mathf.Sign(dx) * GameBalance.CamDeadzone;
            targetX = Mathf.Clamp(targetX, GameBalance.CamFollowMinX, GameBalance.CamFollowMaxX);

            // v5: 정차 프레이밍 - 두상 앞 갈림길이 다 보이는 x 로 (셰프가 움직여도 고정)
            targetX = Mathf.Lerp(targetX, GameBalance.RouteStopCamX, RouteFrameEase());

            // Y는 기차 기준 유지 (기차 태그 없으면 현재 y)
            float targetY = targetTransform != null
                ? targetTransform.position.y + offset.y : basePos.y;

            Vector3 followPos = new Vector3(targetX, targetY, offset.z);
            basePos = Vector3.Lerp(basePos, followPos, Time.deltaTime * GameBalance.CamFollowLerp);
        }
        else if (targetTransform != null)
        {
            // 팔로우 오프 = 기존 기차 고정 추적 (v5: 정차 프레이밍은 여기도)
            Vector3 targetPos = targetTransform.position + offset;
            targetPos.x = Mathf.Lerp(targetPos.x, GameBalance.RouteStopCamX, RouteFrameEase());
            basePos = Vector3.Lerp(basePos, targetPos, Time.deltaTime * smoothSpeed);
        }

        // v3: 셰이크 적용. 줌 배율로 스케일해서 줌인/줌아웃 상관없이 체감 강도가 일정
        float zoomScale = cam != null ? cam.orthographicSize / 7f : 1f;
        transform.position = basePos + (Vector3)(GameFeel.ShakeOffset * zoomScale);
    }

    // ─────────────────────────────────────────────
    // 줌 리셋 (Z키) - v3: R에서 변경 (R = 마지막 주문)
    // ─────────────────────────────────────────────
    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            targetZoom = defaultZoom;
            Debug.Log("[CameraZoom] 줌 리셋");
        }
    }

    // ─────────────────────────────────────────────
    // 줌 레벨 조회 (0~1, 0=줌아웃, 1=줌인)
    // ─────────────────────────────────────────────
    public float GetZoomLevel()
    {
        if (cam == null) return 0.5f;
        return 1f - (cam.orthographicSize - minZoom) / (maxZoom - minZoom);
    }
}
