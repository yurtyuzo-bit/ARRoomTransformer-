using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;

namespace ARRoomTransformer
{
    /// <summary>
    /// Sahne verilerini JSON formatında kaydetme ve yükleme yöneticisi.
    /// Application.persistentDataPath altında dosya tabanlı depolama yapar.
    /// Otomatik kayıt, dışa/içe aktarma ve sürüm uyumluluğu destekler.
    /// </summary>
    public class SaveLoadManager : MonoBehaviour
    {
        [Header("Ayarlar")]
        [SerializeField] private string scenesSubfolder = "Scenes";
        [SerializeField] private float autoSaveIntervalSeconds = 60f;
        [SerializeField] private bool enableAutoSave = false;

        [Header("Events")]
        [SerializeField] private UnityEvent<string> onSceneSaved = new UnityEvent<string>();
        [SerializeField] private UnityEvent<SceneData> onSceneLoaded = new UnityEvent<SceneData>();
        [SerializeField] private UnityEvent<string> onSceneDeleted = new UnityEvent<string>();
        [SerializeField] private UnityEvent<string> onError = new UnityEvent<string>();

        private string _scenesDirectory;
        private Coroutine _autoSaveCoroutine;
        private SceneData _currentAutoSaveData;

        // JSON ayarları
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            DefaultValueHandling = DefaultValueHandling.Include
        };

        // Events
        public UnityEvent<string> OnSceneSaved => onSceneSaved;
        public UnityEvent<SceneData> OnSceneLoaded => onSceneLoaded;
        public UnityEvent<string> OnSceneDeleted => onSceneDeleted;
        public UnityEvent<string> OnError => onError;

        /// <summary>Sahnelerin kaydedildiği dizin yolu.</summary>
        public string ScenesDirectory => _scenesDirectory;

        private void Awake()
        {
            _scenesDirectory = Path.Combine(Application.persistentDataPath, scenesSubfolder);
            CreateDirectoryIfNotExists(_scenesDirectory);
        }

        private void OnDestroy()
        {
            StopAutoSave();
        }

        // ================================================================
        // Kaydetme
        // ================================================================

