using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Text.RegularExpressions;


public class TitleScreenController : MonoBehaviour
{
    [Header("초기 화면 UI")]
    [SerializeField] CanvasGroup TitleScreenCanvasGroup;
    
    [Header("메인 메뉴 화면 UI")]
    [SerializeField] CanvasGroup mainMenuScreenCanvasGroup;

    [Header("화면 전환 설정")]
    [SerializeField] float fadeOutDuration = 0.5f;
    [SerializeField] float fadeInDuration = 0.7f;

    [Header("메뉴 화면 버튼들")]
    [SerializeField] Button newStartButton;
    [SerializeField] Button LoadButton;
    [SerializeField] TextMeshProUGUI loadButtonText;
    [SerializeField, Range(0f, 1f)] float disabledButtonTextAlpha = 0.35f;

    [Header("닉네임 입력 패널")]
    [SerializeField] GameObject nicknamePanel;
    [SerializeField] TMP_InputField nicknameInputField;
    [SerializeField] TextMeshProUGUI nicknameMessageText;
    [SerializeField] TextMeshProUGUI nicknameLimitText;
    [SerializeField] Button nicknameConfirmButton;
    [SerializeField] Button nicknameCancelButton;
    [Header("닉네임 제한 설정")]
    [SerializeField, Min(1)] int maxNicknameLength = 12;

    [Header("저장 데이터 선택 패널")]
    [SerializeField] GameObject saveSelectPanel;
    [SerializeField] Transform saveSlotContent;
    [SerializeField] SaveSlotItemUI saveSlotItemPrefab;
    [SerializeField] Button saveSelectConfirmButton;
    [SerializeField] Button saveSelectCancelButton;
    [SerializeField] TextMeshProUGUI saveSelectMessageText;

    [Header("ESC 처리 연동")]
    [SerializeField] SettingsController settingsController;
    [SerializeField] ExitConfirmPanelController exitConfirmPanelController;

    // 최초 화면 상태에서 메뉴화면으로 전환했는지 확인
    bool hasEnterMenu;
    // 화면 전환 코루틴 실행 중 입력 차단
    bool isTransitioning;
    // 닉네임 입력 패널이 열려 있는지 확인
    bool isNicknamePanelOpen;
    // 저장 데이터 선택 패널이 열려 있는지 확인
    bool isSaveSelectPanelOpen;
    // 새 게임을 위해 기존 저장 슬롯을 교체하는 중인지 확인
    bool isReplaceSaveMode;

    string pendingNewSaveName;
    string saveSelectConfirmDefaultText;
    string saveSelectCancelDefaultText;
    string saveSelectMessageDefaultText;

    readonly List<SaveSlotItemUI> spawnedSaveSlots = new List<SaveSlotItemUI>();
    SaveSlotItemUI selectedSaveSlot;


    void Start()
    {
        // CanvasGroup 알파값 설정
        TitleScreenCanvasGroup.alpha = 1f;
        mainMenuScreenCanvasGroup.alpha = 0f;
        mainMenuScreenCanvasGroup.interactable = false;
        mainMenuScreenCanvasGroup.blocksRaycasts = false;

        // 인게임에서 복귀했을 때도 마우스 커서가 보이도록 초기화
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        nicknamePanel.SetActive(false);
        isNicknamePanelOpen = false;
        if (saveSelectPanel != null)
        {
            saveSelectPanel.SetActive(false);
        }
        isSaveSelectPanelOpen = false;
        saveSelectConfirmDefaultText = GetButtonText(saveSelectConfirmButton);
        saveSelectCancelDefaultText = GetButtonText(saveSelectCancelButton);
        saveSelectMessageDefaultText = saveSelectMessageText != null
            ? saveSelectMessageText.text
            : string.Empty;

        // 닉네임 입력 필드의 최대 글자 수 적용
        nicknameInputField.characterLimit = maxNicknameLength;
        // 초기 텍스트 작업
        nicknameLimitText.text = $"최대 {nicknameInputField.characterLimit}자 까지";
        
        // 버튼 이벤트 연결
        newStartButton.onClick.AddListener(OpenNicknamePanel);
        if (LoadButton != null)
        {
            LoadButton.onClick.AddListener(OpenSaveSelectPanel);
        }
        if (saveSelectConfirmButton != null)
        {
            saveSelectConfirmButton.onClick.AddListener(ConfirmSelectedSave);
        }
        if (saveSelectCancelButton != null)
        {
            saveSelectCancelButton.onClick.AddListener(CloseSaveSelectPanel);
        }
        nicknameConfirmButton.onClick.AddListener(ConfirmNickname);
        nicknameCancelButton.onClick.AddListener(CloseNicknamePanel);

        UpdateLoadButtonState();
    }

