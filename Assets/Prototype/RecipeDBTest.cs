using UnityEngine;

// 레시피 DB 로드 테스트 (확인 후 삭제)
public class RecipeDBTest : MonoBehaviour
{
    void Start()
    {
        int count = 0;
        foreach (RecipeData r in RecipeDatabase.All) count++;
        Debug.Log("[테스트] 레시피 총 " + count + "개 로드됨 (기대값: 42)");

        RecipeData t1 = RecipeDatabase.GetByMaterials(MaterialType.Meat, MaterialType.Fire);
        Debug.Log("[테스트] 고기+화염 = " + t1.displayName + " (기대값: 매운 육포)");

        RecipeData t2 = RecipeDatabase.GetFusion(FoodTag.Fire, FoodTag.Fire);
        Debug.Log("[테스트] 화염+화염 합성 = " + t2.displayName + " (기대값: 태양의 심장포)");
    }
}