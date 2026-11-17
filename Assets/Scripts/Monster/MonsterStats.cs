using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemDropEntry
{
    [Header("드롭 아이템")]
    public ItemData itemData;

    [Header("드롭 수량")]
    public int dropAmount = 1;
}

/// <summary>
/// 몬스터의 능력치, 체력 및 피격 / 사망 판정을 전담합니다.
/// </summary>
public class MonsterStats : MonoBehaviour, ITakeDamage
{
    [Header("기본 스탯")]
    [SerializeField] float maxHp;
    [SerializeField] float curHp;
    [SerializeField] float atkPower;
    [SerializeField] float monDef;
    [SerializeField] float moveSpeed;
    [SerializeField] float chaseSpeed;
    [SerializeField] MonsterType monsterType;

    [Header("데미지 텍스트 스폰 위치")]
    [SerializeField] Transform damageTextPoint;

    [Header("월드 드롭 설정")]
    [SerializeField] WorldDropItem worldDropPrefab;
    [SerializeField] float dropSpawnHeight = 1.0f;

    [Header("아이템 드롭 설정")]
    [SerializeField] List<ItemDropEntry> itemDrops;

    // 외부 읽기 전용
    public float MaxHp => maxHp;
    public float CurHp => curHp;
    public float AtkPower => atkPower;
    public float MonDef => monDef;
    public float MoveSpeed => moveSpeed;
    public float ChaseSpeed => chaseSpeed;
    public MonsterType MonsterType => monsterType;

    public bool IsDead { get; private set; }

    // 피격 및 사망 이벤트
    public event Action<GameObject, float> OnTakeDamage;
    public event Action<float, float> OnHpChanged;
    public event Action OnDeath;

    /// <summary>
    /// [오브젝트 풀 연동] 소환 시 체력 및 사망 상태 초기화
    /// </summary>
    public void ResetStats()
    {
        IsDead = false;
        curHp = maxHp;
    }

    /// <summary>
    /// 복귀시 체력 풀 회복
    /// </summary>
    public void RestoreHp()
    {
        curHp = maxHp;
        // ui 슬라이더 채우기용
        OnHpChanged?.Invoke(curHp, maxHp);
    }

    private void Awake()
    {
        // 체력 초기화
        curHp = maxHp;
    }

    public void TakeDamage(GameObject attacker, float damage)
    {
        // 사망 상태라면 건너뜀
        if (IsDead) return;

        // 몬스터 데미지 감소율 = 몬스터 방어력 / (몬스터 방어력 + 300)
        // 최종 데미지 = 기본 데미지 * 플레이어 타격 모션 계수 * (1 - 몬스터 데미지 감소율)
        // 최소 데미지 보장 = 최종 데미지가 1미만이면 1의 데미지 보장
        float damageReduction = monDef / (monDef + 300f);
        float finalDamage = damage * (1f - damageReduction);
        finalDamage = MathF.Max(1f, finalDamage);

        curHp -= finalDamage;
        // 피격 알림 이벤트 실행
        OnTakeDamage?.Invoke(attacker, damage);
        // 체력 변화 이벤트 실행
        OnHpChanged?.Invoke(curHp, maxHp);

        // 데미지 텍스트 스폰 기준점이 설정되어 있다면 그 위치를 사용하고,
        // 설정되지 않았다면 기존 몬스터 루트 위치를 사용한다.
        Vector3 textPos = damageTextPoint != null ? damageTextPoint.position : transform.position;
        // 데미지 텍스트 매니저에 텍스트 스폰 위치를 전달한다.
        DamageTextManager.Instance.ShowDamageText(textPos, finalDamage);

        if(curHp <= 0f)
        {
            curHp = 0f;
            IsDead = true;
            // 몬스터 사망 시 설정된 아이템을 플레이어에게 지급
            DropItems();

            // 사망 알림 이벤트 실행
            OnDeath?.Invoke();
            if(QuestManager.instance != null)
            {
                QuestManager.instance.OnMonsterKilled(this.monsterType);
            }
        }
    }

    /// <summary>
    /// 몬스터 사망시 인스펙터에 설정된 아이템을 플레이어 인벤토리에 지급
    /// </summary>
    private void DropItems()
    {
        // 드롭 목록이 없거나 비어 있다면 지급할 아이템이 없으므로 종료
        if(itemDrops == null || itemDrops.Count == 0) return;

        // 플레이어 인벤토리가 없다면 아이템 지급을 진행할 수 없다.
        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("[MonsterStats] 플레이어 인벤토리를 찾을 수 없습니다.");
            return;
        }

        // 몬스터에 설정된 모든 드롭 항목을 순회
        foreach(ItemDropEntry drop in itemDrops)
        {
            // 아이템 데이터가 없거나 드롭 수향이 유효하지 않으면 건너뜀
            if(drop == null || drop.itemData == null || drop.dropAmount <= 0) continue;

            // 먼저 플레이어 인벤토리에 아이템 지급을 시도
            bool success = PlayerInventory.Instance.Additem(drop.itemData, drop.dropAmount);
            
            // 인벤토리 지급에 성공했다면 월드 드롭을 생성할 필요가 없다
            if(success) continue;

            // 지급에 실패했다면 월드 드롭 프리팹을 생성
            if(worldDropPrefab == null)
            {
                Debug.LogWarning("[MonsterStats] 월드 드롭 프리팹이 설정되지 않았습니다.");
                continue;
            }

            // 몬스터가 죽은 위치보다 약간 높은 위치를 생성 위치로 지정
            Vector3 spawnPos = transform.position + Vector3.up * dropSpawnHeight;

            // 프리팹 생성
            WorldDropItem worldDrop = Instantiate(worldDropPrefab, spawnPos, Quaternion.identity);

            // 생성된 월드 드롭에 아이템 정보와 수량 전달
            worldDrop.Initialize(drop.itemData, drop.dropAmount);
        }
    }
}
