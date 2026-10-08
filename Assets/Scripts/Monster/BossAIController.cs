using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossAIController : MonoBehaviour
{
    [Header("기본 상태")]
    [SerializeField] BossState state = BossState.Idle;
    [Header("패턴 설정")]
    [SerializeField, Tooltip("이 시간동안 사거리에 안닿으면 점프 공격")]
    float jumpAttackTriggerTime;
    [SerializeField, Tooltip("땅 구르기 쿨타임")]
    float groundSlamCooldown;
    [Header("광폭화 설정")]
    [SerializeField, Tooltip("체력이 이 비율 이하로 떨어지면 광폭화")]
    float enrageHpRatio = 0.5f;
    bool isEnraged = false;

    [Header("모듈 참조")]
    NavMeshAgent agent;
    Animator animator;
    MonsterStats stats;
    MonsterCombat combat;

    Coroutine activeRoutine;
    Transform targetPlayer;
    Coroutine lookRoutine;

    // 쿨타임 및 타이머 추적용 변수
    float nextGroundSlamTime;
    float currentChaseTime;

    bool wasComboCaceld;

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

        // 게임 시작시 기본 이동속도 적용
        if(stats != null && agent != null)
        {
            agent.speed = stats.MoveSpeed;
        }
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

        // 광폭화 조건 체크 ( 1회 발동)
        if(!isEnraged && (stats.CurHp / stats.MaxHp) <= enrageHpRatio)
        {
            EnterEnrageMode();
        }
    }

    private void EnterEnrageMode()
    {
        isEnraged = true;
        agent.speed = stats.ChaseSpeed;
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
            yield return LookAtTarget(0.4f);
            yield return ExecutePattern(HashSpinAttack, 2f);
        }
        else
        {
            // 정면에 있다면 쿨타임 확인 후 땅구르기 vs 기본 콤보 선택
            if(Time.time >= nextGroundSlamTime)
            {
                StartCoroutine(LookAtTarget(0.45f));
                nextGroundSlamTime = Time.time + groundSlamCooldown;
                yield return ExecutePattern(HashStompAttack, 2f);
            }
            else
            {
                yield return ExecuteComboPattern(HashComboAttack, 3, 1f, 2f);

                // 연계 로직
                // 콤보가 중단되었고, 플레이어와의 거리가 멀다면 즉시 점프 공격으로 전환
                if(wasComboCaceld && targetPlayer != null)
                {
                    float distance = Vector3.Distance(transform.position, targetPlayer.position);

                    // 일정 거리 이상 멀어졌다면 즉시 점프 공격 실행
                    if(distance >= combat.AtkDistance * 1.4f)
                    {
                        Debug.Log("<color=red>[Boss] 플레이어 도망 감지! 즉시 점프 공격 연계</color>");
                        ChangeRoutine(JumpAttackRoutine(), BossState.Attack);
                        yield break; // SelectAttackRoutine 즉시 종료
                    }
                }
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

        yield return LookAtTarget(0.3f);

        // 가변 점프 이동 처리를 위한 서브 코루틴 병행 시작
        StartCoroutine(JumpMoveRoutine());

        // 후딜레이 3초 부여
        yield return ExecutePattern(HashJumpAttack, 3.0f);
        ChangeRoutine(ChaseRoutine(), BossState.Chase);
    }

    private IEnumerator JumpMoveRoutine()
    {
        if (targetPlayer == null) yield break;

        // 도약 시작 시점의 플레이어 위치 저장
        Vector3 startPos = transform.position;
        Vector3 targetPos = targetPlayer.position;

        // 도약 전 대기
        yield return new WaitForSeconds(0.6f);

        // NavmeshAgent 비활성화 및 이동
        agent.enabled = false;
        // 공중에 떠서 목표 위치 까지 날아가는 시간
        // (애니에미션에 맞춰서)
        float jumpDuration = 0.933f;
        float timer = 0f;

        while(timer < jumpDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / jumpDuration;

            // 플레이어 위치까지 보간이동
            transform.position = Vector3.Lerp(startPos, targetPos, progress);
            yield return null;
        }

        // 착지 후 NavMeshAget 다시 활성화
        transform.position = targetPos;
        agent.enabled = true;
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

    /// <summary>
    /// 3타 콤보 전용 코루틴
    /// </summary>
    /// <param name="triggerName"></param>
    /// <param name="maxCombo"></param>
    /// <param name="recoveryTime"></param>
    /// <param name="coolDown"></param>
    /// <returns></returns>
    private IEnumerator ExecuteComboPattern(int triggerName, int maxCombo, float recoveryTime, float coolDown)
    {
        // 콤보 시작 시 플래그 초기화
        wasComboCaceld = false;

        for(int i = 0; i < maxCombo; i++)
        {
            if(i> 0)
            {
                // 이전 타격의 후딜레이 대기
                yield return new WaitForSeconds(recoveryTime);

                // 거리 검사 : 플레이어가 사거리 밖으로 너무 멀어졌는지 확인
                if(targetPlayer != null)
                {
                    float currentDistance = Vector3.Distance(transform.position, targetPlayer.position);

                    // 공격 사거리의 1.4배 이상 멀어졌다면 콤보 중단
                    if (currentDistance > combat.AtkDistance * 1.4f)
                    {
                        Debug.Log("<color=yellow>[Boss] 플레이어가 도망침 - 콤보 중단 후 추격 전환</color>");
                        wasComboCaceld = true; // 취소 플래그 설정
                        break;
                    }
                }

                // 거리 내에 있다면 플레이어를 향해 회전 후 다음 타수 진입
                if(lookRoutine != null) StopCoroutine(lookRoutine);
                yield return lookRoutine = StartCoroutine(LookAtTarget(0.3f));
            }
            else
            {
                // 1타 : 0.6초간 플레이어를 향해 회전
                if (lookRoutine != null) StopCoroutine(lookRoutine);
                lookRoutine =  StartCoroutine(LookAtTarget(0.6f));
            }
            animator.SetTrigger(triggerName);
        }

        // 진행 중이던 공격 애니메이션 모션이 와전히 마칠때까지 대기
        yield return new WaitUntil(() => !animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack")
        && !animator.IsInTransition(0));
        
        // 콤보가 중단된 경우 후딜레이를 대폭 줄여 점프공격으로 빠르게 연결
        if(wasComboCaceld)
        {
            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            // 정상 완주 시 원래 후딜레이 적용
            yield return new WaitForSeconds(coolDown);
        }
    }

    /// <summary>
    /// 플레이어를 바라봅니다.
    /// duration을 넣지 않거나 0이면 즉시 회전, 시간을 넘겨주면 부드럽게 보간 회전합니다.
    /// </summary>
    private IEnumerator LookAtTarget(float duration = 0f)
    {
        if(targetPlayer == null) yield break;
        Vector3 lookDir = targetPlayer.position - transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude <= 0.01f) yield break;

        Quaternion targetRotation = Quaternion.LookRotation(lookDir);

        // duration = 0 이면 즉시회전
        if (duration <= 0f)
        {
            transform.rotation = targetRotation;
            yield break;
        }

        // duration이 지정되어 있으면 해당 시간 동안 부드럽게 회전
        float timer = 0f;
        Quaternion startRotation = transform.rotation;
        while(timer < duration)
        {
            timer += Time.deltaTime;

            // 회전하는 도중 플레이어가 조금씩 이동하는 것도 실시간 반영
            lookDir = targetPlayer.position - transform.position;
            lookDir.y = 0f;

            if(lookDir.sqrMagnitude > 0.01f)
            {
                targetRotation = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, timer / duration);
            }
            yield return null;
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

    private void OnDrawGizmosSelected()
    {
        if (combat == null) combat = GetComponent<MonsterCombat>();
        if (combat == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, combat.AtkDistance);
    }
}
