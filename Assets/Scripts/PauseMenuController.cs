using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    public static bool isPaused = false;

    [Header("UI Panels")]
    public GameObject pauseMenuPanel;
    public GameObject settingsPanel; // สามารถลากหน้า Settings จาก Main Menu มาใช้ซ้ำได้

    void Update()
    {
        // 🟢 กด Escape เพื่อพักเกมหรือกลับมาเล่นต่อ
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        pauseMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        
        Time.timeScale = 1f; // 🟢 คืนเวลาให้โลกในเกม
        isPaused = false;
        
        // ล็อคเมาส์กลับคืน (ถ้าเกมมีการล็อคเมาส์)
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    public void Pause()
    {
        pauseMenuPanel.SetActive(true);
        
        Time.timeScale = 0f; // 🟢 หยุดเวลาทุกอย่างในเกม (Update จะยังทำงานแต่ฟิสิกส์จะหยุด)
        isPaused = true;

        // ปลดล็อคเมาส์ให้กดปุ่มได้
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenSettings()
    {
        pauseMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        pauseMenuPanel.SetActive(true);
    }

    public void LoadSave()
    {
        Debug.Log("Loading last save point...");
        // 🟢 Logic: Resume เกมก่อนแล้วค่อยวาร์ปผู้เล่นไปจุดเซฟ
        Resume();
        // เรียกใช้ฟังก์ชัน Load จาก SaveSystem ของคุณที่นี่
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // 🟢 สำคัญมาก: ต้องคืนเวลาก่อนเปลี่ยน Scene ไม่เช่นนั้นหน้าเมนูจะหยุดนิ่ง
        SceneManager.LoadScene(0); // กลับไปหน้าเมนู (Index 0)
    }
}