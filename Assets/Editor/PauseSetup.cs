using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Towelman > Add Pause Menu: adds the pause button and pause menu to the open level.
public static class PauseSetup
{
    [MenuItem("Tools/Towelman/Add Pause Menu")]
    static void Add()
    {
        PauseUI existing = Object.FindFirstObjectByType<PauseUI>();
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject go = new GameObject("PauseMenu", typeof(PauseUI));
        PauseUI ui = go.GetComponent<PauseUI>();
        ui.buttonSprite = FindSprite("button_rounded");
        ui.panelSprite = FindSprite("panel_rounded");
        ui.font = FindFont("Fredoka");
        Undo.RegisterCreatedObjectUndo(go, "Create pause menu");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeObject = go;
        Debug.Log("Pause menu added. Press Play, then click the pause button at the top or press Esc / P.");
    }

    static Sprite FindSprite(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Sprite"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }

    static TMP_FontAsset FindFont(string key)
    {
        foreach (string guid in AssetDatabase.FindAssets(key + " t:TMP_FontAsset"))
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
        return null;
    }
}
