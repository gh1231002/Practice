using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("무기 데이터")]
    [SerializeField] WeaponData Data;
    [Header("공격 판정 지점")]
    [SerializeField] Transform AtkPoint;

    public WeaponData weaponData => Data;
    public Transform atkPoint => AtkPoint;
}
