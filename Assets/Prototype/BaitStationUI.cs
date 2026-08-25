using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [BaitStationUI.cs] v1 (½Å±Ô ÆÄÀÏ) - º¸½º ÆÐÅÏ C´Ü°è
/// ¹Ì³¢ È­´ö - ³ì½¼ ¹ßÅé(Áö¿ª 1 º¸½º) Àü¿ë ½Ã±×´ÏÃ³ ±â¹Í.
///
/// ½ºÅä¸®: ¼±´ëÀÇ ¸Þ¸ð - "¿ÕÀ» ÀâÀ¸·Á¸é ¿ÕÀÇ ¼Õ´ÔºÎÅÍ ´ëÁ¢ÇØ¶ó."
/// ¹«¸®ÀÇ ¿ÕÀº ¹«¸®¸¦ ¸ÔÀÌ´Â °¡ÀåÀÌ´Ù. ¹Ì³¢·Î ¹«¸®°¡ ¸ô·Á°¡¸é ¿Õµµ ÁöÅ°·¯ ¿Â´Ù.
///
/// ±â¹Í: °í±â Àç·á 1°³·Î ¹Ì³¢¸¦ ±Á´Â´Ù(Å¸ÀÌ¹Ö ÆÇÁ¤) -> ÀÚµ¿ ÅõÃ´ ->
///       ·¦ÅÍ ¹«¸® + º¸½º°¡ ¹Ì³¢·Î À¯ÀÎµÈ´Ù (¹°¾î¶â´Â µ¿¾È ±âÂ÷ ¹«ÇÇÇØ)
///       Àß ±¸¿ï¼ö·Ï(PERFECT) À¯ÀÎ ½Ã°£ÀÌ ±æ´Ù: 8ÃÊ / 6ÃÊ / 4ÃÊ
///
/// »ç¿ë¹ý: ¾øÀ½! BossGimmickSystemÀÌ ³ì½¼ ¹ßÅé º¸½ºÀü ½ÃÀÛ ½Ã ÀÚµ¿ »ý¼º.
/// ¼öÄ¡´Â GameBalance 'º¸½º ÆÐÅÏ (C´Ü°è)' ¼½¼Ç¿¡¼­ Á¶Á¤.
/// VS 2017 (C# 7.3) È£È¯.
/// </summary>
public class BaitStationUI : MonoBehaviour
{
    private BossEnemy boss;

    // ¦¡¦¡ UI ¦¡¦¡
    private GameObject canvasGo;
    private Button bakeButton;
    private Text bakeLabel;
    private Text statusText;

    // ¦¡¦¡ ±Á±â Å¸ÀÌ¹Ö ¹Ì´Ï°ÔÀÓ ¦¡¦¡
    private GameObject timingRoot;
    private RectTransform timingCursor;
    private bool timingActive = false;
    private float cursorPos = 0f;
    private float cursorDir = 1f;
    private const float CURSOR_SPEED = 90f;
    private const float TRACK_W = 460f;

    // ¦¡¦¡ »óÅÂ ¦¡¦¡
    private float cooldownUntil = 0f;
    private static Sprite baitSprite;

    public void Setup(BossEnemy targetBoss)
    {
        boss = targetBoss;
        BuildUI();
        UIManager.Instance?.ShowStatChange("[¹Ì³¢ È­´ö] °¡µ¿! °í±â¸¦ ±¸¿ö ±¾ÁÖ¸° ¹«¸®¸¦ ´ëÁ¢ÇÏ¶ó!");
        Debug.Log("[BaitStation] ¹Ì³¢ È­´ö °¡µ¿ (³ì½¼ ¹ßÅé º¸½ºÀü)");
    }

    private void OnDestroy()
    {
        if (canvasGo != null) Destroy(canvasGo);
    }

