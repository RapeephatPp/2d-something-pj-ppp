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

    void Start()
    {
        CloseAllPanels();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        Time.timeScale = 1f; 
    }

    // --- START SECTION ---
    public void OpenStartOptions() => SwitchPanel(startOptionsPanel, startFirstBtn);
    
    public void NewGame()
    {
        Debug.Log("Starting New Game...");
        
        // 🟢 เปลี่ยนจากการโหลดฉากตัดฉับๆ มาเป็นการเรียกใช้ ScreenFader แทน!
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeToScene(1); // เปลี่ยน 1 เป็นตัวเลขด่านแรกของคุณ
        }
        else
        {
            SceneManager.LoadScene(1); 
        }
    }

    public void ContinueGame()
    {
        Debug.Log("Loading Saved Game...");
        
        // 🟢 ให้โหลดด่านเซฟด้วยการเฟดจอเหมือนกัน
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeToScene(1); // (เดี๋ยวค่อยเปลี่ยนเป็นเลขด่านตามเซฟทีหลัง)
        }
    }

    // --- SETTINGS SECTION ---
    public void OpenSettings() => SwitchPanel(settingsPanel, settingsFirstBtn);

    public void SetMasterVolume(float value) { PlayerPrefs.SetFloat("MasterVol", value); }
    public void SetBGMVolume(float value) { PlayerPrefs.SetFloat("BGMVol", value); }
    public void SetSFXVolume(float value) { PlayerPrefs.SetFloat("SFXVol", value); }

    // --- EXTRAS SECTION ---
    public void OpenExtras() => SwitchPanel(extrasPanel, extrasFirstBtn);
    public void OpenGallery() => Debug.Log("Gallery Coming Soon...");

    // --- QUIT SECTION ---
    public void AskToQuit() { if (quitConfirmPopup != null) quitConfirmPopup.SetActive(true); }
    public void CancelQuit() { if (quitConfirmPopup != null) quitConfirmPopup.SetActive(false); }
    
    public void ConfirmQuit()
    {
        Debug.Log("Exiting...");
        Application.Quit();
    }

    // --- HELPER METHODS ---
    public void BackToMain() => SwitchPanel(mainMenuPanel, mainFirstBtn);

    private void SwitchPanel(GameObject targetPanel, GameObject firstBtn)
    {
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