using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [InfusingMinigame.cs] v1 (½Å±Ô ÆÄÀÏ) - P1: T2 ÀÎÇ»Â¡ ¹Ì´Ï°ÔÀÓ (°¨»ç 1-A Ã³¹æ 2)
///
/// ¹è°æ: ·±ÀÇ °¡Àå Å« ÆÄ¿ö ½ºÆÄÀÌÅ©(T2 Àü¼³ Æ÷Å¾ Åº»ý)°¡ Å¬¸¯ µÎ ¹øÀÌ¾ú´Ù.
/// "°¡Àå Å« ¼ø°£¿¡ ¼ÕÀÌ °¡Àå ÇÑ°¡ÇÑ °ÍÀº Á¤Ã¼¼º À§¹Ý" - ÀÌÁ¦ T2 ÁøÈ­´Â Á÷Á¢ Á¶¸®ÇÑ´Ù.
///
/// Èå¸§: ´Ù¸¥ T1 Æ÷Å¾ 2°³ ÇÕÃ¼ ½Ãµµ -> ÀÌ ¹Ì´Ï°ÔÀÓ ÀÚµ¿ ½ÃÀÛ (TurretSlotManager°¡ È£Ãâ)
///  - 1¶ó¿îµå [Á¤¼ö ÃßÃâ] : ±Á±â ¹®¹ý - Å¸ÀÌ¹Ö ¹Ù 1È¸ (Space/Å¬¸¯)
///  - 2¶ó¿îµå [À¶ÇÕ ¾ÈÁ¤È­]: ²úÀÌ±â ¹®¹ý - ¿òÁ÷ÀÌ´Â Á¸¿¡ °ÔÀÌÁö À¯Áö (Space È¦µå)
///  - ÆÇÁ¤ ÇÕ°è(¶ó¿îµå´ç PERFECT 2 / Good 1)°¡ ±âÁØ ÀÌ»óÀÌ¸é T2°¡ +1·¹º§·Î Åº»ý
///  - ½ÇÆÐÇØµµ ÁøÈ­´Â ¼º°ø (º¸³Ê½º¸¸ ¾øÀ½ - ¹úÁ¡ ¾øÀ½, µµÆÄ¹Î º¸Á¸)
///  - ESC = ÀÎÇ»Â¡ Áß´Ü (Æ÷Å¾ 2°³ ±×´ë·Î, ¾Æ¹« ÀÏ ¾øÀ½)
///
/// »ç¿ë¹ý: ¾øÀ½! ÆÄÀÏ¸¸ ³ÖÀ¸¸é TurretSlotManager°¡ ¾Ë¾Æ¼­ È£ÃâÇÑ´Ù.
/// ¼öÄ¡´Â GameBalance 'ÀÎÇ»Â¡' ¼½¼Ç. VS 2017 (C# 7.3) È£È¯.
/// </summary>
public class InfusingMinigame : MonoBehaviour
{
    /// <summary>ÀÎÇ»Â¡ ÁøÇà Áß ¿©ºÎ (ÇÕÃ¼/Á¶¸®/ÀÏ½ÃÁ¤Áö Ãæµ¹ ¹æÁö¿ë)</summary>
    public static bool IsActive = false;

    // ¦¡¦¡ ´ë»ó ¦¡¦¡
    private int idxA, idxB;
    private RecipeData fusion;
    private TurretSlotManager manager;

    // ¦¡¦¡ ÁøÇà »óÅÂ ¦¡¦¡
    private int phase = 0;            // 0=±Á±â, 1=²úÀÌ±â, 2=Á¾·á ¿¬Ãâ
    private int score = 0;            // ÆÇÁ¤ ÇÕ°è (ÃÖ´ë 4)
    private float endTimer = 0f;

    // ¦¡¦¡ 1¶ó¿îµå: Å¸ÀÌ¹Ö ¹Ù ¦¡¦¡
    private float bar = 0f;
    private float dir = 1f;

    // ¦¡¦¡ 2¶ó¿îµå: ¾Ð·Â À¯Áö ¦¡¦¡
    private float gauge = 50f;
    private float zonePhase = 0f;
    private float zoneCenter = 50f;
    private float holdTimer = 0f;
    private float inZone = 0f;

    // ¦¡¦¡ UI ¦¡¦¡
    private GameObject canvasGo;
    private Text titleText;
    private Text infoText;
    private Text judgeText;
    private float judgeTimer = 0f;
    private RectTransform track;
    private RectTransform zoneRect;
    private RectTransform cursorRect;
    private RectTransform fillRect;

