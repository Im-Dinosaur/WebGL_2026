using System;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class MazeGenerationComponent : MonoBehaviour
    {
        [SerializeField] private float tileSize = 2.2f; //지형 한 칸의 크기
        [SerializeField] private int practiceModules = 6; //연습 구역 수
        [SerializeField] private int normalModules = 7; //기본 구역 수
        [SerializeField] private int challengeModules = 8; //도전 구역 수

        public MazeData generate(int seed, SwimDifficulty difficulty) //연결이 검증된 구역 미로 생성
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                MazeData data = build(seed + attempt, difficulty); //생성 후보
                if (validate(data)) return data;
            }
            MazeData fallback = build(4817, SwimDifficulty.Practice); //검증용 기본 시드
            if (!validate(fallback)) throw new InvalidOperationException("연결된 미로를 만들지 못했습니다.");
            return fallback;
        }

        public bool validate(MazeData data) //미끼 없이 출구에 도달할 수 있는지 검사
        {
            data.route.Clear();
            data.route.AddRange(data.findPath(data.start, data.exit));
            if (data.route.Count < 20) return false;
            foreach (int home in data.homes)
                if (!data.passable(home, true) || Vector2.Distance(data.center(home), data.center(data.start)) < tileSize * 5f) return false;
            return true;
        }

        private MazeData build(int seed, SwimDifficulty difficulty) //입구와 출구를 잇는 모듈 배치
        {
            var random = new System.Random(seed); //재현 가능한 난수
            int modules = difficulty == SwimDifficulty.Practice ? practiceModules : difficulty == SwimDifficulty.Normal ? normalModules : challengeModules; //구역 수
            var data = new MazeData { width = 11, height = modules * 5 + 3, tileSize = tileSize, seed = seed }; //미로 데이터
            data.cells = new MazeCell[data.width * data.height];
            for (int i = 0; i < data.cells.Length; i++) data.cells[i] = new MazeCell();
            int entryX = 2; //구역 입구의 가로 위치
            data.start = data.width + entryX;
            carve(data, entryX, 1);
            for (int module = 0; module < modules; module++)
            {
                int baseY = module * 5 + 2; //구역의 시작 높이
                int exitX = entryX <= 4 ? random.Next(7, 9) : random.Next(2, 4); //반대편 출구 위치
                int middleY = baseY + 2; //주 통로 높이
                for (int y = baseY - 1; y <= middleY; y++) carve(data, entryX, y);
                for (int x = Mathf.Min(entryX, exitX); x <= Mathf.Max(entryX, exitX); x++) carve(data, x, middleY);
                for (int y = middleY; y <= baseY + 5; y++) carve(data, exitX, y);
                //주 통로 옆의 작은 회랑은 우회와 시야 차단에 사용한다.
                int loopLeft = random.Next(3, 5); //회랑 왼쪽 끝
                int loopRight = random.Next(6, 8); //회랑 오른쪽 끝
                int offset = module % 2 == 0 ? 1 : -1; //회랑이 뻗는 방향
                for (int x = loopLeft; x <= loopRight; x++) carve(data, x, middleY + offset * 2);
                for (int distance = 1; distance <= 2; distance++)
                {
                    carve(data, loopLeft, middleY + offset * distance);
                    carve(data, loopRight, middleY + offset * distance);
                }
                if (module > 0)
                {
                    int feature = baseY * data.width + entryX; //구역 경계의 상호작용 칸
                    data.cells[feature].kind = module % 2 == 1 ? TerrainKind.Gap : TerrainKind.Net;
                    data.cells[feature].horizontal = false;
                }
                int grassX = entryX < exitX ? entryX + 1 : entryX - 1; //진입 직후의 은신처
                data.cells[middleY * data.width + grassX].kind = TerrainKind.Grass;
                data.cells[(middleY + offset * 2) * data.width + loopLeft].kind = TerrainKind.Grass;
                if (module == 2 || (module == 5 && difficulty != SwimDifficulty.Practice))
                    data.homes.Add(middleY * data.width + (entryX < exitX ? exitX - 1 : exitX + 1));
                entryX = exitX;
            }
            data.exit = (data.height - 2) * data.width + entryX;
            data.cells[data.exit].kind = TerrainKind.Water;
            return data;
        }

        private void carve(MazeData data, int x, int y) //외벽 안에 물길 생성
        {
            if (x > 0 && x < data.width - 1 && y > 0 && y < data.height - 1)
                data.cells[y * data.width + x].kind = TerrainKind.Water;
        }
    }
}
