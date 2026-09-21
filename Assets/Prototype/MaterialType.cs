// [MaterialType.cs] v1.1 (v9.10.1 2026-09-21: 재료 이름을 한 곳(MaterialNames)으로 - "고기 등심 옆에 독 전기 화염" 괴리 정리)
// 재료 6종 정의
// 적 6종이 각자 다른 재료를 드롭한다 (랩터->고기, 아르마딜로->등심, 테라노돈->전기알, 카르노/캑터스->화염꽃, 모사->얼음꽃, 전갈/프테라->독샘)
public enum MaterialType
{
    Meat,    // 고기 (랩터)
    Armor,   // 등심 (장갑 손님)
    Elec,    // 전기알
    Fire,    // 화염꽃
    Ice,     // 얼음꽃
    Poison   // 독샘
}

/// <summary>
/// 재료 이름표 - HUD·주방·정비소·카드·사고 이벤트가 전부 여기서 읽는다. 이름을 바꾸려면 이 표 한 곳만.
/// 순서 = MaterialType 순서 (Meat, Armor, Elec, Fire, Ice, Poison).
///   MaterialNames.Kor(MaterialType.Fire) -> "화염꽃"
///   MaterialNames.Kor("fire")            -> "화염꽃"   (레시피 키 "meat"/"armor"/"elec"/"fire"/"ice"/"poison")
///   MaterialNames.KOR[i]                  -> 배열로 (HUD 칸 순회용)
/// </summary>
public static class MaterialNames
{
    /// <summary>재료 6종 한글 이름 (MaterialType 순서)</summary>
    public static readonly string[] KOR = { "고기", "등심", "전기알", "화염꽃", "얼음꽃", "독샘" };

    /// <summary>레시피 키 순서 (RecipeDatabase 의 "meat+fire" 식 키와 같은 낱말)</summary>
    private static readonly string[] KEYS = { "meat", "armor", "elec", "fire", "ice", "poison" };

    public static string Kor(MaterialType t)
    {
        int i = (int)t;
        return i >= 0 && i < KOR.Length ? KOR[i] : t.ToString();
    }

    /// <summary>레시피 키 낱말 -> 한글 이름. 모르는 키는 그대로 돌려준다</summary>
    public static string Kor(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        string k = key.Trim().ToLowerInvariant();
        for (int i = 0; i < KEYS.Length; i++)
            if (KEYS[i] == k) return KOR[i];
        return key;
    }

    /// <summary>"고기 + 화염꽃" 식으로 - 레시피 키 "meat+fire" 를 사람 말로</summary>
    public static string PairKor(string recipeKey)
    {
        if (string.IsNullOrEmpty(recipeKey)) return "";
        string[] parts = recipeKey.Split('+');
        if (parts.Length < 2) return Kor(recipeKey);
        return Kor(parts[0]) + " + " + Kor(parts[1]);
    }
}

// 요리 계열 태그 (T2 합성에 사용)
public enum FoodTag
{
    Phys,   // 물리 계열
    Elec,   // 전기 계열
    Fire,   // 화염 계열
    Ice,    // 냉기 계열
    Poison, // 독 계열
    Def     // 방어 계열
}

// 역할 6종
public enum TurretRole
{
    PhysDealer,  // 물리 딜러
    MagicDealer, // 마법 딜러
    Debuffer,    // 방깎/마깎
    Buffer,      // 인접 버프
    CC,          // 슬로우/스턴
    Support      // 회복/장갑/반격
}

// 공격 형태 8종
public enum AttackShape
{
    Projectile, // 단일 투사체
    Pierce,     // 관통 레일 (일직선 전부)
    Cone,       // 부채꼴 방사 (화염방사)
    Explode,    // 착탄 폭발
    Chain,      // 체인 (번개 튀김)
    Field,      // 장판 (바닥에 남음)
    Aura,       // 오라 (기차 주변 상시)
    Passive     // 상시 패시브
}

// 데미지 타입 (적 DEF/RES와 대응)
public enum DamageType
{
    Phys, // 물리 - 적 DEF에 감소
    Magic // 마법 - 적 RES에 감소
}
