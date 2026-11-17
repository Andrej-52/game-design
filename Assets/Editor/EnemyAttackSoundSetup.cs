using UnityEditor;
using UnityEngine;

// Tools > Towelman > Add Enemy Attack Sounds
// Puts an attack sound on each enemy prefab (old lady, bike guy, pedestrian).
public static class EnemyAttackSoundSetup
{
    [MenuItem("Tools/Towelman/Add Enemy Attack Sounds")]
    static void Apply()
    {
        Assign("Assets/Prefabs/Enemies/OldLady.prefab", "Assets/Audio/Small Monster Attack by Daviddumasaudio.mp3");
        Assign("Assets/Prefabs/Enemies/BikeGuy.prefab", "Assets/Audio/Rotating Bicycle Bell by Freesound_community.mp3");
        Assign("Assets/Prefabs/Enemies/Pedestrian.prefab", "Assets/Audio/Male Fighter Voice by Phatphrogstudio.mp3");
        AssetDatabase.SaveAssets();
    }

    static void Assign(string prefabPath, string clipPath)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        if (clip == null) { Debug.LogError("Could not find " + clipPath); return; }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        EnemySounds sounds = root.GetComponentInChildren<EnemySounds>(true);
        if (sounds == null) sounds = root.AddComponent<EnemySounds>();
        sounds.attackClip = clip;
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("Attack sound set on " + prefabPath);
    }
}
