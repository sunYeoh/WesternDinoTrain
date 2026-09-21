using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// [HitFeel.cs] v1 (신규, v9.11 2026-09-22) - 타격감 계층: 손님이 맞는다 / 죽는다 / 월드 팝 (스펙 표 A1 A2 A3 A6 + 월드 공용)
///
/// 원칙 (타격감 스펙 표): 일반 사건(매초 수십 번인 명중)은 플래시·찌그러짐·작은 스파크·숫자까지만.
/// 중요 사건(처치·크리·큰 손님)은 킬 버스트 + 채널 쿨타임 흔들림. 흔들림·히트스탑·줌은 여기서 늘리지 않는다.
/// 전 강도는 GameBalance.GameFeelMaster (0 = 전부 끔) + v9.11 섹션 스위치.
///
/// 구성 (전부 이 파일):
///   HitFeel      - 정적 API. TurretAttackExecutor 가 NextHit(색·크리) 를 걸고 Enemy.TakeDamage 가 OnHit, Enemy.Die 가 OnKill 을 부른다
///   HitFeelBody  - 손님 하나에 자동으로 붙는 컴포넌트: 흰 플래시(실루엣 겹침) · 찌그러짐 · 죽는 과정(플래시 -> 찌그러짐 -> 링 + 조각 -> 페이드)
///   FlashSprites - 스프라이트의 흰 실루엣 캐시 (셰이더 없이: 텍스처를 복사해 알파만 남기고 흰색)
///   SparkPool    - 풀링된 스파크 조각 160개 (코루틴·할당 없음 - 물량전 안전)
///   WorldFeel    - 월드 팝 공용: Ring(확산 링) / TextPop("Lv3" 같은 월드 글자) / PlateDrop(접시 낙하)
/// 사용법: 파일만 넣으면 된다. 호출부는 Enemy v3.3 / BossEnemy v7.3 / TurretAttackExecutor v5.1 / TurretSlot v6.5 (같은 팩).
/// VS 2017 (C# 7.3) 호환
/// </summary>
public static class HitFeel
{
    // 다음 TakeDamage 한 번에 쓰일 힌트 (TurretAttackExecutor.DealDamage 가 건다). 소비하면 지워진다
    private static Color nextColor = Color.clear;
    private static bool nextCrit = false;
    private static int nextFrame = -1;

    /// <summary>다음 한 번의 명중에 쓸 색(요리 속성색)·크리 여부. 같은 프레임 안에서만 유효</summary>
    public static void NextHit(Color col, bool crit)
    {
        nextColor = col; nextCrit = crit; nextFrame = Time.frameCount;
    }

    /// <summary>손님이 직접 명중을 맞았다 (도트 틱은 부르지 않는다). Enemy.TakeDamage 에서</summary>
    public static void OnHit(Enemy e, float damage, bool isMagic)
    {
        if (e == null || GameBalance.GameFeelMaster <= 0f || !GameBalance.HitFeelOn) return;
        Color col; bool crit;
        if (nextFrame == Time.frameCount && nextColor.a > 0f) { col = nextColor; crit = nextCrit; }
        else { col = isMagic ? new Color(0.6f, 0.85f, 1f) : new Color(1f, 0.9f, 0.7f); crit = false; }
        nextFrame = -1;

        HitFeelBody body = HitFeelBody.Of(e);
        float ratio = damage / Mathf.Max(1f, e.scaledMaxHP);   // 딜 비례 (적 최대 HP 대비)
        if (body != null) body.Hit(ratio, crit);

        // 스파크: 조각 수 = 딜 비례 3 / 6 / 10, 크리는 10 + 흰 조각
        int count = crit ? 10 : (ratio < 0.1f ? 3 : (ratio < 0.3f ? 6 : 10));
        float size = crit ? 0.16f : 0.11f;
        SparkPool.Emit(e.transform.position, col, count, size, crit ? 3.6f : 2.6f);
        if (crit) SparkPool.Emit(e.transform.position, Color.white, 3, 0.13f, 4f);
    }

