using System.Collections.Generic;
using UnityEngine;

namespace ARRoomTransformer
{
    /// <summary>
    /// Sahne içindeki tüm yerleştirilmiş asset'lerin merkezi yönetimi.
    /// Seçim, silme, listeleme ve toplu işlemleri sağlar.
    /// </summary>
    public class PlacedAssetsManager : MonoBehaviour
    {
        [Header("Ayarlar")]
        [SerializeField] private Material selectionHighlightMaterial;
        [SerializeField] private Color selectionOutlineColor = new Color(0.2f, 0.8f, 1f, 1f);
        [SerializeField] private float selectionPulseSpeed = 2f;

        private readonly List<PlacedAssetInstance> _placedAssets = new List<PlacedAssetInstance>();
        private PlacedAssetInstance _selectedAsset;

        /// <summary>Yerleştirilmiş asset sayısı.</summary>
        public int Count => _placedAssets.Count;

        /// <summary>Seçili asset.</summary>
        public PlacedAssetInstance SelectedAsset => _selectedAsset;

        /// <summary>Seçili bir asset var mı?</summary>
        public bool HasSelection => _selectedAsset != null;

        /// <summary>Tüm yerleştirilmiş asset'lerin listesi (readonly).</summary>
        public IReadOnlyList<PlacedAssetInstance> PlacedAssets => _placedAssets;

        // Events
        public event System.Action<PlacedAssetInstance> OnAssetPlaced;
        public event System.Action<PlacedAssetInstance> OnAssetRemoved;
        public event System.Action<PlacedAssetInstance> OnAssetSelected;
        public event System.Action OnSelectionCleared;

        /// <summary>Yeni yerleştirilmiş asset kaydeder.</summary>
        public PlacedAssetInstance RegisterAsset(GameObject gameObject, string catalogId, string assetName)
        {
            var instance = new PlacedAssetInstance
            {
                instanceId = System.Guid.NewGuid().ToString(),
                catalogId = catalogId,
                assetName = assetName,
                gameObject = gameObject
            };

            gameObject.tag = Constants.TAG_PLACED_ASSET;
            _placedAssets.Add(instance);
            OnAssetPlaced?.Invoke(instance);

            Debug.Log($"[PlacedAssetsManager] Asset yerleştirildi: {assetName} (toplam: {_placedAssets.Count})");
            return instance;
        }

        /// <summary>Asset seçer ve görsel highlight uygular.</summary>
        public void SelectAsset(PlacedAssetInstance asset)
        {
            if (_selectedAsset == asset) return;

            // Önceki seçimi kaldır
            ClearSelection();

            _selectedAsset = asset;

            if (asset?.gameObject != null)
            {
                // Highlight efekti
                var highlight = asset.gameObject.GetOrAddComponent<SelectionHighlight>();
                highlight.SetColor(selectionOutlineColor);
                highlight.SetPulseSpeed(selectionPulseSpeed);
                highlight.Enable();
            }

            OnAssetSelected?.Invoke(asset);
        }

        /// <summary>GameObject referansı ile asset seçer.</summary>
        public void SelectAsset(GameObject go)
        {
            var instance = _placedAssets.Find(a => a.gameObject == go);
            if (instance != null) SelectAsset(instance);
        }

        /// <summary>Seçimi temizler.</summary>
        public void ClearSelection()
        {
            if (_selectedAsset?.gameObject != null)
            {
                var highlight = _selectedAsset.gameObject.GetComponent<SelectionHighlight>();
                if (highlight != null) highlight.Disable();
            }

            _selectedAsset = null;
            OnSelectionCleared?.Invoke();
        }

        /// <summary>Seçili asset'i siler.</summary>
        public bool DeleteSelected()
        {
            if (_selectedAsset == null) return false;
            return RemoveAsset(_selectedAsset);
        }

        /// <summary>Belirli bir asset'i siler.</summary>
        public bool RemoveAsset(PlacedAssetInstance asset)
        {
            if (asset == null) return false;

            if (_selectedAsset == asset) ClearSelection();

            _placedAssets.Remove(asset);

            if (asset.gameObject != null)
            {
                Destroy(asset.gameObject);
            }

            OnAssetRemoved?.Invoke(asset);
            Debug.Log($"[PlacedAssetsManager] Asset silindi: {asset.assetName} (kalan: {_placedAssets.Count})");
            return true;
        }

