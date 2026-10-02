using UnityEngine;
using UnityEngine.Playables;

public class BossCutsceneManager : MonoBehaviour
{
    [SerializeField] PlayableDirector timeLineDirector;
    public void StartBossCutscene()
    {
        if(UiManager.Instance != null)
        {
            UiManager.Instance.ToggleMainUi(false);
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
            UiManager.Instance.ToggleMainUi(true);
            UiManager.Instance.ToggleBossInfoGroup(true);
        }
    }
}
