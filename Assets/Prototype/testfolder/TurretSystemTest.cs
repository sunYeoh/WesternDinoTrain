using UnityEngine;

/// <summary>
/// 포탑 슬롯 시스템 테스트 (확인 후 삭제)
/// F1: 매운 육포 투입 / F2: 과부하 코일 투입 / F3: 철판 정식 투입
/// </summary>
public class TurretSystemTest : MonoBehaviour
{
    void Update()
    {
        if (TurretSlotManager.Instance == null) return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            string id = RecipeDatabase.MakeKey(MaterialType.Meat, MaterialType.Fire);
            TurretSlotManager.Instance.TryInsertFood(id);
            Debug.Log("[테스트] 매운 육포 투입 시도");
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            string id = RecipeDatabase.MakeKey(MaterialType.Elec, MaterialType.Elec);
            TurretSlotManager.Instance.TryInsertFood(id);
            Debug.Log("[테스트] 과부하 코일 투입 시도");
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            string id = RecipeDatabase.MakeKey(MaterialType.Armor, MaterialType.Armor);
            TurretSlotManager.Instance.TryInsertFood(id);
            Debug.Log("[테스트] 철판 정식 투입 시도 (최대HP +60 확인)");
        }
    }
}