using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [UISkin.cs] v1.1 - "쇳냄새" 픽셀 UI 스킨 (2026-09-07, HUD 목업 v3 컨펌)
/// - v1.1 (v9.5): Relabel(명판 글자 바꾸기 + 폭 재계산) 추가 - 조리 미니게임 제목 명판용. 그 외 변경 없음
///
/// Resources/Sprites/WDT/ui_*.png (파이프 프레임 / 무쇠 평판 / 테 / 버튼 / 황동 명판 / 위험 스트라이프 / 게이지 / 장식)를
/// 코드 생성 UI 전부에 입힌다. 세 갈래:
///   1) API  : UIFactory.CreatePanel/CreateButton, GameHUD 가 직접 호출 (Pipe / Plate / Ring / ButtonSkin / Nameplate / Gauge / Ornament)
///   2) 스캐너: 0.15초마다 캔버스의 Image를 훑어 아직 스킨이 없는 "단색 박스"에 자동 적용
///             (증강/상점/정비소/선로 카드/일시정지/일지/베팅 등 KitchenEventManager.MakeBox 계열 - 파일 무수정)
///             규칙: Button 달린 것 = 버튼 / 320x300 또는 600x150 이상 = 파이프 프레임(조상에 파이프 있으면 카드) / 100x36 이상 = 카드(평판+테)
///                   제외: 이름에 Dim/Track/Zone/Cursor/Fill/Band/Dot/Edge/Row/Viewport/Scroll/Mask/Bar/Gauge/Icon/Marker/Line/Handle/Good/Perfect/BG/Top,
///                        알파 0.5 미만, 화면 90% 이상 덮는 것(암전), 슬라이더 부품, 이미 스프라이트가 있는 것
///                   스킨된 부모를 거의 꽉 채우는 자식 박스(증강 카드 "Inner" 등)는 숨긴다 (부모 평판이 속지)
///   3) 씬 HUD 재배치: [HUD Canvas]의 HPBar/HPText/GoldText/WaveText/StateText/WaveNoticeText를
///             파이프 패널(좌상단 HP 게이지, 우상단 웨이브 명판·골드·상태) 안으로 옮기고
///             웨이브 예고/안내 텍스트에는 글자 크기에 맞춰 스스로 커지는 카드 배경을 깔아 준다 (글자가 테두리에 붙지 않는다)
///             (씬 잔재 SatietyBar/SatietyText는 끈다 - 포만감 시스템은 v2에서 제거됨)
/// 끄기: ENABLED = false (모든 UI가 예전 단색 박스로 돌아간다). ui_pipe.png 가 없어도 자동으로 꺼진다
/// 사용법: 없음! 파일만 넣으면 게임 시작 시 스스로 생성된다. (SpriteBank.cs + ui_*.png + UIFactory.cs v4 필요)
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class UISkin : MonoBehaviour
{
    public static bool ENABLED = true;                    // 스킨 전체 스위치 (false = v3 단색 박스 UI)
    private const float SCAN_INTERVAL = 0.15f;
    private const float REF_W = 1920f, REF_H = 1080f;    // CanvasScaler 기준 해상도 (전 캔버스 공통)

    // 팔레트 (기차와 동일 계열)
    public static readonly Color IRON = new Color(0.204f, 0.196f, 0.243f, 1f);      // 무쇠 평판
    public static readonly Color IRON_LIGHT = new Color(0.30f, 0.29f, 0.35f, 1f);
    public static readonly Color BRASS = new Color(0.84f, 0.667f, 0.282f, 1f);      // 황동
    public static readonly Color BRASS_DIM = new Color(0.55f, 0.47f, 0.32f, 1f);    // 흐린 황동 (카드 기본 테)
    public static readonly Color INK = new Color(0.118f, 0.086f, 0.063f, 1f);       // 명판 글자
    public static readonly Color CREAM = new Color(0.969f, 0.910f, 0.776f, 1f);
    public static readonly Color HP_RED = new Color(0.84f, 0.20f, 0.22f, 1f);
    public static readonly Color GAUGE_GOLD = new Color(0.886f, 0.698f, 0.227f, 1f);

    private static readonly string[] EXCLUDE = { "Dim", "Track", "Zone", "Cursor", "Fill", "Band", "Dot", "Edge", "Row",
        "Viewport", "Scroll", "Mask", "Bar", "Gauge", "Icon", "Marker", "Line", "Handle", "Skin", "Nameplate", "Ornament",
        "Good", "Perfect", "BG", "Top" };     // Good/Perfect = 타이밍 구간, BG = 바 배경, Top = 카드 머리띠 (Band 와 같은 역할)

    private static bool loaded, available;
    private static Sprite pipe, plate, ring, btn, nameplate, hazard, gaugeBg, gaugeFill, gaugeRound, valve, vent;
    private static Sprite pipeV;   // 세로 파이프 조각 (ui_pipe 에서 잘라 만든다 - PipeVertical)
    private static UISkin instance;

    /// <summary>스킨 적용 여부 표시 (kind: 0=건너뜀 1=파이프 2=카드 3=버튼 4=직접 지정)</summary>
    public class Mark : MonoBehaviour { public int kind; }

    // ─────────────────────────────────────────────
    // 로드 / 가용성
    // ─────────────────────────────────────────────
    public static bool Available
    {
        get { Load(); return available; }
    }

    private static void Load()
    {
        if (loaded) return;
        loaded = true;
        if (!ENABLED) { available = false; return; }
        pipe = SpriteBank.Get("ui_pipe"); plate = SpriteBank.Get("ui_plate");
        ring = SpriteBank.Get("ui_ring"); btn = SpriteBank.Get("ui_button");
        nameplate = SpriteBank.Get("ui_nameplate"); hazard = SpriteBank.Get("ui_hazard");
        gaugeBg = SpriteBank.Get("ui_gauge_bg"); gaugeFill = SpriteBank.Get("ui_gauge_fill");
        gaugeRound = SpriteBank.Get("ui_gauge_round"); valve = SpriteBank.Get("ui_valve"); vent = SpriteBank.Get("ui_vent");
        available = pipe != null && plate != null && ring != null && btn != null && nameplate != null;
        if (!available) Debug.Log("[UISkin] ui_*.png 없음 - 단색 박스 UI 유지");
    }

    public static Sprite Ornament(string name)
    {
        Load();
        if (name == "gauge") return gaugeRound;
        if (name == "valve") return valve;
        if (name == "vent") return vent;
        if (name == "hazard") return hazard;
        return null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null || !Available) return;
        GameObject go = new GameObject("UISkin");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<UISkin>();
        Debug.Log("[UISkin] 픽셀 UI 스킨 준비 (파이프 프레임 + 무쇠 평판)");
    }

    // ─────────────────────────────────────────────
    // 스타일 API (UIFactory / GameHUD 가 직접 호출)
    // ─────────────────────────────────────────────
    /// <summary>파이프 프레임 (타일 반복: 커플링·철판이 크기에 따라 늘어난다). 색은 흰색 고정</summary>
    public static void Pipe(Image img)
    {
        img.sprite = pipe; img.type = Image.Type.Tiled; img.color = Color.white; SetMark(img, 1);
    }

    /// <summary>세로 파이프 구분선 (ui_pipe 왼쪽 테의 커플링 조각 28x40 을 세로로 타일). 폭 28 로 쓴다</summary>
    public static void PipeVertical(Image img)
    {
        if (pipeV == null && pipe != null)
        {
            Rect r = pipe.rect;   // 텍스처 안의 스프라이트 영역 (아틀라스에 묶여도 안전)
            pipeV = Sprite.Create(pipe.texture, new Rect(r.x, r.y + 28f, 28f, r.height - 56f),
                new Vector2(0.5f, 0.5f), pipe.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            pipeV.name = "ui_pipe_v";
        }
        img.sprite = pipeV; img.type = Image.Type.Tiled; img.color = Color.white; SetMark(img, 1);
    }

    /// <summary>무쇠 평판 (틴트 가능 - Color.white = 기본 무쇠)</summary>
    public static void Plate(Image img, Color tint)
    {
        img.sprite = plate; img.type = Image.Type.Tiled; img.color = tint; SetMark(img, 2);
    }

    /// <summary>테 (가운데 투명, 리벳 4) - 색 있는 카드 테두리</summary>
    public static void Ring(Image img, Color tint)
    {
        img.sprite = ring; img.type = Image.Type.Sliced; img.color = tint; SetMark(img, 2);
    }

    /// <summary>버튼 판 (틴트 = 버튼 색)</summary>
    public static void ButtonSkin(Image img, Color tint)
    {
        img.sprite = btn; img.type = Image.Type.Sliced; img.color = tint; SetMark(img, 3);
    }

    /// <summary>호스트 안에 꽉 찬 테 자식 추가 (글자 아래로 가도록 첫 번째 자식). inset = 안쪽으로 들여쓰기 (파이프 안쪽이면 28)</summary>
    public static Image AddRing(RectTransform host, Color tint, float inset = 0f)
    {
        GameObject go = new GameObject("SkinRing");
        go.transform.SetParent(host, false);
        go.transform.SetAsFirstSibling();
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        Ring(img, tint);
        return img;
    }

    /// <summary>황동 명판 + 글자. 앵커/피벗은 왼쪽 위 기준, pos = 앵커 기준 위치. 폭은 글자 수로 어림 (한글 기준)</summary>
    public static RectTransform Nameplate(Transform parent, string name, string label, int fontSize, Vector2 anchor, Vector2 pos, float width = 0f)
    {
        GameObject go = new GameObject("Nameplate_" + name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0f, 1f);
        if (width <= 0f) width = EstimateWidth(label, fontSize) + 28f;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, 32f);
        Image img = go.AddComponent<Image>();
        img.sprite = nameplate; img.type = Image.Type.Sliced; img.color = Color.white; img.raycastTarget = false;
        SetMark(img, 4);
        Text t = UIFactory.CreateText(go.transform, "Label", label, fontSize, INK, TextAnchor.MiddleCenter);
        t.rectTransform.offsetMin = new Vector2(6f, 0f); t.rectTransform.offsetMax = new Vector2(-6f, 0f);
        return rt;
    }

    /// <summary>명판 글자 바꾸기 - 폭도 새 글자 수에 맞춰 다시 잡는다 (조리 미니게임 제목처럼 내용이 바뀌는 명판용)</summary>
    public static void Relabel(RectTransform plate, string label, int fontSize)
    {
        if (plate == null) return;
        Transform lt = plate.Find("Label");
        Text t = lt != null ? lt.GetComponent<Text>() : null;
        if (t != null) { t.text = label; t.fontSize = fontSize; }
        plate.sizeDelta = new Vector2(EstimateWidth(label, fontSize) + 28f, plate.sizeDelta.y);
    }

    /// <summary>장식 스프라이트 (게이지/밸브/그릴/위험 스트라이프). 앵커 기준 pos, 크기는 원본 픽셀</summary>
    public static Image AddOrnament(Transform parent, string which, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        Sprite s = Ornament(which);
        if (s == null) return null;
        GameObject go = new GameObject("Ornament_" + which);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.sprite = s; img.type = which == "hazard" ? Image.Type.Tiled : Image.Type.Simple; img.raycastTarget = false;
        SetMark(img, 4);
        return img;
    }

    /// <summary>슬라이더를 유리관 게이지로 (배경 = 황동 캡 튜브, 채움 = 틴트)</summary>
    public static void Gauge(Slider s, Color fill)
    {
        if (s == null) return;
        Transform bg = s.transform.Find("Background");
        if (bg != null)
        {
            Image bi = bg.GetComponent<Image>();
            if (bi != null) { bi.sprite = gaugeBg; bi.type = Image.Type.Sliced; bi.color = Color.white; SetMark(bi, 4); }
            RectTransform brt = bg as RectTransform;
            if (brt != null) { brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero; }
        }
        Transform fa = s.transform.Find("Fill Area");
        if (fa != null)
        {
            RectTransform frt = fa as RectTransform;
            if (frt != null) { frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = new Vector2(6f, 6f); frt.offsetMax = new Vector2(-6f, -6f); }
            Transform f = fa.Find("Fill");
            if (f != null)
            {
                Image fi = f.GetComponent<Image>();
                if (fi != null) { fi.sprite = gaugeFill; fi.type = Image.Type.Sliced; fi.color = fill; SetMark(fi, 4); }
                RectTransform fr = f as RectTransform;
                if (fr != null) { fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero; }
            }
        }
        Transform h = s.transform.Find("Handle Slide Area");
        if (h != null) h.gameObject.SetActive(false);
        s.interactable = false;
    }

    private static void SetMark(Image img, int kind)
    {
        Mark m = img.GetComponent<Mark>();
        if (m == null) m = img.gameObject.AddComponent<Mark>();
        m.kind = kind;
    }

    private static float EstimateWidth(string s, int fontSize)
    {
        float w = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == ' ') w += fontSize * 0.32f;
            else if (c < 128) w += fontSize * 0.58f;
            else w += fontSize * 1.0f;
        }
        return w;
    }

    // ─────────────────────────────────────────────
    // 스캐너 + 씬 HUD 재배치
    // ─────────────────────────────────────────────
    private float timer;
    private bool hudDone;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(SkinSceneHudLater());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        hudDone = false;
        StartCoroutine(SkinSceneHudLater());
    }

    private void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer < SCAN_INTERVAL) return;
        timer = 0f;
        Scan();
    }

    private readonly List<Image> pending = new List<Image>();
    private readonly List<int> pendingDepth = new List<int>();

    private void Scan()
    {
        Image[] images = FindObjectsByType<Image>(FindObjectsSortMode.None);
        pending.Clear(); pendingDepth.Clear();
        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];
            if (img == null || img.GetComponent<Mark>() != null) continue;
            pending.Add(img); pendingDepth.Add(Depth(img.transform));
        }
        // 부모를 먼저 분류해야 자식 규칙(파이프 조상, 꽉 찬 속판)이 같은 스캔에서 맞게 돈다 - 얕은 것부터 (삽입 정렬, 수십 개 수준)
        for (int i = 1; i < pending.Count; i++)
        {
            Image img = pending[i]; int d = pendingDepth[i]; int j = i - 1;
            while (j >= 0 && pendingDepth[j] > d) { pending[j + 1] = pending[j]; pendingDepth[j + 1] = pendingDepth[j]; j--; }
            pending[j + 1] = img; pendingDepth[j + 1] = d;
        }
        for (int i = 0; i < pending.Count; i++)
            Classify(pending[i]);
    }

    private static int Depth(Transform t)
    {
        int d = 0;
        while (t.parent != null) { d++; t = t.parent; }
        return d;
    }

    private void Classify(Image img)
    {
        // 이미 그림이 있는 것(스프라이트 아트, 우리 스킨)은 건드리지 않는다
        if (img.sprite != null && img.sprite.name != "UISprite" && img.sprite.name != "Background") { SetMark(img, 0); return; }
        if (img.GetComponentInParent<Slider>() != null) { SetMark(img, 0); return; }
        string n = img.gameObject.name;
        for (int i = 0; i < EXCLUDE.Length; i++)
            if (n.Contains(EXCLUDE[i])) { SetMark(img, 0); return; }

        RectTransform rt = img.rectTransform;
        Rect r = rt.rect;
        if (r.width <= 1f && r.height <= 1f) return;              // 아직 레이아웃 전 - 다음 스캔에
        Color c = img.color;

        // 스킨 입힌 부모를 거의 꽉 채우는 자식(예: 증강 카드의 "Inner" 어두운 속판)은 숨긴다 - 부모의 평판이 곧 속지
        if (IsNearFullChildOfSkinned(rt)) { img.enabled = false; SetMark(img, 0); return; }

        Button b = img.GetComponent<Button>();
        if (b != null)
        {
            Color bc = c; bc.a = 1f;
            if (bc.r + bc.g + bc.b < 0.45f) bc = IRON_LIGHT;         // 너무 어두운 버튼은 무쇠로
            ButtonSkin(img, bc);
            return;
        }
        if (c.a < 0.5f) { SetMark(img, 0); return; }
        if (r.width >= REF_W * 0.9f && r.height >= REF_H * 0.9f) { SetMark(img, 0); return; }   // 암전

        bool saturated = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b)) > 0.2f;
        Color ringColor = saturated ? new Color(c.r, c.g, c.b, 1f) : BRASS_DIM;

        bool pipeSize = (r.width >= 320f && r.height >= 300f) || (r.width >= 600f && r.height >= 150f);   // 파이프 테 28px 가 답답하지 않을 크기
        if (pipeSize && !HasPipeAncestor(rt))
        {
            Pipe(img);
            if (saturated) AddRing(rt, ringColor, 28f);               // 등급색 등은 파이프 안쪽 테로 남긴다
            return;
        }
        if (r.width >= 100f && r.height >= 36f)
        {
            Plate(img, Color.white);
            AddRing(rt, ringColor);
            return;
        }
        SetMark(img, 0);
    }

    /// <summary>부모가 파이프/카드로 스킨됐고, 이 사각형이 부모보다 사방 12px 이내로만 작은가</summary>
    private static bool IsNearFullChildOfSkinned(RectTransform rt)
    {
        RectTransform p = rt.parent as RectTransform;
        if (p == null) return false;
        Mark m = p.GetComponent<Mark>();
        if (m == null || (m.kind != 1 && m.kind != 2)) return false;
        Rect pr = p.rect;
        return pr.width - rt.rect.width <= 24f && pr.height - rt.rect.height <= 24f;
    }

    private static bool HasPipeAncestor(Transform t)
    {
        Transform p = t.parent;
        while (p != null)
        {
            Mark m = p.GetComponent<Mark>();
            if (m != null && m.kind == 1) return true;
            p = p.parent;
        }
        return false;
    }

    // ─────────────────────────────────────────────
    // 씬 HUD ([HUD Canvas]) 재배치: HP/포만감 게이지 패널 + 웨이브/골드 패널 + 웨이브 예고 카드
    // ─────────────────────────────────────────────
    private IEnumerator SkinSceneHudLater()
    {
        yield return null;            // TrainDeck 의 HPBar 조정보다 뒤에
        yield return null;
        if (hudDone) yield break;
        GameObject hudGo = GameObject.Find("[HUD Canvas]");
        if (hudGo == null) yield break;
        hudDone = true;
        Transform root = hudGo.transform;

        UIManager um = UIManager.Instance;

        // ── 좌상단: HP 게이지 (파이프 안쪽 28px 를 피해 세로 가운데. 포만감 시스템은 v2에서 제거 - 씬 잔재는 끈다) ──
        RectTransform tl = MakePanel(root, "SkinPanel_TL", new Vector2(0f, 1f), new Vector2(8f, -8f), new Vector2(470f, 112f));
        Nameplate(tl, "HP", "HP", 16, new Vector2(0f, 1f), new Vector2(22f, -40f), 56f);
        Slider hp = um != null && um.hpSlider != null ? um.hpSlider : FindSlider("HPBar");
        if (hp != null) { Dock(hp.transform as RectTransform, tl, new Vector2(88f, -40f), new Vector2(300f, 32f)); Gauge(hp, HP_RED); }
        if (um != null && um.hpText != null && hp != null) DockTextInto(um.hpText, hp.transform, 15f);
        AddOrnament(tl, "gauge", new Vector2(1f, 1f), new Vector2(-64f, -28f), new Vector2(56f, 56f));
        GameObject satBar = GameObject.Find("SatietyBar"); if (satBar != null) satBar.SetActive(false);
        GameObject satText = GameObject.Find("SatietyText"); if (satText != null) satText.SetActive(false);

        // ── 우상단: 웨이브(파이프에 걸린 황동 명판) / 골드 / 상태 ──
        RectTransform tr = MakePanel(root, "SkinPanel_TR", new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(330f, 112f));
        if (um != null)
        {
            RectTransform wavePlate = Nameplate(tr, "Wave", "", 16, new Vector2(0f, 1f), new Vector2(26f, 4f), 170f);   // 파이프 위에 4px 걸림
            DockTmpInto(um.waveText, wavePlate, 16f, INK);
            DockTmp(um.goldText, tr, new Vector2(26f, -34f), new Vector2(220f, 28f), 20f, CREAM);
            DockTmp(um.stateText, tr, new Vector2(26f, -62f), new Vector2(220f, 22f), 14f, new Color(0.63f, 0.55f, 0.43f, 1f));
        }
        AddOrnament(tr, "vent", new Vector2(1f, 1f), new Vector2(-92f, -54f), new Vector2(64f, 24f));

        // ── 웨이브 예고 / 안내: 글자 크기에 맞춰 커지는 카드 (좌우 80·상하 12 여백, 안내 카드는 예고 카드 아래에 따라붙는다) ──
        if (um != null)
        {
            NoticeFollower notice = WrapNotice(um.waveNoticeText, root, "SkinNotice", new Vector2(0f, -96f),
                new Vector2(820f, 66f), new Vector2(80f, 12f), true);
            NoticeFollower warning = WrapNotice(um.waveWarningText, root, "SkinWarning", new Vector2(0f, -176f),
                new Vector2(700f, 50f), new Vector2(48f, 10f), false);
            if (notice != null && warning != null) { warning.above = notice.transform as RectTransform; warning.gap = 10f; }
        }
        Debug.Log("[UISkin] 씬 HUD 재배치 완료 (HP 게이지, 웨이브/골드 패널, 예고 카드)");
    }

    private RectTransform MakePanel(Transform root, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.SetAsFirstSibling();                 // 다른 HUD 요소보다 뒤(아래)에
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        Pipe(img);
        return rt;
    }

    private static Slider FindSlider(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.GetComponent<Slider>() : null;
    }

    /// <summary>RectTransform을 패널 안 왼쪽 위 기준 좌표로 옮긴다</summary>
    private static void Dock(RectTransform rt, RectTransform panel, Vector2 pos, Vector2 size)
    {
        if (rt == null) return;
        rt.SetParent(panel, false);
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        rt.localScale = Vector3.one;
    }

    /// <summary>씬 TMP 텍스트(HPText)를 게이지 안 가운데로</summary>
    private static void DockTextInto(TMPro.TextMeshProUGUI tmp, Transform host, float fontSize)
    {
        DockTmpInto(tmp, host, fontSize, CREAM);
    }

    /// <summary>씬 TMP 텍스트를 호스트(게이지/명판) 안에 꽉 채워 가운데 정렬</summary>
    private static void DockTmpInto(TMPro.TextMeshProUGUI tmp, Transform host, float fontSize, Color color)
    {
        if (tmp == null || host == null) return;
        RectTransform rt = tmp.rectTransform;
        rt.SetParent(host, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.SetAsLastSibling();
        tmp.fontSize = fontSize; tmp.alignment = TMPro.TextAlignmentOptions.Center; tmp.color = color;
    }

    private static void DockTmp(TMPro.TextMeshProUGUI tmp, RectTransform panel, Vector2 pos, Vector2 size, float fontSize, Color color)
    {
        if (tmp == null) return;
        Dock(tmp.rectTransform, panel, pos, size);
        tmp.fontSize = fontSize; tmp.alignment = TMPro.TextAlignmentOptions.Left; tmp.color = color;
    }

    /// <summary>
    /// 예고 텍스트에 카드 배경(무쇠 평판 + 황동 테)을 깔고(부모로) 여백을 준다.
    /// 카드는 글자 길이/크기에 맞춰 스스로 커지고(minSize 이상, 화면 폭 이하), 텍스트의 표시/알파를 따라간다.
    /// padding = (좌우 여백, 상하 여백). 좌우 여백 안에 위험 스트라이프(56px)가 들어간다
    /// </summary>
    private static NoticeFollower WrapNotice(TMPro.TextMeshProUGUI tmp, Transform root, string name, Vector2 pos,
        Vector2 minSize, Vector2 padding, bool hazardEnds)
    {
        if (tmp == null) return null;
        GameObject go = new GameObject(name);
        go.transform.SetParent(root, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos; rt.sizeDelta = minSize;
        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        Plate(img, Color.white);
        AddRing(rt, BRASS);
        if (hazardEnds)
        {
            // 카드 높이가 바뀌어도 세로 가운데를 유지하도록 좌우 가운데 앵커 (피벗이 왼쪽 위라 y=+8 이 가운데)
            AddOrnament(rt, "hazard", new Vector2(0f, 0.5f), new Vector2(14f, 8f), new Vector2(56f, 16f));
            AddOrnament(rt, "hazard", new Vector2(1f, 0.5f), new Vector2(-70f, 8f), new Vector2(56f, 16f));
        }
        RectTransform trt = tmp.rectTransform;
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(padding.x, padding.y); trt.offsetMax = new Vector2(-padding.x, -padding.y);
        trt.localScale = Vector3.one;
        trt.SetAsLastSibling();
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        NoticeFollower f = go.AddComponent<NoticeFollower>();
        f.target = tmp; f.minSize = minSize; f.padding = padding;
        return f;
    }

    /// <summary>
    /// 예고 카드 동작: (1) 글자 길이/크기에 맞춰 카드 크기 조절 (2) 텍스트의 활성/알파를 따라감 (UIManager 코루틴 무수정)
    /// (3) above 가 있으면 그 카드 바로 아래에 붙는다 (예고가 두 줄이 되어도 안내가 겹치지 않는다)
    /// </summary>
    public class NoticeFollower : MonoBehaviour
    {
        public TMPro.TextMeshProUGUI target;
        public Vector2 minSize;                 // 카드 최소 크기
        public Vector2 padding;                 // 글자 주변 여백 (좌우, 상하)
        public RectTransform above;             // 이 카드 위에 있는 카드 (있으면 그 아래에 따라붙는다)
        public float gap = 10f;
        private const float MAX_W = 1500f;      // 카드 최대 폭 (1920 기준 화면 안)
        private Image[] parts;
        private string lastText;
        private float lastFontSize = -1f;

        private void LateUpdate()
        {
            if (target == null) { gameObject.SetActive(false); return; }
            bool on = target.gameObject.activeSelf && !string.IsNullOrEmpty(target.text);
            if (on && (target.text != lastText || target.fontSize != lastFontSize)) Fit();

            RectTransform rt = transform as RectTransform;
            if (above != null)
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, above.anchoredPosition.y - above.sizeDelta.y - gap);

            if (parts == null) parts = GetComponentsInChildren<Image>(true);
            float a = on ? target.color.a : 0f;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == null) continue;
                Color c = parts[i].color; c.a = a; parts[i].color = c;
            }
        }

        /// <summary>글자가 한 줄로 들어가는 폭(여백 포함)으로 넓히고, 그 폭에서 줄바꿈된 높이만큼 키운다</summary>
        private void Fit()
        {
            lastText = target.text; lastFontSize = target.fontSize;
            float w = Mathf.Clamp(target.preferredWidth + padding.x * 2f, minSize.x, MAX_W);
            float innerW = w - padding.x * 2f;
            float textH = target.GetPreferredValues(target.text, innerW, 0f).y;
            float h = Mathf.Max(minSize.y, textH + padding.y * 2f);
            (transform as RectTransform).sizeDelta = new Vector2(w, h);
        }
    }
}
