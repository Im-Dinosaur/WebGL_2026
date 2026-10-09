using System;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class Player : MonoBehaviour
    {
        [SerializeField] private PlayerInputComponent input; //입력 구성 요소
        [SerializeField] private PlayerMovementComponent movement; //이동 구성 요소
        [SerializeField] private PlayerInteractionComponent interaction; //상호작용 구성 요소
        [SerializeField] private PlayerVisualComponent visual; //표시 구성 요소
        private Maze maze; //현재 지형 진입점
        public Vector2 position => transform.position; //외부에 제공하는 위치
        public float radius => movement.radius; //접촉 판정 반경
        public bool hidden => maze != null && maze.isHidden(position, radius); //수풀 안의 은신 상태
        public bool squeezed => movement.squeezed; //현재 몸을 줄인 상태
        public int baitCount => interaction.baitCount; //남은 미끼 수
        public bool nearNet => interaction.nearNet; //작업 가능한 그물 존재
        public bool working => interaction.working; //그물 작업 여부
        public float netProgress => interaction.netProgress; //그물 작업 표시
        public string message => interaction.message; //상호작용 알림
        public event Action<SwimCue> cue; //효과음 요청

        private void Awake() //구성 요소의 이벤트 연결
        {
            interaction.cue += relayCue;
        }

        private void OnDestroy() //이벤트 참조 해제
        {
            interaction.cue -= relayCue;
        }

        private void relayCue(SwimCue value) //구성 요소의 효과음 요청 전달
        {
            cue?.Invoke(value);
        }

        public void initialize(Maze world) //플레이어 구성 요소 초기화 조율
        {
            maze = world;
            input.clear();
            movement.initialize(world);
            interaction.initialize(world);
            visual.initialize();
        }

        public void tick(float deltaTime) //입력과 상호작용 및 이동 순서 조율
        {
            input.sample();
            bool wasSqueezed = movement.squeezed; //직전 몸체 상태
            interaction.tick(position, input.moveInput, movement.facing, input.holdInput, input.consumeBaitRequest(), deltaTime);
            movement.tick(input.moveInput, input.holdInput, interaction.working, deltaTime);
            if (!wasSqueezed && movement.squeezed) cue?.Invoke(SwimCue.Squeeze);
            visual.tick(movement.facing, movement.squeezed, movement.moving, hidden, deltaTime);
        }

        public void setMove(Vector2 direction) //가상 스틱 진입점
        {
            input.setMove(direction);
        }

        public void setHold(bool held) //누르고 있기 진입점
        {
            input.setHold(held);
        }

        public void useBait() //미끼 버튼 진입점
        {
            input.requestBait();
        }

        public void resetInput() //중단 시 입력과 관성 해제
        {
            input.clear();
            movement.stop();
        }
    }
}
