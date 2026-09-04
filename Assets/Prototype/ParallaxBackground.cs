using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [ParallaxBackground.cs] v3 - 고퀄 PNG 지면 (2026-09-03) / v2 탑뷰 지면 스크롤 (2026-09-02)
///
/// - v3: Resources/Sprites/WDT/ 의 ground_a, ground_b(모래 16x16유닛) / horizon(지평선 띠 16x2) / rails(레일 띠 16x2)를
///   SpriteBank로 우선 사용. 없으면 v2 코드 도트. PNG 레일은 16x2 띠라 층 2의 기준 y가 -1.9(레일 밑선)로 바뀐다.
///
/// v1은 사이드뷰용(능선/바위 실루엣/자갈 스트립)이었다. 탑뷰 확정으로 "지면 타일 스크롤"이 주인공이 되고
/// 하늘 패럴랙스는 화면 상단의 얇은 지평선 띠(2.5D 단서)로 축소됐다. 전부 코드 생성 도트 (PixelPainter.cs).
///
/// 구성 (3층, 전부 16x? 월드 타일 4장 순환):
///  - 층 0 모래 지면 (1.00배속): 큰 명암 패치 + 통제된 스펙클. 화면 전체를 덮는다 (정렬 -30)
///  - 층 1 지평선 띠 (0.12배속): 하늘 그라데이션 + 원경 메사 2톤 + 지평선 (화면 최상단, 정렬 -20)
///  - 층 2 레일 + 소품 (1.00배속): 침목/자갈/2줄 레일(기차 밑) + 바퀴 자국 + 바위/선인장/소 두개골/풀 (정렬 -10)
///
/// 방향: 기차는 두상 쪽(왼쪽)으로 달린다 -> 지면은 오른쪽으로 흐른다 (v1은 반대. EngineCab의 바위와 동일 방향)
///
/// 동작 (v1 유지):
///  - 전투(Battle) 중에만 목표 속도 1.0, 그 외는 0 -> 스르륵 가감속 (정차/출발 연출)
///  - 지역 색: 카메라 배경색을 곱 틴트로 은은하게 (밝기 3~6 왕복은 카메라 배경색이 담당)
///  - 줌 배율을 따라 스케일 -> 줌인/줌아웃해도 구도 유지 / 스케일드 시간 -> 일시정지 시 정지
///
/// 사용법: 없음! 파일만 넣으면 게임 시작 시 스스로 생성된다. (PixelPainter.cs 필요)
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
    private const float TILE_W = 16f;           // 타일 1장의 가로 폭 (월드 단위)
    private const int TILES_PER_LAYER = 4;      // 층당 타일 수 (총 폭 64 - 울트라와이드 커버)
    private const float VIEW_HALF_H = 7f;       // 기준 줌 (CameraZoom defaultZoom과 동일)
    private const float PPU = 16f;              // 지면 도트 배율 (16px = 1유닛. 기차 20보다 살짝 성글게 - 배경이 뒤로 물러난다)

    // 층 정의: 속도 배율 / 정렬 순서 / 타일 기준 y(피벗 아래)
    private static readonly float[] SPEED_MUL = { 1.00f, 0.12f, 1.00f };
    private static readonly int[] SORT_ORDER = { -30, -20, -10 };
    private static readonly float[] LAYER_Y = { -(VIEW_HALF_H + 1f), VIEW_HALF_H - 2f, -(VIEW_HALF_H + 1f) };
    private const float RAILS_PNG_Y = -1.9f;     // v3: PNG 레일 띠(16x2)의 밑선 = 월드 -1.9 (기차 남벽 바로 아래)

    // 모래 팔레트 (목업 v2)
    private static readonly Color32 SAND = new Color32(214, 166, 102, 255);
    private static readonly Color32 SAND_D = new Color32(206, 156, 92, 255);
    private static readonly Color32 SAND_L = new Color32(222, 176, 112, 255);
    private static readonly Color32 SPECK_D = new Color32(200, 148, 84, 255);
    private static readonly Color32 SPECK_L = new Color32(228, 184, 122, 255);
    private static readonly Color32 TRACK = new Color32(198, 148, 86, 255);

    // ─────────────────────────────────────────────
    // 내부 상태
    // ─────────────────────────────────────────────
    private Transform[] layerRoots = new Transform[3];
    private SpriteRenderer[][] tiles = new SpriteRenderer[3][];
    private float[] offsets = new float[3];      // 층별 스크롤 오프셋
    private Color[] tintNow = new Color[3];      // 층별 현재 색 (부드러운 전환용)

    private float speedFactor = 0f;              // 0=정차, 1=주행 (가감속으로 변함)
    private bool railsPng = false;               // v3: 레일 층이 PNG 띠인가 (기준 y 전환용)
    private static float externalMul = 1f;       // 외부 배율 (레버 전속 등)

    private static ParallaxBackground instance;

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

        BuildLayers();
        DisableLegacyBackground();
        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log("[ParallaxBackground] 탑뷰 지면 배경 생성 (모래/지평선/레일+소품)");
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
    // 층/타일 생성
    // ─────────────────────────────────────────────
    private void BuildLayers()
    {
        for (int L = 0; L < 3; L++)
        {
            GameObject root = new GameObject("Layer" + L);
            root.transform.SetParent(transform, false);
            layerRoots[L] = root.transform;
            tiles[L] = new SpriteRenderer[TILES_PER_LAYER];
            tintNow[L] = Color.white;

            // 타일 변형 2종을 번갈아 배치 (반복 티 줄이기)
            Sprite varA = MakeLayerSprite(L, 1000 + L * 77);
            Sprite varB = MakeLayerSprite(L, 5001 + L * 131);   // 홀수 시드 -> PNG 모래 b 변형

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
    // 매 프레임: 속도 상태 -> 스크롤 -> 색 -> 카메라 추종
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

        // 2) 층별 스크롤 (스케일드 시간 - 일시정지/히트스톱 시 배경도 정지)
        float move = BASE_SPEED * speedFactor * externalMul * Time.deltaTime;
        float stripW = TILE_W * TILES_PER_LAYER;
        for (int L = 0; L < 3; L++)
        {
            offsets[L] = Mathf.Repeat(offsets[L] + move * SPEED_MUL[L], stripW);
            for (int i = 0; i < TILES_PER_LAYER; i++)
            {
                // v2: 오프셋만큼 오른쪽으로 (기차가 왼쪽으로 달린다), 벗어나면 반대쪽으로 순환
                float x = Mathf.Repeat(i * TILE_W + offsets[L] + stripW / 2f, stripW) - stripW / 2f;
                float ly = (L == 2 && railsPng) ? RAILS_PNG_Y : LAYER_Y[L];
                tiles[L][i].transform.localPosition = new Vector3(x, ly, 0f);
            }
        }

        // 3) 지역 색: 카메라 배경색을 곱 틴트로 은은하게 (지역 전환 시 자동으로 부드럽게)
        Color bg = cam.backgroundColor;
        Color want = Color.Lerp(Color.white, Color.Lerp(bg, Color.white, 0.5f), REGION_TINT);
        for (int L = 0; L < 3; L++)
        {
            tintNow[L] = Color.Lerp(tintNow[L], want, TINT_LERP * Time.unscaledDeltaTime);
            for (int i = 0; i < TILES_PER_LAYER; i++)
                tiles[L][i].color = tintNow[L];
        }

        // 4) 카메라 추종 + 줌 스케일 (줌아웃해도 구도 유지)
        transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        float s = cam.orthographicSize / VIEW_HALF_H;
        transform.localScale = new Vector3(s, s, 1f);
    }

    // ─────────────────────────────────────────────
    // 타일 도트 생성 (목업 v2 hq2.py 지면/레일/소품 문법 이식)
    // ─────────────────────────────────────────────
    private Sprite MakeLayerSprite(int layer, int seed)
    {
        // v3: PNG 우선 (모래는 a/b 2종을 시드로 번갈아), 없으면 코드 도트
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
        // 큰 명암 패치 (타일 경계를 넘지 않게 - 이음새 보호)
        int patches = Random.Range(6, 9);
        for (int i = 0; i < patches; i++)
        {
            int pw = Random.Range(40, 90), ph = Random.Range(24, 60);
            int px = Random.Range(2, w - pw - 2), py = Random.Range(2, h - ph - 2);
            p.Ellipse(px, py, px + pw, py + ph, Random.value < 0.5f ? SAND_D : SAND_L, PixelPainter.CLEAR);
        }
        // 통제된 스펙클 (2px 가로 점)
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
        // 원경 메사 (경계 안쪽에만)
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

        // 월드 y -> 캔버스 행 (피벗 아래 중앙, 타일 바닥 = 월드 -8)
        // 레일 밴드: 월드 -1.9 ~ -0.4 (기차 몸통 남쪽 절반 밑 - 칸 사이 틈과 기차 양끝에서 보인다)
        int railTop = h - Mathf.RoundToInt((-0.4f + 8f) * PPU);     // 행 (위)
        int railBot = h - Mathf.RoundToInt((-1.9f + 8f) * PPU);     // 행 (아래)
        // 자갈
        for (int gx = 0; gx < w; gx += 2)
            for (int gy = railTop; gy <= railBot; gy += 3)
                if (Random.value < 0.5f) p.Point(gx + Random.Range(0, 2), gy + Random.Range(0, 3), new Color32(186, 148, 104, 255));
        // 침목 (16px 간격 - 256의 약수라 이음새 없음)
        for (int sx = 0; sx < w; sx += 16)
        {
            p.Rect(sx, railTop, sx + 7, railBot, PixelPainter.WD);
            p.Rect(sx, railTop, sx + 7, railTop + 1, PixelPainter.WD_H);
            p.Rect(sx, railBot - 1, sx + 7, railBot, PixelPainter.WD_D);
            p.Point(sx + 2, railTop + 3, PixelPainter.IR_O); p.Point(sx + 5, railBot - 3, PixelPainter.IR_O);
        }
        // 2줄 레일
        int[] railY = { railTop + 3, railBot - 6 };
        for (int i = 0; i < 2; i++)
        {
            int ry = railY[i];
            p.Rect(0, ry, w - 1, ry + 2, new Color32(96, 96, 108, 255));
            p.Rect(0, ry, w - 1, ry, new Color32(196, 198, 208, 255));
            p.Rect(0, ry + 3, w - 1, ry + 3, new Color32(52, 52, 62, 255));
        }
        // 바퀴 자국 (기차 남쪽, 레일과 평행한 옅은 점선 2줄)
        int trackY = h - Mathf.RoundToInt((-2.45f + 8f) * PPU);
        for (int gx = 0; gx < w; gx += 3) { p.Point(gx, trackY, TRACK); p.Point(gx, trackY + 3, TRACK); }

        // 소품: 기차 띠(월드 -2.6~2.6 = 행 bandTop~bandBot) 바깥에만
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

    // ── 소품 (목업 v2 rock/cactus/skull/grass 이식) ──
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
