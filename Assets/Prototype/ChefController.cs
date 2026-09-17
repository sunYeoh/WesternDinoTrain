using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [ChefController.cs] v5.2 (v9.10 2026-09-17 테스터 피드백: 벽에 막힌 대시는 소리·먼지 없이 바로 취소(쿨타임 환급) - 대시 연출은 실제로 움직인 첫 프레임에 /
///   ChefVisual 이 바라보는 방향을 실제 위치 변화가 아니라 '가려는 속도'(CurrentVel)로 정하게 공개 - 벽·통로에서 밀려날 때 뒤도는 것 제거) /
/// v5.1 (교수 피드백 2026-09-14: 도구 경고 화면 표시 / 마모 스위치 / 전갈 대체 효과) / v5 (통로 보행 2026-09-08) / v4 (B-1: 셰프의 몸 - 방향결정 2026-08-31)
/// 셰프 이동 + 도구 내구도 + 전투 연동(피격 연출/조리 디버프)을 담당합니다.
///
/// - v5 변경점 (통로로 칸 건너기):
///   활동 범위가 "기차 전체 사각형"에서 "칸 바닥 + 칸 사이 통로 발판"으로 바뀐다.
///   칸 안에서는 예전처럼 자유롭게 걷고, 옆 칸으로 갈 때는 통로 높이(y -0.5~0.5)로 내려와 발판을 건너야 한다.
///   벽에 부딪히면 벽을 따라 미끄러진다 (가로/세로 분리 판정). 판정은 TrainDeck.ResolveWalk (데크 지오메트리 단일 소스).
///   TrainDeck.cs v5.1 이상 필요. 이동 속도/대시/세로 범위 수치는 GameBalance 그대로
/// - v4 변경점 (B-1 이동감):
///   1) 이동 속도/활동 범위를 GameBalance로 이관 (Inspector 값은 Start에서 덮어씀)
///   2) 가감속 곡선 - 즉발 속도 대신 짧은 가속/감속 (달리는 몸의 무게감)
///   3) [Shift] 대시 - 순간 가속 + 흙먼지 팝 + 쿨타임 (위기 대응 달리기용)
///   4) 발소리 훅 (sfx_step - 클립 없으면 무시)
///   5) InteractConsumedFrame - 근접 [E]의 이중 소비 방지 (해빙 vs 조리대)
///
/// 남은 역할:
///   1) 셰프 WASD 이동 (활동 범위 = 칸 바닥 + 통로, TrainDeck 이 판정)
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
    private bool dashFxPending = false;          // v5.2: 대시 연출(소리·먼지)은 실제로 움직인 첫 프레임에

    /// <summary>v5.2: 지금 가려는 속도 (벽에 막힌 축은 0). ChefVisual 이 바라보는 방향 판정에 쓴다</summary>
    public Vector2 CurrentVel { get { return currentVel; } }
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

        Debug.Log("[ChefController] 초기화 완료 (v5 - 속도 " + moveSpeed
            + ", 범위 X " + kitchenMinX + "~" + kitchenMaxX + ", 칸 사이는 통로(|y| <= " + TrainDeck.GANGWAY_HALF_Y + ")로만)");
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
            dashFxPending = true;            // v5.2: 연출은 실제로 움직였을 때 (벽에 막히면 아무것도 안 나온다)
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

        // v5: 칸 바닥 + 통로 발판 안으로 잘라낸다 (벽에 닿으면 미끄러짐). 세로 한계는 예전 값 그대로
        Vector2 before = transform.position;
        Vector2 wanted = (Vector2)transform.position + currentVel * dt;
        wanted.y = Mathf.Clamp(wanted.y, kitchenMinY, kitchenMaxY);
        Vector2 resolved = TrainDeck.ResolveWalk(transform.position, wanted);
        transform.position = new Vector3(resolved.x, resolved.y, transform.position.z);

        // 벽에 막힌 축은 관성도 끊는다 (벽에 붙어 미는 동안 속도가 쌓여 있다가 튀어나가는 것 방지)
        if (Mathf.Abs(resolved.x - wanted.x) > 0.0001f) currentVel.x = 0f;
        if (Mathf.Abs(resolved.y - wanted.y) > 0.0001f) currentVel.y = 0f;

        // v5.2: 대시 연출은 실제로 움직인 첫 프레임에. 첫 프레임부터 벽에 막혔으면(제자리) 대시 취소 + 쿨타임 환급 - "벽에 부딪힐 때 대시 이펙트" 제거
        if (dashFxPending)
        {
            dashFxPending = false;
            float moved = Vector2.Distance(resolved, before);
            if (moved > 0.01f)
            {
                SoundManager.Play("sfx_dash");   // 클립 없으면 무시
                // 발밑 흙먼지 (처치 팝 재사용 - 작게, 흙색)
                GameFeel.DeathPop(transform.position + Vector3.down * 0.3f, new Color(0.72f, 0.63f, 0.48f), 0.45f);
            }
            else
            {
                dashTimer = 0f;
                dashReadyTime = Time.time;       // 헛대시 - 쿨타임 안 먹는다
                currentVel = Vector2.zero;
            }
        }

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
        // v5.1 (교수 피드백 B3 실험 스위치): 마모 off면 아무것도 닳지 않는다
        if (!GameBalance.ToolWearEnabled) return;

        // Phase 2-3 아이템 '휴대용 숫돌': 도구 마모 감소 (기본 1 = 그대로)
        float wearMul = ItemManager.ToolWearMul;

        if (method == 0)
            knifeSharpness = Mathf.Max(0f, knifeSharpness - 5f * wearMul);   // 굽기 = 칼 마모
        else
            panCondition = Mathf.Max(0f, panCondition - 8f * wearMul);       // 볶기/끓이기 = 팬 마모

        CheckToolWarnings();
    }

    // v5.1 (교수 피드백 A9): 마모 경고가 콘솔에만 찍히던 것을 화면 경고로. 30% 아래로 내려가는 순간 1회,
    // 정비소에서 수리하면 다시 무장. (도구 상태 자체는 GameHUD 하단 바의 칼/팬 칩이 상시 표시)
    private bool knifeWarned = false;
    private bool panWarned = false;

    private void CheckToolWarnings()
    {
        if (knifeSharpness <= 30f && !knifeWarned)
        {
            knifeWarned = true;
            UIManager.Instance?.ShowDanger("칼이 무뎌졌다 (" + Mathf.RoundToInt(knifeSharpness) + "%) - 굽기 판정이 좁아진다. 정비소 [G]에서 연마");
        }
        else if (knifeSharpness > 30f) knifeWarned = false;

        if (panCondition <= 30f && !panWarned)
        {
            panWarned = true;
            UIManager.Instance?.ShowDanger("팬이 눌어붙었다 (" + Mathf.RoundToInt(panCondition) + "%) - 볶기·끓이기 시간이 줄어든다. 정비소 [G]에서 정비");
        }
        else if (panCondition > 30f) panWarned = false;
    }

    /// <summary>v5.1 (B3): 마모 off일 때 전갈 명중의 대체 효과 - 조리 속도 디버프 (Enemy가 호출)</summary>
    public void ApplyScorpionAlt()
    {
        StartCoroutine(ScorpionAltCoroutine());
    }

    private IEnumerator ScorpionAltCoroutine()
    {
        // 프테라 디버프(0.5)보다 약하게, 이미 더 센 디버프가 걸려 있으면 덮어쓰지 않는다
        if (cookingSpeedMultiplier > GameBalance.ScorpionAltCookSlow)
            cookingSpeedMultiplier = GameBalance.ScorpionAltCookSlow;
        UIManager.Instance?.ShowStatChange("[사막 전갈] 독이 손에 묻었다 - 조리 속도 -" + Mathf.RoundToInt((1f - GameBalance.ScorpionAltCookSlow) * 100f) + "% (" + Mathf.RoundToInt(GameBalance.ScorpionAltCookSlowSec) + "초)");
        yield return new WaitForSeconds(GameBalance.ScorpionAltCookSlowSec);
        if (cookingSpeedMultiplier <= GameBalance.ScorpionAltCookSlow + 0.001f && cookingSpeedMultiplier > 0.5f)
            cookingSpeedMultiplier = 1.0f;
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
        // v5.1: 구 CookingUIManager는 씬에 없어 알림이 뜨지 않았다 -> HUD 경고로 (교수 피드백 15.5 항목)
        UIManager.Instance?.ShowDanger("[독침 프테라] 독침에 맞았다 - 조리 속도 -50% (" + Mathf.RoundToInt(duration) + "초)");
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

    /// <summary>에디터 기즈모: 칸 바닥(노랑) + 칸 사이 통로 발판(초록)</summary>
    private void OnDrawGizmosSelected()
    {
        float[] e = GameBalance.CarEdgesX;
        float minY = GameBalance.TrainWalkMinY, maxY = GameBalance.TrainWalkMaxY;
        for (int car = 0; car < e.Length - 1; car++)
        {
            float l = e[car] + TrainDeck.FLOOR_INSET_X, r = e[car + 1] - TrainDeck.FLOOR_INSET_X;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3((l + r) * 0.5f, (minY + maxY) * 0.5f, 0f), new Vector3(r - l, maxY - minY, 0f));
            if (car < e.Length - 2)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(new Vector3(e[car + 1], 0f, 0f),
                    new Vector3(TrainDeck.FLOOR_INSET_X * 2f, TrainDeck.GANGWAY_HALF_Y * 2f, 0f));
            }
        }
    }
}
