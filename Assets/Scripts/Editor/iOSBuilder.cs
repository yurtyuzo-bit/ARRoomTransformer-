using UnityEditor;
using UnityEngine;
using System.IO;

namespace ARRoomTransformer.Editor
{
    public class iOSBuilder
    {
        [MenuItem("ARRoomTransformer/1. iOS Ayarlarını Yap ve Xcode Projesi Çıktısı Al", false, 50)]
        public static void BuildForiOS()
        {
            Debug.Log("iOS Ayarları yapılıyor...");

            // Gerekli ARKit ve iOS ayarları
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.samil.arroomtransformer");
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.iOS.buildNumber = "1";
            
            // İzinler
            PlayerSettings.iOS.cameraUsageDescription = "Odanızı 3D taramak ve AR özellikleri için kameraya ihtiyaç vardır.";
            PlayerSettings.iOS.locationUsageDescription = "AR konumlandırma için gereklidir.";
            
            // Minimum iOS versiyonu (LiDAR ve ARKit 6 için en az 15.0 veya 16.0 önerilir)
            PlayerSettings.iOS.targetOSVersionString = "16.0";
            
            // Mimari ayarları
            PlayerSettings.SetArchitecture(UnityEditor.Build.NamedBuildTarget.iOS, (int)AppleMobileArchitecture.ARM64);

            
            // Build klasörünü hazırla
            string buildPath = "Builds/iOS";
            if (!Directory.Exists("Builds"))
            {
                Directory.CreateDirectory("Builds");
            }

            Debug.Log("Xcode projesi derleniyor, bu işlem birkaç dakika sürebilir. Lütfen bekleyin...");
            
            // Sahneler
            string[] scenes = { "Assets/Scenes/ARRoomTransformer.unity" };

            // Build al
            BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            });

            Debug.Log("iOS Build Başarılı! Klasör: " + buildPath);
            EditorUtility.RevealInFinder(buildPath);
            EditorUtility.DisplayDialog("Xcode Projesi Hazır", "iOS projesi başarıyla dışa aktarıldı!\n\nKlasör: " + buildPath + "\n\nŞimdi bu klasörü Sanal Mac'e kopyalamanız gerekiyor.", "Tamam");
        }
    }
}
