
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static InteractNpc;


public class InteractNpc : MonoBehaviour
{
    [Header("NPC 식별 정보")]
    [SerializeField] string NpcId;
    [SerializeField] string NpcName;
    [SerializeField] InteractSensor InSensor;
    [SerializeField] float RotateSpeed;
    [Header("퀘스트 정보")]
    [SerializeField] List<QuestData> QuestList = new List<QuestData>();
    [Header("기본 대사(진행 가능한 퀘스트가 없을 때)")]
    [SerializeField] string[] DefalutDialogues;

    string InteractKey;
    string DeviceGroup;
    Quaternion OriginRotation;
    bool isRestore;
    Player_CC Player;

    public string npcId => NpcId;
    public string npcName => NpcName;
    public List<QuestData> questList => QuestList;
    public string[] defalutDialogues => DefalutDialogues;

    void Start()
    {
        GameObject obj = GameObject.FindWithTag("Player");
        Player = obj.GetComponent<Player_CC>();

        InSensor.OnInteract += OnInteractPanel;
        InSensor.OffInteract += OffInteractPanel;
        InSensor.StayInteract += Rotation;

        OriginRotation = transform.rotation;
    }

    private void OnInteractPanel()
    {
        TalkManager.Instance.OnInteractPanel();
        isRestore = false;
        Player.OnDialogue -= SendNpcInfo;//중복방지
        Player.OnDialogue += SendNpcInfo;
    }
    private void OffInteractPanel()
    {
        TalkManager.Instance.OffInteractPanel();
        isRestore = true;
        Player.OnDialogue -= SendNpcInfo;
    }
    //플레이어가 상호작용이벤트발생할때 자신의 정보를
    //퀘스트매니저한테 전달
    private void SendNpcInfo()
    {
        QuestManager.instance.ProcessInteraction(this);
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
