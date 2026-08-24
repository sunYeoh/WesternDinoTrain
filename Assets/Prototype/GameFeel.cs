using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// [GameFeel.cs] v1 (½Å±Ô ÆÄÀÏ) - P1: °ÔÀÓÇÊ °èÃþ (±â¼ú°¨»ç Ã³¹æ)
/// È­¸é ¼ÎÀÌÅ© / È÷Æ®½ºÅé / Ã³Ä¡ ÆËÀ» static ÇÑ ÁÙ È£Ãâ·Î Á¦°øÇÏ´Â ¿¬Ãâ ¿£Áø.
///
/// ¼³°è ¿øÄ¢ (»ç¿ëÀÚ Áö½Ã: "°úÇÏ¸é ÇÇ·Î¿Í ¸Ö¹Ì - Àû´çÇÑ Å¸ÇùÁ¡"):
///  - Àü °­µµ´Â GameBalance '°ÔÀÓÇÊ' ¼½¼Ç °è¼ö·Î Á¦¾î. 0ÀÌ¸é ÇØ´ç ¿¬Ãâ ¿ÏÀü ²¨Áü
///  - GameFeelMaster ÇÏ³ª·Î ÀüÃ¼ ÀÏ°ý Á¶Àý (ÇÃ·¹ÀÌÅ×½ºÆ®¿¡¼­ ÀÌ °ª¸¸ ¸¸Áö¸é µÊ)
///  - ¼ÎÀÌÅ©´Â Perlin °î¼±(ºÎµå·¯¿î ¿¬¼Ó Èçµé¸²) + È¸Àü ¾øÀ½ - ¸Ö¹Ì ÃÖ¼ÒÈ­
///  - ¼ÎÀÌÅ©´Â Ä«¸Þ¶ó¸¸ Èçµç´Ù. Á¶¸® ¹Ì´Ï°ÔÀÓ µî UI´Â Èçµé¸®Áö ¾Ê¾Æ ÆÇÁ¤ ¹æÇØ ¾øÀ½
///  - È÷Æ®½ºÅéÀº º¸½º ¼ø°£¿¡¸¸ (±×·Î±â ÁøÀÔ/º¸½º Ã³Ä¡) - ³²¹ß ±ÝÁö + 0.45ÃÊ Àç»ç¿ë Á¦ÇÑ
///
/// »ç¿ë¹ý: ¾øÀ½! ¾î´À ½ºÅ©¸³Æ®µç GameFeel.Shake(0.3f) Ã³·³ ºÎ¸£¸é ÀÚµ¿ »ý¼ºµÈ´Ù.
///  - GameFeel.Shake(°­µµ)                    : È­¸é Èçµé¸² (ÄðÅ¸ÀÓ ¾øÀ½ - º¸½º µî µå¹® ¼ø°£¿ë)
///  - GameFeel.Shake(°­µµ, Ã¤³Î, ÄðÅ¸ÀÓ)      : °°Àº Ã¤³ÎÀº ÄðÅ¸ÀÓ(ÃÊ)¿¡ ÇÑ ¹ø¸¸ - ÀæÀº ÇÇ°Ý¿ë
///  - GameFeel.Hitstop(ÃÊ)                    : ÂªÀº ½Ã°£ Á¤Áö (½Ç½Ã°£ ±âÁØ)
///  - GameFeel.DeathPop(À§Ä¡, »ö)             : Ã³Ä¡ ¼ø°£ Á¶°¢ ÆË
/// Ä«¸Þ¶ó ¹Ý¿µÀº CameraZoom v3°¡ GameFeel.ShakeOffsetÀ» ÀÐ¾î Ã³¸®ÇÑ´Ù.
/// VS 2017 (C# 7.3) È£È¯.
/// </summary>
public class GameFeel : MonoBehaviour
{
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¼ÎÀÌÅ© »óÅÂ (Æ®¶ó¿ì¸¶ ¹æ½Ä: °­µµÀÇ Á¦°öÀ¸·Î ÁøÆø °è»ê - ÀÜÁøµ¿ÀÌ »¡¸® Àæ¾Æµê)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    /// <summary>ÇöÀç ÇÁ·¹ÀÓÀÇ Ä«¸Þ¶ó Èçµé¸² ¿ÀÇÁ¼Â (CameraZoomÀÌ ÀÐÀ½, ¿ùµå ´ÜÀ§)</summary>
    public static Vector2 ShakeOffset { get; private set; }

