using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 전용 전투 및 히트박스 모듈
/// 공격 범위 판정, 피격 대상에게 공격에 따른 데미지 전달
/// </summary>
public class BossCombat : MonoBehaviour
{
    // 바위 하나의 기즈모 정보를 저장하는 구조체
    private struct RockSpikeGizmoData
    {
        // 파티클의 원래 위치
        public Vector3 particlePosition;

        // Pivot을 적용한 메시의 기준 위치
        public Vector3 renderedMeshPosition;

        // 바위와 지면이 만나는 위치
        public Vector3 groundPoint;

        // 충돌 캡슐의 아래쪽과 위쪽 위치
        public Vector3 capsuleBottom;
        public Vector3 capsuleTop;

        // 바위의 성장 방향과 충돌 반지름
        public Vector3 growthDir;
        public float radius;
    }
    // 현재 활성화된 모든 바위의 기즈모 정보를 저장한다.
    private List<RockSpikeGizmoData> rockSpikeGizmos = new List<RockSpikeGizmoData>();

    [Header("공격 관련 설정")]
    [SerializeField, Tooltip("왼손 공격 포인트")] Transform leftHandAtkPoint;
    [SerializeField, Tooltip("오른손 공격 포인트")] Transform rightHandAtkPoint;
    [SerializeField, Tooltip("내려찍기 공격 포인트")] Transform stompAtkPoint;
    [SerializeField, Tooltip("회전 공격 포인트")] Transform spinAtkPoint;
    [SerializeField, Tooltip("점프 공격 포인트")] Transform jumpAtkPoint;

    [SerializeField, Tooltip("왼손 임팩트 범위 반지름")] float leftHandRadius;
    [SerializeField, Tooltip("오른손 임팩트 범위 반지름")] float rightHandRadius;
    [SerializeField, Tooltip("내려찍기 임팩트 범위 반지름")] float stompRadius;
    [SerializeField, Tooltip("회전 공격 임팩트 범위 반지름")] float rotationRadius;
    [SerializeField, Tooltip("점프 공격 임팩트 범위 반지름")] float jumpRadius;

    [SerializeField, Tooltip("공격 가능 사거리")] float atkDistance;
    [SerializeField, Tooltip("타격 대상 레이어")] LayerMask targetLayer;
    [SerializeField, Tooltip("지면 레이어")] LayerMask groundLayer;
    [SerializeField, Tooltip("파티클 위치를 읽고 피해 판정을 수행할 시간")] float rockSpikeDamageWindow;
    [SerializeField, Tooltip("바위가 지면을 찾을 때 사용할 탐색 거리")] float groundRayDistance;

    [Header("공격별 피해 배율 설정")]
    [SerializeField, Min(0f)]
    float stompDamageMultiplier;
    [SerializeField, Min(0f)]
    float jumpDamageMultiplier;
    [SerializeField, Min(0f)]
    float spinDamageMultiplier;
    [SerializeField, Min(0f), Tooltip("대지 분쇄 파티클이 대상에게 줄 기본 공격력의 배율")]
    float rockSpikeDamageMultiplier;

    // 애니메이션 이벤트에서 타수별 피해 비율 값을 저장한다.
    float comboDamageMultiplier;

    [Header("경고 원 스프라이트")]
    [SerializeField] GameObject warningCirclePrefab;
    [SerializeField, Tooltip("바닥과 겹쳐 깜빡이는 현상 방지")] float warningGroundOffset;

    [Header("분노 패턴 - 점프 충격파 프리팹")]
    [SerializeField] GameObject shockWavePrefab;

    [Header("분노 패턴 - 대지 분쇄 프리팹")]
    [SerializeField] GameObject earthShatter;

    // 대지 분쇄 프리팹 내부 RockSpike 파티클 시스템 참조
    ParticleSystem rockSpikeParticle;
    // 현재 파티클 위치들을 임시 저장할 배열
    ParticleSystem.Particle[] rockSpikeParticles;
    // RockSpike의 MeshRenderer를 캐싱한다
    ParticleSystemRenderer rockSpikeRenderer;
    
    public float AtkDistance => atkDistance;

    float currentAtkPower;

    // 실제 플레이에서 사용할 프리팹 복사본들
    GameObject warningCircleInstance;
    GameObject shockWaveInstance;
    GameObject earthShatterInstance;

    // 피해 판정이 활성화되어 있는지 확인한다.
    bool isSpinDamageActive;
    bool isComboDamageActive;

