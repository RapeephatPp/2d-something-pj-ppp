using UnityEngine;
using System.Collections;

public class EnemyBehavior : MonoBehaviour
{
    public enum EnemyType 
    { 
        Passive,            // ไม่สู้ วิ่งหนีเมื่อเห็นดาบ
        MeleeHostile,       // วิ่งเข้าหา บาดเจ็บแล้วถอย
        RangedHostile,      // รักษาระยะห่าง คอยยิง
        FlyingHostile,      // บินยิงกวนผู้เล่น
        StationaryTarget,   // อยู่นิ่งๆ ให้ผู้เล่นกด F พุ่งหา (ไม่ทำดาเมจ)
        BigChaser           // อมตะ ไล่ล่าอย่างเดียว
    }
    
    public EnemyType type;

    [Header("Base Settings")]
    public int maxHealth = 3; 
    private int currentHealth;
    public int damage = 1; 
    public float speed = 2f;
    
    [Header("Detection & Movement")]
    public float detectionRange = 5f;   // ระยะมองเห็นผู้เล่น
    public float safeDistance = 3f;     // ระยะห่างที่ Ranged จะพยายามรักษาไว้
    public float retreatTime = 2f;      // เวลาหนีเมื่อ Melee บาดเจ็บ
    private bool isRetreating = false;
    
    [Header("Passive NPC Settings")]
    public float fleeToSafeZoneChance = 0.5f; // โอกาส 50% ที่จะวิ่งกลับบ้าน
    public float wanderRadius = 3f;           // รัศมีระยะการเดินเล่นรอบๆ จุดเกิด
    public float wanderInterval = 2f;         // เวลาที่ใช้พักก่อนเดินไปจุดต่อไป
    
    private float spawnTime; // 🟢 เก็บเวลาตอนที่เกิดมา
    public float gracePeriod = 1.5f; // 🟢 เวลาตั้งตัว (วินาที) ที่จะไม่สนใจผู้เล่น
    
    [Header("Passive AI Enhancements")]
    public GameObject alertIcon;        // 🟢 ลากรูประฆัง/เครื่องหมายตกใจมาใส่ช่องนี้
    public LayerMask obstacleLayer;     // 🟢 เลเยอร์ที่เป็นกำแพง/พื้น (เอาไว้เช็คทางตัน)
    public float wallCheckDist = 1f;    // 🟢 ระยะยิงเรดาร์เช็คกำแพง
    public float panicDuration = 5f;
    
    private Transform safeZone;               // จุดเกิด/จุดหนีกลับ
    private Vector2 startPosition;
    private Vector2 wanderTarget;
    private float wanderTimer;
    private bool isFleeingToSafeZone = false;
    private bool hasRolledFleeChance = false; // เช็คเพื่อไม่ให้ทอยเต๋าซ้ำทุกเฟรม
    
    private float panicTimer = 0f;           // เอาไว้จับเวลาหนี
    private float currentFleeDirection = 1f; // ็อคทิศทางการวิ่งหนี จะได้ไม่สั่น
    private bool isCornered = false;
    private float corneredTimer = 0f;
    private bool isChatting = false;
    private float chatTimer = 0f;
    private EnemyBehavior chatPartner = null;
    private bool isAlerted = false;

    [Header("Combat Settings")]
    public GameObject projectilePrefab; // กระสุนสำหรับ Range/Fly
    public Transform firePoint;
    public float fireRate = 1.5f;
    private float nextFireTime;
    
    [Header("Melee Game Feel")]
    public float lungeRange = 2.5f;      // ระยะที่จะเริ่มง้างฟัน
    public float telegraphTime = 0.5f;   // เวลาง้างดาบ (ยิ่งเยอะ ผู้เล่นยิ่งมีเวลาหลบ)
    public float lungeSpeedMultiplier = 3f; // ความแรงตอนพุ่งฟัน (เร็วกว่าเดินปกติ 3 เท่า)
    public float meleeCooldown = 1.5f;   // เวลาพักเหนื่อยหลังฟันเสร็จ

