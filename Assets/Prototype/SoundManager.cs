using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [SoundManager.cs] v2 (v9.16 2026-09-29 손맛 2차 - 소리 한 번에. 유저: 8비트 말고 서부 증기기관·쇠와 화약, 포탑 종류별·손님 종류별로 소리가 달라야)
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
        // 배경음 페이드/덕킹 (unscaled - 일시정지·시간 정지 카드 중에도 자연스럽게)
        if (bgmSource == null) return;
        float target = bgmTarget * (bgmDuckReasons.Count > 0 ? 0.65f : 1f);
        if (Mathf.Abs(bgmCur - target) > 0.001f)
        {
            bgmCur = Mathf.MoveTowards(bgmCur, target, Time.unscaledDeltaTime / 1.5f);
            ApplyBgmVolume();
        }
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
        sm.bgmSource.Play();
    }

    public static void StopBGM()
    {
        if (instance != null && instance.bgmSource != null) { instance.bgmTarget = 0f; instance.bgmCur = 0f; instance.bgmSource.Stop(); }
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
