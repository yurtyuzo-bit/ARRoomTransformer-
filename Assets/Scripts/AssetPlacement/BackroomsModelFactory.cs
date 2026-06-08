using UnityEngine;
using System.Collections.Generic;

namespace ARRoomTransformer
{
    /// <summary>
    /// Backrooms temalı prosedürel 3D modeller oluşturur.
    /// Asset Store'a gerek kalmadan kod ile modeller üretir.
    /// Unity Editor menüsünden veya runtime'da çağrılabilir.
    /// </summary>
    public static class BackroomsModelFactory
    {
        // ================================================================
        // FLORESAN LAMBA
        // ================================================================

        /// <summary>
        /// Backrooms tarzı tavan floresan lambası oluşturur.
        /// Uzun dikdörtgen kutu + ışık yayan yüzey.
        /// </summary>
        public static GameObject CreateFluorescentLight(Transform parent = null)
        {
            var root = new GameObject("Fluorescent_Light");
            if (parent != null) root.transform.SetParent(parent);

            // Ana gövde (metal kasa)
            var housing = CreateBox(root.transform, "Housing",
                new Vector3(1.2f, 0.05f, 0.15f),
                Vector3.zero,
                CreateMaterial("FluorescentHousing", new Color(0.75f, 0.75f, 0.75f), 0.6f, 0.2f));

            // Işık tüpü (parlayan kısım)
            var tube = CreateBox(root.transform, "LightTube",
                new Vector3(1.1f, 0.02f, 0.08f),
                new Vector3(0, -0.035f, 0),
                CreateEmissiveMaterial("FluorescentTube",
                    new Color(0.95f, 0.95f, 0.85f),
                    new Color(1f, 0.98f, 0.9f), 3f));

            // Gerçek ışık kaynağı
            var lightGO = new GameObject("PointLight");
            lightGO.transform.SetParent(root.transform);
            lightGO.transform.localPosition = new Vector3(0, -0.1f, 0);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.98f, 0.95f, 0.85f);
            light.intensity = 2f;
            light.range = 5f;
            light.shadows = LightShadows.Soft;

            // Flicker efekti (opsiyonel)
            root.AddComponent<FluorescentFlicker>();

            return root;
        }

        // ================================================================
        // BORU / PIPE
        // ================================================================

        /// <summary>
        /// Duvar/tavan borusu oluşturur.
        /// </summary>
        public static GameObject CreatePipe(float length = 3f, float radius = 0.03f, Transform parent = null)
        {
            var root = new GameObject("Pipe");
            if (parent != null) root.transform.SetParent(parent);

            var pipe = CreateCylinder(root.transform, "PipeBody",
                radius, length,
                Vector3.zero,
                Quaternion.Euler(0, 0, 90),
                CreateMaterial("PipeMetal", new Color(0.5f, 0.48f, 0.45f), 0.7f, 0.5f));

            // Bağlantı noktaları (uçlarda halka)
            CreateCylinder(root.transform, "Joint_Start",
                radius * 1.5f, 0.03f,
                new Vector3(-length / 2f, 0, 0),
                Quaternion.Euler(0, 0, 90),
                CreateMaterial("PipeJoint", new Color(0.4f, 0.38f, 0.35f), 0.8f, 0.4f));

            CreateCylinder(root.transform, "Joint_End",
                radius * 1.5f, 0.03f,
                new Vector3(length / 2f, 0, 0),
                Quaternion.Euler(0, 0, 90),
                CreateMaterial("PipeJoint2", new Color(0.4f, 0.38f, 0.35f), 0.8f, 0.4f));

            return root;
        }

        // ================================================================
        // KAPI ÇERÇEVESI
        // ================================================================

