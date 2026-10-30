using System;
using UnityEngine;

public class MonsterSensor : MonoBehaviour
{
    //부모클래스가 받을 수 있도록 이벤트
    public event Action<Collider> OnSensorEnter;
    public event Action<Collider> OnSensorLost;

    private void OnTriggerEnter(Collider other)
    {
        //영역 안으로 플레이어태그 오브젝트라면 신호를 보냄
        if (other.CompareTag("Player"))
        {
            OnSensorEnter?.Invoke(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //영역 밖으로 플레이어태그 오브젝트가 나가면 신호를 보냄
        if (other.CompareTag("Player"))
        {
            OnSensorLost?.Invoke(other);
        }
    }

    /// <summary>
    /// 센서 콜라이더의 실제 월드 스케일 기준 반지름을 반환합니다.
    /// </summary>
    /// <returns></returns>
    public float GetSensorRadius()
    {
        // SphereCollider인 경우
        if(TryGetComponent<SphereCollider>(out var sphere))
        {
            Vector3 scale = transform.lossyScale;
            float maxScale = MathF.Max(MathF.Max(scale.x, scale.y), scale.z);

            return sphere.radius * maxScale;
        }

        // 다른 콜라이더인 경우 bounds 크기로 대체
        if(TryGetComponent<Collider>(out var col))
        {
            return col.bounds.extents.x;
        }

        // 기본 예외값
        return 5f;
    }
}
