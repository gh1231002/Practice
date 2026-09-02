using Unity.Cinemachine;
using UnityEngine;

public class CinemachineTargetAssigner : MonoBehaviour
{
    void Start()
    {
        // 씬이 로드 될때 player 태그를 가진 플레이어를 찾아 카메라 타겟으로 등록
        GameObject player = GameObject.FindWithTag("Player");
        if(player != null)
        {
            var cam = GetComponent<CinemachineCamera>();
            cam.Target.TrackingTarget = player.transform;
        }
    }
}
