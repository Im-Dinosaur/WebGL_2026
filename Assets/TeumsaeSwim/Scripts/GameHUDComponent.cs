using UnityEngine;
using UnityEngine.UI;

namespace TeumsaeSwim
{
    public sealed class GameHUDComponent : MonoBehaviour
    {
        [SerializeField, Min(20f)] private float baitFullDragDistance = 90f; //미끼 최대 거리까지 드래그할 UI 길이
        [SerializeField, Min(0f)] private float baitCancelRadius = 12f; //미끼 조준을 취소하는 버튼 중앙 반경
        private SwimGame game; //플레이 정보 진입점
        private Player player; //입력과 상호작용 진입점
        private GameUIStyleComponent style; //공통 화면 디자인
        private GameObject panel; //HUD 전체
        private Text timerLabel; //경과 시간
        private Text timeGoalLabel; //시간 별 잔여 시간
        private Text statusLabel; //안전과 위험 상태
        private Text hintLabel; //상황별 조작 안내
        private Text baitLabel; //미끼 개수
        private Text cargoLabel; //진주 운반 상태
        private Text holdCaption; //꾹 동작 설명
        private Text netLabel; //그물 진행 퍼센트
        private Image progressBar; //출구까지의 진행률
        private Image netRing; //꾹 버튼 작업 진행
        private Image holdFace; //꾹 버튼 활성 상태
        private Button baitButton; //남은 미끼가 있을 때만 누르는 버튼
        private GameUIIconComponent timeIcon; //시간 목표 가능 여부
        private GameUIIconComponent cargoIcon; //진주 소지 여부
        private RectTransform netCallout; //플레이어 근처 그물 안내
        private TouchControlComponent stick; //스틱 입력
        private TouchControlComponent hold; //누름 입력
        private TouchBaitAimComponent baitAim; //미끼 드래그 조준 입력
        private UIButtonFeedbackComponent holdFeedback; //꾹 버튼의 누름 상태와 표시 효과
        private float messageTime; //임시 알림 남은 시간
        private string temporaryMessage = ""; //임시 알림
        public string cargoText => cargoLabel.text; //검증과 접근성에 제공하는 실제 진주 안내

        public void initialize(SwimGame owner, Player target, GameUIStyleComponent theme) //실제 게임 상태와 두 손가락 조작 연결
        {
            game = owner;
            player = target;
            style = theme;
            RectTransform root = style.screen("HUD", false, false); //입력을 가로막지 않는 HUD
            panel = root.gameObject;
            createHeader(root);
            createControls(root);
            netCallout = style.card(root, "Net callout", new Vector2(0, 85), new Vector2(168, 40), GameUIStyleComponent.panel, SwimArt.mint).rectTransform;
            netLabel = style.text(netCallout, "", Vector2.zero, new Vector2(156, 30), 15, GameUIStyleComponent.cream, TextAnchor.MiddleCenter, true);
            netCallout.gameObject.SetActive(false);
        }

