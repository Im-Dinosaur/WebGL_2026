using UnityEngine;

namespace ZombieCqb
{
    public sealed class PlayerSpecialAbilityComponent : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float stealthDuration = 3.4f; //은신 기본 지속 시간
        [SerializeField, Min(0.1f)] private float stealthCooldown = 10f; //은신 재사용 대기 시간
        [SerializeField, Min(0.1f)] private float dashDistance = 2.65f; //기본 돌진 거리
        [SerializeField, Min(0.1f)] private float dashCooldown = 5f; //돌진 재사용 대기 시간
        [SerializeField, Min(0.1f)] private float commandDuration = 4.5f; //좀비 이동 명령의 기본 지속 시간
        [SerializeField, Min(0.1f)] private float commandCooldown = 7f; //좀비 조종 재사용 대기 시간

        private float stealthRemaining; //은신이 적용될 남은 시간
        private float stealthWait; //은신 재사용까지 남은 시간
        private float dashWait; //돌진 재사용까지 남은 시간
        private float commandWait; //좀비 조종 재사용까지 남은 시간

        public bool isHidden => stealthRemaining > 0f; //은신 효과 활성 여부
        public float detectionMultiplier => isHidden ? 0.23f : 1f; //경비의 탐지 진행 배율

        public void resetForStage() //스테이지 진입 시 활성 효과와 재사용 대기 초기화
        {
            stealthRemaining = 0f;
            stealthWait = 0f;
            dashWait = 0f;
            commandWait = 0f;
        }

        public void tick(float deltaTime) //현재 특수능력의 지속 및 대기 시간 경과
        {
            stealthRemaining = Mathf.Max(0f, stealthRemaining - deltaTime);
            stealthWait = Mathf.Max(0f, stealthWait - deltaTime);
            dashWait = Mathf.Max(0f, dashWait - deltaTime);
            commandWait = Mathf.Max(0f, commandWait - deltaTime);
        }

        public void tryStealth(int rank) //해금되고 준비된 은신 사용
        {
            if (rank <= 0 || stealthWait > 0f)
                return;

            stealthRemaining = stealthDuration + (rank - 1) * 0.85f;
            stealthWait = stealthCooldown;
        }

        public float tryDash(int rank) //준비된 돌진의 이동 거리 반환
        {
            if (rank <= 0 || dashWait > 0f)
                return 0f;

            dashWait = dashCooldown;
            return dashDistance + (rank - 1) * 0.65f;
        }

        public float tryCommand(int rank, bool hasZombies) //좀비가 있을 때 조종 지속 시간 반환
        {
            if (rank <= 0 || !hasZombies || commandWait > 0f)
                return 0f;

            commandWait = commandCooldown;
            return commandDuration + (rank - 1) * 1.3f;
        }

        public float getCooldown(UpgradeId id) //화면에 표시할 특수능력 재사용 대기 시간
        {
            return id switch
            {
                UpgradeId.Stealth => stealthWait,
                UpgradeId.Dash => dashWait,
                UpgradeId.Command => commandWait,
                _ => 0f
            };
        }
    }
}
