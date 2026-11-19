using System;
using UnityEngine;

//인벤토리 한 칸의 데이터를 관리할 헬퍼 클래스
[Serializable]
public class InventorySlot
{
    // 실제로 획득한 개별 아이템 데이터를 보관
    [SerializeReference] InventoryItemInstance itemInstance;

    //칸이 비어있는지 확인하는 프로퍼티
    public bool IsEmpty => itemInstance == null
                        || itemInstance.ItemData == null
                        || itemInstance.Amount <= 0;
    // 개별 아이템 데이터 접근
    public InventoryItemInstance ItemInstance => itemInstance;

    // 기존 코드와의 호환성을 위해 유지하는 프로퍼티
    public ItemData itemData => ItemInstance?.ItemData;
    public int amount => ItemInstance?.Amount ?? 0;

    //슬롯 초기화
    public void Clear()
    {
        itemInstance = null;
    }

    //아이템 수량을 추가하거나 차감하는 함수
    public void AddAmount(int value)
    {
        // 비어 있는 슬롯에는 수량을 변경할 아이템이 없다.
        if (ItemInstance == null) return;

        // 실제 수량 변경은 개별 아이템 데이터에 위임한다.
        ItemInstance.AddAmount(value);
    }
    
    // 빈 슬롯에 새로운 아이템을 할당하는 함수
    public void SetItem(ItemData newItem, int newAmount)
    {
        // 유효하지 않은 아이템이나 수량은 할당하지 않는다.
        if (newItem == null || newAmount <= 0) return;

        // 새로운 아이템 인스턴스를 생성한다.
        // 이때 고유 ID가 발급되고 강화 단계는 +0으로 시작한다.
        itemInstance = new InventoryItemInstance(newItem, newAmount);
    }
}

