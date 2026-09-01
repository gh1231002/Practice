using UnityEngine;

// 포탈이 이동시켜야하는 씬의 이름들을 ENUM형태로 선언
public enum TargetScene
{
    FirstVillage,
    Grave,
}

public class Portal : MonoBehaviour
{
    [Header("Portal Setting")]
    [SerializeField] TargetScene targetScene;
    [SerializeField] Vector3 targetPos;
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            switch (targetScene)
            {
                case TargetScene.FirstVillage:
                    LoadingSceneManager.LoadScene("First village");
                    break;
                case TargetScene.Grave:
                    LoadingSceneManager.LoadScene("Dungeon");
                    break;
            }
        }
    }
}
