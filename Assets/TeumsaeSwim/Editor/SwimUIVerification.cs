using System;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TeumsaeSwim.Editor
{
    public static class SwimUIVerification
    {
        private static int checks; //통과한 UI와 점수 검사 수

        public static void run() //별 경계 조건과 실제 UI 입력 및 전환 검사
        {
            checks = 0;
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            SwimGame game = UnityEngine.Object.FindFirstObjectByType<SwimGame>(); //기존 씬 게임
            Player player = UnityEngine.Object.FindFirstObjectByType<Player>(); //실제 입력 대상
            GameSessionComponent session = game.GetComponent<GameSessionComponent>(); //별 판정 대상
            for (int i = 0; i < 3; i++)
            {
                SwimDifficulty difficulty = (SwimDifficulty)i; //검사할 난이도
                session.begin(difficulty);
                float goal = session.timeGoal; //이 난이도의 목표
                session.advance(goal, false, true, true);
                require(session.stars == 3 && session.timeStar, "Exact time boundary earns three stars with cargo");
                session.begin(difficulty);
                session.advance(goal + .01f, false, true, true);
                require(session.stars == 2 && !session.timeStar, "Late cargo escape earns two stars");
                session.begin(difficulty);
                session.advance(1, false, true, false);
                require(session.stars == 2 && !session.cargoDelivered, "Fast optional escape earns two stars");
                session.begin(difficulty);
                session.advance(goal + 10, false, false);
                require(session.phase == SwimPhase.Playing && session.stars == 0, "Expired time does not end play or award premature stars");
                session.advance(0, false, true, false);
                require(session.stars == 1, "Late empty escape earns exactly one star");
                session.advance(10, true, false, true);
                require(session.stars == 1, "Final score cannot change after result");
                session.begin(difficulty);
                session.pause();
                session.advance(goal + 30, false, true, true);
                require(session.elapsed == 0 && session.stars == 0, "Pause freezes bonus clock");
                session.resume();
                session.advance(1, true, true, true);
                require(session.stars == 0 && !session.timeStar && !session.cargoDelivered, "Caught with cargo at exit earns no stars");
            }
            session.home();
            invoke(player, "Awake");
            invoke(game, "Start");
            click(game, "Button 플레이 방법");
            require(active(game, "Help"), "Help opens from home");
            click(game, "Button 포식자 도감");
            require(active(game, "Predators page") && !active(game, "Rules page"), "Help tabs switch page");
            click(game, "Button 알겠어, 헤엄칠 준비 됐어");
            click(game, "Button 기본");
            click(game, "Button 게임 시작");
            require(game.phase == SwimPhase.Playing && game.difficulty == SwimDifficulty.Normal, "Selected difficulty starts through actual button");
            TouchControlComponent stick = game.GetComponentsInChildren<TouchControlComponent>().Single(item => item.name == "Move stick"); //이동 터치
            TouchControlComponent hold = game.GetComponentsInChildren<TouchControlComponent>().Single(item => item.name == "Hold action"); //꾹 터치
            Vector2 stickPoint = RectTransformUtility.WorldToScreenPoint(null, stick.transform.position) + Vector2.right * 30; //스틱 오른쪽 누름
            var moveFinger = new PointerEventData(EventSystem.current) { pointerId = 11, position = stickPoint }; //첫 손가락
            var holdFinger = new PointerEventData(EventSystem.current) { pointerId = 22 }; //두 번째 손가락
            PlayerInputComponent input = player.GetComponent<PlayerInputComponent>(); //실제 입력 상태
            stick.OnPointerDown(moveFinger);
            hold.OnPointerDown(holdFinger);
            input.sample();
            require(input.moveInput.sqrMagnitude > .01f && input.holdInput, "Move and hold work simultaneously");
            hold.OnPointerUp(moveFinger);
            input.sample();
            require(input.holdInput, "Other finger cannot release held action");
            game.pause();
            input.sample();
            require(input.moveInput == Vector2.zero && !input.holdInput, "Pause clears both touch owners");
            require(active(game, "Pause") && !active(game, "HUD"), "Pause blocks gameplay controls");
            click(game, "Button 계속 헤엄치기");
            require(game.phase == SwimPhase.Playing, "Continue button resumes game");
            session.advance(1, false, true, true);
            game.GetComponent<GameUIComponent>().showPhase();
            require(active(game, "Result") && !active(game, "HUD"), "Result replaces gameplay inputs");
            click(game, "Button 같은 미로 다시 도전");
            require(game.phase == SwimPhase.Playing && game.stars == 0 && !player.hasCargo && game.elapsed == 0, "Retry clears score and carried item");
            game.home();
            game.begin(SwimDifficulty.Practice, false);
            Maze maze = UnityEngine.Object.FindFirstObjectByType<Maze>(); //실제 미로 범위
            Camera camera = Camera.main; //화면 가장자리 검증 카메라
            CameraFollowComponent follow = camera.GetComponent<CameraFollowComponent>(); //UI 가시 영역을 지키는 추적
            Rect field = game.GetComponent<GameUIComponent>().playfield; //가려지지 않는 화면 범위
            for (int corner = 0; corner < 4; corner++)
            {
                player.transform.position = new Vector2(corner % 2 == 0 ? maze.data.tileSize * 1.5f : (maze.data.width - 1.5f) * maze.data.tileSize,
                    corner < 2 ? maze.data.tileSize * 1.5f : (maze.data.height - 1.5f) * maze.data.tileSize);
                follow.snap();
                Vector3 point = camera.WorldToViewportPoint(player.position); //가장자리에서의 실제 화면 위치
                require(point.y > field.yMin && point.y < field.yMax, "Map edge player stays between header and controls");
            }
            Debug.Log("SUMBAKKOGI_UI_VERIFIED: " + checks + " checks");
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
        }

        private static bool active(SwimGame game, string name) //생성된 화면의 실제 활성 상태
        {
            return game.GetComponentsInChildren<Transform>(true).Single(item => item.name == name).gameObject.activeInHierarchy;
        }

        private static void click(SwimGame game, string name) //실제 화면 버튼 호출
        {
            Button button = game.GetComponentsInChildren<Button>().Single(item => item.name == name); //누를 실제 버튼
            Canvas.ForceUpdateCanvases();
            button.onClick.Invoke();
        }

        private static void invoke(object target, string method) //에디터 검사에서 Unity 초기화 호출
        {
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }

        private static void require(bool value, string message) //검증 실패를 배치 실행 오류로 전달
        {
            if (!value) throw new InvalidOperationException(message);
            checks++;
        }
    }
}