public class PlayerInventory : MonoBehaviour
{
    [Header("인벤토리 세팅")]
    [SerializeField] int MaxStackAmount= 99;
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
            return;
        }

        //게임 시작시 지정된 용량만큼 슬롯 배렬 초기화
        Slots = new InventorySlot[MaxCapacity];
        for(int i = 0; i < MaxCapacity; i++)
        {
            Slots[i] = new InventorySlot();
        };
    }

    /// <summary>
    /// 외부에서 인벤토리 최대 용량을 가져갈 수 있게 하는 함수
    /// </summary>
    public int GetMaxCapacity()
    {
        return MaxCapacity;
    }

    /// <summary>
    /// 특정 인덱스의 슬롯 데이터를 반환하는 함수
    /// </summary>
    public InventorySlot Getslot(int index)
    {
        if(Slots == null || index < 0 || index >= Slots.Length) return null;
        return Slots[index];
    }

    /// <summary>
    /// 아이템 획득 로직
    /// </summary>
    public bool Additem(ItemData item, int amount = 1)
    {
        return Additem(item, amount, out _);
    }

    /// <summary>
    /// 아이템 획득 로직 (생성/할당된 첫 번째 아이템 인스턴스를 out으로 반환)
    /// </summary>
    public bool Additem(ItemData item, int amount, out InventoryItemInstance createdInstance)
    {
        createdInstance = null;

        // 잘못된 아이템이나 수량이 전달되면 지급하지 않는다
        if(item == null || amount <= 0)
        {
            Debug.LogWarning("[PlayerInventory] 유효하지 않은 아이템 또는 수량입니다.");
            return false;
        }
        // 인벤토리 슬롯이 초기화되지 않았다면 지급할 수 없다
        if(Slots == null || Slots.Length == 0)
        {
            Debug.LogWarning("[PlayerInventory] 인벤토리 슬롯이 초기화되지 않았습니다.");
            return false;
        }
        // 슬롯당 최대 중첩 수량이 0 이하라면 정상적인 지급이 불가능하다.
        if (item.isStackable && MaxStackAmount <= 0)
        {
            Debug.LogWarning("[PlayerInventory] 최대 중첩 수량이 올바르지 않습니다.");
            return false;
        }

        // 비중첩 아이템은 슬롯당 1개
        // 중첩 가능 아이템은 설정된 최대 수량까지 들어갈 수 있다
        int stackLimit = item.isStackable ? MaxStackAmount : 1;
        // 지급할 수량을 아직 변경하지 않고 별도로 관리
        int remainingAmount = amount;

        // 1. 전체 수량을 수용할 수 있는지 사전검사
        // 아이템이 중첩가능하다면 기존 동일 아이템 슬롯의 여유 공간을 확인
        if(item.isStackable)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                // 초기화되지 않은 슬롯은 건너뛴다
                if(Slots[i] == null) continue;
                // 비어 있거나 다른 아이템이라면 확인할 필요 없다
                if(Slots[i].IsEmpty || Slots[i].itemData != item) continue;

                // 현재 슬롯에서 추가로 보관할 수 있는 수량을 계산
                int availableAmount = stackLimit - Slots[i].amount;
                // 음수 또는 0이라면 더 이상 추가할 공간이 없다
                if(availableAmount <= 0) continue;
                // 기존 슬롯의 여유 공간만큼 지급 예정 수량에서 차감
                remainingAmount -= Mathf.Min(remainingAmount, availableAmount);
                // 모든 수량을 기존 슬롯에 보관할 수 있다면 검사 종료
                if(remainingAmount <= 0) break;
            }
        }

        // 기존 슬롯만으로 부족하다면 빈 슬롯이 얼마나 필요한지 확인한다
        if(remainingAmount > 0 )
        {
            int emptySlotCount = 0;
            // 사용 가능한 빈 슬롯 개수를 확인한다
            for(int i = 0; i < Slots.Length; i++)
            {
                // null이거나 비어 있는 슬롯을 사용할 수 있는 슬롯으로 취급한다
                if(Slots[i] == null || Slots[i].IsEmpty)
                {
                    emptySlotCount++;
                }
            }

            // 빈 슬롯 하나에 들어갈 수 있는 수량을 기준으로 필요한 슬롯 수를 계산한다
            int requiredSlotCount = Mathf.CeilToInt((float)remainingAmount / stackLimit);

            // 필요한 슬롯보다 빈 슬롯이 적다면 전체 지급을 취소한다
            // 알림은 AddItem()을 호출한 함수에서 처리한다
            if(emptySlotCount < requiredSlotCount)
            {
                return false;
            }
        }

        // 2. 사전검사에 성공했으므로 실제 아이템 지급
        remainingAmount = amount;
        // 중첩 가능한 아이템은 기존 동일 아이템 슬롯부터 채운다
        if(item.isStackable)
        {
            for(int i = 0; i < Slots.Length; i++)
            {
                if(Slots[i] == null) continue;
                // 같은 아이템이 들어 있는 슬롯만 처리한다
                if(Slots[i].IsEmpty || Slots[i].itemData != item) continue;
                // 기존 슬롯에 추가할 수 있는 수량을 계산한다
                int availableAmount = stackLimit - Slots[i].amount;
                if(availableAmount <= 0) continue;

                // 남은 수량과 슬롯의 여유 공간 중 작은 값을 추가한다
                int addAmount = Mathf.Min(remainingAmount, availableAmount);
                Slots[i].AddAmount(addAmount);
                if (createdInstance == null)
                {
                    createdInstance = Slots[i].ItemInstance;
                }
                // 실제 추가한 수량만큼 남은 수량을 줄인다
                remainingAmount -= addAmount;

                // 모든 수량을 지급했다면 기존 슬롯 처리를 종료한다
                if(remainingAmount <= 0) break;
            }
        }
        // 남은 수량을 빈 슬롯에 배치한다
        for(int i = 0; i < Slots.Length; i++)
        {
            // 더 지급할 수량이 없다면 종료한다
            if(remainingAmount <= 0) break;
            // null 슬롯은 새 슬롯 객체로 초기화한다
            if(Slots[i] == null)
            {
                Slots[i] = new InventorySlot();
            }
            // 이미 아이템이 들어 있는 슬롯은 건너뛴다
            if(!Slots[i].IsEmpty) continue;

            // 비중첩 아이템은 1개씩
            // 중첩 가능한 아이템은 최대 제한 수량만큼 배치한다
            int addAmount = Mathf.Min(remainingAmount, stackLimit);
            Slots[i].SetItem(item, addAmount);
            if (createdInstance == null)
            {
                createdInstance = Slots[i].ItemInstance;
            }
            remainingAmount -= addAmount;
        }

        // 모든 지급이 끝난 뒤 UI 갱신을 한번 요청
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 인벤토리에 특정 itemdata가 존재하는지 체크
    /// </summary>
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
    public void ConsumeItem(int slotIndex, int amount = 1)
    {
        // 인벤토리 배열이 초기화되지 않았거나 차감 수량이 잘못되었다면 종료
        if (Slots == null || amount <= 0) return;

        // 슬롯 번호가 유효한 범위를 벗어나면 종료
        if (slotIndex < 0 || slotIndex >= Slots.Length) return;

        // 해당 슬롯 객체가 존재하지 않거나 비어 있다면 종료
        if (Slots[slotIndex] == null || Slots[slotIndex].IsEmpty) return;

        // 수량만큼 차감
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

    public InventoryItemInstance GetItemInstance(int index)
    {
       InventorySlot slot = Getslot(index);
       
       // 슬롯이 없거나 비어 있다면 null 반환
       if(slot == null || slot.IsEmpty) return null;
       return slot.ItemInstance;
    }

    /// <summary>
    /// 인벤토리 전체에서 특정 ItemData를 지정 수량만큼 차감합니다.
    /// (보유량이 부족하면 차감하지 않고 false 반환)
    /// </summary>
    public bool ConsumeItemTotal(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return false;

        // 먼저 총 보유량 확인
        if (GetItemCount(item) < amount)
        {
            return false;
        }

        int remainingToConsume = amount;
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i] != null && !Slots[i].IsEmpty && Slots[i].itemData == item)
            {
                int consumeAmount = Mathf.Min(remainingToConsume, Slots[i].amount);
                Slots[i].AddAmount(-consumeAmount);
                remainingToConsume -= consumeAmount;

                if (Slots[i].amount <= 0)
                {
                    Slots[i].Clear();
                }

                if (remainingToConsume <= 0) break;
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 특정 인스턴스가 여전히 인벤토리에 존재하는지 확인
    /// </summary>
    public bool ContainsInstance(InventoryItemInstance instance)
    {
        if (instance == null || Slots == null) return false;
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i] != null && !Slots[i].IsEmpty && Slots[i].ItemInstance == instance)
            {
                return true;
            }
        }
        return false;
    }
}
