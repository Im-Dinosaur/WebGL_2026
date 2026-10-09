using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class GameSessionComponent : MonoBehaviour
    {
        public SwimPhase phase { get; private set; } = SwimPhase.Home; //현재 게임 단계
        public SwimDifficulty difficulty { get; private set; } //현재 난이도
        public float elapsed { get; private set; } //실제 플레이 시간
        public bool newRecord { get; private set; } //이번 탈출의 최고 기록 여부

        public void begin(SwimDifficulty value) //새 플레이의 시간과 결과 초기화
        {
            difficulty = value;
            elapsed = 0;
            newRecord = false;
            phase = SwimPhase.Playing;
        }

        public void advance(float deltaTime, bool caught, bool escaped) //시간과 접촉 우선 결과 판정
        {
            if (phase != SwimPhase.Playing) return;
            elapsed += deltaTime;
            if (caught) phase = SwimPhase.Lost;
            else if (escaped) phase = SwimPhase.Won;
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
