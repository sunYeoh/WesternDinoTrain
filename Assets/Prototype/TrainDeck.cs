using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [TrainDeck.cs] v4 - 고퀄 스프라이트 PNG 적용 (목업 v7d 컨펌 2026-09-03) / v3 탑뷰 재스킨 (2026-09-02)
///
/// - v4: Resources/Sprites/WDT/ 의 PNG(car0/car1/car2/head/tail/chimney)를 SpriteBank로 읽어 쓴다.
///   PNG가 없으면 v3 코드 도트(PixelPainter)로 자동 폴백. 꼬리(tail)는 PNG가 있을 때만 붙는다.
///   좌표·정렬은 v3 그대로 (피벗은 Editor/WDTSpriteImporter.cs가 임포트 시 맞춘다).
///
/// 기차 4칸(기관차/주방/포탑A/포탑B)을 코드 생성 도트 그림으로 그린다.
/// v2까지는 사각형 몇 개였고, v3부터는 목업 v2의 "위에서 본 기차" 문법을 그대로 옮겼다:
///   - 칸 = 지붕(구리 5톤 램프 + 북쪽 하이라이트 + 판금 이음새 + 리벳) + 남벽 얇게(2.5D) + 발밑 그림자 타원
///   - 무쇠 코너 플레이트(검은 포인트), 주방칸은 천장 개방(목재 바닥 판자)
///   - 기관차 앞 = T-Rex 두상(탑뷰): 돌출 눈망울 2개 + 왼쪽 테이퍼 주둥이 + 쐐기 아가리 + 지그재그 이빨
///     + 콧구멍 2쌍 + 강철 눈썹 장갑 + 정수리 리지 + 목 관절 밴드 + 등줄기 다이아 가시
///   - 굴뚝(무쇠 실린더)은 두개골과 분리 배치, 연결부는 무쇠 박스
/// 좌표계는 v2 그대로 (CarEdgesX / 몸체 y -1.8~1.8). 바뀐 건 "그리는 문법"뿐이라
/// TrainManager/슬롯/조리대/이벤트 앵커에 영향 없음. 픽셀 도구 = PixelPainter.cs (신규).
///
/// 유지 기능 (v2): 구 기차 스프라이트 자동 숨김(HideLegacyTrainVisual) / 조리대 자동 정렬(AlignStations)
/// / 씬 리로드마다 재정렬. GetWhiteSprite/GetCircleSprite는 다른 파일(AttackVFX/DeckLoot)이 쓰므로 유지.
///
/// 사용법: 없음! 파일만 넣으면 자동 생성된다. (PixelPainter.cs가 같이 있어야 한다)
/// 아트 반영 시: 칸별 스프라이트를 씬에 놓고 HideLegacyTrainVisual=false + 이 파일의 Build()를 비우면 된다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class TrainDeck : MonoBehaviour
{
    private static TrainDeck instance;

    // 정렬 순서: 패럴랙스(-30~-10)보다 앞, 셰프/적(0+)보다 뒤
    private const int SORT_BODY = -6;
    private const int SORT_TRIM = -5;
    private const int SORT_DETAIL = -4;

    /// <summary>도트 배율: 월드 1유닛 = 20px (목업 480x270 도트 캔버스와 같은 밀도)</summary>
    public const float PPU = 20f;

    // 칸 캔버스 세로 구성 (px, 위에서 아래로): 지붕 0~57 / 남벽 58~71 / 그림자 ~84
    private const int CAR_H = 92;
    private const int ROOF_BOTTOM = 57;
    private const int WALL_BOTTOM = 71;
    private const int PIVOT_Y = 36;          // 월드 y=0 에 해당하는 행 (1.8 * 20)

    private static Sprite whiteSprite;   // 1x1 (다른 파일 공용)
    private static Sprite circleSprite;  // 원 (다른 파일 공용)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("TrainDeck");
        DontDestroyOnLoad(go);
        go.AddComponent<TrainDeck>();
        // 씬 리로드(런 재시작)마다 구 비주얼 숨김/조리대를 다시 정렬
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (instance != null) instance.AlignLegacyVisuals();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        Build();
        AlignLegacyVisuals();
    }

    // ─────────────────────────────────────────────
    // v2: 씬 구 오브젝트를 4칸 체계에 맞춰 정렬
    // ─────────────────────────────────────────────
    private void AlignLegacyVisuals()
    {
        // 1) 구 기차 스프라이트 숨김 - 4칸 데크가 기차 본체를 이어받는다
        //    (렌더러만 끈다. TrainManager/태그/적 타겟팅 로직은 전부 그대로)
        if (GameBalance.HideLegacyTrainVisual)
        {
            GameObject trainObj = GameObject.FindGameObjectWithTag("Train");
            if (trainObj != null)
            {
                SpriteRenderer[] srs = trainObj.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < srs.Length; i++) srs[i].enabled = false;
                if (srs.Length > 0)
                    Debug.Log("[TrainDeck] 구 기차 스프라이트 " + srs.Length + "개 숨김 - 4칸 데크로 대체");
            }

            // 구 HUD 잔재도 함께 정리: StatChangeText는 v2부터 미사용 (단순 텍스트 라벨 하나)
            GameObject statLegacy = GameObject.Find("StatChangeText");
            if (statLegacy != null)
            {
                statLegacy.SetActive(false);
                Debug.Log("[TrainDeck] 구 상태 StatChangeText 숨김 (미사용 - 삭제해도 무방)");
            }

            // HUD 정리: 좌상단 HP바가 너무 작게 보이던 문제 - 코드로 키운다 (씬 작업 0)
            GameObject hpBar = GameObject.Find("HPBar");
            if (hpBar != null)
            {
                RectTransform barRt = hpBar.GetComponent<RectTransform>();
                if (barRt != null)
                {
                    barRt.sizeDelta = new Vector2(340f, 26f);            // 250x20 -> 340x26
                    barRt.anchoredPosition = new Vector2(14f, -14f);     // 가장자리 여백
                }
            }
            GameObject hpTextGo = GameObject.Find("HPText");
            if (hpTextGo != null)
            {
                RectTransform txtRt = hpTextGo.GetComponent<RectTransform>();
                if (txtRt != null)
                {
                    txtRt.sizeDelta = new Vector2(130f, 26f);
                    txtRt.anchoredPosition = new Vector2(364f, -14f);    // 커진 바 오른쪽에
                }
                TMPro.TextMeshProUGUI tmp = hpTextGo.GetComponent<TMPro.TextMeshProUGUI>();
                if (tmp != null) tmp.fontSize = 20f;
            }
        }

        // 2) 조리대 3대를 주방칸 안 정위치로 (그릴/볶음팬/냄비 = StationXs 순서)
        //    B-2.2: 스케일도 통일 (씬 0.4는 너무 작았음 - StationScale이 단일소스)
        if (GameBalance.AlignStations)
        {
            CookingStation[] stations = FindObjectsByType<CookingStation>(FindObjectsSortMode.None);
            for (int i = 0; i < stations.Length; i++)
            {
                int idx = (int)stations[i].stationType;   // 0=그릴 1=볶음팬 2=냄비
                if (idx < 0 || idx >= GameBalance.StationXs.Length) continue;
                stations[i].transform.position =
                    new Vector3(GameBalance.StationXs[idx], GameBalance.StationY, 0f);
                stations[i].transform.localScale = Vector3.one * GameBalance.StationScale;
            }
            if (stations.Length > 0)
                Debug.Log("[TrainDeck] 조리대 " + stations.Length + "대 주방칸 정렬 완료");
        }
    }

    // ─────────────────────────────────────────────
    // 데크 생성 (v3: 도트 스프라이트)
    // ─────────────────────────────────────────────
    private void Build()
    {
        float[] edges = GameBalance.CarEdgesX;

        for (int car = 0; car < edges.Length - 1; car++)
        {
            // 칸 몸체 (경계에서 0.12씩 들여쓰기 - 칸 사이 틈이 보이게)
            float left = edges[car] + 0.12f;
            float right = edges[car + 1] - 0.12f;
            int w = Mathf.RoundToInt((right - left) * PPU);

            // v4: PNG 우선 (칸 3은 칸 2와 같은 포탑칸 그림), 없으면 코드 도트
            Sprite carSprite = SpriteBank.Get(car == 3 ? "car2" : "car" + car);
            if (carSprite == null) carSprite = PaintCar(w, car);
            PixelPainter.Attach(transform, "Car" + car + "_Body", carSprite,
                new Vector3((left + right) * 0.5f, 0f, 0f), SORT_BODY);

            // 연결부 (다음 칸과의 틈) - 무쇠 박스 + 사선 하이라이트
            if (car < edges.Length - 2)
                PixelPainter.Attach(transform, "Coupler" + car, PaintCoupler(),
                    new Vector3(edges[car + 1], -0.85f, 0f), SORT_TRIM);
        }

        // 기관차 히어로 피스: T-Rex 두상 (칸 0 앞쪽에 겹쳐 앉는다) + 굴뚝
        float locoLeft = edges[0];
        Sprite headSprite = SpriteBank.Get("head");
        if (headSprite == null) headSprite = PaintHead();
        PixelPainter.Attach(transform, "TRexHead", headSprite, new Vector3(locoLeft, 0f, 0f), SORT_DETAIL);
        Sprite chimneySprite = SpriteBank.Get("chimney");
        if (chimneySprite == null) chimneySprite = PaintChimney();
        PixelPainter.Attach(transform, "Chimney", chimneySprite, new Vector3(locoLeft + 2.55f, -0.9f, 0f), SORT_DETAIL);

        // v4: 꼬리 (마지막 칸 뒤, PNG가 있을 때만 - 기차 전체가 공룡으로 읽히는 포인트)
        Sprite tailSprite = SpriteBank.Get("tail");
        if (tailSprite != null)
            PixelPainter.Attach(transform, "TRexTail", tailSprite,
                new Vector3(edges[edges.Length - 1] - 0.1f, 0f, 0f), SORT_TRIM);

        Debug.Log("[TrainDeck] 4칸 데크 생성 완료 - 탑뷰 v3 (경계 " + edges[0] + " ~ " + edges[edges.Length - 1] + ")");
    }

    // ─────────────────────────────────────────────
    // 칸 1개 (kind: 0=기관차 1=주방(개방) 2,3=포탑칸)
    // 메카 문법: 빨강 장갑 지붕 + 금 트림 프레임 + 판넬 분할선 / 검정 섀시 남벽 + 회색 바퀴가드 + 금 스트라이프
    // ─────────────────────────────────────────────
    private static Sprite PaintCar(int w, int kind)
    {
        PixelPainter p = new PixelPainter(w, CAR_H);
        int r = w - 1;

        // 발밑 그림자 (남벽 아래로 살짝 삐져나온다 - 2.5D 단서)
        p.Shadow(1, 62, r + 1, 84);

        // 검정 섀시가 지붕보다 살짝 넓게 깔린다 (완구의 하부 프레임)
        p.Rect(0, 2, r, WALL_BOTTOM + 1, PixelPainter.BLK);
        p.RectOutline(0, 2, r, WALL_BOTTOM + 1, PixelPainter.BLK_O);

        // 지붕 장갑 (빨강) + 금 트림 프레임 + 북쪽 하이라이트
        p.RoundRect(2, 0, r - 2, ROOF_BOTTOM - 2, 4, PixelPainter.RED, PixelPainter.RED_O);
        p.Rect(3, 1, r - 3, 4, PixelPainter.RED_L);
        p.RoundRect(4, 3, r - 4, ROOF_BOTTOM - 5, 3, PixelPainter.CLEAR, PixelPainter.GOLD);
        p.Rect(6, ROOF_BOTTOM - 10, r - 6, ROOF_BOTTOM - 9, PixelPainter.SILVER);   // 은색 액센트 줄 (전대물 흰 스트라이프)
        p.Line(5, ROOF_BOTTOM - 4, r - 5, ROOF_BOTTOM - 4, PixelPainter.GOLD_D, 1);

        // 남벽 = 검정 섀시 + 금 스트라이프 + 회색 바퀴가드 2개 + 통풍구
        p.Rect(1, ROOF_BOTTOM - 1, r - 1, WALL_BOTTOM, PixelPainter.BLK);
        p.Rect(1, ROOF_BOTTOM + 1, r - 1, ROOF_BOTTOM + 2, PixelPainter.GOLD);
        p.Rect(1, ROOF_BOTTOM + 3, r - 1, ROOF_BOTTOM + 3, PixelPainter.GOLD_D);
        int[] gx = { 6, r - 18 };
        for (int i = 0; i < 2; i++)
        {
            p.RoundRect(gx[i], ROOF_BOTTOM + 5, gx[i] + 12, WALL_BOTTOM - 1, 2, PixelPainter.GREY, PixelPainter.BLK_O);
            p.Line(gx[i] + 1, ROOF_BOTTOM + 6, gx[i] + 11, ROOF_BOTTOM + 6, PixelPainter.GREY_L, 1);
            for (int vx = gx[i] + 3; vx <= gx[i] + 9; vx += 3) p.Line(vx, ROOF_BOTTOM + 8, vx, WALL_BOTTOM - 3, PixelPainter.BLK, 1);
        }
        for (int vx = 24; vx < r - 22; vx += 4) p.Line(vx, ROOF_BOTTOM + 7, vx, WALL_BOTTOM - 3, PixelPainter.BLK_L, 1);   // 통풍구 슬릿

        if (kind == 1)
        {
            // 주방칸: 천장 개방 - 검정 체크 플레이트 바닥 + 금 난간 테두리 + 남쪽 안쪽 그늘
            p.Rect(8, 6, r - 8, ROOF_BOTTOM - 8, PixelPainter.BLK_L);
            p.RectOutline(8, 6, r - 8, ROOF_BOTTOM - 8, PixelPainter.GOLD);
            p.RectOutline(7, 5, r - 7, ROOF_BOTTOM - 7, PixelPainter.RED_O);
            for (int py = 9; py < ROOF_BOTTOM - 10; py += 4)
                for (int px = 10; px < r - 9; px += 4)
                    if (((px + py) / 4) % 2 == 0) p.Rect(px, py, px + 1, py + 1, PixelPainter.BLK);
            p.Rect(9, ROOF_BOTTOM - 14, r - 9, ROOF_BOTTOM - 9, PixelPainter.BLK);
        }
        else if (kind == 0)
        {
            // 기관차: 보일러 등판 - 금 센터 스트라이프 + 판넬 분할 + 흡기 슬릿
            p.Rect(10, 26, r - 6, 30, PixelPainter.GOLD);
            p.Line(10, 30, r - 6, 30, PixelPainter.GOLD_D, 1); p.Line(11, 26, r - 7, 26, PixelPainter.GOLD_L, 1);
            for (int px = 30; px < r - 6; px += 12)
            {
                p.Line(px, 5, px, ROOF_BOTTOM - 6, PixelPainter.RED_D, 1);
                p.Rect(px + 3, 9, px + 8, 12, PixelPainter.BLK); p.Rect(px + 3, 44, px + 8, 47, PixelPainter.BLK);
            }
        }
        else
        {
            // 포탑칸: 넓은 판넬 분할선 + 금 볼트 + 가로 보강대 (금)
            for (int px = 12; px < r - 8; px += 20)
            {
                p.Line(px, 5, px, ROOF_BOTTOM - 6, PixelPainter.RED_D, 1);
                p.Rivet(px - 1, 9, PixelPainter.GOLD_D, PixelPainter.GOLD_L); p.Rivet(px - 1, 46, PixelPainter.GOLD_D, PixelPainter.GOLD_L);
            }
            p.Rect(6, 25, r - 6, 29, PixelPainter.GOLD);
            p.Line(6, 29, r - 6, 29, PixelPainter.GOLD_D, 1); p.Line(7, 25, r - 7, 25, PixelPainter.GOLD_L, 1);
        }

        // 검정 코너 장갑 4개 (완구의 모서리 블록)
        int[] cx = { 2, r - 7, 2, r - 7 };
        int[] cy = { 0, 0, ROOF_BOTTOM - 7, ROOF_BOTTOM - 7 };
        for (int i = 0; i < 4; i++)
        {
            p.RoundRect(cx[i], cy[i], cx[i] + 5, cy[i] + 5, 2, PixelPainter.BLK, PixelPainter.BLK_O);
            p.Point(cx[i] + 2, cy[i] + 2, PixelPainter.BLK_L);
        }

        return p.Bake(PPU, w * 0.5f, PIVOT_Y);
    }

    /// <summary>연결부: 검정 박스 + 금 핀 (칸 사이 틈, 남벽 높이)</summary>
    private static Sprite PaintCoupler()
    {
        PixelPainter p = new PixelPainter(12, 12);
        p.Rect(0, 0, 11, 11, PixelPainter.BLK);
        p.RectOutline(0, 0, 11, 11, PixelPainter.BLK_O);
        p.Rect(4, 3, 7, 8, PixelPainter.GOLD); p.Point(4, 3, PixelPainter.GOLD_L);
        return p.Bake(PPU);
    }

    /// <summary>굴뚝: 검정 실린더 + 금 림 + 회색 받침 (메카 배기통)</summary>
    private static Sprite PaintChimney()
    {
        PixelPainter p = new PixelPainter(24, 32);
        p.Rect(3, 9, 21, 25, PixelPainter.BLK);
        p.Line(3, 9, 3, 25, PixelPainter.BLK_O, 1); p.Line(21, 9, 21, 25, PixelPainter.BLK_O, 1);
        p.Line(5, 10, 5, 24, PixelPainter.BLK_L, 1);                                  // 몸통 하이라이트
        p.Ellipse(1, 21, 23, 31, PixelPainter.GREY, PixelPainter.BLK_O);               // 받침 플랜지
        p.Ellipse(1, 1, 23, 13, PixelPainter.GOLD, PixelPainter.GOLD_D);               // 상단 금 림
        p.Ellipse(5, 4, 19, 11, PixelPainter.BLK_O, PixelPainter.CLEAR);               // 구멍
        return p.Bake(PPU);
    }

    // ─────────────────────────────────────────────
    // 전대물 메가조드 T-Rex 두상 (탑뷰) v5 - 소년만화 히어로 메카 톤 (2026-09-02 사용자 피드백: v3 빌런 / v4 유아 사이)
    //   각진 후드(파셋) + 각진 발광 눈(코어 밝음) + 이마 금 V 크레스트 + 금 마우스플레이트(통풍 슬릿, 턱 안 가름)
    //   + 은색 액센트 판넬 + 회색 턱 범퍼 + 후방 금 핀. 캔버스 90x76, 피벗 (30,28)
    // ─────────────────────────────────────────────
    private static Sprite PaintHead()
    {
        PixelPainter p = new PixelPainter(90, 76);
        Color32 eyeO = new Color32(10, 60, 30, 255), eye = new Color32(60, 220, 110, 255), eyeC = new Color32(200, 255, 210, 255);

        p.Shadow(4, 54, 80, 74);                                                      // 머리 그림자

        // 1) 검정 하부 섀시 (후드보다 3px 크게, 각진) + 회색 범퍼 블레이드 (앞 좌우, 앞으로 뻗음)
        p.Polygon(new int[] { 76, 3, 76, 53, 30, 57, 10, 49, 2, 36, 2, 20, 10, 7, 30, -1 }, PixelPainter.BLK, PixelPainter.BLK_O);
        p.Polygon(new int[] { 0, 13, 9, 9, 13, 15, 3, 19 }, PixelPainter.GREY, PixelPainter.BLK_O);
        p.Polygon(new int[] { 0, 43, 3, 37, 13, 41, 9, 47 }, PixelPainter.GREY, PixelPainter.BLK_O);
        p.Line(2, 13, 8, 10, PixelPainter.GREY_L, 1); p.Line(2, 43, 8, 46, PixelPainter.GREY_L, 1);

        // 목 관절 (검정 밴드 + 금 볼트) + 후방 금 핀 2개 (남북으로 뻗은 각진 날개)
        p.Rect(68, 12, 74, 44, PixelPainter.BLK_L);
        p.Line(68, 12, 68, 44, PixelPainter.BLK_O, 1); p.Line(74, 12, 74, 44, PixelPainter.BLK_O, 1);
        p.Rivet(71, 20, PixelPainter.GOLD_D, PixelPainter.GOLD_L); p.Rivet(71, 38, PixelPainter.GOLD_D, PixelPainter.GOLD_L);
        p.Polygon(new int[] { 56, 4, 66, 0, 70, 4, 60, 8 }, PixelPainter.GOLD, PixelPainter.GOLD_D);
        p.Polygon(new int[] { 56, 52, 60, 48, 70, 52, 66, 56 }, PixelPainter.GOLD, PixelPainter.GOLD_D);
        p.Line(58, 4, 65, 1, PixelPainter.GOLD_L, 1); p.Line(58, 52, 65, 55, PixelPainter.GOLD_L, 1);

        // 2) 빨강 후드 (파셋 8각, 앞으로 테이퍼) + 은색 측면 액센트 판넬 + 금 트림 + 하이라이트
        int[] hood = { 70, 6, 70, 50, 30, 54, 12, 46, 5, 34, 5, 22, 12, 10, 30, 2 };
        p.Polygon(hood, PixelPainter.RED, PixelPainter.RED_O);
        p.Polygon(new int[] { 44, 4, 66, 8, 66, 14, 44, 12 }, PixelPainter.SILVER, PixelPainter.GREY);     // 은 판넬(북)
        p.Polygon(new int[] { 44, 44, 66, 42, 66, 48, 44, 52 }, PixelPainter.SILVER, PixelPainter.GREY);   // 은 판넬(남)
        p.Line(45, 5, 65, 9, PixelPainter.WHITE, 1); p.Line(45, 51, 65, 47, PixelPainter.WHITE, 1);
        int[] trim = { 67, 9, 67, 47, 30, 51, 14, 44, 8, 33, 8, 23, 14, 12, 30, 5 };
        p.Polygon(trim, PixelPainter.CLEAR, PixelPainter.GOLD);
        p.Line(31, 4, 43, 5, PixelPainter.RED_L, 1); p.Line(14, 12, 29, 4, PixelPainter.RED_L, 1);
        p.Line(48, 15, 48, 41, PixelPainter.RED_D, 1);                                 // 파셋 분할선

        // 3) 이마 금 V 크레스트 (센터라인, 앞을 가리킴) + 금 센터 리지
        p.Polygon(new int[] { 42, 20, 60, 13, 60, 16, 46, 23, 46, 33, 60, 40, 60, 43, 42, 36, 39, 28 }, PixelPainter.GOLD, PixelPainter.GOLD_D);
        p.Line(43, 20, 58, 15, PixelPainter.GOLD_L, 1);
        p.Rect(14, 26, 40, 30, PixelPainter.GOLD); p.Line(14, 26, 40, 26, PixelPainter.GOLD_L, 1); p.Line(14, 30, 40, 30, PixelPainter.GOLD_D, 1);

        // 4) 각진 발광 눈 2개 (트라페조이드, 앞이 뾰족) - 외곽 어두운 초록 + 본체 + 안쪽 밝은 코어 + 검정 눈썹 장갑
        p.Polygon(new int[] { 18, 15, 38, 10, 38, 20, 22, 21 }, eye, eyeO);
        p.Polygon(new int[] { 22, 15, 34, 12, 34, 17, 24, 18 }, eyeC, PixelPainter.CLEAR);
        p.Polygon(new int[] { 18, 41, 22, 35, 38, 36, 38, 46 }, eye, eyeO);
        p.Polygon(new int[] { 22, 41, 24, 38, 34, 39, 34, 44 }, eyeC, PixelPainter.CLEAR);
        p.Polygon(new int[] { 16, 13, 40, 7, 40, 10, 18, 15 }, PixelPainter.BLK, PixelPainter.BLK_O);     // 눈썹 장갑(북) - 날카롭게
        p.Polygon(new int[] { 16, 43, 18, 41, 40, 46, 40, 49 }, PixelPainter.BLK, PixelPainter.BLK_O);    // 눈썹 장갑(남)

        // 5) 앞면 금 마우스플레이트 (턱을 가르지 않는다) + 통풍 슬릿 3 + 은 송곳니 2 (플레이트 가장자리에 살짝)
        p.Polygon(new int[] { 6, 21, 18, 20, 20, 28, 18, 36, 6, 35 }, PixelPainter.GOLD, PixelPainter.GOLD_D);
        p.Line(7, 22, 17, 21, PixelPainter.GOLD_L, 1);
        for (int sy = 25; sy <= 31; sy += 3) p.Line(8, sy, 16, sy, PixelPainter.GOLD_D, 1);
        p.Polygon(new int[] { 6, 20, 8, 17, 10, 20 }, PixelPainter.SILVER, PixelPainter.GREY);
        p.Polygon(new int[] { 6, 36, 8, 39, 10, 36 }, PixelPainter.SILVER, PixelPainter.GREY);
        p.Rect(11, 15, 13, 17, PixelPainter.GOLD_L); p.Rect(11, 39, 13, 41, PixelPainter.GOLD_L);      // 헤드램프

        return p.Bake(PPU, 30f, 28f);
    }

    // ─────────────────────────────────────────────
    // 공용 스프라이트 (다른 파일이 쓴다 - 유지)
    // ─────────────────────────────────────────────
    /// <summary>1x1 흰 스프라이트 (스케일로 사각형을 만든다)</summary>
    public static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return whiteSprite;
    }

    /// <summary>지름 1 유닛 흰 원 스프라이트 (팝/빔 마커용)</summary>
    public static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        int s = 32;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        float r = s * 0.5f - 0.5f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = x - r, dy = y - r;
                bool inside = dx * dx + dy * dy <= r * r;
                tex.SetPixel(x, y, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        return circleSprite;
    }
}