        private void createHeader(RectTransform root) //제목과 시간 및 별 세 가지 목표
        {
            RectTransform top = style.bar(root, "Top HUD", true, 140); //상단 고정 배치
            style.card(top, "Header", new Vector2(0, -63), new Vector2(456, 112), GameUIStyleComponent.panel, GameUIStyleComponent.border);
            style.logo(top, new Vector2(-123, -34), new Vector2(184, 61));
            style.icon(top, SwimUIIcon.Clock, new Vector2(8, -32), 20, SwimArt.mint);
            style.text(top, "경과", new Vector2(43, -32), new Vector2(40, 25), 13, GameUIStyleComponent.muted);
            timerLabel = style.text(top, "00:00.0", new Vector2(115, -32), new Vector2(105, 36), 23, GameUIStyleComponent.cream, TextAnchor.MiddleCenter, true);
            Button pause = style.button(top, "", new Vector2(197, -32), Vector2.one * 44, () => game.pause()); //멈춤 버튼
            style.icon(pause.GetComponent<RectTransform>(), SwimUIIcon.Pause, new Vector2(0, 4), 20, GameUIStyleComponent.cream);
            string[] names = { "탈출", "시간", "진주" }; //세 가지 별 목표
            for (int i = 0; i < 3; i++)
            {
                RectTransform chip = style.card(top, "Goal " + names[i], new Vector2((i - 1) * 146, -83), new Vector2(138, 39), SwimArt.ink, GameUIStyleComponent.border).rectTransform; //목표별 상태 판
                GameUIIconComponent star = style.icon(chip, SwimUIIcon.Star, new Vector2(-48, 0), 23, SwimArt.mint); //미확정 목표는 빈 별
                Text label = style.text(chip, names[i], new Vector2(13, 0), new Vector2(95, 32), 14, GameUIStyleComponent.cream); //목표 문구
                if (i == 0) label.text = "출구 찾기";
                if (i == 1) { timeGoalLabel = label; timeIcon = star; }
                if (i == 2) { cargoLabel = label; cargoIcon = star; label.fontSize = 12; }
            }
            style.picture(top, "Distance track", "pill", new Vector2(0, -112), new Vector2(430, 4), GameUIStyleComponent.border);
            progressBar = style.picture(top, "Distance progress", "pill", new Vector2(-215, -112), new Vector2(0, 4), SwimArt.mint);
            progressBar.rectTransform.pivot = new Vector2(0, .5f);
            statusLabel = style.text(top, "", new Vector2(0, -131), new Vector2(450, 22), 12, GameUIStyleComponent.cream);
            Shadow shadow = statusLabel.gameObject.AddComponent<Shadow>(); //지형 위 작은 글자 대비
            shadow.effectColor = SwimArt.ink;
            shadow.effectDistance = new Vector2(1, -1);
        }

        private void createControls(RectTransform root) //이동과 미끼 및 꾹의 독립 조작 영역
        {
            RectTransform bottom = style.bar(root, "Controls", false, 190); //하단 고정 배치
            style.card(bottom, "Control deck", new Vector2(0, 65), new Vector2(520, 174), new Color32(6, 39, 54, 238), GameUIStyleComponent.border);
            RectTransform hint = style.card(bottom, "Hint", new Vector2(0, 169), new Vector2(434, 34), GameUIStyleComponent.panel, GameUIStyleComponent.border).rectTransform; //손가락 위 안내
            hintLabel = style.text(hint, "", Vector2.zero, new Vector2(415, 29), 12, GameUIStyleComponent.cream);
            Image baseImage = style.picture(bottom, "Move stick", "circle", new Vector2(-141, 88), Vector2.one * 126, GameUIStyleComponent.panel); //넓은 스틱 터치 영역
            baseImage.raycastTarget = true;
            style.picture(baseImage.rectTransform, "Stick edge", "ring", Vector2.zero, Vector2.one * 126, new Color32(110, 188, 202, 255));
            style.picture(baseImage.rectTransform, "Stick well", "circle", Vector2.zero, Vector2.one * 68, new Color32(38, 88, 105, 255));
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * .5f; //방향 표식 회전
                Image arrow = style.picture(baseImage.rectTransform, "Direction", "triangle", new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 52, new Vector2(9, 12), GameUIStyleComponent.border); //스틱 방향 표시
                arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, i * 90);
            }
            Image knob = style.picture(baseImage.rectTransform, "Stick handle", "circle", Vector2.zero, Vector2.one * 56, SwimArt.mint); //민트색 손잡이
            style.picture(knob.rectTransform, "Knob edge", "ring", Vector2.zero, Vector2.one * 54, new Color32(187, 255, 224, 255));
            stick = baseImage.gameObject.AddComponent<TouchControlComponent>();
            stick.initialize(player, knob.rectTransform, false);
            style.text(bottom, "이동", new Vector2(-141, 15), new Vector2(100, 26), 14, SwimArt.mint, TextAnchor.MiddleCenter, true);

