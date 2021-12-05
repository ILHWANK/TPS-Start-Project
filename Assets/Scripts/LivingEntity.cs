using System;
using UnityEngine;

/* IDamageable 을 사용 하였기 때문에 반드시 구현 해주어야함
 * 데미지 받 
 */
public class LivingEntity : MonoBehaviour, IDamageable // 게임속 생명체 들이 공통적으로 가지게 될 기능 구현 
{
    public float startingHealth = 100f;
    public float health { get; protected set; }
    public bool dead { get; protected set; }
    
    public event Action OnDeath;
    
    private const float minTimeBetDamaged = 0.1f;
    // 시간 오차 때문에 짧은 시간에 데미지가 두번 들어 가는 것을 방지 하기 위한 여유시간
    // 시간이 너무 크면 정상적인 게임 플레이가 불가능 할 수 있다(무적)
    // 게임 도중 변경할 일이 없기 때문에 const로 선/
    private float lastDamagedTime; 

    protected bool IsInvulnerabe
    {
        get
        {
            if (Time.time >= lastDamagedTime + minTimeBetDamaged) return false;

            return true;
        }
    }
    
    protected virtual void OnEnable()
    {
        dead = false;
        health = startingHealth;
    }

    public virtual bool ApplyDamage(DamageMessage damageMessage)
    {
        if (IsInvulnerabe || damageMessage.damager == gameObject || dead) return false;
        // 특정 상황에서 잘못된 데미지가 들어온 경우 예외 처리
        // 자기 자신 공격, 이미 사망한 경우 등..

        lastDamagedTime = Time.time;
        health -= damageMessage.amount;
        
        if (health <= 0) Die();

        return true;
    }
    
    public virtual void RestoreHealth(float newHealth)// 체력 회,
    {
        if (dead) return;
        
        health += newHealth;
    }
    
    public virtual void Die()
    {
        if (OnDeath != null) OnDeath();
        
        dead = true;
    }
}