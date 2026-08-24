using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// [SlotMarkerUI.cs] v2
/// 슬롯 8개 위치에 화면 마커 표시 (월드 따라다님)
/// - 좌클릭(투입 모드): 요리 투입
/// - 좌클릭(평시): 합체 선택 -> 다른 포탑 클릭 = 합체 (기획 B-3)
///   같은 요리 = 레벨 합산 / 다른 T1 = T2 진화. 재클릭/ESC = 취소
/// - 우클릭: 폐기 (재료 환급)
/// - 호버: 성능 툴팁
/// - v2 변경점: 잠금 슬롯 표시 / v3 변경점: 합체 진화 조작
/// GameSystems 오브젝트에 부착
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class SlotMarkerUI : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform[] markers = new RectTransform[8];
    private Image[] markerBorders = new Image[8];
    private Image[] markerBGs = new Image[8];
    private Text[] markerTexts = new Text[8];
    private RectTransform tooltip;
    private Text tooltipText;
    private int hoverIndex = -1;

    // 합체 선택 상태 (-1 = 선택 없음)
    private int mergeSelectIndex = -1;
    private RectTransform mergeBanner;
    private Text mergeBannerText;

    /// <summary>합체 선택 중인지 (PauseMenu가 ESC 용도 판별에 사용)</summary>
    public static bool MergeSelecting { get; private set; }

    private static readonly Color BG_NORMAL = new Color(0.12f, 0.075f, 0.05f, 0.9f);
    private static readonly Color BG_LOCKED = new Color(0.05f, 0.04f, 0.03f, 0.85f);
    private static readonly Color BORDER_LOCKED = new Color(0.3f, 0.26f, 0.22f);

    void Start()
    {
        canvas = UIFactory.CreateCanvas("SlotMarker_Canvas", 9); // HUD보다 아래

        for (int i = 0; i < 8; i++)
            CreateMarker(i);

        // 툴팁 (맨 위 표시)
        RectTransform tipPanel = UIFactory.CreatePanel(canvas.transform, "Tooltip",
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            Vector2.zero, new Vector2(340f, 150f),
            new Color(0.09f, 0.05f, 0.03f, 0.96f), UIFactory.GOLD, 2f);
        tooltipText = UIFactory.CreateText(tipPanel, "Text", "", 16, UIFactory.CREAM, TextAnchor.UpperLeft);
        tooltipText.rectTransform.offsetMin = new Vector2(10f, 8f);
        tooltipText.rectTransform.offsetMax = new Vector2(-10f, -8f);
        tooltip = tipPanel;
        tooltip.gameObject.SetActive(false);

        // 합체 안내 배너 (상단 중앙, 투입 배너보다 아래)
        mergeBanner = UIFactory.CreatePanel(canvas.transform, "MergeBanner",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-370f, -148f), new Vector2(370f, -104f),
            new Color(0.14f, 0.085f, 0.05f, 0.95f), UIFactory.T2PINK, 2f);
        mergeBannerText = UIFactory.CreateText(mergeBanner, "Text", "", 18, UIFactory.CREAM, TextAnchor.MiddleCenter);
        mergeBanner.gameObject.SetActive(false);
    }

    void Update()
    {
        // ESC = 합체 선택 취소
        if (mergeSelectIndex >= 0 && Input.GetKeyDown(KeyCode.Escape))
            SetMergeSelect(-1);

        // 투입 모드가 켜지면 합체 선택 해제 (조작 충돌 방지)
        if (mergeSelectIndex >= 0 && GameHUD.Instance != null &&
            !string.IsNullOrEmpty(GameHUD.Instance.placingRecipeId))
            SetMergeSelect(-1);
    }

    /// <summary>합체 선택 상태 변경 + 배너 갱신</summary>
    private void SetMergeSelect(int index)
    {
        mergeSelectIndex = index;
        bool on = index >= 0;
        MergeSelecting = on;
        mergeBanner.gameObject.SetActive(on);
        if (on)
        {
            TurretSlot s = TurretSlotManager.Instance.slots[index];
            string name = s != null && !s.IsEmpty ? s.Recipe.displayName : "?";
            mergeBannerText.text = "[합체] " + name + " 선택 - 합칠 포탑 클릭!\n같은 요리 = 레벨 합산 / 다른 T1 = T2 진화  (재클릭/ESC 취소)";
        }
    }

    private void CreateMarker(int index)
    {
        GameObject go = new GameObject("SlotMarker_" + index);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        rt.sizeDelta = new Vector2(96f, 52f);
        markers[index] = rt;

        Image border = go.AddComponent<Image>();
        border.color = UIFactory.DIM;
        markerBorders[index] = border;

        GameObject bg = new GameObject("BG");
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.SetParent(rt, false);
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = new Vector2(3f, 3f);
        bgRt.offsetMax = new Vector2(-3f, -3f);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = BG_NORMAL;
        bgImg.raycastTarget = false;
        markerBGs[index] = bgImg;

        Text label = UIFactory.CreateText(bgRt, "Label", "+", 15, UIFactory.CREAM, TextAnchor.MiddleCenter);
        markerTexts[index] = label;

        // 클릭/호버 핸들러
        SlotMarkerHandler handler = go.AddComponent<SlotMarkerHandler>();
        handler.Init(this, index);
    }

    void LateUpdate()
    {
        if (TurretSlotManager.Instance == null || Camera.main == null) return;

        for (int i = 0; i < 8; i++)
        {
            TurretSlot slot = TurretSlotManager.Instance.slots[i];
            if (slot == null) { markers[i].gameObject.SetActive(false); continue; }

            // 월드 -> 스크린 좌표 (마커가 슬롯을 따라다님)
            Vector3 screen = Camera.main.WorldToScreenPoint(slot.transform.position);
            markers[i].gameObject.SetActive(screen.z > 0f);
            markers[i].position = screen;

            // 상태 표시
            if (slot.isLocked)
            {
                // 잠금 슬롯: 어두운 색 + 자물쇠 문구
                markerTexts[i].text = "잠김\n(증강 해금)";
                markerTexts[i].color = new Color(0.5f, 0.45f, 0.4f);
                markerBorders[i].color = BORDER_LOCKED;
                markerBGs[i].color = BG_LOCKED;
            }
            else if (slot.IsEmpty)
            {
                markerTexts[i].text = "+";
                markerTexts[i].color = UIFactory.CREAM;
                markerBGs[i].color = BG_NORMAL;
                // 투입 모드일 때 금색 강조
                markerBorders[i].color = string.IsNullOrEmpty(GameHUD.Instance != null ? GameHUD.Instance.placingRecipeId : "")
                    ? UIFactory.DIM : UIFactory.GOLD;
            }
            else if (slot.IsStunned)
            {
                // v3: 보스 낙뢰에 감전된 포탑 - 클릭 한 번으로 재가동
                RecipeData rs = slot.Recipe;
                markerTexts[i].text = rs.displayName + "\n[감전! 클릭 재가동]";
                markerTexts[i].color = new Color(1f, 0.9f, 0.3f);
                markerBGs[i].color = new Color(0.25f, 0.22f, 0.05f, 0.85f);
                markerBorders[i].color = new Color(1f, 0.85f, 0.2f);
            }
            else
            {
                RecipeData r = slot.Recipe;
                markerTexts[i].text = r.displayName + "\n" + slot.GradeName + " Lv" + slot.level;
                markerTexts[i].color = UIFactory.CREAM;
                markerBGs[i].color = BG_NORMAL;

                // 합체 선택된 슬롯은 금색 강조
                if (i == mergeSelectIndex)
                    markerBorders[i].color = UIFactory.GOLD;
                else
                    markerBorders[i].color = r.tier == 2 ? UIFactory.T2PINK : UIFactory.GradeColor(slot.GradeName);
            }
        }

        // 툴팁 위치 (마우스 따라감)
        if (tooltip.gameObject.activeSelf)
        {
            Vector2 pos = (Vector2)Input.mousePosition + new Vector2(20f, -20f);
            // 화면 밖 방지
            if (pos.x + 340f > Screen.width) pos.x = Screen.width - 350f;
            if (pos.y - 150f < 0f) pos.y = 160f;
            tooltip.position = pos;
        }
    }

    // ── SlotMarkerHandler에서 호출 ──
    public void OnMarkerClick(int index, PointerEventData.InputButton button)
    {
        TurretSlot slot = TurretSlotManager.Instance != null ? TurretSlotManager.Instance.slots[index] : null;
        if (slot == null) return;

        // 잠금 슬롯은 안내만
        if (slot.isLocked)
        {
            UIManager.Instance?.ShowStatChange("잠긴 슬롯! 증강 [증축된 주방 칸]으로 해금");
            return;
        }

        // v3: 감전(낙뢰 마비) 해제가 모든 클릭보다 우선 - 한 번 클릭으로 재가동
        if (slot.IsStunned)
        {
            slot.ClearStun();
            UIManager.Instance?.ShowStatChange("포탑 재가동! (감전 해제)");
            return;
        }

        if (button == PointerEventData.InputButton.Left)
        {
            // 투입 모드면 기존대로 요리 투입
            bool placing = GameHUD.Instance != null && !string.IsNullOrEmpty(GameHUD.Instance.placingRecipeId);
            if (placing)
            {
                GameHUD.Instance.OnSlotClicked(slot);
                return;
            }

            // 평시 좌클릭 = 합체 조작 (기획 B-3)
            if (slot.IsEmpty) { SetMergeSelect(-1); return; }

            if (mergeSelectIndex < 0)
            {
                SetMergeSelect(index);            // 첫 번째 포탑 선택
            }
            else if (mergeSelectIndex == index)
            {
                SetMergeSelect(-1);               // 재클릭 = 취소
            }
            else
            {
                // 두 번째 포탑 클릭 = 합체 시도
                string msg;
                bool ok = TurretSlotManager.Instance.TryMergeSlots(mergeSelectIndex, index, out msg);
                UIManager.Instance?.ShowStatChange(ok ? msg : "합체 실패: " + msg);
                SetMergeSelect(-1);
                HideTooltip();
            }
        }
        else if (button == PointerEventData.InputButton.Right)
        {
            // 우클릭 폐기 + 재료 환급
            if (slot.IsEmpty) return;
            if (mergeSelectIndex == index) SetMergeSelect(-1);   // 선택 중이던 포탑 폐기 시 해제
            int refund = slot.Scrap();
            for (int k = 0; k < refund; k++)
                MaterialInventory.Instance.Add((MaterialType)Random.Range(0, 6), 1);
            HideTooltip();
        }
    }

    public void OnMarkerEnter(int index)
    {
        hoverIndex = index;
        TurretSlot slot = TurretSlotManager.Instance != null ? TurretSlotManager.Instance.slots[index] : null;
        if (slot == null || slot.IsEmpty || slot.isLocked) { HideTooltip(); return; }

        RecipeData r = slot.Recipe;
        string roleStr = RoleName(r.role);
        string shapeStr = ShapeName(r.shape);
        string dtypeStr = r.damageType == DamageType.Magic ? "마법" : "물리";

        string info = r.displayName + (r.tier == 2 ? "  [T2 전설]" : "") + "\n";
        info += slot.GradeName + "등급 Lv" + slot.level + "  x" + slot.LevelMult.ToString("F1") + "배\n";
        info += roleStr + " / " + shapeStr;
        if (r.damage > 0f)
        {
            info += " / " + dtypeStr + "\n";
            float dmg = r.damage * slot.LevelMult;
            info += "공격 " + dmg.ToString("F0") + "  쿨 " + r.cooldown.ToString("F2") + "s";
            info += "  DPS " + (dmg / r.cooldown).ToString("F1") + "\n";
        }
        else info += "\n";
        info += r.description + "\n";
        info += "(우클릭: 폐기, 재료 " + Mathf.Max(1, slot.level) + "개 환급)";

        tooltipText.text = info;
        tooltip.gameObject.SetActive(true);
    }

    public void OnMarkerExit(int index)
    {
        if (hoverIndex == index) HideTooltip();
    }

    private void HideTooltip()
    {
        hoverIndex = -1;
        tooltip.gameObject.SetActive(false);
    }

    private string RoleName(TurretRole role)
    {
        switch (role)
        {
            case TurretRole.PhysDealer: return "물리 딜러";
            case TurretRole.MagicDealer: return "마법 딜러";
            case TurretRole.Debuffer: return "디버퍼";
            case TurretRole.Buffer: return "버퍼";
            case TurretRole.CC: return "CC";
            default: return "서포트";
        }
    }

    private string ShapeName(AttackShape shape)
    {
        switch (shape)
        {
            case AttackShape.Projectile: return "단일 투사체";
            case AttackShape.Pierce: return "관통 레일";
            case AttackShape.Cone: return "부채꼴 방사";
            case AttackShape.Explode: return "착탄 폭발";
            case AttackShape.Chain: return "체인";
            case AttackShape.Field: return "장판";
            case AttackShape.Aura: return "오라";
            default: return "상시";
        }
    }
}

/// <summary>마커 1개의 클릭/호버 이벤트 수신기</summary>
public class SlotMarkerHandler : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private SlotMarkerUI owner;
    private int index;

    public void Init(SlotMarkerUI ui, int idx)
    {
        owner = ui;
        index = idx;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        owner.OnMarkerClick(index, eventData.button);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        owner.OnMarkerEnter(index);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner.OnMarkerExit(index);
    }
}
