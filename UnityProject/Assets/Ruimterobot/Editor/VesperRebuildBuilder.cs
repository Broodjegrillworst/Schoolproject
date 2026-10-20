using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VesperRebuildBuilder
{
    private const string SceneFolder = "Assets/Ruimterobot/Scenes";
    private static readonly string[] SceneNames = { "VesperMenu", "Region01", "Region02", "Region03", "Region04", "Region05" };

    [MenuItem("Vesper/Herbouw de vijf verbonden gebieden")]
    public static void RebuildScenes()
    {
        BuildScenes();
        Debug.Log("VESPER: vijf nieuwe gebiedsscènes zijn gebouwd; de oude PULSE-scènes zijn niet langer actief in Build Settings.");
    }

    [MenuItem("Vesper/Herbouw en exporteer voor Windows")]
    public static void RebuildAndExport()
    {
        BuildScenes();
        ExportWindows();
    }

    private static void BuildScenes()
    {
        if (!AssetDatabase.IsValidFolder(SceneFolder))
            throw new System.IO.DirectoryNotFoundException("Unity-map ontbreekt: " + SceneFolder);

        foreach (string sceneName in SceneNames)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = sceneName;
            GameObject bootstrap = new GameObject("Vesper // world bootstrap");
            bootstrap.AddComponent<VesperGameBootstrap>();
            EditorSceneManager.SaveScene(scene, SceneFolder + "/" + sceneName + ".unity");
        }

        EditorBuildSettings.scenes = SceneNames
            .Select(name => new EditorBuildSettingsScene(SceneFolder + "/" + name + ".unity", true))
            .ToArray();
        PlayerSettings.companyName = "Jaiden Asruf";
        PlayerSettings.productName = "VESPER: Echoes Below";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.jaidenasruf.vesper.echoesbelow");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(SceneFolder + "/VesperMenu.unity", OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
    }

    private static void ExportWindows()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            throw new System.InvalidOperationException("Windows x64 build support ontbreekt in deze Unity-installatie.");
        string root = Directory.GetParent(Application.dataPath).FullName;
        string output = Path.Combine(root, "Builds", "VESPER Echoes Below Windows");
        Directory.CreateDirectory(output);
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(output, "VESPER.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.InvalidOperationException("VESPER Windows-export mislukt: " + report.summary.result);
        Debug.Log("VESPER Windows-export klaar: " + Path.Combine(output, "VESPER.exe") + " (" + report.summary.totalSize + " bytes)");
    }
}
