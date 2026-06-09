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
            arRaycastManager = FindObjectOfType<ARRaycastManager>();
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
            var uiManager = FindObjectOfType<DynamicUIManager>();

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
            ProcessTap();
        }

        private void ProcessTap()
        {
            if (!isSetupMode || cornerPoints.Count >= 4) return;
            
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            
            if (TryGetHit(screenCenter, out Vector3 hitPoint))
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

                    GenerateBoundaryMesh();
                    isSetupMode = false;
                    
                    var appManager = FindObjectOfType<AppManager>();
                    if (appManager != null) appManager.SendMessage("OnBoundarySetupComplete", SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        private bool TryGetHit(Vector2 screenPos, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;

            // 1. AR Düzlemleri (Gerçek Telefon Testi)
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            if (arRaycastManager != null && arRaycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
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
#endif

            return false;
        }

        private void GenerateBoundaryMesh()
        {
            Vector3 centroid = Vector3.zero;
            foreach (var p in cornerPoints) centroid += p;
            centroid /= 4f;

            var sortedPoints = cornerPoints.OrderBy(p => Mathf.Atan2(p.z - centroid.z, p.x - centroid.x)).ToList();

            Vector3[] outerPoints = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 dir = (sortedPoints[i] - centroid).normalized;
                dir.y = 0; 
                outerPoints[i] = centroid + dir * 1000f; 
            }

            Vector3[] vertices = new Vector3[8];
            for (int i = 0; i < 4; i++)
            {
                vertices[i] = sortedPoints[i]; 
                vertices[i + 4] = outerPoints[i]; 
            }

            int[] triangles = new int[]
            {
                4, 5, 1,  4, 1, 0,  5, 6, 2,  5, 2, 1,
                6, 7, 3,  6, 3, 2,  7, 4, 0,  7, 0, 3
            };

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            boundaryMeshObject = new GameObject("OceanBoundaryMask");
            var mf = boundaryMeshObject.AddComponent<MeshFilter>();
            var mr = boundaryMeshObject.AddComponent<MeshRenderer>();
            mf.mesh = mesh;

            if (boundaryMaterial == null)
            {
                Shader shader = Shader.Find("Unlit/Color"); 
                if (shader == null) shader = Shader.Find("Standard");
                boundaryMaterial = new Material(shader);
                boundaryMaterial.color = new Color(0.01f, 0.05f, 0.15f, 1.0f); 
            }
            mr.material = boundaryMaterial;
            
            boundaryMeshObject.transform.position = new Vector3(0, 0.005f, 0);
            Debug.Log("[RoomBoundaryManager] Okyanus maskesi oluşturuldu!");
        }
    }
}
