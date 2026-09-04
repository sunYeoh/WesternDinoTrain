using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [SpriteBank.cs] v1 (신규 파일) - 고퀄 스프라이트 PNG 로더 (2026-09-03, 목업 v7d 컨펌)
///
/// Assets/Resources/Sprites/WDT/ 폴더의 PNG를 이름으로 꺼내 쓴다. 한 번 읽으면 캐시.
///   예) SpriteBank.Get("head") -> Resources/Sprites/WDT/head.png
/// 파일이 없으면 null을 돌려주고, 각 사용처(TrainDeck/TurretSlot/WaveManager/EngineCab/ParallaxBackground)는
/// null이면 예전처럼 코드 도트(PixelPainter)로 그린다 = PNG 팩이 없어도 게임은 돈다.
///
/// 임포트 설정(픽셀/유닛, 피벗, Point 필터)은 Editor/WDTSpriteImporter.cs가 자동으로 잡는다.
/// 사용법: 없음! 파일만 넣으면 된다.
/// VS 2017 (C# 7.3) 호환
/// </summary>
public static class SpriteBank
{
    /// <summary>Resources 아래 경로 (Resources.Load 기준이라 "Assets/Resources/"는 뺀다)</summary>
    public const string ROOT = "Sprites/WDT/";

    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    private static bool loggedMissing = false;
    private static bool loggedBadImport = false;

    /// <summary>이름으로 스프라이트를 꺼낸다. 없으면 null (호출부가 코드 도트로 폴백)</summary>
    public static Sprite Get(string name)
    {
        Sprite s;
        if (cache.TryGetValue(name, out s)) return s;

        s = Resources.Load<Sprite>(ROOT + name);
        cache[name] = s;   // null도 캐시 (매 프레임 Resources.Load 반복 방지)

        // 임포트 설정이 안 잡힌 채(유니티 기본 100px/유닛, 중앙 피벗) 들어오면 크기/위치가 다 틀어진다 - 한 번만 경고
        if (s != null && !loggedBadImport && Mathf.Abs(s.pixelsPerUnit - 100f) < 0.5f)
        {
            loggedBadImport = true;
            Debug.LogWarning("[SpriteBank] '" + name + "' 의 Pixels Per Unit이 100 = 임포트 설정 미적용. 메뉴 WDT > 스프라이트 재임포트 를 한 번 실행해라");
        }

        if (s == null && !loggedMissing)
        {
            loggedMissing = true;
            Debug.Log("[SpriteBank] '" + ROOT + name + "' 없음 - 코드 도트로 폴백 (PNG 팩을 Assets/Resources/Sprites/WDT/ 에 넣으면 고퀄로 바뀐다)");
        }
        return s;
    }

    /// <summary>해당 이름의 PNG가 있는가</summary>
    public static bool Has(string name)
    {
        return Get(name) != null;
    }

    /// <summary>
    /// 스프라이트 렌더러 1개를 붙인다. PNG가 있으면 그것을, 없으면 fallback(코드 도트)을 쓴다.
    /// 피벗/픽셀 단위는 임포터가 잡아두므로 위치만 넘기면 된다.
    /// </summary>
    public static SpriteRenderer Attach(Transform parent, string objName, string spriteName, Sprite fallback,
        Vector3 localPos, int order)
    {
        Sprite s = Get(spriteName);
        if (s == null) s = fallback;
        return PixelPainter.Attach(parent, objName, s, localPos, order);
    }
}
