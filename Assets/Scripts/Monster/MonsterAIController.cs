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
                navAgent.isStopped = false;
                navAgent.SetDestination(hit.position);
                monAnim.SetBool(HashMove, true);
            }
            else
            {
                yield return null;
                continue;
            }

            // 목적지 도착 대기 (길 찾기 완료 후 남은 거리 검사)
            yield return new WaitUntil(() => !navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance);
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
        navAgent.isStopped = true;
        monAnim.applyRootMotion = false;
        // 진행 중이던 공격 판정 취소
        combat.EndAttack();

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
