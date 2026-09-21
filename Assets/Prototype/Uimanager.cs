using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// [UIManager.cs] v3.1 (v9.11 2026-09-22 Å¸°Ý°¨: ±âÂ÷ HP ¹Ù Áö¿¬ ÀÜ·®(»¡°£ ¶ì°¡ 0.5ÃÊ µÚ µû¶ó ³»·Á¿Â´Ù, È¸º¹Àº Áï½Ã) / ¿þÀÌºê ¿¹°í¡¤Å¬¸®¾î ¹®±¸ À§¿¡¼­ ³»·Á¿À¸ç ÆË, »õ ¹®±¸°¡ ¿À¸é ÀÌÀü ¹®±¸ Áï½Ã ±³Ã¼) / v3
/// °ÔÀÓ HUD ÀüÃ¼¸¦ ´ã´çÇÏ´Â UI °ü¸® ½ºÅ©¸³Æ®ÀÔ´Ï´Ù.
/// - v3 º¯°æÁ¡ (P1: ¾Ë¸² Ã¤³Î 2ºÐ¸® - ±â¼ú°¨»ç Ã³¹æ):
///   1) ShowStatChange°¡ "¿ìÃø ·Î±× ½ºÅÃ"À¸·Î °³Á¶ - ¿©·¯ ¾Ë¸²ÀÌ °ãÃÄµµ ¾ÃÈ÷Áö ¾Ê°í
///      ÃÖ±Ù 5ÁÙÀÌ ½×¿´´Ù°¡ Â÷·Ê·Î »ç¶óÁø´Ù (È£ÃâºÎ 30¿© °÷Àº ¼öÁ¤ ºÒÇÊ¿ä)
///   2) ShowDanger ½Å¼³ - À§Çè ¾Ë¸²(ºù°á/±â¸§/µ¶Ä§ µî)Àº ÁÖÈ² ±½Àº ÁÙ·Î ±¸ºÐ
///   3) ´ëÇü °æ°í(º¸½º ¿¹°í µî)´Â ±âÁ¸ WarningFX(Áß¾Ó+°¡ÀåÀÚ¸® ¸Æµ¿)°¡ ´ã´ç - Ã¤³Î 2°³ Ã¼Á¦
///   4) ¾ÀÀÇ StatChangeText ¿ÀºêÁ§Æ®´Â ´õ ÀÌ»ó »ç¿ëÇÏÁö ¾ÊÀ½ (ÀÚµ¿ ºñÈ°¼º, »èÁ¦ÇØµµ ¹«¹æ)
/// - v2 º¯°æÁ¡ (±¸½Ã½ºÅÛ Á¤¸®):
///   1) Æ÷¸¸°¨ °ÔÀÌÁö / Çã±â °æ°í ¿¬Ãâ ÀüºÎ Á¦°Å (Çã±â ½Ã½ºÅÛ »èÁ¦)
///   2) '´ÙÀ½ ¿þÀÌºê' ¹öÆ° UI Á¦°Å (¿þÀÌºê´Â Áõ°­ ¼±ÅÃ ÈÄ ÀÚµ¿ ÁøÇà)
///   3) HP ¹Ù / °ñµå¡¤¿þÀÌºê ÅØ½ºÆ® / »óÅÂ ÆÐ³Î / ¿þÀÌºê ¿¹°í Ç¥½Ã´Â À¯Áö
/// VS 2017 (C# 7.3) È£È¯ ¹öÀüÀÔ´Ï´Ù.
/// </summary>
public class UIManager : MonoBehaviour
{
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ½Ì±ÛÅæ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public static UIManager Instance { get; private set; }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // HP ¹Ù
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [Header("¦¡ HP ¹Ù ¦¡")]
    public Slider hpSlider;
    private Image hpTrailImage;            // v3.1: Áö¿¬ ÀÜ·® (ÄÚµå »ý¼º, Ã¤¿ò µÚ)
    private float hpTrailRatio = 1f;       // v3.1: Áö¿¬ ÀÜ·® ºñÀ²
    private float hpTrailHoldUntil = 0f;   // v3.1: ÀÌ ½Ã°¢±îÁö ¸ØÃè´Ù°¡ ³»·Á¿Â´Ù
    private float hpLastRatio = 1f;
    private Coroutine waveNoticeRoutine;   // v3.1: ÁøÇà ÁßÀÎ ¿¹°í (»õ ¹®±¸°¡ ¿À¸é ²÷´Â´Ù)
    private Vector2 waveNoticeBasePos, waveWarningBasePos;
    private bool waveNoticeBaseSaved = false;
    public Image hpFillImage;
    public TextMeshProUGUI hpText;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // »ó´Ü Á¤º¸
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [Header("¦¡ »ó´Ü Á¤º¸ ÅØ½ºÆ® ¦¡")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI stateText;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // °ÔÀÓ »óÅÂº° ÆÐ³Î
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [Header("¦¡ °ÔÀÓ »óÅÂº° ÆÐ³Î ¦¡")]
    public GameObject lobbyPanel;
    public GameObject battlePanel;
    public GameObject townPanel;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    [Header("¦¡ HP »ö»ó ¦¡")]
    public Color colorHPHigh = new Color(0.2f, 0.8f, 0.2f);
    public Color colorHPMid = new Color(1.0f, 0.8f, 0.0f);
    public Color colorHPLow = new Color(1.0f, 0.2f, 0.2f);

    [Header("¦¡ ¾Ë¸² ÅØ½ºÆ® ¦¡")]
    public TextMeshProUGUI statChangeText;    // ½ºÅÈ º¯È­ / Àç·á È¹µæ ¾Ë¸²
    public TextMeshProUGUI waveNoticeText;    // ¿þÀÌºê ¼Ó¼º ¿¹°í ÅØ½ºÆ®
    public TextMeshProUGUI waveWarningText;   // µå·Ó/´ëÀÀ ¾È³» ÅØ½ºÆ®

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ³»ºÎ ÂüÁ¶
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private TrainManager trainManager;
    private GameManager gameManager;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÃÊ±âÈ­
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        trainManager = FindFirstObjectByType<TrainManager>();
        gameManager = GameManager.Instance;

        if (gameManager != null)
            gameManager.OnGameStateChanged.AddListener(OnGameStateChanged);

        SetupSliders();
        ShowOnlyPanel(lobbyPanel);

        if (statChangeText != null) statChangeText.gameObject.SetActive(false);

        Debug.Log("[UIManager] HUD ÃÊ±âÈ­ ¿Ï·á (v2 - Æ÷¸¸°¨/´ÙÀ½¿þÀÌºê ¹öÆ° Á¦°Å)");
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ½½¶óÀÌ´õ ÃÊ±â ¼³Á¤
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void SetupSliders()
    {
        if (hpSlider != null && trainManager != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = trainManager.currentMaxHP;
            hpSlider.value = trainManager.currentHP;
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¸Å ÇÁ·¹ÀÓ °»½Å
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void Update()
    {
        RefreshHPBar();
        RefreshInfoTexts();
        UpdateLogStack();   // P1: ¿ìÃø ¾Ë¸² ·Î±× ¼ö¸í °ü¸®
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // HP ¹Ù °»½Å
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void RefreshHPBar()
    {
        if (trainManager == null || hpSlider == null) return;

        hpSlider.maxValue = trainManager.currentMaxHP;
        hpSlider.value = trainManager.currentHP;

        if (hpFillImage != null)
        {
            float hpRatio = trainManager.currentHP / trainManager.currentMaxHP;
            if (hpRatio >= 0.7f) hpFillImage.color = colorHPHigh;
            else if (hpRatio >= 0.3f) hpFillImage.color = colorHPMid;
            else hpFillImage.color = colorHPLow;
        }

        if (hpText != null)
            hpText.text = (int)trainManager.currentHP + " / " + (int)trainManager.currentMaxHP;

        RefreshHPTrail();
    }

    /// <summary>
    /// v3.1: Áö¿¬ ÀÜ·® - ¸ÂÀ¸¸é »¡°£ ¶ì°¡ Àá±ñ(TrailingHpDelay) ³²¾Æ ÀÖ´Ù°¡ TrailingHpSpeed(ÃÊ´ç ºñÀ²)·Î µû¶ó ³»·Á¿Â´Ù.
    /// È¸º¹Àº Áï½Ã µû¶ó°£´Ù. Ã¤¿ò(hpFillImage)ÀÌ Filled Å¸ÀÔÀÌ¸é fillAmount, ¾Æ´Ï¸é ¾ÞÄ¿·Î °°Àº ¹æ½ÄÀ¸·Î ±×¸°´Ù.
    /// </summary>
    private void RefreshHPTrail()
    {
        if (!GameBalance.TrailingHpBarOn || hpFillImage == null) { if (hpTrailImage != null) hpTrailImage.enabled = false; return; }
        float ratio = Mathf.Clamp01(trainManager.currentHP / Mathf.Max(1f, trainManager.currentMaxHP));

        if (hpTrailImage == null)
        {
            GameObject go = new GameObject("HpTrail");
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(hpFillImage.transform.parent, false);
            rt.SetSiblingIndex(hpFillImage.transform.GetSiblingIndex());   // Ã¤¿ò µÚ¿¡ ±×·ÁÁø´Ù
            RectTransform fillRt = hpFillImage.rectTransform;
            rt.anchorMin = fillRt.anchorMin; rt.anchorMax = fillRt.anchorMax;
            rt.offsetMin = fillRt.offsetMin; rt.offsetMax = fillRt.offsetMax;
            rt.pivot = fillRt.pivot;
            hpTrailImage = go.AddComponent<Image>();
            hpTrailImage.sprite = hpFillImage.sprite;
            hpTrailImage.type = hpFillImage.type;
            hpTrailImage.fillMethod = hpFillImage.fillMethod;
            hpTrailImage.fillOrigin = hpFillImage.fillOrigin;
            hpTrailImage.color = new Color(0.85f, 0.18f, 0.12f, 0.95f);
            hpTrailImage.raycastTarget = false;
            hpTrailRatio = ratio; hpLastRatio = ratio;
        }
        hpTrailImage.enabled = true;

        if (ratio < hpLastRatio - 0.0005f) hpTrailHoldUntil = Time.unscaledTime + GameBalance.TrailingHpDelay;   // ¸Â¾Ò´Ù - Àá±ñ ¸ØÃã
        hpLastRatio = ratio;

        if (ratio >= hpTrailRatio) hpTrailRatio = ratio;   // È¸º¹¡¤ÃÊ±âÈ­´Â Áï½Ã
        else if (Time.unscaledTime >= hpTrailHoldUntil)
            hpTrailRatio = Mathf.MoveTowards(hpTrailRatio, ratio, GameBalance.TrailingHpSpeed * Time.unscaledDeltaTime);

        RectTransform trt = hpTrailImage.rectTransform;
        if (hpFillImage.type == Image.Type.Filled)
        {
            hpTrailImage.fillAmount = hpTrailRatio;
        }
        else
        {
            // Slider °¡ Ã¤¿ò ¾ÞÄ¿¸¦ 0..value ·Î ³õ´Â °Í°ú °°Àº ¹æ½Ä
            RectTransform fillRt = hpFillImage.rectTransform;
            Vector2 aMin = fillRt.anchorMin, aMax = fillRt.anchorMax;
            if (hpSlider != null && hpSlider.direction == Slider.Direction.LeftToRight) { aMin.x = 0f; aMax.x = hpTrailRatio; }
            else if (hpSlider != null && hpSlider.direction == Slider.Direction.RightToLeft) { aMin.x = 1f - hpTrailRatio; aMax.x = 1f; }
            trt.anchorMin = aMin; trt.anchorMax = aMax;
            trt.offsetMin = fillRt.offsetMin; trt.offsetMax = fillRt.offsetMax;
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // °ñµå ¡¤ ¿þÀÌºê ÅØ½ºÆ® °»½Å
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void RefreshInfoTexts()
    {
        if (gameManager == null) return;

        if (goldText != null)
            goldText.text = "G  " + gameManager.playerGold;

        if (waveText != null)
            waveText.text = "Wave  " + gameManager.currentWave;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // P1: ¾Ë¸² ·Î±× ½ºÅÃ (¿ìÃø) - Ã¤³Î 1 (ÀÏ¹Ý/À§Çè ¶óÀÎ)
    // Ã¤³Î 2(´ëÇü °æ°í)´Â WarningFX.Flash°¡ ´ã´ç.
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    private const int LOG_LINES = 5;       // µ¿½Ã Ç¥½Ã ÁÙ ¼ö
    private const float LOG_LIFE = 3.5f;   // ÁÙ ¼ö¸í(ÃÊ)
    private const float LOG_FADE = 0.6f;   // ¼ö¸í ³¡ ÆäÀÌµå ±¸°£

    private Text[] logTexts;               // ÄÚµå »ý¼º ·Î±× ÁÙ (0 = ÃÖ½Å, ¸Ç À§)
    private string[] logMsgs = new string[LOG_LINES];
    private Color[] logColors = new Color[LOG_LINES];
    private float[] logAges = new float[LOG_LINES];   // °æ°ú ½Ã°£ (¼ö¸í Áö³ª¸é ¼û±è)
    private bool[] logUsed = new bool[LOG_LINES];

    private static readonly Color LOG_NORMAL = new Color(1f, 0.92f, 0.55f);   // ÀÏ¹Ý: Å©¸² ³ë¶û
    private static readonly Color LOG_DANGER = new Color(1f, 0.5f, 0.25f);    // À§Çè: ÁÖÈ²

    /// <summary>
    /// ÀÏ¹Ý ¾Ë¸² (º¸»ó/È¹µæ/ÁøÇà µî). ¿©·¯ °³°¡ ¿¬´Þ¾Æ ¿Íµµ ½ºÅÃ¿¡ ½×¿© ¾ÃÈ÷Áö ¾Ê´Â´Ù.
    /// »ç¿ë¹ý: UIManager.Instance?.ShowStatChange("Àç·á +1");
    /// </summary>
    public void ShowStatChange(string message)
    {
        PushLog(message, LOG_NORMAL, false);
    }

    /// <summary>
    /// À§Çè ¾Ë¸² (ºù°á/±â¸§/µ¶Ä§ µî Áö±Ý ÇÃ·¹ÀÌ¿¡ ¿µÇâ ÁÖ´Â °Í) - ÁÖÈ² ±½Àº ÁÙ.
    /// º¸½º±Þ ´ëÇü °æ°í´Â ÀÌ°É ¾²Áö ¸»°í WarningFX.Flash¸¦ ¾µ °Í.
    /// </summary>
    public void ShowDanger(string message)
    {
        PushLog(message, LOG_DANGER, true);
    }

    private void PushLog(string message, Color col, bool bold)
    {
        if (logTexts == null) BuildLogStack();

        // ÇÑ Ä­¾¿ ¾Æ·¡·Î ¹Ð±â (¸Ç ¾Æ·¡´Â ¹ö¸²)
        for (int i = LOG_LINES - 1; i >= 1; i--)
        {
            logMsgs[i] = logMsgs[i - 1];
            logColors[i] = logColors[i - 1];
            logAges[i] = logAges[i - 1];
            logUsed[i] = logUsed[i - 1];
            logTexts[i].fontStyle = logTexts[i - 1].fontStyle;
        }

        logMsgs[0] = message;
        logColors[0] = col;
        logAges[0] = 0f;
        logUsed[0] = true;
        logTexts[0].fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;

        RenderLog();
    }

    /// <summary>¸Å ÇÁ·¹ÀÓ: ·Î±× ¼ö¸í/ÆäÀÌµå °»½Å (ÀÏ½ÃÁ¤Áö Áß¿¡µµ Èå¸£°Ô unscaled)</summary>
    private void UpdateLogStack()
    {
        if (logTexts == null) return;

        bool any = false;
        for (int i = 0; i < LOG_LINES; i++)
        {
            if (!logUsed[i]) continue;
            logAges[i] += Time.unscaledDeltaTime;
            if (logAges[i] >= LOG_LIFE) logUsed[i] = false;
            else any = true;
        }
        if (any || logTexts[0].gameObject.activeSelf) RenderLog();
    }

    private void RenderLog()
    {
        for (int i = 0; i < LOG_LINES; i++)
        {
            if (!logUsed[i])
            {
                if (logTexts[i].gameObject.activeSelf) logTexts[i].gameObject.SetActive(false);
                continue;
            }

            float alpha = 1f;
            float remain = LOG_LIFE - logAges[i];
            if (remain < LOG_FADE) alpha = Mathf.Clamp01(remain / LOG_FADE);
            // ¾Æ·¡ ÁÙ(¿À·¡µÈ °Í)ÀÏ¼ö·Ï »ìÂ¦ Èå¸®°Ô - ½Ã¼±Àº ÃÖ½Å ÁÙ·Î
            alpha *= Mathf.Lerp(1f, 0.55f, i / (float)(LOG_LINES - 1));

            logTexts[i].text = logMsgs[i];
            Color c = logColors[i];
            c.a = alpha;
            logTexts[i].color = c;
            if (!logTexts[i].gameObject.activeSelf) logTexts[i].gameObject.SetActive(true);
        }
    }

    /// <summary>·Î±× ½ºÅÃ UI »ý¼º (ÃÖÃÊ 1È¸, ÄÚµå »ý¼º - ¾À ÀÛ¾÷ ºÒÇÊ¿ä)</summary>
    private void BuildLogStack()
    {
        GameObject canvasGo = new GameObject("LogStackCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 455;   // °ø¸í HUD(470) ¹Ù·Î ¾Æ·¡
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        logTexts = new Text[LOG_LINES];
        for (int i = 0; i < LOG_LINES; i++)
        {
            Text t = KitchenEventManager.MakeText(canvasGo.transform, "Log" + i, "", 19, LOG_NORMAL);
            RectTransform rt = t.rectTransform;
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-14f, 200f - i * 28f);   // ¿ìÃø, À§¿¡¼­ ¾Æ·¡·Î
            rt.sizeDelta = new Vector2(560f, 26f);
            t.alignment = TextAnchor.MiddleRight;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;

            // °¡µ¶¼º¿ë ¾ãÀº ±×¸²ÀÚ
            UnityEngine.UI.Shadow sh = t.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            sh.effectDistance = new Vector2(1f, -1f);
            sh.effectColor = new Color(0f, 0f, 0f, 0.8f);

            t.gameObject.SetActive(false);
            logTexts[i] = t;
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // °ÔÀÓ »óÅÂ º¯°æ ÄÝ¹é
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void OnGameStateChanged(GameManager.GameState newState)
    {
        if (stateText != null)
        {
            if (newState == GameManager.GameState.Lobby)
                stateText.text = "´ë±â Áß";
            else if (newState == GameManager.GameState.Battle)
                stateText.text = "ÀüÅõ Áß";
            else if (newState == GameManager.GameState.Town)
                stateText.text = "¸¶À» Á¤ºñ";
            else if (newState == GameManager.GameState.GameOver)
                stateText.text = "°ÔÀÓ ¿À¹ö";
            else if (newState == GameManager.GameState.Victory)
                stateText.text = "½Â¸®!";
            else
                stateText.text = "";
        }

        // ÆÐ³Î ÀüÈ¯
        if (newState == GameManager.GameState.Lobby)
            ShowOnlyPanel(lobbyPanel);
        else if (newState == GameManager.GameState.Battle)
            ShowOnlyPanel(battlePanel);
        else if (newState == GameManager.GameState.Town)
            ShowOnlyPanel(townPanel);
        else if (newState == GameManager.GameState.GameOver)
            ShowOnlyPanel(gameOverPanel);
        else if (newState == GameManager.GameState.Victory)
            ShowOnlyPanel(victoryPanel);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÆÐ³Î ÀüÈ¯
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void ShowOnlyPanel(GameObject targetPanel)
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (battlePanel != null) battlePanel.SetActive(false);
        if (townPanel != null) townPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);

        if (targetPanel != null) targetPanel.SetActive(true);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¹öÆ° OnClick ¿¬°á¿ë
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public void OnClickStartGame()
    {
        GameManager.Instance?.ChangeState(GameManager.GameState.Battle);
    }

    public void OnClickStartBattle()
    {
        GameManager.Instance?.ChangeState(GameManager.GameState.Battle);
    }

    public void OnClickGoToTown()
    {
        GameManager.Instance?.ChangeState(GameManager.GameState.Town);
    }

    public void OnClickRestart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public void OnClickNextWave()
    {
        GameManager.Instance?.OnClickNextWave();
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ±¸½Ã½ºÅÛ È£È¯ ½ºÅÓ (´ÙÀ½ ¿þÀÌºê ¹öÆ° Á¦°ÅµÊ)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    /// <summary>[±¸½Ã½ºÅÛ È£È¯] ¹öÆ° UI Á¦°ÅµÊ - ¾Æ¹« °Íµµ ÇÏÁö ¾Ê´Â´Ù</summary>
    public void ShowNextWaveButton(int nextWave) { }

    /// <summary>[±¸½Ã½ºÅÛ È£È¯] ¹öÆ° UI Á¦°ÅµÊ - ¾Æ¹« °Íµµ ÇÏÁö ¾Ê´Â´Ù</summary>
    public void HideNextWaveButton() { }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¿þÀÌºê ¿¹°í
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    /// <summary>¿þÀÌºê ½ÃÀÛ ½Ã ¼Ó¼º ¿¹°í Ç¥½Ã (3ÃÊ ÈÄ »ç¶óÁü)</summary>
    public void ShowWaveNotice(string notice, string warning)
    {
        // v3.1: ÀÌÀü ¹®±¸°¡ ¾ÆÁ÷ ¶° ÀÖÀ¸¸é ²÷°í »õ ¹®±¸·Î (µÎ ÄÚ·çÆ¾ÀÌ ¼­·Î ¾ËÆÄ¸¦ µ¤¾î¾²´ø °Í)
        if (waveNoticeRoutine != null) StopCoroutine(waveNoticeRoutine);
        waveNoticeRoutine = StartCoroutine(WaveNoticeCoroutine(notice, warning));
    }

    private IEnumerator WaveNoticeCoroutine(string notice, string warning)
    {
        if (!waveNoticeBaseSaved)
        {
            if (waveNoticeText != null) waveNoticeBasePos = waveNoticeText.rectTransform.anchoredPosition;
            if (waveWarningText != null) waveWarningBasePos = waveWarningText.rectTransform.anchoredPosition;
            waveNoticeBaseSaved = true;
        }

        if (waveNoticeText != null)
        {
            waveNoticeText.gameObject.SetActive(true);
            waveNoticeText.text = notice;
            waveNoticeText.color = new Color(1f, 0.9f, 0.2f, 1f); // ³ë¶õ»ö
            waveNoticeText.rectTransform.anchoredPosition = waveNoticeBasePos;
            waveNoticeText.rectTransform.localScale = Vector3.one;
        }

        bool hasWarning = waveWarningText != null && !string.IsNullOrEmpty(warning);
        if (hasWarning)
        {
            waveWarningText.gameObject.SetActive(true);
            waveWarningText.text = warning;
            waveWarningText.color = new Color(0.4f, 1f, 0.4f, 1f); // ÃÊ·Ï»ö
            waveWarningText.rectTransform.anchoredPosition = waveWarningBasePos;
        }
        else if (waveWarningText != null) waveWarningText.gameObject.SetActive(false);

        // v3.1: À§¿¡¼­ ³»·Á¿À¸ç 1.15 -> 1.0 (0.2ÃÊ, ½Ç½Ã°£ - Ä«µå·Î ½Ã°£ÀÌ ¸ØÃçµµ ¿òÁ÷ÀÎ´Ù)
        if (GameBalance.WaveBannerOn && GameBalance.GameFeelMaster > 0f)
        {
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.2f);
                float e = 1f - (1f - k) * (1f - k);
                if (waveNoticeText != null)
                {
                    waveNoticeText.rectTransform.anchoredPosition = waveNoticeBasePos + Vector2.up * 40f * (1f - e);
                    waveNoticeText.rectTransform.localScale = Vector3.one * (1.15f - 0.15f * e);
                    Color c = waveNoticeText.color; c.a = e; waveNoticeText.color = c;
                }
                if (hasWarning)
                {
                    waveWarningText.rectTransform.anchoredPosition = waveWarningBasePos + Vector2.up * 24f * (1f - e);
                    Color c = waveWarningText.color; c.a = e; waveWarningText.color = c;
                }
                yield return null;
            }
            if (waveNoticeText != null) { waveNoticeText.rectTransform.anchoredPosition = waveNoticeBasePos; waveNoticeText.rectTransform.localScale = Vector3.one; }
            if (hasWarning) waveWarningText.rectTransform.anchoredPosition = waveWarningBasePos;
        }

        // 2ÃÊ Ç¥½Ã ÈÄ 1ÃÊ ÆäÀÌµå ¾Æ¿ô
        yield return new WaitForSeconds(2f);

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - elapsed;

            if (waveNoticeText != null)
            {
                Color c = waveNoticeText.color; c.a = alpha;
                waveNoticeText.color = c;
            }
            if (waveWarningText != null)
            {
                Color c = waveWarningText.color; c.a = alpha;
                waveWarningText.color = c;
            }
            yield return null;
        }

        if (waveNoticeText != null) waveNoticeText.gameObject.SetActive(false);
        if (waveWarningText != null) waveWarningText.gameObject.SetActive(false);
        waveNoticeRoutine = null;
    }
}
