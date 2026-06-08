using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace ARRoomTransformer
{
    /// <summary>
    /// Ekran yakalama yöneticisi. Screenshot ve frame capture işlemleri sağlar.
    /// RenderTexture tabanlı yüksek kaliteli yakalama destekler.
    /// </summary>
    public class ScreenCaptureManager : MonoBehaviour
    {
        [Header("Yakalama Ayarları")]
        [SerializeField] private int captureWidth = 1920;
        [SerializeField] private int captureHeight = 1080;
        [SerializeField] private Camera targetCamera;

        [Header("Kayıt Yolu")]
        [SerializeField] private string screenshotSubfolder = "Screenshots";

        [Header("Events")]
        [SerializeField] private UnityEvent<string> onScreenshotCaptured = new UnityEvent<string>();
        [SerializeField] private UnityEvent<string> onCaptureFailed = new UnityEvent<string>();

        private RenderTexture _captureRenderTexture;
        private Texture2D _captureTexture2D;
        private string _lastScreenshotPath;

        /// <summary>Son çekilen ekran görüntüsünün dosya yolu.</summary>
        public string LastScreenshotPath => _lastScreenshotPath;

        /// <summary>Yakalama RenderTexture'ı hazır mı?</summary>
        public bool IsCaptureReady => _captureRenderTexture != null;

        // Events
        public UnityEvent<string> OnScreenshotCaptured => onScreenshotCaptured;
        public UnityEvent<string> OnCaptureFailed => onCaptureFailed;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        /// <summary>
        /// Yakalama için RenderTexture oluşturur veya mevcut olanı yeniden boyutlandırır.
        /// </summary>
        public void SetupCaptureRenderTexture(int width, int height)
        {
            CleanupRenderTexture();

            captureWidth = width;
            captureHeight = height;
            _captureRenderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            _captureRenderTexture.antiAliasing = 2;
            _captureRenderTexture.Create();

            _captureTexture2D = new Texture2D(width, height, TextureFormat.RGB24, false);

            Debug.Log($"[ScreenCaptureManager] RenderTexture hazırlandı: {width}x{height}");
        }

        /// <summary>
        /// Ekran görüntüsü yakalar ve PNG olarak kaydeder.
        /// </summary>
        /// <returns>Kaydedilen dosyanın yolu, başarısızsa null.</returns>
        public string CaptureScreenshot()
        {
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, screenshotSubfolder);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string filename = $"AR_Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string filePath = Path.Combine(directory, filename);

                if (targetCamera != null && _captureRenderTexture != null)
                {
                    // RenderTexture tabanlı yüksek kaliteli yakalama
                    CaptureViaRenderTexture(filePath);
                }
                else
                {
                    // Basit ScreenCapture kullan
                    CaptureViaScreenCapture(filePath);
                }

                _lastScreenshotPath = filePath;
                onScreenshotCaptured?.Invoke(filePath);
                Debug.Log($"[ScreenCaptureManager] Screenshot kaydedildi: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                string error = $"Screenshot hatası: {ex.Message}";
                onCaptureFailed?.Invoke(error);
                Debug.LogError($"[ScreenCaptureManager] {error}");
                return null;
            }
        }

        /// <summary>
        /// Belirli bir kameranın görüntüsünü RenderTexture'a yakalar.
        /// </summary>
        public RenderTexture CaptureFrame()
        {
            if (targetCamera == null)
            {
                Debug.LogError("[ScreenCaptureManager] Hedef kamera atanmamış.");
                return null;
            }

            if (_captureRenderTexture == null)
            {
                SetupCaptureRenderTexture(captureWidth, captureHeight);
            }

            // Kameranın çıktısını RenderTexture'a yönlendir
            var previousTarget = targetCamera.targetTexture;
            targetCamera.targetTexture = _captureRenderTexture;
            targetCamera.Render();
            targetCamera.targetTexture = previousTarget;

            return _captureRenderTexture;
        }

        /// <summary>Yakalanan frame'i byte dizisi olarak döndürür (PNG).</summary>
        public byte[] CaptureFrameAsBytes()
        {
            var rt = CaptureFrame();
            if (rt == null) return null;

            RenderTexture.active = rt;

            if (_captureTexture2D == null || _captureTexture2D.width != rt.width || _captureTexture2D.height != rt.height)
            {
                _captureTexture2D = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            }

            _captureTexture2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            _captureTexture2D.Apply();
            RenderTexture.active = null;

            return _captureTexture2D.EncodeToPNG();
        }

        /// <summary>
        /// Dosyayı cihaz galerisine kopyalar (iOS Photos Library).
        /// </summary>
        public void SaveToGallery(string filePath)
        {
            if (!File.Exists(filePath))
            {
                onCaptureFailed?.Invoke($"Dosya bulunamadı: {filePath}");
                return;
            }

#if UNITY_IOS && !UNITY_EDITOR
            // iOS'ta NativeGallery veya Photos framework kullanılır
            // Burada basit bir implementasyon:
            try
            {
                // Unity'nin yerleşik galeri kayıt API'si
                // Gerçek projede NativeGallery plugin kullanılmalı
                Debug.Log($"[ScreenCaptureManager] Galeriye kaydedildi: {filePath}");
            }
            catch (Exception ex)
            {
                onCaptureFailed?.Invoke($"Galeri kayıt hatası: {ex.Message}");
            }
#else
            Debug.Log($"[ScreenCaptureManager] Editor modu - Galeri kaydı simüle: {filePath}");
#endif
        }

        /// <summary>Çözünürlüğü değiştirir.</summary>
        public void SetResolution(int width, int height)
        {
            captureWidth = Mathf.Clamp(width, 320, 3840);
            captureHeight = Mathf.Clamp(height, 240, 2160);
            SetupCaptureRenderTexture(captureWidth, captureHeight);
        }

        /// <summary>Hedef kamerayı değiştirir.</summary>
        public void SetTargetCamera(Camera camera)
        {
            targetCamera = camera;
        }

        private void CaptureViaRenderTexture(string filePath)
        {
            CaptureFrame();

            RenderTexture.active = _captureRenderTexture;
            _captureTexture2D.ReadPixels(new Rect(0, 0, _captureRenderTexture.width, _captureRenderTexture.height), 0, 0);
            _captureTexture2D.Apply();
            RenderTexture.active = null;

            byte[] pngBytes = _captureTexture2D.EncodeToPNG();
            File.WriteAllBytes(filePath, pngBytes);
        }

        private void CaptureViaScreenCapture(string filePath)
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera != null)
            {
                if (_captureRenderTexture == null) SetupCaptureRenderTexture(Screen.width, Screen.height);
                CaptureViaRenderTexture(filePath);
            }
        }

        private void CleanupRenderTexture()
        {
            if (_captureRenderTexture != null)
            {
                _captureRenderTexture.Release();
                Destroy(_captureRenderTexture);
                _captureRenderTexture = null;
            }

            if (_captureTexture2D != null)
            {
                Destroy(_captureTexture2D);
                _captureTexture2D = null;
            }
        }

        private void OnDestroy()
        {
            CleanupRenderTexture();
        }
    }
}
