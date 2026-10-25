using System.Collections.Generic;

/// <summary>
/// [SoundMix.cs] v1 (신규, v9.33 2026-10-25 소리 팩 - tools/audio_v933.py 가 유저 제작 오디오의 metadata 로 만든다. 손으로 고치지 말고 스크립트를 다시 돌린다)
/// 효과음 키마다: 버스(게임 전체 예산 넷 중 어디) · 우선순위(0 = 가장 낮음, 100 = 가장 높음) · 같은 키 최소 간격(초) · 같은 키 동시 수.
/// 버스 = routing_manifest.json 의 voice_budgets: 일반 전투(무기·속성 장식·손님 공격·명중·처치·상태·보상·전환) / 긴급 경고 / 보스 전경 / 작은 소리·UI(발소리·획득·클릭·조리·손잡이).
/// 우선순위는 팩 값 그대로가 아니다 (줄 끝 "팩 우선순위" = 원래 값): 무기 발사 45 -> 30 · 명중 30 -> 40 · 처치 25 -> 50 · UI 클릭·창 15 -> 55.
///   팩 값이면 포탑 서넛이 쏠 때 발사음(45)이 일반 전투 8칸을 늘 채워 명중·처치가 들어갈 자리가 없고, 조리·투입 셋이 겹치면 클릭이 안 난다.
///   무기 낮음(속성 장식·연쇄) 25 · 중상(폭발·대포) 65 · 작살 45 는 그대로. 호출하는 쪽이 문맥으로 버스를 바꾸면(SoundManager.PlayIn) 경고 = 95, 보스 = 90 으로 덮는다.
/// 갈래(family) = 일반 전투 안에서 한 종류가 칸을 다 먹지 않게 하는 묶음: 명중(속성 장식 포함) · 처치는 GameBalance.SoundCapHit · SoundCapDeath 까지만 동시에.
///   발사 · 손님 공격은 상한 없이 표시만 (로그·나중 조정용).
/// 표에 없는 키 = SoundKeys.Parent 를 따라 올라가 찾고, 끝까지 없으면 일반 전투 · 35 · 0.06초 · 2 (SoundManager 가 키마다 한 번 로그). VS 2017 (C# 7.3) 호환
/// </summary>
public enum SfxFamily { None, Fire, Hit, Death, Attack }

public static class SoundMix
{
    public struct Def
    {
        public SfxBus bus; public SfxFamily family; public int priority; public float gap; public int maxVoices;
        public Def(SfxBus bus, SfxFamily family, int priority, float gap, int maxVoices) { this.bus = bus; this.family = family; this.priority = priority; this.gap = gap; this.maxVoices = maxVoices; }
    }

    public static readonly Def Unknown = new Def(SfxBus.Combat, SfxFamily.None, 35, 0.06f, 2);

