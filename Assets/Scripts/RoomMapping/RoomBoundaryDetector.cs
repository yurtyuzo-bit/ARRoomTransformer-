using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace ARRoomTransformer
{
    /// <summary>
    /// Kullanıcının yerleştirdiği köşe noktalarından oda sınır poligonunu hesaplar.
    /// Minimum 4 köşe gerektirir. Poligonu doğrular, boyutları hesaplar
    /// ve oda anahatını oluşturur.
    /// </summary>
    public class RoomBoundaryDetector : MonoBehaviour
    {
        #region Nested Types

        /// <summary>
        /// Holds validated room boundary data.
        /// </summary>
        [Serializable]
        public class RoomBoundaryData
        {
            /// <summary>Ordered list of corner positions (world space, Y=floor).</summary>
            public List<Vector3> Corners;

            /// <summary>Whether the polygon is convex.</summary>
            public bool IsConvex;

            /// <summary>Area of the floor polygon in square meters.</summary>
            public float Area;

            /// <summary>Perimeter of the floor polygon in meters.</summary>
            public float Perimeter;

            /// <summary>Room width (X-axis extent) in meters.</summary>
            public float Width;

            /// <summary>Room length (Z-axis extent) in meters.</summary>
            public float Length;

            /// <summary>Room height (Y-axis) in meters.</summary>
            public float Height;

            /// <summary>Center point of the boundary polygon.</summary>
            public Vector3 Center;
        }

        /// <summary>
        /// Result of a boundary validation check.
        /// </summary>
        [Serializable]
        public struct ValidationResult
        {
            /// <summary>Whether the validation passed.</summary>
            public bool IsValid;

            /// <summary>Description of the validation result.</summary>
            public string Message;

            /// <summary>
            /// Creates a successful validation result.
            /// </summary>
            public static ValidationResult Success(string message = "Geçerli") =>
                new ValidationResult { IsValid = true, Message = message };

            /// <summary>
            /// Creates a failed validation result.
            /// </summary>
            public static ValidationResult Failure(string message) =>
                new ValidationResult { IsValid = false, Message = message };
        }

        #endregion

        #region Serialized Fields

        [Header("Doğrulama Ayarları")]
        [SerializeField]
        [Tooltip("Gerekli minimum köşe sayısı.")]
        private int _minimumCornerCount = 4;

        [SerializeField]
        [Tooltip("Geçerli sayılması için gereken minimum alan (m²).")]
        private float _minimumArea = 1.0f;

        [SerializeField]
        [Tooltip("Geçerli sayılması için gereken maksimum alan (m²).")]
        private float _maximumArea = 500.0f;

        [SerializeField]
        [Tooltip("İki köşe arası minimum mesafe (m).")]
        private float _minimumEdgeLength = 0.3f;

        [SerializeField]
        [Tooltip("Varsayılan oda yüksekliği (m). Tavan algılanamazsa kullanılır.")]
        private float _defaultRoomHeight = 2.7f;

        [Header("Olaylar")]
        [SerializeField]
        [Tooltip("Sınır başarıyla oluşturulduğunda tetiklenir.")]
        private UnityEvent<RoomBoundaryData> _onBoundaryCalculated = new UnityEvent<RoomBoundaryData>();

        [SerializeField]
        [Tooltip("Doğrulama hatası olduğunda tetiklenir.")]
        private UnityEvent<string> _onValidationFailed = new UnityEvent<string>();

        #endregion

        #region Private Fields

        private RoomBoundaryData _currentBoundary;

        #endregion

        #region Public Events

        /// <summary>Fired when a boundary is successfully calculated.</summary>
        public event Action<RoomBoundaryData> BoundaryCalculated;

        /// <summary>Fired when validation fails.</summary>
        public event Action<string> ValidationFailed;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the most recently calculated room boundary data. May be null.
        /// </summary>
        public RoomBoundaryData CurrentBoundary => _currentBoundary;

        /// <summary>
        /// Gets a value indicating whether a valid boundary has been calculated.
        /// </summary>
        public bool HasValidBoundary => _currentBoundary != null;

        /// <summary>Gets the UnityEvent fired on boundary calculation.</summary>
        public UnityEvent<RoomBoundaryData> OnBoundaryCalculated => _onBoundaryCalculated;

        /// <summary>Gets the UnityEvent fired on validation failure.</summary>
        public UnityEvent<string> OnValidationFailed => _onValidationFailed;

        #endregion

        #region Public Methods

        /// <summary>
        /// Calculates the room boundary from the given corner positions.
        /// Corners should be in world-space with consistent Y (floor level).
        /// </summary>
        /// <param name="corners">Ordered list of corner positions.</param>
        /// <param name="roomHeight">
        /// Room height in meters. Use 0 or negative to apply default.
        /// </param>
        /// <returns>The calculated <see cref="RoomBoundaryData"/> if valid; otherwise null.</returns>
        public RoomBoundaryData CalculateBoundary(List<Vector3> corners, float roomHeight = 0f)
        {
            if (corners == null)
            {
                RaiseValidationFailed("Köşe listesi null.");
                return null;
            }

            // Köşeleri zemin düzlemine yansıt (Y'yi ortalama al)
            var flatCorners = FlattenCornersToFloor(corners);

            // Doğrulama
            var validation = ValidateCorners(flatCorners);
            if (!validation.IsValid)
            {
                RaiseValidationFailed(validation.Message);
                return null;
            }

            // Saat yönünde sıralama
            var orderedCorners = EnsureClockwiseOrder(flatCorners);

            // Hesaplamalar
            float area = CalculatePolygonArea(orderedCorners);

            if (area < _minimumArea)
            {
                RaiseValidationFailed($"Alan çok küçük: {area:F2}m² (min: {_minimumArea:F2}m²)");
                return null;
            }

            if (area > _maximumArea)
            {
                RaiseValidationFailed($"Alan çok büyük: {area:F2}m² (max: {_maximumArea:F2}m²)");
                return null;
            }

            float height = roomHeight > 0f ? roomHeight : _defaultRoomHeight;
            bool isConvex = IsPolygonConvex(orderedCorners);
            float perimeter = CalculatePerimeter(orderedCorners);
            var bounds = CalculateBounds(orderedCorners);
            var center = CalculateCenter(orderedCorners);

            _currentBoundary = new RoomBoundaryData
            {
                Corners = orderedCorners,
                IsConvex = isConvex,
                Area = area,
                Perimeter = perimeter,
                Width = bounds.x,
                Length = bounds.y,
                Height = height,
                Center = center
            };

            Debug.Log($"[RoomBoundaryDetector] Sınır hesaplandı: " +
                      $"{orderedCorners.Count} köşe, {area:F2}m², " +
                      $"{bounds.x:F2}m × {bounds.y:F2}m × {height:F2}m, " +
                      $"Konveks: {isConvex}");

            BoundaryCalculated?.Invoke(_currentBoundary);
            _onBoundaryCalculated?.Invoke(_currentBoundary);

            return _currentBoundary;
        }

        /// <summary>
        /// Validates whether the given corners form a valid room boundary
        /// without performing the full calculation.
        /// </summary>
        /// <param name="corners">The corners to validate.</param>
        /// <returns>A <see cref="ValidationResult"/>.</returns>
        public ValidationResult ValidateCorners(List<Vector3> corners)
        {
            if (corners == null || corners.Count < _minimumCornerCount)
            {
                return ValidationResult.Failure(
                    $"En az {_minimumCornerCount} köşe gerekli (mevcut: {corners?.Count ?? 0}).");
            }

            // Minimum kenar uzunluğu kontrolü
            for (int i = 0; i < corners.Count; i++)
            {
                int next = (i + 1) % corners.Count;
                float edgeLength = Vector3.Distance(corners[i], corners[next]);

                if (edgeLength < _minimumEdgeLength)
                {
                    return ValidationResult.Failure(
                        $"Köşe {i} ve {next} arası çok kısa: {edgeLength:F2}m (min: {_minimumEdgeLength:F2}m).");
                }
            }

            // Kenar kesişim kontrolü (self-intersecting polygon)
            if (DoEdgesIntersect(corners))
            {
                return ValidationResult.Failure("Poligon kenarları birbirini kesiyor (geçersiz şekil).");
            }

            return ValidationResult.Success("Tüm doğrulamalar geçti.");
        }

        /// <summary>
        /// Generates world-space outline points for rendering the room boundary.
        /// Returns a closed loop (last point = first point).
        /// </summary>
        /// <param name="yOffset">Y offset above the floor for the outline.</param>
        /// <returns>Array of outline points, or null if no boundary exists.</returns>
        public Vector3[] GenerateOutlinePoints(float yOffset = 0.01f)
        {
            if (_currentBoundary == null || _currentBoundary.Corners == null)
                return null;

            var corners = _currentBoundary.Corners;
            var points = new Vector3[corners.Count + 1];

            for (int i = 0; i < corners.Count; i++)
            {
                points[i] = corners[i] + Vector3.up * yOffset;
            }

            // Döngüyü kapat
            points[corners.Count] = points[0];

            return points;
        }

        /// <summary>
        /// Resets the detector, clearing the current boundary.
        /// </summary>
        public void ClearBoundary()
        {
            _currentBoundary = null;
            Debug.Log("[RoomBoundaryDetector] Sınır verisi temizlendi.");
        }

        #endregion

        #region Geometry Utilities

        /// <summary>
        /// Flattens corners to a common Y plane (average Y of all corners).
        /// </summary>
        private List<Vector3> FlattenCornersToFloor(List<Vector3> corners)
        {
            float avgY = corners.Average(c => c.y);
            return corners.Select(c => new Vector3(c.x, avgY, c.z)).ToList();
        }

        /// <summary>
        /// Ensures corners are ordered clockwise when viewed from above (Y-up).
        /// Uses the centroid and sorts by angle.
        /// </summary>
        private List<Vector3> EnsureClockwiseOrder(List<Vector3> corners)
        {
            var center = CalculateCenter(corners);
            var sorted = corners.OrderBy(c =>
            {
                float angle = Mathf.Atan2(c.z - center.z, c.x - center.x);
                return -angle; // Negatif: saat yönü
            }).ToList();

            return sorted;
        }

        /// <summary>
        /// Calculates the area of a polygon using the Shoelace formula (XZ plane).
        /// </summary>
        /// <param name="polygon">Ordered polygon vertices.</param>
        /// <returns>The absolute area in square meters.</returns>
        public static float CalculatePolygonArea(List<Vector3> polygon)
        {
            int n = polygon.Count;
            float area = 0f;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                area += polygon[i].x * polygon[j].z;
                area -= polygon[j].x * polygon[i].z;
            }

            return Mathf.Abs(area) * 0.5f;
        }

        /// <summary>
        /// Calculates the perimeter of a polygon.
        /// </summary>
        private float CalculatePerimeter(List<Vector3> polygon)
        {
            float perimeter = 0f;

            for (int i = 0; i < polygon.Count; i++)
            {
                int next = (i + 1) % polygon.Count;
                perimeter += Vector3.Distance(polygon[i], polygon[next]);
            }

            return perimeter;
        }

        /// <summary>
        /// Determines whether a polygon is convex.
        /// Uses cross product sign consistency on the XZ plane.
        /// </summary>
        /// <param name="polygon">Ordered polygon vertices.</param>
        /// <returns><c>true</c> if the polygon is convex; otherwise <c>false</c>.</returns>
        public static bool IsPolygonConvex(List<Vector3> polygon)
        {
            int n = polygon.Count;
            if (n < 3) return false;

            bool? sign = null;

            for (int i = 0; i < n; i++)
            {
                int i1 = (i + 1) % n;
                int i2 = (i + 2) % n;

                float cross = CrossProduct2D(
                    polygon[i], polygon[i1], polygon[i2]);

                if (Mathf.Abs(cross) < 1e-6f) continue; // Doğrusal noktaları atla

                bool currentSign = cross > 0;

                if (sign == null)
                    sign = currentSign;
                else if (sign.Value != currentSign)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Calculates the 2D cross product (XZ plane) of vectors AB and BC.
        /// </summary>
        private static float CrossProduct2D(Vector3 a, Vector3 b, Vector3 c)
        {
            return (b.x - a.x) * (c.z - b.z) - (b.z - a.z) * (c.x - b.x);
        }

        /// <summary>
        /// Calculates the width (X) and length (Z) extents of the polygon.
        /// </summary>
        private Vector2 CalculateBounds(List<Vector3> polygon)
        {
            float minX = polygon.Min(p => p.x);
            float maxX = polygon.Max(p => p.x);
            float minZ = polygon.Min(p => p.z);
            float maxZ = polygon.Max(p => p.z);

            return new Vector2(maxX - minX, maxZ - minZ);
        }

        /// <summary>
        /// Calculates the centroid of a polygon.
        /// </summary>
        private Vector3 CalculateCenter(List<Vector3> polygon)
        {
            Vector3 sum = Vector3.zero;
            foreach (var p in polygon)
                sum += p;

            return sum / polygon.Count;
        }

        /// <summary>
        /// Checks if any non-adjacent edges of the polygon intersect each other.
        /// </summary>
        private bool DoEdgesIntersect(List<Vector3> polygon)
        {
            int n = polygon.Count;

            for (int i = 0; i < n; i++)
            {
                for (int j = i + 2; j < n; j++)
                {
                    // Bitişik kenarları atla
                    if (i == 0 && j == n - 1) continue;

                    if (SegmentsIntersect(
                            polygon[i], polygon[(i + 1) % n],
                            polygon[j], polygon[(j + 1) % n]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if two 2D line segments (projected on XZ) intersect.
        /// </summary>
        private bool SegmentsIntersect(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
        {
            float d1 = CrossProduct2D(b1, b2, a1);
            float d2 = CrossProduct2D(b1, b2, a2);
            float d3 = CrossProduct2D(a1, a2, b1);
            float d4 = CrossProduct2D(a1, a2, b2);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Raises the validation-failed events and logs a warning.
        /// </summary>
        private void RaiseValidationFailed(string message)
        {
            Debug.LogWarning($"[RoomBoundaryDetector] Doğrulama hatası: {message}");
            ValidationFailed?.Invoke(message);
            _onValidationFailed?.Invoke(message);
        }

        #endregion
    }
}
