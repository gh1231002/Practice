using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoUI : MonoBehaviour
{
    [SerializeField] Sprite EmptyWeaponSprite;
    [SerializeField] WeaponIconData WeaponIcon;
    [SerializeField] Image WeaponIconImage;
    [SerializeField] TextMeshProUGUI InfoAtkText;
    [SerializeField] TextMeshProUGUI InfoHpText;
    [SerializeField] TextMeshProUGUI InfoDefText;
    Player_CC Player;

    public void SettingStatPanel()
    {
        GameObject obj = GameObject.FindWithTag("Player");
        Player = obj.GetComponent<Player_CC>();
        UpdateWeaponUi(Player.ReturnWeapon());
        var stats = Player.ReturnPlayerStats();
        UpdateStatUi(stats.atkPower,stats.weaponAtk, stats.maxHp, stats.playerDef);
    }

    private void UpdateWeaponUi(GameObject Weapon)
    {
        WeaponData currentWeaponData = null;
        if (Weapon == null)
        {
            WeaponIconImage.sprite = EmptyWeaponSprite;
            WeaponIconImage.color = new Color(1f, 1f, 1f, 0.02f);
            return;
        }
        else
        {
            WeaponController weaponCtrl = Weapon.GetComponent<WeaponController>();
            if (weaponCtrl != null)
            {
                currentWeaponData = weaponCtrl.weaponData;
            }
        }
        WeaponIconImage.sprite = currentWeaponData.itemIcon;
        WeaponIconImage.color = new Color(1f, 1f, 1f, 1f);

        //int layerIndex = Weapon.layer;
        //string layerName = LayerMask.LayerToName(layerIndex);
        
        //Sprite weaponIcon = WeaponIcon.GetIconByLayerName(layerName);
        
        //if(weaponIcon != null)
        //{
        //    WeaponIconImage.sprite = weaponIcon;
        //    WeaponIconImage.color = new Color(1f, 1f, 1f, 1f);
        //}
    }

    private void UpdateStatUi(float playeratk, float weaponatk, float maxhp, float playerdef)
    {
        float finalAtk = playeratk + weaponatk;
        InfoAtkText.text = $"공격력: {finalAtk}";
        InfoDefText.text = $"방어력: {playerdef}";
        InfoHpText.text = $"최대 체력: {maxhp}";
    }
}