    private bool isPreparingMelee = false;
    private bool isLunging = false;
    private float nextMeleeTime;
    
    [Header("Drone (Flying) Settings")]
    public float hoverHeight = 3f;          // ความสูงตอนลอยเหนือผู้เล่น
    public float hoverOffset = 2.5f;        // ระยะเยื้องซ้ายขวา (ไม่ให้อยู่ตรงหัวเป๊ะๆ จะได้ยิงเฉียงๆ)
    public float bobFrequency = 2f;         // ความเร็วในการกระเพื่อมขึ้นลง
    public float bobAmplitude = 0.4f;       // ความสูงที่กระเพื่อม
    public float flySmoothTime = 0.4f;      // ความนุ่มนวลตอนบินตาม (ยิ่งเยอะยิ่งหนืด)
    public float droneRecoilForce = 1.5f;   // แรงถีบตอนยิงปืน
    
    [Header("Drone Game Feel")]
    public float tiltAngle = 15f;           // องศาการเอียงซ้ายขวาตอนบิน
    public float tiltSmoothTime = 0.15f;    // ความนุ่มนวลตอนเอียงตัว
    public bool invertFacing = false;       // 🟢 ติ๊กถูกอันนี้ใน Inspector ถ้าโดรนคุณหันหน้าผิดฝั่ง!
    
    private float currentTiltVelocity;      // เอาไว้คำนวณเอียงตัว

    private Vector2 flyVelocity;            // ตัวแปรซ่อนไว้ใช้คำนวณ SmoothDamp
    private bool isPreparingToShoot = false;

    [Header("References")]
    public Transform player;            // ลาก Player มาใส่ หรือจะหาจาก Tag ใน Start ก็ได้// Prefab เลือดตอนตาย
    public Animator animator; // 🟢 เพิ่มบรรทัดนี้ เพื่อรอรับ Animator ของศัตรู
    
    [Header("Blood Splatter Settings")]
    public GameObject bloodPrefab;      // Prefab เลือดตอนตาย
    public int minBloodSpawns = 3;      // จำนวนเลือดขั้นต่ำที่จะกระเด็นออกมา
    public int maxBloodSpawns = 6;      // จำนวนเลือดสูงสุด
    public float bloodSpread = 1.5f;    // รัศมีการกระจายตัวของเลือด (ยิ่งเยอะยิ่งกระจายกว้าง)
    public float bloodHeightOffset = 1.0f;
    
    [Header("Chaser Settings")]
    public bool waitToChase = false;
    private bool isChasing = false;
    
    [Header("AI Spacing (Anti-Overlap)")]
    public float separationRadius = 0.8f; // 🟢 ระยะวงกลมเช็คมอนสเตอร์ตัวอื่น
    public float separationForce = 1.5f;  // 🟢 แรงผลักไม่ให้ยืนทับกัน