        /// <summary>
        /// Sahne verisini JSON dosyası olarak kaydeder.
        /// </summary>
        /// <param name="data">Kaydedilecek sahne verisi.</param>
        /// <returns>Başarılı ise true.</returns>
        public bool SaveScene(SceneData data)
        {
            if (data == null)
            {
                ReportError("Kaydedilecek veri null.");
                return false;
            }

            try
            {
                data.MarkModified();
                string json = JsonConvert.SerializeObject(data, JsonSettings);
                string filePath = GetSceneFilePath(data.sceneId);

                CreateDirectoryIfNotExists(_scenesDirectory);
                File.WriteAllText(filePath, json);

                onSceneSaved?.Invoke(data.sceneId);
                Debug.Log($"[SaveLoadManager] Sahne kaydedildi: {data.sceneName} → {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                ReportError($"Kayıt hatası: {ex.Message}");
                return false;
            }
        }

        // ================================================================
        // Yükleme
        // ================================================================

        /// <summary>
        /// Sahne verisini dosyadan yükler.
        /// </summary>
        /// <param name="sceneId">Yüklenecek sahnenin kimliği.</param>
        /// <returns>Yüklenen SceneData, başarısızsa null.</returns>
        public SceneData LoadScene(string sceneId)
        {
            if (string.IsNullOrEmpty(sceneId))
            {
                ReportError("Scene ID boş olamaz.");
                return null;
            }

            try
            {
                string filePath = GetSceneFilePath(sceneId);

                if (!File.Exists(filePath))
                {
                    ReportError($"Sahne dosyası bulunamadı: {sceneId}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                SceneData data = JsonConvert.DeserializeObject<SceneData>(json, JsonSettings);

                // Sürüm kontrolü
                if (data.version > SceneData.CURRENT_VERSION)
                {
                    Debug.LogWarning($"[SaveLoadManager] Sahne sürümü ({data.version}) " +
                                     $"uygulamadan yeni ({SceneData.CURRENT_VERSION}). " +
                                     "Bazı özellikler eksik olabilir.");
                }

                // Eski sürümlerden migration
                MigrateIfNeeded(data);

                onSceneLoaded?.Invoke(data);
                Debug.Log($"[SaveLoadManager] Sahne yüklendi: {data.sceneName}");
                return data;
            }
            catch (Exception ex)
            {
                ReportError($"Yükleme hatası: {ex.Message}");
                return null;
            }
        }

        // ================================================================
        // Silme
        // ================================================================

        /// <summary>
        /// Kaydedilmiş bir sahneyi siler.
        /// </summary>
        /// <param name="sceneId">Silinecek sahnenin kimliği.</param>
        /// <returns>Başarılı ise true.</returns>
        public bool DeleteScene(string sceneId)
        {
            try
            {
                string filePath = GetSceneFilePath(sceneId);

                if (!File.Exists(filePath))
                {
                    ReportError($"Silinecek sahne bulunamadı: {sceneId}");
                    return false;
                }

                File.Delete(filePath);

                // Thumbnail da varsa sil
                string thumbDir = Path.Combine(_scenesDirectory, "Thumbnails");
                string thumbPath = Path.Combine(thumbDir, $"{sceneId}.png");
                if (File.Exists(thumbPath))
                {
                    File.Delete(thumbPath);
                }

                onSceneDeleted?.Invoke(sceneId);
                Debug.Log($"[SaveLoadManager] Sahne silindi: {sceneId}");
                return true;
            }
            catch (Exception ex)
            {
                ReportError($"Silme hatası: {ex.Message}");
                return false;
            }
        }

        // ================================================================
        // Listeleme
        // ================================================================

        /// <summary>
        /// Kayıtlı tüm sahnelerin metadata listesini döndürür.
        /// Tam veri yüklemeden hafif bir liste sağlar.
        /// </summary>
        public List<SceneMetadata> GetAllSceneMetadata()
        {
            var metadataList = new List<SceneMetadata>();

            try
            {
                if (!Directory.Exists(_scenesDirectory))
                    return metadataList;

                string[] files = Directory.GetFiles(_scenesDirectory, "*.json");

                foreach (string file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        SceneData data = JsonConvert.DeserializeObject<SceneData>(json, JsonSettings);
                        if (data != null)
                        {
                            metadataList.Add(data.ToMetadata());
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[SaveLoadManager] Metadata okunamadı ({Path.GetFileName(file)}): {ex.Message}");
                    }
                }

                // Son değiştirilene göre sırala (en yeni önce)
                metadataList.Sort((a, b) => b.LastModifiedDateTime.CompareTo(a.LastModifiedDateTime));
            }
            catch (Exception ex)
            {
                ReportError($"Listeleme hatası: {ex.Message}");
            }

            return metadataList;
        }

        /// <summary>Kayıtlı sahne sayısını döndürür.</summary>
        public int GetSceneCount()
        {
            if (!Directory.Exists(_scenesDirectory)) return 0;
            return Directory.GetFiles(_scenesDirectory, "*.json").Length;
        }

        /// <summary>Belirtilen ID'li sahne mevcut mu?</summary>
        public bool SceneExists(string sceneId)
        {
            return File.Exists(GetSceneFilePath(sceneId));
        }

        // ================================================================
        // Dışa / İçe Aktarma
        // ================================================================

        /// <summary>
        /// Sahneyi belirtilen yola dışa aktarır.
        /// </summary>
        public bool ExportScene(string sceneId, string exportPath)
        {
            try
            {
                string sourcePath = GetSceneFilePath(sceneId);
                if (!File.Exists(sourcePath))
                {
                    ReportError($"Dışa aktarılacak sahne bulunamadı: {sceneId}");
                    return false;
                }

                string exportDir = Path.GetDirectoryName(exportPath);
                if (!string.IsNullOrEmpty(exportDir))
                    CreateDirectoryIfNotExists(exportDir);

                File.Copy(sourcePath, exportPath, overwrite: true);
                Debug.Log($"[SaveLoadManager] Sahne dışa aktarıldı: {exportPath}");
                return true;
            }
            catch (Exception ex)
            {
                ReportError($"Dışa aktarma hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Dış dosyadan sahne içe aktarır.
        /// </summary>
        public SceneData ImportScene(string importPath)
        {
            try
            {
                if (!File.Exists(importPath))
                {
                    ReportError($"İçe aktarılacak dosya bulunamadı: {importPath}");
                    return null;
                }

                string json = File.ReadAllText(importPath);
                SceneData data = JsonConvert.DeserializeObject<SceneData>(json, JsonSettings);

                if (data == null)
                {
                    ReportError("Geçersiz sahne dosyası.");
                    return null;
                }

                // Çakışma kontrolü — aynı ID varsa yeni ID ata
                if (SceneExists(data.sceneId))
                {
                    data.sceneId = Guid.NewGuid().ToString();
                    data.sceneName += " (İçe Aktarılan)";
                }

                // Kaydet
                SaveScene(data);
                Debug.Log($"[SaveLoadManager] Sahne içe aktarıldı: {data.sceneName}");
                return data;
            }
            catch (Exception ex)
            {
                ReportError($"İçe aktarma hatası: {ex.Message}");
                return null;
            }
        }

        // ================================================================
        // Otomatik Kayıt
        // ================================================================

        /// <summary>
        /// Otomatik kayıt başlatır. Belirli aralıklarla sahneyi kaydeder.
        /// </summary>
        public void StartAutoSave(SceneData data)
        {
            _currentAutoSaveData = data;

            if (!enableAutoSave) return;

            StopAutoSave();
            _autoSaveCoroutine = StartCoroutine(AutoSaveCoroutine());
            Debug.Log($"[SaveLoadManager] Otomatik kayıt başladı (her {autoSaveIntervalSeconds}s).");
        }

        /// <summary>Otomatik kaydı durdurur.</summary>
        public void StopAutoSave()
        {
            if (_autoSaveCoroutine != null)
            {
                StopCoroutine(_autoSaveCoroutine);
                _autoSaveCoroutine = null;
                Debug.Log("[SaveLoadManager] Otomatik kayıt durduruldu.");
            }
        }

        /// <summary>Otomatik kayıt verisini günceller (yeni referans atar).</summary>
        public void UpdateAutoSaveData(SceneData data)
        {
            _currentAutoSaveData = data;
        }

        private IEnumerator AutoSaveCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(autoSaveIntervalSeconds);

                if (_currentAutoSaveData != null)
                {
                    SaveScene(_currentAutoSaveData);
                    Debug.Log("[SaveLoadManager] Otomatik kayıt yapıldı.");
                }
            }
        }

        // ================================================================
        // Yardımcı Metodlar
        // ================================================================

        /// <summary>Sahne dosyasının tam yolunu döndürür.</summary>
        private string GetSceneFilePath(string sceneId)
        {
            return Path.Combine(_scenesDirectory, $"{sceneId}.json");
        }

        /// <summary>Dizin yoksa oluşturur.</summary>
        private void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>Sürüm migration işlemi.</summary>
        private void MigrateIfNeeded(SceneData data)
        {
            if (data.version < SceneData.CURRENT_VERSION)
            {
                // Gelecek sürüm migration'ları buraya eklenecek
                // Örnek:
                // if (data.version < 2) { MigrateV1ToV2(data); }

                data.version = SceneData.CURRENT_VERSION;
                Debug.Log($"[SaveLoadManager] Sahne sürümü güncellendi: v{data.version}");
            }
        }

        /// <summary>Hata raporla.</summary>
        private void ReportError(string message)
        {
            Debug.LogError($"[SaveLoadManager] {message}");
            onError?.Invoke(message);
        }
    }
}
