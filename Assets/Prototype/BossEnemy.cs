using System.Collections;
using UnityEngine;

/// <summary>
/// [BossEnemy.cs] v6 - 보스 패턴 C단계 1차 (보스패턴설계 문서)
/// - v6 변경점:
///   1) 미끼 도발 대응: 도발 중엔 미끼를 쫓아가고 물어뜯는다 (기차 무피해)
///   2) 디 오리지널 3페이즈:
///      P1 사냥(100~70%): 포효 소환 (기존)
///      P2 폭식(70~35%): 재료 조각 쟁탈전 - 보스가 조각을 먹으면 회복+공격력 스택
///         (회복 상한 = 최대 HP 15%, 공격력 상한 +50%)
///      P3 해치 개방(35%~): 받는 피해 +30%, 폭식 종료 (마지막 주문/엔딩 분기는 C-2에서)
/// ---------------------------------------------------------------
/// (v5) 보스 패턴 B단계
/// - v5 변경점:
///   1) 번개 병 패링 (천둥 둥지): 낙뢰 예고 마지막 0.6초에 Space -> 낙뢰 무효 + 병 1충전
///      3병 모으면 여왕에게 되쏘아 강제 그로기. 미사용 병은 처치 시 전기 재료로 환급
///      조리 미니게임 중이면 미니게임이 잠시 대기하고 Space가 패링으로 쓰인다
///   2) 해동포 연동 (동면자): ThawCannonUI가 호출하는 HitByThawCannon (갑주 파괴/약화)
///   3) 발악 페이즈: HP 50% 이하 -> 패턴 가속 + 소환/낙뢰 규모 증가
/// ---------------------------------------------------------------
/// (v4) 보스 패턴 A단계
/// 프리팹 1개를 그대로 쓰면서, 등장 지역에 따라 다른 보스가 된다.
///
/// - v4 변경점:
///   1) 보스 4종 개성화 (지역 번호로 자동 결정 - 씬/프리팹 작업 0):
///      지역 1 "녹슨 발톱"   (알파 랩터, 녹슨 적갈색, 빠름)
///      지역 2 "천둥 둥지"   (프테라 여왕, 뇌운 보라, 원거리)
///      지역 3 "동면자"      (고대 모사, 한랭 청록, 단단함)
///      최종   "디 오리지널" (메카 티렉스, 핏빛)
///   2) 패턴 시스템: 예고(텔레그래프) -> 실행 -> 파훼 판정
///      - 사냥 호령(지역1): 랩터 소환. 예고 중 보스에게 스턴 명중 시 소환 절반
///      - 낙뢰 폭격(지역2): 포탑 슬롯 감전 마비. 마커 클릭으로 즉시 재가동
///      - 빙하 갑주(지역3): 받는 피해 90% 감소. 화상 스택 누적으로 파괴(+보너스 그로기)
///        (화염 도트는 갑주를 무시하고 태운다 - Enemy.ModifyIncomingDamage 주석 참조)
///      - 포효(최종): 정예 증원 소환
///   3) 수치는 전부 GameBalance의 '보스 패턴' 섹션에서 조정
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class BossEnemy : Enemy
{
    // ─────────────────────────────────────────────
    // 보스 종류 (지역 기반 자동 결정)
    // ─────────────────────────────────────────────
    public enum BossKind
    {
        RustClaw,     // 지역 1: 녹슨 발톱 (알파 랩터)
        ThunderNest,  // 지역 2: 천둥 둥지 (프테라 여왕)
        Hibernator,   // 지역 3: 동면자 (고대 모사)
        Original      // 최종: 디 오리지널
    }

    [Header("─ 보스 전용 (런타임 계산 - GameBalance에서 조정) ─")]
    public float bossMaxHP = 1000f;
    public BossKind kind = BossKind.RustClaw;   // Start에서 지역 기반으로 덮어씀

    [Header("─ 보스 이동/공격 ─")]
    public float bossAttackRange = 5f;
    public float bossAttackCooldown = 2.5f;
    public float bossMoveSpeed = 1.6f;

    [Header("─ 보스 방어 스탯 ─")]
    public float bossDefense = 25f;
    public float bossResistance = 25f;

    [Header("─ 그로기 설정 ─")]
    public float groggyDuration = 7f;
    public float groggyCooldownGap = 6f;

    // ── 그로기 상태 ──
    private bool isGroggy = false;
    private float groggyLockUntil = 0f;
    private float[] groggyThresholds = { 0.75f, 0.50f, 0.25f };
    private bool[] groggyTriggered = { false, false, false };

    /// <summary>이번 그로기의 실제 지속 시간 (BossGimmickSystem이 게이지에 사용)</summary>
    public float CurrentGroggyDuration { get; private set; }

    // ── 런지/패턴 상태 ──
    private bool isLunging = false;
    private bool isCasting = false;        // 패턴 시전 중 (이동/공격 정지)
    private float patternTimer = 0f;       // 다음 패턴까지 남은 시간

    // ── 빙하 갑주 (동면자) ──
    private bool armorActive = false;
    private bool secondArmorUsed = false;  // 50% 재전개는 1회만
    private int burnBaseline = 0;          // 갑주 전개 시점의 화상 누적치
    private float armorDR = 0f;            // v5: 현재 갑주 감쇄율 (해동포 GOOD으로 절반 가능)

    // ── v5: 번개 병 패링 (천둥 둥지) ──
    public int ParryCharges { get; private set; }

    // ── v5: 발악 페이즈 ──
    private bool enraged = false;

    // ── v6: 디 오리지널 3페이즈 ──
    private int originalPhase = 1;
    private float feedHealAccum = 0f;   // 폭식으로 회복한 총량 (상한 관리)
    private float feedAtkBonus = 0f;    // 폭식 공격력 보너스 누적
    private bool hatchOpen = false;     // P3: 가슴 해치 개방 (받는 피해 증가)

    // ── v7 (C-2): 마지막 식사 (엔딩 B) ──
    private bool isServing = false;     // 정찬 대접 연출 중 (모든 행동 정지)

    /// <summary>디 오리지널 현재 페이즈 (FinalOrderUI 참조용. 다른 보스는 1)</summary>
    public int OriginalPhaseNow { get { return originalPhase; } }

    /// <summary>갑주 활성 여부 (ThawCannonUI 참조용)</summary>
    public bool ArmorActive { get { return armorActive; } }

    // ── 디버프 요리 복구용 ──
    private float baseDefenseValue;
    private float baseResistanceValue;

    // ── 연출 ──
    private SpriteRenderer[] sprites;
    private Color baseTint = Color.white;
    private WaveManager waveManagerRef;

    private void Awake()
    {
        // 보스 데이터 초기화 (이름/수치는 Start에서 지역 기반으로 채움)
        data = new EnemyData
        {
            enemyName = "메카 티렉스 보스",
            baseHP = 1000f,
            baseATK = 50f,
            baseSPD = 1.0f,
            dropMaterialName = "메카 티렉스의 심장",
            goldReward = 1000,
            xpReward = 500,
            targetPriority = "기차 전체",
            specialAbility = "HP 75/50/25% 그로기"
        };
    }

    private void Start()
    {
        int wave = GameManager.Instance != null ? GameManager.Instance.currentWave : 3;

        // ── 지역 기반 보스 종류 결정 ──
        int region = GameBalance.RegionOf(wave);
        if (region == 1) kind = BossKind.RustClaw;
        else if (region == 2) kind = BossKind.ThunderNest;
        else if (region == 3) kind = BossKind.Hibernator;
        else kind = BossKind.Original;

        // ── 공통 웨이브 비례 스탯 ──
        bossMaxHP = GameBalance.BossHPBase + wave * GameBalance.BossHPPerWave;
        float bossATK = GameBalance.BossATKBase + wave * GameBalance.BossATKPerWave;
        float spd = bossMoveSpeed;

        // ── 종류별 개성 (이름 / 스탯 방향 / 색) ──
        string intro;
        if (kind == BossKind.RustClaw)
        {
            data.enemyName = "녹슨 발톱";
            bossMaxHP *= 0.9f; bossATK *= 0.9f; spd = 2.0f;      // 빠르고 가벼움
            baseTint = new Color(0.9f, 0.55f, 0.38f);
            intro = "무리의 왕이 나타났다! 호령은 스턴으로 저지할 수 있다!";
        }
        else if (kind == BossKind.ThunderNest)
        {
            data.enemyName = "천둥 둥지";
            bossMaxHP *= 0.95f; spd = 1.5f; bossAttackRange = 6.5f; // 멀리서 때림
            baseTint = new Color(0.72f, 0.72f, 1f);
            intro = "선대의 번개 병이 기차에 실려 있다 - 낙뢰의 마지막 순간, [Space]로 병을 치켜라!";
        }
        else if (kind == BossKind.Hibernator)
        {
            data.enemyName = "동면자";
            bossMaxHP *= 1.15f; spd = 1.25f;                      // 느리고 단단함
            baseTint = new Color(0.6f, 0.85f, 1f);
            intro = "고대 모사! 갑주는 화염으로만 녹는다 - 광산의 해동포에 화염을 장전하라!";
        }
        else
        {
            data.enemyName = "디 오리지널";
            bossMaxHP *= 1.2f; bossATK *= 1.1f; spd = 1.4f;
            baseTint = new Color(1f, 0.5f, 0.45f);
            intro = "대륙에서 가장 오래 굶은 손님이 식탁에 앉았다.";
        }

        currentHP = bossMaxHP;
        scaledMaxHP = bossMaxHP;
        scaledATK = bossATK;
        scaledSPD = spd;

        // 방어 스탯 (부모 자동배정이 안 돌므로 직접)
        defense = bossDefense;
        resistance = bossResistance;
        baseDefenseValue = defense;
        baseResistanceValue = resistance;

        attackRange = bossAttackRange;
        attackCooldown = bossAttackCooldown;

        GameObject trainObj = GameObject.FindGameObjectWithTag("Train");
        if (trainObj != null) trainTarget = trainObj.transform;
        trainManager = FindFirstObjectByType<TrainManager>();
        waveManagerRef = FindFirstObjectByType<WaveManager>();

        // 색 입히기 (자식 스프라이트 전부)
        sprites = GetComponentsInChildren<SpriteRenderer>();
        ApplyTint(baseTint);

        // 동면자: 개전 시 빙하 갑주 전개
        if (kind == BossKind.Hibernator)
            ActivateArmor();

        // 첫 패턴 타이머
        patternTimer = GameBalance.BossPatternFirstDelay;

        BossGimmickSystem.Instance?.RegisterBoss(this);
        UIManager.Instance?.ShowWaveNotice("[" + data.enemyName + "]", intro);

        Debug.Log("[BossEnemy] " + data.enemyName + " 등장! (웨이브 " + wave + ") HP:" + (int)bossMaxHP
            + " ATK:" + (int)scaledATK + " 종류:" + kind);
    }

    // ─────────────────────────────────────────────
    // 메인 루프
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (!IsAlive) return;

        // 도트/방깎 타이머 (v3에서 수정된 보스 도트 버그 유지)
        TickStatusEffects();

        // 빙하 갑주 파괴 판정 (화상 스택 누적 감시)
        if (armorActive && TotalBurnApplied - burnBaseline >= GameBalance.GlacierBreakBurnStacks)
            BreakArmor();

        // v5: 발악 페이즈 진입 (HP 50% 이하, 1회)
        if (!enraged && currentHP / bossMaxHP <= GameBalance.EnrageHPRatio)
        {
            enraged = true;
            UIManager.Instance?.ShowStatChange("[" + data.enemyName + "] 발악! 패턴이 빨라진다!");
            Debug.Log("[BossEnemy] 발악 페이즈 진입!");
        }

        // v6: 디 오리지널 페이즈 전환
        if (kind == BossKind.Original)
            CheckOriginalPhases();

        // 그로기 진입 체크
        if (!isGroggy && Time.time >= groggyLockUntil)
            CheckGroggyThresholds();

        if (isServing) return;   // v7: 마지막 식사 연출 중 - 완전 정지
        if (isGroggy || isLunging || isCasting) return;

        if (trainTarget == null)
        {
            GameObject trainObj = GameObject.FindGameObjectWithTag("Train");
            if (trainObj != null) trainTarget = trainObj.transform;
            return;
        }

        // 패턴 타이머 (통상 상태에서만 감소)
        patternTimer -= Time.deltaTime;
        if (patternTimer <= 0f)
        {
            StartCoroutine(RunPattern());
            return;
        }

        // 통상 이동/공격 (v6: 도발 중이면 미끼를 추적)
        attackTimer += Time.deltaTime;
        float distanceToTrain = Vector3.Distance(transform.position, CurrentTarget.position);

        if (distanceToTrain > attackRange)
            MoveTowardsTrain();
        else if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            StartCoroutine(AttackLunge());
        }
    }

    // ─────────────────────────────────────────────
    // v6: 디 오리지널 3페이즈 관리
    // ─────────────────────────────────────────────
    private void CheckOriginalPhases()
    {
        float ratio = currentHP / bossMaxHP;

        // P2 폭식 (70% 이하): 재료 조각 쟁탈전 시작
        if (originalPhase == 1 && ratio <= GameBalance.FeedPhaseStartRatio)
        {
            originalPhase = 2;
            PickupFX.FeedingBoss = this;
            UIManager.Instance?.ShowWaveNotice("[디 오리지널] 폭식!",
                "재료 조각을 도둑맞는다 - 보스 곁에서 적을 잡지 마라!");
            Debug.Log("[BossEnemy] P2 폭식 페이즈 - 조각 쟁탈전 시작");
        }

        // P3 해치 개방 (35% 이하): 폭식 종료 + 받는 피해 증가
        if (originalPhase == 2 && ratio <= GameBalance.HatchPhaseStartRatio)
        {
            originalPhase = 3;
            PickupFX.FeedingBoss = null;
            hatchOpen = true;
            ApplyTint(Color.Lerp(baseTint, Color.white, 0.35f));   // 해치의 빛
            UIManager.Instance?.ShowWaveNotice("[디 오리지널] 가슴 해치 개방!",
                "받는 피해 +" + Mathf.RoundToInt((GameBalance.HatchDamageTakenMul - 1f) * 100f)
                + "%! 지금이 기회다!");
            Debug.Log("[BossEnemy] P3 해치 개방 - 받는 피해 증가");
        }
    }

    /// <summary>v6: 폭식 - 재료 조각을 먹어치움 (PickupFX가 호출)</summary>
    public void EatFragment()
    {
        if (!IsAlive) return;

        // 회복 (총량 상한)
        float cap = bossMaxHP * GameBalance.FeedHealCapRatio;
        if (feedHealAccum < cap)
        {
            float heal = Mathf.Min(GameBalance.FeedHealPerFragment, cap - feedHealAccum);
            feedHealAccum += heal;
            currentHP = Mathf.Min(currentHP + heal, bossMaxHP);
        }

        // 공격력 스택 (상한)
        if (feedAtkBonus < GameBalance.FeedAtkCap)
        {
            feedAtkBonus += GameBalance.FeedAtkPerFragment;
            scaledATK *= (1f + GameBalance.FeedAtkPerFragment);
        }

        Debug.Log("[BossEnemy] 폭식! 조각 흡수 (회복 누적 " + (int)feedHealAccum
            + " / ATK 보너스 " + Mathf.RoundToInt(feedAtkBonus * 100f) + "%)");
    }

    // ─────────────────────────────────────────────
    // 패턴 시스템 (A단계: 종류별 시그니처 1개)
    // ─────────────────────────────────────────────
    private IEnumerator RunPattern()
    {
        isCasting = true;

        if (kind == BossKind.RustClaw)
            yield return StartCoroutine(PatternHowl());
        else if (kind == BossKind.ThunderNest)
            yield return StartCoroutine(PatternLightning());
        else if (kind == BossKind.Hibernator)
            yield return StartCoroutine(PatternRearmor());
        else
            yield return StartCoroutine(PatternRoar());

        isCasting = false;

        // v5: 발악 시 패턴 간격 단축
        float interval = GameBalance.BossPatternInterval + Random.Range(-2f, 2f);
        if (enraged) interval *= GameBalance.EnragePatternIntervalMul;
        patternTimer = interval;
    }

    /// <summary>예고 대기 공통 처리. 그로기/사망으로 끊기면 false</summary>
    private IEnumerator Telegraph(string text)
    {
        BossGimmickSystem.Instance?.ShowPatternTelegraph(text, GameBalance.BossTelegraphSec);
        ApplyTint(Color.Lerp(baseTint, Color.white, 0.6f));   // 예고 중 발광

        float t = 0f;
        while (t < GameBalance.BossTelegraphSec)
        {
            t += Time.deltaTime;
            if (isGroggy || !IsAlive) break;
            yield return null;
        }

        ApplyTint(armorActive ? ArmorTint() : baseTint);
    }

    /// <summary>지역 1 - 사냥 호령: 랩터 소환. 예고 중 스턴 명중 시 절반으로 저지</summary>
    private IEnumerator PatternHowl()
    {
        float castStart = Time.time;
        yield return StartCoroutine(Telegraph("사냥 호령! 울음소리가 황야를 가른다 (스턴으로 저지!)"));
        if (isGroggy || !IsAlive) yield break;

        int count = GameBalance.HowlSummonCount + (enraged ? GameBalance.EnrageExtraSummon : 0);
        bool disrupted = LastStunTime >= castStart;   // 예고 중 스턴 맞았는가
        if (disrupted)
        {
            count = Mathf.Max(1, count / 2);
            UIManager.Instance?.ShowStatChange("호령 저지 성공! 소환 절반!");
        }

        if (waveManagerRef != null)
            waveManagerRef.SpawnReinforcements("raptor", count, 0.7f);
        Debug.Log("[BossEnemy] 사냥 호령 - 랩터 " + count + "마리" + (disrupted ? " (저지됨)" : ""));
    }

    /// <summary>
    /// 지역 2 - 낙뢰 폭격 + 번개 병 패링 (v5)
    /// 예고 마지막 ParryWindowSec 동안 Space -> 낙뢰를 병에 담는다 (낙뢰 무효 + 1충전)
    /// 너무 일찍 누르면 헛스윙 (이번 낙뢰의 패링 기회 소진)
    /// 3병 모으면 여왕에게 되쏘아 강제 그로기
    /// </summary>
    private IEnumerator PatternLightning()
    {
        float teleSec = GameBalance.BossTelegraphSec;
        BossGimmickSystem.Instance?.ShowPatternTelegraph(
            "낙뢰 폭격! 게이지 끝자락에서 [Space] 패링 - 번개를 병에 담아라!", teleSec);
        ApplyTint(Color.Lerp(baseTint, Color.white, 0.6f));

        bool parried = false;
        bool attempted = false;
        float t = 0f;

        while (t < teleSec)
        {
            t += Time.deltaTime;
            if (isGroggy || !IsAlive) { ApplyTint(baseTint); yield break; }

            bool inWindow = (teleSec - t) <= GameBalance.ParryWindowSec;

            // 패링 창이 열리면 조리 미니게임을 잠시 대기시켜 Space를 빌려온다
            if (inWindow && CookingMinigame.Instance != null)
                CookingMinigame.Instance.HoldFor(0.2f);

            if (!attempted && Input.GetKeyDown(KeyCode.Space))
            {
                attempted = true;
                if (inWindow)
                {
                    parried = true;
                    ParryCharges++;
                    SoundManager.Play("sfx_parry");
                    UIManager.Instance?.ShowStatChange("패링! 번개를 병에 담았다 ("
                        + ParryCharges + "/" + GameBalance.ParryChargesForCounter + ")");
                    Debug.Log("[BossEnemy] 번개 병 패링 성공! 충전 " + ParryCharges);
                }
                else
                {
                    UIManager.Instance?.ShowStatChange("너무 빨랐다! 병을 헛들었다...");
                }
            }

            yield return null;
        }

        ApplyTint(armorActive ? ArmorTint() : baseTint);
        if (isGroggy || !IsAlive) yield break;

        // 패링 성공 -> 낙뢰 무효. 3병이면 되쏘기(강제 그로기)
        if (parried)
        {
            if (ParryCharges >= GameBalance.ParryChargesForCounter)
            {
                ParryCharges = 0;
                UIManager.Instance?.ShowWaveNotice("되쏘기!", "병에 담은 번개가 여왕을 꿰뚫는다 - 그로기!");
                Debug.Log("[BossEnemy] 번개 되쏘기 - 강제 그로기!");
                ForceGroggy(GameBalance.ParryCounterGroggySec);
            }
            yield break;
        }

        if (TurretSlotManager.Instance == null) yield break;

        // 마비 후보: 가동 중(비어있지 않고, 잠금 아니고, 이미 마비 아님)
        TurretSlot[] slots = TurretSlotManager.Instance.slots;
        System.Collections.Generic.List<TurretSlot> candidates =
            new System.Collections.Generic.List<TurretSlot>();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null || slots[i].IsEmpty || slots[i].isLocked || slots[i].IsStunned) continue;
            candidates.Add(slots[i]);
        }

        int hitCount = 0;
        int strikeCount = GameBalance.LightningSlotCount + (enraged ? GameBalance.EnrageExtraLightning : 0);
        for (int n = 0; n < strikeCount && candidates.Count > 0; n++)
        {
            int idx = Random.Range(0, candidates.Count);
            candidates[idx].StunSlot(GameBalance.LightningStunSec);
            candidates.RemoveAt(idx);
            hitCount++;
        }

        if (hitCount > 0)
            UIManager.Instance?.ShowStatChange("포탑 " + hitCount + "기 감전! 마커 클릭으로 재가동!");
        Debug.Log("[BossEnemy] 낙뢰 폭격 - 슬롯 " + hitCount + "곳 마비");
    }

    /// <summary>지역 3 - 갑주 재전개: 50% 이하에서 1회, 빙하 갑주를 다시 두른다</summary>
    private IEnumerator PatternRearmor()
    {
        // 갑주가 이미 있거나 재전개를 썼으면 이번 사이클은 조용히 넘어간다
        if (armorActive || secondArmorUsed || currentHP / bossMaxHP > 0.5f)
            yield break;

        yield return StartCoroutine(Telegraph("냉기가 다시 뭉친다 - 갑주 재전개!"));
        if (isGroggy || !IsAlive) yield break;

        secondArmorUsed = true;
        ActivateArmor();
    }

    /// <summary>최종 - 포효: 정예 증원 소환</summary>
    private IEnumerator PatternRoar()
    {
        yield return StartCoroutine(Telegraph("포효! 대륙이 울린다!"));
        if (isGroggy || !IsAlive) yield break;

        int roarCount = GameBalance.OriginalRoarCount + (enraged ? GameBalance.EnrageExtraSummon : 0);
        if (waveManagerRef != null)
            waveManagerRef.SpawnReinforcements("raptor", roarCount, 0.8f);
        Debug.Log("[BossEnemy] 포효 - 증원 " + roarCount + "마리");
    }

    // ─────────────────────────────────────────────
    // 빙하 갑주 (동면자)
    // ─────────────────────────────────────────────
    private void ActivateArmor()
    {
        armorActive = true;
        armorDR = GameBalance.GlacierArmorDR;   // v5: 현재 감쇄율 (해동포 GOOD으로 절반 가능)
        burnBaseline = TotalBurnApplied;
        ApplyTint(ArmorTint());
        UIManager.Instance?.ShowStatChange("[빙하 갑주] 받는 피해 -"
            + Mathf.RoundToInt(GameBalance.GlacierArmorDR * 100f) + "%! 화염으로 녹여라!");
        Debug.Log("[BossEnemy] 빙하 갑주 전개 (화상 " + GameBalance.GlacierBreakBurnStacks + "스택으로 파괴)");
    }

    private void BreakArmor()
    {
        armorActive = false;
        ApplyTint(baseTint);
        UIManager.Instance?.ShowStatChange("빙하 갑주 파괴! 보스 그로기!");
        Debug.Log("[BossEnemy] 빙하 갑주 파괴 - 보너스 그로기 " + GameBalance.GlacierBreakGroggySec + "초");

        // 파괴 보상: 짧은 보너스 그로기
        ForceGroggy(GameBalance.GlacierBreakGroggySec);
    }

    private Color ArmorTint()
    {
        return new Color(0.45f, 0.95f, 1f);   // 갑주 중엔 얼음빛 강조
    }

    /// <summary>강제 그로기 (갑주 파괴 보상 / 추후 패링 반격 등에서 사용)</summary>
    public void ForceGroggy(float seconds)
    {
        if (isGroggy || !IsAlive) return;
        StartCoroutine(EnterGroggyState(seconds));
    }

    // ─────────────────────────────────────────────
    // 갑주 데미지 감쇄 (Enemy 훅 오버라이드)
    // 도트(화상/독)는 이 훅을 거치지 않는다 - 화염 도트가 갑주 파훼 수단
    // ─────────────────────────────────────────────
    protected override float ModifyIncomingDamage(float damage, DamageType dtype)
    {
        if (armorActive)
            return damage * (1f - armorDR);

        // v6: 해치 개방 (디 오리지널 P3) - 받는 피해 증가
        if (hatchOpen)
            return damage * GameBalance.HatchDamageTakenMul;

        return damage;
    }

    // ─────────────────────────────────────────────
    // v5: 해동포 (ThawCannonUI가 호출)
    // quality: 2=PERFECT(정중앙) / 1=GOOD(존 안) / 0=MISS(존 밖)
    // ─────────────────────────────────────────────
    public void HitByThawCannon(int quality)
    {
        if (!IsAlive) return;

        if (quality >= 2)
        {
            // 정중앙: 갑주 즉시 전파괴(보너스 그로기 포함) + 대미지
            if (armorActive) BreakArmor();
            TakeDamage(GameBalance.ThawPerfectDamage, DamageType.Magic);
            UIManager.Instance?.ShowStatChange("해동포 직격! 갑주가 산산조각났다!");
        }
        else if (quality == 1)
        {
            // 존 안: 갑주 감쇄율 절반 + 중간 대미지
            if (armorActive)
            {
                armorDR *= 0.5f;
                UIManager.Instance?.ShowStatChange("해동포 명중! 갑주 감쇄율 절반!");
            }
            TakeDamage(GameBalance.ThawGoodDamage, DamageType.Magic);
        }
        else
        {
            // 빗맞음: 대미지만
            TakeDamage(GameBalance.ThawMissDamage, DamageType.Magic);
            UIManager.Instance?.ShowStatChange("해동포 빗맞음...");
        }

        Debug.Log("[BossEnemy] 해동포 피격 (품질 " + quality + ")");
    }

    // ─────────────────────────────────────────────
    // 돌진 공격 (v3 유지)
    // ─────────────────────────────────────────────
    private IEnumerator AttackLunge()
    {
        isLunging = true;

        Vector3 startPos = transform.position;
        Vector3 dir = (CurrentTarget.position - startPos).normalized;
        Vector3 peakPos = startPos + dir * 1.2f;

        float t = 0f;
        while (t < 0.16f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, peakPos, t / 0.16f);
            yield return null;
        }

        // P1 게임필: 런지 착지 임팩트 (기차 피격 셰이크와 별개의 육중함 - 절반 강도)
        GameFeel.Shake(GameBalance.ShakeBoss * 0.5f);

        // v6: 도발 중이면 미끼를 물어뜯는다 (기차 무피해)
        if (!IsTaunted)
        {
            AttackTrain();
            Debug.Log("[BossEnemy] 기차 공격! -" + (int)scaledATK);
        }
        else
        {
            Debug.Log("[BossEnemy] 미끼를 물어뜯는 중!");
        }

        t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(peakPos, startPos, t / 0.3f);
            yield return null;
        }
        transform.position = startPos;

        isLunging = false;
    }

    // ─────────────────────────────────────────────
    // 그로기 (v3 유지 + 지속 시간 파라미터화)
    // ─────────────────────────────────────────────
    private void CheckGroggyThresholds()
    {
        float hpRatio = currentHP / bossMaxHP;
        for (int i = 0; i < groggyThresholds.Length; i++)
        {
            if (!groggyTriggered[i] && hpRatio <= groggyThresholds[i])
            {
                groggyTriggered[i] = true;
                StartCoroutine(EnterGroggyState(groggyDuration));
                break;
            }
        }
    }

    private IEnumerator EnterGroggyState(float duration)
    {
        isGroggy = true;
        CurrentGroggyDuration = duration;
        Debug.Log("[BossEnemy] !! 보스 그로기 !! F키로 디버프 요리 투척! (" + duration + "초)");

        // P1 게임필: 그로기 진입 = 히트스톱 + 강한 셰이크 (거체가 무너지는 순간)
        GameFeel.Hitstop(GameBalance.HitstopBossGroggy);
        GameFeel.Shake(GameBalance.ShakeBoss);

        yield return new WaitForSeconds(duration);

        isGroggy = false;
        CurrentGroggyDuration = 0f;
        groggyLockUntil = Time.time + groggyCooldownGap;

        defense = baseDefenseValue;
        resistance = baseResistanceValue;
        Debug.Log("[BossEnemy] 보스 그로기 종료 - 방어력 복구");
    }

    public void ReceiveDebuffFood(float reductionMultiplier = 0.5f)
    {
        if (!isGroggy)
        {
            Debug.Log("[BossEnemy] 그로기 상태가 아니어서 디버프 요리 무효!");
            return;
        }

        defense = baseDefenseValue * reductionMultiplier;
        resistance = baseResistanceValue * reductionMultiplier;
        Debug.Log("[BossEnemy] 디버프 요리 적중! DEF/RES " +
                  ((1f - reductionMultiplier) * 100f).ToString("F0") + "% 감소! (" +
                  (int)defense + "/" + (int)resistance + ")");
    }

    // ─────────────────────────────────────────────
    // v7 (C-2): 마지막 식사 - 풀코스 QTE 성공 시 (FinalOrderUI가 호출)
    // 격파가 아니라 "대접"으로 끝나는 진엔딩 경로
    // ─────────────────────────────────────────────
    public void ServeLastSupper()
    {
        if (!IsAlive || isServing) return;

        isServing = true;
        isGroggy = false;   // 그로기 해제 (연출 우선)
        StopAllCoroutines();   // 패턴/그로기 코루틴 정리

        // 흡수 참조 정리
        if (PickupFX.FeedingBoss == this) PickupFX.FeedingBoss = null;

        Debug.Log("[BossEnemy] 마지막 식사 - 디 오리지널이 정찬을 받았다");
        SoundManager.Play("sfx_train_whistle");

        // 엔딩 B 연출 -> 닫히면 기록 + 처치 처리 (웨이브 클리어 -> Victory로 이어짐)
        StoryTexts.ShowEndingB(delegate
        {
            MetaProgress.RecordEndingB();
            Die();   // 보상 지급 + 웨이브 클리어 체인 (최종전 -> Victory)
        });
    }

    // ─────────────────────────────────────────────
    // 연출 헬퍼
    // ─────────────────────────────────────────────
    private void ApplyTint(Color c)
    {
        if (sprites == null) return;
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) sprites[i].color = c;
    }

    // ─────────────────────────────────────────────
    // 사망
    // ─────────────────────────────────────────────
    protected override void Die()
    {
        // P1 게임필: 보스 처치 = 가장 긴 히트스톱 + 강한 셰이크 + 금색 대형 팝
        GameFeel.Hitstop(GameBalance.HitstopBossKill);
        GameFeel.Shake(GameBalance.ShakeBoss);
        GameFeel.DeathPop(transform.position, new Color(1f, 0.85f, 0.4f), 3f);

        // 보스는 전 재료 2개씩 지급 (base.Die()가 심장 매핑 1개도 추가로 줌)
        if (MaterialInventory.Instance != null)
        {
            foreach (MaterialType t in System.Enum.GetValues(typeof(MaterialType)))
                MaterialInventory.Instance.Add(t, 2);
            Debug.Log("[BossEnemy] 보스 처치 보상: 전 재료 2개씩 지급!");
        }

        // v5: 미사용 번개 병은 전기 재료로 환급 (패링 보상 = 식재료 수확)
        if (ParryCharges > 0 && MaterialInventory.Instance != null)
        {
            MaterialInventory.Instance.Add(MaterialType.Elec, ParryCharges);
            UIManager.Instance?.ShowStatChange("번개 병 " + ParryCharges + "개 -> 전기 재료로 환급!");
            ParryCharges = 0;
        }

        base.Die();
        BossGimmickSystem.Instance?.OnBossDefeated();
    }

    // v6: 어떤 이유로든 사라질 때 폭식 참조 정리 (씬 전환/사망)
    private void OnDestroy()
    {
        if (PickupFX.FeedingBoss == this)
            PickupFX.FeedingBoss = null;
    }

    public bool IsGroggy => isGroggy;
}
