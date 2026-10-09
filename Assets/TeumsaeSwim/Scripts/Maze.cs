using System.Collections.Generic;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class Maze : MonoBehaviour
    {
        [SerializeField] private MazeGenerationComponent generation; //미로 생성 구성 요소
        [SerializeField] private MazeTerrainComponent terrain; //지형 처리 구성 요소
        public MazeData data { get; private set; } //현재 미로
        public IReadOnlyList<BaitData> baits => terrain.baits; //현재 배치된 미끼

        public void initialize(int seed, SwimDifficulty difficulty) //생성과 지형 표시 연결
        {
            data = generation.generate(seed, difficulty);
            terrain.initialize(data);
        }

        public void tick(float deltaTime) //미끼 수명 갱신
        {
            terrain.tick(deltaTime);
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
            return terrain.workNet(id, deltaTime);
        }

        public bool placeBait(Vector2 position, Vector2 facing) //유효한 위치에 미끼 배치
        {
            return terrain.placeBait(position, facing);
        }

        public void consumeBait(BaitData bait) //도착한 포식자의 미끼 소모
        {
            terrain.consumeBait(bait);
        }
    }
}
