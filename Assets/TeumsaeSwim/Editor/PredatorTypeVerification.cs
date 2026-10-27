using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TeumsaeSwim.Editor
{
    public static class PredatorTypeVerification
    {
        private static int assertions; //종류별 검증 통과 수

        public static void verifyHiddenSight() //시야를 숨긴 네 종류의 행동과 화면을 실제 씬에서 검증
        {
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            try
            {
                Maze maze = UnityEngine.Object.FindFirstObjectByType<Maze>(); //실제 미로
                Player player = UnityEngine.Object.FindFirstObjectByType<Player>(); //실제 플레이어
                SwimGame game = UnityEngine.Object.FindFirstObjectByType<SwimGame>(); //실제 게임 파사드
                typeof(Player).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
                typeof(SwimGame).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
                int count = run(maze, player, game); //종류별 탐지와 상태 표시 검사 수
                Debug.Log("SUMBAKKOGI_HIDDEN_SIGHT_OK: " + count + " assertions");
            }
            finally { EditorSceneManager.OpenScene(SwimProjectSetup.scenePath); }
            PredatorTypePreview.capture();
        }

        public static int run(Maze maze, Player player, SwimGame game) //실제 자원과 파사드를 사용하는 포식자 종류 회귀 검사
        {
            assertions = 0;
            PredatorRosterComponent roster = game.GetComponent<PredatorRosterComponent>(); //실제 씬의 조합 구성
            require(roster != null && roster.types.Count == 4, "Four profiles connected");
            verifyRosters(roster);
            Predator prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TeumsaeSwim/Prefabs/Predator.prefab").GetComponent<Predator>(); //기존 프리팹
            var spriteIds = new HashSet<int>(); //같은 그림을 중복 연결하지 않았는지 검사
            foreach (PredatorTypeProfile profile in roster.types)
            {
                require(profile.sprite != null && spriteIds.Add(profile.sprite.GetInstanceID()), "Distinct sprite for " + profile.type);
                require(profile.sprite.rect.width > 200 && profile.sprite.rect.height > 150, "Uncropped sprite size " + profile.type);
                verifyType(maze, player, prefab, profile);
            }
            verifyGameProtection(maze, player, game);
            return assertions;
        }

        private static void verifyRosters(PredatorRosterComponent roster) //여러 시드에서 구성 다양성과 재현 및 중복 상한 검사
        {
            foreach (SwimDifficulty difficulty in Enum.GetValues(typeof(SwimDifficulty)))
            {
                var combinations = new HashSet<string>(); //시드에 따라 바뀌는 순서
                int count = difficulty == SwimDifficulty.Practice ? 3 : difficulty == SwimDifficulty.Normal ? 12 : 18; //실제 난이도별 포식자 수
                for (int seed = 0; seed < 200; seed++)
                {
                    PredatorTypeProfile[] first = roster.createRoster(seed, count, difficulty); //첫 조합
                    PredatorTypeProfile[] replay = roster.createRoster(seed, count, difficulty); //같은 시드의 조합
                    require(first.SequenceEqual(replay), "Deterministic roster " + difficulty);
                    require(first.Length == count, "Population unchanged");
                    require(first.Take(Mathf.Min(4, count)).Select(profile => profile.type).Distinct().Count() == Mathf.Min(4, count), "No duplicates in first bag");
                    require(first.GroupBy(profile => profile.type).All(group => group.Count() <= Mathf.CeilToInt(count / 4f)), "No concentrated specialist population");
                    if (difficulty == SwimDifficulty.Practice) require(first[0].type == PredatorKind.Balanced, "Practice always includes balanced fish");
                    combinations.Add(string.Join(",", first.Select(profile => profile.type)));
                }
                require(combinations.Count >= 3, "Roster varies across seeds " + difficulty);
            }
        }

        private static void prepareArena(Maze maze, Player player) //동일한 열린 통로에서 종류별 거리와 속도를 비교
        {
            maze.initialize(4817, SwimDifficulty.Normal);
            for (int y = 2; y <= 11; y++)
            for (int x = 2; x <= 11; x++)
            {
                int id = y * maze.data.width + x; //검사에 사용할 물길
                maze.data.cells[id].kind = TerrainKind.Water;
                maze.data.cells[id].safeZone = false;
            }
            player.initialize(maze);
        }

        private static void verifyType(Maze maze, Player player, Predator prefab, PredatorTypeProfile profile) //실제 탐지와 추격 및 이동과 미끼 전환 검사
        {
            prepareArena(maze, player);
            int home = 5 * maze.data.width + 5; //오른쪽을 바라보며 시작하는 공통 위치
            Vector2 origin = maze.data.center(home); //중앙 검사 위치
            Predator predator = UnityEngine.Object.Instantiate(prefab); //실제 컴포넌트가 연결된 포식자
            try
            {
                predator.initialize(maze, player, home, SwimDifficulty.Normal, 67, profile);
                PredatorAIComponent ai = predator.GetComponent<PredatorAIComponent>(); //종류가 적용된 AI
                PredatorMovementComponent mover = predator.GetComponent<PredatorMovementComponent>(); //종류가 적용된 이동
                PredatorVisualComponent visual = predator.GetComponent<PredatorVisualComponent>(); //물고기와 발견 신호 표시
                require(predator.kind == profile.type && Mathf.Approximately(ai.range, profile.range), "Facade applies type " + profile.type);
                float[] ranges = { 3.5f, 5.6f, 2.8f, 3.5f }; //승인한 초기 시야 거리
                float[] notices = { .9f, 1.1f, .45f, 1.2f }; //승인한 초기 발견 시간
                require(Mathf.Abs(profile.range - ranges[(int)profile.type]) < .001f && Mathf.Abs(profile.noticeTime - notices[(int)profile.type]) < .001f, "Initial balance " + profile.type);
                player.transform.position = origin + Vector2.right * (profile.range - .02f);
                player.tick(0);
                require(ai.canSee(maze, origin, Vector2.right, player), "Player inside type range");
                player.transform.position = origin + Vector2.right * (profile.range + .02f);
                player.tick(0);
                require(!ai.canSee(maze, origin, Vector2.right, player), "Player outside type range");
                player.transform.position = origin + Vector2.right * 2.5f;
                player.tick(0);
                maze.data.cells[home + 1].kind = TerrainKind.Wall;
                require(!ai.canSee(maze, origin, Vector2.right, player), "Even scout cannot see through rock");
                maze.data.cells[home + 1].kind = TerrainKind.Water;
                player.transform.position = origin + Vector2.right;
                player.tick(0);
                predator.tick(profile.noticeTime - .02f);
                require(predator.state == PredatorState.Notice && ai.noticeFraction > .9f, "Notice lasts correct time " + profile.type);
                require(predator.GetComponentsInChildren<TextMesh>().Any(text => text.text == "?"), "Notice marker visible");
                predator.tick(.03f);
                require(predator.state == PredatorState.Chase, "Chase threshold " + profile.type);
                require(predator.GetComponentsInChildren<TextMesh>().Any(text => text.text == "!"), "Chase marker visible");
                player.transform.position = origin - Vector2.right * 8f;
                player.tick(0);
                predator.tick(.01f);
                require(predator.state != PredatorState.Chase && ai.noticeProgress == 0, "Lost sight resets each type");
                ai.initialize(profile);
                require(Mathf.Approximately(ai.range, profile.range) && ai.noticeFraction == 0, "AI reset does not compound settings");

                foreach (SwimDifficulty difficulty in Enum.GetValues(typeof(SwimDifficulty)))
                foreach (PredatorState state in new[] { PredatorState.Patrol, PredatorState.Chase, PredatorState.Lure })
                {
                    mover.initialize(maze, home, difficulty, 67, profile);
                    mover.initialize(maze, home, difficulty, 67, profile);
                    mover.tick(origin + Vector2.right * 4f, state, .1f);
                    float baseSpeed = state == PredatorState.Chase ? 3.7f : state == PredatorState.Lure ? 2.1f : 1.22f; //기존 프리팹 기준 속도
                    float scale = difficulty == SwimDifficulty.Practice ? .8f : difficulty == SwimDifficulty.Challenge ? 1.06f : 1f; //기존 난이도 배율
                    require(Mathf.Abs(Vector2.Distance(origin, predator.position) - baseSpeed * scale * profile.speedScale(state) * .1f) < .002f, "Physical movement scale " + profile.type + " " + difficulty + " " + state);
                }
                mover.initialize(maze, home, SwimDifficulty.Normal, 67, profile);
                ai.initialize(profile);
                require(maze.placeBait(origin, Vector2.right), "Bait available to every species");
                for (int frame = 0; frame < 150; frame++) maze.tick(.02f);
                BaitData bait = maze.baits[0]; //정지한 미끼
                player.transform.position = maze.data.center(maze.data.start);
                player.tick(0);
                predator.tick(.02f);
                require(predator.state == PredatorState.Lure && ai.target == bait.position, "Every type follows bait");
                for (int frame = 0; frame < 600 && maze.baits.Count > 0; frame++) predator.tick(.02f);
                require(maze.baits.Count == 0, "Every type reaches and eats bait");
                require(predator.GetComponentsInChildren<SpriteRenderer>().Any(renderer => renderer.sprite == profile.sprite), "Profile artwork rendered");
                Transform body = predator.transform.Find("Predator body"); //회전과 헤엄을 적용한 몸체
                Vector3 pose = body.localScale; //일시정지 전 자세
                visual.tick(mover.facing, predator.state, ai.noticeFraction, 0);
                require(body.localScale == pose, "Paused sprite animation does not advance");
                visual.tick(Vector2.left, PredatorState.Patrol, 0);
                require(body.localScale.y < 0 && Vector3.Dot(body.TransformVector(Vector3.up), Vector3.up) > 0, "Left-facing sprite stays upright");
                require(predator.GetComponentsInChildren<MeshFilter>(true).Length == 0 && predator.transform.Find("Field of view") == null,
                    "No visible or hidden sight cone is created");
                TextMesh marker = predator.transform.Find("Label ").GetComponent<TextMesh>(); //플레이어가 볼 발견 신호
                Transform gauge = predator.transform.Find("Notice gauge"); //발견 진행 게이지
                visual.tick(Vector2.right, PredatorState.Notice, .5f);
                require(marker.text == "?" && gauge.gameObject.activeSelf && Mathf.Approximately(gauge.localScale.x, .4f), "Notice keeps question mark and progress gauge");
                visual.tick(Vector2.right, PredatorState.Chase, 1);
                require(marker.text == "!" && !gauge.gameObject.activeSelf, "Chase keeps exclamation mark");
                visual.tick(Vector2.right, PredatorState.Lure, 0);
                require(marker.text == "?" && !gauge.gameObject.activeSelf, "Bait lure keeps question mark");
                visual.tick(Vector2.right, PredatorState.Patrol, 0);
                require(marker.text == "" && !gauge.gameObject.activeSelf, "Patrol hides alert indicators");
            }
            finally { UnityEngine.Object.DestroyImmediate(predator.gameObject); }
        }

        private static void verifyGameProtection(Maze maze, Player player, SwimGame game) //모든 종류에 실제 진행 루프의 풀숲 및 안전구역 보호 적용
        {
            MethodInfo update = typeof(SwimGame).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic); //실제 포획 판정 루프
            foreach (PredatorKind kind in Enum.GetValues(typeof(PredatorKind)))
            {
                game.begin(SwimDifficulty.Normal, false);
                PredatorKind[] before = game.GetComponentsInChildren<Predator>().Select(predator => predator.kind).ToArray(); //재도전 전 조합
                game.begin(SwimDifficulty.Normal, false);
                Predator[] predators = game.GetComponentsInChildren<Predator>(); //실제 네 종류 배치
                require(before.SequenceEqual(predators.Select(predator => predator.kind)), "Same maze retry keeps species order");
                Predator subject = predators.First(predator => predator.kind == kind); //접촉을 검사할 종류
                foreach (Predator predator in predators) predator.transform.position = maze.data.center(maze.data.exit);
                int grass = Array.FindIndex(maze.data.cells, cell => cell.kind == TerrainKind.Grass); //이번 미로의 수풀
                player.transform.position = maze.data.center(grass);
                subject.transform.position = player.position;
                update.Invoke(game, null);
                require(game.phase == SwimPhase.Playing && player.hidden && !subject.seesPlayer, "Grass blocks sight and contact " + kind);
                player.transform.position = maze.data.center(maze.data.start);
                subject.transform.position = player.position;
                update.Invoke(game, null);
                require(game.phase == SwimPhase.Playing && player.safe && !subject.seesPlayer, "Safe room blocks sight and contact " + kind);
                player.transform.position = maze.data.center(grass) + Vector2.right * .8f;
                subject.transform.position = player.position;
                update.Invoke(game, null);
                require(game.phase == SwimPhase.Lost && !player.hidden, "Outside grass restores capture " + kind);
            }
        }

        private static void require(bool condition, string message) //구체적인 실패 지점을 남기는 검사
        {
            if (!condition) throw new InvalidOperationException("TEUMSAE_TYPE_TEST_FAILED: " + message);
            assertions++;
        }
    }
}
