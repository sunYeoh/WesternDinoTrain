using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [UIFeel.cs] v1.2 (v9.17 2026-10-06 화면 손맛 2차: CardFeel(카드 하나씩 등장 / 고른 카드만 남기고 창 퇴장) + ScreenFx(세상 어둠 / 붉은 가장자리 / 결과 막 / 덮개 페이드 - 보스 등장·패배·승리·장면 전환이 쓴다)) / v1.1 (v9.16 2026-09-29 손맛 2차 - 소리: 창이 뜰 때(ModalFeel.Play) sfx_ui_open 한 번 - 호출부마다 넣지 않는다) / v1 (신규, v9.11 2026-09-22) - UI 반응 계층 (스펙 표 C1 C2 + B4 의 UI 쪽)
///
///   ButtonFeel  - 버튼 컴포넌트: 호버 1.03배 / 프레스 0.96배 / 비활성 회색. UIFactory.CreateButton, KitchenEventManager.MakeButton,
///                 GameHUD 요리 카드가 붙인다 (ButtonFeel.Attach(button)). 실시간 기준이라 시간이 멈춘 창에서도 반응한다
///   ModalFeel   - 모달 등장: ModalFeel.Play(루트) 한 줄. 루트의 전체 펼침 자식(어둠)은 알파 0 -> 원래로 0.15초, 나머지 자식(판)은 0.92 -> 1.02 -> 1.0 (0.18초)
///   UIFeel      - Bounce(RectTransform, 양, 초) / FlyTo(캔버스, 화면 좌표, 목표 Rect, 그림, 색, 초, 도착 콜백)
///   CardFeel    - v1.2: StaggerIn(카드들, 간격) / PickExit(창, 카드 영역, 카드들, 고른 카드, 끝나면)
///   ScreenFx    - v1.2: WorldDim / Vignette / Curtain / Cover. 캔버스 3장(세상 바로 위 -50 / 명성 상점 바로 아래 555 / 맨 위 30000)을 처음 쓸 때 만든다. 씬이 바뀌면 덮개만 남기고 걷는다
/// 전부 GameBalance.ButtonFeelOn / ModalFeelOn / GameFeelMaster 로 끌 수 있다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class ButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private const float HOVER = 1.03f, PRESS = 0.96f, SPEED = 14f;
    private RectTransform rt;
    private Button button;
    private Vector3 baseScale = Vector3.one;
    private bool hover = false, press = false;
    private float cur = 1f;

    /// <summary>버튼에 반응을 붙인다 (이미 있으면 그대로)</summary>
    public static ButtonFeel Attach(Button b)
    {
        if (b == null) return null;
        ButtonFeel f = b.GetComponent<ButtonFeel>();
        if (f == null) f = b.gameObject.AddComponent<ButtonFeel>();
        return f;
    }

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        button = GetComponent<Button>();
        baseScale = rt != null ? rt.localScale : Vector3.one;
        if (button != null)
        {
            // 비활성 = 확실히 회색 (기본 0.78 은 눌러도 되는 것처럼 보인다)
            ColorBlock cb = button.colors;
            cb.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.8f);
            button.colors = cb;
        }
    }

    public void OnPointerEnter(PointerEventData e) { hover = true; }
    public void OnPointerExit(PointerEventData e) { hover = false; press = false; }
    public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) press = true; }
    public void OnPointerUp(PointerEventData e) { press = false; }

    private void OnDisable() { hover = false; press = false; cur = 1f; if (rt != null) rt.localScale = baseScale; }

    private void Update()
    {
        if (rt == null) return;
        bool on = GameBalance.ButtonFeelOn && GameBalance.GameFeelMaster > 0f && (button == null || button.interactable);
        float target = !on ? 1f : (press ? PRESS : (hover ? HOVER : 1f));
        float next = Mathf.MoveTowards(cur, target, SPEED * Time.unscaledDeltaTime * Mathf.Max(0.02f, Mathf.Abs(target - cur) + 0.02f));
        // 다른 연출(카드 튀기)이 잠깐 크기를 바꿨다 돌려놓아도 내 상태로 되돌린다
        if (Mathf.Abs(next - cur) < 0.0001f && Mathf.Abs(rt.localScale.x - baseScale.x * cur) < 0.001f) return;
        cur = next;
        rt.localScale = baseScale * cur;
    }
}

