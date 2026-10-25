using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// [WDTAudioImporter.cs] v1 (신규, Editor 전용, v9.33 2026-10-25 소리 팩) - Assets/Resources/Sounds/ 의 새 소리를 들일 때 설정을 자동으로 잡는다
///   새 배경음 8곡 (bgm_map_* · bgm_boss_* · bgm_maintenance) = Streaming (디스크에서 흘려 읽는다 - 지역이 바뀌는 순간 50초짜리 곡을 통째로 푸느라 멈칫하지 않게) + Vorbis 품질 70% + 백그라운드 로드
///   새 효과음 (이름이 _v01 ~ _v09 로 끝나는 것) = Decompress On Load + PCM (짧고 자주 나서 미리 풀어 둔다). 파일이 이미 모노라 Force To Mono 는 끈다
///     (켜면 섞는 김에 정규화(Normalize)가 걸려 파일끼리 맞춰 둔 크기 위계가 무너질 수 있다)
///   둘 다 샘플레이트는 그대로 (48kHz). 예전 파일(bgm_main · bgm_climax · sfx_whistle_long 같은 맨 이름)은 건드리지 않는다 - 클라이맥스 층의 박 맞춤이 지금 설정에서 검증됐다
/// 설정 버전(SETTINGS_VERSION)이 바뀌면 에디터가 스크립트를 다시 읽은 뒤 Sounds 폴더를 스스로 한 번 재임포트한다 (소리를 먼저 넣고 이 스크립트가 나중에 컴파일돼도 설정이 잡힌다)
/// 메뉴 WDT > 소리 재임포트 (Sounds) 로 언제든 다시. 이 파일은 Editor 폴더 안 (Assets/Prototype/Editor/) - 빌드에 안 들어간다
/// VS 2017 (C# 7.3) 호환
/// </summary>
public class WDTAudioImporter : AssetPostprocessor
{
    private const string SETTINGS_VERSION = "1";
    private const string FOLDER = "Assets/Resources/Sounds";
    private static readonly Regex VARIANT = new Regex("_v[0-9][0-9]$");
    private static readonly string[] NEW_MUSIC = { "bgm_map_copper", "bgm_map_tesla", "bgm_map_cobalt", "bgm_boss_rust", "bgm_boss_thunder", "bgm_boss_hibernator", "bgm_boss_original", "bgm_maintenance" };

    [InitializeOnLoadMethod]
    private static void ReimportWhenSettingsChanged()
    {
        string key = "WDT_AudioSettings_" + Application.dataPath.Replace('/', '_').Replace('\\', '_').Replace(':', '_');
        if (EditorPrefs.GetString(key, "") == SETTINGS_VERSION) return;
        EditorApplication.delayCall += delegate
        {
            if (EditorPrefs.GetString(key, "") == SETTINGS_VERSION) return;
            EditorPrefs.SetString(key, SETTINGS_VERSION);
            if (!AssetDatabase.IsValidFolder(FOLDER)) return;
            ReimportAll();
            Debug.Log("[WDTAudioImporter] 소리 설정 v" + SETTINGS_VERSION + " - Sounds 를 자동으로 다시 임포트했다");
        };
    }

    /// <summary>메뉴 WDT > 소리 재임포트: 소리를 이 스크립트보다 먼저 넣었는데 설정이 안 잡혔을 때 한 번</summary>
    [MenuItem("WDT/소리 재임포트 (Sounds)")]
    private static void ReimportAll()
    {
        AssetDatabase.ImportAsset(FOLDER, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        Debug.Log("[WDTAudioImporter] Sounds 재임포트 완료");
    }

    private void OnPreprocessAudio()
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.Contains("/Resources/Sounds/")) return;
        AudioImporter ai = assetImporter as AudioImporter;
        if (ai == null) return;

        string name = Path.GetFileNameWithoutExtension(path);
        bool music = System.Array.IndexOf(NEW_MUSIC, name) >= 0;
        bool sfx = !music && name.StartsWith("sfx_") && VARIANT.IsMatch(name);
        if (!music && !sfx) return;   // 예전 파일은 그대로

        AudioImporterSampleSettings s = ai.defaultSampleSettings;
        s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        if (music)
        {
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.7f;
            ai.forceToMono = false;
            ai.loadInBackground = true;
        }
        else
        {
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.PCM;
            ai.forceToMono = false;   // 이미 모노 (tools/audio_v933.py 가 확인) - 정규화로 크기가 바뀌지 않게
            ai.loadInBackground = false;
        }
        ai.defaultSampleSettings = s;
    }
}
