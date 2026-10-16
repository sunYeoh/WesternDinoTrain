using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [SoundManager.cs] v2.3 (v9.24 2026-10-15 클라이맥스 층 B3: bgm_main 을 틀 때 bgm_climax · bgm_climax_lite 를 같은 오디오 시계 순간에 소리 0 으로 같이 틀어 두고(PlayScheduled) LayerIn 이 박 머리에 소리만 올린다 - 박이 어긋날 수가 없다. 층 길이가 bgm_main 의 정수분의 1 이 아니면(생성 도구로 뽑은 독립 곡) 클라이맥스 층만 단독 곡으로 - 켜면 배경음을 소리 0 으로 두고 그 곡을 처음부터. SecondsToNextBeat = 다음 박까지 남은 시간 (GameBalance.BgmBpm) / 새 키 sfx_fork_clank (갈림길 덜컹, 부모 sfx_lever) · sfx_whistle_long (긴 기적, 부모 sfx_train_whistle)) / v2.2 (v9.21 2026-10-12 보스 1단계: 등장 연출 소리 키 10개의 부모(폴백) - 사운드 프롬프트 3절의 이름 (sfx_boss_far_howl · sfx_boss_land · sfx_thunder_roll · sfx_wing_gust · sfx_ground_crack · sfx_iron_groan · sfx_rival_wheels · sfx_iron_scream · sfx_hp_ratchet · sfx_boss_cast). 파일이 오기 전엔 부모 소리가 난다 / BgmCut - 배경음을 잠깐 뚝 끊는다 (디 오리지널 전조)) / v2.1 (v9.19 2026-10-06 웨이브 편성: 예고음 2개 sfx_cue_approach / sfx_cue_ambush 의 키 표 한 줄 - px/sfx5.py 가 만든다) / v2 (v9.16 2026-09-29 손맛 2차 - 소리 한 번에. 유저: 8비트 말고 서부 증기기관·쇠와 화약, 포탑 종류별·손님 종류별로 소리가 달라야)
/// 전역 사운드 매니저 - 씬 세팅 불필요 (첫 호출 시 스스로 생성).
///
/// - v2 변경점:
///   1) 키 표(KeyDef): 키마다 기본 볼륨·피치 변주 폭·최소 간격·"큰 소리" 표시. 잦은 소리(shot·hit·step)는 작고 촘촘하게, 큰 소리는 덕킹을 건다
///   2) 덕킹: 큰 소리(폭발·큰 처치·보스·기차 피격) 뒤 0.3초 동안 잦은 소리 -40%
///   3) PlayAt(key, worldPos): 화면 가운데에서 멀수록 작게 (화면 밖 포탑·손님은 절반 이하) - 총성이 시끄럽지 않게
///   4) 폴백: 가족 키(sfx_shot_fire / sfx_hit_iron / sfx_die_wing ...)가 없으면 부모 키(sfx_shot / sfx_hit / sfx_enemy_die)로. 파일을 덜 넣어도 무음이 아니다
///   5) 배경음 페이드 인 + 덕킹(사고·보스전 동안 -35%)
///   6) SoundKeys: 요리(속성·티어·모양) -> 발사 키 / 손님 이름 -> 재질(맞는 소리·처치 소리) / 손님 이름 -> 공격 키 / 보스 종류 -> 포효 키
/// - v1 동작 원칙 그대로: 클립이 없으면 조용히 무시(경고는 클립당 1회) / Resources/Sounds/ 에서 이름으로 로드 / 볼륨은 PlayerPrefs
///
/// 클립 목록 (px/sfx3.py + sfx4.py 가 만든다, 이름 그대로 Assets/Resources/Sounds/):
///  기본 46: ui_click/open/close, augment_pick, judge_perfect/good/bad, pickup, gold, relic, levelup, insert, fusion, shot, shot_heavy, hit, hit_crit, ricochet, enemy_die, kill_big, explosion,
///          train_hit, train_break, stun, freeze, overheat, cool, break, parry, cannon_fire, harpoon, dash, step, lever, train_whistle, whistle_low, wave_clear, boss_warning, alarm,
///          boss_groggy, boss_roar, event_resolve, event_fail, game_over, victory + bgm_main
///  가족 55: shot_<phys|fire|elec|ice|poison>[_heavy], shot_fire_cone, launch_mortar, rail, chain, field / hit_<scale|iron|crystal|wing|magma>, acc_<fire|elec|ice|poison>, die_<재질> /
///          atk_<raptor|ankylo|cactus|scorpion|tortoise|bolt|ptera|parasaur|fly|flame|steel|mosa|pachy|carno|mammoth|necro> / boss_roar_<pack|lightning|hibernator> / cook_<grill|fry|boil> / ev_<intrusion|break|fire|spill>
/// 훅 지도 (v9.16, 어디서 무엇이 나는가):
///  포탑: TurretAttackExecutor.Execute 발사(Shot) / 폭발 착탄 explosion / 장판 field / 연쇄 chain / TurretSlot 투입 insert·레벨업 levelup·마비 stun/freeze/overheat·파손 break·식음 cool / SlotMarkerUI 냉각 cool·얼음 hit_crystal→die_crystal·감전 털기 lever / TurretSlotManager 진화 fusion
///  손님: HitFeel.OnHit 명중 = 재질(Hit) + 속성 겹침(Accent), 크리 hit_crit, 물리 튕김 ricochet / Enemy.Die 처치 = 재질(Die), 큰 손님 kill_big / Enemy.AttackTrain 공격 = 종류(Attack) / BossEnemy.Setup 포효(BossRoar), 디 오리지널 whistle_low
///  주방: CookingMinigame.StartGame 조리법(Cook) / KitchenEventManager 사고 alarm + 종류(Event) + 배경음 덕킹, 결과 event_resolve/fail / ItemManager.Acquire relic / GameManager 정차 gold
///  진행: GameManager 패배 train_break→game_over, 승리 whistle→victory, 배경음 로비부터 / BossGimmickSystem 보스전 덕킹 / UI: ModalFeel.Play ui_open, 닫힘 ui_close, 증강 augment_pick
///  편성 (v9.19): 매복 바닥 고리 = cue_ambush (조각 예고음 cue_approach 는 v9.26 에서 화살표와 같이 지웠다)
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;

    private const int SFX_POOL = 12;
    private const float THROTTLE_SEC = 0.06f;

    private AudioSource[] sfxSources;
    private AudioSource bgmSource;
    private int nextSource = 0;

    private Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
    private HashSet<string> missingWarned = new HashSet<string>();
    private Dictionary<string, float> lastPlayTime = new Dictionary<string, float>();

    // v2: 덕킹·배경음
    private float duckUntil = -1f;          // 큰 소리 뒤 잦은 소리를 줄이는 기한 (unscaled)
    private float bgmTarget = 0f;           // 배경음 목표 배율 (0 = 정지 상태, 1 = 정상)
    private float bgmCur = 0f;
    private HashSet<string> bgmDuckReasons = new HashSet<string>();   // 덕킹 이유 (사고·보스전·게임오버가 겹쳐도 하나가 끝났다고 풀리지 않는다)

    /// <summary>v2: 키별 기본값. vol = 기본 배율, jitter = 피치 변주 폭, gap = 같은 키 최소 간격(초), big = 큰 소리(덕킹 유발), small = 덕킹을 받는 잦은 소리</summary>
    private struct KeyDef
    {
        public float vol, jitter, gap; public bool big, small;
        public KeyDef(float v, float j, float g, bool b, bool s) { vol = v; jitter = j; gap = g; big = b; small = s; }
    }
    private static readonly Dictionary<string, KeyDef> DEFS = new Dictionary<string, KeyDef>();
    private static readonly KeyDef DEFAULT = new KeyDef(1f, 0.06f, THROTTLE_SEC, false, false);

    private static void Def(string prefix, float vol, float jitter, float gap, bool big, bool small) { DEFS[prefix] = new KeyDef(vol, jitter, gap, big, small); }

    static SoundManager()
    {
        // 잦은 소리: 작게·촘촘하게·변주 크게 (접두어 매칭 - sfx_shot_fire_heavy 는 sfx_shot 의 값을 받는다)
        Def("sfx_shot", 0.55f, 0.09f, 0.045f, false, true);
        Def("sfx_hit", 0.5f, 0.1f, 0.04f, false, true);
        Def("sfx_acc", 0.4f, 0.1f, 0.05f, false, true);
        Def("sfx_ricochet", 0.55f, 0.08f, 0.12f, false, true);
        Def("sfx_step", 0.35f, 0.12f, 0.12f, false, true);
        Def("sfx_chain", 0.5f, 0.08f, 0.08f, false, true);
        Def("sfx_launch_mortar", 0.7f, 0.06f, 0.08f, false, false);
        Def("sfx_rail", 0.85f, 0.05f, 0.12f, false, false);
        Def("sfx_field", 0.65f, 0.06f, 0.15f, false, false);
        Def("sfx_die", 0.8f, 0.07f, 0.08f, false, false);
        Def("sfx_enemy_die", 0.8f, 0.07f, 0.08f, false, false);
        Def("sfx_atk", 0.7f, 0.06f, 0.1f, false, false);
        // 큰 소리: 덕킹 유발
        Def("sfx_explosion", 1f, 0.05f, 0.1f, true, false);
        Def("sfx_kill_big", 1f, 0.05f, 0.15f, true, false);
        Def("sfx_train_hit", 1f, 0.04f, 0.2f, true, false);
        Def("sfx_train_break", 1f, 0f, 1f, true, false);
        Def("sfx_boss", 1f, 0.03f, 0.3f, true, false);
        Def("sfx_cannon_fire", 1f, 0.03f, 0.3f, true, false);
        Def("sfx_break", 1f, 0.03f, 0.5f, true, false);
        Def("sfx_game_over", 1f, 0f, 2f, true, false);
        Def("sfx_victory", 1f, 0f, 2f, true, false);
        // 그 외 기본: 1.0 / 0.06 / 0.06초
        Def("sfx_ui", 0.8f, 0.03f, 0.05f, false, false);
        Def("sfx_cook", 0.7f, 0.04f, 0.3f, false, false);
        Def("sfx_ev", 0.9f, 0.03f, 0.5f, false, false);
        Def("sfx_event_", 1f, 0.03f, 0.3f, false, false);   // 결과음(resolve/fail)은 현장음(sfx_ev_) 표와 별개
        Def("sfx_alarm", 0.8f, 0f, 0.3f, false, false);
        Def("sfx_cue", 1f, 0.05f, 0.3f, false, false);      // v2.1: 편성 예고 - 두 조각의 예고가 겹쳐도 0.3초 안에는 한 번만
        Def("sfx_fork_clank", 0.8f, 0.05f, 0.12f, false, false);   // v2.3: 갈림길 - 칸마다 (0.12초 안에 두 칸이 겹치면 한 번)
        Def("sfx_whistle_long", 1f, 0.02f, 1f, false, false);      // v2.3: 클라이맥스 긴 기적 - 화음이라 피치 변주는 작게
    }

    private static KeyDef DefOf(string key)
    {
        // 가장 긴 접두어 우선
        KeyDef best = DEFAULT; int bestLen = -1;
        foreach (KeyValuePair<string, KeyDef> kv in DEFS)
            if (key.StartsWith(kv.Key) && kv.Key.Length > bestLen) { best = kv.Value; bestLen = kv.Key.Length; }
        return best;
    }

    // ─────────────────────────────────────────────
    // 볼륨 (PlayerPrefs 영구 저장)
    // ─────────────────────────────────────────────
    public static float SfxVolume
    {
        get { return PlayerPrefs.GetFloat("WDT_VolSFX", 0.8f); }
        set { PlayerPrefs.SetFloat("WDT_VolSFX", Mathf.Clamp01(value)); }
    }

    public static float BgmVolume
    {
        get { return PlayerPrefs.GetFloat("WDT_VolBGM", 0.4f); }
        set
        {
            PlayerPrefs.SetFloat("WDT_VolBGM", Mathf.Clamp01(value));
            if (instance != null && instance.bgmSource != null) instance.ApplyBgmVolume();
        }
    }

    // ─────────────────────────────────────────────
    // 초기화 (첫 호출 시 자동 생성)
    // ─────────────────────────────────────────────
    private static SoundManager Get()
    {
        if (instance != null) return instance;

        GameObject go = new GameObject("SoundManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SoundManager>();

        instance.sfxSources = new AudioSource[SFX_POOL];
        for (int i = 0; i < SFX_POOL; i++)
        {
            instance.sfxSources[i] = go.AddComponent<AudioSource>();
            instance.sfxSources[i].playOnAwake = false;
        }

        instance.bgmSource = go.AddComponent<AudioSource>();
        instance.bgmSource.playOnAwake = false;
        instance.bgmSource.loop = true;

        return instance;
    }

    private void Update()
    {
        TickMain();
        TickLayers();   // v2.3: 배경음 배율을 정한 뒤에 - 끊김이 풀리는 프레임에 배경음과 층이 같이 오른다
    }

    private void TickMain()
    {
        // 배경음 페이드/덕킹 (unscaled - 일시정지·시간 정지 카드 중에도 자연스럽게)
        if (bgmSource == null) return;
        float target = bgmTarget * (bgmDuckReasons.Count > 0 ? 0.65f : 1f) * (soloOn ? 0f : 1f);   // v2.3: 단독 곡이 대신하는 동안 배경음은 소리 0 으로 돈다
        // v2.2: 뚝 끊긴 동안은 0, 끝나는 프레임에 바로 제자리 (1.5초 페이드는 덕킹·시작·끝에만 - "끊긴다"가 들려야 한다)
        if (Time.unscaledTime < bgmCutUntil) { if (bgmCur != 0f) { bgmCur = 0f; ApplyBgmVolume(); } return; }
        if (bgmCutResume) { bgmCutResume = false; bgmCur = target; ApplyBgmVolume(); return; }
        if (Mathf.Abs(bgmCur - target) > 0.001f)
        {
            bgmCur = Mathf.MoveTowards(bgmCur, target, Time.unscaledDeltaTime / 1.5f);
            ApplyBgmVolume();
        }
    }

    private float bgmCutUntil = 0f;      // v2.2: 이 시각(실시간)까지 배경음이 끊긴다
    private bool bgmCutResume = false;   // 끊김이 끝나는 프레임에 바로 돌아오기 위한 표시

    /// <summary>v2.2: 배경음을 sec 초(실시간) 동안 뚝 끊었다가 바로 돌린다 (페이드 없음). 디 오리지널 등장의 전조 - 침묵 뒤 낮은 기적</summary>
    public static void BgmCut(float sec)
    {
        if (instance == null || instance.bgmSource == null || sec <= 0f) return;
        instance.bgmCutUntil = Time.unscaledTime + sec;
        instance.bgmCutResume = true;
        instance.bgmCur = 0f;
        instance.ApplyBgmVolume();
    }

    private void ApplyBgmVolume()
    {
        if (bgmSource != null) bgmSource.volume = BgmVolume * bgmCur;
    }

    // ─────────────────────────────────────────────
    // 효과음 재생
    // ─────────────────────────────────────────────

    /// <summary>효과음 재생. 클립 없으면 조용히 무시. 키 표의 기본 볼륨·변주 폭을 쓴다</summary>
    public static void Play(string key)
    {
        Play(key, 1f, -1f);
    }

    /// <summary>효과음 재생 (볼륨 배율 + 피치 랜덤 폭 지정. pitchJitter 가 0 미만이면 키 표 값)</summary>
    public static void Play(string key, float volumeMul, float pitchJitter)
    {
        PlayInternal(key, volumeMul, pitchJitter, 1f);
    }

    /// <summary>v2: 월드 위치에서 나는 소리 - 화면 가운데에서 가로로 멀수록 작게 (반 화면 안 = 그대로, 1.5 화면 밖 = 35%)</summary>
    public static void PlayAt(string key, Vector3 worldPos)
    {
        PlayAt(key, worldPos, 1f);
    }

    public static void PlayAt(string key, Vector3 worldPos, float volumeMul)
    {
        float dist = 1f;
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 v = cam.WorldToViewportPoint(worldPos);
            float dx = Mathf.Abs(v.x - 0.5f);   // 0 = 가운데, 0.5 = 화면 끝
            dist = dx <= 0.5f ? 1f : Mathf.Lerp(1f, 0.35f, Mathf.Clamp01((dx - 0.5f) / 1f));
        }
        PlayInternal(key, volumeMul, -1f, dist);
    }

    private static void PlayInternal(string key, float volumeMul, float pitchJitter, float distMul)
    {
        if (string.IsNullOrEmpty(key)) return;
        SoundManager sm = Get();
        KeyDef def = DefOf(key);

        // 스팸 방지: 같은 키 최소 간격 (unscaled)
        float last;
        if (sm.lastPlayTime.TryGetValue(key, out last) && Time.unscaledTime - last < def.gap)
            return;
        sm.lastPlayTime[key] = Time.unscaledTime;

        AudioClip clip = sm.LoadClipWithFallback(key);
        if (clip == null) return;

        // 덕킹: 큰 소리 뒤 0.3초 동안 잦은 소리 -40%
        float duck = 1f;
        if (def.small && Time.unscaledTime < sm.duckUntil) duck = 0.6f;
        if (def.big) sm.duckUntil = Time.unscaledTime + 0.3f;

        // 풀에서 쉬고 있는 소스를 고른다 (PlayOneShot 은 소스의 피치를 같이 쓰므로, 긴 소리가 나는 중인 소스의 피치를 바꾸면 그 소리가 흔들린다)
        AudioSource src = null;
        for (int i = 0; i < SFX_POOL; i++)
        {
            int k = (sm.nextSource + i) % SFX_POOL;
            if (!sm.sfxSources[k].isPlaying) { src = sm.sfxSources[k]; sm.nextSource = (k + 1) % SFX_POOL; break; }
        }
        float jitter = pitchJitter >= 0f ? pitchJitter : def.jitter;
        if (src != null) src.pitch = 1f + Random.Range(-jitter, jitter);   // 변주 (기계음 반복감 제거)
        else { src = sm.sfxSources[sm.nextSource]; sm.nextSource = (sm.nextSource + 1) % SFX_POOL; }   // 전부 바쁘면 겹쳐 재생, 피치는 그대로
        src.PlayOneShot(clip, Mathf.Clamp01(SfxVolume * def.vol * volumeMul * duck * distMul));
    }

    // ─────────────────────────────────────────────
    // 배경음
    // ─────────────────────────────────────────────
    public static void PlayBGM(string key)
    {
        SoundManager sm = Get();
        AudioClip clip = sm.LoadClip(key);
        if (clip == null) return;
        sm.bgmTarget = 1f;
        sm.bgmDuckReasons.Clear();   // 새 운행: 지난 운행의 덕킹(게임오버·보스전)은 잊는다
        if (sm.bgmSource.clip == clip && sm.bgmSource.isPlaying) return;

        sm.bgmSource.clip = clip;
        sm.bgmCur = 0f;   // v2: 페이드 인
        sm.ApplyBgmVolume();
        // v2.3: 클라이맥스 층과 같은 오디오 시계 순간에 시작한다 (프레임 시각이 아니라 dspTime - 둘이 같은 샘플에서 출발해 박이 안 어긋난다)
        double startAt = AudioSettings.dspTime + 0.1;
        sm.bgmSource.PlayScheduled(startAt);
        if (key == "bgm_main") sm.StartLayers(startAt);
        else sm.StopLayers();
    }

    public static void StopBGM()
    {
        if (instance != null && instance.bgmSource != null) { instance.bgmTarget = 0f; instance.bgmCur = 0f; instance.bgmSource.Stop(); }
        if (instance != null) instance.StopLayers();   // v2.3
    }

    // ─────────────────────────────────────────────
    // v2.3 (v9.24): 클라이맥스 층 - bgm_main 과 같은 순간에 소리 0 으로 같이 틀어 두고, 박 머리에 소리만 올린다
    //   같이 도는 층 = 길이가 bgm_main 의 정수분의 1 (px/sfx6.py 는 4마디 = 1/4). 아니면 단독 곡 - 클라이맥스 층만, 켜면 배경음 대신 처음부터
    // ─────────────────────────────────────────────
    public const int LayerClimax = 0;   // 디 오리지널 P3
    public const int LayerLite = 1;     // 지역 보스 발악 (피스톤만)
    private static readonly string[] LAYER_KEYS = { "bgm_climax", "bgm_climax_lite" };
    private AudioSource[] layerSrc;
    private readonly bool[] layerSync = new bool[2];         // 배경음과 같이 도는 층인가
    private readonly float[] layerCur = new float[2];         // 지금 배율
    private readonly float[] layerTarget = new float[2];      // 목표 배율
    private readonly float[] layerRate = new float[2];        // 내려갈 때 초당 변화 (LayerOut 이 정한다)
    private readonly float[] layerEnterAt = { -1f, -1f };     // 이 실시간에 목표로 뛴다 (-1 = 없음)
    private bool soloOn = false;                              // 단독 곡이 배경음을 대신하는 중

    private void EnsureLayers()
    {
        if (layerSrc != null) return;
        layerSrc = new AudioSource[LAYER_KEYS.Length];
        for (int i = 0; i < layerSrc.Length; i++)
        {
            layerSrc[i] = gameObject.AddComponent<AudioSource>();
            layerSrc[i].playOnAwake = false;
            layerSrc[i].loop = true;
            layerSrc[i].volume = 0f;
        }
    }

    /// <summary>배경음과 같은 dsp 시각에 층들을 소리 0 으로 시작한다. 같이 돌 수 없는 층(길이가 안 맞음)은 틀지 않고 표시만</summary>
    private void StartLayers(double startAt)
    {
        EnsureLayers();
        soloOn = false;
        AudioClip main = bgmSource.clip;
        for (int i = 0; i < layerSrc.Length; i++)
        {
            AudioSource src = layerSrc[i];
            src.Stop();
            layerCur[i] = 0f; layerTarget[i] = 0f; layerEnterAt[i] = -1f; src.volume = 0f;
            AudioClip clip = LoadClip(LAYER_KEYS[i]);
            src.clip = clip;
            layerSync[i] = clip != null && main != null && LayerFits(main, clip);
            if (layerSync[i]) src.PlayScheduled(startAt);
            if (clip != null && !layerSync[i])
                Debug.Log("[SoundManager] " + LAYER_KEYS[i] + " 길이가 bgm_main 과 안 맞는다 - " + (i == LayerClimax ? "켤 때 배경음 대신 단독으로 튼다" : "쓰지 않는다"));
        }
    }

    private void StopLayers()
    {
        soloOn = false;
        if (layerSrc == null) return;
        for (int i = 0; i < layerSrc.Length; i++)
        {
            layerSrc[i].Stop();
            layerSrc[i].volume = 0f;
            layerCur[i] = 0f; layerTarget[i] = 0f; layerEnterAt[i] = -1f;
        }
    }

    /// <summary>층 길이가 배경음 길이의 정수분의 1 인가 (한 바퀴에 3ms 안으로만 어긋나면 된다) - 그래야 같이 틀어 둔 채 박이 맞는다</summary>
    private static bool LayerFits(AudioClip main, AudioClip layer)
    {
        if (layer.samples <= 0 || main.samples <= 0 || layer.frequency != main.frequency) return false;
        int k = Mathf.RoundToInt((float)main.samples / layer.samples);
        if (k < 1) return false;
        return Mathf.Abs(main.samples - k * layer.samples) <= main.frequency * 0.003f;
    }

    /// <summary>매 프레임 (실시간): 들어올 시각이 된 층은 목표로 바로 (박 머리 - 페이드 없음), 내려가는 층은 정한 속도로</summary>
    private void TickLayers()
    {
        if (layerSrc == null) return;
        float now = Time.unscaledTime;
        for (int i = 0; i < layerSrc.Length; i++)
        {
            if (layerEnterAt[i] >= 0f)
            {
                if (now < layerEnterAt[i]) { layerSrc[i].volume = 0f; continue; }   // 아직 숨
                layerEnterAt[i] = -1f;
                if (!layerSync[i]) BeginSolo(i);
                layerCur[i] = layerTarget[i];
            }
            else if (layerCur[i] != layerTarget[i])
            {
                layerCur[i] = Mathf.MoveTowards(layerCur[i], layerTarget[i], Mathf.Max(0.01f, layerRate[i]) * Time.unscaledDeltaTime);
                if (layerCur[i] <= 0f && soloOn && i == LayerClimax) EndSolo();
            }
            layerSrc[i].volume = BgmVolume * layerCur[i] * LayerGain(i);
        }
    }

    /// <summary>
    /// 층에 곱하는 배경음 쪽 배율 - 같이 도는 층은 배경음의 지금 배율(덕킹·끊김·페이드)을 같이 탄다 (지역 보스전의 -35% 덕킹 아래에서도 배경음 대비 비율이 그대로).
    /// 단독 곡은 배경음이 소리 0 이라 덕킹·끊김만 따른다
    /// </summary>
    private float LayerGain(int i)
    {
        if (Time.unscaledTime < bgmCutUntil) return 0f;
        if (layerSync[i]) return bgmTarget > 0f ? Mathf.Clamp01(bgmCur / bgmTarget) : 0f;
        return bgmDuckReasons.Count > 0 ? 0.65f : 1f;
    }

    private void BeginSolo(int i)
    {
        if (i != LayerClimax || layerSrc[i].clip == null) return;
        layerSrc[i].Stop();
        layerSrc[i].Play();   // 단독 곡은 처음부터
        soloOn = true;
        bgmCur = 0f;
        ApplyBgmVolume();
    }

    private void EndSolo()
    {
        layerSrc[LayerClimax].Stop();
        soloOn = false;   // 배경음은 Update 의 1.5초 페이드로 돌아온다 (소리 0 으로 계속 돌고 있었다)
    }

    /// <summary>
    /// v2.3: 다음 박까지 남은 실시간 초 (minSec 이상) - bgm_main 의 지금 자리와 GameBalance.BgmBpm 으로 센다. 배경음이 안 돌면 minSec.
    /// 클라이맥스의 "숨"이 이 길이다 - 숨이 끝나는 순간이 박 머리
    /// </summary>
    public static float SecondsToNextBeat(float minSec)
    {
        if (instance == null || instance.bgmSource == null || instance.bgmSource.clip == null || !instance.bgmSource.isPlaying) return minSec;
        AudioClip c = instance.bgmSource.clip;
        int freq = c.frequency > 0 ? c.frequency : 44100;
        double beat = freq * 60.0 / Mathf.Max(30f, GameBalance.BgmBpm);
        double pos = instance.bgmSource.timeSamples;
        double next = System.Math.Ceiling((pos + minSec * freq) / beat) * beat;
        return (float)((next - pos) / freq);
    }

    /// <summary>
    /// v2.3: 층 i (LayerClimax / LayerLite) 를 delaySec(실시간) 뒤에 level 로 올린다 - 그 순간 바로 (페이드 없음).
    /// 같이 도는 층이 아니면 클라이맥스 층만 단독 곡으로 바꿔 튼다 (배경음은 소리 0). 파일이 없거나 쓸 수 없으면 false
    /// </summary>
    public static bool LayerIn(int i, float level, float delaySec)
    {
        SoundManager sm = instance;
        if (sm == null || sm.layerSrc == null || i < 0 || i >= sm.layerSrc.Length || sm.layerSrc[i].clip == null) return false;
        if (!sm.layerSync[i] && i != LayerClimax) return false;
        sm.layerTarget[i] = Mathf.Clamp01(level);
        sm.layerEnterAt[i] = Time.unscaledTime + Mathf.Max(0f, delaySec);
        return true;
    }

    /// <summary>v2.3: 층 i 를 fadeSec(실시간)에 걸쳐 내린다 (0 = 바로 끊김). 단독 곡이면 다 내려간 뒤 배경음이 돌아온다</summary>
    public static void LayerOut(int i, float fadeSec)
    {
        SoundManager sm = instance;
        if (sm == null || sm.layerSrc == null || i < 0 || i >= sm.layerSrc.Length) return;
        sm.layerEnterAt[i] = -1f;
        sm.layerTarget[i] = 0f;
        if (fadeSec <= 0f)
        {
            sm.layerCur[i] = 0f;
            sm.layerSrc[i].volume = 0f;
            if (sm.soloOn && i == LayerClimax) sm.EndSolo();
        }
        else sm.layerRate[i] = sm.layerCur[i] / fadeSec;
    }

    /// <summary>v2: 사고·보스전 동안 배경음을 낮춘다 (-35%, 1.5초 걸쳐). reason 별로 켜고 끈다 - 겹친 이유가 하나라도 남아 있으면 낮은 채로</summary>
    public static void BgmDuck(string reason, bool on)
    {
        if (instance == null) { if (!on) return; Get(); }
        if (on) instance.bgmDuckReasons.Add(reason);
        else instance.bgmDuckReasons.Remove(reason);
    }

    /// <summary>v2: 잠깐 뒤에 재생 (실시간 - 게임오버·시간 정지 중에도). 연출 순서용 (기차 정지음 -> 패배 스팅)</summary>
    public static void PlayDelayed(string key, float delaySec)
    {
        SoundManager sm = Get();
        sm.StartCoroutine(sm.DelayedRoutine(key, delaySec));
    }

    private System.Collections.IEnumerator DelayedRoutine(string key, float delaySec)
    {
        yield return new WaitForSecondsRealtime(delaySec);
        Play(key);
    }

    // ─────────────────────────────────────────────
    // 클립 로드 (Resources/Sounds/ + 캐시)
    // ─────────────────────────────────────────────
    private AudioClip LoadClip(string key)
    {
        AudioClip clip;
        if (clipCache.TryGetValue(key, out clip)) return clip;

        clip = Resources.Load<AudioClip>("Sounds/" + key);
        clipCache[key] = clip;   // null도 캐시 (매번 디스크 탐색 방지)

        if (clip == null && !missingWarned.Contains(key))
        {
            missingWarned.Add(key);
            Debug.Log("[SoundManager] 클립 없음 (무시하고 진행): Resources/Sounds/" + key);
        }
        return clip;
    }

    /// <summary>v2: 가족 키가 없으면 부모 키로 (sfx_shot_fire_heavy -> sfx_shot_fire -> sfx_shot). 부모도 없으면 null</summary>
    private AudioClip LoadClipWithFallback(string key)
    {
        AudioClip clip = LoadClip(key);
        if (clip != null) return clip;
        string parent = SoundKeys.Parent(key);
        int guard = 0;
        while (clip == null && !string.IsNullOrEmpty(parent) && guard++ < 4)
        {
            clip = LoadClip(parent);
            parent = SoundKeys.Parent(parent);
        }
        return clip;
    }
}

