using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class SetAppIcon
{
    static SetAppIcon()
    {
        EditorApplication.delayCall += ApplyIcon;
    }

    static void ApplyIcon()
    {
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/AppIcon.png");
        if (icon != null)
        {
            Texture2D[] icons = new Texture2D[] { icon, icon, icon, icon, icon, icon, icon, icon, icon, icon, icon, icon, icon, icon, icon, icon, icon, icon, icon, icon, icon };
            
            // For iOS specifically
            var iOSIconKinds = PlayerSettings.GetSupportedIconKindsForPlatform(BuildTargetGroup.iOS);
            foreach (var kind in iOSIconKinds)
            {
                var sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.iOS, kind);
                Texture2D[] kindIcons = new Texture2D[sizes.Length];
                for (int i = 0; i < sizes.Length; i++)
                {
                    kindIcons[i] = icon;
                }
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, kindIcons, kind);
            }
            
            AssetDatabase.SaveAssets();
        }
    }
}
