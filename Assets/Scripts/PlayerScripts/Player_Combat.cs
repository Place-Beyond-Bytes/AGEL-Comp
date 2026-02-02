using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Combat : MonoBehaviour
{
    public LayerMask enemyLayer;
    public Transform attackPoint;
    public StatsUI statsUI;
    public Animator anim;

    public float cooldown = 2;
    private float timer;



    private void Update()
    {
        if(timer > 0)
        {
            timer -= Time.deltaTime;
        }
    }


    public void Attack()
    {
        if(timer <= 0)
        {
            anim.SetBool("isAttacking", true);

            timer = cooldown;
        }

    }


    public void DealDamage()
    {
        Debug.Log("Deal Damage was called");
        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.position, StatsManager.Instance.weaponRange, enemyLayer);

        if (enemies.Length > 0)
        {
            var enemyHealth = enemies[0].GetComponent<Enemy_Health>();
            if (enemyHealth != null)
            {
                enemyHealth.ChangeHealth(-StatsManager.Instance.damage);
                var knockback = enemies[0].GetComponent<Enemy_Knockback>();
                if (knockback != null)
                {
                    knockback.Knockback(transform, StatsManager.Instance.knockbackForce, StatsManager.Instance.knockbackTime, StatsManager.Instance.stunTime);
                }
            }
            else
            {
                var sheepHealth = enemies[0].GetComponent<Sheep_Health>();
                if (sheepHealth != null)
                {
                    sheepHealth.ChangeHealth(-StatsManager.Instance.damage);
                }
            }
        }
    }




    public void FinishAttacking()
    {
        anim.SetBool("isAttacking", false);
    }
}
