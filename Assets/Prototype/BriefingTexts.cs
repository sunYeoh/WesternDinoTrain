using UnityEngine;

/// <summary>
/// [BriefingTexts.cs] v3.10 (v9.27 2026-10-20 튜토리얼 개편: 견습 브리핑을 16장으로 (카드 1 달려가서 넣기 / 5 Lv + 정보창·폐기 / 6 팬·솥 / 7 진화 조리·교체 / 9 과열 / 10 주방 사고 / 13 실전 + 단골 주문 / 14 정산 - 증강은 몇 웨이브마다 / 15 정차역 - 행상인·갈림길·베팅 / 16 앞길 - [V]·[J]·휠), 예습 카드에 무방비 [F] 줄, 지역 1 카드의 "웨이브를 넘기면 증강" 을 실제 규칙(2·4·8)으로, 보스 1 카드의 예습 판정 = TutorialDirector.BossPracticeDone) / v3.9 (v9.23 2026-10-14 보스 새 패턴: 보스 카드 4장 - 녹슨 발톱 부관·도약 / 천둥 둥지 알 / 동면자 입김·눈사태 / 디 오리지널 하차. 손님·보스가 하는 일엔 주어 "놈이") / v3.8 (v9.22.2 2026-10-14 유저 "튜토리얼 말투가 번역체·AI 같고 딱딱하다": 안내 글 말투를 반말 해체로 - 명령형 ~라·설명 ~다 끝맺음, 문장 속 기호, 어려운 말(투입·재가동·곁·유인)를 걷어냈다. 규칙 = 문구원칙 10-14 저녁 "말투" 절) / v3.7 (v9.22.1 2026-10-14 유저 "튜토리얼에 드래그가 반영 안 됐다": 견습 카드 2·5·6 = 끌어다 놓기(클릭 ▶ 클릭도 된다)·교체·카드 위 카드 진화 조리 / 13 = 명성 상점 [M] 한 줄) / v3.6 (v9.22 2026-10-14 조작 팩 2-1: 견습 5번 카드 "더블 육포 고르기" -> "고기 둘을 그릴에 올리고 [굽기 시작]" - 조리 창이 재료 중심으로 바뀌었다) / v3.5 (v9.21.2 2026-10-14 문구 팩: 용어 통일 - 접시->요리(개) / 데미지->피해 / 슬롯->칸 / 랜덤->무작위 / 레시피->요리 / 적·아군->손님 / 드랍->드롭 / 마법 저항->저항 / 힐러·러시·청구 / 진화 조리 판 이름) / v3.4 (v9.20 2026-10-07: 글 속 화살표를 게임 글꼴에 있는 세모로(게임 글꼴에 화살표 글리프가 없어 PC 의 다른 글꼴로 그려졌다 - 모양이 따로 놀고, 대신할 글꼴이 없는 빌드에선 빈칸)) / v3.3 (v9.18 2026-10-06: 주방 사고 첫 등장 카드의 실패 피해를 "기차 최대 HP 의 N%" 로 - 실패 피해가 최대 HP 비례가 됐다(EventFailScaleOn). 끄면 예전 고정 숫자) / v3.2 (v9.15.1 2026-09-29 스토리 개정: 지역 1 카드에 스피노의 첫 손님 농담 + 목표 = 기차 방어 / 최종전·디 오리지널 카드 = 급식 열차 1호였던 것, 원료 삼키기와 식사의 구분, 해치가 열리면 식사 / 새 손님 카드 "지닌 배급품" / 새 카드 SpinoFinal - 최종전 직전 고백과 "이번엔 안 건다") / v3.1 (v9.15 2026-09-29: 자유 연습 카드 SandboxIntro / 구간 완료 카드 "[R] 한 번 더" / 6단계 문구 - 상한 대신 접시 곡선) / v3 (v9.14 2026-09-28 테스터 "설명이 길고 지루하고 현학적": 카드 전부 2~3줄로 - 지금 할 조작 먼저, 이야기·수치 나열 제거. 새 손님 카드 3줄) / v2.5 (v9.13 2026-09-23: 분기 선로 첫 등장 카드 - 갈림길·왼쪽 카드·새 보상) / v2.4 (v9.12 2026-09-22: 견습 구간화 - 미니 보스 예습 카드·구간 완료 카드·협곡 낙뢰 사고 카드, 첫 보스 카드는 예습 여부로 분기, 굽기 카드 제목 = 그릴만, 리롤 복원(유저 09-22)) / v2.3 (v9.11.1 2026-09-22 문구 검토 반영: 첫 카드는 지금 행동만·시간 정지 명시·수리 시점·실행 가능한 조언·이번 운행/다음 운행·공명 예외·그로기=무방비·디버프 요리=독샘 요리·리롤=다시 뽑기) / v2.2 (v9.10.1 2026-09-21: 재료 이름 MaterialNames - 전기알·화염꽃·얼음꽃·독샘) / [BriefingTexts.cs] v2.1 (v9.10 2026-09-17: RecipeIntro(레시피) 소개 카드 - 웨이브 3 용암 폭탄밥 / 숙련·베팅 문구 일상어) / v2 (v9.9.2 2026-09-16: 정식 런 첫 등장 카드 34장 - 지역 4 / 새 손님 16 / 주방 사고 4 / 베팅 1 / 보스 4 / 승격 5.
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
    /// <summary>
    /// 견습 운행 브리핑 (v3.10 번호 = 카드와 다르다 - 한 카드에 둘인 것이 있다):
    ///   1 달려가서 카드 넣기(카드 1) / 2 첫 손님(카드 2) / 3 재료(카드 2, 찬 뒤) / 4 굽기(카드 3) / 5 Lv(카드 4) / 6 팬과 솥(카드 5) / 7 진화 조리와 교체(카드 6) /
    ///   8 감전(카드 7) / 9 과열(카드 7, 감전을 푼 뒤) / 10 주방 사고(카드 8) / 11 작살(카드 9 + 인라인 연습 3) / 12 레버(카드 9 + 인라인 연습 4) /
    ///   13 실전·주문(카드 10) / 14 정산(카드 11) / 15 정차역에서 만나는 것(카드 13) / 16 앞길(카드 13). 없는 번호는 null
    /// </summary>
    public static BriefingUI.BriefDef Tutorial(int step)
    {
        switch (step)
        {
            case 1:
                return Make("스피노", "여기가 네 주방이자 포대야", new string[] {
                    "[WASD]로 달리고 [Shift]로 대시해. 칸과 칸 사이는 통로 발판으로만 건널 수 있어.",
                    "오른쪽 포탑 칸까지 가 봐. 도착하면 화면 아래 더블 육포 카드를 끌어서 화살표 밑 [+] 이름표에 놔. 요리 하나에 포탑 하나야." },
                    "[WASD]", "[Shift] 대시");
            case 2:
                return Make("스피노", "손님 왔다! 포탑이 알아서 쏴", new string[] {
                    "겨누는 것도 쏘는 것도 포탑이 알아서 해. 넌 요리만 늘리면 돼.",
                    "쓰러진 손님은 재료를 떨궈. 이번엔 구경만 해. 기차는 안 다치니까." },
                    "자동", "포탑이 알아서 쏴");
            case 3:
                return Make("스피노", "재료는 화면 아래 왼쪽에 모여", new string[] {
                    "재료 두 개면 요리 하나야. 고기 두 개면 더블 육포고.",
                    "재료는 알아서 빨려 와. 갑판에 떨어진 상자만 밟아서 주우면 돼. 고기 두 개 찼네, 구우러 가자." },
                    "재료 2", "= 요리 1");
            case 4:
                return Make("스피노", "불 앞에 설 차례야. 그릴부터", new string[] {
                    "그릴 앞에서 [E]. 고기 카드를 끌거나 눌러서 그릴 두 칸에 올리고 [굽기 시작]. 눈금이 초록 구간에 들어오면 [Space].",
                    "한가운데서 누르면 PERFECT, 요리 두 개. 구간 안이면 GOOD, 하나. 벗어나면 다 타." },
                    "[E]", "그릴 앞에서");
            case 5:
                return Make("스피노", "같은 요리를 또 넣으면 레벨업", new string[] {
                    "같은 요리를 같은 포탑에 또 넣으면 레벨이 올라" + (GameBalance.T1MaxLevel > 0 ? " (Lv" + GameBalance.T1MaxLevel + "까지)" : ". 높아질수록 요리가 더 들어") + ". 빈 칸이면 새 포탑, 다른 요리가 든 칸이면 교체야.",
                    "방금 구운 육포를 화살표 포탑에 놔 봐. 포탑 이름표를 누르면 정보창이 떠. 폐기는 이름표에서 우클릭 두 번 - 넣은 요리 절반만큼 재료가 돌아와." },
                    "끌어다 놓기", "요리 카드 ▶ 포탑");
            case 6:
                return Make("스피노", "불은 셋이야. 팬과 솥도 써 봐", new string[] {
                    "조리법은 요리마다 정해져 있어. 고기는 그릴, 속성 요리는 팬, 지원 요리는 솥. 전기알 둘로 과부하 코일, 장갑 둘로 철판 정식. 재료는 줬어.",
                    "볶기는 뜨는 키를 순서대로. 끓이기는 [Space] 꾹이면 바늘이 오른쪽, 떼면 왼쪽. 구간 안에 붙잡아 둬. 팬부터." },
                    "[E]", "팬 ▶ 솥");
            case 7:
                return Make("스피노", "진화 조리 - 기본 둘이 전설 하나", new string[] {
                    "서로 다른 기본 요리 카드 둘을 합치면 전설이야. 카드를 끌면 합칠 수 있는 카드에 분홍 테가 떠. 그 위에 놓으면 진화 조리 두 판이야.",
                    "나온 전설 카드를 더블 육포 포탑에 놓으면 교체야. 든 요리는 폐기되고 재료 일부가 돌아와. 철판 정식은 빈 칸에 - 안 쏘는 대신 기차 최대 HP가 올라." },
                    "끌어다 놓기", "카드 ▶ 카드");
            case 8:
                return Make("스피노", "낙뢰! 포탑이 멈췄어", new string[] {
                    "스파크 튀는 포탑은 못 쏴. 옆으로 달려가서 [E] 한 번이면 포탑이 바로 다시 쏴.",
                    "가만두면 " + Mathf.RoundToInt(GameBalance.LightningStunSec) + "초 동안 멈춰 있어. 협곡(지역 2)에선 웨이브마다 한 번쯤 쳐. 빙결은 [E]를 " + GameBalance.UnfreezeTaps + "번 두드려야 깨져." },
                    "[E]", "멈춘 포탑 옆에서");
            case 9:
                return Make("스피노", "과열! 이번엔 열을 식혀", new string[] {
                    "포탑이 쉬지 않고 쏘면 달아올라. 붉은 연기가 나면 멈춘 거야. 옆에서 [E]를 꾹 누른 채 마우스를 흔들어서 부채질해. " + GameBalance.OverheatCoolHold + "초면 식어.",
                    "손을 떼면 열이 도로 올라. 정식 운행에선 " + Mathf.RoundToInt(GameBalance.StunBreakSec) + "초 넘게 두면 포탑이 망가져. 정차하면 전부 풀려." },
                    "[E] 꾹", "+ 마우스 부채질");
            case 10:
                return Make("스피노", "주방 사고 - 현장으로 달려가", new string[] {
                    "운행 중엔 침입, 기구 고장, 화재, 재료 흘림이 터져. 붉은 화살표가 현장을 가리켜. 닿기 전엔 손을 못 대.",
                    "현장에 닿으면 카드가 뭘 할지 알려 줘. 시간 안에 못 막으면 기차가 다쳐. 지금은 견습이라 안 다치니까 두 번 해 보자 - 기구 고장, 그다음 화재." },
                    "현장", "붉은 화살표 쪽");
            case 11:
                return Make("스피노", "창밖 바위는 재료 광맥이야", new string[] {
                    "기관실 작살포 옆에서 [E]. 지나가는 바위를 낚으면 그 색 재료가 " + GameBalance.HarpoonMatMin + "~" + GameBalance.HarpoonMatMax + "개.",
                    "재장전은 " + Mathf.RoundToInt(GameBalance.HarpoonCooldown) + "초. 가끔 손님이 딸려 와. 바위 온다, 기관실로 가." },
                    "[E]", "기관실 작살포");
            case 12:
                return Make("스피노", "레버를 당기면 더 벌고 더 위험해", new string[] {
                    "기관실 레버 옆에서 [E]를 " + GameBalance.LeverHoldSec + "초 꾹 누르면 전속 주행. 골드는 " + GameBalance.LeverGoldMul + "배로 늘지만, 손님이 더 자주 오고 조리 판정도 빡빡해져.",
                    "한번 당겨 봐. 다시 당기면 원래대로 돌아와." },
                    "[E] 꾹", "기관실 레버");
            case 13:
                return Make("스피노", "손님이 몰려와. 배운 대로 막아", new string[] {
                    "이번엔 진짜야. 기차도 다쳐. 요리를 늘리고, 멈춘 포탑은 달려가서 살려. 기차가 멈추면 이 카드만 다시 하면 돼.",
                    "왼쪽에 단골 주문이 와. 적힌 계열 요리를 그 수만큼 만들면 그 계열 포탑 피해 +" + Mathf.RoundToInt(GameBalance.OrderBuffPct * 100f) + "%에 골드 " + GameBalance.OrderGold + ". 못 채워도 벌칙은 없어.",
                    "처음 보는 손님이 하나 섞여 있는데, 다음 카드에서 알려 줄게." },
                    "방어", "전부 쓰러뜨려");
            case 14:
                return Make("스피노", "웨이브가 끝나면 증강과 정비", new string[] {
                    "증강은 웨이브 2·4·8에, 그 뒤론 두 웨이브마다 골라. 카드 중 하나를 숫자키로. [0]은 건너뛰기, [9]는 골드 내고 리롤.",
                    "정차역에선 [G]로 정비소를 열어. 기차 수리, 칼·팬 연마, 재료 시장까지 있어." },
                    "[1~5]", "[G] 정비소");
            case 15:
                return Make("스피노", "정차역에서 만나는 것들", new string[] {
                    "행상인 안킬로가 가끔 와. 매대 둘, [1] [2]로 사고 [0]으로 안 사. 두상 앞 선로가 갈라지면 왼쪽 카드에서 [1~3]으로 길을 고르고 [Enter]로 출발. 위험할수록 보상이 커.",
                    "보스 앞 정차역엔 내가 와. 베팅이야. [1] 일반은 져도 잃는 게 없고, [2] 도박은 판돈을 걸어. [0]은 거절. 조건은 그때 카드에 적혀 있어." },
                    "[1] / [2]", "사기 / 베팅");
            case 16:
                return Make("스피노", "앞으로 갈 길. 지역 셋과 최종전", new string[] {
                    "새 손님, 사고, 보스는 처음 만나면 카드로 알려 줄게. [H]로 다시 봐. 갑판 상자를 밟으면 유물이고, 증강과 유물은 [V]에서 봐.",
                    "명성은 죽어도 안 사라져. 로비 [M]은 명성 상점, [J]는 선대의 일지. 마우스 휠로 화면을 당겨 봐. 견습은 여기까지. 출발해, 신입." },
                    "[출발]", "로비에서 [Enter]");
        }
        return null;
    }

    /// <summary>카드 10 앞에 붙는 새 손님 카드 - 독침 프테라 (정식 런 첫 등장 카드와 같은 생성기, 기록은 남기지 않는다)</summary>
    public static BriefingUI.BriefDef TutorialPtera()
    {
        return Enemy(global::Enemy.PoisonPtera);
    }

    /// <summary>견습 운행 완료 요약 카드</summary>
    public static BriefingUI.BriefDef TutorialDone(bool firstTime, int reward, float seconds, int skips, int cookFails)
    {
        int m = Mathf.FloorToInt(seconds / 60f), s = Mathf.FloorToInt(seconds % 60f);
        string stat = "걸린 시간 " + m + "분 " + s + "초  /  건너뛴 카드 " + skips + "  /  조리 실패 " + cookFails;
        BriefingUI.BriefDef d = firstTime && reward > 0
            ? Make("견습 운행 완료", "명성 +" + reward, new string[] {
                stat,
                "로비에서 [출발]을 누르면 진짜 운행이야. 다시 연습하려면 [T] 훈련장 (보상은 처음 한 번만)." }, "완료", "[Enter] 로비로")
            : Make("견습 운행 완료", "복습 끝", new string[] {
                stat,
                "로비에서 [출발]을 누르면 돼." }, "완료", "[Enter] 로비로");
        return d;
    }

    /// <summary>v9.12 구간 8 시작 브리핑 - 왜 미끼인가 (목업 v4.2 (D)). v3.10: 무방비 [F] 줄</summary>
    public static BriefingUI.BriefDef TutorialBossPractice()
    {
        return Make("스피노", "첫 보스 예습. 미끼로 무리 꾀어내기", new string[] {
            "왕은 정면으로는 못 이겨. 무리가 미끼를 물면 왕도 따라오는데, 그동안 포탑이 때려. 왼쪽 아래 미끼 화덕에서 고기를 구워.",
            "왕이 무방비(HP 바 밑 노란 띠)가 되면 [F]로 독샘 요리를 던져. 독침 육포를 하나 실어 뒀어. 새끼로 먼저 연습하자." },
            "[미끼 굽기]", "왼쪽 아래 화덕");
    }

    /// <summary>v9.12 구간 8 실패(기차 HP 50% 아래) 뒤 다시 - 한 번만</summary>
    public static BriefingUI.BriefDef TutorialBossRetry()
    {
        BriefingUI.BriefDef d = Make("스피노", "기차가 너무 물렸어. 다시 하자", new string[] {
            "미끼부터 던져. 무리가 미끼 쪽에 가 있는 동안 포탑이 때려.",
            "기차는 고쳐 놨고, 고기도 채워 놨어." }, "다시", "미끼부터");
        d.ring = RED;
        return d;
    }

    /// <summary>v9.12 한 구간만 연습했을 때의 완료 카드 (훈련장에서 들어온 런). 전부 돌린 런은 TutorialDone</summary>
    public static BriefingUI.BriefDef TutorialSegmentDone(string segTitle, float seconds, bool skipped)
    {
        int m = Mathf.FloorToInt(seconds / 60f), sec = Mathf.FloorToInt(seconds % 60f);
        return Make("훈련장", skipped ? "구간 끝. 건너뛴 카드가 있어" : "구간 완료 - " + segTitle, new string[] {
            "걸린 시간 " + m + "분 " + sec + "초" + (skipped ? "  /  건너뛴 카드가 있어서 완료 기록은 안 남아" : ""),
            GameBalance.TrainingRepeatKey ? "[R]이면 같은 구간 한 번 더, [Enter]면 로비로. 다른 구간은 [T] 훈련장에서." : "이제 로비로 돌아가. 다른 구간은 [T] 훈련장에서 골라." },
            GameBalance.TrainingRepeatKey ? "[R]" : "완료", GameBalance.TrainingRepeatKey ? "한 번 더" : "[Enter] 로비로");
    }

    /// <summary>v3.1: 자유 연습 시작 카드 (훈련장 8)</summary>
    public static BriefingUI.BriefDef SandboxIntro()
    {
        return Make("훈련장", "자유 연습. 마음대로 해 봐", new string[] {
            "손님은 계속 와. 재료는 안 떨어지고, 기차는 멈춰도 바로 고쳐져.",
            "[1] 낙뢰  [2] 화재  [3] 재료 흘림  [4] 기구 고장  [5] 침입  [6] 과열. 사고를 직접 일으켜서 막아 봐.",
            "과열된 포탑을 " + Mathf.RoundToInt(GameBalance.StunBreakSec) + "초 넘게 두면 진짜로 망가져. 나갈 땐 [ESC] 메뉴에서 그만두기." },
            "[1~6]", "사고 일으키기");
    }

    /// <summary>카드 실패/재시작 안내 (카드 10 기차 정지)</summary>
    public static BriefingUI.BriefDef TutorialRetry()
    {
        BriefingUI.BriefDef d = Make("스피노", "기차가 멈췄어. 다시 하자", new string[] {
            "요리를 더 넣어. 요리 수가 곧 화력이야. 멈춘 포탑은 먼저 살리고.",
            "기차는 고쳐 놨어." }, "재시작", "이 단계만 다시");
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
                    "\"예약도 없이 몰려오네. 주방부터 지켜, 신입.\" - 스피노. 할 일은 하나야, 기차를 지켜.",
                    "랩터 무리부터 와. 요리가 곧 포탑이고, 포탑 수가 곧 화력이야.",
                    "증강은 웨이브 2·4·8에 골라. 정차역에선 [G] 정비소에 들렀다가 다음 웨이브로. 웨이브 " + len + "에 보스가 나와." }, "", "");
                d.portrait = EnemySkin.PortraitFor(global::Enemy.SteamRaptor.enemyName, out tint); d.portraitTint = tint; d.ring = BRASS;
                return d;
            case 2:
                d = Make("지역 2", "테슬라 협곡 - 웨이브 " + (len + 1) + " ~ " + (len * 2), new string[] {
                    "하늘에서 오는 손님이 많아. 날개 달린 놈들한텐 육포·스테이크 같은 물리 요리가 잘 박혀.",
                    "낙뢰가 포탑을 감전시켜. 스파크가 보이면 달려가서 [E] 한 번." }, "", "");
                d.portrait = EnemySkin.PortraitFor(global::Enemy.BoltTeranodon.enemyName, out tint); d.portraitTint = tint; d.ring = ELEC;
                return d;
            case 3:
                d = Make("지역 3", "코발트 광산 - 웨이브 " + (len * 2 + 1) + " ~ " + (len * 3), new string[] {
                    "두껍고 느린 손님들이야. 전설 요리나, 같은 속성 포탑 " + GameBalance.ResonanceCount + "개를 모은 공명으로 밀어붙여.",
                    "기차 바퀴를 멈추는 손님이 있어. 붙기 전에 때려. 보스는 화염으로만 녹는 갑주를 둘러." }, "", "");
                d.portrait = EnemySkin.PortraitFor(global::Enemy.IceMosa.enemyName, out tint); d.portraitTint = tint; d.ring = ICE;
                return d;
            default:
                d = Make("최종전", "황야의 끝 - 웨이브 " + GameBalance.FinalWave, new string[] {
                    "막힌 종착역. 길을 막고 선 건 옛 급식 열차 1호였던 무언가야. 지금은 '디 오리지널'이라고 불러.",
                    "원료 조각을 삼키면 회복하니까 조각을 놈 옆에 두지 마. HP 75 / 50 / 25 / 12%에서 무방비가 되면 [F]로 독샘 요리.",
                    "이 판을 넘기면 철길이 열려. 해치가 열리면 다른 길도 있어." }, "", "");
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
        if (def >= res + 10f) advice = "전기알·화염꽃·얼음꽃·독샘 같은 속성 요리가 잘 박혀";
        else if (res >= def + 10f) advice = "육포·스테이크·등심 같은 물리 요리가 잘 박혀";
        else if (def >= 30f) advice = "방어도 저항도 두꺼워. 레벨이랑 공명으로 밀어붙여";
        else advice = "아무 요리나 잘 박혀. 화력 싸움이야";

        string tip;
        if (name.Contains("플라이")) tip = "포탑 옆에서 터져(공격 " + Mathf.RoundToInt(data.baseATK) + "). 닿기 전에 떨어뜨려.";
        else if (name.Contains("프테라")) tip = "먼저 떨어뜨리고 나서 조리대에 서.";
        else if (name.Contains("파라사우") || name.Contains("네크로")) tip = "옆에 있는 손님을 세게 만들어. 폭발·관통처럼 무리째 맞는 요리로 같이 때려.";
        else if (name.Contains("모사") || name.Contains("맘모스")) tip = "기차를 세우는 놈이야. 붙기 전에 사거리 긴 포탑으로 때려.";
        else if (name.Contains("강철")) tip = "물리 요리는 반만 박혀(방어 50). 전기알·화염꽃·얼음꽃·독샘 같은 속성 요리 포탑을 세워.";
        else if (name.Contains("카르노")) tip = "화상은 계속 들어오는 피해야. 정차하면 [G] 정비소에서 기차를 고쳐.";
        else if (name.Contains("전갈")) tip = "독이 칼·팬을 갉아먹어. 정차역에서 연마해.";
        else if (name.Contains("캑터스")) tip = "멀리서 던지는 놈이야. 사거리 긴 포탑이라야 먼저 닿아.";
        else if (name.Contains("파키")) tip = "맞으면 피해 일부를 되돌려 보내는 놈이야. 기차 HP가 낮을 땐 조심해.";
        else if (name.Contains("거북") || name.Contains("아르마딜로")) tip = "느리지만 두꺼워. 엔진 쪽을 노리는 놈이야.";
        else if (name.Contains("테라노돈") || name.Contains("익룡")) tip = "포탑이랑 주방으로 내리꽂혀. 하늘도 사거리 안이야.";
        else tip = "떼로 와. 요리 수가 곧 답이야.";

        BriefingUI.BriefDef d = Make("새 손님", name + " - " + ShortAbility(data.specialAbility), new string[] {
            data.specialAbility + ". 노리는 곳은 " + data.targetPriority + ".",
            advice + " (방어 " + Mathf.RoundToInt(def) + " / 저항 " + Mathf.RoundToInt(res) + "). 지닌 배급품은 " + MatName(mat) + ", 쓰러지면 떨궈.",
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
        BriefingUI.BriefDef d = Make("스피노", "새 요리 - " + r.displayName + " (" + RecipeText.RoleWord(r) + ")", new string[] {
            src + " ▶ " + RecipeText.MethodWord(r) + ". 재료는 넣어 놨어. " + RecipeText.What(r),
            "이럴 때 좋아: " + RecipeText.When(r) + ". 만들어서 빈 칸에 넣어 봐." }, "[E]", RecipeText.MethodWord(r));
        return d;
    }

    private static string MatNameKey(string key) { return MaterialNames.Kor(key); }   // v2.2: 재료 이름 한 곳

    // ─────────────────────────────────────────────
    // 정식 런 첫 등장 - 주방 사고 4 (첫 발생 순간, KitchenEvents 의 제목·조작·제한 시간·실패 피해)
    // ─────────────────────────────────────────────
    /// <summary>v9.18: 사고 실패 피해 문구. 표의 값(기차 HP 500 기준)을 지금 규칙대로 적는다 - 최대 HP 비례면 "최대 HP 의 12%", 아니면 "60"</summary>
    private static string EventLoss(float baseAmount)
    {
        if (!GameBalance.EventFailScaleOn) return Mathf.RoundToInt(baseAmount).ToString();
        return "최대 HP 의 " + Mathf.RoundToInt(baseAmount / Mathf.Max(1f, GameBalance.EventFailRefHP) * 100f) + "%";
    }

    public static BriefingUI.BriefDef Event(string key)
    {
        BriefingUI.BriefDef d;
        switch (key)
        {
            case "intrusion":
                d = Make("주방 사고", "침입자 - 랩터가 기차에 뛰어들었다", new string[] {
                    "빨간 화살표 쪽으로 달려가서 [E]를 연타해. 게이지를 다 채우면 쫓아낼 수 있어. 6.5초 안에.",
                    "못 쫓아내면 기차가 " + EventLoss(60f) + " 피해를 입어. 조리는 그다음이야." }, "[E] 연타", "현장에서");
                d.portrait = "e_raptor"; d.ring = RED;
                return d;
            case "break":
                d = Make("주방 사고", "기구 고장 - 배선에서 불꽃이 튄다", new string[] {
                    "빨간 화살표 쪽으로 달려가서 화면에 뜬 방향키를 순서대로 눌러. 틀리면 처음부터. 7초 안에.",
                    "못 고치면 기차가 " + EventLoss(40f) + " 피해를 입어." }, "방향키", "순서대로");
                d.portrait = "ui_ev_spark_1"; d.ring = ELEC;
                return d;
            case "fire":
                d = Make("주방 사고", "화재 - 기차가 계속 타들어간다", new string[] {
                    "빨간 화살표 쪽으로 달려가서 [E]를 꾹 눌러. 게이지가 100%가 될 때까지. 8초 안에.",
                    "타는 동안 기차가 계속 다쳐. 못 끄면 " + EventLoss(50f) + " 피해를 더 입어." }, "[E] 꾹", "불길 앞에서");
                d.portrait = "ui_ev_fire_1"; d.ring = FIRE;
                return d;
            case "lightning":
                d = Make("사고", "낙뢰 - 포탑이 감전됐다", new string[] {
                    "스파크 튀는 포탑은 못 쏴. 옆으로 달려가서 [E] 한 번이면 포탑이 바로 다시 쏴.",
                    "가만두면 " + Mathf.RoundToInt(GameBalance.LightningStunSec) + "초 동안 멈춰 있어. 이 협곡에선 웨이브마다 한 번쯤 쳐." }, "[E]", "멈춘 포탑 옆에서");
                d.portrait = "ui_ev_spark_1"; d.ring = ELEC;
                return d;
            default:
                d = Make("주방 사고", "흔들림 - 재료가 바닥에 쏟아졌다", new string[] {
                    "화면에 떨어진 재료를 마우스 좌클릭으로 전부 주워. 7.5초 안에.",
                    "못 주운 재료 하나마다 기차가 " + EventLoss(10f) + " 피해를 입어." }, "좌클릭", "떨어진 재료");
                d.portrait = "ui_mat_meat"; d.ring = MEAT;
                return d;
        }
    }

    // ─────────────────────────────────────────────
    // v3.2: 최종전 시작 - 스피노의 고백 (베팅 대신). 검토 보고서 5절 "최종전 직전": 전직 기관사였음을 인정하고 베팅을 거둔다.
    //   "필요한 재료는 실어 뒀다" = 보스 웨이브 긴급 보급(독샘 + 고기)을 스피노가 보낸 것으로 (WaveManager 가 최종전에서 문구를 바꾼다)
    // ─────────────────────────────────────────────
    public static BriefingUI.BriefDef SpinoFinal()
    {
        BriefingUI.BriefDef d = Make("스피노", "이번엔 판돈 안 걸어", new string[] {
            "\"저건 내가 몰던 열차야. 급식 열차 1호. 멈추던 날, 난 뛰어내렸지.\"",
            "\"이번엔 판돈 안 걸어. 필요한 재료는 실어 뒀어. 고기랑 독샘. 나도 여기 남을 거야.\"",
            "\"해치가 열리면 심장이 보여. 그때 식사를 건넬 수 있어. ...아니면 부숴. 네 주방이니까.\"" },
            "[F] / [R]", "무방비 때 격파 / 마지막 식사");
        d.portrait = SPINO; d.ring = new Color(0.8f, 0.6f, 1f, 1f);
        return d;
    }

    // ─────────────────────────────────────────────
    // 정식 런 첫 등장 - 도박꾼 베팅 (보스 직전 정차, SpinoBetUI 가 열리기 직전)
    // ─────────────────────────────────────────────
    public static BriefingUI.BriefDef BetFirst()
    {
        return Make("스피노", "베팅이야. 보스전에 뭘 걸래?", new string[] {
            "[1] 일반은 조건을 채우면 상을 주고, 못 채워도 잃는 건 없어. [2] 도박은 판돈을 걸어. 따면 크고, 지면 아파.",
            "[0]은 거절. 조건은 카드에 적혀 있어." }, "[1] / [2]", "[0] 거절");
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
                    string bait = TutorialDirector.BossPracticeDone
                        ? "미끼는 예습 때 한 그대로야. 왼쪽 아래 [미끼 굽기]."
                        : "미끼 화덕에서 고기를 구워 던지면 무리랑 왕이 그쪽으로 가.";
                    d = Make("보스", "녹슨 발톱 - 무리의 왕", new string[] {
                        "부관 둘을 데리고 오는 놈이야. 놈이 무리를 부를 때 마비시키면 랩터가 절반만 와.",
                        "바닥에 숫자 원이 뜨면 놈이 그 순서로 덮쳐. " + bait,
                        "HP 75 / 50 / 25%에서 무방비. [F]로 독샘 요리를 던져." }, "[F]", "무방비 때 던지기");
                }
                d.portrait = "e_raptor"; d.portraitTint = new Color(0.9f, 0.55f, 0.38f); d.ring = RED;
                return d;
            case 2:
                d = Make("보스", "천둥 둥지 - 프테라 여왕", new string[] {
                    "하늘에서 낙뢰로 포탑을 감전시키는 놈이야. 낙뢰 게이지 끝에서 [Space]면 병에 담겨. 병이 셋이면 되쏘기로 무방비.",
                    "놈이 알을 떨구면 부화하기 전에 깨. 늦으면 새끼 테라노돈이 나와. 감전된 포탑은 달려가서 [E].",
                    "HP 75 / 50 / 25%에서도 무방비. [F]로 독샘 요리를 던져." }, "[Space]", "낙뢰 끝자락 패링");
                d.portrait = "e_ptera"; d.portraitTint = new Color(0.72f, 0.72f, 1f); d.ring = ELEC;
                return d;
            case 3:
                d = Make("보스", "동면자 - 고대 모사", new string[] {
                    "놈이 빙하 갑주를 두르면 받는 피해가 " + Mathf.RoundToInt(GameBalance.GlacierArmorDR * 100f) + "% 줄어. 화상을 쌓거나 해동포에 화염꽃을 넣고 쏴서 녹여.",
                    "놈이 서리를 뿜으면 그 칸 포탑이 느려져. 굽기 요리 하나면 녹아.",
                    "HP " + Mathf.RoundToInt(GameBalance.HibAvalancheHPRatio * 100f) + "% 밑에선 눈사태가 내려와. [F] 독샘 요리나 해동포로 멈춰.",
                    "갑주가 깨지거나 HP 75 / 50 / 25%면 무방비." }, "화염꽃", "갑주를 녹여");
                d.portrait = "e_mosa"; d.ring = ICE;
                return d;
            default:
                d = Make("보스", "디 오리지널 - 급식 열차 1호였던 것", new string[] {
                    "닥치는 대로 삼키긴 해도, 제대로 된 식사는 백 년째 못 했지.",
                    "화물칸에서 손님이 뛰어내려. " + Mathf.RoundToInt(GameBalance.FeedPhaseStartRatio * 100f) + "% 밑에선 놈이 옆에 떨어진 원료 조각을 삼켜 회복해.",
                    Mathf.RoundToInt(GameBalance.HatchPhaseStartRatio * 100f) + "% 밑에선 해치가 열려 받는 피해 +" + Mathf.RoundToInt((GameBalance.HatchDamageTakenMul - 1f) * 100f) + "%. 무방비 때 [F]로 독샘 요리.",
                    "해치가 열리면 [R]로 마지막 식사. 일지 12장과 전설 요리가 필요해." }, "[F]", "무방비 때 던지기");
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
                return Make("안킬로", "정비소 - 골드로 고치고 가시오", new string[] {
                    "[G] 정비소에선 기차 수리, 칼·팬 연마, 재료 장사까지 다 하오. 칼·팬이 무디면 조리 판정이 빡빡해진다오.",
                    "정차역마다 들르시오. 수리와 장갑 보강은 정차 중에만 파오. 값은 깊이 들어갈수록 오른다오." }, "[G]", "정차역에서");
            case "first_augment":
                return Make("차장", "증강 고르기", new string[] {
                    "카드 하나를 숫자키로 골라. 효과는 이번 운행 동안만이야. [0]은 건너뛰기(명성 조금), [9]는 리롤(골드).",
                    "고른 증강은 [V] 소지품에서 다시 볼 수 있어." }, "[1~5]", "[0] 건너뛰기 / [9] 리롤");
            case "first_route":
                return Make("차장", "갈림길 - 위험할수록 보상도 커", new string[] {
                    "두상 앞에서 선로가 갈라져. 왼쪽 카드에서 [1~3]을 누르거나 클릭해서 길을 골라. 위험할수록 보상이 커.",
                    "고른 다음 [Enter]로 출발." }, "[1~3]", "길 고르기");
            case "first_item":
                return Make("차장", "유물 - 갑판의 전리품", new string[] {
                    "노란 화살표 밑 상자를 밟으면 유물이 나와. 효과는 이번 운행 동안이고, [V] 소지품에서 볼 수 있어." }, "[V]", "소지품");
            default:
                return Make("차장", "과열 - 포탑이 달아올랐다", new string[] {
                    "빨간 연기가 나는 포탑은 멈춰. 옆에서 [E]를 꾹 누른 채 마우스를 흔들어서 부채질해. " + GameBalance.OverheatCoolHold + "초면 식어.",
                    "손을 떼면 열이 도로 올라. 정차하면 전부 풀려." }, "[E] 꾹", "+ 마우스 부채질");
        }
    }
}
