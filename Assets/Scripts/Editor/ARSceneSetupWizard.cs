using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;


namespace ARRoomTransformer.Editor
{
    /// <summary>
    /// Unity Editor menüsüne ARRoomTransformer sahne kurulum sihirbazı ekler.
    /// Tek tıkla AR sahnesini tüm gerekli bileşenlerle oluşturur.
    /// </summary>
    public class ARSceneSetupWizard : EditorWindow
    {
        private bool _createARSession = true;
        private bool _createXROrigin = true;
        private bool _createLighting = true;
        private bool _createCanvas = true;
        private bool _createManagers = true;
        private float _defaultWallHeight = 2.5f;

        [MenuItem("ARRoomTransformer/Sahne Kurulum Sihirbazı", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<ARSceneSetupWizard>("AR Sahne Kurulumu");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        [MenuItem("ARRoomTransformer/Hızlı Sahne Oluştur", false, 1)]
        public static void QuickSetup()
        {
            if (EditorUtility.DisplayDialog(
                "Hızlı Sahne Kurulumu",
                "Yeni bir AR sahnesi oluşturulacak. Mevcut sahne kaydedilmemiş değişiklikler içeriyorsa kaybolabilir.\n\nDevam etmek istiyor musunuz?",
                "Evet, Oluştur", "İptal"))
            {
                CreateFullARScene();
            }
        }

        [MenuItem("ARRoomTransformer/Tag'leri Oluştur", false, 20)]
        public static void CreateRequiredTags()
        {
            AddTag("RoomWall");
            AddTag("RoomFloor");
            AddTag("RoomCeiling");
            AddTag("CornerMarker");
            AddTag("PlacedAsset");
            Debug.Log("[ARRoomTransformer] Tüm gerekli tag'ler oluşturuldu.");
            EditorUtility.DisplayDialog("Başarılı", "Gerekli tag'ler oluşturuldu:\n• RoomWall\n• RoomFloor\n• RoomCeiling\n• CornerMarker\n• PlacedAsset", "Tamam");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            // Başlık
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("🏠 AR Sahne Kurulum Sihirbazı", titleStyle);
            GUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Bu sihirbaz, ARRoomTransformer için gerekli tüm sahne objelerini otomatik oluşturur.",
                MessageType.Info);
            GUILayout.Space(10);

            // Seçenekler
            EditorGUILayout.LabelField("Oluşturulacak Bileşenler", EditorStyles.boldLabel);
            _createARSession = EditorGUILayout.Toggle("AR Session", _createARSession);
            _createXROrigin = EditorGUILayout.Toggle("XR Origin (AR Camera)", _createXROrigin);
            _createLighting = EditorGUILayout.Toggle("AR Işıklandırma", _createLighting);
            _createCanvas = EditorGUILayout.Toggle("UI Canvas", _createCanvas);
            _createManagers = EditorGUILayout.Toggle("Manager Objeleri", _createManagers);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Ayarlar", EditorStyles.boldLabel);
            _defaultWallHeight = EditorGUILayout.FloatField("Varsayılan Duvar Yüksekliği (m)", _defaultWallHeight);

            GUILayout.Space(20);

            // Butonlar
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("🚀 Sahneyi Oluştur", GUILayout.Height(40)))
            {
                CreateScene();
            }

            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.9f);
            if (GUILayout.Button("🏷️ Gerekli Tag'leri Oluştur", GUILayout.Height(30)))
            {
                CreateRequiredTags();
            }

