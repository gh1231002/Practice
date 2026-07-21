using UnityEngine;

[CreateAssetMenu(fileName = "QuestData", menuName = "Scriptable Objects/QuestData")]
public class QuestData : ScriptableObject
{
    [Header("퀘스트 기본정보")]
    [SerializeField] string QuestTitle;
    [TextArea(3, 5)]
    [SerializeField] string QuestScription;
    [SerializeField] QuestType Type;
    [SerializeField] int TargetCount;

    public string questtitle => QuestTitle;
    public string qusetscription => QuestScription;
    public QuestType questType => Type;
    public int targetcount => TargetCount;

    public enum QuestType
    {
        General,
        Hunt,
        Collect,
    }
}
