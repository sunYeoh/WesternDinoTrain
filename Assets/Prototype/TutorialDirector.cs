using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [TutorialDirector.cs] v2.7 (v9.27 2026-10-20 유저 "5초면 끝나는 단계는 묶고, 새 기능 튜토리얼을 더하고, 바뀐 조작은 고쳐라": 8구간 13카드 - 구간 1 기본(옛 1·2 묶음 + 옛 3·4 묶음 + 굽기 + Lv) / 2 요리 심화(새 - 팬·솥 조리법 둘, 진화 조리, 교체, 지원 요리) / 3 멈춘 포탑 살리기(감전 + 과열) / 4 주방 사고(새 - 기구 고장·화재, KitchenEventManager.ForceEvent) / 5 기관실(옛 작살·레버 묶음) / 6 실전(+ 단골 주문 OrderSystem.TutorialOrder) / 7 정산 / 8 보스 예습(+ 무방비 [F] 독침 육포, 처치까지). 옛 구간 기록은 MigrateV3 가 새 번호로 옮긴다 (새 구간 2·4 는 "아직"). 인라인 연습(정식 운행 첫 낙뢰·바위·레버)은 그대로이고 기록만 구간과 따로(WDT_Tut_In2·3·4) - 묶인 구간이 통째로 완료로 찍히지 않게. 한 카드 안에서 할 일이 넘어가면 SetLines 로 본문만 바꾼다. 리뷰 반영: 조리 카드는 FoodStock.OnCooked 횟수로 판정(재고 아님) / 사고 카드는 취소·거부·미니게임을 기다린다 / 전설이 폐기되면 다시 준다 / 못 일으키거나 포기한 카드는 구간 기록을 "건너뜀"으로 - 훈련장에서 다시, 인라인 연습도 막히지 않게 / 인라인 연습 인자는 kind) / v2.6 (v9.22.2 2026-10-14 유저 "튜토리얼 말투가 번역체·AI 같고 딱딱하다": 안내 글 말투를 반말 해체로 - 명령형 ~라·설명 ~다 끝맺음, 문장 속 기호, 어려운 말(투입·재가동·곁·유인)를 걷어냈다. 규칙 = 문구원칙 10-14 저녁 "말투" 절) / v2.5 (v9.22.1 2026-10-14 유저 "튜토리얼에 드래그가 반영 안 됐다": 2·5·6단계 목표 글과 지연 힌트를 v9.22 조작(카드 끌어다 놓기 - 교체 / 재료 중심 조리 창) 기준으로. 클릭 ▶ 클릭도 된다고 덧붙임. 5단계에 20초 지연 힌트(고기 카드 두 번 누르기) 추가) / v2.4 (v9.21.2 2026-10-14 문구 팩: 용어 통일 - 접시->요리(개) / 데미지->피해 / 슬롯->칸 / 랜덤->무작위 / 레시피->요리 / 적·아군->손님 / 드랍->드롭 / 마법 저항->저항 / 힐러·러시·청구 / 진화 조리 판 이름) / v2.3 (v9.20 2026-10-07: 글 속 화살표를 게임 글꼴에 있는 세모로(게임 글꼴에 화살표 글리프가 없어 PC 의 다른 글꼴로 그려졌다 - 모양이 따로 놀고, 대신할 글꼴이 없는 빌드에선 빈칸)) / v2.2 (v9.18 2026-10-06: 목표 카드의 아래 끝·왼쪽 끝을 밖에서 읽게(CardBottomY·CardLeftFromRight - 알림 줄과 예고 카드가 카드를 피한다) / [Enter] 건너뛰기 오작동 - 요리 창·카드가 닫히는 그 프레임의 Enter 가 단계 건너뛰기로 새던 것(직전 프레임에 창이 떠 있었으면 무시) / 4단계 고기: 손님이 재료를 반드시 떨어뜨리고(Enemy), 그래도 모자라면 10초 뒤 채운다(구 30초)) / v2.1 (v9.15 2026-09-29 2차 피드백 "훈련장 무한 반복·맨땅 연습": 구간 완료 카드 [R] 한 번 더(PendingSegment - 씬 리로드 뒤 자동 시작) / 자유 연습 Begin(SANDBOX=8) - 손님이 계속 오고(SandboxSpawn*) 재료가 안 떨어지고 기차가 안 죽는다, [1] 낙뢰 [2] 화재 [3] 흘림 [4] 고장 [5] 침입 을 직접 일으킨다, 나가기 = [ESC] 메뉴 그만두기. SandboxActive 동안 포탑 파손도 진짜로 일어난다) / v2 (v9.12 2026-09-22: 견습 운행 구간화 - 7구간(S1 이동과 첫 포탑 1~6 / S2 감전된 포탑 복구 7 / S3 작살로 재료 얻기 8 / S4 전속 주행 켜고 끄기 9 /
///   S5 요리하며 기차 지키기 10 / S6 증강 선택과 정비 11 / S7 미끼로 첫 보스 상대하기 12(신규)) + 마지막 앞길 카드 13. Begin(segment) 로 한 구간만 (훈련장 TrainingGroundUI),
///   구간마다 PlayerPrefs WDT_Tut_S1..S7 = 1 완료 / 2 건너뜀. 정식 운행 인라인 연습 PlayInline(seg) - 새 기믹 첫 등장 순간(협곡 낙뢰 / 첫 바위 / 레버 웨이브) 손님·스폰·사고·포탑을 멈추고(InlineFreeze)
///   셰프만 움직여 그 행동을 해낸다. 30초 = 힌트 추가(자동 통과 없음), [Enter] = 건너뛰기(기록 2). 큰 카드 540x250 = 연습·예습, 목표 카드 330x156 은 그대로 (목업 v4.2).
///   새끼 발톱 = BossEnemy.practice (보상·카드·베팅 체인 없음), 완료 = 미끼 유인 2회. 처음 실행은 로비 [출발] 도 견습부터 (GameBalance.TutorialForceFirst))
///   / v1.3 (v9.11.1 문구) / v1.2 (v9.9.2: 단계 7~12) / v1.1 (v9.9.1) / v1 (v9.9 2026-09-16) - "견습 운행": 로비 [T]로 들어가는 전용 튜토리얼 런
///
/// 계획: claude/다음팩_설계메모_2026-09-21.md B·C (목업 WDT_튜토리얼목업v4.2_구간화.png 컨펌 09-22). 문구 규칙: claude/문구원칙_2026-09-22.md
/// 링 규칙 (RingStyle): 포탑 = 큰 링, 받침 정렬(-3) 뒤 z / 작살·레버 = 정렬 -4, z -0.01 / 조리대 = 작은 링, 정렬 0 / 바닥 목표 = 작은 링, 정렬 -3.
/// 8단계(구간 3)부터 EngineCabUnlocked = true: EngineCab 이 TutorialGateActive 를 무시하고 바위·작살·레버를 켠다.
///
/// 구조
///  - 정식 씬 없음. 로비에서 Begin(segment) -> GameManager.StartTutorial() 이 Battle 상태로 보내되, 웨이브/보급/메타 기록은 이 디렉터가 맡는다.
///  - 단계 = 브리핑 카드(BriefingUI, 시간 정지) -> 목표 카드(우상단 웨이브 판 아래) + 현장 마커(월드 화살표/링) -> 코드 상태로 판정.
///  - [Enter] = 이 단계 건너뛰기 (브리핑이 떠 있으면 브리핑이 먼저 먹는다 - BriefingUI.KeyConsumedFrame). 건너뛴 단계가 있는 구간은 "건너뜀"(2) 으로 적힌다.
///  - [ESC] 일시정지 메뉴의 "견습 운행 그만두기" -> Quit() (완료 기록 없이 로비).
///  - 전부 돌린 런 완료 -> 요약 카드 -> WDT_TutorialDone=1 (+ WDT_PrologueSeen=1) -> 명성 +30 (최초 1회) -> 로비. 한 구간 런 완료 -> 구간 카드 -> 로비.
///  - 무적: GameBalance.TutorialGodMode 면 실전(10)·예습(12) 전까지 기차 HP 를 매 프레임 가득 채운다.
///  - 방해 이벤트·바위·작살·레버는 BlockAmbient(기본 true) 동안 WaveManager.TutorialGateActive 를 통해 막힌다.
///  - 인라인 연습: Active 는 false 인 채 InlineActive/InlineFreeze 만 켠다. Enemy/BossEnemy/TurretSlot.TickFire/Bullet/KitchenEventManager/WaveManager 가 InlineFreeze 를 보고 쉰다.
///    (Time.timeScale 은 1 - 셰프·조리·[E] 는 그대로 움직인다)
///  - 씬이 다시 로드되면(런 포기 등) 조용히 정리 (정적 플래그 잔존 방지).
///
/// 사용법: 파일만 넣으면 자동 생성. LobbyUI/TrainingGroundUI 가 Begin(segment) 를, WaveManager/EngineCab 이 PlayInline(seg) 를 부른다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class TutorialDirector : MonoBehaviour
{
    public static TutorialDirector Instance { get; private set; }

    /// <summary>견습 운행(전용 런) 진행 중인가 (다른 시스템 가드용: GameManager/WaveManager/치트/힌트)</summary>
    public static bool Active { get; private set; }

    /// <summary>v2: 정식 운행 안에서 인라인 연습 진행 중 (카드·마커·Enter 건너뛰기가 산다)</summary>
    public static bool InlineActive { get; private set; }

    /// <summary>v2.1: 자유 연습(훈련장 8) 진행 중. Active 도 같이 true. 포탑 파손·사고 강제 발생이 여기서만 열린다</summary>
    public static bool SandboxActive { get; private set; }

    /// <summary>v2.1: 구간 완료 카드 [R] - 씬이 다시 뜨면 이 구간을 자동으로 시작한다 (-1 = 없음)</summary>
    public static int PendingSegment = -1;

    /// <summary>v2.1: 훈련장 마지막 줄 = 자유 연습 (v2.7: 8 -> 9)</summary>
    public const int SANDBOX = 9;

    /// <summary>v2.7: 구간 4(주방 사고) 동안 - KitchenEventManager 가 견습 중에도 디렉터가 일으킨 사고만 돌린다 (무작위 사고는 없음)</summary>
    public static bool ForcedEventsOnly = false;

    /// <summary>v2.7: 첫 보스 예습(구간 8)을 끝냈나 (BriefingTexts 보스 카드의 미끼 줄)</summary>
    public static bool BossPracticeDone { get { return SegmentState(8) == 1; } }

    /// <summary>v2: 인라인 연습 동안 손님·보스·스폰·사고 타이머·포탑 사격·탄이 쉰다 (셰프·조리·[E] 는 그대로)</summary>
    public static bool InlineFreeze { get; private set; }

    /// <summary>견습 운행을 한 번이라도 끝까지(전부) 마쳤는가</summary>
    public static bool Done
    {
        get { return PlayerPrefs.GetInt(DONE_KEY, 0) == 1; }
    }

    /// <summary>true 면 방해 이벤트가 막힌다 (WaveManager.TutorialGateActive 가 읽는다). 견습 내내 true</summary>
    public static bool BlockAmbient = true;

    /// <summary>8단계(구간 3)부터 true - EngineCab 이 게이트를 무시하고 바위·작살·레버를 켠다</summary>
    public static bool EngineCabUnlocked = false;

    public const string DONE_KEY = "WDT_TutorialDone";
    public const string SEG_KEY_PREFIX = "WDT_Tut_S";
    public const int SEGMENTS = 8;             // v2.7: 7 -> 8 (요리 심화 · 주방 사고가 새로)
    public const int TOTAL_STEPS = 13;          // 카드 수 (구간 1 = 4, 2 = 2, 3 ~ 8 = 1 씩, + 앞길 카드)

    private const int CARD_SORT = 690;           // 조리 미니게임(30)·증강(600)·배너(610) 위, 일시정지(700) 아래
    private const int ARROW_ORDER = 7;           // 셰프(6) 위
    private const string STARTER_RECIPE = "meat+meat";   // 더블 육포 (GameBalance.StarterFoods[0] 와 같은 것)

    // ─────────────────────────────────────────────
    // 구간 표 (훈련장이 읽는다) - 제목 = 배울 행동 (검토 보고서 §5)
    // ─────────────────────────────────────────────
    public struct SegmentInfo
    {
        public int no;
        public string title;      // 배울 행동
        public string line;       // 한 줄
        public string stepLabel;  // "목표 1~4"
        public string estimate;   // "예상 3분" (실측 전 값)
        public int firstStep, lastStep;
        public SegmentInfo(int no, string title, string line, string stepLabel, string estimate, int firstStep, int lastStep)
        { this.no = no; this.title = title; this.line = line; this.stepLabel = stepLabel; this.estimate = estimate; this.firstStep = firstStep; this.lastStep = lastStep; }
    }

    public static readonly SegmentInfo[] SEGMENT_TABLE = new SegmentInfo[] {
        new SegmentInfo(1, "기본 - 달리기, 포탑, 요리",  "달려가서 카드 넣고, 손님 보고, 굽고, Lv 올리기",    "목표 1~4", "예상 3분",      1, 4),
        new SegmentInfo(2, "요리 심화 - 팬, 솥, 진화", "볶기와 끓이기, 진화 조리로 전설 요리, 포탑 교체",   "목표 5~6", "예상 3분",      5, 6),
        new SegmentInfo(3, "멈춘 포탑 살리기",        "감전은 [E] 한 번, 과열은 [E] 꾹 + 마우스",          "목표 7",   "예상 1분",      7, 7),
        new SegmentInfo(4, "주방 사고 막기",          "기구 고장과 화재 - 현장으로 달려가서 처리",        "목표 8",   "예상 1분 30초", 8, 8),
        new SegmentInfo(5, "기관실 - 작살과 레버",    "창밖 바위 낚기, 레버로 전속 주행",                "목표 9",   "예상 1분",      9, 9),
        new SegmentInfo(6, "요리하며 기차 지키기",    "몰려오는 손님 막기 + 단골 주문. 기차가 다쳐",      "목표 10",  "예상 1분 30초", 10, 10),
        new SegmentInfo(7, "증강 고르고 정비하기",    "증강 카드 고르고 정차역에서 [G] 정비소 열기",      "목표 11",  "예상 40초",     11, 11),
        new SegmentInfo(8, "첫 보스 예습",            "미끼로 무리 꾀어내고 [F] 던져서 새끼 발톱 잡기",   "목표 12",  "예상 1분 30초", 12, 12),
    };

    /// <summary>구간 기록: 0 = 아직 / 1 = 완료 / 2 = 건너뜀(단계를 건너뛰었거나 인라인 연습을 건너뜀)</summary>
    public static int SegmentState(int seg)
    {
        return PlayerPrefs.GetInt(SEG_KEY_PREFIX + seg, 0);
    }

    public static bool SegDone(int seg) { return SegmentState(seg) == 1; }

    /// <summary>완료(1)로 적힌 구간 수</summary>
    public static int CompletedCount()
    {
        int n = 0;
        for (int i = 1; i <= SEGMENTS; i++) if (SegmentState(i) == 1) n++;
        return n;
    }

    /// <summary>구간 기록이 하나라도 있는가 (처음 실행 판정)</summary>
    public static bool HasAnyRecord()
    {
        for (int i = 1; i <= SEGMENTS; i++) if (SegmentState(i) != 0) return true;
        return false;
    }

    /// <summary>처음 실행: 로비 [출발] 도 견습부터 (GameBalance.TutorialForceFirst)</summary>
    public static bool MustPlayFirst
    {
        get { return GameBalance.TutorialForceFirst && GameBalance.TutorialRunEnabled && !Done && !HasAnyRecord(); }
    }

    /// <summary>치트 F4: 구간 기록 전부 삭제 (TutorialHint 가 DONE_KEY 와 함께 부른다)</summary>
    public static void ResetSegments()
    {
        for (int i = 1; i <= SEGMENTS; i++) PlayerPrefs.DeleteKey(SEG_KEY_PREFIX + i);
        for (int k = 2; k <= 4; k++) PlayerPrefs.DeleteKey(INLINE_KEY_PREFIX + k);   // v2.7
        PlayerPrefs.Save();
    }

    /// <summary>구간 기록 쓰기 - 이미 완료(1)면 건너뜀(2)으로 내리지 않는다</summary>
    private static void MarkSegment(int seg, int state)
    {
        if (seg < 1 || seg > SEGMENTS) return;
        int cur = SegmentState(seg);
        if (cur == 1 && state == 2) return;
        PlayerPrefs.SetInt(SEG_KEY_PREFIX + seg, state);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] 구간 " + seg + " 기록 = " + (state == 1 ? "완료" : "건너뜀"));
    }

    /// <summary>v9.11 이전에 견습을 끝낸 저장: 구간 기록이 없으면 옛 1~6 을 완료로 (그 뒤 MigrateV3 가 새 번호로 옮긴다)</summary>
    private static void MigrateOldDone()
    {
        if (PlayerPrefs.GetInt(VER_KEY, 0) >= VER) return;   // 새 번호 체계에선 할 일 없음
        if (!Done || HasAnyRecord()) return;
        for (int i = 1; i <= 6; i++) PlayerPrefs.SetInt(SEG_KEY_PREFIX + i, 1);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] 옛 완료 기록 -> 구간 1~6 완료로 옮김");
    }

    private const string VER_KEY = "WDT_Tut_Ver";
    private const int VER = 3;                 // v2.7 (v9.27) 번호 체계
    private const string INLINE_KEY_PREFIX = "WDT_Tut_In";   // v2.7: 인라인 연습 기록 (2 낙뢰 / 3 바위 / 4 레버) - 구간 기록과 따로
    private static bool newSegmentsNotice = false;           // v2.7: 기록을 옮겼다 - 로비에서 한 번 "새 구간이 생겼어"

    /// <summary>
    /// v2.7: 옛 7구간 기록 -> 새 8구간 번호로. 옛 1 -> 1 / 옛 2(감전) -> 3(감전+과열, 값 그대로) / 옛 3·4(작살·레버) -> 5(둘 다 완료면 완료, 하나라도 있으면 건너뜀) /
    /// 옛 5 -> 6 / 옛 6 -> 7 / 옛 7 -> 8. 새 구간 2(요리 심화)·4(주방 사고)는 "아직". 옛 2·3·4 를 했으면 인라인 연습 기록도 같이 (정식 운행에서 또 뜨지 않게).
    /// 한 번만 (WDT_Tut_Ver). 기록이 하나도 없는 새 저장은 번호만 올린다
    /// </summary>
    private static void MigrateV3()
    {
        if (PlayerPrefs.GetInt(VER_KEY, 0) >= VER) return;
        int[] old = new int[8];
        bool any = false;
        for (int i = 1; i <= 7; i++) { old[i] = PlayerPrefs.GetInt(SEG_KEY_PREFIX + i, 0); if (old[i] != 0) any = true; }
        if (any)
        {
            int both34 = (old[3] == 1 && old[4] == 1) ? 1 : (old[3] != 0 || old[4] != 0) ? 2 : 0;
            int[] fresh = { 0, old[1], 0, old[2], 0, both34, old[5], old[6], old[7] };
            for (int i = 1; i <= SEGMENTS; i++)
            {
                if (fresh[i] != 0) PlayerPrefs.SetInt(SEG_KEY_PREFIX + i, fresh[i]);
                else PlayerPrefs.DeleteKey(SEG_KEY_PREFIX + i);
            }
            if (old[2] == 1) PlayerPrefs.SetInt(INLINE_KEY_PREFIX + 2, 1);   // 끝낸 것만 - 건너뛴 사람은 정식 운행에서 연습을 받는다 (새 저장과 같게)
            if (old[3] == 1) PlayerPrefs.SetInt(INLINE_KEY_PREFIX + 3, 1);
            if (old[4] == 1) PlayerPrefs.SetInt(INLINE_KEY_PREFIX + 4, 1);
            newSegmentsNotice = true;
            Debug.Log("[Tutorial] 구간 기록을 새 번호로 옮김: " + old[1] + old[2] + old[3] + old[4] + old[5] + old[6] + old[7] + " -> "
                + fresh[1] + fresh[2] + fresh[3] + fresh[4] + fresh[5] + fresh[6] + fresh[7] + fresh[8] + " (새 구간 2·4 는 아직)");
        }
        PlayerPrefs.SetInt(VER_KEY, VER);
        PlayerPrefs.Save();
    }

    /// <summary>v2.7: 인라인 연습 기록 (kind 2 낙뢰 / 3 바위 / 4 레버): 0 아직 / 1 완료 / 2 건너뜀</summary>
    public static int InlineState(int kind) { return PlayerPrefs.GetInt(INLINE_KEY_PREFIX + kind, 0); }

    /// <summary>인라인 연습 종류가 든 견습 구간 (그 구간을 견습에서 끝냈으면 인라인 연습은 안 뜬다)</summary>
    private static int InlineSegmentOf(int kind) { return kind == 2 ? 3 : 5; }

    // ─────────────────────────────────────────────
    // 링 스타일
    // ─────────────────────────────────────────────
    private struct RingStyle
    {
        public bool large;      // tut_ring_l(72x26) / tut_ring(36x14)
        public float dy;        // 목표점 기준 y 오프셋 (오브젝트 발 자리)
        public int order;       // sortingOrder
        public float z;         // 같은 정렬값 안에서 앞뒤 (z+ 가 먼저 그려진다 = 뒤)
        public float scale;
        public RingStyle(bool large, float dy, int order, float z, float scale)
        { this.large = large; this.dy = dy; this.order = order; this.z = z; this.scale = scale; }
    }
    private static readonly RingStyle RING_TURRET = new RingStyle(true, -0.6f, -3, 0.05f, 1.0f);    // 받침(-3) 뒤
    private static readonly RingStyle RING_HARPOON = new RingStyle(true, -0.45f, -4, -0.01f, 1.3f); // 작살(-4, z -0.02) 뒤, 두상(-4, z 0) 앞
    private static readonly RingStyle RING_LEVER = new RingStyle(false, -0.5f, -4, -0.01f, 1.2f);   // 레버 기둥(-4, z -0.02) 뒤
    private static readonly RingStyle RING_STATION = new RingStyle(false, 0f, 0, -0.01f, 1.0f);      // 조리대 그림자(0) 위, 조리대(1) 아래
    private static readonly RingStyle RING_FLOOR = new RingStyle(false, 0f, -3, 0.05f, 1.0f);       // 바닥 목표 (갑판 위)

    // ── 진행 상태 ──
    private int step = 0;
    private int runSegment = 0;                  // 0 = 전부, 1~8 = 그 구간만
    private readonly List<int> runSteps = new List<int>();   // 이번 런의 단계 번호 목록 (카드 "목표 n / N")
    private int curSeg = 0;
    private int segSkips = 0;                    // 현재 구간에서 건너뛴 단계 수
    private float runStartTime;                  // unscaled
    private float stepStartTime;                 // scaled (조리/투입 시각 비교용)
    private float stepStartUnscaled;             // 단계별 걸린 시간 로그
    private readonly float[] stepSeconds = new float[TOTAL_STEPS + 1];
    private int skips = 0;
    private int badsAtStart = 0;
    private int retries10 = 0;                   // 실전 카드 재시작 횟수 (로그)
    private int retries12 = 0;                   // 예습 재시작 횟수
    private bool skipRequested = false;
    private bool windowOpenLastFrame = false;   // v2.2: 직전 프레임에 다른 창이 떠 있었나 ([Enter] 건너뛰기 가드)
    private bool godMode = false;
    private bool trainStoppedFlag = false;
    private Coroutine runRoutine;
    private Coroutine inlineRoutine;

    // ── 목표 카드 UI ──
    private Canvas cardCanvas;
    private GameObject cardRoot;
    private RectTransform cardRect;
    private RectTransform stepPlate;             // 스킨 명판
    private Text stepFallback;                   // 스킨 없을 때
    private Text titleText;
    private Text linesText;
    private Text progressText;
    private Text footLeftText;
    private Text footRightText;
    private readonly List<Image> pips = new List<Image>();
    private Sprite pipSprite;
    private bool cardBig = false;
    private static readonly Color PIP_ON = new Color(0.886f, 0.698f, 0.227f, 1f);
    private static readonly Color PIP_OFF = new Color(0.55f, 0.47f, 0.35f, 1f);
    private static readonly Color HINT_GOLD = new Color(1f, 0.9f, 0.3f, 1f);

    // ── "멈춤" 명판 (인라인 연습 - 손님 머리 위) ──
    private readonly List<RectTransform> freezePlates = new List<RectTransform>();
    private RectTransform canvasRect;
    private float freezeScanAt = 0f;
    private Enemy[] frozenEnemies = new Enemy[0];

    // ── 현장 마커 (월드) ──
    private GameObject markerRoot;
    private Transform ringTf, arrowTf;
    private SpriteRenderer ringSr;
    private Transform followTarget;
    private Vector3 fixedTarget;
    private float arrowTipOffset = 1.0f;
    private RingStyle ringStyle = RING_FLOOR;
    private bool markerOn = false;
    private Sprite arrowSprite, ringSprite, ringLargeSprite;

    // ── 참조 ──
    private Transform chefTf;

    // ─────────────────────────────────────────────
    // 부트스트랩
    // ─────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("TutorialDirector");
        DontDestroyOnLoad(go);
        go.AddComponent<TutorialDirector>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Active = false;          // 씬 리로드 대비 - 정적 플래그는 여기서 항상 내린다
        InlineActive = false; InlineFreeze = false;
        BlockAmbient = true;
        EngineCabUnlocked = false;
        SceneManager.sceneLoaded += OnSceneLoaded;
        FoodStock.OnCooked += OnCookedForTutorial;   // v2.7: 카드 3·5 의 "만들었다" 판정
        MigrateOldDone();
        MigrateV3();   // v2.7
        BuildCard();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        FoodStock.OnCooked -= OnCookedForTutorial;
        if (Instance == this) { Instance = null; Active = false; InlineActive = false; InlineFreeze = false; SandboxActive = false; }
    }

    /// <summary>씬이 다시 로드되면 진행 중이던 견습 운행·인라인 연습은 조용히 끝난다 (런 포기/재시작/EndTutorial 전부)</summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (runRoutine != null) { StopCoroutine(runRoutine); runRoutine = null; }
        if (inlineRoutine != null) { StopCoroutine(inlineRoutine); inlineRoutine = null; }
        Active = false;
        InlineActive = false; InlineFreeze = false;
        SandboxActive = false;
        BlockAmbient = true;
        EngineCabUnlocked = false;
        ForcedEventsOnly = false;
        godMode = false;
        markerRoot = null; ringTf = null; arrowTf = null; ringSr = null; markerOn = false;   // 씬 오브젝트였으므로 이미 사라졌다
        chefTf = null;
        frozenEnemies = new Enemy[0];
        HideFreezePlates();
        if (cardRoot != null) cardRoot.SetActive(false);

        // v2.1: [R] 한 번 더 - 로비가 서면 같은 구간을 바로 시작
        if (PendingSegment > 0) StartCoroutine(AutoBeginAfterReload(PendingSegment));
        PendingSegment = -1;
    }

    /// <summary>v2.1: 씬 리로드 뒤 GameManager 가 로비 상태가 되기를 기다렸다가 Begin(seg)</summary>
    private IEnumerator AutoBeginAfterReload(int seg)
    {
        float until = Time.unscaledTime + 5f;
        while (Time.unscaledTime < until)
        {
            yield return null;
            if (GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Lobby && !Active) break;
        }
        yield return null; yield return null;   // 로비 UI 가 서도록 두 프레임
        if (!Active && GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Lobby)
        {
            Debug.Log("[Tutorial] [R] 한 번 더 - 구간 " + seg + " 자동 시작");
            Begin(seg);
        }
    }

    // ─────────────────────────────────────────────
    // 진입 / 종료 (정적 API)
    // ─────────────────────────────────────────────
    /// <summary>로비에서: 견습 운행 전부 (1~8 + 앞길 카드)</summary>
    public static void Begin() { Begin(0); }

    /// <summary>로비에서: segment = 0 전부 / 1~8 그 구간만 (훈련장)</summary>
    public static void Begin(int segment)
    {
        if (Instance == null) Bootstrap();
        if (Instance == null || Active || InlineActive) return;
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Lobby) return;
        if (!GameBalance.TutorialRunEnabled) return;
        if (segment < 0 || segment > SANDBOX) segment = 0;

        Active = true;
        SandboxActive = segment == SANDBOX;
        BlockAmbient = true;
        EngineCabUnlocked = false;
        Instance.runSegment = segment;
        BriefingUI.ClearQueue();
        GameManager.Instance.StartTutorial();          // Battle 상태로 (웨이브 시작 없음)
        Instance.runRoutine = Instance.StartCoroutine(segment == SANDBOX ? Instance.RunSandbox() : Instance.Run());
        Debug.Log("[Tutorial] 견습 운행 시작 (구간 " + (segment == 0 ? "전부" : segment == SANDBOX ? "자유 연습" : segment.ToString()) + ")");
    }

    /// <summary>일시정지 메뉴 "견습 운행 그만두기": 완료 기록 없이 로비로</summary>
    public static void Quit()
    {
        if (Instance == null || !Active) return;
        Debug.Log("[Tutorial] 그만두기 - 단계 " + Instance.step + " 에서 (기록 없음)");
        Instance.Teardown();
        if (GameManager.Instance != null) GameManager.Instance.EndTutorial();
    }

    /// <summary>GameManager.OnTrainDestroyed: 견습 운행 중 기차가 멈추면 게임오버 대신 이쪽 (실전·예습 단계가 재시작 처리)</summary>
    public void OnTrainStopped()
    {
        trainStoppedFlag = true;
    }

    private void Teardown()
    {
        if (runRoutine != null) { StopCoroutine(runRoutine); runRoutine = null; }
        Active = false;
        SandboxActive = false;
        BlockAmbient = true;
        EngineCabUnlocked = false;
        ForcedEventsOnly = false;
        OrderSystem.TutorialEnd();
        godMode = false;
        HideMarker();
        if (cardRoot != null) cardRoot.SetActive(false);
        BriefingUI.CloseAll();
        Time.timeScale = 1f;
    }

    // ─────────────────────────────────────────────
    // 매 프레임: 건너뛰기 입력 / 마커 까딱 / 무적 / HUD 글자
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (newSegmentsNotice && !Active && GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Lobby && UIManager.Instance != null)
        {
            newSegmentsNotice = false;   // v2.7: 기록을 옮긴 뒤 처음 로비에서 한 번
            UIManager.Instance.ShowStatChange("[견습] 새 구간이 생겼어 - 요리 심화, 주방 사고. [T] 훈련장");
        }
        if (!Active && !InlineActive) return;

        // [Enter] = 단계(연습) 건너뛰기. 브리핑 카드가 같은 프레임에 Enter 를 먹었으면 무시
        // v2.2: 다른 창이 "직전 프레임까지" 떠 있었어도 무시한다. 요리 창에서 [Enter] 로 조리를 시작하면 그 프레임에 창이 닫히는데,
        //   창 쪽 Update 가 먼저 돌면 여기서는 이미 닫힌 걸로 보여 같은 Enter 가 단계 건너뛰기가 됐다 (안 했는데 단계가 넘어가던 것)
        bool windowOpen = BriefingUI.IsOpen || PauseMenu.IsOpen || CookingMinigame.IsActive || AugmentListUI.ReadingOpen
            || WorkshopUI.IsOpen || AugmentPickUI.IsOpen || KitchenPanel.IsOpenStatic;   // 다른 창이 시간을 잡고 있을 때는 안 넘어간다
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            && !windowOpen && !windowOpenLastFrame && BriefingUI.KeyConsumedFrame != Time.frameCount)
            skipRequested = true;
        windowOpenLastFrame = windowOpen;

        TickMarker();
        if (InlineFreeze) TickFreezePlates();
    }

    private void LateUpdate()
    {
        if (!Active && !InlineActive) return;

        UIManager um = UIManager.Instance;
        if (Active)
        {
            // 무적: 실전·예습 단계 전까지 기차는 다치지 않는다 (HP 를 채워 두는 방식 - TrainManager 무수정)
            if (godMode && GameBalance.TutorialGodMode && TrainManager.Instance != null)
                TrainManager.Instance.currentHP = TrainManager.Instance.currentMaxHP;

            // 우상단 판 글자: 웨이브 대신 "견습 운행", 상태 "견습 중" (UIManager.Update 가 매 프레임 덮어쓰므로 LateUpdate 에서 다시)
            if (um != null)
            {
                if (um.waveText != null) um.waveText.text = runSegment == 0 ? "견습 운행" : runSegment == SANDBOX ? "자유 연습" : "훈련장  구간 " + runSegment;
                if (um.stateText != null) um.stateText.text = runSegment == SANDBOX ? "연습 중  ([ESC] 메뉴에서 나가기)" : "견습 중  " + StepPos(step) + " / " + runSteps.Count;
            }
        }
        else if (um != null && um.stateText != null)
            um.stateText.text = "전투 중  (연습 - 손님 멈춤)";
    }

    /// <summary>이번 런에서 단계 n 이 몇 번째인가 (카드 "목표 n / N")</summary>
    private int StepPos(int n)
    {
        int i = runSteps.IndexOf(n);
        return i < 0 ? Mathf.Max(1, n) : i + 1;
    }

    // ─────────────────────────────────────────────
    // 런 본체 (코루틴 상태기계) - 구간 단위
    // ─────────────────────────────────────────────
    private bool RunsSeg(int seg) { return runSegment == 0 || runSegment == seg; }

    private IEnumerator Run()
    {
        yield return null;                                   // Battle 전환이 끝나도록 한 프레임
        runStartTime = Time.unscaledTime;
        skips = 0;
        retries10 = 0; retries12 = 0;
        badsAtStart = CookingBridge.BadsThisRun;
        godMode = true;
        trainStoppedFlag = false;
        ForcedEventsOnly = false;                            // v2.7: 카드 8 이 켜고 내린다 - 혹시 남았으면 여기서도
        for (int i = 0; i < stepSeconds.Length; i++) stepSeconds[i] = 0f;

        // 이번 런의 단계 목록
        runSteps.Clear();
        for (int s = 1; s <= SEGMENTS; s++)
            if (RunsSeg(s))
                for (int n = SEGMENT_TABLE[s - 1].firstStep; n <= SEGMENT_TABLE[s - 1].lastStep; n++) runSteps.Add(n);
        if (runSegment == 0) runSteps.Add(13);

        SetupKit(runSegment);

        if (RunsSeg(1))
        {
            BeginSegment(1);
            yield return Step1_MoveInsert();
            yield return Step2_GuestsMaterials();
            yield return Step3_Grill();
            yield return Step4_LevelUp();
            EndSegment(1);
        }
        if (RunsSeg(2))
        {
            BeginSegment(2);
            yield return Step5_PanPot();
            yield return Step6_FuseReplace();
            EndSegment(2);
        }
        if (RunsSeg(3)) { BeginSegment(3); yield return Step7_StunOverheat(); EndSegment(3); }
        if (RunsSeg(4)) { BeginSegment(4); yield return Step8_KitchenEvents(); EndSegment(4); }
        if (RunsSeg(5)) { BeginSegment(5); yield return Step9_EngineCab(); EndSegment(5); }
        if (RunsSeg(6)) { BeginSegment(6); yield return Step10_Defense(); EndSegment(6); }
        if (RunsSeg(7)) { BeginSegment(7); yield return Step11_Settlement(); EndSegment(7); }
        if (RunsSeg(8)) { BeginSegment(8); yield return Step12_BossPractice(); EndSegment(8); }
        if (runSegment == 0) yield return Step13_Cards();

        yield return Finish();
    }

    /// <summary>구간 시작: 예고 띠 한 줄 ("다음 구간" 카드 대신 - 클릭을 늘리지 않는다)</summary>
    private void BeginSegment(int seg)
    {
        curSeg = seg; segSkips = 0;
        SegmentInfo info = SEGMENT_TABLE[seg - 1];
        if (runSegment == 0 && seg > 1)
            UIManager.Instance?.ShowWaveNotice("[구간 " + seg + " / " + SEGMENTS + "]  " + info.title, info.line);
        Debug.Log("[Tutorial] 구간 " + seg + " 시작: " + info.title);
    }

    private void EndSegment(int seg)
    {
        MarkSegment(seg, segSkips > 0 ? 2 : 1);
        curSeg = 0;
    }

    /// <summary>
    /// 시작 상태 (구간별): 슬롯 0 = 더블 육포 프리셋(선대가 걸어둔 포탑) 은 항상. 구간 3·6·8 은 슬롯 1 도 (감전시킬 포탑 / 화력).
    /// 보급 요리 1개는 구간 1(카드 1 투입용)·2(진화 조리 재료)·6. 재료: 구간 2 전기알·장갑 2 (카드 5 가 또 채운다), 6 고기 2, 8 고기 BossPracticeMeat + 독침 육포 1.
    /// 구간 5 는 기관실을 바로 연다 (v2.7: 번호 = 새 구간 표)
    /// </summary>
    private void SetupKit(int seg)
    {
        TurretSlot first = SlotAt(0);
        if (first != null && first.IsEmpty && !first.isLocked) first.TryInsertFood(STARTER_RECIPE);
        if (seg == 6 || seg == 8)
        {
            if (first != null && !first.IsEmpty) first.TryInsertFood(STARTER_RECIPE);   // Lv2
            TurretSlot second = SlotAt(1);
            if (second != null && second.IsEmpty && !second.isLocked) second.TryInsertFood(STARTER_RECIPE);
        }
        else if (seg == 3)
        {
            TurretSlot second = SlotAt(1);
            if (second != null && second.IsEmpty && !second.isLocked) second.TryInsertFood(STARTER_RECIPE);
        }
        if ((seg == 0 || seg == 1 || seg == 2 || seg == 6) && FoodStock.Instance != null && FoodStock.Instance.Get(STARTER_RECIPE) < 1)
            FoodStock.Instance.Add(STARTER_RECIPE, 1);
        if (seg == 2) { TopUpPair(MaterialType.Elec, MaterialType.Elec); TopUpPair(MaterialType.Armor, MaterialType.Armor); }
        if (seg == 6 && MaterialInventory.Instance != null && MeatCount() < 2) MaterialInventory.Instance.Add(MaterialType.Meat, 2 - MeatCount());
        if (seg == 8)
        {
            if (MaterialInventory.Instance != null && MeatCount() < GameBalance.BossPracticeMeat)
                MaterialInventory.Instance.Add(MaterialType.Meat, GameBalance.BossPracticeMeat - MeatCount());
            if (FoodStock.Instance != null && FoodStock.Instance.Get(THROW_RECIPE) < 1) FoodStock.Instance.Add(THROW_RECIPE, 1);
        }
        if (seg == 5) EngineCabUnlocked = true;
        UIManager.Instance?.ShowStatChange(seg == 0 || seg == 1 ? "[견습 운행] 더블 육포 1개가 보급으로 왔다" : "[훈련장] 구간 " + seg + " - " + SEGMENT_TABLE[seg - 1].title);
    }

    // ── 1. 이동 + 보급 요리 넣기 (v2.7: 옛 1·2 묶음 - 끌어다 놓기는 어디서든 되지만, 달리기를 먼저 익힌다) ──
    private IEnumerator Step1_MoveInsert()
    {
        TurretSlot slot = SlotAt(1);
        BeginStep(1, "포탑 칸으로 가서 카드 넣기", "[WASD] 이동, [Shift] 대시\n칸 사이는 통로 발판으로만 건너가", "할 일", 0, 2);
        float carL = GameBalance.CarEdgesX[2] + 0.12f, carR = GameBalance.CarEdgesX[3] - 0.12f;
        Vector3 target = new Vector3((carL + carR) * 0.5f, 0.3f, 0f);   // 포탑 칸 A 바닥 가운데 (약 4.75, 0.3)
        ShowMarkerAt(target, 0.9f, RING_FLOOR);
        if (FoodStock.Instance != null && FoodStock.Instance.Get(STARTER_RECIPE) < 1) FoodStock.Instance.Add(STARTER_RECIPE, 1);
        yield return Brief(BriefingTexts.Tutorial(1));

        // 1) 달려가기
        float blinkAt = Time.time + 20f;
        while (!skipRequested)
        {
            Transform chef = Chef();
            if (chef != null && Vector2.Distance(chef.position, target) <= 1.0f) break;
            if (Time.time > blinkAt) { blinkAt = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] 포탑 칸은 주방 오른쪽이야. 칸 사이 통로 발판으로 건너가"); }
            yield return null;
        }
        SetProgress(1, 2);

        // 2) 카드 끌어다 놓기 (브리핑이 닫힌 뒤의 투입만 인정 - 프리셋 투입은 그 전)
        if (!skipRequested)
        {
            SetLines("화면 아래 더블 육포 카드를 끌어서\n화살표 밑 [+] 이름표에 놔");
            if (slot != null) ShowMarkerFollow(slot.transform, SlotArrowTip(1), RING_TURRET);
            int filledAtStart = FilledSlotCount();
            float insertMark = Time.time;
            float hintAt = Time.time + 15f;
            while (!skipRequested)
            {
                if (FilledSlotCount() > filledAtStart || TurretSlot.LastInsertTime > insertMark) break;
                if (Time.time > hintAt) { hintAt = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] 카드를 [+] 이름표에 끌어다 놔. 카드 누르고 이름표 눌러도 돼"); }
                yield return null;
            }
        }
        SetProgress(2, 2);
        yield return EndStep();
    }

    // ── 2. 첫 손님 + 재료 (v2.7: 옛 3·4 묶음 - 랩터 2가 쓰러지고 고기 2가 모이면) ──
    private IEnumerator Step2_GuestsMaterials()
    {
        BeginStep(2, "첫 손님과 재료", "포탑이 알아서 쏘니까 지켜만 봐\n쓰러진 손님 재료는 기차로 모여", "손님", 0, 2);
        HideMarker();
        yield return Brief(BriefingTexts.Tutorial(2));

        float spawnDist = TailSpawnDistance();
        List<Enemy> guests = WaveManager.Instance != null ? WaveManager.Instance.SpawnForTutorial("raptor", 2, 0.7f, 0f, spawnDist) : null;
        int need = guests == null ? 0 : guests.Count;         // 프리팹도 폴백도 없으면 0 - 바로 통과 (막히지 않게)
        yield return null;                                    // 스폰 프레임엔 HP 가 아직 - 다음 프레임부터 센다
        Spino("[스피노] 애피타이저 왔네. 요리가 곧 총알이야");
        float giveUpAt = Time.time + 90f;
        while (!skipRequested && need > 0)
        {
            int dead = CountDead(guests);
            SetProgress(dead, 2);
            if (dead >= need) break;
            if (Time.time > giveUpAt) break;                  // 포탑이 못 잡는 이상 상황 - 막히지 않게
            yield return null;
        }
        KillAll(guests);
        SetProgress(2, 2);

        // 재료가 날아와 쌓이는 것을 본다 (10초 안에 안 오면 찬장에서)
        if (!skipRequested)
        {
            SetLines("재료는 알아서 빨려 와\n화면 아래 왼쪽에서 개수를 봐");
            progLabel = "고기"; SetProgress(MeatCount(), 2);
            float fallbackAt = Time.time + 10f;
            while (!skipRequested)
            {
                SetProgress(MeatCount(), 2);
                if (MeatCount() >= 2) break;
                if (Time.time > fallbackAt)
                {
                    if (MaterialInventory.Instance != null) MaterialInventory.Instance.Add(MaterialType.Meat, 2 - MeatCount());
                    UIManager.Instance?.ShowStatChange("[견습] 찬장에서 고기 꺼내 놨어");
                    break;
                }
                yield return null;
            }
        }
        if (MeatCount() < 2 && MaterialInventory.Instance != null) MaterialInventory.Instance.Add(MaterialType.Meat, 2 - MeatCount());
        SetProgress(2, 2);
        if (!skipRequested) yield return Brief(BriefingTexts.Tutorial(3));   // 재료 칸 설명은 찬 뒤에
        yield return EndStep();
    }

    // ── 3. 굽기 (진짜 미니게임) ──
    private IEnumerator Step3_Grill()
    {
        CookingStation grill = FindStation(CookingStation.StationType.Grilling);
        BeginStep(3, "그릴에서 더블 육포 굽기", "그릴 앞에서 [E], 고기 두 개 올려\n눈금이 초록 구간에 오면 [Space]", "굽기", 0, 1);
        if (grill != null) ShowMarkerFollow(grill.transform, 1.0f, RING_STATION);
        yield return Brief(BriefingTexts.Tutorial(4));
        yield return CookOne(STARTER_RECIPE, MaterialType.Meat, MaterialType.Meat, "[견습] 조리 창에서 고기 카드를 두 번 누르거나 끌어 놓고 [굽기 시작]", "[견습] 탔네. 고기 다시 줬어. 초록 구간에서 [Space]");
        if (!skipRequested && !lastCookGifted) Spino("[스피노] 바로 그거야. 요리가 곧 총알이지");
        SetProgress(1, 1);
        yield return EndStep();
    }

    private bool lastCookGifted = false;

    /// <summary>
    /// v2.7: 요리 하나를 만들 때까지 기다린다 (카드 3·5 공용). 성공 = 그 요리를 만들었다는 알림(FoodStock.OnCooked)이 기준치 since 보다 늘었다 -
    /// 재고 수로 세지 않는다 (만든 걸 바로 포탑에 넣어도, 앞 할 일에서 미리 만들어 뒀어도 인정). since = CookedCount(recipeId) 기준치 (-1 = 지금).
    /// 조리 창을 열고 20초 안에 미니게임에 못 들어가면 힌트 / BAD 로 재료를 잃으면 다시 준다 (3회) - 3회 다 태우면 요리를 주고 넘긴다 (막히지 않게) /
    /// 재료를 다른 데 써 버렸거나 [ESC] 로 그만뒀으면 조리 창이 닫혀 있을 때 2초마다 채운다
    /// </summary>
    private IEnumerator CookOne(string recipeId, MaterialType a, MaterialType b, string openHint, string badHint, int since = -1)
    {
        lastCookGifted = false;
        if (FoodStock.Instance == null) yield break;
        if (since < 0) since = CookedCount(recipeId);
        if (CookedCount(recipeId) > since) yield break;      // 앞 할 일에서 이미 만들었다
        int badsSeen = CookingBridge.BadsThisRun;
        int resupplies = 0;
        float hintAt = float.MaxValue;
        float topUpAt = Time.time + 2f;
        bool kitchenSeen = false;
        TopUpPair(a, b);
        while (!skipRequested)
        {
            if (CookedCount(recipeId) > since) break;
            if (!kitchenSeen && KitchenPanel.IsOpenStatic) { kitchenSeen = true; hintAt = Time.time + 20f; }
            if (CookingMinigame.IsActive) hintAt = float.MaxValue;
            if (Time.time > hintAt) { hintAt = float.MaxValue; UIManager.Instance?.ShowStatChange(openHint); }

            if (CookingBridge.BadsThisRun > badsSeen)
            {
                badsSeen = CookingBridge.BadsThisRun;
                resupplies++;
                if (resupplies > 3)
                {
                    FoodStock.Instance.Add(recipeId, 1);
                    UIManager.Instance?.ShowStatChange("[견습] 요리 하나 만들어 놨어. 조리는 나중에 더 연습하자");
                    lastCookGifted = true;
                    break;
                }
                TopUpPair(a, b);
                UIManager.Instance?.ShowStatChange(badHint);
            }
            else if (Time.time > topUpAt && !KitchenPanel.IsOpenStatic && !CookingMinigame.IsActive)
            {
                topUpAt = Time.time + 2f;
                TopUpPair(a, b);   // 창이 닫혀 있을 때만 - 창에 올려 둔 재료와 겹쳐 세지 않게
            }
            yield return null;
        }
    }

    /// <summary>재료 둘(같은 종류면 2개)이 모자라면 채운다</summary>
    private static void TopUpPair(MaterialType a, MaterialType b)
    {
        if (MaterialInventory.Instance == null) return;
        if (a == b) { int have = MaterialInventory.Instance.Get(a); if (have < 2) MaterialInventory.Instance.Add(a, 2 - have); return; }
        if (MaterialInventory.Instance.Get(a) < 1) MaterialInventory.Instance.Add(a, 1);
        if (MaterialInventory.Instance.Get(b) < 1) MaterialInventory.Instance.Add(b, 1);
    }

    // ── 4. 투입 = 레벨업 ──
    private IEnumerator Step4_LevelUp()
    {
        TurretSlot first = SlotAt(0);
        BeginStep(4, "같은 요리로 Lv 올리기", "구운 육포 카드를 화살표 포탑에 놔\n다른 요리가 든 칸에 놓으면 교체야", "넣기", 0, 1);
        if (FoodStock.Instance != null && TotalFood() < 1) FoodStock.Instance.Add(STARTER_RECIPE, 1);
        if (first != null) ShowMarkerFollow(first.transform, SlotArrowTip(0), RING_TURRET);
        yield return Brief(BriefingTexts.Tutorial(5));

        float insertMark = Time.time;
        float hintAt = Time.time + 20f;
        while (!skipRequested)
        {
            if (TurretSlot.LastInsertTime > insertMark) break;
            if (Time.time > hintAt) { hintAt = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] 카드를 화살표 포탑 이름표에 끌어다 놔. 카드 누르고 이름표 눌러도 돼"); }
            yield return null;
        }
        SetProgress(1, 1);
        yield return EndStep();
    }

    // ── 5. (v2.7 신규) 팬과 솥: 과부하 코일 볶기(커맨드) -> 철판 정식 끓이기(압력) ──
    private IEnumerator Step5_PanPot()
    {
        CookingStation pan = FindStation(CookingStation.StationType.Saute);
        CookingStation pot = FindStation(CookingStation.StationType.Boiling);
        int panSince = CookedCount(PAN_RECIPE), potSince = CookedCount(POT_RECIPE);   // 카드 시작 기준치 - 팬 차례에 솥 요리를 먼저 만들어도 인정
        SetCardBig(true);
        BeginStep(5, "팬과 솥에서 요리 둘", "팬 [E]: 전기알 둘로 과부하 코일. 뜨는 키를 순서대로\n솥 [E]: 장갑 둘로 철판 정식. [Space] 꾹으로 바늘을 구간에", "요리", 0, 2);
        SetFooter("조리법은 요리마다 정해져 있어. 틀리면 재료는 다시 줘", "[Enter] 건너뛰기");
        TopUpPair(MaterialType.Elec, MaterialType.Elec);
        TopUpPair(MaterialType.Armor, MaterialType.Armor);
        if (pan != null) ShowMarkerFollow(pan.transform, 1.0f, RING_STATION);
        yield return Brief(BriefingTexts.Tutorial(6));

        // 1) 팬 - 볶기
        bool gifted = false;
        yield return CookOne(PAN_RECIPE, MaterialType.Elec, MaterialType.Elec,
            "[견습] 팬 앞에서 [E]. 전기알 카드 두 번, [볶기 시작]",
            "[견습] 틀렸네. 전기알 다시 줬어. 뜨는 키를 차례로", panSince);
        gifted |= lastCookGifted;
        SetProgress(1, 2);

        // 2) 솥 - 끓이기 (팬 차례에 먼저 끓여 뒀으면 바로 통과)
        if (!skipRequested)
        {
            SetLines("볶았어. 이제 솥. 장갑 둘 올리고 [끓이기 시작]\n[Space] 꾹 = 바늘이 오른쪽, 떼면 왼쪽. 구간 안에 둬");
            if (pot != null) ShowMarkerFollow(pot.transform, 1.0f, RING_STATION);
            if (CookedCount(POT_RECIPE) > potSince) UIManager.Instance?.ShowStatChange("[견습] 솥은 벌써 했네");
            else UIManager.Instance?.ShowStatChange("[견습] 됐어. 이번엔 솥 - 장갑 둘로 철판 정식");
            yield return CookOne(POT_RECIPE, MaterialType.Armor, MaterialType.Armor,
                "[견습] 솥 앞에서 [E]. 장갑 카드 두 번, [끓이기 시작]",
                "[견습] 넘쳤네. 장갑 다시 줬어. [Space]로 바늘을 구간 안에", potSince);
            gifted |= lastCookGifted;
        }
        SetProgress(2, 2);
        if (!skipRequested && !gifted) Spino("[스피노] 셋 다 해 봤네. 조리법은 요리마다 정해져 있어");
        yield return EndStep();
        SetCardBig(false);
    }

    private const string PAN_RECIPE = "elec+elec";     // 과부하 코일 (볶기)
    private const string POT_RECIPE = "armor+armor";   // 철판 정식 (끓이기 - 쏘지 않는 지원 요리)

    // ── 6. (v2.7 신규) 진화 조리 + 교체: 더블 육포 카드를 과부하 코일 카드 위에 -> 전설 -> 육포 포탑에 교체 / 철판 정식은 빈 칸 ──
    private IEnumerator Step6_FuseReplace()
    {
        TurretSlot first = SlotAt(0);
        SetCardBig(true);
        BeginStep(6, "진화 조리와 교체", "더블 육포 카드를 끌어서 과부하 코일 카드 위에 놔\n2판을 넘기면 전설 요리 카드가 나와\n전설은 육포 포탑에 놓아 교체, 철판 정식은 빈 칸에", "할 일", 0, 3);
        SetFooter("끌면 합칠 수 있는 카드에 분홍 테가 떠", "[Enter] 건너뛰기");
        HideMarker();
        EnsureFuseCards();
        yield return Brief(BriefingTexts.Tutorial(7));

        // 1) 진화 조리 - 전설 요리 재고가 늘면
        int t2Start = LegendaryStock();
        float hintAt = Time.time + 25f;
        float checkAt = Time.time + 2f;
        while (!skipRequested)
        {
            if (LegendaryStock() > t2Start) break;
            if (Time.time > hintAt) { hintAt = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] 육포 카드를 끌어서 분홍 테 카드 위에 놔"); AddHintLine("끌면 분홍 테가 뜨는 카드 위에 놔. 안 되면 [Enter]"); }
            if (Time.time > checkAt) { checkAt = Time.time + 2f; EnsureFuseCards(); }   // 카드를 포탑에 넣어 버렸으면 다시 준다
            yield return null;
        }
        SetProgress(1, 3);

        // 2) 전설은 육포 포탑에 (교체) + 철판 정식은 빈 칸에 - 순서는 상관없다
        if (!skipRequested)
        {
            SetLines("전설 카드를 화살표 포탑에 놔 = 교체\n든 요리는 폐기되고 재료 일부가 돌아와\n철판 정식은 빈 칸에. 안 쏘지만 기차 최대 HP가 올라");
            if (first != null) ShowMarkerFollow(first.transform, SlotArrowTip(0), RING_TURRET);
            RecipeData fusion = RecipeDatabase.GetFusion(FoodTag.Phys, FoodTag.Elec);   // 개틀링 티렉스 (더블 육포 + 과부하 코일)
            float hint2At = Time.time + 25f;
            float regiveAt = 0f;
            bool legendaryIn = false, potIn = false;
            while (!skipRequested)
            {
                legendaryIn = AnySlotTier(2);
                potIn = AnySlotRecipe(POT_RECIPE);
                SetProgress(1 + (legendaryIn ? 1 : 0) + (potIn ? 1 : 0), 3);
                if (legendaryIn && potIn) break;
                if (legendaryIn && markerOn) HideMarker();
                if (FoodStock.Instance != null && Time.time > regiveAt && !InfusingMinigame.IsActive && !CookingMinigame.IsActive)
                {
                    if (!potIn && FoodStock.Instance.Get(POT_RECIPE) < 1)
                    {
                        FoodStock.Instance.Add(POT_RECIPE, 1);   // 폐기나 교체로 없어졌으면 다시
                        regiveAt = Time.time + 1f;
                        UIManager.Instance?.ShowStatChange("[견습] 철판 정식 하나 더 줬어. 빈 칸에 놔");
                    }
                    else if (!legendaryIn && LegendaryStock() == 0)
                    {
                        // 전설을 넣은 칸에 다른 카드를 놓아 폐기됐다 (또는 카드를 버렸다) - 두 판을 또 시키지 않고 전설 하나를 다시
                        if (fusion != null) FoodStock.Instance.Add(fusion.recipeId, 1); else EnsureFuseCards();
                        regiveAt = Time.time + 2f;
                        UIManager.Instance?.ShowStatChange("[견습] 전설이 없어졌네. 하나 더 줬어 - 육포 포탑에 놔");
                    }
                }
                if (Time.time > hint2At) { hint2At = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] 전설은 육포 포탑 이름표에, 철판 정식은 [+]에"); }
                yield return null;
            }
        }
        SetProgress(3, 3);
        if (!skipRequested) Spino("[스피노] 전설 하나에 지원 하나. 이제 진짜 주방이네");
        yield return EndStep();
        SetCardBig(false);
    }

    /// <summary>진화 조리 재료 카드(더블 육포 1 + 과부하 코일 1)가 재고에 없으면 준다 - 포탑에 넣어 버렸거나 폐기했을 때</summary>
    private static void EnsureFuseCards()
    {
        if (FoodStock.Instance == null || InfusingMinigame.IsActive) return;
        bool gave = false;
        if (FoodStock.Instance.Get(STARTER_RECIPE) < 1) { FoodStock.Instance.Add(STARTER_RECIPE, 1); gave = true; }
        if (FoodStock.Instance.Get(PAN_RECIPE) < 1) { FoodStock.Instance.Add(PAN_RECIPE, 1); gave = true; }
        if (gave) UIManager.Instance?.ShowStatChange("[견습] 합칠 카드 둘을 아래에 놔 뒀어");
    }

    /// <summary>재고의 전설(2등급) 요리 수</summary>
    private static int LegendaryStock()
    {
        if (FoodStock.Instance == null || RecipeDatabase.All == null) return 0;
        int n = 0;
        foreach (RecipeData r in RecipeDatabase.All)
            if (r != null && r.tier == 2) n += FoodStock.Instance.Get(r.recipeId);
        return n;
    }

    private static bool AnySlotTier(int tier)
    {
        if (TurretSlotManager.Instance == null || TurretSlotManager.Instance.slots == null) return false;
        for (int i = 0; i < TurretSlotManager.Instance.slots.Length; i++)
        {
            TurretSlot s = TurretSlotManager.Instance.slots[i];
            if (s != null && !s.IsEmpty && s.Recipe != null && s.Recipe.tier == tier) return true;
        }
        return false;
    }

    private static bool AnySlotRecipe(string recipeId)
    {
        if (TurretSlotManager.Instance == null || TurretSlotManager.Instance.slots == null) return false;
        for (int i = 0; i < TurretSlotManager.Instance.slots.Length; i++)
        {
            TurretSlot s = TurretSlotManager.Instance.slots[i];
            if (s != null && !s.IsEmpty && s.recipeId == recipeId) return true;
        }
        return false;
    }

    // ── 7. 멈춘 포탑 살리기 (v2.7: 감전 [E] 한 번 -> 같은 포탑 과열 [E] 꾹 + 마우스) ──
    private IEnumerator Step7_StunOverheat()
    {
        TurretSlot target = AnyShootingSlot(1);
        BeginStep(7, "멈춘 포탑 살리기", "감전: 옆에 서서 [E] 한 번\n과열: [E] 꾹 + 마우스 흔들기", "포탑", 0, 2);
        HideMarker();
        if (target == null)
        {
            // 가동 중인 포탑이 하나도 없다 - 연습이 성립하지 않는다. 구간 기록은 "건너뜀" (훈련장에서 다시)
            Debug.LogWarning("[Tutorial] 멈출 포탑이 없어 카드 7 을 건너뛴다");
            UIManager.Instance?.ShowStatChange("[견습] 멈출 포탑이 없어서 이 카드는 건너뛸게");
            segSkips++;
        }
        else
        {
            // 1) 낙뢰 연출: 흰 번쩍 + 흔들림 + 감전 (브리핑은 그 뒤 - "포탑이 멈췄다" 는 과거형)
            LightningFx();
            target.StunSlot(999f, "감전");
            yield return WaitUnscaled(0.7f);
            yield return Brief(BriefingTexts.Tutorial(8));
            if (target.IsStunned) ShowMarkerFollow(target.transform, SlotArrowTip(SlotIndexOf(target)), RING_TURRET);

            float hintAt = Time.time + 20f;
            while (!skipRequested)
            {
                if (!target.IsStunned) break;                  // SlotMarkerUI 가 [E] 한 번에 ClearStun
                if (Time.time > hintAt) { hintAt = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] 감전된 포탑 바로 앞, 벽 쪽까지 붙어서 [E] 한 번"); }
                yield return null;
            }
            if (target.IsStunned) target.ClearStun();   // 건너뛰기 - 멈춘 채로 두지 않는다
            SetProgress(1, 2);

            // 2) 같은 포탑을 과열시킨다 - 카드를 읽은 뒤에 (읽는 동안 식는 시간이 흐르거나 먼저 식혀 버리지 않게). [E] 꾹 + 마우스 (견습에선 방치해도 파손되지 않는다 - TurretSlot 이 Active 를 본다)
            if (!skipRequested && GameBalance.TutorialOverheatOn)
            {
                yield return WaitUnscaled(0.8f);
                yield return Brief(BriefingTexts.Tutorial(9));
                if (!skipRequested)
                {
                    target.ForceOverheat();
                    SetLines("붉은 연기 나는 포탑 옆에서\n[E]를 꾹 누른 채 마우스를 흔들어");
                    if (target.IsStunned) ShowMarkerFollow(target.transform, SlotArrowTip(SlotIndexOf(target)), RING_TURRET);
                    float hint2At = Time.time + 20f;
                    while (!skipRequested)
                    {
                        if (!target.IsStunned) break;
                        if (Time.time > hint2At) { hint2At = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] [E]를 누른 채 마우스를 좌우로 흔들어"); }
                        yield return null;
                    }
                    if (target.IsStunned) target.ClearStun();
                }
            }
        }
        SetProgress(2, 2);
        yield return EndStep();
    }

    /// <summary>낙뢰 연출 (견습 카드 7 / 인라인 연습 / 자유 연습 공용). 정식 운행 협곡 낙뢰는 WaveManager 가 같은 연출을 낸다</summary>
    public static void LightningFx()
    {
        WarningFX.Flash("낙뢰!", 0.6f, new Color(1f, 0.96f, 0.7f));
        GameFeel.Shake(0.2f);
        SoundManager.Play("sfx_boss_warning");   // 클립 없으면 무시
    }

    // ── 8. (v2.7 신규) 주방 사고: 기구 고장(방향키) -> 화재([E] 꾹). 실패해도 기차는 안 다치고(무적), 한 번 더 ──
    private IEnumerator Step8_KitchenEvents()
    {
        SetCardBig(true);
        BeginStep(8, "주방 사고 막기", "사고가 나면 붉은 화살표 쪽으로 달려가\n현장에 닿으면 카드가 뭘 할지 알려 줘\n기구 고장 ▶ 화재, 둘. 실패해도 기차는 안 다쳐", "사고", 0, 2);
        SetFooter("못 막으면 같은 사고를 한 번 더", "[Enter] 건너뛰기");
        HideMarker();
        yield return Brief(BriefingTexts.Tutorial(10));

        int[] kinds = { 4, 2 };          // KitchenEventManager.ForceEvent: 4 = 기구 고장 / 2 = 화재
        string[] names = { "기구 고장", "화재" };
        int failedKinds = 0;             // 두 번 다 못 막은 사고 수
        bool unavailable = false;        // 사고를 일으킬 수 없다 (매니저 없음·계속 거부·계속 취소)
        ForcedEventsOnly = true;
        for (int k = 0; k < kinds.Length && !skipRequested && !unavailable; k++)
        {
            KitchenEventManager kem = KitchenEventManager.Instance;
            if (kem == null) { Debug.LogWarning("[Tutorial] KitchenEventManager 없음 - 사고 카드는 건너뛴다"); unavailable = true; break; }
            int tries = 0, cancels = 0;
            while (!skipRequested)
            {
                // 앞 사고의 결과 연출·조리 미니게임이 끝나길 (기구 고장의 방향키가 조리 입력과 겹친다)
                while (!skipRequested && (kem.EventRunning || CookingMinigame.IsActive)) yield return null;
                if (skipRequested) break;
                int ended = KitchenEventManager.EventsEnded;
                float tryUntil = Time.unscaledTime + 3f;
                while (!skipRequested && !kem.EventRunning && Time.unscaledTime < tryUntil)
                {
                    kem.ForceEvent(kinds[k]);                 // 매니저가 거부하면(소화기 등) 3초 동안 다시
                    if (!kem.EventRunning) yield return null;
                }
                if (!kem.EventRunning) { Debug.LogWarning("[Tutorial] 사고를 못 일으켰다 - " + names[k]); unavailable = true; break; }
                // 끝나길 (성공·실패). 결과 없이 취소되면(EventsEnded 그대로) 같은 사고를 다시 - 세 번 취소되면 포기
                while (!skipRequested && kem.EventRunning && KitchenEventManager.EventsEnded == ended) yield return null;
                if (skipRequested) break;
                if (KitchenEventManager.EventsEnded == ended)
                {
                    cancels++;
                    if (cancels >= 3) { Debug.LogWarning("[Tutorial] 사고가 계속 취소된다 - " + names[k]); unavailable = true; break; }
                    yield return null;
                    continue;
                }
                if (TrainManager.Instance != null) TrainManager.Instance.currentHP = TrainManager.Instance.currentMaxHP;   // 실패 피해는 바로 되돌린다
                if (KitchenEventManager.LastEventSuccess) { SetProgress(k + 1, 2); break; }
                tries++;
                if (tries >= 2) { UIManager.Instance?.ShowStatChange("[견습] " + names[k] + " - 정식 운행에서 다시 해 보자"); failedKinds++; SetProgress(k + 1, 2); break; }
                UIManager.Instance?.ShowStatChange("[견습] 못 막았네. 기차는 안 다쳤어. 같은 사고 한 번 더");
                yield return WaitUnscaled(1.5f);
            }
            if (k == 0 && !skipRequested && !unavailable) yield return WaitUnscaled(2.0f);   // 결과 배너를 읽을 틈
        }
        ForcedEventsOnly = false;
        if (TrainManager.Instance != null) TrainManager.Instance.currentHP = TrainManager.Instance.currentMaxHP;
        if (unavailable && !skipRequested) UIManager.Instance?.ShowStatChange("[견습] 사고를 일으킬 수 없어서 이 카드는 건너뛸게");
        if ((unavailable || failedKinds > 0) && !skipRequested) segSkips++;   // 못 막았거나 못 일으켰다 - 구간 기록은 "건너뜀" (훈련장에서 다시)
        SetProgress(2, 2);
        if (!skipRequested && !unavailable && failedKinds == 0) Spino("[스피노] 침입이랑 흘림도 있어. 처음 만나면 카드가 알려 줄 거야");
        yield return EndStep();
        SetCardBig(false);
    }

    // ── 9. 기관실: 작살(바위) -> 레버(전속 주행) (v2.7: 옛 8·9 묶음. 자동 통과 없음 - 30초에 힌트, [Enter] 만) ──
    private IEnumerator Step9_EngineCab()
    {
        EngineCabUnlocked = true;
        SetCardBig(true);
        BeginStep(9, "기관실: 작살과 레버", "작살포 옆에서 [E]로 창밖 바위를 낚아\n그다음 레버 옆에서 [E]를 꾹 = 전속 주행", "기관실", 0, 2);
        SetFooter(Mathf.RoundToInt(GameBalance.InlineHintSec) + "초가 지나면 힌트가 더 떠", "[Enter] 건너뛰기");
        ShowMarkerAt(new Vector3(GameBalance.HarpoonX, 1.7f, 0f), 1.0f, RING_HARPOON);
        yield return Brief(BriefingTexts.Tutorial(11));

        // 1) 작살 - 디렉터가 바위를 띄운다
        int mark = EngineCab.HarpoonRetrievals;
        float nextRockAt = 0f;
        float hintAt = Time.time + 15f;
        float hint2At = Time.time + GameBalance.InlineHintSec;
        while (!skipRequested)
        {
            if (EngineCab.HarpoonRetrievals > mark) break;
            if (EngineCab.RockCount == 0 && Time.time >= nextRockAt)
            {
                EngineCab.SpawnRockNow(-13f);
                nextRockAt = Time.time + 1.5f;
            }
            if (Time.time > hintAt) { hintAt = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] 작살포는 기관실 왼쪽 위에 있어. 바위가 사거리에 들어오면 [E]"); }
            if (Time.time > hint2At) { hint2At = float.MaxValue; AddHintLine("바위가 작살포 앞을 지날 때 [E]. 재장전 " + Mathf.RoundToInt(GameBalance.HarpoonCooldown) + "초. 안 되면 [Enter]"); }
            yield return null;
        }
        SetProgress(1, 2);

        // 2) 레버 - [E] 잠깐 꾹
        if (!skipRequested)
        {
            SetLines("낚았어. 이제 레버 - 기관실 오른쪽 아래 기둥\n옆에서 [E]를 " + GameBalance.LeverHoldSec + "초 꾹. 다시 당기면 원래대로\n빨라지면 골드가 늘지만 손님도 더 와");
            ShowMarkerAt(new Vector3(GameBalance.LeverX, 0.35f, 0f), 1.0f, RING_LEVER);
            yield return Brief(BriefingTexts.Tutorial(12));
            int leverMark = EngineCab.LeverPulls;
            float hint3At = Time.time + 12f;
            float hint4At = Time.time + GameBalance.InlineHintSec;
            while (!skipRequested)
            {
                if (EngineCab.LeverPulls > leverMark) break;
                if (Time.time > hint3At) { hint3At = float.MaxValue; UIManager.Instance?.ShowStatChange("[견습] 레버 옆에서 [E]를 " + GameBalance.LeverHoldSec + "초 꾹 눌러. 떼면 취소돼"); }
                if (Time.time > hint4At) { hint4At = float.MaxValue; AddHintLine("레버는 기관실 오른쪽 아래 기둥이야. [E]를 " + GameBalance.LeverHoldSec + "초 꾹. 안 되면 [Enter]"); }
                yield return null;
            }
        }
        if (EngineCab.FullSteam) EngineCab.ForceCruise();   // 다음 카드는 순항으로
        SetProgress(2, 2);
        yield return EndStep();
        SetCardBig(false);
    }

    // ── 10. 실전: 랩터 5 + 프테라 1 (정식 스탯), 기차 피해 켬, 단골 주문 (v2.7). 기차가 멈추면 이 카드만 다시 ──
    private IEnumerator Step10_Defense()
    {
        int RAPTORS = Mathf.Max(0, GameBalance.TutorialDefenseRaptors), PTERAS = Mathf.Max(0, GameBalance.TutorialDefensePteras);
        BeginStep(10, "몰려오는 손님 막기", "요리 늘리고, 멈춘 포탑은 [E]로 살려\n왼쪽 주문을 채우면 포탑이 세져", "손님", 0, RAPTORS + PTERAS);
        HideMarker();
        if (MaterialInventory.Instance != null && MeatCount() < 2) MaterialInventory.Instance.Add(MaterialType.Meat, 2 - MeatCount());   // 싸우면서 구울 재료
        yield return Brief(BriefingTexts.Tutorial(13));
        yield return Brief(BriefingTexts.TutorialPtera());   // 정식 운행 첫 등장 카드와 같은 형식 (기록은 안 남긴다)

        godMode = false;                                     // 이 카드만 기차가 다친다
        float statMul = 1.0f;
        int need = 0;
        bool abandoned = false;
        List<Enemy> guests = SpawnDefenseWave(RAPTORS, PTERAS, statMul, out need);
        float capAt = GameBalance.TutorialDefenseSec > 0f ? Time.time + GameBalance.TutorialDefenseSec : float.MaxValue;   // 못 잡는 손님이 남는 이상 상황의 한도
        yield return null;                                   // 스폰 프레임엔 HP 가 아직 - 다음 프레임부터 센다
        Spino("[스피노] 이번엔 진짜야. 요리 늘리고, 멈춘 포탑은 달려가서 살려");
        trainStoppedFlag = false;
        if (GameBalance.TutorialOrderOn) OrderSystem.TutorialOrder(FoodTag.Phys, 1, GameBalance.TutorialOrderSec);   // 단골 주문: 물리 요리 1개

        while (!skipRequested && need > 0)
        {
            int dead = CountDead(guests);
            SetProgress(dead, RAPTORS + PTERAS);
            if (dead >= need) break;
            if (Time.time > capAt) { UIManager.Instance?.ShowStatChange("[견습] 남은 손님은 물러갔어. 다음으로 가자"); break; }

            if (trainStoppedFlag)
            {
                // GameManager.OnTrainDestroyed 가 기차를 고쳐 놓고 알렸다 - 손님 정리 -> 재시작 카드 -> 같은 구성 다시 (3회째부터 약체, 4회째는 포기)
                trainStoppedFlag = false;
                retries10++;
                KillAll(guests);
                godMode = true;
                if (retries10 >= 4) { UIManager.Instance?.ShowStatChange("[견습] 손님들이 물러갔어. 정식 운행에서 다시 해 보자"); abandoned = true; break; }
                yield return Brief(BriefingTexts.TutorialRetry());
                if (retries10 >= 2) statMul = 0.6f;
                if (MaterialInventory.Instance != null && MeatCount() < 2) MaterialInventory.Instance.Add(MaterialType.Meat, 2 - MeatCount());
                godMode = false;
                guests = SpawnDefenseWave(RAPTORS, PTERAS, statMul, out need);
                capAt = GameBalance.TutorialDefenseSec > 0f ? Time.time + GameBalance.TutorialDefenseSec : float.MaxValue;
                SetProgress(0, RAPTORS + PTERAS);
                if (GameBalance.TutorialOrderOn && !OrderSystem.TutorialDone) OrderSystem.TutorialOrder(FoodTag.Phys, 1, GameBalance.TutorialOrderSec);
                Debug.Log("[Tutorial] 카드 10 재시작 " + retries10 + "회 (배율 " + statMul + ")");
                yield return null;
            }
            yield return null;
        }
        KillAll(guests);
        godMode = true;
        if (TrainManager.Instance != null) TrainManager.Instance.currentHP = TrainManager.Instance.currentMaxHP;
        OrderSystem.TutorialEnd();
        if (abandoned && !skipRequested) segSkips++;         // 네 번 멈췄다 - 구간 기록은 "건너뜀" (훈련장에서 다시)
        SetProgress(RAPTORS + PTERAS, RAPTORS + PTERAS);
        yield return EndStep();
    }

    /// <summary>기차 꼬리 오른쪽 화면 안 스폰 거리 (카메라 반폭 15.1u) - 기차 피벗은 0 이라 거리 = 꼬리 x + 3.5</summary>
    private static float TailSpawnDistance()
    {
        float d = GameBalance.CarEdgesX[GameBalance.CarEdgesX.Length - 1] + 3.5f;
        if (TrainManager.Instance != null) d -= TrainManager.Instance.transform.position.x;
        return d;
    }

    /// <summary>실전 구성 스폰: 랩터는 기차 꼬리 오른쪽 화면 안에서, 프테라는 조금 더 멀리서. need = 실제로 나온 수</summary>
    private static List<Enemy> SpawnDefenseWave(int raptors, int pteras, float statMul, out int need)
    {
        List<Enemy> all = new List<Enemy>();
        need = 0;
        if (WaveManager.Instance == null) return all;
        float spawnDist = TailSpawnDistance();
        List<Enemy> a = WaveManager.Instance.SpawnForTutorial("raptor", raptors, statMul, 0f, spawnDist);
        List<Enemy> b = WaveManager.Instance.SpawnForTutorial("ptera", pteras, statMul, 0f, spawnDist + 2.5f);
        if (a != null) all.AddRange(a);
        if (b != null) all.AddRange(b);
        need = all.Count;
        return all;
    }

    // ── 11. 정산: 진짜 증강 선택창 -> [G] 정비소 한 번 열기 ──
    private IEnumerator Step11_Settlement()
    {
        BeginStep(11, "증강 고르고 정비소 열기", "[1~5]로 증강 하나 골라\n그다음 [G]로 정비소를 열어 봐", "증강·정비소", 0, 2);
        HideMarker();
        yield return Brief(BriefingTexts.Tutorial(14));

        bool picked = false;
        if (AugmentPickUI.Instance != null) AugmentPickUI.Instance.Open(1, delegate { picked = true; });
        else picked = true;
        while (!picked && !skipRequested && AugmentPickUI.IsOpen) yield return null;
        if (!picked && !skipRequested) yield return null;   // 콜백이 닫힘 다음 프레임에 오는 경우
        SetProgress(1, 2);

        float hintAt = Time.unscaledTime + 20f;
        bool highlighted = false;
        while (!skipRequested)
        {
            if (WorkshopUI.IsOpen) break;
            if (WorkshopUI.Instance == null) { Debug.LogWarning("[Tutorial] WorkshopUI 없음 - 정비소 반쪽은 건너뛴다"); break; }
            if (!highlighted && Time.unscaledTime > hintAt)
            {
                highlighted = true;
                if (linesText != null) { linesText.text = "지금 [G]를 눌러서 정비소를 열어 봐\n정식 운행에선 정차역마다 열 수 있어"; linesText.color = HINT_GOLD; }
                UIManager.Instance?.ShowStatChange("[견습] [G]를 눌러서 정비소를 열어 봐. 수리, 연마, 재료 시장이 있어");
            }
            yield return null;
        }
        if (linesText != null) linesText.color = UIFactory.CREAM;
        SetProgress(2, 2);
        while (!skipRequested && WorkshopUI.IsOpen) yield return null;   // 구경 끝날 때까지 (닫으면 완료)
        yield return EndStep();
    }

    // ── 12. 미니 보스 예습: 새끼 발톱 + 랩터 무리 + 미끼 화덕. 미끼 BossPracticeLures 회 -> (v2.7) 무방비 때 [F] -> 새끼 발톱 처치 ──
    private IEnumerator Step12_BossPractice()
    {
        int LURES = Mathf.Max(1, GameBalance.BossPracticeLures);
        string body = "1. 왼쪽 아래 [미끼 굽기]를 눌러. 고기 1개가 들어\n2. 눈금이 초록 구간에 오면 [Space]\n3. 무리가 미끼 쪽에 가 있는 동안 포탑이 왕을 때려\n4. 왕이 무방비(노란 띠)가 되면 [F]로 독침 육포를 던져";
        SetCardBig(true);
        BeginStep(12, "미끼로 무리 꾀어내고 보스 잡기", body, "미끼", 0, LURES);
        SetFooter("미끼 " + LURES + "번 + 처치. HP " + Mathf.RoundToInt(GameBalance.BossPracticeRetryHp * 100f) + "% 밑이면 한 번만 다시", "[Enter] 건너뛰기");
        HideMarker();
        if (MaterialInventory.Instance != null && MeatCount() < GameBalance.BossPracticeMeat)
            MaterialInventory.Instance.Add(MaterialType.Meat, GameBalance.BossPracticeMeat - MeatCount());
        if (FoodStock.Instance != null && FoodStock.Instance.Get(THROW_RECIPE) < 1) FoodStock.Instance.Add(THROW_RECIPE, 1);   // [F] 로 던질 독침 육포
        yield return Brief(BriefingTexts.TutorialBossPractice());

        // 스폰: 새끼 발톱(연습 보스) + 랩터 무리 - 기차 꼬리 오른쪽에서. 미끼 화덕은 보스가 등록될 때 생긴다 - 보스가 없으면 연습이 성립하지 않는다
        godMode = false;
        trainStoppedFlag = false;
        int luresAtStart = BaitStationUI.LuresThisScene;
        List<Enemy> pack = new List<Enemy>();
        BossEnemy cub = SpawnPractice(pack);
        yield return null;                                   // 스폰 프레임엔 HP 가 아직 - 다음 프레임부터 센다
        if (cub == null)
        {
            KillAll(pack);
            godMode = true;
            UIManager.Instance?.ShowStatChange("[예습] 보스가 없어서 이 카드는 건너뛸게");
            segSkips++;                                      // 구간 기록은 "건너뜀" (훈련장에서 다시)
            SetProgress(LURES, LURES);
            yield return EndStep();
            SetCardBig(false);
            yield break;
        }
        UIManager.Instance?.ShowWaveNotice("[예습]  첫 보스 연습 - 미끼로 무리 꾀어내기", "왼쪽 아래 미끼 화덕에서 고기를 구워");
        Spino("[스피노] 왕의 새끼야. 정면으로 붙지 말고 미끼부터 구워");

        float hintAt = Time.time + 20f;
        float hint2At = Time.time + GameBalance.InlineHintSec;
        float killBy = float.MaxValue;     // 미끼를 다 채운 뒤 처치를 기다리는 한도
        float throwRegiveAt = 0f;          // 독침 육포 다시 주기 (던졌든 포탑에 넣었든 없어졌으면 - 3초 간격, 최대 3번)
        int throwRegives = 0;
        bool retried = false, luresDone = false;
        while (!skipRequested)
        {
            int lures = BaitStationUI.LuresThisScene - luresAtStart;
            if (!luresDone)
            {
                SetProgress(lures, LURES);
                if (lures >= LURES)
                {
                    luresDone = true;
                    progLabel = "처치"; SetProgress(0, 1);
                    killBy = Time.time + GameBalance.TutorialBossKillSec;
                    SetLines("미끼는 됐어. 이제 왕을 잡아\n무리가 미끼에 간 동안 포탑이 때려\n무방비(노란 띠)가 되면 [F]로 독침 육포\n" + Mathf.RoundToInt(GameBalance.TutorialBossKillSec) + "초 안에 못 잡아도 넘어가");
                    UIManager.Instance?.ShowWaveNotice("", "[예습] 미끼 " + LURES + "번 됐어. 이제 새끼 발톱을 잡아 - 무방비면 [F]");
                }
            }
            if (cub == null || !cub.IsAlive)
            {
                if (!luresDone) UIManager.Instance?.ShowStatChange("[예습] 새끼 발톱을 잡았네. 미끼 없이도 통과야");
                break;
            }
            if (luresDone && Time.time > killBy) { UIManager.Instance?.ShowStatChange("[예습] 시간이 됐어. 진짜 왕은 정식 운행에서"); break; }
            if (luresDone && throwRegives < 3 && Time.time > throwRegiveAt && FoodStock.Instance != null && FoodStock.Instance.Get(THROW_RECIPE) < 1 && !CookingMinigame.IsActive)
            {
                FoodStock.Instance.Add(THROW_RECIPE, 1);
                throwRegives++;
                throwRegiveAt = Time.time + 3f;
                UIManager.Instance?.ShowStatChange("[예습] 독침 육포 하나 더 실어 뒀어. 무방비 때 [F]");
            }
            // 고기가 다 떨어지면 채워 준다 (미끼 = 고기 1)
            if (MaterialInventory.Instance != null && MeatCount() <= 0 && !BaitStationUI.TimingActive)
            {
                MaterialInventory.Instance.Add(MaterialType.Meat, 2);
                UIManager.Instance?.ShowStatChange("[예습] 고기 2개 더 줬어. 미끼 하나에 고기 1개야");
            }
            // 기차 HP 절반 아래 -> 다시 (1회). 두 번째부터는 기차가 안 다친다
            bool lowHp = TrainManager.Instance != null && TrainManager.Instance.HPRatio <= GameBalance.BossPracticeRetryHp;
            if ((trainStoppedFlag || lowHp) && !godMode)
            {
                trainStoppedFlag = false;
                retries12++;
                if (!retried)
                {
                    retried = true;
                    KillAll(pack); DespawnPractice(ref cub);
                    godMode = true;
                    if (TrainManager.Instance != null) TrainManager.Instance.currentHP = TrainManager.Instance.currentMaxHP;
                    if (MaterialInventory.Instance != null && MeatCount() < GameBalance.BossPracticeMeat)
                        MaterialInventory.Instance.Add(MaterialType.Meat, GameBalance.BossPracticeMeat - MeatCount());
                    if (FoodStock.Instance != null && FoodStock.Instance.Get(THROW_RECIPE) < 1) FoodStock.Instance.Add(THROW_RECIPE, 1);
                    yield return Brief(BriefingTexts.TutorialBossRetry());
                    godMode = false;
                    luresAtStart = BaitStationUI.LuresThisScene;
                    luresDone = false; killBy = float.MaxValue; progLabel = "미끼";
                    throwRegives = 0; throwRegiveAt = 0f;
                    cub = SpawnPractice(pack);
                    SetLines(body);                          // "이제 왕을 잡아" 가 남지 않게 처음 글로
                    SetProgress(0, LURES);
                    AddHintLine("무리가 붙기 전에 미끼를 던져. 굽는 동안은 포탑이 지켜");
                    Debug.Log("[Tutorial] 카드 12 재시작 1회");
                    yield return null;
                    if (cub == null) { UIManager.Instance?.ShowStatChange("[예습] 보스가 안 나와서 여기까지"); break; }
                }
                else
                {
                    godMode = true;
                    if (TrainManager.Instance != null) TrainManager.Instance.currentHP = TrainManager.Instance.currentMaxHP;
                    UIManager.Instance?.ShowStatChange("[예습] 이제 기차는 안 다쳐. 잡거나 [Enter]로 넘어가");
                }
            }
            if (Time.time > hintAt) { hintAt = float.MaxValue; UIManager.Instance?.ShowStatChange("[예습] 왼쪽 아래 [미끼 굽기]를 누르고, 눈금이 초록 구간에 오면 [Space]"); }
            if (Time.time > hint2At) { hint2At = float.MaxValue; AddHintLine("초록이면 " + Mathf.RoundToInt(GameBalance.BaitDurationGood) + "초, 노란이면 " + Mathf.RoundToInt(GameBalance.BaitDurationPerfect) + "초 붙잡아 둬. 안 되면 [Enter]"); }
            yield return null;
        }
        KillAll(pack); DespawnPractice(ref cub);
        godMode = true;
        if (TrainManager.Instance != null) TrainManager.Instance.currentHP = TrainManager.Instance.currentMaxHP;
        progLabel = luresDone ? "처치" : "미끼";
        SetProgress(1, 1);
        if (!skipRequested) Spino("[스피노] 바로 그거야. 왕은 무리를 지키러 와. 진짜 왕도 똑같아");
        yield return EndStep();
        SetCardBig(false);
    }

    private const string THROW_RECIPE = "meat+poison";   // 독침 육포 - 무방비 때 [F] 로 던지는 독샘 요리

    /// <summary>새끼 발톱 + 랩터 무리 스폰. 돌려주는 값 = 새끼 발톱 (프리팹이 없으면 null)</summary>
    private BossEnemy SpawnPractice(List<Enemy> pack)
    {
        pack.Clear();
        if (WaveManager.Instance == null) return null;
        float spawnDist = TailSpawnDistance();
        List<Enemy> raptors = WaveManager.Instance.SpawnForTutorial("raptor", Mathf.Max(0, GameBalance.BossPracticeRaptors), GameBalance.BossPracticeRaptorMul, 0f, spawnDist);
        if (raptors != null) pack.AddRange(raptors);
        return WaveManager.Instance.SpawnBossForPractice(spawnDist + 2.5f);
    }

    private void DespawnPractice(ref BossEnemy cub)
    {
        if (cub != null)
        {
            if (cub.IsAlive && BossGimmickSystem.Instance != null) BossGimmickSystem.Instance.ClearBossUI();
            Destroy(cub.gameObject);
            cub = null;
        }
    }

    // ── 13. 정차역에서 만나는 것 / 앞길: 실습 없이 카드 2장 (전부 돌리는 런에만) ──
    private IEnumerator Step13_Cards()
    {
        BeginStep(13, "앞으로 알아 둘 것", "카드를 읽어 봐\n[Enter] 다음 장", null, 0, 0);
        HideMarker();
        yield return Brief(BriefingTexts.Tutorial(15));
        yield return Brief(BriefingTexts.Tutorial(16));
        yield return EndStep();
    }

    /// <summary>v2.7: 카드 본문 바꾸기 (한 카드 안에서 할 일이 넘어갈 때)</summary>
    private void SetLines(string lines)
    {
        if (linesText == null) return;
        linesText.text = lines;
        linesText.color = UIFactory.CREAM;
        if (titleText != null) titleText.color = UIFactory.GOLD;
    }

    // ── v2.7: 조리 완료 알림 (FoodStock.OnCooked) 을 요리별로 센다 - 카드 3·5 의 "만들었다" 판정 (재고 수가 아니라 만든 횟수로) ──
    private static readonly Dictionary<string, int> cookedCounts = new Dictionary<string, int>();

    private static void OnCookedForTutorial(string recipeId)
    {
        if (string.IsNullOrEmpty(recipeId)) return;
        int n;
        cookedCounts.TryGetValue(recipeId, out n);
        cookedCounts[recipeId] = n + 1;
    }

    private static int CookedCount(string recipeId)
    {
        int n;
        return cookedCounts.TryGetValue(recipeId, out n) ? n : 0;
    }

    /// <summary>v2.7: 쏘는 요리가 든 가동 중 포탑 하나 (preferred 번 우선) - 감전·과열 카드용. 쏘는 칸이 없으면 아무 가동 칸 (지원 요리 칸도 멈추긴 한다)</summary>
    private static TurretSlot AnyShootingSlot(int preferred)
    {
        TurretSlot s = SlotAt(preferred);
        if (IsShooting(s)) return s;
        if (TurretSlotManager.Instance != null && TurretSlotManager.Instance.slots != null)
            for (int i = 0; i < TurretSlotManager.Instance.slots.Length; i++)
                if (IsShooting(TurretSlotManager.Instance.slots[i])) return TurretSlotManager.Instance.slots[i];
        return AnyActiveSlot(preferred);
    }

    private static bool IsShooting(TurretSlot s)
    {
        return s != null && !s.IsEmpty && !s.isLocked && !s.isBroken && s.Recipe != null
            && s.Recipe.shape != AttackShape.Passive && s.Recipe.shape != AttackShape.Aura;
    }

    private IEnumerator WaitUnscaled(float sec)
    {
        float until = Time.unscaledTime + sec;
        while (Time.unscaledTime < until && (Active || InlineActive)) yield return null;
    }

    /// <summary>슬롯 위 화살표 끝점 높이: 마커 칩(북 = 머리 위 / 남 = 발 아래) 과 안 겹치게 칩 위</summary>
    private static float SlotArrowTip(int slotIndex)
    {
        return GameBalance.IsSouthSlot(slotIndex) ? 0.95f : GameBalance.SlotMarkerYOffset + 0.55f;
    }

    private static int SlotIndexOf(TurretSlot slot)
    {
        if (TurretSlotManager.Instance == null || slot == null) return 0;
        for (int i = 0; i < TurretSlotManager.Instance.slots.Length; i++)
            if (TurretSlotManager.Instance.slots[i] == slot) return i;
        return 0;
    }

    /// <summary>가동 중인 포탑 하나 (preferred 번 슬롯 우선, 없으면 아무거나)</summary>
    private static TurretSlot AnyActiveSlot(int preferred)
    {
        TurretSlot s = SlotAt(preferred);
        if (s != null && !s.IsEmpty && !s.isLocked) return s;
        if (TurretSlotManager.Instance == null) return null;
        for (int i = 0; i < TurretSlotManager.Instance.slots.Length; i++)
        {
            s = TurretSlotManager.Instance.slots[i];
            if (s != null && !s.IsEmpty && !s.isLocked) return s;
        }
        return null;
    }

    // ── 완료 ──
    private IEnumerator Finish()
    {
        HideMarker();
        SetCardBig(false);
        EngineCabUnlocked = false;
        float seconds = Time.unscaledTime - runStartTime;
        int cookFails = CookingBridge.BadsThisRun - badsAtStart;
        if (cardRoot != null) cardRoot.SetActive(false);

        BriefingUI.BriefDef done;
        if (runSegment == 0)
        {
            bool first = !Done;
            PlayerPrefs.SetInt(DONE_KEY, 1);
            if (GameBalance.TutorialSkipsPrologue) PlayerPrefs.SetInt("WDT_PrologueSeen", 1);
            PlayerPrefs.Save();
            if (first && GameBalance.TutorialReward > 0) MetaProgress.AddFame(GameBalance.TutorialReward);

            string perStep = "";
            for (int i = 1; i <= TOTAL_STEPS; i++) perStep += (i > 1 ? " " : "") + i + ":" + Mathf.RoundToInt(stepSeconds[i]);
            Debug.Log("[Tutorial] 완료 " + Mathf.FloorToInt(seconds / 60f) + "분" + Mathf.FloorToInt(seconds % 60f) + "초 | 스킵 " + skips
                + " | 조리 실패 " + cookFails + " | 실전 재시작 " + retries10 + " | 예습 재시작 " + retries12
                + (first ? " | 명성 +" + GameBalance.TutorialReward : " | 재플레이") + " | 카드별 초 " + perStep);
            done = BriefingTexts.TutorialDone(first, GameBalance.TutorialReward, seconds, skips, cookFails);
        }
        else
        {
            Debug.Log("[Tutorial] 구간 " + runSegment + " 끝 " + Mathf.RoundToInt(seconds) + "초 | 스킵 " + skips);
            done = BriefingTexts.TutorialSegmentDone(SEGMENT_TABLE[runSegment - 1].title, seconds, skips > 0);
        }

        bool closed = false, again = false;
        done.onClose = delegate { closed = true; };
        if (runSegment > 0 && GameBalance.TrainingRepeatKey)
        {
            // v2.1: [R] 한 번 더 - 로비를 안 거치고 같은 구간을 다시 (씬 리로드 뒤 자동 시작)
            done.altKey = KeyCode.R;
            done.altHint = "[R] 한 번 더";
            done.onAlt = delegate { again = true; closed = true; };
        }
        BriefingUI.Show(done);
        while (!closed) yield return null;
        if (again) PendingSegment = runSegment;

        Teardown();
        if (GameManager.Instance != null) GameManager.Instance.EndTutorial();
    }

    // ─────────────────────────────────────────────
    // v2.1: 자유 연습 (훈련장 8) - 배그 훈련장처럼. 끝이 없다: 나가기는 [ESC] 메뉴 "그만두기"
    //   손님: SandboxSpawnGap 마다 SandboxSpawnCount 마리 (살아 있는 수가 SandboxMaxAlive 면 쉼), 세 번에 한 번 프테라 1
    //   재료: 6종이 SandboxMaterialFloor 밑으로 내려가면 채움 / 기차: HP 가 SandboxTrainHealBelow 밑이면 가득 (멈춰도 GameManager 가 고친다)
    //   사고: [1] 낙뢰(가동 포탑 감전) [2] 화재 [3] 흘림 [4] 고장 [5] 침입 - KitchenEventManager.ForceEvent / [6] 과열(TurretSlot.ForceOverheat). 파손(과열 방치)도 진짜로 일어난다
    // ─────────────────────────────────────────────
    private IEnumerator RunSandbox()
    {
        yield return null;
        runStartTime = Time.unscaledTime;
        skips = 0; trainStoppedFlag = false;
        godMode = false;
        runSteps.Clear();
        step = 0;

        // 시작 상태: 첫 포탑 + 보급 접시 1 + 재료 + 기관실 개방
        TurretSlot first = SlotAt(0);
        if (first != null && first.IsEmpty && !first.isLocked) first.TryInsertFood(STARTER_RECIPE);
        if (FoodStock.Instance != null && FoodStock.Instance.Get(STARTER_RECIPE) < 1) FoodStock.Instance.Add(STARTER_RECIPE, 1);
        TopUpMaterials();
        EngineCabUnlocked = true;
        if (KitchenEventManager.Instance != null) BlockAmbient = true;   // 무작위 사고는 안 온다 - 키로만

        yield return Brief(BriefingTexts.SandboxIntro());
        ShowCard(0, "자유 연습", SandboxLines(), null, 0, 0, "연습");
        SetFooter("", "");
        UIManager.Instance?.ShowStatChange("[자유 연습] 손님은 끝없이 와. 재료도 기차도 걱정 마");

        float nextSpawn = Time.time + 2f;
        int spawnCount = 0;
        List<Enemy> alive = new List<Enemy>();
        while (true)
        {
            // 손님
            alive.RemoveAll(delegate (Enemy e) { return e == null || !e.IsAlive; });
            if (Time.time >= nextSpawn && alive.Count < Mathf.Max(1, GameBalance.SandboxMaxAlive) && WaveManager.Instance != null)
            {
                nextSpawn = Time.time + Mathf.Max(1f, GameBalance.SandboxSpawnGap);
                spawnCount++;
                float dist = TailSpawnDistance();
                List<Enemy> a = WaveManager.Instance.SpawnForTutorial("raptor", Mathf.Max(1, GameBalance.SandboxSpawnCount), 1f, 0f, dist);
                if (a != null) alive.AddRange(a);
                if (spawnCount % 3 == 0)
                {
                    List<Enemy> b = WaveManager.Instance.SpawnForTutorial("ptera", 1, 1f, 0f, dist + 2.5f);
                    if (b != null) alive.AddRange(b);
                }
            }

            // 재료·기차
            TopUpMaterials();
            if (TrainManager.Instance != null && TrainManager.Instance.HPRatio < GameBalance.SandboxTrainHealBelow)
            {
                TrainManager.Instance.Heal(TrainManager.Instance.currentMaxHP);
                UIManager.Instance?.ShowStatChange("[자유 연습] 기차를 고쳤어. 진짜 운행이었으면 여기서 멈췄을 거야");
            }
            if (trainStoppedFlag)
            {
                trainStoppedFlag = false;
                UIManager.Instance?.ShowStatChange("[자유 연습] 기차가 멈췄어. 여기선 바로 고쳐 줄게");
            }

            // 사고 키 (창이 떠 있거나 조리 중이면 무시)
            if (!BriefingUI.IsOpen && !PauseMenu.IsOpen && !CookingMinigame.IsActive && !KitchenPanel.IsOpenStatic && !WorkshopUI.IsOpen && !AugmentListUI.ReadingOpen)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) SandboxLightning();
                else if (Input.GetKeyDown(KeyCode.Alpha2)) SandboxEvent(2);
                else if (Input.GetKeyDown(KeyCode.Alpha3)) SandboxEvent(3);
                else if (Input.GetKeyDown(KeyCode.Alpha4)) SandboxEvent(4);
                else if (Input.GetKeyDown(KeyCode.Alpha5)) SandboxEvent(5);
                else if (Input.GetKeyDown(KeyCode.Alpha6)) SandboxOverheat();
            }
            if (linesText != null && cardRoot != null && cardRoot.activeSelf) linesText.text = SandboxLines();
            yield return null;
        }
    }

    private static string SandboxLines()
    {
        bool busy = KitchenEventManager.Instance != null && KitchenEventManager.Instance.EventRunning;
        return "[1] 낙뢰   [2] 화재   [3] 재료 흘림\n[4] 기구 고장   [5] 침입   [6] 과열" + (busy ? "   (사고 처리 중)" : "") + "\n[ESC] 메뉴에서 그만두면 로비로";
    }

    /// <summary>6종 재료가 바닥 밑이면 채운다</summary>
    private static void TopUpMaterials()
    {
        if (MaterialInventory.Instance == null) return;
        int floor = Mathf.Max(1, GameBalance.SandboxMaterialFloor);
        foreach (MaterialType t in System.Enum.GetValues(typeof(MaterialType)))
        {
            int have = MaterialInventory.Instance.Get(t);
            if (have < floor) MaterialInventory.Instance.Add(t, floor - have);
        }
    }

    /// <summary>[1] 낙뢰: 가동 중인 포탑 하나를 감전 (협곡 낙뢰와 같은 연출·시간)</summary>
    private static void SandboxLightning()
    {
        if (TurretSlotManager.Instance == null) return;
        List<TurretSlot> cand = new List<TurretSlot>();
        for (int i = 0; i < TurretSlotManager.Instance.slots.Length; i++)
        {
            TurretSlot s = TurretSlotManager.Instance.slots[i];
            if (s != null && !s.IsEmpty && !s.isLocked && !s.IsStunned && !s.isBroken) cand.Add(s);
        }
        if (cand.Count == 0) { UIManager.Instance?.ShowStatChange("[자유 연습] 감전시킬 포탑이 없어. 요리부터 넣어"); return; }
        TurretSlot target = cand[Random.Range(0, cand.Count)];
        LightningFx();
        target.StunSlot(GameBalance.LightningStunSec, "감전");
        UIManager.Instance?.ShowDanger("[낙뢰] 포탑 감전! 옆에서 [E] 한 번. 가만두면 " + Mathf.RoundToInt(GameBalance.LightningStunSec) + "초 동안 못 쏴");
    }

    /// <summary>[6] 과열: 가동 중인 포탑 하나를 지금 과열시킨다 - 식힐 때까지 멈추고, StunBreakSec 방치하면 파손 (파손을 눈으로 보는 길)</summary>
    private static void SandboxOverheat()
    {
        if (TurretSlotManager.Instance == null) return;
        List<TurretSlot> cand = new List<TurretSlot>();
        for (int i = 0; i < TurretSlotManager.Instance.slots.Length; i++)
        {
            TurretSlot s = TurretSlotManager.Instance.slots[i];
            if (s != null && !s.IsEmpty && !s.isLocked && !s.IsStunned && !s.isBroken) cand.Add(s);
        }
        if (cand.Count == 0) { UIManager.Instance?.ShowStatChange("[자유 연습] 과열시킬 포탑이 없어. 요리부터 넣어"); return; }
        cand[Random.Range(0, cand.Count)].ForceOverheat();
        UIManager.Instance?.ShowDanger("[과열] 옆에서 [E]를 누른 채 마우스를 흔들어 식혀. " + Mathf.RoundToInt(GameBalance.StunBreakSec) + "초 넘으면 망가져");
    }

    private static void SandboxEvent(int kind)
    {
        if (KitchenEventManager.Instance == null) { UIManager.Instance?.ShowStatChange("[자유 연습] 여기선 사고를 못 일으켜"); return; }
        if (KitchenEventManager.Instance.EventRunning) { UIManager.Instance?.ShowStatChange("[자유 연습] 이미 사고가 났어. 그것부터 처리해"); return; }
        KitchenEventManager.Instance.ForceEvent(kind);
    }

    // ─────────────────────────────────────────────
    // v2: 정식 운행 인라인 연습 (새 기믹 첫 등장 순간, 그 종류만)
    //   kind 2 = 협곡 낙뢰 (WaveManager 가 감전 직후) / 3 = 첫 바위 (EngineCab 이 바위를 띄운 직후, 웨이브 InlineHarpoonMinWave 부터) / 4 = 레버 (WaveManager 가 InlineLeverWave 시작에)
    //   v2.7: kind 는 옛 구간 번호를 그대로 쓴다 (WaveManager·EngineCab 호출부 불변). 기록은 WDT_Tut_In<kind> - 견습 구간 3(낙뢰) · 5(바위·레버)를 끝냈으면 안 뜬다
    // ─────────────────────────────────────────────
    /// <summary>지금 이 종류의 인라인 연습을 띄울 수 있는가 (연습 기록이 없고, 그 견습 구간을 끝내지 않았고, 전투 중이고, 보스·사고·다른 연습이 없을 때)</summary>
    public static bool WantsInline(int kind)
    {
        if (!GameBalance.InlinePracticeOn || !GameBalance.TutorialRunEnabled) return false;
        if (Instance == null || Active || InlineActive) return false;
        if (kind < 2 || kind > 4 || InlineState(kind) != 0 || SegmentState(InlineSegmentOf(kind)) == 1) return false;
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Battle) return false;
        if (WaveManager.TutorialGateActive) return false;
        if (BossGimmickSystem.Instance != null && BossGimmickSystem.Instance.HasActiveBoss) return false;
        if (KitchenEventManager.IsActive) return false;
        return true;
    }

    /// <summary>인라인 연습 시작. kind = 종류(2 낙뢰 / 3 바위 / 4 레버), slot = 낙뢰에서 감전된 포탑 (null 이면 디렉터가 하나 고른다)</summary>
    public static void PlayInline(int kind, TurretSlot slot)
    {
        if (!WantsInline(kind)) return;
        InlineActive = true;
        InlineFreeze = true;
        Instance.inlineRoutine = Instance.StartCoroutine(Instance.InlineRoutine(kind, slot));
        Debug.Log("[Tutorial] 인라인 연습 시작 - 종류 " + kind);
    }

    private IEnumerator InlineRoutine(int kind, TurretSlot slot)
    {
        yield return null;
        while (BriefingUI.Busy && InlineActive) yield return null;   // 첫 등장 카드(사고 카드 등)가 먼저

        skipRequested = false;
        string title = "", lines = "", notice = "", noticeSub = "", hint = "", doneNotice = "";
        int harpoonMark = EngineCab.HarpoonRetrievals, leverMark = EngineCab.LeverPulls;
        float nextRockAt = 0f;

        if (kind == 2)
        {
            if (slot == null || slot.IsEmpty || slot.isLocked) slot = FindStunnedSlot();
            if (slot == null) slot = AnyActiveSlot(1);
            if (slot == null) { EndInline(kind, false, null); yield break; }   // 포탑이 하나도 없으면 연습이 성립하지 않는다 (기록 없음)
            slot.StunSlot(999f, "감전");                      // 연습 동안은 저절로 안 풀린다
            title = "감전된 포탑 살리기";
            lines = "화살표를 따라 \"감전\" 표시가 뜬 포탑으로 가\n포탑 바로 앞에서 [E]를 한 번 눌러\n포탑이 살아나면 멈췄던 전투가 다시 이어져\n앞으로 낙뢰가 칠 때마다 이렇게 하면 돼";
            notice = "[연습]  첫 낙뢰 - 손님은 잠깐 멈춰 있어"; noticeSub = "멈춘 포탑 옆에서 [E] 한 번";
            hint = "포탑 바로 앞, 벽 쪽까지 붙어서 [E] 한 번. 멀면 안 돼";
            doneNotice = "살렸어. 이제 손님이 다시 와";
            ShowMarkerFollow(slot.transform, SlotArrowTip(SlotIndexOf(slot)), RING_TURRET);
        }
        else if (kind == 3)
        {
            yield return Brief(BriefingTexts.Tutorial(11));   // 왜 낚나 (시간 정지 카드 - 견습 카드 9 와 같은 글) - 그 다음 연습 카드
            if (!InlineActive) yield break;
            title = "작살포로 바위 낚기";
            lines = "화살표를 따라 기관실 작살포 옆으로 가\n창밖 바위가 사거리에 들어오면 [E]\n놓쳐도 바위는 또 와\n낚은 재료는 알아서 기차로 빨려 와";
            notice = "[연습]  첫 바위 - 손님은 잠깐 멈춰 있어"; noticeSub = "기관실 왼쪽 위 작살포에서 [E]";
            hint = "작살포는 기관실 왼쪽 위야. 바위가 사거리 안일 때만 맞아";
            doneNotice = "낚았어. 이제 손님이 다시 와";
            ShowMarkerAt(new Vector3(GameBalance.HarpoonX, 1.7f, 0f), 1.0f, RING_HARPOON);
        }
        else
        {
            yield return Brief(BriefingTexts.Tutorial(12));
            if (!InlineActive) yield break;
            title = "레버로 전속 주행 켜기";
            lines = "화살표를 따라 기관실 레버 옆으로 가\n[E]를 " + GameBalance.LeverHoldSec + "초 꾹 누르면 전속 주행이야\n빨라지면 골드가 늘지만 손님도 더 와\n다시 당기면 원래대로. 연습이 끝나면 되돌려 놓을게";
            notice = "[연습]  레버 - 손님은 잠깐 멈춰 있어"; noticeSub = "레버 옆에서 [E]를 " + GameBalance.LeverHoldSec + "초 꾹 눌러 봐";
            hint = "레버 옆에서 [E]를 " + GameBalance.LeverHoldSec + "초 꾹 눌러. 떼면 취소돼";
            doneNotice = "당겼어. 레버는 원래대로 돌려놨고, 이제 손님이 다시 와";
            ShowMarkerAt(new Vector3(GameBalance.LeverX, 0.35f, 0f), 1.0f, RING_LEVER);
        }

        SetCardBig(true);
        ShowCard(0, title, lines, null, 0, 0, "연습");
        SetFooter(Mathf.RoundToInt(GameBalance.InlineHintSec) + "초가 지나면 힌트가 더 떠", "[Enter] 연습 건너뛰기");
        UIManager.Instance?.ShowWaveNotice(notice, noticeSub);
        freezeScanAt = 0f;

        float hintAt = Time.time + GameBalance.InlineHintSec;
        bool success = false;
        while (InlineActive && !skipRequested)
        {
            if (kind == 2) { if (!slot.IsStunned) { success = true; break; } }
            else if (kind == 3)
            {
                if (EngineCab.HarpoonRetrievals > harpoonMark) { success = true; break; }
                if (EngineCab.RockCount == 0 && Time.time >= nextRockAt) { EngineCab.SpawnRockNow(-13f); nextRockAt = Time.time + 1.5f; }
            }
            else { if (EngineCab.LeverPulls > leverMark) { success = true; break; } }

            if (Time.time > hintAt) { hintAt = float.MaxValue; AddHintLine(hint); UIManager.Instance?.ShowStatChange("[연습] " + hint); }
            yield return null;
        }
        if (!InlineActive) yield break;   // 씬 리로드 등으로 이미 정리됨

        if (success)
        {
            SoundManager.Play("sfx_wave_clear");
            if (titleText != null) { titleText.text = "완료 - " + titleText.text; titleText.color = new Color(0.55f, 0.95f, 0.55f, 1f); }
            if (linesText != null) { linesText.text = "잘했어. 이제 손님이 다시 와"; linesText.color = UIFactory.CREAM; }
            UIManager.Instance?.ShowWaveNotice("", doneNotice);
            HideMarker();
            yield return WaitUnscaled(DONE_BEAT_SEC);
            if (titleText != null) titleText.color = UIFactory.GOLD;
        }
        EndInline(kind, success, slot);
    }

    /// <summary>인라인 연습 정리: 기록(완료 1 / 건너뜀 2 - v2.7: 구간 기록이 아니라 연습 기록), 멈춤 해제, 카드·마커·명판 끄기. 레버 연습은 순항으로 되돌린다</summary>
    private void EndInline(int kind, bool success, TurretSlot slot)
    {
        if (kind == 2 && slot != null && slot.IsStunned) slot.ClearStun();
        if (kind == 4 && EngineCab.FullSteam) EngineCab.ForceCruise();
        if (success) MarkInline(kind, 1);
        else if (skipRequested)
        {
            MarkInline(kind, 2);
            UIManager.Instance?.ShowWaveNotice("", "연습을 건너뛰었어. 로비 [T] 훈련장에서 다시 할 수 있어");
        }
        skipRequested = false;
        HideMarker();
        HideFreezePlates();
        frozenEnemies = new Enemy[0];
        if (cardRoot != null) cardRoot.SetActive(false);
        SetCardBig(false);
        InlineFreeze = false;
        InlineActive = false;
        inlineRoutine = null;
        Debug.Log("[Tutorial] 인라인 연습 끝 - 종류 " + kind + (success ? " 완료" : " 건너뜀/중단"));
    }

    /// <summary>v2.7: 인라인 연습 기록 쓰기 - 이미 완료(1)면 건너뜀(2)으로 내리지 않는다</summary>
    private static void MarkInline(int kind, int state)
    {
        if (InlineState(kind) == 1 && state == 2) return;
        PlayerPrefs.SetInt(INLINE_KEY_PREFIX + kind, state);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] 인라인 연습 " + kind + " 기록 = " + (state == 1 ? "완료" : "건너뜀"));
    }

    private static TurretSlot FindStunnedSlot()
    {
        if (TurretSlotManager.Instance == null) return null;
        for (int i = 0; i < TurretSlotManager.Instance.slots.Length; i++)
        {
            TurretSlot s = TurretSlotManager.Instance.slots[i];
            if (s != null && !s.IsEmpty && !s.isLocked && s.IsStunned && s.StunKind == "감전") return s;
        }
        return null;
    }

    // ─────────────────────────────────────────────
    // 단계 공통
    // ─────────────────────────────────────────────
    private void BeginStep(int n, string title, string lines, string progLabel, int done, int total)
    {
        step = n;
        stepStartTime = Time.time;
        stepStartUnscaled = Time.unscaledTime;
        skipRequested = false;
        ShowCard(n, title, lines, progLabel, done, total, null);
        Debug.Log("[Tutorial] 카드 " + n + " 시작: " + title);
    }

    /// <summary>
    /// 단계 마무리: 건너뛰었으면 기록만, 해냈으면 카드를 "완료" 상태로 바꿔 DONE_BEAT_SEC 동안 보여준 뒤 다음으로.
    /// (해내자마자 다음 카드가 뜨면 "막무가내로 넘어가는" 느낌 - 유저 피드백 09-16)
    /// </summary>
    private IEnumerator EndStep()
    {
        HideMarker();
        if (step >= 1 && step <= TOTAL_STEPS) stepSeconds[step] = Time.unscaledTime - stepStartUnscaled;
        if (skipRequested)
        {
            skips++; segSkips++;
            Debug.Log("[Tutorial] 카드 " + step + " 건너뜀");
            skipRequested = false;
            yield break;
        }
        SoundManager.Play("sfx_wave_clear");
        if (titleText != null) { titleText.text = "완료 - " + titleText.text; titleText.color = new Color(0.55f, 0.95f, 0.55f, 1f); }
        if (linesText != null) { linesText.text = "잘했어. 다음 거 가자"; linesText.color = UIFactory.CREAM; }
        float until = Time.unscaledTime + DONE_BEAT_SEC;
        while (Time.unscaledTime < until && Active) yield return null;
        if (titleText != null) titleText.color = UIFactory.GOLD;
    }

    private const float DONE_BEAT_SEC = 1.2f;

    /// <summary>브리핑 카드를 띄우고 닫힐 때까지 기다린다 (시간 정지 중에도 프레임은 돈다)</summary>
    private IEnumerator Brief(BriefingUI.BriefDef def)
    {
        if (def == null) yield break;
        bool closed = false;
        def.onClose = delegate { closed = true; };
        BriefingUI.Show(def);
        while (!closed && (Active || InlineActive)) yield return null;
        skipRequested = false;    // 카드를 닫은 Enter 가 건너뛰기로 새지 않게 (같은 프레임 가드에 더해 한 번 더)
    }

    private void Spino(string line)
    {
        UIManager.Instance?.ShowWaveNotice("", line);    // 예고 카드는 비우고 안내 카드(초록 줄)만
    }

    /// <summary>카드에 힌트 (30초 규칙: 자동 통과 대신 힌트). 큰 카드 = 본문 아래 한 줄 추가, 작은 카드 = 본문을 힌트로 바꾼다(두 줄 자리라 덧붙이면 핍과 겹친다)</summary>
    private void AddHintLine(string hint)
    {
        if (linesText == null) return;
        string body = linesText.text;
        if (body.Contains(hint)) return;
        linesText.text = cardBig ? body + "\n" + hint : hint;
        linesText.color = cardBig ? UIFactory.CREAM : HINT_GOLD;
        if (titleText != null) titleText.color = HINT_GOLD;
    }

    // ─────────────────────────────────────────────
    // 판정 헬퍼
    // ─────────────────────────────────────────────
    private Transform Chef()
    {
        if (chefTf != null) return chefTf;
        GameObject go = GameObject.Find("Chef");
        if (go != null) { chefTf = go.transform; return chefTf; }
        ChefController cc = FindFirstObjectByType<ChefController>();
        if (cc != null) chefTf = cc.transform;
        return chefTf;
    }

    private static TurretSlot SlotAt(int i)
    {
        if (TurretSlotManager.Instance == null || i < 0 || i >= TurretSlotManager.Instance.slots.Length) return null;
        return TurretSlotManager.Instance.slots[i];
    }

    private static int FilledSlotCount()
    {
        if (TurretSlotManager.Instance == null) return 0;
        int n = 0;
        for (int i = 0; i < TurretSlotManager.Instance.slots.Length; i++)
        {
            TurretSlot s = TurretSlotManager.Instance.slots[i];
            if (s != null && !s.IsEmpty) n++;
        }
        return n;
    }

    private static int MeatCount()
    {
        return MaterialInventory.Instance != null ? MaterialInventory.Instance.Get(MaterialType.Meat) : 0;
    }

    private static int TotalFood()
    {
        if (FoodStock.Instance == null) return 0;
        int n = 0;
        foreach (RecipeData r in RecipeDatabase.All)
            if (r != null) n += FoodStock.Instance.Get(r.recipeId);
        return n;
    }

    private static CookingStation FindStation(CookingStation.StationType type)
    {
        CookingStation[] all = FindObjectsByType<CookingStation>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].stationType == type) return all[i];
        return null;
    }

    private static int CountDead(List<Enemy> list)
    {
        int dead = 0;
        for (int i = 0; i < list.Count; i++)
            if (list[i] == null || !list[i].IsAlive) dead++;
        return dead;
    }

    private static void KillAll(List<Enemy> list)
    {
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null && list[i].IsAlive) Destroy(list[i].gameObject);
        list.Clear();
    }

    // ─────────────────────────────────────────────
    // 목표 카드 (우상단 웨이브 판 아래 - 목업 v2 (A) 좌표). v2: 큰 카드(540x250, 연습·예습) / 작은 카드(330x156, 목표) 한 위젯
    // ─────────────────────────────────────────────
    private const float CARD_W = 330f;
    private const float CARD_H = 156f;
    private const float BIG_W = 540f;
    private const float BIG_H = 250f;
    /// <summary>카드 위 끝 (화면 위에서). 우상단 판(8 ~ 158)의 빈 아래 여백에 30 걸친다 - 더 내리면 큰 카드(250)의 아래 끝이 포탑 칸 B 지붕을 덮는다</summary>
    private const float CARD_TOP = 128f;

    /// <summary>
    /// v2.2: 목표 카드가 떠 있으면 그 아래 끝 (화면 위에서, 캔버스 단위), 없으면 0.
    /// UIManager 의 알림 줄이 읽는다 - 카드가 알림 줄 자리(우상단 판 아래)를 덮어 알림이 카드 뒤에 가려지던 것
    /// </summary>
    public static float CardBottomY
    {
        get
        {
            TutorialDirector d = Instance;
            if (d == null || d.cardRoot == null || !d.cardRoot.activeInHierarchy) return 0f;
            return CARD_TOP + (d.cardBig ? BIG_H : CARD_H);
        }
    }

    /// <summary>
    /// v2.2: 목표 카드가 떠 있으면 카드 왼쪽 끝이 화면 오른쪽 끝에서 얼마나 안쪽인가 (캔버스 단위), 없으면 0.
    /// 가운데 예고 카드(UISkin.NoticeFollower)가 읽는다 - 큰 카드(540)는 가운데 띠까지 들어와 예고 카드와 겹친다
    /// </summary>
    public static float CardLeftFromRight
    {
        get
        {
            TutorialDirector d = Instance;
            if (d == null || d.cardRoot == null || !d.cardRoot.activeInHierarchy) return 0f;
            return 8f + (d.cardBig ? BIG_W : CARD_W);
        }
    }

    private void BuildCard()
    {
        cardCanvas = UIFactory.CreateCanvas("TutorialCard_Canvas", CARD_SORT);
        cardCanvas.transform.SetParent(transform, false);      // 디렉터(DontDestroyOnLoad) 밑 - 씬 리로드에도 남는다
        canvasRect = cardCanvas.GetComponent<RectTransform>();

        RectTransform card = UIFactory.CreatePanel(cardCanvas.transform, "GoalCard",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-8f - CARD_W, -CARD_TOP - CARD_H), new Vector2(-8f, -CARD_TOP),
            UIFactory.PANEL, UIFactory.GOLD, 2f);
        cardRoot = card.gameObject;
        cardRect = card;

        if (UISkin.Available)
            stepPlate = UISkin.Nameplate(card, "Step", "목표  1 / " + TOTAL_STEPS, 16, new Vector2(0f, 1f), new Vector2(26f, 4f));
        else
        {
            stepFallback = UIFactory.CreateText(card, "Step", "목표  1 / " + TOTAL_STEPS, 15, UIFactory.GOLD, TextAnchor.MiddleLeft);
            PlaceTopLeft(stepFallback.rectTransform, 26f, -6f, 200f, 22f);
        }

        titleText = UIFactory.CreateText(card, "Title", "", 18, UIFactory.GOLD, TextAnchor.MiddleLeft);
        linesText = UIFactory.CreateText(card, "Lines", "", 15, UIFactory.CREAM, TextAnchor.UpperLeft);
        linesText.lineSpacing = 1.15f;

        pipSprite = SpriteBank.Get("ui_mg_meat");
        for (int i = 0; i < 8; i++)
        {
            GameObject pg = new GameObject("Pip_" + i);
            pg.transform.SetParent(card, false);
            RectTransform prt = pg.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 1f); prt.anchorMax = new Vector2(0f, 1f); prt.pivot = new Vector2(0f, 1f);
            prt.sizeDelta = new Vector2(16f, 16f);
            Image pi = pg.AddComponent<Image>();
            if (pipSprite != null) pi.sprite = pipSprite;
            pi.color = PIP_OFF; pi.raycastTarget = false;
            pg.SetActive(false);
            pips.Add(pi);
        }

        progressText = UIFactory.CreateText(card, "Progress", "", 15, UIFactory.CREAM, TextAnchor.MiddleLeft);

        footLeftText = UIFactory.CreateText(card, "FootL", "", 12, UIFactory.DIM, TextAnchor.LowerLeft);
        footLeftText.rectTransform.anchorMin = new Vector2(0f, 0f); footLeftText.rectTransform.anchorMax = new Vector2(0f, 0f);
        footLeftText.rectTransform.pivot = new Vector2(0f, 0f);
        footLeftText.rectTransform.anchoredPosition = new Vector2(26f, 8f); footLeftText.rectTransform.sizeDelta = new Vector2(360f, 16f);

        footRightText = UIFactory.CreateText(card, "Footer", "[Enter] 건너뛰기", 12, UIFactory.DIM, TextAnchor.LowerRight);
        footRightText.rectTransform.anchorMin = new Vector2(1f, 0f); footRightText.rectTransform.anchorMax = new Vector2(1f, 0f);
        footRightText.rectTransform.pivot = new Vector2(1f, 0f);
        footRightText.rectTransform.anchoredPosition = new Vector2(-16f, 8f); footRightText.rectTransform.sizeDelta = new Vector2(200f, 16f);

        SetCardBig(false);
        cardRoot.SetActive(false);
    }

    /// <summary>카드 크기 전환 - 작은 목표 카드(330x156) / 큰 연습·예습 카드(540x250, 제목 22 / 본문 17 / 최대 5줄 + 힌트)</summary>
    private void SetCardBig(bool big)
    {
        cardBig = big;
        lastProgDone = -1;   // 핍·진행 글 자리가 바뀐다 - 다음 SetProgress 가 다시 그린다
        if (cardRect == null) return;
        float w = big ? BIG_W : CARD_W, h = big ? BIG_H : CARD_H;
        cardRect.offsetMin = new Vector2(-8f - w, -CARD_TOP - h);
        cardRect.offsetMax = new Vector2(-8f, -CARD_TOP);
        titleText.fontSize = big ? 22 : 18;
        PlaceTopLeft(titleText.rectTransform, 26f, big ? -30f : -24f, w - 52f, big ? 30f : 26f);
        linesText.fontSize = big ? 17 : 15;
        PlaceTopLeft(linesText.rectTransform, 26f, big ? -70f : -56f, w - 52f, big ? 130f : 48f);
        linesText.lineSpacing = big ? 1.2f : 1.15f;
        float pipY = big ? -(h - 44f) : -102f;
        for (int i = 0; i < pips.Count; i++)
            pips[i].rectTransform.anchoredPosition = new Vector2(26f + i * 22f, pipY);
        PlaceTopLeft(progressText.rectTransform, 26f, pipY + 2f, 300f, 20f);
        progressText.fontSize = big ? 16 : 15;
        footLeftText.fontSize = big ? 14 : 12;
        footRightText.fontSize = big ? 14 : 12;
        footLeftText.gameObject.SetActive(big);
        if (!big) { footLeftText.text = ""; footRightText.text = "[Enter] 건너뛰기"; }
    }

    private void SetFooter(string left, string right)
    {
        if (footLeftText != null) footLeftText.text = left ?? "";
        if (footRightText != null) footRightText.text = right ?? "[Enter] 건너뛰기";
    }

    private int progTotal = 0;
    private string progLabel = "";

    /// <summary>카드 표시. n = 단계 번호(0 이면 명판은 chip 글자만), chip = 명판 글자를 직접 줄 때 ("연습")</summary>
    private void ShowCard(int n, string title, string lines, string label, int done, int total, string chip)
    {
        if (cardRoot == null) return;
        cardRoot.SetActive(true);
        string plate = chip != null ? chip : "목표  " + StepPos(n) + " / " + runSteps.Count;
        if (stepPlate != null) UISkin.Relabel(stepPlate, plate, 16);
        if (stepFallback != null) stepFallback.text = plate;
        titleText.text = title;
        titleText.color = UIFactory.GOLD;
        linesText.text = lines;
        linesText.color = UIFactory.CREAM;
        if (!cardBig) SetFooter("", "[Enter] 건너뛰기");
        progLabel = label ?? "";
        progTotal = Mathf.Clamp(total, 0, pips.Count);
        lastProgDone = -1;
        SetProgress(done, total);
    }

    private int lastProgDone = -1, lastProgTotal = -1;   // v2.7: SetProgress 가 마지막으로 그린 값 (매 프레임 불러도 바뀔 때만 그린다)
    private string lastProgLabel = null;

    private void SetProgress(int done, int total)
    {
        if (done == lastProgDone && total == lastProgTotal && progLabel == lastProgLabel) return;
        lastProgDone = done; lastProgTotal = total; lastProgLabel = progLabel;
        progTotal = Mathf.Clamp(total, 0, pips.Count);
        for (int i = 0; i < pips.Count; i++)
        {
            bool show = i < progTotal;
            if (pips[i].gameObject.activeSelf != show) pips[i].gameObject.SetActive(show);
            if (show) pips[i].color = i < done ? PIP_ON : PIP_OFF;
        }
        if (progTotal > 0)
        {
            progressText.text = progLabel + "  " + Mathf.Clamp(done, 0, progTotal) + " / " + progTotal;
            float pipY = pips.Count > 0 ? pips[0].rectTransform.anchoredPosition.y : -100f;
            progressText.rectTransform.anchoredPosition = new Vector2(26f + progTotal * 22f + 8f, pipY + 2f);
        }
        else progressText.text = "";
    }

    private static void PlaceTopLeft(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    // ─────────────────────────────────────────────
    // v2: "멈춤" 명판 - 인라인 연습 동안 살아 있는 손님 머리 위 (화면 캔버스, 0.25초마다 손님 목록 갱신)
    // ─────────────────────────────────────────────
    private void TickFreezePlates()
    {
        if (cardCanvas == null || canvasRect == null) return;
        if (Time.unscaledTime >= freezeScanAt)
        {
            freezeScanAt = Time.unscaledTime + 0.25f;
            frozenEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        }
        Camera cam = Camera.main;
        int used = 0;
        if (cam != null)
        {
            for (int i = 0; i < frozenEnemies.Length && used < 16; i++)
            {
                Enemy e = frozenEnemies[i];
                if (e == null || !e.IsAlive) continue;
                Vector3 sp = cam.WorldToScreenPoint(e.transform.position + Vector3.up * 1.25f);
                if (sp.z < 0f) continue;
                Vector2 local;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out local)) continue;
                RectTransform plate = FreezePlate(used);
                plate.gameObject.SetActive(true);
                plate.anchoredPosition = local;
                used++;
            }
        }
        for (int i = used; i < freezePlates.Count; i++)
            if (freezePlates[i] != null && freezePlates[i].gameObject.activeSelf) freezePlates[i].gameObject.SetActive(false);
    }

    private RectTransform FreezePlate(int i)
    {
        while (freezePlates.Count <= i)
        {
            RectTransform rt;
            if (UISkin.Available)
                rt = UISkin.Nameplate(cardCanvas.transform, "Freeze_" + freezePlates.Count, "멈춤", 13, new Vector2(0.5f, 0.5f), Vector2.zero, 56f);
            else
            {
                rt = UIFactory.CreatePanel(cardCanvas.transform, "Freeze_" + freezePlates.Count,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 24f), UIFactory.PANEL, UIFactory.GOLD, 1f);
                UIFactory.CreateText(rt, "Label", "멈춤", 13, UIFactory.GOLD, TextAnchor.MiddleCenter);
            }
            rt.pivot = new Vector2(0.5f, 0f);
            rt.gameObject.SetActive(false);
            freezePlates.Add(rt);
        }
        return freezePlates[i];
    }

    private void HideFreezePlates()
    {
        for (int i = 0; i < freezePlates.Count; i++)
            if (freezePlates[i] != null) freezePlates[i].gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // 현장 마커 (월드 스프라이트 2개: 발밑 링 + 위 화살표) - 목업 v2 (B) + v3 링 규칙 (오브젝트 발 자리, 오브젝트보다 넓게, 밑에 깔림)
    // ─────────────────────────────────────────────
    private void EnsureMarker()
    {
        if (markerRoot != null) return;
        if (arrowSprite == null) arrowSprite = SpriteBank.Get("tut_arrow") ?? PaintFallbackArrow();
        if (ringSprite == null) ringSprite = SpriteBank.Get("tut_ring") ?? PaintFallbackRing(36, 14);
        if (ringLargeSprite == null) ringLargeSprite = SpriteBank.Get("tut_ring_l") ?? PaintFallbackRing(72, 26);

        markerRoot = new GameObject("TutorialMarker");
        ringSr = PixelPainter.Attach(markerRoot.transform, "Ring", ringSprite, Vector3.zero, RING_FLOOR.order);
        SpriteRenderer arrow = PixelPainter.Attach(markerRoot.transform, "Arrow", arrowSprite, Vector3.zero, ARROW_ORDER);
        ringTf = ringSr.transform; arrowTf = arrow.transform;
    }

    /// <summary>고정 위치 위에 마커</summary>
    private void ShowMarkerAt(Vector3 pos, float tipOffset, RingStyle ring)
    {
        EnsureMarker();
        followTarget = null; fixedTarget = pos; arrowTipOffset = tipOffset;
        ApplyRing(ring);
        markerOn = true;
        markerRoot.SetActive(true);
        TickMarker();
    }

    /// <summary>오브젝트(슬롯/조리대)를 따라다니는 마커</summary>
    private void ShowMarkerFollow(Transform t, float tipOffset, RingStyle ring)
    {
        EnsureMarker();
        followTarget = t; arrowTipOffset = tipOffset;
        ApplyRing(ring);
        markerOn = true;
        markerRoot.SetActive(true);
        TickMarker();
    }

    /// <summary>링 스타일 적용: 스프라이트(작은/큰), 정렬, 배율. 위치·z 는 TickMarker 가 매 프레임</summary>
    private void ApplyRing(RingStyle style)
    {
        ringStyle = style;
        if (ringSr == null) return;
        ringSr.sprite = style.large ? ringLargeSprite : ringSprite;
        ringSr.sortingOrder = style.order;
        ringTf.localScale = new Vector3(style.scale, style.scale, 1f);
        ringTf.gameObject.SetActive(true);
    }

    private void HideMarker()
    {
        markerOn = false;
        if (markerRoot != null) markerRoot.SetActive(false);
    }

    private void TickMarker()
    {
        if (!markerOn || markerRoot == null) return;
        Vector3 pos = followTarget != null ? followTarget.position : fixedTarget;
        pos.z = 0f;
        // 까딱: 0.6초 왕복, 4px(0.125u) - 시간 정지 중에도 움직인다 (unscaled)
        float t = Mathf.Repeat(Time.unscaledTime, GameBalance.TutorialMarkerBobSec) / GameBalance.TutorialMarkerBobSec;
        float bob = GameBalance.TutorialMarkerBob * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f));
        // 링 = 오브젝트 발 자리 (dy), 같은 정렬값 안의 앞뒤는 z 로 (z+ = 뒤 = 먼저 그려진다)
        if (ringTf != null) ringTf.position = new Vector3(pos.x, pos.y + ringStyle.dy, ringStyle.z);
        if (arrowTf != null) arrowTf.position = pos + Vector3.up * (arrowTipOffset + bob);
    }

    /// <summary>PNG 없을 때: 구리색 아래 화살표 24x21 (끝점 피벗) - 몸통 8x7 + 머리(폭 22 -> 1) + 검정 1px 외곽</summary>
    private static Sprite PaintFallbackArrow()
    {
        PixelPainter p = new PixelPainter(24, 21);
        Color32 cu = new Color32(184, 112, 52, 255), blk = new Color32(16, 14, 20, 255), hi = new Color32(232, 168, 96, 255);
        p.Rect(8, 1, 15, 7, cu);                            // 몸통 (1~7행, x 8~15)
        p.Rect(8, 1, 15, 1, hi);                            // 윗줄 하이라이트
        p.Rect(7, 0, 16, 0, blk);                           // 위 외곽
        for (int y = 1; y <= 7; y++) { p.Point(7, y, blk); p.Point(16, y, blk); }
        p.Rect(0, 7, 6, 7, blk); p.Rect(17, 7, 23, 7, blk); // 어깨 위 외곽
        for (int y = 8; y <= 18; y++)                       // 머리: 8행 폭 22 (x 1~22) -> 18행 폭 2
        {
            int half = 11 - (y - 8);
            p.Rect(12 - half, y, 11 + half, y, cu);
            p.Point(11 - half, y, blk); p.Point(12 + half, y, blk);
        }
        p.Point(11, 19, cu); p.Point(10, 19, blk); p.Point(12, 19, blk);   // 끝
        p.Point(11, 20, blk);                                             // 끝 아래 외곽 (피벗 줄)
        return p.Bake(32f, 11.5f, 21f);
    }

    /// <summary>PNG 없을 때: 황동 타원 링 (중앙 피벗) - 36x14 작은 링 / 72x26 큰 링</summary>
    private static Sprite PaintFallbackRing(int w, int h)
    {
        PixelPainter p = new PixelPainter(w, h);
        Color32 clear = new Color32(0, 0, 0, 0), brass = new Color32(214, 170, 72, 255), blk = new Color32(16, 14, 20, 200);
        int band = w >= 60 ? 3 : 2;
        p.Ellipse(0, 0, w - 1, h - 1, clear, blk);
        for (int i = 1; i <= band; i++) p.Ellipse(i, i, w - 1 - i, h - 1 - i, clear, brass);
        p.Ellipse(band + 1, band + 1, w - 2 - band, h - 2 - band, clear, blk);
        return p.Bake(32f, w * 0.5f, h * 0.5f);
    }
}
