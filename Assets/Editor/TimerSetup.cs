using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Towelman > Add Level Timer: adds the LevelTimer and the watch HUD to the open scene.
public static class TimerSetup
{
    [MenuItem("Tools/Towelman/Add Level Timer")]
    static void Add()
    {
        LevelTimer timer = Object.FindFirstObjectByType<LevelTimer>();
        if (timer == null)
        {
            GameObject go = new GameObject("LevelTimer", typeof(LevelTimer));
            Undo.RegisterCreatedObjectUndo(go, "Create LevelTimer");
            timer = go.GetComponent<LevelTimer>();
            timer.timeLimitSeconds = 600f;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            GameObject canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create HUD");
        }

        WatchUI existing = Object.FindFirstObjectByType<WatchUI>();
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject watch = new GameObject("Watch", typeof(RectTransform), typeof(WatchUI));
        watch.transform.SetParent(canvas.transform, false);
        RectTransform rt = watch.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(30f, -30f);
        rt.sizeDelta = new Vector2(520f, 330f);
        watch.GetComponent<WatchUI>().timer = timer;
        Undo.RegisterCreatedObjectUndo(watch, "Create watch");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeObject = timer;
        Debug.Log("Level timer added. Change Time Limit Seconds on the LevelTimer object (600 = 10 minutes).");
    }
}
