using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace ARRoomTransformer
{
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
            arRaycastManager = FindAnyObjectByType<ARRaycastManager>();
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += OnFingerDown;
        }

        private void OnDisable()
        {
            Touch.onFingerDown -= OnFingerDown;
            EnhancedTouchSupport.Disable();
        }

        public void StartSetup()
        {
            isSetupMode = true;
            cornerPoints.Clear();

            var planeManager = FindAnyObjectByType<ARPlaneManager>();
            if (planeManager != null) planeManager.enabled = true;

#if UNITY_EDITOR
            // Bilgisayar testleri için çok KRİTİK: Kamerayı yerden insan boyuna kaldır!
            if (Camera.main != null && Camera.main.transform.position.y < 0.5f)
            {
                Camera.main.transform.position = new Vector3(0, 1.5f, -2f);
                Camera.main.transform.rotation = Quaternion.Euler(20f, 0, 0); // Yere doğru hafif eğik
            }
#endif
            
            if (boundaryMeshObject != null) Destroy(boundaryMeshObject);
            if (cornerMarkersParent != null) Destroy(cornerMarkersParent);
            
            cornerMarkersParent = new GameObject("BoundaryMarkers");
            
            var lineObj = new GameObject("StringRenderer");
            lineObj.transform.SetParent(cornerMarkersParent.transform);
            stringRenderer = lineObj.AddComponent<LineRenderer>();
            stringRenderer.startWidth = 0.02f;
            stringRenderer.endWidth = 0.02f;
            stringRenderer.material = new Material(Shader.Find("Sprites/Default"));
            stringRenderer.startColor = Color.red;
            stringRenderer.endColor = Color.yellow;
            stringRenderer.positionCount = 0;
            stringRenderer.numCapVertices = 5;
            stringRenderer.numCornerVertices = 5;

            Debug.Log("[RoomBoundaryManager] Çivi/İp Sınır belirleme modu başladı.");
        }

        private void Update()
        {
            if (!isSetupMode || cornerPoints.Count >= 4 || stringRenderer == null) return;

            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
#if UNITY_EDITOR
            if (Mouse.current != null) screenCenter = Mouse.current.position.ReadValue();
#endif
            var uiManager = FindAnyObjectByType<DynamicUIManager>();

            if (TryGetHit(screenCenter, out Vector3 currentHit))
            {
                if (uiManager != null) uiManager.SetCrosshairColor(Color.red); // Kırmızı (Hazır)
                
                int pointCount = cornerPoints.Count;
                if (pointCount > 0)
                {
                    stringRenderer.positionCount = pointCount + 1;
                    for (int i = 0; i < pointCount; i++)
                    {
                        stringRenderer.SetPosition(i, cornerPoints[i] + Vector3.up * 0.02f); 
                    }
                    stringRenderer.SetPosition(pointCount, currentHit + Vector3.up * 0.02f);
                }
            }
            else
            {
                // Bilgisayarda veya zemin bulunamadığında imleci SARI yapıp görünür tutalım
                if (uiManager != null) uiManager.SetCrosshairColor(Color.yellow); 
                if (stringRenderer != null) stringRenderer.positionCount = cornerPoints.Count; 
            }

            // --- EDITOR FARE (MOUSE) DESTEĞİ ---
#if UNITY_EDITOR
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                ProcessTap();
            }
