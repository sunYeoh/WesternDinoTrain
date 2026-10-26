using UnityEngine;

/// <summary>
/// [TurretSlotManager.cs] v2.15 (v9.35.3 2026-10-26 오라 범위 = 기차 몸에서 GameBalance.AuraReach (예전: 가운데에서 10 - 오른쪽 끝을 무는 손님에 안 닿았다) / 부식의 정수 = 부식 (Enemy.corrodedUntil - 직접 맞는 피해 +n%) / 속성 줄 = 어두운 판 + "물리 4기 · 공명 켜짐 +20%" + "?" 칩 (UI 목업 v1 의 3 - "4/3" 이 분수로 읽혔다)) / v2.14 (v9.35.2 2026-10-26 오라가 설명대로: 화염 방벽 불 = 화상 (화상·독 배율 + 약점 파고들기 기록, GameBalance.AuraBurnIsDot) / 부식의 정수 = 방어·저항 깎기 1겹 (AuraShredReal - 예전엔 임시 감속 -15%) / 빙벽 스튜 감속 · 축전 장갑 마비도 '느려지거나 마비된 손님'으로 (TrainCcCounts - 사냥꾼의 본능 · 동상 파편)) / v2.13 (v9.34 2026-10-25 유저 "2레벨 요리끼리 합치면 2레벨 전설": 진화 결과 레벨 = 재료 레벨 그대로. 진화 조리 판정이 좋으면 레벨 대신 그 전설 요리 1개를 하단 카드로, 숙련 +1 없음 - GameBalance.FusionKeepLevel·FusionBonusDishes) / v2.12 (v9.30 2026-10-21 포탑 칸: 시작 4칸(GameBalance.BaseSlotCount), 지역 보스를 잡고 정차할 때 하나씩 더 (BossSlotUnlocks - 운행 단위, GameManager 가 부른다 UnlockByBoss - 열린 칸에 금빛 고리 + 알림), 그다음 증강. 잠긴 칸 글은 SlotMarkerUI) / v2.11 (v9.26 2026-10-16 마감 정리: 어디서도 안 부르는 RepairOne 삭제) / v2.10 (v9.25 2026-10-16 B5 싸움 유물: CountRecipe · EmptyUnlockedCount (짝꿍 숟가락·빈 접시가 읽는다) / 짝꿍 숟가락을 가지면 공명이 꺼진다 (ItemManager.NoResonance - 피해·방어 공명·발동 알림·속성 줄)) / v2.9 (v9.24.1 2026-10-16 5차 피드백 1·2번: 같은 요리 합체가 상한을 넘치면 넘친 레벨만큼 그 요리를 하단 카드로 돌려준다 (4 + 4 = Lv6 + 요리 2개 - 예전엔 말없이 사라졌다, GameBalance.MergeOverflowRefund). 진화 결과 글에 레벨이 나온 까닭 (높은 쪽 Lv + 판정 보너스 + 숙련). 손댄 안내 글은 10-14 말투로) / v2.8 (v9.23 2026-10-14 점검 수정: 포탑 진화 조리 성공 = 그 전설 요리 숙련 1회 - FoodStock.CountCook) / v2.7 (v9.21.2 2026-10-14 문구 팩: 용어 통일 - 접시->요리(개) / 데미지->피해 / 슬롯->칸 / 랜덤->무작위 / 레시피->요리 / 적·아군->손님 / 드랍->드롭 / 마법 저항->저항 / 힐러·러시·청구 / 진화 조리 판 이름) / v2.6 (v9.18 2026-10-06: 진화 조리는 같은 레벨끼리만(FusionSameLevelOnly) + 결과 레벨도 상한까지 / 합체·진화 때 들어간 접시 수를 넘겨받는다(폐기 환급) / 과열은 조건이 된 포탑 중 무작위 / 정비 거리 = 슬롯 앞 바닥(TrainDeck.NearestWalkPoint)까지 / 속성 줄에 마우스를 올리면 공명·옆자리 효과 설명) /
/// v2.5 (v9.16 2026-09-29 손맛 2차 - 소리: 진화 완료 sfx_fusion, 완벽 판정이면 판정음이 뒤따른다 / 오라 화염 틱은 Enemy.TakeTickDamage - 명중음 없이) / v2.4 (v9.15 2026-09-29: 파손 슬롯 - RepairAllBroken(새 운행)·BrokenCount·RepairOne(정비소) / 동종 병합 레벨 배율 표기 GameBalance.LevelMultOf) / v2.3 (v9.14 레벨 상한) / v2.1 (v9.9 2026-09-16: 슬롯 4모서리 배치 GameBalance.SlotPosition + 근접 판정 "가까운 벽 쪽 거리") / v2
/// 포탑 슬롯 8개를 자동 생성/관리하는 매니저 (싱글톤)
/// - v2.1: 슬롯 위치는 GameBalance.SlotPosition(i) 한 곳에서 (모서리 4 = 북 2 지붕선 위 / 남 2 섀시 위).
///   FindStunnedSlotNear 는 셰프가 걸을 수 있는 띠(TrainWalkMinY~MaxY)로 슬롯 y 를 붙인 점까지의 거리로 잰다 -
///   가로만 재면 같은 x 의 북/남 슬롯이 구분되지 않고, 직선 거리로 재면 지붕 위 슬롯에 영영 못 닿는다
/// - 기차 오브젝트에 붙이면 시작 시 슬롯 8개(2x4)를 자식으로 생성
/// - 매 프레임: 인접 버프 계산 + 각 슬롯 발사 + 패시브(재생/오라) 처리
/// - v2 변경점: 슬롯 잠금 시스템
///   기본 해금 = GameBalance.BaseSlotCount (v9.30: 4칸) + 지역 보스 격파마다 하나 (BossSlotUnlocks, 최대 GameBalance.BossSlotUnlockMax)
///   증강 '증축된 주방 칸'(ExtraSlotUnlock)으로 최대 8칸까지 확장
/// - v3 변경점: 속성 공명 (기획 B-5)
///   같은 속성(FoodTag) 포탑 3개 이상 배치 시 해당 속성 데미지 +20%
///   방어(Def) 속성 공명은 기차 받는 피해 -10%로 대체
///   증강 '속성 공명 증폭기'로 보너스 강화 가능
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class TurretSlotManager : MonoBehaviour
{
    public static TurretSlotManager Instance { get; private set; }

    // (B-2: 구 2열 4행 배치 필드 제거 - 배치는 GameBalance.SlotRowAX/BX/GapX/SlotY가 담당)

    [Header("─ 런타임 ─")]
    public TurretSlot[] slots = new TurretSlot[8];

    private TrainManager train;
    private float auraTimer = 0f;

    // B-2.2: 정비 시간 진입 감지용 (마비/과열 일괄 해제)
    private GameManager.GameState lastSeenState = GameManager.GameState.Lobby;

    // 속성 공명 (v3): 태그별 배치 수 + 발동 알림 상태
    private int[] tagCounts = new int[16];
    private System.Collections.Generic.HashSet<FoodTag> resonanceActive =
        new System.Collections.Generic.HashSet<FoodTag>();

    // 공명 현황 HUD (v3.1): 우하단 상시 표시
    private UnityEngine.UI.Text resonanceText;

    /// <summary>v2.12: 이번 운행에서 보스를 잡아 연 칸 수 (운행 시작에 GameManager 가 0 으로)</summary>
    public static int BossSlotUnlocks = 0;

    /// <summary>현재 해금된 슬롯 수 (기본 + 보스 격파 + 증강)</summary>
    public int UnlockedSlotCount
    {
        get { return Mathf.Min(8, GameBalance.BaseSlotCount + BossSlotUnlocks + AugmentManager.ExtraSlotUnlock); }
    }

    /// <summary>v2.12: 지역 보스를 잡고 정차했다 - 칸 하나를 연다 (한도 GameBalance.BossSlotUnlockMax). 열린 칸에 금빛 고리 + 알림. 더 열 칸이 없으면 아무 일 없음</summary>
    public void UnlockByBoss()
    {
        if (BossSlotUnlocks >= GameBalance.BossSlotUnlockMax) return;
        int before = UnlockedSlotCount;
        BossSlotUnlocks++;
        int after = UnlockedSlotCount;
        if (after <= before) return;   // 증강으로 이미 8칸
        TurretSlot opened = (after - 1 < slots.Length) ? slots[after - 1] : null;
        if (opened != null)
        {
            opened.isLocked = false;   // 바로 (Update 의 동기화를 안 기다린다)
            if (GameBalance.GameFeelMaster > 0f)
            {
                WorldFeel.Ring(opened.transform.position, new Color(1f, 0.85f, 0.4f), 1.3f, 0.45f);
                SparkPool.Emit(opened.transform.position, new Color(1f, 0.85f, 0.4f), 8, 0.12f, 3f);
            }
        }
        SoundManager.Play("sfx_augment_pick");
        UIManager.Instance?.ShowStatChange("[보스 격파] 포탑 칸이 하나 더 열렸다 (" + after + " / 8)");
        Debug.Log("[TurretSlotManager] 보스 격파로 칸 열림 - " + after + "칸");
    }

    /// <summary>해당 인덱스 슬롯이 해금됐는지</summary>
    public bool IsSlotUnlocked(int index)
    {
        return index < UnlockedSlotCount;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        train = FindFirstObjectByType<TrainManager>();

        // B-2: 슬롯 8개 (트레일러 확장 - 방향결정 2026-08-31)
        // v2.1 (v9.9): 배치는 GameBalance.SlotPosition(i) - 기본 = 칸당 4모서리 (0 NW / 1 NE / 2 SW / 3 SE = 포탑 A, 4~7 = 포탑 B)
        //   6·7(포탑 B 남쪽)은 기본 잠금 - 증강 해금. SlotCornerLayout=false 면 종전 북쪽 1열 4+4
        for (int i = 0; i < 8; i++)
        {
            Vector2 p = GameBalance.SlotPosition(i);

            GameObject go = new GameObject("TurretSlot_" + i);
            go.transform.SetParent(transform);
            // B-2.1: 월드 좌표로 고정 (부모 오브젝트가 어디에 있든 데크 칸 위에 정확히 앉는다)
            go.transform.position = new Vector3(p.x, p.y, 0f);

            slots[i] = go.AddComponent<TurretSlot>();
        }

        BuildResonanceHUD();
        Debug.Log("[TurretSlotManager] 슬롯 8개 생성 완료 (해금 " + UnlockedSlotCount + "칸, 나머지는 증강으로 확장)");
    }

    /// <summary>공명 현황 상시 표시 HUD (우하단, 코드 생성)</summary>
    private void BuildResonanceHUD()
    {
        GameObject canvasGo = new GameObject("ResonanceHUDCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas cv = canvasGo.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 470;
        UnityEngine.UI.CanvasScaler scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UIFactory.RefResolution;   // v9.14: UI 전체 배율 (GameBalance.UIScale)
        scaler.matchWidthOrHeight = 0.5f;

        // v2.15 (v9.35.3): 어두운 판 - 글이 땅 그림에 묻히지 않게. 오른쪽 끝에 "?" 칩 (마우스를 올리면 설명 상자 - 예전 [마우스를 올리면 설명] 글 대신)
        resonanceBar = KitchenEventManager.MakeBox(canvasGo.transform, "ResonanceBar", new Color(0.07f, 0.05f, 0.04f, 0.86f));
        resonanceBar.anchorMin = new Vector2(1f, 0f);
        resonanceBar.anchorMax = new Vector2(1f, 0f);
        resonanceBar.pivot = new Vector2(1f, 0f);
        resonanceBar.anchoredPosition = new Vector2(-16f, 190f);   // 하단 HUD(위 끝 190) 바로 위 (사고 배너 아래 끝 216 아래로 - 판 높이 26)
        resonanceBar.sizeDelta = new Vector2(300f, 26f);
        resonanceBar.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        resonanceBar.gameObject.SetActive(false);

        resonanceText = KitchenEventManager.MakeText(resonanceBar, "ResonanceText", "", 17,
            new Color(0.85f, 0.85f, 0.8f));
        RectTransform rt = resonanceText.rectTransform;
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(12f, 0f);
        rt.sizeDelta = new Vector2(260f, 24f);
        resonanceText.alignment = TextAnchor.MiddleLeft;
        resonanceText.supportRichText = true;

        RectTransform chip = KitchenEventManager.MakeBox(resonanceBar, "HelpChip", new Color(0.36f, 0.27f, 0.16f, 1f));
        chip.anchorMin = new Vector2(1f, 0.5f);
        chip.anchorMax = new Vector2(1f, 0.5f);
        chip.pivot = new Vector2(1f, 0.5f);
        chip.anchoredPosition = new Vector2(-4f, 0f);
        chip.sizeDelta = new Vector2(20f, 20f);
        chip.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        UnityEngine.UI.Text q = KitchenEventManager.MakeText(chip, "Q", "?", 15, new Color(1f, 0.82f, 0.3f));
        q.rectTransform.anchorMin = Vector2.zero; q.rectTransform.anchorMax = Vector2.one;
        q.rectTransform.offsetMin = Vector2.zero; q.rectTransform.offsetMax = Vector2.zero;
        q.raycastTarget = false;

        // v2.6: 속성 줄 설명 상자 (마우스를 올리면 뜬다 - 유저 "시너지가 어떤 버프인지 뜨게")
        resonanceTip = KitchenEventManager.MakeBox(canvasGo.transform, "ResonanceTip", new Color(0.08f, 0.06f, 0.05f, 0.95f));
        resonanceTip.anchorMin = new Vector2(1f, 0f);
        resonanceTip.anchorMax = new Vector2(1f, 0f);
        resonanceTip.pivot = new Vector2(1f, 0f);
        resonanceTip.anchoredPosition = new Vector2(-16f, 222f);   // 속성 줄 바로 위 (v2.15: 판 위 끝 216 + 6)
        resonanceTip.sizeDelta = new Vector2(560f, 60f);
        UnityEngine.UI.Image tipImg = resonanceTip.GetComponent<UnityEngine.UI.Image>();
        tipImg.raycastTarget = false;
        if (UISkin.Available)
        {
            // 다른 창과 같은 무쇠 평판 + 황동 테 (직접 지정 - 스킨 스캐너가 크기를 보고 파이프 테를 씌우지 않게)
            UISkin.Plate(tipImg, Color.white);
            UISkin.AddRing(resonanceTip, UISkin.BRASS_DIM);
        }
        else
        {
            UnityEngine.UI.Outline tipEdge = resonanceTip.gameObject.AddComponent<UnityEngine.UI.Outline>();
            tipEdge.effectColor = new Color(0.72f, 0.44f, 0.2f, 0.9f);
            tipEdge.effectDistance = new Vector2(2f, -2f);
        }
        resonanceTipText = KitchenEventManager.MakeText(resonanceTip, "Text", "", 17, new Color(0.95f, 0.9f, 0.78f));
        RectTransform tt = resonanceTipText.rectTransform;
        tt.anchorMin = Vector2.zero; tt.anchorMax = Vector2.one;
        tt.offsetMin = new Vector2(14f, 10f); tt.offsetMax = new Vector2(-14f, -10f);
        resonanceTipText.alignment = TextAnchor.UpperLeft;
        resonanceTipText.supportRichText = true;
        resonanceTipText.lineSpacing = 1.1f;
        resonanceTip.gameObject.SetActive(false);
    }

    // v2.6: 속성 줄 설명
    private RectTransform resonanceBar;   // v2.15: 속성 줄 판 (글 + "?" 칩)
    private RectTransform resonanceTip;
    private UnityEngine.UI.Text resonanceTipText;
    private string resonanceLineShown = null;   // 마지막으로 찍은 속성 줄 (바뀔 때만 폭을 다시 잰다)
    private const float TIP_LINE_H = 24f;

    /// <summary>공명에 필요한 포탑 수. 증강 '공명 폭주'는 공격 속성만 줄인다 (방어 공명은 GetDamageReduction 이 기본 개수를 쓴다)</summary>
    private int NeedFor(FoodTag tag)
    {
        if (tag != FoodTag.Def && AugmentManager.ResonanceNeedOverride > 0) return AugmentManager.ResonanceNeedOverride;
        return GameBalance.ResonanceCount;
    }

    /// <summary>슬롯 번호 -> "포탑 A 왼쪽 위" (설명 상자용)</summary>
    private static string SlotPlaceName(int i)
    {
        string car = i < 4 ? "포탑 A" : "포탑 B";
        if (!GameBalance.SlotCornerLayout) return car + " " + (i % 4 + 1) + "번";
        int k = i % 4;
        return car + " " + (k % 2 == 0 ? "왼쪽" : "오른쪽") + (k < 2 ? " 위" : " 아래");
    }

    /// <summary>속성 줄에 마우스가 올라와 있으면 지금 걸린 효과와 다음 조건을 풀어 쓴 상자를 띄운다</summary>
    private void TickResonanceTip()
    {
        if (resonanceTip == null || resonanceText == null) return;
        bool over = !string.IsNullOrEmpty(resonanceText.text)
            && RectTransformUtility.RectangleContainsScreenPoint(resonanceBar != null ? resonanceBar : resonanceText.rectTransform, Input.mousePosition, null);   // v2.15: 판 전체 (글 + ? 칩)
        if (!over)
        {
            if (resonanceTip.gameObject.activeSelf) resonanceTip.gameObject.SetActive(false);
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int lines = 0;
        int bonusPct = Mathf.RoundToInt((GameBalance.ResonanceBonus + AugmentManager.ResonanceBonusAdd) * 100f);
        sb.Append("<color=#FFD24D>속성 공명</color>  같은 속성 포탑을 모으면 켜진다\n"); lines++;
        foreach (FoodTag tag in System.Enum.GetValues(typeof(FoodTag)))
        {
            int c = tagCounts[(int)tag];
            if (c <= 0) continue;
            int need = NeedFor(tag);
            string effect = tag == FoodTag.Def ? "기차가 받는 피해 -10%" : TagKor(tag) + " 포탑 피해 +" + bonusPct + "%";
            if (c >= need) sb.Append("<color=#FFD24D>" + TagKor(tag) + " " + c + "/" + need + " 켜짐</color>  " + effect + "\n");
            else sb.Append(TagKor(tag) + " " + c + "/" + need + "  " + (need - c) + "문 더 놓으면 " + effect + "\n");
            lines++;
        }

        bool anyBuff = false;
        for (int i = 0; i < 8; i++)
        {
            TurretSlot s = slots[i];
            if (s == null || s.IsEmpty || s.isLocked) continue;
            RecipeData r = s.Recipe;
            if (r == null || string.IsNullOrEmpty(r.buffType)) continue;
            if (!anyBuff)
            {
                sb.Append("<color=#FFD24D>이웃 강화</color>  같은 칸의 가로·세로 옆 포탑에만 닿는다\n"); lines++;
                anyBuff = true;
            }
            float v = r.buffValue * s.LevelMult * AugmentManager.AdjacentBuffMul * GameBalance.AdjBuffScale;
            string what = r.buffType == "as" ? "발사 속도" : r.buffType == "pd" ? "물리 공격력" : "속성 공격력";   // RecipeText 와 같은 낱말
            sb.Append(r.displayName + " (" + SlotPlaceName(i) + ")  옆 포탑 " + what + " +" + Mathf.RoundToInt(v * 100f) + "%\n");
            lines++;
        }

        string tip = sb.ToString().TrimEnd('\n');
        if (!resonanceTip.gameObject.activeSelf) resonanceTip.gameObject.SetActive(true);
        if (tip != resonanceTipText.text)
        {
            resonanceTipText.text = tip;
            // 상자 크기 = 가장 긴 줄 + 여백 (줄 수는 위에서 센 값 - 줄바꿈 없이 쓴다)
            float w = Mathf.Clamp(resonanceTipText.preferredWidth + 32f, 320f, 900f);
            resonanceTip.sizeDelta = new Vector2(w, 22f + lines * TIP_LINE_H);
        }
    }

    void Update()
    {
        TickResonanceTip();   // v2.6: 정차 중·기차가 멈춘 뒤에도 설명은 읽을 수 있게 맨 앞에서
        if (train == null || !train.IsAlive) return;
        if (train.IsPowerSaveMode) return; // (구시스템 호환 - 항상 false)

        // 슬롯 잠금 상태 동기화 (증강 획득 즉시 반영)
        for (int i = 0; i < 8; i++)
            if (slots[i] != null) slots[i].isLocked = !IsSlotUnlocked(i);

        // 속성 공명 집계 (같은 태그 포탑 수)
        UpdateResonance();

        // B-2.2: 정비 시간(Town) 진입 시 마비/과열 전체 해제
        // 과열은 [E] 홀드 전용이라 방치하면 다음 웨이브까지 끌고 간다 - 정비 시간 서사와 모순
        if (GameBalance.ClearStunsOnTown && GameManager.Instance != null)
        {
            GameManager.GameState st = GameManager.Instance.currentState;
            if (st != lastSeenState)
            {
                if (st == GameManager.GameState.Town)
                {
                    int cleared = 0;
                    for (int i = 0; i < 8; i++)
                        if (slots[i] != null && slots[i].IsStunned) { slots[i].ClearStun(); cleared++; }
                    if (cleared > 0)
                        UIManager.Instance?.ShowStatChange("[정비 시간] 멈췄던 포탑 " + cleared + "문 응급 정비 완료!");
                }
                lastSeenState = st;
            }
        }

        // 전투 중에만 발사
        if (GameManager.Instance != null &&
            GameManager.Instance.currentState != GameManager.GameState.Battle) return;

        float dt = Time.deltaTime;

        // 각 슬롯 발사 (인접 버프 반영)
        for (int i = 0; i < 8; i++)
        {
            TurretSlot s = slots[i];
            if (s == null || s.IsEmpty || s.isLocked) continue;
            RecipeData r = s.Recipe;

            float buffAS, buffPD, buffMD;
            GetBuffsFor(i, out buffAS, out buffPD, out buffMD);

            float dmgBuff = (r.damageType == DamageType.Magic) ? buffMD : buffPD;

            // 속성 공명 보너스 (같은 태그 3개 이상이면 해당 태그 데미지 증폭)
            dmgBuff += GetResonanceBonus(r.tag);

            s.TickFire(dt, buffAS, dmgBuff);
        }

        // v2.6: 과열 - 전체 간격이 열렸을 때 조건이 된 포탑(임계만큼 쏜 것) 가운데 하나를 무작위로
        //   포탑이 못 쏘는 장면(인라인 연습으로 손님이 멈춤 / 마지막 식사 / 웨이브가 끝나 사고 수습을 기다리는 중)에는 뽑지 않는다 - TickFire 와 같은 문
        if (GameBalance.OverheatEnabled && GameBalance.OverheatRandomPick && CanOverheatNow()
            && !TutorialDirector.InlineFreeze && !BossEnemy.LastSupperServing && !WaveManager.PostWaveHold)
        {
            int ready = 0;
            for (int i = 0; i < 8; i++)
                if (slots[i] != null && slots[i].OverheatReady) ready++;
            if (ready > 0)
            {
                int pick = Random.Range(0, ready);
                for (int i = 0; i < 8; i++)
                {
                    if (slots[i] == null || !slots[i].OverheatReady) continue;
                    if (pick == 0) { slots[i].TriggerOverheat(); break; }
                    pick--;
                }
            }
        }

        // 패시브: 재생 (해독 스튜 / 정화의 성찬 / 오메가 리페어)
        for (int i = 0; i < 8; i++)
        {
            TurretSlot s = slots[i];
            if (s == null || s.IsEmpty || s.isLocked) continue;
            RecipeData r = s.Recipe;
            if (r.passiveType == "regen")
                train.Heal(r.passiveValue * s.LevelMult * dt);
            else if (r.passiveType == "omega")
                train.Heal(3f * s.LevelMult * dt);
        }

        // 오라: 0.5초 간격으로 처리 (매 프레임은 낭비)
        auraTimer += dt;
        if (auraTimer >= 0.5f)
        {
            auraTimer = 0f;
            TickAuras();
        }
    }

    // 인접 버프 합산 계산 (B-2: 가로 1열 재배치 - 인접 = 같은 포탑칸 안의 양옆 슬롯)
    private void GetBuffsFor(int index, out float atkSpeed, out float physDmg, out float magDmg)
    {
        atkSpeed = 0f; physDmg = 0f; magDmg = 0f;

        for (int i = 0; i < 8; i++)
        {
            if (i == index) continue;
            TurretSlot o = slots[i];
            if (o == null || o.IsEmpty || o.isLocked) continue;
            RecipeData r = o.Recipe;
            if (string.IsNullOrEmpty(r.buffType)) continue;

            // 같은 칸(0~3 / 4~7) 안에서 바로 옆 슬롯만 인접으로 친다
            // v2.1 (모서리 배치): 0 NW / 1 NE / 2 SW / 3 SE - 가로 이웃(0-1, 2-3)과 세로 이웃(0-2, 1-3)만, 대각선(0-3, 1-2)은 아니다
            bool sameCar = (i / 4 == index / 4);
            bool adjacent;
            if (GameBalance.SlotCornerLayout)
            {
                int a = i % 4, b = index % 4;
                adjacent = sameCar && ((a ^ b) == 1 || (a ^ b) == 2);
            }
            else adjacent = sameCar && Mathf.Abs(i - index) == 1;
            if (!adjacent) continue;

            // 증강 '주방 동선 최적화': 인접 버프 배율
            // 밸런스 1차: AdjBuffScale - 1열 재배치로 줄어든 수혜 폭 보정 (GameBalance 참고)
            float v = r.buffValue * o.LevelMult * AugmentManager.AdjacentBuffMul
                * GameBalance.AdjBuffScale;
            if (r.buffType == "as") atkSpeed += v;
            else if (r.buffType == "pd") physDmg += v;
            else if (r.buffType == "md") magDmg += v;
        }
    }

    // 오라 슬롯 처리 (화염 방벽 / 빙벽 스튜 / 부식의 정수)
    private void TickAuras()
    {
        float auraRange = GameBalance.AuraFromTrainBody ? GameBalance.AuraReach : 10f;   // v2.15 (v9.35.3): 기차 몸에서 6 (예전: 가운데에서 10)
        Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        for (int i = 0; i < 8; i++)
        {
            TurretSlot s = slots[i];
            if (s == null || s.IsEmpty || s.isLocked) continue;
            RecipeData r = s.Recipe;
            if (r.shape != AttackShape.Aura) continue;

            for (int e = 0; e < all.Length; e++)
            {
                if (!all[e].IsAlive) continue;
                float d = GameBalance.AuraFromTrainBody ? DistToTrainBody(all[e])
                    : Vector3.Distance(train.transform.position, all[e].transform.position);
                if (d > auraRange) continue;

                if (r.passiveType == "auraBurn")
                {
                    // 0.5초마다 화염 틱 (v2.5: 틱은 명중음·플래시 없이 - 0.5초 메트로놈 방지)
                    // v2.14 (v9.35.2): 화상으로 친다 - 화상·독 배율(증강·유물)을 받고 '화상 붙은 손님'으로 기록 (약점 파고들기). 예전: 순수 피해, 증강 안 붙음
                    float burnMul = GameBalance.AuraBurnIsDot ? AugmentManager.DotMul * ItemManager.DotMul : 1f;
                    all[e].TakeTickDamage(3f * s.LevelMult * burnMul, true);
                    if (GameBalance.AuraBurnIsDot) AugmentHooks.RegisterDot(all[e], 0.6f);
                }
                else if (r.passiveType == "auraSlow")
                {
                    all[e].ApplySpeedDebuff(0.5f, 0.6f);
                    if (GameBalance.TrainCcCounts) AugmentHooks.RegisterControl(all[e], 0.6f);   // v2.14: '느려진 손님' (사냥꾼의 본능 · 동상 파편)
                }
                else if (r.passiveType == "auraShred")
                {
                    // v2.14 (v9.35.2): 설명대로 방어·저항 깎기 1겹 (+ 깎기 증강). 예전: 임시로 이동 속도 -15%
                    if (GameBalance.AuraShredReal)
                    {
                        all[e].ApplyShredDef(1 + AugmentManager.ShredAdd);
                        all[e].ApplyShredRes(1 + AugmentManager.ShredAdd);
                    }
                    else all[e].ApplySpeedDebuff(0.85f, 0.6f);
                    // v2.15 (v9.35.3): 부식 - 직접 맞는 피해 +n% (깎기만으로는 방어가 낮은 손님에게 아무 일도 없었다). 0.5초 틱이라 0.6초씩
                    if (GameBalance.CorrodeVulnBonus > 0f) all[e].corrodedUntil = Time.time + 0.6f;
                }
            }
        }
    }

    /// <summary>
    /// v2.15 (v9.35.3): 손님에서 기차 몸까지 거리 - 기차를 선로 방향 선분(GameBalance.CarEdgesX 첫 끝 ~ 마지막 끝, 기차 y)으로 보고 잰다 (Enemy.CurrentTargetPos 와 같은 끝).
    /// 오라가 '기차 주변'이 되게 - 예전 (기차 가운데에서 10) 은 오른쪽 끝을 무는 손님에 안 닿았다
    /// </summary>
    private float DistToTrainBody(Enemy en)
    {
        float[] edges = GameBalance.CarEdgesX;
        Vector3 p = en.transform.position;
        float ty = train != null ? train.transform.position.y : 0f;
        float x = Mathf.Clamp(p.x, edges[0], edges[edges.Length - 1]);
        return Vector2.Distance(new Vector2(p.x, p.y), new Vector2(x, ty));
    }

    // ─────────────────────────────────────────────
    // 속성 공명 (v3)
    // ─────────────────────────────────────────────

    /// <summary>태그별 배치 수 집계 + 새로 발동한 공명 알림</summary>
    private void UpdateResonance()
    {
        for (int t = 0; t < tagCounts.Length; t++) tagCounts[t] = 0;

        for (int i = 0; i < 8; i++)
        {
            TurretSlot s = slots[i];
            if (s == null || s.IsEmpty || s.isLocked) continue;
            int tagIdx = (int)s.Recipe.tag;
            if (tagIdx >= 0 && tagIdx < tagCounts.Length) tagCounts[tagIdx]++;
        }

        // 발동/해제 감지 (발동 순간에만 알림). v2.10: 유물 짝꿍 숟가락이면 공명이 없다
        foreach (FoodTag tag in System.Enum.GetValues(typeof(FoodTag)))
        {
            bool active = !ItemManager.NoResonance && tagCounts[(int)tag] >= NeedFor(tag);   // v2.6: 증강 '공명 폭주'(3 -> 2)가 알림·HUD 에도 반영된다
            if (active && !resonanceActive.Contains(tag))
            {
                resonanceActive.Add(tag);
                string effect = (tag == FoodTag.Def)
                    ? "기차 받는 피해 -10%"
                    : "피해 +" + Mathf.RoundToInt((GameBalance.ResonanceBonus + AugmentManager.ResonanceBonusAdd) * 100f) + "%";
                Debug.Log("[공명] [" + TagKor(tag) + "] 속성 공명 발동! " + effect);
                UIManager.Instance?.ShowStatChange("[" + TagKor(tag) + "] 속성 공명 발동! " + effect);
            }
            else if (!active && resonanceActive.Contains(tag))
            {
                resonanceActive.Remove(tag);
            }
        }

        // 공명 현황 HUD 갱신: 배치된 태그만 "화염 2/3" 형태로, 발동 중이면 금색
        if (resonanceText != null)
        {
            // v2.15 (v9.35.3): "물리 4/3 공명!" 이 분수로 읽혔다 -> "물리 4기 · 공명 켜짐 +20%" / 아직이면 "화염 2기 · 1기 더" (UI 목업 v1 의 3)
            int bonusPct = Mathf.RoundToInt((GameBalance.ResonanceBonus + AugmentManager.ResonanceBonusAdd) * 100f);
            string line = "";
            foreach (FoodTag tag in System.Enum.GetValues(typeof(FoodTag)))
            {
                int c = tagCounts[(int)tag];
                if (c <= 0) continue;
                if (line.Length > 0) line += "    ";

                int need = NeedFor(tag);
                if (ItemManager.NoResonance)
                    line += TagKor(tag) + " " + c + "기";   // v2.10: 공명이 꺼져 있으면 개수만
                else if (c >= need)
                    line += "<color=#FFD24D>" + TagKor(tag) + " " + c + "기 · 공명 켜짐 " + (tag == FoodTag.Def ? "받는 피해 -10%" : "+" + bonusPct + "%") + "</color>";
                else
                    line += TagKor(tag) + " " + c + "기 <color=#8C8068>· " + (need - c) + "기 더</color>";
            }
            string shown = line.Length > 0 ? "<color=#A08C6E>속성</color>   " + line + (ItemManager.NoResonance ? "    <color=#8C8068>짝꿍 숟가락이 공명을 껐어</color>" : "") : "";
            if (shown != resonanceLineShown)
            {
                resonanceLineShown = shown;
                resonanceText.text = shown;
                // 판 폭 = 글 + 양옆 여백 + ? 칩 (마우스 판정 범위 = 판)
                float textW = Mathf.Ceil(resonanceText.preferredWidth) + 4f;
                resonanceText.rectTransform.sizeDelta = new Vector2(textW, 24f);
                if (resonanceBar != null)
                {
                    resonanceBar.sizeDelta = new Vector2(12f + textW + 10f + 20f + 6f, 26f);
                    resonanceBar.gameObject.SetActive(shown.Length > 0);
                }
            }
        }
    }

    /// <summary>v2.10 (v9.25 B5 짝꿍 숟가락): 이 요리가 올라간 포탑 수 (잠긴 칸 제외)</summary>
    public int CountRecipe(string recipeId)
    {
        if (string.IsNullOrEmpty(recipeId)) return 0;
        int n = 0;
        for (int i = 0; i < 8; i++)
        {
            TurretSlot s = slots[i];
            if (s != null && !s.IsEmpty && !s.isLocked && s.recipeId == recipeId) n++;
        }
        return n;
    }

    /// <summary>v2.10 (v9.25 B5 빈 접시): 열려 있는데 비어 있는 포탑 칸 수</summary>
    public int EmptyUnlockedCount()
    {
        int n = 0;
        for (int i = 0; i < 8; i++)
        {
            TurretSlot s = slots[i];
            if (s != null && s.IsEmpty && IsSlotUnlocked(i)) n++;
        }
        return n;
    }

    /// <summary>v5.1 (합체 미리보기용): 현재 배치된 해당 태그 포탑 수</summary>
    public int GetTagCount(FoodTag tag)
    {
        int idx = (int)tag;
        return (idx >= 0 && idx < tagCounts.Length) ? tagCounts[idx] : 0;
    }

    /// <summary>태그 한글명 (외부용)</summary>
    public string TagName(FoodTag tag) { return TagKor(tag); }

    /// <summary>해당 태그의 공명 데미지 보너스 (미발동이면 0)</summary>
    public float GetResonanceBonus(FoodTag tag)
    {
        if (tag == FoodTag.Def) return 0f;   // 방어 공명은 피해감소로 처리
        if (ItemManager.NoResonance) return 0f;   // v2.10: 유물 짝꿍 숟가락의 대가
        int idx = (int)tag;
        if (idx < 0 || idx >= tagCounts.Length) return 0f;
        // Phase 2-2 증강 '공명 폭주': 발동 조건 3개 -> 2개
        int need = AugmentManager.ResonanceNeedOverride > 0
            ? AugmentManager.ResonanceNeedOverride : GameBalance.ResonanceCount;
        if (tagCounts[idx] < need) return 0f;
        return GameBalance.ResonanceBonus + AugmentManager.ResonanceBonusAdd;
    }

    /// <summary>태그 한글 이름</summary>
    private string TagKor(FoodTag tag)
    {
        switch (tag)
        {
            case FoodTag.Phys: return "물리";
            case FoodTag.Elec: return "전기";
            case FoodTag.Fire: return "화염";
            case FoodTag.Ice: return "냉기";
            case FoodTag.Poison: return "독";
            default: return "방어";
        }
    }

    // 슬롯 패시브 조회: 받는 피해 감소 합산 (최대 60%)
    public float GetDamageReduction()
    {
        float dr = 0f;
        for (int i = 0; i < 8; i++)
        {
            TurretSlot s = slots[i];
            if (s == null || s.IsEmpty || s.isLocked) continue;
            RecipeData r = s.Recipe;
            if (r.passiveType == "dr")
                dr += r.passiveValue * s.LevelMult;
        }

        // 방어 속성 공명: 받는 피해 -10% 추가 (v2.10: 짝꿍 숟가락이면 없다)
        if (!ItemManager.NoResonance && (int)FoodTag.Def < tagCounts.Length &&
            tagCounts[(int)FoodTag.Def] >= GameBalance.ResonanceCount)
            dr += 0.10f;

        return Mathf.Min(0.6f, dr);
    }

    // 축전 장갑: 기차 피격 시 주변 감전 반격
    public void TriggerThorns(Vector3 trainPos)
    {
        bool hasThorns = false;
        float mult = 1f;
        for (int i = 0; i < 8; i++)
        {
            TurretSlot s = slots[i];
            if (s == null || s.IsEmpty || s.isLocked) continue;
            if (s.Recipe.passiveType == "thorns")
            {
                hasThorns = true;
                mult = Mathf.Max(mult, s.LevelMult);
            }
        }
        if (!hasThorns) return;

        Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int e = 0; e < all.Length; e++)
        {
            if (!all[e].IsAlive) continue;
            if (Vector3.Distance(trainPos, all[e].transform.position) <= 5f)
            {
                all[e].TakeDamage(12f * mult);
                all[e].ApplyStun(0.3f);
                if (GameBalance.TrainCcCounts) AugmentHooks.RegisterControl(all[e], 0.3f);   // v2.14: '마비된 손님' (사냥꾼의 본능 · 동상 파편)
            }
        }
    }

    // ─────────────────────────────────────────────
    // 포탑 합체 진화 (기획 B-3, DRD 방식)
    // ─────────────────────────────────────────────

    /// <summary>
    /// 슬롯 A를 슬롯 B에 합친다. 성공 시 A는 비워진다.
    /// - 같은 요리: 레벨 합산 (슬롯 정리 + 고속 등급업)
    /// - 다른 T1 요리: 두 태그의 T2 전설 포탑으로 진화 (레벨은 평균)
    /// 반환: 성공 여부. resultMsg에 결과 설명
    /// </summary>
    public bool TryMergeSlots(int idxA, int idxB, out string resultMsg)
    {
        resultMsg = "";
        if (idxA == idxB) { resultMsg = "같은 칸"; return false; }
        if (idxA < 0 || idxA >= 8 || idxB < 0 || idxB >= 8) return false;

        TurretSlot a = slots[idxA];
        TurretSlot b = slots[idxB];
        if (a == null || b == null || a.IsEmpty || b.IsEmpty || a.isLocked || b.isLocked)
        {
            resultMsg = "빈 칸이나 잠긴 칸은 합체 불가";
            return false;
        }

        RecipeData ra = a.Recipe;
        RecipeData rb = b.Recipe;

        // 1) 같은 요리: 레벨 합산 병합
        if (a.recipeId == b.recipeId)
        {
            int merged = a.level + b.level;
            int cap = TurretSlot.MaxLevelOf(rb);   // v2.3 (v9.14): 레벨 상한
            int overflow = 0;                      // v2.9: 상한을 넘친 레벨 - 그만큼 요리를 하단 카드로 돌려준다
            if (cap > 0 && merged > cap)
            {
                if (b.level >= cap)
                {
                    // v2.6: 전설·'선대의 기본기' 에는 진화 안내를 붙이지 않는다 (못 하는 걸 하라고 하지 않는다)
                    resultMsg = rb.displayName + " Lv" + b.level + " - 이미 최대"
                        + (rb.tier >= 2 || AugmentManager.BasicsDoctrine ? "" : ". 다른 기본 요리 Lv" + b.level + " 하나와 합치면 전설로 진화해");
                    return false;
                }
                overflow = merged - cap;
                merged = cap;
            }
            bool refund = overflow > 0 && GameBalance.MergeOverflowRefund && FoodStock.Instance != null;
            int plates = a.platesTotal + b.platesTotal;   // v2.6: 폐기 환급용 - 두 포탑에 들어간 접시를 합친다
            if (refund) plates = Mathf.Max(1, plates - overflow);   // v2.9: 돌려준 요리는 폐기 환급에서 빼야 두 번 받지 않는다
            b.SetTurret(b.recipeId, merged);
            b.platesTotal = plates;
            a.ClearSlot();
            if (refund) FoodStock.Instance.Add(rb.recipeId, overflow);   // v2.9: 넘친 레벨 = 요리 n개 (하단 카드로 날아간다)
            resultMsg = rb.displayName + " 합체! " + (merged >= cap && cap > 0 ? "최대 " : "") + "Lv" + merged
                + (refund ? ". 넘친 " + overflow + "레벨은 요리 " + overflow + "개로 돌려받았다" : "");
            Debug.Log("[합체] 동종 병합: " + resultMsg);
            return true;
        }

        // 2) 다른 요리: 둘 다 T1이면 T2 진화
        // P1 (감사 1-A 처방 2): 즉시 진화 대신 '인퓨징 미니게임'을 거친다.
        // 판정이 좋으면 T2가 +1레벨로 탄생 - 실제 진화는 CompleteFusion에서 수행.
        if (ra.tier == 1 && rb.tier == 1)
        {
            // Phase 2-3 증강 '선대의 기본기': T2 진화 봉인 (T1 강화의 대가)
            if (AugmentManager.BasicsDoctrine)
            {
                resultMsg = "선대의 기본기 - 전설 요리 진화는 봉인됐다";
                return false;
            }

            RecipeData fusion = RecipeDatabase.GetFusion(ra.tag, rb.tag);
            if (fusion == null)
            {
                resultMsg = "이 조합은 진화할 요리가 없어";
                return false;
            }

            // v2.6 (유저 10-06): 레벨이 같은 포탑끼리만 진화 - "80레벨 + 1레벨 = 81레벨 전설" 차단
            if (GameBalance.FusionSameLevelOnly && a.level != b.level)
            {
                resultMsg = ra.displayName + " Lv" + a.level + " / " + rb.displayName + " Lv" + b.level + " - 레벨이 같아야 진화해";
                return false;
            }

            if (InfusingMinigame.IsActive)
            {
                resultMsg = "진화 조리가 이미 진행 중";
                return false;
            }
            if (CookingMinigame.IsActive)
            {
                resultMsg = "조리 중에는 진화 조리 불가";
                return false;
            }

            InfusingMinigame.Begin(idxA, idxB, fusion, this);
            resultMsg = "[진화 조리] " + ra.displayName + " + " + rb.displayName + " - 두 요리를 하나로!";
            return true;
        }

        resultMsg = "전설 포탑은 같은 요리끼리만 합체 가능";
        return false;
    }

    // ── B-2: 과열 빈도 제어 (동시 위기 상한 1 + 기차 전체 최소 간격) ──
    private float lastOverheatTime = -999f;

    /// <summary>어느 슬롯이든 마비(감전/빙결/과열) 중인가 - 과열 동시 발생 차단용</summary>
    // ── v2.4: 파손 슬롯 (TurretSlot.isBroken) ──
    /// <summary>새 운행 시작 때 전부 수리 (GameManager 첫 웨이브 블록에서). 파손은 "이번 운행" 한정</summary>
    public void RepairAllBroken()
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null && slots[i].isBroken) slots[i].Repair();
    }

    /// <summary>파손된 슬롯 수 (정비소 수리 줄 표시용)</summary>
    public int BrokenCount
    {
        get { int n = 0; for (int i = 0; i < slots.Length; i++) if (slots[i] != null && slots[i].isBroken) n++; return n; }
    }

    public bool AnySlotStunned()
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null && slots[i].IsStunned) return true;
        return false;
    }

    /// <summary>지금 새 과열이 발생해도 되는가 (TurretSlot.TickFire가 확인)</summary>
    public bool CanOverheatNow()
    {
        return Time.time - lastOverheatTime >= GameBalance.OverheatGlobalGap && !AnySlotStunned();
    }

    /// <summary>과열 발생 기록 (전체 간격 타이머 리셋)</summary>
    public void NoteOverheat() { lastOverheatTime = Time.time; }

    /// <summary>
    /// B-1: 셰프 근처에 마비(빙결/감전)된 포탑이 있는가.
    /// CookingStation이 [E] 우선순위 판별에 사용 (위기 대응 > 조리대 열기)
    /// </summary>
    public bool HasStunnedSlotNear(Vector3 chefPos, float reach)
    {
        return FindStunnedSlotNear(chefPos, reach) >= 0;
    }

    /// <summary>
    /// B-1: 셰프 근처의 마비된 포탑 중 가장 가까운 슬롯 인덱스 (-1 = 없음).
    /// SlotMarkerUI가 근접 [E] 해제 대상 결정에 사용.
    /// v2.1 (v9.9 모서리 배치): 슬롯 y 를 셰프가 설 수 있는 띠(TrainWalkMinY~MaxY)로 붙인 점(벽 앞)까지의 거리.
    ///   북쪽 슬롯(지붕 위 1.95)은 북쪽 벽 앞 y 1.5, 남쪽 슬롯(섀시 -1.45)은 남쪽 벽 앞 y -1.5 에서 재므로
    ///   같은 x 의 북/남 포탑을 셰프가 서 있는 쪽으로 구분한다. 종전(가로 거리만)은 SlotCornerLayout=false 일 때
    /// </summary>
    public int FindStunnedSlotNear(Vector3 chefPos, float reach)
    {
        int best = -1;
        float bestDist = reach;
        for (int i = 0; i < slots.Length; i++)
        {
            TurretSlot s = slots[i];
            if (s == null || s.isLocked || s.IsEmpty || !s.IsStunned) continue;
            float d = SlotReachDistance(chefPos, s.transform.position);
            if (d <= bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    /// <summary>셰프 위치에서 슬롯까지의 "정비 거리" (v2.1). 다른 시스템(튜토리얼 마커 등)도 같은 자로 잰다</summary>
    public static float SlotReachDistance(Vector3 chefPos, Vector3 slotPos)
    {
        if (!GameBalance.SlotCornerLayout)
            return Mathf.Abs(chefPos.x - slotPos.x);
        // v2.6: 슬롯에서 가장 가까운 "설 수 있는 바닥"(포탑 바로 앞 복도)까지의 거리 - 포탑이 바닥 밖 모서리에 있어도 그 앞에 서면 닿는다
        Vector2 stand = TrainDeck.NearestWalkPoint(new Vector2(slotPos.x, slotPos.y));
        float dx = chefPos.x - stand.x;
        float dy = chefPos.y - stand.y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Phase 2-3 증강 '주방장은 하나다': 현재 가장 레벨이 높은 포탑의 레시피 키.
    /// 동률이면 앞 슬롯 우선. 빈 주방이면 "" (보너스/페널티 둘 다 미적용)
    /// </summary>
    public string GetChefRecipeId()
    {
        int bestLevel = 0;
        string bestId = "";
        for (int i = 0; i < slots.Length; i++)
        {
            TurretSlot s = slots[i];
            if (s == null || s.IsEmpty || s.isLocked) continue;
            if (s.level > bestLevel)
            {
                bestLevel = s.level;
                bestId = s.recipeId;
            }
        }
        return bestId;
    }

    /// <summary>
    /// P1: 인퓨징 미니게임 완료 콜백 - 실제 T2 진화를 여기서 수행한다.
    /// bonusLevel = 판정 보너스 (기준 미달이면 0), perfect = 만점(연출용).
    /// 미니게임 도중 슬롯이 비었으면(다른 합체 등) 진화는 무산되고 아무것도 잃지 않는다.
    /// </summary>
    /// <summary>
    /// v2.9 (v9.24.1): 진화(다른 기본 요리 둘 -> 전설)의 기준 레벨 - 결과(CompleteFusion)·결과 글·미리보기(SlotMarkerUI)가 이것 하나를 쓴다.
    /// v2.3 (v9.14): 둘 중 높은 레벨 (구 동작 평균 - 테스터 "전설이 더 약함": Lv3+Lv3 이 Lv3 이 돼 두 칸 화력보다 약했다).
    /// v2.6: 같은 레벨끼리만 규칙 - 미니게임 도중 한쪽만 올랐으면 낮은 쪽 (시작할 때만 맞춰 놓고 한쪽을 키우는 우회 차단)
    /// </summary>
    public static int FusionBaseLevel(int levelA, int levelB)
    {
        if (GameBalance.FusionSameLevelOnly && levelA != levelB) return Mathf.Min(levelA, levelB);
        return GameBalance.FusionLevelMax ? Mathf.Max(levelA, levelB) : Mathf.Max(1, (levelA + levelB) / 2);
    }

    /// <summary>v2.9: 결과 글에서 기준 레벨을 부르는 말 (FusionBaseLevel 과 짝)</summary>
    public static string FusionBaseWord(int levelA, int levelB)
    {
        if (levelA == levelB) return "Lv";
        if (GameBalance.FusionSameLevelOnly) return "낮은 쪽 Lv";
        return GameBalance.FusionLevelMax ? "높은 쪽 Lv" : "평균 Lv";
    }

    /// <summary>P1+: 요리 숙련 '장인의 감각'(50회) - 숙련된 전설 요리는 탄생 레벨 +1 (결과·미리보기 같이)</summary>
    public static bool FusionMasteryUp(string fusionId)
    {
        return MetaProgress.GetMasteryTier(fusionId) >= GameBalance.MasteryStartLevelTier;
    }

    public void CompleteFusion(int idxA, int idxB, RecipeData fusion, int bonusLevel, bool perfect)
    {
        TurretSlot a = (idxA >= 0 && idxA < 8) ? slots[idxA] : null;
        TurretSlot b = (idxB >= 0 && idxB < 8) ? slots[idxB] : null;

        if (a == null || b == null || a.IsEmpty || b.IsEmpty || fusion == null)
        {
            UIManager.Instance?.ShowStatChange("진화 조리 무산 - 재료 포탑이 사라졌다 (아무것도 잃지 않음)");
            Debug.Log("[합체] 인퓨징 무산: 슬롯 상태 변경됨");
            return;
        }

        // 레벨은 완료 시점의 실제 레벨로 계산 (미니게임 중 동종 병합으로 올랐다면 반영). 기준 레벨 = FusionBaseLevel (미리보기 SlotMarkerUI 와 같은 셈 하나)
        int baseLevel = FusionBaseLevel(a.level, b.level);
        // v2.13 (v9.34): 레벨은 재료 레벨 그대로 (Lv2 + Lv2 = 전설 Lv2). 판정 보너스(bonusLevel > 0 = 판정이 좋았다)는 레벨 대신 그 전설 요리 몇 개 - 카드끼리 진화 조리와 같은 보너스. 숙련 +1 없음
        bool keep = GameBalance.FusionKeepLevel;
        bool sameLevel = a.level == b.level;   // 진화 전 재료 레벨 (아래 SetTurret·ClearSlot 뒤엔 바뀐다)
        bool masteryUp = !keep && FusionMasteryUp(fusion.recipeId);
        int levelBonus = keep ? 0 : bonusLevel;
        int bonusDishes = keep && bonusLevel > 0 ? Mathf.Max(0, GameBalance.FusionBonusDishes) : 0;
        int newLevel = baseLevel + levelBonus + (masteryUp ? 1 : 0);

        // v2.9: 결과 글에 레벨이 나온 까닭 (5차 피드백 2번 "2렙 2렙 합쳤는데 3렙" - 진화는 기준 레벨에 판정 보너스가 붙는 규칙인데 글에 없었다). 위 셈에 쓴 값 그대로
        string why = FusionBaseWord(a.level, b.level) + baseLevel
            + (levelBonus > 0 ? " + 판정 보너스 " + levelBonus : "") + (masteryUp ? " + 숙련 1" : "");

        // v2.6: 전설 상한 (보너스가 겹쳐도 넘지 않는다) + 폐기 환급용 접시 수 넘겨받기
        int fusionCap = TurretSlot.MaxLevelOf(fusion);
        if (fusionCap > 0 && newLevel > fusionCap) { newLevel = fusionCap; why += ", 최대 Lv" + fusionCap; }
        int plates = a.platesTotal + b.platesTotal;

        b.SetTurret(fusion.recipeId, newLevel);
        b.platesTotal = plates;
        a.ClearSlot();

        // 도감 발견 처리 (수량 0으로 등록 - FoodStock.Add는 0이어도 발견 처리)
        if (FoodStock.Instance != null && !FoodStock.Instance.IsDiscovered(fusion.recipeId))
            FoodStock.Instance.Add(fusion.recipeId, 0);
        // v2.13: 판정 보너스 요리 (하단 카드 - 이 포탑에 놓으면 레벨이 오르고, 빈 칸에 놓으면 새 포탑)
        if (bonusDishes > 0 && FoodStock.Instance != null) FoodStock.Instance.Add(fusion.recipeId, bonusDishes);

        // v2.13: 같은 레벨 그대로면 "= Lv2" 풀이는 군더더기 - 판정 보너스 요리만 덧붙인다 (레벨이 다른 채 진화한 경우·스위치를 끈 경우만 풀이)
        string tail = keep && sameLevel && newLevel == baseLevel ? "" : " = " + why;   // 상한에 걸려 깎였으면 ", 최대 Lv" 풀이는 남긴다
        if (bonusDishes > 0) tail += " + 이 요리 " + bonusDishes + "개 더";
        string msg;
        if (perfect)
            msg = "완벽한 융합! " + fusion.displayName + " [전설] Lv" + newLevel + tail;
        else
            msg = fusion.displayName + " [전설] 진화! Lv" + newLevel + tail;

        UIManager.Instance?.ShowStatChange(msg);
        SoundManager.Play("sfx_fusion");   // v2.5: 전설 탄생 - 쇳물 붓는 소리
        // v2.8 (v9.23 점검 수정): 진화 조리 성공 = 그 전설 요리를 한 번 만든 것 - 숙련이 오른다 (예전엔 일반 조리 완료만 셌다. 전설 요리 숙련이 영영 0 이었다)
        FoodStock.Instance?.CountCook(fusion.recipeId);
        if (bonusLevel > 0) SoundManager.PlayDelayed("sfx_judge_perfect", 0.35f);
        Debug.Log("[합체] T2 진화 완료: " + msg);
    }

    // 외부 요리 투입: 같은 요리 슬롯 우선, 없으면 첫 해금 빈 슬롯
    public bool TryInsertFood(string recipeId)
    {
        // 1순위: 같은 요리가 이미 있는 슬롯 (레벨업)
        for (int i = 0; i < 8; i++)
            if (slots[i] != null && !slots[i].isLocked && slots[i].recipeId == recipeId && !slots[i].AtMaxLevel)   // v2.6: 상한에 닿은 포탑은 건너뛴다 (다음 같은 요리·빈 슬롯으로)
                return slots[i].TryInsertFood(recipeId);

        // 2순위: 해금된 빈 슬롯 (v2.4: 파손 슬롯은 건너뛴다)
        for (int i = 0; i < 8; i++)
            if (slots[i] != null && !slots[i].isLocked && !slots[i].isBroken && slots[i].IsEmpty)
                return slots[i].TryInsertFood(recipeId);

        Debug.Log("[TurretSlotManager] 빈 슬롯 없음! (해금 " + UnlockedSlotCount + "칸 - 보스 격파·증강 '증축된 주방 칸'으로 확장)");
        return false;
    }
}
