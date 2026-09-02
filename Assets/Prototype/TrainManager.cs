using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [TrainManager.cs] v3
/// 메카 티렉스 열차의 핵심 스탯을 관리합니다.
/// - v3 변경점 (구시스템 정리):
///   1) 허기/포만감 시스템 완전 제거 (감소/등급/절전모드/스탯 페널티 전부 삭제)
///      - 다른 스크립트가 아직 부를 수 있는 FeedTrain/ConsumeSatiety는
///        컴파일 호환용 빈 껍데기로만 남김 (FeedTrain은 회복으로 전환)
///   2) 구 자동공격(AutoAttack) 제거 - 공격은 TurretSlotManager가 전담
///   3) WagonSlotUI 참조 제거 (구 슬롯 UI 삭제 대비)
///   4) 증강 연동: 받는 피해 감소 (AugmentManager.DamageReductionAdd)
/// - Phase 2-3 추가: 가시철조망 도금(DEF 가산 + 피격 반격) / 넘치는 솥(증기 보호막)
/// 웨건 슬롯 API(InstallWagon 등)는 구 스크립트(CraftingUI 등)가 삭제되기 전까지
/// 컴파일 호환을 위해 남겨둠 - 실제 스탯에는 반영되지만 새 시스템에서는 안 쓴다.
/// VS 2017 (C# 7.3) 호환 버전입니다.
/// </summary>
public class TrainManager : MonoBehaviour
{
    /// <summary>싱글톤 참조 (Phase 2-2: 증강 '최후의 만찬' 등 외부에서 HP 비율 조회용)</summary>
    public static TrainManager Instance { get; private set; }

