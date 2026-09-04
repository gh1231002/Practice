using UnityEngine;

public class MiniMapCameraManager : MonoBehaviour
{
    [Header("카메라 높이 설정")]
    [SerializeField] float Height;
    Player_CC Player;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        GameObject obj = GameObject.FindWithTag("Player");
        Player = obj.GetComponent<Player_CC>();
    }

    private void LateUpdate()
    {
        if (Player == null) return;
        transform.position = new Vector3(Player.transform.position.x,
                                         Player.transform.position.y + Height,
                                         Player.transform.position.z);
    }
}
