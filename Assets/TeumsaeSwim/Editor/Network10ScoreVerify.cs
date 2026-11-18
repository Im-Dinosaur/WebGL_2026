using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace TeumsaeSwim.Editor
{
    public static class Network10ScoreVerify
    {
        private const string DbUrl = "https://networkpro2026-9c3fa-default-rtdb.asia-southeast1.firebasedatabase.app";
        private static int stage;
        private static double deadline, stageAt;
        private static UnityWebRequest request;
        private static UnityWebRequestAsyncOperation operation;

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
            string scene = SceneManager.GetActiveScene().name;
            if (stage == 0 && scene == "Login" && AuthManager.Instance != null)
            {
                stage = 1;
                AuthManager.Instance.GuestLogin();
            }
            else if (stage == 1 && scene == "TeumsaeSwim" && ScoreSaver.Instance != null
                && !string.IsNullOrEmpty(AuthManager.Uid))
            {
                stage = 2;
                ScoreSaver.Instance.Save(3);
            }
            else if (stage == 2 && ScoreSaver.Instance != null &&
                (ScoreSaver.Instance.Status == "서버에 최고 별 3개 저장됨" || ScoreSaver.Instance.Status == "서버 최고 별 3개"))
            {
                stage = 3;
                ScoreSaver.Instance.Save(2);
                stageAt = EditorApplication.timeSinceStartup;
            }
            else if (stage == 3 && EditorApplication.timeSinceStartup > stageAt + 4)
            {
                stage = 4;
                request = UnityWebRequest.Get(DbUrl + "/scores/" + UnityWebRequest.EscapeURL(AuthManager.Uid) + ".json");
                request.timeout = 15;
                operation = request.SendWebRequest();
            }
            else if (stage == 4 && operation.isDone)
            {
                if (request.result != UnityWebRequest.Result.Success) { Finish(false, "Public score read failed"); return; }
                Score data = JsonUtility.FromJson<Score>(request.downloadHandler.text);
                if (data == null || data.score != 3) { Finish(false, "Lower result overwrote best"); return; }
                request.Dispose();
                request = UnityWebRequest.Put(DbUrl + "/scores/__unauthorized_test__.json", "{\"score\":1}");
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 15;
                operation = request.SendWebRequest();
                stage = 5;
            }
            else if (stage == 5 && operation.isDone)
            {
                bool denied = request.responseCode == 401 || request.responseCode == 403;
                Finish(denied, denied ? "Best score and unauthenticated write protection verified" : "Unauthenticated write was not denied");
            }
        }

        private static void Finish(bool success, string detail)
        {
            EditorApplication.update -= Tick;
            if (request != null) request.Dispose();
            Debug.Log((success ? "NETWORK10_SCORE_VERIFY_OK " : "NETWORK10_SCORE_VERIFY_FAILED ") + detail);
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(success ? 0 : 1);
        }

        [Serializable]
        private sealed class Score { public int score; }
    }
}
