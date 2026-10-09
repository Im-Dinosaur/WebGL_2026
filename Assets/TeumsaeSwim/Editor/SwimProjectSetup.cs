using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TeumsaeSwim.Editor
{
    public static class SwimProjectSetup
    {
        public const string scenePath = "Assets/TeumsaeSwim/Scenes/TeumsaeSwim.unity"; //게임 전용 씬 경로
        private const string artPath = "Assets/TeumsaeSwim/Art/SwimArt.asset"; //공용 그림 자원 경로
        private const string prefabPath = "Assets/TeumsaeSwim/Prefabs/Predator.prefab"; //포식자 프리팹 경로

        [MenuItem("Teumsae Swim/1. Create game scene")]
        public static void setup() //기존 실습 씬을 건드리지 않고 게임 씬 조립
        {
            AssetDatabase.Refresh();
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/TeumsaeSwim/Fonts/NotoSansKR-Regular.otf"); //배포 가능한 한국어 글꼴
            if (font == null) throw new InvalidOperationException("Noto Sans KR 글꼴이 필요합니다.");
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"); //2D 파이프라인의 조명 없는 재질
            if (shader == null) throw new InvalidOperationException("URP 2D Sprite Unlit 셰이더를 찾을 수 없습니다.");
            const string materialPath = "Assets/TeumsaeSwim/Art/SwimSprite.mat"; //게임 공용 재질 경로
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath); //재사용할 공용 재질
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
            SwimArt art = AssetDatabase.LoadAssetAtPath<SwimArt>(artPath); //그림과 글꼴 공용 자원
            if (art == null) { art = ScriptableObject.CreateInstance<SwimArt>(); AssetDatabase.CreateAsset(art, artPath); }
            link(art, "koreanFont", font);
            link(art, "spriteMaterial", material);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollowComponent)); //게임 카메라
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>(); //카메라 설정
            camera.orthographic = true;
            camera.orthographicSize = 6.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SwimArt.ink;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 100f;
            cameraObject.transform.position = new Vector3(0, 0, -10);
            link(cameraObject.GetComponent<CameraFollowComponent>(), "gameCamera", camera);

            var mazeObject = new GameObject("Maze", typeof(MazeGenerationComponent), typeof(MazeTerrainComponent), typeof(Maze)); //미로 조립
            Maze maze = mazeObject.GetComponent<Maze>(); //미로 진입점
            link(maze, "generation", mazeObject.GetComponent<MazeGenerationComponent>());
            link(maze, "terrain", mazeObject.GetComponent<MazeTerrainComponent>());
            link(mazeObject.GetComponent<MazeTerrainComponent>(), "art", art);

            var playerObject = new GameObject("Player", typeof(PlayerInputComponent), typeof(PlayerMovementComponent), typeof(PlayerInteractionComponent), typeof(PlayerVisualComponent), typeof(Player)); //플레이어 조립
            Player player = playerObject.GetComponent<Player>(); //플레이어 진입점
            link(player, "input", playerObject.GetComponent<PlayerInputComponent>());
            link(player, "movement", playerObject.GetComponent<PlayerMovementComponent>());
            link(player, "interaction", playerObject.GetComponent<PlayerInteractionComponent>());
            link(player, "visual", playerObject.GetComponent<PlayerVisualComponent>());
            link(playerObject.GetComponent<PlayerVisualComponent>(), "art", art);

            var predatorObject = new GameObject("Predator", typeof(PredatorAIComponent), typeof(PredatorMovementComponent), typeof(PredatorVisualComponent), typeof(Predator)); //포식자 조립
            Predator predator = predatorObject.GetComponent<Predator>(); //포식자 진입점
            link(predator, "intelligence", predatorObject.GetComponent<PredatorAIComponent>());
            link(predator, "movement", predatorObject.GetComponent<PredatorMovementComponent>());
            link(predator, "visual", predatorObject.GetComponent<PredatorVisualComponent>());
            link(predatorObject.GetComponent<PredatorVisualComponent>(), "art", art);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(predatorObject, prefabPath); //참조가 연결된 포식자 프리팹
            UnityEngine.Object.DestroyImmediate(predatorObject);

            var gameObject = new GameObject("SwimGame", typeof(GameSessionComponent), typeof(GameUIComponent), typeof(AudioSource), typeof(GameAudioComponent), typeof(SwimGame)); //게임 진입점 조립
            SwimGame game = gameObject.GetComponent<SwimGame>(); //게임 파사드
            AudioSource source = gameObject.GetComponent<AudioSource>(); //효과음 재생 장치
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            link(gameObject.GetComponent<GameUIComponent>(), "art", art);
            link(gameObject.GetComponent<GameAudioComponent>(), "source", source);
            link(game, "session", gameObject.GetComponent<GameSessionComponent>());
            link(game, "maze", maze);
            link(game, "player", player);
            link(game, "predatorPrefab", prefab.GetComponent<Predator>());
            link(game, "cameraFollow", cameraObject.GetComponent<CameraFollowComponent>());
            link(game, "ui", gameObject.GetComponent<GameUIComponent>());
            link(game, "audioComponent", gameObject.GetComponent<GameAudioComponent>());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("TEUMSAE_SETUP_OK: " + scenePath);
        }

        [MenuItem("Teumsae Swim/2. Verify game rules")]
        public static void verify() //핵심 동작 검증 진입점
        {
            SwimVerification.run();
        }

        [MenuItem("Teumsae Swim/3. Build WebGL")]
        public static void build() //별도 하위 주소로 WebGL 빌드
        {
            if (!File.Exists(scenePath)) setup();
            string settingsPath = "ProjectSettings/ProjectSettings.asset"; //기존 사용자 설정을 보존할 파일
            byte[] settings = File.ReadAllBytes(settingsPath); //빌드 전 원본 설정
            string productName = PlayerSettings.productName; //복구할 제품 이름
            string template = PlayerSettings.WebGL.template; //복구할 웹 템플릿
            WebGLCompressionFormat compression = PlayerSettings.WebGL.compressionFormat; //복구할 압축 형식
            bool fallback = PlayerSettings.WebGL.decompressionFallback; //복구할 압축 해제 설정
            int width = PlayerSettings.defaultWebScreenWidth; //복구할 기본 너비
            int height = PlayerSettings.defaultWebScreenHeight; //복구할 기본 높이
            bool background = PlayerSettings.runInBackground; //복구할 백그라운드 설정
            try
            {
                PlayerSettings.productName = "Teumsae Swim";
                PlayerSettings.WebGL.template = "PROJECT:TeumsaeSwim";
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
                PlayerSettings.defaultWebScreenWidth = 480;
                PlayerSettings.defaultWebScreenHeight = 854;
                PlayerSettings.runInBackground = false;
                var options = new BuildPlayerOptions //게임 씬만 포함하는 빌드 설정
                {
                    scenes = new[] { scenePath }, locationPathName = "docs/teumsae-swim", target = BuildTarget.WebGL,
                    options = BuildOptions.None
                };
                BuildReport report = BuildPipeline.BuildPlayer(options); //실제 빌드 보고서
                if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("WebGL build failed: " + report.summary.result);
                Debug.Log("TEUMSAE_BUILD_OK: " + report.summary.totalSize + " bytes, " + report.summary.totalTime);
            }
            finally
            {
                PlayerSettings.productName = productName;
                PlayerSettings.WebGL.template = template;
                PlayerSettings.WebGL.compressionFormat = compression;
                PlayerSettings.WebGL.decompressionFallback = fallback;
                PlayerSettings.defaultWebScreenWidth = width;
                PlayerSettings.defaultWebScreenHeight = height;
                PlayerSettings.runInBackground = background;
                AssetDatabase.SaveAssets();
                File.WriteAllBytes(settingsPath, settings);
            }
        }

        public static void prepareAndVerify() //배치 실행에서 씬 조립과 검증 연결
        {
            setup();
            verify();
        }

        private static void link(UnityEngine.Object target, string property, UnityEngine.Object value) //직렬화 참조를 실제 씬과 자원에 연결
        {
            var serialized = new SerializedObject(target); //직렬화 편집 대상
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
