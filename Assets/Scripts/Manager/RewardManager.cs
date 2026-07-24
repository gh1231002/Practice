using UnityEngine;

public class RewardManager : MonoBehaviour
{
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
    }
    /// <summary>
    /// 무기 생성 함수
    /// </summary>
    public void CraeteWeapon()
    {

    }
}
