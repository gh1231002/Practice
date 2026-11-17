using UnityEngine;

public class WorldDropItem : MonoBehaviour
{
    [SerializeField] LayerMask groundLayer;
    [SerializeField] SphereCollider pickupTrigger;

    // 아이템 정보
    ItemData itemData;
    int dropAmount;

    Rigidbody rb;
    // 아이템이 지면에 착지했는지 확인하는 변수
    bool isLanded;

    // 획득 처리가 이미 진행 중인지 확인하는 변수
    // 여러 콜라이더가 동시에 접근해도 중복 지급되지 않도록 한다
    bool isCollecting;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // 착지하기 전에는 플레이어가 아이템을 획득할 수 없도록 한다
        if(pickupTrigger != null)
        {
            pickupTrigger.enabled = false;
        }
    }

    /// <summary>
    /// 몬스터가 아이템을 생성할 때 아이템 정보와 수량을 전달한다
    /// </summary>
    /// <param name="data"></param>
    /// <param name="amount"></param>
    public void Initialize(ItemData data, int amount)
    {
        itemData = data;
        dropAmount = amount;
    }

    void OnCollisionEnter(Collision collision)
    {
        // 이미 착지했다면 중복 처리 X
        if(isLanded) return;

        // 충돌한 오브젝트가 지면 레이어인지 확인한다
        int ohterLayerMask = 1 << collision.gameObject.layer;
        if((groundLayer.value & ohterLayerMask) == 0) return;

        // 지면에 착지한 상태로 변경한다
        isLanded = true;
        // 착지 후에는 더이상 물리적으로 움직이지 않도록 정지
        if(rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // 착지한 이후부터 플레이어의 접근을 감지한다
        if(pickupTrigger != null)
        {
            pickupTrigger.enabled = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 착지 전이거나 이미 획득 처리가 진행 중이면 무시한다
        if(!isLanded || isCollecting) return;

        // 충돌한 오브젝트 또는 상위 오브젝트에서 인벤토리를 찾는다
        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();

        // 플레이어 인벤토리가 아니라면 무시
        if(inventory == null) return;
        // 잘못된 아이템 정보가 전달되었다면 처리하지않는다
        if(itemData == null || dropAmount <= 0) return;
        // 중복 획득을 방지하기 위해 처리 상태로 변경
        isCollecting = true;

        // 인벤토리에 아이템 지급을 시도한다
        bool success = inventory.Additem(itemData, dropAmount);
        if(success)
        {
            Destroy(gameObject);
        }
        else
        {
            // 지급에 실패했다면 아이템을 유지한다.
            if(UiManager.Instance != null)
            {
                UiManager.Instance.StartNoticePanel("인벤토리 공간이 부족합니다.");
            }

            // 다음 접근 시 다시 획득을 시도할 수 있도록 상태를 복구한다.
            isCollecting = false;
        }
    }
}
