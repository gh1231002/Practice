using System;
using UnityEngine;

[Serializable]
public class QuestProgress
{
    [SerializeField] QuestData questData;
    [SerializeField] QuestState state;
    [SerializeField] int currentCount;

    public QuestData QuestData => questData;
    public QuestState State => state;
    public int CurrentCount => currentCount;

    //생성자
    public QuestProgress(QuestData Data)
    {
        this.questData = Data;
        this.state = QuestState.InProgress;
        this.currentCount = 0;
    }
    /// <summary>
    /// 진행 수치를 증가시키고 목표 달성 여부를 검사
    /// </summary>
    /// <param name="amount"></param>
    public void AddCount(int amount = 1)
    {
        if (state != QuestState.InProgress) return;

        currentCount += amount;

        if(currentCount >= questData.targetcount)
        {
            currentCount = questData.targetcount;
            SetState(QuestState.CanComplete);
        }
    }
    /// <summary>
    /// 퀘스트 상태를 강제로 변경
    /// </summary>
    /// <param name="NewState"></param>
    public void SetState(QuestState NewState)
    {
        state = NewState;
    }
    /// <summary>
    /// 목표를 완수했는지 확인하는 도우미 프로퍼티
    /// </summary>
    public bool IsTargetReached => currentCount >= questData.targetcount;
}
