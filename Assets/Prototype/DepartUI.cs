using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [DepartUI.cs] v1 (신규, v9.10 2026-09-17) - 정차 출발 버튼
///
/// 개정안 §4 "정비는 출발 확인제": WaveManager 가 정차 뒤 자동 출발 대신 WaitingDepart 로 기다린다(GameBalance.DepartConfirm).
/// 이 버튼은 그동안만 화면 아래 가운데(하단 바 위)에 뜬다. 클릭 = WaveManager.RequestDepart(). [Enter] 는 WaveManager.Update 가 처리.
/// 다른 창(증강·정비소·베팅·행상인·선로·카드·일시정지)이 떠 있으면 숨긴다 - 뒤 화면 클릭 금지.
///
/// 사용법: 없음! 파일만 넣으면 자동 생성 (DontDestroyOnLoad, 씬 리로드 뒤에도 남는다).
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class DepartUI : MonoBehaviour
{
    private static DepartUI instance;
    private Canvas canvas;
    private Button button;
    private RectTransform buttonRt;
    private Text label;
    private float pulse;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("DepartUI");
        DontDestroyOnLoad(go);
        go.AddComponent<DepartUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        Build();
    }

    private void Build()
    {
        canvas = UIFactory.CreateCanvas("Depart_Canvas", 20);   // HUD(10) 위, 조리 미니게임(30) 아래
        canvas.transform.SetParent(transform, false);

        // 하단 바(184) 위 16px, 가운데. 340x56 - 로비 출발 버튼과 같은 말투
        button = UIFactory.CreateButton(canvas.transform, "DepartBtn", "출발한다!  [Enter]",
            new Vector2(340f, 56f), UIFactory.COPPER, UIFactory.CREAM, 24);
        buttonRt = button.GetComponent<RectTransform>();
        buttonRt.anchorMin = new Vector2(0.5f, 0f); buttonRt.anchorMax = new Vector2(0.5f, 0f);
        buttonRt.pivot = new Vector2(0.5f, 0f);
        buttonRt.anchoredPosition = new Vector2(0f, 184f + 16f);
        label = button.GetComponentInChildren<Text>();
        button.onClick.AddListener(delegate { WaveManager.RequestDepart(); });
        button.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (button == null) return;
        bool show = WaveManager.WaitingDepart
            && !BriefingUI.IsOpen && !PauseMenu.IsOpen && !AugmentPickUI.IsOpen && !WorkshopUI.IsOpen
            && !AugmentListUI.ReadingOpen && !CookingMinigame.IsActive && !KitchenPanel.IsOpenStatic
            && !SpinoBetUI.IsOpen && !MerchantUI.IsOpen && !BranchRouteUI.IsOpen;
        if (button.gameObject.activeSelf != show) button.gameObject.SetActive(show);
        if (!show) return;

        // 살짝 숨쉬기 (놓치지 않게) - 0.9초 주기, 크기 1.0~1.04
        pulse += Time.unscaledDeltaTime;
        float k = 1f + 0.04f * (0.5f + 0.5f * Mathf.Sin(pulse * Mathf.PI * 2f / 0.9f));
        buttonRt.localScale = new Vector3(k, k, 1f);
    }
}
