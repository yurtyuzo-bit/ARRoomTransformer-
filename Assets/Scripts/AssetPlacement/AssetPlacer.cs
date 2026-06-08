using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARRoomTransformer
{
    /// <summary>
    /// Yerleştirilmiş bir varlığa eklenen bileşen; kimlik ve meta verileri tutar.
    /// Component attached to every placed asset instance. Stores identity, metadata,
    /// and the surface normal at the point of placement.
    /// </summary>
    public class PlacedAsset : MonoBehaviour
    {
        /// <summary>Unique runtime identifier for this placed instance.</summary>
        [HideInInspector] public string instanceId;

        /// <summary>Reference to the catalog entry this instance was created from.</summary>
        [HideInInspector] public AssetEntry sourceEntry;

        /// <summary>The surface normal at the placement point.</summary>
        [HideInInspector] public Vector3 placementNormal;

        /// <summary>The AR plane trackable ID this was placed on, if any.</summary>
        [HideInInspector] public TrackableId arPlaneId;

        /// <summary>Timestamp when this asset was placed.</summary>
        [HideInInspector] public float placedTime;
    }

    /// <summary>
    /// Event data broadcast when an asset is placed or removed.
    /// </summary>
    [Serializable]
    public class AssetPlacementEventData
    {
        /// <summary>The placed asset instance.</summary>
        public PlacedAsset placedAsset;

        /// <summary>World-space position of the placement.</summary>
        public Vector3 position;

        /// <summary>World-space rotation of the placement.</summary>
        public Quaternion rotation;
    }

    /// <summary>
    /// AR sahnesine 3D varlıkların yerleştirilmesini yönetir.
    /// Handles placing 3D assets into the AR scene. On user tap, performs raycasts
    /// against both AR planes and generated room surfaces (walls, floor), instantiates
    /// the selected asset prefab at the hit point with correct rotation aligned to the
    /// surface normal, supports moving placed assets via drag, maintains a list of all
    /// placed assets, and fires events on place/remove.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="ARRaycastManager"/> on the same GameObject (typically the
    /// XR Origin). Uses Enhanced Touch API for input handling.
    /// </remarks>
    [RequireComponent(typeof(ARRaycastManager))]
    public class AssetPlacer : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Catalog")]
        [Tooltip("Varlıkların seçileceği katalog referansı.")]
        [SerializeField] private AssetCatalog catalog;

        [Header("Raycast Settings")]
        [Tooltip("AR düzlemlerine karşı raycast yapılsın mı?")]
        [SerializeField] private bool raycastAgainstPlanes = true;

        [Tooltip("Oda yüzeylerine (duvar, zemin) karşı raycast yapılsın mı?")]
        [SerializeField] private bool raycastAgainstRoomSurfaces = true;

        [Tooltip("Oda yüzeylerini tanımlamak için kullanılan katman maskesi.")]
        [SerializeField] private LayerMask roomSurfaceLayerMask = ~0;

        [Tooltip("Yerleştirme için maksimum raycast mesafesi (metre).")]
        [SerializeField] [Range(0.5f, 50f)] private float maxRaycastDistance = 10f;

        [Header("Placement Settings")]
        [Tooltip("Yerleştirme sonrası nesne yüzeye hizalansın mı?")]
        [SerializeField] private bool alignToSurfaceNormal = true;

        [Tooltip("Varlığın yüzeyden uzaklık ofseti (metre).")]
        [SerializeField] [Range(0f, 0.1f)] private float surfaceOffset = 0.001f;

        [Tooltip("Sürüklemeyi tetiklemek için minimum parmak hareketi (piksel).")]
        [SerializeField] [Range(1f, 50f)] private float dragThreshold = 10f;

        [Header("Drag Settings")]
        [Tooltip("Sürükleme sırasında pozisyon interpolasyon hızı.")]
        [SerializeField] [Range(1f, 30f)] private float dragSmoothSpeed = 15f;

        [Header("Events — Yerleştirme Olayları")]
        [Tooltip("Bir varlık yerleştirildiğinde tetiklenir.")]
        [SerializeField] private UnityEvent<AssetPlacementEventData> onAssetPlaced;

        [Tooltip("Bir varlık kaldırıldığında tetiklenir.")]
        [SerializeField] private UnityEvent<AssetPlacementEventData> onAssetRemoved;

        [Tooltip("Bir varlık seçildiğinde tetiklenir.")]
        [SerializeField] private UnityEvent<PlacedAsset> onAssetSelected;

        [Tooltip("Seçim kaldırıldığında tetiklenir.")]
        [SerializeField] private UnityEvent onAssetDeselected;

        #endregion

        #region Public Properties

        /// <summary>
        /// Currently selected asset entry from the catalog that will be placed on next tap.
        /// Set to null to switch to selection/drag mode.
        /// </summary>
        public AssetEntry SelectedCatalogEntry { get; set; }

        /// <summary>
        /// Currently selected (tapped) placed asset in the scene.
        /// </summary>
        public PlacedAsset SelectedPlacedAsset { get; private set; }

        /// <summary>
        /// Read-only list of all placed asset instances in the scene.
        /// </summary>
        public IReadOnlyList<PlacedAsset> PlacedAssets => _placedAssets;

        /// <summary>
        /// The asset catalog in use.
        /// </summary>
        public AssetCatalog Catalog
        {
            get => catalog;
            set => catalog = value;
        }

        /// <summary>
        /// Whether the placer is currently in placement mode (has a catalog entry selected).
        /// </summary>
        public bool IsInPlacementMode => SelectedCatalogEntry != null;

        /// <summary>
        /// Whether a drag operation is currently in progress.
        /// </summary>
        public bool IsDragging => _isDragging;

        /// <summary>Event fired when an asset is placed.</summary>
        public UnityEvent<AssetPlacementEventData> OnAssetPlaced => onAssetPlaced;

        /// <summary>Event fired when an asset is removed.</summary>
        public UnityEvent<AssetPlacementEventData> OnAssetRemoved => onAssetRemoved;

        /// <summary>Event fired when an asset is selected.</summary>
        public UnityEvent<PlacedAsset> OnAssetSelected => onAssetSelected;

        /// <summary>Event fired when selection is cleared.</summary>
        public UnityEvent OnAssetDeselected => onAssetDeselected;

        #endregion

        #region Private State

        private ARRaycastManager _arRaycastManager;
        private Camera _arCamera;
        private readonly List<PlacedAsset> _placedAssets = new List<PlacedAsset>();
        private readonly List<ARRaycastHit> _arHits = new List<ARRaycastHit>();

        // Drag state
        private bool _isDragging;
        private bool _hasDragStarted;
        private Vector2 _touchStartPos;
        private PlacedAsset _dragTarget;
        private Vector3 _dragTargetPosition;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _arRaycastManager = GetComponent<ARRaycastManager>();
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown += OnFingerDown;
            UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove += OnFingerMove;
            UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp += OnFingerUp;
        }

        private void OnDisable()
        {
            UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown -= OnFingerDown;
            UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerMove -= OnFingerMove;
            UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerUp -= OnFingerUp;
            EnhancedTouchSupport.Disable();
        }

        private void Start()
        {
            _arCamera = Camera.main;
            if (_arCamera == null)
            {
                Debug.LogError("[AssetPlacer] Ana kamera bulunamadı! AR Camera'nın MainCamera olarak etiketlendiğinden emin olun.");
            }
        }

        private void Update()
        {
            // Smooth drag interpolation
            if (_isDragging && _dragTarget != null)
            {
                _dragTarget.transform.position = Vector3.Lerp(
                    _dragTarget.transform.position,
                    _dragTargetPosition,
                    Time.deltaTime * dragSmoothSpeed);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Places an asset at the specified world position and rotation.
        /// </summary>
        /// <param name="entry">The catalog entry to place.</param>
        /// <param name="position">World-space position.</param>
        /// <param name="rotation">World-space rotation.</param>
        /// <param name="surfaceNormal">The surface normal at the placement point.</param>
        /// <param name="planeId">Optional AR plane trackable ID.</param>
        /// <returns>The newly created <see cref="PlacedAsset"/> component.</returns>
        public PlacedAsset PlaceAsset(
            AssetEntry entry,
            Vector3 position,
            Quaternion rotation,
            Vector3 surfaceNormal,
            TrackableId planeId = default)
        {
            if (entry == null || entry.prefab == null)
            {
                Debug.LogWarning("[AssetPlacer] Geçersiz varlık girişi veya prefab null.");
                return null;
            }

            // Instantiate
            GameObject instance = Instantiate(entry.prefab, position, rotation);
            instance.name = $"Placed_{entry.displayName}_{_placedAssets.Count}";

            // Apply default scale
            float scale = Mathf.Max(entry.defaultScale, 0.01f);
            instance.transform.localScale = Vector3.one * scale;

            // Attach PlacedAsset component
            PlacedAsset placed = instance.AddComponent<PlacedAsset>();
            placed.instanceId = Guid.NewGuid().ToString("N");
            placed.sourceEntry = entry;
            placed.placementNormal = surfaceNormal;
            placed.arPlaneId = planeId;
            placed.placedTime = Time.time;

            _placedAssets.Add(placed);

            // Fire event
            var eventData = new AssetPlacementEventData
            {
                placedAsset = placed,
                position = position,
                rotation = rotation
            };
            onAssetPlaced?.Invoke(eventData);

            Debug.Log($"[AssetPlacer] Varlık yerleştirildi: {entry.displayName} @ {position}");
            return placed;
        }

        /// <summary>
        /// Removes a placed asset from the scene and the internal registry.
        /// </summary>
        /// <param name="placed">The placed asset to remove.</param>
        public void RemoveAsset(PlacedAsset placed)
        {
            if (placed == null) return;

            if (SelectedPlacedAsset == placed)
                DeselectAsset();

            _placedAssets.Remove(placed);

            var eventData = new AssetPlacementEventData
            {
                placedAsset = placed,
                position = placed.transform.position,
                rotation = placed.transform.rotation
            };
            onAssetRemoved?.Invoke(eventData);

            Debug.Log($"[AssetPlacer] Varlık kaldırıldı: {placed.sourceEntry?.displayName}");
            Destroy(placed.gameObject);
        }

        /// <summary>
        /// Removes all placed assets from the scene.
        /// </summary>
        public void RemoveAllAssets()
        {
            for (int i = _placedAssets.Count - 1; i >= 0; i--)
            {
                RemoveAsset(_placedAssets[i]);
            }
        }

        /// <summary>
        /// Selects a placed asset for transformation or inspection.
        /// </summary>
        /// <param name="placed">The asset to select.</param>
        public void SelectAsset(PlacedAsset placed)
        {
            if (placed == null) return;

            if (SelectedPlacedAsset != null && SelectedPlacedAsset != placed)
                DeselectAsset();

            SelectedPlacedAsset = placed;
            onAssetSelected?.Invoke(placed);
            Debug.Log($"[AssetPlacer] Varlık seçildi: {placed.sourceEntry?.displayName}");
        }

        /// <summary>
        /// Clears the current asset selection.
        /// </summary>
        public void DeselectAsset()
        {
            if (SelectedPlacedAsset == null) return;

            SelectedPlacedAsset = null;
            onAssetDeselected?.Invoke();
        }

        /// <summary>
        /// Sets the selected catalog entry for placement mode.
        /// </summary>
        /// <param name="entryIndex">Index into the catalog entries list.</param>
        public void SelectCatalogEntry(int entryIndex)
        {
            if (catalog == null || entryIndex < 0 || entryIndex >= catalog.Count)
            {
                Debug.LogWarning($"[AssetPlacer] Geçersiz katalog indeksi: {entryIndex}");
                SelectedCatalogEntry = null;
                return;
            }

            SelectedCatalogEntry = catalog.Entries[entryIndex];
            Debug.Log($"[AssetPlacer] Katalog girişi seçildi: {SelectedCatalogEntry.displayName}");
        }

        /// <summary>
        /// Exits placement mode and switches to selection/interaction mode.
        /// </summary>
        public void ExitPlacementMode()
        {
            SelectedCatalogEntry = null;
        }

        #endregion

        #region Input Handling

        /// <summary>
        /// Handles initial finger touch — determines if we're placing or selecting.
        /// </summary>
        private void OnFingerDown(Finger finger)
        {
            // Only handle single-finger input
            if (finger.index != 0) return;
            if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers.Count > 1) return;

            _touchStartPos = finger.currentTouch.screenPosition;
            _hasDragStarted = false;
            _isDragging = false;

            // Check if tapping on an existing placed asset
            if (!IsInPlacementMode)
            {
                PlacedAsset tapped = RaycastForPlacedAsset(finger.currentTouch.screenPosition);
                if (tapped != null)
                {
                    SelectAsset(tapped);
                    _dragTarget = tapped;
                    _dragTargetPosition = tapped.transform.position;
                    return;
                }
                else
                {
                    DeselectAsset();
                }
            }
        }

        /// <summary>
        /// Handles finger movement for drag operations.
        /// </summary>
        private void OnFingerMove(Finger finger)
        {
            if (finger.index != 0) return;
            if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers.Count > 1) return;

            // Check drag threshold
            if (!_hasDragStarted)
            {
                float dist = Vector2.Distance(_touchStartPos, finger.currentTouch.screenPosition);
                if (dist < dragThreshold) return;
                _hasDragStarted = true;
            }

            // Drag selected asset
            if (_dragTarget != null && !IsInPlacementMode)
            {
                _isDragging = true;
                if (TryGetSurfaceHitPoint(finger.currentTouch.screenPosition, out Vector3 hitPos, out Vector3 hitNormal, out TrackableId _))
                {
                    _dragTargetPosition = hitPos + hitNormal * surfaceOffset;
                }
            }
        }

        /// <summary>
        /// Handles finger lift — finalizes placement or drag.
        /// </summary>
        private void OnFingerUp(Finger finger)
        {
            if (finger.index != 0) return;

            // If we didn't drag and we're in placement mode, place asset
            if (!_hasDragStarted && IsInPlacementMode)
            {
                TryPlaceAtScreenPoint(finger.currentTouch.screenPosition);
            }

            // Reset drag state
            if (_isDragging && _dragTarget != null)
            {
                // Snap to final position
                _dragTarget.transform.position = _dragTargetPosition;
            }

            _isDragging = false;
            _hasDragStarted = false;
            _dragTarget = null;
        }

        #endregion

        #region Raycasting

        /// <summary>
        /// Attempts to place the currently selected catalog entry at the given screen position.
        /// </summary>
        /// <param name="screenPos">Screen-space touch position.</param>
        /// <returns>True if placement succeeded.</returns>
        private bool TryPlaceAtScreenPoint(Vector2 screenPos)
        {
            if (SelectedCatalogEntry == null) return false;

            if (TryGetSurfaceHitPoint(screenPos, out Vector3 hitPos, out Vector3 hitNormal, out TrackableId planeId))
            {
                Vector3 placementPos = hitPos + hitNormal * surfaceOffset;
                Quaternion placementRot = CalculatePlacementRotation(hitNormal);

                PlaceAsset(SelectedCatalogEntry, placementPos, placementRot, hitNormal, planeId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Performs a combined raycast against AR planes and room surfaces
        /// and returns the closest hit.
        /// </summary>
        /// <param name="screenPos">Screen-space position to raycast from.</param>
        /// <param name="hitPosition">Output world-space hit position.</param>
        /// <param name="hitNormal">Output surface normal at hit point.</param>
        /// <param name="planeId">Output AR plane trackable ID (default if hit was on a room surface).</param>
        /// <returns>True if any surface was hit.</returns>
        private bool TryGetSurfaceHitPoint(
            Vector2 screenPos,
            out Vector3 hitPosition,
            out Vector3 hitNormal,
            out TrackableId planeId)
        {
            hitPosition = Vector3.zero;
            hitNormal = Vector3.up;
            planeId = default;

            float closestDist = float.MaxValue;
            bool hasHit = false;

            // 1) AR Plane raycast
            if (raycastAgainstPlanes && _arRaycastManager != null)
            {
                _arHits.Clear();
                if (_arRaycastManager.Raycast(screenPos, _arHits, TrackableType.PlaneWithinPolygon))
                {
                    var arHit = _arHits[0]; // Sorted by distance
                    float dist = Vector3.Distance(_arCamera.transform.position, arHit.pose.position);
                    if (dist < closestDist && dist <= maxRaycastDistance)
                    {
                        closestDist = dist;
                        hitPosition = arHit.pose.position;
                        hitNormal = arHit.pose.up;
                        planeId = arHit.trackableId;
                        hasHit = true;
                    }
                }
            }

            // 2) Physics raycast against room surfaces
            if (raycastAgainstRoomSurfaces && _arCamera != null)
            {
                Ray ray = _arCamera.ScreenPointToRay(screenPos);
                if (Physics.Raycast(ray, out RaycastHit physicsHit, maxRaycastDistance, roomSurfaceLayerMask))
                {
                    float dist = physicsHit.distance;
                    if (dist < closestDist)
                    {
                        hitPosition = physicsHit.point;
                        hitNormal = physicsHit.normal;
                        planeId = default;
                        hasHit = true;
                    }
                }
            }

            return hasHit;
        }

        /// <summary>
        /// Raycasts from a screen position to find a placed asset under the finger.
        /// </summary>
        /// <param name="screenPos">Screen-space position.</param>
        /// <returns>The <see cref="PlacedAsset"/> hit, or null.</returns>
        private PlacedAsset RaycastForPlacedAsset(Vector2 screenPos)
        {
            if (_arCamera == null) return null;

            Ray ray = _arCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance))
            {
                // Walk up the hierarchy to find PlacedAsset component
                PlacedAsset placed = hit.collider.GetComponentInParent<PlacedAsset>();
                return placed;
            }
            return null;
        }

        /// <summary>
        /// Calculates the rotation for a placed asset based on the surface normal.
        /// For floor-like surfaces (normal pointing up), the asset is placed upright.
        /// For walls, the asset is rotated to face outward from the wall.
        /// </summary>
        /// <param name="surfaceNormal">The surface normal at the hit point.</param>
        /// <returns>The placement rotation.</returns>
        private Quaternion CalculatePlacementRotation(Vector3 surfaceNormal)
        {
            if (!alignToSurfaceNormal)
                return Quaternion.identity;

            // Determine if this is a horizontal surface (floor/ceiling) or vertical (wall)
            float dot = Vector3.Dot(surfaceNormal, Vector3.up);

            if (Mathf.Abs(dot) > 0.7f)
            {
                // Horizontal surface — place upright, face camera
                Vector3 cameraForward = _arCamera.transform.forward;
                cameraForward.y = 0;
                if (cameraForward.sqrMagnitude < 0.001f)
                    cameraForward = Vector3.forward;
                cameraForward.Normalize();

                return Quaternion.LookRotation(cameraForward, Vector3.up);
            }
            else
            {
                // Vertical surface (wall) — align up with world up, forward along surface normal
                return Quaternion.LookRotation(surfaceNormal, Vector3.up);
            }
        }

        #endregion
    }
}
