using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [BaitStationUI.cs] v1.1 (v9.12 2026-09-22: 판 340x190 - 무엇을 하나 두 줄 + 굽기 버튼 + 판정 안내 한 줄(목업 v4.2 (C)) / 유인 횟수 LuresThisScene·TimingActive (견습 구간 7 미니 보스 예습이 완료 판정에 쓴다) / 문구 일상어)
/// / v1 (신규 파일) - 보스 패턴 C단계
/// 미끼 화덕 - 녹슨 발톱(지역 1 보스) 전용 시그니처 기믹.
///
/// 스토리: 선대의 메모 - "왕을 잡으려면 왕의 손님부터 대접해라."
/// 무리의 왕은 무리를 먹이는 가장이다. 미끼로 무리가 몰려가면 왕도 지키러 온다.
///
/// 기믹: 고기 재료 1개로 미끼를 굽는다(타이밍 판정) -> 자동 투척 ->
///       랩터 무리 + 보스가 미끼로 유인된다 (물어뜯는 동안 기차 무피해)
///       잘 구울수록(PERFECT) 유인 시간이 길다: 8초 / 6초 / 4초
///
/// 사용법: 없음! BossGimmickSystem이 녹슨 발톱 보스전 시작 시 자동 생성.
/// 수치는 GameBalance '보스 패턴 (C단계)' 섹션에서 조정.
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class BaitStationUI : MonoBehaviour
{
    private BossEnemy boss;

    // ── UI ──
    private GameObject canvasGo;
    private Button bakeButton;
    private Text bakeLabel;
    private Text statusText;

    // ── 굽기 타이밍 미니게임 ──
    private GameObject timingRoot;
    private RectTransform timingCursor;
    private bool timingActive = false;
    private float cursorPos = 0f;
    private float cursorDir = 1f;
    private const float CURSOR_SPEED = 90f;
    private const float TRACK_W = 460f;

    // ── 상태 ──
    private float cooldownUntil = 0f;
    private static Sprite baitSprite;

    /// <summary>v1.1: 이 화덕이 생긴 뒤 미끼를 던져 유인에 성공한 횟수 (한 마리라도 물었으면 1). 예습 완료 판정용</summary>
    public static int LuresThisScene { get; private set; }
    /// <summary>v1.1: 굽기 판정 눈금이 움직이는 중</summary>
    public static bool TimingActive { get; private set; }

    public void Setup(BossEnemy targetBoss)
    {
        boss = targetBoss;
        TimingActive = false;          // LuresThisScene 은 누적 - 디렉터가 시작 전 값과의 차이로 센다
        BuildUI();
        UIManager.Instance?.ShowStatChange("[미끼 화덕] 가동 - 고기를 구워 던지면 무리가 미끼로 몰린다");
        Debug.Log("[BaitStation] 미끼 화덕 가동 (녹슨 발톱 보스전)");
    }

    private void OnDestroy()
    {
        if (canvasGo != null) Destroy(canvasGo);
    }

    private void Update()
    {
        if (boss == null || !boss.IsAlive)
        {
            Destroy(gameObject);
            return;
        }

        // 버튼 상태 갱신
        bool onCooldown = Time.time < cooldownUntil;
        int meat = MaterialInventory.Instance != null ? MaterialInventory.Instance.Get(MaterialType.Meat) : 0;
        bakeButton.interactable = !timingActive && !onCooldown && meat > 0 && !CookingMinigame.IsActive;

        if (onCooldown)
            statusText.text = "화덕 재가열 중... " + Mathf.CeilToInt(cooldownUntil - Time.time) + "초";
        else if (meat <= 0)
            statusText.text = "고기가 없다 - 손님이 남긴 고기를 모아라";
        else
            statusText.text = "고기 " + meat + "개 - 잘 구울수록 오래 유인한다";

        TimingActive = timingActive;
        if (timingActive)
            UpdateTiming();
    }

    // ─────────────────────────────────────────────
    // 미끼 굽기 시작
    // ─────────────────────────────────────────────
    private void OnBake()
    {
        if (timingActive || Time.time < cooldownUntil) return;
        if (MaterialInventory.Instance == null || MaterialInventory.Instance.Get(MaterialType.Meat) <= 0)
            return;

        MaterialInventory.Instance.Add(MaterialType.Meat, -1);

        timingActive = true;
        TimingActive = true;
        cursorPos = 0f;
        cursorDir = 1f;
        timingRoot.SetActive(true);
    }

    private void UpdateTiming()
    {
        cursorPos += cursorDir * CURSOR_SPEED * Time.deltaTime;
        if (cursorPos >= 100f) { cursorPos = 100f; cursorDir = -1f; }
        if (cursorPos <= 0f) { cursorPos = 0f; cursorDir = 1f; }

        timingCursor.anchoredPosition = new Vector2(-TRACK_W / 2f + TRACK_W * (cursorPos / 100f), 0f);

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            ResolveBake();
    }

    private void ResolveBake()
    {
        timingActive = false;
        TimingActive = false;
        timingRoot.SetActive(false);
        cooldownUntil = Time.time + GameBalance.BaitCooldown;

        // 판정: 중앙(50) 거리
        float d = Mathf.Abs(cursorPos - 50f);
        float duration;
        string grade;
        if (d <= 7f) { duration = GameBalance.BaitDurationPerfect; grade = "PERFECT"; }
        else if (d <= 20f) { duration = GameBalance.BaitDurationGood; grade = "GOOD"; }
        else { duration = GameBalance.BaitDurationMiss; grade = "탄 미끼"; }

        DeployBait(duration);
        UIManager.Instance?.ShowStatChange("[" + grade + "] 미끼를 던졌다 - " + duration + "초 동안 무리가 미끼로 몰린다");
        Debug.Log("[BaitStation] 미끼 굽기 " + grade + " -> 유인 " + duration + "초");
    }

    // ─────────────────────────────────────────────
    // 미끼 설치 + 무리/보스 유인
    // ─────────────────────────────────────────────
    private void DeployBait(float duration)
    {
        // 기차에서 일정 거리 떨어진 랜덤 위치
        GameObject train = GameObject.FindGameObjectWithTag("Train");
        Vector3 center = train != null ? train.transform.position : Vector3.zero;
        float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 pos = center + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * GameBalance.BaitDistance;

        // 미끼 오브젝트 (코드 생성 스프라이트)
        GameObject bait = new GameObject("Bait");
        bait.transform.position = pos;
        SpriteRenderer sr = bait.AddComponent<SpriteRenderer>();
        sr.sprite = GetBaitSprite();
        sr.color = new Color(0.85f, 0.45f, 0.3f);
        sr.sortingOrder = 55;
        bait.transform.localScale = Vector3.one * 0.9f;
        bait.AddComponent<BaitPulse>();

        // 유인: 랩터 무리(Pack) + 보스
        int lured = 0;
        Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].IsAlive) continue;
            if (all[i] == boss || all[i].behavior == Enemy.BehaviorPattern.Pack)
            {
                all[i].Taunt(bait.transform, duration);
                lured++;
            }
        }

        if (lured > 0) LuresThisScene++;
        else UIManager.Instance?.ShowStatChange("[미끼] 물 손님이 없었다 - 무리가 왔을 때 던져라");

        Destroy(bait, duration);
        Debug.Log("[BaitStation] 미끼 설치 - " + lured + "마리 유인 (" + duration + "초) / 누적 성공 " + LuresThisScene);
    }

    /// <summary>미끼가 침 흘리게 맥동하는 연출용 보조 컴포넌트</summary>
    private class BaitPulse : MonoBehaviour
    {
        private float t = 0f;
        private void Update()
        {
            t += Time.deltaTime;
            float s = 0.9f + Mathf.Sin(t * 6f) * 0.12f;
            transform.localScale = Vector3.one * s;
        }
    }

    private static Sprite GetBaitSprite()
    {
        if (baitSprite != null) return baitSprite;

        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), c) <= 7f ? Color.white : Color.clear);
        tex.Apply();
        baitSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        return baitSprite;
    }

    // ─────────────────────────────────────────────
    // UI 생성 (좌측 중단 - 해동포와 같은 자리, 서로 다른 보스전이라 안 겹침)
    // ─────────────────────────────────────────────
    private void BuildUI()
    {
        canvasGo = new GameObject("BaitStationCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 485;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform panel = KitchenEventManager.MakeBox(canvasGo.transform, "BaitPanel",
            new Color(0.1f, 0.08f, 0.06f, 0.92f));
        panel.anchorMin = new Vector2(0f, 0.5f);
        panel.anchorMax = new Vector2(0f, 0.5f);
        panel.pivot = new Vector2(0f, 0.5f);
        panel.anchoredPosition = new Vector2(14f, 60f);
        panel.sizeDelta = new Vector2(340f, 190f);

        Text title = KitchenEventManager.MakeText(panel, "Title", "미끼 화덕", 21,
            new Color(1f, 0.7f, 0.35f));
        RectTransform tRt = title.rectTransform;
        tRt.anchorMin = new Vector2(0f, 1f); tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -8f);
        tRt.sizeDelta = new Vector2(0f, 26f);

        // v1.1: 무엇을 하나 (두 줄) - 처음 보는 사람도 읽고 바로 한다
        Text how = KitchenEventManager.MakeText(panel, "How",
            "고기 1개를 구워 던지면 무리가 미끼로 몰린다\n무리가 미끼를 물면 왕도 따라온다 (" + Mathf.RoundToInt(GameBalance.BaitDurationMiss) + "~" + Mathf.RoundToInt(GameBalance.BaitDurationPerfect) + "초)", 14,
            new Color(0.969f, 0.910f, 0.776f));
        RectTransform hRt = how.rectTransform;
        hRt.anchorMin = new Vector2(0f, 1f); hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0.5f, 1f);
        hRt.anchoredPosition = new Vector2(0f, -36f);
        hRt.sizeDelta = new Vector2(-20f, 40f);

        statusText = KitchenEventManager.MakeText(panel, "Status", "", 14,
            new Color(0.85f, 0.8f, 0.7f));
        RectTransform sRt = statusText.rectTransform;
        sRt.anchorMin = new Vector2(0f, 1f); sRt.anchorMax = new Vector2(1f, 1f);
        sRt.pivot = new Vector2(0.5f, 1f);
        sRt.anchoredPosition = new Vector2(0f, -80f);
        sRt.sizeDelta = new Vector2(0f, 22f);

        bakeButton = KitchenEventManager.MakeButton(panel, "미끼 굽기  (고기 1)",
            new Color(0.5f, 0.3f, 0.12f), new Vector2(0f, -30f), new Vector2(280f, 44f));

        Text hint = KitchenEventManager.MakeText(panel, "Hint",
            "눈금이 판정 구간에 오면 [Space]  -  유인 " + Mathf.RoundToInt(GameBalance.BaitDurationPerfect) + "초 / " + Mathf.RoundToInt(GameBalance.BaitDurationGood) + "초 / " + Mathf.RoundToInt(GameBalance.BaitDurationMiss) + "초", 12,
            new Color(0.627f, 0.549f, 0.431f));
        RectTransform hiRt = hint.rectTransform;
        hiRt.anchorMin = new Vector2(0f, 0f); hiRt.anchorMax = new Vector2(1f, 0f);
        hiRt.pivot = new Vector2(0.5f, 0f);
        hiRt.anchoredPosition = new Vector2(0f, 8f);
        hiRt.sizeDelta = new Vector2(-20f, 18f);
        bakeLabel = bakeButton.GetComponentInChildren<Text>();
        bakeButton.onClick.AddListener(OnBake);

        // ── 굽기 타이밍 오버레이 (화면 중앙) ──
        RectTransform tRoot = KitchenEventManager.MakeBox(canvasGo.transform, "TimingRoot",
            new Color(0.08f, 0.05f, 0.04f, 0.95f));
        tRoot.anchorMin = new Vector2(0.5f, 0.5f);
        tRoot.anchorMax = new Vector2(0.5f, 0.5f);
        tRoot.pivot = new Vector2(0.5f, 0.5f);
        tRoot.anchoredPosition = new Vector2(0f, 140f);
        tRoot.sizeDelta = new Vector2(560f, 110f);
        timingRoot = tRoot.gameObject;

        Text tTitle = KitchenEventManager.MakeText(tRoot, "TTitle",
            "눈금이 판정 구간에 오면 [Space] - 가운데일수록 오래 유인한다", 22, new Color(1f, 0.8f, 0.4f));
        RectTransform ttRt = tTitle.rectTransform;
        ttRt.anchorMin = new Vector2(0f, 1f); ttRt.anchorMax = new Vector2(1f, 1f);
        ttRt.pivot = new Vector2(0.5f, 1f);
        ttRt.anchoredPosition = new Vector2(0f, -8f);
        ttRt.sizeDelta = new Vector2(0f, 28f);

        RectTransform track = KitchenEventManager.MakeBox(tRoot, "Track", new Color(0f, 0f, 0f, 0.6f));
        track.anchorMin = new Vector2(0.5f, 0f);
        track.anchorMax = new Vector2(0.5f, 0f);
        track.pivot = new Vector2(0.5f, 0f);
        track.anchoredPosition = new Vector2(0f, 22f);
        track.sizeDelta = new Vector2(TRACK_W, 26f);

        RectTransform goodZone = KitchenEventManager.MakeBox(track, "Good", new Color(0.35f, 0.6f, 0.3f, 0.8f));
        goodZone.anchorMin = new Vector2(0.5f, 0f); goodZone.anchorMax = new Vector2(0.5f, 1f);
        goodZone.pivot = new Vector2(0.5f, 0.5f);
        goodZone.anchoredPosition = Vector2.zero;
        goodZone.sizeDelta = new Vector2(TRACK_W * 0.4f, 0f);

        RectTransform perfectZone = KitchenEventManager.MakeBox(track, "Perfect", new Color(1f, 0.85f, 0.3f, 0.9f));
        perfectZone.anchorMin = new Vector2(0.5f, 0f); perfectZone.anchorMax = new Vector2(0.5f, 1f);
        perfectZone.pivot = new Vector2(0.5f, 0.5f);
        perfectZone.anchoredPosition = Vector2.zero;
        perfectZone.sizeDelta = new Vector2(TRACK_W * 0.14f, 0f);

        timingCursor = KitchenEventManager.MakeBox(track, "Cursor", Color.white);
        timingCursor.anchorMin = new Vector2(0.5f, 0f); timingCursor.anchorMax = new Vector2(0.5f, 1f);
        timingCursor.pivot = new Vector2(0.5f, 0.5f);
        timingCursor.sizeDelta = new Vector2(6f, 8f);

        timingRoot.SetActive(false);
    }
}
