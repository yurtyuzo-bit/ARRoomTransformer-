using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ARRoomTransformer
{
    /// <summary>
    /// Uygulama durumlarını (state) tanımlar.
    /// Her durum, UI'da farklı bir panel grubuna karşılık gelir.
    /// </summary>
    public enum AppState
    {
        /// <summary>Ana menü durumu.</summary>
        MainMenu,
        /// <summary>Oda tarama durumu.</summary>
        RoomScanning,
        /// <summary>Varlık yerleştirme durumu.</summary>
        AssetPlacement,
        /// <summary>Video kayıt durumu.</summary>
        Recording,
        /// <summary>Sahne yükleme/listeleme durumu.</summary>
        SceneLoading,
        /// <summary>Ayarlar durumu.</summary>
        Settings
    }

    /// <summary>
    /// Tüm UI panellerini yönetir.
    /// <see cref="AppState"/> değişikliklerine göre panelleri gösterir/gizler.
    /// Paneller arası geçişlerde fade animasyonu uygular.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Singleton

        private static UIManager _instance;

        /// <summary>
        /// UIManager singleton erişim noktası.
        /// </summary>
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<UIManager>();
                    if (_instance == null)
                    {
                        Debug.LogError("[UIManager] Sahnede UIManager bulunamadı!");
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Inspector Fields

        [Header("Panel Referansları")]
        [SerializeField, Tooltip("Ana menü paneli.")]
        private CanvasGroup _mainMenuPanel;

        [SerializeField, Tooltip("Oda tarama paneli.")]
        private CanvasGroup _roomScanPanel;

        [SerializeField, Tooltip("Varlık yerleştirme paneli.")]
        private CanvasGroup _assetPlacementPanel;

        [SerializeField, Tooltip("Kayıt kontrolleri paneli.")]
        private CanvasGroup _recordingPanel;

        [SerializeField, Tooltip("Sahne yükleme/listeleme paneli.")]
        private CanvasGroup _sceneLoadPanel;

        [SerializeField, Tooltip("Ayarlar paneli.")]
        private CanvasGroup _settingsPanel;

        [Header("Geçiş Ayarları")]
        [SerializeField, Tooltip("Fade animasyonu süresi (saniye)."), Range(0.05f, 1f)]
        private float _fadeDuration = 0.25f;

        [SerializeField, Tooltip("Fade eğrisi.")]
        private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        #endregion

        #region Events

        /// <summary>
        /// Panel geçişi başladığında tetiklenir. (önceki durum, yeni durum)
        /// </summary>
        [Header("Olaylar")]
        public UnityEvent<AppState, AppState> OnPanelTransitionStarted;

        /// <summary>
        /// Panel geçişi tamamlandığında tetiklenir. (yeni durum)
        /// </summary>
        public UnityEvent<AppState> OnPanelTransitionCompleted;

        /// <summary>
        /// Uygulama durumu değiştiğinde tetiklenir.
        /// </summary>
        public UnityEvent<AppState> OnAppStateChanged;

        #endregion

        #region Private State

        private AppState _currentState = AppState.MainMenu;
        private Dictionary<AppState, CanvasGroup> _panelMap;
        private Coroutine _activeTransition;
        private bool _isTransitioning;

        #endregion

        #region Properties

        /// <summary>
        /// Mevcut uygulama durumu.
        /// </summary>
        public AppState CurrentState => _currentState;

        /// <summary>
        /// Geçiş animasyonu devam ediyor mu?
        /// </summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary>Ana menü paneli referansı.</summary>
        public CanvasGroup MainMenuPanel => _mainMenuPanel;

        /// <summary>Oda tarama paneli referansı.</summary>
        public CanvasGroup RoomScanPanel => _roomScanPanel;

        /// <summary>Varlık yerleştirme paneli referansı.</summary>
        public CanvasGroup AssetPlacementPanel => _assetPlacementPanel;

        /// <summary>Kayıt kontrolleri paneli referansı.</summary>
        public CanvasGroup RecordingPanel => _recordingPanel;

        /// <summary>Sahne yükleme paneli referansı.</summary>
        public CanvasGroup SceneLoadPanel => _sceneLoadPanel;

        /// <summary>Ayarlar paneli referansı.</summary>
        public CanvasGroup SettingsPanel => _settingsPanel;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton kontrolü
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[UIManager] Birden fazla UIManager bulundu. Bu nesne yok ediliyor.");
                Destroy(gameObject);
                return;
            }
            _instance = this;

            InitializePanelMap();
        }

        private void Start()
        {
            // Başlangıçta tüm panelleri gizle, sadece ana menüyü göster
            SetAllPanelsHidden();
            ShowPanelImmediate(_currentState);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Uygulama durumunu değiştirir ve ilgili panele geçiş yapar.
        /// Fade animasyonu uygulanır.
        /// </summary>
        /// <param name="newState">Geçiş yapılacak yeni durum.</param>
        public void SetState(AppState newState)
        {
            if (_currentState == newState)
            {
                Debug.LogWarning($"[UIManager] Zaten '{newState}' durumunda.");
                return;
            }

            if (_isTransitioning)
            {
                Debug.LogWarning("[UIManager] Geçiş animasyonu devam ediyor. Yeni geçiş engellendi.");
                return;
            }

            AppState previousState = _currentState;
            _currentState = newState;

            OnAppStateChanged?.Invoke(newState);

            if (_activeTransition != null)
            {
                StopCoroutine(_activeTransition);
            }

            _activeTransition = StartCoroutine(TransitionCoroutine(previousState, newState));
        }

        /// <summary>
        /// Belirtilen paneli animasyonsuz, anında gösterir.
        /// </summary>
        /// <param name="state">Gösterilecek panelin durumu.</param>
        public void ShowPanelImmediate(AppState state)
        {
            if (!_panelMap.TryGetValue(state, out CanvasGroup panel) || panel == null)
            {
                Debug.LogWarning($"[UIManager] '{state}' için panel atanmamış.");
                return;
            }

            SetCanvasGroupActive(panel, true);
        }

        /// <summary>
        /// Belirtilen paneli animasyonsuz, anında gizler.
        /// </summary>
        /// <param name="state">Gizlenecek panelin durumu.</param>
        public void HidePanelImmediate(AppState state)
        {
            if (!_panelMap.TryGetValue(state, out CanvasGroup panel) || panel == null)
            {
                return;
            }

            SetCanvasGroupActive(panel, false);
        }

        /// <summary>
        /// Tüm panelleri anında gizler.
        /// </summary>
        public void HideAllPanels()
        {
            SetAllPanelsHidden();
        }

        /// <summary>
        /// Belirtilen duruma ait panelin aktif olup olmadığını döndürür.
        /// </summary>
        /// <param name="state">Kontrol edilecek durum.</param>
        /// <returns>Panel görünür ve etkileşime açıksa true.</returns>
        public bool IsPanelActive(AppState state)
        {
            if (_panelMap.TryGetValue(state, out CanvasGroup panel) && panel != null)
            {
                return panel.alpha > 0.99f && panel.interactable;
            }
            return false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Panel haritasını oluşturur.
        /// </summary>
        private void InitializePanelMap()
        {
            _panelMap = new Dictionary<AppState, CanvasGroup>
            {
                { AppState.MainMenu, _mainMenuPanel },
                { AppState.RoomScanning, _roomScanPanel },
                { AppState.AssetPlacement, _assetPlacementPanel },
                { AppState.Recording, _recordingPanel },
                { AppState.SceneLoading, _sceneLoadPanel },
                { AppState.Settings, _settingsPanel }
            };

            // Null kontrolleri
            foreach (var kvp in _panelMap)
            {
                if (kvp.Value == null)
                {
                    Debug.LogWarning($"[UIManager] '{kvp.Key}' için panel referansı atanmamış.");
                }
            }
        }

        /// <summary>
        /// Tüm panelleri gizler.
        /// </summary>
        private void SetAllPanelsHidden()
        {
            foreach (var kvp in _panelMap)
            {
                if (kvp.Value != null)
                {
                    SetCanvasGroupActive(kvp.Value, false);
                }
            }
        }

        /// <summary>
        /// CanvasGroup'u aktif veya pasif yapar.
        /// </summary>
        private void SetCanvasGroupActive(CanvasGroup group, bool active)
        {
            if (group == null) return;

            group.alpha = active ? 1f : 0f;
            group.interactable = active;
            group.blocksRaycasts = active;
            group.gameObject.SetActive(active);
        }

        /// <summary>
        /// İki panel arasında fade geçişi yapar.
        /// Eski panel fade-out, yeni panel fade-in şeklinde çalışır.
        /// </summary>
        private IEnumerator TransitionCoroutine(AppState fromState, AppState toState)
        {
            _isTransitioning = true;
            OnPanelTransitionStarted?.Invoke(fromState, toState);

            _panelMap.TryGetValue(fromState, out CanvasGroup fromPanel);
            _panelMap.TryGetValue(toState, out CanvasGroup toPanel);

            // --- Fade Out (eski panel) ---
            if (fromPanel != null)
            {
                fromPanel.interactable = false;
                fromPanel.blocksRaycasts = false;

                float elapsed = 0f;
                while (elapsed < _fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / _fadeDuration);
                    float curveValue = _fadeCurve.Evaluate(t);
                    fromPanel.alpha = 1f - curveValue;
                    yield return null;
                }

                fromPanel.alpha = 0f;
                fromPanel.gameObject.SetActive(false);
            }

            // --- Fade In (yeni panel) ---
            if (toPanel != null)
            {
                toPanel.alpha = 0f;
                toPanel.gameObject.SetActive(true);
                toPanel.interactable = false;
                toPanel.blocksRaycasts = false;

                float elapsed = 0f;
                while (elapsed < _fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / _fadeDuration);
                    float curveValue = _fadeCurve.Evaluate(t);
                    toPanel.alpha = curveValue;
                    yield return null;
                }

                toPanel.alpha = 1f;
                toPanel.interactable = true;
                toPanel.blocksRaycasts = true;
            }

            _isTransitioning = false;
            _activeTransition = null;
            OnPanelTransitionCompleted?.Invoke(toState);
        }

        #endregion
    }
}
