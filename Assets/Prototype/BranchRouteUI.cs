using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 분기 선로 1개의 정의 (다음 웨이브에 적용될 규칙 + 보상)
/// </summary>
public class RouteData
{
    public string id;          // 내부 식별자
    public string routeName;   // 표시 이름
    public string desc;        // 위험 설명
    public string rewardDesc;  // 보상 설명

    public float countMul = 1f;   // 적 물량 배율
    public float statMul = 1f;    // 적 HP/ATK 배율
    public int rewardGold = 0;    // 클리어 보상 골드
    public int rewardMats = 0;    // 클리어 보상 랜덤 재료 수
    public bool journal = false;  // 클리어 시 선대의 일지 발견 (폐역)
    public bool earlyEvent = false; // 방해 이벤트가 이르게 옴 (안개)
}

/// <summary>
/// [BranchRouteUI.cs] v1 (신규 파일) - Phase 2: 분기 선로
/// 증강 선택이 끝난 뒤 "다음 웨이브로 가는 길"을 2~3개 중에서 고른다 (슬더스 맵 노드식).
/// 위험을 얼마나 감수하고 무엇을 얻을지가 매 웨이브의 선택이 된다.
///
/// 사용법:
///  1) 파일을 Assets/Prototype에 넣는다
///  2) 하이어라키 아무 오브젝트(AugmentPickUI 있는 곳 추천)에 AddComponent
///  3) 씬 배치 필요 없음 - WaveManager가 자동으로 호출한다 (없으면 기존 흐름 그대로)
///
/// 선로 종류:
///  - 곧은 선로: 표준 (항상 등장)
///  - 위험 선로: 적 물량 +40%, 강화 +15% / 보상 골드 +150, 재료 +2
///  - 사냥터 선로: 적 물량 +20% / 보상 재료 +3
///  - 안개 선로: 적 물량 -25%, 대신 방해 이벤트가 이르게 온다 / 보상 골드 +80
///  - 폐역 (확률 등장): 적 물량 -40% / 클리어 시 선대의 일지 1장 발견 (영구 수집)
///  - 보스 웨이브 직전에는 곧은/위험 2택만 (보스전 변수 최소화)
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class BranchRouteUI : MonoBehaviour
{
    public static BranchRouteUI Instance;

    public static bool IsOpen { get; private set; }

    private GameObject canvasGo;
    private System.Action<RouteData> onChosen;

    // v1.1: 숫자키 선택용 - 현재 표시 중인 선로 목록
    private List<RouteData> shownRoutes = new List<RouteData>();

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        // v1.1 (감사): 숫자키 1~3으로 선로 선택
        if (!IsOpen) return;
        for (int i = 0; i < shownRoutes.Count && i < 3; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                Choose(shownRoutes[i]);
                return;
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (canvasGo != null) Destroy(canvasGo);
        IsOpen = false;
    }

    // ─────────────────────────────────────────────
    // 선로 후보 생성
    // ─────────────────────────────────────────────
    private List<RouteData> BuildRoutes(int nextWave)
    {
        List<RouteData> routes = new List<RouteData>();

        // 곧은 선로 (항상)
        RouteData straight = new RouteData();
        straight.id = "straight";
        straight.routeName = "곧은 선로";
        straight.desc = "특이사항 없음.";
        straight.rewardDesc = "무난한 저녁.";
        routes.Add(straight);

        // 보스 웨이브 직전: 위험 선로와 2택만
        if (GameBalance.IsBossWave(nextWave))
        {
            routes.Add(MakeDanger());
            return routes;
        }

        // 일반 웨이브: 후보 풀에서 2개 뽑기
        List<RouteData> pool = new List<RouteData>();
        pool.Add(MakeDanger());

        RouteData hunt = new RouteData();
        hunt.id = "hunt";
        hunt.routeName = "사냥터 선로";
        hunt.desc = "적 물량 +20%";
        hunt.rewardDesc = "클리어 시 재료 +3";
        hunt.countMul = 1.2f;
        hunt.rewardMats = 3;
        pool.Add(hunt);

        RouteData fog = new RouteData();
        fog.id = "fog";
        fog.routeName = "안개 선로";
        fog.desc = "적 물량 -25%, 주방 사고가 일찍 찾아온다";
        fog.rewardDesc = "클리어 시 골드 +80";
        fog.countMul = 0.75f;
        fog.earlyEvent = true;
        fog.rewardGold = 80;
        pool.Add(fog);

        // 풀에서 무작위 2개
        while (routes.Count < 3 && pool.Count > 0)
        {
            int idx = Random.Range(0, pool.Count);
            routes.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        // 폐역: 25% 확률, 미수집 일지가 남아 있을 때만 마지막 칸을 대체
        if (Random.value < 0.25f && MetaProgress.PickUncollectedJournal() > 0)
        {
            RouteData ghost = new RouteData();
            ghost.id = "ghost";
            ghost.routeName = "폐역";
            ghost.desc = "적 물량 -40%. 버려진 역에 무언가 남아 있다";
            ghost.rewardDesc = "클리어 시 선대의 일지 발견";
            ghost.countMul = 0.6f;
            ghost.journal = true;
            routes[routes.Count - 1] = ghost;
        }

        return routes;
    }

    private RouteData MakeDanger()
    {
        RouteData danger = new RouteData();
        danger.id = "danger";
        danger.routeName = "위험 선로";
        danger.desc = "적 물량 +40%, 적 강화 +15%";
        danger.rewardDesc = "클리어 시 골드 +150, 재료 +2";
        danger.countMul = 1.4f;
        danger.statMul = 1.15f;
        danger.rewardGold = 150;
        danger.rewardMats = 2;
        return danger;
    }

    // ─────────────────────────────────────────────
    // 표시 (WaveManager가 호출)
    // ─────────────────────────────────────────────
    public void ShowRoutes(int nextWave, System.Action<RouteData> chosenCallback)
    {
        onChosen = chosenCallback;
        List<RouteData> routes = BuildRoutes(nextWave);
        shownRoutes = routes;   // v1.1: 숫자키 선택용

        IsOpen = true;
        Time.timeScale = 0f;   // 고르는 동안 일시정지 (증강 선택과 동일)

        canvasGo = new GameObject("BranchRouteCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 590;   // 정비소(550)와 증강(600) 사이
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // 어두운 배경
        RectTransform dim = KitchenEventManager.MakeBox(canvasGo.transform, "Dim",
            new Color(0f, 0f, 0f, 0.6f));
        dim.anchorMin = Vector2.zero;
        dim.anchorMax = Vector2.one;
        dim.offsetMin = Vector2.zero;
        dim.offsetMax = Vector2.zero;

        // 제목
        Text title = KitchenEventManager.MakeText(canvasGo.transform, "Title",
            "분기 선로 - 다음 길을 선택하라 (Wave " + nextWave + ")  [1~" + routes.Count + "]", 32,
            new Color(1f, 0.78f, 0.32f));
        RectTransform tRt = title.rectTransform;
        tRt.anchorMin = new Vector2(0.5f, 0.5f);
        tRt.anchorMax = new Vector2(0.5f, 0.5f);
        tRt.pivot = new Vector2(0.5f, 0.5f);
        tRt.anchoredPosition = new Vector2(0f, 250f);
        tRt.sizeDelta = new Vector2(1200f, 44f);

        // 선로 카드들 (가로 배치)
        int count = routes.Count;
        float cardW = 300f;
        float gap = 40f;
        float totalW = count * cardW + (count - 1) * gap;
        float startX = -totalW / 2f + cardW / 2f;

        for (int i = 0; i < count; i++)
        {
            RouteData route = routes[i];   // 클로저 캡처용 지역 변수
            float x = startX + i * (cardW + gap);

            RectTransform card = KitchenEventManager.MakeBox(canvasGo.transform, "Card_" + route.id,
                new Color(0.14f, 0.11f, 0.09f, 0.97f));
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = new Vector2(x, 30f);
            card.sizeDelta = new Vector2(cardW, 340f);

            // 선로 이름
            Text nameText = KitchenEventManager.MakeText(card, "Name", route.routeName, 26,
                route.id == "ghost" ? new Color(0.7f, 0.85f, 1f) :
                route.id == "danger" ? new Color(1f, 0.55f, 0.4f) :
                new Color(1f, 0.92f, 0.8f));
            RectTransform nRt = nameText.rectTransform;
            nRt.anchorMin = new Vector2(0f, 1f);
            nRt.anchorMax = new Vector2(1f, 1f);
            nRt.pivot = new Vector2(0.5f, 1f);
            nRt.anchoredPosition = new Vector2(0f, -18f);
            nRt.sizeDelta = new Vector2(0f, 34f);

            // 위험 설명
            Text descText = KitchenEventManager.MakeText(card, "Desc", route.desc, 19,
                new Color(0.85f, 0.82f, 0.75f));
            RectTransform dRt = descText.rectTransform;
            dRt.anchorMin = new Vector2(0f, 1f);
            dRt.anchorMax = new Vector2(1f, 1f);
            dRt.pivot = new Vector2(0.5f, 1f);
            dRt.anchoredPosition = new Vector2(0f, -70f);
            dRt.offsetMin = new Vector2(14f, dRt.offsetMin.y);
            dRt.offsetMax = new Vector2(-14f, dRt.offsetMax.y);
            dRt.sizeDelta = new Vector2(dRt.sizeDelta.x, 90f);

            // 보상 설명
            Text rewardText = KitchenEventManager.MakeText(card, "Reward", route.rewardDesc, 19,
                new Color(0.98f, 0.85f, 0.45f));
            RectTransform rRt = rewardText.rectTransform;
            rRt.anchorMin = new Vector2(0f, 1f);
            rRt.anchorMax = new Vector2(1f, 1f);
            rRt.pivot = new Vector2(0.5f, 1f);
            rRt.anchoredPosition = new Vector2(0f, -170f);
            rRt.offsetMin = new Vector2(14f, rRt.offsetMin.y);
            rRt.offsetMax = new Vector2(-14f, rRt.offsetMax.y);
            rRt.sizeDelta = new Vector2(rRt.sizeDelta.x, 70f);

            // 선택 버튼
            Button btn = KitchenEventManager.MakeButton(card, "이 길로 간다",
                new Color(0.5f, 0.32f, 0.12f), new Vector2(0f, -130f), new Vector2(220f, 52f));
            btn.onClick.AddListener(delegate { Choose(route); });
        }
    }

    // ─────────────────────────────────────────────
    // 선택 처리
    // ─────────────────────────────────────────────
    private void Choose(RouteData route)
    {
        Debug.Log("[분기선로] 선택: " + route.routeName);

        if (canvasGo != null) Destroy(canvasGo);
        canvasGo = null;
        IsOpen = false;
        Time.timeScale = 1f;

        System.Action<RouteData> cb = onChosen;
        onChosen = null;
        if (cb != null) cb(route);
    }
}
