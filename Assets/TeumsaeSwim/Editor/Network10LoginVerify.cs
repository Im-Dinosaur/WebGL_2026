using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TeumsaeSwim.Editor
{
    public static class Network10LoginVerify
    {
        private static int stage;
        private static string firstUid;
        private static double deadline;

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/TeumsaeSwim/Scenes/Login.unity");
            stage = 0;
            deadline = EditorApplication.timeSinceStartup + 100;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup > deadline) { Finish(false, "Timed out"); return; }
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            var scene = SceneManager.GetActiveScene().name;
            if (stage == 0 && scene == "Login" && AuthManager.Instance != null)
            {
                stage = 1;
                AuthManager.Instance.GuestLogin();
            }
            else if (stage == 1 && scene == "TeumsaeSwim" && !string.IsNullOrEmpty(AuthManager.Uid))
            {
                firstUid = AuthManager.Uid;
                stage = 2;
                AuthManager.Logout();
            }
            else if (stage == 2 && scene == "Login" && AuthManager.Instance != null)
            {
                stage = 3;
                AuthManager.Instance.GuestLogin();
            }
            else if (stage == 3 && scene == "TeumsaeSwim" && !string.IsNullOrEmpty(AuthManager.Uid))
            {
                Finish(firstUid == AuthManager.Uid, "First and returning guest comparison");
            }
        }

        private static void Finish(bool success, string detail)
        {
            EditorApplication.update -= Tick;
            Debug.Log((success ? "NETWORK10_LOGIN_VERIFY_OK " : "NETWORK10_LOGIN_VERIFY_FAILED ") + detail);
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
