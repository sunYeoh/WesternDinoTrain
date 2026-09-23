using UnityEngine;

/// <summary>
/// [BriefingTexts.cs] v2.4 (v9.12 2026-09-22: 견습 구간화 - 미니 보스 예습 카드·구간 완료 카드·협곡 낙뢰 사고 카드, 첫 보스 카드는 예습 여부로 분기, 굽기 카드 제목 = 그릴만, 리롤 복원(유저 09-22)) / v2.3 (v9.11.1 2026-09-22 문구 검토 반영: 첫 카드는 지금 행동만·시간 정지 명시·수리 시점·실행 가능한 조언·이번 운행/다음 운행·공명 예외·그로기=무방비·디버프 요리=독샘 요리·리롤=다시 뽑기) / v2.2 (v9.10.1 2026-09-21: 재료 이름 MaterialNames - 전기알·화염꽃·얼음꽃·독샘) / [BriefingTexts.cs] v2.1 (v9.10 2026-09-17: RecipeIntro(레시피) 소개 카드 - 웨이브 3 용암 폭탄밥 / 숙련·베팅 문구 일상어) / v2 (v9.9.2 2026-09-16: 정식 런 첫 등장 카드 34장 - 지역 4 / 새 손님 16 / 주방 사고 4 / 베팅 1 / 보스 4 / 승격 5.
///   스피노 카드는 전부 실루엣 초상(ui_npc_spino) + 키, 정비소는 안킬로(ui_npc_ankylo). 12번 카드는 실제 베팅 창(카드 2장, [1] 일반 / [2] 도박 / [0] 거절)에 맞춤)
///   / v1 (신규, v9.9 2026-09-16) - 브리핑 카드 문구 표
///
/// 문구는 전부 코드로 확인한 값만 쓴다:
///   손님 = Enemy.EnemyData(이름·특기·노리는 곳·드랍) + Enemy.AutoAssignDefense 의 방어/저항 표 + GetDropMaterialType 의 재료 규칙
///   사고 = KitchenEvents (제목·조작·제한 시간·실패 피해) / 베팅 = SpinoBet (일반 3 = 실패 무손실, 도박 3 = 대가) / 보스 = BossEnemy v7 패턴
///   승격 5 = TutorialHint 의 기존 힌트를 카드로 (id 는 그대로 first_town 등)
/// 톤: 스피노(스토리바이블 "카론+멘토") - 명령형, 브래킷, 적은 "손님/굶주린 것들", 위험은 "베팅". 지역·손님·사고·보스 카드의 화자는 명판 그대로.
/// 한 줄은 34자 안쪽 (본문 폭 560px / 17pt). 넘치면 카드가 접는다.
///
/// 사용법:
///   BriefingTexts.Tutorial(step) / TutorialPtera() / TutorialDone(...) / TutorialRetry()    - 견습 운행 (TutorialDirector)
///   BriefingTexts.Region(1~4) / Enemy(Enemy.EnemyData) / Event("intrusion"|"break"|"fire"|"spill") / BetFirst() / Boss(1~4) / Promoted(id)
///   -> BriefingUI.ShowOnce(id, def) 로 한 번만 (WaveManager / KitchenEventManager / SpinoBetUI / TutorialHint 가 호출)
/// VS 2017 (C# 7.3) 호환
/// </summary>
public static class BriefingTexts
{
    private static readonly Color BRASS = new Color(0.84f, 0.667f, 0.282f, 1f);
    private static readonly Color POISON = new Color(0.678f, 0.451f, 0.910f, 1f);
    private static readonly Color RED = new Color(0.84f, 0.20f, 0.22f, 1f);
    private static readonly Color ELEC = new Color(1f, 0.84f, 0.25f, 1f);
    private static readonly Color FIRE = new Color(0.91f, 0.38f, 0.19f, 1f);
    private static readonly Color ICE = new Color(0.47f, 0.78f, 1f, 1f);
    private static readonly Color ARMOR = new Color(0.62f, 0.66f, 0.70f, 1f);
    private static readonly Color MEAT = new Color(0.86f, 0.45f, 0.36f, 1f);

    private const string SPINO = "ui_npc_spino";
    private const string ANKYLO = "ui_npc_ankylo";

    private static BriefingUI.BriefDef Make(string speaker, string title, string[] lines, string keyGlyph, string keyLabel)
    {
        BriefingUI.BriefDef d = new BriefingUI.BriefDef();
        d.speaker = speaker; d.title = title; d.lines = lines;
        d.keyGlyph = keyGlyph; d.keyLabel = keyLabel; d.ring = BRASS;
        if (speaker == "스피노") d.portrait = SPINO;        // 스피노가 말하는 카드는 전부 실루엣 (유저 09-16: 이름만 있으면 불편)
        else if (speaker == "안킬로") d.portrait = ANKYLO;
        return d;
    }

