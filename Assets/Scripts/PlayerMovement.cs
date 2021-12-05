using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController characterController;
    private PlayerInput playerInput;
    private PlayerShooter playerShooter;
    private Animator animator;
    
    private Camera followCam;
    
    public float speed = 6f;
    public float jumpVelocity = 20f;
    [Range(0.01f, 1f)] public float airControlPercent;
     // 플레이어가 공중에 있을때 원래 속도의 몇퍼센트 만큼 움직일지 결정함

    public float speedSmoothTime = 0.1f;
    public float turnSmoothTime = 0.1f;
    
    private float speedSmoothVelocity;
    private float turnSmoothVelocity;
    
    private float currentVelocityY;
    
    public float currentSpeed =>
        new Vector2(characterController.velocity.x, characterController.velocity.z).magnitude;
    
    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        playerShooter  = GetComponent<PlayerShooter>();
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        followCam = Camera.main;
    }

    private void FixedUpdate()//물리 갱신 주기 따라 작동할 함수 
    {
        if (currentSpeed > 0.2f || playerInput.fire || playerShooter.aimState == PlayerShooter.AimState.HipFire) Rotate();
        // 플레이어가 움직이지 않을 때에는 카메라의 위치를 자유롭게 변경 하는하지만 움직임이 있는 경우 또는 무기를 사용하려는 경우 앞을 보도록 변경 함. 
        // 일정 시간이 지난 경우에도 앞을 보도록 설정 하기위해서 뒤에 조건 추가함 

        Move(playerInput.moveInput);
        
        if (playerInput.jump) Jump();
    }

    private void Update()
    {
        UpdateAnimation(playerInput.moveInput);
    }

    public void Move(Vector2 moveInput)
    {
        var targetSpeed = speed * moveInput.magnitude;
        // magnitude 을 사용하면 컨트롤러 사용시 1이하의 값이 들어 왔을때 천천히 이동하거나 할 때 사용된다.
        var movedirection = Vector3.Normalize(transform.forward * moveInput.y + transform.right * moveInput.x);
        // 길이가 1인 방향 Vector를 만들어 주기 위해서  Vector.Normalize를 사용

        var smoothTime = characterController.isGrounded ? speedSmoothTime : speedSmoothTime / airControlPercent;
        // 케릭터가 공중에 떠 있는 동안은 케릭터의 움직임 조작이 어렵도록 조절 하기 위해 사용함

        targetSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, smoothTime);
        // 현제 스피드, 목표값, 변화량, 지연시ㅍ  

        currentVelocityY += Time.deltaTime * Physics.gravity.y;

        var velocity = movedirection *  targetSpeed + Vector3.up * currentVelocityY;

        characterController.Move(velocity * Time.deltaTime);

        if (characterController.isGrounded) currentVelocityY = 0f;
        // 케릭터가 점프, 떨어지는 도중 바닥에 착지 한다면 Y Vector를 0 으로 초기화 해준다.  
    }

    public void Rotate()
    {
        var targetRotation = followCam.transform.eulerAngles.y;

        targetRotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, turnSmoothTime);

        transform.eulerAngles = Vector3.up * targetRotation;
    }

    public void Jump()
    {
        if (!characterController.isGrounded) return;
        currentVelocityY = jumpVelocity; 
    }

    private void UpdateAnimation(Vector2 moveInput)
    {
        var animationSpeedPercent = currentSpeed / speed;
        animator.SetFloat("Vertical Move",   moveInput.y * animationSpeedPercent, 0.05f, Time.deltaTime);
        animator.SetFloat("Horizontal Move", moveInput.x * animationSpeedPercent, 0.05f, Time.deltaTime);
        // 스피드에 따라 animation 동작 속도를 조절 하기 위해,
    }
}