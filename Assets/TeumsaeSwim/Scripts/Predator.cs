using System;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class Predator : MonoBehaviour
    {
        [SerializeField] private PredatorAIComponent intelligence; //발견과 행동 판단 구성 요소
        [SerializeField] private PredatorMovementComponent movement; //순찰과 길찾기 구성 요소
        [SerializeField] private PredatorVisualComponent visual; //물고기와 발견 신호 표시 구성 요소
        private Maze maze; //지형 진입점
        private Player player; //플레이어 진입점
        public PredatorKind kind { get; private set; } //현재 포식자 종류
        public PredatorState state => intelligence.state; //외부에 표시할 행동 상태
        public bool seesPlayer { get; private set; } //이번 프레임 발견 결과
        public Vector2 position => transform.position; //현재 위치
        public float radius => movement.radius; //접촉 반경
        public event Action<SwimCue> cue; //발견과 추격 효과음 요청

        public void initialize(Maze world, Player target, int home, SwimDifficulty difficulty, int seed, PredatorTypeProfile profile = null) //종류 설정을 담당 구성 요소에 전달
        {
            maze = world;
            player = target;
            kind = profile != null ? profile.type : PredatorKind.Balanced;
            intelligence.initialize(profile);
            movement.initialize(maze, home, difficulty, seed, profile);
            visual.initialize(profile);
            visual.tick(movement.facing, state, 0f);
        }

        public void tick(float deltaTime) //감지와 판단 및 이동 순서 조율
        {
            PredatorState before = state; //직전 행동 상태
            seesPlayer = intelligence.canSee(maze, position, movement.facing, player);
            Vector2 patrolTarget = state == PredatorState.Patrol ? movement.patrolTarget(deltaTime) : position; //기본 순찰 목표
            BaitData baitTarget = seesPlayer ? null : intelligence.findBait(maze, position, deltaTime); //미끼 감지 결과
            intelligence.advance(seesPlayer, position, player.position, patrolTarget, deltaTime, baitTarget);
            movement.tick(intelligence.target, state, deltaTime);
            if (state == PredatorState.Lure && baitTarget != null && Vector2.Distance(position, baitTarget.position) < .65f) maze.consumeBait(baitTarget);
            if (state != before && (state == PredatorState.Notice || state == PredatorState.Lure)) cue?.Invoke(SwimCue.Notice);
            if (state != before && state == PredatorState.Chase) cue?.Invoke(SwimCue.Chase);
            visual.tick(movement.facing, state, intelligence.noticeFraction, deltaTime);
        }
    }
}
