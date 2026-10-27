using System;
using UnityEngine;
using UnityEngine.UI;

namespace TeumsaeSwim
{
    public sealed class GameUIStyleComponent : MonoBehaviour
    {
        [SerializeField] private Sprite oceanBackground; //홈 바닷속 그림
        [SerializeField] private Sprite homeBackground; //개편한 홈 전용 숨바꼬기 그림
        [SerializeField] private Sprite titleLogo; //소제목 없는 숨바꼬기 로고
        [SerializeField] private Sprite playerFish; //메인 화면 물고기 그림
        private SwimArt art; //글꼴과 기본 도형 자원
        private Canvas canvas; //실제 표시 캔버스
        private RectTransform safeRoot; //노치와 홈 영역을 제외한 배치 루트
        private Vector2 lastSize; //직전 화면 크기
        private Rect lastSafe; //직전 안전 영역
        private readonly System.Collections.Generic.List<RectTransform> sheets = new System.Collections.Generic.List<RectTransform>(); //고정 비율 메뉴들
        private readonly System.Collections.Generic.List<RectTransform> bars = new System.Collections.Generic.List<RectTransform>(); //위아래 고정 HUD들
        public RectTransform root { get; private set; } //전체 화면 루트
        public RectTransform safe => safeRoot; //안전 배치 영역
        public Sprite fish => playerFish; //공통 물고기 그림
        public float controlScale { get; private set; } = 1f; //작은 화면에서 조작과 알림에 공유할 배율
        public Rect playfield { get; private set; } = new Rect(0, .23f, 1, .61f); //조작판 사이의 가시 영역
        public static readonly Color cream = new Color32(255, 242, 203, 255); //주요 글자 색
        public static readonly Color muted = new Color32(164, 205, 208, 255); //보조 글자 색
        public static readonly Color panel = new Color32(9, 42, 58, 245); //정보 판 색
        public static readonly Color border = new Color32(60, 133, 151, 255); //윤곽선 색

        public void initialize(SwimArt resource) //공통 캔버스와 안전 영역 구성
        {
            art = resource;
            root = rect(transform, "Swim canvas", Vector2.zero, Vector2.zero);
            canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            CanvasScaler scaler = root.gameObject.AddComponent<CanvasScaler>(); //너비 기준 모바일 UI 배율
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480, 854);
            scaler.matchWidthOrHeight = 0;
            root.gameObject.AddComponent<GraphicRaycaster>();
            safeRoot = rect(root, "Safe area", Vector2.zero, Vector2.zero);
            stretch(safeRoot);
        }

        private void LateUpdate() //회전과 화면 크기 변화에 맞춰 재배치
        {
            if (root == null) return;
            Vector2 size = new Vector2(Screen.width, Screen.height); //현재 픽셀 크기
            if (size != lastSize || Screen.safeArea != lastSafe) layout(size, Screen.safeArea);
        }

        public void layout(Vector2 screenSize, Rect safeArea) //실기기와 렌더 검증에서 동일한 안전 영역 배치
        {
            if (screenSize.x <= 0 || screenSize.y <= 0) return;
            lastSize = screenSize;
            lastSafe = safeArea;
            safeRoot.anchorMin = safeArea.min / screenSize;
            safeRoot.anchorMax = safeArea.max / screenSize;
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            float factor = Mathf.Max(.01f, canvas.scaleFactor); //캔버스 픽셀 환산 배율
            Vector2 size = safeArea.size / factor; //안전 영역의 UI 크기
            float menuScale = Mathf.Min(size.x / 480f, size.y / 854f); //메뉴 전체를 담는 배율
            float hudScale = Mathf.Min(size.x / 480f, size.y / 640f); //가로 모드에서도 조작판이 겹치지 않는 배율
            controlScale = hudScale;
            foreach (RectTransform sheet in sheets) sheet.localScale = Vector3.one * menuScale;
            foreach (RectTransform bar in bars) bar.localScale = Vector3.one * hudScale;
            float bottom = safeArea.yMin + 190f * hudScale * factor; //조작판 위 경계
            float top = safeArea.yMax - 140f * hudScale * factor; //헤더 아래 경계
            playfield = new Rect(0, bottom / screenSize.y, 1, Mathf.Max(1, top - bottom) / screenSize.y);
        }

