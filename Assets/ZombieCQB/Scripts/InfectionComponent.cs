using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieCqb
{
    public sealed class InfectionComponent : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float contactRange = 1.08f; //접촉 감염을 유지할 수 있는 거리
        [SerializeField, Min(0.1f)] private float contactDuration = 0.8f; //기본 접촉 감염에 걸리는 시간
        [SerializeField, Min(0.1f)] private float liquidRadius = 1.34f; //액체 감염의 기본 장판 반경
        [SerializeField, Min(0.1f)] private float liquidDuration = 6f; //액체 장판의 기본 지속 시간
        [SerializeField, Min(0.1f)] private float liquidCooldown = 7.5f; //액체 감염 재사용 대기 시간
        [SerializeField, Min(0.1f)] private float dropletRange = 2.65f; //전방 범위 감염의 기본 거리
        [SerializeField, Min(0.1f)] private float dropletCooldown = 4.7f; //전방 범위 감염 재사용 대기 시간
        [SerializeField] private Sprite liquidSprite; //액체 감염 영역을 표시할 외형
        [SerializeField] private SpriteRenderer dropletFlash; //전방 감염 발동의 짧은 표시

        private sealed class LiquidZone
        {
            public Vector2 center; //장판 중심 위치
            public float radius; //효과가 닿는 거리
            public float remaining; //장판이 유지되는 시간
            public SpriteRenderer view; //장판 표시
        }

        private readonly List<LiquidZone> zones = new List<LiquidZone>(); //현재 활성화된 액체 영역
        private float liquidWait; //액체 감염 재사용까지 남은 시간
        private float dropletWait; //전방 감염 재사용까지 남은 시간
        private float flashRemaining; //전방 감염 표시 시간
        public event Action infected; //시민 한 명이 처음 감염됐을 때 발생하는 신호

        public void resetForStage() //스테이지에 남은 감염 영역과 대기 시간 초기화
        {
            foreach (LiquidZone zone in zones) //제거할 이전 맵의 장판
                Destroy(zone.view.gameObject);

            zones.Clear();
            liquidWait = 0f;
            dropletWait = 0f;
            flashRemaining = 0f;
            dropletFlash.enabled = false;
        }

        public void tick(float deltaTime, PlayerInputFrame input, Vector2 position, Vector2 facing, StageMapComponent map, UpgradeProgressComponent progress) //접촉과 해금된 감염 방식 실행
        {
            liquidWait = Mathf.Max(0f, liquidWait - deltaTime);
            dropletWait = Mathf.Max(0f, dropletWait - deltaTime);
            flashRemaining = Mathf.Max(0f, flashRemaining - deltaTime);
            dropletFlash.enabled = flashRemaining > 0f;

            if (input.infect)
            {
                CivilianComponent nearest = findTarget(position, contactRange, map); //직접 접촉할 수 있는 시민
                if (nearest != null)
                {
                    float duration = contactDuration * Mathf.Pow(0.78f, progress.getRank(UpgradeId.InfectionSpeed)); //강화 후 접촉 감염에 필요한 시간
                    apply(nearest, deltaTime / duration);
                }
            }

            int liquidRank = progress.getRank(UpgradeId.Liquid); //액체 감염 강화 단계
            if (input.liquid && liquidRank > 0 && liquidWait <= 0f)
                createZone(position, liquidRank);

            int dropletRank = progress.getRank(UpgradeId.Droplet); //전방 감염 강화 단계
            if (input.droplet && dropletRank > 0 && dropletWait <= 0f)
                castDroplet(position, facing, dropletRank, map);

            tickZones(deltaTime, map);
            foreach (CivilianComponent ally in map.people) //이동 명령을 따르며 감염을 퍼뜨리는 좀비
            {
                if (!ally.isInfected || !ally.isCommanded)
                    continue;

                CivilianComponent target = findTarget(ally.position, 0.79f, map); //조종된 좀비와 가까운 시민
                if (target != null)
                    apply(target, deltaTime * 0.72f);
            }
        }

        public float getCooldown(UpgradeId id) //감염 능력의 남은 대기 시간 표시
        {
            return id == UpgradeId.Liquid ? liquidWait : dropletWait;
        }

        private static CivilianComponent findTarget(Vector2 position, float range, StageMapComponent map) //범위와 시야를 만족하는 가장 가까운 시민 찾기
        {
            CivilianComponent nearest = null; //선택한 시민
            float best = range * range; //허용하는 목표 거리 제곱
            foreach (CivilianComponent civilian in map.people) //감염 대상 후보
            {
                float distance = (civilian.position - position).sqrMagnitude; //후보와의 거리 제곱
                if (civilian.isInfected || distance > best || !map.navigation.hasSight(position, civilian.position))
                    continue;

                nearest = civilian;
                best = distance;
            }

            return nearest;
        }

        private void createZone(Vector2 position, int rank) //플레이어 위치에 일시적인 감염 영역 배치
        {
            GameObject marker = new GameObject("Liquid infection"); //맵 위 액체 영역 오브젝트
            marker.transform.SetParent(transform, false);
            marker.transform.position = position;
            float radius = liquidRadius + (rank - 1) * 0.28f; //강화 후 장판 반경
            marker.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            SpriteRenderer view = marker.AddComponent<SpriteRenderer>(); //액체 영역의 표시
            view.sprite = liquidSprite;
            view.sortingOrder = 7;
            zones.Add(new LiquidZone
            {
                center = position,
                radius = radius,
                remaining = liquidDuration + (rank - 1) * 1.6f,
                view = view
            });
            liquidWait = liquidCooldown;
        }

        private void tickZones(float deltaTime, StageMapComponent map) //밟은 시민에게 장판 효과를 주고 만료 처리
        {
            for (int i = zones.Count - 1; i >= 0; i--) //현재 활성화된 장판의 인덱스
            {
                LiquidZone zone = zones[i]; //이번에 처리하는 감염 영역
                zone.remaining -= deltaTime;
                if (zone.remaining <= 0f)
                {
                    Destroy(zone.view.gameObject);
                    zones.RemoveAt(i);
                    continue;
                }

                zone.view.color = new Color(1f, 1f, 1f, Mathf.Min(1f, zone.remaining * 2f));
                foreach (CivilianComponent civilian in map.people) //영역을 밟을 수 있는 시민
                {
                    if (!civilian.isInfected && (civilian.position - zone.center).sqrMagnitude <= zone.radius * zone.radius
                        && map.navigation.hasSight(zone.center, civilian.position))
                        apply(civilian, deltaTime * 0.92f);
                }
            }
        }

        private void castDroplet(Vector2 position, Vector2 facing, int rank, StageMapComponent map) //가까운 전방 범위에 감염 진행도 적용
        {
            float range = dropletRange + (rank - 1) * 0.48f; //강화 후 전방 범위 거리
            foreach (CivilianComponent civilian in map.people) //전방 범위에 들어오는 시민 후보
            {
                Vector2 offset = civilian.position - position; //플레이어에서 시민으로 향하는 방향
                if (civilian.isInfected || offset.sqrMagnitude > range * range
                    || Vector2.Dot(offset.normalized, facing) < 0.70f || !map.navigation.hasSight(position, civilian.position))
                    continue;

                apply(civilian, 0.77f + (rank - 1) * 0.13f);
            }

            dropletFlash.transform.position = position + facing * (range * 0.5f);
            dropletFlash.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg - 90f);
            dropletFlash.transform.localScale = new Vector3(range * 1.4f, range, 1f);
            dropletWait = dropletCooldown;
            flashRemaining = 0.22f;
            dropletFlash.enabled = true;
        }

        private void apply(CivilianComponent civilian, float amount) //감염 진행과 최초 완료 신호를 연결
        {
            if (civilian.addInfection(amount))
                infected?.Invoke();
        }
    }
}
