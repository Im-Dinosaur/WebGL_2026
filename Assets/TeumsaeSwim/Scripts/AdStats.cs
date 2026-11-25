using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace TeumsaeSwim
{
    public sealed class AdStats : MonoBehaviour
    {
        private const string DbUrl = "https://networkpro2026-9c3fa-default-rtdb.asia-southeast1.firebasedatabase.app";
        public static AdStats Instance { get; private set; }

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Count(string kind)
        {
            if (kind != "interstitial" && kind != "rewarded") return;
            string uid = AuthManager.Uid;
            string key = "teumsae.ad_" + uid + "_" + kind;
            int count = PlayerPrefs.GetInt(key, 0) + 1;
            PlayerPrefs.SetInt(key, count);
            PlayerPrefs.Save();
            if (!string.IsNullOrEmpty(uid) && !string.IsNullOrEmpty(AuthManager.IdToken))
                StartCoroutine(Put(uid, kind, count));
        }

        private IEnumerator Put(string uid, string kind, int count)
        {
            string url = DbUrl + "/adstats/" + UnityWebRequest.EscapeURL(uid) + "/" + kind +
                         ".json?auth=" + UnityWebRequest.EscapeURL(AuthManager.IdToken);
            using (UnityWebRequest request = UnityWebRequest.Put(url, count.ToString()))
            {
                request.timeout = 20;
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                    Debug.Log("adstats " + kind + " = " + count);
                else Debug.LogWarning("adstats failed: " + request.responseCode + " " + request.error);
            }
        }
    }
}