            baitButton = style.button(bottom, "", new Vector2(18, 85), new Vector2(78, 78), () => { });
            RectTransform baitRoot = baitButton.GetComponent<RectTransform>(); //미끼 그림과 개수
            UIButtonFeedbackComponent baitFeedback = baitRoot.GetComponent<UIButtonFeedbackComponent>(); //조준 중 영역 밖에서도 누름 유지
            baitFeedback.initialize(baitButton.targetGraphic.rectTransform, baitRoot.Find("Touch ripple").GetComponent<Image>(), baitButton, true);
            style.icon(baitRoot, SwimUIIcon.Bait, new Vector2(0, 16), 37, new Color32(255, 151, 64, 255));
            style.text(baitRoot, "미끼", new Vector2(0, -15), new Vector2(64, 24), 15, GameUIStyleComponent.cream, TextAnchor.MiddleCenter, true);
            RectTransform badge = style.card(baitRoot, "Bait count", new Vector2(29, 36), Vector2.one * 29, SwimArt.ink, GameUIStyleComponent.muted).rectTransform; //잔여 개수 배지
            baitLabel = style.text(badge, "2", Vector2.zero, Vector2.one * 28, 17, GameUIStyleComponent.cream, TextAnchor.MiddleCenter, true);
            RectTransform aimHandle = style.picture(baitRoot, "Aim handle", "circle", Vector2.zero, Vector2.one * 15, SwimArt.mint).rectTransform; //드래그 방향을 보여주는 표시
            baitAim = baitRoot.gameObject.AddComponent<TouchBaitAimComponent>();
            baitAim.initialize(player, aimHandle, baitFullDragDistance, baitCancelRadius);
            style.text(bottom, "드래그 조준 · 놓기", new Vector2(18, 15), new Vector2(120, 26), 11, GameUIStyleComponent.muted);

