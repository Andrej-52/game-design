using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Select one or more enemy FBX files in the Project window, then use
// Tools > Towelman > Set Up Enemy From Selected FBX.
// It fixes the import settings, builds the Animator Controller and saves a ready-to-use enemy prefab.
public static class EnemySetup
{
    const string PrefabFolder = "Assets/Prefabs/Enemies";

    [MenuItem("Tools/Towelman/Set Up Enemy From Selected FBX")]
    static void SetUpSelected()
    {
        foreach (UnityEngine.Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (AssetImporter.GetAtPath(path) is ModelImporter)
                SetUp(path);
        }
    }

    [MenuItem("Tools/Towelman/Set Up Enemy From Selected FBX", true)]
    static bool ValidateSetUpSelected()
    {
        return Selection.objects.Any(o => AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(o)) is ModelImporter);
    }

    // For prefabs made before the Avatar was set: gives every enemy prefab's Animator its Avatar
    [MenuItem("Tools/Towelman/Fix Avatar On Enemy Prefabs")]
    static void FixAvatars()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);

            Animator animator = contents.GetComponentInChildren<Animator>();
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(animator.gameObject);
            string fbxPath = AssetDatabase.GetAssetPath(source);

            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
            }

            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Avatar>().FirstOrDefault();
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
            Debug.Log(prefabPath + ": avatar set");
        }

        AssetDatabase.SaveAssets();
    }

    static void SetUp(string fbxPath)
    {
        string enemyName = Path.GetFileNameWithoutExtension(fbxPath);
        string folder = Path.GetDirectoryName(fbxPath).Replace('\\', '/');

        // 1. Import settings: generic rig, no compression, clean clip names, loop the walk / ride
        ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.animationCompression = ModelImporterAnimationCompression.Off;

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation c in clips)
        {
            c.name = c.name.Substring(c.name.LastIndexOf('|') + 1);
            c.loopTime = Has(c.name, "Walk") || Has(c.name, "Ride");
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();

        // 2. Find the clips
        AnimationClip[] all = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__"))
            .ToArray();

        AnimationClip move = Find(all, "Walk") ?? Find(all, "Ride");
        AnimationClip attack = Find(all, "Attack");
        AnimationClip hurt = Find(all, "Hurt");
        AnimationClip death = Find(all, "Death");

        if (move == null || attack == null || hurt == null || death == null)
        {
            Debug.LogError(enemyName + ": could not find Walk/Ride, Attack, Hurt and Death clips. Found: "
                + string.Join(", ", all.Select(c => c.name)));
            return;
        }

        // 3. Animator Controller
        string controllerPath = folder + "/" + enemyName + "Controller.controller";
        if (File.Exists(controllerPath))
            AssetDatabase.DeleteAsset(controllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState moveState = sm.AddState("Walk");
        moveState.motion = move;
        sm.defaultState = moveState;

        AnimatorState attackState = sm.AddState("Attack");
        attackState.motion = attack;
        AnimatorState hurtState = sm.AddState("Hurt");
        hurtState.motion = hurt;
        AnimatorState deathState = sm.AddState("Death");
        deathState.motion = death;

        AddFromAnyState(sm, attackState, "Attack");
        AddFromAnyState(sm, hurtState, "Hurt");
        AddFromAnyState(sm, deathState, "Die");   // no way out of Death

        AddBackToMove(attackState, moveState);
        AddBackToMove(hurtState, moveState);

        // 4. Prefab: root (physics + scripts) with the FBX as a child called Model
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

        GameObject root = new GameObject(enemyName);
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
        model.name = "Model";
        model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        Animator animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.avatar = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Avatar>().FirstOrDefault();
        animator.applyRootMotion = false;

        // collider that fits the model (side view: x = length, y = height)
        Bounds b = default;
        bool first = true;
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
        {
            if (r.bounds.size.magnitude < 0.05f) continue;   // hidden effect pieces
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = b.center;
        box.size = new Vector3(b.size.x, b.size.y, Mathf.Clamp(b.size.z, 0.6f, 1.2f));

        EnemyHealth health = root.AddComponent<EnemyHealth>();
        EnemyAI ai = root.AddComponent<EnemyAI>();

        // per-enemy defaults (all adjustable in the Inspector)
        if (Has(enemyName, "Bike"))
        {
            health.maxHealth = 3;
            ai.walkSpeed = 4f;
            ai.chaseSpeedMultiplier = 1.6f;
            ai.detectRange = 9f;
            ai.attackRange = 2.5f;
            ai.hitBoxOffset = 1.5f;
            ai.hitMoment = 0.45f;
        }
        else if (Has(enemyName, "Lady"))
        {
            health.maxHealth = 3;
            ai.walkSpeed = 1.2f;
            ai.chaseSpeedMultiplier = 1.3f;
            ai.detectRange = 5f;
            ai.attackRange = 1.8f;
            ai.hitMoment = 0.4f;
        }
        else
        {
            health.maxHealth = 2;
            ai.walkSpeed = 2f;
            ai.detectRange = 6f;
            ai.attackRange = 1.6f;
            ai.hitMoment = 0.35f;
        }

        EnsureFolder(PrefabFolder);
        string prefabPath = PrefabFolder + "/" + enemyName + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(prefab);
        Debug.Log(enemyName + ": created " + prefabPath + " and " + controllerPath);
    }

    static void AddFromAnyState(AnimatorStateMachine sm, AnimatorState to, string trigger)
    {
        AnimatorStateTransition t = sm.AddAnyStateTransition(to);
        t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        t.hasExitTime = false;
        t.duration = 0.05f;
        t.canTransitionToSelf = false;
    }

    static void AddBackToMove(AnimatorState from, AnimatorState move)
    {
        AnimatorStateTransition t = from.AddTransition(move);
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.duration = 0.1f;
    }

    static AnimationClip Find(AnimationClip[] clips, string key)
    {
        return clips.FirstOrDefault(c => Has(c.name, key));
    }

    static bool Has(string text, string key)
    {
        return text.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
