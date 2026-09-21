using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// [KitchenEvents.cs] v2 / v9.10.1 2026-09-21: 재료 이름 MaterialNames
/// 주방 돌발 이벤트 인터페이스 + 4종 구현체 (기획 B-4)
///
/// 새 조작키를 만들지 않고 기존 조작만 재활용한다
///  - 몬스터 침입 : E 연타      (조리 상호작용 키)
///  - 기구 고장   : 방향키 커맨드 (볶기 미니게임 조작)
///  - 주방 화재   : E 홀드       (조리 상호작용 키)
///  - 재료 흘림   : 마우스 좌클릭 (슬롯 마커 조작)
///
/// - v2 (v9.6, 2026-09-09): "화면 전체 경보" 비주얼 (이벤트 목업 v2 컨펌)
///   판정 / 게이지 / 제한시간 / 보상 / 페널티 / 앵커 근접 게이트는 v1 그대로. KitchenEventManager.SkinReady 가 true 일 때만 그림을 바꾼다
///   * 침입자: 화면 오른쪽 위 큰 발톱 자국 + 화면 금, 왼쪽 아래 작은 발톱 (OverlayRoot). E 연타마다 화면 흔들림 + 발톱이 번쩍,
///             몰아낼수록 옅어진다. 현장에는 150 카드(빨간 테) 안에 e_raptor 앞쪽 절반이 들이닥쳐 있고 연타할수록 밖으로 밀려난다 (마스크)
///   * 고장  : 화면 노이즈 줄 깜빡 + 노란 경보. 현장 카드에 화살표 칩 n개 (완료 흐림 / 현재 황금 / 대기 크림) + 모서리 스파크. 오입력 = 테 빨강
///   * 화재  : HUD 바로 위에서 화면 폭 전체로 타는 불의 벽 (3프레임 타일, 끌수록 낮아짐) + 떠오르는 연기 + 진한 붉은 경보.
///             현장에는 그을음 + 불길 3 (진화 1/3 마다 하나씩 꺼짐) + 연기
///   * 흘림  : 시작 시 화면 흔들림 + 재료 칩 (평판 + 계열색 테 + 재료 아이콘 ui_mat_* + 이름, 살짝 기울어짐)
///   그림이 없으면 v1 단색 박스/글자 그대로
///
/// VS 2017 (C# 7.3) 호환
/// </summary>
public interface IKitchenEvent
{
    /// <summary>배너 제목</summary>
    string Title { get; }
    /// <summary>배너 조작 안내 (매 프레임 갱신되므로 실시간 상태 표시에 써도 된다)</summary>
    string Guide { get; }
    /// <summary>제한 시간(초)</summary>
    float TimeLimit { get; }
    /// <summary>진행도 0~1 (게이지 표시용)</summary>
    float Progress { get; }

    /// <summary>이벤트 시작. difficulty는 0~1 (누적될수록 증가)</summary>
    void OnStart(KitchenEventManager mgr, float difficulty);

    /// <summary>매 프레임 호출. 반환값 true면 종료, success에 성공 여부를 담는다</summary>
    bool OnUpdate(float dt, out bool success);

    /// <summary>종료 처리 (보상 / 페널티)</summary>
    void OnEnd(bool success);
}


/// <summary>v2: 이벤트 4종이 같이 쓰는 색/헬퍼</summary>
public static class KitchenEventSkin
{
    public static readonly Color RED = new Color(0.84f, 0.16f, 0.16f, 1f);        // 경보 / 위험 테
    public static readonly Color YELLOW = new Color(0.9f, 0.67f, 0.16f, 1f);      // 전기 경보
    public static readonly Color COPPER = new Color(0.722f, 0.439f, 0.204f, 1f);  // 카드 기본 테
    public static readonly Color GOLD = new Color(0.886f, 0.698f, 0.227f, 1f);
    public static readonly Color CREAM = new Color(0.969f, 0.910f, 0.776f, 1f);
    public static readonly Color DIMR = new Color(0.55f, 0.47f, 0.35f, 1f);       // 완료/대기 흐림

    /// <summary>불길/스파크 같은 n 프레임 순환 인덱스</summary>
    public static int Frame(float time, float fps, int count, int offset)
    {
        return (Mathf.FloorToInt(time * fps) + offset) % count;
    }
}


// ======================================================================
//  1. 몬스터 침입 - E 연타로 격퇴
// ======================================================================
public class MonsterIntrusionEvent : IKitchenEvent
{
    private KitchenEventManager manager;
    private float gauge;              // 현재 격퇴 게이지
    private float needGauge;          // 목표치
    private float decayPerSec = 14f;  // 가만히 있으면 게이지가 줄어든다
    private float gainPerPress = 9f;  // E 한 번당 상승량
    private RectTransform intruderIcon;
    private float shakeTimer;
    private float baseX;   // B-1: 위치 앵커의 캔버스 X (아이콘이 현장에 뜬다)

    // v2 스킨
    private bool skin;
    private RectTransform raptor;     // 카드 안 랩터 (마스크 안에서 오른쪽으로 밀려난다)
    private Image clawBig, clawSmall, crack;
    private float clawFlash;          // E 를 친 직후 발톱이 번쩍 (초)
    private const float RAPTOR_PUSH = 170f;   // 게이지 100% 일 때 랩터가 밀려나는 거리 (px)

