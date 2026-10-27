using System;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TeumsaeSwim.Editor
{
    public static class SwimRefreshVerification
    {
        private static int checks; //통과한 보급과 난이도 검사 수

        public static void run() //실제 씬에서 보급 배치와 가방 상한 및 재생성과 초기화 검사
        {
            checks = 0;
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            try
            {
                Maze maze = UnityEngine.Object.FindFirstObjectByType<Maze>(); //실제 지형
                Player player = UnityEngine.Object.FindFirstObjectByType<Player>(); //실제 획득 진입점
                MazeBaitSupplyComponent supply = maze.GetComponent<MazeBaitSupplyComponent>(); //검사할 보급 상태
                foreach (SwimDifficulty difficulty in Enum.GetValues(typeof(SwimDifficulty)))
                for (int seed = 0; seed < 16; seed++)
                {
                    maze.initialize(3500 + seed, difficulty);
                    int[] cells = supply.fieldSupplies.Select(item => item.cell).ToArray(); //같은 시드의 첫 보급 위치
                    require(maze.data.homes.Count == 3 * ((int)difficulty + 1), "Increased predator population");
                    require(supply.fieldSupplies.Count == (int)difficulty + 2, "Difficulty supply population");
                    require(cells.Distinct().Count() == cells.Length && supply.fieldSupplies.All(item => item.available), "All supplies spawn separately");
                    int[] distances = maze.data.distancesFrom(maze.data.start); //실제 통로 연결
                    foreach (BaitSupplyData item in supply.fieldSupplies)
                    {
                        require(maze.data.cells[item.cell].kind == TerrainKind.Water && !maze.data.cells[item.cell].safeZone && distances[item.cell] >= 6, "Reachable supply outside safe zone and obstacles");
                        require(item.cell != maze.GetComponent<MazeItemComponent>().cell && !maze.data.homes.Contains(item.cell), "Supply avoids pearl and predator home");
                    }
                    maze.initialize(3500 + seed, difficulty);
                    require(cells.SequenceEqual(supply.fieldSupplies.Select(item => item.cell)), "Deterministic initial supply positions");
                }
                maze.initialize(4817, SwimDifficulty.Normal);
                player.initialize(maze);
                PlayerInteractionComponent interaction = player.GetComponent<PlayerInteractionComponent>(); //실제 보유량과 획득 처리
                BaitSupplyData first = supply.fieldSupplies[0]; //첫 보급
                int oldCell = first.cell; //직전 생성 위치
                Vector2 firstPosition = maze.data.center(oldCell); //접촉 위치
                require(player.baitCount == 2, "Initial bait inventory remains two");
                interaction.collectBaitSupply(firstPosition + Vector2.right * 2f, player.radius);
                require(player.baitCount == 2 && first.available, "No distant pickup");
                interaction.collectBaitSupply(firstPosition, player.radius);
                interaction.collectBaitSupply(firstPosition, player.radius);
                require(player.baitCount == 3 && !first.available && Mathf.Approximately(first.remaining, 30f), "Contact adds exactly one and schedules thirty seconds");
                interaction.collectBaitSupply(maze.data.center(supply.fieldSupplies[1].cell), player.radius);
                BaitSupplyData third = supply.fieldSupplies[2]; //상한일 때 남겨 둘 보급
                interaction.collectBaitSupply(maze.data.center(third.cell), player.radius);
                require(player.baitCount == 4 && third.available, "Full inventory preserves field pickup");
                maze.tick(0f, firstPosition);
                require(Mathf.Approximately(first.remaining, 30f), "Zero game time preserves respawn timer");
                maze.tick(29.99f, firstPosition);
                require(!first.available, "No early respawn");
                maze.tick(.02f, firstPosition);
                require(first.available && first.cell != oldCell && Vector2.Distance(first.view.position, firstPosition) >= maze.data.tileSize * 2f, "Respawn moves away from previous cell and player");
                interaction.tick(maze.data.center(maze.data.start), Vector2.zero, Vector2.up, false, true, 0f);
                require(player.baitCount == 3, "Collected bait can be fired");
                interaction.collectBaitSupply(maze.data.center(third.cell), player.radius);
                require(player.baitCount == 4 && !third.available, "Pickup becomes usable after spending bait");
                Transform oldView = first.view; //재시작 후 제거할 표시
                maze.initialize(4817, SwimDifficulty.Normal);
                player.initialize(maze);
                require(oldView == null && player.baitCount == 2 && supply.fieldSupplies.All(item => item.available && item.remaining == 0f), "Retry clears old visuals and all respawn timers");
                SwimGame game = UnityEngine.Object.FindFirstObjectByType<SwimGame>(); //일시정지 시 실제 호출 흐름
                typeof(Player).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
                typeof(SwimGame).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
                game.begin(SwimDifficulty.Normal, false);
                first = supply.fieldSupplies[0];
                interaction.collectBaitSupply(maze.data.center(first.cell), player.radius);
                game.pause();
                typeof(SwimGame).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
                require(first.remaining == 30f && !first.available, "Pause does not advance supply clock");
                Debug.Log("SUMBAKKOGI_REFRESH_VERIFIED: " + checks + " checks");
            }
            finally { EditorSceneManager.OpenScene(SwimProjectSetup.scenePath); }
        }

        private static void require(bool value, string message) //검증 실패를 배치 오류로 전달
        {
            if (!value) throw new InvalidOperationException(message);
            checks++;
        }
    }
}
