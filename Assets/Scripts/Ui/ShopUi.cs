using UnityEngine;

public class ShopUi : MonoBehaviour
{
    ShopSlotUi[] shopSlots;
    PlayerInventory inventory;

    private void Awake()
    {
        // 하위의 모든 ShopSlotUi 검색
        shopSlots = GetComponentsInChildren<ShopSlotUi>(true);

        GameObject obj = GameObject.FindWithTag("Player");
        if (obj != null)
        {
            inventory = obj.GetComponent<PlayerInventory>();
        }
    }

    private void OnEnable()
    {
        RefreshAllSlots();

        // 인벤토리 상태 변경 시 상점 버튼 상태도 자동 갱신
        if(inventory != null)
        {
            inventory.OnInventoryChanged += RefreshAllSlots;
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshAllSlots;
        }
    }

    public void RefreshAllSlots()
    {
        if (shopSlots == null) return;

        foreach(var slot in shopSlots)
        {
            slot.RefreshSlot();
        }
    }
}
