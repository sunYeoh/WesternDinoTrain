using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [ParallaxBackground.cs] v1 (신규 파일) - 도박수 1순위: 패럴랙스 주행 배경 1단계
/// 기차는 제자리, 배경 3겹이 서로 다른 속도로 흘러 "달리는 기차"를 만든다.
///
/// 구성 (전부 코드 생성 - 아트 에셋 불필요, 나중에 스프라이트만 교체):
///  - 원경(0.12배속): 능선 실루엣. 멀어서 거의 안 움직임
///  - 중경(0.40배속): 바위/선인장 실루엣
///  - 근경(1.00배속): 자갈 지면 스트립. 제일 빨라서 속도감 담당
///
/// 동작:
///  - 전투(Battle) 중에만 목표 속도 1.0, 그 외(로비/마을/게임오버/승리)는 0
///    -> 즉시 멈추지 않고 스르륵 감속/가속 (기차 정차/출발 연출)
///  - 층 색은 매 프레임 카메라 배경색에서 파생 -> 지역 전환 시 자동으로 부드럽게 물듦
///  - 줌 배율을 따라 스케일 -> 줌인/줌아웃해도 배경 구도가 유지 (스카이박스처럼)
///  - 스크롤은 스케일드 시간 -> 일시정지/히트스톱이면 배경도 같이 멈춤
///
/// 사용법: 없음! 이 파일만 넣으면 게임 시작 시 스스로 생성된다.
///  - 단, 구 배경은 정리할 것: 씬(Hierarchy)의 Background_1/2/3 삭제 + BackgroundScroll.cs 파일 삭제
///    (잊어도 이 스크립트가 구 배경을 자동 비활성해서 겹치지는 않음)
///  - 2단계(속도 스탯화: 부스터/정지 기믹)용 훅: ParallaxBackground.SetSpeedMultiplier(배율)
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // 튜닝 상수 (여기 숫자로 조절)
    // ─────────────────────────────────────────────
    private const float BASE_SPEED = 3.2f;      // 근경 기준 스크롤 속도 (월드 단위/초)
    private const float ACCEL_RATE = 0.55f;     // 출발 가속 (1.0까지 약 1.8초)
    private const float DECEL_RATE = 0.45f;     // 정차 감속 (0까지 약 2.2초)
    private const float TINT_LERP = 1.2f;       // 지역 색 전환 속도
    private const float TILE_W = 16f;           // 타일 1장의 가로 폭 (월드 단위)
    private const int TILES_PER_LAYER = 4;      // 층당 타일 수 (총 폭 64 - 울트라와이드 커버)
    private const float VIEW_HALF_H = 7f;       // 기준 줌 (CameraZoom defaultZoom과 동일)

    // 층 정의: 속도 배율 / 밝기 보정(카메라 배경색 -> 흰색 방향 비율) / 정렬 순서
    private static readonly float[] SPEED_MUL = { 0.12f, 0.40f, 1.00f };
    private static readonly float[] LIGHTEN = { 0.10f, 0.22f, 0.34f };
    private static readonly int[] SORT_ORDER = { -30, -20, -10 };

    // ─────────────────────────────────────────────
    // 내부 상태
    // ─────────────────────────────────────────────
    private Transform[] layerRoots = new Transform[3];
    private SpriteRenderer[][] tiles = new SpriteRenderer[3][];
    private float[] offsets = new float[3];      // 층별 스크롤 오프셋
    private Color[] tintNow = new Color[3];      // 층별 현재 색 (부드러운 전환용)

    private float speedFactor = 0f;              // 0=정차, 1=주행 (가감속으로 변함)
    private static float externalMul = 1f;       // 2단계용 외부 배율 (부스터 등)

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

    /// <summary>2단계 훅: 주행 속도 외부 배율 (부스터 1.5, 서행 0.5 등). 기본 1</summary>
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

        Debug.Log("[ParallaxBackground] 패럴랙스 배경 생성 (3겹)");
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
            tintNow[L] = Color.black;

            // 타일 변형 2종을 번갈아 배치 (반복 티 줄이기)
            Sprite varA = MakeLayerSprite(L, 1000 + L * 77);
            Sprite varB = MakeLayerSprite(L, 5000 + L * 131);

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
                // 기준 위치에서 오프셋만큼 왼쪽으로, 벗어나면 반대쪽으로 순환
                float x = Mathf.Repeat(i * TILE_W - offsets[L] + stripW / 2f, stripW) - stripW / 2f;
                tiles[L][i].transform.localPosition = new Vector3(x, -(VIEW_HALF_H + 1f), 0f);
            }
        }

        // 3) 지역 색: 카메라 배경색에서 층별로 파생 (지역 전환 시 자동으로 부드럽게)
        Color bg = cam.backgroundColor;
        for (int L = 0; L < 3; L++)
        {
            Color want = Color.Lerp(bg, Color.white, LIGHTEN[L]);
            tintNow[L] = Color.Lerp(tintNow[L], want, TINT_LERP * Time.unscaledDeltaTime);
            for (int i = 0; i < TILES_PER_LAYER; i++)
                tiles[L][i].color = tintNow[L];
        }

        // 4) 카메라 추종 + 줌 스케일 (줌아웃해도 배경 구도 유지 - 스카이박스처럼)
        transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        float s = cam.orthographicSize / VIEW_HALF_H;
        transform.localScale = new Vector3(s, s, 1f);
    }

    // ─────────────────────────────────────────────
    // 프로시저럴 스프라이트 생성 (픽셀 아트 톤, 흰색으로 그리고 color로 물들임)
    // ─────────────────────────────────────────────
    private Sprite MakeLayerSprite(int layer, int seed)
    {
        if (layer == 0) return MakeRidge(seed);
        if (layer == 1) return MakeRocks(seed);
        return MakeGround(seed);
    }

    /// <summary>원경: 능선 실루엣 (아래는 지면까지 꽉 채움)</summary>
    private Sprite MakeRidge(int seed)
    {
        int w = 256, h = 176;   // 16 x 11 월드 (바닥 -8 ~ 꼭대기 +3)
        Texture2D tex = NewTex(w, h);
        Random.State backup = Random.state;
        Random.InitState(seed);
        float p1 = Random.Range(0f, 100f);
        float p2 = Random.Range(0f, 100f);

        for (int x = 0; x < w; x++)
        {
            float u = x / (float)(w - 1);
            // 완만한 큰 능선 + 작은 굴곡
            float big = Mathf.PerlinNoise(p1 + u * 3.1f, p1);
            float small = Mathf.PerlinNoise(p2 + u * 7.3f, p2);
            int ridge = 96 + Mathf.RoundToInt(big * 52f + small * 22f);   // 96 ~ 170

            // 가장자리 12% 구간은 공통 높이(128)로 수렴 -> 어떤 타일끼리 붙어도 이음새 없음
            float edge = Mathf.Clamp01(Mathf.Min(u, 1f - u) / 0.12f);
            ridge = Mathf.RoundToInt(Mathf.Lerp(128f, ridge, edge));

            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, y <= ridge ? Color.white : Color.clear);
        }
        Random.state = backup;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
    }

    /// <summary>중경: 낮은 지면 + 바위/선인장 실루엣</summary>
    private Sprite MakeRocks(int seed)
    {
        int w = 256, h = 96;   // 16 x 6 월드
        Texture2D tex = NewTex(w, h);
        Random.State backup = Random.state;
        Random.InitState(seed);

        int groundTop = 26;
        for (int x = 0; x < w; x++)
            for (int y = 0; y <= groundTop; y++)
                tex.SetPixel(x, y, Color.white);

        // 바위: 반타원 4~6개 (타일 경계를 넘지 않게 안쪽에만 - 이음새 보호)
        int rocks = Random.Range(4, 7);
        for (int r = 0; r < rocks; r++)
        {
            int rw = Random.Range(8, 20);
            int rh = Random.Range(6, 15);
            int cx = Random.Range(rw + 2, w - rw - 2);
            for (int dx = -rw; dx <= rw; dx++)
            {
                int x = cx + dx;
                int top = groundTop + Mathf.RoundToInt(rh * Mathf.Sqrt(Mathf.Max(0f, 1f - (dx * dx) / (float)(rw * rw))));
                for (int y = groundTop; y <= top; y++) tex.SetPixel(x, y, Color.white);
            }
        }

        // 선인장: 기둥 + 팔 (역시 경계 안쪽에만)
        int cacti = Random.Range(2, 4);
        for (int c = 0; c < cacti; c++)
        {
            int cx = Random.Range(8, w - 8);
            int ch = Random.Range(20, 38);
            for (int dx = -1; dx <= 1; dx++)
                for (int y = groundTop; y <= groundTop + ch; y++)
                    tex.SetPixel(cx + dx, y, Color.white);
            // 팔: 옆으로 4px 나갔다가 위로 7px
            int armY = groundTop + Mathf.RoundToInt(ch * 0.55f);
            int dir = Random.value < 0.5f ? -1 : 1;
            for (int a = 1; a <= 4; a++) tex.SetPixel(cx + dir * (1 + a), armY, Color.white);
            for (int a = 0; a <= 7; a++) tex.SetPixel(cx + dir * 5, armY + a, Color.white);
        }

        Random.state = backup;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
    }

    /// <summary>근경: 자갈 지면 스트립 (윗선은 살짝 울퉁불퉁)</summary>
    private Sprite MakeGround(int seed)
    {
        int w = 256, h = 56;   // 16 x 3.5 월드
        Texture2D tex = NewTex(w, h);
        Random.State backup = Random.state;
        Random.InitState(seed);
        float p = Random.Range(0f, 100f);

        for (int x = 0; x < w; x++)
        {
            float u = x / (float)(w - 1);
            int top = 44 + Mathf.RoundToInt(Mathf.PerlinNoise(p + u * 5.7f, p) * 8f);

            // 가장자리는 공통 높이(48)로 수렴 -> 타일 이음새 없음
            float edge = Mathf.Clamp01(Mathf.Min(u, 1f - u) / 0.1f);
            top = Mathf.RoundToInt(Mathf.Lerp(48f, top, edge));

            for (int y = 0; y < h; y++)
            {
                if (y > top) { tex.SetPixel(x, y, Color.clear); continue; }
                // 자갈 반점: 일부 픽셀만 살짝 투명 -> 톤 얼룩
                float a = 1f;
                if (Random.value < 0.06f) a = 0.72f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        Random.state = backup;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
    }

    private Texture2D NewTex(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;   // 픽셀 아트 톤
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }
}
