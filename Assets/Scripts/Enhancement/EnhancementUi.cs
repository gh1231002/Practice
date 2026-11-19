using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 무기 강화 UI 패널의 전체 라이프사이클과 화면 갱신, 상호작용을 총괄하는 컨트롤러 클래스입니다.
/// 인벤토리 무기 목록 수집, 선택 무기 상세 표시, 예상 스탯 계산, 강화 실행 요청을 처리합니다.
/// </summary>
public class EnhancementUi : MonoBehaviour
{
    public static EnhancementUi Instance { get; private set; }

    [Header("강화 설정 데이터")]
    [Tooltip("강화 확률 및 재료 소모량 등이 정의된 ScriptableObject 에셋입니다.")]
    [SerializeField] EnhancementData enhancementData;

    [Header("좌측 보유 무기 목록 UI")]
    [Tooltip("무기 슬롯들이 배치될 ScrollView의 Content Transform입니다.")]
    [SerializeField] Transform WeaponSlotParent;
    [Tooltip("무기 슬롯 생성을 위한 프리팹 (EnhanceWeaponSlotUi 부착)입니다.")]
    [SerializeField] GameObject WeaponSlotPrefab;
    [Tooltip("보유한 강화 가능 무기가 없을 때 표시되는 안내 문구 오브젝트입니다.")]
    [SerializeField] GameObject EmptyWeaponListNotice;

    [Header("우측 선택 무기 상세 정보 UI")]
    [Tooltip("무기 상세 정보 전체를 감싸는 그룹 오브젝트입니다.")]
    [SerializeField] GameObject WeaponDetailGroup;
    [Tooltip("선택된 무기의 대형 프리뷰 아이콘 이미지입니다.")]
    [SerializeField] Image SelectedWeaponIcon;
    [Tooltip("선택된 무기의 이름 텍스트입니다.")]
    [SerializeField] TextMeshProUGUI SelectedWeaponNameText;
    [Tooltip("선택된 무기의 설명 텍스트입니다.")]
    [SerializeField] TextMeshProUGUI SelectedWeaponDescText;
    [Tooltip("선택된 무기의 현재 강화 단계 (+0, +5 등) 텍스트입니다.")]
    [SerializeField] TextMeshProUGUI CurrentEnhanceLevelText;
    [Tooltip("강화 수치가 반영된 현재 무기 공격력 텍스트입니다.")]
    [SerializeField] TextMeshProUGUI CurrentAtkText;
    [Tooltip("강화 성공 시 적용될 다음 단계 공격력 및 증가폭 텍스트입니다.")]
    [SerializeField] TextMeshProUGUI NextAtkText;
    [Tooltip("현재 단계의 강화 성공 확률 (%) 텍스트입니다.")]
    [SerializeField] TextMeshProUGUI SuccessRateText;
    [Tooltip("강화 성공 확률을 시각적으로 나타내는 게이지 슬라이더(ProgressBar)입니다.")]
    [SerializeField] Slider SuccessRateSlider;

    [Header("강화 재료 정보 UI")]
    [Tooltip("강화 재료(강화석)의 아이콘 이미지입니다.")]
    [SerializeField] Image MaterialIcon;
    [Tooltip("강화 재료의 이름 텍스트 ('강화석')입니다.")]
    [SerializeField] TextMeshProUGUI MaterialNameText;
    [Tooltip("인벤토리 보유 수량과 필요 수량 비교 텍스트 (예: '12 / 1')입니다.")]
    [SerializeField] TextMeshProUGUI MaterialCountText;
    [Tooltip("재료 충족 여부를 나타내는 뱃지 오브젝트입니다.")]
    [SerializeField] GameObject MaterialStatusBadge;
    [Tooltip("재료 충족 상태 텍스트 ('✔ 충분' / '✘ 부족')입니다.")]
    [SerializeField] TextMeshProUGUI MaterialStatusText;

    [Header("하단 조작 및 결과 UI")]
    [Tooltip("강화를 시도하는 대형 버튼입니다.")]
    [SerializeField] Button BtnEnhance;
    [Tooltip("강화 버튼 내부의 텍스트 ('강화하기', '재료 부족', '최대 강화')입니다.")]
    [SerializeField] TextMeshProUGUI BtnEnhanceText;
    [Tooltip("패널 우측 상단의 닫기(X) 버튼입니다.")]
    [SerializeField] Button BtnClose;
    [Tooltip("강화 성공/실패 등 최근 결과와 안내를 출력하는 텍스트입니다.")]
    [SerializeField] TextMeshProUGUI ResultNoticeText;

