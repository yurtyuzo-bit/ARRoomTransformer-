using UnityEngine;
using TMPro;

namespace ARRoomTransformer
{
    /// <summary>
    /// Geliştirme sırasında AR bilgilerini ekranda gösteren debug arayüzü.
    /// Build'lerde devre dışı bırakılabilir.
    /// </summary>
    public class ARDebugUI : MonoBehaviour
    {
        [Header("UI Referansları")]
        [SerializeField] private TextMeshProUGUI debugText;
        [SerializeField] private GameObject debugPanel;

        [Header("Ayarlar")]
#pragma warning disable 0414
        [SerializeField] private bool showInBuild = false;
#pragma warning restore 0414
        [SerializeField] private float updateInterval = 0.25f;
        [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote; // ` tuşu

        // Referanslar
        private AppManager _appManager;
        private UnityEngine.XR.ARFoundation.ARPlaneManager _planeManager;
        private UnityEngine.XR.ARFoundation.ARSession _arSession;

        private float _lastUpdateTime;
        private bool _isVisible;

        private void Start()
        {
            _appManager = FindAnyObjectByType<AppManager>();
            _planeManager = FindAnyObjectByType<UnityEngine.XR.ARFoundation.ARPlaneManager>();
            _arSession = FindAnyObjectByType<UnityEngine.XR.ARFoundation.ARSession>();

#if !UNITY_EDITOR
            if (!showInBuild)
            {
                if (debugPanel != null) debugPanel.SetActive(false);
                enabled = false;
                return;
            }
#endif

            _isVisible = true;
            if (debugPanel != null) debugPanel.SetActive(true);
        }

        private void Update()
        {
            // Toggle kontrolü (Editor'da)
#if UNITY_EDITOR
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleDebugPanel();
            }
#endif
            // 3 parmak dokunma ile toggle (mobilde)
            if (Input.touchCount == 3)
            {
                foreach (var touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        ToggleDebugPanel();
                        break;
                    }
                }
            }

            if (!_isVisible || debugText == null) return;

            if (Time.time - _lastUpdateTime < updateInterval) return;
            _lastUpdateTime = Time.time;

            UpdateDebugInfo();
        }

        private void UpdateDebugInfo()
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("<b>═══ AR DEBUG ═══</b>");
            sb.AppendLine();

            // FPS
            float fps = 1f / Time.unscaledDeltaTime;
            string fpsColor = fps >= 50 ? "green" : fps >= 30 ? "yellow" : "red";
            sb.AppendLine($"<color={fpsColor}>FPS: {fps:F0}</color>");

            // Uygulama durumu
            if (_appManager != null)
            {
                sb.AppendLine($"State: <b>{_appManager.CurrentState}</b>");
            }

            // AR Session durumu
            if (_arSession != null)
            {
                var state = UnityEngine.XR.ARFoundation.ARSession.state;
                string stateColor = state == UnityEngine.XR.ARFoundation.ARSessionState.SessionTracking
                    ? "green" : "yellow";
                sb.AppendLine($"AR: <color={stateColor}>{state}</color>");
            }

            // Algılanan düzlemler
            if (_planeManager != null)
            {
                int planeCount = 0;
                foreach (var plane in _planeManager.trackables) planeCount++;
                sb.AppendLine($"Düzlemler: {planeCount}");
            }

            // Bellek
            float memoryMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);
            string memColor = memoryMB < 200 ? "green" : memoryMB < 400 ? "yellow" : "red";
            sb.AppendLine($"Bellek: <color={memColor}>{memoryMB:F1} MB</color>");

            // Ekran bilgisi
            sb.AppendLine($"Çözünürlük: {Screen.width}x{Screen.height}");

            // Pil durumu (mobil)
            if (SystemInfo.batteryLevel >= 0)
            {
                float battery = SystemInfo.batteryLevel * 100f;
                string batColor = battery > 30 ? "green" : battery > 15 ? "yellow" : "red";
                sb.AppendLine($"Pil: <color={batColor}>{battery:F0}%</color>");
            }

            // Cihaz bilgisi
            sb.AppendLine($"Cihaz: {SystemInfo.deviceModel}");
            sb.AppendLine($"GPU: {SystemInfo.graphicsDeviceName}");

            debugText.text = sb.ToString();
        }

        /// <summary>Debug panelini göster/gizle.</summary>
        public void ToggleDebugPanel()
        {
            _isVisible = !_isVisible;
            if (debugPanel != null) debugPanel.SetActive(_isVisible);
        }
    }
}
