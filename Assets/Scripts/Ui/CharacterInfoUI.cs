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
    Player_CC Player;

    public void OnPanel()
    {
        gameObject.SetActive(true);
        GameObject obj = GameObject.FindWithTag("Player");
        Player = obj.GetComponent<Player_CC>();
        UpdateWeaponUi(Player.ReturnWeapon());
        UpdateStatUi(Player.ReturnAtk(), Player.ReturnCurHp());
    }

    public void OffPanel()
    {
        gameObject.SetActive(false);
    }

    private void UpdateWeaponUi(GameObject Weapon)
    {
        if(Weapon == null)
        {
            WeaponIconImage.sprite = EmptyWeaponSprite;
            WeaponIconImage.color = new Color(1f, 1f, 1f, 0.02f);
            return;
        }

        int layerIndex = Weapon.layer;
        string layerName = LayerMask.LayerToName(layerIndex);
        
        Sprite weaponIcon = WeaponIcon.GetIconByLayerName(layerName);

        if(weaponIcon != null)
        {
            WeaponIconImage.sprite = weaponIcon;
            WeaponIconImage.color = new Color(1f, 1f, 1f, 1f);
        }
    }

    private void UpdateStatUi(float atk, float hp)
    {
        InfoAtkText.text = $"공격력: {atk}";
        InfoHpText.text = $"체력: {hp}";
    }
}
