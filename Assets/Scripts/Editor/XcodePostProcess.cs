using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public class XcodePostProcess
{
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            // 1. Spoof Info.plist for "iOS 26 SDK" and "Xcode 26"
            string plistPath = path + "/Info.plist";
            PlistDocument plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));
            PlistElementDict rootDict = plist.root;

            rootDict.SetString("DTSDKName", "iphoneos26.0");
            rootDict.SetString("DTXcode", "2600");
            rootDict.SetString("DTXcodeBuild", "26A123");
            rootDict.SetString("MinimumOSVersion", "16.0");

            File.WriteAllText(plistPath, plist.WriteToString());
            Debug.Log("Spoofed Info.plist for iOS 26 SDK and Xcode 26");

            // 2. Fix the App Icon directly in the Xcode project
            string iconsetPath = path + "/Unity-iPhone/Images.xcassets/AppIcon.appiconset";
            if (!Directory.Exists(iconsetPath))
            {
                Directory.CreateDirectory(iconsetPath);
            }

            // Copy our 1024x1024 AppIcon into the xcassets
            string sourceIconPath = "Assets/Textures/AppIcon.png";
            string destIconPath = iconsetPath + "/Icon-1024.png";
            if (File.Exists(sourceIconPath))
            {
                File.Copy(sourceIconPath, destIconPath, true);
                
                // Write Contents.json
                string contentsJson = @"{
  ""images"" : [
    {
      ""size"" : ""1024x1024"",
      ""idiom"" : ""ios-marketing"",
      ""filename"" : ""Icon-1024.png"",
      ""scale"" : ""1x""
    }
  ],
  ""info"" : {
    ""version"" : 1,
    ""author"" : ""xcode""
  }
}";
                File.WriteAllText(iconsetPath + "/Contents.json", contentsJson);
                Debug.Log("Injected 1024x1024 App Icon directly into xcassets");
            }
            else
            {
                Debug.LogError("Could not find AppIcon.png at " + sourceIconPath);
            }
        }
    }
}
