using UnityEngine;
using UnityEngine.Pool;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance {  get; private set; }

    [SerializeField] DamageText damageTextPrefab;
    IObjectPool<DamageText> textPool;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        textPool = new ObjectPool<DamageText>(
            createFunc: () => Instantiate(damageTextPrefab, transform),
            actionOnGet: (text) => text.gameObject.SetActive(true),
            actionOnRelease: (text) => text.gameObject.SetActive(false),
            actionOnDestroy: (text) => Destroy(text.gameObject),
            collectionCheck: true,
            defaultCapacity: 20,
            maxSize: 50
            );
    }

    public void ShowDamageText(Vector3 spawnPos, float damage)
    {
        DamageText textObj = textPool.Get();

        // 랜덤 위치 오프셋을 주어 연속 타격 시 텍스트가 겹치지 않게
        Vector3 randomOffset = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.8f, 1.2f), Random.Range(-0.2f, 0.2f));
        textObj.transform.position = spawnPos + randomOffset;

        textObj.Setup(damage, textPool);
    }
}
