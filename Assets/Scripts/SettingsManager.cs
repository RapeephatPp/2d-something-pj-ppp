using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("Audio Configuration")]
    public AudioMixer mainMixer; // ถ้าคุณใช้ Audio Mixer ใน Unity

    void Awake()
    {
        // ระบบ Singleton เพื่อให้เรียกใช้จากที่ไหนก็ได้
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ให้ตัวนี้อยู่ข้าม Scene ได้
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 🟢 ฟังก์ชันหลักที่ทั้ง Main Menu และ Pause Menu จะมาเรียกใช้
    public void SetMasterVolume(float value)
    {
        // ปรับเสียงจริงในเครื่อง
        AudioListener.volume = value; 
        PlayerPrefs.SetFloat("MasterVol", value);
    }

    public void SetBGMVolume(float value)
    {
        // ถ้ามี Audio Mixer ให้ใส่ Logic ตรงนี้
        PlayerPrefs.SetFloat("BGMVol", value);
    }

    public void SetSFXVolume(float value)
    {
        PlayerPrefs.SetFloat("SFXVol", value);
    }
}