    // 점프 공격 시작 시 정한 착지 예정 위치를 저장한다.
    // 경고 원과 실제 피해 판정에서 같은 좌표를 사용하기 위함이다.
    Vector3 jumpLandingPos;

    // 착지 위치가 정상적으로 설정되었는지 확인한다.
    // 위치가 설정되지 않았는데 피해가 발생하는 것을 방지한다.
    bool hasJumpLandingPos;

    BossAttackPattern attackPattern;

    BossAIController bossAi;

    // 이미 피해를 받은 대상을 기록한다.
    // 같은 대상의 콜라이더가 여러 개여도 중복 피해를 막기 위한 용도이다.
    readonly HashSet<ITakeDamageWithReaction> jumpAndStompAttackTargets = new HashSet<ITakeDamageWithReaction>();
    readonly HashSet<ITakeDamageWithReaction> comboTargets = new HashSet<ITakeDamageWithReaction>();
    readonly HashSet<ITakeDamageWithReaction> spinTargets = new HashSet<ITakeDamageWithReaction>();

    private void Awake()
    {
        // Inspector에 프리팹이 연결되어 있는지 확인한다.
        if (warningCirclePrefab == null)
        {
            Debug.LogWarning("[BossCombat] 내려찍기 예고 원 프리팹이 연결되지 않았습니다.", this);
            return;
        }
        if(shockWavePrefab == null)
        {
            Debug.LogWarning("[BossCombat] 충격파 프리팹이 연결되지 않았습니다.", this);
            return;
        }
        if(earthShatter == null)
        {
            Debug.LogWarning("[BossCombat] 지면 분쇄 프리팹이 연결되지 않았습니다.", this);
            return;
        }

        bossAi = GetComponent<BossAIController>();

        // 프리팹을 현재 씬에 한번만 생성한다.
        // 부모를 지정하지 않아 분리된 오브젝트로 유지한다.
        warningCircleInstance = Instantiate(warningCirclePrefab);
        shockWaveInstance = Instantiate(shockWavePrefab);
        earthShatterInstance = Instantiate(earthShatter);

        // EarthShatter 자식 중 RockSpike인 오브젝트를 찾는다
        Transform rockSpikeTrs = earthShatterInstance.transform.Find("RockSpike");
        // ParticleSystem 컴포넌트를 가져온다
        if(rockSpikeTrs != null)
        {
            rockSpikeParticle = rockSpikeTrs.GetComponent<ParticleSystem>();

            // 매 프레임 Renderer를 다시 찾지 않도록 미리 저장한다
            if(rockSpikeParticle != null)
            {
                rockSpikeRenderer = rockSpikeParticle.GetComponent<ParticleSystemRenderer>();
            }
        }
        // 프리팹 구조나 이름이 다르면 로그 출력
        if(rockSpikeParticle == null || rockSpikeRenderer == null)
        {
            Debug.LogWarning("[BossCombat] EarthShatter 프리팹에서 RockSpike 파티클 시스템 또는 Renderer 찾지 못했습니다.", this);
        }

        // Hierarchy에서 원본과 실행 중 인스턴스를 구별하기 위한 이름설정
        warningCircleInstance.name = warningCirclePrefab.name + "_Runtime";
        shockWaveInstance.name = shockWavePrefab.name + "_Runtime";
        earthShatterInstance.name = earthShatter.name + "_Runtime";

        // 게임 시작 시에는 화면에 나타나지 않도록 비활성화한다.
        warningCircleInstance.SetActive(false);
        shockWaveInstance.SetActive(false);
        earthShatterInstance.SetActive(false);
    }

    /// <summary>
    /// AIController에서 초기화 시 호출
    /// </summary>
    /// <param name="defaultAtkPower"></param>
    public void Init(float defaultAtkPower)
    {
        currentAtkPower = defaultAtkPower;
    }

