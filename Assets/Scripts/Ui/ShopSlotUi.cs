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
            BtnBuy.onClick.AddListener(BuyButtonClick);
        }
    }

    // 구매 버튼 클릭 시
    private void BuyButtonClick()
    {
        if (inventory == null || itemData == null) return;

        // 무기 아이템인 경우 실제 구매 직전에도 언락 조건 확인
        if(itemData is WeaponData && !CanPurchaseWeapon())
        {
            // 조건이 충족되지 않았다면 구매하지 않고 버튼 상태 갱신
            RefreshSlot();
            return;
        }

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

            // 무기 해금을 완료했거나 디버그모드로 언락했는지 확인
            if (!CanPurchaseWeapon())
            {
                BtnBuy.interactable = false;
                BtnBuyText.text = "구매불가";
                return;
            }
        }

        // 소비성 아이템이거나 미소지 무기인 경우 구매 가능
        BtnBuy.interactable = true;
        BtnBuyText.text = "구 매";
    }

    /// <summary>
    /// 현재 무기를 구매할 수 있는지 판단하는 함수
    /// </summary>
    /// <returns></returns>
    private bool CanPurchaseWeapon()
    {
        // 정상적인 게임 진행으로 무기 구매 조건을 해금했는지 확인
        bool questUnlock = player != null && player.ReturnWeaponUnlock();

        // 디버그 매니저가 존재하고 무기 언락 토글이 켜져 있는지 확인
        bool debugUnlock = DebugManager.Instance != null && DebugManager.Instance.WeaponUnlock;

        // 정상 해금 또는 디버그 해금 중 하나라도 만족하면 구매 가능
        return questUnlock || debugUnlock;
    }
}
