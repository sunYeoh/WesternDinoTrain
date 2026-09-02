using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [StationStop.cs] v1 - 정차역 라이트 (감사 3-C: "마을에 들렀다는 감각이 없다")
///
/// 웨이브 사이의 마을(Town)을 '간이역 정차'로 연출한다. 새 시스템 없이
/// 이미 있는 것들(배경 스크롤 / 기적 / 배너 / 정비소 / 행상인)을 정차 리듬으로 묶는다.
/// - 전투가 아니면 기차는 서 있다: 배경 스크롤 0
///   (로비 = 차고 / 마을 = 간이역 / 게임오버 = 멈춘 기차 / 승리 = 종착역)
/// - Town 진입: 제동 덜컹 + 기적 + "[역 이름] 정차!" 배너 (지역별 간이역 이름 로테이션)
/// - Town -> Battle: 기적 + "[출발]" 알림 + 배경 재가속 (전속 레버 상태 반영)
/// - 승리의 "종착역 도착!"(GameManager)과 세계관 연결: 간이역들을 지나 종착역으로
///
/// 사용법: 없음! 파일만 넣으면 자동 생성된다. (GameBalance.StationStopEnabled로 끄기)
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class StationStop : MonoBehaviour
{
    private static StationStop instance;

    private GameManager.GameState lastState = GameManager.GameState.Lobby;
    private int stopCount = 0;              // 런 내 몇 번째 정차인지 (역 이름 로테이션)
    private static bool storyShown = false; // 첫 정차 스토리 1회 (앱 세션당)

    // 지역별 간이역 이름 (황야 노선 - '종착역'은 승리 전용이라 여기 없다)
    private static readonly string[] REGION1_STATIONS = { "녹슨 물탱크 역", "선인장 그늘 역", "방울뱀 갈림목" };
    private static readonly string[] REGION2_STATIONS = { "뇌운 고개 역", "피뢰침 망루 역", "스파크 협곡 역" };
    private static readonly string[] REGION3_STATIONS = { "서리이빨 역", "얼어붙은 갱도 역", "푸른 수정 역" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("StationStop");
        DontDestroyOnLoad(go);
        go.AddComponent<StationStop>();
        // 씬 리로드(런 재시작)마다 정차 카운트/상태 추적을 처음부터
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (instance != null)
        {
            instance.lastState = GameManager.GameState.Lobby;
            instance.stopCount = 0;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    // ─────────────────────────────────────────────
    // 상태 전이 감지 (정차 / 출발)
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (!GameBalance.StationStopEnabled) return;
        if (GameManager.Instance == null) return;

        GameManager.GameState s = GameManager.Instance.currentState;
        if (s == lastState) return;

        if (s == GameManager.GameState.Town)
            OnArrive();
        else if (s == GameManager.GameState.Battle)
            OnDepart(lastState);

        lastState = s;
    }

    /// <summary>
    /// 전투가 아니면 기차는 서 있다.
    /// (마을에서 레버를 미리 당겨두는 건 유효 전략 - 토글이 배경 배율을 덮어써도
    ///  매 프레임 0으로 되돌려서 '서 있는 기차'를 유지한다. 출발하면 그 속도로 달린다)
    /// </summary>
    private void LateUpdate()
    {
        if (!GameBalance.StationStopEnabled) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.currentState != GameManager.GameState.Battle)
            ParallaxBackground.SetSpeedMultiplier(0f);
    }

    // ─────────────────────────────────────────────
    // 정차 (Town 진입)
    // ─────────────────────────────────────────────
    private void OnArrive()
    {
        stopCount++;

        SoundManager.Play("sfx_train_whistle");   // 클립 없으면 무시
        GameFeel.Shake(0.18f, "station", 0.8f);   // 제동 덜컹

        UIManager.Instance?.ShowWaveNotice("[" + PickStationName() + "] 정차!",
            "정비 시간이다 - [G] 정비소를 열고, 선로를 골라 출발하라!");

        if (!storyShown)
        {
            storyShown = true;
            UIManager.Instance?.ShowStatChange("황야의 간이역 - 잠깐의 평화도 메뉴에 있다.");
        }
    }

    // ─────────────────────────────────────────────
    // 출발 (Battle 진입)
    // ─────────────────────────────────────────────
    private void OnDepart(GameManager.GameState from)
    {
        // 배경 재가속 - 전속 레버가 켜져 있으면 그 속도 그대로
        ParallaxBackground.SetSpeedMultiplier(
            EngineCab.FullSteam ? GameBalance.LeverParallaxMul : 1f);

        SoundManager.Play("sfx_train_whistle");

        // 첫 출발(로비 -> 전투)은 오프닝 연출이 담당 - 마을 출발에만 한 줄
        if (from == GameManager.GameState.Town)
            UIManager.Instance?.ShowStatChange("[출발] 바퀴가 다시 구른다 - 다음 손님들을 마중 나가자!");
    }

    /// <summary>현재 지역의 간이역 이름 (정차 횟수로 로테이션)</summary>
    private string PickStationName()
    {
        int wave = GameManager.Instance != null ? GameManager.Instance.currentWave : 1;
        int region = GameBalance.RegionOf(wave);

        string[] pool;
        if (region <= 1) pool = REGION1_STATIONS;
        else if (region == 2) pool = REGION2_STATIONS;
        else pool = REGION3_STATIONS;

        int idx = (stopCount - 1) % pool.Length;
        if (idx < 0) idx = 0;
        return pool[idx];
    }
}
