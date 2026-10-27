using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

namespace TeumsaeSwim.Editor
{
    public static class BaitAimVerification
    {
        private static int checks; //통과한 조준과 실제 비행 검사 수

        public static void connectAndVerify() //현재 타일 씬에 조준을 연결하고 영향 범위를 검증
        {
            SwimUISetup.connectAim();
            SwimVerification.run();
            SwimUIVerification.run();
            run();
            SwimUIPreview.captureAim();
        }

        public static void verifyAndBuild() //비행 보정 후 기존 규칙과 실제 화면을 확인하고 WebGL 빌드
        {
            SwimVerification.run();
            SwimUIPreview.captureAim();
            SwimProjectSetup.build();
        }

        public static void run() //독립 조준과 취소 및 실제 반사 도착점 검사
        {
            checks = 0;
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            try
            {
                Player player = UnityEngine.Object.FindFirstObjectByType<Player>(); //실제 플레이어
                SwimGame game = UnityEngine.Object.FindFirstObjectByType<SwimGame>(); //실제 게임
                Maze maze = UnityEngine.Object.FindFirstObjectByType<Maze>(); //타일이 연결된 실제 미로
                typeof(Player).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
                typeof(SwimGame).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
                game.begin(SwimDifficulty.Normal, false);
                require(maze.GetComponentsInChildren<Tilemap>().Length == 2, "Water and rock tile layers remain connected");
                verifyProjection(maze, player);
                game.begin(SwimDifficulty.Normal, false);
                verifyControls(game, maze, player);
                Debug.Log("SUMBAKKOGI_BAIT_AIM_VERIFIED: " + checks + " checks");
            }
            finally { EditorSceneManager.OpenScene(SwimProjectSetup.scenePath); }
        }

        private static void verifyProjection(Maze maze, Player player) //여러 거리와 방향 및 프레임 간격에서 실제 도착 위치 비교
        {
            var path = new List<Vector2>(); //예상 경로
            Vector2 origin = maze.data.center(maze.data.start); //안전한 발사 시작점
            int inventory = player.baitCount; //예측 전 보유량
            int bounces = 0; //실제로 반사를 검사한 횟수
            foreach (float step in new[] { 1f / 120f, 1f / 30f, .05f })
            foreach (float strength in new[] { .15f, .55f, 1f })
            for (int direction = 0; direction < 8; direction++)
            {
                Vector2 aim = new Vector2(Mathf.Cos(direction * Mathf.PI / 4f), Mathf.Sin(direction * Mathf.PI / 4f)); //여덟 방향의 조준
                require(maze.predictBait(origin, aim, strength, path), "Valid aim has projection");
                require(maze.baits.Count == 0 && player.baitCount == inventory, "Projection creates no bait and consumes no stock");
                Vector2 expected = path[path.Count - 1]; //예상 정지 위치
                require(maze.placeBait(origin, aim, strength), "Selected shot launches");
                BaitData bait = maze.baits[0]; //실제 발사체
                int frames = 0; //검증의 프레임 상한
                while (!bait.settled && frames++ < 1000) maze.tick(step);
                require(bait.settled && Vector2.Distance(bait.position, expected) < .015f, "Projection matches actual endpoint: step=" + step + " strength=" + strength +
                    " direction=" + direction + " expected=" + expected + " actual=" + bait.position + " bounces=" + bait.bounceCount);
                require(bait.bounceCount <= 1 && maze.canOccupyProjectile(bait.position, .18f), "One-bounce shot stops outside obstacles");
                if (bait.bounceCount > 0) bounces++;
                maze.consumeBait(bait);
            }
            require(bounces > 0, "Projection tests include actual wall reflections");
            require(!maze.predictBait(origin, Vector2.zero, 1, path) && path.Count == 0, "Centered aim produces no path");
            int wall = Array.FindIndex(maze.data.cells, cell => cell.kind == TerrainKind.Wall); //무효 발사 위치
            require(!maze.predictBait(maze.data.center(wall), Vector2.up, 1, path), "Blocked origin has no misleading projection");

            for (int y = 4; y <= 14; y++)
            for (int x = 4; x <= 14; x++) maze.data.cells[y * maze.data.width + x].kind = TerrainKind.Water;
            origin = maze.data.center(9 * maze.data.width + 9);
            maze.predictBait(origin, Vector2.right, 0, path);
            float shortDistance = Vector2.Distance(origin, path[path.Count - 1]); //짧은 발사의 실제 예측 거리
            maze.predictBait(origin, Vector2.right, 1, path);
            float fullDistance = Vector2.Distance(origin, path[path.Count - 1]); //최대 발사의 실제 예측 거리
            require(Mathf.Abs(shortDistance - 1.1f) < .01f && Mathf.Abs(fullDistance - 6.6f) < .01f, "Drag strength controls configured short and full distances");
        }

