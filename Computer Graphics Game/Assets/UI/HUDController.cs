using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    private Label timerLabel;
    private Label streakLabel;
    private Label velocityLabel;

    private PlayerController playerController;

    private float elapsedTime = 0f;
    private bool isRunning = true;

    private int bhopStreak = 0;
    private BhopQuality bestQualityInStreak = BhopQuality.None;

    private Coroutine popCoroutine;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        timerLabel = root.Q<Label>(className: "timer");
        streakLabel = root.Q<Label>(className: "bhop-streak");
        velocityLabel = root.Q<Label>(className: "velocity");

        playerController = FindFirstObjectByType<PlayerController>();

        PlayerController.OnBhopPerformed += HandleBhop;
        PlayerController.OnBhopChainBroken += HandleChainBroken;
    }

    private void OnDisable()
    {
        PlayerController.OnBhopPerformed -= HandleBhop;
        PlayerController.OnBhopChainBroken -= HandleChainBroken;
    }

    private void Update()
    {
        if (isRunning)
            elapsedTime += Time.deltaTime;

        timerLabel.text = FormatTime(elapsedTime);

        if (playerController != null)
            velocityLabel.text = $"{playerController.HorizontalSpeed:F1} m/s";
    }

    private void HandleBhop(BhopQuality quality)
    {
        if (quality == BhopQuality.None)
        {
            ResetStreak();
            return;
        }

        bhopStreak++;
        if (quality > bestQualityInStreak)
            bestQualityInStreak = quality;

        string label = bestQualityInStreak == BhopQuality.Perfect ? "PERFECT" : "GREAT";
        streakLabel.text = $"x{bhopStreak} {label}";

        Color streakColor = GetStreakColor(bhopStreak);
        streakLabel.style.color = streakColor;
        streakLabel.style.textShadow = new StyleTextShadow(new TextShadow
        {
            offset = Vector2.zero,
            blurRadius = 30f,
            color = new Color(streakColor.r, streakColor.g, streakColor.b, 0.9f)
        });

        TriggerPop();
    }

    private void TriggerPop()
    {
        if (popCoroutine != null)
        {
            StopCoroutine(popCoroutine);
            streakLabel.RemoveFromClassList("bhop-streak--pop");
        }
        popCoroutine = StartCoroutine(PopAnimation());
    }

    private IEnumerator PopAnimation()
    {
        streakLabel.AddToClassList("bhop-streak--pop");
        yield return null;
        streakLabel.RemoveFromClassList("bhop-streak--pop");
        popCoroutine = null;
    }

    private Color GetStreakColor(int streak)
    {
        if (streak >= 7) return new Color(1f, 0.2f, 0.8f);   // magenta
        if (streak >= 5) return new Color(1f, 0.15f, 0.15f); // red
        if (streak >= 3) return new Color(1f, 0.55f, 0f);    // orange
        return new Color(1f, 0.86f, 0.2f);                   // yellow
    }

    private void HandleChainBroken() => ResetStreak();

    private void ResetStreak()
    {
        bhopStreak = 0;
        bestQualityInStreak = BhopQuality.None;
        streakLabel.text = "";
        streakLabel.style.color = StyleKeyword.Null;
        streakLabel.style.textShadow = StyleKeyword.Null;
    }

    public void StartTimer() => isRunning = true;
    public void StopTimer()  => isRunning = false;
    public void ResetTimer() => elapsedTime = 0f;

    private string FormatTime(float t)
    {
        int minutes = (int)t / 60;
        int seconds = (int)t % 60;
        int ms      = (int)((t % 1) * 100);
        return $"{minutes:00}:{seconds:00}.{ms:00}";
    }
}
