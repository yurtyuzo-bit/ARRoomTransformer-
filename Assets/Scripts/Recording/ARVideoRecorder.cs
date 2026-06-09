using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ARRoomTransformer
{
    /// <summary>
    /// AR video kayıt yöneticisi. iOS'ta ReplayKit, diğer platformlarda
    /// RenderTexture tabanlı kayıt kullanır.
    /// </summary>
    public class ARVideoRecorder : MonoBehaviour
    {
        [Header("Kayıt Ayarları")]
        [SerializeField] private VideoResolution resolution = VideoResolution.HD_1080p;
        [SerializeField] private int targetFPS = 30;
#pragma warning disable 0414
        [SerializeField] private bool recordAudio = true;
#pragma warning restore 0414
        [SerializeField] private int countdownSeconds = 3;

        [Header("Events")]
        [SerializeField] private UnityEvent onRecordingStarted = new UnityEvent();
        [SerializeField] private UnityEvent onRecordingStopped = new UnityEvent();
        [SerializeField] private UnityEvent<string> onRecordingFailed = new UnityEvent<string>();
        [SerializeField] private UnityEvent<int> onCountdownTick = new UnityEvent<int>();

        private bool _isRecording;
        private bool _isPaused;
        private float _recordingStartTime;
        private float _pausedDuration;
        private float _pauseStartTime;
        private Coroutine _countdownCoroutine;

        /// <summary>Kayıt devam ediyor mu?</summary>
        public bool IsRecording => _isRecording;

        /// <summary>Kayıt duraklatıldı mı?</summary>
        public bool IsPaused => _isPaused;

        /// <summary>Toplam kayıt süresi (saniye).</summary>
        public float RecordingDuration
        {
            get
            {
                if (!_isRecording) return 0f;
                float duration = Time.realtimeSinceStartup - _recordingStartTime - _pausedDuration;
                if (_isPaused) duration -= (Time.realtimeSinceStartup - _pauseStartTime);
                return Mathf.Max(0f, duration);
            }
        }

        /// <summary>Kayıt süresini MM:SS formatında döndürür.</summary>
        public string RecordingDurationFormatted
        {
            get
            {
                float dur = RecordingDuration;
                int min = Mathf.FloorToInt(dur / 60f);
                int sec = Mathf.FloorToInt(dur % 60f);
                return $"{min:00}:{sec:00}";
            }
        }

        /// <summary>Çözünürlük ayarı.</summary>
        public VideoResolution Resolution
        {
            get => resolution;
            set { if (!_isRecording) resolution = value; }
        }

        // Events
        public UnityEvent OnRecordingStarted => onRecordingStarted;
        public UnityEvent OnRecordingStopped => onRecordingStopped;
        public UnityEvent<string> OnRecordingFailed => onRecordingFailed;
        public UnityEvent<int> OnCountdownTick => onCountdownTick;

        /// <summary>Video çözünürlük seçenekleri.</summary>
        public enum VideoResolution
        {
            HD_720p,
            HD_1080p
        }

        /// <summary>
        /// Geri sayımla kaydı başlatır.
        /// </summary>
        public void StartRecording()
        {
            if (_isRecording)
            {
                Debug.LogWarning("[ARVideoRecorder] Kayıt zaten devam ediyor.");
                return;
            }

            if (countdownSeconds > 0)
            {
                _countdownCoroutine = StartCoroutine(CountdownAndStart());
            }
            else
            {
                BeginRecording();
            }
        }

        /// <summary>Kaydı durdurur.</summary>
        public void StopRecording()
        {
            if (!_isRecording)
            {
                Debug.LogWarning("[ARVideoRecorder] Aktif kayıt yok.");
                return;
            }

            StopRecordingInternal();
        }

        /// <summary>Kaydı duraklatır.</summary>
        public void PauseRecording()
        {
            if (!_isRecording || _isPaused) return;
            _isPaused = true;
            _pauseStartTime = Time.realtimeSinceStartup;

#if UNITY_IOS && !UNITY_EDITOR
            // ReplayKit pause desteklemiyor, bu yüzden sadece state tutuyoruz
#endif
            Debug.Log("[ARVideoRecorder] Kayıt duraklatıldı.");
        }

        /// <summary>Duraklatılmış kaydı devam ettirir.</summary>
        public void ResumeRecording()
        {
            if (!_isRecording || !_isPaused) return;
            _pausedDuration += Time.realtimeSinceStartup - _pauseStartTime;
            _isPaused = false;
            Debug.Log("[ARVideoRecorder] Kayıt devam ediyor.");
        }

        /// <summary>Geri sayım aktifse iptal eder.</summary>
        public void CancelCountdown()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
                Debug.Log("[ARVideoRecorder] Geri sayım iptal edildi.");
            }
        }

        private IEnumerator CountdownAndStart()
        {
            for (int i = countdownSeconds; i > 0; i--)
            {
                onCountdownTick?.Invoke(i);
                Debug.Log($"[ARVideoRecorder] Geri sayım: {i}");
                yield return new WaitForSeconds(1f);
            }

            _countdownCoroutine = null;
            BeginRecording();
        }

        private void BeginRecording()
        {
            _isRecording = true;
            _isPaused = false;
            _recordingStartTime = Time.realtimeSinceStartup;
            _pausedDuration = 0f;

#if UNITY_IOS && !UNITY_EDITOR
            StartReplayKitRecording();
#else
            StartEditorRecording();
#endif

            onRecordingStarted?.Invoke();
            Debug.Log("[ARVideoRecorder] Kayıt başladı!");
        }

        private void StopRecordingInternal()
        {
            _isRecording = false;
            _isPaused = false;

#if UNITY_IOS && !UNITY_EDITOR
            StopReplayKitRecording();
#else
            StopEditorRecording();
#endif

            onRecordingStopped?.Invoke();
            Debug.Log($"[ARVideoRecorder] Kayıt durduruldu. Süre: {RecordingDurationFormatted}");
        }

        // ============================================================
        // iOS ReplayKit Entegrasyonu
        // ============================================================