/// <summary>
/// v2: 게임 데이터 -> 소리 키. 코드 곳곳이 문자열을 직접 쓰지 않고 여기서 받는다.
/// </summary>
public static class SoundKeys
{
    /// <summary>가족 키의 부모 (폴백용)</summary>
    public static string Parent(string key)
    {
        if (key.StartsWith("sfx_shot_fire_cone")) return "sfx_shot_fire";
        if (key.StartsWith("sfx_shot_") && key.EndsWith("_heavy")) return key.Substring(0, key.Length - 6);
        if (key.StartsWith("sfx_shot_")) return "sfx_shot";
        if (key.StartsWith("sfx_hit_") && key != "sfx_hit_crit") return "sfx_hit";
        if (key.StartsWith("sfx_die_")) return "sfx_enemy_die";
        if (key.StartsWith("sfx_boss_roar_")) return "sfx_boss_roar";
        if (key == "sfx_rail" || key == "sfx_launch_mortar") return "sfx_shot_heavy";
        if (key == "sfx_chain") return "sfx_shot_elec";
        if (key == "sfx_field") return "sfx_shot_poison";
        if (key == "sfx_ricochet") return "sfx_hit";
        if (key == "sfx_whistle_low") return "sfx_train_whistle";
        if (key == "sfx_whistle_long") return "sfx_train_whistle";   // v2.3 (v9.24): 클라이맥스 긴 기적
        if (key == "sfx_fork_clank") return "sfx_lever";             // v2.3 (v9.24): 갈림길 덜컹 - 파일이 없으면 쇠 손잡이 소리 (기차 피격음은 맞은 줄 안다)
        // v2.2 (v9.21): 보스 등장 연출의 소리 - 파일이 오기 전엔 있는 소리를 빌려 쓴다 (claude/사운드_프롬프트_2026-10-08.md 3절)
        if (key == "sfx_boss_far_howl") return "sfx_boss_roar_pack";       // 먼 울음 (녹슨 발톱 전조)
        if (key == "sfx_boss_land") return "sfx_explosion";                // 뛰어내려 착지
        if (key == "sfx_thunder_roll") return "sfx_boss_roar_lightning";   // 천둥 굴러옴 (천둥 둥지 전조)
        if (key == "sfx_wing_gust") return "sfx_atk_ptera";                // 날갯짓 바람
        if (key == "sfx_ground_crack") return "sfx_break";                 // 땅이 갈라짐 (동면자)
        if (key == "sfx_iron_groan") return "sfx_boss_roar_hibernator";    // 떠오를 때 낮은 쇠 울음
        if (key == "sfx_rival_wheels") return "sfx_train_hit";             // 붉은 열차가 추월해 들어옴
        if (key == "sfx_iron_scream") return "sfx_break";                  // 머리가 나란해지는 순간 쇠 비명
        if (key == "sfx_hp_ratchet") return "sfx_ui_click";                // HP 바가 차오를 때 래칫
        if (key == "sfx_boss_cast") return "sfx_boss_roar";                // 패턴 준비 울음 (v9.20 은 등장 울음을 작게 - 파일이 오면 CastBegin 이 이 키를 쓴다)
        if (key == "sfx_event_resolve") return "sfx_judge_perfect";
        if (key == "sfx_event_fail") return "sfx_judge_bad";
        if (key == "sfx_levelup" || key == "sfx_insert" || key == "sfx_fusion" || key == "sfx_gold" || key == "sfx_relic") return "sfx_pickup";
        if (key == "sfx_ui_open" || key == "sfx_ui_close") return "sfx_ui_click";
        if (key == "sfx_cool") return "sfx_overheat";
        if (key == "sfx_stun" || key == "sfx_freeze") return "sfx_hit_crit";
        if (key == "sfx_break") return "sfx_train_hit";
        if (key == "sfx_kill_big") return "sfx_enemy_die";
        if (key == "sfx_game_over") return "sfx_train_break";
        if (key == "sfx_victory") return "sfx_wave_clear";
        return "";
    }

