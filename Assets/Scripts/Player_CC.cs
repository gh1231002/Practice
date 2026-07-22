using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_CC : MonoBehaviour, ITakeDamage
{
    [Header("InputAction")]
    [SerializeField] InputActionProperty IapMove;
    [SerializeField] InputActionProperty IapJump;
    [SerializeField] InputActionProperty IapWalk;
    [SerializeField] InputActionProperty IapRoll;
    [SerializeField] InputActionProperty IapCrouch;
    [SerializeField] InputActionProperty IapCombat;
    [SerializeField] InputActionProperty IapAttack;
    [SerializeField] InputActionProperty IapLook;
    [SerializeField] InputActionProperty IapInteract;
    [Header("무기 관련")]
    [SerializeField] Transform TrsWeapons;
    [SerializeField] Transform AtkPoint;
    [SerializeField] Vector3 AtkHalfBox;
    [SerializeField] GameObject ObjWeapons;
    [SerializeField] LayerMask TargetLayer;
    [SerializeField] float PlayerAtkDuration;
    float PlayerAtkTimer = 0f;
    bool CheckAtk;
    List<ITakeDamage> HitTartgetList = new List<ITakeDamage>();
    [Header("PlayerSetting")]
    [SerializeField] float MaxHp;
    [SerializeField] float CurHp;
    [SerializeField] float AtkPower;
    [SerializeField] float InvincibleTime;
    [SerializeField] float MoveSpeed;
    [SerializeField] float JumpForce;
    [SerializeField] float BackJumpForce;
    [SerializeField] float BackJumpSpeed;
    [SerializeField] float KnockBackSpeed;
    float CurBackJumpSpeed;
    [Header("회전속도")]
    [SerializeField] float RotationSpeed = 360f;
    [Header("중력관련")]
    [SerializeField] float Gravity = -9.81f;
    [SerializeField, Tooltip("최대 하강 속도")] float MaxVelocityY;
    [SerializeField] float RayDistance = 0.3f;
    [SerializeField] LayerMask GroundLayer;
    [Header("경사면 미끄러짐 설정(캐릭터 컨트롤러와 동일)")]
    [SerializeField] float SlopeLimitAngle;
    [SerializeField] float SlideSpeed;
    //isground()에서 계산할 벡터
    Vector3 GroundNormal = Vector3.zero;
    //미끄러지는중인지 체크
    bool CheckSliding;

    CharacterController ContPlayer;
    Animator Anim;
    Transform TrsMainCam;

    AnimatorStateInfo CurAniState;
    Vector3 MoveDir;
    Vector3 KnockBackVelocity = Vector3.zero;
    Vector2 LookInput;


    [SerializeField] float VelocityY;
    float MoveValue;
    float CombatValue;
    [Header("애니메이션 간 딜레이타임")]
    [SerializeField] float DelayTime;

    int CrouchMove;

    bool CheckGround;
    bool CheckCrouch;
    bool CheckWalk;
    bool CheckRoll;
    bool CheckCombat;
    bool CheckJump;
    [SerializeField] bool CanCombo = false;
    bool CheckBackJump;
    bool CheckDeath;
    [SerializeField] bool CheckHit;
    bool CheckKnockBack;
    bool isAttack;
    bool isInteract;
    bool isDialogue;
    //이벤트 함수들
    public event Action<float,float> ChangeHp;
    public event Action OnDialogue;
    public event Action OffDialogue;

    public void SetInteract(bool State)
    {
        isInteract = State;
    }

    private void Awake()
    {
        CurHp = MaxHp;
        if (ContPlayer == null)
        {
            TryGetComponent(out ContPlayer);

            if (ContPlayer == null)
            {
                Debug.LogError("캐릭터 컨트롤러를 찾을수 없습니다.", this);
            }
        }
    }

    void Start()
    {
        ContPlayer = GetComponent<CharacterController>();
        Anim = GetComponent<Animator>();

        //인풋액션이 연결되있다면 활성화
        OnInputAction();
        ObjWeapons.SetActive(false);
        Anim.applyRootMotion = false;

        if (Camera.main != null)
        {
            TrsMainCam = Camera.main.transform;
        }

        //커서잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //이벤트 설정
        ChangeHp?.Invoke(CurHp, MaxHp);
        TalkManager.Instance.OffTalk += UnlockMove;
    }

    private void OnInputAction()
    {
        IapMove.action?.Enable();
        IapLook.action?.Enable();
        IapRoll.action?.Enable();
        IapCrouch.action?.Enable();
        IapCombat.action?.Enable();
        IapWalk.action?.Enable();
        IapAttack.action?.Enable();
        IapInteract.action?.Enable();
    }

    void Update()
    {
        if (CheckDeath == true) return;

        isAttack = Anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack");

        if (CheckHit == true) { }
        else
        {
            if(!isDialogue)
            {
                InputMove();
                InputJump();
                InputRoll();
                InputCrouch();
                InputCombat();
                InputWalk();
                InputAttack();
            }
            InputInteract();
        }
        CheckAni();
        CheckAniState();
        CheckRollInvincible();
        PlayerOverLapBoxCheck();
    }
    /// <summary>
    /// 이동갑입력받아서 MoveDir값에 저장
    /// </summary>
    private void InputMove()
    {
        Vector2 move = Vector2.zero;

        if (IapMove.action != null)
        {
            move = IapMove.action.ReadValue<Vector2>();
        }
        //시네머신 메인 카메라 기준 벡터값 추출
        Vector3 CamForward = TrsMainCam.forward;
        Vector3 CamRight = TrsMainCam.right;

        CamForward.y = 0f;
        CamRight.y = 0f;
        CamForward.Normalize();
        CamRight.Normalize();

        MoveDir = (CamForward * move.y + CamRight * move.x).normalized;
    }
    /// <summary>
    /// 카메라 입력받는 함수
    /// </summary>
    private void InputLook()
    {
        if (IapLook.action != null)
        {
            LookInput = IapLook.action.ReadValue<Vector2>();
        }
    }
    /// <summary>
    /// 점프입력받았는지 체크하고, 점프실행
    /// </summary>
    private void InputJump()
    {
        if (IapJump.action == null || isAttack == true) return;
        //점프키가 눌리고 캐릭터가 땅에 닿아있다면 점프실행
        if (IapJump.action.WasPressedThisFrame() && CheckGround == true)
        {
            StartCoroutine(JumpRoutine());
        }
    }
    /// <summary>
    /// 점프 코루틴
    /// </summary>
    /// <returns></returns>
    IEnumerator JumpRoutine()
    {
        if (CheckJump == true) yield break;

        CheckJump = true;
        CurBackJumpSpeed = BackJumpSpeed;

        if(MoveValue == 0f)//제자리일때
        {
            Anim.SetTrigger("Jump");
            if(CheckCombat == true)//캐릭터가 보는 방향 뒷방향으로 점프
            {
                CheckBackJump = true;
                VelocityY = BackJumpForce;
            }
            if(CheckCombat == false)
            {
                VelocityY = JumpForce;
            }
        }
        else
        {
            Anim.SetTrigger("RunningJump");
            VelocityY = JumpForce;
        }

        //점프가 되기위해 1프레임 기다려줌
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        while(IsGround() == false)//공중에 떠있는동안 기다림
        {
            yield return new WaitForFixedUpdate();
        }

        CheckJump = false;
        CheckBackJump = false;
        Anim.ResetTrigger("Jump");
        Anim.ResetTrigger("RunningJump");
    }
    /// <summary>
    /// 구르기 입력시 조건체크하고, 애니메이션 실행
    /// </summary>
    private void InputRoll()
    {
        if (IapRoll.action == null) return;

        if (IapRoll.action.WasPressedThisFrame() && CheckGround == true)
        {
            StartCoroutine(RollRoutine());
        }
    }

    IEnumerator RollRoutine()
    {
        //중복실행방지
        if(CheckRoll == true) yield break;
        CheckRoll = true;
        CheckHit = true;

        Anim.SetTrigger("Roll");
        yield return new WaitForSeconds(DelayTime);
        Anim.ResetTrigger("Roll");
    }
    /// <summary>
    /// 앉기 키 누르면 앉고, 그상태로 움직이면 앉은자세로 이동
    /// </summary>
    private void InputCrouch()
    {
        if(IapCrouch.action == null) return;

        if(IapCrouch.action.WasPressedThisFrame() && CheckCrouch == false)
        {
            CheckCrouch = true;
            StartCoroutine(CrouchRouutine());
        }
        else if(IapCrouch.action.WasPressedThisFrame() && CheckCrouch == true)
        {
            CheckCrouch = false;
            StartCoroutine(CrouchRouutine());
        }
    }
    IEnumerator CrouchRouutine()
    {
        switch (CheckCrouch)
        {
            case true:
                Anim.SetTrigger("Crouch");
                yield return new WaitForSeconds(DelayTime);
                Anim.ResetTrigger("Crouch");
                break;

            case false:
                Anim.SetTrigger("Crouch");
                yield return new WaitForSeconds(DelayTime);
                Anim.ResetTrigger("Crouch");
                break;
        }
        yield return null;
    }

    private void InputCombat()
    {
        if(IapCombat == null) return;

        if(IapCombat.action.WasPressedThisFrame() && CheckCombat == false)
        {
            CheckCombat = true;
            CombatValue = 1f;
            ObjWeapons.SetActive(true);
        }
        else if(IapCombat.action.WasPressedThisFrame() && CheckCombat == true)
        {
            CheckCombat = false;
            CombatValue = 0f;
            ObjWeapons.SetActive(false);
        }
    }
    /// <summary>
    /// 걷기 상태인지 체크하고, bool값 변경
    /// </summary>
    private void InputWalk()
    {
        if (IapWalk.action == null) return;

        if (IapWalk.action.WasPressedThisFrame() && CheckWalk == false)
        {
            CheckWalk = true;
        }
        else if (IapWalk.action.WasPressedThisFrame() && CheckWalk == true)
        {
            CheckWalk = false;
        }
    }
    /// <summary>
    /// 공격입력받는 함수
    /// </summary>
    private void InputAttack()
    {
        if (IapAttack.action == null || CheckCombat == false) return;
        //전투모드이고 공격키 눌렀을때
        if(IapAttack.action.WasPressedThisFrame() && CheckCombat == true)
        {
            AttackProcess();
        }
    }

    private void AttackProcess()
    {
        //구르고있거나 앉은상태이거나 공중에 떠있으면 공격불가
        if (CheckRoll == true || CheckCrouch == true || IsGround() == false) return;

        bool isTransition = Anim.IsInTransition(0);
        //다른애니메이션으로 전환중이라면
        if (isTransition == true) return;
        //공격 시작전 입력중인 방향이 있다면 그방향으로 회전
        if(MoveDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(MoveDir.normalized);
        }

        //첫공격일때
        if(isAttack == false)
        {
            Anim.SetTrigger("Attack");
            CanCombo = false;
        }
        //콤보 입력일때 (이미 공격중이고, 애니메이션 이벤트에 의해 true가 되면 실행
        else if(CanCombo == true)
        {
            Anim.SetTrigger("Attack");
            CanCombo = false;//연타 방지를위해 바로 false
        }
    }

    private void InputInteract()
    {
        if (isInteract == false)
        {
            isDialogue = false;
            OffDialogue?.Invoke();
        }
        //플레이어가 npc트리거에 닿아있고 상호작용키를 누른다면
        if(isInteract == true && isDialogue == false && IapInteract.action.WasPressedThisFrame())
        {
            isDialogue = true;
            OnDialogue?.Invoke();
        }
        //현재 대화상태중이고 이동불가상태이며 상호작용키 입력이 들어왔을때
        else if(isDialogue == true && IapInteract.action.WasPressedThisFrame())
        {
            //다음대사로 넘어가라고 talkmanager에게 전달
            TalkManager.Instance.NextDialogueText();
        }
    }

    /// <summary>
    /// 애니메이터에게 변수값 전달하는 함수
    /// </summary>
    private void CheckAni()
    {
        if (isDialogue == true) return;

        Anim.SetFloat("MoveValue", MoveValue);
        Anim.SetFloat("CombatValue", CombatValue);
        Anim.SetInteger("CrouchMove", CrouchMove);
        Anim.SetBool("Walk", CheckWalk);
        Anim.SetBool("Combat", CheckCombat);
        Anim.SetBool("Combo", CanCombo);
        Anim.SetBool("Big Hit", CheckKnockBack);
    }
    /// <summary>
    /// 현재 어떤 애니메이션이 재생중인지 체크하는 함수
    /// </summary>
    private void CheckAniState()
    {
        CurAniState = Anim.GetCurrentAnimatorStateInfo(0);
    }

    private void CheckRollInvincible()
    {
        //현재 roll태그를 가진 애니메이션이 재생중인지 확인
        bool CheckRollAnim = Anim.GetCurrentAnimatorStateInfo(0).IsTag("Roll");
        if(CheckRollAnim == true)
        {
            //구르는동안 무적
            CheckHit = true;
        }
        else
        {
            //넉백중이 아니라면 무적 플래그 끔
            if(CheckKnockBack == false && CheckRoll == false)
            {
                CheckHit = false;
            }
        }
    }

    private void StartAtk()
    {
        CheckAtk = true;
        PlayerAtkTimer = 0f;
        HitTartgetList.Clear();
    }

    private void PlayerOverLapBoxCheck()
    {
        if (CheckAtk == false || AtkPoint == null) return;

        PlayerAtkTimer += Time.deltaTime;
        if(PlayerAtkTimer >= PlayerAtkDuration)
        {
            CheckAtk = false;
            return;
        }
        Collider[] HitTargets = Physics.OverlapBox(AtkPoint.position, AtkHalfBox,
                                AtkPoint.rotation, TargetLayer);
        foreach(Collider Target in HitTargets)
        {
            if(Target.TryGetComponent<ITakeDamage>(out var Damage))
            {
                if (HitTartgetList.Contains(Damage)) continue;
                Damage.TakeDamage(this.gameObject, AtkPower);
                HitTartgetList.Add(Damage);
            }
        }
    }


    private void FixedUpdate()
    {
        if (CheckDeath == true || isDialogue == true) return;

        PlayerRotation();
        Vector3 MoveVelocity = MovingVelocity();
        Vector3 Vertivelocity = VerticalVelocity();
        Vector3 FinalVelocity = MoveVelocity + Vertivelocity;


        ContPlayer.Move(FinalVelocity * Time.fixedDeltaTime);
    }
    /// <summary>
    /// 플레이어 회전
    /// </summary>
    private void PlayerRotation()
    {
        if(CheckRoll == false && isAttack == false &&
            CheckHit == false && MoveDir.sqrMagnitude >0.01f)
        {
            Quaternion TargetRotation = Quaternion.LookRotation(MoveDir.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, TargetRotation,
                                                          RotationSpeed * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// 중력관리
    /// </summary>
    private Vector3 VerticalVelocity()
    {
        //땅인지 아닌지 체크
        if (IsGround() == true && CheckJump == false)
        {
            CheckGround = true;
            VelocityY = -2f;
        }
        //땅이 아니거나 경사가 가파르면
        else
        {
            CheckGround = false;
            VelocityY += Gravity * Time.fixedDeltaTime;
        }

        //최대 하강 속도 제한
        VelocityY = Mathf.Max(VelocityY, MaxVelocityY);

        if (CheckSliding == true)
        {
            //경사면 경사방향계산(하방벡터 추출)
            Vector3 SlideDir = new Vector3(GroundNormal.x, -GroundNormal.y, GroundNormal.z);
            //중력낙하값에 미끄러지는속도 결합, 최종 반환
            Vector3 FinalSlideVelocity = SlideDir * SlideSpeed;
            //기존 중력은 유지
            FinalSlideVelocity.y = VelocityY;
            return FinalSlideVelocity;
        }

        return new Vector3(0f, VelocityY, 0f);
    }

    /// <summary>
    /// 움직임 담당하는 함수
    /// </summary>
    private Vector3 MovingVelocity()
    {
        //구르는 중이거나 공격애니메이션 진행중이면 이동입력 안받음 
        if (CheckRoll == true || isAttack == true) return Vector3.zero;

        //넉백일때
        if(CheckHit == true)
        {
            return KnockBackVelocity;
        }

        float CurSpeed = MoveSpeed;//캐릭터 이동속도

        Vector3 BackJumpVelocity = Vector3.zero;

        //전투모드이고 입력값없이 점프할때
        if (CheckJump == true && CheckCombat == true && CheckBackJump == true)
        {
            //뒤로 얼마나 밀려날건지
            BackJumpVelocity = -transform.forward * CurBackJumpSpeed;
            return BackJumpVelocity;
        }

        //입력값이 없을때
        if (MoveDir.magnitude < 0.01f)
        {
            MoveValue = 0f;
            CrouchMove = 0;
            return Vector3.zero;
        }
        else
        {
            //앉기인지 아닌지
            if (CheckCrouch == false)
            {
                //걷기일땐 이동속도 변화
                switch (CheckWalk)
                {
                    case false:
                        //애니메이션value값
                        MoveValue = 1f;
                        break;

                    case true:
                        //이동속도 0.5배
                        CurSpeed *= 0.5f;
                        //애니메이션value값
                        MoveValue = 0.5f;
                        break;
                }
            }
            else
            {
                //이동속도 0.25배
                CurSpeed *= 0.25f;
                //애니메이션value값
                CrouchMove = 1;
            }
        }
        return (MoveDir * CurSpeed) + BackJumpVelocity;
    }

    private bool IsGround()
    {
        float Radius = ContPlayer.radius;
        Vector3 Origin = transform.position + Vector3.up * Radius;

        if(Physics.SphereCast(Origin, Radius, Vector3.down,
                              out RaycastHit Hit, RayDistance, GroundLayer))
        {
            //부딫힌 경사방향을 저장
            GroundNormal = Hit.normal;
            //지면과 수직벡터사이 각도구함
            float SlopeAngle = Vector3.Angle(Vector3.up, GroundNormal);
            //한계값보다 경가사 크면 미끄러짐
            if(SlopeAngle > SlopeLimitAngle)
            {
                CheckSliding = true;
                return false;
            }
            CheckSliding = false;
            return true;
        }
        //공중에 떠있다면 초기화
        CheckSliding = false;
        GroundNormal = Vector3.up;
        return false;

        //return Physics.SphereCast(Origin, Radius, Vector3.down, out RaycastHit Hit, RayDistance, GroundLayer);
    }

    /// <summary>
    /// 루트모션을 제어하기위한 함수
    /// </summary>
    private void OnAnimatorMove()
    {
        //구르기 동작때는 본스크립트에서 작동
        if(CheckRoll == true && Anim != null)
        {
            //루트모션 키고 루트모션 이동량에 따라 컨트롤러에 값 전달
            Anim.applyRootMotion = true;
            Vector3 DeltaPosition = Anim.deltaPosition;
            ContPlayer.Move(DeltaPosition);
        }

        //나머지 애니메이션에서 루트모션이 켜질때
        else if(Anim != null && Anim.applyRootMotion == true)
        {
            Vector3 DeltaPosition = Anim.deltaPosition;
            ContPlayer.Move(DeltaPosition);
        }
    }

    public void TakeDamage(GameObject Attacker, float Damage)
    {
        //이미 경직중이거나 죽으면 실행 금지
        if (CheckHit == true || CheckDeath == true) return;

        CurHp -= Damage;
        ChangeHp?.Invoke(CurHp, MaxHp);
        
        //비전투상태일때 맞으면 피격애니메이션만 진행
        if (CheckCombat == false)
        {
            Anim.SetTrigger("Small Hit");
        }
        //잠깐의 무적시간 후 조작가능
        else if(CheckCombat == true)
        {
            //전투 중에 맞으면 애니메이션과 함께 살짝 밀려남
            StartCoroutine(Invincible(Attacker.transform.position, InvincibleTime));
        }


        if (CurHp <= 0)//0이 되면 사망처리
        {
            Anim.SetTrigger("Die");
            CheckDeath = true;
        }
    }

    IEnumerator Invincible(Vector3 Pos, float Timer)
    {
        CheckHit = true;
        CheckKnockBack = true;
        //넉백 방향 계산
        Vector3 dir = transform.position - Pos;
        dir.y = 0f;
        dir.Normalize();
        float KnockBackTimer = 0f;
        //전달받은 무적시간 동안 뒤로 밀려남
        while(KnockBackTimer < Timer)
        {
            //유니티 물리사이클에 맞춰 한프레임 쉼
            yield return new WaitForFixedUpdate();
            KnockBackTimer += Time.fixedDeltaTime;
            KnockBackVelocity = dir * KnockBackSpeed;
        }
        KnockBackVelocity = Vector3.zero;
        CheckKnockBack = false;
        CheckHit = false;
    }

    private void EndRoll()
    {
        CheckRoll = false;
        CheckHit = false;
        Anim.applyRootMotion = false;
    }

    private void EnableCombo()
    {
        CanCombo = true;
    }

    private void DisalbeCombo()
    {
        CanCombo = false;
    }

    private void UnlockMove()
    {
        isDialogue = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (AtkPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.matrix = AtkPoint.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, AtkHalfBox * 2f);
    }
}