    /// <summary>손님이 죽었다 - 죽는 과정 시작. Enemy.Die 에서 (DeathPop 은 그대로 두고 그 위에 얹는다)</summary>
    public static void OnKill(Enemy e, Color col, bool boss)
    {
        if (e == null || GameBalance.GameFeelMaster <= 0f || !GameBalance.KillBurstOn) return;
        HitFeelBody body = HitFeelBody.Of(e);
        bool big = boss || e.scaledMaxHP >= GameBalance.KillBurstBigHP;
        if (body != null) body.Kill(boss ? 0.45f : 0.28f, big);

        Vector3 pos = e.transform.position;
        WorldFeel.Ring(pos, boss ? new Color(1f, 0.85f, 0.4f) : col, boss ? 2.2f : (big ? 1.4f : 0.9f), boss ? 0.5f : 0.25f);
        SparkPool.Emit(pos, col, big ? 14 : 8, big ? 0.15f : 0.12f, big ? 3.8f : 3f);
        SparkPool.Emit(pos, Color.white, big ? 6 : 3, 0.1f, 4.5f);
        if (big && !boss) GameFeel.Shake(GameBalance.ShakeBigKill, "kill", 0.3f);
    }
}

/// <summary>손님 하나의 몸 연출. HitFeel 이 필요할 때 붙인다 (씬·프리팹 수정 0)</summary>
public class HitFeelBody : MonoBehaviour
{
    private SpriteRenderer[] bodies = new SpriteRenderer[0];    // 실제 보이는 렌더러들 (Skin 또는 프리팹 것)
    private SpriteRenderer[] flashes = new SpriteRenderer[0];   // 각 렌더러 위에 겹친 흰 실루엣
    private Transform squashTf;            // 찌그러뜨릴 트랜스폼 (주 렌더러의 것)
    private Vector3 squashBase = Vector3.one;
    private float flashUntil = 0f;
    private float squashT = -1f, squashAmt = 0f;
    private const float SQUASH_SEC = 0.12f;
    private bool dying = false;
    private float dieT = 0f, dieSec = 0.28f;
    private Color[] dieStart = new Color[0];
    private float lastHitTime = -1f;

    public static HitFeelBody Of(Enemy e)
    {
        if (e == null) return null;
        HitFeelBody b = e.GetComponent<HitFeelBody>();
        if (b == null) { b = e.gameObject.AddComponent<HitFeelBody>(); b.Bind(); }
        else if (b.bodies.Length == 0) b.Bind();
        return b;
    }

    /// <summary>보이는 렌더러를 찾아 묶는다 (EnemySkin 의 Skin / WaveManager 폴백 Body / 프리팹 렌더러)</summary>
    private void Bind()
    {
        List<SpriteRenderer> list = new List<SpriteRenderer>();
        SpriteRenderer[] all = GetComponentsInChildren<SpriteRenderer>(false);
        for (int i = 0; i < all.Length; i++)
        {
            SpriteRenderer sr = all[i];
            if (sr == null || !sr.enabled || sr.sprite == null) continue;
            if (sr.gameObject.name == "HpBarBg" || sr.gameObject.name == "HpBarFill" || sr.gameObject.name.StartsWith("Flash_")) continue;
            list.Add(sr);
        }
        bodies = list.ToArray();
        flashes = new SpriteRenderer[bodies.Length];
        if (bodies.Length > 0)
        {
            // 주 렌더러 = 가장 큰 것. 그 트랜스폼을 찌그러뜨린다 (루트가 아니면 루트 회전과 무관)
            int main = 0; float best = -1f;
            for (int i = 0; i < bodies.Length; i++)
            {
                float a = bodies[i].bounds.size.x * bodies[i].bounds.size.y;
                if (a > best) { best = a; main = i; }
            }
            squashTf = bodies[main].transform;
            squashBase = squashTf.localScale;
        }
    }

