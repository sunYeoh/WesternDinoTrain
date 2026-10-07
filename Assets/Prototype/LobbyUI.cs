using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [LobbyUI.cs] v1.10 (v9.20 2026-10-07: 글 속 화살표를 게임 글꼴에 있는 세모로(게임 글꼴에 화살표 글리프가 없어 PC 의 다른 글꼴로 그려졌다 - 모양이 따로 놀고, 대신할 글꼴이 없는 빌드에선 빈칸)) / v1.9 (v9.17 2026-10-06 D6: 출발 때 검정 페이드 - 덮인 사이에 로비 -> 운행) / v1.8 (v9.15 2026-09-29: 왼쪽 아래 소리·화면 줄을 [설정] 버튼 하나로 - SettingsUI(화면·소리·언어). 화면 모드 코드는 SettingsUI 로 이동) / v1.7 (v9.14 2026-09-28 테스터 반영: 화면 설정 줄(전체화면 / 창 1920·1600·1280, PlayerPrefs WDT_ScreenMode, 시작 때 적용) / 도감은 클릭으로 고정(다시 클릭 = 닫기, 마우스 스침 무시) + 설명 짧게(맛 문구 제외)) / v1.6 (v9.13.1 2026-09-24: 오른쪽 아래 빌드 표시 GameBalance.BuildTag) / v1.5 (v9.12 2026-09-22: [T] = 견습 기록이 없으면 견습 운행 전부, 있으면 훈련장(TrainingGroundUI) 목록 / 처음 실행이면 [출발]·[Enter] 도 견습부터(GameBalance.TutorialForceFirst) / 버튼 글자 "훈련장") / v1.4 (v9.10.1 2026-09-21: 재료 이름 MaterialNames) / [LobbyUI.cs] v1.3 (v9.10 2026-09-17: 요리 도감에 설명 상자 - 이름에 마우스를 올리거나 클릭하면 무엇을 하나·어떤 손님에·언제 (RecipeText)) / v1.2 (v9.9 2026-09-16: [T] 견습 운행 버튼 + 첫 실행 강조) / v1.1 (v9.8: 칭호 표시) / v1 - 로비 개편 (튜토리얼_온보딩_설계 6절 + 화면 검수 "시작 버튼 묻힘")
///
/// - v1.2: 출발 버튼 아래 [T] 견습 운행 (340x44, y 130). 미완료(TutorialDirector.Done == false)면 목업 v2 (C) 대로
///   위에 현장 마커 화살표(tut_arrow 2배)가 까딱이고, 버튼 양끝 경광등(ui_ev_beacon_0/1)이 0.3초마다 교대, 황동 테,
///   오른쪽 황동 명판 "← 처음이면 이것부터". 완료면 "[T] 견습 운행 - 다시 보기" 만.
///   자리 확보: 출발 y 156 -> 236, 상점/도감 y 98 -> 80, 안내줄 52~78 -> 28~50.
///   로비 캔버스는 DontDestroyOnLoad - 씬 리로드(런 포기/견습 종료) 뒤에도 로비 UI 가 남는다 (v1.1 까지는 사라졌다)
/// - v1.1: 부제 아래에 칭호 줄. 도감 42종을 완성한 채 엔딩 B를 본 요리사(MetaProgress.MasterChefTitle)에게만
///   "황야의 마스터 셰프" 칭호가 뜬다 (교수 피드백 C1 - 도감 완성의 명예 보상). 로비에 들어올 때마다 갱신
///
/// 로비를 게임의 대문으로 만든다:
/// - 상단: 타이틀 + 부제
/// - 하단: 큰 [출발한다!] 버튼 (클릭 또는 [Enter])
/// - 안내줄: [M] 명성 상점 / [J] 선대의 일지 / [H] 차장의 안내 일지
/// - 좌하단: [설정] 버튼 (v1.8 - SettingsUI: 화면 / 소리 / 언어)
/// - 우하단: 서체 라이선스 고지 (백로그 "로비 크레딧" 항목)
/// 명성 상점(중앙 패널)은 그대로 두고 이 화면이 위아래로 감싼다.
/// 씬의 구 lobbyPanel(어두운 배경에 묻힌 시작 버튼)은 자동 숨김 (HideLegacyLobbyPanel).
///
/// 사용법: 없음! 파일만 넣으면 자동 생성된다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class LobbyUI : MonoBehaviour
{
    private static LobbyUI instance;

    private Canvas canvas;
    private GameObject root;      // 로비에서만 켜는 묶음
    private Text titleBadge;      // v1.1: 칭호 줄 (없으면 빈 글자)
    private bool wasLobby = false;

    // v1.2: [T] 견습 운행 버튼 + 강조 부품
    private Button tutorialBtn;
    private Text tutorialLabel;
    private Image tutorialRing;          // 황동 테 (스킨 있을 때)
    private Image beaconL, beaconR;      // 경광등 2개
    private Image tutorialArrow;         // 마커 화살표
    private RectTransform tutorialHint;  // 명판 "← 처음이면 이것부터"
    private Text tutorialHintFallback;
    private Sprite beaconOff, beaconOn;
    private bool highlightOn = false;
    private const float TUT_BTN_Y = 130f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("LobbyUI");
        DontDestroyOnLoad(go);
        go.AddComponent<LobbyUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        SettingsUI.ApplySavedScreenMode();   // v1.7: 저장된 화면 모드 (0 = 전체화면 = 기본이라 손대지 않음). v1.8: SettingsUI 로 이동
        BuildUI();
    }

    // v1.8: 화면 모드 코드는 SettingsUI 로 옮겼다 (SettingsUI.ApplySavedScreenMode / Toggle)

    private void Update()
    {
        bool lobby = GameManager.Instance != null
            && GameManager.Instance.currentState == GameManager.GameState.Lobby;

        if (root != null && root.activeSelf != lobby)
            root.SetActive(lobby);
        // v1.1: 로비에 들어오는 순간 칭호 갱신 (엔딩 B 직후 돌아왔을 때 바로 보이게)
        if (lobby && !wasLobby) { RefreshTitleBadge(); RefreshTutorialButton(); }
        wasLobby = lobby;
        if (!lobby)
        {
            // 로비를 떠나면 도감도 닫는다 (출발 후 화면에 남지 않게)
            if (collectionRoot != null) { Destroy(collectionRoot.gameObject); collectionRoot = null; }
            return;
        }

        TickTutorialHighlight();

        // v1.9 (D6): 화면이 덮이는 중엔 로비 키([T]·[Enter])를 받지 않는다 (출발 페이드 0.3초 사이에 [T] 가 먹던 것 방지).
        //   아래 구 로비 패널 숨김은 덮이는 중에도 돌아야 한다 - 씬을 다시 실은 직후 페이드 사이로 구 패널이 비친다
        bool keysOk = !ScreenFx.Covering;

        // v1.2: [T] 견습 운행 (일지/일시정지/브리핑이 열려 있으면 양보). v1.5: 훈련장 창이 떠 있으면 그 창이 T 를 닫기로 쓴다
        if (keysOk && Input.GetKeyDown(KeyCode.T) && !JournalViewerUI.IsOpen && !PauseMenu.IsOpen && !BriefingUI.IsOpen
            && !AugmentListUI.ReadingOpen && !FameShopUI.IsOpen && !TrainingGroundUI.IsOpen && !SettingsUI.IsOpen)
            StartTutorial();

        // 구 씬 로비 패널 숨김 (Uimanager.ShowOnlyPanel이 다시 켜도 매 프레임 꺼서 유지)
        if (GameBalance.HideLegacyLobbyPanel && UIManager.Instance != null
            && UIManager.Instance.lobbyPanel != null
            && UIManager.Instance.lobbyPanel.activeSelf)
            UIManager.Instance.lobbyPanel.SetActive(false);

        // [Enter] 출발 (일지/일시정지/훈련장이 열려 있으면 양보)
        if (keysOk && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            && !JournalViewerUI.IsOpen && !PauseMenu.IsOpen && !TrainingGroundUI.IsOpen && !SettingsUI.IsOpen
            && !BriefingUI.IsOpen && BriefingUI.KeyConsumedFrame != Time.frameCount)   // v1.2: 카드를 닫은 Enter 로 출발하지 않게
            StartRun();
    }

    /// <summary>v1.1: 칭호 줄 갱신 - 도감 완성 + 엔딩 B 기록이 있을 때만 표시</summary>
    private void RefreshTitleBadge()
    {
        if (titleBadge == null) return;
        titleBadge.text = MetaProgress.MasterChefTitle
            ? "[칭호]  황야의 마스터 셰프  -  요리책 " + GameBalance.TrueEndingRecipesNeeded + "종을 완성하고 마지막 손님을 대접한 요리사"
            : "";
    }

    private void StartRun()
    {
        // v1.5: 처음 실행이면 출발 대신 견습 운행부터 (끝나면 로비로 돌아와 바로 출발할 수 있다)
        if (TutorialDirector.MustPlayFirst)
        {
            if (collectionRoot != null) { Destroy(collectionRoot.gameObject); collectionRoot = null; }
            UIManager.Instance?.ShowStatChange("처음이니 견습 운행부터 - 끝나면 바로 출발할 수 있다");
            Debug.Log("[LobbyUI] 처음 실행 - 출발 대신 견습 운행 (TutorialForceFirst)");
            TutorialDirector.Begin();
            return;
        }
        if (ScreenFx.Covering) return;   // v1.9: 이미 넘어가는 중 ([Enter] 연타)
        SoundManager.Play("sfx_train_whistle");   // 출발 기적 (클립 없으면 무시)
        // v1.9 (D6): 검정 페이드로 덮인 사이에 로비 -> 운행 (SceneFadeSec 0 = 바로)
        ScreenFx.Cover(Color.black, GameBalance.SceneFadeSec, GameBalance.SceneFadeSec, delegate
        {
            if (UIManager.Instance != null) UIManager.Instance.OnClickStartGame();
            else GameManager.Instance?.ChangeState(GameManager.GameState.Battle);
        });
        Debug.Log("[LobbyUI] 출발! 로비 -> 전투");
    }

    /// <summary>v1.2: [T] 견습 운행 - 전용 튜토리얼 런 (TutorialDirector 가 진행). v1.5: 기록이 있으면 훈련장 목록(구간별 연습)</summary>
    private void StartTutorial()
    {
        if (!GameBalance.TutorialRunEnabled) return;
        if (collectionRoot != null) { Destroy(collectionRoot.gameObject); collectionRoot = null; }
        if (TutorialDirector.Done || TutorialDirector.HasAnyRecord())
        {
            Debug.Log("[LobbyUI] [T] 훈련장 열기");
            TrainingGroundUI.Toggle();
            return;
        }
        Debug.Log("[LobbyUI] [T] 견습 운행 -> TutorialDirector.Begin");
        TutorialDirector.Begin();
    }

    /// <summary>v1.2: 버튼 글자/강조 상태 갱신 (로비 진입 때마다 - 견습을 마치고 돌아오면 바로 "다시 보기")</summary>
    private void RefreshTutorialButton()
    {
        if (tutorialBtn == null) return;
        bool enabled = GameBalance.TutorialRunEnabled;
        tutorialBtn.gameObject.SetActive(enabled);
        bool done = TutorialDirector.Done || TutorialDirector.HasAnyRecord();   // v1.5: 구간 기록이 있어도 "한 번 한 것" 으로
        highlightOn = enabled && !done && GameBalance.TutorialFirstLaunchHighlight;
        if (tutorialLabel != null)
        {
            int did = TutorialDirector.CompletedCount();
            tutorialLabel.text = done ? "[T] 훈련장  -  구간 연습 " + did + "/" + TutorialDirector.SEGMENTS : "[T] 견습 운행";   // 340px 버튼 - 짧게
            tutorialLabel.color = done && did >= TutorialDirector.SEGMENTS ? UIFactory.CREAM : UIFactory.GOLD;
        }
        if (tutorialRing != null) tutorialRing.enabled = highlightOn;
        if (beaconL != null) beaconL.gameObject.SetActive(highlightOn);
        if (beaconR != null) beaconR.gameObject.SetActive(highlightOn);
        if (tutorialArrow != null) tutorialArrow.gameObject.SetActive(highlightOn);
        if (tutorialHint != null) tutorialHint.gameObject.SetActive(highlightOn);
        if (tutorialHintFallback != null) tutorialHintFallback.gameObject.SetActive(highlightOn);
    }

    /// <summary>v1.2: 강조 애니메이션 - 경광등 0.3초 교대 + 화살표 위로 0~8px 까딱 (0.6초, unscaled)</summary>
    private void TickTutorialHighlight()
    {
        if (!highlightOn) return;
        bool on = Mathf.Repeat(Time.unscaledTime, 0.6f) < 0.3f;
        if (beaconL != null && beaconOn != null && beaconOff != null)
        {
            beaconL.sprite = on ? beaconOn : beaconOff;
            beaconR.sprite = on ? beaconOn : beaconOff;
        }
        if (tutorialArrow != null)
        {
            float t = Mathf.Repeat(Time.unscaledTime, 0.6f) / 0.6f;
            float bob = 8f * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f));
            tutorialArrow.rectTransform.anchoredPosition = new Vector2(0f, TUT_BTN_Y + 22f + 6f + bob);
        }
    }

    // ─────────────────────────────────────────────
    // UI 생성 (코드 생성 - 씬 작업 0)
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        canvas = UIFactory.CreateCanvas("Lobby_Canvas", 555);   // 명성 상점(560) 바로 아래
        DontDestroyOnLoad(canvas.gameObject);                    // v1.2: 씬 리로드 뒤에도 로비 UI 유지 (이 오브젝트처럼)

        root = new GameObject("Root");
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        // ── 상단: 타이틀 ──
        Text title = UIFactory.CreateText(root.transform, "Title",
            "WESTERN DINO TRAIN", 46, UIFactory.GOLD, TextAnchor.MiddleCenter);
        SetTopStrip(title.rectTransform, -104f, -30f);

        Text subtitle = UIFactory.CreateText(root.transform, "Subtitle",
            "황야의 마스터 셰프 - 무장 조리 열차의 기록", 18, UIFactory.CREAM, TextAnchor.MiddleCenter);
        SetTopStrip(subtitle.rectTransform, -138f, -104f);

        // v1.1: 칭호 줄 (부제 아래) - 자격이 없으면 빈 글자
        titleBadge = UIFactory.CreateText(root.transform, "TitleBadge", "", 16, UIFactory.GOLD, TextAnchor.MiddleCenter);
        SetTopStrip(titleBadge.rectTransform, -166f, -140f);
        RefreshTitleBadge();

        // ── 하단: 출발 버튼 ──
        Button startBtn = UIFactory.CreateButton(root.transform, "StartBtn",
            "출발한다!  [Enter]", new Vector2(340f, 62f),
            UIFactory.COPPER, UIFactory.CREAM, 26);
        RectTransform startRt = startBtn.GetComponent<RectTransform>();
        startRt.anchorMin = new Vector2(0.5f, 0f);
        startRt.anchorMax = new Vector2(0.5f, 0f);
        startRt.anchoredPosition = new Vector2(0f, 236f);   // v1.2: 156 -> 236 ([T] 버튼 + 마커 화살표 자리)
        startBtn.onClick.AddListener(StartRun);

        // ── v1.2: 출발 버튼 아래 [T] 견습 운행 (목업 v2 (C)) ──
        BuildTutorialButton();

        // ── 출발 버튼 밑: 명성 상점 / 도감 버튼 (나란히) ──
        // 상점이 로비를 자동으로 덮지 않는다 - 출발 전에 원하는 사람만 열어 본다
        Button shopBtn = UIFactory.CreateButton(root.transform, "FameShopBtn",
            "명성 상점  [M]", new Vector2(214f, 42f),
            UIFactory.PANEL, UIFactory.GOLD, 17);
        RectTransform shopRt = shopBtn.GetComponent<RectTransform>();
        shopRt.anchorMin = new Vector2(0.5f, 0f);
        shopRt.anchorMax = new Vector2(0.5f, 0f);
        shopRt.anchoredPosition = new Vector2(-114f, 80f);   // v1.2: 98 -> 80
        shopBtn.onClick.AddListener(delegate
        {
            if (FameShopUI.Instance != null) FameShopUI.Instance.ToggleShop();
        });

        // 도감 열람 (설계 6절 잔여): 지금까지 발견한 요리를 출발 전에 훑어본다
        Button bookBtn = UIFactory.CreateButton(root.transform, "CollectionBtn",
            "요리 도감", new Vector2(214f, 42f),
            UIFactory.PANEL, UIFactory.CREAM, 17);
        RectTransform bookRt = bookBtn.GetComponent<RectTransform>();
        bookRt.anchorMin = new Vector2(0.5f, 0f);
        bookRt.anchorMax = new Vector2(0.5f, 0f);
        bookRt.anchoredPosition = new Vector2(114f, 80f);    // v1.2: 98 -> 80
        bookBtn.onClick.AddListener(ToggleCollection);

        // ── 안내줄 ──
        Text guide = UIFactory.CreateText(root.transform, "Guide",
            "[J] 선대의 일지   |   [H] 차장의 안내 일지",
            14, UIFactory.DIM, TextAnchor.MiddleCenter);
        guide.rectTransform.anchorMin = new Vector2(0f, 0f);
        guide.rectTransform.anchorMax = new Vector2(1f, 0f);
        guide.rectTransform.offsetMin = new Vector2(0f, 28f);   // v1.2: 52~78 -> 28~50
        guide.rectTransform.offsetMax = new Vector2(0f, 50f);

        // ── 좌하단: [설정] 버튼 - 화면 / 소리 / 언어 (v1.8: 줄 5개 -> 창 하나, SettingsUI) ──
        Button settingsBtn = UIFactory.CreateButton(root.transform, "SettingsBtn", "설정  (화면 · 소리 · 언어)", new Vector2(230f, 36f), UIFactory.PANEL, UIFactory.CREAM, 16);
        PlaceCornerButton(settingsBtn, 16f, 28f);
        settingsBtn.onClick.AddListener(delegate { SettingsUI.Toggle(); });

        // ── 우하단: 서체 고지 (크레딧) ──
        Text credit = UIFactory.CreateText(root.transform, "Credit",
            "서체: Neo둥근모 (라이선스: FONT_LICENSE 파일 참조)", 11,
            UIFactory.DIM, TextAnchor.MiddleRight);
        credit.rectTransform.anchorMin = new Vector2(1f, 0f);
        credit.rectTransform.anchorMax = new Vector2(1f, 0f);
        credit.rectTransform.pivot = new Vector2(1f, 0f);
        credit.rectTransform.sizeDelta = new Vector2(420f, 20f);
        credit.rectTransform.anchoredPosition = new Vector2(-14f, 12f);

        // v1.6: 빌드 표시 (크레딧 위) - 테스트 시트에 어느 빌드였는지 적는다
        Text build = UIFactory.CreateText(root.transform, "BuildTag",
            "빌드 " + GameBalance.BuildTag + (Application.isEditor ? "  에디터" : GameBalance.CheatsInBuild ? "  치트 켬" : ""), 11,
            UIFactory.DIM, TextAnchor.MiddleRight);
        build.rectTransform.anchorMin = new Vector2(1f, 0f);
        build.rectTransform.anchorMax = new Vector2(1f, 0f);
        build.rectTransform.pivot = new Vector2(1f, 0f);
        build.rectTransform.sizeDelta = new Vector2(420f, 20f);
        build.rectTransform.anchoredPosition = new Vector2(-14f, 30f);

        root.SetActive(false);   // 상태 폴링이 로비에서 켠다
    }

    /// <summary>
    /// v1.2: [T] 견습 운행 버튼 (340x44, 아래 앵커 y 130) + 강조 부품.
    /// 미완료 = 황동 테 + 양끝 경광등(ui_ev_beacon) + 위 화살표(tut_arrow) + 오른쪽 명판. 전부 RefreshTutorialButton 이 켜고 끈다
    /// </summary>
    private void BuildTutorialButton()
    {
        tutorialBtn = UIFactory.CreateButton(root.transform, "TutorialBtn", "[T] 견습 운행", new Vector2(340f, 44f),
            UIFactory.PANEL, UIFactory.GOLD, 18);
        RectTransform rt = tutorialBtn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, TUT_BTN_Y);
        tutorialBtn.onClick.AddListener(StartTutorial);
        Transform lt = tutorialBtn.transform.Find("Label");
        tutorialLabel = lt != null ? lt.GetComponent<Text>() : null;

        bool skin = UISkin.Available;
        if (skin) tutorialRing = UISkin.AddRing(rt, UISkin.BRASS, 0f);

        // 경광등 (이벤트 배너와 같은 그림). PNG 없으면 생략
        beaconOff = SpriteBank.Get("ui_ev_beacon_0");
        beaconOn = SpriteBank.Get("ui_ev_beacon_1");
        if (beaconOff != null && beaconOn != null)
        {
            beaconL = MakeBeacon(rt, "BeaconL", new Vector2(0f, 0.5f), new Vector2(12f + 16f, 0f));
            beaconR = MakeBeacon(rt, "BeaconR", new Vector2(1f, 0.5f), new Vector2(-12f - 16f, 0f));
            beaconL.sprite = beaconOff; beaconR.sprite = beaconOff;   // 첫 프레임에 흰 사각형이 안 보이게
        }

        // 마커 화살표 (월드 마커와 같은 그림, 2배) - 끝점 = 버튼 윗변 6px 위, 위로만 까딱
        Sprite arrow = SpriteBank.Get("tut_arrow");
        if (arrow != null)
        {
            GameObject ago = new GameObject("TutorialArrow");
            ago.transform.SetParent(root.transform, false);
            RectTransform art = ago.AddComponent<RectTransform>();
            art.anchorMin = new Vector2(0.5f, 0f); art.anchorMax = new Vector2(0.5f, 0f);
            art.pivot = new Vector2(0.5f, 0f);
            art.sizeDelta = new Vector2(arrow.rect.width * 2f, arrow.rect.height * 2f);
            art.anchoredPosition = new Vector2(0f, TUT_BTN_Y + 22f + 6f);
            tutorialArrow = ago.AddComponent<Image>();
            tutorialArrow.sprite = arrow; tutorialArrow.preserveAspect = true; tutorialArrow.raycastTarget = false;
        }

        // 명판 "← 처음이면 이것부터" (버튼 오른쪽 14px, 세로 가운데). 스킨 없으면 금색 글자
        if (skin)
            tutorialHint = UISkin.Nameplate(root.transform, "TutHint", "◀  처음이면 이것부터", 15,
                new Vector2(0.5f, 0f), new Vector2(170f + 14f, TUT_BTN_Y + 16f));
        else
        {
            tutorialHintFallback = UIFactory.CreateText(root.transform, "TutHint", "◀  처음이면 이것부터", 15, UIFactory.GOLD, TextAnchor.MiddleLeft);
            RectTransform hrt = tutorialHintFallback.rectTransform;
            hrt.anchorMin = new Vector2(0.5f, 0f); hrt.anchorMax = new Vector2(0.5f, 0f); hrt.pivot = new Vector2(0f, 0.5f);
            hrt.anchoredPosition = new Vector2(170f + 14f, TUT_BTN_Y); hrt.sizeDelta = new Vector2(240f, 24f);
        }

        RefreshTutorialButton();
    }

    private static Image MakeBeacon(RectTransform parent, string name, Vector2 anchor, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(32f, 32f);
        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    // ─────────────────────────────────────────────
    // 요리 도감 열람 (읽기 전용 - 발견 = 이름, 미발견 = ???)
    // ─────────────────────────────────────────────
    private Canvas collectionCanvas;
    private RectTransform collectionRoot;

    private void ToggleCollection()
    {
        if (collectionRoot != null)
        {
            Destroy(collectionRoot.gameObject);
            collectionRoot = null;
            return;
        }
        BuildCollection();
    }

    /// <summary>열 때마다 새로 그린다 (발견 수가 런마다 늘어나니)</summary>
    private void BuildCollection()
    {
        if (collectionCanvas == null)
            collectionCanvas = UIFactory.CreateCanvas("LobbyCollection_Canvas", 565);   // 명성 상점(560) 위

        // 요리 목록을 티어별로 모은다
        System.Collections.Generic.List<RecipeData> t1 = new System.Collections.Generic.List<RecipeData>();
        System.Collections.Generic.List<RecipeData> t2 = new System.Collections.Generic.List<RecipeData>();
        int found = 0, total = 0;
        foreach (RecipeData r in RecipeDatabase.All)
        {
            if (r == null) continue;
            total++;
            if (MetaProgress.IsRecipeDiscovered(r.recipeId)) found++;
            if (r.tier == 2) t2.Add(r); else t1.Add(r);
        }

        int rows = Mathf.Max(t1.Count, t2.Count);
        float height = 110f + rows * 24f + DEX_DETAIL_H;   // v1.3: 아래 설명 상자

        collectionRoot = UIFactory.CreatePanel(collectionCanvas.transform, "Collection",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-360f, -height * 0.5f), new Vector2(360f, height * 0.5f),
            UIFactory.PANEL, UIFactory.COPPER, 2f);

        Text title = UIFactory.CreateText(collectionRoot, "Title",
            "황야의 요리 도감  -  발견 " + found + " / " + total, 20, UIFactory.GOLD, TextAnchor.UpperCenter);
        title.rectTransform.offsetMin = new Vector2(10f, height - 44f);
        title.rectTransform.offsetMax = new Vector2(-10f, -10f);

        Text colA = UIFactory.CreateText(collectionRoot, "HeadT1",
            "- 기본 요리 -", 15, UIFactory.CREAM, TextAnchor.MiddleCenter);
        SetBookRow(colA.rectTransform, true, height, -1);
        Text colB = UIFactory.CreateText(collectionRoot, "HeadT2",
            "- 전설 요리 -", 15, UIFactory.T2PINK, TextAnchor.MiddleCenter);
        SetBookRow(colB.rectTransform, false, height, -1);

        for (int i = 0; i < rows; i++)
        {
            if (i < t1.Count) MakeBookRow(t1[i], true, height, i);
            if (i < t2.Count) MakeBookRow(t2[i], false, height, i);
        }

        Text footer = UIFactory.CreateText(collectionRoot, "Footer",
            "요리는 처음 만드는 순간 도감에 새겨진다 - [요리 도감] 버튼으로 닫기", 12,
            UIFactory.DIM, TextAnchor.LowerCenter);
        footer.rectTransform.offsetMin = new Vector2(10f, 8f);
        footer.rectTransform.offsetMax = new Vector2(-10f, -(height - 30f));

        // v1.3: 설명 상자 (아래쪽, 푸터 위) - 이름에 마우스를 올리면 채워진다
        RectTransform box = UIFactory.CreatePanel(collectionRoot, "DexDetail",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(16f, 30f), new Vector2(-16f, 30f + DEX_DETAIL_H - 10f),
            new Color(0.10f, 0.065f, 0.045f, 0.96f), UIFactory.GOLD, 2f);
        dexSelected = null;
        dexDetailText = UIFactory.CreateText(box, "Text", DEX_PROMPT, 15,
            UIFactory.DIM, TextAnchor.UpperLeft);
        dexDetailText.rectTransform.offsetMin = new Vector2(12f, 8f);
        dexDetailText.rectTransform.offsetMax = new Vector2(-12f, -8f);
        dexDetailText.lineSpacing = 1.15f;
        dexDetailText.horizontalOverflow = HorizontalWrapMode.Wrap;
        dexDetailText.raycastTarget = false;
    }

    private const float DEX_DETAIL_H = 120f;
    private Text dexDetailText;

    /// <summary>v1.3: 도감 설명 (발견한 요리만 - 미발견은 ??? 그대로)</summary>
    private RecipeData dexSelected;   // v1.7: 클릭으로 고정된 요리 (다시 클릭 = 닫기)

    /// <summary>v1.7: 클릭 = 고정 (마우스가 다른 줄을 스쳐도 안 바뀐다). 같은 줄 다시 클릭 = 닫기. 설명은 짧게 - 맛 문구(flavor)는 뺀다 (테스터 "길고 현학적")</summary>
    private void ShowDexDetail(RecipeData r)
    {
        if (dexDetailText == null || r == null) return;
        if (dexSelected == r)
        {
            dexSelected = null;
            dexDetailText.text = DEX_PROMPT;
            dexDetailText.color = UIFactory.DIM;
            return;
        }
        dexSelected = r;
        bool seen = MetaProgress.IsRecipeDiscovered(r.recipeId);
        if (!seen) { dexDetailText.text = "???  아직 만든 적 없는 요리 - 재료 둘을 조리대에 올리면 알게 된다"; dexDetailText.color = UIFactory.DIM; return; }
        string t = r.displayName + (r.tier == 2 ? "  [전설]" : "") + "   " + RecipeText.RoleWord(r) + "   |   " + RecipeText.Source(r) + "\n";
        t += RecipeText.Full(r, 1f);
        dexDetailText.text = t;
        dexDetailText.color = UIFactory.CREAM;
    }

    private const string DEX_PROMPT = "요리 이름을 클릭하면 여기에 설명 - 무엇을 하나 / 어떤 손님에 / 언제. 다시 클릭하면 닫힌다";

    private static string MatKorName(string key) { return MaterialNames.Kor(key); }   // v1.4: 재료 이름 한 곳

    private void MakeBookRow(RecipeData r, bool left, float height, int row)
    {
        bool seen = MetaProgress.IsRecipeDiscovered(r.recipeId);
        string label = seen ? r.displayName : "???";
        Color c = !seen ? UIFactory.DIM : (r.tier == 2 ? UIFactory.T2PINK : UIFactory.CREAM);

        Text t = UIFactory.CreateText(collectionRoot, "Row_" + r.recipeId, label, 15,
            c, TextAnchor.MiddleLeft);
        t.raycastTarget = true;   // 클릭을 받아야 한다
        SetBookRow(t.rectTransform, left, height, row);
        // v1.7: 이름 클릭 = 설명 상자에 고정 (v1.3 의 마우스 스침은 읽는 도중 바뀌어서 뺐다)
        RecipeHoverRelay relay = t.gameObject.AddComponent<RecipeHoverRelay>();
        relay.recipe = r; relay.onClick = ShowDexDetail;
    }

    /// <summary>도감 행 배치 (row -1 = 컬럼 머리글)</summary>
    private static void SetBookRow(RectTransform rt, bool left, float height, int row)
    {
        float top = height - 74f - (row + 1) * 24f + 24f;   // v1.3: height 에 설명 상자 높이가 포함돼 있어 행은 그만큼 위 (아래 기준 오프셋)
        rt.anchorMin = new Vector2(left ? 0f : 0.5f, 0f);
        rt.anchorMax = new Vector2(left ? 0.5f : 1f, 0f);
        rt.offsetMin = new Vector2(left ? 26f : 20f, top - 22f);
        rt.offsetMax = new Vector2(left ? -20f : -26f, top);
    }

    // ─────────────────────────────────────────────
    // 배치 헬퍼
    // ─────────────────────────────────────────────
    /// <summary>화면 상단 가로줄 (top 기준 offsetY0 ~ offsetY1)</summary>
    private static void SetTopStrip(RectTransform rt, float yMin, float yMax)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(0f, yMin);
        rt.offsetMax = new Vector2(0f, yMax);
    }

    /// <summary>좌/우 하단 코너 고정 (left=true면 좌하단 기준 x,y)</summary>
    private static void SetCorner(RectTransform rt, bool left, float x, float y, float w, float h)
    {
        Vector2 a = left ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
        rt.anchorMin = a;
        rt.anchorMax = a;
        rt.pivot = new Vector2(left ? 0f : 1f, 0f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(left ? x : -x, y);
    }

    private static void PlaceCornerButton(Button b, float x, float y)
    {
        RectTransform rt = b.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(x, y);
    }
}