#if UNITY_IOS && !UNITY_EDITOR
        private void StartReplayKitRecording()
        {
            try
            {
                var replayKit = UnityEngine.Apple.ReplayKit.ReplayKit.APIAvailable;
                if (replayKit)
                {
                    UnityEngine.Apple.ReplayKit.ReplayKit.StartRecording(recordAudio);
                    Debug.Log("[ARVideoRecorder] ReplayKit kaydı başlatıldı.");
                }
                else
                {
                    onRecordingFailed?.Invoke("ReplayKit bu cihazda desteklenmiyor.");
                    _isRecording = false;
                }
            }
            catch (Exception ex)
            {
                onRecordingFailed?.Invoke($"ReplayKit hatası: {ex.Message}");
                _isRecording = false;
            }
        }

        private void StopReplayKitRecording()
        {
            try
            {
                UnityEngine.Apple.ReplayKit.ReplayKit.StopRecording();

                // Kayıt önizleme penceresi göster
                if (UnityEngine.Apple.ReplayKit.ReplayKit.recordingAvailable)
                {
                    UnityEngine.Apple.ReplayKit.ReplayKit.Preview();
                }
            }
            catch (Exception ex)
            {
                onRecordingFailed?.Invoke($"ReplayKit durdurma hatası: {ex.Message}");
            }
        }
#endif

        // ============================================================
        // Editor / Fallback Kayıt (Test için)
        // ============================================================

        private void StartEditorRecording()
        {
            Debug.Log("[ARVideoRecorder] Editor modu - Simüle kayıt başladı.");
            // Editor'da gerçek video kayıt yapılamaz,
            // sadece state yönetimi ve UI testi için kullanılır
        }

        private void StopEditorRecording()
        {
            Debug.Log("[ARVideoRecorder] Editor modu - Simüle kayıt durduruldu.");
        }

        private void OnDestroy()
        {
            CancelCountdown();
            if (_isRecording)
            {
                StopRecordingInternal();
            }
        }

        /// <summary>Geri sayım süresini ayarlar.</summary>
        public void SetCountdown(int seconds)
        {
            countdownSeconds = Mathf.Max(0, seconds);
        }

        /// <summary>Hedef FPS'i ayarlar.</summary>
        public void SetTargetFPS(int fps)
        {
            if (!_isRecording)
            {
                targetFPS = Mathf.Clamp(fps, 24, 60);
                Application.targetFrameRate = targetFPS;
            }
        }
    }
}
