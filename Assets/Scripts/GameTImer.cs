using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public float timeRemaining = 90f; 
    public TextMeshProUGUI timerText; 

    [Header("Danger Mode")]
    public float dangerTime = 15f; 
    public Color normalColor = Color.white;
    public Color dangerColor = Color.red;
    public float blinkSpeed = 5f; 

    public bool isTimerRunning = false; 

    void Start()
    {
        // Force the timer to stop at the beginning, ignoring Inspector values
        isTimerRunning = false; 

        // Hide timer UI at the beginning of the level
        if (timerText != null) timerText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isTimerRunning) 
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerDisplay(timeRemaining);

                if (timeRemaining <= dangerTime)
                {
                    float t = Mathf.PingPong(Time.time * blinkSpeed, 1f);
                    timerText.color = Color.Lerp(dangerColor, normalColor, t);
                    
                    if (timeRemaining <= 5f) blinkSpeed = 10f; 
                }
            }
            else
            {
                timeRemaining = 0;
                timerText.text = "00:00";
                timerText.color = dangerColor;
                Debug.Log("Time's Up! Game Over.");
                isTimerRunning = false;
            }
        }
    }

    void UpdateTimerDisplay(float timeToDisplay)
    {
        timeToDisplay += 1; 
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void StartTimer()
    {
        isTimerRunning = true;
        
        // Show UI when triggered
        if (timerText != null) timerText.gameObject.SetActive(true); 
    }

    public void StopTimer()
    {
        isTimerRunning = false;
        timerText.color = Color.green; 
    }
}