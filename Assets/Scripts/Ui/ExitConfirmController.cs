using UnityEngine;
using UnityEngine.UI;

public class ExitConfirmPanelController : MonoBehaviour
{
    [Header("종료 확인 패널 UI")]
    [SerializeField] GameObject exitConfirmPanel;
    [SerializeField] Button yesButton;
    [SerializeField] Button noButton;

    void Start()
    {
        exitConfirmPanel.SetActive(false);

        // 버튼 이벤트 연결
        yesButton.onClick.AddListener(YesButtonClicked);
        noButton.onClick.AddListener(() => exitConfirmPanel.SetActive(false));
    }

    private void YesButtonClicked()
    {
        Application.Quit();
    #if UNITY_EDITOR
    // Unity 에디터에서는 플레이 모드 종료
    UnityEditor.EditorApplication.isPlaying = false;
    #endif
    }

    public void OpenExitConfirmPanel()
    {
        exitConfirmPanel.SetActive(true);
    }
}
