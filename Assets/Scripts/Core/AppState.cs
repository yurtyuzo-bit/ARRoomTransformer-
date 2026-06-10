namespace ARRoomTransformer
{
    /// <summary>
    /// Uygulama durum makinesi durumları.
    /// AppManager (Idle/Scanning/Placing/Recording) ve UIManager
    /// (MainMenu/RoomScanning/AssetPlacement/Recording/SceneLoading/Settings)
    /// enum'larını tek çatı altında birleştirir.
    ///
    /// Sayısal değerler AppManager akışıyla uyumludur:
    ///   0 = Boşta / Ana Menü
    ///   1 = Tarama
    ///   2 = Yerleştirme
    ///   3 = Kayıt
    ///   4 = Sahne Yükleme
    ///   5 = Ayarlar
    /// </summary>
    public enum AppState
    {
        // ── Core flow states ────────────────────────────────────────
        /// <summary>Uygulama boşta, Ana Menü görünür.</summary>
        Idle = 0,

        /// <summary>Oda taranıyor, AR düzlemleri algılanıyor.</summary>
        Scanning = 1,

        /// <summary>Asset yerleştirme modu.</summary>
        Placing = 2,

        /// <summary>Video kayıt modu.</summary>
        Recording = 3,

        // ── Extended UI states (UIManager) ───────────────────────────
        /// <summary>Sahne yükleme/listeleme ekranı.</summary>
        SceneLoading = 4,

        /// <summary>Ayarlar ekranı.</summary>
        Settings = 5,

        // ── Aliases — UIManager isimlendirmesiyle uyumluluk için ─────
        /// <summary>Idle ile aynı — Ana Menü.</summary>
        MainMenu = 0,

        /// <summary>Scanning ile aynı — Oda tarama.</summary>
        RoomScanning = 1,

        /// <summary>Placing ile aynı — Asset yerleştirme.</summary>
        AssetPlacement = 2,
    }
}
