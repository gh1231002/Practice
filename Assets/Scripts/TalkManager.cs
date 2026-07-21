using System;
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
    string[] DialoguesList;

    int DialogueIndex;

    Player_CC Player;

    public event Action OffTalk;

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

    public void StartDialoguePanel(DialogueData data)
    {
        InteractPanel.SetActive(false);
        DialoguePanel.SetActive(true);
        SetDialogue(data.npcname, data.dialogues);
    }

    public void OffDialoguePanel()
    {
        DialoguePanel.SetActive(false);
    }

    private void SetDialogue(string Name, string[] Dialogue)
    {
        NpcNameText.text = Name;
        DialoguesList = Dialogue;
        NextText.text = $"[{InteractKey}] 다음으로";
        DialogueIndex = 0;
        NextDialogueText();
    }

    public void NextDialogueText()
    {
        if(DialogueIndex >= DialoguesList.Length)
        {
            EndDialogue();
            return;
        }
        DialogueText.text = DialoguesList[DialogueIndex];
        DialogueIndex++;
    }

    private void EndDialogue()
    {
        DialogueIndex = 0;
        DialoguesList = null;
        DialoguePanel.SetActive(false);
        OffTalk?.Invoke();
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
