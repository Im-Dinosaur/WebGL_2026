using UnityEngine;
using UnityEngine.UI;

namespace TeumsaeSwim
{
    public sealed class GameMenuComponent : MonoBehaviour
    {
        private SwimGame game; //게임 단계와 기록 진입점
        private Player player; //진주 소지 결과 조회
        private GameUIStyleComponent style; //공통 화면 디자인
        private GameObject homePanel; //시작 화면
        private GameObject pausePanel; //멈춤 화면
        private GameObject resultPanel; //결과 화면
        private GameObject helpPanel; //도움말 화면
        private GameObject rulesPage; //이동과 목표 안내
        private GameObject predatorsPage; //종류별 포식자 안내
        private Text recordLabel; //난이도별 기록
        private Text goalLabel; //난이도별 시간 목표
        private Text soundLabel; //음소거 안내
        private Text resultTitle; //성공 또는 실패 제목
        private Text resultTime; //결과 시간
        private Text resultHint; //결과 안내
        private Text resultCargo; //진주 운반 결과
        private Text resultTimeGoal; //시간 별 결과 사유
        private Text resultEscape; //탈출 별 결과 사유
        private Text resultCount; //획득한 별 개수
        private GameUIIconComponent homeSound; //홈 소리 버튼 그림
        private readonly Button[] difficulties = new Button[3]; //선택 가능한 난이도
        private readonly GameUIIconComponent[] stars = new GameUIIconComponent[3]; //결과 별 그림
        private readonly GameUIIconComponent[] checks = new GameUIIconComponent[3]; //난이도 선택 표식
        private SwimDifficulty selected = SwimDifficulty.Practice; //처음 실행은 연습
        public string cargoResult => resultCargo.text; //실제 운반 결과 문구

        public void initialize(SwimGame owner, Player target, GameUIStyleComponent theme) //메뉴별 버튼과 게임 진입점 연결
        {
            game = owner;
            player = target;
            style = theme;
            createHome();
            createPause();
            createResult();
            createHelp();
            selectDifficulty(selected);
        }

        private void createHome() //숨은 물고기 배경과 난이도 및 큰 시작 버튼으로 구성한 홈
        {
            RectTransform outer = style.screen("Home", true); //홈 전체 배경
            homePanel = outer.gameObject;
            RectTransform root = style.sheet(outer, "Home content"); //세로 메뉴 배치
            Button sound = style.button(root, "", new Vector2(195, 369), Vector2.one * 44, () => game.toggleMute()); //홈 소리 버튼
            homeSound = style.icon(sound.GetComponent<RectTransform>(), SwimUIIcon.Sound, new Vector2(0, 4), 23, GameUIStyleComponent.cream);
            style.logo(root, new Vector2(0, 278), new Vector2(435, 145));
            style.text(root, "난이도", new Vector2(0, 0), new Vector2(180, 34), 23, SwimArt.mint, TextAnchor.MiddleCenter, true);
            style.picture(root, "Difficulty line left", "box", new Vector2(-88, 0), new Vector2(30, 1), SwimArt.mint);
            style.picture(root, "Difficulty line right", "box", new Vector2(88, 0), new Vector2(30, 1), SwimArt.mint);
            string[] goals = { "탈출", "시간", "진주" }; //별 목표 이름
            style.card(root, "Star goals", new Vector2(0, -319), new Vector2(302, 78), new Color32(9, 42, 58, 220), GameUIStyleComponent.border);
            for (int i = 0; i < 3; i++)
            {
                style.icon(root, SwimUIIcon.Star, new Vector2((i - 1) * 91, -305), 28, SwimArt.mint);
                style.text(root, goals[i], new Vector2((i - 1) * 91, -335), new Vector2(78, 25), 16, GameUIStyleComponent.cream);
            }
            for (int i = 0; i < 3; i++)
            {
                SwimDifficulty value = (SwimDifficulty)i; //선택할 난이도
                Button choice = style.button(root, GameUIComponent.difficultyName(value), new Vector2((i - 1) * 139, -56), new Vector2(130, 57), () => selectDifficulty(value), false, 23); //난이도 선택
                difficulties[i] = choice;
                checks[i] = style.icon(choice.GetComponent<RectTransform>(), SwimUIIcon.Check, new Vector2(-40, 4), 15, SwimArt.ink);
            }
            style.text(root, "미끼를 모으고, 포식자를 피해 출구로!", new Vector2(0, -111), new Vector2(438, 28), 16, GameUIStyleComponent.cream, TextAnchor.MiddleCenter, true);
            goalLabel = style.text(root, "", new Vector2(0, -144), new Vector2(434, 27), 13, GameUIStyleComponent.cream);
            recordLabel = style.text(root, "", new Vector2(0, -174), new Vector2(422, 26), 13, GameUIStyleComponent.muted);
            Button start = style.startButton(root, new Vector2(0, -235), () => game.begin(selected)); //시작 동작
            style.icon(start.GetComponent<RectTransform>(), SwimUIIcon.Chevron, new Vector2(162, 4), 26, SwimArt.ink);
            style.button(root, "플레이 방법", new Vector2(0, -387), new Vector2(270, 44), openHelp, false, 19);
        }

