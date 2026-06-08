using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ARRoomTransformer
{
    /// <summary>
    /// Oda sınır köşelerinden duvar mesh geometrisi oluşturur.
    /// Her ardışık köşe çifti arasında dikdörtgen bir duvar segmenti yaratır.
    /// Her duvar ayrı bir alt GameObject olarak oluşturulur ve
    /// <see cref="MeshFilter"/>, <see cref="MeshRenderer"/>, <see cref="MeshCollider"/> içerir.
    /// </summary>
    public class WallGenerator : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Duvar Ayarları")]
        [SerializeField]
        [Tooltip("Duvar yüksekliği (m).")]
        private float _wallHeight = 2.7f;

        [SerializeField]
        [Tooltip("Duvarlara uygulanacak varsayılan malzeme.")]
        private Material _defaultWallMaterial;

        [SerializeField]
        [Tooltip("Backrooms teması malzemesi (opsiyonel, null ise varsayılan kullanılır).")]
        private Material _backroomsWallMaterial;

        [SerializeField]
        [Tooltip("UV tekrarlama ölçeği. (1,1) = tek karo, (2,2) = duvar başına 2×2 karo.")]
        private Vector2 _uvScale = Vector2.one;

        [SerializeField]
        [Tooltip("Duvarların oluşturulacağı parent Transform.")]
        private Transform _wallParent;

        [SerializeField]
        [Tooltip("Mesh Collider eklensin mi?")]
        private bool _addMeshColliders = true;

        [SerializeField]
        [Tooltip("Duvarlar çift taraflı mı olsun? (İç ve dış yüzey)")]
        private bool _doubleSided = true;

        [Header("Olaylar")]
        [SerializeField]
        [Tooltip("Duvarlar oluşturulduğunda tetiklenir. Parametre: duvar sayısı.")]
        private UnityEvent<int> _onWallsGenerated = new UnityEvent<int>();

        [SerializeField]
        [Tooltip("Duvarlar temizlendiğinde tetiklenir.")]
        private UnityEvent _onWallsCleared = new UnityEvent();

        #endregion

        #region Private Fields

        private readonly List<GameObject> _wallObjects = new List<GameObject>();
        private Transform _wallContainer;

        #endregion

        #region Public Events

        /// <summary>Fired when walls are generated. Parameter: wall count.</summary>
        public event Action<int> WallsGenerated;

        /// <summary>Fired when walls are cleared.</summary>
        public event Action WallsCleared;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the list of generated wall GameObjects.
        /// </summary>
        public IReadOnlyList<GameObject> WallObjects => _wallObjects.AsReadOnly();

        /// <summary>
        /// Gets the number of generated wall segments.
        /// </summary>
        public int WallCount => _wallObjects.Count;

        /// <summary>
        /// Gets or sets the wall height in meters.
        /// </summary>
        public float WallHeight
        {
            get => _wallHeight;
            set => _wallHeight = Mathf.Max(0.1f, value);
        }

        /// <summary>Gets the UnityEvent fired on wall generation.</summary>
        public UnityEvent<int> OnWallsGenerated => _onWallsGenerated;

        /// <summary>Gets the UnityEvent fired when walls are cleared.</summary>
        public UnityEvent OnWallsCleared => _onWallsCleared;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_wallParent == null)
                _wallParent = transform;
        }

        private void OnDestroy()
        {
            ClearWalls();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates wall segments between consecutive corner points.
        /// Previous walls are cleared before generating new ones.
        /// </summary>
        /// <param name="corners">Ordered room boundary corners (world space).</param>
        /// <param name="height">Wall height in meters. If 0 or negative, uses the configured default.</param>
        /// <param name="material">Optional material override. If null, uses the configured default.</param>
        /// <returns>The list of generated wall GameObjects.</returns>
        public List<GameObject> GenerateWalls(List<Vector3> corners, float height = 0f, Material material = null)
        {
            if (corners == null || corners.Count < 2)
            {
                Debug.LogError("[WallGenerator] En az 2 köşe gerekli.");
                return null;
            }

            // Mevcut duvarları temizle
            ClearWalls();

            float wallHeight = height > 0f ? height : _wallHeight;
            Material wallMaterial = material ?? _defaultWallMaterial;

            // Duvar konteynerı oluştur
            _wallContainer = new GameObject("WallContainer").transform;
            _wallContainer.SetParent(_wallParent);
            _wallContainer.localPosition = Vector3.zero;
            _wallContainer.localRotation = Quaternion.identity;

            // Her ardışık köşe çifti için duvar oluştur
            for (int i = 0; i < corners.Count; i++)
            {
                int nextIndex = (i + 1) % corners.Count;
                Vector3 start = corners[i];
                Vector3 end = corners[nextIndex];

                var wallObj = CreateWallSegment(start, end, wallHeight, wallMaterial, i);
                _wallObjects.Add(wallObj);
            }

            Debug.Log($"[WallGenerator] {_wallObjects.Count} duvar segmenti oluşturuldu. Yükseklik: {wallHeight:F2}m");

            WallsGenerated?.Invoke(_wallObjects.Count);
            _onWallsGenerated?.Invoke(_wallObjects.Count);

            return new List<GameObject>(_wallObjects);
        }

        /// <summary>
        /// Generates walls from <see cref="RoomBoundaryDetector.RoomBoundaryData"/>.
        /// </summary>
        /// <param name="boundaryData">The calculated room boundary data.</param>
        /// <param name="material">Optional material override.</param>
        /// <returns>The list of generated wall GameObjects.</returns>
        public List<GameObject> GenerateWalls(RoomBoundaryDetector.RoomBoundaryData boundaryData, Material material = null)
        {
            if (boundaryData == null)
            {
                Debug.LogError("[WallGenerator] RoomBoundaryData null.");
                return null;
            }

            return GenerateWalls(boundaryData.Corners, boundaryData.Height, material);
        }

        /// <summary>
        /// Applies the Backrooms theme material to all walls.
        /// </summary>
        public void ApplyBackroomsMaterial()
        {
            if (_backroomsWallMaterial == null)
            {
                Debug.LogWarning("[WallGenerator] Backrooms malzemesi atanmamış.");
                return;
            }

            ApplyMaterialToAllWalls(_backroomsWallMaterial);
        }

        /// <summary>
        /// Applies the specified material to all generated wall segments.
        /// </summary>
        /// <param name="material">The material to apply.</param>
        public void ApplyMaterialToAllWalls(Material material)
        {
            if (material == null)
            {
                Debug.LogWarning("[WallGenerator] Malzeme null, işlem iptal.");
                return;
            }

            foreach (var wallObj in _wallObjects)
            {
                if (wallObj == null) continue;

                var renderer = wallObj.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.sharedMaterial = material;
            }

            Debug.Log($"[WallGenerator] {_wallObjects.Count} duvara yeni malzeme uygulandı.");
        }

        /// <summary>
        /// Applies a material to a specific wall segment by index.
        /// </summary>
        /// <param name="wallIndex">Index of the wall segment.</param>
        /// <param name="material">The material to apply.</param>
        public void ApplyMaterialToWall(int wallIndex, Material material)
        {
            if (wallIndex < 0 || wallIndex >= _wallObjects.Count)
            {
                Debug.LogError($"[WallGenerator] Geçersiz duvar indeksi: {wallIndex}");
                return;
            }

            var renderer = _wallObjects[wallIndex]?.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
        }

        /// <summary>
        /// Destroys all generated wall GameObjects and clears the list.
        /// </summary>
        public void ClearWalls()
        {
            foreach (var wallObj in _wallObjects)
            {
                if (wallObj != null)
                    Destroy(wallObj);
            }

            _wallObjects.Clear();

            if (_wallContainer != null)
            {
                Destroy(_wallContainer.gameObject);
                _wallContainer = null;
            }

            WallsCleared?.Invoke();
            _onWallsCleared?.Invoke();
        }

        /// <summary>
        /// Sets the visibility of all walls.
        /// </summary>
        /// <param name="visible">Whether walls should be visible.</param>
        public void SetWallsVisible(bool visible)
        {
            foreach (var wallObj in _wallObjects)
            {
                if (wallObj != null)
                    wallObj.SetActive(visible);
            }
        }

        #endregion

        #region Private Methods — Mesh Generation

        /// <summary>
        /// Creates a single wall segment mesh between two corner points.
        /// </summary>
        /// <param name="start">Start corner position (world space).</param>
        /// <param name="end">End corner position (world space).</param>
        /// <param name="height">Wall height in meters.</param>
        /// <param name="material">Material to apply.</param>
        /// <param name="index">Wall segment index (for naming).</param>
        /// <returns>The wall segment GameObject.</returns>
        private GameObject CreateWallSegment(Vector3 start, Vector3 end, float height, Material material, int index)
        {
            var wallObj = new GameObject($"Wall_{index}");
            wallObj.transform.SetParent(_wallContainer);
            wallObj.transform.position = Vector3.zero;
            wallObj.transform.rotation = Quaternion.identity;

            // Mesh oluştur
            Mesh mesh = _doubleSided
                ? GenerateDoubleSidedQuadMesh(start, end, height)
                : GenerateSingleSidedQuadMesh(start, end, height);

            mesh.name = $"WallMesh_{index}";

            // MeshFilter
            var meshFilter = wallObj.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            // MeshRenderer
            var meshRenderer = wallObj.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

            // MeshCollider
            if (_addMeshColliders)
            {
                var meshCollider = wallObj.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
            }

            return wallObj;
        }

        /// <summary>
        /// Generates a single-sided quad mesh for a wall segment (faces inward).
        /// </summary>
        private Mesh GenerateSingleSidedQuadMesh(Vector3 start, Vector3 end, float height)
        {
            var mesh = new Mesh();

            // Dört köşe: alttaki iki, üstteki iki
            // Saat yönünde sıralama (iç yüz — sağ el kuralı)
            Vector3 bottomLeft = start;
            Vector3 bottomRight = end;
            Vector3 topLeft = start + Vector3.up * height;
            Vector3 topRight = end + Vector3.up * height;

            mesh.vertices = new Vector3[]
            {
                bottomLeft,  // 0
                topLeft,     // 1
                topRight,    // 2
                bottomRight  // 3
            };

            mesh.triangles = new int[]
            {
                0, 1, 2,
                0, 2, 3
            };

            // UV hesaplama
            float wallWidth = Vector3.Distance(start, end);
            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(0, height * _uvScale.y),
                new Vector2(wallWidth * _uvScale.x, height * _uvScale.y),
                new Vector2(wallWidth * _uvScale.x, 0)
            };

            // Normal — iç yüze bakacak şekilde
            Vector3 wallDir = (end - start).normalized;
            Vector3 normal = Vector3.Cross(Vector3.up, wallDir).normalized;

            mesh.normals = new Vector3[] { normal, normal, normal, normal };

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }

        /// <summary>
        /// Generates a double-sided quad mesh for a wall segment.
        /// Front face (inward) + back face (outward).
        /// </summary>
        private Mesh GenerateDoubleSidedQuadMesh(Vector3 start, Vector3 end, float height)
        {
            var mesh = new Mesh();

            Vector3 bottomLeft = start;
            Vector3 bottomRight = end;
            Vector3 topLeft = start + Vector3.up * height;
            Vector3 topRight = end + Vector3.up * height;

            float wallWidth = Vector3.Distance(start, end);
            Vector3 wallDir = (end - start).normalized;
            Vector3 inwardNormal = Vector3.Cross(Vector3.up, wallDir).normalized;
            Vector3 outwardNormal = -inwardNormal;

            // 8 vertices: 4 iç yüz + 4 dış yüz
            mesh.vertices = new Vector3[]
            {
                // İç yüz (0-3)
                bottomLeft, topLeft, topRight, bottomRight,
                // Dış yüz (4-7)
                bottomRight, topRight, topLeft, bottomLeft
            };

            mesh.triangles = new int[]
            {
                // İç yüz
                0, 1, 2,
                0, 2, 3,
                // Dış yüz
                4, 5, 6,
                4, 6, 7
            };

            mesh.uv = new Vector2[]
            {
                // İç yüz
                new Vector2(0, 0),
                new Vector2(0, height * _uvScale.y),
                new Vector2(wallWidth * _uvScale.x, height * _uvScale.y),
                new Vector2(wallWidth * _uvScale.x, 0),
                // Dış yüz
                new Vector2(0, 0),
                new Vector2(0, height * _uvScale.y),
                new Vector2(wallWidth * _uvScale.x, height * _uvScale.y),
                new Vector2(wallWidth * _uvScale.x, 0)
            };

            mesh.normals = new Vector3[]
            {
                inwardNormal, inwardNormal, inwardNormal, inwardNormal,
                outwardNormal, outwardNormal, outwardNormal, outwardNormal
            };

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }

        #endregion
    }
}
