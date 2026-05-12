using UnityEngine;
using UnityEngine.UI;
using TMPro; // 🟢 ขาดไม่ได้! ต้อง using TMPro เพื่อให้เรียกใช้ TextMeshPro ได้

public class SettingsManager : MonoBehaviour
{
    [Header("UI Sliders (ลากหลอดจากหน้า Pause มาใส่)")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("UI Text (ลาก Text ที่จะใช้โชว์ % มาใส่)")]
    // 🌟 ถ้าเกมคุณใช้ Text ธรรมดา (Legacy) ให้เปลี่ยน TMP_Text เป็น Text แทนนะครับ
    public TMP_Text masterText; 
    public TMP_Text bgmText;
    public TMP_Text sfxText;

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
        // 1. สร้างค่าสำรอง
        float defMaster = 1.0f;
        float defBGM = 0.8f;
        float defSFX = 1.0f;

        if (AudioManager.Instance != null)
        {
            defMaster = AudioManager.Instance.defaultMaster;
            defBGM = AudioManager.Instance.defaultBGM;
            defSFX = AudioManager.Instance.defaultSFX;
        }

        // ดึงค่าเสียงจากเครื่อง (ถ้าไม่มีให้ใช้ค่าสำรอง)
        float currentMaster = PlayerPrefs.GetFloat("MasterVol", defMaster);
        float currentBGM = PlayerPrefs.GetFloat("BGMVol", defBGM);
        float currentSFX = PlayerPrefs.GetFloat("SFXVol", defSFX);

        // 2. ใช้ SetValueWithoutNotify เพื่อขยับหลอด โดยไม่ไปกระตุ้น Event
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(currentMaster);
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(currentBGM);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(currentSFX);

        // 🌟 3. อัปเดตตัวเลข % ทันทีที่เปิดหน้าตั้งค่า
        UpdateTextUI(masterText, currentMaster);
        UpdateTextUI(bgmText, currentBGM);
        UpdateTextUI(sfxText, currentSFX);
    }

    // 🟢 ฟังก์ชันพระเอก! แปลงค่า 0.0 - 1.0 ให้เป็นเลข 0 - 100% พร้อมแปะลงจอ
    private void UpdateTextUI(TMP_Text textUI, float value)
    {
        if (textUI != null)
        {
            // ใช้ Mathf.RoundToInt เพื่อปัดเศษทศนิยมทิ้งไปเลย จะได้ตัวเลขกลมๆ สวยๆ
            int percent = Mathf.RoundToInt(value * 100f);
            textUI.text = percent.ToString() + "%";
        }
    }

    // ==========================================
    // 🟢 ระบบปรับเสียงสด พร้อมบังคับเซฟลงเครื่อง!
    // ==========================================
    public void SetMasterVolume(float value)
    {
        PlayerPrefs.SetFloat("MasterVol", value);
        PlayerPrefs.Save(); 
        
        AudioListener.volume = value; 

        if (AudioManager.Instance != null) 
            AudioManager.Instance.LiveUpdateMasterVolume(value); 

        // 🌟 อัปเดต % ทันทีที่ผู้เล่นกำลังลากหลอดสไลเดอร์
        UpdateTextUI(masterText, value);
    }

    public void SetBGMVolume(float value)
    {
        PlayerPrefs.SetFloat("BGMVol", value);
        PlayerPrefs.Save();
        
        if (AudioManager.Instance != null) 
            AudioManager.Instance.LiveUpdateBGMVolume(value); 

        // 🌟 อัปเดต %
        UpdateTextUI(bgmText, value);
    }

    public void SetSFXVolume(float value)
    {
        PlayerPrefs.SetFloat("SFXVol", value);
        PlayerPrefs.Save();
        
        if (AudioManager.Instance != null) 
            AudioManager.Instance.LiveUpdateSFXVolume(value); 

        // 🌟 อัปเดต %
        UpdateTextUI(sfxText, value);
    }
}