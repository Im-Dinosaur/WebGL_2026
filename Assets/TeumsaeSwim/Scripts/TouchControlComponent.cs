using UnityEngine;
using UnityEngine.EventSystems;

namespace TeumsaeSwim
{
    public sealed class TouchControlComponent : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private Player player; //입력을 전달할 진입점
        private RectTransform handle; //가상 스틱 손잡이
        private RectTransform area; //가상 스틱 입력 영역
        private bool holdButton; //누르고 있기 버튼 여부
        private int activePointer = int.MinValue; //이 컨트롤을 점유한 포인터

        public void initialize(Player target, RectTransform knob, bool hold) //터치 입력 대상 연결
        {
            player = target;
            handle = knob;
            holdButton = hold;
            area = (RectTransform)transform;
        }

        public void OnPointerDown(PointerEventData eventData) //터치 시작과 포인터 점유
        {
            if (activePointer != int.MinValue) return;
            activePointer = eventData.pointerId;
            if (holdButton) player.setHold(true);
            else updateStick(eventData);
        }

        public void OnDrag(PointerEventData eventData) //점유 중인 손가락의 스틱 이동
        {
            if (activePointer == eventData.pointerId && !holdButton) updateStick(eventData);
        }

        public void OnPointerUp(PointerEventData eventData) //터치 해제 시 입력 초기화
        {
            if (activePointer == eventData.pointerId) resetControl();
        }

        private void OnDisable() //패널 전환 시 남은 터치 해제
        {
            resetControl();
        }

        public void resetControl() //일시정지 후 고착된 입력 방지
        {
            activePointer = int.MinValue;
            if (player != null)
            {
                if (holdButton) player.setHold(false);
                else player.setMove(Vector2.zero);
            }
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }

        private void updateStick(PointerEventData eventData) //화면 좌표를 스틱 입력으로 변환
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out Vector2 point); //로컬 입력 위치
            float radius = area.rect.width * .32f; //손잡이 이동 반경
            Vector2 offset = Vector2.ClampMagnitude(point, radius); //제한된 손잡이 위치
            handle.anchoredPosition = offset;
            player.setMove(offset / radius);
        }
    }
}
