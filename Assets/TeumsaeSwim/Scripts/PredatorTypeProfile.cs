using UnityEngine;

namespace TeumsaeSwim
{
    [CreateAssetMenu(menuName = "Teumsae Swim/Predator type")]
    public sealed class PredatorTypeProfile : ScriptableObject
    {
        [SerializeField] private PredatorKind kind; //포식자 종류
        [SerializeField] private string displayName = "기본형"; //게임에 표시할 종류 이름
        [SerializeField, TextArea] private string description = "균형 잡힌 포식자"; //도움말에 표시할 특징
        [SerializeField] private Color accent = new Color32(190, 133, 206, 255); //종류 안내 색상
        [SerializeField] private Sprite fishSprite; //투명 배경의 종류별 물고기 그림
        [SerializeField, Min(.1f)] private float sightDistance = 3.5f; //플레이어를 발견할 최대 거리
        [SerializeField, Min(.05f)] private float noticeDuration = .9f; //추격 전 연속 발견 시간
        [SerializeField, Min(.1f)] private float patrolMultiplier = 1f; //기본 순찰 속도의 배율
        [SerializeField, Min(.1f)] private float chaseMultiplier = 1f; //기본 추격 속도의 배율
        [SerializeField, Min(.1f)] private float lureMultiplier = 1f; //기본 미끼 이동 속도의 배율
        public PredatorKind type => kind; //종류 식별자
        public string title => displayName; //표시 이름
        public string hint => description; //역할 안내
        public Color color => accent; //종류 표시 색
        public Sprite sprite => fishSprite; //물고기 그림
        public float range => Mathf.Max(.1f, sightDistance); //유효한 탐지 거리
        public float noticeTime => Mathf.Max(.05f, noticeDuration); //유효한 발견 대기 시간

        public float speedScale(PredatorState state) //행동에 해당하는 종류별 이동 배율
        {
            return Mathf.Max(.1f, state == PredatorState.Chase ? chaseMultiplier : state == PredatorState.Lure ? lureMultiplier : patrolMultiplier);
        }
    }
}
