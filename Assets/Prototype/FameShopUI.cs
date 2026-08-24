using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// [FameShopUI.cs] v1 (½Å±Ô ÆÄÀÏ)
/// ¸í¼º »óÁ¡ - ·± »çÀÌ(·Îºñ/°ÔÀÓ¿À¹ö)¿¡ ¸í¼ºÀ» ¼Ò¸ðÇØ ¿µ±¸ ¾÷±×·¹ÀÌµå¸¦ »ç´Â UI.
/// ÇÏµ¥½ºÀÇ '¾îµÒÀÇ °Å¿ï' Æ÷Áö¼Ç: Á×¾îµµ ¸í¼ºÀº ³²°í, ±×°É·Î ´ÙÀ½ ·±À» °­ÇÏ°Ô ¸¸µç´Ù.
///
/// »ç¿ë¹ý:
///  1) ÀÌ ÆÄÀÏÀ» Assets/Prototype Æú´õ¿¡ ³Ö´Â´Ù
///  2) ÇÏÀÌ¾î¶óÅ°ÀÇ ¾Æ¹« ¿ÀºêÁ§Æ®(¿¹: UIManager°¡ ºÙÀº ¿ÀºêÁ§Æ®)¿¡ AddComponent
///  3) ¾À ¹èÄ¡ ÇÊ¿ä ¾øÀ½ - UI´Â ÀüºÎ ÄÚµå·Î »ý¼ºµÈ´Ù
///
/// µ¿ÀÛ:
///  - ·Îºñ / °ÔÀÓ¿À¹ö »óÅÂ¿¡¼­ ÀÚµ¿À¸·Î Ç¥½Ã, ÀüÅõ ½ÃÀÛÇÏ¸é ÀÚµ¿À¸·Î ¼û±è
///  - M Å°·Î Á¢±â/ÆîÄ¡±â (·Îºñ, °ÔÀÓ¿À¹ö¿¡¼­¸¸)
///  - ¾÷±×·¹ÀÌµå È¿°ú´Â ÀÌ¹Ì GameManager/TrainManager/CookingMinigame¿¡ ¿¬°áµÇ¾î ÀÖ¾î
///    ±¸¸Å Áï½Ã(´ÙÀ½ ·±ºÎÅÍ) Àû¿ëµÈ´Ù
/// VS 2017 (C# 7.3) È£È¯.
/// </summary>
public class FameShopUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // »óÇ° Á¤ÀÇ
    // °¡°ÝÀº baseCost * (ÇöÀç·¹º§ + 1) - ·¹º§ÀÌ ¿À¸¦¼ö·Ï ºñ½ÎÁø´Ù
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private class ShopItem
    {
        public string id;        // MetaProgress ÀúÀå Å°
        public string itemName;  // Ç¥½Ã ÀÌ¸§
        public string desc;      // È¿°ú ¼³¸í
        public int baseCost;     // 1·¹º§ °¡°Ý
        public int maxLevel;     // ÃÖ´ë ·¹º§

        public ShopItem(string id, string itemName, string desc, int baseCost, int maxLevel)
        {
            this.id = id; this.itemName = itemName; this.desc = desc;
            this.baseCost = baseCost; this.maxLevel = maxLevel;
        }

        public int CostAt(int level) { return baseCost * (level + 1); }
    }

    private ShopItem[] items;

    // ¦¡¦¡ UI ÂüÁ¶ ¦¡¦¡
    private GameObject canvasGo;
    private GameObject root;
    private Text fameText;
    private Text[] levelTexts;
    private Text[] buyLabels;
    private Button[] buyButtons;
    private GameObject restartButtonGo;   // v1.2: [´Ù½Ã ±Á´Â´Ù] - °ÔÀÓ¿À¹ö/½Â¸® ½Ã¿¡¸¸ Ç¥½Ã

    // Ç¥½Ã »óÅÂ ÃßÀû
    private bool userCollapsed = false;   // M Å°·Î Á¢¾ú´Â°¡

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÃÊ±âÈ­
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void Start()
    {
        items = new ShopItem[]
        {
            new ShopItem("gold",  "µÎµÏÇÑ Àü´ë",   "½ÃÀÛ °ñµå +100",             80,  3),
            new ShopItem("hp",    "°­È­ º¸ÀÏ·¯",   "±âÂ÷ ÃÖ´ë HP +50",           100, 3),
            new ShopItem("food",  "¿©ºÐÀÇ µµ½Ã¶ô", "½ÃÀÛ ¿ä¸® +1 (Ã¹ Æ÷Å¾ °¡¼Ó)", 120, 2),
            new ShopItem("mat",   "Àç·á °¡¹æ",     "½ÃÀÛ ½Ã ·£´ý Àç·á +2",        100, 2),
            new ShopItem("judge", "¼ÎÇÁÀÇ °¨°¢",   "Á¶¸® ÆÇÁ¤ Á¸ +4% (¿µ±¸)",     150, 3),
        };

        BuildUI();
        root.SetActive(false);
        IsOpen = false;
    }

    private void OnDestroy()
    {
        if (canvasGo != null) Destroy(canvasGo);
        IsOpen = false;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Ç¥½Ã Á¶°Ç: ·Îºñ ¶Ç´Â °ÔÀÓ¿À¹ö »óÅÂ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void Update()
    {
        bool allowed = false;
        if (GameManager.Instance != null)
        {
            GameManager.GameState s = GameManager.Instance.currentState;
            allowed = (s == GameManager.GameState.Lobby
                || s == GameManager.GameState.GameOver
                || s == GameManager.GameState.Victory);
        }

        // M Å°·Î Á¢±â/ÆîÄ¡±â
        if (allowed && Input.GetKeyDown(KeyCode.M))
            userCollapsed = !userCollapsed;

        bool shouldShow = allowed && !userCollapsed;
        if (root.activeSelf != shouldShow)
        {
            root.SetActive(shouldShow);
            IsOpen = shouldShow;
            if (shouldShow) Refresh();   // ¿­¸± ¶§¸¶´Ù ¸í¼º/°¡°Ý °»½Å
        }

        // v1.2 (°¨»ç 3-E): [´Ù½Ã ±Á´Â´Ù] ¹öÆ°Àº ·±ÀÌ ³¡³µÀ» ¶§¸¸ (·Îºñ¿¡¼­´Â ¼û±è)
        if (restartButtonGo != null && GameManager.Instance != null)
        {
            bool runEnded = GameManager.Instance.currentState == GameManager.GameState.GameOver
                || GameManager.Instance.currentState == GameManager.GameState.Victory;
            bool showRestart = runEnded && !userCollapsed;
            if (restartButtonGo.activeSelf != showRestart)
                restartButtonGo.SetActive(showRestart);
        }
    }

    /// <summary>
    /// v1.2 (°¨»ç 3-E): Áï½Ã ÀçÃâ¹ß - Á×À½ÀÌ °¡Àå ¶ß°Å¿î ÀçµµÀü ¿å±¸ÀÇ ¼ø°£.
    /// PauseMenuÀÇ ·± Æ÷±â¿Í °°Àº ¹æ½Ä: GameManager ÆÄ±« ÈÄ ¾À ¸®·Îµå (DontDestroyOnLoad ÀÜÀç ¹æÁö)
    /// </summary>
    private void RestartRun()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
            Destroy(GameManager.Instance.gameObject);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // UI »ý¼º (ÀüºÎ ÄÚµå)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void BuildUI()
    {
        // Àü¿ë Äµ¹ö½º (Workshop 550 °ú Augment 600 »çÀÌ)
        canvasGo = new GameObject("FameShopCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 560;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // ¹ÝÅõ¸í ¹è°æ ÆÐ³Î (Áß¾Ó)
        RectTransform panel = KitchenEventManager.MakeBox(canvasGo.transform, "FameShopPanel",
            new Color(0.08f, 0.06f, 0.05f, 0.94f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = new Vector2(0f, 20f);
        panel.sizeDelta = new Vector2(860f, 600f);
        root = panel.gameObject;

        // Á¦¸ñ
        Text title = KitchenEventManager.MakeText(panel, "Title",
            "¸í¼º »óÁ¡ - È²¾ßÀÇ Àü¼³", 32, new Color(1f, 0.78f, 0.32f));
        SetTopStretch(title.rectTransform, -14f, 40f);

        // º¸À¯ ¸í¼º
        fameText = KitchenEventManager.MakeText(panel, "Fame", "", 24,
            new Color(0.95f, 0.9f, 0.6f));
        SetTopStretch(fameText.rectTransform, -58f, 30f);

        // »óÇ° ¸ñ·Ï
        int count = items.Length;
        levelTexts = new Text[count];
        buyLabels = new Text[count];
        buyButtons = new Button[count];

        float rowY = 130f;
        for (int i = 0; i < count; i++)
        {
            // Å¬·ÎÀú Ä¸Ã³¿ë Áö¿ª º¯¼ö (for º¯¼ö Á÷Á¢ Ä¸Ã³ ±ÝÁö)
            int index = i;

            RectTransform row = KitchenEventManager.MakeBox(panel, "Row_" + items[i].id,
                new Color(0.16f, 0.13f, 0.1f, 0.9f));
            row.anchorMin = new Vector2(0.5f, 0.5f);
            row.anchorMax = new Vector2(0.5f, 0.5f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.anchoredPosition = new Vector2(0f, rowY);
            row.sizeDelta = new Vector2(800f, 72f);
            rowY -= 82f;

            // ÀÌ¸§ (ÁÂÃø »ó´Ü)
            Text nameText = KitchenEventManager.MakeText(row, "Name", items[i].itemName, 23,
                new Color(1f, 0.92f, 0.8f));
            nameText.alignment = TextAnchor.MiddleLeft;
            RectTransform nRt = nameText.rectTransform;
            nRt.anchorMin = new Vector2(0f, 0.5f);
            nRt.anchorMax = new Vector2(0f, 0.5f);
            nRt.pivot = new Vector2(0f, 0.5f);
            nRt.anchoredPosition = new Vector2(18f, 14f);
            nRt.sizeDelta = new Vector2(300f, 30f);

            // ¼³¸í (ÁÂÃø ÇÏ´Ü)
            Text descText = KitchenEventManager.MakeText(row, "Desc", items[i].desc, 18,
                new Color(0.75f, 0.72f, 0.65f));
            descText.alignment = TextAnchor.MiddleLeft;
            RectTransform dRt = descText.rectTransform;
            dRt.anchorMin = new Vector2(0f, 0.5f);
            dRt.anchorMax = new Vector2(0f, 0.5f);
            dRt.pivot = new Vector2(0f, 0.5f);
            dRt.anchoredPosition = new Vector2(18f, -14f);
            dRt.sizeDelta = new Vector2(420f, 26f);

            // ·¹º§ Ç¥½Ã (Áß¾Ó ¿ìÃø)
            levelTexts[i] = KitchenEventManager.MakeText(row, "Level", "", 21,
                new Color(0.6f, 0.85f, 0.95f));
            RectTransform lRt = levelTexts[i].rectTransform;
            lRt.anchorMin = new Vector2(1f, 0.5f);
            lRt.anchorMax = new Vector2(1f, 0.5f);
            lRt.pivot = new Vector2(1f, 0.5f);
            lRt.anchoredPosition = new Vector2(-190f, 0f);
            lRt.sizeDelta = new Vector2(120f, 30f);

            // ±¸¸Å ¹öÆ° (¿ìÃø)
            buyButtons[i] = KitchenEventManager.MakeButton(row, "±¸¸Å",
                new Color(0.55f, 0.35f, 0.12f), new Vector2(310f, 0f), new Vector2(150f, 50f));
            buyLabels[i] = buyButtons[i].GetComponentInChildren<Text>();
            buyButtons[i].onClick.AddListener(delegate { OnBuy(index); });
        }

        // v1.2 (°¨»ç 3-E): [´Ù½Ã ±Á´Â´Ù] ¹öÆ° - Äµ¹ö½º Á÷¼Ó (ÆÐ³Î ¾Æ·¡)
        Button restartBtn = KitchenEventManager.MakeButton(canvasGo.transform,
            "´Ù½Ã ±Á´Â´Ù (Áï½Ã ÀçÃâ¹ß)",
            new Color(0.62f, 0.25f, 0.12f), Vector2.zero, new Vector2(340f, 58f));
        RectTransform rRt = restartBtn.GetComponent<RectTransform>();
        rRt.anchorMin = new Vector2(0.5f, 0.5f);
        rRt.anchorMax = new Vector2(0.5f, 0.5f);
        rRt.pivot = new Vector2(0.5f, 0.5f);
        rRt.anchoredPosition = new Vector2(0f, -330f);   // »óÁ¡ ÆÐ³Î ¹Ù·Î ¾Æ·¡
        restartBtn.onClick.AddListener(RestartRun);
        restartButtonGo = restartBtn.gameObject;
        restartButtonGo.SetActive(false);

        // ÇÏ´Ü ¾È³»
        Text hint = KitchenEventManager.MakeText(panel, "Hint",
            "¸í¼ºÀº ¿þÀÌºê¸¦ Å¬¸®¾îÇÒ ¶§¸¶´Ù ½×ÀÌ°í, Á×¾îµµ ÀÒÁö ¾Ê´Â´Ù.  [M] Á¢±â/ÆîÄ¡±â", 17,
            new Color(0.6f, 0.58f, 0.52f));
        RectTransform hRt = hint.rectTransform;
        hRt.anchorMin = new Vector2(0f, 0f);
        hRt.anchorMax = new Vector2(1f, 0f);
        hRt.pivot = new Vector2(0.5f, 0f);
        hRt.anchoredPosition = new Vector2(0f, 12f);
        hRt.sizeDelta = new Vector2(0f, 26f);
    }

    /// <summary>»ó´Ü¿¡ °¡·Î·Î ºÙ´Â ÅØ½ºÆ® ¹èÄ¡ ÇïÆÛ</summary>
    private void SetTopStretch(RectTransform rt, float y, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(0f, height);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ±¸¸Å Ã³¸®
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void OnBuy(int index)
    {
        ShopItem item = items[index];
        int level = MetaProgress.UpgradeLevel(item.id);
        int cost = item.CostAt(level);

        if (MetaProgress.TryBuyUpgrade(item.id, cost, item.maxLevel))
        {
            UIManager.Instance?.ShowStatChange("[¸í¼º »óÁ¡] " + item.itemName + " Lv."
                + MetaProgress.UpgradeLevel(item.id) + " ±¸¸Å!");
        }
        Refresh();
    }

    /// <summary>º¸À¯ ¸í¼º / °¢ »óÇ°ÀÇ ·¹º§, °¡°Ý, ¹öÆ° »óÅÂ °»½Å</summary>
    private void Refresh()
    {
        fameText.text = "º¸À¯ ¸í¼º: " + MetaProgress.Fame
            + "   |   ÃÖ°í ±â·Ï: " + MetaProgress.BestWave + "¿þÀÌºê"
            + "   |   µµ°¨: " + MetaProgress.DiscoveredCount + "Á¾";

        for (int i = 0; i < items.Length; i++)
        {
            int level = MetaProgress.UpgradeLevel(items[i].id);
            bool maxed = level >= items[i].maxLevel;
            levelTexts[i].text = "Lv." + level + " / " + items[i].maxLevel;

            if (maxed)
            {
                buyLabels[i].text = "¿Ï¼º";
                buyButtons[i].interactable = false;
            }
            else
            {
                int cost = items[i].CostAt(level);
                buyLabels[i].text = cost + " ¸í¼º";
                buyButtons[i].interactable = MetaProgress.Fame >= cost;
            }
        }
    }
}
