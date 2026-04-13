using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio; // สำหรับคนที่ใช้ AudioMixer

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
        // เริ่มต้น: เปิดหน้าหลักหน้าเดียว ที่เหลือปิดให้หมด
        CloseAllPanels();
        mainMenuPanel.SetActive(true);
        Time.timeScale = 1f; // คืนเวลาให้โลกปกติ
    }

    // --- START SECTION ---
    public void OpenStartOptions() => SwitchPanel(startOptionsPanel, startFirstBtn);
    
    public void NewGame()
    {
        Debug.Log("Starting New Game...");
        // สมมติว่าด่านแรกอยู่ที่ Index 1 ใน Build Settings
        SceneManager.LoadScene(1); 
    }

    public void ContinueGame()
    {
        Debug.Log("Loading Saved Game...");
        // ใส่ Logic การโหลดไฟล์เซฟของคุณตรงนี้
    }

    // --- SETTINGS SECTION ---
    public void OpenSettings() => SwitchPanel(settingsPanel, settingsFirstBtn);

    public void SetMasterVolume(float value)
    {
        Debug.Log($"Master Volume: {value}");
        // ถ้าใช้ AudioMixer: mixer.SetFloat("Master", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("MasterVol", value);
    }

    public void SetBGMVolume(float value)
    {
        Debug.Log($"BGM Volume: {value}");
        PlayerPrefs.SetFloat("BGMVol", value);
    }

    public void SetSFXVolume(float value)
    {
        Debug.Log($"SFX Volume: {value}");
        PlayerPrefs.SetFloat("SFXVol", value);
    }

    // --- EXTRAS SECTION ---
    public void OpenExtras() => SwitchPanel(extrasPanel, extrasFirstBtn);
    public void OpenGallery() => Debug.Log("Gallery Coming Soon...");

    // --- QUIT SECTION ---
    public void AskToQuit() => quitConfirmPopup.SetActive(true);
    public void CancelQuit() => quitConfirmPopup.SetActive(false);
    
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
        targetPanel.SetActive(true);
        if (firstBtn != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(firstBtn);
    }

    private void CloseAllPanels()
    {
        mainMenuPanel.SetActive(false);
        startOptionsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        extrasPanel.SetActive(false);
        quitConfirmPopup.SetActive(false);
    }
}