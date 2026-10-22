using UnityEngine;

/// <summary>
/// [MapRoute.cs] v1 (신규, v9.32 2026-10-22 노선 - 유저 10-22 "보스 순서도 최종 보스 빼고 무작위로 / 맵별로 나오는 기본 손님도 다 다르게 / 스테이지 진행별로 지금보다 조금 더 강하게, 순차적으로")
/// - 맵 셋 = 1 구리 사막(녹슨 발톱) · 2 테슬라 협곡(천둥 둥지) · 3 코발트 광산(동면자). 4 = 황야의 끝(디 오리지널) - 최종전은 늘 마지막
/// - "지역 n" 은 자리다 (1·2·3 = 웨이브 1~8 / 9~16 / 17~24 - GameBalance.RegionOf). 그 자리에 어느 맵이 오는지가 노선이고, 운행 시작에 NewRun 이 정한다
///     자리를 따르는 것 = 세기 (지역 곡선 · 조리 난이도 · 급식 인분 · 정비소 값 · 보스 HP 배율 · 맵 손님 세기 맞추기)
///     맵을 따르는 것   = 손님 구성 · 보스 종류 · 배경 그림과 색 · 지역 카드와 문구 · 정차역 이름 · 협곡의 낙뢰
/// - 지금 순서(사막 ▶ 협곡 ▶ 광산) 그대로 가는 경우: GameBalance.MapShuffleOn false / 첫 보스를 한 번도 못 잡은 저장(최고 기록 < RegionLength) /
///   프롤로그를 볼 운행 / 견습 운행·훈련장 (MapOfWave 가 지금 순서로 읽는다 - 웨이브가 없어 늘 사막)
/// - 시험용: GameBalance.MapRouteForce = "312" 처럼 쓰면 그 순서로 고정 (빌드 전에 비울 것)
/// 씬 배치 없음 - 정적 클래스. GameManager 가 운행 시작에 NewRun, 로비에 ResetToDefault 를 부른다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public static class MapRoute
{
    public const int Desert = 1, Canyon = 2, Mine = 3, Final = 4;

    private const string LAST_KEY = "WDT_LastRoute";       // 지난 운행의 노선 (같은 순서가 두 번 연달아 나오지 않게)
    private static readonly int[] order = { Desert, Canyon, Mine };

    /// <summary>이번 운행의 노선을 섞었나 (시험용 고정 포함)</summary>
    public static bool Shuffled { get; private set; }

    /// <summary>첫 보스를 아직 못 잡아서 지금 순서로 가는 운행인가 (오프닝 문구가 "잡으면 바뀐다" 를 알려 준다)</summary>
    public static bool LockedUntilFirstBoss { get; private set; }

    // ─────────────────────────────────────────────
    // 운행 시작 / 로비
    // ─────────────────────────────────────────────
    /// <summary>운행 시작 (GameManager 의 첫 웨이브 보급 블록, StartWave(1) 이 구성표를 다시 만들기 전). prologuePending = 이번 운행에 프롤로그가 뜬다</summary>
    public static void NewRun(bool prologuePending)
    {
        ResetToDefault();
        string why = Decide(prologuePending);
        // 이번 운행의 순서를 남긴다 - 섞지 않은 운행도 (첫 섞기가 바로 앞 운행의 지금 순서를 또 뽑으면 "철길이 또 바뀌었어" 가 거짓말이 된다)
        PlayerPrefs.SetString(LAST_KEY, Code());
        PlayerPrefs.Save();
        Debug.Log("[MapRoute] 노선 " + RouteText() + " (" + why + ")");
    }

    /// <summary>이번 운행의 순서를 정한다 (order · Shuffled · LockedUntilFirstBoss). 돌려주는 값 = 로그에 남길 까닭</summary>
    private static string Decide(bool prologuePending)
    {
        string force = GameBalance.MapRouteForce;
        if (!string.IsNullOrEmpty(force))
        {
            if (TryApply(force)) { Shuffled = true; return "시험용 고정 MapRouteForce"; }
            Debug.LogWarning("[MapRoute] MapRouteForce \"" + force + "\" 를 못 읽었다 - 1·2·3 을 한 번씩 쓴 세 자리 (예: 312)");
        }
        if (!GameBalance.MapShuffleOn) return "섞기 꺼짐";
        if (prologuePending) return "프롤로그 운행 - 지금 순서";
        if (GameBalance.MapShuffleAfterFirstBoss && MetaProgress.BestWave < GameBalance.RegionLength)
        {
            LockedUntilFirstBoss = true;
            return "첫 보스 전 - 지금 순서";
        }

        // 섞는다. 지난 운행과 같은 순서면 다시 (네 번까지)
        string last = PlayerPrefs.GetString(LAST_KEY, "");
        for (int tryNo = 0; tryNo < 4; tryNo++)
        {
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int t = order[i]; order[i] = order[j]; order[j] = t;
            }
            if (Code() != last) break;
        }
        Shuffled = true;
        return "무작위";
    }

    /// <summary>지금 순서로 되돌린다 (로비 - 견습·훈련장·로비 배경은 사막)</summary>
    public static void ResetToDefault()
    {
        order[0] = Desert; order[1] = Canyon; order[2] = Mine;
        Shuffled = false;
        LockedUntilFirstBoss = false;
    }

    // ─────────────────────────────────────────────
    // 자리 ↔ 맵
    // ─────────────────────────────────────────────
    /// <summary>자리(지역 1·2·3, 4 = 최종전)에 놓인 맵</summary>
    public static int MapAtStage(int stage)
    {
        if (stage >= 4) return Final;
        return order[Mathf.Clamp(stage, 1, 3) - 1];
    }

    /// <summary>이 웨이브의 맵. 견습 운행·훈련장은 지금 순서 (자리 번호 = 맵 번호 - 지난 운행의 노선이 남아 있어도)</summary>
    public static int MapOfWave(int wave)
    {
        int stage = GameBalance.RegionOf(wave);
        if (TutorialDirector.Active || TutorialDirector.SandboxActive) return Mathf.Clamp(stage, Desert, Final);
        return MapAtStage(stage);
    }

    /// <summary>이 맵의 보스</summary>
    public static BossEnemy.BossKind BossOf(int map)
    {
        switch (map)
        {
            case Desert: return BossEnemy.BossKind.RustClaw;
            case Canyon: return BossEnemy.BossKind.ThunderNest;
            case Mine: return BossEnemy.BossKind.Hibernator;
            default: return BossEnemy.BossKind.Original;
        }
    }

    // ─────────────────────────────────────────────
    // 이름·색·글 (맵을 따르는 것)
    // ─────────────────────────────────────────────
    /// <summary>"구리 사막" / "테슬라 협곡" / "코발트 광산" / "황야의 끝"</summary>
    public static string Name(int map)
    {
        switch (map)
        {
            case Desert: return "구리 사막";
            case Canyon: return "테슬라 협곡";
            case Mine: return "코발트 광산";
            default: return "황야의 끝";
        }
    }

    /// <summary>노선 줄에 쓰는 짧은 이름 - "사막" / "협곡" / "광산" / "종착역"</summary>
    public static string Short(int map)
    {
        switch (map)
        {
            case Desert: return "사막";
            case Canyon: return "협곡";
            case Mine: return "광산";
            default: return "종착역";
        }
    }

    /// <summary>그림 파일 이름 조각 - ground_desert.png · deco_canyon_1.png 처럼 (Resources/Sprites/WDT)</summary>
    public static string Key(int map)
    {
        switch (map)
        {
            case Desert: return "desert";
            case Canyon: return "canyon";
            case Mine: return "mine";
            default: return "final";
        }
    }

    /// <summary>"협곡 ▶ 광산 ▶ 사막 ▶ 종착역"</summary>
    public static string RouteText()
    {
        return Short(order[0]) + " ▶ " + Short(order[1]) + " ▶ " + Short(order[2]) + " ▶ " + Short(Final);
    }

    /// <summary>오프닝에 붙는 스피노 한 줄 (빈 문자열 = 안 붙인다). 처음 운행(프롤로그)은 오프닝 3줄만</summary>
    public static string OpeningLine()
    {
        if (Shuffled) return "오늘 노선은 " + RouteText() + ". 철길이 또 바뀌었어.";
        if (LockedUntilFirstBoss) return "노선은 " + RouteText() + ". 녹슨 발톱을 한 번 잡으면 철길이 매번 바뀌어.";
        return "";
    }

    /// <summary>지역 전환 때 카메라 배경색 (ParallaxBackground 가 땅에 곱 틴트로 배게 한다 - 맵 그림이 있으면 땅은 제 색)</summary>
    public static Color BgColor(int map)
    {
        switch (map)
        {
            case Desert: return new Color(0.24f, 0.16f, 0.10f);   // 구리빛 사막
            case Canyon: return new Color(0.12f, 0.13f, 0.22f);   // 번개 낀 협곡의 밤
            case Mine: return new Color(0.08f, 0.15f, 0.20f);     // 코발트 광산의 냉기
            default: return new Color(0.15f, 0.06f, 0.06f);       // 황야의 끝
        }
    }

    /// <summary>지역 전환 띠의 둘째 줄 (짧은 내레이션 - 순서와 상관없는 말로)</summary>
    public static string TransitionLine(int map)
    {
        switch (map)
        {
            case Desert: return "녹슨 모래는 굶주림을 기억한다.";
            case Canyon: return "번개가 둥지를 트는 곳. 하늘을 조심해.";
            case Mine: return "대붕괴가 가장 깊이 남은 곳. 얼음 밑에서 뭔가 자고 있다.";
            default: return "대륙에서 가장 오래 굶은 손님이 기다린다.";
        }
    }

    /// <summary>먼지 퍼프 색 (DustFX - 모래 먼지 그림에 곱한다)</summary>
    public static Color DustTint(int map)
    {
        switch (map)
        {
            case Canyon: return new Color(0.72f, 0.74f, 0.9f);
            case Mine: return new Color(0.86f, 0.95f, 1f);
            case Final: return new Color(0.78f, 0.62f, 0.6f);
            default: return Color.white;
        }
    }

    // ─────────────────────────────────────────────
    // 내부
    // ─────────────────────────────────────────────
    private static string Code()
    {
        return order[0].ToString() + order[1] + order[2];
    }

    /// <summary>"312" 같은 세 자리를 노선으로 (1·2·3 을 한 번씩). 못 읽으면 false 이고 아무것도 안 바꾼다</summary>
    private static bool TryApply(string code)
    {
        if (code == null || code.Length != 3) return false;
        int[] got = new int[3];
        bool[] seen = new bool[4];
        for (int i = 0; i < 3; i++)
        {
            int m = code[i] - '0';
            if (m < 1 || m > 3 || seen[m]) return false;
            seen[m] = true; got[i] = m;
        }
        for (int i = 0; i < 3; i++) order[i] = got[i];
        return true;
    }
}