    /// <summary>1회성 카드 id 전부 (치트 F4 리셋용 - BriefingUI.ResetSeen)</summary>
    public static string[] AllOnceIds()
    {
        return new string[] {
            "region_1", "region_2", "region_3", "region_4",
            "enemy_raptor", "enemy_ankylo", "enemy_cactus", "enemy_scorpion", "enemy_tortoise", "enemy_bolt", "enemy_ptera", "enemy_parasaur",
            "enemy_fly", "enemy_steel", "enemy_flame", "enemy_mosa", "enemy_pachy", "enemy_carno", "enemy_mammoth", "enemy_necro",
            "event_intrusion", "event_break", "event_fire", "event_spill", "event_lightning",
            "bet_first",
            "boss_1", "boss_2", "boss_3", "boss_4",
            "promo_first_town", "promo_first_augment", "promo_first_route", "promo_first_item", "promo_first_overheat",
            "recipe_fire_fire" };
    }

    // ─────────────────────────────────────────────
    // 견습 운행
    // ─────────────────────────────────────────────
    /// <summary>견습 운행 단계 시작 브리핑 (1~13). 없는 단계는 null</summary>
    public static BriefingUI.BriefDef Tutorial(int step)
    {
        switch (step)
        {
            case 1:
                return Make("스피노", "여기가 네 주방이자 포대다", new string[] {
                    "기차는 네 칸 - 기관실, 주방, 포탑 칸 둘. 셰프는 너 하나다.",
                    "칸 사이는 통로 발판으로만 건넌다. 난간은 못 넘는다.",
                    "[WASD] 달리고 [Shift] 대시. 발이 느리면 접시가 식는다.",
                    "먼저 오른쪽 포탑 칸으로 - 통로 발판을 건너 화살표 자리까지." },
                    "[WASD]", "[Shift] 대시");
            case 2:
                return Make("스피노", "요리가 곧 포탑이다", new string[] {
                    "보급 요리 한 접시가 하단 바에 있다. 요리 카드를 클릭해라.",
                    "그다음 포탑 이름표(빈 칸의 [+])를 클릭 - 그게 투입이다.",
                    "요리 하나 = 포탑 하나. 우클릭은 취소.",
                    "화살표가 가리키는 이름표에 넣어라. 첫 손님이 곧 온다." },
                    "클릭", "요리 카드 → 이름표");
            case 3:
                return Make("스피노", "손님이다 - 포탑이 알아서 쏜다", new string[] {
                    "포탑은 사거리 안의 손님을 알아서 겨눈다. 넌 쏠 필요 없다.",
                    "네가 할 일은 접시를 늘리는 것 - 포탑이 많을수록 화력이다.",
                    "쓰러진 손님은 재료를 남긴다. 조각이 기차로 날아온다.",
                    "이번엔 구경만 해라. 기차는 다치지 않는다." },
                    "자동", "포탑이 알아서 쏜다");
            case 4:
                return Make("스피노", "재료가 왔다 - 하단 바를 봐라", new string[] {
                    "왼쪽 재료 칸: 고기·등심·전기알·화염꽃·얼음꽃·독샘 여섯 가지.",
                    "재료 두 개 = 요리 한 접시. 고기 둘이면 더블 육포다.",
                    "재료는 알아서 빨려 온다. 갑판에 떨어진 상자(유물)만 밟아서 줍는다.",
                    "고기 2개가 찼다. 이제 굽는다." },
                    "재료 2", "= 요리 1");
            case 5:
                return Make("스피노", "불 앞에 서라 - 그릴부터", new string[] {
                    "그릴 곁에 서서 [E] - 조리창이 열린다. 더블 육포를 골라라.",
                    "움직이는 눈금이 판정 칸(초록) 안에 왔을 때 [Space].",
                    "칸 한가운데면 PERFECT - 접시가 두 개. 칸 안이면 GOOD - 한 개.",
                    "칸을 벗어나면 태운다 - 재료만 없어진다. [ESC] 중단은 재료를 돌려받는다." },
                    "[E]", "그릴 곁에서");
            case 6:
                return Make("스피노", "같은 접시를 다시 넣으면 레벨업", new string[] {
                    "가동 중인 포탑에 같은 요리를 또 넣으면 레벨이 오른다.",
                    "다른 요리를 빈 칸에 넣으면 새 포탑. 종류가 곧 전략이다.",
                    "레벨이 오르면 이름표의 Lv 숫자가 오르고 공격이 세진다.",
                    "방금 구운 육포를 첫 포탑에 넣어라." },
                    "클릭", "요리 카드 → 포탑");
            case 7:
                return Make("스피노", "낙뢰다 - 포탑이 멈췄다", new string[] {
                    "감전된 포탑은 다시 만질 때까지 한 발도 못 쏜다. 스파크가 그 표시다.",
                    "곁으로 달려가 [E] 한 번 - 털어내면 바로 재가동.",
                    "가만두면 " + Mathf.RoundToInt(GameBalance.LightningStunSec) + "초는 멈춰 있다. 그동안 손님은 기다려 주지 않는다.",
                    "네가 주방에만 있을 수 없는 이유다. 협곡에 들어서면 웨이브마다 한 번쯤 친다." },
                    "[E]", "멈춘 포탑 곁에서");
            case 8:
                return Make("스피노", "창밖 바위는 재료 광맥이다", new string[] {
                    "기관실 작살포 곁에서 [E] - 길가를 지나는 바위를 낚아라.",
                    "낚으면 바위 색 재료가 " + GameBalance.HarpoonMatMin + "~" + GameBalance.HarpoonMatMax + "개. 재장전은 " + Mathf.RoundToInt(GameBalance.HarpoonCooldown) + "초.",
                    "정식 운행에선 가끔 굶주린 것들이 딸려온다 - 손님 없을 때 노려라.",
                    "바위가 온다. 기관실로 달려가라." },
                    "[E]", "기관실 작살포");
            case 9:
                return Make("스피노", "레버 - 전속 주행은 베팅이다", new string[] {
                    "기관실 레버 곁에서 [E] 를 " + GameBalance.LeverHoldSec + "초 꾹 - 전속 주행.",
                    "빨리 달리면 처치 골드 x" + GameBalance.LeverGoldMul + ", 대신 손님이 더 자주 오고 판정 칸이 좁아진다.",
                    "벌이가 좋은 만큼 위험하다. 걸지 말지는 네 몫.",
                    "한 번 당겨 봐라. 다시 당기면 원래대로." },
                    "[E] 꾹", "기관실 레버");
            case 10:
                return Make("스피노", "손님이 몰려온다 - 배운 대로 막아라", new string[] {
                    "이번엔 진짜다. 기차도 다친다.",
                    "접시를 늘리고, 멈춘 포탑은 달려가 털어라.",
                    "같은 속성 포탑 " + GameBalance.ResonanceCount + "문이면 공명 - 그 속성 피해 +" + Mathf.RoundToInt(GameBalance.ResonanceBonus * 100f) + "% (방어 계열은 기차가 받는 피해 -10%).",
                    "기차가 멈추면 이 단계만 다시 한다. 새 손님이 하나 섞여 있다 - 다음 카드." },
                    "방어", "전부 쓰러뜨려라");
            case 11:
                return Make("스피노", "정산 - 증강과 정비소", new string[] {
                    "웨이브를 넘기면 정산이다. 증강 카드 셋 중 하나 - [1~5] 숫자키.",
                    "[0] 건너뛰면 명성이 조금, [9] 리롤(선택지 새로 뽑기)은 골드가 든다.",
                    "정차역에선 [G] 정비소 - 기차 수리·칼 연마·재료 시장. 수리와 장갑 보강은 정차 중에만 판다.",
                    "칼과 팬은 쓸수록 닳는다. 하단 바 오른쪽 명판이 상태다." },
                    "[1~5]", "[G] 정비소");
            case 12:
                return Make("스피노", "도박꾼 스피노 - 나 말이다", new string[] {
                    "보스 앞 정차역에 내가 온다. 카드는 둘 - [1] 일반, [2] 도박.",
                    "일반은 져도 잃는 게 없다. 도박은 판돈·재료·골드·HP 를 건다.",
                    "[0] 거절해도 된다. 하지만 크게 따는 건 판돈이 클 때뿐이지.",
                    "조건은 그때 카드에 적혀 있다 - 시간 안 격파, 완벽 조리, 피격 횟수 같은 것들." },
                    "[1] / [2]", "일반 / 도박");
            case 13:
                return Make("스피노", "앞길 - 지역 셋과 최종전", new string[] {
                    "구리 사막, 테슬라 협곡, 코발트 광산. 그리고 최종전.",
                    "새 손님·사고·보스는 처음 만날 때 이런 카드로 알려주지. [H]로 다시 읽는다.",
                    "[M] 명성 상점은 다음 운행에도 남는 강화. [J] 선대의 일지는 이야기다.",
                    "견습은 끝이다. 출발해라, 신입." },
                    "[출발]", "로비에서 [Enter]");
        }
        return null;
    }

