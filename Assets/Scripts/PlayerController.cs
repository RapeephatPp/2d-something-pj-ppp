using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // --- Re-bound inputs to common PC controls ---
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode interactKey = KeyCode.E; 
    public KeyCode normalDashKey = KeyCode.LeftShift; // Tap for normal dash, hold for run
    public KeyCode targetDashKey = KeyCode.F; 
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode attackKey = KeyCode.Mouse0; // Mouse 1 (Left Click)
    public KeyCode blockKey = KeyCode.Mouse1; // Mouse 2 (Right Click)

    [Header("Movement Settings")]
    public float walkSpeed = 6f;
    public float runSpeed = 10f; // New run mechanic
    public float crouchSpeed = 3f; // New crouch mechanic
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

    [Header("Normal Directional Dash (Tap Shift)")]
    public float normalDashSpeed = 22f;
    public float normalDashDuration = 0.2f;
    public float normalDashCooldown = 1.0f;
    private float nextNormalDashTime;
    private bool isNormalDashing;

    [Header("Target Dash Attack (F)")]
    public float dashRange = 7f; 
    public float dashSpeed = 28f;
    public float dashCooldown = 1.5f;
    private float nextDashTime = 0f;
    private bool isTargetDashing = false;

    [Header("Blocking & Armor Break Settings (Mouse 2)")]
    public float maxGuardGauge = 100f; // Stamina for blocking
    public float currentGuardGauge;
    [Range(0, 1)] public float blockDamageReduction = 0.5f; // Reduces 50% damage
    public float guardRegenRate = 15f; // Guard stamina gained per second
    public float guardDepleteRateBlocking = 8f; // Gauge spent per second holding block
    public float guardDepleteOnHit = 25f; // Gauge spent per received hit
    public float stunDuration = 2.0f; // Time player is stunned after Armor Break
    private bool isBlocking;
    private bool isStunned;
    private float stunTimer;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Visual Effects (Blocks/Armor Break)")]
    public Color blockColor = Color.blue;
    public Color armorBreakColor = Color.yellow;
    public Color stunColor = Color.cyan;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private Rigidbody2D rb;
    private float defaultGravity;
    private Coroutine currentHitStop;

    // --- NEW: GREATSWORD COMBAT SYSTEM ---
    [Header("Greatsword Attack Settings (Procedural Animation)")]
    public int maxHealth = 5;
    public int currentHealth;
    public int attackDamage = 3; 
    public float attackRange = 2.5f; 

    [Tooltip("Multiplier for attack animation speed. Slower = lower value.")]
    public float attackSpeedMultiplier = 0.7f; 

    public float baseAttackSwingDuration = 0.5f; 
    public float attackCooldownBuffer = 0.2f; 
    private float nextNormalAttackTime = 0f;  
    
    [Header("Combo Settings")]
    public int currentCombo = 0; 
    public float comboResetWindow = 1.5f; // 🟢 ปรับเป็น 1.5 วิ ให้ผู้เล่นมีเวลาพักหายใจ/ดูจังหวะก่อนกดฮิตต่อไป
    private float lastAttackTime = 0f;

    // 🟢 [เพิ่ม 2 ตัวแปรนี้] สำหรับระบบจำการกดปุ่มล่วงหน้า
    public float attackInputBufferTime = 0.4f; // จะจำปุ่มที่กดล่วงหน้าไว้ 0.4 วินาที
    private float lastAttackInputTime = -10f;

    public Transform swordAnchor; 
    public Transform swordVisual; 
    public LayerMask enemyLayer;
    public LayerMask obstacleLayer; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        HandleCombatInput();
        HandleNormalDashInput();
        HandleBlockingState(); 
        HandleInteractInput();
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
        bool isHoldingRun = Input.GetKey(normalDashKey);

        if (isHoldingCrouch)
        {
            moveSpeed = crouchSpeed;
        }
        else if (isHoldingRun)
        {
            moveSpeed = runSpeed;
        }
        else
        {
            moveSpeed = walkSpeed;
        }

        if (moveInputX != 0)
        {
            // 🟢 แก้ใหม่: ดึงสเกลเดิม (Abs) มาใช้ แล้วเปลี่ยนแค่การหันซ้าย-ขวา (Sign) 
            // ทำให้สเกลแกน Y และ Z ของเดิมไม่ถูกรบกวน
            float facingDirection = Mathf.Sign(moveInputX);
            transform.localScale = new Vector3(facingDirection * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
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

        if (Input.GetKeyDown(jumpKey))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

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
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = defaultGravity * fallGravityMultiplier;
        }
        else
        {
            rb.gravityScale = defaultGravity;
        }
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
        
        float dashDirection = transform.localScale.x;
        rb.linearVelocity = new Vector2(dashDirection * normalDashSpeed, 0f);

        yield return new WaitForSeconds(normalDashDuration);

        rb.gravityScale = originalGravity;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); 
        isNormalDashing = false;
    }

    private void HandleCombatInput()
    {
        if (isBlocking) return;

        // 1. รับค่าการกดปุ่มตลอดเวลา (แม้อยู่ระหว่างแอนิเมชันโจมตี)
        if (Input.GetKeyDown(attackKey))
        {
            lastAttackInputTime = Time.time;
        }

        // 2. เช็คการหลุดคอมโบ (ถอยกลับไปเริ่มใหม่ถ้าหยุดตีนานเกินไป)
        if (Time.time - lastAttackTime > comboResetWindow && currentCombo != 0)
        {
            currentCombo = 0;
        }

        float calculatedSwingDuration = baseAttackSwingDuration / attackSpeedMultiplier;

        // 3. ปล่อยคอมโบ: เช็คว่า "มีการกดปุ่มรอไว้ในช่วง Buffer ไหม" AND "ถึงเวลาที่พร้อมตีหรือยัง"
        if (Time.time - lastAttackInputTime <= attackInputBufferTime && Time.time >= nextNormalAttackTime)
        {
            // ล้างค่าปุ่มทิ้งทันทีที่เริ่มตี จะได้ไม่ตีเบิ้ลรัวๆ
            lastAttackInputTime = -10f; 

            nextNormalAttackTime = Time.time + calculatedSwingDuration + attackCooldownBuffer;
            lastAttackTime = Time.time;

            // สั่งเล่นคอมโบ
            StartCoroutine(GreatswordComboRoutine(calculatedSwingDuration, currentCombo));

            // บวกคอมโบ
            currentCombo++;
            if (currentCombo > 2) currentCombo = 0;
        }

        // (Dash ของเดิม ไม่ได้เปลี่ยน)
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

    private void HandleBlockingState()
    {
        isBlocking = Input.GetKey(blockKey) && isGrounded; 
        
        if (isBlocking)
        {
            moveSpeed = 0f;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); 

            currentGuardGauge -= guardDepleteRateBlocking * Time.deltaTime;

            TriggerJuice(JuiceType.Block);
        }
        else if (!isStunned)
        {
            currentGuardGauge += guardRegenRate * Time.deltaTime;
            currentGuardGauge = Mathf.Clamp(currentGuardGauge, 0, maxGuardGauge);
            
            if (spriteRenderer != null && spriteRenderer.color == blockColor) 
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    private void HandleStunTimer()
    {
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            TriggerJuice(JuiceType.Stun);

            if (stunTimer <= 0)
            {
                EndStun();
            }
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
        // ถ้ากำลังหน่วงเวลาอยู่ ให้ยกเลิกอันเก่าทิ้งก่อน (ป้องกันการทับซ้อน)
        if (currentHitStop != null) 
        {
            StopCoroutine(currentHitStop);
        }
        currentHitStop = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        currentHitStop = null; // คืนค่าให้ว่างเมื่อทำเสร็จ
    }

    private void HandleInteractInput()
    {
        if (Input.GetKeyDown(interactKey))
        {
            Debug.Log("Interact with E!");
        }
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

    private IEnumerator GreatswordComboRoutine(float totalDuration, int comboStep)
    {
        float originalWalkSpeed = walkSpeed;
        walkSpeed = walkSpeed * 0.2f; 
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y);

        float windUpPercent = 0.5f; 
        float strikePercent = 0.15f; 

        int currentDamage = attackDamage;
        float currentHitStop = 0.12f;
        float currentCamShake = 0.3f;

        // กำหนดตั้งค่าดาเมจตามจังหวะคอมโบ
        if (comboStep == 1) 
        {
            totalDuration *= 0.8f; // ฮิตที่ 2 ตีเร็วขึ้นนิดนึง
        }
        else if (comboStep == 2)
        {
            currentDamage = attackDamage + 2; 
            currentHitStop = 0.25f; 
            currentCamShake = 0.5f; 
            windUpPercent = 0.6f; 
        }

        // 1. จังหวะง้างดาบ (Wind Up)
        // 💡 อนาคต: คุณสามารถสั่ง animator.SetTrigger("Attack" + comboStep) ตรงนี้ได้เลย
        yield return new WaitForSeconds(totalDuration * windUpPercent);

        if (CameraShake.Instance != null) StartCoroutine(CameraShake.Instance.Shake(0.1f, 0.05f)); 
        
        bool hitEnemyInSwing = false;

        // 2. จังหวะฟาด (Strike) & ตรวจจับการชน
        if (swordAnchor != null)
        {
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(swordAnchor.position, attackRange, enemyLayer);
            foreach (Collider2D enemy in hitEnemies)
            {
                EnemyBehavior enemyScript = enemy.GetComponent<EnemyBehavior>();
                if (enemyScript != null) 
                {
                    enemyScript.TakeDamage(currentDamage); 
                    hitEnemyInSwing = true;
                }
            }
        }
        
        yield return new WaitForSeconds(totalDuration * strikePercent);

        // 3. จังหวะโดนศัตรู (HitStop) หรือฟาดพื้น (Combo 3 สั่นแม้ไม่โดนศัตรู)
        if (hitEnemyInSwing || comboStep == 2)
        {
            TriggerHitStop(currentHitStop); 
            if (CameraShake.Instance != null) StartCoroutine(CameraShake.Instance.Shake(0.3f, currentCamShake)); 
        }

        // 4. จังหวะดึงดาบกลับ (Recovery)
        yield return new WaitForSeconds(totalDuration * (1f - windUpPercent - strikePercent));

        walkSpeed = originalWalkSpeed;
    }

    private IEnumerator TargetDashAttack(Transform target)
    {
        isTargetDashing = true;
        Vector2 startPos = transform.position;
        Vector2 dirToTarget = (target.position - transform.position).normalized;
        Vector2 targetPos = (Vector2)target.position - (dirToTarget * 1.0f); // ปรับเลข 1.0f ได้ถ้ารู้สึกว่าหยุดไกลไป

        float dist = Vector2.Distance(startPos, targetPos);
        float duration = dist / dashSpeed;
        float time = 0;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0; 
        rb.linearVelocity = Vector2.zero; // เคลียร์แรงเก่าออกก่อนพุ่ง

        while (time < duration)
        {
            if (target == null) break; 

            // ใหม่: ใช้ rb.MovePosition แทน transform.position เพื่อให้มันเคารพระบบชน (Collision)
            Vector2 newPos = Vector2.Lerp(startPos, targetPos, time / duration);
            rb.MovePosition(newPos);
        
            // ใหม่: ใช้ fixedDeltaTime และรอ WaitForFixedUpdate() เพื่อให้ Sync กับระบบฟิสิกส์ของ Unity
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
                TriggerHitStop(0.15f); // ใช้ระบบ HitStop ตัวใหม่ที่เราเพิ่งแก้ไป!
            }
        }
    
        if (CameraShake.Instance != null) StartCoroutine(CameraShake.Instance.Shake(0.25f, 0.2f)); 

        rb.gravityScale = originalGravity; 
        isTargetDashing = false;
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
            
            if (currentGuardGauge <= 0)
            {
                TriggerArmorBreak();
            }
            else
            {
                damage = Mathf.RoundToInt(damage * blockDamageReduction);
            }
        }
        else
        {
            TriggerJuice(JuiceType.Hurt);
        }

        currentHealth -= damage;
        
        if (UIManager.Instance != null) UIManager.Instance.UpdateHealth(currentHealth);

        if (currentHealth <= 0) Die();
    }

    private void TriggerArmorBreak()
    {
        isStunned = true;
        isBlocking = false; 
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
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, dashRange); 
    }
}