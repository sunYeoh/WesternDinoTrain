using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [ParallaxBackground.cs] v4.6 (v9.35.4 2026-10-26 유저 맵 그림 4장 + 소품 24종 적용 (tools/map_art_v9354.py): 소품을 칸(32u / MapDecoGrid)에 하나씩 섞어 깐다 (MapDecoStratified - 뭉치거나 비는 데 없이), 종류는 섞은 주머니에서 (같은 소품이 안 몰린다), 납작한 소품(FLAT_DECO)은 선 소품 아래, 땅 타일을 열마다 좌우로 뒤집어 (MapGroundFlipVariants, 맵 그림일 때만 - 가장자리가 거울이라 이음은 맞는다. 상하는 안 뒤집는다 - 줄마다 다르면 세로 감김 순간 펄럭인다) 32u 반복이 덜 보이게) / v4.5 (v9.32 2026-10-22 맵 그림 자리 (그림은 유저가 만든다 - claude/맵_그림_프롬프트_2026-10-22.md): SetMap(맵) - Resources/Sprites/WDT/ground_(desert·canyon·mine·final).png 가 있으면 땅 타일마다 다음 그림을 겹쳐 MapGroundFadeSec 에 걸쳐 바꾸고 그 그림엔 지역 색을 안 입힌다 (없으면 ground_ae + 지역 색 그대로, 운행의 첫 지역은 바로, 같은 맵이면 그대로). deco_(맵)_1 ~ 6.png 가 있으면 땅 타일마다 MapDecoPerTile 개를 깔아 땅과 같이 흐른다 (같은 열은 같은 배치 - 세로로 감겨도 안 튄다). 기차가 달리는 선로(월드 y 0)와 보스전 두 번째 선로의 위아래 MapDecoClearY 에 든 소품은 매 프레임 숨긴다 - 갈림길에서 땅이 세로로 밀려도 (PaintDeco). 소품 색은 땅을 따른다. CurrentMap = 먼지 그림) / v4.4 (v9.24 2026-10-15 갈림길 칸 꺾임 - 테스터피드백3 D1~D3 + 클라이맥스 B4: 세계를 미는 기준을 두상 앞 -> 기차 가운데(주방 칸, GameBalance.ForkWorldRefX)로 옮기고 TrackYAt 으로 기차 x 밑 선로 높이를 내준다 - TrainDeck 이 칸마다 앞뒤 바퀴가 밟은 높이로 칸을 꺾는다 / ForkFollowActive = 분기점 출발부터 마지막 칸 뒷바퀴까지 평행이 될 때까지 (WaveManager 가 첫 손님을 기다린다) / 넘는 동안 배경 ForkDepartBoost 배 (34u 를 3.2u/s 로 가면 12초라서) / 칸마다 앞바퀴가 분기점을 밟으면 sfx_fork_clank + 작은 흔들림 / 칸이 직접 꺾이니 화면 기울임은 ForkRollMul(0) / SetClimaxBoost - 클라이맥스 배경 가속) / v4.3 (v9.21 2026-10-12 보스 1단계: SideRail - 디 오리지널(맞수 열차)이 달릴 두 번째 선로. rails_ae 한 줄을 본선과 같은 오프셋으로 y 자리에 깔고(본선과 같이 흐른다) 위에서 미끄러져 내려와 자리 잡는다, SideRailOff 로 흐려지며 걷는다) / v4.2 (v9.13 2026-09-23: 선로 v2 갈림길 + 세계 밀림) / v4.1 - 완전 탑다운 지면 (2026-09-07) / v3 고퀄 PNG 지면 / v2 탑뷰 지면 스크롤
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

    // ── v4.5 (v9.32): 맵 그림 (MapRoute) ──
    /// <summary>지금 땅이 그리는 맵 (MapRoute.Desert ~ Final, 0 = 아직 없음 - 사막으로 본다). DustFX 가 먼지 그림을 고른다</summary>
    public static int CurrentMap { get; private set; }
    private const int ORDER_GROUND_NEXT = -29;   // 겹쳐 들어오는 다음 맵 땅 (땅 -30 위)
    private const int ORDER_DECO = -27;          // 소품 (땅 위, 선로 -10 · 바위 -8 아래)
    private const int MAX_DECO_KINDS = 6;        // deco_(맵)_1 ~ 6
    /// <summary>v4.6: 납작한 소품 (기름 얼룩 · 낙뢰 자국 · 얼음 웅덩이 · 탄 구덩이) - 외곽선 없이 땅에 붙은 것. 선 소품(ORDER_DECO + 1) 아래에 그린다</summary>
    private static readonly string[] FLAT_DECO = { "deco_desert_6", "deco_canyon_2", "deco_mine_4", "deco_final_4" };
    private const float DECO_HALF_MAX = 1.6f;    // v4.6: 가장 큰 소품의 반폭 (쓰러진 전신주 3.06u) + 여유 - 칸 흔들기·타일 가장자리 여유가 이것을 쓴다. 더 큰 소품을 넣으면 올린다

    /// <summary>v4.6: 납작한 소품인가 (이름이 FLAT_DECO 로 시작 - 잘린 스프라이트 이름 "deco_desert_6_0" 도)</summary>
    private static bool IsFlatDeco(Sprite s)
    {
        if (s == null || string.IsNullOrEmpty(s.name)) return false;
        for (int i = 0; i < FLAT_DECO.Length; i++) if (s.name.StartsWith(FLAT_DECO[i])) return true;
        return false;
    }
    private const float DECO_EDGE = 1f;          // 선로 띠 바깥 이만큼(u) 동안 소품이 흐려지며 나타난다 (띠 경계에서 툭 끊기지 않게)
    private int appliedMap = 0;                  // 이 배경에 실제로 깔린 맵 (CurrentMap 은 정적이라 배경이 생기기 전에도 바뀐다)
    private bool groundOwnColor = false;         // 땅이 맵 그림이다 - 지역 색을 안 입힌다
    private SpriteRenderer[] groundNext;         // 땅 타일마다 겹쳐 그리는 다음 맵 그림 (겹치는 동안만)
    private DecoSet decoNow, decoNext;           // 지금 소품 / 겹쳐 들어오는 소품 (겹치기가 끝나면 서로 바꾼다)
    private float mapFadeT = -1f;                // 0 -> 1 겹쳐 바꾸는 중 (-1 = 아님)
    private Sprite pendingGround;                // 겹치기가 끝나면 타일에 넣을 그림
    private bool pendingOwnColor;
    private bool fadingGround;                   // 땅 그림도 바뀌는 중 (같은 그림이면 소품만)
    private Color fadeTint = Color.white;        // 겹쳐 들어오는 땅의 색 (마지막 프레임) - 끝날 때 타일 색을 이 값으로 바로

    /// <summary>v4.5: 소품 한 벌 - 땅 타일마다 묶음 루트(타일의 자식이라 땅과 같이 흐른다) + 깔린 소품 전부. 겹쳐 바꾸는 동안만 두 벌이 같이 보인다</summary>
    private class DecoSet
    {
        public readonly Transform[] roots;
        public readonly List<SpriteRenderer> items = new List<SpriteRenderer>();
        public float alpha = 1f;
        public DecoSet(int tileCount) { roots = new Transform[tileCount]; }
    }

    // ── v4.4 (v9.24): 갈림길 칸 꺾임 (테스터피드백3 D1~D3) ──
    private float laneU0 = 0f;                   // 고른 가지의 분기점 월드 x (지난 LateUpdate). TrackYAt 이 이번 프레임 자리를 미리 셈한다
    private bool tailSettled = false;            // 마지막 칸의 뒷바퀴까지 평행이 됐나 (= 칸 꺾임 끝, 손님이 와도 된다)
    private readonly bool[] carClanked = new bool[4];   // 칸마다 앞바퀴가 분기점을 밟았나 (덜컹 소리 1회)
    private float forkBoostCur = 1f;             // 출발 가속 배율 (넘는 동안 ForkDepartBoost, 끝나면 1 로 돌아온다)
    private static float climaxBoostTarget = 1f; // 클라이맥스 배경 가속 목표 (ClimaxDirector 가 정한다)
    private float climaxBoostCur = 1f;

    // v4.3: 두 번째 선로 (보스 디 오리지널의 평행 선로) - 본선 띠(층 2)와 같은 오프셋으로 흐른다
    private SpriteRenderer[] sideTiles;
    private float sideY = 0f, sideT = 0f, sideSec = 0.5f;   // sideT: 0 = 없음, 1 = 자리 잡음
    private bool sideOn = false;

    /// <summary>v4.3: y 자리에 두 번째 선로를 깐다 - slideSec 에 걸쳐 위(북쪽 3u)에서 미끄러져 내려오며 또렷해진다. 탑다운 띠가 없으면(레거시 배경) 아무것도 안 한다</summary>
    public static void SideRail(float y, float slideSec)
    {
        if (instance == null || !instance.topdown) return;
        instance.sideY = y; instance.sideSec = Mathf.Max(0.05f, slideSec); instance.sideOn = true;
        if (instance.sideTiles == null)
        {
            Sprite sp = SpriteBank.Get("rails_ae");
            instance.sideTiles = new SpriteRenderer[RAILS_TILES];
            for (int i = 0; i < RAILS_TILES; i++)
            {
                GameObject t = new GameObject("SideRail" + i);
                t.transform.SetParent(instance.layerRoots[2], false);
                SpriteRenderer sr = t.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingOrder = ORDER_RAILS;
                instance.sideTiles[i] = sr;
            }
            instance.sideT = 0f;
        }
        instance.LayoutSideRail();
    }

    /// <summary>v4.3: 두 번째 선로를 fadeSec 에 걸쳐 걷는다 (보스가 쓰러졌을 때)</summary>
    public static void SideRailOff(float fadeSec)
    {
        if (instance == null || instance.sideTiles == null || !instance.sideOn) return;   // 이미 걷는 중이면 처음 시간대로
        instance.sideSec = Mathf.Max(0.05f, fadeSec); instance.sideOn = false;
    }

    /// <summary>v4.3: 두 번째 선로 타일을 본선과 같은 오프셋으로 늘어놓고 진행도(sideT)만큼 내려오며 또렷해진다. 다 걷히면 지운다</summary>
    private void LayoutSideRail()
    {
        if (sideTiles == null) return;
        float stripW = tileW[2] * cols[2];
        float e = sideT * sideT * (3f - 2f * sideT);
        float y = sideY + 3f * (1f - e);
        Color c = tintNow[2]; c.a = e;
        for (int i = 0; i < sideTiles.Length; i++)
        {
            if (sideTiles[i] == null) continue;
            float x = Mathf.Repeat(i * tileW[2] + offsets[2] + stripW / 2f, stripW) - stripW / 2f;
            sideTiles[i].transform.localPosition = new Vector3(x, y, 0f);
            sideTiles[i].color = c;
        }
    }

    private void TickSideRail()
    {
        if (sideTiles == null) return;
        sideT = Mathf.MoveTowards(sideT, sideOn ? 1f : 0f, Time.deltaTime / sideSec);
        if (!sideOn && sideT <= 0f)
        {
            for (int i = 0; i < sideTiles.Length; i++) if (sideTiles[i] != null) Destroy(sideTiles[i].gameObject);
            sideTiles = null;
            return;
        }
        LayoutSideRail();
    }
    private float oldFadeT = -1f;                // 옛 띠 페이드 진행 시간 (-1 = 아직)
    private SpriteRenderer[] branchTiles;        // 고른 가지의 이어지는 띠

    /// <summary>가지 원호를 도는 동안의 화면 기울임 (도, +면 시계 방향으로 보인다). CameraZoom v5 가 읽는다</summary>
    public static float RouteRollDeg { get; private set; }

    /// <summary>
    /// v4.4 (D1): 칸이 갈림길 선로를 따라 꺾여 있을 수 있는 동안 - 가지로 출발한 뒤부터 마지막 칸의 뒷바퀴까지 평행이 될 때까지.
    /// TrainDeck 이 이 동안 칸 자세를 잡고, WaveManager 가 첫 손님을 내보내지 않고 기다린다 (D2)
    /// </summary>
    public static bool ForkFollowActive
    {
        get { return instance != null && instance.laneActive && instance.forkTf != null && !instance.tailSettled; }
    }

    /// <summary>
    /// v4.4 (D1): 기차 x 밑 선로 중심의 높이 (월드 y, 칸 꺾임이 없으면 0).
    /// TrainDeck 은 Update 에서 부르고 이 클래스는 LateUpdate 에서 띠를 옮기므로, 이번 프레임에 옮겨질 거리만큼 분기점을 미리 당겨 셈한다 (칸이 선로보다 한 프레임 늦지 않게)
    /// </summary>
    public static float TrackYAt(float x)
    {
        if (!ForkFollowActive) return 0f;
        ParallaxBackground pb = instance;
        float u0 = pb.laneU0 + CurrentSpeed * Time.deltaTime;
        float ang;
        float railsYNow = -pb.laneSign * BranchOffset(Mathf.Max(0f, u0 - WorldRefX()), out ang);
        return railsYNow + pb.laneSign * BranchOffset(Mathf.Max(0f, u0 - x), out ang);
    }

    /// <summary>v4.4: 세계를 미는 기준 x - 칸 꺾임이 켜져 있으면 기차 가운데(주방 칸), 아니면 예전대로 두상 앞 (기차는 곧게 서고 세계만 밀린다)</summary>
    private static float WorldRefX()
    {
        return GameBalance.ForkCarFollow ? GameBalance.ForkWorldRefX : GameBalance.RouteHeadFrontX;
    }

    /// <summary>v4.4 (B4): 클라이맥스 배경 가속 목표 (1 = 끔). GameBalance.ClimaxSpeedEaseSec 에 걸쳐 따라간다 - 배경 연출만 (판정·손님 속도와 무관)</summary>
    public static void SetClimaxBoost(float mul)
    {
        climaxBoostTarget = Mathf.Max(0.1f, mul);
    }

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
        if (topdown) { BuildTopdownLayers(); ApplyMap(MapRoute.Desert, true); }   // v4.5: 처음 땅은 사막 (맵 그림이 있으면 그것)
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
        ApplyMap(MapRoute.Desert, true);   // v4.5: 새 씬은 로비·견습부터 - 사막 땅 (운행이 시작되면 웨이브 1 의 지역 전환이 그 노선의 맵으로 바꾼다)
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

    // ─────────────────────────────────────────────
    // v4.5 (v9.32): 맵 그림 - 땅 겹쳐 바꾸기 + 소품
    // ─────────────────────────────────────────────
    /// <summary>땅을 맵 map 의 그림으로 (WaveManager 지역 전환 · GameManager 로비). instant = 겹치지 않고 바로</summary>
    public static void SetMap(int map, bool instant)
    {
        if (instance != null) instance.ApplyMap(map, instant);
        else CurrentMap = map;
    }

    private void ApplyMap(int map, bool instant)
    {
        CurrentMap = map;
        if (!topdown || tiles[0] == null || tiles[0].Length == 0) return;
        // 이미 그 맵이 깔려 있거나 그 맵으로 겹치는 중이면 그대로 - 같은 소품 두 벌을 겹쳐 바꾸면 한가운데서 반투명해진다
        if (!instant && map == appliedMap) return;
        EnsureMapChildren();
        FinishMapFade();   // 겹치던 것이 있으면 끝내 놓고
        appliedMap = map;

        string key = "ground_" + MapRoute.Key(map);
        bool own = SpriteBank.Has(key);
        Sprite next = own ? SpriteBank.Get(key) : SpriteBank.Get("ground_ae");
        BuildDeco(decoNext, map);
        decoNext.alpha = 0f;

        pendingGround = next; pendingOwnColor = own;
        fadingGround = next != null && tiles[0][0].sprite != next;
        bool variants = own && GameBalance.MapGroundFlipVariants;   // v4.6: 맵 그림 땅은 열마다 좌우로 뒤집어 깐다 (예전 모래엔 선로 띠가 그려져 있어 안 뒤집는다)
        for (int i = 0; i < groundNext.Length; i++)
        {
            groundNext[i].sprite = fadingGround ? next : null;
            groundNext[i].color = new Color(1f, 1f, 1f, 0f);
            groundNext[i].flipX = variants && FlipX(i);
        }
        fadeTint = own ? Color.white : tintNow[0];
        mapFadeT = 0f;
        if (instant || GameBalance.MapGroundFadeSec <= 0f) FinishMapFade();
        PaintDeco();
    }

    /// <summary>겹쳐 바꾸기 한 프레임 (실시간 - 히트스탑·정지에도 끝난다). regionTint = 지금 지역 색 (맵 그림이 없는 땅에 입힐 색)</summary>
    private void TickMapFade(Color regionTint)
    {
        mapFadeT = Mathf.Min(1f, mapFadeT + Time.unscaledDeltaTime / Mathf.Max(0.05f, GameBalance.MapGroundFadeSec));
        float a = mapFadeT;
        fadeTint = pendingOwnColor ? Color.white : regionTint;
        Color g = fadeTint; g.a = a;
        if (fadingGround)
            for (int i = 0; i < groundNext.Length; i++) groundNext[i].color = g;
        decoNext.alpha = a;          // 들어오는 소품
        decoNow.alpha = 1f - a;      // 나가는 소품
        if (mapFadeT >= 1f) FinishMapFade();
    }

    /// <summary>겹쳐 바꾸기를 끝낸다 - 타일에 새 그림, 나가던 소품은 지우고 들어오던 소품이 지금 것</summary>
    private void FinishMapFade()
    {
        if (mapFadeT < 0f || groundNext == null) return;
        mapFadeT = -1f;
        if (pendingGround != null)
            for (int i = 0; i < tiles[0].Length; i++) tiles[0][i].sprite = pendingGround;
        groundOwnColor = pendingOwnColor;
        bool variants = groundOwnColor && GameBalance.MapGroundFlipVariants;   // v4.6: 겹치던 그림과 같은 뒤집기로
        for (int i = 0; i < tiles[0].Length; i++) tiles[0][i].flipX = variants && FlipX(i);
        if (fadingGround)
        {
            // 겹친 그림과 같은 색으로 바로 (안 그러면 겹친 뒤에 땅 색이 한 번 더 바뀐다)
            tintNow[0] = fadeTint;
            for (int i = 0; i < tiles[0].Length; i++) tiles[0][i].color = fadeTint;
        }
        for (int i = 0; i < groundNext.Length; i++) { groundNext[i].sprite = null; groundNext[i].color = new Color(1f, 1f, 1f, 0f); }
        ClearDeco(decoNow);
        DecoSet swap = decoNow; decoNow = decoNext; decoNext = swap;
        decoNow.alpha = 1f;
        decoNext.alpha = 0f;
        pendingGround = null;
        fadingGround = false;
    }

    /// <summary>땅 타일마다 "다음 그림" 겹침 렌더러 + 소품 묶음 두 벌 (한 번만)</summary>
    private void EnsureMapChildren()
    {
        if (groundNext != null && groundNext.Length == tiles[0].Length) return;
        int n = tiles[0].Length;
        groundNext = new SpriteRenderer[n];
        decoNow = new DecoSet(n);
        decoNext = new DecoSet(n);
        for (int i = 0; i < n; i++)
        {
            Transform tile = tiles[0][i].transform;
            GameObject g = new GameObject("NextMap");
            g.transform.SetParent(tile, false);
            groundNext[i] = g.AddComponent<SpriteRenderer>();
            groundNext[i].sortingOrder = ORDER_GROUND_NEXT;
            groundNext[i].color = new Color(1f, 1f, 1f, 0f);
            decoNow.roots[i] = new GameObject("DecoA").transform; decoNow.roots[i].SetParent(tile, false);
            decoNext.roots[i] = new GameObject("DecoB").transform; decoNext.roots[i].SetParent(tile, false);
        }
    }

    /// <summary>맵 map 의 소품 그림 (deco_(맵)_1 ~ 6 중 있는 것)</summary>
    private static List<Sprite> DecoSprites(int map)
    {
        List<Sprite> list = new List<Sprite>();
        for (int k = 1; k <= MAX_DECO_KINDS; k++)
        {
            Sprite s = SpriteBank.Get("deco_" + MapRoute.Key(map) + "_" + k);
            if (s != null) list.Add(s);
        }
        return list;
    }

    /// <summary>
    /// 소품 한 벌(set)을 맵 map 의 것으로 깐다. 같은 열(세로 줄)은 같은 배치 - 갈림길에서 땅이 세로로 32u 감겨도 소품이 튀지 않는다. 선로 띠는 PaintDeco 가 매 프레임 비운다.
    /// v4.6: 땅 한 장을 MapDecoGrid x MapDecoGrid 칸으로 나눠 섞은 칸 중 앞 MapDecoPerTile 개에 하나씩 (칸 안에서 조금 흔든다 - 옆 칸 소품과 안 겹치는 폭) -
    ///   아무 데나 뿌리면 뭉치는 데와 빈 데가 같이 생겼다. 종류는 섞은 주머니에서 차례로 (여섯 종이 다 나온 뒤에야 같은 것이 또). 납작한 소품은 선 소품 아래
    /// </summary>
    private void BuildDeco(DecoSet set, int map)
    {
        ClearDeco(set);
        List<Sprite> sprites = DecoSprites(map);
        int per = GameBalance.MapDecoPerTile;
        if (sprites.Count == 0 || per <= 0) return;
        float tile = tileW[0];
        float half = tile * 0.5f - DECO_HALF_MAX;              // 아무 데나 뿌릴 때도 타일 밖으로 안 삐지게
        int grid = Mathf.Clamp(GameBalance.MapDecoGrid, 1, 10);   // 11 부터는 칸(2.9u)이 소품보다 좁아 겹친다
        bool strat = GameBalance.MapDecoStratified && grid >= 2;
        float cellW = tile / grid;
        float jitter = Mathf.Max(0f, cellW * 0.5f - DECO_HALF_MAX);   // 칸 가운데에서 이만큼 흔든다 - 옆 칸 소품과 중심 거리가 최소 3.2u (가장 넓은 소품 3.1u), 타일 가장자리까지 1.6u
        int cellCount = grid * grid;
        int[] cells = new int[cellCount];
        int[] bag = new int[sprites.Count];
        for (int i = 0; i < set.roots.Length; i++)
        {
            int col = i % Mathf.Max(1, cols[0]);
            System.Random rng = new System.Random(map * 7919 + col * 104729);
            for (int c = 0; c < cellCount; c++) cells[c] = c;
            Shuffle(cells, rng);
            for (int b = 0; b < bag.Length; b++) bag[b] = b;
            Shuffle(bag, rng);
            int bagAt = 0;
            int n = strat ? Mathf.Min(per, cellCount) : per;
            for (int k = 0; k < n; k++)
            {
                float x, y;
                if (strat)
                {
                    int cx = cells[k] % grid, cy = cells[k] / grid;
                    x = -tile * 0.5f + (cx + 0.5f) * cellW + (float)(rng.NextDouble() * 2.0 - 1.0) * jitter;
                    y = -tile * 0.5f + (cy + 0.5f) * cellW + (float)(rng.NextDouble() * 2.0 - 1.0) * jitter;
                }
                else
                {
                    x = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
                    y = (float)(rng.NextDouble() * 2.0 - 1.0) * half;
                }
                if (bagAt >= bag.Length) { Shuffle(bag, rng); bagAt = 0; }
                Sprite sp = sprites[bag[bagAt++]];
                GameObject go = new GameObject("Deco");
                go.transform.SetParent(set.roots[i], false);
                go.transform.localPosition = new Vector3(x, y, 0f);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingOrder = IsFlatDeco(sp) ? ORDER_DECO : ORDER_DECO + 1;
                sr.flipX = rng.Next(2) == 1;
                sr.color = new Color(1f, 1f, 1f, 0f);   // 색·알파는 PaintDeco 가 정한다
                set.items.Add(sr);
            }
        }
    }

    /// <summary>v4.6: 피셔-예이츠 섞기 (타일 열마다 같은 씨앗이라 같은 결과)</summary>
    private static void Shuffle(int[] a, System.Random rng)
    {
        for (int i = a.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            int t = a[i]; a[i] = a[j]; a[j] = t;
        }
    }

    /// <summary>
    /// v4.6: 땅 타일 i 의 좌우 뒤집기 = 열(세로 줄)마다 번갈아 (흐르는 동안 안 바뀐다 - 열은 타일의 자리다). 위아래로 붙는 타일끼리 좌우 뒤집기가 같으니 위아래 이음이 맞고,
    /// 옆으로 붙는 둘은 달라도 같은 열의 픽셀이 맞닿아 거울로 이어진다. 상하 뒤집기는 안 한다 - 줄마다 다르면 갈림길에서 땅이 세로로 32u 감기는 순간
    /// (줄 1 자리에 줄 2 가 온다) 화면의 땅이 한 번에 뒤집혀 보이고, 열마다 다르면 옆 이음이 안 맞는다 (리뷰 c)
    /// </summary>
    private bool FlipX(int i)
    {
        int nc = Mathf.Max(1, cols[0]);
        return (i % nc) % 2 == 1;
    }

    private static void ClearDeco(DecoSet set)
    {
        if (set == null) return;
        for (int i = 0; i < set.items.Count; i++) if (set.items[i] != null) Destroy(set.items[i].gameObject);
        set.items.Clear();
    }

    /// <summary>
    /// 소품 색 (매 프레임 - LateUpdate 의 땅 배치·겹치기 뒤, 그리고 맵을 바꾼 직후). 땅이 맵 그림이 아니면 땅과 같은 지역 색을 입힌다.
    /// 선로 띠: 기차가 달리는 선로는 늘 월드 y 0 이다 (갈림길에선 세계가 밀려 가지가 0 으로 온다). 땅은 groundY 만큼 밀려 있어 타일 안 자리로는 못 비우니
    /// 소품의 월드 y 가 MapDecoClearY 안이면 숨기고, 그 밖 DECO_EDGE 동안 흐려지게 한다. 보스전 두 번째 선로(SideRail)도 그 선로가 또렷해지는 만큼 같이 비운다
    /// </summary>
    private void PaintDeco()
    {
        if (decoNow == null) return;
        float sideK = 0f, sideAt = 0f;
        if (sideTiles != null)
        {
            sideK = sideT * sideT * (3f - 2f * sideT);   // LayoutSideRail 과 같은 진행 곡선
            sideAt = sideY + 3f * (1f - sideK);
        }
        PaintDecoSet(decoNow, groundOwnColor ? Color.white : tintNow[0], sideK, sideAt);
        if (mapFadeT >= 0f) PaintDecoSet(decoNext, fadeTint, sideK, sideAt);
    }

    private static void PaintDecoSet(DecoSet set, Color tint, float sideK, float sideAt)
    {
        float clear = GameBalance.MapDecoClearY;
        for (int i = 0; i < set.items.Count; i++)
        {
            SpriteRenderer sr = set.items[i];
            if (sr == null) continue;
            float y = sr.transform.position.y;
            float k = Mathf.Clamp01((Mathf.Abs(y) - clear) / DECO_EDGE);
            if (sideK > 0f) k = Mathf.Min(k, Mathf.Lerp(1f, Mathf.Clamp01((Mathf.Abs(y - sideAt) - clear) / DECO_EDGE), sideK));
            Color c = tint; c.a = set.alpha * k;
            sr.color = c;
            sr.enabled = c.a > 0.004f;   // 숨은 소품은 안 그린다
        }
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
        // v4.4: 출발 가속(가지로 넘는 동안) x 클라이맥스 가속 - 둘 다 배경 연출만 (덜컹임·먼지가 따라 잦아진다. 판정·손님 속도와 무관)
        bool forkBoosting = GameBalance.ForkCarFollow && laneActive && laneSign != 0 && forkTf != null && !tailSettled;
        forkBoostCur = Mathf.MoveTowards(forkBoostCur, forkBoosting ? GameBalance.ForkDepartBoost : 1f,
            Time.deltaTime / Mathf.Max(0.05f, GameBalance.ForkBoostEaseSec));
        climaxBoostCur = Mathf.MoveTowards(climaxBoostCur, climaxBoostTarget, Time.deltaTime / Mathf.Max(0.05f, GameBalance.ClimaxSpeedEaseSec));
        float boost = Mathf.Min(GameBalance.BgBoostCap, forkBoostCur * climaxBoostCur);
        CurrentSpeed = BASE_SPEED * speedFactor * externalMul * boost;

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
        TickSideRail();   // v4.3: 두 번째 선로 (보스전)

        // 3) 지역 색: 카메라 배경색을 곱 틴트로 은은하게 (지역 전환 시 자동으로 부드럽게). 층 2 는 옛 띠 페이드 알파를 곱한다
        Color bg = cam.backgroundColor;
        Color want = Color.Lerp(Color.white, Color.Lerp(bg, Color.white, 0.5f), REGION_TINT);
        for (int L = 0; L < 3; L++)
        {
            Color wantL = (L == 0 && groundOwnColor) ? Color.white : want;   // v4.5: 맵 그림 땅은 제 색 그대로
            tintNow[L] = Color.Lerp(tintNow[L], wantL, TINT_LERP * Time.unscaledDeltaTime);
            Color c = tintNow[L];
            if (L == 2) c.a = railsAlpha;
            for (int i = 0; i < tiles[L].Length; i++)
                tiles[L][i].color = c;
        }
        if (branchTiles != null)
            for (int i = 0; i < branchTiles.Length; i++)
                if (branchTiles[i] != null) branchTiles[i].color = tintNow[2];
        if (forkSr != null) forkSr.color = tintNow[2];
        if (mapFadeT >= 0f) TickMapFade(want);   // v4.5: 맵 그림 겹쳐 바꾸기
        PaintDeco();                             // v4.5: 소품 색·선로 띠 (땅을 다 옮긴 뒤)

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
        // v4.4: 칸 꺾임 상태 초기화 - 분기점 자리는 지금 갈림길 자리에서 (첫 TickFork 전에 TrainDeck 이 읽어도 맞게)
        instance.tailSettled = false;
        for (int c = 0; c < instance.carClanked.Length; c++) instance.carClanked[c] = false;
        instance.laneU0 = instance.forkTf.localPosition.x - (sign < 0 ? GameBalance.RouteStagger : 0f);
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
        tailSettled = false;   // v4.4
        for (int c = 0; c < carClanked.Length; c++) carClanked[c] = false;
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

        // 세계 밀림: 기준점(v4.4: 칸 꺾임이면 기차 가운데, 아니면 두상 앞)이 (고른 가지의) 분기점을 지난 거리 d -> f(d)
        if (laneActive)
        {
            float u0 = fx - (laneSign < 0 ? GameBalance.RouteStagger : 0f);
            laneU0 = u0;
            float d = u0 - WorldRefX();
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
            // v4.4: 칸이 직접 꺾이면 화면 기울임은 ForkRollMul 배 (기본 0 - 같은 말을 두 번 하지 않게. 기울임은 칸을 못 돌리던 때의 대체였다)
            float rollMul = GameBalance.ForkCarFollow ? GameBalance.ForkRollMul : 1f;
            RouteRollDeg = th > 0f ? laneSign * GameBalance.RouteShiftRollDeg * rollMul * (ang / th) : 0f;

            if (!laneEntered && u0 - GameBalance.RouteHeadFrontX > 0f)   // v4.4: 기적은 두상 앞이 분기점을 지날 때 (기준점이 옮겨져도)
            {
                laneEntered = true;   // 분기점 진입: 기적 + 덜컹 (v4.4: 칸 꺾임이면 기적만 - 칸마다 덜컹이 곧 따라온다)
                SoundManager.Play("sfx_train_whistle");
                if (!GameBalance.ForkCarFollow) GameFeel.Shake(0.12f);
            }
            if (!laneSettled && d >= BranchTotal())
            {
                laneSettled = true;   // 평행이 됐다: 덜컹 한 번 더 (v4.4: 칸 꺾임이면 없음 - 칸마다 덜컹이 맡는다)
                if (!GameBalance.ForkCarFollow) GameFeel.Shake(0.08f);
                RouteRollDeg = 0f;
            }

            // v4.4 (D3): 칸마다 앞바퀴가 분기점을 밟을 때 쇠 덜컹 두 번 + 바퀴 긁힘 (소리 하나에 들어 있다) + 작은 흔들림. 앞 칸부터 차례로
            if (GameBalance.ForkCarFollow)
            {
                for (int c = 0; c < carClanked.Length; c++)
                {
                    if (carClanked[c] || u0 < GameBalance.CarBogieX(c, true)) continue;
                    carClanked[c] = true;
                    SoundManager.Play("sfx_fork_clank", c == 0 ? 1f : 0.85f, -1f);
                    GameFeel.Shake(GameBalance.ForkClankShake);
                }
                // 마지막 칸의 뒷바퀴가 원호를 다 지나면 칸 꺾임 끝 (손님이 와도 된다)
                if (!tailSettled && u0 - GameBalance.CarBogieX(carClanked.Length - 1, false) >= BranchTotal())
                {
                    tailSettled = true;
                    Debug.Log("[ParallaxBackground] 꼬리까지 넘었다 - 칸 꺾임 끝");
                }
            }
            else tailSettled = laneSettled;

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
            if (railsAlpha <= 0f && (tailSettled || !laneActive)) RebaseToBranch(forkLeft);   // v4.4: 칸이 아직 꺾여 있으면 다 넘을 때까지 기다린다
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
