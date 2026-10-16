using System.Collections;
using UnityEngine;

/// <summary>
/// [AttackVFX.cs] v1.1 (v9.24.1 2026-10-16 5Â÷ ÇÇµå¹é 5¹ø "ÅºÈ¯ Áõ¹ß": ProjectileFollow - ¸Å ÇÁ·¹ÀÓ °Ü´« ÀÚ¸®(aimAt)·Î ²ª¾î ³¯¾Æ°¡´Â Åº. ½ð ¼ø°£ÀÇ ÀÚ¸®·Î¸¸ ³¯´ø Projectile Àº ½ºÀ§Ä¡¸¦ ²ö ¶§¸¦ À§ÇØ ±×´ë·Î µÐ´Ù)
/// °ø°Ý ÀÌÆåÆ® ·»´õ·¯ (¿¡¼Â ¾øÀÌ ÄÚµå »ý¼º - ÃßÈÄ ÆÄÆ¼Å¬·Î ±³Ã¼ °¡´É)
/// - Åõ»çÃ¼(ÀÌµ¿ÇÏ´Â ¿ø), °üÅë ºö, Ã¼ÀÎ ¹ø°³(Áö±×Àç±×), Æø¹ß ¿ø, ºÎÃ¤²Ã, ÀåÆÇ
/// ½Ì±ÛÅæ. GameSystems ¿ÀºêÁ§Æ®¿¡ ºÎÂø
/// VS 2017 (C# 7.3) È£È¯
/// </summary>
public class AttackVFX : MonoBehaviour
{
    public static AttackVFX Instance { get; private set; }

    private static Sprite circleSprite; // °ø¿ë ¿øÇü ½ºÇÁ¶óÀÌÆ® (1È¸ »ý¼º)

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>¿øÇü ½ºÇÁ¶óÀÌÆ®¸¦ ÄÚµå·Î »ý¼º (¿¡¼Â ºÒÇÊ¿ä)</summary>
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
                // °¡ÀåÀÚ¸® ºÎµå·´°Ô
                float a = Mathf.Clamp01((r - dist) / 2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        return circleSprite;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Åõ»çÃ¼: ½ÃÀÛ->¸ñÇ¥·Î ³¯¾Æ°¡´Â ¿ø. µµÂø ½Ã onHit ½ÇÇà
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
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

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // v1.1 (v9.24.1): µû¶ó°¡´Â Åõ»çÃ¼ - ¸Å ÇÁ·¹ÀÓ aimAt(Åº ÀÚ¸®) ÀÌ µ¹·ÁÁØ ÀÚ¸®·Î speed ¸¸Å­ ´Ù°¡°£´Ù.
    //   ±× ÀÚ¸®¿¡ ´ê°Å³ª maxFlight ÃÊ°¡ Áö³ª¸é onHit(¶³¾îÁø ÀÚ¸®). Ç¥ÀûÀ» ¹«¾ùÀ¸·Î »ïÀ»Áö´Â ºÎ¸£´Â ÂÊ(aimAt)ÀÌ Á¤ÇÑ´Ù
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public void ProjectileFollow(Vector3 from, System.Func<Vector3, Vector3> aimAt, Color color, float speed,
        float size, float maxFlight, System.Action<Vector3> onHit)
    {
        StartCoroutine(ProjectileFollowCo(from, aimAt, color, speed, size, maxFlight, onHit));
    }

    private IEnumerator ProjectileFollowCo(Vector3 from, System.Func<Vector3, Vector3> aimAt, Color color, float speed,
        float size, float maxFlight, System.Action<Vector3> onHit)
    {
        GameObject go = MakeCircle("VFX_Projectile", from, color, size);
        Vector3 pos = from;
        float flown = 0f;
        float stepSpeed = Mathf.Max(1f, speed);
        // Ã¹ ÇÁ·¹ÀÓÀº ÃÑ±¸¿¡ ±×¸®±â¸¸ ÇÑ´Ù - StartCoroutine Àº Ã¹ yield ±îÁö ±× ÀÚ¸®¿¡¼­ µ¹¾Æ¼­, ¹Ù·Î ¾Õ ¼Õ´ÔÀÌ¸é ½î´Â ÇÔ¼ö ¾È¿¡¼­ ¸Â°í ¾²·¯Á³´Ù (¿¹Àü Åºµµ ÇÑ ÇÁ·¹ÀÓÀº ³¯¾Ò´Ù)
        yield return null;
        while (true)
        {
            if (go == null) yield break;                                  // ÆÇÀ» Á¤¸®ÇÏ¸ç Áö¿î Åº (¿¹Àü Projectile °ú °°°Ô ÆÇÁ¤ ¾øÀÌ ³¡)
            float dt = Time.deltaTime;
            flown += dt;
            Vector3 dest = aimAt != null ? aimAt(pos) : pos;
            Vector3 delta = dest - pos;
            float move = stepSpeed * dt;
            if (delta.magnitude <= move) { pos = dest; break; }            // ´ê¾Ò´Ù
            pos += delta.normalized * move;
            if (maxFlight > 0f && flown >= maxFlight) break;              // ³Ê¹« ¿À·¡ ÂÑ¾Ò´Ù - ±× ÀÚ¸®¿¡¼­ ¸Â´Â´Ù
            go.transform.position = pos;
            yield return null;
        }
        if (go != null) Destroy(go);
        if (onHit != null) onHit(pos);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // °üÅë ºö: ±½Àº Á÷¼±ÀÌ 0.15ÃÊ Ç¥½Ã ÈÄ »ç¶óÁü
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
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

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Ã¼ÀÎ ¹ø°³: Áö±×Àç±× ¶óÀÎ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
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

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Æø¹ß: Ä¿Áö¸ç »ç¶óÁö´Â ¿ø
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
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
            float s = Mathf.Lerp(0.3f, radius * 2f, k); // Áö¸§
            go.transform.localScale = new Vector3(s, s, 1f);
            Color c = color;
            c.a = 1f - k;
            sr.color = c;
            yield return null;
        }
        Destroy(go);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ºÎÃ¤²Ã: ¸ñÇ¥ ¹æÇâ ¹ÝÅõ¸í ºÎÃ¤ (Âª°Ô Ç¥½Ã)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public void Cone(Vector3 origin, Vector3 dir, float range, float halfAngleDeg, Color color)
    {
        GameObject go = new GameObject("VFX_Cone");
        go.transform.position = origin;
        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Sprites/Default"));
        mr.sortingOrder = 5;

        // ºÎÃ¤²Ã ¸Þ½Ã »ý¼º
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

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÀåÆÇ: ¹Ù´Ú¿¡ ³²´Â ¹ÝÅõ¸í ¿ø (Áö¼Ó ½Ã°£ µ¿¾È)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
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
        sr.sortingOrder = -1; // Àû/±âÂ÷ ¾Æ·¡¿¡ ±ò¸²

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            // ¸¶Áö¸· 1ÃÊ ÆäÀÌµå¾Æ¿ô
            if (duration - t < 1f)
            {
                c.a = 0.28f * (duration - t);
                sr.color = c;
            }
            yield return null;
        }
        Destroy(go);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÇïÆÛ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
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