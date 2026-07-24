using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TalkManager : MonoBehaviour
{
    [SerializeField] GameObject InteractPanel;
    [SerializeField] GameObject DialoguePanel;
    [SerializeField] GameObject MainUi;
    [SerializeField] GameObject ChoiceUi;
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
    QuestData CurrentQuest;

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
        ChoiceUi.SetActive(false);

        InputSystem.onActionChange += SaveDevice;
        Player.OnDialogue += StartDialoguePanel;
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

    public void StartDialoguePanel()
    {
        InteractPanel.SetActive(false);
        MainUi.SetActive(false);
        DialoguePanel.SetActive(true);
    }

    public void StartDialogue(string name, string[] dialogue, QuestData quest)
    {
        CurrentQuest = quest;
        NpcNameText.text = name;
        DialoguesList = dialogue;
        NextText.text = $"[{InteractKey}] 다음으로";
        DialogueIndex = 0;
        NextDialogueText();
    }
    //private void SetDialogue(InteractNpc npc)
    //{
    //    //NpcNameText.text = Name;
    //    //DialoguesList = Dialogue;
    //    NextText.text = $"[{InteractKey}] 다음으로";
    //    DialogueIndex = 0;
    //    //NextDialogueText();
    //}

    public void NextDialogueText()
    {
        if(DialogueIndex >= DialoguesList.Length)
        {
            EndDialogue();
            return;
        }
        DialogueText.text = DialoguesList[DialogueIndex];
        //만약 선택지가 있는 퀘스트인지 확인
        if(CurrentQuest != null && CurrentQuest.ischoicedialogue == true)
        {
            //선택지로 설정한 대화와 현재 대화가 같은지 확인
            if(CurrentQuest.choiscedialogueindex == DialogueIndex)
            {
                OnChoiceUi();
            }
            //일단 임시로 같지 않다면 ui off
            if(CurrentQuest.choiscedialogueindex != DialogueIndex)
            {
                OffChoiceUi();
            }
        }
        DialogueIndex++;
    }

    private void OnChoiceUi()
    {
        ChoiceUi.SetActive(true);
    }
    private void OffChoiceUi()
    {
        ChoiceUi.SetActive(false);
    }

    private void EndDialogue()
    {
        DialogueIndex = 0;
        DialoguesList = null;
        DialoguePanel.SetActive(false);
        //대화캠 종료 후 플레이어 카메라로 전환
        DialogueCamManager.Instance.EnddialogueCam();
        MainUi.SetActive(true);
        InteractPanel.SetActive(true);
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
