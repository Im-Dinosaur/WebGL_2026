using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class PredatorVisualComponent : MonoBehaviour
    {
        [SerializeField] private SwimArt art; //포식자 표시 자원
        private Transform body; //회전할 물고기 그림
        private TextMesh marker; //발견과 복귀 상태 표시
        private Transform gauge; //발견 누적 시간 표시
        private Mesh cone; //지형에 잘리는 시야 부채꼴
        private MeshRenderer coneRenderer; //시야 렌더러
        private Material coneMaterial; //포식자별 시야 색상 재질
        private readonly Vector3[] vertices = new Vector3[26]; //시야 꼭짓점

        public void initialize() //물고기와 상태 표시 구성
        {
            body = new GameObject("Predator body").transform;
            body.SetParent(transform, false);
            var tail = art.shape(body, "Tail", "triangle", new Vector2(-.55f, 0), new Vector2(.5f, .62f), SwimArt.coral, 9); //포식자 꼬리
            tail.transform.localRotation = Quaternion.Euler(0, 0, 180f);
            art.shape(body, "Body", "circle", Vector2.zero, new Vector2(1.1f, .72f), new Color32(157, 105, 140, 255), 10);
            art.shape(body, "Fin", "triangle", new Vector2(-.1f, .38f), new Vector2(.43f, .4f), SwimArt.coral, 9);
            art.shape(body, "Eye white", "circle", new Vector2(.31f, .1f), Vector2.one * .2f, Color.white, 11);
            art.shape(body, "Eye", "circle", new Vector2(.35f, .1f), Vector2.one * .1f, SwimArt.ink, 12);
            marker = art.label(transform, "", new Vector2(0, .8f), .62f, SwimArt.yellow, 17);
            gauge = art.shape(transform, "Notice gauge", "round", new Vector2(0, 1.3f), new Vector2(.8f, .07f), SwimArt.yellow, 16).transform;
            var coneObject = new GameObject("Field of view"); //시야 표시 오브젝트
            coneObject.transform.SetParent(transform, false);
            cone = new Mesh { name = "Clipped field of view" };
            cone.MarkDynamic();
            coneObject.AddComponent<MeshFilter>().sharedMesh = cone;
            coneRenderer = coneObject.AddComponent<MeshRenderer>();
            coneMaterial = new Material(art.material);
            coneRenderer.sharedMaterial = coneMaterial;
            coneRenderer.sortingOrder = 0;
            int[] triangles = new int[24 * 3]; //부채꼴 삼각형 인덱스
            for (int i = 0; i < 24; i++) { triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = i + 2; }
            cone.vertices = vertices;
            cone.triangles = triangles;
            var colors = new Color[vertices.Length]; //시야 메시의 기본 색
            var uv = new Vector2[vertices.Length]; //공용 재질의 텍스처 좌표
            for (int i = 0; i < colors.Length; i++) { colors[i] = Color.white; uv[i] = Vector2.one * .5f; }
            cone.colors = colors;
            cone.uv = uv;
        }

        public void tick(Maze maze, Vector2 facing, PredatorState state, float progress, float range, float angle) //시야와 발견 표시 갱신
        {
            float heading = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg; //물고기 방향 각도
            body.localRotation = Quaternion.Euler(0, 0, heading);
            marker.text = state == PredatorState.Chase ? "!" : state == PredatorState.Notice || state == PredatorState.Return ? "?" : "";
            marker.color = state == PredatorState.Chase ? SwimArt.coral : SwimArt.yellow;
            gauge.gameObject.SetActive(state == PredatorState.Notice);
            gauge.localScale = new Vector3(Mathf.Clamp01(progress) * .8f, .07f, 1f);
            Color color = state == PredatorState.Chase ? new Color(1f, .3f, .25f, .2f) : new Color(1f, .8f, .35f, .14f); //행동별 시야 색
            coneMaterial.SetColor("_Color", color);
            for (int i = 0; i <= 24; i++)
            {
                float rayAngle = (heading - angle * .5f + angle * i / 24f) * Mathf.Deg2Rad; //시야 경계의 방향
                Vector2 direction = new Vector2(Mathf.Cos(rayAngle), Mathf.Sin(rayAngle)); //시야 광선 방향
                vertices[i + 1] = direction * maze.rayDistance(transform.position, direction, range);
            }
            cone.vertices = vertices;
            cone.RecalculateBounds();
        }

        private void OnDestroy() //동적으로 생성한 메시와 재질 정리
        {
            if (cone != null) Destroy(cone);
            if (coneMaterial != null) Destroy(coneMaterial);
        }
    }
}
