using UnityEngine;

public class HealZone : MonoBehaviour
{
    [Header("Heal Settings")]
    public int healAmount = 5; 
    public KeyCode interactKey = KeyCode.E;

    [Header("Cooldown Settings")]
    public float cooldownDuration = 20f; // 🟢 ตั้งค่าคูลดาวน์ (เช่น 20 วินาที)
    private float nextHealTime = 0f;      // ตัวเก็บเวลาที่จะใช้ได้ครั้งต่อไป

    [Header("Visual Feedback")]
    public Color readyColor = Color.green;    // สีตอนพร้อมใช้
    public Color cooldownColor = Color.gray;   // สีตอนติดคูลดาวน์
    private SpriteRenderer sr;

    private bool isPlayerNear = false;
    private PlayerController playerInZone;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        // เริ่มเกมมาให้เป็นสีพร้อมใช้งาน
        if (sr != null) sr.color = readyColor;
    }

    void Update()
    {
        // 1. เช็คว่าปัจจุบัน "พร้อมใช้งาน" หรือยัง
        bool isReady = Time.time >= nextHealTime;

        // 2. ปรับสีตัวเครื่องตามสถานะคูลดาวน์
        if (sr != null)
        {
            sr.color = isReady ? readyColor : cooldownColor;
        }

        // 3. ตรวจสอบการกดใช้งาน
        if (isPlayerNear && Input.GetKeyDown(interactKey))
        {
            if (isReady)
            {
                if (playerInZone != null)
                {
                    // ทำการฮีล
                    playerInZone.Heal(healAmount);
                    
                    // 🟢 เซ็ตเวลาคูลดาวน์ครั้งต่อไป
                    nextHealTime = Time.time + cooldownDuration;

                    // เพิ่มเอฟเฟกต์สั่นกล้องเบาๆ ให้รู้ว่าฮีลแล้ว
                    if (CameraShake.Instance != null)
                        CameraShake.Instance.StartManagedShake(0.1f, 0.05f);
                }
            }
            else
            {
                Debug.Log("จุดฮีลกำลังชาร์จพลัง... รออีก " + (nextHealTime - Time.time).ToString("F1") + " วินาที");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = true;
            playerInZone = collision.GetComponent<PlayerController>();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = false;
            playerInZone = null;
        }
    }
}