    private static readonly Dictionary<string, Def> TABLE = new Dictionary<string, Def>
    {
        { "sfx_acc_elec", new Def(SfxBus.Combat, SfxFamily.Hit, 25, 0.120f, 2) },   // 무기 3개 0.10초
        { "sfx_acc_fire", new Def(SfxBus.Combat, SfxFamily.Hit, 25, 0.130f, 2) },   // 무기 3개 0.12초
        { "sfx_acc_ice", new Def(SfxBus.Combat, SfxFamily.Hit, 25, 0.130f, 2) },   // 무기 3개 0.12초
        { "sfx_acc_poison", new Def(SfxBus.Combat, SfxFamily.Hit, 25, 0.150f, 2) },   // 무기 3개 0.14초
        { "sfx_alarm", new Def(SfxBus.Warning, SfxFamily.None, 95, 1.500f, 1) },   // 이벤트 1개 0.50초
        { "sfx_anky", new Def(SfxBus.Combat, SfxFamily.None, 55, 0.250f, 1) },   // 이벤트 1개 0.56초
        { "sfx_atk_ankylo", new Def(SfxBus.Combat, SfxFamily.Attack, 45, 0.110f, 2) },   // 이벤트 1개 0.28초
        { "sfx_atk_bolt", new Def(SfxBus.Combat, SfxFamily.Attack, 45, 0.110f, 2) },   // 이벤트 1개 0.44초
        { "sfx_atk_cactus", new Def(SfxBus.Combat, SfxFamily.Attack, 45, 0.110f, 2) },   // 이벤트 1개 0.37초
        { "sfx_atk_carno", new Def(SfxBus.Combat, SfxFamily.Attack, 45, 0.110f, 2) },   // 이벤트 1개 0.48초
        { "sfx_atk_flame", new Def(SfxBus.Combat, SfxFamily.Attack, 45, 0.110f, 2) },   // 이벤트 1개 0.44초
        { "sfx_atk_fly", new Def(SfxBus.Combat, SfxFamily.Attack, 45, 0.110f, 2) },   // 이벤트 1개 0.35초
        { "sfx_atk_mammoth", new Def(SfxBus.Combat, SfxFamily.Attack, 45, 0.110f, 2) },   // 이벤트 1개 0.39초
        { "sfx_atk_mosa", new Def(SfxBus.Combat, SfxFamily.Attack, 45, 0.110f, 2) },   // 이벤트 1개 0.37초
        { "sfx_atk_necro", new Def(SfxBus.Combat, SfxFamily.Attack, 45, 0.110f, 2) },   // 이벤트 1개 0.40초
        { "sfx_atk_pachy", new Def(SfxBus.Combat, SfxFamily.Attack, 45, 0.110f, 2) },   // 이벤트 1개 0.40초
        { "sfx_atk_parasaur", new Def(SfxBus.Combat, SfxFamily.Attack, 45, 0.110f, 2) },   // 이벤트 1개 0.47초
        { "sfx_atk_ptera", new Def(SfxBus.Combat, SfxFamily.Attack, 45, 0.110f, 2) },   // 이벤트 1개 0.44초
        { "sfx_atk_raptor", new Def(SfxBus.Combat, SfxFamily.Attack, 45, 0.110f, 2) },   // 이벤트 1개 0.25초
        { "sfx_atk_scorpion", new Def(SfxBus.Combat, SfxFamily.Attack, 45, 0.110f, 2) },   // 이벤트 1개 0.26초
        { "sfx_atk_steel", new Def(SfxBus.Combat, SfxFamily.Attack, 45, 0.110f, 2) },   // 이벤트 1개 0.30초
        { "sfx_atk_tortoise", new Def(SfxBus.Combat, SfxFamily.Attack, 45, 0.110f, 2) },   // 이벤트 1개 0.38초
        { "sfx_augment_pick", new Def(SfxBus.Combat, SfxFamily.None, 55, 0.250f, 1) },   // 이벤트 1개 0.32초
        { "sfx_bolt_strike", new Def(SfxBus.Boss, SfxFamily.None, 90, 0.600f, 1) },   // 이벤트 1개 0.40초
        { "sfx_boss_cast", new Def(SfxBus.Warning, SfxFamily.None, 95, 1.500f, 1) },   // 이벤트 1개 0.38초
        { "sfx_boss_far_howl", new Def(SfxBus.Boss, SfxFamily.None, 90, 0.600f, 1) },   // 이벤트 1개 0.97초
        { "sfx_boss_groggy", new Def(SfxBus.Combat, SfxFamily.None, 75, 0.400f, 1) },   // 이벤트 1개 0.43초
        { "sfx_boss_land", new Def(SfxBus.Boss, SfxFamily.None, 90, 0.600f, 1) },   // 이벤트 1개 0.41초
        { "sfx_boss_roar", new Def(SfxBus.Boss, SfxFamily.None, 90, 0.600f, 1) },   // 이벤트 1개 1.45초
        { "sfx_boss_roar_hibernator", new Def(SfxBus.Boss, SfxFamily.None, 90, 0.600f, 1) },   // 이벤트 1개 1.44초
        { "sfx_boss_roar_lightning", new Def(SfxBus.Boss, SfxFamily.None, 90, 0.600f, 1) },   // 이벤트 1개 1.40초
        { "sfx_boss_roar_pack", new Def(SfxBus.Boss, SfxFamily.None, 90, 0.600f, 1) },   // 이벤트 1개 1.10초
        { "sfx_boss_warning", new Def(SfxBus.Warning, SfxFamily.None, 95, 1.500f, 1) },   // 이벤트 1개 0.47초
        { "sfx_break", new Def(SfxBus.Combat, SfxFamily.None, 75, 0.400f, 1) },   // 이벤트 1개 0.48초
        { "sfx_cannon_fire", new Def(SfxBus.Combat, SfxFamily.Fire, 65, 0.450f, 1) },   // 무기 3개 0.62초
        { "sfx_chain", new Def(SfxBus.Combat, SfxFamily.Hit, 25, 0.075f, 2) },   // 무기 3개 0.10초
        { "sfx_cook_boil", new Def(SfxBus.Small, SfxFamily.None, 50, 0.180f, 1) },   // 이벤트 1개 0.69초
        { "sfx_cook_fry", new Def(SfxBus.Small, SfxFamily.None, 50, 0.180f, 1) },   // 이벤트 1개 0.49초
        { "sfx_cook_grill", new Def(SfxBus.Small, SfxFamily.None, 50, 0.180f, 1) },   // 이벤트 1개 0.64초
        { "sfx_cool", new Def(SfxBus.Combat, SfxFamily.None, 75, 0.400f, 1) },   // 이벤트 1개 0.37초
        { "sfx_cue_ambush", new Def(SfxBus.Warning, SfxFamily.None, 95, 1.500f, 1) },   // 이벤트 1개 0.54초
        { "sfx_dash", new Def(SfxBus.Small, SfxFamily.None, 10, 0.090f, 2) },   // 이벤트 3개 0.22초
        { "sfx_die_crystal", new Def(SfxBus.Combat, SfxFamily.Death, 50, 0.130f, 2) },   // 이벤트 3개 0.52초 (팩 우선순위 25)
        { "sfx_die_iron", new Def(SfxBus.Combat, SfxFamily.Death, 50, 0.130f, 2) },   // 이벤트 3개 0.52초 (팩 우선순위 25)
        { "sfx_die_magma", new Def(SfxBus.Combat, SfxFamily.Death, 50, 0.130f, 2) },   // 이벤트 3개 0.52초 (팩 우선순위 25)
        { "sfx_die_scale", new Def(SfxBus.Combat, SfxFamily.Death, 50, 0.130f, 2) },   // 이벤트 3개 0.42초 (팩 우선순위 25)
        { "sfx_die_wing", new Def(SfxBus.Combat, SfxFamily.Death, 50, 0.130f, 2) },   // 이벤트 3개 0.40초 (팩 우선순위 25)
        { "sfx_enemy_die", new Def(SfxBus.Combat, SfxFamily.Death, 50, 0.100f, 2) },   // 이벤트 1개 0.22초 (팩 우선순위 35)
        { "sfx_ev_break", new Def(SfxBus.Warning, SfxFamily.None, 95, 1.500f, 1) },   // 이벤트 1개 0.48초
        { "sfx_ev_fire", new Def(SfxBus.Warning, SfxFamily.None, 95, 1.500f, 1) },   // 이벤트 1개 0.80초
        { "sfx_ev_intrusion", new Def(SfxBus.Warning, SfxFamily.None, 95, 1.500f, 1) },   // 이벤트 1개 0.60초
        { "sfx_ev_spill", new Def(SfxBus.Warning, SfxFamily.None, 95, 1.500f, 1) },   // 이벤트 1개 0.64초
        { "sfx_event_fail", new Def(SfxBus.Combat, SfxFamily.None, 75, 0.400f, 1) },   // 이벤트 1개 0.40초
        { "sfx_event_resolve", new Def(SfxBus.Combat, SfxFamily.None, 75, 0.400f, 1) },   // 이벤트 1개 0.47초
        { "sfx_explosion", new Def(SfxBus.Combat, SfxFamily.None, 65, 0.160f, 2) },   // 무기 3개 0.53초
        { "sfx_field", new Def(SfxBus.Combat, SfxFamily.Fire, 30, 0.300f, 1) },   // 무기 3개 0.34초 (팩 우선순위 45)
        { "sfx_fork_clank", new Def(SfxBus.Small, SfxFamily.None, 10, 0.090f, 2) },   // 이벤트 1개 0.36초
        { "sfx_freeze", new Def(SfxBus.Combat, SfxFamily.None, 75, 0.400f, 1) },   // 이벤트 1개 0.57초
        { "sfx_fusion", new Def(SfxBus.Combat, SfxFamily.None, 55, 0.250f, 1) },   // 이벤트 1개 0.85초
        { "sfx_game_over", new Def(SfxBus.Combat, SfxFamily.None, 80, 0.700f, 1) },   // 이벤트 1개 0.80초
        { "sfx_gold", new Def(SfxBus.Combat, SfxFamily.None, 55, 0.250f, 1) },   // 이벤트 1개 0.42초
        { "sfx_ground_crack", new Def(SfxBus.Boss, SfxFamily.None, 90, 0.600f, 1) },   // 이벤트 1개 0.81초
        { "sfx_harpoon", new Def(SfxBus.Combat, SfxFamily.Fire, 45, 0.180f, 2) },   // 무기 3개 0.37초
        { "sfx_hit", new Def(SfxBus.Combat, SfxFamily.Hit, 40, 0.100f, 2) },   // 이벤트 1개 0.14초 (팩 우선순위 35)
        { "sfx_hit_crit", new Def(SfxBus.Combat, SfxFamily.Hit, 40, 0.065f, 3) },   // 이벤트 3개 0.23초 (팩 우선순위 30)
        { "sfx_hit_crystal", new Def(SfxBus.Combat, SfxFamily.Hit, 40, 0.065f, 3) },   // 이벤트 3개 0.18초 (팩 우선순위 30)
        { "sfx_hit_iron", new Def(SfxBus.Combat, SfxFamily.Hit, 40, 0.065f, 3) },   // 이벤트 3개 0.17초 (팩 우선순위 30)
        { "sfx_hit_magma", new Def(SfxBus.Combat, SfxFamily.Hit, 40, 0.065f, 3) },   // 이벤트 3개 0.19초 (팩 우선순위 30)
        { "sfx_hit_scale", new Def(SfxBus.Combat, SfxFamily.Hit, 40, 0.065f, 3) },   // 이벤트 3개 0.12초 (팩 우선순위 30)
        { "sfx_hit_wing", new Def(SfxBus.Combat, SfxFamily.Hit, 40, 0.065f, 3) },   // 이벤트 3개 0.13초 (팩 우선순위 30)
        { "sfx_hp_ratchet", new Def(SfxBus.Small, SfxFamily.None, 15, 0.090f, 1) },   // 이벤트 1개 0.10초
        { "sfx_insert", new Def(SfxBus.Small, SfxFamily.None, 50, 0.180f, 1) },   // 이벤트 1개 0.30초
        { "sfx_iron_groan", new Def(SfxBus.Boss, SfxFamily.None, 90, 0.600f, 1) },   // 이벤트 1개 0.93초
        { "sfx_iron_scream", new Def(SfxBus.Boss, SfxFamily.None, 90, 0.600f, 1) },   // 이벤트 1개 0.38초
        { "sfx_judge_bad", new Def(SfxBus.Combat, SfxFamily.None, 75, 0.400f, 1) },   // 이벤트 1개 0.25초
        { "sfx_judge_good", new Def(SfxBus.Combat, SfxFamily.None, 55, 0.250f, 1) },   // 이벤트 1개 0.20초
        { "sfx_judge_perfect", new Def(SfxBus.Combat, SfxFamily.None, 55, 0.250f, 1) },   // 이벤트 1개 0.26초
        { "sfx_kill_big", new Def(SfxBus.Combat, SfxFamily.None, 80, 0.700f, 1) },   // 이벤트 1개 0.60초
        { "sfx_launch_mortar", new Def(SfxBus.Combat, SfxFamily.Fire, 30, 0.200f, 2) },   // 무기 3개 0.29초 (팩 우선순위 45)
        { "sfx_levelup", new Def(SfxBus.Combat, SfxFamily.None, 55, 0.250f, 1) },   // 이벤트 1개 0.46초
        { "sfx_lever", new Def(SfxBus.Small, SfxFamily.None, 50, 0.180f, 1) },   // 이벤트 1개 0.43초
        { "sfx_overheat", new Def(SfxBus.Combat, SfxFamily.None, 75, 0.400f, 1) },   // 이벤트 1개 0.46초
        { "sfx_parry", new Def(SfxBus.Combat, SfxFamily.None, 75, 0.400f, 1) },   // 이벤트 1개 0.41초
        { "sfx_pickup", new Def(SfxBus.Small, SfxFamily.None, 15, 0.090f, 1) },   // 이벤트 3개 0.14초
        { "sfx_rail", new Def(SfxBus.Combat, SfxFamily.Fire, 30, 0.130f, 2) },   // 무기 3개 0.25초 (팩 우선순위 45)
        { "sfx_relic", new Def(SfxBus.Combat, SfxFamily.None, 55, 0.250f, 1) },   // 이벤트 1개 0.80초
        { "sfx_ricochet", new Def(SfxBus.Combat, SfxFamily.Hit, 40, 0.065f, 3) },   // 이벤트 3개 0.22초 (팩 우선순위 30)
        { "sfx_rival_wheels", new Def(SfxBus.Boss, SfxFamily.None, 90, 0.600f, 1) },   // 이벤트 1개 1.16초
        { "sfx_shot", new Def(SfxBus.Combat, SfxFamily.Fire, 30, 0.100f, 2) },   // 이벤트 1개 0.25초 (팩 우선순위 35)
        { "sfx_shot_elec", new Def(SfxBus.Combat, SfxFamily.Fire, 30, 0.110f, 2) },   // 무기 3개 0.23초 (팩 우선순위 45)
        { "sfx_shot_elec_heavy", new Def(SfxBus.Combat, SfxFamily.Fire, 30, 0.180f, 2) },   // 무기 3개 0.34초 (팩 우선순위 45)
        { "sfx_shot_fire", new Def(SfxBus.Combat, SfxFamily.Fire, 30, 0.110f, 2) },   // 무기 3개 0.29초 (팩 우선순위 45)
        { "sfx_shot_fire_cone", new Def(SfxBus.Combat, SfxFamily.Fire, 30, 0.480f, 1) },   // 무기 3개 0.57초 (팩 우선순위 45)
        { "sfx_shot_fire_heavy", new Def(SfxBus.Combat, SfxFamily.Fire, 30, 0.180f, 2) },   // 무기 3개 0.43초 (팩 우선순위 45)
        { "sfx_shot_heavy", new Def(SfxBus.Combat, SfxFamily.Fire, 30, 0.100f, 2) },   // 이벤트 1개 0.36초 (팩 우선순위 35)
        { "sfx_shot_ice", new Def(SfxBus.Combat, SfxFamily.Fire, 30, 0.100f, 2) },   // 무기 3개 0.26초 (팩 우선순위 45)
        { "sfx_shot_ice_heavy", new Def(SfxBus.Combat, SfxFamily.Fire, 30, 0.170f, 2) },   // 무기 3개 0.38초 (팩 우선순위 45)
        { "sfx_shot_phys", new Def(SfxBus.Combat, SfxFamily.Fire, 30, 0.085f, 3) },   // 무기 3개 0.23초 (팩 우선순위 45)
        { "sfx_shot_phys_heavy", new Def(SfxBus.Combat, SfxFamily.Fire, 30, 0.145f, 2) },   // 무기 3개 0.34초 (팩 우선순위 45)
        { "sfx_shot_poison", new Def(SfxBus.Combat, SfxFamily.Fire, 30, 0.120f, 2) },   // 무기 3개 0.28초 (팩 우선순위 45)
        { "sfx_shot_poison_heavy", new Def(SfxBus.Combat, SfxFamily.Fire, 30, 0.180f, 2) },   // 무기 3개 0.39초 (팩 우선순위 45)
        { "sfx_spino", new Def(SfxBus.Combat, SfxFamily.None, 55, 0.250f, 1) },   // 이벤트 1개 0.56초
        { "sfx_step", new Def(SfxBus.Small, SfxFamily.None, 10, 0.120f, 1) },   // 이벤트 3개 0.13초
        { "sfx_stun", new Def(SfxBus.Combat, SfxFamily.None, 75, 0.400f, 1) },   // 이벤트 1개 0.35초
        { "sfx_thunder_roll", new Def(SfxBus.Boss, SfxFamily.None, 90, 0.600f, 1) },   // 이벤트 1개 0.86초
        { "sfx_train_break", new Def(SfxBus.Combat, SfxFamily.None, 80, 0.700f, 1) },   // 이벤트 1개 1.44초
        { "sfx_train_hit", new Def(SfxBus.Combat, SfxFamily.None, 75, 0.130f, 2) },   // 이벤트 3개 0.28초
        { "sfx_train_whistle", new Def(SfxBus.Combat, SfxFamily.None, 80, 0.700f, 1) },   // 이벤트 1개 0.74초
        { "sfx_ui_click", new Def(SfxBus.Small, SfxFamily.None, 55, 0.090f, 1) },   // 이벤트 3개 0.09초 (팩 우선순위 15)
        { "sfx_ui_close", new Def(SfxBus.Small, SfxFamily.None, 55, 0.090f, 1) },   // 이벤트 1개 0.13초 (팩 우선순위 15)
        { "sfx_ui_open", new Def(SfxBus.Small, SfxFamily.None, 55, 0.090f, 1) },   // 이벤트 1개 0.16초 (팩 우선순위 15)
        { "sfx_victory", new Def(SfxBus.Combat, SfxFamily.None, 80, 0.700f, 1) },   // 이벤트 1개 0.80초
        { "sfx_wave_clear", new Def(SfxBus.Combat, SfxFamily.None, 55, 0.250f, 1) },   // 이벤트 1개 0.54초
        { "sfx_whistle_low", new Def(SfxBus.Combat, SfxFamily.None, 80, 0.700f, 1) },   // 이벤트 1개 1.23초
        { "sfx_wing_gust", new Def(SfxBus.Combat, SfxFamily.Attack, 45, 0.110f, 2) },   // 이벤트 1개 0.55초
        // 이 팩에 없는 키 (지금 파일 그대로)
        { "sfx_whistle_long", new Def(SfxBus.Combat, SfxFamily.None, 80, 1.000f, 1) },   // 클라이맥스 긴 기적 (px/sfx6.py) - 기적과 같은 값
    };

    /// <summary>키의 믹스 값. 없으면 false + Unknown</summary>
    public static bool TryGet(string key, out Def def)
    {
        if (key != null && TABLE.TryGetValue(key, out def)) return true;
        def = Unknown;
        return false;
    }
}