        private RectTransform modal(string name, string title) //공통 메뉴 배경과 제목
        {
            RectTransform outer = style.screen(name, true); //뒤 입력을 막는 메뉴
            RectTransform tint = style.picture(outer, "Shade", "box", Vector2.zero, Vector2.zero, new Color(0, .05f, .09f, .5f)).rectTransform; //문구 대비용 막
            style.stretch(tint);
            RectTransform root = style.sheet(outer, name + " content"); //메뉴 배치
            style.logo(root, new Vector2(0, 343), new Vector2(215, 72));
            if (!string.IsNullOrEmpty(title)) style.text(root, title, new Vector2(0, 260), new Vector2(420, 62), 34, GameUIStyleComponent.cream, TextAnchor.MiddleCenter, true);
            return root;
        }

        private void createPause() //같은 분위기의 정지 메뉴
        {
            RectTransform root = modal("Pause", "잠깐, 숨 고르기"); //멈춤 배치
            pausePanel = root.parent.gameObject;
            style.illustration(root, "Resting fish", style.fish, new Vector2(0, 133), new Vector2(185, 130));
            style.text(root, "시간도 포식자도 잠시 쉬는 중이에요", new Vector2(0, 42), new Vector2(420, 32), 16, GameUIStyleComponent.muted);
            style.button(root, "계속 헤엄치기", new Vector2(0, -38), new Vector2(366, 65), () => game.resume(), true, 23);
            Button sound = style.button(root, "", new Vector2(0, -120), new Vector2(366, 51), () => game.toggleMute()); //소리 상태 전환
            soundLabel = sound.GetComponentInChildren<Text>();
            style.button(root, "시작 화면으로", new Vector2(0, -192), new Vector2(366, 51), () => game.home(), false, 18);
            style.text(root, "다른 창으로 이동하면 자동으로 멈춰요", new Vector2(0, -280), new Vector2(420, 31), 13, GameUIStyleComponent.muted);
        }

        private void createResult() //별 획득 사유와 재도전 동작
        {
            RectTransform root = modal("Result", ""); //결과 배치
            resultPanel = root.parent.gameObject;
            resultTitle = style.text(root, "", new Vector2(0, 260), new Vector2(430, 62), 34, GameUIStyleComponent.cream, TextAnchor.MiddleCenter, true);
            for (int i = 0; i < 3; i++) stars[i] = style.icon(root, SwimUIIcon.Star, new Vector2((i - 1) * 89, 174 + (i == 1 ? 9 : 0)), i == 1 ? 76 : 64, SwimArt.yellow);
            resultCount = style.text(root, "", new Vector2(0, 112), new Vector2(420, 31), 16, SwimArt.mint);
            resultTime = style.text(root, "", new Vector2(0, 60), new Vector2(420, 62), 39, GameUIStyleComponent.cream, TextAnchor.MiddleCenter, true);
            resultHint = style.text(root, "", new Vector2(0, 13), new Vector2(420, 30), 13, GameUIStyleComponent.muted);
            RectTransform summary = style.card(root, "Star results", new Vector2(0, -84), new Vector2(390, 133), GameUIStyleComponent.panel, GameUIStyleComponent.border).rectTransform; //개별 달성 조건
            resultEscape = style.text(summary, "", new Vector2(0, 39), new Vector2(350, 32), 16, GameUIStyleComponent.cream, TextAnchor.MiddleLeft);
            resultTimeGoal = style.text(summary, "", Vector2.zero, new Vector2(350, 32), 16, GameUIStyleComponent.cream, TextAnchor.MiddleLeft);
            resultCargo = style.text(summary, "", new Vector2(0, -39), new Vector2(350, 32), 16, GameUIStyleComponent.cream, TextAnchor.MiddleLeft);
            style.button(root, "같은 미로 다시 도전", new Vector2(0, -209), new Vector2(378, 61), () => game.begin(game.difficulty, false), true, 22);
            style.button(root, "새 미로 헤엄치기", new Vector2(0, -286), new Vector2(378, 50), () => game.begin(game.difficulty), false, 18);
            style.button(root, "시작 화면으로", new Vector2(0, -353), new Vector2(280, 44), () => game.home(), false, 16);
        }

