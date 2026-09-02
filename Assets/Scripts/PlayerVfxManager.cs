using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class PlayerVfxManager : MonoBehaviour
{
    [Header("이펙트 프리팹 및 소켓 위치")]
    [SerializeField] ParticleSystem HealParticle;
    [SerializeField] Transform ChestSocket;

    Player_CC player;
    IObjectPool<ParticleSystem> healPool;

    private void Awake()
    {
        player = GetComponent<Player_CC>();

        // 유니티 ObjectPool 초기화
        healPool = new ObjectPool<ParticleSystem>(
            createFunc: CreateParticle, // 풀에 오브젝트가 없을 때 생성할 함수
            actionOnGet: OnGetParticle, // Get() 호출 시 실행할 로직
            actionOnRelease: OnReleaseParticle, // Release() 호출 시 실행할 로직
            actionOnDestroy: OnDestroyParticle, // maxSize 초과시 파괴 로직
            collectionCheck: true, // 중복 반납 검사
            defaultCapacity: 2, // 기본 생성 개수
            maxSize: 5); // 최대 보관 개수
    }

    // 오브젝트 풀 콜백 함수들
    private ParticleSystem CreateParticle()
    {
        // 소켓의 자식을 생성하여 위치와 회전값을 0으로 맞춤
        ParticleSystem ps = Instantiate(HealParticle, ChestSocket);
        ps.transform.localPosition = Vector3.zero;
        ps.transform.localRotation = Quaternion.identity;
        return ps;
    }

    private void OnGetParticle(ParticleSystem ps)
    {
        ps.gameObject.SetActive(true);
        ps.Play();
    }

    private void OnReleaseParticle(ParticleSystem ps)
    {
        // 삭제 대신 비활성화 후 풀로 회수
        ps.gameObject.SetActive(false);
    }

    private void OnDestroyParticle(ParticleSystem ps)
    {
        // maxSize 초과 시 메모리 해제
        Destroy(ps.gameObject);
    }

    private void OnEnable()
    {
        if(player != null)
        {
            player.OnHeal += PlayHealVfx;
        }
    }

    private void OnDisable()
    {
        if(player != null)
        {
            player.OnHeal -= PlayHealVfx;
        }
    }

    private void PlayHealVfx()
    {
        if(HealParticle != null)
        {
            // 오브젝트 풀에서 꺼내어 이펙트 재생
            ParticleSystem ps = healPool.Get();

            // 이펙트 재생 완료 후 반납
            StartCoroutine(AutoRelease());

            IEnumerator AutoRelease()
            {
                float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                yield return new WaitForSeconds(duration);

                healPool.Release(ps);
            }
        }
    }
}
