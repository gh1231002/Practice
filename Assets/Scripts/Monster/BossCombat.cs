using System.Collections.Generic;
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
    [SerializeField, Tooltip("내려찍기 공격 포인트")] Transform stompAtkPoint;
    [SerializeField, Tooltip("회전 공격 포인트")] Transform spinAtkPoint;
    [SerializeField, Tooltip("점프 공격 포인트")] Transform jumpAtkPoint;

    [SerializeField, Tooltip("왼손 임팩트 범위 반지름")] float leftHandRadius;
    [SerializeField, Tooltip("오른손 임팩트 범위 반지름")] float rightHandRadius;
    [SerializeField, Tooltip("내려찍기 임팩트 범위 반지름")] float stompRadius;
    [SerializeField, Tooltip("회전 공격 임팩트 범위 반지름")] float rotationRadius;
    [SerializeField, Tooltip("점프 공격 임팩트 범위 반지름")] float jumpRadius;

    [SerializeField, Tooltip("공격 가능 사거리")] float atkDistance;
    [SerializeField, Tooltip("타격 대상 레이어")] LayerMask targetLayer;

    [Header("공격별 피해 배율 설정")]
    [SerializeField, Min(0f)]
    float stompDamageMultiplier;
    [SerializeField, Min(0f)]
    float jumpDamageMultiplier;
    [SerializeField, Min(0f)]
    float spinDamageMultiplier;

    // 애니메이션 이벤트에서 타수별 피해 비율 값을 저장한다.
    float comboDamageMultiplier;

    [Header("경고 원 스프라이트")]
    [SerializeField] GameObject warningCirclePrefab;
    [SerializeField, Tooltip("바닥과 겹쳐 깜빡이는 현상 방지")] float warningGroundOffset;

    public float AtkDistance => atkDistance;

    float currentAtkPower;

    GameObject warningCircleInstance;

    // 피해 판정이 활성화되어 있는지 확인한다.
    bool isSpinDamageActive;
    bool isComboDamageActive;

    // 점프 공격 시작 시 정한 착지 예정 위치를 저장한다.
    // 경고 원과 실제 피해 판정에서 같은 좌표를 사용하기 위함이다.
    Vector3 jumpLandingPos;

    // 착지 위치가 정상적으로 설정되었는지 확인한다.
    // 위치가 설정되지 않았는데 피해가 발생하는 것을 방지한다.
    bool hasJumpLandingPos;

    BossAttackPattern attackPattern;

    // 이미 피해를 받은 대상을 기록한다.
    // 같은 대상의 콜라이더가 여러 개여도 중복 피해를 막기 위한 용도이다.
    readonly HashSet<ITakeDamage> jumpAndStompAttackTargets = new HashSet<ITakeDamage>();
    readonly HashSet<ITakeDamage> comboTargets = new HashSet<ITakeDamage>();
    readonly HashSet<ITakeDamage> spinTargets = new HashSet<ITakeDamage>();

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
    public void SetWarningCircle(BossAttackPattern attackType, Transform targetPlayer = null)
    {
        // 경고 원 오브젝트가 없다면 실행하지 않는다.
        if (warningCircleInstance == null) return;

        // 공격 별 위치과 반경을 저장한 다음 공통 처리한다.
        Vector3 warningPos = Vector3.zero;
        float waringRadius = 0f;
        // 예고 원을 공격 타입에 따라 위치로 이동한다.
        switch (attackType)
        {
            case BossAttackPattern.StompAttack:
                warningPos = stompAtkPoint.position;
                waringRadius = stompRadius;
                break;

            case BossAttackPattern.JumpAttack:
                if(targetPlayer == null)
                {
                    hasJumpLandingPos = false;
                    warningCircleInstance.SetActive(false);
                    return;
                }

                // 플레이어 위치를 점프 공격 착지 예정 위치로 사용한다.
                // 실제 피해 판정에서도 이 좌표를 재사용한다.
                jumpLandingPos = targetPlayer.position;
                hasJumpLandingPos = true;

                warningPos = jumpLandingPos;
                waringRadius = jumpRadius;
                break;
        }

        // 경고 원 표시 위치만 높이 오프셋을 적용한다.
        // 실제 피해 판정 좌표에는 이 오프셋을 더하지 않는다.
        warningPos.y += warningGroundOffset;
        warningCircleInstance.transform.position = warningPos;

        // 원 지름을 계산
        float diameter = waringRadius * 2f;

        // Scale 값 변경 후 적용
        Vector3 scale = warningCircleInstance.transform.localScale;
        scale.x = diameter;
        scale.y = diameter;
        warningCircleInstance.transform.localScale = scale;

        // 경고 원 활성화
        warningCircleInstance.SetActive(true);
    }

    /// <summary>
    /// 내려찍기 애니메이션의 실제 충격 프레임에서 호출한다.
    /// </summary>
    private void StompImpact()
    {
        // 공격 포인트가 연결되지 않았다면 실행하지 않는다.
        if(stompAtkPoint == null) return;

        Collider[] hits = Physics.OverlapSphere(stompAtkPoint.position, stompRadius, targetLayer);
        // 피해 함수 호출
        ApplyDamageToTargets(hits, jumpAndStompAttackTargets, stompDamageMultiplier);
    }
    /// <summary>
    /// 점프공격 애니메이션의 실제 충격 프레임에서 호출한다.
    /// </summary>
    private void JumpImpact()
    {
        // 착지 위치가 설정되지 않았다면 실행하지 않는다.
        if(!hasJumpLandingPos) return;

        Collider[] hits = Physics.OverlapSphere(jumpLandingPos, jumpRadius, targetLayer);
        ApplyDamageToTargets(hits, jumpAndStompAttackTargets, jumpDamageMultiplier);

        // 같은 착지 위치가 다음 공격에서 잘못 재사용되지 않도록 정리한다.
        hasJumpLandingPos = false;
    }

    /// <summary>
    /// 검출된 콜라이더를 대상으로 피해를 적용한다.
    /// 지정된 공격 구간에서 같은 대상은 한 번만 피해를 받는다.
    /// </summary>
    private void ApplyDamageToTargets(Collider[] hitColliders, HashSet<ITakeDamage> hitTargets, float damageMultiplier)
    {
        foreach (Collider col in hitColliders)
        {
            // 콜라이더가 자식에 있더라도 부모에서 피해 처리 인터페이스를 찾는다.
            ITakeDamage damage = col.GetComponentInParent<ITakeDamage>();

            // 피해 처리 인터페이스가 없으면 건너뛴다.
            if (damage == null)
                continue;

            // HashSet.Add()는 새 대상이면 true, 이미 등록된 대상이면 false를 반환한다.
            // 따라서 같은 구간에서 동일 대상에게 중복 피해를 주지 않는다.
            if (!hitTargets.Add(damage))
                continue;

            // 기본 공격력과 해당 공격의 배율을 적용한 피해를 전달한다.
            float finalDamage = currentAtkPower * damageMultiplier;
            damage.TakeDamage(gameObject, finalDamage);
        }
    }

    /// <summary>
    /// 피해 판정이 일어나는 시점에 호출되어 경고 원을 비활성화한다.
    /// </summary>
    private void HideWarningCircle()
    {
        if(warningCircleInstance != null)
        {
            warningCircleInstance.SetActive(false);
        }
    }

    // 애니메이션이 반영된 뒤의 위치를 확인하기 위함
    private void LateUpdate()
    {
        if(isComboDamageActive)
        {
            CheckComboDamage();
        }
        if(isSpinDamageActive)
        {
            Collider[] hits = Physics.OverlapSphere(spinAtkPoint.position, rotationRadius, targetLayer);
            ApplyDamageToTargets(hits, spinTargets, spinDamageMultiplier);
        }
    }

    /// <summary>
    /// 현재 콤보 공격 종류에 맞는 충돌 판정을 실행한다.
    /// </summary>
    private void CheckComboDamage()
    {
        Collider[] hits;

        // 현재 타격 종류에 따라 올바른 공격 포인트와 반경을 선택한다.
        switch (attackPattern)
        {
            case BossAttackPattern.leftHandAttack:
                hits = Physics.OverlapSphere(
                    leftHandAtkPoint.position,
                    leftHandRadius,
                    targetLayer
                );
                break;

            case BossAttackPattern.RightHandAttack:
                hits = Physics.OverlapSphere(
                    rightHandAtkPoint.position,
                    rightHandRadius,
                    targetLayer
                );
                break;

            default:
                // 지원하지 않는 공격 유형이면 피해 판정을 실행하지 않는다.
                return;
        }

        // 콤보 공격 전용 대상 목록과 피해 배율을 사용한다.
        ApplyDamageToTargets(hits, comboTargets, comboDamageMultiplier);
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
        if(stompAtkPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(stompAtkPoint.position, stompRadius);
        }
        if(spinAtkPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(spinAtkPoint.position, rotationRadius);
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

    // 애니메이션 이벤트 함수
    // 점프 공격
    public void OnJumpLanding()
    {
        jumpAndStompAttackTargets.Clear();
        attackPattern = BossAttackPattern.JumpAttack;
        JumpImpact();
        HideWarningCircle();
    }
    // 내려찍기 공격
    public void OnStompLanding()
    {
        jumpAndStompAttackTargets.Clear();
        attackPattern = BossAttackPattern.StompAttack;
        StompImpact();
        HideWarningCircle();
    }
    // 콤보 공격
    public void BeginLeftAttack(float damageMultiplier)
    {
        comboTargets.Clear();
        attackPattern = BossAttackPattern.leftHandAttack;
        comboDamageMultiplier = Mathf.Max(0f, damageMultiplier);
        isComboDamageActive = true;
    }
    public void EndLeftAndRightAttack()
    {
        isComboDamageActive = false;
        comboTargets.Clear();
    }
    public void BeginRightAttack(float damageMultiplier)
    {
        comboTargets.Clear();
        attackPattern = BossAttackPattern.RightHandAttack;
        comboDamageMultiplier = Mathf.Max(0f, damageMultiplier);
        isComboDamageActive = true;
    }
    // 회전 공격
    public void BeginSpinAttack(float damageMultiplier)
    {
        spinTargets.Clear();
        spinDamageMultiplier = Mathf.Max(0f, damageMultiplier);
        isSpinDamageActive = true;
    }
    public void EndSpinAttack()
    {
        isSpinDamageActive = false;
        spinTargets.Clear();
    }

    /// <summary>
    /// 공격 애니메이션이 강제로 중단되었을 때 활성화된 판정을 정리한다.
    /// 보스의 사망, 피격 경직, 공격 취소 등의 상황에서 호출한다.
    /// </summary>
    public void CancelActiveDamageWindows()
    {
        // 지속 피해 판정이 더 이상 실행되지 않도록 비활성화한다.
        isComboDamageActive = false;
        isSpinDamageActive = false;

        // 이전 공격에서 사용한 대상 목록들을 정리한다.
        comboTargets.Clear();
        spinTargets.Clear();
        jumpAndStompAttackTargets.Clear();

        // 저장한 착지 위치가 다음 공격에 재사용되지 않도록 무효화한다.
        hasJumpLandingPos = false;

        // 화면에 남아 있는 경고 원도 숨긴다.
        HideWarningCircle();
    }
}
