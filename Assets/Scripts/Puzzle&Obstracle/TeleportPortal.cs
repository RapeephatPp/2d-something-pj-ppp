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
    public TargetFormMode formAfterTeleport = TargetFormMode.KeepCurrent;

    [Header("Visuals")]
    public GameObject interactPrompt;        // ป้าย "Press E" (ถ้ามี)

    [Header("Audio SFX")]
    public AudioClip teleportSound;  // 🟢 เสียงวาร์ปตอนกดเข้าประตู (ฟริ้วว!)
    public AudioClip portalHumSound; // 🟢 เสียงพลังงานมิติครางหึ่งๆ (วนลูป)

    private bool isPlayerNear = false;
    
    // 🟢 [ไม้ตายแก้บั๊ก] ใช้ Time.time จับเวลาแทนการใช้ Invoke
    // ต่อให้ประตูนี้โดน Culler สั่งปิดกลางอากาศ ระบบวาร์ปก็จะไม่ค้างอีกต่อไป!
    private static float nextTeleportTime = 0f; 
    
    private AudioSource audioSource; // ลำโพงส่วนตัวของประตูนี้

    void Start()
    {
        // 🟢 สร้างลำโพงสำหรับเสียงประตู (ถ้ามีการใส่เสียงไว้)
        if (portalHumSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = portalHumSound;
            audioSource.loop = true;
            audioSource.spatialBlend = 1f; // ให้เป็น 3D Sound (เดินใกล้ถึงได้ยิน)
            audioSource.maxDistance = 15f; // ระยะที่ได้ยินเสียง
            audioSource.Play();
        }
    }

    void Update()
    {
        // ถ้ายังอยู่ในช่วงคูลดาวน์วาร์ป (1.5 วิ) ให้ข้ามไปเลย
        if (Time.time < nextTeleportTime) return;

        if (isPlayerNear)
        {
            // 🟢 ดักบั๊ก 1: ถ้าลืมใส่ปลายทาง ให้แจ้ง Error แต่ไม่ทำให้เกมค้าง
            if (destinationPortal == null)
            {
                if (Input.GetKeyDown(interactKey)) 
                    Debug.LogError("🚨 บั๊ก: ประตูนี้ยังไม่ได้ใส่ Destination Portal ใน Inspector!");
                return;
            }

            if (autoTeleport || Input.GetKeyDown(interactKey))
            {
                // ล็อคการวาร์ปทุกประตูในเกมไปอีก 1.5 วินาที
                nextTeleportTime = Time.time + 1.5f;
                
                // 🟢 เล่นเสียงวาร์ป!
                if (AudioManager.Instance != null && teleportSound != null)
                    AudioManager.Instance.PlaySFX(teleportSound, 1.0f);
                
                // 🟢 โยนงานไปให้ ScreenFader เป็นคนวาร์ป
                if (ScreenFader.Instance != null)
                {
                    ScreenFader.Instance.StartCoroutine(ScreenFader.Instance.TeleportFadeRoutine(destinationPortal.transform.position, formAfterTeleport));
                }
                else
                {
                    // 🟢 ดักบั๊ก 2: ถ้าเทสเกมในฉากที่ไม่มี ScreenFader (ไม่มีจอดำ) ให้วาร์ปดื้อๆ เลยจะได้ไม่ค้าง
                    if (CharacterSwitcher.Instance != null)
                    {
                        if (formAfterTeleport == TargetFormMode.ForceArmed) CharacterSwitcher.Instance.SwitchToArmed();
                        else if (formAfterTeleport == TargetFormMode.ForceUnarmed) CharacterSwitcher.Instance.SwitchToUnarmed();
                        CharacterSwitcher.Instance.TeleportActivePlayer(destinationPortal.transform.position);
                    }
                }
            }
        }
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