    public string Title { get { return "침입자! 주방에 랩터가 들어왔다"; } }
    public string Guide { get { return "[E] 연타해서 몰아내라!   " + Mathf.RoundToInt(gauge) + " / " + Mathf.RoundToInt(needGauge); } }
    public float TimeLimit { get { return 6.5f; } }
    public float Progress { get { return needGauge > 0f ? gauge / needGauge : 0f; } }

    public void OnStart(KitchenEventManager mgr, float difficulty)
    {
        manager = mgr;
        gauge = 0f;
        needGauge = 90f * (1f + difficulty);   // 난이도에 따라 최대 180

        // Phase 2-3 아이템 '랩터 덫': 침입자가 덫을 밟고 시작 - 격퇴 게이지 감소
        needGauge *= ItemManager.IntruderGaugeMul;

        // 침입자 표시용 아이콘 - B-1: 위치 앵커 지점에 뜬다 (달려갈 곳이 보이게)
        baseX = mgr.AnchorCanvasX();
        skin = KitchenEventManager.SkinReady && SpriteBank.Has("e_raptor");
        if (skin)
        {
            BuildSkin(mgr);
            return;
        }

        intruderIcon = KitchenEventManager.MakeBox(mgr.CustomRoot, "Intruder", new Color(0.75f, 0.2f, 0.18f, 0.92f));
        intruderIcon.anchorMin = new Vector2(0.5f, 0.5f);
        intruderIcon.anchorMax = new Vector2(0.5f, 0.5f);
        intruderIcon.anchoredPosition = new Vector2(baseX, -40f);
        intruderIcon.sizeDelta = new Vector2(150f, 150f);
        intruderIcon.GetComponent<Image>().raycastTarget = false;

        Text label = KitchenEventManager.MakeText(intruderIcon, "Label", "침입자", 24, Color.white);
        RectTransform lrt = label.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
    }

