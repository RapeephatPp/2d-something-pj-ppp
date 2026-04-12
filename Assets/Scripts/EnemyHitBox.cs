using UnityEngine;
using System.Collections.Generic;

public class EnemyHitbox : MonoBehaviour
{
    [Header("Enemy Hitbox Settings")]
    public int damage = 1;
    public GameObject hitSparkPrefab; // เอฟเฟกต์เวลาศัตรูฟันโดนเรา

    private HashSet<Collider2D> hitPlayers = new HashSet<Collider2D>();

    void Awake()
    {
        gameObject.SetActive(false); // ซ่อนไว้รอ Animation สั่งเปิด
    }

    void OnEnable()
    {
        hitPlayers.Clear(); // ล้างความจำฮิตทุกครั้งที่เริ่มฟัน
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ถ้าดาบศัตรูไปโดนผู้เล่น
        if (collision.CompareTag("Player") && !hitPlayers.Contains(collision))
        {
            hitPlayers.Add(collision);
            
            PlayerController p = collision.GetComponent<PlayerController>();
            if (p != null)
            {
                p.TakeDamage(damage); // ทำดาเมจใส่ผู้เล่น

                if (hitSparkPrefab != null)
                {
                    Vector2 contactPoint = collision.ClosestPoint(transform.position);
                    Instantiate(hitSparkPrefab, contactPoint, Quaternion.identity);
                }
            }
        }
    }
}