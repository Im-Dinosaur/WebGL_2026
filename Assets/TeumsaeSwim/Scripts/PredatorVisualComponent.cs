using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class PredatorVisualComponent : MonoBehaviour
    {
        [SerializeField] private SwimArt art; //포식자 표시 자원
        [SerializeField] private bool showTypeName = true; //색과 함께 종류 이름도 표시
        private Transform body; //회전할 물고기 그림
        private TextMesh marker; //발견과 복귀 상태 표시
        private Transform gauge; //발견 누적 시간 표시
        private float animationTime; //일시정지에 함께 멈추는 헤엄 시간

        public void initialize(PredatorTypeProfile profile = null) //종류별 물고기와 상태 표시 구성
        {
            body = new GameObject("Predator body").transform;
            body.SetParent(transform, false);
            if (profile != null && profile.sprite != null) drawSpecies(profile);
            else drawFallback();
            if (profile != null && showTypeName) art.label(transform, profile.title, new Vector2(0, -.83f), .25f, profile.color, 17);
            animationTime = 0;
            marker = art.label(transform, "", new Vector2(0, .8f), .62f, SwimArt.yellow, 17);
            gauge = art.shape(transform, "Notice gauge", "round", new Vector2(0, 1.3f), new Vector2(.8f, .07f), SwimArt.yellow, 16).transform;
        }

        private void drawSpecies(PredatorTypeProfile profile) //시안에서 만든 투명 스프라이트를 몸체 크기에 맞춰 표시
        {
            var fish = new GameObject("Species fish"); //종류별 그림 오브젝트
            fish.transform.SetParent(body, false);
            var renderer = fish.AddComponent<SpriteRenderer>(); //종류 그림 렌더러
            renderer.sprite = profile.sprite;
            renderer.sharedMaterial = art.material;
            renderer.sortingOrder = 10;
            float width = profile.type == PredatorKind.Sprinter ? 1.65f : 1.45f; //꼬리를 포함한 그림의 가로 크기
            fish.transform.localScale = Vector3.one * (width / profile.sprite.bounds.size.x);
        }

        private void drawFallback() //종류 설정이 없는 기존 호출의 기본 도형 유지
        {
            var tail = art.shape(body, "Tail", "triangle", new Vector2(-.55f, 0), new Vector2(.5f, .62f), SwimArt.coral, 9); //포식자 꼬리
            tail.transform.localRotation = Quaternion.Euler(0, 0, 180f);
            art.shape(body, "Body", "circle", Vector2.zero, new Vector2(1.1f, .72f), new Color32(157, 105, 140, 255), 10);
            art.shape(body, "Fin", "triangle", new Vector2(-.1f, .38f), new Vector2(.43f, .4f), SwimArt.coral, 9);
            art.shape(body, "Eye white", "circle", new Vector2(.31f, .1f), Vector2.one * .2f, Color.white, 11);
            art.shape(body, "Eye", "circle", new Vector2(.35f, .1f), Vector2.one * .1f, SwimArt.ink, 12);
        }

        public void tick(Vector2 facing, PredatorState state, float progress, float deltaTime = 0f) //헤엄과 발견 신호 및 진행 게이지 갱신
        {
            animationTime += Mathf.Max(0f, deltaTime);
            float pulse = Mathf.Sin(animationTime * (state == PredatorState.Chase ? 15f : 7f)) * .025f; //작고 부드러운 헤엄 변형
            body.localScale = new Vector3(1f + pulse, (1f - pulse) * (facing.x < 0f ? -1f : 1f), 1f);
            float heading = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg; //물고기 방향 각도
            body.localRotation = Quaternion.Euler(0, 0, heading);
            marker.text = state == PredatorState.Chase ? "!" : state == PredatorState.Notice || state == PredatorState.Return || state == PredatorState.Lure ? "?" : "";
            marker.color = state == PredatorState.Chase ? SwimArt.coral : SwimArt.yellow;
            gauge.gameObject.SetActive(state == PredatorState.Notice);
            gauge.localScale = new Vector3(Mathf.Clamp01(progress) * .8f, .07f, 1f);
        }
    }
}
