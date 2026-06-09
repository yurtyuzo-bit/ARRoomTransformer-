using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem.EnhancedTouch;

namespace ARRoomTransformer
{
    /// <summary>
    /// Kullanıcının 4 köşe belirleyerek, dışarısını okyanusla/karanlıkla kaplamasını sağlar.
    /// Ters (Hole) Mesh algoritması kullanarak "Güvenli Alan" oluşturur.
    /// </summary>
    public class RoomBoundaryManager : MonoBehaviour
    {
        public bool isSetupMode = false;
        private List<Vector3> cornerPoints = new List<Vector3>();
        private ARRaycastManager arRaycastManager;
        private GameObject boundaryMeshObject;
        private GameObject cornerMarkersParent;
        public Material boundaryMaterial; // Opsiyonel, yoksa otomatik koyu mavi atanır

        private void Awake()
        {
            arRaycastManager = FindObjectOfType<ARRaycastManager>();
            if (arRaycastManager == null)
                Debug.LogError("[RoomBoundaryManager] ARRaycastManager bulunamadı!");
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown += OnFingerDown;
        }

        private void OnDisable()
        {
            UnityEngine.InputSystem.EnhancedTouch.Touch.onFingerDown -= OnFingerDown;
            EnhancedTouchSupport.Disable();
        }

        public void StartSetup()
        {
            isSetupMode = true;
            cornerPoints.Clear();
            
            if (boundaryMeshObject != null) Destroy(boundaryMeshObject);
            if (cornerMarkersParent != null) Destroy(cornerMarkersParent);
            
            cornerMarkersParent = new GameObject("CornerMarkers");
            Debug.Log("[RoomBoundaryManager] Sınır belirleme modu başladı. Zemine 4 köşe dokunun.");
        }

        private void OnFingerDown(Finger finger)
        {
            if (!isSetupMode || finger.index != 0 || cornerPoints.Count >= 4) return;
            
            // UI'a tıklanıp tıklanmadığını kontrol etmek iyi olur, ancak şu an Canvas tam ekran değil.

            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            if (arRaycastManager != null && arRaycastManager.Raycast(finger.currentTouch.screenPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                Vector3 hitPoint = hits[0].pose.position;
                cornerPoints.Add(hitPoint);
                Debug.Log($"[RoomBoundaryManager] Köşe {cornerPoints.Count} eklendi: {hitPoint}");

                // Geçici nokta görseli (Küçük bir küre)
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = hitPoint;
                sphere.transform.localScale = Vector3.one * 0.05f;
                sphere.transform.SetParent(cornerMarkersParent.transform);
                var mat = sphere.GetComponent<MeshRenderer>().material;
                mat.color = Color.green;

                if (cornerPoints.Count == 4)
                {
                    GenerateBoundaryMesh();
                    isSetupMode = false;
                    
                    // Nokta işaretçilerini gizle/sil
                    Destroy(cornerMarkersParent);
                    
                    // AppManager'a işlemin bittiğini bildir
                    var appManager = FindObjectOfType<AppManager>();
                    if (appManager != null) appManager.SendMessage("OnBoundarySetupComplete", SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        private void GenerateBoundaryMesh()
        {
            // 1. Merkez Noktasını (Centroid) Bul
            Vector3 centroid = Vector3.zero;
            foreach (var p in cornerPoints) centroid += p;
            centroid /= 4f;

            // 2. Noktaları Saat Yönünde Sırala (Açıya göre)
            var sortedPoints = cornerPoints.OrderBy(p => Mathf.Atan2(p.z - centroid.z, p.x - centroid.x)).ToList();

            // 3. Dış Noktaları Hesapla (Devasa okyanusun sınırları)
            Vector3[] outerPoints = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 dir = (sortedPoints[i] - centroid).normalized;
                dir.y = 0; // Zeminle düz olması için
                outerPoints[i] = centroid + dir * 1000f; // 1000 metre dışarıya doğru
            }

            // Vertices array: 0-3 İç Kısım, 4-7 Dış Kısım
            Vector3[] vertices = new Vector3[8];
            for (int i = 0; i < 4; i++)
            {
                vertices[i] = sortedPoints[i]; // İç
                vertices[i + 4] = outerPoints[i]; // Dış
            }

            // 4. Mesh Üçgenlerini Bağla (Delik açılmış devasa poligon)
            int[] triangles = new int[]
            {
                // Segment 0
                4, 5, 1,
                4, 1, 0,
                // Segment 1
                5, 6, 2,
                5, 2, 1,
                // Segment 2
                6, 7, 3,
                6, 3, 2,
                // Segment 3
                7, 4, 0,
                7, 0, 3
            };

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            boundaryMeshObject = new GameObject("OceanBoundaryMask");
            var mf = boundaryMeshObject.AddComponent<MeshFilter>();
            var mr = boundaryMeshObject.AddComponent<MeshRenderer>();
            mf.mesh = mesh;

            // Eğer özel materyal atanmamışsa dinamik siyah/okyanus rengi üret
            if (boundaryMaterial == null)
            {
                Shader shader = Shader.Find("Unlit/Color"); // Işıksız saf renk için
                if (shader == null) shader = Shader.Find("Standard");
                
                boundaryMaterial = new Material(shader);
                boundaryMaterial.color = new Color(0.01f, 0.05f, 0.15f, 1.0f); // Derin Okyanus Mavisi
            }
            mr.material = boundaryMaterial;
            
            // Z-fighting'i önlemek için kameradan/zeminden 0.5 cm yukarı kaldırıyoruz
            boundaryMeshObject.transform.position = new Vector3(0, 0.005f, 0);

            Debug.Log("[RoomBoundaryManager] Okyanus maskesi oluşturuldu!");
        }
    }
}
