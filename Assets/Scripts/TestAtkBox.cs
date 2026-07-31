using UnityEngine;

public class TestAtkBox : MonoBehaviour
{
    [SerializeField] Transform AtkPoint;
    [SerializeField] Vector3 AtkHalfBox;

    private void OnDrawGizmosSelected()
    {
        if (AtkPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.matrix = AtkPoint.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, AtkHalfBox * 2f);
    }
}
