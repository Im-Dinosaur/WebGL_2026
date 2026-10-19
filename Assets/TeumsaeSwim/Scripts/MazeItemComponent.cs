using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class MazeItemComponent : MonoBehaviour
    {
        [SerializeField] private SwimArt art; //진주 표시 자원
        [SerializeField, Min(.05f)] private float pickupRadius = .3f; //플레이어 몸체와 닿을 때 획득하는 진주 반경
        [SerializeField, Min(1)] private int minimumDetour = 6; //진주를 경유할 때 선호하는 추가 이동 칸 수
        [SerializeField, Min(1)] private int endpointClearance = 6; //시작과 출구에서 떨어질 최소 통로 칸 수
        [SerializeField, Min(0f)] private float predatorClearance = 2f; //포식자 시작 위치와의 최소 간격 칸 수
        private Maze maze; //지형 기능 진입점
        private Transform itemRoot; //현재 판의 진주 표시 루트
        private Transform glow; //맥박처럼 커지는 빛 표시
        private float glowTime; //일시정지를 반영한 빛 애니메이션 시간
        public int cell { get; private set; } = -1; //이번 판의 진주 위치
        public bool available { get; private set; } //아직 획득하지 않은 진주 여부

        public void initialize(Maze world) //같은 시드에서는 같은 위치에 진주를 새로 배치
        {
            if (itemRoot != null)
            {
                itemRoot.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(itemRoot.gameObject);
                else DestroyImmediate(itemRoot.gameObject);
            }
            maze = world;
            cell = selectSpawnCell(maze.data);
            available = true;
            glowTime = 0f;
            drawItem();
        }

        public int selectSpawnCell(MazeData data) //도달 가능한 분기에서 안전한 간격을 두고 진주 위치 선택
        {
            int[] fromStart = data.distancesFrom(data.start); //시작에서 모든 칸까지의 통로 거리
            int[] fromExit = data.distancesFrom(data.exit); //출구에서 모든 칸까지의 통로 거리
            int directDistance = fromStart[data.exit]; //진주를 거치지 않은 탈출 거리
            var preferred = new List<int>(); //충분한 우회와 시작 및 출구 간격을 확보한 후보
            var branches = new List<int>(); //짧은 우회만 가능한 작은 미로의 후보
            for (int id = 0; id < data.cells.Length; id++)
            {
                if (data.cells[id].kind != TerrainKind.Water || data.cells[id].safeZone || fromStart[id] < 0 || fromExit[id] < 0) continue;
                int detour = fromStart[id] + fromExit[id] - directDistance; //아이템 수집에 추가로 필요한 이동 거리
                if (detour <= 0 || data.homes.Contains(id)) continue;
                bool nearPredator = false; //포식자 시작 몸체와 겹치거나 바로 옆인 후보
                foreach (int home in data.homes)
                    if (Vector2.Distance(data.center(home), data.center(id)) < predatorClearance * data.tileSize) { nearPredator = true; break; }
                if (nearPredator) continue;
                branches.Add(id);
                if (detour >= minimumDetour && fromStart[id] >= endpointClearance && fromExit[id] >= endpointClearance) preferred.Add(id);
            }
            List<int> candidates = preferred.Count > 0 ? preferred : branches; //연결이 검증된 최종 후보
            if (candidates.Count == 0) throw new InvalidOperationException("도달 가능한 분기에 진주를 배치하지 못했습니다.");
            var random = new System.Random(unchecked(data.seed ^ 0x53a9e217)); //미로 생성 순서에 영향을 주지 않는 아이템 난수
            return candidates[random.Next(candidates.Count)];
        }

        public bool tryCollect(Vector2 position, float playerRadius) //벽에 가리지 않은 진주와 접촉했을 때 한 번만 획득
        {
            if (!available || maze == null || playerRadius <= 0f) return false;
            Vector2 itemPosition = maze.data.center(cell); //실제 획득 판정 위치
            if (Vector2.Distance(position, itemPosition) > pickupRadius + playerRadius || !maze.hasSight(position, itemPosition)) return false;
            available = false;
            itemRoot.gameObject.SetActive(false);
            return true;
        }

        public void tick(float deltaTime) //진주가 남아 있을 때만 게임 시간에 맞춰 빛 표시 갱신
        {
            if (!available || glow == null || deltaTime <= 0f) return;
            glowTime += deltaTime;
            glow.localScale = Vector3.one * (1.15f + Mathf.Sin(glowTime * 3f) * .12f);
        }

        private void drawItem() //미끼와 구분되는 흰 진주와 보랏빛 테두리 표시
        {
            itemRoot = new GameObject("Collectible pearl").transform;
            itemRoot.SetParent(transform, false);
            itemRoot.position = maze.data.center(cell);
            Color pearlColor = new Color32(245, 237, 255, 255); //진주의 밝은 색
            Color glowColor = new Color32(190, 155, 246, 150); //진주 주위의 보랏빛
            glow = art.shape(itemRoot, "Pearl glow", "ring", Vector2.zero, Vector2.one * 1.15f, glowColor, 9).transform;
            art.shape(itemRoot, "Pearl shadow", "circle", new Vector2(.025f, -.03f), Vector2.one * .61f, new Color32(151, 119, 204, 255), 10);
            art.shape(itemRoot, "Pearl", "circle", Vector2.zero, Vector2.one * .51f, pearlColor, 11);
            art.shape(itemRoot, "Pearl highlight", "circle", new Vector2(-.09f, .09f), Vector2.one * .15f, Color.white, 12);
            art.shape(itemRoot, "Pearl sparkle", "box", new Vector2(.38f, .31f), new Vector2(.24f, .045f), pearlColor, 12);
            art.shape(itemRoot, "Pearl sparkle", "box", new Vector2(.38f, .31f), new Vector2(.045f, .24f), pearlColor, 12);
            art.label(itemRoot, "진주", Vector2.down * .6f, .25f, pearlColor);
        }
    }
}
