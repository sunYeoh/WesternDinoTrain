using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// [ClimaxDirector.cs] v1 (신규, v9.24 2026-10-15) - 클라이맥스 (claude/테스터피드백3_2026-10-06.md 의 B 스펙 표, 3차 피드백 5번 "보스 때 뽕차고 웅장한 구간")
///
/// 최종 보스 디 오리지널의 P3 (해치가 열려 기관심장이 드러나는 순간)에 한 번 - 음악이 "버틴다"에서 "이긴다"로 바뀌는 순간을 숨 -> 층 -> 전속으로.
///   1) 숨 (B2): P3 전환 순간의 히트스톱 (BossEnemy 가 ClimaxHitstop 으로 건다) + 화면 흰 번쩍 (클릭은 막지 않는다) + 배경음이 다음 박까지 끊긴다 (76BPM 이면 0.4 ~ 1.2초)
///   2) 층 (B3): 박 머리에 배경음이 돌아오며 bgm_climax 층이 같이 들어온다 (SoundManager.LayerIn - 처음부터 같이 돌던 층이라 박이 어긋나지 않는다). 보스전 배경음 낮춤도 푼다
///   3) 전속 (B4): 같은 박에 긴 기적 + 전조등 빛줄기 -> 배경 1.5배 (덜컹임이 따라 잦아진다) + 지면 줄무늬·불티가 화면을 가로지르고 0.8초에 호박색
///   4) 끝 (B5): 처치 히트스톱과 같은 프레임에 층이 끊긴다 (BossEnemy.Die 가 End(true)) / 식사 엔딩은 ClimaxMealFadeSec 에 잦아든다 (ServeLastSupper 가 End(false)) /
///      게임오버·패배 연출·운행 포기·보스가 사라지면 스스로 끊는다
/// 지역 보스의 발악에는 가벼운 층 (피스톤만) 이 다음 박에 들어오고 처치에 끊긴다 - 화면 연출 없음 (BeginLite)
///
/// 사용법: 없음 - BossEnemy 가 부른다. 스위치 GameBalance.ClimaxOn / ClimaxLiteOn, 값은 GameBalance v9.24 섹션.
/// 소리: Resources/Sounds/ 의 bgm_climax · bgm_climax_lite · sfx_whistle_long (px/sfx6.py). 층 파일이 없으면 소리만 빠지고 화면 연출은 그대로.
/// 멈추는 시간은 P3 히트스톱 0.12초뿐 (나머지는 싸우는 동안 깔린다). 화면 연출은 GameFeelMaster 를 따른다
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class ClimaxDirector : MonoBehaviour
{
    private static ClimaxDirector instance;

    /// <summary>클라이맥스 (또는 가벼운 층) 가 돌고 있는가 - 끝나는 중은 아니다</summary>
    public static bool Active { get { return instance != null && !instance.ending; } }

    private BossEnemy owner;        // 이 클라이맥스를 연 보스 (사라지면 끝)
    private bool full;              // 최종 보스 클라이맥스 (화면 연출 포함) / false = 지역 보스 가벼운 층
    private bool driving;           // 드롭 뒤 - 줄무늬·불티가 흐른다
    private bool ending;            // 끝나는 중 (새로 만들지 않고 걷는다)
    private float destroyAt = -1f;  // 이 실시간에 스스로 지운다

    // ── 숨의 흰 번쩍 (UIFeel.Cover 는 클릭을 막아서 따로 둔다) ──
    private Canvas flashCanvas;
    private Image flash;
    private float flashAge = -1f;

    // ── 전조등 빛줄기 ──
    private SpriteRenderer beam;
    private float beamAge = -1f;
    private static Sprite beamSprite;
    private const float BEAM_IN = 0.06f, BEAM_HOLD = 0.15f, BEAM_OUT = 0.6f;

    // ── 지면 줄무늬 · 불티 (작은 풀 - 다 쓰면 가장 오래된 것을 다시 쓴다) ──
    private const int STREAKS = 28, EMBERS = 48;
    private SpriteRenderer[] streakSr, emberSr;
    private Vector2[] streakVel, emberVel;
    private float[] emberAge, emberLife;
    private int streakNext, emberNext;
    private float streakAcc, emberAcc;

    private const int SORT_STREAK = -8;   // 레일(-10) 위, 기차 몸(-6) 아래 - 지면을 긁고 지나간다
    private const int SORT_EMBER = 3;     // 기차 위
    private const int SORT_BEAM = 1;

    private static bool FeelOn { get { return GameBalance.GameFeelMaster > 0f; } }

    // ─────────────────────────────────────────────
    // 여는 곳 / 닫는 곳 (BossEnemy)
    // ─────────────────────────────────────────────
    /// <summary>B1: 디 오리지널 P3 진입 (PhaseShift 와 같은 프레임). 숨 -> 다음 박에 층 + 기적 + 전조등 -> 전속</summary>
    public static void BeginFinal(BossEnemy boss)
    {
        if (!GameBalance.ClimaxOn || boss == null) return;
        if (Active && instance.full) return;     // 이미 돌고 있다
        ClimaxDirector d = Create(boss, true);
        d.StartCoroutine(d.FinalRoutine());
    }

    /// <summary>B1: 지역 보스 발악 - 가벼운 층(피스톤만)이 다음 박에 들어온다. 화면 연출 없음. 클라이맥스가 돌고 있으면 아무것도 안 한다</summary>
    public static void BeginLite(BossEnemy boss)
    {
        if (!GameBalance.ClimaxLiteOn || boss == null || Active) return;
        ClimaxDirector d = Create(boss, false);
        float wait = SoundManager.SecondsToNextBeat(0.05f);
        if (!SoundManager.LayerIn(SoundManager.LayerLite, GameBalance.ClimaxLiteVol, wait))
            Debug.Log("[Climax] 가벼운 층을 쓸 수 없다 (bgm_climax_lite 없음 또는 길이가 안 맞음) - 발악은 소리 층 없이");
    }

    /// <summary>B5: 끝. cut = 처치 히트스톱 프레임에 바로 끊김 / false = 식사 엔딩 - ClimaxMealFadeSec 에 잦아든다</summary>
    public static void End(bool cut)
    {
        if (instance == null || instance.ending) return;
        instance.Finish(cut ? 0f : GameBalance.ClimaxMealFadeSec);
    }

    private static ClimaxDirector Create(BossEnemy boss, bool fullClimax)
    {
        if (instance != null)
        {
            if (!instance.ending) instance.Finish(0f);   // 가벼운 층이 돌고 있었으면 끊고 새로 (클라이맥스가 덮는다)
            Destroy(instance.gameObject);
            instance = null;
        }
        GameObject go = new GameObject(fullClimax ? "ClimaxDirector" : "ClimaxDirector_Lite");
        ClimaxDirector d = go.AddComponent<ClimaxDirector>();
        d.owner = boss;
        d.full = fullClimax;
        instance = d;
        return d;
    }

    // ─────────────────────────────────────────────
    // 숨 -> 드롭
    // ─────────────────────────────────────────────
    private IEnumerator FinalRoutine()
    {
        // 1) 숨: 배경음이 다음 박까지 끊긴다. 층·배경음은 박보다 조금 일찍 올린다 - 프레임 단위로 소리를 올리면 첫 모루의 머리가 깎인다
        float breath = SoundManager.SecondsToNextBeat(GameBalance.ClimaxBreathMin);
        float early = Mathf.Min(0.05f, breath * 0.5f);
        SoundManager.BgmDuck("boss", false);   // 보스전 배경음 낮춤을 지금 푼다 - 숨이 끝나는 박에 배경음이 제 크기로 바로 돌아온다 ("이긴다")
        SoundManager.BgmCut(breath - early);
        bool layered = SoundManager.LayerIn(SoundManager.LayerClimax, GameBalance.ClimaxLayerVol, breath - early);
        if (FeelOn) StartFlash();
        Debug.Log("[Climax] 숨 " + breath.ToString("F2") + "초 -> 다음 박에 " + (layered ? "클라이맥스 층" : "층 없이 (bgm_climax 없음)"));

        float t = 0f;
        while (t < breath) { t += Time.unscaledDeltaTime; yield return null; }   // 실시간 - 히트스톱과 무관하게 박에 맞춘다
        if (ending) yield break;

        // 2)·3) 드롭: 배경음과 층은 SoundManager 가 같은 순간에 올렸다. 여기서 긴 기적 + 전조등 -> 전속
        SoundManager.Play("sfx_whistle_long");
        if (FeelOn)
        {
            StartHeadlight();
            ParallaxBackground.SetClimaxBoost(GameBalance.ClimaxSpeedMul);
            RouteFX.SetClimaxTone(true);
            EnsurePools();
            driving = true;
        }
        Debug.Log("[Climax] 드롭 - 층 + 긴 기적 + 전조등 -> 전속");
    }

    /// <summary>걷기: 층을 내리고 배경 속도·색을 되돌린다. 줄무늬·불티는 새로 안 만들고 남은 것이 흘러 나간 뒤 스스로 지운다</summary>
    private void Finish(float fadeSec)
    {
        ending = true;
        driving = false;
        StopAllCoroutines();
        SoundManager.LayerOut(full ? SoundManager.LayerClimax : SoundManager.LayerLite, fadeSec);
        if (full)
        {
            ParallaxBackground.SetClimaxBoost(1f);
            RouteFX.SetClimaxTone(false);
        }
        destroyAt = Time.unscaledTime + Mathf.Max(1.2f, fadeSec + 0.5f);   // 그 뒤로 남은 줄무늬·불티가 다 나가면 지운다 (Update)
        Debug.Log("[Climax] 끝 - " + (fadeSec <= 0f ? "끊김" : "잦아듦 " + fadeSec.ToString("F1") + "초"));
    }

    private void Update()
    {
        if (!ending)
        {
            // 스스로 끊는 경우: 전투가 아니다 (승리·패배·마을·로비) / 패배 연출 중 / 보스가 사라졌다 (칸이 떨어져 나가는 중은 아직 - 처치 히트스톱에서 끊긴다)
            GameManager gm = GameManager.Instance;
            bool battle = gm == null || (gm.currentState == GameManager.GameState.Battle && !gm.DefeatPending);
            bool ownerGone = owner == null || (!owner.IsAlive && !owner.Collapsing);
            if (!battle || ownerGone) Finish(0f);
        }

        TickFlash();
        TickBeam();
        if (streakSr != null) TickStreaks();
        if (emberSr != null) TickEmbers();

        // 끝난 뒤: 남은 줄무늬·불티·빛줄기가 다 흘러 나가면 지운다 (처치 히트스톱·슬로모션 동안은 스케일드 시간이 느리다 - 실시간으로 자르면 여운 한가운데서 한꺼번에 사라진다). 상한 +4초
        if (ending && destroyAt > 0f)
        {
            float now = Time.unscaledTime;
            if ((now >= destroyAt && !AnyFxLeft()) || now >= destroyAt + 4f) Destroy(gameObject);
        }
    }

    /// <summary>아직 화면에 남은 줄무늬·불티·빛줄기가 있나</summary>
    private bool AnyFxLeft()
    {
        if (beam != null) return true;
        if (streakSr != null)
            for (int i = 0; i < streakSr.Length; i++) if (streakSr[i] != null && streakSr[i].gameObject.activeSelf) return true;
        if (emberSr != null)
            for (int i = 0; i < emberSr.Length; i++) if (emberSr[i] != null && emberSr[i].gameObject.activeSelf) return true;
        return false;
    }

    private void OnDestroy()
    {
        // 끝을 거치지 않고 사라질 때 (씬 전환 등): 배경 속도·색·층을 되돌린다 (셋 다 씬을 넘어 사는 싱글톤이다)
        if (!ending)
        {
            SoundManager.LayerOut(full ? SoundManager.LayerClimax : SoundManager.LayerLite, 0f);
            if (full) { ParallaxBackground.SetClimaxBoost(1f); RouteFX.SetClimaxTone(false); }
        }
        if (instance == this) instance = null;
    }

    // ─────────────────────────────────────────────
    // B2: 흰 번쩍 (HUD 위, 클릭을 막지 않는다)
    // ─────────────────────────────────────────────
    private void StartFlash()
    {
        if (flash == null)
        {
            flashCanvas = UIFactory.CreateCanvas("Climax_Canvas", 15);   // HUD(10) 위 - 한 장 덮였다 걷힌다
            flashCanvas.transform.SetParent(transform, false);           // 이 오브젝트와 같이 지워진다
            GraphicRaycaster gr = flashCanvas.GetComponent<GraphicRaycaster>();
            if (gr != null) gr.enabled = false;                          // 클릭을 절대 막지 않는다
            GameObject go = new GameObject("Flash");
            go.transform.SetParent(flashCanvas.transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            flash = go.AddComponent<Image>();
            flash.raycastTarget = false;
        }
        flash.color = new Color(1f, 1f, 1f, GameBalance.ClimaxFlashAlpha * Mathf.Clamp01(GameBalance.GameFeelMaster));
        flash.gameObject.SetActive(true);
        flashAge = 0f;
    }

    private void TickFlash()
    {
        if (flash == null || flashAge < 0f) return;
        flashAge += Time.unscaledDeltaTime;
        const float HOLD = 0.04f;   // 히트스톱 동안 한 장 가득
        float k = Mathf.Clamp01((flashAge - HOLD) / Mathf.Max(0.05f, GameBalance.ClimaxFlashSec));
        Color c = flash.color;
        c.a = GameBalance.ClimaxFlashAlpha * Mathf.Clamp01(GameBalance.GameFeelMaster) * (1f - k);
        flash.color = c;
        if (k >= 1f) { flash.gameObject.SetActive(false); flashAge = -1f; }
    }

    // ─────────────────────────────────────────────
    // B4: 전조등 빛줄기 - 기관차 코끝에서 앞(왼쪽)으로 뻗는 도트 원뿔이 번쩍 했다가 걷힌다 + 코끝 고리
    // ─────────────────────────────────────────────
    private void StartHeadlight()
    {
        Vector3 nose = TrainDeck.RidePos(new Vector3(GameBalance.RouteHeadFrontX + 0.3f, 0.05f, 0f));
        GameObject go = new GameObject("ClimaxHeadlight");
        go.transform.SetParent(transform, false);
        go.transform.position = nose;
        go.transform.rotation = Quaternion.Euler(0f, 0f, TrainDeck.RideAngle(GameBalance.RouteHeadFrontX));
        beam = go.AddComponent<SpriteRenderer>();
        beam.sprite = BeamSprite();
        beam.sortingOrder = SORT_BEAM;
        beam.color = new Color(1f, 0.95f, 0.78f, 0f);
        beamAge = 0f;
        WorldFeel.Ring(nose, new Color(1f, 0.92f, 0.62f), 1.3f, 0.35f);
    }

    private void TickBeam()
    {
        if (beam == null || beamAge < 0f) return;
        beamAge += Time.unscaledDeltaTime;
        float a;
        if (beamAge < BEAM_IN) a = beamAge / BEAM_IN;
        else if (beamAge < BEAM_IN + BEAM_HOLD) a = 1f;
        else a = 1f - Mathf.Clamp01((beamAge - BEAM_IN - BEAM_HOLD) / BEAM_OUT);
        Color c = beam.color; c.a = 0.85f * a; beam.color = c;
        if (beamAge >= BEAM_IN + BEAM_HOLD + BEAM_OUT) { Destroy(beam.gameObject); beam = null; beamAge = -1f; }
    }

    /// <summary>
    /// 빛줄기 도트 (64 x 28 px, PPU 10 = 6.4 x 2.8u). 피벗 = 오른쪽 가운데(코끝) - 왼쪽으로 갈수록 넓어지는 원뿔.
    /// 매끈한 그라데이션 대신 세 단 알파 (가운데 진함 / 가 / 끝 흐림) - 도트 그림체와 같은 결
    /// </summary>
    private static Sprite BeamSprite()
    {
        if (beamSprite != null) return beamSprite;
        const int W = 64, H = 28;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[W * H];
        float midY = (H - 1) * 0.5f;
        for (int x = 0; x < W; x++)
        {
            float along = (W - 1 - x) / (float)(W - 1);                  // 0 = 코끝(오른쪽), 1 = 먼 끝
            float half = Mathf.Lerp(1.5f, H * 0.5f, along);               // 원뿔 반폭
            float farFade = along < 0.55f ? 1f : (along < 0.8f ? 0.6f : 0.3f);
            for (int y = 0; y < H; y++)
            {
                float d = Mathf.Abs(y - midY);
                byte alpha = 0;
                if (d <= half * 0.35f) alpha = (byte)(230 * farFade);
                else if (d <= half * 0.7f) alpha = (byte)(140 * farFade);
                else if (d <= half) alpha = (byte)(70 * farFade);
                px[y * W + x] = new Color32(255, 255, 255, alpha);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        beamSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(1f, 0.5f), 10f);
        return beamSprite;
    }

    // ─────────────────────────────────────────────
    // B4: 지면 줄무늬 (지면 위를 뒤로 긁고 지나가는 가는 선) · 불티 (바퀴 줄에서 튀어 뒤로 흩어지는 작은 점)
    //   스케일드 시간 - 일시정지·QTE·히트스톱 동안은 같이 멈춘다 (세계의 움직임이다)
    // ─────────────────────────────────────────────
    private void EnsurePools()
    {
        if (streakSr != null) return;
        Sprite white = TrainDeck.GetWhiteSprite();
        streakSr = new SpriteRenderer[STREAKS]; streakVel = new Vector2[STREAKS];
        for (int i = 0; i < STREAKS; i++)
        {
            GameObject go = new GameObject("Streak");
            go.transform.SetParent(transform, false);
            streakSr[i] = go.AddComponent<SpriteRenderer>();
            streakSr[i].sprite = white;
            streakSr[i].sortingOrder = SORT_STREAK;
            go.SetActive(false);
        }
        emberSr = new SpriteRenderer[EMBERS]; emberVel = new Vector2[EMBERS]; emberAge = new float[EMBERS]; emberLife = new float[EMBERS];
        for (int i = 0; i < EMBERS; i++)
        {
            GameObject go = new GameObject("Ember");
            go.transform.SetParent(transform, false);
            emberSr[i] = go.AddComponent<SpriteRenderer>();
            emberSr[i].sprite = white;
            emberSr[i].sortingOrder = SORT_EMBER;
            go.SetActive(false);
        }
    }

    private void TickStreaks()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        float dt = Time.deltaTime;
        float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
        Vector3 cp = cam.transform.position;
        if (driving && dt > 0f)
        {
            streakAcc += dt * GameBalance.ClimaxStreakRate;
            while (streakAcc >= 1f) { streakAcc -= 1f; SpawnStreak(cp, halfW, halfH); }
        }
        for (int i = 0; i < STREAKS; i++)
        {
            SpriteRenderer sr = streakSr[i];
            if (!sr.gameObject.activeSelf) continue;
            Transform tf = sr.transform;
            tf.position += (Vector3)(streakVel[i] * dt);
            if (tf.position.x - tf.localScale.x * 0.5f > cp.x + halfW + 1f) sr.gameObject.SetActive(false);
        }
    }

    private void SpawnStreak(Vector3 cp, float halfW, float halfH)
    {
        int i = streakNext; streakNext = (streakNext + 1) % STREAKS;
        SpriteRenderer sr = streakSr[i];
        float len = Random.Range(1.2f, 3.4f);
        float y = cp.y + Random.Range(-halfH * 0.92f, halfH * 0.92f);
        sr.transform.position = new Vector3(cp.x - halfW - len, y, 0f);
        sr.transform.localScale = new Vector3(len, 0.06f, 1f);
        sr.color = new Color(1f, 0.93f, 0.78f, Random.Range(0.18f, 0.32f));
        streakVel[i] = new Vector2(Mathf.Max(6f, ParallaxBackground.CurrentSpeed * 3f) * Random.Range(0.85f, 1.2f), 0f);
        sr.gameObject.SetActive(true);
    }

    private void TickEmbers()
    {
        float dt = Time.deltaTime;
        if (driving && dt > 0f)
        {
            emberAcc += dt * GameBalance.ClimaxEmberRate;
            while (emberAcc >= 1f) { emberAcc -= 1f; SpawnEmber(); }
        }
        for (int i = 0; i < EMBERS; i++)
        {
            SpriteRenderer sr = emberSr[i];
            if (!sr.gameObject.activeSelf) continue;
            emberAge[i] += dt;
            float k = emberAge[i] / Mathf.Max(0.05f, emberLife[i]);
            if (k >= 1f) { sr.gameObject.SetActive(false); continue; }
            sr.transform.position += (Vector3)(emberVel[i] * dt);
            emberVel[i].y -= 1.5f * dt;   // 살짝 처진다
            // 노랑 -> 주황 -> 검붉게 식으며 사라진다
            Color c = k < 0.4f ? Color.Lerp(new Color(1f, 0.92f, 0.5f), new Color(1f, 0.55f, 0.15f), k / 0.4f)
                               : Color.Lerp(new Color(1f, 0.55f, 0.15f), new Color(0.6f, 0.15f, 0.05f), (k - 0.4f) / 0.6f);
            c.a = 1f - k * k;
            sr.color = c;
        }
    }

    private void SpawnEmber()
    {
        int i = emberNext; emberNext = (emberNext + 1) % EMBERS;
        SpriteRenderer sr = emberSr[i];
        // 바퀴 줄 (기차 몸 위아래 테두리 바로 바깥 ±1.85 - DustFX 바퀴 먼지와 같은 줄)에서 튄다. 기차 앞쪽일수록 자주
        float x = Mathf.Lerp(GameBalance.CarEdgesX[0], GameBalance.CarEdgesX[GameBalance.CarEdgesX.Length - 1], Random.value * Random.value);
        float y = (Random.value < 0.5f ? -1.85f : 1.85f) + Random.Range(-0.15f, 0.15f);
        sr.transform.position = TrainDeck.RidePos(new Vector3(x, y, 0f), false);
        float sz = Random.Range(0.07f, 0.13f);
        sr.transform.localScale = new Vector3(sz, sz, 1f);
        emberVel[i] = new Vector2(Random.Range(7f, 13f), Random.Range(-0.8f, 1.6f));
        emberAge[i] = 0f;
        emberLife[i] = Random.Range(0.45f, 0.9f);
        sr.gameObject.SetActive(true);
    }
}