    /// <summary>v2: 화면 발톱 + 금 (오버레이) / 현장 랩터 카드 (커스텀)</summary>
    private void BuildSkin(KitchenEventManager mgr)
    {
        mgr.SetAlarm(KitchenEventSkin.RED, 0.55f);

        // 화면 금 -> 큰 발톱 (오른쪽 위) -> 작은 발톱 (왼쪽 아래, 뒤집음). 목업 v2 좌표 (1920 기준, 화면 중앙 원점)
        crack = KitchenEventManager.MakeSprite(mgr.OverlayRoot, "Crack", SpriteBank.Get("ui_ev_crack"), new Vector2(440f, 290f), new Vector2(320f, 320f));
        clawBig = KitchenEventManager.MakeSprite(mgr.OverlayRoot, "ClawBig", SpriteBank.Get("ui_ev_claw"), new Vector2(420f, 280f), new Vector2(400f, 440f));
        clawSmall = KitchenEventManager.MakeSprite(mgr.OverlayRoot, "ClawSmall", SpriteBank.Get("ui_ev_claw"), new Vector2(-700f, -10f), new Vector2(400f, 440f));
        clawSmall.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

        // 현장 카드: 평판 + 빨간 테 + 마스크 안 랩터 + 명판 + [E] 칩
        Image ring;
        intruderIcon = KitchenEventManager.MakeCard(mgr.CustomRoot, "Intruder", new Vector2(baseX, -40f), new Vector2(150f, 150f), KitchenEventSkin.RED, out ring);

        GameObject maskGo = new GameObject("Mask");
        RectTransform mrt = maskGo.AddComponent<RectTransform>();
        mrt.SetParent(intruderIcon, false);
        mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one;
        mrt.offsetMin = new Vector2(8f, 8f); mrt.offsetMax = new Vector2(-8f, -8f);
        maskGo.AddComponent<RectMask2D>();

        // e_raptor 앞쪽 절반 (x 40~112) 을 잘라 2배로. 머리가 카드 안쪽(왼쪽)을 보도록 뒤집는다
        Sprite full = SpriteBank.Get("e_raptor");
        Rect r = full.rect;
        Sprite front = Sprite.Create(full.texture, new Rect(r.x + 40f, r.y, 72f, r.height), new Vector2(0.5f, 0.5f), full.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        front.name = "e_raptor_front";
        Image rimg = KitchenEventManager.MakeSprite(mrt, "Raptor", front, Vector2.zero, new Vector2(144f, 120f));
        rimg.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        raptor = rimg.rectTransform;
        PlaceRaptor(0f);

        UISkin.Nameplate(intruderIcon, "Intruder", "침입자", 14, new Vector2(0f, 1f), new Vector2(8f, 4f), 74f);
        KitchenEventManager.MakeChip(intruderIcon, "Hint", "[E] 연타!", new Vector2(0f, -95f), KitchenEventSkin.RED);
    }

    /// <summary>랩터 위치: 카드 왼쪽에 머리, 게이지만큼 오른쪽으로 밀려난다</summary>
    private void PlaceRaptor(float t)
    {
        if (raptor == null) return;
        raptor.anchoredPosition = new Vector2(5f + t * RAPTOR_PUSH, 0f);
    }

    public bool OnUpdate(float dt, out bool success)
    {
        success = false;
        gauge -= decayPerSec * dt;
        if (gauge < 0f) gauge = 0f;

        // B-1: 현장(앵커) 근처에서만 격퇴 가능 - 멀리서 누르면 헛손질
        if (Input.GetKeyDown(KeyCode.E) && KitchenEventManager.ChefInReach)
        {
            gauge += gainPerPress;
            shakeTimer = 0.12f;   // 때린 느낌으로 아이콘을 흔든다
            if (skin)
            {
                clawFlash = 0.14f;
                GameFeel.Shake(0.12f, "intrude", 0.05f);
            }
        }

        // 아이콘 흔들기 + 게이지에 따라 작아지는 연출 (v2 스킨: 크기 대신 랩터가 밀려난다)
        if (intruderIcon != null)
        {
            // 플레이테스트 픽스: 카메라가 셰프를 따라가므로 화면 좌표를 매 프레임 재계산
            // (한 번만 계산하면 아이콘이 화면에 눌어붙어 현장이 어딘지 알 수 없었다)
            baseX = manager.AnchorCanvasX();

            float t = Mathf.Clamp01(Progress);
            if (skin)
            {
                PlaceRaptor(t);
                if (clawFlash > 0f) clawFlash -= dt;
                // 몰아낼수록 발톱 자국이 옅어진다. 연타 직후엔 번쩍
                float a = clawFlash > 0f ? 1f : Mathf.Lerp(1f, 0.25f, t);
                if (clawBig != null) clawBig.color = new Color(1f, 1f, 1f, a);
                if (crack != null) crack.color = new Color(1f, 1f, 1f, a);
                if (clawSmall != null) clawSmall.color = new Color(1f, 1f, 1f, a * 0.75f);
            }
            else
            {
                float size = Mathf.Lerp(150f, 60f, t);
                intruderIcon.sizeDelta = new Vector2(size, size);
            }

            if (shakeTimer > 0f)
            {
                shakeTimer -= dt;
                intruderIcon.anchoredPosition = new Vector2(baseX + Random.Range(-9f, 9f), -40f + Random.Range(-9f, 9f));
            }
            else
            {
                intruderIcon.anchoredPosition = new Vector2(baseX, -40f);
            }
        }

        if (gauge >= needGauge)
        {
            success = true;
            return true;
        }
        return false;
    }

    public void OnEnd(bool success)
    {
        if (success)
        {
            manager.HealTrain(25f);
            // 보상 다양화 (v3): 격퇴한 침입자가 재료를 떨군다
            if (MaterialInventory.Instance != null)
                MaterialInventory.Instance.Add(MaterialType.Meat, 1);

            // Phase 2-3 아이템 '장물 주머니': 침입자 격퇴마다 추가 골드
            if (ItemManager.SwagGoldPerIntruder > 0)
            {
                GameManager.Instance?.AddGold(ItemManager.SwagGoldPerIntruder);
                UIManager.Instance?.ShowStatChange("[장물 주머니] 침입자의 주머니를 털었다 +"
                    + ItemManager.SwagGoldPerIntruder + "G");
            }

            // Phase 2-3: 침입자가 낮은 확률로 아이템을 떨군다 (장물 주머니 우선)
            ItemManager.TryIntruderDrop();

            Debug.Log("[주방이벤트] 침입자 격퇴 성공 - 기차 25 회복 + 고기 1");
        }
        else
        {
            manager.DamageTrain(60f);   // HP 500 기준 조정 (기존 90)
            Debug.Log("[주방이벤트] 침입자 격퇴 실패 - 기차 60 피해");
        }
    }
}


// ======================================================================
//  2. 기구 고장 - 방향키 커맨드 입력으로 수리
// ======================================================================
public class EquipmentBreakEvent : IKitchenEvent
{
    private KitchenEventManager manager;
    private List<KeyCode> command = new List<KeyCode>();
    private int inputIndex;
    private Text commandText;
    private float wrongFlash;         // 오입력 시 빨갛게 깜빡이는 시간

    // v2 스킨
    private bool skin;
    private RectTransform card;                       // 화살표 칩 카드 (현장)
    private Image cardRing;
    private Image[] chipRings, chipGlyphs;            // 칩 테 / 화살표 글리프
    private Image[] sparks = new Image[2];
    private Image[] glitchLines = new Image[5];       // 화면 노이즈 줄
    private float glitchTimer;
    private float elapsed;
    private static readonly string[] ARROW_PNG = { "u", "d", "l", "r" };   // KeyCode 순서: Up Down Left Right
    private const float CHIP_STEP = 61f;

    public string Title { get { return "조리 기구 고장! 배선에서 불꽃이 튄다"; } }
    public string Guide { get { return "방향키를 순서대로 입력해 수리하라   " + inputIndex + " / " + command.Count; } }
    public float TimeLimit { get { return 7f; } }
    public float Progress { get { return command.Count > 0 ? (float)inputIndex / command.Count : 0f; } }

