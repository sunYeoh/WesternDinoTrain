using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// [WDTCleanup.cs] v1 (신규, v9.26 2026-10-16 마감 정리) - 메뉴 WDT > 마감 정리 (안 쓰는 파일 지우기)
/// 마감 빌드 전에 저장소에 남은 옛 파일을 한 번에 지운다. 목록은 아래 TARGETS 에 박혀 있다
/// (2026-10-16 저장소 v9.25 를 씬·프리팹·코드와 대조해서 고른 것 - 어디서도 안 쓴다):
///   - 옛 스크립트 6 (v9.26 에서 내용을 비웠다 - 지우기 전에도 컴파일·실행에 영향 없음)
///   - 씬이 안 쓰는 프리팹 4 / 새 배경(ground_ae · rails_ae)이 대신하는 옛 배경 그림 4 / v9.26 에서 지운 예고 화살표의 소리 1
/// .meta 는 AssetDatabase.DeleteAsset 이 같이 지운다. 이미 없는 것은 건너뛴다 (두 번 눌러도 된다).
/// 지운 뒤: Play 로 한 번 확인 -> 저장소에 커밋. 이 파일은 Editor 폴더라 빌드에 안 들어간다
/// VS 2017 (C# 7.3) 호환
/// </summary>
public static class WDTCleanup
{
    // 지울 것 (프로젝트 폴더 기준 경로). 폴더면 안에 든 것까지 같이 지워진다
    private static readonly string[] TARGETS =
    {
        // 옛 스크립트 (씬·프리팹·코드 어디서도 안 쓴다)
        "Assets/Prototype/CookingUiManager.cs",
        "Assets/Prototype/TurretPart.cs",
        "Assets/Prototype/TestInput.cs",
        "Assets/Prototype/RecipeDBTest.cs",
        "Assets/Prototype/testfolder",                    // TurretSystemTest.cs 하나만 든 폴더
        "Assets/Prototype/Scripts/Bullet.cs",             // 안 쓰는 Bullet.prefab 에만 붙어 있던 것 (탄은 AttackVFX 가 그린다)
        // 씬이 안 쓰는 프리팹
        "Assets/Prototype/Enemy_Dummy.prefab",
        "Assets/Prototype/Prefabs/Bullet.prefab",
        "Assets/Prototype/Prefabs/PartButton.prefab",
        "Assets/Prototype/Prefabs/WeaponSlotItem.prefab",
        // 옛 배경 그림 (Resources 안이라 빌드 용량에 들어간다)
        "Assets/Resources/Sprites/WDT/ground_a.png",
        "Assets/Resources/Sprites/WDT/ground_b.png",
        "Assets/Resources/Sprites/WDT/horizon.png",
        "Assets/Resources/Sprites/WDT/rails.png",
        // 지운 예고 화살표의 소리
        "Assets/Resources/Sounds/sfx_cue_approach.wav",
    };

    // 위를 지우고 나서 비었으면 같이 지울 폴더
    private static readonly string[] EMPTY_FOLDERS = { "Assets/Prototype/Scripts" };

    [MenuItem("WDT/마감 정리 (안 쓰는 파일 지우기)")]
    private static void RunCleanup()
    {
        // 1. 아직 남아 있는 것만 추린다
        List<string> found = new List<string>();
        foreach (string path in TARGETS)
            if (File.Exists(path) || Directory.Exists(path)) found.Add(path);

        if (found.Count == 0)
        {
            EditorUtility.DisplayDialog("마감 정리", "지울 게 없어 - 이미 정리됐어.", "확인");
            return;
        }

        // 2. 목록을 보여 주고 한 번 묻는다
        string list = string.Join("\n", found.ToArray());
        bool go = EditorUtility.DisplayDialog("마감 정리",
            "아래 " + found.Count + "개를 지울게 (.meta 도 같이). 씬·코드 어디서도 안 쓰는 것들이야.\n\n" + list,
            "지우기", "취소");
        if (!go) return;

        // 3. 지운다 (한 번에 묶어서 - 스크립트를 지우면 컴파일이 다시 도는데, 그걸 한 번으로)
        List<string> failed = new List<string>();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string path in found)
                if (!AssetDatabase.DeleteAsset(path)) failed.Add(path);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        // 4. 비게 된 폴더
        foreach (string folder in EMPTY_FOLDERS)
            if (Directory.Exists(folder) && Directory.GetFileSystemEntries(folder).Length == 0)
                AssetDatabase.DeleteAsset(folder);
        AssetDatabase.Refresh();

        int deleted = found.Count - failed.Count;
        Debug.Log("[WDTCleanup] 지움 " + deleted + "개" + (failed.Count > 0 ? " / 못 지움: " + string.Join(", ", failed.ToArray()) : ""));
        EditorUtility.DisplayDialog("마감 정리",
            failed.Count == 0
                ? deleted + "개 지웠어. Play 로 한 번 확인하고 커밋하면 끝."
                : "못 지운 게 있어 (콘솔 참고):\n" + string.Join("\n", failed.ToArray()),
            "확인");
    }
}
