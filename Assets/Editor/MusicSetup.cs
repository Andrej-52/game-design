using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Towelman > Add Background Music
// Adds a looping music player to the open scene using "Speed race" from Assets/Audio.
// It follows the Music / Master / Mute settings from the settings menu (through MusicVolume).
public static class MusicSetup
{
    const string ClipPath = "Assets/Audio/Speed race by Spinopel.mp3";

    [MenuItem("Tools/Towelman/Add Background Music")]
    static void Add()
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
        if (clip == null)
        {
            Debug.LogError("Could not find " + ClipPath + ". Did the file get renamed?");
            return;
        }

        // stream the long track instead of loading all of it into memory
        AudioImporter importer = AssetImporter.GetAtPath(ClipPath) as AudioImporter;
        if (importer != null)
        {
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (settings.loadType != AudioClipLoadType.Streaming)
            {
                settings.loadType = AudioClipLoadType.Streaming;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        GameObject old = GameObject.Find("BackgroundMusic");
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject go = new GameObject("BackgroundMusic", typeof(AudioSource), typeof(MusicVolume));
        AudioSource source = go.GetComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = true;
        source.spatialBlend = 0f;   // plain 2D sound, same volume everywhere
        source.volume = 0.7f;       // MusicVolume sets the real volume from the settings
        Undo.RegisterCreatedObjectUndo(go, "Add background music");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeObject = go;
        Debug.Log("Background music added. Press Play to hear it.");
    }
}
