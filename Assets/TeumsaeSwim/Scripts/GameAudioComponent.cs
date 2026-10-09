using System.Collections.Generic;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class GameAudioComponent : MonoBehaviour
    {
        [SerializeField] private AudioSource source; //효과음 재생 장치
        [SerializeField, Range(0, 1)] private float volume = .22f; //전체 효과음 크기
        private readonly Dictionary<SwimCue, AudioClip> clips = new Dictionary<SwimCue, AudioClip>(); //직접 합성한 효과음
        public bool muted { get; private set; } //사용자가 저장한 음소거 상태

        public void initialize() //외부 파일 없이 간단한 효과음 합성
        {
            muted = PlayerPrefs.GetInt("teumsae.muted", 0) == 1;
            foreach (SwimCue cue in System.Enum.GetValues(typeof(SwimCue)))
            {
                float duration = cue == SwimCue.Win ? .65f : cue == SwimCue.Lose ? .45f : .18f; //효과음 길이
                const int sampleRate = 22050; //합성 샘플 빈도
                var samples = new float[Mathf.CeilToInt(duration * sampleRate)]; //합성 파형
                float frequency = 280f + (int)cue * 74f; //효과음 기본 높이
                for (int i = 0; i < samples.Length; i++)
                {
                    float time = (float)i / sampleRate; //샘플의 시간
                    float envelope = Mathf.Sin(Mathf.PI * i / samples.Length) * Mathf.Exp(-time * 4f); //부드러운 음량 외피
                    float shift = cue == SwimCue.Lose ? -180f : cue == SwimCue.Win ? 350f : 90f; //음높이 변화
                    samples[i] = Mathf.Sin(2f * Mathf.PI * (frequency * time + shift * time * time)) * envelope * .5f;
                }
                var clip = AudioClip.Create("Swim " + cue, samples.Length, 1, sampleRate, false); //재생할 효과음
                clip.SetData(samples, 0);
                clips.Add(cue, clip);
            }
        }

        public void play(SwimCue cue) //음소거 상태를 반영한 효과음 재생
        {
            if (!muted && clips.TryGetValue(cue, out AudioClip clip)) source.PlayOneShot(clip, volume);
        }

        public void toggleMute() //음소거 상태 저장
        {
            muted = !muted;
            if (muted) source.Stop();
            PlayerPrefs.SetInt("teumsae.muted", muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnDestroy() //합성 오디오 자원 해제
        {
            foreach (AudioClip clip in clips.Values) Destroy(clip);
        }
    }
}
