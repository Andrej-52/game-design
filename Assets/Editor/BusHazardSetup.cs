using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Towelman > Make Buses Hurt In Open Scene
// Makes every driving bus in the open scene hurt the player like the tutorial bus:
// each bus needs a trigger collider with the RestartOnTouch script on it.
public static class BusHazardSetup
{
    [MenuItem("Tools/Towelman/Make Buses Hurt In Open Scene")]
    static void Run()
    {
        var buses = new HashSet<GameObject>();
        foreach (BusDriveX b in Object.FindObjectsByType<BusDriveX>(FindObjectsSortMode.None)) buses.Add(b.gameObject);
        foreach (BusDriveZ b in Object.FindObjectsByType<BusDriveZ>(FindObjectsSortMode.None)) buses.Add(b.gameObject);

        int fixedTriggers = 0, madeTriggers = 0, alreadyOk = 0;

        foreach (GameObject bus in buses)
        {
            bool hasTrigger = false;

            foreach (Collider c in bus.GetComponentsInChildren<Collider>(true))
            {
                if (!c.isTrigger) continue;
                hasTrigger = true;

                if (c.GetComponent<RestartOnTouch>() == null)
                {
                    Undo.AddComponent<RestartOnTouch>(c.gameObject);
                    EditorUtility.SetDirty(c.gameObject);
                    fixedTriggers++;
                }
                else alreadyOk++;
            }

            // no trigger at all: make one around the bus
            if (!hasTrigger)
            {
                Renderer[] renderers = bus.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    Debug.LogWarning("Bus '" + bus.name + "' has no renderers, add a trigger collider and RestartOnTouch yourself.", bus);
                    continue;
                }

                Bounds b = renderers[0].bounds;
                foreach (Renderer r in renderers) b.Encapsulate(r.bounds);

                GameObject zone = new GameObject("HurtZone");
                Undo.RegisterCreatedObjectUndo(zone, "Create bus hurt zone");
                zone.transform.SetParent(bus.transform, true);
                zone.transform.position = b.center;
                zone.transform.rotation = Quaternion.identity;

                BoxCollider box = zone.AddComponent<BoxCollider>();
                box.isTrigger = true;
                // box size is in the zone's local space, undo the parent's scale
                Vector3 s = zone.transform.lossyScale;
                box.size = new Vector3(b.size.x / s.x, b.size.y / s.y, Mathf.Max(b.size.z, 5f) / s.z);

                zone.AddComponent<RestartOnTouch>();
                madeTriggers++;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Buses found: " + buses.Count + ". Added RestartOnTouch to " + fixedTriggers + " trigger(s), made " + madeTriggers
            + " new hurt zone(s), " + alreadyOk + " were already fine. Save the scene.");
    }
}
