using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("ระยะความสูงที่ประตูจะเลื่อนเปิดขึ้นไป")]
    public float openHeight = 3.0f; 
    
    [Tooltip("ความเร็วในการเลื่อนประตู")]
    public float slideSpeed = 5.0f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isPlayerNear = false;

    void Start()
    {
        // จำตำแหน่งตอนปิด (ตำแหน่งเริ่มต้น) ไว้
        closedPosition = transform.position;
        // คำนวณตำแหน่งตอนเปิด (เลื่อนขึ้นไปตามแนวแกน Y)
        openPosition = closedPosition + new Vector3(0, openHeight, 0);
    }

    void Update()
    {
        // 🟢 ถ้าผู้เล่นอยู่ใกล้ ให้เลื่อนไปตำแหน่งเปิด ถ้าไม่อยู่ ให้เลื่อนกลับตำแหน่งปิด
        Vector3 targetPosition = isPlayerNear ? openPosition : closedPosition;

        // สั่งให้ประตูค่อยๆ สไลด์ไปหาเป้าหมายอย่างนุ่มนวล
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, slideSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // เช็คว่าคนที่เดินมาชนเซนเซอร์ คือผู้เล่นใช่ไหม
        if (collision.CompareTag("Player") || collision.CompareTag("Enemy"))
        {
            isPlayerNear = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // พอผู้เล่นเดินออกนอกเซนเซอร์ ก็สั่งให้ประตูปิด
        if (collision.CompareTag("Player") || collision.CompareTag("Enemy"))
        {
            isPlayerNear = false;
        }
    }
}