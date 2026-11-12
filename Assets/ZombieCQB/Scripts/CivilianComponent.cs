using System.Collections.Generic;
using UnityEngine;

namespace ZombieCqb
{
    public sealed class CivilianComponent : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body; //시민과 좀비의 외형
        [SerializeField] private Sprite infectedSprite; //감염 완료 뒤 바뀌는 외형
        [SerializeField] private SpriteRenderer infectionMeter; //감염 중 표시할 게이지
        [SerializeField, Min(0.1f)] private float fleeSpeed = 2.55f; //도주 이동 속도
        [SerializeField, Min(0.1f)] private float zombieSpeed = 2.7f; //조종할 때 좀비 이동 속도
        [SerializeField, Min(0.1f)] private float noticeRange = 3.7f; //감염원을 발견하는 거리

        private readonly List<Vector2> route = new List<Vector2>(); //도주 또는 조종 이동 경로
        private int waypoint; //현재 경유점 번호
        private float routeDelay; //다음 경로 갱신까지 남은 시간
        private float fearRemaining; //마지막으로 본 위협의 기억 시간
        private float commandRemaining; //좀비 조종 명령이 남은 시간
        private Vector2 threatPosition; //마지막으로 본 위협의 위치
        private Vector2 commandPoint; //좀비에게 지시한 위치
        private bool visible = true; //플레이어 시야에 보이는 상태

        public Vector2 position => transform.position; //시민의 현재 위치
        public float infection { get; private set; } //누적 감염 진행도
        public bool isInfected => infection >= 1f; //좀비로 전환되었는지 여부
        public bool isCommanded => commandRemaining > 0f; //현재 좀비가 이동 명령을 따르는 상태
        public bool isMoving { get; private set; } //이번 프레임의 발소리 발생 여부

        public bool addInfection(float amount) //감염 진행을 올리고 최초 전환 여부 반환
        {
            if (isInfected)
                return false;

            infection = Mathf.Clamp01(infection + amount);
            updateMeter();
            if (!isInfected)
                return false;

            body.sprite = infectedSprite;
            route.Clear();
            routeDelay = 0f;
            fearRemaining = 0f;
            return true;
        }

        public void command(Vector2 destination, float duration) //감염된 좀비에게 이동 지시
        {
            if (!isInfected)
                return;

            commandPoint = destination;
            commandRemaining = duration;
            routeDelay = 0f;
        }

        public void tick(float deltaTime, Vector2 playerPosition, IndoorNavigationComponent navigation) //도주와 감염 이후의 이동 처리
        {
            isMoving = false;
            routeDelay -= deltaTime;
            commandRemaining = Mathf.Max(0f, commandRemaining - deltaTime);
            if (isInfected)
            {
                if (isCommanded)
                    moveTo(commandPoint, false, zombieSpeed, deltaTime, navigation);
                return;
            }

            if ((position - playerPosition).sqrMagnitude < noticeRange * noticeRange && navigation.hasSight(position, playerPosition))
            {
                threatPosition = playerPosition;
                fearRemaining = 1.5f;
            }
            else
            {
                fearRemaining = Mathf.Max(0f, fearRemaining - deltaTime);
            }

            if (fearRemaining > 0f)
                moveTo(threatPosition, true, fleeSpeed, deltaTime, navigation);
        }

        public void setVisible(bool value) //시야에 들어온 시민과 감염 게이지 표시
        {
            visible = value;
            body.enabled = visible;
            updateMeter();
        }

        private void moveTo(Vector2 target, bool retreat, float speed, float deltaTime, IndoorNavigationComponent navigation) //현재 상태의 이동 경로 따라가기
        {
            if (routeDelay <= 0f)
            {
                navigation.findRoute(position, target, retreat, route);
                routeDelay = 0.42f;
                waypoint = 0;
            }

            Vector2 oldPosition = position; //이동 전 위치
            Vector2 next = navigation.followRoute(oldPosition, route, ref waypoint, speed * deltaTime, 0.20f); //이동 뒤 위치
            Vector2 delta = next - oldPosition; //이동한 방향
            transform.position = next;
            isMoving = delta.sqrMagnitude > 0.00001f;
            if (isMoving)
                body.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);
        }

        private void updateMeter() //현재 감염량을 시민 위에 표시
        {
            infectionMeter.enabled = visible && infection > 0f && !isInfected;
            infectionMeter.transform.localScale = new Vector3(0.72f * infection, 0.075f, 1f);
        }
    }
}
