using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance; 

    [Header("Health UI")]
    public Image[] hearts; 
    public Color fullHealthColor = Color.red; 
    public Color emptyHealthColor = new Color(0.2f, 0.2f, 0.2f, 1f); 

    [Header("Game Over UI")]
    public UIPanelTransition gameOverPanel; // 🟢 ลากหน้าต่าง Game Over มาใส่ช่องนี้

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.HidePanel(); // ปิดไว้ก่อนตอนเริ่ม
    }

    public void UpdateHealth(int currentHealth)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < currentHealth) hearts[i].color = fullHealthColor; 
            else hearts[i].color = emptyHealthColor; 
        }
    }

    // 🟢 โชว์หน้า Game Over
    public void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.ShowPanel();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 🟢 กดปุ่ม Respawn
    public void Button_Respawn()
    {
        if (gameOverPanel != null) gameOverPanel.HidePanel();
        
        // สั่งโหลดด่านใหม่ เพื่อรีเซ็ตมอนสเตอร์ทั้งหมด แล้วเกมจะดึงจุดเกิดล่าสุดมาใช้เอง
        int currentScene = SceneManager.GetActiveScene().buildIndex;
        if (ScreenFader.Instance != null) ScreenFader.Instance.FadeToScene(currentScene);
        else SceneManager.LoadScene(currentScene);
    }

    // 🟢 กดปุ่ม กลับเมนูหลัก
    public void Button_MainMenu()
    {
        if (gameOverPanel != null) gameOverPanel.HidePanel();
        
        Time.timeScale = 1f; 
        if (ScreenFader.Instance != null) ScreenFader.Instance.FadeToScene(0); 
        else SceneManager.LoadScene(0);
    }
}