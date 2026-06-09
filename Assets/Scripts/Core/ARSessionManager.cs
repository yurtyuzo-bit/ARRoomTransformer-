using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

namespace ARRoomTransformer
{
    /// <summary>
    /// AR oturumunun yaşam döngüsünü yönetir.
    /// <see cref="ARSession"/>, <see cref="XROrigin"/>, düzlem algılama,
    /// nokta bulutu ve çevre problarını yapılandırır.
    /// </summary>
    [RequireComponent(typeof(ARSession))]
    public class ARSessionManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("AR Bileşen Referansları")]
        [SerializeField]
        [Tooltip("Sahnedeki XROrigin bileşeni.")]
        private XROrigin _xrOrigin;

        [SerializeField]
        [Tooltip("Düzlem algılama yöneticisi.")]
        private ARPlaneManager _planeManager;

        [SerializeField]
        [Tooltip("Nokta bulutu yöneticisi.")]
        private ARPointCloudManager _pointCloudManager;

        [SerializeField]
        [Tooltip("Raycast yöneticisi.")]
        private ARRaycastManager _raycastManager;

        [SerializeField]
        [Tooltip("Çevre prob yöneticisi (opsiyonel).")]
        private AREnvironmentProbeManager _environmentProbeManager;

        [Header("Düzlem Algılama Ayarları")]
        [SerializeField]
        [Tooltip("Başlangıçta algılanacak düzlem tipleri.")]
        private PlaneDetectionMode _initialPlaneDetectionMode = PlaneDetectionMode.Horizontal | PlaneDetectionMode.Vertical;

        [Header("Başlangıç Özellik Durumları")]
        [SerializeField]
        [Tooltip("Uygulama başladığında düzlem algılama aktif mi?")]
        private bool _enablePlanesOnStart = true;

        [SerializeField]
        [Tooltip("Uygulama başladığında nokta bulutu aktif mi?")]
        private bool _enablePointCloudOnStart = true;

        [SerializeField]
        [Tooltip("Uygulama başladığında çevre probları aktif mi?")]
        private bool _enableEnvironmentProbesOnStart = false;

        [Header("Olaylar")]
        [SerializeField]
        [Tooltip("AR oturumu hazır olduğunda tetiklenir.")]
        private UnityEvent _onARSessionReady = new UnityEvent();

        [SerializeField]
        [Tooltip("AR oturum hatası oluştuğunda tetiklenir. Parametre: hata mesajı.")]
        private UnityEvent<string> _onARSessionError = new UnityEvent<string>();

        [SerializeField]
        [Tooltip("AR oturum durumu değiştiğinde tetiklenir.")]
        private UnityEvent<ARSessionState> _onARSessionStateChanged = new UnityEvent<ARSessionState>();

        #endregion

        #region Private Fields

        private ARSession _arSession;
        private bool _isSessionReady;

        #endregion

        #region Public Events

        /// <summary>
        /// Fired when the AR session becomes ready (tracking state achieved).
        /// </summary>
        public event Action ARSessionReady;

        /// <summary>
        /// Fired when the AR session encounters an error.
        /// Parameter: error description.
        /// </summary>
        public event Action<string> ARSessionError;

        /// <summary>
        /// Fired when the AR session state changes.
        /// Parameter: new session state.
        /// </summary>
        public event Action<ARSessionState> ARSessionStateChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the AR session is ready and tracking.
        /// </summary>
        public bool IsSessionReady => _isSessionReady;

        /// <summary>
        /// Gets the current <see cref="ARSessionState"/>.
        /// </summary>
        public ARSessionState CurrentSessionState => ARSession.state;

        /// <summary>
        /// Gets the <see cref="XROrigin"/> reference.
        /// </summary>
        public XROrigin XROrigin => _xrOrigin;

        /// <summary>
        /// Gets the <see cref="ARPlaneManager"/> reference.
        /// </summary>
        public ARPlaneManager PlaneManager => _planeManager;

        /// <summary>
        /// Gets the <see cref="ARPointCloudManager"/> reference.
        /// </summary>
        public ARPointCloudManager PointCloudManager => _pointCloudManager;

        /// <summary>
        /// Gets the <see cref="ARRaycastManager"/> reference.
        /// </summary>
        public ARRaycastManager RaycastManager => _raycastManager;

        /// <summary>
        /// Gets the <see cref="AREnvironmentProbeManager"/> reference (may be null).
        /// </summary>
        public AREnvironmentProbeManager EnvironmentProbeManager => _environmentProbeManager;

        /// <summary>Gets the UnityEvent fired when the AR session is ready.</summary>
        public UnityEvent OnARSessionReady => _onARSessionReady;

        /// <summary>Gets the UnityEvent fired on AR session errors.</summary>
        public UnityEvent<string> OnARSessionError => _onARSessionError;

        /// <summary>Gets the UnityEvent fired on AR session state changes.</summary>
        public UnityEvent<ARSessionState> OnARSessionStateChanged => _onARSessionStateChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _arSession = GetComponent<ARSession>();

            if (_arSession == null)
            {
                Debug.LogError("[ARSessionManager] ARSession bileşeni bulunamadı!");
                return;
            }

            // XROrigin otomatik bulma
            if (_xrOrigin == null)
                _xrOrigin = FindAnyObjectByType<XROrigin>();

            // Yöneticileri otomatik bulma
            if (_planeManager == null && _xrOrigin != null)
                _planeManager = _xrOrigin.GetComponentInChildren<ARPlaneManager>();

            if (_pointCloudManager == null && _xrOrigin != null)
                _pointCloudManager = _xrOrigin.GetComponentInChildren<ARPointCloudManager>();

            if (_raycastManager == null && _xrOrigin != null)
                _raycastManager = _xrOrigin.GetComponentInChildren<ARRaycastManager>();

            if (_environmentProbeManager == null && _xrOrigin != null)
                _environmentProbeManager = _xrOrigin.GetComponentInChildren<AREnvironmentProbeManager>();
        }

        private void OnEnable()
        {
            ARSession.stateChanged += HandleARSessionStateChanged;
        }

        private void OnDisable()
        {
            ARSession.stateChanged -= HandleARSessionStateChanged;
        }

        private void Start()
        {
            ApplyInitialFeatureStates();
        }

        #endregion

        #region Public Methods — Feature Control

        /// <summary>
        /// Enables or disables plane detection.
        /// </summary>
        /// <param name="enabled">Whether plane detection should be enabled.</param>
        public void SetPlaneDetectionEnabled(bool enabled)
        {
            if (_planeManager == null)
            {
                Debug.LogWarning("[ARSessionManager] ARPlaneManager bulunamadı, düzlem algılama ayarlanamıyor.");
                return;
            }

            _planeManager.enabled = enabled;
            Debug.Log($"[ARSessionManager] Düzlem algılama: {(enabled ? "AKTİF" : "PASİF")}");
        }

        /// <summary>
        /// Sets the plane detection mode (horizontal, vertical, or both).
        /// </summary>
        /// <param name="mode">The desired <see cref="PlaneDetectionMode"/>.</param>
        public void SetPlaneDetectionMode(PlaneDetectionMode mode)
        {
            if (_planeManager == null)
            {
                Debug.LogWarning("[ARSessionManager] ARPlaneManager bulunamadı.");
                return;
            }

            _planeManager.requestedDetectionMode = mode;
            Debug.Log($"[ARSessionManager] Düzlem algılama modu: {mode}");
        }

        /// <summary>
        /// Enables or disables the point cloud feature.
        /// </summary>
        /// <param name="enabled">Whether point cloud should be enabled.</param>
        public void SetPointCloudEnabled(bool enabled)
        {
            if (_pointCloudManager == null)
            {
                Debug.LogWarning("[ARSessionManager] ARPointCloudManager bulunamadı.");
                return;
            }

            _pointCloudManager.enabled = enabled;
            Debug.Log($"[ARSessionManager] Nokta bulutu: {(enabled ? "AKTİF" : "PASİF")}");
        }

        /// <summary>
        /// Enables or disables environment probes.
        /// </summary>
        /// <param name="enabled">Whether environment probes should be enabled.</param>
        public void SetEnvironmentProbesEnabled(bool enabled)
        {
            if (_environmentProbeManager == null)
            {
                Debug.LogWarning("[ARSessionManager] AREnvironmentProbeManager bulunamadı.");
                return;
            }

            _environmentProbeManager.enabled = enabled;
            Debug.Log($"[ARSessionManager] Çevre probları: {(enabled ? "AKTİF" : "PASİF")}");
        }

        /// <summary>
        /// Toggles the visibility of all currently detected AR planes.
        /// </summary>
        /// <param name="visible">Whether planes should be rendered.</param>
        public void SetPlanesVisible(bool visible)
        {
            if (_planeManager == null) return;

            foreach (var plane in _planeManager.trackables)
            {
                if (plane != null)
                    plane.gameObject.SetActive(visible);
            }

            Debug.Log($"[ARSessionManager] Düzlem görünürlüğü: {(visible ? "GÖRÜNÜR" : "GİZLİ")}");
        }

        /// <summary>
        /// Toggles the visibility of all currently detected AR point clouds.
        /// </summary>
        /// <param name="visible">Whether point clouds should be rendered.</param>
        public void SetPointCloudsVisible(bool visible)
        {
            if (_pointCloudManager == null) return;

            foreach (var pointCloud in _pointCloudManager.trackables)
            {
                if (pointCloud != null)
                    pointCloud.gameObject.SetActive(visible);
            }

            Debug.Log($"[ARSessionManager] Nokta bulutu görünürlüğü: {(visible ? "GÖRÜNÜR" : "GİZLİ")}");
        }

        /// <summary>
        /// Resets the AR session. Destroys all trackables and restarts tracking.
        /// </summary>
        public void ResetSession()
        {
            if (_arSession == null)
            {
                Debug.LogError("[ARSessionManager] ARSession referansı bulunamadı.");
                return;
            }

            _isSessionReady = false;
            _arSession.Reset();
            Debug.Log("[ARSessionManager] AR oturumu sıfırlandı.");
        }

        /// <summary>
        /// Checks whether AR is supported on the current device.
        /// This is an asynchronous check; use as a coroutine.
        /// </summary>
        /// <param name="callback">Callback with the availability result.</param>
        /// <returns>A coroutine enumerator.</returns>
        public IEnumerator CheckAvailability(Action<bool> callback)
        {
            if (ARSession.state == ARSessionState.None ||
                ARSession.state == ARSessionState.CheckingAvailability)
            {
                yield return ARSession.CheckAvailability();
            }

            bool isSupported = ARSession.state != ARSessionState.Unsupported;
            Debug.Log($"[ARSessionManager] AR Desteği: {(isSupported ? "EVET" : "HAYIR")}");
            callback?.Invoke(isSupported);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Applies the initial feature states configured in the Inspector.
        /// </summary>
        private void ApplyInitialFeatureStates()
        {
            SetPlaneDetectionEnabled(_enablePlanesOnStart);

            if (_enablePlanesOnStart && _planeManager != null)
            {
                _planeManager.requestedDetectionMode = _initialPlaneDetectionMode;
            }

            SetPointCloudEnabled(_enablePointCloudOnStart);
            SetEnvironmentProbesEnabled(_enableEnvironmentProbesOnStart);
        }

        /// <summary>
        /// Handles AR session state change events from <see cref="ARSession"/>.
        /// </summary>
        /// <param name="args">The state change event arguments.</param>
        private void HandleARSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            Debug.Log($"[ARSessionManager] AR Oturum durumu: {args.state}");

            // C# event
            ARSessionStateChanged?.Invoke(args.state);

            // UnityEvent
            _onARSessionStateChanged?.Invoke(args.state);

            switch (args.state)
            {
                case ARSessionState.Ready:
                case ARSessionState.SessionInitializing:
                case ARSessionState.SessionTracking:
                    if (!_isSessionReady)
                    {
                        _isSessionReady = true;
                        ARSessionReady?.Invoke();
                        _onARSessionReady?.Invoke();
                        Debug.Log("[ARSessionManager] AR oturumu hazır.");
                    }
                    break;

                case ARSessionState.Unsupported:
                    HandleError("AR bu cihazda desteklenmiyor.");
                    break;

                case ARSessionState.NeedsInstall:
                    HandleError("AR yazılımı yüklenmesi gerekiyor.");
                    break;

                case ARSessionState.None:
                    _isSessionReady = false;
                    break;
            }
        }

        /// <summary>
        /// Handles an AR session error by logging and raising events.
        /// </summary>
        /// <param name="errorMessage">The error description.</param>
        private void HandleError(string errorMessage)
        {
            Debug.LogError($"[ARSessionManager] Hata: {errorMessage}");
            ARSessionError?.Invoke(errorMessage);
            _onARSessionError?.Invoke(errorMessage);
        }

        #endregion
    }
}
