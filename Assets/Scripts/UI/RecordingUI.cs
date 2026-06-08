using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace ARRoomTransformer
{
    /// <summary>
    /// Kayıt durumlarını tanımlar.
    /// </summary>
    public enum RecordingState
    {
        /// <summary>Boşta - kayıt yapılmıyor.</summary>
        Idle,
        /// <summary>Geri sayım devam ediyor (3-2-1).</summary>
        Countdown,
        /// <summary>Kayıt yapılıyor.</summary>
        Recording,
        /// <summary>Kayıt duraklatıldı.</summary>
        Paused
    }

    /// <summary>
    /// Video kayıt kontrol arayüzünü yönetir.
    /// Kayıt/Durdur butonu, süre göstergesi, geri sayım ve
    /// animasyonlu göstergeler içerir.
    /// </summary>
    public class RecordingUI : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Kayıt Butonu")]
        [SerializeField, Tooltip("Kayıt başlat/durdur butonu.")]
        private Button _recordButton;

        [SerializeField, Tooltip("Kayıt butonu ikonu (kayıt ve durdurma durumunda değişir).")]
        private Image _recordButtonIcon;

        [SerializeField, Tooltip("Kayıt durumunda gösterilecek ikon (kırmızı daire).")]
        private Sprite _recordingSprite;

        [SerializeField, Tooltip("Boşta gösterilecek ikon (kırmızı dolu daire).")]
        private Sprite _idleSprite;

        [SerializeField, Tooltip("Kırmızı kayıt göstergesi (yanıp sönen).")]
        private Image _recordingIndicator;

        [Header("Süre Göstergesi")]
        [SerializeField, Tooltip("Kayıt süresi metni (MM:SS formatında).")]
        private TextMeshProUGUI _timerText;

        [Header("Geri Sayım")]
        [SerializeField, Tooltip("Geri sayım metni (3-2-1).")]
        private TextMeshProUGUI _countdownText;

        [SerializeField, Tooltip("Geri sayım paneli CanvasGroup.")]
        private CanvasGroup _countdownPanel;

        [SerializeField, Tooltip("Geri sayım süresi (saniye).")]
        private int _countdownDuration = 3;

        [Header("Ek Butonlar")]
        [SerializeField, Tooltip("Son kaydı önizle butonu.")]
        private Button _previewButton;

        [SerializeField, Tooltip("Kaydı paylaş butonu.")]
        private Button _shareButton;

        [SerializeField, Tooltip("Geri dön butonu.")]
        private Button _backButton;

        [Header("Animasyon Ayarları")]
        [SerializeField, Tooltip("Kayıt göstergesi yanıp sönme hızı.")]
        private float _pulseSpeed = 2f;

        [SerializeField, Tooltip("Kayıt göstergesi minimum alpha.")]
        private float _pulseMinAlpha = 0.2f;

        [SerializeField, Tooltip("Kayıt göstergesi maksimum alpha.")]
        private float _pulseMaxAlpha = 1f;

        [SerializeField, Tooltip("Ekran kenarı kırmızı flaş efekti.")]
        private Image _flashOverlay;

        [SerializeField, Tooltip("Flaş efekti hızı.")]
        private float _flashSpeed = 1.5f;

        [SerializeField, Tooltip("Flaş efekti maksimum alpha.")]
        private float _flashMaxAlpha = 0.08f;

        #endregion

        #region Events

        /// <summary>Kayıt başlatıldığında tetiklenir.</summary>
        [Header("Olaylar")]
        public UnityEvent OnRecordingStarted;

        /// <summary>Kayıt durdurulduğunda tetiklenir.</summary>
        public UnityEvent OnRecordingStopped;

        /// <summary>Önizleme butonuna basıldığında tetiklenir.</summary>
        public UnityEvent OnPreviewPressed;

        /// <summary>Paylaş butonuna basıldığında tetiklenir.</summary>
        public UnityEvent OnSharePressed;

        /// <summary>Geri butonuna basıldığında tetiklenir.</summary>
        public UnityEvent OnBackPressed;

        /// <summary>Geri sayım tamamlandığında tetiklenir.</summary>
        public UnityEvent OnCountdownCompleted;

        /// <summary>Kayıt durumu değiştiğinde tetiklenir.</summary>
        public UnityEvent<RecordingState> OnRecordingStateChanged;

        #endregion

        #region Private State

        private RecordingState _currentState = RecordingState.Idle;
        private float _recordingTime;
        private bool _isRecordingActive;
        private Coroutine _countdownCoroutine;
        private Coroutine _pulseCoroutine;
        private bool _hasLastRecording;

        #endregion

        #region Properties

        /// <summary>Mevcut kayıt durumu.</summary>
        public RecordingState CurrentState => _currentState;

        /// <summary>Kayıt süresi (saniye).</summary>
        public float RecordingTime => _recordingTime;

        /// <summary>Kayıt aktif mi?</summary>
        public bool IsRecording => _currentState == RecordingState.Recording;

        /// <summary>
        /// Kayıt süresini MM:SS formatında döndürür.
        /// </summary>
        public string FormattedTime
        {
            get
            {
                int minutes = Mathf.FloorToInt(_recordingTime / 60f);
                int seconds = Mathf.FloorToInt(_recordingTime % 60f);
                return $"{minutes:00}:{seconds:00}";
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            BindButtons();
            ResetUI();
        }

        private void OnDisable()
        {
            UnbindButtons();
            StopAllAnimations();
        }

        private void Update()
        {
            if (_currentState == RecordingState.Recording)
            {
                _recordingTime += Time.deltaTime;
                UpdateTimerDisplay();
                AnimateFlash();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Kayıt butonuna basıldığında çağrılır.
        /// Boştaysa geri sayım başlatır, kayıt yapılıyorsa durdurur.
        /// </summary>
        public void ToggleRecording()
        {
            switch (_currentState)
            {
                case RecordingState.Idle:
                    StartCountdown();
                    break;

                case RecordingState.Recording:
                    StopRecording();
                    break;

                case RecordingState.Countdown:
                    // Geri sayım sırasında iptal
                    CancelCountdown();
                    break;

                case RecordingState.Paused:
                    ResumeRecording();
                    break;
            }
        }

        /// <summary>
        /// Kayıt süresini sıfırlar.
        /// </summary>
        public void ResetTimer()
        {
            _recordingTime = 0f;
            UpdateTimerDisplay();
        }

        /// <summary>
        /// Son kayıt var olarak işaretler (önizleme ve paylaşım butonlarını aktif eder).
        /// </summary>
        public void SetHasLastRecording(bool hasRecording)
        {
            _hasLastRecording = hasRecording;
            UpdateButtonStates();
        }

        /// <summary>
        /// Kayıt durumunu dışarıdan ayarlar (örn. ARVideoRecorder'dan gelen bildirim).
        /// </summary>
        /// <param name="state">Yeni kayıt durumu.</param>
        public void SetRecordingState(RecordingState state)
        {
            var prevState = _currentState;
            _currentState = state;

            UpdateRecordButtonVisual();
            UpdateButtonStates();

            if (state == RecordingState.Recording && prevState != RecordingState.Recording)
            {
                StartPulseAnimation();
            }
            else if (state != RecordingState.Recording)
            {
                StopPulseAnimation();
            }

            OnRecordingStateChanged?.Invoke(state);
        }

        #endregion

        #region Recording Flow

        /// <summary>
        /// Geri sayımı başlatır (3-2-1).
        /// </summary>
        private void StartCountdown()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
            }
            _countdownCoroutine = StartCoroutine(CountdownCoroutine());
        }

        /// <summary>
        /// Geri sayımı iptal eder.
        /// </summary>
        private void CancelCountdown()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }

            SetRecordingState(RecordingState.Idle);
            SetCountdownVisible(false);
        }

        /// <summary>
        /// Geri sayım coroutine'i. Tamamlandığında kaydı başlatır.
        /// </summary>
        private IEnumerator CountdownCoroutine()
        {
            SetRecordingState(RecordingState.Countdown);
            SetCountdownVisible(true);

            for (int i = _countdownDuration; i > 0; i--)
            {
                if (_countdownText != null)
                {
                    _countdownText.text = i.ToString();
                }

                // Sayı büyüyüp küçülen animasyon
                yield return StartCoroutine(AnimateCountdownNumber());
            }

            SetCountdownVisible(false);
            _countdownCoroutine = null;

            OnCountdownCompleted?.Invoke();
            StartRecording();
        }

        /// <summary>
        /// Geri sayım sayısını büyütüp küçülten animasyon.
        /// </summary>
        private IEnumerator AnimateCountdownNumber()
        {
            if (_countdownText == null)
            {
                yield return new WaitForSeconds(1f);
                yield break;
            }

            RectTransform rect = _countdownText.rectTransform;
            float duration = 0.8f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                // Büyüyüp küçülme: 0 → 1.2 → 1.0
                float scale;
                if (t < 0.3f)
                {
                    scale = Mathf.Lerp(0.5f, 1.3f, t / 0.3f);
                }
                else
                {
                    scale = Mathf.Lerp(1.3f, 1f, (t - 0.3f) / 0.7f);
                }
                rect.localScale = Vector3.one * scale;
                yield return null;
            }

            rect.localScale = Vector3.one;
            yield return new WaitForSeconds(0.2f);
        }

        /// <summary>
        /// Kaydı başlatır.
        /// </summary>
        private void StartRecording()
        {
            _recordingTime = 0f;
            SetRecordingState(RecordingState.Recording);
            UpdateTimerDisplay();

            Debug.Log("[RecordingUI] Kayıt başladı.");
            OnRecordingStarted?.Invoke();
        }

        /// <summary>
        /// Kaydı durdurur.
        /// </summary>
        private void StopRecording()
        {
            SetRecordingState(RecordingState.Idle);
            _hasLastRecording = true;
            UpdateButtonStates();

            Debug.Log($"[RecordingUI] Kayıt durduruldu. Süre: {FormattedTime}");
            OnRecordingStopped?.Invoke();
        }

        /// <summary>
        /// Duraklatılmış kaydı devam ettirir.
        /// </summary>
        private void ResumeRecording()
        {
            SetRecordingState(RecordingState.Recording);
            Debug.Log("[RecordingUI] Kayıt devam ediyor.");
        }

        #endregion

        #region Button Handlers

        private void BindButtons()
        {
            if (_recordButton != null) _recordButton.onClick.AddListener(ToggleRecording);
            if (_previewButton != null) _previewButton.onClick.AddListener(HandlePreview);
            if (_shareButton != null) _shareButton.onClick.AddListener(HandleShare);
            if (_backButton != null) _backButton.onClick.AddListener(HandleBack);
        }

        private void UnbindButtons()
        {
            if (_recordButton != null) _recordButton.onClick.RemoveListener(ToggleRecording);
            if (_previewButton != null) _previewButton.onClick.RemoveListener(HandlePreview);
            if (_shareButton != null) _shareButton.onClick.RemoveListener(HandleShare);
            if (_backButton != null) _backButton.onClick.RemoveListener(HandleBack);
        }

        private void HandlePreview()
        {
            Debug.Log("[RecordingUI] Önizleme butonuna basıldı.");
            OnPreviewPressed?.Invoke();
        }

        private void HandleShare()
        {
            Debug.Log("[RecordingUI] Paylaş butonuna basıldı.");
            OnSharePressed?.Invoke();
        }

        private void HandleBack()
        {
            if (_currentState == RecordingState.Recording)
            {
                StopRecording();
            }

            Debug.Log("[RecordingUI] Geri butonuna basıldı.");
            OnBackPressed?.Invoke();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetState(AppState.AssetPlacement);
            }
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// UI'ı başlangıç durumuna getirir.
        /// </summary>
        private void ResetUI()
        {
            _currentState = RecordingState.Idle;
            _recordingTime = 0f;

            UpdateTimerDisplay();
            UpdateRecordButtonVisual();
            UpdateButtonStates();
            SetCountdownVisible(false);
            StopPulseAnimation();

            if (_flashOverlay != null)
            {
                Color c = _flashOverlay.color;
                c.a = 0f;
                _flashOverlay.color = c;
            }
        }

        /// <summary>
        /// Süre göstergesini günceller.
        /// </summary>
        private void UpdateTimerDisplay()
        {
            if (_timerText != null)
            {
                _timerText.text = FormattedTime;
            }
        }

        /// <summary>
        /// Kayıt butonunun görselini duruma göre günceller.
        /// </summary>
        private void UpdateRecordButtonVisual()
        {
            if (_recordButtonIcon != null)
            {
                _recordButtonIcon.sprite = _currentState == RecordingState.Recording
                    ? _recordingSprite
                    : _idleSprite;
            }
        }

        /// <summary>
        /// Buton erişilebilirliğini günceller.
        /// </summary>
        private void UpdateButtonStates()
        {
            bool isIdle = _currentState == RecordingState.Idle;

            if (_previewButton != null)
            {
                _previewButton.interactable = isIdle && _hasLastRecording;
            }

            if (_shareButton != null)
            {
                _shareButton.interactable = isIdle && _hasLastRecording;
            }

            if (_backButton != null)
            {
                _backButton.interactable = isIdle;
            }
        }

        /// <summary>
        /// Geri sayım panelinin görünürlüğünü ayarlar.
        /// </summary>
        private void SetCountdownVisible(bool visible)
        {
            if (_countdownPanel != null)
            {
                _countdownPanel.alpha = visible ? 1f : 0f;
                _countdownPanel.interactable = visible;
                _countdownPanel.blocksRaycasts = visible;
                _countdownPanel.gameObject.SetActive(visible);
            }
        }

        #endregion

        #region Animations

        /// <summary>
        /// Kayıt göstergesi yanıp sönme animasyonunu başlatır.
        /// </summary>
        private void StartPulseAnimation()
        {
            StopPulseAnimation();
            _pulseCoroutine = StartCoroutine(PulseCoroutine());
        }

        /// <summary>
        /// Yanıp sönme animasyonunu durdurur.
        /// </summary>
        private void StopPulseAnimation()
        {
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }

            if (_recordingIndicator != null)
            {
                Color c = _recordingIndicator.color;
                c.a = _currentState == RecordingState.Recording ? 1f : 0f;
                _recordingIndicator.color = c;
            }
        }

        /// <summary>
        /// Kayıt göstergesini sürekli yanıp söndüren coroutine.
        /// </summary>
        private IEnumerator PulseCoroutine()
        {
            while (_currentState == RecordingState.Recording)
            {
                if (_recordingIndicator != null)
                {
                    float alpha = Mathf.Lerp(_pulseMinAlpha, _pulseMaxAlpha,
                        (Mathf.Sin(Time.time * _pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f);

                    Color c = _recordingIndicator.color;
                    c.a = alpha;
                    _recordingIndicator.color = c;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Ekran kenarı flaş efektini günceller (Update'de çağrılır).
        /// </summary>
        private void AnimateFlash()
        {
            if (_flashOverlay == null) return;

            float alpha = Mathf.Lerp(0f, _flashMaxAlpha,
                (Mathf.Sin(Time.time * _flashSpeed * Mathf.PI * 2f) + 1f) * 0.5f);

            Color c = _flashOverlay.color;
            c.a = alpha;
            _flashOverlay.color = c;
        }

        /// <summary>
        /// Tüm animasyonları durdurur.
        /// </summary>
        private void StopAllAnimations()
        {
            StopPulseAnimation();

            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }

            if (_flashOverlay != null)
            {
                Color c = _flashOverlay.color;
                c.a = 0f;
                _flashOverlay.color = c;
            }
        }

        #endregion
    }
}
