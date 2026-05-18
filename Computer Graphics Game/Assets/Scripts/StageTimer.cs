using TMPro;
using UnityEngine;

public class StageTimer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Timer")]
    [SerializeField] private bool startAutomatically = true;

    private float elapsedTime = 0f;
    private bool isRunning = false;

    private void Awake()
    {
        if (timerText == null)
            timerText = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        if (startAutomatically)
            StartTimer();

        UpdateTimerText();
    }

    private void Update()
    {
        if (!isRunning)
            return;

        elapsedTime += Time.deltaTime;
        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        int milliseconds = Mathf.FloorToInt((elapsedTime * 1000f) % 1000f);

        timerText.text = $"Time: {minutes:00}:{seconds:00}.{milliseconds:000}";
    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
        UpdateTimerText();
    }

    public float GetElapsedTime()
    {
        return elapsedTime;
    }
}