using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [RecipeDatabase.cs] v2
/// 레시피 데이터베이스 (정적 정의)
/// 프로토타입 v3에서 밸런스 검증된 수치를 그대로 사용
/// - v2 변경점: 도감 플레이버 텍스트 42종 추가 (스토리바이블 Phase 1)
///   첫 발견 시 FoodStock -> StoryTexts가 크게 표시한다.
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public static class RecipeDatabase
{
    private static Dictionary<string, RecipeData> _all;

    // 재료 2개 → 정렬된 조합 키 생성 (순서 무관)
    public static string MakeKey(MaterialType a, MaterialType b)
    {
        // enum 이름을 소문자로 정렬 결합 → "fire+meat" 형태
        string sa = a.ToString().ToLower();
        string sb = b.ToString().ToLower();
        return string.CompareOrdinal(sa, sb) <= 0 ? sa + "+" + sb : sb + "+" + sa;
    }

    // 태그 2개 → 정렬된 합성 키 생성 (순서 무관)
    public static string MakeTagKey(FoodTag a, FoodTag b)
    {
        string sa = a.ToString().ToLower();
        string sb = b.ToString().ToLower();
        string body = string.CompareOrdinal(sa, sb) <= 0 ? sa + "+" + sb : sb + "+" + sa;
        return "T2:" + body;
    }

    // T1 요리 2개의 태그로 T2 결과 조회
    public static RecipeData GetFusion(FoodTag a, FoodTag b)
    {
        return Get(MakeTagKey(a, b));
    }

    public static RecipeData Get(string key)
    {
        EnsureInit();
        RecipeData r;
        return _all.TryGetValue(key, out r) ? r : null;
    }

    public static RecipeData GetByMaterials(MaterialType a, MaterialType b)
    {
        return Get(MakeKey(a, b));
    }

    public static IEnumerable<RecipeData> All
    {
        get { EnsureInit(); return _all.Values; }
    }

    private static void EnsureInit()
    {
        if (_all != null) return;
        _all = new Dictionary<string, RecipeData>();

        // ─── T1: 재료 조합 21종 ───
        // 물리 계열 (5)
        AddT1("meat+meat", "더블 육포", FoodTag.Phys, TurretRole.PhysDealer, AttackShape.Projectile, DamageType.Phys,
              26f, 1.0f, 430f, r => { r.description = "묵직한 물리 강타"; });
        AddT1("armor+meat", "하티 스테이크", FoodTag.Phys, TurretRole.Support, AttackShape.Projectile, DamageType.Phys,
              14f, 1.0f, 420f, r => { r.healOnHit = 1f; r.description = "물리탄 · 명중 시 기차 HP+1"; });
        AddT1("fire+meat", "매운 육포", FoodTag.Phys, TurretRole.PhysDealer, AttackShape.Projectile, DamageType.Phys,
              13f, 0.9f, 450f, r => { r.burnStack = 1; r.description = "물리탄 + 화상 도트"; });
        AddT1("ice+meat", "냉동 육포", FoodTag.Phys, TurretRole.CC, AttackShape.Projectile, DamageType.Phys,
              12f, 0.9f, 420f, r => { r.slowLevel = 1; r.description = "물리탄 + 감속"; });
        AddT1("meat+poison", "독침 육포", FoodTag.Phys, TurretRole.Debuffer, AttackShape.Projectile, DamageType.Phys,
              10f, 0.9f, 430f, r => { r.shredDef = 1; r.description = "물리탄 + 방어력 감소(방깎)"; });

        // 전기 계열 (3)
        AddT1("elec+meat", "전기 스테이크", FoodTag.Elec, TurretRole.CC, AttackShape.Projectile, DamageType.Magic,
              12f, 0.8f, 520f, r => { r.stunSec = 0.45f; r.description = "감전탄 · 짧은 스턴"; });
        AddT1("elec+elec", "과부하 코일", FoodTag.Elec, TurretRole.MagicDealer, AttackShape.Chain, DamageType.Magic,
              9f, 0.55f, 600f, r => { r.chainCount = 3; r.description = "체인 라이트닝 · 3체 전이"; });
        AddT1("elec+fire", "플라즈마 볶음", FoodTag.Elec, TurretRole.MagicDealer, AttackShape.Pierce, DamageType.Magic,
              11f, 1.1f, 0f, r => { r.description = "관통 레일 · 일직선 전부 타격"; });

        // 화염 계열 (4)
        AddT1("armor+fire", "화염 방벽", FoodTag.Fire, TurretRole.MagicDealer, AttackShape.Aura, DamageType.Magic,
              0f, 0f, 0f, r => { r.passiveType = "auraBurn"; r.description = "[오라] 근접 적에게 화상"; });
        AddT1("fire+fire", "용암 폭탄밥", FoodTag.Fire, TurretRole.MagicDealer, AttackShape.Explode, DamageType.Magic,
              15f, 1.3f, 360f, r => { r.explodeRadius = 70f; r.description = "착탄 폭발 (직스식)"; });
        AddT1("fire+ice", "증기 폭발", FoodTag.Fire, TurretRole.MagicDealer, AttackShape.Explode, DamageType.Magic,
              11f, 1.4f, 340f, r => { r.explodeRadius = 100f; r.slowLevel = 1; r.description = "대범위 폭발 + 감속"; });
        AddT1("fire+poison", "맹독 화염방사", FoodTag.Fire, TurretRole.MagicDealer, AttackShape.Cone, DamageType.Magic,
              5f, 0.4f, 0f, r => { r.burnStack = 1; r.description = "부채꼴 지속 방사 + 화상"; });

        // 냉기 계열 (4)
        AddT1("armor+ice", "빙벽 스튜", FoodTag.Ice, TurretRole.CC, AttackShape.Aura, DamageType.Magic,
              0f, 0f, 0f, r => { r.passiveType = "auraSlow"; r.description = "[오라] 근접 적 감속"; });
        AddT1("elec+ice", "정전기 서리", FoodTag.Ice, TurretRole.CC, AttackShape.Projectile, DamageType.Magic,
              8f, 0.7f, 500f, r => { r.slowLevel = 1; r.stunSec = 0.3f; r.description = "감속 + 짧은 감전"; });
        AddT1("ice+ice", "절대영도 수프", FoodTag.Ice, TurretRole.CC, AttackShape.Field, DamageType.Magic,
              5f, 1.6f, 340f, r => { r.slowLevel = 2; r.description = "착탄 지점에 강감속 장판"; });
        AddT1("ice+poison", "맹독 빙수", FoodTag.Ice, TurretRole.MagicDealer, AttackShape.Projectile, DamageType.Magic,
              7f, 1.0f, 400f, r => { r.slowLevel = 1; r.poisonStack = 1; r.description = "감속 + 독 도트"; });

        // 독 계열 (2)
        AddT1("elec+poison", "마비독 꼬치", FoodTag.Poison, TurretRole.Debuffer, AttackShape.Projectile, DamageType.Magic,
              8f, 0.8f, 500f, r => { r.shredRes = 1; r.description = "마법탄 + 마법저항 감소(마깎)"; });
        AddT1("poison+poison", "맹독 진액", FoodTag.Poison, TurretRole.MagicDealer, AttackShape.Projectile, DamageType.Magic,
              6f, 0.9f, 430f, r => { r.poisonStack = 2; r.description = "맹독 · 도트 2중첩"; });

        // 방어 계열 (3)
        AddT1("armor+armor", "철판 정식", FoodTag.Def, TurretRole.Support, AttackShape.Passive, DamageType.Phys,
              0f, 0f, 0f, r => { r.passiveType = "maxhp"; r.passiveValue = 60f; r.description = "[상시] 기차 최대 HP +60"; });
        AddT1("armor+elec", "축전 장갑", FoodTag.Def, TurretRole.Support, AttackShape.Passive, DamageType.Magic,
              0f, 0f, 0f, r => { r.passiveType = "thorns"; r.description = "[상시] 기차 피격 시 감전 반격"; });
        AddT1("armor+poison", "해독 스튜", FoodTag.Def, TurretRole.Support, AttackShape.Passive, DamageType.Phys,
              0f, 0f, 0f, r => { r.passiveType = "regen"; r.passiveValue = 2f; r.description = "[상시] 기차 HP 초당 +2"; });

        // ─── T2: 태그 합성 21종 (전설 요리) ───
        // 물리 조합
        AddT2(FoodTag.Phys, FoodTag.Phys, "거포 정식", TurretRole.PhysDealer, AttackShape.Projectile, DamageType.Phys,
              70f, 1.6f, 460f, r => { r.description = "초강력 물리 강타"; });
        AddT2(FoodTag.Phys, FoodTag.Elec, "개틀링 티렉스", TurretRole.PhysDealer, AttackShape.Projectile, DamageType.Phys,
              8f, 0.14f, 640f, r => { r.description = "초고속 연사 물리탄"; });
        AddT2(FoodTag.Phys, FoodTag.Fire, "화포 바베큐", TurretRole.PhysDealer, AttackShape.Explode, DamageType.Phys,
              30f, 1.4f, 380f, r => { r.explodeRadius = 85f; r.description = "물리 폭발탄"; });
        AddT2(FoodTag.Phys, FoodTag.Ice, "얼음송곳 정식", TurretRole.PhysDealer, AttackShape.Pierce, DamageType.Phys,
              24f, 1.0f, 0f, r => { r.slowLevel = 1; r.description = "관통 물리 레일 + 감속"; });
        AddT2(FoodTag.Phys, FoodTag.Poison, "부식탄 정식", TurretRole.Debuffer, AttackShape.Projectile, DamageType.Phys,
              18f, 0.9f, 450f, r => { r.shredDef = 2; r.description = "물리탄 + 강력 방깎(2중)"; });
        AddT2(FoodTag.Phys, FoodTag.Def, "지휘관의 만찬", TurretRole.Buffer, AttackShape.Passive, DamageType.Phys,
              0f, 0f, 0f, r => { r.buffType = "pd"; r.buffValue = 0.4f; r.description = "[버프] 인접 슬롯 물리 공격력 +40%"; });

        // 전기 조합
        AddT2(FoodTag.Elec, FoodTag.Elec, "테슬라 갓 핑거", TurretRole.MagicDealer, AttackShape.Chain, DamageType.Magic,
              14f, 0.5f, 640f, r => { r.chainCount = 6; r.description = "체인 라이트닝 6체 전이"; });
        AddT2(FoodTag.Elec, FoodTag.Fire, "플라즈마 캐논", TurretRole.MagicDealer, AttackShape.Pierce, DamageType.Magic,
              26f, 1.2f, 0f, r => { r.burnStack = 1; r.description = "강화 관통 레일 + 화상"; });
        AddT2(FoodTag.Elec, FoodTag.Ice, "뇌빙 결정포", TurretRole.CC, AttackShape.Explode, DamageType.Magic,
              18f, 1.3f, 400f, r => { r.explodeRadius = 80f; r.stunSec = 0.6f; r.description = "폭발 + 스턴 0.6초"; });
        AddT2(FoodTag.Elec, FoodTag.Poison, "신경독 코일", TurretRole.Debuffer, AttackShape.Chain, DamageType.Magic,
              10f, 0.7f, 580f, r => { r.chainCount = 3; r.shredRes = 1; r.description = "체인 3 + 마깎 전파"; });
        AddT2(FoodTag.Elec, FoodTag.Def, "축포의 연회", TurretRole.Buffer, AttackShape.Passive, DamageType.Phys,
              0f, 0f, 0f, r => { r.buffType = "as"; r.buffValue = 0.3f; r.description = "[버프] 인접 슬롯 공격 속도 +30%"; });

        // 화염 조합
        AddT2(FoodTag.Fire, FoodTag.Fire, "태양의 심장포", TurretRole.MagicDealer, AttackShape.Explode, DamageType.Magic,
              34f, 1.8f, 340f, r => { r.explodeRadius = 140f; r.burnStack = 1; r.description = "초대형 폭발 (반경 140)"; });
        AddT2(FoodTag.Fire, FoodTag.Ice, "증기 기관포", TurretRole.MagicDealer, AttackShape.Explode, DamageType.Magic,
              24f, 1.5f, 350f, r => { r.explodeRadius = 120f; r.slowLevel = 2; r.description = "대범위 폭발 + 강감속"; });
        AddT2(FoodTag.Fire, FoodTag.Poison, "지옥불 정찬", TurretRole.MagicDealer, AttackShape.Cone, DamageType.Magic,
              9f, 0.35f, 0f, r => { r.burnStack = 1; r.poisonStack = 1; r.description = "강화 부채꼴 방사 + 화상/독"; });
        AddT2(FoodTag.Fire, FoodTag.Def, "마법사의 만찬", TurretRole.Buffer, AttackShape.Passive, DamageType.Phys,
              0f, 0f, 0f, r => { r.buffType = "md"; r.buffValue = 0.4f; r.description = "[버프] 인접 슬롯 주문력 +40%"; });

        // 냉기 조합
        AddT2(FoodTag.Ice, FoodTag.Ice, "절대영도 엔진", TurretRole.CC, AttackShape.Field, DamageType.Magic,
              8f, 1.4f, 340f, r => { r.slowLevel = 2; r.fieldBig = true; r.description = "대형 강감속 장판"; });
        AddT2(FoodTag.Ice, FoodTag.Poison, "영구동토 진액", TurretRole.MagicDealer, AttackShape.Field, DamageType.Magic,
              7f, 1.4f, 340f, r => { r.slowLevel = 1; r.fieldPoison = true; r.description = "감속 + 맹독 장판"; });
        AddT2(FoodTag.Ice, FoodTag.Def, "수정 방패 연회", TurretRole.Support, AttackShape.Passive, DamageType.Phys,
              0f, 0f, 0f, r => { r.passiveType = "dr"; r.passiveValue = 0.2f; r.description = "[상시] 기차 받는 피해 -20%"; });

        // 독/방어 조합
        AddT2(FoodTag.Poison, FoodTag.Poison, "부식의 정수", TurretRole.Debuffer, AttackShape.Aura, DamageType.Magic,
              0f, 0f, 0f, r => { r.passiveType = "auraShred"; r.description = "[오라] 주변 적 방깎+마깎"; });
        AddT2(FoodTag.Poison, FoodTag.Def, "정화의 성찬", TurretRole.Support, AttackShape.Passive, DamageType.Phys,
              0f, 0f, 0f, r => { r.passiveType = "regen"; r.passiveValue = 5f; r.description = "[상시] 기차 HP 초당 +5"; });
        AddT2(FoodTag.Def, FoodTag.Def, "오메가 리페어", TurretRole.Support, AttackShape.Passive, DamageType.Phys,
              0f, 0f, 0f, r => { r.passiveType = "omega"; r.passiveValue = 120f; r.description = "[상시] 최대HP+120, 초당 +3 회복"; });

        // ─── v2: 도감 플레이버 텍스트 (스토리바이블 - 첫 발견 시 크게 표시) ───
        ApplyFlavors();
    }

    // ==================================================================
    //  도감 플레이버 42종 - 세계관 한 줄씩
    //  ("선대" = 최초의 셰프. 일부 문구는 스토리바이블 확정본 그대로)
    // ==================================================================
    private static void ApplyFlavors()
    {
        // T1 물리
        SetFlavor("더블 육포", "황야의 기본기. 두 배로 질기고, 두 배로 든든하다.");
        SetFlavor("하티 스테이크", "심장 근처 부위는 왜인지 기차가 좋아한다. 이유는 묻지 않기로 했다.");
        SetFlavor("매운 육포", "선대의 메모 - 매운맛은 화력이다. 문자 그대로.");
        SetFlavor("냉동 육포", "씹는 데 오래 걸린다. 적이 걷는 데 오래 걸리는 것과 같은 이치다.");
        SetFlavor("독침 육포", "프테라의 독침은 버리지 마라. 갑옷 이음새에 잘 스며든다.");

        // T1 전기
        SetFlavor("전기 스테이크", "혀가 아니라 척추로 맛보는 요리. 손님은 잠시 멈춘다.");
        SetFlavor("과부하 코일", "국물이 튀는 방향으로 번개도 튄다. 셋째 그릇까지.");
        SetFlavor("플라즈마 볶음", "너무 뜨거워서 접시를 뚫는다. 줄 서 있는 손님 전부에게 서빙된다.");

        // T1 화염
        SetFlavor("화염 방벽", "가까이 오는 손님은 전채부터 태워 드린다.");
        SetFlavor("용암 폭탄밥", "밥알 하나하나가 화산이다. 씹는 순간을 조심할 것.");
        SetFlavor("증기 폭발", "얼음과 불을 한 냄비에 넣으면 요리가 아니라 기상 현상이 된다.");
        SetFlavor("맹독 화염방사", "주방장 특선. 부채꼴로 서빙되며 환불은 불가능하다.");

        // T1 냉기
        SetFlavor("빙벽 스튜", "차갑게 식은 스튜 곁에서는 누구도 서두르지 못한다.");
        SetFlavor("정전기 서리", "겨울 담요에서 튀는 그 불꽃. 그걸 포탄만 하게 키웠다.");
        SetFlavor("절대영도 수프", "모사사우루스가 기억하는 마지막 바다의 온도.");
        SetFlavor("맹독 빙수", "천천히 녹고, 천천히 퍼진다. 서두르는 건 손님의 죽음뿐.");

        // T1 독
        SetFlavor("마비독 꼬치", "마법 저항이라는 게 있다면, 이 꼬치는 그걸 녹이는 소스다.");
        SetFlavor("맹독 진액", "한 방울은 약. 두 방울은 요리. 세 방울은 실례.");

        // T1 방어
        SetFlavor("철판 정식", "철판째 먹는 정식. 기차의 뼈대가 두꺼워진다.");
        SetFlavor("축전 장갑", "만지면 찌릿한 갑옷. 무는 쪽이 더 아프다.");
        SetFlavor("해독 스튜", "선대의 메모 - 독은 미워해도 독개구리는 미워하지 말 것.");

        // T2 물리
        SetFlavor("거포 정식", "한 발 쏘고 한 끼 먹는다. 순서는 바뀌어도 된다.");
        SetFlavor("개틀링 티렉스", "씹기도 전에 다음 숟갈이 온다. 총열도 같은 심정이다.");
        SetFlavor("화포 바베큐", "구운 고기가 터지는 게 아니다. 터지는 걸 굽는 거다.");
        SetFlavor("얼음송곳 정식", "송곳은 줄을 서지 않는다. 줄을 뚫는다.");
        SetFlavor("부식탄 정식", "갑옷째 부드러워지는 마법. 정확히는 요리.");
        SetFlavor("지휘관의 만찬", "잘 먹인 포대는 배신하지 않는다. 선대의 병법서 1페이지.");

        // T2 전기
        SetFlavor("테슬라 갓 핑거", "신의 손가락이 아니다. 601번째 실패작이다. 성공했을 뿐.");
        SetFlavor("플라즈마 캐논", "협곡의 번개를 통조림으로 만들려던 시도의 부산물.");
        SetFlavor("뇌빙 결정포", "천둥과 서리가 같은 접시에 담기면 손님은 식사 중에 잠든다.");
        SetFlavor("신경독 코일", "저릿함이 옆 손님에게 옮는다. 식탁 예절의 붕괴.");
        SetFlavor("축포의 연회", "요리사가 바쁘면 포탑도 바빠진다. 주방의 오랜 법칙.");

        // T2 화염
        SetFlavor("태양의 심장포", "황야의 정오를 그릇에 담았다. 직시하지 말 것.");
        SetFlavor("증기 기관포", "기차가 마시는 차. 손님에게는 폭풍우.");
        SetFlavor("지옥불 정찬", "메뉴판에 없는 요리. 주문한 손님도 없는데 전원에게 나간다.");
        SetFlavor("마법사의 만찬", "비법은 향신료가 아니라 화력 조절이다. 옆 포탑이 증명한다.");

        // T2 냉기
        SetFlavor("절대영도 엔진", "이 장판 위에서는 시간도 반쯤 얼어붙는다.");
        SetFlavor("영구동토 진액", "녹지 않는 땅, 지워지지 않는 독. 광산의 유산.");
        SetFlavor("수정 방패 연회", "코발트 수정은 두 번 깨지지 않는다. 기차도 그걸 배웠다.");

        // T2 독/방어
        SetFlavor("부식의 정수", "가만히 놓아두기만 해도 갑옷이 한숨을 쉰다.");
        SetFlavor("정화의 성찬", "독으로 독을 씻는다. 선대는 이것을 '설거지'라 불렀다.");
        SetFlavor("오메가 리페어", "부서진 것을 고치는 요리가 아니다. 포기하지 않는 요리다.");
    }

    /// <summary>표시 이름으로 레시피를 찾아 플레이버 텍스트를 지정</summary>
    private static void SetFlavor(string displayName, string flavor)
    {
        foreach (RecipeData r in _all.Values)
        {
            if (r.displayName == displayName)
            {
                r.flavor = flavor;
                return;
            }
        }
        Debug.LogWarning("[RecipeDB] 플레이버 대상 레시피 없음: " + displayName);
    }

    // T1 등록 헬퍼 — 공통 필드 세팅 후 개별 효과는 콜백으로
    private static void AddT1(string key, string name, FoodTag tag, TurretRole role,
        AttackShape shape, DamageType dtype, float dmg, float cd, float spd,
        System.Action<RecipeData> extra)
    {
        RecipeData r = new RecipeData();
        r.recipeId = key;
        r.displayName = name;
        r.tier = 1;
        r.tag = tag;
        r.role = role;
        r.shape = shape;
        r.damageType = dtype;
        r.damage = dmg;
        r.cooldown = cd;
        r.projectileSpeed = spd;
        r.passiveType = "";
        r.buffType = "";
        if (extra != null) extra(r);
        _all[key] = r;
    }

    // T2 등록 헬퍼 — 태그 조합 키로 등록
    private static void AddT2(FoodTag tagA, FoodTag tagB, string name, TurretRole role,
        AttackShape shape, DamageType dtype, float dmg, float cd, float spd,
        System.Action<RecipeData> extra)
    {
        RecipeData r = new RecipeData();
        r.recipeId = MakeTagKey(tagA, tagB);
        r.displayName = name;
        r.tier = 2;
        r.tag = tagA; // T2는 태그가 합성에 안 쓰이므로 대표값만 저장
        r.role = role;
        r.shape = shape;
        r.damageType = dtype;
        r.damage = dmg;
        r.cooldown = cd;
        r.projectileSpeed = spd;
        r.passiveType = "";
        r.buffType = "";
        if (extra != null) extra(r);
        _all[r.recipeId] = r;
    }
}
