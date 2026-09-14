using System;
using UnityEngine;

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

    // 외부 읽기 전용
    public float MaxHp => maxHp;
    public float CurHp => curHp;
    public float AtkPower => atkPower;
    public float MonDef => monDef;
    public float MoveSpeed => moveSpeed;
    public float ChaseSpeed => chaseSpeed;

    public bool IsDead { get; private set; }

    // 피격 및 사망 이벤트
    public event Action<GameObject, float> OnTakeDamage;
    public event Action OnDeath;

    /// <summary>
    /// [오브젝트 풀 연동] 소환 시 체력 및 사망 상태 초기화
    /// </summary>
    public void ResetStats()
    {
        IsDead = false;
        curHp = maxHp;
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

        // 데미지 텍스트 출력
        Vector3 hitPos = transform.position;
        DamageTextManager.Instance.ShowDamageText(hitPos, finalDamage);

        if(curHp <= 0f)
        {
            curHp = 0f;
            IsDead = true;
            // 사망 알림 이벤트 실행
            OnDeath?.Invoke();
        }
    }
}
