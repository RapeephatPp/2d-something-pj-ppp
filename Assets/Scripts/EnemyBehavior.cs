using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
    public enum EnemyType { Stationary, MeleePatrol, BigChaser }
    public EnemyType type;

    [Header("Settings")]
    public int health = 3; 
    public int damage = 1; 
    public float speed = 2f;
    
    [Header("Patrol Settings (For Melee)")]
    public Transform pointA;
    public Transform pointB;
    private Vector3 targetPoint;

    [Header("Chaser Settings (For Big Monster)")]
    public Vector3 rushDirection = Vector3.right; 
    
    // 🟢 [เพิ่มใหม่] การตั้งค่าระบบรอคำสั่ง
    public bool waitToChase = false; // ติ๊กถูกถ้ายากให้มันยืนรอจนกว่าผู้เล่นจะเหยียบ Trigger
    private bool isChasing = false;  // สถานะการวิ่งปัจจุบัน

    void Start()
    {
        if (type == EnemyType.MeleePatrol && pointA != null && pointB != null)
        {
            targetPoint = pointB.position;
        }

        // 🟢 ถ้าไม่ได้ตั้งให้รอ ก็สั่งให้มันวิ่งตั้งแต่เริ่มเกมเลย
        if (!waitToChase)
        {
            isChasing = true;
        }
    }

    void Update()
    {
        if (type == EnemyType.MeleePatrol)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPoint) < 0.1f)
            {
                targetPoint = targetPoint == pointA.position ? pointB.position : pointA.position;
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;
            }
        }
        else if (type == EnemyType.BigChaser)
        {
            // 🟢 [อัปเดต] เช็คก่อนว่าได้รับอนุญาตให้วิ่ง (isChasing) หรือยัง
            if (isChasing) 
            {
                if (pointB != null)
                {
                    transform.position = Vector3.MoveTowards(transform.position, pointB.position, speed * Time.deltaTime);
                }
                else 
                {
                    transform.position += rushDirection * speed * Time.deltaTime;
                }
            }
        }
    }

    // 🟢 [เพิ่มใหม่] ฟังก์ชันสำหรับให้ Trigger ภายนอกส่งคำสั่งมาปลุก
    public void TriggerChase()
    {
        isChasing = true;
    }

    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        Debug.Log(gameObject.name + " Hit Current Health: " + health);

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " Dead!");
        Destroy(gameObject); 
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }
        }
    }
}