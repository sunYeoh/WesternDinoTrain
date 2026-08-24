using UnityEngine;

/// <summary>
/// [CookingResultListener.cs]
/// 기존 미니게임(ChefController)의 조리 완료 이벤트를 받아
/// 새 요리 시스템(CookingBridge)으로 전달하는 연결 다리
/// GameSystems 오브젝트에 부착
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class CookingResultListener : MonoBehaviour
{
    private ChefController chef;

    void Start()
    {
        chef = FindFirstObjectByType<ChefController>();
        if (chef != null)
        {
            chef.OnCookingCompleted.AddListener(OnCooked);
            Debug.Log("[CookingResultListener] 미니게임 이벤트 연결 완료");
        }
        else
        {
            Debug.LogWarning("[CookingResultListener] ChefController를 찾지 못함");
        }
    }

    void OnDestroy()
    {
        if (chef != null)
            chef.OnCookingCompleted.RemoveListener(OnCooked);
    }

    private void OnCooked(ChefController.CookingResult result)
    {
        // 새 시스템 조리가 아니면 무시 (pendingRecipeId가 비어있음)
        if (string.IsNullOrEmpty(CookingBridge.pendingRecipeId)) return;

        // 기존 판정 -> 새 시스템 품질 문자열 변환
        string quality;
        if (result.quality == ChefController.CookingQuality.Perfect)
            quality = "perfect";
        else if (result.quality == ChefController.CookingQuality.Good)
            quality = "good";
        else
            quality = "bad"; // Bad / Burnt 등 나머지 전부

        CookingBridge.FinishCook(quality);
    }
}