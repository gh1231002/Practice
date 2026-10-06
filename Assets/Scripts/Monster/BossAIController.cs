using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossAIController : MonoBehaviour
{
    [Header("기본 상태")]
    [SerializeField] BossState state = BossState.Idle;
    [Header("패턴 설정")]
    [SerializeField, Tooltip("이 시간동안 사거리에 안닿으면 점프 공격")]
    float jumpAttackTriggerTime = 4.0f;
    [SerializeField, Tooltip("땅 구르기 쿨타임")]
    float groundSlamCooldown = 15f;

    [Header("모듈 참조")]
    NavMeshAgent agent;
    Animator animator;
    MonsterStats stats;
    MonsterCombat combat;

    Coroutine activeRoutine;
    Transform targetPlayer;

    // 쿨타임 및 타이머 추적용 변수
    float nextGroundSlamTime;
    float currentChaseTime;

    // Animator 파라미터
    static readonly int HashIsRun = Animator.StringToHash("isRun");
    static readonly int HashMoveX = Animator.StringToHash("MoveX");
    static readonly int HashMoveZ = Animator.StringToHash("MoveZ");
    static readonly int HashDeath = Animator.StringToHash("Death");
    static readonly int HashTaunt = Animator.StringToHash("Taunt");
    static readonly int HashComboAttack = Animator.StringToHash("ComboAttack");
    static readonly int HashJumpAttack = Animator.StringToHash("JumpAttack");
    static readonly int HashSpinAttack = Animator.StringToHash("SpinAttack");
    static readonly int HashStompAttack = Animator.StringToHash("StompAttack");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        stats = GetComponent<MonsterStats>();
        combat = GetComponent<MonsterCombat>();

        animator.applyRootMotion = false;
    }

    private void Start()
    {
        stats.OnTakeDamage += TakeDamage;
        stats.OnDeath += Death;
    }

    private void Update()
    {
        UpdateMovementAnimation();
    }

    /// <summary>
    /// NavMeshAgent의 실제 이동 벡터를 보스의 로컬 기준 Blend Tree 파라미터로 변환하여 전달
    /// </summary>
    private void UpdateMovementAnimation()
    {
        if (agent == null || animator == null) return;

        // 에이전트가 멈춰있거나 목적지가 없을 때 0,0 (idle) 처리
        if (agent.isStopped || agent.remainingDistance <= agent.stoppingDistance)
        {
            animator.SetFloat(HashMoveX, 0f, 0.1f, Time.deltaTime);
            animator.SetFloat(HashMoveZ, 0f, 0.1f, Time.deltaTime);
            return;
        }

        // 월드 이동 속도 벡터 취득
        Vector3 worldVelocity = agent.desiredVelocity;
        // 보스의 현재 바라보는 방향 기준으로 전환
        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        // Normalized로 정규화
        float moveX = localVelocity.x;
        float moveZ = localVelocity.z;

        // 애니메이터 블렌드 트리에 부드럽게 전달
        animator.SetFloat(HashMoveX, moveX, 0.1f, Time.deltaTime);
        animator.SetFloat(HashMoveZ, moveZ, 0.1f, Time.deltaTime);
    }

    private void TakeDamage(GameObject attacker, float damage)
    {
        if (state == BossState.Die) return;
    }

    public void StartBossBattle(Transform player)
    {
        targetPlayer = player;
        // 코루틴 변경
        ChangeRoutine(ChaseRoutine(), BossState.Chase);
    }

    protected void ChangeRoutine(IEnumerator newRoutine, BossState newState)
    {
        if (state == BossState.Die) return;

        state = newState;
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(newRoutine);
    }

    private IEnumerator ChaseRoutine()
    {
        state = BossState.Chase;
        agent.isStopped = false;
        // 추격 시작 시 타이머 초기화
        currentChaseTime = 0f;

        while(true)
        {
            if (targetPlayer == null) yield break;

            float distance = Vector3.Distance(transform.position, targetPlayer.position);

            // 공격 사거리 진입 시 공격 패턴 진입
            if(distance <= combat.AtkDistance)
            {
                // 루틴 변경
                ChangeRoutine(SelectAttackRoutine(), BossState.Attack);
                yield break;
            }
            // 거리가 멀어서 사거리에 안닿지만, 일정 시간 이상 추격만 했을 때
            // 점프 공격 실행 
            if(currentChaseTime >= jumpAttackTriggerTime)
            {
                ChangeRoutine(JumpAttackRoutine(), BossState.Attack);
                yield break;
            }

            agent.SetDestination(targetPlayer.position);
            yield return new WaitForSeconds(0.2f);
            currentChaseTime += 0.1f;
        }
    }

    private IEnumerator SelectAttackRoutine()
    {
        state = BossState.Attack;
        agent.isStopped = true;

        // 플레이어가 보스의 등 뒤에 있는지 확인
        if(IsPlayerBehind())
        {
            // 360도 공격 전 살짝 돌아보는 느낌 연출
            LookAtTarget();
            yield return ExecutePattern(HashSpinAttack, 2.0f);
        }
        else
        {
            LookAtTarget();

            // 정면에 있다면 쿨타임 확인 후 땅구르기 vs 기본 콤보 선택
            if(Time.time >= nextGroundSlamTime)
            {
                nextGroundSlamTime = Time.time + groundSlamCooldown;
                yield return ExecutePattern(HashStompAttack, 2.5f);
            }
            else
            {
                yield return ExecutePattern(HashComboAttack, 1f);
            }
        }

        // 패턴 종료 후 다시 추격
        ChangeRoutine(ChaseRoutine(), BossState.Chase);
    }

    // 점프 공격 전용 루틴
    private IEnumerator JumpAttackRoutine()
    {
        state = BossState.Attack;
        agent.isStopped = true;

        LookAtTarget();
        // 후딜레이 3초 부여
        yield return ExecutePattern(HashJumpAttack, 3.0f);
        ChangeRoutine(ChaseRoutine(), BossState.Chase);
    }

    /// <summary>
    /// 플레이어가 보스 등 뒤에 있는지 판별하는 함수
    /// </summary>
    /// <returns></returns>
    private bool IsPlayerBehind()
    {
        if (targetPlayer == null) return false;

        Vector3 toPlayer = (targetPlayer.position - transform.position).normalized;
        // 보스의 정면 벡터와 플레이어 방향 벡터를 내적
        float dotProduct = Vector3.Dot(transform.forward, toPlayer);

        // 내적값이 0미만이면 보스 양옆 90도 바깥(뒤쪽)에 있다는 뜻입니다.
        // -0.2f 정도롤 설정하면 확실히 등 뒤쪽 100~180도 범위에 있을 때만 발동됩니다.
        return dotProduct < 0.2f;
    }

    /// <summary>
    /// 패턴 실행 및 애니메이션 완료 대기 공통 로직
    /// </summary>
    /// <param name="triggerName"></param>
    /// <param name="cooldown">공격 후 후딜레이</param>
    /// <returns></returns>
    private IEnumerator ExecutePattern(int triggerName, float cooldown)
    {
        animator.SetTrigger(triggerName);
        // 애니메이션 완료 대기
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack")
        || animator.IsInTransition(0));
        yield return new WaitUntil(() => !animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack")
        && !animator.IsInTransition(0));

        yield return new WaitForSeconds(cooldown);
    }

    private void LookAtTarget()
    {
        if(targetPlayer == null) return;
        Vector3 lookDir = targetPlayer.position - transform.position;
        lookDir.y = 0f;
        if(lookDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    /// <summary>
    /// 플레이어를 시선 고정한 채로 옆이나 뒤로 이동하고 싶을 때 사용
    /// </summary>
    /// <param name="isLockOn"></param>
    public void SetLockOnStrafeMode(bool isLockOn)
    {
        // NavMeshAgent의 자체 회전을 끄고 수동으로 플레이어를 바라보게 만듦
        agent.updateRotation = !isLockOn;
    }

    private void Death()
    {
        state = BossState.Die;
        if (activeRoutine != null) StopCoroutine(activeRoutine);

        animator.SetTrigger(HashDeath);
        agent.isStopped = true;
        agent.enabled = false;

        if (TryGetComponent<Collider>(out var col)) col.enabled = false;
    }
}
