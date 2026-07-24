using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestUi : MonoBehaviour
{
    [SerializeField] GameObject QuestPrefab;
    /// <summary>
    /// QuestManager의 activeQuests목록 받아옴
    /// </summary>
    /// <param name="activeQuests"></param>
    public void UpdateTrackerUi(List<QuestProgress> activeQuests)
    {
        //자기 자신 아래에있는 자식들 삭제
        foreach(Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        //퀘스트 수만큼 자식으로 생성
        foreach(var progress in activeQuests)
        {
            GameObject obj = Instantiate(QuestPrefab, transform);

            TextMeshProUGUI titleText = obj.transform.Find("QuestTitle_Text").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI progressText = obj.transform.Find("QuestProgress_Text").GetComponent<TextMeshProUGUI>();

            titleText.text = progress.questData.questTitle;

            if(progress.questState == QuestState.CanComplete)
            {
                progressText.text = $"완료 가능";
            }
            else
            {
                progressText.text = $"{progress.currentCount} / {progress.questData.targetCount}";
            }
        }
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }
}
