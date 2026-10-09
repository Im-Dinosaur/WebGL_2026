using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class PlayerMovementComponent : MonoBehaviour
    {
        [SerializeField] private float swimSpeed = 3.35f; //기본 헤엄 속도
        [SerializeField] private float squeezeSpeed = 1.65f; //몸을 줄일 때 이동 속도
        [SerializeField] private float bodyRadius = .36f; //기본 충돌 반경
        [SerializeField] private float squeezeRadius = .22f; //몸을 줄인 충돌 반경
        [SerializeField] private float acceleration = 15f; //헤엄 가속도
        private Vector2 velocity; //현재 헤엄 속도
        private Maze maze; //지형 진입점
        public Vector2 facing { get; private set; } = Vector2.up; //바라보는 방향
        public bool squeezed { get; private set; } //몸을 줄인 상태
        public bool moving => velocity.sqrMagnitude > .02f; //실제 이동 중 여부
        public float radius => squeezed ? squeezeRadius : bodyRadius; //현재 충돌 반경

        public void initialize(Maze world) //이동 상태 초기화
        {
            maze = world;
            transform.position = maze.data.center(maze.data.start);
            velocity = Vector2.zero;
            facing = Vector2.up;
            squeezed = false;
        }

        public void tick(Vector2 input, bool hold, bool working, float deltaTime) //지형과 입력에 따른 헤엄 처리
        {
            Vector2 position = transform.position; //현재 위치
            bool insideGap = maze.overlapsGap(position, bodyRadius); //틈새에 걸친 상태
            squeezed = hold || insideGap;
            if (working || (insideGap && !hold)) { velocity = Vector2.zero; return; }
            if (input.sqrMagnitude > .04f) facing = input.normalized;
            velocity = Vector2.MoveTowards(velocity, input * (squeezed ? squeezeSpeed : swimSpeed), acceleration * deltaTime);
            if (hold) position = maze.alignGap(position, input, deltaTime);
            Vector2 next = maze.move(position, velocity * deltaTime, radius, false, squeezed); //충돌 적용 결과
            if (deltaTime > 0) velocity = (next - position) / deltaTime;
            transform.position = next;
        }

        public void stop() //남은 이동 관성 해제
        {
            velocity = Vector2.zero;
        }
    }
}
