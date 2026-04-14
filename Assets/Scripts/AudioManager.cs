using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource; // ลาก AudioSource ที่ไว้เล่นเพลงมาใส่
    public AudioSource sfxSource; // ลาก AudioSource ที่ไว้เล่นเอฟเฟกต์มาใส่

    [Header("Default Volumes")]
    public float defaultMaster = 1.0f;
    public float defaultBGM = 0.8f;
    public float defaultSFX = 1.0f;

    void Awake()
    {
        // ทำเป็น Singleton คงกระพันข้ามฉาก
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        ApplySavedVolumes();
    }

    // 🟢 ดึงค่าที่ผู้เล่นเคยปรับไว้ในหน้า Settings มาใช้งาน
    public void ApplySavedVolumes()
    {
        float masterVol = PlayerPrefs.GetFloat("MasterVol", defaultMaster);
        float bgmVol = PlayerPrefs.GetFloat("BGMVol", defaultBGM);
        float sfxVol = PlayerPrefs.GetFloat("SFXVol", defaultSFX);

        AudioListener.volume = masterVol; // คุมเสียงรวมทั้งเกม
        if (bgmSource != null) bgmSource.volume = bgmVol;
        if (sfxSource != null) sfxSource.volume = sfxVol;
    }

    // 🟢 ฟังก์ชันสำหรับเรียกเล่นเสียงเอฟเฟกต์ (เช่น เสียงปาดาบ, เสียงปุ่มกด)
    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (sfxSource != null && clip != null)
        {
            // ใช้ PlayOneShot จะทำให้เสียงเล่นซ้อนกันได้ ไม่ขัดกันเอง
            sfxSource.PlayOneShot(clip, volumeMultiplier); 
        }
    }

    // 🟢 ฟังก์ชันสำหรับเปลี่ยนเพลงพื้นหลัง (ตอนเปลี่ยนด่าน หรือเจอบอส)
    public void PlayBGM(AudioClip bgmClip)
    {
        if (bgmSource != null && bgmClip != null)
        {
            if (bgmSource.clip == bgmClip) return; // ถ้าเพลงเดิมอยู่แล้ว ไม่ต้องเริ่มใหม่
            
            bgmSource.clip = bgmClip;
            bgmSource.Play();
        }
    }
}