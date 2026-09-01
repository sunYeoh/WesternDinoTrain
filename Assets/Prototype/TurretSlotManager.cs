using UnityEngine;

/// <summary>
/// [TurretSlotManager.cs] v2
/// 포탑 슬롯 8개를 자동 생성/관리하는 매니저 (싱글톤)
/// - 기차 오브젝트에 붙이면 시작 시 슬롯 8개(2x4)를 자식으로 생성
/// - 매 프레임: 인접 버프 계산 + 각 슬롯 발사 + 패시브(재생/오라) 처리
/// - v2 변경점: 슬롯 잠금 시스템
///   기본 해금 = GameBalance.BaseSlotCount (6칸)
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

    /// <summary>현재 해금된 슬롯 수 (기본 + 증강)</summary>
    public int UnlockedSlotCount
    {
        get { return Mathf.Min(8, GameBalance.BaseSlotCount + AugmentManager.ExtraSlotUnlock); }
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

        // B-2: 슬롯 8개 = 포탑칸 가로 1열 배치 (트레일러 확장 - 방향결정 2026-08-31)
        // [0][1][2][3] = 포탑칸 A   [4][5][6][7] = 포탑칸 B (6,7은 기본 잠금 - 증강 해금)
        // B-2.2: 슬롯은 지붕 위(SlotY 1.95) - 셰프가 발밑에 서면 근접 [E]가 닿는다 (가로 거리 판정)
        for (int i = 0; i < 8; i++)
        {
            int car = i / 4;    // 0 = 포탑 A, 1 = 포탑 B
            int idx = i % 4;
            float x = (car == 0 ? GameBalance.SlotRowAX : GameBalance.SlotRowBX)
                      + idx * GameBalance.SlotGapX;

            GameObject go = new GameObject("TurretSlot_" + i);
            go.transform.SetParent(transform);
            // B-2.1: 월드 좌표로 고정 (부모 오브젝트가 어디에 있든 데크 칸 위에 정확히 앉는다)
            go.transform.position = new Vector3(x, GameBalance.SlotY, 0f);

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
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        resonanceText = KitchenEventManager.MakeText(canvasGo.transform, "ResonanceText", "", 19,
            new Color(0.85f, 0.85f, 0.8f));
        RectTransform rt = resonanceText.rectTransform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-16f, 195f);   // 하단 HUD(176px) 위
        rt.sizeDelta = new Vector2(420f, 30f);
        resonanceText.alignment = TextAnchor.LowerRight;
        resonanceText.supportRichText = true;
    }

    void Update()
    {
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
            bool adjacent = (i / 4 == index / 4) && Mathf.Abs(i - index) == 1;
            if (!adjacent) continue;

            // 증강 '주방 동선 최적화': 인접 버프 배율
            float v = r.buffValue * o.LevelMult * AugmentManager.AdjacentBuffMul;
            if (r.buffType == "as") atkSpeed += v;
            else if (r.buffType == "pd") physDmg += v;
            else if (r.buffType == "md") magDmg += v;
        }
    }

    // 오라 슬롯 처리 (화염 방벽 / 빙벽 스튜 / 부식의 정수)
    private void TickAuras()
    {
        float auraRange = 10f;
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
                float d = Vector3.Distance(train.transform.position, all[e].transform.position);
                if (d > auraRange) continue;

                if (r.passiveType == "auraBurn")
                    all[e].TakeDamage(3f * s.LevelMult); // 0.5초마다 화염 틱
                else if (r.passiveType == "auraSlow")
                    all[e].ApplySpeedDebuff(0.5f, 0.6f);
                else if (r.passiveType == "auraShred")
                    all[e].ApplySpeedDebuff(0.85f, 0.6f); // 방깎/마깎은 5단계에서, 임시로 이속 감속
            }
        }
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

        // 발동/해제 감지 (발동 순간에만 알림)
        foreach (FoodTag tag in System.Enum.GetValues(typeof(FoodTag)))
        {
            bool active = tagCounts[(int)tag] >= GameBalance.ResonanceCount;
            if (active && !resonanceActive.Contains(tag))
            {
                resonanceActive.Add(tag);
                string effect = (tag == FoodTag.Def)
                    ? "기차 받는 피해 -10%"
                    : "데미지 +" + Mathf.RoundToInt((GameBalance.ResonanceBonus + AugmentManager.ResonanceBonusAdd) * 100f) + "%";
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
            string line = "";
            foreach (FoodTag tag in System.Enum.GetValues(typeof(FoodTag)))
            {
                int c = tagCounts[(int)tag];
                if (c <= 0) continue;
                if (line.Length > 0) line += "   ";

                if (c >= GameBalance.ResonanceCount)
                    line += "<color=#FFD24D>" + TagKor(tag) + " " + c + "/" + GameBalance.ResonanceCount + " 공명!</color>";
                else
                    line += TagKor(tag) + " " + c + "/" + GameBalance.ResonanceCount;
            }
            resonanceText.text = line.Length > 0 ? "속성:  " + line : "";
        }
    }

    /// <summary>해당 태그의 공명 데미지 보너스 (미발동이면 0)</summary>
    public float GetResonanceBonus(FoodTag tag)
    {
        if (tag == FoodTag.Def) return 0f;   // 방어 공명은 피해감소로 처리
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

        // 방어 속성 공명: 받는 피해 -10% 추가
        if ((int)FoodTag.Def < tagCounts.Length &&
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
        if (idxA == idxB) { resultMsg = "같은 슬롯"; return false; }
        if (idxA < 0 || idxA >= 8 || idxB < 0 || idxB >= 8) return false;

        TurretSlot a = slots[idxA];
        TurretSlot b = slots[idxB];
        if (a == null || b == null || a.IsEmpty || b.IsEmpty || a.isLocked || b.isLocked)
        {
            resultMsg = "빈 슬롯이나 잠긴 슬롯은 합체 불가";
            return false;
        }

        RecipeData ra = a.Recipe;
        RecipeData rb = b.Recipe;

        // 1) 같은 요리: 레벨 합산 병합
        if (a.recipeId == b.recipeId)
        {
            int merged = a.level + b.level;
            b.SetTurret(b.recipeId, merged);
            a.ClearSlot();
            resultMsg = rb.displayName + " 합체! " + b.GradeName + "등급 Lv" + merged;
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
                resultMsg = "선대의 기본기 - T2 진화는 봉인됐다. 기본으로 돌아가라";
                return false;
            }

            RecipeData fusion = RecipeDatabase.GetFusion(ra.tag, rb.tag);
            if (fusion == null)
            {
                resultMsg = "이 조합의 진화 레시피 없음";
                return false;
            }

            if (InfusingMinigame.IsActive)
            {
                resultMsg = "인퓨징이 이미 진행 중";
                return false;
            }
            if (CookingMinigame.IsActive)
            {
                resultMsg = "조리 중에는 인퓨징 불가";
                return false;
            }

            InfusingMinigame.Begin(idxA, idxB, fusion, this);
            resultMsg = "[인퓨징] " + ra.displayName + " + " + rb.displayName + " - 정수를 융합한다!";
            return true;
        }

        resultMsg = "T2 포탑은 같은 요리끼리만 합체 가능";
        return false;
    }

    // ── B-2: 과열 빈도 제어 (동시 위기 상한 1 + 기차 전체 최소 간격) ──
    private float lastOverheatTime = -999f;

    /// <summary>어느 슬롯이든 마비(감전/빙결/과열) 중인가 - 과열 동시 발생 차단용</summary>
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
    /// B-2.2: 슬롯이 지붕 위(y 1.95)로 올라갔으므로 가로 거리만 본다
    /// (직선 거리로 재면 갑판의 셰프가 영영 닿지 못한다)
    /// </summary>
    public int FindStunnedSlotNear(Vector3 chefPos, float reach)
    {
        int best = -1;
        float bestDist = reach;
        for (int i = 0; i < slots.Length; i++)
        {
            TurretSlot s = slots[i];
            if (s == null || s.isLocked || s.IsEmpty || !s.IsStunned) continue;
            float d = Mathf.Abs(chefPos.x - s.transform.position.x);
            if (d <= bestDist) { bestDist = d; best = i; }
        }
        return best;
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
    public void CompleteFusion(int idxA, int idxB, RecipeData fusion, int bonusLevel, bool perfect)
    {
        TurretSlot a = (idxA >= 0 && idxA < 8) ? slots[idxA] : null;
        TurretSlot b = (idxB >= 0 && idxB < 8) ? slots[idxB] : null;

        if (a == null || b == null || a.IsEmpty || b.IsEmpty || fusion == null)
        {
            UIManager.Instance?.ShowStatChange("인퓨징 무산 - 재료 포탑이 사라졌다 (아무것도 잃지 않음)");
            Debug.Log("[합체] 인퓨징 무산: 슬롯 상태 변경됨");
            return;
        }

        // 레벨은 완료 시점의 실제 레벨로 계산 (미니게임 중 동종 병합으로 올랐다면 반영)
        int newLevel = Mathf.Max(1, (a.level + b.level) / 2) + bonusLevel;

        // P1+: 요리 숙련 '장인의 감각'(50회) - 숙련된 T2 레시피는 탄생 레벨 +1
        if (MetaProgress.GetMasteryTier(fusion.recipeId) >= GameBalance.MasteryStartLevelTier)
            newLevel += 1;

        b.SetTurret(fusion.recipeId, newLevel);
        a.ClearSlot();

        // 도감 발견 처리 (수량 0으로 등록 - FoodStock.Add는 0이어도 발견 처리)
        if (FoodStock.Instance != null && !FoodStock.Instance.IsDiscovered(fusion.recipeId))
            FoodStock.Instance.Add(fusion.recipeId, 0);

        string msg;
        if (perfect)
            msg = "완벽한 융합! " + fusion.displayName + " [T2] Lv" + newLevel + " - 두 요리의 심장이 하나로 뛴다";
        else if (bonusLevel > 0)
            msg = fusion.displayName + " [T2] 진화! Lv" + newLevel + " (인퓨징 보너스 +" + bonusLevel + ")";
        else
            msg = fusion.displayName + " [T2] 진화! Lv" + newLevel;

        UIManager.Instance?.ShowStatChange(msg);
        SoundManager.Play(bonusLevel > 0 ? "sfx_judge_perfect" : "sfx_augment_pick");
        Debug.Log("[합체] T2 진화 완료: " + msg);
    }

    // 외부 요리 투입: 같은 요리 슬롯 우선, 없으면 첫 해금 빈 슬롯
    public bool TryInsertFood(string recipeId)
    {
        // 1순위: 같은 요리가 이미 있는 슬롯 (레벨업)
        for (int i = 0; i < 8; i++)
            if (slots[i] != null && !slots[i].isLocked && slots[i].recipeId == recipeId)
                return slots[i].TryInsertFood(recipeId);

        // 2순위: 해금된 빈 슬롯
        for (int i = 0; i < 8; i++)
            if (slots[i] != null && !slots[i].isLocked && slots[i].IsEmpty)
                return slots[i].TryInsertFood(recipeId);

        Debug.Log("[TurretSlotManager] 빈 슬롯 없음! (해금 " + UnlockedSlotCount + "칸 - 증강 '증축된 주방 칸'으로 확장 가능)");
        return false;
    }
}