    /// <summary>
    /// 경고 원 프리팹을 배치할때 호출한다.
    /// 원의 크기, 표시 설정
    /// </summary>
    public void SetWarningCircle(BossAttackPattern attackType, Transform targetPlayer = null)
    {
        // 경고 원 오브젝트가 없다면 실행하지 않는다.
        if (warningCircleInstance == null) return;

        // 공격 별 위치과 반경을 저장한 다음 공통 처리한다.
        Vector3 warningPos = Vector3.zero;
        float waringRadius = 0f;
        // 예고 원을 공격 타입에 따라 위치로 이동한다.
        switch (attackType)
        {
            case BossAttackPattern.StompAttack:
                warningPos = stompAtkPoint.position;
                waringRadius = stompRadius;
                break;

            case BossAttackPattern.JumpAttack:
                if(targetPlayer == null)
                {
                    hasJumpLandingPos = false;
                    warningCircleInstance.SetActive(false);
                    return;
                }

                // 플레이어 위치를 점프 공격 착지 예정 위치로 사용한다.
                // 실제 피해 판정에서도 이 좌표를 재사용한다.
                jumpLandingPos = targetPlayer.position;
                hasJumpLandingPos = true;

                warningPos = jumpLandingPos;
                waringRadius = jumpRadius;
                break;
        }

        // 경고 원 표시 위치만 높이 오프셋을 적용한다.
        // 실제 피해 판정 좌표에는 이 오프셋을 더하지 않는다.
        warningPos.y += warningGroundOffset;
        warningCircleInstance.transform.position = warningPos;

        // 원 지름을 계산
        float diameter = waringRadius * 2f;

        // Scale 값 변경 후 적용
        Vector3 scale = warningCircleInstance.transform.localScale;
        scale.x = diameter;
        scale.y = diameter;
        warningCircleInstance.transform.localScale = scale;

        // 경고 원 활성화
        warningCircleInstance.SetActive(true);
    }

    /// <summary>
    /// 검출된 콜라이더를 대상으로 피해를 적용한다.
    /// 지정된 공격 구간에서 같은 대상은 한 번만 피해를 받는다.
    /// </summary>
    private void ApplyDamageToTargets(Collider[] hitColliders, HashSet<ITakeDamageWithReaction> hitTargets, float damageMultiplier, HitReactionType reactionType = HitReactionType.Normal)
    {
        foreach (Collider col in hitColliders)
        {
            // 콜라이더가 자식에 있더라도 부모에서 피해 처리 인터페이스를 찾는다.
            ITakeDamageWithReaction damage = col.GetComponentInParent<ITakeDamageWithReaction>();

            // 피해 처리 인터페이스가 없으면 건너뛴다.
            if (damage == null)
                continue;

            // HashSet.Add()는 새 대상이면 true, 이미 등록된 대상이면 false를 반환한다.
            // 따라서 같은 구간에서 동일 대상에게 중복 피해를 주지 않는다.
            if (!hitTargets.Add(damage))
                continue;

            // 기본 공격력과 해당 공격의 배율을 적용한 피해를 전달한다.
            float finalDamage = currentAtkPower * damageMultiplier;

            // 피격 반응을 지원하는지에 따라 구분하여 데미지 전달
            if(damage is ITakeDamageWithReaction reactionTarget)
            {
                reactionTarget.TakeDamage(gameObject, finalDamage, reactionType);
            }
        }
    }

    /// <summary>
    /// 피해 판정이 일어나는 시점에 호출되어 경고 원을 비활성화한다.
    /// </summary>
    public void HideWarningCircle()
    {
        if(warningCircleInstance != null)
        {
            warningCircleInstance.SetActive(false);
        }
    }

    // 애니메이션이 반영된 뒤의 위치를 확인하기 위함
    private void LateUpdate()
    {
        if(isComboDamageActive)
        {
            CheckComboDamage();
        }
        if(isSpinDamageActive && spinAtkPoint != null)
        {
            Collider[] hits = Physics.OverlapSphere(spinAtkPoint.position, rotationRadius, targetLayer);
            ApplyDamageToTargets(hits, spinTargets, spinDamageMultiplier, HitReactionType.Knockback);
        }
    }

    /// <summary>
    /// 현재 콤보 공격 종류에 맞는 충돌 판정을 실행한다.
    /// </summary>
    private void CheckComboDamage()
    {
        if (leftHandAtkPoint == null || rightHandAtkPoint == null) return;

        Collider[] hits;

        // 현재 타격 종류에 따라 올바른 공격 포인트와 반경을 선택한다.
        switch (attackPattern)
        {
            case BossAttackPattern.leftHandAttack:
                hits = Physics.OverlapSphere(
                    leftHandAtkPoint.position,
                    leftHandRadius,
                    targetLayer
                );
                break;

            case BossAttackPattern.RightHandAttack:
                hits = Physics.OverlapSphere(
                    rightHandAtkPoint.position,
                    rightHandRadius,
                    targetLayer
                );
                break;

            default:
                // 지원하지 않는 공격 유형이면 피해 판정을 실행하지 않는다.
                return;
        }

        // 콤보 공격 전용 대상 목록과 피해 배율을 사용한다.
        ApplyDamageToTargets(hits, comboTargets, comboDamageMultiplier);
    }

