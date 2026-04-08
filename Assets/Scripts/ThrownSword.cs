using UnityEngine;

public class ThrownSword : MonoBehaviour
{
    public float flySpeed = 25f; 
    
    [Tooltip("ความลึกตอนที่ดาบเสียบเข้าไปในกำแพง (ยิ่งเยอะยิ่งปักมิดด้าม)")]
    public float embedDepth = 0.5f; 
    
    private Rigidbody2D rb;
    private bool isStuck = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // 🟢 เปลี่ยนมารับค่าเป็น Vector2 (ทิศทางไปยังเมาส์)
    public void Initialize(Vector2 throwDirection)
    {
        // คำนวณองศาหมุนให้ดาบชี้ไปทางเดียวกับที่พุ่ง
        float angle = Mathf.Atan2(throwDirection.y, throwDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // ปิดแรงโน้มถ่วงชั่วคราวตอนกำลังพุ่ง
        rb.gravityScale = 0f;
        rb.linearVelocity = throwDirection * flySpeed;
    }

    // 🟢 เช็คการชนกำแพง/พื้น (ใช้ Collider แบบ Physics ปกติ ไม่ต้องติ๊ก IsTrigger)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isStuck) return;

        // ไม่ชนผู้เล่น ศัตรู หรืออาวุธ
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("Weapon")) return;

        // ถ้าชนกำแพง หรือพื้น (Layer = Ground หรือ Obstacle) ให้ปักเลย!
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            StickToWall();
        }
        else 
        {
            // ถ้าชนอย่างอื่นที่ปักไม่ได้ ให้ดาบร่วงลงพื้น!
            FallToGround();
        }
    }

    // เผื่อเผลอตั้งดาบเป็น Is Trigger ไว้ ก็ให้มันปักเหมือนกัน
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isStuck) return;

        if (collision.CompareTag("Player") || collision.CompareTag("Enemy") || collision.CompareTag("Weapon")) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
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
        
        // 🟢 ดันดาบให้จมเข้าไปในกำแพงตามทิศทางที่พุ่งมา (embedDepth)
        transform.position += (Vector3)rb.linearVelocity.normalized * embedDepth;

        // ล็อคดาบให้ติดแน่น
        rb.linearVelocity = Vector2.zero; 
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // เปลี่ยนตัวเองให้เป็น Obstacle เผื่อใช้บังเลเซอร์
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (obstacleLayer != -1) gameObject.layer = obstacleLayer; 
    }

    private void FallToGround()
    {
        // เปิดแรงโน้มถ่วงให้ร่วงตกตามธรรมชาติ
        rb.gravityScale = 3f; 
        rb.linearVelocity = new Vector2(0f, -5f); 
    }
}