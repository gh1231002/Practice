using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneManager : MonoBehaviour
{
    // 외부에서 씬 이름을 전달받아 보관하는 static 변수
    // static으로 선언하여 씬이 변경되어도 데이터가 메모리에 유지됩니다.
    private static string NextSceneName;

    [Header("UI 연결")]
    [SerializeField] TextMeshProUGUI LoadingText;
    [SerializeField] Slider LoadingBar;
    [SerializeField] CanvasGroup loadingCavasGroup;

    [Header("설정")]
    [SerializeField] float minLoadingTime = 1.0f;

    static Vector3 targetPos;

    private void Start()
    {
        // 정상적으로 목표 씬 이름이 입력되었는지 확인 후 코루틴 실행
        if(!string.IsNullOrEmpty(NextSceneName))
        {
            StartCoroutine(LoadSceneAsync());
            StartCoroutine(AniLoadingText());
        }
    }
    /// <summary>
    /// 외부 스크립트에서 호출하여 로딩 씬을 시작하는 함수
    /// </summary>
    /// <param name="sceneName"></param>
    /// <param name="pos"></param>
    public static void LoadScene(string sceneName, Vector3 pos)
    {
        NextSceneName = sceneName;
        targetPos = pos;
        SceneManager.LoadScene("Loading");
    }

    /// <summary>
    /// 로딩 텍스트의 점 개수를 로딩 동작 중임을 연출
    /// </summary>
    /// <returns></returns>
    private IEnumerator AniLoadingText()
    {
        string baseText = "LOADING";
        int dotCount = 0;

        while(true)
        {
            // 1 -> 2 -> 3 순환
            dotCount = (dotCount % 3) + 1;
            if(LoadingText != null)
            {
                LoadingText.text = baseText + new string('.', dotCount);
            }
            yield return new WaitForSeconds(0.4f);
        }
    }

    /// <summary>
    /// 씬 전환
    /// </summary>
    /// <param name="targetScene"></param>
    /// <returns></returns>
    private IEnumerator LoadSceneAsync()
    {
        // UI를 덮고 있던 패널을 거서 로딩화면이 보이게 만듭니다.
        if(UiManager.Instance != null)
        {
            UiManager.Instance.ResetFade();
        }

        // 다음 씬을 Additive(중첩) 모드로 비동기 로드 시작
        // 로딩 씬이 파괴되지 않고 유치된 상태로 다음 씬을 불러옵니다.
        AsyncOperation op = SceneManager.LoadSceneAsync(NextSceneName, LoadSceneMode.Additive);

        // 씬 로딩이 100% 끝나도 바로 화면을 교체하지 않도록 자동 활성화 차단
        op.allowSceneActivation = false;

        float timer = 0.0f;

        // 최소 로딩 시간 및 비동기 데이터 수집 대기
        while(!op.isDone)
        {
            yield return null;
            timer += Time.deltaTime;

            // allowSceneActivation이 false일 때 op.progress는 0.0 ~ 0.9 까지만 상승합니다.
            // 이를 0.0 ~ 1.0(100%) 게이지 비율로 정규화 보정합니다.
            float progress = Mathf.Clamp01(op.progress / 0.9f);

            if(LoadingBar != null)
            {
                LoadingBar.value = progress;
            }

            // 다음 씬 로딩 완료이고 최소 로딩 시간이 충족되면 루프 탈출
            if(op.progress >= 0.9f && timer >= minLoadingTime)
            {
                break;
            }
        }

        // 다음 씬 진행
        op.allowSceneActivation = true;

        // 다음 씬의 메모리 로드가 완전히 마무리될 때까지 대기
        while(!op.isDone)
        {
            yield return null;
        }
        
        // 플레이어의 위치이동을 위해 값을 전달하고 위치 이동
        GameObject playerobj = GameObject.FindWithTag("Player");
        Player_CC player = playerobj.GetComponent<Player_CC>();
        if(player != null)
        {
            player.Teleport(targetPos);
        }

        // 불러온 씬을 유니티의 '주 씬(Active Scene)'으로 설정합니다.
        // (조명, 스카이박스, 네비메시 기준이 다음 씬으로 지정됩니다.)
        Scene nextScene = SceneManager.GetSceneByName(NextSceneName);
        if(nextScene.IsValid())
        {
            SceneManager.SetActiveScene(nextScene);
        }

        // 다음 씬의 첫 프레임 그래픽 렌더링 완료 대기
        // 다음 씬의 모든 스크립트 실행이 완료되고,
        // 그래픽 렌더링 결과물이 화면 프레임에 그려질 때까지 대기합니다.
        yield return new WaitForEndOfFrame();
        yield return null;

        // 로딩 화면 페이드 아웃 후 로딩 씬 제거
        if(loadingCavasGroup != null)
        {
            float fadeDuration = 0.3f;
            float fadeTimer = 0f;

            while(fadeTimer < fadeDuration)
            {
                fadeTimer += Time.deltaTime;
                loadingCavasGroup.alpha = Mathf.Lerp(1f, 0f, fadeTimer / fadeDuration);
                yield return null;
            }
        }
        //플레이어 입력 활성화
        player.SetInputState(true);

        // 다음 씬의 렌더링이 완전히 끝났으므로 기존 로딩씬 삭제
        SceneManager.UnloadSceneAsync("Loading");
    }
}
