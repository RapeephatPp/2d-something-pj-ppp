using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E;
    private bool isPlayerNear = false;

    void Update()
    {
        // ถ้าผู้เล่นอยู่ใกล้ และกด E
        if (isPlayerNear && Input.GetKeyDown(interactKey))
        {
            // 1. สั่งให้ผู้เล่นเปลี่ยนเป็นร่างถือดาบ
            CharacterSwitcher.Instance.SwitchToArmed();
            
            // 2. ทำลายวัตถุจุดเก็บดาบทิ้งไปเลย (เก็บได้ครั้งเดียว เปลี่ยนกลับไม่ได้)
            Destroy(gameObject);
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