    /// <summary>단계 10 앞에 붙는 새 손님 카드 - 독침 프테라 (정식 런 첫 등장 카드와 같은 생성기, 기록은 남기지 않는다)</summary>
    public static BriefingUI.BriefDef TutorialPtera()
    {
        return Enemy(global::Enemy.PoisonPtera);
    }

    /// <summary>견습 운행 완료 요약 카드</summary>
    public static BriefingUI.BriefDef TutorialDone(bool firstTime, int reward, float seconds, int skips, int cookFails)
    {
        int m = Mathf.FloorToInt(seconds / 60f), s = Mathf.FloorToInt(seconds % 60f);
        string stat = "걸린 시간 " + m + "분 " + s + "초  /  건너뛴 단계 " + skips + "  /  조리 실패 " + cookFails;
        BriefingUI.BriefDef d = firstTime && reward > 0
            ? Make("견습 운행 완료", "명성 +" + reward + " - 황야가 너를 기억한다", new string[] {
                stat,
                "이제 진짜 손님들이다. 로비에서 [출발]을 눌러라.",
                "다시 보고 싶으면 언제든 [T] - 보상은 처음 한 번뿐이다." }, "완료", "[Enter] 로비로")
            : Make("견습 운행 완료", "복습 끝 - 보상은 처음 한 번뿐이다", new string[] {
                stat,
                "이제 진짜 손님들이다. 로비에서 [출발]을 눌러라." }, "완료", "[Enter] 로비로");
        return d;
    }