    /// <summary>현재 HP 비율 (0~1)</summary>
    public float HPRatio
    {
        get { return currentMaxHP > 0f ? Mathf.Clamp01(currentHP / currentMaxHP) : 1f; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── 구시스템 호환용 열거형 (허기 시스템 제거됨 - 삭제 예정) ──
    public enum SatietyGrade
    {
        Overcharge,
        Satisfied,
        Normal,
        Hungry
    }

    public enum WagonType
    {
        Empty,
        Attack,
        Armor,
        Support
    }

    [Header("─ 기차 기본 스탯 ─")]
    public float baseHP = 1000f;
    public float baseATK = 20f;
    public float baseDEF = 5f;
    public float baseAttackSpeed = 1.2f;

    [Header("─ 현재 스탯 (런타임) ─")]
    public float currentHP;
    public float currentMaxHP;
    public float currentATK;
    public float currentDEF;
    public float currentAttackSpeed;
    public float supportHealPerSec = 0f;

    // 패시브 요리(철판 정식/오메가 리페어)와 증강으로 늘어난 최대HP 누적분
    // RecalculateStats가 최대HP를 재계산해도 사라지지 않도록 별도 보관
    private float passiveBonusMaxHP = 0f;

    // ── Phase 2-3 증강 상태 ──
    private float steamShield = 0f;      // 넘치는 솥: 초과 회복으로 쌓인 증기 보호막
    private float nextThornsTime = 0f;   // 가시철조망 도금: 반격 쿨타임

    /// <summary>현재 증기 보호막 수치 (HUD 표시용)</summary>
    public float SteamShield { get { return steamShield; } }

    // ── 구시스템 호환용 (허기 제거됨 - 값은 항상 고정) ──
    [HideInInspector] public float satiety = 100f;                       // 항상 100 고정
    [HideInInspector] public SatietyGrade currentSatietyGrade = SatietyGrade.Normal;
    public UnityEvent<float, SatietyGrade> OnSatietyChanged = new UnityEvent<float, SatietyGrade>();

    [Header("─ 웨건 슬롯 (구시스템 - 삭제 예정) ─")]
    public WagonType[] wagonSlots = new WagonType[8];

    private const float ARMOR_BONUS_HP = 500f;
    private const float ARMOR_BONUS_DEF = 10f;
    private const float SUPPORT_HPS = 2f;

    private bool isAlive = true;

    // 연속 피격 완충 (무리 러시 즉사 방지)
    private float burstWindowEnd = 0f;
    private int burstHitCount = 0;

    private void Start()
    {
        // 밸런스 설정이 Inspector 값을 덮어쓴다 (조정은 GameBalance.cs에서)
        // 명성 상점 '강화 보일러' 보너스 가산 (영구 업그레이드)
        baseHP = GameBalance.TrainStartHP + MetaProgress.TrainHPBonus;

        currentMaxHP = baseHP;
        currentHP = currentMaxHP;
        RecalculateStats();
        Debug.Log("[TrainManager] 시작 HP " + baseHP + " (GameBalance 적용)");
    }

    private void Update()
    {
        if (!isAlive) return;

        // 서포트 초당 회복만 유지 (허기 감소/절전모드 로직 제거됨)
        if (supportHealPerSec > 0f)
            Heal(supportHealPerSec * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    // 구시스템 호환 스텁 (허기 제거)
    // ─────────────────────────────────────────────

    /// <summary>[구시스템 호환] 허기 제거됨 - 아무 것도 하지 않는다</summary>
    public void ConsumeSatiety(float amount)
    {
        // 허기 시스템 제거 - 의도적으로 비워둠
    }

    /// <summary>[구시스템 호환] 허기 제거됨 - 급양은 기차 회복으로 전환</summary>
    public void FeedTrain(float amount)
    {
        Heal(amount);
        Debug.Log("[TrainManager] (구)급양 호출 - 허기 시스템 제거로 HP " + amount + " 회복 처리");
    }

    // ─────────────────────────────────────────────
    // 스탯 재계산 (허기 배율 제거 - 항상 기본값 기준)
    // ─────────────────────────────────────────────
    public void RecalculateStats()
    {
        float bonusHP = 0f;
        float bonusDEF = 0f;
        supportHealPerSec = 0f;

        // 구 웨건 보너스 (새 시스템에서는 슬롯이 전부 Empty라 영향 없음)
        foreach (WagonType wagon in wagonSlots)
        {
            if (wagon == WagonType.Armor)
            {
                bonusHP += ARMOR_BONUS_HP;
                bonusDEF += ARMOR_BONUS_DEF;
            }
            else if (wagon == WagonType.Support)
            {
                supportHealPerSec += SUPPORT_HPS;
            }
        }

        currentMaxHP = baseHP + bonusHP + passiveBonusMaxHP;
        // Phase 2-3 증강 '가시철조망 도금': DEF 가산
        currentDEF = baseDEF + bonusDEF + AugmentManager.TrainDefAdd;
        currentHP = Mathf.Min(currentHP, currentMaxHP);

        // 허기 배율 제거: 항상 기본 공격력/공속
        currentATK = baseATK;
        currentAttackSpeed = baseAttackSpeed;
    }

    /// <summary>최대 HP 증감 (패시브 요리 / 증강 / 스피노 벌금). 음수여도 죽지는 않는다</summary>
    public void AddMaxHP(float amount)
    {
        passiveBonusMaxHP += amount;
        currentMaxHP += amount;
        currentHP += amount; // 늘어난(줄어든) 만큼 현재 HP도 조정

        // Phase 2-1: 음수 적용 안전장치 - 벌금으로 즉사하는 일은 없게
        if (currentMaxHP < 100f) currentMaxHP = 100f;
        if (currentHP < 1f) currentHP = 1f;
        if (currentHP > currentMaxHP) currentHP = currentMaxHP;

        Debug.Log("[TrainManager] 최대 HP " + (amount >= 0 ? "+" : "") + amount +
                  " (현재 " + currentHP.ToString("F0") + "/" + currentMaxHP.ToString("F0") + ")");
    }

    public void TakeDamage(float rawDamage)
    {
        if (!isAlive) return;

        // 플레이테스트 픽스 (정차 성역): 정비 턴/로비에서는 기차가 피해를 받지 않는다
        // (선로·베팅 고르는 사이 늦게 온 적/잔여 도트에게 물려 죽던 사고 방지)
        if (GameBalance.TownSanctuary && GameManager.Instance != null
            && GameManager.Instance.currentState != GameManager.GameState.Battle)
            return;

        // 피격음 (0.06초 스로틀은 SoundManager가 처리)
        SoundManager.Play("sfx_train_hit");

        // P1 게임필: 피격 셰이크 - 쿨타임 채널 방식 (매 피격마다 흔들리면 피로 - 1.5초에 1번만)
        GameFeel.Shake(GameBalance.ShakeTrainHit, "train_hit", GameBalance.ShakeTrainHitCooldown);

        // Phase 2-1: 스피노 베팅 [철벽 주방] 피격 카운트
        SpinoBet.CountTrainHit();

        // Phase 2-3 증강 '가시철조망 도금': 피격 시 근처 적 반격 (스팸 방지 쿨타임)
        if (AugmentManager.ThornsStacks > 0 && Time.time >= nextThornsTime)
        {
            nextThornsTime = Time.time + GameBalance.ThornsCooldown;
            float thornsDamage = currentDEF * GameBalance.ThornsDefRatio * AugmentManager.ThornsStacks;
            Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (!all[i].IsAlive) continue;
                if (Vector3.Distance(all[i].transform.position, transform.position) <= GameBalance.ThornsRadius)
                    all[i].TakeDamage(thornsDamage);
            }
        }

        float finalDamage = Mathf.Max(1f, rawDamage - currentDEF);

        // 피해 감소 합산: 슬롯 패시브(수정 방패 연회) + 증강(나노 수복 장갑 등)
        float totalReduction = AugmentManager.DamageReductionAdd;
        if (TurretSlotManager.Instance != null)
        {
            totalReduction += TurretSlotManager.Instance.GetDamageReduction();
            TurretSlotManager.Instance.TriggerThorns(transform.position);
        }
        // -0.85(유리 대포 등으로 받는 피해 증가) ~ 0.85(최대 85% 감소) 범위로 제한
        totalReduction = Mathf.Clamp(totalReduction, -0.85f, 0.85f);
        finalDamage *= (1f - totalReduction);

        // 연속 피격 완충: 같은 시간 창 안에서 N번째 이후 타격은 데미지 감소
        // (무리 러시가 동시에 때려도 순간 즉사하지 않게)
        if (Time.time > burstWindowEnd)
        {
            burstWindowEnd = Time.time + GameBalance.BurstHitWindow;
            burstHitCount = 0;
        }
        burstHitCount++;
        if (burstHitCount > GameBalance.BurstFreeHits)
            finalDamage *= GameBalance.BurstExtraHitMul;

        // Phase 2-3 증강 '넘치는 솥': 증기 보호막이 피해를 먼저 받는다
        if (steamShield > 0f)
        {
            float absorbed = Mathf.Min(steamShield, finalDamage);
            steamShield -= absorbed;
            finalDamage -= absorbed;
            if (finalDamage <= 0f) return;   // 전부 막았다 - HP 무손실
        }

        currentHP -= finalDamage;
        currentHP = Mathf.Clamp(currentHP, 0f, currentMaxHP);

        float shakeIntensity = Mathf.Clamp01(finalDamage / 50f);
        ChefController chef = FindFirstObjectByType<ChefController>();
        chef?.OnTrainHit(shakeIntensity);

        if (currentHP <= 0f) OnTrainDestroyed();
    }

    public void Heal(float amount)
    {
        float before = currentHP;
        currentHP = Mathf.Min(currentHP + amount, currentMaxHP);

        // Phase 2-3 증강 '넘치는 솥': 최대 HP를 넘는 회복분은 증기 보호막으로
        if (AugmentManager.OverflowShield && amount > 0f)
        {
            float overflow = amount - (currentHP - before);
            if (overflow > 0f)
                steamShield = Mathf.Min(steamShield + overflow,
                    currentMaxHP * GameBalance.OverflowShieldCap);
        }
    }

    // ─────────────────────────────────────────────
    // 구 웨건 API (CraftingUI 등 구 스크립트 삭제 전까지 호환 유지)
    // ─────────────────────────────────────────────
    public bool InstallWagon(int slotIndex, WagonType type)
    {
        if (slotIndex < 0 || slotIndex >= wagonSlots.Length)
        {
            Debug.LogWarning("[TrainManager] 유효하지 않은 슬롯 인덱스: " + slotIndex);
            return false;
        }

        wagonSlots[slotIndex] = type;
        RecalculateStats();
        return true;
    }

    public bool HasWagonType(WagonType type)
    {
        foreach (WagonType slot in wagonSlots)
            if (slot == type) return true;
        return false;
    }

    public int GetEmptySlotIndex()
    {
        for (int i = 0; i < wagonSlots.Length; i++)
            if (wagonSlots[i] == WagonType.Empty) return i;
        return -1;
    }

    private void OnTrainDestroyed()
    {
        isAlive = false;
        Debug.Log("[TrainManager] 기차가 격파되었습니다!");
        GameManager.Instance?.OnTrainDestroyed();

        // '아홉 개의 목숨' 증강으로 부활했다면 GameManager가 Heal을 호출해 HP가 차 있다
        if (currentHP > 0f)
        {
            isAlive = true;
            Debug.Log("[TrainManager] 부활 - 기차 재가동!");
        }
    }

    // ── 구시스템 호환 프로퍼티 (허기 제거 - 항상 고정값) ──
    public bool IsPowerSaveMode => false;   // 절전모드 제거 - 포탑은 항상 가동
    public bool IsBerserkMode => false;     // 폭주모드(포만감 기반) 제거
    public bool IsAlive => isAlive;
}
