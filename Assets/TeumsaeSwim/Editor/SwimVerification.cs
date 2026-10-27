using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
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
            verifyGeneratedMazes(generator, maze.GetComponent<MazeItemComponent>());
            maze.initialize(4817, SwimDifficulty.Normal);
            int gap = Array.FindIndex(maze.data.cells, cell => cell.kind == TerrainKind.Gap); //틈새 검사 위치
            int net = Array.FindIndex(maze.data.cells, cell => cell.kind == TerrainKind.Net); //그물 검사 위치
            Vector2 gapPosition = maze.data.center(gap); //틈새 중심
            Vector2 gapAxis = maze.data.cells[gap].horizontal ? Vector2.right : Vector2.up; //틈새의 통과 방향
            require(!maze.canOccupy(gapPosition, .36f), "Full body cannot enter gap");
            require(maze.canOccupy(gapPosition, .22f, false, true), "Squeezed body passes gap");
            require(!maze.canOccupy(gapPosition, .22f, true, true), "Predator cannot enter gap");
            require(maze.hasSight(gapPosition - gapAxis, gapPosition + gapAxis), "Gap is not automatic invisibility");
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
            movement.tick(gapAxis, false, false, .2f);
            require(Vector2.Distance(player.position, gapPosition) < .001f && player.squeezed, "Releasing hold inside gap freezes squeezed body");
            movement.tick(gapAxis, true, false, .2f);
            require(Vector2.Dot(player.position - gapPosition, gapAxis) > 0f, "Holding resumes gap movement");
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
            for (int trial = 0; trial < 4; trial++)
            {
                maze.initialize(6129 + trial, difficulty);
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
            verifyItemRules(maze, player);
            assertions += PredatorTypeVerification.run(maze, player, UnityEngine.Object.FindFirstObjectByType<SwimGame>());
            Directory.CreateDirectory("Logs/TeumsaeSwim");
            File.WriteAllText("Logs/TeumsaeSwim/verification.txt", "PASS: " + assertions + " assertions; 600 randomized mazes and pearl placements; deterministic replay, endpoint diversity, safe-room entry/contact/bait and distance progress; moving net release, contextual hold, projectile bounce, lure radius/path/priority, 0.9s notice, grass contact protection, pause, outcome, 12 exit routes and physical start-to-pearl-to-exit traversal; item single pickup, wall occlusion, carrying, delivery, loss, optional escape, HUD and restart resets.\n");
            Debug.Log("TEUMSAE_VERIFICATION_OK: " + assertions + " assertions");
            File.AppendAllText("Logs/TeumsaeSwim/verification.txt", "Predator types: 600 deterministic rosters; four distinct sprites; per-type range, notice thresholds, wall occlusion, actual patrol/chase/lure movement across three difficulties, reset, bait consumption, facing/animation, same-maze retry and all-type grass/safe contact protection.\n");
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
        }

        private static void verifyMovingNet(Maze maze, Player player, PlayerMovementComponent movement, PlayerInteractionComponent interaction) //이동 입력과 그물 해제의 동시 실행 검사
        {
            maze.initialize(4817, SwimDifficulty.Normal);
            player.initialize(maze);
            int net = Array.FindIndex(maze.data.cells, cell => cell.kind == TerrainKind.Net); //검사할 닫힌 그물
            Vector2 center = maze.data.center(net); //그물 중심
            Vector2 axis = maze.data.cells[net].horizontal ? Vector2.right : Vector2.up; //그물의 통과 방향
            player.transform.position = center - axis * .9f;
            for (int frame = 0; frame < 30; frame++)
            {
                interaction.tick(player.position, axis, axis, true, false, 1f / 60f);
                movement.tick(axis, true, interaction.working, 1f / 60f);
            }
            require(interaction.working && !player.squeezed, "Moving hold works net without squeezing");
            require(Vector2.Dot(player.position - center, axis) > -.9f, "Net work no longer freezes movement");
            float saved = maze.data.cells[net].netProgress; //중단 전 작업 진행도
            interaction.tick(player.position, axis, axis, false, false, .2f);
            require(Mathf.Approximately(saved, maze.data.cells[net].netProgress), "Released hold preserves net progress");
            for (int frame = 0; frame < 120; frame++)
            {
                interaction.tick(player.position, axis, axis, true, false, 1f / 60f);
                movement.tick(axis, true, interaction.working, 1f / 60f);
            }
            require(maze.data.cells[net].netOpen && Vector2.Dot(player.position - center, axis) > .2f, "Held movement crosses newly opened net");
            player.initialize(maze);
            movement.tick(axis, true, false, .02f);
            require(!player.squeezed, "Hold in open water does not slow player");
        }

        private static void prepareBaitArena(Maze maze) //미끼 반사와 우회 경로를 확인할 독립 지형 구성
        {
            maze.initialize(4817, SwimDifficulty.Normal);
            foreach (MazeCell cell in maze.data.cells) { cell.kind = TerrainKind.Wall; cell.safeZone = false; }
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
            typeof(Player).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
            typeof(SwimGame).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
            game.begin(SwimDifficulty.Practice);
            verifySafeRooms(maze, player, game);
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


        private static void verifyGeneratedMazes(MazeGenerationComponent generator, MazeItemComponent items) //연결과 형태 다양성 및 아이템 배치 재현성 검사
        {
            foreach (SwimDifficulty difficulty in Enum.GetValues(typeof(SwimDifficulty)))
            {
                var layouts = new HashSet<string>(); //지형 배치의 중복 확인
                var starts = new HashSet<int>(); //시작 위치의 다양성
                var exits = new HashSet<int>(); //출구 위치의 다양성
                int left = 0, right = 0, below = 0, above = 0; //출구가 위치한 네 방향의 횟수
                for (int seed = 0; seed < 200; seed++)
                {
                    MazeData data = generator.generate(seed, difficulty); //서로 다른 시드의 미로
                    require(generator.validate(data), "Connected random maze " + difficulty + " seed " + seed);
                    require(data.width == (difficulty == SwimDifficulty.Practice ? 25 : difficulty == SwimDifficulty.Normal ? 31 : 37) && data.height == data.width, "Two-dimensional maze dimensions");
                    require(data.homes.Count == (difficulty == SwimDifficulty.Practice ? 3 : difficulty == SwimDifficulty.Normal ? 6 : 9), "Predator count");
                    require(data.route.Count >= data.width * 1.4f, "Endpoint path distance");
                    require(Vector2.Distance(data.center(data.start), data.center(data.exit)) >= data.width * data.tileSize * .4f, "Endpoint spatial separation");
                    int[] connected = data.distancesFrom(data.start); //시작과 연결된 물길
                    int item = items.selectSpawnCell(data); //진주 배치 후보
                    require(data.cells[item].kind == TerrainKind.Water && !data.cells[item].safeZone, "Pearl is on clear water outside safe rooms");
                    require(connected[item] > 0 && data.exitDistances[item] > 0 && connected[item] + data.exitDistances[item] > connected[data.exit], "Pearl is reachable and requires a detour");
                    require(connected[item] >= 6 && data.exitDistances[item] >= 6, "Pearl keeps distance from both endpoints");
                    foreach (int home in data.homes) require(Vector2.Distance(data.center(home), data.center(item)) >= data.tileSize * 2f, "Pearl is not beside predator spawn");
                    if (seed < 8) require(item == items.selectSpawnCell(generator.generate(seed, difficulty)), "Same seed recreates pearl placement");
                    int safeCells = 0, junctions = 0, deadEnds = 0, gaps = 0, nets = 0; //생성된 지형의 구조 집계
                    bool validCells = true; //외벽과 연결 및 안전구역 조건
                    var layout = new StringBuilder(); //벽 배치만 비교할 지문
                    for (int id = 0; id < data.cells.Length; id++)
                    {
                        MazeCell cell = data.cells[id]; //현재 칸
                        layout.Append(cell.kind == TerrainKind.Wall ? '#' : '.');
                        int x = id % data.width, y = id / data.width; //현재 좌표
                        if (x == 0 || y == 0 || x == data.width - 1 || y == data.height - 1) validCells &= cell.kind == TerrainKind.Wall;
                        if (!data.passable(id, false)) continue;
                        validCells &= connected[id] >= 0;
                        if (cell.safeZone) { safeCells++; validCells &= cell.kind == TerrainKind.Water && !data.passable(id, true); }
                        int neighbors = 0; //인접 물길 수
                        foreach (int neighbor in data.neighbors(id)) if (data.passable(neighbor, false)) neighbors++;
                        if (neighbors >= 3) junctions++;
                        if (neighbors == 1) deadEnds++;
                        if (cell.kind == TerrainKind.Gap) gaps++;
                        if (cell.kind == TerrainKind.Net) nets++;
                    }
                    require(validCells && safeCells == 18, "Connected water, closed outer walls and two clear 3x3 safe rooms");
                    require(junctions >= 10 && deadEnds >= 5, "Random branches and dead ends");
                    require(gaps == nets && gaps == (difficulty == SwimDifficulty.Practice ? 2 : difficulty == SwimDifficulty.Normal ? 3 : 4), "Mixed obstacles");
                    bool mainGap = false, mainNet = false; //실제 출구 경로의 상호작용
                    float previous = -1f; //경로를 따라 증가해야 하는 진행률
                    foreach (int id in data.route)
                    {
                        mainGap |= data.cells[id].kind == TerrainKind.Gap;
                        mainNet |= data.cells[id].kind == TerrainKind.Net;
                        float progress = data.progressAt(data.center(id)); //출구 방향에 독립적인 진행률
                        require(!float.IsNaN(progress) && progress > previous, "Route progress increases in any direction");
                        previous = progress;
                    }
                    require(mainGap && mainNet, "Exit route includes gap and net");
                    require(Mathf.Approximately(data.progressAt(data.center(data.start)), 0f) && Mathf.Approximately(data.progressAt(data.center(data.exit)), 1f), "Progress starts at zero and ends at one");
                    foreach (int home in data.homes)
                        require(!data.cells[home].safeZone && Vector2.Distance(data.center(home), data.center(data.start)) >= data.tileSize * 6f && Vector2.Distance(data.center(home), data.center(data.exit)) >= data.tileSize * 6f, "Both endpoints exclude nearby predator spawns");
                    layouts.Add(layout.ToString());
                    starts.Add(data.start);
                    exits.Add(data.exit);
                    Vector2 difference = data.center(data.exit) - data.center(data.start); //시작에서 출구를 향한 방향
                    if (difference.x < 0) left++;
                    if (difference.x > 0) right++;
                    if (difference.y < 0) below++;
                    if (difference.y > 0) above++;
                    if (seed < 8) require(fingerprint(data) == fingerprint(generator.generate(seed, difficulty)), "Same seed recreates full layout and placement");
                }
                require(layouts.Count == 200 && starts.Count > 40 && exits.Count > 20, "New seeds vary maze and both endpoints");
                require(left > 10 && right > 10 && below > 10 && above > 10, "Exit is not fixed above or on one side");
            }
        }

        private static string fingerprint(MazeData data) //지형과 안전구역 및 포식자 배치 재현 확인
        {
            var text = new StringBuilder(); //비교할 전체 상태
            text.Append(data.start).Append(':').Append(data.exit).Append(':');
            foreach (MazeCell cell in data.cells) text.Append((int)cell.kind).Append(cell.horizontal ? 'h' : 'v').Append(cell.safeZone ? 's' : '-');
            foreach (int home in data.homes) text.Append(':').Append(home);
            return text.ToString();
        }

        private static void verifySafeRooms(Maze maze, Player player, SwimGame game) //실제 충돌과 행동 루프의 양쪽 안전구역 보호 검사
        {
            MethodInfo update = typeof(SwimGame).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic); //실제 게임 갱신
            Predator[] predators = game.GetComponentsInChildren<Predator>(); //실제 포식자 구성
            foreach (Predator predator in predators) predator.transform.position = maze.data.center(maze.data.homes[0]);
            var probe = new GameObject("Safe zone probe", typeof(PredatorAIComponent), typeof(PredatorMovementComponent)); //발견과 이동 검사 대상
            PredatorAIComponent ai = probe.GetComponent<PredatorAIComponent>(); //발견 검사 구성
            PredatorMovementComponent movement = probe.GetComponent<PredatorMovementComponent>(); //추격과 유인 이동 검사 구성
            foreach (int endpoint in new[] { maze.data.start, maze.data.exit })
            {
                Vector2 center = maze.data.center(endpoint); //안전방 중심
                player.transform.position = center + Vector2.right * maze.data.tileSize;
                require(player.safe && !player.hidden, "Safe room protects without grass");
                require(!ai.canSee(maze, player.position + Vector2.right, Vector2.left, player), "Predator cannot detect player in safe room");
                predators[0].transform.position = player.position;
                update.Invoke(game, null);
                require(game.phase == SwimPhase.Playing, "Safe room contact never loses");
                int boundary = -1, outside = -1; //물길로 열린 안전구역 경계
                for (int y = -1; y <= 1 && outside < 0; y++)
                for (int x = -1; x <= 1 && outside < 0; x++)
                {
                    int id = endpoint + y * maze.data.width + x; //검사할 안전 칸
                    foreach (int adjacent in maze.data.neighbors(id))
                        if (!maze.data.cells[adjacent].safeZone && maze.data.passable(adjacent, true)) { boundary = id; outside = adjacent; break; }
                }
                require(outside >= 0, "Safe room has an open approach");
                Vector2 from = maze.data.center(outside); //안전구역 밖의 접근 위치
                Vector2 direction = (maze.data.center(boundary) - from).normalized; //안전구역 안쪽 방향
                Vector2 stopped = maze.move(from, direction * maze.data.tileSize * 1.5f, .43f, true, false); //직접 충돌 이동 결과
                require(!maze.isSafeZone(stopped) && maze.canOccupy(stopped, .43f, true), "Predator body stops outside safe boundary");
                require(maze.data.findPath(outside, endpoint, true).Count == 0, "Predator path cannot enter safe room");
                movement.initialize(maze, outside, SwimDifficulty.Normal, 17);
                for (int frame = 0; frame < 60; frame++) movement.tick(center, PredatorState.Chase, .02f);
                require(!maze.isSafeZone(probe.transform.position), "Chasing cannot enter safe room");
                for (int frame = 0; frame < 60; frame++) movement.tick(center, PredatorState.Lure, .02f);
                require(!maze.isSafeZone(probe.transform.position), "Lure cannot lead predator into safe room");
                require(maze.placeBait(maze.data.center(boundary), -direction), "Bait launches inside safe room");
                BaitData bait = maze.baits[maze.baits.Count - 1]; //경계를 넘어 나갈 미끼
                for (int frame = 0; frame < 20; frame++) maze.tick(.01f);
                require(!maze.isSafeZone(bait.position) && bait.bounceCount == 0, "Bait crosses safe boundary without reflection");
                maze.consumeBait(bait);
                predators[0].transform.position = maze.data.center(maze.data.homes[0]);
            }
            UnityEngine.Object.DestroyImmediate(probe);
            player.initialize(maze);
        }

        private static void verifyItemRules(Maze maze, Player player) //실제 플레이 흐름에서 진주 획득과 운반 및 초기화 검사
        {
            SwimGame game = UnityEngine.Object.FindFirstObjectByType<SwimGame>(); //이미 초기화된 게임 진입점
            GameUIComponent ui = game.GetComponent<GameUIComponent>(); //아이템 상태를 표시할 UI
            GameSessionComponent session = game.GetComponent<GameSessionComponent>(); //운반 성공 결과 저장소
            MazeItemComponent items = maze.GetComponent<MazeItemComponent>(); //현재 판의 진주 구성 요소
            MethodInfo update = typeof(SwimGame).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic); //실제 게임 진행 루프
            const string recordKey = "teumsae.best.Practice"; //검사에서 보존할 사용자 최고 기록
            bool hadRecord = PlayerPrefs.HasKey(recordKey); //기존 기록 존재 여부
            float savedRecord = PlayerPrefs.GetFloat(recordKey); //검사 전 최고 기록
            int pickupCues = 0; //실제 획득 알림 횟수
            Action<SwimCue> countPickup = cue => { if (cue == SwimCue.Pickup && player.message.StartsWith("진주를 찾았어")) pickupCues++; }; //필드 미끼와 구분한 진주 중복 획득 감지
            player.cue += countPickup;
            try
            {
                game.begin(SwimDifficulty.Practice);
                parkPredators(game, maze);
                int originalCell = items.cell; //같은 미로 재도전에서 유지할 아이템 위치
                string originalLayout = fingerprint(maze.data); //재도전할 지형과 배치
                Vector2 itemPosition = maze.data.center(originalCell); //진주 접촉 위치
                require(items.available && !player.hasCargo && !game.cargoDelivered, "New game starts with pearl available and empty inventory");
                require(uiText(ui, "cargoLabel") == "진주 미획득", "HUD shows missing pearl");
                require(!maze.tryCollectItem(player.position, player.radius) && items.available, "Distant pearl is not collected");
                verifyItemOcclusion(maze, items);
                require(items.cell == originalCell && items.available, "Occlusion fixture restores original pearl");

                game.pause();
                player.transform.position = itemPosition;
                update.Invoke(game, null);
                require(!player.hasCargo && items.available, "Paused game cannot pick up pearl");
                player.transform.position = maze.data.center(maze.data.start);
                game.resume();
                traverseCargoRoute(maze, player, maze.data.findPath(maze.data.start, items.cell));
                require(player.hasCargo && !items.available && pickupCues == 1, "Walking into pearl collects exactly once: carrying=" + player.hasCargo + " available=" + items.available + " cues=" + pickupCues);
                require(!items.transform.Find("Collectible pearl").gameObject.activeSelf, "Collected pearl disappears from map");
                require(!maze.tryCollectItem(itemPosition, player.radius), "Duplicate pickup is rejected");
                update.Invoke(game, null);
                require(uiText(ui, "cargoLabel") == "진주 운반 중", "HUD shows carried pearl");
                int inventoryBeforeShot = player.baitCount; //경로에서 보충했을 수 있는 실제 미끼 수
                player.useBait();
                player.tick(.02f);
                require(player.hasCargo && player.baitCount == inventoryBeforeShot - 1 && pickupCues == 1, "Bait use preserves cargo without duplicate pickup cue");
                game.pause();
                update.Invoke(game, null);
                require(player.hasCargo && !items.available && !game.cargoDelivered, "Pause preserves inventory without delivering or respawning item");
                game.resume();
                traverseCargoRoute(maze, player, maze.data.findPath(items.cell, maze.data.exit));
                update.Invoke(game, null);
                require(game.phase == SwimPhase.Won && game.cargoDelivered, "Carried pearl is delivered only on successful escape");
                require(uiText(ui, "resultCargo").Contains("운반 성공"), "Result shows successful delivery");
                session.advance(10f, true, false, false);
                require(game.cargoDelivered && game.phase == SwimPhase.Won, "Finished delivery result cannot be changed by later updates");

                game.begin(SwimDifficulty.Practice, false);
                parkPredators(game, maze);
                require(items.cell == originalCell && fingerprint(maze.data) == originalLayout, "Retry preserves maze and pearl location");
                require(!player.hasCargo && items.available && !game.cargoDelivered, "Retry resets carried item and previous delivery result");
                player.transform.position = maze.data.center(maze.data.exit);
                update.Invoke(game, null);
                require(game.phase == SwimPhase.Won && !game.cargoDelivered, "Escape without pearl still succeeds");
                require(uiText(ui, "resultCargo").Contains("미획득"), "Optional escape reports missing pearl");

                game.begin(SwimDifficulty.Practice, false);
                parkPredators(game, maze);
                player.transform.position = maze.data.center(items.cell);
                update.Invoke(game, null);
                require(player.hasCargo, "Pearl can be picked up again on retry");
                game.GetComponentsInChildren<Predator>()[0].transform.position = player.position;
                update.Invoke(game, null);
                require(game.phase == SwimPhase.Lost && !game.cargoDelivered, "Being caught while carrying never counts as delivery");
                require(uiText(ui, "resultCargo") == "진주 운반 실패", "Loss reports failed delivery");
                game.begin(SwimDifficulty.Practice);
                require(items.available && !player.hasCargo && !game.cargoDelivered, "New maze clears item and outcome state");
                session.advance(.1f, true, true, true);
                require(session.phase == SwimPhase.Lost && !session.cargoDelivered, "Catch takes precedence over simultaneous cargo escape");
            }
            finally
            {
                player.cue -= countPickup;
                if (hadRecord) PlayerPrefs.SetFloat(recordKey, savedRecord);
                else PlayerPrefs.DeleteKey(recordKey);
                PlayerPrefs.Save();
            }
        }

        private static void verifyItemOcclusion(Maze maze, MazeItemComponent items) //획득 반경을 넓혀도 벽 너머의 진주를 획득하지 못하는지 검사
        {
            int step = items.cell % maze.data.width >= 3 ? -1 : 1; //외벽을 넘지 않는 검사 방향
            int wall = items.cell + step; //진주와 접근 위치 사이의 장애물
            int origin = items.cell + step * 2; //진주를 향해 접근할 위치
            TerrainKind oldWall = maze.data.cells[wall].kind; //복구할 중간 지형
            TerrainKind oldOrigin = maze.data.cells[origin].kind; //복구할 출발 지형
            var settings = new SerializedObject(items); //Inspector 획득 반경 조정
            float oldRadius = settings.FindProperty("pickupRadius").floatValue; //복구할 획득 반경
            try
            {
                settings.FindProperty("pickupRadius").floatValue = maze.data.tileSize * 3f;
                settings.ApplyModifiedPropertiesWithoutUndo();
                maze.data.cells[origin].kind = TerrainKind.Water;
                maze.data.cells[wall].kind = TerrainKind.Wall;
                require(!maze.tryCollectItem(maze.data.center(origin), .36f) && items.available, "Wall blocks pickup even within configured radius");
                maze.data.cells[wall].kind = TerrainKind.Water;
                require(maze.tryCollectItem(maze.data.center(origin), .36f), "Removing intervening wall allows the same pickup");
            }
            finally
            {
                maze.data.cells[wall].kind = oldWall;
                maze.data.cells[origin].kind = oldOrigin;
                settings.Update();
                settings.FindProperty("pickupRadius").floatValue = oldRadius;
                settings.ApplyModifiedPropertiesWithoutUndo();
                items.initialize(maze);
            }
        }

        private static void traverseCargoRoute(Maze maze, Player player, List<int> route) //실제 입력과 이동으로 진주를 거쳐 출구까지 통과
        {
            require(route.Count > 1, "Cargo route exists");
            int waypoint = 1; //다음에 방문할 경로 칸
            int frames = 0; //무한 반복을 막을 제한 프레임
            while (waypoint < route.Count && frames++ < 60000)
            {
                Vector2 delta = maze.data.center(route[waypoint]) - player.position; //다음 경유 위치
                if (delta.magnitude < .12f) { waypoint++; continue; }
                player.setMove(delta.normalized);
                player.setHold(maze.nearbyNet(player.position) >= 0 || maze.overlapsGap(player.position, .95f));
                player.tick(1f / 60f);
                maze.tick(1f / 60f);
            }
            player.resetInput();
            require(waypoint == route.Count, "Physical cargo route traversal at waypoint " + waypoint);
        }

        private static void parkPredators(SwimGame game, Maze maze) //아이템 흐름 검사 중 포식자를 이동할 수 없는 안전구역에 격리
        {
            foreach (Predator predator in game.GetComponentsInChildren<Predator>()) predator.transform.position = maze.data.center(maze.data.start);
        }

        private static string uiText(GameUIComponent ui, string field) //실제 생성된 UI 안내 문구 조회
        {
            return field == "cargoLabel" ? ui.cargoText : ui.cargoResult;
        }

        private static void require(bool condition, string description) //불일치가 있으면 배치 검증 실패
        {
            if (!condition) throw new InvalidOperationException("TEUMSAE_TEST_FAILED: " + description);
            assertions++;
        }
    }
}