    /// <summary>v9.12 구간 7 시작 브리핑 - 왜 미끼인가 (목업 v4.2 (D))</summary>
    public static BriefingUI.BriefDef TutorialBossPractice()
    {
        return Make("스피노", "첫 보스 예습 - 미끼로 무리를 유인한다", new string[] {
            "곧 진짜 왕(녹슨 발톱)이 온다. 그 전에 새끼로 연습이다.",
            "왕은 세다. 정면으로 때리면 기차가 먼저 부서진다.",
            "무리가 미끼를 물면 왕도 따라온다 - 그동안 포탑이 때린다.",
            "왼쪽 아래 미끼 화덕에서 고기를 굽는다. 유인이 끝나면 다시 기차를 노린다." },
            "[미끼 굽기]", "왼쪽 아래 화덕");
    }

    /// <summary>v9.12 구간 7 실패(기차 HP 50% 아래) 뒤 다시 - 한 번만</summary>
    public static BriefingUI.BriefDef TutorialBossRetry()
    {
        BriefingUI.BriefDef d = Make("스피노", "기차가 너무 물렸다 - 다시", new string[] {
            "무리를 기차 곁에 두면 왕이 같이 문다. 기차를 고쳐 놨다.",
            "먼저 미끼를 던져라 - 무리가 미끼로 가 있는 동안 포탑이 때린다.",
            "고기가 없으면 못 굽는다. 고기를 채워 뒀다." }, "다시", "미끼부터");
        d.ring = RED;
        return d;
    }

    /// <summary>v9.12 한 구간만 연습했을 때의 완료 카드 (훈련장에서 들어온 런). 전부 돌린 런은 TutorialDone</summary>
    public static BriefingUI.BriefDef TutorialSegmentDone(string segTitle, float seconds, bool skipped)
    {
        int m = Mathf.FloorToInt(seconds / 60f), sec = Mathf.FloorToInt(seconds % 60f);
        return Make("훈련장", skipped ? "구간 끝 - 건너뛴 단계가 있다" : "구간 완료 - " + segTitle, new string[] {
            "걸린 시간 " + m + "분 " + sec + "초" + (skipped ? "  /  건너뛴 단계가 있어 완료로 적지 않는다" : ""),
            "로비로 돌아간다. 다른 구간은 [T] 훈련장에서 고른다." }, "완료", "[Enter] 로비로");
    }

    /// <summary>단계 실패/재시작 안내 (단계 10 기차 정지)</summary>
    public static BriefingUI.BriefDef TutorialRetry()
    {
        BriefingUI.BriefDef d = Make("스피노", "기차가 멈췄다 - 다시", new string[] {
            "손님이 너무 많았나. 기차를 고쳐 놨다.",
            "요리를 더 넣어라 - 접시 수가 곧 화력이다.",
            "멈춘 포탑이 있으면 먼저 털어라." }, "재시작", "이 단계만 다시");
        d.ring = RED;
        return d;
    }

    // ─────────────────────────────────────────────
    // 정식 런 첫 등장 - 지역 4 (지역 첫 웨이브 예고 때. 최종전은 1장)
    // ─────────────────────────────────────────────
    public static BriefingUI.BriefDef Region(int region)
    {
        int len = GameBalance.RegionLength;
        BriefingUI.BriefDef d;
        Color tint;
        switch (region)
        {
            case 1:
                d = Make("지역 1", "구리 사막 - 웨이브 1 ~ " + len, new string[] {
                    "녹슨 모래. 손님은 랩터 무리부터 - 새 얼굴은 그때마다 카드로 알려준다.",
                    "재료 두 개 = 접시 하나. 접시가 곧 포탑, 포탑 수가 곧 화력이다.",
                    "웨이브를 넘기면 정산(증강) → 정차역([G] 정비소) → 다음 웨이브.",
                    "지역 마지막 웨이브 " + len + " 에 보스 '녹슨 발톱' - 무방비(그로기)일 때 [F] 로 독샘 요리를 던진다." }, "", "");
                d.portrait = EnemySkin.PortraitFor(global::Enemy.SteamRaptor.enemyName, out tint); d.portraitTint = tint; d.ring = BRASS;
                return d;
            case 2:
                d = Make("지역 2", "테슬라 협곡 - 웨이브 " + (len + 1) + " ~ " + (len * 2), new string[] {
                    "번개가 둥지를 트는 곳. 하늘에서 오는 손님이 많다 - 급강하·저격.",
                    "날개 달린 것들은 방어 0 / 저항 30 - 물리 요리(육포·스테이크)가 잘 박힌다.",
                    "낙뢰가 포탑을 감전시킨다. 스파크가 보이면 달려가 [E] 한 번.",
                    "보스 '천둥 둥지' - 낙뢰 마지막 순간 [Space] 패링, 병 셋이면 되쏘기." }, "", "");
                d.portrait = EnemySkin.PortraitFor(global::Enemy.BoltTeranodon.enemyName, out tint); d.portraitTint = tint; d.ring = ELEC;
                return d;
            case 3:
                d = Make("지역 3", "코발트 광산 - 웨이브 " + (len * 2 + 1) + " ~ " + (len * 3), new string[] {
                    "대붕괴가 가장 깊이 남은 곳. 여기부터는 손님도 정예다 - 두껍고 느리다.",
                    "방어·저항이 다 높은 놈들 - 공명(같은 속성 " + GameBalance.ResonanceCount + "문)과 레벨로 밀어라.",
                    "결빙·정지 - 기차 바퀴를 멈추는 손님이 있다. 사거리 긴 포탑으로 붙기 전에 때려라.",
                    "보스 '동면자' - 빙하 갑주는 화염으로만 녹는다. 광산의 해동포에 화염꽃 요리를." }, "", "");
                d.portrait = EnemySkin.PortraitFor(global::Enemy.IceMosa.enemyName, out tint); d.portraitTint = tint; d.ring = ICE;
                return d;
            default:
                d = Make("최종전", "황야의 끝 - 웨이브 " + GameBalance.FinalWave, new string[] {
                    "대륙에서 가장 오래 굶은 손님이 식탁에 앉는다 - 메카 티렉스 '디 오리지널'.",
                    "사냥(포효 소환) → 폭식(재료 조각을 먹고 회복·강화 - 조각이 보스 곁에 떨어지면 뺏긴다) → 해치 개방.",
                    "무방비(그로기)는 HP 75 / 50 / 25% 그리고 12% 에 한 번 더 - [F] 독샘 요리.",
                    "마지막 주문이 온다. 이 판을 넘기면 끝이다." }, "", "");
                d.portrait = "e_raptor"; d.portraitTint = new Color(1f, 0.5f, 0.45f); d.ring = RED;
                return d;
        }
    }

