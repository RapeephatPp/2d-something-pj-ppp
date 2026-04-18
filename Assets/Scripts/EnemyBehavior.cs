using UnityEngine;
using System.Collections;

public class EnemyBehavior : MonoBehaviour
{
    public enum EnemyType 
    { 
        Passive,            
        MeleeHostile,       
        RangedHostile,      // 🟢 ตัวนี้แหละที่เราจะอัปเกรด
        FlyingHostile,      
        StationaryTarget,   
        BigChaser           
    }
    
    public EnemyType type;

    [Header("Base Settings")]
    public int maxHealth = 3; 
    private int currentHealth;
    public int damage = 1; 
    public float speed = 2f;
    
    [Header("Detection & Movement")]
    public float detectionRange = 5f;   
    public float safeDistance = 3f;     
    public float retreatTime = 2f;      
    private bool isRetreating = false;

    // ==========================================
    // 🟢 [อัปเกรดใหม่] ระบบ Ranged แบบจัดเต็ม
    // ==========================================
    [Header("Ranged Hostile Settings")]
    [Tooltip("ติ๊กถูก = ยืนนิ่งอยู่กับที่ (สไนเปอร์), เอาออก = เดินไปมาและหนีเมื่อเข้าใกล้ (พลปืน)")]
    public bool isStationaryShooter = false; 
    
    [Tooltip("โอกาสที่จะไม่วิ่งหนี แต่ยืนยิงแลก (0.0 ถึง 1.0) เช่น 0.3 = โอกาส 30%")]
    public float standGroundChance = 0.3f;   
    
    [Tooltip("เวลาง้างปืน (Prepare To Shoot) ก่อนกระสุนออก")]
    public float aimTime = 0.5f;

    private bool isRangedAiming = false;
    private bool willStandGround = false;
    private float nextKiteDecisionTime = 0f;

    // ==========================================

    [Header("Passive NPC Settings")]
    public float fleeToSafeZoneChance = 0.5f; 
    public float wanderRadius = 3f;           
    public float wanderInterval = 2f;         
    
    private float spawnTime; 
    public float gracePeriod = 1.5f; 
    
    [Header("Passive AI Enhancements")]
    public GameObject alertIcon;        
    public LayerMask obstacleLayer;     
    public float wallCheckDist = 1f;    
    public float panicDuration = 5f;
    
    private Transform safeZone;               
    private Vector2 startPosition;
    private Vector2 wanderTarget;
    private float wanderTimer;
    private bool isFleeingToSafeZone = false;
    private bool hasRolledFleeChance = false; 
    
    private float panicTimer = 0f;           
    private float currentFleeDirection = 1f; 
    private bool isCornered = false;
    private float corneredTimer = 0f;
    private bool isChatting = false;
    private float chatTimer = 0f;
    private EnemyBehavior chatPartner = null;
    private bool isAlerted = false;

    [Header("Combat Settings")]
    public GameObject projectilePrefab; 
    public Transform firePoint;
    public float fireRate = 1.5f;
    private float nextFireTime;
    
    [Header("Melee Game Feel")]
    public float lungeRange = 2.5f;      
    public float telegraphTime = 0.5f;   
    public float lungeSpeedMultiplier = 3f; 
    public float meleeCooldown = 1.5f;   

    private bool isPreparingMelee = false;
    private bool isLunging = false;
    private float nextMeleeTime;
    
    [Header("Drone (Flying) Settings")]
    public float hoverHeight = 3f;          
    public float hoverOffset = 2.5f;        
    public float bobFrequency = 2f;         
    public float bobAmplitude = 0.4f;       
    public float flySmoothTime = 0.4f;      
    public float droneRecoilForce = 1.5f;   
    
    [Header("Drone Game Feel")]
    public float tiltAngle = 15f;           
    public float tiltSmoothTime = 0.15f;    
    public bool invertFacing = false;       
    
    private float currentTiltVelocity;      
    private Vector2 flyVelocity;            
    private bool isPreparingToShoot = false;

    [Header("References")]
    public Transform player;            
    public Animator animator; 
    
    [Header("Blood Splatter Settings")]
    public GameObject bloodPrefab;      
    public int minBloodSpawns = 3;      
    public int maxBloodSpawns = 6;      
    public float bloodSpread = 1.5f;    
    public float bloodHeightOffset = 1.0f;
    
