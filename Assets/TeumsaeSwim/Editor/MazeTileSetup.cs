using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace TeumsaeSwim.Editor
{
    public static class MazeTileSetup
    {
        public const string surfacePath = "Assets/TeumsaeSwim/Art/Terrain/TerrainSurfaces.png"; //물과 바위 원본
        private const string overlayPath = "Assets/TeumsaeSwim/Art/Terrain/TerrainOverlays.png"; //풀숲과 그물 원본
        private const string setPath = "Assets/TeumsaeSwim/Art/Terrain/MazeTileSet.asset"; //타일 설정 자원

        [MenuItem("Teumsae Swim/8. Connect map tiles")]
        public static void connect() //기존 씬과 판정 설정을 유지하며 타일 표시 연결
        {
            AssetDatabase.Refresh();
            importAtlas(surfacePath, false);
            importAtlas(overlayPath, true);
            MazeTileSet tiles = AssetDatabase.LoadAssetAtPath<MazeTileSet>(setPath); //재사용할 타일 설정
            if (tiles == null) { tiles = ScriptableObject.CreateInstance<MazeTileSet>(); AssetDatabase.CreateAsset(tiles, setPath); }
            foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(surfacePath).OfType<Sprite>()) link(tiles, sprite.name, sprite);
            foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(overlayPath).OfType<Sprite>()) link(tiles, sprite.name, sprite);
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            Maze maze = UnityEngine.Object.FindFirstObjectByType<Maze>(); //기존 미로 진입점
            MazeTileVisualComponent visual = maze.GetComponent<MazeTileVisualComponent>(); //타일 표시 구성 요소
            if (visual == null) visual = maze.gameObject.AddComponent<MazeTileVisualComponent>();
            link(visual, "art", AssetDatabase.LoadAssetAtPath<SwimArt>("Assets/TeumsaeSwim/Art/SwimArt.asset"));
            link(visual, "tiles", tiles);
            link(maze, "visual", visual);
            EditorSceneManager.MarkSceneDirty(maze.gameObject.scene);
            EditorSceneManager.SaveScene(maze.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("SUMBAKKOGI_TILES_CONNECTED");
        }

        public static void connectAndVerify() //씬 연결 후 지형과 기존 플레이 규칙 검사
        {
            connect();
            SwimVerification.run();
            MazeTileVerification.run();
        }

        private static void importAtlas(string path, bool overlays) //원본을 수정하지 않고 Sprite 영역과 알파를 가져오기
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false); //가져오기 경계 검사에만 쓰는 원본
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(path))) throw new InvalidOperationException("타일 이미지를 읽지 못했습니다: " + path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); //텍스처 가져오기 설정
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = overlays ? 256 : source.width * .5f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = overlays;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.isReadable = false;
                var settings = new TextureImporterSettings(); //겹치지 않는 사각 스프라이트 영역
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                string[] names = overlays ? new[] { "grass", "coral", "closedNet", "openNet" } : new[] { "water", "rock" }; //설정 필드와 같은 그림 이름
                Rect[] bounds = overlays ? overlayBounds(source) : new[]
                {
                    new Rect(0, 0, source.width / 2, source.height),
                    new Rect(source.width / 2, 0, source.width / 2, source.height)
                }; //원본 좌표의 그림 영역
                var factories = new SpriteDataProviderFactories(); //설치된 Sprite 편집 API
                factories.Init();
                ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer); //스프라이트 영역 편집기
                provider.InitSpriteEditorDataProvider();
                SpriteRect[] previous = provider.GetSpriteRects(); //기존 참조 식별자
                var rects = new SpriteRect[names.Length]; //이번 그림 영역
                for (int i = 0; i < names.Length; i++)
                {
                    SpriteRect existing = previous.FirstOrDefault(rect => rect.name == names[i]); //이전 같은 그림
                    rects[i] = new SpriteRect { name = names[i], rect = bounds[i], alignment = SpriteAlignment.Center,
                        pivot = Vector2.one * .5f, spriteID = existing != null ? existing.spriteID : GUID.Generate() };
                }
                provider.SetSpriteRects(rects);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
                provider.Apply();
                importer.SaveAndReimport();
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
        }

        private static Rect[] overlayBounds(Texture2D source) //원본의 비어 있는 띠로 나눈 네 그림의 실제 알파 경계
        {
            Rect[] regions = { Rect.MinMaxRect(0, .43f, .53f, 1), Rect.MinMaxRect(.55f, .43f, 1, 1),
                Rect.MinMaxRect(0, 0, .495f, .4f), Rect.MinMaxRect(.495f, 0, 1, .4f) }; //그림 사이 투명 여백으로 구분한 검색 영역
            Color32[] pixels = source.GetPixels32(); //투명도 검사 픽셀
            var result = new Rect[regions.Length]; //잘림 없는 각 그림의 사각 영역
            for (int i = 0; i < regions.Length; i++)
            {
                int left = Mathf.RoundToInt(regions[i].xMin * source.width); //검색 왼쪽
                int right = Mathf.RoundToInt(regions[i].xMax * source.width); //검색 오른쪽
                int bottom = Mathf.RoundToInt(regions[i].yMin * source.height); //검색 아래쪽
                int top = Mathf.RoundToInt(regions[i].yMax * source.height); //검색 위쪽
                int minX = right, maxX = left, minY = top, maxY = bottom; //불투명 픽셀 경계
                for (int y = bottom; y < top; y++)
                for (int x = left; x < right; x++)
                {
                    if (pixels[y * source.width + x].a <= 8) continue;
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                }
                if (minX >= maxX || minY >= maxY || minX < left + 2 || maxX > right - 3 || minY < bottom + 2 || maxY > top - 3)
                    throw new InvalidOperationException("타일 그림의 투명 여백을 확인하세요: " + i + " bounds=" + minX + "," + minY + "," + maxX + "," + maxY);
                result[i] = Rect.MinMaxRect(minX - 2, minY - 2, maxX + 3, maxY + 3);
            }
            return result;
        }

        private static void link(UnityEngine.Object target, string property, UnityEngine.Object value) //다른 설정을 보존하는 Inspector 참조 연결
        {
            var settings = new SerializedObject(target); //직렬화 편집 대상
            settings.FindProperty(property).objectReferenceValue = value;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
