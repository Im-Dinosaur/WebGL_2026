# 틈새 헤엄

작은 물고기가 바위와 수풀, 좁은 틈, 그물을 이용해 포식자를 피하고 출구에 도달하는 2D 세로형 WebGL 게임입니다.

- Unity: **6000.3.23f1**, URP 2D, Input System, uGUI
- 전용 씬: `Assets/TeumsaeSwim/Scenes/TeumsaeSwim.unity`
- 웹 빌드: `docs/teumsae-swim/`
- 게시 주소: https://im-dinosaur.github.io/WebGL_2026/teumsae-swim/
- 기존 실습 씬, 기존 Pages 루트의 게임은 별도로 유지합니다.

## 실행과 조작

Unity에서 전용 씬을 열고 Play를 누릅니다. 씬과 프리팹의 참조는 연결되어 있습니다. Game View를 480 × 854 또는 9:16에 가까운 세로 화면으로 설정하면 웹 화면을 확인하기 쉽습니다.

| 행동 | 모바일 | PC |
| --- | --- | --- |
| 헤엄 | 왼쪽 스틱 | WASD / 방향키 |
| 틈 통과 | 오른쪽 ‘꾹’을 누르며 이동 | Space + 이동 |
| 그물 작업 | 그물 근처에서 이동하며 ‘꾹’ 유지 | 이동 + Space |
| 미끼 | 미끼 버튼 | E |
| 일시정지 | 상단 Ⅱ | Escape |

시간 제한은 없습니다. 피드백 반영으로 맵 길이와 분기를 늘렸으며, 플레이 시간은 숙련도와 경로 및 난이도에 따라 달라집니다. 카메라가 플레이어를 부드럽게 따라가고 미로 경계에서 멈춥니다. 정보와 조작 버튼은 화면에 고정됩니다.

## 주요 규칙

- 연습/기본/도전은 각각 8/10/12개 구역, 포식자 2/4/6마리입니다. 가로 폭은 13칸이며 우회 회랑과 미끼를 던질 곁가지가 생성됩니다. 시작 안전 구역을 확보하고 출구 경로를 검사합니다.
- 바위는 이동과 시야를 막습니다. 수풀 안에 몸 전체가 들어가면 시야에서 숨고 포식자와 접촉해도 잡히지 않습니다. 몸이 수풀 밖으로 나오면 보호가 해제됩니다.
- 좁은 틈은 누른 상태에서만 이동합니다. 안에서 놓으면 작은 자세로 멈춥니다. 포식자는 통과하지 못하며 틈 자체는 투명한 통로입니다.
- 그물은 이동을 막지만 시야는 통과합니다. 가까이서 이동과 ‘꾹’을 함께 누르면 누적 2초 후 열립니다. 범위를 벗어나거나 버튼을 놓으면 중단하고 진행도는 유지됩니다. 그물 작업 중에는 몸을 줄이지 않으며, 일반 물길에서 ‘꾹’만 누른다고 느려지지 않습니다.
- 미끼는 2개이며 발사에 성공할 때만 소모됩니다. 바라보는 방향으로 6.6유닛까지 날아가며, 벽·닫힌 그물·포식자가 통과하지 못하는 틈에 닿으면 한 번 반사하여 1.1유닛 이동 후 정지합니다. 정지 후 8초 동안 유지됩니다.
- 미끼가 정지하면 8.8유닛 반경 안에서 접근할 경로가 있는 포식자가 ‘?’를 띄우고 이동합니다. 벽 너머라도 돌아갈 길이 있으면 반응하며, 도착해 먹거나 미끼가 사라지면 순찰 위치로 돌아갑니다. 플레이어를 직접 발견하면 발견·추격을 우선합니다.
- 포식자는 연속 0.9초간 발견하면 추격합니다. 시야를 놓치면 주변 미끼에 반응하거나 발견 직전 순찰 위치로 복귀합니다. 재발견은 새로운 0.9초 판정으로 시작합니다.
- 같은 프레임에 접촉과 출구 도달이 발생하면 잡힌 결과가 우선합니다.
- 창 전환 시 자동 정지하며 다시 누르기 전까지 진행하지 않습니다. 최고 기록과 음소거는 브라우저의 로컬 PlayerPrefs에 저장됩니다.