        private void createHelp() //짧은 두 페이지로 나눈 규칙과 포식자 안내
        {
            RectTransform root = modal("Help", "헤엄 안내"); //도움말 배치
            helpPanel = root.parent.gameObject;
            style.button(root, "조작과 목표", new Vector2(-103, 199), new Vector2(194, 42), () => showHelpPage(false), false, 17);
            style.button(root, "포식자 도감", new Vector2(103, 199), new Vector2(194, 42), () => showHelpPage(true), false, 17);
            RectTransform rules = style.rect(root, "Rules page", Vector2.zero, Vector2.zero); //규칙 페이지
            RectTransform predators = style.rect(root, "Predators page", Vector2.zero, Vector2.zero); //도감 페이지
            rulesPage = rules.gameObject;
            predatorsPage = predators.gameObject;
            string[] titles = { "01   출구까지 헤엄쳐요", "02   이동하면서 꾹 눌러요", "03   숨고, 미끼로 유인해요", "04   별 세 개에 도전해요" }; //규칙 제목
            string[] bodies = {
                "스틱으로 움직여 빛나는 출구를 찾아요.\n시작·도착의 민트색 구역에서는 안전해요.",
                "좁은 틈에서는 꾹 누른 채 이동해요.\n그물도 움직이며 꾹 2초! 진행도는 유지돼요.",
                "풀숲에 완전히 숨으면 닿아도 잡히지 않아요.\n미끼는 드래그해 발사하고 주워 보충해요.",
                "탈출 + 시간 내 도착 + 진주 운반 = 별 3개!\n시간이 지나도 계속할 수 있어요. 잡히면 별 0개."
            }; //조작과 점수 규칙
            for (int i = 0; i < 4; i++)
            {
                RectTransform card = style.card(rules, "Rule " + (i + 1), new Vector2(0, 119 - i * 105), new Vector2(420, 94), GameUIStyleComponent.panel, GameUIStyleComponent.border).rectTransform; //규칙 카드
                style.text(card, titles[i], new Vector2(0, 24), new Vector2(380, 31), 18, SwimArt.yellow, TextAnchor.MiddleLeft, true);
                style.text(card, bodies[i], new Vector2(0, -17), new Vector2(380, 52), 14, GameUIStyleComponent.cream, TextAnchor.MiddleLeft);
            }
            for (int i = 0; i < game.predatorTypes.Count; i++)
            {
                PredatorTypeProfile profile = game.predatorTypes[i]; //실제 종류 설정
                RectTransform card = style.card(predators, "Predator " + profile.title, new Vector2(0, 119 - i * 105), new Vector2(420, 94), GameUIStyleComponent.panel, GameUIStyleComponent.border).rectTransform; //도감 카드
                style.illustration(card, profile.title, profile.sprite, new Vector2(-153, 0), new Vector2(94, 77));
                style.text(card, profile.title, new Vector2(46, 21), new Vector2(270, 30), 19, profile.color, TextAnchor.MiddleLeft, true);
                style.text(card, profile.hint, new Vector2(46, -16), new Vector2(270, 49), 14, GameUIStyleComponent.cream, TextAnchor.MiddleLeft);
            }
            style.text(root, "PC  WASD 이동 · SPACE 꾹 · 마우스 드래그 조준\nE 빠른 발사 · ESC 멈춤 · 미끼 최대 4개 / 보급 30초", new Vector2(0, -275), new Vector2(442, 44), 12, GameUIStyleComponent.muted);
            style.text(root, "BGM: Underwater Ambient Pad · isaiah658 · CC0", new Vector2(0, -309), new Vector2(442, 20), 10, GameUIStyleComponent.muted);
            style.button(root, "알겠어, 헤엄칠 준비 됐어", new Vector2(0, -350), new Vector2(390, 57), () => helpPanel.SetActive(false), true, 20);
            showHelpPage(false);
            helpPanel.SetActive(false);
        }

