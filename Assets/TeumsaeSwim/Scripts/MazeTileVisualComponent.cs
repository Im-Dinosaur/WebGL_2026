using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;

namespace TeumsaeSwim
{
    public sealed class MazeTileVisualComponent : MonoBehaviour
    {
        [SerializeField] private SwimArt art; //공용 재질과 안내 글꼴
        [SerializeField] private MazeTileSet tiles; //타일 그림과 표시 설정
        private MazeData data; //현재 표시할 미로
        private Transform terrainRoot; //재생성할 지형 그림
        private Tile waterTile; //재사용하는 물길 타일
        private Mesh shoreline; //연결된 벽의 노출된 가장자리
        private readonly Dictionary<int, Tile> rockTiles = new Dictionary<int, Tile>(); //인접 방향별 바위 타일
        private readonly Dictionary<int, SpriteRenderer> nets = new Dictionary<int, SpriteRenderer>(); //상태를 바꿀 그물 그림
        private readonly List<Sprite> gapSprites = new List<Sprite>(); //실제 바위 너비에 맞춰 잘라 쓰는 틈새 그림
        private readonly List<Vector3> edgeVertices = new List<Vector3>(); //경계 메시의 꼭짓점
        private readonly List<int> edgeTriangles = new List<int>(); //경계 메시의 삼각형
        private readonly List<Color> edgeColors = new List<Color>(); //경계 메시의 음영

        public void initialize(MazeData mazeData, float gapWidth, float hidingRadius) //같은 미로 데이터와 판정 크기로 타일 표시
        {
            clearTerrain();
            data = mazeData;
            terrainRoot = new GameObject("Generated tile terrain").transform;
            terrainRoot.SetParent(transform, false);
            art.shape(terrainRoot, "Ocean bed", "box", new Vector2(data.width, data.height) * data.tileSize * .5f,
                new Vector2(data.width, data.height) * data.tileSize, new Color32(24, 88, 100, 255), -20);
            var gridObject = new GameObject("Terrain grid", typeof(Grid)); //한 칸을 실제 지형 크기에 맞춘 격자
            gridObject.transform.SetParent(terrainRoot, false);
            gridObject.transform.localScale = new Vector3(data.tileSize, data.tileSize, 1);
            Tilemap water = createLayer(gridObject.transform, "Water tiles", -10); //물길 층
            Tilemap rocks = createLayer(gridObject.transform, "Rock tiles", 1); //바위 층
            if (waterTile == null) waterTile = createTile(tiles.waterSprite, tiles.waterColor);
            for (int id = 0; id < data.cells.Length; id++)
            {
                int x = id % data.width; //현재 열
                int y = id / data.width; //현재 행
                Vector3Int position = new Vector3Int(x, y, 0); //타일 격자 좌표
                bool flipX = x % 2 != 0; //경계 픽셀이 이어지는 가로 반복
                bool flipY = y % 2 != 0; //경계 픽셀이 이어지는 세로 반복
                Matrix4x4 mirror = Matrix4x4.Scale(new Vector3(flipX ? -1 : 1, flipY ? -1 : 1, 1)); //표면 반복 변환
                water.SetTile(position, waterTile);
                water.SetTransformMatrix(position, mirror);
                MazeCell cell = data.cells[id]; //표시할 지형 종류
                if (cell.kind == TerrainKind.Wall)
                {
                    int mask = connectionMask(x, y); //위 오른쪽 아래 왼쪽의 연결 비트
                    int textureMask = mirrorMask(mask, flipX, flipY); //반전된 그림 좌표의 연결 방향
                    rocks.SetTile(position, rockTile(textureMask));
                    rocks.SetTransformMatrix(position, mirror);
                    addShoreline(outline(mask), data.center(id), data.tileSize, mask);
                    if (mask != 15 && (x * 17 + y * 31) % 13 == 0)
                        drawSprite("Coral", tiles.coralSprite, data.center(id), Vector2.one * .72f, 3);
                }
                else if (cell.kind == TerrainKind.Grass) drawGrass(id, hidingRadius);
                else if (cell.kind == TerrainKind.Gap) drawGap(id, gapWidth);
                else if (cell.kind == TerrainKind.Net) drawNet(id);
            }
            createShoreline();
            drawSafeRoom(data.start, "시작 · 안전구역");
            drawSafeRoom(data.exit, "도착 · 안전구역");
            art.shape(terrainRoot, "Safe start ring", "ring", data.center(data.start), Vector2.one * 1.7f, new Color(.65f, 1f, .85f, .45f), 2);
            art.shape(terrainRoot, "Exit halo", "circle", data.center(data.exit), Vector2.one * 1.9f, new Color(.45f, .95f, .8f, .3f), 3);
            art.shape(terrainRoot, "Exit ring", "ring", data.center(data.exit), Vector2.one * 1.5f, SwimArt.mint, 4);
            art.label(terrainRoot, "출구", data.center(data.exit), .42f, Color.white);
        }