        /// <summary>
        /// Boş kapı çerçevesi oluşturur (arkasında karanlık).
        /// </summary>
        public static GameObject CreateDoorFrame(float width = 0.9f, float height = 2.1f, Transform parent = null)
        {
            var root = new GameObject("DoorFrame");
            if (parent != null) root.transform.SetParent(parent);

            float frameWidth = 0.08f;
            float frameDepth = 0.15f;
            var frameMat = CreateMaterial("DoorFrame", new Color(0.6f, 0.55f, 0.4f), 0.1f, 0.2f);

            // Sol dikey
            CreateBox(root.transform, "LeftFrame",
                new Vector3(frameWidth, height, frameDepth),
                new Vector3(-width / 2f - frameWidth / 2f, height / 2f, 0),
                frameMat);

            // Sağ dikey
            CreateBox(root.transform, "RightFrame",
                new Vector3(frameWidth, height, frameDepth),
                new Vector3(width / 2f + frameWidth / 2f, height / 2f, 0),
                frameMat);

            // Üst yatay
            CreateBox(root.transform, "TopFrame",
                new Vector3(width + frameWidth * 2, frameWidth, frameDepth),
                new Vector3(0, height + frameWidth / 2f, 0),
                frameMat);

            // Karanlık arka plan (kapının içi)
            CreateBox(root.transform, "DarkInside",
                new Vector3(width, height, 0.02f),
                new Vector3(0, height / 2f, frameDepth / 2f),
                CreateMaterial("DarkVoid", new Color(0.02f, 0.02f, 0.03f), 0f, 0f));

            return root;
        }

        // ================================================================
        // METAL RAF
        // ================================================================

        /// <summary>
        /// Metal depo rafı oluşturur.
        /// </summary>
        public static GameObject CreateMetalShelf(int shelfCount = 4, Transform parent = null)
        {
            var root = new GameObject("MetalShelf");
            if (parent != null) root.transform.SetParent(parent);

            float width = 1.0f;
            float depth = 0.4f;
            float totalHeight = 1.8f;
            float legRadius = 0.02f;

            var legMat = CreateMaterial("ShelfLeg", new Color(0.45f, 0.43f, 0.40f), 0.8f, 0.4f);
            var shelfMat = CreateMaterial("ShelfPlate", new Color(0.5f, 0.48f, 0.44f), 0.6f, 0.3f);

            // 4 bacak
            Vector3[] legPositions = {
                new Vector3(-width/2f, totalHeight/2f, -depth/2f),
                new Vector3(width/2f, totalHeight/2f, -depth/2f),
                new Vector3(-width/2f, totalHeight/2f, depth/2f),
                new Vector3(width/2f, totalHeight/2f, depth/2f)
            };

            for (int i = 0; i < 4; i++)
            {
                CreateCylinder(root.transform, $"Leg_{i}",
                    legRadius, totalHeight,
                    legPositions[i], Quaternion.identity, legMat);
            }

            // Raf katları
            for (int i = 0; i < shelfCount; i++)
            {
                float y = (totalHeight / (shelfCount - 1)) * i;
                CreateBox(root.transform, $"Shelf_{i}",
                    new Vector3(width, 0.02f, depth),
                    new Vector3(0, y, 0),
                    shelfMat);
            }

            return root;
        }

        // ================================================================
        // SANDALYE (OFIS TİPİ)
        // ================================================================

        /// <summary>
        /// Basit ofis sandalyesi oluşturur.
        /// </summary>
        public static GameObject CreateOfficeChair(Transform parent = null)
        {
            var root = new GameObject("OfficeChair");
            if (parent != null) root.transform.SetParent(parent);

            var seatMat = CreateMaterial("ChairSeat", new Color(0.25f, 0.25f, 0.28f), 0.1f, 0.15f);
            var metalMat = CreateMaterial("ChairMetal", new Color(0.35f, 0.35f, 0.38f), 0.8f, 0.5f);

            // Oturma yeri
            CreateBox(root.transform, "Seat",
                new Vector3(0.45f, 0.06f, 0.45f),
                new Vector3(0, 0.45f, 0), seatMat);

            // Sırt dayama
            CreateBox(root.transform, "Backrest",
                new Vector3(0.43f, 0.4f, 0.04f),
                new Vector3(0, 0.7f, -0.2f), seatMat);

            // Merkez direk
            CreateCylinder(root.transform, "CenterPole",
                0.025f, 0.25f,
                new Vector3(0, 0.3f, 0), Quaternion.identity, metalMat);

            // Taban (yıldız şekli - 5 kol basitleştirilmiş)
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle) * 0.25f;
                float z = Mathf.Cos(angle) * 0.25f;

