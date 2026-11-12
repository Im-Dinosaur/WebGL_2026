using UnityEngine;

namespace ZombieCqb
{
    public sealed class PlayerStatComponent : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float baseHealth = 160f; //기본 최대 체력
        [SerializeField, Min(0.1f)] private float baseSpeed = 4.5f; //기본 이동 속도
        [SerializeField, Min(0f)] private float baseHearing = 4.2f; //기본 발소리 감지 거리
        [SerializeField, Min(0f)] private float healthPerRank = 35f; //강화 단계마다 늘어나는 최대 체력
        [SerializeField, Min(0f)] private float speedPerRank = 0.55f; //강화 단계마다 늘어나는 이동 속도
        [SerializeField, Min(0f)] private float hearingPerRank = 2f; //강화 단계마다 늘어나는 청력 거리

        public float health { get; private set; } //현재 체력
        public float maxHealth { get; private set; } //현재 최대 체력
        public float speed { get; private set; } //현재 이동 속도
        public float hearing { get; private set; } //현재 발소리 감지 거리
        public bool isAlive => health > 0f; //생존 상태

        public void resetStats() //새 플레이의 기본 능력치 적용
        {
            maxHealth = baseHealth;
            health = maxHealth;
            speed = baseSpeed;
            hearing = baseHearing;
        }

        public void applyRanks(int healthRank, int speedRank, int hearingRank) //능력치 강화 단계를 현재 수치에 적용
        {
            float upgradedHealth = baseHealth + healthRank * healthPerRank; //강화 후 최대 체력
            health = Mathf.Min(upgradedHealth, health + Mathf.Max(0f, upgradedHealth - maxHealth));
            maxHealth = upgradedHealth;
            speed = baseSpeed + speedRank * speedPerRank;
            hearing = baseHearing + hearingRank * hearingPerRank;
        }

        public void takeDamage(float amount) //공격 피해를 체력에 반영
        {
            health = Mathf.Max(0f, health - Mathf.Max(0f, amount));
        }
    }
}
