using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 보스 전용 전투 및 히트박스 모듈
/// 공격 범위 판정, 피격 대상에게 공격에 따른 데미지 전달
/// </summary>
public class BossCombat : MonoBehaviour
{
    [Header("공격 관련 설정")]
    [SerializeField, Tooltip("왼손 공격 포인트")] Transform leftHandAtkPoint;
    [SerializeField, Tooltip("오른손 공격 포인트")] Transform rightHandAtkPoint;
    [SerializeField, Tooltip("내려찍기 공격 포인트")] Transform stompHandAtkPoint;
    [SerializeField, Tooltip("회전 공격 포인트")] Transform rotationAtkPoint;
    [SerializeField, Tooltip("점프 공격 포인트")] Transform jumpAtkPoint;

    [SerializeField, Tooltip("왼손 임팩트 범위 반지름")] float leftHandRadius;
    [SerializeField, Tooltip("오른손 임팩트 범위 반지름")] float rightHandRadius;
    [SerializeField, Tooltip("내려찍기 임팩트 범위 반지름")] float stompHandRadius;
    [SerializeField, Tooltip("회전 공격 임팩트 범위 반지름")] float rotationRadius;
    [SerializeField, Tooltip("점프 공격 임팩트 범위 반지름")] float jumpRadius;

    [SerializeField, Tooltip("공격 가능 사거리")] float atkDistance;
    [SerializeField, Tooltip("타격 대상 레이어")] LayerMask targetLayer;

    [Header("경고 원 스프라이트")]
    [SerializeField] GameObject warningCirclePrefab;
    [SerializeField, Tooltip("바닥과 겹쳐 깜빡이는 현상 방지")] float warningGroundOffset;

    public float AtkDistance => atkDistance;

    float currentAtkPower;

    GameObject warningCircleInstance;

    // 피해 판정이 활성화되어 있는지 확인한다.
    bool isSpinDamageActive;
    bool isComboDamageActive;

    BossAttackPattern attackPattern;

    // 이미 피해를 받은 대상을 기록한다.
    // 같은 대상의 콜라이더가 여러 개여도 중복 피해를 막기 위한 용도이다.
    readonly HashSet<ITakeDamage> targets = new();

    // 애니메이션 이벤트 함수에서 사용하는 함수
    public void StartSpinAttack()
    {
        // 이전 공격 기록을 초기화
        targets.Clear();
        isSpinDamageActive = true;
    }
    public void EndSpinAttack()
    {
        isSpinDamageActive = false;
        // 다음 공격에서 새로운 피해 판정을 준비한다.
        targets.Clear();
    }
    public void StartComboAttack()
    {
        targets.Clear();
        isComboDamageActive = true;
    }
    public void EndComboAttack()
    {
        isComboDamageActive = false;
        targets.Clear();
    }

    private void Awake()
    {
        // Inspector에 프리팹이 연결되어 있는지 확인한다.
        if (warningCirclePrefab == null)
        {
            Debug.LogWarning("내려찍기 예고 원 프리팹이 연결되지 않았습니다.");
            return;
        }

        // 프리팹을 현재 씬에 한번만 생성한다.
        // 부모를 지정하지 않아 분리된 오브젝트로 유지한다.
        warningCircleInstance = Instantiate(warningCirclePrefab);

        // Hierarchy에서 원본과 실행 중 인스턴스를 구별하기 위한 이름설정
        warningCircleInstance.name = warningCirclePrefab.name + "_Runtime";

        // 게임 시작 시에는 화면에 나타나지 않도록 비활성화한다.
        warningCircleInstance.SetActive(false);
    }

    /// <summary>
    /// AIController에서 초기화 시 호출
    /// </summary>
    /// <param name="defaultAtkPower"></param>
    public void Init(float defaultAtkPower)
    {
        currentAtkPower = defaultAtkPower;
    }

    /// <summary>
    /// 경고 원 프리팹을 배치할때 호출한다.
    /// 원의 크기, 표시 설정
    /// </summary>
    public void SetWarningCircleSize(BossAttackPattern attackType)
    {
        if (warningCircleInstance == null) return;

        // 예고 원을 공격 타입에 따라 위치로 이동한다.
        switch (attackType)
        {
            case BossAttackPattern.StompAttack:
                Vector3 pos = stompHandAtkPoint.position;
                pos.y += warningGroundOffset;
                warningCircleInstance.transform.position = pos;

                float diameter = stompHandRadius * 2f;

                // 현재 스프라이트의 Scale을 가져온다.
                Vector3 scale = warningCircleInstance.transform.localScale;

                // 원의 크기 변경
                scale.x = diameter;
                scale.y = diameter;

                // 값 적용
                warningCircleInstance.transform.localScale = scale;
                break;

            case BossAttackPattern.JumpAttack:
                Vector3 jumpPos = jumpAtkPoint.position;
                jumpPos.y += warningGroundOffset;
                warningCircleInstance.transform.position = jumpPos;

                float meter = jumpRadius * 2f;

                Vector3 jumpScale = warningCircleInstance.transform.localScale;

                jumpScale.x = meter;
                jumpScale.y = meter;

                warningCircleInstance.transform.localScale = jumpScale;
                break;
        }

        warningCircleInstance.SetActive(true);
    }