        private Tilemap createLayer(Transform parent, string title, int order) //배치 렌더링으로 묶이는 타일맵 층 생성
        {
            var item = new GameObject(title, typeof(Tilemap), typeof(TilemapRenderer)); //타일맵 오브젝트
            item.transform.SetParent(parent, false);
            TilemapRenderer renderer = item.GetComponent<TilemapRenderer>(); //타일맵 렌더러
            renderer.sharedMaterial = art.material;
            renderer.sortingOrder = order;
            renderer.mode = TilemapRenderer.Mode.Chunk;
            return item.GetComponent<Tilemap>();
        }

        private Tile createTile(Sprite sprite, Color color) //충돌 없는 시각 전용 타일 생성
        {
            Tile tile = ScriptableObject.CreateInstance<Tile>(); //타일 표시 설정
            tile.sprite = sprite;
            tile.color = color;
            tile.colliderType = Tile.ColliderType.None;
            tile.flags = TileFlags.LockColor;
            return tile;
        }

        private int connectionMask(int x, int y) //바위가 연속되는 네 방향 계산
        {
            return (isRock(x, y + 1) ? 1 : 0) | (isRock(x + 1, y) ? 2 : 0) |
                (isRock(x, y - 1) ? 4 : 0) | (isRock(x - 1, y) ? 8 : 0);
        }

        private bool isRock(int x, int y) //맵 바깥을 포함한 바위 연결 여부
        {
            return x < 0 || y < 0 || x >= data.width || y >= data.height || data.cells[y * data.width + x].kind == TerrainKind.Wall;
        }

        private int mirrorMask(int mask, bool horizontal, bool vertical) //텍스처 반전 후에도 실제 지형 방향을 유지
        {
            if (horizontal) mask = (mask & 5) | ((mask & 2) << 2) | ((mask & 8) >> 2);
            if (vertical) mask = (mask & 10) | ((mask & 1) << 2) | ((mask & 4) >> 2);
            return mask;
        }

        private Tile rockTile(int mask) //인접 지형에 맞춘 바위 모서리 그림 재사용
        {
            if (rockTiles.TryGetValue(mask, out Tile cached)) return cached;
            Sprite source = tiles.rockSprite; //원본 바위 표면
            Sprite sprite = Sprite.Create(source.texture, source.rect, Vector2.one * .5f, source.rect.width, 0, SpriteMeshType.FullRect); //한 칸 크기의 바위 그림
            sprite.name = "Connected rock " + mask;
            List<Vector2> points = outline(mask); //노출된 바깥 모서리만 둥글게 자른 꼭짓점
            var vertices = new Vector2[points.Count + 1]; //삼각형 부채의 중심과 둘레
            var indices = new ushort[points.Count * 3]; //바위 표면 삼각형
            for (int i = 0; i < points.Count; i++)
            {
                vertices[i + 1] = points[i];
                indices[i * 3] = 0;
                indices[i * 3 + 1] = (ushort)(i + 1);
                indices[i * 3 + 2] = (ushort)((i + 1) % points.Count + 1);
            }
            var positionValues = new Vector3[vertices.Length]; //편집기와 플레이 모드에서 공유할 꼭짓점
            var coordinateValues = new Vector2[vertices.Length]; //원본 바위 표면의 텍스처 좌표
            for (int i = 0; i < vertices.Length; i++)
            {
                positionValues[i] = vertices[i];
                coordinateValues[i] = new Vector2((source.rect.x + (vertices[i].x + .5f) * source.rect.width) / source.texture.width,
                    (source.rect.y + (vertices[i].y + .5f) * source.rect.height) / source.texture.height);
            }
            using (var positions = new NativeArray<Vector3>(positionValues, Allocator.Temp)) //렌더러에 전달할 꼭짓점
            using (var coordinates = new NativeArray<Vector2>(coordinateValues, Allocator.Temp)) //렌더러에 전달할 UV
            using (var triangles = new NativeArray<ushort>(indices, Allocator.Temp)) //잘린 모서리를 포함한 삼각형
            {
                sprite.SetVertexCount(vertices.Length);
                sprite.SetVertexAttribute(VertexAttribute.Position, positions);
                sprite.SetVertexAttribute(VertexAttribute.TexCoord0, coordinates);
                sprite.SetIndices(triangles);
            }
            Tile result = createTile(sprite, Color.white); //연결 방향별 표시 타일
            rockTiles.Add(mask, result);
            return result;
        }