    /// <summary>묶어 둔 렌더러가 꺼졌으면(스폰 직후 맞았다가 EnemySkin 이 나중에 입혀진 경우) 다시 묶는다</summary>
    private void EnsureBound()
    {
        bool stale = bodies.Length == 0;
        for (int i = 0; i < bodies.Length && !stale; i++)
            if (bodies[i] == null || !bodies[i].enabled) stale = true;
        if (!stale) return;
        for (int i = 0; i < flashes.Length; i++) if (flashes[i] != null) Destroy(flashes[i].gameObject);
        if (squashTf != null) squashTf.localScale = squashBase;
        Bind();
    }

    /// <summary>명중: 흰 플래시 + 찌그러짐 (딜 비례)</summary>
    public void Hit(float ratio, bool crit)
    {
        if (dying) return;
        EnsureBound();
        if (bodies.Length == 0) return;
        if (Time.time - lastHitTime < 0.04f) return;   // 같은 프레임 다중 명중은 한 번만
        lastHitTime = Time.time;
        flashUntil = Time.time + GameBalance.HitFlashSec;
        SetFlash(true, 1f);
        squashAmt = (ratio >= 0.1f || crit) ? GameBalance.HitSquashBig : GameBalance.HitSquash;
        squashT = 0f;
    }

    /// <summary>죽는 과정: 플래시 -> 찌그러짐 -> 페이드 (조각·링은 HitFeel.OnKill 이 만든다)</summary>
    public void Kill(float sec, bool big)
    {
        EnsureBound();
        if (bodies.Length == 0) return;
        dying = true; dieT = 0f; dieSec = sec;
        dieStart = new Color[bodies.Length];
        for (int i = 0; i < bodies.Length; i++) dieStart[i] = bodies[i] != null ? bodies[i].color : Color.white;
        flashUntil = Time.time + 0.08f;
        SetFlash(true, 1f);
        squashAmt = big ? 0.3f : 0.22f;
        squashT = 0f;
    }

    private void Update()
    {
        if (bodies.Length == 0) return;
        float now = Time.time;

        // 플래시 끝
        if (flashUntil > 0f && now >= flashUntil) { flashUntil = 0f; SetFlash(false, 0f); }

        // 찌그러짐: (1+a, 1-a) 에서 0.12초 동안 원래 크기로 (약간 되튀김)
        if (squashT >= 0f && squashTf != null)
        {
            squashT += Time.deltaTime;
            float k = Mathf.Clamp01(squashT / SQUASH_SEC);
            float w = 1f - k; w = w * w;                     // easeOut
            float bounce = Mathf.Sin(k * Mathf.PI) * 0.35f;  // 중간에 반대로 살짝
            float sx = 1f + squashAmt * w - squashAmt * bounce * (1f - w);
            float sy = 1f - squashAmt * w + squashAmt * bounce * (1f - w);
            if (dying)
            {
                // 죽을 땐 옆으로 조금 퍼지며 납작해진다 (큰 손님도 반쯤은 남게)
                sx = 1f + squashAmt * k * 0.5f; sy = Mathf.Max(0.05f, 1f - squashAmt * k - 0.25f * k);
            }
            squashTf.localScale = new Vector3(squashBase.x * sx, squashBase.y * sy, squashBase.z);
            if (k >= 1f && !dying) { squashT = -1f; squashTf.localScale = squashBase; }
        }

        // 죽는 과정: 실루엣 페이드
        if (dying)
        {
            dieT += Time.deltaTime;
            float k = Mathf.Clamp01(dieT / dieSec);
            for (int i = 0; i < bodies.Length; i++)
            {
                if (bodies[i] == null) continue;
                Color c = dieStart[i]; c.a = dieStart[i].a * (1f - k);
                bodies[i].color = c;
            }
            if (k >= 1f) { for (int i = 0; i < bodies.Length; i++) if (bodies[i] != null) bodies[i].enabled = false; enabled = false; }
        }
    }

