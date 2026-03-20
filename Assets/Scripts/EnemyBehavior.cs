using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
    public enum EnemyType { Stationary, MeleePatrol, BigChaser }
    public EnemyType type;

    [Header("Settings")]
    public int health = 3; // เลือดของมอนสเตอร์
    public int damage = 1; // ดาเมจที่ตีผู้เล่น
    public float speed = 2f;
    
    [Header("Patrol Settings (For Melee)")]
    public Transform pointA;
    public Transform pointB;
    private Vector3 targetPoint;

    [Header("Chaser Settings (For Big Monster)")]
    public Vector3 rushDirection = Vector3.right; 

    void Start()
    {
        if (type == EnemyType.MeleePatrol && pointA != null && pointB != null)
        {
            targetPoint = pointB.position;
        }
    }

    void Update()
    {
        if (type == EnemyType.MeleePatrol)
        {
            // โค้ดเดินไปมา (เหมือนเดิม)
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
            // [แก้ใหม่] ให้วิ่งไปหาจุด B แล้วหยุด
            if (pointB != null)
            {
                transform.position = Vector3.MoveTowards(transform.position, pointB.position, speed * Time.deltaTime);
                
                // ถ้าอยากให้มันทำอะไรตอนพุ่งชนกำแพง/จุดหมาย (เช่น กล้องสั่น) ใส่เพิ่มตรงนี้ได้
                if (Vector3.Distance(transform.position, pointB.position) < 0.1f)
                {
                    // ถึงจุด B แล้ว จะหยุดนิ่งๆ
                }
            }
            else 
            {
                // ถ้าลืมใส่จุด B ใน Inspector มันจะพุ่งไปข้างหน้าเรื่อยๆ เหมือนเดิม
                transform.position += rushDirection * speed * Time.deltaTime;
            }
        }
    }

    // --- ฟังก์ชันรับดาเมจจากผู้เล่น ---
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
        Destroy(gameObject); // ทำลายศัตรูทิ้ง
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<PlayerController>().TakeDamage(damage);
        }
    }
}