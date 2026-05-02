using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("UI Sliders (ลากหลอดจากหน้า Pause มาใส่)")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    void Start()
    {
        LoadSettingsToSliders();
    }
    
    // 🟢 ถ้าหน้าต่างนี้ถูกปิด-เปิดใหม่ ให้มันโหลดค่ามาอัปเดตเสมอ
    void OnEnable()
    {
        LoadSettingsToSliders();
    }

    public void LoadSettingsToSliders()
    {
        // 🟢 1. สร้างค่าสำรองไว้ เผื่อคุณกดเทสจากฉากด่านโดยตรง (ที่ไม่มี AudioManager) สคริปต์จะได้ไม่พัง!
        float defMaster = 1.0f;
        float defBGM = 0.8f;
        float defSFX = 1.0f;

        if (AudioManager.Instance != null)
        {
            defMaster = AudioManager.Instance.defaultMaster;
            defBGM = AudioManager.Instance.defaultBGM;
            defSFX = AudioManager.Instance.defaultSFX;
        }

        // 🟢 2. ใช้ SetValueWithoutNotify เพื่อขยับหลอด โดยไม่ไปกระตุ้น Event ให้รวน
        if (masterSlider != null) 
            masterSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MasterVol", defMaster));
        
        if (bgmSlider != null) 
            bgmSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("BGMVol", defBGM));
        
        if (sfxSlider != null) 
            sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SFXVol", defSFX));
    }

    // ==========================================
    // 🟢 ระบบปรับเสียงสด พร้อมบังคับเซฟลงเครื่อง!
    // ==========================================
    public void SetMasterVolume(float value)
    {
        PlayerPrefs.SetFloat("MasterVol", value);
        PlayerPrefs.Save(); // 🟢 3. บังคับเซฟลงเครื่องทันที! (กัน Unity ลืมตอนกด Stop)
        
        // Master Volume สามารถปรับได้เลยแม้จะไม่มี AudioManager ในฉาก
        AudioListener.volume = value; 

        if (AudioManager.Instance != null) 
            AudioManager.Instance.LiveUpdateMasterVolume(value); 
    }

    public void SetBGMVolume(float value)
    {
        PlayerPrefs.SetFloat("BGMVol", value);
        PlayerPrefs.Save();
        
        if (AudioManager.Instance != null) 
            AudioManager.Instance.LiveUpdateBGMVolume(value); 
    }

    public void SetSFXVolume(float value)
    {
        PlayerPrefs.SetFloat("SFXVol", value);
        PlayerPrefs.Save();
        
        if (AudioManager.Instance != null) 
            AudioManager.Instance.LiveUpdateSFXVolume(value); 
    }
}