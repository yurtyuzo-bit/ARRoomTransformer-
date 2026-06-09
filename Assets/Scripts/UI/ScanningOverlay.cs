using UnityEngine;
using TMPro;

namespace ARRoomTransformer
{
    /// <summary>
    /// AR tarama sırasında kullanıcıya rehberlik eden görsel overlay.
    /// Crosshair, mesafe göstergesi ve yüzey algılama animasyonu gösterir.
    /// </summary>
    public class ScanningOverlay : MonoBehaviour
    {
        [Header("UI Elemanları")]
        [SerializeField] private RectTransform crosshair;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private UnityEngine.UI.Image scanProgressRing;
        [SerializeField] private CanvasGroup overlayGroup;

        [Header("Crosshair Ayarları")]
        [SerializeField] private float crosshairSize = 40f;
        [SerializeField] private Color crosshairValidColor = new Color(0.2f, 0.9f, 0.4f, 0.9f);
        [SerializeField] private Color crosshairInvalidColor = new Color(0.9f, 0.3f, 0.2f, 0.7f);
        [SerializeField] private float crosshairPulseSpeed = 2f;

        [Header("Talimatlar (Türkçe)")]
        [SerializeField] private string msgLookAround = "Telefonunuzu yavaşça çevirin";
        [SerializeField] private string msgDetectingPlanes = "Yüzeyler algılanıyor...";
        [SerializeField] private string msgMarkCorners = "Odanın köşelerini işaretleyin";
        [SerializeField] private string msgTapToMark = "Köşeyi işaretlemek için dokunun";
        [SerializeField] private string msgMinCorners = "En az 3 köşe işaretleyin";
        [SerializeField] private string msgConfirm = "Onaylamak için ✓ butonuna basın";

        private UnityEngine.XR.ARFoundation.ARRaycastManager _raycastManager;
        private UnityEngine.UI.Image _crosshairImage;
        private bool _isActive;
        private float _pulseTimer;

        /// <summary>Overlay aktif mi?</summary>
        public bool IsActive => _isActive;

        private void Awake()
        {
            _raycastManager = FindAnyObjectByType<UnityEngine.XR.ARFoundation.ARRaycastManager>();

            if (crosshair != null)
            {
                _crosshairImage = crosshair.GetComponent<UnityEngine.UI.Image>();
            }
        }

        private void Update()
        {
            if (!_isActive) return;

            UpdateCrosshair();
            UpdateDistance();
        }

        /// <summary>Overlay'i gösterir.</summary>
        public void Show()
        {
            _isActive = true;
            if (overlayGroup != null)
            {
                overlayGroup.alpha = 1f;
                overlayGroup.blocksRaycasts = false;
            }
        }

        /// <summary>Overlay'i gizler.</summary>
        public void Hide()
        {
            _isActive = false;
            if (overlayGroup != null)
            {
                overlayGroup.alpha = 0f;
            }
        }

        /// <summary>Talimat metnini faza göre günceller.</summary>
        public void SetPhase(ScanPhase phase, int cornerCount = 0)
        {
            if (instructionText == null) return;

            instructionText.text = phase switch
            {
                ScanPhase.Initializing => msgLookAround,
                ScanPhase.ScanningPlanes => msgDetectingPlanes,
                ScanPhase.MarkingCorners => $"{msgMarkCorners}\n<size=70%>{msgTapToMark}</size>",
                ScanPhase.Confirming => $"{msgConfirm}\n<size=70%>{cornerCount} köşe işaretlendi</size>",
                ScanPhase.Completed => "",
                _ => ""
            };
        }

        /// <summary>İlerleme halkasını günceller (0-1).</summary>
        public void SetProgress(float progress)
        {
            if (scanProgressRing != null)
            {
                scanProgressRing.fillAmount = Mathf.Clamp01(progress);
            }
        }

        private void UpdateCrosshair()
        {
            if (_crosshairImage == null || _raycastManager == null) return;

            // Ekran merkezinden raycast
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var hits = new System.Collections.Generic.List<UnityEngine.XR.ARFoundation.ARRaycastHit>();
            bool hitSurface = _raycastManager.Raycast(screenCenter, hits,
                UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon);

            // Renk güncelle
            Color targetColor = hitSurface ? crosshairValidColor : crosshairInvalidColor;

            // Pulse animasyonu
            _pulseTimer += Time.deltaTime * crosshairPulseSpeed;
            float pulse = hitSurface ? 1f : 0.6f + Mathf.Sin(_pulseTimer) * 0.4f;

            _crosshairImage.color = targetColor * pulse;

            // Boyut animasyonu
            float targetSize = hitSurface ? crosshairSize : crosshairSize * 1.2f;
            crosshair.sizeDelta = Vector2.Lerp(crosshair.sizeDelta,
                new Vector2(targetSize, targetSize), Time.deltaTime * 8f);
        }

        private void UpdateDistance()
        {
            if (distanceText == null || _raycastManager == null) return;

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var hits = new System.Collections.Generic.List<UnityEngine.XR.ARFoundation.ARRaycastHit>();

            if (_raycastManager.Raycast(screenCenter, hits,
                UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
            {
                float distance = hits[0].distance;
                distanceText.text = $"{distance:F2}m";
                distanceText.gameObject.SetActive(true);
            }
            else
            {
                distanceText.gameObject.SetActive(false);
            }
        }
    }


}
