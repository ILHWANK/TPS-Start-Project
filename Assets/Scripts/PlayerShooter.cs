using UnityEngine;

/* 플레잉어 케릭터가 총을 사용하거나 장전하는 역할을 할 소스
 * 플레이어의 왼손 위치가 총구 앞 쪽 손잡이에 위치 하도록  
 */
public class PlayerShooter : MonoBehaviour
{
    public enum AimState // 해당 상태를확인 해서 플레이어가 무기를 사용하는 State 라면 카메라 바향으로 시선을 이동 
    {
        Idle,
        HipFire
    }

    public AimState aimState { get; private set; }

    public Gun gun;
    public LayerMask excludeTarget;
    
    private PlayerInput playerInput;
    private Animator playerAnimator;
    private Camera playerCamera;

    private float waitingTimeForReleasingim = 2.5f; //2.5초를 의미함.
    private float lastFireInputTime;
    
    private Vector3 aimPoint;
    /* TPS게임 에서는 조준점이 중앙에 위치하는데 이때 플레이어와 조준점 사이에 장애물이 있는경우
     * 장애물이 아닌 카메라 중간에 조준점에 총알이 명중하는 경우를 구분하기 위해 사용/
     */
    private bool linedUp => !(Mathf.Abs( playerCamera.transform.eulerAngles.y - transform.eulerAngles.y) > 1f);
    private bool hasEnoughDistance => !Physics.Linecast(transform.position + Vector3.up * gun.fireTransform.position.y,gun.fireTransform.position, ~excludeTarget);
    // 총구앞쪽 부분에 특정 사물에 겹친 상황에서는 총을 발사 할 수 없도록 하기 위해서
    void Awake()
    {
        if (excludeTarget != (excludeTarget | (1 << gameObject.layer)))
        {
            excludeTarget |= 1 << gameObject.layer;
        }// 플레이어가 자기 자신을 쏘는 것을 방지하기 위해서 
    }

    private void Start()
    {
        playerCamera = Camera.main;
        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        aimState = AimState.Idle;
        gun.gameObject.SetActive(true);
        gun.Setup(this);
    }

    private void OnDisable()
    {
        aimState = AimState.Idle;
        gun.gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (playerInput.fire)
        {
            lastFireInputTime = Time.time;//매번 현제 시간으로 초기화 해줌 
            Shoot();
        }
        else if (playerInput.reload)
        {
            Reload();
        }
    }

    private void Update()
    {
        UpdateAimTarget(); // MainTarget 위치를 계속해서 확인 하기위해서 사용

        var angle = playerCamera.transform.eulerAngles.x;
        if (angle >= 270f) // -90 도가 아닌 270 도가 들어 오는 경우 -90 도로 변경하기 위해/
        {
            angle -= 360f;
        }

        angle = angle / -180f + 0.5f;
        playerAnimator.SetFloat("Angle", angle);

        if(!playerInput.fire && Time.time >= waitingTimeForReleasingim + lastFireInputTime)
        {
            aimState = AimState.Idle;
        }
        UpdateUI();
    }

    public void Shoot()
    {
        if(aimState == AimState.Idle)
        {
            if (linedUp) aimState = AimState.HipFire;
        }
        else if(aimState == AimState.HipFire)
        {
            if (hasEnoughDistance)
            {
                if (gun.Fire(aimPoint))
                {
                    playerAnimator.SetTrigger("Shoot");
                }
            }
        }
        else
        {
            aimState = AimState.Idle;
        }
    }

    public void Reload()
    {
        if (gun.Reload())
        {
            playerAnimator.SetTrigger("Reload");
        }   
    }

    private void UpdateAimTarget()
    {
        RaycastHit hit;

        var ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));

        if(Physics.Raycast(ray, out hit, gun.fireDistance, ~excludeTarget))
        {
            aimPoint = hit.point;

            if(Physics.Linecast(gun.fireTransform.position, hit.point, out hit, ~excludeTarget))
            {
                aimPoint = hit.point;
            }
        }
        else
        {
            aimPoint = playerCamera.transform.position + playerCamera.transform.forward * gun.fireDistance;
        }
    } 

    private void UpdateUI() // 화면에 UI 요소를 수정함
    {
        if (gun == null || UIManager.Instance == null) return;
        
        UIManager.Instance.UpdateAmmoText(gun.magAmmo, gun.ammoRemain);
        // UIManager 는 싱글턴 패턴을 사용함
        UIManager.Instance.SetActiveCrosshair(hasEnoughDistance);
        UIManager.Instance.UpdateCrossHairPosition(aimPoint);
    }

    private void OnAnimatorIK(int layerIndex)// 애니메이션인 갱 신 될때 바다 실행 됨.
    {
        if(gun == null || gun.state == Gun.State.Reloading)
        {
            return;
        }

        playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);
        playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1.0f);

        playerAnimator.SetIKPosition(AvatarIKGoal.LeftHand, gun.leftHandMount.position);
        playerAnimator.SetIKRotation(AvatarIKGoal.LeftHand, gun.leftHandMount.rotation);
    }
}