public enum QuestType
{
    General,//일반
    Hunt,//사냥
    Collect,//수집
    Reward//보상
}

public enum MonsterType
{
    Any,
    Skeleton,
    Boss
}

public enum QuestState
{
    NotStarted,
    InProgress,
    CanComplete,
    Completed
}

public enum TalkType
{
    Start,
    Remind,
    Complete,
    Default
}

// 몬스터 상태 정의
public enum MonsterState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Hit,
    Die
}

enum TimePhase
{
    Sunrise,
    Day,
    Sunset,
    Night,
    None
}

public enum HitCheckType
{ 
    none,
    Box,
    Sphere
}

// 포탈이 이동시켜야하는 씬의 이름들을 ENUM형태로 선언
public enum SceneName
{
    Town,
    Grave,
    Boss
}

public enum BossState
{
    Idle,
    Chase,
    Attack,
    Enrage,
    Die
}
public enum BossAttackPattern
{
    leftHandAttack,
    RightHandAttack,
    SpinAttack,
    JumpAttack,
    StompAttack
}
// 공격을 맞았을 때 플레이어에게 적용할 피격 반응을 구분한다.
public enum HitReactionType
{
    Normal,
    Knockback
}