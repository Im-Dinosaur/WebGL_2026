using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieCqb
{
    public sealed class ZombieCqbUIComponent : MonoBehaviour
    {
        [SerializeField] private GameObject hud; //플레이 중 보이는 화면
        [SerializeField] private GameObject title; //시작 화면
        [SerializeField] private GameObject upgrades; //스테이지 완료 뒤의 강화 선택 화면
        [SerializeField] private GameObject result; //게임 오버와 완료 결과 화면
        [SerializeField] private Button startButton; //시작 버튼
        [SerializeField] private Button restartButton; //다시 시작 버튼
        [SerializeField] private Text stageText; //현재 맵 이름과 번호
        [SerializeField] private Text healthText; //현재 체력과 최대 체력
        [SerializeField] private Image healthFill; //현재 체력 비율
        [SerializeField] private Text infectionText; //감염된 시민 수와 경험치
        [SerializeField] private Text statusText; //탐지 및 은신 상태
        [SerializeField] private Text hintText; //가까운 문과 출구의 조작 안내
        [SerializeField] private Text[] activeAbilities; //사용할 수 있는 능력과 대기 시간
        [SerializeField] private Text upgradeSummary; //스테이지 통과 결과
        [SerializeField] private Button[] upgradeButtons; //세 분류의 강화 선택 버튼
        [SerializeField] private Text[] upgradeRanks; //선택 시 다음 강화 단계 표시
        [SerializeField] private Text resultTitle; //게임 종료 결과 제목
        [SerializeField] private Text resultSummary; //종료된 스테이지와 경험치

        private static readonly string[] abilityLabels = { "Q  액체", "R  비말", "F  은신", "SHIFT  돌진", "C  조종" }; //해금 능력의 단축키 이름

        public void bind(Action start, Action<UpgradeId> choose) //화면 버튼의 요청을 게임 진입점에 연결
        {
            startButton.onClick.AddListener(() => start());
            restartButton.onClick.AddListener(() => start());
            for (int i = 0; i < upgradeButtons.Length; i++) //각 버튼에 연결할 강화 항목
            {
                UpgradeId id = (UpgradeId)i; //버튼의 선택 항목
                upgradeButtons[i].onClick.AddListener(() => choose(id));
            }
        }

        public void showTitle() //처음 시작 화면 표시
        {
            showOverlay(title);
            hud.SetActive(false);
        }

        public void showGameplay() //플레이 화면과 HUD 표시
        {
            showOverlay(null);
            hud.SetActive(true);
        }

        public void setHud(int stage, int stageCount, string stageName, float health, float maximum, int infected, int population,
            int experience, bool detected, bool hidden, string hint) //플레이 중 확인할 정보 표시
        {
            stageText.text = $"{stage:00} / {stageCount:00}    {stageName}";
            healthText.text = $"{Mathf.CeilToInt(health)} / {Mathf.CeilToInt(maximum)}";
            healthFill.fillAmount = health / maximum;
            healthFill.color = health < maximum * 0.30f ? new Color32(255, 111, 91, 255) : new Color32(147, 234, 170, 255);
            infectionText.text = $"감염 {infected:00} / {population:00}      XP {experience:00}";
            statusText.text = detected ? "추격 중" : hidden ? "은신 중" : "탐색 중";
            statusText.color = detected ? new Color32(255, 128, 102, 255) : new Color32(156, 188, 171, 255);
            hintText.text = hint;
            hintText.transform.parent.gameObject.SetActive(hint.Length > 0);
        }

        public void setAbility(UpgradeId id, int rank, float cooldown) //해금된 단축키와 재사용 대기 표시
        {
            int index = (int)id - (int)UpgradeId.Liquid; //해금 능력 표시 인덱스
            Text label = activeAbilities[index]; //해당 능력의 화면 문구
            label.transform.parent.gameObject.SetActive(rank > 0);
            label.text = cooldown > 0f ? $"{abilityLabels[index]}   {Mathf.CeilToInt(cooldown)}s" : abilityLabels[index];
            label.color = cooldown > 0f ? new Color32(144, 164, 161, 255) : new Color32(163, 242, 179, 255);
        }

        public void showUpgrades(StageOutcome outcome, int stage, UpgradeProgressComponent progress) //스테이지를 통과한 뒤 세 가지 분류의 강화 표시
        {
            showOverlay(upgrades);
            string result = outcome == StageOutcome.InfectedEveryone ? "전원 감염" : "탈출 성공"; //이번 맵의 통과 방식
            upgradeSummary.text = $"구역 {stage:00}  ·  {result}  ·  누적 XP {progress.experience}";
            for (int i = 0; i < upgradeButtons.Length; i++) //선택 가능한 전체 강화 버튼
            {
                UpgradeId id = (UpgradeId)i; //확인할 강화 종류
                int rank = progress.getRank(id); //이미 획득한 강화 단계
                bool available = progress.canUpgrade(id); //추가 선택이 가능한지 여부
                upgradeButtons[i].interactable = available;
                upgradeRanks[i].text = available ? $"Lv. {rank + 1}" : "최대";
            }
        }

        public void showResult(bool cleared, int stage, int experience) //최종 완료 또는 사망 결과 표시
        {
            showOverlay(result);
            resultTitle.text = cleared ? "구역 통과 완료" : "게임 오버";
            resultTitle.color = cleared ? new Color32(167, 241, 179, 255) : new Color32(255, 128, 102, 255);
            resultSummary.text = cleared ? $"모든 구역을 통과했습니다.  ·  누적 XP {experience}" : $"구역 {stage:00}에서 쓰러졌습니다.  ·  누적 XP {experience}";
        }

        private void showOverlay(GameObject overlay) //표시할 메뉴 화면 하나 선택
        {
            title.SetActive(overlay == title);
            upgrades.SetActive(overlay == upgrades);
            result.SetActive(overlay == result);
        }
    }
}
