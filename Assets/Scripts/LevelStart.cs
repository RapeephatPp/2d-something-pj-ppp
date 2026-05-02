using UnityEngine;

public class LevelStart : MonoBehaviour
{
    [Header("Level Audio")]
    public AudioClip levelMusic; // ลากไฟล์ไฟล์เพลง mp3 หรือ wav มาใส่ช่องนี้ใน Inspector

    void Start()
    {
        // สั่งเล่นเพลงทันทีที่เริ่มฉาก (ระบบมี Crossfade ให้แล้ว สมูทแน่นอน!)
        if (AudioManager.Instance != null && levelMusic != null)
        {
            AudioManager.Instance.PlayBGM(levelMusic);
        }
    }
}