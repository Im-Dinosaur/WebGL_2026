using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static ZombieCqb.ZombieCqbSceneSetup;

namespace ZombieCqb
{
    internal static class ZombieCqbSceneUI
    {
        private static readonly Color32 ink = new Color32(12, 21, 27, 255); //화면의 짙은 기본색
        private static readonly Color32 panelColor = new Color32(23, 36, 42, 255); //메뉴 판 색
        private static readonly Color32 cardColor = new Color32(34, 51, 56, 255); //강화 선택 카드 색
        private static readonly Color32 pale = new Color32(232, 243, 231, 255); //주요 글자색
        private static readonly Color32 dim = new Color32(161, 185, 175, 255); //보조 글자색
        private static readonly Color32 mint = new Color32(165, 241, 176, 255); //감염원과 선택의 강조색
        private static readonly Color32 amber = new Color32(244, 198, 116, 255); //특수능력 분류 강조색
        private static readonly Color32 blue = new Color32(154, 207, 232, 255); //기본 능력치 분류 강조색
        private static Font font; //한국어 게임 화면 글꼴

        internal static ZombieCqbUIComponent create(Sprite player, Sprite civilian, Sprite guard) //HUD와 세 메뉴 화면 구성
        {
            font = AssetDatabase.LoadAssetAtPath<Font>(root + "/Fonts/NotoSansKR-Regular.otf");
            if (font == null)
                throw new InvalidOperationException("Zombie CQB font is missing.");

            GameObject screen = new GameObject("Game Screen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); //게임 전체 화면 캔버스
            Canvas canvas = screen.GetComponent<Canvas>(); //화면 공간 캔버스
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = screen.GetComponent<CanvasScaler>(); //화면 크기에 따른 UI 배율
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            ZombieCqbUIComponent ui = screen.AddComponent<ZombieCqbUIComponent>(); //런타임에서 화면을 조작할 컴포넌트
            RectTransform hud = stretch("HUD", screen.transform); //플레이 중 정보 화면
            RectTransform header = panel("Header", hud, 0, 323, 1280, 74, new Color32(13, 23, 29, 247)); //상단 플레이 정보 띠
            panel("Header Line", header, 0, -36, 1280, 2, new Color32(60, 88, 81, 255));
            text("Game Name", header, -477, 17, 260, 23, 13, "감염 구역", mint, TextAnchor.MiddleLeft, true);
            Text stageText = text("Stage", header, -477, -10, 260, 30, 20, "01 / 02    폐쇄 진료소", pale, TextAnchor.MiddleLeft, true); //스테이지 이름과 번호
            text("Health Label", header, -139, 14, 56, 26, 15, "체력", dim);
            Text healthText = text("Health", header, 87, 14, 122, 33, 17, "160 / 160", pale, TextAnchor.MiddleRight, true); //남은 체력 수치
            panel("Health Track", header, 0, -9, 302, 12, new Color32(48, 68, 67, 255));
            Image healthFill = panel("Health Fill", header, 0, -9, 302, 12, mint).GetComponent<Image>(); //체력 게이지 채움
            healthFill.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(root + "/Art/Pixel.png");
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            Text infection = text("Infections", header, 464, -10, 290, 30, 20, "감염 00 / 12      XP 00", pale, TextAnchor.MiddleRight, true); //감염과 경험치
            Text status = text("Status", header, 464, 17, 290, 24, 15, "탐색 중", dim, TextAnchor.MiddleRight); //경비 발견과 은신 상태

            RectTransform footer = panel("Controls", hud, 0, -329, 1280, 62, new Color32(13, 23, 29, 249)); //하단 기본 조작 안내
            panel("Footer Line", footer, 0, 30, 1280, 2, new Color32(60, 88, 81, 255));
            text("Keys", footer, -358, 0, 505, 35, 17, "WASD  이동     E  문·탈출     SPACE  접촉 감염", pale, TextAnchor.MiddleLeft);
            RectTransform abilityList = rect("Abilities", footer, 260, 0, 720, 45); //해금된 능력들의 표시 공간
            HorizontalLayoutGroup layout = abilityList.gameObject.AddComponent<HorizontalLayoutGroup>(); //해금된 항목을 한 줄에 정렬
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.spacing = 8f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            Text[] activeAbilities = new Text[5]; //해금된 기술의 단축키 글자
            for (int i = 0; i < activeAbilities.Length; i++) //하단에 배치할 능력 표시
            {
                RectTransform chip = panel($"Ability {i + 1}", abilityList, 0, 0, 125, 35, cardColor); //단축키와 대기 시간을 담는 카드
                activeAbilities[i] = text("Key", chip, 0, 0, 115, 33, 15, "", mint, TextAnchor.MiddleCenter, true);
                chip.gameObject.SetActive(false);
            }

            RectTransform hintPanel = panel("Interaction", hud, 0, -276, 176, 35, new Color32(30, 50, 49, 245)); //상호작용 가능 표시
            Text hint = text("Hint", hintPanel, 0, 0, 164, 34, 16, "", mint, TextAnchor.MiddleCenter, true); //문이나 출구 조작 문구
            hintPanel.gameObject.SetActive(false);

            RectTransform title = overlay("Title", screen.transform, 170); //첫 플레이 시작 화면
            Button start = createTitle(title, player, civilian, guard); //플레이 시작 버튼
            RectTransform upgradeMenu = overlay("Stage Reward", screen.transform, 203); //강화 선택 화면
            createUpgradeMenu(upgradeMenu, out Text summary, out Button[] choices, out Text[] ranks);
            RectTransform result = overlay("Result", screen.transform, 207); //스테이지 완료와 사망 화면
            RectTransform resultCard = panel("Result Card", result, 0, 0, 570, 274, panelColor); //결과 안내 판
            panel("Result Accent", resultCard, 0, 135, 570, 4, mint);
            Text resultTitle = text("Result Title", resultCard, 0, 65, 500, 73, 39, "", pale, TextAnchor.MiddleCenter, true); //결과 제목
            Text resultSummary = text("Result Summary", resultCard, 0, 0, 500, 50, 19, "", dim, TextAnchor.MiddleCenter); //스테이지 결과 요약
            Button restart = button("Restart", resultCard, 0, -77, 225, 51, mint); //새 플레이를 시작할 버튼
            text("Label", restart.transform, 0, 0, 207, 45, 19, "다시 시작", ink, TextAnchor.MiddleCenter, true);

            setReference(ui, "hud", hud.gameObject);
            setReference(ui, "title", title.gameObject);
            setReference(ui, "upgrades", upgradeMenu.gameObject);
            setReference(ui, "result", result.gameObject);
            setReference(ui, "startButton", start);
            setReference(ui, "restartButton", restart);
            setReference(ui, "stageText", stageText);
            setReference(ui, "healthText", healthText);
            setReference(ui, "healthFill", healthFill);
            setReference(ui, "infectionText", infection);
            setReference(ui, "statusText", status);
            setReference(ui, "hintText", hint);
            setReferences(ui, "activeAbilities", activeAbilities);
            setReference(ui, "upgradeSummary", summary);
            setReferences(ui, "upgradeButtons", choices);
            setReferences(ui, "upgradeRanks", ranks);
            setReference(ui, "resultTitle", resultTitle);
            setReference(ui, "resultSummary", resultSummary);

            hud.gameObject.SetActive(false);
            upgradeMenu.gameObject.SetActive(false);
            result.gameObject.SetActive(false);
            return ui;
        }

        private static Button createTitle(RectTransform title, Sprite player, Sprite civilian, Sprite guard) //시작 화면과 캐릭터 구분 안내 만들기
        {
            RectTransform card = panel("Mission Brief", title, -226, 5, 594, 392, panelColor); //시작 안내 판
            panel("Accent", card, -295, 0, 4, 392, mint);
            text("First Stage", card, -2, 133, 510, 26, 17, "01  /  폐쇄 진료소", mint, TextAnchor.MiddleLeft, true);
            text("Title", card, -2, 65, 514, 99, 68, "감염 구역", pale, TextAnchor.MiddleLeft, true);
            text("Objective", card, -2, -10, 510, 57, 21, "시민을 모두 감염시키거나 출구로 탈출하세요.", pale, TextAnchor.MiddleLeft);
            text("Keys", card, -2, -66, 510, 34, 16, "WASD 이동   ·   E 문/탈출   ·   SPACE 감염 유지", dim, TextAnchor.MiddleLeft);
            Button start = button("Enter", card, -158, -131, 200, 53, mint); //첫 스테이지 진입 버튼
            text("Label", start.transform, 0, 0, 178, 46, 19, "구역 진입   →", ink, TextAnchor.MiddleCenter, true);

            RectTransform legend = panel("Actors", title, 354, -115, 296, 168, new Color32(20, 33, 39, 239)); //캐릭터 구분 표
            Sprite[] sprites = { player, civilian, guard }; //게임에 나오는 캐릭터 그림
            string[] labels = { "감염원", "시민", "경비" }; //캐릭터마다 표시할 역할
            for (int i = 0; i < sprites.Length; i++) //역할마다 스프라이트와 이름 배치
            {
                int x = -94 + i * 94; //역할 표시의 가로 위치
                Image actor = image("Portrait", legend, x, 20, 63, 63, Color.white); //캐릭터 역할 그림
                actor.sprite = sprites[i];
                actor.preserveAspect = true;
                text("Role", legend, x, -43, 85, 30, 16, labels[i], pale, TextAnchor.MiddleCenter);
            }

            return start;
        }

        private static void createUpgradeMenu(RectTransform menu, out Text summary, out Button[] choices, out Text[] ranks) //세 분류에 강화 카드 배치
        {
            RectTransform board = panel("Upgrade Board", menu, 0, 0, 1112, 575, panelColor); //세 분류를 담는 강화 화면 판
            text("Upgrade Title", board, 0, 230, 1012, 55, 36, "능력 강화", pale, TextAnchor.MiddleLeft, true);
            summary = text("Clear Route", board, 0, 175, 1012, 33, 18, "", mint, TextAnchor.MiddleLeft);
            text("Choose One", board, 0, 175, 1012, 33, 16, "한 가지 능력을 선택하세요", dim, TextAnchor.MiddleRight);
            string[] groups = { "기본 능력치", "감염 능력", "특수능력" }; //강화 화면의 분류 이름
            Color32[] colors = { blue, mint, amber }; //강화 분류의 구분 색
            string[] names = { "최대 체력", "이동 속도", "청력", "접촉 감염 속도", "액체 감염", "비말 감염", "은신", "돌진", "좀비 조종" }; //강화할 수 있는 아홉 항목
            string[] descriptions = //각 항목을 선택했을 때 적용되는 효과
            {
                "최대 체력과 현재 체력을 35 올립니다.",
                "이동 속도가 빨라집니다.",
                "더 먼 곳의 발소리 방향을 표시합니다.",
                "접촉 감염이 더 빨리 진행됩니다.",
                "Q  ·  밟은 시민을 감염시키는 영역을 남깁니다.",
                "R  ·  가까운 전방의 시민을 감염시킵니다.",
                "F  ·  잠시 경비의 탐지를 늦춥니다.",
                "SHIFT  ·  전방으로 짧게 돌진합니다.",
                "C  ·  마우스 위치로 좀비를 보내 감염시킵니다."
            };
            choices = new Button[names.Length];
            ranks = new Text[names.Length];
            for (int group = 0; group < groups.Length; group++) //각 강화 분류의 열
            {
                int x = (group - 1) * 340; //분류 열의 가로 중심 위치
                panel("Group Accent", board, x, 137, 320, 3, colors[group]);
                text("Group", board, x, 110, 314, 38, 22, groups[group], colors[group], TextAnchor.MiddleLeft, true);
                for (int row = 0; row < 3; row++) //각 분류의 세 가지 강화
                {
                    int index = group * 3 + row; //화면과 강화 종류가 공유하는 순서
                    Button choice = button(names[index], board, x, 40 - row * 111, 320, 99, cardColor); //강화 항목 선택 카드
                    text("Name", choice.transform, -24, 23, 240, 36, 20, names[index], pale, TextAnchor.MiddleLeft, true);
                    text("Effect", choice.transform, 0, -20, 286, 48, 15, descriptions[index], dim, TextAnchor.MiddleLeft);
                    ranks[index] = text("Rank", choice.transform, 115, 23, 58, 33, 15, "", colors[group], TextAnchor.MiddleRight, true);
                    choices[index] = choice;
                }
            }
        }

        private static RectTransform overlay(string name, Transform parent, byte opacity) //입력을 받는 전체 화면 메뉴 배경
        {
            RectTransform rect = stretch(name, parent); //화면 전체를 덮는 사각형
            Image background = rect.gameObject.AddComponent<Image>(); //맵을 어둡게 하는 메뉴 배경
            background.color = new Color32(6, 12, 17, opacity);
            return rect;
        }

        private static RectTransform stretch(string name, Transform parent) //부모 영역을 채우는 사각형 생성
        {
            RectTransform child = rect(name, parent, 0, 0, 0, 0); //크기를 부모에 맞출 사각형
            child.anchorMin = Vector2.zero;
            child.anchorMax = Vector2.one;
            child.offsetMin = Vector2.zero;
            child.offsetMax = Vector2.zero;
            return child;
        }

        private static RectTransform panel(string name, Transform parent, float x, float y, float width, float height, Color color) //색이 있는 UI 판 생성
        {
            return image(name, parent, x, y, width, height, color).rectTransform;
        }

        private static Image image(string name, Transform parent, float x, float y, float width, float height, Color color) //UI 그림과 바탕 생성
        {
            Image image = rect(name, parent, x, y, width, height).gameObject.AddComponent<Image>(); //표시할 UI 이미지
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Button button(string name, Transform parent, float x, float y, float width, float height, Color color) //마우스로 조작하는 화면 버튼 생성
        {
            Image background = image(name, parent, x, y, width, height, color); //버튼 배경
            background.raycastTarget = true;
            Button button = background.gameObject.AddComponent<Button>(); //버튼 입력 컴포넌트
            button.targetGraphic = background;
            Navigation navigation = button.navigation; //키보드 게임 조작과 겹치지 않는 설정
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            ColorBlock colors = button.colors; //선택 상태의 색
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.77f, 1f, 0.84f, 1f);
            colors.pressedColor = new Color(0.70f, 0.88f, 0.72f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            return button;
        }

        private static Text text(string name, Transform parent, float x, float y, float width, float height, int size, string value,
            Color color, TextAnchor alignment = TextAnchor.MiddleLeft, bool bold = false) //한국어 화면 글자 배치
        {
            Text label = rect(name, parent, x, y, width, height).gameObject.AddComponent<Text>(); //배치할 글자 컴포넌트
            label.font = font;
            label.fontSize = size;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = alignment;
            label.color = color;
            label.text = value;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform rect(string name, Transform parent, float x, float y, float width, float height) //UI 오브젝트의 크기와 중앙 기준 좌표 설정
        {
            RectTransform child = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); //배치할 UI 사각형
            child.SetParent(parent, false);
            child.anchoredPosition = new Vector2(x, y);
            child.sizeDelta = new Vector2(width, height);
            return child;
        }
    }
}
