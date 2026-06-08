using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARRoomTransformer
{
    // ================================================================
    // Temel Serileştirilebilir Yapılar
    // ================================================================

    /// <summary>
    /// JSON serileştirme için Vector3 sarmalayıcısı.
    /// UnityEngine.Vector3 ile implicit dönüşüm destekler.
    /// </summary>
    [System.Serializable]
    public struct Vector3Serializable
    {
        public float x;
        public float y;
        public float z;

        public Vector3Serializable(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3Serializable(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        /// <summary>Vector3Serializable → UnityEngine.Vector3</summary>
        public static implicit operator Vector3(Vector3Serializable v)
            => new Vector3(v.x, v.y, v.z);

        /// <summary>UnityEngine.Vector3 → Vector3Serializable</summary>
        public static implicit operator Vector3Serializable(Vector3 v)
            => new Vector3Serializable(v.x, v.y, v.z);

        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2})";
    }

    /// <summary>
    /// JSON serileştirme için Quaternion sarmalayıcısı.
    /// UnityEngine.Quaternion ile implicit dönüşüm destekler.
    /// </summary>
    [System.Serializable]
    public struct QuaternionSerializable
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public QuaternionSerializable(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public QuaternionSerializable(Quaternion q)
        {
            x = q.x;
            y = q.y;
            z = q.z;
            w = q.w;
        }

        /// <summary>QuaternionSerializable → UnityEngine.Quaternion</summary>
        public static implicit operator Quaternion(QuaternionSerializable q)
            => new Quaternion(q.x, q.y, q.z, q.w);

        /// <summary>UnityEngine.Quaternion → QuaternionSerializable</summary>
        public static implicit operator QuaternionSerializable(Quaternion q)
            => new QuaternionSerializable(q.x, q.y, q.z, q.w);

        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2}, {w:F2})";
    }

    // ================================================================
    // Algılanan Düzlem Verisi
    // ================================================================

    /// <summary>
    /// ARKit tarafından algılanan bir düzlemin serileştirilebilir verisi.
    /// </summary>
    [System.Serializable]
    public class DetectedPlaneData
    {
        /// <summary>ARKit düzlem kimliği.</summary>
        public string planeId;

        /// <summary>Düzlemin merkez noktası (world space).</summary>
        public Vector3Serializable center;

        /// <summary>Düzlemin normal vektörü.</summary>
        public Vector3Serializable normal;

        /// <summary>Düzlemin boyutu (genişlik, yükseklik).</summary>
        public float sizeX;
        public float sizeY;

        /// <summary>Düzlem sınıflandırması (Floor, Wall, Ceiling, vb.).</summary>
        public string classification;

        /// <summary>Düzlem ilk algılanma zamanı.</summary>
        public string detectedAt;

        public DetectedPlaneData()
        {
            planeId = "";
            center = new Vector3Serializable();
            normal = new Vector3Serializable();
            classification = "Unknown";
            detectedAt = DateTime.UtcNow.ToString("o");
        }

        public DetectedPlaneData(string id, Vector3 center, Vector3 normal, Vector2 size, string classification)
        {
            this.planeId = id;
            this.center = center;
            this.normal = normal;
            this.sizeX = size.x;
            this.sizeY = size.y;
            this.classification = classification;
            this.detectedAt = DateTime.UtcNow.ToString("o");
        }
    }

    // ================================================================
    // Oda Verisi
    // ================================================================

    /// <summary>
    /// Taranan odanın tüm verilerini içeren serileştirilebilir sınıf.
    /// Köşe noktaları, duvar yüksekliği, boyutlar ve algılanan düzlemler.
    /// </summary>
    [System.Serializable]
    public class RoomData
    {
        /// <summary>Oda benzersiz kimliği (GUID).</summary>
        public string roomId;

        /// <summary>Odanın köşe noktaları (saat yönünde sıralı).</summary>
        public List<Vector3Serializable> corners;

        /// <summary>Duvar yüksekliği (metre).</summary>
        public float wallHeight;

        /// <summary>Oda genişliği (metre, X ekseni).</summary>
        public float roomWidth;

        /// <summary>Oda uzunluğu (metre, Z ekseni).</summary>
        public float roomLength;

        /// <summary>Oda alanı (metrekare).</summary>
        public float roomArea;

        /// <summary>ARKit tarafından algılanan düzlemler.</summary>
        public List<DetectedPlaneData> detectedPlanes;

        /// <summary>Tarama tarihi (UTC, ISO 8601).</summary>
        public string scanDate;

        /// <summary>Tarama süresi (saniye).</summary>
        public float scanDuration;

        /// <summary>Yeni boş RoomData oluşturur.</summary>
        public RoomData()
        {
            roomId = Guid.NewGuid().ToString();
            corners = new List<Vector3Serializable>();
            detectedPlanes = new List<DetectedPlaneData>();
            wallHeight = 2.5f;
            scanDate = DateTime.UtcNow.ToString("o");
        }

        /// <summary>Köşe noktalarından RoomData oluşturur ve boyutları hesaplar.</summary>
        public RoomData(Vector3[] cornerPositions, float height) : this()
        {
            wallHeight = height;
            foreach (var corner in cornerPositions)
            {
                corners.Add(corner);
            }
            CalculateDimensions();
        }

        /// <summary>Köşe noktalarından oda boyutlarını hesaplar.</summary>
        public void CalculateDimensions()
        {
            if (corners == null || corners.Count < 3) return;

            // Bounding box hesapla
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var corner in corners)
            {
                Vector3 v = corner;
                if (v.x < minX) minX = v.x;
                if (v.x > maxX) maxX = v.x;
                if (v.z < minZ) minZ = v.z;
                if (v.z > maxZ) maxZ = v.z;
            }

            roomWidth = maxX - minX;
            roomLength = maxZ - minZ;

            // Shoelace formülü ile poligon alanı hesapla
            roomArea = CalculatePolygonArea();
        }

        /// <summary>Shoelace formülü ile poligon alanını hesaplar.</summary>
        private float CalculatePolygonArea()
        {
            if (corners.Count < 3) return 0f;

            float area = 0f;
            int count = corners.Count;
            for (int i = 0; i < count; i++)
            {
                Vector3 current = corners[i];
                Vector3 next = corners[(i + 1) % count];
                area += current.x * next.z;
                area -= next.x * current.z;
            }
            return Mathf.Abs(area) / 2f;
        }

        /// <summary>Odanın geçerli veri içerip içermediğini kontrol eder.</summary>
        public bool IsValid()
        {
            return corners != null && corners.Count >= 3 && wallHeight > 0f;
        }

        /// <summary>Oda bilgilerini okunabilir metin olarak döndürür.</summary>
        public override string ToString()
        {
            return $"Oda [{roomId.Substring(0, 8)}] - " +
                   $"Köşe: {corners?.Count ?? 0}, " +
                   $"Boyut: {roomWidth:F1}m x {roomLength:F1}m, " +
                   $"Alan: {roomArea:F1}m², " +
                   $"Yükseklik: {wallHeight:F1}m";
        }
    }
}
