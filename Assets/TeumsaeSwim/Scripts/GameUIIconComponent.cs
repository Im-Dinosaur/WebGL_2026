using UnityEngine;
using UnityEngine.UI;

namespace TeumsaeSwim
{
    public enum SwimUIIcon { Star, Pause, Sound, Muted, Clock, Bait, Knot, Chevron, Check, Close } //글꼴에 의존하지 않는 UI 기호

    [ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
    public sealed class GameUIIconComponent : MaskableGraphic
    {
        private SwimUIIcon kind; //그릴 기호 종류
        private bool filled; //별의 획득 표시

        protected override void OnEnable() //현행 UI 메시 생성 방식으로 등록
        {
            useLegacyMeshGeneration = false;
            base.OnEnable();
        }

        public void configure(SwimUIIcon value, Color tint, bool solid = false) //아이콘 종류와 표시 상태 지정
        {
            kind = value;
            filled = solid;
            color = tint;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh) //해상도에 독립적인 아이콘 도형 작성
        {
            mesh.Clear();
            if (kind == SwimUIIcon.Star)
            {
                var points = new Vector2[10]; //별 외곽과 안쪽 꼭짓점
                for (int i = 0; i < 10; i++)
                {
                    float angle = (90 + i * 36) * Mathf.Deg2Rad; //별 꼭짓점 각도
                    points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (i % 2 == 0 ? .91f : .45f);
                }
                for (int i = 0; i < 10; i++)
                {
                    if (filled) triangle(mesh, Vector2.zero, points[i], points[(i + 1) % 10]);
                    else line(mesh, points[i], points[(i + 1) % 10], .12f);
                }
            }
            else if (kind == SwimUIIcon.Pause)
            {
                line(mesh, new Vector2(-.3f, -.65f), new Vector2(-.3f, .65f), .28f);
                line(mesh, new Vector2(.3f, -.65f), new Vector2(.3f, .65f), .28f);
            }
            else if (kind == SwimUIIcon.Clock)
            {
                arc(mesh, .73f, 0, 360);
                line(mesh, Vector2.zero, new Vector2(0, .48f), .13f);
                line(mesh, Vector2.zero, new Vector2(.36f, -.15f), .13f);
                line(mesh, new Vector2(-.24f, .95f), new Vector2(.24f, .95f), .13f);
            }
            else if (kind == SwimUIIcon.Bait)
            {
                for (int i = 0; i < 24; i++)
                {
                    float a = i * Mathf.PI / 12f; //미끼 타원 시작 각도
                    float b = (i + 1) * Mathf.PI / 12f; //미끼 타원 끝 각도
                    triangle(mesh, Vector2.zero, new Vector2(Mathf.Cos(a) * .82f, Mathf.Sin(a) * .55f), new Vector2(Mathf.Cos(b) * .82f, Mathf.Sin(b) * .55f));
                }
            }
            else if (kind == SwimUIIcon.Knot)
            {
                for (int i = -1; i <= 1; i++)
                {
                    line(mesh, new Vector2(-.72f, i * .55f), new Vector2(.72f, i * .55f), .12f);
                    line(mesh, new Vector2(i * .55f, -.72f), new Vector2(i * .55f, .72f), .12f);
                }
                line(mesh, new Vector2(-.65f, -.65f), new Vector2(.65f, .65f), .21f);
            }
            else if (kind == SwimUIIcon.Chevron || kind == SwimUIIcon.Check)
            {
                line(mesh, new Vector2(-.55f, kind == SwimUIIcon.Check ? 0 : .7f), new Vector2(kind == SwimUIIcon.Check ? -.12f : .25f, kind == SwimUIIcon.Check ? -.5f : 0), .21f);
                line(mesh, new Vector2(kind == SwimUIIcon.Check ? -.12f : .25f, kind == SwimUIIcon.Check ? -.5f : 0), new Vector2(kind == SwimUIIcon.Check ? .65f : -.55f, kind == SwimUIIcon.Check ? .6f : -.7f), .21f);
            }
            else if (kind == SwimUIIcon.Close)
            {
                line(mesh, new Vector2(-.6f, -.6f), new Vector2(.6f, .6f), .16f);
                line(mesh, new Vector2(-.6f, .6f), new Vector2(.6f, -.6f), .16f);
            }
            else
            {
                triangle(mesh, new Vector2(-.85f, -.3f), new Vector2(-.85f, .3f), new Vector2(-.12f, .7f));
                triangle(mesh, new Vector2(-.85f, -.3f), new Vector2(-.12f, .7f), new Vector2(-.12f, -.7f));
                if (kind == SwimUIIcon.Sound) { arc(mesh, .5f, -65, 65); arc(mesh, .9f, -65, 65); }
                else
                {
                    line(mesh, new Vector2(.22f, -.35f), new Vector2(.85f, .35f), .13f);
                    line(mesh, new Vector2(.22f, .35f), new Vector2(.85f, -.35f), .13f);
                }
            }
        }

        private void arc(VertexHelper mesh, float radius, float start, float end) //원호의 선분 연결
        {
            for (int i = 0; i < 32; i++)
            {
                float a = Mathf.Lerp(start, end, i / 32f) * Mathf.Deg2Rad; //시작 각도
                float b = Mathf.Lerp(start, end, (i + 1f) / 32f) * Mathf.Deg2Rad; //끝 각도
                line(mesh, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, .12f);
            }
        }

        private void line(VertexHelper mesh, Vector2 start, Vector2 end, float width) //일정 굵기의 선분 메시 작성
        {
            Vector2 normal = new Vector2(-(end - start).y, (end - start).x).normalized * width * .5f; //선의 반너비
            triangle(mesh, start - normal, start + normal, end + normal);
            triangle(mesh, start - normal, end + normal, end - normal);
        }

        private void triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c) //로컬 사각형에 맞춘 삼각형 추가
        {
            Rect bounds = rectTransform.rect; //아이콘 표시 범위
            float scale = Mathf.Min(bounds.width, bounds.height) * .5f; //정규 좌표 배율
            int first = mesh.currentVertCount; //추가 꼭짓점 시작 번호
            mesh.AddVert(bounds.center + a * scale, color, Vector2.zero);
            mesh.AddVert(bounds.center + b * scale, color, Vector2.zero);
            mesh.AddVert(bounds.center + c * scale, color, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
        }
    }
}
