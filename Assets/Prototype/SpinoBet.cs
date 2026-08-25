using UnityEngine;

/// <summary>
/// [SpinoBet.cs] v1 (½Å±Ô ÆÄÀÏ) - Phase 2-1: ½ºÇÇ³ë º£ÆÃ Á¶°Ç ÃßÀû/Á¤»ê ¿£Áø
///
/// µµ¹Ú»ç ½ºÇÇ³ë°¡ º¸½º Á÷Àü Á¤Â÷¿¡¼­ °Å´Â º£ÆÃÀÇ "ÀåºÎ" ¿ªÇÒ.
///  - Ä«µå 6Á¾ (ÀÏ¹Ý 3: ½ÇÆÐ ¹«¼Õ½Ç / µµ¹Ú 3: È­²öÇÑ ´ë°¡ - »ç¿ëÀÚ °áÁ¤)
///  - º¸½ºÀü Áß Á¶°ÇÀ» ÀÚµ¿ ÃßÀû (½Ã°£/PERFECT Á¶¸®/ÇÇ°Ý/ÅõÃ´ ¸íÁß/Æ÷Å¾ ¼ö)
///  - º¸½º °ÝÆÄ ¼ø°£ Resolve()·Î Á¤»ê (º¸»ó/¹ú±Ý + ·Î±× ¾Ë¸² + ½ºÇÇ³ë ÇÑ ÁÙ)
///
/// ÈÅ ¿¬°á (ÀüºÎ 1ÁÙ):
///  - BossGimmickSystem.RegisterBoss  -> OnBossStart()
///  - BossGimmickSystem ÅõÃ´ ÀûÁßºÎ   -> CountThrowHit()
///  - BossGimmickSystem.OnBossDefeated-> Resolve()
///  - TrainManager.TakeDamage         -> CountTrainHit()
///  - CookingBridge.FinishCook(perfect)-> CountPerfect()
///  - GameManager º¸½º º¸³Ê½º Áö±ÞºÎ  -> ConsumeForfeit()·Î ¸ô¼ö È®ÀÎ
/// ¼öÄ¡´Â GameBalance '½ºÇÇ³ë º£ÆÃ' ¼½¼Ç. UI´Â SpinoBetUI.cs.
/// VS 2017 (C# 7.3) È£È¯.
/// </summary>
public static class SpinoBet
{
    public enum BetId { None, OnTime, Perfect, Tank, Ledger, Rush, Feast }

    /// <summary>ÇöÀç °É·Á ÀÖ´Â º£ÆÃ (None = ¾øÀ½)</summary>
    public static BetId Active = BetId.None;

    /// <summary>Á÷Àü º£ÆÃ °á°ú (0=¾øÀ½, 1=½Â¸®, 2=ÆÐ¹è) - ½ºÇÇ³ë ÀçµîÀå ´ë»ç¿ë (¼¼¼Ç ÇÑÁ¤)</summary>
    public static int LastResult = 0;

    /// <summary>Phase 2-2 Áõ°­ 'ÆÇµ· µÎ ¹è': ÆÇµ·/º¸»ó/´ë°¡ °øÅë ¹èÀ²</summary>
    private static float StakesMul { get { return AugmentManager.BetStakesMul; } }

    // ¦¡¦¡ º¸½ºÀü ÃßÀû Ä«¿îÅÍ ¦¡¦¡
    private static float bossStartTime = 0f;
    private static int perfectCooks = 0;
    private static int trainHits = 0;
    private static int throwHits = 0;

    // ¦¡¦¡ °ÝÆÄ º¸³Ê½º ¸ô¼ö ÇÃ·¡±× (µµ¹Ú ÆÐ¹è ´ë°¡) ¦¡¦¡
    private static bool forfeitBossBonus = false;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Ä«µå Á¤º¸ (UI Ç¥±â¿ë)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    public static bool IsGamble(BetId id)
    {
        return id == BetId.Ledger || id == BetId.Rush || id == BetId.Feast;
    }

