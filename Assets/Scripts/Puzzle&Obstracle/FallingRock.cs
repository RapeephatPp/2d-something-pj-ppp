using UnityEngine;
using System.Collections;

public class FallingRock : MonoBehaviour
{
    [Header("Rock Settings")]
    public float shakeDuration = 0.4f;
    public float shakeAmount = 0.1f;
    public int damageToBoss = 1;

    private Rigidbody2D rb;
    private bool isTriggered = false;
    private Vector3 originalPos;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // ให้หินลอยอยู่บนเพดานก่อน (ระวังอย่าให้มี Gravity สวนทางนะ)
        rb.isKinematic = true; 
        originalPos = transform.position;
    }

    // สมมติว่าดาบที่ปามา (ThrownSword) มี Tag ว่า "PlayerAttack" หรือคุณใช้ Layer Collision เช็คเอา
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ถ้าโดนอาวุธของผู้เล่น และยังไม่เคยถูกทริกเกอร์
        if (!isTriggered && (collision.CompareTag("PlayerAttack") || collision.gameObject.name.Contains("Sword")))
        {
            StartCoroutine(ShakeAndFall());
            
            // TODO: ถ้ามีสคริปต์ HitSparkJuice เรียกใช้ตรงนี้ได้เลยให้มีประกายไฟตอนปาโดนหิน!
        }
    }

    IEnumerator ShakeAndFall()
    {
        isTriggered = true;
        
        // --- GAME FEEL: สั่นเตือนก่อนร่วง (Anticipation) ---
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = originalPos.x + Random.Range(-shakeAmount, shakeAmount);
            transform.position = new Vector3(x, originalPos.y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos; // คืนตำแหน่งเดิมก่อนปล่อยตก

        // ปล่อยร่วงลงมา (ต้องมีแรงโน้มถ่วงใน Rigidbody2D ด้วยนะ)
        rb.isKinematic = false; 
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ถ้าหินตกลงพื้นเฉยๆ (หลบอสไม่โดน)
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // สั่นจอเบาๆ ตอนหินกระทบพื้น
            if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(2f, 0.2f);
            // TODO: เพิ่ม Instantiate Particle ฝุ่นกระจายตรงนี้
            Destroy(gameObject);
        }
    }
}