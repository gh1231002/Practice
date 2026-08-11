using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("무기 데이터")]
    [SerializeField] WeaponData Data;

    public WeaponData weaponData => Data;
}
