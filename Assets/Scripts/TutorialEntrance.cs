using UnityEngine;
using System.Collections;

public class TutorialEntrance : MonoBehaviour
{
    [Header("Tutorial location point")]
    public Transform tutorialSpawnPoint; 
    public KeyCode interactKey = KeyCode.E;
    private bool isPlayerNear = false;
    private bool isTeleporting = false; // กันกดเบิ้ล

    void Update()
    {
        if (isPlayerNear && !isTeleporting && Input.GetKeyDown(interactKey))
        {
            StartCoroutine(EnterTutorialRoutine());
        }
    }

    private IEnumerator EnterTutorialRoutine()
    {
        isTeleporting = true;

        // 1. จอมืดลง
        if (ScreenFader.Instance != null) yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(1f));

        // 2. เซฟจุดเกิด แจกดาบ และวาร์ปตัว
        CharacterSwitcher.Instance.savedPreTutorialPosition = CharacterSwitcher.Instance.unarmedPlayer.transform.position;
        CharacterSwitcher.Instance.SwitchToArmed();
        CharacterSwitcher.Instance.TeleportActivePlayer(tutorialSpawnPoint.position);

        yield return new WaitForSeconds(0.1f);

        // 3. จอสว่างขึ้น
        if (ScreenFader.Instance != null) yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(0f));
        isTeleporting = false;
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