using System;
using UnityEngine;

public class EquipmentSystem : MonoBehaviour
{
    public WeaponData CurrentWeapon {  get; private set; }
    public bool IsWeaponUnlocked { get; private set; } = false;

    // (이전 무기 데이터, 새로 장착된 무기 데이터)를 전달하는 이벤트
    public event Action<WeaponData, WeaponData> OnWeaponEquippedChanged;

    /// <summary>
    /// 무기 해금 처리
    /// </summary>
    /// <param name="weapon"></param>
    public void UnlockWeapon(WeaponData weapon)
    {
        IsWeaponUnlocked = true;

    }

    // 장착/해제/교체를 처리하는 함수
    public void ToggleEquip(WeaponData clickedWeapon)
    {
        if(clickedWeapon == null) return;

        WeaponData previousWeapon = CurrentWeapon;

        // 이미 장착 중인 무기를 다시 더블클릭한 경우 > 장착해제
        if(CurrentWeapon == clickedWeapon)
        {
            CurrentWeapon = null;
        }
        // 미장착 무기를 더블클릭하거나, 다른 무기로 교체하는 경우 > 새로장착
        else
        {
            CurrentWeapon = clickedWeapon;
        }

        // 변경 사실을 알림
        OnWeaponEquippedChanged?.Invoke(previousWeapon, CurrentWeapon);
    }
}
