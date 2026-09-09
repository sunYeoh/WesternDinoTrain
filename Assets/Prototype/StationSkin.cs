using UnityEngine;

/// <summary>
/// [StationSkin.cs] v1 (신규 파일) - 씬 조리대 3대에 픽셀 PNG 입히기 (2026-09-09, v9.5 조리 목업 v1 컨펌)
///
/// 씬의 CookingStation 오브젝트(그릴/볶음팬/냄비)는 아직 placeholder 그림이다. 이 파일은 EnemySkin 과 같은 방식으로
/// 게임 시작 뒤 0.25초마다 CookingStation 을 훑어 아직 스킨이 없는 조리대에 PNG 자식("Skin")을 붙이고 기존 렌더러는 끈다.
///   Grilling -> st_grill.png (무쇠 그릴 + 숯불 + 고기 2점, 28x24)
///   Saute    -> st_pan.png   (화구 + 검은 볶음팬 + 노란 볶음, 30x22)
///   Boiling  -> st_pot.png   (화구 + 구리 솥 + 황동 뚜껑, 24x26)
/// 그림 피벗은 바닥 가운데(임포터 표) - 조리대 위치(GameBalance.StationXs / StationY)가 그림의 발밑이 된다.
/// 크기: 화면에서 1유닛 = 32px 도트 그대로 (기차/셰프와 같은 밀도). TrainDeck 이 조리대 루트를 StationScale(0.55)로 줄이므로
///       자식 스케일로 되돌린다 (루트 스케일이 나중에 바뀌어도 매 프레임 따라간다). 발밑 그림자 타원 포함.
/// 정렬 1 (데크 -6~-4 위, 적 5 / 셰프 6 아래). CookingStation 로직·상호작용 범위·[E] 프롬프트는 건드리지 않는다.
///
/// 사용법: 없음! 파일만 넣으면 게임 시작 시 스스로 생성된다. (SpriteBank.cs + st_*.png + 임포터 v4.1 표 필요)
/// 끄기: ENABLED = false
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class StationSkin : MonoBehaviour
{
    public static bool ENABLED = true;

    private const int SORT_ORDER = 1;               // 데크(-6~-4) 위, 적(5)/셰프(6) 아래
    private const float SHADOW_ALPHA = 0.35f;
    private const float SHADOW_WIDEN = 1.15f;       // 그림자 폭 = 그림 폭 x 1.15

    /// <summary>이 조리대에 입힌 스프라이트 (다른 코드가 강조/깜빡임에 쓰고 싶을 때)</summary>
    public SpriteRenderer skin;

    private Transform skinRoot;                     // 스케일 보정 자식 ("Skin")
    private float lastRootScale = -1f;

    private static Sprite shadowSprite;
    private static Scanner scanner;

    // ─────────────────────────────────────────────
    // 스캐너 (싱글턴) - 스킨 없는 조리대를 찾아 입힌다
    // ─────────────────────────────────────────────
    private class Scanner : MonoBehaviour
    {
        private const float SCAN_INTERVAL = 0.25f;
        private float timer;
        private bool loggedMissing;

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < SCAN_INTERVAL) return;
            timer = 0f;
            CookingStation[] stations = FindObjectsByType<CookingStation>(FindObjectsSortMode.None);
            for (int i = 0; i < stations.Length; i++)
            {
                CookingStation s = stations[i];
                if (s == null || s.GetComponent<StationSkin>() != null) continue;
                if (!Apply(s) && !loggedMissing)
                {
                    loggedMissing = true;
                    Debug.LogWarning("[StationSkin] st_*.png 를 찾지 못했다 - 조리대 기존 그림 유지 (Resources/Sprites/WDT/st_grill.png 등 확인)");
                }
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (scanner != null || !ENABLED) return;
        if (!SpriteBank.Has("st_grill")) { Debug.Log("[StationSkin] st_grill.png 없음 - 조리대 스킨 생략"); return; }
        GameObject go = new GameObject("StationSkinScanner");
        DontDestroyOnLoad(go);
        scanner = go.AddComponent<Scanner>();
        Debug.Log("[StationSkin] 조리대 PNG 스킨 스캐너 준비");
    }

    private static string PngFor(CookingStation.StationType type)
    {
        if (type == CookingStation.StationType.Saute) return "st_pan";
        if (type == CookingStation.StationType.Boiling) return "st_pot";
        return "st_grill";
    }

    /// <summary>조리대 하나에 PNG 스킨 적용. 그림이 없으면 false</summary>
    private static bool Apply(CookingStation station)
    {
        Sprite sprite = SpriteBank.Get(PngFor(station.stationType));
        if (sprite == null) return false;

        // placeholder 렌더러 끄기 - [E] 프롬프트 아래 것은 남긴다 (프롬프트가 스프라이트로 만들어졌을 수도 있으므로)
        SpriteRenderer[] old = station.GetComponentsInChildren<SpriteRenderer>(true);
        int hidden = 0;
        for (int i = 0; i < old.Length; i++)
        {
            if (station.interactPrompt != null && old[i].transform.IsChildOf(station.interactPrompt.transform)) continue;
            old[i].enabled = false; hidden++;
        }

        GameObject root = new GameObject("Skin");
        root.transform.SetParent(station.transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;

        // 발밑 그림자 (그림 폭에 맞춘 타원, 살짝 위로 올려 바닥선에 걸친다)
        float widthUnits = sprite.rect.width / sprite.pixelsPerUnit;
        GameObject sh = new GameObject("StationShadow");
        sh.transform.SetParent(root.transform, false);
        sh.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        sh.transform.localScale = new Vector3(widthUnits * SHADOW_WIDEN / 0.75f, 1f, 1f);
        SpriteRenderer ssr = sh.AddComponent<SpriteRenderer>();
        ssr.sprite = GetShadowSprite();
        ssr.color = new Color(0f, 0f, 0f, SHADOW_ALPHA);
        ssr.sortingOrder = SORT_ORDER - 1;

        GameObject go = new GameObject("StationSprite");
        go.transform.SetParent(root.transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = SORT_ORDER;

        StationSkin marker = station.gameObject.AddComponent<StationSkin>();
        marker.skin = sr;
        marker.skinRoot = root.transform;
        marker.FitScale();
        Debug.Log("[StationSkin] " + station.stationType + " -> " + sprite.name + " (기존 렌더러 " + hidden + "개 숨김)");
        return true;
    }

    /// <summary>루트 스케일(StationScale)을 상쇄해 그림을 항상 1유닛 = 32px 로 보이게 한다</summary>
    private void FitScale()
    {
        float rootScale = Mathf.Abs(transform.localScale.x);
        if (rootScale < 0.01f) rootScale = 1f;
        if (Mathf.Abs(rootScale - lastRootScale) < 0.0001f) return;
        lastRootScale = rootScale;
        if (skinRoot != null) skinRoot.localScale = new Vector3(1f / rootScale, 1f / rootScale, 1f);
    }

    private void LateUpdate()
    {
        // TrainDeck 이 Start 에서 조리대 스케일을 바꾸므로 (스캐너보다 늦을 수 있다) 매 프레임 확인
        FitScale();
    }

    /// <summary>발밑 그림자: 24x8 타원 텍스처를 코드로 만든다 (32px/유닛 = 0.75 x 0.25 유닛)</summary>
    private static Sprite GetShadowSprite()
    {
        if (shadowSprite != null) return shadowSprite;
        const int w = 24, h = 8;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color32[] px = new Color32[w * h];
        float cx = (w - 1) * 0.5f, cy = (h - 1) * 0.5f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x - cx) / (w * 0.5f), dy = (y - cy) / (h * 0.5f);
                bool inside = dx * dx + dy * dy <= 1f;
                px[y * w + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        tex.SetPixels32(px);
        tex.Apply(false, false);
        shadowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        shadowSprite.name = "station_shadow";
        return shadowSprite;
    }
}
