using System;
using UnityEngine;

/// <summary>
/// 대장간의 모루 등 강화 장소 오브젝트에 부착하여 플레이어와의 상호작용을 처리하는 컴포넌트입니다.
/// 일반 NPC 대화 시스템과 분리되어, F키 입력 시 대화창(DialogueUi) 없이 오직 강화 UI만 단독으로 열리도록 제어합니다.
/// </summary>
public class EnhancementStation : MonoBehaviour
{
    [Header("센서 연결 (비어 있으면 자식 오브젝트에서 자동 탐색)")]
    [Tooltip("플레이어의 접근 및 이탈을 감지하는 InteractSensor 컴포넌트입니다.")]
    [SerializeField] InteractSensor InSensor;

    [Header("강화 UI 연결 (비어 있으면 씬에서 자동 탐색)")]
    [Tooltip("열고 닫을 대상 EnhancementUi 패널 컴포넌트입니다.")]
    [SerializeField] EnhancementUi enhancementUi;

    Player_CC player;
    bool isPlayerInZone;

    private void Awake()
    {
        // 인스펙터에서 센서가 연결되지 않았다면 자식 오브젝트에서 자동 탐색
        if (InSensor == null)
        {
            InSensor = GetComponentInChildren<InteractSensor>();
        }

        // 센서의 진입/이탈 이벤트 구독
        if (InSensor != null)
        {
            InSensor.OnInteract += HandlePlayerEnter;
            InSensor.OffInteract += HandlePlayerExit;
        }
    }

    private void Start()
    {
        FindReferences();
    }

    /// <summary>
    /// 플레이어 및 강화 UI에 대한 참조를 안전하게 획득합니다.
    /// </summary>
    private void FindReferences()
    {
        if (player == null)
        {
            GameObject obj = GameObject.FindWithTag("Player");
            if (obj != null)
            {
                player = obj.GetComponent<Player_CC>();
            }
        }

        if (enhancementUi == null)
        {
            enhancementUi = FindAnyObjectByType<EnhancementUi>(FindObjectsInactive.Include);
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (InSensor != null)
        {
            InSensor.OnInteract -= HandlePlayerEnter;
            InSensor.OffInteract -= HandlePlayerExit;
        }

        // 플레이어에 등록된 커스텀 상호작용 핸들러 안전 해제
        if (player != null && player.CustomInteractAction == OpenEnhanceUi)
        {
            player.CustomInteractAction = null;
        }
    }

    /// <summary>
    /// 플레이어가 강화기 상호작용 감지 영역(Trigger)에 들어왔을 때 호출됩니다.
    /// </summary>
    private void HandlePlayerEnter()
    {
        isPlayerInZone = true;
        FindReferences();

        // [F] 상호작용 안내 UI 팝업 활성화
        if (UiManager.Instance != null)
        {
            UiManager.Instance.OnInteractPanel();
        }

        // 일반 NPC 대화(OnDialogue)가 아닌, 기능형 전용 액션으로 강화 UI 오픈 메서드를 등록합니다.
        // 이렇게 함으로써 대화창이 중복으로 열리는 현상을 원천 방지합니다.
        if (player != null)
        {
            player.CustomInteractAction = OpenEnhanceUi;
        }
    }

    /// <summary>
    /// 플레이어가 강화기 상호작용 감지 영역을 벗어났을 때 호출됩니다.
    /// </summary>
    private void HandlePlayerExit()
    {
        isPlayerInZone = false;

        // [F] 상호작용 안내 UI 숨김
        if (UiManager.Instance != null)
        {
            UiManager.Instance.OffInteractPanel();
        }

        // 등록했던 상호작용 액션 해제
        if (player != null && player.CustomInteractAction == OpenEnhanceUi)
        {
            player.CustomInteractAction = null;
        }
    }

    /// <summary>
    /// 플레이어가 F키를 눌렀을 때 실행되는 강화 UI 오픈 메서드입니다.
    /// </summary>
    private void OpenEnhanceUi()
    {
        if (!isPlayerInZone) return;

        FindReferences();

        if (enhancementUi != null)
        {
            // UiManager의 PanelList와 연동하여 커서 활성화 및 플레이어 조작 차단과 함께 UI 오픈
            enhancementUi.OpenUi();

            // UI가 열렸으므로 바닥의 [F] 상호작용 안내 텍스트는 숨김
            if (UiManager.Instance != null)
            {
                UiManager.Instance.OffInteractPanel();
            }
        }
        else
        {
            Debug.LogWarning("[EnhancementStation] 씬에 EnhancementUi가 연결되지 않았습니다.");
        }
    }
}
