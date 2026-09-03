using UnityEngine;
using UnityEngine.Android;

public class RewardManager : MonoBehaviour
{
    [SerializeField] Transform TrsWeapon;
    Player_CC Player;
    public static RewardManager instance { get; private set; }

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
    public void CreateWeapon(int index, QuestData quest)
    {
        //이미 무기가 있다면 삭제
        if(TrsWeapon.childCount > 0)
        {
            Destroy(TrsWeapon.GetChild(0).gameObject);
        }
        //무기 생성
        //GameObject NewWeapon = Instantiate(quest.choices[index].WeaponDate.objWeapon, TrsWeapon);
        //NewWeapon.transform.localPosition = quest.choices[index].WeaponDate.trsWeapon;
        //NewWeapon.transform.localRotation = Quaternion.Euler(quest.choices[index].WeaponDate.rotWeapon);

        //무기 전달
        //Player.SetWeapon(quest.choices[index].WeaponDate,NewWeapon);

        //보상 무기 데이터 추출
        WeaponData rewardweapon = quest.choices[index].WeaponDate;
        if (rewardweapon == null) return;

        //플레이어 인벤토리 컴포넌트 가져와서 아이템도 함께 집어넣음
        PlayerInventory inventory = Player.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            inventory.Additem(quest.choices[index].WeaponDate, 1);
        }

        //EquipmentSystem을 통한 자동 장착 처리
        // 이벤트 호출을 통해 3d 생성과 ui 태그 갱신이 동시에 실행됩니다.
        EquipmentSystem equipment = Player.GetComponent<EquipmentSystem>();
        if(equipment != null)
        {
            equipment.UnlockWeapon(rewardweapon);
            equipment.ToggleEquip(rewardweapon);
        }
    }
}