/// <summary>모달 등장 연출</summary>
public static class ModalFeel
{
    private const float DIM_SEC = 0.15f, POP_SEC = 0.18f;

    /// <summary>루트(캔버스 또는 판)를 등장시킨다. 시간이 멈춰 있어도(실시간) 움직인다</summary>
    public static void Play(Transform root)
    {
        if (root == null) return;
        SoundManager.Play("sfx_ui_open");   // v1.1: 창 열림 (같은 프레임 중복은 SoundManager 가 걸러낸다)
        if (!GameBalance.ModalFeelOn || GameBalance.GameFeelMaster <= 0f) return;
        RectTransform rrt = root as RectTransform;
        UIFeelRunner r = UIFeel.Runner();

        bool isCanvas = root.GetComponent<Canvas>() != null;   // 캔버스 루트 = 컨테이너 (자식들만 본다)

        // 루트 자체가 판이면 (전체 펼침이 아니면) 그것만 튀긴다
        if (!isCanvas && rrt != null && !IsFullStretch(rrt)) { r.StartCoroutine(r.PopRoutine(rrt, POP_SEC)); return; }

        // 루트 자신이 어둠(전체 펼침 + Image)이면 그것도 페이드
        if (!isCanvas && rrt != null) { Image self = rrt.GetComponent<Image>(); if (self != null) r.StartCoroutine(r.DimRoutine(self, DIM_SEC)); }

        for (int i = 0; i < root.childCount; i++)
        {
            RectTransform c = root.GetChild(i) as RectTransform;
            if (c == null || !c.gameObject.activeSelf) continue;
            if (IsFullStretch(c))
            {
                Image img = c.GetComponent<Image>();
                if (img != null) r.StartCoroutine(r.DimRoutine(img, DIM_SEC));
                // 전체 펼침 컨테이너 안의 판들 (한 단계만)
                for (int j = 0; j < c.childCount; j++)
                {
                    RectTransform g = c.GetChild(j) as RectTransform;
                    if (g != null && g.gameObject.activeSelf && !IsFullStretch(g) && g.GetComponent<Graphic>() != null) r.StartCoroutine(r.PopRoutine(g, POP_SEC));
                }
            }
            else r.StartCoroutine(r.PopRoutine(c, POP_SEC));
        }
    }

    private static bool IsFullStretch(RectTransform t)
    {
        return t.anchorMin.x <= 0.001f && t.anchorMin.y <= 0.001f && t.anchorMax.x >= 0.999f && t.anchorMax.y >= 0.999f
            && Mathf.Abs(t.offsetMin.x) < 2f && Mathf.Abs(t.offsetMin.y) < 2f && Mathf.Abs(t.offsetMax.x) < 2f && Mathf.Abs(t.offsetMax.y) < 2f;
    }
}

/// <summary>UI 팝 공용</summary>
public static class UIFeel
{
    private static UIFeelRunner runner;

    public static UIFeelRunner Runner()
    {
        if (runner != null) return runner;
        GameObject go = new GameObject("UIFeel");
        Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<UIFeelRunner>();
        return runner;
    }

    /// <summary>1+amount 에서 1 로 (실시간, easeOut). 카드가 생겼다 / 개수가 늘었다</summary>
    public static void Bounce(RectTransform rt, float amount, float sec)
    {
        if (rt == null || GameBalance.GameFeelMaster <= 0f) return;
        Runner().StartCoroutine(Runner().BounceRoutine(rt, amount, sec));
    }

