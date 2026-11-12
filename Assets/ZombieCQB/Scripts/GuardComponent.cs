using System.Collections.Generic;
using UnityEngine;

namespace ZombieCqb
{
    public sealed class GuardComponent : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body; //경비의 외형
        [SerializeField] private SpriteRenderer attackFlash; //짧은 피격 방향 효과
        [SerializeField] private Vector2[] patrolPoints; //순찰 경유점
        [SerializeField, Min(0.1f)] private float sightRange = 6.8f; //탐지 거리
        [SerializeField, Min(0.1f)] private float detectionTime = 0.38f; //발견 확정까지 필요한 시야 시간
        [SerializeField, Min(0.1f)] private float patrolSpeed = 1.45f; //순찰 속도
        [SerializeField, Min(0.1f)] private float chaseSpeed = 3.35f; //추격 속도
        [SerializeField, Min(0.1f)] private float attackRange = 3.7f; //공격 가능한 거리
        [SerializeField, Min(0.1f)] private float attackInterval = 0.96f; //공격 사이 간격
        [SerializeField, Min(1f)] private float attackDamage = 13f; //한 번에 입히는 피해

        private readonly List<Vector2> route = new List<Vector2>(); //순찰 또는 추격 경로
        private Vector2 facing = Vector2.left; //경비가 바라보는 방향
        private Vector2 lastSeen; //감염원을 마지막으로 본 위치
        private float suspicion; //현재까지 쌓인 발견 시간
        private float memory; //추격할 위치의 기억 시간
        private float routeDelay; //다음 경로 갱신까지 남은 시간
        private float cooldown = 0.6f; //다음 공격까지 남은 시간
        private float flashTime; //공격 표시의 남은 시간
        private int waypoint; //현재 경유점 번호
        private int patrolIndex; //목표 순찰 지점 번호
        private bool visible = true; //플레이어가 경비를 보는 상태

        public Vector2 position => transform.position; //경비의 월드 위치
        public bool isMoving { get; private set; } //현재 발소리가 발생하는지 여부
        public bool isAlert => memory > 0f; //감염원을 추격하는 상태

        public float tick(float deltaTime, Vector2 playerPosition, float detectionMultiplier, IndoorNavigationComponent navigation) //순찰, 탐지, 추격 및 공격 피해 계산
        {
            routeDelay -= deltaTime;
            cooldown -= deltaTime;
            memory = Mathf.Max(0f, memory - deltaTime);
            flashTime = Mathf.Max(0f, flashTime - deltaTime);
            attackFlash.enabled = visible && flashTime > 0f;
            isMoving = false;

            Vector2 towardPlayer = playerPosition - position; //경비에서 플레이어로 향하는 방향
            float distance = towardPlayer.magnitude; //경비와 플레이어 사이 거리
            bool inField = distance < 1.45f || isAlert || Vector2.Dot(facing, towardPlayer.normalized) > 0.32f; //경비의 시야각 안인지 여부
            bool seesPlayer = distance <= sightRange && inField && navigation.hasSight(position, playerPosition); //지형에 막히지 않는 플레이어 시야
            if (seesPlayer)
                suspicion += deltaTime * detectionMultiplier;
            else
                suspicion = Mathf.Max(0f, suspicion - deltaTime * 1.8f);

            if (seesPlayer && (isAlert || suspicion >= detectionTime))
            {
                if (!isAlert)
                {
                    cooldown = Mathf.Max(cooldown, 0.44f);
                    routeDelay = 0f;
                }
                lastSeen = playerPosition;
                memory = 2.5f;
                suspicion = detectionTime;
            }

            body.color = isAlert ? new Color(1f, 0.72f, 0.59f) : Color.white;
            if (isAlert && seesPlayer && distance <= attackRange)
            {
                face(towardPlayer);
                if (cooldown > 0f)
                    return 0f;

                cooldown = attackInterval;
                showAttack(playerPosition);
                return attackDamage;
            }

            if (!isAlert && (patrolPoints[patrolIndex] - position).sqrMagnitude < 0.10f)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                routeDelay = 0f;
            }

            Vector2 target = isAlert ? lastSeen : patrolPoints[patrolIndex]; //추격 또는 순찰 목표
            if (routeDelay <= 0f)
            {
                navigation.findRoute(position, target, false, route);
                waypoint = 0;
                routeDelay = 0.28f;
            }

            Vector2 oldPosition = position; //이동 전 경비 위치
            float speed = isAlert ? chaseSpeed : patrolSpeed; //현재 상태의 이동 속도
            Vector2 next = navigation.followRoute(oldPosition, route, ref waypoint, speed * deltaTime, 0.22f); //이동 후 경비 위치
            transform.position = next;
            isMoving = (next - oldPosition).sqrMagnitude > 0.00001f;
            if (isMoving)
                face(next - oldPosition);

            return 0f;
        }

        public void setVisible(bool value) //플레이어 시야에 맞춰 경비 표시
        {
            visible = value;
            body.enabled = visible;
            attackFlash.enabled = visible && flashTime > 0f;
        }

        private void face(Vector2 direction) //경비 외형과 시야 방향 변경
        {
            if (direction.sqrMagnitude <= 0.00001f)
                return;

            facing = direction.normalized;
            body.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg - 90f);
        }

        private void showAttack(Vector2 target) //공격한 방향의 짧은 화면 효과
        {
            Vector2 delta = target - position; //공격 효과의 방향과 길이
            attackFlash.transform.position = (target + position) * 0.5f;
            attackFlash.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            attackFlash.transform.localScale = new Vector3(delta.magnitude, 0.045f, 1f);
            flashTime = 0.11f;
            attackFlash.enabled = visible;
        }
    }
}
