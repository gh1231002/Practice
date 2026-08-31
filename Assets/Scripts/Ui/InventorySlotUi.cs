using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUi : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] Image ItemIcon;
    [SerializeField] TextMeshProUGUI QuantiyuText;
    [SerializeField] GameObject EquippedTag;

    public int SlotIndex {  get; private set; }
    public int CurrentAmount { get; private set; }
    public ItemData CurrentItem { get; private set; }

    bool isEmpty => CurrentItem == null;

    Player_CC player;

    private void Awake()
    {
        GameObject obj = GameObject.FindWithTag("Player");
        if (obj != null)
        {
            player = obj.GetComponent<Player_CC>();
        }
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        // 빈 슬롯이거나. 아이템이 없으면 리턴
        if (isEmpty || CurrentItem == null || player == null) return;

        // 마우스 좌클릭 더블클릭인 경우
        if (eventData.clickCount == 2 && eventData.button == PointerEventData.InputButton.Left)
        {
            // 패턴 매칭(is)를 활용해 현재 아이템이 무기 데이터인지 검사
            if(CurrentItem is WeaponData weaponData)
            {
                //플레이어 스크립트의 통합 무기 시스템 함수 호출
                EquipmentSystem equipSys = player.GetComponent<EquipmentSystem>();
                if (equipSys != null)
                {
                    equipSys.ToggleEquip(weaponData);
                }
            }
        }
    }

    public void InitSlot(int index)
    {
        SlotIndex = index;
        ClearSlot();
    }

    public void UpdateSlot(int amount, ItemData item, bool isEquipped = false)
    {
        CurrentAmount = amount;
        CurrentItem = item;

        if(amount > 0 && item != null)
        {
            ItemIcon.sprite = item.itemIcon;
            ItemIcon.gameObject.SetActive(true);

            //2개 이상일때만 수량 표시
            if(amount > 1)
            {
                QuantiyuText.text = amount.ToString();
                QuantiyuText.gameObject.SetActive(true);
            }
            else
            {
                QuantiyuText.gameObject.SetActive(false);
            }

            if(EquippedTag != null)
            {
                EquippedTag.SetActive(isEquipped);
            }
        }
        else
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        CurrentAmount = 0;
        CurrentItem = null;
        ItemIcon.sprite = null;
        ItemIcon.gameObject.SetActive(false);
        QuantiyuText.gameObject.SetActive(false);

        if(EquippedTag != null)
        {
            EquippedTag.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CurrentItem != null && ItemTooltipUi.instance != null)
        {
            ItemTooltipUi.instance.ShowTooltip(CurrentItem.itemName, CurrentItem.itemInfo);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(ItemTooltipUi.instance != null)
        {
            ItemTooltipUi.instance.HideTooltip();
        }
    }

    private void OnDisable()
    {
        // 인벤토리 창이 닫히거나 슬롯이 비활성화될 때 툴팁도 함꼐 숨김
        if(ItemTooltipUi.instance != null)
        {
            ItemTooltipUi.instance.HideTooltip();
        }
    }
}
