using System.Collections.Generic;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class Maze : MonoBehaviour
    {
        [SerializeField] private MazeGenerationComponent generation; //미로 생성 구성 요소
        [SerializeField] private MazeTerrainComponent terrain; //지형 처리 구성 요소
        [SerializeField] private MazeTileVisualComponent visual; //타일 지형 표시 구성 요소
        [SerializeField] private MazeBaitComponent bait; //미끼 발사와 수명 구성 요소
        [SerializeField] private MazeItemComponent items; //진주 배치와 획득 구성 요소
        public MazeData data { get; private set; } //현재 미로
        public IReadOnlyList<BaitData> baits => bait.baits; //현재 날아가거나 배치된 미끼

        public void initialize(int seed, SwimDifficulty difficulty) //생성과 지형 표시 연결
        {
            data = generation.generate(seed, difficulty);
            terrain.initialize(data);
            visual.initialize(data, terrain.gapSize, terrain.hidingRadius);
            bait.initialize(this);
            items.initialize(this);
        }

        public void tick(float deltaTime) //미끼 수명과 진주 표시 갱신
        {
            bait.tick(deltaTime);
            items.tick(deltaTime);
        }

        public bool tryCollectItem(Vector2 position, float radius) //플레이어 접촉에 따른 일회성 진주 획득 전달
        {
            return items.tryCollect(position, radius);
        }

        public Vector2 move(Vector2 position, Vector2 delta, float radius, bool predator, bool squeeze) //지형 충돌을 적용한 이동
        {
            return terrain.move(position, delta, radius, predator, squeeze);
        }

        public bool canOccupy(Vector2 position, float radius, bool predator = false, bool squeeze = false) //몸체가 들어갈 수 있는 위치 검사
        {
            return terrain.canOccupy(position, radius, predator, squeeze);
        }

        public bool hasSight(Vector2 from, Vector2 to) //장애물에 가리지 않은 시선 검사
        {
            return terrain.hasSight(from, to);
        }

        public float rayDistance(Vector2 origin, Vector2 direction, float range) //시야 표시의 지형 절단 거리
        {
            return terrain.rayDistance(origin, direction, range);
        }

        public bool isHidden(Vector2 position, float radius) //수풀 안에 몸체가 완전히 들어갔는지 검사
        {
            return terrain.isHidden(position, radius);
        }

        public bool isSafeZone(Vector2 position) //플레이어의 중심이 시작 또는 도착 안전구역 안인지 검사
        {
            int id = data.cellAt(position); //현재 칸
            return id >= 0 && data.cells[id].safeZone;
        }

        public float progressAt(Vector2 position) //통로 거리를 기준으로 진행률 전달
        {
            return data.progressAt(position);
        }

        public bool canOccupyProjectile(Vector2 position, float radius) //안전구역을 통과하는 미끼의 지형 충돌 검사
        {
            return terrain.canOccupyProjectile(position, radius);
        }

        public bool overlapsGap(Vector2 position, float radius) //좁은 통로와 몸체의 겹침 검사
        {
            return terrain.overlapsGap(position, radius);
        }

        public Vector2 alignGap(Vector2 position, Vector2 input, float deltaTime) //틈새 진입 시 중앙 정렬 보조
        {
            return terrain.alignGap(position, input, deltaTime);
        }

        public int nearbyNet(Vector2 position) //작업할 수 있는 가까운 그물
        {
            return terrain.nearbyNet(position);
        }

        public bool workNet(int id, float deltaTime) //그물의 누적 작업과 개방
        {
            bool opened = terrain.workNet(id, deltaTime); //이번 작업으로 그물이 열렸는지 여부
            if (id >= 0 && id < data.cells.Length) visual.refreshNet(id, terrain.netProgress(id));
            return opened;
        }

        public bool placeBait(Vector2 position, Vector2 direction, float strength = 1f) //선택한 방향과 거리로 미끼 발사
        {
            return bait.launch(position, direction, strength);
        }

        public bool predictBait(Vector2 position, Vector2 direction, float strength, List<Vector2> path) //실제 비행과 같은 충돌 규칙의 예상 경로 제공
        {
            return bait.predict(position, direction, strength, path);
        }

        public void consumeBait(BaitData bait) //도착한 포식자의 미끼 소모
        {
            this.bait.consume(bait);
        }

        public Vector2 moveProjectile(Vector2 position, Vector2 delta, float radius, out Vector2 normal) //미끼의 충돌 지점과 반사 방향 전달
        {
            return terrain.moveProjectile(position, delta, radius, out normal);
        }

        public float netProgress(int id) //설정된 작업 시간에 대한 그물 진행률
        {
            return terrain.netProgress(id);
        }
    }
}
