using UnityEngine;

public class ThrownSword : MonoBehaviour
{
    [Header("Throw Settings")]
    public float flySpeed = 25f; 
    public float returnSpeed = 35f; 
    public float embedDepth = 0.5f; 
    public int throwDamage = 15;    
    public float stunDuration = 0.3f;

    private Rigidbody2D rb;
    private Collider2D col;
    private bool isStuck = false;
    private bool isReturning = false;
    private Transform playerTarget;
    private PlayerController playerController;

    // 🟢 เพิ่มตัวแปรสำหรับจำขนาดดั้งเดิมของดาบ
    private Vector3 baseScale; 

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        
        // 🟢 จำขนาดที่คุณตั้งไว้ใน Inspector ตั้งแต่ตอนเกิด
        baseScale = transform.localScale; 
    }

    void Start()
    {
        // ตอนดาบเพิ่งเกิด สั่งให้ทะลุผู้เล่นไปก่อน จะได้ไม่กระเด็นชนกันเอง
        SetPlayerCollision(true); 
    }

    // 🟢 สร้างฟังก์ชันเปิด/ปิด การทะลุผู้เล่นแบบสั่งได้!
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

        // 🟢 สลับด้านโดยใช้ขนาดดั้งเดิมของคุณ (ไม่กลายเป็นดาบจิ๋วแล้ว!)
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

        // 🟢 ตอนเรียกดาบกลับ สั่งให้ทะลุผู้เล่นอีกรอบ จะได้ไม่ไปกระแทกหน้าตัวเอง
        SetPlayerCollision(true);
    }

    void Update()
    {
        if (isReturning && playerTarget != null)
        {
            Vector2 dir = (playerTarget.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // 🟢 สลับด้านตอนบินกลับ (คงขนาดเดิมไว้)
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
        if (isStuck || isReturning) return;
        if (hitObj.CompareTag("Weapon")) return;

        if (hitObj.CompareTag("Enemy"))
        {
            EnemyBehavior enemy = hitObj.GetComponent<EnemyBehavior>();
            if (enemy != null) 
            {
                // 🟢 เปลี่ยนมาเรียกใช้ฟังก์ชันทำสตันแทน TakeDamage() เดิม! 
                // ทำให้ศัตรูไม่ปลิวกระเด็นอีกต่อไป
                enemy.ApplySwordStun(throwDamage, stunDuration);
            }
            FallToGround(); 
            return;
        }

        if (hitObj.layer == LayerMask.NameToLayer("Ground"))
        {
            StickToWall();
        }
        else 
        {
            FallToGround();
        }
    }

    private void StickToWall()
    {
        isStuck = true;
        transform.position += transform.right * embedDepth;
        
        rb.linearVelocity = Vector2.zero; 
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        gameObject.layer = LayerMask.NameToLayer("Ground");

        // 🟢 พระเอกอยู่ตรงนี้! ยกเลิกการทะลุผู้เล่น ทำให้เราเหยียบดาบที่ปักกำแพงได้แล้ว!
        SetPlayerCollision(false);
    }

    private void FallToGround()
    {
        isStuck = true; 
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f; 
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * -0.3f, 5f); 
        rb.angularVelocity = Random.Range(300f, 700f) * (Random.value > 0.5f ? 1 : -1);

        // ตอนดาบตกพื้น ก็ให้เหยียบได้หรือชนได้ตามปกติ
        SetPlayerCollision(false);
    }
}