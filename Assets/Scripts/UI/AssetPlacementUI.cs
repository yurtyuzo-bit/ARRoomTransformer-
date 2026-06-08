using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace ARRoomTransformer
{


    /// <summary>
    /// Dönüşüm modlarını tanımlar.
    /// Seçili nesne üzerinde hangi dönüşümün uygulanacağını belirler.
    /// </summary>
    public enum TransformMode
    {
        /// <summary>Taşıma modu.</summary>
        Move,
        /// <summary>Döndürme modu.</summary>
        Rotate,
        /// <summary>Ölçekleme modu.</summary>
        Scale
    }

    /// <summary>
    /// Katalog öğesi bilgilerini taşıyan veri yapısı.
    /// Inspector'da veya kod ile doldurulur.
    /// </summary>
    [Serializable]
    public class AssetCatalogItem
    {
        /// <summary>Varlık benzersiz kimliği.</summary>
        [Tooltip("Varlık benzersiz kimliği.")]
        public string AssetId;

        /// <summary>Varlık görünen adı.</summary>
        [Tooltip("Varlık görünen adı.")]
        public string DisplayName;

        /// <summary>Varlık kategorisi.</summary>
        [Tooltip("Varlık kategorisi.")]
        public AssetCategory Category;

        /// <summary>Katalog küçük resmi (thumbnail).</summary>
        [Tooltip("Katalog küçük resmi.")]
        public Sprite Thumbnail;

        /// <summary>Yerleştirilecek prefab referansı.</summary>
        [Tooltip("Yerleştirilecek prefab.")]
        public GameObject Prefab;
    }

    /// <summary>
    /// Varlık seçimi ve yerleştirme panelini yönetir.
    /// Kaydırılabilir katalog, kategori filtreleri, dönüşüm modları
    /// ve seçili nesne özellikleri panelini içerir.
    /// </summary>
    public class AssetPlacementUI : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Katalog")]
        [SerializeField, Tooltip("Varlık kataloğu listesi.")]
        private List<AssetCatalogItem> _catalog = new List<AssetCatalogItem>();

        [SerializeField, Tooltip("Katalog ScrollRect bileşeni.")]
        private ScrollRect _catalogScrollRect;

        [SerializeField, Tooltip("Katalog öğesi prefab'ı (buton + ikon + isim).")]
        private GameObject _catalogItemPrefab;

        [SerializeField, Tooltip("Katalog öğelerinin yerleştirileceği içerik alanı (Content).")]
        private RectTransform _catalogContent;

        [Header("Kategori Filtreleri")]
        [SerializeField, Tooltip("'Tümü' filtre sekmesi.")]
        private Toggle _filterAll;

        [SerializeField, Tooltip("'Mobilya' filtre sekmesi.")]
        private Toggle _filterFurniture;

        [SerializeField, Tooltip("'Dekorasyon' filtre sekmesi.")]
        private Toggle _filterDecoration;

        [SerializeField, Tooltip("'Yapısal' filtre sekmesi.")]
        private Toggle _filterStructural;

        [SerializeField, Tooltip("'Aydınlatma' filtre sekmesi.")]
        private Toggle _filterLighting;

        [SerializeField, Tooltip("Filtre toggle grubu.")]
        private ToggleGroup _filterToggleGroup;

        [Header("Seçili Varlık Bilgisi")]
        [SerializeField, Tooltip("Seçili varlık adı metni.")]
        private TextMeshProUGUI _selectedAssetNameText;

        [SerializeField, Tooltip("Seçili varlık küçük resmi.")]
        private Image _selectedAssetThumbnail;

        [SerializeField, Tooltip("Seçili varlık vurgu çerçevesi.")]
        private Image _selectionHighlight;

        [Header("Dönüşüm Kontrolleri")]
        [SerializeField, Tooltip("Taşıma modu butonu.")]
        private Toggle _moveToggle;

        [SerializeField, Tooltip("Döndürme modu butonu.")]
        private Toggle _rotateToggle;

        [SerializeField, Tooltip("Ölçekleme modu butonu.")]
        private Toggle _scaleToggle;

        [SerializeField, Tooltip("Dönüşüm modu toggle grubu.")]
        private ToggleGroup _transformToggleGroup;

        [Header("İşlem Butonları")]
        [SerializeField, Tooltip("Seçili yerleştirilmiş varlığı sil butonu.")]
        private Button _deleteButton;

        [SerializeField, Tooltip("Geri dön butonu.")]
        private Button _backButton;

        [SerializeField, Tooltip("Kayıt moduna geç butonu.")]
        private Button _recordingButton;

        [Header("Özellikler Paneli")]
        [SerializeField, Tooltip("Özellikler paneli CanvasGroup (seçili nesne olmadığında gizlenir).")]
        private CanvasGroup _propertiesPanel;

        [SerializeField, Tooltip("Konum X değeri.")]
        private TMP_InputField _posXInput;

        [SerializeField, Tooltip("Konum Y değeri.")]
        private TMP_InputField _posYInput;

        [SerializeField, Tooltip("Konum Z değeri.")]
        private TMP_InputField _posZInput;

        [SerializeField, Tooltip("Döndürme Y değeri (derece).")]
        private TMP_InputField _rotYInput;

        [SerializeField, Tooltip("Ölçek uniform değeri.")]
        private Slider _scaleSlider;

        [SerializeField, Tooltip("Ölçek değeri metni.")]
        private TextMeshProUGUI _scaleValueText;

        [Header("Renkler")]
        [SerializeField, Tooltip("Seçili öğe vurgu rengi.")]
        private Color _highlightColor = new Color(0.3f, 0.7f, 1f, 1f);

        [SerializeField, Tooltip("Normal öğe rengi.")]
        private Color _normalColor = new Color(1f, 1f, 1f, 0.6f);

        #endregion

        #region Events

        /// <summary>Katalogdan bir varlık seçildiğinde tetiklenir. (AssetCatalogItem)</summary>
        [Header("Olaylar")]
        public UnityEvent<AssetCatalogItem> OnAssetSelected;

        /// <summary>Yerleştirilmiş varlık silme isteğinde tetiklenir.</summary>
        public UnityEvent OnDeletePlacedAsset;

        /// <summary>Dönüşüm modu değiştiğinde tetiklenir. (TransformMode)</summary>
        public UnityEvent<TransformMode> OnTransformModeChanged;

        /// <summary>Geri butonuna basıldığında tetiklenir.</summary>
        public UnityEvent OnBackPressed;

        /// <summary>Kayıt moduna geçiş istendiğinde tetiklenir.</summary>
        public UnityEvent OnRecordingRequested;

        /// <summary>Özellikler panelinden değer değiştiğinde tetiklenir. (position, rotationY, scale)</summary>
        public UnityEvent<Vector3, float, float> OnPropertiesChanged;

        #endregion

        #region Private State

        private AssetCategory _currentFilter = AssetCategory.All;
        private TransformMode _currentTransformMode = TransformMode.Move;
        private AssetCatalogItem _selectedCatalogItem;
        private readonly List<GameObject> _spawnedCatalogItems = new List<GameObject>();
        private bool _hasSelectedPlacedObject;

        #endregion

        #region Properties

        /// <summary>Mevcut kategori filtresi.</summary>
        public AssetCategory CurrentFilter => _currentFilter;

        /// <summary>Mevcut dönüşüm modu.</summary>
        public TransformMode CurrentTransformMode => _currentTransformMode;

        /// <summary>Seçili katalog öğesi.</summary>
        public AssetCatalogItem SelectedCatalogItem => _selectedCatalogItem;

        /// <summary>Varlık kataloğuna erişim.</summary>
        public IReadOnlyList<AssetCatalogItem> Catalog => _catalog.AsReadOnly();

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            BindFilterToggles();
            BindTransformToggles();
            BindButtons();
            BindPropertyInputs();

            PopulateCatalog();
            UpdatePropertiesPanelVisibility();
        }

        private void OnDisable()
        {
            UnbindFilterToggles();
            UnbindTransformToggles();
            UnbindButtons();
            UnbindPropertyInputs();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Katalog öğelerini yeniler (filtre veya katalog değişikliğinde çağrılır).
        /// </summary>
        public void PopulateCatalog()
        {
            ClearSpawnedItems();

            if (_catalogItemPrefab == null || _catalogContent == null)
            {
                Debug.LogWarning("[AssetPlacementUI] Katalog prefab veya content referansı atanmamış.");
                return;
            }

            foreach (var item in _catalog)
            {
                if (_currentFilter != AssetCategory.All && item.Category != _currentFilter)
                    continue;

                GameObject go = Instantiate(_catalogItemPrefab, _catalogContent);
                _spawnedCatalogItems.Add(go);

                // Küçük resim ayarla
                Image thumbnail = go.GetComponentInChildren<Image>();
                if (thumbnail != null && item.Thumbnail != null)
                {
                    thumbnail.sprite = item.Thumbnail;
                }

                // İsim ayarla
                TextMeshProUGUI nameText = go.GetComponentInChildren<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = item.DisplayName;
                }

                // Buton tıklama olayı
                Button btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    AssetCatalogItem capturedItem = item;
                    btn.onClick.AddListener(() => SelectCatalogItem(capturedItem));
                }
            }
        }

        /// <summary>
        /// Katalog öğesini seçer ve UI'ı günceller.
        /// </summary>
        /// <param name="item">Seçilecek katalog öğesi.</param>
        public void SelectCatalogItem(AssetCatalogItem item)
        {
            _selectedCatalogItem = item;

            if (_selectedAssetNameText != null)
            {
                _selectedAssetNameText.text = item?.DisplayName ?? "";
            }

            if (_selectedAssetThumbnail != null)
            {
                _selectedAssetThumbnail.sprite = item?.Thumbnail;
                _selectedAssetThumbnail.enabled = item?.Thumbnail != null;
            }

            if (_selectionHighlight != null)
            {
                _selectionHighlight.color = item != null ? _highlightColor : _normalColor;
                _selectionHighlight.enabled = item != null;
            }

            Debug.Log($"[AssetPlacementUI] Varlık seçildi: {item?.DisplayName ?? "Yok"}");
            OnAssetSelected?.Invoke(item);
        }

        /// <summary>
        /// Yerleştirilmiş bir nesne seçildiğinde çağrılır.
        /// Özellikler panelini gösterir ve değerleri doldurur.
        /// </summary>
        /// <param name="position">Nesnenin pozisyonu.</param>
        /// <param name="rotationY">Y eksenindeki dönüşü (derece).</param>
        /// <param name="scale">Uniform ölçek değeri.</param>
        public void ShowPlacedObjectProperties(Vector3 position, float rotationY, float scale)
        {
            _hasSelectedPlacedObject = true;
            UpdatePropertiesPanelVisibility();

            if (_posXInput != null) _posXInput.text = position.x.ToString("F3");
            if (_posYInput != null) _posYInput.text = position.y.ToString("F3");
            if (_posZInput != null) _posZInput.text = position.z.ToString("F3");
            if (_rotYInput != null) _rotYInput.text = rotationY.ToString("F1");

            if (_scaleSlider != null)
            {
                _scaleSlider.SetValueWithoutNotify(scale);
            }

            if (_scaleValueText != null)
            {
                _scaleValueText.text = $"x{scale:F2}";
            }
        }

        /// <summary>
        /// Yerleştirilmiş nesne seçimini kaldırır ve özellikler panelini gizler.
        /// </summary>
        public void ClearPlacedObjectSelection()
        {
            _hasSelectedPlacedObject = false;
            UpdatePropertiesPanelVisibility();
        }

        /// <summary>
        /// Dönüşüm modunu programatik olarak değiştirir.
        /// </summary>
        /// <param name="mode">Yeni dönüşüm modu.</param>
        public void SetTransformMode(TransformMode mode)
        {
            _currentTransformMode = mode;

            // Toggle'ları güncelle (programatik)
            switch (mode)
            {
                case TransformMode.Move:
                    if (_moveToggle != null) _moveToggle.SetIsOnWithoutNotify(true);
                    break;
                case TransformMode.Rotate:
                    if (_rotateToggle != null) _rotateToggle.SetIsOnWithoutNotify(true);
                    break;
                case TransformMode.Scale:
                    if (_scaleToggle != null) _scaleToggle.SetIsOnWithoutNotify(true);
                    break;
            }

            OnTransformModeChanged?.Invoke(mode);
        }

        /// <summary>
        /// Kataloga yeni öğe ekler ve listeyi yeniler.
        /// </summary>
        /// <param name="item">Eklenecek katalog öğesi.</param>
        public void AddCatalogItem(AssetCatalogItem item)
        {
            if (item == null)
            {
                Debug.LogWarning("[AssetPlacementUI] Eklenecek öğe null.");
                return;
            }

            _catalog.Add(item);
            PopulateCatalog();
        }

        /// <summary>
        /// Kategori filtresini programatik olarak değiştirir.
        /// </summary>
        /// <param name="category">Yeni filtre kategorisi.</param>
        public void SetFilter(AssetCategory category)
        {
            _currentFilter = category;
            PopulateCatalog();
        }

        #endregion

        #region Filter Handlers

        private void BindFilterToggles()
        {
            if (_filterAll != null) _filterAll.onValueChanged.AddListener(v => { if (v) ApplyFilter(AssetCategory.All); });
            if (_filterFurniture != null) _filterFurniture.onValueChanged.AddListener(v => { if (v) ApplyFilter(AssetCategory.Furniture); });
            if (_filterDecoration != null) _filterDecoration.onValueChanged.AddListener(v => { if (v) ApplyFilter(AssetCategory.Decoration); });
            if (_filterStructural != null) _filterStructural.onValueChanged.AddListener(v => { if (v) ApplyFilter(AssetCategory.Structural); });
            if (_filterLighting != null) _filterLighting.onValueChanged.AddListener(v => { if (v) ApplyFilter(AssetCategory.Lighting); });
        }

        private void UnbindFilterToggles()
        {
            if (_filterAll != null) _filterAll.onValueChanged.RemoveAllListeners();
            if (_filterFurniture != null) _filterFurniture.onValueChanged.RemoveAllListeners();
            if (_filterDecoration != null) _filterDecoration.onValueChanged.RemoveAllListeners();
            if (_filterStructural != null) _filterStructural.onValueChanged.RemoveAllListeners();
            if (_filterLighting != null) _filterLighting.onValueChanged.RemoveAllListeners();
        }

        private void ApplyFilter(AssetCategory category)
        {
            _currentFilter = category;
            PopulateCatalog();
            Debug.Log($"[AssetPlacementUI] Filtre uygulandı: {category}");
        }

        #endregion

        #region Transform Mode Handlers

        private void BindTransformToggles()
        {
            if (_moveToggle != null) _moveToggle.onValueChanged.AddListener(v => { if (v) SetTransformModeInternal(TransformMode.Move); });
            if (_rotateToggle != null) _rotateToggle.onValueChanged.AddListener(v => { if (v) SetTransformModeInternal(TransformMode.Rotate); });
            if (_scaleToggle != null) _scaleToggle.onValueChanged.AddListener(v => { if (v) SetTransformModeInternal(TransformMode.Scale); });
        }

        private void UnbindTransformToggles()
        {
            if (_moveToggle != null) _moveToggle.onValueChanged.RemoveAllListeners();
            if (_rotateToggle != null) _rotateToggle.onValueChanged.RemoveAllListeners();
            if (_scaleToggle != null) _scaleToggle.onValueChanged.RemoveAllListeners();
        }

        private void SetTransformModeInternal(TransformMode mode)
        {
            _currentTransformMode = mode;
            OnTransformModeChanged?.Invoke(mode);
            Debug.Log($"[AssetPlacementUI] Dönüşüm modu: {mode}");
        }

        #endregion

        #region Button Handlers

        private void BindButtons()
        {
            if (_deleteButton != null) _deleteButton.onClick.AddListener(HandleDelete);
            if (_backButton != null) _backButton.onClick.AddListener(HandleBack);
            if (_recordingButton != null) _recordingButton.onClick.AddListener(HandleRecording);
        }

        private void UnbindButtons()
        {
            if (_deleteButton != null) _deleteButton.onClick.RemoveListener(HandleDelete);
            if (_backButton != null) _backButton.onClick.RemoveListener(HandleBack);
            if (_recordingButton != null) _recordingButton.onClick.RemoveListener(HandleRecording);
        }

        private void HandleDelete()
        {
            if (!_hasSelectedPlacedObject)
            {
                Debug.LogWarning("[AssetPlacementUI] Silinecek seçili nesne yok.");
                return;
            }

            Debug.Log("[AssetPlacementUI] Seçili varlık siliniyor.");
            OnDeletePlacedAsset?.Invoke();
            ClearPlacedObjectSelection();
        }

        private void HandleBack()
        {
            Debug.Log("[AssetPlacementUI] Geri butonuna basıldı.");
            OnBackPressed?.Invoke();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetState(AppState.MainMenu);
            }
        }

        private void HandleRecording()
        {
            Debug.Log("[AssetPlacementUI] Kayıt moduna geçiş istendi.");
            OnRecordingRequested?.Invoke();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetState(AppState.Recording);
            }
        }

        #endregion

        #region Property Input Handlers

        private void BindPropertyInputs()
        {
            if (_posXInput != null) _posXInput.onEndEdit.AddListener(_ => EmitPropertyChanges());
            if (_posYInput != null) _posYInput.onEndEdit.AddListener(_ => EmitPropertyChanges());
            if (_posZInput != null) _posZInput.onEndEdit.AddListener(_ => EmitPropertyChanges());
            if (_rotYInput != null) _rotYInput.onEndEdit.AddListener(_ => EmitPropertyChanges());
            if (_scaleSlider != null) _scaleSlider.onValueChanged.AddListener(HandleScaleChange);
        }

        private void UnbindPropertyInputs()
        {
            if (_posXInput != null) _posXInput.onEndEdit.RemoveAllListeners();
            if (_posYInput != null) _posYInput.onEndEdit.RemoveAllListeners();
            if (_posZInput != null) _posZInput.onEndEdit.RemoveAllListeners();
            if (_rotYInput != null) _rotYInput.onEndEdit.RemoveAllListeners();
            if (_scaleSlider != null) _scaleSlider.onValueChanged.RemoveAllListeners();
        }

        private void HandleScaleChange(float value)
        {
            if (_scaleValueText != null)
            {
                _scaleValueText.text = $"x{value:F2}";
            }
            EmitPropertyChanges();
        }

        private void EmitPropertyChanges()
        {
            if (!_hasSelectedPlacedObject) return;

            float.TryParse(_posXInput?.text, out float px);
            float.TryParse(_posYInput?.text, out float py);
            float.TryParse(_posZInput?.text, out float pz);
            float.TryParse(_rotYInput?.text, out float ry);
            float scale = _scaleSlider != null ? _scaleSlider.value : 1f;

            OnPropertiesChanged?.Invoke(new Vector3(px, py, pz), ry, scale);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Özellikler panelinin görünürlüğünü günceller.
        /// </summary>
        private void UpdatePropertiesPanelVisibility()
        {
            if (_propertiesPanel != null)
            {
                _propertiesPanel.alpha = _hasSelectedPlacedObject ? 1f : 0f;
                _propertiesPanel.interactable = _hasSelectedPlacedObject;
                _propertiesPanel.blocksRaycasts = _hasSelectedPlacedObject;
            }

            if (_deleteButton != null)
            {
                _deleteButton.interactable = _hasSelectedPlacedObject;
            }
        }

        /// <summary>
        /// Oluşturulmuş katalog öğelerini temizler.
        /// </summary>
        private void ClearSpawnedItems()
        {
            foreach (var go in _spawnedCatalogItems)
            {
                if (go != null) Destroy(go);
            }
            _spawnedCatalogItems.Clear();
        }

        #endregion
    }
}
