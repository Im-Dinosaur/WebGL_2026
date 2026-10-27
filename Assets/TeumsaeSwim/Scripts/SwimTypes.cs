using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeumsaeSwim
{
    public enum SwimPhase { Home, Playing, Paused, Won, Lost }
    public enum SwimDifficulty { Practice, Normal, Challenge }
    public enum TerrainKind { Water, Wall, Gap, Net, Grass }
    public enum PredatorState { Patrol, Notice, Chase, Return, Lure }
    public enum PredatorKind { Balanced, Scout, Alert, Sprinter } //종류별 능력치와 그림의 식별자
    public enum SwimCue { Bait, Squeeze, Tear, Open, Notice, Chase, Win, Lose, Pickup, Heartbeat }

    [Serializable]
    public sealed class MazeCell
    {
        public TerrainKind kind = TerrainKind.Wall; //칸의 지형
        public bool horizontal; //통로의 가로 방향 여부
        public float netProgress; //그물의 누적 작업 시간
        public bool netOpen; //그물 개방 여부
        public bool safeZone; //포식자의 진입과 포획을 막는 안전구역
    }

    public sealed class MazeData
    {
        public int width; //가로 칸 수
        public int height; //세로 칸 수
        public int seed; //미로 생성 시드
        public float tileSize; //칸의 월드 크기
        public int start; //시작 칸
        public int exit; //출구 칸
        public MazeCell[] cells; //전체 지형 데이터
        public int[] exitDistances; //출구까지의 실제 통로 거리
        public readonly List<int> homes = new List<int>(); //포식자 시작 칸
        public readonly List<int> route = new List<int>(); //검증된 출구 경로

        public Vector2 center(int id) //칸의 중심 좌표
        {
            return new Vector2((id % width + .5f) * tileSize, (id / width + .5f) * tileSize);
        }

        public int cellAt(Vector2 position) //위치에 해당하는 칸
        {
            int x = Mathf.FloorToInt(position.x / tileSize); //가로 좌표
            int y = Mathf.FloorToInt(position.y / tileSize); //세로 좌표
            return x < 0 || y < 0 || x >= width || y >= height ? -1 : y * width + x;
        }

        public List<int> findPath(int from, int to, bool predator = false) //통행 가능한 칸의 최단 경로
        {
            var result = new List<int>(); //검색 결과
            if (!passable(from, predator) || !passable(to, predator)) return result;
            int[] previous = new int[cells.Length]; //이전 방문 칸
            Array.Fill(previous, -1);
            var queue = new Queue<int>(); //검색 대기 칸
            queue.Enqueue(from);
            previous[from] = from;
            while (queue.Count > 0)
            {
                int id = queue.Dequeue(); //현재 검색 칸
                if (id == to) break;
                foreach (int next in neighbors(id))
                {
                    if (previous[next] >= 0 || !passable(next, predator) || !gapAllows(id, next) || !gapAllows(next, id)) continue;
                    previous[next] = id;
                    queue.Enqueue(next);
                }
            }
            if (previous[to] < 0) return result;
            for (int id = to; id != from; id = previous[id]) result.Add(id);
            result.Add(from);
            result.Reverse();
            return result;
        }

        public bool passable(int id, bool predator) //길찾기의 통행 가능 여부
        {
            if (id < 0 || id >= cells.Length || cells[id].kind == TerrainKind.Wall) return false;
            return !predator || (!cells[id].safeZone && cells[id].kind != TerrainKind.Gap && (cells[id].kind != TerrainKind.Net || cells[id].netOpen));
        }

        public int[] distancesFrom(int origin, bool predator = false) //한 번의 탐색으로 모든 칸까지의 통로 거리 계산
        {
            int[] distances = new int[cells.Length]; //도달하지 못한 칸은 음수로 유지
            Array.Fill(distances, -1);
            if (!passable(origin, predator)) return distances;
            var queue = new Queue<int>(); //탐색할 칸
            queue.Enqueue(origin);
            distances[origin] = 0;
            while (queue.Count > 0)
            {
                int id = queue.Dequeue(); //현재 칸
                foreach (int next in neighbors(id))
                {
                    if (distances[next] >= 0 || !passable(next, predator) || !gapAllows(id, next) || !gapAllows(next, id)) continue;
                    distances[next] = distances[id] + 1;
                    queue.Enqueue(next);
                }
            }
            return distances;
        }

        public float progressAt(Vector2 position) //출구 방향과 관계없이 남은 통로 거리로 진행률 계산
        {
            int id = cellAt(position); //현재 위치의 칸
            if (id < 0 || exitDistances == null || exitDistances[id] < 0 || exitDistances[start] <= 0) return 0f;
            float remaining = exitDistances[id] + Vector2.Distance(position, center(id)) / tileSize; //현재 칸을 거쳐 출구까지의 거리
            foreach (int next in neighbors(id))
            {
                if (exitDistances[next] < 0 || !gapAllows(id, next) || !gapAllows(next, id)) continue;
                remaining = Mathf.Min(remaining, exitDistances[next] + Vector2.Distance(position, center(next)) / tileSize);
            }
            return Mathf.Clamp01(1f - remaining / exitDistances[start]);
        }

        private bool gapAllows(int from, int to) //틈의 실제 통로 방향과 일치하는 경로만 허용
        {
            if (cells[from].kind != TerrainKind.Gap) return true;
            return cells[from].horizontal ? from / width == to / width : from % width == to % width;
        }

        public IEnumerable<int> neighbors(int id) //상하좌우 인접 칸
        {
            if (id % width > 0) yield return id - 1;
            if (id % width < width - 1) yield return id + 1;
            if (id >= width) yield return id - width;
            if (id < cells.Length - width) yield return id + width;
        }
    }

    public sealed class BaitData
    {
        public Vector2 position; //미끼 위치
        public Vector2 velocity; //날아가는 미끼의 속도
        public float travelRemaining; //정지 전 남은 이동 거리
        public float attractDistance; //주변 포식자를 유인하는 거리
        public int bounceCount; //벽에 반사된 횟수
        public bool settled; //정지하여 유인 효과가 켜진 상태
        public float remaining; //미끼의 남은 시간
        public Transform visual; //미끼 표시 오브젝트
    }
}
