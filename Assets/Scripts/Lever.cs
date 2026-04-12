using UnityEngine;

public class Lever : MonoBehaviour
{
    public GameObject lockedGate; // ลากประตูมาใส่ช่องนี้
    public LaserTrap targetLaser;
    private bool isPlayerNear = false;
    private bool isUsed = false;

    void Update()
    {
        // เช็คว่าผู้เล่นอยู่ใกล้ และกดยกเลิกสวิตช์ (ปุ่มลูกศรขึ้น ตามโจทย์ Part 1)
        if (isPlayerNear && !isUsed && Input.GetKeyDown(KeyCode.E))
        {
            isUsed = true;
            lockedGate.SetActive(false); // ปิดประตู (ทำให้ประตูหายไป)
            Debug.Log("Gate Opened!");
            // จะเปลี่ยนสีสวิตช์ตรงนี้ก็ได้ให้รู้ว่ากดแล้ว
            GetComponent<SpriteRenderer>().color = Color.gray;
            if (targetLaser != null) targetLaser.TurnOffLaser();
        }
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerNear = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerNear = false;
    }
}