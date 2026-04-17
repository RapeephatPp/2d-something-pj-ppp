using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    public static bool isPaused = false;

    [Header("UI Panels (ลาก Panel ที่มี UIPanelTransition มาใส่)")]
    // 🟢 เปลี่ยนจาก GameObject เป็น UIPanelTransition เพื่อให้มันเรียกใช้แอนิเมชันได้
    public UIPanelTransition pauseMenuPanel;
    public UIPanelTransition settingsPanel;

    void Start()
    {
        isPaused = false;
        // ถ้าเกมเริ่มมา ให้แน่ใจว่าหน้าต่างพวกนี้โดนซ่อนอยู่
        if (pauseMenuPanel != null) pauseMenuPanel.HidePanel();
        if (settingsPanel != null) settingsPanel.HidePanel();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        // 🟢 เปลี่ยนมาใช้ HidePanel() แทน SetActive(false)
        if (pauseMenuPanel != null) pauseMenuPanel.HidePanel();
        if (settingsPanel != null) settingsPanel.HidePanel();
        
        Time.timeScale = 1f; 
        isPaused = false;
        
        // ถ้าเกมคุณล็อคเมาส์ตอนเล่น ก็ปลดตรงนี้
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    public void Pause()
    {
        // 🟢 เปลี่ยนมาใช้ ShowPanel() แทน SetActive(true)
        if (pauseMenuPanel != null) pauseMenuPanel.ShowPanel();
        
        Time.timeScale = 0f; 
        isPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenSettings()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.HidePanel();
        if (settingsPanel != null) settingsPanel.ShowPanel();
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.HidePanel();
        if (pauseMenuPanel != null) pauseMenuPanel.ShowPanel();
    }

    public void LoadSave()
    {
        Debug.Log("Loading last save point...");
        
        // 1. ปิดหน้าต่าง Pause และปลดล็อคเวลาให้เดินปกติก่อน
        Resume(); 
        
        // 2. 🟢 สั่งโหลดฉากปัจจุบันใหม่ (เมื่อฉากโหลดเสร็จ ระบบ Checkpoint จะจับผู้เล่นไปวางจุดเซฟให้อัตโนมัติ)
        if (ScreenFader.Instance != null)
        {
            int currentScene = SceneManager.GetActiveScene().buildIndex;
            ScreenFader.Instance.FadeToScene(currentScene);
        }
        else
        {
            // Failsafe เผื่อลืมใส่ ScreenFader ไว้ในฉาก
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; 
        
        // 🟢 อัปเกรด: ถ้ามีระบบ ScreenFader ให้ใช้เฟดจอตอนกลับเมนูหลัก!
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeToScene(0);
        }
        else
        {
            SceneManager.LoadScene(0); 
        }
    }
}