    private void Update()
    {
        if (boss == null || !boss.IsAlive)
        {
            Destroy(gameObject);
            return;
        }

        // ¹öÆ° »óÅÂ °»½Å
        bool onCooldown = Time.time < cooldownUntil;
        int meat = MaterialInventory.Instance != null ? MaterialInventory.Instance.Get(MaterialType.Meat) : 0;
        bakeButton.interactable = !timingActive && !onCooldown && meat > 0 && !CookingMinigame.IsActive;

        if (onCooldown)
            statusText.text = "È­´ö Àç°¡¿­ Áß... " + Mathf.CeilToInt(cooldownUntil - Time.time) + "ÃÊ";
        else if (meat <= 0)
            statusText.text = "°í±â Àç·á°¡ ¾ø´Ù!";
        else
            statusText.text = "°í±â º¸À¯ " + meat + " - Àß ±¸¿ï¼ö·Ï ¿À·¡ À¯ÀÎÇÑ´Ù";

        if (timingActive)
            UpdateTiming();
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¹Ì³¢ ±Á±â ½ÃÀÛ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void OnBake()
    {
        if (timingActive || Time.time < cooldownUntil) return;
        if (MaterialInventory.Instance == null || MaterialInventory.Instance.Get(MaterialType.Meat) <= 0)
            return;

        MaterialInventory.Instance.Add(MaterialType.Meat, -1);

        timingActive = true;
        cursorPos = 0f;
        cursorDir = 1f;
        timingRoot.SetActive(true);
    }

    private void UpdateTiming()
    {
        cursorPos += cursorDir * CURSOR_SPEED * Time.deltaTime;
        if (cursorPos >= 100f) { cursorPos = 100f; cursorDir = -1f; }
        if (cursorPos <= 0f) { cursorPos = 0f; cursorDir = 1f; }

        timingCursor.anchoredPosition = new Vector2(-TRACK_W / 2f + TRACK_W * (cursorPos / 100f), 0f);

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            ResolveBake();
    }

    private void ResolveBake()
    {
        timingActive = false;
        timingRoot.SetActive(false);
        cooldownUntil = Time.time + GameBalance.BaitCooldown;

        // ÆÇÁ¤: Áß¾Ó(50) °Å¸®
        float d = Mathf.Abs(cursorPos - 50f);
        float duration;
        string grade;
        if (d <= 7f) { duration = GameBalance.BaitDurationPerfect; grade = "PERFECT"; }
        else if (d <= 20f) { duration = GameBalance.BaitDurationGood; grade = "Good"; }
        else { duration = GameBalance.BaitDurationMiss; grade = "Åº ¹Ì³¢"; }

        DeployBait(duration);
        UIManager.Instance?.ShowStatChange("[" + grade + "] ¹Ì³¢ ÅõÃ´! " + duration + "ÃÊ°£ À¯ÀÎ!");
        Debug.Log("[BaitStation] ¹Ì³¢ ±Á±â " + grade + " -> À¯ÀÎ " + duration + "ÃÊ");
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¹Ì³¢ ¼³Ä¡ + ¹«¸®/º¸½º À¯ÀÎ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void DeployBait(float duration)
    {
        // ±âÂ÷¿¡¼­ ÀÏÁ¤ °Å¸® ¶³¾îÁø ·£´ý À§Ä¡
        GameObject train = GameObject.FindGameObjectWithTag("Train");
        Vector3 center = train != null ? train.transform.position : Vector3.zero;
        float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 pos = center + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * GameBalance.BaitDistance;

        // ¹Ì³¢ ¿ÀºêÁ§Æ® (ÄÚµå »ý¼º ½ºÇÁ¶óÀÌÆ®)
        GameObject bait = new GameObject("Bait");
        bait.transform.position = pos;
        SpriteRenderer sr = bait.AddComponent<SpriteRenderer>();
        sr.sprite = GetBaitSprite();
        sr.color = new Color(0.85f, 0.45f, 0.3f);
        sr.sortingOrder = 55;
        bait.transform.localScale = Vector3.one * 0.9f;
        bait.AddComponent<BaitPulse>();

        // À¯ÀÎ: ·¦ÅÍ ¹«¸®(Pack) + º¸½º
        int lured = 0;
        Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].IsAlive) continue;
            if (all[i] == boss || all[i].behavior == Enemy.BehaviorPattern.Pack)
            {
                all[i].Taunt(bait.transform, duration);
                lured++;
            }
        }

