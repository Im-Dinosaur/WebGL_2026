using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class GameSoundSynthesisComponent : MonoBehaviour
    {
        [SerializeField, Range(16000, 48000)] private int sampleRate = 22050; //짧은 효과음의 샘플 빈도

        public AudioClip createCue(SwimCue cue) //외부 샘플 없이 발사와 그물 및 위험 신호 합성
        {
            float duration = cue == SwimCue.Tear ? .8f : cue == SwimCue.Open ? .4f : cue == SwimCue.Bait ? .24f : cue == SwimCue.Heartbeat ? .4f : cue == SwimCue.Chase ? .65f : cue == SwimCue.Win ? .65f : cue == SwimCue.Lose ? .45f : .18f; //소리별 길이
            var samples = new float[Mathf.CeilToInt(duration * sampleRate)]; //단일 채널 파형
            var random = new System.Random(4501 + (int)cue * 71); //재현 가능한 잡음
            float lowNoise = 0f; //거친 잡음을 다듬는 필터
            float slowNoise = 0f; //바람과 마찰의 대역을 나누는 필터
            float phase = 0f; //연속되는 발사음 높이
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / sampleRate; //샘플 시각
                float progress = (float)i / (samples.Length - 1); //전체 재생 비율
                float noise = (float)random.NextDouble() * 2f - 1f; //백색 잡음
                lowNoise = Mathf.Lerp(lowNoise, noise, .38f);
                slowNoise = Mathf.Lerp(slowNoise, noise, .035f);
                float envelope = Mathf.Max(0f, Mathf.Sin(Mathf.PI * progress)); //끝점 부동소수 오차와 클릭 방지
                float value; //현재 샘플
                if (cue == SwimCue.Bait)
                {
                    phase += 2f * Mathf.PI * Mathf.Lerp(1100f, 180f, progress) / sampleRate;
                    value = ((lowNoise - slowNoise) * 1.9f + Mathf.Sin(phase) * .13f) * Mathf.Pow(envelope, 1.4f) * .6f;
                }
                else if (cue == SwimCue.Tear || cue == SwimCue.Open)
                {
                    float grains = .25f + .75f * Mathf.Pow(Mathf.Sin(time * (cue == SwimCue.Open ? 83f : 49f)), 4); //섬유가 스치는 마찰 묶음
                    value = (lowNoise - slowNoise) * grains * envelope * (cue == SwimCue.Open ? 1.7f : 1.05f);
                }
                else if (cue == SwimCue.Heartbeat)
                    value = heartbeatPulse(time, 0f, 62f) * .65f + heartbeatPulse(time, .17f, 48f) * .43f;
                else if (cue == SwimCue.Chase)
                {
                    float pitch = 115f * time - 42f * time * time; //낮게 내려가는 짧은 위협음
                    value = (Mathf.Sin(2f * Mathf.PI * pitch) * .48f + Mathf.Sin(2f * Mathf.PI * pitch * 1.51f) * .18f + slowNoise * 1.1f) * envelope;
                }
                else
                {
                    float frequency = 280f + (int)cue * 74f; //기존 알림의 기본 높이
                    float shift = cue == SwimCue.Lose ? -180f : cue == SwimCue.Win ? 350f : 90f; //성공과 실패의 높낮이
                    value = Mathf.Sin(2f * Mathf.PI * (frequency * time + shift * time * time)) * envelope * Mathf.Exp(-time * 4f) * .5f;
                }
                samples[i] = Mathf.Clamp(value, -.85f, .85f);
            }
            var clip = AudioClip.Create("Swim " + cue, samples.Length, 1, sampleRate, false); //재생할 효과음
            clip.SetData(samples, 0);
            return clip;
        }

        private float heartbeatPulse(float time, float start, float frequency) //두 번의 짧고 둔탁한 박동 중 한 번 합성
        {
            float elapsed = time - start; //개별 박동 시간
            if (elapsed < 0f || elapsed > .18f) return 0f;
            float envelope = Mathf.Min(1f, elapsed / .008f) * Mathf.Exp(-elapsed * 29f); //부드러운 시작과 빠른 감쇠
            return Mathf.Sin(2f * Mathf.PI * frequency * elapsed) * envelope;
        }
    }
}