    private const float TRACK_W = 380f;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ½ÃÀÛ (TurretSlotManager.TryMergeSlotsÀÇ T2 ºÐ±â°¡ È£Ãâ)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public static void Begin(int slotA, int slotB, RecipeData fusionRecipe, TurretSlotManager mgr)
    {
        if (IsActive) return;
        GameObject go = new GameObject("InfusingMinigame");
        InfusingMinigame m = go.AddComponent<InfusingMinigame>();
        m.idxA = slotA;
        m.idxB = slotB;
        m.fusion = fusionRecipe;
        m.manager = mgr;
        m.Setup();
    }

    private void Setup()
    {
        IsActive = true;
        phase = 0;
        score = 0;
        bar = 0f; dir = 1f;

        BuildUI();
        titleText.text = "ÀÎÇ»Â¡  -  " + fusion.displayName + " [T2]";
        SetPhaseInfo();

        Debug.Log("[Infusing] ÀÎÇ»Â¡ ½ÃÀÛ -> " + fusion.displayName);
    }

    private void OnDestroy()
    {
        IsActive = false;
        if (canvasGo != null) Destroy(canvasGo);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¸Å ÇÁ·¹ÀÓ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void Update()
    {
        // °ÔÀÓÀÌ ³¡³µÀ¸¸é Á¶¿ëÈ÷ Áß´Ü
        GameManager gm = GameManager.Instance;
        if (gm != null && (gm.currentState == GameManager.GameState.GameOver
                        || gm.currentState == GameManager.GameState.Victory))
        {
            Destroy(gameObject);
            return;
        }

        // ESC = Áß´Ü (Æ÷Å¾Àº ±×´ë·Î - ¾Æ¹« ÀÏµµ ÀÏ¾î³ªÁö ¾ÊÀº °ÍÀ¸·Î)
        if (phase < 2 && Input.GetKeyDown(KeyCode.Escape))
        {
            UIManager.Instance?.ShowStatChange("ÀÎÇ»Â¡ Áß´Ü - µÎ ¿ä¸®´Â ±×´ë·Î ³²¾Ò´Ù");
            Destroy(gameObject);
            return;
        }

        // ÆÇÁ¤ ÆË¾÷ ÆäÀÌµå
        if (judgeTimer > 0f)
        {
            judgeTimer -= Time.deltaTime;
            if (judgeText != null)
            {
                Color c = judgeText.color;
                c.a = Mathf.Clamp01(judgeTimer / 0.35f);
                judgeText.color = c;
            }
        }

        if (phase == 0) UpdateGrillRound();
        else if (phase == 1) UpdateBoilRound();
        else UpdateFinish();
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // 1¶ó¿îµå: Á¤¼ö ÃßÃâ (±Á±â ¹®¹ý)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void UpdateGrillRound()
    {
        bar += dir * GameBalance.InfuseGrillSpeed * Time.deltaTime;
        if (bar >= 100f) { bar = 100f; dir = -1f; }
        if (bar <= 0f) { bar = 0f; dir = 1f; }

        cursorRect.anchoredPosition = new Vector2(-TRACK_W / 2f + TRACK_W * (bar / 100f), 0f);

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            float d = Mathf.Abs(bar - 50f);
            if (d <= 6f) { score += 2; ShowJudge("PERFECT!", UIFactory.GOLD); SoundManager.Play("sfx_judge_perfect"); }
            else if (d <= 20f) { score += 1; ShowJudge("Good", new Color(0.6f, 0.85f, 0.54f)); SoundManager.Play("sfx_judge_good"); }
            else { ShowJudge("Á¤¼ö°¡ Èð¾îÁ³´Ù...", new Color(1f, 0.6f, 0.48f)); SoundManager.Play("sfx_judge_bad"); }

            // 2¶ó¿îµå ÁØºñ
            phase = 1;
            gauge = 50f;
            zonePhase = Random.Range(0f, 6.28f);
            holdTimer = GameBalance.InfuseBoilTime;
            inZone = 0f;
            cursorRect.gameObject.SetActive(false);
            fillRect.gameObject.SetActive(true);
            zoneRect.gameObject.SetActive(true);
            SetPhaseInfo();
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // 2¶ó¿îµå: À¶ÇÕ ¾ÈÁ¤È­ (²úÀÌ±â ¹®¹ý - °¡·Î °ÔÀÌÁö)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void UpdateBoilRound()
    {
        bool hold = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);
        gauge += (hold ? 55f : -38f) * Time.deltaTime;
        gauge = Mathf.Clamp(gauge, 0f, 100f);

        zonePhase += Time.deltaTime * 1.1f;
        zoneCenter = 50f + Mathf.Sin(zonePhase) * 26f;

        holdTimer -= Time.deltaTime;
        if (Mathf.Abs(gauge - zoneCenter) <= 12f)
            inZone += Time.deltaTime;

        // ·»´õ: °ÔÀÌÁö Ã¤¿ò + Á¸ À§Ä¡
        fillRect.sizeDelta = new Vector2(TRACK_W * (gauge / 100f), 26f);
        zoneRect.anchoredPosition = new Vector2(-TRACK_W / 2f + TRACK_W * (zoneCenter / 100f), 0f);

        if (holdTimer <= 0f)
        {
            float ratio = inZone / GameBalance.InfuseBoilTime;
            if (ratio >= 0.75f) { score += 2; ShowJudge("PERFECT!", UIFactory.GOLD); SoundManager.Play("sfx_judge_perfect"); }
            else if (ratio >= 0.45f) { score += 1; ShowJudge("Good", new Color(0.6f, 0.85f, 0.54f)); SoundManager.Play("sfx_judge_good"); }
            else { ShowJudge("À¶ÇÕÀÌ ¿äµ¿Ä£´Ù...", new Color(1f, 0.6f, 0.48f)); SoundManager.Play("sfx_judge_bad"); }

            phase = 2;
            endTimer = 0.8f;
            SetPhaseInfo();
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Á¾·á: Àá±ñ °á°ú º¸¿©ÁÖ°í ½ÇÁ¦ ÁøÈ­ ¼öÇà
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void UpdateFinish()
    {
        endTimer -= Time.deltaTime;
        if (endTimer > 0f) return;

        int bonus = score >= GameBalance.InfuseBonusScoreNeed ? GameBalance.InfuseBonusLevel : 0;

        if (manager != null)
            manager.CompleteFusion(idxA, idxB, fusion, bonus, score >= 4);

        Destroy(gameObject);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // UI
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void SetPhaseInfo()
    {
        if (phase == 0)
            infoText.text = "1/2 [Á¤¼ö ÃßÃâ]  Á¤Áß¾Ó¿¡¼­ [Space]  (ÇÕ°è "
                + GameBalance.InfuseBonusScoreNeed + "Á¡ ÀÌ»ó = Lv+" + GameBalance.InfuseBonusLevel + " Åº»ý)";
        else if (phase == 1)
            infoText.text = "2/2 [À¶ÇÕ ¾ÈÁ¤È­]  [Space] È¦µå·Î °ÔÀÌÁö¸¦ Á¸¿¡ À¯Áö  (ÇöÀç " + score + "Á¡)";
        else
            infoText.text = "À¶ÇÕ ÆÇÁ¤ ÇÕ°è " + score + "Á¡...";
    }

    private void ShowJudge(string msg, Color col)
    {
        judgeText.text = msg;
        judgeText.color = col;
        judgeTimer = 0.9f;
    }

    private void BuildUI()
    {
        canvasGo = new GameObject("InfusingCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31;   // Á¶¸® ¹Ì´Ï°ÔÀÓ(30) ¹Ù·Î À§ (µ¿½Ã È°¼ºÀº ÄÚµå·Î Â÷´ÜµÊ)
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // ÁÂÇÏ´Ü ÆÐ³Î (Á¶¸® ¹Ì´Ï°ÔÀÓ°ú °°Àº ÀÚ¸® - ÁÖ¹æ ÀÛ¾÷ °ø°£)
        RectTransform panel = KitchenEventManager.MakeBox(canvasGo.transform, "Panel",
            new Color(0.10f, 0.06f, 0.09f, 0.95f));
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(0f, 0f);
        panel.pivot = new Vector2(0f, 0f);
        panel.anchoredPosition = new Vector2(12f, 192f);
        panel.sizeDelta = new Vector2(440f, 210f);

        // T2 ÇÎÅ© Å×µÎ¸® (Àü¼³ À¶ÇÕÀÇ »ö)
        RectTransform border = KitchenEventManager.MakeBox(panel, "Border", UIFactory.T2PINK);
        border.anchorMin = Vector2.zero; border.anchorMax = new Vector2(1f, 0f);
        border.pivot = new Vector2(0.5f, 0f);
        border.anchoredPosition = Vector2.zero;
        border.sizeDelta = new Vector2(0f, 3f);

        titleText = KitchenEventManager.MakeText(panel, "Title", "", 21, UIFactory.T2PINK);
        RectTransform tRt = titleText.rectTransform;
        tRt.anchorMin = new Vector2(0f, 1f); tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -10f);
        tRt.sizeDelta = new Vector2(-16f, 28f);

        infoText = KitchenEventManager.MakeText(panel, "Info", "", 15,
            new Color(0.92f, 0.88f, 0.82f));
        RectTransform iRt = infoText.rectTransform;
        iRt.anchorMin = new Vector2(0f, 0f); iRt.anchorMax = new Vector2(1f, 0f);
        iRt.pivot = new Vector2(0.5f, 0f);
        iRt.anchoredPosition = new Vector2(0f, 14f);
        iRt.sizeDelta = new Vector2(-16f, 44f);

        judgeText = KitchenEventManager.MakeText(panel, "Judge", "", 26, UIFactory.GOLD);
        RectTransform jRt = judgeText.rectTransform;
        jRt.anchorMin = new Vector2(0.5f, 0.5f); jRt.anchorMax = new Vector2(0.5f, 0.5f);
        jRt.pivot = new Vector2(0.5f, 0.5f);
        jRt.anchoredPosition = new Vector2(0f, 48f);
        jRt.sizeDelta = new Vector2(400f, 34f);

        // ÆÇÁ¤ Æ®·¢
        track = KitchenEventManager.MakeBox(panel, "Track", new Color(0f, 0f, 0f, 0.6f));
        track.anchorMin = new Vector2(0.5f, 0.5f); track.anchorMax = new Vector2(0.5f, 0.5f);
        track.pivot = new Vector2(0.5f, 0.5f);
        track.anchoredPosition = new Vector2(0f, 2f);
        track.sizeDelta = new Vector2(TRACK_W, 30f);

        // 1¶ó¿îµå¿ë: Áß¾Ó ÆÇÁ¤ Á¸ (°íÁ¤) - Good Æø
        RectTransform goodZone = KitchenEventManager.MakeBox(track, "Good", new Color(0.35f, 0.6f, 0.3f, 0.8f));
        goodZone.anchorMin = new Vector2(0.5f, 0f); goodZone.anchorMax = new Vector2(0.5f, 1f);
        goodZone.pivot = new Vector2(0.5f, 0.5f);
        goodZone.anchoredPosition = Vector2.zero;
        goodZone.sizeDelta = new Vector2(TRACK_W * 0.4f, 0f);

        RectTransform perfectZone = KitchenEventManager.MakeBox(track, "Perfect", new Color(1f, 0.42f, 0.85f, 0.9f));
        perfectZone.anchorMin = new Vector2(0.5f, 0f); perfectZone.anchorMax = new Vector2(0.5f, 1f);
        perfectZone.pivot = new Vector2(0.5f, 0.5f);
        perfectZone.anchoredPosition = Vector2.zero;
        perfectZone.sizeDelta = new Vector2(TRACK_W * 0.12f, 0f);

        cursorRect = KitchenEventManager.MakeBox(track, "Cursor", Color.white);
        cursorRect.anchorMin = new Vector2(0.5f, 0f); cursorRect.anchorMax = new Vector2(0.5f, 1f);
        cursorRect.pivot = new Vector2(0.5f, 0.5f);
        cursorRect.sizeDelta = new Vector2(6f, 8f);

        // 2¶ó¿îµå¿ë: °ÔÀÌÁö Ã¤¿ò(¿ÞÂÊºÎÅÍ) + ¿òÁ÷ÀÌ´Â Á¸ (½ÃÀÛ ½Ã ¼û±è)
        fillRect = KitchenEventManager.MakeBox(track, "Fill", new Color(1f, 0.42f, 0.85f, 0.45f));
        fillRect.anchorMin = new Vector2(0f, 0.5f); fillRect.anchorMax = new Vector2(0f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(0f, 26f);
        fillRect.gameObject.SetActive(false);

        zoneRect = KitchenEventManager.MakeBox(track, "Zone", new Color(1f, 1f, 1f, 0.85f));
        zoneRect.anchorMin = new Vector2(0.5f, 0f); zoneRect.anchorMax = new Vector2(0.5f, 1f);
        zoneRect.pivot = new Vector2(0.5f, 0.5f);
        zoneRect.sizeDelta = new Vector2(TRACK_W * 0.24f, 4f);
        zoneRect.gameObject.SetActive(false);
    }
}
