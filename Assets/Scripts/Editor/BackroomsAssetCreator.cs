using UnityEngine;
using UnityEditor;
using System.IO;

namespace ARRoomTransformer.Editor
{
    /// <summary>
    /// Backrooms modellerinden prefab'lar ve AssetCatalog ScriptableObject oluşturur.
    /// Unity menüsünden tek tıkla çalışır.
    /// </summary>
    public class BackroomsAssetCreator : UnityEditor.Editor
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Backrooms";
        private const string CATALOG_PATH = "Assets/ScriptableObjects";
        private const string THUMBNAIL_PATH = "Assets/Textures/Thumbnails";

        [MenuItem("ARRoomTransformer/Backrooms Asset'leri Oluştur", false, 30)]
        public static void CreateAllBackroomsAssets()
        {
            EnsureDirectories();

            // 1. Prefab'ları oluştur
            CreatePrefab("Fluorescent_Light", BackroomsModelFactory.CreateFluorescentLight());
            CreatePrefab("Pipe_3m", BackroomsModelFactory.CreatePipe(3f));
            CreatePrefab("Pipe_2m", BackroomsModelFactory.CreatePipe(2f));
            CreatePrefab("Pipe_1m", BackroomsModelFactory.CreatePipe(1f));
            CreatePrefab("DoorFrame", BackroomsModelFactory.CreateDoorFrame());
            CreatePrefab("MetalShelf", BackroomsModelFactory.CreateMetalShelf());
            CreatePrefab("OfficeChair", BackroomsModelFactory.CreateOfficeChair());
            CreatePrefab("Table", BackroomsModelFactory.CreateTable());
            CreatePrefab("CardboardBox_Large", BackroomsModelFactory.CreateCardboardBox(0.5f));
            CreatePrefab("CardboardBox_Small", BackroomsModelFactory.CreateCardboardBox(0.3f));
            CreatePrefab("Barrel", BackroomsModelFactory.CreateBarrel());
            CreatePrefab("ExitSign", BackroomsModelFactory.CreateExitSign());
            CreatePrefab("WaterPuddle", BackroomsModelFactory.CreateWaterPuddle());
            CreatePrefab("WaterPuddle_Small", BackroomsModelFactory.CreateWaterPuddle(0.25f));
            CreatePrefab("Couch", BackroomsModelFactory.CreateCouch());

            // 2. AssetCatalog oluştur
            CreateAssetCatalog();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Backrooms Asset'leri Hazır! 🎉",
                "15 prefab ve 1 AssetCatalog oluşturuldu.\n\n" +
                $"Prefab'lar: {PREFAB_PATH}\n" +
                $"Katalog: {CATALOG_PATH}/BackroomsAssetCatalog.asset",
                "Harika!");
        }

        [MenuItem("ARRoomTransformer/Sadece Prefab'ları Oluştur", false, 31)]
        public static void CreateOnlyPrefabs()
        {
            EnsureDirectories();

            CreatePrefab("Fluorescent_Light", BackroomsModelFactory.CreateFluorescentLight());
            CreatePrefab("Pipe_3m", BackroomsModelFactory.CreatePipe(3f));
            CreatePrefab("DoorFrame", BackroomsModelFactory.CreateDoorFrame());
            CreatePrefab("MetalShelf", BackroomsModelFactory.CreateMetalShelf());
            CreatePrefab("OfficeChair", BackroomsModelFactory.CreateOfficeChair());
            CreatePrefab("Table", BackroomsModelFactory.CreateTable());
            CreatePrefab("CardboardBox_Large", BackroomsModelFactory.CreateCardboardBox(0.5f));
            CreatePrefab("Barrel", BackroomsModelFactory.CreateBarrel());
            CreatePrefab("ExitSign", BackroomsModelFactory.CreateExitSign());
            CreatePrefab("WaterPuddle", BackroomsModelFactory.CreateWaterPuddle());
            CreatePrefab("Couch", BackroomsModelFactory.CreateCouch());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[BackroomsAssetCreator] 11 prefab oluşturuldu!");
        }

        [MenuItem("ARRoomTransformer/Sadece AssetCatalog Oluştur", false, 32)]
        public static void CreateOnlyCatalog()
        {
            EnsureDirectories();
            CreateAssetCatalog();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[BackroomsAssetCreator] AssetCatalog oluşturuldu!");
        }

        // ================================================================
        // Prefab Oluşturma
        // ================================================================

        private static void CreatePrefab(string name, GameObject instance)
        {
            string path = $"{PREFAB_PATH}/{name}.prefab";

            // Varolan prefab'ı güncelle veya yeni oluştur
            bool success;
            PrefabUtility.SaveAsPrefabAsset(instance, path, out success);

            if (success)
                Debug.Log($"  ✓ Prefab oluşturuldu: {path}");
            else
                Debug.LogWarning($"  ✗ Prefab oluşturulamadı: {path}");

            // Sahnedeki geçici objeyi sil
            DestroyImmediate(instance);
        }

        // ================================================================
        // AssetCatalog Oluşturma
        // ================================================================

        private static void CreateAssetCatalog()
        {
            string catalogPath = $"{CATALOG_PATH}/BackroomsAssetCatalog.asset";

            // Mevcut katalog varsa yükle, yoksa yeni oluştur
            var catalog = AssetDatabase.LoadAssetAtPath<AssetCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<AssetCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            // Katalog girdilerini ekle
            catalog.ClearEntries();

            // --- AYDINLATMA ---
            AddCatalogEntry(catalog, "fluorescent_light", "Floresan Lamba",
                "Backrooms/Fluorescent_Light", AssetCategory.Lighting,
                new[] { "backrooms", "light", "ceiling", "floresan" },
                "Klasik Backrooms floresan tavan lambası. Titreme efekti dahil.");

            // --- YAPISAL ---
            AddCatalogEntry(catalog, "pipe_3m", "Boru (3m)",
                "Backrooms/Pipe_3m", AssetCategory.Structural,
                new[] { "backrooms", "pipe", "industrial" },
                "3 metre uzunluğunda metal boru.");

            AddCatalogEntry(catalog, "pipe_2m", "Boru (2m)",
                "Backrooms/Pipe_2m", AssetCategory.Structural,
                new[] { "backrooms", "pipe", "industrial" },
                "2 metre uzunluğunda metal boru.");

            AddCatalogEntry(catalog, "pipe_1m", "Boru (1m)",
                "Backrooms/Pipe_1m", AssetCategory.Structural,
                new[] { "backrooms", "pipe", "industrial" },
                "1 metre uzunluğunda metal boru.");

            AddCatalogEntry(catalog, "door_frame", "Kapı Çerçevesi",
                "Backrooms/DoorFrame", AssetCategory.Structural,
                new[] { "backrooms", "door", "frame", "exit" },
                "Karanlık bir geçişe açılan boş kapı çerçevesi.");

            AddCatalogEntry(catalog, "exit_sign", "EXIT Tabelası",
                "Backrooms/ExitSign", AssetCategory.Structural,
                new[] { "backrooms", "sign", "exit", "emergency" },
                "Kırmızı LED EXIT çıkış tabelası.");

            // --- MOBİLYA ---
            AddCatalogEntry(catalog, "metal_shelf", "Metal Raf",
                "Backrooms/MetalShelf", AssetCategory.Furniture,
                new[] { "backrooms", "shelf", "storage", "metal" },
                "4 katlı endüstriyel metal depo rafı.");

            AddCatalogEntry(catalog, "office_chair", "Ofis Sandalyesi",
                "Backrooms/OfficeChair", AssetCategory.Furniture,
                new[] { "backrooms", "chair", "office", "seat" },
                "Tekerlekli ofis sandalyesi.");

            AddCatalogEntry(catalog, "table", "Masa",
                "Backrooms/Table", AssetCategory.Furniture,
                new[] { "backrooms", "table", "desk", "furniture" },
                "Ahşap masa.");

            AddCatalogEntry(catalog, "couch", "Koltuk",
                "Backrooms/Couch", AssetCategory.Furniture,
                new[] { "backrooms", "couch", "sofa", "seat" },
                "Eski tip kumaş koltuk.");

            // --- DEKORASYON ---
            AddCatalogEntry(catalog, "box_large", "Karton Kutu (Büyük)",
                "Backrooms/CardboardBox_Large", AssetCategory.Decoration,
                new[] { "backrooms", "box", "cardboard", "storage" },
                "Büyük karton kargo kutusu.");

            AddCatalogEntry(catalog, "box_small", "Karton Kutu (Küçük)",
                "Backrooms/CardboardBox_Small", AssetCategory.Decoration,
                new[] { "backrooms", "box", "cardboard" },
                "Küçük karton kutu.");

            AddCatalogEntry(catalog, "barrel", "Metal Varil",
                "Backrooms/Barrel", AssetCategory.Decoration,
                new[] { "backrooms", "barrel", "industrial", "container" },
                "Yeşil metal varil/bidon.");

            AddCatalogEntry(catalog, "puddle_large", "Su Birikintisi (Büyük)",
                "Backrooms/WaterPuddle", AssetCategory.Decoration,
                new[] { "backrooms", "water", "puddle", "floor" },
                "Zeminde büyük su birikintisi.");

            AddCatalogEntry(catalog, "puddle_small", "Su Birikintisi (Küçük)",
                "Backrooms/WaterPuddle_Small", AssetCategory.Decoration,
                new[] { "backrooms", "water", "puddle" },
                "Zeminde küçük su birikintisi.");

            EditorUtility.SetDirty(catalog);
            Debug.Log($"  ✓ AssetCatalog oluşturuldu: {catalogPath} ({catalog.EntryCount} asset)");
        }

        private static void AddCatalogEntry(AssetCatalog catalog, string id, string displayName,
            string prefabPath, AssetCategory category, string[] tags, string description)
        {
            // Prefab referansını yükle
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/{prefabPath}.prefab");

            catalog.AddEntry(new AssetEntry
            {
                assetId = id,
                displayName = displayName,
                prefab = prefab,
                category = category,
                tags = new System.Collections.Generic.List<string>(tags),
                description = description,
                defaultScale = 1f
            });
        }

        // ================================================================
        // Yardımcı
        // ================================================================

        private static void EnsureDirectories()
        {
            EnsureDirectory("Assets/Prefabs");
            EnsureDirectory("Assets/Prefabs/Backrooms");
            EnsureDirectory("Assets/ScriptableObjects");
            EnsureDirectory("Assets/Textures/Thumbnails");
        }

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folderName = Path.GetFileName(path);

                if (!AssetDatabase.IsValidFolder(parent))
                {
                    EnsureDirectory(parent);
                }

                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
