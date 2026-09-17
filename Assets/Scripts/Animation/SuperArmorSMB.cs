using Unity.VisualScripting;
using UnityEngine;

public class SuperArmorSMB : StateMachineBehaviour
{
    // 애니메이션 노드 진입 시 실행
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(animator.TryGetComponent<Player_CC>(out var player))
        {
            player.SetSuperArmor(true);
        }
    }

    // 애니메이션 노드가 끝나거나 캔슬되어 나갈 때 실행
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.TryGetComponent<Player_CC>(out var player))
        {
            player.SetSuperArmor(false);
        }
    }
}
