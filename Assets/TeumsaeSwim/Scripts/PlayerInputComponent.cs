using UnityEngine;
using UnityEngine.InputSystem;

namespace TeumsaeSwim
{
    public sealed class PlayerInputComponent : MonoBehaviour
    {
        private Vector2 touchMove; //가상 스틱 입력
        private bool touchHold; //터치 버튼 유지 상태
        private bool baitRequested; //미끼 사용 예약
        private Vector2 requestedBaitDirection; //발사 예약에 저장한 조준 방향
        private float requestedBaitStrength = 1f; //발사 예약에 저장한 거리 비율
        public Vector2 moveInput { get; private set; } //정규화된 이동 입력
        public bool holdInput { get; private set; } //틈 통과와 그물 작업 입력
        public bool baitAiming { get; private set; } //미끼 조준 중 여부
        public Vector2 baitAimDirection { get; private set; } //이동과 독립된 조준 방향
        public float baitAimStrength { get; private set; } //드래그 길이에 따른 발사 거리 비율

        public void sample() //키보드와 터치 입력 결합
        {
            Vector2 keyboardMove = Vector2.zero; //키보드 방향
            Keyboard keyboard = Keyboard.current; //현재 키보드
            if (keyboard != null)
            {
                keyboardMove.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
                keyboardMove.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
                if (keyboard.eKey.wasPressedThisFrame) requestBait();
            }
            moveInput = Vector2.ClampMagnitude(touchMove + keyboardMove, 1f);
            holdInput = touchHold || (keyboard != null && keyboard.spaceKey.isPressed);
        }

        public void setMove(Vector2 value) //가상 스틱 방향 전달
        {
            touchMove = Vector2.ClampMagnitude(value, 1f);
        }

        public void setHold(bool value) //터치 누름 상태 전달
        {
            touchHold = value;
        }

        public void requestBait() //미끼 입력 예약
        {
            if (baitAiming) return;
            baitRequested = true;
            requestedBaitDirection = Vector2.zero;
            requestedBaitStrength = 1f;
        }

        public void beginBaitAim() //이동 입력과 별도로 조준 시작
        {
            baitAiming = true;
            baitAimDirection = Vector2.zero;
            baitAimStrength = 0;
        }

        public void updateBaitAim(Vector2 value) //정규화된 드래그 벡터를 방향과 거리로 분리
        {
            if (!baitAiming) return;
            baitAimStrength = Mathf.Clamp01(value.magnitude);
            baitAimDirection = value.sqrMagnitude > .0001f ? value.normalized : Vector2.zero;
        }

        public void endBaitAim(bool fire) //조준 완료 시 선택한 방향과 거리를 한 번만 예약
        {
            if (!baitAiming) return;
            if (fire && baitAimDirection.sqrMagnitude > .5f && baitAimStrength > 0)
            {
                baitRequested = true;
                requestedBaitDirection = baitAimDirection;
                requestedBaitStrength = baitAimStrength;
            }
            baitAiming = false;
            baitAimDirection = Vector2.zero;
            baitAimStrength = 0;
        }

        public bool consumeBaitRequest(out Vector2 direction, out float strength) //예약된 방향과 거리를 한 번 읽기
        {
            bool requested = baitRequested; //반환할 입력
            direction = requestedBaitDirection;
            strength = requestedBaitStrength;
            baitRequested = false;
            requestedBaitDirection = Vector2.zero;
            requestedBaitStrength = 1f;
            return requested;
        }

        public void clear() //일시정지와 재시작 때 남은 터치 해제
        {
            touchMove = Vector2.zero;
            touchHold = false;
            baitRequested = false;
            requestedBaitDirection = Vector2.zero;
            requestedBaitStrength = 1f;
            baitAiming = false;
            baitAimDirection = Vector2.zero;
            baitAimStrength = 0;
            moveInput = Vector2.zero;
            holdInput = false;
        }
    }
}
