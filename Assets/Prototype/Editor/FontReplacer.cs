using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// [FontReplacer.cs]
/// 씬 안의 모든 TextMeshPro 텍스트 폰트를 한번에 교체합니다.
///
/// 사용법:
/// 1. 이 파일을 Assets/Editor/ 폴더에 넣기
///    (Editor 폴더가 없으면 새로 만들기)
/// 2. Unity 상단 메뉴 → Tools → Replace All TMP Fonts 클릭
/// 3. 교체할 폰트 선택 후 Replace 클릭
/// </summary>
public class FontReplacer : EditorWindow
{
    private TMP_FontAsset newFont;

    [MenuItem("Tools/Replace All TMP Fonts")]
    public static void ShowWindow()
    {
        GetWindow<FontReplacer>("Font Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("씬 전체 TMP 폰트 교체", EditorStyles.boldLabel);
        GUILayout.Space(10);

        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "새 폰트 선택", newFont, typeof(TMP_FontAsset), false);

        GUILayout.Space(10);

        if (newFont == null)
        {
            EditorGUILayout.HelpBox("교체할 폰트를 선택하세요.", MessageType.Info);
            return;
        }

        if (GUILayout.Button("씬 전체 폰트 교체", GUILayout.Height(40)))
            ReplaceAllFonts();
    }

    private void ReplaceAllFonts()
    {
        int count = 0;

        // 1. 씬 오브젝트 교체
        TextMeshProUGUI[] sceneTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (TextMeshProUGUI tmp in sceneTexts)
        {
            if (tmp.gameObject.scene.isLoaded)
            {
                Undo.RecordObject(tmp, "Replace Font");
                tmp.font = newFont;
                EditorUtility.SetDirty(tmp);
                count++;
            }
        }

        // 2. 프리팹 에셋 교체
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            TextMeshProUGUI[] prefabTexts = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (prefabTexts.Length == 0) continue;

            foreach (TextMeshProUGUI tmp in prefabTexts)
            {
                Undo.RecordObject(tmp, "Replace Font");
                tmp.font = newFont;
                EditorUtility.SetDirty(tmp);
                count++;
            }

            PrefabUtility.SavePrefabAsset(prefab);
        }

        // 씬 저장
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[FontReplacer] 완료! " + count + "개 텍스트 폰트 교체됨 (씬 + 프리팹)");
        EditorUtility.DisplayDialog("완료",
            count + "개의 텍스트 폰트가 교체되었습니다!\n(씬 오브젝트 + 프리팹 포함)", "확인");
    }
}