    void Update()
    {
        // 화면 전환 중에는 모든 입력 차단
        if(isTransitioning) return;

        // ESC 입력을 anyKey보다 먼저 처리 (충돌 방지)
        if(Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapeKey();
            return;
        }

        // 패널이 열려 있으면 아무 키/클릭 메뉴 진입 차단
        if((exitConfirmPanelController != null && exitConfirmPanelController.IsOpen) ||
           (settingsController != null && settingsController.IsOpen) ||
           isNicknamePanelOpen ||
           isSaveSelectPanelOpen)
        {
            return;
        }

        // 이미 메뉴 화면으로 전환했다면 이하 처리하지 않음
        if(hasEnterMenu) return;

        // 키보드 아무 키 입력 또는 마우스 클릭 감지
        if(Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame ||
            Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            hasEnterMenu = true;
            StartCoroutine(TransitionToMenu());
        }
    }

    IEnumerator TransitionToMenu()
    {
        isTransitioning = true;

        // 초기 화면 페이드 아웃
        yield return StartCoroutine(FadeCanvasGroup(TitleScreenCanvasGroup, 1f, 0f, fadeOutDuration));

        // 메뉴 화면 페이드 인
        yield return StartCoroutine(FadeCanvasGroup(mainMenuScreenCanvasGroup, 0f, 1f, fadeInDuration));

        mainMenuScreenCanvasGroup.interactable = true;
        mainMenuScreenCanvasGroup.blocksRaycasts = true;
        isTransitioning = false;
    }

    /// <summary>
    /// ESC 입력 우선순위에 따라 타이틀 씬 UI 상태를 제어한다.
    /// 닉네임 입력 패널 닫기
    /// 종료 확인 패널 닫기
    /// 설정 패널 닫기
    /// 메인 메뉴 → 초기 타이틀 화면 복귀
    /// 초기 타이틀 화면 → 종료 확인 패널 열기
    /// </summary>
    private void HandleEscapeKey()
    {
        if(isNicknamePanelOpen)
        {
            CloseNicknamePanel();
            return;
        }
        if(isSaveSelectPanelOpen)
        {
            CloseSaveSelectPanel();
            return;
        }
        if(exitConfirmPanelController != null && exitConfirmPanelController.IsOpen)
        {
            exitConfirmPanelController.CloseExitConfirmPanel();
            return;
        }

        if(settingsController != null && settingsController.IsOpen)
        {
            settingsController.OffSettingsPanel();
            return;
        }

        if(hasEnterMenu)
        {
            StartCoroutine(ReturnToTitleScreen());
            return;
        }

        if(exitConfirmPanelController != null)
        {
            exitConfirmPanelController.OpenExitConfirmPanel();
        }
    }

