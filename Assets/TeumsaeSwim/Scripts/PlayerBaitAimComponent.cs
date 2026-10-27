using System.Collections.Generic;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class PlayerBaitAimComponent : MonoBehaviour
    {
        [SerializeField] private SwimArt art; //조준 점과 도착점의 공용 그림 자원
        [SerializeField, Min(.08f)] private float dotSpacing = .24f; //예상 경로의 점 간격
        [SerializeField, Min(.02f)] private float dotSize = .07f; //예상 경로의 점 크기
        [SerializeField] private Color aimColor = new Color(1f, .86f, .43f, .9f); //미끼 조준선의 표시 색
        private Maze maze; //미끼 비행 예측을 제공하는 진입점
        private Transform aimRoot; //조준할 때만 보이는 표시 루트
        private SpriteRenderer endpoint; //미끼가 멈출 위치의 테두리
        private SpriteRenderer center; //예상 도착점의 중앙 표시
        private readonly List<Vector2> path = new List<Vector2>(4); //실제 충돌 규칙으로 계산한 경로
        private readonly List<SpriteRenderer> dots = new List<SpriteRenderer>(); //드래그 중 재사용하는 경로 점

        public void initialize(Maze world) //미로 연결과 조준 표시 초기화
        {
            maze = world;
            if (aimRoot == null)
            {
                aimRoot = new GameObject("Bait aim preview").transform;
                aimRoot.SetParent(transform, false);
                endpoint = art.shape(aimRoot, "Bait aim endpoint", "ring", Vector2.zero, Vector2.one * .48f, aimColor, 14);
                center = art.shape(aimRoot, "Bait aim center", "circle", Vector2.zero, Vector2.one * .1f, aimColor, 14);
            }
            hide();
        }

        public void tick(Vector2 position, Vector2 direction, float strength, bool aiming) //이동한 위치를 기준으로 점선과 반사 후 도착점 갱신
        {
            if (!aiming || maze == null || !maze.predictBait(position, direction, strength, path)) { hide(); return; }
            aimRoot.gameObject.SetActive(true);
            int used = 0; //이번 경로에서 사용한 점 수
            float next = .65f; //플레이어 몸 바로 앞을 비워 두는 첫 점 거리
            for (int segment = 1; segment < path.Count; segment++)
            {
                Vector2 delta = path[segment] - path[segment - 1]; //현재 직선 구간
                float length = delta.magnitude; //현재 구간의 길이
                while (next <= length)
                {
                    if (used == dots.Count) dots.Add(art.shape(aimRoot, "Bait aim dot", "circle", Vector2.zero, Vector2.one * dotSize, aimColor, 14));
                    SpriteRenderer dot = dots[used++]; //재사용할 경로 점
                    dot.gameObject.SetActive(true);
                    dot.transform.position = path[segment - 1] + delta.normalized * next;
                    next += dotSpacing;
                }
                next -= length;
            }
            for (int i = used; i < dots.Count; i++) dots[i].gameObject.SetActive(false);
            endpoint.transform.position = path[path.Count - 1];
            center.transform.position = path[path.Count - 1];
        }

        public void hide() //발사와 취소 및 화면 전환 시 조준선 숨김
        {
            if (aimRoot != null) aimRoot.gameObject.SetActive(false);
        }
    }
}
