using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterglow.Editor
{
    public static class AfterglowProjectBuilder
    {
        private const string SceneRoot = "Assets/Afterglow/Scenes/";

        [MenuItem("Afterglow/Build Windows Release")]
        public static void BuildAndExport()
        {
            try
            {
                CreateScenes();
                ConfigureProject();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                string[] scenes =
                {
                    SceneRoot + "AfterglowMenu.unity",
                    SceneRoot + "Afterglow_01_Rooftops.unity",
                    SceneRoot + "Afterglow_02_Market.unity",
                    SceneRoot + "Afterglow_03_Core.unity"
                };
                string output = Path.GetFullPath("Builds/Afterglow_Windows/Afterglow.exe");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = output,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                Debug.Log("AFTERGLOW Windows build: " + report.summary.result + " | " + report.summary.totalSize + " bytes | " + report.summary.totalTime);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Windows build failed: " + report.summary.result);
                CopyDocumentation(output);
                if (!Application.isBatchMode)
                    EditorUtility.DisplayDialog("AFTERGLOW export", "De Windows-build is klaar:\n" + output, "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (!Application.isBatchMode)
                    EditorUtility.DisplayDialog("AFTERGLOW export mislukt", exception.Message + "\n\nBekijk de Unity Console voor details.", "OK");
            }
        }

        private static void CreateScenes()
        {
            Directory.CreateDirectory(SceneRoot);
            CreateScene("AfterglowMenu", true, 0);
            CreateScene("Afterglow_01_Rooftops", false, 0);
            CreateScene("Afterglow_02_Market", false, 1);
            CreateScene("Afterglow_03_Core", false, 2);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SceneRoot + "AfterglowMenu.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "Afterglow_01_Rooftops.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "Afterglow_02_Market.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "Afterglow_03_Core.unity", true)
            };
            EditorSceneManager.OpenScene(SceneRoot + "AfterglowMenu.unity", OpenSceneMode.Single);
        }

        private static void CreateScene(string name, bool menu, int stage)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Afterglow Scene Controller");
            root.AddComponent<AfterglowBootstrap>().ConfigureScene(menu, stage);
            scene.name = name;
            EditorSceneManager.SaveScene(scene, SceneRoot + name + ".unity");
        }

        private static void ConfigureProject()
        {
            PlayerSettings.companyName = "Jaiden Asruf";
            PlayerSettings.productName = "AFTERGLOW: The Rootbound Bell";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = false;
        }

        private static void CopyDocumentation(string buildPath)
        {
            string sourceRoot = Path.Combine(Application.dataPath, "Afterglow");
            string outputRoot = Path.GetDirectoryName(buildPath);
            CopyDocumentationFile(sourceRoot, outputRoot, "LEESMIJ.md", "LEESMIJ.txt");
            CopyDocumentationFile(sourceRoot, outputRoot, "KWALITEITSRAPPORT.md", "KWALITEITSRAPPORT.txt");
        }

        private static void CopyDocumentationFile(string sourceRoot, string outputRoot, string sourceName, string outputName)
        {
            string source = Path.Combine(sourceRoot, sourceName);
            if (!File.Exists(source)) return;
            File.Copy(source, Path.Combine(outputRoot, outputName), true);
        }
    }
}
