using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

/// <summary>
/// 몬스터 스폰 및 리스폰 관리자
/// 지정된 트리거 영역 내에서 NavMesh 위치를 검사하여 오브젝트 풀 방식으로 몬스터를 스폰합니다.
/// </summary>
public class MonsterSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField, Tooltip("소환할 몬스터 프리팹")] GameObject monsterPrefab;
    [SerializeField, Tooltip("몬스터 순찰 및 스폰 범위")] Collider spawnAreaCollider;
    [SerializeField, Tooltip("최초 씬 시작시 생성할 몬스터 수량")] int initialCount = 5;
    [SerializeField, Tooltip("오브젝트 풀에서 관리할 최대 몬스터 수량")] int maxCapacity = 10;
    [SerializeField, Tooltip("사망 모션 재생 후 풀 수납 대기시간")] float deathDelay = 3f;
    [SerializeField, Tooltip("리스폰 대기시간")] float respawnDelay = 5f;

    [Header("NavMesh 샘플링 설정")]
    [SerializeField, Tooltip("무작위 좌표 추출 시 NavMesh 지형을 인식할 탐색 반경")] float navMeshSampleDistance = 2f;

    private IObjectPool<GameObject> monsterPool;

    private void Awake()
    {
        // 유니티 내장 ObjectPool 초기화
        monsterPool = new ObjectPool<GameObject>(
            createFunc: CreateMonster,
            actionOnGet: OnGetMonster,
            actionOnRelease: OnReleaseMonster,
            actionOnDestroy: OndestroyMonster,
            defaultCapacity: initialCount,
            maxSize: maxCapacity
            );
    }

    private void Start()
    {
        // 초기 배치
        for( int i = 0; i < initialCount; i++ )
        {
            SpawnMonster();
        }
    }

    private GameObject CreateMonster()
    {
        monsterPrefab.SetActive(false);
        GameObject obj = Instantiate(monsterPrefab, transform);
        monsterPrefab.SetActive(true);
        return obj;
    }
    private void OnGetMonster(GameObject obj)
    {
        obj.SetActive(true);
    }
    private void OnReleaseMonster(GameObject obj)
    {
        obj.SetActive(false);
    }
    private void OndestroyMonster(GameObject obj)
    {
        Destroy(obj);
    }

    /// <summary>
    /// NavMesh 검사 후 풀에서 몬스터 추출 및 배치
    /// </summary>
    public void SpawnMonster()
    {
        // 스폰 영역 내에서 유효한 NavMesh 좌표 검색
        if(TryGetNavMeshPosition(out Vector3 spawnPos))
        {
            // 오브젝트 풀에서 몬스터 가져오기
            GameObject monsterObj = monsterPool.Get();

            monsterObj.transform.position = spawnPos;

            monsterObj.SetActive(true);

            // NavMeshAgent Warp 처리로 위치 이동 버그 방지
            if (monsterObj.TryGetComponent<NavMeshAgent>(out var agent))
            {
                agent.enabled = true;
                agent.Warp(spawnPos);
            }

            // AI 스크립트 및 순찰 영역 할당
            if (monsterObj.TryGetComponent<MonsterAIController>(out var ai))
            {
                ai.SetPatrolRange(spawnAreaCollider);
            }

            // spawnAreaCollider에 MonsterSensor 컴포넌트가 붙어있다면 전달
            if(monsterObj.TryGetComponent<MonsterDetect>(out var detect))
            {
                if(spawnAreaCollider.TryGetComponent<MonsterSensor>(out var sensor))
                {
                    detect.SetPatrolSensor(sensor);
                }
            }

            // AI 및 스탯 상태 초기화
            if(ai != null)
            {
                ai.InitAI();
            }

            // 사망 이벤트 일회성 구독 연동
            if(monsterObj.TryGetComponent<MonsterStats>(out var stats))
            {
                Action death = null;
                death = () =>
                {
                    // 이벤트 중복 호출 및 메모리 누수를 막기 위해 구독 해제
                    stats.OnDeath -= death;
                    StartCoroutine(DespawnAndRespawnRountine(monsterObj));
                };
                stats.OnDeath += death;
            }
        }
        else
        {
            Debug.LogWarning("[MonsterSpawner] 유효한 NavMesh 스폰 위치를 찾지 못했습니다.");
        }
    }

    /// <summary>
    /// 콜라이더 트리거 범위 내 무작위 좌표 추출 후 유효 NavMesh 검사
    /// </summary>
    /// <param name="resultPos"></param>
    /// <returns></returns>
    private bool TryGetNavMeshPosition(out Vector3 resultPos)
    {
        Bounds bounds = spawnAreaCollider.bounds;
        // 최대 재시도 횟수
        int maxAttempts = 30;

        for( int i = 0;i < maxAttempts; i++ )
        {
            // 콜라이더 Bounds 최소/최대 범위 안에서 무작위 x, z 추출
            Vector3 randomPoint = new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                bounds.min.y,
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
                );

            // 해당 좌표가 유효한 NavMesh 영역내에 있는지 확인
            if(NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                resultPos = hit.position;
                return true;
            }
        }

        resultPos = Vector3.zero;
        return false;
    }

    /// <summary>
    /// 사망 애니메이션 -> 풀 수납 -> 리스폰 대기 -> 재소환 흐름 제어
    /// </summary>
    /// <param name="monsterObj"></param>
    /// <returns></returns>
    private IEnumerator DespawnAndRespawnRountine(GameObject monsterObj)
    {
        // 사망 모션이 끝날 때까지 대기
        yield return new WaitForSeconds(deathDelay);

        // 오브젝트 풀로 반환
        monsterPool.Release(monsterObj);

        // 리스폰 시간 대기
        yield return new WaitForSeconds(respawnDelay);

        // 재소환
        SpawnMonster();
    }
}