    private static string TagKey(FoodTag tag)
    {
        switch (tag)
        {
            case FoodTag.Fire: return "fire";
            case FoodTag.Elec: return "elec";
            case FoodTag.Ice: return "ice";
            case FoodTag.Poison: return "poison";
            default: return "phys";   // Phys·Def
        }
    }

    /// <summary>포탑 발사 키: 속성 + 전설이면 _heavy. 모양이 특별하면 그 키 (박격 발사 / 레일 / 화염 방사)</summary>
    public static string Shot(RecipeData r, AttackShape shapeUsed)
    {
        if (r == null) return "sfx_shot";
        switch (shapeUsed)
        {
            case AttackShape.Explode: return "sfx_launch_mortar";
            case AttackShape.Pierce: return "sfx_rail";
            case AttackShape.Cone: return "sfx_shot_fire_cone";
            case AttackShape.Field: return "sfx_shot_" + TagKey(r.tag);
            case AttackShape.Chain: return "sfx_shot_elec" + (r.tier >= 2 ? "_heavy" : "");
        }
        return "sfx_shot_" + TagKey(r.tag) + (r.tier >= 2 ? "_heavy" : "");
    }

    /// <summary>속성 겹침 (명중 때 재질 소리 위에 얹는다). 물리·방어는 없음</summary>
    public static string Accent(FoodTag tag)
    {
        switch (tag)
        {
            case FoodTag.Fire: return "sfx_acc_fire";
            case FoodTag.Elec: return "sfx_acc_elec";
            case FoodTag.Ice: return "sfx_acc_ice";
            case FoodTag.Poison: return "sfx_acc_poison";
        }
        return "";
    }

