using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExitConfirmPanelController : MonoBehaviour
{
    [Header("종료 확인 패널 UI")]
    [SerializeField] GameObject exitConfirmPanel;
    [SerializeField] Button yesButton;
    [SerializeField] Button noButton;

    /// <summary>
    /// 종료 확인 패널이 현재 열려 있는지 여부
    /// </summary>
    public bool IsOpen => exitConfirmPanel != null && exitConfirmPanel.activeSelf;

    /// <summary>
    /// UiManager.OpenPanel() 호출 시 전달할 패널 GameObjct
    /// </summary>
    public GameObject Panel => exitConfirmPanel;

    void Start()
    {
        exitConfirmPanel.SetActive(false);

        // 버튼 이벤트 연결
        yesButton.onClick.AddListener(YesButtonClicked);
        noButton.onClick.AddListener(CloseExitConfirmPanel);
    }

    private void YesButtonClicked()
    {
        // 현재 씬이 타이틀 씬이면 게임 종료
        if (SceneManager.GetActiveScene().name == SceneName.Title.ToString())
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            return;
        }

        // 타이틀 씬이 아니면 타이틀 씬으로 이동 (UiManager가 있으면 페이드 + 로딩 씬 경유)
        if (UiManager.Instance != null)
        {
            UiManager.Instance.ReturnToTitleScene();
        }
        else
        {
            LoadingSceneManager.LoadScene(SceneName.Title);
        }
    }

    /// <summary>
    /// 종료 확인 패널을 닫는다.
    /// - 인게임(UiManager 존재): UiManager.ClosePanel()을 경유하여 PanelList 제거 및 커서 복구
    /// - 타이틀 씬(UiManager 없음): 단순 비활성화
    /// </summary>
    public void CloseExitConfirmPanel()
    {
        if (UiManager.Instance != null)
        {
            UiManager.Instance.ClosePanel(exitConfirmPanel);
        }
        else
        {
            exitConfirmPanel.SetActive(false);
        }
    }

    public void OpenExitConfirmPanel()
    {
        exitConfirmPanel.SetActive(true);
    }
}
