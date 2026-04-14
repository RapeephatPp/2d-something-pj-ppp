using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public KeyCode interactKey = KeyCode.E;
    public Color activeColor = Color.yellow; // สีตอนที่กดเซฟแล้ว (ให้รู้ว่าทำงานแล้ว)
    
    private bool isPlayerNear = false;
    private bool isActive = false;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // ถ้าผู้เล่นอยู่ใกล้และกด E
        if (isPlayerNear && Input.GetKeyDown(interactKey))
        {
            ActivateCheckpoint();
        }
    }

    void ActivateCheckpoint()
    {
        // 1. บันทึกข้อมูลเข้าตัวแปร Static
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
        if (!isActive)
        {
            isActive = true;
            if (sr != null) sr.color = activeColor;
            
            if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.2f, 0.1f);
            Debug.Log("Checkpoint Saved!");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerNear = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerNear = false;
    }
}