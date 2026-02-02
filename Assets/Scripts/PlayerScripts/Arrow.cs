using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGEL;

public class Arrow : MonoBehaviour
{
    public Rigidbody2D rb;
    public Vector2 direction = Vector2.right;
    public float lifeSpawn = 2;
    public float speed;

    public LayerMask enemyLayer;
    public LayerMask obstacleLayer;

    public SpriteRenderer sr;
    public Sprite buriedSprite;

    public int damage;
    public float knockbackForce;
    public float knockbackTime;
    public float stunTime;



    // Start is called before the first frame update
    void Start()
    {
        rb.velocity = direction * speed;
        RotateArrow();
        Destroy(gameObject, lifeSpawn);
    }




    private void RotateArrow()
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }



    public void OnCollisionEnter2D(Collision2D collision)
    {
        if((enemyLayer.value & (1 << collision.gameObject.layer)) > 0)
        {
            var enemyHealth = collision.gameObject.GetComponent<Enemy_Health>();
            if (enemyHealth != null)
                enemyHealth.MarkKilledByArrow();
            enemyHealth.ChangeHealth(-damage);
            collision.gameObject.GetComponent<Enemy_Knockback>().Knockback(transform, knockbackForce, knockbackTime, stunTime);
            AttachToTarget(collision.gameObject.transform);
            if (AGELAgent.Instance != null)
                StartCoroutine(AGELAgent.Instance.LLMPerceiveAndAct());
        }

        else if ((obstacleLayer.value & (1 << collision.gameObject.layer)) > 0)
        {
            AttachToTarget(collision.gameObject.transform);
            if (AGELAgent.Instance != null)
                StartCoroutine(AGELAgent.Instance.LLMPerceiveAndAct());
        }
    }



    private void AttachToTarget(Transform target)
    {
        sr.sprite = buriedSprite;

        rb.velocity = Vector2.zero;
        rb.isKinematic = true;

        transform.SetParent(target);
    }
}