    /// <summary>
    /// 캔버스 위에 그림 하나를 만들어 화면 좌표 from 에서 target 의 가운데로 sec 동안 날린다 (살짝 포물선, 1.1 -> 0.7 배).
    /// 도착하면 onArrive. ScreenSpaceOverlay 캔버스 기준
    /// </summary>
    public static void FlyTo(Canvas canvas, Vector2 fromScreen, RectTransform target, Sprite sprite, Color col, float size, float sec, System.Action onArrive)
    {
        if (canvas == null || target == null || GameBalance.GameFeelMaster <= 0f) { if (onArrive != null) onArrive(); return; }
        RectTransform croot = canvas.transform as RectTransform;
        GameObject go = new GameObject("Fly");
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(croot, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite; img.color = col; img.raycastTarget = false; img.preserveAspect = true;
        Vector2 a, b;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(croot, fromScreen, null, out a);
        Vector3 targetCenter = target.TransformPoint(target.rect.center);   // 피벗이 아니라 가운데로
        RectTransformUtility.ScreenPointToLocalPointInRectangle(croot, RectTransformUtility.WorldToScreenPoint(null, targetCenter), null, out b);
        rt.anchoredPosition = a;
        Runner().StartCoroutine(Runner().FlyRoutine(rt, a, b, sec, onArrive));
    }
}

public class UIFeelRunner : MonoBehaviour
{
    // 같은 Rect 에 팝이 겹쳐 시작해도 원래 크기를 잃지 않게 (진행 중인 애니메이션의 중간 크기를 원본으로 착각하지 않는다)
    private readonly System.Collections.Generic.Dictionary<RectTransform, Vector3> baseScales = new System.Collections.Generic.Dictionary<RectTransform, Vector3>();
    private readonly System.Collections.Generic.Dictionary<RectTransform, int> running = new System.Collections.Generic.Dictionary<RectTransform, int>();

    private Vector3 Begin(RectTransform rt)
    {
        Vector3 b;
        if (!baseScales.TryGetValue(rt, out b)) { b = rt.localScale; baseScales[rt] = b; running[rt] = 0; }
        running[rt] = running[rt] + 1;
        return b;
    }

    private void End(RectTransform rt, Vector3 b)
    {
        int n; if (!running.TryGetValue(rt, out n)) return;
        n--; running[rt] = n;
        if (n <= 0) { running.Remove(rt); baseScales.Remove(rt); if (rt != null) rt.localScale = b; }
    }

