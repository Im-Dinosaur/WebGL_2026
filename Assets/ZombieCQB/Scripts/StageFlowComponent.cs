using UnityEngine;

namespace ZombieCqb
{
    public enum RunPhase { Title, Playing, Upgrade, Dead, Complete }
    public enum StageOutcome { InfectedEveryone, Escaped }

    public sealed class StageFlowComponent : MonoBehaviour
    {
        [SerializeField] private StageMapComponent[] stages; //플레이 순서대로 정리한 맵 프리팹

        public RunPhase phase { get; private set; } //현재 진행 단계
        public StageOutcome outcome { get; private set; } //현재 스테이지의 통과 방식
        public int stageIndex { get; private set; } //현재 스테이지 번호의 인덱스
        public int infectedCount { get; private set; } //현재 맵에서 감염된 시민 수
        public int stageCount => stages.Length; //플레이 가능한 스테이지 수
        public StageMapComponent stagePrefab => stages[stageIndex]; //현재 단계의 맵 프리팹

        public void startRun() //첫 스테이지부터 새 플레이 시작
        {
            stageIndex = 0;
            infectedCount = 0;
            phase = RunPhase.Playing;
        }

        public void recordInfection() //감염 전환을 스테이지 진행에 반영
        {
            if (phase == RunPhase.Playing)
                infectedCount++;
        }

        public void resolve(bool alive, bool escaped, int civilianCount) //사망 또는 스테이지 통과 판정
        {
            if (phase != RunPhase.Playing)
                return;

            if (!alive)
            {
                phase = RunPhase.Dead;
                return;
            }

            if (infectedCount >= civilianCount || escaped)
            {
                outcome = infectedCount >= civilianCount ? StageOutcome.InfectedEveryone : StageOutcome.Escaped;
                phase = RunPhase.Upgrade;
            }
        }

        public bool advance() //강화 선택을 마치고 다음 스테이지로 전환
        {
            if (phase != RunPhase.Upgrade)
                return false;

            if (stageIndex + 1 >= stages.Length)
            {
                phase = RunPhase.Complete;
                return false;
            }

            stageIndex++;
            infectedCount = 0;
            phase = RunPhase.Playing;
            return true;
        }
    }
}
