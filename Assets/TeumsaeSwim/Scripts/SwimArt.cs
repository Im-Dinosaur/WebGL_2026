using System.Collections.Generic;
using UnityEngine;

namespace TeumsaeSwim
{
    [CreateAssetMenu(menuName = "Teumsae Swim/Art")]
    public sealed class SwimArt : ScriptableObject
    {
        [SerializeField] private Font koreanFont; //한국어 공용 글꼴
        [SerializeField] private Material spriteMaterial; //빌드에 포함할 스프라이트 재질
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>(); //재사용하는 도형 이미지
        public Font font => koreanFont; //UI와 월드 안내에 사용할 글꼴
        public Material material => spriteMaterial; //공용 그림 재질
        public static readonly Color ink = new Color32(9, 31, 45, 255); //깊은 바다 색
        public static readonly Color mint = new Color32(115, 215, 184, 255); //안전 표시 색
        public static readonly Color yellow = new Color32(255, 214, 105, 255); //플레이어 색
        public static readonly Color coral = new Color32(246, 126, 111, 255); //위험 표시 색

        public Sprite sprite(string kind) //코드로 만든 도형 이미지 재사용
        {
            if (sprites.TryGetValue(kind, out Sprite cached)) return cached;
            const int size = 96; //생성 이미지 해상도
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false); //투명 도형 텍스처
            texture.name = "Swim " + kind;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size]; //도형 픽셀
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + .5f) / size * 2f - 1f; //가로 정규화 좌표
                float v = (y + .5f) / size * 2f - 1f; //세로 정규화 좌표
                float alpha; //픽셀 투명도
                if (kind == "circle") alpha = Mathf.Clamp01((1f - Mathf.Sqrt(u * u + v * v)) * size * .5f);
                else if (kind == "triangle") alpha = Mathf.Clamp01(Mathf.Min(1f - Mathf.Abs(v), (u + 1f) * .5f - Mathf.Abs(v)) * size);
                else if (kind == "ring") alpha = Mathf.Clamp01((.055f - Mathf.Abs(Mathf.Sqrt(u * u + v * v) - .91f)) * size);
                else if (kind == "round")
                {
                    Vector2 edge = new Vector2(Mathf.Max(Mathf.Abs(u) - .72f, 0f), Mathf.Max(Mathf.Abs(v) - .72f, 0f)); //둥근 모서리 거리
                    alpha = Mathf.Clamp01((.28f - edge.magnitude) * size);
                }
                else alpha = 1f;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite generated = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size); //월드 한 칸 크기의 원본
            generated.name = "Swim " + kind;
            sprites[kind] = generated;
            return generated;
        }

        public SpriteRenderer shape(Transform parent, string name, string kind, Vector2 position, Vector2 scale, Color color, int order) //장면용 도형 생성
        {
            var item = new GameObject(name); //도형 오브젝트
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            var renderer = item.AddComponent<SpriteRenderer>(); //도형 렌더러
            renderer.sprite = sprite(kind);
            renderer.sharedMaterial = spriteMaterial;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        public TextMesh label(Transform parent, string value, Vector2 position, float size, Color color, int order = 15) //월드 공간의 한국어 안내
        {
            var item = new GameObject("Label " + value); //안내 오브젝트
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            var text = item.AddComponent<TextMesh>(); //월드 텍스트
            text.font = koreanFont;
            text.fontSize = 48;
            text.characterSize = size / 4f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.text = value;
            text.color = color;
            var renderer = item.GetComponent<MeshRenderer>(); //텍스트 재질 연결
            renderer.sharedMaterial = koreanFont.material;
            renderer.sortingOrder = order;
            return text;
        }
    }
}
