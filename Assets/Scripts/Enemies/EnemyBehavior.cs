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
    
    private bool isTriggeringCombatFX = false;
    private float searchPlayerTimer = 0f;

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
    private Coroutine activeStunRoutine;
    
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
    
    [Header("Stun Effect (Thrown Sword)")]
    public GameObject dizzyEffect; // ลาก GameObject เอฟเฟคดาวหมุนๆ มาใส่ช่องนี้
    private bool isSwordStunned = false; // ตัวแปรเช็คว่ามึนอยู่ไหม
    
    [Header("Chaser Settings")]
    public bool waitToChase = false;
    private bool isChasing = false;
    
    [Header("AI Spacing (Anti-Overlap)")]
    public float separationRadius = 0.8f; 
    public float separationForce = 1.5f;  
    
    [Header("Audio SFX")]
    public AudioClip alertSound;   // 🟢 เสียงตกใจตอนเจอผู้เล่น (เครื่องหมายตกใจขึ้น)
    public AudioClip attackSound;  // 🟢 เสียงโจมตี (ฟัน/ยิงปืน/โดรนยิง)
    public AudioClip hurtSound;    // 🟢 เสียงร้องตอนโดนฟัน
    public AudioClip deathSound;   // 🟢 เสียงตาย (ระเบิดเลือด)
    public AudioClip stunSound;    // 🟢 เสียงมึนงง (ตอนโดนผู้เล่นปาดาบอัดหน้า)
    
    [Header("Raycast Tuning")]
    [Tooltip("ปรับจุดกำเนิดเลเซอร์ให้ยื่นออกจากตัว ป้องกันการยิงติด Collider ตัวเอง (X: แนวนอน, Y: แนวตั้ง)")]
    public Vector2 raycastOffset = Vector2.zero; 

    void Start()
    {   
        if (dizzyEffect != null) dizzyEffect.SetActive(false);
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
        // 🟢 แทนที่บล็อค IgnoreCollision เดิม
        if (type == EnemyType.MeleeHostile || type == EnemyType.Passive || 
            type == EnemyType.RangedHostile || type == EnemyType.StationaryTarget)
        {
            Collider2D[] myColliders = GetComponentsInChildren<Collider2D>();
    
            // 🟢 เช็คทั้งสองร่าง ไม่ใช่แค่ตัวที่ Active อยู่
            if (CharacterSwitcher.Instance != null)
            {
                // รวม Collider ของทั้งสองร่างไว้ใน List เดียว
                var allPlayerCols = new System.Collections.Generic.List<Collider2D>();
                if (CharacterSwitcher.Instance.unarmedPlayer != null)
                    allPlayerCols.AddRange(CharacterSwitcher.Instance.unarmedPlayer.GetComponentsInChildren<Collider2D>());
                if (CharacterSwitcher.Instance.armedPlayer != null)
                    allPlayerCols.AddRange(CharacterSwitcher.Instance.armedPlayer.GetComponentsInChildren<Collider2D>());
        
                foreach (Collider2D myCol in myColliders)
                foreach (Collider2D pCol in allPlayerCols)
                    Physics2D.IgnoreCollision(myCol, pCol, true);
            }
        }
    }

    void Update()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            searchPlayerTimer -= Time.deltaTime;
            if (searchPlayerTimer <= 0f)
            {
                GameObject activePlayer = GameObject.FindGameObjectWithTag("Player");
                if (activePlayer != null) 
                {
                    player = activePlayer.transform;
                }
                searchPlayerTimer = 0.5f; // ถ้าหาไม่เจอ ให้รออีกครึ่งวินาทีค่อยหาใหม่ (ประหยัดสเปคสุดๆ)
            }
            return; // ยังไม่มีผู้เล่น ก็ให้มอนสเตอร์ยืนโง่ๆ ไปก่อน
        }
        
        if (isSwordStunned && type != EnemyType.BigChaser) return;
        
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
    
    void HandleRangedHostile(float distance)
    {
        // ถ้าง้างปืนอยู่ หรือโดนตีถอยหลัง ห้ามเดินหรือคิดอะไรทั้งนั้น
        if (isRangedAiming || isRetreating) return; 

        if (distance <= detectionRange && HasLineOfSightToPlayer(distance))
        {
            // --- เจอผู้เล่นแล้ว ---
            if (!isTriggeringCombatFX) // ถ้ายังไม่ได้เปิดเอฟเฟกต์
            {
                isTriggeringCombatFX = true;
                if (CameraJuiceFX.Instance != null) CameraJuiceFX.Instance.SetCombatMode(true); 
            }
            
            // 1. ถ้าไม่ใช่สายยืนนิ่งๆ และผู้เล่นเข้ามาใกล้เกินไป (เข้าระยะ Safe Distance)
            if (!isStationaryShooter && distance < (willStandGround ? safeDistance : safeDistance + 0.5f))
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
                    return; 
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
            if (isTriggeringCombatFX) // ถ้าเคยเปิดเอฟเฟกต์ไว้ ต้องปิดคืน
            {
                isTriggeringCombatFX = false;
                if (CameraJuiceFX.Instance != null) CameraJuiceFX.Instance.SetCombatMode(false); 
            }
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
    // 🟢 ระบบอนิเมชันตอนยิงปืน (อัปเกรดระดับ Pro: หันปืนตามเป้าหมาย & ยกเลิกถ้านอกระยะ)
    IEnumerator RangedShootRoutine()
    {
        isRangedAiming = true; // ล็อค State
        
        if (animator != null) 
        {
            animator.SetBool("isMoving", false);
            animator.ResetTrigger("PrepareShoot");
            animator.ResetTrigger("Shoot");
        }

        // 1. เริ่มง้างปืน
        if (animator != null) animator.SetTrigger("PrepareShoot");
        
        // 2. ช่วงเวลาง้างปืน (Aiming Phase)
        float elapsed = 0f;
        while (elapsed < aimTime) 
        {
            // 🔥 Failsafe: ถ้าผู้เล่นตาย, หายไป, หรือวิ่งหนีออกนอกระยะสายตาแล้ว ให้ยกเลิกการยิง!
            if (player == null || !player.gameObject.activeInHierarchy || Vector2.Distance(transform.position, player.position) > detectionRange) 
            {
                // กลับไปยืนโหมดปกติ
                if (animator != null) animator.SetTrigger("CancelShoot"); // ⚠️ อย่าลืมไปสร้าง Trigger นี้ใน Animator เพื่อกลับไปท่า Idle นะครับ
                isRangedAiming = false; 
                yield break; // หยุดคอรูทีนทันที!
            }

            // 🔥 Game Feel: หันหน้าตามผู้เล่นตลอดเวลาที่กำลังง้างปืน (โคตรกดดัน!)
            FlipSprite(player.position.x); 

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 3. ลั่นไกยิง!
        if (animator != null) animator.SetTrigger("Shoot");
        Shoot(); // เสกกระสุนบินออกไป

        // 4. รอแอนิเมชันยิงปืน (จังหวะปืนดีดกลับ / Recoil)
        yield return new WaitForSeconds(0.2f);

        nextFireTime = Time.time + fireRate;
        isRangedAiming = false;
    }

    // 🟢 ระบบเดินเล่นลาดตระเวน (ใช้ร่วมกันได้ทั้ง Passive และ Ranged)
    // 🟢 ระบบเดินเล่นลาดตระเวน (อัปเกรดให้เดินๆ หยุดๆ เนียนขึ้น)
    // 🟢 อัปเกรด: ระบบเดินเล่นลาดตระเวน (แก้บัคย่ำเท้าอยู่กับที่)
    void IdleWander()
    {
        Vector2 centerPoint = safeZone != null ? (Vector2)safeZone.position : startPosition;
        wanderTimer -= Time.deltaTime;

        bool isCloseToTarget = Mathf.Abs(transform.position.x - wanderTarget.x) < 0.1f;

        // ถ้าถึงที่หมายแล้ว หรือ ติดบัคยืนแช่นานเกินไป (wanderTimer ติดลบเกิน 2 วิ) ให้หาเป้าหมายใหม่!
        if (isCloseToTarget || wanderTimer <= -2f)
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
            
            // ลองเดินดู ถ้าเดินไม่ได้ (ติดเหว/กำแพง)
            bool successfullyMoved = SafeMoveX(dirX, speed * 0.5f); 
            
            if (!successfullyMoved)
            {
                // ถ้าติดเหวหรือกำแพง ให้ล้างเวลาทิ้ง เพื่อบังคับให้มันคิดหาทิศทางเดินใหม่ทันที!
                wanderTimer = 0f; 
                if (animator != null) animator.SetBool("isMoving", false);
            }

            FlipSprite(wanderTarget.x);
        }
    }

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

                // 🟢 เช็คกำแพงก่อนผลัก
                Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y + 0.6f);
                Vector2 boxSize = new Vector2(0.5f, 0.2f);
                RaycastHit2D wallHit = Physics2D.BoxCast(rayOrigin, boxSize, 0f, Vector2.right * dirX, 0.2f, obstacleLayer);
                if (wallHit.collider != null) continue; // มีกำแพง → ข้ามไปเลย ห้ามผลัก

                // 🟢 เช็คว่ามีพื้นรองรับทางที่จะผลักไหม (กันตกแมพ)
                Vector2 groundCheckPos = new Vector2(transform.position.x + (dirX * separationRadius), transform.position.y);
                RaycastHit2D groundHit = Physics2D.Raycast(groundCheckPos, Vector2.down, 1.5f, obstacleLayer);
                if (groundHit.collider == null) continue; // ไม่มีพื้น → ห้ามผลัก

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
            
            AudioManager.Instance.PlaySFX(alertSound, 0.8f);
            
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

        if (distance <= detectionRange && HasLineOfSightToPlayer(distance))
        {   
            if (!isTriggeringCombatFX) // ถ้ายังไม่ได้เปิดเอฟเฟกต์
            {
                isTriggeringCombatFX = true;
                if (CameraJuiceFX.Instance != null) CameraJuiceFX.Instance.SetCombatMode(true); 
            }
            
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
            if (isTriggeringCombatFX) // ถ้าเคยเปิดเอฟเฟกต์ไว้ ต้องปิดคืน
            {
                isTriggeringCombatFX = false;
                if (CameraJuiceFX.Instance != null) CameraJuiceFX.Instance.SetCombatMode(false); 
            }
            
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
        
        AudioManager.Instance.PlaySFX(attackSound, 0.3f);
        
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
        if (distance > detectionRange || !HasLineOfSightToPlayer(distance)) return;
        
        if (!isTriggeringCombatFX) // ถ้ายังไม่ได้เปิดเอฟเฟกต์
        {
            isTriggeringCombatFX = true;
            if (CameraJuiceFX.Instance != null) CameraJuiceFX.Instance.SetCombatMode(true); 
        }
        
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
        if (isChasing) 
        {
            // 🟢 เปิดเอฟเฟกต์ตึงเครียดตอนบอสไล่กวด!
            if (!isTriggeringCombatFX && CameraJuiceFX.Instance != null)
            {
                isTriggeringCombatFX = true;
                CameraJuiceFX.Instance.SetCombatMode(true);
            }
        
            MoveTowardsPlayer(); 
        }
    }

    
    // ==========================================
    // 🟢 ระบบเดินปลอดภัย: อัปเกรดรองรับ Raycast Offset หนี Collider
    // ==========================================
    bool SafeMoveX(float dirX, float currentSpeed)
    {
        if (dirX == 0) return false;

        float wallCheckDist = 0.3f; 

        // 🟢 สร้างจุดอ้างอิงใหม่ที่บวกค่า Offset เข้าไปแล้ว (คูณ dirX ให้ขยับตามหน้าเว็บที่หัน)
        Vector2 basePosition = new Vector2(
            transform.position.x + (raycastOffset.x * dirX), 
            transform.position.y + raycastOffset.y
        );

        // 1. เช็คกำแพงด้วยเลเซอร์เส้นบน
        Vector2 originTop = new Vector2(basePosition.x, basePosition.y + 0.8f);
        RaycastHit2D wallTop = Physics2D.Raycast(originTop, Vector2.right * dirX, wallCheckDist, obstacleLayer);

        // 2. เช็คกำแพงด้วยเลเซอร์เส้นล่าง
        Vector2 originBottom = new Vector2(basePosition.x, basePosition.y + 0.2f);
        RaycastHit2D wallBottom = Physics2D.Raycast(originBottom, Vector2.right * dirX, wallCheckDist, obstacleLayer);

        if (wallTop.collider != null || wallBottom.collider != null) return false;

        // 3. เช็คพื้น (ยื่นเลเซอร์ไปข้างหน้า 0.5 หน่วย จากจุดฐานใหม่)
        Vector2 groundCheckPos = new Vector2(basePosition.x + (dirX * 0.5f), basePosition.y + 0.2f);
        RaycastHit2D groundHit = Physics2D.Raycast(groundCheckPos, Vector2.down, 1.5f, obstacleLayer);
    
        if (groundHit.collider == null) return false; 

        // 4. เดินได้!
        Vector2 targetPos = new Vector2(transform.position.x + (dirX * currentSpeed * Time.deltaTime), transform.position.y);
        transform.position = targetPos;
        return true; 
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
            SafeMoveX(dirX, speed); 
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

        SafeMoveX(dirX, speed * 1.2f); 
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
        AudioManager.Instance.PlaySFX(attackSound, 1.0f);
        
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
        
        AudioManager.Instance.PlaySFX(hurtSound, 0.6f);

        if (type == EnemyType.MeleeHostile)
        {
            StartCoroutine(RetreatRoutine());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 🟢 ฟังก์ชันใหม่: โดนดาบปาใส่ (ลดเลือด + ติดมึนงง โดยไม่กระเด็น)
    public void ApplySwordStun(int damageAmount, float stunDuration)
    {
        if (type == EnemyType.BigChaser) return; // บอสไม่โดนสตัน

        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (activeStunRoutine != null) StopCoroutine(activeStunRoutine);
            activeStunRoutine = StartCoroutine(SwordStunRoutine(stunDuration));
        }
    }

    // 🟢 กระบวนการทำมึนงง
    IEnumerator SwordStunRoutine(float duration)
    {
        isSwordStunned = true;
        
        AudioManager.Instance.PlaySFX(stunSound, 1.0f);
        
        // ยกเลิกสถานะการโจมตี/เดิน อื่นๆ ทั้งหมด
        isRetreating = false; 
        isLunging = false;
        isPreparingMelee = false;
        isRangedAiming = false;

        // สั่งหยุดเดินและเล่นอนิเมชันเจ็บ
        if (animator != null)
        {
            animator.SetBool("isMoving", false);
            animator.SetTrigger("Hurt");
            animator.SetBool("isStunned", true); // ถ้าคุณมี State นี้ใน Animator
        }

        // 🌟 เปิด GameObject เอฟเฟควินเวียน
        if (dizzyEffect != null) dizzyEffect.SetActive(true);

        // รอเวลาให้หายมึน
        yield return new WaitForSeconds(duration);

        // 🌟 ปิด GameObject เอฟเฟควินเวียน
        if (dizzyEffect != null) dizzyEffect.SetActive(false);
        if (animator != null) animator.SetBool("isStunned", false);

        isSwordStunned = false; // กลับมาขยับได้ปกติ
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
        if (isTriggeringCombatFX && CameraJuiceFX.Instance != null)
        {
            isTriggeringCombatFX = false;
            CameraJuiceFX.Instance.SetCombatMode(false);
        }
        
        AudioManager.Instance.PlaySFX(deathSound, 1.2f);
        
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
        // ... (โค้ดวาดระยะการมองเห็นสีเหลือง/แดง/ส้มด้านบน ปล่อยไว้เหมือนเดิม) ...

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (type == EnemyType.MeleeHostile)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, lungeRange);
        }
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

        // ==========================================
        // 🟢 DEBUG GIZMOS: เลเซอร์เช็คทาง (อัปเดตตาม Offset)
        // ==========================================
        float fakeDirX = transform.localScale.x > 0 ? 1f : -1f; 
        
        Vector2 basePosGizmo = new Vector2(
            transform.position.x + (raycastOffset.x * fakeDirX), 
            transform.position.y + raycastOffset.y
        );

        // 1. วาดเลเซอร์เช็คกำแพง (เส้นบน-ล่าง สีฟ้า)
        Gizmos.color = Color.cyan;
        Vector2 originTop = new Vector2(basePosGizmo.x, basePosGizmo.y + 0.8f);
        Vector2 originBottom = new Vector2(basePosGizmo.x, basePosGizmo.y + 0.2f);
        Gizmos.DrawLine(originTop, originTop + (Vector2.right * fakeDirX * 0.3f));
        Gizmos.DrawLine(originBottom, originBottom + (Vector2.right * fakeDirX * 0.3f));

        // 2. วาดเส้นเช็คพื้น (สีเขียว)
        Gizmos.color = Color.green;
        Vector2 groundCheckPos = new Vector2(basePosGizmo.x + (fakeDirX * 0.5f), basePosGizmo.y + 0.2f);
        Gizmos.DrawLine(groundCheckPos, groundCheckPos + (Vector2.down * 1.5f));
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
    
    private bool HasLineOfSightToPlayer(float currentDistance)
    {
        if (player == null) return false;
        
        // ถ้าผู้เล่นอยู่นอกระยะมองเห็น ก็ไม่ต้องคำนวณให้เสียเวลา
        if (currentDistance > detectionRange) return false;

        // คำนวณทิศทางจากตาของศัตรู ไปหา ผู้เล่น
        Vector2 origin = new Vector2(transform.position.x, transform.position.y + 0.5f);
        Vector2 target = new Vector2(player.position.x, player.position.y + 0.5f);
        Vector2 direction = (target - origin).normalized;

        // ยิงเลเซอร์ไปหาผู้เล่น โดยเช็คเฉพาะ Layer ที่เป็นอุปสรรค (obstacleLayer)
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, currentDistance, obstacleLayer);

        // ถ้าเลเซอร์ยิงไป "ไม่โดนกำแพงเลย" แสดงว่ามองเห็นผู้เล่นชัดเจน! (Return True)
        return hit.collider == null;
    }
    
    private void OnDisable()
    {
        // 🟢 ถ้าศัตรูโดนปิดการทำงาน หรือถูกลบทิ้งไปกลางอากาศ 
        // ต้องเคลียร์ค่าความเครียดในกล้องทิ้งทันที!
        if (isTriggeringCombatFX && CameraJuiceFX.Instance != null)
        {
            isTriggeringCombatFX = false;
            CameraJuiceFX.Instance.SetCombatMode(false);
        }
    }
    
}