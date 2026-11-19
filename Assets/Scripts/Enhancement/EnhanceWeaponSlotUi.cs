using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 무기 강화 UI 내 좌측 '보유 무기 목록'에서 각 무기 아이템을 표시하고 선택하는 개별 슬롯 컴포넌트입니다.
/// </summary>
public class EnhanceWeaponSlotUi : MonoBehaviour
{
    [Header("UI 컴포넌트 바인딩")]
    [Tooltip("무기 아이콘 이미지입니다.")]
    [SerializeField] Image WeaponIcon;

    [Tooltip("무기 이름 텍스트입니다.")]
    [SerializeField] TextMeshProUGUI WeaponNameText;

    [Tooltip("무기의 현재 강화 단계 텍스트 (+0, +5 등)입니다.")]
    [SerializeField] TextMeshProUGUI EnhanceLevelText;

    [Tooltip("현재 플레이어가 장착 중인 무기일 때 켜지는 '장착' 태그 오브젝트입니다.")]
    [SerializeField] GameObject EquippedTag;

    [Tooltip("해당 슬롯이 현재 강화 대상으로 선택되었을 때 켜지는 하이라이트 및 체크마크 오브젝트입니다.")]
    [SerializeField] GameObject SelectedHighlight;

    [Tooltip("슬롯 전체를 감싸는 버튼 컴포넌트입니다.")]
    [SerializeField] Button SlotButton;

    /// <summary>
    /// 이 슬롯이 보관하고 있는 실제 무기 인스턴스 데이터
    /// </summary>
    public InventoryItemInstance WeaponInstance { get; private set; }

    // 슬롯 클릭 시 부모 UI(EnhancementUi)에 선택 사실을 알리는 콜백
    Action<EnhanceWeaponSlotUi> onSelectCallback;

    private void Awake()
    {
        // 버튼 클릭 이벤트 리스너 등록
        if (SlotButton != null)
        {
            SlotButton.onClick.AddListener(OnClickSlot);
        }
    }

    /// <summary>
    /// 무기 인스턴스 데이터로 슬롯 UI를 초기화하고 화면을 갱신합니다.
    /// </summary>
    /// <param name="instance">표시할 무기 인스턴스</param>
    /// <param name="isEquipped">현재 플레이어가 장착 중인 무기인지 여부</param>
    /// <param name="isSelected">현재 강화 대상으로 선택된 상태인지 여부</param>
    /// <param name="onSelect">클릭 시 실행될 선택 콜백</param>
    public void Init(InventoryItemInstance instance, bool isEquipped, bool isSelected, Action<EnhanceWeaponSlotUi> onSelect)
    {
        WeaponInstance = instance;
        onSelectCallback = onSelect;

        // 무기 데이터 유효성 검사 및 UI 요소 반영
        if (instance != null && instance.ItemData is WeaponData weaponData)
        {
            // 무기 아이콘 설정 (스크립트의 ItemData에서 가져옴)
            if (WeaponIcon != null)
            {
                WeaponIcon.sprite = weaponData.itemIcon;
                WeaponIcon.gameObject.SetActive(true);
            }

            // 무기 이름 설정
            if (WeaponNameText != null)
            {
                WeaponNameText.text = weaponData.itemName;
            }

            // 강화 수치 표시 (+0, +5 등)
            if (EnhanceLevelText != null)
            {
                EnhanceLevelText.text = $"+{instance.EnhanceLevel}";
                EnhanceLevelText.gameObject.SetActive(true);
            }

            // 장착 태그 표시
            if (EquippedTag != null)
            {
                EquippedTag.SetActive(isEquipped);
            }

            // 선택 하이라이트 테두리/체크마크 상태 반영
            SetSelected(isSelected);
            gameObject.SetActive(true);
        }
        else
        {
            // 유효하지 않은 데이터인 경우 슬롯 비활성화
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 이 슬롯의 선택 상태(하이라이트/체크마크)를 켜거나 끕니다.
    /// </summary>
    /// <param name="isSelected">선택 여부</param>
    public void SetSelected(bool isSelected)
    {
        if (SelectedHighlight != null)
        {
            SelectedHighlight.SetActive(isSelected);
        }
    }

    /// <summary>
    /// 슬롯 클릭 시 호출되어 부모 UI에 선택 이벤트를 전달합니다.
    /// </summary>
    private void OnClickSlot()
    {
        onSelectCallback?.Invoke(this);
    }
}
