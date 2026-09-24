using UnityEngine;

/// <summary>
/// [TurretSystemTest.cs] v1.1 (v9.13.1 2026-09-24) - 포탑 슬롯 테스트 키 (씬의 TurretSlotManager 오브젝트에 켜진 채 붙어 있다)
/// Shift+F1: 매운 육포 투입 / Shift+F2: 과부하 코일 투입 / Shift+F3: 철판 정식 투입 (요리 없이 공짜)
/// v1.1: 빌드에서는 안 먹는다 (GameBalance.CheatsAllowed) - 테스터가 F1 을 눌러 포탑을 공짜로 받던 구멍.
///       에디터에서도 Shift 를 같이 눌러야 한다 (F3 이 DevCheat 짧은 런 토글과 겹쳤다). 견습 운행 중엔 무시
/// </summary>
public class TurretSystemTest : MonoBehaviour
{
    void Update()
    {
        if (TurretSlotManager.Instance == null) return;
        if (!GameBalance.CheatsAllowed || TutorialDirector.Active) return;
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) return;

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
