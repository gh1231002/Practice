using UnityEngine;
using UnityEngine.InputSystem;

public class QuickSlotManager : MonoBehaviour
{
    public static QuickSlotManager Instance { get; private set; }

    [Header("퀵슬롯 배열")]
    [SerializeField] QuickSlotUI[] quickSlotUIs;

    [Header("Input System 액션 참조")]
    [SerializeField] InputActionProperty[] quickSlotProperty;

    Player_CC player;

    private void Awake()
    {
        // 싱글톤 초기화
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 플레이어 가져오기
        GameObject obj = GameObject.FindWithTag("Player");
        if(obj != null)
        {
            player = obj.GetComponent<Player_CC>();
        }
    }

    private void Start()
    {
        // 퀵슬롯 초기화 (인덱스 및 단축키 매핑)
        for(int i = 0; i < quickSlotUIs.Length; i++)
        {
            if(quickSlotUIs[i] != null)
            {
                quickSlotUIs[i].InitQuickSlot(i);
            }
        }
    }

    private void OnEnable()
    {
        for(int i = 0; i < quickSlotProperty.Length; i++)
        {
            quickSlotProperty[i].action?.Enable();
        }

        // 인벤토리 변화 이벤트 구독 (인벤토리 수량이 바뀌면 퀵슬롯 UI도 자동 갱신)
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged += RefreshAllQuickSlots;
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < quickSlotProperty.Length; i++)
        {
            quickSlotProperty[i].action?.Disable();
        }

        // 이벤트 해제 (메모리 누수 방지)
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged -= RefreshAllQuickSlots;
        }
    }

    private void Update()
    {
        // UI 창이 하나라도 열려있다면 단축키 입력감지 안함
        if (UiManager.Instance != null && UiManager.Instance.HasOpenPanel) return;

        // 단축키 입력 감지
        for(int i = 0; i < quickSlotProperty.Length; i++)
        {
            if (quickSlotProperty[i].action != null && quickSlotProperty[i].action.WasPressedThisFrame())
            {
                UseQuickSlotItem(i);
            }
        }
    }

    /// <summary>
    /// 퀵슬롯 단축키 입력시 아이템 사용
    /// </summary>
    /// <param name="index"></param>
    private void UseQuickSlotItem(int index)
    {
        // index 값이 0보다 작거나 퀵슬롯 개수보다 큰 값이면 작동 안함
        if (index < 0 || index >= quickSlotUIs.Length) return;

        QuickSlotUI cell = quickSlotUIs[index];
        ItemData item = cell.RegisteredItem;

        // 등록된 아이템이 없는 경우 리턴
        if (item == null)
        {
            UiManager.Instance.StartNoticePanel("아이템을 등록하세요.");
            return;
        }

        // 포션 아이템 사용 시 체력 조건검사
        if(item is PotionData potion)
        {
            if(player != null && player.ReturnCurHp() >= player.ReturnMaxHp())
            {
                UiManager.Instance.StartNoticePanel("체력이 이미 가득 차 있습니다.");
                return;
            }

            // 포션 효과 실행
            potion.UseItem(player);

            // 인벤토리 수량 1개 차감
            PlayerInventory.Instance.ConsumeItem(item, 1);
        }
    }

    /// <summary>
    /// 모든 퀵슬롯 UI를 갱신
    /// </summary>
    public void RefreshAllQuickSlots()
    {
        for(int i = 0; i < quickSlotUIs.Length; i++)
        {
            if(quickSlotUIs[i] != null)
            {
                quickSlotUIs[i].RefreshUi();
            }
        }
    }
}
