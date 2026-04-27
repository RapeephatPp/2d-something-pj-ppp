using UnityEngine;
using System.Collections;

public class ChaserBoss : MonoBehaviour
{
    [Header("Boss Stats")]
    public int maxHp = 4;
    private int currentHp;
    public float moveSpeed = 5f;

    [Header("Game Feel - Hit Stop & Stun")]
    public float hitStopDuration = 0.15f; // เวลาที่เกมจะค้างไปเสี้ยววิ (สะใจจัดๆ)
    public float stunDuration = 1.5f;     // เวลาที่บอสชะงักหลังโดนหินทับ

    private bool isDead = false;
    private bool isStunned = false;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHp = maxHp;
    }

    void FixedUpdate()
    {
        if (isDead || isStunned) 
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // วิ่งไล่ล่าไปทางขวาเรื่อยๆ อย่างไร้ความปราณี
        rb.linearVelocity = new Vector2(moveSpeed, rb.linearVelocity.y);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. โดนหินร่วงทับ!
        if (collision.gameObject.GetComponent<FallingRock>())
        {
            FallingRock rock = collision.gameObject.GetComponent<FallingRock>();
            TakeDamage(rock.damageToBoss, true);
            Destroy(rock.gameObject); // บดขยี้หินทิ้ง
        }
        // 2. โดนผู้เล่นปาดาบใส่ตรงๆ (ปาอัดหน้า)
        else if (collision.CompareTag("PlayerAttack") || collision.gameObject.name.Contains("Sword")) 
        {
            // เด้งดาบออก เลือดไม่ลด! สอนผู้เล่นว่า "ปาใส่หินสิโว้ย!"
            TakeDamage(0, false); 
            // TODO: เรียกเสียง SFX เหล็กกระทบเหล็ก "ติ๊ง!" (Deflect)
        }
        // 3. วิ่งทันผู้เล่น! งับ!
        else if (collision.CompareTag("Player"))
        {
            DevourPlayer(collision.gameObject);
        }
    }

    public void TakeDamage(int damage, bool isHeavyHit)
    {
        if (isDead) return;

        currentHp -= damage;

        if (isHeavyHit) // โดนหินทับ
        {
            StartCoroutine(HitStopAndStun());
            
            // --- GAME FEEL: สั่นกล้องรุนแรง! ---
            if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(6f, 0.1f);
            
            // TODO: ใส่เอฟเฟกต์ BloodFadeEffect เลือดสาดตรงนี้
        }
        else // โดนดาบปาใส่ตรงๆ
        {
            // สั่นกล้องเบาๆ เหมือนตีไม่เข้า
            if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.15f, 0.1f);
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    IEnumerator HitStopAndStun()
    {
        isStunned = true;
        
        // --- GAME FEEL: HIT STOP (หยุดเวลา) ---
        Time.timeScale = 0.05f; // ทำให้เกมสโลว์โมชั่นสุดๆ 
        yield return new WaitForSecondsRealtime(hitStopDuration); // ใช้ Realtime รอ
        Time.timeScale = 1f; // คืนค่าความเร็วปกติ!

        // บอสชะงักให้ผู้เล่นมีเวลาวิ่งหนีต่อ
        yield return new WaitForSeconds(stunDuration);
        isStunned = false;
    }

    void DevourPlayer(GameObject player)
    {
        if (isDead || isStunned) return;

        // --- ฉากจบสายดาร์ก ---
        Debug.Log("Game Over: บอสงับผู้เล่นแล้ว!");
        player.SetActive(false); // ปิดผู้เล่นไปเลย
        if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(8f, 0.5f);
        // TODO: เรียก UIManager โชว์หน้า Game Over
    }

    void Die()
    {
        isDead = true;
        // --- GAME FEEL: บอสตาย ---
        if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(10f, 0.5f);
        Debug.Log("Boss Defeated! ทางเปิดออก!");
        
        // TODO: เรียก Particle ชิ้นส่วนกระจุยกระจาย (SpreadParts ของคุณ)
        
        Destroy(gameObject, 0.2f);
    }
}