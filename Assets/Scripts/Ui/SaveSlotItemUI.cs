using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 저장 데이터 선택 목록의 개별 슬롯 UI를 제어하는 컴포넌트
/// </summary>
public class SaveSlotItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("슬롯 UI 참조")]
    [SerializeField] TextMeshProUGUI playerNameText;
    [SerializeField] TextMeshProUGUI createdDateText;
    [SerializeField] TextMeshProUGUI lastSavedDateText;
    [SerializeField] GameObject selectedIndicator;

    Action<SaveSlotItemUI> onClickCallback;

    public string SaveSlotId { get; private set; }
    public SaveData BoundData { get; private set; }

    void Awake()
    {
        SetSelected(false);
    }

    /// <summary>
    /// 저장 데이터를 슬롯 UI에 바인딩하고 클릭 콜백을 등록한다.
    /// </summary>
    public void Bind(SaveData data, Action<SaveSlotItemUI> onClicked)
    {
        BoundData = data;
        SaveSlotId = data != null ? data.SaveSlotId : string.Empty;
        onClickCallback = onClicked;

        if (playerNameText != null)
        {
            playerNameText.text = data != null ? data.PlayerName : string.Empty;
        }

        if (createdDateText != null)
        {
            string createdText = data != null ? FormatDateText(data.CreatedAt) : "-";
            createdDateText.text = $"생성 날짜 : {createdText}";
        }

        if (lastSavedDateText != null)
        {
            string lastSavedText = data != null ? FormatDateText(data.LastSavedAt) : "-";
            lastSavedDateText.text = $"마지막 저장 : {lastSavedText}";
        }

        SetSelected(false);
    }

    /// <summary>
    /// 슬롯의 선택 표시 상태를 갱신한다.
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (selectedIndicator != null && selectedIndicator.activeSelf != isSelected)
        {
            selectedIndicator.SetActive(isSelected);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        onClickCallback?.Invoke(this);
    }

    private static string FormatDateText(string isoDateString)
    {
        if (string.IsNullOrWhiteSpace(isoDateString))
        {
            return "-";
        }

        if (DateTime.TryParse(
                isoDateString,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTime parsedDate))
        {
            return parsedDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }

        return isoDateString;
    }
}
