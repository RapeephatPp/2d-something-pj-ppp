using UnityEngine;

public class Lever : MonoBehaviour
{
    [Header("Switch Targets")]
    public GameObject lockedGate; 
    public SlidingDoor targetSlidingDoor; // 🟢 ตัวเชื่อมกับ SlidingDoor
    public LaserTrap targetLaser;
    
    [Header("Visuals")]
    public Sprite activatedSprite; 
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
        // 🟢 ถ้าใช้งานไปแล้ว (isUsed) จะเข้าเงื่อนไขนี้ไม่ได้ ป้ายก็จะไม่ขึ้น
        if (isPlayerNear && !isUsed && Input.GetKeyDown(KeyCode.E))
        {
            ActivateLever();
        }
    }

    public void ActivateLever()
    {
        if (isUsed) return; 
        isUsed = true;
        
        Debug.Log("Lever Activated!");

        if (lockedGate != null) lockedGate.SetActive(false); 
        if (targetLaser != null) targetLaser.TurnOffLaser();
        if (targetSlidingDoor != null) targetSlidingDoor.OpenDoor();

        if (sr != null) 
        {
            if (activatedSprite != null) sr.sprite = activatedSprite;
            else sr.color = activatedColor;
        }

        if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.15f, 0.1f);

        // 🟢 ใช้งานเสร็จ สั่งทำลายป้าย E ทิ้งไปเลย!
        DisablePrompt();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isUsed) return; // 🟢 ถ้าถูกสับสวิตช์ไปแล้ว ไม่ต้องสนใจผู้เล่นที่เดินมาใกล้ๆ อีก

        if (collision.CompareTag("Player")) isPlayerNear = true;

        if (collision.GetComponent<ThrownSword>() != null || collision.GetComponent<MeleeHitbox>() != null)
        {
            ActivateLever();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerNear = false;
    }

    // 🟢 ฟังก์ชันสำหรับลบทิ้งป้ายแจ้งเตือน
    private void DisablePrompt()
    {
        InteractPrompt prompt = GetComponent<InteractPrompt>();
        if (prompt != null)
        {
            // ทำลายรูปป้าย E และทำลายสคริปต์ทิ้งไปเลย
            if (prompt.promptVisual != null) Destroy(prompt.promptVisual);
            Destroy(prompt);
        }
    }
}