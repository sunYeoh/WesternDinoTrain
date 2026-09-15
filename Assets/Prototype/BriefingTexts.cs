using UnityEngine;

/// <summary>
/// [BriefingTexts.cs] v1 (신규, v9.9 2026-09-16) - 브리핑 카드 문구 표
///
/// 견습 운행(TutorialDirector) 12단계의 시작 브리핑 + 완료 요약. 정식 런 "첫 등장" 카드(지역/새 손님/이벤트/베팅/보스)는
/// 2차 팩(v9.9.2)에서 이 파일에 추가한다 - 문구는 전부 코드로 확인한 값만 쓴다
/// (예: 독침 프테라 = 조리 속도 -50% 10초 / 감전 = [E] 한 번, 빙결 = [E] 연타, 과열 = [E] 꾹 + 마우스).
/// 톤: 스피노(스토리바이블 "카론+멘토") - 명령형, 브래킷, 적은 "손님/굶주린 것들", 위험은 "베팅".
/// 한 줄은 34자 안쪽 (본문 폭 560px / 17pt). 넘치면 카드가 접는다.
///
/// 사용법: BriefingTexts.Tutorial(step) -> BriefingUI.BriefDef (onClose 는 호출부가 채운다)
/// VS 2017 (C# 7.3) 호환
/// </summary>
public static class BriefingTexts
{
    private static readonly Color BRASS = new Color(0.84f, 0.667f, 0.282f, 1f);
    private static readonly Color POISON = new Color(0.678f, 0.451f, 0.910f, 1f);
    private static readonly Color RED = new Color(0.84f, 0.20f, 0.22f, 1f);

    private static BriefingUI.BriefDef Make(string speaker, string title, string[] lines, string keyGlyph, string keyLabel)
    {
        BriefingUI.BriefDef d = new BriefingUI.BriefDef();
        d.speaker = speaker; d.title = title; d.lines = lines;
        d.keyGlyph = keyGlyph; d.keyLabel = keyLabel; d.ring = BRASS;
        return d;
    }