        private List<Vector2> outline(int mask) //반시계 방향의 바위 외곽 다각형
        {
            var result = new List<Vector2>(16); //외곽 꼭짓점
            int[] neighbors = { 12, 6, 3, 9 }; //왼쪽 아래부터 각 모서리의 인접 방향
            Vector2[] corners = { new Vector2(-.5f, -.5f), new Vector2(.5f, -.5f), new Vector2(.5f, .5f), new Vector2(-.5f, .5f) }; //타일 모서리
            for (int corner = 0; corner < 4; corner++)
            {
                if ((mask & neighbors[corner]) != 0) { result.Add(corners[corner]); continue; }
                float radius = tiles.rounding; //바깥 모서리에만 적용하는 반경
                Vector2 center = corners[corner] * (1f - radius * 2f); //원호 중심
                for (int step = 0; step <= 3; step++)
                {
                    float angle = (180f + corner * 90f + step * 30f) * Mathf.Deg2Rad; //모서리 원호 각도
                    result.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                }
            }
            return result;
        }

        private void addShoreline(List<Vector2> points, Vector2 center, float scale, int mask) //연속된 바위 안쪽 선을 생략한 경계 메시 추가
        {
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 a = points[i]; //경계 시작점
                Vector2 b = points[(i + 1) % points.Count]; //경계 끝점
                if (((mask & 1) != 0 && a.y > .499f && b.y > .499f) ||
                    ((mask & 2) != 0 && a.x > .499f && b.x > .499f) ||
                    ((mask & 4) != 0 && a.y < -.499f && b.y < -.499f) ||
                    ((mask & 8) != 0 && a.x < -.499f && b.x < -.499f)) continue;
                Vector2 delta = (b - a).normalized; //외곽선 방향
                Vector2 inward = new Vector2(-delta.y, delta.x); //바위 안쪽 방향
                Color color = inward.y < 0 || inward.x > 0 ? new Color32(75, 128, 148, 255) : new Color32(9, 39, 54, 255); //빛을 받는 면과 그늘
                int start = edgeVertices.Count; //이번 경계의 첫 꼭짓점
                edgeVertices.Add(center + a * scale);
                edgeVertices.Add(center + b * scale);
                edgeVertices.Add(center + (b + inward * .025f) * scale);
                edgeVertices.Add(center + (a + inward * .025f) * scale);
                edgeTriangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                for (int vertex = 0; vertex < 4; vertex++) edgeColors.Add(color);
            }
        }

