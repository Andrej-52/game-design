using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Towelman > Add Menu Runner Animation
// Open MenuScene first. Adds a stripped-down Towelman (just the model and animator, no gameplay scripts)
// that plays the looping walk / check watch / run / stumble / run off sequence.
public static class MenuRunnerSetup
{
    [MenuItem("Tools/Towelman/Add Menu Runner Animation")]
    static void Add()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        if (prefab == null) { Debug.LogError("Could not find Assets/Prefabs/Player.prefab"); return; }

        GameObject old = GameObject.Find("MenuRunner");
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = "MenuRunner";
        go.tag = "Untagged";

        // remove everything gameplay-related, keep the model, materials and animator
        foreach (MonoBehaviour m in go.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(m);
        foreach (AudioSource a in go.GetComponentsInChildren<AudioSource>(true)) Object.DestroyImmediate(a);
        foreach (Rigidbody r in go.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(r);
        foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

        Animator anim = go.GetComponentInChildren<Animator>(true);
        MenuRunner runner = go.AddComponent<MenuRunner>();
        runner.animator = anim;
        runner.cam = Camera.main;

        // start somewhere in front of the camera so he is visible in the editor too
        if (Camera.main != null)
            go.transform.position = Camera.main.transform.position + Camera.main.transform.forward * runner.depth;

        Undo.RegisterCreatedObjectUndo(go, "Add menu runner");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeObject = go;
        Debug.Log("Menu runner added. Press Play to see him. Tune it on the MenuRunner object (groundHeight, heightFraction, speeds, timings).");
    }
}
