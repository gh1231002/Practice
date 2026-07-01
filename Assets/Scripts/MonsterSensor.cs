using System;
using UnityEngine;

public class MonsterSensor : MonoBehaviour
{
    //부모클래스가 받을 수 있도록 이벤트
    public event Action<Collider> OnPlayerDetected;
    public event Action<Collider> OnPlayerLost;

    private void OnTriggerEnter(Collider other)
    {
        //영역 안으로 플레이어태그 오브젝트라면 신호를 보냄
        if (other.CompareTag("Player"))
        {
            OnPlayerDetected?.Invoke(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //영역 밖으로 플레이어태그 오브젝트가 나가면 신호를 보냄
        if (other.CompareTag("Player"))
        {
            OnPlayerLost?.Invoke(other);
        }
    }
}
