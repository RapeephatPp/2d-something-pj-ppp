using UnityEngine;

public class TutorialExit : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E; 
    private bool isPlayerNear = false;

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(interactKey))
        {
            // 1. ริบดาบคืน เปลี่ยนเป็นร่างมือเปล่า
            CharacterSwitcher.Instance.SwitchToUnarmed();

            // 2. วาร์ปกลับไปจุดที่เซฟไว้ก่อนเข้าห้อง
            CharacterSwitcher.Instance.TeleportActivePlayer(CharacterSwitcher.Instance.savedPreTutorialPosition);
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