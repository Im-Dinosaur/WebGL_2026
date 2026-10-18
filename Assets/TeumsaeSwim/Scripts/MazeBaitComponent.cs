using System.Collections.Generic;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class MazeBaitComponent : MonoBehaviour
    {
        [SerializeField] private SwimArt art; //미끼 표시 자원
        [SerializeField, Min(.1f)] private float launchSpeed = 8f; //미끼 발사 속도
        [SerializeField, Min(.1f)] private float launchDistance = 6.6f; //벽이 없을 때의 발사 거리
        [SerializeField, Min(.05f)] private float bounceDistance = 1.1f; //벽 반사 후 이동 거리
        [SerializeField, Min(.1f)] private float attractDistance = 8.8f; //정지한 미끼의 유인 반경
        [SerializeField, Min(.1f)] private float baitDuration = 8f; //정지 후 미끼 유지 시간
        [SerializeField, Min(.05f)] private float bodyRadius = .18f; //미끼 충돌 반경
        private readonly List<BaitData> activeBaits = new List<BaitData>(); //현재 발사된 미끼
        private Maze maze; //지형 기능 진입점
        private Transform baitRoot; //새 판에서 정리할 표시 루트
        public IReadOnlyList<BaitData> baits => activeBaits; //외부에서 조회할 미끼 상태

        public void initialize(Maze world) //새 미로의 미끼 상태 초기화
        {
            if (baitRoot != null)
            {
                baitRoot.gameObject.SetActive(false);
                removeVisual(baitRoot.gameObject);
            }
            maze = world;
            activeBaits.Clear();
            baitRoot = new GameObject("Generated baits").transform;
            baitRoot.SetParent(transform, false);
        }

        public bool launch(Vector2 position, Vector2 direction) //플레이어 위치에서 바라보는 방향으로 발사
        {
            if (!maze.canOccupy(position, bodyRadius, true)) return false;
            if (direction.sqrMagnitude < .001f) direction = Vector2.up;
            SpriteRenderer visual = art.shape(baitRoot, "Bait", "circle", position, Vector2.one * .3f, SwimArt.yellow, 8); //날아가는 미끼 표시
            BaitData bait = new BaitData //비행과 유인에 필요한 상태
            {
                position = position, velocity = direction.normalized * launchSpeed,
                travelRemaining = launchDistance, attractDistance = attractDistance,
                remaining = baitDuration, visual = visual.transform
            };
            activeBaits.Add(bait);
            return true;
        }

        public void tick(float deltaTime) //미끼 비행과 정지 후 수명 진행
        {
            if (deltaTime <= 0f) return;
            for (int i = activeBaits.Count - 1; i >= 0; i--)
            {
                BaitData bait = activeBaits[i]; //갱신할 미끼
                if (!bait.settled) fly(bait, deltaTime);
                else
                {
                    bait.remaining -= deltaTime;
                    if (bait.remaining <= 0f) { consume(bait); continue; }
                }
                if (bait.visual != null) bait.visual.position = bait.position;
            }
        }

        private void fly(BaitData bait, float deltaTime) //충돌면에 한 번 반사한 뒤 짧게 이동하여 정지
        {
            float distance = Mathf.Min(bait.travelRemaining, bait.velocity.magnitude * deltaTime); //이번 갱신에서 이동할 거리
            while (distance > .0001f && !bait.settled)
            {
                float step = Mathf.Min(.06f, distance); //얇은 그물을 넘지 않는 이동 단위
                Vector2 next = maze.moveProjectile(bait.position, bait.velocity.normalized * step, bodyRadius, out Vector2 normal); //충돌을 적용한 위치
                float travelled = Vector2.Distance(bait.position, next); //실제 이동한 거리
                bait.position = next;
                bait.travelRemaining -= travelled;
                distance -= step;
                if (normal.sqrMagnitude > .01f)
                {
                    if (bait.bounceCount > 0) { settle(bait); break; }
                    bait.bounceCount++;
                    bait.velocity = Vector2.Reflect(bait.velocity, normal);
                    bait.travelRemaining = bounceDistance;
                    distance = Mathf.Min(distance, bait.travelRemaining);
                }
                if (bait.travelRemaining <= .001f) settle(bait);
            }
        }

        private void settle(BaitData bait) //정지 위치에 유인 파동 표시
        {
            bait.settled = true;
            bait.velocity = Vector2.zero;
            art.shape(bait.visual, "Bait ripple", "ring", Vector2.zero, Vector2.one * 3.4f, new Color(1f, .82f, .4f, .45f), 7);
        }

        public void consume(BaitData bait) //먹었거나 만료된 미끼 제거
        {
            if (bait == null || !activeBaits.Remove(bait)) return;
            if (bait.visual != null) removeVisual(bait.visual.gameObject);
        }

        private void removeVisual(GameObject item) //실행 환경에 맞춰 미끼 표시 정리
        {
            if (Application.isPlaying) Destroy(item);
            else DestroyImmediate(item);
        }
    }
}
