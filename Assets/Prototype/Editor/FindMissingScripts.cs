using UnityEngine;
using UnityEditor;

/// <summary>
/// [FindMissingScripts.cs] - 에디터 전용 도구
/// 씬에서 Missing Script(지워진 스크립트 껍데기)가 붙은 오브젝트를 찾고 제거한다.
///
/// 설치 위치: 반드시 Assets/Editor/ 폴더 안에 넣을 것 (폴더 없으면 만들기)
/// 사용법: Unity 상단 메뉴 -> Tools -> Missing Script 찾기 / 자동 제거
/// 빌드에는 포함되지 않는다 (Editor 폴더는 빌드 제외)
/// </summary>
public static class FindMissingScripts
{
    /// <summary>씬 전체에서 Missing Script가 붙은 오브젝트를 찾아 Console에 목록 출력</summary>
    [MenuItem("Tools/Missing Script 찾기")]
    private static void FindAll()
    {
        int foundCount = 0;

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (GameObject go in allObjects)
        {
            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    foundCount++;
                    // 클릭하면 해당 오브젝트가 하이어라키에서 선택되도록 오브젝트를 함께 넘긴다
                    Debug.LogWarning("[MissingScript] '" + GetPath(go) + "' 에 Missing Script 있음", go);
                    break;
                }
            }
        }

        if (foundCount == 0)
            Debug.Log("[MissingScript] 씬에 Missing Script 없음 - 깨끗함!");
        else
            Debug.Log("[MissingScript] 총 " + foundCount + "개 오브젝트에서 발견. Tools > Missing Script 자동 제거 로 정리 가능");
    }

    /// <summary>씬 전체의 Missing Script 컴포넌트를 자동으로 제거</summary>
    [MenuItem("Tools/Missing Script 자동 제거")]
    private static void RemoveAll()
    {
        int removedCount = 0;

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (GameObject go in allObjects)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removed > 0)
            {
                removedCount += removed;
                Debug.Log("[MissingScript] '" + GetPath(go) + "' 에서 " + removed + "개 제거", go);
            }
        }

        if (removedCount == 0)
            Debug.Log("[MissingScript] 제거할 Missing Script 없음");
        else
            Debug.Log("[MissingScript] 총 " + removedCount + "개 제거 완료. 씬 저장(Ctrl+S) 필수!");
    }

    /// <summary>하이어라키 경로 문자열 (부모/자식/오브젝트 형태)</summary>
    private static string GetPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }
}
