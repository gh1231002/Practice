using UnityEngine;
using UnityEngine.AI;

public class Mon_Humanoid : MonsterBase
{
    //스크립트가 위치한 오브젝트에 있는 compernent
    //부모클래스 변수에 연결
    protected override void InitSettings()
    {
        State = MonsterState.Idle;
        NavAgent = GetComponent<NavMeshAgent>();
        MonAnim = GetComponent<Animator>();
        MonController = GetComponent<CharacterController>();
    }
}
