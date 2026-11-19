using System;
using UnityEngine;

/// <summary>
/// 무기 강화 시스템의 설정 데이터를 관리하는 ScriptableObject입니다.
/// 인스펙터에서 최대 강화 단계, 단계별 성공 확률, 재료 소모량, 재료 아이템을 조정할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "EnhancementData", menuName = "Scriptable Objects/EnhancementData")]
public class EnhancementData : ScriptableObject
{
    [Header("기본 설정")]
    [Tooltip("강화 가능한 최대 단계입니다. (기본값: 10, +0 ~ +10)")]
    [SerializeField] int maxEnhanceLevel = 10;

    [Tooltip("강화 시 소모되는 재료 아이템 데이터입니다. (예: 강화석)")]
    [SerializeField] ItemData materialItem;

    [Header("단계별 설정 (배열 인덱스 = 현재 강화 단계, 0 = 0->1 강화)")]
    [Tooltip("각 단계별 강화 성공 확률 (%)입니다. (0 ~ 100)")]
    [SerializeField] float[] successRates = new float[10]
    {
        100f, // 0 -> 1 강화 성공 확률 (100%)
        90f,  // 1 -> 2 강화 성공 확률 (90%)
        80f,  // 2 -> 3 강화 성공 확률 (80%)
        70f,  // 3 -> 4 강화 성공 확률 (70%)
        60f,  // 4 -> 5 강화 성공 확률 (60%)
        50f,  // 5 -> 6 강화 성공 확률 (50%)
        40f,  // 6 -> 7 강화 성공 확률 (40%)
        30f,  // 7 -> 8 강화 성공 확률 (30%)
        20f,  // 8 -> 9 강화 성공 확률 (20%)
        10f   // 9 -> 10 강화 성공 확률 (10%)
    };

    [Tooltip("각 단계별 강화 시 소모되는 재료 개수입니다.")]
    [SerializeField] int[] materialCosts = new int[10]
    {
        1, // 0 -> 1 강화 시 필요 재료 개수
        1, // 1 -> 2 강화 시 필요 재료 개수
        2, // 2 -> 3 강화 시 필요 재료 개수
        2, // 3 -> 4 강화 시 필요 재료 개수
        3, // 4 -> 5 강화 시 필요 재료 개수
        3, // 5 -> 6 강화 시 필요 재료 개수
        4, // 6 -> 7 강화 시 필요 재료 개수
        4, // 7 -> 8 강화 시 필요 재료 개수
        5, // 8 -> 9 강화 시 필요 재료 개수
        5  // 9 -> 10 강화 시 필요 재료 개수
    };

    /// <summary>
    /// 최대 강화 단계 프로퍼티 (+10)
    /// </summary>
    public int MaxEnhanceLevel => maxEnhanceLevel;

    /// <summary>
    /// 강화 재료 아이템 데이터 프로퍼티
    /// </summary>
    public ItemData MaterialItem => materialItem;

    /// <summary>
    /// 현재 강화 단계에서 다음 단계로 시도할 때의 성공 확률(0 ~ 100%)을 반환합니다.
    /// 배열 범위를 벗어나거나 설정이 누락되어도 안전하게 기본값을 반환합니다.
    /// </summary>
    /// <param name="currentLevel">강화 시도 전 현재 강화 단계</param>
    /// <returns>성공 확률 (0 ~ 100)</returns>
    public float GetSuccessRate(int currentLevel)
    {
        // 유효하지 않은 단계이거나 이미 최대 단계에 도달한 경우 확률 0%
        if (currentLevel < 0 || currentLevel >= maxEnhanceLevel)
        {
            return 0f;
        }

        // 설정 배열이 비어있을 경우 안전 방어값 (50%) 반환
        if (successRates == null || successRates.Length == 0)
        {
            return 50f;
        }

        // 정상 인덱스 범위 내의 확률 반환 (0 ~ 100 사이로 보정)
        if (currentLevel < successRates.Length)
        {
            return Mathf.Clamp(successRates[currentLevel], 0f, 100f);
        }

        // 배열 길이가 maxEnhanceLevel보다 짧을 경우 마지막 설정값으로 보정
        return Mathf.Clamp(successRates[successRates.Length - 1], 0f, 100f);
    }

    /// <summary>
    /// 현재 강화 단계에서 다음 단계로 시도할 때 소모되는 재료 아이템 개수를 반환합니다.
    /// </summary>
    /// <param name="currentLevel">강화 시도 전 현재 강화 단계</param>
    /// <returns>소모 재료 개수 (최소 1개 이상)</returns>
    public int GetMaterialCost(int currentLevel)
    {
        // 유효하지 않은 단계이거나 이미 최대 단계인 경우 재료 불필요 (0개)
        if (currentLevel < 0 || currentLevel >= maxEnhanceLevel)
        {
            return 0;
        }

        // 설정 배열이 비어있을 경우 안전 방어값 (1개) 반환
        if (materialCosts == null || materialCosts.Length == 0)
        {
            return 1;
        }

        // 정상 인덱스 범위 내의 소모 개수 반환
        if (currentLevel < materialCosts.Length)
        {
            return Mathf.Max(1, materialCosts[currentLevel]);
        }

        // 배열 길이가 maxEnhanceLevel보다 짧을 경우 마지막 설정값으로 보정
        return Mathf.Max(1, materialCosts[materialCosts.Length - 1]);
    }

    /// <summary>
    /// 다음 단계 강화에 성공했을 때의 예상 공격력을 계산합니다.
    /// 공식: 기본 공격력 * (1 + (현재단계 + 1) * 0.1)
    /// </summary>
    /// <param name="weaponInstance">대상 무기 인스턴스</param>
    /// <returns>다음 단계 공격력</returns>
    public static float CalculateNextWeaponAtk(InventoryItemInstance weaponInstance)
    {
        if (weaponInstance == null || !(weaponInstance.ItemData is WeaponData weaponData))
        {
            return 0f;
        }

        // 다음 단계 (최대 10단계를 초과하지 않음)
        int nextLevel = Mathf.Min(weaponInstance.EnhanceLevel + 1, InventoryItemInstance.maxEnhanceLevel);
        return weaponData.weaponAtk * (1f + nextLevel * 0.1f);
    }
}
