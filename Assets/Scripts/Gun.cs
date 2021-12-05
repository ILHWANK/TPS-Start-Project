using System;
using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public enum State
    {
        Ready,
        Empty,
        Reloading
    }
    public State state { get; private set; }
    
    private PlayerShooter gunHolder;
    private LineRenderer bulletLineRenderer;
    
    private AudioSource gunAudioPlayer;
    public AudioClip shotClip;
    public AudioClip reloadClip;
    
    public ParticleSystem muzzleFlashEffect;
    public ParticleSystem shellEjectEffect;
    
    public Transform fireTransform;
    public Transform leftHandMount;

    public float damage = 25;
    public float fireDistance = 100f;

    public int ammoRemain = 100;
    public int magAmmo;
    public int magCapacity = 30;

    public float timeBetFire = 0.12f;
    public float reloadTime = 1.8f;
    
    [Range(0f, 10f)] public float maxSpread = 3f;
    [Range(1f, 10f)] public float stability = 1f;
    [Range(0.01f, 3f)] public float restoreFromRecoilSpeed = 2f;
    private float currentSpread;
    private float currentSpreadVelocity;

    private float lastFireTime;

    private LayerMask excludeTarget;

    private void Awake()// 필요한 컨퍼넌트를 가지고오기 위해서  
    {
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();

        bulletLineRenderer.positionCount = 2;
        bulletLineRenderer.enabled = false;
    }

    public void Setup(PlayerShooter gunHolder)// 현제 총을 들고 있는 사람이 누구인지 확인 
    {
        this.gunHolder = gunHolder;
        excludeTarget = gunHolder.excludeTarget;
    }

    private void OnEnable()
    {
        magAmmo = magCapacity;
        currentSpread = 0f;
        lastFireTime = 0f;
        state = State.Ready;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public bool Fire(Vector3 aimTarget)
    {
        if(state == State.Ready && Time.time >= lastFireTime + timeBetFire)// 총을 발사 할 수 있는경우에만 사용함 
        {
            var fireDirection = aimTarget - fireTransform.position;

            var xError = Utility.GedRandomNormalDistribution(0f, currentSpread);
            var yError = Utility.GedRandomNormalDistribution(0f, currentSpread);

            fireDirection = Quaternion.AngleAxis(yError, Vector3.up) * fireDirection;
            fireDirection = Quaternion.AngleAxis(xError, Vector3.right) * fireDirection;

            currentSpread += 1f / stability; 

            lastFireTime = Time.time;
            Shot(fireTransform.position, fireDirection);

            return true;
        }

        return false;
    }
    
    private void Shot(Vector3 startPoint, Vector3 direction)
    {
        RaycastHit hit;
        Vector3 hitPosition;

        if(Physics.Raycast(startPoint, direction, out hit, fireDistance, ~excludeTarget))
        {
            var target = hit.collider.GetComponent<IDamageable>();
            // 레이케스트 이후 데미지를 가할수 있는지 확인을 위해

            if(target != null)// 데미지를 가할 수 있다면 
            {
                DamageMessage damageMessage;

                damageMessage.damager = gunHolder.gameObject;
                damageMessage.amount = damage;
                damageMessage.hitPoint = hit.point;
                damageMessage.hitNormal = hit.normal;

                target.ApplyDamage(damageMessage);

                //데미지 정보를 전달하기위해 사용됨 
            }
            else
            {
                EffectManager.Instance.PlayHitEffect(hit.point, hit.normal, hit.transform);
            }
            hitPosition = hit.point;
        }
        else// 총알이 레이케스트 되지않았다면 총알이 최대로 날아 갈수 있는 거리를 표시
        {
            hitPosition = startPoint + direction * fireDistance;
        }

        StartCoroutine(ShotEffect(hitPosition));

        magAmmo--;

        if (magAmmo <= 0) state = State.Empty;
    }

    private IEnumerator ShotEffect(Vector3 hitPosition)
    {
        muzzleFlashEffect.Play();
        shellEjectEffect.Play();

        gunAudioPlayer.PlayOneShot(shotClip);
        // 총을 연사로 사용할 경우 총 소리가 살아지는 것을 방지하기 위해서
        bulletLineRenderer.enabled = true;
        bulletLineRenderer.SetPosition(0, fireTransform.position);
        bulletLineRenderer.SetPosition(1, hitPosition);

        yield return new WaitForSeconds(0.03f);

        bulletLineRenderer.enabled = false;
    }
    
    public bool Reload()
    {
        if(state == State.Reloading || ammoRemain <= 0 || magAmmo >= magCapacity)
        {
            return false;
        }

        StartCoroutine(ReloadRoutine());

        return true;
    }

    private IEnumerator ReloadRoutine()
    {
        state = State.Reloading;
        gunAudioPlayer.PlayOneShot(reloadClip);

        yield return new WaitForSeconds(reloadTime);

        var ammoToFill = Mathf.Clamp(magCapacity - magAmmo, 0, ammoRemain);

        magAmmo += ammoToFill;
        ammoRemain -= ammoToFill;

        state = State.Ready;
    }
     
    private void Update()
    {
        currentSpread = Mathf.Clamp(currentSpread, 0f, maxSpread);
        // 총기 반동 값을 일정 범위를 넘어가지 못하도록 하기위해서  Clamp 를 사용함
        currentSpread = Mathf.SmoothDamp(currentSpread, 0f, ref currentSpreadVelocity, 1f / restoreFromRecoilSpeed);  
    }
}