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

        public static void captureAim() //타일 맵에서 반사 조준과 실제 도착 위치 및 안내 화면 확인
        {
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            SwimGame game = Object.FindFirstObjectByType<SwimGame>(); //실제 게임
            Player player = Object.FindFirstObjectByType<Player>(); //실제 플레이어
            Camera camera = Camera.main; //게임 카메라
            typeof(Player).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
            typeof(SwimGame).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
            Canvas canvas = game.GetComponentInChildren<Canvas>(); //현재 UI
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            try
            {
                game.begin(SwimDifficulty.Normal, false);
                Canvas.ForceUpdateCanvases();
                Maze maze = Object.FindFirstObjectByType<Maze>(); //실제 미끼 비행 대상
                var path = new System.Collections.Generic.List<Vector2>(); //반사가 화면에 보이는 조준 방향 선택
                Vector2 direction = Vector2.zero; //검토에 사용할 사선 반사 방향
                foreach (float angle in new[] { 70f, 110f, 60f, 120f, 45f, 135f, -45f, -135f })
                {
                    Vector2 candidate = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)); //현재 후보 방향
                    if (!maze.predictBait(player.position, candidate, 1, path) || path.Count < 3) continue;
                    Vector2 impact = path[1] - player.position; //충돌 지점의 화면 안 상대 위치
                    if (Mathf.Abs(impact.x) > 4.5f || Mathf.Abs(impact.y) > 5.4f) continue;
                    if (Mathf.Abs(Vector2.Dot((path[1] - path[0]).normalized, (path[2] - path[1]).normalized)) > .95f) continue;
                    direction = candidate;
                    break;
                }
                if (direction == Vector2.zero) throw new System.InvalidOperationException("No visible oblique bounce for aim preview");
                TouchBaitAimComponent control = game.GetComponentInChildren<TouchBaitAimComponent>(); //실제 드래그 입력 대상
                Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(camera, control.transform.position); //버튼 화면 중심
                var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                {
                    pointerId = 33, position = screenCenter,
                    pointerPressRaycast = new UnityEngine.EventSystems.RaycastResult { module = canvas.GetComponent<GraphicRaycaster>(), gameObject = control.gameObject }
                }; //실제 카메라 좌표를 사용하는 검토 포인터
                control.OnPointerDown(pointer);
                pointer.position = RectTransformUtility.WorldToScreenPoint(camera, control.transform.TransformPoint(direction * 90f));
                control.OnDrag(pointer);
                player.tick(0);
                save(game, camera, canvas, "bait-aim-bounce", 720, 1280);
                save(game, camera, canvas, "bait-aim-landscape", 1280, 720);
                player.endBaitAim(true);
                control.resetControl();
                player.tick(0);
                for (int frame = 0; frame < 180 && maze.baits.Count > 0 && !maze.baits[0].settled; frame++) maze.tick(1f / 60f);
                save(game, camera, canvas, "bait-aim-landed", 720, 1280);
                game.GetComponent<GameMenuComponent>().openHelp();
                save(game, camera, canvas, "bait-aim-help", 720, 1280);
                Debug.Log("SUMBAKKOGI_BAIT_AIM_PREVIEW_OK");
            }
            finally { EditorSceneManager.OpenScene(SwimProjectSetup.scenePath); }
        }

        public static void captureTerrain() //새 타일의 시작 구간과 그물 및 풀숲을 실제 모바일 화면으로 확인
        {
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            SwimGame game = Object.FindFirstObjectByType<SwimGame>(); //실제 게임
            Player player = Object.FindFirstObjectByType<Player>(); //실제 플레이어
            Camera camera = Camera.main; //게임 카메라
            typeof(Player).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
            typeof(SwimGame).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
            Canvas canvas = game.GetComponentInChildren<Canvas>(); //실제 UI
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            try
            {
                game.begin(SwimDifficulty.Normal, false);
                save(game, camera, canvas, "tile-start", 720, 1280);
                Maze maze = Object.FindFirstObjectByType<Maze>(); //이번 랜덤 지형
                int net = System.Array.FindIndex(maze.data.cells, cell => cell.kind == TerrainKind.Net); //그물 위치
                Vector2 approach = maze.data.cells[net].horizontal ? Vector2.left : Vector2.down; //통로의 접근 방향
                player.transform.position = maze.data.center(net) + approach * .8f;
                save(game, camera, canvas, "tile-net-closed", 720, 1280);
                maze.workNet(net, 5);
                save(game, camera, canvas, "tile-net-open", 720, 1280);
                int grass = System.Array.FindIndex(maze.data.cells, cell => cell.kind == TerrainKind.Grass); //은신 구간
                player.transform.position = maze.data.center(grass);
                save(game, camera, canvas, "tile-grass", 720, 1280);
                int gap = System.Array.FindIndex(maze.data.cells, cell => cell.kind == TerrainKind.Gap); //좁은 통로
                approach = maze.data.cells[gap].horizontal ? Vector2.left : Vector2.down;
                player.transform.position = maze.data.center(gap) + approach * 1.4f;
                save(game, camera, canvas, "tile-gap", 720, 1280);
                save(game, camera, canvas, "tile-landscape", 1280, 720);
                Debug.Log("SUMBAKKOGI_TILE_PREVIEW_OK");
            }
            finally { EditorSceneManager.OpenScene(SwimProjectSetup.scenePath); }
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
