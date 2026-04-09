using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("Settings")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Replacement Object")]
    [Tooltip("เอา Prefab ของแท่นวางเปล่าๆ หรือของที่จะให้โผล่มาแทนที่ มาใส่ตรงนี้")]
    public GameObject replacementPrefab; // 🟢 เพิ่มตัวแปรนี้เข้ามา

    private bool isPlayerNear = false;

    void Update()
    {
        // ถ้าผู้เล่นอยู่ใกล้ และกด E
        if (isPlayerNear && Input.GetKeyDown(interactKey))
        {
            // 1. สั่งให้ผู้เล่นเปลี่ยนเป็นร่างถือดาบ
            CharacterSwitcher.Instance.SwitchToArmed();
            
            // 🟢 2. เสก Object ใหม่ขึ้นมาแทนที่ตำแหน่งเดิมและองศาเดิม (ถ้ามีการลากมาใส่ไว้)
            if (replacementPrefab != null)
            {
                Instantiate(replacementPrefab, transform.position, transform.rotation);
            }

            // 3. ทำลายวัตถุจุดเก็บดาบทิ้งไปเลย
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