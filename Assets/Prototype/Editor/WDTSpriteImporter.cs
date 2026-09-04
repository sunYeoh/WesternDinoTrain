using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// [WDTSpriteImporter.cs] (Editor 전용, 신규) - 고퀄 스프라이트 PNG 자동 임포트 설정 (2026-09-03)
///
/// Assets/Resources/Sprites/WDT/ 아래 PNG가 임포트될 때 자동으로:
///   Texture Type = Sprite (Single) / Pixels Per Unit = 파일별 값(64 또는 32) / Filter = Point(도트 선명) /
///   Compression = None / Mipmap 끔 / Pivot = 파일별 커스텀(게임 좌표와 1:1로 맞춘 값)
/// 을 잡아준다. 그래서 유저는 PNG를 폴더에 복사하기만 하면 된다.
///
/// 이 파일은 반드시 "Editor" 폴더 안에 있어야 한다 (예: Assets/Prototype/Editor/WDTSpriteImporter.cs).
/// PNG를 먼저 넣고 이 파일을 나중에 넣었다면: Project 창에서 Sprites/WDT 폴더 우클릭 -> Reimport 한 번.
/// </summary>
public class WDTSpriteImporter : AssetPostprocessor
{
    private struct Info
    {
        public float ppu, px, py;   // px, py = 정규화 피벗 (0~1, 왼쪽 아래 원점)
        public Info(float ppu, float px, float py) { this.ppu = ppu; this.px = px; this.py = py; }
    }

    // 파일 이름(확장자 제외) -> 픽셀/유닛 + 피벗. (렌더러 meta.json에서 생성)
    private static readonly Dictionary<string, Info> TABLE = new Dictionary<string, Info>
    {
            { "car0", new Info(64f, 0.5000f, 0.6082f) },
            { "car1", new Info(64f, 0.5000f, 0.6082f) },
            { "car2", new Info(64f, 0.5000f, 0.6082f) },
            { "chimney", new Info(64f, 0.5053f, 0.4980f) },
            { "e_necro", new Info(64f, 0.5000f, 0.5000f) },
            { "e_ptera", new Info(64f, 0.5200f, 0.5000f) },
            { "e_raptor", new Info(64f, 0.5000f, 0.5000f) },
            { "e_scorpion", new Info(64f, 0.5000f, 0.5000f) },
            { "e_steel", new Info(64f, 0.5000f, 0.5000f) },
            { "e_tortoise", new Info(64f, 0.4800f, 0.5000f) },
            { "ground_a", new Info(32f, 0.5000f, 0.0000f) },
            { "ground_b", new Info(32f, 0.5000f, 0.0000f) },
            { "harpoon", new Info(64f, 0.3500f, 0.3375f) },
            { "head", new Info(64f, 0.3200f, 0.6290f) },
            { "horizon", new Info(32f, 0.5000f, 0.0000f) },
            { "leverhandle", new Info(64f, 0.4571f, 0.0456f) },
            { "leverpost", new Info(64f, 0.5053f, 0.4947f) },
            { "rails", new Info(32f, 0.5000f, 0.0000f) },
            { "rock_elec", new Info(64f, 0.5053f, 0.5000f) },
            { "rock_fire", new Info(64f, 0.5053f, 0.5000f) },
            { "rock_herb", new Info(64f, 0.5053f, 0.5000f) },
            { "rock_meat", new Info(64f, 0.5053f, 0.5000f) },
            { "rock_oil", new Info(64f, 0.5053f, 0.5000f) },
            { "rock_shell", new Info(64f, 0.5053f, 0.5000f) },
            { "t_barrel", new Info(64f, 0.0938f, 0.5000f) },
            { "t_barrel2", new Info(64f, 0.0938f, 0.5000f) },
            { "t_base", new Info(64f, 0.5000f, 0.4583f) },
            { "t_dome_def", new Info(64f, 0.5000f, 0.4583f) },
            { "t_dome_elec", new Info(64f, 0.5000f, 0.4583f) },
            { "t_dome_fire", new Info(64f, 0.5000f, 0.4583f) },
            { "t_dome_ice", new Info(64f, 0.5000f, 0.4583f) },
            { "t_dome_phys", new Info(64f, 0.5000f, 0.4583f) },
            { "t_dome_poison", new Info(64f, 0.5000f, 0.4583f) },
            { "tail", new Info(64f, 0.0000f, 0.5000f) },
    };

    /// <summary>메뉴 WDT > 스프라이트 재임포트: PNG를 스크립트보다 먼저 넣었을 때 한 번 눌러주면 설정이 다시 잡힌다</summary>
    [MenuItem("WDT/스프라이트 재임포트 (Sprites/WDT)")]
    private static void ReimportAll()
    {
        AssetDatabase.ImportAsset("Assets/Resources/Sprites/WDT", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        Debug.Log("[WDTSpriteImporter] Sprites/WDT 재임포트 완료");
    }

    private void OnPreprocessTexture()
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.Contains("/Resources/Sprites/WDT/")) return;

        TextureImporter ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.maxTextureSize = 1024;

        string name = Path.GetFileNameWithoutExtension(path);
        Info info;
        if (TABLE.TryGetValue(name, out info))
        {
            ti.spritePixelsPerUnit = info.ppu;
            TextureImporterSettings st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.Custom;
            st.spritePivot = new Vector2(info.px, info.py);
            st.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(st);
        }
        else
        {
            ti.spritePixelsPerUnit = 64f;   // 표에 없는 새 파일: 기본 64px/유닛, 중앙 피벗
            Debug.LogWarning("[WDTSpriteImporter] 피벗 표에 없는 스프라이트: " + name + " (중앙 피벗으로 임포트)");
        }
    }
}
