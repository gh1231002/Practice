using UnityEngine;

public class Ui_Billboard : MonoBehaviour
{
    Transform TrsCam;

    private void Start()
    {
        if(Camera.main != null)
        {
            TrsCam = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (TrsCam == null) return;

        // 체력바가 항상 카메라의 바라보는 방향과 평행하도록 회전 고정
        transform.forward = TrsCam.forward;
    }
}
