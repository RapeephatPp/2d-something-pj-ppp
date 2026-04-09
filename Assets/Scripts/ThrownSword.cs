using UnityEngine;

public class ThrownSword : MonoBehaviour
{
    [Header("Throw Settings")]
    public float flySpeed = 25f; 
    public float returnSpeed = 35f; // บินกลับต้องไวกว่าปาไป!
    public float embedDepth = 0.5f; 
    public int throwDamage = 15;    // 🟢 ดาเมจตอนปาอัดหน้าศัตรู

    private Rigidbody2D rb;
    private Collider2D col;
    private bool isStuck = false;
    private bool isReturning = false;
    private Transform playerTarget;
    private PlayerController playerController;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public void Initialize(Vector2 throwDirection)
    {
        float angle = Mathf.Atan2(throwDirection.y, throwDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        rb.gravityScale = 0f;
        rb.linearVelocity = throwDirection * flySpeed;
    }

    // 🟢 ฟังก์ชันใหม่: เรียกดาบกลับ
    public void ReturnToPlayer(Transform player)
    {
        isReturning = true;
        isStuck = false;
        playerTarget = player;
        playerController = player.GetComponent<PlayerController>();

        // ตั้งค่าฟิสิกส์ให้บินทะลุกำแพงกลับมาได้
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        col.isTrigger = true; // ทะลุทุกอย่างกลับมาหาเรา
    }

    void Update()
    {
        // 🟢 ลอจิกตอนดาบบินกลับเข้ามือ
        if (isReturning && playerTarget != null)
        {
            // ให้ดาบหมุนชี้หน้าเข้าหาผู้เล่น (เท่ๆ)
            Vector2 dir = (playerTarget.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // บินเข้าหาอย่างรวดเร็ว
            transform.position = Vector2.MoveTowards(transform.position, playerTarget.position, returnSpeed * Time.deltaTime);

            // ถ้าบินมาถึงตัว (ระยะใกล้กว่า 1 Unit) ให้เข้ามือ
            if (Vector2.Distance(transform.position, playerTarget.position) < 1.0f)
            {
                if (playerController != null) playerController.CatchSword();
                Destroy(gameObject); // ทำลายตัวเองทิ้ง
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleImpact(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleImpact(collision.gameObject);
    }

    // 🟢 รวมลอจิกการชนมาไว้ที่เดียว
    private void HandleImpact(GameObject hitObj)
    {
        if (isStuck || isReturning) return;

        // ไม่สนถ้าชนผู้เล่น หรือ อาวุธด้วยกันเอง
        if (hitObj.CompareTag("Player") || hitObj.CompareTag("Weapon")) return;

        // 1. ถ้าชนศัตรู
        if (hitObj.CompareTag("Enemy"))
        {
            EnemyBehavior enemy = hitObj.GetComponent<EnemyBehavior>();
            if (enemy != null) enemy.TakeDamage(throwDamage);
            
            FallToGround(); // ชนศัตรูเสร็จ ดาบต้องกระเด้งตกพื้น
            return;
        }

        // 2. ถ้าชนกำแพง/พื้น (เช็คจาก Layer)
        if (hitObj.layer == LayerMask.NameToLayer("Ground") || hitObj.layer == LayerMask.NameToLayer("Obstacle"))
        {
            StickToWall();
        }
        else 
        {
            // 3. ถ้าชนอย่างอื่นที่ไม่ได้ระบุไว้ ให้ตกพื้น
            FallToGround();
        }
    }

    private void StickToWall()
    {
        isStuck = true;
        transform.position += (Vector3)rb.linearVelocity.normalized * embedDepth;
        rb.linearVelocity = Vector2.zero; 
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    // 🟢 ฟังก์ชันใหม่: ฟิสิกส์ตอนดาบร่วง
    private void FallToGround()
    {
        isStuck = true; // ป้องกันไม่ให้โดนชนซ้ำๆ จนเด้งมั่ว
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f; // ร่วงเร็วๆ หน่อย จะได้ดูมีน้ำหนัก
        
        // ให้มันกระเด้งถอยหลังนิดๆ แล้วหมุนติ้วๆ ตกลงพื้น
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * -0.3f, 5f); 
        rb.angularVelocity = Random.Range(300f, 700f) * (Random.value > 0.5f ? 1 : -1);
    }
}