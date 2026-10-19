using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace TeumsaeSwim.Editor
{
    public static class PredatorTypeSetup
    {
        public const string atlasPath = "Assets/TeumsaeSwim/Art/PredatorAtlas.png"; //원본 투명 스프라이트 묶음
        public const string profileFolder = "Assets/TeumsaeSwim/Art/PredatorTypes"; //종류별 Inspector 설정 폴더

        [MenuItem("Teumsae Swim/6. Connect predator types")]
        public static void connect() //기존 씬의 설정을 유지하며 그림과 종류 조합 연결
        {
            importSprites();
            PredatorTypeProfile[] profiles = createProfiles(); //처음에만 기본값으로 생성할 설정
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            SwimGame game = UnityEngine.Object.FindFirstObjectByType<SwimGame>(); //기존 게임 진입점
            PredatorRosterComponent roster = game.GetComponent<PredatorRosterComponent>(); //종류 선택 구성 요소
            if (roster == null) roster = game.gameObject.AddComponent<PredatorRosterComponent>();
            var rosterSettings = new SerializedObject(roster); //목록의 기존 개별 설정을 유지하는 참조 연결
            SerializedProperty list = rosterSettings.FindProperty("profiles"); //종류 목록
            list.arraySize = profiles.Length;
            for (int i = 0; i < profiles.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = profiles[i];
            rosterSettings.ApplyModifiedPropertiesWithoutUndo();
            var settings = new SerializedObject(game); //게임 진입점의 연결
            settings.FindProperty("roster").objectReferenceValue = roster;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("TEUMSAE_PREDATOR_TYPES_CONNECTED");
        }

        public static void connectAndVerify() //로컬 연결 후 기존 규칙과 종류별 동작 함께 검사
        {
            connect();
            SwimVerification.run();
        }

        public static PredatorTypeProfile[] createProfiles() //기본 설정을 처음 생성하고 기존 조정값은 보존
        {
            if (!AssetDatabase.IsValidFolder(profileFolder)) AssetDatabase.CreateFolder("Assets/TeumsaeSwim/Art", "PredatorTypes");
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(atlasPath).OfType<Sprite>().ToArray(); //이름으로 연결할 네 장의 그림
            string[] names = { "기본형", "정찰형", "예민형", "추격형" }; //종류 표시 이름
            string[] hints = { "거리·반응·속도가 균형 잡혀요", "멀리 보지만 이동이 느려요", "반응이 빠르지만 가까이만 봐요", "빠르게 쫓지만 발견 대기가 길어요" }; //화면에 표시할 특성
            Color[] colors = { new Color32(200, 143, 221, 255), new Color32(99, 196, 255, 255), SwimArt.coral, new Color32(255, 143, 197, 255) }; //식별 색상
            float[] ranges = { 3.5f, 5.6f, 2.8f, 3.5f }; //종류별 탐지 거리
            float[] notices = { .9f, 1.1f, .45f, 1.2f }; //종류별 발견 대기
            var result = new PredatorTypeProfile[4]; //연결할 설정 배열
            for (int i = 0; i < result.Length; i++)
            {
                string key = ((PredatorKind)i).ToString(); //파일과 스프라이트의 이름
                string path = profileFolder + "/" + key + ".asset"; //설정 저장 위치
                PredatorTypeProfile profile = AssetDatabase.LoadAssetAtPath<PredatorTypeProfile>(path); //기존 설정
                bool created = profile == null; //첫 생성 여부
                if (created)
                {
                    profile = ScriptableObject.CreateInstance<PredatorTypeProfile>();
                    AssetDatabase.CreateAsset(profile, path);
                }
                var settings = new SerializedObject(profile); //편집 가능한 설정
                if (created)
                {
                    settings.FindProperty("kind").enumValueIndex = i;
                    settings.FindProperty("displayName").stringValue = names[i];
                    settings.FindProperty("description").stringValue = hints[i];
                    settings.FindProperty("accent").colorValue = colors[i];
                    settings.FindProperty("sightDistance").floatValue = ranges[i];
                    settings.FindProperty("noticeDuration").floatValue = notices[i];
                    settings.FindProperty("patrolMultiplier").floatValue = i == 1 ? .8f : 1f;
                    settings.FindProperty("chaseMultiplier").floatValue = i == 1 ? .8f : i == 3 ? 1.3f : 1f;
                    settings.FindProperty("lureMultiplier").floatValue = i == 1 ? .8f : 1f;
                }
                settings.FindProperty("fishSprite").objectReferenceValue = sprites.Single(sprite => sprite.name == key);
                settings.ApplyModifiedPropertiesWithoutUndo();
                result[i] = profile;
            }
            return result;
        }

        private static void importSprites() //투명 픽셀 경계를 기준으로 네 그림을 잘림 없이 임포트
        {
            AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(atlasPath); //원본 이미지 임포터
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 320;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.filterMode = FilterMode.Bilinear;
            importer.isReadable = false;
            importer.SaveAndReimport();
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false); //경계 검사에만 사용하는 임시 원본
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(atlasPath))) throw new InvalidOperationException("포식자 그림을 읽지 못했습니다.");
                Color32[] pixels = texture.GetPixels32(); //알파 경계 검사 픽셀
                var factories = new SpriteDataProviderFactories(); //설치된 2D Sprite 패키지의 임포트 API
                factories.Init();
                ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer); //스프라이트 사각형 편집기
                provider.InitSpriteEditorDataProvider();
                SpriteRect[] previous = provider.GetSpriteRects(); //기존 스프라이트 식별자 보존
                var rects = new SpriteRect[4]; //시안 순서의 네 영역
                for (int i = 0; i < 4; i++)
                {
                    string name = ((PredatorKind)i).ToString(); //종류별 스프라이트 이름
                    SpriteRect existing = previous.FirstOrDefault(rect => rect.name == name); //기존 참조 식별자
                    rects[i] = new SpriteRect
                    {
                        name = name, rect = findBounds(pixels, texture.width, texture.height, i),
                        alignment = SpriteAlignment.Custom, pivot = new Vector2(.64f, .45f),
                        spriteID = existing != null ? existing.spriteID : GUID.Generate()
                    };
                }
                provider.SetSpriteRects(rects);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
                provider.Apply();
                importer.SaveAndReimport();
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        private static Rect findBounds(Color32[] pixels, int width, int height, int quadrant) //각 사분면의 실제 그림과 투명 여백 검사
        {
            int left = quadrant % 2 * (width / 2); //사분면 왼쪽
            int bottom = (quadrant < 2 ? 1 : 0) * (height / 2); //이미지 아래에서부터 센 행
            int right = left + width / 2; //사분면 오른쪽
            int top = bottom + height / 2; //사분면 위쪽
            int minX = right, minY = top, maxX = left, maxY = bottom; //불투명 픽셀 경계
            for (int y = bottom; y < top; y++)
            for (int x = left; x < right; x++)
            {
                if (pixels[y * width + x].a <= 8) continue;
                minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
            }
            if (minX >= maxX || minY >= maxY || minX < left + 3 || minY < bottom + 3 || maxX >= right - 3 || maxY >= top - 3)
                throw new InvalidOperationException("포식자 그림에 투명 여백이 부족합니다: " + quadrant);
            return Rect.MinMaxRect(minX - 2, minY - 2, maxX + 3, maxY + 3);
        }
    }
}
