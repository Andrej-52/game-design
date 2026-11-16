using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Towelman > Add Game Over Screen: adds the game over screen to the open scene,
// using the same button sprite and font as the main menu.
public static class GameOverSetup
{
    [MenuItem("Tools/Towelman/Add Game Over Screen")]
    static void Add()
    {
        GameOverUI existing = Object.FindFirstObjectByType<GameOverUI>();
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject go = new GameObject("GameOverScreen", typeof(GameOverUI));
        GameOverUI ui = go.GetComponent<GameOverUI>();
        ui.buttonSprite = FindSprite("button_rounded");
        ui.font = FindFont("Fredoka");
        Undo.RegisterCreatedObjectUndo(go, "Create game over screen");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeObject = go;
        Debug.Log("Game over screen added. To test it, set the LevelTimer to 10 seconds and wait.");
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
