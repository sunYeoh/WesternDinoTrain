using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [ChefController.cs] v4 (B-1: 셰프의 몸 - 방향결정 2026-08-31)
/// 셰프 이동 + 도구 내구도 + 전투 연동(피격 연출/조리 디버프)을 담당합니다.
///
/// - v4 변경점 (B-1 이동감):
///   1) 이동 속도/활동 범위를 GameBalance로 이관 (Inspector 값은 Start에서 덮어씀)
///   2) 가감속 곡선 - 즉발 속도 대신 짧은 가속/감속 (달리는 몸의 무게감)
///   3) [Shift] 대시 - 순간 가속 + 흙먼지 팝 + 쿨타임 (위기 대응 달리기용)
///   4) 발소리 훅 (sfx_step - 클립 없으면 무시)
///   5) InteractConsumedFrame - 근접 [E]의 이중 소비 방지 (해빙 vs 조리대)
///
/// 남은 역할:
///   1) 셰프 WASD 이동 (활동 범위 제한 - B-2에서 트레일러로 확장)
///   2) 도구 내구도 (칼/팬) - 조리할 때마다 마모, 정비소에서 수리
///   3) 피격 연출(OnTrainHit) / 독침 프테라 조리 디버프
///
/// VS 2017 (C# 7.3) 호환 버전입니다.
/// </summary>
public class ChefController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // 열거형 / 구조체 (다른 스크립트 호환용 유지)
    // ─────────────────────────────────────────────
    public enum CookingMethod
    {
        None,
        Grilling,    // 굽기
        Saute,       // 볶기
        Boiling,     // 끓이기
        Frying,      // 튀기기 (구 시스템 - 미사용)
        Fermenting   // 절임 (구 시스템 - 미사용)
    }

    public enum CookingQuality
    {
        Perfect,
        Good,
        Bad,
        Burnt
    }

    [System.Serializable]
    public struct CookingResult
    {
        public CookingMethod method;
        public CookingQuality quality;
        public float satietyGained;
        public bool triggerEvolution;
        public string foodName;
        public bool canBeUsedAsWeapon;
    }

    // ─────────────────────────────────────────────
    // Inspector 설정
    // ─────────────────────────────────────────────
    [Header("─ 셰프 이동 (Start에서 GameBalance 값으로 덮어씀) ─")]
    public float moveSpeed = 3f;
    public float kitchenMinX = -2f;
    public float kitchenMaxX = 2f;
    public float kitchenMinY = -1.5f;
    public float kitchenMaxY = 1.5f;

    // ── B-1: 이동감 상태 ──
    private Vector2 currentVel = Vector2.zero;   // 가감속용 현재 속도
    private float dashTimer = 0f;                // 대시 지속 잔여
    private float dashReadyTime = 0f;            // 다음 대시 가능 시각
    private Vector2 dashDir = Vector2.right;
    private float nextStepSoundTime = 0f;

    /// <summary>
    /// B-1: 근접 [E]가 이번 프레임에 이미 소비됐는가 (해빙이 조리대 열림보다 우선).
    /// 소비한 쪽이 Time.frameCount를 기록하고, 다른 쪽은 같은 프레임이면 무시한다.
    /// </summary>
    public static int InteractConsumedFrame = -1;

    [Header("─ 조리 해금 현황 (구 시스템 호환) ─")]
    public bool isGrillingUnlocked = true;
    public bool isSauteUnlocked = true;
    public bool isBoilingUnlocked = true;
    public bool isFryingUnlocked = true;
    public bool isFermentingUnlocked = true;

    [Header("─ 도구 내구도 ─")]
    [Range(0f, 100f)] public float knifeSharpness = 100f;   // 낮으면 미니게임 판정 존 축소
    [Range(0f, 100f)] public float panCondition = 100f;     // 낮으면 미니게임 제한 시간 감소

    [Header("─ 현재 상태 (구 시스템 호환 - 항상 None) ─")]
    public CookingMethod activeCookingMethod = CookingMethod.None;
    public bool isCookingEnabled = false;

    [Header("─ 전투 연동 ─")]
    [Range(0.1f, 1f)]
    public float cookingSpeedMultiplier = 1.0f; // 조리 속도 배율 (독침 디버프 시 0.5)

    // ─────────────────────────────────────────────
    // 이벤트 (다른 스크립트 호환용 유지 - v3에서는 발행 안 함)
    // ─────────────────────────────────────────────
    public UnityEvent<CookingResult> OnCookingCompleted = new UnityEvent<CookingResult>();
    public UnityEvent<int> OnSauteCommandProgress = new UnityEvent<int>();
    public UnityEvent<CookingMethod> OnCookingMethodUnlocked = new UnityEvent<CookingMethod>();

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────
    private void Start()
    {
        // B-1: 이동 수치/활동 범위는 GameBalance가 단일 소스 (조정은 GameBalance.cs에서)
        moveSpeed = GameBalance.ChefMoveSpeed;
        kitchenMinX = GameBalance.TrainWalkMinX;
        kitchenMaxX = GameBalance.TrainWalkMaxX;
        kitchenMinY = GameBalance.TrainWalkMinY;
        kitchenMaxY = GameBalance.TrainWalkMaxY;

        Debug.Log("[ChefController] 초기화 완료 (v4 - 속도 " + moveSpeed
            + ", 범위 X " + kitchenMinX + "~" + kitchenMaxX + ")");
    }

    // ─────────────────────────────────────────────
    // 매 프레임: 이동만
    // ─────────────────────────────────────────────
    private void Update()
    {
        HandleMovement();
    }

    // ─────────────────────────────────────────────
    // 셰프 이동 (WASD + Shift 대시, 미니게임/주방창 중에는 정지)
    // ─────────────────────────────────────────────
    private void HandleMovement()
    {
        if (CookingMinigame.IsActive || KitchenPanel.IsOpenStatic)
        {
            currentVel = Vector2.zero;   // 조리에 들어가면 관성도 멈춘다
            return;
        }

        float h = 0f, v = 0f;

        if (Input.GetKey(KeyCode.W)) v = 1f;
        if (Input.GetKey(KeyCode.S)) v = -1f;
        if (Input.GetKey(KeyCode.A)) h = -1f;
        if (Input.GetKey(KeyCode.D)) h = 1f;

        Vector2 inputDir = new Vector2(h, v);
        bool hasInput = inputDir.sqrMagnitude > 0.01f;
        float dt = Time.deltaTime;

        // ── B-1 대시: [Shift] - 위기 현장으로 달려가는 순간 가속 ──
        if (hasInput && Time.time >= dashReadyTime
            && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)))
        {
            dashTimer = GameBalance.ChefDashTime;
            dashReadyTime = Time.time + GameBalance.ChefDashCooldown;
            dashDir = inputDir.normalized;
            SoundManager.Play("sfx_dash");   // 클립 없으면 무시
            // 발밑 흙먼지 (처치 팝 재사용 - 작게, 흙색)
            GameFeel.DeathPop(transform.position + Vector3.down * 0.3f,
                new Color(0.72f, 0.63f, 0.48f), 0.45f);
        }

        // ── 속도 계산: 대시 중 = 고정 고속 / 평시 = 가감속 곡선 ──
        if (dashTimer > 0f)
        {
            dashTimer -= dt;
            currentVel = dashDir * GameBalance.ChefDashSpeed;
        }
        else
        {
            Vector2 targetVel = hasInput ? inputDir.normalized * moveSpeed : Vector2.zero;
            float rate = hasInput ? GameBalance.ChefAccel : GameBalance.ChefDecel;
            currentVel = Vector2.MoveTowards(currentVel, targetVel, rate * dt);
        }

        Vector3 newPos = transform.position + (Vector3)(currentVel * dt);
        newPos.x = Mathf.Clamp(newPos.x, kitchenMinX, kitchenMaxX);
        newPos.y = Mathf.Clamp(newPos.y, kitchenMinY, kitchenMaxY);
        transform.position = newPos;

        // ── 발소리 (이동 중 0.28초 간격, 클립 없으면 무시) ──
        if (currentVel.sqrMagnitude > 0.25f && Time.time >= nextStepSoundTime)
        {
            nextStepSoundTime = Time.time + 0.28f;
            SoundManager.Play("sfx_step");
        }
    }

    // ─────────────────────────────────────────────
    // 도구 내구도
    // ─────────────────────────────────────────────

    /// <summary>조리 완료 시 도구 마모 (CookingMinigame이 호출). method: 0=굽기 1=볶기 2=끓이기</summary>
    public void WearToolsByMethod(int method)
    {
        // Phase 2-3 아이템 '휴대용 숫돌': 도구 마모 감소 (기본 1 = 그대로)
        float wearMul = ItemManager.ToolWearMul;

        if (method == 0)
            knifeSharpness = Mathf.Max(0f, knifeSharpness - 5f * wearMul);   // 굽기 = 칼 마모
        else
            panCondition = Mathf.Max(0f, panCondition - 8f * wearMul);       // 볶기/끓이기 = 팬 마모

        if (knifeSharpness <= 30f)
            Debug.Log("[ChefController] 칼이 무뎌졌다! 정비소(G)에서 연마 필요 (" + Mathf.RoundToInt(knifeSharpness) + "%)");
        if (panCondition <= 30f)
            Debug.Log("[ChefController] 팬이 눌어붙었다! 정비소(G)에서 정비 필요 (" + Mathf.RoundToInt(panCondition) + "%)");
    }

    public void RepairKnife(float amount) { knifeSharpness = Mathf.Min(100f, knifeSharpness + amount); }
    public void RepairPan(float amount) { panCondition = Mathf.Min(100f, panCondition + amount); }

    // ─────────────────────────────────────────────
    // 전투 연동
    // ─────────────────────────────────────────────

    /// <summary>기차 피격 시 TrainManager에서 호출. intensity: 0~1</summary>
    public void OnTrainHit(float intensity)
    {
        // v3.1: 기획 복원 - 기차가 흔들리면 조리 미니게임 게이지도 흔들린다
        if (CookingMinigame.Instance != null)
            CookingMinigame.Instance.OnTrainHit(intensity);
    }

    // Phase 2-3 아이템 '김서림 방지 고글': 저격 무효 알림 스팸 방지용 스로틀
    private float nextGoggleNoticeTime = 0f;

    /// <summary>독침 프테라 피격 시 - 조리 속도 50% 감소 (CookingMinigame 제한시간에 반영)</summary>
    public void ApplyCookingSpeedDebuff(float duration = 10f)
    {
        // Phase 2-3 아이템 '김서림 방지 고글': 프테라 저격 무효
        if (ItemManager.SnipeImmune)
        {
            if (Time.time >= nextGoggleNoticeTime)
            {
                nextGoggleNoticeTime = Time.time + 4f;
                UIManager.Instance?.ShowStatChange("[고글] 프테라의 저격을 무시했다");
            }
            return;
        }
        StartCoroutine(CookingSpeedDebuffCoroutine(duration));
    }

    private IEnumerator CookingSpeedDebuffCoroutine(float duration)
    {
        cookingSpeedMultiplier = 0.5f;
        Debug.Log("[독침 프테라] 조리 속도 -50%! " + duration + "초간");
        FindFirstObjectByType<CookingUIManager>()?.ShowPoisonDebuff(duration);
        yield return new WaitForSeconds(duration);
        cookingSpeedMultiplier = 1.0f;
        Debug.Log("[독침 프테라] 조리 속도 정상화");
    }

    // ─────────────────────────────────────────────
    // 조리 해금 (GameManager가 호출 - 유지)
    // ─────────────────────────────────────────────
    public void CheckUnlocks(int clearedWave)
    {
        // v3: 조리법은 레시피에 귀속되므로 해금 개념은 현재 미사용
        // (추후 "조리대 해금"으로 재활용 가능)
    }

    // ─────────────────────────────────────────────
    // 구시스템 호환 스텁
    // 구 UI(CookingUIManager 버튼 등)가 부르던 함수들 - 새 조리창으로 리다이렉트
    // ─────────────────────────────────────────────

    public void EnableCooking(bool enable)
    {
        isCookingEnabled = enable;
    }

    /// <summary>[구시스템 호환] MaterialSelectUI가 호출할 수 있음 - 이제 할 일 없음</summary>
    public void SetPendingMaterials(List<string> materials) { }

    // 구 UI/스크립트가 어디서 호출해도 아무 일도 일어나지 않는다 (키 꼬임 방지)
    // 조리는 오직 CookingStation(E) / Tab -> KitchenPanel 경로로만 시작된다
    public void StartGrilling() { }
    public void StartSaute() { }
    public void StartBoiling() { }
    public void StartFrying() { }
    public void StartFermenting() { }

    /// <summary>[구시스템 호환] 굽기 불 세기 - 이제 할 일 없음</summary>
    public void SetGrillHeat(int level) { }

    // 구 UI가 읽던 상태 프로퍼티 - 항상 기본값
    public float GrillProgress => 0f;
    public bool IsGrillSmoking => false;
    public int GrillHeatLevel => 2;
    public int GrillFlipCount => 0;
    public float SauteShakeValue => 0f;
    public float BoilingGauge => 0f;
    public float FryingTemp => 0f;
    public float FermentingProgress => 0f;

    public string GetMethodName(CookingMethod method)
    {
        switch (method)
        {
            case CookingMethod.Grilling: return "굽기";
            case CookingMethod.Saute: return "볶기";
            case CookingMethod.Boiling: return "끓이기";
            case CookingMethod.Frying: return "튀기기";
            case CookingMethod.Fermenting: return "절임/숙성";
            default: return "없음";
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(
            (kitchenMinX + kitchenMaxX) * 0.5f,
            (kitchenMinY + kitchenMaxY) * 0.5f, 0f);
        Vector3 size = new Vector3(
            kitchenMaxX - kitchenMinX,
            kitchenMaxY - kitchenMinY, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}
