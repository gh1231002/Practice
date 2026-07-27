using Unity.Cinemachine;
using UnityEngine;

public class CinemachineInputController : MonoBehaviour
{
    CinemachineInputAxisController InputAxisController;
    bool isInputState;

    private void Awake()
    {
        InputAxisController = GetComponent<CinemachineInputAxisController>();
    }

    private void Update()
    {
        if(UiManager.Instance == null || InputAxisController == null) return;
        //ui매니저의 커서 상태를 확인하여 input on/off 조절
        isInputState = UiManager.Instance.CurrentCursorState();
        //커서 on이라면 입력 비활성화
        InputAxisController.enabled = !isInputState;
    }
}