    [Header("Chaser Settings")]
    public bool waitToChase = false;
    private bool isChasing = false;
    
    [Header("AI Spacing (Anti-Overlap)")]
    public float separationRadius = 0.8f; 
    public float separationForce = 1.5f;  

    void Start()
    {   
        spawnTime = Time.time;
        currentHealth = maxHealth;
        startPosition = transform.position;
        wanderTarget = transform.position;
        
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (type == EnemyType.BigChaser && !waitToChase)
        {
            isChasing = true;
        }

        // 🟢 อัปเกรดใหม่: สั่งให้กล่องชน "ทุกใบ" ของศัตรู เมินกล่องชน "ทุกใบ" ของผู้เล่น
        if (type == EnemyType.MeleeHostile || type == EnemyType.Passive || type == EnemyType.RangedHostile || type == EnemyType.StationaryTarget)
        {
            Collider2D[] myColliders = GetComponentsInChildren<Collider2D>(); // กวาดกล่องของตัวเอง
            if (player != null)
            {
                Collider2D[] playerColliders = player.GetComponentsInChildren<Collider2D>(); // กวาดกล่องของผู้เล่น
                
                foreach (Collider2D myCol in myColliders)
                {
                    foreach (Collider2D pCol in playerColliders)
                    {
                        Physics2D.IgnoreCollision(myCol, pCol, true); // สั่งเมินกันและกัน 100%
                    }
                }
            }
        }
    }

