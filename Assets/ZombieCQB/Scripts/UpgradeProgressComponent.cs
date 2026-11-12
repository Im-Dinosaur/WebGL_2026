using System;
using UnityEngine;

namespace ZombieCqb
{
    public enum UpgradeId
    {
        Health, Speed, Hearing,
        InfectionSpeed, Liquid, Droplet,
        Stealth, Dash, Command
    }

    public sealed class UpgradeProgressComponent : MonoBehaviour
    {
        public const int upgradeCount = 9; //기본 능력치, 감염, 특수능력의 전체 항목 수
        [SerializeField, Min(2)] private int maximumRank = 3; //항목별 최대 강화 단계

        private readonly int[] ranks = new int[upgradeCount]; //현재 플레이에서 선택한 강화 단계
        public int experience { get; private set; } //시민 감염으로 쌓인 경험치

        public void resetProgress() //새 플레이의 강화 단계와 경험치 초기화
        {
            Array.Clear(ranks, 0, ranks.Length);
            experience = 0;
        }

        public void addInfectionExperience() //시민 전환 한 번의 경험치 지급
        {
            experience++;
        }

        public int getRank(UpgradeId upgrade) //항목의 누적 단계 조회
        {
            return ranks[(int)upgrade];
        }

        public bool canUpgrade(UpgradeId upgrade) //선택한 항목을 더 강화할 수 있는지 확인
        {
            int index = (int)upgrade; //항목의 배열 인덱스
            return index >= 0 && index < ranks.Length && ranks[index] < maximumRank;
        }

        public bool tryUpgrade(UpgradeId upgrade) //선택 가능한 항목의 강화 단계 증가
        {
            if (!canUpgrade(upgrade))
                return false;

            ranks[(int)upgrade]++;
            return true;
        }
    }
}
