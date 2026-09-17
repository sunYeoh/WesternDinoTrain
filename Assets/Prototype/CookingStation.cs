using UnityEngine;

/// <summary>
/// [CookingStation.cs] v2.2 (v9.10 2026-09-17 Å×½ºÅÍ ÇÇµå¹é: Çà»óÀÎ¡¤º£ÆÃ¡¤ºÐ±â ¼±·Î Ã¢ÀÌ ¶° ÀÖÀ» ¶§ Á¶¸® ½ÃÀÛ ±ÝÁö - "¾ÈÅ³·Î »óÀÎ ³ª¿Ã ¶§ ¿ä¸® µÇ´ø °Í") / v2.1 (±³¼ö ÇÇµå¹é A10: ¿­¶÷ ÆÐ³Î Áß ±â±¸ »óÈ£ÀÛ¿ë Â÷´Ü 2026-09-14) / v2
/// ÁÖ¹æ ±â±¸(±×¸±/ººÀ½ÆÒ/³¿ºñ) ¿ÀºêÁ§Æ®¿¡ ºÙÀÌ´Â ½ºÅ©¸³Æ®ÀÔ´Ï´Ù.
/// ¼ÎÇÁ°¡ »óÈ£ÀÛ¿ë ¹üÀ§ ¾È¿¡¼­ EÅ°¸¦ ´©¸£¸é Á¶¸®Ã¢ÀÌ ¿­¸³´Ï´Ù.
///
/// - v2 º¯°æÁ¡ (Á¶¸® ½Ã½ºÅÛ ÅëÀÏ):
///   EÅ° -> ChefController ±¸ ¹Ì´Ï°ÔÀÓ ´ë½Å KitchenPanel(»õ Á¶¸®Ã¢)À» ¿¬´Ù.
///   ÀÌ¶§ ÀÌ Á¶¸®´ëÀÇ Á¶¸®¹ý¿¡ ¸Â´Â ¿ä¸®¸¸ Ç¥½ÃµÈ´Ù:
///     ±×¸± = ±Á±â ¿ä¸® / ººÀ½ÆÒ = ºº±â ¿ä¸® / ³¿ºñ = ²úÀÌ±â ¿ä¸®
///   (TabÀ¸·Î ¿­¸é ÀüÃ¼ ¿ä¸®°¡ º¸ÀÌ¹Ç·Î, Á¶¸®´ë´Â "°¡±î¿î ±â±¸¿¡¼­ ¹Ù·Î Á¶¸®" µ¿¼±¿ë)
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

        // »óÈ£ÀÛ¿ë ÇÁ·ÒÇÁÆ® ±âº» ºñÈ°¼ºÈ­
        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        Debug.Log("[CookingStation] " + GetStationName() + " ÃÊ±âÈ­ ¿Ï·á (v2 - »õ Á¶¸®Ã¢ ¿¬µ¿)");
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¸Å ÇÁ·¹ÀÓ: ¼ÎÇÁ °Å¸® Ã¼Å© + Å° ÀÔ·Â
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void Update()
    {
        // ´Ù¸¥ ÀüÃ¼È­¸é UI ÁøÇà Áß¿£ ±â±¸ »óÈ£ÀÛ¿ë Â÷´Ü
        if (CookingMinigame.IsActive || KitchenEventManager.IsActive ||
            KitchenPanel.IsOpenStatic || WorkshopUI.IsOpen || AugmentPickUI.IsOpen ||
            AugmentListUI.ReadingOpen ||   // A10: ¿­¶÷ ÆÐ³Î(V/J) Áß¿¡´Â ¸ØÃá ½Ã°£¿¡ Á¶¸® ½ÃÀÛ ºÒ°¡
            MerchantUI.IsOpen || SpinoBetUI.IsOpen || BranchRouteUI.IsOpen)   // v2.2: Á¤Â÷¿ª Ã¢(Çà»óÀÎ¡¤º£ÆÃ¡¤¼±·Î)ÀÌ ÀÔ·ÂÀ» µ¶Á¡ÇÑ´Ù
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

    /// <summary>[±¸½Ã½ºÅÛ È£È¯] MaterialSelectUI µîÀÌ È£ÃâÇÒ ¼ö ÀÖÀ½ - ÀÌÁ¦ ÇÒ ÀÏ ¾øÀ½</summary>
    public void ResetCooking() { }

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
