using UnityEngine;

public class Portal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            // 텔레포트 창 UI on
            UiManager.Instance.ToggleTeleportPanel(true);
        }
    }
}