    public void OnStart(KitchenEventManager mgr, float difficulty)
    {
        manager = mgr;
        inputIndex = 0;
        command.Clear();

        // 커맨드 길이 4 ~ 8개 (난이도에 비례)
        int length = 4 + Mathf.RoundToInt(difficulty * 4f);
        KeyCode[] pool = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };
        for (int i = 0; i < length; i++)
            command.Add(pool[Random.Range(0, pool.Length)]);

        skin = KitchenEventManager.SkinReady && SpriteBank.Has("ui_mg_arrow_l") && SpriteBank.Has("ui_ev_spark_0") && SpriteBank.Has("ui_ev_glitch");
        if (skin)
        {
            BuildSkin(mgr);
            RefreshChips();
            return;
        }

        // B-1: 고장난 기구가 있는 앵커 지점에 커맨드가 뜬다 (텍스트 폭 고려해 좁게 클램프)
        commandText = KitchenEventManager.MakeText(mgr.CustomRoot, "Command", "", 52, Color.white);
        RectTransform rt = commandText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(Mathf.Clamp(mgr.AnchorCanvasX(), -420f, 420f), -30f);
        rt.sizeDelta = new Vector2(700f, 80f);

        RefreshCommandText();
    }

    /// <summary>v2: 노이즈 줄 (오버레이) + 화살표 칩 카드 + 스파크 (현장)</summary>
    private void BuildSkin(KitchenEventManager mgr)
    {
        mgr.SetAlarm(KitchenEventSkin.YELLOW, 0.35f);

        Sprite glitch = SpriteBank.Get("ui_ev_glitch");
        for (int i = 0; i < glitchLines.Length; i++)
        {
            RectTransform g = KitchenEventManager.MakeBox(mgr.OverlayRoot, "Glitch_" + i, Color.white);
            g.anchorMin = new Vector2(0f, 0.5f); g.anchorMax = new Vector2(1f, 0.5f);
            g.offsetMin = new Vector2(0f, -4f); g.offsetMax = new Vector2(0f, 4f);
            g.anchoredPosition = new Vector2(0f, Random.Range(-500f, 500f));
            glitchLines[i] = g.GetComponent<Image>();
            glitchLines[i].sprite = glitch;
            glitchLines[i].type = Image.Type.Tiled;
            glitchLines[i].raycastTarget = false;
        }

        int n = command.Count;
        float w = n * CHIP_STEP + 30f;
        card = KitchenEventManager.MakeCard(mgr.CustomRoot, "Command", new Vector2(Mathf.Clamp(mgr.AnchorCanvasX(), -420f, 420f), -30f),
            new Vector2(w, 80f), KitchenEventSkin.COPPER, out cardRing);

        chipRings = new Image[n]; chipGlyphs = new Image[n];
        for (int i = 0; i < n; i++)
        {
            float cx = -w / 2f + 15f + 27f + i * CHIP_STEP;
            Image ring;
            RectTransform chip = KitchenEventManager.MakeCard(card, "Chip_" + i, new Vector2(cx, 0f), new Vector2(54f, 54f), KitchenEventSkin.DIMR, out ring);
            chipRings[i] = ring;
            chipGlyphs[i] = KitchenEventManager.MakeSprite(chip, "Glyph", SpriteBank.Get("ui_mg_arrow_" + ARROW_PNG[ArrowIndex(command[i])]), Vector2.zero, new Vector2(32f, 32f));
        }

        sparks[0] = KitchenEventManager.MakeSprite(card, "Spark0", SpriteBank.Get("ui_ev_spark_0"), new Vector2(-w / 2f + 6f, 46f), new Vector2(32f, 32f));
        sparks[1] = KitchenEventManager.MakeSprite(card, "Spark1", SpriteBank.Get("ui_ev_spark_2"), new Vector2(w / 2f - 8f, -44f), new Vector2(32f, 32f));

        UISkin.Nameplate(card, "Break", "고장 - 수리", 14, new Vector2(0f, 1f), new Vector2(8f, 4f), 104f);
        KitchenEventManager.MakeChip(card, "Hint", "방향키 순서대로", new Vector2(0f, -60f), KitchenEventSkin.COPPER);
    }

    private static int ArrowIndex(KeyCode key)
    {
        if (key == KeyCode.UpArrow) return 0;
        if (key == KeyCode.DownArrow) return 1;
        if (key == KeyCode.LeftArrow) return 2;
        return 3;
    }

