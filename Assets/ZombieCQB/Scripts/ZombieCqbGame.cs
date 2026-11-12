using UnityEngine;

namespace ZombieCqb
{
    public sealed class ZombieCqbGame : MonoBehaviour
    {
        [SerializeField] private Player player; //플레이어 기능의 진입점
        [SerializeField] private StageFlowComponent flow; //스테이지 순서와 통과 판정
        [SerializeField] private InfectionComponent infection; //감염 방식과 대상 판정
        [SerializeField] private UpgradeProgressComponent progress; //경험치와 강화 단계
        [SerializeField] private ZombieCqbUIComponent ui; //조작 안내와 진행 화면
        [SerializeField] private Camera gameCamera; //실내 맵을 보여주는 카메라

        private StageMapComponent map; //현재 사용 중인 스테이지
        private float hudDelay; //HUD 수치 갱신까지 남은 시간
        private float cameraAspect; //이전 화면 비율

        private void Awake() //컴포넌트의 요청 연결
        {
            Application.targetFrameRate = 60;
            infection.infected += recordInfection;
            ui.bind(startRun, chooseUpgrade);
        }

        private void Start() //첫 맵과 시작 화면 준비
        {
            player.resetRun();
            mountStage();
            ui.showTitle();
        }

        private void Update() //플레이 중 조작, 감염, 피격, 스테이지 진행을 조율
        {
            if (map == null)
                return;

            if (!Mathf.Approximately(gameCamera.aspect, cameraAspect))
                fitCamera();
            if (flow.phase != RunPhase.Playing)
                return;

            float deltaTime = Mathf.Min(Time.deltaTime, 0.05f); //낮은 프레임에서 이동을 제한할 프레임 시간
            PlayerInputFrame controls = player.readInput(); //이번 프레임의 플레이어 조작
            player.tickMovement(controls, deltaTime, map.navigation, progress);
            bool canExit = map.canExit(player.position); //플레이어가 출구를 사용할 수 있는 상태
            bool escaped = controls.interact && canExit; //이번 프레임의 탈출 요청
            DoorComponent door = map.getNearbyDoor(player.position); //플레이어가 조작할 수 있는 문

            if (!escaped)
            {
                if (controls.interact)
                    map.useDoor(door, player.position, player.radius);
                if (controls.command)
                    commandZombies(controls.pointer);

                infection.tick(deltaTime, controls, player.position, player.facing, map, progress);
                foreach (CivilianComponent civilian in map.people) //이번 프레임에 움직일 시민과 좀비
                    civilian.tick(deltaTime, player.position, map.navigation);
                foreach (GuardComponent guard in map.security) //이번 프레임의 경비 행동과 공격
                    player.receiveAttack(guard.tick(deltaTime, player.position, player.detectionMultiplier, map.navigation));
            }

            map.updateVisibility(player.position, deltaTime);
            player.updateHearing(map);
            flow.resolve(player.isAlive, escaped, map.people.Length);
            if (flow.phase == RunPhase.Upgrade)
            {
                refreshHud();
                ui.showUpgrades(flow.outcome, flow.stageIndex + 1, progress);
            }
            else if (flow.phase == RunPhase.Dead)
            {
                refreshHud();
                ui.showResult(false, flow.stageIndex + 1, progress.experience);
            }
            else
            {
                hudDelay -= deltaTime;
                if (hudDelay <= 0f)
                {
                    hudDelay = 0.08f;
                    refreshHud();
                }
            }
        }

        private void startRun() //시작 화면 또는 결과에서 새 플레이 실행
        {
            bool firstStart = flow.phase == RunPhase.Title; //이미 첫 맵이 준비된 시작 상태
            progress.resetProgress();
            player.resetRun();
            flow.startRun();
            if (!firstStart)
                mountStage();

            ui.showGameplay();
            refreshHud();
        }

        private void chooseUpgrade(UpgradeId id) //스테이지 보상 한 항목을 적용하고 다음 화면으로 전환
        {
            if (flow.phase != RunPhase.Upgrade || !progress.tryUpgrade(id))
                return;

            player.applyUpgrades(progress);
            if (flow.advance())
            {
                mountStage();
                ui.showGameplay();
                refreshHud();
            }
            else
            {
                refreshHud();
                ui.showResult(true, flow.stageCount, progress.experience);
            }
        }

        private void mountStage() //현재 맵 프리팹을 생성하고 플레이어와 카메라 연결
        {
            if (map != null)
            {
                map.gameObject.SetActive(false);
                Destroy(map.gameObject);
            }

            infection.resetForStage();
            map = Instantiate(flow.stagePrefab);
            map.name = flow.stagePrefab.name;
            map.initialize();
            player.enterStage(map.startPosition);
            fitCamera();
        }

        private void fitCamera() //브라우저 크기에 맞춰 전체 맵과 HUD 공간 확보
        {
            cameraAspect = gameCamera.aspect;
            float width = map.navigation.width; //맵의 가로 길이
            float height = map.navigation.height; //맵의 세로 길이
            gameCamera.transform.position = new Vector3(width * 0.5f, height * 0.5f, -10f);
            gameCamera.orthographicSize = Mathf.Max((height + 4.6f) * 0.5f, (width + 2.2f) * 0.5f / cameraAspect);
        }

        private void commandZombies(Vector2 pointer) //마우스가 가리키는 지점으로 감염된 시민을 이동
        {
            Vector2 destination = gameCamera.ScreenToWorldPoint(pointer); //지시한 맵 위치
            if (!map.navigation.isOpenCell(IndoorNavigationComponent.cellAt(destination)))
                return;

            float duration = player.tryCommand(progress.getRank(UpgradeId.Command), flow.infectedCount > 0); //지시가 유지될 시간
            if (duration <= 0f)
                return;

            foreach (CivilianComponent civilian in map.people) //이동 명령을 전달할 감염 시민 후보
                civilian.command(destination, duration);
        }

        private void recordInfection() //최초 감염 신호를 시민 수와 경험치에 반영
        {
            flow.recordInfection();
            progress.addInfectionExperience();
        }

        private void refreshHud() //현재 상태를 화면에 전달
        {
            bool detected = false; //추격 중인 경비가 있는지 여부
            foreach (GuardComponent guard in map.security) //경보 상태를 확인할 경비
                detected |= guard.isAlert;

            string hint = string.Empty; //문 또는 출구의 안내 문구
            DoorComponent door = map.getNearbyDoor(player.position); //현재 안내할 문
            if (map.canExit(player.position))
                hint = "E   탈출";
            else if (door != null)
                hint = door.isOpen ? "E   문 닫기" : "E   문 열기";

            ui.setHud(flow.stageIndex + 1, flow.stageCount, map.displayName, player.health, player.maxHealth,
                flow.infectedCount, map.people.Length, progress.experience, detected, player.isHidden, hint);
            for (int index = (int)UpgradeId.Liquid; index <= (int)UpgradeId.Command; index++) //단축키가 있는 해금 능력
            {
                UpgradeId id = (UpgradeId)index; //화면에 표시할 능력
                float cooldown = id <= UpgradeId.Droplet ? infection.getCooldown(id) : player.getCooldown(id); //현재 능력의 재사용 대기
                ui.setAbility(id, progress.getRank(id), cooldown);
            }
        }

        private void OnDestroy() //씬이 닫힐 때 감염 신호 연결 해제
        {
            infection.infected -= recordInfection;
        }
    }
}
