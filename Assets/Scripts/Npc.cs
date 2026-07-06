using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Npc : MonoBehaviour
{
    public enum NpcState
    {
        Idle, Patrol
    }

    public enum NpcType
    {
        Patrol,
        Dialogue,
        Idle,
    }
    [Header("NPC설정")]
    [SerializeField] float NpcSpeed;
    [SerializeField] float MinWatiTimer;
    [SerializeField] float MaxWatiTimer;
    [SerializeField] float PatrolRadius;
    [SerializeField] NpcType Type;
    [SerializeField]int IdleIndex;
    NpcState State;
    Animator NpcAnim;
    NavMeshAgent NpcAgent;
    Vector3 StartPos;
    bool isPatrol;

    void Awake()
    {
        NpcAnim = GetComponent<Animator>();
        NpcAgent = GetComponent<NavMeshAgent>();

        NpcAgent.speed = NpcSpeed;
        StartPos = transform.position;
    }

    private void Start()
    {
        State = NpcState.Idle;
        StartCoroutine(PatrolMove());
    }

    IEnumerator PatrolMove()
    {
        //돌아다니는 타입이아니면 종료
        if (Type != NpcType.Patrol) yield break;
        //초기에 서있는 위치를 기준으로 정해진반경에서 랜덤좌표값 얻고 이동
        while(true)
        {
            State = NpcState.Idle;
            NpcAgent.isStopped = true;
            isPatrol = false;
            //Range(int,int)는 마지막 최댓값이 제외이므로 1 더함
            int RandomIndex = Random.Range(0, IdleIndex + 1);
            NpcAnim.SetInteger("IdleIndex", RandomIndex);
            float RandomWaitTime = Random.Range(MinWatiTimer, MaxWatiTimer);
            float timer = 0f;

            while(timer < RandomWaitTime)
            {
                timer += Time.deltaTime;
                //waittime값만큼 대기
                yield return null;
            }

            State = NpcState.Patrol;
            Vector3 RandomPos = Random.insideUnitSphere * PatrolRadius;
            RandomPos += StartPos;

            if(NavMesh.SamplePosition(RandomPos, out NavMeshHit Hit, PatrolRadius, NavMesh.AllAreas))
            {
                NpcAgent.isStopped = false;
                isPatrol = true;
                NpcAgent.SetDestination(Hit.position);
            }
            else
            {
                yield return null;
            }
            yield return new WaitUntil(() => !NpcAgent.pathPending
                             && NpcAgent.remainingDistance <= NpcAgent.stoppingDistance);
        }
    }

    private void Update()
    {
        DoAni();
        SpeedMixMotion();
    }

    private void DoAni()
    {
        NpcAnim.SetBool("isPatrol", isPatrol);
    }
    private void SpeedMixMotion()
    {
        float CurSpeed = NpcAgent.velocity.magnitude;

        if(CurSpeed > 0.1f)
        {
            //현재 속력 / 최대 지정 속도 비율
            float SpeedRatio = CurSpeed / NpcAgent.speed;
            //너무 느려서 끊겨보이는걸 방지
            float finalAniSpeed = Mathf.Max(0.4f, SpeedRatio);
            NpcAnim.SetFloat("Speed", finalAniSpeed);
        }
        else
        {
            //멈췄을때는 기본배율
            NpcAnim.SetFloat("Speed", 1.0f);
        }
    }
}
