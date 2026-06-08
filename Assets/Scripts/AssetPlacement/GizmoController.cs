using System;
using UnityEngine;
using UnityEngine.Events;

namespace ARRoomTransformer
{
    /// <summary>
    /// Seçili nesne üzerinde görsel dönüşüm gizmosu gösterir.
    /// Shows a visual transform gizmo on the selected object with:
    /// <list type="bullet">
    /// <item>3 colored arrows (Red/Green/Blue for X/Y/Z) for axis movement</item>
    /// <item>Rotation ring for Y-axis rotation</item>
    /// <item>Scale handles at corners</item>
    /// <item>Active axis highlighting during drag</item>
    /// <item>Toggle on/off support</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// The gizmo procedurally generates its visual elements at runtime using
    /// <see cref="GL"/> line drawing and primitive meshes. Attach this component
    /// to the XR Origin or a manager GameObject.
    /// </remarks>
    public class GizmoController : MonoBehaviour
    {
        #region Enums

        /// <summary>
        /// Represents the axis or handle currently being interacted with.
        /// </summary>
        public enum GizmoAxis
        {
            /// <summary>No axis selected.</summary>
            None,
            /// <summary>X axis (Red).</summary>
            X,
            /// <summary>Y axis (Green).</summary>
            Y,
            /// <summary>Z axis (Blue).</summary>
            Z,
            /// <summary>Rotation ring.</summary>
            Rotation,
            /// <summary>Uniform scale handle.</summary>
            Scale
        }

        /// <summary>
        /// The transform operation mode of the gizmo.
        /// </summary>
        public enum GizmoMode
        {
            /// <summary>Translation mode — axis arrows.</summary>
            Translate,
            /// <summary>Rotation mode — rotation ring.</summary>
            Rotate,
            /// <summary>Scale mode — corner handles.</summary>
            Scale
        }

        #endregion

        #region Serialized Fields

        [Header("Gizmo Appearance — Gizmo Görünümü")]
        [Tooltip("Gizmo görünür mü?")]
        [SerializeField] private bool isVisible = true;

        [Tooltip("Aktif gizmo modu.")]
        [SerializeField] private GizmoMode currentMode = GizmoMode.Translate;

        [Tooltip("Gizmo ok uzunluğu (metre).")]
        [SerializeField] [Range(0.05f, 1f)] private float arrowLength = 0.15f;

        [Tooltip("Gizmo ok kalınlığı.")]
        [SerializeField] [Range(0.001f, 0.05f)] private float arrowThickness = 0.005f;

        [Tooltip("Döndürme halkası yarıçapı (metre).")]
        [SerializeField] [Range(0.05f, 1f)] private float rotationRingRadius = 0.12f;

        [Tooltip("Ölçek tutamak boyutu (metre).")]
        [SerializeField] [Range(0.01f, 0.1f)] private float scaleHandleSize = 0.02f;

        [Tooltip("Gizmo'nun kameraya olan mesafeye göre ölçeklenmesi.")]
        [SerializeField] private bool scaleWithDistance = true;

        [Tooltip("Mesafe ölçekleme faktörü.")]
        [SerializeField] [Range(0.01f, 1f)] private float distanceScaleFactor = 0.15f;

        [Header("Colors — Renkler")]
        [SerializeField] private Color xAxisColor = new Color(1f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color yAxisColor = new Color(0.2f, 1f, 0.2f, 0.9f);
        [SerializeField] private Color zAxisColor = new Color(0.3f, 0.5f, 1f, 0.9f);
        [SerializeField] private Color rotationRingColor = new Color(1f, 1f, 0.2f, 0.7f);
        [SerializeField] private Color scaleHandleColor = new Color(1f, 0.6f, 0f, 0.9f);
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 1f, 1f);

        [Header("Target — Hedef")]
        [Tooltip("Gizmo'nun gösterileceği hedef transform.")]
        [SerializeField] private Transform targetTransform;

