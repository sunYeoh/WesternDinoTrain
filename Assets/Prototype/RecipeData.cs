using System;

/// <summary>
/// [RecipeData.cs] v2
/// 요리(레시피) 1종의 정의.
/// T1: 재료 2개 조합으로 제작 / T2: T1 태그 2개 합성으로 제작
/// - v2 변경점: 도감 플레이버 텍스트(flavor) 필드 추가 (스토리바이블 Phase 1)
/// VS 2017 (C# 7.3) 호환.
/// </summary>
[Serializable]
public class RecipeData
{
    public string recipeId;       // 조합 키 (예: "meat+fire", "T2:fire+fire")
    public string displayName;    // 한글 이름
    public int tier;              // 1 or 2
    public FoodTag tag;           // 합성용 계열 태그 (T1만 의미 있음)
    public TurretRole role;
    public AttackShape shape;
    public DamageType damageType;

    // 전투 수치 (프로토타입 v3 검증값 그대로)
    public float damage;          // 기본 데미지 (패시브형은 0)
    public float cooldown;        // 발사 쿨다운(초)
    public float projectileSpeed; // 투사체 속도

    // 효과 플래그 (0이면 없음)
    public int burnStack;         // 화상 도트 (3초, 스택당 4/s)
    public int poisonStack;       // 독 도트 (5초, 스택당 3/s)
    public int slowLevel;         // 1=50% 2초, 2=70% 3초
    public float stunSec;         // 스턴 시간
    public int shredDef;          // 방어 감소 (스택당 DEF-15, 5초)
    public int shredRes;          // 저항 감소
    public float healOnHit;       // 명중 시 기차 회복
    public float explodeRadius;   // 폭발 반경 (0=폭발 없음)
    public int chainCount;        // 체인 전이 수

    // 패시브 종류 ("" = 발사형)
    // maxhp / regen / thorns / auraBurn / auraSlow / auraShred / dr / omega
    public string passiveType;
    public float passiveValue;    // regen량, dr비율 등

    // 버프 종류 ("" = 버프 아님) : "pd"=물리공격, "md"=주문력, "as"=공속
    public string buffType;
    public float buffValue;

    // 장판(Field) 옵션
    public bool fieldBig;     // true면 장판 반경 130, 아니면 90
    public bool fieldPoison;  // 장판에 독 도트가 붙음

    public string description;    // 효과 설명 (전투 정보)

    // v2: 도감 플레이버 텍스트 (세계관 한 줄. 첫 발견 시 크게 표시 + 도감 영구 기록)
    public string flavor = "";
}
