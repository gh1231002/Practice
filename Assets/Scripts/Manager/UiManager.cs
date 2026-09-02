using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    [SerializeField] GameObject InteractPanel;
    [SerializeField] GameObject DialogueUi;
    [SerializeField] GameObject ChoiceUi;
    [SerializeField] GameObject NoticePanel;
    [SerializeField] GameObject InfoPanel;
    [SerializeField] GameObject InventoryPanel;
    [SerializeField] GameObject ShopPanel;

    [SerializeField] TextMeshProUGUI InteractText;
    [SerializeField] TextMeshProUGUI NpcNameText;
    [SerializeField] TextMeshProUGUI DialogueText;
    [SerializeField] TextMeshProUGUI NextText;
    [SerializeField] TextMeshProUGUI InfoText;
    [SerializeField] TextMeshProUGUI[] BtnText;
    [SerializeField] TextMeshProUGUI InfoShortCutText;
    [SerializeField] TextMeshProUGUI InventoryShortCutText;
    [SerializeField] TextMeshProUGUI ShopShortCutText;

    [SerializeField] InputActionAsset InputActions;

    [SerializeField] Button[] BtnChoice;
    [SerializeField] Button BtnInfo;
    [SerializeField] Button BtnInventory;
    [SerializeField] Button BtnShop;

    [SerializeField] InputActionProperty IapCursor;
    [SerializeField] InputActionProperty IapCharacterInfo;
    [SerializeField] InputActionProperty IapInventory;
    [SerializeField] InputActionProperty IapShop;
    [SerializeField] InputActionProperty IapCancel;

    [SerializeField] CanvasGroup FadeCanvasGroup;

    string InteractKey;
    string InventoryKey;
    string ShopKey;
    string CharacterInfoKey;
    string DeviceGroup = "Keyboard&Mouse";
    string[] DialoguesList;

    int DialogueIndex;

    bool isChoice;
    bool isHolding;
    bool isStatPanel;
    bool isCursorLock;
    bool isInventory;
    bool isShop;

    Player_CC Player;
    QuestData CurrentQuest;
    CharacterInfoUI characterInfoUI;

    [SerializeField]List<GameObject> PanelList = new List<GameObject>();

    public event Action OffTalk;
    // true: UI 열림 (플레이어 조작 차단), false: UI 닫힘 (플레이어 조작 허용)
    public event Action<bool> OnUiStateChanged;

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
        GameObject obj = GameObject.FindWithTag("Player");
        Player = obj.GetComponent<Player_CC>();
        characterInfoUI = FindAnyObjectByType<CharacterInfoUI>(FindObjectsInactive.Include);

        InputSystem.onActionChange += SaveDevice;
        Player.OnDialogue += StartDialoguePanel;
    }

    private void OnDestroy()
    {
        InputSystem.onActionChange -= SaveDevice;
        if(Player != null) Player.OnDialogue -= StartDialoguePanel;
    }

    private void Start()
    {
        // UI 초기화
        InteractPanel.SetActive(false);
        DialogueUi.SetActive(false);
        ChoiceUi.SetActive(false);
        NoticePanel.SetActive(false);
        InfoPanel.SetActive(false);
        InventoryPanel.SetActive(false);
        ShopPanel.SetActive(false);
        FadeCanvasGroup.gameObject.SetActive(false);
        FadeCanvasGroup.alpha = 0f;

        // InputAction 활성화
        IapCursor.action?.Enable();
        IapCharacterInfo.action?.Enable();
        IapInventory.action?.Enable();
        IapCancel.action?.Enable();
        IapShop.action?.Enable();

        //커서잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        //게임 시작시 기본 디바이스 기준으로 전체 ui 숏컷 텍스트 1회 초기화
        UpdateAllShortcutText();
    }
    // 숏컷 및 디바이스 관리
    public void UpdateAllShortcutText()
    {
        ShowCharacterInfoKey();
        ShowInteractKey();
        ShowInventoryKey();
        ShowShopKey();
    }

    public void OnInteractPanel()
    {
        InteractPanel.SetActive(true);
    }

    private void ShowInteractKey()
    {
        InteractKey = InputActions.FindActionMap("Player")
            .FindAction("Interact").GetBindingDisplayString(group: DeviceGroup);

        InteractText.text = $"[{InteractKey} 대화하기]";
    }

    private void ShowCharacterInfoKey()
    {
        CharacterInfoKey = InputActions.FindActionMap("Player")
            .FindAction("CharacterInfo").GetBindingDisplayString (group: DeviceGroup);
        InfoShortCutText.text = $"[ {CharacterInfoKey} ]";
    }

    private void ShowInventoryKey()
    {
        InventoryKey = InputActions.FindActionMap("Player")
            .FindAction("Inventory").GetBindingDisplayString(group: DeviceGroup);
        InventoryShortCutText.text = $"[ {InventoryKey} ]";
    }

    private void ShowShopKey()
    {
        ShopKey = InputActions.FindActionMap("Player")
            .FindAction("Shop").GetBindingDisplayString(group: DeviceGroup);
        ShopShortCutText.text = $"[ {ShopKey} ]";
    }

    public void OffInteractPanel()
    {
        InteractPanel.SetActive(false);
    }

    public void StartDialoguePanel()
    {
        InteractPanel.SetActive(false);
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
            StartNoticePanel("선택지를 골라야 다음으로 넘어갈 수 있습니다.");
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
                isCursorLock = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        isCursorLock = false;
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
                RewardManager.instance.CreateWeapon(index, CurrentQuest);
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
        InteractPanel.SetActive(true);
        OffTalk?.Invoke();
    }

    public void StartNoticePanel(string info)
    {
        NoticePanel.SetActive(true);
        InfoText.text = info;
        Invoke("EndInfoPanel", 0.5f);
    }

    public void EndInfoPanel()
    {
        NoticePanel.SetActive(false);
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
                string newDeviceGroup = DeviceGroup;

                if (DeviceName.Contains("Keyboard") || DeviceName.Contains("Mouse"))
                {
                    DeviceGroup = "Keyboard&Mouse";
                }
                else if (DeviceName.Contains("Gamepad"))
                {
                    DeviceGroup = "Gamepad";
                }
                //장치 그룹이 실제로 변경외었을 때만 ui 전체 갱신
                if(DeviceGroup != newDeviceGroup)
                {
                    DeviceGroup = newDeviceGroup;
                    UpdateAllShortcutText();
                }
            }
        }
    }

    private void Update()
    {
        OnOffCursor();
        if(IapCharacterInfo.action.WasPressedThisFrame())
        {
            ToggleCharacterInfo();
        }
        if(IapInventory.action.WasPressedThisFrame())
        {
            ToggleInventory();
        }
        if(IapShop.action.WasPressedThisFrame())
        {
            ToggleShop();
        }
        if(IapCancel.action.WasPressedThisFrame())
        {
            ExitWindow();
        }
    }

    private void OnOffCursor()
    {
        //창이 열려있다면 커서 On/Off 입력 작동안함
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

    public void ToggleCharacterInfo()
    {
        isStatPanel = InfoPanel.activeSelf;
        //닫혀있다면 열기
        if (!isStatPanel)
        {
            InfoPanel.SetActive(true);
            PanelList.Add(InfoPanel);
            InfoPanel.transform.SetAsLastSibling();
            characterInfoUI.SettingStatPanel();
        }
        //열려있다면 닫기
        else
        {
            InfoPanel.SetActive(false);
            PanelList.Remove(InfoPanel);
        }
        RefreshCursorState();
    }
    public void ToggleInventory()
    {
        isInventory = InventoryPanel.activeSelf;
        if(!isInventory)
        {
            InventoryPanel.SetActive(true);
            PanelList.Add(InventoryPanel);
            InventoryPanel.transform.SetAsLastSibling();
        }
        else
        {
            InventoryPanel.SetActive(false);
            PanelList.Remove(InventoryPanel);
        }
        RefreshCursorState();
    }
    public void ToggleShop()
    {
        isShop = ShopPanel.activeSelf;

        if(!isShop)
        {
            ShopPanel.SetActive(true);
            PanelList.Add(ShopPanel);
            ShopPanel.transform.SetAsLastSibling();
        }
        else
        {
            ShopPanel.SetActive(false);
            PanelList.Remove(ShopPanel);
        }
        RefreshCursorState();
    }
    private void ExitWindow()
    {
        // List 목록에 등록된 창이 있는지 확인
        if(PanelList.Count > 0)
        {
            // 목록이 있다면 맨 마지막 창 하나만 닫고 List 목록에서 제거
            int lastIndex = PanelList.Count - 1;
            PanelList[lastIndex].gameObject.SetActive(false);
            PanelList.RemoveAt(lastIndex);
        }
        // 남아있는 창의 개수가 0개라면 마우스 커서를 잠그고 플레이어 조작모드로 전환
        if(PanelList.Count == 0)
        {
            RefreshCursorState();
        }
    }
    /// <summary>
    /// 열린 UI 패널 및 대화창 상태에 맞춰 마우스 커서 및 플레이어 입력 제어
    /// </summary>
    private void RefreshCursorState()
    {
        bool isDialogueActive = DialogueUi.activeSelf;
        // 열려있는 창이 있다면 커서 보이게, 0개라면 커서 숨기기
        bool hasOpenPanel = PanelList.Count > 0;
        isCursorLock = hasOpenPanel;
        isHolding = hasOpenPanel;

        Cursor.lockState = hasOpenPanel ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = hasOpenPanel;

        // 플레이어 조작 차단/허용 이벤트 발생
        OnUiStateChanged?.Invoke(hasOpenPanel);
    }

    public bool CurrentCursorState()
    {
        return isHolding || isCursorLock;
    }
    /// <summary>
    /// 화면을 먼저 어둡게 만든 뒤 LoadingSceneManager를 호출합니다.
    /// </summary>
    /// <param name="nextSceneName"></param>
    /// <param name="pos"></param>
    /// <param name="fadeDuration"></param>
    /// <returns></returns>
    public IEnumerator FadeOutAndLoad(string nextSceneName, Vector3 pos, float fadeDuration = 0.3f)
    {
        if(FadeCanvasGroup != null)
        {
            FadeCanvasGroup.gameObject.SetActive(true);
            float timer = 0f;

            // 화면이 부드럽게 어두워짐 (씬 이동시 멈춤 현상을 가려줌)
            while(timer < fadeDuration)
            {
                timer += Time.deltaTime;
                FadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            FadeCanvasGroup.alpha = 1f;
        }

        // 화면이 완전히 암전되면 로딩 씬 호출
        LoadingSceneManager.LoadScene(nextSceneName, pos);
    }

    public void ResetFade()
    {
        if (FadeCanvasGroup != null)
        {
            FadeCanvasGroup.alpha = 0f;
            FadeCanvasGroup.gameObject.SetActive(false);
        }
    }
}
