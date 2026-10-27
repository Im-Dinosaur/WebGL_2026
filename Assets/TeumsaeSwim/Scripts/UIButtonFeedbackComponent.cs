using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TeumsaeSwim
{
    public sealed class UIButtonFeedbackComponent : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, Range(.85f, 1f)] private float pressedScale = .95f; //누른 면의 축소 비율
        [SerializeField, Range(0f, .05f)] private float reboundScale = .025f; //놓았을 때 살짝 튀는 비율
        [SerializeField, Min(.01f)] private float pressDuration = .06f; //눌림 전환 시간
        [SerializeField, Min(.01f)] private float releaseDuration = .16f; //원래 크기 복원 시간
        [SerializeField, Min(.01f)] private float rippleDuration = .25f; //물결이 사라지는 시간
        private RectTransform surface; //입력 영역과 분리한 표시 면
        private Image ripple; //터치를 막지 않는 물결 그림
        private Button button; //비활성 여부를 확인할 일반 버튼
        private Vector3 originalScale; //표시 면의 원래 크기
        private Vector2 originalPosition; //표시 면의 원래 위치
        private Color rippleColor; //물결의 원래 색
        private int activePointer = int.MinValue; //효과를 점유한 손가락
        private bool retainOutside; //조준과 꾹은 영역 밖에서도 누름 유지
        private float depression; //현재 눌린 정도
        private float releaseAge; //복원 효과의 경과 시간
        private float rippleAge; //물결 효과의 경과 시간
        public bool pressed { get; private set; } //현재 점유 손가락의 유효한 누름 상태

        public void initialize(RectTransform visual, Image wave, Button selectable = null, bool keepOutside = false) //표시 면만 움직이고 기존 입력 영역은 유지
        {
            surface = visual;
            ripple = wave;
            button = selectable;
            retainOutside = keepOutside;
            originalScale = surface.localScale;
            originalPosition = surface.anchoredPosition;
            rippleColor = ripple.color;
            ripple.raycastTarget = false;
            resetFeedback();
        }

        public void OnPointerDown(PointerEventData eventData) //첫 유효 손가락에만 눌림 반응
        {
            if (surface == null || activePointer != int.MinValue || eventData.button != PointerEventData.InputButton.Left || !canPress()) return;
            activePointer = eventData.pointerId;
            pressed = true;
            releaseAge = releaseDuration;
            rippleAge = rippleDuration;
        }

        public void OnPointerUp(PointerEventData eventData) //점유 손가락을 놓으면 복원과 짧은 물결 표시
        {
            if (activePointer != eventData.pointerId) return;
            bool completed = pressed && canPress(); //취소된 메뉴 누름에는 물결 생략
            activePointer = int.MinValue;
            pressed = false;
            releaseAge = completed ? 0 : releaseDuration;
            rippleAge = completed ? 0 : rippleDuration;
        }

        public void OnPointerEnter(PointerEventData eventData) //같은 손가락이 버튼으로 돌아오면 눌림 복원
        {
            if (activePointer == eventData.pointerId && canPress()) pressed = true;
        }

        public void OnPointerExit(PointerEventData eventData) //일반 버튼 밖으로 이동하면 누름 취소
        {
            if (activePointer == eventData.pointerId && !retainOutside) pressed = false;
        }

        private bool canPress() //미끼 부족 등 비활성 상태는 반응하지 않음
        {
            return isActiveAndEnabled && (button == null || button.IsActive() && button.IsInteractable());
        }

        private void Update() //일시정지 화면에서도 실제 시간으로 효과 재생
        {
            animate(Time.unscaledDeltaTime);
        }

        private void animate(float deltaTime) //입력 좌표와 무관한 표시 면의 눌림 및 복원 계산
        {
            if (surface == null) return;
            if (!canPress()) { resetFeedback(); return; }
            float duration = pressed ? pressDuration : releaseDuration * .55f; //짧게 눌러도 빠르게 복원
            depression = Mathf.MoveTowards(depression, pressed ? 1f : 0f, deltaTime / Mathf.Max(.01f, duration));
            releaseAge = Mathf.Min(releaseDuration, releaseAge + deltaTime);
            float rebound = pressed ? 0 : Mathf.Sin(Mathf.PI * releaseAge / releaseDuration) * reboundScale; //한 번만 부드럽게 되튀기
            surface.localScale = originalScale * (Mathf.Lerp(1f, pressedScale, depression) + rebound);
            surface.anchoredPosition = originalPosition + Vector2.down * (2f * depression);
            rippleAge = Mathf.Min(rippleDuration, rippleAge + deltaTime);
            float progress = rippleAge / rippleDuration; //확장하면서 옅어지는 물결
            ripple.rectTransform.localScale = Vector3.one * Mathf.Lerp(.45f, 1.5f, progress);
            Color color = rippleColor; //기본 색은 유지하고 투명도만 조절
            color.a *= 1f - progress;
            ripple.color = color;
        }

        private void resetFeedback() //화면 전환과 포커스 상실 시 눌림과 물결 초기화
        {
            activePointer = int.MinValue;
            pressed = false;
            depression = 0;
            releaseAge = releaseDuration;
            rippleAge = rippleDuration;
            if (surface != null) { surface.localScale = originalScale; surface.anchoredPosition = originalPosition; }
            if (ripple != null) { Color color = rippleColor; color.a = 0; ripple.color = color; }
        }

        private void OnDisable() //숨긴 패널의 효과가 복귀 후 남지 않도록 정리
        {
            resetFeedback();
        }

        private void OnApplicationFocus(bool focused) //앱 전환으로 놓침 이벤트가 없어도 표시 복구
        {
            if (!focused) resetFeedback();
        }

        private void OnApplicationPause(bool paused) //모바일 브라우저가 중단되면 표시 복구
        {
            if (paused) resetFeedback();
        }
    }
}
