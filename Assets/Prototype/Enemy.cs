using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [Enemy.cs] v3
/// 모든 적 유닛의 기본 동작 + 전투 스탯(DEF/RES) + 상태이상(도트/방깎/마깎)
/// - v3 변경점: 행동 패턴 시스템 (이름 기반 자동 배정 - 프리팹 설정 불필요)
///   1) 무리 사냥꾼(랩터): 주변 랩터가 많을수록 이동 속도 증가
///   2) 급강하(비행 유닛): 기차를 향해 돌진 -> 스치듯 타격 -> 관성으로 지나감 -> 선회 재접근
///   3) 원거리 사수(캑터스): 멀리 멈춰서 투척 공격
///   4) 서포터(자석 파라사우): 주변 아군 이동/공격 버프 오라 (우선 처치 대상)
///   5) 힐러(네크로 스피노): 주변 아군 지속 회복 오라
///   6) 자폭병(과부하 플라이): 기차에 닿으면 폭발 후 소멸
///   + 재료 드랍에 증강 MaterialDropMul 반영
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class Enemy : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // 적 유닛 데이터 구조체
    // ─────────────────────────────────────────────
    [System.Serializable]
    public struct EnemyData
    {
        public string enemyName;
        public float baseHP;
        public float baseATK;
        public float baseSPD;
        public string dropMaterialName;
        public int goldReward;
        public int xpReward;
        public string targetPriority;
        public string specialAbility;
    }

    // ═══════════════════════════════════════════
    // [Phase 1] Wave 1~10: 구리 사막의 약탈자
    // ═══════════════════════════════════════════

    public static EnemyData SteamRaptor = new EnemyData
    {
        enemyName = "스팀 랩터",
        baseHP = 30f,
        baseATK = 10f,
        baseSPD = 5.0f,
        dropMaterialName = "질긴 랩터 고기",
        goldReward = 10,
        xpReward = 5,
        targetPriority = "주방 칸",
        specialAbility = "무리 사냥 (주변 랩터당 가속)"
    };

    public static EnemyData SpringAnkylo = new EnemyData
    {
        enemyName = "태엽 아르마딜로",
        baseHP = 120f,
        baseATK = 25f,
        baseSPD = 1.5f,
        dropMaterialName = "단단한 안킬로 등심",
        goldReward = 30,
        xpReward = 15,
        targetPriority = "엔진(헤드)",
        specialAbility = "돌진 공격 시 DEF +20"
    };

    public static EnemyData OilCactus = new EnemyData
    {
        enemyName = "오일 캑터스",
        baseHP = 50f,
        baseATK = 15f,
        baseSPD = 2.5f,
        dropMaterialName = "식물성 오일",
        goldReward = 20,
        xpReward = 10,
        targetPriority = "랜덤 슬롯",
        specialAbility = "원거리 기름 투척"
    };

    public static EnemyData DesertScorpion = new EnemyData
    {
        enemyName = "사막 전갈",
        baseHP = 45f,
        baseATK = 18f,
        baseSPD = 3.0f,
        dropMaterialName = "전갈 독침",
        goldReward = 25,
        xpReward = 12,
        targetPriority = "주방 칸",
        specialAbility = "독 공격 (조리 도구 내구도 -10)"
    };

    public static EnemyData CopperTortoise = new EnemyData
    {
        enemyName = "구리 거북",
        baseHP = 200f,
        baseATK = 20f,
        baseSPD = 0.8f,
        dropMaterialName = "구리 등딱지",
        goldReward = 40,
        xpReward = 20,
        targetPriority = "엔진(헤드)",
        specialAbility = "고방어력 (관통 속성 필요)"
    };

    // ═══════════════════════════════════════════
    // [Phase 2] Wave 11~25: 테슬라 협곡의 전술 유닛
    // ═══════════════════════════════════════════

    public static EnemyData BoltTeranodon = new EnemyData
    {
        enemyName = "볼트 테라노돈",
        baseHP = 45f,
        baseATK = 20f,
        baseSPD = 4.0f,
        dropMaterialName = "전기 뱀장어 꼬리",
        goldReward = 25,
        xpReward = 12,
        targetPriority = "포탑 슬롯",
        specialAbility = "급강하 폭격"
    };

    public static EnemyData PoisonPtera = new EnemyData
    {
        enemyName = "독침 프테라",
        baseHP = 60f,
        baseATK = 15f,
        baseSPD = 3.5f,
        dropMaterialName = "프테라 독샘",
        goldReward = 30,
        xpReward = 15,
        targetPriority = "셰프",
        specialAbility = "급강하 + 셰프 저격 (조리 속도 -50%, 10초)"
    };

    public static EnemyData MagnetParasaur = new EnemyData
    {
        enemyName = "자석 파라사우",
        baseHP = 250f,
        baseATK = 40f,
        baseSPD = 1.0f,
        dropMaterialName = "고농축 자기장 젤리",
        goldReward = 50,
        xpReward = 25,
        targetPriority = "기차 전체",
        specialAbility = "자기장 오라 (주변 아군 이동/공격 강화)"
    };

    public static EnemyData OverloadFly = new EnemyData
    {
        enemyName = "과부하 플라이",
        baseHP = 10f,
        baseATK = 80f,
        baseSPD = 6.0f,
        dropMaterialName = "전기 자극 가루",
        goldReward = 15,
        xpReward = 8,
        targetPriority = "랜덤 슬롯",
        specialAbility = "자폭 공격"
    };

    public static EnemyData FlamePterosaur = new EnemyData
    {
        enemyName = "화염 익룡",
        baseHP = 80f,
        baseATK = 35f,
        baseSPD = 3.0f,
        dropMaterialName = "화염 깃털",
        goldReward = 35,
        xpReward = 18,
        targetPriority = "주방 칸",
        specialAbility = "급강하 화염 폭격"
    };

    public static EnemyData SteelRaptor = new EnemyData
    {
        enemyName = "강철 랩터",
        baseHP = 150f,
        baseATK = 45f,
        baseSPD = 3.5f,
        dropMaterialName = "강철 비늘",
        goldReward = 45,
        xpReward = 22,
        targetPriority = "포탑 슬롯",
        specialAbility = "물리 면역 (속성 공격 필요)"
    };

    // ═══════════════════════════════════════════
    // [Phase 3] Wave 26~40: 코발트 광산의 정예 군단
    // ═══════════════════════════════════════════

    public static EnemyData IceMosa = new EnemyData
    {
        enemyName = "아이스 모사사우르스",
        baseHP = 400f,
        baseATK = 50f,
        baseSPD = 2.0f,
        dropMaterialName = "마지막 바다의 얼음꽃",
        goldReward = 80,
        xpReward = 40,
        targetPriority = "바퀴/엔진",
        specialAbility = "기차 결빙(2초)"
    };

    public static EnemyData CrystalPachy = new EnemyData
    {
        enemyName = "크리스탈 파키케팔로",
        baseHP = 600f,
        baseATK = 70f,
        baseSPD = 1.5f,
        dropMaterialName = "코발트 결정 조각",
        goldReward = 100,
        xpReward = 50,
        targetPriority = "엔진(헤드)",
        specialAbility = "데미지 10% 반사"
    };

    public static EnemyData MagmaCarno = new EnemyData
    {
        enemyName = "마그마 카르노타우르스",
        baseHP = 800f,
        baseATK = 120f,
        baseSPD = 2.5f,
        dropMaterialName = "화염 꽃",
        goldReward = 120,
        xpReward = 60,
        targetPriority = "기차 전체",
        specialAbility = "화염 방사(도트 데미지)"
    };

    public static EnemyData FrostMammoth = new EnemyData
    {
        enemyName = "서리 맘모스",
        baseHP = 2000f,
        baseATK = 150f,
        baseSPD = 0.8f,
        dropMaterialName = "얼어붙은 등심",
        goldReward = 200,
        xpReward = 100,
        targetPriority = "정면 충돌",
        specialAbility = "기차 전진 완전 정지"
    };

    public static EnemyData NecroSpino = new EnemyData
    {
        enemyName = "네크로 스피노사우르스",
        baseHP = 1200f,
        baseATK = 90f,
        baseSPD = 1.8f,
        dropMaterialName = "부활의 뼛가루",
        goldReward = 180,
        xpReward = 90,
        targetPriority = "기차 전체",
        specialAbility = "재생 오라 (주변 아군 지속 회복)"
    };

    // ═══════════════════════════════════════════
    // 행동 패턴 (v3)
    // ═══════════════════════════════════════════
    public enum BehaviorPattern
    {
        Chase,      // 기본: 접근해서 근접 공격
        Pack,       // 무리 사냥꾼: 주변 동족 수에 비례해 가속
        Swoop,      // 급강하: 스치듯 타격 후 지나가서 선회
        Ranged,     // 원거리: 멀리 멈춰서 공격
        Support,    // 서포터: 아군 버프 오라 (본인도 Chase 이동)
        Healer,     // 힐러: 아군 회복 오라 (본인도 Chase 이동)
        Suicide     // 자폭병: 닿으면 폭발 후 소멸
    }

    [Header("─ 행동 패턴 (자동 배정 - 수동 설정 시 존중) ─")]
    public BehaviorPattern behavior = BehaviorPattern.Chase;
    private bool behaviorAssigned = false;

    // 급강하 상태
    private bool swoopPassing = false;
    private Vector3 swoopDir;
    private float swoopTimer = 0f;

    // 오라 틱 타이머 (서포터/힐러)
    private float auraTimer = 0f;
    private const float AURA_RADIUS = 5f;

    // 서포터에게 버프 받은 상태
    private float buffedUntil = -1f;
    private bool IsBuffed { get { return Time.time < buffedUntil; } }

    // v3.6: 무리 카운트 캐시 (성능 - 0.5초마다 갱신)
    private int packCountCache = 0;
    private float packCacheUntil = 0f;

    // 사거리 진입 시 첫 공격 랜덤 딜레이 (무리 도착 동시타격 방지)
    private bool wasInRange = false;

    // ─────────────────────────────────────────────
    // v3.5: 도발(미끼) - 보스 '미끼 화덕' 기믹
    // 도발 중에는 미끼를 향해 이동하고, 미끼를 물어뜯는 동안 기차에 피해를 주지 않는다
    // ─────────────────────────────────────────────
    private Transform tauntTarget = null;
    private float tauntUntil = 0f;

    /// <summary>도발 중인가?</summary>
    protected bool IsTaunted { get { return Time.time < tauntUntil && tauntTarget != null; } }

    /// <summary>현재 추적 대상 (도발 중이면 미끼, 아니면 기차)</summary>
    protected Transform CurrentTarget { get { return IsTaunted ? tauntTarget : trainTarget; } }

    /// <summary>미끼로 유인 (BaitStationUI가 호출)</summary>
    public void Taunt(Transform bait, float seconds)
    {
        if (!isAlive) return;
        tauntTarget = bait;
        tauntUntil = Time.time + seconds;
    }

    // ═══════════════════════════════════════════
    // Inspector 설정
    // ═══════════════════════════════════════════
    [Header("─ 적 유닛 설정 ─")]
    public EnemyData data;

    // v3.2: 구 드롭 시스템(materialItemPrefab/dropChance) 삭제
    // 재료 지급은 PickupFX 흡수 연출 하나로 통일 (Die 참고)

    [Header("─ 런타임 스탯 ─")]
    public float currentHP;
    public float scaledMaxHP;      // v3: 힐러 회복 상한용
    public float scaledATK;
    public float scaledSPD;

    [Header("─ 전투 스탯 (v3 기획: 물리/마법 카운터) ─")]
    public float defense = 0f;      // 물리 방어력
    public float resistance = 0f;   // 마법 저항

    [Header("─ 이동 / 공격 ─")]
    public Transform trainTarget;
    public float attackRange = 2f;
    public float attackCooldown = 2f;

    protected bool isAlive = true;
    protected float attackTimer = 0f;
    protected TrainManager trainManager;

    // ─────────────────────────────────────────────
    // 상태이상 (도트/방깎/마깎)
    // ─────────────────────────────────────────────
    private class DotEffect
    {
        public string type;      // "burn" / "poison"
        public float remainSec;
        public float dps;
        public float tickAcc;    // 1 데미지 누적기
    }

    private List<DotEffect> dots = new List<DotEffect>();
    private int shredDefStack = 0;   // 방깎 스택 (스택당 DEF -15)
    private int shredResStack = 0;   // 마깎 스택 (스택당 RES -15)
    private float shredTimer = 0f;   // 깎임 남은 시간 (갱신형)

    private void Start()
    {
        trainManager = FindFirstObjectByType<TrainManager>();
        GameObject trainObj = GameObject.FindGameObjectWithTag("Train");
        if (trainObj != null) trainTarget = trainObj.transform;

        if (currentHP <= 0f) currentHP = data.baseHP;
        if (scaledMaxHP <= 0f) scaledMaxHP = currentHP;
        if (scaledATK <= 0f) scaledATK = data.baseATK;
        if (scaledSPD <= 0f) scaledSPD = data.baseSPD;

        // DEF/RES + 행동 패턴 자동 배정 (이름 기반 - 프리팹 설정 불필요)
        AssignCombatStats();
        AssignBehavior();
    }

    /// <summary>
    /// 적 이름으로 DEF/RES 자동 배정
    /// 장갑형(안킬로/거북/강철)은 DEF 높음 -> 마법으로 잡아야 함
    /// 비행/전기형(프테라/테라노돈)은 RES 높음 -> 물리로 잡아야 함
    /// </summary>
    private void AssignCombatStats()
    {
        if (defense > 0f || resistance > 0f) return; // Inspector 수동 설정 존중

        string n = data.enemyName;
        if (n.Contains("아르마딜로") || n.Contains("안킬로")) { defense = 35f; resistance = 5f; }
        else if (n.Contains("거북")) { defense = 45f; resistance = 5f; }
        else if (n.Contains("강철")) { defense = 50f; resistance = 20f; }
        else if (n.Contains("테라노돈") || n.Contains("프테라") || n.Contains("익룡")) { defense = 0f; resistance = 30f; }
        else if (n.Contains("전갈")) { defense = 12f; resistance = 12f; }
        else if (n.Contains("파라사우")) { defense = 20f; resistance = 20f; }
        else if (n.Contains("모사")) { defense = 40f; resistance = 40f; }
        else if (n.Contains("파키")) { defense = 45f; resistance = 30f; }
        else if (n.Contains("카르노")) { defense = 15f; resistance = 30f; }
        else if (n.Contains("맘모스")) { defense = 50f; resistance = 35f; }
        else if (n.Contains("스피노")) { defense = 30f; resistance = 30f; }
        // 랩터/캑터스/플라이 등은 0/0 (아무거나 잘 박힘)
    }

    /// <summary>적 이름으로 행동 패턴 자동 배정 (Inspector 수동 설정 존중)</summary>
    private void AssignBehavior()
    {
        if (behaviorAssigned || behavior != BehaviorPattern.Chase) { behaviorAssigned = true; return; }
        behaviorAssigned = true;

        string n = data.enemyName;
        if (n.Contains("스팀 랩터")) behavior = BehaviorPattern.Pack;
        else if (n.Contains("테라노돈") || n.Contains("프테라") || n.Contains("익룡")) behavior = BehaviorPattern.Swoop;
        else if (n.Contains("캑터스")) { behavior = BehaviorPattern.Ranged; attackRange = 6f; }
        else if (n.Contains("파라사우")) behavior = BehaviorPattern.Support;
        else if (n.Contains("네크로")) behavior = BehaviorPattern.Healer;
        else if (n.Contains("플라이")) behavior = BehaviorPattern.Suicide;
    }

    public void InitializeWithWaveScaling(int waveNumber, int playerLevel, float difficultyL = 2.0f)
    {
        float multiplier = 1f + (waveNumber * 0.15f) / difficultyL;
        // 전역 밸런스 배율 (GameBalance에서 조정)
        currentHP = data.baseHP * multiplier * GameBalance.EnemyHPMul;
        scaledMaxHP = currentHP;
        scaledATK = data.baseATK * multiplier * GameBalance.EnemyATKMul;
        scaledSPD = data.baseSPD;
    }

    private void Update()
    {
        if (!isAlive) return;

        // 상태이상 갱신
        TickStatusEffects();

        // 서포터/힐러 오라 틱
        if (behavior == BehaviorPattern.Support || behavior == BehaviorPattern.Healer)
            TickAura();

        if (trainTarget == null) return;
        attackTimer += Time.deltaTime;

        // 급강하 패턴은 전용 이동 처리
        if (behavior == BehaviorPattern.Swoop)
        {
            UpdateSwoop();
            return;
        }

        // v3.5: 도발 중이면 미끼를 추적
        float distance = Vector3.Distance(transform.position, CurrentTarget.position);
        bool inRange = (distance <= attackRange);

        // 사거리에 처음 진입하는 순간, 첫 공격을 개체별로 랜덤하게 늦춘다
        // (무리 러시가 도착하자마자 전원이 같은 프레임에 때리는 것 방지)
        if (inRange && !wasInRange)
            attackTimer = Mathf.Min(attackTimer, attackCooldown - Random.Range(0.3f, 1.2f));
        wasInRange = inRange;

        if (!inRange)
            MoveTowardsTrain();
        else if (attackTimer >= attackCooldown)
        {
            // 도발 중이면 미끼를 물어뜯는다 (기차 무피해)
            if (!IsTaunted) AttackTrain();
            attackTimer = 0f;
        }
    }

    // ─────────────────────────────────────────────
    // 행동 패턴 처리 (v3)
    // ─────────────────────────────────────────────

    /// <summary>급강하: 접근 -> 스치듯 타격 -> 관성으로 지나감 -> 선회 재접근</summary>
    private void UpdateSwoop()
    {
        if (swoopPassing)
        {
            // 타격 후 관성 비행 (기차를 지나쳐 날아간다)
            transform.position += swoopDir * scaledSPD * SpeedMul() * 1.2f * Time.deltaTime;
            swoopTimer -= Time.deltaTime;
            if (swoopTimer <= 0f)
                swoopPassing = false;   // 선회 완료 - 재접근
            return;
        }

        // 접근 (일반보다 조금 빠르게 돌진) - 도발 중이면 미끼로
        Vector3 direction = (CurrentTarget.position - transform.position).normalized;
        transform.position += direction * scaledSPD * SpeedMul() * 1.15f * Time.deltaTime;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 스치듯 타격
        float distance = Vector3.Distance(transform.position, CurrentTarget.position);
        if (distance <= attackRange && attackTimer >= attackCooldown)
        {
            if (!IsTaunted) AttackTrain();
            attackTimer = 0f;

            // 진행 방향 그대로 지나쳐 날아감
            swoopPassing = true;
            swoopDir = direction;
            swoopTimer = 2.2f;
        }
    }

    /// <summary>서포터/힐러 오라: 1초마다 주변 아군에게 효과</summary>
    private void TickAura()
    {
        auraTimer -= Time.deltaTime;
        if (auraTimer > 0f) return;
        auraTimer = 1f;

        Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == this || !all[i].IsAlive) continue;
            if (Vector3.Distance(transform.position, all[i].transform.position) > AURA_RADIUS) continue;

            if (behavior == BehaviorPattern.Support)
            {
                // 자기장 오라: 1.3초간 이동 +30% / 공격 +25% (지속 갱신)
                all[i].ReceiveSupportBuff(1.3f);
            }
            else
            {
                // 재생 오라: 초당 회복 (최대 HP까지)
                all[i].ReceiveHeal(6f);
            }
        }
    }

    /// <summary>서포터 버프 수신 (이동/공격 강화)</summary>
    public void ReceiveSupportBuff(float duration)
    {
        if (!isAlive) return;
        buffedUntil = Time.time + duration;
    }

    /// <summary>힐러 회복 수신</summary>
    public void ReceiveHeal(float amount)
    {
        if (!isAlive) return;
        currentHP = Mathf.Min(currentHP + amount, scaledMaxHP);
    }

    /// <summary>버프 반영 이동 배율</summary>
    private float SpeedMul()
    {
        float mul = IsBuffed ? 1.3f : 1f;

        // 무리 사냥꾼: 주변 5칸 내 같은 랩터 1마리당 +8% (최대 +40%)
        // v3.6 (기술감사): 매 프레임 전체 스캔이 O(n^2)라서 0.5초 캐시로 완화
        if (behavior == BehaviorPattern.Pack)
        {
            if (Time.time >= packCacheUntil)
            {
                packCacheUntil = Time.time + 0.5f;
                packCountCache = 0;
                Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == this || !all[i].IsAlive) continue;
                    if (all[i].behavior != BehaviorPattern.Pack) continue;
                    if (Vector3.Distance(transform.position, all[i].transform.position) <= 5f)
                    {
                        packCountCache++;
                        if (packCountCache >= 5) break;
                    }
                }
            }
            mul *= 1f + 0.08f * packCountCache;
        }

        return mul;
    }

    // ─────────────────────────────────────────────
    // 상태이상 틱
    // ─────────────────────────────────────────────

    /// <summary>도트 데미지 + 방깎/마깎 타이머 처리 (v3.3: BossEnemy도 호출하도록 protected)</summary>
    protected void TickStatusEffects()
    {
        // 도트
        for (int i = dots.Count - 1; i >= 0; i--)
        {
            DotEffect d = dots[i];
            d.remainSec -= Time.deltaTime;
            d.tickAcc += d.dps * Time.deltaTime;

            // 1 이상 누적되면 정수 데미지로 적용 (팝업 스팸 방지)
            if (d.tickAcc >= 1f)
            {
                int dmg = Mathf.FloorToInt(d.tickAcc);
                d.tickAcc -= dmg;
                ApplyRawDamage(dmg, d.type == "burn");
            }

            if (d.remainSec <= 0f) dots.RemoveAt(i);
        }

        // 깎임 지속시간
        if (shredTimer > 0f)
        {
            shredTimer -= Time.deltaTime;
            if (shredTimer <= 0f)
            {
                shredDefStack = 0;
                shredResStack = 0;
            }
        }
    }

    protected virtual void MoveTowardsTrain()
    {
        // v3.5: 도발 중이면 미끼를 향해 이동
        Vector3 direction = (CurrentTarget.position - transform.position).normalized;
        transform.position += direction * scaledSPD * SpeedMul() * Time.deltaTime;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    // ─────────────────────────────────────────────
    // P1: 아이스 모사 슬롯 빙결 (전체 모사 공유 쿨타임)
    // ─────────────────────────────────────────────
    private static float nextFreezeAllowed = 0f;

    /// <summary>가동 중인 슬롯 하나를 무작위로 빙결시킨다 (대상 없으면 아무 일 없음)</summary>
    private void TryFreezeRandomSlot()
    {
        TurretSlotManager mgr = TurretSlotManager.Instance;
        if (mgr == null) return;

        // 빙결 가능한 슬롯 수집: 해금 + 포탑 있음 + 아직 마비 아님
        int count = 0;
        TurretSlot[] candidates = new TurretSlot[8];
        for (int i = 0; i < 8; i++)
        {
            TurretSlot s = mgr.slots[i];
            if (s != null && !s.isLocked && !s.IsEmpty && !s.IsStunned)
                candidates[count++] = s;
        }
        if (count == 0) return;

        TurretSlot target = candidates[Random.Range(0, count)];
        target.StunSlot(GameBalance.FreezeSlotSec, "빙결");
        nextFreezeAllowed = Time.time + GameBalance.FreezeGlobalCooldown;

        UIManager.Instance?.ShowDanger("[아이스 모사] 냉기가 포탑을 덮쳤다! 빙결 - 클릭으로 해빙");
        Debug.Log("[Enemy] 아이스 모사 빙결: " + (target.Recipe != null ? target.Recipe.displayName : "?"));
    }

    protected virtual void AttackTrain()
    {
        float damage = scaledATK * (IsBuffed ? 1.25f : 1f);
        trainManager?.TakeDamage(damage);

        if (data.enemyName == "독침 프테라")
        {
            ChefController chef = FindFirstObjectByType<ChefController>();
            if (chef != null)
            {
                chef.ApplyCookingSpeedDebuff(10f);
                Debug.Log("[독침 프테라] 셰프 저격! 조리 속도 -50% 10초");
            }
        }

        // 사막 전갈: 공격할 때마다 조리 도구를 부식시킨다 (정비소 수요 창출)
        // Phase 2-3 아이템 '휴대용 숫돌': 부식도 마모이므로 같이 감소
        if (data.enemyName.Contains("전갈"))
        {
            ChefController chef = FindFirstObjectByType<ChefController>();
            if (chef != null)
            {
                float corrode = 3f * ItemManager.ToolWearMul;
                chef.knifeSharpness = Mathf.Max(0f, chef.knifeSharpness - corrode);
                chef.panCondition = Mathf.Max(0f, chef.panCondition - corrode);
                Debug.Log("[사막 전갈] 독 공격 - 조리 도구 부식! (칼/팬 -" + corrode + ")");
            }
        }

        // P1 (감사 2-C): 오일 캑터스 - 죽은 플레이버의 실기믹화
        // 투척이 명중하면 주방에 기름이 튀어 잠시 조리대가 미끄러워진다 (굽기 요동/끓이기 하강 가속)
        if (data.enemyName.Contains("캑터스"))
            CookingMinigame.ApplyOilSlip(GameBalance.OilSlipDuration);

        // P1 (감사 2-C): 아이스 모사 - 죽은 플레이버("바퀴 결빙")의 실기믹화
        // 명중 시 확률로 가동 중인 포탑 슬롯 1기를 빙결 (낙뢰 마비와 같은 기믹 - 클릭으로 해빙)
        // 전체 모사가 쿨타임을 공유해서 다중 모사 스턴락은 발생하지 않는다
        if (data.enemyName.Contains("모사")
            && Time.time >= nextFreezeAllowed
            && Random.value < GameBalance.FreezeChance)
        {
            TryFreezeRandomSlot();
        }

        // 자폭병: 일격 후 즉시 소멸 (보상은 정상 지급)
        if (behavior == BehaviorPattern.Suicide)
        {
            Debug.Log("[Enemy] " + data.enemyName + " 자폭!");
            Die();
        }
    }

    // ─────────────────────────────────────────────
    // 데미지 처리
    // ─────────────────────────────────────────────

    /// <summary>기존 호환용 - 타입 없는 데미지 (방어 무시 순수 데미지)</summary>
    public void TakeDamage(float damage)
    {
        ApplyRawDamage(damage, false);
    }

    /// <summary>
    /// v3 전투 공식 - 물리/마법 타입 데미지
    /// 최종딜 = 기본딜 x 50/(50+스탯), 방깎/마깎으로 스탯 감소 가능
    /// </summary>
    public void TakeDamage(float damage, DamageType dtype)
    {
        float stat;
        if (dtype == DamageType.Magic)
            stat = Mathf.Max(0f, resistance - shredResStack * 15f);
        else
            stat = Mathf.Max(0f, defense - shredDefStack * 15f);

        float finalDamage = damage * 50f / (50f + stat);

        // v3.4: 파생 클래스 데미지 훅 (보스 '빙하 갑주' 등 - 도트는 이 훅을 안 거친다)
        finalDamage = ModifyIncomingDamage(finalDamage, dtype);

        ApplyRawDamage(finalDamage, dtype == DamageType.Magic);
    }

    /// <summary>
    /// v3.4: 최종 데미지 보정 훅. 기본은 그대로 통과.
    /// BossEnemy가 빙하 갑주(피해 90% 감소) 등에 사용한다.
    /// 주의: 도트(화상/독)는 ApplyRawDamage 직행이라 이 훅을 안 거친다 - 의도된 동작
    /// (화염 도트가 갑주를 무시하고 태우는 파훼 수단이 되기 위함)
    /// </summary>
    protected virtual float ModifyIncomingDamage(float damage, DamageType dtype)
    {
        return damage;
    }

    /// <summary>실제 HP 차감 + 팝업 (계산 완료된 데미지)</summary>
    private void ApplyRawDamage(float damage, bool isMagic)
    {
        if (!isAlive) return;
        currentHP -= damage;

        bool isCritical = damage >= scaledATK * 2f;
        DamagePopup.Create(transform.position, damage, isCritical);

        if (currentHP <= 0f) Die();
    }

    // ─────────────────────────────────────────────
    // 상태이상 부여 API (TurretAttackExecutor에서 호출)
    // ─────────────────────────────────────────────

    // v3.4: 누적 화상 스택 카운터 (보스 '빙하 갑주' 파괴 판정용)
    public int TotalBurnApplied { get; private set; }

    /// <summary>화상 도트: 3초간 초당 4 x 스택 (마법 도트)</summary>
    public void ApplyBurn(int stack, float multiplier)
    {
        if (!isAlive) return;
        DotEffect d = new DotEffect();
        d.type = "burn";
        d.remainSec = 3f;
        d.dps = 4f * stack * multiplier;
        dots.Add(d);
        TotalBurnApplied += Mathf.Max(1, stack);
    }

    /// <summary>독 도트: 5초간 초당 3 x 스택 (중첩 가능)</summary>
    public void ApplyPoison(int stack, float multiplier)
    {
        if (!isAlive) return;
        DotEffect d = new DotEffect();
        d.type = "poison";
        d.remainSec = 5f;
        d.dps = 3f * stack * multiplier;
        dots.Add(d);
    }

    /// <summary>방깎: 스택당 DEF -15, 5초 (최대 3스택, 시간 갱신)</summary>
    public void ApplyShredDef(int stack)
    {
        if (!isAlive) return;
        shredDefStack = Mathf.Min(3, shredDefStack + stack);
        shredTimer = 5f;
    }

    /// <summary>마깎: 스택당 RES -15, 5초 (최대 3스택, 시간 갱신)</summary>
    public void ApplyShredRes(int stack)
    {
        if (!isAlive) return;
        shredResStack = Mathf.Min(3, shredResStack + stack);
        shredTimer = 5f;
    }

    /// <summary>도트 걸려있는지 (UI 표시용)</summary>
    public bool HasDot(string type)
    {
        for (int i = 0; i < dots.Count; i++)
            if (dots[i].type == type) return true;
        return false;
    }

    public int ShredDefStack { get { return shredDefStack; } }
    public int ShredResStack { get { return shredResStack; } }

    // ─────────────────────────────────────────────
    // 사망 처리
    // ─────────────────────────────────────────────
    protected virtual void Die()
    {
        isAlive = false;
        SoundManager.Play("sfx_enemy_die");

        // P1 게임필: 처치 팝 (드랍 재료 색과 통일 - 조각 흡수 연출과 이어져 보이게)
        GameFeel.DeathPop(transform.position, PickupFX.ColorOf(GetDropMaterialType()));

        // 밸런스 1차 (B-3 레버 리턴): 전속 주행 중 처치 골드 +25%
        // - 스폰 압박/판정 페널티를 감수한 값. 회전율이 곧 매출이다
        int gold = data.goldReward;
        if (EngineCab.FullSteam)
            gold = Mathf.RoundToInt(gold * GameBalance.LeverGoldMul);
        GameManager.Instance?.AddGold(gold);

        // Phase 2-3: 아주 낮은 확률로 아이템(유물) 드랍 - 보스는 확률 대폭 상향
        // B-2: 즉시 지급 대신 갑판 상자로 떨어진다 (죽은 자리 방향의 갑판 - 밟아서 회수)
        float itemChance = (this is BossEnemy)
            ? GameBalance.ItemDropChanceBoss : GameBalance.ItemDropChance;
        if (Random.value < itemChance)
            DeckLoot.SpawnItemCrate(transform.position.x, data.enemyName + " 잔해에서 발견");
        // (감사 3-B: XP 시스템 절단 - AddXP 호출 제거)
        // v3 재료 시스템 드롭 (증강 '자석 흡입기 개조' 반영)
        // v3.1: 즉시 지급 대신 흡수 연출 - 조각이 기차에 도착하면 지급 (PickupFX)
        if (MaterialInventory.Instance != null)
        {
            MaterialType matType = GetDropMaterialType();
            int amount = 1;
            float extra = AugmentManager.MaterialDropMul - 1f;
            while (extra >= 1f) { amount++; extra -= 1f; }
            if (extra > 0f && Random.value < extra) amount++;
            PickupFX.Spawn(transform.position, matType, amount);
        }

        StartCoroutine(DestroyAfterDelay(0.5f));
    }

    /// <summary>기존 dropMaterialName 문자열 -> 새 재료 6종 자동 매핑</summary>
    protected MaterialType GetDropMaterialType()
    {
        string n = data.dropMaterialName;
        if (string.IsNullOrEmpty(n)) return MaterialType.Meat;

        if (n.Contains("랩터 고기")) return MaterialType.Meat;
        if (n.Contains("등심") || n.Contains("등딱지") || n.Contains("비늘") || n.Contains("결정"))
            return MaterialType.Armor;
        if (n.Contains("전기") || n.Contains("자기장")) return MaterialType.Elec;
        if (n.Contains("화염") || n.Contains("오일")) return MaterialType.Fire;
        if (n.Contains("얼음") || n.Contains("서리")) return MaterialType.Ice;
        if (n.Contains("독") || n.Contains("뼛가루")) return MaterialType.Poison;

        return MaterialType.Meat;
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    public bool IsAlive => isAlive;
    public float HPRatio => currentHP / Mathf.Max(1f, scaledMaxHP);

    // ─────────────────────────────────────────────
    // 디버프 메서드 (포탑/투척에서 호출)
    // ─────────────────────────────────────────────

    public void ApplySpeedDebuff(float speedMultiplier, float duration)
    {
        if (!isAlive) return;
        StartCoroutine(SpeedDebuffCoroutine(speedMultiplier, duration));
    }

    private IEnumerator SpeedDebuffCoroutine(float multiplier, float duration)
    {
        float originalSpeed = scaledSPD;
        scaledSPD = originalSpeed * multiplier;
        yield return new WaitForSeconds(duration);
        if (isAlive) scaledSPD = originalSpeed;
    }

    // v3.4: 마지막 스턴 명중 시각 (보스 '사냥 호령' 파훼 판정용)
    public float LastStunTime { get; private set; }

    public void ApplyStun(float duration)
    {
        if (!isAlive) return;
        LastStunTime = Time.time;
        StartCoroutine(StunCoroutine(duration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        float originalSpeed = scaledSPD;
        scaledSPD = 0f;
        yield return new WaitForSeconds(duration);
        if (isAlive) scaledSPD = originalSpeed;
    }

    public void ApplyKnockback(Vector3 explosionPos, float force)
    {
        if (!isAlive) return;
        Vector3 dir = (transform.position - explosionPos).normalized;
        transform.position += dir * force * 0.3f;
    }
}
