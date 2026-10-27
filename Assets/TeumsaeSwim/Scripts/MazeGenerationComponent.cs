using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class MazeGenerationComponent : MonoBehaviour
    {
        [SerializeField, Min(1.5f)] private float tileSize = 2.2f; //지형 한 칸의 크기
        [SerializeField, Range(17, 61)] private int practiceSize = 25; //연습 미로의 한 변 칸 수
        [SerializeField, Range(17, 61)] private int normalSize = 31; //기본 미로의 한 변 칸 수
        [SerializeField, Range(17, 61)] private int challengeSize = 37; //도전 미로의 한 변 칸 수
        [SerializeField, Range(0f, .15f)] private float loopRatio = .045f; //격자 방 수에 대한 추가 우회로 비율
        [SerializeField, Min(1)] private int practicePredators = 3; //연습 포식자 수
        [SerializeField, Min(1)] private int normalPredators = 12; //기본 포식자 수
        [SerializeField, Min(1)] private int challengePredators = 18; //도전 포식자 수
        [SerializeField, Range(2f, 6f)] private float predatorSpacing = 3.5f; //포식자 시작 위치 사이의 최소 간격 칸 수
        [SerializeField, Range(.5f, 1f)] private float mainRoutePredatorRatio = .7f; //출구 경로에 우선 배치할 포식자 비율

        public int predatorCountFor(SwimDifficulty difficulty) //생성과 메뉴에서 공유하는 난이도별 포식자 수
        {
            return difficulty == SwimDifficulty.Practice ? practicePredators : difficulty == SwimDifficulty.Normal ? normalPredators : challengePredators;
        }

        public MazeData generate(int seed, SwimDifficulty difficulty) //전체 통로와 양쪽 안전구역을 무작위 생성
        {
            for (int attempt = 0; attempt < 32; attempt++)
            {
                MazeData data = build(unchecked(seed + attempt * 104729), difficulty); //같은 시드로 재현할 생성 후보
                if (data != null && validate(data)) return data;
            }
            throw new InvalidOperationException("안전구역과 출구가 연결된 미로를 만들지 못했습니다.");
        }

        public bool validate(MazeData data) //출구 연결과 안전구역 및 포식자 배치 검사
        {
            data.route.Clear();
            data.route.AddRange(data.findPath(data.start, data.exit));
            if (data.route.Count < data.width * 1.4f || !data.cells[data.start].safeZone || !data.cells[data.exit].safeZone) return false;
            int[] distances = data.distancesFrom(data.start); //분리된 물길을 확인할 거리
            for (int id = 0; id < data.cells.Length; id++)
            {
                if (data.passable(id, false) && distances[id] < 0) return false;
                if (data.cells[id].safeZone && (data.cells[id].kind != TerrainKind.Water || data.passable(id, true))) return false;
            }
            foreach (int home in data.homes)
                if (!data.passable(home, true) || nearEndpoint(data, home, 6f)) return false;
            data.exitDistances = data.distancesFrom(data.exit);
            return true;
        }

        private MazeData build(int seed, SwimDifficulty difficulty) //통로 생성과 양쪽 안전방 및 장애물 배치를 순서대로 구성
        {
            var random = new System.Random(seed); //재현 가능한 난수
            int size = difficulty == SwimDifficulty.Practice ? practiceSize : difficulty == SwimDifficulty.Normal ? normalSize : challengeSize; //선택한 미로 크기
            size = Mathf.Clamp(size, 17, 61) | 1;
            var data = new MazeData { width = size, height = size, tileSize = tileSize, seed = seed }; //정사각형 미로 데이터
            data.cells = new MazeCell[size * size];
            for (int id = 0; id < data.cells.Length; id++) data.cells[id] = new MazeCell();
            carveMaze(data, random);
            openLoops(data, random);
            if (!placeEndpoints(data, random)) return null;
            int featureCount = difficulty == SwimDifficulty.Practice ? 4 : difficulty == SwimDifficulty.Normal ? 6 : 8; //틈과 그물의 합계
            if (!placeFeatures(data, random, featureCount)) return null;
            int predators = predatorCountFor(difficulty); //포식자 배치 수
            if (!placePredators(data, random, Mathf.Max(1, predators))) return null;
            placeGrass(data, random);
            return data;
        }

        private void carveMaze(MazeData data, System.Random random) //무작위 깊이 우선 탐색으로 모든 방을 연결
        {
            var stack = new Stack<int>(); //아직 분기를 탐색할 방
            var candidates = new List<int>(4); //방문하지 않은 이웃 방
            int rooms = (data.width - 1) / 2; //한 변의 방 수
            int start = (random.Next(rooms) * 2 + 1) * data.width + random.Next(rooms) * 2 + 1; //미로를 파기 시작할 무작위 방
            data.cells[start].kind = TerrainKind.Water;
            stack.Push(start);
            while (stack.Count > 0)
            {
                int current = stack.Peek(); //현재 탐색할 방
                candidates.Clear();
                int x = current % data.width; //현재 열
                int y = current / data.width; //현재 행
                if (x > 2 && data.cells[current - 2].kind == TerrainKind.Wall) candidates.Add(current - 2);
                if (x < data.width - 3 && data.cells[current + 2].kind == TerrainKind.Wall) candidates.Add(current + 2);
                if (y > 2 && data.cells[current - data.width * 2].kind == TerrainKind.Wall) candidates.Add(current - data.width * 2);
                if (y < data.height - 3 && data.cells[current + data.width * 2].kind == TerrainKind.Wall) candidates.Add(current + data.width * 2);
                if (candidates.Count == 0) { stack.Pop(); continue; }
                int next = candidates[random.Next(candidates.Count)]; //다음으로 확장할 방
                data.cells[(current + next) / 2].kind = TerrainKind.Water;
                data.cells[next].kind = TerrainKind.Water;
                stack.Push(next);
            }
        }

        private void openLoops(MazeData data, System.Random random) //일부 통로 사이의 벽을 열어 순환하는 우회로 생성
        {
            var walls = new List<int>(); //추가 연결 후보 벽
            for (int id = 0; id < data.cells.Length; id++)
                if (data.cells[id].kind == TerrainKind.Wall && straightCorridor(data, id, out _)) walls.Add(id);
            shuffle(walls, random);
            int remaining = Mathf.RoundToInt((data.width - 1) * (data.height - 1) * .25f * loopRatio); //추가로 열 벽 수
            foreach (int id in walls)
            {
                if (remaining <= 0) break;
                if (!straightCorridor(data, id, out _)) continue;
                data.cells[id].kind = TerrainKind.Water;
                remaining--;
            }
        }

        private bool placeEndpoints(MazeData data, System.Random random) //서로 멀리 떨어진 무작위 시작과 도착 방 선택
        {
            var candidates = new List<int>(); //외벽을 보존하며 안전방을 만들 수 있는 중심
            for (int y = 3; y < data.height - 3; y += 2)
            for (int x = 3; x < data.width - 3; x += 2) candidates.Add(y * data.width + x);
            data.start = candidates[random.Next(candidates.Count)];
            carveSafeRoom(data, data.start);
            int[] distances = data.distancesFrom(data.start); //실제 통로를 따라 이동할 거리
            int farthest = 0; //후보 중 가장 먼 거리
            foreach (int id in candidates) farthest = Mathf.Max(farthest, distances[id]);
            candidates.RemoveAll(id => distances[id] < farthest * .75f || Vector2.Distance(data.center(id), data.center(data.start)) < data.width * data.tileSize * .4f);
            if (candidates.Count == 0) return false;
            data.exit = candidates[random.Next(candidates.Count)];
            carveSafeRoom(data, data.exit);
            data.route.AddRange(data.findPath(data.start, data.exit));
            return data.route.Count >= data.width * 1.4f;
        }

        private void carveSafeRoom(MazeData data, int center) //시작과 도착에 포식자가 들어올 수 없는 넓은 물길 확보
        {
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
            {
                MazeCell cell = data.cells[center + y * data.width + x]; //안전구역의 칸
                cell.kind = TerrainKind.Water;
                cell.safeZone = true;
            }
        }

        private bool placeFeatures(MazeData data, System.Random random, int count) //직선 통로에만 틈과 그물을 분산 배치
        {
            var candidates = new List<int>(data.route); //출구 경로에서 먼저 고를 후보
            var placed = new List<int>(); //간격을 유지할 배치 위치
            shuffle(candidates, random);
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (int id in candidates)
                {
                    int limit = pass == 0 ? 2 : count; //첫 단계는 주 경로에 두 종류 배치
                    if (placed.Count >= limit) break;
                    if (data.cells[id].kind != TerrainKind.Water || data.cells[id].safeZone || nearEndpoint(data, id, 4f) || !straightCorridor(data, id, out bool horizontal)) continue;
                    if (placed.Exists(other => Vector2.Distance(data.center(id), data.center(other)) < data.tileSize * 3f)) continue;
                    data.cells[id].kind = placed.Count % 2 == 0 ? TerrainKind.Gap : TerrainKind.Net;
                    data.cells[id].horizontal = horizontal;
                    placed.Add(id);
                }
                if (pass == 0 && placed.Count < 2) return false;
                candidates.Clear();
                for (int id = 0; id < data.cells.Length; id++) candidates.Add(id);
                shuffle(candidates, random);
            }
            return placed.Count == count;
        }

        private bool placePredators(MazeData data, System.Random random, int count) //주 경로의 앞과 중간 및 뒤에 포식자를 나누고 분기까지 배치
        {
            var routeCandidates = new List<int>(); //안전구역 여유를 제외한 순서 있는 출구 경로
            foreach (int id in data.route)
                if (data.passable(id, true) && !nearEndpoint(data, id, 6f)) routeCandidates.Add(id);
            if (routeCandidates.Count < Mathf.Min(3, count)) return false;
            var sections = new[] { new List<int>(), new List<int>(), new List<int>() }; //실제로 배치 가능한 경로의 세 구간
            for (int i = 0; i < routeCandidates.Count; i++) sections[Mathf.Min(2, i * 3 / routeCandidates.Count)].Add(routeCandidates[i]);
            foreach (List<int> section in sections) shuffle(section, random);
            for (int i = 0; i < Mathf.Min(3, count); i++)
                if (!tryPlacePredator(data, sections[i])) return false;
            int routeTarget = Mathf.Min(count, Mathf.Max(3, Mathf.CeilToInt(count * mainRoutePredatorRatio))); //주 경로 우선 할당량
            while (data.homes.Count < routeTarget)
            {
                bool placed = false; //한 구간에 몰리지 않도록 돌아가며 배치
                foreach (List<int> section in sections)
                {
                    if (data.homes.Count >= routeTarget) break;
                    placed |= tryPlacePredator(data, section);
                }
                if (!placed) break;
            }
            if (data.homes.Count < Mathf.CeilToInt(count * .5f)) return false;
            var routeCells = new HashSet<int>(data.route); //분기 후보를 빠르게 구분할 경로 집합
            var branches = new List<int>(); //나머지 탐색 구역의 후보
            for (int id = 0; id < data.cells.Length; id++)
                if (!routeCells.Contains(id) && data.passable(id, true) && !nearEndpoint(data, id, 6f)) branches.Add(id);
            shuffle(branches, random);
            while (data.homes.Count < count && tryPlacePredator(data, branches)) { }
            foreach (List<int> section in sections)
                while (data.homes.Count < count && tryPlacePredator(data, section)) { }
            return data.homes.Count == count;
        }

        private bool tryPlacePredator(MazeData data, List<int> candidates) //간격과 실제 순찰 공간을 확보한 후보 하나를 사용
        {
            while (candidates.Count > 0)
            {
                int last = candidates.Count - 1; //한 번 검사한 후보는 다시 검색하지 않음
                int id = candidates[last]; //섞인 순서의 다음 후보
                candidates.RemoveAt(last);
                if (data.homes.Exists(home => Vector2.Distance(data.center(home), data.center(id)) < data.tileSize * predatorSpacing)) continue;
                int[] distances = data.distancesFrom(id, true); //닫힌 장애물을 반영한 순찰 가능 범위
                int patrolCells = 0; //충분히 움직일 수 있는 주변 칸 수
                foreach (int distance in distances)
                    if (distance >= 0 && distance <= 8 && ++patrolCells >= 7) break;
                if (patrolCells < 7) continue;
                data.homes.Add(id);
                return true;
            }
            return false;
        }

        private void placeGrass(MazeData data, System.Random random) //주 경로와 분기에 은신처 분산 배치
        {
            var candidates = new List<int>(data.route); //주 경로에 먼저 확보할 은신처
            var others = new List<int>(); //분기 통로의 후보
            for (int id = 0; id < data.cells.Length; id++) if (!data.route.Contains(id)) others.Add(id);
            shuffle(others, random);
            candidates.AddRange(others);
            var grass = new List<int>(); //이미 배치한 수풀
            int count = data.width / 2; //미로 크기에 따른 은신처 수
            foreach (int id in candidates)
            {
                if (grass.Count >= count) break;
                if (data.cells[id].kind != TerrainKind.Water || data.cells[id].safeZone || data.homes.Contains(id)) continue;
                if (grass.Exists(other => Vector2.Distance(data.center(other), data.center(id)) < data.tileSize * 3f)) continue;
                data.cells[id].kind = TerrainKind.Grass;
                grass.Add(id);
            }
        }

        private bool straightCorridor(MazeData data, int id, out bool horizontal) //양옆은 벽이고 마주 보는 두 칸만 열린 통로 검사
        {
            horizontal = false;
            int x = id % data.width; //후보 열
            int y = id / data.width; //후보 행
            if (x <= 0 || y <= 0 || x >= data.width - 1 || y >= data.height - 1) return false;
            bool left = data.passable(id - 1, false); //왼쪽 물길
            bool right = data.passable(id + 1, false); //오른쪽 물길
            bool down = data.passable(id - data.width, false); //아래 물길
            bool up = data.passable(id + data.width, false); //위 물길
            horizontal = left && right && !down && !up;
            return horizontal || (down && up && !left && !right);
        }

        private bool nearEndpoint(MazeData data, int id, float tiles) //시작과 도착의 여유 거리 검사
        {
            return Vector2.Distance(data.center(id), data.center(data.start)) < tiles * data.tileSize || Vector2.Distance(data.center(id), data.center(data.exit)) < tiles * data.tileSize;
        }

        private void shuffle(List<int> items, System.Random random) //시드를 유지하는 후보 순서 섞기
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int other = random.Next(i + 1); //맞바꿀 후보
                (items[i], items[other]) = (items[other], items[i]);
            }
        }
    }
}
