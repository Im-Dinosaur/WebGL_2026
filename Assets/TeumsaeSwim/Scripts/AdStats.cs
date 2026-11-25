using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace TeumsaeSwim
{
    public sealed class AdStats : MonoBehaviour
    {
        private const string DbUrl = "https://networkpro2026-9c3fa-default-rtdb.asia-southeast1.firebasedatabase.app";
        public static AdStats Instance { get; private set; }
        private bool sendingInterstitial;
        private bool sendingRewarded;

        private void Awake() { Instance = this; }
        private void Start() { Queue("interstitial"); Queue("rewarded"); }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Count(string kind)
        {
            if (kind != "interstitial" && kind != "rewarded") return;
            string uid = AuthManager.Uid;
            string key = "teumsae.ad_" + uid + "_" + kind;
            int count = PlayerPrefs.GetInt(key, 0) + 1;
            PlayerPrefs.SetInt(key, count);
            PlayerPrefs.Save();
            Queue(kind);
        }

        private void Queue(string kind)
        {
            string uid = AuthManager.Uid;
            if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(AuthManager.IdToken)) return;
            if (kind == "interstitial" ? sendingInterstitial : sendingRewarded) return;
            string key = "teumsae.ad_" + uid + "_" + kind;
            if (PlayerPrefs.GetInt(key, 0) <= PlayerPrefs.GetInt(key + "_sent", 0)) return;
            if (kind == "interstitial") sendingInterstitial = true;
            else sendingRewarded = true;
            StartCoroutine(Sync(uid, kind, key));
        }

        private IEnumerator Sync(string uid, string kind, string key)
        {
            // Allow an immediately following ad to pause first; send when it closes.
            yield return null;
            while (uid == AuthManager.Uid)
            {
                while (Time.timeScale == 0f && uid == AuthManager.Uid) yield return null;
                if (uid != AuthManager.Uid) break;
                int count = PlayerPrefs.GetInt(key, 0);
                if (count <= PlayerPrefs.GetInt(key + "_sent", 0)) break;
                string url = DbUrl + "/adstats/" + UnityWebRequest.EscapeURL(uid) + "/" + kind +
                             ".json?auth=" + UnityWebRequest.EscapeURL(AuthManager.IdToken);
                using (UnityWebRequest request = UnityWebRequest.Put(url, count.ToString()))
                {
                    request.timeout = 20;
                    request.SetRequestHeader("Content-Type", "application/json");
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        PlayerPrefs.SetInt(key + "_sent", count);
                        PlayerPrefs.Save();
                        Debug.Log("adstats " + kind + " = " + count);
                    }
                    else
                    {
                        Debug.LogWarning("adstats retry: " + request.responseCode + " " + request.error);
                        yield return new WaitForSecondsRealtime(3f);
                    }
                }
            }
            if (kind == "interstitial") sendingInterstitial = false;
            else sendingRewarded = false;
        }
    }
}
