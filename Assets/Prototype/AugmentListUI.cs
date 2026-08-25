using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [AugmentListUI.cs] v2 - 감사 3-B 결정 사항 + Phase 2-3 아이템 표시
/// 보유 증강 + 아이템(유물) 목록 패널 - V키로 열고 닫는다.
/// "내가 지금 뭘 골랐더라?"를 언제든 확인 (레벨 시스템 절단의 대체 정보창).
/// - v2: 증강 목록 아래에 보유 아이템도 이어서 표시 (희귀도 색)
///
/// 사용법: 없음! AugmentPickUI가 시작 시 자동 생성한다. 파일만 넣으면 끝.
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class AugmentListUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    private GameObject canvasGo;
    private GameObject root;
    private RectTransform listArea;
    private Text titleText;

    private void Start()
    {
        BuildUI();
        root.SetActive(false);
        IsOpen = false;
    }

    private void OnDestroy()
    {
        if (canvasGo != null) Destroy(canvasGo);
        IsOpen = false;
    }

    private void Update()
    {
        // V키 토글 (조리 미니게임/증강 선택 중에는 열지 않음 - 화면 겹침 방지)
        if (Input.GetKeyDown(KeyCode.V))
        {
            if (IsOpen) Close();
            else if (!CookingMinigame.IsActive && !AugmentPickUI.IsOpen && !PauseMenu.IsOpen)
                Open();
        }

        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    private void Open()
    {
        RefreshList();
        root.SetActive(true);
        IsOpen = true;
    }

    private void Close()
    {
        root.SetActive(false);
        IsOpen = false;
    }

    // ─────────────────────────────────────────────
    // 목록 갱신 (열 때마다 다시 그린다)
    // ─────────────────────────────────────────────
    private void RefreshList()
    {
        // 이전 행 정리
        for (int i = listArea.childCount - 1; i >= 0; i--)
            Destroy(listArea.GetChild(i).gameObject);

        // 같은 증강 중첩은 xN으로 묶는다
        List<AugmentData> unique = new List<AugmentData>();
        List<int> counts = new List<int>();
        for (int i = 0; i < AugmentManager.Owned.Count; i++)
        {
            AugmentData a = AugmentManager.Owned[i];
            int found = -1;
            for (int u = 0; u < unique.Count; u++)
                if (unique[u].id == a.id) { found = u; break; }

            if (found >= 0) counts[found]++;
            else { unique.Add(a); counts.Add(1); }
        }

        titleText.text = "보유 증강 (" + AugmentManager.Owned.Count + ") / 아이템 ("
            + ItemManager.OwnedCount + ")   [V] 닫기";

        if (unique.Count == 0 && ItemManager.OwnedCount == 0)
        {
            Text empty = KitchenEventManager.MakeText(listArea, "Empty",
                "아직 획득한 증강도 아이템도 없다", 22, new Color(0.6f, 0.58f, 0.52f));
            RectTransform eRt = empty.rectTransform;
            eRt.anchorMin = new Vector2(0.5f, 1f);
            eRt.anchorMax = new Vector2(0.5f, 1f);
            eRt.pivot = new Vector2(0.5f, 1f);
            eRt.anchoredPosition = new Vector2(0f, -40f);
            eRt.sizeDelta = new Vector2(600f, 30f);
            return;
        }

        // 2열 배치: 증강 먼저, 이어서 아이템 (같은 그리드에 계속 채운다)
        int slot = 0;
        for (int i = 0; i < unique.Count; i++)
        {
            AugmentData a = unique[i];
            string label = a.name
                + (a.family != null ? " [" + a.family + "]" : "")
                + (counts[i] > 1 ? "  x" + counts[i] : "");
            MakeRow(slot++, label, a.GradeColor(), a.desc);
        }

        // Phase 2-3: 보유 아이템(유물) - 희귀도 색 + [아이템] 접두
        for (int i = 0; i < ItemManager.Owned.Count; i++)
        {
            ItemData it = ItemManager.Owned[i];
            MakeRow(slot++, "[아이템] " + it.name, it.RarityColor(), it.desc);
        }
    }

    /// <summary>목록 행 1개 생성 (증강/아이템 공용)</summary>
    private void MakeRow(int index, string label, Color labelColor, string desc)
    {
        float rowH = 58f;
        int col = index % 2;
        int rowIdx = index / 2;

        RectTransform row = KitchenEventManager.MakeBox(listArea, "Row" + index,
            new Color(0.15f, 0.13f, 0.11f, 0.9f));
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(0f, 1f);
        row.pivot = new Vector2(0f, 1f);
        row.anchoredPosition = new Vector2(14f + col * 462f, -8f - rowIdx * (rowH + 6f));
        row.sizeDelta = new Vector2(450f, rowH);

        // 이름 (등급/희귀도 색)
        Text nameTxt = KitchenEventManager.MakeText(row, "Name", label, 19, labelColor);
        nameTxt.alignment = TextAnchor.MiddleLeft;
        RectTransform nRt = nameTxt.rectTransform;
        nRt.anchorMin = new Vector2(0f, 0.5f);
        nRt.anchorMax = new Vector2(1f, 0.5f);
        nRt.pivot = new Vector2(0.5f, 0.5f);
        nRt.anchoredPosition = new Vector2(0f, 13f);
        nRt.offsetMin = new Vector2(12f, nRt.offsetMin.y);
        nRt.offsetMax = new Vector2(-12f, nRt.offsetMax.y);
        nRt.sizeDelta = new Vector2(nRt.sizeDelta.x, 26f);

        // 설명 (작게, 한 줄 잘림 허용)
        Text descTxt = KitchenEventManager.MakeText(row, "Desc", desc, 15,
            new Color(0.72f, 0.7f, 0.65f));
        descTxt.alignment = TextAnchor.MiddleLeft;
        descTxt.verticalOverflow = VerticalWrapMode.Truncate;
        RectTransform dRt = descTxt.rectTransform;
        dRt.anchorMin = new Vector2(0f, 0.5f);
        dRt.anchorMax = new Vector2(1f, 0.5f);
        dRt.pivot = new Vector2(0.5f, 0.5f);
        dRt.anchoredPosition = new Vector2(0f, -13f);
        dRt.offsetMin = new Vector2(12f, dRt.offsetMin.y);
        dRt.offsetMax = new Vector2(-12f, dRt.offsetMax.y);
        dRt.sizeDelta = new Vector2(dRt.sizeDelta.x, 24f);
    }

    // ─────────────────────────────────────────────
    // UI 생성
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        canvasGo = new GameObject("AugmentListCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 585;   // 정비소(550)와 분기선로(590) 사이
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform panel = KitchenEventManager.MakeBox(canvasGo.transform, "AugListPanel",
            new Color(0.08f, 0.07f, 0.06f, 0.95f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = new Vector2(0f, 10f);
        panel.sizeDelta = new Vector2(960f, 640f);
        root = panel.gameObject;

        titleText = KitchenEventManager.MakeText(panel, "Title", "보유 증강", 28,
            new Color(1f, 0.78f, 0.32f));
        RectTransform tRt = titleText.rectTransform;
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -10f);
        tRt.sizeDelta = new Vector2(0f, 36f);

        // 목록 영역
        RectTransform area = KitchenEventManager.MakeBox(panel, "ListArea", new Color(0f, 0f, 0f, 0f));
        area.anchorMin = new Vector2(0f, 0f);
        area.anchorMax = new Vector2(1f, 1f);
        area.offsetMin = new Vector2(6f, 12f);
        area.offsetMax = new Vector2(-6f, -56f);
        area.GetComponent<Image>().raycastTarget = false;
        listArea = area;
    }
}
