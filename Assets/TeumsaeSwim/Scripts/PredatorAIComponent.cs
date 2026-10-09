using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class PredatorAIComponent : MonoBehaviour
    {
        [SerializeField] private float noticeDuration = 1f; //추격 전 연속 발견 시간
        [SerializeField] private float sightRange = 3.5f; //포식자 시야 거리
        [SerializeField] private float sightAngle = 86f; //포식자 시야 각도
        private Vector2 returnPosition; //발견 직전 순찰 위치
        public PredatorState state { get; private set; } //현재 행동 상태
        public float noticeProgress { get; private set; } //연속 발견 누적 시간
        public Vector2 target { get; private set; } //이동할 행동 목표
        public float range => sightRange; //시야 표시 거리
        public float angle => sightAngle; //시야 표시 각도

        public void initialize() //행동 상태 초기화
        {
            state = PredatorState.Patrol;
            noticeProgress = 0;
        }

        public bool canSee(Maze maze, Vector2 position, Vector2 facing, Player player) //수풀과 지형 및 시야각을 적용한 발견 판정
        {
            Vector2 delta = player.position - position; //플레이어 방향
            return !player.hidden && delta.magnitude <= sightRange && Vector2.Angle(facing, delta) <= sightAngle * .5f && maze.hasSight(position, player.position);
        }

        public void advance(bool seesPlayer, Vector2 position, Vector2 playerPosition, Vector2 patrolTarget, float deltaTime) //순찰과 발견 및 추격과 복귀 전환
        {
            if (seesPlayer)
            {
                if (state == PredatorState.Patrol || state == PredatorState.Return)
                {
                    if (state == PredatorState.Patrol) returnPosition = position;
                    state = PredatorState.Notice;
                    noticeProgress = 0;
                }
                if (state == PredatorState.Notice)
                {
                    noticeProgress += deltaTime;
                    if (noticeProgress >= noticeDuration) state = PredatorState.Chase;
                }
                target = playerPosition;
                return;
            }
            if (state == PredatorState.Notice || state == PredatorState.Chase)
            {
                state = PredatorState.Return;
                noticeProgress = 0;
            }
            if (state == PredatorState.Return && Vector2.Distance(position, returnPosition) < .12f) state = PredatorState.Patrol;
            target = state == PredatorState.Return ? returnPosition : patrolTarget;
        }
    }
}
