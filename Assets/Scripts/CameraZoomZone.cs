using UnityEngine;

public class CameraZoomZone : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomOutSize = 8f;   // ขนาดกล้องตอนซูมออก (ยิ่งค่ามาก ยิ่งเห็นกว้าง)
    public float normalSize = 5f;    // ขนาดกล้องปกติ (ค่าเริ่มต้นของ Unity มักจะเป็น 5)
    public float zoomSpeed = 3f;     // ความเร็วในการซูมเข้า/ออก (สมูท)

    private Camera mainCamera;
    private bool isPlayerInside = false;

    void Start()
    {
        // ดึงกล้องหลักของฉากมาใช้งานอัตโนมัติ
        mainCamera = Camera.main; 

        // ถ้าลืมตั้งค่า normalSize จะดึงค่าเริ่มต้นของกล้องมาใช้เลยเพื่อความปลอดภัย
        if (normalSize <= 0 && mainCamera != null) 
        {
            normalSize = mainCamera.orthographicSize;
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        // กำหนดเป้าหมายว่าตอนนี้กล้องควรจะมีขนาดเท่าไหร่
        float targetSize = isPlayerInside ? zoomOutSize : normalSize;

        // ค่อยๆ ปรับขนาดกล้องให้สมูทด้วย Lerp
        mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetSize, zoomSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // เมื่อผู้เล่นเดินเข้าเขต
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // เมื่อผู้เล่นเดินออกจากเขต
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = false;
        }
    }
}