using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TeumsaeSwim.Editor
{
    public static class SwimRefreshSetup
    {
        public static void connect() //승인한 홈 그림과 보급 및 포식자 설정만 기존 씬에 연결
        {
            AssetDatabase.Refresh();
            Sprite background = importSprite("OceanHomeRefresh"); //새 홈 전용 배경
            Sprite logo = importSprite("SumbakkogiLogoRefresh"); //크림과 민트 제목
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            Maze maze = Object.FindFirstObjectByType<Maze>(); //기존 지형 진입점
            MazeBaitSupplyComponent supply = maze.GetComponent<MazeBaitSupplyComponent>(); //재실행 시 기존 구성 요소 재사용
            if (supply == null) supply = maze.gameObject.AddComponent<MazeBaitSupplyComponent>();
            setReference(supply, "art", AssetDatabase.LoadAssetAtPath<SwimArt>("Assets/TeumsaeSwim/Art/SwimArt.asset"));
            setReference(maze, "supplies", supply);
            GameUIStyleComponent style = Object.FindFirstObjectByType<GameUIStyleComponent>(); //기존 UI 스타일
            setReference(style, "homeBackground", background);
            setReference(style, "titleLogo", logo);
            var generation = new SerializedObject(maze.GetComponent<MazeGenerationComponent>()); //실제 씬에 저장된 난이도 값
            generation.FindProperty("practicePredators").intValue = 3;
            generation.FindProperty("normalPredators").intValue = 6;
            generation.FindProperty("challengePredators").intValue = 9;
            generation.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(maze.gameObject.scene);
            EditorSceneManager.SaveScene(maze.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("SUMBAKKOGI_REFRESH_CONNECTED");
        }

        public static void connectAndBuild() //연결 후 영향 범위 검증과 실제 UI 확인 및 WebGL 빌드
        {
            connect();
            verifyAndBuild();
        }

        public static void verifyAndBuild() //이미 연결한 씬에서 영향 범위 검증과 WebGL 빌드
        {
            SwimVerification.run();
            SwimUIVerification.run();
            BaitAimVerification.run();
            SwimRefreshVerification.run();
            SwimUIPreview.capture();
            SwimProjectSetup.build();
        }

        private static Sprite importSprite(string name) //모바일 표시 크기에 맞는 새 그림만 가져오기
        {
            string path = "Assets/TeumsaeSwim/Art/UI/" + name + ".png"; //자원 위치
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); //이미지 가져오기 설정
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void setReference(Object target, string field, Object value) //다른 Inspector 값을 보존하며 참조 연결
        {
            var settings = new SerializedObject(target); //변경할 직렬화 대상
            settings.FindProperty(field).objectReferenceValue = value;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