    // 에디터 씬에서 공격 범위를 시각화 하는 기즈모 함수
    private void OnDrawGizmosSelected()
    {
        if(leftHandAtkPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(leftHandAtkPoint.position, leftHandRadius);
        }
        if(rightHandAtkPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(rightHandAtkPoint.position, rightHandRadius);
        }
        if(stompAtkPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(stompAtkPoint.position, stompRadius);
        }
        if(spinAtkPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(spinAtkPoint.position, rotationRadius);
        }
        if(jumpAtkPoint != null)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(jumpAtkPoint.position, jumpRadius);
        }

        // 대지 분쇄 기즈모
        foreach(RockSpikeGizmoData data in rockSpikeGizmos)
        {
            Vector3 axis = data.growthDir.normalized;
            Vector3 side = Vector3.Cross(axis, Vector3.up);
            if(side.sqrMagnitude < 0.001f)
            {
                side = Vector3.Cross(axis, Vector3.right);
            }
            side.Normalize();

            Vector3 otherSide = Vector3.Cross(axis, side).normalized;

            // 충돌 캡슐의 양쪽 끝을 자홍색으로 표시한다.
            Gizmos.color = Color.magenta;

            Gizmos.DrawWireSphere(data.capsuleBottom, data.radius);
            Gizmos.DrawWireSphere(data.capsuleTop, data.radius);

            // 파티클의 원래 위치를 노란색으로 표시한다.
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(data.particlePosition, 0.2f);

            // Pivot을 적용한 메시의 기준 위치를 주황색으로 표시한다.
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawSphere(data.renderedMeshPosition, 0.2f);

            // 실제 지면과 만나는 지점을 흰색으로 표시한다.
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(data.groundPoint, 0.15f);

            // 캡슐의 옆면을 네 방향의 선으로 표시한다.
            Vector3 sideOffset = side * data.radius;
            Vector3 otherSideOffset = otherSide * data.radius;

            Gizmos.color = Color.magenta;

            Gizmos.DrawLine(data.capsuleBottom + sideOffset, data.capsuleTop + sideOffset);

            Gizmos.DrawLine(data.capsuleBottom - sideOffset, data.capsuleTop - sideOffset);

            Gizmos.DrawLine(data.capsuleBottom + otherSideOffset, data.capsuleTop + otherSideOffset);

            Gizmos.DrawLine(data.capsuleBottom - otherSideOffset, data.capsuleTop - otherSideOffset);
        }
    }

    private void OnDestroy()
    {
        if(warningCircleInstance != null)
        {
            Destroy(warningCircleInstance);
        }
    }

    // 애니메이션 이벤트 함수
    // 점프 공격
    public void OnJumpLanding()
    {
        jumpAndStompAttackTargets.Clear();
        attackPattern = BossAttackPattern.JumpAttack;
        JumpImpact();

        // 이번 공격이 광폭화 상태에서 시작한 경우에만 충격파를 재생한다.
        if(bossAi != null && bossAi.IsCurrentAttackEnraged)
        {
            StartCoroutine(PlayShockWaveRoutine());
        }
        HideWarningCircle();
    }

    /// <summary>
    /// 점프공격 애니메이션의 실제 충격 프레임에서 호출한다.
    /// </summary>
    private void JumpImpact()
    {
        // 착지 위치가 설정되지 않았다면 실행하지 않는다.
        if(!hasJumpLandingPos) return;

        Collider[] hits = Physics.OverlapSphere(jumpLandingPos, jumpRadius, targetLayer);
        ApplyDamageToTargets(hits, jumpAndStompAttackTargets, jumpDamageMultiplier, HitReactionType.Knockback);

        // 같은 착지 위치가 다음 공격에서 잘못 재사용되지 않도록 정리한다.
        hasJumpLandingPos = false;
    }

    // 내려찍기 공격
    public void OnStompLanding()
    {
        jumpAndStompAttackTargets.Clear();
        attackPattern = BossAttackPattern.StompAttack;
        StompImpact();
        HideWarningCircle();
        // 이번 공격이 광폭화 상태라면 대지 분쇄 패턴 추가
        if(bossAi != null && bossAi.IsCurrentAttackEnraged)
        {
            StartCoroutine(PlayEarthShatterRoutine());
        }
    }

    /// <summary>
    /// 내려찍기 애니메이션의 실제 충격 프레임에서 호출한다.
    /// </summary>
    private void StompImpact()
    {
        // 공격 포인트가 연결되지 않았다면 실행하지 않는다.
        if(stompAtkPoint == null) return;

        Collider[] hits = Physics.OverlapSphere(stompAtkPoint.position, stompRadius, targetLayer);
        // 피해 함수 호출
        ApplyDamageToTargets(hits, jumpAndStompAttackTargets, stompDamageMultiplier, HitReactionType.Knockback);
    }

    /// <summary>
    /// 대지 분쇄 파티클을 재생하고
    /// 모든 파티클이 재생을 마치면 오브젝트를 비활성화한다.
    /// </summary>
    /// <returns></returns>
    private IEnumerator PlayEarthShatterRoutine()
    {
        // 인스펙터 누락시 오류 방지
        if(earthShatterInstance == null)
        {
            Debug.LogWarning("[BossCombat] 대지분쇄 오브젝트가 연결되지 않았습니다.");
            yield break;
        }

        earthShatterInstance.transform.SetPositionAndRotation(stompAtkPoint.position, transform.rotation);
        earthShatterInstance.SetActive(true);

        ParticleSystem particle = earthShatterInstance.GetComponent<ParticleSystem>();

        // 이전에 남아있는 파티클을 제거하고 새로 재생한다.
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play();
        // RockSpike 파티클을 찾았을 때만 위치 추적 코루틴 시작
        if(rockSpikeParticle != null)
        {
            // 파티클이 생성되는 위치를 읽어 피해 판정한다
            StartCoroutine(DetectRockSpikeDamage());
        }
        yield return null;

        // 루트와 자식 파티클의 재생이 모두 끝날 때까지 기다린다.
        while(particle.IsAlive(true))
        {
            yield return null;
        }
        earthShatterInstance.SetActive(false);
    }

    private IEnumerator DetectRockSpikeDamage()
    {
        // 설정된 시간 동안 RockSpike의 파티클 위치를 확인한다
        float elapsed = 0f;
        while(elapsed < rockSpikeDamageWindow)
        {
            if(rockSpikeParticle == null) yield break;
            // 현재 살아있는 파티클 수만큼 배열 공간을 준비한다
            int particleCount = rockSpikeParticle.particleCount;
            if(rockSpikeParticles == null || rockSpikeParticles.Length < particleCount)
            {
                rockSpikeParticles = new ParticleSystem.Particle[particleCount];
            }

            // 현재 살아있는 파티클 정보를 배열에 가져온다
            int count = rockSpikeParticle.GetParticles(rockSpikeParticles);
            // 파티클 위치가 월드 좌표인지 로컬 좌표인지 확인할때 사용한다
            var main = rockSpikeParticle.main;

            rockSpikeGizmos.Clear();

            for(int i = 0; i < count; i++)
            {
                ParticleSystem.Particle particle = rockSpikeParticles[i];

                // 파티클의 현재 위치를 가져온다
                Vector3 hitPos;

                // 파티클 시스템의 Simulation Space에 맞춰 월드 좌표로 변환한다
                switch(main.simulationSpace)
                {
                    case ParticleSystemSimulationSpace.World:
                    // 이미 월드 좌표이므로 그대로 사용한다
                    hitPos = particle.position;
                    break;

                    case ParticleSystemSimulationSpace.Custom:
                    // Custom 설정에 지정된 TransForm 기준 좌표를 월드 좌표로 바꾼다
                    hitPos = main.customSimulationSpace != null
                     ? main.customSimulationSpace.TransformPoint(particle.position)
                    // Custom Transform이 빠졌으면 파티클 Transform을 기준으로 처리한다.
                    : rockSpikeParticle.transform.TransformPoint(particle.position);
                    break;

                    default: // Local
                    // 파티클 시스템의 로컬 좌표를 월드 좌표로 바꾼다
                    hitPos = rockSpikeParticle.transform.TransformPoint(particle.position);
                    break;
                }

                // 파티클의 회전값을 이용해 바위의 성장 방향을 계산한다
                Vector3 localGrowthAxis = Vector3.right;
                Vector3 growthDir = Quaternion.Euler(particle.rotation3D) * localGrowthAxis;

                // 파티클 시뮬레이션 공간에 맞춰 월드 방향으로 반환한다
                switch(main.simulationSpace)
                {
                    case ParticleSystemSimulationSpace.World:
                    // 이미 월드 방향으로 계산한 값을 사용한다.
                    break;

                    case ParticleSystemSimulationSpace.Custom:
                        if (main.customSimulationSpace != null)
                        {
                            growthDir = main.customSimulationSpace.TransformDirection(growthDir);
                        }
                        else
                        {
                            growthDir = rockSpikeParticle.transform.TransformDirection(growthDir);
                        }
                        break;

                    default: // Local
                        growthDir = rockSpikeParticle.transform.TransformDirection(growthDir);
                        break;
                }
                growthDir.Normalize();

                // Renderer Pivot을 적용해 바위의 밑부분 위치를 계산한다
                Vector3 particleSize = particle.GetCurrentSize3D(rockSpikeParticle);
                // 메시의 원본 크기를 가져온다.
                Vector3 meshSize = rockSpikeRenderer.mesh.bounds.size;

                // 메시의 중심에서 X축 가장자리까지의 거리를 구한다.
                // 현재 메시에서는 X축이 바위의 성장 방향이다.
                float halfMeshLength = meshSize.x * 0.5f;

                // 메시의 가장 아래쪽에 해당하는 로컬 오프셋을 계산한다.
                // 기존 Renderer Pivot(-0.32) 대신 메시의 실제 절반 길이를 사용한다.
                Vector3 scaledPivot = new Vector3(-halfMeshLength * particleSize.x, 0f, 0f);

                // 파티클의 회전을 적용해 로컬 오프셋을 월드 방향으로 변환한다.
                Vector3 worldPivotOffset = Quaternion.Euler(particle.rotation3D) * scaledPivot;

                // 충돌 계산에 사용할 바위의 시작 위치를 구한다.
                Vector3 meshBasePos = hitPos + worldPivotOffset;

                // 바위의 전체 길이를 계산하고 꼭대기 위치를 구한다
                float spikeHeight = meshSize.x * particleSize.x;
                // Y/Z축 중 큰 값을 바위의 단면 크기로 사용한다
                float spikeWidth = Mathf.Max(meshSize.y * particleSize.y, meshSize.z * particleSize.z);
                Vector3 meshTopPos = meshBasePos + growthDir * spikeHeight;

                // 바위의 성장 방향을 따라 지면과 만나는 위치를 찾는다
                if(!TryGetRockGroundPoint(meshBasePos, growthDir, out Vector3 groundPoint)) continue;
                
                // 지면부터 바위 꼭대기까지 실제 노출된 길이를 계산한다
                float exposedLength = Vector3.Dot(meshTopPos - groundPoint, growthDir);

                // 지면 위로 노출된 영역이 없다면 충돌 판정을 생략한다
                if(exposedLength <= 0f) continue;

                // 노출된 바위 크기에 맞춰 캡슐 반지름을 계산한다
                float radius = Mathf.Min(spikeWidth * 0.5f, exposedLength * 0.5f);

                // 캡슐의 양 끝 구 중심을 계산한다
                Vector3 capsuleBottom = groundPoint + growthDir * radius;
                Vector3 capsuleTop = meshTopPos - growthDir * radius;
                
                // 기즈모 정보를 리스트에 추가한다
                RockSpikeGizmoData gizmoData = new RockSpikeGizmoData();
                gizmoData.particlePosition = hitPos;
                gizmoData.renderedMeshPosition = meshBasePos;
                gizmoData.groundPoint = groundPoint;
                gizmoData.capsuleBottom = capsuleBottom;
                gizmoData.capsuleTop = capsuleTop;
                gizmoData.growthDir = growthDir;
                gizmoData.radius = radius;
                rockSpikeGizmos.Add(gizmoData);

                // 파티클 위치를 중심으로 구형 물리 판정을 실행한다
                // targetLayer에 포함된 콜라이더만 검색한다
                Collider[] hits = Physics.OverlapCapsule(capsuleBottom, capsuleTop, radius, targetLayer);

                // 기존 피해 처리 함수를 사용해 피해와 넉백을 적용한다
                // 내려찍기에 이미 맞은 대상은 같은 HashSet에 등록되어
                // 이번 대지 분쇄 공격에서 중복 피해를 받지 않는다
                ApplyDamageToTargets(hits, jumpAndStompAttackTargets, rockSpikeDamageMultiplier, HitReactionType.Knockback);
            }
            // 다음 프레임에 파티클 위치를 다시 확인하도록 대기한다
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// 바위의 밑부분에서 성장 방향으로 이어지는 직선과 지면이 만나는 위치를 찾는다
    /// </summary>
    /// <param name="meshBasePos"></param>
    /// <param name="growthDir"></param>
    /// <param name="groundPoint"></param>
    /// <returns></returns>
    private bool TryGetRockGroundPoint(Vector3 meshBasePos, Vector3 growthDir, out Vector3 groundPoint)
    {
        groundPoint = Vector3.zero;
        // 바위가 거의 수평 또는 아래쪽을 향한다면
        // 지면 교차점을 안정적으로 구하기 어려우므로 제외한다
        if(growthDir.y <= 0.05f) return false;

        // 바위 밑부분에서 성장 방향으로 일정거리 떨어진 곳을 Raycast 시작점으로 설정
        // 밑부분이 지면 아래에 있더라도 지면 위에서 탐색이 시작되도록 한다
        Vector3 rayOrigin = meshBasePos + growthDir * groundRayDistance;

        // 성장 방향의 반대쪽으로 Raycast하여 지면과 만나는 지점을 찾는다
        float rayLength = groundRayDistance * 2f;
        if(Physics.Raycast(rayOrigin, - growthDir, out RaycastHit groundHit, rayLength, groundLayer, QueryTriggerInteraction.Ignore))
        {
            groundPoint = groundHit.point;
            return true;
        }
        // 지정한 지면 Layer에서 충돌 지점을 찾지 못한 경우 
        return false;
    }

    // 콤보 공격
    public void BeginLeftAttack(float damageMultiplier)
    {
        comboTargets.Clear();
        attackPattern = BossAttackPattern.leftHandAttack;
        comboDamageMultiplier = Mathf.Max(0f, damageMultiplier);
        isComboDamageActive = true;
    }
    public void EndLeftAndRightAttack()
    {
        isComboDamageActive = false;
        comboTargets.Clear();
    }
    public void BeginRightAttack(float damageMultiplier)
    {
        comboTargets.Clear();
        attackPattern = BossAttackPattern.RightHandAttack;
        comboDamageMultiplier = Mathf.Max(0f, damageMultiplier);
        isComboDamageActive = true;
    }
    // 회전 공격
    public void BeginSpinAttack(float damageMultiplier)
    {
        spinTargets.Clear();
        spinDamageMultiplier = Mathf.Max(0f, damageMultiplier);
        isSpinDamageActive = true;
    }
    public void EndSpinAttack()
    {
        isSpinDamageActive = false;
        spinTargets.Clear();
    }

    /// <summary>
    /// 공격 애니메이션이 강제로 중단되었을 때 활성화된 판정을 정리한다.
    /// 보스의 사망, 피격 경직, 공격 취소 등의 상황에서 호출한다.
    /// </summary>
    public void CancelActiveDamageWindows()
    {
        // 지속 피해 판정이 더 이상 실행되지 않도록 비활성화한다.
        isComboDamageActive = false;
        isSpinDamageActive = false;

        // 이전 공격에서 사용한 대상 목록들을 정리한다.
        comboTargets.Clear();
        spinTargets.Clear();
        jumpAndStompAttackTargets.Clear();

        // 저장한 착지 위치가 다음 공격에 재사용되지 않도록 무효화한다.
        hasJumpLandingPos = false;

        // 화면에 남아 있는 경고 원도 숨긴다.
        HideWarningCircle();
    }

    /// <summary>
    /// 광폭화 점프 공격의 충격파를 재생하고,
    /// 모든 파티클이 재생을 마치면 오브젝트를 비활성화한다.
    /// </summary>
    /// <returns></returns>
    private IEnumerator PlayShockWaveRoutine()
    {
        // 인스펙터 누락시 오류 방지
        if (shockWaveInstance == null)
        {
            Debug.LogWarning("[BossCombat] 충격파 오브젝트가 연결되지 않았습니다.");
            yield break;
        }

        // 충격파 위치 보정값
        const float offSet = 0.1f;

        shockWaveInstance.SetActive(true);

        Vector3 effectPos = jumpLandingPos;
        effectPos.y += offSet;
        shockWaveInstance.transform.position = effectPos;

        ParticleSystem particle = shockWaveInstance.GetComponent<ParticleSystem>();

        // 이전에 남아있는 파티클을 제거하고 새로 재생한다.
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play();
        yield return null;

        // 피격 판정 진행
        ShockWaveImpact();

        // 루트와 자식 파티클의 재생이 모두 끝날 때까지 기다린다.
        while(particle.IsAlive(true))
        {
            yield return null;
        }

        // 충격파가 완전히 비활성화하여 다음 공격에서 재사용한다.
        shockWaveInstance.SetActive(false);
    }
    
    /// <summary>
    /// 점프 충격파 피해 판정을 처리하는 함수
    /// </summary>
    private void ShockWaveImpact()
    {
        if (shockWaveInstance == null)
            return;
        
        // 충격파 중심 위치
        Vector3 effectPos = shockWaveInstance.transform.position;

        // 파티클 오브젝트와 자식 오브젝트의 렌더러를 모두 가져온다
        ParticleSystemRenderer[] renderers = shockWaveInstance.GetComponentsInChildren<ParticleSystemRenderer>();
        bool hasBounds = false;
        Bounds effectBounds = default;

        foreach(ParticleSystemRenderer parti in renderers)
        {
            // 사용하지 않는 렌더러는 실제 효과 범위에 포함하지 않는다
            if(parti == null || !parti.enabled) continue;
            // 렌더러가 붙어있는 오브젝트의 파티클 시스템을 확인한다
            ParticleSystem particleSystem = parti.GetComponent<ParticleSystem>();
            // 재생 후에도 파티클이 없는 시스템은 경계 계산에서 제외한다
            if(particleSystem == null || particleSystem.particleCount == 0) continue;
            
            // 첫 렌더러의 경계를 시작점으로 삼고 나머지 경계를 합친다
            if(!hasBounds)
            {
                effectBounds = parti.bounds;
                hasBounds = true;
            }
            else
            {
                effectBounds.Encapsulate(parti.bounds);
            }
        }
        // 사용할 수 있는 파티클렌더러 경계가 없으면 판정하지 않는다
        if(!hasBounds) return;
        // 렌더러 경계의 중심이 충격파 중심과 다를 수 있으므로
        // 중심 위치 차이와 경계 반폭을 더해 X/Z 방향의 최대 범위를 구한다
        float radiusX = Mathf.Abs(effectBounds.center.x - effectPos.x) + effectBounds.extents.x;
        float radiusZ = Mathf.Abs(effectBounds.center.z - effectPos.z) + effectBounds.extents.z;

        // 두 축 중 더 큰 범위를 원의 반경으로 사용한다
        float radius = Mathf.Max(radiusX, radiusZ);
        // 원기둥 판정 높이 설정
        float cylinderHeight = 1f;
        float halfHeight = cylinderHeight * 0.5f;

        // 원기둥의 절반 높이만큼 위아래로 검색 영역 설정
        Vector3 halfExtents = new Vector3(radius, halfHeight, radius);
        // OverlapBox로 주변 후보 콜라이더를 찾는다
        Collider[] hits = Physics.OverlapBox(effectPos, halfExtents, Quaternion.identity, targetLayer);
        // 실제 원기둥 안에 있는 대상만 저장한다
        List<Collider> targets = new List<Collider>();
        foreach(Collider col in hits)
        {
            // 예외 조건
            if(col == null) continue;
            // 플레이어 컴포넌트를 찾는다
            Player_CC player = col.GetComponentInParent<Player_CC>();
            if(player == null || player.IkFootRoot == null) continue;

            // 충격파 중심에 가장 가까운 표면 지점을 구한다
            // 콜라이더 가장자리가 원 안에 걸치는 경우도 판정할 수 있다
            Vector3 closestPoint = col.ClosestPoint(effectPos);
            
            // X/Z 평면에서만 거리를 비교한다
            Vector2 horiOffset = new Vector2(closestPoint.x - effectPos.x, closestPoint.z - effectPos.z);
            // 제곱 거리로 비교해 제곱근 계산을 생략한다
            // 원의 경계에 정확히 닿은 경우는 판정에 포함한다
            if(horiOffset.sqrMagnitude > radius * radius) continue;

            // 플레이어 발밑 기준 위치를 가져온다.
            Vector3 footPos = player.IkFootRoot.position;
            // 충격파 발생 지점과 플레이어 발밑의 높이 차이를 계산
            float distance = Mathf.Abs(footPos.y - effectPos.y);

            // cylinderHeight는 원기둥의 전체 높이다
            // 중심에서 위아래로 허용할 거리는 전체 높이의 절반인 halfHeight로 계산한다
            // 플레이어 발 위치와 충격파 중심의 높이 차이가 halfHeight를 넘으면 판정에서 제외한다
            if(distance > halfHeight) continue;

            // 맞는 높이에 있다면 list추가
            targets.Add(col);
        }

        // 데미지 처리 함수에 전달한다.
        // jumpAndStompAttackTargets를 공유해 같은 점프 공격 중 이미 맞은 대상의 중복 피해를 막는다
        ApplyDamageToTargets(targets.ToArray(), jumpAndStompAttackTargets, jumpDamageMultiplier, HitReactionType.Knockback);
    }
}
