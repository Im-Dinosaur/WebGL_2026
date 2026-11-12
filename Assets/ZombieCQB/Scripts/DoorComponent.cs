using UnityEngine;

namespace ZombieCqb
{
    public sealed class DoorComponent : MonoBehaviour
    {
        [SerializeField] private bool startsOpen; //스테이지 시작 때 열린 상태
        [SerializeField] private SpriteRenderer leaf; //닫혔을 때 보이는 문짝
        public bool isOpen { get; private set; } //시야와 이동이 통하는 상태

        public void resetDoor() //맵에 저장된 초기 상태 적용
        {
            setOpen(startsOpen);
        }

        public void toggle() //문을 열거나 닫기
        {
            setOpen(!isOpen);
        }

        private void setOpen(bool open) //문 상태와 표시 동기화
        {
            isOpen = open;
            leaf.enabled = !open;
        }
    }
}