    public System.Collections.IEnumerator PopRoutine(RectTransform rt, float sec)
    {
        Vector3 baseScale = Begin(rt);
        float t = 0f;
        while (t < sec && rt != null)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / sec);
            // 0.92 -> 1.02 (0~0.7) -> 1.0 (0.7~1) : 오버슈트
            float s = k < 0.7f ? Mathf.Lerp(0.92f, 1.02f, 1f - (1f - k / 0.7f) * (1f - k / 0.7f)) : Mathf.Lerp(1.02f, 1f, (k - 0.7f) / 0.3f);
            rt.localScale = baseScale * s;
            yield return null;
        }
        End(rt, baseScale);
    }

    public System.Collections.IEnumerator DimRoutine(Image img, float sec)
    {
        Color target = img.color;
        float t = 0f;
        while (t < sec && img != null)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / sec);
            img.color = new Color(target.r, target.g, target.b, target.a * k);
            yield return null;
        }
        if (img != null) img.color = target;
    }

    public System.Collections.IEnumerator BounceRoutine(RectTransform rt, float amount, float sec)
    {
        Vector3 baseScale = Begin(rt);
        float t = 0f;
        while (t < sec && rt != null)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / sec);
            float e = 1f - (1f - k) * (1f - k);
            rt.localScale = baseScale * (1f + amount * (1f - e));
            yield return null;
        }
        End(rt, baseScale);
    }

    public System.Collections.IEnumerator FlyRoutine(RectTransform rt, Vector2 a, Vector2 b, float sec, System.Action onArrive)
    {
        float t = 0f;
        while (t < sec && rt != null)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / sec);
            float e = k < 0.5f ? 2f * k * k : 1f - 2f * (1f - k) * (1f - k);   // easeInOut
            Vector2 p = Vector2.Lerp(a, b, e);
            p.y += Mathf.Sin(k * Mathf.PI) * 60f;                            // 살짝 포물선
            rt.anchoredPosition = p;
            rt.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.7f, e);
            yield return null;
        }
        if (rt != null) Destroy(rt.gameObject);
        if (onArrive != null) onArrive();
    }

    // ── v1.2: 카드 ──
    /// <summary>delay 뒤 알파 0 -> 1, 0.9 -> 1.0배 (sec). 그사이 카드가 없어지면(리롤) 그만둔다</summary>
    public System.Collections.IEnumerator CardInRoutine(RectTransform rt, CanvasGroup g, float delay, float sec)
    {
        float w = 0f;
        while (w < delay) { w += Time.unscaledDeltaTime; yield return null; }
        if (rt == null) yield break;
        Vector3 baseScale = Begin(rt);
        float t = 0f;
        while (t < sec && rt != null)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / sec);
            float e = 1f - (1f - k) * (1f - k);
            rt.localScale = baseScale * Mathf.Lerp(0.9f, 1f, e);
            if (g != null) g.alpha = e;
            yield return null;
        }
        if (g != null) g.alpha = 1f;
        End(rt, baseScale);
    }

    /// <summary>고른 카드만 hold 동안 밝게(흰 덮개 + 1.05배) 남기고 나머지는 흐리게 -> 창이 0.95배 + 알파 0 (exitSec) -> 원래 값으로 돌려놓고 onDone</summary>
    public System.Collections.IEnumerator CardExitRoutine(RectTransform fadeRoot, RectTransform scaleRoot, IList<RectTransform> cards,
        RectTransform picked, float hold, float exitSec, System.Action onDone)
    {
        // 나가는 동안의 재입력은 부르는 쪽이 막는다 (AugmentPickUI·SpinoBetUI 의 closing). 여기서 클릭을 통과시키면 뒤의 HUD 가 눌린다
        CanvasGroup rootG = CardFeel.Group(fadeRoot);

        Image flash = null;
        Vector3 pickedBase = Vector3.one;
        bool pickedBegun = false;
        if (picked != null && hold > 0f)
        {
            flash = CardFeel.MakeOverlay(picked);
            pickedBase = Begin(picked); pickedBegun = true;
            float t = 0f;
            while (t < hold && fadeRoot != null && picked != null)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / hold);
                for (int i = 0; cards != null && i < cards.Count; i++)
                {
                    RectTransform c = cards[i];
                    if (c == null || c == picked) continue;
                    CanvasGroup cg = CardFeel.Group(c);
                    cg.alpha = Mathf.Min(cg.alpha, 1f - 0.75f * k);
                }
                picked.localScale = pickedBase * (1f + 0.05f * Mathf.Sin(k * Mathf.PI * 0.5f));
                if (flash != null) flash.color = new Color(1f, 1f, 1f, 0.35f * (1f - k));
                yield return null;
            }
        }

        Vector3 scaleBase = Vector3.one;
        bool scaleBegun = false;
        if (scaleRoot != null) { scaleBase = Begin(scaleRoot); scaleBegun = true; }
        float t2 = 0f;
        while (t2 < exitSec && fadeRoot != null)
        {
            t2 += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t2 / Mathf.Max(0.01f, exitSec));
            rootG.alpha = 1f - k;
            if (scaleRoot != null) scaleRoot.localScale = scaleBase * Mathf.Lerp(1f, 0.95f, k);
            yield return null;
        }

        // 다음에 다시 열릴 창이니 원래 값으로 (같은 프레임에 onDone 이 창을 끈다)
        if (flash != null) Destroy(flash.gameObject);
        if (pickedBegun) End(picked, pickedBase);
        if (scaleBegun) End(scaleRoot, scaleBase);
        for (int i = 0; cards != null && i < cards.Count; i++)
            if (cards[i] != null) CardFeel.Group(cards[i]).alpha = 1f;
        if (fadeRoot == null) yield break;   // 그사이 창이 없어졌다 (씬이 바뀜) - 끝낼 것이 없다
        rootG.alpha = 1f;
        if (onDone != null) onDone();
    }
}

/// <summary>v1.2 (C3·C4): 카드 묶음 연출 - 하나씩 등장 / 고른 카드만 남기고 창 퇴장. 전부 실시간 (시간이 멈춘 창에서도 움직인다)</summary>
public static class CardFeel
{
    /// <summary>cards 를 순서대로 gap 초 간격으로 하나씩 나타낸다. gap 0 이하이거나 연출이 꺼져 있으면 아무것도 안 한다 (카드는 그냥 보인다)</summary>
    public static void StaggerIn(IList<RectTransform> cards, float gap)
    {
        if (cards == null || !Active(gap)) return;
        UIFeelRunner r = UIFeel.Runner();
        for (int i = 0; i < cards.Count; i++)
        {
            RectTransform c = cards[i];
            if (c == null) continue;
            CanvasGroup g = Group(c);
            g.alpha = 0f;
            r.StartCoroutine(r.CardInRoutine(c, g, i * gap, 0.14f));
        }
    }

