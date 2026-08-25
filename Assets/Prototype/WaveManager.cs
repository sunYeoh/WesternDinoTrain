using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [WaveManager.cs] v6
/// 웨이브 단위로 적 유닛을 스폰하고, 모든 적 처치 시 웨이브 완료를 알립니다.
/// - v6 변경점 (25웨이브 3지역 개편 - 슬더스 3막 구조):
///   지역 1 구리 사막(물리/러시) -> 지역 2 테슬라 협곡(전기/독/공중) -> 지역 3 코발트 광산(냉기/장갑/힐러)
///   각 지역 마지막 웨이브 = 지역 보스, 최종 웨이브 = 최종전.
///   지역 길이/보스 배치는 전부 GameBalance(RegionLength/FinalWave/IsBossWave)에서 온다.
///   지역이 바뀌면 카메라 배경색 교체 + 지역 도입 문구 표시.
///   최종 웨이브 클리어 시 증강 대신 GameManager 승리 처리로 직행.
/// 스폰 포인트 없이 기차 중심 랜덤 위치에서 자동 스폰합니다.
/// - v2 변경점 (로그라이크 연동):
///   1) 웨이브 클리어 시 증강 3택1(AugmentPickUI)을 먼저 띄우고, 선택 후 기존 흐름 진행
///   2) 증강 선택이 끝나면 일정 시간 뒤 다음 웨이브 자동 시작 (autoProgress)
///   3) 웨이브 시작 시각을 기록해 '개전 포격' 증강이 참조
/// - v3 변경점:
///   4) 자동 진행이 GameManager.OnClickNextWave()를 경유 - 웨이브 카운트/상태 동기화
///   5) 증강 '출혈 배팅': 웨이브 시작 시 기차 HP 차감
/// VS 2017 (C# 7.3) 호환 버전입니다.
/// </summary>
public class WaveManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // 웨이브 구성 데이터
    // ─────────────────────────────────────────────
    [System.Serializable]
    public struct WaveConfig
    {
        public int waveNumber;

        // Phase 1 유닛
        public int steamRaptorCount;    // 스팀 랩터
        public int springAnkyloCount;   // 태엽 아르마딜로
        public int oilCactusCount;      // 오일 캑터스
        public int scorpionCount;       // 사막 전갈 (v5 신규)
        public int tortoiseCount;       // 구리 거북 (v5 신규)

        // Phase 2 유닛
        public int boltTeranodonCount;  // 볼트 테라노돈
        public int poisonPteraCount;    // 독침 프테라
        public int magnetParasaurCount; // 자석 파라사우
        public int overloadFlyCount;    // 과부하 플라이
        public int steelRaptorCount;    // 강철 랩터 (v5 신규)
        public int flamePteroCount;     // 화염 익룡 (v5 신규)

        // Phase 3 유닛
        public int iceMosaCount;        // 아이스 모사
        public int crystalPachyCount;   // 크리스탈 파키
        public int magmaCarnoCount;     // 마그마 카르노
        public int frostMammothCount;   // 서리 맘모스
        public int necroSpinoCount;     // 네크로 스피노 - 힐러 (v5 신규)

        public bool hasBoss;
        public float spawnInterval;
        public float difficultyL;         // 난이도 계수 (Easy=2.0, Hard=1.0)
    }

    // ─────────────────────────────────────────────
    // Inspector 설정 - 적 프리팹
    // ─────────────────────────────────────────────

    [Header("─ Phase 1 프리팹 ─")]
    public GameObject steamRaptorPrefab;
    public GameObject springAnkyloPrefab;
    public GameObject oilCactusPrefab;

    [Header("─ Phase 2 프리팹 ─")]
    public GameObject boltTeranodonPrefab;
    public GameObject poisonPteraPrefab;
    public GameObject magnetParasaurPrefab;
    public GameObject overloadFlyPrefab;

    [Header("─ Phase 3 프리팹 ─")]
    public GameObject iceMosaPrefab;
    public GameObject crystalPachyPrefab;
    public GameObject magmaCarnoPrefab;
    public GameObject frostMammothPrefab;

    [Header("─ v5 신규 유닛 프리팹 (미할당이면 자동 스킵) ─")]
    public GameObject scorpionPrefab;       // 사막 전갈 (도구 파괴꾼)
    public GameObject tortoisePrefab;       // 구리 거북 (고방어 탱커)
    public GameObject steelRaptorPrefab;    // 강철 랩터 (물리 면역급)
    public GameObject flamePteroPrefab;     // 화염 익룡 (급강하 폭격)
    public GameObject necroSpinoPrefab;     // 네크로 스피노 (힐러 - 우선 처치)

    [Header("─ 보스 프리팹 ─")]
    public GameObject bossPrefab;

    [Header("─ 랜덤 스폰 설정 ─")]
    public float spawnDistanceMin = 12f;
    public float spawnDistanceMax = 16f;

    [Header("─ 웨이브 설정 목록 ─")]
    public List<WaveConfig> waveConfigs;

    [Header("─ 로그라이크 자동 진행 (v2) ─")]
    public bool autoProgress = true;        // 증강 선택 후 다음 웨이브 자동 시작
    public float autoProgressDelay = 4f;    // 자동 시작까지 대기 시간(초) - 정비 시간
    public int maxWave = 40;                // 이 웨이브를 넘기면 자동 진행 중단

    [Header("─ 치트 (빌드 전 false) ─")]
    public bool debugBossJumpEnabled = true;   // B키: 다음 보스 웨이브로 즉시 점프

    // ─────────────────────────────────────────────
    // 런타임 상태
    // ─────────────────────────────────────────────
    private int currentWaveNumber = 0;
    private int aliveEnemyCount = 0;
    private bool isWaveActive = false;
    private Transform trainTransform;

    // v6.2: 분기 선로 - 다음 웨이브에 적용될 선로 / 이번 웨이브에 적용 중인 선로
    private RouteData pendingRoute = null;
    private RouteData activeRoute = null;

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────
    private void Start()
    {
        GameObject trainObj = GameObject.FindGameObjectWithTag("Train");
        if (trainObj != null)
            trainTransform = trainObj.transform;

        // v6: 런 길이는 GameBalance가 결정 (Inspector 값 무시)
        maxWave = GameBalance.FinalWave;

        if (waveConfigs == null || waveConfigs.Count == 0)
            GenerateDefaultWaveConfigs(GameBalance.FinalWave);
    }

    // ─────────────────────────────────────────────
    // 웨이브 시작 (GameManager 또는 자동 진행에서 호출)
    // ─────────────────────────────────────────────
    public void StartWave(int waveNumber)
    {
        if (isWaveActive)
        {
            Debug.LogWarning("[WaveManager] 이미 웨이브가 진행 중입니다.");
            return;
        }

        currentWaveNumber = waveNumber;
        isWaveActive = true;
        aliveEnemyCount = 0;

        // 증강 '개전 포격' 참조용: 웨이브 시작 시각 기록
        AugmentManager.WaveStartTime = Time.time;

        // 증강 '출혈 배팅': 웨이브 시작마다 기차 HP -80 (데미지 +35%의 대가)
        if (AugmentManager.BloodBet)
        {
            TrainManager tm = FindFirstObjectByType<TrainManager>();
            if (tm != null)
            {
                tm.TakeDamage(80f);
                Debug.Log("[WaveManager] 출혈 배팅 발동 - 기차 HP -80");
            }
        }

        // 보스 웨이브 긴급 보급 (v4.1): 초반엔 독/전기 재료 수급처가 없어
        // 디버프 요리를 못 만드는 문제 해결 - 보스전 시작 시 재료 지급
        WaveConfig bossCheck = GetWaveConfig(waveNumber);
        if (bossCheck.hasBoss && MaterialInventory.Instance != null)
        {
            MaterialInventory.Instance.Add(MaterialType.Poison, 1);
            MaterialInventory.Instance.Add(MaterialType.Meat, 1);
            Debug.Log("[WaveManager] 보스 감지 - 긴급 보급: 독 재료 + 고기 지급");
            UIManager.Instance?.ShowWaveNotice("보스 접근 중! 긴급 보급 도착!",
                "독 재료 + 고기 지급 - 독침 육포를 조리해 그로기 때 던져라! (그릴에서 굽기)");
        }

        WaveConfig config = GetWaveConfig(waveNumber);

        // v6.2: 분기 선로 규칙 적용 (물량 배율 / 이른 이벤트)
        activeRoute = pendingRoute;
        pendingRoute = null;
        if (activeRoute != null)
        {
            ApplyRouteCounts(ref config, activeRoute.countMul);

            if (activeRoute.earlyEvent)
            {
                KitchenEventManager kem = FindFirstObjectByType<KitchenEventManager>();
                if (kem != null) kem.ScheduleEarlyEvent();
            }

            if (activeRoute.id != "straight")
                UIManager.Instance?.ShowStatChange("[" + activeRoute.routeName + "] 규칙 적용!");
        }

        // v6: 지역 전환 연출 (배경색 + 도입 문구)
        // 지역이 바뀐 웨이브에서는 도입 문구가 우선 - 속성 예고는 생략 (문구 덮어쓰기 방지)
        bool regionChanged = ApplyRegionTransition(waveNumber);

        // 웨이브 속성 예고
        if (!regionChanged)
            ShowWaveAttributeNotice(config);

        Debug.Log("[WaveManager] 웨이브 " + waveNumber + " 시작!");
        StartCoroutine(SpawnWaveCoroutine(config));
    }

    /// <summary>v6.2: 분기 선로 물량 배율을 웨이브 구성 전체에 적용</summary>
    private void ApplyRouteCounts(ref WaveConfig config, float mul)
    {
        if (Mathf.Approximately(mul, 1f)) return;

        config.steamRaptorCount = ScaleCount(config.steamRaptorCount, mul);
        config.springAnkyloCount = ScaleCount(config.springAnkyloCount, mul);
        config.oilCactusCount = ScaleCount(config.oilCactusCount, mul);
        config.scorpionCount = ScaleCount(config.scorpionCount, mul);
        config.tortoiseCount = ScaleCount(config.tortoiseCount, mul);
        config.boltTeranodonCount = ScaleCount(config.boltTeranodonCount, mul);
        config.poisonPteraCount = ScaleCount(config.poisonPteraCount, mul);
        config.magnetParasaurCount = ScaleCount(config.magnetParasaurCount, mul);
        config.overloadFlyCount = ScaleCount(config.overloadFlyCount, mul);
        config.steelRaptorCount = ScaleCount(config.steelRaptorCount, mul);
        config.flamePteroCount = ScaleCount(config.flamePteroCount, mul);
        config.iceMosaCount = ScaleCount(config.iceMosaCount, mul);
        config.crystalPachyCount = ScaleCount(config.crystalPachyCount, mul);
        config.magmaCarnoCount = ScaleCount(config.magmaCarnoCount, mul);
        config.frostMammothCount = ScaleCount(config.frostMammothCount, mul);
        config.necroSpinoCount = ScaleCount(config.necroSpinoCount, mul);
    }

    /// <summary>0마리는 0으로 유지, 1마리 이상은 배율 적용 후 최소 1 보장</summary>
    private int ScaleCount(int count, float mul)
    {
        if (count <= 0) return 0;
        return Mathf.Max(1, Mathf.RoundToInt(count * mul));
    }

    // ─────────────────────────────────────────────
    // v6: 지역 전환 - 카메라 배경색 교체 + 지역 도입 문구
    // ─────────────────────────────────────────────
    private int lastRegion = 0;   // 마지막으로 연출한 지역 (0 = 아직 없음)

    private bool ApplyRegionTransition(int waveNumber)
    {
        int region = GameBalance.RegionOf(waveNumber);
        if (region == lastRegion) return false;
        lastRegion = region;

        // 지역별 배경색 (스팀펑크 톤 유지, 지역 정체성만 살짝)
        Color bg;
        string regionTitle;
        string regionLine;

        if (region == 1)
        {
            bg = new Color(0.24f, 0.16f, 0.10f);   // 구리빛 사막
            regionTitle = "지역 1 - 구리 사막";
            regionLine = "녹슨 모래는 굶주림을 기억한다.";
        }
        else if (region == 2)
        {
            bg = new Color(0.12f, 0.13f, 0.22f);   // 번개 낀 협곡의 밤
            regionTitle = "지역 2 - 테슬라 협곡";
            regionLine = "번개가 둥지를 트는 곳. 하늘을 조심해라.";
        }
        else if (region == 3)
        {
            bg = new Color(0.08f, 0.15f, 0.20f);   // 코발트 광산의 냉기
            regionTitle = "지역 3 - 코발트 광산";
            regionLine = "대붕괴가 가장 깊이 남은 곳. 여기부터는 놈들도 정예다.";
        }
        else
        {
            bg = new Color(0.15f, 0.06f, 0.06f);   // 황야의 끝
            regionTitle = "황야의 끝";
            regionLine = "대륙에서 가장 오래 굶은 손님이 기다린다.";
        }

        if (Camera.main != null)
            Camera.main.backgroundColor = bg;

        UIManager.Instance?.ShowWaveNotice(regionTitle, regionLine);
        Debug.Log("[WaveManager] 지역 전환 -> " + regionTitle);
        return true;
    }

    /// <summary>웨이브 등장 적 속성 + 드롭 재료 예고 (v6: 지역 기반)</summary>
    private void ShowWaveAttributeNotice(WaveConfig config)
    {
        int region = GameBalance.RegionOf(config.waveNumber);
        string notice = "Wave " + config.waveNumber;
        string warning = "";

        if (region == 1)
        {
            notice += " - 물리 속성";
            warning = "질긴 고기류 드롭!";
            if (config.scorpionCount > 0)
                notice += "  전갈 주의(도구 부식)!";
        }
        else if (region == 2)
        {
            if (config.boltTeranodonCount > 0 || config.overloadFlyCount > 0)
            {
                notice += " - 전기 속성!";
                warning += "전기 재료 드롭 - 전격 요리 준비!";
            }
            if (config.poisonPteraCount > 0)
                notice += "  독침 프테라 주의!";
            if (config.magnetParasaurCount > 0)
                notice += "  자석 파라사우 주의!";
        }
        else
        {
            if (config.iceMosaCount > 0)
            {
                notice += " - 냉기 속성!";
                warning += "얼음꽃 드롭 - 화염 요리로 대응!";
            }
            if (config.magmaCarnoCount > 0)
            {
                notice += "  화염 속성!";
                warning += " 화염 꽃 드롭 - 냉기 요리로 대응!";
            }
            if (config.crystalPachyCount > 0)
                notice += "  반사 장갑 주의!";
            if (config.necroSpinoCount > 0)
                notice += "  힐러 우선 처치!";
        }

        Debug.Log("[WaveManager] " + notice + " | " + warning);
        UIManager.Instance?.ShowWaveNotice(notice, warning);
    }

    // ─────────────────────────────────────────────
    // 스폰 코루틴
    // ─────────────────────────────────────────────
    private IEnumerator SpawnWaveCoroutine(WaveConfig config)
    {
        // v6.1: 오프닝 등 전체 화면 스토리 연출이 재생 중이면 끝날 때까지 스폰 보류
        // (시작하자마자 텍스트와 몬스터가 동시에 나와서 읽을 틈이 없던 문제)
        while (StoryTexts.IsBlocking)
            yield return null;

        // 연출이 끝난 뒤 잠깐 숨 고를 시간
        yield return new WaitForSeconds(1f);

        int playerLevel = GameManager.Instance != null ? GameManager.Instance.playerLevel : 1;
        float diffL = config.difficultyL > 0f ? config.difficultyL : GameBalance.EnemyDifficultyL;

        // ── 무리 러시 이벤트 (v4): 랩터가 6마리 이상이면 절반이 같은 방향에서 떼로 몰려온다 ──
        int rushCount = 0;
        if (config.steamRaptorCount >= 6)
        {
            rushCount = config.steamRaptorCount / 2;
            float rushAngle = Random.Range(0f, 360f);

            UIManager.Instance?.ShowWaveNotice("무리 러시!", "랩터 떼가 한 방향에서 몰려온다!");
            Debug.Log("[WaveManager] 무리 러시 - 랩터 " + rushCount + "마리 집중 스폰 (각도 " + (int)rushAngle + ")");

            for (int i = 0; i < rushCount; i++)
            {
                SpawnEnemyAt(steamRaptorPrefab, Enemy.SteamRaptor, config.waveNumber, playerLevel, diffL,
                    rushAngle + Random.Range(-12f, 12f));
                yield return new WaitForSeconds(0.15f);   // 빠른 연속 스폰 = 떼 지어 돌격
            }
        }

        // ── Phase 1 유닛 스폰 ──
        for (int i = 0; i < config.steamRaptorCount - rushCount; i++)
        {
            SpawnEnemy(steamRaptorPrefab, Enemy.SteamRaptor, config.waveNumber, playerLevel, diffL);
            yield return new WaitForSeconds(config.spawnInterval);
        }

        for (int i = 0; i < config.springAnkyloCount; i++)
        {
            SpawnEnemy(springAnkyloPrefab, Enemy.SpringAnkylo, config.waveNumber, playerLevel, diffL);
            yield return new WaitForSeconds(config.spawnInterval);
        }

        for (int i = 0; i < config.oilCactusCount; i++)
        {
            SpawnEnemy(oilCactusPrefab, Enemy.OilCactus, config.waveNumber, playerLevel, diffL);
            yield return new WaitForSeconds(config.spawnInterval);
        }

        for (int i = 0; i < config.scorpionCount; i++)
        {
            SpawnEnemy(scorpionPrefab, Enemy.DesertScorpion, config.waveNumber, playerLevel, diffL);
            yield return new WaitForSeconds(config.spawnInterval);
        }

        for (int i = 0; i < config.tortoiseCount; i++)
        {
            SpawnEnemy(tortoisePrefab, Enemy.CopperTortoise, config.waveNumber, playerLevel, diffL);
            yield return new WaitForSeconds(config.spawnInterval * 2f);
        }

        // ── Phase 2 유닛 스폰 ──
        for (int i = 0; i < config.boltTeranodonCount; i++)
        {
            SpawnEnemy(boltTeranodonPrefab, Enemy.BoltTeranodon, config.waveNumber, playerLevel, diffL);
            yield return new WaitForSeconds(config.spawnInterval);
        }

        for (int i = 0; i < config.poisonPteraCount; i++)
        {
            SpawnEnemy(poisonPteraPrefab, Enemy.PoisonPtera, config.waveNumber, playerLevel, diffL);
            yield return new WaitForSeconds(config.spawnInterval);
        }

        for (int i = 0; i < config.magnetParasaurCount; i++)
        {
            SpawnEnemy(magnetParasaurPrefab, Enemy.MagnetParasaur, config.waveNumber, playerLevel, diffL);
            yield return new WaitForSeconds(config.spawnInterval * 2f);
        }

        for (int i = 0; i < config.overloadFlyCount; i++)
        {
            SpawnEnemy(overloadFlyPrefab, Enemy.OverloadFly, config.waveNumber, playerLevel, diffL);
            yield return new WaitForSeconds(config.spawnInterval * 0.5f); // 빠르게 스폰
        }

        for (int i = 0; i < config.steelRaptorCount; i++)
        {
            SpawnEnemy(steelRaptorPrefab, Enemy.SteelRaptor, config.waveNumber, playerLevel, diffL);
            yield return new WaitForSeconds(config.spawnInterval);
        }

        for (int i = 0; i < config.flamePteroCount; i++)
        {
            SpawnEnemy(flamePteroPrefab, Enemy.FlamePterosaur, config.waveNumber, playerLevel, diffL);
            yield return new WaitForSeconds(config.spawnInterval);
        }

        // ── Phase 3 유닛 스폰 ──
        for (int i = 0; i < config.iceMosaCount; i++)
        {
            SpawnEnemy(iceMosaPrefab, Enemy.IceMosa, config.waveNumber, playerLevel, diffL);
            yield return new WaitForSeconds(config.spawnInterval * 2f);
        }

        for (int i = 0; i < config.crystalPachyCount; i++)
        {
            SpawnEnemy(crystalPachyPrefab, Enemy.CrystalPachy, config.waveNumber, playerLevel, diffL);
            yield return new WaitForSeconds(config.spawnInterval * 2f);
        }

        for (int i = 0; i < config.magmaCarnoCount; i++)
        {
            SpawnEnemy(magmaCarnoPrefab, Enemy.MagmaCarno, config.waveNumber, playerLevel, diffL);
            yield return new WaitForSeconds(config.spawnInterval * 2f);
        }

        for (int i = 0; i < config.frostMammothCount; i++)
        {
            SpawnEnemy(frostMammothPrefab, Enemy.FrostMammoth, config.waveNumber, playerLevel, diffL);
            yield return new WaitForSeconds(config.spawnInterval * 3f);
        }

        for (int i = 0; i < config.necroSpinoCount; i++)
        {
            SpawnEnemy(necroSpinoPrefab, Enemy.NecroSpino, config.waveNumber, playerLevel, diffL);
            yield return new WaitForSeconds(config.spawnInterval * 3f);
        }

        // ── 보스 스폰 ──
        if (config.hasBoss)
        {
            yield return new WaitForSeconds(3f);
            SpawnBoss();
        }
    }

    // ─────────────────────────────────────────────
    // 적 스폰 및 초기화
    // ─────────────────────────────────────────────
    /// <summary>지정 각도에서 스폰 (무리 러시/보스 증원용). 생성된 Enemy 반환</summary>
    private Enemy SpawnEnemyAt(GameObject prefab, Enemy.EnemyData enemyData, int waveNum, int playerLevel, float diffL, float angleDeg)
    {
        if (prefab == null) return null;

        Vector3 center = trainTransform != null ? trainTransform.position : Vector3.zero;
        float rad = angleDeg * Mathf.Deg2Rad;
        float distance = Random.Range(spawnDistanceMin, spawnDistanceMax);
        Vector3 spawnPos = new Vector3(
            center.x + Mathf.Cos(rad) * distance,
            center.y + Mathf.Sin(rad) * distance, 0f);

        GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.data = enemyData;
            enemy.InitializeWithWaveScaling(waveNum, playerLevel, diffL);
            ApplyRouteStats(enemy);
        }
        aliveEnemyCount++;
        return enemy;
    }

    /// <summary>
    /// v6.3: 보스 패턴용 증원 스폰 ('사냥 호령' / '포효').
    /// kind: "raptor"(스팀 랩터) 또는 "ptera"(볼트 테라노돈)
    /// statMul: 증원 스탯 배율 (1보다 작으면 새끼/약체)
    /// 같은 방향에서 떼로 나타난다 (무리 연출)
    /// </summary>
    public void SpawnReinforcements(string kind, int count, float statMul)
    {
        GameObject prefab = (kind == "ptera") ? boltTeranodonPrefab : steamRaptorPrefab;
        Enemy.EnemyData ed = (kind == "ptera") ? Enemy.BoltTeranodon : Enemy.SteamRaptor;

        if (prefab == null)
        {
            Debug.LogWarning("[WaveManager] 증원 프리팹 미할당: " + kind);
            return;
        }

        int playerLevel = GameManager.Instance != null ? GameManager.Instance.playerLevel : 1;
        float baseAngle = Random.Range(0f, 360f);

        for (int i = 0; i < count; i++)
        {
            Enemy e = SpawnEnemyAt(prefab, ed, currentWaveNumber, playerLevel,
                GameBalance.EnemyDifficultyL, baseAngle + Random.Range(-30f, 30f));

            if (e != null && !Mathf.Approximately(statMul, 1f))
            {
                e.currentHP *= statMul;
                e.scaledMaxHP = e.currentHP;
                e.scaledATK *= statMul;
            }
        }

        Debug.Log("[WaveManager] 보스 증원 스폰: " + kind + " x" + count + " (배율 " + statMul + ")");
    }

    /// <summary>v6.2: 위험 선로 등 - 이번 웨이브 적 스탯 배율 적용</summary>
    private void ApplyRouteStats(Enemy enemy)
    {
        if (activeRoute == null || Mathf.Approximately(activeRoute.statMul, 1f)) return;

        enemy.currentHP *= activeRoute.statMul;
        enemy.scaledMaxHP = enemy.currentHP;
        enemy.scaledATK *= activeRoute.statMul;
    }

    private void SpawnEnemy(GameObject prefab, Enemy.EnemyData enemyData, int waveNum, int playerLevel, float diffL)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[WaveManager] '" + enemyData.enemyName + "' 프리팹 미할당 - 스킵");
            return;
        }

        Vector3 spawnPos = GetRandomSpawnPosition();
        GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);

        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.data = enemyData;
            enemy.InitializeWithWaveScaling(waveNum, playerLevel, diffL);
            ApplyRouteStats(enemy);
        }

        aliveEnemyCount++;
    }

    private void SpawnBoss()
    {
        if (bossPrefab == null)
        {
            Debug.LogWarning("[WaveManager] 보스 프리팹 미할당");
            return;
        }

        Vector3 spawnPos = GetRandomSpawnPosition();
        Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        aliveEnemyCount++;
        Debug.Log("[WaveManager] 보스 메카 티렉스 등장!");
    }

    // ─────────────────────────────────────────────
    // 매 프레임: 생존 적 수 체크
    // ─────────────────────────────────────────────
    private void Update()
    {
        // [치트] B키: 다음 보스 웨이브로 점프 (테스트용 - 빌드 전 debugBossJumpEnabled false)
        if (debugBossJumpEnabled && Input.GetKeyDown(KeyCode.B))
        {
            JumpToNextBossWave();
            return;
        }

        if (!isWaveActive) return;

        Enemy[] remainingEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        if (remainingEnemies.Length == 0 && aliveEnemyCount > 0)
            OnAllEnemiesDefeated();
    }

    /// <summary>
    /// [치트] 진행 중인 웨이브를 정리하고 다음 보스 웨이브를 바로 시작한다.
    /// 보상/증강/선로 없이 건너뛰므로 순수 보스 테스트용.
    /// </summary>
    private void JumpToNextBossWave()
    {
        if (GameManager.Instance == null) return;

        // 다음 보스 웨이브 탐색
        int target = GameManager.Instance.currentWave + 1;
        while (!GameBalance.IsBossWave(target) && target < GameBalance.FinalWave)
            target++;

        // 진행 중인 스폰/자동진행 코루틴 중단 + 남은 적 제거 (보상 없이 조용히)
        StopAllCoroutines();
        Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
            Destroy(all[i].gameObject);

        isWaveActive = false;
        aliveEnemyCount = 0;
        pendingRoute = null;
        activeRoute = null;

        // 웨이브 카운터를 보스 직전으로 두고 바로 시작
        // (currentState 직접 변경: Town 보상 지급 없이 Battle 가드만 통과)
        GameManager.Instance.currentWave = target - 1;
        GameManager.Instance.currentState = GameManager.GameState.Town;
        GameManager.Instance.OnClickNextWave();

        UIManager.Instance?.ShowWaveNotice("[치트] 보스 점프", "웨이브 " + target + " 즉시 시작!");
        Debug.Log("[WaveManager] 치트 - 보스 웨이브 " + target + "로 점프");
    }

    // ─────────────────────────────────────────────
    // 웨이브 완료 처리 (v2: 증강 선택 -> 기존 흐름 -> 자동 진행)
    // ─────────────────────────────────────────────
    private void OnAllEnemiesDefeated()
    {
        isWaveActive = false;
        aliveEnemyCount = 0;
        SoundManager.Play("sfx_wave_clear");
        Debug.Log("[WaveManager] 웨이브 " + currentWaveNumber + " 모든 적 처치 완료!");

        // v6.2: 분기 선로 클리어 보상 정산
        int journalNo = -1;
        if (activeRoute != null)
        {
            if (activeRoute.rewardGold > 0)
            {
                GameManager.Instance?.AddGold(activeRoute.rewardGold);
                UIManager.Instance?.ShowStatChange("[선로 보상] 골드 +" + activeRoute.rewardGold);
            }
            if (activeRoute.rewardMats > 0 && MaterialInventory.Instance != null)
            {
                for (int i = 0; i < activeRoute.rewardMats; i++)
                    MaterialInventory.Instance.Add((MaterialType)Random.Range(0, 6), 1);
                UIManager.Instance?.ShowStatChange("[선로 보상] 랜덤 재료 +" + activeRoute.rewardMats);
            }
            if (activeRoute.journal)
                journalNo = MetaProgress.PickUncollectedJournal();

            activeRoute = null;
        }

        // v6: 최종전 클리어 -> 증강 없이 바로 승리 처리
        if (currentWaveNumber >= GameBalance.FinalWave)
        {
            GameManager.Instance?.OnWaveCleared();
            return;
        }

        // 웨이브 속성에 맞는 재료 드롭 보장
        GuaranteeAttributeDrop(currentWaveNumber);

        // v6.2: 폐역에서 일지 발견 -> 전문 연출이 닫힌 뒤 증강 선택으로
        if (journalNo > 0)
        {
            MetaProgress.CollectJournal(journalNo);
            StoryTexts.ShowJournal(journalNo, delegate { OpenAugmentPick(); });
            return;
        }

        OpenAugmentPick();
    }

    /// <summary>증강 3택1을 띄우고, 선택이 끝나면 기존 흐름을 이어간다</summary>
    private void OpenAugmentPick()
    {
        if (AugmentPickUI.Instance != null)
        {
            AugmentPickUI.Instance.OnWaveCleared(currentWaveNumber, delegate { AfterAugmentPick(); });
        }
        else
        {
            // 증강 UI가 씬에 없으면 기존 흐름 그대로
            AfterAugmentPick();
        }
    }

    /// <summary>증강 선택 완료 후: GameManager 흐름 + (보스 직전 스피노) + 분기 선로 + 자동 시작</summary>
    private void AfterAugmentPick()
    {
        GameManager.Instance?.OnWaveCleared();

        if (autoProgress && currentWaveNumber < maxWave)
        {
            int nextWave = currentWaveNumber + 1;

            // Phase 2-1: 보스 직전 정차에는 도박사 스피노가 먼저 다가온다 (베팅 후 선로 선택)
            if (GameBalance.IsBossWave(nextWave))
                SpinoBetUI.Show(nextWave, delegate { ProceedRouteChoice(nextWave); });
            else
                ProceedRouteChoice(nextWave);
        }
    }

    /// <summary>분기 선로 선택 -> 자동 웨이브 시작 (스피노 이후의 기존 체인)</summary>
    private void ProceedRouteChoice(int nextWave)
    {
        // v6.5 (감사 2-A): 선로 선택 빈도 1/2 - 메뉴 피로 완화
        // 짝수 웨이브 앞 + 보스 직전에만 선택, 나머지는 자동 '곧은 선로'
        bool routeChoice = BranchRouteUI.Instance != null
            && (nextWave % 2 == 0 || GameBalance.IsBossWave(nextWave));

        if (routeChoice)
        {
            BranchRouteUI.Instance.ShowRoutes(nextWave, delegate (RouteData route)
            {
                pendingRoute = route;
                StartCoroutine(AutoNextWaveCoroutine(nextWave));
            });
        }
        else
        {
            pendingRoute = null;   // 곧은 선로
            StartCoroutine(AutoNextWaveCoroutine(nextWave));
        }
    }

    /// <summary>정비 시간 후 다음 웨이브 자동 시작</summary>
    private IEnumerator AutoNextWaveCoroutine(int nextWave)
    {
        Debug.Log("[WaveManager] " + autoProgressDelay + "초 후 웨이브 " + nextWave + " 자동 시작");
        UIManager.Instance?.ShowWaveNotice("정비 시간", autoProgressDelay + "초 후 웨이브 " + nextWave + " 시작!");

        yield return new WaitForSeconds(autoProgressDelay);

        // 그 사이 다른 경로로 이미 웨이브가 시작됐다면 중복 시작하지 않는다
        if (isWaveActive)
        {
            Debug.Log("[WaveManager] 이미 다른 경로로 웨이브가 시작됨 - 자동 시작 스킵");
            yield break;
        }

        // GameManager를 경유해야 currentWave 카운트와 Battle 상태가 같이 올라간다
        if (GameManager.Instance != null)
            GameManager.Instance.OnClickNextWave();
        else
            StartWave(nextWave);   // GameManager 없는 테스트 씬용 폴백
    }

    /// <summary>
    /// v5.2: 웨이브 종료 시 보유량이 0인 재료를 1개 보장 지급.
    /// (구 문자열 Inventory 기반 -> MaterialInventory 6종 기반으로 교체)
    /// 웨이브 구간에 따라 우선순위가 다르다:
    ///  - 초반(~10): 고기 / 화염 위주 (기초 요리 재료)
    ///  - 중반(~20): 전기 / 독 (제어 계열 해금 구간)
    ///  - 후반(21~): 냉기 / 장갑 (상위 조합 구간)
    /// 우선순위 목록에서 "보유량 0"인 첫 재료만 1개 지급하므로
    /// 파밍을 잘하는 플레이어에게는 아무것도 주지 않는다 (밸런스 유지).
    /// </summary>
    private void GuaranteeAttributeDrop(int waveNum)
    {
        if (MaterialInventory.Instance == null) return;

        // v6: 지역 기반 우선순위 목록 구성
        int region = GameBalance.RegionOf(waveNum);
        MaterialType[] priority;
        if (region == 1)
            priority = new MaterialType[] { MaterialType.Meat, MaterialType.Fire };
        else if (region == 2)
            priority = new MaterialType[] { MaterialType.Elec, MaterialType.Poison, MaterialType.Meat };
        else
            priority = new MaterialType[] { MaterialType.Ice, MaterialType.Armor, MaterialType.Fire };

        for (int i = 0; i < priority.Length; i++)
        {
            if (MaterialInventory.Instance.Get(priority[i]) > 0) continue;

            MaterialInventory.Instance.Add(priority[i], 1);
            Debug.Log("[WaveManager] 재료 보장 지급: " + priority[i]);
            UIManager.Instance?.ShowStatChange("보급: 부족한 재료 1개 지급!");
            return; // 한 번에 1종만 지급
        }
    }

    // ─────────────────────────────────────────────
    // 랜덤 스폰 위치 계산
    // ─────────────────────────────────────────────
    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 center = trainTransform != null ? trainTransform.position : Vector3.zero;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(spawnDistanceMin, spawnDistanceMax);

        float x = center.x + Mathf.Cos(angle) * distance;
        float y = center.y + Mathf.Sin(angle) * distance;

        return new Vector3(x, y, 0f);
    }

    // ─────────────────────────────────────────────
    // 웨이브 구성 조회
    // ─────────────────────────────────────────────
    private WaveConfig GetWaveConfig(int waveNumber)
    {
        WaveConfig config = waveConfigs.Find(w => w.waveNumber == waveNumber);

        // 해당 웨이브 없으면 마지막 구성 기반으로 자동 생성
        if (config.waveNumber == 0 && waveConfigs.Count > 0)
        {
            config = waveConfigs[waveConfigs.Count - 1];
            config.waveNumber = waveNumber;
            config.steamRaptorCount += waveNumber;
        }

        return config;
    }

    // ─────────────────────────────────────────────
    // 기본 웨이브 구성 자동 생성 (v6: 3지역 구조)
    // 지역 길이가 바뀌어도(테스트 3 / 정식 8) 물량 곡선이 유지되도록
    // "지역 내 진행도 p(0~1)" 기준으로 계산한다.
    // ─────────────────────────────────────────────
    private void GenerateDefaultWaveConfigs(int totalWaves)
    {
        waveConfigs = new List<WaveConfig>();

        for (int i = 1; i <= totalWaves; i++)
        {
            WaveConfig config = new WaveConfig();
            config.waveNumber = i;
            config.spawnInterval = Mathf.Max(0.3f, 1.0f - (i * 0.03f));
            config.difficultyL = GameBalance.EnemyDifficultyL; // 난이도 계수 (GameBalance에서 조정)
            config.hasBoss = GameBalance.IsBossWave(i);        // 각 지역 마지막 + 최종전

            int region = GameBalance.RegionOf(i);
            float p = GameBalance.RegionProgress(i);           // 지역 내 진행도 0~1

            if (region == 1)
            {
                // 지역 1: 구리 사막 - 물리 물량 + 러시. 도구 파괴꾼(전갈)로 정비 압박
                config.steamRaptorCount = 3 + Mathf.RoundToInt(p * 12f);       // 4~15 (6+ 시 무리 러시)
                config.springAnkyloCount = p >= 0.4f ? Mathf.RoundToInt(p * 4f) : 0;
                config.oilCactusCount = p >= 0.6f ? Mathf.RoundToInt(p * 3f) : 0;
                config.scorpionCount = p >= 0.5f ? Mathf.RoundToInt(p * 2f) : 0;
                config.tortoiseCount = p >= 0.85f ? 1 : 0;
            }
            else if (region == 2)
            {
                // 지역 2: 테슬라 협곡 - 전기/독/공중. 조리 방해(프테라)와 원거리전 강제
                config.steamRaptorCount = 4;
                config.boltTeranodonCount = 1 + Mathf.RoundToInt(p * 5f);
                config.poisonPteraCount = p >= 0.3f ? Mathf.RoundToInt(p * 3f) : 0;
                config.overloadFlyCount = p >= 0.5f ? Mathf.RoundToInt(p * 4f) : 0;
                config.magnetParasaurCount = p >= 0.75f ? 1 : 0;
                config.steelRaptorCount = p >= 0.4f ? Mathf.RoundToInt(p * 2f) : 0;
                config.flamePteroCount = p >= 0.6f ? Mathf.RoundToInt(p * 2f) : 0;
            }
            else if (region == 3)
            {
                // 지역 3: 코발트 광산 - 냉기/장갑 정예 + 힐러. 속성 대응과 집중 사격 강제
                config.steamRaptorCount = 3;
                config.boltTeranodonCount = 2;
                config.iceMosaCount = 1 + Mathf.RoundToInt(p * 3f);
                config.crystalPachyCount = p >= 0.35f ? Mathf.RoundToInt(p * 2f) : 0;
                config.magmaCarnoCount = p >= 0.55f ? Mathf.RoundToInt(p * 2f) : 0;
                config.frostMammothCount = p >= 0.85f ? 1 : 0;
                config.steelRaptorCount = 2;
                config.necroSpinoCount = p >= 0.5f ? (p >= 0.9f ? 2 : 1) : 0;
            }
            else
            {
                // 최종전: 정예 호위 소수 + 최종 보스
                config.iceMosaCount = 2;
                config.magmaCarnoCount = 2;
                config.steelRaptorCount = 2;
                config.necroSpinoCount = 1;
            }

            waveConfigs.Add(config);
        }

        Debug.Log("[WaveManager] " + totalWaves + "웨이브 구성 생성 완료 (지역 길이 "
            + GameBalance.RegionLength + ", 보스 " + GameBalance.RegionLength + "/"
            + (GameBalance.RegionLength * 2) + "/" + (GameBalance.RegionLength * 3)
            + ", 최종전 " + GameBalance.FinalWave + ")");
    }

    // ─────────────────────────────────────────────
    // Scene 뷰 스폰 범위 시각화
    // ─────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Vector3 center = trainTransform != null ? trainTransform.position : Vector3.zero;

        Gizmos.color = Color.yellow;
        DrawWireCircle(center, spawnDistanceMin);

        Gizmos.color = Color.green;
        DrawWireCircle(center, spawnDistanceMax);
    }

    private void DrawWireCircle(Vector3 center, float radius)
    {
        int segments = 36;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 p1 = center + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0f) * radius;
            Vector3 p2 = center + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0f) * radius;

            Gizmos.DrawLine(p1, p2);
        }
    }
}
