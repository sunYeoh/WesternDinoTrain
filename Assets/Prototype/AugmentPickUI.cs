using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// [AugmentPickUI.cs] v1.1
/// 웨이브 클리어 시 뜨는 증강 3택1 화면 (기획 C)
/// - v1.1: '행운의 부적'(선택지 +1) / '야전 정비반'(웨이브당 최대 HP 성장) 반영
///
/// 사용법
/// - "GameSystems" 오브젝트에 이 스크립트를 추가 (UI는 코드로 자동 생성)
/// - 웨이브가 끝나는 시점에 AugmentPickUI.Instance.OnWaveCleared(웨이브번호) 를 호출하면 열린다
/// - 테스트용으로 F12를 누르면 즉시 열린다
///
/// 카드를 고를 때까지 게임은 일시정지(Time.timeScale = 0)된다.
///
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class AugmentPickUI : MonoBehaviour
{
    public static AugmentPickUI Instance;

    /// <summary>선택 화면이 열려 있는지 (다른 시스템에서 입력 차단용으로 확인)</summary>
    public static bool IsOpen
    {
        get { return Instance != null && Instance.isOpen; }
    }

    [Header("설정")]
    public int cardCount = 3;              // 제시할 카드 수
    public bool resetRunOnStart = true;    // 씬 시작 시 증강 초기화
    public bool pauseWhilePicking = true;  // 선택 중 게임 일시정지

    [Header("디버그")]
    public bool debugKeyEnabled = true;    // F12로 강제 오픈 (빌드 전 false)

    private bool isOpen;
    private int currentWave;
    private Canvas canvas;
    private RectTransform dimRoot;         // 전체 화면 어두운 배경
    private RectTransform cardArea;        // 카드가 붙는 영역
    private Text headerText;
    private Text ownedText;                // 현재 보유 증강 목록

    // v1.2: 숫자키 선택용 - 현재 제시 중인 카드 목록
    private List<AugmentData> currentCards = new List<AugmentData>();

    /// <summary>선택이 끝난 뒤 실행할 동작 (WaveManager에서 다음 웨이브 시작에 사용)</summary>
    private System.Action onClosed;

    void Awake()
    {
        Instance = this;
        if (resetRunOnStart) AugmentManager.ResetRun();
        BuildUI();
        dimRoot.gameObject.SetActive(false);
    }

    void Update()
    {
        if (debugKeyEnabled && Input.GetKeyDown(KeyCode.F12) && !isOpen)
            Open(currentWave + 1, null);

        // v1.2 (감사): 숫자키로 카드 선택 (1~5), 0 = 건너뛰기
        if (isOpen)
        {
            for (int i = 0; i < currentCards.Count && i < 5; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    Pick(currentCards[i]);
                    return;
                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha0))
                Skip();
        }
    }

    /// <summary>v1.2 (감사 2-A): 이번 증강을 건너뛰고 명성으로 환급</summary>
    private void Skip()
    {
        MetaProgress.AddFame(GameBalance.AugmentSkipFame);
        UIManager.Instance?.ShowStatChange("증강 건너뛰기 - 명성 +" + GameBalance.AugmentSkipFame);
        Debug.Log("[증강] 건너뛰기 (+" + GameBalance.AugmentSkipFame + " 명성)");
        Close();
    }

    // ==================================================================
    //  외부에서 호출하는 진입점
    // ==================================================================

    /// <summary>웨이브 클리어 처리 - 회복 증강 적용 후 증강 선택창을 연다</summary>
    public void OnWaveCleared(int waveNumber)
    {
        OnWaveCleared(waveNumber, null);
    }

    /// <summary>웨이브 클리어 처리 (선택 완료 후 실행할 동작을 함께 지정)</summary>
    public void OnWaveCleared(int waveNumber, System.Action afterPick)
    {
        // '응급 정비' 계열 증강 효과: 웨이브마다 자동 회복
        if (AugmentManager.HealPerWave > 0f)
            AugmentManager.HealTrain(AugmentManager.HealPerWave);

        // '야전 정비반' 증강 효과: 웨이브마다 최대 HP 영구 증가
        if (AugmentManager.MaxHPPerWave > 0f)
        {
            AugmentManager.AddTrainMaxHP(AugmentManager.MaxHPPerWave);
            AugmentManager.HealTrain(AugmentManager.MaxHPPerWave);
        }

        Open(waveNumber, afterPick);
    }

    /// <summary>증강 선택창 열기</summary>
    public void Open(int waveNumber, System.Action afterPick)
    {
        if (isOpen) return;

        isOpen = true;
        currentWave = waveNumber;
        onClosed = afterPick;

        // '행운의 부적' 증강 효과: 선택지 수 증가 (최대 5장)
        int count = Mathf.Min(cardCount + AugmentManager.ExtraCards, 5);
        List<AugmentData> rolled = AugmentDatabase.Roll(waveNumber, count);
        currentCards = rolled;   // v1.2: 숫자키 선택용 보관
        BuildCards(rolled);

        headerText.text = "웨이브 " + waveNumber + " 클리어!   증강 선택 [1~" + rolled.Count + "]  /  건너뛰기 [0]";
        ownedText.text = BuildOwnedSummary();

        dimRoot.gameObject.SetActive(true);
        if (pauseWhilePicking) Time.timeScale = 0f;

        Debug.Log("[증강] 선택창 오픈 - 웨이브 " + waveNumber + " / 후보 " + rolled.Count + "개");
    }

    /// <summary>카드를 골랐을 때</summary>
    private void Pick(AugmentData aug)
    {
        AugmentManager.Acquire(aug);
        Close();
    }

    private void Close()
    {
        isOpen = false;
        dimRoot.gameObject.SetActive(false);
        if (pauseWhilePicking) Time.timeScale = 1f;

        System.Action cb = onClosed;
        onClosed = null;
        if (cb != null) cb();
    }

    // ==================================================================
    //  카드 생성
    // ==================================================================

    private void BuildCards(List<AugmentData> list)
    {
        // 이전 카드 정리
        for (int i = cardArea.childCount - 1; i >= 0; i--)
            Destroy(cardArea.GetChild(i).gameObject);

        float cardW = 330f;
        float cardH = 430f;
        float gap = 40f;
        float totalW = list.Count * cardW + (list.Count - 1) * gap;
        float startX = -totalW * 0.5f + cardW * 0.5f;

        for (int i = 0; i < list.Count; i++)
        {
            AugmentData aug = list[i];
            Color gc = aug.GradeColor();

            // 카드 본체 (등급색 테두리 역할)
            RectTransform card = KitchenEventManager.MakeBox(cardArea, "Card" + i, gc);
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = new Vector2(startX + i * (cardW + gap), 0f);
            card.sizeDelta = new Vector2(cardW, cardH);

            // 안쪽 어두운 배경
            RectTransform inner = KitchenEventManager.MakeBox(card, "Inner", new Color(0.10f, 0.09f, 0.08f, 0.98f));
            inner.anchorMin = Vector2.zero;
            inner.anchorMax = Vector2.one;
            inner.offsetMin = new Vector2(4f, 4f);
            inner.offsetMax = new Vector2(-4f, -4f);
            inner.GetComponent<Image>().raycastTarget = false;

            // 등급 띠
            RectTransform band = KitchenEventManager.MakeBox(inner, "Band", gc);
            band.anchorMin = new Vector2(0f, 1f);
            band.anchorMax = new Vector2(1f, 1f);
            band.pivot = new Vector2(0.5f, 1f);
            band.offsetMin = new Vector2(0f, 0f);
            band.offsetMax = new Vector2(0f, 0f);
            band.sizeDelta = new Vector2(0f, 46f);
            band.GetComponent<Image>().raycastTarget = false;

            Text gradeTxt = KitchenEventManager.MakeText(band, "Grade", aug.GradeName(), 24, new Color(0.08f, 0.07f, 0.06f));
            StretchFull(gradeTxt.rectTransform);

            // 증강 이름 (패밀리 태그가 있으면 함께 표시)
            string displayName = aug.family != null ? aug.name + " [" + aug.family + "]" : aug.name;
            Text nameTxt = KitchenEventManager.MakeText(inner, "Name", displayName, 30, gc);
            RectTransform nRt = nameTxt.rectTransform;
            nRt.anchorMin = new Vector2(0f, 1f);
            nRt.anchorMax = new Vector2(1f, 1f);
            nRt.pivot = new Vector2(0.5f, 1f);
            nRt.anchoredPosition = new Vector2(0f, -70f);
            nRt.sizeDelta = new Vector2(-24f, 80f);
            nameTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

            // 설명
            Text descTxt = KitchenEventManager.MakeText(inner, "Desc", aug.desc, 21, new Color(0.88f, 0.88f, 0.84f));
            RectTransform dRt = descTxt.rectTransform;
            dRt.anchorMin = new Vector2(0f, 0f);
            dRt.anchorMax = new Vector2(1f, 1f);
            dRt.offsetMin = new Vector2(20f, 90f);
            dRt.offsetMax = new Vector2(-20f, -160f);
            descTxt.alignment = TextAnchor.UpperCenter;
            descTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            descTxt.verticalOverflow = VerticalWrapMode.Truncate;

            // 선택 버튼
            AugmentData captured = aug;   // 클로저 캡처용 지역 변수 (C# 7.3 필수)
            Button pickBtn = KitchenEventManager.MakeButton(
                inner, "선택", new Color(gc.r * 0.45f, gc.g * 0.45f, gc.b * 0.45f, 1f),
                new Vector2(0f, -cardH * 0.5f + 52f), new Vector2(cardW - 60f, 62f));
            pickBtn.onClick.AddListener(delegate { Pick(captured); });
        }
    }

    /// <summary>현재 보유 증강을 한 줄 요약으로 만든다</summary>
    private string BuildOwnedSummary()
    {
        if (AugmentManager.Owned.Count == 0) return "보유 증강: 없음";

        string s = "보유 증강 (" + AugmentManager.Owned.Count + "): ";
        for (int i = 0; i < AugmentManager.Owned.Count; i++)
        {
            if (i > 0) s += " / ";
            s += AugmentManager.Owned[i].name;
        }
        return s;
    }

    // ==================================================================
    //  UI 뼈대 생성
    // ==================================================================

    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("AugmentCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;   // 주방 이벤트(500)보다 위
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // 전체 화면 암전 (뒤쪽 클릭 차단 역할도 한다)
        dimRoot = KitchenEventManager.MakeBox(canvasGo.transform, "Dim", new Color(0f, 0f, 0f, 0.82f));
        dimRoot.anchorMin = Vector2.zero;
        dimRoot.anchorMax = Vector2.one;
        dimRoot.offsetMin = Vector2.zero;
        dimRoot.offsetMax = Vector2.zero;

        // 상단 제목
        headerText = KitchenEventManager.MakeText(dimRoot, "Header", "", 40, new Color(1f, 0.82f, 0.36f));
        RectTransform hRt = headerText.rectTransform;
        hRt.anchorMin = new Vector2(0.5f, 1f);
        hRt.anchorMax = new Vector2(0.5f, 1f);
        hRt.pivot = new Vector2(0.5f, 1f);
        hRt.anchoredPosition = new Vector2(0f, -90f);
        hRt.sizeDelta = new Vector2(1400f, 60f);

        // 카드 영역
        cardArea = KitchenEventManager.MakeBox(dimRoot, "CardArea", new Color(0f, 0f, 0f, 0f));
        cardArea.anchorMin = new Vector2(0.5f, 0.5f);
        cardArea.anchorMax = new Vector2(0.5f, 0.5f);
        cardArea.anchoredPosition = new Vector2(0f, 10f);
        cardArea.sizeDelta = new Vector2(1500f, 500f);
        cardArea.GetComponent<Image>().raycastTarget = false;

        // 하단 보유 증강 목록
        ownedText = KitchenEventManager.MakeText(dimRoot, "Owned", "", 20, new Color(0.75f, 0.75f, 0.72f));
        RectTransform oRt = ownedText.rectTransform;
        oRt.anchorMin = new Vector2(0.5f, 0f);
        oRt.anchorMax = new Vector2(0.5f, 0f);
        oRt.pivot = new Vector2(0.5f, 0f);
        oRt.anchoredPosition = new Vector2(0f, 60f);
        oRt.sizeDelta = new Vector2(1600f, 60f);
        ownedText.horizontalOverflow = HorizontalWrapMode.Wrap;

        // v1.2 (감사 2-A): 건너뛰기 버튼 - 억지 선택 방지 + 명성 미세 수급
        Button skipBtn = KitchenEventManager.MakeButton(dimRoot,
            "건너뛴다 (+" + GameBalance.AugmentSkipFame + " 명성)  [0]",
            new Color(0.28f, 0.26f, 0.22f), Vector2.zero, new Vector2(320f, 52f));
        RectTransform sRt = skipBtn.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0.5f, 0f);
        sRt.anchorMax = new Vector2(0.5f, 0f);
        sRt.pivot = new Vector2(0.5f, 0f);
        sRt.anchoredPosition = new Vector2(0f, 140f);
        skipBtn.onClick.AddListener(Skip);

        // v1.2: 보유 증강 목록 패널 자동 생성 (V키 열람 - 씬 세팅 불필요)
        if (FindFirstObjectByType<AugmentListUI>() == null)
        {
            GameObject listGo = new GameObject("AugmentListUI");
            listGo.AddComponent<AugmentListUI>();
        }
    }

    /// <summary>부모 영역에 꽉 채우기</summary>
    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
