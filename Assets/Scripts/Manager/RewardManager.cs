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

        //보상 무기 데이터 추출
        WeaponData rewardweapon = quest.choices[index].WeaponDate;
        if (rewardweapon == null) return;

        //플레이어 인벤토리 컴포넌트 가져와서 아이템도 함께 집어넣음
        PlayerInventory inventory = Player.GetComponent<PlayerInventory>();
        if(inventory == null) return;
        
        // 인벤토리에 무기 지급을 시도하고 성공 여부를 확인한다
        bool success = inventory.Additem(rewardweapon, 1);
        // 지급 실패 시 장비 해금과 장착도 진행하지 않는다
        if(!success)
        {
            if(UiManager.Instance != null)
            {
                UiManager.Instance.StartNoticePanel("[Quest] 보상 무기 지급에 실패했습니다");
            }
            return;
        }

        // 인벤토리 지급에 성공한 경우
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