#endif
        }

        private void OnFingerDown(Finger finger)
        {
            if (finger.index != 0) return;
            ProcessTap(finger.screenPosition);
        }

        private void ProcessTap(Vector2? tapPos = null)
        {
            if (!isSetupMode || cornerPoints.Count >= 4) return;
            
            Vector2 screenPos;
            if (tapPos.HasValue) 
            {
                screenPos = tapPos.Value;
            }
            else
            {
                screenPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
#if UNITY_EDITOR
                if (Mouse.current != null) screenPos = Mouse.current.position.ReadValue();
#endif
            }
            
            if (TryGetHit(screenPos, out Vector3 hitPoint))
            {
                cornerPoints.Add(hitPoint);
                Debug.Log($"[RoomBoundaryManager] Çivi {cornerPoints.Count} çakıldı: {hitPoint}");

                GameObject nail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                nail.name = "Nail_" + cornerPoints.Count;
                nail.transform.position = hitPoint + new Vector3(0, 0.03f, 0); 
                nail.transform.localScale = new Vector3(0.02f, 0.1f, 0.02f); // Çivi
                nail.transform.SetParent(cornerMarkersParent.transform);
                var mat = nail.GetComponent<MeshRenderer>().material;
                mat.color = new Color(0.8f, 0.8f, 0.8f); 

                if (cornerPoints.Count == 4)
                {
                    stringRenderer.positionCount = 5; 
                    for (int i = 0; i < 4; i++)
                    {
                        stringRenderer.SetPosition(i, cornerPoints[i] + Vector3.up * 0.02f);
                    }
                    stringRenderer.SetPosition(4, cornerPoints[0] + Vector3.up * 0.02f); 

                    var detector = FindAnyObjectByType<RoomBoundaryDetector>();
                    if (detector != null)
                    {
                        detector.CalculateBoundary(cornerPoints);
                    }

                    GenerateBoundaryMesh();
                    isSetupMode = false;
                    
                    var appManager = FindAnyObjectByType<AppManager>();
                    if (appManager != null) appManager.SendMessage("OnBoundarySetupComplete", SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        private bool TryGetHit(Vector2 screenPos, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;

            // 1. AR Düzlemleri (Gerçek Telefon Testi)
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            if (arRaycastManager != null && arRaycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon | TrackableType.PlaneEstimated))
            {
                hitPoint = hits[0].pose.position;
                return true;
            }

            Camera cam = Camera.main;
            if (cam == null) return false;

            // 2. Fiziksel Objeler (Editor içindeki 3D modeller)
            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit physHit, 50f))
            {
                hitPoint = physHit.point;
                return true;
            }

            // 3. Bilgisayar Testi İçin Sanal Zemin (Y=0 Düzlemi)
#if UNITY_EDITOR
            Plane floorPlane = new Plane(Vector3.up, Vector3.zero);
            if (floorPlane.Raycast(ray, out float enter))
            {
                hitPoint = ray.GetPoint(enter);
                return true;
            }
            
            // DÜZ BAKMA DURUMU: Eğer kamera ufka bakıyorsa ışın yeri kesmez.
            // Bu durumda zorla kameranın 3 metre önüne çiviyi çak.
            hitPoint = cam.transform.position + cam.transform.forward * 3f;
            hitPoint.y = 0; // Çiviyi yere sabitle
            return true;
#else
            return false;
#endif
        }

        private void GenerateBoundaryMesh()
        {
            Vector3 centroid = Vector3.zero;
            foreach (var p in cornerPoints) centroid += p;
            centroid /= 4f;

            var sortedPoints = cornerPoints.OrderBy(p => Mathf.Atan2(p.z - centroid.z, p.x - centroid.x)).ToList();

            boundaryMeshObject = new GameObject("OceanBoundaryMask");
            boundaryMeshObject.transform.position = new Vector3(0, 0.005f, 0);

            if (boundaryMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit"); 
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Standard");
                
                boundaryMaterial = new Material(shader);
                if (boundaryMaterial.HasProperty("_BaseColor"))
                    boundaryMaterial.SetColor("_BaseColor", new Color(0.0f, 0.8f, 1.0f, 0.6f)); // Açık Turkuaz / Su Rengi
                else
                    boundaryMaterial.color = new Color(0.0f, 0.8f, 1.0f, 0.6f); // Açık Turkuaz
            }

            // 1. ZEMİN (Dışarısı)
            Vector3[] outerPoints = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 dir = (sortedPoints[i] - centroid).normalized;
                dir.y = 0; 
                outerPoints[i] = centroid + dir * 1000f; 
            }
            Vector3[] floorVertices = new Vector3[8];
            for (int i = 0; i < 4; i++)
            {
                floorVertices[i] = sortedPoints[i]; 
                floorVertices[i + 4] = outerPoints[i]; 
            }
            int[] floorTriangles = new int[]
            {
                4, 5, 1,  4, 1, 0,  5, 6, 2,  5, 2, 1,
                6, 7, 3,  6, 3, 2,  7, 4, 0,  7, 0, 3
            };
            Mesh floorMesh = new Mesh { vertices = floorVertices, triangles = floorTriangles };
            floorMesh.RecalculateNormals();
            floorMesh.RecalculateBounds();

            var floorObj = new GameObject("OceanFloor");
            floorObj.transform.SetParent(boundaryMeshObject.transform, false);
            floorObj.AddComponent<MeshFilter>().mesh = floorMesh;
            floorObj.AddComponent<MeshRenderer>().material = boundaryMaterial;

            // 2. DUVARLAR (Çok uzaktaki ufuk çizgisinden gökyüzüne uzanan perdeler)
            float wallHeight = 500f;
            Vector3[] wallVertices = new Vector3[8];
            for (int i = 0; i < 4; i++)
            {
                wallVertices[i] = outerPoints[i]; // Alt kenar (Uzak Ufuk)
                wallVertices[i + 4] = outerPoints[i] + new Vector3(0, wallHeight, 0); // Üst kenar (Uzak Ufuk)
            }
            int[] wallTriangles = new int[]
            {
                0, 1, 5,  0, 5, 4,
                1, 2, 6,  1, 6, 5,
                2, 3, 7,  2, 7, 6,
                3, 0, 4,  3, 4, 7
            };
            // Çift taraflı görünmesi için ters üçgenleri de ekle
            int[] doubleWalls = new int[wallTriangles.Length * 2];
            for (int i = 0; i < wallTriangles.Length; i += 3)
            {
                doubleWalls[i] = wallTriangles[i];
                doubleWalls[i + 1] = wallTriangles[i + 1];
                doubleWalls[i + 2] = wallTriangles[i + 2];

                doubleWalls[wallTriangles.Length + i] = wallTriangles[i];
                doubleWalls[wallTriangles.Length + i + 1] = wallTriangles[i + 2];
                doubleWalls[wallTriangles.Length + i + 2] = wallTriangles[i + 1];
            }
            Mesh wallMesh = new Mesh { vertices = wallVertices, triangles = doubleWalls };
            wallMesh.RecalculateNormals();
            wallMesh.RecalculateBounds();

            var wallObj = new GameObject("OceanWalls");
            wallObj.transform.SetParent(boundaryMeshObject.transform, false);
            wallObj.AddComponent<MeshFilter>().mesh = wallMesh;
            wallObj.AddComponent<MeshRenderer>().material = boundaryMaterial;

            // 3. TAVAN (Kutuyu Kapatır)
            Vector3[] ceilingVertices = new Vector3[4];
            for (int i = 0; i < 4; i++) ceilingVertices[i] = outerPoints[i] + new Vector3(0, wallHeight, 0);
            int[] ceilingTriangles = new int[] { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 }; // Çift yönlü
            Mesh ceilingMesh = new Mesh { vertices = ceilingVertices, triangles = ceilingTriangles };
            ceilingMesh.RecalculateNormals();
            ceilingMesh.RecalculateBounds();

            var ceilingObj = new GameObject("OceanCeiling");
            ceilingObj.transform.SetParent(boundaryMeshObject.transform, false);
            ceilingObj.AddComponent<MeshFilter>().mesh = ceilingMesh;
            ceilingObj.AddComponent<MeshRenderer>().material = boundaryMaterial;

            Debug.Log("[RoomBoundaryManager] 3 Boyutlu Okyanus Maskesi oluşturuldu!");

            // Asıl duvar ve zeminleri oluştur
            var wallGen = FindAnyObjectByType<WallGenerator>();
            if (wallGen != null) 
            {
                wallGen.GenerateWalls(cornerPoints);
                var matManager = FindAnyObjectByType<MaterialManager>();
                if (matManager != null && matManager.CurrentTheme != null)
                {
                    wallGen.ApplyMaterialToAllWalls(matManager.CurrentTheme.wallMaterial);
                }
            }

            var floorGen = FindAnyObjectByType<FloorPlanGenerator>();
            if (floorGen != null) 
            {
                Material floorMat = null;
                Material ceilingMat = null;
                var matManager = FindAnyObjectByType<MaterialManager>();
                if (matManager != null && matManager.CurrentTheme != null)
                {
                    floorMat = matManager.CurrentTheme.floorMaterial;
                    ceilingMat = matManager.CurrentTheme.ceilingMaterial;
                }
                floorGen.GenerateFloorPlan(cornerPoints, 2.7f, floorMat, ceilingMat);
            }
        }
    }
}
