using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TeumsaeSwim.Editor
{
    public static class PredatorEncounterVerification
    {
        private static int checks; //배치 이후의 실제 순찰 이동 검사 수

        public static void verifyAndBuild() //생성 및 안전 규칙과 개체 종류를 검사한 뒤 WebGL 빌드
        {
            SwimVerification.run();
            SwimUIVerification.run();
            SwimRefreshVerification.run();
            run();
            SwimProjectSetup.build();
        }

        public static void verifyPatrolAndBuild() //이미 통과한 생성 검사를 반복하지 않고 순찰 검사 후 빌드
        {
            run();
            SwimProjectSetup.build();
        }

        public static void run() //기본 미로의 기준 동선과 닫힌 통로에서 실제 순찰 검증
        {
            checks = 0;
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            Maze maze = UnityEngine.Object.FindFirstObjectByType<Maze>(); //실제 충돌 지형 진입점
            var probes = new List<GameObject>(); //검사 후 제거할 순찰 개체
            float latestEncounter = 0; //검사한 기본 미로에서 가장 늦은 첫 조우
            try
            {
                for (int seed = 0; seed < 12; seed++)
                {
                    maze.initialize(9100 + seed, SwimDifficulty.Normal);
                    var movers = new List<PredatorMovementComponent>(); //플레이어를 미리 알지 못하는 순찰 상태만 사용
                    foreach (int home in maze.data.homes)
                    {
                        var probe = new GameObject("Patrol contact probe", typeof(PredatorMovementComponent)); //실제 이동 구성 요소
                        probes.Add(probe);
                        PredatorMovementComponent mover = probe.GetComponent<PredatorMovementComponent>(); //실제 이동 대상
                        mover.initialize(maze, home, SwimDifficulty.Normal, seed * 31 + movers.Count);
                        movers.Add(mover);
                    }
                    Vector2 position = maze.data.center(maze.data.start); //3.35 속도로 출구 경로를 따라갈 기준 플레이어
                    int waypoint = 1; //다음 경유 칸
                    float time = 0; //첫 조우까지 걸린 시간
                    bool encountered = false; //벽으로 가리지 않은 5 월드 단위 안에서의 조우
                    while (!encountered && waypoint < maze.data.route.Count && time < 180f)
                    {
                        foreach (PredatorMovementComponent mover in movers) mover.tick(mover.patrolTarget(.1f), PredatorState.Patrol, .1f);
                        Vector2 target = maze.data.center(maze.data.route[waypoint]); //검증된 다음 통로 중심
                        position = Vector2.MoveTowards(position, target, .335f);
                        if (Vector2.Distance(position, target) < .01f) waypoint++;
                        time += .1f;
                        foreach (PredatorMovementComponent mover in movers)
                            if (Vector2.Distance(position, mover.transform.position) <= 5f && maze.hasSight(position, mover.transform.position)) { encountered = true; break; }
                    }
                    require(encountered && time <= 60f, "Normal reference route meets a visible patrol within sixty seconds, seed " + seed + ", time " + time);
                    latestEncounter = Mathf.Max(latestEncounter, time);
                    foreach (GameObject probe in probes) UnityEngine.Object.DestroyImmediate(probe);
                    probes.Clear();
                }
                verifyPatrolBounds(maze, probes);
                Debug.Log("SUMBAKKOGI_ENCOUNTERS_VERIFIED: " + checks + " checks; 12 normal reference routes; latest first contact " + latestEncounter.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "s. Patrol-only simulation, not human play or a guaranteed encounter time.");
            }
            finally
            {
                foreach (GameObject probe in probes) UnityEngine.Object.DestroyImmediate(probe);
                EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            }
        }

        private static void verifyPatrolBounds(Maze maze, List<GameObject> probes) //넓어진 순찰과 장애물 개방 후 갱신 및 안전 경계 검사
        {
            foreach (MazeCell cell in maze.data.cells) { cell.kind = TerrainKind.Wall; cell.safeZone = false; }
            maze.data.route.Clear();
            int row = 12 * maze.data.width; //독립된 가로 통로
            for (int x = 2; x <= 26; x++) maze.data.cells[row + x].kind = TerrainKind.Water;
            for (int x = 21; x <= 24; x++) maze.data.route.Add(row + x);
            maze.data.cells[row + 5].kind = TerrainKind.Gap;
            maze.data.cells[row + 5].horizontal = true;
            maze.data.cells[row + 20].kind = TerrainKind.Net;
            maze.data.cells[row + 20].netOpen = false;
            maze.data.cells[row + 26].safeZone = true;
            int home = row + 12; //넓게 이동할 통로 중앙
            var probe = new GameObject("Patrol bounds probe", typeof(PredatorMovementComponent)); //실제 충돌을 적용할 순찰 대상
            probes.Add(probe);
            PredatorMovementComponent mover = probe.GetComponent<PredatorMovementComponent>(); //검사할 이동
            var settings = new SerializedObject(mover); //개방 후 구간을 확실히 선택하는 검사 조건
            settings.FindProperty("mainRoutePreference").floatValue = 1f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            mover.initialize(maze, home, SwimDifficulty.Normal, 42);
            float farthest = 0; //생성 위치에서 멀리 이동한 거리
            bool crossedOpenedNet = false; //개방 후 새로운 순찰 구간으로 이동했는지 여부
            for (int frame = 0; frame < 3600; frame++)
            {
                if (frame == 1800) maze.data.cells[row + 20].netOpen = true;
                mover.tick(mover.patrolTarget(.1f), PredatorState.Patrol, .1f);
                int cell = maze.data.cellAt(mover.transform.position); //몸체 중심이 점유한 실제 칸
                require(maze.data.passable(cell, true) && maze.canOccupy(mover.transform.position, mover.radius, true), "Patrol respects walls, gap, net and safe room");
                farthest = Mathf.Max(farthest, Vector2.Distance(mover.transform.position, maze.data.center(home)));
                crossedOpenedNet |= frame >= 1800 && cell % maze.data.width > 20;
            }
            require(farthest > maze.data.tileSize * 4.5f, "Patrol travels beyond the former three-tile neighborhood");
            require(crossedOpenedNet, "Opening a net refreshes reachable patrol destinations, final position " + mover.transform.position);
        }

        private static void require(bool value, string message) //검증 실패를 원인과 함께 전달
        {
            if (!value) throw new InvalidOperationException("PREDATOR_ENCOUNTER_TEST_FAILED: " + message);
            checks++;
        }
    }
}
