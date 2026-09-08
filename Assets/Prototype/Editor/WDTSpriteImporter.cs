using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// [WDTSpriteImporter.cs] v4 (Editor 전용) - 스프라이트 PNG 자동 임포트 설정 (2026-09-07, v9 픽셀 팩 + UI 스킨)
///
/// Assets/Resources/Sprites/WDT/ 아래 PNG가 임포트될 때 자동으로:
///   Texture Type = Sprite (Single) / Pixels Per Unit = 파일별 값 / Filter = Point(도트 선명) /
///   Compression = None / Mipmap 끔 / Pivot = 파일별 커스텀(게임 좌표와 1:1로 맞춘 값)
/// 을 잡아준다. 그래서 유저는 PNG를 폴더에 복사하기만 하면 된다.
///
/// v4: UI 스킨 ui_*.png 규칙 - PPU 100(캔버스 1유닛 = 1px), Point, FullRect, 9-슬라이스 테두리(UI_BORDER 표). 타일/슬라이스는 UISkin.cs 가 Image.type 으로 정함
/// v3: v9 픽셀 팩 - 전 스프라이트 32px/유닛(표 재생성: 기차/포탑/적/바위/작살/레버/굴뚝 + ground_ae/rails_ae/dust_0~3)
/// v2: 유저 제작 셰프 도트 hero_*.png (32x32, 발이 아래에서 두 번째 줄) 규칙 - HERO_PPU 로 크기 조절
///     (32 = 기차와 같은 도트 밀도(확정), 1080p에서 약 60px 키 / 24 = 1.33배 크게 / 21 = 1.5배 크게)
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

    // UI 스킨 9-슬라이스 테두리 (px). 0 = 테두리 없음 (타일/단순)
    private static readonly Dictionary<string, float> UI_BORDER = new Dictionary<string, float>
    {
            { "ui_button", 16f },
            { "ui_gauge_bg", 16f },
            { "ui_gauge_fill", 4f },
            { "ui_gauge_round", 0f },
            { "ui_hazard", 0f },
            { "ui_nameplate", 12f },
            { "ui_pipe", 28f },
            { "ui_plate", 0f },
            { "ui_ring", 16f },
            { "ui_valve", 0f },
            { "ui_vent", 0f },
    };

    private const float HERO_PPU = 32f;                 // 셰프(hero_*) 크기: 낮출수록 화면에서 커진다 (32 = 기차와 같은 밀도(확정), 24 / 21 = 크게)
    private const float HERO_PIVOT_Y = 1f / 32f;        // 발바닥 = 아래에서 두 번째 픽셀 줄 (유저 도트 기준)

    // 파일 이름(확장자 제외) -> 픽셀/유닛 + 피벗. (렌더러 meta.json에서 생성)
    private static readonly Dictionary<string, Info> TABLE = new Dictionary<string, Info>
    {
            { "car0", new Info(32f, 0.5000f, 0.6082f) },
            { "car1", new Info(32f, 0.5000f, 0.6082f) },
            { "car2", new Info(32f, 0.5000f, 0.6082f) },
            { "chimney", new Info(32f, 0.5053f, 0.4980f) },
            { "dust_0", new Info(32f, 0.5000f, 0.5000f) },
            { "dust_1", new Info(32f, 0.5000f, 0.5000f) },
            { "dust_2", new Info(32f, 0.5000f, 0.5000f) },
            { "dust_3", new Info(32f, 0.5000f, 0.5000f) },
            { "e_necro", new Info(32f, 0.5000f, 0.5000f) },
            { "e_ptera", new Info(32f, 0.5200f, 0.5000f) },
            { "e_raptor", new Info(32f, 0.5000f, 0.5000f) },
            { "e_scorpion", new Info(32f, 0.5000f, 0.5000f) },
            { "e_steel", new Info(32f, 0.5000f, 0.5000f) },
            { "e_tortoise", new Info(32f, 0.4800f, 0.5000f) },
            { "gangway", new Info(32f, 0.5053f, 0.4947f) },
            { "ground_ae", new Info(32f, 0.5000f, 0.5000f) },
            { "harpoon", new Info(32f, 0.3500f, 0.3375f) },
            { "head", new Info(32f, 0.3200f, 0.6290f) },
            { "leverhandle", new Info(32f, 0.4571f, 0.0621f) },
            { "leverpost", new Info(32f, 0.5053f, 0.4947f) },
            { "rails_ae", new Info(32f, 0.5000f, 0.5000f) },
            { "rock_armor", new Info(32f, 0.5053f, 0.5000f) },
            { "rock_elec", new Info(32f, 0.5053f, 0.5000f) },
            { "rock_fire", new Info(32f, 0.5053f, 0.5000f) },
            { "rock_ice", new Info(32f, 0.5053f, 0.5000f) },
            { "rock_meat", new Info(32f, 0.5053f, 0.5000f) },
            { "rock_poison", new Info(32f, 0.5053f, 0.5000f) },
            { "t_barrel", new Info(32f, 0.0938f, 0.5000f) },
            { "t_barrel2", new Info(32f, 0.0938f, 0.5000f) },
            { "t_base", new Info(32f, 0.5000f, 0.4583f) },
            { "t_dome_def", new Info(32f, 0.5000f, 0.4583f) },
            { "t_dome_elec", new Info(32f, 0.5000f, 0.4583f) },
            { "t_dome_fire", new Info(32f, 0.5000f, 0.4583f) },
            { "t_dome_ice", new Info(32f, 0.5000f, 0.4583f) },
            { "t_dome_phys", new Info(32f, 0.5000f, 0.4583f) },
            { "t_dome_poison", new Info(32f, 0.5000f, 0.4583f) },
            { "tail", new Info(32f, 0.0000f, 0.5000f) },
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
        if (name.StartsWith("ui_"))
        {
            // UI 스킨: 캔버스 픽셀 1:1 (PPU 100 = uGUI 기본), 중앙 피벗, 9-슬라이스 테두리
            ti.spritePixelsPerUnit = 100f;
            float bd;
            if (!UI_BORDER.TryGetValue(name, out bd)) bd = 0f;
            TextureImporterSettings us = new TextureImporterSettings();
            ti.ReadTextureSettings(us);
            us.spriteAlignment = (int)SpriteAlignment.Center;
            us.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(us);
            ti.spriteBorder = new Vector4(bd, bd, bd, bd);
            return;
        }
        if (name.StartsWith("hero_"))
        {
            // 유저 제작 셰프 도트: 표 대신 접두어 규칙 (새 프레임을 추가해도 표 수정 불필요)
            info = new Info(HERO_PPU, 0.5f, HERO_PIVOT_Y);
            ApplyPivot(ti, info);
        }
        else if (TABLE.TryGetValue(name, out info))
        {
            ApplyPivot(ti, info);
        }
        else
        {
            ti.spritePixelsPerUnit = 32f;   // 표에 없는 새 파일: 기본 32px/유닛, 중앙 피벗
            Debug.LogWarning("[WDTSpriteImporter] 피벗 표에 없는 스프라이트: " + name + " (중앙 피벗으로 임포트)");
        }
    }

    private static void ApplyPivot(TextureImporter ti, Info info)
    {
        ti.spritePixelsPerUnit = info.ppu;
        TextureImporterSettings st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.Custom;
        st.spritePivot = new Vector2(info.px, info.py);
        st.spriteMeshType = SpriteMeshType.FullRect;
        ti.SetTextureSettings(st);
    }
}
