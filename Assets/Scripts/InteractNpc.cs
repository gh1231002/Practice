using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractNpc : MonoBehaviour
{
    [SerializeField] InteractSensor InSensor;
    [SerializeField] float RotateSpeed;
    [Header("대화 데이터")]
    [SerializeField] DialogueData TalkData;

    string InteractKey;
    string DeviceGroup;
    Quaternion OriginRotation;
    bool isRestore;

    Player_CC Player;

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
        //이름과 대사들을 넘겨줌
        TalkManager.Instance.StartDialoguePanel(TalkData.name, TalkData.dialogues);
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


    private void Update()
    {
        if(isRestore)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, OriginRotation, RotateSpeed * Time.deltaTime);
        }
    }
}
