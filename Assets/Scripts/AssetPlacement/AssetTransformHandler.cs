using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARRoomTransformer
{
    /// <summary>
    /// Seçili varlıkların dokunma tabanlı dönüşümlerini yönetir.
    /// Handles touch-based transformation of selected assets in the AR scene.
    /// <list type="bullet">
    /// <item>Single finger drag → move on surface plane</item>
    /// <item>Two finger pinch → scale</item>
    /// <item>Two finger rotate → rotate around Y axis</item>
    /// </list>
    /// Provides optional grid snap, angle snap, scale clamping, and smooth interpolation.
    /// </summary>
    /// <remarks>
    /// Works in tandem with <see cref="AssetPlacer"/>. Subscribe to the placer's
    /// selection events or set <see cref="TargetAsset"/> directly.
    /// </remarks>
    [RequireComponent(typeof(ARRaycastManager))]
    public class AssetTransformHandler : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Target")]
        [Tooltip("Dönüştürülecek hedef varlık. AssetPlacer tarafından otomatik olarak ayarlanabilir.")]
        [SerializeField] private PlacedAsset targetAsset;

        [Header("Move Settings — Hareket Ayarları")]
        [Tooltip("Hareket sırasında pozisyon interpolasyon hızı.")]
        [SerializeField] [Range(1f, 30f)] private float moveSmoothSpeed = 12f;

        [Tooltip("Izgara yakalama etkinleştirilsin mi?")]
        [SerializeField] private bool enableGridSnap;

        [Tooltip("Izgara yakalama boyutu (metre).")]
        [SerializeField] [Range(0.01f, 1f)] private float gridSnapSize = 0.1f;

        [Header("Scale Settings — Ölçek Ayarları")]
        [Tooltip("Minimum ölçek çarpanı.")]
        [SerializeField] [Range(0.01f, 1f)] private float minScale = 0.1f;

        [Tooltip("Maksimum ölçek çarpanı.")]
        [SerializeField] [Range(1f, 50f)] private float maxScale = 10f;

        [Tooltip("Ölçek değişim hassasiyeti.")]
        [SerializeField] [Range(0.1f, 5f)] private float scaleSensitivity = 1f;

        [Tooltip("Ölçek interpolasyon hızı.")]
        [SerializeField] [Range(1f, 30f)] private float scaleSmoothSpeed = 10f;

        [Header("Rotation Settings — Döndürme Ayarları")]
        [Tooltip("Açı yakalama etkinleştirilsin mi?")]
        [SerializeField] private bool enableAngleSnap;

        [Tooltip("Açı yakalama adımı (derece).")]
        [SerializeField] [Range(1f, 90f)] private float angleSnapStep = 15f;

        [Tooltip("Döndürme hassasiyeti.")]
        [SerializeField] [Range(0.1f, 5f)] private float rotationSensitivity = 1f;

        [Tooltip("Döndürme interpolasyon hızı.")]
        [SerializeField] [Range(1f, 30f)] private float rotationSmoothSpeed = 10f;

        [Header("Raycast")]
        [Tooltip("Hareket için yüzey raycast katman maskesi.")]
        [SerializeField] private LayerMask surfaceLayerMask = ~0;

        [Tooltip("Maksimum raycast mesafesi (metre).")]
        [SerializeField] [Range(0.5f, 50f)] private float maxRaycastDistance = 10f;

        [Header("Events — Olaylar")]
        [Tooltip("Dönüşüm başladığında tetiklenir.")]
        [SerializeField] private UnityEvent<PlacedAsset> onTransformStarted;

        [Tooltip("Dönüşüm sona erdiğinde tetiklenir.")]
        [SerializeField] private UnityEvent<PlacedAsset> onTransformEnded;

        #endregion

        #region Public Properties

        /// <summary>
        /// The currently targeted placed asset for transformation.
        /// </summary>
        public PlacedAsset TargetAsset
        {
            get => targetAsset;
            set => targetAsset = value;
        }

        /// <summary>Whether grid snapping is enabled.</summary>
        public bool EnableGridSnap
        {
            get => enableGridSnap;
            set => enableGridSnap = value;
        }

        /// <summary>Grid snap size in meters.</summary>
        public float GridSnapSize
        {
            get => gridSnapSize;
            set => gridSnapSize = Mathf.Max(0.01f, value);
        }

        /// <summary>Whether angle snapping is enabled.</summary>
        public bool EnableAngleSnap
        {
            get => enableAngleSnap;
            set => enableAngleSnap = value;
        }

        /// <summary>Angle snap step in degrees.</summary>
        public float AngleSnapStep
        {
            get => angleSnapStep;
            set => angleSnapStep = Mathf.Clamp(value, 1f, 90f);
        }

        /// <summary>Whether any transformation is currently active.</summary>
        public bool IsTransforming => _currentGesture != GestureType.None;

        /// <summary>The type of gesture currently being performed.</summary>
        public GestureType CurrentGesture => _currentGesture;

        #endregion

        #region Enums

        /// <summary>
        /// Types of touch gestures recognized by the transform handler.
        /// </summary>
        public enum GestureType
        {
            /// <summary>No active gesture.</summary>
            None,
            /// <summary>Single-finger drag to move.</summary>
            Move,
            /// <summary>Two-finger pinch to scale.</summary>
            Scale,
            /// <summary>Two-finger rotate.</summary>
            Rotate,
            /// <summary>Combined pinch + rotate.</summary>
            ScaleRotate
        }

        #endregion

        #region Private State

        private ARRaycastManager _arRaycastManager;
        private Camera _arCamera;

        private GestureType _currentGesture;
        private bool _isTransformActive;

        // Move state
        private Vector3 _targetPosition;

        // Scale state
        private float _initialPinchDistance;
        private float _initialScale;
        private float _targetScale;

        // Rotate state
        private float _initialAngle;
        private float _initialRotationY;
        private float _targetRotationY;

        // Two-finger tracking
        private Vector2 _prevFinger0Pos;
        private Vector2 _prevFinger1Pos;

        private readonly System.Collections.Generic.List<ARRaycastHit> _arHits =
            new System.Collections.Generic.List<ARRaycastHit>();

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
        }

        private void Update()
        {
            if (targetAsset == null || !_isTransformActive) return;

            ApplySmoothTransforms();
        }

        #endregion

        #region Smooth Transform Application

        /// <summary>
        /// Smoothly interpolates position, scale, and rotation toward their targets.
        /// </summary>
        private void ApplySmoothTransforms()
        {
            Transform t = targetAsset.transform;
            float dt = Time.deltaTime;

            // Position
            if (_currentGesture == GestureType.Move)
            {
                Vector3 snappedPos = enableGridSnap ? SnapToGrid(_targetPosition) : _targetPosition;
                t.position = Vector3.Lerp(t.position, snappedPos, dt * moveSmoothSpeed);
            }

            // Scale
            if (_currentGesture == GestureType.Scale || _currentGesture == GestureType.ScaleRotate)
            {
                float clampedScale = Mathf.Clamp(_targetScale, minScale, maxScale);
                float currentUniformScale = t.localScale.x;
                float smoothedScale = Mathf.Lerp(currentUniformScale, clampedScale, dt * scaleSmoothSpeed);
                t.localScale = Vector3.one * smoothedScale;
            }

            // Rotation
            if (_currentGesture == GestureType.Rotate || _currentGesture == GestureType.ScaleRotate)
            {
                float snappedRot = enableAngleSnap ? SnapAngle(_targetRotationY) : _targetRotationY;
                Vector3 euler = t.eulerAngles;
                euler.y = Mathf.LerpAngle(euler.y, snappedRot, dt * rotationSmoothSpeed);
                t.eulerAngles = euler;
            }
        }

        #endregion

        #region Input Handling

        private void OnFingerDown(Finger finger)
        {
            if (targetAsset == null) return;

            int activeCount = UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers.Count;

            if (activeCount == 2 && finger.index <= 1)
            {
                // Two fingers down — initialize pinch/rotate
                BeginTwoFingerGesture();
            }
        }

        private void OnFingerMove(Finger finger)
        {
            if (targetAsset == null) return;

            int activeCount = UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers.Count;

            if (activeCount == 1 && finger.index == 0)
            {
                HandleSingleFingerMove(finger);
            }
            else if (activeCount == 2)
            {
                HandleTwoFingerMove();
            }
        }

        private void OnFingerUp(Finger finger)
        {
            if (targetAsset == null) return;

            int activeCount = UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers.Count;

            // When last finger lifts, end gesture
            if (activeCount <= 1)
            {
                EndGesture();
            }
        }

        /// <summary>
        /// Handles single-finger drag for movement on the surface plane.
        /// </summary>
        private void HandleSingleFingerMove(Finger finger)
        {
            if (!_isTransformActive)
            {
                _isTransformActive = true;
                _currentGesture = GestureType.Move;
                _targetPosition = targetAsset.transform.position;
                onTransformStarted?.Invoke(targetAsset);
            }

            if (_currentGesture != GestureType.Move) return;

            Vector2 screenPos = finger.currentTouch.screenPosition;

            // Try AR plane raycast first, then physics raycast
            if (TryGetSurfacePoint(screenPos, out Vector3 hitPoint))
            {
                _targetPosition = hitPoint;
            }
        }

        /// <summary>
        /// Initializes the two-finger gesture tracking state.
        /// </summary>
        private void BeginTwoFingerGesture()
        {
            var fingers = UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers;
            if (fingers.Count < 2) return;

            _prevFinger0Pos = fingers[0].currentTouch.screenPosition;
            _prevFinger1Pos = fingers[1].currentTouch.screenPosition;

            _initialPinchDistance = Vector2.Distance(_prevFinger0Pos, _prevFinger1Pos);
            _initialScale = targetAsset.transform.localScale.x;
            _targetScale = _initialScale;

            _initialAngle = GetTwoFingerAngle(_prevFinger0Pos, _prevFinger1Pos);
            _initialRotationY = targetAsset.transform.eulerAngles.y;
            _targetRotationY = _initialRotationY;

            _isTransformActive = true;
            _currentGesture = GestureType.ScaleRotate;
            onTransformStarted?.Invoke(targetAsset);
        }

        /// <summary>
        /// Handles two-finger gestures: pinch (scale) and twist (rotation).
        /// </summary>
        private void HandleTwoFingerMove()
        {
            var fingers = UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers;
            if (fingers.Count < 2) return;

            Vector2 pos0 = fingers[0].currentTouch.screenPosition;
            Vector2 pos1 = fingers[1].currentTouch.screenPosition;

            // --- Scale (Pinch) ---
            float currentDistance = Vector2.Distance(pos0, pos1);
            if (_initialPinchDistance > 0.01f)
            {
                float scaleRatio = currentDistance / _initialPinchDistance;
                _targetScale = _initialScale * Mathf.Pow(scaleRatio, scaleSensitivity);
                _targetScale = Mathf.Clamp(_targetScale, minScale, maxScale);
            }

            // --- Rotation (Twist) ---
            float currentAngle = GetTwoFingerAngle(pos0, pos1);
            float angleDelta = Mathf.DeltaAngle(_initialAngle, currentAngle);
            _targetRotationY = _initialRotationY - angleDelta * rotationSensitivity;

            _prevFinger0Pos = pos0;
            _prevFinger1Pos = pos1;
        }

        /// <summary>
        /// Ends the current gesture and fires the transform-ended event.
        /// </summary>
        private void EndGesture()
        {
            if (!_isTransformActive) return;

            // Apply final snap
            if (targetAsset != null)
            {
                Transform t = targetAsset.transform;

                if (enableGridSnap && _currentGesture == GestureType.Move)
                {
                    t.position = SnapToGrid(_targetPosition);
                }

                if (enableAngleSnap &&
                    (_currentGesture == GestureType.Rotate || _currentGesture == GestureType.ScaleRotate))
                {
                    Vector3 euler = t.eulerAngles;
                    euler.y = SnapAngle(_targetRotationY);
                    t.eulerAngles = euler;
                }

                // Final scale clamp
                if (_currentGesture == GestureType.Scale || _currentGesture == GestureType.ScaleRotate)
                {
                    float finalScale = Mathf.Clamp(t.localScale.x, minScale, maxScale);
                    t.localScale = Vector3.one * finalScale;
                }
            }

            _isTransformActive = false;
            _currentGesture = GestureType.None;
            onTransformEnded?.Invoke(targetAsset);
        }

        #endregion

        #region Surface Raycasting

        /// <summary>
        /// Attempts to find a surface point at the given screen position using AR planes
        /// and physics raycasts.
        /// </summary>
        /// <param name="screenPos">Screen-space position.</param>
        /// <param name="hitPoint">Output world-space hit position.</param>
        /// <returns>True if a surface was hit.</returns>
        private bool TryGetSurfacePoint(Vector2 screenPos, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            float closestDist = float.MaxValue;
            bool hasHit = false;

            // AR Plane raycast
            if (_arRaycastManager != null)
            {
                _arHits.Clear();
                if (_arRaycastManager.Raycast(screenPos, _arHits, TrackableType.PlaneWithinPolygon))
                {
                    var hit = _arHits[0];
                    float dist = Vector3.Distance(_arCamera.transform.position, hit.pose.position);
                    if (dist < closestDist && dist <= maxRaycastDistance)
                    {
                        closestDist = dist;
                        hitPoint = hit.pose.position;
                        hasHit = true;
                    }
                }
            }

            // Physics raycast
            if (_arCamera != null)
            {
                Ray ray = _arCamera.ScreenPointToRay(screenPos);
                if (Physics.Raycast(ray, out RaycastHit physHit, maxRaycastDistance, surfaceLayerMask))
                {
                    if (physHit.distance < closestDist)
                    {
                        hitPoint = physHit.point;
                        hasHit = true;
                    }
                }
            }

            return hasHit;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Calculates the angle (in degrees) of the line between two screen-space points.
        /// </summary>
        private float GetTwoFingerAngle(Vector2 pos0, Vector2 pos1)
        {
            Vector2 delta = pos1 - pos0;
            return Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Snaps a world position to the nearest grid point.
        /// </summary>
        /// <param name="position">The position to snap.</param>
        /// <returns>The snapped position.</returns>
        private Vector3 SnapToGrid(Vector3 position)
        {
            if (gridSnapSize <= 0.001f) return position;

            float inv = 1f / gridSnapSize;
            return new Vector3(
                Mathf.Round(position.x * inv) / inv,
                position.y, // Don't snap vertical axis
                Mathf.Round(position.z * inv) / inv
            );
        }

        /// <summary>
        /// Snaps a rotation angle to the nearest step.
        /// </summary>
        /// <param name="angle">The angle in degrees.</param>
        /// <returns>The snapped angle.</returns>
        private float SnapAngle(float angle)
        {
            if (angleSnapStep <= 0.01f) return angle;
            return Mathf.Round(angle / angleSnapStep) * angleSnapStep;
        }

        #endregion
    }
}