        private void createShoreline() //벽 경계를 하나의 메시로 묶어 표시
        {
            shoreline = new Mesh { name = "Connected rock shorelines", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            shoreline.SetVertices(edgeVertices);
            shoreline.SetTriangles(edgeTriangles, 0);
            shoreline.SetColors(edgeColors);
            shoreline.uv = new Vector2[edgeVertices.Count];
            shoreline.RecalculateBounds();
            var item = new GameObject("Rock shoreline", typeof(MeshFilter), typeof(MeshRenderer)); //바위 경계 표시
            item.transform.SetParent(terrainRoot, false);
            item.GetComponent<MeshFilter>().sharedMesh = shoreline;
            MeshRenderer renderer = item.GetComponent<MeshRenderer>(); //경계 렌더러
            renderer.sharedMaterial = art.material;
            renderer.sortingOrder = 2;
        }

        private SpriteRenderer drawSprite(string title, Sprite sprite, Vector2 position, Vector2 size, int order) //그림의 여백을 제외한 실제 크기로 표시
        {
            var item = new GameObject(title, typeof(SpriteRenderer)); //그림 오브젝트
            item.transform.SetParent(terrainRoot, false);
            item.transform.localPosition = position;
            item.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
            SpriteRenderer renderer = item.GetComponent<SpriteRenderer>(); //그림 렌더러
            renderer.sprite = sprite;
            renderer.sharedMaterial = art.material;
            renderer.sortingOrder = order;
            return renderer;
        }

        private void drawGrass(int id, float radius) //실제 은신 원에 맞춘 풀숲 그림과 경계 표시
        {
            Vector2 center = data.center(id); //수풀 중심
            art.shape(terrainRoot, "Grass boundary", "circle", center, Vector2.one * radius * 2f, new Color(.16f, .42f, .3f, .65f), 2);
            drawSprite("Hiding grass", tiles.grassSprite, center, Vector2.one * radius * 2f, 11);
            art.shape(terrainRoot, "Grass edge", "ring", center, Vector2.one * radius * 2f, new Color(.46f, .88f, .61f, .2f), 12);
        }

        private void drawGap(int id, float gapWidth) //충돌 사각형과 같은 너비의 양쪽 바위 표시
        {
            Vector2 center = data.center(id); //틈새 중심
            float ledge = (data.tileSize - gapWidth) * .5f; //양쪽 돌출부 너비
            for (int side = -1; side <= 1; side += 2)
            {
                bool horizontal = data.cells[id].horizontal; //통로 방향
                Vector2 axis = horizontal ? Vector2.up : Vector2.right; //통로에 수직인 방향
                Vector2 size = horizontal ? new Vector2(data.tileSize, ledge) : new Vector2(ledge, data.tileSize); //실제 돌출부 크기
                Sprite source = tiles.rockSprite; //늘리지 않고 잘라 쓸 바위 표면
                Rect region = source.rect; //이번 돌출부에 대응하는 원본 영역
                if (horizontal)
                {
                    region.height *= ledge / data.tileSize;
                    if (side > 0) region.y = source.rect.yMax - region.height;
                }
                else
                {
                    region.width *= ledge / data.tileSize;
                    if (side > 0) region.x = source.rect.xMax - region.width;
                }
                Sprite bank = Sprite.Create(source.texture, region, Vector2.one * .5f, source.rect.width, 0, SpriteMeshType.FullRect); //원래 돌의 비율을 유지한 바위
                gapSprites.Add(bank);
                drawSprite("Gap bank", bank, center + axis * side * (gapWidth + ledge) * .5f, size, 5);
                art.shape(terrainRoot, "Gap edge", "box", center + axis * side * gapWidth * .5f,
                    horizontal ? new Vector2(data.tileSize, .035f) : new Vector2(.035f, data.tileSize), new Color32(76, 132, 148, 255), 6);
            }
            art.label(terrainRoot, "꾹", center, .22f, SwimArt.yellow);
        }

        private void drawNet(int id) //통로를 가로지르는 그물 그림 배치
        {
            SpriteRenderer renderer = drawSprite("Net " + id, tiles.closedNetSprite, data.center(id), new Vector2(data.tileSize, .55f), 5); //닫힌 그물 그림
            if (data.cells[id].horizontal) renderer.transform.localRotation = Quaternion.Euler(0, 0, 90);
            nets.Add(id, renderer);
            refreshNet(id, 0);
        }

        public void refreshNet(int id, float progress) //누적 해제 진행과 열린 그물의 남은 매듭 표시
        {
            if (!nets.TryGetValue(id, out SpriteRenderer renderer)) return;
            bool opened = data.cells[id].netOpen; //그물의 판정 상태
            Sprite sprite = opened ? tiles.openNetSprite : tiles.closedNetSprite; //판정에 대응하는 그림
            renderer.sprite = sprite;
            renderer.transform.localScale = new Vector3(data.tileSize / sprite.bounds.size.x,
                .55f * (opened ? 1f : Mathf.Lerp(1f, .5f, progress)) / sprite.bounds.size.y, 1);
        }

        private void drawSafeRoom(int id, string title) //실제 세 칸 안전구역의 범위와 안내 표시
        {
            Vector2 center = data.center(id); //안전구역 중심
            float size = data.tileSize * 3f; //안전방 한 변
            art.shape(terrainRoot, "Safe water", "box", center, Vector2.one * size, new Color(.45f, .95f, .78f, .25f), -4);
            for (int side = -1; side <= 1; side += 2)
            {
                art.shape(terrainRoot, "Safe boundary", "box", center + Vector2.right * side * size * .5f, new Vector2(.035f, size), SwimArt.mint, 2);
                art.shape(terrainRoot, "Safe boundary", "box", center + Vector2.up * side * size * .5f, new Vector2(size, .035f), SwimArt.mint, 2);
            }
            art.label(terrainRoot, title, center + Vector2.down * 1.5f, .28f, Color.white);
        }

        private void clearTerrain() //재시작 시 이전 지형과 메시 제거
        {
            if (terrainRoot != null) { terrainRoot.gameObject.SetActive(false); removeGenerated(terrainRoot.gameObject); }
            if (shoreline != null) removeGenerated(shoreline);
            nets.Clear();
            foreach (Sprite sprite in gapSprites) removeGenerated(sprite);
            gapSprites.Clear();
            edgeVertices.Clear();
            edgeTriangles.Clear();
            edgeColors.Clear();
        }

        private void OnDestroy() //재사용한 런타임 타일과 스프라이트 정리
        {
            clearTerrain();
            if (waterTile != null) removeGenerated(waterTile);
            foreach (Tile tile in rockTiles.Values) { removeGenerated(tile.sprite); removeGenerated(tile); }
            rockTiles.Clear();
        }

        private void removeGenerated(Object item) //현재 실행 상태에 맞춘 생성 자원 해제
        {
            if (Application.isPlaying) Destroy(item);
            else DestroyImmediate(item);
        }
    }
}
