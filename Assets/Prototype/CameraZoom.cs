using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// [CameraZoom.cs] v4 (B-2: ¼ÎÇÁ ¼ÒÇÁÆ® ÆÈ·Î¿ì - ¹æÇâ°áÁ¤ 2026-08-31)
/// ¸¶¿ì½º ÈÙ·Î Ä«¸Þ¶ó ÁÜÀÎ/ÁÜ¾Æ¿ôÇÕ´Ï´Ù.
/// Main Camera ¿ÀºêÁ§Æ®¿¡ ºÙÀÌ¼¼¿ä.
/// ÁÜ¾Æ¿ô: ÀüÀå ÀüÃ¼ ÆÄ¾Ç / ÁÜÀÎ: ÁÖ¹æ Á¤¹Ð Á¶ÀÛ
///
/// - v4 º¯°æÁ¡ (B-2 Æ®·¹ÀÏ·¯ È®Àå):
///   1) ¼ÎÇÁ ¼ÒÇÁÆ® ÆÈ·Î¿ì - µ¥µåÁ¸ ¹ÛÀ¸·Î ³ª°¡¸é Ä«¸Þ¶ó X°¡ µû¶ó°£´Ù
///      (±âÂ÷°¡ 4Ä­ÀÌ µÇ¸é¼­ È­¸é ÇÑ Àå¿¡ ´Ù ¾È µé¾î°¨ - ¸öÀÌ °¡´Â °÷ÀÌ È­¸éÀÇ Áß½É)
///   2) Ä«¸Þ¶ó X ÀÌµ¿ ÇÑ°è(CamFollowMinX/MaxX) - ÀüÀåÀÌ È­¸é ¹ÛÀ¸·Î »õÁö ¾Ê°Ô
///   3) ±âº» ÁÜ 7 -> GameBalance.CamDefaultZoom(8.5) - ±ä ±âÂ÷ ÇÁ·¹ÀÌ¹Ö
///   4) ¼öÄ¡ ÀüºÎ GameBalance (CamFollowChef=false¸é ±âÁ¸ ±âÂ÷ °íÁ¤ ÃßÀûÀ¸·Î º¹±Í)
/// - v3: GameFeel.ShakeOffset ÃÖÁ¾ Àû¿ë(basePos ºÐ¸®) / ÁÜ ¸®¼Â R -> Z
/// - v2: ÁÜ ¹üÀ§ 2~20 / UI À§ ÈÙ ¹«½Ã
///
/// VS 2017 (C# 7.3) È£È¯ ¹öÀüÀÔ´Ï´Ù.
/// </summary>
public class CameraZoom : MonoBehaviour
{
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Inspector ¼³Á¤ (Âü°í¿ë - ½ÇÁ¦ °ªÀº Start¿¡¼­ °­Á¦ Àû¿ë)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [Header("¦¡ ÁÜ ¼³Á¤ (Start¿¡¼­ ¾Æ·¡ °ªÀ¸·Î µ¤¾î¾¸) ¦¡")]
    public float zoomSpeed = 3f;    // ÁÜ ¼Óµµ
    public float minZoom = 2f;    // ÃÖ´ë ÁÜÀÎ (ÀÛÀ»¼ö·Ï °¡±îÀÌ)
    public float maxZoom = 20f;   // ÃÖ´ë ÁÜ¾Æ¿ô
    public float defaultZoom = 7f;    // ±âº» Ä«¸Þ¶ó Å©±â
    public float smoothSpeed = 5f;    // ÁÜ ºÎµå·¯¿ò

    [Header("¦¡ Ä«¸Þ¶ó ÃßÀû ´ë»ó ¦¡")]
    public Transform targetTransform;  // ±âÂ÷ Transform (Inspector¿¡¼­ ¿¬°á)
    public Vector3 offset = new Vector3(0f, 0f, -10f); // Ä«¸Þ¶ó ¿ÀÇÁ¼Â

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ³»ºÎ »óÅÂ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private Camera cam;
    private float targetZoom;

