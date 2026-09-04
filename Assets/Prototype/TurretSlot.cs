using UnityEngine;

/// <summary>
/// [TurretSlot.cs] v6 (고퀄 PNG 적용 2026-09-03) / v5 탑뷰 재스킨 (2026-09-02)
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
/// - v5 (탑뷰 재스킨 - 목업 v2 컨펌): 도형 조합 -> 도트 스프라이트 (PixelPainter.cs 신규)
///   무쇠 베이스 링(볼트 8) + 속성 발광 링 + 구리 돔 + 중량 포신(외곽6/몸4/상단광1) + 머즐 브레이크
///   포신은 마지막 표적을 향해 회전, 표적이 없으면 북쪽을 보며 천천히 흔들린다.
///   좌표/판정/로직은 v4 그대로 - 바뀐 건 그리는 문법뿐. (마비 틴트는 돔에 적용, 2티어 = 포신 2연장)
/// - v6: Resources/Sprites/WDT/ 의 t_base / t_dome_<속성> / t_barrel / t_barrel2 PNG를 SpriteBank로 우선 사용.
///   없으면 v5 코드 도트로 폴백. 속성 키: fire/elec/ice/poison/def/phys
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
        lastTarget = target;   // v5: 포신이 이쪽을 향한다

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
    //  v5: 포탑 실물 비주얼 (탑뷰 도트 - 목업 v2 turret() 좌표를 옮김)
    // ──────────────────────────────────────────────
    /// <summary>포탑 도트 배율 (기차 20보다 촘촘하게 - 작은 물건이라 디테일 확보)</summary>
    private const float TURRET_PPU = 32f;
    private const int SORT_BASE = -3;      // 데크(-6~-4) 위
    private const int SORT_BARREL = -2;
    private const int SORT_DOME = -1;      // 셰프/적(0+) 아래

    private static Sprite baseSprite;      // 베이스 링 (공용 캐시)
    private static Sprite pinSprite;       // 빈 슬롯 페그 (공용 캐시)
    private static Sprite barrelSprite;    // 단일 포신
    private static Sprite barrelSprite2;   // 2티어: 2연장 포신

    private Transform visualRoot;          // 상태가 바뀌면 통째로 다시 그린다
    private Transform barrelPivot;         // 회전하는 포신
    private SpriteRenderer bodySr;         // 돔 렌더러 (마비 틴트용)
    private string vRecipeId = null;       // 마지막으로 그린 상태 캐시
    private int vLevel = -1;
    private bool vLocked = false;
    private Enemy lastTarget;              // 포신이 향할 표적
    private float barrelAngle = 90f;       // 현재 포신 각도 (0=동, 90=북)
    private float idlePhase;               // 슬롯마다 다른 흔들림 위상

    private void Awake()
    {
        idlePhase = Random.Range(0f, 6.28f);
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

        // 마비 틴트: 과열=달아오름 / 빙결=서리 / 마비=스파크색 (해제되면 구리로 복귀)
        if (bodySr != null)
        {
            Color c = Color.white;
            if (IsStunned)
            {
                if (StunKind == "과열") c = Color.Lerp(c, new Color(1f, 0.25f, 0.1f), 0.75f);
                else if (StunKind == "빙결") c = Color.Lerp(c, new Color(0.5f, 0.8f, 1f), 0.65f);
                else c = Color.Lerp(c, new Color(1f, 0.95f, 0.3f), 0.5f);
            }
            bodySr.color = c;
        }

        TickBarrel();
    }

    /// <summary>포신 회전: 표적이 살아 있으면 조준, 없으면 북쪽을 보며 천천히 흔들림</summary>
    private void TickBarrel()
    {
        if (barrelPivot == null) return;
        float want;
        if (lastTarget != null && lastTarget.IsAlive)
        {
            Vector3 d = lastTarget.transform.position - transform.position;
            want = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        }
        else
        {
            lastTarget = null;
            want = 90f + Mathf.Sin(Time.time * 0.8f + idlePhase) * 22f;
        }
        // 마비/과열 중엔 포신도 굳는다 (정지 상태가 눈에 보이게)
        float turnSpeed = IsStunned ? 0f : 420f;
        barrelAngle = Mathf.MoveTowardsAngle(barrelAngle, want, turnSpeed * Time.deltaTime);
        barrelPivot.localEulerAngles = new Vector3(0f, 0f, barrelAngle);
    }

    /// <summary>포탑 비주얼을 현 상태에 맞게 다시 그린다 (잠금/빈 슬롯/가동 중)</summary>
    private void RebuildVisual()
    {
        vRecipeId = recipeId;
        vLevel = level;
        vLocked = isLocked;

        if (visualRoot != null) Destroy(visualRoot.gameObject);
        bodySr = null;
        barrelPivot = null;
        if (!GameBalance.TurretVisuals) return;

        GameObject rootGo = new GameObject("TurretVisual");
        visualRoot = rootGo.transform;
        visualRoot.SetParent(transform, false);   // 슬롯(지붕 자리)을 따라다닌다
        visualRoot.localPosition = new Vector3(0f, -0.07f, 0f);

        // 베이스 링은 어느 상태에서나 (잠금은 어둡게)
        SpriteRenderer baseSr = SpriteBank.Attach(visualRoot, "Base", "t_base", GetBaseSprite(), Vector3.zero, SORT_BASE);
        if (isLocked)
        {
            baseSr.color = new Color(0.55f, 0.55f, 0.55f);   // 잠금 슬롯: 어두운 빈 받침 (마커 칩이 "잠금" 표시)
            return;
        }

        if (IsEmpty)
        {
            // 빈 슬롯: 구리 페그 - "여기 요리를 꽂아라" 자리 표시
            PixelPainter.Attach(visualRoot, "Pin", GetPinSprite(), Vector3.zero, SORT_DOME);
            return;
        }

        RecipeData r = Recipe;
        bool tier2 = r != null && r.tier >= 2;

        // 레벨이 오를수록 조금씩 커진다 (C 1.0 ~ S급 언저리 1.27)
        float grow = Mathf.Min(1.27f, 1f + 0.09f * (level - 1));
        visualRoot.localScale = new Vector3(grow, grow, 1f);

        // 포신 (회전 피벗) - 2티어는 2연장
        GameObject pivotGo = new GameObject("BarrelPivot");
        barrelPivot = pivotGo.transform;
        barrelPivot.SetParent(visualRoot, false);
        barrelPivot.localEulerAngles = new Vector3(0f, 0f, barrelAngle);
        SpriteBank.Attach(barrelPivot, "Barrel", tier2 ? "t_barrel2" : "t_barrel",
            tier2 ? GetBarrelSprite(true) : GetBarrelSprite(false), Vector3.zero, SORT_BARREL);

        // 구리 돔 + 속성 코어 램프 + 발광 링 (램프 색 = 공명 HUD와 같은 기준)
        Sprite domePng = SpriteBank.Get("t_dome_" + TagKey(r));
        bodySr = PixelPainter.Attach(visualRoot, "Dome", domePng != null ? domePng : PaintDome(TagColor(r)), Vector3.zero, SORT_DOME);
    }

    // ── 도트 그리기 (캔버스 48x48, 포탑 중심 = (24,26)) ──
    private static Sprite GetBaseSprite()
    {
        if (baseSprite != null) return baseSprite;
        PixelPainter p = new PixelPainter(48, 48);
        p.Shadow(15, 33, 33, 39);                                                   // 지붕 그림자
        p.Ellipse(14, 18, 34, 37, PixelPainter.BLK, PixelPainter.BLK_O);            // 베이스 링 (검정 섀시)
        p.Ellipse(14, 18, 34, 29, PixelPainter.BLK_L, PixelPainter.BLK_O);          // 링 윗면
        p.Ellipse(17, 20, 31, 26, PixelPainter.GREY, PixelPainter.CLEAR);           // 윗면 광
        for (int a = 0; a < 360; a += 45)                                           // 볼트 8개
        {
            int bx = 24 + Mathf.RoundToInt(8f * Mathf.Cos(a * Mathf.Deg2Rad));
            int by = 24 + Mathf.RoundToInt(8f * Mathf.Sin(a * Mathf.Deg2Rad) * 0.8f);
            p.Point(bx, by, PixelPainter.GOLD);                                      // 금 볼트
        }
        baseSprite = p.Bake(TURRET_PPU, 24f, 26f);
        return baseSprite;
    }

    private static Sprite GetPinSprite()
    {
        if (pinSprite != null) return pinSprite;
        PixelPainter p = new PixelPainter(48, 48);
        p.Ellipse(20, 22, 28, 30, PixelPainter.GOLD_D, PixelPainter.BLK_O);         // 페그 받침 (금)
        p.Ellipse(22, 22, 26, 26, PixelPainter.GOLD_L, PixelPainter.CLEAR);         // 페그 머리 광
        pinSprite = p.Bake(TURRET_PPU, 24f, 26f);
        return pinSprite;
    }

    /// <summary>포신: +x 방향으로 뻗음, 피벗 = 뿌리. 외곽6 / 몸4 / 북쪽 광1 + 머즐 브레이크 틱 + 총구</summary>
    private static Sprite GetBarrelSprite(bool twin)
    {
        if (twin && barrelSprite2 != null) return barrelSprite2;
        if (!twin && barrelSprite != null) return barrelSprite;

        PixelPainter p = new PixelPainter(32, 16);
        int[] rows = twin ? new int[] { 5, 10 } : new int[] { 8 };
        for (int i = 0; i < rows.Length; i++)
        {
            int y = rows[i];
            p.Line(3, y, 24, y, PixelPainter.BLK_O, 6);
            p.Line(3, y, 24, y, PixelPainter.BLK_L, 4);
            p.Line(4, y - 1, 23, y - 1, PixelPainter.GREY, 1);
            p.Line(12, y - 3, 12, y + 3, PixelPainter.GOLD, 2);                      // 금 밴드
            p.Line(19, y - 4, 19, y + 4, PixelPainter.BLK_O, 2);                     // 머즐 브레이크
            p.Ellipse(21, y - 3, 27, y + 3, PixelPainter.BLK_O, PixelPainter.CLEAR); // 총구
            p.Point(24, y, PixelPainter.BLK); p.Point(24, y - 1, PixelPainter.GREY_L);
        }
        Sprite s = p.Bake(TURRET_PPU, 3f, twin ? 7.5f : 8f);
        if (twin) barrelSprite2 = s; else barrelSprite = s;
        return s;
    }

    private static Sprite PaintDome(Color lamp)
    {
        Color32 c = lamp;
        PixelPainter p = new PixelPainter(48, 48);
        p.Ellipse(16, 18, 32, 34, PixelPainter.CLEAR, c);                            // 속성 발광 링
        p.Ellipse(17, 19, 31, 33, PixelPainter.RED, PixelPainter.RED_O);            // 빨강 장갑 돔
        p.Ellipse(19, 21, 29, 31, PixelPainter.CLEAR, PixelPainter.GOLD);           // 금 트림
        p.Ellipse(18, 19, 27, 25, PixelPainter.RED_L, PixelPainter.CLEAR);          // 돔 하이라이트
        p.Ellipse(22, 24, 26, 28, c, PixelPainter.CLEAR);                           // 코어 램프
        p.Point(20, 26, c); p.Point(28, 26, c); p.Point(24, 22, c); p.Point(24, 30, c);   // 글로우
        return p.Bake(TURRET_PPU, 24f, 26f);
    }

    /// <summary>속성 -> PNG 이름 키 (t_dome_fire 등)</summary>
    private static string TagKey(RecipeData r)
    {
        if (r == null) return "phys";
        if (r.tag == FoodTag.Fire) return "fire";
        if (r.tag == FoodTag.Elec) return "elec";
        if (r.tag == FoodTag.Ice) return "ice";
        if (r.tag == FoodTag.Poison) return "poison";
        if (r.tag == FoodTag.Def) return "def";
        return "phys";
    }

    /// <summary>속성 램프 색 (FoodTag = 공명 HUD와 같은 기준)</summary>
    private Color TagColor(RecipeData r)
    {
        if (r == null) return new Color(0.85f, 0.8f, 0.7f);
        if (r.tag == FoodTag.Fire) return new Color(1f, 0.45f, 0.15f);      // 화염 주황
        if (r.tag == FoodTag.Elec) return new Color(1f, 0.85f, 0.25f);      // 전기 노랑
        if (r.tag == FoodTag.Ice) return new Color(0.45f, 0.85f, 1f);       // 빙결 하늘
        if (r.tag == FoodTag.Poison) return new Color(0.72f, 0.42f, 0.9f);  // 독 보라
        if (r.tag == FoodTag.Def) return new Color(0.4f, 0.8f, 0.45f);      // 방어 초록
        return new Color(0.85f, 0.8f, 0.7f);                                // 물리 강철색
    }
}
