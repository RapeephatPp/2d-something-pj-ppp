using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    public static bool isPaused = false;

    [Header("UI Panels (ลาก Panel ที่มี UIPanelTransition มาใส่)")]
    public UIPanelTransition pauseMenuPanel;
    public UIPanelTransition settingsPanel;

    [Header("Audio SFX")]
    public AudioClip pauseSound;     // 🟢 เสียงตอนกด ESC เพื่อหยุดเกม (ฟรึ่บ!)
    public AudioClip resumeSound;    // 🟢 เสียงตอนกดกลับเข้าเกม (วื้ดด!)
    public AudioClip uiClickSound;   // 🟢 เสียงกดปุ่มเมนูต่างๆ ในหน้า Pause

    [Header("Game Feel Settings")]
    [Tooltip("ความทุ้มของเพลงตอนพับจอ (1.0 = ปกติ, ยิ่งน้อยยิ่งทุ้มยาน)")]
    public float pausedBGMPitch = 0.8f; 

    void Start()
    {
        isPaused = false;
        // ถ้าเกมเริ่มมา ให้แน่ใจว่าหน้าต่างพวกนี้โดนซ่อนอยู่
        if (pauseMenuPanel != null) pauseMenuPanel.HidePanel();
        if (settingsPanel != null) settingsPanel.HidePanel();
    }

    void Update()
    {
        // 🟢 กด ESC เพื่อสลับไปมา
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.HidePanel();
        if (settingsPanel != null) settingsPanel.HidePanel();
        
        Time.timeScale = 1f; 
        isPaused = false;
        
        // 🟢 เล่นเสียงกลับเข้าเกม และ คืนค่าเพลงให้กลับมาจังหวะปกติ
        if (AudioManager.Instance != null)
        {
            if (resumeSound != null) AudioManager.Instance.PlaySFX(resumeSound, 1.0f);
            AudioManager.Instance.SetBGMPitch(1.0f);
        }

        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    public void Pause()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.ShowPanel();
        
        Time.timeScale = 0f; 
        isPaused = true;

        // 🟢 เล่นเสียง Pause และ ปรับเพลงให้ทุ้มยานลง!
        if (AudioManager.Instance != null)
        {
            if (pauseSound != null) AudioManager.Instance.PlaySFX(pauseSound, 1.0f);
            AudioManager.Instance.SetBGMPitch(pausedBGMPitch);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenSettings()
    {
        PlayClickSound(); // 🟢 เสียงคลิก
        if (pauseMenuPanel != null) pauseMenuPanel.HidePanel();
        if (settingsPanel != null) 
        {
            settingsPanel.ShowPanel();
            
            // 🟢 [แก้บั๊ก] บังคับโหลดค่าเสียงใหม่ทันทีที่หน้าต่างสไลด์เข้ามา!
            SettingsManager sm = settingsPanel.GetComponentInChildren<SettingsManager>();
            if (sm != null) sm.LoadSettingsToSliders();
        }
    }

    public void CloseSettings()
    {
        PlayClickSound(); // 🟢 เสียงคลิก
        if (settingsPanel != null) settingsPanel.HidePanel();
        if (pauseMenuPanel != null) pauseMenuPanel.ShowPanel();
    }

    public void LoadSave()
    {
        PlayClickSound(); // 🟢 เสียงคลิก
        Debug.Log("Loading last save point...");
        
        // คืนค่าเพลงก่อนโหลดฉากด้วย เผื่อเพลงติดบั๊กยานไปยันด่านหน้า
        if (AudioManager.Instance != null) AudioManager.Instance.SetBGMPitch(1.0f);

        Resume(); 
        
        if (ScreenFader.Instance != null)
        {
            int currentScene = SceneManager.GetActiveScene().buildIndex;
            ScreenFader.Instance.FadeToScene(currentScene);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void ReturnToMainMenu()
    {
        PlayClickSound(); // 🟢 เสียงคลิก
        Time.timeScale = 1f; 
        
        // คืนค่าเพลงให้กลับมาปกติก่อนออกไปเมนูหลัก
        if (AudioManager.Instance != null) AudioManager.Instance.SetBGMPitch(1.0f);

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeToScene(0);
        }
        else
        {
            SceneManager.LoadScene(0); 
        }
    }

    // 🟢 ฟังก์ชันช่วยเล่นเสียงปุ่ม
    private void PlayClickSound()
    {
        if (AudioManager.Instance != null && uiClickSound != null)
        {
            AudioManager.Instance.PlaySFX(uiClickSound, 0.8f);
        }
    }
}