        public void openHelp() //규칙 페이지 열기
        {
            showHelpPage(false);
            helpPanel.SetActive(true);
        }

        public void showHelpPage(bool predators) //도움말 탭 전환
        {
            rulesPage.SetActive(!predators);
            predatorsPage.SetActive(predators);
        }

        public void showPhase() //진행 단계에 맞는 메뉴와 결과 갱신
        {
            homePanel.SetActive(game.phase == SwimPhase.Home);
            pausePanel.SetActive(game.phase == SwimPhase.Paused);
            bool ended = game.phase == SwimPhase.Won || game.phase == SwimPhase.Lost; //결과 확정 상태
            resultPanel.SetActive(ended);
            helpPanel.SetActive(false);
            if (game.phase == SwimPhase.Home) selectDifficulty(selected);
            if (ended)
            {
                bool won = game.phase == SwimPhase.Won; //탈출 성공
                resultTitle.text = won ? "바깥바다에 도착!" : "앗, 들켜 버렸어";
                resultTitle.color = won ? SwimArt.mint : SwimArt.coral;
                resultCount.text = won ? "별 " + game.stars + "개 획득!" : "다시 숨고, 다시 헤엄쳐요";
                bool[] earned = { won, game.timeStar, game.cargoDelivered }; //별별 확정 결과
                for (int i = 0; i < 3; i++) stars[i].configure(SwimUIIcon.Star, earned[i] ? SwimArt.yellow : GameUIStyleComponent.border, earned[i]);
                resultTime.text = GameUIComponent.formatTime(game.elapsed);
                resultHint.text = won ? game.newRecord ? "새로운 최고 기록!" : game.bestTime(game.difficulty) > 0 ? GameUIComponent.difficultyName(game.difficulty) + " · 최고 " + GameUIComponent.formatTime(game.bestTime(game.difficulty)) : "무사히 탈출했어요!" : "풀숲에 완전히 숨거나, 미끼로 유인해 봐요";
                resultEscape.text = won ? "탈출 성공 · 별 +1" : "탈출 실패 · 별은 도착해야 얻어요";
                resultEscape.color = won ? SwimArt.mint : GameUIStyleComponent.muted;
                resultTimeGoal.text = (game.timeStar ? "시간 목표 달성 · " : "시간 목표 미달성 · ") + GameUIComponent.clockTime(game.timeGoal) + " 이내";
                resultTimeGoal.color = game.timeStar ? SwimArt.mint : GameUIStyleComponent.muted;
                resultCargo.text = game.cargoDelivered ? "진주 운반 성공 · 1/1" : !won && player.hasCargo ? "진주 운반 실패" : "진주 미획득 · 0/1";
                resultCargo.color = game.cargoDelivered ? SwimArt.mint : GameUIStyleComponent.muted;
            }
            refreshSound();
        }

        public void refreshSound() //여러 화면의 소리 상태 동기화
        {
            homeSound.configure(game.muted ? SwimUIIcon.Muted : SwimUIIcon.Sound, GameUIStyleComponent.cream);
            soundLabel.text = game.muted ? "소리 켜기" : "소리 끄기";
        }

        private void selectDifficulty(SwimDifficulty value) //선택 상태와 기록 및 시간 목표 표시
        {
            selected = value;
            for (int i = 0; i < 3; i++)
            {
                bool active = i == (int)value; //선택한 버튼
                difficulties[i].targetGraphic.color = active ? SwimArt.mint : GameUIStyleComponent.panel;
                Text caption = difficulties[i].GetComponentInChildren<Text>(); //선택 이름
                caption.color = active ? SwimArt.ink : GameUIStyleComponent.cream;
                caption.rectTransform.anchoredPosition = new Vector2(active ? 8 : 0, 0);
                checks[i].gameObject.SetActive(active);
            }
            float best = game.bestTime(value); //난이도별 최고 기록
            recordLabel.text = best > 0 ? "최고 기록 " + GameUIComponent.formatTime(best) : "첫 탈출 기록을 만들어 보세요";
            int count = game.predatorCountFor(value); //Inspector 설정과 동일한 포식자 수
            goalLabel.text = "포식자 " + count + "마리  ·  시간 별 " + GameUIComponent.clockTime(game.timeGoalFor(value)) + " 이내";
        }
    }
}
