using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 몬스터 상태 정의
public enum MonsterState
{
    Idle, Patrol, Chase, Attack, Hit, Die
}

// 몬스터의 '뇌' 역할을 담당, 각 모듈의 이벤트를 구독하여 상태를 전환
// NavMeshagent 및 Animator를 총괄 제어
public class MonsterAIController : MonoBehaviour
{
    [Header("순찰 및 대기 설정")]
    [SerializeField, Tooltip("순찰 가능 구역")] Collider patrolRange;
    [SerializeField, Tooltip("목적지 도착 후 대기 시간")] float waitTimer;

    [Header("회피 우선순위 설정")]
    [SerializeField, Tooltip("정지 상태 시 우선순위 (작을수록 고정체로 인식)")] int stationaryPriority;
    [SerializeField, Tooltip("이동 상태 시 최소 우선순위")] int minMovingPriority;
    [SerializeField, Tooltip("이동 상태 시 최대 우선순위")] int maxMovingPriority;

    protected MonsterState state = MonsterState.Idle;
    public MonsterState State => state;

    // 모둘형 스크립트 참조
    protected NavMeshAgent navAgent;
    protected Animator monAnim;
    protected MonsterStats stats;
    protected MonsterDetect detect;
    protected MonsterCombat combat;

    // 현재 실행 중인 코루틴 저장용
    Coroutine activeRoutine;
    Transform targetPlayer;

    // Animator 파라미터 StringToHash
    static readonly int HashAttack = Animator.StringToHash("Attack");
    static readonly int HashMove = Animator.StringToHash("Move");
    static readonly int HashHit = Animator.StringToHash("Hit");
    static readonly int HashDeath = Animator.StringToHash("Death");

    /// <summary>
    /// [오브젝트 풀 연동] 외부 스포너 설정 및 초기 순찰 구역 지정
    /// </summary>
    /// <param name="rangeArea"></param>
    public void SetPatrolRange(Collider rangeArea)
    {
        patrolRange = rangeArea;
    }

    /// <summary>
    /// [오브젝트 풀 연동] 재소환 시 NavMesh, 콜라이더, 애니메이터 및 AI 상태 복구
    /// </summary>
    public void InitAI()
    {
        state = MonsterState.Idle;
        // 컴포넌트 및 스탯 재활성화
        stats.ResetStats();
        if (TryGetComponent<Collider>(out var col)) col.enabled = true;

        // 초기화 시 정지 우선순위 적용
        SetAvoidancePriority(true);

        // 타깃 및 애니메이터 파라미터 리셋
        targetPlayer = null;
        monAnim.Rebind();
        monAnim.Update(0f);
        monAnim.applyRootMotion = false;

        // 순찰 상태 재시작
        if(patrolRange != null)
        {
            ChangeRoutine(PatrolRoutine(), MonsterState.Patrol);
        }
    }

    /// <summary>
    /// [동적 회피 핵심] 이동/정지 상태에 따라 NavMeshAgent의 회피 우선순위를 변경합니다.
    /// </summary>
    /// <param name="isStationary">서 있는 상태(True) 또는 이동 중인 상태(False)</param>
    private void SetAvoidancePriority(bool isStationary)
    {
        if(navAgent == null || !navAgent.enabled) return;

        if(isStationary)
        {
            // 서있을 때는 높은 우선순위를 부여해 지나가는 몬스터가 '벽'처럼 인식하고 돌아감
            navAgent.avoidancePriority = stationaryPriority;
            navAgent.velocity = Vector3.zero;
        }
        else
        {
            // 이동 중일 때는 낮은 우선순위를 무작위 할당해 이동체끼리 서로 우회하도록 유도
            navAgent.avoidancePriority = Random.Range(minMovingPriority, maxMovingPriority);
        }
    }

