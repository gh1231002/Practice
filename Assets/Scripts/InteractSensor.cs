using System;
using UnityEngine;

public class InteractSensor : MonoBehaviour
{
    public event Action OnInteract;
    public event Action<Collider> StayInteract;
    public event Action<Collider> OffInteract;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            OnInteract?.Invoke();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            StayInteract?.Invoke(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OffInteract?.Invoke(other);
        }
    }
}