    public bool OnUpdate(float dt, out bool success)
    {
        success = false;
        if (wrongFlash > 0f) wrongFlash -= dt;
        elapsed += dt;

        // 플레이테스트 픽스: 커맨드 표시를 현장 월드 좌표에 매 프레임 재고정
        float cx = Mathf.Clamp(manager.AnchorCanvasX(), -420f, 420f);
        if (commandText != null)
            commandText.rectTransform.anchoredPosition = new Vector2(cx, -30f);
        if (card != null)
            card.anchoredPosition = new Vector2(cx, -30f);

        KeyCode pressed = ReadArrowKey();
        // B-1: 고장난 기구 곁에서만 수리 입력이 먹힌다 (떨어져서 누르면 무효 - 리셋도 없음)
        if (pressed != KeyCode.None && KitchenEventManager.ChefInReach)
        {
            if (pressed == command[inputIndex])
            {
                inputIndex++;
                if (inputIndex >= command.Count)
                {
                    success = true;
                    return true;
                }
            }
            else
            {
                // 틀리면 처음부터 다시
                inputIndex = 0;
                wrongFlash = 0.25f;
            }
            RefreshCommandText();
            RefreshChips();
        }

        if (commandText != null)
            commandText.color = wrongFlash > 0f ? new Color(1f, 0.35f, 0.3f) : Color.white;

        if (skin)
        {
            // 오입력 = 카드 테가 빨갛게 / 스파크 프레임 순환 / 노이즈 줄은 0.12초마다 자리를 바꾸며 깜빡
            if (cardRing != null) cardRing.color = wrongFlash > 0f ? KitchenEventSkin.RED : KitchenEventSkin.COPPER;
            for (int i = 0; i < sparks.Length; i++)
                if (sparks[i] != null) sparks[i].sprite = SpriteBank.Get("ui_ev_spark_" + KitchenEventSkin.Frame(elapsed, 12f, 3, i));
            glitchTimer += dt;
            if (glitchTimer >= 0.12f)
            {
                glitchTimer = 0f;
                for (int i = 0; i < glitchLines.Length; i++)
                {
                    if (glitchLines[i] == null) continue;
                    bool on = Random.value < 0.6f;
                    glitchLines[i].enabled = on;
                    if (on) glitchLines[i].rectTransform.anchoredPosition = new Vector2(0f, Random.Range(-500f, 500f));
                }
            }
        }

        return false;
    }

    public void OnEnd(bool success)
    {
        if (success)
        {
            manager.HealTrain(15f);
            Debug.Log("[주방이벤트] 기구 수리 성공");
        }
        else
        {
            manager.DamageTrain(40f);   // HP 500 기준 조정 (기존 60)
            Debug.Log("[주방이벤트] 기구 수리 실패 - 기차 40 피해");
        }
    }

    /// <summary>이번 프레임에 눌린 방향키 하나를 반환 (없으면 None)</summary>
    private KeyCode ReadArrowKey()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow)) return KeyCode.UpArrow;
        if (Input.GetKeyDown(KeyCode.DownArrow)) return KeyCode.DownArrow;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) return KeyCode.LeftArrow;
        if (Input.GetKeyDown(KeyCode.RightArrow)) return KeyCode.RightArrow;
        return KeyCode.None;
    }

    /// <summary>남은 커맨드는 흰색, 입력 완료분은 회색으로 표시 (단색 UI)</summary>
    private void RefreshCommandText()
    {
        if (commandText == null) return;
        string s = "";
        for (int i = 0; i < command.Count; i++)
        {
            string arrow = ArrowChar(command[i]);
            if (i < inputIndex) s += "<color=#555555>" + arrow + "</color> ";
            else if (i == inputIndex) s += "<color=#FFC94D>" + arrow + "</color> ";
            else s += arrow + " ";
        }
        commandText.supportRichText = true;
        commandText.text = s;
    }

    /// <summary>v2: 칩 색 - 완료 흐림 / 현재 황금(테도) / 대기 크림</summary>
    private void RefreshChips()
    {
        if (chipGlyphs == null) return;
        for (int i = 0; i < chipGlyphs.Length; i++)
        {
            Color c = i < inputIndex ? KitchenEventSkin.DIMR : i == inputIndex ? KitchenEventSkin.GOLD : KitchenEventSkin.CREAM;
            if (chipGlyphs[i] != null) chipGlyphs[i].color = c;
            if (chipRings[i] != null) chipRings[i].color = i == inputIndex ? KitchenEventSkin.GOLD : KitchenEventSkin.DIMR;
        }
    }

    private string ArrowChar(KeyCode key)
    {
        if (key == KeyCode.UpArrow) return "↑";
        if (key == KeyCode.DownArrow) return "↓";
        if (key == KeyCode.LeftArrow) return "←";
        return "→";
    }
}


// ======================================================================
//  3. 주방 화재 - E 홀드로 진화
// ======================================================================
public class KitchenFireEvent : IKitchenEvent
{
    private KitchenEventManager manager;
    private float gauge;
    private float needGauge = 100f;
    private float holdGain = 42f;     // E 누르고 있을 때 초당 상승
    private float releaseLoss = 22f;  // 떼면 초당 감소
    private float burnDamagePerSec;   // 진화 전까지 기차가 계속 입는 피해
    private float burnTickTimer;      // 도트 적용 주기 누적기 (0.5초 묶음)
    private RectTransform fireBox;
    private Text fireLabel;

    // v2 스킨
    private bool skin;
    private RectTransform wall;                 // 불의 벽 (마스크 안에서 아래로 내려가며 낮아진다)
    private Image wallImg;
    private Image[] wallSmoke = new Image[3];   // 벽 위로 떠오르는 연기
    private float[] smokeT = new float[3];
    private Image[] flames = new Image[3];      // 현장 불길 (순서대로 꺼진다: 왼쪽 -> 오른쪽 -> 가운데)
    private Image[] markerSmoke = new Image[2];
    private float elapsed;
    private const float WALL_H = 96f;           // 불의 벽 높이 (HUD 184 바로 위)
    private const float WALL_DROP = 80f;        // 진화 100% 때 벽이 내려가는 양

