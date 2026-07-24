using UnityEngine;

[CreateAssetMenu(fileName = "NewScriptableObjectScript", menuName = "Scriptable Objects/DialogueData")]
public class DialogueData : ScriptableObject
{
    [Header("NPC 대화정보")]
    [SerializeField] string NpcName;

    [TextArea(3, 5)]
    [SerializeField] string[] Dialogues;

    public string npcname => NpcName;
    public string[] dialogues => Dialogues;
}