                CreateCylinder(root.transform, $"BaseLeg_{i}",
                    0.015f, 0.25f,
                    new Vector3(x / 2f, 0.05f, z / 2f),
                    Quaternion.Euler(0, 0, 90f - i * 72f),
                    metalMat);

                // Tekerlek
                CreateSphere(root.transform, $"Wheel_{i}",
                    0.02f,
                    new Vector3(x, 0.02f, z),
                    CreateMaterial($"Wheel{i}", new Color(0.15f, 0.15f, 0.15f), 0.1f, 0.3f));
            }

            return root;
        }

        // ================================================================
        // MASA
        // ================================================================

        /// <summary>
        /// Basit masa oluşturur.
        /// </summary>
        public static GameObject CreateTable(Transform parent = null)
        {
            var root = new GameObject("Table");
            if (parent != null) root.transform.SetParent(parent);

            float width = 1.2f;
            float depth = 0.6f;
            float height = 0.75f;
            float topThickness = 0.03f;
            float legSize = 0.04f;

            var topMat = CreateMaterial("TableTop", new Color(0.55f, 0.45f, 0.3f), 0.1f, 0.2f);
            var legMat = CreateMaterial("TableLeg", new Color(0.5f, 0.4f, 0.28f), 0.1f, 0.15f);

            // Masa üstü
            CreateBox(root.transform, "Top",
                new Vector3(width, topThickness, depth),
                new Vector3(0, height, 0), topMat);

            // 4 bacak
            float legHeight = height - topThickness;
            Vector3[] legPos = {
                new Vector3(-width/2f + legSize, legHeight/2f, -depth/2f + legSize),
                new Vector3(width/2f - legSize, legHeight/2f, -depth/2f + legSize),
                new Vector3(-width/2f + legSize, legHeight/2f, depth/2f - legSize),
                new Vector3(width/2f - legSize, legHeight/2f, depth/2f - legSize)
            };

            for (int i = 0; i < 4; i++)
            {
                CreateBox(root.transform, $"Leg_{i}",
                    new Vector3(legSize, legHeight, legSize),
                    legPos[i], legMat);
            }

            return root;
        }

        // ================================================================
        // KUTU / KARGO
        // ================================================================

        /// <summary>Karton kutu oluşturur.</summary>
        public static GameObject CreateCardboardBox(float size = 0.4f, Transform parent = null)
        {
            var root = new GameObject("CardboardBox");
            if (parent != null) root.transform.SetParent(parent);

            CreateBox(root.transform, "Box",
                new Vector3(size, size * 0.8f, size),
                new Vector3(0, size * 0.4f, 0),
                CreateMaterial("Cardboard", new Color(0.72f, 0.58f, 0.38f), 0f, 0.1f));

            // Bant şeridi (üst)
            CreateBox(root.transform, "Tape",
                new Vector3(0.06f, 0.005f, size),
                new Vector3(0, size * 0.8f + 0.003f, 0),
                CreateMaterial("Tape", new Color(0.8f, 0.75f, 0.5f), 0f, 0.4f));

            return root;
        }

        // ================================================================
        // VARIL / BIDON
        // ================================================================

        /// <summary>Metal varil oluşturur.</summary>
        public static GameObject CreateBarrel(Transform parent = null)
        {
            var root = new GameObject("Barrel");
            if (parent != null) root.transform.SetParent(parent);

            CreateCylinder(root.transform, "Body",
                0.25f, 0.9f,
                new Vector3(0, 0.45f, 0), Quaternion.identity,
                CreateMaterial("BarrelBody", new Color(0.3f, 0.45f, 0.25f), 0.5f, 0.3f));

            // Üst/alt halka
            CreateCylinder(root.transform, "TopRing",
                0.27f, 0.03f,
                new Vector3(0, 0.88f, 0), Quaternion.identity,
                CreateMaterial("BarrelRing1", new Color(0.4f, 0.38f, 0.35f), 0.8f, 0.5f));

            CreateCylinder(root.transform, "BottomRing",
                0.27f, 0.03f,
                new Vector3(0, 0.03f, 0), Quaternion.identity,
                CreateMaterial("BarrelRing2", new Color(0.4f, 0.38f, 0.35f), 0.8f, 0.5f));

            return root;
        }

        // ================================================================
        // DUVAR POSTERİ / İŞARET
        // ================================================================

        /// <summary>"EXIT" tabela oluşturur.</summary>
        public static GameObject CreateExitSign(Transform parent = null)
        {
            var root = new GameObject("ExitSign");
            if (parent != null) root.transform.SetParent(parent);

            // Tabela gövdesi
            CreateBox(root.transform, "SignBody",
                new Vector3(0.35f, 0.12f, 0.03f),
                Vector3.zero,
                CreateEmissiveMaterial("ExitSignMat",
                    new Color(0.8f, 0.1f, 0.1f),
                    new Color(1f, 0.2f, 0.15f), 2f));

            // Montaj braketi
            CreateBox(root.transform, "Bracket",
                new Vector3(0.04f, 0.15f, 0.04f),
                new Vector3(0, 0.12f, 0),
                CreateMaterial("BracketMetal", new Color(0.5f, 0.5f, 0.5f), 0.8f, 0.5f));

            return root;
        }

        // ================================================================
        // SU BİRİKİNTİSİ (ZEMIN)
        // ================================================================

        /// <summary>Zeminde su birikintisi oluşturur.</summary>
        public static GameObject CreateWaterPuddle(float radius = 0.4f, Transform parent = null)
        {
            var root = new GameObject("WaterPuddle");
            if (parent != null) root.transform.SetParent(parent);

            // Düz disk (çok ince silindir)
            CreateCylinder(root.transform, "Puddle",
                radius, 0.002f,
                new Vector3(0, 0.001f, 0), Quaternion.identity,
                CreateMaterial("Water", new Color(0.3f, 0.35f, 0.4f, 0.6f), 0.3f, 0.9f, true));

            return root;
        }

        // ================================================================
        // KOLTUK
        // ================================================================

        /// <summary>Eski tip koltuk oluşturur.</summary>
        public static GameObject CreateCouch(Transform parent = null)
        {
            var root = new GameObject("Couch");
            if (parent != null) root.transform.SetParent(parent);

            var fabricMat = CreateMaterial("CouchFabric", new Color(0.4f, 0.32f, 0.25f), 0f, 0.15f);
            var legMat = CreateMaterial("CouchLeg", new Color(0.3f, 0.25f, 0.18f), 0.1f, 0.2f);

            float w = 1.8f, d = 0.7f, seatH = 0.4f, backH = 0.35f;

            // Oturma
            CreateBox(root.transform, "Seat",
                new Vector3(w, 0.15f, d),
                new Vector3(0, seatH, 0), fabricMat);

            // Sırt
            CreateBox(root.transform, "Back",
                new Vector3(w, backH, 0.12f),
                new Vector3(0, seatH + 0.15f + backH / 2f, -d / 2f + 0.06f), fabricMat);

            // Sol kol
            CreateBox(root.transform, "ArmLeft",
                new Vector3(0.1f, 0.2f, d),
                new Vector3(-w / 2f + 0.05f, seatH + 0.1f, 0), fabricMat);

            // Sağ kol
            CreateBox(root.transform, "ArmRight",
                new Vector3(0.1f, 0.2f, d),
                new Vector3(w / 2f - 0.05f, seatH + 0.1f, 0), fabricMat);

            // Bacaklar
            float legH = seatH - 0.15f;
            Vector3[] legs = {
                new Vector3(-w/2f+0.1f, legH/2f, -d/2f+0.1f),
                new Vector3(w/2f-0.1f, legH/2f, -d/2f+0.1f),
                new Vector3(-w/2f+0.1f, legH/2f, d/2f-0.1f),
                new Vector3(w/2f-0.1f, legH/2f, d/2f-0.1f)
            };

            for (int i = 0; i < 4; i++)
            {
                CreateBox(root.transform, $"Leg_{i}",
                    new Vector3(0.05f, legH, 0.05f), legs[i], legMat);
            }

            return root;
        }

        // ================================================================
        // YARDIMCI METOTLAR
        // ================================================================

        private static GameObject CreateBox(Transform parent, string name, Vector3 size, Vector3 position, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().material = material;
            return go;
        }

        private static GameObject CreateCylinder(Transform parent, string name, float radius, float height, Vector3 position, Quaternion rotation, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.transform.localScale = new Vector3(radius * 2, height / 2f, radius * 2);
            go.GetComponent<Renderer>().material = material;
            return go;
        }

        private static GameObject CreateSphere(Transform parent, string name, float radius, Vector3 position, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * radius * 2;
            go.GetComponent<Renderer>().material = material;
            return go;
        }

        private static Material CreateMaterial(string name, Color color, float metallic, float smoothness, bool transparent = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = name };

            if (transparent)
            {
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.renderQueue = 3000;
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            return mat;
        }

        private static Material CreateEmissiveMaterial(string name, Color baseColor, Color emissionColor, float emissionIntensity)
        {
            var mat = CreateMaterial(name, baseColor, 0f, 0.5f);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", emissionColor * emissionIntensity);
            return mat;
        }
    }

    /// <summary>
    /// Floresan lamba titreme (flicker) efekti.
    /// Backrooms atmosferi için.
    /// </summary>
    public class FluorescentFlicker : MonoBehaviour
    {
        [SerializeField] private float flickerChance = 0.02f;
        [SerializeField] private float flickerDuration = 0.05f;
        [SerializeField] private float minIntensity = 0.3f;
        [SerializeField] private float maxIntensity = 1.0f;
        [SerializeField] private bool enableFlicker = true;

        private Light _light;
        private Renderer _tubeRenderer;
        private float _baseIntensity;
        private float _flickerTimer;

        private void Start()
        {
            _light = GetComponentInChildren<Light>();
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r.gameObject.name == "LightTube")
                {
                    _tubeRenderer = r;
                    break;
                }
            }

            if (_light != null) _baseIntensity = _light.intensity;
        }

        private void Update()
        {
            if (!enableFlicker || _light == null) return;

            _flickerTimer -= Time.deltaTime;

            if (_flickerTimer <= 0)
            {
                if (Random.value < flickerChance)
                {
                    // Flicker!
                    float flickerIntensity = Random.Range(minIntensity, maxIntensity);
                    _light.intensity = flickerIntensity * _baseIntensity;

                    if (_tubeRenderer != null)
                    {
                        Color emColor = Color.white * flickerIntensity * 3f;
                        _tubeRenderer.material.SetColor("_EmissionColor", emColor);
                    }

                    _flickerTimer = flickerDuration;
                }
                else
                {
                    // Normal
                    _light.intensity = Mathf.Lerp(_light.intensity, _baseIntensity, Time.deltaTime * 10f);
                    _flickerTimer = 0.1f;
                }
            }
        }
    }
}