    /// <summary>흰 실루엣 켜기/끄기 (처음 켤 때 만든다)</summary>
    private void SetFlash(bool on, float alpha)
    {
        for (int i = 0; i < bodies.Length; i++)
        {
            SpriteRenderer sr = bodies[i];
            if (sr == null) continue;
            if (flashes[i] == null)
            {
                if (!on) continue;
                Sprite white = FlashSprites.Get(sr.sprite);
                if (white == null) continue;
                GameObject go = new GameObject("Flash_" + i);
                go.transform.SetParent(sr.transform, false);
                go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
                SpriteRenderer fs = go.AddComponent<SpriteRenderer>();
                fs.sprite = white; fs.sortingLayerID = sr.sortingLayerID; fs.sortingOrder = sr.sortingOrder + 1;
                fs.flipX = sr.flipX; fs.flipY = sr.flipY;
                flashes[i] = fs;
            }
            SpriteRenderer f = flashes[i];
            if (on)
            {
                // 스프라이트가 바뀌었을 수 있다 (걷기 프레임 등)
                if (f.sprite == null || f.sprite.name != sr.sprite.name + "_white") { Sprite w = FlashSprites.Get(sr.sprite); if (w != null) f.sprite = w; }
                f.flipX = sr.flipX; f.flipY = sr.flipY;
                f.color = new Color(1f, 1f, 1f, alpha * Mathf.Clamp01(GameBalance.GameFeelMaster));
                f.enabled = true;
            }
            else f.enabled = false;
        }
    }
}

/// <summary>스프라이트의 흰 실루엣 캐시. 셰이더 없이 텍스처를 읽어(Blit + ReadPixels) 알파만 남기고 흰색으로</summary>
public static class FlashSprites
{
    private static readonly Dictionary<Texture2D, Texture2D> whiteTex = new Dictionary<Texture2D, Texture2D>();
    private static readonly Dictionary<Sprite, Sprite> cache = new Dictionary<Sprite, Sprite>();

    public static Sprite Get(Sprite s)
    {
        if (s == null || s.texture == null) return null;
        Sprite w;
        if (cache.TryGetValue(s, out w)) return w;

        Texture2D wt;
        if (!whiteTex.TryGetValue(s.texture, out wt))
        {
            wt = MakeWhite(s.texture);
            whiteTex[s.texture] = wt;
        }
        if (wt == null) { cache[s] = null; return null; }
        Vector2 pivot = new Vector2(s.pivot.x / Mathf.Max(1f, s.rect.width), s.pivot.y / Mathf.Max(1f, s.rect.height));
        w = Sprite.Create(wt, s.rect, pivot, s.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        w.name = s.name + "_white";
        cache[s] = w;
        return w;
    }

    /// <summary>텍스처 전체를 흰 실루엣으로 복사 (한 텍스처에 여러 스프라이트가 있어도 한 번만)</summary>
    private static Texture2D MakeWhite(Texture2D src)
    {
        try
        {
            RenderTexture rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            Color32[] px = copy.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                byte a = px[i].a;
                px[i] = new Color32(255, 255, 255, a);
            }
            copy.SetPixels32(px);
            copy.Apply(false, false);
            copy.filterMode = src.filterMode;
            copy.wrapMode = TextureWrapMode.Clamp;
            return copy;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[HitFeel] 실루엣 생성 실패 (" + src.name + "): " + ex.Message + " - 플래시 생략");
            return null;
        }
    }
}

/// <summary>풀링된 스파크 조각. Emit 한 줄. 160개 넘으면 오래된 것부터 재사용</summary>
public class SparkPool : MonoBehaviour
{
    private const int MAX = 160;
    private const int SORT_ORDER = 58;   // 재료 조각(60) 아래, 손님(5) 위
    private static SparkPool inst;