        [Header("Events — Olaylar")]
        [SerializeField] private UnityEvent<GizmoAxis> onAxisHoverEnter;
        [SerializeField] private UnityEvent<GizmoAxis> onAxisHoverExit;
        [SerializeField] private UnityEvent<GizmoAxis> onAxisDragStart;
        [SerializeField] private UnityEvent<GizmoAxis> onAxisDragEnd;

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether the gizmo is currently visible.
        /// </summary>
        public bool IsVisible
        {
            get => isVisible;
            set
            {
                isVisible = value;
                UpdateVisibility();
            }
        }

        /// <summary>
        /// The current gizmo operation mode.
        /// </summary>
        public GizmoMode CurrentMode
        {
            get => currentMode;
            set
            {
                currentMode = value;
                RebuildGizmo();
            }
        }

        /// <summary>
        /// The currently active (highlighted) axis.
        /// </summary>
        public GizmoAxis ActiveAxis { get; private set; } = GizmoAxis.None;

        /// <summary>
        /// The target transform the gizmo tracks.
        /// </summary>
        public Transform TargetTransform
        {
            get => targetTransform;
            set
            {
                targetTransform = value;
                if (value != null)
                {
                    IsVisible = true;
                    RebuildGizmo();
                }
                else
                {
                    IsVisible = false;
                }
            }
        }

        #endregion

        #region Private State

        private Camera _camera;
        private GameObject _gizmoRoot;

        // Arrow components
        private GameObject _xArrow;
        private GameObject _yArrow;
        private GameObject _zArrow;
        private Renderer _xArrowRenderer;
        private Renderer _yArrowRenderer;
        private Renderer _zArrowRenderer;

        // Arrow tip (cone) components
        private GameObject _xTip;
        private GameObject _yTip;
        private GameObject _zTip;
        private Renderer _xTipRenderer;
        private Renderer _yTipRenderer;
        private Renderer _zTipRenderer;

        // Rotation ring
        private GameObject _rotationRing;
        private LineRenderer _rotationLineRenderer;

        // Scale handles
        private GameObject[] _scaleHandles;
        private Renderer[] _scaleHandleRenderers;

        // Material cache
        private Material _xMat;
        private Material _yMat;
        private Material _zMat;
        private Material _rotMat;
        private Material _scaleMat;
        private Material _highlightMat;

        private static readonly int s_colorPropId = Shader.PropertyToID("_Color");
        private static readonly int s_baseColorPropId = Shader.PropertyToID("_BaseColor");
        private const int ROTATION_RING_SEGMENTS = 64;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _camera = Camera.main;
            CreateMaterials();
            BuildGizmoHierarchy();
        }

        private void Start()
        {
            UpdateVisibility();
        }

        private void LateUpdate()
        {
            if (targetTransform == null || !isVisible)
                return;

            // Follow target
            _gizmoRoot.transform.position = targetTransform.position;

            // Scale with distance for consistent screen-space size
            if (scaleWithDistance && _camera != null)
            {
                float dist = Vector3.Distance(_camera.transform.position, targetTransform.position);
                float scale = dist * distanceScaleFactor;
                _gizmoRoot.transform.localScale = Vector3.one * Mathf.Max(scale, 0.01f);
            }
            else
            {
                _gizmoRoot.transform.localScale = Vector3.one;
            }
        }

