using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace TeumsaeSwim
{
    public sealed class GameUIComponent : MonoBehaviour
    {
        [SerializeField] private SwimArt art; //기존 공용 글꼴과 도형
        [SerializeField] private GameUIStyleComponent style; //그림과 화면 비율 구성 요소
        [SerializeField] private GameHUDComponent hud; //실시간 정보와 조작 구성 요소
        [SerializeField] private GameMenuComponent menus; //홈과 안내 및 결과 구성 요소
        public Rect playfield => style.playfield; //플레이어가 가려지지 않는 화면 범위
        public string cargoText => hud.cargoText; //현재 운반 안내
        public string cargoResult => menus.cargoResult; //결과 운반 안내

        public void initialize(SwimGame game, Player player) //UI 구성 요소의 연결과 초기화 조율
        {
            style.initialize(art);
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Swim input events", typeof(EventSystem), typeof(InputSystemUIInputModule)); //UI 이벤트
                events.transform.SetParent(transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            hud.initialize(game, player, style);
            menus.initialize(game, player, style);
            Canvas.ForceUpdateCanvases();
            style.layout(new Vector2(Screen.width, Screen.height), Screen.safeArea);
        }

        public void showPhase() //게임 상태에 따른 화면 전환 조율
        {
            hud.showPhase();
            menus.showPhase();
        }

        public void tick(float deltaTime) //현재 플레이 정보 갱신 위임
        {
            hud.tick(deltaTime);
        }

        public void refreshSound() //여러 화면의 소리 버튼 동기화
        {
            menus.refreshSound();
        }

        public static string formatTime(float time) //소수점 한 자리의 플레이 기록
        {
            return Mathf.FloorToInt(time / 60).ToString("00") + ":" + (time % 60).ToString("00.0", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string clockTime(float time) //목표 시간의 분과 초 표시
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0, time)); //남은 목표 초
            return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }

        public static string difficultyName(SwimDifficulty value) //공통 난이도 한국어 이름
        {
            return value == SwimDifficulty.Practice ? "연습" : value == SwimDifficulty.Normal ? "기본" : "도전";
        }
    }
}
