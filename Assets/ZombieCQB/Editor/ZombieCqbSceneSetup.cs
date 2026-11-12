using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;
using static ZombieCqb.ZombieCqbSceneArt;

namespace ZombieCqb
{
    internal static class ZombieCqbSceneSetup
    {
        internal const string root = "Assets/ZombieCQB"; //새 게임의 자산 경로
        internal const string scenePath = root + "/Scenes/ZombieCQB.unity"; //전용 실행 씬
        private const string stageFolder = root + "/Stages"; //교체할 스테이지 프리팹 위치

        [MenuItem("Zombie CQB/Create Prototype Scene")]
        public static void createPrototype() //전용 씬과 두 개의 플레이 가능 맵 생성
        {
            if (File.Exists(scenePath))
                throw new InvalidOperationException("Zombie CQB scene already exists.");

            Directory.CreateDirectory(root + "/Scenes");
            Directory.CreateDirectory(stageFolder);
            AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Sprite solid = pixel(); //단색 표시 그림
            Sprite playerSprite = actor("Player", rgb(73, 134, 83), rgb(151, 207, 114), true, false); //최초 감염원의 외형
            Sprite zombie = actor("Zombie", rgb(68, 102, 79), rgb(110, 169, 107), true, false); //감염된 시민 외형
            Sprite orangeCivilian = actor("CivilianAmber", rgb(222, 155, 84), rgb(233, 190, 137), false, false); //주황 옷의 시민
            Sprite tealCivilian = actor("CivilianTeal", rgb(112, 191, 178), rgb(222, 175, 123), false, false); //청록 옷의 시민
            Sprite guardSprite = actor("Guard", rgb(105, 138, 158), rgb(66, 94, 113), false, true); //경비 외형
            Sprite doorSprite = door(); //닫힌 문 외형
            Sprite hearingSprite = marker(); //발소리 방향 외형
            Sprite liquidSprite = liquid(); //액체 감염 외형
            Sprite fanSprite = fan(); //전방 범위 감염 외형

            StageMapComponent[] stages = new StageMapComponent[2]; //완성한 순서대로 재생할 맵
            for (int i = 0; i < stages.Length; i++) //생성할 스테이지의 번호
                stages[i] = createStage(i, solid, zombie, orangeCivilian, tealCivilian, guardSprite, doorSprite);

            GameObject system = new GameObject("Zombie CQB"); //게임 흐름과 상태의 루트
            StageFlowComponent flow = system.AddComponent<StageFlowComponent>(); //스테이지 진행
            UpgradeProgressComponent progress = system.AddComponent<UpgradeProgressComponent>(); //경험치와 강화
            InfectionComponent infection = system.AddComponent<InfectionComponent>(); //감염 판정
            SpriteRenderer flash = createSprite("Droplet Flash", system.transform, fanSprite, Vector2.zero, 8); //전방 감염 효과 표시
            flash.enabled = false;
            setReferences(flow, "stages", stages);
            setReference(infection, "liquidSprite", liquidSprite);
            setReference(infection, "dropletFlash", flash);
            Player player = createPlayer(playerSprite, hearingSprite); //플레이어 객체와 담당 컴포넌트
            Camera camera = createCamera(); //맵을 위에서 보는 카메라
            ZombieCqbUIComponent ui = ZombieCqbSceneUI.create(playerSprite, orangeCivilian, guardSprite); //플레이와 강화 화면

            ZombieCqbGame game = system.AddComponent<ZombieCqbGame>(); //각 기능을 조율하는 게임 진입점
            setReference(game, "player", player);
            setReference(game, "flow", flow);
            setReference(game, "progress", progress);
            setReference(game, "infection", infection);
            setReference(game, "gameCamera", camera);
            setReference(game, "ui", ui);

            Light2D light = new GameObject("Ambient Light").AddComponent<Light2D>(); //2D 그림을 비추는 전체 조명
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            GameObject events = new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule)); //강화 선택과 화면 버튼 입력
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath))
                throw new InvalidOperationException("Could not save Zombie CQB scene.");

            AssetDatabase.SaveAssets();
            Debug.Log("ZOMBIE_CQB_SCENE_OK: " + scenePath);
        }

        private static StageMapComponent createStage(int index, Sprite solid, Sprite zombie, Sprite amber, Sprite teal, Sprite guardSprite, Sprite doorSprite) //스테이지 그림과 배치 프리팹 구성
        {
            Layout layout = createLayout(index); //맵의 격자와 캐릭터 배치
            GameObject stage = new GameObject(layout.assetName); //프리팹이 될 맵 루트
            StageMapComponent map = stage.AddComponent<StageMapComponent>(); //맵에서 사용할 참조
            IndoorNavigationComponent navigation = stage.AddComponent<IndoorNavigationComponent>(); //이동 및 시야에 사용하는 격자
            Sprite floor = ZombieCqbSceneArt.map(layout.assetName + "Floor", layout.rows, layout.spawn, layout.exit, index == 1); //바닥과 장애물 그림
            createSprite("Facility", stage.transform, floor, new Vector2(layout.rows[0].Length * 0.5f, layout.rows.Length * 0.5f), -12);

            Transform entrance = createNode("Spawn", stage.transform, center(layout.spawn)); //플레이어 시작점
            Transform exit = createNode("Exit", stage.transform, center(layout.exit)); //플레이어 탈출 지점
            SpriteRenderer fog = createSprite("Sight Overlay", stage.transform, null, Vector2.zero, 12); //현재 시야 표시
            Transform doorsRoot = createNode("Doors", stage.transform, Vector2.zero); //맵에 배치된 문의 루트
            List<DoorComponent> doors = new List<DoorComponent>(); //격자에 연결할 문 목록
            for (int y = 0; y < layout.rows.Length; y++) //문을 확인할 격자의 행
            {
                for (int x = 0; x < layout.rows[y].Length; x++) //문을 확인할 격자의 열
                {
                    char tile = layout.rows[y][x]; //현재 셀의 종류
                    if (tile != 'D' && tile != 'd')
                        continue;

                    Vector2 position = center(new Vector2Int(x, y)); //문을 설치할 위치
                    Transform node = createNode($"Door {x},{y}", doorsRoot, position); //상호작용할 문 오브젝트
                    DoorComponent door = node.gameObject.AddComponent<DoorComponent>(); //문 상태
                    SpriteRenderer leaf = createSprite("Door Leaf", node, doorSprite, position, 13); //닫힌 상태의 문짝
                    setReference(door, "leaf", leaf);
                    setValue(door, "startsOpen", property => property.boolValue = tile == 'd');
                    leaf.enabled = tile == 'D';
                    doors.Add(door);
                }
            }

            Transform civiliansRoot = createNode("Civilians", stage.transform, Vector2.zero); //시민들을 관리하는 루트
            CivilianComponent[] civilians = new CivilianComponent[layout.civilians.Length]; //시민 참조 배열
            for (int i = 0; i < civilians.Length; i++) //맵에 배치할 시민 번호
            {
                Vector2 position = center(layout.civilians[i]); //시민의 초기 좌표
                Transform node = createNode($"Civilian {i + 1:00}", civiliansRoot, position); //시민 오브젝트
                CivilianComponent civilian = node.gameObject.AddComponent<CivilianComponent>(); //시민의 행동과 감염 상태
                SpriteRenderer body = createSprite("Body", node, i % 2 == 0 ? amber : teal, position, 20); //시민의 외형
                SpriteRenderer meter = createSprite("Infection Meter", node, solid, position + new Vector2(0f, 0.48f), 27); //시민의 감염 진행 표시
                meter.color = rgb(170, 246, 150);
                meter.enabled = false;
                setReference(civilian, "body", body);
                setReference(civilian, "infectedSprite", zombie);
                setReference(civilian, "infectionMeter", meter);
                civilians[i] = civilian;
            }

            Transform guardsRoot = createNode("Security", stage.transform, Vector2.zero); //경비들을 관리하는 루트
            GuardComponent[] guards = new GuardComponent[layout.patrols.Length]; //경비 참조 배열
            for (int i = 0; i < guards.Length; i++) //맵에 배치할 경비 번호
            {
                Vector2Int[] cells = layout.patrols[i]; //경비의 순찰 격자 경로
                Vector2 position = center(cells[0]); //경비의 초기 좌표
                Transform node = createNode($"Guard {i + 1:00}", guardsRoot, position); //경비 오브젝트
                GuardComponent guard = node.gameObject.AddComponent<GuardComponent>(); //경비의 행동과 공격
                SpriteRenderer body = createSprite("Body", node, guardSprite, position, 22); //경비의 외형
                SpriteRenderer flash = createSprite("Attack Flash", node, solid, position, 24); //피격 방향 표시
                flash.color = rgb(255, 165, 110, 188);
                flash.enabled = false;
                setReference(guard, "body", body);
                setReference(guard, "attackFlash", flash);
                setValue(guard, "patrolPoints", property =>
                {
                    property.arraySize = cells.Length;
                    for (int point = 0; point < cells.Length; point++) //직렬화할 경비 순찰 위치
                        property.GetArrayElementAtIndex(point).vector2Value = center(cells[point]);
                });
                setValue(guard, "attackDamage", property => property.floatValue = index == 0 ? 13f : 17f);
                guards[i] = guard;
            }

            setValue(navigation, "rows", property =>
            {
                property.arraySize = layout.rows.Length;
                for (int i = 0; i < layout.rows.Length; i++) //직렬화할 지형의 행
                    property.GetArrayElementAtIndex(i).stringValue = layout.rows[i];
            });
            setReferences(navigation, "doors", doors.ToArray());
            setValue(map, "stageName", property => property.stringValue = layout.name);
            setReference(map, "spawn", entrance);
            setReference(map, "exit", exit);
            setReference(map, "navigationComponent", navigation);
            setReference(map, "fog", fog);
            setReferences(map, "doors", doors.ToArray());
            setReferences(map, "civilians", civilians);
            setReferences(map, "guards", guards);
            string path = $"{stageFolder}/{layout.assetName}.prefab"; //완성한 맵 프리팹 저장 경로
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(stage, path); //씬에서 불러올 맵 프리팹
            Object.DestroyImmediate(stage);
            if (prefab == null)
                throw new InvalidOperationException("Could not save " + path);

            return prefab.GetComponent<StageMapComponent>();
        }

        private static Player createPlayer(Sprite playerSprite, Sprite hearingSprite) //플레이어와 조작, 이동, 능력치의 참조 구성
        {
            GameObject root = new GameObject("Patient Zero"); //최초 감염원의 루트 오브젝트
            Player player = root.AddComponent<Player>(); //플레이어의 진입점
            PlayerInputComponent input = root.AddComponent<PlayerInputComponent>(); //키보드 및 마우스 조작
            PlayerMovementComponent movement = root.AddComponent<PlayerMovementComponent>(); //이동과 충돌
            PlayerStatComponent stats = root.AddComponent<PlayerStatComponent>(); //체력과 능력치
            PlayerHearingComponent hearing = root.AddComponent<PlayerHearingComponent>(); //발소리 방향 표시
            PlayerSpecialAbilityComponent special = root.AddComponent<PlayerSpecialAbilityComponent>(); //해금한 특수능력
            SpriteRenderer body = createSprite("Body", root.transform, playerSprite, Vector2.zero, 28); //플레이어 외형
            body.transform.localScale = Vector3.one * 1.13f;
            SpriteRenderer[] indicators = new SpriteRenderer[5]; //동시에 표시할 발소리 방향
            for (int i = 0; i < indicators.Length; i++) //추가할 방향 표시
            {
                indicators[i] = createSprite($"Sound {i + 1}", root.transform, hearingSprite, Vector2.zero, 30);
                indicators[i].transform.localScale = Vector3.one * 0.47f;
                indicators[i].enabled = false;
            }

            setReference(player, "input", input);
            setReference(player, "movement", movement);
            setReference(player, "stats", stats);
            setReference(player, "hearing", hearing);
            setReference(player, "special", special);
            setReference(player, "body", body);
            setReference(movement, "spriteBody", body.transform);
            setReferences(hearing, "indicators", indicators);
            return player;
        }

        private static Camera createCamera() //탑다운 전용 카메라 설정
        {
            GameObject view = new GameObject("Main Camera", typeof(Camera)); //게임 화면을 그리는 카메라
            view.tag = "MainCamera";
            Camera camera = view.GetComponent<Camera>(); //탑다운 카메라 컴포넌트
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = rgb(11, 17, 22);
            return camera;
        }

        private static Layout createLayout(int index) //진료소와 통제실의 방, 복도, 배치 정의
        {
            bool second = index == 1; //통제실 스테이지 여부
            int width = second ? 31 : 29; //맵 가로 크기
            int height = second ? 19 : 17; //맵 세로 크기
            int lower = second ? 6 : 5; //아래쪽 방의 가로 벽
            int upper = second ? 12 : 11; //위쪽 방의 가로 벽
            char[][] tiles = new char[height][]; //맵 격자 편집 공간
            for (int y = 0; y < height; y++) //맵 벽을 채울 행
            {
                tiles[y] = new char[width];
                for (int x = 0; x < width; x++) //맵 벽을 채울 열
                {
                    bool border = x == 0 || x == width - 1 || y == 0 || y == height - 1; //맵 외벽
                    bool partitions = y < lower ? x == 9 || x == (second ? 21 : 19) : y > upper && (x == (second ? 10 : 9) || x == (second ? 20 : 19)); //방 사이의 칸막이
                    tiles[y][x] = border || partitions || y == lower || y == upper ? '#' : '.';
                }
            }

            Vector2Int[] doorCells = second ? points(5, 6, 15, 6, 26, 6, 6, 12, 16, 12, 24, 12) : points(4, 5, 14, 5, 24, 5, 5, 11, 14, 11, 24, 11); //방으로 이어지는 문 위치
            for (int i = 0; i < doorCells.Length; i++) //열린 문과 닫힌 문 배치
                tiles[doorCells[i].y][doorCells[i].x] = i == 0 || i == 4 ? 'd' : 'D';

            Vector2Int[] obstacles = second //맵마다 다르게 배치한 실내 장애물
                ? points(3, 2, 4, 2, 7, 4, 13, 2, 18, 3, 19, 3, 24, 2, 27, 4, 11, 7, 11, 8, 21, 10, 21, 11, 3, 15, 4, 15, 13, 16, 14, 16, 25, 15, 26, 15)
                : points(2, 2, 6, 2, 12, 2, 16, 2, 23, 3, 26, 3, 10, 6, 10, 7, 19, 9, 19, 10, 3, 13, 7, 14, 12, 14, 16, 14, 22, 14, 25, 14);
            foreach (Vector2Int cell in obstacles) //벽처럼 이동을 막는 가구 위치
                tiles[cell.y][cell.x] = 'B';

            for (int y = 1; y < height - 1; y++) //바닥 표식을 배치할 행
                for (int x = 1; x < width - 1; x++) //바닥 표식을 배치할 열
                    if (tiles[y][x] == '.' && (x * 19 + y * 13) % 31 == 0)
                        tiles[y][x] = 'X';

            string[] rows = Array.ConvertAll(tiles, row => new string(row)); //런타임에서 사용할 지형 문자열
            return new Layout
            {
                name = second ? "격리 통제실" : "폐쇄 진료소",
                assetName = second ? "Stage02Control" : "Stage01Clinic",
                rows = rows,
                spawn = second ? new Vector2Int(3, 9) : new Vector2Int(3, 8),
                exit = second ? new Vector2Int(28, 10) : new Vector2Int(26, 8),
                civilians = second
                    ? points(4, 4, 7, 2, 12, 4, 17, 4, 25, 4, 28, 2, 4, 14, 8, 16, 15, 14, 18, 16, 24, 14, 28, 16, 8, 9, 18, 8)
                    : points(5, 3, 7, 2, 13, 3, 17, 3, 23, 2, 26, 2, 4, 13, 8, 14, 13, 13, 17, 13, 24, 13, 8, 8),
                patrols = second
                    ? new[] { points(10, 10, 6, 10, 10, 9), points(17, 9, 23, 8, 18, 7), points(26, 11, 27, 9, 23, 9) }
                    : new[] { points(12, 9, 7, 9, 11, 8), points(22, 7, 25, 9, 20, 8) }
            };
        }

        private static Vector2Int[] points(params int[] xy) //편집기 좌표 목록을 격자 위치 배열로 변환
        {
            Vector2Int[] cells = new Vector2Int[xy.Length / 2]; //변환할 격자 위치 목록
            for (int i = 0; i < cells.Length; i++) //변환할 좌표 쌍
                cells[i] = new Vector2Int(xy[i * 2], xy[i * 2 + 1]);
            return cells;
        }

        private static Vector2 center(Vector2Int cell) //격자 위치를 월드 좌표로 변환
        {
            return IndoorNavigationComponent.cellCenter(cell);
        }

        internal static Transform createNode(string name, Transform parent, Vector2 position) //월드 오브젝트를 지정 위치에 배치
        {
            Transform node = new GameObject(name).transform; //배치할 오브젝트
            node.SetParent(parent, false);
            node.position = position;
            return node;
        }

        internal static SpriteRenderer createSprite(string name, Transform parent, Sprite sprite, Vector2 position, int order) //씬의 스프라이트와 표시 순서 생성
        {
            SpriteRenderer renderer = createNode(name, parent, position).gameObject.AddComponent<SpriteRenderer>(); //표시할 스프라이트 렌더러
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        internal static void setReference(Object target, string field, Object value) //Inspector 참조를 직렬화
        {
            setValue(target, field, property => property.objectReferenceValue = value);
        }

        internal static void setReferences(Object target, string field, Object[] values) //Inspector의 여러 참조를 직렬화
        {
            setValue(target, field, property =>
            {
                property.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++) //직렬화할 참조 항목
                    property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            });
        }

        private static void setValue(Object target, string field, Action<SerializedProperty> write) //직렬화 필드에 배치 설정 저장
        {
            SerializedObject serialized = new SerializedObject(target); //편집할 오브젝트의 직렬화 표현
            SerializedProperty property = serialized.FindProperty(field); //현재 저장할 필드
            if (property == null)
                throw new InvalidOperationException($"{target.GetType().Name}.{field} is missing.");
            write(property);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class Layout
        {
            public string name; //화면에 표시할 이름
            public string assetName; //프리팹과 그림의 파일 이름
            public string[] rows; //맵 지형 행
            public Vector2Int spawn; //플레이어의 시작 셀
            public Vector2Int exit; //탈출 셀
            public Vector2Int[] civilians; //시민의 초기 셀
            public Vector2Int[][] patrols; //경비의 순찰 셀 목록
        }
    }
}
