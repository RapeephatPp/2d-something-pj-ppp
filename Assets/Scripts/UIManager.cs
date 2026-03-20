using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance; // ทำเป็น Singleton เพื่อให้ Player เรียกใช้ง่ายๆ

    [Header("Health UI")]
    public Image[] hearts; // ลากรูปหัวใจ (ช่องสี่เหลี่ยม) ทั้ง 5 อันมาใส่ในนี้
    public Color fullHealthColor = Color.red; // สีตอนมีเลือด
    public Color emptyHealthColor = new Color(0.2f, 0.2f, 0.2f, 1f); // สีเทาเข้มตอนเลือดลด

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // ฟังก์ชันนี้จะถูกเรียกจาก PlayerController เวลาโดนตี
    public void UpdateHealth(int currentHealth)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < currentHealth)
            {
                hearts[i].color = fullHealthColor; // เลือดที่ยังเหลือ
            }
            else
            {
                hearts[i].color = emptyHealthColor; // เลือดที่เสียไป
            }
        }
    }
}