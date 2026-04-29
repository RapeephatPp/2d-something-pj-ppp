using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform posA, posB;
    public float speed = 3f;
    private Vector3 targetPos;
    
    // เอาไว้จำตัวผู้เล่น และตำแหน่งล่าสุดของแพลตฟอร์ม
    private Transform playerTransform;
    private Vector3 lastPlatformPosition;

    void Start() 
    { 
        targetPos = posB.position; 
        lastPlatformPosition = transform.position;
    }

    void Update()
    {
        // 1. ขยับแพลตฟอร์ม
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
        
        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            targetPos = targetPos == posA.position ? posB.position : posA.position;
        }
    }

    void LateUpdate()
    {
        // 2. ถ้ามีผู้เล่นยืนอยู่ ให้ดันผู้เล่นตามระยะที่แพลตฟอร์มขยับ (ไม่ใช้ SetParent แล้วตัวจะไม่ยืด)
        if (playerTransform != null)
        {
            Vector3 deltaMovement = transform.position - lastPlatformPosition;
            playerTransform.position += deltaMovement;
        }
        
        // อัปเดตตำแหน่งล่าสุดไว้ใช้เฟรมถัดไป
        lastPlatformPosition = transform.position;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            playerTransform = collision.transform; // จำว่าผู้เล่นเหยียบแล้ว
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            playerTransform = null; // ผู้เล่นกระโดดออกไปแล้ว
    }
}