    /// <summary>
    /// 메인 메뉴 화면에서 초기 타이틀 화면으로 역방향 전환한다.
    /// </summary>
    private IEnumerator ReturnToTitleScreen()
    {
        isTransitioning = true;
        hasEnterMenu = false;
        mainMenuScreenCanvasGroup.interactable = false;
        mainMenuScreenCanvasGroup.blocksRaycasts = false;

        // 메인 메뉴 화면 페이드 아웃
        yield return StartCoroutine(FadeCanvasGroup(mainMenuScreenCanvasGroup, 1f, 0f, fadeOutDuration));

        // 초기 타이틀 화면 페이드 인
        yield return StartCoroutine(FadeCanvasGroup(TitleScreenCanvasGroup, 0f, 1f, fadeInDuration));

        isTransitioning = false;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float targetAlpha, float duration)
    {
        // 페이드 시작값 설정
        canvasGroup.alpha = startAlpha;

        // 전환 시간이 0 이하라며 즉시 변경
        if(duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsedTime = 0f;

        // 지정한 시간 동안 Alpha를 부드럽게 변경
        while(elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
             
            // 부드러운 페이드 효과 적용
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null; // 다음 프레임까지 대기
        }

        // 마지막 프레임의 Alpha를 정확하게 설정
        canvasGroup.alpha = targetAlpha;
    }

    /// <summary>
    /// 저장 데이터 유무에 따라 이어하기 버튼의 활성화 상태와 텍스트 투명도를 갱신한다.
    /// </summary>
    private void UpdateLoadButtonState()
    {
        if (LoadButton == null) return;

        bool hasSaveData = SaveManager.Instance != null && SaveManager.Instance.HasAnySaveData();
        LoadButton.interactable = hasSaveData;

        TextMeshProUGUI targetText = loadButtonText != null
            ? loadButtonText
            : LoadButton.GetComponentInChildren<TextMeshProUGUI>();

        if (targetText != null)
        {
            Color textColor = targetText.color;
            textColor.a = hasSaveData ? 1f : disabledButtonTextAlpha;
            targetText.color = textColor;
        }
    }

    /// <summary>
    /// 닉네임 입력 패널 열기
    /// </summary>
    private void OpenNicknamePanel()
    {
        if(isNicknamePanelOpen || isSaveSelectPanelOpen || isTransitioning) return;

        pendingNewSaveName = string.Empty;
        isReplaceSaveMode = false;
        nicknamePanel.SetActive(true);
        isNicknamePanelOpen = true;

        // 이전 입력값 초기화
        nicknameInputField.text = string.Empty;
        // 이전 안내문구 초기화
        nicknameMessageText.text = "영어와 숫자만 입력할 수 있습니다.";
        // 닉네임 입력 필드에 포커스
        nicknameInputField.Select();
        nicknameInputField.ActivateInputField();
    }

    /// <summary>
    /// 닉네임 입력 패널 닫기
    /// </summary>
    private void CloseNicknamePanel()
    {
        nicknamePanel.SetActive(false);
        isNicknamePanelOpen = false;
        if (!isReplaceSaveMode)
        {
            pendingNewSaveName = string.Empty;
        }
    }

    /// <summary>
    /// 닉네임 입력 확인 및 신규 저장 데이터 생성
    /// </summary>
    private void ConfirmNickname()
    {
        // 입력된 닉네임의 앞뒤 공백 제거
        string playerName = nicknameInputField.text.Trim();

        if(string.IsNullOrWhiteSpace(playerName))
        {
            nicknameMessageText.text = "닉네임을 입력해주세요.";
            nicknameInputField.ActivateInputField();
            return;
        }

        // 영어 알파벳과 숫자만 허용
        if (!Regex.IsMatch(playerName, @"^[a-zA-Z0-9]+$"))
        {
            nicknameMessageText.text = "영어와 숫자만 입력할 수 있습니다.";
            nicknameInputField.ActivateInputField();
            return;
        }

        // InputField에 설정된 글자 수 제한을 기준으로 검사
        if(nicknameInputField.characterLimit > 0 &&
            playerName.Length > nicknameInputField.characterLimit)
        {
            nicknameMessageText.text = "닉네임 길이를 확인해주세요.";
            nicknameInputField.ActivateInputField();
            return;
        }

        // SaveManger 존재 여부 확인
        if(SaveManager.Instance == null)
        {
            nicknameMessageText.text = "저장 시스템을 찾을 수 없습니다.";
            Debug.Log("SaveManger Instance가 존재하지 않습니다.");
            return;
        }

        if (SaveManager.Instance.CanCreateNewSave())
        {
            if (SaveManager.Instance.IsPlayerNameDuplicate(playerName))
            {
                nicknameMessageText.text = "이미 사용 중인 닉네임입니다.";
                nicknameInputField.ActivateInputField();
                return;
            }

            CreateNewGameSave(playerName);
            return;
        }

        pendingNewSaveName = playerName;
        isReplaceSaveMode = true;
        nicknamePanel.SetActive(false);
        isNicknamePanelOpen = false;
        SetSaveSelectMode(true);
        OpenSaveSelectPanel();

        if (!isSaveSelectPanelOpen)
        {
            isReplaceSaveMode = false;
            SetSaveSelectMode(false);
            RestoreNicknamePanel("저장 슬롯이 가득 찼습니다. 교체할 저장 데이터를 선택해주세요.");
        }
    }

    private void CreateNewGameSave(string playerName)
    {
        bool saveResult = SaveManager.Instance.CreateNewSave(playerName, SceneName.Town.ToString());
        if(!saveResult)
        {
            nicknameMessageText.text = "저장 데이터 생성에 실패했습니다.";
            nicknameInputField.ActivateInputField();
            return;
        }

        // 저장 성공 후 중복 입력 차단 및 마을 씬으로 이동
        nicknameMessageText.text = "저장 데이터 생성에 성공했습니다.";
        isTransitioning = true;
        nicknameConfirmButton.interactable = false;
        nicknameCancelButton.interactable = false;
        StartCoroutine(TransitionToTownScene());
    }

    private void RestoreNicknamePanel(string message)
    {
        nicknamePanel.SetActive(true);
        isNicknamePanelOpen = true;
        nicknameInputField.text = pendingNewSaveName;
        nicknameMessageText.text = message;
        nicknameInputField.Select();
        nicknameInputField.ActivateInputField();
    }

    /// <summary>
    /// 닉네임 패널과 메인 메뉴를 페이드 아웃한 뒤 마을 씬으로 이동한다.
    /// </summary>
    private IEnumerator TransitionToTownScene()
    {
        CloseNicknamePanel();
        mainMenuScreenCanvasGroup.interactable = false;
        mainMenuScreenCanvasGroup.blocksRaycasts = false;

        yield return StartCoroutine(FadeCanvasGroup(mainMenuScreenCanvasGroup, mainMenuScreenCanvasGroup.alpha, 0f, fadeOutDuration));

        LoadingSceneManager.LoadScene(SceneName.Town);
    }

    /// <summary>
    /// 저장 데이터 선택 패널을 열고 저장 목록을 갱신한다.
    /// </summary>
    private void OpenSaveSelectPanel()
    {
        if (isSaveSelectPanelOpen || isNicknamePanelOpen || isTransitioning) return;

        if (SaveManager.Instance == null || !SaveManager.Instance.HasAnySaveData())
        {
            UpdateLoadButtonState();
            return;
        }

        if (saveSelectPanel == null)
        {
            Debug.LogWarning("saveSelectPanel 참조가 연결되지 않았습니다.");
            return;
        }

        saveSelectPanel.SetActive(true);
        isSaveSelectPanelOpen = true;
        PopulateSaveSlots();
    }

    /// <summary>
    /// 저장 데이터 선택 패널을 닫고 메뉴 상태로 복귀한다.
    /// </summary>
    private void CloseSaveSelectPanel()
    {
        bool returnToNickname = isReplaceSaveMode;

        if (saveSelectPanel != null)
        {
            saveSelectPanel.SetActive(false);
        }

        isSaveSelectPanelOpen = false;
        selectedSaveSlot = null;
        isReplaceSaveMode = false;
        SetSaveSelectMode(false);
        UpdateSaveSelectConfirmButton();
        UpdateLoadButtonState();

        if (returnToNickname)
        {
            RestoreNicknamePanel("닉네임을 확인하거나 새 게임 생성을 취소할 수 있습니다.");
        }
    }

    /// <summary>
    /// SaveManager에서 저장 목록을 조회해 ScrollView Content에 슬롯을 동적 생성한다.
    /// </summary>
    private void PopulateSaveSlots()
    {
        selectedSaveSlot = null;
        spawnedSaveSlots.Clear();

        if (saveSlotContent != null)
        {
            for (int i = saveSlotContent.childCount - 1; i >= 0; i--)
            {
                Destroy(saveSlotContent.GetChild(i).gameObject);
            }
        }

        if (saveSlotItemPrefab == null || saveSlotContent == null)
        {
            Debug.LogWarning("saveSlotItemPrefab 또는 saveSlotContent 참조가 연결되지 않았습니다.");
            UpdateSaveSelectConfirmButton();
            return;
        }

        List<SaveData> saveDataList = SaveManager.Instance.GetSaveDataList();
        for (int i = 0; i < saveDataList.Count; i++)
        {
            SaveSlotItemUI slotItem = Instantiate(saveSlotItemPrefab, saveSlotContent);
            slotItem.Bind(saveDataList[i], HandleSaveSlotSelected);
            spawnedSaveSlots.Add(slotItem);
        }

        UpdateSaveSelectConfirmButton();
    }

    /// <summary>
    /// 저장 슬롯 클릭 시 선택 상태를 갱신한다.
    /// </summary>
    private void HandleSaveSlotSelected(SaveSlotItemUI clickedSlot)
    {
        selectedSaveSlot = clickedSlot;

        for (int i = 0; i < spawnedSaveSlots.Count; i++)
        {
            if (spawnedSaveSlots[i] != null)
            {
                spawnedSaveSlots[i].SetSelected(spawnedSaveSlots[i] == selectedSaveSlot);
            }
        }

        UpdateSaveSelectConfirmButton();
    }

    private void UpdateSaveSelectConfirmButton()
    {
        if (saveSelectConfirmButton != null)
        {
            saveSelectConfirmButton.interactable = selectedSaveSlot != null;
        }
    }

    /// <summary>
    /// 선택한 저장 데이터를 불러온 뒤 해당 씬으로 이동한다.
    /// </summary>
    private void ConfirmSelectedSave()
    {
        if (isTransitioning || selectedSaveSlot == null || SaveManager.Instance == null)
        {
            return;
        }

        if (isReplaceSaveMode)
        {
            ConfirmReplacementSave();
            return;
        }

        if (!SaveManager.Instance.LoadSaveData(selectedSaveSlot.SaveSlotId))
        {
            Debug.LogWarning($"저장 데이터 불러오기에 실패했습니다: {selectedSaveSlot.SaveSlotId}");
            return;
        }

        SaveData loadedData = SaveManager.Instance.CurrentData;
        if (loadedData == null)
        {
            return;
        }

        string targetScene = string.IsNullOrWhiteSpace(loadedData.CurrentScene)
            ? SceneName.Town.ToString()
            : loadedData.CurrentScene;

        isTransitioning = true;
        if (saveSelectConfirmButton != null) saveSelectConfirmButton.interactable = false;
        if (saveSelectCancelButton != null) saveSelectCancelButton.interactable = false;

        StartCoroutine(TransitionToLoadedScene(targetScene, loadedData.PlayerPos));
    }

    private void ConfirmReplacementSave()
    {
        string selectedSaveSlotId = selectedSaveSlot.SaveSlotId;
        if (SaveManager.Instance.IsPlayerNameDuplicate(pendingNewSaveName, selectedSaveSlotId))
        {
            CloseSaveSelectPanelWithNicknameMessage("이미 사용 중인 닉네임입니다.");
            return;
        }

        if (!SaveManager.Instance.ReplaceSaveData(
                selectedSaveSlotId,
                pendingNewSaveName,
                SceneName.Town.ToString()))
        {
            CloseSaveSelectPanelWithNicknameMessage("선택한 저장 데이터를 교체하지 못했습니다.");
            return;
        }

        isReplaceSaveMode = false;
        pendingNewSaveName = string.Empty;
        SetSaveSelectMode(false);
        saveSelectPanel.SetActive(false);
        isSaveSelectPanelOpen = false;
        selectedSaveSlot = null;
        UpdateLoadButtonState();

        isTransitioning = true;
        nicknameConfirmButton.interactable = false;
        nicknameCancelButton.interactable = false;
        if (saveSelectConfirmButton != null) saveSelectConfirmButton.interactable = false;
        if (saveSelectCancelButton != null) saveSelectCancelButton.interactable = false;
        StartCoroutine(TransitionToTownScene());
    }

    private void CloseSaveSelectPanelWithNicknameMessage(string message)
    {
        CloseSaveSelectPanel();
        nicknameMessageText.text = message;
    }

    private void SetSaveSelectMode(bool replaceMode)
    {
        SetButtonText(
            saveSelectConfirmButton,
            replaceMode ? "선택 저장을 교체하고 새 게임 시작" : saveSelectConfirmDefaultText);
        SetButtonText(
            saveSelectCancelButton,
            replaceMode ? "닉네임 입력으로 돌아가기" : saveSelectCancelDefaultText);

        if (saveSelectMessageText != null)
        {
            saveSelectMessageText.text = replaceMode
                ? "새 게임으로 교체할 저장 데이터를 선택하세요."
                : saveSelectMessageDefaultText;
        }
    }

    private static string GetButtonText(Button button)
    {
        if (button == null) return string.Empty;
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        return text != null ? text.text : string.Empty;
    }

    private static void SetButtonText(Button button, string value)
    {
        if (button == null || string.IsNullOrEmpty(value)) return;
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            text.text = value;
        }
    }

    /// <summary>
    /// 저장 선택 패널과 메인 메뉴를 페이드 아웃한 뒤 불러온 씬으로 이동한다.
    /// </summary>
    private IEnumerator TransitionToLoadedScene(string targetScene, Vector3 playerPos)
    {
        CloseSaveSelectPanel();
        mainMenuScreenCanvasGroup.interactable = false;
        mainMenuScreenCanvasGroup.blocksRaycasts = false;

        yield return StartCoroutine(FadeCanvasGroup(mainMenuScreenCanvasGroup, mainMenuScreenCanvasGroup.alpha, 0f, fadeOutDuration));

        if (playerPos != Vector3.zero)
        {
            LoadingSceneManager.LoadScene(targetScene, playerPos);
        }
        else
        {
            LoadingSceneManager.LoadScene(targetScene);
        }
    }
}
