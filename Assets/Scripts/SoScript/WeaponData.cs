using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Scriptable Objects/WeaponData")]
public class WeaponData : ItemData
{
    [Header("무기 정보")]
    [SerializeField] float WeaponAtk;
    [SerializeField] float AtkRadius;
    [SerializeField] Vector3 TrsWeapon;
    [SerializeField] Vector3 RotWeapon;
    [SerializeField] Vector3 AtkHalfBox;
    [SerializeField] GameObject ObjWeapon;

    public float weaponAtk => WeaponAtk;
    public float atkRadius => AtkRadius;
    public GameObject objWeapon => ObjWeapon;
    public Vector3 trsWeapon => TrsWeapon;
    public Vector3 rotWeapon => RotWeapon;
    public Vector3 atkHalfbox => AtkHalfBox;
}
