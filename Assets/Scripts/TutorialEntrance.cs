using UnityEngine;

public class TutorialEntrance : MonoBehaviour
{
    [Header("Tutorial location point")]
    public Transform tutorialSpawnPoint; 
    public KeyCode interactKey = KeyCode.E;
    private bool isPlayerNear = false;

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(interactKey))
        {
            // 1. เซฟตำแหน่งปัจจุบันเอาไว้ก่อนวาร์ป
            CharacterSwitcher.Instance.savedPreTutorialPosition = CharacterSwitcher.Instance.unarmedPlayer.transform.position;
            
            // 2. เสกดาบใส่มือให้ผู้เล่นเอาไว้ใช้ในห้อง Tutorial
            CharacterSwitcher.Instance.SwitchToArmed();

            // 3. วาร์ปไปที่จุดหมาย!
            CharacterSwitcher.Instance.TeleportActivePlayer(tutorialSpawnPoint.position);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerNear = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerNear = false;
    }
}