using UnityEngine;
using System.Collections;

// 🟢 1. สร้างตัวเลือก (Enum) ให้เราเลือกใน Inspector ได้ง่ายๆ
public enum TargetFormMode
{
    KeepCurrent, // ไม่เปลี่ยน คงร่างเดิมไว้
    ForceArmed,  // บังคับเปลี่ยนเป็นร่างถือดาบ
    ForceUnarmed // บังคับเปลี่ยนเป็นร่างมือเปล่า
}

public class TeleportPortal : MonoBehaviour
{
    [Header("Portal Settings")]
    public TeleportPortal destinationPortal; // ลากประตูปลายทางมาใส่ช่องนี้
    public KeyCode interactKey = KeyCode.E;  // ปุ่มสำหรับกดวาร์ป
    public bool autoTeleport = false;        // ถ้าติ๊กถูก จะวาร์ปทันทีที่เดินชน (ไม่ต้องกด E)
    
    [Header("Character Form Override")]
    [Tooltip("เมื่อวาร์ปผ่านจุดนี้ จะบังคับเปลี่ยนเป็นร่างไหน?")]
    public TargetFormMode formAfterTeleport = TargetFormMode.KeepCurrent; // 🟢 2. เพิ่มตัวแปรให้เลือกโหมด

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

        // 1. เริ่มการ Fade จอดำ
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(1f));
        }

        // 🟢 2. เช็คและเปลี่ยนร่างตัวละคร (ทำตอนที่จอมืดสนิทไปแล้ว ผู้เล่นจะไม่เห็นจังหวะกระพริบ)
        if (CharacterSwitcher.Instance != null)
        {
            if (formAfterTeleport == TargetFormMode.ForceArmed)
            {
                CharacterSwitcher.Instance.SwitchToArmed();
            }
            else if (formAfterTeleport == TargetFormMode.ForceUnarmed)
            {
                CharacterSwitcher.Instance.SwitchToUnarmed();
            }

            // 3. ย้ายตำแหน่งตัวละครร่างที่กำลัง Active ไปที่ประตูปลายทาง
            CharacterSwitcher.Instance.TeleportActivePlayer(destinationPortal.transform.position);
        }

        // 4. รอสักนิดเพื่อให้กล้องขยับตามทัน
        yield return new WaitForSeconds(0.1f);

        // 5. Fade จอให้สว่างขึ้น
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(0f));
        }

        // 6. ปลดล็อคให้วาร์ปต่อได้
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