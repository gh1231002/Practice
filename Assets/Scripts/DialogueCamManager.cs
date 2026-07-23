using Unity.Cinemachine;
using UnityEngine;

public class DialogueCamManager : MonoBehaviour
{
    public static DialogueCamManager Instance;

    [Header("시네머신 대화 카메라")]
    [SerializeField] CinemachineCamera DialogueCam;
    [Header("카메라 오프셋 설정")]
    [SerializeField] Vector3 CamOffset;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartDialogueCam(InteractNpc npc)
    {
        if (DialogueCam == null) return;
        //npc위치를 기준으로 카메라의 위치를 계산해서 배치
        //npc가 바라보는 방향 기준으로
        Transform HeadTrs = npc.LookTarget;
        Vector3 targetPos = HeadTrs.position
                          + (HeadTrs.forward * CamOffset.z)
                          + (HeadTrs.right * CamOffset.x)
                          + (Vector3.up * CamOffset.y);

        DialogueCam.transform.position = targetPos;
        //npc를 바라보게 설정
        DialogueCam.LookAt = HeadTrs;
        //플레이어 카메라보다 값을 올려서 전환
        DialogueCam.Priority.Value = 20;
    }

    public void EnddialogueCam()
    {
        if (DialogueCam == null) return;
        DialogueCam.Priority.Value = 0;
    }
}
