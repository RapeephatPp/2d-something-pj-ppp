using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public KeyCode interactKey = KeyCode.E;
    public Color activeColor = Color.yellow; 
    
    private bool isPlayerNear = false;
    private bool isActive = false;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 🟢 เพิ่มเช็คว่า ต้องยังไม่เคยถูกเปิด (!isActive) ถึงจะกด E ได้
        if (isPlayerNear && !isActive && Input.GetKeyDown(interactKey))
        {
            ActivateCheckpoint();
        }
    }

    void ActivateCheckpoint()
    {
        if (isActive) return; // ป้องกันการทำงานซ้ำ

        // 1. บันทึกข้อมูล
        CharacterSwitcher.currentCheckpointPosition = transform.position;
        CharacterSwitcher.hasCheckpoint = true;
        
        if (CharacterSwitcher.Instance != null)
        {
            CharacterSwitcher.savedIsArmed = CharacterSwitcher.Instance.isArmed;
        }

        // 2. เติมเลือดให้เต็ม
        PlayerController activePlayer = CharacterSwitcher.Instance.isArmed ? 
            CharacterSwitcher.Instance.armedPlayer.GetComponent<PlayerController>() : 
            CharacterSwitcher.Instance.unarmedPlayer.GetComponent<PlayerController>();
            
        if (activePlayer != null) activePlayer.Heal(activePlayer.maxHealth);

        // 3. เปิดรูปปั้นทำงาน
        isActive = true;
        if (sr != null) sr.color = activeColor;
        
        if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.2f, 0.1f);
        Debug.Log("Checkpoint Saved!");

        // 🟢 ใช้งานเสร็จ สั่งทำลายป้าย E ทิ้ง!
        DisablePrompt();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isActive) return; // 🟢 ถ้าจุดเซฟถูกเปิดไปแล้ว ไม่ต้องสนใจใครเดินมาใกล้อีก
        if (collision.CompareTag("Player")) isPlayerNear = true;
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
            // ทำลายรูปป้าย E และทำลายสคริปต์ทิ้ง
            if (prompt.promptVisual != null) Destroy(prompt.promptVisual);
            Destroy(prompt);
        }
    }
}