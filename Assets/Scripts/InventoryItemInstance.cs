using System;
using UnityEngine;

/// <summary>
/// 인벤토리에 보관되는 개별 아이템의 데이터를 관리하는 클래스
/// ItemData가 아이템의 종류를 나타낸다면
/// 이 클래스는 플레이어가 실제로 획득한 아이템 하나의 상태를 나타냅니다
/// </summary>
[Serializable]
public class InventoryItemInstance
{
    // 아이템의 종류를 나타내는 원본 데이터
    [SerializeField] ItemData itemData;

    // 해당 아이템의 수량
    [SerializeField] int amount;

    // 개별 아이템을 구분하기 위한 고유 ID
    [SerializeField] string instanceId;

    // 해당 무기 강화 단계
    // 일반 아이템은 기본값인 0
    [SerializeField] int enhanceLevel;

    // 다른 클래스에서 데이터를 읽을 수 있도록 프로퍼티 제공
    public ItemData ItemData => itemData;
    public int Amount => amount;
    public string InstanceId => instanceId;
    public int EnhanceLevel => enhanceLevel;

    // 강화 가능한 무기인지 확인
    public bool CanEnhance => itemData is WeaponData;

    // 최대 강화 단계
    public const int maxEnhanceLevel = 10;


    /// <summary>
    /// 새로운 아이템 인스턴스를 생성합니다
    /// 실제 아이템을 인벤토리에 추가할 떄 사용합니다
    /// </summary>
    public InventoryItemInstance(ItemData itemData, int amount)
    {
        // 유효하지 않은 아이템은 생성하지 않음
        if(itemData == null)
        {
            throw new ArgumentNullException(nameof(itemData));
        }
        if(amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "아이템 수량은 1 이상이어야 합니다.");
        }

        this.itemData = itemData;
        this.amount = amount;

        // 아이템이 생성될 때마다 서로 다른 고유 ID 발급
        instanceId = Guid.NewGuid().ToString("N");
        // 모든 신규 무기는 0부터 시작
        enhanceLevel = 0;
    }

    /// <summary>
    /// 아이템 수량을 변경합니다
    /// 포션이나 가오하 재료처럼 중첩되는 아이템에 사용합니다
    /// </summary>
    public void AddAmount(int value)
    {
        // 수량 변경 후 음수가 되지 않도록 최소 0으로 제한
        amount = Mathf.Max(0, amount + value);
    }

    /// <summary>
    /// 아이템 수량을 지정한 값으로 변경합니다.
    /// </summary>
    public void SetAmount(int value)
    {
        amount = Mathf.Max(0, value);
    }

    /// <summary>
    /// 무기의 강화 단계를 변경합니다.
    /// 무기가 아니거나 강화 단계가 유효하지 않으면 변경하지 않습니다.
    /// </summary>
    public bool SetEnhanceLevel(int level)
    {
        if (!CanEnhance)
        {
            return false;
        }

        if (level < 0 || level > maxEnhanceLevel)
        {
            return false;
        }

        enhanceLevel = level;
        return true;
    }
}
