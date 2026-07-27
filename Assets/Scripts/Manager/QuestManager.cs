using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Timeline.Actions;
using UnityEngine;


public enum QuestState
{
    NotStarted,
    InProgress,
    CanComplete,
    Completed,
}

public enum TalkType
{
    Start, Remind, Complete, Default,
}

[Serializable]
public class QuestProgress
{
    public QuestData questData;
    public QuestState questState;
    public int currentCount;

    public QuestProgress(QuestData data)
    {
        questData = data;
        questState = QuestState.InProgress;
        currentCount = 0;
    }
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;
    [Header("퀘스트 전체 데이터 저장소")]
    [SerializeField] List<QuestProgress> ActiveQuests = new List<QuestProgress>();
    [SerializeField] List<QuestData> CompletedQuests = new List<QuestData>();
    [Header("QuestTrackerUi 스크립트")]
    [SerializeField] QuestUi questUi;

    //대화 진행 중인 임시 데이터
    InteractNpc currentNpc;
    QuestData currentQuest;
    TalkType currentTalkType;
    string[] currentDialogues;

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

    private void Start()
    {
        UiManager.Instance.OffTalk += OnDialogueEnd;
    }

    /// <summary>
    /// npc가 말을 걸어올때 처리하는 곳
    /// </summary>
    /// <param name="npc"></param>
    public void ProcessInteraction(InteractNpc npc)
    {
        //전달 받은 npc 정보 저장
        currentNpc = npc;
        CheckNpcTalkComplete(npc);
        //어떤 대화/퀘스트를 진행할지 우선순위 판단
        SelectQuestAndTalkType(npc);
        //대화카메라에게 npc의 위치정보 전달
        DialogueCamManager.Instance.StartDialogueCam(currentNpc);
        //talkmanager에게 대화창 출력 요청
        UiManager.Instance.StartDialogue(currentNpc.npcName, currentDialogues, currentQuest);
    }
    /// <summary>
    /// npc에게 말을 걸때 진행 중인 퀘스트의 방문/대화 목표를 달성시키는 함수
    /// </summary>
    /// <param name="npc"></param>
    private void CheckNpcTalkComplete(InteractNpc npc)
    {
        foreach(var progress in ActiveQuests)
        {
            //진행 중인 퀘스트 중에서
            if(progress.questState == QuestState.InProgress)
            {
                //방문 퀘스트 목표 npc와 말을 건 npc가 동일할때
                if(progress.questData.targetNpcName == npc.npcId)
                {
                    progress.questState = QuestState.CanComplete;
                }
            }
        }
    }

    private void SelectQuestAndTalkType(InteractNpc npc)
    {
        //1순위 cancomplete
        //가지고 있는 퀘스트 중 완료가능한 퀘스트가 있는지 검사
        //완료 대상 npc와 지금 말을 건 npc와 같은 경우
        foreach(var quest in ActiveQuests)
        {
            if(quest.questState == QuestState.CanComplete
                && quest.questData.targetNpcName == npc.npcId)
            {
                currentQuest = quest.questData;
                currentTalkType = TalkType.Complete;
                currentDialogues = quest.questData.completeDialogues;
                return;
            }
        }
        //2순위 inprogress
        //진행 중인 퀘스트가 있고, 이 npc가 발주자(startnpc)인지 검사
        foreach (var quest in npc.questList)
        {
            if (GetQuestState(quest) == QuestState.InProgress)
            {
                if(npc.npcId == quest.startNpcName)
                {
                    currentQuest = quest;
                    currentTalkType = TalkType.Remind;
                    currentDialogues = quest.remindDialogues;
                    return;
                }
            }
        }
        //3순위 notstart + canstartquest
        //수락가능한 퀘스트가 있는지 검사(선행퀘 체크)
        foreach (var quest in npc.questList)
        {
            if (GetQuestState(quest) == QuestState.NotStarted && CanStartQuest(quest))
            {
                currentQuest = quest;
                currentTalkType = TalkType.Start;
                currentDialogues = quest.startDialogues;
                return;
            }
        }
        //4순위 default
        currentQuest = null;
        currentTalkType = TalkType.Default;
        currentDialogues = npc.defalutDialogues;
    }

