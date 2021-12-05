using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor; // Unity Editor 내부에서만 적용 된다. 즉 최종 빌드에서는 빠지게 된다.
#endif

public class Enemy : LivingEntity // 적을 구상하는 Class
{
    private enum State
    {
        Patrol,
        Tracking,
        AttackBegin,
        Attacking
    }
    
    private State state;
    
    private NavMeshAgent agent;
    private Animator animator;

    public Transform attackRoot;
    public Transform eyeTransform;
    
    private AudioSource audioPlayer;
    public AudioClip hitClip;
    public AudioClip deathClip;
    
    private Renderer skinRenderer; // 좀비의 공격력에 따라 피부 색상을 다르게 하기 위해 

    public float runSpeed = 10f;
    [Range(0.01f, 2f)] public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;
    
    public float damage = 30f;
    public float attackRadius = 2f;
    private float attackDistance;
    
    public float fieldOfView = 50f;
    public float viewDistance = 10f;
    public float patrolSpeed = 3f;

    [HideInInspector] public LivingEntity targetEntity; // 플레이어 또는 LivingEntity 를 사용하는 대상  
    //public LivingEntity targetEntity; // 플레이어 또는 LivingEntity 를 사용하는 대상  
    public LayerMask whatIsTarget;


    private RaycastHit[] hits = new RaycastHit[10];
    private List<LivingEntity> lastAttackedTargets = new List<LivingEntity>();
    
    private bool hasTarget => targetEntity != null && !targetEntity.dead;
    

#if UNITY_EDITOR

    private void OnDrawGizmosSelected() //Unity Editor 내부에서 적용되는 Event, 프레임 마다 실행 된다.
    {
        if(attackRoot != null)
        {
            Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
            Gizmos.DrawSphere(attackRoot.position, attackRadius);
        }

        if (eyeTransform != null){
            var leftEyeRotation = Quaternion.AngleAxis(-fieldOfView * 0.5f, Vector3.up);
            var leftRayDirection = leftEyeRotation * transform.forward;
            Handles.color = new Color(1f, 1f, 1f, 0.2f);
            Handles.DrawSolidArc(eyeTransform.position, Vector3.up, leftRayDirection, fieldOfView, viewDistance);
        }
    }
    
#endif
    
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioPlayer = GetComponent<AudioSource>();
        skinRenderer = GetComponentInChildren<Renderer>(); // 자식 에 Renderer 가 있기때문에 InChildren 을 사용 

        var attackPivot = attackRoot.position;
        attackPivot.y = transform.position.y;
        attackDistance = Vector3.Distance(transform.position, attackPivot) + attackRadius;

