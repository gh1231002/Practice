using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GuardNPC : MonoBehaviour
{
    [Header("경비병 설정")]
    [SerializeField] List<Transform> WayPoints;
    [SerializeField] float NpcSpeed;
    [SerializeField] float MinWaitTimer;
    [SerializeField] float MaxWaitTimer;
    [SerializeField] int IdleIndex;

    NavMeshAgent GuardAgent;
    NavMeshObstacle GuardObstacle;
    Animator GuardAnim;
    bool isPatrol;

    private void Awake()
    {
        GuardAnim = GetComponent<Animator>();
        GuardAgent = GetComponent<NavMeshAgent>();
        GuardObstacle = GetComponent<NavMeshObstacle>();

        if (GuardAgent != null)
        {
            GuardAgent.speed = NpcSpeed;
            GuardAgent.enabled = false;
        }
        if (GuardObstacle != null) GuardObstacle.enabled = false;
    }
   
    void Start()
    {
        //우선순위 다르게 해서 서로 회피
        GuardAgent.avoidancePriority = Random.Range(50, 99);
        StartCoroutine(PatrolMove());
    }

    IEnumerator PatrolMove()
    {
        //순찰 포인트 순서대로 이동
        while(true)
        {
            GuardAgent.enabled = false;
            GuardObstacle.enabled = true;
            isPatrol = false;

            int RandomIndex = Random.Range(0, IdleIndex + 1);
            GuardAnim.SetInteger("IdleIndex", RandomIndex);
            float RandomWaitTime = Random.Range(MinWaitTimer, MaxWaitTimer);
            float Timer = 0f;

            while(Timer < RandomWaitTime)
            {
                Timer += Time.deltaTime;
                yield return null;
            }

            for(int WayPointsIndex = 0; WayPointsIndex < WayPoints.Count; WayPointsIndex++)
            {
                Vector3 MovePos = WayPoints[WayPointsIndex].position;
                if(NavMesh.SamplePosition(MovePos, out NavMeshHit Hit, 2.0f, NavMesh.AllAreas))
                {
                    GuardObstacle.enabled = false;
                    GuardAgent.enabled = true;
                    isPatrol = true;
                    GuardAgent.SetDestination(Hit.position);
                }
                else
                {
                    yield return null;
                    continue;
                }
                //1프레임대기 후 기다림
                yield return null;
                yield return new WaitUntil(() => !GuardAgent.pathPending
                                           && GuardAgent.remainingDistance <= GuardAgent.stoppingDistance);
            }
        }
    }

    private void Update()
    {
        GuardAnim.SetBool("isPatrol", isPatrol);
        SpeedMixMotion();
    }

    private void SpeedMixMotion()
    {
        //agent가 꺼져있거나 비어져있을때 접근시
        if(GuardAgent == null || !GuardAgent.enabled)
        {
            GuardAnim.SetFloat("Speed", 1.0f);
        }
        float CurSpeed = GuardAgent.velocity.magnitude;

        if (CurSpeed > 0.1f)
        {
            //현재 속력 / 최대 지정 속도 비율
            float SpeedRatio = CurSpeed / GuardAgent.speed;
            //너무 느려서 끊겨보이는걸 방지
            float finalAniSpeed = Mathf.Max(0.4f, SpeedRatio);
            GuardAnim.SetFloat("Speed", finalAniSpeed);
        }
        else
        {
            //멈췄을때는 기본배율
            GuardAnim.SetFloat("Speed", 1.0f);
        }
    }
}
