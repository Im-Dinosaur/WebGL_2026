using System.Collections.Generic;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class MazeBaitSupplyComponent : MonoBehaviour
    {
        [SerializeField] private SwimArt art; //필드 미끼 표시 자원
        [SerializeField, Min(1)] private int practiceSupplies = 2; //연습 보급 수
        [SerializeField, Min(1)] private int normalSupplies = 3; //기본 보급 수
        [SerializeField, Min(1)] private int challengeSupplies = 4; //도전 보급 수
        [SerializeField, Min(1f)] private float respawnSeconds = 30f; //획득 후 다시 생기는 게임 시간
        [SerializeField, Min(.05f)] private float pickupRadius = .32f; //미끼 접촉 반경
        private readonly List<BaitSupplyData> supplies = new List<BaitSupplyData>(); //이번 판 보급 상태
        private readonly List<int> candidates = new List<int>(); //도달 가능한 물길 후보
        private Maze maze; //지형 진입점
        private System.Random random; //미로와 분리한 재현 가능한 난수
        private Transform supplyRoot; //이번 판 표시 루트
        private float animationTime; //일시정지를 반영하는 장식 시간
        public IReadOnlyList<BaitSupplyData> fieldSupplies => supplies; //필드 보급 상태 조회

        public void initialize(Maze world, SwimDifficulty difficulty, int pearlCell) //기존 보급과 타이머를 지우고 도달 가능한 위치에 새 보급 배치
        {
            if (supplyRoot != null)
            {
                supplyRoot.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(supplyRoot.gameObject);
                else DestroyImmediate(supplyRoot.gameObject);
            }
            maze = world;
            random = new System.Random(unchecked(maze.data.seed ^ 0x246ba18));
            supplies.Clear();
            candidates.Clear();
            animationTime = 0f;
            supplyRoot = new GameObject("Field bait supplies").transform;
            supplyRoot.SetParent(transform, false);
            int[] fromStart = maze.data.distancesFrom(maze.data.start); //분리되거나 안전구역에 가까운 후보 제외
            int[] fromExit = maze.data.distancesFrom(maze.data.exit); //출구 쪽 여유 거리
            for (int id = 0; id < maze.data.cells.Length; id++)
            {
                if (maze.data.cells[id].kind != TerrainKind.Water || maze.data.cells[id].safeZone || id == pearlCell || fromStart[id] < 6 || fromExit[id] < 6) continue;
                bool clear = true; //포식자 시작 몸체와의 간격
                foreach (int home in maze.data.homes)
                    if (Vector2.Distance(maze.data.center(id), maze.data.center(home)) < maze.data.tileSize * 2f) { clear = false; break; }
                if (clear) candidates.Add(id);
            }
            int count = difficulty == SwimDifficulty.Practice ? practiceSupplies : difficulty == SwimDifficulty.Normal ? normalSupplies : challengeSupplies; //난이도별 필드 보급 수
            for (int i = 0; i < count; i++)
            {
                var supply = new BaitSupplyData(); //수집 전후 상태를 유지할 보급
                supplies.Add(supply);
                supply.view = drawSupply(i);
                respawn(supply, null);
            }
        }

        public bool tryCollect(Vector2 position, float playerRadius) //벽 너머 획득과 중복 획득을 막고 한 개만 회수
        {
            if (maze == null || playerRadius <= 0f) return false;
            foreach (BaitSupplyData supply in supplies)
            {
                if (!supply.available) continue;
                Vector2 target = maze.data.center(supply.cell); //표시 중심과 동일한 접촉 위치
                if (Vector2.Distance(position, target) > pickupRadius + playerRadius || !maze.hasSight(position, target)) continue;
                supply.available = false;
                supply.remaining = respawnSeconds;
                supply.view.gameObject.SetActive(false);
                return true;
            }
            return false;
        }

        public void tick(float deltaTime, Vector2? playerPosition) //플레이 중에만 타이머를 진행하고 플레이어와 떨어진 다른 위치에 재생성
        {
            if (deltaTime <= 0f) return;
            animationTime += deltaTime;
            foreach (BaitSupplyData supply in supplies)
            {
                if (!supply.available)
                {
                    supply.remaining = Mathf.Max(0f, supply.remaining - deltaTime);
                    if (supply.remaining <= 0f) respawn(supply, playerPosition);
                }
                if (supply.available) supply.view.localScale = Vector3.one * (1f + Mathf.Sin(animationTime * 2.5f + supply.cell) * .04f);
            }
        }

        private void respawn(BaitSupplyData supply, Vector2? playerPosition) //직전 위치와 다른 보급 및 플레이어 주변을 제외한 무작위 물길 선택
        {
            int offset = candidates.Count == 0 ? 0 : random.Next(candidates.Count); //새 위치 탐색 시작점
            for (int i = 0; i < candidates.Count; i++)
            {
                int cell = candidates[(i + offset) % candidates.Count]; //검사할 물길
                if (cell == supply.cell) continue;
                Vector2 position = maze.data.center(cell); //재생성 후보 위치
                if (playerPosition.HasValue && Vector2.Distance(position, playerPosition.Value) < maze.data.tileSize * 2f) continue;
                bool occupied = false; //다른 보급과의 간격
                foreach (BaitSupplyData other in supplies)
                    if (other != supply && other.available && Vector2.Distance(position, maze.data.center(other.cell)) < maze.data.tileSize * 3f) { occupied = true; break; }
                if (occupied) continue;
                supply.cell = cell;
                supply.available = true;
                supply.remaining = 0f;
                supply.view.position = position;
                supply.view.gameObject.SetActive(true);
                return;
            }
            supply.view.gameObject.SetActive(false);
        }

        private Transform drawSupply(int index) //발사된 미끼와 구분되는 초록 고리와 황금 사료 묶음
        {
            Transform view = new GameObject("Bait supply " + (index + 1)).transform; //개별 보급 표시
            view.SetParent(supplyRoot, false);
            art.shape(view, "Supply halo", "circle", Vector2.zero, Vector2.one * .9f, new Color32(115, 215, 184, 45), 8);
            art.shape(view, "Supply ring", "ring", Vector2.zero, Vector2.one * .82f, SwimArt.mint, 9);
            art.shape(view, "Food", "circle", new Vector2(-.13f, .03f), Vector2.one * .3f, SwimArt.yellow, 10);
            art.shape(view, "Food", "circle", new Vector2(.14f, .07f), Vector2.one * .27f, new Color32(248, 168, 75, 255), 10);
            art.shape(view, "Food", "circle", new Vector2(.02f, -.16f), Vector2.one * .27f, SwimArt.yellow, 11);
            art.label(view, "미끼 +1", Vector2.down * .62f, .24f, GameUIStyleComponent.cream);
            return view;
        }
    }

    public sealed class BaitSupplyData
    {
        public int cell = -1; //현재 또는 직전 생성 칸
        public bool available; //현재 획득 가능한 상태
        public float remaining; //다시 생기기까지 남은 게임 시간
        public Transform view; //보급 표시
    }
}
