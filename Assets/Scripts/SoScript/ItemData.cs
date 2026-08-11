using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("공통 정보")]
    [SerializeField] string ItemName;
    [SerializeField] Sprite ItemIcon;
    [TextArea(3, 5)]
    [SerializeField] string ItemInfo;
    [SerializeField] bool IsStackable;

    public string itemName => ItemName;
    public virtual string itemInfo => ItemInfo;
    public Sprite itemIcon => ItemIcon;
    public bool isStackable => IsStackable;
}
