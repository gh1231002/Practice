using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TitleScreenController : MonoBehaviour
{
    [Header("초기 화면 UI")]
    [SerializeField] CanvasGroup TitleScreenCanvasGroup;
    
    [Header("메인 메뉴 화면 UI")]
    [SerializeField] CanvasGroup mainMenuScreenCanvasGroup;

    [Header("화면 전환 설정")]
    [SerializeField] float fadeOutDuration = 0.5f;
    [SerializeField] float fadeInDuration = 0.7f;

    [Header("메뉴 화면 버튼들")]
    [SerializeField] Button newStartButton;
    [SerializeField] Button LoadButton;
    
    // 최초 화면 상태에서 메뉴화면으로 전환했는지 확인
    bool hasEnterMenu;


    void Start()
    {
        // CanvasGroup 알파값 설정
        TitleScreenCanvasGroup.alpha = 1f;
        mainMenuScreenCanvasGroup.alpha = 0f;
    }

    void Update()
    {
        // 이미 메뉴 화면으로 전환했다면 입력을 다시 처리하지 않음
        if(hasEnterMenu) return;

        // 키보드 아무 키 입력 또는 마우스 클릭 감지
        if(Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame ||
            Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            hasEnterMenu = true;
            StartCoroutine(TransitionToMenu());
        }
    }

    IEnumerator TransitionToMenu()
    {
        // 초기 화면 페이드 아웃
        yield return StartCoroutine(FadeCanvasGroup(TitleScreenCanvasGroup, 1f, 0f, fadeOutDuration));

        // 메뉴 화면 페이드 인
        yield return StartCoroutine(FadeCanvasGroup(mainMenuScreenCanvasGroup, 0f, 1f, fadeInDuration));
    }

    IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float targetAlpha, float duration)
    {
        // 페이드 시작값 설정
        canvasGroup.alpha = startAlpha;

        // 전환 시간이 0 이하라며 즉시 변경
        if(duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsedTime = 0f;

        // 지정한 시간 동안 Alpha를 부드럽게 변경
        while(elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
             
            // 부드러운 페이드 효과 적용
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null; // 다음 프레임까지 대기
        }

        // 마지막 프레임의 Alpha를 정확하게 설정
        canvasGroup.alpha = targetAlpha;
    }
}
