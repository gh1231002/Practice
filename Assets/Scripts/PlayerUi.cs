using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUi : MonoBehaviour
{
    [SerializeField] Slider HpBar;
    [SerializeField] TextMeshProUGUI HpText;
    Player_CC Player;

    private void Awake()
    {
        GameObject obj = GameObject.FindWithTag("Player");
        Player = obj.GetComponent<Player_CC>();
    }

    private void Start()
    {
        Player.ChangeHp += ChangeHpBar;
    }

    private void ChangeHpBar(float CurHp, float MaxHp)
    {
        HpBar.maxValue = MaxHp;
        HpBar.value = CurHp;
        HpText.text = $"{CurHp} / {MaxHp}";
    }
}