    void Start()
    {   
        spawnTime = Time.time;
        currentHealth = maxHealth;
        startPosition = transform.position;
        
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (type == EnemyType.BigChaser && !waitToChase)
        {
            isChasing = true;
        }

        // 🟢 [เพิ่มใหม่] สั่งปิดการชนกันแบบฟิสิกส์ (ป้องกันศัตรูดันผู้เล่นจนลอย)
        // ศัตรูจะพุ่งทะลุตัวเราได้ แต่ "กล่องดาบ" ของมันยังคงฟันโดนเราเสียเลือดอยู่ดี!
        if (type == EnemyType.MeleeHostile || type == EnemyType.Passive)
        {
            Collider2D myCollider = GetComponent<Collider2D>();
            if (player != null && myCollider != null)
            {
                Collider2D[] playerColliders = player.GetComponents<Collider2D>();
                foreach (Collider2D pCol in playerColliders)
                {
                    Physics2D.IgnoreCollision(myCollider, pCol, true);
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

        // 1. ให้มันคิดพฤติกรรมการเดินปกติก่อน
        switch (type)
        {
            case EnemyType.Passive: HandlePassive(distanceToPlayer); break;
            case EnemyType.MeleeHostile: HandleMeleeHostile(distanceToPlayer); break;
            case EnemyType.RangedHostile: HandleRangedHostile(distanceToPlayer); break;
            case EnemyType.FlyingHostile: HandleFlyingHostile(distanceToPlayer); break;
            case EnemyType.StationaryTarget: break;
            case EnemyType.BigChaser: HandleBigChaser(); break;
        }

        // 🟢 2. [ย้ายมาไว้ตรงนี้!] พอเดินเสร็จปุ๊บ ค่อยมาเช็คว่าเดินทับเพื่อนไหม ถ้าทับให้ดีดออกทันที
        bool shouldSeparate = true;
        if (type == EnemyType.Passive)
        {
            // ถ้าเป็นตัว Passive ให้ผลักกัน "เฉพาะตอนกำลังหนีตกใจ" เท่านั้น
            // เวลาเดินเล่นปกติจะได้เดินทะลุซ้อนกันได้
            shouldSeparate = isAlerted || isFleeingToSafeZone; 
        }

        if (shouldSeparate && !isLunging && !isRetreating && !isPreparingMelee)
        {
            SeparateFromOtherEnemies();
        }
    }
    
    void SeparateFromOtherEnemies()
    {
        // ค้นหามอนสเตอร์ตัวอื่นที่อยู่ในระยะ
        Collider2D[] others = Physics2D.OverlapCircleAll(transform.position, separationRadius);
        foreach (Collider2D col in others)
        {
            if (col.gameObject != gameObject && col.CompareTag("Enemy"))
            {
                // ดูว่าเพื่อนอยู่ฝั่งไหน
                float dirX = Mathf.Sign(transform.position.x - col.transform.position.x);
                
                // ถ้าเกิดมาทับกันเป๊ะๆ ระดับมิลลิเมตร ให้สุ่มดีดคนละฝั่ง
                if (Mathf.Abs(transform.position.x - col.transform.position.x) < 0.1f) 
                {
                    dirX = Random.Range(0, 2) == 0 ? 1f : -1f;
                }
                
                // 🟢 [อัปเกรด!] ใช้การ "บวกตำแหน่ง" (+ Vector3) ทับเข้าไปเลย 
                // แบบนี้ต่อให้กำลังเดินอยู่ มันก็จะถูกไถออกข้างๆ อย่างเนียนๆ!
                Vector3 pushVector = new Vector3(dirX * separationForce * Time.deltaTime, 0, 0);
                transform.position += pushVector;
            }
        }
    }
    
    public void SetSafeZone(Transform zone)
    {
        safeZone = zone;
        wanderTarget = transform.position; // เริ่มต้นด้วยการยืนที่เดิมก่อน
    }

    // --- Behavior Methods ---

    void HandlePassive(float distance)
    {
        bool playerHasSword = PlayerController.isArmed; 
        bool isScared = distance <= detectionRange && playerHasSword;

        // 🟢 1. ระบบตกใจเริ่มทำงาน
        if (isScared && !isAlerted)
        {
            isAlerted = true;
            isChatting = false; 
            
            // ล็อคทิศทางให้วิ่งไปฝั่งตรงข้ามผู้เล่น ทันทีที่ตกใจ!
            currentFleeDirection = Mathf.Sign(transform.position.x - player.position.x);
            if (currentFleeDirection == 0) currentFleeDirection = 1f;
            
            StartCoroutine(ShowAlertIcon());
        }

        // รีเฟรชเวลาหนีเรื่อยๆ ถ้าผู้เล่นยังตามติดอยู่ในระยะ
        if (isScared) 
        {
            panicTimer = panicDuration;
        }

        // 🟢 2. โหมดลดความกลัว (ถ้าวิ่งหนีจนพ้นระยะแล้ว)
        if (panicTimer > 0 && !isScared)
        {
            panicTimer -= Time.deltaTime;
            if (panicTimer <= 0) 
            {
                isAlerted = false; // เลิกตกใจ กลับไปเดินชิล
            }
        }

        // 🟢 3. โหมดหนีตาย (หนีเพราะแพนิคอยู่ หรือกำลังหนีกลับหลุม)
        if (panicTimer > 0 || isFleeingToSafeZone)
        {
            if (animator != null) animator.SetBool("isMoving", true);

            if (safeZone != null && (isFleeingToSafeZone || (!hasRolledFleeChance && RollFleeToSafeZone())))
            {
                Vector2 targetPos = new Vector2(safeZone.position.x, transform.position.y);
                transform.position = Vector2.MoveTowards(transform.position, targetPos, speed * 1.5f * Time.deltaTime);
                FlipSprite(safeZone.position.x);

                if (Mathf.Abs(transform.position.x - safeZone.position.x) < 0.2f) Destroy(gameObject); 
                return;
            }
            
            // หนีแบบธรรมดา
            SmartFleeFromPlayer();
        }
        // 🟢 4. โหมดชิล (เดินเล่น / คุย)
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
        // 🟢 อัปเกรด: ใช้ทิศทางเดิม (currentFleeDirection) ที่ล็อคไว้ตอนตกใจ 
        // ไม่ต้องคำนวณตำแหน่งผู้เล่นใหม่ตลอดเวลา มันจะได้ไม่สับสนเวลาวิ่งทะลุตัวผู้เล่น!
        
        Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y + 0.5f);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * currentFleeDirection, wallCheckDist, obstacleLayer);
        
        if (hit.collider != null)
        {
            // ถ้าชนกำแพงปุ๊บ ให้หันหลังกลับ 180 องศา แล้ววิ่งหน้าตั้งต่อไปเลย
            currentFleeDirection = -currentFleeDirection; 
        }

        Vector2 targetPos = new Vector2(transform.position.x + (currentFleeDirection * 5f), transform.position.y);
        transform.position = Vector2.MoveTowards(transform.position, targetPos, (speed * 1.5f) * Time.deltaTime);
        FlipSprite(targetPos.x);
    }

    // 🟢 ระบบเดินเล่นหาเพื่อน
    void WanderAndLookForFriends()
    {
        // 🟢 ถ้าไม่มีหลุม SafeZone ให้ใช้ตำแหน่งเริ่มต้นตอนวางในฉาก เป็นศูนย์กลางแทน! (แก้บั๊กยืนนิ่ง)
        Vector2 centerPoint = safeZone != null ? (Vector2)safeZone.position : startPosition;
        
        if (animator != null) animator.SetBool("isMoving", true);

        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0 || Mathf.Abs(transform.position.x - wanderTarget.x) < 0.1f)
        {
            float randomX = Random.Range(-wanderRadius, wanderRadius);
            wanderTarget = new Vector2(centerPoint.x + randomX, transform.position.y);
            wanderTimer = Random.Range(wanderInterval, wanderInterval + 2f);
        }

        Vector2 targetPosition = new Vector2(wanderTarget.x, transform.position.y);
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, (speed * 0.5f) * Time.deltaTime);
        FlipSprite(wanderTarget.x);

