using UnityEngine;

namespace ARRoomTransformer
{
    /// <summary>
    /// Proje genelinde kullanılan sabit değerler.
    /// </summary>
    public static class Constants
    {
        // ================================================================
        // Uygulama
        // ================================================================
        public const string APP_NAME = "ARRoomTransformer";
        public const string APP_VERSION = "0.1.0";

        // ================================================================
        // Oda Varsayılanları
        // ================================================================
        public const float DEFAULT_WALL_HEIGHT = 2.5f;       // metre
        public const float MIN_WALL_HEIGHT = 1.5f;
        public const float MAX_WALL_HEIGHT = 5.0f;
        public const int MIN_CORNERS = 3;
        public const int MAX_CORNERS = 20;
        public const float MIN_ROOM_AREA = 1.0f;             // metrekare
        public const float CORNER_SNAP_DISTANCE = 0.15f;     // metre

        // ================================================================
        // Asset Yerleştirme
        // ================================================================
        public const float MIN_ASSET_SCALE = 0.1f;
        public const float MAX_ASSET_SCALE = 5.0f;
        public const float DEFAULT_ASSET_SCALE = 1.0f;
        public const float ASSET_ROTATION_SNAP = 15f;        // derece
        public const float ASSET_GRID_SNAP = 0.1f;           // metre

        // ================================================================
        // AR Ayarları
        // ================================================================
        public const float AR_RAYCAST_MAX_DISTANCE = 10f;    // metre
        public const float PLANE_DETECTION_TIMEOUT = 30f;    // saniye
        public const float LIGHT_LERP_SPEED = 3f;

        // ================================================================
        // UI
        // ================================================================
        public const float UI_FADE_DURATION = 0.3f;          // saniye
        public const float UI_PANEL_TRANSITION = 0.25f;

        // ================================================================
        // Kayıt
        // ================================================================
        public const int DEFAULT_VIDEO_FPS = 30;
        public const int DEFAULT_COUNTDOWN = 3;
        public const int SCREENSHOT_QUALITY = 100;

        // ================================================================
        // Kayıt/Yükleme
        // ================================================================
        public const string SCENES_FOLDER = "Scenes";
        public const string SCREENSHOTS_FOLDER = "Screenshots";
        public const float AUTO_SAVE_INTERVAL = 60f;          // saniye

        // ================================================================
        // Tag'ler
        // ================================================================
        public const string TAG_ROOM_WALL = "RoomWall";
        public const string TAG_ROOM_FLOOR = "RoomFloor";
        public const string TAG_ROOM_CEILING = "RoomCeiling";
        public const string TAG_CORNER_MARKER = "CornerMarker";
        public const string TAG_PLACED_ASSET = "PlacedAsset";

        // ================================================================
        // Layer'lar (Unity'de manuel tanımlanmalı)
        // ================================================================
        public const string LAYER_AR_PLANES = "ARPlanes";
        public const string LAYER_ROOM_SURFACES = "RoomSurfaces";
        public const string LAYER_PLACED_ASSETS = "PlacedAssets";
        public const string LAYER_UI = "UI";

        // ================================================================
        // Tema İsimleri
        // ================================================================
        public const string THEME_BACKROOMS = "Backrooms";
        public const string THEME_DARK_CORRIDOR = "Dark Corridor";
        public const string THEME_HOSPITAL = "Hospital";
    }
}
