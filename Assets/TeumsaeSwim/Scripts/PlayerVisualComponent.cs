using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class PlayerVisualComponent : MonoBehaviour
    {
        [SerializeField] private SwimArt art; //물고기 도형과 색상
        private Transform fish; //회전과 크기를 적용할 몸체
        private Transform tail; //헤엄치는 꼬리
        private SpriteRenderer body; //몸체 색상 표시
        private float animationTime; //게임 진행 중 애니메이션 시간

        public void initialize() //노란 물고기 표시 구성
        {
            if (fish != null) return;
            fish = new GameObject("Yellow fish").transform;
            fish.SetParent(transform, false);
            tail = art.shape(fish, "Tail", "triangle", new Vector2(-.43f, 0), new Vector2(.45f, .52f), new Color32(235, 168, 62, 255), 9).transform;
            tail.localRotation = Quaternion.Euler(0, 0, 180f);
            body = art.shape(fish, "Body", "circle", Vector2.zero, new Vector2(.85f, .6f), SwimArt.yellow, 10);
            art.shape(fish, "Fin", "triangle", new Vector2(-.05f, -.22f), new Vector2(.3f, .23f), new Color32(229, 166, 65, 255), 11);
            art.shape(fish, "Eye white", "circle", new Vector2(.22f, .08f), Vector2.one * .18f, Color.white, 12);
            art.shape(fish, "Eye", "circle", new Vector2(.25f, .08f), Vector2.one * .085f, SwimArt.ink, 13);
            art.shape(fish, "Cheek", "circle", new Vector2(.23f, -.1f), new Vector2(.1f, .055f), new Color32(242, 151, 89, 255), 12);
        }

        public void tick(Vector2 facing, bool squeezed, bool moving, bool hidden, float deltaTime) //진행 상태에 맞춘 물고기 자세
        {
            animationTime += deltaTime;
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg; //목표 회전각
            fish.localRotation = Quaternion.Slerp(fish.localRotation, Quaternion.Euler(0, 0, angle), 1f - Mathf.Exp(-14f * deltaTime));
            fish.localScale = Vector3.Lerp(fish.localScale, new Vector3(squeezed ? .83f : 1f, squeezed ? .56f : 1f, 1f), 1f - Mathf.Exp(-18f * deltaTime));
            tail.localScale = new Vector3(.45f, .52f * (.8f + Mathf.Sin(animationTime * (moving ? 19f : 6f)) * .2f), 1f);
            body.color = hidden ? new Color32(160, 210, 134, 255) : SwimArt.yellow;
        }
    }
}
