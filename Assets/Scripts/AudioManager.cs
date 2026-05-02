using UnityEngine;
using System.Collections; // อย่าลืม using System.Collections สำหรับ Coroutine

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

    [Header("Game Feel Settings")]
    public float fadeDuration = 1.0f; // ความเร็วในการเฟดเปลี่ยนเพลง

    private Coroutine activeFadeRoutine;

    void Awake()
    {
        // ทำเป็น Singleton คงกระพันข้ามฉาก
        if (Instance == null)
        {
            Instance = this;
            // 🟢 แก้บั๊ก 2: บังคับให้อยู่ระดับ Root เสมอกัน Error
            transform.SetParent(null); 
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

    // 🟢 ฟังก์ชันนี้ใช้ตอนโหลดเริ่มเกม
    public void ApplySavedVolumes()
    {
        float masterVol = PlayerPrefs.GetFloat("MasterVol", defaultMaster);
        float bgmVol = PlayerPrefs.GetFloat("BGMVol", defaultBGM);
        float sfxVol = PlayerPrefs.GetFloat("SFXVol", defaultSFX);

        AudioListener.volume = masterVol; 
        if (bgmSource != null) bgmSource.volume = bgmVol;
        if (sfxSource != null) sfxSource.volume = sfxVol;
    }

    // 🟢 แก้บั๊ก 1: เพิ่ม API ให้ Slider ใน UI เรียกใช้เพื่อเปลี่ยนเสียงแบบ Real-time!
    public void LiveUpdateBGMVolume(float newVol)
    {
        if (bgmSource != null) bgmSource.volume = newVol;
    }

    public void LiveUpdateSFXVolume(float newVol)
    {
        if (sfxSource != null) sfxSource.volume = newVol;
    }
    
    public void LiveUpdateMasterVolume(float newVol)
    {
        AudioListener.volume = newVol;
    }

    // ฟังก์ชันสำหรับเรียกเล่นเสียงเอฟเฟกต์
    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, volumeMultiplier); 
        }
    }

    // 🟢 แก้บั๊ก 3: เปลี่ยนเพลงด้วยการเฟดเข้า-ออก (Game Feel Upgrade!)
    public void PlayBGM(AudioClip bgmClip)
    {
        if (bgmSource != null && bgmClip != null)
        {
            if (bgmSource.clip == bgmClip) return; 
            
            // ถ้ากำลังเฟดเพลงอื่นอยู่ ให้หยุดการเฟดเก่าก่อน
            if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
            
            activeFadeRoutine = StartCoroutine(CrossfadeBGM(bgmClip));
        }
    }

    private IEnumerator CrossfadeBGM(AudioClip newClip)
    {
        float startVolume = bgmSource.volume;
        float targetVolume = PlayerPrefs.GetFloat("BGMVol", defaultBGM); // ดึงค่าเสียงเป้าหมาย

        // 1. ค่อยๆ เฟดเสียงเก่าลง
        if (bgmSource.isPlaying)
        {
            for (float t = 0; t < fadeDuration; t += Time.deltaTime)
            {
                bgmSource.volume = Mathf.Lerp(startVolume, 0, t / fadeDuration);
                yield return null;
            }
        }

        // 2. เปลี่ยนคลิปเสียง และเริ่มเล่น
        bgmSource.clip = newClip;
        bgmSource.Play();

        // 3. ค่อยๆ เฟดเสียงใหม่ขึ้นมา
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0, targetVolume, t / fadeDuration);
            yield return null;
        }

        bgmSource.volume = targetVolume;
        activeFadeRoutine = null;
    }
    
    public void SetBGMPitch(float pitchValue)
    {
        if (bgmSource != null)
        {
            bgmSource.pitch = pitchValue;
        }
    }
    
}