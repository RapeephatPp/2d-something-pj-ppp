using UnityEngine;

public class EscapeEndZone : MonoBehaviour
{
    public GameTimer gameTimer;    // ลากตัว GameManager ที่มีสคริปต์นับเวลามาใส่
    public GameObject gateToClose; // ลากประตูด้านหลังที่จะให้ปิดลงมาใส่

    private bool hasTriggered = false; // ป้องกันไม่ให้โดนทริกเกอร์ซ้ำสองรอบ

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasTriggered && collision.CompareTag("Player"))
        {
            hasTriggered = true;

            // 1. สั่งหยุดเวลา
            if (gameTimer != null) gameTimer.StopTimer();

            // 2. ปิดประตูกั้นหลัง (เปิดใช้งาน Object ประตู)
            if (gateToClose != null) gateToClose.SetActive(true);

            // 3. สั่งกล้องหยุดส่าย
            if (CameraShake.Instance != null) CameraShake.Instance.StopSway();

            Debug.Log("w!");
        }
    }
}