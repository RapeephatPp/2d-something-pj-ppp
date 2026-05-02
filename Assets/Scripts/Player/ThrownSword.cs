using UnityEngine;

public class ThrownSword : MonoBehaviour
{
    [Header("Throw Settings")]
    public float flySpeed = 25f; 
    public float returnSpeed = 35f; 
    public float embedDepth = 0.5f; 
    public int throwDamage = 15;    
    public float stunDuration = 0.3f;

    [Header("Audio SFX")]
    public AudioClip hitEnemySound;  // 🟢 เสียงดาบปักเนื้อ/ศัตรู (ฉัวะ!)
    public AudioClip stickWallSound; // 🟢 เสียงดาบปักกำแพง (ฉึก!)
    public AudioClip bounceSound;    // 🟢 เสียงดาบหล่นกระแทกพื้น (เพ้ง/แกร๊ง!)

    private Rigidbody2D rb;
    private Collider2D col;
    private bool isStuck = false;
    private bool isReturning = false;
    private Transform playerTarget;
    private PlayerController playerController;

    private Vector3 baseScale; 

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        baseScale = transform.localScale; 
    }

    void Start()
    {
        SetPlayerCollision(true); 
    }

    private void SetPlayerCollision(bool ignore)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach(GameObject p in players)
        {
            Collider2D[] pCols = p.GetComponents<Collider2D>();
            foreach(Collider2D pc in pCols)
            {
                if (col != null && pc != null)
                {
                    Physics2D.IgnoreCollision(col, pc, ignore);
                }
            }
        }
    }

    public void Initialize(Vector2 throwDirection)
    {
        float angle = Mathf.Atan2(throwDirection.y, throwDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        float flipY = throwDirection.x < 0 ? -Mathf.Abs(baseScale.y) : Mathf.Abs(baseScale.y);
        transform.localScale = new Vector3(baseScale.x, flipY, baseScale.z);

        rb.gravityScale = 0f;
        rb.linearVelocity = throwDirection * flySpeed;
    }

    public void ReturnToPlayer(Transform player)
    {
        isReturning = true;
        isStuck = false;
        playerTarget = player;
        playerController = player.GetComponent<PlayerController>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        col.isTrigger = true; 
        
        gameObject.layer = LayerMask.NameToLayer("Default"); 

        SetPlayerCollision(true);
    }

    void Update()
    {
        if (isReturning && playerTarget != null)
        {
            Vector2 dir = (playerTarget.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            float flipY = dir.x < 0 ? -Mathf.Abs(baseScale.y) : Mathf.Abs(baseScale.y);
            transform.localScale = new Vector3(baseScale.x, flipY, baseScale.z);

            transform.position = Vector2.MoveTowards(transform.position, playerTarget.position, returnSpeed * Time.deltaTime);

            if (Vector2.Distance(transform.position, playerTarget.position) < 1.0f)
            {
                if (playerController != null) playerController.CatchSword();
                Destroy(gameObject); 
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 🟢 ตรวจจับเสียงตอนดาบกลิ้งหล่นกระแทกพื้น!
        // ถ้าดาบหมดสภาพ (กลิ้งอยู่) และเป็น Dynamic ให้เช็คแรงกระแทก
        if (isStuck && !isReturning && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            // ถ้าแรงกระแทก (relativeVelocity) มากกว่า 1 ค่อยส่งเสียง (กันเสียงรัวตอนดาบนอนไถลนิ่งๆ)
            if (collision.relativeVelocity.magnitude > 1f)
            {
                if (AudioManager.Instance != null && bounceSound != null)
                {
                    // 🟢 คำนวณความดังตามความแรงที่ตกกระทบ! ยิ่งตกแรง ยิ่งดัง!
                    float volume = Mathf.Clamp(collision.relativeVelocity.magnitude * 0.1f, 0.1f, 0.8f);
                    AudioManager.Instance.PlaySFX(bounceSound, volume);
                }
            }
        }

        HandleImpact(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {   
        if (collision.isTrigger)
        {
            if (!collision.CompareTag("Enemy") && !collision.CompareTag("Boss")) 
            {
                return; // เมินสิ่งนี้ซะ!
            }
        }
        
        HandleImpact(collision.gameObject);
    }

    private void HandleImpact(GameObject hitObj)
    {
        if (isStuck) return; // ถ้าปักอยู่ ไม่ต้องสนใจอะไร
        if (hitObj.CompareTag("Weapon")) return;

        if (hitObj.CompareTag("Enemy") || hitObj.CompareTag("Boss"))
        {
            if (AudioManager.Instance != null && hitEnemySound != null)
                AudioManager.Instance.PlaySFX(hitEnemySound, 0.8f);

            EnemyBehavior enemy = hitObj.GetComponent<EnemyBehavior>();
            if (enemy != null) 
            {
                enemy.ApplySwordStun(throwDamage, stunDuration);
            }
            
            // 🟢 [อัปเกรด] ถ้าดาบกำลังบินกลับ "ไม่ต้องหล่นพื้น" ให้มันบินทะลุสับศัตรูต่อยันถึงมือเราเลย!
            if (!isReturning) 
            {
                FallToGround(); 
            }
            return;
        }

        // 🟢 ถ้ากำลังบินกลับ แล้วชนกำแพง ให้ทะลุกำแพงกลับมาเลย ไม่ต้องปักใหม่
        if (isReturning) return; 

        if (hitObj.layer == LayerMask.NameToLayer("Ground"))
        {
            StickToWall();
        }
        else 
        {
            if (AudioManager.Instance != null && bounceSound != null)
                AudioManager.Instance.PlaySFX(bounceSound, 1.0f);

            FallToGround();
        }
    }

    private void StickToWall()
    {
        // 🟢 เสียงดาบปักกำแพงอย่างจัง!
        if (AudioManager.Instance != null && stickWallSound != null)
            AudioManager.Instance.PlaySFX(stickWallSound, 1.0f);

        isStuck = true;
        transform.position += transform.right * embedDepth;
        
        rb.linearVelocity = Vector2.zero; 
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        gameObject.layer = LayerMask.NameToLayer("Ground");
        SetPlayerCollision(false);
    }

    private void FallToGround()
    {
        isStuck = true; 
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f; 
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * -0.3f, 5f); 
        rb.angularVelocity = Random.Range(300f, 700f) * (Random.value > 0.5f ? 1 : -1);
        SetPlayerCollision(false);
    }
}