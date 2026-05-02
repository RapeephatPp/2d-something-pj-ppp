using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Settings")]
    public int damage = 1;
    public float lifeTime = 3f;           // อายุของกระสุน (ป้องกันกระสุนลอยค้างเต็มแมพ)
    public GameObject hitEffectPrefab;    // (ใส่หรือไม่ใส่ก็ได้) เอฟเฟกต์ตอนกระสุนกระทบเป้าหมาย

    void Start()
    {
        // สั่งทำลายตัวเองล่วงหน้า ถ้าบินไป 3 วินาทีแล้วไม่ชนอะไรเลย
        Destroy(gameObject, lifeTime);
    }

    // 🟢 ใช้ OnTriggerEnter2D แทน OnCollision เพื่อให้กระสุนทะลุกันเองได้ ไม่เด้งเป็นลูกชิ้น
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ถ้าชนผู้เล่น
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }
            Explode();
        }
        // ถ้าชนฉาก (สมมติว่าฉากหรือพื้นของคุณอยู่ Layer ชื่อ "Ground" หรือ "Obstacle")
        // เปลี่ยนชื่อ "Ground" เป็นชื่อ Layer พื้นของคุณได้เลยนะครับ
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground")) 
        {
            Explode();
        }
    }

    void Explode()
    {
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}