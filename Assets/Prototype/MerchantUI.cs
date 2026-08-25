using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [MerchantUI.cs] v1 (신규 파일) - Phase 2-3: 등짐장수 안킬로 (아이템 행상인)
///
/// 세계관: 등껍질에 냄비며 부지깽이를 주렁주렁 매단 안킬로사우르스 행상인.
/// 도박꾼 스피노와 대비되는 캐릭터 - 느긋하고, 값은 정직하다.
/// 스피노가 있는 곳(보스 직전 정차)에는 절대 오지 않는다. "그 도마뱀 옆엔 안 앉수다."
///
/// 흐름: 보스 직전이 "아닌" 정차에서 확률 등장 (WaveManager.AfterAugmentPick이 호출)
///  - 각 지역 첫 정차는 확정 등장 (시스템 소개 보장)
///  - 매대 2칸 (일반 1 + 희귀 1 우선), 골드로 구매. [1]/[2] 구매, [0]/[ESC] 안 산다
///  - 둘 다 사거나 떠나면 닫힘 -> 기존 선로 선택 체인으로 이어짐
///
/// 사용법: 없음! WaveManager가 MerchantUI.ShouldAppear/Show로 자동 생성.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class MerchantUI : MonoBehaviour
{
    /// <summary>패널 열림 여부 (PauseMenu ESC 가드용)</summary>
    public static bool IsOpen { get; private set; }

    private System.Action onClosed;
    private ItemData cardA;
    private ItemData cardB;
    private bool soldA = false;
    private bool soldB = false;

    private GameObject canvasGo;
    private Text speechText;
    private Text titleA;
    private Text titleB;
    private Text goldText;   // 하단 보유 골드 표시 (구매할 때마다 갱신)
    private bool closing = false;

    // ─────────────────────────────────────────────
    // 등장 판정 + 등장 (WaveManager.AfterAugmentPick이 호출)
    // ─────────────────────────────────────────────

    /// <summary>
    /// 이번 정차에 행상인이 오는가.
    /// 지역 첫 정차 = 확정 / 그 외 = GameBalance.MerchantChance 확률.
    /// 팔 물건이 다 떨어졌으면 오지 않는다.
    /// </summary>
    public static bool ShouldAppear(int nextWave)
    {
        if (IsOpen) return false;
        if (!ItemManager.HasStock()) return false;

        int region = GameBalance.RegionOf(nextWave);
        if (region != ItemManager.MerchantGuaranteedRegion)
        {
            ItemManager.MerchantGuaranteedRegion = region;
            return true;
        }
        return Random.value < GameBalance.MerchantChance;
    }

    public static void Show(int nextWave, System.Action onClosedCallback)
    {
        if (IsOpen) { if (onClosedCallback != null) onClosedCallback(); return; }

        GameObject go = new GameObject("MerchantUI");
        MerchantUI ui = go.AddComponent<MerchantUI>();
        ui.onClosed = onClosedCallback;
        ui.Setup();
    }

    private void Setup()
    {
        IsOpen = true;
        ItemManager.GetShopOffer(out cardA, out cardB);
        BuildUI();

        speechText.text = PickGreeting();
        MetaProgress.AddAnkyMeeting();
        SoundManager.Play("sfx_train_whistle");   // 임시: 무거운 발소리 클립(sfx_anky) 생기면 교체

        Debug.Log("[MerchantUI] 안킬로 등장 (만남 " + MetaProgress.AnkyMeetings + "회차)");
    }

    private void OnDestroy()
    {
        IsOpen = false;
        if (canvasGo != null) Destroy(canvasGo);
    }

    // ─────────────────────────────────────────────
    // 입력
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (closing) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) TryBuy(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) TryBuy(1);
        else if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Escape)) Leave();
    }

    private void TryBuy(int index)
    {
        if (closing) return;
        ItemData item = index == 0 ? cardA : cardB;
        bool sold = index == 0 ? soldA : soldB;
        if (item == null || sold) return;

        int price = ItemManager.PriceOf(item);
        if (GameManager.Instance == null || !GameManager.Instance.SpendGold(price))
        {
            speechText.text = "\"주머니가 가볍구려. 억지로는 안 팔우. 다음에 사시구려.\"";
            SoundManager.Play("sfx_ui_click");
            return;
        }

        ItemManager.Acquire(item);
        if (index == 0) { soldA = true; if (titleA != null) titleA.text = "- 팔림 -"; }
        else { soldB = true; if (titleB != null) titleB.text = "- 팔림 -"; }

        speechText.text = "\"좋은 선택이우. 오래 쓰시구려.\"";
        RefreshGoldText();
        SoundManager.Play("sfx_pickup");

        // 매대가 다 비면 인사하고 떠난다
        if ((cardA == null || soldA) && (cardB == null || soldB))
        {
            closing = true;
            speechText.text = "\"오늘 장사는 끝이우. 살펴 가시구려, 주방장 양반.\"";
            Invoke("CloseNow", 1.2f);
        }
    }

    private void Leave()
    {
        if (closing) return;
        closing = true;

        speechText.text = "\"허허. 황야는 넓고 역은 또 있지. 살펴 가시우.\"";
        SoundManager.Play("sfx_ui_click");
        Invoke("CloseNow", 1.1f);
    }

    private void CloseNow()
    {
        System.Action cb = onClosed;
        Destroy(gameObject);
        if (cb != null) cb();
    }

    // ─────────────────────────────────────────────
    // 대사 로테이션 (첫만남 > 재회 변주)
    // ─────────────────────────────────────────────
    private string PickGreeting()
    {
        // 첫만남 (영구 기준)
        if (MetaProgress.AnkyMeetings == 0)
            return "\"어이쿠. 살아있는 손님은 오랜만이구려.\n등껍질에 좋은 물건 있수다. 값은 정직하게 받지.\"";

        float roll = Random.value;
        if (roll < 0.25f)
            return "\"그 보라색 도마뱀이랑은 엮이지 마시구려.\n...뭐, 이미 늦은 얼굴이구먼.\"";
        if (roll < 0.5f)
            return "\"급하게 갈 것 없수다. 황야에서 제일 무거운 게 나요.\n천천히 골라 보시우.\"";
        if (roll < 0.75f)
            return "\"굶주린 것들이 요즘 사납지.\n주방 지키는 물건들, 미리 챙겨 두시구려.\"";
        return "\"또 만났구려. 기차 냄새가 점점 그럴싸해지는구먼.\n오늘 매대는 이렇수다.\"";
    }

    // ─────────────────────────────────────────────
    // UI 생성 (전부 코드 생성 - 씬 작업 불필요)
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        canvasGo = new GameObject("MerchantCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 594;   // 분기선로(590)와 스피노(595) 사이
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // 하단 대화 패널
        RectTransform panel = KitchenEventManager.MakeBox(canvasGo.transform, "Panel",
            new Color(0.10f, 0.08f, 0.06f, 0.96f));
        panel.anchorMin = new Vector2(0.5f, 0f);
        panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 40f);
        panel.sizeDelta = new Vector2(900f, 330f);

        // 구리 테두리 (안킬로 = 냄비 장수의 색)
        RectTransform border = KitchenEventManager.MakeBox(panel, "Border",
            new Color(0.85f, 0.55f, 0.25f));
        border.anchorMin = Vector2.zero; border.anchorMax = new Vector2(1f, 0f);
        border.pivot = new Vector2(0.5f, 0f);
        border.anchoredPosition = Vector2.zero;
        border.sizeDelta = new Vector2(0f, 3f);

        // 명패
        Text name = KitchenEventManager.MakeText(panel, "Name",
            "등짐장수 안킬로  -  등껍질이 철렁, 하고 내려앉는다", 22, new Color(0.95f, 0.75f, 0.45f));
        RectTransform nRt = name.rectTransform;
        nRt.anchorMin = new Vector2(0f, 1f); nRt.anchorMax = new Vector2(1f, 1f);
        nRt.pivot = new Vector2(0.5f, 1f);
        nRt.anchoredPosition = new Vector2(0f, -10f);
        nRt.sizeDelta = new Vector2(-24f, 28f);
        name.alignment = TextAnchor.MiddleLeft;

        // 대사
        speechText = KitchenEventManager.MakeText(panel, "Speech", "", 19,
            new Color(0.93f, 0.9f, 0.84f));
        RectTransform sRt = speechText.rectTransform;
        sRt.anchorMin = new Vector2(0f, 1f); sRt.anchorMax = new Vector2(1f, 1f);
        sRt.pivot = new Vector2(0.5f, 1f);
        sRt.anchoredPosition = new Vector2(0f, -42f);
        sRt.sizeDelta = new Vector2(-28f, 58f);
        speechText.alignment = TextAnchor.UpperLeft;

        // 매대 2칸
        titleA = BuildCard(panel, 0, cardA);
        titleB = BuildCard(panel, 1, cardB);

        // 떠나기 안내 + 보유 골드 (구매할 때마다 갱신)
        goldText = KitchenEventManager.MakeText(panel, "Pass", "", 16, new Color(0.6f, 0.58f, 0.55f));
        RectTransform pRt = goldText.rectTransform;
        pRt.anchorMin = new Vector2(0f, 0f); pRt.anchorMax = new Vector2(1f, 0f);
        pRt.pivot = new Vector2(0.5f, 0f);
        pRt.anchoredPosition = new Vector2(0f, 10f);
        pRt.sizeDelta = new Vector2(0f, 22f);
        RefreshGoldText();
    }

    /// <summary>하단 안내줄 갱신 (보유 골드 포함)</summary>
    private void RefreshGoldText()
    {
        if (goldText == null) return;
        goldText.text = "[0] / [ESC]  안 산다   (보유 골드 "
            + (GameManager.Instance != null ? GameManager.Instance.playerGold : 0) + "G)";
    }

    /// <summary>매대 카드 1칸. 반환값: 제목 텍스트 (팔림 표시용)</summary>
    private Text BuildCard(RectTransform parent, int index, ItemData item)
    {
        RectTransform card = KitchenEventManager.MakeBox(parent, "Card" + index,
            new Color(0.14f, 0.11f, 0.08f, 0.97f));
        card.anchorMin = new Vector2(0.5f, 0f);
        card.anchorMax = new Vector2(0.5f, 0f);
        card.pivot = new Vector2(0.5f, 0f);
        card.anchoredPosition = new Vector2(index == 0 ? -218f : 218f, 44f);
        card.sizeDelta = new Vector2(412f, 170f);

        if (item == null)
        {
            // 매물이 1개뿐일 때: 빈 매대
            KitchenEventManager.MakeText(card, "Empty", "(빈 매대)", 18,
                new Color(0.45f, 0.42f, 0.38f));
            return null;
        }

        Color frame = item.RarityColor();

        // 카드 상단 띠 (희귀도 색)
        RectTransform top = KitchenEventManager.MakeBox(card, "Top", frame);
        top.anchorMin = new Vector2(0f, 1f); top.anchorMax = new Vector2(1f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.anchoredPosition = Vector2.zero;
        top.sizeDelta = new Vector2(0f, 3f);

        Text title = KitchenEventManager.MakeText(card, "Title",
            "[" + (index + 1) + "]  " + item.name + "  -  " + ItemManager.PriceOf(item) + "G", 20, frame);
        RectTransform tRt = title.rectTransform;
        tRt.anchorMin = new Vector2(0f, 1f); tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -8f);
        tRt.sizeDelta = new Vector2(-20f, 26f);
        title.alignment = TextAnchor.MiddleLeft;

        Text desc = KitchenEventManager.MakeText(card, "Desc",
            "(" + item.RarityName() + ") " + item.desc, 16,
            new Color(0.9f, 0.87f, 0.8f));
        RectTransform dRt = desc.rectTransform;
        dRt.anchorMin = Vector2.zero; dRt.anchorMax = Vector2.one;
        dRt.offsetMin = new Vector2(12f, 8f);
        dRt.offsetMax = new Vector2(-12f, -38f);
        desc.alignment = TextAnchor.UpperLeft;

        // 클릭도 가능
        Button btn = card.gameObject.AddComponent<Button>();
        btn.targetGraphic = card.GetComponent<Image>();
        int captured = index;
        btn.onClick.AddListener(delegate { TryBuy(captured); });

        return title;
    }
}