            Image holdBase = style.picture(bottom, "Hold action", "circle", new Vector2(151, 88), Vector2.one * 125, GameUIStyleComponent.panel); //꾹 터치 영역
            holdBase.raycastTarget = true;
            style.picture(holdBase.rectTransform, "Hold edge", "ring", Vector2.zero, Vector2.one * 125, GameUIStyleComponent.border);
            holdFace = style.picture(holdBase.rectTransform, "Hold gold", "circle", Vector2.zero, Vector2.one * 103, SwimArt.yellow);
            style.picture(holdFace.rectTransform, "Hold highlight", "ring", Vector2.zero, Vector2.one * 100, GameUIStyleComponent.cream);
            style.icon(holdFace.rectTransform, SwimUIIcon.Knot, new Vector2(0, 20), 29, SwimArt.ink);
            style.text(holdFace.rectTransform, "꾹", new Vector2(0, -17), new Vector2(72, 57), 29, SwimArt.ink, TextAnchor.MiddleCenter, true);
            netRing = style.picture(holdBase.rectTransform, "Net progress", "ring", Vector2.zero, Vector2.one * 125, SwimArt.mint);
            netRing.type = Image.Type.Filled;
            netRing.fillMethod = Image.FillMethod.Radial360;
            netRing.fillOrigin = (int)Image.Origin360.Top;
            netRing.fillClockwise = true;
            netRing.fillAmount = 0;
            hold = holdBase.gameObject.AddComponent<TouchControlComponent>();
            hold.initialize(player, null, true);
            holdFeedback = style.feedback(holdBase.rectTransform, holdFace.rectTransform, null, true);
            holdCaption = style.text(bottom, "틈 통과 · 그물 풀기", new Vector2(151, 15), new Vector2(145, 26), 12, GameUIStyleComponent.cream, TextAnchor.MiddleCenter, true);
        }

        public void showPhase() //다른 화면으로 전환할 때 조작 점유 해제
        {
            stick.resetControl();
            hold.resetControl();
            baitAim.resetControl();
            panel.SetActive(game.phase == SwimPhase.Playing);
            if (game.phase == SwimPhase.Playing) { messageTime = 0; temporaryMessage = ""; }
        }

        public void tick(float deltaTime) //실제 상태에 맞는 목표와 조작 안내 갱신
        {
            timerLabel.text = GameUIComponent.formatTime(game.elapsed);
            float remaining = Mathf.Max(0, game.timeGoal - game.elapsed); //시간 별 남은 시간
            bool withinTime = game.elapsed <= game.timeGoal; //시간 목표 달성 가능 상태
            timeGoalLabel.text = withinTime ? GameUIComponent.clockTime(remaining) + " 남음" : "시간 초과";
            timeGoalLabel.color = withinTime ? remaining <= 30 ? SwimArt.yellow : GameUIStyleComponent.cream : GameUIStyleComponent.muted;
            timeIcon.configure(SwimUIIcon.Star, withinTime ? SwimArt.mint : GameUIStyleComponent.border);
            progressBar.rectTransform.sizeDelta = new Vector2(430 * game.progress, 4);
            baitLabel.text = player.baitCount.ToString();
            baitButton.interactable = player.baitCount > 0;
            cargoLabel.text = player.hasCargo ? "진주 운반 중" : "진주 미획득";
            cargoLabel.color = player.hasCargo ? new Color32(227, 210, 255, 255) : GameUIStyleComponent.cream;
            cargoIcon.configure(SwimUIIcon.Star, player.hasCargo ? new Color32(227, 210, 255, 255) : SwimArt.mint);
            statusLabel.text = player.safe ? "안전구역 · 여기서는 잡히지 않아요" : player.hidden ? "풀숲에 숨었어요 · 지금은 안전해요" : !string.IsNullOrEmpty(game.danger) ? game.danger : "별은 출구에 도착하면 확정돼요";
            statusLabel.color = player.safe || player.hidden ? SwimArt.mint : string.IsNullOrEmpty(game.danger) ? GameUIStyleComponent.cream : SwimArt.coral;
            if (!string.IsNullOrEmpty(player.message)) { temporaryMessage = player.message; messageTime = 3; }
            messageTime -= deltaTime;
            hintLabel.text = player.baitAiming ? player.baitAimStrength > 0 ? "조준 " + Mathf.RoundToInt(player.baitAimStrength * 100) + "% · 놓으면 발사 / 중앙으로 취소" : "드래그로 방향·거리 조절 · 중앙에서 놓으면 취소" :
                messageTime > 0 ? temporaryMessage : game.elapsed < 3f ? "왼쪽 스틱을 드래그해 이동 · 출구를 찾아요" : player.nearNet ? "이동하면서 꾹 눌러도 그물이 풀려요" : player.squeezed ? "틈 안에서는 꾹 누른 채 움직여요" : player.hasCargo ? "진주를 챙겼어요! 출구까지 가져가요" : "미끼 버튼을 드래그해 조준하고, 풀숲에 숨어요";
            holdCaption.text = player.nearNet ? "그물 풀기" : player.squeezed ? "틈 통과 중" : "틈 통과 · 그물 풀기";
            holdFace.color = player.working ? new Color32(255, 238, 164, 255) : holdFeedback.pressed ? new Color32(235, 195, 82, 255) : SwimArt.yellow;
            netRing.fillAmount = player.nearNet ? player.netProgress : 0;
            netCallout.gameObject.SetActive(player.nearNet);
            netLabel.text = "그물 해제 " + Mathf.RoundToInt(player.netProgress * 100) + "%";
            if (player.nearNet && Camera.main != null)
            {
                float scale = style.controlScale; //HUD와 같은 알림 배율
                netCallout.localScale = Vector3.one * scale;
                Vector3 viewport = Camera.main.WorldToViewportPoint(player.position); //따라가는 플레이어 화면 비율
                Vector2 point = style.viewportToSafePoint(viewport); //안전 영역 중앙 기준 UI 위치
                float halfWidth = style.safe.rect.width * .5f; //안내가 화면 밖으로 나가지 않는 가로 경계
                point.x = Mathf.Clamp(point.x, -halfWidth + 88 * scale, halfWidth - 88 * scale);
                point.y = Mathf.Clamp(point.y + 70 * scale, style.safe.rect.yMin + 220 * scale, style.safe.rect.yMax - 175 * scale);
                netCallout.anchoredPosition = point;
            }
        }
    }
}
