using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUi : MonoBehaviour
{
    [SerializeField] Slider HpBar;
    [SerializeField] TextMeshProUGUI HpText;
    Player_CC Player;

    private void OnEnable()
    {
        GameObject obj = GameObject.FindWithTag("Player");
        if (obj != null && obj.TryGetComponent<Player_CC>(out var player))
        {
            // 중복 구독 방지
            player.ChangeHp -= ChangeHpBar;
            player.ChangeHp += ChangeHpBar;
        }
    }
    private void OnDestroy()
    {
        GameObject obj = GameObject.FindWithTag("Player");

        if(obj != null && obj.TryGetComponent<Player_CC>(out var player))
        {
            player.ChangeHp -= ChangeHpBar;
        }
    }

    private void ChangeHpBar(float CurHp, float MaxHp)
    {
        // 슬라이더 게이지 비율 연산
        HpBar.value = CurHp / MaxHp;

        // 텍스트 표시는 정수로 변환하여 가독성 확보
        // 0.1이라도 살아있다면 1로 표시
        int displayCurHp = Mathf.CeilToInt(CurHp);
        int displayMaxHp = Mathf.RoundToInt(MaxHp);

        // UI 텍스트 갱신
        HpText.text = $"{displayCurHp} / {displayMaxHp}";
    }
}
