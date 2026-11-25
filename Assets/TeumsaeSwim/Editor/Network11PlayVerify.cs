using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TeumsaeSwim.Editor
{
    [InitializeOnLoad]
    public static class Network11PlayVerify
    {
        private const string ActiveKey = "teumsae.network11.test.active";
        private static int stage;
        private static double deadline;
        private static int expected;
        private static readonly FieldInfo Closed = typeof(DummyAds).GetField("closed", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo Showing = typeof(DummyAds).GetField("showing", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo Label = typeof(DummyAds).GetField("label", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo Completed = typeof(SwimGame).GetField("completedPlays", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo LastAd = typeof(SwimGame).GetField("lastInterstitialTime", BindingFlags.Static | BindingFlags.NonPublic);

        static Network11PlayVerify()
        {
            if (SessionState.GetBool(ActiveKey, false)) EditorApplication.update += Tick;
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            var ads = UnityEngine.Object.FindFirstObjectByType<DummyAds>();
            ads.networks = new[] { new DummyNetwork { name = "Test network", eCPM = 10f, fillRate = 1f } };
            ads.askSeconds = .05f;
            ads.rewardedSeconds = 5f;
            SessionState.SetInt("teumsae.network11.oldbest", PlayerPrefs.GetInt(ScoreSaver.LocalBestKey, 0));
            SessionState.SetBool(ActiveKey, true);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            deadline = EditorApplication.timeSinceStartup + 120;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 120;
            if (EditorApplication.timeSinceStartup > deadline) { Finish(false, "timeout"); return; }
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            var game = UnityEngine.Object.FindFirstObjectByType<SwimGame>();
            var session = UnityEngine.Object.FindFirstObjectByType<GameSessionComponent>();
            var ads = DummyAds.Instance;
            if (game == null || session == null || ads == null) return;
            if (stage == 0 && ads.IsRewardedReady())
            {
                game.begin(SwimDifficulty.Practice);
                session.advance(1, false, true);
                expected = game.stars;
                if (expected <= 0) { Finish(false, "no score on win"); return; }
                game.watchAdForDouble();
                stage = 1;
            }
            else if (stage == 1 && (bool)Showing.GetValue(ads))
            {
                Closed.SetValue(ads, true);
                stage = 2;
            }
            else if (stage == 2 && !(bool)Showing.GetValue(ads) && ads.IsRewardedReady())
            {
                if (game.resultScore != expected) { Finish(false, "early close expected=" + expected + " score=" + game.resultScore); return; }
                ads.rewardedSeconds = .25f;
                game.watchAdForDouble();
                stage = 3;
            }
            else if (stage == 3 && (bool)Showing.GetValue(ads) && (string)Label.GetValue(ads) == "광고 끝")
            {
                Closed.SetValue(ads, true);
                stage = 4;
            }
            else if (stage == 4 && !(bool)Showing.GetValue(ads))
            {
                if (game.resultScore != expected * 2) { Finish(false, "completed ad did not double score"); return; }
                game.watchAdForDouble();
                if (game.resultScore != expected * 2) { Finish(false, "duplicate reward"); return; }
                Completed.SetValue(null, 1);
                LastAd.SetValue(null, -999f);
                game.restartWithAd(false);
                if (game.phase != SwimPhase.Playing || (bool)Showing.GetValue(ads)) { Finish(false, "first round showed interstitial"); return; }
                session.advance(1, false, true);
                Completed.SetValue(null, 2);
                ads.interstitialSeconds = .25f;
                stage = 5;
            }
            else if (stage == 5 && ads.IsInterstitialReady())
            {
                game.restartWithAd(false);
                if (!(bool)Showing.GetValue(ads)) { Finish(false, "second round did not show interstitial"); return; }
                stage = 6;
            }
            else if (stage == 6 && (string)Label.GetValue(ads) == "광고 끝")
            {
                Closed.SetValue(ads, true);
                stage = 7;
            }
            else if (stage == 7 && !(bool)Showing.GetValue(ads))
            {
                if (game.phase != SwimPhase.Playing) { Finish(false, "interstitial did not restart game"); return; }
                session.advance(1, false, true);
                game.restartWithAd(false);
                if (game.phase != SwimPhase.Playing || (bool)Showing.GetValue(ads)) { Finish(false, "cooldown was not enforced"); return; }
                Finish(true, "reward close/completion/once and interstitial first-round/cooldown");
            }
        }

        private static void Finish(bool ok, string detail)
        {
            SessionState.SetBool(ActiveKey, false);
            EditorApplication.update -= Tick;
            PlayerPrefs.SetInt(ScoreSaver.LocalBestKey, SessionState.GetInt("teumsae.network11.oldbest", 0));
            PlayerPrefs.Save();
            Debug.Log((ok ? "NETWORK11_PLAY_VERIFY_OK " : "NETWORK11_PLAY_VERIFY_FAILED ") + detail);
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