        Destroy(bait, duration);
        Debug.Log("[BaitStation] ¹Ì³¢ ¼³Ä¡ - " + lured + "¸¶¸® À¯ÀÎ (" + duration + "ÃÊ)");
    }

    /// <summary>¹Ì³¢°¡ Ä§ Èê¸®°Ô ¸Æµ¿ÇÏ´Â ¿¬Ãâ¿ë º¸Á¶ ÄÄÆ÷³ÍÆ®</summary>
    private class BaitPulse : MonoBehaviour
    {
        private float t = 0f;
        private void Update()
        {
            t += Time.deltaTime;
            float s = 0.9f + Mathf.Sin(t * 6f) * 0.12f;
            transform.localScale = Vector3.one * s;
        }
    }

    private static Sprite GetBaitSprite()
    {
        if (baitSprite != null) return baitSprite;

        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), c) <= 7f ? Color.white : Color.clear);
        tex.Apply();
        baitSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        return baitSprite;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // UI »ý¼º (ÁÂÃø Áß´Ü - ÇØµ¿Æ÷¿Í °°Àº ÀÚ¸®, ¼­·Î ´Ù¸¥ º¸½ºÀüÀÌ¶ó ¾È °ãÄ§)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void BuildUI()
    {
        canvasGo = new GameObject("BaitStationCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 485;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform panel = KitchenEventManager.MakeBox(canvasGo.transform, "BaitPanel",
            new Color(0.1f, 0.08f, 0.06f, 0.92f));
        panel.anchorMin = new Vector2(0f, 0.5f);
        panel.anchorMax = new Vector2(0f, 0.5f);
        panel.pivot = new Vector2(0f, 0.5f);
        panel.anchoredPosition = new Vector2(14f, 60f);
        panel.sizeDelta = new Vector2(280f, 130f);

        Text title = KitchenEventManager.MakeText(panel, "Title", "¹Ì³¢ È­´ö", 21,
            new Color(1f, 0.7f, 0.35f));
        RectTransform tRt = title.rectTransform;
        tRt.anchorMin = new Vector2(0f, 1f); tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -8f);
        tRt.sizeDelta = new Vector2(0f, 26f);

        statusText = KitchenEventManager.MakeText(panel, "Status", "", 15,
            new Color(0.85f, 0.8f, 0.7f));
        RectTransform sRt = statusText.rectTransform;
        sRt.anchorMin = new Vector2(0f, 1f); sRt.anchorMax = new Vector2(1f, 1f);
        sRt.pivot = new Vector2(0.5f, 1f);
        sRt.anchoredPosition = new Vector2(0f, -36f);
        sRt.sizeDelta = new Vector2(0f, 22f);

        bakeButton = KitchenEventManager.MakeButton(panel, "¹Ì³¢ ±Á±â (°í±â 1)",
            new Color(0.5f, 0.3f, 0.12f), new Vector2(0f, -20f), new Vector2(250f, 38f));
        bakeLabel = bakeButton.GetComponentInChildren<Text>();
        bakeButton.onClick.AddListener(OnBake);

        // ¦¡¦¡ ±Á±â Å¸ÀÌ¹Ö ¿À¹ö·¹ÀÌ (È­¸é Áß¾Ó) ¦¡¦¡
        RectTransform tRoot = KitchenEventManager.MakeBox(canvasGo.transform, "TimingRoot",
            new Color(0.08f, 0.05f, 0.04f, 0.95f));
        tRoot.anchorMin = new Vector2(0.5f, 0.5f);
        tRoot.anchorMax = new Vector2(0.5f, 0.5f);
        tRoot.pivot = new Vector2(0.5f, 0.5f);
        tRoot.anchoredPosition = new Vector2(0f, 140f);
        tRoot.sizeDelta = new Vector2(560f, 110f);
        timingRoot = tRoot.gameObject;

        Text tTitle = KitchenEventManager.MakeText(tRoot, "TTitle",
            "³ë¸©ÇÑ Á¤Áß¾Ó¿¡¼­ [Space] - ¹Ì³¢ ±Á±â!", 22, new Color(1f, 0.8f, 0.4f));
        RectTransform ttRt = tTitle.rectTransform;
        ttRt.anchorMin = new Vector2(0f, 1f); ttRt.anchorMax = new Vector2(1f, 1f);
        ttRt.pivot = new Vector2(0.5f, 1f);
        ttRt.anchoredPosition = new Vector2(0f, -8f);
        ttRt.sizeDelta = new Vector2(0f, 28f);

        RectTransform track = KitchenEventManager.MakeBox(tRoot, "Track", new Color(0f, 0f, 0f, 0.6f));
        track.anchorMin = new Vector2(0.5f, 0f);
        track.anchorMax = new Vector2(0.5f, 0f);
        track.pivot = new Vector2(0.5f, 0f);
        track.anchoredPosition = new Vector2(0f, 22f);
        track.sizeDelta = new Vector2(TRACK_W, 26f);

        RectTransform goodZone = KitchenEventManager.MakeBox(track, "Good", new Color(0.35f, 0.6f, 0.3f, 0.8f));
        goodZone.anchorMin = new Vector2(0.5f, 0f); goodZone.anchorMax = new Vector2(0.5f, 1f);
        goodZone.pivot = new Vector2(0.5f, 0.5f);
        goodZone.anchoredPosition = Vector2.zero;
        goodZone.sizeDelta = new Vector2(TRACK_W * 0.4f, 0f);

        RectTransform perfectZone = KitchenEventManager.MakeBox(track, "Perfect", new Color(1f, 0.85f, 0.3f, 0.9f));
        perfectZone.anchorMin = new Vector2(0.5f, 0f); perfectZone.anchorMax = new Vector2(0.5f, 1f);
        perfectZone.pivot = new Vector2(0.5f, 0.5f);
        perfectZone.anchoredPosition = Vector2.zero;
        perfectZone.sizeDelta = new Vector2(TRACK_W * 0.14f, 0f);

        timingCursor = KitchenEventManager.MakeBox(track, "Cursor", Color.white);
        timingCursor.anchorMin = new Vector2(0.5f, 0f); timingCursor.anchorMax = new Vector2(0.5f, 1f);
        timingCursor.pivot = new Vector2(0.5f, 0.5f);
        timingCursor.sizeDelta = new Vector2(6f, 8f);

        timingRoot.SetActive(false);
    }
}
