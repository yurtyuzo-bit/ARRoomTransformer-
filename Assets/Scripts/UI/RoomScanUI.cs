using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace ARRoomTransformer
{
    /// <summary>
    /// Tarama aşamalarını tanımlar.
    /// Her aşamada kullanıcıya farklı talimatlar gösterilir.
    /// </summary>
    public enum ScanPhase
    {
        /// <summary>Başlatma - AR oturumu hazırlanıyor.</summary>
        Initializing,
        /// <summary>Yüzey tarama aşaması.</summary>
        ScanningPlanes,
        /// <summary>Köşe işaretleme aşaması.</summary>
        MarkingCorners,
        /// <summary>Tarama onayı aşaması.</summary>
        Confirming,
        /// <summary>Tarama tamamlandı.</summary>
        Completed
    }

    /// <summary>
    /// Oda tarama arayüzünü yönetir.
    /// Tarama ilerleme durumunu, talimat metinlerini, köşe sayacını ve
    /// düzlem algılama bilgilerini gösterir. Aşamalara göre talimatlar değişir.
    /// </summary>
    public class RoomScanUI : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Talimat Alanı")]
        [SerializeField, Tooltip("Ana talimat metni (aşamaya göre değişir).")]
        private TextMeshProUGUI _instructionText;

        [SerializeField, Tooltip("Alt bilgi / ipucu metni.")]
        private TextMeshProUGUI _hintText;

        [Header("İlerleme Göstergesi")]
        [SerializeField, Tooltip("Tarama ilerleme çubuğu.")]
        private Slider _progressBar;

        [SerializeField, Tooltip("İlerleme yüzde metni.")]
        private TextMeshProUGUI _progressPercentText;

        [Header("Köşe Bilgisi")]
        [SerializeField, Tooltip("İşaretlenen köşe sayısını gösteren metin.")]
        private TextMeshProUGUI _cornerCountText;

        [SerializeField, Tooltip("Minimum gerekli köşe sayısı.")]
        private int _minCornerCount = 4;

        [SerializeField, Tooltip("Maksimum köşe sayısı.")]
        private int _maxCornerCount = 20;

        [Header("Düzlem Algılama")]
        [SerializeField, Tooltip("Algılanan düzlem sayısını gösteren metin.")]
        private TextMeshProUGUI _planeCountText;

        [SerializeField, Tooltip("Düzlem algılama göstergesi (ikon veya grafik).")]
        private Image _planeIndicatorIcon;

        [SerializeField, Tooltip("Düzlem algılandığında kullanılacak renk.")]
        private Color _planeDetectedColor = new Color(0.2f, 0.8f, 0.4f, 1f);

        [SerializeField, Tooltip("Düzlem algılanmadığında kullanılacak renk.")]
        private Color _planeNotDetectedColor = new Color(0.8f, 0.3f, 0.2f, 1f);

        [Header("Butonlar")]
        [SerializeField, Tooltip("Son köşeyi geri al butonu.")]
        private Button _undoButton;

        [SerializeField, Tooltip("Taramayı onayla butonu.")]
        private Button _confirmButton;

        [SerializeField, Tooltip("Taramayı sıfırla butonu.")]
        private Button _resetButton;

        [SerializeField, Tooltip("Geri dön butonu.")]
        private Button _backButton;

        [Header("Animasyon")]
        [SerializeField, Tooltip("Talimat metninin kaybolma-belirme süresi.")]
        private float _instructionFadeDuration = 0.3f;

        #endregion

        #region Events

        /// <summary>Geri Al butonuna basıldığında tetiklenir.</summary>
        [Header("Olaylar")]
        public UnityEvent OnUndoPressed;

        /// <summary>Onayla butonuna basıldığında tetiklenir.</summary>
        public UnityEvent OnConfirmPressed;

        /// <summary>Sıfırla butonuna basıldığında tetiklenir.</summary>
        public UnityEvent OnResetPressed;

        /// <summary>Geri butonuna basıldığında tetiklenir.</summary>
        public UnityEvent OnBackPressed;

        /// <summary>Tarama aşaması değiştiğinde tetiklenir.</summary>
        public UnityEvent<ScanPhase> OnScanPhaseChanged;

        #endregion

        #region Private State

        private ScanPhase _currentPhase = ScanPhase.Initializing;
        private int _cornerCount;
        private int _detectedPlaneCount;
        private Coroutine _instructionFadeCoroutine;

        /// <summary>
        /// Tarama aşamalarına göre talimat metinleri sözlüğü.
        /// </summary>
        private static readonly Dictionary<ScanPhase, string> PhaseInstructions = new Dictionary<ScanPhase, string>
        {
            { ScanPhase.Initializing,    "AR oturumu hazırlanıyor..." },
            { ScanPhase.ScanningPlanes,  "Telefonunuzu yavaşça çevirin\nZemin ve duvarları tarayın" },
            { ScanPhase.MarkingCorners,  "Köşeleri işaretleyin\nOdanın köşelerine dokunun" },
            { ScanPhase.Confirming,      "Taramayı kontrol edin\nDoğruysa onaylayın" },
            { ScanPhase.Completed,       "Tarama tamamlandı!" }
        };

        private static readonly Dictionary<ScanPhase, string> PhaseHints = new Dictionary<ScanPhase, string>
        {
            { ScanPhase.Initializing,    "Lütfen bekleyin..." },
            { ScanPhase.ScanningPlanes,  "İyi aydınlatılmış ortamlarda daha iyi sonuç alırsınız" },
            { ScanPhase.MarkingCorners,  "En az 4 köşe işaretleyin" },
            { ScanPhase.Confirming,      "Yanlışsa 'Sıfırla' ile baştan başlayın" },
            { ScanPhase.Completed,       "Varlık yerleştirmeye geçebilirsiniz" }
        };

        #endregion

        #region Properties

        /// <summary>Mevcut tarama aşaması.</summary>
        public ScanPhase CurrentPhase => _currentPhase;

        /// <summary>İşaretlenen köşe sayısı.</summary>
        public int CornerCount => _cornerCount;

        /// <summary>Algılanan düzlem sayısı.</summary>
        public int DetectedPlaneCount => _detectedPlaneCount;

        /// <summary>Minimum köşe sayısı karşılandı mı?</summary>
        public bool HasMinimumCorners => _cornerCount >= _minCornerCount;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            BindButtons();
            RefreshUI();
        }

        private void OnDisable()
        {
            UnbindButtons();

            if (_instructionFadeCoroutine != null)
            {
                StopCoroutine(_instructionFadeCoroutine);
                _instructionFadeCoroutine = null;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Tarama aşamasını değiştirir ve talimatları günceller.
        /// </summary>
        /// <param name="phase">Yeni tarama aşaması.</param>
        public void SetScanPhase(ScanPhase phase)
        {
            if (_currentPhase == phase) return;

            _currentPhase = phase;
            UpdateInstructionWithAnimation();
            UpdateButtonStates();
            OnScanPhaseChanged?.Invoke(phase);

            Debug.Log($"[RoomScanUI] Tarama aşaması değişti: {phase}");
        }

        /// <summary>
        /// Köşe sayısını günceller ve UI'ı yeniler.
        /// </summary>
        /// <param name="count">Yeni köşe sayısı.</param>
        public void SetCornerCount(int count)
        {
            _cornerCount = Mathf.Clamp(count, 0, _maxCornerCount);
            UpdateCornerDisplay();
            UpdateButtonStates();
        }

        /// <summary>
        /// Köşe sayısını bir artırır.
        /// </summary>
        public void IncrementCornerCount()
        {
            SetCornerCount(_cornerCount + 1);
        }

        /// <summary>
        /// Son köşeyi geri alır (köşe sayısını bir azaltır).
        /// </summary>
        public void DecrementCornerCount()
        {
            SetCornerCount(_cornerCount - 1);
        }

        /// <summary>
        /// Algılanan düzlem sayısını günceller.
        /// </summary>
        /// <param name="count">Algılanan düzlem sayısı.</param>
        public void SetDetectedPlaneCount(int count)
        {
            _detectedPlaneCount = Mathf.Max(0, count);
            UpdatePlaneDisplay();
        }

        /// <summary>
        /// Tarama ilerleme yüzdesini günceller (0-1 arası).
        /// </summary>
        /// <param name="progress">İlerleme değeri (0.0 - 1.0).</param>
        public void SetProgress(float progress)
        {
            float clamped = Mathf.Clamp01(progress);

            if (_progressBar != null)
            {
                _progressBar.value = clamped;
            }

            if (_progressPercentText != null)
            {
                _progressPercentText.text = $"%{Mathf.RoundToInt(clamped * 100)}";
            }
        }

        /// <summary>
        /// Tüm tarama verilerini sıfırlar ve UI'ı başlangıç durumuna getirir.
        /// </summary>
        public void ResetScan()
        {
            _cornerCount = 0;
            _detectedPlaneCount = 0;
            _currentPhase = ScanPhase.Initializing;
            SetProgress(0f);
            RefreshUI();
        }

        #endregion

        #region Button Handlers

        private void BindButtons()
        {
            if (_undoButton != null) _undoButton.onClick.AddListener(HandleUndo);
            if (_confirmButton != null) _confirmButton.onClick.AddListener(HandleConfirm);
            if (_resetButton != null) _resetButton.onClick.AddListener(HandleReset);
            if (_backButton != null) _backButton.onClick.AddListener(HandleBack);
        }

        private void UnbindButtons()
        {
            if (_undoButton != null) _undoButton.onClick.RemoveListener(HandleUndo);
            if (_confirmButton != null) _confirmButton.onClick.RemoveListener(HandleConfirm);
            if (_resetButton != null) _resetButton.onClick.RemoveListener(HandleReset);
            if (_backButton != null) _backButton.onClick.RemoveListener(HandleBack);
        }

        private void HandleUndo()
        {
            Debug.Log("[RoomScanUI] Geri Al butonuna basıldı.");
            DecrementCornerCount();
            OnUndoPressed?.Invoke();
        }

        private void HandleConfirm()
        {
            if (!HasMinimumCorners)
            {
                Debug.LogWarning($"[RoomScanUI] Onay için en az {_minCornerCount} köşe gerekli.");
                return;
            }

            Debug.Log("[RoomScanUI] Onayla butonuna basıldı.");
            SetScanPhase(ScanPhase.Completed);
            OnConfirmPressed?.Invoke();
        }

        private void HandleReset()
        {
            Debug.Log("[RoomScanUI] Sıfırla butonuna basıldı.");
            ResetScan();
            OnResetPressed?.Invoke();
        }

        private void HandleBack()
        {
            Debug.Log("[RoomScanUI] Geri butonuna basıldı.");
            OnBackPressed?.Invoke();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetState(AppState.MainMenu);
            }
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// Tüm UI öğelerini mevcut duruma göre yeniler.
        /// </summary>
        private void RefreshUI()
        {
            UpdateInstructionText();
            UpdateCornerDisplay();
            UpdatePlaneDisplay();
            UpdateButtonStates();
        }

        /// <summary>
        /// Talimat metnini mevcut aşamaya göre animasyonla günceller.
        /// </summary>
        private void UpdateInstructionWithAnimation()
        {
            if (_instructionFadeCoroutine != null)
            {
                StopCoroutine(_instructionFadeCoroutine);
            }
            _instructionFadeCoroutine = StartCoroutine(FadeInstructionCoroutine());
        }

        /// <summary>
        /// Talimat metnini animasyonsuz günceller.
        /// </summary>
        private void UpdateInstructionText()
        {
            if (_instructionText != null && PhaseInstructions.TryGetValue(_currentPhase, out string instruction))
            {
                _instructionText.text = instruction;
            }

            if (_hintText != null && PhaseHints.TryGetValue(_currentPhase, out string hint))
            {
                _hintText.text = hint;
            }
        }

        /// <summary>
        /// Köşe sayısı göstergesini günceller.
        /// </summary>
        private void UpdateCornerDisplay()
        {
            if (_cornerCountText != null)
            {
                _cornerCountText.text = $"Köşeler: {_cornerCount} / {_minCornerCount}+";

                // Minimum karşılandıysa yeşil, değilse beyaz
                _cornerCountText.color = HasMinimumCorners
                    ? new Color(0.2f, 0.9f, 0.4f, 1f)
                    : Color.white;
            }
        }

        /// <summary>
        /// Düzlem algılama göstergesini günceller.
        /// </summary>
        private void UpdatePlaneDisplay()
        {
            if (_planeCountText != null)
            {
                _planeCountText.text = $"Düzlemler: {_detectedPlaneCount}";
            }

            if (_planeIndicatorIcon != null)
            {
                _planeIndicatorIcon.color = _detectedPlaneCount > 0
                    ? _planeDetectedColor
                    : _planeNotDetectedColor;
            }
        }

        /// <summary>
        /// Buton durumlarını mevcut aşamaya göre günceller.
        /// </summary>
        private void UpdateButtonStates()
        {
            bool isMarking = _currentPhase == ScanPhase.MarkingCorners;
            bool isConfirming = _currentPhase == ScanPhase.Confirming;
            bool isCompleted = _currentPhase == ScanPhase.Completed;

            // Geri al butonu: Sadece köşe işaretleme aşamasında ve köşe varsa aktif
            if (_undoButton != null)
            {
                _undoButton.interactable = isMarking && _cornerCount > 0;
            }

            // Onayla butonu: Köşe işaretleme veya onay aşamasında, minimum köşe varsa aktif
            if (_confirmButton != null)
            {
                _confirmButton.interactable = (isMarking || isConfirming) && HasMinimumCorners;
            }

            // Sıfırla butonu: Başlatma hariç her aşamada aktif, tamamlandıysa pasif
            if (_resetButton != null)
            {
                _resetButton.interactable = _currentPhase != ScanPhase.Initializing && !isCompleted;
            }
        }

        #endregion

        #region Animations

        /// <summary>
        /// Talimat metnini fade-out → güncelle → fade-in şeklinde animasyonlar.
        /// </summary>
        private IEnumerator FadeInstructionCoroutine()
        {
            // Fade out
            if (_instructionText != null)
            {
                float elapsed = 0f;
                Color startColor = _instructionText.color;
                Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

                while (elapsed < _instructionFadeDuration * 0.5f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / (_instructionFadeDuration * 0.5f));
                    _instructionText.color = Color.Lerp(startColor, targetColor, t);
                    yield return null;
                }

                // Metni güncelle
                UpdateInstructionText();

                // Fade in
                elapsed = 0f;
                startColor = targetColor;
                targetColor = new Color(startColor.r, startColor.g, startColor.b, 1f);

                while (elapsed < _instructionFadeDuration * 0.5f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / (_instructionFadeDuration * 0.5f));
                    _instructionText.color = Color.Lerp(startColor, targetColor, t);
                    yield return null;
                }

                _instructionText.color = new Color(_instructionText.color.r, _instructionText.color.g, _instructionText.color.b, 1f);
            }

            _instructionFadeCoroutine = null;
        }

        #endregion
    }
}
