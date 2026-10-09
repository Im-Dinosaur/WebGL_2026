using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class CameraFollowComponent : MonoBehaviour
    {
        [SerializeField] private Camera gameCamera; //게임 화면 카메라
        [SerializeField] private float viewHeight = 13.2f; //확대하지 않는 월드 시야 높이
        [SerializeField] private float followSpeed = 7f; //물고기 추적의 부드러움
        private Player player; //따라갈 플레이어 진입점
        private Maze maze; //카메라를 제한할 미로

        public void initialize(Player target, Maze world) //고정 시야와 추적 범위 연결
        {
            player = target;
            maze = world;
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = viewHeight * .5f;
            gameCamera.rect = new Rect(0, .211f, 1f, .673f);
            snap();
        }

        public void tick(float deltaTime) //게임 진행에 맞춰 물고기를 부드럽게 추적
        {
            if (player == null) return;
            Vector3 target = clampedTarget(); //미로 경계 안의 추적 위치
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-followSpeed * deltaTime));
        }

        public void snap() //시작과 재시작 때 즉시 위치 적용
        {
            transform.position = clampedTarget();
        }

        private Vector3 clampedTarget() //화면 가장자리가 미로 밖으로 나가지 않는 위치
        {
            float halfHeight = gameCamera.orthographicSize; //시야 반높이
            float halfWidth = halfHeight * gameCamera.aspect; //시야 반너비
            float width = maze.data.width * maze.data.tileSize; //미로 너비
            float height = maze.data.height * maze.data.tileSize; //미로 높이
            return new Vector3(width <= halfWidth * 2 ? width * .5f : Mathf.Clamp(player.position.x, halfWidth, width - halfWidth),
                height <= halfHeight * 2 ? height * .5f : Mathf.Clamp(player.position.y, halfHeight, height - halfHeight), -10f);
        }
    }
}