    // 생성된 무기 슬롯 객체 풀 리스트
    List<EnhanceWeaponSlotUi> spawnedSlots = new List<EnhanceWeaponSlotUi>();
    // 현재 플레이어가 강화 대상으로 선택 중인 무기 인스턴스
    InventoryItemInstance selectedWeaponInstance;

    PlayerInventory inventory;
    EquipmentSystem equipment;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        // 버튼 클릭 리스너 연결
        if (BtnEnhance != null)
        {
            BtnEnhance.onClick.AddListener(OnClickEnhance);
        }

        if (BtnClose != null)
        {
            BtnClose.onClick.AddListener(CloseUi);
        }
    }

    private void Start()
    {
        FindPlayerReferences();
    }

    private void OnEnable()
    {
        FindPlayerReferences();

        // 인벤토리 데이터 변경 시 강화 UI 자동 새로고침을 위한 이벤트 구독
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshUi;
            inventory.OnInventoryChanged += RefreshUi;
        }

        // 결과 텍스트 초기화
        if (ResultNoticeText != null)
        {
            ResultNoticeText.text = string.Empty;
        }

        // 창이 열릴 때마다 최신 인벤토리 데이터로 갱신
        RefreshUi();
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshUi;
        }
    }

    /// <summary>
    /// 플레이어 객체 및 관련 컴포넌트 참조를 획득합니다.
    /// </summary>
    void FindPlayerReferences()
    {
        if (inventory != null && equipment != null) return;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            if (inventory == null) inventory = playerObj.GetComponent<PlayerInventory>();
            if (equipment == null) equipment = playerObj.GetComponent<EquipmentSystem>();
        }
    }

    /// <summary>
    /// 외부(대장간 상호작용 등)에서 강화 UI 창을 열 때 호출합니다.
    /// UiManager의 PanelList에 등록되어 플레이어 조작 차단 및 마우스 커서 활성화가 연동됩니다.
    /// </summary>
    public void OpenUi()
    {
        FindPlayerReferences();

        if (UiManager.Instance != null)
        {
            UiManager.Instance.OpenPanel(gameObject);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 강화 UI 창을 닫을 때 호출합니다.
    /// UiManager를 통해 닫히며 열려있는 다른 패널이 없다면 플레이어 입력이 정상 복구됩니다.
    /// </summary>
    public void CloseUi()
    {
        if (UiManager.Instance != null)
        {
            UiManager.Instance.ClosePanel(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 인벤토리 데이터를 바탕으로 무기 목록 슬롯들과 선택 무기 상세 정보를 최신 상태로 새로고침합니다.
    /// </summary>
    public void RefreshUi()
    {
        FindPlayerReferences();
        if (inventory == null) return;

        // 1. 인벤토리 전체 슬롯에서 강화 가능한 무기 인스턴스들만 필터링하여 수집
        List<InventoryItemInstance> weaponInstances = new List<InventoryItemInstance>();
        int capacity = inventory.GetMaxCapacity();
        for (int i = 0; i < capacity; i++)
        {
            var slot = inventory.Getslot(i);
            if (slot != null && !slot.IsEmpty && slot.ItemInstance != null && slot.ItemInstance.CanEnhance)
            {
                weaponInstances.Add(slot.ItemInstance);
            }
        }

        // 이전에 선택했던 무기가 인벤토리에서 버려지거나 사라졌는지 검사
        if (selectedWeaponInstance != null && !weaponInstances.Contains(selectedWeaponInstance))
        {
            selectedWeaponInstance = null;
        }

        // 선택된 무기가 없고 무기 목록이 존재한다면 첫 번째 무기를 기본 선택으로 지정
        if (selectedWeaponInstance == null && weaponInstances.Count > 0)
        {
            selectedWeaponInstance = weaponInstances[0];
        }

        // 2. 좌측 무기 슬롯 UI 생성 및 갱신 (오브젝트 풀링 방식)
        if (WeaponSlotParent != null && WeaponSlotPrefab != null)
        {
            // 필요한 만큼 슬롯 객체 추가 생성
            while (spawnedSlots.Count < weaponInstances.Count)
            {
                GameObject obj = Instantiate(WeaponSlotPrefab, WeaponSlotParent);
                var slotUi = obj.GetComponent<EnhanceWeaponSlotUi>();
                spawnedSlots.Add(slotUi);
            }

            // 각 슬롯 데이터 바인딩
            for (int i = 0; i < spawnedSlots.Count; i++)
            {
                if (i < weaponInstances.Count)
                {
                    var inst = weaponInstances[i];
                    // 현재 플레이어가 장착 중인 인스턴스인지 비교
                    bool isEquipped = (equipment != null && equipment.CurrentWeaponInstance == inst);
                    // 현재 강화창에서 선택된 인스턴스인지 비교
                    bool isSelected = (inst == selectedWeaponInstance);
                    spawnedSlots[i].Init(inst, isEquipped, isSelected, OnSelectWeaponSlot);
                    spawnedSlots[i].gameObject.SetActive(true);
                }
                else
                {
                    spawnedSlots[i].gameObject.SetActive(false);
                }
            }
        }

        // 보유 무기가 하나도 없을 때의 빈 목록 안내 문구 표시 제어
        if (EmptyWeaponListNotice != null)
        {
            EmptyWeaponListNotice.SetActive(weaponInstances.Count == 0);
        }

        // 3. 우측 선택 무기 상세 정보 뷰 갱신
        UpdateDetailView();
    }

    /// <summary>
    /// 좌측 목록에서 특정 무기 슬롯을 클릭했을 때 호출되는 콜백입니다.
    /// </summary>
    /// <param name="clickedSlot">클릭된 슬롯 컴포넌트</param>
    void OnSelectWeaponSlot(EnhanceWeaponSlotUi clickedSlot)
    {
        if (clickedSlot == null || clickedSlot.WeaponInstance == null) return;

        selectedWeaponInstance = clickedSlot.WeaponInstance;

        // 모든 슬롯의 선택 하이라이트(체크마크) 상태 갱신
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i].gameObject.activeSelf)
            {
                spawnedSlots[i].SetSelected(spawnedSlots[i].WeaponInstance == selectedWeaponInstance);
            }
        }

        // 이전 결과 안내 텍스트 초기화
        if (ResultNoticeText != null)
        {
            ResultNoticeText.text = string.Empty;
        }

        // 우측 상세 정보 화면 갱신
        UpdateDetailView();
    }

    /// <summary>
    /// 현재 선택된 무기의 스탯, 성공 확률, 필요 재료, 버튼 상태를 계산하여 우측 패널에 표시합니다.
    /// </summary>
    void UpdateDetailView()
    {
        // 선택된 무기가 없거나 유효하지 않은 경우 상세창 비활성화
        if (selectedWeaponInstance == null || !(selectedWeaponInstance.ItemData is WeaponData weaponData))
        {
            if (WeaponDetailGroup != null) WeaponDetailGroup.SetActive(false);
            if (BtnEnhance != null) BtnEnhance.interactable = false;
            return;
        }

        if (WeaponDetailGroup != null) WeaponDetailGroup.SetActive(true);

        // 1. 무기 기본 정보 (아이콘, 이름, 설명)
        if (SelectedWeaponIcon != null)
        {
            SelectedWeaponIcon.sprite = weaponData.itemIcon;
            SelectedWeaponIcon.gameObject.SetActive(true);
        }

        if (SelectedWeaponNameText != null)
        {
            SelectedWeaponNameText.text = weaponData.itemName;
        }

        if (SelectedWeaponDescText != null)
        {
            SelectedWeaponDescText.text = weaponData.itemInfo;
        }

        // 2. 현재 강화 단계 (+0, +5 등)
        if (CurrentEnhanceLevelText != null)
        {
            CurrentEnhanceLevelText.text = $"+{selectedWeaponInstance.EnhanceLevel}";
        }

        // 3. 현재 공격력 (계산식: 기본 무기 공격력 * (1 + 강화 단계 * 0.1))
        float curAtk = Player_CC.CalculateWeaponAtk(selectedWeaponInstance);
        if (CurrentAtkText != null)
        {
            CurrentAtkText.text = $"{curAtk:0.#}";
        }

        // 4. 최대 강화 단계(+10) 도달 여부 검사
        int maxLevel = enhancementData != null ? enhancementData.MaxEnhanceLevel : InventoryItemInstance.maxEnhanceLevel;
        bool isMaxLevel = selectedWeaponInstance.EnhanceLevel >= maxLevel;

        if (isMaxLevel)
        {
            if (NextAtkText != null) NextAtkText.text = "MAX";
            if (SuccessRateText != null) SuccessRateText.text = "-";
            if (SuccessRateSlider != null) SuccessRateSlider.value = 1f;
            if (MaterialCountText != null) MaterialCountText.text = "-";
            if (MaterialStatusText != null) MaterialStatusText.text = "-";
            if (BtnEnhance != null)
            {
                BtnEnhance.interactable = false;
                if (BtnEnhanceText != null) BtnEnhanceText.text = "최대 강화";
            }
            return;
        }

        // 5. 다음 단계 예상 공격력 및 증가폭 계산
        float nextAtk = weaponData.weaponAtk * (1f + (selectedWeaponInstance.EnhanceLevel + 1) * 0.1f);
        if (NextAtkText != null)
        {
            float bonus = nextAtk - curAtk;
            NextAtkText.text = $"{nextAtk:0.#} <color=#66FF66>(+{bonus:0.#})</color>";
        }

        // 6. 현재 단계의 성공 확률 및 슬라이더 게이지 반영
        float rate = enhancementData != null ? enhancementData.GetSuccessRate(selectedWeaponInstance.EnhanceLevel) : 50f;
        if (SuccessRateText != null)
        {
            SuccessRateText.text = $"{rate:0.#}%";
        }
        if (SuccessRateSlider != null)
        {
            SuccessRateSlider.value = rate / 100f;
        }

        // 7. 재료 아이템 정보 및 보유량/필요량 검사
        ItemData matItem = enhancementData != null ? enhancementData.MaterialItem : null;
        int reqCost = enhancementData != null ? enhancementData.GetMaterialCost(selectedWeaponInstance.EnhanceLevel) : 1;
        int ownedCount = (inventory != null && matItem != null) ? inventory.GetItemCount(matItem) : 0;

        // 재료 아이콘
        if (MaterialIcon != null)
        {
            if (matItem != null && matItem.itemIcon != null)
            {
                MaterialIcon.sprite = matItem.itemIcon;
                MaterialIcon.gameObject.SetActive(true);
            }
            else
            {
                MaterialIcon.gameObject.SetActive(false);
            }
        }

        // 재료 이름
        if (MaterialNameText != null)
        {
            MaterialNameText.text = matItem != null ? matItem.itemName : "강화석";
        }

        // 보유량 / 필요량 텍스트 (충분하면 초록색, 부족하면 빨간색 표시)
        bool hasEnoughMaterials = ownedCount >= reqCost;
        if (MaterialCountText != null)
        {
            string colorTag = hasEnoughMaterials ? "<color=#00FF66>" : "<color=#FF4444>";
            MaterialCountText.text = $"{colorTag}{ownedCount}</color> / {reqCost}";
        }

        // 상태 뱃지 텍스트
        if (MaterialStatusText != null)
        {
            MaterialStatusText.text = hasEnoughMaterials ? "✔ 충분" : "✘ 부족";
            MaterialStatusText.color = hasEnoughMaterials ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);
        }

        if (MaterialStatusBadge != null)
        {
            MaterialStatusBadge.SetActive(true);
        }

        // 8. 강화 버튼 활성화 및 안내 텍스트 조정
        if (BtnEnhance != null)
        {
            BtnEnhance.interactable = hasEnoughMaterials;
            if (BtnEnhanceText != null)
            {
                BtnEnhanceText.text = hasEnoughMaterials ? "강화하기" : "재료 부족";
            }
        }
    }

    /// <summary>
    /// [강화하기] 버튼 클릭 시 호출되어 EnhancementSystem에 강화를 요청합니다.
    /// </summary>
    void OnClickEnhance()
    {
        if (selectedWeaponInstance == null) return;

        // EnhancementSystem 인스턴스 탐색
        EnhancementSystem sys = EnhancementSystem.Instance;
        if (sys == null)
        {
            sys = FindAnyObjectByType<EnhancementSystem>();
        }

        if (sys == null)
        {
            Debug.LogError("[EnhancementUi] EnhancementSystem이 씬에 존재하지 않습니다.");
            return;
        }

        // 실제 강화 연산 실행
        EnhancementResult result = sys.TryEnhance(selectedWeaponInstance, enhancementData);

        // 결과 유형에 따른 피드백 메시지 표시
        switch (result.resultType)
        {
            case EnhancementResultType.Success:
                ShowNotice($"<color=#33FF33>강화 성공!</color> (+{result.previousLevel} → +{result.currentLevel})");
                break;
            case EnhancementResultType.Failed:
                ShowNotice($"<color=#FF5555>강화 실패...</color> (+{result.currentLevel} 유지)");
                break;
            case EnhancementResultType.MaxLevelReached:
                ShowNotice("이미 최대 강화 단계에 도달했습니다.");
                break;
            case EnhancementResultType.NotEnoughMaterials:
                ShowNotice("<color=#FF3333>강화 재료가 부족합니다.</color>");
                break;
            default:
                ShowNotice("강화를 진행할 수 없습니다.");
                break;
        }

        // 결과 수치를 반영하여 UI 즉시 새로고침
        RefreshUi();
    }

    /// <summary>
    /// 하단 결과 텍스트 및 중앙 NoticePanel에 메시지를 출력합니다.
    /// </summary>
    /// <param name="msg">출력할 안내 문구</param>
    void ShowNotice(string msg)
    {
        if (ResultNoticeText != null)
        {
            ResultNoticeText.text = msg;
        }

        if (UiManager.Instance != null)
        {
            UiManager.Instance.StartNoticePanel(msg);
        }
    }
}
