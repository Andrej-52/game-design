using UnityEditor;
using UnityEngine;

// Tools > Towelman > Set Up Open Level
// Adds everything the tutorial has to the scene you have open (e.g. Level1): hearts, level timer + watch,
// game over screen, victory screen with the office finish line, the invisible level walls, the background music and the intro.
// The player and camera are left alone. Enemies, buses and billboards are level content, drag the prefabs in yourself.
public static class LevelSetup
{
    static readonly string[] Steps =
    {
        "Tools/Towelman/Add Hearts HUD",
        "Tools/Towelman/Add Level Timer",
        "Tools/Towelman/Add Game Over Screen",
        "Tools/Towelman/Add Victory Screen",
        "Tools/Towelman/Add Level Walls",
        "Tools/Towelman/Add Background Music",
        "Tools/Towelman/Add Intro Sequence",
    };

    [MenuItem("Tools/Towelman/Set Up Open Level")]
    static void Run()
    {
        if (GameObject.FindGameObjectWithTag("Player") == null)
        {
            EditorUtility.DisplayDialog("Set Up Open Level",
                "There is no object tagged Player in the open scene. Put the Player prefab in the scene first.", "OK");
            return;
        }

        foreach (string step in Steps)
        {
            if (!EditorApplication.ExecuteMenuItem(step))
                Debug.LogWarning("Could not run '" + step + "'.");
        }

        Debug.Log("Level set up. Check the green OfficeGoal box, the LeftWall / RightWall and the LevelTimer time limit, then save the scene.");
    }
}
