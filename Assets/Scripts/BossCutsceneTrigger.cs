using UnityEngine;

public class BossCutsceneTrigger : MonoBehaviour
{
    [SerializeField] BossCutsceneManager cutsceneManager;
    bool hasTriggered;

    
    private void OnTriggerExit(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            if (cutsceneManager != null)
            {
                cutsceneManager.StartBossCutscene();
            }
            // 트리거 콜라이더 비활성화 (중복 실행 방지)
            GetComponent<Collider>().enabled = false;
        }
    }
}
