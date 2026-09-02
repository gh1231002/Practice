using UnityEngine;

public class QuickSlotManager : MonoBehaviour
{
    public static QuickSlotManager Instance;

    [Header("ƒ¸ΩΩ∑‘ πËø≠")]
    [SerializeField] QuickSlotUI[] quickSlotUIs;

    [Header("±‚∫ª ¥‹√‡±‚ º≥¡§")]
    [SerializeField]
    KeyCode[] defaultHotKeys = new KeyCode[]
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
    };

    private void Awake()
    {
        // ΩÃ±€≈Ê √ ±‚»≠
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
}
