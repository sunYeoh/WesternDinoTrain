using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [StoryTexts.cs] v1.2
/// 스토리 텍스트 - 오프닝 / 귀환 인사 / 사망 / 부활 / 승리 / 도감 플레이버 / 선대의 일지.
/// 스토리바이블(2026-08-18) Phase 1 물량.
///
/// - v1.2 변경점 (정합성 검수):
///   1) "돌아왔군" 대사가 죽는 순간에 나오던 모순 수정
///      -> 사망 순간 = 기관심장이 셰프를 다시 굽는 문구 / 다음 런 시작 = 스피노 귀환 인사
///   2) 아홉 목숨 부활 대사를 사망 문구와 분리 (전용 대사)
///   3) 일지 10번 문장을 일지 12번(연료칸)과 시간순이 맞게 수정
///
/// 사용법: 파일만 Assets/Prototype에 넣으면 끝.
/// 씬 배치/AddComponent 불필요 - GameManager가 static 함수를 직접 호출한다.
/// UI는 호출 순간 코드로 생성되고 연출이 끝나면 스스로 삭제된다.
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public static class StoryTexts
{
    /// <summary>
    /// 화면 전체를 덮는 연출(오프닝)이 재생 중인가?
    /// WaveManager가 이 값을 보고 적 스폰을 연출이 끝날 때까지 미룬다.
    /// (사망/승리/도감 문구 같은 작은 연출은 게임을 막지 않음)
    /// </summary>
    public static bool IsBlocking { get; private set; }

    // ==================================================================
    //  텍스트 풀
    // ==================================================================

    // 오프닝 3줄 (1회차 시작)
    private static readonly string[] OpeningLines =
    {
        "황야에는 두 종류의 기계가 있다. 굶주린 것과, 아직 저녁을 얻는 것.",
        "너의 기차는 후자다. 네가 요리하는 한.",
        "종착역: 황야의 끝, 디 오리지널. - 출발한다.",
    };

    // 사망 순간 - 기관심장이 죽은 셰프를 "다시 굽기" 시작한다 (부활의 세계관 납득)
    private static readonly string DeathLine =
        "심장이 레시피를 뒤적인다... 너를 다시 굽는다.";

    // 2회차 첫 시작 전용 - 스피노의 귀환 인사 ("돌아왔군"은 돌아온 시점에 해야 맞다)
    private static readonly string FirstReturnLine =
        "돌아왔군. 그 심장은 요리사를 굶기느니 죽음을 다시 굽는 물건이지. ...부럽다고는 안 했다.";

    // 3회차 이후 시작 인사 풀 (랜덤 로테이션)
    private static readonly string[] ReturnLines =
    {
        "황야는 아침마다 다시 배고파진다. 너도 다시 온 거고.",
        "지난 저녁은 황야가 이겼지. 판돈은 이번 판으로 이월이다.",
        "죽음은 레시피의 일부다. 최초의 셰프도 그렇게 말했지. ...아마도.",
        "몇 번째냐고? 세는 건 관뒀다. 세어서 뭐 하게.",
    };

    // 부활 (아홉 개의 목숨 발동) - 사망 문구와 구분되는 전용 대사
    private static readonly string ReviveLine =
        "심장이 고집을 부린다. 아직 저녁 전이라고.";

    // 승리 (스피노의 이례적 침묵)
    private static readonly string VictoryLine =
        "...오늘은 내가 졌다. 저녁값은 내가 내지.";

    // 선대의 일지 12장 (분기 선로 '폐역'에서 수집 - 최초의 셰프의 기록)
    // 1,4,9,12는 스토리바이블 확정본
    private static readonly string[] JournalTexts =
    {
        "굶주림은 병이 아니다. 굶주림은 시간이다. 나는 시간을 요리하기로 했다.",
        "오늘 첫 손님이 왔다. 강철 이빨에 성질머리는 최악. 그래도 그릇은 비웠다. 요리사에게는 그거면 된다.",
        "협곡의 번개를 병에 담았다. 세 병이 터졌고 한 병이 남았다. 요리는 원래 그런 것이다.",
        "태엽을 단 안킬로가 처음 한 일은 제 배급을 새끼에게 넘긴 것이었다. 기계가 되어도 부모는 부모다.",
        "대붕괴 이후 다들 총부터 들었다. 나는 국자를 들었다. 아직 어느 쪽이 옳았는지 모르겠다.",
        "광산의 일꾼들이 떠나며 절임통을 두고 갔다. 코발트 냉기에 삭힌 맛. 슬픔도 오래 절이면 조미료가 된다.",
        "기차가 오늘 처음으로 기적을 울렸다. 배가 불러서 우는 소리였다. 나는 그만 주저앉아 웃었다.",
        "스피노가 또 판을 벌였다. 저 녀석은 잃을 게 없어서 거는 게 아니다. 걸 것이 그것밖에 없는 거다.",
        "심장은 레시피를 기억한다. 고기와 불꽃과 시간을. 그렇다면... 사람도 기억할 수 있지 않을까.",
        "그 애가 황야의 끝으로 떠났다. 아무도 저녁을 주지 않는 곳으로. 요리사가 손님을 쫓아가지 않으면, 누가 가나.",
        "연료가 다 떨어져 간다. 요리사가 기차에게 줄 수 있는 마지막 재료가 무엇인지, 나는 이미 알고 있다.",
        "내 기차가 더는 내 목소리를 모른다. 오늘 마지막 식사를 함께 했다. 내일 나는 저 애의 연료칸에 눕는다. 부디 다음 요리사는, 나보다 나은 사람이기를.",
    };

    // ==================================================================
    //  공개 API (GameManager가 호출)
    // ==================================================================

    /// <summary>
    /// 런 시작 연출. 클릭/아무 키로 즉시 스킵 가능.
    /// - 1회차: 오프닝 3줄 (세계관 소개)
    /// - 2회차: 스피노의 첫 귀환 인사 (고정 - "죽어도 끝이 아니다" 납득)
    /// - 3회차 이후: 스피노 인사 랜덤 로테이션
    /// (BeginRun이 먼저 호출되어 RunsPlayed가 이미 이번 런을 포함한 상태)
    /// </summary>
    public static void ShowOpening()
    {
        int runs = MetaProgress.RunsPlayed;

        if (runs <= 1)
        {
            ShowLines(OpeningLines, null, 2.2f, true);
        }
        else if (runs == 2)
        {
            ShowLines(new string[] { FirstReturnLine }, "- 도박사 스피노", 3.5f, true);
        }
        else
        {
            string line = ReturnLines[Random.Range(0, ReturnLines.Length)];
            ShowLines(new string[] { line }, "- 도박사 스피노", 2.5f, true);
        }
    }

    /// <summary>사망 시 - 기관심장이 셰프를 다시 굽기 시작한다 (다음 런의 복선).</summary>
    public static void ShowDeathQuote()
    {
        ShowLines(new string[] { DeathLine }, "- 기관심장이 낮게 웅웅거린다", 5.5f, false);
    }

    /// <summary>아홉 개의 목숨 부활 연출 문구.</summary>
    public static void ShowReviveQuote()
    {
        ShowLines(new string[] { ReviveLine }, null, 3.5f, false);
    }

    /// <summary>승리 문구.</summary>
    public static void ShowVictoryQuote()
    {
        ShowLines(new string[] { VictoryLine }, "- 도박사 스피노 (그답지 않게 조용했다)", 6f, false);
    }

    /// <summary>
    /// 도감 신규 등록 연출 - 역대 최초 발견한 요리의 플레이버 텍스트를 크게 표시.
    /// (FoodStock이 호출. 도감에는 영구 기록되어 다음 런에도 남는다)
    /// </summary>
    public static void ShowRecipeFlavor(string dishName, string flavor, int totalCount)
    {
        ShowLines(new string[]
        {
            "[도감 " + totalCount + "번째 등록]  " + dishName,
            flavor
        }, null, 2.2f, false);
    }

    // ─────────────────────────────────────────────
    // C-2: 엔딩 B "마지막 식사" (진엔딩)
    // ─────────────────────────────────────────────

    /// <summary>
    /// 엔딩 B 직후 승리 처리에서 스피노 침묵 문구를 생략하기 위한 플래그
    /// (엔딩 B 연출이 이미 스피노의 마지막 대사를 포함하므로 중복 방지)
    /// </summary>
    public static bool TrueEndingJustPlayed = false;

    private static readonly string[] EndingBLines =
    {
        "디 오리지널은 아주 천천히, 아주 오래 씹었다.",
        "백 년 만의 저녁이었다.",
        "기차가 기적을 울렸다. 배부른 소리였다. 두 대가, 나란히.",
        "- 엔딩 B: 마지막 식사 -",
        "\"...계산은 내가 하지. 전부 다.\"  - 도박사 스피노",
    };

    /// <summary>엔딩 B 연출 (FinalOrderUI가 호출). 닫히면 onClosed 실행.</summary>
    public static void ShowEndingB(System.Action onClosed)
    {
        TrueEndingJustPlayed = true;
        ShowLines(EndingBLines, "선대의 일지, 마지막 장 뒤에 새 글씨가 적혔다", 3f, true, onClosed);
    }

    /// <summary>
    /// 선대의 일지 연출 - 폐역 클리어 보상 (WaveManager가 호출).
    /// 전체 화면을 어둡게 깔고 전문을 표시. 클릭/아무 키로 스킵.
    /// 연출이 닫히면 onClosed 콜백 실행 (증강 선택으로 이어짐).
    /// </summary>
    public static void ShowJournal(int number, System.Action onClosed)
    {
        if (number < 1 || number > JournalTexts.Length)
        {
            if (onClosed != null) onClosed();
            return;
        }

        ShowLines(new string[]
        {
            "선대의 일지  #" + number + "  (" + MetaProgress.CollectedJournalCount + "/12)",
            JournalTexts[number - 1]
        }, "- 서명은 불탄 자국뿐이다", 4.5f, true, onClosed);
    }

    /// <summary>일지 총 장수 (P1: 열람 UI용)</summary>
    public static int JournalCount { get { return JournalTexts.Length; } }

    /// <summary>일지 본문 조회 (P1: 열람 UI용, 1-based. 범위 밖이면 빈 문자열)</summary>
    public static string GetJournalText(int number)
    {
        if (number < 1 || number > JournalTexts.Length) return "";
        return JournalTexts[number - 1];
    }

    // ==================================================================
    //  연출 구현 - 호출 시 캔버스를 만들고 러너가 페이드 처리 후 자폭
    // ==================================================================

    /// <param name="lines">표시할 줄들 (순차 페이드 인)</param>
    /// <param name="speaker">화자 표기 (null이면 생략)</param>
    /// <param name="secondsPerLine">줄당 유지 시간</param>
    /// <param name="dimBackground">배경을 어둡게 깔지 (오프닝/일지용)</param>
    /// <param name="onDone">연출이 끝나거나 스킵되면 호출 (없으면 null)</param>
    private static void ShowLines(string[] lines, string speaker, float secondsPerLine, bool dimBackground,
        System.Action onDone = null)
    {
        GameObject canvasGo = new GameObject("StoryCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 650;   // 증강(600)보다 위, 일시정지(700)보다 아래
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // 배경 (오프닝만 화면 전체를 어둡게)
        if (dimBackground)
        {
            RectTransform bg = KitchenEventManager.MakeBox(canvasGo.transform, "Dim",
                new Color(0f, 0f, 0f, 0.78f));
            bg.anchorMin = Vector2.zero;
            bg.anchorMax = Vector2.one;
            bg.offsetMin = Vector2.zero;
            bg.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().raycastTarget = false;
        }

        // 본문 줄들 (가운데, 세로로 나열)
        // dim 모드(오프닝/일지)는 긴 문장이 줄바꿈될 수 있어 간격/높이를 넉넉하게
        int count = lines.Length;
        float spacing = dimBackground ? 96f : 68f;
        float lineHeight = dimBackground ? 120f : 60f;
        Text[] lineTexts = new Text[count];
        float startY = (count - 1) * spacing * 0.5f;
        for (int i = 0; i < count; i++)
        {
            lineTexts[i] = KitchenEventManager.MakeText(canvasGo.transform, "Line" + i,
                lines[i], dimBackground ? 30 : 26,
                new Color(0.95f, 0.9f, 0.78f));
            lineTexts[i].verticalOverflow = VerticalWrapMode.Overflow;   // 줄바꿈 시 잘림 방지
            RectTransform rt = lineTexts[i].rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, startY - i * spacing + (dimBackground ? 40f : 120f));
            rt.sizeDelta = new Vector2(1500f, lineHeight);
            SetAlpha(lineTexts[i], 0f);   // 페이드 인 대기
        }

        // 화자 표기
        Text speakerText = null;
        if (!string.IsNullOrEmpty(speaker))
        {
            speakerText = KitchenEventManager.MakeText(canvasGo.transform, "Speaker",
                speaker, 20, new Color(0.7f, 0.62f, 0.45f));
            RectTransform srt = speakerText.rectTransform;
            srt.anchorMin = new Vector2(0.5f, 0.5f);
            srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2(0f, startY - count * spacing + 130f);
            srt.sizeDelta = new Vector2(1200f, 34f);
            SetAlpha(speakerText, 0f);
        }

        // 러너가 페이드/스킵/자폭을 담당
        StoryRunner runner = canvasGo.AddComponent<StoryRunner>();
        runner.Setup(lineTexts, speakerText, secondsPerLine, dimBackground);
        runner.onDone = onDone;

        // 전체 화면 연출(오프닝)은 스폰 차단 플래그를 올린다
        if (dimBackground)
        {
            IsBlocking = true;
            runner.blocksSpawn = true;
        }
    }

    private static void SetAlpha(Text t, float a)
    {
        Color c = t.color; c.a = a; t.color = c;
    }

    // ==================================================================
    //  연출 러너 (같은 파일 안의 보조 컴포넌트 - 직접 붙일 일 없음)
    // ==================================================================
    private class StoryRunner : MonoBehaviour
    {
        private Text[] lines;
        private Text speaker;
        private float perLine;
        private bool skippable;

        // 이 러너가 스폰 차단 플래그를 올렸는가 (오프닝/일지만 true)
        public bool blocksSpawn = false;

        // 연출 종료/스킵 시 호출할 콜백 (일지 -> 증강 선택 이어가기 등)
        public System.Action onDone = null;

        public void Setup(Text[] lineTexts, Text speakerText, float secondsPerLine, bool canSkip)
        {
            lines = lineTexts;
            speaker = speakerText;
            perLine = secondsPerLine;
            skippable = canSkip;
            StartCoroutine(Play());
        }

        // 연출이 어떤 이유로든 사라지면(정상 종료/스킵/씬 전환) 스폰 차단 해제 + 콜백
        private void OnDestroy()
        {
            if (blocksSpawn) StoryTexts.IsBlocking = false;

            System.Action cb = onDone;
            onDone = null;
            if (cb != null) cb();
        }

        private IEnumerator Play()
        {
            // 줄 순차 페이드 인
            for (int i = 0; i < lines.Length; i++)
            {
                float t = 0f;
                while (t < 0.6f)
                {
                    if (SkipPressed()) { Destroy(gameObject); yield break; }
                    t += Time.unscaledDeltaTime;
                    SetA(lines[i], t / 0.6f);
                    yield return null;
                }
                SetA(lines[i], 1f);

                // 다음 줄까지 대기 (마지막 줄은 아래 유지 구간에서 처리)
                if (i < lines.Length - 1)
                {
                    float w = 0f;
                    while (w < perLine)
                    {
                        if (SkipPressed()) { Destroy(gameObject); yield break; }
                        w += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }
            }

            // 화자 표기 페이드 인
            if (speaker != null)
            {
                float t = 0f;
                while (t < 0.5f)
                {
                    if (SkipPressed()) { Destroy(gameObject); yield break; }
                    t += Time.unscaledDeltaTime;
                    SetA(speaker, t / 0.5f);
                    yield return null;
                }
            }

            // 유지
            float hold = 0f;
            while (hold < perLine)
            {
                if (SkipPressed()) { Destroy(gameObject); yield break; }
                hold += Time.unscaledDeltaTime;
                yield return null;
            }

            // 전체 페이드 아웃
            float f = 0f;
            while (f < 0.8f)
            {
                f += Time.unscaledDeltaTime;
                float a = 1f - (f / 0.8f);
                for (int i = 0; i < lines.Length; i++) SetA(lines[i], a);
                if (speaker != null) SetA(speaker, a);
                yield return null;
            }

            Destroy(gameObject);
        }

        private bool SkipPressed()
        {
            // 오프닝만 스킵 허용 (사망/승리 문구는 짧아서 그대로 재생)
            return skippable && (Input.anyKeyDown || Input.GetMouseButtonDown(0));
        }

        private void SetA(Text t, float a)
        {
            if (t == null) return;
            Color c = t.color; c.a = a; t.color = c;
        }
    }
}
