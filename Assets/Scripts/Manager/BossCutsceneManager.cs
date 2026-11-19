
using UnityEngine;
using UnityEngine.Playables;

public class BossCutsceneManager : MonoBehaviour
{
    [SerializeField] GameObject bossObject;          // 보스 오브젝트
    [SerializeField] GameObject bossRestrictedArea;  // 보스전 시작 시 입구를 차단하는 오브젝트
    [SerializeField] string bossName;                // 보스 HP UI에 표시할 이름
    [SerializeField] PlayableDirector timeLineDirector; // 보스 등장 컷신을 재생하는 Timeline
    [SerializeField] BossAIController aIController;  // 보스 AI를 제어하는 컴포넌트

    [Header("테스트용 셋팅")]
    [SerializeField] Transform debugTargetPoint;     // 디버그 전투에서 보스가 공격할 테스트 대상

    bool isDebugTest;       // 디버그 전투 여부
    bool hasStartCutscene;  // 컷신이 이미 시작되었는지 확인
    bool isBossDefeated;    // 보스 사망 처리가 이미 실행되었는지 확인

    MonsterStats bossStats; // 보스의 체력 및 사망 이벤트를 관리하는 컴포넌트

    private void Awake()
    {
        // 씬 진입 시 보스전 입구가 열려 있는 상태로 시작하도록 설정한다.
        // 보스전이 시작되기 전에는 입구를 차단하지 않는다.
        if (bossRestrictedArea != null)
            bossRestrictedArea.SetActive(false);
    }

    public void StartBossCutscene()
    {
        // 이미 컷신이 시작되었다면 중복 실행하지 않는다.
        if (hasStartCutscene) return;

        // 컷신이 재생되는 동안 플레이어의 일반 UI를 컷신 모드로 전환한다.
        if (UiManager.Instance != null)
        {
            UiManager.Instance.ToggleCutsceneMode(true);
        }

        // Timeline이 연결되어 있다면 보스 등장 컷신을 재생한다.
        if (timeLineDirector != null)
        {
            timeLineDirector.Play();
        }

        // Timeline이 없더라도 컷신 시작 요청이 처리된 것으로 기록한다.
        hasStartCutscene = true;
    }

    public void StartBossDebugTest()
    {
        // 이미 컷신 또는 전투 시작 절차가 진행되었다면 중복 실행하지 않는다.
        if (hasStartCutscene) return;

        // 디버그 전투에 사용할 대상이 없으면 실행하지 않는다.
        if (debugTargetPoint == null)
        {
            Debug.LogWarning("[BossDebug] Debug Target Point가 지정되지 않았습니다.", this);
            return;
        }

        // 이후 IntroFinished()에서 플레이어 대신 테스트 대상을 사용하도록 설정한다.
        isDebugTest = true;

        // 일반 보스 컷신 시작 절차를 재사용한다.
        StartBossCutscene();
    }

    public void IntroFinished()
    {
        // 컷신이 끝났을 때 보스의 MonsterStats를 가져온다.
        if (bossObject != null)
        {
            bossStats = bossObject.GetComponent<MonsterStats>();

            if (bossStats != null)
            {
                // 같은 이벤트가 중복 등록되는 것을 방지하기 위해
                // 기존 등록을 먼저 해제한 뒤 다시 등록한다.
                bossStats.OnDeath -= OnBossDefeated;
                bossStats.OnDeath += OnBossDefeated;
            }
        }

        // 컷신 모드를 종료하고 보스 HP UI를 표시한다.
        if (UiManager.Instance != null)
        {
            UiManager.Instance.ToggleCutsceneMode(false);
            UiManager.Instance.ToggleBossInfoGroup(true);

            // 보스의 체력 정보를 UI에 전달한다.
            if (bossStats != null)
            {
                UiManager.Instance.InitBossHpUi(bossStats, bossName);
            }
        }

        // 컷신이 끝나고 보스전이 시작되는 시점에 입구를 차단한다.
        if (bossRestrictedArea != null)
            bossRestrictedArea.SetActive(true);

        Transform battleTarget = null;

        // 디버그 테스트라면 Inspector에 지정한 테스트 대상을 사용한다.
        if (isDebugTest)
        {
            battleTarget = debugTargetPoint;
        }
        else
        {
            // 일반 전투라면 Player 태그가 지정된 오브젝트를 찾는다.
            GameObject obj = GameObject.FindWithTag("Player");

            if (obj != null)
            {
                battleTarget = obj.transform;
            }
        }

        // 보스 AI에 전투 대상을 전달하고 전투를 시작한다.
        if (aIController != null)
        {
            aIController.StartBossBattle(battleTarget);
        }
    }

    private void OnBossDefeated()
    {
        // 이미 사망 처리가 실행되었다면 중복으로 처리하지 않는다.
        if (isBossDefeated) return;
        isBossDefeated = true;

        // 보스 사망 이벤트 구독을 해제한다.
        UnsubscribeBossDeath();

        // 보스가 사망하면 입구 차단 오브젝트를 비활성화하여 다시 개방한다.
        if (bossRestrictedArea != null)
        {
            bossRestrictedArea.SetActive(false);
        }

        // 보스 HP UI를 숨긴다.
        if (UiManager.Instance != null)
        {
            UiManager.Instance.ToggleBossInfoGroup(false);
        }
    }

    private void OnDisable()
    {
        // 오브젝트가 비활성화될 때 이벤트 구독을 해제하여
        // 비활성화된 매니저가 보스 사망 이벤트를 받지 않도록 한다.
        UnsubscribeBossDeath();
    }

    private void OnDestroy()
    {
        // 오브젝트가 파괴될 때도 이벤트 구독을 해제한다.
        // OnDisable()에서 이미 해제했더라도 중복 해제는 안전하다.
        UnsubscribeBossDeath();
    }

    private void UnsubscribeBossDeath()
    {
        // MonsterStats가 존재할 때만 이벤트 구독을 해제한다.
        if (bossStats != null)
        {
            bossStats.OnDeath -= OnBossDefeated;
        }
    }
}
