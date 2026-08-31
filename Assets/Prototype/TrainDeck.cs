using UnityEngine;

/// <summary>
/// [TrainDeck.cs] v1 (신규 파일) - B-2: 트레일러 4칸 코드 데크 (방향결정 2026-08-31)
///
/// 기차가 4칸(기관차/주방/포탑A/포탑B)으로 늘어나면서, 셰프가 걷는 갑판을
/// 코드 생성 도형으로 그린다. 칸 몸체 / 지붕·바닥 트림 / 연결부 / 바퀴 / 기관차 굴뚝.
/// 씬 작업 0 원칙 - 아트 단계에서 칸별 스프라이트로 교체 예정 (백로그 1절).
///
/// 칸 경계는 GameBalance.CarEdgesX가 단일 소스 (기차 스트립 UI / 이벤트 앵커와 공유).
/// 씬에 기존 기차 스프라이트가 있으면 겹쳐 보일 수 있음 - 거슬리면 그 오브젝트만
/// 비활성화하면 된다 (선택 사항, 필수 아님).
///
/// 사용법: 없음! 파일만 넣으면 자동 생성된다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class TrainDeck : MonoBehaviour
{
    private static TrainDeck instance;

    // 정렬 순서: 패럴랙스(-30~-10)보다 앞, 셰프/적(0+)보다 뒤
    private const int SORT_BODY = -6;
    private const int SORT_TRIM = -5;
    private const int SORT_DETAIL = -4;

    // 색 (구리 기차 - 원안: Copper + 검정 포인트)
    private static readonly Color BODY_A = new Color(0.42f, 0.26f, 0.16f);   // 기관차 (짙은 구리)
    private static readonly Color BODY_B = new Color(0.48f, 0.30f, 0.18f);   // 주방칸
    private static readonly Color BODY_C = new Color(0.45f, 0.28f, 0.17f);   // 포탑칸
    private static readonly Color TRIM = new Color(0.16f, 0.11f, 0.08f);     // 검정 포인트
    private static readonly Color COUPLER = new Color(0.12f, 0.09f, 0.07f);  // 연결부
    private static readonly Color WHEEL = new Color(0.10f, 0.08f, 0.07f);    // 바퀴

    private static Sprite whiteSprite;   // 1x1 (사각형용)
    private static Sprite circleSprite;  // 바퀴용 원

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("TrainDeck");
        DontDestroyOnLoad(go);
        go.AddComponent<TrainDeck>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        Build();
    }

    // ─────────────────────────────────────────────
    // 데크 생성
    // ─────────────────────────────────────────────
    private void Build()
    {
        float[] edges = GameBalance.CarEdgesX;
        float bodyBottom = -1.8f, bodyTop = 1.8f;

        for (int car = 0; car < edges.Length - 1; car++)
        {
            // 칸 몸체 (경계에서 0.12씩 안쪽으로 - 칸 사이 틈이 보이게)
            float left = edges[car] + 0.12f;
            float right = edges[car + 1] - 0.12f;
            Color body = car == 0 ? BODY_A : (car == 1 ? BODY_B : BODY_C);

            MakeQuad("Car" + car + "_Body", left, right, bodyBottom, bodyTop, body, SORT_BODY);
            MakeQuad("Car" + car + "_Roof", left, right, bodyTop - 0.22f, bodyTop, TRIM, SORT_TRIM);
            MakeQuad("Car" + car + "_Floor", left, right, bodyBottom, bodyBottom + 0.28f, TRIM, SORT_TRIM);

            // 바퀴 2개 (칸 양끝 쪽)
            MakeWheel("Car" + car + "_WheelL", left + 0.7f);
            MakeWheel("Car" + car + "_WheelR", right - 0.7f);

            // 연결부 (다음 칸과의 틈)
            if (car < edges.Length - 2)
                MakeQuad("Coupler" + car, edges[car + 1] - 0.18f, edges[car + 1] + 0.18f,
                    -0.9f, -0.4f, COUPLER, SORT_DETAIL);
        }

        // 기관차 디테일: 굴뚝 + 보일러 돔 (칸 0 왼쪽)
        float locoLeft = edges[0];
        MakeQuad("Chimney", locoLeft + 0.7f, locoLeft + 1.3f, bodyTop, bodyTop + 1.2f, TRIM, SORT_DETAIL);
        MakeQuad("BoilerDome", locoLeft + 1.9f, locoLeft + 2.7f, bodyTop, bodyTop + 0.5f,
            new Color(0.55f, 0.35f, 0.2f), SORT_DETAIL);

        Debug.Log("[TrainDeck] 4칸 데크 생성 완료 (경계 " + edges[0] + " ~ " + edges[edges.Length - 1] + ")");
    }

    // ─────────────────────────────────────────────
    // 도형 헬퍼
    // ─────────────────────────────────────────────
    private void MakeQuad(string name, float left, float right, float bottom, float top,
        Color color, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0f);
        go.transform.localScale = new Vector3(right - left, top - bottom, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetWhiteSprite();
        sr.color = color;
        sr.sortingOrder = order;
    }

    private void MakeWheel(string name, float x)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(x, -1.95f, 0f);
        go.transform.localScale = Vector3.one * 0.85f;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircleSprite();
        sr.color = WHEEL;
        sr.sortingOrder = SORT_BODY;
    }

    /// <summary>1x1 흰 스프라이트 (스케일로 사각형을 만든다)</summary>
    public static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return whiteSprite;
    }

    /// <summary>지름 1 월드 유닛짜리 원 스프라이트 (바퀴/상자 장식용)</summary>
    public static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        int s = 32;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        float r = s * 0.5f - 0.5f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = x - r, dy = y - r;
                bool inside = dx * dx + dy * dy <= r * r;
                tex.SetPixel(x, y, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        return circleSprite;
    }
}
