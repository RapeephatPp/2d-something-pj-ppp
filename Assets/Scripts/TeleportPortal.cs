using UnityEngine;
using System.Collections;

public class TeleportPortal : MonoBehaviour
{
    [Header("Portal Settings")]
    public TeleportPortal destinationPortal; // ลากประตูปลายทางมาใส่ช่องนี้
    public KeyCode interactKey = KeyCode.E;  // ปุ่มสำหรับกดวาร์ป
    public bool autoTeleport = false;        // ถ้าติ๊กถูก จะวาร์ปทันทีที่เดินชน (ไม่ต้องกด E)

    [Header("Visuals")]
    public GameObject interactPrompt;        // ป้าย "Press E" (ถ้ามี)

    private bool isPlayerNear = false;
    private static bool isTeleporting = false; // กันบั๊กกดวาร์ปรัวๆ หรือวาร์ปวนลูป

    void Update()
    {
        if (isTeleporting || destinationPortal == null) return;

        if (isPlayerNear)
        {
            // ถ้าเป็นแบบกดปุ่ม หรือแบบวาร์ปอัตโนมัติ
            if (autoTeleport || Input.GetKeyDown(interactKey))
            {
                StartCoroutine(TeleportRoutine());
            }
        }
    }

    private IEnumerator TeleportRoutine()
    {
        isTeleporting = true;

        // 1. เริ่มการ Fade จอดำ (เรียกใช้ ScreenFader ที่คุณมีอยู่แล้ว)
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(1f));
        }

        // 2. ย้ายตำแหน่งตัวละครไปที่ประตูปลายทาง
        // ใช้ฟังก์ชันจาก CharacterSwitcher เพื่อให้ย้ายได้ทั้งร่างถือดาบและร่างมือเปล่า
        if (CharacterSwitcher.Instance != null)
        {
            CharacterSwitcher.Instance.TeleportActivePlayer(destinationPortal.transform.position);
        }

        // 3. รอสักนิดเพื่อให้กล้องขยับตามทัน
        yield return new WaitForSeconds(0.1f);

        // 4. Fade จอให้สว่างขึ้น
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(0f));
        }

        // 5. ปลดล็อคให้วาร์ปต่อได้ (ใส่ Delay เล็กน้อยกันการวาร์ปกลับทันที)
        yield return new WaitForSeconds(0.5f);
        isTeleporting = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = true;
            if (interactPrompt != null && !autoTeleport) interactPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = false;
            if (interactPrompt != null) interactPrompt.SetActive(false);
        }
    }
}