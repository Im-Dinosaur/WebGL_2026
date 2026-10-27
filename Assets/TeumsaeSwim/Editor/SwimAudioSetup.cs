using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TeumsaeSwim.Editor
{
    public static class SwimAudioSetup
    {
        public static void connect() //승인한 음악 파일과 효과음 구성 요소 및 재생 장치만 연결
        {
            AssetDatabase.Refresh();
            const string path = "Assets/TeumsaeSwim/Audio/UnderwaterAmbientPad.ogg"; //원본을 보존한 배경음
            var importer = (AudioImporter)AssetImporter.GetAtPath(path); //모바일용 음악 설정
            importer.forceToMono = true;
            AudioImporterSampleSettings audioSettings = importer.defaultSampleSettings; //기존 플랫폼 선택을 보존할 샘플 설정
            audioSettings.loadType = AudioClipLoadType.DecompressOnLoad;
            audioSettings.compressionFormat = AudioCompressionFormat.Vorbis;
            audioSettings.quality = .5f;
            audioSettings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
            audioSettings.sampleRateOverride = 22050;
            importer.defaultSampleSettings = audioSettings;
            importer.SaveAndReimport();
            EditorSceneManager.OpenScene(SwimProjectSetup.scenePath);
            GameAudioComponent audio = Object.FindFirstObjectByType<GameAudioComponent>(); //기존 사운드 진입점
            GameMusicComponent music = audio.GetComponent<GameMusicComponent>(); //파일 배경음 구성 요소
            if (music == null) music = audio.gameObject.AddComponent<GameMusicComponent>();
            GameSoundSynthesisComponent synthesis = audio.GetComponent<GameSoundSynthesisComponent>(); //효과음 제작 구성 요소
            if (synthesis == null) synthesis = audio.gameObject.AddComponent<GameSoundSynthesisComponent>();
            setReference(music, "source", ensureSource(audio.transform, "Calm music"));
            setReference(music, "calmLoop", AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            setReference(audio, "music", music);
            setReference(audio, "synthesis", synthesis);
            setReference(audio, "netSource", ensureSource(audio.transform, "Net rustle"));
            EditorSceneManager.MarkSceneDirty(audio.gameObject.scene);
            EditorSceneManager.SaveScene(audio.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("SUMBAKKOGI_AUDIO_CONNECTED");
        }

        public static void connectVerifyAndBuild() //새 사운드와 메뉴 대비를 검증하고 배포용 빌드
        {
            connect();
            SwimUIVerification.run();
            verifyAndBuild();
        }

        public static void verifyAndBuild() //이미 연결한 오디오만 검증하고 최종 빌드
        {
            SwimAudioVerification.run();
            SwimUIPreview.capture();
            SwimProjectSetup.build();
        }

        private static AudioSource ensureSource(Transform parent, string name) //음악과 작업 루프 전용 재생 장치 재사용
        {
            Transform child = parent.Find(name); //기존 재생 오브젝트
            if (child == null)
            {
                child = new GameObject(name, typeof(AudioSource)).transform;
                child.SetParent(parent, false);
            }
            AudioSource source = child.GetComponent<AudioSource>(); //2D 배경음과 효과 재생 장치
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            return source;
        }

        private static void setReference(Object target, string field, Object value) //다른 Inspector 값을 보존하며 참조 연결
        {
            var settings = new SerializedObject(target); //직렬화 대상
            settings.FindProperty(field).objectReferenceValue = value;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
