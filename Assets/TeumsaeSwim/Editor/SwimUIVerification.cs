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

        public static void verifyFeedbackAndBuild() //버튼 효과와 조준 회귀 검사 후 기존 공개 경로로 빌드
        {
            run();
            BaitAimVerification.run();
            SwimProjectSetup.build();
        }

        public static void run() //별 경계 조건과 실제 UI 입력 및 전환 검사
        {
            checks = 0;
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            SwimGame game = UnityEngine.Object.FindFirstObjectByType<SwimGame>(); //기존 씬 게임
            Player player = UnityEngine.Object.FindFirstObjectByType<Player>(); //실제 입력 대상
            GameSessionComponent session = game.GetComponent<GameSessionComponent>(); //별 판정 대상
            require(session.timeGoalFor(SwimDifficulty.Challenge) == 180f && session.timeGoalFor(SwimDifficulty.Normal) == 240f && session.timeGoalFor(SwimDifficulty.Practice) == 180f, "Challenge is three minutes while other goals remain unchanged");
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
            verifyFeedback(game);
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
            UIButtonFeedbackComponent holdEffect = hold.GetComponent<UIButtonFeedbackComponent>(); //동시 누름의 표시 대상
            holdEffect.OnPointerDown(holdFinger);
            animate(holdEffect, .07f);
            input.sample();
            require(input.moveInput.sqrMagnitude > .01f && input.holdInput, "Move and hold work simultaneously");
            require(holdEffect.pressed && hold.transform.localScale == Vector3.one, "Hold feedback preserves movement input area");
            holdEffect.OnPointerExit(holdFinger);
            require(holdEffect.pressed, "Hold feedback remains pressed outside its area");
            hold.OnPointerUp(moveFinger);
            holdEffect.OnPointerUp(moveFinger);
            input.sample();
            require(input.holdInput, "Other finger cannot release held action");
            require(holdEffect.pressed, "Other finger cannot release hold feedback");
            game.pause();
            animate(holdEffect, .01f);
            require(!holdEffect.pressed, "Hidden gameplay panel clears feedback");
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

        private static void verifyFeedback(SwimGame game) //실제 버튼의 점유와 취소 및 표시 전용 변형 검사
        {
            require(game.GetComponentsInChildren<Button>(true).All(item => item.GetComponent<UIButtonFeedbackComponent>() != null), "Every menu and bait button has feedback");
            Button button = game.GetComponentsInChildren<Button>().Single(item => item.name == "Button 도전"); //현재 홈의 도전 버튼
            UIButtonFeedbackComponent effect = button.GetComponent<UIButtonFeedbackComponent>(); //실제 연결된 효과
            RectTransform area = button.GetComponent<RectTransform>(); //변하면 안 되는 입력 기준
            RectTransform face = button.targetGraphic.rectTransform; //눌리는 표시 면
            Vector2 size = area.sizeDelta; //원래 터치 크기
            Vector2 position = area.anchoredPosition; //원래 터치 위치
            var finger = new PointerEventData(EventSystem.current) { pointerId = 71 }; //점유할 손가락
            var other = new PointerEventData(EventSystem.current) { pointerId = 72 }; //개입하면 안 되는 손가락
            ExecuteEvents.Execute(button.gameObject, finger, ExecuteEvents.pointerDownHandler);
            animate(effect, .07f);
            require(effect.pressed && face.localScale.x < .97f, "Pointer down visibly depresses the actual button face");
            require(area.sizeDelta == size && area.anchoredPosition == position && area.localScale == Vector3.one, "Button effect keeps touch position and area unchanged");
            effect.OnPointerDown(other);
            effect.OnPointerUp(other);
            require(effect.pressed, "Second finger cannot steal or release feedback");
            effect.OnPointerExit(finger);
            effect.OnPointerUp(finger);
            animate(effect, .3f);
            Image ripple = area.Find("Touch ripple").GetComponent<Image>(); //취소와 발산 표시
            require(!effect.pressed && face.localScale == Vector3.one && ripple.color.a == 0, "Dragging out cancels and restores menu feedback");
            effect.OnPointerDown(finger);
            animate(effect, .07f);
            effect.OnPointerUp(finger);
            animate(effect, .05f);
            require(!effect.pressed && ripple.color.a > 0 && !ripple.raycastTarget, "Release emits a nonblocking short ripple");
            animate(effect, .3f);
            require(face.localScale == Vector3.one && ripple.color.a == 0, "Release settles without persistent scaling or animation");
            button.interactable = false;
            effect.OnPointerDown(finger);
            animate(effect, .07f);
            require(!effect.pressed && face.localScale == Vector3.one, "Disabled buttons do not react");
            button.interactable = true;
            effect.OnPointerDown(finger);
            animate(effect, .07f);
            invoke(effect, "OnDisable");
            require(!effect.pressed && face.localScale == Vector3.one, "Panel disable restores the visual state immediately");
        }

        private static void animate(UIButtonFeedbackComponent effect, float deltaTime) //에디터에서도 동일한 표시 계산 한 단계 실행
        {
            typeof(UIButtonFeedbackComponent).GetMethod("animate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(effect, new object[] { deltaTime });
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
