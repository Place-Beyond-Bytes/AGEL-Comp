using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGEL;

public class PlayerMovement : MonoBehaviour
{
    public int facingDirection = 1;

    public Rigidbody2D rb;
    public Animator anim;

    private bool isKnockedBack;
    public bool isShooting;

    public Player_Combat player_Combat;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError("PlayerMovement: Rigidbody2D not found on player GameObject!");
            }
        }
    }

    private void Update()
    {
        if (Input.GetButtonDown("Slash") && player_Combat.enabled == true)
        {
            player_Combat.Attack();
        }

        // AGEL: Log episode on any key except movement keys
        if (Input.anyKeyDown)
        {
            if (!Input.GetKeyDown(KeyCode.UpArrow) &&
                !Input.GetKeyDown(KeyCode.DownArrow) &&
                !Input.GetKeyDown(KeyCode.LeftArrow) &&
                !Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (AGELAgent.Instance != null)
                    StartCoroutine(AGELAgent.Instance.LLMPerceiveAndAct());
            }
        }
    }


    // Fixed Update is called 50x second
    void FixedUpdate()
    {
        if(isShooting == true)
        {
            if (rb != null)
                rb.velocity = Vector2.zero;
        }
        else if (isKnockedBack == false)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            if (horizontal > 0 && transform.localScale.x < 0 ||
                horizontal < 0 && transform.localScale.x > 0)
            {
                Flip();
            }

            anim.SetFloat("horizontal", Mathf.Abs(horizontal));
            anim.SetFloat("vertical", Mathf.Abs(vertical));

            if (rb != null && StatsManager.Instance != null)
                rb.velocity = new Vector2(horizontal, vertical) * StatsManager.Instance.speed;
            else if (rb == null)
                Debug.LogError("PlayerMovement: Rigidbody2D is null in FixedUpdate!");
            else if (StatsManager.Instance == null)
                Debug.LogError("PlayerMovement: StatsManager.Instance is null in FixedUpdate!");
        }
    }



    void Flip()
    {
        facingDirection *= -1;
        transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
    }


    public void Knockback(Transform enemy, float force, float stunTime)
    {
        isKnockedBack = true;
        Vector2 direction = (transform.position - enemy.position).normalized;
        rb.velocity = direction * force;
        StartCoroutine(KnockbackCounter(stunTime));
        // AGEL: Log episode on knockback
        if (AGELAgent.Instance != null)
            StartCoroutine(AGELAgent.Instance.LLMPerceiveAndAct());
    }


    IEnumerator KnockbackCounter(float stunTime)
    {
        yield return new WaitForSeconds(stunTime);
        rb.velocity = Vector2.zero;
        isKnockedBack = false;
    }

}
