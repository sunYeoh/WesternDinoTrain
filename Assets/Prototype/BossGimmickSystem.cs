using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [BossGimmickSystem.cs] v9.19 (2026-10-06 보스 페이즈 모습 A4: 보스 쪽 알림 한 줄(ShowBossLine - 페이즈 전환·발악·대응법)을 가운데 예고 카드 대신 HP 바 밑 띠에 띄운다. 띠는 무방비 > 패턴 예고 > 알림 순으로 쓴다 / 패턴 예고·무방비 시작의 화면 가운데 큰 글자를 뺐다(띠와 같은 말이 보스 몸 위에 한 번 더 떴다) - 가장자리 맥동과 띠 등장 강조만. GameBalance.BossNoticeInBar) /
/// v9.18 (2026-10-06 테스터 피드백 3: 패턴 예고·무방비 띠를 HP 바 바로 밑에 붙였다(폭도 바와 같게, 긴 안내는 글자를 줄여 한 줄 - 구 자리는 보스가 서는 높이라 몸을 가렸다) + 띠가 떠 있으면 가운데 예고 카드를 그 아래로 / 보스 HP 바를 화면 위 가운데(기차 상황판 바로 아래)로 올리고 키웠다 - 이름 28 / 수치 20 / 바 30, 깎인 만큼 밝은 띠가 남았다 줄어든다, 무방비 눈금, 상태 딱지(무방비·빙하 갑주·해치 개방·폭식·발악·번개 병), 발악하면 바 색이 달아오른다 / 등장: HoldBarForIntro -> PlayBarIntro 로 위에서 내려와 차오른다 / 바가 떠 있는 동안 가운데 예고 카드를 그 아래로 민다(UISkin.NoticeShiftY). GameBalance.BossBarBig = false 면 구 배치) / v9.16 (2026-09-29 손맛 2차 - 소리: 보스전 동안 배경음 덕킹 - 등록에 켜고 처치·정리에 끈다) / v9.12 (2026-09-22: ClearBossUI - 예습 보스용) / v4.1
/// 보스전 전용 기믹 + 보스 UI를 관리합니다.
///
/// - v4.1 (교수 피드백 A6, 2026-09-14): 씬에 이 컴포넌트가 없으면 자동 생성한다.
///   저장소 씬(08-25 커밋)에는 부착돼 있지 않았고, 호출부가 전부 Instance?. 라 보스 HP 바·그로기·[F] 투척·
///   미끼/해동포/마지막 주문·베팅 정산이 오류 없이 통째로 빠지고 있었다. 이제 씬 의존 없음.
///
/// - v4 변경점 (UI 재작성):
///   하이어라키 수동 패널 전부 제거 -> UI를 코드로 자동 생성 (겹침 문제 해결)
///   * 상단 중앙: 보스 이름 + HP 바 + 수치
///   * 그 아래: 그로기 배너 (안내 문구 + 남은 시간 게이지)
///   씬 세팅 필요 없음 - 기존 보스 HP/그로기 패널은 하이어라키에서 삭제할 것
///
/// 동작 흐름:
///   보스 HP 75/50/25% 도달 -> 그로기 발동 (groggyDuration 7초 정지, 디 오리지널은 12%에 한 번 더 - C3)
///   그로기 중 보유한 디버프 요리를 자동 탐색해 표시
///   F키 -> FoodStock에서 1개 소모 -> 보스 DEF/RES 무력화
///
/// 사용법: 없음 - 씬에 없으면 스스로 생성된다 (v4.1). 붙여 두어도 무방 (중복 생성 안 함)
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class BossGimmickSystem : MonoBehaviour
{
    public static BossGimmickSystem Instance { get; private set; }

    /// <summary>
    /// v4.1: 씬에 없으면 스스로 생성 (다른 자동 생성 시스템과 같은 방식).
    /// 런 포기/재도전은 씬을 다시 불러오므로(자동 생성 오브젝트도 함께 사라진다) 씬이 로드될 때마다 다시 확인한다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureExists();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureExists();
    }

    private static void EnsureExists()
    {
        if (Instance != null) return;
        if (FindFirstObjectByType<BossGimmickSystem>() != null) return;
        GameObject go = new GameObject("BossGimmickSystem(auto)");
        go.AddComponent<BossGimmickSystem>();
        Debug.Log("[BossGimmickSystem] 씬에 없어 자동 생성 (v4.1)");
    }

    [Header("─ 설정 ─")]
    public float groggyDuration = 7f;         // 그로기 지속 시간 (BossEnemy.groggyDuration 7초와 동일 - v4.1에서 10 -> 7 동기화)

    // ─────────────────────────────────────────────
    // 내부 상태
    // ─────────────────────────────────────────────
    private BossEnemy currentBoss = null;
    private bool isGroggyPhase = false;
    private float groggyTimer = 0f;
    private bool hasThrownThisGroggy = false;
    private float guideRefreshTimer = 0f;

    // ─────────────────────────────────────────────
    // 코드 생성 UI
    // ─────────────────────────────────────────────
    private Canvas canvas;
    private RectTransform bossRoot;       // 보스 이름 + HP 바
    private Text bossNameText;
    private Text bossHPText;
    private RectTransform hpFill;
    private RectTransform groggyRoot;     // 그로기 배너
    private Text groggyGuideText;
    private RectTransform groggyTimeFill;
    private Image groggyTimeFillImg;

    // ── v9.18: 큰 HP 바 (GameBalance.BossBarBig) ──
    private const float BIG_BAR_Y = -46f;       // 기차 상황판(위에서 8 ~ 42) 바로 아래
    private const float BIG_BAR_H = 82f;
    private const float GROGGY_H = 66f;         // 무방비 띠 높이 (안내 한 줄 + 남은 시간 게이지)
    private const float GROGGY_GAP = 4f;        // HP 바와 무방비 띠 사이
    private RectTransform hpBarArea;            // 채움·잔상·눈금·수치가 들어가는 바 영역
    private RectTransform hpTrail;              // 깎인 만큼 남았다 줄어드는 밝은 띠
    private Image hpFillImg;
    private Text bossTagText;                   // 상태 딱지 (이름 줄 오른쪽)
    private readonly List<GameObject> hpTicks = new List<GameObject>();   // 무방비 눈금
    private float trailRatio = 1f;
    private float lastRatio = 1f;
    private float trailHoldUntil = 0f;
    private bool barHeld = false;               // 등장 연출이 내려줄 때까지 숨김
    private float barHeldSince = 0f;
    private float introFill = 1f;               // 등장 중 차오르는 비율 (1 = 다 참)
    private Coroutine barIntroCo;
    private static readonly Color HP_RED = new Color(0.85f, 0.2f, 0.15f);
    private static readonly Color HP_RAGE = new Color(1f, 0.38f, 0.1f);

    // ── v9.19 (A4): 보스 알림 한 줄 - 띠의 세 번째 쓰임 (무방비 > 패턴 예고 > 알림) ──
    private Text lineBodyText;                  // 띠 아랫줄 (알림의 본문). 예고·무방비일 때는 그 자리에 남은 시간 게이지가 있다
    private GameObject timeBarGo;               // 남은 시간 게이지
    private string lineTitle = "", lineBody = "";
    private float lineLeft = 0f;                // 알림을 더 보여 줄 시간 (실시간 초). 0 이하 = 없음
    private float lineWaited = 0f;              // 띠가 비기를 기다린 시간
    private float lineMaxWait = 12f;            // 이만큼 기다려도 띠가 안 비면 알림을 버린다 (알림마다 다르다 - ShowBossLine)
    private bool lineShowing = false;           // 지금 띠가 알림을 보여 주는 중
    private float bannerPunchT = -1f;           // 띠 등장 강조 (1.1배에서 제자리로 0.18초). 음수 = 없음
    private const float LINE_MIN_RESUME = 2.5f; // 예고에 밀렸다가 다시 뜰 때 최소 이만큼은 보여 준다

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    private void Start()
    {
        bossRoot.gameObject.SetActive(false);
        groggyRoot.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // 씬 리로드로 사라질 때 죽은 참조를 남기지 않는다 (Instance?. 호출부가 파괴된 오브젝트를 건드리지 않게)
        if (Instance == this) Instance = null;
    }

    // ─────────────────────────────────────────────
    // 보스 등록 (BossEnemy.Start에서 호출)
    // ─────────────────────────────────────────────
    /// <summary>보스가 살아서 활동 중인가? (주방 이벤트 차단 등 외부 참조용)</summary>
    public bool HasActiveBoss
    {
        get { return currentBoss != null && currentBoss.IsAlive; }
    }

    public void RegisterBoss(BossEnemy boss)
    {
        currentBoss = boss;
        SoundManager.BgmDuck("boss", true);   // v9.16: 보스전 - 배경음 -35%

        // Phase 2-1: 스피노 베팅 조건 추적 시작 (시간/조리/피격/투척 카운터 리셋)
        SpinoBet.OnBossStart();

        // v4.1: 그로기 시간을 보스 쪽 설정과 자동 동기화 (게이지 바 길이 불일치 방지)
        groggyDuration = boss.groggyDuration;

        bossRoot.gameObject.SetActive(true);
        // v5: 보스 4종 개성화 - 등록된 보스의 실제 이름 표시
        if (bossNameText != null) bossNameText.text = boss.data.enemyName;
        // v9.18: 새 보스 - 잔상·등장 상태 초기화 + 무방비 눈금
        if (barIntroCo != null) { StopCoroutine(barIntroCo); barIntroCo = null; }
        barHeld = false; introFill = 1f; trailRatio = 1f; lastRatio = 1f; trailHoldUntil = 0f;
        if (GameBalance.BossBarBig)
        {
            bossRoot.anchoredPosition = new Vector2(0f, BIG_BAR_Y);
            BuildTicks(boss);
        }

        // v5.1: 동면자 보스전이면 해동포 UI 자동 생성 (씬 세팅 불필요)
        if (boss.kind == BossEnemy.BossKind.Hibernator)
        {
            GameObject cannonGo = new GameObject("ThawCannon");
            cannonGo.AddComponent<ThawCannonUI>().Setup(boss);
        }

        // v5.2 (C단계): 녹슨 발톱 보스전이면 미끼 화덕 자동 생성
        if (boss.kind == BossEnemy.BossKind.RustClaw)
        {
            GameObject baitGo = new GameObject("BaitStation");
            baitGo.AddComponent<BaitStationUI>().Setup(boss);
        }

        // v5.3 (C-2): 디 오리지널 보스전이면 '마지막 주문' UI 자동 생성 (엔딩 B 분기 담당)
        if (boss.kind == BossEnemy.BossKind.Original)
        {
            GameObject finalGo = new GameObject("FinalOrder");
            finalGo.AddComponent<FinalOrderUI>().Setup(boss);
        }

        Debug.Log("[BossGimmickSystem] 보스 등록 완료: " + boss.data.enemyName);
    }

    // ─────────────────────────────────────────────
    // v5: 패턴 예고(텔레그래프) 배너 - 그로기 배너 재사용
    // ─────────────────────────────────────────────
    private Coroutine telegraphCo = null;

    /// <summary>보스 패턴 예고 표시. seconds 후 자동으로 숨김 (그로기가 시작되면 그로기가 우선)</summary>
    public void ShowPatternTelegraph(string text, float seconds)
    {
        if (isGroggyPhase) return;   // 그로기 안내가 우선

        TakeBannerFromLine();   // v9.19: 알림 한 줄이 떠 있었으면 내린다 (예고가 먼저)
        groggyRoot.gameObject.SetActive(true);
        if (groggyGuideText != null) groggyGuideText.text = text;
        if (groggyTimeFillImg != null) groggyTimeFillImg.color = new Color(0.8f, 0.3f, 0.9f); // 보라 = 패턴 예고
        SetFill(groggyTimeFill, 1f);
        SoundManager.Play("sfx_boss_warning");   // 예고 경보음

        // v5.2 (감사 2-D): 화면 가장자리 붉은 플래시 + 대형 경고 - 예고가 눈에 확 들어오게
        // v9.19: 띠가 HP 바 밑으로 올라온 뒤로 큰 글자는 띠와 같은 말을 보스 몸 위에 한 번 더 띄웠다 - 가장자리 맥동과 띠 강조만 남긴다
        if (BarNotices) { WarningFX.FlashEdges(seconds, new Color(1f, 0.15f, 0.1f)); bannerPunchT = 0f; }
        else WarningFX.Flash(text, seconds);

        if (telegraphCo != null) StopCoroutine(telegraphCo);
        telegraphCo = StartCoroutine(TelegraphCountdown(seconds));
    }

    private IEnumerator TelegraphCountdown(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            if (isGroggyPhase) yield break;   // 그로기 시작되면 배너 넘겨줌
            SetFill(groggyTimeFill, 1f - Mathf.Clamp01(t / seconds));
            yield return null;
        }

        if (!isGroggyPhase)
            groggyRoot.gameObject.SetActive(false);
        telegraphCo = null;
    }

    // ─────────────────────────────────────────────
    // 매 프레임
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (currentBoss == null || !currentBoss.IsAlive)
        {
            if (bossRoot.gameObject.activeSelf) bossRoot.gameObject.SetActive(false);
            if (groggyRoot.gameObject.activeSelf) groggyRoot.gameObject.SetActive(false);
            UISkin.NoticeShiftY = 0f;   // v9.18
            if (lineLeft > 0f || lineShowing) ResetBossLine();   // v9.19
            return;
        }

        // v9.18: 등장 연출이 내려줄 때까지는 숨겨 둔다 (8초 넘게 안 불리면 그냥 띄운다)
        if (barHeld)
        {
            // 첫 등장 카드·스토리 글·증강 창·일시정지가 떠 있는 동안은 세지 않는다 (등장 연출도 그동안 기다린다 - BossEnemy.EntranceRoutine)
            if (BriefingUI.IsOpen || StoryTexts.IsBlocking || AugmentPickUI.IsOpen || PauseMenu.IsOpen) barHeldSince = Time.unscaledTime;
            else if (Time.unscaledTime - barHeldSince > 8f) { barHeld = false; bossRoot.gameObject.SetActive(true); }
        }
        // 큰 바가 떠 있는 동안 가운데 예고 카드(위에서 96)는 바 아래로 내린다. 무방비 띠까지 떠 있으면 그 아래로
        float noticeShift = 0f;
        if (GameBalance.BossBarBig && bossRoot.gameObject.activeSelf)
        {
            noticeShift = -BIG_BAR_Y + BIG_BAR_H + 6f - 96f;
            if (groggyRoot.gameObject.activeSelf) noticeShift += GROGGY_GAP + GROGGY_H;
        }
        UISkin.NoticeShiftY = noticeShift;

        UpdateBossHPBar();

        if (!isGroggyPhase && currentBoss.IsGroggy)
            StartGroggyPhase();

        if (isGroggyPhase)
            UpdateGroggyPhase();

        TickBossLine();
        TickBannerPunch();
    }

    // ─────────────────────────────────────────────
    // v9.19 (A4): 보스 알림 한 줄
    // ─────────────────────────────────────────────
    /// <summary>보스 쪽 알림을 띠에 띄우는 설정인가 (큰 HP 바 + 스위치)</summary>
    private static bool BarNotices { get { return GameBalance.BossBarBig && GameBalance.BossNoticeInBar; } }

    /// <summary>
    /// 보스 쪽 알림(페이즈 전환·발악·대응법)을 HP 바 밑 띠에 띄운다. 띠를 패턴 예고·무방비가 쓰는 중이면 비는 대로 뜬다.
    /// maxWait = 띠가 비기를 기다리는 한도(초) - 넘으면 버린다. 그 순간에만 뜻이 있는 알림(발악)은 짧게, 대응법 안내는 무방비(7초)가 끝날 때까지 기다리게 길게.
    /// false = 띠에 못 띄운다 (HP 바가 없거나 스위치가 꺼졌다) - 부른 쪽이 가운데 예고 카드로 띄운다
    /// </summary>
    public bool ShowBossLine(string title, string body, float seconds, float maxWait)
    {
        if (!BarNotices || lineBodyText == null) return false;
        if (currentBoss == null || !currentBoss.IsAlive) return false;
        lineTitle = title ?? "";
        lineBody = body ?? "";
        lineLeft = Mathf.Max(1f, seconds);
        lineWaited = 0f;
        lineMaxWait = Mathf.Max(0.5f, maxWait);
        if (lineShowing) ApplyLineText();   // 떠 있던 알림을 새 것으로 바꾼다
        return true;
    }

    private void ApplyLineText()
    {
        if (groggyGuideText != null) groggyGuideText.text = lineTitle;
        lineBodyText.text = lineBody;
        bannerPunchT = 0f;
    }

    /// <summary>띠를 알림 모양(제목 + 본문)과 예고 모양(안내 + 남은 시간 게이지) 사이에서 바꾼다</summary>
    private void SetBannerLineMode(bool line)
    {
        if (timeBarGo != null) timeBarGo.SetActive(!line);
        if (lineBodyText != null) lineBodyText.gameObject.SetActive(line);
        if (groggyGuideText != null)
        {
            // 알림일 때는 제목 줄을 조금 올려 본문 두 줄(아래 30)과 안 닿게 한다
            RectTransform rt = groggyGuideText.rectTransform;
            rt.anchoredPosition = new Vector2(0f, line ? -3f : -6f);
            rt.sizeDelta = new Vector2(-20f, line ? 28f : 32f);
        }
    }

    /// <summary>패턴 예고·무방비가 띠를 가져간다. 알림이 거의 다 보였으면 버리고, 아니면 띠가 빈 뒤에 다시 띄운다</summary>
    private void TakeBannerFromLine()
    {
        if (lineShowing)
        {
            lineShowing = false;
            lineLeft = lineLeft < 1.2f ? 0f : Mathf.Max(lineLeft, LINE_MIN_RESUME);
            lineWaited = 0f;
        }
        SetBannerLineMode(false);
    }

    private void TickBossLine()
    {
        if (lineBodyText == null) return;
        bool busy = isGroggyPhase || telegraphCo != null;      // 예고·무방비가 띠를 쓰는 중
        if (lineShowing)
        {
            if (busy) { TakeBannerFromLine(); return; }
            lineLeft -= Time.unscaledDeltaTime;
            if (lineLeft <= 0f)
            {
                lineShowing = false;
                SetBannerLineMode(false);
                groggyRoot.gameObject.SetActive(false);
            }
            return;
        }
        if (lineLeft <= 0f) return;
        if (busy || !bossRoot.gameObject.activeSelf)
        {
            lineWaited += Time.unscaledDeltaTime;
            if (lineWaited > lineMaxWait) lineLeft = 0f;
            return;
        }
        lineShowing = true;
        SetBannerLineMode(true);
        ApplyLineText();
        groggyRoot.gameObject.SetActive(true);
    }

    /// <summary>띠가 새 내용으로 뜨는 순간: 1.1배에서 제자리로 0.18초 (큰 글자를 뺀 대신 띠 자체가 눈에 들어오게)</summary>
    private void TickBannerPunch()
    {
        if (bannerPunchT < 0f) return;
        bannerPunchT += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(bannerPunchT / 0.18f);
        float s = 1f + 0.1f * (1f - k) * (1f - k) * Mathf.Clamp01(GameBalance.GameFeelMaster);
        groggyRoot.localScale = new Vector3(s, s, 1f);
        if (k >= 1f) { bannerPunchT = -1f; groggyRoot.localScale = Vector3.one; }
    }

    private void UpdateBossHPBar()
    {
        float ratio = Mathf.Clamp01(currentBoss.currentHP / Mathf.Max(1f, currentBoss.bossMaxHP));

        if (!GameBalance.BossBarBig)
        {
            // 구 배치 (v9.17 까지)
            SetFill(hpFill, ratio);
            if (bossHPText != null)
                bossHPText.text = (int)currentBoss.currentHP + " / " + (int)currentBoss.bossMaxHP;
            // v5.1: 천둥 둥지 - 번개 병 충전 수 표시
            if (bossNameText != null && currentBoss.kind == BossEnemy.BossKind.ThunderNest)
                bossNameText.text = currentBoss.data.enemyName + "   [번개 병 "
                    + currentBoss.ParryCharges + "/" + GameBalance.ParryChargesForCounter + "]";
            return;
        }

        // ── v9.18 큰 바 ──
        // 잔상: 맞으면 0.25초 머물렀다가 실제 HP 로 줄어든다 (큰 한 방일수록 띠가 넓게 남는다). 회복(폭식)은 바로 따라간다
        if (ratio < lastRatio - 0.0001f) trailHoldUntil = Time.time + 0.25f;
        lastRatio = ratio;
        if (trailRatio < ratio) trailRatio = ratio;
        else if (Time.time >= trailHoldUntil)
        {
            float speed = Mathf.Max(0.12f, (trailRatio - ratio) / Mathf.Max(0.05f, GameBalance.BossBarTrailSec));
            trailRatio = Mathf.MoveTowards(trailRatio, ratio, speed * Time.deltaTime);
        }
        SetFill(hpFill, ratio * introFill);
        SetFill(hpTrail, trailRatio * introFill);

        if (bossHPText != null)
            bossHPText.text = Mathf.CeilToInt(currentBoss.currentHP).ToString("#,0") + " / " + Mathf.RoundToInt(currentBoss.bossMaxHP).ToString("#,0");

        // 바 색: 발악하면 달아오른 주황으로 맥동
        if (hpFillImg != null)
        {
            if (currentBoss.IsEnraged && GameBalance.BossEnrageSignal)
                hpFillImg.color = Color.Lerp(HP_RED, HP_RAGE, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 7f));
            else hpFillImg.color = HP_RED;
        }

        // 상태 딱지: 지금 보스가 어떤 상태인지 한 줄 (무방비 > 빙하 갑주 > 해치 개방 > 폭식, 발악·번개 병은 덧붙인다)
        if (bossTagText != null)
        {
            string tag = "";
            Color tagCol = new Color(1f, 0.82f, 0.4f);
            if (currentBoss.IsGroggy) tag = "무방비";
            else if (currentBoss.ArmorActive) { tag = "빙하 갑주"; tagCol = new Color(0.55f, 0.9f, 1f); }
            else if (currentBoss.HatchOpen) { tag = "해치 개방"; tagCol = new Color(1f, 0.9f, 0.7f); }
            else if (currentBoss.kind == BossEnemy.BossKind.Original && currentBoss.OriginalPhaseNow == 2) { tag = "폭식"; tagCol = new Color(1f, 0.6f, 0.3f); }
            if (currentBoss.IsEnraged)
            {
                if (tag == "") tagCol = new Color(1f, 0.4f, 0.25f);
                tag = tag == "" ? "발악" : tag + " · 발악";
            }
            if (currentBoss.kind == BossEnemy.BossKind.ThunderNest)
                tag = "번개 병 " + currentBoss.ParryCharges + "/" + GameBalance.ParryChargesForCounter + (tag == "" ? "" : " · " + tag);
            bossTagText.text = tag;
            bossTagText.color = tagCol;
        }
    }

    // ─────────────────────────────────────────────
    // v9.18: 등장 연출 연동 (BossEnemy 가 부른다)
    // ─────────────────────────────────────────────
    /// <summary>등장 연출이 있는 보스: 포효 순간(PlayBarIntro)까지 HP 바를 숨겨 둔다. 8초 안에 안 불리면 그냥 띄운다 (안전장치)</summary>
    public void HoldBarForIntro()
    {
        if (!GameBalance.BossBarBig || bossRoot == null) return;
        barHeld = true;
        barHeldSince = Time.unscaledTime;
        bossRoot.gameObject.SetActive(false);
    }

    /// <summary>HP 바가 위에서 내려와(0.22초) 0 에서 지금 HP 까지 차오른다(0.7초). 실시간</summary>
    public void PlayBarIntro()
    {
        if (!GameBalance.BossBarBig || bossRoot == null || currentBoss == null) return;
        barHeld = false;
        bossRoot.gameObject.SetActive(true);
        if (GameBalance.GameFeelMaster <= 0f) { introFill = 1f; return; }
        if (barIntroCo != null) StopCoroutine(barIntroCo);
        barIntroCo = StartCoroutine(BarIntroRoutine());
    }

    private IEnumerator BarIntroRoutine()
    {
        introFill = 0f;
        Vector2 home = new Vector2(0f, BIG_BAR_Y);
        Vector2 from = home + new Vector2(0f, BIG_BAR_H + 60f);   // 화면 위 밖
        float t = 0f;
        while (t < 0.22f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.22f);
            float e = 1f - (1f - k) * (1f - k);
            bossRoot.anchoredPosition = Vector2.Lerp(from, home, e);
            yield return null;
        }
        bossRoot.anchoredPosition = home;
        t = 0f;
        while (t < 0.7f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.7f);
            introFill = 1f - (1f - k) * (1f - k);
            yield return null;
        }
        introFill = 1f;
        barIntroCo = null;
    }

    /// <summary>무방비 눈금: 이 HP 에 닿으면 보스가 무방비가 된다 (75 / 50 / 25%, 디 오리지널은 한 번 더). 예습 보스는 없음</summary>
    private void BuildTicks(BossEnemy boss)
    {
        for (int i = 0; i < hpTicks.Count; i++) if (hpTicks[i] != null) Destroy(hpTicks[i]);
        hpTicks.Clear();
        if (hpBarArea == null || boss == null || boss.practice) return;

        List<float> marks = new List<float> { 0.75f, 0.5f, 0.25f };
        if (boss.kind == BossEnemy.BossKind.Original && GameBalance.OriginalExtraGroggyRatio > 0f) marks.Add(GameBalance.OriginalExtraGroggyRatio);
        for (int i = 0; i < marks.Count; i++)
        {
            RectTransform tick = KitchenEventManager.MakeBox(hpBarArea, "TickLine" + i, new Color(0.04f, 0.02f, 0.02f, 0.85f));
            tick.anchorMin = new Vector2(marks[i], 0f);
            tick.anchorMax = new Vector2(marks[i], 1f);
            tick.pivot = new Vector2(0.5f, 0.5f);
            tick.sizeDelta = new Vector2(3f, 0f);
            tick.anchoredPosition = Vector2.zero;
            tick.GetComponent<Image>().raycastTarget = false;
            hpTicks.Add(tick.gameObject);
        }
        if (bossHPText != null) bossHPText.transform.SetAsLastSibling();   // 수치는 눈금 위에
    }

    // ─────────────────────────────────────────────
    // 그로기 페이즈
    // ─────────────────────────────────────────────
    private void StartGroggyPhase()
    {
        isGroggyPhase = true;
        groggyTimer = 0f;
        hasThrownThisGroggy = false;
        guideRefreshTimer = 0f;

        // v5: 패턴 예고가 떠 있었으면 중단하고 그로기가 배너를 가져간다
        if (telegraphCo != null) { StopCoroutine(telegraphCo); telegraphCo = null; }

        // v5: 보너스 그로기(빙하 갑주 파괴 등)는 보스가 지정한 짧은 시간 사용
        if (currentBoss != null && currentBoss.CurrentGroggyDuration > 0f)
            groggyDuration = currentBoss.CurrentGroggyDuration;

        TakeBannerFromLine();   // v9.19
        groggyRoot.gameObject.SetActive(true);
        if (groggyTimeFillImg != null) groggyTimeFillImg.color = new Color(1f, 0.55f, 0.15f);
        SoundManager.Play("sfx_boss_groggy");

        // v5.2: 그로기는 기회의 순간 - 금색 플래시로 구분. v9.19: 큰 글자는 띠의 안내와 같은 말 - 금빛 가장자리와 띠 강조만
        if (BarNotices) { WarningFX.FlashEdges(1.6f, new Color(1f, 0.8f, 0.2f)); bannerPunchT = 0f; }
        else WarningFX.Flash("보스 무방비! [F] 독샘 요리 투척!", 1.6f, new Color(1f, 0.8f, 0.2f));
        RefreshGuideText();

        Debug.Log("[BossGimmickSystem] 보스 그로기 발동! 10초 안에 디버프 요리 투척!");
        UIManager.Instance?.ShowStatChange("보스 무방비!! [F] 독샘 요리 투척!");
    }

    private void UpdateGroggyPhase()
    {
        groggyTimer += Time.deltaTime;
        SetFill(groggyTimeFill, 1f - Mathf.Clamp01(groggyTimer / groggyDuration));

        // 안내 문구 주기 갱신 (그로기 중에 요리를 새로 만들 수도 있으므로)
        guideRefreshTimer -= Time.deltaTime;
        if (guideRefreshTimer <= 0f && !hasThrownThisGroggy)
        {
            guideRefreshTimer = 1f;
            RefreshGuideText();
        }

        // v4.1: 마지막 주문 선택창([R]/[F])이 떠 있는 동안, 그리고 선택창이 F를 막 소비한 프레임에는 던지지 않는다
        if (Input.GetKeyDown(KeyCode.F) && !FinalOrderUI.QteOpen && FinalOrderUI.KeyConsumedFrame != Time.frameCount)
            TryThrowDebuffFood();

        if (!currentBoss.IsGroggy || groggyTimer >= groggyDuration)
            EndGroggyPhase(hasThrownThisGroggy);
    }

    // ─────────────────────────────────────────────
    // 디버프 요리 탐색 (FoodStock + RecipeDatabase 자동 판정)
    // ─────────────────────────────────────────────

    /// <summary>보유 요리 중 가장 좋은 디버프 요리. 없으면 null</summary>
    private RecipeData FindBestDebuffFood()
    {
        if (FoodStock.Instance == null) return null;

        RecipeData best = null;
        int bestScore = 0;

        foreach (KeyValuePair<string, int> pair in FoodStock.Instance.AllStock)
        {
            if (pair.Value <= 0) continue;

            RecipeData r = RecipeDatabase.Get(pair.Key);
            if (r == null) continue;

            int score = GetDebuffScore(r);
            if (score > bestScore)
            {
                bestScore = score;
                best = r;
            }
        }

        return best;
    }

    /// <summary>디버프 요리 점수. 0이면 디버프 요리가 아님</summary>
    private int GetDebuffScore(RecipeData r)
    {
        int score = 0;
        if (r.shredDef > 0) score += r.shredDef * 100;
        if (r.shredRes > 0) score += r.shredRes * 100;
        if (r.stunSec > 0f) score += 50;
        if (r.slowLevel >= 2) score += 30;

        if (score > 0) score += r.tier * 10;
        return score;
    }

    /// <summary>계열별 디버프 강도 (남는 방어력 비율 - 낮을수록 강력)</summary>
    private float GetDebuffPower(RecipeData r)
    {
        if (r.shredDef > 0 || r.shredRes > 0) return 0.25f;  // 부식 계열
        if (r.stunSec > 0f) return 0.40f;                    // 마비 계열
        return 0.50f;                                        // 빙결 계열
    }

    private void RefreshGuideText()
    {
        if (groggyGuideText == null || hasThrownThisGroggy) return;

        RecipeData found = FindBestDebuffFood();
        if (found != null)
            groggyGuideText.text = "무방비!  [F] " + found.displayName + " 투척!";
        else
            groggyGuideText.text = "던질 요리 없음!  독침 육포(고기+독샘)를 그릴에서 구워라!";
    }

    private void TryThrowDebuffFood()
    {
        if (hasThrownThisGroggy) return;

        RecipeData food = FindBestDebuffFood();
        if (food == null)
        {
            RefreshGuideText();
            return;
        }

        if (FoodStock.Instance == null || !FoodStock.Instance.TryConsume(food.recipeId, 1))
            return;

        hasThrownThisGroggy = true;

        float power = GetDebuffPower(food);
        currentBoss.ReceiveDebuffFood(power);

        int reducedPct = Mathf.RoundToInt((1f - power) * 100f);
        Debug.Log("[BossGimmickSystem] " + food.displayName + " 투척! 보스 방어력 " + reducedPct + "% 감소!");
        UIManager.Instance?.ShowStatChange(food.displayName + " 적중! 보스 방어력 -" + reducedPct + "%!");
        SpinoBet.CountThrowHit();   // Phase 2-1: [외상 장부] 베팅 조건 추적

        if (groggyGuideText != null)
            groggyGuideText.text = food.displayName + " 적중!  방어력 -" + reducedPct + "%";
        if (groggyTimeFillImg != null)
            groggyTimeFillImg.color = new Color(0.25f, 0.9f, 0.3f);
    }

    private void EndGroggyPhase(bool wasDebuffed)
    {
        isGroggyPhase = false;
        groggyRoot.gameObject.SetActive(false);

        if (!wasDebuffed)
        {
            Debug.Log("[BossGimmickSystem] 그로기 미투척 - 기회를 놓쳤다!");
            UIManager.Instance?.ShowStatChange("투척 실패! 무방비 기회를 놓쳤다!");
        }
    }

    // ─────────────────────────────────────────────
    // 보스 처치 시 정리
    // ─────────────────────────────────────────────
    /// <summary>v9.12: 예습 보스(새끼 발톱)가 사라질 때 - HP 바·무방비 배너만 내린다 (승리 알림·베팅 정산 없음)</summary>
    public void ClearBossUI()
    {
        currentBoss = null;
        ResetBossLine();
        SoundManager.BgmDuck("boss", false);   // v9.16
        isGroggyPhase = false;
        barHeld = false;
        UISkin.NoticeShiftY = 0f;
        if (bossRoot != null) bossRoot.gameObject.SetActive(false);
        if (groggyRoot != null) groggyRoot.gameObject.SetActive(false);
    }

    /// <summary>v9.19: 알림 한 줄과 띠 강조를 지운다 (보스가 사라질 때)</summary>
    private void ResetBossLine()
    {
        lineLeft = 0f; lineShowing = false; bannerPunchT = -1f;
        SetBannerLineMode(false);
        if (groggyRoot != null) groggyRoot.localScale = Vector3.one;
    }

    public void OnBossDefeated()
    {
        currentBoss = null;
        ResetBossLine();
        SoundManager.BgmDuck("boss", false);   // v9.16
        isGroggyPhase = false;
        barHeld = false;
        UISkin.NoticeShiftY = 0f;
        bossRoot.gameObject.SetActive(false);
        groggyRoot.gameObject.SetActive(false);

        Debug.Log("[BossGimmickSystem] 보스 처치!");
        UIManager.Instance?.ShowStatChange("보스 처치! 승리!");

        // Phase 2-1: 스피노 베팅 정산 (격파 보너스 몰수 판정은 GameManager 지급부에서)
        SpinoBet.Resolve();
    }

    // ─────────────────────────────────────────────
    // UI 생성 (코드 생성 - 씬 세팅 불필요)
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("BossUICanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 480;   // 주방 이벤트(500)/정비소(550)/증강(600)보다 아래
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIFactory.RefResolution;   // v9.14: UI 전체 배율 (GameBalance.UIScale)
        scaler.matchWidthOrHeight = GameBalance.BossBarBig ? 0f : 0.5f;   // v9.18: 큰 바는 폭 기준 (기차 상황판·씬 HUD 와 같은 기준이라야 자리가 맞는다)
        canvasGo.AddComponent<GraphicRaycaster>();

        // ---------- 보스 HP 바 ----------
        if (GameBalance.BossBarBig) BuildBigBar(canvasGo.transform);
        else BuildOldBar(canvasGo.transform);

        // ---------- 그로기 배너 (보스 바 아래) ----------
        groggyRoot = KitchenEventManager.MakeBox(canvasGo.transform, "GroggyBanner", new Color(0.25f, 0.08f, 0.05f, 0.92f));
        groggyRoot.anchorMin = new Vector2(0.5f, 1f);
        groggyRoot.anchorMax = new Vector2(0.5f, 1f);
        groggyRoot.pivot = new Vector2(0.5f, 1f);
        // v9.18: 큰 HP 바를 쓰면 바 바로 밑에 붙이고 폭도 바와 같게 (보스 묶음 한 덩어리). 구 자리(-222)는 이제 보스가 서는 높이라 몸을 가렸다
        groggyRoot.anchoredPosition = GameBalance.BossBarBig ? new Vector2(0f, BIG_BAR_Y - BIG_BAR_H - GROGGY_GAP) : new Vector2(0f, -222f);
        groggyRoot.sizeDelta = new Vector2(GameBalance.BossBarBig ? bossRoot.sizeDelta.x : 700f, GROGGY_H);
        groggyRoot.GetComponent<Image>().raycastTarget = false;

        // 안내 문구 (한 줄, 겹침 없음)
        groggyGuideText = KitchenEventManager.MakeText(groggyRoot, "Guide", "", 22, new Color(1f, 0.85f, 0.4f));
        if (GameBalance.BossBarBig)
        {
            // 띠가 700 -> HP 바 폭으로 좁아졌다 - 긴 안내는 띠 밖으로 삐져나가지 않게 글자를 줄여 한 줄에 맞춘다 (22 -> 최소 15)
            groggyGuideText.horizontalOverflow = HorizontalWrapMode.Wrap;
            groggyGuideText.verticalOverflow = VerticalWrapMode.Truncate;
            groggyGuideText.resizeTextForBestFit = true;
            groggyGuideText.resizeTextMinSize = 15;
            groggyGuideText.resizeTextMaxSize = 22;
        }
        RectTransform gRt = groggyGuideText.rectTransform;
        gRt.anchorMin = new Vector2(0f, 1f);
        gRt.anchorMax = new Vector2(1f, 1f);
        gRt.pivot = new Vector2(0.5f, 1f);
        gRt.anchoredPosition = new Vector2(0f, -6f);
        gRt.sizeDelta = new Vector2(-20f, 32f);

        // 남은 시간 게이지 (하단)
        RectTransform tBg = KitchenEventManager.MakeBox(groggyRoot, "TimeBG", new Color(0f, 0f, 0f, 0.55f));
        tBg.anchorMin = new Vector2(0f, 0f);
        tBg.anchorMax = new Vector2(1f, 0f);
        tBg.pivot = new Vector2(0.5f, 0f);
        tBg.offsetMin = new Vector2(14f, 8f);
        tBg.offsetMax = new Vector2(-14f, 8f);
        tBg.sizeDelta = new Vector2(tBg.sizeDelta.x, 12f);
        tBg.GetComponent<Image>().raycastTarget = false;
        timeBarGo = tBg.gameObject;

        // v9.19 (A4): 알림 한 줄의 본문 - 게이지 자리에 (알림일 때만 보인다). 길면 두 줄까지, 글자를 13 까지 줄인다
        lineBodyText = KitchenEventManager.MakeText(groggyRoot, "LineBody", "", 16, new Color(0.95f, 0.9f, 0.82f));
        lineBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        lineBodyText.verticalOverflow = VerticalWrapMode.Truncate;
        lineBodyText.resizeTextForBestFit = true;
        lineBodyText.resizeTextMinSize = 13;
        lineBodyText.resizeTextMaxSize = 16;
        lineBodyText.alignment = TextAnchor.MiddleCenter;
        RectTransform lbRt = lineBodyText.rectTransform;
        lbRt.anchorMin = new Vector2(0f, 0f);
        lbRt.anchorMax = new Vector2(1f, 0f);
        lbRt.pivot = new Vector2(0.5f, 0f);
        lbRt.anchoredPosition = new Vector2(0f, 3f);
        lbRt.sizeDelta = new Vector2(-20f, 30f);
        lineBodyText.gameObject.SetActive(false);

        groggyTimeFill = KitchenEventManager.MakeBox(tBg, "TimeFill", new Color(1f, 0.55f, 0.15f));
        groggyTimeFill.anchorMin = new Vector2(0f, 0f);
        groggyTimeFill.anchorMax = new Vector2(1f, 1f);
        groggyTimeFill.offsetMin = Vector2.zero;
        groggyTimeFill.offsetMax = Vector2.zero;
        groggyTimeFillImg = groggyTimeFill.GetComponent<Image>();
        groggyTimeFillImg.raycastTarget = false;
    }

    /// <summary>v9.17 까지의 배치 (위에서 150 아래, 700x64). GameBalance.BossBarBig = false 일 때</summary>
    private void BuildOldBar(Transform parent)
    {
        bossRoot = KitchenEventManager.MakeBox(parent, "BossBar", new Color(0.08f, 0.06f, 0.05f, 0.88f));
        bossRoot.anchorMin = new Vector2(0.5f, 1f);
        bossRoot.anchorMax = new Vector2(0.5f, 1f);
        bossRoot.pivot = new Vector2(0.5f, 1f);
        bossRoot.anchoredPosition = new Vector2(0f, -150f);
        bossRoot.sizeDelta = new Vector2(700f, 64f);
        bossRoot.GetComponent<Image>().raycastTarget = false;

        // 보스 이름 (좌측)
        bossNameText = KitchenEventManager.MakeText(bossRoot, "Name", "메카 티렉스 보스", 20, new Color(1f, 0.45f, 0.35f));
        RectTransform nRt = bossNameText.rectTransform;
        nRt.anchorMin = new Vector2(0f, 1f);
        nRt.anchorMax = new Vector2(0.5f, 1f);
        nRt.pivot = new Vector2(0f, 1f);
        nRt.anchoredPosition = new Vector2(14f, -4f);
        nRt.sizeDelta = new Vector2(0f, 24f);
        bossNameText.alignment = TextAnchor.MiddleLeft;

        // HP 바 배경
        RectTransform hpBg = KitchenEventManager.MakeBox(bossRoot, "HPBG", new Color(0f, 0f, 0f, 0.6f));
        hpBg.anchorMin = new Vector2(0f, 0f);
        hpBg.anchorMax = new Vector2(1f, 0f);
        hpBg.pivot = new Vector2(0.5f, 0f);
        hpBg.offsetMin = new Vector2(14f, 8f);
        hpBg.offsetMax = new Vector2(-14f, 8f);
        hpBg.sizeDelta = new Vector2(hpBg.sizeDelta.x, 26f);
        hpBg.GetComponent<Image>().raycastTarget = false;

        // HP 채움 (빨강)
        hpFill = KitchenEventManager.MakeBox(hpBg, "HPFill", new Color(0.85f, 0.2f, 0.15f));
        hpFill.anchorMin = new Vector2(0f, 0f);
        hpFill.anchorMax = new Vector2(1f, 1f);
        hpFill.offsetMin = Vector2.zero;
        hpFill.offsetMax = Vector2.zero;
        hpFill.GetComponent<Image>().raycastTarget = false;

        // HP 수치 (바 위 중앙)
        bossHPText = KitchenEventManager.MakeText(hpBg, "HPText", "", 17, Color.white);
        RectTransform hRt = bossHPText.rectTransform;
        hRt.anchorMin = Vector2.zero;
        hRt.anchorMax = Vector2.one;
        hRt.offsetMin = Vector2.zero;
        hRt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// v9.18 큰 바: 화면 위 가운데, 기차 상황판 바로 아래 (위에서 46 ~ 128). 폭 = GameBalance.BossBarWidth (좌상단·우상단 판 사이에 들어가는 620).
    /// 윗줄 = 이름(왼쪽, 28) + 상태 딱지(오른쪽, 20) / 아랫줄 = HP 바 30 (잔상 띠 -> 채움 -> 무방비 눈금 -> 수치 20)
    /// </summary>
    private void BuildBigBar(Transform parent)
    {
        float width = Mathf.Max(360f, Mathf.Min(GameBalance.BossBarWidth, UISkin.TopBandWidth));   // 좌우 판 사이 띠를 넘지 않게
        bossRoot = KitchenEventManager.MakeBox(parent, "BossBar", new Color(0.07f, 0.05f, 0.05f, 0.93f));
        bossRoot.anchorMin = new Vector2(0.5f, 1f);
        bossRoot.anchorMax = new Vector2(0.5f, 1f);
        bossRoot.pivot = new Vector2(0.5f, 1f);
        bossRoot.anchoredPosition = new Vector2(0f, BIG_BAR_Y);
        bossRoot.sizeDelta = new Vector2(width, BIG_BAR_H);
        bossRoot.GetComponent<Image>().raycastTarget = false;
        Outline frame = bossRoot.gameObject.AddComponent<Outline>();   // 황동 테
        frame.effectColor = new Color(0.72f, 0.44f, 0.2f, 0.95f);
        frame.effectDistance = new Vector2(2f, -2f);

        // 이름 (왼쪽 위)
        bossNameText = KitchenEventManager.MakeText(bossRoot, "Name", "", 28, new Color(1f, 0.5f, 0.38f));
        bossNameText.fontStyle = FontStyle.Bold;
        bossNameText.alignment = TextAnchor.MiddleLeft;
        RectTransform nRt = bossNameText.rectTransform;
        nRt.anchorMin = new Vector2(0f, 1f);
        nRt.anchorMax = new Vector2(0.6f, 1f);
        nRt.pivot = new Vector2(0f, 1f);
        nRt.anchoredPosition = new Vector2(16f, -5f);
        nRt.sizeDelta = new Vector2(0f, 34f);
        Outline nameEdge = bossNameText.gameObject.AddComponent<Outline>();
        nameEdge.effectColor = new Color(0f, 0f, 0f, 0.8f);
        nameEdge.effectDistance = new Vector2(1.5f, -1.5f);

        // 상태 딱지 (오른쪽 위)
        bossTagText = KitchenEventManager.MakeText(bossRoot, "Tag", "", 20, new Color(1f, 0.82f, 0.4f));
        bossTagText.fontStyle = FontStyle.Bold;
        bossTagText.alignment = TextAnchor.MiddleRight;
        RectTransform tRt = bossTagText.rectTransform;
        tRt.anchorMin = new Vector2(0.4f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(1f, 1f);
        tRt.anchoredPosition = new Vector2(-16f, -7f);
        tRt.sizeDelta = new Vector2(0f, 30f);

        // HP 바 영역 (아래)
        hpBarArea = KitchenEventManager.MakeBox(bossRoot, "HPBG", new Color(0f, 0f, 0f, 0.7f));
        hpBarArea.anchorMin = new Vector2(0f, 0f);
        hpBarArea.anchorMax = new Vector2(1f, 0f);
        hpBarArea.pivot = new Vector2(0.5f, 0f);
        hpBarArea.offsetMin = new Vector2(14f, 10f);
        hpBarArea.offsetMax = new Vector2(-14f, 10f);
        hpBarArea.sizeDelta = new Vector2(hpBarArea.sizeDelta.x, 30f);
        hpBarArea.GetComponent<Image>().raycastTarget = false;

        // 잔상 띠 (채움 뒤)
        hpTrail = KitchenEventManager.MakeBox(hpBarArea, "HPTrailFill", new Color(1f, 0.93f, 0.72f, 0.92f));   // 이름에 Fill·Line·Bar 가 들어가면 스킨 스캐너가 건드리지 않는다
        hpTrail.anchorMin = new Vector2(0f, 0f);
        hpTrail.anchorMax = new Vector2(1f, 1f);
        hpTrail.offsetMin = Vector2.zero;
        hpTrail.offsetMax = Vector2.zero;
        hpTrail.GetComponent<Image>().raycastTarget = false;

        // 채움
        hpFill = KitchenEventManager.MakeBox(hpBarArea, "HPFill", HP_RED);
        hpFill.anchorMin = new Vector2(0f, 0f);
        hpFill.anchorMax = new Vector2(1f, 1f);
        hpFill.offsetMin = Vector2.zero;
        hpFill.offsetMax = Vector2.zero;
        hpFillImg = hpFill.GetComponent<Image>();
        hpFillImg.raycastTarget = false;

        // 수치 (바 가운데)
        bossHPText = KitchenEventManager.MakeText(hpBarArea, "HPText", "", 20, Color.white);
        bossHPText.fontStyle = FontStyle.Bold;
        RectTransform hRt = bossHPText.rectTransform;
        hRt.anchorMin = Vector2.zero;
        hRt.anchorMax = Vector2.one;
        hRt.offsetMin = Vector2.zero;
        hRt.offsetMax = Vector2.zero;
        Outline hpEdge = bossHPText.gameObject.AddComponent<Outline>();
        hpEdge.effectColor = new Color(0f, 0f, 0f, 0.85f);
        hpEdge.effectDistance = new Vector2(1.5f, -1.5f);
    }

    /// <summary>게이지 채움 비율 (0~1)</summary>
    private void SetFill(RectTransform fill, float ratio)
    {
        if (fill == null) return;
        Vector2 max = fill.anchorMax;
        max.x = Mathf.Clamp01(ratio);
        fill.anchorMax = max;
        fill.offsetMax = new Vector2(0f, fill.offsetMax.y);
    }
}
