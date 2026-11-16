using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Towelman > Add Intro Sequence: adds the start-of-level intro to the open scene.
public static class IntroSetup
{
    [MenuItem("Tools/Towelman/Add Intro Sequence")]
    static void Add()
    {
        IntroSequence existing = Object.FindFirstObjectByType<IntroSequence>();
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject go = new GameObject("IntroSequence", typeof(IntroSequence));
        go.GetComponent<IntroSequence>().font = FindFont("Fredoka");
        Undo.RegisterCreatedObjectUndo(go, "Add intro sequence");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeObject = go;
        // the clip of the hand with the watch (made in Blender)
        UnityEngine.Video.VideoClip clip = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/Videos/HandWatch.mp4");
        if (clip != null) go.GetComponent<IntroSequence>().handClip = clip;
        else Debug.LogWarning("Assets/Videos/HandWatch.mp4 not found, the intro will use the on-screen alarm clock instead.");

        Debug.Log("Intro added. Press Play to see it (Enter or click skips it).");
    }

    static TMP_FontAsset FindFont(string key)
    {
        foreach (string guid in AssetDatabase.FindAssets(key + " t:TMP_FontAsset"))
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
        return null;
    }
}
