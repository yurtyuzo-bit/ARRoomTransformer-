using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class XcodePostProcess
{
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            // Inject the App Icons directly into the Xcode project's xcassets
            string iconsetPath = path + "/Unity-iPhone/Images.xcassets/AppIcon.appiconset";
            if (!Directory.Exists(iconsetPath))
            {
                Directory.CreateDirectory(iconsetPath);
            }

            // Copy our 1024x1024 and 120x120 AppIcons into the xcassets
            string sourceIcon1024 = "Assets/Textures/AppIcon.png";
            string sourceIcon120 = "Assets/Textures/Icon-120.png";
            
            if (File.Exists(sourceIcon1024) && File.Exists(sourceIcon120))
            {
                File.Copy(sourceIcon1024, iconsetPath + "/Icon-1024.png", true);
                File.Copy(sourceIcon120, iconsetPath + "/Icon-120.png", true);
                
                // Write Contents.json for xcassets
                string contentsJson = @"{
  ""images"" : [
    {
      ""size"" : ""1024x1024"",
      ""idiom"" : ""ios-marketing"",
      ""filename"" : ""Icon-1024.png"",
      ""scale"" : ""1x""
    },
    {
      ""size"" : ""60x60"",
      ""idiom"" : ""iphone"",
      ""filename"" : ""Icon-120.png"",
      ""scale"" : ""2x""
    }
  ],
  ""info"" : {
    ""version"" : 1,
    ""author"" : ""xcode""
  }
}";
                File.WriteAllText(iconsetPath + "/Contents.json", contentsJson);
                Debug.Log("Injected 1024x1024 and 120x120 App Icons directly into xcassets");
            }
            else
            {
                Debug.LogError("Could not find AppIcon.png or Icon-120.png!");
            }
        }
    }
}
