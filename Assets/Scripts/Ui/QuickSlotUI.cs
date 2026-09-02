using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// InventorySlotUi가 없으면 자동 추가
[RequireComponent(typeof(InventorySlotUi))]
public class QuickSlotUI : MonoBehaviour, IDropHandler
{
    [SerializeField] TextMeshPro KeyText;

    public int QuickSlotIndex { get; private set; }
    public KeyCode HotKey { get; private set; }
    public ItemData RegisteredItem { get; private set; }

    InventorySlotUi TargetSlotUi;

    private void Awake()
    {
        TargetSlotUi = GetComponent<InventorySlotUi>();
    }
    /// <summary>
    /// 퀵슬롯 초기화
    /// </summary>
    /// <param name="index"></param>
    /// <param name="key"></param>
    public void InitQuickSlot(int index, KeyCode key)
    {
        QuickSlotIndex = index;
        HotKey = key;

        if(KeyText != null)
        {
            KeyText.text = (index + 1).ToString();
        }
        ClearQuickSlot();
    }

    public void ClearQuickSlot()
    {
        RegisteredItem = null;
        // 기존 InventorySlotUi의 비우기 기능 재사용
        TargetSlotUi.ClearSlot();
    }

    /// <summary>
    /// 드래그 앤 드롭으로 아이템이 올려졌을 때 실행
    /// </summary>
    /// <param name="eventData"></param>
    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUi draggedSlot = eventData.pointerDrag?.GetComponent<InventorySlotUi>();

        // 드래그해온 슬롯에 아이템이 들어있다면 등록 처리
        if(draggedSlot != null && draggedSlot.CurrentItem != null)
        {
            SetQuickSlotItem(draggedSlot.CurrentItem);
        }
    }

    public void SetQuickSlotItem(ItemData item)
    {
        RegisteredItem = item;
        RefreshUi();
    }
    /// <summary>
    /// 인벤토리 수량 변화에 따라 퀵슬롯 갱신
    /// </summary>
    public void RefreshUi()
    {
        if(RegisteredItem == null)
        {
            TargetSlotUi.ClearSlot();
            return;
        }

        // 인벤토리에서 실제 이 아이템을 몇개 가졌는지 확인
        int currentCount = PlayerInventory.Instance != null ?
            PlayerInventory.Instance.GetItemCount(RegisteredItem) : 0;

        TargetSlotUi.UpdateSlot(currentCount, RegisteredItem, false);
    }
}
