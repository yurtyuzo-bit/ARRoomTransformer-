using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ARRoomTransformer
{
    /// <summary>
    /// Varlık kategorilerini tanımlar.
    /// Asset categories used for filtering and organizing the catalog.
    /// </summary>
    public enum AssetCategory
    {
        /// <summary>Tüm kategoriler.</summary>
        All = 0,
        /// <summary>Mobilya öğeleri — masalar, sandalyeler, dolaplar vb.</summary>
        Furniture,
        /// <summary>Dekoratif öğeler — tablolar, vazolar, bitkiler vb.</summary>
        Decoration,
        /// <summary>Yapısal öğeler — duvarlar, kapılar, pencereler vb.</summary>
        Structural,
        /// <summary>Aydınlatma öğeleri — lambalar, avizeler vb.</summary>
        Lighting
    }

    /// <summary>
    /// Katalogdaki tek bir 3D varlık girişini temsil eder.
    /// Represents a single 3D asset entry in the catalog with all metadata
    /// required for placement, display, and filtering.
    /// </summary>
    [Serializable]
    public class AssetEntry
    {
        /// <summary>
        /// Unique asset identifier for catalog lookups and serialization.
        /// </summary>
        [Tooltip("Katalog aramaları ve serileştirme için benzersiz asset kimliği.")]
        public string assetId;

        /// <summary>
        /// Display name shown in the UI catalog browser.
        /// </summary>
        [Tooltip("Katalog tarayıcısında gösterilen görüntü adı.")]
        public string displayName;

        /// <summary>
        /// Description of the asset shown in detail views.
        /// </summary>
        [Tooltip("Detay görünümünde gösterilen asset açıklaması.")]
        [TextArea(1, 3)]
        public string description;

        /// <summary>
        /// Thumbnail sprite used for catalog UI previews.
        /// </summary>
        [Tooltip("Katalog UI önizlemeleri için küçük resim.")]
        public Sprite thumbnail;

        /// <summary>
        /// Reference to the prefab that will be instantiated when placing this asset.
        /// </summary>
        [Tooltip("Bu varlık yerleştirildiğinde oluşturulacak prefab referansı.")]
        public GameObject prefab;

        /// <summary>
        /// Primary category for broad filtering.
        /// </summary>
        [Tooltip("Geniş filtreleme için birincil kategori.")]
        public AssetCategory category;

        /// <summary>
        /// Tags for fine-grained filtering and search (e.g., "modern", "wooden", "small").
        /// </summary>
        [Tooltip("Ayrıntılı filtreleme ve arama için etiketler.")]
        public List<string> tags = new List<string>();

        /// <summary>
        /// Default scale multiplier applied when the asset is first placed.
        /// </summary>
        [Tooltip("Varlık ilk yerleştirildiğinde uygulanan varsayılan ölçek çarpanı.")]
        [Min(0.01f)]
        public float defaultScale = 1f;

        /// <summary>
        /// Unique identifier generated at creation time. Used for serialization and lookups.
        /// </summary>
        [HideInInspector]
        public string uniqueId;

        /// <summary>
        /// Checks whether this entry matches a given search query against name and tags.
        /// </summary>
        /// <param name="query">The search query string (case-insensitive).</param>
        /// <returns>True if the query matches the display name or any tag.</returns>
        public bool MatchesSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            string lowerQuery = query.ToLowerInvariant();

            if (!string.IsNullOrEmpty(displayName) &&
                displayName.ToLowerInvariant().Contains(lowerQuery))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(description) &&
                description.ToLowerInvariant().Contains(lowerQuery))
            {
                return true;
            }

            if (tags != null)
            {
                for (int i = 0; i < tags.Count; i++)
                {
                    if (!string.IsNullOrEmpty(tags[i]) &&
                        tags[i].ToLowerInvariant().Contains(lowerQuery))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    /// <summary>
    /// AR sahnesine yerleştirilebilecek 3D varlıkların ScriptableObject tabanlı kataloğu.
    /// ScriptableObject-based catalog that holds a collection of <see cref="AssetEntry"/>
    /// items and provides search, filter, and lookup functionality.
    /// </summary>
    /// <remarks>
    /// Create instances via Assets → Create → ARRoomTransformer → Asset Catalog.
    /// Each catalog can represent a themed collection (e.g., "Backrooms Props", "Office Furniture").
    /// </remarks>
    [CreateAssetMenu(
        fileName = "NewAssetCatalog",
        menuName = "ARRoomTransformer/Asset Catalog",
        order = 100)]
    public class AssetCatalog : ScriptableObject
    {
        [Header("Catalog Metadata")]
        [Tooltip("Katalog için insan tarafından okunabilir ad.")]
        [SerializeField] private string catalogName = "New Catalog";

        [Tooltip("Bu katalog koleksiyonunun açıklaması.")]
        [SerializeField] [TextArea(2, 4)] private string description;

        [Header("Assets")]
        [Tooltip("Bu katalogdaki tüm varlık girişlerinin listesi.")]
        [SerializeField] private List<AssetEntry> entries = new List<AssetEntry>();

        /// <summary>
        /// Human-readable name for this catalog.
        /// </summary>
        public string CatalogName => catalogName;

        /// <summary>
        /// Description of this catalog collection.
        /// </summary>
        public string Description => description;

        /// <summary>
        /// Read-only access to all entries in this catalog.
        /// </summary>
        public IReadOnlyList<AssetEntry> Entries => entries;

        /// <summary>
        /// Total number of entries in the catalog.
        /// </summary>
        public int Count => entries.Count;

        /// <summary>
        /// Total number of entries (alias for editor scripts).
        /// </summary>
        public int EntryCount => entries.Count;

        /// <summary>
        /// Adds a new entry to the catalog.
        /// </summary>
        public void AddEntry(AssetEntry entry)
        {
            if (entry == null) return;
            if (string.IsNullOrEmpty(entry.uniqueId))
                entry.uniqueId = Guid.NewGuid().ToString("N");
            entries.Add(entry);
            _idLookup = null; // Force rebuild
        }

        /// <summary>
        /// Removes all entries from the catalog.
        /// </summary>
        public void ClearEntries()
        {
            entries.Clear();
            _idLookup = null;
        }

        /// <summary>
        /// Retrieves an asset entry by its assetId field.
        /// </summary>
        public AssetEntry GetEntryByAssetId(string assetId)
        {
            if (string.IsNullOrEmpty(assetId)) return null;
            return entries.FirstOrDefault(e => e.assetId == assetId);
        }

        // Lazy-initialized lookup dictionary for fast ID-based access.
        private Dictionary<string, AssetEntry> _idLookup;

        /// <summary>
        /// Ensures all entries have a unique ID assigned. Called automatically on load.
        /// </summary>
        private void OnEnable()
        {
            EnsureUniqueIds();
        }

        /// <summary>
        /// Called in the editor when values change. Re-validates unique IDs.
        /// </summary>
        private void OnValidate()
        {
            EnsureUniqueIds();
        }

        /// <summary>
        /// Assigns unique IDs to any entries that are missing them and rebuilds the lookup cache.
        /// </summary>
        private void EnsureUniqueIds()
        {
            bool dirty = false;
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.IsNullOrEmpty(entries[i].uniqueId))
                {
                    entries[i].uniqueId = Guid.NewGuid().ToString("N");
                    dirty = true;
                }
            }

            // Rebuild lookup
            _idLookup = new Dictionary<string, AssetEntry>(entries.Count);
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.uniqueId))
                    _idLookup[entry.uniqueId] = entry;
            }

