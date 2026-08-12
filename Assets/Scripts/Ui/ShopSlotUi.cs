using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopSlotUi : MonoBehaviour
{
    [Header("Ui 연결")]
    [SerializeField] ItemData itemData;
    [SerializeField] Button BtnBuy;
    [SerializeField] TextMeshProUGUI BtnBuyText;

    PlayerInventory inventory;
    Player_CC player;

    private void Awake()
    {
        GameObject obj = GameObject.FindWithTag("Player");
        if (obj != null)
        {
            inventory = obj.GetComponent<PlayerInventory>();
            player = obj.GetComponent<Player_CC>();
        }
        if(BtnBuy != null)
        {
            BtnBuy.onClick.AddListener(ButButtonClick);
        }
    }

    // 구매 버튼 클릭 시
    private void ButButtonClick()
    {
        if (inventory == null || itemData == null) return;
        // 구매 성공 notice panel 띄움
        UiManager.Instance.StartNoticePanel("구매 성공");
        // 인벤토리에 아이템 추가
        inventory.Additem(itemData, 1);
        // 구매 후 버튼 상태 재갱신 ( 무기 구매시 즉시 소유중으로 변경 )
        RefreshSlot();
    }

    // 슬롯 갱신 함수 (상점 창이 열리거나 인벤토리가 변경될 때)
    public void RefreshSlot()
    {
        if(itemData == null || inventory == null || player == null) return;

        // 무기 아이템인 경우 소지/장착 여부 확인
        if(itemData is WeaponData weaponData)
        {
            // 현재 장착 중인지
            bool isEquipped = (player.CurrentWeaponData == weaponData);
            // 인벤토리에 보관 중인지
            bool inInventory = inventory.HasItem(weaponData);

            if (isEquipped || inInventory)
            {
                // 버튼 잠금
                BtnBuy.interactable = false;
                BtnBuyText.text = isEquipped ? "장착중" : "보유중";
                return;
            }
        }

        // 소비성 아이템이거나 미소지 무기인 경우 구매 가능
        BtnBuy.interactable = true;
        BtnBuyText.text = "구 매";
    }
}
