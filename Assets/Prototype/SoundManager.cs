using System.Collections.Generic;
using UnityEngine;

/// <summary>효과음 버스 = 게임 전체 동시 재생 예산 넷 (v9.33, routing_manifest voice_budgets). Auto = 키 표(SoundMix)의 버스를 쓴다</summary>
public enum SfxBus { Auto = 0, Combat = 1, Warning = 2, Boss = 3, Small = 4 }

/// <summary>
/// [SoundManager.cs] v3 (v9.33 2026-10-25 소리 팩 - 유저 제작 오디오 WDT_world_audio_2026-10-22 + 그 팩의 음악_전환과_재생규칙.md · routing_manifest.json)
///   1) 효과음 = 버스 넷의 동시 수 예산 (일반 전투 8 · 긴급 경고 1 · 보스 전경 2 · 작은 소리·UI 3 - GameBalance.SoundVoices*). 키마다 우선순위(0~100)·갈래·최소 간격·동시 수는 SoundMix 표.
///      새 소리는 세 단계를 지난다: 같은 키 동시 수 -> 갈래 상한(명중 4 · 처치 3 - 일반 전투 8칸을 한 종류가 다 먹지 않게) -> 버스 예산.
///      단계마다 차 있으면 그 안의 가장 약한 소리(우선순위 낮은 것 -> 작게 들리는 것 -> 오래된 것)와 견준다 - 새 소리가 우선순위가 높거나, 같고 더 작지 않으면 그것을 끊고, 아니면 새 소리를 안 낸다.
///      끊는 페이드 = SoundStealFadeSec(12ms) 이되 적어도 2.5 프레임 (짧으면 다음 프레임에 볼륨 단계 없이 뚝 멈춘다). 긴급 경고는 시작한 지 SoundWarnProtectSec(0.3초) 안 된 경고를 안 끊는다.
///      문맥 재생(PlayIn)은 같은 파일이라도 간격·동시 수를 따로 센다 (포탑 폭발이 방금 났다고 보스 전환 폭음이 먹히지 않게). 표에 없는 키는 부모 키(SoundKeys.Parent)의 값
///   2) 변형: Resources/Sounds/키_v01 ~ _v09 를 다 읽어 직전 것을 연속으로 안 고른다. _vNN 이 없으면 예전 이름 키.wav
///   3) 경고 덕킹: 긴급 경고 버스에서 소리가 실제로 나는 동안만 배경음 -5dB · 일반 전투음 -3dB (걸리는 20ms 는 한 프레임보다 짧아 시작 프레임에 바로, 300ms 에 풀린다, 겹쳐도 더하지 않는다).
///      예전 "큰 소리 뒤 잦은 소리 -40%" 는 뗐고, "보스전·사고 동안 배경음 -35%"(BgmDuck) 는 GameBalance.SoundLongDuckOn 일 때만
///   4) 키별 볼륨 표(v2 의 발사 0.55 · 발소리 0.35 ...)는 뗐다 - 새 파일이 이미 크기를 나눠 놨다 (tools/audio_v933.py 가 전부 같은 +5dB). 호출부의 배율은 그대로 곱한다
///   5) 배경음 두 소스 교차 전환: MusicTo(곡, 초, 이어서) - 같은 곡이면 그 자리에서 계속, 다른 곡이면 진폭 선형으로 겹쳐 바꾸고 나간 곡의 자리를 기억한다 (같은 지역으로 돌아오면 이어서).
///      MusicStop(초) = 꺼짐 (보스 처치·승리·패배). 어느 순간이든 소리 나는 곡은 둘까지 - 셋째 곡이 오면 둘 중 작게 들리는 쪽을 멈춘다. 곡 파일이 없으면 bgm_main.
///      BgmCut 이 풀릴 때 bgm_main 은 바로 (박 머리), 다른 곡은 0.12초에 걸쳐
///   6) 클라이맥스 층(bgm_climax · _lite)은 bgm_main 위에서만 (같은 dspTime 에 같이 튼다 - v2.3 그대로). 다른 곡이 돌면 LayerIn 이 false, SecondsToNextBeat 는 minSec.
///      같이 도는 층은 bgm_main 의 지금 배율을 그대로 탄다 (MusicStop 이면 같이 내려간다)
///   배경음 기본 볼륨 0.4 -> 0.7 (설정을 한 번도 안 건드린 PC 만 - PlayerPrefs WDT_VolBGM). 새 음악이 제작 의도의 음악:효과음 비율에 가깝다
/// 이전: v2.3 (v9.24 클라이맥스 층 PlayScheduled · SecondsToNextBeat · sfx_fork_clank · sfx_whistle_long) / v2.2 (v9.21 등장 연출 소리 키 10개의 부모 · BgmCut) / v2.1 (v9.19 편성 예고음) /
///       v2 (v9.16 키 표 · PlayAt 거리 감쇠 · 부모 폴백 · 배경음 페이드 · SoundKeys) / v1 (Resources/Sounds/ 에서 이름으로, 클립이 없으면 조용히 무시)
/// 전역 사운드 매니저 - 씬 세팅 불필요 (첫 호출 시 스스로 생성, DontDestroyOnLoad).
///
/// 훅 지도 (어디서 무엇이 나는가):
///  포탑: TurretAttackExecutor.Execute 발사(Shot) / 폭발 착탄 explosion / 장판 field / 연쇄 chain / TurretSlot 투입 insert·레벨업 levelup·마비 stun/freeze/overheat·파손 break·식음 cool / SlotMarkerUI 냉각 cool·얼음 die_crystal·감전 털기 lever / TurretSlotManager 진화 fusion
///  손님: HitFeel.OnHit 명중 = 재질(Hit) + 속성 겹침(Accent), 크리 hit_crit, 물리 튕김 ricochet / Enemy.Die 처치 = 재질(Die), 큰 손님 kill_big / Enemy.AttackTrain 공격 = 종류(Attack)
///  보스: BossEnemy 등장·발악 울음(BossRoar - 보스 버스) / 패턴 준비 boss_cast (경고 버스) / 낙뢰 착탄 bolt_strike / 전환·패턴 폭음 = PlayIn(Boss, explosion) / 눈사태 울음 = PlayIn(Warning, ...)
///  주방: CookingMinigame.StartGame 조리법(Cook) / KitchenEventManager 사고 = 종류별 현장음(Event) 하나, 결과 event_resolve/fail / ItemManager.Acquire relic / GameManager 정차 gold
///  사람: MerchantUI 안킬로 anky / SpinoBetUI 스피노 spino / 정차·출발 train_whistle
///  배경음: GameManager 로비 bgm_main · 정차 bgm_maintenance · 패배·승리 MusicStop / WaveManager 지역 진입 = 맵 곡(SoundKeys.MapMusic) · 같은 지역 출발 = 이어서 / BossEnemy 등장 = 보스 곡 · 처치 = MusicStop
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;

    private const int SFX_POOL = 20;              // 예산 합 14 + 끊기는 중인 소리 · 여유
    private const int MAX_VARIANTS = 9;           // 키_v01 ~ _v09
    private const string BGM_MAIN = "bgm_main";

    // ── 효과음 소리 (소스 하나 = 소리 하나) ──
    private AudioSource[] sfxSources;
    private string[] vKey;        // 이 소스에서 나는 키 (null = 비었다)
    private SfxBus[] vBus;
    private int[] vPri;           // 우선순위 0 ~ 100
    private SfxFamily[] vFam;     // 갈래 (명중 · 처치 상한용)
    private float[] vBase;        // 버스 덕킹 전 볼륨 (= 들리는 크기의 어림 - 거리 감쇠 포함)
    private float[] vStart;       // 시작 실시간
    private float[] vFadeAt;      // 끊기기 시작한 실시간 (-1 = 아님)
    private float[] vFadeFrom;    // 끊기기 시작할 때 볼륨
    private float[] vFadeDur;     // 끊는 페이드 길이 (초)

    private readonly Dictionary<string, AudioClip[]> setCache = new Dictionary<string, AudioClip[]>();
    private readonly Dictionary<string, AudioClip> musicCache = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, int> lastVariant = new Dictionary<string, int>();
    private readonly Dictionary<string, float> lastPlayTime = new Dictionary<string, float>();   // 간격을 세는 이름 (문맥 재생은 "키@버스") -> 마지막 재생 실시간
    private readonly Dictionary<string, SoundMix.Def> defCache = new Dictionary<string, SoundMix.Def>();   // 키 -> 믹스 값 (부모까지 찾은 결과)
    private readonly HashSet<string> missingWarned = new HashSet<string>();

    /// <summary>끊을 소리를 찾는 묶음 - 같은 키 / 같은 갈래 / 같은 버스</summary>
    private enum VoiceGroup { SameKey, SameFamily, SameBus }

    // ── 경고 덕킹 ──
    private float warnMusic = 1f;        // 지금 배경음 배율
    private float warnCombat = 1f;       // 지금 일반 전투음 배율

    // ── 배경음 (두 소스 - 지금 곡 / 나가는 곡) ──
    private AudioSource bgmSource;       // 지금 곡
    private AudioSource bgmOut;          // 나가는 곡 (교차 전환 동안만 소리가 난다)
    private string bgmKey = "", bgmOutKey = "";
    private float bgmCur = 0f, bgmTarget = 0f, bgmRate = 1f / 1.5f;   // 지금 곡 배율 · 목표 · 초당 변화
    private float bgmOutCur = 0f, bgmOutRate = 1f;
    private readonly Dictionary<string, int> musicPos = new Dictionary<string, int>();   // 곡이 나갈 때 멈춘 자리 (샘플) - 같은 곡으로 돌아오면 이어서
    private readonly HashSet<string> bgmDuckReasons = new HashSet<string>();             // 예전 긴 덕킹 이유 (SoundLongDuckOn 일 때만)
    private float bgmCutUntil = 0f;      // 이 실시간까지 배경음이 뚝 끊긴다 (BgmCut)
    private const float CUT_RETURN_SEC = 0.12f;   // 끊김이 풀릴 때 bgm_main 이 아닌 곡이 돌아오는 시간 (박을 모르는 곡이 뚝 돌아오면 튄다)

    // ─────────────────────────────────────────────
    // 볼륨 (PlayerPrefs 영구 저장)
    // ─────────────────────────────────────────────
    public static float SfxVolume
    {
        get { return PlayerPrefs.GetFloat("WDT_VolSFX", 0.8f); }
        set { PlayerPrefs.SetFloat("WDT_VolSFX", Mathf.Clamp01(value)); }
    }

    /// <summary>배경음 볼륨. v3: 기본 0.4 -> 0.7 (새 음악은 효과음과 같은 크기 위계로 만들어졌다 - 설정을 저장한 적 있으면 그 값)</summary>
    public static float BgmVolume
    {
        get { return PlayerPrefs.GetFloat("WDT_VolBGM", 0.7f); }
        set
        {
            PlayerPrefs.SetFloat("WDT_VolBGM", Mathf.Clamp01(value));
            if (instance != null) instance.ApplyBgmVolume();
        }
    }

    /// <summary>v3: 지금 도는 배경음 키 ("" = 없음)</summary>
    public static string CurrentMusic { get { return instance != null ? instance.bgmKey : ""; } }

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
        instance.vKey = new string[SFX_POOL];
        instance.vBus = new SfxBus[SFX_POOL];
        instance.vPri = new int[SFX_POOL];
        instance.vFam = new SfxFamily[SFX_POOL];
        instance.vBase = new float[SFX_POOL];
        instance.vStart = new float[SFX_POOL];
        instance.vFadeAt = new float[SFX_POOL];
        instance.vFadeFrom = new float[SFX_POOL];
        instance.vFadeDur = new float[SFX_POOL];
        for (int i = 0; i < SFX_POOL; i++)
        {
            instance.sfxSources[i] = go.AddComponent<AudioSource>();
            instance.sfxSources[i].playOnAwake = false;
            instance.sfxSources[i].loop = false;
            instance.vFadeAt[i] = -1f;
        }

        instance.bgmSource = go.AddComponent<AudioSource>();
        instance.bgmSource.playOnAwake = false;
        instance.bgmSource.loop = true;
        instance.bgmOut = go.AddComponent<AudioSource>();
        instance.bgmOut.playOnAwake = false;
        instance.bgmOut.loop = true;

        return instance;
    }

    private void Update()
    {
        TickWarnDuck();
        TickVoices();
        TickMain();
        TickLayers();   // v2.3: 배경음 배율을 정한 뒤에 - 끊김이 풀리는 프레임에 배경음과 층이 같이 오른다
    }

    // ─────────────────────────────────────────────
    // 효과음 재생
    // ─────────────────────────────────────────────

    /// <summary>효과음 재생. 클립 없으면 조용히 무시</summary>
    public static void Play(string key)
    {
        PlayInternal(key, 1f, -1f, 1f, SfxBus.Auto);
    }

    /// <summary>효과음 재생 (볼륨 배율 + 피치 흔들기 폭. pitchJitter 가 0 미만이면 버스 기본값)</summary>
    public static void Play(string key, float volumeMul, float pitchJitter)
    {
        PlayInternal(key, volumeMul, pitchJitter, 1f, SfxBus.Auto);
    }

    /// <summary>
    /// v3: 호출 문맥으로 버스를 정해서 재생 - 같은 파일이 쓰임새마다 다른 예산을 쓸 때 (폭발: 포탄 착탄 = 전투 / 보스 전환 = 보스. 보스 울음: 등장 = 보스 / 눈사태 예고 = 경고).
    /// 경고 = 우선순위 95, 보스 = 90 으로 덮는다
    /// </summary>
    public static void PlayIn(SfxBus bus, string key, float volumeMul)
    {
        PlayInternal(key, volumeMul, -1f, 1f, bus);
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
        PlayInternal(key, volumeMul, -1f, dist, SfxBus.Auto);
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

    private static void PlayInternal(string key, float volumeMul, float pitchJitter, float distMul, SfxBus busOverride)
    {
        if (string.IsNullOrEmpty(key)) return;
        SoundManager sm = Get();
        float now = Time.unscaledTime;

        SoundMix.Def def = sm.ResolveDef(key);
        SfxBus bus = def.bus;
        SfxFamily fam = def.family;
        int pri = def.priority;
        string slotKey = key;   // 간격·동시 수를 세는 이름
        if (busOverride != SfxBus.Auto)
        {
            // 문맥 재생 (PlayIn): 같은 파일의 다른 쓰임새 - 간격·동시 수를 따로 세고 갈래 상한도 안 탄다
            bus = busOverride;
            fam = SfxFamily.None;
            slotKey = key + "@" + (int)busOverride;
            if (busOverride == SfxBus.Warning) pri = 95;        // 실제 위험 예고 문맥
            else if (busOverride == SfxBus.Boss) pri = 90;      // 보스 등장·큰 행동 문맥
        }

        // 같은 키 최소 간격 (실시간)
        float last;
        if (sm.lastPlayTime.TryGetValue(slotKey, out last) && now - last < def.gap) return;

        AudioClip clip = sm.PickClip(key);
        if (clip == null) return;
        float baseVol = Mathf.Clamp01(SfxVolume * volumeMul * distMul);

        // 끊을 소리 - 같은 키 동시 수 -> 갈래 상한 -> 버스 예산. 단계마다 차 있으면 가장 약한 소리와 견줘 이기면 끊고 지면 안 낸다.
        // 앞 단계에서 끊으면 뒤 단계에도 자리가 난다 (같은 키 = 같은 갈래 = 같은 버스) - 한 번 재생에 끊기는 소리는 많아야 하나
        int weakest;
        if (sm.CountVoices(VoiceGroup.SameKey, slotKey, fam, bus, out weakest) >= Mathf.Max(1, def.maxVoices))
        {
            if (!sm.NewBeats(pri, baseVol, weakest, bus, now)) return;
            sm.FadeVoice(weakest);
        }
        int famCap = FamilyCap(fam);
        if (famCap > 0 && sm.CountVoices(VoiceGroup.SameFamily, slotKey, fam, bus, out weakest) >= famCap)
        {
            if (!sm.NewBeats(pri, baseVol, weakest, bus, now)) return;
            sm.FadeVoice(weakest);
        }
        if (sm.CountVoices(VoiceGroup.SameBus, slotKey, fam, bus, out weakest) >= Mathf.Max(1, Budget(bus)))
        {
            if (!sm.NewBeats(pri, baseVol, weakest, bus, now)) return;
            sm.FadeVoice(weakest);
        }

        // 빈 소스 (끊기는 중인 소스는 아직 소리가 난다 - 다른 소스를 쓴다)
        int slot = -1;
        for (int i = 0; i < SFX_POOL; i++)
            if (sm.vKey[i] == null && !sm.sfxSources[i].isPlaying) { slot = i; break; }
        if (slot < 0)
        {
            // 풀이 다 찼다 = 끊기는 중인 소리가 몰린 순간 (예산 합 14 < 풀 20 이라 나머지는 끊기는 중이다) - 그 소스를 바로 멈추고 쓴다
            for (int i = 0; i < SFX_POOL; i++)
                if (sm.vKey[i] == null || sm.vFadeAt[i] >= 0f) { slot = i; break; }
            if (slot < 0) return;
            sm.sfxSources[slot].Stop();
            sm.vKey[slot] = null;
        }

        AudioSource src = sm.sfxSources[slot];
        float jitter = pitchJitter >= 0f ? pitchJitter : (bus == SfxBus.Combat || bus == SfxBus.Small ? GameBalance.SoundPitchJitter : 0f);
        src.pitch = 1f + Random.Range(-jitter, jitter);
        src.clip = clip;
        src.volume = baseVol * (bus == SfxBus.Combat ? sm.warnCombat : 1f);
        src.priority = Mathf.Clamp(Mathf.RoundToInt((100 - pri) * 2.56f), 0, 256);   // 공통 점수(높을수록 중요) -> 유니티(0 이 가장 중요)
        src.Play();
        sm.vKey[slot] = slotKey; sm.vBus[slot] = bus; sm.vFam[slot] = fam; sm.vPri[slot] = pri; sm.vBase[slot] = baseVol; sm.vStart[slot] = now; sm.vFadeAt[slot] = -1f;
        sm.lastPlayTime[slotKey] = now;

        if (bus == SfxBus.Warning) sm.BeginWarnDuck();
    }

    /// <summary>키의 믹스 값 - 표에 없으면 부모 키(SoundKeys.Parent)를 따라 올라가 찾는다. 끝까지 없으면 Unknown (키마다 로그 1회). 키마다 한 번만 찾고 기억한다</summary>
    private SoundMix.Def ResolveDef(string key)
    {
        SoundMix.Def def;
        if (defCache.TryGetValue(key, out def)) return def;
        bool found = false;
        string k = key;
        int guard = 0;
        while (!string.IsNullOrEmpty(k) && guard++ < 6)
        {
            if (SoundMix.TryGet(k, out def)) { found = true; break; }
            k = SoundKeys.Parent(k);
        }
        if (!found)
        {
            def = SoundMix.Unknown;
            Debug.Log("[SoundManager] 믹스 표에 없는 키 - 일반 전투 · 우선순위 35 로 낸다: " + key);
        }
        defCache[key] = def;
        return def;
    }

    /// <summary>묶음 안의 (끊기는 중이 아닌) 소리 수와 그중 가장 약한 소리 (없으면 -1)</summary>
    private int CountVoices(VoiceGroup group, string slotKey, SfxFamily fam, SfxBus bus, out int weakest)
    {
        int count = 0;
        weakest = -1;
        for (int i = 0; i < SFX_POOL; i++)
        {
            if (vKey[i] == null || vFadeAt[i] >= 0f) continue;
            if (group == VoiceGroup.SameKey && vKey[i] != slotKey) continue;
            if (group == VoiceGroup.SameFamily && vFam[i] != fam) continue;
            if (group == VoiceGroup.SameBus && vBus[i] != bus) continue;
            count++;
            if (weakest < 0 || Weaker(i, weakest)) weakest = i;
        }
        return count;
    }

    /// <summary>소리 a 가 b 보다 약한가 - 우선순위 낮은 것 -> 작게 들리는 것 -> 오래된 것</summary>
    private bool Weaker(int a, int b)
    {
        if (vPri[a] != vPri[b]) return vPri[a] < vPri[b];
        return Quieter(a, b);
    }

    /// <summary>
    /// 새 소리(우선순위 pri, 크기 vol)가 소리 i 를 끊고 들어가나. 우선순위가 높으면 예 · 낮으면 아니오 · 같으면 더 작게 들리는 새 소리만 아니오
    /// (멀리서 난 작은 폭발이 가까운 큰 폭발을 끊지 않는다. 같은 크기면 새 것 - 반복은 새로 치는 게 낫다).
    /// 긴급 경고는 시작한 지 SoundWarnProtectSec 안 된 경고를 끊지 않는다 (머리가 잘리면 무슨 경고인지 모른다)
    /// </summary>
    private bool NewBeats(int pri, float vol, int i, SfxBus bus, float now)
    {
        if (i < 0) return false;
        if (pri > vPri[i]) return true;
        if (pri < vPri[i]) return false;
        if (bus == SfxBus.Warning && now - vStart[i] < GameBalance.SoundWarnProtectSec) return false;
        return vol >= vBase[i] - 0.001f;
    }

    /// <summary>갈래 상한 (0 = 없음): 명중(속성 장식 포함) · 처치만. 발사 · 손님 공격은 버스 예산만</summary>
    private static int FamilyCap(SfxFamily fam)
    {
        switch (fam)
        {
            case SfxFamily.Hit: return GameBalance.SoundCapHit;
            case SfxFamily.Death: return GameBalance.SoundCapDeath;
            default: return 0;
        }
    }

    private static int Budget(SfxBus bus)
    {
        switch (bus)
        {
            case SfxBus.Warning: return GameBalance.SoundVoicesWarning;
            case SfxBus.Boss: return GameBalance.SoundVoicesBoss;
            case SfxBus.Small: return GameBalance.SoundVoicesSmall;
            default: return GameBalance.SoundVoicesCombat;
        }
    }

    /// <summary>a 가 b 보다 작게 들리나 (같으면 오래된 쪽)</summary>
    private bool Quieter(int a, int b)
    {
        if (vBase[a] < vBase[b] - 0.001f) return true;
        if (vBase[a] > vBase[b] + 0.001f) return false;
        return vStart[a] < vStart[b];
    }

    /// <summary>
    /// 소리 i 를 끊는다 - 페이드 SoundStealFadeSec(12ms) 이되 적어도 2.5 프레임 (60fps 한 프레임 = 17ms: 12ms 면 다음 프레임에 이미 끝나 볼륨 단계 없이 뚝 멈춘다).
    /// 한 프레임 전에 시작한 셈으로 쳐서 이 프레임에 바로 한 단계 내린다 (새 소리와 같은 프레임에 줄기 시작한다)
    /// </summary>
    private void FadeVoice(int i)
    {
        if (i < 0 || vKey[i] == null || vFadeAt[i] >= 0f) return;
        float frame = Mathf.Clamp(Time.unscaledDeltaTime, 0.004f, 0.05f);
        vFadeDur[i] = Mathf.Max(GameBalance.SoundStealFadeSec, 2.5f * frame);
        vFadeAt[i] = Mathf.Max(0f, Time.unscaledTime - frame);
        vFadeFrom[i] = sfxSources[i].volume;
        sfxSources[i].volume = vFadeFrom[i] * Mathf.Clamp01(1f - frame / vFadeDur[i]);
    }

    /// <summary>매 프레임: 끝난 소리 비우기 · 끊기는 소리 페이드 · 일반 전투음에 경고 덕킹</summary>
    private void TickVoices()
    {
        if (sfxSources == null) return;
        float now = Time.unscaledTime;
        for (int i = 0; i < SFX_POOL; i++)
        {
            if (vKey[i] == null) continue;
            AudioSource src = sfxSources[i];
            if (!src.isPlaying) { vKey[i] = null; continue; }
            if (vFadeAt[i] >= 0f)
            {
                float k = (now - vFadeAt[i]) / Mathf.Max(0.001f, vFadeDur[i]);
                if (k >= 1f) { src.Stop(); vKey[i] = null; continue; }
                src.volume = vFadeFrom[i] * (1f - k);
            }
            else if (vBus[i] == SfxBus.Combat) src.volume = vBase[i] * warnCombat;
        }
    }

    // ─────────────────────────────────────────────
    // v3: 경고 덕킹 - 긴급 경고가 울리는 동안만 배경음 · 일반 전투음을 낮춘다
    // ─────────────────────────────────────────────
    private static float Db(float db) { return Mathf.Pow(10f, db / 20f); }

    /// <summary>
    /// 경고 소리가 시작하는 그 프레임에 바로 낮춘다 - 걸리는 시간 SoundWarnDuckAttackSec(20ms)는 한 프레임보다 짧아서 사실상 바로다 (늦게 걸리면 경고 머리가 묻힌다).
    /// 겹친 경고는 더 낮추지 않는다. 풀리는 건 경고가 다 끝난 뒤 (TickWarnDuck)
    /// </summary>
    private void BeginWarnDuck()
    {
        warnMusic = Mathf.Min(warnMusic, Db(GameBalance.SoundWarnDuckMusicDb));
        warnCombat = Mathf.Min(warnCombat, Db(GameBalance.SoundWarnDuckCombatDb));
        ApplyBgmVolume();
        for (int i = 0; i < SFX_POOL; i++)
            if (vKey[i] != null && vFadeAt[i] < 0f && vBus[i] == SfxBus.Combat) sfxSources[i].volume = vBase[i] * warnCombat;
    }

    /// <summary>긴급 경고 버스에서 지금 소리가 나나 (끊기는 중인 것은 뺀다) - 덕킹은 실제로 나는 동안만 (끊긴 경고 · 피치가 바뀐 경고의 길이를 어림하지 않는다)</summary>
    private bool WarningSounding()
    {
        if (sfxSources == null) return false;
        for (int i = 0; i < SFX_POOL; i++)
            if (vKey[i] != null && vBus[i] == SfxBus.Warning && vFadeAt[i] < 0f && sfxSources[i].isPlaying) return true;
        return false;
    }

    private void TickWarnDuck()
    {
        bool hold = WarningSounding();
        float dt = Time.unscaledDeltaTime;
        warnMusic = Approach(warnMusic, hold ? Db(GameBalance.SoundWarnDuckMusicDb) : 1f, Db(GameBalance.SoundWarnDuckMusicDb), dt);
        warnCombat = Approach(warnCombat, hold ? Db(GameBalance.SoundWarnDuckCombatDb) : 1f, Db(GameBalance.SoundWarnDuckCombatDb), dt);
    }

    /// <summary>덕킹 한 단계: 내려갈 땐 AttackSec, 올라올 땐 ReleaseSec 에 한 번 다 움직인다 (low = 덕킹 바닥)</summary>
    private static float Approach(float cur, float target, float low, float dt)
    {
        if (Mathf.Abs(cur - target) < 0.0001f) return target;
        float span = Mathf.Max(0.01f, 1f - low);
        float sec = target < cur ? GameBalance.SoundWarnDuckAttackSec : GameBalance.SoundWarnDuckReleaseSec;
        return Mathf.MoveTowards(cur, target, span * dt / Mathf.Max(0.001f, sec));
    }

    // ─────────────────────────────────────────────
    // 클립 (Resources/Sounds/ + 캐시)
    // ─────────────────────────────────────────────
    /// <summary>키의 변형 묶음: 키_v01 ~ _v09 (끊기는 데까지). 없으면 예전 이름 키 하나. 그것도 없으면 빈 묶음 (키마다 로그 1회)</summary>
    private AudioClip[] LoadSet(string key)
    {
        AudioClip[] set;
        if (setCache.TryGetValue(key, out set)) return set;
        List<AudioClip> list = new List<AudioClip>();
        for (int v = 1; v <= MAX_VARIANTS; v++)
        {
            AudioClip c = Resources.Load<AudioClip>("Sounds/" + key + "_v" + v.ToString("00"));
            if (c == null) break;
            list.Add(c);
        }
        if (list.Count == 0)
        {
            AudioClip c = Resources.Load<AudioClip>("Sounds/" + key);
            if (c != null) list.Add(c);
        }
        set = list.ToArray();
        setCache[key] = set;
        if (set.Length == 0 && missingWarned.Add(key))
            Debug.Log("[SoundManager] 클립 없음 (무시하고 진행): Resources/Sounds/" + key + "_v01 / " + key);
        return set;
    }

    /// <summary>키(없으면 부모 키 - SoundKeys.Parent)의 변형 하나. 직전에 고른 변형은 연속으로 안 고른다</summary>
    private AudioClip PickClip(string key)
    {
        string k = key;
        int guard = 0;
        while (!string.IsNullOrEmpty(k) && guard++ < 5)
        {
            AudioClip[] set = LoadSet(k);
            if (set.Length == 1) return set[0];
            if (set.Length > 1)
            {
                int lastI;
                if (!lastVariant.TryGetValue(k, out lastI)) lastI = -1;
                int i = Random.Range(0, set.Length - (lastI >= 0 ? 1 : 0));
                if (lastI >= 0 && i >= lastI) i++;
                lastVariant[k] = i;
                return set[i];
            }
            k = SoundKeys.Parent(k);
        }
        return null;
    }

    /// <summary>배경음 한 곡 (변형 없음). 없으면 null - 로그 1회</summary>
    private AudioClip LoadMusic(string key)
    {
        AudioClip clip;
        if (musicCache.TryGetValue(key, out clip)) return clip;
        clip = Resources.Load<AudioClip>("Sounds/" + key);
        musicCache[key] = clip;
        if (clip == null && missingWarned.Add(key)) Debug.Log("[SoundManager] 배경음 없음: Resources/Sounds/" + key);
        return clip;
    }

    // ─────────────────────────────────────────────
    // 배경음 (두 소스 교차 전환)
    // ─────────────────────────────────────────────
    /// <summary>로비·견습의 배경음 (예전 진입점). 예전 긴 덕킹을 잊고 MusicLobbyFadeSec 에 걸쳐 그 곡으로 (같은 곡이 돌고 있으면 그대로)</summary>
    public static void PlayBGM(string key)
    {
        SoundManager sm = Get();
        sm.bgmDuckReasons.Clear();
        MusicTo(key, GameBalance.MusicLobbyFadeSec, false);
    }

    /// <summary>
    /// v3: 배경음을 key 로. 같은 곡이 돌고 있으면 그 자리에서 계속 (내려가던 중이면 다시 올린다). 다른 곡이면 fadeSec 에 걸쳐 진폭 선형으로 겹쳐 바꾼다.
    /// resume = 그 곡이 전에 나갈 때 멈춘 자리부터 (같은 지역으로 돌아올 때 · 정차 곡). 파일이 없으면 bgm_main
    /// </summary>
    public static void MusicTo(string key, float fadeSec, bool resume)
    {
        if (string.IsNullOrEmpty(key)) return;
        SoundManager sm = Get();
        string use = key;
        AudioClip clip = sm.LoadMusic(use);
        if (clip == null && key != BGM_MAIN) { use = BGM_MAIN; clip = sm.LoadMusic(use); }
        if (clip == null) return;
        float rate = fadeSec > 0.001f ? 1f / fadeSec : 1000f;

        // 1) 같은 곡이 돌고 있다 - 그 자리에서 계속
        if (sm.bgmSource.clip == clip && sm.bgmSource.isPlaying && sm.bgmKey == use)
        {
            sm.bgmTarget = 1f; sm.bgmRate = rate;
            return;
        }
        // 2) 나가던 곡으로 되돌아온다 - 두 소스를 맞바꾼다
        if (sm.bgmOut.clip == clip && sm.bgmOut.isPlaying && sm.bgmOutKey == use)
        {
            sm.SwapMusicSources();
            sm.bgmTarget = 1f; sm.bgmRate = rate; sm.bgmOutRate = rate;
            if (use != BGM_MAIN) sm.StopLayers();
            return;
        }
        // 3) 새 곡. 소리 나는 곡은 둘까지 - 둘 다 돌고 있으면 더 작게 들리는 쪽을 바로 멈춘다 (큰 쪽이 뚝 끊기면 다 들린다)
        if (sm.bgmOut.isPlaying && sm.bgmOutCur > sm.bgmCur)
        {
            // 나가는 곡이 아직 더 크다 (방금 들어오기 시작한 곡이 있다) - 들어오던 곡을 멈추고 그 자리에 새 곡. 나가는 곡은 하던 대로 내려간다
            sm.SavePos(sm.bgmKey, sm.bgmSource);
            sm.bgmSource.Stop();
        }
        else
        {
            // 보통: 지금 곡을 나가는 쪽으로 (나가던 곡이 있었으면 그게 더 작다 - 바로 멈춘다)
            if (sm.bgmOut.isPlaying) { sm.SavePos(sm.bgmOutKey, sm.bgmOut); sm.bgmOut.Stop(); }
            sm.SwapMusicSources();
            if (!sm.bgmOut.isPlaying) sm.bgmOutCur = 0f;
        }
        sm.bgmOutRate = rate;

        int pos = 0;
        if (!resume || !sm.musicPos.TryGetValue(use, out pos) || pos <= 0 || pos >= clip.samples) pos = 0;
        sm.bgmSource.clip = clip;
        sm.bgmKey = use;
        sm.bgmCur = fadeSec > 0.001f ? 0f : 1f;
        sm.bgmTarget = 1f; sm.bgmRate = rate;
        if (use == BGM_MAIN)
        {
            // v2.3: 클라이맥스 층과 같은 오디오 시계 순간에 시작한다 (프레임 시각이 아니라 dspTime - 둘이 같은 샘플에서 출발해 박이 안 어긋난다)
            double startAt = AudioSettings.dspTime + 0.1;
            sm.bgmSource.PlayScheduled(startAt);
            sm.StartLayers(startAt);
        }
        else
        {
            sm.StopLayers();
            sm.bgmSource.Play();
            if (pos > 0) sm.bgmSource.timeSamples = pos;
        }
        sm.ApplyBgmVolume();
        Debug.Log("[SoundManager] 배경음 -> " + use + (pos > 0 ? " (멈춘 자리부터)" : "") + " / " + fadeSec.ToString("0.0") + "초");
    }

    /// <summary>
    /// v3: 배경음을 fadeSec 에 걸쳐 끈다 (보스 처치 · 승리 · 패배). 다 내려가면 멈춘 자리를 기억하고 멈춘다.
    /// 같이 도는 클라이맥스 층은 배경음 배율을 따라 같이 내려가고, 단독 클라이맥스 곡이 배경음 대신 돌고 있으면 그것도 같은 시간에 내린다
    /// </summary>
    public static void MusicStop(float fadeSec)
    {
        if (instance == null || instance.bgmSource == null) return;
        float rate = fadeSec > 0.001f ? 1f / fadeSec : 1000f;
        instance.bgmTarget = 0f;
        instance.bgmRate = rate;
        instance.bgmOutRate = Mathf.Max(instance.bgmOutRate, rate);
        if (instance.soloOn) LayerOut(LayerClimax, fadeSec);
    }

    /// <summary>v3: 새 운행 - 지난 운행 곡들의 멈춘 자리와 예전 긴 덕킹을 잊는다 (곡은 그대로 - 웨이브 1 의 지역 전환이 바꾼다)</summary>
    public static void MusicNewRun()
    {
        SoundManager sm = Get();
        sm.musicPos.Clear();
        sm.bgmDuckReasons.Clear();
    }

    public static void StopBGM()
    {
        if (instance == null || instance.bgmSource == null) return;
        instance.bgmTarget = 0f; instance.bgmCur = 0f; instance.bgmSource.Stop(); instance.bgmKey = "";
        instance.bgmOutCur = 0f; instance.bgmOut.Stop(); instance.bgmOutKey = "";
        instance.StopLayers();   // v2.3
    }

    private void SwapMusicSources()
    {
        AudioSource s = bgmSource; bgmSource = bgmOut; bgmOut = s;
        string k = bgmKey; bgmKey = bgmOutKey; bgmOutKey = k;
        float c = bgmCur; bgmCur = bgmOutCur; bgmOutCur = c;
    }

    private void SavePos(string key, AudioSource src)
    {
        if (string.IsNullOrEmpty(key) || src == null || src.clip == null || key == BGM_MAIN) return;
        musicPos[key] = src.timeSamples;
    }

    /// <summary>매 프레임 (실시간 - 일시정지·시간 정지 카드 중에도): 지금 곡은 목표로, 나가는 곡은 0 으로. 다 내려간 곡은 자리를 기억하고 멈춘다</summary>
    private void TickMain()
    {
        if (bgmSource == null) return;
        float dt = Time.unscaledDeltaTime;
        float want = bgmTarget * (bgmDuckReasons.Count > 0 ? GameBalance.SoundLongDuckMul : 1f) * (soloOn ? 0f : 1f);   // v2.3: 단독 곡이 대신하는 동안 배경음은 소리 0 으로 돈다
        if (Mathf.Abs(bgmCur - want) > 0.0001f) bgmCur = Mathf.MoveTowards(bgmCur, want, bgmRate * dt);
        if (bgmTarget <= 0f && bgmCur <= 0f && bgmSource.isPlaying && !soloOn)   // 단독 클라이맥스 곡이 내려가는 중이면 그게 끝난 뒤에 (먼저 멈추면 StopLayers 가 그 곡을 뚝 끊는다)
        {
            SavePos(bgmKey, bgmSource);
            bgmSource.Stop();
            bgmKey = "";
            StopLayers();
        }
        if (bgmOut.isPlaying)
        {
            bgmOutCur = Mathf.MoveTowards(bgmOutCur, 0f, bgmOutRate * dt);
            if (bgmOutCur <= 0f) { SavePos(bgmOutKey, bgmOut); bgmOut.Stop(); bgmOutKey = ""; }
        }
        ApplyBgmVolume();
    }

    private void ApplyBgmVolume()
    {
        float m = BgmVolume * warnMusic * CutMul();
        if (bgmSource != null) bgmSource.volume = m * bgmCur;
        if (bgmOut != null) bgmOut.volume = m * bgmOutCur;
    }

    /// <summary>BgmCut 배율: 끊긴 동안 0. 풀릴 때 bgm_main 은 바로 1 (클라이맥스 숨 - 박 머리에 돌아온다), 다른 곡은 CUT_RETURN_SEC 에 걸쳐</summary>
    private float CutMul()
    {
        float now = Time.unscaledTime;
        if (now < bgmCutUntil) return 0f;
        if (bgmKey == BGM_MAIN) return 1f;
        return Mathf.Clamp01((now - bgmCutUntil) / CUT_RETURN_SEC);
    }

    /// <summary>v2.2: 배경음을 sec 초(실시간) 동안 뚝 끊었다가 돌린다 (끊을 땐 페이드 없음). 디 오리지널 등장의 전조 - 침묵 뒤 낮은 기적 / 클라이맥스의 숨. v3: 맵·보스 곡은 0.12초에 걸쳐 돌아온다</summary>
    public static void BgmCut(float sec)
    {
        if (instance == null || instance.bgmSource == null || sec <= 0f) return;
        instance.bgmCutUntil = Time.unscaledTime + sec;
        instance.ApplyBgmVolume();
    }

    /// <summary>
    /// v2: 사고·보스전 동안 배경음을 낮춘다 (x SoundLongDuckMul, 1.5초 걸쳐). reason 별로 켜고 끈다.
    /// v3: GameBalance.SoundLongDuckOn 일 때만 - 새 규칙은 경고가 울리는 순간만 낮춘다 (보스전엔 보스 곡이 제 크기로 돈다)
    /// </summary>
    public static void BgmDuck(string reason, bool on)
    {
        if (!GameBalance.SoundLongDuckOn) return;
        if (instance == null) { if (!on) return; Get(); }
        bool changed = on ? instance.bgmDuckReasons.Add(reason) : instance.bgmDuckReasons.Remove(reason);
        if (changed) instance.bgmRate = 1f / 1.5f;
    }

    // ─────────────────────────────────────────────
    // v2.3 (v9.24): 클라이맥스 층 - bgm_main 과 같은 순간에 소리 0 으로 같이 틀어 두고, 박 머리에 소리만 올린다
    //   같이 도는 층 = 길이가 bgm_main 의 정수분의 1 (px/sfx6.py 는 4마디 = 1/4). 아니면 단독 곡 - 클라이맥스 층만, 켜면 배경음 대신 처음부터
    //   v3: bgm_main 이 지금 곡일 때만 (맵·보스 곡 위에서는 박이 안 맞는다 - LayerIn 이 false)
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
            AudioClip clip = LoadMusic(LAYER_KEYS[i]);
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
    /// 층에 곱하는 배경음 쪽 배율 - 같이 도는 층은 bgm_main 의 지금 배율(긴 덕킹·끊김·들어오고 나가는 페이드)을 같이 탄다. 단독 곡은 배경음이 소리 0 이라 덕킹·끊김만 따른다.
    /// v3: 경고 덕킹도 같이. MusicStop 으로 배경음이 내려가면 층도 같이 내려간다 (예전 = 목표가 0 이 되는 순간 층만 뚝 끊겼다)
    /// </summary>
    private float LayerGain(int i)
    {
        if (Time.unscaledTime < bgmCutUntil) return 0f;
        if (layerSync[i]) return bgmKey == BGM_MAIN ? Mathf.Clamp01(bgmCur) * warnMusic : 0f;
        return (bgmDuckReasons.Count > 0 ? GameBalance.SoundLongDuckMul : 1f) * warnMusic;
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
        soloOn = false;
        bgmRate = 1f / 1.5f;   // 배경음은 1.5초 페이드로 돌아온다 (소리 0 으로 계속 돌고 있었다)
    }

    /// <summary>
    /// v2.3: 다음 박까지 남은 실시간 초 (minSec 이상) - bgm_main 의 지금 자리와 GameBalance.BgmBpm 으로 센다. 배경음이 안 돌면 minSec.
    /// 클라이맥스의 "숨"이 이 길이다 - 숨이 끝나는 순간이 박 머리. v3: bgm_main 이 아니면 minSec (다른 곡의 박을 모른다)
    /// </summary>
    public static float SecondsToNextBeat(float minSec)
    {
        if (instance == null || instance.bgmSource == null || instance.bgmSource.clip == null || !instance.bgmSource.isPlaying || instance.bgmKey != BGM_MAIN) return minSec;
        AudioClip c = instance.bgmSource.clip;
        int freq = c.frequency > 0 ? c.frequency : 44100;
        double beat = freq * 60.0 / Mathf.Max(30f, GameBalance.BgmBpm);
        double pos = instance.bgmSource.timeSamples;
        double next = System.Math.Ceiling((pos + minSec * freq) / beat) * beat;
        return (float)((next - pos) / freq);
    }

    /// <summary>
    /// v2.3: 층 i (LayerClimax / LayerLite) 를 delaySec(실시간) 뒤에 level 로 올린다 - 그 순간 바로 (페이드 없음).
    /// 같이 도는 층이 아니면 클라이맥스 층만 단독 곡으로 바꿔 튼다 (배경음은 소리 0). 파일이 없거나 쓸 수 없으면 false. v3: bgm_main 이 지금 곡이 아니어도 false
    /// </summary>
    public static bool LayerIn(int i, float level, float delaySec)
    {
        SoundManager sm = instance;
        if (sm == null || sm.layerSrc == null || i < 0 || i >= sm.layerSrc.Length || sm.layerSrc[i].clip == null) return false;
        if (sm.bgmKey != BGM_MAIN) return false;
        if (!sm.layerSync[i] && i != LayerClimax) return false;
        if (sm.layerSync[i] && !sm.layerSrc[i].isPlaying) return false;   // 같이 도는 층이 멈춰 있다 (bgm_main 이 나갔다 되돌아온 경우 - 박을 다시 맞출 수 없다)
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
}

/// <summary>
/// v2: 게임 데이터 -> 소리 키. 코드 곳곳이 문자열을 직접 쓰지 않고 여기서 받는다.
/// </summary>
public static class SoundKeys
{
    /// <summary>v3: 맵의 곡 (MapRoute 맵 번호). 황야의 끝은 일반 웨이브가 없어 디 오리지널 곡이 맵 곡을 겸한다</summary>
    public static string MapMusic(int map)
    {
        switch (map)
        {
            case MapRoute.Canyon: return "bgm_map_tesla";
            case MapRoute.Mine: return "bgm_map_cobalt";
            case MapRoute.Final: return "bgm_boss_original";
            default: return "bgm_map_copper";
        }
    }

    /// <summary>v3: 보스 곡</summary>
    public static string BossMusic(BossEnemy.BossKind kind)
    {
        switch (kind)
        {
            case BossEnemy.BossKind.RustClaw: return "bgm_boss_rust";
            case BossEnemy.BossKind.ThunderNest: return "bgm_boss_thunder";
            case BossEnemy.BossKind.Hibernator: return "bgm_boss_hibernator";
            default: return "bgm_boss_original";
        }
    }

    /// <summary>v3: 정차 곡</summary>
    public const string MaintenanceMusic = "bgm_maintenance";

    /// <summary>가족 키의 부모 (폴백용)</summary>
    public static string Parent(string key)
    {
        if (key.StartsWith("sfx_shot_fire_cone")) return "sfx_shot_fire";
        if (key.StartsWith("sfx_shot_") && key.EndsWith("_heavy")) return key.Substring(0, key.Length - 6);
        if (key.StartsWith("sfx_shot_")) return "sfx_shot";
        if (key.StartsWith("sfx_hit_") && key != "sfx_hit_crit") return "sfx_hit";
        if (key.StartsWith("sfx_die_")) return "sfx_enemy_die";
        if (key.StartsWith("sfx_boss_roar_")) return "sfx_boss_roar";
        if (key.StartsWith("sfx_ev_")) return "sfx_alarm";                  // v3 (v9.33): 사고 현장음이 없으면 공통 경보 (사고 시작은 현장음 하나만 낸다)
        if (key == "sfx_rail" || key == "sfx_launch_mortar") return "sfx_shot_heavy";
        if (key == "sfx_chain") return "sfx_shot_elec";
        if (key == "sfx_bolt_strike") return "sfx_chain";                    // v3: 보스 낙뢰 착탄 (예전엔 연쇄 번개 소리를 빌려 썼다)
        if (key == "sfx_field") return "sfx_shot_poison";
        if (key == "sfx_ricochet") return "sfx_hit";
        if (key == "sfx_whistle_low") return "sfx_train_whistle";
        if (key == "sfx_whistle_long") return "sfx_train_whistle";   // v2.3 (v9.24): 클라이맥스 긴 기적
        if (key == "sfx_anky" || key == "sfx_spino") return "sfx_train_whistle";   // v3: 안킬로·스피노 등장 (예전엔 기적을 빌려 썼다)
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
        if (key == "sfx_boss_cast") return "sfx_boss_roar";                // 패턴 준비 울음
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
