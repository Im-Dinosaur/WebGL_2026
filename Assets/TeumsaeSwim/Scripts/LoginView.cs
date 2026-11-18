using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TeumsaeSwim
{
    public sealed class LoginView : MonoBehaviour
    {
        [SerializeField] private Font koreanFont;
        private Text status;
        private AuthManager auth;

        private void Start()
        {
            auth = AuthManager.Instance;
            auth.StatusChanged += SetStatus;
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Login input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            var root = new GameObject("Login Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480, 854);
            scaler.matchWidthOrHeight = .5f;
            var bg = MakeRect(root.transform, "Background", Vector2.zero, new Vector2(480, 854));
            bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.offsetMin = bg.offsetMax = Vector2.zero;
            bg.gameObject.AddComponent<Image>().color = new Color32(10, 42, 61, 255);
            MakeText(root.transform, "숨바꼬기", new Vector2(0, 155), 38, new Color32(255, 242, 203, 255));
            MakeText(root.transform, "작은 물고기의 바닷속 탈출", new Vector2(0, 100), 19, new Color32(164, 205, 208, 255));
            var buttonRect = MakeRect(root.transform, "Guest login", new Vector2(0, -10), new Vector2(340, 68));
            var image = buttonRect.gameObject.AddComponent<Image>();
            image.color = new Color32(255, 177, 93, 255);
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(auth.GuestLogin);
            var caption = MakeText(buttonRect, "게스트 로그인", Vector2.zero, 24, new Color32(22, 54, 65, 255));
            caption.raycastTarget = false;
            status = MakeText(root.transform, "같은 브라우저에서는 같은 손님으로 로그인해요.", new Vector2(0, -90), 15, Color.white);
        }

        private void SetStatus(string message) { if (status != null) status.text = message; }
        private void OnDestroy() { if (auth != null) auth.StatusChanged -= SetStatus; }

        private static RectTransform MakeRect(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            var rt = obj.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        private Text MakeText(Transform parent, string value, Vector2 pos, int size, Color color)
        {
            var rt = MakeRect(parent, value, pos, new Vector2(440, 60));
            var label = rt.gameObject.AddComponent<Text>();
            label.font = koreanFont;
            label.text = value;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }
    }
}
