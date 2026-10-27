using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TeumsaeSwim.Editor
{
    public static class SwimUISetup
    {
        [MenuItem("Teumsae Swim/7. Connect Sumbakkogi UI")]
        public static void connect() //기존 씬의 동작과 설정을 유지하며 새 UI 자원과 구성 요소 연결
        {
            AssetDatabase.Refresh();
            Sprite ocean = importSprite("OceanHome", new Vector2(.5f, .5f)); //홈 배경
            Sprite fish = importSprite("PlayerFish", new Vector2(.64f, .5f)); //몸통 중심 물고기
            Sprite logo = importSprite("SumbakkogiLogo", new Vector2(.5f, .5f)); //단독 제목 로고
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            SwimGame game = Object.FindFirstObjectByType<SwimGame>(); //기존 게임 진입점
            GameUIStyleComponent style = ensure<GameUIStyleComponent>(game.gameObject); //공통 UI 스타일
            GameHUDComponent hud = ensure<GameHUDComponent>(game.gameObject); //실시간 조작 UI
            GameMenuComponent menus = ensure<GameMenuComponent>(game.gameObject); //메뉴 UI
            GameUIComponent ui = game.GetComponent<GameUIComponent>(); //UI 조율 진입점
            link(style, "oceanBackground", ocean);
            link(style, "titleLogo", logo);
            link(style, "playerFish", fish);
            link(ui, "style", style);
            link(ui, "hud", hud);
            link(ui, "menus", menus);
            link(Object.FindFirstObjectByType<PlayerVisualComponent>(), "fishSprite", fish);
            connectAimToPlayer();
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("SUMBAKKOGI_UI_CONNECTED");
        }

        [MenuItem("Teumsae Swim/9. Connect bait aiming")]
        public static void connectAim() //기존 타일과 UI 자원을 다시 가져오지 않고 조준 구성만 연결
        {
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            connectAimToPlayer();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("SUMBAKKOGI_BAIT_AIM_CONNECTED");
        }

        private static void connectAimToPlayer() //플레이어 진입점과 조준 그림의 Inspector 참조 연결
        {
            Player player = Object.FindFirstObjectByType<Player>(); //기존 플레이어
            PlayerBaitAimComponent aim = ensure<PlayerBaitAimComponent>(player.gameObject); //조준 경로 표시 구성 요소
            link(aim, "art", AssetDatabase.LoadAssetAtPath<SwimArt>("Assets/TeumsaeSwim/Art/SwimArt.asset"));
            link(player, "baitAim", aim);
        }

        public static void connectAndVerify() //한 번의 배치 실행에서 연결과 기능 검증
        {
            connect();
            SwimVerification.run();
            SwimUIVerification.run();
        }

        private static T ensure<T>(GameObject target) where T : Component //이미 있는 구성 요소 재사용
        {
            T component = target.GetComponent<T>(); //기존 연결
            return component != null ? component : target.AddComponent<T>();
        }

        private static Sprite importSprite(string name, Vector2 pivot) //투명도와 해상도를 유지하는 UI 그림 가져오기
        {
            string path = "Assets/TeumsaeSwim/Art/UI/" + name + ".png"; //그림 경로
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path); //Unity 가져오기 설정
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            var settings = new TextureImporterSettings(); //그림 원본의 사각 영역과 몸통 기준점
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void link(Object target, string name, Object value) //Inspector 참조만 갱신
        {
            var serialized = new SerializedObject(target); //기존 다른 설정을 보존할 직렬화 대상
            serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
