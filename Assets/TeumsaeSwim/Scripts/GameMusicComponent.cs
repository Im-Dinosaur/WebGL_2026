using UnityEngine;

namespace TeumsaeSwim
{
    public sealed class GameMusicComponent : MonoBehaviour
    {
        [SerializeField] private AudioSource source; //배경음악 전용 재생 장치
        [SerializeField] private AudioClip calmLoop; //CC0 바닷속 배경음 원본
        [SerializeField, Range(0f, 1f)] private float volume = .16f; //평온한 상태의 음악 크기
        [SerializeField, Min(.05f)] private float fadeSpeed = .18f; //위험에 따른 음량 변화 속도
        private bool paused; //이어 재생할 일시정지 상태
        public AudioClip track => calmLoop; //연결 검증에 제공할 배경음

        public void initialize(bool muted) //음악 파일과 반복 재생 및 초기 음량 연결
        {
            source.clip = calmLoop;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = volume;
            source.mute = muted;
            paused = false;
        }

        public void setPhase(SwimPhase phase) //정지 화면에서는 멈추고 브라우저 사용자 입력 뒤 재생
        {
            if (phase == SwimPhase.Paused) { source.Pause(); paused = true; return; }
            if (paused) { source.UnPause(); paused = false; }
            if (phase == SwimPhase.Home) source.volume = volume;
            if (!source.isPlaying && calmLoop != null) source.Play();
        }

        public void tick(float deltaTime, float danger) //위험할 때 음악을 낮춰 박동과 위협음 강조
        {
            source.volume = Mathf.MoveTowards(source.volume, volume * Mathf.Lerp(1f, .45f, Mathf.Clamp01(danger)), fadeSpeed * deltaTime);
        }

        public void setMuted(bool muted) //음악 재생 위치를 유지하는 전체 음소거
        {
            source.mute = muted;
        }
    }
}
