using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// [SlotMarkerUI.cs] v5.5 (v9.12 2026-09-22: 체인 = "연쇄 번개") / v5.4 (v9.11.1 2026-09-22 문구: 마비 종류별 안내, 전설·진화 조리 용어, 역할 낱말) / v5.3 (v9.10 2026-09-17 테스터 피드백: 포탑 정보창이 마우스를 따라다니며 커서 밑에 겹쳐 깜빡이고 클릭을 가로채던 것 ->
///   화면 한 자리(왼쪽 아래, 하단 바 위) 고정 + 클릭 통과(raycastTarget off) + 합체 선택 중엔 고정 유지 / 포탑 실물 클릭·호버도 이름표와 같이 /
///   설명은 RecipeText 일상어 ("무엇을 하나 / 어떤 손님에 / 언제")) /
/// v5.2 (v9.9.2 2026-09-16: 마비 칩 = "감전!/빙결!/과열!" + 할 일 한 줄, 빨간 테, 칩 위 모서리 경광등 0.3초 교대 (GameBalance.StunChipBeacons) - 목업 v3 (E), 정식 런 공용) / v5.1 (v9.9 2026-09-16: 4모서리 배치 - 남쪽 슬롯 마커는 발 아래, 폭 96->120(GameBalance.SlotMarkerWidth), 로비에서 숨김) / v5 (교수 피드백 A5/A12 반영 2026-09-14) / v4 (B-1: 근접 위기 대응 - 방향결정 2026-08-31)
/// 슬롯 8개 위치에 화면 마커 표시 (월드 따라다님)
/// - 좌클릭(투입 모드): 요리 투입
/// - 좌클릭(평시): 합체 선택 -> 다른 포탑 클릭 = 합체 (기획 B-3)
///   같은 요리 = 강화(레벨 합산) / 다른 T1 = 진화(T2). 재클릭/ESC/우클릭 = 취소
/// - 우클릭: 투입 모드·합체 선택 중이면 "취소"만 한다 (v5: 취소하려다 포탑이 날아가던 충돌 제거)
///   평시에는 폐기 예고 -> GameBalance.ScrapArmSec 안에 같은 슬롯을 한 번 더 우클릭하면 폐기 (확인창 없음)
/// - 호버: 성능 툴팁. v5: 합체 선택 중 다른 포탑에 올리면 결과 미리보기
///   (강화/진화 구분, 결과 레벨, 비는 슬롯, 공명 변화, 미발견 T2는 이름 대신 역할)
/// - v4 변경점 (B-1): 빙결/감전 해제가 클릭 -> "달려가서 [E]"로 전환.
///   셰프가 그 포탑 곁(GameBalance.SlotReach)에 있어야 해제된다 - 몸이 움직일 이유.
///   GameBalance.ProximityInteract = false 면 기존 클릭 방식으로 복귀.
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
    private Image[] beaconL = new Image[8];        // v5.2: 마비 칩 경광등 (칩 윗변 양 모서리)
    private Image[] beaconR = new Image[8];
    private Sprite beaconOff, beaconOn;
    private static readonly Color BORDER_STUN = new Color(0.78f, 0.16f, 0.16f);   // v5.2: 마비 = 빨간 테 (종류 색은 글자·속에)
    private RectTransform tooltip;
    private Text tooltipText;
    private int hoverIndex = -1;

    // B-1: 근접 해제 대상 (셰프와 가장 가까운 마비 슬롯, -1 = 없음)
    private Transform chefTransform;
    private int reachStunIndex = -1;

    // B-2: 과열 냉각 홀드 상태 ([E] 꾹 - 손을 떼면 서서히 식힌 게 날아간다)
    private float coolHold = 0f;
    private int coolIndex = -1;

    // 픽스 2차: 빙결 = [E] 연타로 깨기 (상호작용 변주)
    private int iceTaps = 0;
    private int iceTapIndex = -1;

    // v5 (A5): 폐기 예고 상태 - 같은 슬롯을 ScrapArmSec 안에 한 번 더 우클릭해야 폐기
    private int scrapArmIndex = -1;
    private float scrapArmUntil = 0f;

    // 합체 선택 상태 (-1 = 선택 없음)
    private int mergeSelectIndex = -1;
    private RectTransform mergeBanner;
    private Text mergeBannerText;

    /// <summary>합체 선택 중인지 (PauseMenu가 ESC 용도 판별에 사용)</summary>
    public static bool MergeSelecting { get; private set; }

    private const float TIP_W = 400f, TIP_H = 200f;   // v5.3 고정 정보창 크기
    private int worldHoverIndex = -1;                  // v5.3: 포탑 실물 위 호버 (UI 이름표가 아닌 월드)

    private static readonly Color BG_NORMAL = new Color(0.12f, 0.075f, 0.05f, 0.9f);
    private static readonly Color BG_LOCKED = new Color(0.05f, 0.04f, 0.03f, 0.85f);
    private static readonly Color BORDER_LOCKED = new Color(0.3f, 0.26f, 0.22f);

    void Start()
    {
        canvas = UIFactory.CreateCanvas("SlotMarker_Canvas", 9); // HUD보다 아래

        for (int i = 0; i < 8; i++)
            CreateMarker(i);

        // 툴팁 (맨 위 표시). v5.3: 왼쪽 아래 고정(하단 바 184 위 12px) 400x200, 마우스를 안 따라간다, 클릭을 안 막는다
        RectTransform tipPanel = UIFactory.CreatePanel(canvas.transform, "Tooltip",
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(12f, 196f), new Vector2(12f + TIP_W, 196f + TIP_H),
            new Color(0.09f, 0.05f, 0.03f, 0.96f), UIFactory.GOLD, 2f);
        tooltipText = UIFactory.CreateText(tipPanel, "Text", "", 15, UIFactory.CREAM, TextAnchor.UpperLeft);
        tooltipText.rectTransform.offsetMin = new Vector2(12f, 8f);
        tooltipText.rectTransform.offsetMax = new Vector2(-12f, -8f);
        tooltipText.lineSpacing = 1.1f;
        tooltipText.raycastTarget = false;
        Image[] tipImgs = tipPanel.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < tipImgs.Length; i++) tipImgs[i].raycastTarget = false;
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

        // ── B-1: 근접 [E] 마비 해제 - "달려가서 몸으로 되살린다" ──
        UpdateProximityUnstun();
    }

    /// <summary>
    /// B-1: 셰프가 마비 포탑 곁에 있으면 [E]로 즉시 해제.
    /// 조리대(E)와 겹칠 때는 위기 대응이 우선 - InteractConsumedFrame으로 이중 소비 방지.
    /// </summary>
    private void UpdateProximityUnstun()
    {
        reachStunIndex = -1;
        if (!GameBalance.ProximityInteract) return;
        if (TurretSlotManager.Instance == null) return;
        if (CookingMinigame.IsActive || KitchenPanel.IsOpenStatic || PauseMenu.IsOpen
            || AugmentPickUI.IsOpen || WorkshopUI.IsOpen || AugmentListUI.ReadingOpen) return;

        if (chefTransform == null)
        {
            GameObject chefObj = GameObject.Find("Chef");
            if (chefObj != null) chefTransform = chefObj.transform;
            if (chefTransform == null) return;
        }

        reachStunIndex = TurretSlotManager.Instance.FindStunnedSlotNear(
            chefTransform.position, GameBalance.SlotReach);
        if (reachStunIndex < 0) { coolHold = 0f; coolIndex = -1; return; }

        TurretSlot slot = TurretSlotManager.Instance.slots[reachStunIndex];
        if (slot == null || !slot.IsStunned) return;

        // ── B-2 과열: [E] 홀드 냉각 (즉시 해제가 아니라 잠깐 '작업'한다) ──
        if (slot.StunKind == "과열")
        {
            if (reachStunIndex != coolIndex) { coolIndex = reachStunIndex; coolHold = 0f; }

            if (Input.GetKey(KeyCode.E))
            {
                ChefController.InteractConsumedFrame = Time.frameCount;   // 조리대 열림 방지

                // 픽스 2차 (상호작용 변주): 부채질 - [E] 꾹 + 마우스를 휘저으면 냉각 가속
                // (프레임당 마우스 이동량 기반. 안 휘저어도 기본 속도는 그대로)
                float mouseMove = new Vector2(
                    Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")).magnitude;
                float fanBonus = Mathf.Min(mouseMove * GameBalance.OverheatValveBonus,
                    GameBalance.OverheatValveMax);
                coolHold += Time.deltaTime * (1f + fanBonus);
                if (coolHold >= GameBalance.OverheatCoolHold)
                {
                    coolHold = 0f; coolIndex = -1;
                    slot.ClearStun();
                    SoundManager.Play("sfx_ui_click");
                    GameFeel.DeathPop(slot.transform.position, new Color(0.9f, 0.9f, 0.95f), 0.55f); // 증기 빠짐
                    UIManager.Instance?.ShowStatChange("포탑 냉각 완료! 다시 불을 뿜는다");
                }
            }
            else
                coolHold = Mathf.Max(0f, coolHold - Time.deltaTime * 2f);   // 손 떼면 식힌 게 샌다
            return;
        }

        // ── 픽스 2차 (상호작용 변주): 감전 = [E] 탁 털기(1회) / 빙결 = [E] 연타로 깨기 ──
        coolHold = 0f; coolIndex = -1;
        if (Input.GetKeyDown(KeyCode.E))
        {
            ChefController.InteractConsumedFrame = Time.frameCount;   // 조리대 열림 방지
            string kind = slot.StunKind;

            if (kind == "빙결")
            {
                // 얼음은 한 방에 안 깨진다 - 깡, 깡, 깡!
                if (reachStunIndex != iceTapIndex) { iceTapIndex = reachStunIndex; iceTaps = 0; }
                iceTaps++;
                SoundManager.Play("sfx_ui_click");
                GameFeel.DeathPop(slot.transform.position, new Color(0.6f, 0.9f, 1f), 0.3f); // 얼음 조각
                if (iceTaps < GameBalance.UnfreezeTaps) return;
                iceTaps = 0; iceTapIndex = -1;
            }

            slot.ClearStun();
            SoundManager.Play("sfx_ui_click");
            GameFeel.DeathPop(slot.transform.position, kind == "빙결"
                ? new Color(0.6f, 0.9f, 1f) : new Color(1f, 0.9f, 0.3f), 0.5f);
            UIManager.Instance?.ShowStatChange(kind == "빙결"
                ? "포탑 해빙! (얼음을 깡깡 깨뜨렸다)"
                : "포탑 재가동! (감전을 털어냈다)");
        }
    }

    /// <summary>합체 선택 상태 변경 + 배너 갱신</summary>
    private void SetMergeSelect(int index)
    {
        mergeSelectIndex = index;
        bool on = index >= 0;
        MergeSelecting = on;
        mergeBanner.gameObject.SetActive(on);
        if (GameBalance.SlotInfoFixed)
        {
            if (on) ShowSlotInfo(index);                       // v5.3: 선택 = 정보창 고정
            else if (hoverIndex < 0) tooltip.gameObject.SetActive(false);
        }
        if (on)
        {
            TurretSlot s = TurretSlotManager.Instance.slots[index];
            string name = s != null && !s.IsEmpty ? s.Recipe.displayName : "?";
            // v5.3: 일상어 - 선택 = 설명 고정 + 합체 준비
            mergeBannerText.text = "[선택] " + name + " - 설명은 왼쪽 아래 창. 다른 포탑을 클릭하면 둘을 합친다\n같은 요리끼리 = 레벨을 합쳐 더 세게 / 다른 기본 요리끼리 = 전설 요리로 진화  (다시 클릭 · ESC · 우클릭 = 선택 해제)";
        }
    }

    private void CreateMarker(int index)
    {
        GameObject go = new GameObject("SlotMarker_" + index);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        rt.sizeDelta = new Vector2(GameBalance.SlotMarkerWidth, 52f);   // v5.1: 폭은 GameBalance (모서리 배치 120)
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

        // v5.2: 경광등 2개 - 칩 윗변 양 모서리에 얹힌다 (32px, 옆 칩과 27px 밖에 안 떨어져 바깥쪽엔 못 둔다). 마비 중에만 켜진다
        if (beaconOff == null) beaconOff = SpriteBank.Get("ui_ev_beacon_0");
        if (beaconOn == null) beaconOn = SpriteBank.Get("ui_ev_beacon_1");
        beaconL[index] = MakeBeacon(rt, "BeaconL", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-6f, 22f));
        beaconR[index] = MakeBeacon(rt, "BeaconR", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(6f, 22f));

        // 클릭/호버 핸들러
        SlotMarkerHandler handler = go.AddComponent<SlotMarkerHandler>();
        handler.Init(this, index);
    }

    private Image MakeBeacon(RectTransform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        RectTransform brt = go.AddComponent<RectTransform>();
        brt.SetParent(parent, false);
        brt.anchorMin = anchor; brt.anchorMax = anchor; brt.pivot = pivot;
        brt.anchoredPosition = pos; brt.sizeDelta = new Vector2(32f, 32f);
        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;
        if (beaconOff != null) img.sprite = beaconOff;
        go.SetActive(false);
        return img;
    }

    /// <summary>v5.2: 마비 칩 경광등 켜기/끄기 + 0.3초 교대 (unscaled - 시간 정지 카드 위에서도 깜빡인다)</summary>
    private void TickBeacons(int i, bool on)
    {
        if (beaconL[i] == null || beaconR[i] == null) return;
        bool show = on && GameBalance.StunChipBeacons && beaconOff != null && beaconOn != null;
        if (beaconL[i].gameObject.activeSelf != show) { beaconL[i].gameObject.SetActive(show); beaconR[i].gameObject.SetActive(show); }
        if (!show) return;
        float period = Mathf.Max(0.05f, GameBalance.StunChipBeaconSec);
        bool phase = Mathf.Repeat(Time.unscaledTime, period * 2f) < period;
        beaconL[i].sprite = phase ? beaconOn : beaconOff;
        beaconR[i].sprite = phase ? beaconOff : beaconOn;
    }

    void LateUpdate()
    {
        if (TurretSlotManager.Instance == null || Camera.main == null) return;

        // v5.1: 로비(대기 화면)에서는 마커를 숨긴다 - 로비 UI 와 겹쳐 보이던 것
        bool lobby = GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Lobby;
        if (lobby)
        {
            for (int i = 0; i < 8; i++)
                if (markers[i] != null && markers[i].gameObject.activeSelf) markers[i].gameObject.SetActive(false);
            if (tooltip.gameObject.activeSelf) tooltip.gameObject.SetActive(false);
            return;
        }

        for (int i = 0; i < 8; i++)
        {
            TurretSlot slot = TurretSlotManager.Instance.slots[i];
            if (slot == null) { markers[i].gameObject.SetActive(false); continue; }

            // 월드 -> 스크린 좌표 (마커가 슬롯을 따라다님)
            // B-2.2: 이제 슬롯 자리에 포탑 실물이 서 있으므로 칩은 머리 위로 띄운다
            //        (칩이 포탑/지붕선을 가리던 것이 "따로 논다"의 주범이었음)
            // v5.1: 남쪽(섀시) 슬롯은 발 아래로 - 위로 띄우면 칸 바닥을 가린다
            float markerDy = GameBalance.IsSouthSlot(i) ? -GameBalance.SlotMarkerYOffset : GameBalance.SlotMarkerYOffset;
            Vector3 screen = Camera.main.WorldToScreenPoint(
                slot.transform.position + Vector3.up * markerDy);
            markers[i].gameObject.SetActive(screen.z > 0f);
            markers[i].position = screen;

            // v5.2: 경광등 (마비 중에만)
            TickBeacons(i, !slot.isLocked && !slot.IsEmpty && slot.IsStunned);

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
                // v3: 마비된 포탑 / B-1: 근접 [E] 해제 안내 (스위치 꺼져 있으면 클릭 안내)
                // v5.2 (목업 v3 (E)): 1줄 = 종류 "감전!" / 2줄 = 할 일. 요리 이름은 뺀다 - 칩이 경보다. 테는 빨강, 글자·속은 종류 색
                bool frozen = slot.StunKind == "빙결";
                bool overheated = slot.StunKind == "과열";
                string hint;
                if (!GameBalance.ProximityInteract && !overheated)
                    hint = "클릭 재가동";
                else if (overheated && i == reachStunIndex)
                    hint = "[E] 꾹+마우스 " + Mathf.RoundToInt(Mathf.Clamp01(coolHold / GameBalance.OverheatCoolHold) * 100f) + "%";
                else if (overheated)
                    hint = "[E] 꾹+마우스";
                else if (frozen)
                    hint = i == reachStunIndex ? "[E] 연타 (" + iceTaps + "/" + GameBalance.UnfreezeTaps + ")" : "[E] 연타 " + GameBalance.UnfreezeTaps + "번";
                else
                    hint = i == reachStunIndex ? "[E] 털어라!" : "[E] 한 번";
                markerTexts[i].text = slot.StunKind + "!\n" + hint;
                markerBorders[i].color = BORDER_STUN;
                if (overheated)
                {
                    markerTexts[i].color = new Color(1f, 0.62f, 0.35f);
                    markerBGs[i].color = new Color(0.26f, 0.09f, 0.03f, 0.9f);
                }
                else if (frozen)
                {
                    markerTexts[i].color = new Color(0.65f, 0.9f, 1f);
                    markerBGs[i].color = new Color(0.06f, 0.16f, 0.24f, 0.9f);
                }
                else
                {
                    markerTexts[i].color = new Color(1f, 0.9f, 0.3f);
                    markerBGs[i].color = new Color(0.17f, 0.04f, 0.04f, 0.9f);
                }
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

                // v5 (A5): 폐기 예고 중인 슬롯은 붉게 + 안내
                if (i == scrapArmIndex && Time.time < scrapArmUntil)
                {
                    markerTexts[i].text = r.displayName + "\n[우클릭 1회 더 = 폐기]";
                    markerTexts[i].color = new Color(1f, 0.6f, 0.5f);
                    markerBorders[i].color = new Color(0.95f, 0.25f, 0.2f);
                }
            }
        }

        // v5 (A5): 폐기 예고 만료
        if (scrapArmIndex >= 0 && Time.time >= scrapArmUntil) scrapArmIndex = -1;

        // 툴팁 위치: v5.3 기본 = 고정 (SlotInfoFixed). 구 동작(마우스 따라감)은 스위치 false 일 때만
        if (!GameBalance.SlotInfoFixed && tooltip.gameObject.activeSelf)
        {
            Vector2 pos = (Vector2)Input.mousePosition + new Vector2(20f, -20f);
            if (pos.x + TIP_W > Screen.width) pos.x = Screen.width - TIP_W - 10f;
            if (pos.y - TIP_H < 0f) pos.y = TIP_H + 10f;
            tooltip.position = pos;
        }

        TickWorldPointer();
    }

    /// <summary>
    /// v5.3: 포탑 실물(월드 스프라이트) 위에서도 이름표처럼 - 테스터는 이름표가 아니라 포탑을 눌렀다.
    /// UI(이름표·버튼) 위가 아니고, 마우스 월드 좌표가 어느 슬롯 자리에서 SlotWorldClickRadius 안이면 호버/클릭을 그 슬롯으로 보낸다.
    /// </summary>
    private void TickWorldPointer()
    {
        if (TurretSlotManager.Instance == null || Camera.main == null) return;
        if (GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Lobby) return;
        bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        int near = -1;
        if (!overUi && !PauseMenu.IsOpen && !CookingMinigame.IsActive && !AugmentPickUI.IsOpen && !WorkshopUI.IsOpen && !AugmentListUI.ReadingOpen && !KitchenPanel.IsOpenStatic
            && !BriefingUI.IsOpen && !MerchantUI.IsOpen && !SpinoBetUI.IsOpen && !BranchRouteUI.IsOpen)
        {
            Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition); wp.z = 0f;
            float best = GameBalance.SlotWorldClickRadius;
            for (int i = 0; i < TurretSlotManager.Instance.slots.Length; i++)
            {
                TurretSlot s = TurretSlotManager.Instance.slots[i];
                if (s == null) continue;
                float d = Vector2.Distance(wp, s.transform.position);
                if (d < best) { best = d; near = i; }
            }
        }
        if (near != worldHoverIndex)
        {
            if (worldHoverIndex >= 0 && hoverIndex == worldHoverIndex) OnMarkerExit(worldHoverIndex);
            worldHoverIndex = near;
            if (near >= 0 && hoverIndex < 0) OnMarkerEnter(near);
        }
        if (near >= 0)
        {
            if (Input.GetMouseButtonDown(0)) OnMarkerClick(near, PointerEventData.InputButton.Left);
            else if (Input.GetMouseButtonDown(1)) OnMarkerClick(near, PointerEventData.InputButton.Right);
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

        // v3: 마비 해제가 모든 클릭보다 우선
        // B-1: 근접 모드에서는 클릭으로 해제 불가 - 달려가야 한다 (안내만)
        if (slot.IsStunned)
        {
            if (GameBalance.ProximityInteract)
            {
                UIManager.Instance?.ShowDanger((slot.StunKind == "빙결" ? "포탑 곁으로 달려가 [E] 여러 번 - 얼음을 깨라!" : slot.StunKind == "과열" ? "포탑 곁에서 [E] 를 누른 채 마우스를 움직여 식혀라!" : "포탑 곁으로 달려가 [E] 한 번 - 감전을 털어라!"));
                return;
            }
            string kind = slot.StunKind;
            slot.ClearStun();
            UIManager.Instance?.ShowStatChange(kind == "빙결"
                ? "포탑 해빙! (얼음을 깨뜨렸다)"
                : "포탑 재가동! (감전 해제)");
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
            // v5 (교수 피드백 A5): 우클릭은 먼저 "취소"다. 투입 모드나 합체 선택 중에 커서 밑 포탑이
            // 폐기되던 충돌을 없앤다. GameHUD.Update는 버튼을 누르는 순간 취소하고 이 클릭 이벤트는 뗄 때 오므로,
            // 방금(0.5초 안) 취소된 직후의 우클릭도 같은 "취소 클릭"으로 본다
            bool placingNow = GameHUD.Instance != null && !string.IsNullOrEmpty(GameHUD.Instance.placingRecipeId);
            bool justCancelled = Time.unscaledTime - GameHUD.LastPlacingCancelTime < 0.5f;
            if (placingNow || justCancelled)
            {
                if (placingNow) GameHUD.Instance.SetPlacing("");
                return;
            }
            if (mergeSelectIndex >= 0)
            {
                SetMergeSelect(-1);
                HideTooltip();
                UIManager.Instance?.ShowStatChange("합체 선택 취소");
                return;
            }

            if (slot.IsEmpty) return;

            // 폐기 = 같은 슬롯 우클릭 2회 (첫 번째는 예고: 환급량·손실을 보여준다, 확인창 없음)
            if (scrapArmIndex == index && Time.time < scrapArmUntil)
            {
                scrapArmIndex = -1;
                string scrappedName = slot.Recipe != null ? slot.Recipe.displayName : "포탑";
                int scrappedLevel = slot.level;
                int refund = slot.Scrap();
                for (int k = 0; k < refund; k++)
                    MaterialInventory.Instance.Add((MaterialType)Random.Range(0, 6), 1);
                UIManager.Instance?.ShowStatChange("[폐기] " + scrappedName + " Lv" + scrappedLevel
                    + " - 랜덤 재료 " + refund + "개 환급");
                HideTooltip();
            }
            else
            {
                scrapArmIndex = index;
                scrapArmUntil = Time.time + GameBalance.ScrapArmSec;
                int refund = Mathf.Max(1, slot.level);
                UIManager.Instance?.ShowStatChange("[폐기 예고] " + slot.Recipe.displayName + " Lv" + slot.level
                    + " - " + GameBalance.ScrapArmSec.ToString("F0") + "초 안에 한 번 더 우클릭하면 폐기 (랜덤 재료 "
                    + refund + "개 환급, 레벨은 사라짐)");
            }
        }
    }

    public void OnMarkerEnter(int index)
    {
        hoverIndex = index;
        TurretSlot slot = TurretSlotManager.Instance != null ? TurretSlotManager.Instance.slots[index] : null;
        if (slot == null || slot.IsEmpty || slot.isLocked) { HideTooltip(); return; }

        // v5 (교수 피드백 A12): 합체 선택 중 다른 포탑 위 = 결과 미리보기
        if (mergeSelectIndex >= 0 && mergeSelectIndex != index)
        {
            tooltipText.text = BuildMergePreview(mergeSelectIndex, index);
            tooltip.gameObject.SetActive(true);
            return;
        }

        ShowSlotInfo(index);
    }

    /// <summary>v5.3: 슬롯 하나의 설명을 고정 정보창에 (호버·선택 공용)</summary>
    private void ShowSlotInfo(int index)
    {
        TurretSlot slot = TurretSlotManager.Instance != null ? TurretSlotManager.Instance.slots[index] : null;
        if (slot == null || slot.IsEmpty || slot.isLocked) { tooltip.gameObject.SetActive(false); return; }
        RecipeData r = slot.Recipe;

        // v5.3: 일상어 설명 (RecipeText) - 역할 낱말 / 무엇을 하나 / 어떤 손님에 / 언제 / 숫자
        string info = r.displayName + (r.tier == 2 ? "  [전설]" : "") + "   " + RecipeText.RoleWord(r) + "  Lv" + slot.level + " (x" + slot.LevelMult.ToString("F1") + ")\n";
        info += RecipeText.Full(r, slot.LevelMult) + "\n";

        // P1+: 요리 숙련 표시 (평생 조리 횟수 + 칭호) - 일상어
        int cookCount = MetaProgress.GetCookCount(r.recipeId);
        if (cookCount > 0)
        {
            int mTier = GameBalance.MasteryTier(cookCount);
            info += "이 요리를 " + cookCount + "번 만들었다" + (mTier >= 0 ? " - 손에 익어 더 세다 (" + GameBalance.MasteryTitles[mTier] + ")" : "") + "\n";
        }

        info += "같은 요리를 또 넣으면 레벨업 / 좌클릭 = 합체 선택 / 우클릭 2번 = 폐기(재료 " + Mathf.Max(1, slot.level) + "개 환급)";

        tooltipText.text = info;
        tooltip.gameObject.SetActive(true);
    }

    /// <summary>등급명 (레벨에서 파생, TurretSlot.GradeName과 같은 규칙)</summary>
    private static string GradeOf(int level)
    {
        if (level >= 5) return "S";
        if (level >= 3) return "A";
        if (level >= 2) return "B";
        return "C";
    }

    /// <summary>
    /// v5 (A12): 합체 결과 미리보기 텍스트.
    /// 강화(같은 요리)와 진화(다른 T1)를 구분하고, 결과 레벨·비는 슬롯·공명 변화·역할 변화를 확정 전에 보여준다.
    /// 미발견 T2는 이름 대신 역할/공격 형태만. 실전 DPS 같은 불확실한 숫자는 적지 않는다.
    /// </summary>
    private string BuildMergePreview(int idxA, int idxB)
    {
        TurretSlotManager mgr = TurretSlotManager.Instance;
        TurretSlot a = mgr.slots[idxA];
        TurretSlot b = mgr.slots[idxB];
        if (a == null || b == null || a.IsEmpty || b.IsEmpty) return "미리보기 불가";
        RecipeData ra = a.Recipe, rb = b.Recipe;

        // 1) 강화: 같은 요리
        if (a.recipeId == b.recipeId)
        {
            int merged = a.level + b.level;
            int cnt = mgr.GetTagCount(ra.tag);
            string s = "[강화] " + ra.displayName + "\n";
            s += "Lv" + a.level + " + Lv" + b.level + " -> Lv" + merged + " (" + GradeOf(merged) + "등급, x"
                + (1f + 0.6f * (merged - 1)).ToString("F1") + "배)\n";
            s += "슬롯 1개 비움 / 공격원 2 -> 1\n";
            s += "공명 " + mgr.TagName(ra.tag) + " " + cnt + " -> " + (cnt - 1)
                + (cnt >= GameBalance.ResonanceCount && cnt - 1 < GameBalance.ResonanceCount ? "  (공명 해제!)" : "") + "\n";
            s += "(클릭 = 확정)";
            return s;
        }

        // 2) 진화: 다른 T1 두 개
        if (ra.tier == 1 && rb.tier == 1)
        {
            if (AugmentManager.BasicsDoctrine) return "[진화 불가] 선대의 기본기 - 전설 요리 진화 봉인";
            RecipeData fusion = RecipeDatabase.GetFusion(ra.tag, rb.tag);
            if (fusion == null) return "[진화 불가] 이 조합의 진화 레시피 없음";

            int baseLevel = Mathf.Max(1, (a.level + b.level) / 2);
            bool masteryUp = MetaProgress.GetMasteryTier(fusion.recipeId) >= GameBalance.MasteryStartLevelTier;
            bool known = FoodStock.Instance != null && FoodStock.Instance.IsDiscovered(fusion.recipeId);

            string s = "[진화] " + ra.displayName + " + " + rb.displayName + "\n";
            s += "-> " + (known ? fusion.displayName + " [전설]" : "미발견 전설 요리") + "\n";
            s += "역할: " + RoleName(fusion.role) + " / " + ShapeName(fusion.shape) + "\n";
            s += "레벨: Lv" + (baseLevel + (masteryUp ? 1 : 0)) + " (+진화 조리 판정 보너스 최대 +1)\n";
            s += "슬롯 1개 비움 / 진화 조리(미니게임) 진행\n";

            // 공명 변화: 두 T1 태그 -1씩, T2 태그 +1
            int ca = mgr.GetTagCount(ra.tag), cb = mgr.GetTagCount(rb.tag);
            int na = ca - 1, nb = (ra.tag == rb.tag) ? na - 1 : cb - 1;
            if (fusion.tag == ra.tag) na += 1; else if (fusion.tag == rb.tag) nb += 1;
            string res = "공명 " + mgr.TagName(ra.tag) + " " + ca + " -> " + na;
            if (ca >= GameBalance.ResonanceCount && na < GameBalance.ResonanceCount) res += " (해제!)";
            if (rb.tag != ra.tag)
            {
                res += " / " + mgr.TagName(rb.tag) + " " + cb + " -> " + nb;
                if (cb >= GameBalance.ResonanceCount && nb < GameBalance.ResonanceCount) res += " (해제!)";
            }
            if (fusion.tag != ra.tag && fusion.tag != rb.tag)
                res += " / " + mgr.TagName(fusion.tag) + " +1";
            s += res + "\n(클릭 = 확정)";
            return s;
        }

        return "[합체 불가] 전설 포탑은 같은 요리끼리만 합칠 수 있다";
    }

    public void OnMarkerExit(int index)
    {
        if (hoverIndex == index) HideTooltip();
    }

    private void HideTooltip()
    {
        hoverIndex = -1;
        // v5.3: 포탑을 선택(합체 선택)해 뒀으면 정보창은 그 포탑 것으로 남는다 - "클릭한 포탑의 설명이 남아 있어야 한다"
        if (mergeSelectIndex >= 0 && GameBalance.SlotInfoFixed) { ShowSlotInfo(mergeSelectIndex); return; }
        tooltip.gameObject.SetActive(false);
    }

    private string RoleName(TurretRole role)
    {
        switch (role)
        {
            case TurretRole.PhysDealer: return "물리 화력";
            case TurretRole.MagicDealer: return "속성 화력";
            case TurretRole.Debuffer: return "약화";
            case TurretRole.Buffer: return "이웃 강화";
            case TurretRole.CC: return "제어";
            default: return "지원";
        }
    }

    private string ShapeName(AttackShape shape)
    {
        switch (shape)
        {
            case AttackShape.Projectile: return "단일 투사체";
            case AttackShape.Pierce: return "직선 관통";
            case AttackShape.Cone: return "부채꼴 방사";
            case AttackShape.Explode: return "착탄 폭발";
            case AttackShape.Chain: return "연쇄 번개";
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