    private Transform[] tf = new Transform[MAX];
    private SpriteRenderer[] sr = new SpriteRenderer[MAX];
    private Vector2[] vel = new Vector2[MAX];
    private float[] life = new float[MAX], age = new float[MAX], size = new float[MAX];
    private Color[] col = new Color[MAX];
    private bool[] on = new bool[MAX];
    private int cursor = 0;
    private int emittedThisFrame = 0, emitFrame = -1;

    private static void Ensure()
    {
        if (inst != null) return;
        GameObject go = new GameObject("SparkPool");
        Object.DontDestroyOnLoad(go);
        inst = go.AddComponent<SparkPool>();
        Sprite sq = TrainDeck.GetWhiteSprite();
        for (int i = 0; i < MAX; i++)
        {
            GameObject p = new GameObject("Spark");
            p.transform.SetParent(go.transform, false);
            SpriteRenderer s = p.AddComponent<SpriteRenderer>();
            s.sprite = sq; s.sortingOrder = SORT_ORDER;
            p.SetActive(false);
            inst.tf[i] = p.transform; inst.sr[i] = s;
        }
    }

    /// <summary>조각 count 개를 pos 에서 사방으로. size = 한 변(유닛), speed = 초기 속도</summary>
    public static void Emit(Vector3 pos, Color c, int count, float sz, float speed)
    {
        if (GameBalance.GameFeelMaster <= 0f || !GameBalance.HitSparksOn) return;
        Ensure();
        SparkPool p = inst;
        if (p.emitFrame != Time.frameCount) { p.emitFrame = Time.frameCount; p.emittedThisFrame = 0; }
        if (p.emittedThisFrame >= 48) return;   // 한 프레임에 너무 많이 (물량전) 는 조용히 생략
        for (int n = 0; n < count; n++)
        {
            int i = p.cursor; p.cursor = (p.cursor + 1) % MAX;
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float spd = speed * Random.Range(0.6f, 1.2f);
            p.vel[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
            p.life[i] = Random.Range(0.16f, 0.26f); p.age[i] = 0f; p.size[i] = sz * Random.Range(0.7f, 1.2f);
            p.col[i] = c; p.on[i] = true;
            p.tf[i].position = pos + new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.15f, 0.15f), 0f);
            p.tf[i].rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));
            p.tf[i].localScale = Vector3.one * p.size[i];
            p.sr[i].color = c;
            p.tf[i].gameObject.SetActive(true);
            p.emittedThisFrame++;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;   // 히트스톱 중엔 조각도 멈춘다 (정지감)
        for (int i = 0; i < MAX; i++)
        {
            if (!on[i]) continue;
            age[i] += dt;
            float k = age[i] / life[i];
            if (k >= 1f) { on[i] = false; tf[i].gameObject.SetActive(false); continue; }
            vel[i] *= 1f - 5f * dt;
            tf[i].position += (Vector3)(vel[i] * dt);
            tf[i].localScale = Vector3.one * size[i] * (1f - k * 0.8f);
            Color c = col[i]; c.a = 1f - k * k;
            sr[i].color = c;
        }
    }
}

/// <summary>월드 팝 공용 (링·글자·접시). 어디서든 한 줄</summary>
public static class WorldFeel
{
    private static Sprite ringSprite;
    private static WorldFeelRunner runner;

    private static WorldFeelRunner Runner()
    {
        if (runner != null) return runner;
        GameObject go = new GameObject("WorldFeel");
        Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<WorldFeelRunner>();
        return runner;
    }

    /// <summary>확산 링: 지름 0 -> radius*2 로 sec 동안 커지며 사라진다</summary>
    public static void Ring(Vector3 pos, Color c, float radius, float sec)
    {
        if (GameBalance.GameFeelMaster <= 0f) return;
        GameObject go = new GameObject("Ring");
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetRing(); sr.color = c; sr.sortingOrder = 57;
        Runner().StartCoroutine(Runner().RingRoutine(go.transform, sr, radius, sec, c));
    }

