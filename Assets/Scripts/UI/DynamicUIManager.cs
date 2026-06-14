using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ARRoomTransformer
{
    /// <summary>
    /// Tamamen kod ile oluşturulan profesyonel AR arayüzü.
    /// Onboarding, kategori filtreli obje kütüphanesi, durum bazlı panel geçişleri içerir.
    /// </summary>
    public class DynamicUIManager : MonoBehaviour
    {
        // --- Manager referansları ---
        private AppManager appManager;
        private AssetPlacer assetPlacer;
        private ARVideoRecorder videoRecorder;

        // --- UI Kök ---
        private GameObject canvasObj;
        private Font defaultFont;

        // --- Paneller ---
        private CanvasGroup welcomePanel;
        private CanvasGroup scanPanel;
        private CanvasGroup placePanel;
        private CanvasGroup recordPanel;

        // --- Welcome ---
        private Text welcomeTitle;

        // --- Scan ---
        private Text scanStatusText;
        private GameObject crosshairObj;

        // --- Place ---
        private Text placePromptText;
        private Text placeCounterText;
        private Transform assetContentParent;
        private List<GameObject> assetCards = new List<GameObject>();
        private AssetCategory currentFilter = AssetCategory.All;
        private int selectedCardIndex = -1;
        private List<GameObject> filterButtons = new List<GameObject>();

        // --- Record ---
        private Text recordTimerText;
        private Image recordDot;
        private float recordStartTime;

        // --- Genel ---
        private Text promptText;
        private Coroutine promptCoroutine;
        private bool catalogLoaded = false;

        // Renkler
        private static readonly Color DarkBg = new Color(0.08f, 0.08f, 0.12f, 0.88f);
        private static readonly Color GreenAccent = new Color(0.30f, 0.69f, 0.31f, 1f);
        private static readonly Color RedAccent = new Color(0.96f, 0.26f, 0.21f, 1f);
        private static readonly Color YellowAccent = new Color(1f, 0.76f, 0.03f, 1f);
        private static readonly Color CardBg = new Color(0.15f, 0.15f, 0.20f, 0.95f);
        private static readonly Color CardSelected = new Color(0.20f, 0.50f, 0.25f, 0.95f);

        private void Start()
        {
            appManager = FindAnyObjectByType<AppManager>();
            assetPlacer = FindAnyObjectByType<AssetPlacer>();
            videoRecorder = FindAnyObjectByType<ARVideoRecorder>();

            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            EnsureEventSystem();
            BuildAllUI();

            // Durum değişikliklerini dinle
            if (appManager != null)
                appManager.StateChanged += OnStateChanged;

            // İlk panel: Hoş Geldin
            ShowPanel(AppState.Idle);

            // Katalog yüklenmesini bekle
            StartCoroutine(WaitForCatalog());
        }

        private void OnDestroy()
        {
            if (appManager != null)
                appManager.StateChanged -= OnStateChanged;
        }

        private IEnumerator WaitForCatalog()
        {
            float timeout = 5f;
            float elapsed = 0f;
            while (elapsed < timeout)
            {
                if (assetPlacer != null && assetPlacer.Catalog != null && assetPlacer.Catalog.Count > 0)
                {
                    PopulateAssetLibrary();
                    catalogLoaded = true;
                    Debug.Log($"[DynamicUI] Katalog yüklendi: {assetPlacer.Catalog.Count} öğe");
                    yield break;
                }
                yield return new WaitForSeconds(0.3f);
                elapsed += 0.3f;
            }
            Debug.LogWarning("[DynamicUI] Katalog yüklenemedi, zaman aşımı.");
        }

        // ================================================================
        // UI YAPISI
        // ================================================================

        private void BuildAllUI()
        {
            var existingCanvas = GameObject.Find("DynamicUICanvas");
            if (existingCanvas != null) Destroy(existingCanvas);

            // Ana Canvas
            canvasObj = new GameObject("DynamicUICanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            BuildWelcomePanel();
            BuildScanPanel();
            BuildPlacePanel();
            BuildRecordPanel();
            BuildPromptOverlay();
        }

        // --- HOŞ GELDİN PANELİ ---
        private void BuildWelcomePanel()
        {
            var panel = CreatePanel("WelcomePanel");
            welcomePanel = panel;

            // Koyu arka plan
            var bg = panel.GetComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

            // Başlık
            welcomeTitle = CreateText(panel.transform, "AR Oda Dönüştürücü", 64, TextAnchor.MiddleCenter,
                new Vector2(0, 0.75f), new Vector2(1, 0.9f), Color.white);
            AddOutline(welcomeTitle.gameObject);

            // 3 Adım
            string[] steps = { "1. Odayı Tara", "2. Eşya Yerleştir", "3. Kaydet & Paylaş" };
            string[] icons = { "◎", "▣", "◉" };
            Color[] colors = { GreenAccent, YellowAccent, RedAccent };

            for (int i = 0; i < 3; i++)
            {
                float yMin = 0.50f - i * 0.10f;
                float yMax = yMin + 0.08f;

                var icon = CreateText(panel.transform, icons[i], 52, TextAnchor.MiddleCenter,
                    new Vector2(0.1f, yMin), new Vector2(0.25f, yMax), colors[i]);
                AddOutline(icon.gameObject);

                var label = CreateText(panel.transform, steps[i], 42, TextAnchor.MiddleLeft,
                    new Vector2(0.28f, yMin), new Vector2(0.9f, yMax), Color.white);
                AddOutline(label.gameObject);
            }

            // BAŞLA Butonu
            var startBtn = CreateButtonUI("StartBtn", panel.transform,
                new Vector2(0.15f, 0.08f), new Vector2(0.85f, 0.18f),
                GreenAccent, "BAŞLA", 52);
            startBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (appManager != null) appManager.StartScanning();
            });
        }

        // --- TARAMA PANELİ ---
        private void BuildScanPanel()
        {
            var panel = CreatePanel("ScanPanel");
            scanPanel = panel;
            panel.alpha = 0;
            panel.gameObject.SetActive(false);

            // Üst durum çubuğu
            var topBar = CreateRect("ScanTopBar", panel.transform,
                new Vector2(0, 0.9f), new Vector2(1, 1f));
            topBar.gameObject.AddComponent<Image>().color = DarkBg;

            scanStatusText = CreateText(topBar.transform, "Zemine dokunarak 4 köşeyi işaretleyin",
                38, TextAnchor.MiddleCenter,
                new Vector2(0, 0), new Vector2(1, 1), YellowAccent);
            AddOutline(scanStatusText.gameObject);

            // Crosshair
            crosshairObj = new GameObject("Crosshair");
            crosshairObj.transform.SetParent(panel.transform, false);
            var chRect = crosshairObj.AddComponent<RectTransform>();
            chRect.anchorMin = new Vector2(0.5f, 0.5f);
            chRect.anchorMax = new Vector2(0.5f, 0.5f);
            chRect.sizeDelta = new Vector2(80, 80);
            var chText = crosshairObj.AddComponent<Text>();
            chText.text = "+";
            chText.font = defaultFont;
            chText.fontSize = 80;
            chText.alignment = TextAnchor.MiddleCenter;
            chText.color = RedAccent;
            AddOutline(crosshairObj);

            // Alt çubuk
            var bottomBar = CreateRect("ScanBottomBar", panel.transform,
                new Vector2(0, 0), new Vector2(1, 0.1f));
            bottomBar.gameObject.AddComponent<Image>().color = DarkBg;

            var resetBtn = CreateButtonUI("ResetBtn", bottomBar.transform,
                new Vector2(0.55f, 0.1f), new Vector2(0.95f, 0.9f),
                RedAccent, "Sıfırla", 36);
            resetBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                var boundary = FindAnyObjectByType<RoomBoundaryManager>();
                if (boundary != null) boundary.StartSetup();
                ShowPrompt("Köşeler sıfırlandı. Tekrar 4 köşeye dokunun.", 3f);
            });
        }

        // --- YERLEŞTİRME PANELİ ---
        private void BuildPlacePanel()
        {
            var panel = CreatePanel("PlacePanel");
            placePanel = panel;
            panel.alpha = 0;
            panel.gameObject.SetActive(false);

            // Üst bilgi çubuğu
            var topBar = CreateRect("PlaceTopBar", panel.transform,
                new Vector2(0, 0.92f), new Vector2(1, 1f));
            topBar.gameObject.AddComponent<Image>().color = DarkBg;

            placeCounterText = CreateText(topBar.transform, "Yerleştirilen: 0",
                32, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0), new Vector2(0.5f, 1), Color.white);
            AddOutline(placeCounterText.gameObject);

            // Kayıt butonu (sağ üst)
            var recBtn = CreateButtonUI("MiniRecBtn", topBar.transform,
                new Vector2(0.75f, 0.1f), new Vector2(0.97f, 0.9f),
                RedAccent, "● REC", 30);
            recBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (appManager != null) appManager.TransitionTo(AppState.Recording);
            });

            // Bilgi mesajı
            placePromptText = CreateText(panel.transform, "Aşağıdan bir eşya seçin, sonra yere dokunun",
                36, TextAnchor.MiddleCenter,
                new Vector2(0, 0.82f), new Vector2(1, 0.91f), Color.white);
            AddOutline(placePromptText.gameObject);

            // Kategori filtreleri
            var filterBar = CreateRect("FilterBar", panel.transform,
                new Vector2(0, 0.14f), new Vector2(1, 0.22f));
            filterBar.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.9f);

            string[] catNames = { "Tümü", "Mobilya", "Dekor", "Yapısal", "Işık" };
            AssetCategory[] cats = { AssetCategory.All, AssetCategory.Furniture, AssetCategory.Decoration, AssetCategory.Structural, AssetCategory.Lighting };

            for (int i = 0; i < catNames.Length; i++)
            {
                float xMin = i * 0.2f;
                float xMax = xMin + 0.2f;
                int catIndex = i;
                var catColor = (i == 0) ? GreenAccent : CardBg;

                var catBtn = CreateButtonUI($"Cat_{catNames[i]}", filterBar.transform,
                    new Vector2(xMin + 0.005f, 0.1f), new Vector2(xMax - 0.005f, 0.9f),
                    catColor, catNames[i], 28);
                filterButtons.Add(catBtn);
                catBtn.GetComponent<Button>().onClick.AddListener(() => OnCategorySelected(cats[catIndex], catIndex));
            }

            // Obje kütüphanesi (yatay scroll)
            var libraryBg = CreateRect("LibraryBg", panel.transform,
                new Vector2(0, 0), new Vector2(1, 0.14f));
            libraryBg.gameObject.AddComponent<Image>().color = DarkBg;

            var scrollObj = new GameObject("AssetScroll");
            scrollObj.transform.SetParent(libraryBg.transform, false);
            var scrollRectTransform = scrollObj.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.sizeDelta = Vector2.zero;
            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(1, 1, 1, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var vpRect = viewport.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            scrollRect.viewport = vpRect;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(0, 1);
            contentRect.pivot = new Vector2(0, 0.5f);
            contentRect.sizeDelta = new Vector2(0, 0);
            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(20, 20, 10, 10);
            hlg.spacing = 15;
            hlg.childControlHeight = true;
            hlg.childControlWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRect;

            assetContentParent = content.transform;

            // "Ana Menü" butonu (sol alt)
            var homeBtn = CreateButtonUI("HomeBtn", panel.transform,
                new Vector2(0.02f, 0.23f), new Vector2(0.25f, 0.30f),
                new Color(0.3f, 0.3f, 0.35f, 0.9f), "← Menü", 30);
            homeBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (appManager != null) appManager.Reset();
            });
        }

        // --- KAYIT PANELİ ---
        private void BuildRecordPanel()
        {
            var panel = CreatePanel("RecordPanel");
            recordPanel = panel;
            panel.alpha = 0;
            panel.gameObject.SetActive(false);

            // Kayıt göstergesi (sol üst)
            var dotObj = new GameObject("RecDot");
            dotObj.transform.SetParent(panel.transform, false);
            var dotRect = dotObj.AddComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0, 1);
            dotRect.anchorMax = new Vector2(0, 1);
            dotRect.anchoredPosition = new Vector2(60, -60);
            dotRect.sizeDelta = new Vector2(40, 40);
            recordDot = dotObj.AddComponent<Image>();
            recordDot.color = RedAccent;

            // Süre
            recordTimerText = CreateText(panel.transform, "00:00", 48, TextAnchor.MiddleLeft,
                new Vector2(0.1f, 0.92f), new Vector2(0.4f, 0.98f), Color.white);
            AddOutline(recordTimerText.gameObject);

            // DURDUR butonu
            var stopBtn = CreateButtonUI("StopBtn", panel.transform,
                new Vector2(0.2f, 0.04f), new Vector2(0.8f, 0.12f),
                RedAccent, "■ KAYDI DURDUR", 44);
            stopBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (videoRecorder != null && videoRecorder.IsRecording)
                    videoRecorder.StopRecording();
                if (appManager != null) appManager.TransitionTo(AppState.Placing);
                ShowPrompt("Video kaydedildi!", 3f);
            });
        }

        // --- PROMPT OVERLAY ---
        private void BuildPromptOverlay()
        {
            var promptBg = CreateRect("PromptBg", canvasObj.transform,
                new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.82f));
            promptBg.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);

            promptText = CreateText(promptBg.transform, "", 40, TextAnchor.MiddleCenter,
                new Vector2(0, 0), new Vector2(1, 1), Color.white);
            AddOutline(promptText.gameObject);
        }

        // ================================================================
        // KATALOG DOLDURMA
        // ================================================================

        private void PopulateAssetLibrary()
        {
            if (assetPlacer == null || assetPlacer.Catalog == null) return;

            ClearAssetCards();
            var entries = assetPlacer.Catalog.Entries;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.prefab == null) continue;

                // Kategori filtresi
                if (currentFilter != AssetCategory.All && entry.category != currentFilter)
                    continue;

                int index = i;
                var card = CreateAssetCard(entry, index);
                assetCards.Add(card);
            }
        }

        private GameObject CreateAssetCard(AssetEntry entry, int catalogIndex)
        {
            var card = new GameObject($"Card_{entry.assetId}");
            card.transform.SetParent(assetContentParent, false);

            var rect = card.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 0);
            var layout = card.AddComponent<LayoutElement>();
            layout.preferredWidth = 200;
            layout.minWidth = 200;

            var bg = card.AddComponent<Image>();
            bg.color = CardBg;

            var btn = card.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => OnAssetCardClicked(catalogIndex, card));

            // Kategori ikonu
            string icon = GetCategoryIcon(entry.category);
            var iconText = CreateText(card.transform, icon, 40, TextAnchor.MiddleCenter,
                new Vector2(0, 0.5f), new Vector2(1, 1f), GetCategoryColor(entry.category));

            // İsim
            var nameText = CreateText(card.transform, entry.displayName, 24, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0f), new Vector2(0.95f, 0.5f), Color.white);

            return card;
        }

        private void ClearAssetCards()
        {
            foreach (var card in assetCards)
            {
                if (card != null) Destroy(card);
            }
            assetCards.Clear();
            selectedCardIndex = -1;
        }

        // ================================================================
        // OLAY İŞLEYİCİLER
        // ================================================================

        private void OnStateChanged(AppState prev, AppState next)
        {
            ShowPanel(next);
        }

        private void ShowPanel(AppState state)
        {
            StartCoroutine(TransitionPanels(state));
        }

        private IEnumerator TransitionPanels(AppState state)
        {
            // Tüm panelleri aynı anda gizle
            StartCoroutine(FadePanel(welcomePanel, false));
            StartCoroutine(FadePanel(scanPanel, false));
            StartCoroutine(FadePanel(placePanel, false));
            StartCoroutine(FadePanel(recordPanel, false));
            
            yield return new WaitForSeconds(0.25f);

            // Hedef paneli göster
            switch (state)
            {
                case AppState.Idle:
                    yield return StartCoroutine(FadePanel(welcomePanel, true));
                    break;
                case AppState.Scanning:
                    yield return StartCoroutine(FadePanel(scanPanel, true));
                    break;
                case AppState.Placing:
                    if (catalogLoaded) PopulateAssetLibrary();
                    yield return StartCoroutine(FadePanel(placePanel, true));
                    break;
                case AppState.Recording:
                    recordStartTime = Time.time;
                    if (videoRecorder != null && !videoRecorder.IsRecording)
                        videoRecorder.StartRecording();
                    yield return StartCoroutine(FadePanel(recordPanel, true));
                    break;
            }
        }

        private IEnumerator FadePanel(CanvasGroup panel, bool show)
        {
            if (panel == null) yield break;

            if (show) panel.gameObject.SetActive(true);
            float start = panel.alpha;
            float end = show ? 1f : 0f;
            float duration = 0.25f;
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                panel.alpha = Mathf.Lerp(start, end, time / duration);
                yield return null;
            }
            panel.alpha = end;

            if (!show)
            {
                panel.gameObject.SetActive(false);
            }
            panel.blocksRaycasts = show;
            panel.interactable = show;
        }

        private void OnCategorySelected(AssetCategory cat, int btnIndex)
        {
            currentFilter = cat;
            // Buton renklerini güncelle
            for (int i = 0; i < filterButtons.Count; i++)
            {
                var img = filterButtons[i].GetComponent<Image>();
                if (img != null) img.color = (i == btnIndex) ? GreenAccent : CardBg;
            }
            PopulateAssetLibrary();
        }

        private void OnAssetCardClicked(int catalogIndex, GameObject card)
        {
            if (assetPlacer == null) return;

            selectedCardIndex = catalogIndex;
            assetPlacer.SelectCatalogEntry(catalogIndex);

            // Tüm kartların rengini sıfırla
            foreach (var c in assetCards)
            {
                if (c != null)
                {
                    var img = c.GetComponent<Image>();
                    if (img != null) img.color = CardBg;
                }
            }
            // Seçili kartı vurgula
            var cardImg = card.GetComponent<Image>();
            if (cardImg != null) cardImg.color = CardSelected;

            var entry = assetPlacer.Catalog.Entries[catalogIndex];
            if (placePromptText != null)
                placePromptText.text = $"{entry.displayName} seçildi — Yere dokunarak yerleştirin";
        }

        // ================================================================
        // PUBLIC API
        // ================================================================

        public void ShowPrompt(string message, float duration = 0)
        {
            if (promptText == null) return;
            promptText.text = message;
            if (promptCoroutine != null) StopCoroutine(promptCoroutine);
            if (duration > 0) promptCoroutine = StartCoroutine(ClearPromptAfter(duration));
        }

        private IEnumerator ClearPromptAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (promptText != null) promptText.text = "";
        }

        public void SetCrosshairColor(Color color)
        {
            if (crosshairObj != null)
            {
                var txt = crosshairObj.GetComponent<Text>();
                if (txt != null) txt.color = color;
            }
        }

        // ================================================================
        // UPDATE
        // ================================================================

        private void Update()
        {
            // Kayıt süresini güncelle
            if (appManager != null && appManager.CurrentState == AppState.Recording)
            {
                float elapsed = Time.time - recordStartTime;
                int min = (int)(elapsed / 60);
                int sec = (int)(elapsed % 60);
                if (recordTimerText != null)
                    recordTimerText.text = $"{min:00}:{sec:00}";

                // Yanıp sönen nokta
                if (recordDot != null)
                    recordDot.color = new Color(RedAccent.r, RedAccent.g, RedAccent.b,
                        0.5f + 0.5f * Mathf.Sin(Time.time * 4f));
            }

            // Yerleştirilen sayacı
            if (appManager != null && appManager.CurrentState == AppState.Placing && placeCounterText != null && assetPlacer != null)
            {
                placeCounterText.text = $"Yerleştirilen: {assetPlacer.PlacedAssets.Count}";
            }
        }

        // ================================================================
        // YARDIMCI METOTLAR
        // ================================================================

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
            }
        }

        private CanvasGroup CreatePanel(string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(canvasObj.transform, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            obj.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            var cg = obj.AddComponent<CanvasGroup>();
            return cg;
        }

        private RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private Text CreateText(Transform parent, string text, int fontSize, TextAnchor anchor,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = defaultFont;
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            return txt;
        }

        private void AddOutline(GameObject obj)
        {
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(2, -2);
        }

        private GameObject CreateButtonUI(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Color bgColor, string label, int fontSize)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            var img = btnObj.AddComponent<Image>();
            img.color = bgColor;
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtObj = CreateText(btnObj.transform, label, fontSize, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Color.white);
            AddOutline(txtObj.gameObject);

            return btnObj;
        }

        private string GetCategoryIcon(AssetCategory cat)
        {
            return cat switch
            {
                AssetCategory.Furniture => "▣",
                AssetCategory.Decoration => "✿",
                AssetCategory.Structural => "⬡",
                AssetCategory.Lighting => "☀",
                _ => "◈"
            };
        }

        private Color GetCategoryColor(AssetCategory cat)
        {
            return cat switch
            {
                AssetCategory.Furniture => new Color(0.6f, 0.4f, 0.2f),
                AssetCategory.Decoration => new Color(0.4f, 0.7f, 0.4f),
                AssetCategory.Structural => new Color(0.5f, 0.5f, 0.7f),
                AssetCategory.Lighting => YellowAccent,
                _ => Color.white
            };
        }
    }
}