        private void OnDestroy()
        {
            DestroyMaterial(_xMat);
            DestroyMaterial(_yMat);
            DestroyMaterial(_zMat);
            DestroyMaterial(_rotMat);
            DestroyMaterial(_scaleMat);
            DestroyMaterial(_highlightMat);

            if (_gizmoRoot != null)
                Destroy(_gizmoRoot);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Toggles the gizmo visibility on or off.
        /// </summary>
        public void ToggleVisibility()
        {
            IsVisible = !IsVisible;
        }

        /// <summary>
        /// Cycles through gizmo modes (Translate → Rotate → Scale → Translate).
        /// </summary>
        public void CycleMode()
        {
            int next = ((int)currentMode + 1) % 3;
            CurrentMode = (GizmoMode)next;
        }

        /// <summary>
        /// Sets the active (highlighted) axis. Call with <see cref="GizmoAxis.None"/> to clear.
        /// </summary>
        /// <param name="axis">The axis to highlight.</param>
        public void SetActiveAxis(GizmoAxis axis)
        {
            GizmoAxis previous = ActiveAxis;
            ActiveAxis = axis;

            // Restore previous axis color
            if (previous != GizmoAxis.None && previous != axis)
            {
                SetAxisColor(previous, false);
                onAxisHoverExit?.Invoke(previous);
            }

            // Highlight new axis
            if (axis != GizmoAxis.None)
            {
                SetAxisColor(axis, true);
                onAxisHoverEnter?.Invoke(axis);
            }
        }

        /// <summary>
        /// Notifies the gizmo that a drag has started on the given axis.
        /// </summary>
        /// <param name="axis">The axis being dragged.</param>
        public void BeginDrag(GizmoAxis axis)
        {
            SetActiveAxis(axis);
            onAxisDragStart?.Invoke(axis);
        }

        /// <summary>
        /// Notifies the gizmo that a drag has ended.
        /// </summary>
        public void EndDrag()
        {
            GizmoAxis prev = ActiveAxis;
            SetActiveAxis(GizmoAxis.None);
            onAxisDragEnd?.Invoke(prev);
        }

        /// <summary>
        /// Performs a screen-space hit test against gizmo handles to determine
        /// which axis (if any) is under the given screen position.
        /// </summary>
        /// <param name="screenPos">Screen-space position to test.</param>
        /// <returns>The <see cref="GizmoAxis"/> under the position, or None.</returns>
        public GizmoAxis HitTest(Vector2 screenPos)
        {
            if (_camera == null || targetTransform == null || !isVisible)
                return GizmoAxis.None;

            Ray ray = _camera.ScreenPointToRay(screenPos);
            float hitRadius = arrowThickness * 4f; // Generous hit area for touch

            if (currentMode == GizmoMode.Translate)
            {
                // Test each arrow axis
                if (TestAxisHit(ray, Vector3.right, hitRadius)) return GizmoAxis.X;
                if (TestAxisHit(ray, Vector3.up, hitRadius)) return GizmoAxis.Y;
                if (TestAxisHit(ray, Vector3.forward, hitRadius)) return GizmoAxis.Z;
            }
            else if (currentMode == GizmoMode.Rotate)
            {
                // Test rotation ring
                if (TestRotationRingHit(ray)) return GizmoAxis.Rotation;
            }
            else if (currentMode == GizmoMode.Scale)
            {
                if (TestScaleHandleHit(ray)) return GizmoAxis.Scale;
            }

            return GizmoAxis.None;
        }

        #endregion

        #region Gizmo Construction

        /// <summary>
        /// Creates the shared unlit materials for gizmo elements.
        /// </summary>
        private void CreateMaterials()
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null)
                unlitShader = Shader.Find("Unlit/Color");

