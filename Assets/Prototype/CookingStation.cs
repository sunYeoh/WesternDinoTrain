using UnityEngine;

/// <summary>
/// [CookingStation.cs] v2.5 (v9.37 2026-10-29 À¯Àú "E-Á¶¸®°¡ ¾Æ´Ï¶ó E-È­·Î, E-²úÀÌ±â ½ÄÀ¸·Î": ¾È³» ±ÛÀ» "[E] ±Á±â / [E] ºº±â / [E] ²úÀÌ±â" ·Î (StationPromptVerb false ¸é "[E] ±×¸± / ÆÒ / ¼Ü". ¾À ±ÛÀ» Start ¿¡¼­ ¹Ù²Û´Ù - TextMesh ¡¤ TMP ¡¤ UI.Text ¾î´À °ÍÀÌµç, GameBalance.StationPromptNames) / Á¶¸® µ¶(GameHUD)ÀÌ ¾²´Â CanInteractNow ¡¤ OpenFromDock - Ã¢ °¡¸² °Ë»ç´Â [E] ¿Í ÇÑ °÷(WindowsClear)) / v2.4 (v9.26 2026-10-16 ¸¶°¨ Á¤¸®: ¿¾ UI ½ºÅÓ ResetCooking »èÁ¦) / v2.3 (v9.14 2026-09-28: [E] ¾È³» ±ÛÀÚ ¹èÀ² GameBalance.InteractPromptScale) / v2.2 (v9.10 2026-09-17 Å×½ºÅÍ ÇÇµå¹é: Çà»óÀÎ¡¤º£ÆÃ¡¤ºÐ±â ¼±·Î Ã¢ÀÌ ¶° ÀÖÀ» ¶§ Á¶¸® ½ÃÀÛ ±ÝÁö - "¾ÈÅ³·Î »óÀÎ ³ª¿Ã ¶§ ¿ä¸® µÇ´ø °Í") / v2.1 (±³¼ö ÇÇµå¹é A10: ¿­¶÷ ÆÐ³Î Áß ±â±¸ »óÈ£ÀÛ¿ë Â÷´Ü 2026-09-14) / v2
/// ÁÖ¹æ ±â±¸(±×¸±/ººÀ½ÆÒ/³¿ºñ) ¿ÀºêÁ§Æ®¿¡ ºÙÀÌ´Â ½ºÅ©¸³Æ®ÀÔ´Ï´Ù.
/// ¼ÎÇÁ°¡ »óÈ£ÀÛ¿ë ¹üÀ§ ¾È¿¡¼­ EÅ°¸¦ ´©¸£¸é Á¶¸®Ã¢ÀÌ ¿­¸³´Ï´Ù.
///
/// - v2 º¯°æÁ¡ (Á¶¸® ½Ã½ºÅÛ ÅëÀÏ):
///   EÅ° -> ChefController ±¸ ¹Ì´Ï°ÔÀÓ ´ë½Å KitchenPanel(»õ Á¶¸®Ã¢)À» ¿¬´Ù.
///   ÀÌ¶§ ÀÌ Á¶¸®´ëÀÇ Á¶¸®¹ý¿¡ ¸Â´Â ¿ä¸®¸¸ Ç¥½ÃµÈ´Ù:
///     ±×¸± = ±Á±â ¿ä¸® / ººÀ½ÆÒ = ºº±â ¿ä¸® / ³¿ºñ = ²úÀÌ±â ¿ä¸®
///   (Á¶¸® Ã¢Àº Á¶¸®´ë¿¡¼­¸¸ ¿¬´Ù - Tab ÀüÃ¼ º¸±â´Â v9.26 ¿¡¼­ Áö¿ü´Ù)
///   ±¸ CookingUIManager / MaterialSelectUI °æÀ¯ Èå¸§ Á¦°Å.
///
/// »ç¿ë¹ý:
/// 1. ÁÖ¹æ ¾È¿¡ ºó ¿ÀºêÁ§Æ® 3°³ ¸¸µé±â (Grill, SautePan, Pot)
/// 2. °¢ ¿ÀºêÁ§Æ®¿¡ CookingStation ½ºÅ©¸³Æ® ºÙÀÌ±â
/// 3. stationType ¼³Á¤ (Grilling/Saute/Boiling)
/// VS 2017 (C# 7.3) È£È¯ ¹öÀüÀÔ´Ï´Ù.
/// </summary>
public class CookingStation : MonoBehaviour
{
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Á¶¸® ±â±¸ Á¾·ù
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public enum StationType
    {
        Grilling,  // ±×¸±    - ±Á±â
        Saute,     // ººÀ½ÆÒ  - ºº±â
        Boiling    // ³¿ºñ    - ²úÀÌ±â
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Inspector ¼³Á¤
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [Header("¦¡ ±â±¸ ¼³Á¤ ¦¡")]
    public StationType stationType = StationType.Grilling;
    public float interactRange = 1.5f;   // »óÈ£ÀÛ¿ë °¡´É °Å¸®
    public KeyCode interactKey = KeyCode.E; // »óÈ£ÀÛ¿ë Å°

    [Header("¦¡ »óÈ£ÀÛ¿ë Ç¥½Ã ¦¡")]
    public GameObject interactPrompt;    // "[E] Á¶¸®" ÅØ½ºÆ®

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ³»ºÎ »óÅÂ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private Transform chefTransform;
    private bool isChefNearby = false;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÃÊ±âÈ­
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void Start()
    {
        // ¼ÎÇÁ ¿ÀºêÁ§Æ® ÀÚµ¿ Å½»ö
        GameObject chefObj = GameObject.Find("Chef");
        if (chefObj != null)
            chefTransform = chefObj.transform;

        // »óÈ£ÀÛ¿ë ÇÁ·ÒÇÁÆ® ±âº» ºñÈ°¼ºÈ­. v2.3 (v9.14): ±ÛÀÚ Å©°Ô (Å×½ºÅÍ "E-Á¶¸® °°Àº ±ÛÀÚ°¡ ³Ê¹« ÀÛ´Ù") - ¾À ¿ÀºêÁ§Æ®¶ó ¹èÀ²·Î
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
            if (GameBalance.InteractPromptScale > 0f && !Mathf.Approximately(GameBalance.InteractPromptScale, 1f))
                interactPrompt.transform.localScale = interactPrompt.transform.localScale * GameBalance.InteractPromptScale;
            if (GameBalance.StationPromptNames) ApplyPromptLabel();   // v2.5: "E-Á¶¸®" -> "[E] ±×¸±" µî
        }

        Debug.Log("[CookingStation] " + GetStationName() + " ÃÊ±âÈ­ ¿Ï·á (v2 - »õ Á¶¸®Ã¢ ¿¬µ¿)");
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¸Å ÇÁ·¹ÀÓ: ¼ÎÇÁ °Å¸® Ã¼Å© + Å° ÀÔ·Â
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void Update()
    {
        // ´Ù¸¥ ÀüÃ¼È­¸é UI ÁøÇà Áß¿£ ±â±¸ »óÈ£ÀÛ¿ë Â÷´Ü (v2.5: Á¶°ÇÀº WindowsClear ÇÑ °÷ - Á¶¸® µ¶µµ °°ÀÌ ¾´´Ù)
        if (!WindowsClear())
        {
            HidePrompt();
            return;
        }

        // Battle / Town »óÅÂ¿¡¼­¸¸ »óÈ£ÀÛ¿ë °¡´É
        if (GameManager.Instance != null)
        {
            GameManager.GameState state = GameManager.Instance.currentState;
            bool isInteractable = (state == GameManager.GameState.Battle ||
                                   state == GameManager.GameState.Town);
            if (!isInteractable)
            {
                HidePrompt();
                return;
            }
        }

        if (chefTransform == null) return;

        float distance = Vector2.Distance(transform.position, chefTransform.position);
        isChefNearby = (distance <= interactRange);

        // ÇÁ·ÒÇÁÆ® Ç¥½Ã/¼û±è
        if (isChefNearby)
            ShowPrompt();
        else
            HidePrompt();

        // EÅ° ÀÔ·Â ½Ã ÀÌ Á¶¸®´ë Àü¿ë Á¶¸®Ã¢ ¿­±â
        // B-1: ±ÙÃ³¿¡ ¸¶ºñ(ºù°á/°¨Àü) Æ÷Å¾ÀÌ ÀÖÀ¸¸é [E]´Â À§±â ´ëÀÀÀÌ ¿ì¼±ÇÑ´Ù
        //      (°°Àº ÇÁ·¹ÀÓ¿¡ ÇØºùÀÌ ¸ÕÀú ¼ÒºñÇßÀ¸¸é InteractConsumedFrameÀ¸·Î °¨Áö)
        if (isChefNearby && Input.GetKeyDown(interactKey))
        {
            if (ChefController.InteractConsumedFrame == Time.frameCount) return;
            if (GameBalance.ProximityInteract && chefTransform != null
                && TurretSlotManager.Instance != null
                && TurretSlotManager.Instance.HasStunnedSlotNear(
                    chefTransform.position, GameBalance.SlotReach))
                return;   // ÀÌ¹ø E´Â ÇØºù ¸ò - Á¶¸®Ã¢Àº ´ÙÀ½¿¡

            OpenKitchen();
        }
    }

    /// <summary>v2.5: Á¶¸®¹ý ¹øÈ£(0 ±Á±â 1 ºº±â 2 ²úÀÌ±â)ÀÇ ±â±¸ ÀÌ¸§ - ÁÖ¹æ Ã¢(KitchenPanel.STATION_NAMES)°ú °°Àº ¸»</summary>
    public static string StationShort(int method)
    {
        return method == 1 ? "ÆÒ" : method == 2 ? "¼Ü" : "±×¸±";
    }

    /// <summary>v2.5: ¾ÀÀÇ ¾È³» ±Û("E-Á¶¸®")À» ÀÌ ±â±¸ ±Û·Î - TextMesh ¡¤ TMP ¡¤ UI.Text ¸¦ ÀüºÎ ¹Ù²Û´Ù (±×¸²ÀÚ¿ë º¹Á¦ ±ÛÀÌ ÀÖ¾îµµ °°ÀÌ). ÇÏ³ªµµ ¾øÀ¸¸é °æ°í ÇÑ ÁÙ</summary>
    private void ApplyPromptLabel()
    {
        int method = MethodIndex();
        string label = "[E] " + (GameBalance.StationPromptVerb ? KitchenPanel.MethodName(method) : StationShort(method));
        int n = 0;
        TextMesh[] tms = interactPrompt.GetComponentsInChildren<TextMesh>(true);
        for (int i = 0; i < tms.Length; i++) { tms[i].text = label; n++; }
        TMPro.TMP_Text[] tmps = interactPrompt.GetComponentsInChildren<TMPro.TMP_Text>(true);
        for (int i = 0; i < tmps.Length; i++) { tmps[i].text = label; n++; }
        UnityEngine.UI.Text[] uts = interactPrompt.GetComponentsInChildren<UnityEngine.UI.Text>(true);
        for (int i = 0; i < uts.Length; i++) { uts[i].text = label; n++; }
        if (n == 0) Debug.LogWarning("[CookingStation] " + GetStationName() + " - [E] ¾È³» ±Û ÄÄÆ÷³ÍÆ®¸¦ ¸ø Ã£¾Ò´Ù (±×¸² ±ÛÀÌ¸é ¾À¿¡¼­ Á÷Á¢ ¹Ù²ã¾ß ÇÑ´Ù)");
    }

    /// <summary>v2.5: Á¶¸®¹ý ¹øÈ£ 0 ±Á±â 1 ºº±â 2 ²úÀÌ±â</summary>
    private int MethodIndex()
    {
        return stationType == StationType.Saute ? 1 : stationType == StationType.Boiling ? 2 : 0;
    }

    // v2.5: ÄÑÁ® ÀÖ´Â Á¶¸®´ë ¸ñ·Ï - Á¶¸® µ¶Àº ÀÌ Ä­¿¡ ½ÇÁ¦·Î ÀÖ´Â Á¶¸®´ë¸¸ ¿¬´Ù
    private static readonly System.Collections.Generic.List<CookingStation> live = new System.Collections.Generic.List<CookingStation>();
    private void OnEnable() { if (!live.Contains(this)) live.Add(this); }
    private void OnDisable() { live.Remove(this); }

    /// <summary>v2.5: ÀÌ Á¶¸®¹ýÀÇ Á¶¸®´ë°¡ Áö±Ý ±âÂ÷¿¡ ÀÖ³ª (Á¶¸® µ¶ ´ÜÃß¸¦ ÄÓÁö)</summary>
    public static bool HasStation(int method)
    {
        for (int i = 0; i < live.Count; i++)
            if (live[i] != null && live[i].isActiveAndEnabled && live[i].MethodIndex() == method) return true;
        return false;
    }

    /// <summary>v2.5: ´Ù¸¥ Ã¢ÀÌ ÇÏ³ªµµ ¾È ¶° ÀÖ³ª - [E] (Update) ¿Í Á¶¸® µ¶ÀÌ °°ÀÌ ¾´´Ù (Ã¢À» ´õÇÏ¸é ¿©±â ÇÑ °÷¸¸)</summary>
    public static bool WindowsClear()
    {
        return !(CookingMinigame.IsActive || KitchenEventManager.IsActive ||
            KitchenPanel.IsOpenStatic || WorkshopUI.IsOpen || AugmentPickUI.IsOpen ||
            AugmentListUI.ReadingOpen ||   // A10: ¿­¶÷ ÆÐ³Î(V/J) Áß¿¡´Â ¸ØÃá ½Ã°£¿¡ Á¶¸® ½ÃÀÛ ºÒ°¡
            MerchantUI.IsOpen || SpinoBetUI.IsOpen || BranchRouteUI.IsOpen);   // v2.2: Á¤Â÷¿ª Ã¢(Çà»óÀÎ¡¤º£ÆÃ¡¤¼±·Î)ÀÌ ÀÔ·ÂÀ» µ¶Á¡ÇÑ´Ù
    }

    /// <summary>v2.5: Áö±Ý Á¶¸® Ã¢À» ¿­¾îµµ µÇ³ª - [E] ¿Í °°Àº °Ë»ç (´Ù¸¥ Ã¢ÀÌ ¶° ÀÖ°Å³ª ½Î¿ò¡¤Á¤Â÷°¡ ¾Æ´Ï¸é false). Á¶¸® µ¶ ´ÜÃß°¡ ¾´´Ù</summary>
    public static bool CanInteractNow()
    {
        if (!WindowsClear() || PauseMenu.IsOpen || BriefingUI.IsOpen || StoryTexts.IsBlocking) return false;
        if (GameManager.Instance != null)
        {
            GameManager.GameState state = GameManager.Instance.currentState;
            if (state != GameManager.GameState.Battle && state != GameManager.GameState.Town) return false;
        }
        return KitchenPanel.Instance != null;
    }

    /// <summary>v2.5: Á¶¸® µ¶ ´ÜÃß - ±× Á¶¸®´ëÀÇ Á¶¸® Ã¢À» ¿¬´Ù (Á¶¸®´ë±îÁö ¾È °¡µµ µÈ´Ù). Á¤Â÷ Á¶¸® È½¼ö °Ë»ç´Â Á¶¸® ½ÃÀÛ ¶§ CookingBridge °¡ ÇÑ´Ù</summary>
    public static bool OpenFromDock(int method)
    {
        method = Mathf.Clamp(method, 0, 2);
        if (!CanInteractNow() || !HasStation(method)) return false;
        for (int i = 0; i < live.Count; i++)
            if (live[i] != null && live[i].isActiveAndEnabled && live[i].MethodIndex() == method) { live[i].OpenKitchen(); return true; }   // [E] ¿Í °°Àº ±æ (¾È³» ±Û ¼û±è ¡¤ ·Î±×)
        return false;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Á¶¸®Ã¢ ¿­±â (Á¶¸®´ë Á¶¸®¹ý ÇÊÅÍ Àû¿ë)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void OpenKitchen()
    {
        if (KitchenPanel.Instance == null)
        {
            Debug.LogWarning("[CookingStation] KitchenPanel ¾øÀ½ - GameSystems¿¡ KitchenPanel ÄÄÆ÷³ÍÆ® ÇÊ¿ä");
            return;
        }

        int method = 0;
        if (stationType == StationType.Saute) method = 1;
        else if (stationType == StationType.Boiling) method = 2;

        HidePrompt();
        KitchenPanel.Instance.OpenForStation(method);
        Debug.Log("[CookingStation] " + GetStationName() + " Á¶¸®Ã¢ ¿­¸²");
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // »óÈ£ÀÛ¿ë ÇÁ·ÒÇÁÆ® Ç¥½Ã/¼û±è
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void ShowPrompt()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(true);
    }

    private void HidePrompt()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ±â±¸ ÀÌ¸§ ¹ÝÈ¯
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private string GetStationName()
    {
        if (stationType == StationType.Grilling) return "±×¸± (±Á±â)";
        if (stationType == StationType.Saute) return "ººÀ½ÆÒ (ºº±â)";
        if (stationType == StationType.Boiling) return "³¿ºñ (²úÀÌ±â)";
        return "Á¶¸® ±â±¸";
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Scene ºä¿¡¼­ »óÈ£ÀÛ¿ë ¹üÀ§ ½Ã°¢È­
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void OnDrawGizmosSelected()
    {
        if (stationType == StationType.Grilling)
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);  // ÁÖÈ²
        else if (stationType == StationType.Saute)
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);    // ³ë¶û
        else
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);  // ÆÄ¶û

        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
