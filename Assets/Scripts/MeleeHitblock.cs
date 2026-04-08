using UnityEngine;
using System.Collections.Generic;

public class MeleeHitbox : MonoBehaviour
{
    [Header("Hitbox Settings")]
    public int damage = 3;
    public float hitStopDuration = 0.1f;
    public float camShakeMagnitude = 0.2f;

    private HashSet<Collider2D> hitEnemies = new HashSet<Collider2D>();
    private PlayerController player;

    void Awake()
    {
        player = GetComponentInParent<PlayerController>();
        
        // 🟢 ซ่อนกล่องแดงไว้ก่อนตอนเริ่มเกม โค้ด Player จะเป็นคนสั่งเปิดเอง
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        // ล้างความจำศัตรูทุกครั้งที่กล่องแดงโผล่มาใหม่ (เริ่มฟันฮิตใหม่)
        hitEnemies.Clear();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ถ้ากล่องแดงไปโดนศัตรู และศัตรูตัวนั้นยังไม่เคยโดนดาเมจในฮิตนี้
        if (collision.CompareTag("Enemy") && !hitEnemies.Contains(collision))
        {
            hitEnemies.Add(collision); // จำไว้ว่าตัวนี้โดนฟันแล้ว
            
            EnemyBehavior enemy = collision.GetComponent<EnemyBehavior>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                
                // สั่งหยุดเวลาและสั่นกล้อง
                if (player != null) player.TriggerHitStop(hitStopDuration);
                if (CameraShake.Instance != null) StartCoroutine(CameraShake.Instance.Shake(0.2f, camShakeMagnitude));
            }
        }
    }
}