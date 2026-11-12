# 감염 구역

플레이어가 최초 감염원으로 등장하는 2D 탑다운 CQB 프로토타입입니다. 폐쇄 진료소와 격리 통제실을 순서대로 플레이합니다. 스테이지마다 시민 전원 감염 또는 출구에서의 탈출로 통과합니다. 경비의 공격으로 체력이 0이 되면 게임 오버입니다.

## 다른 PC에서 이어서 작업하기

Unity Hub에서 **6000.3.23f1**과 **WebGL Build Support** 모듈을 설치합니다. 새 PC에서는 다음 명령으로 작업 브랜치를 받습니다.

    git clone --branch zombie-cqb https://github.com/Im-Dinosaur/WebGL_2026.git

이미 저장소를 받아 둔 경우 현재 작업을 정리한 뒤 `git fetch origin`과 `git switch zombie-cqb`를 실행합니다. 로컬 브랜치가 처음이라면 `git switch --track origin/zombie-cqb`를 사용합니다.

Unity Hub에서 `WebGL_2026` 저장소의 루트 폴더를 추가합니다. 패키지 임포트가 끝나면 [ZombieCQB.unity](Scenes/ZombieCQB.unity)를 열고 Play를 누릅니다. 씬의 플레이어, 맵 프리팹, UI 참조는 연결되어 있습니다.

새 게임 소스는 `Assets/ZombieCQB/`, 브라우저용 결과는 `docs/zombie-cqb/`에 있습니다. Unity가 생성하는 `Library/`, `Temp/`는 새 PC에서 다시 생성됩니다. 새 자산을 커밋할 때 Unity의 `.meta` 파일도 함께 포함합니다.

## 플레이

| 키 | 동작 |
| --- | --- |
| `WASD` 또는 방향키 | 이동 |
| `E` | 문 열기와 닫기, 출구에서 탈출 |
| `Space` 길게 누르기 | 가까운 시민의 접촉 감염 진행 |
| `Q` | 액체 감염 영역 남기기 |
| `R` | 바라보는 방향에 비말 감염 사용 |
| `F` | 은신 |
| `Shift` | 돌진 |
| `C` | 마우스 위치로 감염된 좀비 이동 지시 |

각 맵에서 원하는 만큼 탐색할 수 있습니다. 벽과 닫힌 문은 이동, 시야와 감염 판정을 막습니다. 색으로 표시되는 발소리 표식은 시야 밖에서 움직이는 시민이나 경비의 방향을 알려줍니다.

기본 최대 체력은 160입니다. 스테이지를 통과하면 아래 세 분류의 항목 중 하나를 선택합니다. 현재 체력, 누적 경험치, 고른 능력은 다음 스테이지에 이어집니다. 시민을 처음 감염시켰을 때마다 경험치가 1씩 쌓입니다.

| 강화 분류 | 항목과 효과 |
| --- | --- |
| 기본 능력치 | 최대 체력은 단계마다 최대·현재 체력에 35를 더합니다. 이동 속도와 청력은 각각 이동 속도와 발소리 방향 감지 거리를 올립니다. |
| 감염 능력 | 접촉 감염 속도는 필요한 시간을 줄입니다. 액체는 밟은 시민에게 효과를 주는 장판, 비말은 전방의 가까운 범위에 적용되는 감염입니다. |
| 특수능력 | 은신은 경비의 탐지를 늦춥니다. 돌진은 바라보는 방향으로 짧게 이동합니다. 좀비 조종은 감염된 시민을 지정 지점으로 보내며 주변에 감염을 퍼뜨립니다. |

액체, 비말, 특수능력은 첫 선택으로 해금됩니다. 다시 고르면 거리나 지속 시간 같은 효과가 강화됩니다. 두 번째 스테이지의 강화 선택을 마치면 결과 화면이 열립니다.

## 수정할 위치

| 위치 | 수정할 내용 |
| --- | --- |
| [ZombieCqbGame.cs](Scripts/ZombieCqbGame.cs), [StageFlowComponent.cs](Scripts/StageFlowComponent.cs) | 플레이 상태, 감염 및 탈출에 따른 스테이지 통과, 강화 선택과 전환 |
| [Player.cs](Scripts/Player.cs), `Player*Component.cs` | 조작과 이동, 체력 및 피격, 청력, 특수능력 |
| [InfectionComponent.cs](Scripts/InfectionComponent.cs), [UpgradeProgressComponent.cs](Scripts/UpgradeProgressComponent.cs) | 접촉·액체·비말 감염 판정, 경험치와 강화 단계 |
| [Stages](Stages), [StageMapComponent.cs](Scripts/StageMapComponent.cs), [IndoorNavigationComponent.cs](Scripts/IndoorNavigationComponent.cs) | 맵별 시민·경비·출구 참조, 문 상태에 따른 이동·시야 |
| [ZombieCqbUIComponent.cs](Scripts/ZombieCqbUIComponent.cs), [ZombieCqbSceneUI.cs](Editor/ZombieCqbSceneUI.cs) | 진행 정보와 강화 화면 |

체력과 이동 속도는 씬의 `Patient Zero`에서 `PlayerStatComponent`의 Inspector를 편집합니다. 감염 속도와 범위는 `Zombie CQB`의 `InfectionComponent`, 피해량과 순찰 위치는 스테이지 프리팹의 `GuardComponent`에서 조절합니다. 맵 구조를 수정할 때는 프리팹의 격자와 바닥 그림을 함께 갱신합니다. 두 맵의 초기 배치는 [ZombieCqbSceneSetup.cs](Editor/ZombieCqbSceneSetup.cs)의 `createLayout`에 있습니다. 추가 맵은 `StageFlowComponent`의 `Stages` 목록에 플레이 순서대로 연결합니다.

## WebGL 빌드

Unity에서 **Zombie CQB → Build WebGL**을 실행하면 전용 씬을 `docs/zombie-cqb/`에 빌드합니다. 웹 화면의 원본은 [ZombieCqbWeb.html](Web/ZombieCqbWeb.html)입니다. 빌드된 게임을 로컬에서 확인하려면 저장소 루트에서 HTTP 서버를 실행합니다. Python을 설치한 PC에서는 다음 명령을 사용할 수 있습니다.

    python -m http.server 8080 --directory docs

브라우저에서 [로컬 감염 구역](http://127.0.0.1:8080/zombie-cqb/)을 열면 됩니다.

## 검증

Unity 6000.3.23f1 Play 모드에서 두 맵의 경로, 문에 따른 시야 차단, 두 스테이지 통과 조건, 아홉 강화 효과와 체력 이월, 게임 오버와 재시작을 확인했습니다. Edge에서 WebGL 빌드를 열어 첫 맵에서 이동하고 탈출한 뒤, 돌진을 선택해 두 번째 맵에서 사용하는 흐름을 확인했습니다.
