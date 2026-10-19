using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class PredatorRosterComponent : MonoBehaviour
    {
        [SerializeField] private PredatorTypeProfile[] profiles; //등장 가능한 종류별 설정
        public IReadOnlyList<PredatorTypeProfile> types => profiles; //도움말에서 조회할 종류 목록

        public PredatorTypeProfile[] createRoster(int seed, int count, SwimDifficulty difficulty) //시드와 난이도에 맞춰 중복을 분산한 조합 구성
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            var result = new PredatorTypeProfile[count]; //이번 판에 적용할 순서
            if (count == 0) return result;
            if (profiles == null || profiles.Length == 0) throw new InvalidOperationException("포식자 종류 설정을 연결해주세요.");
            var unique = new HashSet<PredatorKind>(); //잘못 연결한 중복 종류 검사
            PredatorTypeProfile balanced = null; //연습에 반드시 포함할 기본형
            foreach (PredatorTypeProfile profile in profiles)
            {
                if (profile == null || !unique.Add(profile.type)) throw new InvalidOperationException("포식자 종류 설정이 비어 있거나 중복됩니다.");
                if (profile.type == PredatorKind.Balanced) balanced = profile;
            }
            var random = new System.Random(unchecked(seed ^ 0x2b3174a9)); //미로와 독립적인 종류 선택 난수
            var bag = new List<PredatorTypeProfile>(profiles); //한 번씩 등장하게 섞을 묶음
            int next = 0; //배치할 다음 위치
            if (difficulty == SwimDifficulty.Practice && balanced != null)
            {
                result[next++] = balanced;
                bag.Remove(balanced);
            }
            while (next < count)
            {
                if (bag.Count == 0) bag.AddRange(profiles);
                for (int i = bag.Count - 1; i > 0; i--)
                {
                    int swap = random.Next(i + 1); //뒤쪽과 교환할 난수 위치
                    PredatorTypeProfile saved = bag[i]; //교환할 설정
                    bag[i] = bag[swap];
                    bag[swap] = saved;
                }
                foreach (PredatorTypeProfile profile in bag)
                {
                    if (next == count) break;
                    result[next++] = profile;
                }
                bag.Clear();
            }
            return result;
        }
    }
}
