using UnityEngine;

public class CharacterSwitcher : MonoBehaviour
{
    public static CharacterSwitcher Instance;

    [Header("Characters")]
    public GameObject unarmedPlayer; 
    public GameObject armedPlayer;   

    [Header("Animators")]
    public Animator unarmedAnimator; // 🟢 ลาก Animator ตัวมือเปล่ามาใส่
    public Animator armedAnimator;   // 🟢 ลาก Animator ตัวถือดาบมาใส่

    [Header("References")]
    public PlayerController playerController; // 🟢 ลากสคริปต์ PlayerController มาใส่
    public CameraFollow cameraFollow; 

    public bool isArmed { get; private set; } 
    public Vector3 savedPreTutorialPosition; 

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        ForceUnarmed();
    }

    public void SwitchToArmed()
    {
        if (isArmed) return;

        armedPlayer.transform.position = unarmedPlayer.transform.position;
        armedPlayer.transform.localScale = unarmedPlayer.transform.localScale;

        unarmedPlayer.SetActive(false);
        armedPlayer.SetActive(true);

        // 🟢 บอก PlayerController ว่า "เปลี่ยนไปใช้ Animator ตัวถือดาบนะ!"
        if (playerController != null && armedAnimator != null) 
            playerController.ChangeAnimator(armedAnimator);

        playerController.isArmed = true;

        if (cameraFollow != null) cameraFollow.target = armedPlayer.transform;
        isArmed = true;
    }

    public void SwitchToUnarmed()
    {
        if (!isArmed) return;

        unarmedPlayer.transform.position = armedPlayer.transform.position;
        unarmedPlayer.transform.localScale = armedPlayer.transform.localScale;

        armedPlayer.SetActive(false);
        unarmedPlayer.SetActive(true);

        // 🟢 บอก PlayerController ว่า "เปลี่ยนไปใช้ Animator ตัวมือเปล่านะ!"
        if (playerController != null && unarmedAnimator != null) 
            playerController.ChangeAnimator(unarmedAnimator);

        playerController.isArmed = false;

        if (cameraFollow != null) cameraFollow.target = unarmedPlayer.transform;
        isArmed = false;
    }

    private void ForceUnarmed()
    {
        unarmedPlayer.SetActive(true);
        armedPlayer.SetActive(false);
        isArmed = false;
        
        if (playerController != null) 
        {
            playerController.isArmed = false;
            if (unarmedAnimator != null) playerController.ChangeAnimator(unarmedAnimator);
        }

        if (cameraFollow != null) cameraFollow.target = unarmedPlayer.transform;
    }

    public void TeleportActivePlayer(Vector3 newPosition)
    {
        if (isArmed) armedPlayer.transform.position = newPosition;
        else unarmedPlayer.transform.position = newPosition;
    }
}