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
        private Rect playfield = new Rect(0, .23f, 1, .61f); //UI 사이에서 플레이어가 보이는 영역

        public void setPlayfield(Rect value) //UI 비율에 맞춘 가시 영역과 화면 전체 시야 설정
        {
            playfield = value;
            gameCamera.rect = new Rect(0, 0, 1, 1);
            gameCamera.orthographicSize = viewHeight * .5f / Mathf.Max(.3f, playfield.height);
        }

        public void initialize(Player target, Maze world) //고정 시야와 추적 범위 연결
        {
            player = target;
            maze = world;
            gameCamera.orthographic = true;
            setPlayfield(playfield);
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
            float offset = (playfield.center.y - .5f) * halfHeight * 2; //하단 조작판 위쪽으로 플레이어를 배치하는 보정
            float visibleHalfHeight = halfHeight * playfield.height; //UI 사이에서 보이는 월드 반높이
            return new Vector3(width <= halfWidth * 2 ? width * .5f : Mathf.Clamp(player.position.x, halfWidth, width - halfWidth),
                height <= visibleHalfHeight * 2 ? height * .5f - offset : Mathf.Clamp(player.position.y - offset, visibleHalfHeight - offset, height - visibleHalfHeight - offset), -10f);
        }
    }
}