    public string Title { get { return "주방 화재 발생! 기차가 계속 타들어간다"; } }
    public string Guide { get { return "[E] 꾹 눌러 불길을 잡아라   " + Mathf.RoundToInt(gauge) + "%"; } }
    public float TimeLimit { get { return 8f; } }
    public float Progress { get { return gauge / needGauge; } }

    public void OnStart(KitchenEventManager mgr, float difficulty)
    {
        manager = mgr;
        gauge = 0f;
        burnTickTimer = 0f;
        burnDamagePerSec = 5f + difficulty * 5f;   // 난이도에 따라 5 ~ 10 (기차 HP 500 기준 조정)

        skin = KitchenEventManager.SkinReady && SpriteBank.Has("ui_ev_scorch") && SpriteBank.Has("ui_ev_smoke_0");
        if (skin)
        {
            BuildSkin(mgr);
            return;
        }

        // B-1: 불길이 앵커 지점에서 타오른다 (달려갈 곳이 보이게)
        fireBox = KitchenEventManager.MakeBox(mgr.CustomRoot, "Fire", new Color(1f, 0.35f, 0.1f, 0.35f));
        fireBox.anchorMin = new Vector2(0.5f, 0.5f);
        fireBox.anchorMax = new Vector2(0.5f, 0.5f);
        fireBox.anchoredPosition = new Vector2(mgr.AnchorCanvasX(), -40f);
        fireBox.sizeDelta = new Vector2(300f, 190f);
        fireBox.GetComponent<Image>().raycastTarget = false;

        fireLabel = KitchenEventManager.MakeText(fireBox, "Label", "화재", 34, new Color(1f, 0.9f, 0.6f));
        RectTransform lrt = fireLabel.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
    }

    /// <summary>v2: 불의 벽 + 연기 (오버레이) / 현장 그을음 + 불길 3 + 연기 2 + 명판 + 칩 (커스텀, fireBox 가 묶음)</summary>
    private void BuildSkin(KitchenEventManager mgr)
    {
        mgr.SetAlarm(KitchenEventSkin.RED, 0.7f);

        // 불의 벽: HUD 위 96px 띠를 마스크로 잘라, 안의 타일 이미지를 아래로 내리면 벽이 낮아진다
        RectTransform maskRt = KitchenEventManager.MakeBox(mgr.OverlayRoot, "FireWallMask", new Color(0f, 0f, 0f, 0f));
        maskRt.anchorMin = new Vector2(0f, 0f); maskRt.anchorMax = new Vector2(1f, 0f);
        maskRt.pivot = new Vector2(0.5f, 0f);
        maskRt.anchoredPosition = new Vector2(0f, 184f);
        maskRt.sizeDelta = new Vector2(0f, WALL_H);
        maskRt.GetComponent<Image>().raycastTarget = false;
        maskRt.gameObject.AddComponent<RectMask2D>();

        wall = KitchenEventManager.MakeBox(maskRt, "FireWall", Color.white);
        wall.anchorMin = Vector2.zero; wall.anchorMax = Vector2.one;
        wall.offsetMin = Vector2.zero; wall.offsetMax = Vector2.zero;
        wallImg = wall.GetComponent<Image>();
        wallImg.sprite = SpriteBank.Get("ui_ev_firewall_0");
        wallImg.type = Image.Type.Tiled;
        wallImg.raycastTarget = false;

        for (int i = 0; i < wallSmoke.Length; i++)
        {
            float x = i == 0 ? -720f : i == 1 ? -260f : 540f;
            wallSmoke[i] = KitchenEventManager.MakeSprite(mgr.OverlayRoot, "WallSmoke_" + i, SpriteBank.Get("ui_ev_smoke_" + (i % 2)), new Vector2(x, -240f), new Vector2(48f, 48f));
            smokeT[i] = i * 0.33f;
        }

        // 현장 묶음 (300x190, 앵커 X 를 매 프레임 따라간다). 자식 좌표 = 목업 v2 의 상자 기준 좌표를 중앙 원점으로 옮긴 값
        fireBox = KitchenEventManager.MakeBox(mgr.CustomRoot, "Fire", new Color(0f, 0f, 0f, 0f));
        fireBox.anchorMin = new Vector2(0.5f, 0.5f);
        fireBox.anchorMax = new Vector2(0.5f, 0.5f);
        fireBox.anchoredPosition = new Vector2(mgr.AnchorCanvasX(), -40f);
        fireBox.sizeDelta = new Vector2(300f, 190f);
        fireBox.GetComponent<Image>().raycastTarget = false;

        KitchenEventManager.MakeSprite(fireBox, "Scorch", SpriteBank.Get("ui_ev_scorch"), new Vector2(0f, -67f), new Vector2(144f, 56f));
        Vector2[] flamePos = { new Vector2(-72f, -41f), new Vector2(0f, -23f), new Vector2(68f, -37f) };
        for (int i = 0; i < 3; i++)
            flames[i] = KitchenEventManager.MakeSprite(fireBox, "Flame_" + i, SpriteBank.Get("ui_ev_fire_" + i), flamePos[i], new Vector2(80f, 96f));
        markerSmoke[0] = KitchenEventManager.MakeSprite(fireBox, "Smoke0", SpriteBank.Get("ui_ev_smoke_0"), new Vector2(-90f, 65f), new Vector2(48f, 48f));
        markerSmoke[1] = KitchenEventManager.MakeSprite(fireBox, "Smoke1", SpriteBank.Get("ui_ev_smoke_1"), new Vector2(40f, 75f), new Vector2(48f, 48f));

        UISkin.Nameplate(fireBox, "Fire", "화재", 14, new Vector2(0.5f, 0.5f), new Vector2(-26f, 89f), 52f);
        KitchenEventManager.MakeChip(fireBox, "Hint", "[E] 꾹 누르기", new Vector2(0f, -117f), KitchenEventSkin.RED);
    }

