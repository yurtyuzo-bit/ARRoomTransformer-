using UnityEngine;

namespace ARRoomTransformer
{
    /// <summary>
    /// Proje genelinde kullanılan yardımcı (utility) extension metotları.
    /// </summary>
    public static class Extensions
    {
        // ================================================================
        // Vector3 Extensions
        // ================================================================

        /// <summary>Y bileşenini sıfırlayarak düz bir vektör döndürür (XZ düzlemi).</summary>
        public static Vector3 Flat(this Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>Belirtilen Y değeriyle yeni bir vektör döndürür.</summary>
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);

        /// <summary>Belirtilen X değeriyle yeni bir vektör döndürür.</summary>
        public static Vector3 WithX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);

        /// <summary>Belirtilen Z değeriyle yeni bir vektör döndürür.</summary>
        public static Vector3 WithZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);

        /// <summary>İki nokta arasındaki XZ düzlemindeki mesafeyi hesaplar.</summary>
        public static float FlatDistance(this Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a.Flat(), b.Flat());
        }

        // ================================================================
        // Transform Extensions
        // ================================================================

        /// <summary>Transform'un tüm child objelerini siler.</summary>
        public static void DestroyAllChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>Transform'un world-space pozisyonunu düz (XZ) olarak döndürür.</summary>
        public static Vector3 FlatPosition(this Transform transform)
        {
            return transform.position.Flat();
        }

        /// <summary>Hedef pozisyona Y ekseni etrafında döndürür.</summary>
        public static void LookAtFlat(this Transform transform, Vector3 target)
        {
            Vector3 direction = (target - transform.position).Flat();
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        // ================================================================
        // Color Extensions
        // ================================================================

        /// <summary>Rengin alpha değerini değiştirir.</summary>
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        // ================================================================
        // Float Extensions
        // ================================================================

        /// <summary>Değeri belirli bir aralığa yeniden eşler (remap).</summary>
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return Mathf.Lerp(toMin, toMax, Mathf.InverseLerp(fromMin, fromMax, value));
        }

        // ================================================================
        // GameObject Extensions
        // ================================================================

        /// <summary>Component yoksa ekler, varsa mevcut olanı döndürür.</summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>Objenin ve tüm child'larının layer'ını değiştirir.</summary>
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.SetLayerRecursively(layer);
            }
        }
    }
}
