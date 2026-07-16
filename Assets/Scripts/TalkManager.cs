using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TalkManager : MonoBehaviour
{
    [SerializeField] GameObject InteractPanel;
    [SerializeField] GameObject DialoguePanel;
    [SerializeField] TextMeshProUGUI InteractText;
    [SerializeField] TextMeshProUGUI NpcNameText;
    [SerializeField] TextMeshProUGUI DialogueText;
    [SerializeField] TextMeshProUGUI NextText;
    [SerializeField] InputActionAsset InputActions;

    string InteractKey;
    string DeviceGroup;

    int DialogueIndex;

    Player_CC Player;

    public static TalkManager Instance;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        GameObject obj = GameObject.FindWithTag("Player");
        Player = obj.GetComponent<Player_CC>();

        InteractPanel.SetActive(false);
        DialoguePanel.SetActive(false);

        InputSystem.onActionChange += SaveDevice;
    }

    public void OnInteractPanel()
    {
        InteractPanel.SetActive(true);
        ShowInteractKey();
    }

    private void ShowInteractKey()
    {
        InteractKey = InputActions.FindActionMap("Player")
            .FindAction("Interact").GetBindingDisplayString(group: DeviceGroup);

        InteractText.text = $"[{InteractKey} 대화하기]";
    }

    public void OffInteractPanel()
    {
        InteractPanel.SetActive(false);
    }

    public void StartDialoguePanel(string Name, string[] Dialogue)
    {
        InteractPanel.SetActive(false);
        DialoguePanel.SetActive(true);
        SetDialogue(Name, Dialogue);
    }

    public void OffDialoguePanel()
    {
        DialoguePanel.SetActive(false);
    }

    private void SetDialogue(string Name, string[] Dialogue)
    {
        NpcNameText.text = Name;
        NextText.text = $"[{InteractKey}] 다음으로";
        DialogueIndex = 0;
        DisplayDialogueText(Dialogue);
    }

    private void DisplayDialogueText(string[] Dialogue)
    {
        DialogueText.text = Dialogue[DialogueIndex];
    }

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

                if (DeviceName.Contains("Keyboard") || DeviceName.Contains("Mouse"))
                {
                    DeviceGroup = "Keyboard&Mouse";
                }
                else if (DeviceName.Contains("Gamepad"))
                {
                    DeviceGroup = "Gamepad";
                }
            }
        }
    }
}
