using System;
using System.IO;
using System.Reflection;
using UnityEditor;
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
                require(data.homes.Count == (difficulty == SwimDifficulty.Practice ? 2 : difficulty == SwimDifficulty.Normal ? 4 : 6), "Predator count");
                require(data.height == (difficulty == SwimDifficulty.Practice ? 51 : difficulty == SwimDifficulty.Normal ? 63 : 75), "Expanded map length");
                foreach (int home in data.homes) require(Vector2.Distance(data.center(home), data.center(data.start)) > 11f, "Safe start");
                require(data.route.Count >= data.height + 20 && data.route.Count < 230, "Expanded route length " + difficulty + " seed " + seed + ": " + data.route.Count);
                int junctions = 0; //선택 가능한 갈림길 수
                for (int id = 0; id < data.cells.Length; id++)
                {
                    if (!data.passable(id, false)) continue;
                    int exits = 0; //현재 칸에서 이어지는 통로 수
                    foreach (int next in data.neighbors(id)) if (data.passable(next, false)) exits++;
                    if (exits >= 3) junctions++;
                }
                require(junctions >= 16, "Branching routes and bait pockets");
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
            for (int frame = 0; frame < 90; frame++) maze.tick(1f / 60f);
            maze.tick(8.1f);
            require(maze.baits.Count == 0, "Expired bait removed");
            int grass = Array.FindIndex(maze.data.cells, cell => cell.kind == TerrainKind.Grass); //은신 검사 위치
            require(maze.isHidden(maze.data.center(grass), .36f), "Full body in grass hides");
            require(!maze.isHidden(maze.data.center(grass) + Vector2.right * .8f, .36f), "Partial grass overlap does not hide");

            var aiObject = new GameObject("AI verification"); //행동 상태 검사 대상
            PredatorAIComponent ai = aiObject.AddComponent<PredatorAIComponent>(); //독립 행동 판단 구성 요소
            ai.initialize();
            ai.advance(true, Vector2.zero, Vector2.up, Vector2.down, .89f);
            require(ai.state == PredatorState.Notice, "Notice before 0.9 seconds");
            ai.advance(true, Vector2.zero, Vector2.up, Vector2.down, .02f);
            require(ai.state == PredatorState.Chase, "Chase after continuous 0.9 seconds");
            ai.advance(false, Vector2.right, Vector2.up, Vector2.down, .02f);
            require(ai.state == PredatorState.Return && ai.noticeProgress == 0, "Lost sight immediately returns without a search wait");
            ai.advance(true, Vector2.right, Vector2.up, Vector2.down, .2f);
            require(ai.state == PredatorState.Notice && ai.noticeProgress < .21f, "Reacquisition starts a fresh notice timer");
            ai.advance(false, Vector2.right, Vector2.up, Vector2.down, .02f);
            require(ai.state == PredatorState.Return && ai.target == Vector2.zero, "Return keeps pre-detection patrol position");
            UnityEngine.Object.DestroyImmediate(aiObject);
            verifyMovingNet(maze, player, movement, interaction);
            verifyBaitRules(maze);

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
                while (waypoint < maze.data.route.Count && frames++ < 30000)
                {
                    Vector2 delta = maze.data.center(maze.data.route[waypoint]) - player.position; //다음 칸까지 이동 방향
                    if (delta.magnitude < .12f) { waypoint++; continue; }
                    int nearbyNet = maze.nearbyNet(player.position); //현재 작업할 그물
                    bool work = nearbyNet >= 0; //그물 작업 여부
                    bool squeeze = maze.overlapsGap(player.position, .95f); //틈 진입과 이탈 중 누름 유지
                    Vector2 input = delta.normalized; //그물 앞에서도 유지하는 이동 입력
                    interaction.tick(player.position, input, delta.normalized, work || squeeze, false, 1f / 60f);
                    movement.tick(input, work || squeeze, interaction.working, 1f / 60f);
                }
                require(waypoint == maze.data.route.Count, "Physical route traversal " + difficulty + " at waypoint " + waypoint + " position " + player.position + " target " + (waypoint < maze.data.route.Count ? maze.data.center(maze.data.route[waypoint]).ToString() : "done") + " gap " + player.squeezed + " net " + maze.nearbyNet(player.position) + " working " + interaction.working);
                require(interaction.baitCount == 2, "Escape route never requires bait " + difficulty);
            }
            verifyGrassContact(maze, player);
            Directory.CreateDirectory("Logs/TeumsaeSwim");
            File.WriteAllText("Logs/TeumsaeSwim/verification.txt", "PASS: " + assertions + " assertions; 600 expanded mazes; moving net release, contextual hold, projectile bounce, lure radius/path/priority, 0.9s notice, grass contact protection, pause, outcome and physical traversal of all difficulties.\n");
            Debug.Log("TEUMSAE_VERIFICATION_OK: " + assertions + " assertions");
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
        }

        private static void verifyMovingNet(Maze maze, Player player, PlayerMovementComponent movement, PlayerInteractionComponent interaction) //이동 입력과 그물 해제의 동시 실행 검사
        {
            maze.initialize(4817, SwimDifficulty.Normal);
            player.initialize(maze);
            int net = Array.FindIndex(maze.data.cells, cell => cell.kind == TerrainKind.Net); //검사할 닫힌 그물
            Vector2 center = maze.data.center(net); //그물 중심
            player.transform.position = center + Vector2.down * .9f;
            for (int frame = 0; frame < 30; frame++)
            {
                interaction.tick(player.position, Vector2.up, Vector2.up, true, false, 1f / 60f);
                movement.tick(Vector2.up, true, interaction.working, 1f / 60f);
            }
            require(interaction.working && !player.squeezed, "Moving hold works net without squeezing");
            require(player.position.y > center.y - .9f, "Net work no longer freezes movement");
            float saved = maze.data.cells[net].netProgress; //중단 전 작업 진행도
            interaction.tick(player.position, Vector2.up, Vector2.up, false, false, .2f);
            require(Mathf.Approximately(saved, maze.data.cells[net].netProgress), "Released hold preserves net progress");
            for (int frame = 0; frame < 120; frame++)
            {
                interaction.tick(player.position, Vector2.up, Vector2.up, true, false, 1f / 60f);
                movement.tick(Vector2.up, true, interaction.working, 1f / 60f);
            }
            require(maze.data.cells[net].netOpen && player.position.y > center.y + .2f, "Held movement crosses newly opened net");
            player.initialize(maze);
            movement.tick(Vector2.up, true, false, .02f);
            require(!player.squeezed, "Hold in open water does not slow player");
        }

        private static void prepareBaitArena(Maze maze) //미끼 반사와 우회 경로를 확인할 독립 지형 구성
        {
            maze.initialize(4817, SwimDifficulty.Normal);
            foreach (MazeCell cell in maze.data.cells) cell.kind = TerrainKind.Wall;
            for (int y = 2; y <= 8; y++)
            for (int x = 2; x <= 10; x++) maze.data.cells[y * maze.data.width + x].kind = TerrainKind.Water;
            for (int y = 4; y <= 6; y++) maze.data.cells[y * maze.data.width + 6].kind = TerrainKind.Wall;
        }

        private static void verifyBaitRules(Maze maze) //발사와 반사 및 실제 포식자 유인 규칙 검사
        {
            prepareBaitArena(maze);
            Vector2 launch = maze.data.center(5 * maze.data.width + 3); //벽 왼쪽 발사 위치
            require(maze.placeBait(launch, Vector2.right), "Bait launches from open water");
            BaitData bait = maze.baits[0]; //발사된 미끼
            var aiObject = new GameObject("Lure verification", typeof(PredatorAIComponent)); //유인 판단 검사 대상
            PredatorAIComponent ai = aiObject.GetComponent<PredatorAIComponent>(); //판단 구성 요소
            ai.initialize();
            require(ai.findBait(maze, launch, .3f) == null, "Flying bait does not attract");
            for (int frame = 0; frame < 150 && !bait.settled; frame++) maze.tick(.02f);
            require(bait.settled && bait.bounceCount == 1, "Wall impact bounces then settles");
            require(bait.position.x > launch.x && bait.position.x < 6 * maze.data.tileSize - .5f, "Bait remains on near side of wall");
            require(maze.canOccupy(bait.position, .18f, true), "Bounced bait stays outside obstacles");
            Vector2 stopped = bait.position; //정지한 미끼 위치
            float lifetime = bait.remaining; //일시정지 전 수명
            maze.tick(0f);
            require(bait.position == stopped && bait.remaining == lifetime, "Pause freezes bait state");
            Vector2 predatorPosition = maze.data.center(5 * maze.data.width + 7); //벽 뒤에서 우회할 수 있는 위치
            require(!maze.hasSight(predatorPosition, bait.position), "Lure fixture is behind a wall");
            ai.initialize();
            require(ai.findBait(maze, predatorPosition, .3f) == bait, "Nearby predator senses reachable bait behind wall");
            ai.advance(false, predatorPosition, Vector2.zero, predatorPosition, .1f, bait);
            require(ai.state == PredatorState.Lure && ai.target == bait.position, "Lure state targets bait");
            ai.advance(true, predatorPosition, launch, predatorPosition, .1f, bait);
            require(ai.state == PredatorState.Notice && ai.target == launch, "Seeing player overrides lure");
            ai.advance(true, predatorPosition, launch, predatorPosition, .81f, bait);
            require(ai.state == PredatorState.Chase, "Visible player is chased instead of bait");
            ai.advance(false, predatorPosition, launch, predatorPosition, .1f, bait);
            require(ai.state == PredatorState.Lure, "Losing player allows bait distraction");
            ai.initialize();
            require(ai.findBait(maze, bait.position + Vector2.up * (bait.attractDistance + .1f), .3f) == null, "Outside attraction radius ignores bait");
            for (int y = 2; y <= 8; y++) maze.data.cells[y * maze.data.width + 6].kind = TerrainKind.Wall;
            ai.initialize();
            require(ai.findBait(maze, predatorPosition, .3f) == null, "No reachable path means no lure");
            for (int y = 2; y <= 3; y++) maze.data.cells[y * maze.data.width + 6].kind = TerrainKind.Water;
            var moverObject = new GameObject("Lure movement verification", typeof(PredatorMovementComponent)); //유인 이동 검사 대상
            PredatorMovementComponent mover = moverObject.GetComponent<PredatorMovementComponent>(); //실제 길찾기 이동 구성 요소
            mover.initialize(maze, maze.data.cellAt(predatorPosition), SwimDifficulty.Normal, 14);
            for (int frame = 0; frame < 900 && Vector2.Distance(moverObject.transform.position, bait.position) >= .65f; frame++) mover.tick(bait.position, PredatorState.Lure, .02f);
            require(Vector2.Distance(moverObject.transform.position, bait.position) < .65f, "Lured predator actually walks around wall to bait");
            UnityEngine.Object.DestroyImmediate(moverObject);
            maze.consumeBait(bait);
            ai.advance(false, predatorPosition, launch, predatorPosition, .1f);
            require(maze.baits.Count == 0 && ai.state != PredatorState.Lure, "Consumed bait ends lure");
            UnityEngine.Object.DestroyImmediate(aiObject);

            prepareBaitArena(maze);
            for (int y = 4; y <= 6; y++) maze.data.cells[y * maze.data.width + 6].kind = TerrainKind.Water;
            require(maze.placeBait(launch, Vector2.right), "Unobstructed launch");
            bait = maze.baits[0];
            for (int frame = 0; frame < 150 && !bait.settled; frame++) maze.tick(.02f);
            require(bait.settled && Mathf.Abs(Vector2.Distance(launch, bait.position) - 6.6f) < .02f, "Launch distance limits flight");
            maze.tick(8.1f);
            require(maze.baits.Count == 0, "Settled bait expires after eight seconds");

            prepareBaitArena(maze);
            maze.data.cells[3 * maze.data.width + 6].kind = TerrainKind.Wall;
            maze.data.cells[4 * maze.data.width + 5].kind = TerrainKind.Wall;
            Vector2 corner = maze.data.center(3 * maze.data.width + 5); //두 벽이 만나는 발사 위치
            require(maze.placeBait(corner, Vector2.one), "Corner launch");
            bait = maze.baits[0];
            for (int frame = 0; frame < 150 && !bait.settled; frame++) maze.tick(.02f);
            require(bait.settled && bait.bounceCount == 1 && maze.canOccupy(bait.position, .18f, true), "Corner reflection settles outside walls");
        }

        private static void verifyGrassContact(Maze maze, Player player) //게임 진행 루프에서 풀숲의 접촉 보호와 해제 검사
        {
            SwimGame game = UnityEngine.Object.FindFirstObjectByType<SwimGame>(); //실제 게임 진행 진입점
            typeof(SwimGame).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
            game.begin(SwimDifficulty.Practice);
            int grass = Array.FindIndex(maze.data.cells, cell => cell.kind == TerrainKind.Grass); //은신할 수풀 칸
            Predator[] predators = game.GetComponentsInChildren<Predator>(); //실제 프리팹에서 생성된 포식자
            foreach (Predator predator in predators) predator.transform.position = maze.data.center(maze.data.exit);
            player.transform.position = maze.data.center(grass);
            predators[0].transform.position = player.position;
            MethodInfo update = typeof(SwimGame).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic); //실제 접촉 결과 처리 루프
            update.Invoke(game, null);
            require(player.hidden && game.phase == SwimPhase.Playing, "Full grass cover prevents contact loss in game loop");
            player.transform.position = maze.data.center(grass) + Vector2.right * .8f;
            predators[0].transform.position = player.position;
            update.Invoke(game, null);
            require(!player.hidden && game.phase == SwimPhase.Lost, "Leaving full cover restores contact loss");
        }

        private static void require(bool condition, string description) //불일치가 있으면 배치 검증 실패
        {
            if (!condition) throw new InvalidOperationException("TEUMSAE_TEST_FAILED: " + description);
            assertions++;
        }
    }
}
