using UnityEngine;

[CreateAssetMenu(fileName = "PotionData", menuName = "Scriptable Objects/PotionData")]
public class PotionData : ItemData
{
    [Header("포션 전용 정보")]
    [Tooltip("최대 최력 대비 회복 비율(0.1 = 10%, 1.0 = 100%)")]
    [Range(0f, 1f)]
    [SerializeField] float healPercent;

    public float HealPercent => healPercent;

    public override string itemInfo
    {
        get
        {
            //인스펙터에 적은 원본 문구 가져오기
            string originalInfo = base.itemInfo;

            //원본 문구에 {0}이 포함되어 있다면 퍼센트 수치로 치환
            if(!string.IsNullOrEmpty(originalInfo) && originalInfo.Contains("{0}"))
            {
                return string.Format(originalInfo, healPercent * 100f);
            }

            return originalInfo;
        }
    }
}
