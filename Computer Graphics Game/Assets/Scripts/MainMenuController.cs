using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Texture2D[] levelPreviews = new Texture2D[3];
    [SerializeField] private string[] leaderboardUrls = { "", "", "" };
    [SerializeField] private string[] levelSceneNames = { "Level1", "Level2", "Level3" };
    private VisualElement _settingsPanel;
    private VisualElement _levelSelectPanel;
    private VisualElement _levelStats;
    private VisualElement _previewImage;
    private Label _bestTimeLabel;
    private string _currentLeaderboardUrl;
    private int _selectedLevel = -1;
    private Button[] _levelButtons;

    private Slider _masterVolumeSlider;
    private Slider _musicVolumeSlider;
    private Slider _soundVolumeSlider;

    private Image _masterUnmutedIcon;
    private Image _masterMutedIcon;
    private bool _isMasterMuted;

    private Image _musicUnmutedIcon;
    private Image _musicMutedIcon;
    private bool _isMusicMuted;

    private Image _soundUnmutedIcon;
    private Image _soundMutedIcon;
    private bool _isSoundMuted;

    private Label _masterVolumePct;
    private Label _musicVolumePct;
    private Label _soundVolumePct;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        root.Q<Button>("play-btn").clicked += OpenLevelSelect;
        root.Query<Button>(className: "back-btn").ForEach(
            btn => {
                btn.clicked += CloseLevelSelect;
            }
        );
        root.Q<Button>("exit-btn").clicked += Application.Quit;
        root.Q<Button>("leaderboard-btn").clicked += OpenLeaderboard;
        root.Q<Button>("play-level-btn").clicked += PlaySelectedLevel;
        root.Q<Button>("settings-btn").clicked += OpenSettings;

        _levelSelectPanel = root.Q("level-select-panel");
        _settingsPanel = root.Q("settings-panel");
        _levelStats = root.Q("level-stats");
        _previewImage = root.Q("preview-image");
        _bestTimeLabel = root.Q<Label>("best-time-label");

        _masterVolumeSlider = root.Q<Slider>("master-volume-slider");
        _musicVolumeSlider  = root.Q<Slider>("music-volume-slider");
        _soundVolumeSlider  = root.Q<Slider>("sound-volume-slider");

        _masterUnmutedIcon = root.Q<Image>("master-unmuted-icon");
        _masterMutedIcon   = root.Q<Image>("master-muted-icon");
        _musicUnmutedIcon  = root.Q<Image>("music-unmuted-icon");
        _musicMutedIcon    = root.Q<Image>("music-muted-icon");
        _soundUnmutedIcon  = root.Q<Image>("sound-unmuted-icon");
        _soundMutedIcon    = root.Q<Image>("sound-muted-icon");

        _masterVolumePct = root.Q<Label>("master-volume-pct");
        _musicVolumePct  = root.Q<Label>("music-volume-pct");
        _soundVolumePct  = root.Q<Label>("sound-volume-pct");

        _masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        _musicVolumeSlider.value  = PlayerPrefs.GetFloat("MusicVolume",  1f);
        _soundVolumeSlider.value  = PlayerPrefs.GetFloat("SoundVolume",  1f);

        _isMasterMuted = PlayerPrefs.GetInt("MasterMuted", 0) == 1;
        _isMusicMuted  = PlayerPrefs.GetInt("MusicMuted",  0) == 1;
        _isSoundMuted  = PlayerPrefs.GetInt("SoundMuted",  0) == 1;

        AudioListener.volume = _isMasterMuted ? 0f : _masterVolumeSlider.value;
        RefreshMuteIcon(_masterUnmutedIcon, _masterMutedIcon, _isMasterMuted);
        RefreshMuteIcon(_musicUnmutedIcon,  _musicMutedIcon,  _isMusicMuted);
        RefreshMuteIcon(_soundUnmutedIcon,  _soundMutedIcon,  _isSoundMuted);

        _masterVolumePct.text = _isMasterMuted ? "0%" : ToPct(_masterVolumeSlider.value);
        _musicVolumePct.text  = _isMusicMuted  ? "0%" : ToPct(_musicVolumeSlider.value);
        _soundVolumePct.text  = _isSoundMuted  ? "0%" : ToPct(_soundVolumeSlider.value);

        _masterVolumeSlider.RegisterValueChangedCallback(evt =>
        {
            if (!_isMasterMuted) AudioListener.volume = evt.newValue;
            PlayerPrefs.SetFloat("MasterVolume", evt.newValue);
            if (!_isMasterMuted) _masterVolumePct.text = ToPct(evt.newValue);
        });
        _musicVolumeSlider.RegisterValueChangedCallback(evt =>
        {
            PlayerPrefs.SetFloat("MusicVolume", evt.newValue);
            if (!_isMusicMuted) _musicVolumePct.text = ToPct(evt.newValue);
        });
        _soundVolumeSlider.RegisterValueChangedCallback(evt =>
        {
            PlayerPrefs.SetFloat("SoundVolume", evt.newValue);
            if (!_isSoundMuted) _soundVolumePct.text = ToPct(evt.newValue);
        });

        root.Q<Button>("master-mute-btn").clicked += () => ToggleMute(
            ref _isMasterMuted, _masterUnmutedIcon, _masterMutedIcon, _masterVolumePct,
            _masterVolumeSlider.value, "MasterMuted",
            () => AudioListener.volume = _isMasterMuted ? 0f : _masterVolumeSlider.value);
        root.Q<Button>("music-mute-btn").clicked += () => ToggleMute(
            ref _isMusicMuted, _musicUnmutedIcon, _musicMutedIcon, _musicVolumePct,
            _musicVolumeSlider.value, "MusicMuted", null);
        root.Q<Button>("sound-mute-btn").clicked += () => ToggleMute(
            ref _isSoundMuted, _soundUnmutedIcon, _soundMutedIcon, _soundVolumePct,
            _soundVolumeSlider.value, "SoundMuted", null);

        // Title sits above the level panel in the DOM and intercepts pointer events
        // over the back button — ignore it so events pass through.
        var titleContainer = root.Q(className: "title-container");
        if (titleContainer != null)
        {
            titleContainer.pickingMode = PickingMode.Ignore;
            foreach (var child in titleContainer.Query<VisualElement>().ToList())
                child.pickingMode = PickingMode.Ignore;
        }

        _levelButtons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            var btn = root.Q<Button>($"level-btn-{index}");
            _levelButtons[i] = btn;
            btn.clicked += () => SelectLevel(index);
            btn.RegisterCallback<PointerEnterEvent>(_ => OnLevelHover(index));
            btn.RegisterCallback<PointerLeaveEvent>(_ => OnLevelLeave());
        }
    }



    private void OpenLevelSelect() =>
        _levelSelectPanel.AddToClassList("panel-open");


    private void OpenSettings() =>
        _settingsPanel.AddToClassList("panel-open");

    private void CloseLevelSelect()
    {
        _selectedLevel = -1;
        foreach (var btn in _levelButtons)
            btn.RemoveFromClassList("level-btn-selected");
        _levelSelectPanel.RemoveFromClassList("panel-open");

        _settingsPanel.RemoveFromClassList("panel-open");
        _previewImage.RemoveFromClassList("visible");
        _levelStats.RemoveFromClassList("visible");
    }

    private void SelectLevel(int index)
    {
        for (int i = 0; i < _levelButtons.Length; i++)
            _levelButtons[i].EnableInClassList("level-btn-selected", i == index);
        _selectedLevel = index;
        ShowLevelInfo(index);
    }

    private void PlaySelectedLevel()
    {
        if (_selectedLevel >= 0)
            SceneManager.LoadScene(levelSceneNames[_selectedLevel]);
    }

    private void OpenLeaderboard()
    {
        if (!string.IsNullOrEmpty(_currentLeaderboardUrl))
            Application.OpenURL(_currentLeaderboardUrl);
    }

    private void OnLevelHover(int index)
    {
        if (_selectedLevel == -1)
            ShowLevelInfo(index);
    }

    private void OnLevelLeave()
    {
        if (_selectedLevel == -1)
        {
            _previewImage.RemoveFromClassList("visible");
            _levelStats.RemoveFromClassList("visible");
        }
    }

    private void ShowLevelInfo(int index)
    {
        if (index < levelPreviews.Length && levelPreviews[index] != null)
        {
            _previewImage.style.backgroundImage = new StyleBackground(levelPreviews[index]);
            _previewImage.AddToClassList("visible");
        }

        float best = PlayerPrefs.GetFloat($"BestTime_Level{index + 1}", 0f);
        _bestTimeLabel.text = best > 0f ? $"Best time: {FormatTime(best)}" : "Best time: --:--";
        _currentLeaderboardUrl = index < leaderboardUrls.Length ? leaderboardUrls[index] : "";
        _levelStats.AddToClassList("visible");
    }

    private static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60);
        int s = (int)(seconds % 60);
        int cs = (int)((seconds % 1f) * 100);
        return $"{m}:{s:D2}.{cs:D2}";
    }

    private void ToggleMute(ref bool isMuted, Image unmutedIcon, Image mutedIcon,
                             Label pctLabel, float sliderValue, string prefsKey, Action onApply)
    {
        isMuted = !isMuted;
        PlayerPrefs.SetInt(prefsKey, isMuted ? 1 : 0);
        onApply?.Invoke();
        RefreshMuteIcon(unmutedIcon, mutedIcon, isMuted);
        pctLabel.text = isMuted ? "0%" : ToPct(sliderValue);
    }

    private static void RefreshMuteIcon(Image unmutedIcon, Image mutedIcon, bool isMuted)
    {
        unmutedIcon.style.display = isMuted ? DisplayStyle.None : DisplayStyle.Flex;
        mutedIcon.style.display   = isMuted ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private static string ToPct(float value) => $"{Mathf.RoundToInt(value * 100)}%";
}