    private static float trauma = 0f;              // 0~1 ´©Àû Ãæ°Ý·®
    private const float TRAUMA_DECAY = 1.4f;       // ÃÊ´ç °¨¼è (Å¬¼ö·Ï »¡¸® ¸ØÃã)
    private const float MAX_OFFSET = 0.5f;         // Æ®¶ó¿ì¸¶ 1.0ÀÏ ¶§ ÃÖ´ë ¿ÀÇÁ¼Â (¿ùµå ´ÜÀ§, ÁÜ7 ±âÁØ)
    private const float NOISE_FREQ = 19f;          // Èçµé¸² ¼Óµµ (³Ê¹« Å©¸é ¸Ö¹Ì)

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // È÷Æ®½ºÅé »óÅÂ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private static float lastHitstopReal = -10f;   // ¸¶Áö¸· È÷Æ®½ºÅé ½Ã°¢ (½Ç½Ã°£)
    private static bool hitstopActive = false;
    private const float HITSTOP_MIN_GAP = 0.45f;   // ¿¬¼Ó °­Å¸ ½Ã ½ºÆ®·Îºê ¹æÁö °£°Ý
    private const float HITSTOP_SCALE = 0.05f;     // Á¤Áö Áß ½Ã°£ ¹èÀ² (¿ÏÀü 0 ´ë½Å ¹Ì¼¼ÇÏ°Ô Èå¸§)

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Ã³Ä¡ ÆË »óÅÂ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private static int activePops = 0;             // µ¿½Ã Àç»ý ¼ö (¹°·®Àü ÇÁ·¹ÀÓ º¸È£)
    private const int MAX_POPS = 24;
    private static Sprite squareSprite;            // 4x4 Èò »ç°¢Çü (1È¸ »ý¼º Ä³½Ã)

    private static GameFeel instance;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // °ø°³ API
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    // Ã¤³Îº° ¸¶Áö¸· ¼ÎÀÌÅ© ½Ã°¢ (½Ç½Ã°£ ±âÁØ)
    private static Dictionary<string, float> channelLastShake = new Dictionary<string, float>();

    /// <summary>È­¸é ¼ÎÀÌÅ© (ÄðÅ¸ÀÓ ¾øÀ½ - ·±Áö/±×·Î±â/º¸½º Ã³Ä¡ °°Àº µå¹® ´ëÇü ¼ø°£¿¡¸¸ ¾µ °Í)</summary>
    public static void Shake(float strength)
    {
        if (strength <= 0f || GameBalance.GameFeelMaster <= 0f) return;
        Ensure();
        trauma = Mathf.Clamp01(trauma + strength);
    }

    /// <summary>
    /// ÄðÅ¸ÀÓ ÀÖ´Â ¼ÎÀÌÅ©. °°Àº channelÀº cooldownÃÊ¿¡ ÇÑ ¹ø¸¸ Èçµé¸°´Ù.
    /// ÀæÀº ÀÌº¥Æ®(±âÂ÷ ÇÇ°Ý, Æø¹ß)°¡ È­¸éÀ» ½¬Áö ¾Ê°í Èçµé¾î ÇÇ·ÎÇØÁö´Â °ÍÀ» ¸·´Â´Ù.
    /// </summary>
    public static void Shake(float strength, string channel, float cooldown)
    {
        if (strength <= 0f || GameBalance.GameFeelMaster <= 0f) return;

        float last;
        if (channelLastShake.TryGetValue(channel, out last)
            && Time.realtimeSinceStartup - last < cooldown)
            return;   // ¾ÆÁ÷ ÄðÅ¸ÀÓ - Á¶¿ëÈ÷ ¹«½Ã

        channelLastShake[channel] = Time.realtimeSinceStartup;
        Shake(strength);
    }

