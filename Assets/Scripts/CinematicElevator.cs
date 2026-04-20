using UnityEngine;
using System.Collections;

public class CinematicElevator : MonoBehaviour
{
    [Header("Elevator Settings")]
    public Transform elevatorPlatform; 
    public Transform destinationPoint; 
    public float moveSpeed = 5f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Positioning")]
    [Tooltip("ระยะความสูงจากพื้นลิฟต์ (ปรับเพิ่มถ้าตัวละครจมพื้น)")]
    public float playerYOffset = 1.1f; // 🟢 ปรับเพิ่มจากเดิมที่เป็น 0.5f

    private bool isPlayerNear = false;
    private bool isMoving = false;
    private PlayerController currentPlayer;
    private Vector3 originalPosition;

    void Start()
    {
        if (elevatorPlatform != null) originalPosition = elevatorPlatform.position;
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
        currentPlayer.SetRidingElevator(true); // เปลี่ยนเป็น Kinematic ในสคริปต์ PlayerController
        
        // 🟢 เปลี่ยนวิธีล็อค: ย้ายไปเป็นลูกก่อน แล้วค่อยเซ็ตตำแหน่ง Local
        currentPlayer.transform.SetParent(elevatorPlatform); 
        currentPlayer.transform.localPosition = new Vector3(0, playerYOffset, 0); // 0 คือกึ่งกลางลิฟต์พอดี

        InteractPrompt prompt = GetComponent<InteractPrompt>();
        if (prompt != null && prompt.promptVisual != null) prompt.promptVisual.SetActive(false);

        // 3. จอสว่างขึ้น
        if (ScreenFader.Instance != null) yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(0f));

        // 4. เริ่มเลื่อนลิฟต์
        if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.15f, 0.1f);

        Vector3 targetPos = destinationPoint.position;
        while (Vector3.Distance(elevatorPlatform.position, targetPos) > 0.001f)
        {
            elevatorPlatform.position = Vector3.MoveTowards(elevatorPlatform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        elevatorPlatform.position = targetPos;
        if (CameraShake.Instance != null) CameraShake.Instance.StartManagedShake(0.2f, 0.1f);

        // 5. สลับตำแหน่งเพื่อกดครั้งหน้ากลับที่เดิม
        destinationPoint.position = originalPosition;
        originalPosition = elevatorPlatform.position;

        // 6. ปล่อยตัวละคร
        currentPlayer.transform.SetParent(null); 
        currentPlayer.SetRidingElevator(false); // คืนค่าฟิสิกส์เป็น Dynamic
        
        if (CharacterSwitcher.Instance != null) CharacterSwitcher.Instance.enabled = true;
        isMoving = false;
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