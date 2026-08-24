// 재료 6종 정의
// 적 6종이 각자 다른 재료를 드롭한다 (랩터→고기, 안킬로→등심, 프테라→전기, 카르노→화염, 모사→얼음, 독침전갈→독)
public enum MaterialType
{
    Meat,    // 랩터 고기 🥩
    Armor,   // 단단한 등심 🦴
    Elec,    // 전기 꼬리 ⚡
    Fire,    // 화염꽃 🌶
    Ice,     // 얼음꽃 ❄
    Poison   // 독침 🟣
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
    PhysDealer,  // ⚔ 물리 딜러
    MagicDealer, // ✨ 마법 딜러
    Debuffer,    // ⬇ 방깎/마깎
    Buffer,      // ⬆ 인접 버프
    CC,          // 🌀 슬로우/스턴
    Support      // 🛡 회복/장갑/반격
}

// 공격 형태 8종
public enum AttackShape
{
    Projectile, // 단일 투사체
    Pierce,     // 관통 레일 (일직선 전부)
    Cone,       // 부채꼴 방사 (화염방사)
    Explode,    // 착탄 폭발 (직스식)
    Chain,      // 체인 (스태틱식)
    Field,      // 장판 (바닥에 남음)
    Aura,       // 오라 (기차 주변 상시)
    Passive     // 상시 패시브
}

// 데미지 타입 (적 DEF/RES와 대응)
public enum DamageType
{
    Phys, // 물리 — 적 DEF에 감소
    Magic // 마법 — 적 RES에 감소
}