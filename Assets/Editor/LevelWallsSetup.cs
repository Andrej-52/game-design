using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Towelman > Add Level Walls
// Puts an invisible wall at the left end of the start building and the right end of the office,
// so the player (and the enemies) can't run out of the level.
public static class LevelWallsSetup
{
    [MenuItem("Tools/Towelman/Add Level Walls")]
    static void AddLevelWalls()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        float z = player != null ? player.transform.position.z : 0f;

        bool left = AddWall("LeftWall", "Building_Start_LockerRoom", -1f, z);
        bool right = AddWall("RightWall", "Building_Goal_Office", 1f, z);

        if (left || right)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // side = -1: wall at the left edge of the building, side = 1: at the right edge
    static bool AddWall(string wallName, string buildingName, float side, float z)
    {
        GameObject building = GameObject.Find(buildingName);
        if (building == null)
        {
            Debug.LogWarning("Could not find '" + buildingName + "' in the scene, so no " + wallName + " was made. " +
                             "Create an empty object with a Box Collider yourself and place it at the edge.");
            return false;
        }

        Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning(buildingName + " has no renderers, so no " + wallName + " was made.");
            return false;
        }

        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);

        // remove an earlier wall so running this twice doesn't stack walls
        GameObject old = GameObject.Find(wallName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        const float thickness = 4f;
        float edge = side < 0f ? b.min.x : b.max.x;

        GameObject wall = new GameObject(wallName);
        wall.transform.position = new Vector3(edge + side * thickness * 0.5f, b.center.y + 40f, z);
        BoxCollider box = wall.AddComponent<BoxCollider>();
        box.size = new Vector3(thickness, 120f, 40f);   // tall and deep, so nothing can slip past
        Undo.RegisterCreatedObjectUndo(wall, "Add level wall");

        Debug.Log(wallName + " placed at x = " + edge.ToString("0.0") + " (edge of " + buildingName + "). Move it if the edge is somewhere else.");
        return true;
    }
}
