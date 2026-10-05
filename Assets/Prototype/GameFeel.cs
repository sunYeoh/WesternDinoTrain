using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// [GameFeel.cs] v1.1 (v9.17 2026-10-06 A11: SlowMo(배율, 유지, 복귀, 줌) - 보스 처치 전용 슬로모션 + 줌 당김(ZoomMul, CameraZoom 이 곱한다). 히트스톱이 끝난 뒤 시작하고, 다른 곳이 시간을 잡으면 시간에서 손을 뗀다) / v1 (신규 파일) - P1: 게임필 계층 (기술감사 처방)
/// 화면 셰이크 / 히트스톱 / 처치 팝을 static 한 줄 호출로 제공하는 연출 엔진.
///
/// 설계 원칙 (사용자 지시: "과하면 피로와 멀미 - 적당한 타협점"):
///  - 전 강도는 GameBalance '게임필' 섹션 계수로 제어. 0이면 해당 연출 완전 꺼짐
///  - GameFeelMaster 하나로 전체 일괄 조절 (플레이테스트에서 이 값만 만지면 됨)
///  - 셰이크는 Perlin 곡선(부드러운 연속 흔들림) + 회전 없음 - 멀미 최소화
///  - 셰이크는 카메라만 흔든다. 조리 미니게임 등 UI는 흔들리지 않아 판정 방해 없음
///  - 히트스톱은 보스 순간에만 (그로기 진입/보스 처치) - 남발 금지 + 0.45초 재사용 제한
///
/// 사용법: 없음! 어느 스크립트든 GameFeel.Shake(0.3f) 처럼 부르면 자동 생성된다.
///  - GameFeel.Shake(강도)                    : 화면 흔들림 (쿨타임 없음 - 보스 등 드문 순간용)
///  - GameFeel.Shake(강도, 채널, 쿨타임)      : 같은 채널은 쿨타임(초)에 한 번만 - 잦은 피격용
///  - GameFeel.Hitstop(초)                    : 짧은 시간 정지 (실시간 기준)
///  - GameFeel.DeathPop(위치, 색)             : 처치 순간 조각 팝
///  - GameFeel.SlowMo(배율, 유지, 복귀, 줌)   : v1.1 슬로모션 + 줌 당김 (실시간 초). 보스 처치처럼 한 판에 몇 번 없는 순간에만
/// 카메라 반영은 CameraZoom v3가 GameFeel.ShakeOffset을 읽어 처리한다.
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class GameFeel : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // 셰이크 상태 (트라우마 방식: 강도의 제곱으로 진폭 계산 - 잔진동이 빨리 잦아듦)
    // ─────────────────────────────────────────────

    /// <summary>현재 프레임의 카메라 흔들림 오프셋 (CameraZoom이 읽음, 월드 단위)</summary>
    public static Vector2 ShakeOffset { get; private set; }

    private static float trauma = 0f;              // 0~1 누적 충격량
    private const float TRAUMA_DECAY = 1.4f;       // 초당 감쇠 (클수록 빨리 멈춤)
    private const float MAX_OFFSET = 0.5f;         // 트라우마 1.0일 때 최대 오프셋 (월드 단위, 줌7 기준)
    private const float NOISE_FREQ = 19f;          // 흔들림 속도 (너무 크면 멀미)

    // ─────────────────────────────────────────────
    // 히트스톱 상태
    // ─────────────────────────────────────────────
    private static float lastHitstopReal = -10f;   // 마지막 히트스톱 시각 (실시간)
    private static bool hitstopActive = false;
    private const float HITSTOP_MIN_GAP = 0.45f;   // 연속 강타 시 스트로브 방지 간격
    private const float HITSTOP_SCALE = 0.05f;     // 정지 중 시간 배율 (완전 0 대신 미세하게 흐름)

    // ─────────────────────────────────────────────
    // 처치 팝 상태
    // ─────────────────────────────────────────────
    private static int activePops = 0;             // 동시 재생 수 (물량전 프레임 보호)
    private const int MAX_POPS = 24;
    private static Sprite squareSprite;            // 4x4 흰 사각형 (1회 생성 캐시)

    private static GameFeel instance;

    // ─────────────────────────────────────────────
    // 공개 API
    // ─────────────────────────────────────────────

    // 채널별 마지막 셰이크 시각 (실시간 기준)
    private static Dictionary<string, float> channelLastShake = new Dictionary<string, float>();

    /// <summary>화면 셰이크 (쿨타임 없음 - 런지/그로기/보스 처치 같은 드문 대형 순간에만 쓸 것)</summary>
    public static void Shake(float strength)
    {
        if (strength <= 0f || GameBalance.GameFeelMaster <= 0f) return;
        Ensure();
        trauma = Mathf.Clamp01(trauma + strength);
    }

    /// <summary>
    /// 쿨타임 있는 셰이크. 같은 channel은 cooldown초에 한 번만 흔들린다.
    /// 잦은 이벤트(기차 피격, 폭발)가 화면을 쉬지 않고 흔들어 피로해지는 것을 막는다.
    /// </summary>
    public static void Shake(float strength, string channel, float cooldown)
    {
        if (strength <= 0f || GameBalance.GameFeelMaster <= 0f) return;

        float last;
        if (channelLastShake.TryGetValue(channel, out last)
            && Time.realtimeSinceStartup - last < cooldown)
            return;   // 아직 쿨타임 - 조용히 무시

        channelLastShake[channel] = Time.realtimeSinceStartup;
        Shake(strength);
    }

    /// <summary>히트스톱 (duration = 실시간 초). 일시정지/QTE 등 다른 시간 조작과는 절대 겹치지 않는다</summary>
    public static void Hitstop(float duration)
    {
        if (duration <= 0f || GameBalance.GameFeelMaster <= 0f) return;
        if (Time.timeScale != 1f) return;                                    // 일시정지/QTE/스토리 중이면 양보
        if (Time.realtimeSinceStartup - lastHitstopReal < HITSTOP_MIN_GAP) return;
        Ensure();
        instance.StartCoroutine(instance.HitstopRoutine(
            duration * Mathf.Clamp01(GameBalance.GameFeelMaster)));
    }

    // ─────────────────────────────────────────────
    // v1.1: 슬로모션 + 줌 당김
    // ─────────────────────────────────────────────
    /// <summary>카메라 줌 배율 (1 = 그대로, 0.92 = 8% 당김). CameraZoom 이 마지막에 곱한다</summary>
    public static float ZoomMul { get; private set; } = 1f;

    private static bool slowActive = false;
    private static float slowScale = 1f;   // 지금 우리가 걸어 둔 시간 배율 (다른 곳이 바꿨는지 비교용)

    /// <summary>
    /// 시간을 scale 배로 holdSec 동안 늦췄다가 recoverSec 에 걸쳐 1 로 (전부 실시간 초). zoom > 0 이면 그만큼 당겼다가 같이 돌아온다.
    /// 히트스톱이 돌고 있으면 끝난 뒤 시작. 일시정지·카드 창처럼 다른 곳이 시간을 잡으면 시간은 그쪽에 맡기고 줌만 마저 돌려놓는다
    /// </summary>
    public static void SlowMo(float scale, float holdSec, float recoverSec, float zoom)
    {
        if (GameBalance.GameFeelMaster <= 0f || slowActive) return;
        if (holdSec <= 0f && recoverSec <= 0f) return;
        Ensure();
        instance.StartCoroutine(instance.SlowMoRoutine(Mathf.Clamp(scale, 0.05f, 1f), holdSec, recoverSec, zoom));
    }

    private static bool SameScale(float a, float b) { return Mathf.Abs(a - b) < 0.005f; }

    private IEnumerator SlowMoRoutine(float scale, float holdSec, float recoverSec, float zoom)
    {
        slowActive = true;
        while (hitstopActive) yield return null;   // 히트스톱 먼저

        bool ownTime = Time.timeScale == 1f;       // 누가 이미 시간을 잡고 있으면 줌만 한다
        float zoomTo = 1f - Mathf.Clamp(zoom, 0f, 0.3f);
        if (ownTime) { slowScale = scale; Time.timeScale = scale; }

        float t = 0f;
        while (t < holdSec)
        {
            if (ownTime && !SameScale(Time.timeScale, slowScale)) ownTime = false;
            t += Time.unscaledDeltaTime;
            ZoomMul = Mathf.Lerp(1f, zoomTo, Mathf.Clamp01(t / 0.08f));   // 빠르게 당긴다
            yield return null;
        }
        t = 0f;
        while (t < recoverSec)
        {
            if (ownTime && !SameScale(Time.timeScale, slowScale)) ownTime = false;
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / recoverSec);
            if (ownTime) { slowScale = Mathf.Lerp(scale, 1f, k); Time.timeScale = slowScale; }
            ZoomMul = Mathf.Lerp(zoomTo, 1f, k * k * (3f - 2f * k));
            yield return null;
        }
        if (ownTime && SameScale(Time.timeScale, slowScale)) Time.timeScale = 1f;
        ZoomMul = 1f;
        slowActive = false;
    }

    /// <summary>처치 팝 (기본 크기)</summary>
    public static void DeathPop(Vector3 pos, Color col)
    {
        DeathPop(pos, col, 1f);
    }

    /// <summary>처치 팝. sizeMul 2 이상이면 조각 수도 늘어난다 (보스용 3f 권장)</summary>
    public static void DeathPop(Vector3 pos, Color col, float sizeMul)
    {
        if (GameBalance.DeathPopScale <= 0f || GameBalance.GameFeelMaster <= 0f) return;
        if (activePops >= MAX_POPS) return;   // 물량전 프레임 보호 (초과분은 조용히 생략)
        Ensure();
        instance.StartCoroutine(instance.PopRoutine(pos, col, sizeMul * GameBalance.DeathPopScale));
    }

    // ─────────────────────────────────────────────
    // 자동 생성
    // ─────────────────────────────────────────────
    private static void Ensure()
    {
        if (instance != null) return;
        GameObject go = new GameObject("GameFeel");
        instance = go.AddComponent<GameFeel>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    // 씬 리로드([다시 굽는다] 등) 안전장치: 진행 중이던 셰이크/히트스톱이 고착되지 않게 정리
    private void OnDestroy()
    {
        if (instance != this) return;
        ShakeOffset = Vector2.zero;
        trauma = 0f;
        if (hitstopActive)
        {
            hitstopActive = false;
            if (Mathf.Abs(Time.timeScale - HITSTOP_SCALE) < 0.01f)
                Time.timeScale = 1f;
        }
        activePops = 0;
        channelLastShake.Clear();
        // v1.1: 슬로모션 도중에 씬이 바뀌어도 시간·줌이 남지 않게
        if (slowActive)
        {
            slowActive = false;
            if (SameScale(Time.timeScale, slowScale)) Time.timeScale = 1f;
        }
        ZoomMul = 1f;
        instance = null;
    }

    // ─────────────────────────────────────────────
    // 셰이크 갱신 (실시간 기준 - 히트스톱 중에도 잔진동이 살아있어 타격감 유지)
    // ─────────────────────────────────────────────
    private void Update()
    {
        // 진짜 일시정지(메뉴/QTE/스토리) 중에는 화면을 고정한다 (히트스톱은 예외)
        if (Time.timeScale == 0f && !hitstopActive)
        {
            ShakeOffset = Vector2.zero;
            trauma = Mathf.Max(0f, trauma - Time.unscaledDeltaTime * TRAUMA_DECAY);
            return;
        }

        trauma = Mathf.Max(0f, trauma - Time.unscaledDeltaTime * TRAUMA_DECAY);

        if (trauma <= 0f)
        {
            ShakeOffset = Vector2.zero;
            return;
        }

        // 제곱 감쇠: 큰 충격은 크게, 잔여 트라우마는 눈에 띄지 않게
        float amp = trauma * trauma * MAX_OFFSET * Mathf.Clamp01(GameBalance.GameFeelMaster);

        // Perlin 노이즈 = 연속적인 곡선 흔들림 (프레임마다 랜덤 점프하는 방식보다 멀미가 덜함)
        float t = Time.unscaledTime * NOISE_FREQ;
        ShakeOffset = new Vector2(
            (Mathf.PerlinNoise(t, 11.3f) - 0.5f) * 2f,
            (Mathf.PerlinNoise(47.7f, t) - 0.5f) * 2f) * amp;
    }

    // ─────────────────────────────────────────────
    // 히트스톱 코루틴
    // ─────────────────────────────────────────────
    private IEnumerator HitstopRoutine(float duration)
    {
        hitstopActive = true;
        lastHitstopReal = Time.realtimeSinceStartup;
        Time.timeScale = HITSTOP_SCALE;

        yield return new WaitForSecondsRealtime(duration);

        // 히트스톱 도중 다른 시스템(일시정지/QTE)이 시간을 잡았다면 존중하고 물러난다
        if (Mathf.Abs(Time.timeScale - HITSTOP_SCALE) < 0.01f)
            Time.timeScale = 1f;

        hitstopActive = false;
    }

    // ─────────────────────────────────────────────
    // 처치 팝 코루틴: 중앙 섬광 1개 + 사방으로 튀는 조각
    // ─────────────────────────────────────────────
    private IEnumerator PopRoutine(Vector3 pos, Color col, float sizeMul)
    {
        activePops++;

        int pieceCount = sizeMul >= 2f ? 10 : 5;
        Transform[] pieces = new Transform[pieceCount];
        Vector2[] vels = new Vector2[pieceCount];
        SpriteRenderer[] srs = new SpriteRenderer[pieceCount];

        // 중앙 섬광 (한 프레임짜리 흰 번쩍 - 팝 손맛의 핵심)
        GameObject flash = MakePiece(pos, Color.white, 0.4f * sizeMul);
        SpriteRenderer flashSr = flash.GetComponent<SpriteRenderer>();

        for (int i = 0; i < pieceCount; i++)
        {
            GameObject p = MakePiece(pos, col, 0.15f * sizeMul);
            pieces[i] = p.transform;
            srs[i] = p.GetComponent<SpriteRenderer>();
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float spd = Random.Range(1.8f, 3.4f);
            vels[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
        }

        float life = 0.32f;
        float t = 0f;
        while (t < life)
        {
            t += Time.deltaTime;   // 스케일드 시간: 히트스톱 중에는 조각도 멈춰 정지감 강화
            float k = t / life;

            // 섬광은 초반 0.08초만
            if (flash != null)
            {
                if (t > 0.08f) { Destroy(flash); flash = null; }
                else flashSr.color = new Color(1f, 1f, 1f, 1f - t / 0.08f);
            }

            for (int i = 0; i < pieceCount; i++)
            {
                if (pieces[i] == null) continue;
                vels[i] *= 1f - 4.5f * Time.deltaTime;                       // 감속
                pieces[i].position += (Vector3)(vels[i] * Time.deltaTime);
                pieces[i].localScale = Vector3.one * 0.15f * sizeMul * (1f - k); // 축소 소멸
                srs[i].color = new Color(col.r, col.g, col.b, 1f - k * k);   // 후반 페이드
            }
            yield return null;
        }

        if (flash != null) Destroy(flash);
        for (int i = 0; i < pieceCount; i++)
            if (pieces[i] != null) Destroy(pieces[i].gameObject);

        activePops--;
    }

    /// <summary>팝 조각 1개 생성</summary>
    private GameObject MakePiece(Vector3 pos, Color col, float scale)
    {
        GameObject go = new GameObject("PopPiece");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquare();
        sr.color = col;
        sr.sortingOrder = 58;   // 재료 조각(60)보다 아래, 적 위
        return go;
    }

    /// <summary>4x4 흰 사각형 스프라이트 (1회 생성 캐시)</summary>
    private static Sprite GetSquare()
    {
        if (squareSprite != null) return squareSprite;

        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        squareSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return squareSprite;
    }
}
