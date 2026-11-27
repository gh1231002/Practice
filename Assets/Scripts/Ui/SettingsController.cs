using UnityEngine;

public class SettingsController : MonoBehaviour
{
    [Header("설정 화면 패널 UI")]
    [SerializeField] GameObject settingsPanel;

    /// <summary>
    /// 설정 패널이 현재 열려 있는지 여부
    /// </summary>
    public bool IsOpen => settingsPanel != null && settingsPanel.activeSelf;

    void Start()
    {
        // 설정 화면 패널 비활성화
        settingsPanel.SetActive(false);
    }

    public void OffSettingsPanel()
    {
        settingsPanel.SetActive(false);
    }

    public void OpenSettingsPanel()
    {
        settingsPanel.SetActive(true);
    }
}
