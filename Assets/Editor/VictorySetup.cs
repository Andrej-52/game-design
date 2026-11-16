using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Towelman > Add Victory Screen: adds the victory screen (same sprites and font as the main menu)
// and a finish trigger inside the office building. Move / resize the green "OfficeGoal" box if needed.
public static class VictorySetup
{
    [MenuItem("Tools/Towelman/Add Victory Screen")]
    static void Add()
    {
        VictoryUI existing = Object.FindFirstObjectByType<VictoryUI>();
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject go = new GameObject("VictoryScreen", typeof(VictoryUI));
        VictoryUI ui = go.GetComponent<VictoryUI>();
        ui.panelSprite = FindSprite("panel_rounded");
        ui.buttonSprite = FindSprite("button_rounded");
        ui.font = FindFont("Fredoka");
        Undo.RegisterCreatedObjectUndo(go, "Create victory screen");

        OfficeGoal goal = Object.FindFirstObjectByType<OfficeGoal>();
        if (goal == null)
        {
            GameObject goalGo = new GameObject("OfficeGoal", typeof(BoxCollider), typeof(OfficeGoal));
            Undo.RegisterCreatedObjectUndo(goalGo, "Create office goal");
            goal = goalGo.GetComponent<OfficeGoal>();

            BoxCollider box = goalGo.GetComponent<BoxCollider>();
            box.isTrigger = true;

            GameObject office = GameObject.Find("Building_Goal_Office");
            if (office != null)
            {
                Bounds b = default;
                bool first = true;
                foreach (Renderer r in office.GetComponentsInChildren<Renderer>())
                {
                    if (first) { b = r.bounds; first = false; }
                    else b.Encapsulate(r.bounds);
                }

                if (!first)
                {
                    // a box in the middle of the building, tall enough to catch him whatever his height
                    goalGo.transform.position = new Vector3(b.center.x, b.center.y, 0f);
                    box.size = new Vector3(Mathf.Max(b.size.x * 0.3f, 4f), Mathf.Max(b.size.y, 12f), 40f);
                }
            }
            else
            {
                Debug.LogWarning("Building_Goal_Office not found, move the OfficeGoal object to the office entrance yourself.");
            }
        }

        goal.victory = ui;
        goal.timer = Object.FindFirstObjectByType<LevelTimer>();
        EditorUtility.SetDirty(goal);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeObject = goal.gameObject;
        Debug.Log("Victory screen added. Walk into the green OfficeGoal box to win. Move / resize the box if it's in the wrong place.");
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
