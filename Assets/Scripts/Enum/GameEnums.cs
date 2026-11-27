public enum QuestType
{
    General,//�Ϲ�
    Hunt,//���
    Collect,//����
    Reward//����
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

// ���� ���� ����
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

// ��Ż�� �̵����Ѿ��ϴ� ���� �̸����� ENUM���·� ����
public enum SceneName
{
    Title,
    Town,
    Church,
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
// ������ �¾��� �� �÷��̾�� ������ �ǰ� ������ �����Ѵ�.
public enum HitReactionType
{
    Normal,
    Knockback
}