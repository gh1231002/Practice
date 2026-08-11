using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemTooltipUi : MonoBehaviour
{
    public static ItemTooltipUi instance;

    [Header("아이템 텍스트")]
    [SerializeField] TextMeshProUGUI ItemName;
    [SerializeField] TextMeshProUGUI ItemInfo;

    [Header("세팅")]
    [SerializeField] Vector2 offset = new Vector2();

    RectTransform rectTrs;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        rectTrs = GetComponent<RectTransform>();
        HideTooltip();
    }

    private void Update()
    {
        //툴팁이 켜져있을 때 마우스 위치를 실시간으로 추적
        if(gameObject.activeSelf)
        {
            if (Mouse.current != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                rectTrs.position = mousePos + offset;
            }
        }
    }

    public void ShowTooltip(string name, string info)
    {
        ItemName.text = name;
        ItemInfo.text = info;

        gameObject.SetActive(true);

        //텍스트 내용 변경 시 패널 크기를 즉시 다시 계산 (어그러짐 방지)
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTrs);
    }

    public void HideTooltip()
    {
        gameObject.SetActive(false);
    }
}
