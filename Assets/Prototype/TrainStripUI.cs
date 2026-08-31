using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TrainStripUI.cs] v1 (신규 파일) - B-2: 기차 스트립 (방향결정 2026-08-31)
///
/// 화면 상단의 미니 기차 상황판 (FTL식). 기차가 4칸이 되어 화면 밖 칸이 생기면서,
/// "뒷칸에서 불났다"를 눈으로 아는 수단이 필요해졌다.
///  - 칸 4개 셀: 기관차 / 주방 / 포탑 A / 포탑 B
///  - 셰프가 있는 칸 = 크림색 테두리 강조
///  - 위기 표시: 이벤트 앵커 칸(주황 점멸) / 마비 포탑 칸(감전=노랑, 빙결=하늘, 과열=주황빨강)
///
/// 사용법: 없음! 파일만 넣으면 자동 생성된다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class TrainStripUI : MonoBehaviour
{
    private static TrainStripUI instance;

    private GameObject canvasGo;
    private Image[] cellBorders;
    private Image[] cellBGs;
    private Text[] cellTexts;
    private Transform chefTransform;

    private static readonly Color BG_IDLE = new Color(0.10f, 0.08f, 0.06f, 0.85f);
    private static readonly Color BORDER_IDLE = new Color(0.35f, 0.28f, 0.22f);
    private static readonly Color BORDER_CHEF = new Color(0.95f, 0.9f, 0.8f);
    private static readonly Color TEXT_IDLE = new Color(0.7f, 0.65f, 0.58f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("TrainStripUI");
        DontDestroyOnLoad(go);
        go.AddComponent<TrainStripUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        BuildUI();
    }

    private void OnDestroy()
    {
        if (canvasGo != null) Destroy(canvasGo);
        if (instance == this) instance = null;
    }

    // ─────────────────────────────────────────────
    // UI 생성 (상단 중앙 얇은 스트립)
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        canvasGo = new GameObject("TrainStripCanvas");
        DontDestroyOnLoad(canvasGo);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;   // 알림 로그(455) 바로 아래
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        int carCount = GameBalance.CarNames.Length;
        cellBorders = new Image[carCount];
        cellBGs = new Image[carCount];
        cellTexts = new Text[carCount];

        float cellW = 112f, cellH = 34f, gap = 4f;
        float totalW = carCount * cellW + (carCount - 1) * gap;

        for (int i = 0; i < carCount; i++)
        {
            GameObject cell = new GameObject("Cell_" + i);
            RectTransform rt = cell.AddComponent<RectTransform>();
            rt.SetParent(canvasGo.transform, false);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(-totalW * 0.5f + cellW * 0.5f + i * (cellW + gap), -8f);
            rt.sizeDelta = new Vector2(cellW, cellH);

            Image border = cell.AddComponent<Image>();
            border.color = BORDER_IDLE;
            border.raycastTarget = false;
            cellBorders[i] = border;

            GameObject bg = new GameObject("BG");
            RectTransform bgRt = bg.AddComponent<RectTransform>();
            bgRt.SetParent(rt, false);
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(2f, 2f); bgRt.offsetMax = new Vector2(-2f, -2f);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = BG_IDLE;
            bgImg.raycastTarget = false;
            cellBGs[i] = bgImg;

            Text label = KitchenEventManager.MakeText(bgRt, "Label",
                GameBalance.CarNames[i], 15, TEXT_IDLE);
            RectTransform lRt = label.rectTransform;
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
            lRt.offsetMin = Vector2.zero; lRt.offsetMax = Vector2.zero;
            cellTexts[i] = label;
        }
    }

    // ─────────────────────────────────────────────
    // 매 프레임: 칸 상태 갱신
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (cellBorders == null) return;

        if (chefTransform == null)
        {
            GameObject chefObj = GameObject.Find("Chef");
            if (chefObj != null) chefTransform = chefObj.transform;
        }
        int chefCar = chefTransform != null ? GameBalance.CarIndexOf(chefTransform.position.x) : -1;

        // 이벤트 앵커 칸 (침입/화재/고장이 터진 곳)
        int eventCar = KitchenEventManager.HasAnchor && KitchenEventManager.IsActive
            ? GameBalance.CarIndexOf(KitchenEventManager.AnchorX) : -1;

        // 칸별 마비 포탑 상태 수집 (감전/빙결/과열 중 대표 1개)
        string[] stunKinds = new string[cellBorders.Length];
        if (TurretSlotManager.Instance != null)
        {
            for (int i = 0; i < TurretSlotManager.Instance.slots.Length; i++)
            {
                TurretSlot s = TurretSlotManager.Instance.slots[i];
                if (s == null || !s.IsStunned) continue;
                int car = GameBalance.CarIndexOf(s.transform.position.x);
                if (car >= 0 && car < stunKinds.Length) stunKinds[car] = s.StunKind;
            }
        }

        float pulse = 0.55f + Mathf.PingPong(Time.unscaledTime * 1.6f, 0.45f);   // 점멸

        for (int i = 0; i < cellBorders.Length; i++)
        {
            string label = GameBalance.CarNames[i];
            Color bg = BG_IDLE, text = TEXT_IDLE;

            if (stunKinds[i] != null)
            {
                // 마비 포탑이 있는 칸
                label = GameBalance.CarNames[i] + " " + stunKinds[i] + "!";
                if (stunKinds[i] == "빙결") { bg = new Color(0.06f, 0.16f, 0.24f, 0.9f); text = new Color(0.65f, 0.9f, 1f); }
                else if (stunKinds[i] == "과열") { bg = new Color(0.26f, 0.09f, 0.03f, 0.9f); text = new Color(1f, 0.62f, 0.35f); }
                else { bg = new Color(0.25f, 0.22f, 0.05f, 0.9f); text = new Color(1f, 0.9f, 0.3f); }
                bg = new Color(bg.r, bg.g, bg.b, bg.a * pulse + 0.3f);
            }
            else if (i == eventCar)
            {
                // 이벤트가 터진 칸
                label = GameBalance.CarNames[i] + " !!";
                bg = new Color(0.35f, 0.14f, 0.04f, 0.55f + pulse * 0.4f);
                text = new Color(1f, 0.75f, 0.4f);
            }

            cellBGs[i].color = bg;
            cellTexts[i].text = label;
            cellTexts[i].color = text;
            cellBorders[i].color = i == chefCar ? BORDER_CHEF : BORDER_IDLE;
        }
    }
}
