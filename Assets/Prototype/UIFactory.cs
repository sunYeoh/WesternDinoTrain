using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [UIFactory.cs]
/// uGUI 요소를 코드로 생성하는 헬퍼 (에디터 Canvas 세팅 불필요)
/// 프로토타입 v3 색상 팔레트 포함
/// VS 2017 (C# 7.3) 호환
/// </summary>
public static class UIFactory
{
    // ── 색상 팔레트 (프로토타입 v3) ──
    public static readonly Color PANEL = new Color(0.227f, 0.141f, 0.094f, 0.92f);   // 다크 우드
    public static readonly Color COPPER = new Color(0.722f, 0.451f, 0.200f, 1f);     // 구리 테두리
    public static readonly Color GOLD = new Color(0.894f, 0.663f, 0.216f, 1f);       // 골드 강조
    public static readonly Color CREAM = new Color(0.969f, 0.910f, 0.776f, 1f);      // 크림 텍스트
    public static readonly Color DIM = new Color(0.604f, 0.518f, 0.408f, 1f);        // 흐린 텍스트
    public static readonly Color T2PINK = new Color(1f, 0.42f, 0.85f, 1f);           // T2 테두리

    public static Color GradeColor(string grade)
    {
        if (grade == "S") return new Color(1f, 0.42f, 0.85f);
        if (grade == "A") return GOLD;
        if (grade == "B") return new Color(0.29f, 0.565f, 0.851f);
        return new Color(0.604f, 0.549f, 0.478f); // C
    }

    // 요리 계열 태그별 색
    public static Color TagColor(FoodTag tag)
    {
        switch (tag)
        {
            case FoodTag.Phys: return new Color(1f, 0.55f, 0.35f);
            case FoodTag.Elec: return new Color(1f, 0.91f, 0.42f);
            case FoodTag.Fire: return new Color(1f, 0.29f, 0.16f);
            case FoodTag.Ice: return new Color(0.48f, 0.85f, 0.91f);
            case FoodTag.Poison: return new Color(0.68f, 0.45f, 0.91f);
            default: return new Color(0.43f, 0.60f, 0.31f); // Def
        }
    }

    private static Font cachedFont;

    /// <summary>한글 지원 폰트 (맑은 고딕 -> 실패 시 내장 폰트)</summary>
    public static Font GetFont()
    {
        if (cachedFont != null) return cachedFont;
        try
        {
            cachedFont = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 20);
        }
        catch (System.Exception) { }
        if (cachedFont == null)
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return cachedFont;
    }

    /// <summary>루트 캔버스 생성 (스크린 오버레이, 1920x1080 기준 스케일)</summary>
    public static Canvas CreateCanvas(string name, int sortOrder)
    {
        GameObject go = new GameObject(name);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        // EventSystem 없으면 생성
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        return canvas;
    }

    /// <summary>단색 패널 (테두리 포함)</summary>
    public static RectTransform CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        Color bg, Color border, float borderWidth)
    {
        // 테두리 (바깥 이미지)
        GameObject borderGo = new GameObject(name);
        borderGo.transform.SetParent(parent, false);
        RectTransform borderRt = borderGo.AddComponent<RectTransform>();
        borderRt.anchorMin = anchorMin;
        borderRt.anchorMax = anchorMax;
        borderRt.offsetMin = offsetMin;
        borderRt.offsetMax = offsetMax;
        Image borderImg = borderGo.AddComponent<Image>();
        borderImg.color = border;

        // 내부 배경
        GameObject bgGo = new GameObject("BG");
        bgGo.transform.SetParent(borderGo.transform, false);
        RectTransform bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = new Vector2(borderWidth, borderWidth);
        bgRt.offsetMax = new Vector2(-borderWidth, -borderWidth);
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = bg;

        return borderRt;
    }

    /// <summary>텍스트 생성</summary>
    public static Text CreateText(Transform parent, string name, string content,
        int size, Color color, TextAnchor align)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Text t = go.AddComponent<Text>();
        t.font = GetFont();
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    /// <summary>버튼 생성 (배경색 + 텍스트)</summary>
    public static Button CreateButton(Transform parent, string name, string label,
        Vector2 size, Color bg, Color textColor, int fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.color = bg;

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = cb;

        Text t = CreateText(go.transform, "Label", label, fontSize, textColor, TextAnchor.MiddleCenter);
        return btn;
    }
}