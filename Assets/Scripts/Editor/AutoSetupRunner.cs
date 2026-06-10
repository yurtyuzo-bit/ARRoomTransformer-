using UnityEditor;
using UnityEngine;
using ARRoomTransformer.Editor;

namespace ARRoomTransformer.Editor
{
    /// <summary>
    /// Sahne kurulumu ve asset oluşturma işlemlerini tek seferde çalıştırır.
    /// Unity menüsü: ARRoomTransformer → Tümünü Otomatik Kur
    /// </summary>
    public static class AutoSetupRunner
    {
        [MenuItem("ARRoomTransformer/Tümünü Otomatik Kur", false, 50)]
        public static void RunAll()
        {
            Debug.Log("[AutoSetupRunner] Otomatik kurulum başlıyor...");

            // 1. Sahneyi oluştur (AR Session, XR Origin, UI, Manager'lar)
            ARSceneSetupWizard.CreateFullARScene();

            // 2. Prefab'ları oluştur (15 adet)
            BackroomsAssetCreator.CreateOnlyPrefabs();

            // 3. AssetCatalog'u oluştur ve prefab referanslarını bağla
            BackroomsAssetCreator.CreateOnlyCatalog();

            Debug.Log("[AutoSetupRunner] Otomatik kurulum tamamlandı!");

            EditorUtility.DisplayDialog(
                "Kurulum Tamamlandı! 🎉",
                "Tüm adımlar başarıyla tamamlandı:\n\n" +
                "✓ AR Sahnesi oluşturuldu\n" +
                "✓ 15 Backrooms prefab'ı oluşturuldu\n" +
                "✓ AssetCatalog güncellendi\n\n" +
                "Sonraki adım: Edit → Project Settings → XR Plug-in Management → ARKit'i işaretleyin.",
                "Harika!");
        }
    }
}
