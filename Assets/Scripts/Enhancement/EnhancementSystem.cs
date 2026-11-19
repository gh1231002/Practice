using System;
using UnityEngine;

/// <summary>
/// 무기 강화 시도 후 반환되는 결과 유형입니다.
/// </summary>
public enum EnhancementResultType
{
    Success,            // 강화 성공 (+1 증가)
    Failed,             // 강화 실패 (단계 유지, 하락/파괴 없음)
    MaxLevelReached,    // 이미 최대 강화 단계(+10)에 도달하여 추가 강화 불가
    NotEnoughMaterials, // 강화석 등 필요 재료 수량이 부족함
    InvalidItem,        // 대상 아이템이 무기가 아니거나 null임
    InventoryMissing    // 인벤토리에서 대상 무기를 찾을 수 없음
}

/// <summary>
/// 강화 실행 후의 세부 정보를 담는 구조체입니다.
/// </summary>
public struct EnhancementResult
{
    public EnhancementResultType resultType;  // 결과 유형
    public int previousLevel;                 // 강화 시도 전 단계
    public int currentLevel;                  // 강화 시도 후 단계
    public int consumedMaterialAmount;        // 소모된 재료 개수
    public float successRate;                 // 적용된 성공 확률 (%)
}

/// <summary>
/// 무기 강화의 핵심 연산과 성공/실패 판정 및 스탯 반영을 담당하는 시스템 싱글톤 클래스입니다.
/// </summary>
public class EnhancementSystem : MonoBehaviour
{
    public static EnhancementSystem Instance { get; private set; }

    [Header("기본 강화 설정 에셋")]
    [Tooltip("씬 시작 시 기본으로 사용할 강화 설정 ScriptableObject 에셋입니다.")]
    [SerializeField] EnhancementData defaultEnhancementData;

    public EnhancementData DefaultData => defaultEnhancementData;

    /// <summary>
    /// 강화가 실행되었을 때 결과를 외부(UI, 사운드, 이펙트 등)에 알리는 이벤트입니다.
    /// (결과 구조체, 대상 무기 인스턴스)
    /// </summary>
    public event Action<EnhancementResult, InventoryItemInstance> OnEnhanceExecuted;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 지정한 무기 인스턴스의 강화를 1회 시도합니다.
    /// 재료 검사 -> 재료 소모(성공/실패 모두) -> 확률 판정 -> 수치 갱신 -> 장착 공격력 실시간 반영 순서로 진행됩니다.
    /// </summary>
    /// <param name="weaponInstance">강화 대상 개별 무기 인스턴스</param>
    /// <param name="data">적용할 강화 데이터 (null일 경우 defaultEnhancementData 사용)</param>
    /// <returns>강화 결과 구조체</returns>
    public EnhancementResult TryEnhance(InventoryItemInstance weaponInstance, EnhancementData data = null)
    {
        EnhancementResult result = new EnhancementResult();

        // 1. 강화 데이터 유효성 확인
        if (data == null)
        {
            data = defaultEnhancementData;
        }

        if (data == null)
        {
            Debug.LogError("[EnhancementSystem] EnhancementData가 설정되지 않았습니다.");
            result.resultType = EnhancementResultType.InvalidItem;
            return result;
        }

        // 2. 대상 무기 유효성 확인 (null 체크 및 무기 데이터 여부 확인)
        if (weaponInstance == null || !weaponInstance.CanEnhance)
        {
            result.resultType = EnhancementResultType.InvalidItem;
            return result;
        }

        result.previousLevel = weaponInstance.EnhanceLevel;
        result.currentLevel = weaponInstance.EnhanceLevel;

        // 3. 최대 강화 단계(+10) 초과 여부 검사
        if (weaponInstance.EnhanceLevel >= data.MaxEnhanceLevel)
        {
            result.resultType = EnhancementResultType.MaxLevelReached;
            OnEnhanceExecuted?.Invoke(result, weaponInstance);
            return result;
        }

        // 4. 인벤토리 접근 및 대상 무기 실소지 여부 검증
        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) inventory = playerObj.GetComponent<PlayerInventory>();
        }

        if (inventory == null || !inventory.ContainsInstance(weaponInstance))
        {
            result.resultType = EnhancementResultType.InventoryMissing;
            OnEnhanceExecuted?.Invoke(result, weaponInstance);
            return result;
        }

        // 5. 강화 재료 수량 검사 및 사전 소모
        int requiredCost = data.GetMaterialCost(weaponInstance.EnhanceLevel);
        result.consumedMaterialAmount = requiredCost;
        ItemData materialItem = data.MaterialItem;

        if (materialItem != null)
        {
            int currentMaterialCount = inventory.GetItemCount(materialItem);
            // 보유 재료가 부족하면 진행하지 않고 중단
            if (currentMaterialCount < requiredCost)
            {
                result.resultType = EnhancementResultType.NotEnoughMaterials;
                OnEnhanceExecuted?.Invoke(result, weaponInstance);
                return result;
            }

            // 강화 규칙: 성공과 실패 여부에 관계없이 재료는 항상 먼저 소모됨
            bool consumed = inventory.ConsumeItemTotal(materialItem, requiredCost);
            if (!consumed)
            {
                result.resultType = EnhancementResultType.NotEnoughMaterials;
                OnEnhanceExecuted?.Invoke(result, weaponInstance);
                return result;
            }
        }

        // 6. 성공 확률 계산 및 랜덤 주사위 롤 (0.0 ~ 100.0)
        float rate = data.GetSuccessRate(weaponInstance.EnhanceLevel);
        result.successRate = rate;
        float randomRoll = UnityEngine.Random.Range(0f, 100f);

        if (randomRoll < rate)
        {
            // [강화 성공] 단계 +1 증가
            weaponInstance.SetEnhanceLevel(weaponInstance.EnhanceLevel + 1);
            result.currentLevel = weaponInstance.EnhanceLevel;
            result.resultType = EnhancementResultType.Success;
        }
        else
        {
            // [강화 실패] 기존 단계 유지 (하락 및 파괴 없음)
            result.currentLevel = weaponInstance.EnhanceLevel;
            result.resultType = EnhancementResultType.Failed;
        }

        // 7. 현재 플레이어가 장착 중인 무기를 강화했다면 공격력 실시간 재계산 및 UI 갱신
        GameObject playerObjRef = GameObject.FindWithTag("Player");
        if (playerObjRef != null)
        {
            EquipmentSystem equipment = playerObjRef.GetComponent<EquipmentSystem>();
            Player_CC player = playerObjRef.GetComponent<Player_CC>();

            // 현재 장착 중인 인스턴스와 일치하는지 비교
            if (equipment != null && equipment.CurrentWeaponInstance == weaponInstance)
            {
                if (player != null)
                {
                    // 기본공격력 * (1 + 단계 * 0.1) 공식을 즉시 재계산하여 플레이어 공격력에 반영
                    player.RefreshEquippedWeaponStats();
                }
            }

            // 캐릭터 정보창(스탯 UI)이 열려있다면 즉시 스탯 텍스트 갱신
            var charInfoUI = FindAnyObjectByType<CharacterInfoUI>(FindObjectsInactive.Include);
            if (charInfoUI != null && charInfoUI.gameObject.activeInHierarchy)
            {
                charInfoUI.SettingStatPanel();
            }
        }

        // 결과 이벤트 발행 및 반환
        OnEnhanceExecuted?.Invoke(result, weaponInstance);
        return result;
    }
}
