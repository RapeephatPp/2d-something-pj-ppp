using UnityEngine;

public class Lever : MonoBehaviour
{
    [Header("Switch Targets")]
    public GameObject lockedGate; 
    public LaserTrap targetLaser;
    
    [Header("Visuals")]
    public Sprite activatedSprite; // 🟢 (ตัวเลือก) ใส่รูปสวิตช์ตอนสับแล้ว
    public Color activatedColor = Color.gray; 

    private bool isPlayerNear = false;
    private bool isUsed = false;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // วิธีกดที่ 1: เดินมากด E แบบดั้งเดิม
        if (isPlayerNear && !isUsed && Input.GetKeyDown(KeyCode.E))
        {
            ActivateLever();
        }
    }

    // 🟢 สร้างฟังก์ชันกลางสำหรับสั่งเปิดสวิตช์
    public void ActivateLever()
    {
        if (isUsed) return; // ป้องกันการกดซ้ำ
        isUsed = true;
        
        Debug.Log("Lever Activated!");

        // 1. ปิดประตู / เลเซอร์
        if (lockedGate != null) lockedGate.SetActive(false); 
        if (targetLaser != null) targetLaser.TurnOffLaser();

        // 2. เปลี่ยนสีหรือเปลี่ยนรูปให้รู้ว่าทำงานแล้ว
        if (sr != null) 
        {
            if (activatedSprite != null) sr.sprite = activatedSprite;
            else sr.color = activatedColor;
        }

        // 3. เอฟเฟกต์ Game Feel
        if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.15f, 0.1f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerNear = true;

        // 🟢 วิธีกดที่ 2: ถ้าสิ่งที่มาชนคือ "ดาบที่ปามา" หรือ "ดาบที่ฟันมา"
        // (เช็คจาก Component ว่าเป็น ThrownSword หรือ MeleeHitbox ไหม)
        if (!isUsed)
        {
            if (collision.GetComponent<ThrownSword>() != null || collision.GetComponent<MeleeHitbox>() != null)
            {
                ActivateLever();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerNear = false;
    }
}