            GUI.backgroundColor = Color.white;
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Sahne oluşturduktan sonra:\n" +
                "1. Edit → Project Settings → XR Plug-in Management → ARKit ✓\n" +
                "2. Asset Store'dan 3D modeller indirin\n" +
                "3. AssetCatalog ScriptableObject oluşturun\n" +
                "4. iPhone'da build alın",
                MessageType.Warning);
        }

        private void CreateScene()
        {
            // Yeni sahne
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            if (_createARSession) CreateARSession();
            if (_createXROrigin) CreateXROrigin();
            if (_createLighting) CreateARLighting();
            if (_createCanvas) CreateUICanvas();
            if (_createManagers) CreateManagers();

            // Sahneyi kaydet
            string scenePath = "Assets/Scenes/ARRoomTransformer.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log("[ARRoomTransformer] Sahne başarıyla oluşturuldu: " + scenePath);
            EditorUtility.DisplayDialog("Başarılı!", "AR sahnesi oluşturuldu!\n\nKonum: " + scenePath, "Harika!");
        }

        public static void CreateFullARScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateARSession();
            CreateXROrigin();
            CreateARLighting();
            CreateUICanvas();
            CreateManagers();
            CreateRequiredTags();

            string scenePath = "Assets/Scenes/ARRoomTransformer.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log("[ARRoomTransformer] Tam sahne oluşturuldu: " + scenePath);
        }

        private static void CreateARSession()
        {
            var sessionGO = new GameObject("AR Session");
            sessionGO.AddComponent<UnityEngine.XR.ARFoundation.ARSession>();
            // NOT: ARInputManager, AR Foundation 5.x'te deprecated, 6.x'te kaldırıldı.
            // Yeni Input System ile entegrasyon otomatik sağlanıyor.
            Debug.Log("  ✓ AR Session oluşturuldu");
        }

        private static void CreateXROrigin()
        {
            // XR Origin
            var originGO = new GameObject("XR Origin");
            var xrOrigin = originGO.AddComponent<Unity.XR.CoreUtils.XROrigin>();

            // Camera Offset
            var offsetGO = new GameObject("Camera Offset");
            offsetGO.transform.SetParent(originGO.transform);

            // AR Camera
            var cameraGO = new GameObject("AR Camera");
            cameraGO.transform.SetParent(offsetGO.transform);
            cameraGO.tag = "MainCamera";

            var camera = cameraGO.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 20f;

            cameraGO.AddComponent<UnityEngine.XR.ARFoundation.ARCameraManager>();
            cameraGO.AddComponent<UnityEngine.XR.ARFoundation.ARCameraBackground>();

            // XR Origin ayarları
            xrOrigin.CameraFloorOffsetObject = offsetGO;
            xrOrigin.Camera = camera;

            // AR Managers (XR Origin üstüne)
            originGO.AddComponent<UnityEngine.XR.ARFoundation.ARPlaneManager>();
            originGO.AddComponent<UnityEngine.XR.ARFoundation.ARRaycastManager>();
            originGO.AddComponent<UnityEngine.XR.ARFoundation.ARAnchorManager>();
            originGO.AddComponent<UnityEngine.XR.ARFoundation.ARPointCloudManager>();

            // Occlusion Manager (kamera üstüne)
            cameraGO.AddComponent<UnityEngine.XR.ARFoundation.AROcclusionManager>();

            Debug.Log("  ✓ XR Origin + AR Camera oluşturuldu");
        }

        private static void CreateARLighting()
        {
            var lightGO = new GameObject("AR Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.shadows = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

            // ARLightEstimation scripti ekle
            lightGO.AddComponent<ARLightEstimation>();

            Debug.Log("  ✓ AR Işıklandırma oluşturuldu");
        }

        private static void CreateUICanvas()
        {
            var canvasGO = new GameObject("UI Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            var scaler = canvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // EventSystem
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // UI Panel Placeholder'ları
            string[] panels = { "MainMenuPanel", "RoomScanPanel", "AssetPlacementPanel", "RecordingPanel" };
            foreach (var panelName in panels)
            {
                var panel = new GameObject(panelName);
                panel.transform.SetParent(canvasGO.transform, false);
                var rect = panel.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                panel.AddComponent<CanvasGroup>();

                // İlk panel hariç hepsini gizle
                if (panelName != "MainMenuPanel")
                {
                    panel.SetActive(false);
                }
            }

            Debug.Log("  ✓ UI Canvas + 4 panel oluşturuldu");
        }

        private static void CreateManagers()
        {
            var managersGO = new GameObject("--- MANAGERS ---");

            // App Manager
            var appMgrGO = new GameObject("AppManager");
            appMgrGO.transform.SetParent(managersGO.transform);
            appMgrGO.AddComponent<AppManager>();

            // Material Manager
            var matMgrGO = new GameObject("MaterialManager");
            matMgrGO.transform.SetParent(managersGO.transform);
            matMgrGO.AddComponent<MaterialManager>();

            // Save Load Manager
            var saveMgrGO = new GameObject("SaveLoadManager");
            saveMgrGO.transform.SetParent(managersGO.transform);
            saveMgrGO.AddComponent<SaveLoadManager>();

            // Room Scanner
            var scannerGO = new GameObject("RoomScanner");
            scannerGO.transform.SetParent(managersGO.transform);
            scannerGO.AddComponent<RoomScanner>();
            scannerGO.AddComponent<RoomBoundaryDetector>();
            scannerGO.AddComponent<CornerMarker>();
            scannerGO.AddComponent<WallGenerator>();
            scannerGO.AddComponent<FloorPlanGenerator>();

            // Asset Placer
            var placerGO = new GameObject("AssetPlacer");
            placerGO.transform.SetParent(managersGO.transform);
            placerGO.AddComponent<AssetPlacer>();
            placerGO.AddComponent<AssetTransformHandler>();

            // Video Recorder
            var recorderGO = new GameObject("VideoRecorder");
            recorderGO.transform.SetParent(managersGO.transform);
            recorderGO.AddComponent<ARVideoRecorder>();
            recorderGO.AddComponent<ScreenCaptureManager>();

            // Occlusion Controller
            var occlusionGO = new GameObject("OcclusionController");
            occlusionGO.transform.SetParent(managersGO.transform);
            occlusionGO.AddComponent<OcclusionController>();

            // UI Manager
            var uiMgrGO = new GameObject("UIManager");
            uiMgrGO.transform.SetParent(managersGO.transform);
            uiMgrGO.AddComponent<UIManager>();

            Debug.Log("  ✓ Tüm Manager objeleri oluşturuldu");
        }

        /// <summary>Tag yoksa ekler.</summary>
        private static void AddTag(string tag)
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            var tagsProp = tagManager.FindProperty("tags");

            // Zaten var mı kontrol et
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    return; // Zaten mevcut
            }

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"  ✓ Tag eklendi: {tag}");
        }

    }
}
