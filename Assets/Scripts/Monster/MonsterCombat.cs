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
    [SerializeField, Tooltip("OverLapBox 반경 수치")] Vector3 atkHalfBox;
    [SerializeField, Tooltip("공격 가능 사거리")] float atkDistance;
    [SerializeField, Tooltip("타격 대상 레이어")] LayerMask targetLayer;

    public float AtkDistance => atkDistance;

    float atkTimer;
    bool checkAtk;
    float currentAtkPower;

    // 1회 공격 모션 중 중복 타격 방지를 위한 피격리스트
    readonly List<ITakeDamage> hitTargetList = new List<ITakeDamage>();

    /// <summary>
    /// AIController에서 초기화 시 호출
    /// </summary>
    /// <param name="defaultAtkPower"></param>
    public void Init(float defaultAtkPower)
    {
        currentAtkPower = defaultAtkPower;
    }

    /// <summary>
    /// 공격 애니메이션 이벤트에서 판정 시작 시 호출
    /// </summary>
    public void StartAttack()
    {
        checkAtk = true;
        hitTargetList.Clear();
    }

    /// <summary>
    /// 공격 판정 종료시 호출
    /// </summary>
    public void EndAttack()
    {
        checkAtk = false;
        hitTargetList.Clear();
    }

    private void Update()
    {
        // 공격 판정이 활성화된 동안에만 검사함
        OverLapBoxCheck();
    }

    private void OverLapBoxCheck()
    {
        // 공격 위치가 없거나 공격 판정이 false라면 종료
        if (atkPoint == null || !checkAtk) return;

        Collider[] hitTargets = Physics.OverlapBox(
            atkPoint.position,
            atkHalfBox,
            atkPoint.rotation,
            targetLayer
            );

        foreach(Collider target in hitTargets)
        {
            // 타깃에 ITakeDamage 인터페이스 존재 여부 확인
            if(target.TryGetComponent<ITakeDamage>(out var damage))
            {
                // 이미 타격받은 대상이면 스킵
                if (hitTargetList.Contains(damage)) continue;

                // 데미지 전달 및 중복 방지 리스트 추가
                damage.TakeDamage(this.gameObject, currentAtkPower);
                hitTargetList.Add(damage);
                break;
            }
        }
    }

    // 에디터 씬 뷰에서 공격 범위를 시각화하는 기즈모
    private void OnDrawGizmosSelected()
    {
        if (atkPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.matrix = atkPoint.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, atkHalfBox * 2f);
    }
}