    void Update()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            GameObject activePlayer = GameObject.FindGameObjectWithTag("Player");
            if (activePlayer != null) player = activePlayer.transform;
            else return; 
        }
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        switch (type)
        {
            case EnemyType.Passive: HandlePassive(distanceToPlayer); break;
            case EnemyType.MeleeHostile: HandleMeleeHostile(distanceToPlayer); break;
            case EnemyType.RangedHostile: HandleRangedHostile(distanceToPlayer); break; // 🟢 เรียกใช้ Ranged แบบใหม่
            case EnemyType.FlyingHostile: HandleFlyingHostile(distanceToPlayer); break;
            case EnemyType.StationaryTarget: break;
            case EnemyType.BigChaser: HandleBigChaser(); break;
        }

        // อัปเกรดระบบล็อคขา ไม่ให้โดนศัตรูตัวอื่นเบียดกระเด็น
        bool shouldSeparate = true;
        if (type == EnemyType.Passive) 
        {
            shouldSeparate = isAlerted || isFleeingToSafeZone; 
        }
        else if (type == EnemyType.RangedHostile) 
        {
            if (isStationaryShooter) shouldSeparate = false; 
        }
        else if (type == EnemyType.StationaryTarget) 
        {
            shouldSeparate = false; // ถ้าเป็นเป้านิ่ง ห้ามขยับเด็ดขาด!
        }

        if (shouldSeparate && !isLunging && !isRetreating && !isPreparingMelee && !isRangedAiming)
        {
            SeparateFromOtherEnemies();
        }
    }
    
    // ==========================================
    // 🟢 NEW: ระบบตัดสินใจของศัตรูสายยิง (Ranged AI)
    // ==========================================
    void HandleRangedHostile(float distance)
    {
        // ถ้าง้างปืนอยู่ หรือโดนตีถอยหลัง ห้ามเดินหรือคิดอะไรทั้งนั้น
        if (isRangedAiming || isRetreating) return; 

        if (distance <= detectionRange)
        {
            // --- เจอผู้เล่นแล้ว ---
            
            // 1. ถ้าไม่ใช่สายยืนนิ่งๆ และผู้เล่นเข้ามาใกล้เกินไป (เข้าระยะ Safe Distance)
            if (!isStationaryShooter && distance < safeDistance)
            {
                // ทอยลูกเต๋าตัดสินใจทุกๆ 2 วินาที ว่าจะ "หนี" หรือ "ยืนแลก"
                if (Time.time > nextKiteDecisionTime)
                {
                    nextKiteDecisionTime = Time.time + 2f;
                    willStandGround = (Random.value < standGroundChance); 
                }

                if (!willStandGround) 
                {
                    // ตัดสินใจหนี!
                    MoveAwayFromPlayer();
                    if (animator != null) animator.SetBool("isMoving", true);
                    return; // ยกเลิกการยิงไปเลย มุ่งหน้าหนีอย่างเดียว
                }
            }

            // 2. ถ้าถึงเวลายิง (และไม่ได้กำลังหนีอยู่)
            if (Time.time >= nextFireTime)
            {
                StartCoroutine(RangedShootRoutine());
            }
            // 3. ถ้าอยู่นอกระยะยิง (ไกลไป) ให้ขยับเข้าหา
            else if (!isStationaryShooter && distance > safeDistance + 1f)
            {
                MoveTowardsPlayer();
                if (animator != null) animator.SetBool("isMoving", true);
            }
            // 4. ถ้าอยู่ในระยะพอดี และกำลังคูลดาวน์ปืน ให้ยืนรอและเล็งหน้าไปหาผู้เล่น
            else
            {
                if (animator != null) animator.SetBool("isMoving", false);
                FlipSprite(player.position.x);
            }
        }
        else
        {
            // --- ไม่เจอผู้เล่น (อยู่นอกระยะสายตา) ---
            if (!isStationaryShooter)
            {
                // ถ้าเป็นพลปืน ให้เดินลาดตระเวนเล่น
                IdleWander(); 
            }
            else
            {
                // ถ้าเป็นสไนเปอร์ ก็ยืนนิ่งๆ เฝ้าจุด
                if (animator != null) animator.SetBool("isMoving", false);
            }
        }
    }

    // 🟢 ระบบอนิเมชันตอนยิงปืน
    // 🟢 ระบบอนิเมชันตอนยิงปืน (อัปเกรดป้องกันบั๊กลูป)
    IEnumerator RangedShootRoutine()
    {
        isRangedAiming = true;
        
        if (animator != null) 
        {
            animator.SetBool("isMoving", false);
            
            // 🟢 ท่าไม้ตาย: สั่งล้าง Trigger เก่าที่อาจจะค้างอยู่ออกให้หมดก่อน!
            animator.ResetTrigger("PrepareShoot");
            animator.ResetTrigger("Shoot");
        }

        FlipSprite(player.position.x);
        
        // สั่งยกปืน
        if (animator != null) animator.SetTrigger("PrepareShoot");
        
        // รอเวลาง้างปืน
        yield return new WaitForSeconds(aimTime);

        // เช็คเผื่อผู้เล่นตายหรือวาร์ปหายไปตอนง้างปืนพอดี
        if (player == null || !player.gameObject.activeInHierarchy) 
        {
            isRangedAiming = false;
            yield break;
        }

        // หันหน้าอัปเดตเป้าหมายอีกรอบก่อนลั่นไก
        FlipSprite(player.position.x);

        // สั่งลั่นไกปืน
        if (animator != null) animator.SetTrigger("Shoot");
        Shoot(); // เสกกระสุนบินออกไป

        // รอแอนิเมชันยิงปืน (ดีดกลับ) ค้างแปปนึงก่อนกลับไปเดิน
        yield return new WaitForSeconds(0.2f);

        nextFireTime = Time.time + fireRate;
        isRangedAiming = false;
    }

    // 🟢 ระบบเดินเล่นลาดตระเวน (ใช้ร่วมกันได้ทั้ง Passive และ Ranged)
    // 🟢 ระบบเดินเล่นลาดตระเวน (อัปเกรดให้เดินๆ หยุดๆ เนียนขึ้น)
    void IdleWander()
    {
        Vector2 centerPoint = safeZone != null ? (Vector2)safeZone.position : startPosition;
        wanderTimer -= Time.deltaTime;

        if (Mathf.Abs(transform.position.x - wanderTarget.x) < 0.1f)
        {
            if (animator != null) animator.SetBool("isMoving", false); 
            if (wanderTimer <= 0)
            {
                float randomX = Random.Range(-wanderRadius, wanderRadius);
                wanderTarget = new Vector2(centerPoint.x + randomX, transform.position.y);
                wanderTimer = Random.Range(wanderInterval, wanderInterval + 2f);
            }
        }
        else
        {
            if (animator != null) animator.SetBool("isMoving", true); 
            float dirX = Mathf.Sign(wanderTarget.x - transform.position.x);
            SafeMoveX(dirX, speed * 0.5f); // 🟢 ใช้เดินแบบปลอดภัย
            FlipSprite(wanderTarget.x);
        }
    }

    // ==========================================
    // โค้ดส่วนอื่นๆ ปล่อยไว้เหมือนเดิม (ถูกห่อรวมไว้ในคลาสนี้แล้ว)
    // ==========================================

    void SeparateFromOtherEnemies()
    {
        Collider2D[] others = Physics2D.OverlapCircleAll(transform.position, separationRadius);
        foreach (Collider2D col in others)
        {
            if (col.gameObject != gameObject && col.CompareTag("Enemy"))
            {
                float dirX = Mathf.Sign(transform.position.x - col.transform.position.x);
                if (Mathf.Abs(transform.position.x - col.transform.position.x) < 0.1f) 
                {
                    dirX = Random.Range(0, 2) == 0 ? 1f : -1f;
                }
                Vector3 pushVector = new Vector3(dirX * separationForce * Time.deltaTime, 0, 0);
                transform.position += pushVector;
            }
        }
    }
    
    public void SetSafeZone(Transform zone)
    {
        safeZone = zone;
        wanderTarget = transform.position; 
    }

    void HandlePassive(float distance)
    {
        bool playerHasSword = PlayerController.isArmed; 
        bool isScared = distance <= detectionRange && playerHasSword;

        if (isScared && !isAlerted)
        {
            isAlerted = true;
            isChatting = false; 
            currentFleeDirection = Mathf.Sign(transform.position.x - player.position.x);
            if (currentFleeDirection == 0) currentFleeDirection = 1f;
            StartCoroutine(ShowAlertIcon());
        }

        if (isScared) panicTimer = panicDuration;

        if (panicTimer > 0 && !isScared)
        {
            panicTimer -= Time.deltaTime;
            if (panicTimer <= 0) isAlerted = false; 
        }

        if (panicTimer > 0 || isFleeingToSafeZone)
        {
            if (animator != null) animator.SetBool("isMoving", true);

            if (safeZone != null && (isFleeingToSafeZone || (!hasRolledFleeChance && RollFleeToSafeZone())))
            {
                // 🟢 แก้มาใช้ SafeMoveX เพื่อวิ่งเข้า Safe Zone จะได้ไม่มุดกำแพง
                float dirX = Mathf.Sign(safeZone.position.x - transform.position.x);
                SafeMoveX(dirX, speed * 1.5f);
                FlipSprite(safeZone.position.x);
                
                if (Mathf.Abs(transform.position.x - safeZone.position.x) < 0.2f) Destroy(gameObject); 
                return;
            }
            SmartFleeFromPlayer();
        }
        else
        {
            hasRolledFleeChance = false;
            if (Time.time - spawnTime < gracePeriod) return; 

            if (isChatting) HandleChatting();
            else WanderAndLookForFriends();
        }
    }

    bool RollFleeToSafeZone()
    {
        hasRolledFleeChance = true;
        if (Random.value <= fleeToSafeZoneChance)
        {
            isFleeingToSafeZone = true;
            return true;
        }
        return false;
    }

    void SmartFleeFromPlayer()
    {
        Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y + 0.5f);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * currentFleeDirection, wallCheckDist, obstacleLayer);
        
        if (hit.collider != null) currentFleeDirection = -currentFleeDirection; 

        SafeMoveX(currentFleeDirection, speed * 1.5f); // 🟢 ใช้เดินแบบปลอดภัย
        FlipSprite(transform.position.x + currentFleeDirection);
    }

    void WanderAndLookForFriends()
    {
        IdleWander(); // 🟢 เรียกใช้ระบบลาดตระเวนที่แยกไว้

        Collider2D[] others = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        foreach (Collider2D col in others)
        {
            if (col.gameObject != gameObject && col.CompareTag("Enemy"))
            {
                EnemyBehavior friend = col.GetComponent<EnemyBehavior>();
                if (friend != null && friend.type == EnemyType.Passive && !friend.isChatting && !friend.isAlerted)
                {
                    if (Random.value < 0.05f) 
                    {
                        StartChatting(friend);
                        break;
                    }
                }
            }
        }
    }

    public void StartChatting(EnemyBehavior partner)
    {
        isChatting = true;
        chatPartner = partner;
        chatTimer = Random.Range(2f, 4f); 
        FlipSprite(partner.transform.position.x); 
        if (!partner.isChatting) partner.StartChatting(this); 
    }

    void HandleChatting()
    {
        if (animator != null) animator.SetBool("isMoving", false); 
        chatTimer -= Time.deltaTime;

        if (chatTimer <= 0 || chatPartner == null || isAlerted)
        {
            isChatting = false;
            chatPartner = null;
            wanderTimer = 0; 
        }
    }

    IEnumerator ShowAlertIcon()
    {
        if (alertIcon != null)
        {
            alertIcon.SetActive(true);
            yield return new WaitForSeconds(1.0f); 
            alertIcon.SetActive(false);
        }
    }

    void HandleMeleeHostile(float distance)
    {
        if (isRetreating || isPreparingMelee || isLunging) 
        {
            if (animator != null) animator.SetBool("isMoving", false);
            return; 
        }

        if (distance <= detectionRange)
        {
            if (distance <= lungeRange && Time.time >= nextMeleeTime)
            {
                if (animator != null) animator.SetBool("isMoving", false); 
                StartCoroutine(MeleeLungeRoutine()); 
            }
            else if (distance > lungeRange - 0.2f)
            {
                MoveTowardsPlayer();
                if (animator != null) animator.SetBool("isMoving", true); 
            }
            else 
            {
                if (animator != null) animator.SetBool("isMoving", false); 
            }
        }
        else 
        {
            if (animator != null) animator.SetBool("isMoving", false); 
        }
    }
    
    IEnumerator MeleeLungeRoutine()
    {
        isPreparingMelee = true;

        float lockedDirX = Mathf.Sign(player.position.x - transform.position.x);
        if (lockedDirX == 0) lockedDirX = 1f;

        // จุดยิงเรดาร์
        Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y + 0.5f);
        // 🟢 [ท่าไม้ตาย] สร้างกล่องเรดาร์ให้มีความกว้าง/สูง ใกล้เคียงตัวศัตรู (กว้าง 0.8 สูง 0.8)
        Vector2 boxSize = new Vector2(0.8f, 0.8f); 

        // 1. กันทะลุกำแพงตอนง้างตัวถอยหลัง (Windup) แบบยิงกล่อง BoxCast
        float windupDist = 0.5f;
        RaycastHit2D windupHit = Physics2D.BoxCast(rayOrigin, boxSize, 0f, Vector2.right * -lockedDirX, windupDist, obstacleLayer);
        
        // ถ้ากล่องชนกำแพง ให้หักระยะทางออก (ลบแค่ 0.1 พอ เพราะกล่องมันหนาอยู่แล้ว)
        if (windupHit.collider != null) windupDist = Mathf.Max(0f, windupHit.distance - 0.1f); 
        
        Vector2 windupTarget = new Vector2(transform.position.x - (lockedDirX * windupDist), transform.position.y);
        
        float elapsed = 0f;
        Vector2 startPos = transform.position;
        while (elapsed < telegraphTime)
        {
            transform.position = Vector2.Lerp(startPos, windupTarget, elapsed / telegraphTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isPreparingMelee = false;
        isLunging = true;
        
        if (animator != null) animator.SetTrigger("Attack");

        // 2. กันทะลุกำแพงตอนพุ่งโจมตี (Lunge) แบบยิงกล่อง BoxCast
        float distToPlayer = Mathf.Abs(player.position.x - transform.position.x);
        float dashDistance = Mathf.Clamp(distToPlayer - 1.0f, 0.1f, lungeRange * 1.5f); 
        
        RaycastHit2D dashHit = Physics2D.BoxCast(rayOrigin, boxSize, 0f, Vector2.right * lockedDirX, dashDistance, obstacleLayer);
        if (dashHit.collider != null) dashDistance = Mathf.Max(0.1f, dashHit.distance - 0.1f); 

        Vector2 lungeTarget = new Vector2(transform.position.x + (lockedDirX * dashDistance), transform.position.y);
        
        elapsed = 0f;
        float dashDuration = 0.2f; 
        startPos = transform.position;

        while (elapsed < dashDuration)
        {
            transform.position = Vector2.Lerp(startPos, lungeTarget, elapsed / dashDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isLunging = false;
        
        if (animator != null) animator.SetTrigger("Recovery");
        
        nextMeleeTime = Time.time + meleeCooldown; 
    }

    void HandleFlyingHostile(float distance)
    {
        if (distance > detectionRange) return;

        if (!isPreparingToShoot)
        {
            float dirX = Mathf.Sign(transform.position.x - player.position.x); 
            Vector2 baseTargetPos = (Vector2)player.position + new Vector2(dirX * hoverOffset, hoverHeight);

            float bobbingOffset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            Vector2 finalTargetPos = baseTargetPos + new Vector2(0, bobbingOffset);

            transform.position = Vector2.SmoothDamp(transform.position, finalTargetPos, ref flyVelocity, flySmoothTime);
            
            FlipSprite(player.position.x);

            float targetTilt = 0f;
            if (Mathf.Abs(flyVelocity.x) > 0.5f) 
            {
                targetTilt = -Mathf.Sign(flyVelocity.x) * tiltAngle; 
            }
            
            float currentZ = transform.rotation.eulerAngles.z;
            if (currentZ > 180f) currentZ -= 360f; 
            float newZ = Mathf.SmoothDamp(currentZ, targetTilt, ref currentTiltVelocity, tiltSmoothTime);
            transform.rotation = Quaternion.Euler(0, 0, newZ);

            if (Time.time >= nextFireTime && distance <= detectionRange)
            {
                StartCoroutine(DroneShootRoutine());
            }
        }
    }
    
    IEnumerator DroneShootRoutine()
    {
        isPreparingToShoot = true;
        flyVelocity = Vector2.zero; 

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color origColor = sr != null ? sr.color : Color.white;

        if (sr != null) sr.color = Color.red; 
        yield return new WaitForSeconds(0.3f); 

        Shoot();
        
        if (sr != null) sr.color = origColor; 
        Vector2 recoilDir = (transform.position - player.position).normalized;
        
        float recoilTime = 0.15f;
        float elapsed = 0f;
        Vector2 startPos = transform.position;
        Vector2 recoilPos = startPos + (recoilDir * droneRecoilForce); 

        while(elapsed < recoilTime)
        {
            transform.position = Vector2.Lerp(startPos, recoilPos, elapsed / recoilTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        nextFireTime = Time.time + fireRate;
        isPreparingToShoot = false;
    }

    void HandleBigChaser()
    {
        if (isChasing) MoveTowardsPlayer(); 
    }

    // ==========================================
    // 🟢 ระบบเดินแบบปลอดภัย 100% (แก้ไขไม่ให้เรดาร์ขูดพื้น)
    // ==========================================
    void SafeMoveX(float dirX, float currentSpeed)
    {
        if (dirX == 0) return;

        // 🟢 ยกจุดเช็คสูงขึ้นมาที่ระดับอก (Y+0.6) และทำกล่องให้ "แบนลง" (กว้าง 0.5 สูง 0.2)
        // เพื่อป้องกันไม่ให้มันไปชนกับพื้นหรือเนินลาดชัน
        Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y + 0.6f);
        Vector2 boxSize = new Vector2(0.5f, 0.2f);
        
        RaycastHit2D hit = Physics2D.BoxCast(rayOrigin, boxSize, 0f, Vector2.right * dirX, 0.1f, obstacleLayer);

        // ถ้าทางสะดวก ไม่มีกำแพงขวาง ถึงจะเดินได้!
        if (hit.collider == null)
        {
            Vector2 targetPos = new Vector2(transform.position.x + (dirX * currentSpeed * Time.deltaTime), transform.position.y);
            transform.position = targetPos;
        }
    }

    void MoveTowardsPlayer()
    {
        if (type == EnemyType.FlyingHostile)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
            FlipSprite(player.position.x);
            return;
        }

        float dirX = Mathf.Sign(player.position.x - transform.position.x);
        if (Mathf.Abs(player.position.x - transform.position.x) > 0.1f)
        {
            SafeMoveX(dirX, speed); // 🟢 ใช้เดินแบบปลอดภัย
            FlipSprite(player.position.x);
        }
    }

    void MoveAwayFromPlayer()
    {
        if (type == EnemyType.FlyingHostile)
        {
            Vector2 direction = (transform.position - player.position).normalized;
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
            FlipSprite(transform.position.x + direction.x);
            return;
        }

        float dirX = Mathf.Sign(transform.position.x - player.position.x);
        if (dirX == 0) dirX = 1f;

        SafeMoveX(dirX, speed * 1.2f); // 🟢 ใช้เดินแบบปลอดภัย
        FlipSprite(transform.position.x + dirX);
    }

    void FlipSprite(float targetX)
    {
        Vector3 scale = transform.localScale;
        float facingMultiplier = invertFacing ? -1f : 1f; 

        if (targetX > transform.position.x) scale.x = Mathf.Abs(scale.x) * facingMultiplier; 
        else if (targetX < transform.position.x) scale.x = -Mathf.Abs(scale.x) * facingMultiplier; 
        
        transform.localScale = scale;
    }

    void Shoot()
    {
        if (projectilePrefab != null)
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            
            GameObject bullet = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            Vector2 shootDir = (player.position - spawnPos).normalized;
            
            bullet.GetComponent<Rigidbody2D>().linearVelocity = shootDir * 5f; 

            float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    public void TriggerChase()
    {
        isChasing = true;
    }

    public void TakeDamage(int damageAmount)
    {
        if (type == EnemyType.BigChaser) return;

        currentHealth -= damageAmount;

        if (type == EnemyType.MeleeHostile)
        {
            StartCoroutine(RetreatRoutine());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator RetreatRoutine()
    {
        isRetreating = true;
        isPreparingMelee = false;
        isLunging = false;
        
        if (animator != null) animator.SetTrigger("Hurt");
        if (animator != null) animator.SetBool("isStunned", true);

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color origColor = sr != null ? sr.color : Color.white;

        Vector2 knockbackDir = (transform.position - player.position).normalized;
        float kbDirX = Mathf.Sign(knockbackDir.x);
        if (kbDirX == 0) kbDirX = 1f;

        float kbDistance = 2f;
        
        // 🟢 เปลี่ยนมายิงกล่อง BoxCast กวาดไปด้านหลังแทนเลเซอร์เส้นเดียว
        Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y + 0.5f);
        Vector2 boxSize = new Vector2(0.8f, 0.8f);
        
        RaycastHit2D hit = Physics2D.BoxCast(rayOrigin, boxSize, 0f, Vector2.right * kbDirX, kbDistance, obstacleLayer);
        if (hit.collider != null) kbDistance = Mathf.Max(0f, hit.distance - 0.1f);

        Vector2 targetPos = new Vector2(transform.position.x + (kbDirX * kbDistance), transform.position.y);

        float elapsed = 0f;
        float kbDuration = 0.15f; 
        Vector2 startPos = transform.position;

        while(elapsed < kbDuration)
        {
            transform.position = Vector2.Lerp(startPos, targetPos, elapsed / kbDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(Mathf.Max(0, retreatTime - kbDuration));
        
        if (animator != null) animator.SetBool("isStunned", false); 
        if (sr != null) sr.color = origColor;
        
        isRetreating = false;
    }

    void Die()
    {
        if (bloodPrefab != null)
        {
            Vector3 centerPosition = transform.position + new Vector3(0, bloodHeightOffset, 0);
            int bloodAmount = Random.Range(minBloodSpawns, maxBloodSpawns + 1);
            
            for (int i = 0; i < bloodAmount; i++)
            {
                Vector2 randomOffset = new Vector2(
                    Random.Range(-bloodSpread, bloodSpread), 
                    Random.Range(-bloodSpread, bloodSpread)
                );
                
                Vector3 spawnPosition = centerPosition + (Vector3)randomOffset;
                Instantiate(bloodPrefab, spawnPosition, Quaternion.identity);
            }
        }

        Destroy(gameObject); 
    }
    
    [Header("Hitbox Settings")]
    public GameObject enemyHitbox; 

    public void AnimEvent_EnableHitbox()
    {
        if (enemyHitbox != null) enemyHitbox.SetActive(true);
    }

    public void AnimEvent_DisableHitbox()
    {
        if (enemyHitbox != null) enemyHitbox.SetActive(false);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (type == EnemyType.MeleeHostile)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, lungeRange);
        }
        // 🟢 เพิ่มวาดวงกลมสีส้ม สำหรับระยะวิ่งหนีของสายปืน
        else if (type == EnemyType.RangedHostile)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f); // สีส้ม
            Gizmos.DrawWireSphere(transform.position, safeDistance);
        }
        
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePoint.position, 0.15f);
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (type == EnemyType.StationaryTarget || type == EnemyType.Passive || type == EnemyType.MeleeHostile) return;

            PlayerController p = collision.gameObject.GetComponent<PlayerController>();
            if (p != null)
            {
                p.TakeDamage(damage);
            }
        }
    }
    
    
}