    /// <summary>
    /// 카드를 골랐다(picked) 또는 그냥 닫는다(picked = null). fadeRoot 전체가 흐려지고 scaleRoot 가 0.95배로 줄어든 뒤 onDone.
    /// 연출이 꺼져 있으면 바로 onDone. 창이 그사이 없어지지 않는 한(씬 전환) onDone 은 반드시 한 번 불린다
    /// </summary>
    public static void PickExit(RectTransform fadeRoot, RectTransform scaleRoot, IList<RectTransform> cards, RectTransform picked, System.Action onDone)
    {
        if (fadeRoot == null || !GameBalance.CardExitOn || GameBalance.GameFeelMaster <= 0f || !GameBalance.ModalFeelOn)
        {
            if (onDone != null) onDone();
            return;
        }
        UIFeelRunner r = UIFeel.Runner();
        r.StartCoroutine(r.CardExitRoutine(fadeRoot, scaleRoot, cards, picked,
            picked != null ? GameBalance.CardPickHoldSec : 0f, GameBalance.CardExitSec, onDone));
    }

    /// <summary>이 간격으로 StaggerIn 을 부르면 실제로 연출이 도는가 (미리 카드를 숨겨 둘지 정할 때)</summary>
    public static bool Active(float gap)
    {
        return gap > 0f && GameBalance.GameFeelMaster > 0f && GameBalance.ModalFeelOn;
    }

    public static CanvasGroup Group(RectTransform rt)
    {
        CanvasGroup g = rt.GetComponent<CanvasGroup>();
        if (g == null) g = rt.gameObject.AddComponent<CanvasGroup>();
        return g;
    }

    /// <summary>rt 를 꽉 덮는 흰 그림 (클릭은 통과). 고른 카드가 잠깐 밝아지는 데 쓴다</summary>
    public static Image MakeOverlay(RectTransform rt)
    {
        GameObject go = new GameObject("PickFlash");
        RectTransform o = go.AddComponent<RectTransform>();
        o.SetParent(rt, false);
        o.anchorMin = Vector2.zero; o.anchorMax = Vector2.one; o.offsetMin = Vector2.zero; o.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.35f);
        img.raycastTarget = false;
        return img;
    }
}

/// <summary>
/// v1.2: 화면 전체 연출. 어디서든 한 줄, 전부 실시간.
///   WorldDim  - 세상만 잠깐 어둡게 (HUD 는 그대로). 보스 등장
///   Vignette  - 화면 가장자리 색 (가운데는 비침). 패배 순간의 붉은 테두리
///   Curtain   - 결과 화면 뒤에 남는 어둠 (명성 상점 바로 아래 층 - HUD·전장을 가린다). 패배·승리
///   Cover     - 화면을 색으로 덮었다가(그동안 할 일을 하고) 걷는다. 장면 전환(검정)·최종 승리(흰색). 덮여 있는 동안 클릭을 막는다
///   Reveal    - 지금 바로 덮고 걷기만 한다 (앞쪽 페이드 없이). 부르는 쪽이 같은 프레임에 씬을 다시 싣는 견습 종료용
/// </summary>
public static class ScreenFx
{
    private static ScreenFxRunner runner;

    private static ScreenFxRunner R()
    {
        if (runner != null) return runner;
        GameObject go = new GameObject("ScreenFx");
        Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<ScreenFxRunner>();
        runner.Build();
        return runner;
    }

    /// <summary>덮개가 화면을 가리고 있는 중인가 (그동안 새 Cover 요청은 무시된다)</summary>
    public static bool Covering { get { return runner != null && runner.covering; } }

    /// <summary>세상을 alpha 만큼 어둡게: inSec 에 어두워지고 holdSec 머문 뒤 outSec 에 돌아온다</summary>
    public static void WorldDim(float alpha, float inSec, float holdSec, float outSec)
    {
        if (GameBalance.GameFeelMaster <= 0f || alpha <= 0f) return;
        R().PlayDim(alpha * Mathf.Clamp01(GameBalance.GameFeelMaster), inSec, holdSec, outSec);
    }

