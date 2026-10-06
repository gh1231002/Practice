using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHpBarUi : MonoBehaviour
{
    [SerializeField] Slider hpBar;
    [SerializeField] TextMeshProUGUI bossNameText;

    MonsterStats currentBossStats;

    /// <summary>
    /// 보스 MonsterStats를 전달받아 이벤트를 구독하고 UI를 초기화합니다.
    /// </summary>
    /// <param name="bossStats"></param>
    /// <param name="bossName"></param>
    public void BindBoss(MonsterStats bossStats, string bossName)
    {
        // 기존 구독 해제
        UnbindBoss();

        currentBossStats = bossStats;

        if(bossNameText != null) bossNameText.text = bossName;
        if(currentBossStats != null)
        {
            // MonsterStats의 이벤트에 UI 갱신 함수 등록
            currentBossStats.OnHpChanged += UpdateHpBar;
            currentBossStats.OnDeath += OnBossDeath;

            // 초기 체력 상태 반영
            UpdateHpBar(currentBossStats.CurHp, currentBossStats.MaxHp);
        }

        gameObject.SetActive(true);
    }

    private void UpdateHpBar(float curHp, float maxHp)
    {
        if(hpBar != null)
        {
            hpBar.value = curHp / maxHp;
        }
    }

    private void OnBossDeath()
    {
        UnbindBoss();
        gameObject.SetActive(false); // 보스 사망시 UI꺼짐
    }

    private void UnbindBoss()
    {
        if(currentBossStats != null)
        {
            currentBossStats.OnHpChanged -= UpdateHpBar;
            currentBossStats.OnDeath -= OnBossDeath;
            currentBossStats = null;
        }
    }

    private void OnDisable()
    {
        UnbindBoss();
    }
}
