using UnityEngine;

namespace TeumsaeSwim
{
    [CreateAssetMenu(menuName = "Teumsae Swim/Maze tiles")]
    public sealed class MazeTileSet : ScriptableObject
    {
        [SerializeField] private Sprite water; //물길 바탕 타일
        [SerializeField] private Sprite rock; //연결할 바위 표면
        [SerializeField] private Sprite grass; //원형 은신 풀숲
        [SerializeField] private Sprite coral; //바위 위의 작은 산호 장식
        [SerializeField] private Sprite closedNet; //닫힌 그물
        [SerializeField] private Sprite openNet; //양쪽에 풀어진 그물
        [SerializeField] private Color waterTint = new Color(.62f, .78f, .82f, .62f); //바탕과 부드럽게 섞이는 물길 색
        [SerializeField, Range(.02f, .12f)] private float cornerRadius = .07f; //타일 대비 바깥 모서리 반경
        public Sprite waterSprite => water; //물길 이미지
        public Sprite rockSprite => rock; //바위 이미지
        public Sprite grassSprite => grass; //풀숲 이미지
        public Sprite coralSprite => coral; //산호 이미지
        public Sprite closedNetSprite => closedNet; //그물의 닫힌 상태
        public Sprite openNetSprite => openNet; //그물의 열린 상태
        public Color waterColor => waterTint; //물길의 표시 색
        public float rounding => cornerRadius; //바위 모서리의 곡률
    }
}
