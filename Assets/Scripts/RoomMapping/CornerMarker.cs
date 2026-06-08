using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARRoomTransformer
{
    /// <summary>
    /// Kullanıcı dokunuşlarıyla algılanan AR düzlemlerine köşe işaretçileri yerleştirir.
    /// <see cref="ARRaycastManager"/> ile hit-testing yapar,
    /// prefab tabanlı görsel göstergeler oluşturur ve sıralı bir köşe listesi tutar.
    /// </summary>
    [RequireComponent(typeof(ARRaycastManager))]
    public class CornerMarker : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Prefab & Görsel")]
        [SerializeField]
        [Tooltip("Köşe noktasına yerleştirilecek görsel gösterge prefabı.")]
        private GameObject _cornerMarkerPrefab;

        [SerializeField]
        [Tooltip("Köşe numarasını gösterecek TextMesh prefabı (opsiyonel).")]
        private GameObject _cornerLabelPrefab;

        [SerializeField]
        [Tooltip("Yerleştirilen işaretçilerin parent'ı olacak Transform. Null ise bu objenin altına eklenir.")]
        private Transform _markerParent;

        [Header("Ayarlar")]
        [SerializeField]
        [Tooltip("Maksimum köşe sayısı. 0 = sınırsız.")]
        private int _maxCorners = 20;

        [SerializeField]
        [Tooltip("İki köşe arası minimum mesafe (m). Çok yakın yerleştirmeleri engeller.")]
        private float _minimumCornerDistance = 0.2f;

        [SerializeField]
        [Tooltip("Raycast yapılacak trackable tipleri.")]
        private TrackableType _raycastTrackableTypes = TrackableType.PlaneWithinPolygon;

        [SerializeField]
        [Tooltip("Sadece yatay düzlemlere mi köşe yerleştirilsin?")]
        private bool _horizontalPlanesOnly = true;

        [SerializeField]
        [Tooltip("Yerleştirme aktif mi? False ise dokunma işlenmez.")]
        private bool _placementEnabled = true;

        [Header("Olaylar")]
        [SerializeField]
        [Tooltip("Yeni köşe eklendiğinde tetiklenir. Parametre: köşe pozisyonu.")]
        private UnityEvent<Vector3> _onCornerAdded = new UnityEvent<Vector3>();

        [SerializeField]
        [Tooltip("Köşe kaldırıldığında tetiklenir. Parametre: kaldırılan köşe pozisyonu.")]
        private UnityEvent<Vector3> _onCornerRemoved = new UnityEvent<Vector3>();

        [SerializeField]
        [Tooltip("Tüm köşeler temizlendiğinde tetiklenir.")]
        private UnityEvent _onCornersCleared = new UnityEvent();

        [SerializeField]
        [Tooltip("Yerleştirme reddedildiğinde tetiklenir. Parametre: sebep.")]
        private UnityEvent<string> _onPlacementRejected = new UnityEvent<string>();

        #endregion

        #region Private Fields

        private ARRaycastManager _raycastManager;
        private readonly List<Vector3> _cornerPositions = new List<Vector3>();
        private readonly List<GameObject> _cornerMarkers = new List<GameObject>();
        private static readonly List<ARRaycastHit> _raycastHits = new List<ARRaycastHit>();

        #endregion

        #region Public Events

        /// <summary>Fired when a corner is added. Parameter: corner world position.</summary>
        public event Action<Vector3> CornerAdded;

        /// <summary>Fired when a corner is removed (undo). Parameter: removed corner position.</summary>
        public event Action<Vector3> CornerRemoved;

        /// <summary>Fired when all corners are cleared.</summary>
        public event Action CornersCleared;

        /// <summary>Fired when a placement attempt is rejected. Parameter: reason.</summary>
        public event Action<string> PlacementRejected;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the ordered list of corner positions in world space.
        /// </summary>
        public IReadOnlyList<Vector3> CornerPositions => _cornerPositions.AsReadOnly();

        /// <summary>
        /// Gets the number of currently placed corners.
        /// </summary>
        public int CornerCount => _cornerPositions.Count;

        /// <summary>
        /// Gets or sets whether placement is currently enabled.
        /// </summary>
        public bool PlacementEnabled
        {
            get => _placementEnabled;
            set => _placementEnabled = value;
        }

        /// <summary>Gets the UnityEvent fired when a corner is added.</summary>
        public UnityEvent<Vector3> OnCornerAdded => _onCornerAdded;

        /// <summary>Gets the UnityEvent fired when a corner is removed.</summary>
        public UnityEvent<Vector3> OnCornerRemoved => _onCornerRemoved;

        /// <summary>Gets the UnityEvent fired when corners are cleared.</summary>
        public UnityEvent OnCornersCleared => _onCornersCleared;

        /// <summary>Gets the UnityEvent fired on placement rejection.</summary>
        public UnityEvent<string> OnPlacementRejected => _onPlacementRejected;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _raycastManager = GetComponent<ARRaycastManager>();

            if (_raycastManager == null)
            {
                Debug.LogError("[CornerMarker] ARRaycastManager bulunamadı!");
            }

            if (_markerParent == null)
                _markerParent = transform;
        }

        private void Update()
        {
            if (!_placementEnabled) return;

            // Dokunma girişi kontrolü
            if (Input.touchCount == 0) return;

            Touch touch = Input.GetTouch(0);

            // Sadece dokunma başlangıcını işle
            if (touch.phase != TouchPhase.Began) return;

            // UI üzerindeyse atla
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                return;
            }

            TryPlaceCorner(touch.position);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Attempts to place a corner marker at the given screen position via AR raycast.
        /// </summary>
        /// <param name="screenPosition">Screen-space position (e.g., touch position).</param>
        /// <returns><c>true</c> if a corner was successfully placed.</returns>
        public bool TryPlaceCorner(Vector2 screenPosition)
        {
            if (_raycastManager == null)
            {
                RejectPlacement("ARRaycastManager mevcut değil.");
                return false;
            }

            // Maksimum köşe kontrolü
            if (_maxCorners > 0 && _cornerPositions.Count >= _maxCorners)
            {
                RejectPlacement($"Maksimum köşe sayısına ulaşıldı ({_maxCorners}).");
                return false;
            }

            // AR Raycast
            if (!_raycastManager.Raycast(screenPosition, _raycastHits, _raycastTrackableTypes))
            {
                RejectPlacement("AR düzlemi bulunamadı.");
                return false;
            }

            var hit = _raycastHits[0];

            // Yatay düzlem kontrolü
            if (_horizontalPlanesOnly)
            {
                var plane = hit.trackable as ARPlane;
                if (plane != null && plane.alignment != PlaneAlignment.HorizontalUp)
                {
                    RejectPlacement("Sadece yatay düzlemlere yerleştirme yapılabilir.");
                    return false;
                }
            }

            Vector3 hitPosition = hit.pose.position;

            // Minimum mesafe kontrolü
            if (!CheckMinimumDistance(hitPosition))
            {
                RejectPlacement($"Mevcut bir köşeye çok yakın (min: {_minimumCornerDistance:F2}m).");
                return false;
            }

            // Köşeyi yerleştir
            PlaceCornerAtPosition(hitPosition);
            return true;
        }

        /// <summary>
        /// Places a corner directly at the given world-space position
        /// without performing an AR raycast.
        /// </summary>
        /// <param name="worldPosition">World-space position for the corner.</param>
        /// <returns><c>true</c> if the corner was successfully placed.</returns>
        public bool PlaceCornerDirect(Vector3 worldPosition)
        {
            if (_maxCorners > 0 && _cornerPositions.Count >= _maxCorners)
            {
                RejectPlacement($"Maksimum köşe sayısına ulaşıldı ({_maxCorners}).");
                return false;
            }

            if (!CheckMinimumDistance(worldPosition))
            {
                RejectPlacement($"Mevcut bir köşeye çok yakın (min: {_minimumCornerDistance:F2}m).");
                return false;
            }

            PlaceCornerAtPosition(worldPosition);
            return true;
        }

        /// <summary>
        /// Undoes the last placed corner marker.
        /// </summary>
        /// <returns><c>true</c> if a corner was removed; <c>false</c> if no corners exist.</returns>
        public bool UndoLastCorner()
        {
            if (_cornerPositions.Count == 0)
            {
                Debug.LogWarning("[CornerMarker] Geri alınacak köşe yok.");
                return false;
            }

            int lastIndex = _cornerPositions.Count - 1;
            Vector3 removedPosition = _cornerPositions[lastIndex];

            // İşaretçiyi kaldır
            if (lastIndex < _cornerMarkers.Count)
            {
                var marker = _cornerMarkers[lastIndex];
                if (marker != null)
                    Destroy(marker);

                _cornerMarkers.RemoveAt(lastIndex);
            }

            _cornerPositions.RemoveAt(lastIndex);

            Debug.Log($"[CornerMarker] Köşe geri alındı: #{lastIndex + 1} ({removedPosition})");

            CornerRemoved?.Invoke(removedPosition);
            _onCornerRemoved?.Invoke(removedPosition);

            return true;
        }

        /// <summary>
        /// Clears all placed corner markers.
        /// </summary>
        public void ClearAllCorners()
        {
            foreach (var marker in _cornerMarkers)
            {
                if (marker != null)
                    Destroy(marker);
            }

            _cornerMarkers.Clear();
            _cornerPositions.Clear();

            Debug.Log("[CornerMarker] Tüm köşeler temizlendi.");

            CornersCleared?.Invoke();
            _onCornersCleared?.Invoke();
        }

        /// <summary>
        /// Gets a copy of the current corner positions list.
        /// Useful for passing to <see cref="RoomBoundaryDetector"/>.
        /// </summary>
        /// <returns>A new list containing all corner positions.</returns>
        public List<Vector3> GetCornerPositionsCopy()
        {
            return new List<Vector3>(_cornerPositions);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Places a corner at the specified world position and instantiates the visual marker.
        /// </summary>
        /// <param name="position">The world-space position.</param>
        private void PlaceCornerAtPosition(Vector3 position)
        {
            _cornerPositions.Add(position);

            // Görsel işaretçi oluştur
            GameObject marker = null;

            if (_cornerMarkerPrefab != null)
            {
                marker = Instantiate(_cornerMarkerPrefab, position, Quaternion.identity, _markerParent);
                marker.name = $"CornerMarker_{_cornerPositions.Count}";
            }
            else
            {
                // Prefab yoksa basit bir küre oluştur
                marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.transform.position = position;
                marker.transform.localScale = Vector3.one * 0.05f;
                marker.transform.SetParent(_markerParent);
                marker.name = $"CornerMarker_{_cornerPositions.Count}";

                // Collider'ı kaldır (görsel amaçlı)
                var collider = marker.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);
            }

            // Etiket ekle
            if (_cornerLabelPrefab != null && marker != null)
            {
                var label = Instantiate(_cornerLabelPrefab, marker.transform);
                label.transform.localPosition = Vector3.up * 0.1f;

                var textMesh = label.GetComponent<TextMesh>();
                if (textMesh != null)
                    textMesh.text = _cornerPositions.Count.ToString();
            }

            _cornerMarkers.Add(marker);

            Debug.Log($"[CornerMarker] Köşe #{_cornerPositions.Count} yerleştirildi: {position}");

            CornerAdded?.Invoke(position);
            _onCornerAdded?.Invoke(position);
        }

        /// <summary>
        /// Checks if the given position is at least <see cref="_minimumCornerDistance"/>
        /// away from all existing corners.
        /// </summary>
        /// <param name="position">The position to check.</param>
        /// <returns><c>true</c> if the minimum distance is satisfied.</returns>
        private bool CheckMinimumDistance(Vector3 position)
        {
            foreach (var existing in _cornerPositions)
            {
                if (Vector3.Distance(existing, position) < _minimumCornerDistance)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Raises the placement-rejected events and logs the reason.
        /// </summary>
        /// <param name="reason">The rejection reason.</param>
        private void RejectPlacement(string reason)
        {
            Debug.LogWarning($"[CornerMarker] Yerleştirme reddedildi: {reason}");
            PlacementRejected?.Invoke(reason);
            _onPlacementRejected?.Invoke(reason);
        }

        #endregion
    }
}
