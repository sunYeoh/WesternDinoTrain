using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [TextFitGuard.cs] v1 (신규, v9.10.1 2026-09-21) - UI 글자가 칸 밖으로 튀어나오는 것을 자동으로 잡는다
///
/// 유저 소감: "증강이나 다른 창에서 글씨가 옆으로 튀어나오는 경우가 아직 너무 많다".
/// 원인: UIFactory.CreateText / KitchenEventManager.MakeText 가 만드는 Text 는 기본이 Overflow(줄바꿈·축소 없음)라
/// 문구가 조금만 길어도 칸을 넘는다. 창마다 고치는 대신 한 곳에서 0.25초마다 화면의 모든 Text 를 훑어 맞춘다.
///
/// 규칙 (칸 폭 40px 미만인 글자는 점 기준으로 일부러 넘치게 둔 라벨이라 손대지 않는다):
///   1) 한 줄 글자(Overflow)가 칸 폭을 넘으면
///        - 칸 높이가 두 줄 이상 들어가고 문장에 띄어쓰기가 있으면 -> 줄바꿈(Wrap)으로 바꾼다
///        - 아니면 -> 폰트를 1씩 줄인다 (원래 크기의 60% 또는 11px 까지)
///   2) 줄바꿈 글자(Wrap)가 칸 높이(24px 이상)를 넘으면 -> 폰트를 줄인다 (같은 하한)
///   3) 내용이 짧아지면 원래 크기로 되돌린다 (매 훑기마다 원래 크기에서 다시 판정)
///   BestFit 을 이미 켠 Text 는 건드리지 않는다. GameBalance.TextFitGuardOn = false 면 아무것도 안 한다.
/// 사용법: 없음! 파일만 넣으면 자동 생성 (DontDestroyOnLoad).
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class TextFitGuard : MonoBehaviour
{
    private const float SCAN_SEC = 0.25f;
    private const int MIN_FONT = 11;
    private const float MIN_WIDTH = 40f;     // 이보다 좁은 칸은 "점 기준 라벨" - 손대지 않는다
    private const float MIN_HEIGHT = 24f;    // 이보다 낮은 칸의 세로 넘침은 의도된 것으로 본다

    private static TextFitGuard instance;
    private float nextScan = 0f;
    private readonly Dictionary<Text, int> originalSize = new Dictionary<Text, int>();
    private readonly List<Text> gone = new List<Text>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("TextFitGuard");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<TextFitGuard>();
    }

    private void Update()
    {
        if (!GameBalance.TextFitGuardOn) return;
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + SCAN_SEC;

        Text[] all = FindObjectsByType<Text>(FindObjectsSortMode.None);   // 활성 오브젝트만
        for (int i = 0; i < all.Length; i++) Fit(all[i]);

        // 사라진 Text 기록 정리 (창이 닫히며 파괴된 것들)
        gone.Clear();
        foreach (KeyValuePair<Text, int> kv in originalSize)
            if (kv.Key == null) gone.Add(kv.Key);
        for (int i = 0; i < gone.Count; i++) originalSize.Remove(gone[i]);
    }

    /// <summary>Text 하나를 칸에 맞춘다</summary>
    private void Fit(Text t)
    {
        if (t == null || !t.isActiveAndEnabled || t.resizeTextForBestFit) return;
        if (string.IsNullOrEmpty(t.text) || t.font == null) return;

        RectTransform rt = t.rectTransform;
        float w = rt.rect.width, h = rt.rect.height;

        int orig;
        if (!originalSize.TryGetValue(t, out orig)) { orig = t.fontSize; originalSize[t] = orig; }
        int floor = Mathf.Max(MIN_FONT, Mathf.RoundToInt(orig * 0.6f));

        // 3) 내용이 바뀌었을 수 있으니 원래 크기에서 다시 판정 (같은 값이면 Unity 가 다시 그리지 않는다)
        if (t.fontSize != orig) t.fontSize = orig;
        if (w < MIN_WIDTH) return;

        bool wrap = t.horizontalOverflow == HorizontalWrapMode.Wrap;
        if (!wrap)
        {
            float pw = t.preferredWidth;
            if (pw <= w + 1f) return;

            // 1) 긴 문장 + 두 줄 이상 들어갈 높이 -> 줄바꿈으로. 아니면 폭에 맞게 축소
            if (h >= orig * 2.2f && pw > w * 1.25f && t.text.IndexOf(' ') >= 0)
            {
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                wrap = true;
            }
            else
            {
                int guard = 0;
                while (t.fontSize > floor && t.preferredWidth > w + 1f && guard++ < 40) t.fontSize -= 1;
                return;
            }
        }

        // 2) 줄바꿈 글자의 세로 넘침
        if (wrap && h >= MIN_HEIGHT)
        {
            int guard = 0;
            while (t.fontSize > floor && t.preferredHeight > h + 1f && guard++ < 40) t.fontSize -= 1;
        }
    }
}