    // ─────────────────────────────────────────────
    // 정식 런 첫 등장 - 새 손님 16 (그 종류가 처음 포함된 웨이브 예고 때)
    // ─────────────────────────────────────────────
    /// <summary>손님 데이터로 카드 생성. 방어/저항은 Enemy.AutoAssignDefense 와 같은 표, 재료는 GetDropMaterialType 과 같은 규칙</summary>
    public static BriefingUI.BriefDef Enemy(global::Enemy.EnemyData data)
    {
        string name = data.enemyName ?? "";
        float def, res; DefRes(name, out def, out res);
        MaterialType mat = DropTypeOf(data.dropMaterialName);
        string advice;
        if (def >= res + 10f) advice = "속성 요리(전기알·화염꽃·얼음꽃·독샘 요리)가 잘 박힌다";
        else if (res >= def + 10f) advice = "물리 요리(육포·스테이크·등심)가 잘 박힌다";
        else if (def >= 30f) advice = "둘 다 두껍다 - 레벨·공명으로 밀어라";
        else advice = "아무 요리나 박힌다 - 화력 싸움";

        string tip;
        if (name.Contains("플라이")) tip = "포탑 곁에서 터진다(공격 " + Mathf.RoundToInt(data.baseATK) + ") - 닿기 전에 떨어뜨려라.";
        else if (name.Contains("프테라")) tip = "먼저 떨어뜨리고 나서 조리대에 서라.";
        else if (name.Contains("파라사우") || name.Contains("네크로")) tip = "곁의 손님을 세게 만든다 - 무리째 맞는 요리(폭발·관통)로 같이 때려라.";
        else if (name.Contains("모사") || name.Contains("맘모스")) tip = "기차를 세우는 놈이다 - 사거리 긴 포탑이 붙기 전에 때린다.";
        else if (name.Contains("강철")) tip = "물리 요리는 반만 박힌다(방어 50) - 속성 요리(전기알·화염꽃·얼음꽃·독샘)를 세워라.";
        else if (name.Contains("카르노")) tip = "화상은 계속 받는 피해다 - 정차하면 [G] 정비소에서 기차를 고쳐라.";
        else if (name.Contains("전갈")) tip = "독은 칼·팬을 갉는다 - 정차역에서 연마.";
        else if (name.Contains("캑터스")) tip = "멀리서 던진다 - 사거리 긴 포탑이 먼저 닿는다.";
        else if (name.Contains("파키")) tip = "맞은 만큼 일부를 되돌린다 - 기차 HP 가 낮을 땐 조심.";
        else if (name.Contains("거북") || name.Contains("아르마딜로")) tip = "느리지만 두껍다 - 엔진 쪽을 노린다.";
        else if (name.Contains("테라노돈") || name.Contains("익룡")) tip = "포탑·주방으로 급강하 - 하늘도 사거리다.";
        else tip = "무리로 온다 - 접시 수가 곧 답이다.";

        BriefingUI.BriefDef d = Make("새 손님", name + " - " + ShortAbility(data.specialAbility), new string[] {
            data.specialAbility + ". 노리는 곳: " + data.targetPriority + ".",
            "방어 " + Mathf.RoundToInt(def) + " / 저항 " + Mathf.RoundToInt(res) + " - " + advice + ".",
            "쓰러뜨리면 [" + data.dropMaterialName + "] - " + MatName(mat) + " 재료. 골드 " + data.goldReward + ".",
            tip }, "", "");
        Color tint;
        d.portrait = EnemySkin.PortraitFor(name, out tint); d.portraitTint = tint;
        d.ring = MatColor(mat);
        return d;
    }

    /// <summary>특기 문구를 제목용으로 짧게 (괄호 앞까지)</summary>
    private static string ShortAbility(string ability)
    {
        if (string.IsNullOrEmpty(ability)) return "손님";
        int cut = ability.IndexOf('(');
        string s = cut > 0 ? ability.Substring(0, cut) : ability;
        return s.Trim();
    }

