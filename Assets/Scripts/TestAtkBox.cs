using UnityEngine;

public class TestAtkBox : MonoBehaviour
{
    [SerializeField] Transform AtkPoint;
    [SerializeField] Vector3 AtkHalfBox;
    [SerializeField] float AtkRadius;

    private void OnDrawGizmosSelected()
    {
        if (AtkPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.matrix = AtkPoint.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, AtkHalfBox * 2f);

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(AtkPoint.position, AtkRadius);
    }
}
