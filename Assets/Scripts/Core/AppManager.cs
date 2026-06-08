using System;
using UnityEngine;
using UnityEngine.Events;

namespace ARRoomTransformer
{
    /// <summary>
    /// Uygulamanın genel durumunu yöneten singleton sınıf.
    /// Idle → Scanning → Placing → Recording akışını kontrol eder.
    /// </summary>
    public class AppManager : MonoBehaviour
    {
        #region Singleton

        private static AppManager _instance;

        /// <summary>
        /// Gets the singleton instance of <see cref="AppManager"/>.
        /// </summary>
        public static AppManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AppManager>();

                    if (_instance == null)
                    {
                        var go = new GameObject("[AppManager]");
                        _instance = go.AddComponent<AppManager>();
                    }
                }

                return _instance;
            }
        }

        #endregion

        #region Enums

        /// <summary>
        /// Uygulama durum makinesi durumları.
        /// </summary>
        public enum AppState
        {
            /// <summary>Uygulama boşta, hiçbir işlem yapılmıyor.</summary>
            Idle = 0,

            /// <summary>Oda taranıyor, AR düzlemleri algılanıyor.</summary>
            Scanning = 1,

            /// <summary>Köşe noktaları yerleştiriliyor, oda sınırları belirleniyor.</summary>
            Placing = 2,

            /// <summary>Oda geometrisi oluşturuldu, kayıt/render aşaması.</summary>
            Recording = 3
        }

        #endregion

        #region Serialized Fields

        [Header("Başlangıç Ayarları")]
        [SerializeField]
        [Tooltip("Uygulama başladığında gireceği durum.")]
        private AppState _initialState = AppState.Idle;

        [Header("Olaylar (Events)")]
        [SerializeField]
        [Tooltip("Durum değiştiğinde tetiklenir. Parametre: yeni durum.")]
        private UnityEvent<AppState> _onStateChanged = new UnityEvent<AppState>();

        [SerializeField]
        [Tooltip("Scanning durumuna girildiğinde tetiklenir.")]
        private UnityEvent _onScanningStarted = new UnityEvent();

        [SerializeField]
        [Tooltip("Placing durumuna girildiğinde tetiklenir.")]
        private UnityEvent _onPlacingStarted = new UnityEvent();

        [SerializeField]
        [Tooltip("Recording durumuna girildiğinde tetiklenir.")]
        private UnityEvent _onRecordingStarted = new UnityEvent();

        [SerializeField]
        [Tooltip("Idle durumuna dönüldüğünde tetiklenir.")]
        private UnityEvent _onReturnedToIdle = new UnityEvent();

        #endregion

        #region Public Events (C# delegates)

        /// <summary>
        /// Fired when the application state changes.
        /// Parameters: previous state, new state.
        /// </summary>
        public event Action<AppState, AppState> StateChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current application state.
        /// </summary>
        public AppState CurrentState { get; private set; }

        /// <summary>
        /// Gets the previous application state.
        /// </summary>
        public AppState PreviousState { get; private set; }

        /// <summary>
        /// Gets the UnityEvent fired on state changes. Useful for Inspector wiring.
        /// </summary>
        public UnityEvent<AppState> OnStateChanged => _onStateChanged;

        /// <summary>
        /// Gets the UnityEvent fired when scanning starts.
        /// </summary>
        public UnityEvent OnScanningStarted => _onScanningStarted;

        /// <summary>
        /// Gets the UnityEvent fired when placing starts.
        /// </summary>
        public UnityEvent OnPlacingStarted => _onPlacingStarted;

        /// <summary>
        /// Gets the UnityEvent fired when recording starts.
        /// </summary>
        public UnityEvent OnRecordingStarted => _onRecordingStarted;

        /// <summary>
        /// Gets the UnityEvent fired when returning to idle.
        /// </summary>
        public UnityEvent OnReturnedToIdle => _onReturnedToIdle;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton kontrolü
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[AppManager] Birden fazla AppManager bulundu. '{gameObject.name}' yok ediliyor.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            CurrentState = _initialState;
            PreviousState = _initialState;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Public Methods — State Transitions

        /// <summary>
        /// Transitions the application to the specified state.
        /// Invalid transitions are logged as warnings and ignored.
        /// </summary>
        /// <param name="newState">The target state to transition to.</param>
        /// <returns><c>true</c> if the transition was valid and applied; otherwise <c>false</c>.</returns>
        public bool TransitionTo(AppState newState)
        {
            if (newState == CurrentState)
            {
                Debug.LogWarning($"[AppManager] Zaten '{CurrentState}' durumunda.");
                return false;
            }

            if (!IsValidTransition(CurrentState, newState))
            {
                Debug.LogWarning($"[AppManager] Geçersiz durum geçişi: {CurrentState} → {newState}");
                return false;
            }

            PreviousState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[AppManager] Durum değişti: {PreviousState} → {CurrentState}");

            // C# event
            StateChanged?.Invoke(PreviousState, CurrentState);

            // UnityEvent — genel
            _onStateChanged?.Invoke(CurrentState);

            // Duruma özel UnityEvent'ler
            switch (CurrentState)
            {
                case AppState.Idle:
                    _onReturnedToIdle?.Invoke();
                    break;
                case AppState.Scanning:
                    _onScanningStarted?.Invoke();
                    break;
                case AppState.Placing:
                    _onPlacingStarted?.Invoke();
                    break;
                case AppState.Recording:
                    _onRecordingStarted?.Invoke();
                    break;
            }

            return true;
        }

        /// <summary>
        /// Convenience method: transitions to <see cref="AppState.Scanning"/>.
        /// </summary>
        public void StartScanning() => TransitionTo(AppState.Scanning);

        /// <summary>
        /// Convenience method: transitions to <see cref="AppState.Placing"/>.
        /// </summary>
        public void StartPlacing() => TransitionTo(AppState.Placing);

        /// <summary>
        /// Convenience method: transitions to <see cref="AppState.Recording"/>.
        /// </summary>
        public void StartRecording() => TransitionTo(AppState.Recording);

        /// <summary>
        /// Resets the application back to <see cref="AppState.Idle"/>.
        /// This is always a valid transition from any state.
        /// </summary>
        public void Reset()
        {
            PreviousState = CurrentState;
            CurrentState = AppState.Idle;

            Debug.Log($"[AppManager] Sıfırlandı: {PreviousState} → Idle");

            StateChanged?.Invoke(PreviousState, CurrentState);
            _onStateChanged?.Invoke(CurrentState);
            _onReturnedToIdle?.Invoke();
        }

        /// <summary>
        /// Forces a state change without validation. Use only for debugging.
        /// </summary>
        /// <param name="newState">The state to force.</param>
        public void ForceState(AppState newState)
        {
            Debug.LogWarning($"[AppManager] Durum zorla değiştiriliyor: {CurrentState} → {newState}");
            PreviousState = CurrentState;
            CurrentState = newState;

            StateChanged?.Invoke(PreviousState, CurrentState);
            _onStateChanged?.Invoke(CurrentState);
        }

        #endregion

        #region Transition Validation

        /// <summary>
        /// Determines whether a transition from <paramref name="from"/> to <paramref name="to"/> is valid.
        /// Valid flow: Idle → Scanning → Placing → Recording.
        /// Reset to Idle is always valid.
        /// </summary>
        /// <param name="from">Current state.</param>
        /// <param name="to">Target state.</param>
        /// <returns><c>true</c> if the transition is valid.</returns>
        public static bool IsValidTransition(AppState from, AppState to)
        {
            // Idle'a dönüş her zaman geçerli
            if (to == AppState.Idle)
                return true;

            return (from, to) switch
            {
                (AppState.Idle, AppState.Scanning) => true,
                (AppState.Scanning, AppState.Placing) => true,
                (AppState.Placing, AppState.Recording) => true,
                // Geri adım atma (isteğe bağlı olarak izin ver)
                (AppState.Placing, AppState.Scanning) => true,
                (AppState.Recording, AppState.Placing) => true,
                _ => false
            };
        }

        #endregion
    }
}
