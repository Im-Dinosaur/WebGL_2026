using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace TeumsaeSwim
{
    public sealed class AuthManager : MonoBehaviour
    {
        // Firebase web API keys identify the project; database rules enforce access.
        private const string ApiKey = "AIzaSyASrx9gcOxMiZC4L1sq1DfqvK11ahZkN2Q";
        private const string RefreshKey = "teumsae.firebase.refreshToken";
        public static AuthManager Instance { get; private set; }
        public static string Uid { get; private set; } = "";
        public static string IdToken { get; private set; } = "";
        public static string GuestName => Uid.Length >= 6 ? "손님 " + Uid.Substring(0, 6) : "";
        public event Action<string> StatusChanged;
        private bool signingIn;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (GetComponent<ScoreSaver>() == null) gameObject.AddComponent<ScoreSaver>();
        }

        public void GuestLogin()
        {
            if (!signingIn) StartCoroutine(SignIn());
        }

        private IEnumerator SignIn()
        {
            signingIn = true;
            StatusChanged?.Invoke("로그인 중...");
            string saved = PlayerPrefs.GetString(RefreshKey, "");
            bool first = string.IsNullOrEmpty(saved);
            string url = first
                ? "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=" + ApiKey
                : "https://securetoken.googleapis.com/v1/token?key=" + ApiKey;
            string body = first ? "{\"returnSecureToken\":true}"
                : "grant_type=refresh_token&refresh_token=" + UnityWebRequest.EscapeURL(saved);
            string type = first ? "application/json" : "application/x-www-form-urlencoded";
            using (UnityWebRequest req = UnityWebRequest.Post(url, body, type))
            {
                req.timeout = 20;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    if (!first && req.responseCode == 400)
                    {
                        PlayerPrefs.DeleteKey(RefreshKey);
                        PlayerPrefs.Save();
                        StatusChanged?.Invoke("저장된 로그인이 만료됐어요. 다시 눌러 주세요.");
                    }
                    else StatusChanged?.Invoke("로그인하지 못했어요. 인터넷 연결을 확인한 뒤 다시 눌러 주세요.");
                    Debug.LogWarning("Firebase guest login failed: " + req.responseCode + " " + req.error);
                    signingIn = false;
                    yield break;
                }
                AuthResponse result = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
                string uid = first ? result.localId : result.user_id;
                string token = first ? result.idToken : result.id_token;
                string refresh = first ? result.refreshToken : result.refresh_token;
                if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refresh))
                {
                    StatusChanged?.Invoke("로그인 응답을 확인하지 못했어요. 다시 시도해 주세요.");
                    signingIn = false;
                    yield break;
                }
                Uid = uid;
                IdToken = token;
                PlayerPrefs.SetString(RefreshKey, refresh);
                PlayerPrefs.Save();
            }
            signingIn = false;
            ScoreSaver.Instance.Save(PlayerPrefs.GetInt(ScoreSaver.LocalBestKey, 0));
            SceneManager.LoadScene("TeumsaeSwim");
        }

        public static void Logout()
        {
            Uid = "";
            IdToken = "";
            SceneManager.LoadScene("Login");
        }

        [Serializable]
        private sealed class AuthResponse
        {
            public string idToken, localId, refreshToken, id_token, user_id, refresh_token;
        }
    }
}
