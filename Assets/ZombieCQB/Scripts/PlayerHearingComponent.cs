using UnityEngine;

namespace ZombieCqb
{
    public sealed class PlayerHearingComponent : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] indicators; //발소리 방향을 알리는 표시
        [SerializeField, Min(0.1f)] private float markerDistance = 1.85f; //플레이어와 방향 표시 사이 거리

        public void clear() //현재 발소리 표시 지우기
        {
            foreach (SpriteRenderer indicator in indicators) //사용 가능한 방향 표시
                indicator.enabled = false;
        }

        public void tick(Vector2 origin, float range, StageMapComponent map) //시야 밖 발소리 방향만 화면에 표시
        {
            clear();
            int count = 0; //현재 사용한 방향 표시 수
            foreach (GuardComponent guard in map.security) //청력 범위의 경비
            {
                if (guard.isMoving)
                    mark(origin, guard.position, range, new Color(1f, 0.56f, 0.38f), map, ref count);
            }

            foreach (CivilianComponent civilian in map.people) //청력 범위의 시민
            {
                if (civilian.isMoving && !civilian.isInfected)
                    mark(origin, civilian.position, range, new Color(0.99f, 0.83f, 0.50f), map, ref count);
            }
        }

        private void mark(Vector2 origin, Vector2 source, float range, Color color, StageMapComponent map, ref int count) //소리 방향을 알리는 표시 하나 갱신
        {
            Vector2 direction = source - origin; //소리가 난 방향
            if (count >= indicators.Length || direction.sqrMagnitude > range * range || map.isVisible(origin, source))
                return;

            SpriteRenderer marker = indicators[count++]; //이번 발소리의 방향 표시
            marker.transform.position = origin + direction.normalized * markerDistance;
            marker.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            color.a = 0.38f + Mathf.PingPong(Time.time * 1.8f, 0.52f);
            marker.color = color;
            marker.enabled = true;
        }
    }
}