    /// <summary>손님 재질: scale(가죽·비늘 기계) / iron(무쇠 갑옷) / crystal(결정) / wing(날개) / magma(용암)</summary>
    public static string Material(string enemyName)
    {
        string n = enemyName ?? "";
        if (n.Contains("크리스탈") || n.Contains("아이스") || n.Contains("동면자")) return "crystal";
        if (n.Contains("마그마")) return "magma";
        if (n.Contains("테라노돈") || n.Contains("프테라") || n.Contains("익룡") || n.Contains("플라이")) return "wing";
        if (n.Contains("아르마딜로") || n.Contains("안킬로") || n.Contains("거북") || n.Contains("강철") || n.Contains("맘모스") || n.Contains("파라사우") || n.Contains("스피노")
            || n.Contains("메카") || n.Contains("발톱") || n.Contains("오리지널") || n.Contains("둥지")) return "iron";   // 보스 4종·새끼 발톱 = 무쇠
        return "scale";
    }

    public static string Hit(string enemyName) { return "sfx_hit_" + Material(enemyName); }
    public static string Die(string enemyName) { return "sfx_die_" + Material(enemyName); }

    /// <summary>손님 공격 키 (종류별). 보스는 가장 가까운 덩치의 소리를 빌린다. 모르는 이름은 랩터</summary>
    public static string Attack(string enemyName)
    {
        string n = enemyName ?? "";
        if (n.Contains("오리지널")) return "sfx_atk_steel";           // 1호 기관차 - 강철
        if (n.Contains("티렉스") || n.Contains("녹슨 발톱")) return "sfx_atk_carno";   // 큰 턱
        if (n.Contains("둥지")) return "sfx_atk_bolt";
        if (n.Contains("동면자")) return "sfx_atk_mammoth";
        if (n.Contains("강철")) return "sfx_atk_steel";
        if (n.Contains("스팀 랩터") || n.Contains("랩터")) return "sfx_atk_raptor";
        if (n.Contains("아르마딜로") || n.Contains("안킬로")) return "sfx_atk_ankylo";
        if (n.Contains("캑터스")) return "sfx_atk_cactus";
        if (n.Contains("전갈")) return "sfx_atk_scorpion";
        if (n.Contains("거북")) return "sfx_atk_tortoise";
        if (n.Contains("테라노돈")) return "sfx_atk_bolt";
        if (n.Contains("독침")) return "sfx_atk_ptera";
        if (n.Contains("파라사우")) return "sfx_atk_parasaur";
        if (n.Contains("플라이")) return "sfx_atk_fly";
        if (n.Contains("화염 익룡") || n.Contains("익룡")) return "sfx_atk_flame";
        if (n.Contains("모사")) return "sfx_atk_mosa";
        if (n.Contains("파키")) return "sfx_atk_pachy";
        if (n.Contains("카르노")) return "sfx_atk_carno";
        if (n.Contains("맘모스")) return "sfx_atk_mammoth";
        if (n.Contains("스피노")) return "sfx_atk_necro";
        return "sfx_atk_raptor";
    }

    /// <summary>보스 포효 키 (BossEnemy.BossKind 이름: RustClaw / ThunderNest / Hibernator / Original). 디 오리지널은 기본 포효 + 낮은 기적(호출부)</summary>
    public static string BossRoar(string kindName)
    {
        switch (kindName)
        {
            case "RustClaw": return "sfx_boss_roar_pack";
            case "ThunderNest": return "sfx_boss_roar_lightning";
            case "Hibernator": return "sfx_boss_roar_hibernator";
        }
        return "sfx_boss_roar";
    }

    /// <summary>조리대 키 (KitchenPanel.MethodOf: 0 굽기 1 볶기 2 끓이기)</summary>
    public static string Cook(int method)
    {
        return method == 1 ? "sfx_cook_fry" : method == 2 ? "sfx_cook_boil" : "sfx_cook_grill";
    }

    /// <summary>주방 사고 키 (BriefingTexts 의 종류 이름과 같은 키: intrusion / break / fire / spill)</summary>
    public static string Event(string kind) { return "sfx_ev_" + kind; }
}
