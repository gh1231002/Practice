using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.AI;

public class BossAIController : MonoBehaviour
{
    [Header("기본 상태")]
    [SerializeField] BossState state = BossState.Idle;
    [Header("패턴 설정")]
    [SerializeField, Tooltip("플레이어가 이 거리 이상 멀어지면 점프 공격")]
    float jumpAttackDistance;
    [SerializeField, Tooltip("점프 공격 재사용 대기시간")]
    float jumpAttackCooldown;
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
    BossCombat combat;

    Coroutine activeRoutine;
    Transform targetPlayer;

    // 쿨타임 및 타이머 추적용 변수
    float nextGroundSlamTime;
    float nextJumpAttackTime;

    // 콤보가 중간에 취소되었는지 기록
    bool wasComboCaceld;
    // 현재 콤보가 진행 중인지 확인한다.
    bool isComboActive;
    // 3타 애니메이션이 끝남을 알리는 값
    bool comboFinished;
    // 현재 몇번째 타격인지 기록
    int comboIndex;
    // 이번 콤보의 최대 타격 수 기록
    int comboMaxIndex;
    // 현재 실행 중인 콤보에서 다음 타격에 사용할 Trigger ID
    // 콤보마다 다른 Trigger를 사용할 수 있도록 저장한다.
    int activeCombeNextTriggerHash;

    // Animator 파라미터
    static readonly int HashIsRun = Animator.StringToHash("isRun");
    static readonly int HashMoveX = Animator.StringToHash("MoveX");
    static readonly int HashMoveZ = Animator.StringToHash("MoveZ");
    static readonly int HashDeath = Animator.StringToHash("Death");
    static readonly int HashTaunt = Animator.StringToHash("Taunt");
    static readonly int HashComboStart = Animator.StringToHash("ComboStart");
    static readonly int HashComboNext = Animator.StringToHash("ComboNext");
    static readonly int HashJumpAttack = Animator.StringToHash("JumpAttack");
    static readonly int HashSpinAttack = Animator.StringToHash("SpinAttack");
    static readonly int HashStompAttack = Animator.StringToHash("StompAttack");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        stats = GetComponent<MonsterStats>();
        combat = GetComponent<BossCombat>();

