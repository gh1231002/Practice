using System;
using UnityEngine;

/// <summary>
/// 외부 트리거 센서 수신 및 정밀 시야각/레이캐스트 검사를 전담합니다.
/// </summary>
public class MonsterDetect : MonoBehaviour
{
    [Header("센서 연동")]
    // 감지 범위 트리거 센서
    [SerializeField] MonsterSensor monSensor;
    // 순찰 이탈 범위 트리거 센서
    [SerializeField] MonsterSensor patrolSensor;

    [Header("시야각 및 레이캐스트 설정")]
    [SerializeField, Tooltip("레이캐스트 발사 위치")] Transform trsEyes;
    [SerializeField, Tooltip("시야각 (정면 기준 좌우 합산)")] float fovAngle;
    [SerializeField, Tooltip("장애물 및 플레이어 포함 레이어 마스크")] LayerMask obstacleAndPlayerMask;

    // 현재 포착된 플레이어 Transform
    public Transform TargetPlayer {  get; private set; }
    // 시야 내에 완벽하게 포착되었는지 여부
    public bool IsPlayerInSight { get; private set; }

    float checkTimer = 0f;
    float Check_Interval = 0.1f;

    // 탐지 상태 변화 알림 이벤트
    public event Action<Transform> OnPlayerDetected;
    public event Action OnPlayerLost;
    public event Action OnPatrolLost;

    private void Start()
    {
        // 1차 감지 센서 이벤트 구독
        if(monSensor != null)
        {
            monSensor.OnSensorEnter += SensorDetected;
            monSensor.OnSensorLost += SensorLost;
        }
    }

    /// <summary>
    /// 스포너에서 순찰 구역 센서를 전달받아 이벤트를 안전하게 연결합니다.
    /// </summary>
    /// <param name="sensor"></param>
    public void SetPatrolSensor(MonsterSensor sensor)
    {
        // 중복 구독 방지
        if(patrolSensor != null)
        {
            patrolSensor.OnSensorLost -= PatrolLost;
        }
        patrolSensor = sensor;
        // 동적으로 할당된 씬 센서의 이벤트 구독
        if(patrolSensor != null)
        {
            patrolSensor.OnSensorLost += PatrolLost;
        }
    }

    private void SensorDetected(Collider collider)
    {
        // 자식 콜라이더가 감지되어도 항상 최상위 Player 오브젝트 Transform을 할당
        TargetPlayer = collider.transform.root;
    }

    private void SensorLost(Collider collider)
    {
        IsPlayerInSight = false;
        TargetPlayer = null;
        OnPlayerLost?.Invoke();
    }

    private void PatrolLost(Collider collider)
    {
        IsPlayerInSight = false;
        TargetPlayer = null;
        OnPatrolLost?.Invoke();
    }

    /// <summary>
    /// 3단계 정밀 시야 검사 (거리 -> 각도 -> 레이캐스트 장애물 가림)
    /// </summary>
    /// <param name="player">대상 플레이어</param>
    /// <param name="detectRadius">탐지 반경</param>
    /// <returns>시야 내 포착 여부</returns>
    public bool CheckLineOfSight(Transform player, float detectRadius)
    {
        // 만약 player가 없다면 종료
        if (player == null) return false;

        // 눈 위치가 미지정된 경우 본인 높이 기준 자동 설정
        Vector3 eyePos = trsEyes != null ? trsEyes.position : transform.position + Vector3.up * 1.1f;

        Vector3 targetPos = player.position + Vector3.up * 1.0f;
        Vector3 dirToPlayer = targetPos - eyePos;

        // 1단계 거리 검사 : sqrMagnitude 연산으로 제곱근 연산 부하 제거
        if (dirToPlayer.sqrMagnitude > detectRadius * detectRadius) return false;

        // 2단계 시야각 검사 : 몬스터 정면과 플레이어 방향 간 시야각 설정
        float angle = Vector3.Angle(transform.forward, dirToPlayer.normalized);
        // angle 값이 좌 / 우 시야각 보다 크다면 플레이어 감지 실패
        if (angle > fovAngle * 0.5f) return false;

        // 3단계 시선 가림 검사 : 레이를 쏘아 장애물에 가려졌는지 확인
        RaycastHit[] hits = Physics.RaycastAll(eyePos, dirToPlayer.normalized, detectRadius, obstacleAndPlayerMask, QueryTriggerInteraction.Ignore);
        // 거리에 따라 정렬하여 가장 가까운 충돌체 순서대로 검사
        Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach(var target in hits)
        {
            // 몬스터 자기 자신에 부딪힌 경우 무시하고 다음 대상 검사
            if(target.transform.root == transform.root) continue;

            // 가장 먼저 부딪힌 대상이 플레이어인 경우 시야 확보 성공
            if(target.transform.CompareTag("Player") || target.transform.root.CompareTag("Player"))
            {
                return true;
            }

            return false;
        }
        return false;
    }

    /// <summary>
    /// monSensor의 콜라이더 크기를 자동으로 가져와 감지 거리로 사용하는 오버로딩 함수
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public bool CheckLineOfSight(Transform player)
    {
        float radius = (monSensor != null) ? monSensor.GetSensorRadius() : 5f;

        return CheckLineOfSight(player, radius);
    }

    private void Update()
    {
        if(TargetPlayer == null) return;

        checkTimer += Time.deltaTime;
        if (checkTimer < Check_Interval) return;
        checkTimer = 0f;

        // 센서 범위 안에 플레이어가 있을 때, 시야각 및 벽 가림 정밀 검사
        bool canSee = CheckLineOfSight(TargetPlayer);

        if(canSee && !IsPlayerInSight)
        {
            IsPlayerInSight = true;
            OnPlayerDetected?.Invoke(TargetPlayer);
        }
        else if(!canSee && IsPlayerInSight)
        {
            IsPlayerInSight = false;
            OnPlayerLost?.Invoke();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 눈 위치 계산
        Vector3 eyePos = trsEyes != null ? trsEyes.position : transform.position + Vector3.up * 1.2f;
        // monSensor의 콜라이더 크기 가져오기
        float radius = (monSensor != null) ? monSensor.GetSensorRadius() : 5f;
        // 시야 정면 기준 좌/우 외곽선 계산
        Vector3 leftDir = Quaternion.Euler(0, -fovAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, fovAngle * 0.5f, 0) * transform.forward;
        // 부채꼴 시야각 경계선 그리기
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(eyePos, leftDir * radius);
        Gizmos.DrawRay(eyePos, rightDir * radius);
        // 정면 바라보는 방향선
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(eyePos, transform.forward * radius);
    }
}
