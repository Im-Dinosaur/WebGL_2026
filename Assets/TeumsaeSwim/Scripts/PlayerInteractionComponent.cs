using System;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class PlayerInteractionComponent : MonoBehaviour
    {
        [SerializeField] private int initialBaits = 2; //시작 미끼 개수
        private Maze maze; //지형 진입점
        private bool previousWorking; //직전 그물 작업 상태
        private int netTarget = -1; //현재 그물 대상
        public int baitCount { get; private set; } //남은 미끼 개수
        public bool working { get; private set; } //그물 작업 중 여부
        public string message { get; private set; } //최근 상호작용 알림
        public event Action<SwimCue> cue; //상호작용 소리 요청
        public float netProgress => netTarget < 0 ? 0f : maze.data.cells[netTarget].netProgress / 2f; //작업 표시 비율
        public bool nearNet => netTarget >= 0; //그물 안내 표시 여부

        public void initialize(Maze world) //미끼와 상호작용 상태 초기화
        {
            maze = world;
            baitCount = initialBaits;
            working = false;
            previousWorking = false;
            netTarget = -1;
            message = "";
        }

        public void tick(Vector2 position, Vector2 input, Vector2 facing, bool hold, bool bait, float deltaTime) //그물 작업과 미끼 배치 처리
        {
            message = "";
            netTarget = maze.nearbyNet(position);
            working = hold && input.sqrMagnitude < .03f && netTarget >= 0;
            if (working)
            {
                if (!previousWorking) cue?.Invoke(SwimCue.Tear);
                if (maze.workNet(netTarget, deltaTime))
                {
                    cue?.Invoke(SwimCue.Open);
                    message = "그물이 열렸어!";
                    working = false;
                }
            }
            previousWorking = working;
            if (!bait) return;
            if (baitCount <= 0) { message = "남은 미끼가 없어"; return; }
            if (!maze.placeBait(position, facing)) { message = "이 방향에는 미끼를 놓을 수 없어"; return; }
            baitCount--;
            message = "미끼를 놓았어 · 순찰 중일 때만 효과가 있어";
            cue?.Invoke(SwimCue.Bait);
        }
    }
}