    /// <summary>Enemy.AutoAssignDefense 와 같은 표 (이름 부분 일치, 없으면 0/0)</summary>
    private static void DefRes(string n, out float def, out float res)
    {
        def = 0f; res = 0f;
        if (n.Contains("아르마딜로") || n.Contains("안킬로")) { def = 35f; res = 5f; }
        else if (n.Contains("거북")) { def = 45f; res = 5f; }
        else if (n.Contains("강철")) { def = 50f; res = 20f; }
        else if (n.Contains("테라노돈") || n.Contains("프테라") || n.Contains("익룡")) { def = 0f; res = 30f; }
        else if (n.Contains("전갈")) { def = 12f; res = 12f; }
        else if (n.Contains("파라사우")) { def = 20f; res = 20f; }
        else if (n.Contains("모사")) { def = 40f; res = 40f; }
        else if (n.Contains("파키")) { def = 45f; res = 30f; }
        else if (n.Contains("카르노")) { def = 15f; res = 30f; }
        else if (n.Contains("맘모스")) { def = 50f; res = 35f; }
        else if (n.Contains("스피노")) { def = 30f; res = 30f; }
    }

    /// <summary>Enemy.GetDropMaterialType 과 같은 규칙</summary>
    private static MaterialType DropTypeOf(string n)
    {
        if (string.IsNullOrEmpty(n)) return MaterialType.Meat;
        if (n.Contains("랩터 고기")) return MaterialType.Meat;
        if (n.Contains("등심") || n.Contains("등딱지") || n.Contains("비늘") || n.Contains("결정")) return MaterialType.Armor;
        if (n.Contains("전기") || n.Contains("자기장")) return MaterialType.Elec;
        if (n.Contains("화염") || n.Contains("오일")) return MaterialType.Fire;
        if (n.Contains("얼음") || n.Contains("서리")) return MaterialType.Ice;
        if (n.Contains("독") || n.Contains("뼛가루")) return MaterialType.Poison;
        return MaterialType.Meat;
    }

    private static string MatName(MaterialType m) { return MaterialNames.Kor(m); }   // v2.2: 재료 이름 한 곳

    private static Color MatColor(MaterialType m)
    {
        switch (m)
        {
            case MaterialType.Armor: return ARMOR;
            case MaterialType.Elec: return ELEC;
            case MaterialType.Fire: return FIRE;
            case MaterialType.Ice: return ICE;
            case MaterialType.Poison: return POISON;
            default: return MEAT;
        }
    }

    // ─────────────────────────────────────────────
    // v9.10: 레시피 소개 카드 (개정안 §5 W3 - "용암 폭탄밥"처럼 학습 구간에 소개할 요리. 문구는 RecipeText 가 RecipeData 에서 만든다)
    // ─────────────────────────────────────────────
    public static BriefingUI.BriefDef RecipeIntro(string recipeId)
    {
        RecipeData r = RecipeDatabase.Get(recipeId);
        if (r == null) return null;
        string[] parts = recipeId.Replace("T2:", "").Split('+');
        string src = parts.Length >= 2 ? MatNameKey(parts[0]) + " + " + MatNameKey(parts[1]) : recipeId;
        BriefingUI.BriefDef d = Make("스피노", "새 접시 - " + r.displayName + " (" + RecipeText.RoleWord(r) + ")", new string[] {
            "재료 " + src + " → " + RecipeText.MethodWord(r) + ". 재료는 넣어 뒀다.",
            RecipeText.What(r),
            "쓰는 때: " + RecipeText.When(r),
            "무리가 온다. 만들어서 빈 칸에 넣고, 육포와 뭐가 다른지 봐라." }, "[E]", RecipeText.MethodWord(r));
        return d;
    }

    private static string MatNameKey(string key) { return MaterialNames.Kor(key); }   // v2.2: 재료 이름 한 곳

