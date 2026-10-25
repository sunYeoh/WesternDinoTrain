using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// [BossPatterns.cs] v1.1 (v9.33 2026-10-25 소리: 눈사태 예고 울음 = 경고 버스 (울음이 경고 역할 - 한 번만), 눈사태·칸 분리 폭음 = 보스 버스 - SoundManager.PlayIn) / v1 (v9.23 2026-10-14 신규) - 보스 새 패턴. BossEnemy 의 나머지 반쪽이다 (partial class - BossEnemy.cs 와 같은 클래스)
/// 스펙 = claude/보스개편_스펙_2026-10-07.md 3-1 ~ 3-4절. 값과 스위치 = GameBalance 맨 아래 v9.23 섹션 (BossNewPatterns = false 면 v9.22 까지의 보스)
///
///  녹슨 발톱  R1 부관 둘 (등장 전조 때 좌우에서 달려와 붙는다, 쓰러지면 왕이 울고 곧 호령) / R2 삼단 도약 (그림자 원 + 숫자 -> 북 남 북 착지) / R3 호령 -> 편성 (BossEnemy.PatternHowl)
///  천둥 둥지  T1 날갯짓 + 자리 옮기기 / 근접 공격 없음 / T2 알 투하 (부화 전에 깨면 안 나온다 - BossEgg) / T3 편대 발진 (발악 뒤, 표시한 칸으로 플라이)
///  동면자     H1 꼬리 후려치기 (돌진 대신) / H2 백야의 입김 (두 칸 포탑 느려짐 + 조리 빨라짐, 굽기 하나면 녹는다) / H3 눈사태 (12초 벽, [F] 독샘 요리·해동포로 멈춘다)
///  디 오리지널 O1 페이즈마다 앞뒤로 / O2 약점 칸 (AimPoint) / O3 하차 (포효 대신) / O4 측면 충돌 (돌진 대신) / O5 폭식 조각이 화물칸 입으로 (FeedPoint) / O6 쓰러질 때 칸이 떨어져 나간다
///  공통        보스가 쓰러지면 남은 손님이 달아난다 (Enemy.Rout) / B2 기차 HP 바 빗금 (UIManager.PreviewTrainDamage)
///
/// 패턴의 뼈대는 v9.20 그대로: 예고(몸이 부풀고 운다 - CastBegin / TickCastFx) -> 발동(눌렸다 돌아온다 - CastRelease) -> 결과가 전장에 보인다.
/// 새 표시는 PatternMark (바닥 원·칸 테두리·조여 드는 고리·숫자) - BossMark 와 같이 스스로 사라진다.
///
/// 사용법: BossEnemy.cs 와 같은 폴더(Assets/Prototype)에 둔다. 씬·프리팹 작업 없음.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public partial class BossEnemy
{
    // ─────────────────────────────────────────────
    // v9.23 상태
    // ─────────────────────────────────────────────
    private int patternStep = 0;                 // 보스마다 돌아가는 패턴 순서의 몇 번째인가
    private int howlCount = 0;                   // R3: 포위 / 양동을 번갈아
    private bool forceHowlNext = false;          // R1: 부관이 쓰러졌다 - 다음 패턴은 호령
    private Vector2 poseScale = Vector2.one;     // 패턴이 거는 몸 배율 (날갯짓·날개 접기·뛰어오름) - TickSkin 이 곱한다
    private float wingFold = 0f;                 // T3: 날개를 접는다 (0 ~ 1)
    private float lookOffset = 0f;               // 서는 각도에 더하는 각 (R1: 부관이 쓰러지면 고개를 쳐든다)
    private float heatGlow = 0f;                 // O4: 측면 충돌 예고 - 차체가 붉게 달아오른다 (0 ~ 1, TickSkin)
    private bool gliding = false;                // 자리를 옮기는 중 (T1 활공 / O1 앞뒤로) - 그동안 공격·패턴을 쉰다
    private float driftTimer = 0f;               // T1: 다음 자리 옮기기까지
    private bool avalancheUsed = false;          // H3: 한 번만
    private bool avalancheCancelled = false;
    private string avalancheCancelBy = "";
    private AvalancheWall avalanche;             // H3: 내려오는 벽 (진행 중일 때만 null 이 아니다)
    private PatternMark frostMarkA, frostMarkB;  // H2: 언 칸 위의 서리 (효과가 이어지는 동안)
    private bool collapsing = false;             // O6: 칸이 떨어져 나가는 중
    private bool collapseDone = false;
    private readonly List<Enemy> lieutenants = new List<Enemy>();
    private bool lieutenantsSpawned = false;
    private Coroutine formationCo;               // R3: 편성 조각이 나오는 중 (보스가 쓰러지면 멈춘다 - 쓰러진 뒤에 랩터가 더 나오지 않게)

    // 색
    private static readonly Color LEAP_COL = new Color(1f, 0.5f, 0.18f, 1f);      // 도약 착지 자리
    private static readonly Color EGG_COL = new Color(0.75f, 0.5f, 1f, 1f);       // 알 떨어질 자리
    private static readonly Color FLIGHT_COL = new Color(1f, 0.3f, 0.18f, 1f);    // 편대가 노리는 칸
    private static readonly Color TAIL_COL = new Color(1f, 0.55f, 0.3f, 1f);      // 꼬리가 칠 칸
    private static readonly Color FROST_COL = new Color(0.8f, 0.95f, 1f, 1f);     // 서리
    private static readonly Color DOOR_COL = new Color(1f, 0.35f, 0.15f, 1f);     // 화물칸 문
    private static readonly Color WEAK_COL = new Color(1f, 0.9f, 0.3f, 1f);       // 약점 칸 조준 고리

    /// <summary>새 패턴을 쓰는가 (정식 보스 + 스위치)</summary>
    private bool NewPatterns { get { return GameBalance.BossNewPatterns && StandMode; } }

    // ─────────────────────────────────────────────
    // 다른 스크립트가 보는 것 (static)
    // ─────────────────────────────────────────────
    private static BossEnemy northBlockedBy;     // O3: 최종전 동안 북쪽 스폰을 막는 보스
    private static float frostUntil = -1f;       // H2: 서리가 풀리는 시각
    private static int frostMask = 0;            // H2: 언 칸 (비트 = 칸 번호)
    private static BossEnemy frostOwner;

    /// <summary>v9.23: 최종전 동안 북쪽 스폰을 막는다 (맞수 열차 몸이 북쪽을 막고 있다) - WaveManager.EdgePoint 가 본다</summary>
    public static bool NorthBlocked { get { return northBlockedBy != null && northBlockedBy.IsAlive; } }

    /// <summary>v9.23 H2: x 자리 포탑의 발사 속도 배율 (언 칸이면 HibBreathFireMul, 아니면 1) - TurretSlot.TickFire 가 쿨다운에 곱한다</summary>
    public static float FrostFireMulAt(float x)
    {
        if (frostMask == 0 || Time.time >= frostUntil || frostOwner == null) return 1f;   // 보스가 사라졌으면(씬 다시 시작) 남은 값을 안 쓴다
        int car = GameBalance.CarIndexOf(x);
        return ((frostMask >> car) & 1) != 0 ? Mathf.Clamp(GameBalance.HibBreathFireMul, 0.05f, 1f) : 1f;
    }

    /// <summary>v9.23 H2: 조리 미니게임 속도 배율 (서리 동안 HibBreathCookMul - 작을수록 커서가 빠르고 시간이 짧다). CookingMinigame 이 시작할 때 곱한다</summary>
    public static float FrostCookMul { get { return (frostMask != 0 && Time.time < frostUntil && frostOwner != null) ? Mathf.Clamp(GameBalance.HibBreathCookMul, 0.2f, 1f) : 1f; } }

    /// <summary>v9.23 H2: 굽기 요리를 완성했다 (CookingBridge.FinishCook) - 서리가 녹는다</summary>
    public static void OnGrillCooked()
    {
        if (frostMask == 0 || Time.time >= frostUntil) return;
        if (frostOwner != null) frostOwner.MeltFrost(true);
        else { frostMask = 0; frostUntil = -1f; }
    }

    /// <summary>v9.23 H3: 눈사태가 내려오는 중인가 (BossGimmickSystem 이 [F] 를 받는다)</summary>
    public bool AvalancheActive { get { return avalanche != null && !avalancheCancelled && IsAlive; } }

    /// <summary>v9.23 H3: 눈사태를 멈춘다 - by = "food"(독샘 요리) / "cannon"(해동포). 멈췄으면 true (벽이 깨지고 무방비는 다음 프레임에)</summary>
    public bool CancelAvalanche(string by)
    {
        if (!AvalancheActive) return false;
        avalancheCancelled = true;
        avalancheCancelBy = by ?? "";
        return true;
    }

    /// <summary>v9.23 O2: 포탑이 겨누는 자리 - 디 오리지널은 페이즈마다 약점 칸 (P1 머리 / P2 맨 뒤 화물칸 / P3 가운데 심장 칸). 다른 보스는 몸 가운데</summary>
    public override Vector3 AimPoint
    {
        get
        {
            if (kind == BossKind.Original && NewPatterns && GameBalance.OrigWeakPointOn && !entering) return BodyPointAt(WeakFrac());
            return transform.position;
        }
    }

    /// <summary>v9.23 O5: 폭식 조각이 빨려 들어가는 자리 - 맨 뒤 화물칸 (PickupFX). 다른 보스·스위치 끔 = 몸 가운데</summary>
    public Vector3 FeedPoint
    {
        get
        {
            if (kind == BossKind.Original && NewPatterns && GameBalance.OrigFeedMouthOn) return BodyPointAt(Frac(GameBalance.OrigWeakFrac, 1, 0.22f));
            return transform.position;
        }
    }

    // ─────────────────────────────────────────────
    // 시작·매 프레임 (BossEnemy.cs 가 부른다)
    // ─────────────────────────────────────────────
    /// <summary>Start 끝에서: 디 오리지널은 측면 충돌 간격 / 서리 상태 초기화 (녹슨 발톱 왕 HP 는 부관이 실제로 왔을 때 SpawnLieutenants 가 깎는다)</summary>
    private void SetupNewPatterns()
    {
        frostMask = 0; frostUntil = -1f; frostOwner = null;
        if (!NewPatterns) return;
        if (kind == BossKind.Original && GameBalance.OrigRamOn) attackCooldown = Mathf.Max(1f, GameBalance.OrigRamCooldown);
    }

    /// <summary>등장 연출이 끝났다: 디 오리지널은 북쪽 스폰을 막는다 / 천둥 둥지는 자리 옮기기 시계 시작</summary>
    private void OnEntranceEnd()
    {
        driftTimer = 0f;
        if (kind == BossKind.Original && NewPatterns && GameBalance.OrigNorthBlocked) northBlockedBy = this;
    }

    /// <summary>매 프레임 (등장 연출이 끝난 뒤, 무방비·패턴 중에도): 천둥 둥지 날갯짓·자리 옮기기 / 눈사태 시작 시계 / 측면 충돌 열기가 식는다</summary>
    private void TickNewPatterns()
    {
        if (!NewPatterns) return;
        if (kind == BossKind.ThunderNest) TickHover();
        if (kind == BossKind.Hibernator && GameBalance.HibAvalancheOn && !avalancheUsed && !isCasting
            && currentHP / bossMaxHP <= GameBalance.HibAvalancheHPRatio)
            patternTimer = Mathf.Min(patternTimer, 0.6f);   // 30% 를 넘는 순간 다음 패턴이 바로 눈사태
        if (heatGlow > 0f && !isLunging) heatGlow = Mathf.MoveTowards(heatGlow, 0f, Time.deltaTime * 3f);
    }

    /// <summary>v9.23: 이번 패턴 (RunPattern 이 부른다). 녹슨 발톱 = 호령 / 도약 번갈아 (부관이 쓰러지면 호령) / 천둥 둥지 = 낙뢰 -> 알 -> (발악 뒤) 편대 /
    /// 동면자 = 30% 밑 첫 패턴 눈사태, 50% 밑 갑주 재전개 한 번, 나머지 입김 / 디 오리지널 = 하차</summary>
    private IEnumerator PickPattern()
    {
        if (!NewPatterns)
        {
            if (kind == BossKind.RustClaw) return PatternHowl();
            if (kind == BossKind.ThunderNest) return PatternLightning();
            if (kind == BossKind.Hibernator) return PatternRearmor();
            return PatternRoar();
        }
        switch (kind)
        {
            case BossKind.RustClaw:
                {
                    bool howl = forceHowlNext || !GameBalance.RustLeapOn || (patternStep % 2 == 0);
                    forceHowlNext = false;
                    patternStep++;
                    return howl ? PatternHowl() : PatternLeap();
                }
            case BossKind.ThunderNest:
                {
                    int len = (enraged && GameBalance.ThunderFlightOn) ? 3 : 2;
                    int step = patternStep % len;
                    patternStep++;
                    if (step == 1 && GameBalance.ThunderEggsOn) return PatternEggs();
                    if (step == 2) return PatternFlight();
                    return PatternLightning();
                }
            case BossKind.Hibernator:
                {
                    float ratio = currentHP / bossMaxHP;
                    if (GameBalance.HibAvalancheOn && !avalancheUsed && ratio <= GameBalance.HibAvalancheHPRatio) return PatternAvalanche();
                    if (!armorActive && !secondArmorUsed && ratio <= 0.5f) return PatternRearmor();
                    return GameBalance.HibBreathOn ? PatternFrostBreath() : PatternRearmor();
                }
            default:
                return GameBalance.OrigUnloadOn ? PatternUnload() : PatternRoar();
        }
    }

    /// <summary>v9.23: 기본 공격 (Update 가 공격 시계가 찼을 때). 녹슨 발톱 = 돌진 물기 / 천둥 둥지 = 없음 / 동면자 = 꼬리 후려치기 / 디 오리지널 = 측면 충돌</summary>
    private void StartBasicAttack()
    {
        if (NewPatterns)
        {
            if (kind == BossKind.ThunderNest && GameBalance.ThunderNoMelee) { attackTimer = 0f; return; }
            if (kind == BossKind.Hibernator && GameBalance.HibTailOn) { StartCoroutine(AttackTail()); return; }
            if (kind == BossKind.Original && GameBalance.OrigRamOn) { StartCoroutine(AttackRam()); return; }
        }
        StartCoroutine(AttackLunge());
    }

    /// <summary>예고 대기 (길이를 정하는 판 - Telegraph 는 BossTelegraphSec 고정). frostDisc = 동면자의 발밑 서리 원 (갑주 재전개만). perFrame = 예고 동안 매 프레임 (몸 돌리기 등). 무방비·사망으로 끊기면 CastCancel</summary>
    private IEnumerator TelegraphFor(string text, float sec, bool frostDisc, System.Action perFrame)
    {
        if (!string.IsNullOrEmpty(text)) BossGimmickSystem.Instance?.ShowPatternTelegraph(text, sec);
        ApplyTint(Color.Lerp(baseTint, Color.white, 0.6f));
        telegraphing = true;
        CastBegin(frostDisc);
        float t = 0f;
        while (t < sec)
        {
            t += Time.deltaTime;
            if (isGroggy || !IsAlive) break;
            TickCastFx();
            if (perFrame != null) perFrame();
            yield return null;
        }
        telegraphing = false;
        ApplyTint(RestTint());
        if (isGroggy || !IsAlive) CastCancel();
    }

    /// <summary>쉬는 색 - 갑주(얼음빛) / 해치가 열렸으면 그 빛 / 아니면 기본 (예고가 끝날 때 돌아가는 색)</summary>
    private Color RestTint()
    {
        if (armorActive) return ArmorTint();
        if (hatchOpen) return Color.Lerp(baseTint, Color.white, 0.35f);
        return baseTint;
    }

    /// <summary>B2: 기차 HP 바에 맞을 양만큼 빗금 (raw = 들어갈 피해, 방어력 어림은 TrainManager.EstimateDamage). raw 0 = 걷는다</summary>
    private void PreviewTrainDamage(float raw, bool ignoreDef, float sec) { PreviewTrainDamage(raw, 1, ignoreDef, sec); }

    /// <summary>hits 번 맞는 공격 (도약) - 방어력은 한 번마다 빠지니 한 번 어림 x 횟수</summary>
    private void PreviewTrainDamage(float rawEach, int hits, bool ignoreDef, float sec)
    {
        if (!GameBalance.DamagePreviewOn || UIManager.Instance == null) return;
        float est = (rawEach > 0f && hits > 0 && trainManager != null) ? trainManager.EstimateDamage(rawEach, ignoreDef) * hits : 0f;
        UIManager.Instance.PreviewTrainDamage(est, sec);
    }

    private float TrainY { get { return trainTarget != null ? trainTarget.position.y : 0f; } }
    private static float CarMid(int car)
    {
        float[] e = GameBalance.CarEdgesX;
        car = Mathf.Clamp(car, 0, e.Length - 2);
        return (e[car] + e[car + 1]) * 0.5f;
    }
    private static float Frac(float[] arr, int i, float fallback) { return (arr != null && i >= 0 && i < arr.Length) ? arr[i] : fallback; }

    /// <summary>몸 위의 자리: 그림 불투명 부분의 왼쪽 끝에서 frac 만큼 (그림 좌표 -> 세계). 보스 그림은 오른쪽을 보고 그려져 있다 - 서 있을 때(180도) 그림 왼쪽 = 꼬리</summary>
    private Vector3 BodyPointAt(float frac)
    {
        if (skin == null || skin.sprite == null || bodyExtLocal.x <= 0.01f) return transform.position;
        float lx = bodyCenterLocal.x - bodyExtLocal.x + Mathf.Clamp01(frac) * bodyExtLocal.x * 2f;
        Vector3 p = skin.transform.TransformPoint(new Vector3(lx, bodyCenterLocal.y, 0f));
        p.z = 0f;
        return p;
    }

    /// <summary>칸마다 가동 중인 포탑 수 (칸 0 ~ 3)</summary>
    private static int[] TurretsPerCar()
    {
        int[] n = new int[GameBalance.CarEdgesX.Length - 1];
        if (TurretSlotManager.Instance == null) return n;
        TurretSlot[] slots = TurretSlotManager.Instance.slots;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null || slots[i].IsEmpty || slots[i].isLocked) continue;
            int car = GameBalance.CarIndexOf(slots[i].transform.position.x);
            if (car >= 0 && car < n.Length) n[car]++;
        }
        return n;
    }

    /// <summary>보스가 쓰러졌다 - 남은 손님은 먼지를 끌며 달아나고(처치 골드는 준다) 알은 그냥 깨진다 (GameBalance.BossRoutOnKill)</summary>
    private void RoutMinions()
    {
        if (formationCo != null && waveManagerRef != null) { waveManagerRef.StopCoroutine(formationCo); formationCo = null; }
        if (!GameBalance.BossNewPatterns || !GameBalance.BossRoutOnKill || practice) return;
        Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        int n = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || all[i] == this || all[i] is BossEnemy) continue;
            BossEgg egg = all[i] as BossEgg;
            if (egg != null) { egg.Crack(false); continue; }
            if (!all[i].IsAlive) continue;
            all[i].Rout();
            n++;
        }
        if (n > 0) Debug.Log("[BossEnemy] 보스가 쓰러져 남은 손님 " + n + "마리가 달아났다");
    }

    /// <summary>쓰러질 때·사라질 때 v9.23 의 남은 것 정리 (서리·눈사태 벽·빗금·북쪽 막기)</summary>
    private void CleanupNewPatterns()
    {
        if (frostOwner == this) MeltFrost(false);
        if (avalanche != null) { avalanche.Break(false); avalanche = null; }
        if (northBlockedBy == this) northBlockedBy = null;
        PreviewTrainDamage(0f, false, 0f);
        heatGlow = 0f; wingFold = 0f; poseScale = Vector2.one;
    }

    // ─────────────────────────────────────────────
    // 녹슨 발톱
    // ─────────────────────────────────────────────
    /// <summary>R1: 등장 전조 때 부관 발톱이 화면 좌우 끝에서 달려 들어온다 (처음 RustLieutenantDashSec 는 빠르게 - 왕보다 먼저 붙는다)</summary>
    private void SpawnLieutenants()
    {
        if (lieutenantsSpawned || waveManagerRef == null || GameBalance.RustLieutenantCount <= 0) return;
        Camera cam = Camera.main;
        float hw = cam != null ? cam.orthographicSize * cam.aspect : 17f;
        float cx = cam != null ? cam.transform.position.x : standPos.x;
        int n = GameBalance.RustLieutenantCount;
        Vector3[] pts = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float side = (i % 2 == 0) ? -1f : 1f;   // 머리 쪽(왼쪽) · 꼬리 쪽(오른쪽) 번갈아
            int row = i / 2;
            pts[i] = new Vector3(cx + side * (hw + 1.5f + row * 1.5f), TrainY + 2.4f + row * 1.2f, 0f);
        }
        // 공용 손님 표(Enemy.SteamRaptor)를 고치지 않게 새 표 - 이름이 "부관 발톱" 이라 그림·소리 규칙에 안 걸린다 (그림은 아래에서 직접 입힌다)
        Enemy.EnemyData d = new Enemy.EnemyData
        {
            enemyName = "부관 발톱", baseHP = 300f, baseATK = 40f, baseSPD = GameBalance.RustLieutenantSpeed,
            dropMaterialName = "랩터 고기", goldReward = 60, xpReward = 0, targetPriority = "기차 전체", specialAbility = "미끼에 끌린다"
        };
        List<Enemy> made = waveManagerRef.SpawnBossUnits("raptor", d, pts, 1f, 1f);
        if (made.Count == 0) return;   // 못 만들었다 (웨이브가 끝났거나 프리팹 없음) - 왕 HP 는 그대로
        lieutenantsSpawned = true;
        // 부관이 온 만큼 왕 HP 를 깎는다 (왕 RustKingHPMul + 부관 = 예전 왕 하나의 1.2배 안팎). 등장 연출 중이라 아직 한 대도 안 맞았다
        bossMaxHP *= Mathf.Clamp(GameBalance.RustKingHPMul, 0.1f, 1f);
        scaledMaxHP = bossMaxHP;
        currentHP = Mathf.Min(currentHP, bossMaxHP);
        float hp = Mathf.Max(1f, bossMaxHP * GameBalance.RustLieutenantHPRatio);
        float bite = scaledATK * GameBalance.BossMeleeMul * GameBalance.RustLieutenantBiteRatio;
        for (int i = 0; i < made.Count; i++)
        {
            Enemy e = made[i];
            e.currentHP = hp; e.scaledMaxHP = hp;
            e.scaledATK = bite;
            e.scaledSPD = GameBalance.RustLieutenantDashSpeed;
            e.attackCooldown = bossAttackCooldown;   // 왕과 같은 박자 (무는 힘 절반 x 둘 = 왕 하나만큼 - 랩터 박자면 왕보다 세다)
            e.defense = 15f; e.resistance = 15f;   // Start 의 이름 표가 이 값을 보고 건너뛴다 (그 뒤 전체 강화 배율만 곱한다)
            e.LuredByBait = true;
            e.OnDied = OnLieutenantDown;
            DressLieutenant(e);
            lieutenants.Add(e);
        }
        if (made.Count > 0) StartCoroutine(LieutenantPace());
        Debug.Log("[BossEnemy] 부관 발톱 " + made.Count + "마리 (HP " + (int)hp + " / 무는 힘 " + (int)bite + ")");
    }

    /// <summary>부관 그림: 왕 그림을 RustLieutenantLen 길이로 줄여 입힌다. EnemySkin 표지를 먼저 붙여 손님 그림 스캐너가 랩터 그림을 덮지 않게</summary>
    private void DressLieutenant(Enemy e)
    {
        Sprite s = SpriteBank.Get(string.IsNullOrEmpty(skinBase) ? "boss_rust" : skinBase);
        if (s == null) return;   // 왕 그림이 없으면 랩터 그림 그대로 (EnemySkin 이 입힌다)
        SpriteRenderer[] old = e.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < old.Length; i++) old[i].enabled = false;
        float rootScale = Mathf.Abs(e.transform.localScale.x);
        if (rootScale < 0.01f) rootScale = 1f;
        float k = GameBalance.RustLieutenantLen / Mathf.Max(0.05f, OpaqueSize(s).x) / rootScale;
        GameObject go = new GameObject("Skin");
        go.transform.SetParent(e.transform, false);
        go.transform.localScale = new Vector3(k, k, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = 5;                                  // 손님 층 (보스 6 아래)
        sr.color = new Color(1f, 0.86f, 0.78f);               // 왕보다 조금 바랜 색
        EnemySkin marker = e.gameObject.AddComponent<EnemySkin>();
        marker.skin = sr;
    }

    /// <summary>R1: 달려 들어오는 속도를 RustLieutenantDashSec 뒤에 평소 속도로</summary>
    private IEnumerator LieutenantPace()
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, GameBalance.RustLieutenantDashSec));
        for (int i = 0; i < lieutenants.Count; i++)
            if (lieutenants[i] != null && lieutenants[i].IsAlive && lieutenants[i].scaledSPD > 0.01f)
                lieutenants[i].scaledSPD = GameBalance.RustLieutenantSpeed;
    }

    /// <summary>R1: 부관이 쓰러졌다 - 왕이 고개를 쳐들고 울고, RustLieutenantHowlDelay 뒤 호령</summary>
    private void OnLieutenantDown(Enemy e)
    {
        lieutenants.Remove(e);
        if (!IsAlive || isServing || entering) return;
        forceHowlNext = true;
        if (!isCasting) patternTimer = Mathf.Min(patternTimer, Mathf.Max(0.3f, GameBalance.RustLieutenantHowlDelay));
        UIManager.Instance?.ShowStatChange("부관이 쓰러졌다. 녹슨 발톱이 무리를 부른다");
        StartCoroutine(MournLieutenant());
    }

    private IEnumerator MournLieutenant()
    {
        lookOffset = -14f;   // 서는 각 180 기준 - = 머리(왼쪽 끝)가 위로 (고개를 쳐든다)
        if (PatternFx)
        {
            int k = Mathf.Clamp((int)kind, 0, EMBER.Length - 1);
            WorldFeel.Ring(MouthPos(), Color.Lerp(EMBER[k], Color.white, 0.5f), FxRadius() * 0.9f + 0.8f, 0.45f);
        }
        SoundManager.Play(SoundKeys.BossRoar(kind.ToString()), 0.75f, -1f);
        yield return new WaitForSeconds(1.0f);
        lookOffset = 0f;
    }

    /// <summary>
    /// R2 삼단 도약: 예고(그림자 원 + 숫자, 북 -> 남 -> 북) -> 기차를 뛰어넘으며 착지 -> 그 칸 피해 + 크게 덜컹 + 먼지 -> 제자리.
    /// 미끼가 깔려 있으면 첫 착지는 미끼 자리 (기차 무피해). 발악 = 5단. 뛰는 동안 무방비가 되면 그 착지는 피해 없이 내려앉고, 무방비가 끝난 뒤 제자리로
    /// </summary>
    private IEnumerator PatternLeap()
    {
        int leaps = Mathf.Max(1, enraged ? GameBalance.RustLeapCountEnraged : GameBalance.RustLeapCount);
        float halfW = BodyHalfWidth();
        float offY = 1.8f + halfW * 0.65f;   // 지붕(1.8)에서 몸 폭 반의 65% 밖 - 몸 옆이 칸 옆구리에 걸친다
        List<Vector3> spots = new List<Vector3>();
        int lastN = -1, lastS = -1;
        for (int i = 0; i < leaps; i++)
        {
            bool north = (i % 2 == 0);
            int car;
            // 북쪽은 머리·꼬리 칸(0·3)을 번갈아 - 서 있는 왕 몸(가운데 위)과 덜 겹친다 / 남쪽은 가운데 두 칸(1·2)을 번갈아
            if (north) { car = lastN == 0 ? 3 : lastN == 3 ? 0 : (Random.value < 0.5f ? 0 : 3); lastN = car; }
            else { car = lastS == 1 ? 2 : lastS == 2 ? 1 : (Random.value < 0.5f ? 1 : 2); lastS = car; }
            spots.Add(new Vector3(CarMid(car) + Random.Range(-0.5f, 0.5f), TrainY + (north ? offY : -offY), 0f));
        }
        bool baitFirst = IsTaunted && CurrentTarget != null;
        if (baitFirst)
        {
            // 미끼 자리 (화면 밖이면 안으로 당긴다 - 큰 몸이 화면 밖으로 사라지지 않게)
            Vector3 b = CurrentTarget.position;
            Camera cam = Camera.main;
            if (cam != null)
            {
                float hw = cam.orthographicSize * cam.aspect - BodyHalfLength() * 0.5f, hh = cam.orthographicSize;
                b.x = Mathf.Clamp(b.x, cam.transform.position.x - hw, cam.transform.position.x + hw);
                b.y = Mathf.Clamp(b.y, TrainY - 4.2f, cam.transform.position.y + hh - 4f);
            }
            b.z = 0f;
            spots[0] = b;
        }

        float tele = Mathf.Max(0.3f, GameBalance.RustLeapTelegraphSec);
        float air = Mathf.Max(0.15f, GameBalance.RustLeapAirSec), rest = Mathf.Max(0f, GameBalance.RustLeapRestSec);
        float marksLife = tele + leaps * (air + rest) + 1.5f;
        List<PatternMark> marks = new List<PatternMark>();
        for (int i = 0; i < spots.Count; i++)
            marks.Add(PatternMark.Ellipse(spots[i], BodyHalfLength() * 0.72f, halfW * 0.5f, LEAP_COL, marksLife, (i + 1).ToString()));
        float dmgEach = scaledATK * GameBalance.BossMeleeMul * GameBalance.RustLeapDamageMul;
        PreviewTrainDamage(dmgEach, leaps - (baitFirst ? 1 : 0), false, tele + leaps * (air + rest));

        yield return StartCoroutine(TelegraphFor("녹슨 발톱이 뛰어오른다. 놈이 숫자 순서대로 덮쳐", tele, false, null));
        if (isGroggy || !IsAlive) { PatternMark.KillAll(marks); PreviewTrainDamage(0f, false, 0f); yield break; }

        CastRelease(false);   // 박차고 오른다 (몸이 눌렸다 편다 - 고리 없이)
        Vector3 from = transform.position;
        bool cut = false;
        for (int i = 0; i < spots.Count && IsAlive; i++)
        {
            Vector3 to = spots[i];
            float tilt = to.y < from.y ? 12f : -12f;   // 서는 각 180 기준: 내려갈 때 머리가 아래로 (+), 올라갈 때 위로 (-)
            SoundManager.Play("sfx_wing_gust", 0.45f, 0.08f);
            yield return StartCoroutine(LeapArc(from, to, air, tilt));
            if (!IsAlive) break;
            if (i < marks.Count && marks[i] != null) marks[i].Pop();
            if (isGroggy) { cut = true; break; }   // 무방비 - 이 착지는 피해 없이 주저앉는다
            LeapLand(to, baitFirst && i == 0, dmgEach);
            from = to;
            float t = 0f;
            while (t < rest && IsAlive && !isGroggy) { t += Time.deltaTime; yield return null; }
            if (isGroggy) { cut = true; break; }
        }
        PatternMark.KillAll(marks);
        PreviewTrainDamage(0f, false, 0f);
        if (!IsAlive) yield break;
        if (cut) while (IsAlive && isGroggy) yield return null;   // 무방비가 끝난 뒤에 제자리로 (그동안 주저앉은 자리에 있다)
        if (!IsAlive) yield break;
        // 제자리로 (작게 한 번 뛴다)
        yield return StartCoroutine(LeapArc(transform.position, standPos, 0.45f, transform.position.y < standPos.y ? -8f : 8f));
        transform.position = standPos;
        poseScale = Vector2.one;
    }

    /// <summary>R2: from -> to 포물선 한 번 (화면 위로 1.2u 솟고, 꼭대기에서 1.22배 - 가까워진 것처럼). 몸은 기차와 나란히 둔 채 tilt 만큼 기운다</summary>
    private IEnumerator LeapArc(Vector3 from, Vector3 to, float sec, float tilt)
    {
        GameObject sh = new GameObject("BossLeapShadow");   // 땅의 그림자 (날아가는 동안 따라간다)
        SpriteRenderer ssr = sh.AddComponent<SpriteRenderer>();
        ssr.sprite = AttackVFX.GetCircleSprite();
        ssr.color = new Color(0f, 0f, 0f, 0.3f);
        ssr.sortingOrder = -7;
        sh.transform.localScale = new Vector3(BodyHalfLength() * 1.3f, BodyHalfWidth() * 0.8f, 1f);
        float t = 0f;
        while (t < sec)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / sec);
            float arc = Mathf.Sin(k * Mathf.PI);
            Vector3 ground = Vector3.Lerp(from, to, k);
            transform.position = ground + Vector3.up * (arc * 1.2f);
            sh.transform.position = ground;
            poseScale = Vector2.one * (1f + 0.22f * arc);
            faceAngle = 180f + tilt * arc;
            transform.rotation = Quaternion.AngleAxis(faceAngle, Vector3.forward);
            yield return null;
            if (!IsAlive) break;
        }
        Destroy(sh);
        if (!IsAlive) yield break;
        transform.position = to;
        poseScale = Vector2.one;
        faceAngle = 180f;
        transform.rotation = Quaternion.AngleAxis(faceAngle, Vector3.forward);
    }

    /// <summary>R2 착지: 흔들림 + 쿵 + 먼지 고리. 기차 쪽이면 그 칸에 피해 + 크게 덜컹 + 발톱 자국 / 미끼면 미끼만 덮친다</summary>
    private void LeapLand(Vector3 pos, bool onBait, float dmg)
    {
        GameFeel.Shake(GameBalance.ShakeBoss * 0.55f);
        SoundManager.PlayAt("sfx_boss_land", pos);
        if (PatternFx)
        {
            WorldFeel.Ring(pos, DUST, FxRadius() * 1.1f, 0.35f);
            for (int k = 0; k < 4; k++) DustFX.Puff(pos + new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(-0.6f, 0.6f), 0f), Random.Range(1f, 1.4f));
        }
        if (onBait)
        {
            UIManager.Instance?.ShowStatChange("녹슨 발톱이 미끼를 덮쳤다");
            return;
        }
        float ty = TrainY;
        float side = pos.y >= ty ? 1f : -1f;
        Vector3 hull = new Vector3(pos.x, ty + side * 1.6f, 0f);   // 보스 쪽 옆면
        TrainFeel.NextHitX = pos.x;
        SoundManager.PlayAt(SoundKeys.Attack(data.enemyName), pos);
        trainManager?.TakeDamage(dmg);
        Vector2 dir = new Vector2(0f, -side);
        TrainDeck.KickCar(pos.x, dir, GameBalance.RustLeapCarKick);
        if (PatternFx)
        {
            SparkPool.Emit(hull, SPARK, 10, 0.09f, 5f);
            BossMark.Claw(hull, dir, GameBalance.BossBiteMarkSec);
        }
    }

    // ─────────────────────────────────────────────
    // 천둥 둥지
    // ─────────────────────────────────────────────
    /// <summary>T1: 날갯짓 (날개폭 = 그림의 세로 - 내려다볼 때 좌우) + ThunderDriftEvery 초마다 기차를 따라 자리를 옮긴다. T3 예고 동안은 날개를 접는다 (wingFold)</summary>
    private void TickHover()
    {
        if (!GameBalance.BossThunderHover) return;
        float flap = 0f;
        if (GameBalance.ThunderFlapOn && !isGroggy)
        {
            float ph = Time.time / Mathf.Max(0.2f, GameBalance.ThunderFlapSec);
            flap = 0.5f - 0.5f * Mathf.Cos(ph * Mathf.PI * 2f);   // 0 -> 1 -> 0
        }
        poseScale = new Vector2(1f, (1f - GameBalance.ThunderFlapDepth * flap) * (1f - 0.24f * wingFold));

        if (GameBalance.ThunderDriftEvery <= 0f || isCasting || isLunging || isGroggy || gliding || isServing) return;
        driftTimer += Time.deltaTime;
        if (driftTimer < GameBalance.ThunderDriftEvery) return;
        driftTimer = 0f;
        float dx = GameBalance.ThunderDriftDX;
        float cur = standPos.x - GameBalance.BossStandX;
        float pick = cur;
        for (int guard = 0; guard < 8 && Mathf.Abs(pick - cur) < 0.1f; guard++) pick = (Random.Range(0, 3) - 1) * dx;
        StartCoroutine(GlideTo(new Vector3(GameBalance.BossStandX + pick, standPos.y, 0f), GameBalance.ThunderDriftSec, 0));
    }

    /// <summary>
    /// 자리 옮기기 (T1 활공 / O1 앞뒤로): standPos 를 바꾸고 sec 에 걸쳐 미끄러진다 (처음과 끝이 느리다). 그동안 공격·패턴은 쉰다 (gliding).
    /// fx = 0 날개 바람 / 1 두 열차 사이 불티 + 바퀴 소리
    /// </summary>
    private IEnumerator GlideTo(Vector3 to, float sec, int fx)
    {
        gliding = true;
        Vector3 from = transform.position;
        standPos = to;
        if (fx == 0) SoundManager.Play("sfx_wing_gust", 0.55f, -1f);
        else SoundManager.Play("sfx_rival_wheels", 0.8f, -1f);
        float gapY = (TrainY + 1.9f + (to.y - BodyHalfWidth())) * 0.5f;
        float t = 0f, sparkT = 0f;
        sec = Mathf.Max(0.1f, sec);
        while (t < sec)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / sec);
            transform.position = Vector3.Lerp(from, to, k * k * (3f - 2f * k));
            if (fx == 1 && PatternFx)
            {
                sparkT -= Time.deltaTime;
                if (sparkT <= 0f)
                {
                    sparkT = 0.06f;
                    float sx = Mathf.Clamp(transform.position.x + Random.Range(-BodyHalfLength(), BodyHalfLength()), GameBalance.CarEdgesX[0], GameBalance.CarEdgesX[GameBalance.CarEdgesX.Length - 1]);
                    SparkPool.Emit(new Vector3(sx, gapY + Random.Range(-0.3f, 0.3f), 0f), EMBER[3], 3, 0.08f, 3.5f);
                }
            }
            yield return null;
            if (!IsAlive) { gliding = false; yield break; }
        }
        transform.position = to;
        gliding = false;
    }

    /// <summary>T2: 알 떨어질 자리 n 곳 - 칸 가운데의 북쪽(여왕 몸 밑은 빼고)·남쪽 중에서 섞어 고른다</summary>
    private List<Vector3> PickEggSpots(int n)
    {
        List<Vector3> cand = new List<Vector3>();
        int cars = GameBalance.CarEdgesX.Length - 1;
        for (int c = 0; c < cars; c++)
        {
            float x = CarMid(c);
            if (Mathf.Abs(x - transform.position.x) > BodyHalfWidth() + 0.8f) cand.Add(new Vector3(x, TrainY + 2.9f, 0f));
            cand.Add(new Vector3(x, TrainY - 3.1f, 0f));
        }
        for (int i = cand.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); Vector3 tmp = cand[i]; cand[i] = cand[j]; cand[j] = tmp; }
        List<Vector3> pick = new List<Vector3>();
        for (int i = 0; i < n; i++)
        {
            Vector3 p = cand[i % cand.Count];
            pick.Add(p + new Vector3(Random.Range(-0.7f, 0.7f) + (i >= cand.Count ? 1.2f : 0f), 0f, 0f));
        }
        return pick;
    }

    /// <summary>T2: 알 HP = 지금 놓인 포탑 한 발의 평균 x ThunderEggShots (ThunderEggMinHP 이상)</summary>
    private static float EggHP()
    {
        float sum = 0f; int n = 0;
        if (TurretSlotManager.Instance != null)
        {
            TurretSlot[] slots = TurretSlotManager.Instance.slots;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null || slots[i].IsEmpty || slots[i].isLocked) continue;
                float d = slots[i].ShotDamage;
                if (d > 0f) { sum += d; n++; }
            }
        }
        float avg = n > 0 ? sum / n : 0f;
        return Mathf.Max(GameBalance.ThunderEggMinHP, avg * GameBalance.ThunderEggShots);
    }

    /// <summary>T2 알 투하: 예고(바닥 고리가 조여 든다) -> 둥지에서 알이 포물선으로 날아가 박힌다 -> ThunderEggHatchSec 뒤 부화 (BossEgg). 발악 = 5개</summary>
    private IEnumerator PatternEggs()
    {
        int n = Mathf.Max(1, enraged ? GameBalance.ThunderEggCountEnraged : GameBalance.ThunderEggCount);
        List<Vector3> spots = PickEggSpots(n);
        float tele = Mathf.Max(0.3f, GameBalance.ThunderEggTelegraphSec);
        List<PatternMark> rings = new List<PatternMark>();
        for (int i = 0; i < spots.Count; i++) rings.Add(PatternMark.Ring(spots[i], 1.3f, 0.75f, EGG_COL, tele + 0.75f));
        SoundManager.Play("sfx_cue_ambush", 0.7f, 0.04f);
        yield return StartCoroutine(TelegraphFor("천둥 둥지가 알을 떨군다. 부화하기 전에 깨", tele, false, null));
        if (isGroggy || !IsAlive) { PatternMark.KillAll(rings); yield break; }
        CastRelease(false);
        float hp = EggHP();
        for (int i = 0; i < spots.Count; i++)
        {
            BossEgg.Launch(this, transform.position + new Vector3(Random.Range(-0.5f, 0.5f), 0.3f, 0f), spots[i], hp);
            SoundManager.Play("sfx_launch_mortar", 0.55f, 0.06f);
            float t = 0f;
            while (t < 0.12f) { t += Time.deltaTime; yield return null; }
            if (!IsAlive) yield break;
        }
        Debug.Log("[BossEnemy] 알 투하 " + spots.Count + "개 (알 HP " + (int)hp + ")");
    }

    /// <summary>T3 편대 발진 (발악 뒤): 날개를 접고 노릴 칸에 붉은 테두리 -> 등에서 과부하 플라이가 줄지어 쏟아져 그 칸으로 내리꽂힌다. 예고 중 마비·감전이면 절반</summary>
    private IEnumerator PatternFlight()
    {
        if (!enraged || !GameBalance.ThunderFlightOn) { yield return StartCoroutine(PatternLightning()); yield break; }
        // 포탑이 가장 많은 칸 (같으면 무작위)
        int[] per = TurretsPerCar();
        int best = -1, bestN = -1;
        for (int c = 0; c < per.Length; c++)
        {
            int score = per[c] * 10 + Random.Range(0, 5);
            if (score > bestN) { bestN = score; best = c; }
        }
        int car = Mathf.Clamp(best, 0, per.Length - 1);
        float tele = Mathf.Max(0.3f, GameBalance.ThunderFlightTelegraphSec);
        float castStart = Time.time;
        PatternMark frame = PatternMark.CarFrame(car, FLIGHT_COL, tele + 2.2f, 0.1f);
        yield return StartCoroutine(TelegraphFor("천둥 둥지가 편대를 내보낸다. 지금 마비시키면 절반", tele, false,
            delegate { wingFold = Mathf.MoveTowards(wingFold, 1f, Time.deltaTime * 4f); }));
        if (isGroggy || !IsAlive) { wingFold = 0f; if (frame != null) frame.Kill(0.2f); yield break; }
        int count = Mathf.Max(1, GameBalance.ThunderFlightCount);
        if (LastStunTime >= castStart)
        {
            count = Mathf.Max(1, count / 2);
            UIManager.Instance?.ShowStatChange("편대를 끊었다. 플라이가 절반만 와");
        }
        CastRelease(false);
        wingFold = 0f;
        float aimX = CarMid(car);
        for (int i = 0; i < count && IsAlive; i++)
        {
            Vector3 p = transform.position + new Vector3(Random.Range(-0.7f, 0.7f), Random.Range(-0.2f, 0.4f), 0f);
            if (waveManagerRef != null)
            {
                List<Enemy> made = waveManagerRef.SpawnBossUnits("fly", null, new Vector3[] { p }, 1f, GameBalance.BossSummonShare);
                for (int j = 0; j < made.Count; j++) made[j].AimX = aimX + Random.Range(-1.4f, 1.4f);
            }
            if (PatternFx) SparkPool.Emit(p, EMBER[1], 3, 0.07f, 3f);
            float t = 0f;
            while (t < 0.12f) { t += Time.deltaTime; yield return null; }
        }
        Debug.Log("[BossEnemy] 편대 발진 - 플라이 " + count + "마리 -> 칸 " + car);
    }

    // ─────────────────────────────────────────────
    // 동면자
    // ─────────────────────────────────────────────
    /// <summary>
    /// H1 꼬리 후려치기 (돌진 물기 대신): 예고(꼬리 쪽 칸 테두리 + 꼬리를 살짝 젖힌다) -> 머리를 축으로 HibTailDeg 틀어 꼬리 끝이 그 칸을 친다 -> 덜컹 -> 제자리.
    /// 피해 = 무는 힘 그대로 (AttackTrain). 미끼에 끌려 있으면 치지 않는다
    /// </summary>
    private IEnumerator AttackTail()
    {
        isLunging = true;
        float started = Time.time;
        float L = BodyHalfLength();
        Vector3 startPos = transform.position;
        float baseAng = StandAngle();
        float r0 = baseAng * Mathf.Deg2Rad;
        Vector3 head = startPos + new Vector3(Mathf.Cos(r0), Mathf.Sin(r0), 0f) * L;
        Vector3 tip = startPos - new Vector3(Mathf.Cos(r0), Mathf.Sin(r0), 0f) * L;
        int car = GameBalance.CarIndexOf(tip.x);
        float tele = Mathf.Max(0.1f, GameBalance.HibTailTelegraphSec);
        PatternMark frame = PatternMark.CarFrame(car, TAIL_COL, tele + 0.35f, 0.08f);
        float t = 0f;
        while (t < tele)
        {
            t += Time.deltaTime;
            if (isGroggy || !IsAlive) break;
            SetSwing(head, L, baseAng, 2.5f * Mathf.Sin(Mathf.Clamp01(t / tele) * Mathf.PI * 0.5f));   // 젖힌다 (꼬리가 위로)
            yield return null;
        }
        if (isGroggy || !IsAlive)
        {
            if (frame != null) frame.Kill(0.15f);
            if (IsAlive) SetSwing(head, L, baseAng, 0f);
            isLunging = false;
            yield break;
        }
        float deg = Mathf.Max(0f, GameBalance.HibTailDeg);
        t = 0f;
        while (t < 0.12f) { t += Time.deltaTime; SetSwing(head, L, baseAng, Mathf.Lerp(2.5f, -deg, Mathf.Clamp01(t / 0.12f))); yield return null; }
        if (IsAlive)
        {
            float hitX = CarMid(car);
            if (!IsTaunted)
            {
                TrainFeel.NextHitX = hitX;
                AttackTrain();
                TrainDeck.KickCar(hitX, Vector2.down, 0.3f);
                if (PatternFx)
                {
                    Vector3 roof = new Vector3(hitX, TrainY + 1.6f, 0f);
                    SparkPool.Emit(roof, ICE_LINE, 10, 0.09f, 5f);
                    WorldFeel.Ring(roof, new Color(0.85f, 0.95f, 1f, 0.8f), 1.6f, 0.3f);
                }
            }
            GameFeel.Shake(GameBalance.ShakeBoss * 0.5f);
            if (frame != null) frame.Pop();
        }
        t = 0f;
        while (t < 0.08f) { t += Time.deltaTime; yield return null; }
        t = 0f;
        while (t < 0.3f && IsAlive) { t += Time.deltaTime; SetSwing(head, L, baseAng, Mathf.Lerp(-deg, 0f, Mathf.Clamp01(t / 0.3f))); yield return null; }
        if (IsAlive) { SetSwing(head, L, baseAng, 0f); transform.position = startPos; }
        isLunging = false;
        attackTimer = 0f;
        patternTimer -= Time.time - started;   // 치는 동안 멈춰 있던 패턴 시계를 당긴다 (물기와 같은 규칙)
    }

    /// <summary>H1: 머리를 축으로 delta 도 돌린 자세 (몸 가운데 = 머리 - 오른쪽 x 몸 반 길이)</summary>
    private void SetSwing(Vector3 head, float L, float baseAng, float delta)
    {
        float a = baseAng + delta;
        float r = a * Mathf.Deg2Rad;
        Vector3 right = new Vector3(Mathf.Cos(r), Mathf.Sin(r), 0f);
        transform.position = head - right * L;
        transform.rotation = Quaternion.AngleAxis(a, Vector3.forward);
        faceAngle = a;
    }

    /// <summary>
    /// H2 백야의 입김: 예고(머리를 기차로 숙이고 덮일 칸 2개에 서리 테두리) -> 입에서 서리 줄기 -> HibBreathSec 동안 그 칸 포탑 발사 x HibBreathFireMul + 조리 미니게임 x HibBreathCookMul.
    /// 덮이는 칸 = 주방 + 포탑 A, 또는 포탑 A + 포탑 B (반반). 굽기 요리 하나를 완성하면 바로 풀린다 (OnGrillCooked)
    /// </summary>
    private IEnumerator PatternFrostBreath()
    {
        int a = Random.value < 0.5f ? 1 : 2;
        int b = a + 1;
        float tele = Mathf.Max(0.3f, GameBalance.HibBreathTelegraphSec);
        PatternMark fa = PatternMark.CarFrame(a, FROST_COL, tele + 0.2f, 0.12f);
        PatternMark fb = PatternMark.CarFrame(b, FROST_COL, tele + 0.2f, 0.12f);
        float want = StandAngle() + 10f;   // 머리를 기차 쪽으로 숙인다 (서는 각 180 기준 + = 머리가 아래)
        yield return StartCoroutine(TelegraphFor("동면자가 서리를 뿜는다. 굽기 요리 하나면 녹아", tele, false, delegate { TurnBody(want); }));
        if (isGroggy || !IsAlive) { if (fa != null) fa.Kill(0.2f); if (fb != null) fb.Kill(0.2f); yield break; }
        CastRelease(false);
        Vector3 mouth = MouthPos();
        if (PatternFx)
        {
            for (int c = a; c <= b; c++)
                for (int i = 0; i < 6; i++)
                {
                    Vector3 target = new Vector3(CarMid(c) + Random.Range(-1.6f, 1.6f), TrainY + Random.Range(-1f, 1.2f), 0f);
                    Vector3 d = target - mouth;
                    BossMark.Streak(mouth, d, d.magnitude, 0.3f + i * 0.03f, new Color(0.85f, 0.97f, 1f, 0.8f));
                }
        }
        SoundManager.Play("sfx_freeze", 1f, -1f);
        GameFeel.Shake(GameBalance.ShakeBoss * 0.35f);
        ApplyFrost(a, b);
    }

    /// <summary>H2: 서리를 건다 - 두 칸 위에 서리 막(그동안 남는다) + 위 가장자리 서리 + 알림 한 줄</summary>
    private void ApplyFrost(int carA, int carB)
    {
        if (frostOwner != null && frostOwner != this) frostOwner.MeltFrost(false);
        frostMask = (1 << carA) | (1 << carB);
        frostUntil = Time.time + Mathf.Max(0.5f, GameBalance.HibBreathSec);
        frostOwner = this;
        if (frostMarkA != null) frostMarkA.Kill(0.1f);
        if (frostMarkB != null) frostMarkB.Kill(0.1f);
        frostMarkA = PatternMark.CarFrame(carA, FROST_COL, GameBalance.HibBreathSec + 0.5f, 0.3f);
        frostMarkB = PatternMark.CarFrame(carB, FROST_COL, GameBalance.HibBreathSec + 0.5f, 0.3f);
        WarningFX.FrostEdge(1.6f, new Color(0.8f, 0.94f, 1f));
        int[] per = TurretsPerCar();
        int frozen = (carA < per.Length ? per[carA] : 0) + (carB < per.Length ? per[carB] : 0);
        UIManager.Instance?.ShowStatChange(frozen > 0
            ? "포탑 " + frozen + "개가 얼어 느려졌다. 조리 눈금도 빨라져"
            : "서리가 덮였다. 조리 눈금이 빨라져");
        UIManager.Instance?.ShowStatChange("굽기 요리 하나면 서리가 녹아");
        StartCoroutine(FrostWatch());
    }

    private IEnumerator FrostWatch()
    {
        while (frostOwner == this && frostMask != 0 && Time.time < frostUntil) yield return null;
        if (frostOwner == this && frostMask != 0) MeltFrost(false);
    }

    /// <summary>H2: 서리가 풀린다. byGrill = 굽기 요리를 완성해서 (김이 오르고 알림 한 줄) / false = 시간이 다 됐다·보스가 쓰러졌다</summary>
    private void MeltFrost(bool byGrill)
    {
        if (frostMask == 0 && frostMarkA == null && frostMarkB == null) return;
        int mask = frostMask;
        frostMask = 0; frostUntil = -1f;
        if (frostOwner == this) frostOwner = null;
        if (frostMarkA != null) { frostMarkA.Kill(0.4f); frostMarkA = null; }
        if (frostMarkB != null) { frostMarkB.Kill(0.4f); frostMarkB = null; }
        if (!byGrill) return;
        SoundManager.Play("sfx_cool", 0.9f, -1f);
        if (PatternFx)
        {
            if (statePuffs == null) statePuffs = StatePuffs.Create(this);
            for (int c = 0; c < GameBalance.CarEdgesX.Length - 1; c++)
                if (((mask >> c) & 1) != 0)
                    for (int i = 0; i < 3; i++)
                        statePuffs.Emit(new Vector3(CarMid(c) + Random.Range(-1.5f, 1.5f), TrainY + Random.Range(-1f, 1f), 0f), STEAM[2], 0.9f, 0.8f, new Vector3(0f, 1.2f, 0f));
        }
        UIManager.Instance?.ShowStatChange("굽는 불에 서리가 녹았다");
    }

    /// <summary>
    /// H3 눈사태: HP 30% 밑에서 한 번. HibAvalancheSec 동안 화면 위에서 흰 벽이 내려온다 (벽 = 남은 시간). 지붕에 닿으면 기차 최대 HP x HibAvalancheDamageRatio (방어 무시).
    /// [F] 독샘 요리(BossGimmickSystem)나 해동포 GOOD 이상(HitByThawCannon)이면 벽이 깨지고 무방비 HibAvalancheGroggySec. 무방비가 와도 벽은 계속 내려온다 (던지면 멈춘다)
    /// </summary>
    private IEnumerator PatternAvalanche()
    {
        avalancheUsed = true;
        avalancheCancelled = false;
        avalancheCancelBy = "";
        float sec = Mathf.Max(3f, GameBalance.HibAvalancheSec);
        float raw = (trainManager != null ? trainManager.currentMaxHP : 1000f) * Mathf.Clamp01(GameBalance.HibAvalancheDamageRatio);
        BossGimmickSystem.Instance?.ShowPatternTelegraph("눈사태가 내려온다. [F] 독샘 요리나 해동포로 멈춰", sec);
        SoundManager.PlayIn(SfxBus.Warning, SoundKeys.BossRoar(kind.ToString()), 1f);   // v1.1 (v9.33): 눈사태 예고 - 울음이 경고 역할 (경고 버스로 한 번만)
        SoundManager.PlayDelayed("sfx_ground_crack", 0.5f);
        if (PatternFx) WorldFeel.Ring(MouthPos(), new Color(0.9f, 0.97f, 1f, 0.8f), FxRadius() * 1.4f, 0.5f);
        avalanche = AvalancheWall.Create(TrainY + 1.9f);
        PreviewTrainDamage(raw, true, sec);
        float t = 0f, rumble = 0f;
        while (t < sec)
        {
            t += Time.deltaTime;
            if (!IsAlive || avalancheCancelled) break;
            if (avalanche != null) avalanche.SetProgress(t / sec);
            rumble -= Time.deltaTime;
            if (sec - t < 3f && rumble <= 0f)
            {
                rumble = 0.5f;
                GameFeel.Shake(GameBalance.ShakeBoss * 0.15f);
                SoundManager.Play("sfx_iron_groan", 0.35f, 0.1f);
            }
            yield return null;
        }
        PreviewTrainDamage(0f, false, 0f);
        if (!IsAlive) { if (avalanche != null) avalanche.Break(false); avalanche = null; yield break; }
        if (avalancheCancelled)
        {
            // 던진 요리가 날아가는 동안(0.35초) 벽은 그 자리에 멈춰 있다가 맞는 순간 깨진다. 해동포는 맞은 순간 바로
            if (avalancheCancelBy == "food") { float w = 0f; while (w < 0.35f && IsAlive) { w += Time.deltaTime; yield return null; } }
            if (avalanche != null) avalanche.Break(true);
            avalanche = null;
            GameFeel.Shake(GameBalance.ShakeBoss * 0.5f);
            SoundManager.Play("sfx_break", 1f, -1f);
            if (PatternFx) ArmorShard.Burst(transform.position, new Color(0.92f, 0.97f, 1f), SHARD_B[2], 14);
            string by = avalancheCancelBy == "cannon" ? "해동포가 벽을 깨뜨렸다. 동면자가 무방비" : "독샘 요리가 벽을 멈췄다. 동면자가 무방비";
            BossNoticeAtGroggy("[눈사태]", by);
            ForceGroggy(Mathf.Max(0.5f, GameBalance.HibAvalancheGroggySec));
            yield break;
        }
        // 지붕에 닿았다
        if (avalanche != null) avalanche.Crash();
        avalanche = null;
        GameFeel.Hitstop(0.08f);
        GameFeel.Shake(GameBalance.ShakeBoss);
        SoundManager.PlayIn(SfxBus.Boss, "sfx_explosion", 0.9f);   // v1.1: 눈사태가 지붕에 닿는 폭음 = 보스 버스
        SoundManager.Play("sfx_freeze", 1f, -1f);
        trainManager?.TakeDamageIgnoringDef(raw);
        int cars = GameBalance.CarEdgesX.Length - 1;
        for (int c = 0; c < cars; c++) TrainDeck.KickCar(CarMid(c), Vector2.down, 0.32f);
        UIManager.Instance?.ShowStatChange("눈사태가 기차를 덮쳤다");
        Debug.Log("[BossEnemy] 눈사태 - 기차 피해 " + (int)raw);
    }

    // ─────────────────────────────────────────────
    // 디 오리지널
    // ─────────────────────────────────────────────
    private float WeakFrac() { return Frac(GameBalance.OrigWeakFrac, Mathf.Clamp(originalPhase - 1, 0, 2), 0.5f); }

    /// <summary>O1 + O2: 페이즈가 바뀌었다 (CheckOriginalPhases) - 평행 선로에서 앞(P2)·뒤(P3)로 옮기고, 새 약점 칸에 조준 고리</summary>
    private void OnOriginalPhase(int phase)
    {
        if (!NewPatterns) return;
        if (GameBalance.OrigShiftOn) StartCoroutine(ShiftAlong(phase == 2 ? -GameBalance.OrigShiftDX : GameBalance.OrigShiftDX));
        else if (GameBalance.OrigWeakPointOn) StartCoroutine(WeakMarkAfter(0.3f));
    }

    private IEnumerator ShiftAlong(float dx)
    {
        while (IsAlive && (isCasting || isLunging || gliding)) yield return null;   // 패턴·충돌이 끝나기를 기다린다
        if (!IsAlive) yield break;
        yield return StartCoroutine(GlideTo(new Vector3(GameBalance.BossStandX + dx, standPos.y, 0f), GameBalance.OrigShiftSec, 1));
        if (IsAlive && GameBalance.OrigWeakPointOn) yield return StartCoroutine(WeakMarkAfter(0f));
    }

    /// <summary>O2: 약점 칸에 조준 고리 2초 (포탑 탄이 이쪽으로 모인다는 것을 보여 준다)</summary>
    private IEnumerator WeakMarkAfter(float wait)
    {
        if (wait > 0f) yield return new WaitForSeconds(wait);
        if (!IsAlive || !PatternFx) yield break;
        PatternMark.Ring(BodyPointAt(WeakFrac()), 1.4f, 0.9f, WEAK_COL, 2f).FollowBody(this, WeakFrac());
    }

    /// <summary>O3 하차 (포효 대신): 예고(화물칸 문 3개가 붉게 열린다) -> 칸마다 랩터가 내 기차 쪽으로 뛰어내린다 (바로 옆이라 힘 x OrigUnloadStatMul)</summary>
    private IEnumerator PatternUnload()
    {
        float tele = Mathf.Max(0.3f, GameBalance.OrigUnloadTelegraphSec);
        float[] doors = GameBalance.OrigDoorFrac;
        int nd = doors != null ? doors.Length : 0;
        List<PatternMark> marks = new List<PatternMark>();
        float doorY = transform.position.y - BodyHalfWidth() * 0.75f;   // 화물칸의 내 기차 쪽 옆면
        for (int i = 0; i < nd; i++)
        {
            Vector3 p = BodyPointAt(doors[i]);
            marks.Add(PatternMark.Box(new Vector3(p.x, doorY, 0f), new Vector2(1.9f, 0.55f), DOOR_COL, tele + 0.5f, 0.35f));
        }
        SoundManager.Play("sfx_iron_groan", 0.9f, -1f);
        yield return StartCoroutine(TelegraphFor("화물칸 문이 열린다. 손님이 뛰어내려", tele, false, null));
        if (isGroggy || !IsAlive) { PatternMark.KillAll(marks); yield break; }
        CastRelease(false);
        int per = Mathf.Max(1, enraged ? GameBalance.OrigUnloadPerCarEnraged : GameBalance.OrigUnloadPerCar);
        int total = 0;
        for (int i = 0; i < nd && IsAlive; i++)
        {
            Vector3 door = BodyPointAt(doors[i]);
            door.y = doorY;
            Vector3[] pts = new Vector3[per];
            for (int j = 0; j < per; j++) pts[j] = door + new Vector3((j - (per - 1) * 0.5f) * 0.55f, Random.Range(-0.35f, 0f), 0f);
            if (waveManagerRef != null) total += waveManagerRef.SpawnBossUnits("raptor", null, pts, GameBalance.OrigUnloadStatMul, GameBalance.BossSummonShare).Count;
            if (PatternFx)
            {
                DustFX.Puff(door, 1.2f);
                SparkPool.Emit(door, EMBER[3], 5, 0.08f, 3.5f);
            }
            SoundManager.PlayAt("sfx_boss_land", door);
            if (i < marks.Count && marks[i] != null) marks[i].Pop();
            float t = 0f;
            while (t < 0.15f) { t += Time.deltaTime; yield return null; }
        }
        Debug.Log("[BossEnemy] 하차 - 랩터 " + total + "마리");
    }

    /// <summary>
    /// O4 측면 충돌 (돌진 물기 대신): 예고(차체가 붉게 달아오르고 두 열차 사이에 불티) -> OrigRamPush u 붙었다 떨어진다 -> 피해(무는 힘 x OrigRamDamageMul) + 큰 흔들림 + 내 칸이 머리부터 차례로 덜컹.
    /// 미끼에 끌려 있으면 붙지 않는다 (기차 무피해)
    /// </summary>
    private IEnumerator AttackRam()
    {
        isLunging = true;
        float started = Time.time;
        Vector3 startPos = transform.position;
        float dmg = scaledATK * GameBalance.BossMeleeMul * GameBalance.OrigRamDamageMul;
        float tele = Mathf.Max(0.1f, GameBalance.OrigRamTelegraphSec);
        PreviewTrainDamage(dmg, false, tele + 0.2f);
        SoundManager.Play("sfx_iron_groan", 0.7f, 0.05f);
        float gapY = (TrainY + 1.9f + (startPos.y - BodyHalfWidth())) * 0.5f;
        float left = Mathf.Max(GameBalance.CarEdgesX[0], startPos.x - BodyHalfLength());
        float right = Mathf.Min(GameBalance.CarEdgesX[GameBalance.CarEdgesX.Length - 1], startPos.x + BodyHalfLength());
        float t = 0f, sparkT = 0f;
        while (t < tele)
        {
            t += Time.deltaTime;
            if (isGroggy || !IsAlive) break;
            heatGlow = Mathf.Clamp01(t / tele);
            sparkT -= Time.deltaTime;
            if (sparkT <= 0f && PatternFx)
            {
                sparkT = 0.05f;
                SparkPool.Emit(new Vector3(Random.Range(left, right), gapY + Random.Range(-0.25f, 0.25f), 0f), EMBER[3], 2, 0.08f, 3f);
            }
            yield return null;
        }
        if (isGroggy || !IsAlive || IsTaunted)
        {
            heatGlow = 0f;
            PreviewTrainDamage(0f, false, 0f);
            isLunging = false;
            attackTimer = 0f;
            yield break;
        }
        Vector3 hitPos = startPos + Vector3.down * Mathf.Max(0f, GameBalance.OrigRamPush);
        t = 0f;
        while (t < 0.12f) { t += Time.deltaTime; transform.position = Vector3.Lerp(startPos, hitPos, Mathf.Clamp01(t / 0.12f)); yield return null; }
        if (IsAlive)
        {
            float keep = scaledATK;
            scaledATK = keep * GameBalance.OrigRamDamageMul;   // AttackTrain 이 BossMeleeMul 을 곱한다
            AttackTrain();
            scaledATK = keep;
            GameFeel.Shake(GameBalance.ShakeBoss * 0.9f);
            SoundManager.Play("sfx_iron_scream", 0.6f, 0.05f);
            StartCoroutine(KickCarsInOrder());
            if (PatternFx)
                for (int i = 0; i < 6; i++)
                    SparkPool.Emit(new Vector3(Random.Range(left, right), gapY, 0f), SPARK, 6, 0.09f, 5f);
        }
        PreviewTrainDamage(0f, false, 0f);
        t = 0f;
        while (t < 0.35f && IsAlive) { t += Time.deltaTime; transform.position = Vector3.Lerp(hitPos, startPos, Mathf.Clamp01(t / 0.35f)); yield return null; }
        if (IsAlive) transform.position = startPos;
        isLunging = false;
        attackTimer = 0f;
        patternTimer -= Time.time - started;   // 부딪는 동안 멈춰 있던 패턴 시계를 당긴다
    }

    /// <summary>O4: 내 칸이 머리부터 0.06초 간격으로 덜컹 (충격이 기차를 타고 지나간다)</summary>
    private IEnumerator KickCarsInOrder()
    {
        int cars = GameBalance.CarEdgesX.Length - 1;
        for (int c = 0; c < cars; c++)
        {
            TrainDeck.KickCar(CarMid(c), Vector2.down, 0.26f);
            yield return new WaitForSeconds(0.06f);
        }
    }

    /// <summary>
    /// O6: 쓰러질 때 - 꼬리부터 세 덩이가 OrigCollapseGap 간격으로 터지며 떨어져 나가 뒤로 처지고, 머리만 남아 눈의 불이 꺼진다 -> Die 의 원래 순서.
    /// 그동안 표적이 아니다 (isAlive false - 포탑이 멈춘다). 덩이는 지금 그림(상태 그림 포함)을 잘라 쓴다 (GameBalance.OrigCutFrac = 칸 사이 연결부)
    /// </summary>
    private IEnumerator CollapseThenDie()
    {
        collapsing = true;
        isAlive = false;
        if (glow != null) glow.enabled = false;
        CastCancel();
        CleanupNewPatterns();
        RoutMinions();
        PickupFX.FeedingBoss = null;
        Sprite full = skin != null ? skin.sprite : null;
        float[] cuts = GameBalance.OrigCutFrac;
        float gap = Mathf.Max(0.05f, GameBalance.OrigCollapseGap);
        if (full != null && cuts != null)
        {
            // 이음매 자리는 먼저 다 잰다 - 덩이를 떼면 몸 그림의 자리(localPosition)가 옮겨져 BodyPointAt 이 어긋난다
            float[] at = new float[cuts.Length];
            Vector3[] joints = new Vector3[cuts.Length];
            float p0 = 0f;
            for (int i = 0; i < cuts.Length; i++) { at[i] = Mathf.Clamp(cuts[i], p0 + 0.02f, 0.98f); joints[i] = BodyPointAt(at[i]); p0 = at[i]; }
            float prev = 0f;
            for (int i = 0; i < cuts.Length; i++)
            {
                float cut = at[i];
                Vector3 joint = joints[i];
                DetachPiece(full, prev, cut, i);
                ArmorShard.Burst(joint, SHARD_A[3], SHARD_B[3], 9);
                WorldFeel.Ring(joint, EMBER[3], 2.2f, 0.3f);
                SparkPool.Emit(joint, EMBER[3], 10, 0.09f, 5f);
                GameFeel.Shake(GameBalance.ShakeBoss * 0.45f);
                SoundManager.PlayIn(SfxBus.Boss, "sfx_explosion", 0.7f);   // v1.1: 칸이 떨어져 나가는 폭음 = 보스 버스
                prev = cut;
                float t = 0f;
                while (t < gap) { t += Time.deltaTime; yield return null; }
            }
        }
        // 눈의 불이 꺼진다 - 남은 머리가 식는다
        if (skin != null)
        {
            Color from = skin.color, to = new Color(0.38f, 0.35f, 0.37f, from.a);
            float t = 0f;
            while (t < 0.18f) { t += Time.deltaTime; skin.color = Color.Lerp(from, to, Mathf.Clamp01(t / 0.18f)); yield return null; }
        }
        collapseDone = true;
        Die();
    }

    /// <summary>
    /// O6: 지금 그림의 [a, b] 몫(왼쪽 = 꼬리)을 떼어 따로 그린 덩이가 뒤로 처지고, 몸 그림은 [b, 1] 만 남긴다.
    /// 새 그림의 피벗은 제 가운데로 두고 자리를 옮겨 맞춘다 (픽셀이 그 자리에 그대로 남는다)
    /// </summary>
    private void DetachPiece(Sprite full, float a, float b, int index)
    {
        if (skin == null || full == null || full.texture == null) return;
        Rect r = full.rect;
        float ppu = Mathf.Max(1f, full.pixelsPerUnit);
        Vector4 o = OpaqueRect(full);                                  // (가로 u, 세로 u, 가운데 x u, 가운데 y u)
        float leftPx = (o.z - o.x * 0.5f) * ppu + full.pivot.x;        // 불투명 부분 왼쪽 끝 (그림 rect 안 px)
        float widthPx = o.x * ppu;
        float x0 = Mathf.Clamp(Mathf.Round(leftPx + widthPx * a), 0f, r.width - 1f);
        float x1 = Mathf.Clamp(Mathf.Round(leftPx + widthPx * b), x0 + 1f, r.width);
        if (a <= 0.001f) x0 = 0f;                                      // 맨 끝 덩이는 그림 끝까지

        // 떨어지는 덩이
        Sprite piece = Sprite.Create(full.texture, new Rect(r.x + x0, r.y, x1 - x0, r.height), new Vector2(0.5f, 0.5f), ppu);
        Vector3 localCenter = new Vector3(((x0 + x1) * 0.5f - full.pivot.x) / ppu, (r.height * 0.5f - full.pivot.y) / ppu, 0f);
        // 자리는 처음 그림 기준 (몸 그림이 옮겨진 뒤에도 같은 자리) - 부모(SkinPivot) 좌표 = 그림 좌표 x 그림 배율
        Transform par = skin.transform.parent;
        Vector3 ls = skin.transform.localScale;
        GameObject go = new GameObject("RivalPiece" + index);
        go.transform.position = par != null ? par.TransformPoint(new Vector3(localCenter.x * ls.x, localCenter.y * ls.y, 0f)) : skin.transform.TransformPoint(localCenter);
        go.transform.rotation = skin.transform.rotation;
        go.transform.localScale = skin.transform.lossyScale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = piece;
        sr.sortingOrder = SKIN_SORT;
        sr.color = skin.color;
        go.AddComponent<FallingPiece>();

        // 남는 몸: [x1, 끝]
        float keepW = r.width - x1;
        if (keepW < 1f) { skin.enabled = false; return; }
        Sprite rest = Sprite.Create(full.texture, new Rect(r.x + x1, r.y, keepW, r.height), new Vector2(0.5f, 0.5f), ppu);
        Vector3 keepCenter = new Vector3(((x1 + r.width) * 0.5f - full.pivot.x) / ppu, (r.height * 0.5f - full.pivot.y) / ppu, 0f);
        skin.sprite = rest;
        skin.transform.localPosition = new Vector3(keepCenter.x * ls.x, keepCenter.y * ls.y, 0f);   // 부모(SkinPivot) 좌표 - 그림 배율을 곱한다
    }

    /// <summary>O6: 떨어져 나간 덩이 - 뒤로(오른쪽, 기차가 달리는 반대쪽) 처지며 기울고 흐려진다. 스스로 사라진다</summary>
    private class FallingPiece : MonoBehaviour
    {
        private float age, spin, life = 1.3f;
        private SpriteRenderer sr;
        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            spin = Random.Range(-14f, 14f);
        }
        private void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            if (age >= life) { Destroy(gameObject); return; }
            float k = age / life;
            transform.position += new Vector3(1.5f + 9f * k, -0.4f * k, 0f) * dt;   // 점점 빨리 뒤로 처진다
            transform.Rotate(0f, 0f, spin * dt);
            if (sr != null)
            {
                Color c = sr.color;
                c.a = k < 0.55f ? 1f : 1f - (k - 0.55f) / 0.45f;
                sr.color = c;
            }
        }
    }

    // ─────────────────────────────────────────────
    // 천둥 둥지의 알 (T2)
    // ─────────────────────────────────────────────
    /// <summary>
    /// 번개 알: 손님(Enemy)이라 포탑이 쏜다. 날아가는 동안은 표적이 아니다 (박히면 켠다).
    /// 둘레의 고리가 시계 방향으로 차오르면 부화 = 새끼 테라노돈 ThunderEggHatchlings 마리 (마지막 1.5초는 흔들린다). 부화 전에 깨지면 아무것도 안 나온다.
    /// 처치 보상 없음 (깨면 그걸로 끝). 보스가 쓰러지면 그냥 깨진다 (RoutMinions)
    /// </summary>
    private class BossEgg : Enemy
    {
        private BossEnemy owner;
        private Vector3 from, to;
        private float flightT, hatchT;
        private bool landed, cracked;
        private SpriteRenderer body, ground;
        private LineRenderer ring;
        private static Sprite eggSprite;
        private static Material lineMat;
        private const float FLIGHT_SEC = 0.7f;
        private const int RING_SEG = 30;
        private const float RING_R = 0.62f;

        public static BossEgg Launch(BossEnemy owner, Vector3 from, Vector3 to, float hp)
        {
            GameObject go = new GameObject("BossEgg");
            go.transform.position = from;
            BossEgg egg = go.AddComponent<BossEgg>();
            egg.owner = owner; egg.from = from; egg.to = to;
            egg.data = new EnemyData
            {
                enemyName = "번개 알", baseHP = hp, baseATK = 0f, baseSPD = 0f, dropMaterialName = "",
                goldReward = 0, xpReward = 0, targetPriority = "", specialAbility = "부화하면 새끼 테라노돈"
            };
            egg.currentHP = hp; egg.scaledMaxHP = hp; egg.scaledATK = 0f; egg.scaledSPD = 0f;
            egg.attackRange = 0f;
            egg.isAlive = false;   // 날아가는 동안은 표적이 아니다

            GameObject b = new GameObject("Body");
            b.transform.SetParent(go.transform, false);
            b.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            egg.body = b.AddComponent<SpriteRenderer>();
            egg.body.sprite = EggSprite();
            egg.body.sortingOrder = 4;                                     // 손님(5) 아래, 포탑 돔(-1) 위
            EnemySkin marker = go.AddComponent<EnemySkin>();               // 손님 그림 스캐너가 랩터 그림을 입히지 않게
            marker.skin = egg.body;

            GameObject g = new GameObject("EggShadow");                    // 땅 그림자 (날아가는 동안 떨어질 자리로 간다)
            g.transform.position = from;
            egg.ground = g.AddComponent<SpriteRenderer>();
            egg.ground.sprite = AttackVFX.GetCircleSprite();
            egg.ground.color = new Color(0f, 0f, 0f, 0.3f);
            egg.ground.sortingOrder = -7;
            g.transform.localScale = new Vector3(0.7f, 0.35f, 1f);
            return egg;
        }

        /// <summary>코드 도트 알 (14 x 18): 크림색 껍질 + 보라 반점 + 노란 번개 금</summary>
        private static Sprite EggSprite()
        {
            if (eggSprite != null) return eggSprite;
            const int W = 14, H = 18;
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color shell = new Color(0.93f, 0.89f, 0.78f), shade = new Color(0.74f, 0.68f, 0.6f), edge = new Color(0.22f, 0.16f, 0.24f);
            Color spot = new Color(0.55f, 0.36f, 0.78f), bolt = new Color(1f, 0.88f, 0.3f);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float nx = (x + 0.5f - W * 0.5f) / (W * 0.5f);
                    float ny = (y + 0.5f - H * 0.45f) / (H * (y < H * 0.45f ? 0.45f : 0.55f));   // 위가 좁은 달걀꼴
                    float d = nx * nx + ny * ny;
                    Color c = clear;
                    if (d <= 1f) c = d > 0.72f ? edge : (nx > 0.35f || ny < -0.55f ? shade : shell);
                    tex.SetPixel(x, y, c);
                }
            int[,] spots = { { 4, 11 }, { 9, 7 }, { 6, 4 }, { 10, 12 } };
            for (int i = 0; i < spots.GetLength(0); i++) tex.SetPixel(spots[i, 0], spots[i, 1], spot);
            int[,] crack = { { 7, 14 }, { 6, 13 }, { 7, 12 }, { 8, 11 }, { 7, 10 }, { 6, 9 } };
            for (int i = 0; i < crack.GetLength(0); i++) tex.SetPixel(crack[i, 0], crack[i, 1], bolt);
            tex.Apply();
            eggSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.35f), 32f);
            return eggSprite;
        }

        private void Start() { }   // Enemy.Start(이름 표·행동 배정)를 막는다 - 알은 움직이지도 물지도 않는다

        private void Update()
        {
            if (TutorialDirector.InlineFreeze || cracked) return;
            if (!landed)
            {
                flightT += Time.deltaTime;
                float k = Mathf.Clamp01(flightT / FLIGHT_SEC);
                Vector3 p = Vector3.Lerp(from, to, k);
                transform.position = p + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 2.2f);
                transform.Rotate(0f, 0f, 540f * Time.deltaTime);
                if (ground != null) { ground.transform.position = p; ground.transform.localScale = new Vector3(0.7f, 0.35f, 1f) * (0.6f + 0.4f * k); }
                if (k >= 1f) Land();
                return;
            }
            if (!isAlive) return;
            if (owner == null || !owner.IsAlive) { Crack(false); return; }
            hatchT += Time.deltaTime;
            float hatch = Mathf.Max(0.5f, GameBalance.ThunderEggHatchSec);
            DrawRing(hatchT / hatch);
            float left = hatch - hatchT;
            if (left < 1.5f) transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 32f) * 14f * (1f - left / 1.5f));
            if (left <= 0f) Hatch();
        }

        private void Land()
        {
            landed = true;
            isAlive = true;
            transform.position = to;
            transform.rotation = Quaternion.identity;
            if (ground != null) Destroy(ground.gameObject);
            DustFX.Puff(to, 1f);
            SoundManager.PlayAt("sfx_boss_land", to, 0.35f);
            WorldFeel.Ring(to, EGG_COL, 0.9f, 0.25f);
            GameObject rg = new GameObject("HatchRing");
            rg.transform.SetParent(transform, false);
            ring = rg.AddComponent<LineRenderer>();
            if (lineMat == null) lineMat = new Material(Shader.Find("Sprites/Default"));
            ring.sharedMaterial = lineMat;
            ring.useWorldSpace = true;
            ring.startWidth = 0.09f; ring.endWidth = 0.09f;
            ring.sortingOrder = 6;
            ring.positionCount = 2;
            DrawRing(0f);
        }

        /// <summary>부화까지 남은 시간 고리: 위에서 시계 방향으로 k 만큼 차오른다 (노랑 -> 빨강)</summary>
        private void DrawRing(float k)
        {
            if (ring == null) return;
            k = Mathf.Clamp01(k);
            int n = Mathf.Max(2, Mathf.CeilToInt(RING_SEG * k) + 1);
            if (ring.positionCount != n) ring.positionCount = n;
            Vector3 c = transform.position + Vector3.up * 0.15f;
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.PI * 0.5f - (i / (float)(n - 1)) * k * Mathf.PI * 2f;
                ring.SetPosition(i, c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * RING_R);
            }
            Color col = Color.Lerp(new Color(1f, 0.9f, 0.35f, 0.95f), new Color(1f, 0.3f, 0.2f, 1f), k);
            ring.startColor = col; ring.endColor = col;
        }

        /// <summary>부화: 새끼 테라노돈이 나오고 껍질이 흩어진다</summary>
        private void Hatch()
        {
            if (cracked) return;
            WaveManager wm = owner != null ? owner.waveManagerRef : FindFirstObjectByType<WaveManager>();
            if (wm != null)
            {
                int n = Mathf.Max(1, GameBalance.ThunderEggHatchlings);
                Vector3[] pts = new Vector3[n];
                for (int i = 0; i < n; i++) pts[i] = transform.position + new Vector3((i - (n - 1) * 0.5f) * 0.7f, 0.25f, 0f);
                wm.SpawnBossUnits("bolt", HatchlingData(), pts, 1f, GameBalance.BossSummonShare);
            }
            UIManager.Instance?.ShowStatChange("알이 부화했다. 새끼 테라노돈이 날아오른다");
            Burst(false);
        }

        /// <summary>새끼 테라노돈: 볼트 테라노돈의 HP·공격력 x ThunderHatchlingHPMul (이름에 "테라노돈"이 있어 그림·급강하·저항은 같은 규칙. 체력이 작아 그림도 작다)</summary>
        private static EnemyData HatchlingData()
        {
            EnemyData b = BoltTeranodon;
            float m = Mathf.Clamp(GameBalance.ThunderHatchlingHPMul, 0.1f, 2f);
            return new EnemyData
            {
                enemyName = "새끼 테라노돈", baseHP = b.baseHP * m, baseATK = b.baseATK * m, baseSPD = b.baseSPD,
                dropMaterialName = b.dropMaterialName, goldReward = Mathf.Max(1, b.goldReward / 2), xpReward = 0,
                targetPriority = b.targetPriority, specialAbility = b.specialAbility
            };
        }

        /// <summary>포탑에 깨졌다 - 보상 없이 껍질만 (Enemy.Die 를 쓰지 않는다)</summary>
        protected override void Die()
        {
            if (cracked) return;
            Crack(true);
        }

        /// <summary>깨진다 (shot = 포탑에 맞아서 - 고리 / false = 보스가 쓰러졌다)</summary>
        public void Crack(bool shot)
        {
            if (cracked) return;
            if (shot) WorldFeel.Ring(transform.position, EGG_COL, 1.1f, 0.25f);
            Burst(true);
        }

        private void Burst(bool broken)
        {
            cracked = true;
            isAlive = false;
            if (ground != null) Destroy(ground.gameObject);
            ArmorShard.Burst(transform.position, new Color(0.93f, 0.89f, 0.78f), broken ? new Color(1f, 0.85f, 0.3f) : EGG_COL, 8);
            SoundManager.PlayAt("sfx_break", transform.position, 0.8f);
            Destroy(gameObject, 0.02f);
        }

        private void OnDestroy()
        {
            if (ground != null) Destroy(ground.gameObject);
        }
    }

    // ─────────────────────────────────────────────
    // 눈사태 벽 (H3)
    // ─────────────────────────────────────────────
    /// <summary>
    /// 화면 위에서 지붕(roofY)까지 내려오는 흰 벽 - 진행도(SetProgress 0 ~ 1)가 곧 남은 시간이다. 몸(옅게) + 앞쪽 띠(진하게) + 들쭉날쭉한 눈 끝 + 앞에 흩날리는 눈.
    /// 깨지면(Break) 흐려지며 사라지고, 지붕에 닿으면(Crash) 한 번 하얗게 번쩍인 뒤 사라진다. 보스·셰프·손님 위, HUD 아래 (월드 정렬 30 ~ 32)
    /// </summary>
    private class AvalancheWall : MonoBehaviour
    {
        private SpriteRenderer body, band;
        private readonly List<SpriteRenderer> teeth = new List<SpriteRenderer>();
        private float roofY, k, endT = -1f, endSec = 0.4f, flakeT;
        private bool crash;
        private static Sprite toothSprite;
        private const float TOOTH_W = 4f;          // 끝 조각 하나의 폭 (u)

        public static AvalancheWall Create(float roofY)
        {
            GameObject go = new GameObject("AvalancheWall");
            AvalancheWall w = go.AddComponent<AvalancheWall>();
            w.roofY = roofY;
            w.body = MakePart(go.transform, "Body", TrainDeck.GetWhiteSprite(), new Color(0.9f, 0.95f, 1f, 0.42f), 30);
            w.band = MakePart(go.transform, "Band", TrainDeck.GetWhiteSprite(), new Color(0.95f, 0.98f, 1f, 0.7f), 31);
            for (int i = 0; i < 16; i++) w.teeth.Add(MakePart(go.transform, "Edge" + i, ToothSprite(), new Color(0.97f, 0.99f, 1f, 0.95f), 32));
            w.Layout();
            return w;
        }

        private static SpriteRenderer MakePart(Transform parent, string name, Sprite s, Color c, int order)
        {
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent, false);
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.color = c; sr.sortingOrder = order;
            return sr;
        }

        /// <summary>끝 조각 그림 (64 x 16, 4u x 1u): 위는 꽉 차고 아래 끝이 들쭉날쭉 + 끝선 그늘</summary>
        private static Sprite ToothSprite()
        {
            if (toothSprite != null) return toothSprite;
            const int W = 64, H = 16;
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            System.Random rng = new System.Random(923);
            int[] depth = new int[W];
            int cur = 6;
            for (int x = 0; x < W; x++)
            {
                cur = Mathf.Clamp(cur + rng.Next(-2, 3), 2, 12);
                if (x == 0 || x == W - 1) cur = 6;   // 이웃 조각과 이어지게 양 끝은 같은 높이
                depth[x] = cur;
            }
            Color shadeCol = new Color(0.72f, 0.84f, 0.95f, 1f);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int fromTop = H - 1 - y;                       // 위(벽 몸에 붙는 쪽)에서 몇 줄째
                    bool fill = fromTop <= depth[x];               // 위는 꽉 차고 아래 끝이 열마다 다른 깊이
                    Color c = !fill ? new Color(1f, 1f, 1f, 0f) : (fromTop >= depth[x] - 1 ? shadeCol : Color.white);   // 끝 두 줄 = 그늘
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            toothSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 1f), 16f);   // 피벗 = 위 가운데 (벽 몸의 아래 끝에 붙는다)
            return toothSprite;
        }

        public void SetProgress(float progress) { k = Mathf.Clamp01(progress); }

        public void Break(bool shatter)
        {
            if (endT >= 0f) return;
            endT = 0f; endSec = shatter ? 0.35f : 0.25f; crash = false;
        }

        public void Crash()
        {
            if (endT >= 0f) return;
            endT = 0f; endSec = 0.55f; crash = true;
            k = 1f;
        }

        private void Update()
        {
            Layout();
            if (endT < 0f)
            {
                // 벽 앞에 흩날리는 눈
                flakeT -= Time.deltaTime;
                if (flakeT <= 0f && GameBalance.GameFeelMaster > 0f)
                {
                    flakeT = 0.05f;
                    Camera cam = Camera.main;
                    float hw = cam != null ? cam.orthographicSize * cam.aspect : 18f;
                    float cx = cam != null ? cam.transform.position.x : 0f;
                    SparkPool.Emit(new Vector3(cx + Random.Range(-hw, hw), Bottom() - Random.Range(0f, 0.6f), 0f), new Color(1f, 1f, 1f, 0.9f), 1, 0.07f, 1.2f);
                }
                return;
            }
            endT += Time.deltaTime;
            float e = Mathf.Clamp01(endT / endSec);
            float a = crash ? (e < 0.2f ? 1f : 1f - (e - 0.2f) / 0.8f) : 1f - e;
            SetAlpha(a, crash && e < 0.2f);
            if (endT >= endSec) Destroy(gameObject);
        }

        private float Top()
        {
            Camera cam = Camera.main;
            return cam != null ? cam.transform.position.y + cam.orthographicSize + 1f : roofY + 12f;
        }

        private float Bottom() { return Mathf.Lerp(Top(), roofY, k); }

        private void Layout()
        {
            Camera cam = Camera.main;
            float hw = (cam != null ? cam.orthographicSize * cam.aspect : 18f) + 2f;
            float cx = cam != null ? cam.transform.position.x : 0f;
            float top = Top() + 1f, bottom = Bottom();
            float h = Mathf.Max(0.01f, top - bottom);
            body.transform.position = new Vector3(cx, (top + bottom) * 0.5f, 0f);
            body.transform.localScale = new Vector3(hw * 2f, h, 1f);
            float bandH = Mathf.Min(1.4f, h);
            band.transform.position = new Vector3(cx, bottom + bandH * 0.5f, 0f);
            band.transform.localScale = new Vector3(hw * 2f, bandH, 1f);
            float startX = cx - hw + TOOTH_W * 0.5f;
            for (int i = 0; i < teeth.Count; i++)
            {
                float x = startX + i * TOOTH_W;
                teeth[i].enabled = x - TOOTH_W * 0.5f < cx + hw;
                teeth[i].transform.position = new Vector3(x, bottom + 0.15f, 0f);
            }
        }

        private void SetAlpha(float a, bool white)
        {
            a = Mathf.Clamp01(a);
            body.color = white ? new Color(1f, 1f, 1f, 0.95f * a) : new Color(0.9f, 0.95f, 1f, 0.42f * a);   // 부딪는 순간은 하얗게 번쩍
            band.color = new Color(0.95f, 0.98f, 1f, 0.7f * a);
            for (int i = 0; i < teeth.Count; i++) teeth[i].color = new Color(0.97f, 0.99f, 1f, 0.95f * a);
        }
    }

    // ─────────────────────────────────────────────
    // 패턴 표시 (v9.23) - BossMark 와 같은 규칙: 스스로 사라진다
    // ─────────────────────────────────────────────
    /// <summary>
    /// Ellipse = 바닥의 그림자 원(땅 층) + 테두리(데크 위) + 숫자 / Frame = 칸(또는 상자) 테두리 + 옅은 채움 / Ring = 바닥에서 조여 드는 고리 (알·약점).
    /// Pop = 터지듯 커지며 금방 사라진다 (그 자리에 무슨 일이 일어난 순간) / Kill = 흐려지며 사라진다
    /// </summary>
    private class PatternMark : MonoBehaviour
    {
        private int mode;                         // 0 Ellipse / 1 Frame / 2 Ring
        private float age, life, fadeLeft = -1f, fadeSec = 0.2f, popAge = -1f;
        private Color col;
        private float rx, ry, fillA;              // Ellipse: 반지름 / Frame: 반 폭·반 높이
        private float r0, r1;                     // Ring: 처음 -> 끝 반지름
        private SpriteRenderer fill;
        private LineRenderer line;
        private TextMeshPro label;
        private BossEnemy followBoss;             // Ring: 보스 몸 위의 자리를 따라간다 (약점 고리)
        private float followFrac;
        private static Material lineMat;
        private const int SEG = 36;

        private static PatternMark Make(string name, Vector3 pos, int mode, Color c, float life)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            PatternMark m = go.AddComponent<PatternMark>();
            m.mode = mode; m.col = c; m.life = Mathf.Max(0.1f, life);
            m.line = go.AddComponent<LineRenderer>();
            if (lineMat == null) lineMat = new Material(Shader.Find("Sprites/Default"));
            m.line.sharedMaterial = lineMat;
            m.line.useWorldSpace = true;
            m.line.sortingOrder = 3;               // (고리는 마지막 점을 첫 점에 겹쳐 닫는다 - SEG + 1 개)               // 포탑 돔(-1 ~ 0) 위, 손님(5)·보스(6) 아래
            return m;
        }

        public static PatternMark Ellipse(Vector3 pos, float rx, float ry, Color c, float life, string number)
        {
            PatternMark m = Make("PatternMark_Ellipse", pos, 0, c, life);
            m.rx = rx; m.ry = ry;
            GameObject f = new GameObject("Fill");
            f.transform.SetParent(m.transform, false);
            m.fill = f.AddComponent<SpriteRenderer>();
            m.fill.sprite = AttackVFX.GetCircleSprite();
            m.fill.sortingOrder = -7;              // 땅(-10) 위, 데크(-6) 아래 - 그림자는 땅에만
            m.line.positionCount = SEG + 1;
            if (!string.IsNullOrEmpty(number))
            {
                GameObject l = new GameObject("Label");
                l.transform.SetParent(m.transform, false);
                m.label = l.AddComponent<TextMeshPro>();
                TMPFontFixer.Apply(m.label);
                m.label.text = number; m.label.fontSize = 9f; m.label.alignment = TextAlignmentOptions.Center;
                m.label.fontStyle = FontStyles.Bold; m.label.sortingOrder = 58;
            }
            m.Draw();
            return m;
        }

        public static PatternMark CarFrame(int car, Color c, float life, float fillAlpha)
        {
            float[] e = GameBalance.CarEdgesX;
            car = Mathf.Clamp(car, 0, e.Length - 2);
            GameObject train = GameObject.FindGameObjectWithTag("Train");
            float ty = train != null ? train.transform.position.y : 0f;
            return Box(new Vector3((e[car] + e[car + 1]) * 0.5f, ty, 0f), new Vector2(e[car + 1] - e[car] - 0.2f, 3.9f), c, life, fillAlpha);
        }

        public static PatternMark Box(Vector3 center, Vector2 size, Color c, float life, float fillAlpha)
        {
            PatternMark m = Make("PatternMark_Frame", center, 1, c, life);
            m.rx = size.x * 0.5f; m.ry = size.y * 0.5f; m.fillA = Mathf.Clamp01(fillAlpha);
            m.line.positionCount = 5;
            if (m.fillA > 0.001f)
            {
                GameObject f = new GameObject("Fill");
                f.transform.SetParent(m.transform, false);
                m.fill = f.AddComponent<SpriteRenderer>();
                m.fill.sprite = TrainDeck.GetWhiteSprite();
                m.fill.sortingOrder = 0;           // 포탑 돔 위 (덮인 칸이 그 색으로 물든다), 손님·셰프 아래
                f.transform.localScale = new Vector3(size.x, size.y, 1f);
            }
            m.Draw();
            return m;
        }

        public static PatternMark Ring(Vector3 pos, float fromR, float toR, Color c, float life)
        {
            PatternMark m = Make("PatternMark_Ring", pos, 2, c, life);
            m.r0 = fromR; m.r1 = toR;
            m.line.positionCount = SEG + 1;
            m.Draw();
            return m;
        }

        /// <summary>Ring 이 보스 몸 위의 자리(그림 왼쪽 끝에서 frac)를 따라간다 (약점 고리)</summary>
        public PatternMark FollowBody(BossEnemy boss, float frac) { followBoss = boss; followFrac = frac; return this; }

        public void Pop() { if (popAge < 0f && fadeLeft < 0f) popAge = 0f; }

        public void Kill(float sec) { if (fadeLeft < 0f) { fadeSec = Mathf.Max(0.01f, sec); fadeLeft = fadeSec; } }

        public static void KillAll(List<PatternMark> marks)
        {
            if (marks == null) return;
            for (int i = 0; i < marks.Count; i++) if (marks[i] != null) marks[i].Kill(0.2f);
            marks.Clear();
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (popAge >= 0f) { popAge += Time.deltaTime; if (popAge >= 0.22f) { Destroy(gameObject); return; } }
            if (fadeLeft >= 0f) { fadeLeft -= Time.deltaTime; if (fadeLeft <= 0f) { Destroy(gameObject); return; } }
            if (age >= life) { Destroy(gameObject); return; }
            if (followBoss != null && followBoss.skin != null) transform.position = followBoss.BodyPointAt(followFrac);
            Draw();
        }

        private void Draw()
        {
            float k = Mathf.Clamp01(age / life);
            float a = Mathf.Clamp01(age / 0.2f);                                         // 0.2초에 떠오른다
            if (fadeLeft >= 0f) a *= Mathf.Clamp01(fadeLeft / fadeSec);
            float grow = 1f;
            if (popAge >= 0f) { float p = popAge / 0.22f; grow = 1f + 0.35f * p; a *= 1f - p; }
            float pulse = 0.75f + 0.25f * Mathf.Sin(age * 9f);
            Vector3 c = transform.position;
            Color lc = col; lc.a = col.a * a * pulse;
            line.startColor = lc; line.endColor = lc;
            if (mode == 0)
            {
                float sx = rx * grow * Mathf.Lerp(0.85f, 1f, a), sy = ry * grow * Mathf.Lerp(0.85f, 1f, a);
                for (int i = 0; i <= SEG; i++)
                {
                    float t = i * Mathf.PI * 2f / SEG;
                    line.SetPosition(i, c + new Vector3(Mathf.Cos(t) * sx, Mathf.Sin(t) * sy, 0f));
                }
                float w = 0.1f;
                line.startWidth = w; line.endWidth = w;
                if (fill != null)
                {
                    fill.transform.localScale = new Vector3(sx * 2f, sy * 2f, 1f);
                    fill.color = new Color(0.05f, 0.03f, 0.02f, 0.38f * a);
                }
                if (label != null) { Color tc = new Color(1f, 0.93f, 0.8f, a); label.color = tc; label.transform.localScale = Vector3.one * grow; }
            }
            else if (mode == 1)
            {
                float hx = rx * grow, hy = ry * grow;
                line.SetPosition(0, c + new Vector3(-hx, hy, 0f));
                line.SetPosition(1, c + new Vector3(hx, hy, 0f));
                line.SetPosition(2, c + new Vector3(hx, -hy, 0f));
                line.SetPosition(3, c + new Vector3(-hx, -hy, 0f));
                line.SetPosition(4, c + new Vector3(-hx, hy, 0f));
                line.startWidth = 0.12f; line.endWidth = 0.12f;
                if (fill != null) fill.color = new Color(col.r, col.g, col.b, fillA * a);
            }
            else
            {
                float r = Mathf.Lerp(r0, r1, 1f - (1f - k) * (1f - k)) * grow;
                for (int i = 0; i <= SEG; i++)
                {
                    float t = i * Mathf.PI * 2f / SEG;
                    line.SetPosition(i, c + new Vector3(Mathf.Cos(t) * r, Mathf.Sin(t) * r * 0.85f, 0f));
                }
                line.startWidth = 0.09f; line.endWidth = 0.09f;
            }
        }
    }
}
