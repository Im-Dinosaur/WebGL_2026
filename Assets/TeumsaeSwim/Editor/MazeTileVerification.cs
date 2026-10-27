using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TeumsaeSwim.Editor
{
    public static class MazeTileVerification
    {
        private static int checks; //실제로 통과한 검사 수

        public static void runAndCapture() //표시 판정 검사와 실제 모바일 화면 캡처
        {
            run();
            SwimUIPreview.captureTerrain();
        }

        public static void run() //그림의 위치와 크기 및 상태가 플레이 판정과 일치하는지 검사
        {
            checks = 0;
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            Maze maze = UnityEngine.Object.FindFirstObjectByType<Maze>(); //연결된 실제 미로
            maze.initialize(4817, SwimDifficulty.Normal);
            Tilemap[] layers = maze.GetComponentsInChildren<Tilemap>(); //실제 타일맵 층
            require(layers.Length == 2, "Exactly two batched tile layers");
            Tilemap water = layers.Single(layer => layer.name == "Water tiles"); //물길 층
            Tilemap rock = layers.Single(layer => layer.name == "Rock tiles"); //바위 층
            require(maze.GetComponentsInChildren<Collider2D>().Length == 0, "Rendering does not add competing colliders");
            for (int id = 0; id < maze.data.cells.Length; id++)
            {
                Vector3Int cell = new Vector3Int(id % maze.data.width, id / maze.data.width, 0); //검사할 타일 좌표
                require(Vector2.Distance(water.GetCellCenterWorld(cell), maze.data.center(id)) < .0001f, "Tile and collision cell centers agree");
                require(water.HasTile(cell), "Every cell has water backing");
                require(rock.HasTile(cell) == (maze.data.cells[id].kind == TerrainKind.Wall), "Rock art exactly follows generated walls");
                if (rock.HasTile(cell))
                {
                    Vector2 size = rock.GetSprite(cell).bounds.size; //한 칸 크기로 가져온 바위
                    require(Mathf.Abs(size.x - 1f) < .001f && Mathf.Abs(size.y - 1f) < .001f, "Rock sprites fill one grid unit");
                    int x = cell.x; //열
                    int y = cell.y; //행
                    Vector2[] points = rock.GetSprite(cell).vertices; //실제 잘린 바위 외곽
                    Matrix4x4 matrix = rock.GetTransformMatrix(cell); //표면 반전까지 적용할 변환
                    if (x > 0 && y > 0 && maze.data.cells[id - 1].kind != TerrainKind.Wall && maze.data.cells[id - maze.data.width].kind != TerrainKind.Wall)
                        require(points.All(point => { Vector3 corner = matrix.MultiplyPoint3x4(point); return corner.x > -.499f || corner.y > -.499f; }), "Exposed lower-left corner is actually rounded");
                    foreach (Vector2 point in points)
                    {
                        Vector3 transformed = matrix.MultiplyPoint3x4(point); //실제 타일 안의 위치
                        require(Mathf.Abs(transformed.x) <= .501f && Mathf.Abs(transformed.y) <= .501f, "Wall art stays within blocked cell");
                    }
                    if (x + 1 < maze.data.width && maze.data.cells[id + 1].kind == TerrainKind.Wall)
                        require(points.Count(point => matrix.MultiplyPoint3x4(point).x > .499f) >= 2, "Joined right edge has no rounded gap");
                    if (y + 1 < maze.data.height && maze.data.cells[id + maze.data.width].kind == TerrainKind.Wall)
                        require(points.Count(point => matrix.MultiplyPoint3x4(point).y > .499f) >= 2, "Joined top edge has no rounded gap");
                }
            }
            MazeTerrainComponent terrain = maze.GetComponent<MazeTerrainComponent>(); //표시와 공유하는 판정 크기
            foreach (SpriteRenderer grass in maze.GetComponentsInChildren<SpriteRenderer>().Where(renderer => renderer.name == "Hiding grass"))
            {
                require(Mathf.Abs(grass.bounds.size.x - terrain.hidingRadius * 2f) < .001f, "Grass art matches hiding diameter");
                require(maze.isHidden(grass.transform.position, .36f), "Visible grass center conceals player");
            }
            foreach (SpriteRenderer bank in maze.GetComponentsInChildren<SpriteRenderer>().Where(renderer => renderer.name == "Gap bank"))
                require(Mathf.Abs(Mathf.Min(bank.bounds.size.x, bank.bounds.size.y) - (maze.data.tileSize - terrain.gapSize) * .5f) < .001f, "Gap banks match collision width");
            foreach (int id in Enumerable.Range(0, maze.data.cells.Length).Where(id => maze.data.cells[id].kind == TerrainKind.Net))
            {
                SpriteRenderer net = maze.GetComponentsInChildren<SpriteRenderer>().Single(renderer => renderer.name == "Net " + id); // 실제 그물 그림
                require(net.sprite.name == "closedNet" && !maze.canOccupy(maze.data.center(id), .22f), "Closed net is visible and blocks movement");
                require(!maze.workNet(id, .75f) && net.sprite.name == "closedNet", "Partial work keeps closed art");
                require(maze.workNet(id, 4f) && net.sprite.name == "openNet", "Completion switches to open net art");
                require(net.gameObject.activeInHierarchy && maze.canOccupy(maze.data.center(id), .36f), "Open knots remain visible while center is passable");
                require(Mathf.Abs(Mathf.Max(net.bounds.size.x, net.bounds.size.y) - maze.data.tileSize) < .001f, "Net spans one passage cell");
            }
            maze.initialize(91037, SwimDifficulty.Challenge);
            require(maze.transform.Cast<Transform>().Count(child => child.name == "Generated tile terrain" && child.gameObject.activeSelf) == 1, "Retry replaces prior terrain");
            require(maze.data.start != maze.data.exit, "Safe rooms remain separate");
            Debug.Log("SUMBAKKOGI_TILE_VERIFICATION_OK: " + checks + " checks");
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
        }

        private static void require(bool condition, string message) //검증 실패의 구체적인 원인 표시
        {
            if (!condition) throw new InvalidOperationException(message);
            checks++;
        }
    }
}
