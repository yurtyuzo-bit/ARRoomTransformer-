using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARRoomTransformer
{
    /// <summary>
    /// Tematik materyal setlerini yönetir. Backrooms gibi temaları
    /// oda yüzeylerine (duvar, zemin, tavan) uygular.
    /// </summary>
    public class MaterialManager : MonoBehaviour
    {
        [Header("Tema Listesi")]
        [SerializeField] private List<MaterialTheme> availableThemes = new List<MaterialTheme>();

        [Header("Varsayılan Tema")]
        [SerializeField] private string defaultThemeName = "Backrooms";

        private MaterialTheme _currentTheme;
        private readonly List<Renderer> _wallRenderers = new List<Renderer>();
        private readonly List<Renderer> _floorRenderers = new List<Renderer>();
        private readonly List<Renderer> _ceilingRenderers = new List<Renderer>();

        /// <summary>Şu anki aktif tema.</summary>
        public MaterialTheme CurrentTheme => _currentTheme;

        /// <summary>Mevcut tema isimleri.</summary>
        public List<string> ThemeNames
        {
            get
            {
                var names = new List<string>();
                foreach (var theme in availableThemes) names.Add(theme.themeName);
                return names;
            }
        }

        /// <summary>Tema değiştiğinde tetiklenir.</summary>
        public event Action<MaterialTheme> OnThemeChanged;

        private void Start()
        {
            // Varsayılan Backrooms teması yoksa oluştur
            if (availableThemes.Count == 0)
            {
                CreateDefaultBackroomsTheme();
            }

            // Varsayılan temayı uygula
            ApplyTheme(defaultThemeName);
        }

        /// <summary>İsme göre tema uygular.</summary>
        /// <param name="themeName">Uygulanacak temanın adı.</param>
        /// <returns>Tema bulunup uygulandıysa true.</returns>
        public bool ApplyTheme(string themeName)
        {
            var theme = availableThemes.Find(t =>
                t.themeName.Equals(themeName, StringComparison.OrdinalIgnoreCase));

            if (theme == null)
            {
                Debug.LogWarning($"[MaterialManager] Tema bulunamadı: {themeName}");
                return false;
            }

            _currentTheme = theme;
            FindAndApplyToRoomSurfaces();
            OnThemeChanged?.Invoke(_currentTheme);
            Debug.Log($"[MaterialManager] Tema uygulandı: {themeName}");
            return true;
        }

        /// <summary>İndekse göre tema uygular.</summary>
        public bool ApplyTheme(int index)
        {
            if (index < 0 || index >= availableThemes.Count) return false;
            return ApplyTheme(availableThemes[index].themeName);
        }

        /// <summary>
        /// Sahnedeki tüm oda yüzeylerini bulur ve mevcut temayı uygular.
        /// Duvarlar "RoomWall", zemin "RoomFloor", tavan "RoomCeiling" tag'li olmalıdır.
        /// </summary>
        public void FindAndApplyToRoomSurfaces()
        {
            if (_currentTheme == null) return;

            _wallRenderers.Clear();
            _floorRenderers.Clear();
            _ceilingRenderers.Clear();

            // Tag ile bul
            CollectRenderersWithTag("RoomWall", _wallRenderers);
            CollectRenderersWithTag("RoomFloor", _floorRenderers);
            CollectRenderersWithTag("RoomCeiling", _ceilingRenderers);

            // Materyalleri uygula
            ApplyMaterialToRenderers(_wallRenderers, _currentTheme.wallMaterial);
            ApplyMaterialToRenderers(_floorRenderers, _currentTheme.floorMaterial);
            ApplyMaterialToRenderers(_ceilingRenderers, _currentTheme.ceilingMaterial);

            Debug.Log($"[MaterialManager] Tema uygulandı - Duvarlar: {_wallRenderers.Count}, " +
                      $"Zemin: {_floorRenderers.Count}, Tavan: {_ceilingRenderers.Count}");
        }

        /// <summary>Belirli bir renderer listesine materyal uygular.</summary>
        public void ApplyMaterialToRenderers(List<Renderer> renderers, Material material)
        {
            if (material == null) return;
            foreach (var renderer in renderers)
            {
                if (renderer != null) renderer.material = material;
            }
        }

        /// <summary>Bir materyalin tiling değerini ayarlar.</summary>
        public void SetMaterialTiling(Material material, Vector2 tiling)
        {
            if (material == null) return;
            material.mainTextureScale = tiling;
        }

        /// <summary>Bir materyalin renk tonunu ayarlar.</summary>
        public void SetMaterialTint(Material material, Color tint)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", tint);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", tint);
        }

        /// <summary>Mevcut temanın tüm materyallerine tiling uygular.</summary>
        public void SetThemeTiling(Vector2 wallTiling, Vector2 floorTiling, Vector2 ceilingTiling)
        {
            if (_currentTheme == null) return;
            SetMaterialTiling(_currentTheme.wallMaterial, wallTiling);
            SetMaterialTiling(_currentTheme.floorMaterial, floorTiling);
            SetMaterialTiling(_currentTheme.ceilingMaterial, ceilingTiling);
        }

        /// <summary>Manuel olarak renderer kaydeder (tag kullanmak yerine).</summary>
        public void RegisterWallRenderer(Renderer renderer) => _wallRenderers.Add(renderer);
        public void RegisterFloorRenderer(Renderer renderer) => _floorRenderers.Add(renderer);
        public void RegisterCeilingRenderer(Renderer renderer) => _ceilingRenderers.Add(renderer);

        /// <summary>Yeni tema ekler.</summary>
        public void AddTheme(MaterialTheme theme)
        {
            if (theme != null && !availableThemes.Exists(t => t.themeName == theme.themeName))
            {
                availableThemes.Add(theme);
            }
        }

        private void CollectRenderersWithTag(string tag, List<Renderer> list)
        {
            try
            {
                var objects = GameObject.FindGameObjectsWithTag(tag);
                foreach (var obj in objects)
                {
                    var renderer = obj.GetComponent<Renderer>();
                    if (renderer != null) list.Add(renderer);
                }
            }
            catch (UnityException)
            {
                // Tag tanımlı değilse sessizce geç
                Debug.LogWarning($"[MaterialManager] '{tag}' tag'i tanımlı değil. Tags ayarlarından ekleyin.");
            }
        }

        /// <summary>Varsayılan Backrooms temasını oluşturur (runtime materyallerle).</summary>
        private void CreateDefaultBackroomsTheme()
        {
            var backrooms = new MaterialTheme
            {
                themeName = "Backrooms",
                description = "Klasik Backrooms Level 0 - Sarı duvar kağıdı, nemli halı",

                // Runtime'da basit materyaller oluştur
                wallMaterial = CreateSimpleMaterial("Backrooms_Wall",
                    new Color(0.85f, 0.78f, 0.55f)), // Sarımsı-kirli duvar kağıdı
                floorMaterial = CreateSimpleMaterial("Backrooms_Floor",
                    new Color(0.45f, 0.40f, 0.30f)), // Koyu halı
                ceilingMaterial = CreateSimpleMaterial("Backrooms_Ceiling",
                    new Color(0.90f, 0.88f, 0.80f)), // Beyazımsı tavan
                trimMaterial = CreateSimpleMaterial("Backrooms_Trim",
                    new Color(0.70f, 0.65f, 0.50f))  // Süpürgelik
            };

            availableThemes.Add(backrooms);

            // İkinci tema: Karanlık koridor
            var darkCorridor = new MaterialTheme
            {
                themeName = "Dark Corridor",
                description = "Karanlık koridor - Beton duvarlar, metal zemin",
                wallMaterial = CreateSimpleMaterial("Dark_Wall",
                    new Color(0.25f, 0.25f, 0.28f)),
                floorMaterial = CreateSimpleMaterial("Dark_Floor",
                    new Color(0.15f, 0.15f, 0.18f)),
                ceilingMaterial = CreateSimpleMaterial("Dark_Ceiling",
                    new Color(0.20f, 0.20f, 0.22f)),
                trimMaterial = CreateSimpleMaterial("Dark_Trim",
                    new Color(0.30f, 0.28f, 0.25f))
            };

            availableThemes.Add(darkCorridor);

            // Üçüncü tema: Hastane
            var hospital = new MaterialTheme
            {
                themeName = "Hospital",
                description = "Terk edilmiş hastane - Beyaz fayans, steril ortam",
                wallMaterial = CreateSimpleMaterial("Hospital_Wall",
                    new Color(0.88f, 0.92f, 0.90f)),
                floorMaterial = CreateSimpleMaterial("Hospital_Floor",
                    new Color(0.75f, 0.80f, 0.78f)),
                ceilingMaterial = CreateSimpleMaterial("Hospital_Ceiling",
                    new Color(0.95f, 0.95f, 0.95f)),
                trimMaterial = CreateSimpleMaterial("Hospital_Trim",
                    new Color(0.60f, 0.65f, 0.63f))
            };

            availableThemes.Add(hospital);
        }

        private Material CreateSimpleMaterial(string name, Color color)
        {
            // URP Lit shader kullan, yoksa Standard'a düş
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader) { name = name };

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);

            return mat;
        }
    }

    /// <summary>
    /// Materyal teması veri sınıfı.
    /// Bir tema, oda yüzeylerine uygulanacak materyal setini tanımlar.
    /// </summary>
    [System.Serializable]
    public class MaterialTheme
    {
        /// <summary>Tema adı (örn: "Backrooms", "Dark Corridor").</summary>
        public string themeName;

        /// <summary>Tema açıklaması.</summary>
        public string description;

        /// <summary>Duvar materyali.</summary>
        public Material wallMaterial;

        /// <summary>Zemin materyali.</summary>
        public Material floorMaterial;

        /// <summary>Tavan materyali.</summary>
        public Material ceilingMaterial;

        /// <summary>Süpürgelik/köşe materyali.</summary>
        public Material trimMaterial;
    }
}
