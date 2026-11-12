using UnityEngine;

namespace ZombieCqb
{
    public sealed class Player : MonoBehaviour
    {
        [SerializeField] private PlayerInputComponent input; //플레이어의 조작 입력
        [SerializeField] private PlayerMovementComponent movement; //플레이어의 이동
        [SerializeField] private PlayerStatComponent stats; //플레이어의 체력과 기본 능력치
        [SerializeField] private PlayerHearingComponent hearing; //발소리 방향 표시
        [SerializeField] private PlayerSpecialAbilityComponent special; //특수능력 발동과 재사용
        [SerializeField] private SpriteRenderer body; //플레이어의 피격 및 은신 표시
        [SerializeField, Min(1f)] private float dashSpeed = 13.5f; //돌진할 때의 이동 속도

        private float damageFlash; //피격 표시가 남은 시간
        public Vector2 position => transform.position; //현재 위치
        public Vector2 facing => movement.facing; //현재 바라보는 방향
        public float radius => movement.radius; //문과 이동에 사용하는 몸체 반경
        public float health => stats.health; //현재 체력
        public float maxHealth => stats.maxHealth; //현재 최대 체력
        public bool isAlive => stats.isAlive; //플레이어의 생존 상태
        public bool isHidden => special.isHidden; //현재 은신 중인지 여부
        public float detectionMultiplier => special.detectionMultiplier; //경비가 플레이어를 발견하는 속도 배율

        public PlayerInputFrame readInput() //플레이어 조작을 한 프레임 단위로 전달
        {
            return input.read();
        }

        public void resetRun() //새 플레이의 체력과 능력치 초기화
        {
            stats.resetStats();
        }

        public void enterStage(Vector2 start) //체력을 가져와 새 맵에 배치
        {
            movement.place(start);
            hearing.clear();
            special.resetForStage();
            damageFlash = 0f;
            body.color = Color.white;
        }

        public void applyUpgrades(UpgradeProgressComponent progress) //현재 기본 능력치 강화 반영
        {
            stats.applyRanks(progress.getRank(UpgradeId.Health), progress.getRank(UpgradeId.Speed), progress.getRank(UpgradeId.Hearing));
        }

        public void tickMovement(PlayerInputFrame controls, float deltaTime, IndoorNavigationComponent navigation, UpgradeProgressComponent progress) //이동 입력과 직접 발동하는 특수능력 조율
        {
            special.tick(deltaTime);
            if (controls.stealth)
                special.tryStealth(progress.getRank(UpgradeId.Stealth));

            if (controls.dash)
            {
                float distance = special.tryDash(progress.getRank(UpgradeId.Dash)); //이번 돌진의 허용 거리
                if (distance > 0f)
                    movement.startDash(controls.move, distance, dashSpeed);
            }

            movement.tick(controls.move, stats.speed, deltaTime, navigation);
            damageFlash = Mathf.Max(0f, damageFlash - deltaTime);
            body.color = damageFlash > 0f ? new Color(1f, 0.42f, 0.38f) : Color.white;
            if (isHidden)
                body.color = new Color(body.color.r, body.color.g, body.color.b, 0.52f);
        }

        public void updateHearing(StageMapComponent map) //현재 청력 수치에 맞춰 발소리 표시
        {
            hearing.tick(position, stats.hearing, map);
        }

        public float tryCommand(int rank, bool hasZombies) //좀비 이동 명령의 발동 여부와 지속 시간 전달
        {
            return special.tryCommand(rank, hasZombies);
        }

        public float getCooldown(UpgradeId id) //특수능력의 재사용 대기 시간 전달
        {
            return special.getCooldown(id);
        }

        public void receiveAttack(float damage) //경비 공격을 플레이어 체력과 표시로 전달
        {
            if (damage <= 0f)
                return;

            stats.takeDamage(damage);
            damageFlash = 0.19f;
        }
    }
}
