using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TeumsaeSwim.Editor
{
    public static class Network10Build
    {
        private const string LoginPath = "Assets/TeumsaeSwim/Scenes/Login.unity";

        [MenuItem("Teumsae Swim/10. Set up guest login")]
        public static void Setup()
        {
            AssetDatabase.Refresh();
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/TeumsaeSwim/Fonts/NotoSansKR-Regular.otf");
            if (font == null) throw new InvalidOperationException("Korean font is missing");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("AuthManager", typeof(AuthManager));
            var view = new GameObject("LoginView", typeof(LoginView)).GetComponent<LoginView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("koreanFont").objectReferenceValue = font;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), LoginPath))
                throw new InvalidOperationException("Unable to save Login scene");
            var previous = EditorBuildSettings.scenes
                .Where(scene => scene.path != LoginPath && scene.path != SwimProjectSetup.scenePath);
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene(LoginPath, true),
                new EditorBuildSettingsScene(SwimProjectSetup.scenePath, true)
            }.Concat(previous).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("NETWORK10_LOGIN_SETUP_OK");
        }

        [MenuItem("Teumsae Swim/11. Build login and score WebGL")]
        public static void Build()
        {
            if (!File.Exists(LoginPath) || !File.Exists(SwimProjectSetup.scenePath))
                throw new InvalidOperationException("Login or game scene is missing");
            string settingsPath = "ProjectSettings/ProjectSettings.asset";
            byte[] original = File.ReadAllBytes(settingsPath);
            string product = PlayerSettings.productName;
            string template = PlayerSettings.WebGL.template;
            var compression = PlayerSettings.WebGL.compressionFormat;
            bool fallback = PlayerSettings.WebGL.decompressionFallback;
            int width = PlayerSettings.defaultWebScreenWidth;
            int height = PlayerSettings.defaultWebScreenHeight;
            bool background = PlayerSettings.runInBackground;
            var stripping = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL);
            try
            {
                PlayerSettings.productName = "숨바꼬기";
                PlayerSettings.WebGL.template = "PROJECT:TeumsaeSwim";
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
                PlayerSettings.WebGL.decompressionFallback = true;
                PlayerSettings.defaultWebScreenWidth = 480;
                PlayerSettings.defaultWebScreenHeight = 854;
                PlayerSettings.runInBackground = false;
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { LoginPath, SwimProjectSetup.scenePath },
                    locationPathName = "web10", target = BuildTarget.WebGL,
                    options = BuildOptions.DetailedBuildReport
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("WebGL build failed: " + report.summary.result);
                Debug.Log("NETWORK10_BUILD_OK " + report.summary.totalSize + " " + report.summary.totalTime);
            }
            finally
            {
                PlayerSettings.productName = product;
                PlayerSettings.WebGL.template = template;
                PlayerSettings.WebGL.compressionFormat = compression;
                PlayerSettings.WebGL.decompressionFallback = fallback;
                PlayerSettings.defaultWebScreenWidth = width;
                PlayerSettings.defaultWebScreenHeight = height;
                PlayerSettings.runInBackground = background;
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, stripping);
                AssetDatabase.SaveAssets();
                File.WriteAllBytes(settingsPath, original);
            }
        }
    }
}
