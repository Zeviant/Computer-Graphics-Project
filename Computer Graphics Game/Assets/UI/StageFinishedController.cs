using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

public class StageFinishedController : MonoBehaviour
{
    [SerializeField] private int stageIndex; // 0 = Stage 1, 1 = Stage 2, 2 = Stage 3

    private UIDocument uiDocument;
    private Label currentTimeLabel;
    private Label pbTimeLabel;
    private Label pbTextLabel;
    private Coroutine pbAnimCoroutine;
    private bool initialized;
    private bool visible;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        uiDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    private void Init()
    {
        initialized = true;
        var root = uiDocument.rootVisualElement;

        currentTimeLabel = root.Q<Label>(className: "current_time");
        pbTimeLabel      = root.Q<Label>(className: "pb_time");
        pbTextLabel      = root.Q<Label>(className: "pb_text");

        root.Q<Button>("retry-btn").clicked       += () => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        root.Q<Button>("exit-btn").clicked        += () => SceneManager.LoadScene("MainMenu");
        root.Q<Button>("leaderboard-btn").clicked += () => Application.OpenURL("http://google.com");
    }

    public void Show(float currentTime)
    {
        if (!initialized) Init();

        bool isNewPb = PbManager.Submit(stageIndex, currentTime);
        float pbTime = PbManager.GetBest(stageIndex) ?? currentTime;

        currentTimeLabel.text = "Time: " + FormatTime(currentTime);
        pbTimeLabel.text      = "PB: "   + FormatTime(pbTime);

        pbTextLabel.style.display = isNewPb ? DisplayStyle.Flex : DisplayStyle.None;

        if (isNewPb)
        {
            if (pbAnimCoroutine != null)
                StopCoroutine(pbAnimCoroutine);
            pbAnimCoroutine = StartCoroutine(AnimatePbText());
        }

        uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        visible = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (visible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private IEnumerator AnimatePbText()
    {
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            float s = 1f + Mathf.Sin(t * 5f) * 0.2f;
            pbTextLabel.style.scale = new StyleScale(new Scale(new Vector3(s, s, 1f)));
            yield return null;
        }
    }

    private string FormatTime(float time)
    {
        int minutes = (int)time / 60;
        int seconds = (int)time % 60;
        int ms      = (int)((time % 1f) * 100f);
        return $"{minutes:00}:{seconds:00}.{ms:00}";
    }
}
