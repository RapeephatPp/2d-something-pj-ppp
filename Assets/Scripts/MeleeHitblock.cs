using UnityEngine;

public class MeleeHitblock : MonoBehaviour
{
    [Header("Combat Stats")]
    public int damage = 10;
    public float knockbackForce = 5f; // แรงผลักศัตรูกระเด็น

    [Header("Game Feel")]
    public float hitstopDuration = 0.05f; // ระยะเวลาหยุดเฟรม (ยิ่งนานยิ่งรู้สึกว่าตีแรง)
    public float hitstopScale = 0.0f;     // ความช้าตอนชน (0 คือหยุดนิ่งเลย)

    private PlayerController player;

    private void Start()
    {
        // ดึงคอมโพเนนต์ PlayerController จากตัวละครหลัก (เพราะกล่อง Hitbox มักเป็นลูกของ Player)
        player = GetComponentInParent<PlayerController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. เช็คว่าแท็กของสิ่งที่ชนคือ "Enemy" ใช่หรือไม่
        if (other.CompareTag("Enemy"))
        {
            // 2. เรียกให้ศัตรูลดเลือด (อ้างอิงไปที่สคริปต์ศัตรูของคุณ)
            EnemyBehavior enemy = other.GetComponent<EnemyBehavior>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage); 
            }

            // 3. ฟิสิกส์: ดันศัตรูให้กระเด็นถอยหลัง (Knockback)
            Rigidbody2D enemyRb = other.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                // คำนวณหาทิศทางว่าศัตรูอยู่ซ้ายหรือขวาของดาบเรา
                float pushDirection = Mathf.Sign(other.transform.position.x - player.transform.position.x);
                
                // สร้างเวกเตอร์แรงผลัก: ถอยหลัง (X) และลอยขึ้นนิดๆ (Y) ให้ดูมีน้ำหนัก
                Vector2 knockback = new Vector2(pushDirection * knockbackForce, knockbackForce * 0.3f);
                
                enemyRb.linearVelocity = Vector2.zero; // ล้างแรงเก่าของศัตรูทิ้งก่อน
                enemyRb.AddForce(knockback, ForceMode2D.Impulse); // กระแทกเปรี้ยง!
            }

            // 4. Game Feel: สั่งหยุดเวลาชั่วคราวและสั่นกล้อง
            if (player != null)
            {
                // สั่งรัน Coroutine HitStop (ต้องแก้ใน PlayerController ให้เป็น public ด้วยนะ)
                player.StartCoroutine(player.HitStopRoutine(hitstopDuration, hitstopScale));
            }

            if (CameraShake.Instance != null)
            {
                // ตีโดนปุ๊บ กล้องสั่นปั๊บ
                CameraShake.Instance.StartCoroutine(CameraShake.Instance.Shake(0.15f, 0.1f));
            }

            // 🟢 จุดเสริมอนาคต: ถ้ามี Particle เลือดหรือแสงดาบ ก็ Instantiate ตรงนี้ได้เลย!
        }
    }
}