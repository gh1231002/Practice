using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public abstract class MonsterBase : MonoBehaviour, ITakeDamage
{
    public enum MonsterType
    {
        Humanoid, Orc, Skeleton,
    }

    public enum MonsterState
    {
        Idle, Patrol, Chase, Attack, Die, Hit
    }
    
    [Header("공격 설정")]
    [SerializeField] float AtkPower;
    [SerializeField] float MaxDetextDuration;
    [SerializeField] Transform AtkPoint;
    [SerializeField] float AtkDistance;
    [SerializeField] Vector3 AtkHalfBox;
    [SerializeField] LayerMask TargetLayer;
    float AtkTimer;
    bool CheckAtk;
    List<ITakeDamage> HitTargetList = new List<ITakeDamage>();
    [Header("설정")]
    [SerializeField] float MaxHp;
    [SerializeField] float CurHp;
    [SerializeField] float Speed;
    [SerializeField] float ChaseSpeed;
    [SerializeField] float WaitTimer;
    [SerializeField] Collider PatrolRange;
    [SerializeField] MonsterType Type;
    [SerializeField] MonsterSensor MonSensor;
    [SerializeField] MonsterSensor PatrolSensor;

    [SerializeField]protected MonsterState State;
    protected CharacterController MonController;
    protected NavMeshAgent NavAgent;
    protected Animator MonAnim;
    protected SphereCollider ChaseRange;
    protected Transform TrsPlayer;

    Coroutine MonCoroutin;
    bool CheckPlayer;
    bool CheckPatrol;
    bool ChasePlayer;

    private void Awake()
    {
        InitSettings();
        CurHp = MaxHp;
        NavAgent.speed = Speed;
        MonAnim.applyRootMotion = false;
    }

    protected virtual void InitSettings() { }

    private void Start()
    {
        InitStart();

        if(PatrolRange != null)
        {
            ChangeRoutine(PatrolMove(PatrolRange), MonsterState.Patrol);
        }
        if(MonSensor != null)
        {
            //신호를 보낼때 실행할 함수들을 등록(구독)
            MonSensor.OnPlayerDetected += OnDetectPlayer;
            MonSensor.OnPlayerLost += OnLostPlayer;
        }
        if(PatrolSensor != null)
        {
            PatrolSensor.OnPlayerLost += OnLostPatrol;
        }
    }

    private void OnDetectPlayer(Collider collider)
    {
        if (State == MonsterState.Die) return;
        CheckPlayer = true;
        //좌표가 아닌 오브젝트 주소를 통째로 기억
        TrsPlayer = collider.transform;
        ChangeRoutine(Chase(), MonsterState.Chase);
    }

    private void OnLostPlayer(Collider collider)
    {
        //플레이어가 인식범위를 벗어날때
        CheckPlayer = false;
        TrsPlayer = null;
    }

    private void OnLostPatrol(Collider collider)
    {
        if(State == MonsterState.Die) return;
        //순찰영역을 벗어날때
        CheckPlayer = false;
        ChasePlayer = false;
        TrsPlayer = null;
        ChangeRoutine(PatrolMove(PatrolRange), MonsterState.Patrol);
    }

    protected virtual void InitStart() { }

    private void Update()
    {
        if (State == MonsterState.Die) return;
        CheckAni();
        OverLapBoxCheck();
        UpdateLogic();
    }

    private void CheckAni()
    {
        MonAnim.SetBool("CheckPatrol", CheckPatrol);
        MonAnim.SetBool("Chase", ChasePlayer);
    }

    protected virtual void UpdateLogic() { }

    protected IEnumerator PatrolMove(Collider collider)
    {
        if (CheckPlayer == true) yield break;

        State = MonsterState.Patrol;
        //이동 범위 콜라이더를 가져와서 값을전달
        Bounds bounds = collider.bounds;

        //일정 범위 안에서 랜덤좌표 구하고 이동한뒤 도착하면 다시 이동
        while (true)
        {
            //좌표로 이동중 범위 내에 플레이어 인식할시
            //이동 중지, 추격코루틴으로 전환, 이동 코루틴 정지
            if(CheckPlayer == true)
            {
                CheckPatrol = false;
                ChangeRoutine(Chase(), MonsterState.Chase);
                yield break;
            }
            float RandomX = Random.Range(bounds.min.x, bounds.max.x);
            float RandomZ = Random.Range(bounds.min.z, bounds.max.z);

            Vector3 MovePos = new Vector3(RandomX, transform.position.y, RandomZ);

            //좌표가 유효한 navmesh 영역 안인지 검사
            if(NavMesh.SamplePosition(MovePos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                NavAgent.isStopped = false;
                CheckPatrol = true;
                //좌표로 이동
                NavAgent.SetDestination(hit.position);
            }
            else
            {
                //유요한 좌표가아니라면 다음프레임에 다시시도
                yield return null;
                continue;
            }
            //도착할때까지 대기
            //pathPending: 경로 계산 중인지 확인, remainingDistance: 남은 거리
            yield return new WaitUntil(() => !NavAgent.pathPending && NavAgent.remainingDistance <= NavAgent.stoppingDistance);
            NavAgent.isStopped = true;
            CheckPatrol = false;

            //도착후 대기 도중 플레이어 발견시 추격 코루틴 전환
            float timer = 0f;
            while(timer < WaitTimer)
            {
                if(CheckPlayer == true)
                {
                    CheckPatrol = false;
                    ChangeRoutine(Chase(), MonsterState.Chase);
                    yield break;
                }
                timer += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }


    protected IEnumerator Chase()
    {
        State = MonsterState.Chase;
        //대기중 코루틴 전환될때 이동을위함
        NavAgent.isStopped = false;
        //좌표로 이동중 적을 발견해서 들어왔을때 애니메이션 전환
        CheckPatrol = false;
        ChasePlayer = true;
        
        while(true)
        {
            if (TrsPlayer == null)//플레이어를 놓치면 탈출
            {
                State = MonsterState.Patrol;
                NavAgent.speed = Speed;
                ChasePlayer = false;
                ChangeRoutine(PatrolMove(PatrolRange), MonsterState.Patrol);
                yield break;
            }
            //플레이어와의 거리 계산
            float DistanceToPlayer = Vector3.Distance(transform.position, TrsPlayer.position);
            
            //만약 플레이어와 거리가 일정범위에 들어온다면
            if(DistanceToPlayer <= AtkDistance)
            {
                //공격 상태로 변경하고 탈출
                State = MonsterState.Attack;
                ChangeRoutine(Attack(), MonsterState.Attack);
                yield break;
            }
            //플레이어 위치를 계속 받으면서 이동
            NavAgent.speed = ChaseSpeed;
            NavAgent.SetDestination(TrsPlayer.position);
            //수많은 연산을 막기위해 제한을 걸어줌
            yield return new WaitForSeconds(0.2f);
        }
    }

    protected IEnumerator Attack()
    {
        //공격범위에 들어오면 공격 애니메이션 실행
        State = MonsterState.Attack;
        MonAnim.applyRootMotion = true;
        NavAgent.isStopped = true;
        //공격 애니메이션이 재생되기 전 플레이어를 바라보게 회전
        if(TrsPlayer != null)
        {
            Vector3 LookDir = TrsPlayer.position - transform.position;
            LookDir.y = 0f;
            //방향 벡터가 유효할때만 회전적용
            if(LookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(LookDir);
            }
        }
        MonAnim.SetTrigger("Attack");
        yield return new WaitUntil(() => MonAnim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"));
        yield return new WaitUntil(() => !MonAnim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"));

        //공격이 끝난 후 플레이어가 아직 있다면 다시 추격
        if(TrsPlayer != null)
        {
            MonAnim.applyRootMotion = false;
            ChangeRoutine(Chase(), MonsterState.Chase);
        }
        else//플레이어가 없다면
        {
            MonAnim.applyRootMotion = false;
            NavAgent.speed = Speed;
            ChasePlayer = false;
            ChangeRoutine(PatrolMove(PatrolRange), MonsterState.Patrol);
        }
    }
    //공격 애니메이션 진행 중 타격시점에만 실행
    private void StartAttack()
    {
        CheckAtk = true;
        AtkTimer = 0f;
        HitTargetList.Clear();
    }
    /// <summary>
    /// 공격받은 콜라이더 체크함수
    /// </summary>
    private void OverLapBoxCheck()
    {
        if (AtkPoint == null || CheckAtk == false) return;
        //타이머 누적 및 자동종료 체크
        AtkTimer += Time.deltaTime;
        if(AtkTimer >= MaxDetextDuration)
        {
            CheckAtk = false;
            return;
        }
        //AtkPoint에 닿은 콜라이더들 검출
        Collider[] HitTargets = Physics.OverlapBox(AtkPoint.position, AtkHalfBox,
                               AtkPoint.rotation, TargetLayer);
        //데이터 전달
        foreach (Collider Target in HitTargets)
        {
            // 아까 만든 IDamageable 인터페이스가 타겟에 붙어있는지 확인
            if (Target.TryGetComponent<ITakeDamage>(out var Damage))
            {
                //공격중 이미 한번맞은 대상이면 스킵
                if (HitTargetList.Contains(Damage)) continue;
                //처음맞는 대상이라면 데미지 입히고 리스트 등록
                Damage.TakeDamage(this.gameObject, AtkPower);
                HitTargetList.Add(Damage);
                break;
            }
        }
    }

    /// <summary>
    /// 상태와 코루틴 변경을 위한 함수
    /// </summary>
    protected void ChangeRoutine(IEnumerator NewRoutine, MonsterState NewState)
    {
        //죽었다면 행동교체 차단
        if (State == MonsterState.Die) return;

        State = NewState;

        if(MonCoroutin != null)
        {
            StopCoroutine(MonCoroutin);
        }

        MonCoroutin = StartCoroutine(NewRoutine);
    }

    private void OnDrawGizmosSelected()
    {
        if (AtkPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.matrix = AtkPoint.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, AtkHalfBox * 2f);
    }

    public virtual void TakeDamage(GameObject Attacker, float Damage)
    {
        if (State == MonsterState.Die) return;

        CurHp -= Damage;

        if (CurHp <= 0f)
        {
            State = MonsterState.Die;
            StopAllCoroutines();
            MonAnim.ResetTrigger("Hit");
            MonAnim.ResetTrigger("Attack");
            MonAnim.SetTrigger("Death");
            NavAgent.isStopped = true;
            NavAgent.enabled = false;

            if (TryGetComponent<Collider>(out var col)) col.enabled = false;
            if (TryGetComponent<CharacterController>(out var cc)) cc.enabled = false;
            return;
        }
        ChangeRoutine(HitRoutine(), MonsterState.Hit);
    }

    protected IEnumerator HitRoutine()
    {
        State = MonsterState.Hit;
        NavAgent.isStopped = true;//맞는동안 정지
        CheckAtk = false;//공격 중 맞았다면 공격판정상자도 OFF
        MonAnim.applyRootMotion = false;

        MonAnim.ResetTrigger("Attack");
        //피격애니메이션으로 강제전환
        MonAnim.CrossFadeInFixedTime("Shield Hit", 0.1f);
        yield return null;
        //hit으로 진입할때까지 기다림
        yield return new WaitUntil(() => MonAnim.GetCurrentAnimatorStateInfo(0).IsTag("Hit"));
        //hit이 끝날때까지 기다림
        yield return new WaitUntil(() => !MonAnim.GetCurrentAnimatorStateInfo(0).IsTag("Hit"));

        //애니메이션 끝난 후 플레이어가 있는지 없는지 체크 후 맞는 코루틴 시작
        if(TrsPlayer != null)
        {
            ChangeRoutine(Chase(),MonsterState.Chase);
        }
        else
        {
            ChangeRoutine(PatrolMove(PatrolRange), MonsterState.Patrol);
        }
    }
}
