using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// [AugmentPickUI.cs] v1.14 (v9.35.5 2026-10-26 유저 "증강설명이 너무 길다": 카드 글이 짧아졌다 (AugmentDatabase). 상태 줄(보유 · 공속 상한 · 맨 아래 줄)은 설명 밑에 빈 줄 하나 두고 몰아서 붙인다 - 서로 사이 빈 줄 없음. 보유 줄 "[보유 2개 · 지금 피해 +24%]") / v1.13 (v9.35.2 2026-10-26 유저 "어떤 종류에 적용이 되는지 알아야 집든말든하지": 설명 맨 아래 AugmentTargets.Note - 지금 내 포탑 중 몇 기에 걸리나 / 설명이 칸을 넘치면 글자를 줄여 다 보이게 (best fit)) / v1.12 (v9.25 2026-10-16 B1 증강 덧셈: 보유 줄 = "[보유 4개 - 지금 피해 +48%]" 처럼 그 종류의 지금 합계 (AugmentManager.StackSummary) / 웨이브 클리어 회복에 유물 '선대의 식탁보' (ItemManager.WaveHeal)) / v1.11 (v9.24.1 2026-10-16 5차 피드백 6번: "[보유 n개 - 이번 획득 효과 n배!]" 를 실제 실행 횟수(AugmentManager.StackTimes - 최대 3배, 점증 없는 증강은 "[보유 n개]" 만)와 맞춤. 4번째부터 "4배" 라고 적었지만 실제는 3배였다) / v1.10 (v9.18 2026-10-06: 카드 줄이 기준 화면보다 넓으면(카드 5장 = 1810) 카드 영역을 화면 안으로 줄인다 - UIFactory.FitScale) / v1.9 (v9.17 2026-10-06 C3·C4: 카드가 왼쪽부터 0.06초 간격으로 나타난다(리롤도) / 고르면 그 카드만 0.15초 밝게 남고 나머지는 흐려진 뒤 창이 0.95배로 줄며 사라진다 - 그동안 입력은 받지 않는다) / v1.8 (v9.16 2026-09-29 손맛 2차 - 소리: 증강 고르면 sfx_augment_pick, 건너뛰면 sfx_ui_close) / v1.7 (v9.13.1 2026-09-24: F12 강제 오픈을 GameBalance.CheatsAllowed 로 - 빌드에서 꺼진다) / v1.6 (v9.13 2026-09-23: 선로 보상 "증강 1회 더" - OpenExtra(등급 고정, 머리글 "[선로 보상] ..."). 리롤도 그 등급) / v1.5 (v9.12 2026-09-22: 리롤 복원 - 유저 "게이머는 아는 말") / v1.4 (v9.11.1 2026-09-22 문구: 리롤 -> 다시 뽑기) / v1.3 (v9.11 2026-09-22: 등장 연출 ModalFeel) / v1.2 (v9.10 2026-09-17: 증강 선택이 매 웨이브가 아니게 되면서(GameBalance.AugmentPickAt) 선택창을 안 여는 웨이브에도 웨이브 회복·최대 HP 효과는 적용 - ApplyPerWaveEffects) / v1.1
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
    public bool debugKeyEnabled = true;    // F12로 강제 오픈 (에디터 전용 - 빌드는 CheatsAllowed 가 막는다)

    private bool isOpen;
    private int currentWave;
    private Canvas canvas;
    private RectTransform dimRoot;         // 전체 화면 어두운 배경
    private RectTransform cardArea;        // 카드가 붙는 영역
    private Text headerText;
    private Text ownedText;                // 현재 보유 증강 목록

    // v1.2: 숫자키 선택용 - 현재 제시 중인 카드 목록
    private List<AugmentData> currentCards = new List<AugmentData>();
    private readonly List<RectTransform> cardRects = new List<RectTransform>();   // v1.9: 화면의 카드 (currentCards 와 같은 순서)
    private bool closing = false;                                                  // v1.9: 퇴장 연출 중 - 입력을 받지 않는다

    /// <summary>선택이 끝난 뒤 실행할 동작 (WaveManager에서 다음 웨이브 시작에 사용)</summary>
    private System.Action onClosed;

    // v1.6: 선로 보상 판 - 등급 고정 + 출처 (없으면 -1 / null)
    private int extraGrade = -1;
    private string extraSource = null;

    void Awake()
    {
        Instance = this;
        if (resetRunOnStart)
        {
            AugmentManager.ResetRun();
            ItemManager.ResetRun();   // Phase 2-3: 아이템(유물)도 런 단위 리셋
        }
        BuildUI();
        dimRoot.gameObject.SetActive(false);
    }

    void Update()
    {
        // v1.7: 빌드에서는 안 먹는다 (GameBalance.CheatsAllowed) - 테스터가 F12 를 눌러 증강을 공짜로 받던 구멍
        if (debugKeyEnabled && GameBalance.CheatsAllowed && !TutorialDirector.Active && Input.GetKeyDown(KeyCode.F12) && !isOpen)
            Open(currentWave + 1, null);

        // v1.2 (감사): 숫자키로 카드 선택 (1~5), 0 = 건너뛰기, 9 = 리롤 (Phase 2-2)
        if (isOpen && !closing)
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
            else if (Input.GetKeyDown(KeyCode.Alpha9))
                Reroll();
        }
    }

    /// <summary>v1.2 (감사 2-A): 이번 증강을 건너뛰고 명성으로 환급</summary>
    private void Skip()
    {
        if (closing) return;
        MetaProgress.AddFame(GameBalance.AugmentSkipFame);
        UIManager.Instance?.ShowStatChange("증강 건너뛰기 - 명성 +" + GameBalance.AugmentSkipFame);
        Debug.Log("[증강] 건너뛰기 (+" + GameBalance.AugmentSkipFame + " 명성)");
        SoundManager.Play("sfx_ui_close");   // v1.8
        BeginClose(null);
    }

    // ==================================================================
    //  Phase 2-2: 리롤 - 골드를 태워 카드 3장을 새로 뽑는다 (쓸수록 비싸짐)
    // ==================================================================

    /// <summary>현재 리롤 비용 (사용할 때마다 Growth만큼 증가, 런 단위 리셋)</summary>
    private int RerollCost()
    {
        return GameBalance.RerollBaseCost + AugmentManager.RerollsUsed * GameBalance.RerollCostGrowth;
    }

    /// <summary>
    /// 이번 판 카드 수: 기본 3 + 행운의 부적(최대 +2) - 엄선된 메뉴판(-1). 2~5장 범위
    /// </summary>
    private int CardCount()
    {
        int count = cardCount + AugmentManager.ExtraCards;
        if (AugmentManager.CurationBoost) count -= 1;
        return Mathf.Clamp(count, 2, 5);
    }

    private void Reroll()
    {
        if (closing) return;
        int cost = RerollCost();
        if (GameManager.Instance == null || !GameManager.Instance.SpendGold(cost))
        {
            UIManager.Instance?.ShowStatChange("골드 부족! 리롤에는 " + cost + "G가 필요하다");
            return;
        }

        AugmentManager.RerollsUsed++;
        SoundManager.Play("sfx_ui_click");

        int count = CardCount();
        currentCards = RollCards(count);
        BuildCards(currentCards);
        RefreshHeader();

        Debug.Log("[증강] 리롤! -" + cost + "G (다음 비용 " + RerollCost() + "G)");
    }

    /// <summary>v1.6: 카드 뽑기 - 선로 보상 판이면 등급을 고정한다 (리롤도)</summary>
    private List<AugmentData> RollCards(int count)
    {
        AugmentDatabase.ForceGrade = extraGrade;
        List<AugmentData> rolled = AugmentDatabase.Roll(currentWave, count);
        AugmentDatabase.ForceGrade = -1;
        return rolled;
    }

    /// <summary>헤더 문구 갱신 (리롤 비용 포함). v1.6: 선로 보상 판은 출처를 머리글에</summary>
    private void RefreshHeader()
    {
        string tail = "  /  건너뛰기 [0]  /  리롤 [9] (" + RerollCost() + "G)";
        if (extraGrade >= 0)
        {
            string gradeName = extraGrade == (int)AugmentGrade.Gold ? "금" : extraGrade == (int)AugmentGrade.Prismatic ? "프리즘" : "은";
            headerText.text = "[선로 보상]  " + (extraSource ?? "") + "  -  " + gradeName + " 증강 한 번 더 [1~" + currentCards.Count + "]" + tail;
        }
        else
            headerText.text = "웨이브 " + currentWave + " 클리어!   증강 선택 [1~" + currentCards.Count + "]" + tail;
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
        ApplyPerWaveEffects();
        Open(waveNumber, afterPick);
    }

    /// <summary>v1.2: 웨이브 클리어마다 적용되는 증강 효과 (선택창 여부와 무관 - WaveManager 가 선택창을 건너뛰는 웨이브에도 부른다)</summary>
    public void ApplyPerWaveEffects()
    {
        // '응급 정비' 계열 증강 효과: 웨이브마다 자동 회복
        if (AugmentManager.HealPerWave > 0f)
            AugmentManager.HealTrain(AugmentManager.HealPerWave);
        // v1.12 (v9.25 B5): 유물 '선대의 식탁보' - 웨이브를 막을 때마다 기차 HP 회복
        if (ItemManager.WaveHeal > 0f)
            AugmentManager.HealTrain(ItemManager.WaveHeal);

        // '야전 정비반' 증강 효과: 웨이브마다 최대 HP 영구 증가
        if (AugmentManager.MaxHPPerWave > 0f)
        {
            AugmentManager.AddTrainMaxHP(AugmentManager.MaxHPPerWave);
            AugmentManager.HealTrain(AugmentManager.MaxHPPerWave);
        }
    }

    /// <summary>증강 선택창 열기</summary>
    public void Open(int waveNumber, System.Action afterPick)
    {
        if (isOpen) return;
        extraGrade = -1; extraSource = null;
        OpenInternal(waveNumber, afterPick);
    }

    /// <summary>v1.6: 선로 보상 판 - 등급을 고정해 한 번 더 (사냥터 = 은, 위험 = 금). 끝나면 afterPick</summary>
    public void OpenExtra(int waveNumber, AugmentGrade grade, string sourceLabel, System.Action afterPick)
    {
        if (isOpen) { if (afterPick != null) afterPick(); return; }
        extraGrade = (int)grade; extraSource = sourceLabel;
        UIManager.Instance?.ShowStatChange("[선로 보상] " + (sourceLabel ?? "") + " - 증강을 한 번 더 고른다");
        OpenInternal(waveNumber, afterPick);
    }

    private void OpenInternal(int waveNumber, System.Action afterPick)
    {
        if (isOpen) return;

        isOpen = true;
        currentWave = waveNumber;
        onClosed = afterPick;

        // '행운의 부적'(+장) / '엄선된 메뉴판'(-1장, 등급 상승) 반영. v1.6: 선로 보상 판은 등급 고정
        int count = CardCount();
        List<AugmentData> rolled = RollCards(count);
        currentCards = rolled;   // v1.2: 숫자키 선택용 보관
        BuildCards(rolled);

        RefreshHeader();
        ownedText.text = BuildOwnedSummary();

        dimRoot.gameObject.SetActive(true);
        ModalFeel.Play(dimRoot);   // v1.3: 어둠 페이드 + 카드 판 팝
        if (pauseWhilePicking) Time.timeScale = 0f;

        Debug.Log("[증강] 선택창 오픈 - 웨이브 " + waveNumber + " / 후보 " + rolled.Count + "개");
    }

    /// <summary>카드를 골랐을 때</summary>
    private void Pick(AugmentData aug)
    {
        if (closing) return;
        AugmentManager.Acquire(aug);
        SoundManager.Play("sfx_augment_pick");   // v1.8
        int index = currentCards.IndexOf(aug);
        BeginClose(index >= 0 && index < cardRects.Count ? cardRects[index] : null);
    }

    /// <summary>v1.9 (C4): 퇴장 연출 뒤 닫는다. picked = 고른 카드 (건너뛰기는 null - 창만 사라진다). 시간은 닫힐 때까지 멈춘 채</summary>
    private void BeginClose(RectTransform picked)
    {
        closing = true;
        CardFeel.PickExit(dimRoot, cardArea, cardRects, picked, delegate { closing = false; Close(); });
    }

    private void Close()
    {
        isOpen = false;
        extraGrade = -1; extraSource = null;
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
        cardRects.Clear();   // v1.9

        float cardW = 330f;
        float cardH = 430f;
        float gap = 40f;
        float totalW = list.Count * cardW + (list.Count - 1) * gap;
        float startX = -totalW * 0.5f + cardW * 0.5f;
        float fit = UIFactory.FitScale(totalW, 0f);   // v9.18: 카드가 많아 줄이 화면보다 넓으면 영역째 줄인다 (보통 3장 = 1)
        cardArea.localScale = new Vector3(fit, fit, 1f);

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
            cardRects.Add(card);   // v1.9

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

            // 설명 = 짧은 카드 글 + (빈 줄 하나) + 상태 줄 몰아서 (v1.14: 상태 줄끼리는 빈 줄 없이)
            string status = "";
            // Phase 2-2: 이미 보유한 스택형이면 몇 개째인지 + 그 종류의 지금 합계
            int ownedCount = AugmentManager.CountAugment(aug.id);
            if (ownedCount > 0)
            {
                int timesNext = AugmentManager.StackTimes(aug.id, ownedCount + 1);   // v1.11: 실제로 몇 번 실행되나 (Acquire 와 같은 값)
                string sum = AugmentManager.StackSummary(aug.id);                    // v1.12 (v9.25 B1): 덧셈이면 그 종류의 지금 합계
                status = AddLine(status, "[보유 " + ownedCount + "개" + (timesNext > 1 ? " · 이번엔 효과 " + timesNext + "배!" : (sum.Length > 0 ? " · " + sum : "")) + "]");
            }
            // v1.11: 공속 증강은 상한 때문에 덜 붙을 때만 지금 붙는 몫을 적는다
            status = AddLine(status, AugmentManager.AspdGainNote(aug.id));
            // v1.13 (v9.35.2): 맨 아래 - 지금 내 포탑 중 몇 기에 걸리나 (기차 · 골드 증강은 줄 없음)
            status = AddLine(status, AugmentTargets.Note(aug));
            string descBody = status.Length > 0 ? aug.desc + "\n\n" + status : aug.desc;

            Text descTxt = KitchenEventManager.MakeText(inner, "Desc", descBody, 21, new Color(0.88f, 0.88f, 0.84f));
            RectTransform dRt = descTxt.rectTransform;
            dRt.anchorMin = new Vector2(0f, 0f);
            dRt.anchorMax = new Vector2(1f, 1f);
            dRt.offsetMin = new Vector2(20f, 90f);
            dRt.offsetMax = new Vector2(-20f, -160f);
            descTxt.alignment = TextAnchor.UpperCenter;
            descTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            descTxt.verticalOverflow = VerticalWrapMode.Truncate;
            // v1.13: 설명이 길어졌다 (걸리는 요리 이름 + 맨 아래 줄 + 보유 줄) - 칸을 넘치면 글자를 줄여 끝까지 보이게
            descTxt.resizeTextForBestFit = true;
            descTxt.resizeTextMaxSize = descTxt.fontSize;
            descTxt.resizeTextMinSize = Mathf.Min(descTxt.fontSize, Mathf.Max(GameBalance.UIFontFloor, 15));

            // 선택 버튼
            AugmentData captured = aug;   // 클로저 캡처용 지역 변수 (C# 7.3 필수)
            Button pickBtn = KitchenEventManager.MakeButton(
                inner, "선택", new Color(gc.r * 0.45f, gc.g * 0.45f, gc.b * 0.45f, 1f),
                new Vector2(0f, -cardH * 0.5f + 52f), new Vector2(cardW - 60f, 62f));
            pickBtn.onClick.AddListener(delegate { Pick(captured); });
        }

        CardFeel.StaggerIn(cardRects, GameBalance.CardStaggerSec);   // v1.9 (C3): 왼쪽부터 하나씩
    }

    /// <summary>v1.14: 상태 줄 붙이기 - 빈 글이면 그대로, 앞에 줄이 있으면 줄바꿈 하나로 잇는다</summary>
    private static string AddLine(string lines, string line)
    {
        if (string.IsNullOrEmpty(line)) return lines;
        return lines.Length > 0 ? lines + "\n" + line : line;
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
        scaler.referenceResolution = UIFactory.RefResolution;   // v9.14: UI 전체 배율 (GameBalance.UIScale)
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

        // Phase 2-2: 리롤 버튼 (골드 소모 - 비용은 헤더에 표시)
        Button rerollBtn = KitchenEventManager.MakeButton(dimRoot,
            "리롤  [9]", new Color(0.30f, 0.22f, 0.34f), Vector2.zero, new Vector2(220f, 52f));
        RectTransform rrRt = rerollBtn.GetComponent<RectTransform>();
        rrRt.anchorMin = new Vector2(0.5f, 0f);
        rrRt.anchorMax = new Vector2(0.5f, 0f);
        rrRt.pivot = new Vector2(0.5f, 0f);
        rrRt.anchoredPosition = new Vector2(330f, 140f);
        rerollBtn.onClick.AddListener(Reroll);

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