    /// <summary>가장자리 색을 inSec 에 켠다 (VignetteOff 까지 남는다)</summary>
    public static void Vignette(Color c, float inSec) { if (GameBalance.GameFeelMaster > 0f) R().FadeVignette(c, inSec); }

    public static void VignetteOff(float outSec) { if (runner != null) runner.FadeVignette(new Color(0f, 0f, 0f, 0f), outSec); }

    /// <summary>결과 막: 검정이 sec 동안 alpha 까지 짙어진다 (CurtainOff 또는 씬이 바뀔 때까지 남는다)</summary>
    public static void Curtain(float alpha, float sec) { R().FadeCurtain(alpha, sec); }

    public static void CurtainOff() { if (runner != null) runner.FadeCurtain(0f, 0f); }

    /// <summary>
    /// 화면을 c 로 덮고(inSec) -> whileCovered 실행 -> 걷는다(outSec). 덮는 중이면 false 를 돌려주고 아무것도 안 한다 (연타 방지).
    /// inSec 이 0 이하이거나 연출이 꺼져 있으면 whileCovered 만 바로 실행
    /// </summary>
    public static bool Cover(Color c, float inSec, float outSec, System.Action whileCovered)
    {
        if (inSec <= 0f || GameBalance.GameFeelMaster <= 0f)
        {
            if (whileCovered != null) whileCovered();
            return true;
        }
        if (R().covering) return false;
        runner.StartCoroutine(runner.CoverRoutine(c, inSec, outSec, whileCovered));
        return true;
    }

    /// <summary>
    /// 지금 바로 c 로 덮고 outSec 에 걸쳐 걷는다 (앞쪽 페이드 없음). 부른 쪽이 같은 프레임에 씬을 다시 싣는다 - 새 씬이 밝아지며 나타난다.
    /// 이미 덮는 중이거나 연출이 꺼져 있으면 아무것도 안 한다
    /// </summary>
    public static void Reveal(Color c, float outSec)
    {
        if (outSec <= 0f || GameBalance.GameFeelMaster <= 0f) return;
        if (R().covering) return;
        runner.StartCoroutine(runner.RevealRoutine(c, outSec));
    }
}

public class ScreenFxRunner : MonoBehaviour
{
    private Image dimImg, vigImg, curtainImg, coverImg;
    private Coroutine dimCo, vigCo, curtainCo;
    [System.NonSerialized] public bool covering = false;

    public void Build()
    {
        dimImg = MakeLayer("ScreenFx_World", -50, "Dim", null);           // 세상 바로 위, 모든 HUD 아래
        Transform mid = MakeCanvas("ScreenFx_Result", 555);               // 정비소(550) 위, 명성 상점(560) 아래
        vigImg = MakeImage(mid, "Vignette", MakeVignetteSprite());
        curtainImg = MakeImage(mid, "Curtain", null);
        coverImg = MakeLayer("ScreenFx_Cover", 30000, "Cover", null);     // 맨 위
        SetA(dimImg, 0f); SetA(vigImg, 0f); SetA(curtainImg, 0f); SetA(coverImg, 0f);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    /// <summary>씬이 다시 실렸다 - 지난 판의 어둠·테두리·결과 막을 걷는다 (덮개는 CoverRoutine 이 걷는다)</summary>
    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (dimCo != null) { StopCoroutine(dimCo); dimCo = null; }
        if (vigCo != null) { StopCoroutine(vigCo); vigCo = null; }
        if (curtainCo != null) { StopCoroutine(curtainCo); curtainCo = null; }
        SetA(dimImg, 0f); SetA(vigImg, 0f); SetA(curtainImg, 0f);
    }

    private Transform MakeCanvas(string name, int order)
    {
        Canvas c = UIFactory.CreateCanvas(name, order);
        c.transform.SetParent(transform, false);
        return c.transform;
    }

    private Image MakeLayer(string canvasName, int order, string name, Sprite sprite)
    {
        return MakeImage(MakeCanvas(canvasName, order), name, sprite);
    }

