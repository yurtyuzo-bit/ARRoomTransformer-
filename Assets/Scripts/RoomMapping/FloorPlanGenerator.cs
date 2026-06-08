using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace ARRoomTransformer
{
    /// <summary>
    /// Oda sınır köşelerinden zemin ve tavan mesh geometrisi oluşturur.
    /// Ear-clipping triangulation algoritması ile poligon mesh'i üretir.
    /// Zemin ve tavan için ayrı <see cref="MeshFilter"/>, <see cref="MeshRenderer"/>
    /// ve <see cref="MeshCollider"/> oluşturur.
    /// </summary>
    public class FloorPlanGenerator : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Malzemeler")]
        [SerializeField]
        [Tooltip("Zemine uygulanacak varsayılan malzeme.")]
        private Material _floorMaterial;

        [SerializeField]
        [Tooltip("Tavana uygulanacak varsayılan malzeme.")]
        private Material _ceilingMaterial;

        [Header("UV Ayarları")]
        [SerializeField]
        [Tooltip("Zemin UV ölçeği (dünya birimleri başına UV tekrarı).")]
        private Vector2 _floorUVScale = Vector2.one;

        [SerializeField]
        [Tooltip("Tavan UV ölçeği (dünya birimleri başına UV tekrarı).")]
        private Vector2 _ceilingUVScale = Vector2.one;

        [Header("Ayarlar")]
        [SerializeField]
        [Tooltip("Oluşturulan objelerin parent Transform'u.")]
        private Transform _parent;

        [SerializeField]
        [Tooltip("Mesh Collider eklensin mi?")]
        private bool _addMeshColliders = true;

        [SerializeField]
        [Tooltip("Tavan oluşturulsun mu?")]
        private bool _generateCeiling = true;

        [SerializeField]
        [Tooltip("Zemin oluşturulsun mu?")]
        private bool _generateFloor = true;

        [Header("Olaylar")]
        [SerializeField]
        [Tooltip("Zemin ve tavan oluşturulduğunda tetiklenir.")]
        private UnityEvent _onFloorPlanGenerated = new UnityEvent();

        [SerializeField]
        [Tooltip("Zemin ve tavan temizlendiğinde tetiklenir.")]
        private UnityEvent _onFloorPlanCleared = new UnityEvent();

        #endregion

        #region Private Fields

        private GameObject _floorObject;
        private GameObject _ceilingObject;
        private Transform _container;

        #endregion

        #region Public Events

        /// <summary>Fired when the floor plan (floor + ceiling) is generated.</summary>
        public event Action FloorPlanGenerated;

        /// <summary>Fired when the floor plan is cleared.</summary>
        public event Action FloorPlanCleared;

        #endregion

        #region Properties

        /// <summary>Gets the floor GameObject. May be null if not generated.</summary>
        public GameObject FloorObject => _floorObject;

        /// <summary>Gets the ceiling GameObject. May be null if not generated.</summary>
        public GameObject CeilingObject => _ceilingObject;

        /// <summary>Gets a value indicating whether the floor plan has been generated.</summary>
        public bool IsGenerated => _floorObject != null || _ceilingObject != null;

        /// <summary>Gets the UnityEvent fired on generation.</summary>
        public UnityEvent OnFloorPlanGenerated => _onFloorPlanGenerated;

        /// <summary>Gets the UnityEvent fired on clearing.</summary>
        public UnityEvent OnFloorPlanCleared => _onFloorPlanCleared;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_parent == null)
                _parent = transform;
        }

        private void OnDestroy()
        {
            ClearFloorPlan();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates floor and/or ceiling meshes from room boundary corners.
        /// </summary>
        /// <param name="corners">Ordered room boundary corners (world space).</param>
        /// <param name="ceilingHeight">
        /// Height of the ceiling relative to the floor corners' Y position (meters).
        /// </param>
        /// <param name="floorMaterial">Optional floor material override.</param>
        /// <param name="ceilingMaterial">Optional ceiling material override.</param>
        public void GenerateFloorPlan(
            List<Vector3> corners,
            float ceilingHeight = 2.7f,
            Material floorMaterial = null,
            Material ceilingMaterial = null)
        {
            if (corners == null || corners.Count < 3)
            {
                Debug.LogError("[FloorPlanGenerator] En az 3 köşe gerekli.");
                return;
            }

            // Mevcut plan'ı temizle
            ClearFloorPlan();

            // Konteyner oluştur
            _container = new GameObject("FloorPlanContainer").transform;
            _container.SetParent(_parent);
            _container.localPosition = Vector3.zero;
            _container.localRotation = Quaternion.identity;

            Material floorMat = floorMaterial ?? _floorMaterial;
            Material ceilingMat = ceilingMaterial ?? _ceilingMaterial;

            // Zemin oluştur
            if (_generateFloor)
            {
                _floorObject = CreateSurfaceMesh(
                    corners,
                    "Floor",
                    floorMat,
                    _floorUVScale,
                    faceUp: true);

                Debug.Log("[FloorPlanGenerator] Zemin mesh'i oluşturuldu.");
            }

            // Tavan oluştur
            if (_generateCeiling)
            {
                // Tavan köşeleri = zemin köşeleri + yükseklik
                var ceilingCorners = corners
                    .Select(c => c + Vector3.up * ceilingHeight)
                    .ToList();

                _ceilingObject = CreateSurfaceMesh(
                    ceilingCorners,
                    "Ceiling",
                    ceilingMat,
                    _ceilingUVScale,
                    faceUp: false); // Tavan aşağı bakar

                Debug.Log("[FloorPlanGenerator] Tavan mesh'i oluşturuldu.");
            }

            Debug.Log($"[FloorPlanGenerator] Zemin planı oluşturuldu. " +
                      $"Köşe sayısı: {corners.Count}, Tavan yüksekliği: {ceilingHeight:F2}m");

            FloorPlanGenerated?.Invoke();
            _onFloorPlanGenerated?.Invoke();
        }

        /// <summary>
        /// Generates floor and ceiling from <see cref="RoomBoundaryDetector.RoomBoundaryData"/>.
        /// </summary>
        /// <param name="boundaryData">The room boundary data.</param>
        /// <param name="floorMaterial">Optional floor material override.</param>
        /// <param name="ceilingMaterial">Optional ceiling material override.</param>
        public void GenerateFloorPlan(
            RoomBoundaryDetector.RoomBoundaryData boundaryData,
            Material floorMaterial = null,
            Material ceilingMaterial = null)
        {
            if (boundaryData == null)
            {
                Debug.LogError("[FloorPlanGenerator] RoomBoundaryData null.");
                return;
            }

            GenerateFloorPlan(
                boundaryData.Corners,
                boundaryData.Height,
                floorMaterial,
                ceilingMaterial);
        }

        /// <summary>
        /// Applies a new material to the floor mesh.
        /// </summary>
        /// <param name="material">Material to apply.</param>
        public void SetFloorMaterial(Material material)
        {
            if (_floorObject == null || material == null) return;

            var renderer = _floorObject.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
        }

        /// <summary>
        /// Applies a new material to the ceiling mesh.
        /// </summary>
        /// <param name="material">Material to apply.</param>
        public void SetCeilingMaterial(Material material)
        {
            if (_ceilingObject == null || material == null) return;

            var renderer = _ceilingObject.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
        }

        /// <summary>
        /// Destroys generated floor and ceiling GameObjects.
        /// </summary>
        public void ClearFloorPlan()
        {
            if (_floorObject != null)
            {
                Destroy(_floorObject);
                _floorObject = null;
            }

            if (_ceilingObject != null)
            {
                Destroy(_ceilingObject);
                _ceilingObject = null;
            }

            if (_container != null)
            {
                Destroy(_container.gameObject);
                _container = null;
            }

            FloorPlanCleared?.Invoke();
            _onFloorPlanCleared?.Invoke();
        }

        /// <summary>
        /// Sets the visibility of floor and ceiling.
        /// </summary>
        /// <param name="floorVisible">Whether the floor is visible.</param>
        /// <param name="ceilingVisible">Whether the ceiling is visible.</param>
        public void SetVisibility(bool floorVisible, bool ceilingVisible)
        {
            if (_floorObject != null)
                _floorObject.SetActive(floorVisible);

            if (_ceilingObject != null)
                _ceilingObject.SetActive(ceilingVisible);
        }

        #endregion

        #region Private Methods — Mesh Generation

        /// <summary>
        /// Creates a triangulated surface mesh from polygon corners.
        /// </summary>
        /// <param name="corners">The polygon vertices.</param>
        /// <param name="meshName">Name for the GameObject and mesh.</param>
        /// <param name="material">Material to apply.</param>
        /// <param name="uvScale">UV tiling scale.</param>
        /// <param name="faceUp">
        /// If true, normals face up (floor). If false, normals face down (ceiling).
        /// </param>
        /// <returns>The created surface GameObject.</returns>
        private GameObject CreateSurfaceMesh(
            List<Vector3> corners,
            string meshName,
            Material material,
            Vector2 uvScale,
            bool faceUp)
        {
            var surfaceObj = new GameObject(meshName);
            surfaceObj.transform.SetParent(_container);
            surfaceObj.transform.position = Vector3.zero;
            surfaceObj.transform.rotation = Quaternion.identity;

            // Triangulate
            var triangles = TriangulatePolygon(corners, faceUp);

            if (triangles == null || triangles.Length == 0)
            {
                Debug.LogError($"[FloorPlanGenerator] '{meshName}' üçgenleme başarısız.");
                Destroy(surfaceObj);
                return null;
            }

            // Mesh oluştur
            var mesh = new Mesh();
            mesh.name = $"{meshName}Mesh";

            mesh.vertices = corners.ToArray();
            mesh.triangles = triangles;

            // UV hesaplama — dünya koordinatlarından
            var uvs = CalculateWorldSpaceUVs(corners, uvScale);
            mesh.uv = uvs;

            // Normal
            Vector3 normal = faceUp ? Vector3.up : Vector3.down;
            var normals = new Vector3[corners.Count];
            for (int i = 0; i < normals.Length; i++)
                normals[i] = normal;

            mesh.normals = normals;

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            // Bileşenleri ekle
            var meshFilter = surfaceObj.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            var meshRenderer = surfaceObj.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

            if (_addMeshColliders)
            {
                var meshCollider = surfaceObj.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
            }

            return surfaceObj;
        }

        /// <summary>
        /// Calculates UVs based on world XZ coordinates.
        /// </summary>
        private Vector2[] CalculateWorldSpaceUVs(List<Vector3> vertices, Vector2 uvScale)
        {
            var uvs = new Vector2[vertices.Count];

            // Minimum XZ bul (UV orijini)
            float minX = vertices.Min(v => v.x);
            float minZ = vertices.Min(v => v.z);

            for (int i = 0; i < vertices.Count; i++)
            {
                uvs[i] = new Vector2(
                    (vertices[i].x - minX) * uvScale.x,
                    (vertices[i].z - minZ) * uvScale.y
                );
            }

            return uvs;
        }

        #endregion

        #region Triangulation — Ear Clipping

        /// <summary>
        /// Triangulates a simple polygon using the ear-clipping algorithm.
        /// Operates on the XZ plane.
        /// </summary>
        /// <param name="vertices">The polygon vertices (ordered).</param>
        /// <param name="faceUp">
        /// If true, triangle winding is counter-clockwise (face up).
        /// If false, clockwise (face down).
        /// </param>
        /// <returns>Triangle indices array, or null on failure.</returns>
        private int[] TriangulatePolygon(List<Vector3> vertices, bool faceUp)
        {
            int n = vertices.Count;
            if (n < 3) return null;

            // İndeks listesi oluştur
            var indices = new List<int>();
            for (int i = 0; i < n; i++)
                indices.Add(i);

            var triangles = new List<int>();
            int safetyCounter = n * n; // Sonsuz döngü koruması

            while (indices.Count > 2 && safetyCounter-- > 0)
            {
                bool earFound = false;

                for (int i = 0; i < indices.Count; i++)
                {
                    int prevIdx = indices[(i - 1 + indices.Count) % indices.Count];
                    int currIdx = indices[i];
                    int nextIdx = indices[(i + 1) % indices.Count];

                    Vector3 prev = vertices[prevIdx];
                    Vector3 curr = vertices[currIdx];
                    Vector3 next = vertices[nextIdx];

                    // Bu köşe konveks mi?
                    float cross = CrossProduct2D(prev, curr, next);

                    // faceUp ise CCW sıralama beklenir → cross > 0 konveks
                    bool isConvex = faceUp ? cross >= 0 : cross <= 0;

                    if (!isConvex) continue;

                    // Bu üçgenin içinde başka nokta var mı?
                    bool containsPoint = false;

                    for (int j = 0; j < indices.Count; j++)
                    {
                        int testIdx = indices[j];
                        if (testIdx == prevIdx || testIdx == currIdx || testIdx == nextIdx)
                            continue;

                        if (IsPointInTriangle(vertices[testIdx], prev, curr, next))
                        {
                            containsPoint = true;
                            break;
                        }
                    }

                    if (containsPoint) continue;

                    // Ear bulundu — üçgen ekle
                    if (faceUp)
                    {
                        triangles.Add(prevIdx);
                        triangles.Add(currIdx);
                        triangles.Add(nextIdx);
                    }
                    else
                    {
                        triangles.Add(nextIdx);
                        triangles.Add(currIdx);
                        triangles.Add(prevIdx);
                    }

                    indices.RemoveAt(i);
                    earFound = true;
                    break;
                }

                if (!earFound)
                {
                    Debug.LogWarning("[FloorPlanGenerator] Ear bulunamadı, üçgenleme tamamlanamayabilir.");
                    break;
                }
            }

            return triangles.Count >= 3 ? triangles.ToArray() : null;
        }

        /// <summary>
        /// Calculates the 2D cross product on the XZ plane.
        /// </summary>
        private float CrossProduct2D(Vector3 a, Vector3 b, Vector3 c)
        {
            return (b.x - a.x) * (c.z - b.z) - (b.z - a.z) * (c.x - b.x);
        }

        /// <summary>
        /// Determines whether a point lies inside a triangle (XZ plane).
        /// Uses the sign-of-cross-product method.
        /// </summary>
        private bool IsPointInTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
        {
            float d1 = Sign(point, a, b);
            float d2 = Sign(point, b, c);
            float d3 = Sign(point, c, a);

            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNeg && hasPos);
        }

        /// <summary>
        /// Returns the sign of the cross product (p1→p2) × (p1→p3) on XZ.
        /// </summary>
        private float Sign(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return (p1.x - p3.x) * (p2.z - p3.z) - (p2.x - p3.x) * (p1.z - p3.z);
        }

        #endregion
    }
}