    public bool OnUpdate(float dt, out bool success)
    {
        success = false;
        elapsed += dt;

        // 플레이테스트 픽스: 화재 박스를 현장 월드 좌표에 매 프레임 재고정
        if (fireBox != null)
            fireBox.anchoredPosition = new Vector2(manager.AnchorCanvasX(), -40f);

        // 불이 꺼질 때까지 기차가 피해를 입는다
        // [수정] 매 프레임 잘게 넣으면 TrainManager 최소데미지(1) 보정 때문에
        // 실제 피해가 수 배로 뻥튀기됨 -> 0.5초 단위 묶음으로 적용
        burnTickTimer += dt;
        if (burnTickTimer >= 0.5f)
        {
            burnTickTimer -= 0.5f;
            manager.DamageTrain(burnDamagePerSec * 0.5f);
        }

        // B-1: 불길 곁에서만 진압 가능 - 떨어져 있으면 불은 계속 번진다
        if (Input.GetKey(KeyCode.E) && KitchenEventManager.ChefInReach) gauge += holdGain * dt;
        else gauge -= releaseLoss * dt;
        gauge = Mathf.Clamp(gauge, 0f, needGauge);

        float t = Mathf.Clamp01(Progress);
        if (skin)
        {
            UpdateSkin(dt, t);
        }
        else if (fireBox != null)
        {
            // 진화될수록 불길이 작아지고 옅어진다 (단색 UI)
            fireBox.sizeDelta = new Vector2(Mathf.Lerp(300f, 110f, t), Mathf.Lerp(190f, 70f, t));
            Image img = fireBox.GetComponent<Image>();
            float flicker = 0.28f + Mathf.PingPong(Time.time * 2.4f, 0.14f);
            img.color = new Color(1f, 0.35f, 0.1f, Mathf.Lerp(flicker, 0.08f, t));
        }

        if (gauge >= needGauge)
        {
            success = true;
            return true;
        }
        return false;
    }

    /// <summary>v2: 불의 벽 프레임/높이, 연기 상승, 현장 불길 3 -> 2 -> 1</summary>
    private void UpdateSkin(float dt, float t)
    {
        if (wallImg != null)
        {
            wallImg.sprite = SpriteBank.Get("ui_ev_firewall_" + KitchenEventSkin.Frame(elapsed, 9f, 3, 0));
            wall.offsetMin = new Vector2(0f, -t * WALL_DROP);
            wall.offsetMax = new Vector2(0f, -t * WALL_DROP);
        }
        int alive = t < 0.34f ? 3 : t < 0.67f ? 2 : 1;
        for (int i = 0; i < wallSmoke.Length; i++)
        {
            if (wallSmoke[i] == null) continue;
            bool show = i < alive;
            wallSmoke[i].enabled = show;
            if (!show) continue;
            smokeT[i] += dt * 0.45f;
            if (smokeT[i] > 1f) smokeT[i] -= 1f;
            Vector2 p = wallSmoke[i].rectTransform.anchoredPosition;
            wallSmoke[i].rectTransform.anchoredPosition = new Vector2(p.x, -260f + smokeT[i] * 110f - t * WALL_DROP);
            wallSmoke[i].color = new Color(1f, 1f, 1f, 1f - smokeT[i] * 0.85f);
        }
        // 현장 불길: 왼쪽 -> 오른쪽 -> 가운데 순서로 꺼진다 (마지막까지 남는 건 가운데)
        if (flames[0] != null) flames[0].enabled = alive >= 3;
        if (flames[2] != null) flames[2].enabled = alive >= 2;
        for (int i = 0; i < 3; i++)
            if (flames[i] != null && flames[i].enabled) flames[i].sprite = SpriteBank.Get("ui_ev_fire_" + KitchenEventSkin.Frame(elapsed, 9f, 3, i));
        if (markerSmoke[0] != null) markerSmoke[0].enabled = alive >= 2;
        if (markerSmoke[1] != null) markerSmoke[1].enabled = alive >= 3;
    }

    public void OnEnd(bool success)
    {
        if (success)
        {
            Debug.Log("[주방이벤트] 화재 진화 성공");
        }
        else
        {
            manager.DamageTrain(50f);   // 시간 초과 시 폭발 피해 (HP 500 기준 조정)
            Debug.Log("[주방이벤트] 화재 진화 실패 - 기차 50 추가 피해");
        }
    }
}


