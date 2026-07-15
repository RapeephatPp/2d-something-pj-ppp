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
    public static bool isArmed = true; 
    private static ThrownSword activeSword;
    public static bool hasThrownSword = false;
    
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
    
    [Header("Dash & I-Frames")]
    public float dashDuration = 0.2f; 
    private bool isDashing;
    private bool canDash = true; 
    private float dashCooldownTimer;
    
    // 🟢 ตัวแปรพระเอก: โล่อมตะ
    public bool isInvincible = false;
    public bool isDead = false;
    public bool isRidingElevator = false;
    public bool isKnockedBack = false; 
    private float knockbackTimer = 0f;

    public float dashRange = 7f; 
    public float dashSpeed = 15f;
    public float targetDashSpeed = 10f;
    public float dashCooldown = 1.5f;
    private float nextDashTime = 0f;
    private bool isTargetDashing = false;
    
    [Header("Dash Trail Settings")]
    public int trailGhosts = 8;               
    public float ghostFadeDuration = 0.5f;    
    public Color ghostColor = new Color(0.1f, 0.1f, 0.1f, 0.8f); 

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
    
    private GameObject currentOneWayPlatform;
    private bool isDropping = false; 

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
    
    [Header("Health Settings")]
    public int maxHealth = 20;
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
    public float throwCooldown = 1.0f;    
    private static float nextThrowTime = 0f;     
    
    [Header("Death Effects (ระเบิดเลือด)")]
    public GameObject bloodPrefab;      
    public int minBloodSpawns = 10;     
    public int maxBloodSpawns = 20;     
    public float bloodSpread = 1.5f;    
    public GameObject droppedSwordPrefab; 
    
    [Header("Audio SFX")]
    public AudioClip jumpSound;
    public AudioClip attack1Sound;
    public AudioClip attack2Sound;
    public AudioClip attack3Sound;
    public AudioClip dashSound;
    public AudioClip hurtSound;
    public AudioClip deathSound;   
    public AudioClip blockSound;   
    public AudioClip armorBreakSound; 
    
    [Header("Audio SFX (Extra)")]
    public AudioClip doubleJumpSound; 
    public AudioClip throwSwordSound; 
    public AudioClip catchSwordSound; 
    public AudioClip targetDashSound; 
    public AudioClip landSound;       
    public AudioClip footstepSound;   
    
    private bool wasGrounded = true;
    
    private Camera mainCam; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravity = rb.gravityScale;
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 🟢 [จุดแก้บั๊กล่องหน!] ย้าย originalColor มาไว้ใน Awake เพื่อให้มันจำสีได้ทันทีก่อนจะโดน CharacterSwitcher ปิด!
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        
        if (animator == null) animator = GetComponent<Animator>();
        mainCam = Camera.main;
    }

    private void Start()
    {   
        isDead = false;
        if (isRidingElevator) return;
        hasThrownSword = false;
        
        if (gameObject.name.Contains("Maris") && !gameObject.name.Contains("Sword")) 
        {
            isArmed = false;
        }
        
        // 🟢 เปิดการมองเห็นกลับมาเสมอตอนเริ่มเกม/เกิดใหม่
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        
        currentHealth = maxHealth;
        currentGuardGauge = maxGuardGauge;
        
        // (ลบ originalColor ออกจากตรงนี้แล้ว)
        
        if (UIManager.Instance != null) UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
        
        AnimEvent_DisableHitbox();
    }

    private void Update()
    {   
        if (Time.timeScale == 0f || PauseMenuController.isPaused) return;
        
        if (isDead) return;
        if (isDashing) return;
        if (isRidingElevator) return;
        
        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0) isKnockedBack = false;
        }
        
        // 🟢 ถ้าโดนผลักอยู่ ให้เมินคำสั่งเดินทั้งหมด
        if (isTargetDashing || isStunned || isKnockedBack) 
        {
            CheckGrounded(); 
            if (isStunned) HandleStunTimer(); 
            return; 
        }
        
        if (Input.GetKeyDown(jumpKey)) 
        {
            jumpBufferCounter = jumpBufferTime; // เริ่มนับถอยหลัง
        } 
        else 
        {
            jumpBufferCounter -= Time.deltaTime; // ลดเวลาไปเรื่อยๆ
        }
        if (isGrounded) 
        {
            coyoteTimeCounter = coyoteTime;
            canDash = true; // 🟢 สำคัญมาก! เท้าแตะพื้นปุ๊บ รีเซ็ตโควต้าพุ่งกลางอากาศ (Air Dash)
        } 
        else 
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        
        
        if (isTargetDashing || isStunned) 
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
            AimThrowPointAtMouse();
            HandleCombatInput();
            HandleBlockingState(); 
        }
        else 
        {
            if (Input.GetKeyDown(recallKey)) RecallSword();
        }
        
        HandleDashInput();
        HandleInteractInput();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {   
        if (isDead) return;
        if (isRidingElevator) return;
        
        if (isTargetDashing || isStunned || isKnockedBack) 
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            return;
        }

        ApplyMovementPhysics();
        ApplyBetterGravity();

        // 🟢 ระบบดึงเบรกมือ: กันไถลบนทางลาดชัน
        // ถ้ายืนอยู่บนพื้น + ไม่ได้กดปุ่มเดิน + ไม่ได้กำลังฟันดาบ
        if (isGrounded && moveInputX == 0 && !isAttacking && !isDropping)
        {
            // ล็อคแกน X ไว้เลย (แต่ยังร่วงแกน Y ได้ปกติถ้าพื้นหาย)
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        }
        else
        {
            // ถ้าขยับตัว หรือกระโดด ก็ปลดล็อคแกน X ให้เดินได้ลื่นๆ
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
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
        // 🟢 ดึงข้อมูลของพื้นที่เราเหยียบอยู่มาเช็ค
        Collider2D hitCollider = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        bool isGroundedNow = hitCollider != null;
        
        if (isGroundedNow && !wasGrounded && rb.linearVelocity.y <= 0f)
        {
            AudioManager.Instance.PlaySFX(landSound, 0.6f); // ดังปานกลาง
        }
        
        isGrounded = isGroundedNow;
        wasGrounded = isGrounded; // อัปเดตสถานะไว้ใช้เฟรมถัดไป
        
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            canDoubleJump = true;
            canDash = true;

            // 🟢 เช็คว่าพื้นที่เหยียบอยู่ มี PlatformEffector2D ไหม (ถ้ามีแปลว่าเป็น One-Way)
            if (hitCollider.GetComponent<PlatformEffector2D>() != null)
            {
                currentOneWayPlatform = hitCollider.gameObject;
            }
            else
            {
                currentOneWayPlatform = null;
            }
        }
        else 
        {
            coyoteTimeCounter -= Time.deltaTime;
            currentOneWayPlatform = null;
        }
    }

    private void HandleJump()
    {   
        // ระบบ Drop-through ยังคงอยู่ตามเดิม
        if (isGrounded && Input.GetKey(crouchKey) && Input.GetKeyDown(jumpKey))
        {
            if (currentOneWayPlatform != null)
            {
                PlatformEffector2D effector = currentOneWayPlatform.GetComponent<PlatformEffector2D>();
                if (effector != null)
                {
                    jumpBufferCounter = 0f;
                    StartCoroutine(FallThroughRoutine(effector));
                    return;
                }
            }
        }
        
        if (isBlocking) return;
        
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            AudioManager.Instance.PlaySFX(jumpSound, 0.5f);
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            canDoubleJump = true;
            if (animator != null) animator.SetTrigger("Jump");
        }
        else if (Input.GetKeyDown(jumpKey) && canDoubleJump && !isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            
            AudioManager.Instance.PlaySFX(doubleJumpSound, 0.5f);
            
            canDoubleJump = false;
            jumpBufferCounter = 0f;
            if (animator != null) animator.SetTrigger("Jump");
        }

        if (Input.GetKeyUp(jumpKey) && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }
    
    // 🟢 [อุดบั๊ก] เปลี่ยนจากพลิกพื้น เป็นสั่งเมินกล่องชนชั่วคราวแทน ศัตรูจะได้ไม่ร่วงตาม
    // 🟢 [อุดบั๊กร่วงทะลุพื้น] แก้จังหวะการทำ Ignore Collision ใหม่ให้รัดกุมขึ้น
    // 🟢 [อุดบั๊กร่วงทะลุพื้น] ดึง Collider "ทุกชิ้น" ของพื้นแผ่นนั้นมาปิด!
    private IEnumerator FallThroughRoutine(PlatformEffector2D effector)
    {
        isDropping = true; // ปลดเบรกมือทันที
        
        // 1. ดึงกล่องชน "ทั้งหมด" ของพื้นแผ่นนั้น (แก้ปัญหา Tilemap ที่มี Composite Collider ซ้อนกัน)
        Collider2D[] platformCols = effector.GetComponents<Collider2D>();
        Collider2D[] playerCols = GetComponentsInChildren<Collider2D>();
        
        // 2. สั่งปิดการชนเฉพาะตัวเรา กับกล่องทุกใบของพื้นแผ่นนั้น
        foreach (Collider2D platCol in platformCols)
        {
            foreach (Collider2D pCol in playerCols) 
            {
                if (pCol != null && platCol != null) Physics2D.IgnoreCollision(pCol, platCol, true);
            }
        }
        
        // 3. ออกแรงกระชากตัวละครลง เพื่อให้หลุดจากระยะพื้นทันที
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -2f);
        
        // 4. กลับมาใช้ WaitForSeconds (0.35 วินาทีกำลังสมูท) เพื่อแก้บั๊กคำนวณตำแหน่ง Y ของ Tilemap
        yield return new WaitForSeconds(0.35f);
        
        // 5. เปิดการชนกลับมาเหมือนเดิม
        foreach (Collider2D platCol in platformCols)
        {
            foreach (Collider2D pCol in playerCols) 
            {
                if (pCol != null && platCol != null) Physics2D.IgnoreCollision(pCol, platCol, false);
            }
        }
        
        isDropping = false; 
    }

    private void ApplyBetterGravity()
    {
        if (rb.linearVelocity.y < 0) rb.gravityScale = defaultGravity * fallGravityMultiplier;
        else rb.gravityScale = defaultGravity;
    }
    
    private void HandleDashInput()
    {
        dashCooldownTimer -= Time.deltaTime;

        // ถ้ากด Shift + พุ่งได้ + คูลดาวน์เสร็จแล้ว
        if (Input.GetKeyDown(normalDashKey) && canDash && dashCooldownTimer <= 0)
        {
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {   
        AudioManager.Instance.PlaySFX(dashSound);
        
        if (CameraJuiceFX.Instance != null) CameraJuiceFX.Instance.TriggerDashJuice();
        
        isDashing = true;
        canDash = false; 
        isInvincible = true; 
        
        SetEnemyCollision(true);

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        float dashDirection = Mathf.Sign(transform.localScale.x); 
        float inputX = Input.GetAxisRaw("Horizontal");
        if (inputX != 0) dashDirection = Mathf.Sign(inputX);

        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);

        // 🟢 เปลี่ยนวิธีนับเวลา เพื่อให้สั่งเสกเงาได้ระหว่างพุ่ง!
        float ghostSpawnInterval = dashDuration / trailGhosts; 
        float ghostTimer = 0f;
        float dashTime = 0f;

        while (dashTime < dashDuration)
        {   
            if (!isDashing) break;
            
            dashTime += Time.deltaTime;
            ghostTimer -= Time.deltaTime;

            // ถ้าถึงจังหวะเวลา ให้เสกเงา 1 ตัว
            if (ghostTimer <= 0)
            {
                SpawnDashGhost();
                ghostTimer = ghostSpawnInterval; // รีเซ็ตเวลารอเสกตัวถัดไป
            }
            yield return null; // รอขยับเฟรมถัดไป
        }

        rb.gravityScale = originalGravity;
        isDashing = false;
        isInvincible = false; 
        dashCooldownTimer = dashCooldown;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, rb.linearVelocity.y);
        
        SetEnemyCollision(false);
    }
    
    // 🟢 ฟังก์ชันสร้างเงาทิ้งไว้ ณ ตำแหน่งปัจจุบัน
    private void SpawnDashGhost()
    {
        if (spriteRenderer == null) return;

        // 1. สร้าง Object เปล่าๆ ขึ้นมากลางอากาศ
        GameObject ghostObj = new GameObject("DashGhost");
        ghostObj.transform.position = transform.position;
        ghostObj.transform.localScale = transform.localScale;
        ghostObj.transform.rotation = transform.rotation;

        // 2. ก๊อปปี้ภาพ Sprite และตั้งค่าสีดำทมิฬ
        SpriteRenderer ghostSprite = ghostObj.AddComponent<SpriteRenderer>();
        ghostSprite.sprite = spriteRenderer.sprite; // ถ่ายรูปท่าทางปัจจุบันเป๊ะๆ
        ghostSprite.color = ghostColor;
        ghostSprite.sortingLayerID = spriteRenderer.sortingLayerID;
        ghostSprite.sortingOrder = spriteRenderer.sortingOrder - 1; // ให้อยู่หลังตัวผู้เล่น

        // 3. สั่งทำลายตัวเองล่วงหน้า (กันเหนียว เผื่อเกมบั๊กจะได้ไม่รกฉาก)
        Destroy(ghostObj, ghostFadeDuration + 0.1f);

        // 4. สั่งให้เงาคอยๆ จางหายไปอย่างนุ่มนวล
        StartCoroutine(FadeGhostRoutine(ghostSprite, ghostFadeDuration));
    }

    // 🟢 ฟังก์ชันทำให้เงาค่อยๆ จางหายไป (Fade Out)
    private IEnumerator FadeGhostRoutine(SpriteRenderer ghost, float fadeTime)
    {
        float elapsed = 0f;
        Color startColor = ghost.color;
        
        Vector3 startScale = ghost.transform.localScale;
        Vector3 targetScale = startScale * 1.2f; // ให้เงาขยายขึ้น 20% ตอนหายไป

        while (elapsed < fadeTime)
        {
            if (ghost == null) yield break; 
            
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / fadeTime);
            ghost.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            
            // 🟢 เพิ่มบรรทัดนี้เข้าไป! ให้เงามันค่อยๆ ขยายร่างออกตอนที่กำลังจางหาย
            ghost.transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / fadeTime);
            
            yield return null;
        }
    }
    
    // 🟢 ฟังก์ชันสั่งให้ตัวผู้เล่นทะลุศัตรูได้แบบผี!
    private void SetEnemyCollision(bool ignore)
    {
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol == null) return;

        // ค้นหาศัตรูทั้งหมดในฉาก (ที่ติด Tag ว่า "Enemy")
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Collider2D[] enemyCols = enemy.GetComponents<Collider2D>();
            foreach (Collider2D eCol in enemyCols)
            {
                // สั่งให้กล่องของเรา เมินกล่องของศัตรู
                Physics2D.IgnoreCollision(myCol, eCol, ignore);
            }
        }
    }
    
    public void ChangeAnimator(Animator newAnimator) { animator = newAnimator; }

    private void HandleCombatInput()
    {
        if (Input.GetKey(blockKey) && Input.GetKeyDown(attackKey))
        {
            if (isArmed && Time.time >= nextThrowTime) ThrowSword(); 
            return;
        }
        
        if (Input.GetKeyDown(recallKey))
        {
            if (!isArmed) RecallSword();
        }

        if (isBlocking) return;

        // 🟢 1. เปลี่ยนจาก GetKey เป็น GetKeyDown เพื่อบังคับให้ผู้เล่นต้อง "คลิก" เป็นจังหวะ
        if (Input.GetKey(attackKey))
        {
            if (isAttacking || Time.time < nextComboEnableTime) return;

            if (Time.time - lastAttackTime > comboResetTime) comboStep = 0;

            isAttacking = true; 
            lastAttackTime = Time.time;
            
            if (animator != null) animator.SetBool("isAttacking", true); // กางโล่!

            if (!isGrounded)
            {
                comboStep = 2; 
                rb.linearVelocity = Vector2.zero; 
                rb.gravityScale = 0f; 
                
                AudioManager.Instance.PlaySFX(attack2Sound, 0.9f);
            }
            else
            {
                comboStep++;
                if (comboStep > 3) comboStep = 1;
                
                // 🟢 สร้างแรงกระเถิบไปข้างหน้า (Micro-Lunge) ตอนฟันดาบ
                float facingDir = Mathf.Sign(transform.localScale.x);
                rb.linearVelocity = new Vector2(facingDir * 4f, rb.linearVelocity.y); // เปลี่ยนจากเบรค เป็นพุ่งไปข้างหน้าเบาๆ!
                
                if (comboStep == 1) 
                {
                    AudioManager.Instance.PlaySFX(attack1Sound, 0.8f); // ฟันเร็ว เสียงเบาหน่อย
                }
                else if (comboStep == 2) 
                {
                    AudioManager.Instance.PlaySFX(attack2Sound, 0.9f); // หนักขึ้นมานิดนึง
                }
                else if (comboStep == 3) 
                {
                    AudioManager.Instance.PlaySFX(attack3Sound, 1.2f); // ท่าจบ เอาให้ดังสะใจ!
                }
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
        // ใช้ Try-Catch ดักจับบั๊ก ถ้ามีอะไรพังจะได้ไม่กระทบแอนิเมชันส่วนอื่น
        try 
        {
            if (comboStep == 1 && hitboxCombo1 != null) hitboxCombo1.SetActive(true);
            else if (comboStep == 2 && isGrounded && hitboxCombo2 != null) hitboxCombo2.SetActive(true);
            else if (comboStep == 3 && hitboxCombo3 != null) hitboxCombo3.SetActive(true);
            else if (!isGrounded && hitboxAir != null) hitboxAir.SetActive(true);

            // 🟢 ปรับวิธีเรียก Camera Shake ให้ปลอดภัยขึ้น
            if (CameraShake.Instance != null && CameraShake.Instance.gameObject.activeInHierarchy) 
            {
                CameraShake.Instance.StartManagedShake(0.1f, 0.05f);
            }
        }
        catch (System.Exception e) 
        {
            Debug.LogError("เจอตัวการพังใน EnableHitbox: " + e.Message);
        }
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
        if (animator != null) animator.SetBool("isAttacking", false); // เก็บโล่!
        AnimEvent_DisableHitbox(); 

        if (comboStep == 3)
        {
            nextComboEnableTime = Time.time + fullComboCooldown;
        }

        // 🟢 เพิ่มโค้ดส่วนนี้: สั่งให้รีเซ็ตค่าคอมโบและคืนแรงโน้มถ่วงทันทีที่ตีกลางอากาศจบ
        if (rb != null && !isGrounded) 
        {
            rb.gravityScale = defaultGravity; // คืนค่าแรงโน้มถ่วงให้ร่วงลงพื้น
            
            comboStep = 0; // ล้างค่าคอมโบเลย จะได้ไม่ไปบล็อกท่าเดิน/กระโดด
            if (animator != null) animator.SetInteger("comboStep", 0);
        }
    }
    
    public void AnimEvent_PlayFootstep()
    {
        if (isGrounded) // ต้องอยู่บนพื้นเท่านั้นถึงจะมีเสียง
        {
            // ปรับเสียงเดินให้เบาหน่อย จะได้ไม่กวนเสียงสู้
            AudioManager.Instance.PlaySFX(footstepSound, 0.35f); 
        }
    }
    
    public int GetCurrentComboStep() { return comboStep; }

    private void ThrowSword()
    {   
        AudioManager.Instance.PlaySFX(throwSwordSound, 1.0f);
        
        isArmed = false;
        hasThrownSword = true; 
        CharacterSwitcher.Instance.SwitchToUnarmed();

        GameObject swordObj = Instantiate(thrownSwordPrefab, throwPoint.position, throwPoint.rotation);
        activeSword = swordObj.GetComponent<ThrownSword>(); 

        // 🟢 เปลี่ยนจากคำนวณตำแหน่งเมาส์ยาวๆ มาดึง "ทิศทางหน้าของ throwPoint" ได้เลย ง่ายและเป๊ะ!
        Vector2 throwDirection = throwPoint.right; 
        
        activeSword.Initialize(throwDirection);
    }

    private void RecallSword()
    {
        if (activeSword != null)
        {
            // ถ้ามีดาบอยู่ข้างนอก สั่งให้มันบินกลับมาที่ร่างปัจจุบัน (this.transform)
            activeSword.ReturnToPlayer(this.transform); 
        }
        else if (hasThrownSword)
        {
            // ถ้าดาบพังหรือหายไปแล้ว ค่อยเสกกลับเข้ามือ
            CatchSword();
        }
    }
    
    // 🟢 ฟังก์ชันนี้ดาบจะเป็นคนเรียกใช้ตอนที่มันบินมาถึงตัวเราแล้ว
    public void CatchSword()
    {   
        AudioManager.Instance.PlaySFX(catchSwordSound, 1.0f);
        
        isArmed = true; 
        hasThrownSword = false;
        
        // 🟢 ตั้งค่าเวลาที่จะปาดาบครั้งต่อไปได้ (ป้องกันการไปยืนชิดศัตรูแล้วกดเรียกรัวๆ)
        nextThrowTime = Time.time + throwCooldown;

        CharacterSwitcher.Instance.SwitchToArmed();
        if (CameraShake.Instance != null) 
        {
            CameraShake.Instance.StartManagedShake(0.15f, 0.1f);
        }
    }
    
    // 🟢 ระบบเล็ง: ทำให้ throwPoint โคจรรอบตัวและชี้ไปหาเมาส์
    private void AimThrowPointAtMouse()
    {
        if (throwPoint == null) return;

        // 1. หาตำแหน่งเมาส์ในโลกของเกม
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        // 2. คำนวณทิศทางจากตัวละครไปหาเมาส์ (สมมติให้จุดหมุนอยู่กลางอกตัวละคร)
        Vector3 aimDirection = (mousePos - transform.position).normalized;

        // 3. หันหน้า throwPoint ไปหาเมาส์
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        throwPoint.rotation = Quaternion.Euler(0, 0, angle);

        // 4. ขยับตำแหน่ง throwPoint ให้เป็นวงกลมรอบๆ ตัวละคร (ไม่ให้เสกดาบซ้อนทับตัว)
        float orbitRadius = 1.0f; // 👈 ปรับความกว้างของวงโคจรตรงนี้ (ถ้าดาบเสกแล้วติดกำแพงแปลกๆ ให้ลดลงเหลือ 0.8f)
        
        // เราสามารถบวกแกน Y นิดนึง เพื่อให้จุดปาอยู่ระดับ "ไหล่" หรือ "อก" แทนที่จะเป็นเท้า
        Vector3 pivotOffset = new Vector3(0, 0.1f, 0); 
        
        throwPoint.position = transform.position + pivotOffset + (aimDirection * orbitRadius);
    }

    private Transform FindNearestEnemy()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, dashRange, enemyLayer);
        Transform closest = null;
        float minDistance = Mathf.Infinity;

        Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y + 0.5f);
        
        // 🟢 1. หาว่าตอนนี้ตัวละครหันหน้าไปทางไหน (1 = ขวา, -1 = ซ้าย)
        float currentFacingDir = Mathf.Sign(transform.localScale.x);

        foreach (Collider2D enemy in enemies)
        {
            // 🟢 2. เช็คว่าศัตรูอยู่ฝั่งเดียวกับที่เราหันหน้าอยู่ไหม
            float dirToEnemyX = enemy.transform.position.x - transform.position.x;
            
            // ถ้าระยะห่างเกิน 0.1 (กันบั๊กยืนซ้อนทับกัน) และอยู่คนละฝั่งกับที่หันหน้า -> ข้ามตัวนี้ไปเลย!
            if (Mathf.Abs(dirToEnemyX) > 0.1f && Mathf.Sign(dirToEnemyX) != currentFacingDir) 
            {
                continue; 
            }

            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < minDistance)
            {
                Vector2 enemyTarget = new Vector2(enemy.transform.position.x, enemy.transform.position.y + 0.5f);
                Vector2 dir = (enemyTarget - rayOrigin).normalized;
                
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, dir, dist, obstacleLayer);
                
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
        AudioManager.Instance.PlaySFX(targetDashSound, 1.0f);
        
        isTargetDashing = true;
        if (animator != null) animator.SetBool("isDashing", true);

        Vector2 startPos = transform.position;
        Vector2 dirToTarget = (target.position - transform.position).normalized;
        
        float facingDir = Mathf.Sign(dirToTarget.x);
        if (facingDir == 0) facingDir = 1f;
        Vector3 currentScale = transform.localScale;
        currentScale.x = facingDir * Mathf.Abs(currentScale.x);
        transform.localScale = currentScale;

        float dist = Vector2.Distance(startPos, target.position);
        float actualDashDist = Mathf.Max(0.1f, dist - 1.0f); 
        Vector2 targetPos = startPos + (dirToTarget * actualDashDist);

        // องศาพุ่ง
        float dashAngle = Mathf.Atan2(dirToTarget.y, Mathf.Abs(dirToTarget.x)) * Mathf.Rad2Deg * facingDir;
        transform.rotation = Quaternion.Euler(0, 0, dashAngle);

        float duration = actualDashDist / targetDashSpeed; 
        float time = 0;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0; 
        rb.linearVelocity = Vector2.zero;

        float ghostSpawnInterval = duration / trailGhosts; 
        float ghostTimer = 0f;

        while (time < duration)
        {
            if (target == null) break; 
            
            // --------------------------------------------------------
            // 🟢 [อุดบั๊กพุ่งทะลุ] เช็คระยะห่างตลอดเวลา ถ้าประชิดตัวแล้วให้หยุดพุ่งทันที!
            // --------------------------------------------------------
            float currentDist = Vector2.Distance(transform.position, target.position);
            if (currentDist <= 1.2f) // ถ้าระยะห่างน้อยกว่าหรือเท่ากับ 1.2 หน่วย
            {
                break; // เบรกเอี๊ยด! หลุดออกจากลูปการพุ่งทันที
            }

            ghostTimer -= Time.fixedDeltaTime;
            if (ghostTimer <= 0)
            {
                SpawnDashGhost();
                ghostTimer = ghostSpawnInterval; 
            }

            Vector2 newPos = Vector2.Lerp(startPos, targetPos, time / duration);
            rb.MovePosition(newPos);
            time += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // พอพุ่งเสร็จปุ๊บ ค่อยทำดาเมจใส่ศัตรู
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
        
        if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.25f, 0.2f); 

        // จบการพุ่ง คืนองศาให้กลับมายืนตรงๆ 
        transform.rotation = Quaternion.identity;
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
                if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.3f, 0.25f); 
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
        currentHitStop = StartCoroutine(HitStopRoutine(duration, 0));
    }

    public System.Collections.IEnumerator HitStopRoutine(float duration, float scale)
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

    // 🟢 อัปเกรด: เพิ่มพารามิเตอร์ bypassInvincibility เพื่อให้เลเซอร์ระดับสุดยอด ทะลวงอมตะได้!
    public void TakeDamage(int damage, bool bypassInvincibility = false)
    {   
        if (isDead) return;
        if (isInvincible && !bypassInvincibility) return;
        
        // 🟢 ถ้าถูกโจมตีแบบทะลวงอมตะ ให้กระชากผู้เล่นออกจากการพุ่งทันที!
        if (bypassInvincibility)
        {
            isDashing = false;
            isInvincible = false;
        }
        
        isTargetDashing = false;
        if (rb != null) rb.gravityScale = defaultGravity; 

        TriggerHitStop(0.1f);
        if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.3f, 0.2f);

        if (isBlocking)
        {   
            AudioManager.Instance.PlaySFX(blockSound, 1.0f);
            
            if (spriteRenderer != null) StartCoroutine(FlashColorRoutine(blockColor, originalColor));
            if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.15f, 0.08f); 
            TriggerHitStop(0.04f); 

            currentGuardGauge -= guardDepleteOnHit;
            if (currentGuardGauge <= 0) TriggerArmorBreak();
            else damage = Mathf.RoundToInt(damage * blockDamageReduction);
        }
        else 
        {
            AudioManager.Instance.PlaySFX(hurtSound, 1.0f);
            TriggerJuice(JuiceType.Hurt);
        }

        currentHealth -= damage;
        if (UIManager.Instance != null) UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
        
        if (currentHealth <= 0) 
        {
            Die();
        }
        else if (!bypassInvincibility) 
        {
            // 🟢 [อุดบั๊ก!] ถ้ายังไม่ตาย ให้ติดสถานะอมตะชั่วคราว 1.5 วินาที
            StartCoroutine(InvincibilityRoutine(1.5f));
        }
    }

    public void Heal(int healAmount)
    {
        if (currentHealth >= maxHealth) return;
        currentHealth += healAmount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        if (UIManager.Instance != null) UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
        if (spriteRenderer != null) StartCoroutine(FlashColorRoutine(Color.green, originalColor));
    }
    
    private void TriggerArmorBreak()
    {
        isStunned = true;
        isBlocking = false; 
        
        AudioManager.Instance.PlaySFX(armorBreakSound, 1.2f);
        
        if (animator != null) animator.SetBool("isBlocking", false);
        currentGuardGauge = 0; 
        stunTimer = stunDuration;
        TriggerJuice(JuiceType.ArmorBreak);
        
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }
    
    void Die() 
    {   
        if (isDead) return;
        if (isInvincible) return;
        
        
        isDead = true;
        isInvincible = true;
        
        AudioManager.Instance.PlaySFX(deathSound, 1.5f);
        
        if (rb != null) rb.linearVelocity = Vector2.zero;
        isDashing = false;
        isTargetDashing = false;
        isAttacking = false;

        // 1. หยุดเวลาและสั่นกล้อง
        TriggerHitStop(0.2f);
        if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.5f, 0.4f);

        // 🟢 2. เพิ่มใหม่: ถ้าถือดาบอยู่ (Armed) ให้เสกดาบดรอปออกมา
        if (isArmed && droppedSwordPrefab != null)
        {
            GameObject droppedSword = Instantiate(droppedSwordPrefab, transform.position, transform.rotation);
            Rigidbody2D swordRb = droppedSword.GetComponent<Rigidbody2D>();
            if (swordRb != null)
            {
                // สุ่มทิศทางและแรงกระเด็น
                Vector2 popDir = new Vector2(Random.Range(-4f, 4f), Random.Range(5f, 8f));
                swordRb.AddForce(popDir, ForceMode2D.Impulse);
                swordRb.AddTorque(Random.Range(-600f, 600f)); 
            }
        }

        // 3. ปิดรูปตัวละครและกล่องชน
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol != null) myCol.enabled = false;

        // 4. เสกเลือดระเบิดตามปกติ
        if (bloodPrefab != null)
        {
            int bloodAmount = Random.Range(minBloodSpawns, maxBloodSpawns + 1);
            for (int i = 0; i < bloodAmount; i++)
            {
                Vector2 randomOffset = new Vector2(Random.Range(-bloodSpread, bloodSpread), Random.Range(-bloodSpread, bloodSpread));
                Instantiate(bloodPrefab, transform.position + (Vector3)randomOffset, Quaternion.identity);
            }
        }

        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        // 1. รอให้ผู้เล่นซึมซับความตาย (ดูเลือดกระเด็น) สัก 1.5 วินาที
        yield return new WaitForSeconds(1.5f);
        
        if (this == null || gameObject == null) yield break;
        
        // 2. เรียกหน้าต่าง UI Game Over ขึ้นมา
        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameOver();
    
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol != null) myCol.enabled = true;
    }
    
    // 🟢 ระบบผลักกระเด็นที่สมบูรณ์แบบ (หยุดการบังคับชั่วคราวให้ตัวปลิวได้)
    public void ApplyKnockback(Vector2 force, float duration)
    {
        if (isDead) return;

        isKnockedBack = true;       
        knockbackTimer = duration;  
        
        isBlocking = false;
        isDashing = false;
        isTargetDashing = false;
        if (animator != null) animator.SetBool("isBlocking", false);

        if (rb != null)
        {
            // 🟢 [อุดบั๊กปลิวทะลุโลก] ล้างความเร็วเดิมก่อน เพื่อไม่ให้แรงกระแทกมันบวกทบกัน!
            rb.linearVelocity = Vector2.zero; 
            
            // 🟢 ต้องปลดเบรกมือที่ล็อคแกน X ไว้ออกชั่วคราวด้วย ไม่งั้นแรงกระแทกแนวนอนจะไม่ทำงาน
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; 
            
            rb.AddForce(force, ForceMode2D.Impulse); 
        }
    }
    
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
    
    // 🟢 [เพิ่มใหม่] ทำงานอัตโนมัติเมื่อตัวละครถูกปิด (สลับร่าง)
    private void OnDisable()
    {
        // 1. ป้องกันบั๊ก HitStop ค้าง (เกม Freeze)
        isDead = false;
        Time.timeScale = 1f;
        if (currentHitStop != null) currentHitStop = null;

        // 2. ป้องกันตัวละครลอยค้างกลางอากาศ หรืออมตะค้าง
        isDashing = false;
        isTargetDashing = false;
        isInvincible = false;
        isAttacking = false;
        isDropping = false;
        
        if (rb != null)
        {
            rb.gravityScale = defaultGravity;
            // ปลดเบรกมือทุกชนิด ให้ขยับได้ปกติ
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; 
        }

        // 🌟 3. [เพิ่มใหม่] คืนค่าสีตัวละครกลับเป็นปกติ! ท่าไม้ตายแก้บั๊กตัวแดงค้าง
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            spriteRenderer.enabled = true;
        }
        
        // 4. ปิด Hitbox ที่อาจจะเปิดค้างอยู่
        AnimEvent_DisableHitbox();
    }
    
    // 🟢 ส่งค่า % คูลดาวน์ของดาบ (0.0 ถึง 1.0) ไปให้ UI
    public float GetSwordCooldownPercentage()
    {
        if (Time.time >= nextThrowTime) return 1f; // คูลดาวน์เสร็จแล้ว (เต็มหลอด)
        float timeLeft = nextThrowTime - Time.time;
        return 1f - (timeLeft / throwCooldown);    // คำนวณหลอดกำลังวิ่ง
    }
    
    // 🟢 ระบบปลด/ล็อค ฟิสิกส์ให้เกาะลิฟต์นิ่งๆ ไม่สั่น
    public void SetRidingElevator(bool isRiding)
    {
        isRidingElevator = isRiding;
        if (rb != null)
        {
            if (isRiding)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic; // เปลี่ยนเป็นโหมดไร้น้ำหนักชั่วคราว จะได้เกาะลิฟต์เนียนๆ
            }
            else
            {
                rb.bodyType = RigidbodyType2D.Dynamic; // กลับเป็นฟิสิกส์ปกติ
            }
        }
        
        // บังคับหยุดแอนิเมชันเดิน
        if (animator != null) animator.SetBool("isMoving", false);
    }
    
    private IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;

        // 🟢 1. เปลี่ยนให้ตัวละครโปร่งใส (ปรับค่า Alpha เป็น 0.4)
        if (spriteRenderer != null)
        {
            Color transparentColor = originalColor;
            transparentColor.a = 0.4f; // 0.0 คือมองไม่เห็นเลย, 1.0 คือทึบแสง
            spriteRenderer.color = transparentColor;
        }

        // 🟢 2. รอเวลาจนหมดสถานะอมตะ (I-Frame)
        yield return new WaitForSeconds(duration);

        // 🟢 3. คืนร่างให้ทึบแสงเหมือนเดิม
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        isInvincible = false;
    }

    //ฟังก์ชันสำหรับให้ร่างใหม่ ดึงสเตตัสจากร่างเก่าไปใช้
    public float GetDashCooldown() { return dashCooldownTimer; }
    public void SetDashCooldown(float timer) { dashCooldownTimer = timer; }
    
    private void OnDestroy()
    {
        // 🟢 คืนค่าเวลาเสมอเมื่อ Player ถูกทำลาย (กันเกมค้าง)
        Time.timeScale = 1f;
    }
}