using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TeumsaeSwim.Editor
{
    public static class SwimVerification
    {
        private static int assertions; //통과한 검사 수

        public static void run() //생성과 지형 및 행동 규칙의 회귀 검사
        {
            assertions = 0;
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            Maze maze = UnityEngine.Object.FindFirstObjectByType<Maze>(); //검사할 실제 씬의 미로
            MazeGenerationComponent generator = maze.GetComponent<MazeGenerationComponent>(); //생성 검사 대상
            foreach (SwimDifficulty difficulty in Enum.GetValues(typeof(SwimDifficulty)))
            for (int seed = 0; seed < 200; seed++)
            {
                MazeData data = generator.generate(seed, difficulty); //다양한 시드의 생성 결과
                require(generator.validate(data), "Reachable exit " + seed);
                require(data.homes.Count == (difficulty == SwimDifficulty.Practice ? 1 : 2), "Predator count");
                foreach (int home in data.homes) require(Vector2.Distance(data.center(home), data.center(data.start)) > 11f, "Safe start");
                require(data.route.Count < 130, "Bounded route length");
            }
            maze.initialize(4817, SwimDifficulty.Normal);
            int gap = Array.FindIndex(maze.data.cells, cell => cell.kind == TerrainKind.Gap); //틈새 검사 위치
            int net = Array.FindIndex(maze.data.cells, cell => cell.kind == TerrainKind.Net); //그물 검사 위치
            Vector2 gapPosition = maze.data.center(gap); //틈새 중심
            require(!maze.canOccupy(gapPosition, .36f), "Full body cannot enter gap");
            require(maze.canOccupy(gapPosition, .22f, false, true), "Squeezed body passes gap");
            require(!maze.canOccupy(gapPosition, .22f, true, true), "Predator cannot enter gap");
            require(maze.hasSight(gapPosition + Vector2.down, gapPosition + Vector2.up), "Gap is not automatic invisibility");
            Vector2 netPosition = maze.data.center(net); //그물 중심
            require(!maze.canOccupy(netPosition, .22f, false, true), "Closed net blocks movement");
            require(maze.hasSight(netPosition + Vector2.down * .8f, netPosition + Vector2.up * .8f), "Net permits vision");
            require(!maze.workNet(net, 1.1f), "Partial net work");
            require(!maze.workNet(net, .4f), "Net work accumulates without opening early");
            require(maze.workNet(net, .51f), "Net opens after two seconds");
            require(maze.canOccupy(netPosition, .36f), "Open net permits movement");
            int wall = Array.FindIndex(maze.data.cells, cell => cell.kind == TerrainKind.Wall); //벽 검사 위치
            require(!maze.hasSight(maze.data.center(wall), maze.data.center(wall) + Vector2.up), "Rock blocks vision");
            Player player = UnityEngine.Object.FindFirstObjectByType<Player>(); //실제 플레이어 구성 요소
            player.initialize(maze);
            PlayerMovementComponent movement = player.GetComponent<PlayerMovementComponent>(); //이동 규칙 검사 대상
            player.transform.position = gapPosition;
            movement.tick(Vector2.up, false, false, .2f);
            require(Vector2.Distance(player.position, gapPosition) < .001f && player.squeezed, "Releasing hold inside gap freezes squeezed body");
            movement.tick(Vector2.up, true, false, .2f);
            require(player.position.y > gapPosition.y, "Holding resumes gap movement");
            PlayerInteractionComponent interaction = player.GetComponent<PlayerInteractionComponent>(); //미끼 규칙 검사 대상
            interaction.initialize(maze);
            interaction.tick(maze.data.center(wall), Vector2.zero, Vector2.right, false, true, .01f);
            require(interaction.baitCount == 2 && maze.baits.Count == 0, "Invalid bait placement does not consume stock");
            interaction.tick(maze.data.center(maze.data.start), Vector2.zero, Vector2.up, false, true, .01f);
            require(interaction.baitCount == 1 && maze.baits.Count == 1, "Valid bait consumes exactly one");
            maze.tick(8.1f);
            require(maze.baits.Count == 0, "Expired bait removed");
            int grass = Array.FindIndex(maze.data.cells, cell => cell.kind == TerrainKind.Grass); //은신 검사 위치
            require(maze.isHidden(maze.data.center(grass), .36f), "Full body in grass hides");
            require(!maze.isHidden(maze.data.center(grass) + Vector2.right * .8f, .36f), "Partial grass overlap does not hide");

            var aiObject = new GameObject("AI verification"); //행동 상태 검사 대상
            PredatorAIComponent ai = aiObject.AddComponent<PredatorAIComponent>(); //독립 행동 판단 구성 요소
            ai.initialize();
            ai.advance(true, Vector2.zero, Vector2.up, Vector2.down, .99f);
            require(ai.state == PredatorState.Notice, "Notice before one second");
            ai.advance(true, Vector2.zero, Vector2.up, Vector2.down, .02f);
            require(ai.state == PredatorState.Chase, "Chase after continuous one second");
            ai.advance(false, Vector2.right, Vector2.up, Vector2.down, .02f);
            require(ai.state == PredatorState.Return && ai.noticeProgress == 0, "Lost sight immediately returns without a search wait");
            ai.advance(true, Vector2.right, Vector2.up, Vector2.down, .2f);
            require(ai.state == PredatorState.Notice && ai.noticeProgress < .21f, "Reacquisition starts a fresh notice timer");
            ai.advance(false, Vector2.right, Vector2.up, Vector2.down, .02f);
            require(ai.state == PredatorState.Return && ai.target == Vector2.zero, "Return keeps pre-detection patrol position");
            UnityEngine.Object.DestroyImmediate(aiObject);

            GameSessionComponent session = UnityEngine.Object.FindFirstObjectByType<GameSessionComponent>(); //결과와 시간 규칙 검사 대상
            session.begin(SwimDifficulty.Normal);
            session.advance(.1f, true, true);
            require(session.phase == SwimPhase.Lost, "Contact wins over exit in the same frame");
            session.begin(SwimDifficulty.Normal);
            session.advance(.5f, false, false);
            session.pause();
            session.advance(30f, true, true);
            require(session.phase == SwimPhase.Paused && Mathf.Approximately(session.elapsed, .5f), "Pause freezes time and result");
            session.resume();
            session.advance(100f, false, false);
            require(session.phase == SwimPhase.Playing, "No time-limit failure");
            session.advance(.1f, false, true);
            require(session.phase == SwimPhase.Won, "Exit completes the game");
            //길찾기 결과뿐 아니라 실제 몸체와 이동 규칙으로도 출구에 도달하는지 확인한다.
            foreach (SwimDifficulty difficulty in Enum.GetValues(typeof(SwimDifficulty)))
            {
                maze.initialize(6129, difficulty);
                player.initialize(maze);
                int waypoint = 1; //다음에 통과할 경로 칸
                int frames = 0; //경로 검증의 제한 프레임
                while (waypoint < maze.data.route.Count && frames++ < 18000)
                {
                    Vector2 delta = maze.data.center(maze.data.route[waypoint]) - player.position; //다음 칸까지 이동 방향
                    if (delta.magnitude < .12f) { waypoint++; continue; }
                    int nearbyNet = maze.nearbyNet(player.position); //현재 작업할 그물
                    bool work = nearbyNet >= 0; //그물 작업 여부
                    bool squeeze = maze.overlapsGap(player.position, .95f); //틈 진입과 이탈 중 누름 유지
                    Vector2 input = work ? Vector2.zero : delta.normalized; //검증용 실제 이동 입력
                    interaction.tick(player.position, input, delta.normalized, work || squeeze, false, 1f / 60f);
                    movement.tick(input, work || squeeze, interaction.working, 1f / 60f);
                }
                require(waypoint == maze.data.route.Count, "Physical route traversal " + difficulty + " at waypoint " + waypoint);
                require(interaction.baitCount == 2, "Escape route never requires bait " + difficulty);
            }
            Directory.CreateDirectory("Logs/TeumsaeSwim");
            File.WriteAllText("Logs/TeumsaeSwim/verification.txt", "PASS: " + assertions + " assertions; 600 generated mazes; collision, sight, net, bait, hiding, AI, pause, outcome and physical route traversal rules.\n");
            Debug.Log("TEUMSAE_VERIFICATION_OK: " + assertions + " assertions");
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
        }

        private static void require(bool condition, string description) //불일치가 있으면 배치 검증 실패
        {
            if (!condition) throw new InvalidOperationException("TEUMSAE_TEST_FAILED: " + description);
            assertions++;
        }
    }
}
