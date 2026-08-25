using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [SpinoBetUI.cs] v1 (신규 파일) - Phase 2-1: 도박사 스피노 등장/베팅 UI
///
/// 세계관: 스피노는 디 오리지널의 마지막 기관사. 우리에게 베팅을 거는 진짜 이유는
/// "이번 요리사는 끝까지 가는지 판돈을 걸어보는 것" (스토리바이블 3절).
///
/// 흐름: 보스 직전 정차(Town) - 증강 선택 직후 자동 등장 (WaveManager가 호출)
///  - 대사 1줄 (첫만남/재회/직전 베팅 결과 반응 - MetaProgress 연동 로테이션)
///  - 카드 2장: 일반 베팅 1 + 도박 베팅 1 (숫자키 1/2, 거절 0/ESC)
///  - 수락/거절 대사 후 닫힘 -> 기존 선로 선택 체인으로 이어짐
/// 정산은 보스 격파 순간 SpinoBet.Resolve()가 처리.
///
/// 사용법: 없음! WaveManager가 보스 직전에 SpinoBetUI.Show(...)로 자동 생성.
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class SpinoBetUI : MonoBehaviour
{
    /// <summary>패널 열림 여부 (PauseMenu ESC 가드용)</summary>
    public static bool IsOpen { get; private set; }

    private System.Action onClosed;
    private SpinoBet.BetId cardA;   // 일반
    private SpinoBet.BetId cardB;   // 도박

    private GameObject canvasGo;
    private Text speechText;
    private bool closing = false;

    // ─────────────────────────────────────────────
    // 등장 (WaveManager.AfterAugmentPick이 보스 직전에 호출)
    // ─────────────────────────────────────────────
    public static void Show(int bossWave, System.Action onClosedCallback)
    {
        if (IsOpen) { if (onClosedCallback != null) onClosedCallback(); return; }

        GameObject go = new GameObject("SpinoBetUI");
        SpinoBetUI ui = go.AddComponent<SpinoBetUI>();
        ui.onClosed = onClosedCallback;
        ui.Setup();
    }

    private void Setup()
    {
        IsOpen = true;
        SpinoBet.PickCards(out cardA, out cardB);
        BuildUI();

        speechText.text = PickGreeting();
        MetaProgress.AddSpinoMeeting();
        SoundManager.Play("sfx_train_whistle");   // 임시: 오토바이 소리 클립(sfx_spino) 생기면 교체

        Debug.Log("[SpinoBetUI] 스피노 등장 (만남 " + MetaProgress.SpinoMeetings + "회차)");
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

        if (Input.GetKeyDown(KeyCode.Alpha1)) PickCard(cardA);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) PickCard(cardB);
        else if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Escape)) Decline();
    }

    private void PickCard(SpinoBet.BetId id)
    {
        if (closing) return;
        closing = true;

        SpinoBet.Accept(id);
        speechText.text = "\"좋아. 판은 벌어졌다. 황야가 증인이야.\"";
        SoundManager.Play("sfx_augment_pick");
        Invoke("CloseNow", 1.2f);
    }

    private void Decline()
    {
        if (closing) return;
        closing = true;

        speechText.text = "\"현명한 건지 겁쟁이인 건지. ...다음 역에서 보지.\"";
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
    // 대사 로테이션 (첫만남 > 직전 결과 반응 > 일반 재회)
    // ─────────────────────────────────────────────
    private string PickGreeting()
    {
        // 첫만남 (영구 기준)
        if (MetaProgress.SpinoMeetings == 0)
            return "\"허어. 아직도 달리는 급식차가 있었나.\n...냄새는 그럴싸하군. 한 판 걸어볼 텐가, 깡통 주방장?\"";

        // 직전 베팅 결과 반응 (이번 세션)
        if (SpinoBet.LastResult == 1)
        {
            SpinoBet.LastResult = 0;
            return "\"지난판은 운이었다. 운은 연속으로 안 와.\n...그래서, 또 걸 텐가?\"";
        }
        if (SpinoBet.LastResult == 2)
        {
            SpinoBet.LastResult = 0;
            return "\"외상값 받으러 왔지. ...농담이다. 이미 받았잖나.\n자, 만회할 기회다.\"";
        }

        // 도박꾼 기질 감지 (영구 승패 누적)
        if (MetaProgress.BetWins + MetaProgress.BetLosses >= 6)
            return "\"네 눈빛이 점점 나를 닮아간다. 칭찬이 아니야.\"";

        // 일반 재회 로테이션
        string[] pool =
        {
            "\"또 만났군. 황야는 좁아서 탈이야.\"",
            "\"그 기차, 아직 안 망가졌나. 놀랍군.\"",
            "\"냄새로 알았다. 네 그릴은 소리가 나기 전에 냄새가 먼저 와.\"",
            "\"앞 역은 험하다. 판돈 걸기 딱 좋은 밤이지.\""
        };
        return pool[Random.Range(0, pool.Length)];
    }

    // ─────────────────────────────────────────────
    // UI 생성 (하단 대화 패널 + 카드 2장)
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        canvasGo = new GameObject("SpinoCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 595;   // 분기 선로(590) 위, 증강(600) 아래
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // 하단 대화 패널
        RectTransform panel = KitchenEventManager.MakeBox(canvasGo.transform, "Panel",
            new Color(0.08f, 0.06f, 0.10f, 0.96f));
        panel.anchorMin = new Vector2(0.5f, 0f);
        panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 40f);
        panel.sizeDelta = new Vector2(900f, 330f);

        // 보라 테두리 (스피노 = 도박/황혼의 색)
        RectTransform border = KitchenEventManager.MakeBox(panel, "Border",
            new Color(0.65f, 0.4f, 0.95f));
        border.anchorMin = Vector2.zero; border.anchorMax = new Vector2(1f, 0f);
        border.pivot = new Vector2(0.5f, 0f);
        border.anchoredPosition = Vector2.zero;
        border.sizeDelta = new Vector2(0f, 3f);

        // 명패
        Text name = KitchenEventManager.MakeText(panel, "Name",
            "도박사 스피노  -  오토바이가 멈춰 선다", 22, new Color(0.8f, 0.6f, 1f));
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

        // 카드 2장
        BuildCard(panel, 0, cardA);
        BuildCard(panel, 1, cardB);

        // 거절 안내
        Text pass = KitchenEventManager.MakeText(panel, "Pass",
            "[0] / [ESC]  베팅 안 한다", 16, new Color(0.6f, 0.58f, 0.55f));
        RectTransform pRt = pass.rectTransform;
        pRt.anchorMin = new Vector2(0f, 0f); pRt.anchorMax = new Vector2(1f, 0f);
        pRt.pivot = new Vector2(0.5f, 0f);
        pRt.anchoredPosition = new Vector2(0f, 10f);
        pRt.sizeDelta = new Vector2(0f, 22f);
    }

    private void BuildCard(RectTransform parent, int index, SpinoBet.BetId id)
    {
        bool gamble = SpinoBet.IsGamble(id);
        Color frame = gamble ? new Color(1f, 0.55f, 0.2f) : new Color(0.55f, 0.75f, 0.5f);

        RectTransform card = KitchenEventManager.MakeBox(parent, "Card" + index,
            new Color(0.13f, 0.11f, 0.09f, 0.97f));
        card.anchorMin = new Vector2(0.5f, 0f);
        card.anchorMax = new Vector2(0.5f, 0f);
        card.pivot = new Vector2(0.5f, 0f);
        card.anchoredPosition = new Vector2(index == 0 ? -218f : 218f, 44f);
        card.sizeDelta = new Vector2(412f, 170f);

        // 카드 테두리 (상단 띠)
        RectTransform top = KitchenEventManager.MakeBox(card, "Top", frame);
        top.anchorMin = new Vector2(0f, 1f); top.anchorMax = new Vector2(1f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.anchoredPosition = Vector2.zero;
        top.sizeDelta = new Vector2(0f, 3f);

        Text title = KitchenEventManager.MakeText(card, "Title",
            "[" + (index + 1) + "]  " + (gamble ? "(도박) " : "") + SpinoBet.TitleOf(id), 20, frame);
        RectTransform tRt = title.rectTransform;
        tRt.anchorMin = new Vector2(0f, 1f); tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -8f);
        tRt.sizeDelta = new Vector2(-20f, 26f);
        title.alignment = TextAnchor.MiddleLeft;

        Text desc = KitchenEventManager.MakeText(card, "Desc", SpinoBet.DescOf(id), 16,
            new Color(0.9f, 0.87f, 0.8f));
        RectTransform dRt = desc.rectTransform;
        dRt.anchorMin = Vector2.zero; dRt.anchorMax = Vector2.one;
        dRt.offsetMin = new Vector2(12f, 8f);
        dRt.offsetMax = new Vector2(-12f, -38f);
        desc.alignment = TextAnchor.UpperLeft;

        // 클릭도 가능
        Button btn = card.gameObject.AddComponent<Button>();
        btn.targetGraphic = card.GetComponent<Image>();
        SpinoBet.BetId captured = id;
        btn.onClick.AddListener(delegate { PickCard(captured); });
    }
}
