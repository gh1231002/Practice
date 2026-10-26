using UnityEngine;
using UnityEngine.Playables;

public class BossCutsceneManager : MonoBehaviour
{
    [SerializeField] GameObject bossObject;
    [SerializeField] GameObject bossRestrictedArea;
    [SerializeField] string bossName;
    [SerializeField] PlayableDirector timeLineDirector;
    [SerializeField] BossAIController aIController;
    [Header("디버그 테스트 설정")]
    [SerializeField] Transform debugTargetPoint;

    // 현재 디버그 테스트로 컷신이 시작했는지 기록
    bool isDebugTest;
    // 컷신의 중복 실행 방지
    bool hasStartCutscene;

    private void Awake()
    {
        bossRestrictedArea.SetActive(false);
    }

    public void StartBossCutscene()
    {
        // 컷신 중복 실행 방지
        if (hasStartCutscene) return;

        if(UiManager.Instance != null)
        {
            UiManager.Instance.ToggleCutsceneMode(true);
        }

        if(timeLineDirector != null)
        {
            timeLineDirector.Play();
        }
        hasStartCutscene = true;
    }
    /// <summary>
    /// 기존 컷신을 재생하되, 컷신 종료 후 보스 타깃을 디버그용 위치로 설정한다.
    /// </summary>
    public void StartBossDebugTest()
    {
        // 이미 컷신을 시작했다면 중복 실행 안함
        if (hasStartCutscene) return;

        // 테스트 위치가 지정되지 않았다면 실행하지않음
        if(debugTargetPoint == null)
        {
            Debug.LogWarning("[BossDebug] Debug Target Point가 연결되지 않았습니다.", this);
            return;
        }
        
        // 컷신 종료 후 디버그용 위치를 타깃으로 사용하도록 표시한다.
        isDebugTest = true;
        StartBossCutscene();
    }

    public void IntroFinished()
    {
        if(UiManager.Instance != null)
        {
            UiManager.Instance.ToggleCutsceneMode(false);
            UiManager.Instance.ToggleBossInfoGroup(true);

            if(bossObject != null)
            {
                MonsterStats bossStats = bossObject.GetComponent<MonsterStats>();
                UiManager.Instance.InitBossHpUi(bossStats, bossName);
            }
        }

        bossRestrictedArea.SetActive(true);
        // 이번 전투에서 사용할 대상을 결정한다.
        Transform battleTarget = null;
        if(isDebugTest)
        {
            battleTarget = debugTargetPoint;
        }
        else
        {
            GameObject obj = GameObject.FindWithTag("Player");
            if(obj != null)
            {
                battleTarget = obj.transform;
            }
        }

        aIController.StartBossBattle(battleTarget);
    }
}
