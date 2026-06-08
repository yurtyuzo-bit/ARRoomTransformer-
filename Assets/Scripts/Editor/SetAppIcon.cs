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
#pragma warning disable 0618
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new Texture2D[] { icon });
            
            // To be extra safe, set it specifically for iOS too, letting Unity scale it
            int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.iOS);
            Texture2D[] icons = new Texture2D[sizes.Length];
            for (int i = 0; i < sizes.Length; i++) {
                icons[i] = icon;
            }
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, icons);
#pragma warning restore 0618

            AssetDatabase.SaveAssets();
        }
    }
}
