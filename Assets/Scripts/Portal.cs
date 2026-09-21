using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Portal Setting")]
    [SerializeField] TargetScene targetScene;
    [SerializeField] Vector3 targetPos;
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            // 포탈에 닿으면 플레이어 입력 차단
            other.TryGetComponent<Player_CC>(out var player);
            player.SetInputState(false);

            switch (targetScene)
            {
                case TargetScene.FirstVillage:
                    StartCoroutine(UiManager.Instance.FadeOutAndLoad("First village", targetPos));
                    break;
                case TargetScene.Grave:
                    StartCoroutine(UiManager.Instance.FadeOutAndLoad("Grave", targetPos));
                    break;
            }
        }
    }
}
