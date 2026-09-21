using UnityEngine;
using UnityEngine.UI;

public class MonsterHpUi : MonoBehaviour
{
    [SerializeField] Slider HpBar;
    [SerializeField] GameObject HpCanvas;

    MonsterStats stats;
    MonsterAIController aIController;

    private void Awake()
    {
        stats = GetComponent<MonsterStats>();
        aIController = GetComponent<MonsterAIController>();
    }

    private void Start()
    {
        // 초기 상태  설정
        if(HpCanvas != null)
        {
            HpCanvas.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (stats != null)
        {
            stats.OnHpChanged += UpdateHpBar;
        }
        if(aIController != null)
        {
            aIController.OnCombatStateChanged += ToggleHpbar;
        }
    }
    private void OnDisable()
    {
        if(stats != null)
        {
            stats.OnHpChanged -= UpdateHpBar;
        }
        if(aIController != null)
        {
            aIController.OnCombatStateChanged -= ToggleHpbar;
        }
    }

    private void UpdateHpBar(float curHp, float maxHp)
    {
        if (HpCanvas == null || HpBar == null) return;

        HpBar.value = curHp / maxHp;
    }
    
    private void ToggleHpbar(bool toggle)
    {
        if(HpCanvas != null)
        {
            // 체력바를 켤 때 현재 체력 비율로 슬라이더 수치를 최신화
            if(toggle && stats != null && HpBar != null)
            {
                HpBar.value = stats.CurHp / stats.MaxHp;
            }
            
            HpCanvas.SetActive(toggle);
        }
    }
}
