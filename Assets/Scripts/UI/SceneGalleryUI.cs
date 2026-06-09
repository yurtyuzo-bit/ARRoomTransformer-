using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace ARRoomTransformer
{
    /// <summary>
    /// Kaydedilmiş sahneleri listeleyen ve yükleme/silme işlemlerini
    /// sağlayan sahne galerisi paneli.
    /// </summary>
    public class SceneGalleryUI : MonoBehaviour
    {
        [Header("UI Referansları")]
        [SerializeField] private Transform contentParent;
        [SerializeField] private GameObject sceneCardPrefab;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI emptyStateText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button refreshButton;

        [Header("Dialog")]
        [SerializeField] private GameObject deleteConfirmDialog;
        [SerializeField] private TextMeshProUGUI deleteConfirmText;
        [SerializeField] private Button deleteConfirmYes;
        [SerializeField] private Button deleteConfirmNo;

        private SaveLoadManager _saveLoadManager;
        private List<SceneMetadata> _scenes;
        private string _pendingDeleteId;

        // Events
        public event System.Action<string> OnSceneSelected;

        private void Start()
        {
            _saveLoadManager = FindAnyObjectByType<SaveLoadManager>();

            if (closeButton != null)
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));

            if (refreshButton != null)
                refreshButton.onClick.AddListener(RefreshList);

            if (deleteConfirmYes != null)
                deleteConfirmYes.onClick.AddListener(ConfirmDelete);

            if (deleteConfirmNo != null)
                deleteConfirmNo.onClick.AddListener(CancelDelete);

            if (deleteConfirmDialog != null)
                deleteConfirmDialog.SetActive(false);
        }

        private void OnEnable()
        {
            RefreshList();
        }

        /// <summary>Sahne listesini yeniler.</summary>
        public void RefreshList()
        {
            if (_saveLoadManager == null) return;

            _scenes = _saveLoadManager.GetAllSceneMetadata();
            ClearList();

            if (_scenes.Count == 0)
            {
                if (emptyStateText != null)
                {
                    emptyStateText.gameObject.SetActive(true);
                    emptyStateText.text = "Henüz kaydedilmiş sahne yok.\nYeni bir tarama başlatın!";
                }
                return;
            }

            if (emptyStateText != null)
                emptyStateText.gameObject.SetActive(false);

            if (titleText != null)
                titleText.text = $"Kaydedilen Sahneler ({_scenes.Count})";

            foreach (var scene in _scenes)
            {
                CreateSceneCard(scene);
            }
        }

        private void CreateSceneCard(SceneMetadata metadata)
        {
            if (contentParent == null) return;

            GameObject card;
            if (sceneCardPrefab != null)
            {
                card = Instantiate(sceneCardPrefab, contentParent);
            }
            else
            {
                // Prefab yoksa basit bir card oluştur
                card = CreateDefaultCard(metadata);
            }

            // Card'daki metinleri doldur
            var texts = card.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 1) texts[0].text = metadata.sceneName;
            if (texts.Length >= 2) texts[1].text = $"{metadata.assetCount} asset • {metadata.themeName}";
            if (texts.Length >= 3) texts[2].text = metadata.LastModifiedDateTime.ToString("dd MMM yyyy HH:mm");

            // Yükle butonu
            var loadButton = card.GetComponentInChildren<Button>();
            if (loadButton != null)
            {
                string sceneId = metadata.sceneId;
                loadButton.onClick.AddListener(() => LoadScene(sceneId));
            }
        }

        private GameObject CreateDefaultCard(SceneMetadata metadata)
        {
            var card = new GameObject($"SceneCard_{metadata.sceneName}");
            card.transform.SetParent(contentParent, false);

            var rect = card.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 100);

            var layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(15, 15, 10, 10);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleLeft;

            // Sahne bilgi alanı
            var infoGO = new GameObject("Info");
            infoGO.transform.SetParent(card.transform, false);
            var infoRect = infoGO.AddComponent<RectTransform>();
            var infoLayout = infoGO.AddComponent<VerticalLayoutGroup>();
            infoLayout.spacing = 4;
            var infoFitter = infoGO.AddComponent<LayoutElement>();
            infoFitter.flexibleWidth = 1;

            // Başlık
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(infoGO.transform, false);
            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text = metadata.sceneName;
            titleTMP.fontSize = 18;
            titleTMP.fontStyle = FontStyles.Bold;

            // Detay
            var detailGO = new GameObject("Detail");
            detailGO.transform.SetParent(infoGO.transform, false);
            var detailTMP = detailGO.AddComponent<TextMeshProUGUI>();
            detailTMP.text = $"{metadata.assetCount} asset • {metadata.themeName}";
            detailTMP.fontSize = 14;
            detailTMP.color = new Color(0.6f, 0.6f, 0.6f);

            // Tarih
            var dateGO = new GameObject("Date");
            dateGO.transform.SetParent(infoGO.transform, false);
            var dateTMP = dateGO.AddComponent<TextMeshProUGUI>();
            dateTMP.text = metadata.LastModifiedDateTime.ToString("dd MMM yyyy HH:mm");
            dateTMP.fontSize = 12;
            dateTMP.color = new Color(0.5f, 0.5f, 0.5f);

            // Yükle butonu
            var btnGO = new GameObject("LoadButton");
            btnGO.transform.SetParent(card.transform, false);
            var btnRect = btnGO.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(80, 40);
            var btnLayout = btnGO.AddComponent<LayoutElement>();
            btnLayout.minWidth = 80;
            var btnImage = btnGO.AddComponent<Image>();
            btnImage.color = new Color(0.2f, 0.6f, 1f);
            var btn = btnGO.AddComponent<Button>();

            var btnTextGO = new GameObject("Text");
            btnTextGO.transform.SetParent(btnGO.transform, false);
            var btnTMP = btnTextGO.AddComponent<TextMeshProUGUI>();
            btnTMP.text = "Yükle";
            btnTMP.fontSize = 14;
            btnTMP.alignment = TextAlignmentOptions.Center;
            var btnTextRect = btnTextGO.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;

            string sceneId = metadata.sceneId;
            btn.onClick.AddListener(() => LoadScene(sceneId));

            // Arka plan
            var bgImage = card.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.18f, 0.9f);

            return card;
        }

        private void LoadScene(string sceneId)
        {
            HapticFeedback.Medium();
            OnSceneSelected?.Invoke(sceneId);
            Debug.Log($"[SceneGalleryUI] Sahne yükleniyor: {sceneId}");
        }

        /// <summary>Sahne silme dialogunu gösterir.</summary>
        public void ShowDeleteConfirm(string sceneId, string sceneName)
        {
            _pendingDeleteId = sceneId;

            if (deleteConfirmDialog != null)
            {
                deleteConfirmDialog.SetActive(true);
                if (deleteConfirmText != null)
                    deleteConfirmText.text = $"\"{sceneName}\" sahnesini silmek istediğinize emin misiniz?";
            }
        }

        private void ConfirmDelete()
        {
            if (_saveLoadManager != null && !string.IsNullOrEmpty(_pendingDeleteId))
            {
                _saveLoadManager.DeleteScene(_pendingDeleteId);
                HapticFeedback.Heavy();
                RefreshList();
            }

            if (deleteConfirmDialog != null)
                deleteConfirmDialog.SetActive(false);

            _pendingDeleteId = null;
        }

        private void CancelDelete()
        {
            if (deleteConfirmDialog != null)
                deleteConfirmDialog.SetActive(false);
            _pendingDeleteId = null;
        }

        private void ClearList()
        {
            if (contentParent == null) return;
            for (int i = contentParent.childCount - 1; i >= 0; i--)
            {
                Destroy(contentParent.GetChild(i).gameObject);
            }
        }
    }
}