    public QuestState GetQuestState(QuestData targetQuest)
    {
        if (targetQuest == null) return QuestState.NotStarted;

        //이미 완료한 목록에 있는가?
        if(CompletedQuests.Contains(targetQuest))
        {
            return QuestState.Completed;
        }

        QuestProgress progress = ActiveQuests.Find(p => p.questData == targetQuest);
        if(progress != null)
        {
            return progress.questState;
        }
        //진행 중도 아니고 완료도 안 했으면 시작 전
        return QuestState.NotStarted;
    }
    /// <summary>
    /// 선행 퀘스트 확인
    /// </summary>
    /// <param name="targetQuest"></param>
    /// <returns></returns>
    public bool CanStartQuest(QuestData targetQuest)
    {
        //선행 퀘가 없으면 바로 시작 가능
        if (targetQuest.parentQuest == null) return true;
        
        return CompletedQuests.Contains(targetQuest.parentQuest);
    }
    /// <summary>
    /// 퀘스트 수락
    /// </summary>
    /// <param name="quest"></param>
    public void AcceptQuest(QuestData quest)
    {
        if (quest == null) return;
        //이미 진행 중인지 중복 체크
        QuestProgress progress = ActiveQuests.Find(p => p.questData == quest);
        if (progress != null) return;
        //새로운 진행 객체 생성 및 리스트 추가
        QuestProgress newProgress = new QuestProgress(quest);
        ActiveQuests.Add(newProgress);
        questUi.UpdateTrackerUi(ActiveQuests);
    }
    /// <summary>
    /// 퀘스트 진행 상황 업데이트
    /// </summary>
    /// <param name="targetQuest"></param>
    /// <param name="amount"></param>
    public void UpdateQuestProgress(QuestData targetQuest, int amount = 1)
    {
        if(targetQuest == null) return;
        //진행 중이 퀘스트에서 찾기
        QuestProgress progress = ActiveQuests.Find(p => p.questData == targetQuest);

        if(progress != null && progress.questState == QuestState.InProgress)
        {
            progress.currentCount += amount;
            //목표 달성 체크
            if(progress.currentCount >= targetQuest.targetCount)
            {
                progress.currentCount = targetQuest.targetCount;
                progress.questState = QuestState.CanComplete;

                questUi.UpdateTrackerUi(ActiveQuests);
            }
        }
    }
    /// <summary>
    /// 퀘스트 완료 처리
    /// </summary>
    /// <param name="quest"></param>
    public void CompleteQuest(QuestData quest)
    {
        if(quest == null) return;

        QuestProgress progress = ActiveQuests.Find(p => p.questData == quest);

        if(progress != null && progress.questState == QuestState.CanComplete)
        {
            //진행 목록에서 제거
            ActiveQuests.Remove(progress);
            //완료 목록에 저장
            CompletedQuests.Add(quest);

            questUi.UpdateTrackerUi(ActiveQuests);
        }
    }
    /// <summary>
    /// TalkManager의 대화 종료 이벤트와 연결
    /// </summary>
    private void OnDialogueEnd()
    {
        //진행중인 퀘스트 대화가 아니거나 null인상태로 끝났다면 
        //데이터 초기화만 하고 리턴
        if(currentQuest == null)
        {
            ClearCurrentTalkData();
            return;
        }

        switch(currentTalkType)
        {
            case TalkType.Start:
                AcceptQuest(currentQuest);
                break;

            case TalkType.Complete:
                CompleteQuest(currentQuest);
                break;

            case TalkType.Remind:
            case TalkType.Default:
                break;
        }
        //임시 데이터 초기화
        ClearCurrentTalkData();
    }
    private void ClearCurrentTalkData()
    {
        currentNpc = null;
        currentQuest = null;
        currentDialogues = null;
        currentTalkType = TalkType.Default;
    }
}
