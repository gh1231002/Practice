using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestData", menuName = "Scriptable Objects/QuestData")]
public class QuestData : ScriptableObject
{
    [Serializable]
    public class ChoiceData
    {
        public string ChoiceText;
        public WeaponData WeaponDate;
    }

    [Header("퀘스트 기본정보")]
    [SerializeField] string QuestTitle;
    [TextArea(3, 5)]
    [SerializeField] string QuestScription;
    [SerializeField] QuestType Type;
    [SerializeField] int TargetCount;
    [Header("연계 퀘스트 조건, 없으면 null")]
    [SerializeField] QuestData ParentQuest;
    [Header("NPC 정보")]
    [SerializeField] string StartNpcName;
    [SerializeField] string TargetNpcName;
    [Header("대화 데이터")]
    [TextArea(3, 5)]
    [SerializeField] string[] StartDialogues;
    [TextArea(3, 5)]
    [SerializeField] string[] RemindDialogues;
    [TextArea(3, 5)]
    [SerializeField] string[] CompleteDialogues;
    [Header("선택지 & 보상 연동 위치")]
    [SerializeField] bool isChoiceDialogue;
    [SerializeField] int ChoiceDialogueIndex;
    [SerializeField] List<ChoiceData> Choices;


    public string questTitle => QuestTitle;
    public string qusetScription => QuestScription;
    public string startNpcName => StartNpcName;
    public string targetNpcName => TargetNpcName;
    public QuestType questType => Type;
    public int targetCount => TargetCount;
    public string[] startDialogues => StartDialogues;
    public string[] remindDialogues => RemindDialogues;
    public string[] completeDialogues => CompleteDialogues;
    public QuestData parentQuest => ParentQuest;
    public bool ischoicedialogue => isChoiceDialogue;
    public int choiscedialogueindex => ChoiceDialogueIndex;
    public List<ChoiceData> choices => Choices;


    public enum QuestType
    {
        General,//일반
        Hunt,//사냥
        Collect,//수집
        Reward,//보상
    }
}
