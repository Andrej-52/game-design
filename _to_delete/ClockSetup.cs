using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Towelman > Add Intro Clock
// Puts the animated wall clock (Assets/Models/WallClock.fbx, made in Blender) on the wall of the open scene
// and connects it to the intro cutscene. Move / scale the "IntroClock" object where you want it afterwards.
public static class ClockSetup
{
    const string FbxPath = "Assets/Models/WallClock.fbx";
    const string ControllerPath = "Assets/Models/WallClockController.controller";

    [MenuItem("Tools/Towelman/Add Intro Clock")]
    static void Add()
    {
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (fbx == null)
        {
            Debug.LogError("Could not find " + FbxPath + ".");
            return;
        }

        AnimationClip clip = SetUpImport();
        AnimatorController controller = MakeController(clip);

        GameObject old = GameObject.Find("IntroClock");
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject clock = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        clock.name = "IntroClock";
        Undo.RegisterCreatedObjectUndo(clock, "Add intro clock");

        Animator animator = clock.GetComponent<Animator>();
        if (animator == null) animator = clock.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        Camera cam = Camera.main;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        clock.transform.localScale = Vector3.one * 1.3f;

        if (cam != null && player != null)
        {
            FaceCamera(clock.transform, cam);
            Place(clock.transform, cam, player.transform);
        }
        else Debug.LogWarning("No Main Camera or Player found, place the IntroClock by hand.");

        IntroSequence intro = Object.FindFirstObjectByType<IntroSequence>();
        if (intro != null)
        {
            intro.clockAnimator = animator;
            EditorUtility.SetDirty(intro);
        }
        else Debug.LogWarning("No IntroSequence in the scene yet. Run Tools > Towelman > Add Intro Sequence, it adds the clock too.");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeObject = clock;
        Debug.Log("Intro clock added. Move / scale the IntroClock object onto the wall where you want it. The camera starts the cutscene on it.");
    }

    // generic animation, the only clip renamed to "Ring" and not looping
    static AnimationClip SetUpImport()
    {
        ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(FbxPath);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.animationCompression = ModelImporterAnimationCompression.Off;

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        if (clips.Length > 0)
        {
            clips[0].name = "Ring";
            clips[0].loopTime = false;
            importer.clipAnimations = new[] { clips[0] };
        }
        importer.SaveAndReimport();

        return AssetDatabase.LoadAllAssetsAtPath(FbxPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }

    static AnimatorController MakeController(AnimationClip clip)
    {
        if (File.Exists(ControllerPath)) AssetDatabase.DeleteAsset(ControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorState state = controller.layers[0].stateMachine.AddState("Ring");
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        return controller;
    }

    // turn the clock so its face points at the camera and the bells are on top
    static void FaceCamera(Transform root, Camera cam)
    {
        Transform frame = Find(root, "Frame");
        Transform cap = Find(root, "Cap");
        Transform bellL = Find(root, "BellL");
        Transform bellR = Find(root, "BellR");
        if (frame == null || cap == null || bellL == null || bellR == null) return;

        Vector3 front = (cap.position - frame.position).normalized;
        root.rotation = Quaternion.FromToRotation(front, -cam.transform.forward) * root.rotation;

        // roll so the bells end up on top
        front = (cap.position - frame.position).normalized;
        Vector3 up = (bellL.position + bellR.position) * 0.5f - frame.position;
        float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(up, front), Vector3.ProjectOnPlane(Vector3.up, front), front);
        root.rotation = Quaternion.AngleAxis(angle, front) * root.rotation;
    }

    // on the wall behind the player if we can find it, otherwise a few units behind him
    static void Place(Transform root, Camera cam, Transform player)
    {
        Transform frame = Find(root, "Frame");
        Vector3 target = player.position + Vector3.up * 3f + Vector3.right * 2f;
        Vector3 dir = (target - cam.transform.position).normalized;

        Vector3 position = target + cam.transform.forward * 4f;
        float best = float.MaxValue;
        foreach (RaycastHit hit in Physics.RaycastAll(cam.transform.position, dir, 200f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(player) || hit.distance < Vector3.Distance(cam.transform.position, player.position)) continue;
            if (hit.distance < best)
            {
                best = hit.distance;
                position = hit.point - dir * 0.2f;
            }
        }

        Vector3 center = frame != null ? frame.position : root.position;
        root.position += position - center;
    }

    static Transform Find(Transform root, string name)
    {
        return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
    }
}