    protected virtual void Awake()
    {
        // 필수 컴포넌트 참조
        navAgent = GetComponent<NavMeshAgent>();
        monAnim = GetComponent<Animator>();
        stats = GetComponent<MonsterStats>();
        detect = GetComponent<MonsterDetect>();
        combat = GetComponent<MonsterCombat>();

        // 위치 고정 버그를 방지하기 위해 RootMotion 비활성화
        monAnim.applyRootMotion = false;
    }

    protected virtual void Start()
    {
        // 전투 스크립트에 공격력 전달
        combat.Init(stats.AtkPower);
        navAgent.speed = stats.MoveSpeed;

        // 이벤트 구독
        stats.OnTakeDamage += TakeDamage;
        stats.OnDeath += Death;

        detect.OnPlayerDetected += PlayerDetected;
        detect.OnPlayerLost += PlayerLost;
        detect.OnPatrolLost += PatrolLost;

        // 초기 순찰 상태 시작
        if(patrolRange != null)
        {
            ChangeRoutine(PatrolRoutine(), MonsterState.Patrol);
        }
    }

    private void PlayerDetected(Transform player)
    {
        if (state == MonsterState.Die) return;

        targetPlayer = player;
        ChangeRoutine(ChaseRoutine(), MonsterState.Chase);
    }

    private void PlayerLost()
    {
        targetPlayer = null;
    }

    private void PatrolLost()
    {
        if(state == MonsterState.Die) return;
        targetPlayer = null;
        ChangeRoutine(PatrolRoutine(), MonsterState.Patrol);
    }

    // 피격 이벤트 수신 처리
    private void TakeDamage(GameObject attacker, float damage)
    {
        if (state == MonsterState.Die) return;

        // 시야 밖 공격 피겨 시, 나를 공격한 주체를 타깃으로 할당
        if(attacker != null)
        {
            targetPlayer = attacker.transform;
        }

        // 피격 상태로 전환
        ChangeRoutine(HitRoutine(), MonsterState.Hit);
    }

    private void Death()
    {
        state = MonsterState.Die;
        if (activeRoutine != null) StopCoroutine(activeRoutine);

        // 애니메이션 트리거 초기화 및 사망 실행
        monAnim.ResetTrigger(HashAttack);
        monAnim.ResetTrigger(HashHit);
        monAnim.SetBool(HashMove, false);
        monAnim.SetTrigger(HashDeath);

        // 이동 및 충돌체 비활성화
        navAgent.isStopped = true;
        navAgent.enabled = false;

        if(TryGetComponent<Collider>(out var col)) col.enabled = false;
    }

    protected void ChangeRoutine(IEnumerator newRoutine, MonsterState newState)
    {
        if(state == MonsterState.Die) return;

        state = newState;
        // 기존 실행 중인 코루틴 중단 후 새 코루틴 실행
        if(activeRoutine != null) StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(newRoutine);
    }

