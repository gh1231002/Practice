using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

public class DamageText : MonoBehaviour
{
    [Header("텍스트 설정")]
    [SerializeField, Tooltip("텍스트 유지 시간")] float Duration;
    [SerializeField, Tooltip("텍스트가 올라가는 높이")] float RisingHeight;


    TextMeshPro textMesh;
    Color color;
    IObjectPool<DamageText> targetPool;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        color = textMesh.color;
    }

    public void Setup(float damageAmount, IObjectPool<DamageText> pool)
    {
        targetPool = pool;

        textMesh.text = Mathf.RoundToInt(damageAmount).ToString();
        textMesh.color = color; // 색상/투명도 초기화

        StartCoroutine(AnimateRoutine());
    }

    /// <summary>
    /// 프리팹 생성 후 위로 올라가는 연출을 위한 코루틴
    /// </summary>
    /// <returns></returns>
    private IEnumerator AnimateRoutine()
    {
        float duration = Duration;
        float timer = 0f;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * RisingHeight;

        while(timer < duration)
        {
            // 항상 카메라를 바라보도록 설정
            if(Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }

            // 위로 이동
            transform.position = Vector3.Lerp(startPos, targetPos, timer / duration);

            // 알파값 페이드 아웃
            Color c = textMesh.color;
            c.a = Mathf.Lerp(1f, 0f, timer / duration);
            textMesh.color = c;

            timer += Time.deltaTime;
            yield return null;
        }

        // 연출 완료 후 오브젝트 비활성화
        // 오브젝트 풀로 반납
        targetPool?.Release(this);
    }
}
