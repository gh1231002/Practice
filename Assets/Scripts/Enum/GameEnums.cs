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
public enum TargetScene
{
    FirstVillage,
    Grave,
}
