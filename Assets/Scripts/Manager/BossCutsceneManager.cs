using UnityEngine;
using UnityEngine.Playables;

public class BossCutsceneManager : MonoBehaviour
{
    [SerializeField] GameObject bossObject;
    [SerializeField] GameObject bossRestrictedArea;
    [SerializeField] string bossName;
    [SerializeField] PlayableDirector timeLineDirector;
    [SerializeField] BossAIController aIController;
    Transform Trsplayer;

    private void Awake()
    {
        bossRestrictedArea.SetActive(false);
    }

    public void StartBossCutscene()
    {
        if(UiManager.Instance != null)
        {
            UiManager.Instance.ToggleCutsceneMode(true);
        }

        if(timeLineDirector != null)
        {
            timeLineDirector.Play();
        }
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
            bossRestrictedArea.SetActive(true);
        }
        GameObject obj = GameObject.FindWithTag("Player");
        Trsplayer = obj.GetComponent<Transform>();
        if(Trsplayer != null)
        {
            aIController.StartBossBattle(Trsplayer);
        }
    }
}
