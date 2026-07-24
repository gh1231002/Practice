using UnityEngine;

public class RootMotionSMB : StateMachineBehaviour
{

    //애니메이션이 시작되는 첫 프레임에 호출
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(animator != null)
        {
            animator.applyRootMotion = true;
        }
    }
}