    /// <summary>월드 글자 팝 ("Lv3", "+1 접시"): 위로 0.6u 떠오르며 0.7초에 사라진다</summary>
    public static void TextPop(Vector3 pos, string text, Color c, float size)
    {
        if (GameBalance.GameFeelMaster <= 0f) return;
        GameObject go = new GameObject("TextPop");
        go.transform.position = pos;
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        TMPFontFixer.Apply(tmp);
        tmp.text = text; tmp.fontSize = size; tmp.color = c; tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold; tmp.sortingOrder = 101;
        Runner().StartCoroutine(Runner().TextRoutine(go.transform, tmp, c));
    }

    /// <summary>접시 낙하: pos 위 0.9u 에서 떨어져 pos 에 닿으며 납작 -> 사라짐 (0.22초). 포탑 투입용</summary>
    public static void PlateDrop(Vector3 pos, Color c)
    {
        if (GameBalance.GameFeelMaster <= 0f) return;
        GameObject go = new GameObject("Plate");
        go.transform.position = pos + Vector3.up * 0.9f;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = TrainDeck.GetCircleSprite(); sr.color = c; sr.sortingOrder = 8;
        go.transform.localScale = new Vector3(0.45f, 0.3f, 1f);
        Runner().StartCoroutine(Runner().PlateRoutine(go.transform, sr, pos, c));
    }

    /// <summary>1유닛 지름 링 (선 두께 3px, 32px)</summary>
    private static Sprite GetRing()
    {
        if (ringSprite != null) return ringSprite;
        int s = 32;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        float r = s * 0.5f - 0.5f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = x - r, dy = y - r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                bool ring = d <= r && d >= r - 3f;
                tex.SetPixel(x, y, ring ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        ringSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        return ringSprite;
    }

    public class WorldFeelRunner : MonoBehaviour
    {
        public System.Collections.IEnumerator RingRoutine(Transform t, SpriteRenderer sr, float radius, float sec, Color c)
        {
            float k = 0f;
            while (k < 1f && t != null)
            {
                k += Time.deltaTime / Mathf.Max(0.01f, sec);
                float e = 1f - (1f - k) * (1f - k);   // easeOut
                t.localScale = Vector3.one * radius * 2f * e;
                sr.color = new Color(c.r, c.g, c.b, (1f - k) * 0.9f);
                yield return null;
            }
            if (t != null) Object.Destroy(t.gameObject);
        }

        public System.Collections.IEnumerator TextRoutine(Transform t, TextMeshPro tmp, Color c)
        {
            float age = 0f, life = 0.7f;
            Vector3 start = t.position;
            t.localScale = Vector3.one * 1.3f;
            while (age < life && t != null)
            {
                age += Time.deltaTime;
                float k = age / life;
                t.position = start + Vector3.up * 0.6f * (1f - (1f - k) * (1f - k));
                t.localScale = Vector3.one * (1f + 0.3f * Mathf.Max(0f, 1f - k * 5f));
                tmp.color = new Color(c.r, c.g, c.b, 1f - k * k);
                yield return null;
            }
            if (t != null) Object.Destroy(t.gameObject);
        }

        public System.Collections.IEnumerator PlateRoutine(Transform t, SpriteRenderer sr, Vector3 target, Color c)
        {
            float age = 0f, fall = 0.16f, land = 0.1f;
            Vector3 start = t.position;
            while (age < fall && t != null)
            {
                age += Time.deltaTime;
                float k = age / fall; k = k * k;   // 가속 낙하
                t.position = Vector3.Lerp(start, target, k);
                yield return null;
            }
            age = 0f;
            while (age < land && t != null)
            {
                age += Time.deltaTime;
                float k = age / land;
                t.localScale = new Vector3(0.45f + 0.25f * k, 0.3f - 0.2f * k, 1f);
                sr.color = new Color(c.r, c.g, c.b, 1f - k);
                yield return null;
            }
            if (t != null) Object.Destroy(t.gameObject);
        }
    }
}
