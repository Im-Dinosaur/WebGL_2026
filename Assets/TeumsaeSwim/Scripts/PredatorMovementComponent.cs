using System.Collections.Generic;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class PredatorMovementComponent : MonoBehaviour
    {
        [SerializeField] private float bodyRadius = .43f; //포식자의 충돌 반경
        [SerializeField] private float patrolSpeed = 1.22f; //기본 순찰 속도
        [SerializeField] private float chaseSpeed = 3.7f; //기본 추격 속도
        [SerializeField] private float lureSpeed = 2.1f; //미끼를 향해 이동하는 속도
        [SerializeField, Min(3)] private int patrolRange = 12; //시작 위치에서 실제 통로를 따라 순찰할 최대 칸 수
        [SerializeField, Range(0f, 1f)] private float mainRoutePreference = .7f; //출구 경로를 순찰 목적지로 우선 선택할 확률
        [SerializeField, Min(0f)] private float patrolPause = .35f; //목적지 도착 후 잠시 머무는 시간
        private Maze maze; //지형 진입점
        private List<int> path = new List<int>(); //현재 이동 경로
        private int pathIndex; //다음 경유지
        private int goalCell = -1; //경로의 목표 칸
        private float repathTime; //경로 재검사 간격
        private int home; //순찰 구역의 기준 칸
        private int patrolGoal; //현재 순찰 목적지
        private float patrolWait; //목적지 도착 후 대기 시간
        private float difficultyScale; //난이도별 이동 배율
        private PredatorTypeProfile profile; //이번 포식자의 행동별 속도 설정
        private System.Random random; //순찰 선택 난수
        private readonly HashSet<int> mainRoute = new HashSet<int>(); //플레이어와 교차할 출구 경로
        private readonly List<int> patrolCandidates = new List<int>(); //통로로 도달 가능한 순찰 후보
        private readonly List<int> routeCandidates = new List<int>(); //주 경로 위의 순찰 후보
        public Vector2 facing { get; private set; } = Vector2.left; //바라보는 방향
        public float radius => bodyRadius; //접촉 판정 반경

        public void initialize(Maze world, int homeCell, SwimDifficulty difficulty, int seed, PredatorTypeProfile typeProfile = null) //종류와 난이도를 반영하며 순찰 위치 초기화
        {
            profile = typeProfile;
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
            repathTime = 0;
            patrolWait = patrolPause;
            mainRoute.Clear();
            foreach (int id in maze.data.route) mainRoute.Add(id);
        }

        public Vector2 patrolTarget(float deltaTime) //인근 물길 안에서 순찰 목적지 선택
        {
            if (Vector2.Distance(transform.position, maze.data.center(patrolGoal)) < .13f)
            {
                patrolWait -= deltaTime;
                if (patrolWait <= 0)
                {
                    patrolCandidates.Clear();
                    routeCandidates.Clear();
                    int[] homeDistances = maze.data.distancesFrom(home, true); //안전구역과 닫힌 장애물을 피한 순찰 구역
                    int[] currentDistances = maze.data.distancesFrom(maze.data.cellAt(transform.position), true); //후보마다 길찾기하지 않고 한 번에 연결 확인
                    for (int id = 0; id < maze.data.cells.Length; id++)
                    {
                        if (homeDistances[id] < 0 || homeDistances[id] > patrolRange || currentDistances[id] < 2 || currentDistances[id] > patrolRange) continue;
                        patrolCandidates.Add(id);
                        if (mainRoute.Contains(id)) routeCandidates.Add(id);
                    }
                    List<int> candidates = routeCandidates.Count > 0 && random.NextDouble() < mainRoutePreference ? routeCandidates : patrolCandidates; //주 경로도 자주 지나가는 순찰
                    if (candidates.Count > 0) patrolGoal = candidates[random.Next(candidates.Count)];
                    patrolWait = patrolPause;
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
            float speed = (state == PredatorState.Chase ? chaseSpeed : state == PredatorState.Lure ? lureSpeed : patrolSpeed) * difficultyScale * (profile != null ? profile.speedScale(state) : 1f); //종류와 난이도를 각각 한 번 적용한 행동 속도
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
