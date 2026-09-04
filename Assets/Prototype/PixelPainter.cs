using UnityEngine;

/// <summary>
/// [PixelPainter.cs] v1 (신규 파일) - 탑뷰 재스킨 공용 도트 캔버스 (목업 v2 컨펌 2026-09-02)
///
/// 코드 생성 비주얼(기차 데크 / 포탑 / 폴백 적 / 작살·레버 / 자원 바위)을
/// "사각형 몇 개"가 아니라 목업 v2와 같은 문법의 도트 그림으로 그리기 위한 도구.
/// 런타임에 Texture2D 위에 픽셀을 찍고 Sprite로 굽는다 (Point 필터 = 도트 그대로).
///
/// 좌표계: 목업 스크립트(PIL)와 같은 "왼쪽 위 원점, y는 아래로" - 목업 좌표를 그대로 옮길 수 있다.
///   (Texture2D의 원점은 왼쪽 아래라 굽는 순간 y를 뒤집는다)
/// 색 램프: 목업 v2 최종값 (구리 5톤 / 무쇠 4톤 / 목재 4톤)을 static으로 공유.
///
/// 사용법: 없음! 파일만 넣으면 된다. TrainDeck / TurretSlot / WaveManager / EngineCab이 쓴다.
/// 아트 반영 시: 각 사용처의 스위치(TurretVisuals / EnemyFallbackVisuals 등)를 끄면 자동으로 빠진다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class PixelPainter
{
    // ── 목업 v2 팔레트 (재질별 3~5톤 램프) ──
    public static readonly Color32 CU_O = new Color32(54, 26, 12, 255);     // 구리 외곽선
    public static readonly Color32 CU_D = new Color32(118, 54, 22, 255);    // 구리 어두움
    public static readonly Color32 CU = new Color32(184, 94, 40, 255);      // 구리 기본
    public static readonly Color32 CU_L = new Color32(220, 136, 72, 255);   // 구리 밝음
    public static readonly Color32 CU_H = new Color32(246, 180, 108, 255);  // 구리 하이라이트
    public static readonly Color32 IR_O = new Color32(20, 16, 13, 255);     // 무쇠 외곽선
    public static readonly Color32 IR_D = new Color32(51, 41, 31, 255);     // 무쇠 어두움
    public static readonly Color32 IR = new Color32(77, 64, 52, 255);       // 무쇠 기본
    public static readonly Color32 IR_L = new Color32(107, 91, 74, 255);    // 무쇠 밝음
    public static readonly Color32 WD_D = new Color32(46, 28, 16, 255);     // 목재 어두움
    public static readonly Color32 WD = new Color32(92, 58, 32, 255);       // 목재 기본
    public static readonly Color32 WD_L = new Color32(122, 78, 44, 255);    // 목재 밝음
    public static readonly Color32 WD_H = new Color32(150, 100, 58, 255);   // 목재 하이라이트
    // ── 메카 팔레트 v2 (레퍼런스: 다이노코어/토큐저 계열 완구 - 빨강 본체 + 금 트림 + 검정 섀시 + 회색 범퍼) ──
    public static readonly Color32 RED_O = new Color32(96, 22, 30, 255);     // 빨강 외곽선
    public static readonly Color32 RED_D = new Color32(178, 44, 52, 255);    // 빨강 어두움 (판넬 분할선)
    public static readonly Color32 RED = new Color32(226, 52, 58, 255);      // 빨강 기본
    public static readonly Color32 RED_L = new Color32(246, 104, 104, 255);    // 빨강 밝음
    public static readonly Color32 GOLD_D = new Color32(140, 96, 20, 255);   // 금 어두움
    public static readonly Color32 GOLD = new Color32(222, 170, 48, 255);    // 금 기본 (트림)
    public static readonly Color32 GOLD_L = new Color32(250, 220, 120, 255); // 금 하이라이트
    public static readonly Color32 BLK_O = new Color32(24, 20, 30, 255);     // 검정 외곽선
    public static readonly Color32 BLK = new Color32(52, 50, 66, 255);       // 검정 섀시/관절
    public static readonly Color32 BLK_L = new Color32(86, 84, 104, 255);     // 검정 밝음
    public static readonly Color32 GREY = new Color32(120, 124, 132, 255);   // 회색 범퍼
    public static readonly Color32 GREY_L = new Color32(170, 174, 182, 255); // 회색 밝음
    public static readonly Color32 SILVER = new Color32(214, 218, 224, 255); // 은색 이빨/날
    public static readonly Color32 WHITE = new Color32(244, 244, 240, 255);  // 흰 플레이트
    public static readonly Color32 EYE_G = new Color32(60, 240, 110, 255);   // 초록 발광 눈
    public static readonly Color32 EYE_GL = new Color32(180, 255, 200, 255); // 눈 하이라이트
    public static readonly Color32 SHADOW = new Color32(0, 0, 0, 70);       // 발밑 그림자 (반투명)
    public static readonly Color32 RIVET_L = new Color32(190, 170, 140, 255);
    public static readonly Color32 CLEAR = new Color32(0, 0, 0, 0);

    public readonly int Width;
    public readonly int Height;
    private readonly Color32[] pixels;   // 왼쪽 위 원점 (y 아래로)

    public PixelPainter(int width, int height)
    {
        Width = width;
        Height = height;
        pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = CLEAR;
    }

    // ─────────────────────────────────────────────
    // 기본 프리미티브 (PIL ImageDraw와 같은 포함 경계)
    // ─────────────────────────────────────────────
    public void Point(int x, int y, Color32 c)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        if (c.a == 255) { pixels[y * Width + x] = c; return; }
        if (c.a == 0) return;
        // 반투명은 단순 알파 블렌드 (그림자용)
        Color32 d = pixels[y * Width + x];
        float a = c.a / 255f;
        pixels[y * Width + x] = new Color32(
            (byte)(c.r * a + d.r * (1f - a)), (byte)(c.g * a + d.g * (1f - a)),
            (byte)(c.b * a + d.b * (1f - a)), (byte)Mathf.Max(d.a, c.a));
    }

    public void Rect(int x0, int y0, int x1, int y1, Color32 fill)
    {
        if (x0 > x1) { int t = x0; x0 = x1; x1 = t; }
        if (y0 > y1) { int t = y0; y0 = y1; y1 = t; }
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++) Point(x, y, fill);
    }

    public void RectOutline(int x0, int y0, int x1, int y1, Color32 c)
    {
        Line(x0, y0, x1, y0, c, 1); Line(x0, y1, x1, y1, c, 1);
        Line(x0, y0, x0, y1, c, 1); Line(x1, y0, x1, y1, c, 1);
    }

    /// <summary>모서리 둥근 사각형 (r = 모서리 반지름). outline은 1px</summary>
    public void RoundRect(int x0, int y0, int x1, int y1, int r, Color32 fill, Color32 outline)
    {
        if (x0 > x1) { int t = x0; x0 = x1; x1 = t; }
        if (y0 > y1) { int t = y0; y0 = y1; y1 = t; }
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                if (!InRound(x, y, x0, y0, x1, y1, r)) continue;
                bool edge = !InRound(x - 1, y, x0, y0, x1, y1, r) || !InRound(x + 1, y, x0, y0, x1, y1, r)
                    || !InRound(x, y - 1, x0, y0, x1, y1, r) || !InRound(x, y + 1, x0, y0, x1, y1, r);
                Point(x, y, edge && outline.a > 0 ? outline : fill);
            }
    }

    private static bool InRound(int x, int y, int x0, int y0, int x1, int y1, int r)
    {
        if (x < x0 || x > x1 || y < y0 || y > y1) return false;
        int cx = x < x0 + r ? x0 + r : (x > x1 - r ? x1 - r : x);
        int cy = y < y0 + r ? y0 + r : (y > y1 - r ? y1 - r : y);
        int dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy <= r * r;
    }

    /// <summary>타원 (경계 상자 포함). outline은 1px (alpha 0이면 생략)</summary>
    public void Ellipse(int x0, int y0, int x1, int y1, Color32 fill, Color32 outline)
    {
        if (x0 > x1) { int t = x0; x0 = x1; x1 = t; }
        if (y0 > y1) { int t = y0; y0 = y1; y1 = t; }
        float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
        float rx = (x1 - x0) * 0.5f + 0.5f, ry = (y1 - y0) * 0.5f + 0.5f;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                if (!InEll(x, y, cx, cy, rx, ry)) continue;
                bool edge = !InEll(x - 1, y, cx, cy, rx, ry) || !InEll(x + 1, y, cx, cy, rx, ry)
                    || !InEll(x, y - 1, cx, cy, rx, ry) || !InEll(x, y + 1, cx, cy, rx, ry);
                if (edge && outline.a > 0) Point(x, y, outline);
                else if (fill.a > 0) Point(x, y, fill);
            }
    }

    private static bool InEll(int x, int y, float cx, float cy, float rx, float ry)
    {
        float dx = (x - cx) / rx, dy = (y - cy) / ry;
        return dx * dx + dy * dy <= 1f;
    }

    /// <summary>선분 (width = 굵기 px)</summary>
    public void Line(int x0, int y0, int x1, int y1, Color32 c, int width)
    {
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int half = width / 2;
        int x = x0, y = y0;
        while (true)
        {
            if (width <= 1) Point(x, y, c);
            else
                for (int oy = -half; oy <= half; oy++)
                    for (int ox = -half; ox <= half; ox++)
                        if (ox * ox + oy * oy <= half * half + half) Point(x + ox, y + oy, c);
            if (x == x1 && y == y1) break;
            int e2 = err * 2;
            if (e2 > -dy) { err -= dy; x += sx; }
            if (e2 < dx) { err += dx; y += sy; }
        }
    }

    /// <summary>다각형 채우기 (pts = x0,y0,x1,y1,...). outline은 1px</summary>
    public void Polygon(int[] pts, Color32 fill, Color32 outline)
    {
        int n = pts.Length / 2;
        if (n < 3) return;
        int minY = int.MaxValue, maxY = int.MinValue;
        for (int i = 0; i < n; i++) { minY = Mathf.Min(minY, pts[i * 2 + 1]); maxY = Mathf.Max(maxY, pts[i * 2 + 1]); }
        if (fill.a > 0)
        {
            // 스캔라인 채우기 (짝수-홀수 규칙)
            float[] xs = new float[n];
            for (int y = minY; y <= maxY; y++)
            {
                int cnt = 0;
                float sy = y + 0.5f;
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    float ax = pts[i * 2], ay = pts[i * 2 + 1], bx = pts[j * 2], by = pts[j * 2 + 1];
                    if ((ay <= sy && by > sy) || (by <= sy && ay > sy))
                        xs[cnt++] = ax + (sy - ay) * (bx - ax) / (by - ay);
                }
                System.Array.Sort(xs, 0, cnt);
                for (int k = 0; k + 1 < cnt; k += 2)
                    for (int x = Mathf.RoundToInt(xs[k]); x <= Mathf.RoundToInt(xs[k + 1]) - 1; x++)
                        Point(x, y, fill);
            }
        }
        if (outline.a > 0)
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Line(pts[i * 2], pts[i * 2 + 1], pts[j * 2], pts[j * 2 + 1], outline, 1);
            }
    }

    /// <summary>
    /// 메카 장갑판: 다각형 채움 + 1px 외곽선 + 북쪽 변(첫 두 점, 대체로 가로) 바로 아래 하이라이트 줄.
    /// pts는 시계 방향으로, 첫 두 점이 북쪽 변이 되게 넣는다.
    /// </summary>
    public void Plate(int[] pts, Color32 fill, Color32 outline, Color32 hilite)
    {
        Polygon(pts, fill, outline);
        if (pts.Length >= 4 && hilite.a > 0)
        {
            int x0 = Mathf.Min(pts[0], pts[2]) + 1, x1 = Mathf.Max(pts[0], pts[2]) - 1;
            int y0 = pts[1] + 1, y1 = pts[3] + 1;
            if (x1 > x0) Line(x0, y0, x1, y1, hilite, 1);
        }
    }

    /// <summary>리벳 1개 (암 + 위쪽 명)</summary>
    public void Rivet(int x, int y) { Rivet(x, y, IR_O, RIVET_L); }
    public void Rivet(int x, int y, Color32 dark, Color32 lite)
    {
        Point(x, y, dark); Point(x, y - 1, lite);
    }

    /// <summary>발밑 그림자 타원 (반투명)</summary>
    public void Shadow(int x0, int y0, int x1, int y1) { Ellipse(x0, y0, x1, y1, SHADOW, CLEAR); }

    // ─────────────────────────────────────────────
    // 굽기
    // ─────────────────────────────────────────────
    /// <summary>
    /// 스프라이트로 굽는다. pivotX/pivotY = 이 캔버스 좌표계(왼쪽 위 원점)의 피벗 픽셀.
    /// pixelsPerUnit = 월드 1유닛에 몇 px인지 (20 = 목업과 같은 배율)
    /// </summary>
    public Sprite Bake(float pixelsPerUnit, float pivotX, float pivotY)
    {
        Texture2D tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] flipped = new Color32[pixels.Length];
        for (int y = 0; y < Height; y++)
            System.Array.Copy(pixels, y * Width, flipped, (Height - 1 - y) * Width, Width);
        tex.SetPixels32(flipped);
        tex.Apply(false, true);
        Vector2 pivot = new Vector2(pivotX / Width, 1f - pivotY / Height);
        return Sprite.Create(tex, new Rect(0, 0, Width, Height), pivot, pixelsPerUnit);
    }

    /// <summary>캔버스 중앙 피벗으로 굽기</summary>
    public Sprite Bake(float pixelsPerUnit) { return Bake(pixelsPerUnit, Width * 0.5f, Height * 0.5f); }

    /// <summary>스프라이트 렌더러 1개를 만들어 붙인다 (공용 헬퍼)</summary>
    public static SpriteRenderer Attach(Transform parent, string name, Sprite sprite, Vector3 localPos, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    /// <summary>Color -> Color32 (램프 보간용)</summary>
    public static Color32 Mix(Color32 a, Color32 b, float t)
    {
        return Color32.Lerp(a, b, Mathf.Clamp01(t));
    }
}
