using UnityEngine;

public class HealZone : MonoBehaviour
{
    [Header("Heal Settings")]
    public int healAmount = 5; // จำนวนเลือดที่ได้จากการกด E 1 ครั้ง
    public KeyCode interactKey = KeyCode.E;

    private bool isPlayerNear = false;
    private PlayerController playerInZone;

    void Update()
    {
        // ถ้าผู้เล่นอยู่ในระยะ และกดปุ่ม E
        if (isPlayerNear && Input.GetKeyDown(interactKey))
        {
            if (playerInZone != null)
            {
                playerInZone.Heal(healAmount);
                // ถ้าอยากให้จุดฮีลใช้ได้แค่ครั้งเดียวแล้วหายไปเลย ให้ลบ // บรรทัดล่างออกครับ
                // Destroy(gameObject); 
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