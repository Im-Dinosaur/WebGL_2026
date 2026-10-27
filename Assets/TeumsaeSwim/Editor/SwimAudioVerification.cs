using System;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TeumsaeSwim.Editor
{
    public static class SwimAudioVerification
    {
        private static int checks; //통과한 사운드 검사 수

        public static void run() //음악 연결과 파형 및 위험 신호와 전체 음소거 검사
        {
            checks = 0;
            bool hadMute = PlayerPrefs.HasKey("teumsae.muted"); //검사 전 사용자 음소거 설정 유무
            int savedMute = PlayerPrefs.GetInt("teumsae.muted"); //원래 음소거 설정
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            try
            {
                PlayerPrefs.SetInt("teumsae.muted", 0);
                SwimGame game = UnityEngine.Object.FindFirstObjectByType<SwimGame>(); //실제 게임 진입점
                Player player = UnityEngine.Object.FindFirstObjectByType<Player>(); //효과음 전달 대상
                typeof(Player).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
                typeof(SwimGame).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
                GameAudioComponent audio = game.GetComponent<GameAudioComponent>(); //효과음과 상태 조율
                GameMusicComponent music = game.GetComponent<GameMusicComponent>(); //배경음 재생
                AudioSource musicSource = field<AudioSource>(music, "source"); //연결된 배경음 장치
                AudioSource netSource = field<AudioSource>(audio, "netSource"); //연결된 마찰음 장치
                AudioSource effects = field<AudioSource>(audio, "source"); //일회성 효과 장치
                require(music.track != null && music.track.length > 10f && music.track.channels == 1 && music.track.frequency > 0, "Imported calm mono music: " + (music.track == null ? "missing" : music.track.length + " seconds, " + music.track.channels + " channels, " + music.track.frequency + " Hz"));
                require(musicSource.clip == music.track && musicSource.loop && netSource.loop && netSource.clip.name == "Swim Tear", "Music and net have separate looping sources");
                GameSoundSynthesisComponent synthesis = game.GetComponent<GameSoundSynthesisComponent>(); //파형 검사 대상
                foreach (SwimCue cue in Enum.GetValues(typeof(SwimCue)))
                {
                    AudioClip clip = synthesis.createCue(cue); //각 행동의 실제 파형
                    var samples = new float[clip.samples]; //전체 효과음 샘플
                    clip.GetData(samples, 0);
                    float peak = 0f; //무음과 과도한 진폭 검사
                    foreach (float value in samples)
                    {
                        if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidOperationException("Non-finite audio sample");
                        peak = Mathf.Max(peak, Mathf.Abs(value));
                    }
                    require(peak > .05f && peak <= .851f && Mathf.Abs(samples[0]) < .001f && Mathf.Abs(samples[samples.Length - 1]) < .005f, "Non-silent finite click-free cue: " + cue);
                    UnityEngine.Object.DestroyImmediate(clip);
                }
                game.begin(SwimDifficulty.Normal, false);
                audio.tick(.01f, 10f, false, false, false);
                require(field<float>(audio, "heartbeatRemaining") == 0f, "No heartbeat beyond radius");
                audio.tick(.01f, 6f, false, false, false);
                float slowInterval = field<float>(audio, "heartbeatRemaining"); //멀 때 실제 다음 박동 간격
                require(slowInterval > .9f, "Distant predator starts slow heartbeat");
                game.home();
                game.begin(SwimDifficulty.Normal, false);
                audio.tick(.01f, 1f, false, false, false);
                require(field<float>(audio, "heartbeatRemaining") < slowInterval && field<float>(audio, "heartbeatRemaining") >= .48f, "Closer predator accelerates heartbeat");
                audio.tick(.01f, 1f, false, true, false);
                require(field<float>(audio, "heartbeatRemaining") == 0f, "Safe rooms suppress heartbeat");
                audio.play(SwimCue.Notice);
                audio.play(SwimCue.Chase);
                require(field<float>(audio, "threatRemaining") == 1.25f, "Chase threat overrides notice cue");
                audio.play(SwimCue.Chase);
                require(field<float>(audio, "threatRemaining") == 1.25f, "Simultaneous predators share threat cooldown");
                float normalVolume = musicSource.volume; //위험 전 음악 음량
                audio.tick(.5f, 1f, true, false, true);
                require(musicSource.volume < normalVolume, "Threat makes space in background music");
                game.pause();
                audio.tick(10f, 1f, true, false, true);
                require(field<float>(audio, "heartbeatRemaining") == 0f && field<float>(audio, "threatRemaining") == 0f, "Pause clears and freezes danger cues");
                game.resume();
                game.toggleMute();
                require(audio.muted && effects.mute && netSource.mute && musicSource.mute, "Mute covers music and every effect source");
                audio.tick(1f, 1f, true, false, true);
                require(field<float>(audio, "heartbeatRemaining") == 0f, "Muted proximity produces no heartbeat");
                game.toggleMute();
                require(!audio.muted && !effects.mute && !netSource.mute && !musicSource.mute, "Unmute restores all source settings");
                Debug.Log("SUMBAKKOGI_AUDIO_VERIFIED: " + checks + " checks");
            }
            finally
            {
                if (hadMute) PlayerPrefs.SetInt("teumsae.muted", savedMute);
                else PlayerPrefs.DeleteKey("teumsae.muted");
                PlayerPrefs.Save();
                EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            }
        }

        private static T field<T>(object target, string name) //테스트에서만 읽는 내부 재생 상태
        {
            return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private static void require(bool value, string message) //검증 실패를 배치 오류로 전달
        {
            if (!value) throw new InvalidOperationException(message);
            checks++;
        }
    }
}