    /// <summary>È÷Æ®½ºÅé (duration = ½Ç½Ã°£ ÃÊ). ÀÏ½ÃÁ¤Áö/QTE µî ´Ù¸¥ ½Ã°£ Á¶ÀÛ°ú´Â Àý´ë °ãÄ¡Áö ¾Ê´Â´Ù</summary>
    public static void Hitstop(float duration)
    {
        if (duration <= 0f || GameBalance.GameFeelMaster <= 0f) return;
        if (Time.timeScale != 1f) return;                                    // ÀÏ½ÃÁ¤Áö/QTE/½ºÅä¸® ÁßÀÌ¸é ¾çº¸
        if (Time.realtimeSinceStartup - lastHitstopReal < HITSTOP_MIN_GAP) return;
        Ensure();
        instance.StartCoroutine(instance.HitstopRoutine(
            duration * Mathf.Clamp01(GameBalance.GameFeelMaster)));
    }

    /// <summary>Ã³Ä¡ ÆË (±âº» Å©±â)</summary>
    public static void DeathPop(Vector3 pos, Color col)
    {
        DeathPop(pos, col, 1f);
    }

    /// <summary>Ã³Ä¡ ÆË. sizeMul 2 ÀÌ»óÀÌ¸é Á¶°¢ ¼öµµ ´Ã¾î³­´Ù (º¸½º¿ë 3f ±ÇÀå)</summary>
    public static void DeathPop(Vector3 pos, Color col, float sizeMul)
    {
        if (GameBalance.DeathPopScale <= 0f || GameBalance.GameFeelMaster <= 0f) return;
        if (activePops >= MAX_POPS) return;   // ¹°·®Àü ÇÁ·¹ÀÓ º¸È£ (ÃÊ°úºÐÀº Á¶¿ëÈ÷ »ý·«)
        Ensure();
        instance.StartCoroutine(instance.PopRoutine(pos, col, sizeMul * GameBalance.DeathPopScale));
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÀÚµ¿ »ý¼º
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
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

    // ¾À ¸®·Îµå([´Ù½Ã ±Á´Â´Ù] µî) ¾ÈÀüÀåÄ¡: ÁøÇà ÁßÀÌ´ø ¼ÎÀÌÅ©/È÷Æ®½ºÅéÀÌ °íÂøµÇÁö ¾Ê°Ô Á¤¸®
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
        instance = null;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¼ÎÀÌÅ© °»½Å (½Ç½Ã°£ ±âÁØ - È÷Æ®½ºÅé Áß¿¡µµ ÀÜÁøµ¿ÀÌ »ì¾ÆÀÖ¾î Å¸°Ý°¨ À¯Áö)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void Update()
    {
        // ÁøÂ¥ ÀÏ½ÃÁ¤Áö(¸Þ´º/QTE/½ºÅä¸®) Áß¿¡´Â È­¸éÀ» °íÁ¤ÇÑ´Ù (È÷Æ®½ºÅéÀº ¿¹¿Ü)
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

        // Á¦°ö °¨¼è: Å« Ãæ°ÝÀº Å©°Ô, ÀÜ¿© Æ®¶ó¿ì¸¶´Â ´«¿¡ ¶çÁö ¾Ê°Ô
        float amp = trauma * trauma * MAX_OFFSET * Mathf.Clamp01(GameBalance.GameFeelMaster);

        // Perlin ³ëÀÌÁî = ¿¬¼ÓÀûÀÎ °î¼± Èçµé¸² (ÇÁ·¹ÀÓ¸¶´Ù ·£´ý Á¡ÇÁÇÏ´Â ¹æ½Äº¸´Ù ¸Ö¹Ì°¡ ´úÇÔ)
        float t = Time.unscaledTime * NOISE_FREQ;
        ShakeOffset = new Vector2(
            (Mathf.PerlinNoise(t, 11.3f) - 0.5f) * 2f,
            (Mathf.PerlinNoise(47.7f, t) - 0.5f) * 2f) * amp;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // È÷Æ®½ºÅé ÄÚ·çÆ¾
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private IEnumerator HitstopRoutine(float duration)
    {
        hitstopActive = true;
        lastHitstopReal = Time.realtimeSinceStartup;
        Time.timeScale = HITSTOP_SCALE;

        yield return new WaitForSecondsRealtime(duration);

        // È÷Æ®½ºÅé µµÁß ´Ù¸¥ ½Ã½ºÅÛ(ÀÏ½ÃÁ¤Áö/QTE)ÀÌ ½Ã°£À» Àâ¾Ò´Ù¸é Á¸ÁßÇÏ°í ¹°·¯³­´Ù
        if (Mathf.Abs(Time.timeScale - HITSTOP_SCALE) < 0.01f)
            Time.timeScale = 1f;

        hitstopActive = false;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Ã³Ä¡ ÆË ÄÚ·çÆ¾: Áß¾Ó ¼¶±¤ 1°³ + »ç¹æÀ¸·Î Æ¢´Â Á¶°¢
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private IEnumerator PopRoutine(Vector3 pos, Color col, float sizeMul)
    {
        activePops++;

        int pieceCount = sizeMul >= 2f ? 10 : 5;
        Transform[] pieces = new Transform[pieceCount];
        Vector2[] vels = new Vector2[pieceCount];
        SpriteRenderer[] srs = new SpriteRenderer[pieceCount];

        // Áß¾Ó ¼¶±¤ (ÇÑ ÇÁ·¹ÀÓÂ¥¸® Èò ¹øÂ½ - ÆË ¼Õ¸ÀÀÇ ÇÙ½É)
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
            t += Time.deltaTime;   // ½ºÄÉÀÏµå ½Ã°£: È÷Æ®½ºÅé Áß¿¡´Â Á¶°¢µµ ¸ØÃç Á¤Áö°¨ °­È­
            float k = t / life;

            // ¼¶±¤Àº ÃÊ¹Ý 0.08ÃÊ¸¸
            if (flash != null)
            {
                if (t > 0.08f) { Destroy(flash); flash = null; }
                else flashSr.color = new Color(1f, 1f, 1f, 1f - t / 0.08f);
            }

            for (int i = 0; i < pieceCount; i++)
            {
                if (pieces[i] == null) continue;
                vels[i] *= 1f - 4.5f * Time.deltaTime;                       // °¨¼Ó
                pieces[i].position += (Vector3)(vels[i] * Time.deltaTime);
                pieces[i].localScale = Vector3.one * 0.15f * sizeMul * (1f - k); // Ãà¼Ò ¼Ò¸ê
                srs[i].color = new Color(col.r, col.g, col.b, 1f - k * k);   // ÈÄ¹Ý ÆäÀÌµå
            }
            yield return null;
        }

        if (flash != null) Destroy(flash);
        for (int i = 0; i < pieceCount; i++)
            if (pieces[i] != null) Destroy(pieces[i].gameObject);

        activePops--;
    }

    /// <summary>ÆË Á¶°¢ 1°³ »ý¼º</summary>
    private GameObject MakePiece(Vector3 pos, Color col, float scale)
    {
        GameObject go = new GameObject("PopPiece");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquare();
        sr.color = col;
        sr.sortingOrder = 58;   // Àç·á Á¶°¢(60)º¸´Ù ¾Æ·¡, Àû À§
        return go;
    }

    /// <summary>4x4 Èò »ç°¢Çü ½ºÇÁ¶óÀÌÆ® (1È¸ »ý¼º Ä³½Ã)</summary>
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
