using UnityEngine;
using UnityEngine.EventSystems;

namespace TeumsaeSwim
{
    public sealed class TouchBaitAimComponent : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private Player player; //조준을 전달할 플레이어 진입점
        private RectTransform area; //미끼 버튼의 좌표 영역
        private RectTransform handle; //조준 중 움직이는 표시
        private float fullDragDistance; //최대 발사 거리에 해당하는 드래그 길이
        private float cancelRadius; //발사를 취소하는 중앙 영역
        private int activePointer = int.MinValue; //현재 조준을 점유한 포인터
        private bool dragged; //단순 탭과 구별할 실제 드래그 여부
        private bool validAim; //중앙 취소 영역 밖의 조준 여부

        public void initialize(Player target, RectTransform knob, float dragDistance, float cancelDistance) //조준 대상과 화면 크기에 독립적인 감도 연결
        {
            player = target;
            area = (RectTransform)transform;
            handle = knob;
            fullDragDistance = Mathf.Max(1f, dragDistance);
            cancelRadius = Mathf.Clamp(cancelDistance, 0, fullDragDistance - .1f);
            resetControl();
        }

        public void OnPointerDown(PointerEventData eventData) //한 손가락 또는 왼쪽 마우스로 조준 시작
        {
            if (activePointer != int.MinValue || eventData.button != PointerEventData.InputButton.Left || !player.beginBaitAim()) return;
            activePointer = eventData.pointerId;
            dragged = false;
            validAim = false;
            if (handle != null) { handle.anchoredPosition = Vector2.zero; handle.gameObject.SetActive(true); }
        }

        public void OnDrag(PointerEventData eventData) //다른 이동 손가락과 독립된 조준 갱신
        {
            if (activePointer != eventData.pointerId) return;
            dragged = true;
            updateAim(eventData);
        }

        public void OnPointerUp(PointerEventData eventData) //드래그한 손가락을 뗄 때 선택한 방향으로 한 번 발사
        {
            if (activePointer != eventData.pointerId) return;
            if (dragged) updateAim(eventData);
            player.endBaitAim(dragged && validAim);
            resetControl();
        }

        private void updateAim(PointerEventData eventData) //버튼 중앙 기준의 드래그를 방향과 거리 입력으로 전달
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out Vector2 point)) return;
            point -= area.rect.center;
            validAim = point.magnitude > cancelRadius;
            Vector2 value = validAim ? Vector2.ClampMagnitude(point / fullDragDistance, 1f) : Vector2.zero; //중앙에서는 경로와 발사 예약 해제
            player.updateBaitAim(value);
            if (handle != null) handle.anchoredPosition = Vector2.ClampMagnitude(point, area.rect.width * .32f);
        }

        public void resetControl() //일시정지와 화면 전환 시 예약 없이 조준 취소
        {
            activePointer = int.MinValue;
            dragged = false;
            validAim = false;
            if (player != null) player.endBaitAim(false);
            if (handle != null) { handle.anchoredPosition = Vector2.zero; handle.gameObject.SetActive(false); }
        }

        private void OnDisable() //중단된 터치가 복귀 후 발사되지 않도록 정리
        {
            resetControl();
        }
    }
}
