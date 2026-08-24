using UnityEngine;

/// <summary>
/// [CookingStation.cs] v2
/// 주방 기구(그릴/볶음팬/냄비) 오브젝트에 붙이는 스크립트입니다.
/// 셰프가 상호작용 범위 안에서 E키를 누르면 조리창이 열립니다.
///
/// - v2 변경점 (조리 시스템 통일):
///   E키 -> ChefController 구 미니게임 대신 KitchenPanel(새 조리창)을 연다.
///   이때 이 조리대의 조리법에 맞는 요리만 표시된다:
///     그릴 = 굽기 요리 / 볶음팬 = 볶기 요리 / 냄비 = 끓이기 요리
///   (Tab으로 열면 전체 요리가 보이므로, 조리대는 "가까운 기구에서 바로 조리" 동선용)
///   구 CookingUIManager / MaterialSelectUI 경유 흐름 제거.
///
/// 사용법:
/// 1. 주방 안에 빈 오브젝트 3개 만들기 (Grill, SautePan, Pot)
/// 2. 각 오브젝트에 CookingStation 스크립트 붙이기
/// 3. stationType 설정 (Grilling/Saute/Boiling)
/// VS 2017 (C# 7.3) 호환 버전입니다.
/// </summary>
public class CookingStation : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // 조리 기구 종류
    // ─────────────────────────────────────────────
    public enum StationType
    {
        Grilling,  // 그릴    - 굽기
        Saute,     // 볶음팬  - 볶기
        Boiling    // 냄비    - 끓이기
    }

    // ─────────────────────────────────────────────
    // Inspector 설정
    // ─────────────────────────────────────────────
    [Header("─ 기구 설정 ─")]
    public StationType stationType = StationType.Grilling;
    public float interactRange = 1.5f;   // 상호작용 가능 거리
    public KeyCode interactKey = KeyCode.E; // 상호작용 키

    [Header("─ 상호작용 표시 ─")]
    public GameObject interactPrompt;    // "E 눌러서 조리" 텍스트

    // ─────────────────────────────────────────────
    // 내부 상태
    // ─────────────────────────────────────────────
    private Transform chefTransform;
    private bool isChefNearby = false;

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────
    private void Start()
    {
        // 셰프 오브젝트 자동 탐색
        GameObject chefObj = GameObject.Find("Chef");
        if (chefObj != null)
            chefTransform = chefObj.transform;

        // 상호작용 프롬프트 기본 비활성화
        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        Debug.Log("[CookingStation] " + GetStationName() + " 초기화 완료 (v2 - 새 조리창 연동)");
    }

    // ─────────────────────────────────────────────
    // 매 프레임: 셰프 거리 체크 + 키 입력
    // ─────────────────────────────────────────────
    private void Update()
    {
        // 다른 전체화면 UI 진행 중엔 기구 상호작용 차단
        if (CookingMinigame.IsActive || KitchenEventManager.IsActive ||
            KitchenPanel.IsOpenStatic || WorkshopUI.IsOpen || AugmentPickUI.IsOpen)
        {
            HidePrompt();
            return;
        }

        // Battle / Town 상태에서만 상호작용 가능
        if (GameManager.Instance != null)
        {
            GameManager.GameState state = GameManager.Instance.currentState;
            bool isInteractable = (state == GameManager.GameState.Battle ||
                                   state == GameManager.GameState.Town);
            if (!isInteractable)
            {
                HidePrompt();
                return;
            }
        }

        if (chefTransform == null) return;

        float distance = Vector2.Distance(transform.position, chefTransform.position);
        isChefNearby = (distance <= interactRange);

        // 프롬프트 표시/숨김
        if (isChefNearby)
            ShowPrompt();
        else
            HidePrompt();

        // E키 입력 시 이 조리대 전용 조리창 열기
        if (isChefNearby && Input.GetKeyDown(interactKey))
            OpenKitchen();
    }

    // ─────────────────────────────────────────────
    // 조리창 열기 (조리대 조리법 필터 적용)
    // ─────────────────────────────────────────────
    private void OpenKitchen()
    {
        if (KitchenPanel.Instance == null)
        {
            Debug.LogWarning("[CookingStation] KitchenPanel 없음 - GameSystems에 KitchenPanel 컴포넌트 필요");
            return;
        }

        int method = 0;
        if (stationType == StationType.Saute) method = 1;
        else if (stationType == StationType.Boiling) method = 2;

        HidePrompt();
        KitchenPanel.Instance.OpenForStation(method);
        Debug.Log("[CookingStation] " + GetStationName() + " 조리창 열림");
    }

    /// <summary>[구시스템 호환] MaterialSelectUI 등이 호출할 수 있음 - 이제 할 일 없음</summary>
    public void ResetCooking() { }

    // ─────────────────────────────────────────────
    // 상호작용 프롬프트 표시/숨김
    // ─────────────────────────────────────────────
    private void ShowPrompt()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(true);
    }

    private void HidePrompt()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // 기구 이름 반환
    // ─────────────────────────────────────────────
    private string GetStationName()
    {
        if (stationType == StationType.Grilling) return "그릴 (굽기)";
        if (stationType == StationType.Saute) return "볶음팬 (볶기)";
        if (stationType == StationType.Boiling) return "냄비 (끓이기)";
        return "조리 기구";
    }

    // ─────────────────────────────────────────────
    // Scene 뷰에서 상호작용 범위 시각화
    // ─────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (stationType == StationType.Grilling)
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);  // 주황
        else if (stationType == StationType.Saute)
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);    // 노랑
        else
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);  // 파랑

        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
