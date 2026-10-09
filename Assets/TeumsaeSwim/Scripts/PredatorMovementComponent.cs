using System.Collections.Generic;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class PredatorMovementComponent : MonoBehaviour
    {
        [SerializeField] private float bodyRadius = .43f; //포식자의 충돌 반경
        [SerializeField] private float patrolSpeed = 1.22f; //기본 순찰 속도
        [SerializeField] private float chaseSpeed = 3.7f; //기본 추격 속도
        private Maze maze; //지형 진입점
        private List<int> path = new List<int>(); //현재 이동 경로
        private int pathIndex; //다음 경유지
        private int goalCell = -1; //경로의 목표 칸
        private float repathTime; //경로 재검사 간격
        private int home; //순찰 구역의 기준 칸
        private int patrolGoal; //현재 순찰 목적지
        private float patrolWait; //목적지 도착 후 대기 시간
        private float difficultyScale; //난이도별 이동 배율
        private System.Random random; //순찰 선택 난수
        public Vector2 facing { get; private set; } = Vector2.left; //바라보는 방향
        public float radius => bodyRadius; //접촉 판정 반경

        public void initialize(Maze world, int homeCell, SwimDifficulty difficulty, int seed) //순찰 구역과 위치 초기화
        {
            maze = world;
            home = homeCell;
            patrolGoal = home;
            transform.position = maze.data.center(home);
            random = new System.Random(seed);
            difficultyScale = difficulty == SwimDifficulty.Practice ? .8f : difficulty == SwimDifficulty.Challenge ? 1.06f : 1f;
            facing = homeCell % maze.data.width > 5 ? Vector2.left : Vector2.right;
            path.Clear();
            pathIndex = 0;
            goalCell = -1;
            patrolWait = .8f;
        }

        public Vector2 patrolTarget(float deltaTime) //인근 물길 안에서 순찰 목적지 선택
        {
            if (Vector2.Distance(transform.position, maze.data.center(patrolGoal)) < .13f)
            {
                patrolWait -= deltaTime;
                if (patrolWait <= 0)
                {
                    var candidates = new List<int>(); //도달 가능한 주변 순찰 칸
                    for (int id = 0; id < maze.data.cells.Length; id++)
                    {
                        if (!maze.data.passable(id, true) || Vector2.Distance(maze.data.center(id), maze.data.center(home)) > maze.data.tileSize * 3.1f) continue;
                        List<int> route = maze.data.findPath(maze.data.cellAt(transform.position), id, true); //순찰 후보 경로
                        if (route.Count >= 3 && route.Count <= 9) candidates.Add(id);
                    }
                    if (candidates.Count > 0) patrolGoal = candidates[random.Next(candidates.Count)];
                    patrolWait = .8f;
                }
            }
            return maze.data.center(patrolGoal);
        }

        public void tick(Vector2 target, PredatorState state, float deltaTime) //길찾기와 행동별 이동 실행
        {
            Vector2 position = transform.position; //현재 위치
            if (state == PredatorState.Notice)
            {
                turn(target - position, deltaTime);
                return;
            }
            int targetCell = maze.data.cellAt(target); //목표 칸
            repathTime -= deltaTime;
            if (targetCell != goalCell || repathTime <= 0)
            {
                int currentCell = maze.data.cellAt(position); //현재 칸
                path = maze.data.findPath(currentCell, targetCell, true);
                pathIndex = 0;
                if (path.Count > 1 && clearSegment(position, maze.data.center(path[1]))) pathIndex = 1;
                goalCell = targetCell;
                repathTime = .45f;
            }
            if (path.Count == 0) { turn(target - position, deltaTime); return; }
            while (pathIndex < path.Count && Vector2.Distance(position, maze.data.center(path[pathIndex])) < .08f) pathIndex++;
            Vector2 waypoint = pathIndex < path.Count ? maze.data.center(path[pathIndex]) : target; //다음 경유 위치
            Vector2 delta = waypoint - position; //경유지 방향
            if (delta.magnitude < .03f) return;
            turn(delta, deltaTime);
            float speed = (state == PredatorState.Chase ? chaseSpeed : patrolSpeed) * difficultyScale; //현재 행동 속도
            transform.position = maze.move(position, Vector2.ClampMagnitude(delta, speed * deltaTime), bodyRadius, true, false);
        }

        private bool clearSegment(Vector2 from, Vector2 to) //모서리를 침범하지 않고 다음 경유지로 갈 수 있는지 검사
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / .08f)); //선분 검사 수
            for (int i = 1; i <= steps; i++) if (!maze.canOccupy(Vector2.Lerp(from, to, (float)i / steps), bodyRadius, true)) return false;
            return true;
        }

        private void turn(Vector2 direction, float deltaTime) //시야의 급격한 반전을 줄이는 방향 회전
        {
            if (direction.sqrMagnitude < .0001f) return;
            float angle = Mathf.MoveTowardsAngle(Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, 240f * deltaTime); //이번 프레임 방향
            facing = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        }
    }
}