#if UNITY_EDITOR
            if (dirty)
                UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Retrieves an asset entry by its unique identifier.
        /// </summary>
        /// <param name="uniqueId">The unique ID of the entry to find.</param>
        /// <returns>The matching <see cref="AssetEntry"/>, or null if not found.</returns>
        public AssetEntry GetEntryById(string uniqueId)
        {
            if (_idLookup == null)
                EnsureUniqueIds();

            _idLookup.TryGetValue(uniqueId, out AssetEntry entry);
            return entry;
        }

        /// <summary>
        /// Retrieves an asset entry by its display name (case-insensitive).
        /// </summary>
        /// <param name="name">The display name to search for.</param>
        /// <returns>The first matching <see cref="AssetEntry"/>, or null if not found.</returns>
        public AssetEntry GetEntryByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            string lowerName = name.ToLowerInvariant();
            return entries.FirstOrDefault(e =>
                !string.IsNullOrEmpty(e.displayName) &&
                e.displayName.ToLowerInvariant() == lowerName);
        }

        /// <summary>
        /// Filters entries by category.
        /// </summary>
        /// <param name="category">The category to filter by.</param>
        /// <returns>An enumerable of entries matching the specified category.</returns>
        public IEnumerable<AssetEntry> GetByCategory(AssetCategory category)
        {
            return entries.Where(e => e.category == category);
        }

        /// <summary>
        /// Filters entries that contain ALL of the specified tags (case-insensitive).
        /// </summary>
        /// <param name="requiredTags">Tags that must all be present on matching entries.</param>
        /// <returns>An enumerable of entries that have all required tags.</returns>
        public IEnumerable<AssetEntry> GetByTags(params string[] requiredTags)
        {
            if (requiredTags == null || requiredTags.Length == 0)
                return entries;

            var lowerTags = requiredTags.Select(t => t.ToLowerInvariant()).ToArray();

            return entries.Where(entry =>
            {
                if (entry.tags == null || entry.tags.Count == 0)
                    return false;

                var entryLowerTags = entry.tags.Select(t => t.ToLowerInvariant()).ToHashSet();
                return lowerTags.All(lt => entryLowerTags.Contains(lt));
            });
        }

        /// <summary>
        /// Filters entries by category AND tags simultaneously.
        /// </summary>
        /// <param name="category">The category to filter by.</param>
        /// <param name="requiredTags">Tags that must all be present on matching entries.</param>
        /// <returns>An enumerable of entries matching both category and tag criteria.</returns>
        public IEnumerable<AssetEntry> GetByCategoryAndTags(AssetCategory category, params string[] requiredTags)
        {
            return GetByCategory(category).Where(entry =>
            {
                if (requiredTags == null || requiredTags.Length == 0)
                    return true;

                if (entry.tags == null || entry.tags.Count == 0)
                    return false;

                var lowerTags = requiredTags.Select(t => t.ToLowerInvariant()).ToArray();
                var entryLowerTags = entry.tags.Select(t => t.ToLowerInvariant()).ToHashSet();
                return lowerTags.All(lt => entryLowerTags.Contains(lt));
            });
        }

        /// <summary>
        /// Performs a free-text search across display names and tags.
        /// </summary>
        /// <param name="query">The search query (case-insensitive).</param>
        /// <returns>An enumerable of entries matching the search query.</returns>
        public IEnumerable<AssetEntry> Search(string query)
        {
            return entries.Where(e => e.MatchesSearch(query));
        }

        /// <summary>
        /// Combined search with category filter.
        /// </summary>
        /// <param name="query">Free-text search query.</param>
        /// <param name="category">Category filter (nullable — pass null to skip category filtering).</param>
        /// <returns>An enumerable of entries matching both the query and category.</returns>
        public IEnumerable<AssetEntry> Search(string query, AssetCategory? category)
        {
            IEnumerable<AssetEntry> results = entries.Where(e => e.MatchesSearch(query));

            if (category.HasValue)
                results = results.Where(e => e.category == category.Value);

            return results;
        }

        /// <summary>
        /// Returns all distinct categories present in the catalog.
        /// </summary>
        /// <returns>An enumerable of distinct categories.</returns>
        public IEnumerable<AssetCategory> GetAvailableCategories()
        {
            return entries.Select(e => e.category).Distinct();
        }

        /// <summary>
        /// Returns all distinct tags present across all entries.
        /// </summary>
        /// <returns>An enumerable of distinct tag strings (lowercase).</returns>
        public IEnumerable<string> GetAllTags()
        {
            return entries
                .Where(e => e.tags != null)
                .SelectMany(e => e.tags)
                .Where(t => !string.IsNullOrEmpty(t))
                .Select(t => t.ToLowerInvariant())
                .Distinct()
                .OrderBy(t => t);
        }
    }
}
