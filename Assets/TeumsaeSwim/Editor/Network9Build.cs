using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TeumsaeSwim.Editor
{
    public static class Network9Build
    {
        public static void Baseline() { Build("web-baseline", false); }
        [MenuItem("Teumsae Swim/9. Build Cloudflare Web")]
        public static void Optimized()
        {
            // Limit only the WebGL variants; retain the original source artwork and other platforms.
            string[] paths = {
                "Assets/TeumsaeSwim/Art/PredatorAtlas.png",
                "Assets/TeumsaeSwim/Art/Terrain/TerrainOverlays.png",
                "Assets/TeumsaeSwim/Art/Terrain/TerrainSurfaces.png",
                "Assets/TeumsaeSwim/Art/UI/PlayerFish.png",
                "Assets/TeumsaeSwim/Art/UI/OceanHome.png"
            };
            foreach (string path in paths)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null) throw new InvalidOperationException("Missing texture: " + path);
                var platform = importer.GetPlatformTextureSettings("WebGL");
                platform.overridden = true;
                platform.maxTextureSize = 1024;
                platform.textureCompression = TextureImporterCompression.Compressed;
                importer.SetPlatformTextureSettings(platform);
                importer.SaveAndReimport();
            }
            Build("web", true);
        }

        private static void Build(string destination, bool optimized)
        {
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
                if (optimized) PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { SwimProjectSetup.scenePath }, locationPathName = destination,
                    target = BuildTarget.WebGL, options = BuildOptions.DetailedBuildReport
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("WebGL build failed: " + report.summary.result);
                var rows = report.packedAssets.SelectMany(p => p.contents)
                    .GroupBy(a => string.IsNullOrEmpty(a.sourceAssetPath) ? "(generated asset)" : a.sourceAssetPath)
                    .Select(g => new { path = g.Key, bytes = g.Aggregate(0UL, (sum, item) => sum + item.packedSize) })
                    .OrderByDescending(x => x.bytes);
                File.WriteAllLines(destination + "-asset-report.tsv", new[] { "bytes\tasset" }.Concat(rows.Select(x => x.bytes + "\t" + x.path)));
                Debug.Log("NETWORK9_BUILD_OK " + destination + " " + report.summary.totalSize + " " + report.summary.totalTime);
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
            }
        }
    }
}
