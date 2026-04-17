using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset;
    private Vector3 velocity = Vector3.zero;

    [Header("Mouse Follow")]
    [SerializeField] private float mouseOffsetDistance = 2f;
    [SerializeField] private float maxOffsetX = 2f;
    [SerializeField] private float maxOffsetY = 1f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;

        // เอาเมาส์มา offset กล้องเบา ๆ
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 diff = mouseWorld - target.position;
        diff.z = 0f;

        if (diff.sqrMagnitude > 1f)
            diff = diff.normalized;

        Vector3 mouseOffset = new Vector3(
            Mathf.Clamp(diff.x * mouseOffsetDistance, -maxOffsetX, maxOffsetX),
            Mathf.Clamp(diff.y * mouseOffsetDistance, -maxOffsetY, maxOffsetY),
            0f
        );

        desiredPos += mouseOffset;

        Vector3 smoothPos = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, 1f / smoothSpeed);
        transform.position = new Vector3(smoothPos.x, smoothPos.y, transform.position.z);
    }
    // ==========================================
    // 🟢 ฟังก์ชันใหม่สำหรับสั่งกล้องวาร์ปทันที
    // ==========================================
    public void SnapToTarget()
    {
        if (target == null) return;
        
        // คำนวณจุดกึ่งกลางที่กล้องควรอยู่
        Vector3 desiredPos = target.position + offset;
        
        // บังคับวาร์ปกล้องไปตรงนั้นทันที
        transform.position = new Vector3(desiredPos.x, desiredPos.y, transform.position.z);
        
        // ล้างค่าความเร็วสไลด์เดิมทิ้งให้เป็น 0 กล้องจะได้ไม่ไหลต่อ
        velocity = Vector3.zero; 
    }
}