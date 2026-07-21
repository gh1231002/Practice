using System.Collections.Generic;
using UnityEngine;


public enum QuestState
{
    NotStarted,
    InProgress,
    CanComplete,
    Completed,
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    [SerializeField] List<QuestProgress> ActiveQuests = new List<QuestProgress>();
    [SerializeField] List<QuestData> CompletedQuests = new List<QuestData>();

    //퀘스트 수락시
    public void AcceptQuest(QuestData quest)
    {
        QuestProgress progress = new QuestProgress(quest);
        ActiveQuests.Add(progress);
    }

    //몬스터 처치/ 대화 완료 등 이벤트 발생 시 진행도 업데이트
    public void UpdateQuestProgress(QuestData targetQuest, int amount = 1)
    {
        QuestProgress progress = ActiveQuests.Find(q => q.QuestData == targetQuest);

        if(progress != null && progress.State == QuestState.InProgress)
        {
            progress.AddCount(amount);
            if(progress.State == QuestState.CanComplete)
            {
                //퀘스트 완료가능이라고 알려줌
            }
        }
    }
    /// <summary>
    /// NPC가 현재 퀘스트 상태를 물어볼때(DetermineCurrentState())
    /// </summary>
    /// <param name="quest"></param>
    /// <returns></returns>
    public QuestState GetQuestState(QuestData quest)
    {
        //완료한 퀘스트인가?
        if (CompletedQuests.Contains(quest)) return QuestState.Completed;
        //진행 중인 퀘스트인가?
        QuestProgress progress = ActiveQuests.Find(q => q.QuestData == quest);

        if(progress != null)
        {
            //InProgress 또는 CanComplete 반환
            return progress.State;
        }
        //그 외에는 시작 전 반환
        return QuestState.NotStarted;
    }

    public void CompleteQuest(QuestData quest)
    {
        QuestProgress progress = ActiveQuests.Find(q => q.QuestData == quest);

        if (progress != null && progress.State == QuestState.CanComplete)
        {
            ActiveQuests.Remove(progress);
            CompletedQuests.Add(quest);
        }
    }

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
