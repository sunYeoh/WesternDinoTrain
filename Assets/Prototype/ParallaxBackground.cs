using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [ParallaxBackground.cs] v4.2 (v9.13 2026-09-23: 선로 v2 갈림길 + 세계 밀림) / v4.1 - 완전 탑다운 지면 (2026-09-07) / v3 고퀄 PNG 지면 / v2 탑뷰 지면 스크롤
///
/// v4.2 (선로 v2 - 목업 v2 그대로. 도트 px/route.py v2.1: rails_fork / rails_fork_up / rails_fork_hi_up·down·straight):
///   - 정차(선로 선택)에 두상 앞에 갈림길(전철기 스프라이트)을 띠(rails_ae) 위에 놓는다 - PlaceFork(위 가지, 아래 가지)
///       * 스프라이트 피벗 = 위 가지 분기점(오른쪽 끝). 분기점 x 는 띠 침목 위상(20px)에 스냅 - 스프라이트가 본선 침목·레일을 다시 그려 덮으므로 위상이 어긋나면 이음매가 보인다
///       * 갈림길은 띠 매개변수(s)로 붙어 있어 띠와 같이 흐른다 (감속 중에 놓아도 멈출 자리를 미리 계산해 두상 앞 -8.4 근처에 선다)
///       * 고른 길 강조 = 같은 크기의 반투명 금색 띠 스프라이트 깜빡임 (SetHighlight)
///   - 출발(BeginLaneShift(sign)): 두상 앞(RouteHeadFrontX)이 분기점을 지난 거리 d 의 가지 오프셋 f(d) 만큼 세계가 반대로 밀린다 - 기차는 y 0 고정
///       * 밀리는 것: 모래 층(누적, 32u 로 감아 씀) / 옛 곧은 띠 + 갈림길 (railsY) / 손님·바위·재료 조각·미끼 (RouteFX.ShiftWorldObjects)
///       * 고른 가지의 이어지는 띠 = rails_ae 타일 8장을 갈림길 왼쪽 끝에 딱 붙여 왼쪽으로 (침목 간격 20px 가 그대로 이어진다 - route.py v2.1 꼬리 침목 격자)
///       * 원호를 도는 동안 RouteRollDeg (CameraZoom 이 화면을 기울인다) + 분기점 진입 때 기적·덜컹
///       * 갈림길이 화면 오른쪽 밖으로 나가면 옛 곧은 띠가 RouteOldRailFadeSec 동안 사라지고, 가지 띠가 본선(층 2)이 된다 (위상·높이 같아 눈에 안 띈다)
///   - 탑다운 모드의 지면 루트는 카메라 x 를 따라가지 않는다 (v4.1 까지는 따라갔다 - 정차 카메라가 앞으로 6u 가면 멈춘 기차 밑에서 침목이 미끄러졌다). 띠 160u 라 카메라 이동 한계 안에서 빈 데 없음
///
/// v4 (Apocalypse Express 문법, 지평선 없음):
///   - Resources/Sprites/WDT/ 에 ground_ae(모래 32x32유닛, 중앙 피벗) + rails_ae(선로 16x5유닛, 중앙 피벗)가 있으면 "탑다운 모드"
///       층 0: ground_ae 5x3장 (정렬 -30)  - 화면 전체를 덮는다
///       층 1: (없음) - 지평선 층 제거
///       층 2: rails_ae 10장 가로 순환, y=0 = 기차 중심 밑 (정렬 -10) - 칸 사이 틈과 기차 위아래로 침목 끝이 보인다
///     v4.1: 탑다운 모드는 **줌을 따라 스케일하지 않는다** (세계에 고정). 휠 줌 시 지면·선로도 기차와 같이 커지고 작아진다.
///           대신 최대 줌아웃(20 = 세로 40유닛, 울트라와이드 가로 93유닛)까지 덮도록 모래 5x3장(160x96유닛), 선로 10장(160유닛)을 깐다
///   - ground_ae 가 없으면 v3 동작 그대로 (ground_a/b + horizon + rails, 그것도 없으면 v2 코드 도트. 이쪽은 줌 스케일 유지)
///   - 먼지 연출(DustFX.cs)이 쓰는 현재 지면 속도: ParallaxBackground.CurrentSpeed (월드 유닛/초)
///
/// 방향: 기차는 두상 쪽(왼쪽)으로 달린다 -> 지면은 오른쪽으로 흐른다 (EngineCab의 바위와 동일 방향)
/// 동작 (v1 유지): 전투(Battle) 중에만 목표 속도 1.0, 그 외는 0 -> 스르륵 가감속 / 지역 색 곱 틴트 / 줌 스케일 / 스케일드 시간
///
/// 사용법: 없음! 파일만 넣으면 게임 시작 시 스스로 생성된다. (SpriteBank.cs, 폴백용 PixelPainter.cs 필요)
///  - 구 배경은 정리할 것: 씬의 Background_1/2/3 삭제 + BackgroundScroll.cs 삭제 (잊어도 자동 비활성)
///  - 속도 훅: ParallaxBackground.SetSpeedMultiplier(배율) - 레버 전속이 쓴다
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // 튜닝 상수
    // ─────────────────────────────────────────────
    private const float BASE_SPEED = 3.2f;      // 지면 스크롤 속도 (월드 단위/초)
    private const float ACCEL_RATE = 0.55f;     // 출발 가속 (1.0까지 약 1.8초)
    private const float DECEL_RATE = 0.45f;     // 정차 감속 (0까지 약 2.2초)
    private const float TINT_LERP = 1.2f;       // 지역 색 전환 속도
    private const float REGION_TINT = 0.5f;     // 지역 색이 지면에 배는 정도 (0=없음 1=배경색 그대로)
    private const float VIEW_HALF_H_LEGACY = 7f;   // v3 이하 기준 줌
    private const float PPU = 16f;              // 코드 도트 폴백의 지면 배율

    // v4 탑다운 모드: 층별 타일 폭(유닛)/장수/y/정렬
    private const float GROUND_TILE_W = 32f;    // ground_ae = 1024px / 32ppu (정사각)
    private const int GROUND_COLS = 5;          // 가로 160유닛 (최대 줌아웃 울트라와이드 93유닛 + 여유)
    private const int GROUND_ROWS = 3;          // 세로 96유닛 (최대 줌아웃 40유닛 + 카메라 y 이동 여유)
    private const float RAILS_TILE_W = 16f;     // rails_ae = 512px / 32ppu
    private const int RAILS_TILES = 10;         // 가로 160유닛
    private const int ORDER_GROUND = -30;
    private const int ORDER_RAILS = -10;

    // v4.2 갈림길 (선로 v2)
    private const int ORDER_FORK = -9;            // 띠(-10) 위, 길가 바위(-8) 아래
    private const int ORDER_FORK_HI = -8;
    private const float SPRITE_PPU = 32f;         // rails_ae / rails_fork 픽셀 밀도
    private const float SLEEPER_STEP_PX = 20f;    // 띠 침목 간격 (px)
    /// <summary>분기점 = 타일 왼쪽 끝 + (7 + 20k) px 일 때 갈림길의 긴 침목이 띠 침목과 같은 칸에 온다 (route.py 침목 배치에서 유도 - 도트를 안 바꾸면 손대지 말 것)</summary>
    private const float FORK_SLEEPER_PHASE_PX = 7f;
    private const int BRANCH_TILES = 8;           // 가지 이어지는 띠 장수 (128u - 화면 왼쪽 끝을 항상 덮는다)
    private const float HI_BLINK_SEC = 1.2f;      // 강조 띠 깜빡임 주기

    // v3 이하 (레거시) 층 정의
    private const float TILE_W = 16f;
    private const int TILES_PER_LAYER = 4;
    private static readonly float[] SPEED_MUL = { 1.00f, 0.12f, 1.00f };
    private static readonly int[] SORT_ORDER = { -30, -20, -10 };
    private static readonly float[] LAYER_Y = { -(VIEW_HALF_H_LEGACY + 1f), VIEW_HALF_H_LEGACY - 2f, -(VIEW_HALF_H_LEGACY + 1f) };
    private const float RAILS_PNG_Y = -1.9f;

    // 모래 팔레트 (코드 도트 폴백)
    private static readonly Color32 SAND = new Color32(214, 166, 102, 255);
    private static readonly Color32 SAND_D = new Color32(206, 156, 92, 255);
    private static readonly Color32 SAND_L = new Color32(222, 176, 112, 255);
    private static readonly Color32 SPECK_D = new Color32(200, 148, 84, 255);
    private static readonly Color32 SPECK_L = new Color32(228, 184, 122, 255);
    private static readonly Color32 TRACK = new Color32(198, 148, 86, 255);

    // ─────────────────────────────────────────────
    // 내부 상태
    // ─────────────────────────────────────────────
    private bool topdown = false;                // v4 탑다운 모드 (ground_ae + rails_ae 있음)
    private float viewHalfH = VIEW_HALF_H_LEGACY;
    private Transform[] layerRoots = new Transform[3];
    private SpriteRenderer[][] tiles = new SpriteRenderer[3][];
    private float[] tileW = new float[3];        // 층별 타일 폭
    private float[] layerY = new float[3];       // 층별 y
    private int[] cols = new int[3];             // 층별 가로 장수 (세로 줄 수 = tiles.Length / cols)
    private float[] offsets = new float[3];      // 층별 스크롤 오프셋
    private Color[] tintNow = new Color[3];      // 층별 현재 색 (부드러운 전환용)

    private float speedFactor = 0f;              // 0=정차, 1=주행 (가감속으로 변함)
    private bool railsPng = false;               // v3: 레일 층이 PNG 띠인가 (기준 y 전환용)
    private static float externalMul = 1f;       // 외부 배율 (레버 전속 등)

    private static ParallaxBackground instance;

    /// <summary>현재 지면이 흐르는 속도 (월드 유닛/초, 오른쪽 +). 먼지 연출(DustFX)이 읽는다</summary>
    public static float CurrentSpeed { get; private set; }

    // ── v4.2 갈림길 상태 ──
    private Transform forkTf;                    // 갈림길 스프라이트 (층 2 루트의 자식 - 띠와 같이 흐른다)
    private SpriteRenderer forkSr;
    private SpriteRenderer forkHiSr;             // 고른 길 강조 띠 (같은 크기·피벗)
    private float forkStripS;                    // 분기점의 띠 매개변수 s: 월드 x = Repeat(s + offsets[2] + 80, 160) - 80
    private bool forkHasUp, forkHasDn;
    private bool hiOn;
    private int laneSign = 0;                    // 들어가는 가지 (+1 위 / -1 아래 / 0 곧은 길)
    private bool laneActive = false;             // 세계 밀림 진행 중 (가지 띠가 본선이 되면 끝)
    private bool laneEntered = false;            // 분기점 진입 연출(기적·덜컹) 했나
    private bool laneSettled = false;            // 원호가 끝나 평행이 됐나 (덜컹 1회)
    private float railsY = 0f;                   // 띠 층(옛 곧은 선로)·갈림길의 세로 오프셋 = -sign * f(d)
    private float groundY = 0f;                  // 모래 층 세로 오프셋 (누적 - 32u 로 감아 쓴다, 무늬라 이음이 안 보인다)
    private float railsAlpha = 1f;               // 옛 곧은 띠 알파 (페이드)
    private float oldFadeT = -1f;                // 옛 띠 페이드 진행 시간 (-1 = 아직)
    private SpriteRenderer[] branchTiles;        // 고른 가지의 이어지는 띠

    /// <summary>가지 원호를 도는 동안의 화면 기울임 (도, +면 시계 방향으로 보인다). CameraZoom v5 가 읽는다</summary>
    public static float RouteRollDeg { get; private set; }

    // ─────────────────────────────────────────────
    // 자동 부트스트랩 - 파일만 넣으면 게임 시작 시 스스로 생성
    // ─────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("ParallaxBackground");
        DontDestroyOnLoad(go);
        go.AddComponent<ParallaxBackground>();
    }

    /// <summary>주행 속도 외부 배율 (전속 1.5, 서행 0.5 등). 기본 1</summary>
    public static void SetSpeedMultiplier(float mul)
    {
        externalMul = Mathf.Max(0f, mul);
    }

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        topdown = SpriteBank.Has("ground_ae") && SpriteBank.Has("rails_ae");
        if (topdown) BuildTopdownLayers();
        else BuildLegacyLayers();
        DisableLegacyBackground();
        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log("[ParallaxBackground] " + (topdown ? "v4 탑다운 지면 (ground_ae + rails_ae)" : "v3 지면 (지평선 포함)") + " 생성");
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    // 씬 리로드([다시 굽는다]) 후에도 구 배경 정리를 다시 수행
    private void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        DisableLegacyBackground();
        ClearFork();   // v4.2: 런 포기·다시 굽기로 씬이 바뀌면 갈림길·밀림 상태를 버린다 (모래 오프셋은 무늬라 그대로 둬도 된다)
    }

    /// <summary>구 BackgroundScroll 배경이 씬에 남아 있으면 끈다 (겹침 방지, 컴파일 의존 없음)</summary>
    private void DisableLegacyBackground()
    {
        MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].GetType().Name == "BackgroundScroll")
            {
                all[i].gameObject.SetActive(false);
                Debug.Log("[ParallaxBackground] 구 배경 비활성: " + all[i].gameObject.name
                    + " (씬에서 삭제 권장)");
            }
        }
    }

    // ─────────────────────────────────────────────
    // v4: 탑다운 층 생성 (층 0 모래 / 층 2 선로, 층 1 비움)
    // ─────────────────────────────────────────────
    private void BuildTopdownLayers()
    {
        viewHalfH = GameBalance.CamDefaultZoom;   // (탑다운은 스케일 안 함 - 참고값)
        MakeLayer(0, SpriteBank.Get("ground_ae"), GROUND_TILE_W, GROUND_COLS, GROUND_ROWS, 0f, ORDER_GROUND);
        MakeLayer(1, null, 1f, 0, 1, 0f, 0);
        MakeLayer(2, SpriteBank.Get("rails_ae"), RAILS_TILE_W, RAILS_TILES, 1, 0f, ORDER_RAILS);
    }

    private void MakeLayer(int L, Sprite sprite, float w, int colCount, int rowCount, float y, int order)
    {
        GameObject root = new GameObject("Layer" + L);
        root.transform.SetParent(transform, false);
        layerRoots[L] = root.transform;
        int count = colCount * rowCount;
        tiles[L] = new SpriteRenderer[count];
        tileW[L] = w; layerY[L] = y; cols[L] = Mathf.Max(1, colCount); tintNow[L] = Color.white;
        for (int i = 0; i < count; i++)
        {
            GameObject t = new GameObject("Tile" + i);
            t.transform.SetParent(root.transform, false);
            SpriteRenderer sr = t.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            tiles[L][i] = sr;
        }
    }

    // ─────────────────────────────────────────────
    // v3 이하: 층/타일 생성 (모래 a/b, 지평선, 레일)
    // ─────────────────────────────────────────────
    private void BuildLegacyLayers()
    {
        for (int L = 0; L < 3; L++)
        {
            GameObject root = new GameObject("Layer" + L);
            root.transform.SetParent(transform, false);
            layerRoots[L] = root.transform;
            tiles[L] = new SpriteRenderer[TILES_PER_LAYER];
            tileW[L] = TILE_W; layerY[L] = LAYER_Y[L]; cols[L] = TILES_PER_LAYER; tintNow[L] = Color.white;

            // 타일 변형 2종을 번갈아 배치 (반복 티 줄이기)
            Sprite varA = MakeLayerSprite(L, 1000 + L * 77);
            Sprite varB = MakeLayerSprite(L, 5001 + L * 131);   // 홀수 시드 -> PNG 모래 b 변형
            if (L == 2 && railsPng) layerY[L] = RAILS_PNG_Y;

            for (int i = 0; i < TILES_PER_LAYER; i++)
            {
                GameObject t = new GameObject("Tile" + i);
                t.transform.SetParent(root.transform, false);
                SpriteRenderer sr = t.AddComponent<SpriteRenderer>();
                sr.sprite = (i % 2 == 0) ? varA : varB;
                sr.sortingOrder = SORT_ORDER[L];
                tiles[L][i] = sr;
            }
        }
    }

    // ─────────────────────────────────────────────
    // 매 프레임: 속도 상태 -> 스크롤 -> 갈림길·밀림 -> 색 -> 카메라 추종
    // ─────────────────────────────────────────────
    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // 1) 주행 상태: 전투 중에만 달린다 (그 외에는 스르륵 정차)
        float target = 0f;
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.currentState == GameManager.GameState.Battle) target = 1f;
        float rate = target > speedFactor ? ACCEL_RATE : DECEL_RATE;
        speedFactor = Mathf.MoveTowards(speedFactor, target, rate * Time.deltaTime);
        CurrentSpeed = BASE_SPEED * speedFactor * externalMul;

        // 2) 층별 스크롤 (스케일드 시간 - 일시정지/히트스톱 시 배경도 정지)
        float move = CurrentSpeed * Time.deltaTime;
        for (int L = 0; L < 3; L++)
        {
            int n = tiles[L].Length;
            if (n == 0) continue;
            float stripW = tileW[L] * cols[L];
            float mul = topdown ? 1f : SPEED_MUL[L];
            offsets[L] = Mathf.Repeat(offsets[L] + move * mul, stripW);
            LayoutLayer(L);
        }

        // 2.5) v4.2: 갈림길 위치·강조 깜빡임·세계 밀림·옛 띠 페이드
        if (forkTf != null) TickFork(cam);

        // 3) 지역 색: 카메라 배경색을 곱 틴트로 은은하게 (지역 전환 시 자동으로 부드럽게). 층 2 는 옛 띠 페이드 알파를 곱한다
        Color bg = cam.backgroundColor;
        Color want = Color.Lerp(Color.white, Color.Lerp(bg, Color.white, 0.5f), REGION_TINT);
        for (int L = 0; L < 3; L++)
        {
            tintNow[L] = Color.Lerp(tintNow[L], want, TINT_LERP * Time.unscaledDeltaTime);
            Color c = tintNow[L];
            if (L == 2) c.a = railsAlpha;
            for (int i = 0; i < tiles[L].Length; i++)
                tiles[L][i].color = c;
        }
        if (branchTiles != null)
            for (int i = 0; i < branchTiles.Length; i++)
                if (branchTiles[i] != null) branchTiles[i].color = tintNow[2];
        if (forkSr != null) forkSr.color = tintNow[2];

        // 4) 카메라 추종. 탑다운 모드(v4.2): x 도 y 도 세계에 고정 - 정차 카메라가 앞으로 가도 침목이 기차 밑에서 안 미끄러진다. 띠 160u 라 카메라 이동 한계 안에서 빈 데 없음
        //    레거시 모드: 카메라를 따라가고 줌 배율만큼 스케일 (구 지평선 구도 유지)
        if (topdown)
        {
            transform.position = Vector3.zero;
            transform.localScale = Vector3.one;
        }
        else
        {
            transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            float sc = cam.orthographicSize / viewHalfH;
            transform.localScale = new Vector3(sc, sc, 1f);
        }
    }

    /// <summary>층 L 의 타일을 오프셋대로 늘어놓는다 (세로 줄은 타일 높이(=폭, 정사각) 간격). 층 0 은 groundY(32 로 감음), 층 2 는 railsY 만큼 세로로 밀린다</summary>
    private void LayoutLayer(int L)
    {
        int n = tiles[L].Length;
        if (n == 0) return;
        int nc = cols[L]; int nr = n / nc;
        float stripW = tileW[L] * nc;
        float dy = 0f;
        if (topdown && L == 0) dy = Mathf.Repeat(groundY + tileW[0] * 0.5f, tileW[0]) - tileW[0] * 0.5f;
        else if (topdown && L == 2) dy = railsY;
        for (int i = 0; i < n; i++)
        {
            int col = i % nc, row = i / nc;
            // 오프셋만큼 오른쪽으로 (기차가 왼쪽으로 달린다), 벗어나면 반대쪽으로 순환
            float x = Mathf.Repeat(col * tileW[L] + offsets[L] + stripW / 2f, stripW) - stripW / 2f;
            float y = layerY[L] + (row - (nr - 1) * 0.5f) * tileW[L] + dy;
            tiles[L][i].transform.localPosition = new Vector3(x, y, 0f);
        }
    }

    // ─────────────────────────────────────────────
    // v4.2 갈림길 (선로 v2) - 기하: 가지 중심선 = 원호(R, θ) -> 직선 -> 반대 원호 -> 평행 (route.py 와 같은 식)
    // ─────────────────────────────────────────────
    /// <summary>분기점에서 x(u) 앞의 가지 중심 오프셋 (0 -> RouteDY) 과 접선 각(rad). route.py branch_offset 과 같다</summary>
    public static float BranchOffset(float x, out float angRad)
    {
        float R = GameBalance.RouteArcR, DY = GameBalance.RouteDY, th = GameBalance.RouteAngleDeg * Mathf.Deg2Rad;
        float y1 = R * (1f - Mathf.Cos(th));           // 원호 끝 오프셋
        float a1 = R * Mathf.Sin(th);                  // 원호 x 진행
        float ld = Mathf.Max(0f, (DY - 2f * y1) / Mathf.Tan(th));   // 직선 구간
        float total = 2f * a1 + ld;
        angRad = 0f;
        if (x <= 0f) return 0f;
        if (x <= a1) { angRad = Mathf.Asin(Mathf.Min(1f, x / R)); return R - Mathf.Sqrt(Mathf.Max(0f, R * R - x * x)); }
        if (x <= a1 + ld) { angRad = th; return y1 + (x - a1) * Mathf.Tan(th); }
        if (x <= total)
        {
            float e = total - x;
            angRad = Mathf.Asin(Mathf.Min(1f, e / R));
            return DY - (R - Mathf.Sqrt(Mathf.Max(0f, R * R - e * e)));
        }
        return DY;
    }

    /// <summary>분기점부터 평행이 되기까지의 거리 (u, 약 12.5)</summary>
    public static float BranchTotal()
    {
        float R = GameBalance.RouteArcR, th = GameBalance.RouteAngleDeg * Mathf.Deg2Rad;
        float y1 = R * (1f - Mathf.Cos(th)), a1 = R * Mathf.Sin(th);
        return 2f * a1 + Mathf.Max(0f, (GameBalance.RouteDY - 2f * y1) / Mathf.Tan(th));
    }

    /// <summary>
    /// 정차: 두상 앞에 갈림길을 놓는다 (BranchRouteUI 가 부른다). 위 가지만 / 아래 가지만 / 둘 다.
    /// 탑다운 모드가 아니거나 스프라이트가 없으면 false (카드만 뜬다).
    /// </summary>
    public static bool PlaceFork(bool up, bool down)
    {
        if (instance == null || !instance.topdown || !GameBalance.RouteForkOn) return false;
        if (!up && !down) return false;
        instance.ClearFork();
        return instance.PlaceForkInternal(up, down);
    }

    private bool PlaceForkInternal(bool up, bool down)
    {
        // 둘 다 = rails_fork, 위만 = rails_fork_up. 아래만은 위 그림을 세로로 뒤집는다 (피벗이 본선 중심이라 1px 차이)
        string spriteName = (up && down) ? "rails_fork" : "rails_fork_up";
        Sprite sprite = SpriteBank.Get(spriteName);
        if (sprite == null) { Debug.LogWarning("[ParallaxBackground] 갈림길 스프라이트 없음: " + spriteName + " (Resources/Sprites/WDT)"); return false; }

        // 아직 감속 중이면 멈출 때까지 더 흐를 거리만큼 앞(왼쪽)에 놓는다 - 멈춘 뒤 분기점이 RouteForkAheadX 근처에 선다
        float stopDist = (speedFactor > 0f) ? BASE_SPEED * externalMul * speedFactor * speedFactor / (2f * DECEL_RATE) : 0f;
        float xDesired = GameBalance.RouteForkAheadX - stopDist;

        // 분기점 x 를 띠 침목 위상에 스냅: 분기점이 든 타일의 왼쪽 끝 + (7 + 20k) px
        float tileLeft = xDesired - RAILS_TILE_W * 0.5f;
        for (int i = 0; i < tiles[2].Length; i++)
        {
            float tx = tiles[2][i].transform.localPosition.x;
            if (xDesired >= tx - RAILS_TILE_W * 0.5f && xDesired < tx + RAILS_TILE_W * 0.5f) { tileLeft = tx - RAILS_TILE_W * 0.5f; break; }
        }
        float step = SLEEPER_STEP_PX / SPRITE_PPU, phase = FORK_SLEEPER_PHASE_PX / SPRITE_PPU;
        float k = Mathf.Round((xDesired - tileLeft - phase) / step);
        float jx = tileLeft + phase + k * step;
        float stripW = tileW[2] * cols[2];
        forkStripS = Mathf.Repeat(jx - offsets[2], stripW);

        GameObject go = new GameObject("RailsFork");
        go.transform.SetParent(layerRoots[2], false);
        forkTf = go.transform;
        forkSr = go.AddComponent<SpriteRenderer>();
        forkSr.sprite = sprite;
        forkSr.sortingOrder = ORDER_FORK;
        forkSr.flipY = (!up && down);
        forkSr.color = tintNow[2];

        GameObject hi = new GameObject("RailsForkHi");
        hi.transform.SetParent(go.transform, false);
        forkHiSr = hi.AddComponent<SpriteRenderer>();
        forkHiSr.sortingOrder = ORDER_FORK_HI;
        forkHiSr.enabled = false;

        forkHasUp = up; forkHasDn = down; hiOn = false;
        laneSign = 0; laneActive = false; laneEntered = false; laneSettled = false;
        forkTf.localPosition = new Vector3(jx, railsY, 0f);
        Debug.Log("[ParallaxBackground] 갈림길 놓음 x " + jx.ToString("F2") + " (" + spriteName + ", 멈출 거리 " + stopDist.ToString("F1") + ")");
        return true;
    }

    /// <summary>고른 길 강조 (+1 위 / 0 곧은 / -1 아래). 깜빡이는 금색 띠. 갈림길이 없으면 무시</summary>
    public static void SetHighlight(int sign)
    {
        if (instance == null || instance.forkHiSr == null) return;
        string name = sign > 0 ? "rails_fork_hi_up" : sign < 0 ? "rails_fork_hi_down" : "rails_fork_hi_straight";
        // 아래만 있는 갈림길(위 그림 뒤집음)은 위 강조를 같이 뒤집는다
        if (instance.forkSr != null && instance.forkSr.flipY && sign < 0) name = "rails_fork_hi_up";
        Sprite sp = SpriteBank.Get(name);
        instance.forkHiSr.sprite = sp;
        instance.forkHiSr.flipY = instance.forkSr != null && instance.forkSr.flipY;
        instance.forkHiSr.enabled = sp != null;
        instance.hiOn = sp != null;
    }

    /// <summary>강조 끄기</summary>
    public static void ClearHighlight()
    {
        if (instance == null || instance.forkHiSr == null) return;
        instance.forkHiSr.enabled = false;
        instance.hiOn = false;
    }

    /// <summary>
    /// 가지(sign) 중심선이 월드 x 를 지나는 높이 (월드 y). 갈림길이 없으면 평행 높이(sign * RouteDY). 카드 위치용 (BranchRouteUI)
    /// </summary>
    public static float BranchY(int sign, float worldX)
    {
        if (instance == null) return sign * GameBalance.RouteDY;
        if (instance.forkTf == null) return instance.railsY + sign * GameBalance.RouteDY;
        if (sign == 0) return instance.railsY;
        float u0 = instance.forkTf.localPosition.x - (sign < 0 ? GameBalance.RouteStagger : 0f);
        float ang;
        return instance.railsY + sign * BranchOffset(Mathf.Max(0f, u0 - worldX), out ang);
    }

    /// <summary>
    /// 출발: 고른 길로 들어간다. sign 0(곧은 길) = 밀림 없이 갈림길이 흘러가 화면 밖에서 사라진다.
    /// 밀림은 두상 앞이 분기점을 지나면서 LateUpdate 가 매 프레임 f(d) 로 계산한다 (지면 속도가 어떻든 기하가 맞는다)
    /// </summary>
    public static void BeginLaneShift(int sign)
    {
        if (instance == null) return;
        ClearHighlight();
        RouteRollDeg = 0f;
        if (instance.forkTf == null) return;
        instance.laneSign = sign;
        instance.laneActive = sign != 0;
        instance.laneEntered = false; instance.laneSettled = false;
        if (sign != 0) instance.MakeBranchTiles();
        Debug.Log("[ParallaxBackground] 출발 - " + (sign > 0 ? "위 가지" : sign < 0 ? "아래 가지" : "곧은 길"));
    }

    /// <summary>고른 가지의 이어지는 띠: rails_ae 8장, 갈림길 왼쪽 끝에 오른쪽 끝을 딱 붙여 왼쪽으로 (위치는 TickFork 가 매 프레임)</summary>
    private void MakeBranchTiles()
    {
        DestroyBranchTiles();
        Sprite sp = SpriteBank.Get("rails_ae");
        branchTiles = new SpriteRenderer[BRANCH_TILES];
        for (int i = 0; i < BRANCH_TILES; i++)
        {
            GameObject t = new GameObject("BranchTile" + i);
            t.transform.SetParent(layerRoots[2], false);
            SpriteRenderer sr = t.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = ORDER_RAILS;
            sr.color = tintNow[2];
            branchTiles[i] = sr;
        }
    }

    private void DestroyBranchTiles()
    {
        if (branchTiles == null) return;
        for (int i = 0; i < branchTiles.Length; i++)
            if (branchTiles[i] != null) Destroy(branchTiles[i].gameObject);
        branchTiles = null;
    }

    /// <summary>갈림길·가지 띠·밀림 상태를 전부 버린다 (선택 취소, 씬 전환, 곧은 길이 흘러간 뒤). 모래 오프셋은 남긴다</summary>
    public void ClearFork()
    {
        if (forkTf != null) Destroy(forkTf.gameObject);
        forkTf = null; forkSr = null; forkHiSr = null;
        DestroyBranchTiles();
        laneActive = false; laneSign = 0; hiOn = false;
        railsY = 0f; railsAlpha = 1f; oldFadeT = -1f;
        RouteRollDeg = 0f;
        if (tiles[2] != null && tiles[2].Length > 0) LayoutLayer(2);
    }

    /// <summary>ClearFork 의 정적 진입 (WaveManager 치트 점프·런 포기)</summary>
    public static void CancelFork()
    {
        if (instance != null) instance.ClearFork();
    }

    /// <summary>매 프레임: 갈림길을 띠 자리에 두고, 밀림·강조·페이드를 진행한다</summary>
    private void TickFork(Camera cam)
    {
        float stripW = tileW[2] * cols[2];
        float fx = Mathf.Repeat(forkStripS + offsets[2] + stripW / 2f, stripW) - stripW / 2f;
        forkTf.localPosition = new Vector3(fx, railsY, 0f);
        float pivotU = forkSr.sprite != null ? forkSr.sprite.pivot.x / forkSr.sprite.pixelsPerUnit : 16.1f;
        float forkLeft = fx - pivotU;   // 스프라이트 왼쪽(앞) 끝

        // 강조 깜빡임 (실시간 - 정차 중 시간이 멈춰도 깜빡인다)
        if (hiOn && forkHiSr != null)
        {
            float a = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / HI_BLINK_SEC));
            forkHiSr.color = new Color(1f, 1f, 1f, a);
        }

        // 세계 밀림: 두상 앞이 (고른 가지의) 분기점을 지난 거리 d -> f(d)
        if (laneActive)
        {
            float u0 = fx - (laneSign < 0 ? GameBalance.RouteStagger : 0f);
            float d = u0 - GameBalance.RouteHeadFrontX;
            float ang;
            float f = BranchOffset(Mathf.Max(0f, d), out ang);
            float newRailsY = -laneSign * f;
            float delta = newRailsY - railsY;
            if (Mathf.Abs(delta) > 1e-5f)
            {
                railsY = newRailsY;
                groundY += delta;
                LayoutLayer(0); LayoutLayer(2);
                forkTf.localPosition = new Vector3(fx, railsY, 0f);
                RouteFX.ShiftWorldObjects(delta);
            }
            float th = GameBalance.RouteAngleDeg * Mathf.Deg2Rad;
            RouteRollDeg = th > 0f ? laneSign * GameBalance.RouteShiftRollDeg * (ang / th) : 0f;

            if (!laneEntered && d > 0f)
            {
                laneEntered = true;   // 분기점 진입: 기적 + 덜컹
                SoundManager.Play("sfx_train_whistle");
                GameFeel.Shake(0.12f);
            }
            if (!laneSettled && d >= BranchTotal())
            {
                laneSettled = true;   // 평행이 됐다: 덜컹 한 번 더
                GameFeel.Shake(0.08f);
                RouteRollDeg = 0f;
            }

            // 가지 이어지는 띠 - 갈림길 왼쪽 끝에 오른쪽 끝을 붙인다
            if (branchTiles != null)
            {
                float by = railsY + laneSign * GameBalance.RouteDY;
                for (int i = 0; i < branchTiles.Length; i++)
                    if (branchTiles[i] != null)
                        branchTiles[i].transform.localPosition = new Vector3(forkLeft - RAILS_TILE_W * 0.5f - RAILS_TILE_W * i, by, 0f);
            }
        }

        // 갈림길이 화면 오른쪽 밖으로 나갔다
        float halfW = cam.orthographicSize * cam.pixelWidth / Mathf.Max(1, cam.pixelHeight);
        bool offRight = forkLeft > cam.transform.position.x + halfW + 1f;
        if (offRight)
        {
            if (laneActive)
            {
                if (oldFadeT < 0f) oldFadeT = 0f;   // 옛 곧은 띠 페이드 시작
            }
            else
            {
                ClearFork();   // 곧은 길(또는 선택 없이 출발): 그냥 치운다
                return;
            }
        }

        if (oldFadeT >= 0f)
        {
            oldFadeT += Time.deltaTime;
            railsAlpha = 1f - Mathf.Clamp01(oldFadeT / Mathf.Max(0.05f, GameBalance.RouteOldRailFadeSec));
            if (railsAlpha <= 0f) RebaseToBranch(forkLeft);
        }
    }

    /// <summary>옛 곧은 띠가 다 사라졌다: 가지 띠를 본선(층 2)으로 삼는다 - 오프셋 위상·높이가 같아 바꿔치기가 눈에 안 띈다</summary>
    private void RebaseToBranch(float forkLeft)
    {
        // 본선 타일 중심 x ≡ offsets[2] (mod 16). 가지 타일 중심 = forkLeft - 8 - 16i
        offsets[2] = Mathf.Repeat(forkLeft - RAILS_TILE_W * 0.5f, RAILS_TILE_W);
        railsY = 0f;
        railsAlpha = 1f;
        oldFadeT = -1f;
        laneActive = false; laneSign = 0;
        RouteRollDeg = 0f;
        DestroyBranchTiles();
        if (forkTf != null) Destroy(forkTf.gameObject);
        forkTf = null; forkSr = null; forkHiSr = null;
        LayoutLayer(2);
        Debug.Log("[ParallaxBackground] 가지 띠가 본선이 됐다");
    }

    // ─────────────────────────────────────────────
    // v3 이하: 타일 스프라이트 (PNG 우선, 없으면 코드 도트)
    // ─────────────────────────────────────────────
    private Sprite MakeLayerSprite(int layer, int seed)
    {
        Sprite png = null;
        if (layer == 0) png = SpriteBank.Get(seed % 2 == 0 ? "ground_a" : "ground_b");
        else if (layer == 1) png = SpriteBank.Get("horizon");
        else { png = SpriteBank.Get("rails"); railsPng = png != null; }
        if (png != null) return png;

        if (layer == 0) return MakeSand(seed);
        if (layer == 1) return MakeHorizon(seed);
        return MakeRailsAndProps(seed);
    }

    /// <summary>층 0: 모래 지면 16x16 유닛 - 큰 명암 패치(경계 안쪽) + 스펙클</summary>
    private Sprite MakeSand(int seed)
    {
        int w = 256, h = 256;
        PixelPainter p = new PixelPainter(w, h);
        Random.State backup = Random.state;
        Random.InitState(seed);

        p.Rect(0, 0, w - 1, h - 1, SAND);
        int patches = Random.Range(6, 9);
        for (int i = 0; i < patches; i++)
        {
            int pw = Random.Range(40, 90), ph = Random.Range(24, 60);
            int px = Random.Range(2, w - pw - 2), py = Random.Range(2, h - ph - 2);
            p.Ellipse(px, py, px + pw, py + ph, Random.value < 0.5f ? SAND_D : SAND_L, PixelPainter.CLEAR);
        }
        for (int i = 0; i < 700; i++)
        {
            int gx = Random.Range(0, w - 1), gy = Random.Range(0, h);
            Color32 c = Random.value < 0.5f ? SPECK_D : SPECK_L;
            p.Point(gx, gy, c); p.Point(gx + 1, gy, c);
        }
        Random.state = backup;
        return p.Bake(PPU, w * 0.5f, h);   // 피벗 = 아래 중앙
    }

    /// <summary>층 1: 지평선 띠 16x2 유닛 - 하늘 그라데이션 + 원경 메사 2톤 + 지평선 + 모래 이음</summary>
    private Sprite MakeHorizon(int seed)
    {
        int w = 256, h = 32;
        PixelPainter p = new PixelPainter(w, h);
        Random.State backup = Random.state;
        Random.InitState(seed);

        for (int y = 0; y < 26; y++)
        {
            float t = y / 26f;
            p.Rect(0, y, w - 1, y, new Color32((byte)(246 - 14 * t), (byte)(224 - 30 * t), (byte)(178 - 40 * t), 255));
        }
        int x = Random.Range(0, 30);
        while (x < w - 40)
        {
            int mw = Random.Range(40, 90), mh = Random.Range(9, 17);
            if (x + mw > w - 2) break;
            p.Polygon(new int[] { x, 26, x + 5, 26 - mh, x + mw - 5, 26 - mh, x + mw, 26 },
                new Color32(196, 138, 96, 255), PixelPainter.CLEAR);
            p.Polygon(new int[] { x + 3, 26, x + 7, 28 - mh, x + mw / 2, 28 - mh, x + mw / 2, 26 },
                new Color32(210, 156, 112, 255), PixelPainter.CLEAR);
            x += mw + Random.Range(10, 40);
        }
        p.Rect(0, 26, w - 1, 26, new Color32(160, 108, 70, 255));   // 지평선
        p.Rect(0, 27, w - 1, h - 1, SAND);                          // 아래 모래층과 이음
        Random.state = backup;
        return p.Bake(PPU, w * 0.5f, h);
    }

    /// <summary>층 2: 레일(기차 밑) + 바퀴 자국 + 소품 (기차 띠 y -2.6~2.6 바깥에만)</summary>
    private Sprite MakeRailsAndProps(int seed)
    {
        int w = 256, h = 256;
        PixelPainter p = new PixelPainter(w, h);
        Random.State backup = Random.state;
        Random.InitState(seed);

        int railTop = h - Mathf.RoundToInt((-0.4f + 8f) * PPU);
        int railBot = h - Mathf.RoundToInt((-1.9f + 8f) * PPU);
        for (int gx = 0; gx < w; gx += 2)
            for (int gy = railTop; gy <= railBot; gy += 3)
                if (Random.value < 0.5f) p.Point(gx + Random.Range(0, 2), gy + Random.Range(0, 3), new Color32(186, 148, 104, 255));
        for (int sx = 0; sx < w; sx += 16)
        {
            p.Rect(sx, railTop, sx + 7, railBot, PixelPainter.WD);
            p.Rect(sx, railTop, sx + 7, railTop + 1, PixelPainter.WD_H);
            p.Rect(sx, railBot - 1, sx + 7, railBot, PixelPainter.WD_D);
            p.Point(sx + 2, railTop + 3, PixelPainter.IR_O); p.Point(sx + 5, railBot - 3, PixelPainter.IR_O);
        }
        int[] railY = { railTop + 3, railBot - 6 };
        for (int i = 0; i < 2; i++)
        {
            int ry = railY[i];
            p.Rect(0, ry, w - 1, ry + 2, new Color32(96, 96, 108, 255));
            p.Rect(0, ry, w - 1, ry, new Color32(196, 198, 208, 255));
            p.Rect(0, ry + 3, w - 1, ry + 3, new Color32(52, 52, 62, 255));
        }
        int trackY = h - Mathf.RoundToInt((-2.45f + 8f) * PPU);
        for (int gx = 0; gx < w; gx += 3) { p.Point(gx, trackY, TRACK); p.Point(gx, trackY + 3, TRACK); }

        int bandTop = h - Mathf.RoundToInt((2.6f + 8f) * PPU);
        int bandBot = h - Mathf.RoundToInt((-2.6f + 8f) * PPU);
        int props = Random.Range(10, 15);
        for (int i = 0; i < props; i++)
        {
            int px = Random.Range(12, w - 24);
            int py = Random.value < 0.4f ? Random.Range(8, bandTop - 20) : Random.Range(bandBot + 8, h - 24);
            float roll = Random.value;
            if (roll < 0.35f) Rock(p, px, py, Random.Range(12, 20), Random.Range(8, 12));
            else if (roll < 0.55f) Cactus(p, px, py);
            else if (roll < 0.65f) Skull(p, px, py);
            else Grass(p, px, py);
        }

        Random.state = backup;
        return p.Bake(PPU, w * 0.5f, h);
    }

    // ── 소품 (코드 도트 폴백) ──
    private static void Rock(PixelPainter p, int x, int y, int w, int h)
    {
        p.Shadow(x + 1, y + h - 3, x + w + 1, y + h + 2);
        p.Ellipse(x, y, x + w, y + h, new Color32(150, 104, 58, 255), new Color32(96, 62, 34, 255));
        p.Ellipse(x + 2, y + 1, x + w - 3, y + h - 4, new Color32(176, 126, 72, 255), PixelPainter.CLEAR);
        p.Ellipse(x + 3, y + 2, x + w / 2 + 2, y + h / 2, new Color32(198, 148, 90, 255), PixelPainter.CLEAR);
    }

    private static void Cactus(PixelPainter p, int x, int y)
    {
        Color32 g = new Color32(74, 120, 58, 255), gO = new Color32(40, 72, 30, 255), gL = new Color32(108, 156, 84, 255);
        p.Shadow(x - 3, y + 9, x + 7, y + 13);
        int[] ax = { 0, -4, 5 }; int[] ay = { 0, 3, 1 }; int[] aw = { 4, 3, 3 }; int[] ah = { 12, 5, 5 };
        for (int i = 0; i < 3; i++)
        {
            p.RoundRect(x + ax[i], y + ay[i], x + ax[i] + aw[i], y + ay[i] + ah[i], 2, g, gO);
            p.Line(x + ax[i] + 1, y + ay[i] + 1, x + ax[i] + 1, y + ay[i] + ah[i] - 1, gL, 1);
        }
        p.Point(x + 2, y - 1, new Color32(232, 120, 140, 255));   // 꽃
    }

    private static void Skull(PixelPainter p, int x, int y)
    {
        Color32 bone = new Color32(236, 226, 206, 255);
        p.Ellipse(x, y, x + 9, y + 6, bone, new Color32(150, 134, 110, 255));
        p.Line(x - 3, y + 1, x, y + 2, bone, 1); p.Line(x + 9, y + 2, x + 12, y + 1, bone, 1);   // 뿔
        p.Point(x + 3, y + 2, new Color32(60, 50, 40, 255)); p.Point(x + 6, y + 2, new Color32(60, 50, 40, 255));
    }

    private static void Grass(PixelPainter p, int x, int y)
    {
        Color32 g = new Color32(122, 142, 74, 255);
        p.Line(x - 2, y, x - 3, y - 4, g, 1); p.Line(x, y, x + 1, y - 4, g, 1); p.Line(x + 2, y, x + 3, y - 4, g, 1);
    }
}
