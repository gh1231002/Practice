using System;
using UnityEngine;

/// <summary>
/// 몬스터의 능력치, 체력 및 피격 / 사망 전담합니다.
/// </summary>
public class MonsterStats : MonoBehaviour, ITakeDamage
{
    [Header("기본 스탯")]
    [SerializeField] float maxHp;
    [SerializeField] float curHp;
    [SerializeField] float atkPower;
    [SerializeField] float moveSpeed;
    [SerializeField] float chaseSpeed;

    public float MaxHp => maxHp;
    public float CurHp => curHp;
    public float AtkPower => atkPower;
    public float MoveSpeed => moveSpeed;
    public float ChaseSpeed => chaseSpeed;

    public bool IsDead { get; private set; }

    // 피격 및 사망 이벤트
    public event Action OnTakeDamage;
    public event Action OnDeath;

    private void Awake()
    {
        curHp = maxHp;
    }

    public void TakeDamage(GameObject attacker, float damage)
    {
        if (IsDead) return;

        curHp -= damage;
        // 피격 알림 이벤트 실행
        OnTakeDamage?.Invoke();

        if(curHp <= 0f)
        {
            curHp = 0f;
            IsDead = true;
            // 사망 알림 이벤트 실행
            OnDeath?.Invoke();
        }
    }
}
