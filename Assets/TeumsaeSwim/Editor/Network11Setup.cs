using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TeumsaeSwim.Editor
{
    public static class Network11Setup
    {
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            var ads = UnityEngine.Object.FindFirstObjectByType<DummyAds>();
            if (ads == null) ads = new GameObject("DummyAds").AddComponent<DummyAds>();
            ads.displayFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/TeumsaeSwim/Fonts/NotoSansKR-Regular.otf");
            ads.useBidding = true;
            if (UnityEngine.Object.FindFirstObjectByType<AdStats>() == null)
                new GameObject("AdStats").AddComponent<AdStats>();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save the game scene");
            AssetDatabase.SaveAssets();
            Debug.Log("NETWORK11_SETUP_OK");
            Verify();
        }

        public static void Verify()
        {
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            var ads = UnityEngine.Object.FindFirstObjectByType<DummyAds>();
            if (ads == null || UnityEngine.Object.FindFirstObjectByType<AdStats>() == null)
                throw new Exception("Missing advertising components");
            var pick = typeof(DummyAds).GetMethod("Pick", BindingFlags.NonPublic | BindingFlags.Instance);
            var price = typeof(DummyAds).GetField("pickedPrice", BindingFlags.NonPublic | BindingFlags.Instance);
            var asked = typeof(DummyAds).GetField("asked", BindingFlags.NonPublic | BindingFlags.Instance);
            var original = ads.networks.ToArray();
            for (int mode = 0; mode < 3; mode++)
            {
                ads.networks = mode == 1 ? original.Reverse().ToArray() : original;
                ads.useBidding = mode == 2;
                UnityEngine.Random.InitState(20261125);
                int filled = 0, asks = 0;
                float sum = 0;
                for (int i = 0; i < 100; i++)
                {
                    var winner = (DummyNetwork)pick.Invoke(ads, new object[] { false });
                    asks += ads.useBidding ? 1 : (int)asked.GetValue(ads);
                    if (winner != null) { filled++; sum += (float)price.GetValue(ads); }
                }
                Debug.Log("NETWORK11_SIM " + (mode == 0 ? "waterfall ABC" : mode == 1 ? "waterfall CBA" : "bidding ABC") +
                          " filled=" + filled + " avgEcpm=" + (sum / Mathf.Max(1, filled)).ToString("F2") +
                          " revenue=" + (sum / 1000f).ToString("F3") + " wait=" + (asks / 100f * ads.askSeconds).ToString("F2"));
            }
            ads.noFill = true;
            if (pick.Invoke(ads, new object[] { false }) != null) throw new Exception("No fill returned an ad");
            ads.networks = original;
            ads.useBidding = true;
            ads.noFill = false;
            Debug.Log("NETWORK11_VERIFY_OK");
        }
    }
}
