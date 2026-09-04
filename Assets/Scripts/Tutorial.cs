using UnityEngine;

public class Tutorial : MonoBehaviour
{
    [Header("키 가이드 패널")]
    [SerializeField] GameObject GuidePanel;

    private void Awake()
    {
        GuidePanel.SetActive(false);
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            GuidePanel.SetActive(true);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GuidePanel.SetActive(false);
        }
    }
}
