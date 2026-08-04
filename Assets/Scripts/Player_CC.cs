using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEditor;
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
    [Header("무기별 애니메이터")]
    [SerializeField] RuntimeAnimatorController NonCombatController;
    [SerializeField] RuntimeAnimatorController OneHandSwordController;
    [SerializeField] RuntimeAnimatorController TwoHandAxeController;
    [Header("플레이어 무기 관련 설정")]
    [SerializeField] Transform TrsWeapons;
    [SerializeField] LayerMask TargetLayer;
    [Tooltip("몇초 동안 공격판정박스를 킬 것인지에 대한 값")]
    [SerializeField] float PlayerAtkDuration;

    GameObject CurrentWeapon;
    float CurrentWeaponAtk;
    Vector3 CurrentWeaponAtkHalfBox;
    float PlayerAtkTimer = 0f;
    bool CheckAtk;
    List<ITakeDamage> HitTartgetList = new List<ITakeDamage>();

    [Header("플레이어 세팅값")]
    [SerializeField] float MaxHp;
    [SerializeField] float CurHp;
    [SerializeField] float AtkPower;
    [SerializeField] float InvincibleTime;
    [SerializeField] float MoveSpeed;
    [SerializeField] float JumpForce;
    [SerializeField] float BackJumpForce;
    [SerializeField] float BackJumpSpeed;
    [SerializeField] float KnockBackSpeed;
    [Header("블렌드 트리 보간 속도")]
    [Tooltip("값이 작을수록 즉각 반응하고, 크면 부드럽지만 느리게 전환됩니다.")]
    [SerializeField] float DampTime;
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

    Vector3 MoveDir;
    Vector3 KnockBackVelocity = Vector3.zero;


    [SerializeField] float VelocityY;
    float MoveValue;
    float CombatValue;
    [Header("애니메이션 간 딜레이타임")]
    [SerializeField] float DelayTime;

    int CrouchMove;

    bool isGround;
    bool isCrouch;
    bool isWalk;
    bool isRoll;
    bool isCombat;
    bool isJump;
    [SerializeField] bool CanCombo = false;
    [SerializeField] bool isHit;
    [SerializeField] bool isHaveWeapon;
    bool isBackJump;
    bool isDeath;
    bool isKnockBack;
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
    public GameObject ReturnWeapon()
    {
        if (CurrentWeapon == null) return null;
        return CurrentWeapon;
    }
    public float ReturnAtk()
    {
        return AtkPower;
    }
    public float ReturnCurHp()
    {
        return CurHp;
    }
    public void OnSlashAttack()
    {
        if(CurrentWeapon == null) return;

         ParticleSystem[] particles = CurrentWeapon.GetComponentsInChildren<ParticleSystem>();

        foreach(ParticleSystem ps in particles)
        {
            ps.Stop();
            ps.Play();
        }
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
        Anim.applyRootMotion = false;

        if (Camera.main != null)
        {
            TrsMainCam = Camera.main.transform;
        }

        //이벤트 설정
        ChangeHp?.Invoke(CurHp, MaxHp);
        UiManager.Instance.OffTalk += UnlockMove;

        //시작할때 무기를 들고있는지 확인하고 맞는 애니메이터로 교체
        if(Anim.runtimeAnimatorController == null)
        {
            switch (CurrentWeapon)
            {
                case null:
                    Anim.runtimeAnimatorController = NonCombatController;
                    break;
                default:
                    //무기의 레이어가 검이고, 전투모드일때
                    if (CurrentWeapon.layer == LayerMask.NameToLayer("Sword") && isCombat == true)
                    {
                        Anim.runtimeAnimatorController = OneHandSwordController;
                    }
                    //무기의 레이어가 도끼이고, 전투모드일때
                    if (CurrentWeapon.layer == LayerMask.NameToLayer("WarAxe") && isCombat == true)
                    {
                        Anim.runtimeAnimatorController = TwoHandAxeController;
                    }
                    break;
            }
        }
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
        if (isDeath == true) return;

        isAttack = Anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack");

        if (isHit == true) { }
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
        CheckRuntimeAnimator();
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
        //커서가 보이고있는 상태라면 카메라 회전만 금지
        if (UiManager.Instance.CurrentCursorState())
        {
            MoveDir = new Vector3(move.x, 0f, move.y).normalized;
        }
        else
        {
            CamForward.y = 0f;
            CamRight.y = 0f;
            CamForward.Normalize();
            CamRight.Normalize();

            MoveDir = (CamForward * move.y + CamRight * move.x).normalized;
        }
    }

    /// <summary>
    /// 점프입력받았는지 체크하고, 점프실행
    /// </summary>
    private void InputJump()
    {
        if (IapJump.action == null || isAttack == true) return;
        //점프키가 눌리고 캐릭터가 땅에 닿아있다면 점프실행
        if (IapJump.action.WasPressedThisFrame() && isGround == true)
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
        if (isJump == true) yield break;

        isJump = true;
        CurBackJumpSpeed = BackJumpSpeed;

        if(MoveValue == 0f)//제자리일때
        {
            Anim.SetTrigger("Jump");
            if(isCombat == true)//캐릭터가 보는 방향 뒷방향으로 점프
            {
                isBackJump = true;
                VelocityY = BackJumpForce;
            }
            if(isCombat == false)
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

        while(CheckGround() == false)//공중에 떠있는동안 기다림
        {
            yield return new WaitForFixedUpdate();
        }

        isJump = false;
        isBackJump = false;
        Anim.ResetTrigger("Jump");
        Anim.ResetTrigger("RunningJump");
    }
    /// <summary>
    /// 구르기 입력시 조건체크하고, 애니메이션 실행
    /// </summary>
    private void InputRoll()
    {
        if (IapRoll.action == null) return;

        if (IapRoll.action.WasPressedThisFrame() && isGround == true)
        {
            StartCoroutine(RollRoutine());
        }
    }

    IEnumerator RollRoutine()
    {
        //중복실행방지
        if(isRoll == true) yield break;
        isRoll = true;
        isHit = true;
        //이동입력이 있다면 그 방향으로 회전
        if(MoveDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(MoveDir);
        }
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

        if(IapCrouch.action.WasPressedThisFrame() && isCrouch == false)
        {
            isCrouch = true;
            StartCoroutine(CrouchCoolTime());
        }
        else if(IapCrouch.action.WasPressedThisFrame() && isCrouch == true)
        {
            isCrouch = false;
            StartCoroutine(CrouchCoolTime());
        }
    }
    IEnumerator CrouchCoolTime()
    {
        yield return new WaitForSeconds(DelayTime);
    }

    private void InputCombat()
    {
        if(IapCombat == null) return;

        if(IapCombat.action.WasPressedThisFrame() && isCombat == false)
        {
            //무기를 들고 있는지 확인
            isHaveWeapon = CheckWeapon();
            if(isHaveWeapon == true)
            {
                isCombat = true;
                CombatValue = 1f;
                CurrentWeapon.SetActive(true);
            }
            else
            {
                UiManager.Instance.StartInfoPanel("무기가 없습니다.");
            }
        }
        else if(IapCombat.action.WasPressedThisFrame() && isCombat == true)
        {
            isCombat = false;
            CombatValue = 0f;
            CurrentWeapon.SetActive(false);
        }
    }

    private bool CheckWeapon()
    {
        if(TrsWeapons == null) return false;

        foreach(Transform child in TrsWeapons)
        {
            if (child.CompareTag("Weapons"))
            {
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// 걷기 상태인지 체크하고, bool값 변경
    /// </summary>
    private void InputWalk()
    {
        if (IapWalk.action == null) return;

        if (IapWalk.action.WasPressedThisFrame() && isWalk == false)
        {
            isWalk = true;
        }
        else if (IapWalk.action.WasPressedThisFrame() && isWalk == true)
        {
            isWalk = false;
        }
    }
    /// <summary>
    /// 공격입력받는 함수
    /// </summary>
    private void InputAttack()
    {
        if (IapAttack.action == null || isCombat == false) return;
        //전투모드이고 공격키 눌렀을때
        if(IapAttack.action.WasPressedThisFrame() && isCombat == true)
        {
            AttackProcess();
        }
    }

    private void AttackProcess()
    {
        //구르고있거나 앉은상태이거나 공중에 떠있으면 공격불가
        if (isRoll == true || isCrouch == true || CheckGround() == false) return;

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
            ResetAni();
            OnDialogue?.Invoke();
        }
        //현재 대화상태중이고 이동불가상태이며 상호작용키 입력이 들어왔을때
        else if(isDialogue == true && IapInteract.action.WasPressedThisFrame())
        {
            //다음대사로 넘어가라고 talkmanager에게 전달
            UiManager.Instance.NextDialogueText();
        }
    }

    private void ResetAni()
    {
        Anim.SetFloat("MoveValue", 0f);
        Anim.SetFloat("CombatValue", 0f);
        Anim.SetInteger("CrouchMove", 0);
        Anim.ResetTrigger("Jump");
        Anim.ResetTrigger("RunningJump");
        Anim.ResetTrigger("Attack");
        Anim.ResetTrigger("Crouch");
        Anim.ResetTrigger("Roll");
    }

    /// <summary>
    /// 애니메이터에게 변수값 전달하는 함수
    /// </summary>
    private void CheckAni()
    {
        if (isDialogue == true) return;

        Anim.SetFloat("MoveValue", MoveValue, DampTime, Time.deltaTime);
        Anim.SetInteger("CrouchMove", CrouchMove);
        Anim.SetBool("Walk", isWalk);
        Anim.SetBool("Big Hit", isKnockBack);
        Anim.SetBool("Crouch", isCrouch);
        if(isCombat)
        {
            Anim.SetBool("Combat", isCombat);
            Anim.SetBool("Combo", CanCombo);
            Anim.SetFloat("CombatValue", CombatValue);
        }
    }
    /// <summary>
    /// 현재 들고 있는 무기 확인 후 맞는 애니메이터로 변경
    /// </summary>
    private void CheckRuntimeAnimator()
    {
        //전투 모드상태라면
       if(isCombat)
        {
            if(CurrentWeapon.layer == LayerMask.NameToLayer("Sword"))
            {
                Anim.runtimeAnimatorController = OneHandSwordController;
            }
            else if(CurrentWeapon.layer == LayerMask.NameToLayer("WarAxe"))
            {
                Anim.runtimeAnimatorController = TwoHandAxeController;
            }
        }
        else
        {
            Anim.runtimeAnimatorController = NonCombatController;
        }
    }

    private void CheckRollInvincible()
    {
        //현재 roll태그를 가진 애니메이션이 재생중인지 확인
        bool CheckRollAnim = Anim.GetCurrentAnimatorStateInfo(0).IsTag("Roll");
        if(CheckRollAnim == true)
        {
            //구르는동안 무적
            isHit = true;
        }
        else
        {
            //넉백중이 아니라면 무적 플래그 끔
            if(isKnockBack == false && isRoll == false)
            {
                isHit = false;
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
        if (CheckAtk == false || CurrentWeapon == null) return;

        PlayerAtkTimer += Time.deltaTime;
        if(PlayerAtkTimer >= PlayerAtkDuration)
        {
            CheckAtk = false;
            return;
        }
        Collider[] HitTargets = Physics.OverlapBox(CurrentWeapon.transform.position, CurrentWeaponAtkHalfBox,
                                CurrentWeapon.transform.rotation, TargetLayer);
        foreach(Collider Target in HitTargets)
        {
            if(Target.TryGetComponent<ITakeDamage>(out var Damage))
            {
                //리스트에 들어있는데 중복으로 데미지 주는 것 방지용
                if (HitTartgetList.Contains(Damage)) continue;
                //플레이어 현재공격력 + 무기 공격력
                Damage.TakeDamage(this.gameObject, AtkPower + CurrentWeaponAtk);
                HitTartgetList.Add(Damage);
            }
        }
    }


    private void FixedUpdate()
    {
        if (isDeath == true || isDialogue == true) return;

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
        if(isRoll == false && isAttack == false &&
            isHit == false && MoveDir.sqrMagnitude >0.01f)
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
        if (CheckGround() == true && isJump == false)
        {
            isGround = true;
            VelocityY = -2f;
        }
        //땅이 아니거나 경사가 가파르면
        else
        {
            isGround = false;
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
        if (isRoll == true || isAttack == true) return Vector3.zero;

        //넉백일때
        if(isHit == true)
        {
            return KnockBackVelocity;
        }

        float CurSpeed = MoveSpeed;//캐릭터 이동속도

        Vector3 BackJumpVelocity = Vector3.zero;

        //전투모드이고 입력값없이 점프할때
        if (isJump == true && isCombat == true && isBackJump == true)
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
            if (isCrouch == false)
            {
                //걷기일땐 이동속도 변화
                switch (isWalk)
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
                //이동속도 0.3배
                CurSpeed *= 0.3f;
                //애니메이션value값
                CrouchMove = 1;
            }
        }
        return (MoveDir * CurSpeed) + BackJumpVelocity;
    }

    private bool CheckGround()
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
        //구르기
        if(isRoll == true && Anim != null)
        {
            //루트모션 키고 루트모션 이동량에 따라 컨트롤러에 값 전달
            Anim.applyRootMotion = true;
            Vector3 DeltaPosition = Anim.deltaPosition;
            ContPlayer.Move(DeltaPosition);
        }
        //공격하고있을때
        else if(Anim != null && isAttack == true)
        {
            Anim.applyRootMotion = true;
            Vector3 DeltaPosition = Anim.deltaPosition;
            ContPlayer.Move(DeltaPosition);
        }

        //나머지 애니메이션에서 루트모션이 켜질때
        //else if(Anim != null && Anim.applyRootMotion == true)
        //{
        //    Vector3 DeltaPosition = Anim.deltaPosition;
        //    ContPlayer.Move(DeltaPosition);
        //}
    }

    public void TakeDamage(GameObject Attacker, float Damage)
    {
        //이미 경직중이거나 죽으면 실행 금지
        if (isHit == true || isDeath == true) return;

        CurHp -= Damage;
        ChangeHp?.Invoke(CurHp, MaxHp);
        
        //비전투상태일때 맞으면 피격애니메이션만 진행
        if (isCombat == false)
        {
            Anim.SetTrigger("Small Hit");
        }
        //잠깐의 무적시간 후 조작가능
        else if(isCombat == true)
        {
            //전투 중에 맞으면 애니메이션과 함께 살짝 밀려남
            StartCoroutine(Invincible(Attacker.transform.position, InvincibleTime));
        }


        if (CurHp <= 0)//0이 되면 사망처리
        {
            Anim.SetTrigger("Die");
            isDeath = true;
        }
    }

    public void SetWeapon(WeaponData weapondata, GameObject weapon)
    {
        //스크립트용 변수 저장
        CurrentWeapon = weapon;
        CurrentWeaponAtk = weapondata.weaponAtk;
        CurrentWeaponAtkHalfBox = weapondata.atkHalfbox;
        if(weapon != null)
        {
            weapon.SetActive(false);
        }
    }

    IEnumerator Invincible(Vector3 Pos, float Timer)
    {
        isHit = true;
        isKnockBack = true;
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
        isKnockBack = false;
        isHit = false;
    }

    private void EndRoll()
    {
        isRoll = false;
        isHit = false;
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

    private void EndAttack()
    {
        Anim.applyRootMotion = false;
    }

    private void UnlockMove()
    {
        isDialogue = false;
    }
}