// ======================================================================
//  4. 재료 흘림 - 흩어진 재료를 마우스 좌클릭으로 줍기
// ======================================================================
public class MaterialSpillEvent : IKitchenEvent
{
    private KitchenEventManager manager;
    private int totalCount;
    private int pickedCount;
    private List<Button> items = new List<Button>();

    // v2 스킨: 이름 -> 재료 아이콘 / 계열색
    private static readonly string[] NAMES = MaterialNames.KOR;   // v9.10.1: 재료 이름 한 곳 (고기·등심·전기알·화염꽃·얼음꽃·독샘)
    private static readonly string[] ICONS = { "meat", "armor", "elec", "fire", "ice", "poison" };
    private static readonly FoodTag[] TAGS = { FoodTag.Phys, FoodTag.Def, FoodTag.Elec, FoodTag.Fire, FoodTag.Ice, FoodTag.Poison };

    public string Title { get { return "기차 흔들림! 재료가 바닥에 쏟아졌다"; } }
    public string Guide { get { return "떨어진 재료를 [마우스 좌클릭]으로 전부 주워라   " + pickedCount + " / " + totalCount; } }
    public float TimeLimit { get { return 7.5f; } }
    public float Progress { get { return totalCount > 0 ? (float)pickedCount / totalCount : 0f; } }

    public void OnStart(KitchenEventManager mgr, float difficulty)
    {
        manager = mgr;
        pickedCount = 0;
        items.Clear();

        totalCount = 4 + Mathf.RoundToInt(difficulty * 3f);   // 4 ~ 7개

        bool skin = KitchenEventManager.SkinReady && SpriteBank.Has("ui_mat_meat");
        if (skin)
        {
            // v2: 기차가 흔들린 느낌 - 화면 셰이크 + 약한 경보
            GameFeel.Shake(0.4f);
            mgr.SetAlarm(KitchenEventSkin.RED, 0.3f);
        }

        for (int i = 0; i < totalCount; i++)
        {
            int kind = Random.Range(0, NAMES.Length);
            string label = NAMES[kind];
            Vector2 pos = new Vector2(Random.Range(-620f, 620f), Random.Range(-330f, 60f));
            Button btn = skin
                ? MakeChipButton(mgr, kind, pos)
                : KitchenEventManager.MakeButton(mgr.CustomRoot, label, new Color(0.55f, 0.42f, 0.24f, 0.95f), pos, new Vector2(110f, 72f));

            Button captured = btn;   // 클로저 캡처용 지역 변수 (C# 7.3 필수)
            btn.onClick.AddListener(delegate { OnPick(captured); });
            items.Add(btn);
        }
    }

    /// <summary>v2: 재료 칩 = 평판 + 계열색 테 + 재료 아이콘 + 이름, 살짝 기울어진 채 바닥에 흩어짐. 카드 전체가 버튼</summary>
    private static Button MakeChipButton(KitchenEventManager mgr, int kind, Vector2 pos)
    {
        Image ring;
        RectTransform rt = KitchenEventManager.MakeCard(mgr.CustomRoot, "Btn_" + NAMES[kind], pos, new Vector2(110f, 72f), UIFactory.TagColor(TAGS[kind]), out ring);
        rt.localEulerAngles = new Vector3(0f, 0f, Random.Range(-8f, 8f));
        Image plate = rt.GetComponent<Image>();
        plate.raycastTarget = true;

        Button btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = plate;

        KitchenEventManager.MakeSprite(rt, "Icon", SpriteBank.Get("ui_mat_" + ICONS[kind]), new Vector2(-33f, 0f), new Vector2(32f, 32f));
        Text txt = KitchenEventManager.MakeText(rt, "Label", NAMES[kind], 15, KitchenEventSkin.CREAM);
        RectTransform trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(52f, 0f); trt.offsetMax = new Vector2(-6f, 0f);
        txt.alignment = TextAnchor.MiddleLeft;
        return btn;
    }

    public bool OnUpdate(float dt, out bool success)
    {
        success = false;
        if (pickedCount >= totalCount)
        {
            success = true;
            return true;
        }
        return false;
    }

    public void OnEnd(bool success)
    {
        if (success)
        {
            manager.HealTrain(20f);
            // 보상 다양화 (v3): 바닥을 치우다 여분 재료를 발견
            if (MaterialInventory.Instance != null)
                MaterialInventory.Instance.Add((MaterialType)Random.Range(0, 6), 1);
            Debug.Log("[주방이벤트] 재료 전부 회수 성공 + 여분 재료 1");
        }
        else
        {
            int lost = totalCount - pickedCount;
            manager.DamageTrain(10f * lost);   // 못 주운 재료가 기계에 끼어 피해 (HP 500 기준 조정)
            Debug.Log("[주방이벤트] 재료 회수 실패 - 미회수 " + lost + "개 / 기차 " + (10 * lost) + " 피해");
        }
    }

    /// <summary>재료 하나를 주웠을 때</summary>
    private void OnPick(Button btn)
    {
        if (btn == null || !btn.gameObject.activeSelf) return;
        btn.gameObject.SetActive(false);
        pickedCount++;
    }
}
