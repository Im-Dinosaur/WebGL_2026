using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieCqb
{
    public sealed class IndoorNavigationComponent : MonoBehaviour
    {
        [SerializeField] private string[] rows; //아래에서 위로 배치한 맵 셀
        [SerializeField] private DoorComponent[] doors; //셀을 막는 문 목록

        private DoorComponent[] doorByCell; //셀에 대응하는 문
        private int[] parents; //경로 탐색에 사용하는 이전 셀
        private int[] frontier; //경로 탐색 대기열
        private static readonly Vector2Int[] directions = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down }; //인접 셀 순서

        public int width => rows[0].Length; //가로 셀 수
        public int height => rows.Length; //세로 셀 수

        public void initialize() //문 조회와 경로 탐색 공간 준비
        {
            int cellCount = width * height; //맵 전체 셀 수
            doorByCell = new DoorComponent[cellCount];
            parents = new int[cellCount];
            frontier = new int[cellCount];

            foreach (DoorComponent door in doors) //맵에 배치된 문
            {
                door.resetDoor();
                doorByCell[indexOf(cellAt(door.transform.position))] = door;
            }
        }

        public bool isOpenCell(Vector2Int cell) //벽, 가구, 닫힌 문 통과 여부
        {
            if (cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height)
                return false;

            char tile = rows[cell.y][cell.x]; //셀의 지형 종류
            DoorComponent door = doorByCell[indexOf(cell)]; //해당 셀의 문
            return tile != '#' && tile != 'B' && (door == null || door.isOpen);
        }

        public bool hasSight(Vector2 start, Vector2 end) //벽과 문에 막히지 않는 직선 시야 확인
        {
            Vector2Int cell = cellAt(start); //현재 확인할 셀
            Vector2Int goal = cellAt(end); //시야의 끝 셀
            Vector2 delta = end - start; //시야의 방향과 길이
            int stepX = Math.Sign(delta.x); //가로 셀 이동 방향
            int stepY = Math.Sign(delta.y); //세로 셀 이동 방향
            float strideX = stepX == 0 ? float.PositiveInfinity : 1f / Mathf.Abs(delta.x); //가로 경계 사이의 비율
            float strideY = stepY == 0 ? float.PositiveInfinity : 1f / Mathf.Abs(delta.y); //세로 경계 사이의 비율
            float edgeX = stepX == 0 ? float.PositiveInfinity : ((stepX > 0 ? cell.x + 1 : cell.x) - start.x) / delta.x; //첫 가로 경계 비율
            float edgeY = stepY == 0 ? float.PositiveInfinity : ((stepY > 0 ? cell.y + 1 : cell.y) - start.y) / delta.y; //첫 세로 경계 비율

            while (isOpenCell(cell))
            {
                if (cell == goal)
                    return true;

                if (Mathf.Abs(edgeX - edgeY) < 0.00001f)
                {
                    if (!isOpenCell(cell + new Vector2Int(stepX, 0)) || !isOpenCell(cell + new Vector2Int(0, stepY)))
                        return false;
                    cell += new Vector2Int(stepX, stepY);
                    edgeX += strideX;
                    edgeY += strideY;
                }
                else if (edgeX < edgeY)
                {
                    cell.x += stepX;
                    edgeX += strideX;
                }
                else
                {
                    cell.y += stepY;
                    edgeY += strideY;
                }
            }

            return false;
        }

        public Vector2 move(Vector2 position, Vector2 delta, float radius) //벽을 통과하지 않는 이동 계산
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / 0.16f)); //빠른 이동의 충돌 검사 횟수
            Vector2 step = delta / steps; //한 번에 이동할 거리
            for (int i = 0; i < steps; i++) //나누어 검사할 이동 횟수
            {
                Vector2 horizontal = position + new Vector2(step.x, 0f); //가로 이동 후보
                if (fits(horizontal, radius))
                    position.x = horizontal.x;

                Vector2 vertical = position + new Vector2(0f, step.y); //세로 이동 후보
                if (fits(vertical, radius))
                    position.y = vertical.y;
            }

            return position;
        }

        public void findRoute(Vector2 from, Vector2 target, bool retreat, List<Vector2> route) //접근 또는 도주 경로 계산
        {
            route.Clear();
            Vector2Int first = cellAt(from); //출발 셀
            if (!isOpenCell(first))
                return;

            Array.Fill(parents, -1);
            int start = indexOf(first); //출발 셀 번호
            int read = 0; //다음 탐색 위치
            int write = 1; //다음 삽입 위치
            int best = start; //목적에 가장 가까운 도달 가능 셀
            float bestScore = float.NegativeInfinity; //선택한 셀의 점수
            Vector2Int targetCell = cellAt(target); //목표 셀
            frontier[0] = start;
            parents[start] = start;

            while (read < write)
            {
                int current = frontier[read++]; //현재 탐색 셀 번호
                Vector2Int cell = new Vector2Int(current % width, current / width); //현재 탐색 셀
                Vector2 center = cellCenter(cell); //현재 셀의 중심 위치
                float targetDistance = (center - target).sqrMagnitude; //목표와의 거리 제곱
                float score = retreat ? targetDistance - 0.45f * (center - from).sqrMagnitude : -targetDistance; //도주 또는 접근 우선순위
                if (score > bestScore)
                {
                    best = current;
                    bestScore = score;
                }

                if (!retreat && cell == targetCell)
                    break;

                foreach (Vector2Int direction in directions) //다음에 검사할 인접 방향
                {
                    Vector2Int next = cell + direction; //인접 셀
                    if (!isOpenCell(next))
                        continue;

                    int index = indexOf(next); //인접 셀 번호
                    if (parents[index] >= 0)
                        continue;

                    parents[index] = current;
                    frontier[write++] = index;
                }
            }

            for (int current = best; current != start; current = parents[current]) //목표에서 출발지로 경로 복원
                route.Add(cellCenter(new Vector2Int(current % width, current / width)));
            route.Reverse();

            if (!retreat && best == indexOf(targetCell) && isOpenCell(targetCell))
                route.Add(target);
        }

        public Vector2 followRoute(Vector2 position, List<Vector2> route, ref int waypoint, float distance, float radius) //현재 경로를 따라 다음 위치 계산
        {
            while (waypoint < route.Count && (route[waypoint] - position).sqrMagnitude < 0.0225f)
                waypoint++;

            if (waypoint >= route.Count)
                return position;

            Vector2 next = Vector2.MoveTowards(position, route[waypoint], distance); //현재 경유점으로 이동할 위치
            return move(position, next - position, radius);
        }

        public static Vector2Int cellAt(Vector2 position) //월드 위치가 속한 셀
        {
            return new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
        }

        public static Vector2 cellCenter(Vector2Int cell) //셀의 월드 중심 위치
        {
            return new Vector2(cell.x + 0.5f, cell.y + 0.5f);
        }

        private int indexOf(Vector2Int cell) //셀의 일차원 인덱스 계산
        {
            return cell.y * width + cell.x;
        }

        private bool fits(Vector2 position, float radius) //몸체가 막힌 셀과 겹치는지 검사
        {
            return isOpenCell(cellAt(position + new Vector2(-radius, -radius)))
                && isOpenCell(cellAt(position + new Vector2(-radius, radius)))
                && isOpenCell(cellAt(position + new Vector2(radius, -radius)))
                && isOpenCell(cellAt(position + new Vector2(radius, radius)));
        }
    }
}