        private static void verifyControls(SwimGame game, Maze maze, Player player) //실제 터치 컴포넌트를 통해 세 손가락 점유와 취소 검사
        {
            Canvas.ForceUpdateCanvases();
            TouchBaitAimComponent control = game.GetComponentsInChildren<TouchBaitAimComponent>().Single(); //실제 미끼 버튼의 조준 입력
            TouchControlComponent stick = game.GetComponentsInChildren<TouchControlComponent>().Single(item => item.name == "Move stick"); //실제 이동 스틱
            TouchControlComponent hold = game.GetComponentsInChildren<TouchControlComponent>().Single(item => item.name == "Hold action"); //실제 꾹 버튼
            PlayerInputComponent input = player.GetComponent<PlayerInputComponent>(); //저장된 실제 입력
            Vector2 center = RectTransformUtility.WorldToScreenPoint(null, control.transform.position); //미끼 버튼의 화면 중심
            var aimFinger = new PointerEventData(EventSystem.current) { pointerId = 33, position = center }; //조준 손가락
            var moveFinger = new PointerEventData(EventSystem.current) { pointerId = 11, position = RectTransformUtility.WorldToScreenPoint(null, stick.transform.position) + Vector2.left * 30 }; //왼쪽 이동 손가락
            var holdFinger = new PointerEventData(EventSystem.current) { pointerId = 22 }; //꾹 버튼 손가락
            control.OnPointerDown(aimFinger);
            control.OnPointerUp(aimFinger);
            player.tick(0);
            require(player.baitCount == 2 && maze.baits.Count == 0, "Tap does not accidentally fire");
            stick.OnPointerDown(moveFinger);
            hold.OnPointerDown(holdFinger);
            control.OnPointerDown(aimFinger);
            aimFinger.position = RectTransformUtility.WorldToScreenPoint(null, control.transform.TransformPoint(new Vector2(72, 54)));
            control.OnDrag(aimFinger);
            control.OnPointerUp(moveFinger);
            player.useBait();
            player.tick(0);
            require(input.moveInput.x < 0 && input.holdInput && player.baitAiming, "Movement, hold and independent aim coexist");
            require(player.baitCount == 2 && maze.baits.Count == 0, "Other finger and quick-fire cannot release active aim");
            require(Vector2.Dot(input.baitAimDirection, new Vector2(.8f, .6f)) > .999f, "Drag chooses its own direction despite leftward movement");
            Transform preview = player.transform.Find("Bait aim preview"); //실제 월드 조준선
            require(preview.gameObject.activeSelf, "Valid drag displays trajectory");
            Vector2 beforeMove = player.position; //조준 중 이동 전 위치
            player.tick(.05f);
            require(player.position.x < beforeMove.x, "Player keeps moving while aiming");
            var expected = new List<Vector2>(); //현재 위치의 실제 예상 경로
            maze.predictBait(player.position, input.baitAimDirection, input.baitAimStrength, expected);
            require(Vector2.Distance(preview.Find("Bait aim endpoint").position, expected[expected.Count - 1]) < .001f, "Visible endpoint uses physical projection");
            control.OnPointerUp(aimFinger);
            player.tick(0);
            require(player.baitCount == 1 && maze.baits.Count == 1 && Vector2.Dot(maze.baits[0].velocity.normalized, new Vector2(.8f, .6f)) > .999f, "Release fires selected direction once");
            control.OnPointerUp(aimFinger);
            player.tick(0);
            require(player.baitCount == 1 && !preview.gameObject.activeSelf, "Duplicate release does not fire and preview clears");
            stick.OnPointerUp(moveFinger);
            hold.OnPointerUp(holdFinger);
            maze.consumeBait(maze.baits[0]);

            aimFinger.position = center;
            control.OnPointerDown(aimFinger);
            aimFinger.position = RectTransformUtility.WorldToScreenPoint(null, control.transform.TransformPoint(Vector2.up * 80));
            control.OnDrag(aimFinger);
            aimFinger.position = center;
            control.OnDrag(aimFinger);
            control.OnPointerUp(aimFinger);
            player.tick(0);
            require(player.baitCount == 1 && maze.baits.Count == 0, "Returning to button center cancels without consuming bait");

            control.OnPointerDown(aimFinger);
            aimFinger.position = RectTransformUtility.WorldToScreenPoint(null, control.transform.TransformPoint(Vector2.up * 80));
            control.OnDrag(aimFinger);
            player.tick(0);
            game.pause();
            require(!player.baitAiming && !preview.gameObject.activeInHierarchy, "Pause cancels pending aim and hides trajectory");
            game.resume();
            control.OnPointerUp(aimFinger);
            player.tick(0);
            require(player.baitCount == 1 && maze.baits.Count == 0, "Stale finger release after resume cannot fire");

            var mouse = new PointerEventData(EventSystem.current) { pointerId = -1, position = center }; //PC의 왼쪽 마우스
            control.OnPointerDown(mouse);
            mouse.position = RectTransformUtility.WorldToScreenPoint(null, control.transform.TransformPoint(Vector2.down * 45));
            control.OnDrag(mouse);
            control.OnPointerUp(mouse);
            player.tick(0);
            require(player.baitCount == 0 && maze.baits.Count == 1 && maze.baits[0].velocity.y < 0, "Mouse drag aims and releases just like touch");
            require(!player.beginBaitAim(), "Empty inventory cannot start another aim");
            game.begin(SwimDifficulty.Normal, false);
            player.beginBaitAim();
            player.updateBaitAim(Vector2.up);
            game.begin(SwimDifficulty.Normal, false);
            player.endBaitAim(true);
            player.tick(0);
            require(player.baitCount == 2 && maze.baits.Count == 0 && !player.baitAiming, "Retry cancels old aim without firing into new game");
        }

        private static void require(bool condition, string message) //구체적인 조준 실패 원인 표시
        {
            if (!condition) throw new InvalidOperationException("BAIT_AIM_TEST_FAILED: " + message);
            checks++;
        }
    }
}