    // ─────────────────────────────────────────────
    // 정식 런 첫 등장 - 주방 사고 4 (첫 발생 순간, KitchenEvents 의 제목·조작·제한 시간·실패 피해)
    // ─────────────────────────────────────────────
    public static BriefingUI.BriefDef Event(string key)
    {
        BriefingUI.BriefDef d;
        switch (key)
        {
            case "intrusion":
                d = Make("주방 사고", "침입자 - 랩터가 주방에 들어왔다", new string[] {
                    "화살표가 가리키는 현장으로 달려가라. 도착해야 수습이 시작된다.",
                    "[E] 연타 - 게이지를 채우면 몰아낸다. 제한 6.5초.",
                    "못 몰아내면 기차가 60 피해. 조리는 그 다음이다.",
                    "사고는 전투 중 가끔 온다. 경보가 울리면 손부터 멈춰라." }, "[E] 연타", "현장에서");
                d.portrait = "e_raptor"; d.ring = RED;
                return d;
            case "break":
                d = Make("주방 사고", "기구 고장 - 배선에서 불꽃이 튄다", new string[] {
                    "현장으로 달려가서 화면의 방향키 순서대로 입력 - 수리.",
                    "틀리면 처음부터. 제한 7초.",
                    "못 고치면 기차가 40 피해.",
                    "사고는 전투 중 가끔 온다. 경보가 울리면 손부터 멈춰라." }, "방향키", "순서대로");
                d.portrait = "ui_ev_spark_1"; d.ring = ELEC;
                return d;
            case "fire":
                d = Make("주방 사고", "화재 - 기차가 계속 타들어간다", new string[] {
                    "현장으로 달려가서 [E] 꾹 - 불길 게이지를 100% 까지.",
                    "타는 동안 기차가 계속 피해를 입는다. 제한 8초.",
                    "못 끄면 50 추가 피해. 유물 '구리 소화기'가 있으면 웨이브당 1회 자동 진압.",
                    "사고는 전투 중 가끔 온다. 경보가 울리면 손부터 멈춰라." }, "[E] 꾹", "불길 앞에서");
                d.portrait = "ui_ev_fire_1"; d.ring = FIRE;
                return d;
            case "lightning":
                d = Make("사고", "낙뢰 - 포탑이 감전됐다", new string[] {
                    "협곡의 번개가 포탑 하나를 때렸다. 스파크가 튀는 동안 그 포탑은 한 발도 못 쏜다.",
                    "곁으로 달려가 [E] 한 번 - 바로 재가동.",
                    "가만두면 " + Mathf.RoundToInt(GameBalance.LightningStunSec) + "초 뒤에 풀린다. 그동안 손님은 기다려 주지 않는다.",
                    "이 협곡에선 웨이브마다 한 번쯤 친다. 스파크가 보이면 손부터 멈춰라." }, "[E]", "멈춘 포탑 곁에서");
                d.portrait = "ui_ev_spark_1"; d.ring = ELEC;
                return d;
            default:
                d = Make("주방 사고", "흔들림 - 재료가 바닥에 쏟아졌다", new string[] {
                    "바닥에 떨어진 재료를 [마우스 좌클릭]으로 전부 주워라. 제한 7.5초.",
                    "못 주운 재료는 사라지고, 하나당 기차 10 피해.",
                    "주방 칸 안이라 멀리 갈 일은 없다 - 손만 빠르게.",
                    "사고는 전투 중 가끔 온다. 경보가 울리면 손부터 멈춰라." }, "좌클릭", "떨어진 재료");
                d.portrait = "ui_mat_meat"; d.ring = MEAT;
                return d;
        }
    }

    // ─────────────────────────────────────────────
    // 정식 런 첫 등장 - 도박꾼 베팅 (보스 직전 정차, SpinoBetUI 가 열리기 직전)
    // ─────────────────────────────────────────────
    public static BriefingUI.BriefDef BetFirst()
    {
        return Make("스피노", "베팅 - 보스전에 뭘 걸겠나", new string[] {
            "카드 둘. [1] 일반 = 조건 달성하면 상, 실패해도 잃는 게 없다.",
            "[2] 도박 = 판돈·재료·골드·최대 HP 를 건다. 따면 크고, 지면 아프다.",
            "[0] 이나 [ESC] 로 거절. 다음 역에서 또 온다.",
            "조건은 카드에 적혀 있다 - 시간 안 격파, 완벽 조리, 피격 횟수, 무방비 때 투척 명중." }, "[1] / [2]", "[0] 거절");
    }

    // ─────────────────────────────────────────────
    // 정식 런 첫 등장 - 보스 4 (스폰 순간 = HP 바가 뜨는 순간)
    // ─────────────────────────────────────────────
    public static BriefingUI.BriefDef Boss(int region)
    {
        BriefingUI.BriefDef d;
        switch (region)
        {
            case 1:
                {
                    // v9.12: 미끼 화덕 줄은 예습(견습 구간 7) 을 했나로 갈린다
                    string bait = TutorialDirector.SegDone(7)
                        ? "미끼 화덕은 예습 때 한 그대로 - 왼쪽 아래 [미끼 굽기] 로 무리와 왕을 유인해라."
                        : "왼쪽 아래 미끼 화덕: 고기 1개를 구워 던지면 무리가 몰리고 왕도 따라온다 - 그동안 포탑이 때린다.";
                    d = Make("보스", "녹슨 발톱 - 무리의 왕", new string[] {
                        "알파 랩터. 빠르고 가볍다. 패턴 '사냥 호령' = 랩터 소환 - 예고 중에 마비시키면 절반.",
                        bait,
                        "HP 75 / 50 / 25% 에서 가슴 해치가 열린다 = 무방비(그로기). 그때 [F] 로 독샘 요리를 던져라.",
                        "던질 요리는 독샘으로 만든다 (독침 육포·마비독 꼬치). 긴급 보급으로 독샘 1개가 왔다." }, "[F]", "무방비 때 투척");
                }
                d.portrait = "e_raptor"; d.portraitTint = new Color(0.9f, 0.55f, 0.38f); d.ring = RED;
                return d;
            case 2:
                d = Make("보스", "천둥 둥지 - 프테라 여왕", new string[] {
                    "멀리서 때린다. 패턴 '낙뢰 폭격' = 포탑 " + GameBalance.LightningSlotCount + "기 감전 - 달려가 [E] 한 번.",
                    "낙뢰 예고 게이지 끝자락에 [Space] = 번개 병 패링 (낙뢰 무효 + 병 1).",
                    "병 셋이면 되쏘기 - 보스가 바로 무방비가 된다. 남은 병은 처치 후 전기알로.",
                    "무방비(75 / 50 / 25%) 때 [F] 독샘 요리. HP 50% 아래면 패턴이 빨라진다." }, "[Space]", "낙뢰 끝자락 패링");
                d.portrait = "e_ptera"; d.portraitTint = new Color(0.72f, 0.72f, 1f); d.ring = ELEC;
                return d;
            case 3:
                d = Make("보스", "동면자 - 고대 모사", new string[] {
                    "느리고 단단하다. 개전과 동시에 '빙하 갑주' - 받는 피해 90% 감소.",
                    "갑주는 화염으로만 녹는다. 화상이 쌓이면 깨지고 보너스로 무방비가 된다.",
                    "광산의 해동포에 화염꽃 요리를 장전해 쏘면 갑주가 부서진다. 50% 아래서 한 번 다시 두른다.",
                    "무방비(75 / 50 / 25%) 때 [F] 독샘 요리." }, "화염꽃", "갑주를 녹여라");
                d.portrait = "e_mosa"; d.ring = ICE;
                return d;
            default:
                d = Make("보스", "디 오리지널 - 메카 티렉스", new string[] {
                    "사냥(100~70%): 포효로 정예 증원. 폭식(70~35%): 재료 조각을 먹고 회복·공격력 상승 - 조각이 보스 곁에 떨어지면 뺏긴다.",
                    "해치 개방(35%~): 받는 피해 +30%. 무방비는 75 / 50 / 25% 에 12% 한 번 더 - [F] 독샘 요리.",
                    "마지막 주문이 온다 - 화면 안내대로 [R] / [F].",
                    "베팅을 걸었으면 여기서 정산이다." }, "[F]", "무방비 때 투척");
                d.portrait = "e_raptor"; d.portraitTint = new Color(1f, 0.5f, 0.45f); d.ring = RED;
                return d;
        }
    }

