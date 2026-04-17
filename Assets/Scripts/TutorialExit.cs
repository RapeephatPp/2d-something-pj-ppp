using UnityEngine;
using System.Collections;

public class TutorialExit : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.E; 
    private bool isPlayerNear = false;
    private bool isTeleporting = false; // กันกดเบิ้ล

    void Update()
    {
        if (isPlayerNear && !isTeleporting && Input.GetKeyDown(interactKey))
        {
            StartCoroutine(ExitTutorialRoutine());
        }
    }

    private IEnumerator ExitTutorialRoutine()
    {
        isTeleporting = true;

        // 1. จอมืดลง
        if (ScreenFader.Instance != null) yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(1f));

        // 2. ริบดาบ และวาร์ปกลับจุดเดิม
        CharacterSwitcher.Instance.SwitchToUnarmed();
        CharacterSwitcher.Instance.TeleportActivePlayer(CharacterSwitcher.Instance.savedPreTutorialPosition);

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