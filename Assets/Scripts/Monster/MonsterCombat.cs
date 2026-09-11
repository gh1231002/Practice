using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 및 히트박스 모듈
/// 공격 범위 판정 및 피격 대상에 대한 데미지 연산을 전담합니다.
/// </summary>
public class MonsterCombat : MonoBehaviour
{
    [Header("공격 히트박스 설정")]
    [SerializeField, Tooltip("공격 히트박스 위치")] Transform atkPoint;
    [SerializeField, Tooltip("공격 가능 사거리 및 임팩트 범위 반지름")] float atkDistance;
    [SerializeField, Tooltip("타격 대상 레이어")] LayerMask targetLayer;

    public float AtkDistance => atkDistance;

    float currentAtkPower;

    /// <summary>
    /// AIController에서 초기화 시 호출
    /// </summary>
    /// <param name="defaultAtkPower"></param>
    public void Init(float defaultAtkPower)
    {
        currentAtkPower = defaultAtkPower;
    }

    /// <summary>
    /// 공격 애니메이션의 타격 프레임에서 1회 호출합니다.
    /// </summary>
    public void OnHitImpact()
    {
        // atkPoint 미지정 시 몬스터 정면 1.5m, 높이 1.0m를 기준점으로 자동 설정
        Vector3 center = atkPoint != null
            ? atkPoint.position
            : transform.position + transform.forward * 1.5f + Vector3.up * 1.0f;

        // 임팩트 순간 구형 범위를 통한 타깃 검사
        Collider[] hitTargets = Physics.OverlapSphere(center, AtkDistance, targetLayer);

        foreach(Collider target in hitTargets)
        {
            // 자식 충돌체와 최상위 오브젝트 모두에서 ITakeDamage 확인
            ITakeDamage damage = target.GetComponent<ITakeDamage>() ?? target.GetComponentInParent<ITakeDamage>();

            if(damage != null)
            {
                damage.TakeDamage(gameObject, currentAtkPower);
                break;
            }
        }
    }

    // 에디터 씬 뷰에서 공격 범위를 시각화하는 기즈모
    private void OnDrawGizmosSelected()
    {
        if (atkPoint == null) return;
        Vector3 center = atkPoint != null
            ? atkPoint.position
            : transform.position + transform.forward * 1.5f + Vector3.up * 1.0f;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, AtkDistance);
    }
}
