using UnityEngine;

namespace ZombieCqb
{
    public sealed class StageMapComponent : MonoBehaviour
    {
        [SerializeField] private string stageName; //화면에 표시할 스테이지 이름
        [SerializeField] private Transform spawn; //플레이어의 시작 위치
        [SerializeField] private Transform exit; //탈출 상호작용 위치
        [SerializeField] private IndoorNavigationComponent navigationComponent; //시야 및 이동 경로 계산
        [SerializeField] private DoorComponent[] doors; //상호작용 가능한 문
        [SerializeField] private CivilianComponent[] civilians; //이 맵의 감염 대상
        [SerializeField] private GuardComponent[] guards; //이 맵에 배치한 경비
        [SerializeField] private SpriteRenderer fog; //시야 밖 바닥을 어둡게 표시할 외형
        [SerializeField, Min(1f)] private float visionRange = 9f; //플레이어가 보는 최대 거리
        [SerializeField, Min(0.1f)] private float interactRange = 1.22f; //문과 출구의 상호작용 거리

        private Texture2D fogTexture; //셀별 시야를 담는 텍스처
        private Sprite fogSprite; //시야 텍스처를 표시하는 스프라이트
        private Color32[] fogPixels; //현재 셀별 시야 색
        private bool[] revealed; //이미 한 번 확인한 셀
        private float visibilityDelay; //시야 표시 갱신까지 남은 시간

        public string displayName => stageName; //스테이지 표시 이름
        public Vector2 startPosition => spawn.position; //플레이어의 시작 좌표
        public Vector2 exitPosition => exit.position; //탈출 지점 좌표
        public IndoorNavigationComponent navigation => navigationComponent; //현재 맵의 지형 판정
        public CivilianComponent[] people => civilians; //현재 맵의 시민 목록
        public GuardComponent[] security => guards; //현재 맵의 경비 목록

        public void initialize() //맵의 문과 시야 표시 준비
        {
            navigation.initialize();
            int width = navigation.width; //맵의 가로 셀 수
            int height = navigation.height; //맵의 세로 셀 수
            fogTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            fogTexture.filterMode = FilterMode.Point;
            fogTexture.wrapMode = TextureWrapMode.Clamp;
            fogSprite = Sprite.Create(fogTexture, new Rect(0, 0, width, height), Vector2.zero, 1f, 0, SpriteMeshType.FullRect);
            fog.sprite = fogSprite;
            fogPixels = new Color32[width * height];
            revealed = new bool[width * height];
            updateVisibility(startPosition, 0f, true);
        }

        public bool canExit(Vector2 position) //살아 있는 플레이어의 탈출 위치 도달 여부
        {
            return (exitPosition - position).sqrMagnitude <= interactRange * interactRange
                && navigation.hasSight(position, exitPosition);
        }

        public DoorComponent getNearbyDoor(Vector2 position) //플레이어가 조작할 수 있는 가장 가까운 문
        {
            DoorComponent nearest = null; //상호작용할 문 후보
            float best = interactRange * interactRange; //허용하는 문 거리 제곱
            foreach (DoorComponent door in doors) //현재 맵에서 조작 가능한 문
            {
                Vector2 center = door.transform.position; //문 중심 위치
                Vector2 offset = center - position; //플레이어에서 문까지의 방향
                float distance = offset.sqrMagnitude; //문과의 거리 제곱
                Vector2 approach = door.isOpen ? center : center - offset.normalized * 0.65f; //닫힌 문 앞의 접근 위치
                if (distance > best || !navigation.hasSight(position, approach))
                    continue;

                best = distance;
                nearest = door;
            }

            return nearest;
        }

        public void useDoor(DoorComponent door, Vector2 playerPosition, float playerRadius) //문 셀이 비어 있을 때 문을 닫거나 열기
        {
            if (door == null)
                return;

            if (door.isOpen && isDoorOccupied(door.transform.position, playerPosition, playerRadius))
                return;

            door.toggle();
            updateVisibility(playerPosition, 0f, true);
        }

        public bool isVisible(Vector2 origin, Vector2 target) //플레이어 시야로 목표가 보이는지 확인
        {
            return (target - origin).sqrMagnitude <= visionRange * visionRange && navigation.hasSight(origin, target);
        }

        public void updateVisibility(Vector2 origin, float deltaTime, bool force = false) //시야와 시민, 경비의 표시 상태 갱신
        {
            visibilityDelay -= deltaTime;
            if (!force && visibilityDelay > 0f)
                return;

            visibilityDelay = 0.095f;
            int width = navigation.width; //시야를 계산할 가로 크기
            int height = navigation.height; //시야를 계산할 세로 크기
            for (int y = 0; y < height; y++) //시야 텍스처의 행
            {
                for (int x = 0; x < width; x++) //시야 텍스처의 열
                {
                    Vector2Int cell = new Vector2Int(x, y); //현재 표시할 맵 셀
                    int index = y * width + x; //현재 셀의 텍스처 인덱스
                    bool visible = isVisible(origin, IndoorNavigationComponent.cellCenter(cell)); //플레이어가 보는 셀 여부
                    if (visible)
                        revealed[index] = true;

                    byte opacity = visible ? (byte)0 : revealed[index] ? (byte)118 : (byte)165; //시야 밖의 어두운 정도
                    if (!navigation.isOpenCell(cell))
                        opacity = 0;
                    fogPixels[index] = new Color32(4, 9, 15, opacity);
                }
            }

            fogTexture.SetPixels32(fogPixels);
            fogTexture.Apply(false);
            foreach (CivilianComponent civilian in civilians) //시야에 맞춰 표시할 시민
                civilian.setVisible(isVisible(origin, civilian.position));
            foreach (GuardComponent guard in guards) //시야에 맞춰 표시할 경비
                guard.setVisible(isVisible(origin, guard.position));
        }

        private bool isDoorOccupied(Vector2 center, Vector2 playerPosition, float playerRadius) //닫힐 문과 몸체가 겹치는지 확인
        {
            if (overlapsDoor(center, playerPosition, playerRadius))
                return true;

            foreach (CivilianComponent civilian in civilians) //문 위에 있을 수 있는 시민
                if (overlapsDoor(center, civilian.position, 0.20f))
                    return true;

            foreach (GuardComponent guard in guards) //문 위에 있을 수 있는 경비
                if (overlapsDoor(center, guard.position, 0.22f))
                    return true;

            return false;
        }

        private static bool overlapsDoor(Vector2 center, Vector2 position, float radius) //문 셀과 몸체의 겹침 여부
        {
            Vector2 offset = center - position; //문 중심과 몸체 사이 거리
            return Mathf.Abs(offset.x) < 0.5f + radius && Mathf.Abs(offset.y) < 0.5f + radius;
        }

        private void OnDestroy() //스테이지 교체 시 시야 표시 자원 해제
        {
            if (fogSprite != null)
                Destroy(fogSprite);
            if (fogTexture != null)
                Destroy(fogTexture);
        }
    }
}
