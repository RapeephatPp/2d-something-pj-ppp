using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject startOptionsPanel;
    public GameObject settingsPanel;
    public GameObject extrasPanel;
    public GameObject quitConfirmPopup;

    [Header("Audio Settings (Sliders)")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("First Selected Buttons")]
    public GameObject mainFirstBtn;
    public GameObject startFirstBtn;
    public GameObject settingsFirstBtn;
    public GameObject extrasFirstBtn;
    public GameObject quitFirstBtn;
    
    [Header("Audio SFX")]
    public AudioClip panelSwitchSound; // 🟢 เสียงสไลด์หน้าต่างเข้า-ออก (ฟุ่บ!)
    public AudioClip cancelSound;      // 🟢 เสียงกด Back หรือ Cancel (ติ๊ด!)
    public AudioClip gameStartSound;   // 🟢 เสียงกดเริ่มเกม (ตึ้งงงงง!)

    void Start()
    {
        CloseAllPanels();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        Time.timeScale = 1f; 
        
        LoadSettingsToSliders();
    }

    // --- START SECTION ---
    public void OpenStartOptions() => SwitchPanel(startOptionsPanel, startFirstBtn);
    
    public void NewGame()
    {
        Debug.Log("Starting New Game...");
        
        if (AudioManager.Instance != null && gameStartSound != null)
            AudioManager.Instance.PlaySFX(gameStartSound, 1.2f);
        
        // 🟢 ล้างข้อมูล Checkpoint เก่าทิ้ง ป้องกันบั๊กวาร์ปมั่ว
        CharacterSwitcher.hasCheckpoint = false; 

        if (ScreenFader.Instance != null) ScreenFader.Instance.FadeToScene(2); 
        else SceneManager.LoadScene(2); 
    }

    public void ContinueGame()
    {
        Debug.Log("Loading Saved Game...");
        
        // 🟢 ให้โหลดด่านเซฟด้วยการเฟดจอเหมือนกัน
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeToScene(2); // (เดี๋ยวค่อยเปลี่ยนเป็นเลขด่านตามเซฟทีหลัง)
        }
    }

    // --- SETTINGS SECTION ---
    public void OpenSettings() => SwitchPanel(settingsPanel, settingsFirstBtn);

    // --- EXTRAS SECTION ---
    public void OpenExtras() => SwitchPanel(extrasPanel, extrasFirstBtn);
    public void OpenGallery() => Debug.Log("Gallery Coming Soon...");

    // --- QUIT SECTION ---
    public void AskToQuit() { if (quitConfirmPopup != null) quitConfirmPopup.SetActive(true); }
    public void CancelQuit() 
    { 
        if (AudioManager.Instance != null && cancelSound != null)
            AudioManager.Instance.PlaySFX(cancelSound, 0.8f);
            
        if (quitConfirmPopup != null) quitConfirmPopup.SetActive(false); 
    }
    
    public void ConfirmQuit()
    {
        Debug.Log("Exiting...");
        Application.Quit();
    }
    
    // 🟢 ฟังก์ชันจัดตำแหน่ง Slider ให้ตรงกับที่เคยเซฟไว้
    public void LoadSettingsToSliders()
    {
        float defMaster = 1.0f;
        float defBGM = 0.8f;
        float defSFX = 1.0f;

        if (AudioManager.Instance != null)
        {
            defMaster = AudioManager.Instance.defaultMaster;
            defBGM = AudioManager.Instance.defaultBGM;
            defSFX = AudioManager.Instance.defaultSFX;
        }

        // 🟢 เปลี่ยนมาใช้ SetValueWithoutNotify เหมือนกัน
        if (masterSlider != null) 
            masterSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MasterVol", defMaster));
        
        if (bgmSlider != null) 
            bgmSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("BGMVol", defBGM));
        
        if (sfxSlider != null) 
            sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SFXVol", defSFX));
    }

    public void SetMasterVolume(float value) 
    { 
        PlayerPrefs.SetFloat("MasterVol", value); 
        PlayerPrefs.Save(); // 🟢 บังคับเซฟ
        
        AudioListener.volume = value; // ปรับ Master ทันที
        
        if (AudioManager.Instance != null) AudioManager.Instance.LiveUpdateMasterVolume(value);
    }

    public void SetBGMVolume(float value) 
    { 
        PlayerPrefs.SetFloat("BGMVol", value); 
        PlayerPrefs.Save(); // 🟢 บังคับเซฟ
        if (AudioManager.Instance != null) AudioManager.Instance.LiveUpdateBGMVolume(value);
    }

    public void SetSFXVolume(float value) 
    { 
        PlayerPrefs.SetFloat("SFXVol", value); 
        PlayerPrefs.Save(); // 🟢 บังคับเซฟ
        if (AudioManager.Instance != null) AudioManager.Instance.LiveUpdateSFXVolume(value);
    }

    // --- HELPER METHODS ---
    public void BackToMain() 
    {
        // 🟢 เสียงกดยกเลิก/กลับหน้าแรก
        if (AudioManager.Instance != null && cancelSound != null)
            AudioManager.Instance.PlaySFX(cancelSound, 0.8f);
            
        SwitchPanel(mainMenuPanel, mainFirstBtn);
    }

    private void SwitchPanel(GameObject targetPanel, GameObject firstBtn)
    {   
        if (AudioManager.Instance != null && panelSwitchSound != null)
        {
            AudioManager.Instance.PlaySFX(panelSwitchSound, 0.8f);
        }
        
        CloseAllPanels();
        if (targetPanel != null) targetPanel.SetActive(true);
        if (firstBtn != null && UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(firstBtn);
    }

    private void CloseAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (startOptionsPanel != null) startOptionsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (extrasPanel != null) extrasPanel.SetActive(false);
        if (quitConfirmPopup != null) quitConfirmPopup.SetActive(false);
    }
}