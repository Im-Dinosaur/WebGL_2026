using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeumsaeSwim
{
    public enum SwimPhase { Home, Playing, Paused, Won, Lost }
    public enum SwimDifficulty { Practice, Normal, Challenge }
    public enum TerrainKind { Water, Wall, Gap, Net, Grass }
    public enum PredatorState { Patrol, Notice, Chase, Return }
    public enum SwimCue { Bait, Squeeze, Tear, Open, Notice, Chase, Win, Lose }

    [Serializable]
    public sealed class MazeCell
    {
        public TerrainKind kind = TerrainKind.Wall; //칸의 지형
        public bool horizontal; //통로의 가로 방향 여부
        public float netProgress; //그물의 누적 작업 시간
        public bool netOpen; //그물 개방 여부
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
                    if (previous[next] >= 0 || !passable(next, predator)) continue;
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
            return !predator || (cells[id].kind != TerrainKind.Gap && (cells[id].kind != TerrainKind.Net || cells[id].netOpen));
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
        public float remaining; //미끼의 남은 시간
        public Transform visual; //미끼 표시 오브젝트
    }
}
