using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Tools > Towelman > Apply Toon Shading To Characters
// Makes a toon copy of every material on the player and the enemies (keeping their colours and textures)
// and puts them on the character prefabs. "Remove Toon Shading" goes back to the original materials.
public static class ToonSetup
{
    const string ShaderName = "Towelman/ToonLit";
    const string MaterialFolder = "Assets/Materials/Toon";

    static readonly string[] Prefabs =
    {
        "Assets/Prefabs/Player.prefab",
        "Assets/Prefabs/Enemies/Pedestrian.prefab",
        "Assets/Prefabs/Enemies/OldLady.prefab",
        "Assets/Prefabs/Enemies/BikeGuy.prefab",
    };

    [MenuItem("Tools/Towelman/Apply Toon Shading To Characters")]
    static void Apply()
    {
        Shader toon = Shader.Find(ShaderName);
        if (toon == null)
        {
            Debug.LogError("Could not find the shader " + ShaderName + ". Is Assets/Shaders/ToonLit.shader imported without errors?");
            return;
        }

        EnsureFolder("Assets/Materials");
        EnsureFolder(MaterialFolder);

        var made = new Dictionary<Material, Material>();
        var reset = new HashSet<Material>();
        int changed = 0;

        foreach (string path in Prefabs)
        {
            if (!File.Exists(path)) { Debug.LogWarning("Skipped (not found): " + path); continue; }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;

                Material[] mats = r.sharedMaterials;
                bool dirty = false;

                for (int i = 0; i < mats.Length; i++)
                {
                    Material src = mats[i];
                    if (src == null) continue;

                    // already toon (from an earlier run): just reset it to the default look
                    if (src.shader == toon)
                    {
                        if (reset.Add(src))
                        {
                            ApplyLook(src);
                            EditorUtility.SetDirty(src);
                        }
                        continue;
                    }
                    if (src.HasProperty("_Surface") && src.GetFloat("_Surface") > 0.5f) continue;   // see-through effect, leave it

                    if (!made.TryGetValue(src, out Material toonMat))
                    {
                        toonMat = MakeToon(src, toon);
                        made[src] = toonMat;
                    }

                    mats[i] = toonMat;
                    dirty = true;
                }

                if (dirty)
                {
                    r.sharedMaterials = mats;
                    changed++;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Toon shading applied to " + changed + " renderers (" + made.Count + " new toon materials, " + reset.Count + " reset to the default look) in " + MaterialFolder
            + ". Tweak the look on those materials (shadow tint, outline width, ...).");
    }

    [MenuItem("Tools/Towelman/Remove Toon Shading")]
    static void Remove()
    {
        foreach (string path in Prefabs)
        {
            if (!File.Exists(path)) continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                PrefabUtility.RevertObjectOverride(r, InteractionMode.AutomatedAction);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Toon shading removed, the characters use their original materials again.");
    }

    static Material MakeToon(Material src, Shader toon)
    {
        string path = MaterialFolder + "/Toon_" + Sanitize(src.name) + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(toon);
            AssetDatabase.CreateAsset(mat, path);
        }

        // keep the colour and texture of the original
        Color color = Color.white;
        if (src.HasProperty("_BaseColor")) color = src.GetColor("_BaseColor");
        else if (src.HasProperty("_Color")) color = src.GetColor("_Color");
        mat.SetColor("_BaseColor", color);

        ApplyLook(mat);

        Texture tex = null;
        if (src.HasProperty("_BaseMap")) tex = src.GetTexture("_BaseMap");
        if (tex == null && src.HasProperty("_MainTex")) tex = src.GetTexture("_MainTex");
        mat.SetTexture("_BaseMap", tex);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    // the soft default look
    static void ApplyLook(Material mat)
    {
        mat.SetColor("_ShadowColor", new Color(0.74f, 0.70f, 0.86f, 1f));
        mat.SetFloat("_Threshold", 0.35f);
        mat.SetFloat("_Softness", 0.08f);
        mat.SetColor("_RimColor", new Color(1f, 1f, 1f, 0.15f));
    }

    static string Sanitize(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Replace(' ', '_');
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
