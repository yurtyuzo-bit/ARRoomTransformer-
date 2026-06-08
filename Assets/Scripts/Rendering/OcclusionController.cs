using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARRoomTransformer
{
    /// <summary>
    /// AR occlusion yöneticisi. LiDAR destekli cihazlarda derinlik tabanlı 
    /// occlusion sağlar, desteklenmeyenlerde graceful fallback yapar.
    /// </summary>
    public class OcclusionController : MonoBehaviour
    {
        [Header("AR Referansları")]
        [SerializeField] private AROcclusionManager arOcclusionManager;

        [Header("Occlusion Ayarları")]
        [SerializeField] private OcclusionQuality defaultQuality = OcclusionQuality.Medium;
        [SerializeField] private bool enableEnvironmentOcclusion = true;
        [SerializeField] private bool enableHumanOcclusion = true;

        private bool _isLiDARAvailable;
        private bool _isOcclusionActive;

        /// <summary>Cihazda LiDAR sensörü var mı?</summary>
        public bool IsLiDARAvailable => _isLiDARAvailable;

        /// <summary>Occlusion şu an aktif mi?</summary>
        public bool IsOcclusionActive => _isOcclusionActive;

        /// <summary>Mevcut kalite seviyesi.</summary>
        public OcclusionQuality CurrentQuality { get; private set; }

        /// <summary>Occlusion durumu değiştiğinde tetiklenir.</summary>
        public event Action<bool> OnOcclusionStateChanged;

        /// <summary>Kalite seviyesi değiştiğinde tetiklenir.</summary>
        public event Action<OcclusionQuality> OnQualityChanged;

        /// <summary>Occlusion kalite seviyeleri.</summary>
        public enum OcclusionQuality
        {
            Fastest,
            Medium,
            Best
        }

        private void Awake()
        {
            if (arOcclusionManager == null)
            {
                arOcclusionManager = FindFirstObjectByType<AROcclusionManager>();
            }
        }

        private void Start()
        {
            CheckLiDARAvailability();
            SetQuality(defaultQuality);

            if (_isLiDARAvailable)
            {
                EnableOcclusion();
            }
            else
            {
                Debug.Log("[OcclusionController] LiDAR algılanmadı. Occlusion sınırlı olacak.");
                // LiDAR olmadan da temel occlusion dene
                if (enableEnvironmentOcclusion)
                {
                    EnableOcclusion();
                }
            }
        }

        /// <summary>LiDAR sensörünün mevcut olup olmadığını kontrol eder.</summary>
        private void CheckLiDARAvailability()
        {
            // AR Occlusion Manager'ın depth modunu kontrol et
            if (arOcclusionManager != null)
            {
                // LiDAR olan cihazlarda environment depth desteklenir
                _isLiDARAvailable = (arOcclusionManager.descriptor?.environmentDepthImageSupported ?? UnityEngine.XR.ARSubsystems.Supported.Unknown) == UnityEngine.XR.ARSubsystems.Supported.Supported;

                if (!_isLiDARAvailable)
                {
                    // Alternatif kontrol: depth texture almayı dene
                    _isLiDARAvailable = arOcclusionManager.currentEnvironmentDepthMode != EnvironmentDepthMode.Disabled;
                }
            }

            Debug.Log($"[OcclusionController] LiDAR Durumu: {(_isLiDARAvailable ? "Mevcut" : "Mevcut Değil")}");
        }

        /// <summary>Occlusion'ı etkinleştirir.</summary>
        public void EnableOcclusion()
        {
            if (arOcclusionManager == null) return;

            arOcclusionManager.enabled = true;

            if (enableEnvironmentOcclusion)
            {
                arOcclusionManager.requestedEnvironmentDepthMode = GetEnvironmentDepthMode(CurrentQuality);
            }

            if (enableHumanOcclusion)
            {
                arOcclusionManager.requestedHumanStencilMode = GetHumanStencilMode(CurrentQuality);
                arOcclusionManager.requestedHumanDepthMode = GetHumanDepthMode(CurrentQuality);
            }

            _isOcclusionActive = true;
            OnOcclusionStateChanged?.Invoke(true);
            Debug.Log("[OcclusionController] Occlusion etkinleştirildi.");
        }

        /// <summary>Occlusion'ı devre dışı bırakır.</summary>
        public void DisableOcclusion()
        {
            if (arOcclusionManager == null) return;

            arOcclusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;
            arOcclusionManager.requestedHumanStencilMode = HumanSegmentationStencilMode.Disabled;
            arOcclusionManager.requestedHumanDepthMode = HumanSegmentationDepthMode.Disabled;

            _isOcclusionActive = false;
            OnOcclusionStateChanged?.Invoke(false);
            Debug.Log("[OcclusionController] Occlusion devre dışı bırakıldı.");
        }

        /// <summary>Occlusion durumunu toggle eder.</summary>
        public void ToggleOcclusion()
        {
            if (_isOcclusionActive) DisableOcclusion();
            else EnableOcclusion();
        }

        /// <summary>Kalite seviyesini ayarlar.</summary>
        public void SetQuality(OcclusionQuality quality)
        {
            CurrentQuality = quality;

            if (_isOcclusionActive && arOcclusionManager != null)
            {
                arOcclusionManager.requestedEnvironmentDepthMode = GetEnvironmentDepthMode(quality);
                arOcclusionManager.requestedHumanStencilMode = GetHumanStencilMode(quality);
                arOcclusionManager.requestedHumanDepthMode = GetHumanDepthMode(quality);
            }

            OnQualityChanged?.Invoke(quality);
            Debug.Log($"[OcclusionController] Kalite ayarlandı: {quality}");
        }

        /// <summary>En hızlı kaliteye ayarlar (düşük GPU kullanımı).</summary>
        public void SetFastest() => SetQuality(OcclusionQuality.Fastest);

        /// <summary>Orta kaliteye ayarlar.</summary>
        public void SetMedium() => SetQuality(OcclusionQuality.Medium);

        /// <summary>En iyi kaliteye ayarlar (yüksek GPU kullanımı).</summary>
        public void SetBest() => SetQuality(OcclusionQuality.Best);

        private EnvironmentDepthMode GetEnvironmentDepthMode(OcclusionQuality quality)
        {
            return quality switch
            {
                OcclusionQuality.Fastest => EnvironmentDepthMode.Fastest,
                OcclusionQuality.Medium => EnvironmentDepthMode.Medium,
                OcclusionQuality.Best => EnvironmentDepthMode.Best,
                _ => EnvironmentDepthMode.Medium
            };
        }

        private HumanSegmentationStencilMode GetHumanStencilMode(OcclusionQuality quality)
        {
            if (!enableHumanOcclusion) return HumanSegmentationStencilMode.Disabled;
            return quality switch
            {
                OcclusionQuality.Fastest => HumanSegmentationStencilMode.Fastest,
                OcclusionQuality.Medium => HumanSegmentationStencilMode.Medium,
                OcclusionQuality.Best => HumanSegmentationStencilMode.Best,
                _ => HumanSegmentationStencilMode.Medium
            };
        }

        private HumanSegmentationDepthMode GetHumanDepthMode(OcclusionQuality quality)
        {
            if (!enableHumanOcclusion) return HumanSegmentationDepthMode.Disabled;
            return quality switch
            {
                OcclusionQuality.Fastest => HumanSegmentationDepthMode.Fastest,
                OcclusionQuality.Medium => HumanSegmentationDepthMode.Fastest,
                OcclusionQuality.Best => HumanSegmentationDepthMode.Best,
                _ => HumanSegmentationDepthMode.Fastest
            };
        }
    }
}
