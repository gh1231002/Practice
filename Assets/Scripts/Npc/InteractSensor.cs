using System;
using UnityEngine;

public class InteractSensor : MonoBehaviour
{
    public event Action OnInteract;
    public event Action<Collider> StayInteract;
    public event Action OffInteract;

    Player_CC Player;

    private void Start()
    {
        GameObject obj = GameObject.FindWithTag("Player");
        Player = obj.GetComponent<Player_CC>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            OnInteract?.Invoke();
            Player.SetInteract(true);
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
            OffInteract?.Invoke();
            Player.SetInteract(false);
        }
    }
}