## 구조

외부 호출은 `SwimGame`, `Player`, `Predator`, `Maze` 진입점을 통합니다. 각 진입점은 담당 Component를 연결하고 순서대로 호출합니다.

| 진입점 / 구성 요소 | 책임 |
| --- | --- |
| SwimGame / GameSessionComponent | 시스템 진행 순서, 게임 단계, 시간, 승패, 기록 |
| Player / Input, Movement, Interaction, Visual Component | 입력, 헤엄과 틈 통과, 그물과 미끼, 물고기 표시 |
| Predator / AI, Movement, Visual Component | 발견·추격·복귀 판단, 길찾기, 시야 표시 |
| Maze / Generation, Terrain, Bait Component | 구역 생성과 연결 검증, 충돌·시야·지형, 미끼 비행과 수명 |
| CameraFollowComponent | 고정 배율의 플레이어 추적과 맵 경계 제한 |
| GameUIComponent / TouchControlComponent | 고정 UI, 스틱과 누르기 입력 |
| GameAudioComponent / SwimArt | 직접 합성한 효과음, 코드로 만든 도형, 한국어 글꼴 |

Inspector에서 각 Component의 이동 속도, 시야 거리, 발견 시간 등을 조절할 수 있습니다. 프리팹의 포식자 설정은 새 판을 시작할 때 적용됩니다.

`MazeBaitComponent`의 `launchSpeed`, `launchDistance`, `bounceDistance`, `attractDistance`, `baitDuration`으로 미끼를 조절합니다. `MazeGenerationComponent`에는 난이도별 구역 수와 포식자 수가 있습니다. 기존 씬에 새 미끼 Component를 연결하는 갱신 메뉴는 `Teumsae Swim/4. Apply feedback update`이며, 씬 전체를 재생성하지 않고 해당 연결과 승인된 난이도·발견 시간 설정만 갱신합니다.

## 검증과 빌드

Unity 메뉴 `Teumsae Swim`에 씬 생성, 규칙 검사, WebGL 빌드가 있습니다. **씬 생성 메뉴는 전용 씬과 포식자 프리팹을 재조립하므로 수동 편집 후에는 필요한 경우에만 사용하세요.** 규칙 검사는 임시 실행 상태를 만든 뒤 저장하지 않고 원래 씬을 다시 엽니다.

검사는 600개의 생성 미로와 충돌, 이동 중 그물 해제, 미끼 발사·벽 반사·모서리 반사, 유인 거리·우회 경로·발견 우선순위, 은신 중 접촉 보호, 0.9초 발견, 일시정지와 결과 규칙을 확인합니다. 세 난이도에서 이동 스틱과 누르기를 함께 사용해 실제 몸체로 출구까지 통과하는지도 검사합니다. 보고서는 Git에서 제외되는 `Logs/TeumsaeSwim/verification.txt`에 저장됩니다.

빌드는 전용 씬만 포함하며 `docs/teumsae-swim`에 출력합니다. 기존 EditorBuildSettings의 실습 씬 목록을 바꾸지 않고 일시적으로 바꾼 PlayerSettings는 복구합니다. GitHub Pages의 정적 호스팅을 위해 Gzip과 Decompression Fallback을 사용합니다. 웹 템플릿은 모바일 세로 비율, 로딩 진행도, 오류 후 재시도 화면을 제공합니다.

WebGL 파일은 파일 탐색기에서 직접 열지 말고 HTTP 서버나 게시 주소로 접속합니다. `main`의 `/docs`를 사용하는 기존 GitHub Pages 설정에서는 해당 폴더의 커밋이 게시됩니다.

## 자원

게임 그림은 코드로 생성한 도형이며 효과음은 사인파 합성으로 생성됩니다. 한국어 글꼴은 Noto Sans KR입니다. 글꼴의 SIL Open Font License는 `Fonts/OFL.txt`에 포함되어 있습니다.

- Noto 원본: https://github.com/notofonts/noto-cjk/tree/main/Sans/SubsetOTF/KR
- Unity Web 배포 안내: https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-deploying.html
