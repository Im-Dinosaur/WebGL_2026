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
        public bool hasCargo { get; private set; } //이번 판에 획득하여 운반 중인 진주
        public bool working { get; private set; } //그물 작업 중 여부
        public string message { get; private set; } //최근 상호작용 알림
        public event Action<SwimCue> cue; //상호작용 소리 요청
        public float netProgress => maze == null ? 0f : maze.netProgress(netTarget); //작업 표시 비율
        public bool nearNet => netTarget >= 0; //그물 안내 표시 여부

        public void initialize(Maze world) //미끼와 상호작용 상태 초기화
        {
            maze = world;
            baitCount = initialBaits;
            hasCargo = false;
            working = false;
            previousWorking = false;
            netTarget = -1;
            message = "";
        }

        public void tick(Vector2 position, Vector2 input, Vector2 facing, bool hold, bool bait, float deltaTime,
            Vector2 baitDirection = default, float baitStrength = 1f) //그물 작업과 선택한 방향의 미끼 발사 처리
        {
            message = "";
            netTarget = maze.nearbyNet(position);
            working = hold && netTarget >= 0 && !maze.overlapsGap(position, .36f);
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
            Vector2 direction = baitDirection.sqrMagnitude > .04f ? baitDirection.normalized : input.sqrMagnitude > .04f ? input.normalized : facing; //조준을 우선하고 빠른 발사에는 이동과 시선 적용
            if (!maze.placeBait(position, direction, baitStrength)) { message = "여기서는 미끼를 발사할 수 없어"; return; }
            baitCount--;
            message = "미끼 발사! 멈추면 주변 포식자를 유인해요";
            cue?.Invoke(SwimCue.Bait);
        }

        public void collectCargo(Vector2 position, float radius) //이동 후 진주 접촉과 보유 상태 갱신
        {
            if (hasCargo || !maze.tryCollectItem(position, radius)) return;
            hasCargo = true;
            message = "진주를 찾았어! 출구까지 무사히 가져가자";
            cue?.Invoke(SwimCue.Pickup);
        }
    }
}
