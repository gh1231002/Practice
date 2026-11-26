using System.Collections;
using TMPro;
using UnityEngine;

public class TitlePromptBlink : MonoBehaviour
{
    [Header("투명도 설정")]
    [SerializeField] float minAlpha = 0.15f;
    [SerializeField] float maxAlpha = 1f;

    [Header("페이드 설정")]
    [SerializeField] float fadeDuration = 1f;

    // 게임 시간(Time.timeScale)이 변경되더라도
    // 타이틀 텍스트의 연출은 정상적으로 진행되도록 설정
    [SerializeField] bool useUnscaledTime = true;

    // 원래 텍스트 색상을 보존하기 위한 변수
    Color originalColor;

    // 텍스트 컴포넌트
    TMP_Text promptText;

    void Start()
    {
        // 현재 오브젝트에서 TMP_Text 컴포넌트 가져오기
        promptText = GetComponent<TMP_Text>();

        // TMP_Text 컴포넌트를 찾지 못한 경우 실행 중단
        if (promptText == null)
        {
            Debug.LogWarning("현재 오브젝트에서 TMP_Text 컴포넌트를 찾을 수 없습니다.", this);
            return;
        }

        // 원래 텍스트 색상을 저장
        originalColor = promptText.color;

        // 처음에는 최소 투명도로 시작
        SetAlpha(minAlpha);
        // 투명도 반복 변경 Coroutine 시작
        StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        while (true)
        {
            // 최대 투명도로 페이드 아웃
            yield return FadeAlpha(minAlpha, maxAlpha);

            // 최소 투명도로 페이드 인
            yield return FadeAlpha(maxAlpha, minAlpha);
        }
    }

    IEnumerator FadeAlpha(float startAlpha, float endAlpha)
    {
        float elapsedTime = 0f;

        while(elapsedTime < fadeDuration)
        {
            // 게임 시간과 무관하게 진행하거나 일반 게임 시간을 사용
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            elapsedTime += deltaTime;

            // 0~1 범위의 진행률 계산
            float progress = Mathf.Clamp01(elapsedTime / fadeDuration);

            // 시작 투명도와 목표 투명도 사이를 부드럽게 보간
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, progress);

            SetAlpha(currentAlpha);
            yield return null;
        }

        // 페이드 종료 시 정확한 목표 투명도로 설정
        SetAlpha(endAlpha);
    }

    private void SetAlpha(float alpha)
    {
        // 기존 RGB 색상은 유지하고 Alpha 값만 변경
        Color newColor = originalColor;
        newColor.a = alpha;
        promptText.color = newColor;
    }
}
