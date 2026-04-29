using UnityEngine;

[RequireComponent(typeof(Collider2D))] // บังคับว่าต้องมีกรอบ Trigger
public class InteractPrompt : MonoBehaviour
{
    [Header("Prompt Visual")]
    public GameObject promptVisual; 

    [Header("Floating Animation")]
    public float floatSpeed = 3f;       // ความเร็วดึ๋งๆ
    public float floatAmount = 0.03f;   // ระยะความสูงที่เด้ง

    private Vector3 startPos;
    private bool isPlayerNear = false;

    void Start()
    {
        if (promptVisual != null)
        {
            startPos = promptVisual.transform.localPosition;
            promptVisual.SetActive(false); // ซ่อนไว้ก่อนตอนเริ่มเกม
        }
    }

    void Update()
    {
        // 🟢 ทำให้ป้ายเด้งดึ๋งๆ ตลอดเวลาที่โชว์อยู่
        if (isPlayerNear && promptVisual != null && promptVisual.activeSelf)
        {
            float newY = startPos.y + (Mathf.Sin(Time.time * floatSpeed) * floatAmount);
            promptVisual.transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = true;
            if (promptVisual != null) promptVisual.SetActive(true); // โชว์ป้าย
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = false;
            if (promptVisual != null) promptVisual.SetActive(false); // ซ่อนป้าย
        }
    }
}