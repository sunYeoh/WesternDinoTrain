using UnityEngine;

/// <summary>
/// [SpinoBet.cs] v1.1 (v9.21.2 2026-10-14 문구 팩: 용어 통일 - 접시->요리(개) / 데미지->피해 / 슬롯->칸 / 랜덤->무작위 / 레시피->요리 / 적·아군->손님 / 드랍->드롭 / 마법 저항->저항 / 힐러·러시·청구 / 진화 조리 판 이름) / v1 (신규 파일) - Phase 2-1: 스피노 베팅 조건 추적/정산 엔진
///
/// 도박사 스피노가 보스 직전 정차에서 거는 베팅의 "장부" 역할.
///  - 카드 6종 (일반 3: 실패 무손실 / 도박 3: 화끈한 대가 - 사용자 결정)
///  - 보스전 중 조건을 자동 추적 (시간/PERFECT 조리/피격/투척 명중/포탑 수)
///  - 보스 격파 순간 Resolve()로 정산 (보상/벌금 + 로그 알림 + 스피노 한 줄)
///
/// 훅 연결 (전부 1줄):
///  - BossGimmickSystem.RegisterBoss  -> OnBossStart()
///  - BossGimmickSystem 투척 적중부   -> CountThrowHit()
///  - BossGimmickSystem.OnBossDefeated-> Resolve()
///  - TrainManager.TakeDamage         -> CountTrainHit()
///  - CookingBridge.FinishCook(perfect)-> CountPerfect()
///  - GameManager 보스 보너스 지급부  -> ConsumeForfeit()로 몰수 확인
/// 수치는 GameBalance '스피노 베팅' 섹션. UI는 SpinoBetUI.cs.
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public static class SpinoBet
{
    public enum BetId { None, OnTime, Perfect, Tank, Ledger, Rush, Feast }

    /// <summary>현재 걸려 있는 베팅 (None = 없음)</summary>
    public static BetId Active = BetId.None;

    /// <summary>직전 베팅 결과 (0=없음, 1=승리, 2=패배) - 스피노 재등장 대사용 (세션 한정)</summary>
    public static int LastResult = 0;

    /// <summary>Phase 2-2 증강 '판돈 두 배': 판돈/보상/대가 공통 배율</summary>
    private static float StakesMul { get { return AugmentManager.BetStakesMul; } }

    // ── 보스전 추적 카운터 ──
    private static float bossStartTime = 0f;
    private static int perfectCooks = 0;
    private static int trainHits = 0;
    private static int throwHits = 0;

    // ── 격파 보너스 몰수 플래그 (도박 패배 대가) ──
    private static bool forfeitBossBonus = false;

    // ─────────────────────────────────────────────
    // 카드 정보 (UI 표기용)
    // ─────────────────────────────────────────────

    public static bool IsGamble(BetId id)
    {
        return id == BetId.Ledger || id == BetId.Rush || id == BetId.Feast;
    }

    public static string TitleOf(BetId id)
    {
        switch (id)
        {
            case BetId.OnTime: return "정시 배식";
            case BetId.Perfect: return "완벽한 요리";
            case BetId.Tank: return "철벽 주방";
            case BetId.Ledger: return "외상 장부";
            case BetId.Rush: return "속전속결";
            case BetId.Feast: return "굶주린 식탁";
            default: return "";
        }
    }

    /// <summary>카드 본문: 조건 / 성공 / 실패 3줄</summary>
    public static string DescOf(BetId id)
    {
        switch (id)
        {
            case BetId.OnTime:
                return "보스를 " + (int)GameBalance.BetOnTimeSec + "초 안에 격파\n성공: 골드 +"
                    + GameBalance.BetOnTimeGold + "\n실패: 잃는 것 없음";
            case BetId.Perfect:
                return "보스전 중 PERFECT 조리 " + GameBalance.BetPerfectNeed + "회\n성공: 무작위 재료 +"
                    + GameBalance.BetPerfectMats + "\n실패: 잃는 것 없음";
            case BetId.Tank:
                return "기차 피격 " + GameBalance.BetTankHitsMax + "회 이하로 격파\n성공: 최대 HP +"
                    + (int)GameBalance.BetTankMaxHP + "\n실패: 잃는 것 없음";
            case BetId.Ledger:
                return "판돈 " + GameBalance.BetLedgerStake + "G 선불 - 무방비 때 투척 "
                    + GameBalance.BetLedgerThrowNeed + "회 명중\n성공: "
                    + GameBalance.BetLedgerPayoutMul + "배 회수 ("
                    + (GameBalance.BetLedgerStake * GameBalance.BetLedgerPayoutMul) + "G)\n실패: 판돈 몰수 + 재료 절반 압류";
            case BetId.Rush:
                return "보스를 " + (int)GameBalance.BetRushSec + "초 안에 격파\n성공: 골드 +"
                    + GameBalance.BetRushGold + "\n실패: 격파 보너스 몰수 + 최대 HP -"
                    + (int)GameBalance.BetRushHPPenalty;
            case BetId.Feast:
                return "포탑 " + GameBalance.BetFeastSlotsMax + "기 이하로 격파\n성공: 전 재료 +"
                    + GameBalance.BetFeastMats + ", 명성 +" + GameBalance.BetFeastFame
                    + "\n실패: 격파 보너스 몰수 + 골드 절반 압류";
            default: return "";
        }
    }

    // ─────────────────────────────────────────────
    // 카드 뽑기 / 수락
    // ─────────────────────────────────────────────

    /// <summary>일반 1장 + 도박 1장 제시. 외상 장부는 판돈 낼 골드가 있어야 나온다</summary>
    public static void PickCards(out BetId normalCard, out BetId gambleCard)
    {
        Active = BetId.None;   // 지난 런 잔재 정리 (보스전에서 죽으면 정산 없이 남을 수 있음)

        BetId[] normals = { BetId.OnTime, BetId.Perfect, BetId.Tank };
        normalCard = normals[Random.Range(0, normals.Length)];

        bool canStake = GameManager.Instance != null
            && GameManager.Instance.playerGold >= Mathf.RoundToInt(GameBalance.BetLedgerStake * StakesMul);
        BetId[] gambles = canStake
            ? new BetId[] { BetId.Ledger, BetId.Rush, BetId.Feast }
            : new BetId[] { BetId.Rush, BetId.Feast };
        gambleCard = gambles[Random.Range(0, gambles.Length)];
    }

    /// <summary>베팅 수락 (외상 장부는 판돈 즉시 차감)</summary>
    public static void Accept(BetId id)
    {
        Active = id;
        if (id == BetId.Ledger && GameManager.Instance != null)
        {
            int stake = Mathf.RoundToInt(GameBalance.BetLedgerStake * StakesMul);
            GameManager.Instance.AddGold(-stake);
            UIManager.Instance?.ShowStatChange("[베팅] 판돈 " + stake + "G를 걸었다");
        }
        Debug.Log("[SpinoBet] 베팅 수락: " + TitleOf(id));
    }

    // ─────────────────────────────────────────────
    // 보스전 추적 (훅에서 호출)
    // ─────────────────────────────────────────────

    /// <summary>보스 등장 시 카운터 리셋 (BossGimmickSystem.RegisterBoss)</summary>
    public static void OnBossStart()
    {
        bossStartTime = Time.time;
        perfectCooks = 0;
        trainHits = 0;
        throwHits = 0;
    }

    public static void CountTrainHit() { if (Active != BetId.None) trainHits++; }
    public static void CountPerfect() { if (Active != BetId.None) perfectCooks++; }
    public static void CountThrowHit() { if (Active != BetId.None) throwHits++; }

    /// <summary>격파 보너스 몰수 여부 확인 + 소모 (GameManager가 보너스 지급 직전 호출)</summary>
    public static bool ConsumeForfeit()
    {
        bool f = forfeitBossBonus;
        forfeitBossBonus = false;
        return f;
    }

    // ─────────────────────────────────────────────
    // 정산 (보스 격파 순간 - BossGimmickSystem.OnBossDefeated)
    // ─────────────────────────────────────────────
    public static void Resolve()
    {
        if (Active == BetId.None) return;

        BetId bet = Active;
        Active = BetId.None;

        float elapsed = Time.time - bossStartTime;
        bool win = false;

        switch (bet)
        {
            case BetId.OnTime: win = elapsed <= GameBalance.BetOnTimeSec; break;
            case BetId.Perfect: win = perfectCooks >= GameBalance.BetPerfectNeed; break;
            case BetId.Tank: win = trainHits <= GameBalance.BetTankHitsMax; break;
            case BetId.Ledger: win = throwHits >= GameBalance.BetLedgerThrowNeed; break;
            case BetId.Rush: win = elapsed <= GameBalance.BetRushSec; break;
            case BetId.Feast: win = CountActiveSlots() <= GameBalance.BetFeastSlotsMax; break;
        }

        if (win) GrantReward(bet);
        else ApplyPenalty(bet);

        LastResult = win ? 1 : 2;
        MetaProgress.RecordBetResult(win);
        SoundManager.Play(win ? "sfx_judge_perfect" : "sfx_judge_bad");
        Debug.Log("[SpinoBet] 정산: " + TitleOf(bet) + " -> " + (win ? "승리" : "패배")
            + " (시간 " + elapsed.ToString("F0") + "s, PERFECT " + perfectCooks
            + ", 피격 " + trainHits + ", 투척 " + throwHits + ")");
    }

    private static void GrantReward(BetId bet)
    {
        GameManager gm = GameManager.Instance;
        switch (bet)
        {
            case BetId.OnTime:
                int onTimeGold = Mathf.RoundToInt(GameBalance.BetOnTimeGold * StakesMul);
                gm?.AddGold(onTimeGold);
                Notice("[베팅 승리] 정시 배식! 골드 +" + onTimeGold);
                break;
            case BetId.Perfect:
                int matN = Mathf.RoundToInt(GameBalance.BetPerfectMats * StakesMul);
                if (MaterialInventory.Instance != null)
                    for (int i = 0; i < matN; i++)
                        MaterialInventory.Instance.Add(RandomMaterial(), 1);
                Notice("[베팅 승리] 완벽한 요리! 무작위 재료 +" + matN);
                break;
            case BetId.Tank:
                float tankHP = GameBalance.BetTankMaxHP * StakesMul;
                Object.FindFirstObjectByType<TrainManager>()?.AddMaxHP(tankHP);
                Notice("[베팅 승리] 철벽 주방! 최대 HP +" + (int)tankHP);
                break;
            case BetId.Ledger:
                int payout = Mathf.RoundToInt(GameBalance.BetLedgerStake * StakesMul * GameBalance.BetLedgerPayoutMul);
                gm?.AddGold(payout);
                Notice("[베팅 승리] 외상 장부 " + GameBalance.BetLedgerPayoutMul + "배 회수! 골드 +" + payout);
                break;
            case BetId.Rush:
                int rushGold = Mathf.RoundToInt(GameBalance.BetRushGold * StakesMul);
                gm?.AddGold(rushGold);
                Notice("[베팅 승리] 속전속결! 골드 +" + rushGold);
                break;
            case BetId.Feast:
                int feastMats = Mathf.RoundToInt(GameBalance.BetFeastMats * StakesMul);
                int feastFame = Mathf.RoundToInt(GameBalance.BetFeastFame * StakesMul);
                if (MaterialInventory.Instance != null)
                    foreach (MaterialType t in System.Enum.GetValues(typeof(MaterialType)))
                        MaterialInventory.Instance.Add(t, feastMats);
                MetaProgress.AddFame(feastFame);
                Notice("[베팅 승리] 굶주린 식탁! 전 재료 +" + feastMats + ", 명성 +" + feastFame);
                break;
        }
        UIManager.Instance?.ShowStatChange("스피노: \"...이번 요리사는 좀 다른가.\"");
    }

    private static void ApplyPenalty(BetId bet)
    {
        GameManager gm = GameManager.Instance;
        switch (bet)
        {
            case BetId.OnTime:
            case BetId.Perfect:
            case BetId.Tank:
                Notice("[베팅 실패] " + TitleOf(bet) + " - 잃은 것은 자존심뿐이다");
                return;   // 일반 베팅: 무손실, 스피노 조롱도 없음

            case BetId.Ledger:
                // 판돈은 이미 나갔고, 재료 압류 (기본 절반 - '판돈 두 배'면 전부)
                float seizeRatio = Mathf.Min(1f, 0.5f * StakesMul);
                if (MaterialInventory.Instance != null)
                    foreach (MaterialType t in System.Enum.GetValues(typeof(MaterialType)))
                    {
                        int loss = Mathf.FloorToInt(MaterialInventory.Instance.Get(t) * seizeRatio);
                        if (loss > 0) MaterialInventory.Instance.Add(t, -loss);
                    }
                Danger("[베팅 패배] 외상 장부 - 판돈 몰수, 재료 " + (seizeRatio >= 1f ? "전부" : "절반") + " 압류!");
                break;

            case BetId.Rush:
                forfeitBossBonus = true;
                float hpPen = GameBalance.BetRushHPPenalty * StakesMul;
                Object.FindFirstObjectByType<TrainManager>()?.AddMaxHP(-hpPen);
                Danger("[베팅 패배] 속전속결 - 격파 보너스 몰수, 최대 HP -" + (int)hpPen + "!");
                break;

            case BetId.Feast:
                forfeitBossBonus = true;
                float goldRatio = Mathf.Min(1f, 0.5f * StakesMul);
                if (gm != null && gm.playerGold > 0)
                    gm.AddGold(-Mathf.FloorToInt(gm.playerGold * goldRatio));
                Danger("[베팅 패배] 굶주린 식탁 - 격파 보너스 몰수, 골드 "
                    + (goldRatio >= 1f ? "전부" : "절반") + " 압류!");
                break;
        }
        UIManager.Instance?.ShowStatChange("스피노: \"고맙게 받지. 판 벌인 나도 좀 챙겨야 하지 않겠나.\"");
    }

    // ─────────────────────────────────────────────
    // 헬퍼
    // ─────────────────────────────────────────────
    private static int CountActiveSlots()
    {
        TurretSlotManager mgr = TurretSlotManager.Instance;
        if (mgr == null) return 0;
        int n = 0;
        for (int i = 0; i < 8; i++)
            if (mgr.slots[i] != null && !mgr.slots[i].isLocked && !mgr.slots[i].IsEmpty) n++;
        return n;
    }

    private static MaterialType RandomMaterial()
    {
        System.Array vals = System.Enum.GetValues(typeof(MaterialType));
        return (MaterialType)vals.GetValue(Random.Range(0, vals.Length));
    }

    private static void Notice(string msg) { UIManager.Instance?.ShowStatChange(msg); }
    private static void Danger(string msg) { UIManager.Instance?.ShowDanger(msg); }
}
