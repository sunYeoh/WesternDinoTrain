using UnityEngine;

/// [HardMode.cs] v1 (신규, v9.28 2026-10-20 하드 모드 = 2회차. 4차 피드백 3번 "1회차를 클리어하면 하드 모드" - 마감 전이라 값만 바꾼 2회차 (새 손님·보스·재료 없음))
/// - 해금: 종착역에 한 번 닿으면 (MetaProgress.ClearCount, 옛 저장은 최고 기록이 최종전이면). 로비 출발 버튼 오른쪽 [2] 칩으로 켜고 끈다 - 고른 것은 PlayerPrefs 에 남는다 (Picked)
/// - 운행: GameManager 가 첫 웨이브에 BeginRun (Picked 를 이번 운행으로 굳힌다 + 기차 시작 HP), 로비로 돌아오면 EndRun. 견습 운행·훈련장엔 안 걸린다 (Active 가 TutorialDirector.Active 를 본다)
/// - 값: GameBalance 맨 아래 v9.28 섹션 (Hard*). 쓰는 쪽은 x HardMode.Mul(GameBalance.Hard...) - 하드가 아니면 1
public static class HardMode
{
    private const string PICK_KEY = "WDT_HardPick";       // 로비에서 고른 상태 (해금 전엔 무시)
    private const string NOTICE_KEY = "WDT_HardNoticed";  // 해금 알림을 한 번 띄웠나
    private static bool active = false;

    /// <summary>해금됐나 - 종착역 1회 (옛 저장: 최고 기록이 최종전 웨이브면 그때 닿은 것)</summary>
    public static bool Unlocked
    {
        get { return GameBalance.HardModeOn && (MetaProgress.ClearCount > 0 || MetaProgress.BestWave >= GameBalance.FinalWave); }
    }

    /// <summary>로비에서 켜 둔 상태 (해금 전엔 false)</summary>
    public static bool Picked
    {
        get { return Unlocked && PlayerPrefs.GetInt(PICK_KEY, 0) == 1; }
        set { PlayerPrefs.SetInt(PICK_KEY, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    /// <summary>이번 운행이 하드인가 (견습 운행·훈련장·자유 연습은 아니다 - 그쪽은 GameManager 가 BeginRun 을 안 부르지만 한 번 더 막는다)</summary>
    public static bool Active { get { return active && !TutorialDirector.Active && !TutorialDirector.SandboxActive; } }

    /// <summary>마지막으로 시작한 운행이 하드였나 - 운행이 끝나 로비로 온 뒤의 결과 요약용 (Active 는 그때 이미 내려가 있다)</summary>
    public static bool LastRunHard { get; private set; }

    /// <summary>해금 알림을 아직 안 띄웠나 (LobbyUI 가 로비에 들어올 때 한 번)</summary>
    public static bool NoticePending { get { return Unlocked && PlayerPrefs.GetInt(NOTICE_KEY, 0) == 0; } }
    public static void MarkNoticed() { PlayerPrefs.SetInt(NOTICE_KEY, 1); PlayerPrefs.Save(); }

    /// <summary>메타 리셋 (MetaProgress.ResetAll) - 고른 상태·알림 기록도 같이</summary>
    public static void ResetPick() { PlayerPrefs.DeleteKey(PICK_KEY); PlayerPrefs.DeleteKey(NOTICE_KEY); }

    /// <summary>운행 시작 (GameManager.HandleBattlePhase 첫 웨이브): 고른 것을 굳히고 기차 시작 HP 에 배율</summary>
    public static void BeginRun()
    {
        active = Picked;
        LastRunHard = active;
        if (!active) return;
        if (TrainManager.Instance != null) TrainManager.Instance.ApplyStartHpMul(GameBalance.HardTrainHpMul);
        else Debug.LogWarning("[HardMode] TrainManager 가 없어 기차 시작 HP 배율을 못 걸었다");
        UIManager.Instance?.ShowStatChange("[하드 모드] 손님이 세고 수급이 적다. 명성은 " + GameBalance.HardFameMul + "배");
        Debug.Log("[HardMode] 하드 모드 운행 시작");
    }

    /// <summary>로비로 돌아왔다 (GameManager.HandleLobby)</summary>
    public static void EndRun() { active = false; }

    /// <summary>하드면 hard, 아니면 1 - 배율을 곱하는 자리마다</summary>
    public static float Mul(float hard) { return Active ? hard : 1f; }
}
