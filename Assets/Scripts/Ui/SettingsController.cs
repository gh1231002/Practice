using UnityEngine;

public class SettingsController : MonoBehaviour
{
    [Header("설정 화면 패널 UI")]
    [SerializeField] GameObject settingsPanel;

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
