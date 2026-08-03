using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    [SerializeField] GameObject InteractPanel;
    [SerializeField] GameObject DialogueUi;
    [SerializeField] GameObject MainUi;
    [SerializeField] GameObject ChoiceUi;
    [SerializeField] GameObject InfoPanel;
    [SerializeField] TextMeshProUGUI InteractText;
    [SerializeField] TextMeshProUGUI NpcNameText;
    [SerializeField] TextMeshProUGUI DialogueText;
    [SerializeField] TextMeshProUGUI NextText;
    [SerializeField] TextMeshProUGUI InfoText;
    [SerializeField] TextMeshProUGUI[] BtnText;
    [SerializeField] InputActionAsset InputActions;
    [SerializeField] Button[] BtnChoice;
    [SerializeField] InputActionProperty IapCursor;
    [SerializeField] InputActionProperty IapCharacterInfo;

    string InteractKey;
    string DeviceGroup;
    string[] DialoguesList;

    int DialogueIndex;

    bool isChoice;
    bool isHolding;
    bool isStatPanel;
    bool isCursorLock;

    Player_CC Player;
    QuestData CurrentQuest;
    CharacterInfoUI characterInfoUI;
    public event Action OffTalk;

    public static UiManager Instance;

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
        characterInfoUI = FindAnyObjectByType<CharacterInfoUI>(FindObjectsInactive.Include);

        InteractPanel.SetActive(false);
        DialogueUi.SetActive(false);
        ChoiceUi.SetActive(false);
        InfoPanel.SetActive(false);
        characterInfoUI.gameObject.SetActive(false);

        InputSystem.onActionChange += SaveDevice;
        Player.OnDialogue += StartDialoguePanel;
        IapCursor.action?.Enable();
        IapCharacterInfo.action?.Enable();

        //커서잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        DialogueUi.SetActive(true);
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

    public void NextDialogueText()
    {
        if (isChoice)
        {
            StartInfoPanel("선택지를 골라야 다음으로 넘어갈 수 있습니다.");
            return;
        }

        if (DialogueIndex >= DialoguesList.Length)
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
                isChoice = true;
                ShowChoices();
                ChoiceUi.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        DialogueIndex++;
    }

    private void ShowChoices()
    {
        for(int i = 0; i < CurrentQuest.choices.Count; i++)
        {
            //람다 클로저 이슈 방지용 변수 복사
            int index = i;
            //선택지 문구 반영
            BtnText[i].text = CurrentQuest.choices[i].ChoiceText;
            Button btn = BtnChoice[i];
            //기존 이벤트 제거 후 새로 클릭 이벤트 바인딩
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                isChoice = false;
                ChoiceUi.SetActive(false);
                RewardManager.instance.CraeteWeapon(index, CurrentQuest);
                NextDialogueText();
            });
        }
    }

    private void EndDialogue()
    {
        DialogueIndex = 0;
        DialoguesList = null;
        DialogueUi.SetActive(false);
        //대화캠 종료 후 플레이어 카메라로 전환
        DialogueCamManager.Instance.EnddialogueCam();
        MainUi.SetActive(true);
        InteractPanel.SetActive(true);
        OffTalk?.Invoke();
    }

    public void StartInfoPanel(string info)
    {
        InfoPanel.SetActive(true);
        InfoText.text = info;
        Invoke("EndInfoPanel", 0.5f);
    }

    public void EndInfoPanel()
    {
        InfoPanel.SetActive(false);
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

    public bool CurrentCursorState()
    {
        return isHolding;
    }

    private void Update()
    {
        OnOffCursor();
        OnOffCharacterInfo();
    }

    private void OnOffCursor()
    {
        //캐릭터 정보창이 On이라면 커서 On/Off 입력 작동안함
        if (isCursorLock == true) return;
        //키 입력 중인지 확인
        isHolding = IapCursor.action.IsPressed();

        //키 입력 중이라면
        if(isHolding)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnOffCharacterInfo()
    {
        if (IapCharacterInfo.action.WasPressedThisFrame() && isStatPanel == false)
        {
            characterInfoUI.OnPanel();
            //플레이어 카메라입력, 시네머신 카메라 입력 제한
            isHolding = true;
            isStatPanel = true;
            isCursorLock = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if(IapCharacterInfo.action.WasPressedThisFrame() && isStatPanel == true)
        {
            characterInfoUI.OffPanel();
            isHolding = false;
            isStatPanel = false;
            isCursorLock = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
