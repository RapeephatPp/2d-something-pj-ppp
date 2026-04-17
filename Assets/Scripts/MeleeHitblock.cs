using UnityEngine;
using System.Collections.Generic;

public class MeleeHitbox : MonoBehaviour
{
    [Header("Hitbox Settings")]
    public int damage = 3;
    public float hitStopDuration = 0.1f;
    public float camShakeMagnitude = 0.2f;

    [Header("Game Feel (Juice)")]
    public GameObject hitSparkPrefab; // 🟢 ลาก Prefab เอฟเฟกต์ประกายไฟ หรือ เลือด มาใส่ช่องนี้

    [Header("Parry System (ตีปัดการโจมตี)")]
    public bool canDeflectProjectiles = true; // 🟢 เปิด/ปิด ความสามารถในการฟันกระสุนศัตรูทิ้ง!

    private HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();
    private PlayerController player;

    void Awake()
    {
        player = GetComponentInParent<PlayerController>();
        
    }

    void OnEnable()
    {
        // ล้างความจำเป้าหมายทุกครั้งที่กล่องแดงโผล่มาใหม่ (เริ่มฟันฮิตใหม่)
        hitTargets.Clear();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 🟢 1. ถ้าฟันโดนศัตรู
        if (collision.CompareTag("Enemy") && !hitTargets.Contains(collision))
        {
            hitTargets.Add(collision); 
            
            EnemyBehavior enemy = collision.GetComponent<EnemyBehavior>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                
                // เสกเอฟเฟกต์ประกายไฟตรงจุดที่สัมผัสกัน
                SpawnHitSpark(collision);
                
                // สั่งหยุดเวลาและสั่นกล้อง
                if (player != null) player.TriggerHitStop(hitStopDuration);
                if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.2f, camShakeMagnitude);
            }
        }

        // 🟢 2. ถ้าระบบ Parry ทำงาน และฟันไปโดนกระสุนศัตรู (ต้องตั้ง Tag กระสุนเป็น "EnemyProjectile")
        if (canDeflectProjectiles && collision.CompareTag("EnemyProjectile") && !hitTargets.Contains(collision))
        {
            hitTargets.Add(collision); // ป้องกันบั๊กตีโดนซ้ำ

            // เสกประกายไฟตรงกระสุนที่โดนปัด
            SpawnHitSpark(collision);

            // ทำลายกระสุนทิ้งไปเลย! (ผู้เล่นรอดตัว)
            Destroy(collision.gameObject);

            // สั่นกล้องและหยุดเวลาสั้นๆ ให้รู้สึกสะใจที่ปัดได้ทัน
            if (player != null) player.TriggerHitStop(hitStopDuration * 0.5f);
            if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.15f, 0.1f);
        }
    }

    // ฟังก์ชันช่วยเสกเอฟเฟกต์ให้อยู่ตรงขอบที่ชนกันพอดี
    void SpawnHitSpark(Collider2D targetCollider)
    {
        if (hitSparkPrefab != null)
        {
            // หาจุดที่ใกล้ที่สุดระหว่างจุดศูนย์กลางดาบ กับ ขอบกล่องของเป้าหมาย
            Vector2 contactPoint = targetCollider.ClosestPoint(transform.position);
            Instantiate(hitSparkPrefab, contactPoint, Quaternion.identity);
        }
    }
}