        public Vector2 viewportToSafePoint(Vector2 viewport) //카메라 투영 갱신 순서와 무관하게 UI 좌표 계산
        {
            return (viewport * lastSize - lastSafe.center) / Mathf.Max(.01f, canvas.scaleFactor);
        }

        public RectTransform screen(string name, bool backdrop, bool block = true) //전체 화면과 필요할 때 바닷속 배경 추가
        {
            RectTransform outer = rect(safeRoot, name, Vector2.zero, Vector2.zero); //화면 루트
            stretch(outer);
            if (block)
            {
                Image blocker = outer.gameObject.AddComponent<Image>(); //화면 뒤 조작을 막는 투명 판
                blocker.color = Color.clear;
                blocker.raycastTarget = true;
            }
            if (backdrop)
            {
                Image background = picture(outer, "Ocean background", "box", Vector2.zero, Vector2.zero, Color.white); //장식 전용 바다
                Sprite backdropSprite = name == "Home" && homeBackground != null ? homeBackground : oceanBackground; //홈과 모달 배경을 구분
                background.sprite = backdropSprite;
                stretch(background.rectTransform);
                AspectRatioFitter fit = background.gameObject.AddComponent<AspectRatioFitter>(); //화면을 빈틈없이 채우는 그림
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = backdropSprite == null ? 9f / 16f : backdropSprite.rect.width / backdropSprite.rect.height;
            }
            return outer;
        }

        public RectTransform sheet(RectTransform parent, string name) //작은 화면에도 들어가는 세로 메뉴
        {
            RectTransform item = rect(parent, name, Vector2.zero, new Vector2(480, 854)); //메뉴 배치 기준
            sheets.Add(item);
            return item;
        }

        public RectTransform bar(RectTransform parent, string name, bool top, float height) //안전 영역 위아래에 붙는 HUD
        {
            RectTransform item = rect(parent, name, Vector2.zero, new Vector2(480, height)); //고정 HUD 배치 기준
            item.anchorMin = item.anchorMax = new Vector2(.5f, top ? 1 : 0);
            bars.Add(item);
            return item;
        }

        public Image card(RectTransform parent, string name, Vector2 position, Vector2 size, Color fill, Color outline) //둥근 윤곽과 안쪽 면을 가진 판
        {
            Image edge = picture(parent, name, "pill", position, size, outline); //외곽선 이미지
            Image center = picture(edge.rectTransform, "Face", "pill", Vector2.zero, size - Vector2.one * 3, fill); //안쪽 면
            center.rectTransform.localPosition = Vector3.zero;
            return edge;
        }

        public Button button(RectTransform parent, string value, Vector2 position, Vector2 size, Action action, bool primary = false, int fontSize = 20) //누름 반응을 갖춘 큰 버튼
        {
            Image edge = card(parent, "Button " + value, position + new Vector2(0, -3), size, primary ? new Color32(218, 157, 53, 255) : panel, primary ? new Color32(233, 183, 66, 255) : border); //아래 그림자와 테두리
            Image face = picture(edge.rectTransform, "Button face", "pill", new Vector2(0, 4), size - new Vector2(4, 4), primary ? SwimArt.yellow : panel); //실제 입력 면
            face.raycastTarget = true;
            Button item = edge.gameObject.AddComponent<Button>(); //클릭 처리
            edge.raycastTarget = true;
            item.targetGraphic = face;
            item.onClick.AddListener(() => action());
            ColorBlock colors = item.colors; //누르는 동안 색 변화
            colors.pressedColor = new Color(.73f, .88f, .86f);
            colors.highlightedColor = new Color(.93f, 1f, 1f);
            item.colors = colors;
            item.navigation = new Navigation { mode = Navigation.Mode.None };
            text(face.rectTransform, value, Vector2.zero, size - new Vector2(20, 4), fontSize, primary ? SwimArt.ink : cream, TextAnchor.MiddleCenter, true);
            return item;
        }