    // v3: ¼ÎÀÌÅ©¸¦ Á¦¿ÜÇÑ 'ÁøÂ¥' Ä«¸Þ¶ó À§Ä¡ (¼ÎÀÌÅ©°¡ ÃßÀû Lerp¿¡ ¼¯¿© µé¾î°¡´Â °Í ¹æÁö)
    private Vector3 basePos;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÃÊ±âÈ­
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // B-2: ¼ÎÇÁ ÆÈ·Î¿ì ´ë»ó
    private Transform chefTransform;

    private void Start()
    {
        // ÁÜ ¹üÀ§ °­Á¦ Àû¿ë (Inspector¿¡ ÀúÀåµÈ ±¸°ª ¹«½Ã - Á¶ÀýÀº ¿©±â ¼ýÀÚ·Î)
        zoomSpeed = 3f;
        minZoom = 2f;      // ÁÖ¹æ Á¤¹Ð Á¶ÀÛ¿ë ±ÙÁ¢
        maxZoom = 20f;     // ÀüÀå ÀüÃ¼ + ½ºÆù ÁöÁ¡±îÁö Á¶¸Á
        defaultZoom = GameBalance.CamDefaultZoom;   // B-2: ±ä ±âÂ÷ ÇÁ·¹ÀÌ¹Ö (8.5)

        cam = GetComponent<Camera>();
        targetZoom = defaultZoom;
        basePos = transform.position;   // v3: ¼ÎÀÌÅ© ¾ø´Â ±âÁØ À§Ä¡ ÃÊ±âÈ­

        if (cam != null)
            cam.orthographicSize = defaultZoom;

        // Å¸°Ù ÀÚµ¿ Å½»ö (Inspector ¹Ì¿¬°á ½Ã)
        if (targetTransform == null)
        {
            GameObject trainObj = GameObject.FindGameObjectWithTag("Train");
            if (trainObj != null) targetTransform = trainObj.transform;
        }

        // B-2: ¼ÎÇÁ ÀÚµ¿ Å½»ö (ÆÈ·Î¿ì ´ë»ó)
        GameObject chefObj = GameObject.Find("Chef");
        if (chefObj != null) chefTransform = chefObj.transform;

        Debug.Log("[CameraZoom] Ä«¸Þ¶ó ÃÊ±âÈ­ ¿Ï·á (ÁÜ " + minZoom + "~" + maxZoom
            + ", ±âº» " + defaultZoom + ", ¼ÎÇÁ ÆÈ·Î¿ì " + (GameBalance.CamFollowChef ? "ON" : "OFF") + ")");
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¸Å ÇÁ·¹ÀÓ: ÁÜ + Ä«¸Þ¶ó À§Ä¡
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void Update()
    {
        HandleZoom();
        HandleCameraPosition();
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¸¶¿ì½º ÈÙ ÁÜ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void HandleZoom()
    {
        // ¸¶¿ì½º°¡ UI À§¿¡ ÀÖÀ¸¸é ÁÜ ¹«½Ã (¿ä¸® ¸ñ·Ï ½ºÅ©·Ñ/¹öÆ°°ú Ãæµ¹ ¹æÁö)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            // ºÎµå·¯¿î ÁÜ Àû¿ëÀº °è¼Ó (ÁøÇà ÁßÀÌ´ø ÁÜÀÌ ¶Ò ²÷±âÁö ¾Ê°Ô)
            ApplySmoothZoom();
            return;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            // ÈÙ À§ = ÁÜÀÎ (orthographicSize °¨¼Ò)
            // ÈÙ ¾Æ·¡ = ÁÜ¾Æ¿ô (orthographicSize Áõ°¡)
            targetZoom -= scroll * zoomSpeed * 10f;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        ApplySmoothZoom();
    }

    /// <summary>ºÎµå·´°Ô ÁÜ Àû¿ë</summary>
    private void ApplySmoothZoom()
    {
        if (cam == null) return;
        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,
            targetZoom,
            Time.deltaTime * smoothSpeed
        );
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Ä«¸Þ¶ó À§Ä¡ (±âÂ÷ ÃßÀû + ¼ÎÀÌÅ© ÃÖÁ¾ Àû¿ë)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void HandleCameraPosition()
    {
        // ÃßÀûÀº basePos¿¡¸¸ Àû¿ë (¼ÎÀÌÅ© ¿ÀÇÁ¼ÂÀÌ Lerp¿¡ ¿À¿°µÇÁö ¾Ê°Ô ºÐ¸®)
        if (GameBalance.CamFollowChef && chefTransform != null)
        {
            // ¦¡¦¡ B-2: ¼ÎÇÁ ¼ÒÇÁÆ® ÆÈ·Î¿ì ¦¡¦¡
            // µ¥µåÁ¸ ¾È¿¡¼­´Â Ä«¸Þ¶ó°¡ °¡¸¸È÷, ¹þ¾î³ª¸é °¡ÀåÀÚ¸®¸¦ Àâ°í µû¶ó°£´Ù.
            // X ÀÌµ¿ ÇÑ°è·Î ÀüÀå(Àû ½ºÆù ¹æÇâ)ÀÌ È­¸é ¹ÛÀ¸·Î »õ´Â °ÍÀ» ¸·´Â´Ù.
            float targetX = basePos.x;
            float dx = chefTransform.position.x - basePos.x;
            if (Mathf.Abs(dx) > GameBalance.CamDeadzone)
                targetX = chefTransform.position.x - Mathf.Sign(dx) * GameBalance.CamDeadzone;
            targetX = Mathf.Clamp(targetX, GameBalance.CamFollowMinX, GameBalance.CamFollowMaxX);

            // Y´Â ±âÂ÷ ±âÁØ À¯Áö (±âÂ÷ ÅÂ±× ¾øÀ¸¸é ÇöÀç y)
            float targetY = targetTransform != null
                ? targetTransform.position.y + offset.y : basePos.y;

            Vector3 followPos = new Vector3(targetX, targetY, offset.z);
            basePos = Vector3.Lerp(basePos, followPos, Time.deltaTime * GameBalance.CamFollowLerp);
        }
        else if (targetTransform != null)
        {
            // ÆÈ·Î¿ì ¿ÀÇÁ = ±âÁ¸ ±âÂ÷ °íÁ¤ ÃßÀû
            Vector3 targetPos = targetTransform.position + offset;
            basePos = Vector3.Lerp(basePos, targetPos, Time.deltaTime * smoothSpeed);
        }

        // v3: ¼ÎÀÌÅ© Àû¿ë. ÁÜ ¹èÀ²·Î ½ºÄÉÀÏÇØ¼­ ÁÜÀÎ/ÁÜ¾Æ¿ô »ó°ü¾øÀÌ Ã¼°¨ °­µµ°¡ ÀÏÁ¤
        float zoomScale = cam != null ? cam.orthographicSize / 7f : 1f;
        transform.position = basePos + (Vector3)(GameFeel.ShakeOffset * zoomScale);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÁÜ ¸®¼Â (ZÅ°) - v3: R¿¡¼­ º¯°æ (R = ¸¶Áö¸· ÁÖ¹®)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            targetZoom = defaultZoom;
            Debug.Log("[CameraZoom] ÁÜ ¸®¼Â");
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÁÜ ·¹º§ Á¶È¸ (0~1, 0=ÁÜ¾Æ¿ô, 1=ÁÜÀÎ)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public float GetZoomLevel()
    {
        if (cam == null) return 0.5f;
        return 1f - (cam.orthographicSize - minZoom) / (maxZoom - minZoom);
    }
}
