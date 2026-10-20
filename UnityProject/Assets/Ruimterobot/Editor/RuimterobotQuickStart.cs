using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RuimterobotQuickStart
{
    private const string MainMenuScene = "Assets/Ruimterobot/Scenes/MainMenu.unity";
    private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
    private static string PlayStatus => Path.Combine(ProjectRoot, "Library", "PulsePlayMode.status");

    static RuimterobotQuickStart()
    {
        EditorApplication.playModeStateChanged += SavePlayModeStatus;
        EditorApplication.delayCall += SaveCurrentPlayModeStatus;
    }

    [MenuItem("PULSE/Open startscherm")]
    public static void OpenStartScreen()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += OpenStartScreen;
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
        Debug.Log("PULSE: startscherm geopend.");
    }

    [MenuItem("PULSE/Open startscherm en start game")]
    public static void OpenAndPlay()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
        EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
    }

    private static void SaveCurrentPlayModeStatus()
    {
        File.WriteAllText(PlayStatus, EditorApplication.isPlaying ? "In Play Mode" : "Edit Mode");
    }

    private static void SavePlayModeStatus(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            File.WriteAllText(PlayStatus, "In Play Mode");
            FocusGameView();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
            File.WriteAllText(PlayStatus, "Edit Mode");
    }

    private static void FocusGameView()
    {
        if (Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            return;

        System.Type gameViewType = System.Type.GetType("UnityEditor.GameView, UnityEditor");
        if (gameViewType != null)
            EditorWindow.GetWindow(gameViewType).Focus();
    }
}