            _xMat = CreateColorMaterial(unlitShader, xAxisColor);
            _yMat = CreateColorMaterial(unlitShader, yAxisColor);
            _zMat = CreateColorMaterial(unlitShader, zAxisColor);
            _rotMat = CreateColorMaterial(unlitShader, rotationRingColor);
            _scaleMat = CreateColorMaterial(unlitShader, scaleHandleColor);
            _highlightMat = CreateColorMaterial(unlitShader, highlightColor);
        }

        /// <summary>
        /// Creates a single unlit color material.
        /// </summary>
        private Material CreateColorMaterial(Shader shader, Color color)
        {
            var mat = new Material(shader);
            mat.SetColor(s_colorPropId, color);
            mat.SetColor(s_baseColorPropId, color);

            // Enable transparency
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);
            mat.renderQueue = 3100;
            return mat;
        }

        /// <summary>
        /// Builds the complete gizmo GameObject hierarchy.
        /// </summary>
        private void BuildGizmoHierarchy()
        {
            _gizmoRoot = new GameObject("GizmoRoot");
            _gizmoRoot.transform.SetParent(transform, false);

            BuildTranslateGizmo();
            BuildRotationGizmo();
            BuildScaleGizmo();

            RebuildGizmo();
        }

        /// <summary>
        /// Builds the translation arrows (3 axis cylinders with cone tips).
        /// </summary>
        private void BuildTranslateGizmo()
        {
            _xArrow = CreateArrowShaft("XArrow", Vector3.right, _xMat, out _xArrowRenderer);
            _xTip = CreateArrowTip("XTip", Vector3.right, _xMat, out _xTipRenderer);

            _yArrow = CreateArrowShaft("YArrow", Vector3.up, _yMat, out _yArrowRenderer);
            _yTip = CreateArrowTip("YTip", Vector3.up, _yMat, out _yTipRenderer);

            _zArrow = CreateArrowShaft("ZArrow", Vector3.forward, _zMat, out _zArrowRenderer);
            _zTip = CreateArrowTip("ZTip", Vector3.forward, _zMat, out _zTipRenderer);
        }

        /// <summary>
        /// Creates an arrow shaft (cylinder) along the given axis direction.
        /// </summary>
        private GameObject CreateArrowShaft(string name, Vector3 direction, Material mat, out Renderer rend)
        {
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = name;
            shaft.transform.SetParent(_gizmoRoot.transform, false);

            // Remove collider (we do custom hit testing)
            DestroyImmediate(shaft.GetComponent<Collider>());

            // Position: center of arrow along axis
            shaft.transform.localPosition = direction * (arrowLength * 0.5f);

            // Scale: thin cylinder
            shaft.transform.localScale = new Vector3(arrowThickness, arrowLength * 0.5f, arrowThickness);

            // Rotation: align cylinder (default Y-up) to the target direction
            shaft.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction);

            rend = shaft.GetComponent<Renderer>();
            rend.material = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;

            return shaft;
        }

        /// <summary>
        /// Creates an arrow tip (small sphere as cone proxy) at the end of the arrow.
        /// </summary>
        private GameObject CreateArrowTip(string name, Vector3 direction, Material mat, out Renderer rend)
        {
            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = name;
            tip.transform.SetParent(_gizmoRoot.transform, false);

            DestroyImmediate(tip.GetComponent<Collider>());

            float tipSize = arrowThickness * 3f;
            tip.transform.localPosition = direction * arrowLength;
            tip.transform.localScale = Vector3.one * tipSize;

            rend = tip.GetComponent<Renderer>();
            rend.material = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;

            return tip;
        }

        /// <summary>
        /// Builds the rotation ring gizmo using a LineRenderer.
        /// </summary>
        private void BuildRotationGizmo()
        {
            _rotationRing = new GameObject("RotationRing");
            _rotationRing.transform.SetParent(_gizmoRoot.transform, false);

            _rotationLineRenderer = _rotationRing.AddComponent<LineRenderer>();
            _rotationLineRenderer.useWorldSpace = false;
            _rotationLineRenderer.loop = true;
            _rotationLineRenderer.positionCount = ROTATION_RING_SEGMENTS;
            _rotationLineRenderer.startWidth = arrowThickness * 2f;
            _rotationLineRenderer.endWidth = arrowThickness * 2f;
            _rotationLineRenderer.material = _rotMat;
            _rotationLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _rotationLineRenderer.receiveShadows = false;

            // Generate ring points on XZ plane
            for (int i = 0; i < ROTATION_RING_SEGMENTS; i++)
            {
                float angle = (float)i / ROTATION_RING_SEGMENTS * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * rotationRingRadius;
                float z = Mathf.Sin(angle) * rotationRingRadius;
                _rotationLineRenderer.SetPosition(i, new Vector3(x, 0f, z));
            }
        }

        /// <summary>
        /// Builds the scale corner handles (8 small cubes at bounding box corners).
        /// </summary>
        private void BuildScaleGizmo()
        {
            // 8 corners of a unit cube
            Vector3[] corners = new Vector3[]
            {
                new Vector3(-1, -1, -1),
                new Vector3( 1, -1, -1),
                new Vector3(-1,  1, -1),
                new Vector3( 1,  1, -1),
                new Vector3(-1, -1,  1),
                new Vector3( 1, -1,  1),
                new Vector3(-1,  1,  1),
                new Vector3( 1,  1,  1),
            };

            _scaleHandles = new GameObject[corners.Length];
            _scaleHandleRenderers = new Renderer[corners.Length];

            for (int i = 0; i < corners.Length; i++)
            {
                GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                handle.name = $"ScaleHandle_{i}";
                handle.transform.SetParent(_gizmoRoot.transform, false);

                DestroyImmediate(handle.GetComponent<Collider>());

                float halfExtent = arrowLength * 0.6f;
                handle.transform.localPosition = corners[i] * halfExtent;
                handle.transform.localScale = Vector3.one * scaleHandleSize;

                var rend = handle.GetComponent<Renderer>();
                rend.material = _scaleMat;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;

                _scaleHandles[i] = handle;
                _scaleHandleRenderers[i] = rend;
            }
        }

        /// <summary>
        /// Rebuilds gizmo visibility based on the current mode.
        /// </summary>
        private void RebuildGizmo()
        {
            bool showTranslate = currentMode == GizmoMode.Translate;
            bool showRotate = currentMode == GizmoMode.Rotate;
            bool showScale = currentMode == GizmoMode.Scale;

            SetArrowsActive(showTranslate);

            if (_rotationRing != null)
                _rotationRing.SetActive(showRotate);

            if (_scaleHandles != null)
            {
                foreach (var h in _scaleHandles)
                {
                    if (h != null) h.SetActive(showScale);
                }
            }
        }

        /// <summary>
        /// Shows or hides the translation arrows.
        /// </summary>
        private void SetArrowsActive(bool active)
        {
            if (_xArrow != null) _xArrow.SetActive(active);
            if (_yArrow != null) _yArrow.SetActive(active);
            if (_zArrow != null) _zArrow.SetActive(active);
            if (_xTip != null) _xTip.SetActive(active);
            if (_yTip != null) _yTip.SetActive(active);
            if (_zTip != null) _zTip.SetActive(active);
        }

        /// <summary>
        /// Updates the entire gizmo root's visibility.
        /// </summary>
        private void UpdateVisibility()
        {
            if (_gizmoRoot != null)
                _gizmoRoot.SetActive(isVisible && targetTransform != null);
        }

        #endregion

        #region Hit Testing

        /// <summary>
        /// Tests whether a ray intersects with an axis arrow.
        /// </summary>
        private bool TestAxisHit(Ray ray, Vector3 axisDir, float radius)
        {
            if (targetTransform == null) return false;

            Vector3 origin = targetTransform.position;
            Vector3 axisEnd = origin + axisDir * arrowLength * GetCurrentGizmoScale();

            // Closest point on ray to axis line segment
            float dist = DistanceFromRayToSegment(ray, origin, axisEnd);
            return dist < radius * GetCurrentGizmoScale();
        }

        /// <summary>
        /// Tests whether a ray intersects with the rotation ring.
        /// </summary>
        private bool TestRotationRingHit(Ray ray)
        {
            if (targetTransform == null) return false;

            // Project ray onto XZ plane at gizmo center
            Vector3 center = targetTransform.position;
            Plane xzPlane = new Plane(Vector3.up, center);

            if (xzPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                float dist = Vector3.Distance(hitPoint, center);
                float scaledRadius = rotationRingRadius * GetCurrentGizmoScale();
                float tolerance = arrowThickness * 6f * GetCurrentGizmoScale();
                return Mathf.Abs(dist - scaledRadius) < tolerance;
            }
            return false;
        }

        /// <summary>
        /// Tests whether a ray intersects with any scale handle.
        /// </summary>
        private bool TestScaleHandleHit(Ray ray)
        {
            if (_scaleHandles == null) return false;

            float handleRadius = scaleHandleSize * 2f * GetCurrentGizmoScale();
            foreach (var handle in _scaleHandles)
            {
                if (handle == null || !handle.activeSelf) continue;

                Vector3 handleWorldPos = handle.transform.position;
                float dist = DistanceFromRayToPoint(ray, handleWorldPos);
                if (dist < handleRadius) return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the current gizmo scale factor based on distance.
        /// </summary>
        private float GetCurrentGizmoScale()
        {
            if (!scaleWithDistance || _camera == null || targetTransform == null)
                return 1f;

            float dist = Vector3.Distance(_camera.transform.position, targetTransform.position);
            return Mathf.Max(dist * distanceScaleFactor, 0.01f);
        }

        /// <summary>
        /// Calculates the minimum distance from a ray to a line segment.
        /// </summary>
        private float DistanceFromRayToSegment(Ray ray, Vector3 segStart, Vector3 segEnd)
        {
            Vector3 u = ray.direction;
            Vector3 v = segEnd - segStart;
            Vector3 w = ray.origin - segStart;

            float a = Vector3.Dot(u, u);
            float b = Vector3.Dot(u, v);
            float c = Vector3.Dot(v, v);
            float d = Vector3.Dot(u, w);
            float e = Vector3.Dot(v, w);

            float denom = a * c - b * b;
            float sN, sD = denom;
            float tN, tD = denom;

            if (denom < 1e-6f)
            {
                sN = 0f; sD = 1f;
                tN = e; tD = c;
            }
            else
            {
                sN = b * e - c * d;
                tN = a * e - b * d;

                if (sN < 0f) { sN = 0f; tN = e; tD = c; }
            }

            if (tN < 0f) { tN = 0f; if (-d < 0f) sN = 0f; else sN = -d / a; }
            else if (tN > tD) { tN = tD; if ((-d + b) < 0f) sN = 0f; else sN = (-d + b) / a; }

            float sc = Mathf.Abs(sN) < 1e-6f ? 0f : sN / sD;
            float tc = Mathf.Abs(tN) < 1e-6f ? 0f : tN / tD;

            Vector3 closest = w + sc * u - tc * v;
            return closest.magnitude;
        }

        /// <summary>
        /// Calculates the minimum distance from a ray to a point.
        /// </summary>
        private float DistanceFromRayToPoint(Ray ray, Vector3 point)
        {
            Vector3 toPoint = point - ray.origin;
            float proj = Vector3.Dot(toPoint, ray.direction);
            if (proj < 0) return toPoint.magnitude;

            Vector3 closestOnRay = ray.origin + ray.direction * proj;
            return Vector3.Distance(closestOnRay, point);
        }

        #endregion

        #region Axis Coloring

        /// <summary>
        /// Sets the color of a gizmo axis element to its base color or the highlight color.
        /// </summary>
        private void SetAxisColor(GizmoAxis axis, bool highlighted)
        {
            switch (axis)
            {
                case GizmoAxis.X:
                    ApplyMaterialColor(_xArrowRenderer, highlighted ? highlightColor : xAxisColor);
                    ApplyMaterialColor(_xTipRenderer, highlighted ? highlightColor : xAxisColor);
                    break;
                case GizmoAxis.Y:
                    ApplyMaterialColor(_yArrowRenderer, highlighted ? highlightColor : yAxisColor);
                    ApplyMaterialColor(_yTipRenderer, highlighted ? highlightColor : yAxisColor);
                    break;
                case GizmoAxis.Z:
                    ApplyMaterialColor(_zArrowRenderer, highlighted ? highlightColor : zAxisColor);
                    ApplyMaterialColor(_zTipRenderer, highlighted ? highlightColor : zAxisColor);
                    break;
                case GizmoAxis.Rotation:
                    if (_rotationLineRenderer != null)
                    {
                        Color col = highlighted ? highlightColor : rotationRingColor;
                        _rotationLineRenderer.startColor = col;
                        _rotationLineRenderer.endColor = col;
                    }
                    break;
                case GizmoAxis.Scale:
                    if (_scaleHandleRenderers != null)
                    {
                        Color col = highlighted ? highlightColor : scaleHandleColor;
                        foreach (var rend in _scaleHandleRenderers)
                            ApplyMaterialColor(rend, col);
                    }
                    break;
            }
        }

        /// <summary>
        /// Applies a color to a renderer's material.
        /// </summary>
        private void ApplyMaterialColor(Renderer rend, Color color)
        {
            if (rend == null) return;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            rend.GetPropertyBlock(block);
            block.SetColor(s_colorPropId, color);
            block.SetColor(s_baseColorPropId, color);
            rend.SetPropertyBlock(block);
        }

        /// <summary>
        /// Safely destroys a material instance.
        /// </summary>
        private void DestroyMaterial(Material mat)
        {
            if (mat != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(mat);
#else
                Destroy(mat);
#endif
            }
        }

        #endregion
    }
}
