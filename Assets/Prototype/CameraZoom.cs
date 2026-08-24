using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// [CameraZoom.cs] v3
/// ¸¶¿ì½º ÈÙ·Î Ä«¸Þ¶ó ÁÜÀÎ/ÁÜ¾Æ¿ôÇÕ´Ï´Ù.
/// Main Camera ¿ÀºêÁ§Æ®¿¡ ºÙÀÌ¼¼¿ä.
/// ÁÜ¾Æ¿ô: ÀüÀå ÀüÃ¼ ÆÄ¾Ç / ÁÜÀÎ: ÁÖ¹æ Á¤¹Ð Á¶ÀÛ
///
/// - v3 º¯°æÁ¡ (P1 °ÔÀÓÇÊ):
///   1) GameFeel.ShakeOffset ¹Ý¿µ - È­¸é ¼ÎÀÌÅ©´Â ÀÌ ½ºÅ©¸³Æ®°¡ ÃÖÁ¾ Àû¿ë
///      (³»ºÎ basePos¿¡ ÃßÀû À§Ä¡¸¦ µû·Î º¸°üÇØ¼­ ¼ÎÀÌÅ©°¡ ÃßÀû¿¡ ¼¯¿© ¿À¿°µÇÁö ¾ÊÀ½)
///   2) ÁÜ ¸®¼Â Å° R -> Z º¯°æ (RÀº º¸½ºÀü '¸¶Áö¸· ÁÖ¹®'(C-2)¿¡ ¹èÁ¤µÊ)
/// - v2 º¯°æÁ¡:
///   1) ÁÜ ¹üÀ§ ´ëÆø È®Àå (3~12 -> 2~20). ¼öÄ¡´Â StartÀÇ °­Á¦ Àû¿ë°ª¿¡¼­ Á¶Àý
///   2) ¸¶¿ì½º°¡ UI À§¿¡ ÀÖÀ¸¸é ÁÜ ¹«½Ã - ÇÏ´Ü ¿ä¸® ¸ñ·Ï ÈÙ ½ºÅ©·Ñ°ú Ãæµ¹ ¹æÁö
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
    private void Start()
    {
        // ÁÜ ¹üÀ§ °­Á¦ Àû¿ë (Inspector¿¡ ÀúÀåµÈ ±¸°ª ¹«½Ã - Á¶ÀýÀº ¿©±â ¼ýÀÚ·Î)
        zoomSpeed = 3f;
        minZoom = 2f;      // ÁÖ¹æ Á¤¹Ð Á¶ÀÛ¿ë ±ÙÁ¢
        maxZoom = 20f;     // ÀüÀå ÀüÃ¼ + ½ºÆù ÁöÁ¡±îÁö Á¶¸Á
        defaultZoom = 7f;

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

        Debug.Log("[CameraZoom] Ä«¸Þ¶ó ÃÊ±âÈ­ ¿Ï·á (ÁÜ ¹üÀ§ " + minZoom + "~" + maxZoom + ", ±âº» " + defaultZoom + ")");
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
        if (targetTransform != null)
        {
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
