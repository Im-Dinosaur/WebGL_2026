using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class PredatorAIComponent : MonoBehaviour
    {
        [SerializeField, Min(.05f)] private float noticeDuration = .9f; //추격 전 연속 발견 시간
        [SerializeField] private float sightRange = 3.5f; //포식자 시야 거리
        [SerializeField] private float sightAngle = 86f; //포식자 시야 각도
        private Vector2 returnPosition; //발견 직전 순찰 위치
        private BaitData baitTarget; //현재 유인 목표
        private float baitCheckTime; //미끼 경로 검색의 남은 대기 시간
        private float effectiveRange = 3.5f; //이번 종류에 적용한 시야 거리
        private float effectiveNoticeTime = .9f; //이번 종류에 적용한 발견 시간
        public PredatorState state { get; private set; } //현재 행동 상태
        public float noticeProgress { get; private set; } //연속 발견 누적 시간
        public Vector2 target { get; private set; } //이동할 행동 목표
        public float range => effectiveRange; //시야 표시와 판정에 공통으로 적용할 거리
        public float angle => sightAngle; //시야 표시 각도
        public float noticeFraction => noticeProgress / effectiveNoticeTime; //발견 표시의 완료 비율

        public void initialize(PredatorTypeProfile profile = null) //종류별 탐지 설정과 행동 상태 초기화
        {
            effectiveRange = profile != null ? profile.range : Mathf.Max(.1f, sightRange);
            effectiveNoticeTime = profile != null ? profile.noticeTime : Mathf.Max(.05f, noticeDuration);
            state = PredatorState.Patrol;
            noticeProgress = 0;
            baitTarget = null;
            baitCheckTime = 0;
        }

        public bool canSee(Maze maze, Vector2 position, Vector2 facing, Player player) //수풀과 지형 및 시야각을 적용한 발견 판정
        {
            Vector2 delta = player.position - position; //플레이어 방향
            return !player.safe && !player.hidden && delta.magnitude <= effectiveRange && Vector2.Angle(facing, delta) <= sightAngle * .5f && maze.hasSight(position, player.position);
        }

        public BaitData findBait(Maze maze, Vector2 position, float deltaTime) //유인 범위와 이동 경로가 유효한 정지 미끼 선택
        {
            bool retained = false; //기존 미끼가 아직 남아 있는지 여부
            foreach (BaitData bait in maze.baits)
                if (bait == baitTarget && bait.settled) retained = true;
            if (retained) return baitTarget;
            if (baitTarget != null) baitCheckTime = 0;
            baitTarget = null;
            baitCheckTime -= deltaTime;
            if (baitCheckTime > 0f) return null;
            baitCheckTime = .25f;
            float nearest = float.PositiveInfinity; //가장 가까운 유인 미끼 거리
            foreach (BaitData bait in maze.baits)
            {
                if (!bait.settled) continue;
                float distance = Vector2.Distance(position, bait.position); //미끼까지의 거리
                if (distance > bait.attractDistance || distance >= nearest) continue;
                if (maze.data.findPath(maze.data.cellAt(position), maze.data.cellAt(bait.position), true).Count == 0) continue;
                baitTarget = bait;
                nearest = distance;
            }
            return baitTarget;
        }

        public void advance(bool seesPlayer, Vector2 position, Vector2 playerPosition, Vector2 patrolTarget, float deltaTime, BaitData bait = null) //발견과 유인 및 추격과 복귀 전환
        {
            if (seesPlayer)
            {
                if (state == PredatorState.Patrol || state == PredatorState.Return || state == PredatorState.Lure)
                {
                    if (state == PredatorState.Patrol) returnPosition = position;
                    state = PredatorState.Notice;
                    noticeProgress = 0;
                }
                if (state == PredatorState.Notice)
                {
                    noticeProgress += deltaTime;
                    if (noticeProgress >= effectiveNoticeTime) state = PredatorState.Chase;
                }
                target = playerPosition;
                return;
            }
            if (state == PredatorState.Notice || state == PredatorState.Chase || (state == PredatorState.Lure && bait == null))
            {
                state = PredatorState.Return;
                noticeProgress = 0;
            }
            if (bait != null)
            {
                if (state == PredatorState.Patrol) returnPosition = position;
                state = PredatorState.Lure;
                noticeProgress = 0;
                target = bait.position;
                return;
            }
            if (state == PredatorState.Return && Vector2.Distance(position, returnPosition) < .12f) state = PredatorState.Patrol;
            target = state == PredatorState.Return ? returnPosition : patrolTarget;
        }
    }
}