        /// <summary>Instance ID ile asset bulur.</summary>
        public PlacedAssetInstance FindById(string instanceId)
        {
            return _placedAssets.Find(a => a.instanceId == instanceId);
        }

        /// <summary>Tüm asset'leri siler.</summary>
        public void ClearAll()
        {
            ClearSelection();
            foreach (var asset in _placedAssets)
            {
                if (asset.gameObject != null) Destroy(asset.gameObject);
            }
            _placedAssets.Clear();
            Debug.Log("[PlacedAssetsManager] Tüm asset'ler silindi.");
        }

        /// <summary>Mevcut asset'leri PlacedAssetData listesine dönüştürür (kayıt için).</summary>
        public List<PlacedAssetData> ToDataList()
        {
            var dataList = new List<PlacedAssetData>();
            foreach (var asset in _placedAssets)
            {
                if (asset.gameObject == null) continue;

                var data = new PlacedAssetData(
                    asset.catalogId,
                    asset.assetName,
                    asset.gameObject.transform.position,
                    asset.gameObject.transform.rotation,
                    asset.gameObject.transform.localScale
                );
                data.instanceId = asset.instanceId;
                dataList.Add(data);
            }
            return dataList;
        }

        /// <summary>Kaydedilmiş verileri sahneye yükler.</summary>
        public void LoadFromDataList(List<PlacedAssetData> dataList, AssetCatalog catalog)
        {
            if (dataList == null || catalog == null) return;

            ClearAll();

            foreach (var data in dataList)
            {
                // Katalogdan prefab bul
                // Not: Bu basitleştirilmiş versiyon. Gerçek implementasyonda
                // AssetCatalog'dan prefab yüklenip instantiate edilir.
                Debug.Log($"[PlacedAssetsManager] Asset yükleniyor: {data.assetName} @ {(Vector3)data.position}");
            }
        }
    }

    /// <summary>
    /// Yerleştirilmiş bir asset instance'ının runtime verisi.
    /// </summary>
    public class PlacedAssetInstance
    {
        public string instanceId;
        public string catalogId;
        public string assetName;
        public GameObject gameObject;
    }

    /// <summary>
    /// Seçili obje üzerinde highlight/pulse efekti.
    /// </summary>
    public class SelectionHighlight : MonoBehaviour
    {
        private Color _color = Color.cyan;
        private float _pulseSpeed = 2f;
        private bool _isActive;
        private Renderer[] _renderers;
        private Dictionary<Renderer, Color[]> _originalColors = new Dictionary<Renderer, Color[]>();
        private float _timer;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            CacheOriginalColors();
        }

        private void Update()
        {
            if (!_isActive) return;

            _timer += Time.deltaTime * _pulseSpeed;
            float pulse = (Mathf.Sin(_timer) + 1f) * 0.5f; // 0-1
            float intensity = Mathf.Lerp(0.0f, 0.3f, pulse);

            foreach (var renderer in _renderers)
            {
                if (renderer == null) continue;
                foreach (var mat in renderer.materials)
                {
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", _color * intensity);
                    }
                }
            }
        }

        public void SetColor(Color color) => _color = color;
        public void SetPulseSpeed(float speed) => _pulseSpeed = speed;

        public void Enable()
        {
            _isActive = true;
            _timer = 0f;
        }

        public void Disable()
        {
            _isActive = false;
            RestoreOriginalColors();
        }

        private void CacheOriginalColors()
        {
            foreach (var renderer in _renderers)
            {
                if (renderer == null) continue;
                var colors = new Color[renderer.materials.Length];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (renderer.materials[i].HasProperty("_EmissionColor"))
                        colors[i] = renderer.materials[i].GetColor("_EmissionColor");
                }
                _originalColors[renderer] = colors;
            }
        }

        private void RestoreOriginalColors()
        {
            foreach (var kvp in _originalColors)
            {
                if (kvp.Key == null) continue;
                for (int i = 0; i < kvp.Key.materials.Length && i < kvp.Value.Length; i++)
                {
                    if (kvp.Key.materials[i].HasProperty("_EmissionColor"))
                    {
                        kvp.Key.materials[i].SetColor("_EmissionColor", kvp.Value[i]);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_isActive) RestoreOriginalColors();
        }
    }
}
