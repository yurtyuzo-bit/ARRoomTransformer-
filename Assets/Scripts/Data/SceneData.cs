using System;
using System.Collections.Generic;

namespace ARRoomTransformer
{
    // ================================================================
    // Yerleştirilen Asset Verisi
    // ================================================================

    /// <summary>
    /// Sahneye yerleştirilmiş bir 3D asset'in serileştirilebilir verisi.
    /// Pozisyon, rotasyon, ölçek ve asset kataloğu referansını içerir.
    /// </summary>
    [System.Serializable]
    public class PlacedAssetData
    {
        /// <summary>Bu yerleştirilmiş objenin benzersiz kimliği.</summary>
        public string instanceId;

        /// <summary>Asset kataloğundaki referans kimliği.</summary>
        public string assetCatalogId;

        /// <summary>Asset'in adı (görüntüleme için).</summary>
        public string assetName;

        /// <summary>World space pozisyon.</summary>
        public Vector3Serializable position;

        /// <summary>World space rotasyon.</summary>
        public QuaternionSerializable rotation;

        /// <summary>Ölçek (local scale).</summary>
        public Vector3Serializable scale;

        /// <summary>Ek özellikler (JSON string, genişletilebilirlik için).</summary>
        public string customProperties;

        /// <summary>Asset'in yerleştirilme zamanı.</summary>
        public string placedAt;

        /// <summary>Yeni PlacedAssetData oluşturur.</summary>
        public PlacedAssetData()
        {
            instanceId = Guid.NewGuid().ToString();
            scale = new Vector3Serializable(1f, 1f, 1f);
            rotation = new QuaternionSerializable(0f, 0f, 0f, 1f);
            customProperties = "{}";
            placedAt = DateTime.UtcNow.ToString("o");
        }

        /// <summary>Parametreli PlacedAssetData oluşturur.</summary>
        public PlacedAssetData(string catalogId, string name, UnityEngine.Vector3 pos,
            UnityEngine.Quaternion rot, UnityEngine.Vector3 scl) : this()
        {
            assetCatalogId = catalogId;
            assetName = name;
            position = pos;
            rotation = rot;
            scale = scl;
        }

        public override string ToString()
        {
            return $"Asset [{assetName}] @ {position}";
        }
    }

    // ================================================================
    // Sahne Verisi
    // ================================================================

    /// <summary>
    /// Tam bir AR sahnesinin serileştirilebilir verisi.
    /// Oda verisi, yerleştirilen asset'ler, tema ve meta bilgileri içerir.
    /// </summary>
    [System.Serializable]
    public class SceneData
    {
        /// <summary>Veri format sürümü (ileri uyumluluk için).</summary>
        public int version;

        /// <summary>Sahne benzersiz kimliği (GUID).</summary>
        public string sceneId;

        /// <summary>Sahne adı (kullanıcı tarafından verilen).</summary>
        public string sceneName;

        /// <summary>Oluşturulma tarihi (UTC, ISO 8601).</summary>
        public string createdAt;

        /// <summary>Son değişiklik tarihi (UTC, ISO 8601).</summary>
        public string lastModifiedAt;

        /// <summary>Oda tarama verisi.</summary>
        public RoomData roomData;

        /// <summary>Sahneye yerleştirilen tüm asset'ler.</summary>
        public List<PlacedAssetData> placedAssets;

        /// <summary>Uygulanan tema adı.</summary>
        public string themeName;

        /// <summary>Sahne önizleme görüntüsünün dosya yolu.</summary>
        public string thumbnailPath;

        /// <summary>Mevcut veri format sürümü.</summary>
        public const int CURRENT_VERSION = 1;

        /// <summary>Yeni boş SceneData oluşturur.</summary>
        public SceneData()
        {
            version = CURRENT_VERSION;
            sceneId = Guid.NewGuid().ToString();
            sceneName = "Yeni Sahne";
            createdAt = DateTime.UtcNow.ToString("o");
            lastModifiedAt = createdAt;
            roomData = new RoomData();
            placedAssets = new List<PlacedAssetData>();
            themeName = "Backrooms";
            thumbnailPath = "";
        }

        /// <summary>İsimli SceneData oluşturur.</summary>
        public SceneData(string name) : this()
        {
            sceneName = name;
        }

        /// <summary>Sahneye asset ekler.</summary>
        public void AddAsset(PlacedAssetData asset)
        {
            if (asset == null) return;
            placedAssets.Add(asset);
            MarkModified();
        }

        /// <summary>Sahneden asset kaldırır.</summary>
        public bool RemoveAsset(string instanceId)
        {
            int removed = placedAssets.RemoveAll(a => a.instanceId == instanceId);
            if (removed > 0)
            {
                MarkModified();
                return true;
            }
            return false;
        }

        /// <summary>Instance ID ile asset bulur.</summary>
        public PlacedAssetData FindAsset(string instanceId)
        {
            return placedAssets.Find(a => a.instanceId == instanceId);
        }

        /// <summary>Son değişiklik zamanını günceller.</summary>
        public void MarkModified()
        {
            lastModifiedAt = DateTime.UtcNow.ToString("o");
        }

        /// <summary>Sahne verilerinin geçerliliğini kontrol eder.</summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(sceneId) &&
                   !string.IsNullOrEmpty(sceneName) &&
                   roomData != null;
        }

        /// <summary>Hafif metadata versiyonu oluşturur (listeleme için).</summary>
        public SceneMetadata ToMetadata()
        {
            return new SceneMetadata
            {
                sceneId = sceneId,
                sceneName = sceneName,
                thumbnailPath = thumbnailPath,
                lastModifiedAt = lastModifiedAt,
                assetCount = placedAssets?.Count ?? 0,
                themeName = themeName
            };
        }

        public override string ToString()
        {
            return $"Sahne [{sceneName}] - " +
                   $"ID: {sceneId.Substring(0, 8)}, " +
                   $"Asset: {placedAssets?.Count ?? 0}, " +
                   $"Tema: {themeName}";
        }
    }

    // ================================================================
    // Sahne Metadata (Hafif - Listeleme İçin)
    // ================================================================

    /// <summary>
    /// Sahne listesinde gösterilecek hafif metadata.
    /// Tam SceneData yüklemeden sahne bilgisi göstermek için kullanılır.
    /// </summary>
    [System.Serializable]
    public class SceneMetadata
    {
        /// <summary>Sahne benzersiz kimliği.</summary>
        public string sceneId;

        /// <summary>Sahne adı.</summary>
        public string sceneName;

        /// <summary>Önizleme görüntüsü yolu.</summary>
        public string thumbnailPath;

        /// <summary>Son değişiklik tarihi.</summary>
        public string lastModifiedAt;

        /// <summary>Yerleştirilen asset sayısı.</summary>
        public int assetCount;

        /// <summary>Tema adı.</summary>
        public string themeName;

        /// <summary>Son değişiklik tarihini DateTime olarak döndürür.</summary>
        public DateTime LastModifiedDateTime
        {
            get
            {
                if (DateTime.TryParse(lastModifiedAt, out DateTime dt))
                    return dt;
                return DateTime.MinValue;
            }
        }

        public override string ToString()
        {
            return $"{sceneName} ({assetCount} asset, {themeName})";
        }
    }
}
