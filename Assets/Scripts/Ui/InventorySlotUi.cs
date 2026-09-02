using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUi : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] Image ItemIcon;
    [SerializeField] TextMeshProUGUI QuantiyuText;
    [SerializeField] GameObject EquippedTag;

    [Header("드래그 연출용 변수")]
    Canvas parentCanvas;
    CanvasGroup canvasGroup;
    GameObject dragIconObject;

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

        // 드래그 레이어 계산 및 마우스 투과 처리를 위해 추가
        parentCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        // 없다면 컴포넌트 생성
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }
    /// <summary>
    /// 드래그 시작 시 마우스 위치에 임시 아이콘(잔상) 생성
    /// </summary>
    /// <param name="eventData"></param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 아이템이 없는 빈 슬롯은 드래그 안함
        if(CurrentItem == null) return;

        dragIconObject = new GameObject("DragIcon");
        dragIconObject.transform.SetParent(parentCanvas.transform, false);

        // 최상단 레이어로 이동
        dragIconObject.transform.SetAsLastSibling();

        Image dragImage = dragIconObject.AddComponent<Image>();
        dragImage.sprite = ItemIcon.sprite;
        // 드롭 감지(Drop Event)를 방해하지 않도록 설정
        dragImage.raycastTarget = false;
        // 마우스 신호가 슬롯을 투과하여 QuickSlotCellUI에 닿도록 설정
        canvasGroup.blocksRaycasts = false;
    }
    /// <summary>
    /// 드래그 중 장산 아이콘이 마우스 커서를 따라 이동
    /// </summary>
    /// <param name="eventData"></param>
    public void OnDrag(PointerEventData eventData)
    {
        if(dragIconObject != null)
        {
            dragIconObject.transform.position = eventData.position;
        }
    }
    /// <summary>
    /// 드래그 종료 시 잔상 아이콘을 삭제하고 투과 설정 원복
    /// </summary>
    /// <param name="eventData"></param>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIconObject != null) Destroy(dragIconObject);
        canvasGroup.blocksRaycasts = true;
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
            else if(CurrentItem is PotionData potionData)
            {
                // 체력이 이미 가득 차 있다면 사용 불가능
                if(player.ReturnCurHp() >= player.ReturnMaxHp())
                {
                    UiManager.Instance.StartNoticePanel("체력이 이미 가득 차 있습니다.");
                    return;
                }

                // 포션 회복 효과 실행
                potionData.UseItem(player);

                // 인벤토리 수량 1개 차감
                PlayerInventory inv = player.GetComponent<PlayerInventory>();
                if (inv != null)
                {
                    inv.ConsumeItem(SlotIndex, 1);
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