    /// <summary>견습 운행 단계 시작 브리핑 (1~12). 없는 단계는 null</summary>
    public static BriefingUI.BriefDef Tutorial(int step)
    {
        switch (step)
        {
            case 1:
                return Make("스피노", "여기가 네 주방이자 포대다", new string[] {
                    "기차는 네 칸 - 기관실, 주방, 포탑 칸 둘. 셰프는 너 하나다.",
                    "칸 사이는 통로 발판으로만 건넌다. 난간은 못 넘는다.",
                    "[WASD] 달리고 [Shift] 대시. 발이 느리면 접시가 식는다.",
                    "먼저 주방 칸으로 - 화살표가 가리키는 자리까지 달려라." },
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
                    "왼쪽 재료 칸: 고기·등심·전기·화염·얼음·독 여섯 가지.",
                    "재료 두 개 = 요리 한 접시. 고기 둘이면 더블 육포다.",
                    "재료는 알아서 빨려 온다. 갑판에 떨어진 상자(유물)만 밟아서 줍는다.",
                    "고기 2개가 찼다. 이제 굽는다." },
                    "재료 2", "= 요리 1");
            case 5:
                return Make("스피노", "불 앞에 서라 - 조리대 세 가지", new string[] {
                    "조리대는 셋. 그릴 = 굽기(리듬), 팬 = 볶기(커맨드), 솥 = 끓이기(압력).",
                    "요리마다 조리대가 정해져 있다. 판정은 Bad / Good / Perfect.",
                    "그릴 곁에서 [E] → 더블 육포 선택 → 판정 칸 안에서 [Space].",
                    "[ESC]로 중단하면 재료는 돌려받는다. 태우면 없다." },
                    "[E]", "그릴 곁에서");
            case 6:
                return Make("스피노", "같은 접시를 다시 넣으면 레벨업", new string[] {
                    "가동 중인 포탑에 같은 요리를 또 넣으면 레벨이 오른다.",
                    "다른 요리를 빈 칸에 넣으면 새 포탑. 종류가 곧 전략이다.",
                    "같은 포탑 두 문은 좌클릭으로 합체 - 지금은 알아만 둬라.",
                    "방금 구운 육포를 첫 포탑에 넣어라." },
                    "클릭", "요리 카드 → 포탑");
            case 7:
                return Make("스피노", "낙뢰다 - 포탑이 멈췄다", new string[] {
                    "감전된 포탑은 다시 만질 때까지 한 발도 못 쏜다.",
                    "곁으로 달려가 [E] 한 번 - 털어내면 바로 재가동.",
                    "빙결은 [E] 연타로 깨고, 과열은 [E] 꾹 + 마우스로 부채질.",
                    "네가 주방에만 있을 수 없는 이유다. 손님은 기다려 주지 않는다." },
                    "[E]", "멈춘 포탑 곁에서");
            case 8:
                return Make("스피노", "창밖 바위는 재료 광맥이다", new string[] {
                    "기관실 작살포 곁에서 [E] - 지나가는 바위를 낚아라.",
                    "낚으면 재료가 들어온다. 대신 굶주린 것들이 딸려올 때가 있다.",
                    "손님이 없을 때 노려라. 조리 중엔 손이 모자란다.",
                    "바위가 온다. 기관실로 달려가라." },
                    "[E]", "기관실 작살포");
            case 9:
                return Make("스피노", "레버 - 전속 주행은 베팅이다", new string[] {
                    "기관실 레버 곁에서 [E] 를 잠깐 꾹 - 전속 주행.",
                    "빨리 달리면 처치 골드가 늘고, 대신 손님이 더 자주 온다.",
                    "벌이가 좋은 만큼 위험하다. 걸지 말지는 네 몫.",
                    "한 번 당겨 봐라. 다시 당기면 원래대로." },
                    "[E] 꾹", "기관실 레버");
            case 10:
                return Make("스피노", "손님이 몰려온다 - 배운 대로 막아라", new string[] {
                    "이번엔 진짜다. 기차도 다친다.",
                    "접시를 늘리고, 멈춘 포탑은 달려가 털어라.",
                    "같은 속성 포탑 3문이면 공명 - 그 속성 피해 +20%.",
                    "기차가 멈추면 이 단계만 다시 한다." },
                    "방어", "전부 쓰러뜨려라");
            case 11:
                return Make("스피노", "정산 - 증강과 정비소", new string[] {
                    "웨이브를 넘기면 정산이다. 증강 카드 셋 중 하나 - [1~5] 숫자키.",
                    "[0] 건너뛰면 명성이 조금, [9] 리롤은 골드가 든다.",
                    "정차역에선 [G] 정비소 - 수리·칼 연마·재료 시장.",
                    "칼과 팬은 쓸수록 닳는다. 하단 바 오른쪽 명판이 상태다." },
                    "[1~5]", "[G] 정비소");
            case 12:
                return Make("스피노", "도박꾼 스피노 - 나 말이다", new string[] {
                    "보스 앞 정차역엔 내가 온다. 베팅 카드 여섯.",
                    "일반 셋은 져도 잃는 게 없고, 도박 셋은 대가가 있다.",
                    "안 걸어도 된다. 하지만 크게 따는 건 판돈이 클 때뿐이지.",
                    "보스가 가슴 해치를 열면 [F] - 디버프 요리를 던져라." },
                    "[F]", "그로기 때 투척");
            case 13:
                return Make("스피노", "앞길 - 지역 셋과 최종전", new string[] {
                    "구리 사막, 테슬라 협곡, 코발트 광산. 그리고 최종전.",
                    "새 손님이 오면 그때마다 카드로 알려주지. [H]로 다시 읽는다.",
                    "[M] 명성 상점은 죽어도 남는 성장. [J] 선대의 일지는 이야기다.",
                    "견습은 끝이다. 출발해라, 신입." },
                    "[출발]", "로비에서 [Enter]");
        }
        return null;
    }

    /// <summary>단계 10 앞에 붙는 새 손님 카드 (정식 런 첫 등장 카드와 같은 형식) - 독침 프테라</summary>
    public static BriefingUI.BriefDef TutorialPtera()
    {
        BriefingUI.BriefDef d = Make("새 손님", "독침 프테라 - 조리를 방해한다", new string[] {
            "위에서 독침을 쏜다. 맞으면 10초 동안 조리 속도가 절반이다.",
            "방어 0 / 저항 30 - 물리 요리(육포·스테이크)가 잘 박힌다.",
            "먼저 떨어뜨리고 나서 조리대에 서라.",
            "떨어뜨리면 [프테라 독샘] 을 흘린다 - 독 요리 재료." }, "", "");
        d.portrait = "e_ptera"; d.ring = POISON;
        return d;
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
}
