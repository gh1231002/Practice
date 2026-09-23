using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TeleportUi : MonoBehaviour
{
    [Serializable]
    public struct BtnInfo
    {
        public Button Btn;
        public TextMeshProUGUI BtnText;
        public Vector3 Pos;
        public SceneName TargetScene;
        public string DestinationName;
    }
    [Header("이동 버튼 정보 목록")]
    [SerializeField] BtnInfo TownBtnInfo;
    [SerializeField] BtnInfo GraveBtnInfo;
    [SerializeField] BtnInfo BossBtnInfo;

    private void OnEnable()
    {
        RefreshBtnStates();
    }

    private void RefreshBtnStates()
    {
        // 현재 씬 이름을 가져옴
        string currentScene = SceneManager.GetActiveScene().name;

        UpdateBtnState(TownBtnInfo, currentScene);
        UpdateBtnState(GraveBtnInfo, currentScene);
        UpdateBtnState(BossBtnInfo, currentScene);
    }

    private void UpdateBtnState(BtnInfo info, string curSceneName)
    {
        // TargetScene과 현재 씬이 같은지 비교
        bool isCurrentScene = curSceneName == info.TargetScene.ToString();

        // 현재 씬이면 클릭 불가
        info.Btn.interactable = !isCurrentScene;

        if(isCurrentScene)
        {
            info.BtnText.text = $"{info.DestinationName}\n(현재 위치)";
        }
        else
        {
            info.BtnText.text = $"{info.DestinationName}(으)로 이동";
        }
    }

    private void TeleportTo(BtnInfo info)
    {
        // UI 닫기
        UiManager.Instance.ToggleTeleportPanel(false);

        // Ui Manger의 이동 함수 호출
        UiManager.Instance.LoadSceneWithFade(info.TargetScene.ToString(), info.Pos);
    }

    public void OnClickTown() => TeleportTo(TownBtnInfo);
    public void OnClickGrave() => TeleportTo(GraveBtnInfo);
    public void OnClickBoss() => TeleportTo(BossBtnInfo);
}
