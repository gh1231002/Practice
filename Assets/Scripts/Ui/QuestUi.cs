using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestUi : MonoBehaviour
{
    [SerializeField] GameObject QuestPrefab;

    List<GameObject> spawnedSlots = new List<GameObject>();

    /// <summary>
    /// QuestManager의 activeQuests목록 받아옴
    /// </summary>
    /// <param name="activeQuests"></param>
    public void UpdateTrackerUi(List<QuestProgress> activeQuests)
    {
        // 부족한 만큼만 프리팹을 추가 생성하여 리스트에 보관
        while(spawnedSlots.Count <  activeQuests.Count)
        {
            GameObject obj = Instantiate(QuestPrefab, transform);
            spawnedSlots.Add(obj);
        }

        // 슬롯들을 순회하며 activeQuests 데이터 갱싱 및 활성화/비활성화
        for(int i = 0; i < spawnedSlots.Count; i++)
        {
            if(i <  activeQuests.Count)
            {
                var progress = activeQuests[i];
                GameObject obj = spawnedSlots[i];
                obj.SetActive(true);

                TextMeshProUGUI titleText = obj.transform.Find("QuestTitle_Text").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI progressText = obj.transform.Find("QuestProgress_Text").GetComponent<TextMeshProUGUI>();

                titleText.text = progress.questData.questTitle;

                if (progress.questState != QuestState.CanComplete)
                {
                    progressText.text = $"{progress.currentCount} / {progress.questData.targetCount}";
                }
                else
                {
                    progressText.text = "완료 가능";
                }
            }
            else
            {
                // activeQuests 수보다 남는 기존 슬롯은 파괴 대신 비활성화 처리
                spawnedSlots[i].SetActive(false);
            }
        }
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }
}
