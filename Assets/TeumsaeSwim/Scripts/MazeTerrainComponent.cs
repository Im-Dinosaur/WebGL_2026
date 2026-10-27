using System.Collections.Generic;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class MazeTerrainComponent : MonoBehaviour
    {
        [SerializeField] private float netWorkTime = 2f; //그물 개방에 필요한 누적 시간
        [SerializeField] private float gapWidth = .66f; //틈새의 유효 너비
        private MazeData data; //현재 지형 데이터
        public float gapSize => gapWidth; //표시와 공유할 통로의 실제 너비
        public float hidingRadius => .97f; //표시와 공유할 원형 은신 범위

        public void initialize(MazeData mazeData) //지형 판정 데이터 초기화
        {
            data = mazeData;
        }

        public Vector2 move(Vector2 position, Vector2 delta, float radius, bool predator, bool squeeze) //작은 단계로 나눈 충돌 이동
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .08f)); //터널링을 방지하는 단계 수
            Vector2 step = delta / steps; //한 단계의 이동량
            for (int i = 0; i < steps; i++)
            {
                Vector2 next = position + new Vector2(step.x, 0); //가로 이동 후보
                if (canOccupy(next, radius, predator, squeeze)) position = next;
                next = position + new Vector2(0, step.y);
                if (canOccupy(next, radius, predator, squeeze)) position = next;
            }
            return position;
        }

        public bool canOccupy(Vector2 position, float radius, bool predator, bool squeeze) //몸체와 지형의 충돌 검사
        {
            return bodyFits(position, radius, predator, squeeze, false);
        }

        public bool canOccupyProjectile(Vector2 position, float radius) //안전구역의 경계를 통과하는 미끼 충돌 검사
        {
            return bodyFits(position, radius, true, false, true);
        }

        private bool bodyFits(Vector2 position, float radius, bool predator, bool squeeze, bool projectile) //몸체 용도에 따른 지형과 안전구역 검사
        {
            if (position.x < radius || position.y < radius || position.x > data.width * data.tileSize - radius || position.y > data.height * data.tileSize - radius) return false;
            int minX = Mathf.FloorToInt((position.x - radius) / data.tileSize); //검사 시작 열
            int maxX = Mathf.FloorToInt((position.x + radius) / data.tileSize); //검사 끝 열
            int minY = Mathf.FloorToInt((position.y - radius) / data.tileSize); //검사 시작 행
            int maxY = Mathf.FloorToInt((position.y + radius) / data.tileSize); //검사 끝 행
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                int id = y * data.width + x; //검사 대상 칸
                foreach (Rect obstacle in obstacles(id, predator, squeeze, false, projectile))
                    if (circleHits(position, radius, obstacle)) return false;
            }
            return true;
        }

        public Vector2 moveProjectile(Vector2 position, Vector2 delta, float radius, out Vector2 normal) //미끼의 첫 충돌 위치와 반사면 계산
        {
            normal = Vector2.zero;
            if (canOccupyProjectile(position + delta, radius)) return position + delta;
            bool blockedX = !canOccupyProjectile(position + new Vector2(delta.x, 0f), radius); //가로 방향 충돌
            bool blockedY = !canOccupyProjectile(position + new Vector2(0f, delta.y), radius); //세로 방향 충돌
            normal = new Vector2(blockedX ? -Mathf.Sign(delta.x) : 0f, blockedY ? -Mathf.Sign(delta.y) : 0f).normalized;
            if (normal.sqrMagnitude < .01f) normal = -delta.normalized;
            float lower = 0f; //충돌하지 않는 이동 비율
            float upper = 1f; //충돌한 이동 비율
            for (int i = 0; i < 10; i++)
            {
                float middle = (lower + upper) * .5f; //이번에 검사할 이동 비율
                if (canOccupyProjectile(position + delta * middle, radius)) lower = middle;
                else upper = middle;
            }
            return position + delta * lower;
        }

        private IEnumerable<Rect> obstacles(int id, bool predator, bool squeeze, bool sight, bool projectile = false) //용도에 따라 다른 장애물 영역
        {
            MazeCell cell = data.cells[id]; //대상 지형
            Vector2 origin = data.center(id) - Vector2.one * data.tileSize * .5f; //칸의 왼쪽 아래
            float tile = data.tileSize; //지형 크기
            if (cell.kind == TerrainKind.Wall || (cell.safeZone && predator && !projectile && !sight) || (cell.kind == TerrainKind.Gap && !sight && (predator || !squeeze)))
                yield return new Rect(origin.x, origin.y, tile, tile);
            else if (cell.kind == TerrainKind.Net && !cell.netOpen && !sight)
                yield return cell.horizontal ? new Rect(origin.x + tile * .5f - .08f, origin.y, .16f, tile) : new Rect(origin.x, origin.y + tile * .5f - .08f, tile, .16f);
            else if (cell.kind == TerrainKind.Gap)
            {
                float ledge = (tile - gapWidth) * .5f; //바위의 돌출 너비
                if (cell.horizontal)
                {
                    yield return new Rect(origin.x, origin.y, tile, ledge);
                    yield return new Rect(origin.x, origin.y + tile - ledge, tile, ledge);
                }
                else
                {
                    yield return new Rect(origin.x, origin.y, ledge, tile);
                    yield return new Rect(origin.x + tile - ledge, origin.y, ledge, tile);
                }
            }
        }

        private bool circleHits(Vector2 position, float radius, Rect rect) //원과 사각형의 겹침 검사
        {
            Vector2 nearest = new Vector2(Mathf.Clamp(position.x, rect.xMin, rect.xMax), Mathf.Clamp(position.y, rect.yMin, rect.yMax)); //사각형의 가장 가까운 점
            return (position - nearest).sqrMagnitude < radius * radius - .00001f;
        }

        public bool hasSight(Vector2 from, Vector2 to) //그물을 통과하고 바위에 막히는 시선
        {
            Vector2 delta = to - from; //시선 벡터
            return rayDistance(from, delta.normalized, delta.magnitude) >= delta.magnitude - .015f;
        }

        public float rayDistance(Vector2 origin, Vector2 direction, float range) //지형과 만나는 첫 시야 거리
        {
            for (float distance = 0; distance < range; distance += .055f)
            {
                Vector2 point = origin + direction * distance; //시선의 검사 점
                int id = data.cellAt(point); //시선이 지나는 칸
                if (id < 0) return distance;
                foreach (Rect rect in obstacles(id, false, true, true)) if (rect.Contains(point)) return distance;
            }
            return range;
        }

        public bool isHidden(Vector2 position, float radius) //풀의 원형 영역 안에 완전히 들어왔는지 검사
        {
            int id = data.cellAt(position); //현재 칸
            return id >= 0 && data.cells[id].kind == TerrainKind.Grass && Vector2.Distance(position, data.center(id)) + radius <= hidingRadius;
        }

        public bool overlapsGap(Vector2 position, float radius) //몸체가 틈새 칸에 걸쳐 있는지 검사
        {
            int origin = data.cellAt(position); //현재 칸
            if (origin < 0) return false;
            int x = origin % data.width; //중심 열
            int y = origin / data.width; //중심 행
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int cx = x + dx; //검사 열
                int cy = y + dy; //검사 행
                if (cx < 0 || cy < 0 || cx >= data.width || cy >= data.height) continue;
                int id = cy * data.width + cx; //검사 칸
                if (data.cells[id].kind != TerrainKind.Gap) continue;
                Vector2 lower = data.center(id) - Vector2.one * data.tileSize * .5f; //칸의 왼쪽 아래
                if (circleHits(position, radius, new Rect(lower, Vector2.one * data.tileSize))) return true;
            }
            return false;
        }

        public Vector2 alignGap(Vector2 position, Vector2 input, float deltaTime) //통로 방향 입력 중에만 중앙으로 보조 이동
        {
            int id = data.cellAt(position + input.normalized * .65f); //진입하려는 칸
            if (id < 0 || data.cells[id].kind != TerrainKind.Gap) return position;
            Vector2 target = position; //정렬할 위치
            if (!data.cells[id].horizontal && Mathf.Abs(input.y) > .4f) target.x = data.center(id).x;
            else if (data.cells[id].horizontal && Mathf.Abs(input.x) > .4f) target.y = data.center(id).y;
            return move(position, Vector2.ClampMagnitude(target - position, 2.4f * deltaTime), .22f, false, true);
        }

        public int nearbyNet(Vector2 position) //접근 가능한 거리의 가장 가까운 그물
        {
            int nearest = -1; //선택할 그물
            float distance = 1.25f; //최대 작업 거리
            for (int id = 0; id < data.cells.Length; id++)
            {
                if (data.cells[id].kind != TerrainKind.Net || data.cells[id].netOpen) continue;
                float candidate = Vector2.Distance(position, data.center(id)); //그물까지 거리
                if (candidate < distance && hasSight(position, data.center(id))) { nearest = id; distance = candidate; }
            }
            return nearest;
        }

        public bool workNet(int id, float deltaTime) //누적 진행량을 보존하며 그물 개방
        {
            if (id < 0 || data.cells[id].kind != TerrainKind.Net || data.cells[id].netOpen) return false;
            data.cells[id].netProgress = Mathf.Min(netWorkTime, data.cells[id].netProgress + deltaTime);
            if (data.cells[id].netProgress < netWorkTime) return false;
            data.cells[id].netOpen = true;
            return true;
        }

        public float netProgress(int id) //Inspector의 작업 시간에 맞춘 그물 진행률
        {
            return id < 0 ? 0f : Mathf.Clamp01(data.cells[id].netProgress / Mathf.Max(.01f, netWorkTime));
        }


    }
}
