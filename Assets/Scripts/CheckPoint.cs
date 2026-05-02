using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public KeyCode interactKey = KeyCode.E;
    public Color activeColor = Color.yellow; 
    
    [Header("Audio SFX")]
    public AudioClip saveSound; // 🟢 เสียงตอนกดเซฟ (กังวานๆ สบายใจ)

    private bool isPlayerNear = false;
    private bool isActive = false;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isPlayerNear && !isActive && Input.GetKeyDown(interactKey))
        {
            ActivateCheckpoint();
        }
    }

    void ActivateCheckpoint()
    {
        if (isActive) return; 

        // 🟢 เล่นเสียงเซฟเกม!
        if (AudioManager.Instance != null && saveSound != null)
        {
            AudioManager.Instance.PlaySFX(saveSound, 1.0f);
        }

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

        DisablePrompt();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isActive) return; 
        if (collision.CompareTag("Player")) isPlayerNear = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerNear = false;
    }

    private void DisablePrompt()
    {
        InteractPrompt prompt = GetComponent<InteractPrompt>();
        if (prompt != null)
        {
            if (prompt.promptVisual != null) Destroy(prompt.promptVisual);
            Destroy(prompt);
        }
    }
}