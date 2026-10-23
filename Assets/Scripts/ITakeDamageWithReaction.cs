using UnityEngine;

/// <summary>
/// 일반 피해 처리 외에 피격 반응 종류까지 전달할 수 있는 인터페이스
/// </summary>
public interface ITakeDamageWithReaction
{
    void TakeDamage(GameObject attacker, float damage, HitReactionType reactionType);
}
