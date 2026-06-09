using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem.EnhancedTouch;

namespace ARRoomTransformer
{
    /// <summary>
    /// Kullanıcının nişangah (Crosshair) ile 4 çivi çakarak sınırları belirlemesini sağlar.
    /// Çiviler arasına ip gerilir ve sonrasında dışarısı okyanusla kaplanır.
    /// </summary>
    public class RoomBoundaryManager : MonoBehaviour
    {
        public bool isSetupMode = false;
        private List<Vector3> cornerPoints = new List<Vector3>();
        private ARRaycastManager arRaycastManager;
        private GameObject boundaryMeshObject;
        private GameObject cornerMarkersParent;
        private LineRenderer stringRenderer;
        public Material boundaryMaterial;

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
            
            cornerMarkersParent = new GameObject("BoundaryMarkers");
            
            // Çiviler arasındaki İP (String) görseli için LineRenderer
            var lineObj = new GameObject("StringRenderer");
            lineObj.transform.SetParent(cornerMarkersParent.transform);
            stringRenderer = lineObj.AddComponent<LineRenderer>();
            stringRenderer.startWidth = 0.02f; // 2 cm kalınlığında ip
            stringRenderer.endWidth = 0.02f;
            stringRenderer.material = new Material(Shader.Find("Sprites/Default"));
            stringRenderer.startColor = Color.red; // Kırmızı ip
            stringRenderer.endColor = Color.yellow;
            stringRenderer.positionCount = 0;
            stringRenderer.numCapVertices = 5;
            stringRenderer.numCornerVertices = 5;

            Debug.Log("[RoomBoundaryManager] Çivi/İp Sınır belirleme modu başladı.");
        }

        private void Update()
        {
            // Sınır belirleme modundaysak ve 4 çivi dolmamışsa
            if (!isSetupMode || cornerPoints.Count >= 4 || stringRenderer == null) return;

            // Parmak yerine Ekranın Tam Merkezinden (İmleç) AR zeminine Raycast gönder
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            
            if (arRaycastManager != null && arRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
            {
                Vector3 currentHit = hits[0].pose.position;
                
                // --- CANLI İP ÖNİZLEMESİ ---
                // Eğer en az 1 çivi çakılmışsa, son çividen imlecin vurduğu yere kadar ip uzat.
                int pointCount = cornerPoints.Count;
                if (pointCount > 0)
                {
                    stringRenderer.positionCount = pointCount + 1;
                    for (int i = 0; i < pointCount; i++)
                    {
                        // İpi zeminin hafif üstünde tut (0.02f) ki zeminin içine batıp kaybolmasın
                        stringRenderer.SetPosition(i, cornerPoints[i] + Vector3.up * 0.02f); 
                    }
                    stringRenderer.SetPosition(pointCount, currentHit + Vector3.up * 0.02f);
                }
            }
        }

        private void OnFingerDown(Finger finger)
        {
            // Ekrana dokunulduğunda tetiklenir
            if (!isSetupMode || finger.index != 0 || cornerPoints.Count >= 4) return;
            
            // Dokunulan nokta neresi olursa olsun, çiviyi EKRANIN MERKEZİNDEKİ (İmleç) yere çakacağız.
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            
            if (arRaycastManager != null && arRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
            {
                Vector3 hitPoint = hits[0].pose.position;
                cornerPoints.Add(hitPoint);
                Debug.Log($"[RoomBoundaryManager] Çivi {cornerPoints.Count} çakıldı: {hitPoint}");

                // --- 3D ÇİVİ (NAIL) GÖRSELİ OLUŞTURMA ---
                GameObject nail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                nail.name = "Nail_" + cornerPoints.Count;
                
                // Zemine çakılmış gibi göstermek için pozisyonunu ayarlayalım
                nail.transform.position = hitPoint + new Vector3(0, 0.03f, 0); 
                nail.transform.localScale = new Vector3(0.015f, 0.05f, 0.015f); // İnce ve sivri görünüm
                nail.transform.SetParent(cornerMarkersParent.transform);
                
                // Metalik Gri Renk
                var mat = nail.GetComponent<MeshRenderer>().material;
                mat.color = new Color(0.6f, 0.6f, 0.6f); 

                // Eğer 4 çivi de çakıldıysa, alanı kapat
                if (cornerPoints.Count == 4)
                {
                    // İpi son çividen ilk çiviye bağla (Poligonu kapat)
                    stringRenderer.positionCount = 5; // 4 çivi + başa dönüş
                    for (int i = 0; i < 4; i++)
                    {
                        stringRenderer.SetPosition(i, cornerPoints[i] + Vector3.up * 0.02f);
                    }
                    stringRenderer.SetPosition(4, cornerPoints[0] + Vector3.up * 0.02f); // İpi başladığı yere bağla

                    // Sınır belirlendi, Okyanus Mesh'ini oluştur
                    GenerateBoundaryMesh();
                    isSetupMode = false;
                    
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
                vertices[i] = sortedPoints[i]; // İç (Çiviler)
                vertices[i + 4] = outerPoints[i]; // Dış (Sonsuzluk)
            }

            // 4. Mesh Üçgenlerini Bağla (Delik açılmış devasa poligon)
            int[] triangles = new int[]
            {
                4, 5, 1,
                4, 1, 0,
                5, 6, 2,
                5, 2, 1,
                6, 7, 3,
                6, 3, 2,
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

            // Özel materyal atanmamışsa dinamik siyah/okyanus rengi üret
            if (boundaryMaterial == null)
            {
                Shader shader = Shader.Find("Unlit/Color"); 
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
