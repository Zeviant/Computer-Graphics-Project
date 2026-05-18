using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Cursor = UnityEngine.Cursor;

public class PauseMenuController : MonoBehaviour
{
    private VisualElement _pausePanel;
    private bool _isPaused;
    private bool _finished;
    private PlayerController _player;
    private CameraController _camera;

    private void Start()
    {
        _player = FindFirstObjectByType<PlayerController>();
        _camera = FindFirstObjectByType<CameraController>();
    }

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _pausePanel = root.Q("pause-panel");

        root.Q<Button>("restart-btn").clicked += RestartStage;
        root.Q<Button>("mainmenu-btn").clicked += GoToMainMenu;
        root.Q<Button>(className: "back-btn").clicked += ClosePauseMenu;

        var titleContainer = root.Q(className: "title-container");
        if (titleContainer != null)
        {
            titleContainer.pickingMode = PickingMode.Ignore;
            foreach (var child in titleContainer.Query<VisualElement>().ToList())
                child.pickingMode = PickingMode.Ignore;
        }
    }

    private void OnDisable()
    {
        ClosePauseMenu();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();

        // Re-enforce cursor unlock every frame while paused — the Unity Editor re-locks
        // the cursor whenever the Game view is clicked to regain focus.
        if (_isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void DisablePause() => _finished = true;

    private void TogglePause()
    {
        if (_finished) return;
        if (_isPaused) ClosePauseMenu();
        else OpenPauseMenu();
    }

    private void OpenPauseMenu()
    {
        _isPaused = true;
        _pausePanel.AddToClassList("panel-open");
        if (_player != null) _player.InputLocked = true;
        if (_camera != null) _camera.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ClosePauseMenu()
    {
        _isPaused = false;
        _pausePanel.RemoveFromClassList("panel-open");
        if (_player != null) _player.InputLocked = false;
        if (_camera != null) _camera.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void RestartStage()
    {
        ClosePauseMenu();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToMainMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }
}
