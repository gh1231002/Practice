using UnityEngine;

public class ShopUi : MonoBehaviour
{
    ShopSlotUi[] shopSlots;
    PlayerInventory inventory;
    DebugManager debugManager;

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
    private void Start()
    {
        // 최조 OnEnable 시 매니저를 찾지 못했을 경우 다시 시도
        TrySubscribeDebugManager();
        // 실제 현재 상태를 기준으로 슬롯을 다시 갱신
        RefreshAllSlots();
    }

    private void OnEnable()
    {
        RefreshAllSlots();

        // 인벤토리 상태 변경 시 상점 버튼 상태도 자동 갱신
        if(inventory != null)
        {
            inventory.OnInventoryChanged += RefreshAllSlots;
        }

        // 디버그 언락 토글 변경도 감지
        TrySubscribeDebugManager();
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshAllSlots;
        }

        if(debugManager != null)
        {
            debugManager.WeaponUnlockDebugChanged -= OnWeaponUnlockDebugChanged;
            debugManager = null;
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

    private void TrySubscribeDebugManager()
    {
        // 이미 참조를 확보했다면 작동하지 않음
        if (debugManager != null) return;

        // 현재 존재하는 디버그매니저를 가져옴
        debugManager = DebugManager.Instance;

        // 이벤트를 구독한다.
        if(debugManager != null)
        {
            debugManager.WeaponUnlockDebugChanged += OnWeaponUnlockDebugChanged;
        }
    }
    private void OnWeaponUnlockDebugChanged(bool isOn)
    {
        RefreshAllSlots();
    }
}
