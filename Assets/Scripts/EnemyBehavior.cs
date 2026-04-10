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
    
    private Transform safeZone;               // จุดเกิด/จุดหนีกลับ
    private Vector2 wanderTarget;
    private float wanderTimer;
    private bool isFleeingToSafeZone = false;
    private bool hasRolledFleeChance = false; // เช็คเพื่อไม่ให้ทอยเต๋าซ้ำทุกเฟรม

    [Header("Combat Settings")]
    public GameObject projectilePrefab; // กระสุนสำหรับ Range/Fly
    public float fireRate = 1.5f;
    private float nextFireTime;
    
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
    
    [Header("Blood Splatter Settings")]
    public GameObject bloodPrefab;      // Prefab เลือดตอนตาย
    public int minBloodSpawns = 3;      // จำนวนเลือดขั้นต่ำที่จะกระเด็นออกมา
    public int maxBloodSpawns = 6;      // จำนวนเลือดสูงสุด
    public float bloodSpread = 1.5f;    // รัศมีการกระจายตัวของเลือด (ยิ่งเยอะยิ่งกระจายกว้าง)
    public float bloodHeightOffset = 1.0f;
    
    [Header("Chaser Settings")]
    public bool waitToChase = false;
    private bool isChasing = false;

    void Start()
    {   
        spawnTime = Time.time;
        currentHealth = maxHealth;
        
        // ถ้าไม่ได้ลาก Player มาใส่ ให้หาจาก Tag "Player" อัตโนมัติ
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (type == EnemyType.BigChaser && !waitToChase)
        {
            isChasing = true;
        }
    }

    void Update()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            GameObject activePlayer = GameObject.FindGameObjectWithTag("Player");
            if (activePlayer != null) player = activePlayer.transform;
            else return; // ถ้าหาผู้เล่นไม่เจอเลย ให้หยุดทำงานเฟรมนี้ไปก่อน
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        switch (type)
        {
            case EnemyType.Passive: HandlePassive(distanceToPlayer); break;
            case EnemyType.MeleeHostile: HandleMeleeHostile(distanceToPlayer); break;
            case EnemyType.RangedHostile: HandleRangedHostile(distanceToPlayer); break;
            case EnemyType.FlyingHostile: HandleFlyingHostile(distanceToPlayer); break;
            case EnemyType.StationaryTarget: break;
            case EnemyType.BigChaser: HandleBigChaser(); break;
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
        // 1. ถ้ากำลังวิ่งกลับจุดเซฟ
        if (isFleeingToSafeZone && safeZone != null)
        {
            // 🟢 ระบบ AI ตาไว: เช็คว่าผู้เล่น "ขวางทาง" อยู่ระหว่างตัว NPC กับหลุมหรือไม่?
            float distToPlayerX = player.position.x - transform.position.x;
            float distToSafeX = safeZone.position.x - transform.position.x;
            
            // ถ้าผู้เล่นยืนอยู่ฝั่งเดียวกับหลุม แถมอยู่ใกล้เรามากกว่าหลุม แปลว่าขวางทางเต็มๆ!
            bool isPlayerInWay = (Mathf.Sign(distToPlayerX) == Mathf.Sign(distToSafeX)) && 
                                 (Mathf.Abs(distToPlayerX) < Mathf.Abs(distToSafeX));

            // ถ้าขวางทาง และอยู่ใกล้เกินไป (ระยะ 3 หน่วย)
            if (isPlayerInWay && distance < 3f)
            {
                // เลิกหน้ามืดตามัวกลับหลุมชั่วคราว วิ่งหนีเอาชีวิตรอดไปทิศตรงข้ามแทน!
                MoveAwayFromPlayer();
                return; 
            }

            // ถ้าไม่มีคนขวาง ก็วิ่งกลับหลุมปกติ (ล็อคแกน Y ให้เดินติดพื้นด้วย)
            Vector2 targetPosition = new Vector2(safeZone.position.x, transform.position.y);
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, speed * 1.5f * Time.deltaTime);
            FlipSprite(safeZone.position.x);

            // วิ่งถึงหลุมแล้ว มุดลงดิน (Destroy)
            if (Mathf.Abs(transform.position.x - safeZone.position.x) < 0.2f)
            {
                Destroy(gameObject); 
            }
            return; 
        }

        // 2. ช่วงเวลาตั้งตัวตอนเพิ่งเกิด
        if (Time.time - spawnTime < gracePeriod)
        {
            WanderAroundSafeZone();
            return;
        }

        // ดึงสถานะถือดาบมาจาก PlayerController ของคุณ
        bool playerHasSword = PlayerController.isArmed; 

        // 3. ถ้าเจอผู้เล่นถือดาบ
        if (distance <= detectionRange && playerHasSword)
        {
            // ทอยเต๋า 1 ครั้งว่าจะหนีกลับหลุมไหม
            if (!hasRolledFleeChance)
            {
                hasRolledFleeChance = true; 
                if (Random.value <= fleeToSafeZoneChance && safeZone != null)
                {
                    isFleeingToSafeZone = true;
                    return;
                }
            }
            
            // ถ้าไม่กลับหลุม (หรือไม่มีหลุม) ก็ให้วิ่งหนีไปทางตรงข้าม
            MoveAwayFromPlayer();
        }
        else 
        {
            if (distance > detectionRange)
            {
                hasRolledFleeChance = false; // รีเซ็ตการตัดสินใจ
            }
            
            // 4. เดินเล่นแบบชิลๆ
            WanderAroundSafeZone();
        }
    }

    void WanderAroundSafeZone()
    {
        if (safeZone == null) return;

        wanderTimer -= Time.deltaTime;
        
        // 🟢 เปลี่ยนมาเช็คระยะห่างเฉพาะแกน X ว่าเดินถึงจุดหมายหรือยัง
        if (wanderTimer <= 0 || Mathf.Abs(transform.position.x - wanderTarget.x) < 0.1f)
        {
            // 🟢 แก้ไข: สุ่มเฉพาะซ้าย-ขวา (แกน X) เท่านั้น ไม่สุ่มแกน Y
            float randomX = Random.Range(-wanderRadius, wanderRadius);
            
            // จุดหมายใหม่ = X ของจุดเกิด + ค่าที่สุ่มได้, ส่วน Y ใช้ความสูงเดิมของตัวมันเอง
            wanderTarget = new Vector2(safeZone.position.x + randomX, transform.position.y);
            
            wanderTimer = Random.Range(wanderInterval, wanderInterval + 2f);
        }

        // ค่อยๆ เดินไปซ้ายขวาตามจุดหมาย โดยบังคับให้แกน Y เป็นความสูงระดับเดิมเสมอ
        Vector2 targetPosition = new Vector2(wanderTarget.x, transform.position.y);
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, (speed * 0.5f) * Time.deltaTime);
        
        FlipSprite(wanderTarget.x);
    }

    void HandleMeleeHostile(float distance)
    {
        if (distance <= detectionRange)
        {
            if (isRetreating)
            {
                MoveAwayFromPlayer();
            }
            else
            {
                MoveTowardsPlayer();
            }
        }
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
        yield return new WaitForSeconds(0.6f); 

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
            GameObject bullet = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            Vector2 shootDir = (player.position - transform.position).normalized;
            
            bullet.GetComponent<Rigidbody2D>().linearVelocity = shootDir * 5f; // ความเร็วกระสุน (ปรับเลข 5 ได้ตามใจชอบ)

            // 🟢 หมุนหัวกระสุนให้ชี้ไปหาผู้เล่น
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
        yield return new WaitForSeconds(retreatTime);
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // ถ้าเป็น StationaryTarget หรือ Passive อาจจะไม่ทำดาเมจก็ได้
            if (type == EnemyType.StationaryTarget || type == EnemyType.Passive) return;

            PlayerController p = collision.gameObject.GetComponent<PlayerController>();
            if (p != null)
            {
                p.TakeDamage(damage);
            }
        }
    }
}