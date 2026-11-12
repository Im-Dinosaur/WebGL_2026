using UnityEngine;

namespace ZombieCqb
{
    public sealed class PlayerMovementComponent : MonoBehaviour
    {
        [SerializeField, Range(0.1f, 0.45f)] private float bodyRadius = 0.23f; //플레이어의 이동 충돌 반경
        [SerializeField] private Transform spriteBody; //방향을 표시하는 플레이어 외형

        private Vector2 dashDirection; //돌진 중 적용되는 방향
        private float dashRemaining; //돌진의 남은 거리
        private float dashSpeed; //돌진 중 이동 속도

        public Vector2 facing { get; private set; } = Vector2.right; //플레이어가 보는 방향
        public float radius => bodyRadius; //플레이어의 충돌 반경

        public void place(Vector2 position) //스테이지 시작 위치와 방향 적용
        {
            transform.position = position;
            dashRemaining = 0f;
            face(Vector2.right);
        }

        public void startDash(Vector2 direction, float distance, float speed) //이동 컴포넌트에 돌진 적용
        {
            dashDirection = direction.sqrMagnitude > 0f ? direction.normalized : facing;
            dashRemaining = distance;
            dashSpeed = speed;
            face(dashDirection);
        }

        public void tick(Vector2 direction, float speed, float deltaTime, IndoorNavigationComponent navigation) //이동과 돌진의 실제 위치 계산
        {
            Vector2 delta; //이번 프레임에 이동할 거리와 방향
            if (dashRemaining > 0f)
            {
                float distance = Mathf.Min(dashRemaining, dashSpeed * deltaTime); //이번 프레임에 돌진할 거리
                dashRemaining -= distance;
                delta = dashDirection * distance;
            }
            else
            {
                if (direction.sqrMagnitude > 0f)
                    face(direction);
                delta = direction * (speed * deltaTime);
            }

            transform.position = navigation.move(transform.position, delta, bodyRadius);
        }

        private void face(Vector2 direction) //이동 외형을 바라보는 방향으로 회전
        {
            facing = direction.normalized;
            spriteBody.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg - 90f);
        }
    }
}
