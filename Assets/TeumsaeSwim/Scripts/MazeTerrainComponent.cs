using System.Collections.Generic;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class MazeTerrainComponent : MonoBehaviour
    {
        [SerializeField] private SwimArt art; //지형과 안내 그림
        [SerializeField] private float netWorkTime = 2f; //그물 개방에 필요한 누적 시간
        [SerializeField] private float baitDuration = 8f; //미끼 유지 시간
        [SerializeField] private float gapWidth = .66f; //틈새의 유효 너비
        private MazeData data; //현재 지형 데이터
        private Transform terrainRoot; //재생성할 지형의 루트
        private readonly Dictionary<int, Transform> nets = new Dictionary<int, Transform>(); //열리지 않은 그물 표시
        public readonly List<BaitData> baits = new List<BaitData>(); //활성 미끼

        public void initialize(MazeData mazeData) //지형 데이터와 표시 초기화
        {
            if (terrainRoot != null) { terrainRoot.gameObject.SetActive(false); removeGenerated(terrainRoot.gameObject); }
            data = mazeData;
            nets.Clear();
            baits.Clear();
            terrainRoot = new GameObject("Generated terrain").transform;
            terrainRoot.SetParent(transform, false);
            float tile = data.tileSize; //지형 한 칸 크기
            art.shape(terrainRoot, "Ocean", "box", new Vector2(data.width, data.height) * tile * .5f,
                new Vector2(data.width, data.height) * tile, new Color32(21, 64, 79, 255), -20);
            for (int id = 0; id < data.cells.Length; id++)
            {
                MazeCell cell = data.cells[id]; //현재 지형
                Vector2 center = data.center(id); //지형 중심
                if (cell.kind == TerrainKind.Wall)
                {
                    art.shape(terrainRoot, "Rock", "round", center, Vector2.one * (tile + .07f), new Color32(11, 37, 50, 255), 1);
                    if (id % 4 == 0)
                        art.shape(terrainRoot, "Rock glint", "round", center + new Vector2(-.25f, .43f), new Vector2(.8f, .06f), new Color32(35, 67, 76, 255), 2);
                }
                else
                {
                    if (id % 3 == 0) art.shape(terrainRoot, "Water ripple", "round", center + new Vector2(.3f, -.5f), new Vector2(.42f, .035f), new Color32(55, 111, 123, 135), -5);
                    if (cell.kind == TerrainKind.Grass) drawGrass(id);
                    if (cell.kind == TerrainKind.Gap) drawGap(id);
                    if (cell.kind == TerrainKind.Net) drawNet(id);
                }
            }
            art.shape(terrainRoot, "Safe start ring", "ring", data.center(data.start), Vector2.one * 1.7f, new Color( .45f, .82f, .73f, .35f), 2);
            art.label(terrainRoot, "수면을 향해 ↑", data.center(data.start) + Vector2.up * .78f, .24f, SwimArt.mint);
            art.shape(terrainRoot, "Exit halo", "circle", data.center(data.exit), Vector2.one * 1.9f, new Color(.45f, .95f, .8f, .18f), 3);
            art.shape(terrainRoot, "Exit ring", "ring", data.center(data.exit), Vector2.one * 1.5f, SwimArt.mint, 4);
            art.label(terrainRoot, "↑\n출구", data.center(data.exit), .42f, Color.white);
        }

        private void drawGrass(int id) //몸을 숨길 수 있는 수풀 표시
        {
            Vector2 center = data.center(id); //수풀 중심
            art.shape(terrainRoot, "Grass bed", "circle", center, Vector2.one * 1.95f, new Color32(54, 128, 109, 95), 2);
            for (int i = 0; i < 7; i++)
            {
                float x = (i - 3) * .23f; //해초 가로 위치
                var blade = art.shape(terrainRoot, "Seagrass", "round", center + new Vector2(x, -.2f + (i % 2) * .2f), new Vector2(.12f, .9f + (i % 3) * .22f), new Color32(91, 177, 141, 200), 11); //해초 잎
                blade.transform.localRotation = Quaternion.Euler(0, 0, (i - 3) * -6f);
            }
        }

        private void drawGap(int id) //작은 몸체만 통과하는 바위 틈 표시
        {
            Vector2 center = data.center(id); //틈새 중심
            float ledge = (data.tileSize - gapWidth) * .5f; //양쪽 바위 너비
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 offset = data.cells[id].horizontal ? Vector2.up : Vector2.right; //통로에 수직인 축
                Vector2 size = data.cells[id].horizontal ? new Vector2(data.tileSize, ledge) : new Vector2(ledge, data.tileSize); //바위 크기
                art.shape(terrainRoot, "Gap rock", "round", center + offset * side * (gapWidth + ledge) * .5f, size, new Color32(57, 89, 99, 255), 5);
            }
            art.label(terrainRoot, "꾹", center, .22f, SwimArt.yellow);
        }

        private void drawNet(int id) //시야가 통과하는 그물 표시
        {
            var root = new GameObject("Net").transform; //그물 표시 루트
            root.SetParent(terrainRoot, false);
            root.position = data.center(id);
            if (data.cells[id].horizontal) root.localRotation = Quaternion.Euler(0, 0, 90f);
            for (int i = -4; i <= 4; i++)
            {
                art.shape(root, "Rope", "box", new Vector2(i * .24f, 0), new Vector2(.026f, .34f), new Color32(235, 198, 143, 255), 5);
            }
            art.shape(root, "Cross rope", "box", new Vector2(0, .16f), new Vector2(data.tileSize, .035f), SwimArt.yellow, 5);
            art.shape(root, "Cross rope", "box", new Vector2(0, -.16f), new Vector2(data.tileSize, .035f), SwimArt.yellow, 5);
            nets[id] = root;
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
            if (position.x < radius || position.y < radius || position.x > data.width * data.tileSize - radius || position.y > data.height * data.tileSize - radius) return false;
            int minX = Mathf.FloorToInt((position.x - radius) / data.tileSize); //검사 시작 열
            int maxX = Mathf.FloorToInt((position.x + radius) / data.tileSize); //검사 끝 열
            int minY = Mathf.FloorToInt((position.y - radius) / data.tileSize); //검사 시작 행
            int maxY = Mathf.FloorToInt((position.y + radius) / data.tileSize); //검사 끝 행
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                int id = y * data.width + x; //검사 대상 칸
                foreach (Rect obstacle in obstacles(id, predator, squeeze, false))
                    if (circleHits(position, radius, obstacle)) return false;
            }
            return true;
        }

        private IEnumerable<Rect> obstacles(int id, bool predator, bool squeeze, bool sight) //용도에 따라 다른 장애물 영역
        {
            MazeCell cell = data.cells[id]; //대상 지형
            Vector2 origin = data.center(id) - Vector2.one * data.tileSize * .5f; //칸의 왼쪽 아래
            float tile = data.tileSize; //지형 크기
            if (cell.kind == TerrainKind.Wall || (cell.kind == TerrainKind.Gap && !sight && (predator || !squeeze)))
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
            return id >= 0 && data.cells[id].kind == TerrainKind.Grass && Vector2.Distance(position, data.center(id)) + radius <= .97f;
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
            if (nets.TryGetValue(id, out Transform visual)) visual.localScale = new Vector3(1f, Mathf.Lerp(1f, .25f, data.cells[id].netProgress / netWorkTime), 1f);
            if (data.cells[id].netProgress < netWorkTime) return false;
            data.cells[id].netOpen = true;
            if (visual != null) visual.gameObject.SetActive(false);
            return true;
        }

        public bool placeBait(Vector2 position, Vector2 facing) //벽 너머로 배치되지 않는 미끼 생성
        {
            Vector2 target = position + facing.normalized * 1.15f; //바라보는 방향의 투척 위치
            if (!canOccupy(target, .18f, true, false) || !hasSight(position, target)) return false;
            var visual = art.shape(terrainRoot, "Bait", "circle", target, Vector2.one * .26f, SwimArt.yellow, 8); //미끼 알갱이
            art.shape(visual.transform, "Bait ripple", "ring", Vector2.zero, Vector2.one * 3.4f, new Color(1f, .82f, .4f, .4f), 7);
            baits.Add(new BaitData { position = target, remaining = baitDuration, visual = visual.transform });
            return true;
        }

        public void consumeBait(BaitData bait) //먹은 미끼 표시와 데이터 제거
        {
            if (bait.visual != null) removeGenerated(bait.visual.gameObject);
            baits.Remove(bait);
        }

        private void removeGenerated(GameObject item) //실행 중과 편집기 검증에서 생성된 표시만 정리
        {
            if (Application.isPlaying) Destroy(item);
            else DestroyImmediate(item);
        }

        public void tick(float deltaTime) //일시정지 중 멈추는 미끼 수명
        {
            for (int i = baits.Count - 1; i >= 0; i--)
            {
                baits[i].remaining -= deltaTime;
                if (baits[i].remaining <= 0) consumeBait(baits[i]);
            }
        }
    }
}
