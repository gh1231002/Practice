
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static InteractNpc;


public class InteractNpc : MonoBehaviour
{
    [SerializeField] InteractSensor InSensor;
    [SerializeField] float RotateSpeed;
    [Header("NPC가 제일 먼저 줘야하는 퀘스트")]
    [SerializeField] QuestData NpcQuest;
    [Header("대화 데이터")]
    [SerializeField] List<DialogueDataGroup> TalkDataList;
    
    [Serializable] public class DialogueDataGroup
    {
        [SerializeField] QuestData questData;
        [SerializeField] TalkState talkState;
        [SerializeField] DialogueData dialogueData;

        public QuestData QuestData => questData;
        public TalkState StateKey => talkState;
        public DialogueData DialogueData => dialogueData;
    }

    string InteractKey;
    string DeviceGroup;
    Quaternion OriginRotation;
    bool isRestore;

    private Action pendingPostTalkAction;

    Player_CC Player;

    public enum TalkState
    {
        FirstQuest,//시작전
        Remind,//진행중
        CanCompleted,//목표 달성
        Default,//완료 후 또는 일반 대화
    }

    void Start()
    {
        GameObject objPlayer = GameObject.FindWithTag("Player");
        Player = objPlayer.GetComponent<Player_CC>();

        InSensor.OnInteract += OnInteractPanel;
        InSensor.OffInteract += OffInteractPanel;
        InSensor.StayInteract += Rotation;
        Player.OnDialogue += OnDialogue;
        Player.OffDialogue += OffDialogue;

        OriginRotation = transform.rotation;
    }

    private void OnInteractPanel()
    {
        TalkManager.Instance.OnInteractPanel();
        isRestore = false;
    }
    private void OffInteractPanel()
    {
        TalkManager.Instance.OffInteractPanel();
        isRestore = true;
    }

    private void OnDialogue()
    {
        QuestData CurrentQuest = GetQusetData();
        TalkState CurrentState = DetermineCurrentState();
        DialogueData data = GetdialogueDate(CurrentQuest, CurrentState);

        if (data == null && CurrentState != TalkState.Default)
        {
            data = GetdialogueDate(null, TalkState.Default);
        }
        if (data != null)
        {
            SetupPendingQuestAction(CurrentQuest, CurrentState);
            TalkManager.Instance.OffTalk += OnDialogueEnded;
            //이름과 대사들을 넘겨줌
            TalkManager.Instance.StartDialoguePanel(data);
        }
    }


    private TalkState DetermineCurrentState()
    {
        if(NpcQuest == null || QuestManager.instance == null)
            return TalkState.Default;

        QuestState state = QuestManager.instance.GetQuestState(NpcQuest);

        switch(state)
        {
            case QuestState.NotStarted:
                return TalkState.FirstQuest;

            case QuestState.InProgress:
                return TalkState.Remind;

            case QuestState.CanComplete:
                return TalkState.CanCompleted;

            case QuestState.Completed:
            default:
                return TalkState.Default;
        }
    }

    /// <summary>
    /// 대화 상태에 따라 '수락' 또는 '완료' 로직을 상자에 담아둡니다.
    /// </summary>
    private void SetupPendingQuestAction(QuestData quest, TalkState state)
    {
        pendingPostTalkAction = null;

        if (quest == null || QuestManager.instance == null) return;

        if (state == TalkState.FirstQuest)
        {
            // 대화 종료 시 퀘스트 수락
            pendingPostTalkAction = () => QuestManager.instance.AcceptQuest(quest);
        }
        else if (state == TalkState.CanCompleted)
        {
            // 대화 종료 시 퀘스트 완료 처리
            pendingPostTalkAction = () => QuestManager.instance.CompleteQuest(quest);
        }
    }

    /// <summary>
    /// 대화창이 닫혔을 때(TalkManager.OffTalk) 실행
    /// </summary>
    private void OnDialogueEnded()
    {
        // 이벤트 중복 호출 방지를 위해 즉시 구독 해제
        TalkManager.Instance.OffTalk -= OnDialogueEnded;

        // 대화 전에 세팅해 둔 퀘스트 수락/완료 로직 실행!
        pendingPostTalkAction?.Invoke();
        pendingPostTalkAction = null;
    }

    private void OffDialogue()
    {
        TalkManager.Instance.OffDialoguePanel();
    }

    private void Rotation(Collider other)
    {
        Vector3 Dir = other.transform.position - transform.position;
        Dir.y = 0f;
        Quaternion TargetPos = Quaternion.LookRotation(Dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, TargetPos, RotateSpeed * Time.deltaTime);
    }

    private DialogueData GetdialogueDate(QuestData data, TalkState key)
    {
        foreach(var group in TalkDataList)
        {
            if(group.StateKey == key)
            {
                //기본대화인경우 QuestData가 null이어도 정보 반환
                if(key == TalkState.Default)
                {
                    return group.DialogueData;
                }
                //퀘스트 대화인 경우 QuestData가 null이 아니고, 현재 퀘와 같은지 확인
                if (group.QuestData != null && group.QuestData == data)
                {
                    return group.DialogueData;
                }
            }
        }
        return null;
    }

    private QuestData GetQusetData()
    {
        //퀘스트매니저에서 진행 중인 questdata 받아오는 로직연결
        return NpcQuest;
    }

    private void Update()
    {
        if(isRestore)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, OriginRotation, RotateSpeed * Time.deltaTime);
        }
    }
}
