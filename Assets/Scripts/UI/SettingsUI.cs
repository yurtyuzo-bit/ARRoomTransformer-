using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace ARRoomTransformer
{
    /// <summary>
    /// Ayarlar paneli UI. Ses, haptic, performans ve tema ayarlarını yönetir.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        [Header("Ses Ayarları")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider ambianceVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle muteToggle;

        [Header("Haptic Ayarları")]
        [SerializeField] private Toggle hapticToggle;

        [Header("Performans Ayarları")]
        [SerializeField] private TMP_Dropdown occlusionQualityDropdown;
        [SerializeField] private Toggle occlusionToggle;
        [SerializeField] private TMP_Dropdown fpsTargetDropdown;

        [Header("Oda Ayarları")]
        [SerializeField] private Slider wallHeightSlider;
        [SerializeField] private TextMeshProUGUI wallHeightText;

        [Header("Tema Ayarları")]
        [SerializeField] private TMP_Dropdown themeDropdown;

        [Header("Debug")]
        [SerializeField] private Toggle debugOverlayToggle;
        [SerializeField] private Toggle showPlanesToggle;

        [Header("Butonlar")]
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;

        [Header("Bilgi")]
        [SerializeField] private TextMeshProUGUI versionText;

        // Referanslar
        private AudioManager _audioManager;
        private OcclusionController _occlusionController;
        private MaterialManager _materialManager;
        private ARDebugUI _debugUI;

        private void Start()
        {
            // Referansları bul
            _audioManager = FindAnyObjectByType<AudioManager>();
            _occlusionController = FindAnyObjectByType<OcclusionController>();
            _materialManager = FindAnyObjectByType<MaterialManager>();
            _debugUI = FindAnyObjectByType<ARDebugUI>();

            SetupUI();
            LoadSettings();
        }

        private void SetupUI()
        {
            // Versiyon
            if (versionText != null)
                versionText.text = $"v{Constants.APP_VERSION}";

            // Ses slider'ları
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
                masterVolumeSlider.minValue = 0f;
                masterVolumeSlider.maxValue = 1f;
            }

            if (ambianceVolumeSlider != null)
            {
                ambianceVolumeSlider.onValueChanged.AddListener(OnAmbianceVolumeChanged);
                ambianceVolumeSlider.minValue = 0f;
                ambianceVolumeSlider.maxValue = 1f;
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
                sfxVolumeSlider.minValue = 0f;
                sfxVolumeSlider.maxValue = 1f;
            }

            // Mute toggle
            if (muteToggle != null)
                muteToggle.onValueChanged.AddListener(OnMuteChanged);

            // Haptic toggle
            if (hapticToggle != null)
                hapticToggle.onValueChanged.AddListener(OnHapticChanged);

            // Occlusion kalitesi
            if (occlusionQualityDropdown != null)
            {
                occlusionQualityDropdown.ClearOptions();
                occlusionQualityDropdown.AddOptions(new System.Collections.Generic.List<string>
                    { "Hızlı (Fastest)", "Orta (Medium)", "En İyi (Best)" });
                occlusionQualityDropdown.onValueChanged.AddListener(OnOcclusionQualityChanged);
            }

            if (occlusionToggle != null)
                occlusionToggle.onValueChanged.AddListener(OnOcclusionToggled);

            // FPS hedefi
            if (fpsTargetDropdown != null)
            {
                fpsTargetDropdown.ClearOptions();
                fpsTargetDropdown.AddOptions(new System.Collections.Generic.List<string>
                    { "30 FPS", "60 FPS" });
                fpsTargetDropdown.onValueChanged.AddListener(OnFPSTargetChanged);
            }

            // Duvar yüksekliği
            if (wallHeightSlider != null)
            {
                wallHeightSlider.minValue = Constants.MIN_WALL_HEIGHT;
                wallHeightSlider.maxValue = Constants.MAX_WALL_HEIGHT;
                wallHeightSlider.onValueChanged.AddListener(OnWallHeightChanged);
            }

            // Tema
            if (themeDropdown != null && _materialManager != null)
            {
                themeDropdown.ClearOptions();
                themeDropdown.AddOptions(_materialManager.ThemeNames);
                themeDropdown.onValueChanged.AddListener(OnThemeChanged);
            }

            // Debug
            if (debugOverlayToggle != null)
                debugOverlayToggle.onValueChanged.AddListener(OnDebugToggled);

            // Butonlar
            if (resetButton != null)
                resetButton.onClick.AddListener(ResetToDefaults);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        // ================================================================
        // Event Handlers
        // ================================================================

        private void OnMasterVolumeChanged(float value)
        {
            if (_audioManager != null) _audioManager.MasterVolume = value;
            PlayerPrefs.SetFloat("MasterVolume", value);
        }

        private void OnAmbianceVolumeChanged(float value)
        {
            if (_audioManager != null) _audioManager.AmbianceVolume = value;
            PlayerPrefs.SetFloat("AmbianceVolume", value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("SFXVolume", value);
        }

        private void OnMuteChanged(bool muted)
        {
            if (_audioManager != null) _audioManager.SetMute(muted);
            PlayerPrefs.SetInt("Muted", muted ? 1 : 0);
        }

        private void OnHapticChanged(bool enabled)
        {
            HapticFeedback.IsEnabled = enabled;
            PlayerPrefs.SetInt("HapticEnabled", enabled ? 1 : 0);

            if (enabled) HapticFeedback.Light(); // Test titreşimi
        }

        private void OnOcclusionQualityChanged(int index)
        {
            if (_occlusionController != null)
            {
                _occlusionController.SetQuality(
                    (OcclusionController.OcclusionQuality)index);
            }
            PlayerPrefs.SetInt("OcclusionQuality", index);
        }

        private void OnOcclusionToggled(bool enabled)
        {
            if (_occlusionController != null)
            {
                if (enabled) _occlusionController.EnableOcclusion();
                else _occlusionController.DisableOcclusion();
            }
            PlayerPrefs.SetInt("OcclusionEnabled", enabled ? 1 : 0);
        }

        private void OnFPSTargetChanged(int index)
        {
            int fps = index == 0 ? 30 : 60;
            Application.targetFrameRate = fps;
            PlayerPrefs.SetInt("TargetFPS", fps);
        }

        private void OnWallHeightChanged(float value)
        {
            if (wallHeightText != null)
                wallHeightText.text = $"{value:F1}m";
            PlayerPrefs.SetFloat("WallHeight", value);
        }

        private void OnThemeChanged(int index)
        {
            if (_materialManager != null)
                _materialManager.ApplyTheme(index);
            PlayerPrefs.SetInt("ThemeIndex", index);
        }

        private void OnDebugToggled(bool enabled)
        {
            if (_debugUI != null)
                _debugUI.ToggleDebugPanel();
            PlayerPrefs.SetInt("DebugOverlay", enabled ? 1 : 0);
        }

        // ================================================================
        // Ayar Yükleme / Sıfırlama
        // ================================================================

        private void LoadSettings()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);

            if (ambianceVolumeSlider != null)
                ambianceVolumeSlider.value = PlayerPrefs.GetFloat("AmbianceVolume", 0.5f);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

            if (muteToggle != null)
                muteToggle.isOn = PlayerPrefs.GetInt("Muted", 0) == 1;

            if (hapticToggle != null)
                hapticToggle.isOn = PlayerPrefs.GetInt("HapticEnabled", 1) == 1;

            if (occlusionQualityDropdown != null)
                occlusionQualityDropdown.value = PlayerPrefs.GetInt("OcclusionQuality", 1);

            if (occlusionToggle != null)
                occlusionToggle.isOn = PlayerPrefs.GetInt("OcclusionEnabled", 1) == 1;

            if (fpsTargetDropdown != null)
                fpsTargetDropdown.value = PlayerPrefs.GetInt("TargetFPS", 30) == 60 ? 1 : 0;

            if (wallHeightSlider != null)
                wallHeightSlider.value = PlayerPrefs.GetFloat("WallHeight", Constants.DEFAULT_WALL_HEIGHT);

            if (themeDropdown != null)
                themeDropdown.value = PlayerPrefs.GetInt("ThemeIndex", 0);

            HapticFeedback.IsEnabled = PlayerPrefs.GetInt("HapticEnabled", 1) == 1;
            Application.targetFrameRate = PlayerPrefs.GetInt("TargetFPS", 30);
        }

        /// <summary>Tüm ayarları varsayılana sıfırlar.</summary>
        public void ResetToDefaults()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            LoadSettings();
            HapticFeedback.Success();
            Debug.Log("[SettingsUI] Tüm ayarlar sıfırlandı.");
        }

        /// <summary>Ayarlar panelini kapatır.</summary>
        public void Close()
        {
            PlayerPrefs.Save();
            gameObject.SetActive(false);
        }
    }
}
