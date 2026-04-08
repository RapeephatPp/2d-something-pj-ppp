using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode interactKey = KeyCode.E; 
    public KeyCode normalDashKey = KeyCode.LeftShift; 
    public KeyCode targetDashKey = KeyCode.F; 
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode attackKey = KeyCode.Mouse0; 
    public KeyCode blockKey = KeyCode.Mouse1; 
    public KeyCode recallKey = KeyCode.R; 
    
    [Header("Character State")]
    public bool isArmed = true; 

    [Header("Movement Settings")]
    public float walkSpeed = 6f;
    public float crouchSpeed = 3f; 
    public float acceleration = 12f;
    public float deceleration = 12f;
    private float moveInputX;
    private float moveSpeed;

    [Header("Jump Settings")]
    public float jumpForce = 14f;
    public float jumpCutMultiplier = 0.5f; 
    public float fallGravityMultiplier = 2.0f; 
    private bool canDoubleJump;

    [Header("Coyote Time & Jump Buffer")]
    public float coyoteTime = 0.15f;
    private float coyoteTimeCounter;
    public float jumpBufferTime = 0.15f;
    private float jumpBufferCounter;

    [Header("Dash Settings")]
    public float normalDashSpeed = 22f;
    public float normalDashDuration = 0.2f;
    public float normalDashCooldown = 1.0f;
    private float nextNormalDashTime;
    private bool isNormalDashing;

    public float dashRange = 7f; 
    public float dashSpeed = 28f;
    public float dashCooldown = 1.5f;
    private float nextDashTime = 0f;
    private bool isTargetDashing = false;

    [Header("Blocking & Armor Break")]
    public float maxGuardGauge = 100f; 
    public float currentGuardGauge;
    [Range(0, 1)] public float blockDamageReduction = 0.5f; 
    public float guardRegenRate = 15f; 
    public float guardDepleteRateBlocking = 8f; 
    public float guardDepleteOnHit = 25f; 
    public float stunDuration = 2.0f; 
    private bool isBlocking;
    private bool isStunned;
    private float stunTimer;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Visual Effects")]
    public Color blockColor = Color.blue;
    public Color armorBreakColor = Color.yellow;
    public Color stunColor = Color.cyan;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private Rigidbody2D rb;
    private float defaultGravity;
    
    public bool isAttacking = false;
    private Coroutine currentHitStop;

    [Header("Animation & Combat System")]
    public Animator animator; 
    private int comboStep = 0;          
    private float lastAttackTime = 0f;  
    public float comboResetTime = 1.2f; 
    public float fullComboCooldown = 0.5f; 
    private float nextComboEnableTime = 0f; 
    
    // 🟢 [เพิ่มกลับมาแล้ว!] ตัวแปรเลือดที่เผลอลบไป
    [Header("Health Settings")]
    public int maxHealth = 5;
    public int currentHealth;

    [Header("Hitbox Objects")]
    public GameObject hitboxCombo1;
    public GameObject hitboxCombo2;
    public GameObject hitboxCombo3;
    public GameObject hitboxAir;

    public int attackDamage = 3; 
    public LayerMask enemyLayer;
    public LayerMask obstacleLayer; 

    [Header("Sword Throw Mechanics")]
    public GameObject thrownSwordPrefab; 
    public Transform throwPoint;         

    private Camera mainCam; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        mainCam = Camera.main;
    }

    private void Start()
    {   
        defaultGravity = rb.gravityScale;
        currentHealth = maxHealth;
        currentGuardGauge = maxGuardGauge;
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        if (UIManager.Instance != null) UIManager.Instance.UpdateHealth(currentHealth); 
    }

    private void Update()
    {   
        if (isTargetDashing || isNormalDashing || isStunned) 
        {
            CheckGrounded(); 
            HandleStunTimer(); 
            return; 
        }

        CheckGrounded();
        HandleInput();
        HandleJump();
        
        if (isArmed)
        {
            HandleCombatInput();
            HandleNormalDashInput();
            HandleBlockingState(); 
        }
        else 
        {
            if (Input.GetKeyDown(recallKey)) RecallSword();
        }

        HandleInteractInput();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (isTargetDashing || isNormalDashing || isStunned) return;
        ApplyMovementPhysics();
        ApplyBetterGravity();
    }

    private void HandleInput()
    {
        moveInputX = Input.GetAxisRaw("Horizontal");
        bool isHoldingCrouch = Input.GetKey(crouchKey);

        if (isHoldingCrouch) moveSpeed = crouchSpeed;
        else moveSpeed = walkSpeed;

        if (isAttacking) moveSpeed *= 0.3f; 

        if (moveInputX != 0 && !isAttacking && !isBlocking)
        {
            Vector3 currentScale = transform.localScale;
            currentScale.x = Mathf.Sign(moveInputX) * Mathf.Abs(currentScale.x);
            transform.localScale = currentScale;
        }
    }

    private void ApplyMovementPhysics()
    {
        float targetSpeed = moveInputX * moveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, 0.9f) * Mathf.Sign(speedDiff);
        rb.AddForce(movement * Vector2.right);
    }

    private void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            canDoubleJump = true;
        }
        else coyoteTimeCounter -= Time.deltaTime;
    }

    private void HandleJump()
    {
        if (isBlocking) return;

        if (Input.GetKeyDown(jumpKey)) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }
        else if (Input.GetKeyDown(jumpKey) && canDoubleJump && coyoteTimeCounter <= 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            canDoubleJump = false;
            jumpBufferCounter = 0f;
        }

        if (Input.GetKeyUp(jumpKey) && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    private void ApplyBetterGravity()
    {
        if (rb.linearVelocity.y < 0) rb.gravityScale = defaultGravity * fallGravityMultiplier;
        else rb.gravityScale = defaultGravity;
    }

    private void HandleNormalDashInput()
    {
        if (isBlocking) return;
        if (Input.GetKeyDown(normalDashKey) && Time.time >= nextNormalDashTime)
            StartCoroutine(NormalDashRoutine());
    }

    private IEnumerator NormalDashRoutine()
    {
        isNormalDashing = true;
        nextNormalDashTime = Time.time + normalDashCooldown;
        
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        
        float dashDirection = Mathf.Sign(transform.localScale.x);
        rb.linearVelocity = new Vector2(dashDirection * normalDashSpeed, 0f);

        yield return new WaitForSeconds(normalDashDuration);

        rb.gravityScale = originalGravity;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); 
        isNormalDashing = false;
    }
    
    public void ChangeAnimator(Animator newAnimator) { animator = newAnimator; }

    private void HandleCombatInput()
    {
        if (Input.GetKey(blockKey) && Input.GetKeyDown(attackKey))
        {
            ThrowSword();
            return;
        }

        if (isBlocking) return;

        // 🟢 1. เปลี่ยนจาก GetKey เป็น GetKeyDown เพื่อบังคับให้ผู้เล่นต้อง "คลิก" เป็นจังหวะ
        if (Input.GetKeyDown(attackKey))
        {
            if (isAttacking || Time.time < nextComboEnableTime) return;

            if (Time.time - lastAttackTime > comboResetTime) comboStep = 0;

            isAttacking = true; 
            lastAttackTime = Time.time;

            if (!isGrounded)
            {
                comboStep = 2; 
                rb.gravityScale = 0.5f; 
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, 0f));
            }
            else
            {
                comboStep++;
                if (comboStep > 3) comboStep = 1;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.8f, rb.linearVelocity.y);
            }

            if (animator != null)
            {
                animator.SetInteger("comboStep", comboStep);
                animator.ResetTrigger("attackTrig");
                animator.SetTrigger("attackTrig");
            }
        }

        // 🟢 2. เพิ่มระบบ Failsafe (กันเหนียว)! 
        // ถ้าเกิดว่าแอนิเมชันเล่นพลาด (Event ไม่ทำงาน) แล้วค้างนานกว่า 1 วินาที ให้รีเซ็ตตัวเองอัตโนมัติ
        if (isAttacking && Time.time - lastAttackTime > 1.0f)
        {
            AnimEvent_EndAttack();
            Debug.LogWarning("ระบบรีเซ็ตการโจมตีอัตโนมัติทำงาน! (เช็ค Animation Event ด่วน)");
        }

        if (Input.GetKeyDown(targetDashKey) && Time.time >= nextDashTime)
        {
            Transform target = FindNearestEnemy();
            if (target != null)
            {
                StartCoroutine(TargetDashAttack(target));
                nextDashTime = Time.time + dashCooldown;
            }
        }
    }

    public void AnimEvent_EnableHitbox()
    {
        if (comboStep == 1 && hitboxCombo1 != null) hitboxCombo1.SetActive(true);
        else if (comboStep == 2 && isGrounded && hitboxCombo2 != null) hitboxCombo2.SetActive(true);
        else if (comboStep == 3 && hitboxCombo3 != null) hitboxCombo3.SetActive(true);
        else if (!isGrounded && hitboxAir != null) hitboxAir.SetActive(true);

        if (CameraShake.Instance != null) StartCoroutine(CameraShake.Instance.Shake(0.1f, 0.05f)); 
    }

    public void AnimEvent_DisableHitbox()
    {
        if (hitboxCombo1 != null) hitboxCombo1.SetActive(false);
        if (hitboxCombo2 != null) hitboxCombo2.SetActive(false);
        if (hitboxCombo3 != null) hitboxCombo3.SetActive(false);
        if (hitboxAir != null) hitboxAir.SetActive(false);
    }

    public void AnimEvent_EndAttack()
    {
        isAttacking = false;
        AnimEvent_DisableHitbox(); 

        if (comboStep == 3)
        {
            nextComboEnableTime = Time.time + fullComboCooldown;
        }

        if (!isGrounded) rb.gravityScale = defaultGravity; 
    }

    public int GetCurrentComboStep() { return comboStep; }

    private void ThrowSword()
    {
        if (thrownSwordPrefab != null && throwPoint != null)
        {
            Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            Vector2 throwDirection = (mousePos - throwPoint.position).normalized;

            GameObject sword = Instantiate(thrownSwordPrefab, throwPoint.position, Quaternion.identity);
            ThrownSword ts = sword.GetComponent<ThrownSword>();
            if (ts != null) ts.Initialize(throwDirection); 
        }
        
        isBlocking = false;
        if (animator != null) animator.SetBool("isBlocking", false);
        CharacterSwitcher.Instance.SwitchToUnarmed(); 
    }

    private void RecallSword()
    {
        ThrownSword stuckSword = Object.FindFirstObjectByType<ThrownSword>();
        if (stuckSword != null)
        {
            Destroy(stuckSword.gameObject); 
        }
        // 🟢 เอาออกมาระดับนี้เลย เพื่อการันตีว่ากดเรียกแล้วดาบต้องกลับมาที่มือ!
        CharacterSwitcher.Instance.SwitchToArmed(); 
    }

    private Transform FindNearestEnemy()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, dashRange, enemyLayer);
        Transform closest = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider2D enemy in enemies)
        {
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < minDistance)
            {
                Vector2 dir = (enemy.transform.position - transform.position).normalized;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, dist, obstacleLayer);
                
                if (hit.collider == null) 
                {
                    closest = enemy.transform;
                    minDistance = dist;
                }
            }
        }
        return closest;
    }

    private IEnumerator TargetDashAttack(Transform target)
    {
        isTargetDashing = true;
        if (animator != null) animator.SetBool("isDashing", true);

        Vector2 startPos = transform.position;
        Vector2 dirToTarget = (target.position - transform.position).normalized;
        Vector2 targetPos = (Vector2)target.position - (dirToTarget * 1.0f); 

        float dist = Vector2.Distance(startPos, targetPos);
        float duration = dist / dashSpeed;
        float time = 0;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0; 
        rb.linearVelocity = Vector2.zero;

        while (time < duration)
        {
            if (target == null) break; 
            Vector2 newPos = Vector2.Lerp(startPos, targetPos, time / duration);
            rb.MovePosition(newPos);
            time += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (target != null)
        {
            EnemyBehavior enemyScript = target.GetComponent<EnemyBehavior>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(attackDamage);
                canDoubleJump = true; 
                TriggerHitStop(0.15f); 
            }
        }
        
        if (CameraShake.Instance != null) StartCoroutine(CameraShake.Instance.Shake(0.25f, 0.2f)); 

        rb.gravityScale = originalGravity; 
        isTargetDashing = false;
        if (animator != null) animator.SetBool("isDashing", false);
    }

    private void HandleBlockingState()
    {
        isBlocking = Input.GetKey(blockKey) && isGrounded; 
        
        if (animator != null) animator.SetBool("isBlocking", isBlocking);

        if (isBlocking)
        {
            moveSpeed = 0f;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); 

            Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            float facingDir = Mathf.Sign(mousePos.x - transform.position.x);
            Vector3 currentScale = transform.localScale;
            currentScale.x = facingDir * Mathf.Abs(currentScale.x);
            transform.localScale = currentScale;

            currentGuardGauge -= guardDepleteRateBlocking * Time.deltaTime;
            TriggerJuice(JuiceType.Block);
        }
        else if (!isStunned)
        {
            currentGuardGauge += guardRegenRate * Time.deltaTime;
            currentGuardGauge = Mathf.Clamp(currentGuardGauge, 0, maxGuardGauge);
            if (spriteRenderer != null && spriteRenderer.color == blockColor) spriteRenderer.color = originalColor;
        }
    }
    
    private void HandleStunTimer()
    {
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            TriggerJuice(JuiceType.Stun);
            if (stunTimer <= 0) EndStun();
        }
    }

    private void EndStun()
    {
        isStunned = false;
        if (spriteRenderer != null) spriteRenderer.color = originalColor; 
        currentGuardGauge = maxGuardGauge; 
    }

    private enum JuiceType { Hurt, Block, ArmorBreak, Stun }
    private void TriggerJuice(JuiceType type)
    {
        if (spriteRenderer == null) return; 

        switch (type)
        {
            case JuiceType.Hurt: StartCoroutine(FlashColorRoutine(Color.red, originalColor)); break;
            case JuiceType.Block: spriteRenderer.color = blockColor; break;
            case JuiceType.ArmorBreak:
                StartCoroutine(FlashColorRoutine(armorBreakColor, originalColor));
                TriggerHitStop(0.15f); 
                if (CameraShake.Instance != null) StartCoroutine(CameraShake.Instance.Shake(0.3f, 0.25f)); 
                break;
            case JuiceType.Stun:
                float t = Mathf.PingPong(Time.time * 8f, 1f);
                spriteRenderer.color = Color.Lerp(originalColor, stunColor, t);
                break;
        }
    }

    private IEnumerator FlashColorRoutine(Color flashColor, Color defaultColor)
    {
        if (spriteRenderer != null) spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(0.1f);
        if (spriteRenderer != null) spriteRenderer.color = defaultColor;
    }

    public void TriggerHitStop(float duration)
    {
        if (currentHitStop != null) StopCoroutine(currentHitStop);
        currentHitStop = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        currentHitStop = null;
    }

    private void HandleInteractInput()
    {
        if (Input.GetKeyDown(interactKey)) Debug.Log("Interact with E!");
    }

    public void TakeDamage(int damage)
    {
        isNormalDashing = false;
        isTargetDashing = false;
        if (rb != null) rb.gravityScale = defaultGravity; 

        TriggerHitStop(0.1f);
        if (CameraShake.Instance != null) StartCoroutine(CameraShake.Instance.Shake(0.3f, 0.2f));

        if (isBlocking)
        {
            if (spriteRenderer != null) StartCoroutine(FlashColorRoutine(blockColor, originalColor));
            if (CameraShake.Instance != null) StartCoroutine(CameraShake.Instance.Shake(0.15f, 0.08f)); 
            TriggerHitStop(0.04f); 

            currentGuardGauge -= guardDepleteOnHit;
            if (currentGuardGauge <= 0) TriggerArmorBreak();
            else damage = Mathf.RoundToInt(damage * blockDamageReduction);
        }
        else TriggerJuice(JuiceType.Hurt);

        currentHealth -= damage;
        if (UIManager.Instance != null) UIManager.Instance.UpdateHealth(currentHealth); 
        if (currentHealth <= 0) Die();
    }

    public void Heal(int healAmount)
    {
        if (currentHealth >= maxHealth) return;
        currentHealth += healAmount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        if (UIManager.Instance != null) UIManager.Instance.UpdateHealth(currentHealth);
        if (spriteRenderer != null) StartCoroutine(FlashColorRoutine(Color.green, originalColor));
    }
    
    private void TriggerArmorBreak()
    {
        isStunned = true;
        isBlocking = false; 
        if (animator != null) animator.SetBool("isBlocking", false);
        currentGuardGauge = 0; 
        stunTimer = stunDuration;
        TriggerJuice(JuiceType.ArmorBreak);
        
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    void Die() { Debug.Log("Game Over"); }
    
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
    
    private void UpdateAnimations()
    {
        if (animator == null) return;
        animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("VelocityY", rb.linearVelocity.y);
    }
    
    private void OnDestroy()
    {
        // 🟢 คืนค่าเวลาเสมอเมื่อ Player ถูกทำลาย (กันเกมค้าง)
        Time.timeScale = 1f;
    }
}