using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [SoundManager.cs] v1 (신규 파일)
/// 전역 사운드 매니저 - 씬 세팅 불필요 (첫 호출 시 스스로 생성).
///
/// 동작 원칙:
///  1) 클립이 없으면 "조용히 무시" - 사운드 파일을 아직 안 넣어도 게임은 정상 동작.
///     (경고 로그는 클립당 1회만 - 콘솔 도배 방지)
///  2) 클립은 Assets/Resources/Sounds/ 폴더에서 이름으로 로드한다.
///     예: SoundManager.Play("sfx_judge_perfect") -> Assets/Resources/Sounds/sfx_judge_perfect.wav(.ogg)
///  3) 같은 사운드 0.06초 내 재요청은 무시 (동시 사망 스팸 방지)
///  4) 볼륨은 PlayerPrefs 저장 (SFX/BGM 분리) - 나중에 설정 화면에서 조절
///
/// 필요한 클립 목록 (이 이름 그대로 넣으면 끝):
///  sfx_judge_perfect / sfx_judge_good / sfx_judge_bad   - 조리 판정
///  sfx_train_hit / sfx_enemy_die / sfx_explosion        - 전투
///  sfx_boss_warning / sfx_boss_groggy                   - 보스 (예고 경보/그로기)
///  sfx_parry / sfx_cannon_fire                          - 시그니처 기믹
///  sfx_wave_clear / sfx_train_whistle                   - 진행 (클리어/기적)
///  sfx_pickup / sfx_ui_click / sfx_augment_pick         - 기타
///  bgm_main                                             - 배경 음악 (루프)
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;

    private const int SFX_POOL = 8;
    private const float THROTTLE_SEC = 0.06f;

    private AudioSource[] sfxSources;
    private AudioSource bgmSource;
    private int nextSource = 0;

    private Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
    private HashSet<string> missingWarned = new HashSet<string>();
    private Dictionary<string, float> lastPlayTime = new Dictionary<string, float>();

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
        get { return PlayerPrefs.GetFloat("WDT_VolBGM", 0.5f); }
        set
        {
            PlayerPrefs.SetFloat("WDT_VolBGM", Mathf.Clamp01(value));
            if (instance != null && instance.bgmSource != null)
                instance.bgmSource.volume = Mathf.Clamp01(value);
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

    // ─────────────────────────────────────────────
    // 효과음 재생
    // ─────────────────────────────────────────────

    /// <summary>효과음 재생. 클립 없으면 조용히 무시.</summary>
    public static void Play(string key)
    {
        Play(key, 1f, 0.06f);
    }

    /// <summary>효과음 재생 (볼륨 배율 + 피치 랜덤 폭 지정)</summary>
    public static void Play(string key, float volumeMul, float pitchJitter)
    {
        SoundManager sm = Get();

        // 스팸 방지: 같은 키 0.06초 내 재요청 무시
        float last;
        if (sm.lastPlayTime.TryGetValue(key, out last) && Time.unscaledTime - last < THROTTLE_SEC)
            return;
        sm.lastPlayTime[key] = Time.unscaledTime;

        AudioClip clip = sm.LoadClip(key);
        if (clip == null) return;

        // 풀에서 소스 하나 순환 사용
        AudioSource src = sm.sfxSources[sm.nextSource];
        sm.nextSource = (sm.nextSource + 1) % SFX_POOL;

        src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);   // 미세 변주 (기계음 반복감 제거)
        src.PlayOneShot(clip, SfxVolume * volumeMul);
    }

    // ─────────────────────────────────────────────
    // 배경음
    // ─────────────────────────────────────────────
    public static void PlayBGM(string key)
    {
        SoundManager sm = Get();
        AudioClip clip = sm.LoadClip(key);
        if (clip == null) return;
        if (sm.bgmSource.clip == clip && sm.bgmSource.isPlaying) return;

        sm.bgmSource.clip = clip;
        sm.bgmSource.volume = BgmVolume;
        sm.bgmSource.Play();
    }

    public static void StopBGM()
    {
        if (instance != null && instance.bgmSource != null)
            instance.bgmSource.Stop();
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
}
