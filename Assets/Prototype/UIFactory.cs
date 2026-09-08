using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [UIFactory.cs] v4 - uGUI 요소를 코드로 생성하는 헬퍼 (에디터 Canvas 세팅 불필요)
///
/// v4 (2026-09-07, "쇳냄새" 픽셀 스킨): UISkin(ui_*.png)이 있으면
///   - CreatePanel: borderWidth 3 이상 + 큰 창(600x150 이상) = 구리 파이프 프레임(무쇠 평판 내장, 색 인자 무시)
///                  그 외 1~ = 카드(무쇠 평판 + 테두리색 테) / 0 = 예전 단색 박스(암전용)
///   - CreateButton: 무쇠 버튼 판 (bg 색으로 틴트, 너무 어두우면 밝은 무쇠)
///   - CreateCard : 신규 - 평판 + 색 테 카드 (GameHUD 요리 카드)
///   ui_*.png 가 없거나 UISkin.ENABLED=false 면 v3 단색 박스 그대로 (호출부 무수정)
/// 팔레트는 v3 이름 유지 (PANEL/COPPER/GOLD/CREAM/DIM/T2PINK) - 값만 기차 팔레트(무쇠/황동)로
/// VS 2017 (C# 7.3) 호환
/// </summary>
public static class UIFactory
{
    // ── 색상 팔레트 (v4: 무쇠 + 황동) ──
    public static readonly Color PANEL = new Color(0.204f, 0.196f, 0.243f, 0.96f);   // 무쇠 평판 (스킨 없을 때 단색)
    public static readonly Color COPPER = new Color(0.722f, 0.439f, 0.204f, 1f);     // 구리 테두리
    public static readonly Color GOLD = new Color(0.886f, 0.698f, 0.227f, 1f);       // 황동 강조
    public static readonly Color CREAM = new Color(0.969f, 0.910f, 0.776f, 1f);      // 크림 텍스트
    public static readonly Color DIM = new Color(0.627f, 0.549f, 0.431f, 1f);        // 흐린 텍스트
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

    /// <summary>
    /// 한글 지원 폰트.
    /// 1순위: 번들 폰트 Resources/Fonts/GameFont (Neo둥근모 - 빌드에 포함, 어떤 PC에서도 동일)
    /// 2순위: 맑은 고딕 (OS 폰트 - 에디터/윈도우 폴백)
    /// 3순위: 유니티 내장 폰트
    /// KitchenEventManager.GetFont와 같은 우선순위 - 전 UI 폰트 통일
    /// </summary>
    public static Font GetFont()
    {
        if (cachedFont != null) return cachedFont;

        // 1순위: 번들 폰트 (Assets/Resources/Fonts/GameFont.ttf)
        cachedFont = Resources.Load<Font>("Fonts/GameFont");
        if (cachedFont != null) return cachedFont;

        // 2순위: OS 폰트
        try
        {
            cachedFont = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 20);
        }
        catch (System.Exception) { }

        // 3순위: 내장 폰트
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

    /// <summary>
    /// 패널 (테두리 포함). v4: 스킨이 있으면 borderWidth 3 이상(+큰 창) = 파이프 프레임, 그 외 = 카드(평판+색 테), 0 = 단색 박스
    /// 반환값은 바깥(테두리) RectTransform - 자식 "BG"가 안쪽 배경 (호출부는 바깥에 자식을 붙인다)
    /// </summary>
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

        // 파이프 프레임은 테가 28px 라 작은 창(예: 조리 미니게임 440x278)에는 글자가 파이프에 붙는다 - 큰 창(600x150 이상, 늘어나는 축은 통과)만
        float knownW = Mathf.Approximately(anchorMin.x, anchorMax.x) ? offsetMax.x - offsetMin.x : 9999f;
        float knownH = Mathf.Approximately(anchorMin.y, anchorMax.y) ? offsetMax.y - offsetMin.y : 9999f;
        bool bigEnough = knownW >= 600f && knownH >= 150f;

        if (UISkin.Available && borderWidth > 0.5f)
        {
            if (borderWidth >= 3f && bigEnough)
            {
                // 파이프 프레임: 바깥 이미지 하나가 테두리+평판을 다 그린다. 안쪽 BG는 평판 (파이프 안쪽 여백만큼 들여쓰기)
                UISkin.Pipe(borderImg);
                bgRt.offsetMin = new Vector2(28f, 28f);
                bgRt.offsetMax = new Vector2(-28f, -28f);
                UISkin.Plate(bgImg, Color.white);
                bgImg.raycastTarget = false;
            }
            else
            {
                // 카드: 바깥 = 색 테, 안쪽 = 무쇠 평판 (테 두께만큼 들여쓰기)
                Color ringColor = border; ringColor.a = 1f;
                if (border.a < 0.05f) ringColor = UISkin.BRASS_DIM;
                UISkin.Ring(borderImg, ringColor);
                bgRt.offsetMin = new Vector2(10f, 10f);      // 테(리벳 포함 10px) 안쪽만 평판
                bgRt.offsetMax = new Vector2(-10f, -10f);
                UISkin.Plate(bgImg, Color.white);
                bgImg.raycastTarget = false;
            }
        }
        else if (UISkin.Available && borderWidth <= 0.5f && bg.a >= 0.9f
                 && !(anchorMin == Vector2.zero && anchorMax == Vector2.one))
        {
            // 테두리 없는 불투명 박스 (예: 카드 안쪽 판, 전체 화면 스트레치는 제외) - 평판으로만
            UISkin.Plate(bgImg, Color.white);
            UISkin.Plate(borderImg, Color.white);
        }

        return borderRt;
    }

    /// <summary>v4 신규: 카드 (무쇠 평판 + 색 테). 스킨 없으면 CreatePanel(테 2px)과 같다</summary>
    public static RectTransform CreateCard(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color ringColor)
    {
        return CreatePanel(parent, name, anchorMin, anchorMax, offsetMin, offsetMax, PANEL, ringColor, 2f);
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

    /// <summary>버튼 생성 (배경색 + 텍스트). v4: 스킨이 있으면 무쇠 버튼 판(bg 색 틴트)</summary>
    public static Button CreateButton(Transform parent, string name, string label,
        Vector2 size, Color bg, Color textColor, int fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.color = bg;
        if (UISkin.Available)
        {
            Color tint = bg; tint.a = 1f;
            if (tint.r + tint.g + tint.b < 0.45f) tint = UISkin.IRON_LIGHT;   // 너무 어두운 버튼은 밝은 무쇠로 (판 질감이 보이게)
            UISkin.ButtonSkin(img, tint);
        }

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = cb;

        Text t = CreateText(go.transform, "Label", label, fontSize, textColor, TextAnchor.MiddleCenter);
        return btn;
    }
}