    public static string TitleOf(BetId id)
    {
        switch (id)
        {
            case BetId.OnTime: return "Á¤½Ã ¹è½Ä";
            case BetId.Perfect: return "¿Ïº®ÇÑ Á¢½Ã";
            case BetId.Tank: return "Ã¶º® ÁÖ¹æ";
            case BetId.Ledger: return "¿Ü»ó ÀåºÎ";
            case BetId.Rush: return "¼ÓÀü¼Ó°á";
            case BetId.Feast: return "±¾ÁÖ¸° ½ÄÅ¹";
            default: return "";
        }
    }

    /// <summary>Ä«µå º»¹®: Á¶°Ç / ¼º°ø / ½ÇÆÐ 3ÁÙ</summary>
    public static string DescOf(BetId id)
    {
        switch (id)
        {
            case BetId.OnTime:
                return "º¸½º¸¦ " + (int)GameBalance.BetOnTimeSec + "ÃÊ ¾È¿¡ °ÝÆÄ\n¼º°ø: °ñµå +"
                    + GameBalance.BetOnTimeGold + "\n½ÇÆÐ: ÀÒ´Â °Í ¾øÀ½";
            case BetId.Perfect:
                return "º¸½ºÀü Áß PERFECT Á¶¸® " + GameBalance.BetPerfectNeed + "È¸\n¼º°ø: ·£´ý Àç·á +"
                    + GameBalance.BetPerfectMats + "\n½ÇÆÐ: ÀÒ´Â °Í ¾øÀ½";
            case BetId.Tank:
                return "±âÂ÷ ÇÇ°Ý " + GameBalance.BetTankHitsMax + "È¸ ÀÌÇÏ·Î °ÝÆÄ\n¼º°ø: ÃÖ´ë HP +"
                    + (int)GameBalance.BetTankMaxHP + "\n½ÇÆÐ: ÀÒ´Â °Í ¾øÀ½";
            case BetId.Ledger:
                return "ÆÇµ· " + GameBalance.BetLedgerStake + "G ¼±ºÒ - ±×·Î±â ÅõÃ´ "
                    + GameBalance.BetLedgerThrowNeed + "È¸ ¸íÁß\n¼º°ø: "
                    + GameBalance.BetLedgerPayoutMul + "¹è È¸¼ö ("
                    + (GameBalance.BetLedgerStake * GameBalance.BetLedgerPayoutMul) + "G)\n½ÇÆÐ: ÆÇµ· ¸ô¼ö + Àç·á Àý¹Ý ¾Ð·ù";
            case BetId.Rush:
                return "º¸½º¸¦ " + (int)GameBalance.BetRushSec + "ÃÊ ¾È¿¡ °ÝÆÄ\n¼º°ø: °ñµå +"
                    + GameBalance.BetRushGold + "\n½ÇÆÐ: °ÝÆÄ º¸³Ê½º ¸ô¼ö + ÃÖ´ë HP -"
                    + (int)GameBalance.BetRushHPPenalty;
            case BetId.Feast:
                return "Æ÷Å¾ " + GameBalance.BetFeastSlotsMax + "±â ÀÌÇÏ·Î °ÝÆÄ\n¼º°ø: Àü Àç·á +"
                    + GameBalance.BetFeastMats + ", ¸í¼º +" + GameBalance.BetFeastFame
                    + "\n½ÇÆÐ: °ÝÆÄ º¸³Ê½º ¸ô¼ö + °ñµå Àý¹Ý ¾Ð·ù";
            default: return "";
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Ä«µå »Ì±â / ¼ö¶ô
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    /// <summary>ÀÏ¹Ý 1Àå + µµ¹Ú 1Àå Á¦½Ã. ¿Ü»ó ÀåºÎ´Â ÆÇµ· ³¾ °ñµå°¡ ÀÖ¾î¾ß ³ª¿Â´Ù</summary>
    public static void PickCards(out BetId normalCard, out BetId gambleCard)
    {
        Active = BetId.None;   // Áö³­ ·± ÀÜÀç Á¤¸® (º¸½ºÀü¿¡¼­ Á×À¸¸é Á¤»ê ¾øÀÌ ³²À» ¼ö ÀÖÀ½)

        BetId[] normals = { BetId.OnTime, BetId.Perfect, BetId.Tank };
        normalCard = normals[Random.Range(0, normals.Length)];

        bool canStake = GameManager.Instance != null
            && GameManager.Instance.playerGold >= Mathf.RoundToInt(GameBalance.BetLedgerStake * StakesMul);
        BetId[] gambles = canStake
            ? new BetId[] { BetId.Ledger, BetId.Rush, BetId.Feast }
            : new BetId[] { BetId.Rush, BetId.Feast };
        gambleCard = gambles[Random.Range(0, gambles.Length)];
    }

    /// <summary>º£ÆÃ ¼ö¶ô (¿Ü»ó ÀåºÎ´Â ÆÇµ· Áï½Ã Â÷°¨)</summary>
    public static void Accept(BetId id)
    {
        Active = id;
        if (id == BetId.Ledger && GameManager.Instance != null)
        {
            int stake = Mathf.RoundToInt(GameBalance.BetLedgerStake * StakesMul);
            GameManager.Instance.AddGold(-stake);
            UIManager.Instance?.ShowStatChange("[º£ÆÃ] ÆÇµ· " + stake + "G¸¦ °É¾ú´Ù");
        }
        Debug.Log("[SpinoBet] º£ÆÃ ¼ö¶ô: " + TitleOf(id));
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // º¸½ºÀü ÃßÀû (ÈÅ¿¡¼­ È£Ãâ)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    /// <summary>º¸½º µîÀå ½Ã Ä«¿îÅÍ ¸®¼Â (BossGimmickSystem.RegisterBoss)</summary>
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

    /// <summary>°ÝÆÄ º¸³Ê½º ¸ô¼ö ¿©ºÎ È®ÀÎ + ¼Ò¸ð (GameManager°¡ º¸³Ê½º Áö±Þ Á÷Àü È£Ãâ)</summary>
    public static bool ConsumeForfeit()
    {
        bool f = forfeitBossBonus;
        forfeitBossBonus = false;
        return f;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Á¤»ê (º¸½º °ÝÆÄ ¼ø°£ - BossGimmickSystem.OnBossDefeated)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
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
        Debug.Log("[SpinoBet] Á¤»ê: " + TitleOf(bet) + " -> " + (win ? "½Â¸®" : "ÆÐ¹è")
            + " (½Ã°£ " + elapsed.ToString("F0") + "s, PERFECT " + perfectCooks
            + ", ÇÇ°Ý " + trainHits + ", ÅõÃ´ " + throwHits + ")");
    }

    private static void GrantReward(BetId bet)
    {
        GameManager gm = GameManager.Instance;
        switch (bet)
        {
            case BetId.OnTime:
                int onTimeGold = Mathf.RoundToInt(GameBalance.BetOnTimeGold * StakesMul);
                gm?.AddGold(onTimeGold);
                Notice("[º£ÆÃ ½Â¸®] Á¤½Ã ¹è½Ä! °ñµå +" + onTimeGold);
                break;
            case BetId.Perfect:
                int matN = Mathf.RoundToInt(GameBalance.BetPerfectMats * StakesMul);
                if (MaterialInventory.Instance != null)
                    for (int i = 0; i < matN; i++)
                        MaterialInventory.Instance.Add(RandomMaterial(), 1);
                Notice("[º£ÆÃ ½Â¸®] ¿Ïº®ÇÑ Á¢½Ã! ·£´ý Àç·á +" + matN);
                break;
            case BetId.Tank:
                float tankHP = GameBalance.BetTankMaxHP * StakesMul;
                Object.FindFirstObjectByType<TrainManager>()?.AddMaxHP(tankHP);
                Notice("[º£ÆÃ ½Â¸®] Ã¶º® ÁÖ¹æ! ÃÖ´ë HP +" + (int)tankHP);
                break;
            case BetId.Ledger:
                int payout = Mathf.RoundToInt(GameBalance.BetLedgerStake * StakesMul * GameBalance.BetLedgerPayoutMul);
                gm?.AddGold(payout);
                Notice("[º£ÆÃ ½Â¸®] ¿Ü»ó ÀåºÎ " + GameBalance.BetLedgerPayoutMul + "¹è È¸¼ö! °ñµå +" + payout);
                break;
            case BetId.Rush:
                int rushGold = Mathf.RoundToInt(GameBalance.BetRushGold * StakesMul);
                gm?.AddGold(rushGold);
                Notice("[º£ÆÃ ½Â¸®] ¼ÓÀü¼Ó°á! °ñµå +" + rushGold);
                break;
            case BetId.Feast:
                int feastMats = Mathf.RoundToInt(GameBalance.BetFeastMats * StakesMul);
                int feastFame = Mathf.RoundToInt(GameBalance.BetFeastFame * StakesMul);
                if (MaterialInventory.Instance != null)
                    foreach (MaterialType t in System.Enum.GetValues(typeof(MaterialType)))
                        MaterialInventory.Instance.Add(t, feastMats);
                MetaProgress.AddFame(feastFame);
                Notice("[º£ÆÃ ½Â¸®] ±¾ÁÖ¸° ½ÄÅ¹! Àü Àç·á +" + feastMats + ", ¸í¼º +" + feastFame);
                break;
        }
        UIManager.Instance?.ShowStatChange("½ºÇÇ³ë: \"...ÀÌ¹ø ¿ä¸®»ç´Â Á» ´Ù¸¥°¡.\"");
    }

    private static void ApplyPenalty(BetId bet)
    {
        GameManager gm = GameManager.Instance;
        switch (bet)
        {
            case BetId.OnTime:
            case BetId.Perfect:
            case BetId.Tank:
                Notice("[º£ÆÃ ½ÇÆÐ] " + TitleOf(bet) + " - ÀÒÀº °ÍÀº ÀÚÁ¸½É»ÓÀÌ´Ù");
                return;   // ÀÏ¹Ý º£ÆÃ: ¹«¼Õ½Ç, ½ºÇÇ³ë Á¶·Õµµ ¾øÀ½

            case BetId.Ledger:
                // ÆÇµ·Àº ÀÌ¹Ì ³ª°¬°í, Àç·á ¾Ð·ù (±âº» Àý¹Ý - 'ÆÇµ· µÎ ¹è'¸é ÀüºÎ)
                float seizeRatio = Mathf.Min(1f, 0.5f * StakesMul);
                if (MaterialInventory.Instance != null)
                    foreach (MaterialType t in System.Enum.GetValues(typeof(MaterialType)))
                    {
                        int loss = Mathf.FloorToInt(MaterialInventory.Instance.Get(t) * seizeRatio);
                        if (loss > 0) MaterialInventory.Instance.Add(t, -loss);
                    }
                Danger("[º£ÆÃ ÆÐ¹è] ¿Ü»ó ÀåºÎ - ÆÇµ· ¸ô¼ö, Àç·á " + (seizeRatio >= 1f ? "ÀüºÎ" : "Àý¹Ý") + " ¾Ð·ù!");
                break;

            case BetId.Rush:
                forfeitBossBonus = true;
                float hpPen = GameBalance.BetRushHPPenalty * StakesMul;
                Object.FindFirstObjectByType<TrainManager>()?.AddMaxHP(-hpPen);
                Danger("[º£ÆÃ ÆÐ¹è] ¼ÓÀü¼Ó°á - °ÝÆÄ º¸³Ê½º ¸ô¼ö, ÃÖ´ë HP -" + (int)hpPen + "!");
                break;

            case BetId.Feast:
                forfeitBossBonus = true;
                float goldRatio = Mathf.Min(1f, 0.5f * StakesMul);
                if (gm != null && gm.playerGold > 0)
                    gm.AddGold(-Mathf.FloorToInt(gm.playerGold * goldRatio));
                Danger("[º£ÆÃ ÆÐ¹è] ±¾ÁÖ¸° ½ÄÅ¹ - °ÝÆÄ º¸³Ê½º ¸ô¼ö, °ñµå "
                    + (goldRatio >= 1f ? "ÀüºÎ" : "Àý¹Ý") + " ¾Ð·ù!");
                break;
        }
        UIManager.Instance?.ShowStatChange("½ºÇÇ³ë: \"°í¸¿°Ô ¹ÞÁö. µµ¹ÚÀº ¿ø·¡ ÁýÀÌ ÀÌ±â´Â °Å´Ù.\"");
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÇïÆÛ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
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
