using UnityEngine;

/// <summary>
/// [TurretSlot.cs] v4 (B-2.2: 포탑 실물 비주얼)
/// 포탑 슬롯 1개. 요리를 투입하면 포탑으로 가동한다.
/// - 같은 요리 반복 투입 -> 레벨업 (Lv1=C, 2=B, 3~4=A, 5+=S)
/// - 발사형이면 쿨다운마다 가장 가까운 적 공격
/// - 패시브/버프/오라는 TurretSlotManager가 일괄 처리
/// - v2 변경점: 증강 연동 (공격속도 AspdMul / 사거리 RangeMul)
/// - v3 변경점: 보스 낙뢰 패턴용 슬롯 마비 추가
/// - v4 변경점 (플레이 피드백 "포탑이랑 기차가 따로 논다"):
///   지금까지 슬롯은 월드 그림이 0개였다 - 화면의 마커 칩(이름표)이 포탑 행세를
///   하며 지붕선을 가리던 것이 어색함의 정체. 받침+몸통+포신+속성 램프를
///   코드 도형으로 지붕 위에 세운다 (아트 반영 전 임시, TurretVisuals 스위치).
///   레벨업 = 조금씩 커짐 / 2티어 = 우람+안테나 / 마비 = 몸통 틴트(달아오름·서리·스파크)
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class TurretSlot : MonoBehaviour
{
    [Header("─ 슬롯 상태 (런타임) ─")]
    public string recipeId = "";   // 투입된 요리 키 ("" = 빈 슬롯)
    public int level = 0;          // 현재 레벨
    public bool isLocked = false;  // 잠금 슬롯 (증강 '증축된 주방 칸'으로 해금)

    [Header("─ 발사 설정 ─")]
    public float targetRange = 15f;    // 타겟 탐색 사거리
    public Transform firePoint;        // 발사 위치 (없으면 자기 위치)

    private float cooldownTimer = 0f;

    // ── v3: 슬롯 마비 (보스 '낙뢰 폭격' / P1: 모사 빙결 / B-2: 과열) ──
    // 마비 중에는 발사 정지. 해제는 근접 [E] (감전/빙결 = 즉시, 과열 = 홀드 냉각)
    private float stunUntil = 0f;

    /// <summary>마비 종류 표기 ("감전"/"빙결"/"과열") - SlotMarkerUI가 표시에 사용</summary>
    public string StunKind = "감전";

    public bool IsStunned { get { return Time.time < stunUntil; } }

    // ── B-2: 과열 상태 (연속 사격 누적 - 병기 유지 손맛) ──
    private int shotsSinceCool = 0;        // 마지막 냉각 후 사격 수
    private int overheatThreshold = 0;     // 이번 과열 임계 (0 = 미정, 발사 시 롤)
    private float overheatImmuneUntil = 0f; // 냉각 직후 재과열 면역

    /// <summary>슬롯 마비 (보스 낙뢰 - 기존 호환용, 감전 표기)</summary>
    public void StunSlot(float seconds) { StunSlot(seconds, "감전"); }

    /// <summary>슬롯 마비 + 종류 지정 (P1: 모사 빙결 등 - 같은 기믹, 다른 표기)</summary>
    public void StunSlot(float seconds, string kind)
    {
        // B-2: 과열 중에는 낙뢰/빙결이 덮어쓰지 못한다
        // (짧은 마비로 덮이면 냉각 작업 없이 과열이 풀리는 사고 방지)
        if (IsStunned && StunKind == "과열" && kind != "과열") return;

        // Phase 2-2 증강 '부동액 배관': 감전/빙결 지속 단축 (과열은 무기한이라 무관)
        stunUntil = Time.time + seconds * AugmentManager.SlotStunDurMul;
        StunKind = kind;
    }

    /// <summary>마비 즉시 해제. 과열이었다면 냉각 후 면역 시간 부여</summary>
    public void ClearStun()
    {
        if (StunKind == "과열")
        {
            overheatImmuneUntil = Time.time + GameBalance.OverheatImmuneTime;
            shotsSinceCool = 0;
            overheatThreshold = 0;
        }
        stunUntil = 0f;
    }

    // 현재 투입된 레시피 데이터 (없으면 null)
    public RecipeData Recipe
    {
        get { return string.IsNullOrEmpty(recipeId) ? null : RecipeDatabase.Get(recipeId); }
    }

    public bool IsEmpty { get { return string.IsNullOrEmpty(recipeId); } }

    // 레벨 배율: 1 + 0.6 * (Lv-1)  (프로토타입 v3 검증값)
    public float LevelMult
    {
        get { return level <= 0 ? 1f : 1f + 0.6f * (level - 1); }
    }

    // 등급 문자열 (UI용)
    public string GradeName
    {
        get
        {
            if (level >= 5) return "S";
            if (level >= 3) return "A";
            if (level >= 2) return "B";
            return "C";
        }
    }

    /// <summary>요리 투입 시도. 성공하면 true</summary>
    public bool TryInsertFood(string id)
    {
        // 잠금 슬롯에는 투입 불가
        if (isLocked)
        {
            Debug.Log("[TurretSlot] 잠긴 슬롯 - 증강 '증축된 주방 칸'으로 해금 필요");
            return false;
        }

        // 빈 슬롯이거나 같은 요리만 가능
        if (!IsEmpty && recipeId != id) return false;

        RecipeData r = RecipeDatabase.Get(id);
        if (r == null) return false;

        bool wasEmpty = IsEmpty;   // P1+: 새 포탑 탄생인지 (레벨업 투입과 구분)

        recipeId = id;
        level += 1;

        // P1+: 요리 숙련 '장인의 감각'(50회) - 빈 슬롯에 새로 배치할 때 시작 레벨 +1
        if (wasEmpty && MetaProgress.GetMasteryTier(id) >= GameBalance.MasteryStartLevelTier)
        {
            level += 1;
            Debug.Log("[TurretSlot] 장인의 감각 - " + r.displayName + " 시작 Lv" + level);
        }

        // Phase 2-3 증강 '선대의 기본기': T1 새 배치 시작 레벨 +1 (숙련 보너스와 중첩 가능)
        if (wasEmpty && AugmentManager.BasicsDoctrine && r.tier == 1)
        {
            level += 1;
            Debug.Log("[TurretSlot] 선대의 기본기 - " + r.displayName + " 시작 Lv" + level);
        }

        // 최대HP형 패시브는 즉시 기차에 적용
        if (r.passiveType == "maxhp" || r.passiveType == "omega")
        {
            TrainManager tm = FindFirstObjectByType<TrainManager>();
            if (tm != null) tm.AddMaxHP(r.passiveType == "omega" ? 120f : r.passiveValue);
        }

        Debug.Log("[TurretSlot] " + r.displayName + " 투입! " + GradeName + "등급 Lv" + level);
        return true;
    }

    /// <summary>슬롯 비우기 (합체 재료로 소모 - 환급 없음)</summary>
    public void ClearSlot()
    {
        recipeId = "";
        level = 0;
        cooldownTimer = 0f;
    }

    /// <summary>포탑 직접 설정 (합체 진화 결과용). 최대HP형 패시브는 1회 적용</summary>
    public void SetTurret(string id, int newLevel)
    {
        RecipeData r = RecipeDatabase.Get(id);
        if (r == null) return;

        recipeId = id;
        level = Mathf.Max(1, newLevel);
        cooldownTimer = 0f;

        if (r.passiveType == "maxhp" || r.passiveType == "omega")
        {
            TrainManager tm = FindFirstObjectByType<TrainManager>();
            if (tm != null) tm.AddMaxHP(r.passiveType == "omega" ? 120f : r.passiveValue);
        }

        Debug.Log("[TurretSlot] 합체 결과: " + r.displayName + " " + GradeName + "등급 Lv" + level);
    }

    /// <summary>슬롯 비우기 (폐기). 반환값: 환급 재료 수</summary>
    public int Scrap()
    {
        if (IsEmpty) return 0;
        int refund = Mathf.Max(1, level);
        Debug.Log("[TurretSlot] " + Recipe.displayName + " 폐기, 재료 " + refund + "개 환급");
        recipeId = "";
        level = 0;
        cooldownTimer = 0f;
        return refund;
    }

    /// <summary>매 프레임 발사 처리 (TurretSlotManager가 호출)</summary>
    public void TickFire(float deltaTime, float buffAttackSpeed, float buffDamage)
    {
        if (isLocked) return;
        if (IsStunned) return;   // v3: 낙뢰 마비 중 발사 정지
        RecipeData r = Recipe;
        if (r == null) return;
        if (r.shape == AttackShape.Passive || r.shape == AttackShape.Aura) return;
        if (!string.IsNullOrEmpty(r.buffType)) return; // 버프형은 발사 안 함

        cooldownTimer -= deltaTime;
        if (cooldownTimer > 0f) return;

        // 가장 가까운 적 탐색
        Enemy target = FindNearestEnemy();
        if (target == null) return;

        // 쿨다운 리셋 (인접 버프 + 증강 공속 반영)
        // Phase 2-2 증강 '최후의 만찬': 기차 저체력이면 공속 상승 (배수진의 화력)
        float rushMul = 1f;
        if (AugmentManager.LastSupperRush && TrainManager.Instance != null
            && TrainManager.Instance.HPRatio <= GameBalance.LastSupperHPRatio)
            rushMul = GameBalance.LastSupperAspdMul;

        cooldownTimer = r.cooldown / ((1f + buffAttackSpeed) * AugmentManager.AspdMul * rushMul);

        // 최종 데미지 = 기본 x 레벨배율 x (1+버프)
        // (전역 배율/증강 데미지는 TurretAttackExecutor.DealDamage에서 적용)
        float finalDamage = r.damage * LevelMult * (1f + buffDamage);

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        TurretAttackExecutor.Execute(r, origin, target, finalDamage);

        // ── B-2 과열: 쉬지 않고 불을 뿜으면 쇳물도 지친다 ──
        // 임계는 포탑마다 랜덤 + 레벨 높을수록 빨리 (캐리 포탑일수록 손이 간다)
        // 빈도 제어: 기차 전체 최소 간격 + 다른 마비와 동시 발생 금지 (헌법: 동시 위기 1)
        if (GameBalance.OverheatEnabled)
        {
            if (overheatThreshold <= 0)
                overheatThreshold = Mathf.Max(10,
                    Random.Range(GameBalance.OverheatShotsMin, GameBalance.OverheatShotsMax + 1)
                    - (level - 1) * GameBalance.OverheatPerLevel);

            shotsSinceCool++;
            if (shotsSinceCool >= overheatThreshold
                && Time.time >= overheatImmuneUntil
                && TurretSlotManager.Instance != null
                && TurretSlotManager.Instance.CanOverheatNow())
            {
                TurretSlotManager.Instance.NoteOverheat();
                StunSlot(9999f, "과열");
                SoundManager.Play("sfx_overheat");   // 클립 없으면 무시
                UIManager.Instance?.ShowDanger("포탑 과열! 달려가서 [E]를 꾹 눌러 식혀라!");
                Debug.Log("[TurretSlot] " + (Recipe != null ? Recipe.displayName : "?")
                    + " 과열 (사격 " + shotsSinceCool + "발)");
            }
        }
    }

    private Enemy FindNearestEnemy()
    {
        Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Enemy best = null;
        // 증강 사거리 배율 반영
        // 플레이테스트 픽스: 사거리는 GameBalance.TurretRange가 단일 소스 (구 15는 4칸
        // 기차에서 반대편을 무는 적이 사각에 들어갔다 - targetRange 필드는 무시)
        float bestDist = GameBalance.TurretRange * AugmentManager.RangeMul;
        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].IsAlive) continue;
            float d = Vector3.Distance(transform.position, all[i].transform.position);
            if (d < bestDist) { bestDist = d; best = all[i]; }
        }
        return best;
    }

    // ──────────────────────────────────────────────
    //  B-2.2: 포탑 실물 비주얼 (코드 도형)
    // ──────────────────────────────────────────────
    private static readonly Color BODY_IRON = new Color(0.20f, 0.16f, 0.13f);   // 받침/포신 (검정 포인트)
    private static readonly Color BODY_COPPER = new Color(0.55f, 0.35f, 0.20f); // 몸통 (구리)

    private Transform visualRoot;          // 도형 묶음 (상태가 바뀌면 통째로 다시 그림)
    private SpriteRenderer bodySr;         // 몸통 렌더러 (마비 틴트용)
    private string vRecipeId = null;       // 마지막으로 그린 상태 캐시
    private int vLevel = -1;
    private bool vLocked = false;

    private void Awake()
    {
        RebuildVisual();
    }

    private void Update()
    {
        if (!GameBalance.TurretVisuals)
        {
            if (visualRoot != null) { Destroy(visualRoot.gameObject); visualRoot = null; }
            return;
        }

        // 상태(요리/레벨/잠금)가 바뀐 프레임에만 다시 그린다
        if (recipeId != vRecipeId || level != vLevel || isLocked != vLocked)
            RebuildVisual();

        // 마비 틴트: 과열=달아오름 / 빙결=서리 / 감전=스파크빛 (해제되면 원래 구리색)
        if (bodySr != null)
        {
            Color c = BODY_COPPER;
            if (IsStunned)
            {
                if (StunKind == "과열") c = Color.Lerp(c, new Color(1f, 0.25f, 0.1f), 0.75f);
                else if (StunKind == "빙결") c = Color.Lerp(c, new Color(0.5f, 0.8f, 1f), 0.65f);
                else c = Color.Lerp(c, new Color(1f, 0.95f, 0.3f), 0.5f);
            }
            bodySr.color = c;
        }
    }

    /// <summary>포탑 도형을 현 상태에 맞게 다시 그린다 (잠김/빈 슬롯/가동 중)</summary>
    private void RebuildVisual()
    {
        vRecipeId = recipeId;
        vLevel = level;
        vLocked = isLocked;

        if (visualRoot != null) Destroy(visualRoot.gameObject);
        bodySr = null;
        if (!GameBalance.TurretVisuals) return;

        GameObject rootGo = new GameObject("TurretVisual");
        visualRoot = rootGo.transform;
        visualRoot.SetParent(transform, false);   // 슬롯(지붕 자리)에 따라붙는다

        if (isLocked)
        {
            // 잠금 슬롯: 어두운 빈 거치대만 (마커 칩이 "잠김" 설명을 담당)
            MakePart("Base", 0f, -0.07f, 0.5f, 0.14f, 0f, new Color(0.13f, 0.10f, 0.08f), false);
            return;
        }

        // 받침: 지붕선(1.8)에 딱 앉는 거치대 (SlotY 1.95 기준 - 밑면이 지붕과 접합)
        MakePart("Base", 0f, -0.07f, 0.62f, 0.16f, 0f, BODY_IRON, false);

        if (IsEmpty)
        {
            // 빈 슬롯: 거치 핀 - "여기 요리를 꽂아라" 자리 표시
            MakePart("Pin", 0f, 0.12f, 0.10f, 0.22f, 0f, new Color(0.34f, 0.26f, 0.20f), false);
            return;
        }

        RecipeData r = Recipe;
        bool tier2 = r != null && r.tier >= 2;

        // 레벨이 오르면 조금씩 커진다 (C 1.0 ~ S급 언저리 1.27)
        float grow = Mathf.Min(1.27f, 1f + 0.09f * (level - 1));
        visualRoot.localScale = new Vector3(grow, grow, 1f);

        // 몸통 (2티어는 더 우람하게)
        SpriteRenderer body = MakePart("Body", 0f, 0.19f,
            tier2 ? 0.52f : 0.44f, tier2 ? 0.42f : 0.34f, 0f, BODY_COPPER, false);
        bodySr = body;

        // 포신: 적이 오는 오른쪽 위로 비스듬히
        MakePart("Barrel", 0.26f, 0.37f, 0.5f, 0.11f, 18f, BODY_IRON, false);

        // 속성 램프 (공명 속성과 같은 기준색 - 한눈에 덱 구성이 읽힌다)
        MakePart("Lamp", -0.06f, 0.21f, 0.15f, 0.15f, 0f, TagColor(r), true);

        // 2티어 안테나 (정예의 상징)
        if (tier2)
            MakePart("Antenna", -0.18f, 0.48f, 0.05f, 0.26f, -8f, BODY_IRON, false);
    }

    /// <summary>도형 파츠 1개 생성 (TrainDeck의 공용 스프라이트 재사용)</summary>
    private SpriteRenderer MakePart(string partName, float x, float y, float w, float h,
        float tiltZ, Color color, bool circle)
    {
        GameObject go = new GameObject(partName);
        go.transform.SetParent(visualRoot, false);
        go.transform.localPosition = new Vector3(x, y, 0f);
        go.transform.localScale = new Vector3(w, h, 1f);
        go.transform.localEulerAngles = new Vector3(0f, 0f, tiltZ);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circle ? TrainDeck.GetCircleSprite() : TrainDeck.GetWhiteSprite();
        sr.color = color;
        sr.sortingOrder = -3;   // 데크(-6~-4) 위, 셰프/적(0+) 아래
        return sr;
    }

    /// <summary>속성 램프 색 (FoodTag = 공명 속성과 동일 기준)</summary>
    private Color TagColor(RecipeData r)
    {
        if (r == null) return new Color(0.85f, 0.8f, 0.7f);
        if (r.tag == FoodTag.Fire) return new Color(1f, 0.45f, 0.15f);      // 화염 주황
        if (r.tag == FoodTag.Elec) return new Color(1f, 0.85f, 0.25f);      // 전기 노랑
        if (r.tag == FoodTag.Ice) return new Color(0.45f, 0.85f, 1f);       // 냉기 하늘
        if (r.tag == FoodTag.Poison) return new Color(0.72f, 0.42f, 0.9f);  // 독 보라
        if (r.tag == FoodTag.Def) return new Color(0.4f, 0.8f, 0.45f);      // 방어 초록
        return new Color(0.85f, 0.8f, 0.7f);                                // 물리 강철빛
    }
}
