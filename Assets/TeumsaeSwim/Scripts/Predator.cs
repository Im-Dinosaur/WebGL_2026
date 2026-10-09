using System;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class Predator : MonoBehaviour
    {
        [SerializeField] private PredatorAIComponent intelligence; //발견과 행동 판단 구성 요소
        [SerializeField] private PredatorMovementComponent movement; //순찰과 길찾기 구성 요소
        [SerializeField] private PredatorVisualComponent visual; //물고기와 시야 표시 구성 요소
        private Maze maze; //지형 진입점
        private Player player; //플레이어 진입점
        private BaitData baitTarget; //순찰 중 관심을 끈 미끼
        public PredatorState state => intelligence.state; //외부에 표시할 행동 상태
        public bool seesPlayer { get; private set; } //이번 프레임 발견 결과
        public Vector2 position => transform.position; //현재 위치
        public float radius => movement.radius; //접촉 반경
        public event Action<SwimCue> cue; //발견과 추격 효과음 요청

        public void initialize(Maze world, Player target, int home, SwimDifficulty difficulty, int seed) //포식자 구성 요소 연결
        {
            maze = world;
            player = target;
            intelligence.initialize();
            movement.initialize(maze, home, difficulty, seed);
            visual.initialize();
            visual.tick(maze, movement.facing, state, 0f, intelligence.range, intelligence.angle);
        }

        public void tick(float deltaTime) //감지와 판단 및 이동 순서 조율
        {
            PredatorState before = state; //직전 행동 상태
            seesPlayer = intelligence.canSee(maze, position, movement.facing, player);
            Vector2 patrolTarget = state == PredatorState.Patrol ? movement.patrolTarget(deltaTime) : position; //기본 순찰 목표
            if (state == PredatorState.Patrol)
            {
                baitTarget = null;
                float nearest = 5.5f; //미끼 관심 거리
                foreach (BaitData bait in maze.baits)
                {
                    float distance = Vector2.Distance(position, bait.position); //미끼 거리
                    if (distance >= nearest || !maze.hasSight(position, bait.position) || maze.data.findPath(maze.data.cellAt(position), maze.data.cellAt(bait.position), true).Count == 0) continue;
                    nearest = distance;
                    baitTarget = bait;
                }
                if (baitTarget != null) patrolTarget = baitTarget.position;
            }
            intelligence.advance(seesPlayer, position, player.position, patrolTarget, deltaTime);
            if (state != PredatorState.Patrol) baitTarget = null;
            movement.tick(intelligence.target, state, deltaTime);
            if (baitTarget != null && Vector2.Distance(position, baitTarget.position) < .35f) maze.consumeBait(baitTarget);
            if (state != before && state == PredatorState.Notice) cue?.Invoke(SwimCue.Notice);
            if (state != before && state == PredatorState.Chase) cue?.Invoke(SwimCue.Chase);
            visual.tick(maze, movement.facing, state, intelligence.noticeProgress, intelligence.range, intelligence.angle);
        }
    }
}
