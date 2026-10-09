using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TeumsaeSwim
{
    public sealed class GameUIComponent : MonoBehaviour
    {
        [SerializeField] private SwimArt art; //UI 글꼴과 도형
        private SwimGame game; //화면이 호출할 게임 진입점
        private Player player; //조작이 호출할 플레이어 진입점
        private RectTransform canvasRoot; //고정 화면의 루트
        private GameObject homePanel; //시작 화면
        private GameObject hudPanel; //플레이 정보와 조작
        private GameObject pausePanel; //일시정지 화면
        private GameObject resultPanel; //플레이 결과 화면
        private GameObject helpPanel; //조작 설명 화면
        private Text timerLabel; //경과 시간 표시
        private Text statusLabel; //은신과 위험 상태 표시
        private Text hintLabel; //상호작용 안내
        private Text baitLabel; //남은 미끼 표시
        private Text holdLabel; //누르고 있기 동작 안내
        private Text recordLabel; //홈의 최고 기록 표시
        private Text resultTitle; //성공과 실패 제목
        private Text resultTime; //결과의 경과 시간
        private Text resultHint; //결과의 설명
        private Text soundLabel; //음소거 상태 표시
        private Image progressBar; //출구 방향 진행 표시
        private Image netBar; //그물 작업 표시
        private TouchControlComponent stick; //이동 터치 컨트롤
        private TouchControlComponent hold; //누름 터치 컨트롤
        private readonly Image[] difficultyImages = new Image[3]; //난이도 선택 배경
        private SwimDifficulty selected = SwimDifficulty.Practice; //홈에서 선택한 난이도
        private float messageTime; //일시 알림의 남은 시간
        private string temporaryMessage = ""; //표시 중인 일시 알림
        private static readonly Color panelColor = new Color32(15, 43, 56, 255); //UI 판 색상
        private static readonly Color softWhite = new Color32(211, 231, 226, 255); //보조 글자 색상

        public void initialize(SwimGame owner, Player target) //고정 UI와 조작 연결
        {
            game = owner;
            player = target;
            var canvasObject = new GameObject("Swim canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); //고정 화면 캔버스
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>(); //UI 캔버스
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); //모바일 기준 화면 배율
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480, 854);
            scaler.matchWidthOrHeight = .5f;
            canvasRoot = canvasObject.GetComponent<RectTransform>();
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventObject = new GameObject("Swim input events", typeof(EventSystem), typeof(InputSystemUIInputModule)); //새 입력 시스템 UI 이벤트
                eventObject.transform.SetParent(transform, false);
                eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            createHud();
            createHome();
            createPause();
            createResult();
            createHelp();
            selectDifficulty(SwimDifficulty.Practice);
        }

        private void createHome() //첫 화면의 제목과 시작 동작 구성
        {
            homePanel = fullPanel("Home", SwimArt.ink);
            RectTransform root = homePanel.GetComponent<RectTransform>(); //홈 배치 루트
            label(root, "A LITTLE FISH. A BIG WAY OUT.", new Vector2(0, 359), new Vector2(430, 30), 12, SwimArt.mint);
            label(root, "틈새 헤엄", new Vector2(0, 299), new Vector2(420, 80), 53, Color.white);
            label(root, "작은 몸으로, 무사히 바깥으로.", new Vector2(0, 245), new Vector2(420, 42), 17, softWhite);
            for (int i = 0; i < 3; i++)
                picture(root, "Ocean ring", "ring", new Vector2(0, 105), Vector2.one * (136 + i * 54), new Color(.43f, .76f, .73f, .13f));
            Image tail = picture(root, "Fish tail", "triangle", new Vector2(-57, 101), new Vector2(66, 82), new Color32(226, 161, 58, 255)); //대표 물고기 꼬리
            tail.rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
            picture(root, "Fish body", "circle", new Vector2(9, 108), new Vector2(135, 94), SwimArt.yellow);
            picture(root, "Fish fin", "triangle", new Vector2(1, 72), new Vector2(42, 34), new Color32(225, 163, 58, 255));
            picture(root, "Fish eye white", "circle", new Vector2(48, 124), Vector2.one * 24, Color.white);
            picture(root, "Fish eye", "circle", new Vector2(53, 124), Vector2.one * 11, SwimArt.ink);
            picture(root, "Bubble", "ring", new Vector2(114, 158), Vector2.one * 14, SwimArt.mint);
            picture(root, "Bubble", "ring", new Vector2(132, 183), Vector2.one * 7, SwimArt.mint);
            label(root, "숨고 · 비집고 · 빠져나오기", new Vector2(0, -18), new Vector2(420, 30), 16, softWhite);
            for (int i = 0; i < 3; i++)
            {
                SwimDifficulty value = (SwimDifficulty)i; //버튼에 연결할 난이도
                Button item = button(root, difficultyName(value), new Vector2((i - 1) * 124, -84), new Vector2(114, 46), () => selectDifficulty(value), panelColor, Color.white); //난이도 선택 버튼
                difficultyImages[i] = item.image;
            }
            recordLabel = label(root, "", new Vector2(0, -132), new Vector2(420, 40), 13, softWhite);
            button(root, "헤엄 시작하기  →", new Vector2(0, -198), new Vector2(362, 62), () => game.begin(selected), SwimArt.yellow, SwimArt.ink, 21);
            button(root, "처음이라면, 헤엄 안내", new Vector2(0, -268), new Vector2(362, 44), () => helpPanel.SetActive(true), panelColor, softWhite, 15);
            label(root, "시간 제한 없이 내 속도로 탈출해 보세요.", new Vector2(0, -330), new Vector2(440, 30), 13, softWhite);
            label(root, "TEUMSAE SWIM  /  WEB EDITION", new Vector2(0, -380), new Vector2(420, 24), 10, new Color32(115, 154, 158, 255));
        }

        private void createHud() //고정 HUD와 두 손가락 조작 구성
        {
            hudPanel = fullPanel("HUD", Color.clear, false);
            RectTransform root = hudPanel.GetComponent<RectTransform>(); //플레이 UI 루트
            RectTransform top = anchoredPanel(root, "Top bar", new Vector2(.5f, 1), new Vector2(0, -49), new Vector2(480, 98), SwimArt.ink); //상단 정보 판
            label(top, "틈새 헤엄", new Vector2(-164, 18), new Vector2(120, 32), 20, Color.white);
            timerLabel = label(top, "00:00.0", new Vector2(101, 18), new Vector2(110, 34), 21, SwimArt.yellow);
            button(top, "Ⅱ", new Vector2(206, 18), new Vector2(44, 40), () => game.pause(), panelColor, Color.white, 23);
            statusLabel = label(top, "수면의 출구를 찾아 ↑", new Vector2(0, -19), new Vector2(440, 27), 14, softWhite);
            picture(top, "Progress track", "box", new Vector2(0, -46), new Vector2(480, 3), panelColor);
            progressBar = picture(top, "Progress", "box", new Vector2(-240, -46), new Vector2(480, 3), SwimArt.mint);
            progressBar.rectTransform.pivot = new Vector2(0, .5f);
            RectTransform bottom = anchoredPanel(root, "Controls", new Vector2(.5f, 0), new Vector2(0, 90), new Vector2(480, 180), SwimArt.ink); //하단 조작 판
            hintLabel = label(bottom, "이동: 스틱 / WASD / 방향키", new Vector2(0, 72), new Vector2(454, 29), 12, softWhite);
            Image baseImage = picture(bottom, "Move stick", "circle", new Vector2(-140, -3), Vector2.one * 126, new Color32(31, 66, 79, 255)); //스틱 바탕
            baseImage.raycastTarget = true;
            picture(baseImage.rectTransform, "Stick ring", "ring", Vector2.zero, Vector2.one * 122, new Color32(72, 117, 128, 255));
            Image knob = picture(baseImage.rectTransform, "Stick handle", "circle", Vector2.zero, Vector2.one * 55, new Color32(116, 190, 185, 255)); //스틱 손잡이
            label(knob.rectTransform, "+", Vector2.zero, Vector2.one * 36, 27, SwimArt.ink);
            stick = baseImage.gameObject.AddComponent<TouchControlComponent>();
            stick.initialize(player, knob.rectTransform, false);
            label(bottom, "이동", new Vector2(-140, -74), new Vector2(100, 24), 11, softWhite);
            Button baitButton = button(bottom, "", new Vector2(20, -13), Vector2.one * 70, () => player.useBait(), new Color32(33, 68, 78, 255), SwimArt.yellow); //미끼 버튼
            baitButton.image.sprite = art.sprite("circle");
            baitLabel = label(baitButton.GetComponent<RectTransform>(), "미끼\n2", Vector2.zero, new Vector2(68, 57), 16, SwimArt.yellow);
            label(bottom, "E · 순찰 유인", new Vector2(20, -74), new Vector2(115, 24), 10, softWhite);
            Image holdImage = picture(bottom, "Hold action", "circle", new Vector2(140, -3), Vector2.one * 102, SwimArt.yellow); //틈 통과와 그물 작업 버튼
            holdImage.raycastTarget = true;
            holdLabel = label(holdImage.rectTransform, "꾹\n누르기", Vector2.zero, new Vector2(92, 76), 20, SwimArt.ink);
            hold = holdImage.gameObject.AddComponent<TouchControlComponent>();
            hold.initialize(player, null, true);
            label(bottom, "SPACE · 틈 / 그물", new Vector2(140, -74), new Vector2(142, 24), 10, softWhite);
            netBar = picture(bottom, "Net work", "box", new Vector2(89, -59), new Vector2(102, 4), SwimArt.mint);
            netBar.rectTransform.pivot = new Vector2(0, .5f);
        }

        private void createPause() //일시정지 선택 화면
        {
            pausePanel = fullPanel("Pause", new Color(.025f, .08f, .12f, .96f));
            RectTransform root = pausePanel.GetComponent<RectTransform>(); //일시정지 배치 루트
            label(root, "잠깐, 숨 고르기", new Vector2(0, 170), new Vector2(430, 65), 34, Color.white);
            label(root, "물고기도 시간도 기다리고 있어.", new Vector2(0, 114), new Vector2(430, 40), 16, softWhite);
            button(root, "계속 헤엄치기", new Vector2(0, 23), new Vector2(348, 60), () => game.resume(), SwimArt.yellow, SwimArt.ink, 20);
            Button sound = button(root, "", new Vector2(0, -54), new Vector2(348, 48), () => game.toggleMute(), panelColor, Color.white, 16); //음소거 선택 버튼
            soundLabel = sound.GetComponentInChildren<Text>();
            button(root, "시작 화면으로", new Vector2(0, -123), new Vector2(348, 48), () => game.home(), panelColor, softWhite, 16);
            label(root, "다른 창으로 이동하면 자동으로 멈춥니다.", new Vector2(0, -222), new Vector2(440, 40), 13, softWhite);
        }

        private void createResult() //성공과 실패 결과 화면
        {
            resultPanel = fullPanel("Result", new Color(.025f, .08f, .12f, .97f));
            RectTransform root = resultPanel.GetComponent<RectTransform>(); //결과 배치 루트
            label(root, "THE END OF THIS SWIM", new Vector2(0, 252), new Vector2(420, 30), 12, SwimArt.mint);
            resultTitle = label(root, "", new Vector2(0, 175), new Vector2(430, 74), 39, Color.white);
            resultTime = label(root, "", new Vector2(0, 76), new Vector2(440, 70), 45, SwimArt.yellow);
            resultHint = label(root, "", new Vector2(0, -7), new Vector2(400, 78), 15, softWhite);
            button(root, "같은 미로 다시 도전", new Vector2(0, -120), new Vector2(354, 60), () => game.begin(game.difficulty, false), SwimArt.yellow, SwimArt.ink, 19);
            button(root, "새 미로 헤엄치기", new Vector2(0, -195), new Vector2(354, 49), () => game.begin(game.difficulty), panelColor, Color.white, 16);
            button(root, "시작 화면으로", new Vector2(0, -260), new Vector2(354, 44), () => game.home(), panelColor, softWhite, 15);
        }

        private void createHelp() //첫 플레이에 필요한 핵심 규칙 안내
        {
            helpPanel = fullPanel("Help", SwimArt.ink);
            RectTransform root = helpPanel.GetComponent<RectTransform>(); //도움말 배치 루트
            label(root, "헤엄 안내", new Vector2(0, 341), new Vector2(430, 62), 36, Color.white);
            label(root, "위쪽의 빛나는 출구까지 가면 성공!", new Vector2(0, 286), new Vector2(430, 38), 16, SwimArt.mint);
            string[] titles = { "01   천천히, 자유롭게", "02   작은 몸이 가진 힘", "03   풀 속에서 숨 고르기", "04   눈을 마주치지 않기" }; //도움말 항목 제목
            string[] descriptions = {
                "왼쪽 스틱으로 헤엄쳐요.\nPC는 WASD 또는 방향키를 사용해요.",
                "틈에서는 ‘꾹’을 누르며 이동해요.\n그물 앞에서는 멈춰서 2초간 눌러요.",
                "풀 안에 몸이 완전히 들어가면 숨어요.\n숨어도 포식자와 닿으면 잡혀요.",
                "? 표시가 1초 채워지면 ! 추격이 시작돼요.\n미끼는 순찰 중인 포식자에게만 통해요."
            }; //도움말 항목 설명
            for (int i = 0; i < 4; i++)
            {
                float y = 196 - i * 123; //설명 카드 높이
                picture(root, "Help card", "round", new Vector2(0, y - 15), new Vector2(410, 111), panelColor);
                label(root, titles[i], new Vector2(0, y + 14), new Vector2(366, 31), 18, SwimArt.yellow, TextAnchor.MiddleLeft);
                label(root, descriptions[i], new Vector2(0, y - 31), new Vector2(366, 59), 14, softWhite, TextAnchor.MiddleLeft);
            }
            label(root, "SPACE = 꾹 누르기   ·   E = 미끼   ·   ESC = 멈춤", new Vector2(0, -283), new Vector2(452, 31), 12, softWhite);
            button(root, "알겠어, 헤엄칠 준비 됐어", new Vector2(0, -346), new Vector2(376, 58), () => helpPanel.SetActive(false), SwimArt.yellow, SwimArt.ink, 18);
            helpPanel.SetActive(false);
        }

        public void showPhase() //게임 단계에 맞는 화면만 표시
        {
            if (homePanel == null) return;
            stick.resetControl();
            hold.resetControl();
            homePanel.SetActive(game.phase == SwimPhase.Home);
            hudPanel.SetActive(game.phase != SwimPhase.Home);
            pausePanel.SetActive(game.phase == SwimPhase.Paused);
            resultPanel.SetActive(game.phase == SwimPhase.Won || game.phase == SwimPhase.Lost);
            helpPanel.SetActive(false);
            if (game.phase == SwimPhase.Home) selectDifficulty(selected);
            if (game.phase == SwimPhase.Playing) { messageTime = 0; temporaryMessage = ""; }
            refreshSound();
            if (game.phase == SwimPhase.Won || game.phase == SwimPhase.Lost)
            {
                bool won = game.phase == SwimPhase.Won; //탈출 성공 여부
                resultTitle.text = won ? "바깥바다에 도착!" : "앗, 들켜 버렸어";
                resultTitle.color = won ? SwimArt.mint : SwimArt.coral;
                resultTime.text = formatTime(game.elapsed);
                resultHint.text = won ? (game.newRecord ? "이 브라우저의 새로운 최고 기록!\n" : "무사히 빠져나왔어. 잘 헤엄쳤어.\n") + difficultyName(game.difficulty) + " · 최고 " + formatTime(game.bestTime(game.difficulty))
                    : "바위 뒤나 수풀 안으로 몸을 숨겨 봐.\n틈새에서는 포식자가 따라오지 못해.";
            }
        }

        public void tick(float deltaTime) //시간과 위험 및 상황별 조작 안내 갱신
        {
            timerLabel.text = formatTime(game.elapsed);
            progressBar.rectTransform.sizeDelta = new Vector2(480 * game.progress, 3);
            baitLabel.text = "미끼\n" + player.baitCount;
            statusLabel.text = player.hidden ? "●  수풀 안에 숨었어 · 접촉은 조심!" : !string.IsNullOrEmpty(game.danger) ? game.danger : "수면의 출구를 찾아 ↑";
            statusLabel.color = player.hidden ? SwimArt.mint : string.IsNullOrEmpty(game.danger) ? softWhite : SwimArt.coral;
            if (!string.IsNullOrEmpty(player.message)) { temporaryMessage = player.message; messageTime = 3f; }
            messageTime -= deltaTime;
            hintLabel.text = messageTime > 0 ? temporaryMessage : player.nearNet ? "그물 앞에서 멈춘 뒤 꾹 · 진행도는 유지돼요" : player.squeezed ? "틈 안에서는 꾹 누른 채 움직여요" : "풀에 숨고, 바위 뒤로 돌아서 출구까지";
            holdLabel.text = player.working ? "그물\n" + Mathf.RoundToInt(player.netProgress * 100) + "%" : player.nearNet ? "그물\n꾹" : "꾹\n누르기";
            netBar.gameObject.SetActive(player.nearNet);
            netBar.rectTransform.sizeDelta = new Vector2(102 * player.netProgress, 4);
        }

        public void refreshSound() //음소거 버튼 문구 갱신
        {
            if (soundLabel != null) soundLabel.text = game.muted ? "소리 켜기" : "소리 끄기";
        }

        private void selectDifficulty(SwimDifficulty value) //난이도 선택과 기록 표시
        {
            selected = value;
            for (int i = 0; i < 3; i++) difficultyImages[i].color = i == (int)value ? new Color32(53, 121, 119, 255) : panelColor;
            float best = game.bestTime(value); //선택 난이도의 기록
            string count = value == SwimDifficulty.Practice ? "포식자 1마리 · 느긋한 연습" : value == SwimDifficulty.Normal ? "포식자 2마리 · 기본 헤엄" : "포식자 2마리 · 긴 물길, 빠른 추격"; //난이도 설명
            recordLabel.text = count + (best > 0 ? "\n최고 " + formatTime(best) : "");
        }

        public static string formatTime(float time) //읽기 쉬운 분과 초 표시
        {
            int minutes = Mathf.FloorToInt(time / 60); //경과 분
            return minutes.ToString("00") + ":" + (time % 60).ToString("00.0", System.Globalization.CultureInfo.InvariantCulture);
        }

        private string difficultyName(SwimDifficulty value) //난이도 한국어 이름
        {
            return value == SwimDifficulty.Practice ? "연습" : value == SwimDifficulty.Normal ? "기본" : "도전";
        }

        private GameObject fullPanel(string name, Color color, bool block = true) //화면 전체 패널 생성
        {
            Image image = picture(canvasRoot, name, "box", Vector2.zero, Vector2.zero, color); //패널 배경
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
            image.raycastTarget = block;
            return image.gameObject;
        }

        private RectTransform anchoredPanel(RectTransform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color) //화면 모서리에 고정된 패널
        {
            Image image = picture(parent, name, "box", position, size, color); //고정 패널
            image.rectTransform.anchorMin = anchor;
            image.rectTransform.anchorMax = anchor;
            image.raycastTarget = true;
            return image.rectTransform;
        }

        private Image picture(RectTransform parent, string name, string kind, Vector2 position, Vector2 size, Color color) //장식과 버튼 배경 이미지
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Image)); //UI 이미지 오브젝트
            item.transform.SetParent(parent, false);
            var image = item.GetComponent<Image>(); //이미지 컴포넌트
            image.sprite = art.sprite(kind);
            image.color = color;
            image.raycastTarget = false;
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, .5f);
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            return image;
        }

        private Text label(RectTransform parent, string value, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleCenter) //한국어 UI 텍스트 생성
        {
            var item = new GameObject("Text " + value, typeof(RectTransform), typeof(Text)); //텍스트 오브젝트
            item.transform.SetParent(parent, false);
            Text text = item.GetComponent<Text>(); //텍스트 컴포넌트
            text.font = art.font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(.5f, .5f);
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = size;
            return text;
        }

        private Button button(RectTransform parent, string value, Vector2 position, Vector2 size, Action action, Color background, Color foreground, int fontSize = 17) //키보드와 터치로 누를 수 있는 버튼
        {
            Image image = picture(parent, "Button " + value, "round", position, size, background); //버튼 배경
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>(); //클릭 동작
            button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            Navigation navigation = button.navigation; //키 입력이 게임 조작과 겹치지 않게 설정
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            label(image.rectTransform, value, Vector2.zero, size - new Vector2(16, 4), fontSize, foreground);
            return button;
        }
    }
}
