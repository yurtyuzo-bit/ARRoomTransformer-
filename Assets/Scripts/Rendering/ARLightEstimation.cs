using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARRoomTransformer
{
    /// <summary>
    /// AR ortam ışığını algılayıp sahne ışığına uygular.
    /// Gerçekçi aydınlatma için ARKit light estimation verilerini kullanır.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class ARLightEstimation : MonoBehaviour
    {
        [Header("AR Referansları")]
        [SerializeField] private ARCameraManager arCameraManager;

        [Header("Interpolasyon Ayarları")]
        [SerializeField, Range(0.1f, 10f)]
        private float intensityLerpSpeed = 3f;

        [SerializeField, Range(0.1f, 10f)]
        private float colorLerpSpeed = 3f;

        [SerializeField, Range(0.1f, 10f)]
        private float directionLerpSpeed = 2f;

        [Header("Varsayılan Değerler")]
        [SerializeField] private float defaultIntensity = 1f;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private float defaultColorTemperature = 6500f;

        [Header("Kontrol")]
        [SerializeField] private bool isEnabled = true;

        // Hedef değerler (smooth lerp için)
        private float _targetIntensity;
        private Color _targetColor;
        private Quaternion _targetRotation;
        private float _targetColorTemperature;

        private Light _light;
        private bool _hasInitialized;

        /// <summary>Light estimation aktif mi?</summary>
        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                isEnabled = value;
                if (!value) ResetToDefaults();
            }
        }

        /// <summary>Son okunan ambient yoğunluk (lümen).</summary>
        public float CurrentAmbientIntensity { get; private set; }

        /// <summary>Son okunan renk sıcaklığı (Kelvin).</summary>
        public float CurrentColorTemperature { get; private set; }

        /// <summary>Light estimation verisi güncellendiğinde tetiklenir.</summary>
        public event Action<ARLightEstimationData> OnLightEstimationUpdated;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _light.type = LightType.Directional;
            ResetToDefaults();
        }

        private void OnEnable()
        {
            if (arCameraManager != null)
            {
                arCameraManager.frameReceived += OnFrameReceived;
            }
        }

        private void OnDisable()
        {
            if (arCameraManager != null)
            {
                arCameraManager.frameReceived -= OnFrameReceived;
            }
        }

        private void Update()
        {
            if (!_hasInitialized) return;

            // Smooth interpolasyon uygula
            float dt = Time.deltaTime;
            _light.intensity = Mathf.Lerp(_light.intensity, _targetIntensity, dt * intensityLerpSpeed);
            _light.color = Color.Lerp(_light.color, _targetColor, dt * colorLerpSpeed);
            _light.colorTemperature = Mathf.Lerp(_light.colorTemperature, _targetColorTemperature, dt * colorLerpSpeed);
            _light.transform.rotation = Quaternion.Slerp(_light.transform.rotation, _targetRotation, dt * directionLerpSpeed);
        }

        /// <summary>
        /// AR kamera frame alındığında çağrılır.
        /// Light estimation verilerini okur ve hedef değerleri günceller.
        /// </summary>
        private void OnFrameReceived(ARCameraFrameEventArgs args)
        {
            if (!isEnabled) return;

            var lightEstimation = args.lightEstimation;
            _hasInitialized = true;

            // Ambient yoğunluk
            if (lightEstimation.averageBrightness.HasValue)
            {
                _targetIntensity = lightEstimation.averageBrightness.Value;
                CurrentAmbientIntensity = _targetIntensity;
            }

            // Ambient renk
            if (lightEstimation.averageColorTemperature.HasValue)
            {
                _targetColorTemperature = lightEstimation.averageColorTemperature.Value;
                _targetColor = Mathf.CorrelatedColorTemperatureToRGB(_targetColorTemperature);
                CurrentColorTemperature = _targetColorTemperature;
            }

            // Ana ışık yönü
            if (lightEstimation.mainLightDirection.HasValue)
            {
                _targetRotation = Quaternion.LookRotation(lightEstimation.mainLightDirection.Value);
            }

            // Ana ışık yoğunluğu (varsa ambient yerine bunu kullan)
            if (lightEstimation.mainLightIntensityLumens.HasValue)
            {
                // Lümen'den Unity intensity'ye yaklaşık dönüşüm
                _targetIntensity = lightEstimation.mainLightIntensityLumens.Value / 1000f;
            }

            // Ana ışık rengi
            if (lightEstimation.mainLightColor.HasValue)
            {
                _targetColor = lightEstimation.mainLightColor.Value;
            }

            OnLightEstimationUpdated?.Invoke(lightEstimation);
        }

        /// <summary>Işık değerlerini varsayılana sıfırlar.</summary>
        public void ResetToDefaults()
        {
            _targetIntensity = defaultIntensity;
            _targetColor = defaultColor;
            _targetColorTemperature = defaultColorTemperature;
            _targetRotation = Quaternion.Euler(50f, -30f, 0f);

            if (_light != null)
            {
                _light.intensity = defaultIntensity;
                _light.color = defaultColor;
                _light.colorTemperature = defaultColorTemperature;
            }

            _hasInitialized = false;
        }

        /// <summary>Lerp hızlarını çalışma zamanında ayarlar.</summary>
        public void SetLerpSpeeds(float intensity, float color, float direction)
        {
            intensityLerpSpeed = Mathf.Clamp(intensity, 0.1f, 10f);
            colorLerpSpeed = Mathf.Clamp(color, 0.1f, 10f);
            directionLerpSpeed = Mathf.Clamp(direction, 0.1f, 10f);
        }
    }
}
