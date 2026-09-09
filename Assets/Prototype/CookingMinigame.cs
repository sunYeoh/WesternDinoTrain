using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [CookingMinigame.cs] v2.6
/// - v2.6 변경점 (v9.5 조리 미니게임 픽셀 스킨 - 조리 목업 v1 컨펌본):
///   UISkin + ui_mg_*.png 가 있을 때만 그림을 바꾼다. 판정/타이머/게이지 로직과 좌표 상수는 v2.5 그대로.
///   굽기  = 석쇠 트랙(ui_mg_grate) + 판정 구간 틴트 조각(ui_mg_zone: Good 초록 / Perfect 황금) + 꼬치 커서 + 트랙 아래 불꽃 띠 + 우상단 라운드 고기 칩 3개
///   볶기  = 화살표 칩 6개 (무쇠 평판 + 색 테 + 화살표 글리프 ui_mg_arrow_*: 완료 초록 / 현재 황금 / 대기 흐림) + 불꽃 띠 + 팬 엠블럼
///   끓이기 = 유리관 압력 게이지(ui_gauge_bg 안에 채움 ui_gauge_fill + 안정존 ui_mg_zone) + "압력" 라벨 + 솥 엠블럼. 투입 버튼은 UIFactory 가 이미 무쇠 버튼(빨강)으로 입힌다
///   제목  = 패널 위 테에 걸린 황동 명판 (요리 이름이 바뀌면 UISkin.Relabel 로 폭도 다시 잡는다)
///   불꽃 3프레임은 Update 에서 0.11초마다 돌린다. 스킨이 없으면(PNG 미복사 / UISkin.ENABLED=false) v2.5 단색 박스 그대로
/// - v2.5 변경점 (P1 감사 1-A/2-C):
///   1) 지역 기반 난이도: 지역 2부터 커서/시간 압박 +12%, 지역 3부터 +25% + 판정 존 -10%
///      (수치는 GameBalance.CookRegionSpeedUp / CookRegionJudgeShrink)
///   2) 오일 캑터스 '기름 튐': 명중 시 잠시 굽기 커서 요동 + 끓이기 게이지 하강 가속
///      (Enemy.AttackTrain -> ApplyOilSlip. 볶기는 의도적으로 무영향)
/// 새 조리 미니게임 3종 (프로토타입 v3 이식판, uGUI 코드 생성)
/// - 굽기: 3연속 타이밍 바 (라운드마다 빨라짐)
/// - 볶기: 방향키 커맨드 6개 순서 입력
/// - 끓이기: 움직이는 안정존 추적 + 재료 투입 프롬프트 2회
/// 결과는 CookingBridge.FinishCook로 직접 전달
/// GameSystems 오브젝트에 부착
/// - v2 변경점: 증강 연동
///   CookSpeedMul = 제한 시간 증가 / 커서 감속, CookJudgeMul = 판정 존 확대
///   (Phase 2-3: 식칼/황금 조리 기구 등 조리 유틸은 아이템(ItemManager)으로 이관됨)
/// - v2.1 변경점 (기획 복원): 기차 피격 시 조리 방해
///   굽기 = 커서 점프 / 볶기 = 다음 화살표 교체 / 끓이기 = 게이지 출렁
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class CookingMinigame : MonoBehaviour
{
    public static CookingMinigame Instance { get; private set; }

    /// <summary>미니게임 진행 중 여부 (다른 시스템이 입력 충돌 방지용으로 참조)</summary>
    public static bool IsActive { get; private set; }

    // ── 공통 상태 ──
    private int method = 0;           // 0=굽기 1=볶기 2=끓이기
    private bool running = false;
    private bool finished = false;
    private float finishTimer = 0f;

    // ── 증강 배율 캐시 (StartGame 시점에 고정) ──
    private float speedMul = 1f;      // 제한 시간 배율 (클수록 여유)
    private float judgeMul = 1f;      // 판정 존 배율 (클수록 관대)

    // ─── P1: 지역 난이도 안내 (지역당 1회) ───
    private static int regionNoticeShown = 1;

    // ─── P1: 오일 캑터스 '기름 튐' (조리대 미끄러짐) ───
    // Enemy.AttackTrain에서 캑터스 명중 시 ApplyOilSlip 호출.
    // 효과: 굽기 커서가 요동치고(빨라졌다 느려졌다), 끓이기 게이지가 더 잘 미끄러져 내려간다.
    // 볶기(커맨드 입력)는 손 위치 문제라 기름과 무관 - 의도적으로 영향 없음.
    private static float oilSlipUntil = 0f;

    /// <summary>기름 튐(미끄러짐) 상태인가?</summary>
    public static bool OilSlipActive { get { return Time.time < oilSlipUntil; } }

    /// <summary>오일 캑터스 명중 시 호출 - 잠시 조리대가 미끄러워진다 (중첩 시 연장)</summary>
    public static void ApplyOilSlip(float duration)
    {
        // Phase 2-2 증강 '미끄럼 방지 매트': 기름 튐 무효
        if (ItemManager.OilImmune) return;   // Phase 2-3: 미끄럼 방지 매트는 아이템으로 이관됨

        bool fresh = !OilSlipActive;
        oilSlipUntil = Mathf.Max(oilSlipUntil, Time.time + duration);
        if (fresh)
            UIManager.Instance?.ShowDanger("[오일 캑터스] 기름이 튀었다! 조리대가 미끄럽다 ("
                + (int)duration + "초)");
    }

    // v2.2: 외부 홀드 종료 시각 (보스 낙뢰 패링이 Space를 잠시 빌려갈 때)
    private float externalHoldUntil = 0f;

    /// <summary>미니게임을 잠시 멈춘다 (타이머/입력 동결). 보스 패링 창 동안 호출</summary>
    public void HoldFor(float seconds)
    {
        externalHoldUntil = Mathf.Max(externalHoldUntil, Time.time + seconds);
    }

    // ── 굽기 ──
    private int grillRound;
    private float grillBar;           // 0~100
    private float grillDir;
    private float grillSpeed;
    private int grillScore;

    // ── 볶기 ──
    private int[] sauteSeq = new int[6];  // 0=좌 1=우 2=상 3=하
    private int sauteIdx;
    private int sauteMiss;
    private float sauteTimer;

    // ── 끓이기 ──
    private float boilGauge;          // 0~100
    private bool boilHold;
    private float boilTimer;
    private float boilInZone;
    private float boilTotal;
    private float boilZonePhase;
    private float boilZoneCenter;
    private float boilZoneHalf;       // 안정존 반폭 (판정 증강 반영)
    private float boilPromptTimer;    // 남은 프롬프트 활성 시간
    private int boilPromptOk;
    private int boilPromptsLeft;
    private float boilNextPromptAt;

    // ── UI 요소 ──
    private Canvas canvas;
    private RectTransform panel;
    private Text titleText;
    private Text infoText;
    private Text judgeText;           // PERFECT!/Good/Miss 팝업
    private float judgeTimer;

    // 조리법별 묶음 (표시 전환용 투명 컨테이너 - 좌표계는 패널과 동일)
    private RectTransform grillRoot;
    private RectTransform sauteRoot;
    private RectTransform boilRoot;

    // 굽기 UI
    private RectTransform grillTrack;
    private RectTransform grillGoodZone;
    private RectTransform grillPerfectZone;
    private RectTransform grillCursor;

    // 볶기 UI
    private Text[] arrowTexts = new Text[6];
    private static readonly string[] ARROW_STR = { "←", "→", "↑", "↓" };

    // 끓이기 UI
    private RectTransform boilTrack;
    private RectTransform boilInner;      // Fill/Zone 의 부모 (스킨 = 유리관 안쪽 52x150, 스킨 없음 = boilTrack 자신)
    private RectTransform boilZoneRect;
    private RectTransform boilFillRect;
    private Button promptButton;
    private float boilBarW = 56f;         // 채움/안정존 폭 (v2.5 값. 스킨이면 유리관 안폭 52)

    // v2.3: UI 개선 - 조리 중에도 전장이 보이도록 패널 축소 (사용자 피드백)
    private const float TRACK_W = 380f;
    private const float BOIL_H = 150f;

    // ── v2.6 픽셀 스킨 (UISkin + ui_mg_*.png 가 전부 있을 때만) ──
    private bool skin;
    private static readonly string[] ARROW_PNG = { "l", "r", "u", "d" };          // sauteSeq 값 순서 = ui_mg_arrow_{l,r,u,d}
    private static readonly string[] SKIN_PNGS =                                   // 스킨을 켜는 데 필요한 그림 전부
    {
        "ui_mg_grate", "ui_mg_zone", "ui_mg_cursor", "ui_mg_arrow_l", "ui_mg_arrow_r", "ui_mg_arrow_u", "ui_mg_arrow_d",
        "ui_mg_flame_0", "ui_mg_flame_1", "ui_mg_flame_2", "ui_mg_pot", "ui_mg_pan", "ui_mg_meat", "ui_gauge_bg", "ui_gauge_fill",
    };
    private static readonly Color ZONE_GOOD = new Color(0.43f, 0.78f, 0.43f, 0.67f);   // 판정 구간 초록 (목업 (110,200,110) a170)
    private static readonly Color FILL_CYAN = new Color(0.43f, 0.78f, 0.79f, 1f);      // 압력 게이지 채움
    private const float FLAME_FPS = 9f;                                            // 불꽃 프레임 전환 속도
    private const int FLAME_COUNT = 10;                                            // 띠 하나의 불꽃 수 (32px, 34px 간격)
    private RectTransform titlePlate;                                              // 황동 명판 (titleText 대신)
    private Sprite[] flameFrames = new Sprite[3];
    private Image[] grillFlames;                                                   // 트랙 아래 불꽃 띠
    private Image[] sauteFlames;                                                   // 화살표 칩 아래 불꽃 띠
    private Image[] grillChips = new Image[3];                                     // 라운드 고기 칩 (완료 황금 / 진행 크림 / 대기 흐림)
    private Image[] arrowImgs = new Image[6];                                      // 화살표 글리프 (arrowTexts 대신 보이는 것)
    private Image[] arrowRings = new Image[6];                                     // 칩 테 (색 = 화살표 상태색)
    private Sprite[] arrowSprites = new Sprite[4];
    private float flameTimer;
    private int flameFrame;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        BuildUI();
        panel.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────
    // 시작 (KitchenPanel에서 호출)
    // ─────────────────────────────────────────
    public void StartGame(int cookMethod)
    {
        // P1: 인퓨징 진행 중에는 조리 시작 불가 (Space 입력이 겹치는 사고 방지)
        if (InfusingMinigame.IsActive)
        {
            UIManager.Instance?.ShowStatChange("인퓨징 중에는 조리를 시작할 수 없다!");
            return;
        }

        method = cookMethod;
        running = true;
        finished = false;
        IsActive = true;
        judgeTimer = 0f;

        // 증강 배율 캐시 (과하게 커지지 않게 상한)
        // Phase 2-3부터 조리 유틸은 아이템이 담당 - 증강 값은 호환용으로만 곱한다 (항상 1)
        speedMul = Mathf.Min(AugmentManager.CookSpeedMul, 2f);
        judgeMul = Mathf.Min(AugmentManager.CookJudgeMul, 1.9f);

        // Phase 2-3 아이템(유물): 식칼/황금 조리 기구 + 조리법별 전문 도구
        speedMul *= ItemManager.CookTimeMul;
        judgeMul *= ItemManager.CookJudgeMul;
        if (method == 0) judgeMul *= ItemManager.GrillJudgeMul;        // 구리 온도계 (굽기)
        else if (method == 1) speedMul *= ItemManager.StirTimeMul;     // 균형 잡힌 뒤집개 (볶기)
        else if (method == 2) judgeMul *= ItemManager.BoilJudgeMul;    // 압력 조절 밸브 (끓이기)

        // 명성 상점 '셰프의 감각': 판정 존 영구 확대 (레벨당 +4%, 전체 상한 2.0)
        judgeMul = Mathf.Min(judgeMul * (1f + MetaProgress.CookJudgeBonus), 2f);

        // 도구 내구도 + 조리 디버프 반영 (기획: 무뎌진 칼 = 판정 축소 / 눌어붙은 팬 = 시간 부족)
        ChefController chefRef = FindFirstObjectByType<ChefController>();
        if (chefRef != null)
        {
            judgeMul *= Mathf.Lerp(0.65f, 1f, chefRef.knifeSharpness / 100f);   // 칼 상태 -> 판정 존
            speedMul *= Mathf.Lerp(0.7f, 1f, chefRef.panCondition / 100f);      // 팬 상태 -> 제한 시간
            speedMul *= chefRef.cookingSpeedMultiplier;                          // 독침 프테라 디버프
        }

        // P1 (감사 1-A): 지역 기반 조리 난이도 - "협곡에서는 손도 떨린다"
        // 지역 2: 커서/시간 압박 +12% / 지역 3+: +25% + 판정 존 -10% (수치는 GameBalance)
        int region = Mathf.Clamp(GameBalance.RegionOf(
            GameManager.Instance != null ? GameManager.Instance.currentWave : 1), 1, 4);
        speedMul /= (1f + GameBalance.CookRegionSpeedUp[region - 1]);
        judgeMul *= (1f - GameBalance.CookRegionJudgeShrink[region - 1]);

        // P1+: 요리 숙련 - 많이 구운 레시피는 손에 익어 판정이 후해진다 (영구, 10회부터)
        float masteryJudge = MetaProgress.GetMasteryJudge(CookingBridge.pendingRecipeId);
        if (masteryJudge > 0f)
            judgeMul = Mathf.Min(judgeMul * (1f + masteryJudge), 2.2f);

        // B-3 기관차 레버 '전속 주행': 흔들리는 기차 위의 도마 - 판정 존 축소
        if (EngineCab.FullSteam)
            judgeMul *= 1f - GameBalance.LeverJudgePenalty;

        // Phase 2-3: 아이템까지 전부 겹쳤을 때의 안전 상한 (미니게임이 무의미해지는 것 방지)
        speedMul = Mathf.Min(speedMul, 2.6f);
        judgeMul = Mathf.Min(judgeMul, 2.6f);

        // 지역이 바뀐 뒤 첫 조리에서 1회만 안내 (스토리 감싸기)
        if (region < regionNoticeShown) regionNoticeShown = region;   // 새 런 시작 - 안내 재무장
        if (region >= 2 && region != regionNoticeShown)
        {
            regionNoticeShown = region;
            UIManager.Instance?.ShowStatChange(region == 2
                ? "[주방] 선로가 험해졌다... 손이 떨린다 (조리 난이도 상승)"
                : "[주방] 한기가 뼈에 스민다... 손끝이 무뎌진다 (조리 난이도 상승)");
        }

        // EventSystem 포커스 해제 (Space가 버튼에 먹히는 문제 방지)
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        // 결과 요리 이름 (미발견이면 ???)
        string foodName = "???";
        RecipeData r = RecipeDatabase.Get(CookingBridge.pendingRecipeId);
        if (r != null && FoodStock.Instance.IsDiscovered(r.recipeId))
            foodName = r.displayName;

        if (method == 0)
        {
            SetTitle("굽기  -  " + foodName);
            grillRound = 0; grillScore = 0;
            grillBar = 0f; grillDir = 1f;
            grillSpeed = 55f / speedMul;   // 식칼 증강: 커서 감속
        }
        else if (method == 1)
        {
            SetTitle("볶기  -  " + foodName);
            for (int i = 0; i < 6; i++) sauteSeq[i] = Random.Range(0, 4);
            sauteIdx = 0; sauteMiss = 0;
            sauteTimer = 6f * speedMul;    // 식칼 증강: 제한 시간 증가
        }
        else
        {
            SetTitle("끓이기  -  " + foodName);
            boilGauge = 50f; boilHold = false;
            boilTimer = 7f * speedMul;     // 식칼 증강: 제한 시간 증가
            boilInZone = 0f; boilTotal = 0f;
            boilZonePhase = Random.Range(0f, 6.28f);
            boilZoneHalf = Mathf.Min(13f * judgeMul, 26f);   // 조리기구 증강: 안정존 확대
            boilPromptTimer = 0f; boilPromptOk = 0; boilPromptsLeft = 2;
            boilNextPromptAt = boilTimer - (2.2f + Random.Range(0f, 1.2f));
        }

        panel.gameObject.SetActive(true);
        ShowMethodUI();
    }

    // ─────────────────────────────────────────
    // 기차 피격 시 조리 방해 (ChefController.OnTrainHit에서 호출)
    // 기획 원안: "기차 진동과 적의 포격은 게이지를 흔들어 멀티태스킹 난이도를 극대화"
    // ─────────────────────────────────────────
    public void OnTrainHit(float intensity)
    {
        if (!running || finished) return;

        float shake = Mathf.Clamp01(intensity);

        if (method == 0)
        {
            // 굽기: 커서가 순간 점프
            grillBar = Mathf.Clamp(grillBar + Random.Range(-1f, 1f) * 18f * shake, 0f, 100f);
        }
        else if (method == 1)
        {
            // 볶기: 아직 입력 안 한 다음 화살표가 다른 방향으로 바뀐다
            if (sauteIdx < 6 && shake > 0.3f)
            {
                sauteSeq[sauteIdx] = Random.Range(0, 4);
                SetArrowGlyph(sauteIdx, sauteSeq[sauteIdx]);
            }
        }
        else
        {
            // 끓이기: 압력 게이지가 출렁
            boilGauge = Mathf.Clamp(boilGauge + Random.Range(-1f, 1f) * 22f * shake, 0f, 100f);
        }

        if (shake > 0.2f)
            ShowJudge("흔들린다!", new Color(1f, 0.5f, 0.3f));
    }

    // ─────────────────────────────────────────
    void Update()
    {
        if (!running) return;

        // v2.6: 불꽃 띠 애니메이션 (순수 연출 - 홀드 중에도 계속 탄다)
        if (skin) AnimateFlames();

        // v2.2: 외부 홀드 (보스 낙뢰 패링 중) - 미니게임 진행/입력 일시 대기
        // 같은 키(Space)가 패링 판정에 쓰이도록 이 프레임의 조리 입력을 통째로 양보한다
        if (Time.time < externalHoldUntil)
            return;

        // 판정 팝업 페이드
        if (judgeTimer > 0f)
        {
            judgeTimer -= Time.deltaTime;
            if (judgeTimer <= 0f) judgeText.text = "";
        }

        // 종료 연출 대기
        if (finished)
        {
            finishTimer -= Time.deltaTime;
            if (finishTimer <= 0f) Close();
            return;
        }

        // B-1: 조리 자발 중단 - [ESC] 재료 환급 + 즉시 복귀
        // "접시를 마칠까, 포탑을 구할까"가 벌칙 없는 진짜 선택이 되게 한다
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            AbortCook();
            return;
        }

        if (method == 0) UpdateGrill();
        else if (method == 1) UpdateSaute();
        else UpdateBoil();
    }

    // ═════════════ 굽기 ═════════════
    private void UpdateGrill()
    {
        // P1: 기름 튐 - 커서가 미끄러지듯 요동친다 (빨라졌다 느려졌다, 느린 순간을 노리면 파훼)
        float slipMul = OilSlipActive
            ? 1f + GameBalance.OilSlipWobble * Mathf.Sin(Time.time * 7.3f)
            : 1f;

        grillBar += grillDir * grillSpeed * slipMul * Time.deltaTime;
        if (grillBar >= 100f) { grillBar = 100f; grillDir = -1f; }
        if (grillBar <= 0f) { grillBar = 0f; grillDir = 1f; }

        // 커서 이동
        grillCursor.anchoredPosition = new Vector2(-TRACK_W / 2f + TRACK_W * (grillBar / 100f), 0f);
        infoText.text = "라운드 " + (grillRound + 1) + "/3   점수 " + grillScore + "  (5+ PERFECT / 3+ Good)\n"
            + (OilSlipActive ? "[기름!] 커서가 미끄러진다  " : "") + "[Space] 또는 클릭!";

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            GrillHit();
    }

    private void GrillHit()
    {
        float d = Mathf.Abs(grillBar - 50f);
        // 판정 존: 증강(황금 조리 기구)으로 확대
        float perfectHalf = 6f * judgeMul;
        float goodHalf = 22f * judgeMul;

        int pts;
        if (d <= perfectHalf) { pts = 2; ShowJudge("PERFECT!", UIFactory.GOLD); }
        else if (d <= goodHalf) { pts = 1; ShowJudge("Good", new Color(0.6f, 0.85f, 0.54f)); }
        else { pts = 0; ShowJudge("Miss...", new Color(1f, 0.6f, 0.48f)); }

        grillScore += pts;
        grillRound++;
        RefreshGrillChips();

        if (grillRound >= 3)
        {
            Finish(grillScore >= 5 ? "perfect" : grillScore >= 3 ? "good" : "bad");
        }
        else
        {
            grillSpeed = (55f + grillRound * 32f) / speedMul; // 라운드마다 빨라짐 (식칼 증강으로 완화)
            grillBar = 0f; grillDir = 1f;
        }
    }

    // ═════════════ 볶기 ═════════════
    private void UpdateSaute()
    {
        sauteTimer -= Time.deltaTime;
        infoText.text = "화살표/WASD 순서대로!   남은 시간 " + Mathf.Max(0f, sauteTimer).ToString("F1") +
                        "s   오입력 " + sauteMiss + " (0=PERFECT)";

        if (sauteTimer <= 0f)
        {
            Finish("bad");
            return;
        }

        // v2.4 (기술감사): 볶기 커맨드 WASD 겸용 - 화살표로 손 옮길 필요 없음
        // (미니게임 중에는 셰프 이동이 정지되므로 WASD 충돌 없음)
        int input = -1;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) input = 0;
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) input = 1;
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) input = 2;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) input = 3;

        if (input < 0) return;

        if (input == sauteSeq[sauteIdx])
        {
            SetArrowColor(sauteIdx, new Color(0.6f, 0.85f, 0.54f)); // 완료 초록
            sauteIdx++;
            if (sauteIdx >= 6)
            {
                Finish(sauteMiss == 0 ? "perfect" : sauteMiss <= 2 ? "good" : "bad");
                return;
            }
            HighlightArrow();
        }
        else
        {
            sauteMiss++;
            ShowJudge("오입력!", new Color(1f, 0.6f, 0.48f));
        }
    }

    private void HighlightArrow()
    {
        for (int i = 0; i < 6; i++)
        {
            if (i < sauteIdx) continue; // 완료된 건 초록 유지
            SetArrowColor(i, (i == sauteIdx) ? UIFactory.GOLD : UIFactory.DIM);
        }
    }

    /// <summary>화살표 칸 i 에 방향 dir(0=좌 1=우 2=상 3=하) 표시 - 글자와 (스킨이면) 글리프 그림을 함께 바꾼다</summary>
    private void SetArrowGlyph(int i, int dir)
    {
        arrowTexts[i].text = ARROW_STR[dir];
        if (skin && arrowImgs[i] != null) arrowImgs[i].sprite = arrowSprites[dir];
    }

    /// <summary>화살표 칸 i 의 상태색 (완료 초록 / 현재 황금 / 대기 흐림) - 글자·글리프·칩 테에 같이 칠한다</summary>
    private void SetArrowColor(int i, Color c)
    {
        arrowTexts[i].color = c;
        if (!skin) return;
        if (arrowImgs[i] != null) arrowImgs[i].color = c;
        if (arrowRings[i] != null) arrowRings[i].color = c;
    }

    // ═════════════ 끓이기 ═════════════
    private void UpdateBoil()
    {
        // Space 홀드
        // P1: 기름 튐 - 손을 떼면 게이지가 더 잘 미끄러져 내려가고, 안정존도 더 빨리 흔들린다
        boilHold = Input.GetKey(KeyCode.Space);
        float fallSpeed = OilSlipActive ? -44f : -30f;
        boilGauge += (boilHold ? 52f : fallSpeed) * Time.deltaTime;
        boilGauge = Mathf.Clamp(boilGauge, 0f, 100f);

        // 안정존이 사인파로 이동
        boilZonePhase += Time.deltaTime * (OilSlipActive ? 1.35f : 0.9f);
        boilZoneCenter = 50f + Mathf.Sin(boilZonePhase) * 22f;

        boilTotal += Time.deltaTime;
        if (Mathf.Abs(boilGauge - boilZoneCenter) <= boilZoneHalf)
            boilInZone += Time.deltaTime;

        boilTimer -= Time.deltaTime;

        // 재료 투입 프롬프트 발동
        if (boilPromptsLeft > 0 && boilTimer <= boilNextPromptAt)
        {
            boilPromptsLeft--;
            boilPromptTimer = 1.2f;
            promptButton.gameObject.SetActive(true);
            boilNextPromptAt = boilTimer - (1.8f + Random.Range(0f, 1.0f));
        }

        // 프롬프트 시간 초과
        if (boilPromptTimer > 0f)
        {
            boilPromptTimer -= Time.deltaTime;
            if (boilPromptTimer <= 0f)
            {
                promptButton.gameObject.SetActive(false);
                ShowJudge("투입 놓침!", new Color(1f, 0.6f, 0.48f));
            }
        }

        // 게이지/존 렌더
        float fillH = BOIL_H * (boilGauge / 100f);
        boilFillRect.sizeDelta = new Vector2(boilBarW, fillH);
        float zoneH = BOIL_H * (boilZoneHalf * 2f / 100f);
        float zoneY = BOIL_H * ((boilZoneCenter - boilZoneHalf) / 100f);
        boilZoneRect.sizeDelta = new Vector2(boilBarW, zoneH);
        boilZoneRect.anchoredPosition = new Vector2(0f, zoneY);

        float ratio = boilInZone / Mathf.Max(0.1f, boilTotal);
        infoText.text = "[Space] 홀드로 게이지를 초록 존에 유지!\n남은 " + Mathf.Max(0f, boilTimer).ToString("F1") +
                        "s   유지율 " + Mathf.RoundToInt(ratio * 100f) + "%   투입 " + boilPromptOk + "/2";

        if (boilTimer <= 0f)
        {
            if (ratio >= 0.72f && boilPromptOk >= 2) Finish("perfect");
            else if (ratio >= 0.45f && boilPromptOk >= 1) Finish("good");
            else Finish("bad");
        }
    }

    private void OnPromptClicked()
    {
        if (boilPromptTimer <= 0f) return;
        boilPromptTimer = 0f;
        boilPromptOk++;
        promptButton.gameObject.SetActive(false);
        ShowJudge("투입 성공!", UIFactory.GOLD);
    }

    // ─────────────────────────────────────────
    // 종료 처리
    // ─────────────────────────────────────────
    private void Finish(string quality)
    {
        finished = true;
        finishTimer = 0.8f;

        if (quality == "perfect") ShowJudge("PERFECT! 최고의 한 접시!", UIFactory.GOLD);
        else if (quality == "good") ShowJudge("Good! 완성", new Color(0.6f, 0.85f, 0.54f));
        else ShowJudge("실패...", new Color(1f, 0.6f, 0.48f));

        // 도구 마모 (굽기=칼, 볶기/끓이기=팬) - 정비소 수리의 의미가 생긴다
        ChefController chefRef = FindFirstObjectByType<ChefController>();
        if (chefRef != null) chefRef.WearToolsByMethod(method);

        CookingBridge.FinishCook(quality);
    }

    private void Close()
    {
        running = false;
        IsActive = false;
        panel.gameObject.SetActive(false);
    }

    /// <summary>
    /// B-1: 조리 자발 중단이 일어난 프레임 (PauseMenu가 같은 프레임 ESC로
    /// 일시정지를 여는 이중 소비를 막는 데 사용)
    /// </summary>
    public static int EscConsumedFrame = -1;

    /// <summary>B-1: [ESC] 조리 중단 - 재료는 돌려받고 진행만 잃는다 (도구 마모 없음)</summary>
    private void AbortCook()
    {
        EscConsumedFrame = Time.frameCount;
        CookingBridge.AbortCook();   // 재료 환급 + 대기 레시피 정리
        SoundManager.Play("sfx_ui_click");
        Close();
    }

    private void ShowJudge(string text, Color color)
    {
        judgeText.text = text;
        judgeText.color = color;
        judgeTimer = 0.8f;

        // v2.3: 판정음 (클립 없으면 SoundManager가 조용히 무시)
        if (text.StartsWith("PERFECT")) SoundManager.Play("sfx_judge_perfect");
        else if (text.StartsWith("Good")) SoundManager.Play("sfx_judge_good");
        else if (text.StartsWith("실패") || text.StartsWith("꽝")) SoundManager.Play("sfx_judge_bad");
    }

    /// <summary>제목 - 스킨이면 황동 명판 글자(폭 자동), 아니면 v2.5 제목 텍스트</summary>
    private void SetTitle(string title)
    {
        titleText.text = title;
        if (skin) UISkin.Relabel(titlePlate, title, 16);
    }

    // ─────────────────────────────────────────
    // UI 구성 (1회)
    // ─────────────────────────────────────────
    private void BuildUI()
    {
        canvas = UIFactory.CreateCanvas("Minigame_Canvas", 30); // 주방 패널보다 위

        // v2.6: 스킨 가용성 - UISkin 스프라이트 + 미니게임 조각(ui_mg_*) 13장 + 게이지 조각이 전부 있어야 한다. 하나라도 없으면 v2.5 박스
        //       (스프라이트가 null 인 Image 는 흰 사각형으로 그려지므로 일부만 있는 상태로 스킨을 켜지 않는다)
        skin = UISkin.Available;
        string missing = null;
        for (int i = 0; i < SKIN_PNGS.Length; i++)
            if (!SpriteBank.Has(SKIN_PNGS[i])) { missing = SKIN_PNGS[i]; skin = false; break; }
        if (UISkin.Available && !skin)
            Debug.LogWarning("[CookingMinigame] " + missing + ".png 가 빠져 조리 미니게임은 단색 박스로 표시 (Resources/Sprites/WDT 확인)");
        if (skin)
        {
            for (int i = 0; i < 3; i++) flameFrames[i] = SpriteBank.Get("ui_mg_flame_" + i);
            for (int i = 0; i < 4; i++) arrowSprites[i] = SpriteBank.Get("ui_mg_arrow_" + ARROW_PNG[i]);
        }

        // v2.3: 좌하단 컴팩트 패널 (HUD 바로 위)
        // 화면 중앙(기차/전장)을 가리지 않아 조리 중에도 바깥 상황이 보인다
        // (스킨이면 UIFactory 가 리벳 테 카드 + 무쇠 평판으로 만든다)
        panel = UIFactory.CreatePanel(canvas.transform, "MinigamePanel",
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(12f, 192f), new Vector2(452f, 470f),
            new Color(0.14f, 0.085f, 0.05f, 0.88f), UIFactory.COPPER, 4f);

        titleText = UIFactory.CreateText(panel, "Title", "", 24, UIFactory.GOLD, TextAnchor.UpperCenter);
        titleText.rectTransform.offsetMin = new Vector2(10f, -60f);
        titleText.rectTransform.offsetMax = new Vector2(-10f, -14f);
        titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        if (skin)
        {
            // 제목은 패널 위 테에 걸린 황동 명판 (웨이브 명판과 같은 문법). 원래 제목 글자는 끈다
            titleText.enabled = false;
            titlePlate = UISkin.Nameplate(panel, "MinigameTitle", "", 16, new Vector2(0f, 1f), new Vector2(16f, 4f), 200f);
        }

        infoText = UIFactory.CreateText(panel, "Info", "", 17, UIFactory.CREAM, TextAnchor.LowerCenter);
        infoText.rectTransform.anchorMin = new Vector2(0f, 0f);
        infoText.rectTransform.anchorMax = new Vector2(1f, 0f);
        infoText.rectTransform.offsetMin = new Vector2(10f, 12f);
        infoText.rectTransform.offsetMax = new Vector2(-10f, 66f);

        judgeText = UIFactory.CreateText(panel, "Judge", "", 26, UIFactory.GOLD, TextAnchor.UpperCenter);
        judgeText.rectTransform.anchorMin = new Vector2(0f, 1f);
        judgeText.rectTransform.anchorMax = new Vector2(1f, 1f);
        judgeText.rectTransform.offsetMin = new Vector2(10f, -100f);
        judgeText.rectTransform.offsetMax = new Vector2(-10f, -62f);

        // 조리법별 묶음 (투명 컨테이너 - 패널과 같은 좌표계라 v2.5 좌표 그대로)
        grillRoot = MakeStretch(panel, "GrillRoot");
        sauteRoot = MakeStretch(panel, "SauteRoot");
        boilRoot = MakeStretch(panel, "BoilRoot");

        BuildGrillUI();
        BuildSauteUI();
        BuildBoilUI();

        // 판정 팝업은 언제나 맨 위 (엠블럼/칩 위로)
        judgeText.transform.SetAsLastSibling();
    }

    private void BuildGrillUI()
    {
        // 트랙
        grillTrack = MakeRect(grillRoot, "GrillTrack", new Vector2(TRACK_W, 40f), Vector2.zero);
        Image trackImg = grillTrack.gameObject.AddComponent<Image>();
        trackImg.color = new Color(0f, 0f, 0f, 0.55f);
        if (skin) SetSprite(trackImg, SpriteBank.Get("ui_mg_grate"), Image.Type.Simple, Color.white);   // 무쇠 석쇠 + 숯불 (380x40 원본 크기)

        // Good 존 (중앙 44% 기준 - 판정 증강 시 확대)
        grillGoodZone = MakeRect(grillTrack, "GoodZone", new Vector2(TRACK_W * 0.44f, 40f), Vector2.zero);
        Image goodImg = grillGoodZone.gameObject.AddComponent<Image>();
        goodImg.color = new Color(0.6f, 0.85f, 0.54f, 0.45f);
        if (skin) SetSprite(goodImg, SpriteBank.Get("ui_mg_zone"), Image.Type.Sliced, ZONE_GOOD);

        // Perfect 존 (중앙 12% 기준 - 판정 증강 시 확대)
        grillPerfectZone = MakeRect(grillTrack, "PerfectZone", new Vector2(TRACK_W * 0.12f, 40f), Vector2.zero);
        Image perfImg = grillPerfectZone.gameObject.AddComponent<Image>();
        perfImg.color = new Color(0.894f, 0.663f, 0.216f, 0.85f);
        if (skin) SetSprite(perfImg, SpriteBank.Get("ui_mg_zone"), Image.Type.Sliced, new Color(UIFactory.GOLD.r, UIFactory.GOLD.g, UIFactory.GOLD.b, 0.92f));

        // 불꽃 띠 (트랙 아래, 커서보다 먼저 만들어 커서가 위에 그려진다)
        if (skin) grillFlames = MakeFlameRow(grillTrack, -34f);

        // 커서
        grillCursor = MakeRect(grillTrack, "Cursor", new Vector2(9f, 56f), Vector2.zero);
        Image cursorImg = grillCursor.gameObject.AddComponent<Image>();
        cursorImg.color = Color.white;
        if (skin)
        {
            // 황동 꼬치 + 고기 한 점 (12x56)
            SetSprite(cursorImg, SpriteBank.Get("ui_mg_cursor"), Image.Type.Simple, Color.white);
            grillCursor.sizeDelta = new Vector2(12f, 56f);
        }

        // 라운드 고기 칩 3개 + "라운드" 라벨 (패널 우상단)
        if (skin)
        {
            Sprite meat = SpriteBank.Get("ui_mg_meat");
            for (int i = 0; i < 3; i++)
                grillChips[i] = MakeImage(grillRoot, "RoundChip_" + i, meat, new Vector2(1f, 1f),
                    new Vector2(-76f + i * 22f, -32f), new Vector2(16f, 16f));
            Text lbl = UIFactory.CreateText(grillRoot, "RoundLabel", "라운드", 13, UIFactory.DIM, TextAnchor.MiddleRight);
            lbl.rectTransform.anchorMin = new Vector2(1f, 1f); lbl.rectTransform.anchorMax = new Vector2(1f, 1f);
            lbl.rectTransform.pivot = new Vector2(1f, 1f);
            lbl.rectTransform.anchoredPosition = new Vector2(-92f, -22f);
            lbl.rectTransform.sizeDelta = new Vector2(80f, 20f);
        }
    }

    private void BuildSauteUI()
    {
        for (int i = 0; i < 6; i++)
        {
            // v2.3: 축소 패널에 맞춰 화살표 슬롯 간격/크기 축소
            RectTransform slot = MakeRect(sauteRoot, "Arrow_" + i, new Vector2(54f, 54f),
                new Vector2(-152f + i * 61f, 0f));
            Image bg = slot.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);

            Text t = UIFactory.CreateText(slot, "T", "?", 34, UIFactory.DIM, TextAnchor.MiddleCenter);
            arrowTexts[i] = t;

            if (skin)
            {
                // 칩 = 무쇠 평판 + 상태색 테 + 화살표 글리프 (글자는 끈다)
                UISkin.Plate(bg, Color.white);
                arrowRings[i] = UISkin.AddRing(slot, UIFactory.DIM);
                arrowImgs[i] = MakeImage(slot, "Glyph", arrowSprites[0], new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32f, 32f));
                arrowImgs[i].color = UIFactory.DIM;
                t.enabled = false;
            }
        }

        if (skin)
        {
            sauteFlames = MakeFlameRow(sauteRoot, -52f);
            MakeImage(sauteRoot, "PanEmblem", SpriteBank.Get("ui_mg_pan"), new Vector2(1f, 1f), new Vector2(-60f, -46f), new Vector2(80f, 44f));
        }
    }

    private void BuildBoilUI()
    {
        // 세로 트랙 (왼쪽으로 치우침)
        boilTrack = MakeRect(boilRoot, "BoilTrack", new Vector2(52f, BOIL_H), new Vector2(-110f, 4f));
        Image trackImg = boilTrack.gameObject.AddComponent<Image>();
        trackImg.color = new Color(0f, 0f, 0f, 0.55f);
        boilInner = boilTrack;
        if (skin)
        {
            // 유리관(황동 캡) 안에 52x150 논리 트랙을 그대로 넣는다 - 테 6px 만큼 바깥이 커진다 (Fill/Zone 좌표 계산은 v2.5 그대로)
            SetSprite(trackImg, SpriteBank.Get("ui_gauge_bg"), Image.Type.Sliced, Color.white);
            boilTrack.sizeDelta = new Vector2(52f + 12f, BOIL_H + 12f);
            GameObject innerGo = new GameObject("Inner");
            boilInner = innerGo.AddComponent<RectTransform>();
            boilInner.SetParent(boilTrack, false);
            boilInner.anchorMin = new Vector2(0.5f, 0f);
            boilInner.anchorMax = new Vector2(0.5f, 0f);
            boilInner.pivot = new Vector2(0.5f, 0f);
            boilInner.anchoredPosition = new Vector2(0f, 6f);
            boilInner.sizeDelta = new Vector2(52f, BOIL_H);
            boilBarW = 52f;
        }

        // 안정존 (하단 기준 배치)
        GameObject zoneGo = new GameObject("Zone");
        boilZoneRect = zoneGo.AddComponent<RectTransform>();
        boilZoneRect.SetParent(boilInner, false);
        boilZoneRect.anchorMin = new Vector2(0.5f, 0f);
        boilZoneRect.anchorMax = new Vector2(0.5f, 0f);
        boilZoneRect.pivot = new Vector2(0.5f, 0f);
        Image zoneImg = zoneGo.AddComponent<Image>();
        zoneImg.color = new Color(0.6f, 0.85f, 0.54f, 0.5f);

        // 게이지 채움 (하단 기준)
        GameObject fillGo = new GameObject("Fill");
        boilFillRect = fillGo.AddComponent<RectTransform>();
        boilFillRect.SetParent(boilInner, false);
        boilFillRect.anchorMin = new Vector2(0.5f, 0f);
        boilFillRect.anchorMax = new Vector2(0.5f, 0f);
        boilFillRect.pivot = new Vector2(0.5f, 0f);
        boilFillRect.anchoredPosition = Vector2.zero;
        Image fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.43f, 0.78f, 0.79f, 0.8f);

        if (skin)
        {
            // 채움 = 청록 유리관 액체, 안정존 = 초록 판정 조각을 채움 위에 (채움이 가려도 존이 보이게)
            SetSprite(fillImg, SpriteBank.Get("ui_gauge_fill"), Image.Type.Sliced, FILL_CYAN);
            SetSprite(zoneImg, SpriteBank.Get("ui_mg_zone"), Image.Type.Sliced, new Color(ZONE_GOOD.r, ZONE_GOOD.g, ZONE_GOOD.b, 0.6f));
            boilZoneRect.SetAsLastSibling();

            Text lbl = UIFactory.CreateText(boilRoot, "PressureLabel", "압력", 13, UIFactory.DIM, TextAnchor.MiddleCenter);
            lbl.rectTransform.anchorMin = new Vector2(0.5f, 0.5f); lbl.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            lbl.rectTransform.anchoredPosition = new Vector2(-110f, 4f + (BOIL_H + 12f) * 0.5f + 12f);   // 유리관 바로 위
            lbl.rectTransform.sizeDelta = new Vector2(80f, 20f);
            MakeImage(boilRoot, "PotEmblem", SpriteBank.Get("ui_mg_pot"), new Vector2(1f, 1f), new Vector2(-60f, -50f), new Vector2(72f, 60f));
        }

        // 재료 투입 버튼 (오른쪽, 평소 숨김) - 스킨이면 UIFactory 가 무쇠 버튼(빨강 틴트)으로 만든다
        promptButton = UIFactory.CreateButton(boilRoot, "PromptBtn", "재료 투입!\n(클릭)",
            new Vector2(150f, 70f), new Color(0.8f, 0.28f, 0.18f), Color.white, 20);
        RectTransform prt = promptButton.GetComponent<RectTransform>();
        prt.anchoredPosition = new Vector2(100f, 4f);
        promptButton.onClick.AddListener(OnPromptClicked);
        promptButton.gameObject.SetActive(false);
    }

    private RectTransform MakeRect(Transform parent, string name, Vector2 size, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return rt;
    }

    /// <summary>부모를 꽉 채우는 투명 컨테이너 (그림 없음 - 표시 전환용)</summary>
    private RectTransform MakeStretch(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return rt;
    }

    /// <summary>스프라이트 이미지 하나 (앵커 기준 pos 에 중앙 피벗, 클릭 통과)</summary>
    private Image MakeImage(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        SetSprite(img, sprite, Image.Type.Simple, Color.white);
        return img;
    }

    private static void SetSprite(Image img, Sprite sprite, Image.Type type, Color color)
    {
        img.sprite = sprite; img.type = type; img.color = color; img.raycastTarget = false;
    }

    /// <summary>불꽃 띠: 32x24 불꽃 FLAME_COUNT 개를 34px 간격으로 가운데 정렬 (y = 부모 중심 기준)</summary>
    private Image[] MakeFlameRow(Transform parent, float y)
    {
        Image[] row = new Image[FLAME_COUNT];
        float x0 = -(FLAME_COUNT - 1) * 34f * 0.5f;
        for (int i = 0; i < FLAME_COUNT; i++)
            row[i] = MakeImage(parent, "Flame_" + i, flameFrames[i % 3], new Vector2(0.5f, 0.5f),
                new Vector2(x0 + i * 34f, y), new Vector2(32f, 24f));
        return row;
    }

    /// <summary>불꽃 3프레임 순환 - 지금 보이는 띠만 갱신 (이웃 불꽃은 프레임을 하나씩 어긋나게)</summary>
    private void AnimateFlames()
    {
        flameTimer += Time.deltaTime;
        if (flameTimer < 1f / FLAME_FPS) return;
        flameTimer = 0f;
        flameFrame = (flameFrame + 1) % 3;
        Image[] row = method == 0 ? grillFlames : method == 1 ? sauteFlames : null;
        if (row == null) return;
        for (int i = 0; i < row.Length; i++)
            if (row[i] != null) row[i].sprite = flameFrames[(flameFrame + i) % 3];
    }

    /// <summary>라운드 고기 칩: 끝난 라운드 황금 / 진행 중 크림 / 남은 라운드 흐림</summary>
    private void RefreshGrillChips()
    {
        if (!skin) return;
        for (int i = 0; i < 3; i++)
        {
            if (grillChips[i] == null) continue;
            grillChips[i].color = i < grillRound ? UIFactory.GOLD : i == grillRound ? UIFactory.CREAM : UISkin.BRASS_DIM;
        }
    }

    // 미니게임별 UI 표시 전환
    private void ShowMethodUI()
    {
        grillRoot.gameObject.SetActive(method == 0);
        sauteRoot.gameObject.SetActive(method == 1);
        boilRoot.gameObject.SetActive(method == 2);

        grillTrack.gameObject.SetActive(method == 0);
        if (method == 0)
        {
            // 판정 증강에 맞춰 존 폭을 시각적으로도 반영
            grillGoodZone.sizeDelta = new Vector2(Mathf.Min(TRACK_W, TRACK_W * 0.44f * judgeMul), 40f);
            grillPerfectZone.sizeDelta = new Vector2(Mathf.Min(TRACK_W, TRACK_W * 0.12f * judgeMul), 40f);
            RefreshGrillChips();
        }

        for (int i = 0; i < 6; i++)
        {
            arrowTexts[i].transform.parent.gameObject.SetActive(method == 1);
            if (method == 1)
            {
                SetArrowGlyph(i, sauteSeq[i]);
                SetArrowColor(i, UIFactory.DIM);
            }
        }
        if (method == 1) HighlightArrow();

        boilTrack.gameObject.SetActive(method == 2);
        promptButton.gameObject.SetActive(false);
        judgeText.text = "";
    }
}
