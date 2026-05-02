using UnityEngine;

public class HealZone : MonoBehaviour
{
    [Header("Heal Settings")]
    public int healAmount = 5; 
    public KeyCode interactKey = KeyCode.E;

    [Header("Cooldown Settings")]
    public float cooldownDuration = 20f; 
    private float nextHealTime = 0f;      

    [Header("Visual Feedback")]
    public Color readyColor = Color.green;    
    public Color cooldownColor = Color.gray;   
    
    [Header("Audio SFX")]
    public AudioClip healSound;  // 🟢 เสียงตอนฮีลสำเร็จ (วิ้งๆ ฟื้นฟู)
    public AudioClip errorSound; // 🟢 เสียงกดตอนแท่นกำลังชาร์จ (ติ๊ด! ปฏิเสธ)

    private SpriteRenderer sr;
    private bool isPlayerNear = false;
    private PlayerController playerInZone;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = readyColor;
    }

    void Update()
    {
        bool isReady = Time.time >= nextHealTime;

        if (sr != null)
        {
            sr.color = isReady ? readyColor : cooldownColor;
        }

        if (isPlayerNear && Input.GetKeyDown(interactKey))
        {
            if (isReady)
            {
                if (playerInZone != null)
                {
                    playerInZone.Heal(healAmount);
                    nextHealTime = Time.time + cooldownDuration;
                    
                    // 🟢 เล่นเสียงฮีล
                    if (AudioManager.Instance != null && healSound != null)
                        AudioManager.Instance.PlaySFX(healSound, 1.0f);

                    if (CameraShake.Instance != null)
                        CameraShake.Instance.StartManagedShake(0.1f, 0.05f);
                }
            }
            else
            {
                // 🟢 เล่นเสียง Error แจ้งเตือนว่าติดคูลดาวน์
                if (AudioManager.Instance != null && errorSound != null)
                    AudioManager.Instance.PlaySFX(errorSound, 0.5f);
                    
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