using System.Collections.Generic;
using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class GameAudioComponent : MonoBehaviour
    {
        [SerializeField] private AudioSource source; //일회성 효과음 재생 장치
        [SerializeField] private AudioSource netSource; //그물 마찰음 반복 재생 장치
        [SerializeField] private GameMusicComponent music; //파일 배경음 구성 요소
        [SerializeField] private GameSoundSynthesisComponent synthesis; //효과음 제작 구성 요소
        [SerializeField, Range(0, 1)] private float volume = .22f; //효과음 크기
        [SerializeField, Min(1f)] private float heartbeatRadius = 8f; //포식자 접근을 알리는 거리
        [SerializeField, Min(.4f)] private float slowHeartbeatSeconds = 1.15f; //멀 때 심장박동 간격
        [SerializeField, Min(.4f)] private float fastHeartbeatSeconds = .48f; //가까울 때 심장박동 간격
        private readonly Dictionary<SwimCue, AudioClip> clips = new Dictionary<SwimCue, AudioClip>(); //직접 합성한 효과음
        private SwimPhase phase; //현재 게임 단계
        private float heartbeatRemaining; //다음 심장박동까지 남은 시간
        private float warningRemaining; //경고음의 중첩 방지 시간
        private float threatRemaining; //추격 위협음의 독립 재생 간격
        public bool muted { get; private set; } //저장한 음소거 상태

        public void initialize() //효과음과 음악 및 그물 루프 준비
        {
            source.Stop();
            netSource.Stop();
            releaseClips();
            muted = PlayerPrefs.GetInt("teumsae.muted", 0) == 1;
            foreach (SwimCue cue in System.Enum.GetValues(typeof(SwimCue))) clips.Add(cue, synthesis.createCue(cue));
            source.mute = muted;
            netSource.clip = clips[SwimCue.Tear];
            netSource.loop = true;
            netSource.volume = volume * .8f;
            netSource.mute = muted;
            music.initialize(muted);
            heartbeatRemaining = warningRemaining = threatRemaining = 0f;
        }

        public void setPhase(SwimPhase value) //화면 전환에서 작업 루프와 남은 위험 신호 정리
        {
            phase = value;
            if (phase != SwimPhase.Playing)
            {
                netSource.Stop();
                if (phase == SwimPhase.Paused || phase == SwimPhase.Home) source.Stop();
                heartbeatRemaining = warningRemaining = threatRemaining = 0f;
            }
            music.setPhase(value);
        }

        public void tick(float deltaTime, float nearestPredator, bool chasing, bool safe, bool working) //근접 박동과 그물 작업음을 게임 시간에 맞춰 재생
        {
            if (phase != SwimPhase.Playing) return;
            warningRemaining = Mathf.Max(0f, warningRemaining - deltaTime);
            threatRemaining = Mathf.Max(0f, threatRemaining - deltaTime);
            float proximity = safe ? 0f : Mathf.Clamp01(1f - nearestPredator / heartbeatRadius); //안전구역 밖 접근 정도
            music.tick(deltaTime, chasing ? 1f : proximity);
            if (working && !muted) { if (!netSource.isPlaying) netSource.Play(); }
            else if (netSource.isPlaying) netSource.Stop();
            if (proximity <= 0f || muted) { heartbeatRemaining = 0f; return; }
            heartbeatRemaining -= deltaTime;
            if (heartbeatRemaining > 0f) return;
            source.PlayOneShot(clips[SwimCue.Heartbeat], volume * Mathf.Lerp(.55f, 1.35f, proximity));
            heartbeatRemaining = Mathf.Lerp(slowHeartbeatSeconds, fastHeartbeatSeconds, proximity);
        }

        public void play(SwimCue cue) //행동별 효과음 재생과 다수 포식자 경고음의 중첩 제한
        {
            if (muted || cue == SwimCue.Tear || !clips.TryGetValue(cue, out AudioClip clip)) return;
            if (cue == SwimCue.Notice || cue == SwimCue.Chase)
            {
                if (phase != SwimPhase.Playing || (warningRemaining > 0f && cue != SwimCue.Chase)) return;
                if (cue == SwimCue.Chase && threatRemaining > 0f) return;
                if (cue == SwimCue.Chase) threatRemaining = 1.25f;
                warningRemaining = cue == SwimCue.Chase ? 1.25f : .55f;
            }
            source.PlayOneShot(clip, volume * (cue == SwimCue.Chase ? 1.35f : 1f));
        }

        public void toggleMute() //음악과 일회성 효과 및 작업 루프 음소거 동기화와 저장
        {
            muted = !muted;
            source.mute = netSource.mute = muted;
            if (muted) { source.Stop(); netSource.Stop(); }
            music.setMuted(muted);
            PlayerPrefs.SetInt("teumsae.muted", muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void releaseClips() //다시 초기화하거나 장면을 닫을 때 합성 자원 해제
        {
            foreach (AudioClip clip in clips.Values)
            {
                if (Application.isPlaying) Destroy(clip);
                else DestroyImmediate(clip);
            }
            clips.Clear();
        }

        private void OnDestroy() //합성 오디오 자원 해제
        {
            releaseClips();
        }
    }
}
