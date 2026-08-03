using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Scriptable Objects/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("무기 정보")]
    [SerializeField] string WeaponName;
    [SerializeField] float WeaponAtk;
    [SerializeField] Vector3 TrsWeapon;
    [SerializeField] Vector3 RotWeapon;
    [SerializeField] GameObject ObjWeapon;

    public string weaponName => WeaponName;
    public float weaponAtk => WeaponAtk;
    public GameObject objWeapon => ObjWeapon;
    public Vector3 trsWeapon => TrsWeapon;
    public Vector3 rotWeapon => RotWeapon;
}