    // 순찰 상태 코루틴
    protected IEnumerator PatrolRoutine()
    {
        if (patrolRange == null) yield break;

        state = MonsterState.Patrol;
        navAgent.speed = stats.MoveSpeed;

        Bounds bounds = patrolRange.bounds;

        while(true)
        {
            // 플레이어 인지 시 추격 전환
            if(targetPlayer != null)
            {
                ChangeRoutine(ChaseRoutine(), MonsterState.Chase);
                yield break;
            }

            // 순찰 영역 내 무작위 좌표 산출
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            Vector3 movePos = new Vector3(randomX, transform.position.y, randomZ);

            // 해당 좌표가 이동 가능한 NavMesh 상에 존재하는지 확인
            if(NavMesh.SamplePosition(movePos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                SetAvoidancePriority(false);
                navAgent.isStopped = false;
                navAgent.SetDestination(hit.position);
                monAnim.SetBool(HashMove, true);
            }
            else
            {
                yield return null;
                continue;
            }

            // 목적지로 이동중 막힘 발생 시 새 좌표 검색
            float stuckTimer = 0f;
            while(true)
            {
                if (targetPlayer != null) break;

                if(!navAgent.pathPending)
                {
                    if(navAgent.pathStatus == NavMeshPathStatus.PathPartial ||
                        navAgent.pathStatus == NavMeshPathStatus.PathInvalid ||
                        navAgent.remainingDistance <= navAgent.stoppingDistance)
                    {
                        break;
                    }
                }

                // 이동 명령 중이지만 실제 속도가 거의 0인 경우
                if(navAgent.velocity.sqrMagnitude < 0.05f)
                {
                    stuckTimer += Time.deltaTime;
                    // 일정 시간 이상 막혀있으면 포기하고 새로운 순찰 목적지 검색
                    if (stuckTimer > 1.2f) break;
                }
                else
                {
                    // 정상 이동 중이면 타이머 리셋
                    stuckTimer = 0f;
                }
                yield return null;
            }

            SetAvoidancePriority(true);
            navAgent.isStopped = true;
            monAnim.SetBool(HashMove, false);

            // 목적지 도착 후 대기 타이머
            float timer = 0f;
            while(timer < waitTimer)
            {
                
                if (targetPlayer != null)
                {
                    ChangeRoutine(ChaseRoutine(), MonsterState.Chase);
                    yield break;
                }
                timer += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    // 추격 상태 코루틴
    protected IEnumerator ChaseRoutine()
    {
        state = MonsterState.Chase;
        navAgent.isStopped = false;
        navAgent.speed = stats.ChaseSpeed;
        monAnim.SetBool(HashMove, true);

        SetAvoidancePriority(false);

        while(true)
        {
            if(targetPlayer == null)
            {
                ChangeRoutine(PatrolRoutine(), MonsterState.Patrol);
                yield break;
            }

            // 공격 사거리 내 진입 확인
            float distance = Vector3.Distance(transform.position, targetPlayer.position);
            if(distance <= combat.AtkDistance)
            {
                ChangeRoutine(AttackRoutine(), MonsterState.Attack);
                yield break;
            }

            // 타깃 위치 지속 갱신
            navAgent.SetDestination(targetPlayer.position);
            yield return new WaitForSeconds(0.2f);
        }
    }

    // 공격 상태 코루틴
    protected IEnumerator AttackRoutine()
    {
        state = MonsterState.Attack;
        // 공격 전진감을 위해 선택적 적용
        monAnim.applyRootMotion = true;
        SetAvoidancePriority(true);
        navAgent.isStopped = true;

        // 공격 시작 시 플레이어를 정면으로 바라보도록 회전
        if (targetPlayer != null)
        {
            Vector3 lookDir = targetPlayer.position - transform.position;
            lookDir.y = 0f;
            if(lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        // 애니메이션 재생 및 종료 대기
        monAnim.SetTrigger(HashAttack);
        yield return new WaitUntil(() => monAnim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"));
        yield return new WaitUntil(() => !monAnim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"));

        monAnim.applyRootMotion = false;

        // 공격 후 상태 전환
        if(targetPlayer != null)
        {
            ChangeRoutine(ChaseRoutine(), MonsterState.Chase);
        }
        else
        {
            ChangeRoutine(PatrolRoutine(), MonsterState.Patrol);
        }
    }

    // 피격 상태 코루틴
    protected IEnumerator HitRoutine()
    {
        state = MonsterState.Hit;
        SetAvoidancePriority(true);
        navAgent.isStopped = true;
        monAnim.applyRootMotion = false;

        monAnim.ResetTrigger(HashAttack);
        monAnim.SetTrigger(HashHit);

        // 피격 애니메이션 재생 및 종료 대기
        yield return null;
        yield return new WaitUntil(() => monAnim.GetCurrentAnimatorStateInfo(0).IsTag("Hit"));
        yield return new WaitUntil(() => !monAnim.GetCurrentAnimatorStateInfo(0).IsTag("Hit"));

        // 피격 경직 종료 후 플레이어 재추격 또는 순찰 복귀
        if(targetPlayer != null)
        {
            ChangeRoutine(ChaseRoutine(), MonsterState.Chase);
        }
        else
        {
            ChangeRoutine(PatrolRoutine(), MonsterState.Patrol);
        }
    }
}
