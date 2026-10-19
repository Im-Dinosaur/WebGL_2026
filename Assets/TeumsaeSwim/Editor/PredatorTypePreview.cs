using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TeumsaeSwim.Editor
{
    public static class PredatorTypePreview
    {
        public static void capture() //실제 씬과 스프라이트의 세로 화면을 저장하지 않고 렌더링 검사
        {
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            SwimGame game = Object.FindFirstObjectByType<SwimGame>(); //실제 게임 파사드
            Player player = Object.FindFirstObjectByType<Player>(); //실제 플레이어
            Maze maze = Object.FindFirstObjectByType<Maze>(); //실제 생성 미로
            Camera camera = Object.FindFirstObjectByType<Camera>(); //기존 2D 카메라
            var target = new RenderTexture(720, 1280, 24); //세로형 검토 화면
            target.Create();
            camera.targetTexture = target;
            try
            {
                typeof(Player).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
                typeof(SwimGame).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
                game.begin(SwimDifficulty.Normal, false);
                camera.rect = new Rect(0, 0, 1, 1);
                camera.orthographicSize = 6.6f / .673f;
                Canvas canvas = game.GetComponentInChildren<Canvas>(); //실제 게임 UI
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>(); //대상 렌더 해상도에 고정할 검토용 배율
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1.5f;
                Predator[] predators = game.GetComponentsInChildren<Predator>().OrderBy(predator => predator.kind).ToArray(); //실제 네 종류
                Vector2 center = predators[0].position; //한 포식자 주변의 실제 게임 화면
                camera.transform.position = new Vector3(center.x, center.y - .95f, -10);
                Canvas.ForceUpdateCanvases();
                save(camera, target, "predator-gameplay.png");
                GameUIComponent ui = game.GetComponent<GameUIComponent>(); //생성된 도움말
                game.GetComponent<GameMenuComponent>().openHelp();
                Canvas.ForceUpdateCanvases();
                save(camera, target, "predator-help.png");
                canvas.gameObject.SetActive(false);
                maze.gameObject.SetActive(false);
                player.gameObject.SetActive(false);
                for (int i = 0; i < predators.Length; i++)
                {
                    Predator predator = predators[i]; //종류별 그림을 확대해 확인할 대상
                    predator.transform.position = new Vector2(i % 2 == 0 ? -1.55f : 1.55f, i < 2 ? 2f : -1.5f);
                    PredatorTypeProfile profile = game.predatorTypes[(int)predator.kind]; //실제 그림과 시야 설정
                    predator.GetComponent<PredatorVisualComponent>().tick(maze, Vector2.right, PredatorState.Patrol, 0, profile.range, 86, 0);
                    predator.transform.Find("Field of view").GetComponent<MeshRenderer>().enabled = false;
                }
                camera.transform.position = new Vector3(0, 0, -10);
                camera.orthographicSize = 5.1f;
                save(camera, target, "predator-sprites-in-unity.png");
                Debug.Log("TEUMSAE_PREDATOR_PREVIEW_OK");
            }
            finally
            {
                camera.targetTexture = null;
                target.Release();
                Object.DestroyImmediate(target);
                EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            }
        }

        private static void save(Camera camera, RenderTexture target, string name) //현재 렌더링 픽셀을 검토용 파일로 저장
        {
            RenderTexture previous = RenderTexture.active; //기존 렌더 대상 복구
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false); //검토 이미지
            try
            {
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("Logs/TeumsaeSwim");
                File.WriteAllBytes("Logs/TeumsaeSwim/" + name, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(pixels);
            }
        }
    }
}
