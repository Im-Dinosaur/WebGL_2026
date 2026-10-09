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
| 그물 작업 | 그물 앞에 멈춰 ‘꾹’ 유지 | 멈춘 상태로 Space |
| 미끼 | 미끼 버튼 | E |
| 일시정지 | 상단 Ⅱ | Escape |

시간 제한은 없습니다. 60–90초는 초기 플레이 길이의 설계 목표이며 숙련도, 경로와 난이도에 따라 달라집니다. 카메라가 플레이어를 부드럽게 따라가고 미로 경계에서 멈춥니다. 정보와 조작 버튼은 화면에 고정됩니다.

## 주요 규칙

- 연습/기본/도전은 각각 6/7/8개 구역, 포식자 1/2/2마리입니다. 구역의 회랑과 연결 위치가 바뀌며 출구 경로를 생성 후 검사합니다.
- 바위는 이동과 시야를 막습니다. 수풀 안에 몸 전체가 들어가면 숨지만 접촉하면 잡힙니다.
- 좁은 틈은 누른 상태에서만 이동합니다. 안에서 놓으면 작은 자세로 멈춥니다. 포식자는 통과하지 못하며 틈 자체는 투명한 통로입니다.
- 그물은 이동을 막지만 시야는 통과합니다. 멈춰서 누적 2초간 작업하면 열립니다. 중단한 진행도는 유지됩니다.
- 미끼는 2개이며 유효한 장소에 놓을 때만 소모됩니다. 순찰 중인 포식자만 반응합니다.
- 포식자는 연속 1초간 발견하면 추격합니다. 시야를 놓치면 발견 직전 위치로 바로 복귀합니다. 복귀 중 재발견은 새로운 1초 판정으로 시작합니다.
- 같은 프레임에 접촉과 출구 도달이 발생하면 잡힌 결과가 우선합니다.
- 창 전환 시 자동 정지하며 다시 누르기 전까지 진행하지 않습니다. 최고 기록과 음소거는 브라우저의 로컬 PlayerPrefs에 저장됩니다.

## 구조

외부 호출은 `SwimGame`, `Player`, `Predator`, `Maze` 진입점을 통합니다. 각 진입점은 담당 Component를 연결하고 순서대로 호출합니다.

| 진입점 / 구성 요소 | 책임 |
| --- | --- |
| SwimGame / GameSessionComponent | 시스템 진행 순서, 게임 단계, 시간, 승패, 기록 |
| Player / Input, Movement, Interaction, Visual Component | 입력, 헤엄과 틈 통과, 그물과 미끼, 물고기 표시 |
| Predator / AI, Movement, Visual Component | 발견·추격·복귀 판단, 길찾기, 시야 표시 |
| Maze / Generation, Terrain Component | 구역 생성과 연결 검증, 충돌·시야·지형·미끼 |
| CameraFollowComponent | 고정 배율의 플레이어 추적과 맵 경계 제한 |
| GameUIComponent / TouchControlComponent | 고정 UI, 스틱과 누르기 입력 |
| GameAudioComponent / SwimArt | 직접 합성한 효과음, 코드로 만든 도형, 한국어 글꼴 |

Inspector에서 각 Component의 이동 속도, 시야 거리, 발견 시간 등을 조절할 수 있습니다. 프리팹의 포식자 설정은 새 판을 시작할 때 적용됩니다.

## 검증과 빌드

Unity 메뉴 `Teumsae Swim`에 씬 생성, 규칙 검사, WebGL 빌드가 있습니다. **씬 생성 메뉴는 전용 씬과 포식자 프리팹을 재조립하므로 수동 편집 후에는 필요한 경우에만 사용하세요.** 규칙 검사는 임시 실행 상태를 만든 뒤 저장하지 않고 원래 씬을 다시 엽니다.

검사는 600개의 생성 미로와 충돌, 틈, 그물, 미끼, 은신, 발견·복귀, 일시정지, 접촉 우선 결과 규칙을 확인합니다. 세 난이도에서 실제 이동과 상호작용 규칙으로 경로를 통과하는지도 검사합니다. 보고서는 Git에서 제외되는 `Logs/TeumsaeSwim/verification.txt`에 저장됩니다.

빌드는 전용 씬만 포함하며 `docs/teumsae-swim`에 출력합니다. 기존 EditorBuildSettings의 실습 씬 목록을 바꾸지 않고 일시적으로 바꾼 PlayerSettings는 복구합니다. GitHub Pages의 정적 호스팅을 위해 Gzip과 Decompression Fallback을 사용합니다. 웹 템플릿은 모바일 세로 비율, 로딩 진행도, 오류 후 재시도 화면을 제공합니다.

WebGL 파일은 파일 탐색기에서 직접 열지 말고 HTTP 서버나 게시 주소로 접속합니다. `main`의 `/docs`를 사용하는 기존 GitHub Pages 설정에서는 해당 폴더의 커밋이 게시됩니다.

## 자원

게임 그림은 코드로 생성한 도형이며 효과음은 사인파 합성으로 생성됩니다. 한국어 글꼴은 Noto Sans KR입니다. 글꼴의 SIL Open Font License는 `Fonts/OFL.txt`에 포함되어 있습니다.

- Noto 원본: https://github.com/notofonts/noto-cjk/tree/main/Sans/SubsetOTF/KR
- Unity Web 배포 안내: https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-deploying.html