        // เช็คว่ามีเพื่อน Passive อยู่ใกล้ๆ ไหม
        Collider2D[] others = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        foreach (Collider2D col in others)
        {
            if (col.gameObject != gameObject && col.CompareTag("Enemy"))
            {
                EnemyBehavior friend = col.GetComponent<EnemyBehavior>();
                if (friend != null && friend.type == EnemyType.Passive && !friend.isChatting && !friend.isAlerted)
                {
                    // ถ้าเดินมาเจอกัน มีโอกาสทักทายกัน
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
        chatTimer = Random.Range(2f, 4f); // สุ่มเวลายืนคุยกัน 2-4 วินาที
        
        FlipSprite(partner.transform.position.x); // หันหน้าเข้าหากัน
        if (!partner.isChatting) partner.StartChatting(this); // บังคับให้เพื่อนคุยด้วย
    }

    void HandleChatting()
    {
        if (animator != null) animator.SetBool("isMoving", false); // หยุดเดินและยืนคุย
        chatTimer -= Time.deltaTime;

        if (chatTimer <= 0 || chatPartner == null || isAlerted)
        {
            isChatting = false;
            chatPartner = null;
            wanderTimer = 0; // เลิกคุยแล้วเดินไปที่อื่นต่อ
        }
    }

    // 🟢 เอฟเฟกต์โชว์เครื่องหมายตกใจ
    IEnumerator ShowAlertIcon()
    {
        if (alertIcon != null)
        {
            alertIcon.SetActive(true);
            yield return new WaitForSeconds(1.0f); // โชว์ค้างไว้ 1 วินาที
            alertIcon.SetActive(false);
        }
    }

    void HandleMeleeHostile(float distance)
    {
        if (isRetreating || isPreparingMelee || isLunging) 
        {
            // 🟢 ถ้ากำลังง้างดาบ, พุ่ง หรือกระเด็น ให้ปิดท่าเดิน
            if (animator != null) animator.SetBool("isMoving", false);
            return; 
        }

        if (distance <= detectionRange)
        {
            if (distance <= lungeRange && Time.time >= nextMeleeTime)
            {
                if (animator != null) animator.SetBool("isMoving", false); // หยุดเดินเตรียมฟัน
                StartCoroutine(MeleeLungeRoutine()); 
            }
            else if (distance > lungeRange - 0.2f)
            {
                MoveTowardsPlayer();
                if (animator != null) animator.SetBool("isMoving", true); // 🟢 เปิดท่าเดิน
            }
            else 
            {
                if (animator != null) animator.SetBool("isMoving", false); // ยืนนิ่งๆ
            }
        }
        else 
        {
            if (animator != null) animator.SetBool("isMoving", false); // อยู่นอกระยะ ยืนนิ่ง
        }
    }
    
    IEnumerator MeleeLungeRoutine()
    {
        isPreparingMelee = true;

        // 🟢 ลบโค้ดเปลี่ยนสีตัวเหลืองออกแล้ว

        float lockedDirX = Mathf.Sign(player.position.x - transform.position.x);
        Vector2 windupTarget = new Vector2(transform.position.x - (lockedDirX * 0.5f), transform.position.y);
        
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

        // 🟢 อัปเกรด: คํานวณระยะพุ่ง โดย "เว้นระยะห่าง 1.0 หน่วย" หน้าผู้เล่น จะได้ไม่ทะลุไปข้างหลัง!
        float distToPlayer = Mathf.Abs(player.position.x - transform.position.x);
        float dashDistance = Mathf.Clamp(distToPlayer - 1.0f, 0.1f, lungeRange * 1.5f); 
        
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

    void HandleRangedHostile(float distance)
    {
        if (distance <= detectionRange)
        {
            if (distance < safeDistance)
            {
                MoveAwayFromPlayer(); // เข้าใกล้ไป ถอยหลังตั้งหลัก
            }
            else if (distance > safeDistance + 1f)
            {
                MoveTowardsPlayer(); // ไกลไป ขยับเข้าหา
            }

            // ยิงปืน
            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
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

            // 🟢 Game Feel: ระบบเอียงตัว (Tilt) ซ้ายขวาตามทิศทางที่บินอยู่
            float targetTilt = 0f;
            if (Mathf.Abs(flyVelocity.x) > 0.5f) // ถ้าบินเร็วระดับนึง ค่อยเอียง
            {
                // บินไปขวา เอียงตัวไปข้างหลัง (ลบ) / บินไปซ้าย เอียงตัวไปข้างหลัง (บวก)
                targetTilt = -Mathf.Sign(flyVelocity.x) * tiltAngle; 
            }
            
            // หมุนแกน Z อย่างนุ่มนวล
            float currentZ = transform.rotation.eulerAngles.z;
            if (currentZ > 180f) currentZ -= 360f; // แปลงมุมไม่ให้มั่ว
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
        flyVelocity = Vector2.zero; // เบรกกลางอากาศดังเอี๊ยด!

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color origColor = sr != null ? sr.color : Color.white;

        // 🟢 Game Feel 1: Telegraphing (เตือนก่อนยิง)
        // เปลี่ยนเป็นสีแดงกระพริบ เพื่อให้ผู้เล่นรู้ตัวว่า "มันจะยิงแล้วนะ!"
        if (sr != null) sr.color = Color.red; 
        
        // หยุดชาร์จพลังกลางอากาศแปปนึง (ปรับเวลาได้ตามความยากที่อยากได้)
        yield return new WaitForSeconds(0.3f); 

        // สั่งยิง (ฟังก์ชัน Shoot เดิมที่คุณมีอยู่แล้ว)
        Shoot();
        
        // 🟢 Game Feel 2: Recoil (แรงถีบกระเด็นถอยหลัง)
        if (sr != null) sr.color = origColor; // คืนสีเดิม
        Vector2 recoilDir = (transform.position - player.position).normalized;
        
        float recoilTime = 0.15f;
        float elapsed = 0f;
        Vector2 startPos = transform.position;
        Vector2 recoilPos = startPos + (recoilDir * droneRecoilForce); // ตำแหน่งที่กระเด็นไป

        // อนิเมชันกระเด็นถอยหลังแบบรวดเร็ว
        while(elapsed < recoilTime)
        {
            transform.position = Vector2.Lerp(startPos, recoilPos, elapsed / recoilTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // รีเซ็ตคูลดาวน์และกลับไปโหมดบินตามปกติ
        nextFireTime = Time.time + fireRate;
        isPreparingToShoot = false;
    }

    void HandleBigChaser()
    {
        if (isChasing)
        {
            MoveTowardsPlayer(); // ไล่ตามผู้เล่นตรงๆ ตลอดกาล
        }
    }

    // --- Helper Methods ---

    void MoveTowardsPlayer()
    {
        Vector2 targetPos = player.position;
        
        // 🟢 ถ้าไม่ใช่ตัวบิน ให้บังคับล็อคความสูงแกน Y ไว้ที่พื้นเสมอ
        if (type != EnemyType.FlyingHostile) 
        {
            targetPos.y = transform.position.y;
        }

        transform.position = Vector2.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
        FlipSprite(targetPos.x);
    }

    void MoveAwayFromPlayer()
    {
        if (type != EnemyType.FlyingHostile)
        {
            // 🟢 ล็อคให้วิ่งหนีเฉพาะซ้าย-ขวา (แกน X) เท่านั้น ป้องกันบั๊กพยายามบินหนีหรือมุดดิน
            float dirX = Mathf.Sign(transform.position.x - player.position.x);
            Vector2 targetPos = new Vector2(transform.position.x + (dirX * 5f), transform.position.y);
            
            // วิ่งหนีเร็วขึ้นนิดนึงตอนตกใจ
            transform.position = Vector2.MoveTowards(transform.position, targetPos, (speed * 1.2f) * Time.deltaTime);
            FlipSprite(targetPos.x);
        }
        else
        {
            // ถ้าเป็นตัวบิน ให้บินหนีได้ทุกทิศทางเหมือนเดิม
            Vector2 direction = (transform.position - player.position).normalized;
            Vector2 targetPos = (Vector2)transform.position + (direction * 5f);
            
            transform.position = Vector2.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            FlipSprite(targetPos.x);
        }
    }

    void FlipSprite(float targetX)
    {
        Vector3 scale = transform.localScale;
        
        // ถ้าภาพวาดมากลับด้าน ให้เอา -1 ไปคูณ
        float facingMultiplier = invertFacing ? -1f : 1f; 

        if (targetX > transform.position.x) scale.x = Mathf.Abs(scale.x) * facingMultiplier; 
        else if (targetX < transform.position.x) scale.x = -Mathf.Abs(scale.x) * facingMultiplier; 
        
        transform.localScale = scale;
    }

    void Shoot()
    {
        if (projectilePrefab != null)
        {
            // 🟢 ถ้ามีจุด FirePoint ให้เสกกระสุนตรงนั้น ถ้าไม่มีให้เสกกลางตัวเหมือนเดิม
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
        // BigChaser เป็นอมตะ
        if (type == EnemyType.BigChaser) return;

        currentHealth -= damageAmount;
        Debug.Log(gameObject.name + " Hit! Current Health: " + currentHealth);

        // ระบบ Melee ถอยหลังเมื่อโดนตี
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

        // 👉 เราประกาศตัวแปรตรงนี้ไปแล้ว
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color origColor = sr != null ? sr.color : Color.white;

        Vector2 knockbackDir = (transform.position - player.position).normalized;
        knockbackDir.y = 0; 
        Vector2 targetPos = (Vector2)transform.position + (knockbackDir * 2f); 

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
        
        // 🟢 ลบบรรทัดที่ประกาศตัวแปรซ้ำทิ้งไป แล้วเขียนแค่นี้พอ เพื่อคืนค่าสีเดิม
        if (sr != null) sr.color = origColor;
        
        isRetreating = false;
    }

    void Die()
    {
        Debug.Log(gameObject.name + " Dead!");
        
        // 🟢 อัปเกรดระบบเลือด: ยกตำแหน่งขึ้นแล้วค่อยกระจาย
        if (bloodPrefab != null)
        {
            // ดึงตำแหน่งของศัตรู แล้วบวกความสูงขึ้นไปตามลำตัว/หัว
            Vector3 centerPosition = transform.position + new Vector3(0, bloodHeightOffset, 0);

            int bloodAmount = Random.Range(minBloodSpawns, maxBloodSpawns + 1);
            
            for (int i = 0; i < bloodAmount; i++)
            {
                // สุ่มระยะกระจายตัว (กระจายออกจากจุดกึ่งกลางลำตัวที่เรายกขึ้นมาแล้ว)
                Vector2 randomOffset = new Vector2(
                    Random.Range(-bloodSpread, bloodSpread), 
                    Random.Range(-bloodSpread, bloodSpread)
                );
                
                // ตำแหน่งเกิดเลือด = ตำแหน่งลำตัว + ระยะสุ่มกระจาย
                Vector3 spawnPosition = centerPosition + (Vector3)randomOffset;
                
                Instantiate(bloodPrefab, spawnPosition, Quaternion.identity);
            }
        }

        Destroy(gameObject); 
    }
    
    [Header("Hitbox Settings")]
    public GameObject enemyHitbox; // 🟢 กลับไป Unity แล้วลาก Object กล่องดาบศัตรูมาใส่ช่องนี้นะ

    // ฟังก์ชันนี้จะถูกเรียกตอนดาบเริ่มฟาด
    public void AnimEvent_EnableHitbox()
    {
        if (enemyHitbox != null) enemyHitbox.SetActive(true);
    }

    // ฟังก์ชันนี้จะถูกเรียกตอนฟาดดาบเสร็จแล้ว
    public void AnimEvent_DisableHitbox()
    {
        if (enemyHitbox != null) enemyHitbox.SetActive(false);
    }
    
    // 🟢 วาดเส้นบอกระยะใน Editor จะได้กะระยะ Level Design ได้เป๊ะๆ!
    private void OnDrawGizmosSelected()
    {
        // วาดวงกลมสีเหลือง = ระยะมองเห็น (Detection Range)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // วาดวงกลมสีแดง = ระยะง้างฟัน (Lunge Range)
        if (type == EnemyType.MeleeHostile)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, lungeRange);
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 🟢 เพิ่ม && type != EnemyType.MeleeHostile เข้าไป
            // แปลว่า: ไม่ทำดาเมจถ้าเป็นเป้านิ่ง, ตัวชาวบ้าน(Passive), หรือตัวถือดาบ(Melee)
            if (type == EnemyType.StationaryTarget || type == EnemyType.Passive || type == EnemyType.MeleeHostile) return;

            PlayerController p = collision.gameObject.GetComponent<PlayerController>();
            if (p != null)
            {
                p.TakeDamage(damage);
            }
        }
    }
}