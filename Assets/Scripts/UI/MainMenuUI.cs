using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace ARRoomTransformer
{
    /// <summary>
    /// Ana menü arayüzünü yönetir.
    /// Yeni Tarama, Sahne Yükle ve Ayarlar butonlarını içerir.
    /// Animasyonlu arka plan ve sürüm bilgisi gösterir.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Butonlar")]
        [SerializeField, Tooltip("Yeni tarama başlatma butonu.")]
        private Button _newScanButton;

        [SerializeField, Tooltip("Kaydedilmiş sahne yükleme butonu.")]
        private Button _loadSceneButton;

        [SerializeField, Tooltip("Ayarlar butonu.")]
        private Button _settingsButton;

        [Header("Metin Alanları")]
        [SerializeField, Tooltip("Uygulama başlığı.")]
        private TextMeshProUGUI _titleText;

        [SerializeField, Tooltip("Uygulama sürüm bilgisi metni.")]
        private TextMeshProUGUI _versionText;

        [SerializeField, Tooltip("Alt bilgi / telif hakkı metni.")]
        private TextMeshProUGUI _footerText;

        [Header("Animasyonlu Arka Plan")]
        [SerializeField, Tooltip("Arka plan RawImage (gradient veya doku kaydırma için).")]
        private RawImage _backgroundImage;

        [SerializeField, Tooltip("Arka plan UV kaydırma hızı.")]
        private Vector2 _backgroundScrollSpeed = new Vector2(0.02f, 0.01f);

        [SerializeField, Tooltip("Arka plan renk geçişi aktif mi?")]
        private bool _enableColorCycle = true;

        [SerializeField, Tooltip("Arka plan renk geçiş hızı.")]
        private float _colorCycleSpeed = 0.15f;

        [SerializeField, Tooltip("Arka plan renk paleti.")]
        private Color[] _backgroundColors = new Color[]
        {
            new Color(0.1f, 0.1f, 0.3f, 1f),
            new Color(0.15f, 0.05f, 0.25f, 1f),
            new Color(0.05f, 0.15f, 0.3f, 1f)
        };

        [Header("Buton Animasyonu")]
        [SerializeField, Tooltip("Butonların giriş animasyonu süresi.")]
        private float _buttonAnimDuration = 0.4f;

        [SerializeField, Tooltip("Butonlar arası animasyon gecikmesi.")]
        private float _buttonAnimStagger = 0.12f;

        #endregion

        #region Events

        /// <summary>Yeni Tarama butonuna basıldığında tetiklenir.</summary>
        [Header("Olaylar")]
        public UnityEvent OnNewScanPressed;

        /// <summary>Sahne Yükle butonuna basıldığında tetiklenir.</summary>
        public UnityEvent OnLoadScenePressed;

        /// <summary>Ayarlar butonuna basıldığında tetiklenir.</summary>
        public UnityEvent OnSettingsPressed;

        #endregion

        #region Private State

        private RectTransform[] _buttonRects;
        private Vector2 _bgUvOffset;
        private int _currentColorIndex;
        private int _nextColorIndex = 1;
        private float _colorLerpT;
        private Coroutine _entryAnimCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheButtonRects();
            SetVersionText();
        }

        private void OnEnable()
        {
            BindButtons();

            // Panel her açıldığında buton giriş animasyonunu çalıştır
            if (_entryAnimCoroutine != null)
            {
                StopCoroutine(_entryAnimCoroutine);
            }
            _entryAnimCoroutine = StartCoroutine(PlayButtonEntryAnimation());
        }

        private void OnDisable()
        {
            UnbindButtons();

            if (_entryAnimCoroutine != null)
            {
                StopCoroutine(_entryAnimCoroutine);
                _entryAnimCoroutine = null;
            }
        }

        private void Update()
        {
            AnimateBackground();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Sürüm metnini manuel olarak günceller.
        /// </summary>
        /// <param name="version">Gösterilecek sürüm bilgisi.</param>
        public void SetVersion(string version)
        {
            if (_versionText != null)
            {
                _versionText.text = version;
            }
        }

        /// <summary>
        /// Başlık metnini günceller.
        /// </summary>
        /// <param name="title">Gösterilecek başlık.</param>
        public void SetTitle(string title)
        {
            if (_titleText != null)
            {
                _titleText.text = title;
            }
        }

        /// <summary>
        /// Alt bilgi metnini günceller.
        /// </summary>
        /// <param name="footer">Gösterilecek alt bilgi.</param>
        public void SetFooter(string footer)
        {
            if (_footerText != null)
            {
                _footerText.text = footer;
            }
        }

        #endregion

        #region Button Handlers

        private void BindButtons()
        {
            if (_newScanButton != null) _newScanButton.onClick.AddListener(HandleNewScan);
            if (_loadSceneButton != null) _loadSceneButton.onClick.AddListener(HandleLoadScene);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(HandleSettings);
        }

        private void UnbindButtons()
        {
            if (_newScanButton != null) _newScanButton.onClick.RemoveListener(HandleNewScan);
            if (_loadSceneButton != null) _loadSceneButton.onClick.RemoveListener(HandleLoadScene);
            if (_settingsButton != null) _settingsButton.onClick.RemoveListener(HandleSettings);
        }

        private void HandleNewScan()
        {
            Debug.Log("[MainMenuUI] Yeni Tarama butonuna basıldı.");
            OnNewScanPressed?.Invoke();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetState(AppState.RoomScanning);
            }
        }

        private void HandleLoadScene()
        {
            Debug.Log("[MainMenuUI] Sahne Yükle butonuna basıldı.");
            OnLoadScenePressed?.Invoke();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetState(AppState.SceneLoading);
            }
        }

        private void HandleSettings()
        {
            Debug.Log("[MainMenuUI] Ayarlar butonuna basıldı.");
            OnSettingsPressed?.Invoke();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetState(AppState.Settings);
            }
        }

        #endregion

        #region Animation

        /// <summary>
        /// Arka plan UV kaydırma ve renk geçiş animasyonunu günceller.
        /// </summary>
        private void AnimateBackground()
        {
            if (_backgroundImage == null) return;

            // UV kaydırma
            _bgUvOffset += _backgroundScrollSpeed * Time.deltaTime;
            _backgroundImage.uvRect = new Rect(_bgUvOffset, _backgroundImage.uvRect.size);

            // Renk geçişi
            if (_enableColorCycle && _backgroundColors != null && _backgroundColors.Length >= 2)
            {
                _colorLerpT += _colorCycleSpeed * Time.deltaTime;

                if (_colorLerpT >= 1f)
                {
                    _colorLerpT = 0f;
                    _currentColorIndex = _nextColorIndex;
                    _nextColorIndex = (_nextColorIndex + 1) % _backgroundColors.Length;
                }

                _backgroundImage.color = Color.Lerp(
                    _backgroundColors[_currentColorIndex],
                    _backgroundColors[_nextColorIndex],
                    _colorLerpT
                );
            }
        }

        /// <summary>
        /// Butonları sırayla aşağıdan yukarıya slide-in animasyonuyla gösterir.
        /// </summary>
        private IEnumerator PlayButtonEntryAnimation()
        {
            if (_buttonRects == null || _buttonRects.Length == 0)
            {
                yield break;
            }

            // Başlangıçta butonları gizle (aşağıya kaydır)
            float offsetY = 80f;
            foreach (var rect in _buttonRects)
            {
                if (rect == null) continue;
                var cg = rect.GetComponent<CanvasGroup>();
                if (cg == null) cg = rect.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }

            // Her butonu sırayla animasyonla göster
            for (int i = 0; i < _buttonRects.Length; i++)
            {
                if (_buttonRects[i] == null) continue;

                StartCoroutine(AnimateSingleButton(_buttonRects[i], offsetY));
                yield return new WaitForSeconds(_buttonAnimStagger);
            }

            _entryAnimCoroutine = null;
        }

        /// <summary>
        /// Tek bir butonu slide-up + fade-in ile animasyonlar.
        /// </summary>
        private IEnumerator AnimateSingleButton(RectTransform rect, float offsetY)
        {
            var cg = rect.GetComponent<CanvasGroup>();
            if (cg == null) yield break;

            Vector2 originalPos = rect.anchoredPosition;
            Vector2 startPos = originalPos + Vector2.down * offsetY;

            float elapsed = 0f;
            while (elapsed < _buttonAnimDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _buttonAnimDuration);
                // Ease-out cubic
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                rect.anchoredPosition = Vector2.Lerp(startPos, originalPos, eased);
                cg.alpha = eased;
                yield return null;
            }

            rect.anchoredPosition = originalPos;
            cg.alpha = 1f;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Buton RectTransform'larını önbelleğe alır.
        /// </summary>
        private void CacheButtonRects()
        {
            var rects = new System.Collections.Generic.List<RectTransform>();

            if (_newScanButton != null) rects.Add(_newScanButton.GetComponent<RectTransform>());
            if (_loadSceneButton != null) rects.Add(_loadSceneButton.GetComponent<RectTransform>());
            if (_settingsButton != null) rects.Add(_settingsButton.GetComponent<RectTransform>());

            _buttonRects = rects.ToArray();
        }

        /// <summary>
        /// Sürüm metnini Application.version'dan alarak ayarlar.
        /// </summary>
        private void SetVersionText()
        {
            if (_versionText != null)
            {
                _versionText.text = $"v{Application.version}";
            }
        }

        #endregion
    }
}
