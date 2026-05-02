using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    public enum DoorMode { Automatic, SwitchControlled }

    [Header("Door Settings")]
    [Tooltip("Automatic = เดินใกล้แล้วเปิด / SwitchControlled = ต้องให้สวิตช์สั่ง")]
    public DoorMode doorMode = DoorMode.Automatic; 
    
    public float openHeight = 3.0f; 
    public float slideSpeed = 5.0f;

    [Tooltip("เริ่มเกมมาให้เปิดทิ้งไว้ก่อนเลยไหม?")]
    public bool startOpen = false; 

    [Header("Audio SFX")]
    public AudioClip openSound;  // 🟢 เสียงตอนเริ่มเปิดประตู (ครืนนน...)
    public AudioClip closeSound; // 🟢 เสียงตอนเริ่มปิดประตู (ตึ้ง!)

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isPlayerNear = false;
    private bool isOpen = false;
    
    private bool wasTargetOpen; // 🟢 เอาไว้จำว่าเฟรมที่แล้วประตูตั้งใจจะเปิดหรือปิด

    void Start()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + new Vector3(0, openHeight, 0);
        
        isOpen = startOpen;
        wasTargetOpen = isOpen; // ตั้งค่าความจำเริ่มต้น

        if (isOpen)
        {
            transform.position = openPosition; // วาร์ปไปจุดเปิดเลยตอนเริ่มเกม
        }
    }

    void Update()
    {
        bool shouldBeOpen = isOpen;

        if (doorMode == DoorMode.Automatic)
        {
            shouldBeOpen = isPlayerNear || isOpen; 
        }

        // ==========================================
        // 🟢 ระบบเช็คเสียง: ถ้าเป้าหมายเปลี่ยน ให้เล่นเสียงทันที!
        // ==========================================
        if (shouldBeOpen && !wasTargetOpen)
        {
            // ประตูกำลังจะเปิด!
            if (AudioManager.Instance != null && openSound != null)
                AudioManager.Instance.PlaySFX(openSound, 0.8f);
        }
        else if (!shouldBeOpen && wasTargetOpen)
        {
            // ประตูกำลังจะปิด!
            if (AudioManager.Instance != null && closeSound != null)
                AudioManager.Instance.PlaySFX(closeSound, 0.8f);
        }

        // อัปเดตความจำไว้ใช้เฟรมต่อไป
        wasTargetOpen = shouldBeOpen;

        // ขยับประตู
        Vector3 targetPosition = shouldBeOpen ? openPosition : closedPosition;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, slideSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (doorMode == DoorMode.Automatic)
        {
            if (collision.CompareTag("Player") || collision.CompareTag("Enemy"))
            {
                isPlayerNear = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (doorMode == DoorMode.Automatic)
        {
            if (collision.CompareTag("Player") || collision.CompareTag("Enemy"))
            {
                isPlayerNear = false;
            }
        }
    }

    public void OpenDoor()
    {
        isOpen = true;
        if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.15f, 0.1f);
    }

    public void CloseDoor()
    {
        isOpen = false;
        if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.15f, 0.1f);
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
        if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.15f, 0.1f);
    }
}