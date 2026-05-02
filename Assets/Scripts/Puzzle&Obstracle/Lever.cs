using UnityEngine;
using System.Collections.Generic; 

public class Lever : MonoBehaviour
{
    [Header("Switch Targets (ใส่ได้หลายอัน)")]
    public List<GameObject> lockedGates;         // สำหรับปิดประตูล่องหน หรือซ่อนออบเจกต์
    public List<SlidingDoor> targetSlidingDoors; // สำหรับประตูเลื่อน
    public List<LaserTrap> targetLasers;         // สำหรับเลเซอร์
    
    [Space(10)]
    [Header("✨ สิ่งที่ต้องการ 'เปิด' (Activate) เมื่อสับสวิตช์")]
    public List<GameObject> objectsToActivate;   // ลากสะพาน, แท่นกระโดด, หรือแสงไฟ มาใส่ช่องนี้ได้เลย

    [Header("Visuals")]
    public Sprite activatedSprite; 
    public Color activatedColor = Color.gray; 

    [Header("Audio SFX")]
    public AudioClip leverSound; // 🟢 เสียงสับสวิตช์คันโยก (แกร๊ก!)

    private bool isPlayerNear = false;
    private bool isUsed = false;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>(); 
    }

    void Update()
    {
        if (isPlayerNear && !isUsed && Input.GetKeyDown(KeyCode.E))
        {
            ActivateLever();
        }
    }

    public void ActivateLever()
    {
        if (isUsed) return; 
        isUsed = true;
        
        Debug.Log("Lever Activated!");

        // 🟢 เล่นเสียงสับสวิตช์
        if (AudioManager.Instance != null && leverSound != null)
        {
            AudioManager.Instance.PlaySFX(leverSound, 1.0f);
        }

        // 1. สั่งปิดออบเจกต์ (Deactivate) พวกประตูที่ขวางทาง
        foreach (GameObject gate in lockedGates)
        {
            if (gate != null) gate.SetActive(false); 
        }

        // 2. สั่งเปิดออบเจกต์ (Activate) พวกสะพาน หรือกลไกที่ซ่อนอยู่
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null) obj.SetActive(true); 
        }

        // 3. สั่งดับเลเซอร์
        foreach (LaserTrap laser in targetLasers)
        {
            if (laser != null) laser.TurnOffLaser();
        }

        // 4. สั่งเปิดประตูสไลด์
        foreach (SlidingDoor door in targetSlidingDoors)
        {
            if (door != null) door.OpenDoor();
        }

        // เปลี่ยนสีหรือเปลี่ยนรูปสวิตช์
        if (sr != null) 
        {
            if (activatedSprite != null) sr.sprite = activatedSprite;
            else sr.color = activatedColor;
        }

        // เขย่ากล้องเพิ่ม Game Feel
        if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.15f, 0.1f);

        DisablePrompt(); 
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isUsed) return; 

        if (collision.CompareTag("Player")) isPlayerNear = true;

        if (collision.GetComponent<ThrownSword>() != null || collision.GetComponent<MeleeHitbox>() != null)
        {
            ActivateLever();
        }
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