using UnityEngine;

public class CharacterSwitcher : MonoBehaviour
{
    public static CharacterSwitcher Instance; // ทำเป็น Singleton ให้เรียกใช้ง่ายๆ

    [Header("Characters")]
    public GameObject unarmedPlayer; 
    public GameObject armedPlayer;   

    [Header("Camera Setup")]
    public CameraFollow cameraFollow; 

    public bool isArmed { get; private set; } // เช็คสถานะปัจจุบัน
    public Vector3 savedPreTutorialPosition; // เอาไว้จำจุดที่ยืนก่อนวาร์ปไป Tutorial

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // เริ่มเกมด้วยมือเปล่าเสมอ
        ForceUnarmed();
    }

    // ฟังก์ชันสั่งชักดาบ
    public void SwitchToArmed()
    {
        if (isArmed) return;

        armedPlayer.transform.position = unarmedPlayer.transform.position;
        armedPlayer.transform.localScale = unarmedPlayer.transform.localScale;

        unarmedPlayer.SetActive(false);
        armedPlayer.SetActive(true);

        if (cameraFollow != null) cameraFollow.target = armedPlayer.transform;
        isArmed = true;
    }

    // ฟังก์ชันสั่งเก็บดาบ
    public void SwitchToUnarmed()
    {
        if (!isArmed) return;

        unarmedPlayer.transform.position = armedPlayer.transform.position;
        unarmedPlayer.transform.localScale = armedPlayer.transform.localScale;

        armedPlayer.SetActive(false);
        unarmedPlayer.SetActive(true);

        if (cameraFollow != null) cameraFollow.target = unarmedPlayer.transform;
        isArmed = false;
    }

    private void ForceUnarmed()
    {
        unarmedPlayer.SetActive(true);
        armedPlayer.SetActive(false);
        isArmed = false;
        if (cameraFollow != null) cameraFollow.target = unarmedPlayer.transform;
    }

    // ฟังก์ชันสำหรับจับตัวละครปัจจุบันวาร์ป
    public void TeleportActivePlayer(Vector3 newPosition)
    {
        if (isArmed) armedPlayer.transform.position = newPosition;
        else unarmedPlayer.transform.position = newPosition;
    }
}