        public Button startButton(RectTransform parent, Vector2 position, Action action) //홈 전용 산호색 시작 버튼과 밝은 윗면
        {
            Button item = button(parent, "게임 시작", position, new Vector2(400, 74), action, true, 29); //실제 게임 시작 입력
            item.GetComponent<Image>().color = new Color32(255, 177, 93, 255);
            item.transform.Find("Face").GetComponent<Image>().color = new Color32(200, 84, 67, 255);
            item.targetGraphic.color = new Color32(255, 159, 112, 255);
            Image shine = picture(item.targetGraphic.rectTransform, "Top highlight", "pill", new Vector2(0, 28), new Vector2(339, 3), new Color32(255, 227, 171, 235)); //눌림 면의 부드러운 광택
            shine.transform.SetAsFirstSibling();
            return item;
        }

        public void logo(RectTransform parent, Vector2 position, Vector2 size) //그림 로고 표시와 자원 누락 시 제목 유지
        {
            if (titleLogo != null) illustration(parent, "숨바꼬기", titleLogo, position, size);
            else text(parent, "숨바꼬기", position, size, 36, cream, TextAnchor.MiddleCenter, true);
        }

        public Image illustration(RectTransform parent, string name, Sprite sprite, Vector2 position, Vector2 size) //비율을 유지하는 제작 그림
        {
            Image item = picture(parent, name, "box", position, size, Color.white); //그림 이미지
            item.sprite = sprite;
            item.preserveAspect = true;
            return item;
        }

        public Image picture(RectTransform parent, string name, string kind, Vector2 position, Vector2 size, Color color) //장식 이미지 생성
        {
            RectTransform item = rect(parent, name, position, size); //이미지 배치
            Image image = item.gameObject.AddComponent<Image>(); //기본 UI 이미지
            image.sprite = art.sprite(kind == "circle" || kind == "ring" ? "ui-" + kind : kind);
            image.type = kind == "pill" || kind == "round" ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = kind == "pill" ? 1.6f : 1;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public Text text(RectTransform parent, string value, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleCenter, bool bold = false) //읽기 쉬운 한국어 문구 배치
        {
            Text item = rect(parent, "Text " + value, position, size).gameObject.AddComponent<Text>(); //문구 컴포넌트
            item.font = art.font;
            item.text = value;
            item.fontSize = fontSize;
            item.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            item.color = color;
            item.alignment = alignment;
            item.raycastTarget = false;
            item.horizontalOverflow = HorizontalWrapMode.Wrap;
            item.verticalOverflow = VerticalWrapMode.Truncate;
            return item;
        }

        public GameUIIconComponent icon(RectTransform parent, SwimUIIcon kind, Vector2 position, float size, Color color, bool filled = false) //공통 벡터 기호 배치
        {
            GameUIIconComponent item = rect(parent, "Icon " + kind, position, Vector2.one * size).gameObject.AddComponent<GameUIIconComponent>(); //기호 컴포넌트
            item.configure(kind, color, filled);
            return item;
        }

        public RectTransform rect(Transform parent, string name, Vector2 position, Vector2 size) //중앙 기준 UI 영역 생성
        {
            var item = new GameObject(name, typeof(RectTransform)); //UI 오브젝트
            item.transform.SetParent(parent, false);
            RectTransform area = item.GetComponent<RectTransform>(); //위치와 크기
            area.anchorMin = area.anchorMax = area.pivot = new Vector2(.5f, .5f);
            area.anchoredPosition = position;
            area.sizeDelta = size;
            return area;
        }

        public void stretch(RectTransform area) //부모 영역 전체에 맞춤
        {
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = area.offsetMax = Vector2.zero;
        }
    }
}
