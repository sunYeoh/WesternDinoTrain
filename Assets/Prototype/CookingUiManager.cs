using UnityEngine;

/// <summary>
/// [CookingUIManager.cs] v2
/// 구 조리 미니게임 UI 관리자 - 조리 통일로 대부분 기능 제거됨.
///
/// - v2 변경점:
///   구 미니게임 5종 UI 로직 전부 삭제 (조리는 CookingMinigame 한 벌로 통일).
///   다른 스크립트가 부를 수 있는 공개 함수들은 안전한 빈 껍데기로 유지:
///     OnClickGrilling/Saute/Boiling/Frying/Fermenting -> 아무 것도 안 함
///     ShowHitFlash -> 아무 것도 안 함 (피격 연출은 셰프 흔들림으로 대체)
///     ShowPoisonDebuff -> UIManager 알림으로 전달
///   하이어라키의 구 조리 패널들은 삭제해도 된다.
///
/// VS 2017 (C# 7.3) 호환 버전입니다.
/// </summary>
public class CookingUIManager : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("[CookingUIManager] v2 - 구 조리 UI 비활성화됨 (조리는 KitchenPanel로 통일)");
    }

    // ─────────────────────────────────────────────
    // 구시스템 호환 스텁 (호출돼도 아무 일 없음)
    // ─────────────────────────────────────────────
    public void OnClickGrilling() { }
    public void OnClickSaute() { }
    public void OnClickBoiling() { }
    public void OnClickFrying() { }
    public void OnClickFermenting() { }
    public void HideAllCookingPanels() { }

    /// <summary>[구시스템 호환] 피격 플래시 - 제거됨</summary>
    public void ShowHitFlash() { }

    /// <summary>독침 디버프 알림 - UIManager 알림으로 전달</summary>
    public void ShowPoisonDebuff(float duration)
    {
        UIManager.Instance?.ShowStatChange("독침 피격! 조리 속도 -50% (" + (int)duration + "초)");
    }
}