    private static Image MakeImage(Transform parent, string name, Sprite sprite)
    {
        RectTransform rt = KitchenEventManager.MakeBox(parent, name, Color.black);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        Image img = rt.GetComponent<Image>();
        if (sprite != null) img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>가운데가 비고 가장자리로 갈수록 짙어지는 흰 그림 (64x64, 늘려 쓴다)</summary>
    private static Sprite MakeVignetteSprite()
    {
        int s = 64;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x - 31.5f) / 31.5f, dy = (y - 31.5f) / 31.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f;          // 가운데 0, 모서리 1
                float a = Mathf.Clamp01((d - 0.45f) / 0.55f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
    }

    private static void SetA(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color; c.a = a; img.color = c;
        bool show = a > 0.001f;
        if (img.gameObject.activeSelf != show) img.gameObject.SetActive(show);
    }

    // ── 세상 어둠 ──
    public void PlayDim(float alpha, float inSec, float holdSec, float outSec)
    {
        if (dimCo != null) StopCoroutine(dimCo);
        dimCo = StartCoroutine(DimRoutine(alpha, inSec, holdSec, outSec));
    }

    private System.Collections.IEnumerator DimRoutine(float alpha, float inSec, float holdSec, float outSec)
    {
        float from = dimImg.color.a;
        yield return FadeImage(dimImg, from, alpha, inSec);
        float w = 0f;
        while (w < holdSec) { w += Time.unscaledDeltaTime; yield return null; }
        yield return FadeImage(dimImg, alpha, 0f, outSec);
        dimCo = null;
    }

    // ── 가장자리 ──
    public void FadeVignette(Color c, float sec)
    {
        if (vigCo != null) StopCoroutine(vigCo);
        float from = vigImg.color.a;
        if (c.a > 0f) vigImg.color = new Color(c.r, c.g, c.b, from);
        vigCo = StartCoroutine(FadeImage(vigImg, from, c.a, sec));
    }

    // ── 결과 막 ──
    public void FadeCurtain(float alpha, float sec)
    {
        if (curtainCo != null) StopCoroutine(curtainCo);
        curtainCo = StartCoroutine(FadeImage(curtainImg, curtainImg.color.a, alpha, sec));
    }

    // ── 덮개 ──
    public System.Collections.IEnumerator CoverRoutine(Color c, float inSec, float outSec, System.Action whileCovered)
    {
        covering = true;
        coverImg.raycastTarget = true;   // 덮여 있는 동안 클릭을 막는다
        coverImg.color = new Color(c.r, c.g, c.b, 0f);
        yield return FadeImage(coverImg, 0f, c.a, inSec);

        // 덮인 채로 할 일 (씬 다시 싣기 등). 여기서 터져도 덮개는 걷는다 - 검은 화면에 갇히지 않게
        try { if (whileCovered != null) whileCovered(); }
        catch (System.Exception ex) { Debug.LogException(ex); }
        yield return null;   // 새 씬·새 상태가 한 프레임 그려질 틈
        yield return null;

        yield return FadeImage(coverImg, c.a, 0f, outSec);
        coverImg.raycastTarget = false;
        covering = false;
    }

    public System.Collections.IEnumerator RevealRoutine(Color c, float outSec)
    {
        covering = true;
        coverImg.raycastTarget = true;
        coverImg.color = new Color(c.r, c.g, c.b, c.a);
        SetA(coverImg, c.a);
        yield return null;   // 새 씬이 실리고 한 프레임 그려질 틈
        yield return null;
        yield return FadeImage(coverImg, c.a, 0f, outSec);
        coverImg.raycastTarget = false;
        covering = false;
    }

    private const float MAX_FADE_STEP = 0.05f;

    private static System.Collections.IEnumerator FadeImage(Image img, float from, float to, float sec)
    {
        float t = 0f;
        while (t < sec)
        {
            // 씬을 막 실은 프레임은 unscaledDeltaTime 이 로딩 시간만큼 튄다 - 그대로 더하면 페이드가 한 프레임에 끝나 버린다
            t += Mathf.Min(Time.unscaledDeltaTime, MAX_FADE_STEP);
            SetA(img, Mathf.Lerp(from, to, Mathf.Clamp01(t / sec)));
            yield return null;
        }
        SetA(img, to);
    }
}
