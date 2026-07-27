using UnityEngine;

public class RewardManager : MonoBehaviour
{
    [SerializeField] Transform TrsWeapon;
    Player_CC Player;
    public static RewardManager instance;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        GameObject obj = GameObject.FindWithTag("Player");
        Player = obj.GetComponent<Player_CC>();
    }
    /// <summary>
    /// 무기 생성 함수
    /// </summary>
    public void CraeteWeapon(int index, QuestData quset)
    {
        //이미 무기가 있다면 삭제
        if(TrsWeapon.childCount > 0)
        {
            Destroy(TrsWeapon.GetChild(0).gameObject);
        }
        //무기 생성
        GameObject NewWeapon = Instantiate(quset.choices[index].RewardPrefab, TrsWeapon);
        NewWeapon.transform.localPosition = Vector3.zero;
        //무기 전달
        Player.SetWeapon(NewWeapon);
    }
}
