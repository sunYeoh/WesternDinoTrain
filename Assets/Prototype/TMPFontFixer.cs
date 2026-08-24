using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using TMPro;

/// <summary>
/// [TMPFontFixer.cs] v1 (신규 파일) - TMP 폰트 자동 복구/통일
///
/// 배경: 씬의 HUD 텍스트(GoldText/WaveText 등 19개)는 TextMeshPro(TMP)로 만들어져
/// 있었고, 삭제된 NotoSans SDF 폰트를 참조하고 있었다. 그 결과 "LiberationSans SDF
/// Font Asset was not found" 경고와 함께 글자가 표시되지 않는 문제 발생.
///
/// 해결: 게임 시작 시 번들 폰트(Resources/Fonts/GameFont = Neo둥근모)로
/// TMP 폰트를 '실행 중에' 생성해서, 씬의 모든 TMP 텍스트에 자동 배정한다.
///  - 거대한 SDF 폰트 파일(구 NotoSans 133MB)이 다시 필요 없음 (글자를 그때그때 구움)
///  - HUD까지 Neo둥근모로 통일 (uGUI 쪽 UIFactory/KitchenEventManager와 동일 폰트)
///  - 씬 텍스트를 하나하나 수동으로 고칠 필요 없음
///
/// 사용법: 없음! 파일만 넣으면 게임 시작 시 스스로 동작한다.
///  - 런타임에 새로 만드는 TMP 텍스트는 TMPFontFixer.Apply(tmp) 한 줄로 배정
///    (DamagePopup에 적용됨)
///  - 에디터(플레이 전) 콘솔의 노란 경고 몇 개는 남을 수 있으나 무해 (플레이 중엔 사라짐)
/// VS 2017 (C# 7.3) 호환.
/// </summary>
public class TMPFontFixer : MonoBehaviour
{
    private static TMP_FontAsset koreanFont;   // 실행 중 생성한 TMP 폰트 (캐시)
    private static bool buildTried = false;    // 생성 시도 여부 (실패 반복 방지)
    private static TMPFontFixer instance;

    // ─────────────────────────────────────────────
    // 자동 부트스트랩
    // ─────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("TMPFontFixer");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<TMPFontFixer>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        SweepAll();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    // 씬 리로드([다시 굽는다]) 후에도 다시 배정
    private void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        SweepAll();
    }

    // ─────────────────────────────────────────────
    // TMP 폰트 확보: GameFont 기반 동적 생성 -> 실패 시 TMP 기본 폰트
    // ─────────────────────────────────────────────
    public static TMP_FontAsset GetFont()
    {
        if (koreanFont != null) return koreanFont;
        if (buildTried) return null;
        buildTried = true;

        // 1순위: 번들 폰트(Neo둥근모)로 동적 TMP 폰트 생성 (글자를 필요할 때 구움)
        Font src = Resources.Load<Font>("Fonts/GameFont");
        if (src != null)
        {
            koreanFont = TMP_FontAsset.CreateFontAsset(
                src, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            if (koreanFont != null)
            {
                Debug.Log("[TMPFontFixer] GameFont(Neo둥근모) 기반 TMP 폰트 생성 완료");
                return koreanFont;
            }
        }

        // 2순위: TMP 기본 폰트 (한글은 안 나오지만 숫자/영문은 표시됨)
        koreanFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (koreanFont != null)
            Debug.LogWarning("[TMPFontFixer] GameFont가 없어 TMP 기본 폰트로 대체 (한글 표시 불가)");
        else
            Debug.LogWarning("[TMPFontFixer] TMP 폰트를 만들 수 없음 - "
                + "Assets/Resources/Fonts/GameFont.ttf 존재 여부와 "
                + "Window > TextMeshPro > Import TMP Essential Resources 를 확인");
        return koreanFont;
    }

    /// <summary>런타임 생성 TMP 텍스트용: 통일 폰트 배정 (DamagePopup 등에서 호출)</summary>
    public static void Apply(TMP_Text t)
    {
        TMP_FontAsset f = GetFont();
        if (f != null && t != null) t.font = f;
    }

    // ─────────────────────────────────────────────
    // 씬의 모든 TMP 텍스트에 배정 (비활성 오브젝트 포함 - 숨겨진 패널까지)
    // ─────────────────────────────────────────────
    private static void SweepAll()
    {
        TMP_FontAsset f = GetFont();
        if (f == null) return;

        TMP_Text[] all = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null) all[i].font = f;

        Debug.Log("[TMPFontFixer] TMP 텍스트 " + all.Length + "개에 통일 폰트 배정");
    }
}
