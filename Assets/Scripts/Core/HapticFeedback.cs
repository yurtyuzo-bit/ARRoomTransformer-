using UnityEngine;

namespace ARRoomTransformer
{
    /// <summary>
    /// iOS dokunmatik titreşim (haptic) geri bildirimi.
    /// Kullanıcı etkileşimlerinde fiziksel geri bildirim sağlar.
    /// </summary>
    public static class HapticFeedback
    {
        private static bool _isEnabled = true;

        /// <summary>Haptic feedback açık mı?</summary>
        public static bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>Hafif titreşim — UI dokunmaları, küçük geri bildirimler.</summary>
        public static void Light()
        {
            if (!_isEnabled) return;
#if UNITY_IOS && !UNITY_EDITOR
            TriggerImpact(0); // UIImpactFeedbackStyleLight
#endif
        }

        /// <summary>Orta titreşim — asset yerleştirme, köşe işaretleme.</summary>
        public static void Medium()
        {
            if (!_isEnabled) return;
#if UNITY_IOS && !UNITY_EDITOR
            TriggerImpact(1); // UIImpactFeedbackStyleMedium
#endif
        }

        /// <summary>Ağır titreşim — silme, hata, önemli aksiyonlar.</summary>
        public static void Heavy()
        {
            if (!_isEnabled) return;
#if UNITY_IOS && !UNITY_EDITOR
            TriggerImpact(2); // UIImpactFeedbackStyleHeavy
#endif
        }

        /// <summary>Başarı titreşimi — işlem tamamlandı.</summary>
        public static void Success()
        {
            if (!_isEnabled) return;
#if UNITY_IOS && !UNITY_EDITOR
            TriggerNotification(0); // UINotificationFeedbackTypeSuccess
#endif
        }

        /// <summary>Uyarı titreşimi.</summary>
        public static void Warning()
        {
            if (!_isEnabled) return;
#if UNITY_IOS && !UNITY_EDITOR
            TriggerNotification(1); // UINotificationFeedbackTypeWarning
#endif
        }

        /// <summary>Hata titreşimi.</summary>
        public static void Error()
        {
            if (!_isEnabled) return;
#if UNITY_IOS && !UNITY_EDITOR
            TriggerNotification(2); // UINotificationFeedbackTypeError
#endif
        }

        /// <summary>Seçim değişikliği titreşimi — liste kaydırma, değer değiştirme.</summary>
        public static void Selection()
        {
            if (!_isEnabled) return;
#if UNITY_IOS && !UNITY_EDITOR
            TriggerSelection();
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        // iOS Native Plugin çağrıları
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void _triggerImpactFeedback(int style);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void _triggerNotificationFeedback(int type);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void _triggerSelectionFeedback();

        private static void TriggerImpact(int style)
        {
            try { _triggerImpactFeedback(style); }
            catch (System.Exception) { /* Plugin yüklü değilse sessizce geç */ }
        }

        private static void TriggerNotification(int type)
        {
            try { _triggerNotificationFeedback(type); }
            catch (System.Exception) { }
        }

        private static void TriggerSelection()
        {
            try { _triggerSelectionFeedback(); }
            catch (System.Exception) { }
        }
#endif
    }
}
