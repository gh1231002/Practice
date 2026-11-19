using System;
using UnityEngine;

public class EquipmentSystem : MonoBehaviour
{
    // 현재 장착 중인 무기 아이템 인스턴스
    public InventoryItemInstance CurrentWeaponInstance { get; private set; }

    // 기존 코드 호환성을 위한 WeaponData 프로퍼티
    public WeaponData CurrentWeapon => CurrentWeaponInstance?.ItemData as WeaponData;

    public bool IsWeaponUnlocked { get; private set; } = false;

    // (이전 무기 인스턴스, 새로 장착된 무기 인스턴스)를 전달하는 이벤트
    public event Action<InventoryItemInstance, InventoryItemInstance> OnWeaponEquippedChanged;

    /// <summary>
    /// 무기 해금 처리
    /// </summary>
    /// <param name="weapon"></param>
    public void UnlockWeapon(WeaponData weapon)
    {
        IsWeaponUnlocked = true;
    }

    /// <summary>
    /// 개별 무기 인스턴스 기준 장착/해제/교체 처리 함수
    /// </summary>
    /// <param name="clickedWeaponInstance"></param>
    public void ToggleEquip(InventoryItemInstance clickedWeaponInstance)
    {
        if (clickedWeaponInstance == null || !(clickedWeaponInstance.ItemData is WeaponData)) return;

        InventoryItemInstance previousWeaponInstance = CurrentWeaponInstance;

        // 이미 장착 중인 동일한 인스턴스를 다시 클릭한 경우 > 장착 해제
        if (CurrentWeaponInstance == clickedWeaponInstance)
        {
            CurrentWeaponInstance = null;
        }
        // 미장착 무기를 클릭하거나 다른 무기로 교체하는 경우 > 새로 장착
        else
        {
            CurrentWeaponInstance = clickedWeaponInstance;
        }

        // 변경 사실을 알림 (개별 인스턴스 기준)
        OnWeaponEquippedChanged?.Invoke(previousWeaponInstance, CurrentWeaponInstance);
    }

    /// <summary>
    /// 기존 WeaponData 호출부를 지원하기 위한 호환 오버로드
    /// </summary>
    /// <param name="clickedWeapon"></param>
    public void ToggleEquip(WeaponData clickedWeapon)
    {
        if (clickedWeapon == null) return;

        // 현재 이미 이 WeaponData의 인스턴스를 장착 중이라면 해제
        if (CurrentWeapon == clickedWeapon)
        {
            ToggleEquip(CurrentWeaponInstance);
            return;
        }

        // 인벤토리에서 해당 WeaponData를 사용하는 첫 번째 인스턴스를 찾아 장착
        var inventory = GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            for (int i = 0; i < inventory.GetMaxCapacity(); i++)
            {
                var slot = inventory.Getslot(i);
                if (slot != null && !slot.IsEmpty && slot.ItemInstance != null && slot.itemData == clickedWeapon)
                {
                    ToggleEquip(slot.ItemInstance);
                    return;
                }
            }
        }
    }
}
