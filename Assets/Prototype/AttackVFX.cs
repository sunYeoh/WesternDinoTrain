using System.Collections;
using UnityEngine;

/// <summary>
/// [AttackVFX.cs]
/// 공격 이펙트 렌더러 (에셋 없이 코드 생성 - 추후 파티클로 교체 가능)
/// - 투사체(이동하는 원), 관통 빔, 체인 번개(지그재그), 폭발 원, 부채꼴, 장판
/// 싱글톤. GameSystems 오브젝트에 부착
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class AttackVFX : MonoBehaviour
{
    public static AttackVFX Instance { get; private set; }

    private static Sprite circleSprite; // 공용 원형 스프라이트 (1회 생성)

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>원형 스프라이트를 코드로 생성 (에셋 불필요)</summary>
    public static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - r + 0.5f;
                float dy = y - r + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                // 가장자리 부드럽게
                float a = Mathf.Clamp01((r - dist) / 2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        return circleSprite;
    }

    // ─────────────────────────────────────────
    // 투사체: 시작->목표로 날아가는 원. 도착 시 onHit 실행
    // ─────────────────────────────────────────
    public void Projectile(Vector3 from, Vector3 to, Color color, float speed,
        float size, System.Action onHit)
    {
        StartCoroutine(ProjectileCo(from, to, color, speed, size, onHit));
    }

    private IEnumerator ProjectileCo(Vector3 from, Vector3 to, Color color, float speed,
        float size, System.Action onHit)
    {
        GameObject go = MakeCircle("VFX_Projectile", from, color, size);
        float dist = Vector3.Distance(from, to);
        float t = 0f;
        float duration = dist / Mathf.Max(1f, speed);

        while (t < duration)
        {
            t += Time.deltaTime;
            if (go == null) yield break;
            go.transform.position = Vector3.Lerp(from, to, t / duration);
            yield return null;
        }
        Destroy(go);
        if (onHit != null) onHit();
    }

    // ─────────────────────────────────────────
    // 관통 빔: 굵은 직선이 0.15초 표시 후 사라짐
    // ─────────────────────────────────────────
    public void Beam(Vector3 from, Vector3 to, Color color, float width)
    {
        GameObject go = new GameObject("VFX_Beam");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        SetupLine(lr, color, width);
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        StartCoroutine(FadeAndDestroy(go, lr, 0.18f));
    }

    // ─────────────────────────────────────────
    // 체인 번개: 지그재그 라인
    // ─────────────────────────────────────────
    public void Lightning(Vector3 from, Vector3 to, Color color)
    {
        GameObject go = new GameObject("VFX_Lightning");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        SetupLine(lr, color, 0.12f);

        int segments = 6;
        lr.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 p = Vector3.Lerp(from, to, t);
            if (i > 0 && i < segments)
            {
                p.x += Random.Range(-0.35f, 0.35f);
                p.y += Random.Range(-0.35f, 0.35f);
            }
            lr.SetPosition(i, p);
        }
        StartCoroutine(FadeAndDestroy(go, lr, 0.15f));
    }

    // ─────────────────────────────────────────
    // 폭발: 커지며 사라지는 원
    // ─────────────────────────────────────────
    public void Explosion(Vector3 center, Color color, float radius)
    {
        StartCoroutine(ExplosionCo(center, color, radius));
    }

    private IEnumerator ExplosionCo(Vector3 center, Color color, float radius)
    {
        GameObject go = MakeCircle("VFX_Explosion", center, color, 0.3f);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        float t = 0f;
        float dur = 0.32f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            float s = Mathf.Lerp(0.3f, radius * 2f, k); // 지름
            go.transform.localScale = new Vector3(s, s, 1f);
            Color c = color;
            c.a = 1f - k;
            sr.color = c;
            yield return null;
        }
        Destroy(go);
    }

    // ─────────────────────────────────────────
    // 부채꼴: 목표 방향 반투명 부채 (짧게 표시)
    // ─────────────────────────────────────────
    public void Cone(Vector3 origin, Vector3 dir, float range, float halfAngleDeg, Color color)
    {
        GameObject go = new GameObject("VFX_Cone");
        go.transform.position = origin;
        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Sprites/Default"));
        mr.sortingOrder = 5;

        // 부채꼴 메시 생성
        int steps = 12;
        Mesh mesh = new Mesh();
        Vector3[] verts = new Vector3[steps + 2];
        int[] tris = new int[steps * 3];
        verts[0] = Vector3.zero;
        float baseAngle = Mathf.Atan2(dir.y, dir.x);
        float half = halfAngleDeg * Mathf.Deg2Rad;
        for (int i = 0; i <= steps; i++)
        {
            float a = baseAngle - half + (half * 2f) * i / steps;
            verts[i + 1] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * range;
        }
        for (int i = 0; i < steps; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }
        mesh.vertices = verts;
        mesh.triangles = tris;
        mf.mesh = mesh;

        Color c = color;
        c.a = 0.35f;
        mr.material.color = c;

        Destroy(go, 0.22f);
    }

    // ─────────────────────────────────────────
    // 장판: 바닥에 남는 반투명 원 (지속 시간 동안)
    // ─────────────────────────────────────────
    public void Field(Vector3 center, Color color, float radius, float duration)
    {
        StartCoroutine(FieldCo(center, color, radius, duration));
    }

    private IEnumerator FieldCo(Vector3 center, Color color, float radius, float duration)
    {
        GameObject go = MakeCircle("VFX_Field", center, color, radius * 2f);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        Color c = color;
        c.a = 0.28f;
        sr.color = c;
        sr.sortingOrder = -1; // 적/기차 아래에 깔림

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            // 마지막 1초 페이드아웃
            if (duration - t < 1f)
            {
                c.a = 0.28f * (duration - t);
                sr.color = c;
            }
            yield return null;
        }
        Destroy(go);
    }

    // ─────────────────────────────────────────
    // 헬퍼
    // ─────────────────────────────────────────
    private GameObject MakeCircle(string name, Vector3 pos, Color color, float diameter)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(diameter, diameter, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircleSprite();
        sr.color = color;
        sr.sortingOrder = 10;
        return go;
    }

    private void SetupLine(LineRenderer lr, Color color, float width)
    {
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.sortingOrder = 10;
    }

    private IEnumerator FadeAndDestroy(GameObject go, LineRenderer lr, float duration)
    {
        float t = 0f;
        Color start = lr.startColor;
        while (t < duration)
        {
            t += Time.deltaTime;
            Color c = start;
            c.a = 1f - (t / duration);
            lr.startColor = c;
            lr.endColor = c;
            yield return null;
        }
        Destroy(go);
    }
}