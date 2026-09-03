using System;
using Unity.VisualScripting;
using UnityEngine;

//인벤토리 한 칸의 데이터를 관리할 헬퍼 클래스
[Serializable]
public class InventorySlot
{
    [SerializeField] ItemData ItemData;
    [SerializeField] int Amount;
    //칸이 비어있는지 확인하는 프로퍼티
    public bool IsEmpty => ItemData == null;
    public ItemData itemData => ItemData;
    public int amount => Amount;

    //슬롯 초기화
    public void Clear()
    {
        ItemData = null;
        Amount = 0;
    }
    //아이템 갯수 추가
    public void AddAmount(int value)
    {
        Amount += value;
    }
    //처음 빈 슬롯에 새로운 아이템을 할당할 때 사용할 함수
    public void SetItem(ItemData newItem, int newAmount)
    {
        ItemData = newItem;
        Amount = newAmount;
    }
}

public class PlayerInventory : MonoBehaviour
{
    [Header("인벤토리 세팅")]
    [SerializeField] int MaxCapacity;
    [SerializeField] InventorySlot[] Slots;

    public static PlayerInventory Instance;

    //인벤토리에 변화가 생겼을 때 ui에 알리기 위한 이벤트
    public event Action OnInventoryChanged;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

            //게임 시작시 지정된 용량만큼 슬롯 배렬 초기화
            Slots = new InventorySlot[MaxCapacity];
        for(int i = 0; i < MaxCapacity; i++)
        {
            Slots[i] = new InventorySlot();
        }
    }

    //외부에서 인벤토리 최대 용량을 가져갈 수 있게 하는 함수
    public int GetMaxCapacity()
    {
        return MaxCapacity;
    }

    //특정 인덱스의 슬롯 데이터를 반환하는 함수
    public InventorySlot Getslot(int index)
    {
        if(index < 0 || index >= Slots.Length || Slots == null) return null;
        return Slots[index];
    }

    //아이템 획득 로직
    public void Additem(ItemData item, int amount = 1)
    {
        //아이템이 중첩가능한 타입인지 먼저 확인
        if(item.isStackable)
        {
            //이미 같은 아이템이 인벤토리에 있는지 확인하고, 있다면 갯수 누적
            for (int i = 0; i < MaxCapacity; i++)
            {
                if (!Slots[i].IsEmpty && Slots[i].itemData == item)
                {
                    Slots[i].AddAmount(amount);
                    OnInventoryChanged?.Invoke();// ui 갱신 요청
                    return;
                }
            }
        }

        //중첩이 불가능한 아이템이거나
        //중첩 가능하지만 인벤토리에 처음 들어온 아이템인 경우 무조건 빈 칸을 찾음
        for(int i = 0;i < MaxCapacity; i++)
        {
            if (Slots[i].IsEmpty)
            {
                Slots[i].SetItem(item,amount);
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        //빈 칸도 없다면 가득 찬 상태
        UiManager.Instance.StartNoticePanel("인벤토리가 가득 찼습니다.");
    }

    // 인벤토리에 특정 itemdata가 존재하는지 체크
    public bool HasItem(ItemData item)
    {
        for(int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i] != null && !Slots[i].IsEmpty)
            {
                if (Slots[i].itemData == item)
                {
                    return true; // 소지중
                }
            }
        }
        return false; // 미소지
    }
    /// <summary>
    /// 지정한 슬롯의 아이템 수량을 차감하고 0개가 되면 슬롯을 비움
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <param name="amount"></param>
    public void ConsumeItem(int slotIndex, int amount = 1)
    {
        // 지정한 슬롯번호가 0보다 작거나
        // 인벤토리 총 슬롯보다 크거나
        // 해당 슬롯이 비어있다면 return
        if (slotIndex < 0 || slotIndex >= Slots.Length || Slots[slotIndex].IsEmpty) return;

        Slots[slotIndex].AddAmount(-amount);

        // 수량이 0 이하가 되면 슬롯 비우기
        if (Slots[slotIndex].amount <= 0)
        {
            Slots[slotIndex].Clear();
        }

        // 인벤토리 UI 갱신 이벤트 호출
        OnInventoryChanged?.Invoke();
    }
    /// <summary>
    /// 인벤토리 전체에서 특정 아이템의 총 보유 수량을 반환하는 함수
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public int GetItemCount(ItemData item)
    {
        if (item == null) return 0;

        int total = 0;
        
        for(int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i] != null && !Slots[i].IsEmpty && Slots[i].itemData == item)
            {
                total += Slots[i].amount;
            }
        }
        return total;
    }

    /// <summary>
    /// 특정 ItemData를 소지한 첫 번째 슬롯을 찾아 수량을 차감하는 함수
    /// (퀵슬롯 사용 시 호출)
    /// </summary>
    /// <param name="item"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public bool ConsumeItem(ItemData item, int amount = 1)
    {
        if(item == null) return false;

        for(int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i] != null && !Slots[i].IsEmpty && Slots[i].itemData == item)
            {
                ConsumeItem(i, amount);
                return true;
            }
        }
        
        // 해당 아이템이 인벤토리에 없음
        return false;
    }
}
