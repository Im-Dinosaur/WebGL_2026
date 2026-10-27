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
        [SerializeField] private PlayerBaitAimComponent baitAim; //미끼의 예상 경로 표시 구성 요소
        private Maze maze; //현재 지형 진입점
        public Vector2 position => transform.position; //외부에 제공하는 위치
        public float radius => movement.radius; //접촉 판정 반경
        public bool hidden => maze != null && maze.isHidden(position, radius); //수풀 안의 은신 상태
        public bool safe => maze != null && maze.isSafeZone(position); //시작과 도착 안전구역의 보호 상태
        public bool squeezed => movement.squeezed; //현재 몸을 줄인 상태
        public int baitCount => interaction.baitCount; //남은 미끼 수
        public bool hasCargo => interaction.hasCargo; //외부에서 조회할 진주 운반 상태
        public bool nearNet => interaction.nearNet; //작업 가능한 그물 존재
        public bool working => interaction.working; //그물 작업 여부
        public float netProgress => interaction.netProgress; //그물 작업 표시
        public string message => interaction.message; //상호작용 알림
        public bool baitAiming => input.baitAiming; //HUD에서 조회하는 조준 중 상태
        public float baitAimStrength => input.baitAimStrength; //HUD에서 조회하는 조준 거리 비율
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
            baitAim.initialize(world);
        }

        public void tick(float deltaTime) //입력과 상호작용 및 이동 순서 조율
        {
            input.sample();
            bool wasSqueezed = movement.squeezed; //직전 몸체 상태
            bool fireBait = input.consumeBaitRequest(out Vector2 baitDirection, out float baitStrength); //이동과 별도로 예약한 조준 발사
            interaction.tick(position, input.moveInput, movement.facing, input.holdInput, fireBait, deltaTime, baitDirection, baitStrength);
            movement.tick(input.moveInput, input.holdInput, interaction.working, deltaTime);
            interaction.collectCargo(position, radius);
            interaction.collectBaitSupply(position, radius);
            if (!wasSqueezed && movement.squeezed) cue?.Invoke(SwimCue.Squeeze);
            visual.tick(movement.facing, movement.squeezed, movement.moving, hidden, deltaTime);
            baitAim.tick(position, input.baitAimDirection, input.baitAimStrength, input.baitAiming && baitCount > 0);
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

        public bool beginBaitAim() //미끼가 남아 있을 때 드래그 조준 시작
        {
            if (baitCount <= 0) return false;
            input.beginBaitAim();
            return true;
        }

        public void updateBaitAim(Vector2 value) //UI에서 정한 방향과 거리 전달
        {
            input.updateBaitAim(value);
        }

        public void endBaitAim(bool fire) //발사 또는 취소를 입력 구성 요소에 전달
        {
            input.endBaitAim(fire);
            if (baitAim != null) baitAim.hide();
        }

        public void resetInput() //중단 시 입력과 관성 해제
        {
            input.clear();
            movement.stop();
            if (baitAim != null) baitAim.hide();
        }
    }
}
