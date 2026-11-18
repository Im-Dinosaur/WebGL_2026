using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace TeumsaeSwim
{
    public sealed class ScoreSaver : MonoBehaviour
    {
        private const string DbUrl = "https://networkpro2026-9c3fa-default-rtdb.asia-southeast1.firebasedatabase.app";
        public const string LocalBestKey = "teumsae.bestStars";
        public static ScoreSaver Instance { get; private set; }
        public string Status { get; private set; } = "";
        public event Action<string> StatusChanged;
        private bool busy;
        private int pending;

        private void Awake() { Instance = this; }
        private void SetStatus(string status)
        {
            Status = status;
            StatusChanged?.Invoke(status);
        }

        public void Save(int stars)
        {
            if (string.IsNullOrEmpty(AuthManager.Uid) || stars <= 0) return;
            pending = Mathf.Max(pending, stars);
            if (!busy) StartCoroutine(SavePending());
        }

        private IEnumerator SavePending()
        {
            busy = true;
            while (pending > 0 && !string.IsNullOrEmpty(AuthManager.Uid))
            {
                int score = pending;
                pending = 0;
                string uid = AuthManager.Uid;
                string url = DbUrl + "/scores/" + UnityWebRequest.EscapeURL(uid)
                    + ".json?auth=" + UnityWebRequest.EscapeURL(AuthManager.IdToken);
                SetStatus("서버 최고 별 기록 확인 중...");
                using (UnityWebRequest get = UnityWebRequest.Get(url))
                {
                    get.timeout = 20;
                    yield return get.SendWebRequest();
                    if (get.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning("Firebase score read failed: " + get.responseCode + " " + get.error);
                        SetStatus("서버 기록 확인 실패 · 다시 로그인해 주세요.");
                        break;
                    }
                    string old = get.downloadHandler.text;
                    int best = 0;
                    if (!string.IsNullOrWhiteSpace(old) && old.Trim() != "null")
                    {
                        ScoreData existing;
                        try { existing = JsonUtility.FromJson<ScoreData>(old); }
                        catch (Exception)
                        {
                            Debug.LogWarning("Firebase score response is not valid JSON.");
                            SetStatus("서버 기록을 읽지 못했어요.");
                            break;
                        }
                        if (existing == null) { SetStatus("서버 기록을 읽지 못했어요."); break; }
                        best = existing.score;
                        if (best > PlayerPrefs.GetInt(LocalBestKey, 0))
                        {
                            PlayerPrefs.SetInt(LocalBestKey, best);
                            PlayerPrefs.Save();
                        }
                    }
                    if (score <= best) { SetStatus("서버 최고 별 " + best + "개"); continue; }
                }
                if (uid != AuthManager.Uid) break;
                var data = new ScoreData { name = AuthManager.GuestName, score = score };
                using (UnityWebRequest put = UnityWebRequest.Put(url, JsonUtility.ToJson(data)))
                {
                    put.timeout = 20;
                    put.SetRequestHeader("Content-Type", "application/json");
                    yield return put.SendWebRequest();
                    if (put.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning("Firebase score save failed: " + put.responseCode + " " + put.error);
                        SetStatus("서버 저장 실패 · 다시 로그인해 주세요.");
                        break;
                    }
                    SetStatus("서버에 최고 별 " + score + "개 저장됨");
                    Debug.Log("Firebase best-star score saved.");
                }
            }
            busy = false;
        }

        [Serializable]
        private sealed class ScoreData { public string name; public int score; }
    }
}
