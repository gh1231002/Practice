using UnityEngine;
using UnityEngine.InputSystem;

public class Player_RB : MonoBehaviour
{
    [Header("InputMove")]
    [SerializeField] InputActionProperty MoveIAP;
    [Header("InputJump")]
    [SerializeField] InputActionProperty JumpIAP;
    [Header("PlayerSetting")]
    [SerializeField]float MoveSpeed = 2f;
    [SerializeField]float JumpForce = 2f;
    [SerializeField]bool CheckGround;

    Transform TrsPlayer;
    Rigidbody RigidPlayer;
    Vector3 MoveDir;

    
    void Start()
    {
        TrsPlayer = GetComponent<Transform>();
        RigidPlayer = GetComponent<Rigidbody>();

        if(MoveIAP != null)
        {
            MoveIAP.action.Enable();
        }
        if(JumpIAP != null)
        {
            JumpIAP.action.Enable();
        }
    }

    
    void Update()
    {
        InputMove();
        CheckJump();
    }

    private void FixedUpdate()
    {
        Moving();
    }

    private void InputMove()
    {
        Vector2 move = Vector2.zero;
        if (MoveIAP.action != null)
        {
            move = MoveIAP.action.ReadValue<Vector2>();
        }
            MoveDir = new Vector3(move.x, 0f, move.y).normalized;
    }

    private void Moving()
    {
        Vector3 Move = transform.TransformDirection(MoveDir)
                       * MoveSpeed
                       * Time.fixedDeltaTime;
        RigidPlayer.MovePosition(RigidPlayer.position +  Move);
    }

    private void CheckJump()
    {
        if (JumpIAP.action == null) return;

        if(JumpIAP.action.WasPressedThisFrame() && CheckGround == true)
        {
            RigidPlayer.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
            CheckGround = false;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        CheckGround = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        CheckGround = false;
    }
}
