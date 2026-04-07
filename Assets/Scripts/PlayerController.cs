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
    
    // 🟢 [เพิ่มใหม่] ปุ่มสำหรับดึงดาบกลับ
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
    
    private Coroutine currentAttackRoutine;
    public bool isAttacking = false;
    private Coroutine currentHitStop;

    [Header("Animation & Combat System")]
    public Animator animator; 
    private int comboStep = 0;          
    private float lastAttackTime = 0f;  
    public float comboResetTime = 1.5f; 
    public float attackInputBufferTime = 0.4f; 
    private float lastAttackInputTime = -10f;

    [Header("Greatsword Hitbox Settings")]
    public int maxHealth = 5;
    public int currentHealth;
    public int attackDamage = 3; 
    public float attackRange = 2.5f; 
    
    public Transform swordAnchor; 
    public LayerMask enemyLayer;
    public LayerMask obstacleLayer; 

    [Header("Sword Throw Mechanics")]
    public GameObject thrownSwordPrefab; 
    public Transform throwPoint;         

    private Camera mainCam; // เอาไว้เช็คตำแหน่งเมาส์

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
            // 🟢 [เพิ่มใหม่] ถ้าร่างมือเปล่า สามารถกด R เพื่อดึงดาบกลับได้
            if (Input.GetKeyDown(recallKey))
            {
                RecallSword();
            }
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

        // 🟢 ลดอาการเดินกระตุก เดินฟันด้วยความเร็ว 50% ของปกติ จะลื่นไหลขึ้น
        if (isAttacking) moveSpeed *= 0.5f; 

        // 🟢 จะหันหน้าตามคีย์บอร์ดก็ต่อเมื่อไม่ได้กันอยู่ (เพราะกันอยู่จะหันตามเมาส์)
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
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
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
        {
            StartCoroutine(NormalDashRoutine());
        }
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
    
    public void ChangeAnimator(Animator newAnimator)
    {
        animator = newAnimator;
    }

    private void HandleCombatInput()
    {
        // ระบบปาดาบ (กางโล่ + คลิกซ้าย)
        if (Input.GetKey(blockKey) && Input.GetKeyDown(attackKey))
        {
            ThrowSword();
            return;
        }

        if (isBlocking) return;

        if (Input.GetKeyDown(attackKey)) lastAttackInputTime = Time.time;

        if (Time.time - lastAttackTime > comboResetTime && comboStep != 0 && !isAttacking)
        {
            comboStep = 0;
            if (animator != null) animator.SetInteger("comboStep", 0);
        }

        if (isAttacking) return; 

        if (Time.time - lastAttackInputTime <= attackInputBufferTime)
        {
            lastAttackInputTime = -10f; 

            if (!isGrounded)
            {
                currentAttackRoutine = StartCoroutine(AirAttackRoutine());
                return;
            }

            comboStep++;
            if (comboStep > 3) comboStep = 1; 

            lastAttackTime = Time.time;
            currentAttackRoutine = StartCoroutine(GreatswordComboRoutine(comboStep));
        }
    }

    private void ThrowSword()
    {
        if (thrownSwordPrefab != null && throwPoint != null)
        {
            // 🟢 คำนวณทิศทางจากจุดปา ไปหาเมาส์
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

    // 🟢 [เพิ่มใหม่] ฟังก์ชันดึงดาบกลับด้วยปุ่ม R
    private void RecallSword()
    {
        // หาดาบที่อยู่ในฉาก
        ThrownSword stuckSword = FindObjectOfType<ThrownSword>();
        if (stuckSword != null)
        {
            Destroy(stuckSword.gameObject); // ทำลายดาบที่ปักกำแพงทิ้ง
            CharacterSwitcher.Instance.SwitchToArmed(); // สลับร่างเป็นถือดาบ
            Debug.Log("Sword Recalled!");
        }
    }

    private IEnumerator AirAttackRoutine()
    {
        isAttacking = true;
        
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0.5f; 
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, 0f));

        if (animator != null) 
        {
            animator.SetInteger("comboStep", 2); 
            animator.SetTrigger("attackTrig"); 
        }

        float hitTime = 0.3f;
        yield return new WaitForSeconds(hitTime);
        
        bool hitEnemy = false;
        if (swordAnchor != null)
        {
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(swordAnchor.position, attackRange, enemyLayer);
            foreach (Collider2D enemy in hitEnemies)
            {
                EnemyBehavior enemyScript = enemy.GetComponent<EnemyBehavior>();
                if (enemyScript != null) 
                {
                    enemyScript.TakeDamage(attackDamage);
                    hitEnemy = true;
                }
            }
        }

        // 🟢 [เพิ่มใหม่] Screen Shake และ HitStop ในท่า Air Slash
        if (hitEnemy)
        {
            TriggerHitStop(0.12f);
            if (CameraShake.Instance != null) StartCoroutine(CameraShake.Instance.Shake(0.2f, 0.15f));
        }

        yield return new WaitForSeconds(0.4f);

        rb.gravityScale = originalGravity;
        isAttacking = false;
        currentAttackRoutine = null;
    }

    private IEnumerator GreatswordComboRoutine(int currentComboStep)
    {
        isAttacking = true; 
        
        float animDuration = 1.2f;
        float hitTime = 0.5f;

        if (currentComboStep == 1) { animDuration = 1.2f; hitTime = 0.5f; }
        else if (currentComboStep == 2) { animDuration = 0.9f; hitTime = 0.4f; }
        else if (currentComboStep == 3) { animDuration = 0.6f; hitTime = 0.3f; }

        int currentDamage = attackDamage + (currentComboStep == 3 ? 2 : 0);
        float hitStopDuration = currentComboStep == 3 ? 0.25f : 0.12f;
        float currentCamShake = currentComboStep == 3 ? 0.5f : 0.3f;

        if (animator != null) 
        {
            animator.SetInteger("comboStep", currentComboStep);
            animator.SetTrigger("attackTrig");
        }

        yield return new WaitForSeconds(hitTime);

        if (CameraShake.Instance != null) StartCoroutine(CameraShake.Instance.Shake(0.1f, 0.05f)); 
        
        bool hitEnemyInSwing = false;
        float activeHitboxDuration = 0.15f;
        float timer = 0f;
        HashSet<Collider2D> damagedEnemies = new HashSet<Collider2D>();

        while (timer < activeHitboxDuration)
        {
            if (swordAnchor != null)
            {
                Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(swordAnchor.position, attackRange, enemyLayer);
                foreach (Collider2D enemy in hitEnemies)
                {
                    if (!damagedEnemies.Contains(enemy))
                    {
                        EnemyBehavior enemyScript = enemy.GetComponent<EnemyBehavior>();
                        if (enemyScript != null) 
                        {
                            enemyScript.TakeDamage(currentDamage); 
                            damagedEnemies.Add(enemy); 
                            hitEnemyInSwing = true;
                        }
                    }
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (hitEnemyInSwing || currentComboStep == 3)
        {
            TriggerHitStop(hitStopDuration); 
            if (CameraShake.Instance != null) StartCoroutine(CameraShake.Instance.Shake(0.3f, currentCamShake)); 
        }

        float recoveryDuration = animDuration - hitTime - activeHitboxDuration;
        if (recoveryDuration > 0)
        {
            // ดึงดาบกลับไวขึ้นนิดนึงให้สมูท
            if (hitEnemyInSwing) recoveryDuration *= 0.8f; 
            yield return new WaitForSeconds(recoveryDuration);
        }

        isAttacking = false;
        currentAttackRoutine = null;
    }

    private void HandleBlockingState()
    {
        isBlocking = Input.GetKey(blockKey) && isGrounded; 
        
        if (animator != null) animator.SetBool("isBlocking", isBlocking);

        if (isBlocking)
        {
            moveSpeed = 0f;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); 

            // 🟢 [เพิ่มใหม่] เวลาโล่กาง (เล็งปาดาบ) ให้หันหน้าไปหาเมาส์
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
        Debug.Log("Stun Over, Guard Restored!");
    }

    private enum JuiceType { Hurt, Block, ArmorBreak, Stun }
    private void TriggerJuice(JuiceType type)
    {
        if (spriteRenderer == null) return; 

        switch (type)
        {
            case JuiceType.Hurt:
                StartCoroutine(FlashColorRoutine(Color.red, originalColor));
                break;
            case JuiceType.Block:
                spriteRenderer.color = blockColor;
                break;
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

    private void TriggerHitStop(float duration)
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
        else
        {
            TriggerJuice(JuiceType.Hurt);
        }

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
        Debug.Log("ARMOR BROKEN! Stunned!");
    }

    void Die() { Debug.Log("Game Over"); }
    
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        if (swordAnchor != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(swordAnchor.position, attackRange); 
        }
    }
    
    private void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("VelocityY", rb.linearVelocity.y);
        animator.SetBool("isAttacking", isAttacking); 
    }
}