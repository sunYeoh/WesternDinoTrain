using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// [UIFeel.cs] v1 (신규, v9.11 2026-09-22) - UI 반응 계층 (스펙 표 C1 C2 + B4 의 UI 쪽)
///
///   ButtonFeel  - 버튼 컴포넌트: 호버 1.03배 / 프레스 0.96배 / 비활성 회색. UIFactory.CreateButton, KitchenEventManager.MakeButton,
///                 GameHUD 요리 카드가 붙인다 (ButtonFeel.Attach(button)). 실시간 기준이라 시간이 멈춘 창에서도 반응한다
///   ModalFeel   - 모달 등장: ModalFeel.Play(루트) 한 줄. 루트의 전체 펼침 자식(어둠)은 알파 0 -> 원래로 0.15초, 나머지 자식(판)은 0.92 -> 1.02 -> 1.0 (0.18초)
///   UIFeel      - Bounce(RectTransform, 양, 초) / FlyTo(캔버스, 화면 좌표, 목표 Rect, 그림, 색, 초, 도착 콜백)
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
        if (root == null || !GameBalance.ModalFeelOn || GameBalance.GameFeelMaster <= 0f) return;
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
}
