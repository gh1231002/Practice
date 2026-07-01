using UnityEngine;

public class ResetRootMotionSMB : StateMachineBehaviour
{
    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(animator != null)
        {
            animator.applyRootMotion = false;
        }
    }
}
