using System.IO;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TeumsaeSwim.Editor
{
    public static class SwimUIPreview
    {
        public static void captureAndVerify() //화면 출력이 필요한 터치 배치 검사와 동작 검증 실행
        {
            SwimUIVerification.run();
            capture();
        }

        public static void capture() //실제 UI 화면과 여러 비율을 씬 저장 없이 렌더링
        {
            captureScreens(false);
        }

        public static void captureNet() //수정한 그물 안내만 다시 렌더링
        {
            captureScreens(true);
        }

        private static void captureScreens(bool netOnly) //필요한 화면 상태만 생성하고 검토
        {
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            SwimGame game = Object.FindFirstObjectByType<SwimGame>(); //실제 게임
            Player player = Object.FindFirstObjectByType<Player>(); //실제 플레이어
            Camera camera = Camera.main; //게임 카메라
            typeof(Player).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
            typeof(SwimGame).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
            Canvas canvas = game.GetComponentInChildren<Canvas>(); //실제 UI 캔버스
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            try
            {
                if (!netOnly)
                {
                save(game, camera, canvas, "main", 720, 1280);
                game.GetComponent<GameMenuComponent>().openHelp();
                save(game, camera, canvas, "help", 720, 1280);
                game.GetComponent<GameMenuComponent>().showHelpPage(true);
                save(game, camera, canvas, "predators", 720, 1280);
                game.begin(SwimDifficulty.Normal, false);
                save(game, camera, canvas, "gameplay", 720, 1280);
                save(game, camera, canvas, "phone-short", 640, 960);
                save(game, camera, canvas, "phone-tall", 780, 1688, new Rect(0, 68, 780, 1526));
                save(game, camera, canvas, "landscape", 1280, 720);
                }
                else game.begin(SwimDifficulty.Normal, false);
                Maze maze = Object.FindFirstObjectByType<Maze>(); //그물 안내가 표시되는 실제 지형
                for (int id = 0; id < maze.data.cells.Length; id++)
                {
                    if (maze.data.cells[id].kind != TerrainKind.Net) continue;
                    Vector2 approach = maze.data.cells[id].horizontal ? Vector2.left : Vector2.down; //그물 가까이 접근할 방향
                    player.transform.position = maze.data.center(id) + approach * .8f;
                    player.GetComponent<PlayerInteractionComponent>().tick(player.position, Vector2.zero, Vector2.right, true, false, .8f);
                    break;
                }
                game.GetComponent<GameUIComponent>().tick(0);
                save(game, camera, canvas, "net-release", 720, 1280);
                if (netOnly) { Debug.Log("SUMBAKKOGI_NET_PREVIEW_OK"); return; }
                game.pause();
                save(game, camera, canvas, "pause", 720, 1280);
                game.resume();
                game.GetComponent<GameSessionComponent>().advance(84.2f, false, true, true);
                game.GetComponent<GameUIComponent>().showPhase();
                save(game, camera, canvas, "win", 720, 1280);
                game.begin(SwimDifficulty.Practice, false);
                game.GetComponent<GameSessionComponent>().advance(51.4f, true, false, true);
                game.GetComponent<GameUIComponent>().showPhase();
                save(game, camera, canvas, "loss", 720, 1280);
                Debug.Log("SUMBAKKOGI_UI_PREVIEW_OK");
            }
            finally { EditorSceneManager.OpenScene(SwimProjectSetup.scenePath); }
        }

        private static void save(SwimGame game, Camera camera, Canvas canvas, string name, int width, int height, Rect? safe = null) //대상 화면 비율에 맞춘 실제 렌더 픽셀 저장
        {
            var target = new RenderTexture(width, height, 24); //검토 해상도
            target.Create();
            RenderTexture previous = RenderTexture.active; //기존 렌더 대상
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false); //검토 이미지
            try
            {
                camera.targetTexture = target;
                camera.rect = new Rect(0, 0, 1, 1);
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>(); //검토용 캔버스 배율
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = width / 480f;
                Canvas.ForceUpdateCanvases();
                GameUIStyleComponent style = game.GetComponent<GameUIStyleComponent>(); //실제 안전 영역 계산
                style.layout(new Vector2(width, height), safe ?? new Rect(0, 0, width, height));
                if (game.phase != SwimPhase.Home)
                {
                    CameraFollowComponent follow = camera.GetComponent<CameraFollowComponent>(); //UI에 맞춘 플레이 영역
                    follow.setPlayfield(style.playfield);
                    follow.snap();
                    game.GetComponent<GameUIComponent>().tick(0);
                }
                Canvas.ForceUpdateCanvases();
                foreach (GameUIIconComponent icon in canvas.GetComponentsInChildren<GameUIIconComponent>())
                {
                    Mesh mesh = icon.canvasRenderer.GetMesh(); //실제로 등록된 아이콘 메시
                    if (mesh == null || mesh.vertexCount == 0) throw new System.InvalidOperationException("Empty icon mesh: " + icon.name);
                }
                camera.Render();
                if (name == "net-release")
                {
                    Transform callout = canvas.transform.Find("Safe area/HUD/Net callout"); //실제 그물 안내 위치
                    Vector2 calloutScreen = RectTransformUtility.WorldToScreenPoint(camera, callout.position); //렌더 화면의 안내 중심
                    Vector2 playerScreen = camera.WorldToScreenPoint(Object.FindFirstObjectByType<Player>().position); //렌더 화면의 플레이어
                    if (Mathf.Abs(calloutScreen.x - playerScreen.x) > 5 || calloutScreen.y <= playerScreen.y)
                        throw new System.InvalidOperationException("Net callout is not above the player");
                }
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>(); //실제 UI 충돌 검사
                UnityEngine.EventSystems.EventSystem events = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>(); //실제 이벤트 설정
                Transform frontScreen = null; //모달 뒤의 버튼은 의도적으로 입력 차단
                foreach (Transform screen in style.safe) if (screen.gameObject.activeInHierarchy) frontScreen = screen;
                foreach (Button button in frontScreen.GetComponentsInChildren<Button>())
                {
                    if (!button.interactable) continue;
                    var pointer = new UnityEngine.EventSystems.PointerEventData(events) { position = RectTransformUtility.WorldToScreenPoint(camera, button.transform.position) }; //렌더된 버튼의 중심
                    var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>(); //위에서부터 맞는 UI
                    raycaster.Raycast(pointer, hits);
                    if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<Button>() != button)
                        throw new System.InvalidOperationException("UI raycast blocked: " + name + "/" + button.name + ", hits=" + hits.Count);
                }
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("output/ui-implemented");
                File.WriteAllBytes("output/ui-implemented/" + name + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
            }
        }
    }
}
