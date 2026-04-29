using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class CrumblingPlatform : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("เวลาหน่วงหลังจากผู้เล่นเหยียบ ก่อนที่พื้นจะพัง")]
    public float breakDelay = 0.5f; 
    
    [Tooltip("เวลาในการสร้างพื้นกลับมาใหม่ (ใส่ 0 ถ้าพังแล้วพังเลย)")]
    public float respawnTime = 3f;  

    [Header("Juice / Game Feel")]
    [Tooltip("ระยะความกว้างของการสั่น ยิ่งเยอะยิ่งแกว่งแรง")]
    public float shakeIntensity = 0.05f;
    [Tooltip("ความเร็วในการสั่นซ้าย-ขวา ยิ่งเยอะยิ่งสั่นรัว")]
    public float shakeSpeed = 50f;
    public ParticleSystem breakParticles; 

    private bool isSteppedOn = false;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D coll;
    private Vector3 originalPosition;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        coll = GetComponent<BoxCollider2D>();
        originalPosition = transform.position;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isSteppedOn)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // เช็คว่าเหยียบจากด้านบน
                if (contact.normal.y < -0.5f) 
                {
                    StartCoroutine(BreakSequence());
                    break;
                }
            }
        }
    }

    IEnumerator BreakSequence()
    {
        isSteppedOn = true;

        // --- อัปเกรด Juice: สั่นรัวๆ แบบโครงสร้างจะพัง ---
        float timer = 0;
        while (timer < breakDelay)
        {
            timer += Time.deltaTime;
            
            // ใช้ Mathf.Sin เพื่อให้มันแกว่งซ้ายขวาอย่างรวดเร็วและสมูท (Mechanical Vibration)
            float offsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
            
            // สุ่มสั่นขึ้น-ลงเล็กน้อยมากๆ เพื่อให้ดูไม่แข็งทื่อเกินไป (Random Jitter)
            float offsetY = Random.Range(-shakeIntensity, shakeIntensity) * 0.3f;

            // อัปเดตตำแหน่ง
            transform.position = originalPosition + new Vector3(offsetX, offsetY, 0);
            
            yield return null; 
        }

        // จัดตำแหน่งกลับที่เดิมก่อนพัง
        transform.position = originalPosition;

        // พังพื้น!
        BreakPlatform();
    }

    void BreakPlatform()
    {
        if (breakParticles != null) 
        {
            Instantiate(breakParticles, transform.position, Quaternion.identity);
        }

        // ปิดการมองเห็นและการชน
        spriteRenderer.enabled = false;
        coll.enabled = false;

        if (respawnTime > 0)
        {
            StartCoroutine(RespawnSequence());
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    IEnumerator RespawnSequence()
    {
        yield return new WaitForSeconds(respawnTime);
        
        spriteRenderer.enabled = true;
        coll.enabled = true;
        isSteppedOn = false;
    }
}