        agent.stoppingDistance = attackDistance;
        agent.speed = patrolSpeed;
    }

    public void Setup(float health, float damage,
        float runSpeed, float patrolSpeed, Color skinColor) // Enemy 에서 직접 실행하는 것이 아닌 나중에 좀비 생성기에서 사용된다. 
    {
        this.startingHealth = health; //입력으로 들어온 health 값 을 사용함, startinghealth 는 LivingEntity 있는 값.
        this.health = health; //Enemy 의 health 값.  

        this.damage = damage;
        this.runSpeed = runSpeed;
        this.patrolSpeed = patrolSpeed;

        skinRenderer.material.color = skinColor;

        agent.speed = patrolSpeed; //변경된 patrolspeed 를 적용하기 위해서
    }

    private void Start()
    {
        StartCoroutine(UpdatePath());
    }

    private void Update() // 공격및 애니메이션을 Update 해준다.
    {
        if (dead)
        {
            return;
        }

        if(state == State.Tracking)
        {
            var distance = Vector3.Distance(targetEntity.transform.position, transform.position);
            if(distance <= attackDistance)
            {
                BeginAttack(); // 총 공격 과는 다르게 팔을 휘두르거나 한번의 공격에 여려 대상이 공격을 받을 수 있다.
            }
        }

        animator.SetFloat("Speed", agent.desiredVelocity.magnitude);
    }

    private void FixedUpdate()
    {
        if (dead) return;

        if(state == State.AttackBegin || state == State.Attacking) //
        {
            var lookRotation = Quaternion.LookRotation(targetEntity.transform.position - transform.position);
            var targetAngleY = lookRotation.eulerAngles.y;

            targetAngleY = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngleY, ref turnSmoothVelocity, turnSmoothTime);
            transform.eulerAngles = Vector3.up * targetAngleY;
        }// 공격하는 대상을 보도록 회전하는 소스

        if(state == State.Attacking) {
            var direction = transform.forward;
            var deltaDistance = agent.velocity.magnitude * Time.deltaTime;
            // 공격의 괴적이 direction 방향 으로 deltaDistance 만큼 이동한다.

            var size = Physics.SphereCastNonAlloc(attackRoot.position, attackRadius, direction, hits, deltaDistance, whatIsTarget);
            // 감지된 콜라이더의 갯수
            // 움직이면서 공격하는 경우 공격 대상을 지나쳐 공격 할 경우 공격이 정용 되지 않기 때문에 해당 상황을 방지하기 위해서 사용
            
            for (var i = 0; i < size; i++)// 과거의 정보를 사용하지 않고 현재 확인된 hit 범위 안에 대상 만큼 반복한다.
            {
                var attackTargetEntity = hits[i].collider.GetComponent<LivingEntity>();

                if(attackTargetEntity != null && !lastAttackedTargets.Contains(attackTargetEntity))
                // 공격 도중에 공격이 2번 들어 가는 것을 방지 하기 위해
                {
                    var message = new DamageMessage();
                    message.amount = damage;
                    message.damager = gameObject;

                    if(hits[i].distance <= 0f) // hit 위치가 시작 위치와 이미 겹쳐있는경 
                    {
                        message.hitPoint = attackRoot.position;
                    }
                    else
                    {
                        message.hitPoint = hits[i].point;
                    }

                    message.hitNormal = hits[i].normal;
                    // 위 message 에서 공격 message 를 생성함.


                    attackTargetEntity.ApplyDamage(message);// 공격 받은 message
                    lastAttackedTargets.Add(attackTargetEntity);// 공격을 받은 상대방

                    break; // 공격이 끝난 후 종
                }
            }
        }
    }

    private IEnumerator UpdatePath() // 사망하지 않는 이상 무한 반복 됨, 플레이어를 찾기위해 반복 된다.
    {
        while (!dead)
        {
            if (hasTarget)
            {
                if(state == State.Patrol)
                {
                    state = State.Tracking; // 추적 상태로 변경된다.
                    agent.speed = runSpeed; // 뛰어 다니는 상태로 변경 한다.
                }
                agent.SetDestination(targetEntity.transform.position);
            }
            else
            {
                if (targetEntity != null) targetEntity = null; // 대상이 두명 이상인 경우 한명이 죽었을때 다른 한사람을 추적 할 수 있도록 .

                if(state != State.Patrol) // 정찰 상태가 아닌라면 
                {
                    state = State.Patrol; // 정찰 상태로 변경한다.
                    agent.speed = patrolSpeed;
                }

                if (agent.remainingDistance <= 1f){// 정찰 위치 까지 1미터 1유닛 이하인 경우만 새로운 정찰 지점을 지정함.
                    var patrolTargetPosition = Utility.GetRandomPointOnNavMesh(transform.position, 20f, NavMesh.AllAreas);
                    // 본인으 기준에서 20거리 만큼에 있는 랜덤한 지점을 저장한다.
                    agent.SetDestination(patrolTargetPosition);
                }

                var colliders = Physics.OverlapSphere(eyeTransform.position, viewDistance, whatIsTarget);

                foreach(var collider in colliders) // 구안에 있는 모든 collider를 확인함
                {
                    if (!IsTargetOnSight(collider.transform)) //타겟이 없는 경우 다음으로 넘어 간다.
                    {
                        continue;
                    }

                    var livingEntity = collider.GetComponent <LivingEntity>(); // 행동가능한 플레이 추정할 대상인지

                    if(livingEntity != null && !livingEntity.dead) // 상대방이 추적 대상이면서 죽어 있는 경우가 아닌경우
                    {
                        targetEntity = livingEntity; // 해당 대상을 추적한다.
                        break;
                    }
                }
            }
            
            yield return new WaitForSeconds(0.05f); // 0.05초 주기로 반복 된다.
        }
    }
    
    public override bool ApplyDamage(DamageMessage damageMessage)
    {
        if (!base.ApplyDamage(damageMessage)) return false;

        if(targetEntity == null)
        {
            targetEntity = damageMessage.damager.GetComponent<LivingEntity>();
        }

        EffectManager.Instance.PlayHitEffect(damageMessage.hitPoint, damageMessage.hitNormal, transform, EffectManager.EffectType.Flesh);

        audioPlayer.PlayOneShot(hitClip);

        return true;
    }

    public void BeginAttack()
    {
        state = State.AttackBegin;

        agent.isStopped = true;
        animator.SetTrigger("Attack");
    }

    public void EnableAttack()
    {
        state = State.Attacking;
        
        lastAttackedTargets.Clear();
    }

    public void DisableAttack()
    {
        if (hasTarget)
        {
            state = State.Tracking;
        }
        else
        {
            state = State.Patrol;
        }

        agent.isStopped = false;
    }

    private bool IsTargetOnSight(Transform target) 
    {

        var direction = target.position - eyeTransform.position;// 상대방의 방향을 찾는다.
        direction.y = eyeTransform.forward.y; // 높이는 고려 하지 않는다.

        if(Vector3.Angle(direction, eyeTransform.forward) > fieldOfView * 0.5f) // 시아 각에서 버서난 경우
        {
            return false;
        }


        direction = target.position - eyeTransform.position; // target 를 가리는 장애물이 있는기 판단 하기 위해서 초기화 

        RaycastHit hit;

        if (Physics.Raycast(eyeTransform.position, direction, out hit, viewDistance, whatIsTarget)) // 해당 거리에 대상이 어떤 건지 확인
        {
            if(hit.transform == target) // 처음에 봤던 대상과 raycasthit 로 검사한 대상이 갔다면 시아 안에 있다고 판
            {
                return true;
            }
        }

        return false;
    }
    
    public override void Die()
    {
        base.Die();

        GetComponent<Collider>().enabled = false; // 이미 죽은 Collider 를 비활성화 한다.

        //agent.isStopped = true; //agent 의 경우 agent 를 피해서 이동한다.
        agent.enabled = false; // 따라서 agent 를 완전히 비활성화 한다.

        animator.applyRootMotion = true;
        animator.SetTrigger("Die");

        audioPlayer.PlayOneShot(deathClip);
    }
}