    /// <summary>
    /// 점프, 내려찍기 애니메이션 타격 프레임에서 1회 호출
    /// </summary>
    public void OnHitImpact(BossAttackPattern type, float motionValue)
    {
        switch(type)
        {
            case BossAttackPattern.StompAttack:
                Collider[] hitTargets
                    = Physics.OverlapSphere(stompHandAtkPoint.position, stompHandRadius, targetLayer);

                foreach(Collider col in hitTargets)
                {
                    ITakeDamage damage = col.GetComponentInParent<ITakeDamage>();
                    if(damage != null)
                    {
                        float final = currentAtkPower * motionValue;
                        damage.TakeDamage(gameObject, final);
                    }
                }
                break;

            case BossAttackPattern.JumpAttack:
                Collider[] hit
                    = Physics.OverlapSphere(jumpAtkPoint.position, jumpRadius, targetLayer);

                foreach (Collider col in hit)
                {
                    ITakeDamage damage = col.GetComponentInParent<ITakeDamage>();
                    if(damage != null)
                    {
                        float final = currentAtkPower * motionValue;
                        damage.TakeDamage(gameObject, final);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// 피해 판정이 일어나는 시점에 호출되어 경고 원을 비활성화한다.
    /// </summary>
    public void HideWarningCircle()
    {
        if(warningCircleInstance != null)
        {
            warningCircleInstance.SetActive(false);
        }
    }

    // 애니메이션이 반영된 뒤의 위치를 확인하기 위함
    private void LateUpdate()
    {
        if (!isSpinDamageActive || !isComboDamageActive) return;

        if(isComboDamageActive)
        {
            switch (attackPattern)
            {
                case BossAttackPattern.leftHandAttack:
                    Collider[] hit1
                        = Physics.OverlapSphere(leftHandAtkPoint.position, leftHandRadius, targetLayer);

                    foreach (Collider col in hit1)
                    {
                        ITakeDamage damage = col.GetComponentInParent<ITakeDamage>();
                        if (damage == null) continue;
                        // 이번 공격에서 이미 피해를 준 대상이라면 건너뛴다.
                        // Add()는 새 대상은 true, 이미 기록된 대상이면 false를 반환한다.
                        if (!targets.Add(damage)) continue;

                        // 이번 공격에서 아직 피해를 받지 않은 대상에게 피해를 준다.
                        damage.TakeDamage(gameObject, currentAtkPower);
                    }
                    break;

                case BossAttackPattern.RightHandAttck:
                    Collider[] hit2
                        = Physics.OverlapSphere(rightHandAtkPoint.position, rightHandRadius, targetLayer);

                    foreach (Collider col in hit2)
                    {
                        ITakeDamage damage = col.GetComponentInParent<ITakeDamage>();
                        if (damage == null) continue;
                        // 이번 공격에서 이미 피해를 준 대상이라면 건너뛴다.
                        // Add()는 새 대상은 true, 이미 기록된 대상이면 false를 반환한다.
                        if (!targets.Add(damage)) continue;

                        // 이번 공격에서 아직 피해를 받지 않은 대상에게 피해를 준다.
                        damage.TakeDamage(gameObject, currentAtkPower);
                    }
                    break;
            }
        }
        if(isSpinDamageActive)
        {
            Collider[] hit3
                = Physics.OverlapSphere(rotationAtkPoint.position, rotationRadius, targetLayer);

            foreach(Collider col in hit3)
            {
                ITakeDamage damage = col.GetComponentInParent<ITakeDamage>();
                if (damage == null) continue;
                // 이번 공격에서 이미 피해를 준 대상이라면 건너뛴다.
                // Add()는 새 대상은 true, 이미 기록된 대상이면 false를 반환한다.
                if (!targets.Add(damage)) continue;

                // 이번 공격에서 아직 피해를 받지 않은 대상에게 피해를 준다.
                damage.TakeDamage(gameObject, currentAtkPower);
            }
        }
    }

    // 에디터 씬에서 공격 범위를 시각화 하는 기즈모 함수
    private void OnDrawGizmosSelected()
    {
        if(leftHandAtkPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(leftHandAtkPoint.position, leftHandRadius);
        }
        if(rightHandAtkPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(rightHandAtkPoint.position, rightHandRadius);
        }
        if(stompHandAtkPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(stompHandAtkPoint.position, stompHandRadius);
        }
        if(rotationAtkPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(rotationAtkPoint.position, rotationRadius);
        }
        if(jumpAtkPoint != null)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(jumpAtkPoint.position, jumpRadius);
        }
    }

    private void OnDestroy()
    {
        if(warningCircleInstance != null)
        {
            Destroy(warningCircleInstance);
        }
    }
}
