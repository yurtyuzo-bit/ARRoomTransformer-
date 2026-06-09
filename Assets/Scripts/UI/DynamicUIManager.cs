using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace ARRoomTransformer
{
    public class DynamicUIManager : MonoBehaviour
    {
        private AppManager appManager;
        private AssetPlacer assetPlacer;
        private ARVideoRecorder videoRecorder;
        
        private GameObject canvasObj;
        private RectTransform sidePanelRect;
        private Text centerPromptText;
        private Text recordBtnText;
        private GameObject crosshairObj;
        
        private bool isMenuOpen = false;

        private void Start()
        {
            appManager = FindAnyObjectByType<AppManager>();
            assetPlacer = FindAnyObjectByType<AssetPlacer>();
            videoRecorder = FindAnyObjectByType<ARVideoRecorder>();
            
            EnsureEventSystem();
            BuildUI();

            // İlk açılış mesajı
            ShowPrompt("Odayı 4 Köşeden Taramak için Yukarıdaki Butona Basın");
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }
        }

        private void BuildUI()
        {
            // Canvas Oluştur
            canvasObj = new GameObject("DynamicUICanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // --- Üst Sol: Kayıt Butonu ---
            var recordBtnObj = CreateButton("RecordButton", canvasObj.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(200, -80), new Vector2(350, 120), new Color(0.8f, 0.2f, 0.2f, 1f));
            recordBtnText = recordBtnObj.GetComponentInChildren<Text>();
            recordBtnText.text = "KAYDI BAŞLAT";
            recordBtnText.font = defaultFont;
            recordBtnText.fontSize = 40;
            recordBtnText.fontStyle = FontStyle.Bold;
            recordBtnObj.GetComponent<Button>().onClick.AddListener(OnRecordClicked);

            // --- Üst Orta: Odayı Tara Butonu ---
            var scanBtnObj = CreateButton("ScanButton", canvasObj.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(350, 120), new Color(0.2f, 0.6f, 0.2f, 1f));
            var scanText = scanBtnObj.GetComponentInChildren<Text>();
            scanText.text = "ODAYI TARA";
            scanText.font = defaultFont;
            scanText.fontSize = 40;
            scanText.fontStyle = FontStyle.Bold;
            scanBtnObj.GetComponent<Button>().onClick.AddListener(() => 
            {
                appManager.StartScanning();
                ShowPrompt("Sınırı belirlemek için zemine 4 KÖŞE dokunun.");
            });

            // --- Üst Sağ: Hamburger Menü Butonu ---
            var burgerBtnObj = CreateButton("BurgerButton", canvasObj.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -80), new Vector2(120, 120), new Color(0.2f, 0.2f, 0.2f, 1f));
            var burgerText = burgerBtnObj.GetComponentInChildren<Text>();
            burgerText.text = "≡";
            burgerText.font = defaultFont;
            burgerText.fontSize = 80;
            burgerBtnObj.GetComponent<Button>().onClick.AddListener(ToggleMenu);

            // --- Ortadaki Uyarı Metni ---
            var promptObj = new GameObject("PromptText");
            promptObj.transform.SetParent(canvasObj.transform, false);
            var promptRect = promptObj.AddComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0, 0.7f);
            promptRect.anchorMax = new Vector2(1, 0.9f);
            promptRect.anchoredPosition = Vector2.zero;
            promptRect.sizeDelta = Vector2.zero;
            centerPromptText = promptObj.AddComponent<Text>();
            centerPromptText.font = defaultFont;
            centerPromptText.fontSize = 50;
            centerPromptText.alignment = TextAnchor.MiddleCenter;
            centerPromptText.color = Color.white;
            
            var outline = promptObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(3, -3);
            centerPromptText.text = "";

            // --- Merkez İmleç (Crosshair) ---
            crosshairObj = new GameObject("Crosshair");
            crosshairObj.transform.SetParent(canvasObj.transform, false);
            var chRect = crosshairObj.AddComponent<RectTransform>();
            chRect.anchorMin = new Vector2(0.5f, 0.5f);
            chRect.anchorMax = new Vector2(0.5f, 0.5f);
            chRect.anchoredPosition = Vector2.zero;
            chRect.sizeDelta = new Vector2(50, 50);
            var chText = crosshairObj.AddComponent<Text>();
            chText.text = "+";
            chText.font = defaultFont;
            chText.fontSize = 70;
            chText.alignment = TextAnchor.MiddleCenter;
            chText.horizontalOverflow = HorizontalWrapMode.Overflow;
            chText.verticalOverflow = VerticalWrapMode.Overflow;
            chText.color = new Color(1f, 0.2f, 0.2f, 0.9f); // Neon Kırmızı
            crosshairObj.SetActive(false); // Başlangıçta gizli

            // --- Sağ Panel (Kütüphane) ---
            var sidePanelObj = new GameObject("SidePanel");
            sidePanelObj.transform.SetParent(canvasObj.transform, false);
            var bg = sidePanelObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            sidePanelRect = sidePanelObj.GetComponent<RectTransform>();
            sidePanelRect.anchorMin = new Vector2(1, 0);
            sidePanelRect.anchorMax = new Vector2(1, 1);
            sidePanelRect.pivot = new Vector2(1, 0.5f);
            sidePanelRect.sizeDelta = new Vector2(500, 0);
            sidePanelRect.anchoredPosition = new Vector2(500, 0); // Başlangıçta ekran dışı (Gizli)

            // Scroll View
            var scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(sidePanelObj.transform, false);
            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            var scrollRectTransform = scrollObj.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.sizeDelta = new Vector2(0, -200); // Üstten boşluk
            scrollRectTransform.anchoredPosition = new Vector2(0, -100);
            
            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var viewportImg = viewportObj.AddComponent<Image>(); // Mask için gerekli
            viewportImg.color = new Color(1,1,1,0.01f);
            var mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            scrollRect.viewport = viewportRect;

            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            
            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(40, 40, 40, 40);
            vlg.spacing = 30;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            
            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;

            // Kütüphane Öğelerini Doldur
            if (assetPlacer != null && assetPlacer.Catalog != null)
            {
                var titleObj = new GameObject("LibraryTitle");
                titleObj.transform.SetParent(contentObj.transform, false);
                var tTxt = titleObj.AddComponent<Text>();
                tTxt.text = "-- EŞYA KÜTÜPHANESİ --";
                tTxt.font = defaultFont;
                tTxt.fontSize = 35;
                tTxt.color = Color.yellow;
                tTxt.alignment = TextAnchor.MiddleCenter;
                
                var tRect = titleObj.GetComponent<RectTransform>();
                tRect.sizeDelta = new Vector2(0, 60);

                // Dummy veriler (AssetCatalog boşsa)
                string[] names = { "[TEST] Sandalye", "[TEST] Masa", "[TEST] Lamba", "[TEST] Varil", "[TEST] Kutu" };
                PrimitiveType[] primTypes = { PrimitiveType.Cube, PrimitiveType.Cylinder, PrimitiveType.Capsule, PrimitiveType.Cylinder, PrimitiveType.Cube };
                Color[] colors = { Color.cyan, new Color(0.6f, 0.3f, 0f), Color.white, Color.red, new Color(0.8f, 0.6f, 0.2f) };

                for (int i = 0; i < names.Length; i++)
                {
                    var entry = new AssetEntry();
                    entry.displayName = names[i];
                    entry.assetId = i.ToString();
                    entry.defaultScale = 0.3f;
                    
                    // --- CANLI DUMMY 3D MODEL OLUŞTUR ---
                    var dummyObj = GameObject.CreatePrimitive(primTypes[i]);
                    dummyObj.name = names[i] + "_Prefab";
                    
                    // URP Uyumlu Materyal
                    var renderer = dummyObj.GetComponent<MeshRenderer>();
                    Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (urpShader == null) urpShader = Shader.Find("Standard");
                    renderer.material = new Material(urpShader);
                    
                    if (renderer.material.HasProperty("_BaseColor"))
                        renderer.material.SetColor("_BaseColor", colors[i]);
                    else
                        renderer.material.color = colors[i];

                    // Prefab gibi davranması için sahnede gizle
                    dummyObj.SetActive(false);
                    entry.prefab = dummyObj;

                    assetPlacer.Catalog.AddEntry(entry);
                }

                for (int i = 0; i < assetPlacer.Catalog.Count; i++)
                {
                    int index = i;
                    var entry = assetPlacer.Catalog.Entries[i];
                    
                    if (entry.prefab == null) continue; // Boş eşyaları atla

                    var itemBtnObj = CreateButton("Item_" + entry.displayName, contentObj.transform, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 120), new Color(0.3f, 0.3f, 0.3f, 1f));
                    var btnText = itemBtnObj.GetComponentInChildren<Text>();
                    btnText.text = entry.displayName;
                    btnText.font = defaultFont;
                    btnText.fontSize = 40;
                    
                    var btn = itemBtnObj.GetComponent<Button>();
                    btn.onClick.AddListener(() => OnCatalogItemSelected(index));
                }
            }
        }

        private GameObject CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            if (anchorMin == anchorMax) rect.sizeDelta = sizeDelta;
            else rect.sizeDelta = new Vector2(0, sizeDelta.y); 

            var img = btnObj.AddComponent<Image>();
            img.color = color;
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;
            var txt = txtObj.AddComponent<Text>();
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btnObj;
        }

        private void ToggleMenu()
        {
            if (appManager.CurrentState != AppManager.AppState.Placing)
            {
                ShowPrompt("Menüyü açmak için önce odayı tarayıp (4 köşe) sınırları belirleyin!", 3f);
                return;
            }

            isMenuOpen = !isMenuOpen;
            StopCoroutine("AnimatePanel");
            StartCoroutine(AnimatePanel(isMenuOpen ? 0 : 500));
        }

        private IEnumerator AnimatePanel(float targetX)
        {
            float startX = sidePanelRect.anchoredPosition.x;
            float time = 0;
            while (time < 0.25f)
            {
                time += Time.deltaTime;
                float t = time / 0.25f;
                t = t * t * (3f - 2f * t); // Smooth step
                sidePanelRect.anchoredPosition = new Vector2(Mathf.Lerp(startX, targetX, t), 0);
                yield return null;
            }
            sidePanelRect.anchoredPosition = new Vector2(targetX, 0);
        }

        private void OnCatalogItemSelected(int index)
        {
            if (assetPlacer != null)
            {
                assetPlacer.SelectCatalogEntry(index);
                ShowPrompt(assetPlacer.Catalog.Entries[index].displayName + " seçildi. Yere dokunun.", 2f);
                ToggleMenu(); // Menüyü kapat
            }
        }

        private void OnRecordClicked()
        {
            if (videoRecorder != null)
            {
                if (videoRecorder.IsRecording)
                {
                    videoRecorder.StopRecording();
                    recordBtnText.text = "KAYDI BAŞLAT";
                    recordBtnText.transform.parent.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f, 1f);
                    ShowPrompt("Video Galeriye Kaydediliyor...", 3f);
                }
                else
                {
                    videoRecorder.StartRecording();
                    recordBtnText.text = "KAYDI BİTİR";
                    recordBtnText.transform.parent.GetComponent<Image>().color = new Color(0.8f, 0.6f, 0.1f, 1f);
                }
            }
        }

        public void ShowPrompt(string message, float duration = 0)
        {
            centerPromptText.text = message;
            if (duration > 0)
            {
                StopCoroutine("ClearPrompt");
                StartCoroutine(ClearPrompt(duration));
            }
        }

        private IEnumerator ClearPrompt(float delay)
        {
            yield return new WaitForSeconds(delay);
            centerPromptText.text = "";
        }

        private void Update()
        {
            if (appManager != null && crosshairObj != null)
            {
                // İmleç sadece Scanning (Odayı Tara / Sınır Seçme) modunda aktif olur
                crosshairObj.SetActive(appManager.CurrentState == AppManager.AppState.Scanning);
            }
        }

        public void SetCrosshairColor(Color color)
        {
            if (crosshairObj != null)
            {
                var txt = crosshairObj.GetComponent<Text>();
                if (txt != null) txt.color = color;
            }
        }
    }
}
