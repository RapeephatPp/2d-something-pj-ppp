using UnityEngine;
using System.Collections.Generic; // 🟢 ขาดไม่ได้เลยสำหรับการใช้งาน List

public class Lever : MonoBehaviour
{
    [Header("Switch Targets (ใส่ได้หลายอัน)")]
    public List<GameObject> lockedGates;        // 🟢 เปลี่ยนเป็น List สำหรับประตูล่องหน
    public List<SlidingDoor> targetSlidingDoors; // 🟢 เปลี่ยนเป็น List สำหรับประตูเลื่อน
    public List<LaserTrap> targetLasers;         // 🟢 เปลี่ยนเป็น List สำหรับเลเซอร์
    
    [Header("Visuals")]
    public Sprite activatedSprite; 
    public Color activatedColor = Color.gray; 

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

        // 🟢 สั่งปิดประตู Gate แบบล่องหน ทุกอันที่มีใน List
        foreach (GameObject gate in lockedGates)
        {
            if (gate != null) gate.SetActive(false); 
        }

        // 🟢 สั่งดับเลเซอร์ ทุกอันที่มีใน List
        foreach (LaserTrap laser in targetLasers)
        {
            if (laser != null) laser.TurnOffLaser();
        }

        // 🟢 สั่งเปิดประตูสไลด์ ทุกอันที่มีใน List
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