    // ─────────────────────────────────────────────
    // 승격 5 - TutorialHint 의 배너를 카드로 (id = 기존 힌트 id)
    // ─────────────────────────────────────────────
    public static BriefingUI.BriefDef Promoted(string hintId)
    {
        switch (hintId)
        {
            case "first_town":
                return Make("안킬로", "정비소 - 골드로 고치고 간다", new string[] {
                    "[G] 정비소: 수리 = 기차 HP, 연마 = 칼·팬 상태, 시장 = 재료 구매.",
                    "칼이 무디면 판정 칸이 좁아지고, 팬이 눌면 조리가 늦고 실패가 는다.",
                    "정차역마다 들러라. 창은 전투 중에도 열리지만 수리·장갑 보강은 정차 중에만 판다. 읽는 동안 시간은 멈춘다.",
                    "골드는 손님이 남긴다 - 전속 주행이면 더." }, "[G]", "정차역에서");
            case "first_augment":
                return Make("차장", "증강 - 이번 운행의 강화", new string[] {
                    "카드 셋 중 하나 - [1~5] 숫자키. 효과는 이번 운행 동안 (다음 운행엔 안 남는다).",
                    "[0] 건너뛰면 명성이 조금, [9] 리롤(선택지 새로 뽑기)은 골드가 든다.",
                    "같은 계열을 겹치면 세진다. 고른 증강은 [V] 소지품에서 다시 본다.",
                    "읽는 동안 시간은 멈춘다. 카드마다 효과를 보고 하나 골라라." }, "[1~5]", "[0] 건너뛰기 / [9] 리롤");
            case "first_route":
                return Make("차장", "분기 선로 - 위험과 보상의 교환", new string[] {
                    "다음 구간의 선로를 고른다 - [1] [2] [3]. 곧은 선로는 규칙 없음.",
                    "물량이 느는 선로는 보상이 크고, 이른 사고가 오는 선로도 있다.",
                    "폐역에는 선대의 기록(일지)이 잠들어 있다 - [J] 로 읽는다.",
                    "짝수 웨이브 앞과 보스 직전에만 고른다." }, "[1~3]", "선로 선택");
            case "first_item":
                return Make("차장", "유물 - 갑판의 전리품", new string[] {
                    "상자를 밟아 얻은 유물은 [V] 소지품 목록에 들어간다.",
                    "종류마다 하나씩, 효과는 이번 운행 동안 (다음 운행엔 안 남는다).",
                    "상자는 갑판에 떨어진다 - 걸어가서 밟아라. 남쪽 포탑 자리는 피해서 떨어진다.",
                    "폐역 선로와 처치 드랍이 상자의 출처다." }, "[V]", "소지품");
            default:
                return Make("차장", "과열 - 포탑이 달아올랐다", new string[] {
                    "포탑은 쏠수록 달아오른다. 과열되면 멈춘다 - 붉은 연기가 표시.",
                    "곁에서 [E] 꾹 + 마우스를 휘저어 부채질 - " + GameBalance.OverheatCoolHold + "초면 식는다.",
                    "손을 떼면 식힌 게 샌다. 냉각 뒤엔 잠시 재과열 면역.",
                    "저절로 안 풀린다 - 식힐 때까지 멈춘 채다. 정차하면 전부 해제." }, "[E] 꾹", "+ 마우스 부채질");
        }
    }
}
