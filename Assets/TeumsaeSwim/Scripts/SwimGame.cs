using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TeumsaeSwim
{
    public sealed class SwimGame : MonoBehaviour
    {
        [SerializeField] private GameSessionComponent session; //단계와 기록 구성 요소
        [SerializeField] private Maze maze; //미로 진입점
        [SerializeField] private Player player; //플레이어 진입점
        [SerializeField] private Predator predatorPrefab; //포식자 조립 프리팹
        [SerializeField] private PredatorRosterComponent roster; //난이도와 시드에 맞춘 포식자 종류 구성
        [SerializeField] private CameraFollowComponent cameraFollow; //스크롤 카메라 구성 요소
        [SerializeField] private GameUIComponent ui; //화면과 조작 구성 요소
        [SerializeField] private GameAudioComponent audioComponent; //효과음 구성 요소
        private readonly List<Predator> predators = new List<Predator>(); //이번 판 포식자
        private int seed; //재도전에 사용할 미로 시드
        public SwimPhase phase => session.phase; //현재 단계
        public float elapsed => session.elapsed; //현재 플레이 시간
        public SwimDifficulty difficulty => session.difficulty; //현재 난이도
        public bool newRecord => session.newRecord; //최고 기록 달성 여부
        public bool cargoDelivered => session.cargoDelivered; //결과 화면과 향후 별 평가에 제공할 운반 결과
        public float timeGoal => session.timeGoal; //현재 시간 별 목표
        public bool timeStar => session.timeStar; //시간 별 달성 결과
        public int stars => session.stars; //이번 판의 획득 별
        public IReadOnlyList<PredatorTypeProfile> predatorTypes => roster.types; //도움말에 제공할 종류별 설정
        public bool muted => audioComponent.muted; //현재 음소거 여부
        public int predatorCountFor(SwimDifficulty value) => maze.predatorCountFor(value); //메뉴에 제공할 실제 포식자 수
        public float progress => maze.data == null ? 0f : maze.progressAt(player.position); //남은 통로 거리에 따른 진행률
        public string danger { get; private set; } = ""; //가장 높은 위험 상태 안내

        private void Start() //화면과 효과음 초기화
        {
            Application.targetFrameRate = 60;
            Time.timeScale = 1f;
            audioComponent.initialize();
            player.cue += audioComponent.play;
            ui.initialize(this, player);
            ui.showPhase();
            audioComponent.setPhase(phase);
        }

        private void OnDestroy() //플레이어 이벤트 연결 해제
        {
            if (player != null && audioComponent != null) player.cue -= audioComponent.play;
        }

        private void Update() //진행 중 시스템의 호출 순서 관리
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (phase == SwimPhase.Playing) pause();
                else if (phase == SwimPhase.Paused) resume();
            }
            if (phase != SwimPhase.Playing) return;
            float deltaTime = Mathf.Min(Time.deltaTime, .05f); //긴 프레임에서의 급격한 이동 방지
            maze.tick(deltaTime, player.position);
            player.tick(deltaTime);
            bool caught = false; //이번 프레임 접촉 결과
            int dangerRank = 0; //화면에 표시할 위험 단계
            float nearestPredator = float.PositiveInfinity; //가장 가까운 포식자 거리
            foreach (Predator predator in predators)
            {
                predator.tick(deltaTime);
                nearestPredator = Mathf.Min(nearestPredator, Vector2.Distance(player.position, predator.position));
                caught |= !player.safe && !player.hidden && Vector2.Distance(player.position, predator.position) < player.radius + predator.radius;
                dangerRank = Mathf.Max(dangerRank, predator.state == PredatorState.Chase ? 3 : predator.state == PredatorState.Notice ? 2 : predator.state == PredatorState.Return ? 1 : 0);
            }
            danger = dangerRank == 3 ? "추격 중! 풀숲에 숨거나 바위 뒤로 피하자" : dangerRank == 2 ? "들키는 중! 시야를 벗어나자" : dangerRank == 1 ? "포식자가 순찰로 돌아가는 중" : "";
            bool escaped = Vector2.Distance(player.position, maze.data.center(maze.data.exit)) < .65f; //출구 도달 결과
            audioComponent.tick(deltaTime, nearestPredator, dangerRank == 3, player.safe, player.working);
            session.advance(deltaTime, caught, escaped, player.hasCargo);
            cameraFollow.setPlayfield(ui.playfield);
            cameraFollow.tick(deltaTime);
            ui.tick(deltaTime);
            if (phase == SwimPhase.Won || phase == SwimPhase.Lost)
            {
                player.resetInput();
                session.saveRecord();
                audioComponent.setPhase(phase);
                audioComponent.play(phase == SwimPhase.Won ? SwimCue.Win : SwimCue.Lose);
                ui.showPhase();
            }
        }

        public void begin(SwimDifficulty value, bool newMaze = true) //새 게임 또는 같은 미로 재도전
        {
            foreach (Predator predator in predators)
            {
                predator.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(predator.gameObject);
                else DestroyImmediate(predator.gameObject);
            }
            predators.Clear();
            if (newMaze) seed = (int)(DateTime.UtcNow.Ticks & 0x7fffffff);
            maze.initialize(seed, value);
            player.initialize(maze);
            PredatorTypeProfile[] types = roster.createRoster(maze.data.seed, maze.data.homes.Count, value); //같은 미로에서는 같은 종류 순서
            for (int i = 0; i < maze.data.homes.Count; i++)
            {
                Predator predator = Instantiate(predatorPrefab, transform); //이번 판의 포식자
                predator.name = types[i].title + " " + (i + 1);
                predator.initialize(maze, player, maze.data.homes[i], value, seed + i, types[i]);
                predator.cue += audioComponent.play;
                predators.Add(predator);
            }
            session.begin(value);
            audioComponent.setPhase(phase);
            danger = "";
            cameraFollow.setPlayfield(ui.playfield);
            cameraFollow.initialize(player, maze);
            ui.showPhase();
            ui.tick(0);
        }

        public void pause() //모든 게임 시간을 멈추고 입력 해제
        {
            session.pause();
            audioComponent.setPhase(phase);
            player.resetInput();
            ui.showPhase();
        }

        public void resume() //정지된 판 재개
        {
            player.resetInput();
            session.resume();
            audioComponent.setPhase(phase);
            ui.showPhase();
        }

        public void home() //현재 판을 종료하고 시작 화면 표시
        {
            session.home();
            audioComponent.setPhase(phase);
            player.resetInput();
            ui.showPhase();
        }

        public void toggleMute() //음소거 진입점
        {
            audioComponent.toggleMute();
            ui.refreshSound();
        }

        public float bestTime(SwimDifficulty value) //난이도별 기록 진입점
        {
            return session.bestTime(value);
        }

        public float timeGoalFor(SwimDifficulty value) //홈에서 선택한 난이도의 시간 목표 조회
        {
            return session.timeGoalFor(value);
        }

        private void OnApplicationFocus(bool focused) //다른 창으로 이동할 때 자동 일시정지
        {
            if (!focused && phase == SwimPhase.Playing) pause();
        }

        private void OnApplicationPause(bool paused) //모바일 앱 전환 시 자동 일시정지
        {
            if (paused && phase == SwimPhase.Playing) pause();
        }
    }
}
