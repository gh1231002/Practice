using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractNpc : MonoBehaviour
{
    [SerializeField] InteractSensor InSensor;
    [SerializeField] GameObject Interactpanel;
    [SerializeField] TextMeshProUGUI InteractText;
    [SerializeField] InputActionAsset inputActions;
    [SerializeField] float RotateSpeed;
    [SerializeField] string NpcName;

    string InteractKey;
    string DeviceGroup;
    Quaternion OriginRotation;
    Vector3 PlayerPos;
    bool isRestore;

    void Start()
    {
        Interactpanel.SetActive(false);
        InSensor.OnInteract += OnPanel;
        InSensor.OffInteract += OffPanel;
        InSensor.StayInteract += Rotation;
        InputSystem.onActionChange += SaveDevice;
        OriginRotation = transform.rotation;
    }

    private void OnPanel()
    {
        Interactpanel.SetActive(true);
        isRestore = false;
        InteractKey = inputActions.FindActionMap("Player")
                .FindAction("Interact").GetBindingDisplayString(group: DeviceGroup);

        InteractText.text = $"[{InteractKey}] 대화하기";
    }
    private void OffPanel(Collider other)
    {
        Interactpanel.SetActive(false);
        PlayerPos = other.transform.position;
        isRestore = true;
    }
    private void Rotation(Collider other)
    {
        Vector3 Dir = other.transform.position - transform.position;
        Dir.y = 0f;
        Quaternion TargetPos = Quaternion.LookRotation(Dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, TargetPos, RotateSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 최근 조작된 기기의 이름을 저장하는 함수
    /// </summary>
    /// <param name="Obj"></param>
    /// <param name="Change"></param>
    private void SaveDevice(object Obj, InputActionChange Change)
    {
        //버튼이 눌리거나 조작되는 순간인지
        if (Change == InputActionChange.ActionStarted || Change == InputActionChange.ActionPerformed)
        {
            var Action = Obj as InputAction;
            //존재한다면 장치를 분석
            if (Action != null && Action.activeControl != null)
            {
                var DeviceName = Action.activeControl.device.name;

                if(DeviceName.Contains("Keyboard") || DeviceName.Contains("Mouse"))
                {
                    DeviceGroup = "Keyboard&Mouse";
                }
                else if(DeviceName.Contains("Gamepad"))
                {
                    DeviceGroup = "Gamepad";
                }
            }
        }
    }

    private void Update()
    {
        if(isRestore)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, OriginRotation, RotateSpeed * Time.deltaTime);
        }
    }
}
