using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [ChefController.cs] v3
/// 셰프 이동 + 도구 내구도 + 전투 연동(피격 연출/조리 디버프)을 담당합니다.
///
/// - v3 변경점 (조리 시스템 통일):
///   내장 미니게임 5종(굽기/볶기/끓이기/튀기기/절임) 완전 제거.
///   조리는 전부 KitchenPanel(Tab/조리대 E) -> CookingMinigame 흐름으로 일원화.
///   구 UI(CookingUIManager 버튼 등)가 부르던 StartGrilling 등은
///   새 조리창을 여는 리다이렉트 스텁으로 유지 (컴파일/버튼 호환).
///
/// 남은 역할:
///   1) 셰프 WASD 이동 (주방 범위 제한)
///   2) 도구 내구도 (칼/팬) - 조리할 때마다 마모, 정비소에서 수리
///      마모 상태는 CookingMinigame 판정/시간에 반영된다
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
    [Header("─ 셰프 이동 ─")]
    public float moveSpeed = 3f;
    public float kitchenMinX = -2f;
    public float kitchenMaxX = 2f;
    public float kitchenMinY = -1.5f;
    public float kitchenMaxY = 1.5f;

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
        Debug.Log("[ChefController] 초기화 완료 (v3 - 조리는 KitchenPanel로 일원화)");
    }

    // ─────────────────────────────────────────────
    // 매 프레임: 이동만
    // ─────────────────────────────────────────────
    private void Update()
    {
        HandleMovement();
    }

    // ─────────────────────────────────────────────
    // 셰프 이동 (WASD 전용, 미니게임/주방창 중에는 정지)
    // ─────────────────────────────────────────────
    private void HandleMovement()
    {
        if (CookingMinigame.IsActive || KitchenPanel.IsOpenStatic) return;

        float h = 0f, v = 0f;

        if (Input.GetKey(KeyCode.W)) v = 1f;
        if (Input.GetKey(KeyCode.S)) v = -1f;
        if (Input.GetKey(KeyCode.A)) h = -1f;
        if (Input.GetKey(KeyCode.D)) h = 1f;

        Vector3 newPos = transform.position +
                         (Vector3)(new Vector2(h, v).normalized * moveSpeed * Time.deltaTime);
        newPos.x = Mathf.Clamp(newPos.x, kitchenMinX, kitchenMaxX);
        newPos.y = Mathf.Clamp(newPos.y, kitchenMinY, kitchenMaxY);
        transform.position = newPos;
    }

    // ─────────────────────────────────────────────
    // 도구 내구도
    // ─────────────────────────────────────────────

    /// <summary>조리 완료 시 도구 마모 (CookingMinigame이 호출). method: 0=굽기 1=볶기 2=끓이기</summary>
    public void WearToolsByMethod(int method)
    {
        if (method == 0)
            knifeSharpness = Mathf.Max(0f, knifeSharpness - 5f);   // 굽기 = 칼 마모
        else
            panCondition = Mathf.Max(0f, panCondition - 8f);       // 볶기/끓이기 = 팬 마모

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

    /// <summary>독침 프테라 피격 시 - 조리 속도 50% 감소 (CookingMinigame 제한시간에 반영)</summary>
    public void ApplyCookingSpeedDebuff(float duration = 10f)
    {
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
