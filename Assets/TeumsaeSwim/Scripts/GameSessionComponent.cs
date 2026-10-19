using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class GameSessionComponent : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float practiceTimeGoal = 180f; //연습 난이도 시간 별의 목표 초
        [SerializeField, Min(1f)] private float normalTimeGoal = 240f; //기본 난이도 시간 별의 목표 초
        [SerializeField, Min(1f)] private float challengeTimeGoal = 300f; //도전 난이도 시간 별의 목표 초
        public SwimPhase phase { get; private set; } = SwimPhase.Home; //현재 게임 단계
        public SwimDifficulty difficulty { get; private set; } //현재 난이도
        public float elapsed { get; private set; } //실제 플레이 시간
        public bool newRecord { get; private set; } //이번 탈출의 최고 기록 여부
        public bool cargoDelivered { get; private set; } //진주를 보유한 채 무사히 탈출한 결과
        public float timeGoal => timeGoalFor(difficulty); //현재 판의 시간 별 기준
        public bool timeStar => phase == SwimPhase.Won && elapsed <= timeGoal; //목표 시간 안에 탈출한 결과
        public int stars => phase == SwimPhase.Won ? 1 + (timeStar ? 1 : 0) + (cargoDelivered ? 1 : 0) : 0; //탈출 성공 때 확정되는 별 개수

        public float timeGoalFor(SwimDifficulty value) //난이도별 시간 별 기준 조회
        {
            return value == SwimDifficulty.Practice ? practiceTimeGoal : value == SwimDifficulty.Normal ? normalTimeGoal : challengeTimeGoal;
        }

        public void begin(SwimDifficulty value) //새 플레이의 시간과 결과 초기화
        {
            difficulty = value;
            elapsed = 0;
            newRecord = false;
            cargoDelivered = false;
            phase = SwimPhase.Playing;
        }

        public void advance(float deltaTime, bool caught, bool escaped, bool hasCargo = false) //시간과 접촉 우선 결과 및 진주 운반 판정
        {
            if (phase != SwimPhase.Playing) return;
            elapsed += deltaTime;
            if (caught) phase = SwimPhase.Lost;
            else if (escaped)
            {
                phase = SwimPhase.Won;
                cargoDelivered = hasCargo;
            }
        }

        public void saveRecord() //성공한 판의 난이도별 기록 저장
        {
            if (phase != SwimPhase.Won) return;
            float best = bestTime(difficulty); //이전 최고 기록
            newRecord = best <= 0 || elapsed < best;
            if (!newRecord) return;
            PlayerPrefs.SetFloat("teumsae.best." + difficulty, elapsed);
            PlayerPrefs.Save();
        }

        public float bestTime(SwimDifficulty value) //이 브라우저의 난이도별 최고 기록 읽기
        {
            return PlayerPrefs.GetFloat("teumsae.best." + value, 0f);
        }

        public void pause() //플레이 중에만 정지
        {
            if (phase == SwimPhase.Playing) phase = SwimPhase.Paused;
        }

        public void resume() //정지한 플레이 재개
        {
            if (phase == SwimPhase.Paused) phase = SwimPhase.Playing;
        }

        public void home() //시작 화면으로 복귀
        {
            phase = SwimPhase.Home;
        }
    }
}