        animator.applyRootMotion = false;
    }

    private void Start()
    {
        if (stats == null || combat == null || agent == null)
        {
            enabled = false;
            return;
        }

        stats.OnTakeDamage += HandleBossDamaged;
        stats.OnDeath += Death;
        combat.Init(stats.AtkPower);
        agent.speed = stats.MoveSpeed;
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

        // 점프 중 에이전트가 비활성화되거나 NavMesh에서 벗어났다면
        // isStopped와 reainingDistace를 읽지 않는다.
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            animator.SetFloat(HashMoveX, 0f, 0.1f, Time.deltaTime);
            animator.SetFloat(HashMoveZ, 0f, 0.1f, Time.deltaTime);
            return;
        }

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

    /// <summary>
    /// MonsterStats에서 피해 발생 이벤트를 전달받았을 때 실행한다.
    /// 실제 체력 감소는 MonsterStats가 담당하며,
    /// 이 함수는 보스의 광폭화 여부만 판단한다.
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="damage"></param>
    private void HandleBossDamaged(GameObject attacker, float damage)
    {
        // 사망한 보스는 광폭화 처리 하지 않음
        if (state == BossState.Die) return;
        // 이미 광폭화했다면 중복 발동 금지
        if (isEnraged) return;
        // 최대 체력이 0 이하라면 체력비율 계산 안함
        if (stats.MaxHp <= 0) return;

        // 정수 나눗셈으로 소수점 이하가 사라지는 상황을 방지한다.
        float hpRatio = (float)stats.CurHp / stats.MaxHp;
        // 광폭화 조건 체크 ( 1회 발동)
        if (hpRatio <= enrageHpRatio)
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
            // 일정 거리 이상 플레이어가 멀고,
            // 점프 공격의 재사용 대기시간도 끝났다면 점프 공격 선택
            if(distance >= jumpAttackDistance && Time.time >= nextJumpAttackTime)
            {
                // 다음 점프 공격이 가능해지는 시각을 저장한다.
                // 공격 시작 직후 같은 패턴이 반복해서 선택되는 것을 방지한다.
                nextJumpAttackTime = Time.time + jumpAttackCooldown;
                ChangeRoutine(JumpAttackRoutine(), BossState.Attack);
                yield break;
            }

            // 위 조건에 해당하지 않으면 플레이어를 계속 추격한다.
            // 설정한 초마다 위치와 거리를 다시 확인한다.
            agent.SetDestination(targetPlayer.position);
            yield return new WaitForSeconds(0.2f);
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
            yield return ExecutePattern(HashSpinAttack, 1f);
        }
        else
        {
            // 정면에 있다면 쿨타임 확인 후 땅구르기 vs 기본 콤보 선택
            if(Time.time >= nextGroundSlamTime)
            {
                StartCoroutine(LookAtTarget(0.45f));
                nextGroundSlamTime = Time.time + groundSlamCooldown;
                combat.SetWarningCircle(BossAttackPattern.StompAttack);
                yield return ExecutePattern(HashStompAttack, 1f);
            }
            else
            {
                yield return ExecuteComboPattern(HashComboStart, HashComboNext, 3, 1f);

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

        // 후딜레이 부여
        yield return ExecutePattern(HashJumpAttack, 1.5f);
        ChangeRoutine(ChaseRoutine(), BossState.Chase);
    }

    private IEnumerator JumpMoveRoutine()
    {
        if (targetPlayer == null) yield break;

        // 도약 시작 시점의 플레이어 위치 저장
        Vector3 startPos = transform.position;
        Vector3 targetPos = targetPlayer.position;

        // 경고 원 배치 함수 호출
        combat.SetWarningCircle(BossAttackPattern.JumpAttack, targetPlayer);

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
    /// 콤보의 시작, 종료 대기, 쿨다운을 관리한다.
    /// 개별 타격의 진행은 애니메이션 이벤트에 맡긴다.
    /// </summary>
    private IEnumerator ExecuteComboPattern(int startTriggerHash, int nextTriggerHash, int maxCombo, float coolDown)
    {
        // 공격할 횟수가 1회 미만이면 실행하지 않는다.
        if (maxCombo < 1) yield break;

        // 새로운 콤보 시작 전 bool 초기화
        wasComboCaceld = false;
        comboFinished = false;
        isComboActive = false;

        // 첫 타격부터 시작한다.
        comboIndex = 1;
        comboMaxIndex = maxCombo;
        
        // 현재 콤보가 사용할 다음 타격 Trigger를 저장한다.
        activeCombeNextTriggerHash = nextTriggerHash;

        // 이전 실행에서 남아 있을 수 잇는 Trigger를 정리한다.
        animator.ResetTrigger(startTriggerHash);
        animator.ResetTrigger(nextTriggerHash);

        // 콤보 시작 전 플레이어를 향해 한 번 부드럽게 회전한다.
        if (targetPlayer != null)
        {
            yield return LookAtTarget(0.3f);
        }

        // 회전하는 동안 플레이어가 사라졌는지 다시 확인한다.
        if(targetPlayer == null)
        {
            wasComboCaceld = true;
        }
        else
        {
            // 실제 콤보가 시작할 때부터 활성 상태로 전환
            isComboActive = true;
            // 첫 타격을 시작하는 Trigger를 설정
            animator.SetTrigger(startTriggerHash);
        }

        // 3타 종료되거나 콤보가 취소될 때까지 기다린다.
        yield return new WaitUntil(() => comboFinished || wasComboCaceld);

        // 진행 중이던 공격 애니메이션 모션이 와전히 마칠때까지 대기
        yield return new WaitUntil(() => !animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack")
        && !animator.IsInTransition(0));

        // 콤보 종료 후 다음 공격을 준비 할수 있게 상태 정리
        isComboActive = false;
        animator.ResetTrigger(startTriggerHash);
        animator.ResetTrigger(nextTriggerHash);
        activeCombeNextTriggerHash = 0;

        // 콤보가 중단된 경우 후딜레이를 대폭 줄여 점프공격으로 빠르게 연결
        if (wasComboCaceld)
        {
            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            // 정상 완주 시 원래 후딜레이 적용
            yield return new WaitForSeconds(coolDown);
        }
    }

    /// <summary>
    /// 애니메이션 이벤트에서 호출한다.
    /// 다음 공격을 시작할 수 있는지 검사하고 트리거를 전송한다.
    /// </summary>
    public void RequestNextCombo()
    {
        // 콤보가 진행 중이 아니거나 이미 취소되었다면 종료한다.
        if (!isComboActive || wasComboCaceld) return;
        // 최대 타격 수에 도달 했다면 다음 공격을 요청하지 않음
        if (comboIndex >= comboMaxIndex) return;
        // 플레이어가 사라졌다면 취소
        if (targetPlayer == null)
        {
            wasComboCaceld = true;
            animator.ResetTrigger(activeCombeNextTriggerHash);
            return;
        }

        // 현재 골렘과 플레이어 사이의 거리를 검사한다.
        float currentDistance = Vector3.Distance(transform.position, targetPlayer.position);

        // 공격 사거리 보다 멀어졌다면 콤보 중단
        if (currentDistance > combat.AtkDistance)
        {
            wasComboCaceld = true; // 취소 플래그 설정
            animator.ResetTrigger(activeCombeNextTriggerHash);
            Debug.Log("<color=yellow>[Boss] 플레이어가 도망침 - 콤보 중단</color>");
            return;
        }

        // 다음 콤보 번호로 진행
        comboIndex++;

        // 다음 공격을 Animator에 요정한다.
        animator.SetTrigger(HashComboNext);
    }

    /// <summary>
    /// 마지막 콤보 종료 시점에 호출한다.
    /// 코루틴에 콤보가 완료되었다는 사실을 알린다.
    /// </summary>
    public void ComboFinished()
    {
        // 콤보가 정상적으로 진행 중인지 확인한다.
        if (!isComboActive || wasComboCaceld) return;
        // 마지막 콤보가 진행된 상태에서만 정상 완료 처리한다.
        if (comboIndex != comboMaxIndex) return;
        
        comboFinished = true;
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

    private void OnDestroy()
    {
        if(stats != null)
        {
            stats.OnTakeDamage -= HandleBossDamaged;
            stats.OnDeath -= Death;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (combat == null) combat = GetComponent<BossCombat>();
        if (combat == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, combat.AtkDistance);
    }
}
