using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUi : MonoBehaviour
{
    [SerializeField] GameObject SlotPrefab;
    [SerializeField] Transform SlotGroup;

    List<InventorySlotUi> slotList = new List<InventorySlotUi>();

    PlayerInventory Inventory;
    Player_CC Player;
    EquipmentSystem equipSys;

    private void Awake()
    {
        GameObject obj = GameObject.FindWithTag("Player");
        if (obj != null)
        {
            Inventory = obj.GetComponent<PlayerInventory>();
            Player = obj.GetComponent<Player_CC>();
            equipSys = obj.GetComponent<EquipmentSystem>();
        }

        SetSlot();
    }

    private void OnEnable()
    {
        if(Inventory != null)
        {
            Inventory.OnInventoryChanged -= RefreshInventoryUi;
            Inventory.OnInventoryChanged += RefreshInventoryUi;
        }
        if(equipSys != null)
        {
            equipSys.OnWeaponEquippedChanged -= EquipChanged;
            equipSys.OnWeaponEquippedChanged += EquipChanged;
        }
        //인벤토리 창이 켜질 때마다 최신 상태로 새로고침
        RefreshInventoryUi();
    }
    private void OnDisable()
    {
        //메모리 누수 방지를 위해 오브젝트 파괴 시 이벤트 구독 해제
        if(Inventory != null)
        {
            Inventory.OnInventoryChanged -= RefreshInventoryUi;
        }
        if(equipSys != null)
        {
            equipSys.OnWeaponEquippedChanged -= EquipChanged;
        }
    }

    private void SetSlot()
    {
        if (Inventory == null) return;
        int count = Inventory.GetMaxCapacity();
        for(int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(SlotPrefab, SlotGroup);

            InventorySlotUi slotUi = obj.GetComponent<InventorySlotUi>();
            slotUi.InitSlot(i);

            slotList.Add(slotUi);
        }
    }
    private void EquipChanged(InventoryItemInstance oldWeapon, InventoryItemInstance newWeapon)
    {
        RefreshInventoryUi();
    }

    //화면 새로고침 함수
    public void RefreshInventoryUi()
    {
        if (Inventory == null) return;

        //플레이어 현재 장착 무기 인스턴스 가져옴
        InventoryItemInstance equippedInstance = equipSys != null ? equipSys.CurrentWeaponInstance : null;

        //생성된 ui 슬롯 갯수만큼 반복하며 데이터를 채워 넣습니다.
        for(int i = 0; i < slotList.Count; i++)
        {
            //playerinventory에서 해당 슬롯의 데이터를 가져옴
            var slotData = Inventory.Getslot(i);

            //데이터가 존재하고, 비어있지 않다면
            if(slotData != null && !slotData.IsEmpty && slotData.ItemInstance != null)
            {
                bool isEquipped = equippedInstance != null && slotData.ItemInstance == equippedInstance;
                slotList[i].UpdateSlot(slotData.amount, slotData.itemData, isEquipped, slotData.ItemInstance);
            }
            else
            {
                //데이터가 비어있다면 슬롯 화면서 비워줌
                slotList[i].ClearSlot();
            }
        }
    }
}
