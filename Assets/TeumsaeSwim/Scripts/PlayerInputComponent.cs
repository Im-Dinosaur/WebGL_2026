using UnityEngine;
using UnityEngine.InputSystem;

namespace TeumsaeSwim
{
    public sealed class PlayerInputComponent : MonoBehaviour
    {
        private Vector2 touchMove; //가상 스틱 입력
        private bool touchHold; //터치 버튼 유지 상태
        private bool baitRequested; //미끼 사용 예약
        public Vector2 moveInput { get; private set; } //정규화된 이동 입력
        public bool holdInput { get; private set; } //틈 통과와 그물 작업 입력

        public void sample() //키보드와 터치 입력 결합
        {
            Vector2 keyboardMove = Vector2.zero; //키보드 방향
            Keyboard keyboard = Keyboard.current; //현재 키보드
            if (keyboard != null)
            {
                keyboardMove.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
                keyboardMove.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
                if (keyboard.eKey.wasPressedThisFrame) baitRequested = true;
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
            baitRequested = true;
        }

        public bool consumeBaitRequest() //예약된 미끼 입력 한 번 읽기
        {
            bool requested = baitRequested; //반환할 입력
            baitRequested = false;
            return requested;
        }

        public void clear() //일시정지와 재시작 때 남은 터치 해제
        {
            touchMove = Vector2.zero;
            touchHold = false;
            baitRequested = false;
            moveInput = Vector2.zero;
            holdInput = false;
        }
    }
}
