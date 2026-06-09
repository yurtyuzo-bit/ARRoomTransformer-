using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARRoomTransformer
{
    /// <summary>
    /// Ana oda tarama kontrol sınıfı.
    /// <see cref="ARPlaneManager"/> aracılığıyla zemin, duvar ve tavan düzlemlerini algılar,
    /// sınıflandırır ve olaylar tetikler.
    /// </summary>
    public class RoomScanner : MonoBehaviour
    {
        #region Nested Types

        /// <summary>
        /// Algılanan bir yüzeyin sınıflandırması.
        /// </summary>
        public enum SurfaceType
        {
            /// <summary>Bilinmeyen / sınıflandırılamayan yüzey.</summary>
            Unknown = 0,

            /// <summary>Zemin düzlemi.</summary>
            Floor = 1,

            /// <summary>Duvar düzlemi.</summary>
            Wall = 2,

            /// <summary>Tavan düzlemi.</summary>
            Ceiling = 3
        }

        /// <summary>
        /// Represents a classified AR surface detected during scanning.
        /// </summary>
        [Serializable]
        public class ClassifiedSurface
        {
            /// <summary>The underlying AR plane.</summary>
            public ARPlane Plane;

            /// <summary>The classified surface type.</summary>
            public SurfaceType Type;

            /// <summary>The world-space center position of the plane.</summary>
            public Vector3 Center;

            /// <summary>The size of the plane in meters (width × height).</summary>
            public Vector2 Size;

            /// <summary>The world-space normal of the plane.</summary>
            public Vector3 Normal;

            /// <summary>
            /// Initializes a new <see cref="ClassifiedSurface"/>.
            /// </summary>
            public ClassifiedSurface(ARPlane plane, SurfaceType type)
            {
                Plane = plane;
                Type = type;
                Center = plane.center;
                Size = plane.size;
                Normal = plane.normal;
            }
        }

        #endregion

        #region Serialized Fields

        [Header("Referanslar")]
        [SerializeField]
        [Tooltip("AR düzlem yöneticisi. Boş bırakılırsa otomatik bulunur.")]
        private ARPlaneManager _planeManager;

        [Header("Tarama Ayarları")]
        [SerializeField]
        [Tooltip("Bir düzlemin geçerli sayılması için gereken minimum alan (m²).")]
        private float _minimumPlaneArea = 0.25f;

        [SerializeField]
        [Tooltip("Yatay düzlemin tavan sayılması için gereken minimum yükseklik (m).")]
        private float _ceilingHeightThreshold = 1.8f;

        [Header("Olaylar")]
        [SerializeField]
        [Tooltip("Yeni bir yüzey algılandığında tetiklenir.")]
        private UnityEvent<ClassifiedSurface> _onSurfaceDetected = new UnityEvent<ClassifiedSurface>();

        [SerializeField]
        [Tooltip("Bir yüzey güncellendiğinde tetiklenir.")]
        private UnityEvent<ClassifiedSurface> _onSurfaceUpdated = new UnityEvent<ClassifiedSurface>();

        [SerializeField]
        [Tooltip("Bir yüzey kaldırıldığında tetiklenir.")]
        private UnityEvent<ARPlane> _onSurfaceRemoved = new UnityEvent<ARPlane>();

        [SerializeField]
        [Tooltip("Tarama tamamlandığında tetiklenir.")]
        private UnityEvent _onScanFinalized = new UnityEvent();

        #endregion

        #region Private Fields

        private readonly Dictionary<TrackableId, ClassifiedSurface> _classifiedSurfaces = new Dictionary<TrackableId, ClassifiedSurface>();
        private bool _isScanning;
        private float _lowestFloorY = float.MaxValue;

        #endregion

        #region Public Events

        /// <summary>Fired when a new surface is detected and classified.</summary>
        public event Action<ClassifiedSurface> SurfaceDetected;

        /// <summary>Fired when an existing surface is updated.</summary>
        public event Action<ClassifiedSurface> SurfaceUpdated;

        /// <summary>Fired when a surface is removed.</summary>
        public event Action<ARPlane> SurfaceRemoved;

        /// <summary>Fired when the scan is finalized.</summary>
        public event Action ScanFinalized;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether scanning is currently active.
        /// </summary>
        public bool IsScanning => _isScanning;

        /// <summary>
        /// Gets a read-only collection of all classified surfaces.
        /// </summary>
        public IReadOnlyDictionary<TrackableId, ClassifiedSurface> ClassifiedSurfaces => _classifiedSurfaces;

        /// <summary>
        /// Gets all detected floor surfaces.
        /// </summary>
        public IEnumerable<ClassifiedSurface> Floors =>
            _classifiedSurfaces.Values.Where(s => s.Type == SurfaceType.Floor);

        /// <summary>
        /// Gets all detected wall surfaces.
        /// </summary>
        public IEnumerable<ClassifiedSurface> Walls =>
            _classifiedSurfaces.Values.Where(s => s.Type == SurfaceType.Wall);

        /// <summary>
        /// Gets all detected ceiling surfaces.
        /// </summary>
        public IEnumerable<ClassifiedSurface> Ceilings =>
            _classifiedSurfaces.Values.Where(s => s.Type == SurfaceType.Ceiling);

        /// <summary>
        /// Gets the total number of classified surfaces.
        /// </summary>
        public int SurfaceCount => _classifiedSurfaces.Count;

        /// <summary>
        /// Gets the estimated floor Y-position (lowest detected horizontal plane).
        /// </summary>
        public float EstimatedFloorHeight => _lowestFloorY;

        /// <summary>Gets the UnityEvent fired on surface detection.</summary>
        public UnityEvent<ClassifiedSurface> OnSurfaceDetected => _onSurfaceDetected;

        /// <summary>Gets the UnityEvent fired on surface update.</summary>
        public UnityEvent<ClassifiedSurface> OnSurfaceUpdated => _onSurfaceUpdated;

        /// <summary>Gets the UnityEvent fired on surface removal.</summary>
        public UnityEvent<ARPlane> OnSurfaceRemoved => _onSurfaceRemoved;

        /// <summary>Gets the UnityEvent fired on scan finalization.</summary>
        public UnityEvent OnScanFinalized => _onScanFinalized;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_planeManager == null)
                _planeManager = FindAnyObjectByType<ARPlaneManager>();

            if (_planeManager == null)
                Debug.LogError("[RoomScanner] ARPlaneManager bulunamadı!");
        }

        private void OnEnable()
        {
            if (_planeManager != null)
                _planeManager.trackablesChanged.AddListener(OnPlanesChanged);
        }

        private void OnDisable()
        {
            if (_planeManager != null)
                _planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts scanning for room surfaces.
        /// Enables the <see cref="ARPlaneManager"/> and begins classification.
        /// </summary>
        public void StartScanning()
        {
            if (_isScanning)
            {
                Debug.LogWarning("[RoomScanner] Tarama zaten devam ediyor.");
                return;
            }

            _classifiedSurfaces.Clear();
            _lowestFloorY = float.MaxValue;
            _isScanning = true;

            if (_planeManager != null)
                _planeManager.enabled = true;

            Debug.Log("[RoomScanner] Oda taraması başlatıldı.");
        }

        /// <summary>
        /// Stops scanning and finalizes the detected surfaces.
        /// Optionally disables plane detection to save resources.
        /// </summary>
        /// <param name="disablePlaneDetection">
        /// If <c>true</c>, disables <see cref="ARPlaneManager"/> after finalization.
        /// </param>
        public void FinalizeScan(bool disablePlaneDetection = true)
        {
            if (!_isScanning)
            {
                Debug.LogWarning("[RoomScanner] Tarama zaten durdurulmuş.");
                return;
            }

            _isScanning = false;

            if (disablePlaneDetection && _planeManager != null)
                _planeManager.enabled = false;

            Debug.Log($"[RoomScanner] Tarama tamamlandı. Toplam yüzey sayısı: {_classifiedSurfaces.Count}");
            Debug.Log($"  Zemin: {Floors.Count()}, Duvar: {Walls.Count()}, Tavan: {Ceilings.Count()}");

            ScanFinalized?.Invoke();
            _onScanFinalized?.Invoke();
        }

        /// <summary>
        /// Clears all classified surfaces and resets the scanner state.
        /// </summary>
        public void ClearSurfaces()
        {
            _classifiedSurfaces.Clear();
            _lowestFloorY = float.MaxValue;
            Debug.Log("[RoomScanner] Tüm yüzeyler temizlendi.");
        }

        /// <summary>
        /// Gets a summary string of the current scan status.
        /// </summary>
        /// <returns>A human-readable scan summary.</returns>
        public string GetScanSummary()
        {
            return $"Tarama Durumu: {(_isScanning ? "Devam Ediyor" : "Durduruldu")}\n" +
                   $"Toplam Yüzey: {_classifiedSurfaces.Count}\n" +
                   $"Zemin: {Floors.Count()}\n" +
                   $"Duvar: {Walls.Count()}\n" +
                   $"Tavan: {Ceilings.Count()}\n" +
                   $"Tahmini Zemin Y: {_lowestFloorY:F2}m";
        }

        #endregion

        #region Private Methods — Plane Classification

        /// <summary>
        /// Callback for <see cref="ARPlaneManager.trackablesChanged"/> using AR Foundation 5.x API.
        /// </summary>
        private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            if (!_isScanning) return;

            // Yeni düzlemler
            foreach (var plane in args.added)
            {
                ProcessPlane(plane, isNew: true);
            }

            // Güncellenen düzlemler
            foreach (var plane in args.updated)
            {
                ProcessPlane(plane, isNew: false);
            }

            // Kaldırılan düzlemler
            foreach (var removedPair in args.removed)
            {
                var trackableId = removedPair.Key;
                if (_classifiedSurfaces.ContainsKey(trackableId))
                {
                    var surface = _classifiedSurfaces[trackableId];
                    _classifiedSurfaces.Remove(trackableId);

                    SurfaceRemoved?.Invoke(surface.Plane);
                    _onSurfaceRemoved?.Invoke(surface.Plane);

                    Debug.Log($"[RoomScanner] Yüzey kaldırıldı: {trackableId}");
                }
            }
        }

        /// <summary>
        /// Processes a single AR plane — classifies and adds/updates it.
        /// </summary>
        /// <param name="plane">The AR plane to process.</param>
        /// <param name="isNew">Whether this is a newly added plane.</param>
        private void ProcessPlane(ARPlane plane, bool isNew)
        {
            if (plane == null) return;

            // Alt düzlemleri atla (birleştirilmiş düzlemlerin parçaları)
            if (plane.subsumedBy != null) return;

            // Minimum alan kontrolü
            float area = plane.size.x * plane.size.y;
            if (area < _minimumPlaneArea) return;

            // Sınıflandır
            SurfaceType surfaceType = ClassifyPlane(plane);

            // Zemin Y takibi
            if (surfaceType == SurfaceType.Floor && plane.center.y < _lowestFloorY)
            {
                _lowestFloorY = plane.center.y;
            }

            var classified = new ClassifiedSurface(plane, surfaceType);
            var trackableId = plane.trackableId;

            if (isNew || !_classifiedSurfaces.ContainsKey(trackableId))
            {
                _classifiedSurfaces[trackableId] = classified;
                SurfaceDetected?.Invoke(classified);
                _onSurfaceDetected?.Invoke(classified);
                Debug.Log($"[RoomScanner] Yeni yüzey algılandı: {surfaceType} (ID: {trackableId})");
            }
            else
            {
                _classifiedSurfaces[trackableId] = classified;
                SurfaceUpdated?.Invoke(classified);
                _onSurfaceUpdated?.Invoke(classified);
            }
        }

        /// <summary>
        /// Classifies an AR plane as Floor, Wall, Ceiling, or Unknown based on its
        /// alignment, classification data, and position.
        /// </summary>
        /// <param name="plane">The plane to classify.</param>
        /// <returns>The classified <see cref="SurfaceType"/>.</returns>
        private SurfaceType ClassifyPlane(ARPlane plane)
        {
            // ARKit sınıflandırma bilgisi varsa önce onu kullan
            if (plane.classifications.HasFlag(PlaneClassifications.Floor))
                return SurfaceType.Floor;
            if (plane.classifications.HasFlag(PlaneClassifications.Wall))
                return SurfaceType.Wall;
            if (plane.classifications.HasFlag(PlaneClassifications.Ceiling))
                return SurfaceType.Ceiling;

            // Sınıflandırma yoksa, hizalamaya göre karar ver
            switch (plane.alignment)
            {
                case PlaneAlignment.Vertical:
                    return SurfaceType.Wall;

                case PlaneAlignment.HorizontalUp:
                    // Yüksekliğe göre zemin/tavan ayrımı
                    if (_lowestFloorY < float.MaxValue &&
                        plane.center.y > _lowestFloorY + _ceilingHeightThreshold)
                    {
                        return SurfaceType.Ceiling;
                    }
                    return SurfaceType.Floor;

                case PlaneAlignment.HorizontalDown:
                    return SurfaceType.Ceiling;

                default:
                    return SurfaceType.Unknown;
            }
        }

        #endregion
    }
}
