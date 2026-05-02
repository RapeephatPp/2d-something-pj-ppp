using UnityEngine;
using System.Collections;

// 🟢 บังคับให้ Unity แปะ AudioSource ให้ลิฟต์ตัวนี้อัตโนมัติ
[RequireComponent(typeof(AudioSource))] 
public class CinematicElevator : MonoBehaviour
{
    [Header("Elevator Settings")]
    public Transform elevatorPlatform; 
    public Transform destinationPoint; 
    public float moveSpeed = 5f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Positioning")]
    [Tooltip("ระยะความสูงจากพื้นลิฟต์ (ปรับเพิ่มถ้าตัวละครจมพื้น)")]
    public float playerYOffset = 1.1f;

    [Header("Audio SFX")]
    public AudioClip moveSound; // 🟢 เสียงมอเตอร์/โซ่ลิฟต์ (ควรใช้ไฟล์ที่วนลูปได้เนียนๆ)
    public AudioClip dingSound; // 🟢 เสียงติ๊ง! ตอนถึงชั้นเป้าหมาย

    private bool isPlayerNear = false;
    private bool isMoving = false;
    private PlayerController currentPlayer;
    private Vector3 originalPosition;
    private AudioSource audioSource; // 🟢 ตัวเล่นเสียงส่วนตัวของลิฟต์

    void Start()
    {
        if (elevatorPlatform != null) originalPosition = elevatorPlatform.position;
        
        // 🟢 ตั้งค่า Audio Source ของลิฟต์ให้พร้อมใช้งาน
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;          // สั่งให้เสียงมอเตอร์วนลูป
        audioSource.playOnAwake = false;  // ไม่ต้องเล่นตอนเริ่มเกม
    }

    void Update()
    {
        if (isPlayerNear && !isMoving && Input.GetKeyDown(interactKey))
        {
            StartCoroutine(ElevatorRoutine());
        }
    }

    private IEnumerator ElevatorRoutine()
    {
        isMoving = true;

        if (CharacterSwitcher.Instance != null)
        {
            CharacterSwitcher.Instance.enabled = false; 
            currentPlayer = CharacterSwitcher.Instance.isArmed ? 
                CharacterSwitcher.Instance.armedPlayer.GetComponent<PlayerController>() : 
                CharacterSwitcher.Instance.unarmedPlayer.GetComponent<PlayerController>();
        }

        if (currentPlayer == null) yield break;

        // 1. จอมืดลง
        if (ScreenFader.Instance != null) yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(1f));

        // 2. จัดระเบียบตัวละคร
        currentPlayer.SetRidingElevator(true); 
        currentPlayer.transform.SetParent(elevatorPlatform); 
        currentPlayer.transform.localPosition = new Vector3(0, playerYOffset, 0); 

        InteractPrompt prompt = GetComponent<InteractPrompt>();
        if (prompt != null && prompt.promptVisual != null) prompt.promptVisual.SetActive(false);

        // 3. จอสว่างขึ้น
        if (ScreenFader.Instance != null) yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(0f));

        // 4. เริ่มเลื่อนลิฟต์
        if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.15f, 0.1f);
        
        // 🟢 เริ่มเล่นเสียงมอเตอร์ลิฟต์!
        if (moveSound != null)
        {
            audioSource.clip = moveSound;
            audioSource.Play();
        }

        Vector3 targetPos = destinationPoint.position;
        while (Vector3.Distance(elevatorPlatform.position, targetPos) > 0.001f)
        {
            elevatorPlatform.position = Vector3.MoveTowards(elevatorPlatform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        elevatorPlatform.position = targetPos;
        
        // 🟢 ลิฟต์ถึงที่หมายแล้ว: ปิดเสียงมอเตอร์ทันที แล้วเล่นเสียง "ติ๊ง!"
        audioSource.Stop();
        if (AudioManager.Instance != null && dingSound != null)
        {
            AudioManager.Instance.PlaySFX(dingSound, 1.0f);
        }

        if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.2f, 0.1f);

        // 5. สลับตำแหน่งเพื่อกดครั้งหน้ากลับที่เดิม
        destinationPoint.position = originalPosition;
        originalPosition = elevatorPlatform.position;

        // 6. ปล่อยตัวละคร
        currentPlayer.transform.SetParent(null); 
        currentPlayer.SetRidingElevator(false); 
        
        if (CharacterSwitcher.Instance != null) CharacterSwitcher.Instance.enabled = true;
        isMoving = false;
    }
    
    // 🟢 [ฟีเจอร์กันบั๊ก] ลิฟต์ทับคนตาย!
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ถ้าลิฟต์กำลังเลื่อนอยู่ แล้วไปทับโดนใครเข้า
        if (isMoving)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                PlayerController pc = collision.gameObject.GetComponent<PlayerController>();
                // สั่งทำดาเมจตายทันที (ทะลุ I-Frames ด้วย!)
                if (pc != null) pc.TakeDamage(999, true); 
            }
            else if (collision.gameObject.CompareTag("Enemy"))
            {
                EnemyBehavior eb = collision.gameObject.GetComponent<EnemyBehavior>();
                if (eb != null) eb.TakeDamage(999);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isMoving) isPlayerNear = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerNear = false;
    }
}